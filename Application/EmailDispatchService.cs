using Contracts.Application;
using Contracts.DataAccess;
using Contracts.External;
using Contracts.External.Models;
using Domain;
using Microsoft.Extensions.Logging;

namespace Application;

/// <summary>Resolves, renders and delivers templated emails with message-id deduplication.</summary>
public class EmailDispatchService : IEmailDispatchService
{
    private readonly IClientRepository _clientRepository;
    private readonly INotificationTypeRepository _notificationTypeRepository;
    private readonly IAppUow _uow;
    private readonly IEmailSenderRepository _emailSenderRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailDispatchService> _logger;

    private const string DefaultLanguageCode = "en";

    public EmailDispatchService(
        IClientRepository clientRepository,
        INotificationTypeRepository notificationTypeRepository,
        IAppUow uow,
        IEmailSenderRepository emailSenderRepository,
        ITemplateRepository templateRepository,
        IDeliveryRepository deliveryRepository,
        IEmailTemplateRenderer templateRenderer,
        IEmailSender emailSender,
        ILogger<EmailDispatchService> logger)
    {
        _clientRepository = clientRepository;
        _notificationTypeRepository = notificationTypeRepository;
        _uow = uow;
        _emailSenderRepository = emailSenderRepository;
        _templateRepository = templateRepository;
        _deliveryRepository = deliveryRepository;
        _templateRenderer = templateRenderer;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<EmailDispatchResult> SendTemplatedEmailAsync(
        SendTemplatedEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var languageCode = NormalizeLanguageCode(request.LanguageCode);

        var clientSource = request.Source;
        var client = await _clientRepository.GetActiveBySourceAsync(clientSource);
        if (client is null)
        {
            return Permanent(request, $"No active client for '{clientSource}'.");
        }

        var notificationType = await _notificationTypeRepository.GetActiveAsync(client.Id, request.NotificationType);
        if (notificationType is null)
        {
            return Permanent(request, $"No active notification type '{request.NotificationType}' for '{request.Source}'.");
        }

        var branding = await _emailSenderRepository.GetActiveAsync(client.Id, request.Tenant);
        if (branding is null)
        {
            return Permanent(request, $"No active email sender for '{request.Source}', tenant '{request.Tenant}' (or '' fallback).");
        }

        var template =
            await _templateRepository.GetActiveAsync(notificationType.Id, EChannel.Email, request.Tenant, languageCode)
            ?? (request.Tenant == "" ? null : await _templateRepository.GetActiveAsync(notificationType.Id, EChannel.Email, "", languageCode));
        if (template is null && languageCode != DefaultLanguageCode)
        {
            template = await _templateRepository.GetActiveAsync(notificationType.Id, EChannel.Email, request.Tenant, DefaultLanguageCode)
                ?? (request.Tenant == "" ? null : await _templateRepository.GetActiveAsync(notificationType.Id, EChannel.Email, "", DefaultLanguageCode));
        }
        if (template is null)
        {
            return Permanent(request, $"No active template for '{request.Source}', type '{request.NotificationType}', language '{languageCode}'.");
        }

        var delivery = await _deliveryRepository.GetByMessageIdAsync(request.MessageId, EChannel.Email, cancellationToken);
        if (delivery?.Status == EDeliveryStatus.Sent)
        {
            return EmailDispatchResult.From(EmailDispatchOutcome.AlreadySent, delivery.Id);
        }
        if (delivery is null)
        {
            var created = await _deliveryRepository.CreateAsync(new Delivery
            {
                MessageId = request.MessageId,
                Source = request.Source,
                Tenant = request.Tenant,
                NotificationType = request.NotificationType,
                Channel = EChannel.Email,
                Recipient = request.ToEmail,
                Status = EDeliveryStatus.Pending
            });
            if (!created.Successful || created.Value is null)
            {
                return Permanent(request, created.Error?.Message ?? "Could not stage delivery.");
            }
            // Commit before sending so the unique index arbitrates concurrent inserts.
            if (!await _uow.TrySaveDeliveryAsync(cancellationToken))
            {
                return EmailDispatchResult.From(EmailDispatchOutcome.AlreadySent);
            }
            delivery = created.Value;
        }
        var deliveryId = delivery.Id;

        RenderedEmailTemplate rendered;
        try
        {
            rendered = await _templateRenderer.RenderAsync(
                template,
                new EmailTemplateModel
                {
                    Content = request.Content,
                    Branding = new EmailBranding
                    {
                        Tenant = request.Tenant,
                        FromAddress = branding.FromAddress,
                        DisplayName = branding.DisplayName,
                        ReplyTo = branding.ReplyTo
                    },
                    Fmt = new LocaleFormatter(languageCode),
                    Locale = languageCode
                },
                cancellationToken);
        }
        catch (Exception exception)
        {
            // Log only the exception type because render errors can expose OTPs or action links.
            _logger.LogError(
                "Template render failed ({ExceptionType}). MessageId: {MessageId}, Source: {Source}, Tenant: {Tenant}, Action: {Action}",
                exception.GetType().Name, request.MessageId, request.Source, request.Tenant, request.NotificationType);

            return await SaveStatusAsync(delivery, EDeliveryStatus.Failed, null, "Template render failed.", null, EmailDispatchOutcome.PermanentFailure);
        }

        var sendResult = await _emailSender.SendAsync(
            new OutboundEmailMessage
            {
                FromAddress = branding.FromAddress,
                FromDisplayName = branding.DisplayName,
                ReplyTo = branding.ReplyTo,
                ToEmail = request.ToEmail,
                Subject = rendered.Subject,
                HtmlBody = rendered.HtmlBody,
                CorrelationId = request.CorrelationId
            },
            cancellationToken);

        if (sendResult.Successful)
        {
            var result = await SaveStatusAsync(delivery, EDeliveryStatus.Sent, rendered.Subject, null, DateTime.UtcNow, EmailDispatchOutcome.Sent);
            if (result.Outcome != EmailDispatchOutcome.Sent)
            {
                return result;
            }

            _logger.LogInformation(
                "Delivery sent. DeliveryId: {DeliveryId}, MessageId: {MessageId}, Source: {Source}, Tenant: {Tenant}, Action: {Action}",
                deliveryId, request.MessageId, request.Source, request.Tenant, request.NotificationType);
            return result;
        }

        var outcome = sendResult.Transient
            ? EmailDispatchOutcome.TransientFailure
            : EmailDispatchOutcome.PermanentFailure;

        _logger.LogError(
            "Delivery send failed ({Outcome}). DeliveryId: {DeliveryId}, MessageId: {MessageId}, Source: {Source}, Tenant: {Tenant}, Action: {Action}",
            outcome, deliveryId, request.MessageId, request.Source, request.Tenant, request.NotificationType);

        return await SaveStatusAsync(delivery, EDeliveryStatus.Failed, rendered.Subject,
            sendResult.ErrorMessage ?? "Email send failed.", null, outcome);
    }

    private async Task<EmailDispatchResult> SaveStatusAsync(Delivery delivery, EDeliveryStatus status, string? title, string? error, DateTime? sentAt, EmailDispatchOutcome outcome)
    {
        delivery.Status = status;
        delivery.Title = title;
        delivery.Error = error;
        delivery.SentAt = sentAt;
        var response = await _deliveryRepository.UpdateAsync(delivery.Id, delivery, null, Guid.Empty);
        if (!response.Successful)
        {
            _logger.LogError("Could not update delivery status. DeliveryId: {DeliveryId}", delivery.Id);
            return EmailDispatchResult.From(EmailDispatchOutcome.PermanentFailure, delivery.Id, response.Error?.Message);
        }
        await _uow.SaveChangesAsync();
        return EmailDispatchResult.From(outcome, delivery.Id, error);
    }

    private EmailDispatchResult Permanent(SendTemplatedEmailRequest request, string reason)
    {
        _logger.LogError(
            "Delivery dispatch rejected: {Reason} MessageId: {MessageId}, Source: {Source}, Tenant: {Tenant}, Action: {Action}",
            reason, request.MessageId, request.Source, request.Tenant, request.NotificationType);
        return EmailDispatchResult.From(EmailDispatchOutcome.PermanentFailure, error: reason);
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode)
            ? DefaultLanguageCode
            : languageCode.Trim().ToLowerInvariant();
    }
}
