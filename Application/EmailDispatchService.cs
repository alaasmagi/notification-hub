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
    private readonly ISenderIdentityRepository _senderIdentityRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly IEmailRepository _emailRepository;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailDispatchService> _logger;

    private const string DefaultLanguageCode = "en";

    public EmailDispatchService(
        IClientRepository clientRepository,
        ISenderIdentityRepository senderIdentityRepository,
        ITemplateRepository templateRepository,
        IEmailRepository emailRepository,
        IEmailTemplateRenderer templateRenderer,
        IEmailSender emailSender,
        ILogger<EmailDispatchService> logger)
    {
        _clientRepository = clientRepository;
        _senderIdentityRepository = senderIdentityRepository;
        _templateRepository = templateRepository;
        _emailRepository = emailRepository;
        _templateRenderer = templateRenderer;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<EmailDispatchResult> SendTemplatedEmailAsync(
        SendTemplatedEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var languageCode = NormalizeLanguageCode(request.LanguageCode);

        var clientServiceName = request.ClientServiceName;
        var client = await _clientRepository.GetActiveByServiceNameAsync(clientServiceName);
        if (client is null)
        {
            return Permanent(request, $"No active client for '{clientServiceName}'.");
        }

        var branding = await _senderIdentityRepository.GetActiveAsync(
            client.Id, request.EmailType, request.Tenant);
        if (branding is null)
        {
            return Permanent(request,
                $"No active sender identity for client '{clientServiceName}', action '{request.EmailType}', tenant '{request.Tenant}' (or '' fallback).");
        }

        var template =
            await _templateRepository.GetActiveAsync(branding.Id, languageCode)
            ?? await _templateRepository.GetActiveAsync(branding.Id, DefaultLanguageCode);
        if (template is null)
        {
            return Permanent(request,
                $"No active template for client '{clientServiceName}', action '{request.EmailType}', language '{languageCode}'.");
        }

        // Commit the pending row before sending to guard against concurrent duplicates.
        var existing = await _emailRepository.GetByMessageIdAsync(request.MessageId, cancellationToken);
        Guid emailId;
        if (existing is not null)
        {
            if (existing.Status == EEmailStatus.Sent)
            {
                _logger.LogInformation(
                    "Duplicate delivery ignored; already sent. MessageId: {MessageId}, Source: {Source}, Tenant: {Tenant}, Action: {Action}",
                    request.MessageId, request.Source, request.Tenant, request.EmailType);
                return EmailDispatchResult.From(EmailDispatchOutcome.AlreadySent, existing.Id);
            }

            // A prior attempt failed transiently and was requeued — this is the retry, not a duplicate.
            emailId = existing.Id;
        }
        else
        {
            var created = await _emailRepository.TryCreatePendingAsync(
                new Email
                {
                    MessageId = request.MessageId,
                    ServiceName = request.Source,
                    Tenant = request.Tenant,
                    EmailType = request.EmailType,
                    ToRecipients = request.ToEmail,
                    ReplyTo = branding.ReplyTo,
                    Status = EEmailStatus.Pending
                },
                cancellationToken);

            if (created is null)
            {
                // Lost the insert race to a concurrent duplicate delivery — it owns the send.
                _logger.LogInformation(
                    "Duplicate delivery ignored on insert race. MessageId: {MessageId}, Source: {Source}, Tenant: {Tenant}, Action: {Action}",
                    request.MessageId, request.Source, request.Tenant, request.EmailType);
                return EmailDispatchResult.From(EmailDispatchOutcome.AlreadySent);
            }

            emailId = created.Id;
        }

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
                exception.GetType().Name, request.MessageId, request.Source, request.Tenant, request.EmailType);

            await _emailRepository.UpdateDeliveryStatusAsync(
                emailId, EEmailStatus.Failed, null, null, cancellationToken);
            return EmailDispatchResult.From(EmailDispatchOutcome.PermanentFailure, emailId, "Template render failed.");
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
            await _emailRepository.UpdateDeliveryStatusAsync(
                emailId, EEmailStatus.Sent, rendered.Subject, DateTime.UtcNow, cancellationToken);

            _logger.LogInformation(
                "Email sent. EmailId: {EmailId}, MessageId: {MessageId}, Source: {Source}, Tenant: {Tenant}, Action: {Action}",
                emailId, request.MessageId, request.Source, request.Tenant, request.EmailType);
            return EmailDispatchResult.From(EmailDispatchOutcome.Sent, emailId);
        }

        await _emailRepository.UpdateDeliveryStatusAsync(
            emailId, EEmailStatus.Failed, rendered.Subject, null, cancellationToken);

        var outcome = sendResult.Transient
            ? EmailDispatchOutcome.TransientFailure
            : EmailDispatchOutcome.PermanentFailure;

        _logger.LogError(
            "Email send failed ({Outcome}). EmailId: {EmailId}, MessageId: {MessageId}, Source: {Source}, Tenant: {Tenant}, Action: {Action}",
            outcome, emailId, request.MessageId, request.Source, request.Tenant, request.EmailType);

        return EmailDispatchResult.From(outcome, emailId, sendResult.ErrorMessage);
    }

    private EmailDispatchResult Permanent(SendTemplatedEmailRequest request, string reason)
    {
        _logger.LogError(
            "Email dispatch rejected: {Reason} MessageId: {MessageId}, Source: {Source}, Tenant: {Tenant}, Action: {Action}",
            reason, request.MessageId, request.Source, request.Tenant, request.EmailType);
        return EmailDispatchResult.From(EmailDispatchOutcome.PermanentFailure, error: reason);
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode)
            ? DefaultLanguageCode
            : languageCode.Trim().ToLowerInvariant();
    }
}
