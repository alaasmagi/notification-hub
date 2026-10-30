using Application;
using Base.DTO;
using Contracts.DataAccess;
using Contracts.External;
using Contracts.External.Models;
using Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Tests;

public class EmailDispatchServiceTests
{
    private readonly Client _client = new() { Id = Guid.NewGuid(), Source = "identity-hub", DisplayName = "Identity" };
    private readonly NotificationType _type = new() { Id = Guid.NewGuid(), Key = "user-verify" };
    private readonly Mock<IClientRepository> _clients = new();
    private readonly Mock<INotificationTypeRepository> _types = new();
    private readonly Mock<IEmailSenderRepository> _senders = new();
    private readonly Mock<ITemplateRepository> _templates = new();
    private readonly Mock<IDeliveryRepository> _deliveries = new();
    private readonly Mock<IAppUow> _uow = new();
    private readonly Mock<IEmailTemplateRenderer> _renderer = new();
    private readonly Mock<IEmailSender> _sender = new();
    private readonly List<(string Tenant, string Language)> _lookups = [];
    private readonly Dictionary<(string Tenant, string Language), Template> _available = [];
    private Delivery? _saved;
    private Template? _rendered;
    private string? _locale;

    public EmailDispatchServiceTests()
    {
        _clients.Setup(x => x.GetActiveBySourceAsync("identity-hub")).ReturnsAsync(_client);
        _types.Setup(x => x.GetActiveAsync(_client.Id, "user-verify")).ReturnsAsync(_type);
        _senders.Setup(x => x.GetActiveAsync(_client.Id, It.IsAny<string>())).ReturnsAsync(new EmailSender
        {
            Id = Guid.NewGuid(), ClientId = _client.Id, Tenant = "", FromAddress = "sender@example.com", DisplayName = "Sender"
        });
        _templates.Setup(x => x.GetActiveAsync(_type.Id, EChannel.Email, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Guid _, EChannel _, string tenant, string language) =>
            {
                _lookups.Add((tenant, language));
                return _available.GetValueOrDefault((tenant, language));
            });
        _deliveries.Setup(x => x.CreateAsync(It.IsAny<Delivery>(), It.IsAny<Guid>()))
            .ReturnsAsync((Delivery delivery, Guid _) =>
            {
                delivery.Id = Guid.NewGuid();
                _saved = delivery;
                return MethodResponse<Delivery>.Success(delivery);
            });
        _deliveries.Setup(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Delivery>(), null, Guid.Empty))
            .ReturnsAsync((Guid _, Delivery delivery, string? _, Guid _) =>
            {
                _saved = delivery;
                return MethodResponse<Delivery>.Success(delivery);
            });
        _uow.Setup(x => x.TrySaveDeliveryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _renderer.Setup(x => x.RenderAsync(It.IsAny<Template>(), It.IsAny<EmailTemplateModel>(), It.IsAny<CancellationToken>()))
            .Callback<Template, EmailTemplateModel, CancellationToken>((template, model, _) =>
            {
                _rendered = template;
                _locale = model.Locale;
            })
            .ReturnsAsync(new RenderedEmailTemplate { Subject = "Rendered title", HtmlBody = "<p>Body</p>" });
        _sender.Setup(x => x.SendAsync(It.IsAny<OutboundEmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmailSendResult.Success());
    }

    [Theory]
    [InlineData("password-reset")]
    [InlineData("monthly-invoice")]
    [InlineData("arbitrary-database-command")]
    public async Task Command_IsResolvedByDatabaseKey(string command)
    {
        var request = Request();
        request.NotificationType = command;
        _types.Setup(x => x.GetActiveAsync(_client.Id, command)).ReturnsAsync(_type);
        _available[("invoice-service", "et")] = new Template();

        var result = await Service().SendTemplatedEmailAsync(request);

        Assert.Equal(EmailDispatchOutcome.Sent, result.Outcome);
        _types.Verify(x => x.GetActiveAsync(_client.Id, command), Times.Once);
        Assert.Equal(command, _saved!.NotificationType);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Templates_PreferRequestedLanguageBeforeTenantFallback(int firstAvailable)
    {
        (string, string)[] order = [("invoice-service", "et"), ("", "et"), ("invoice-service", "en"), ("", "en")];
        for (var i = firstAvailable; i < order.Length; i++)
            _available[order[i]] = new Template { Id = Guid.NewGuid(), Title = i.ToString(), Body = "Body" };

        var result = await Service().SendTemplatedEmailAsync(Request());

        Assert.Equal(EmailDispatchOutcome.Sent, result.Outcome);
        Assert.Equal(order.Take(firstAvailable + 1), _lookups);
        Assert.Same(_available[order[firstAvailable]], _rendered);
        Assert.Equal("et", _locale);
        _clients.Verify(x => x.GetActiveBySourceAsync("identity-hub"), Times.Once);
        _clients.VerifyNoOtherCalls();
        _senders.Verify(x => x.GetActiveAsync(_client.Id, "invoice-service"), Times.Once);
        Assert.Equal("identity-hub", _saved!.Source);
        Assert.Equal("invoice-service", _saved.Tenant);
        Assert.Equal("user-verify", _saved.NotificationType);
        Assert.Equal(EChannel.Email, _saved.Channel);
        Assert.Equal("user@example.com", _saved.Recipient);
        Assert.Equal("Rendered title", _saved.Title);
        Assert.Equal(EDeliveryStatus.Sent, _saved.Status);
        Assert.NotNull(_saved.SentAt);
    }

    [Theory]
    [InlineData("", "en")]
    [InlineData("  ", "en")]
    [InlineData(" ET ", "et")]
    public async Task Language_IsNormalized_AndSharedTenantIsNotQueriedTwice(string language, string expected)
    {
        var request = Request();
        request.Tenant = "";
        request.LanguageCode = language;
        _available[("", expected)] = new Template();
        await Service().SendTemplatedEmailAsync(request);
        Assert.Equal(new[] { ("", expected) }, _lookups);
    }

    [Fact]
    public async Task MissingTemplate_RejectsWithoutSending()
    {
        var result = await Service().SendTemplatedEmailAsync(Request());
        Assert.Equal(EmailDispatchOutcome.PermanentFailure, result.Outcome);
        Assert.Equal(4, _lookups.Count);
        Assert.Null(_saved);
        _sender.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("client")]
    [InlineData("type")]
    [InlineData("sender")]
    public async Task MissingActiveConfiguration_RejectsWithoutSending(string missing)
    {
        if (missing == "client") _clients.Setup(x => x.GetActiveBySourceAsync("identity-hub")).ReturnsAsync((Client?)null);
        if (missing == "type") _types.Setup(x => x.GetActiveAsync(_client.Id, "user-verify")).ReturnsAsync((NotificationType?)null);
        if (missing == "sender") _senders.Setup(x => x.GetActiveAsync(_client.Id, "invoice-service")).ReturnsAsync((EmailSender?)null);
        var result = await Service().SendTemplatedEmailAsync(Request());
        Assert.Equal(EmailDispatchOutcome.PermanentFailure, result.Outcome);
        _sender.VerifyNoOtherCalls();
        Assert.Null(_saved);
    }

    [Fact]
    public async Task SentEmail_IsDeduplicatedUsingMessageAndChannel()
    {
        _available[("", "en")] = new Template();
        var request = Request();
        _deliveries.Setup(x => x.GetByMessageIdAsync(request.MessageId, EChannel.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Delivery { Id = Guid.NewGuid(), Status = EDeliveryStatus.Sent });
        var result = await Service().SendTemplatedEmailAsync(request);
        Assert.Equal(EmailDispatchOutcome.AlreadySent, result.Outcome);
        _deliveries.Verify(x => x.GetByMessageIdAsync(request.MessageId, EChannel.Email, It.IsAny<CancellationToken>()), Times.Once);
        _sender.VerifyNoOtherCalls();
        Assert.Null(_saved);
    }

    [Fact]
    public async Task InsertRace_DoesNotSend()
    {
        _available[("", "en")] = new Template();
        _uow.Setup(x => x.TrySaveDeliveryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Assert.Equal(EmailDispatchOutcome.AlreadySent, (await Service().SendTemplatedEmailAsync(Request())).Outcome);
        _sender.VerifyNoOtherCalls();
        Assert.Equal(EDeliveryStatus.Pending, _saved!.Status);
    }

    [Theory]
    [InlineData(false, EmailDispatchOutcome.PermanentFailure)]
    [InlineData(true, EmailDispatchOutcome.TransientFailure)]
    public async Task ProviderFailure_StoresErrorAndRenderedTitle(bool transient, EmailDispatchOutcome expected)
    {
        _available[("", "en")] = new Template();
        _sender.Setup(x => x.SendAsync(It.IsAny<OutboundEmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmailSendResult.Failure("Provider unavailable", transient));
        var result = await Service().SendTemplatedEmailAsync(Request());
        Assert.Equal(expected, result.Outcome);
        Assert.Equal(EDeliveryStatus.Failed, _saved!.Status);
        Assert.Equal("Provider unavailable", _saved.Error);
        Assert.Equal("Rendered title", _saved.Title);
        Assert.Null(_saved.SentAt);
    }

    [Fact]
    public async Task RenderFailure_StoresSafeError()
    {
        _available[("", "en")] = new Template();
        _renderer.Setup(x => x.RenderAsync(It.IsAny<Template>(), It.IsAny<EmailTemplateModel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("secret OTP"));
        var result = await Service().SendTemplatedEmailAsync(Request());
        Assert.Equal(EmailDispatchOutcome.PermanentFailure, result.Outcome);
        Assert.Equal("Template render failed.", _saved!.Error);
        Assert.Equal(EDeliveryStatus.Failed, _saved.Status);
        _sender.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(EDeliveryStatus.Pending)]
    [InlineData(EDeliveryStatus.Failed)]
    public async Task Retry_ReusesDeliveryAndClearsPreviousError(EDeliveryStatus status)
    {
        _available[("", "en")] = new Template();
        var request = Request();
        var delivery = new Delivery { Id = Guid.NewGuid(), MessageId = request.MessageId, Channel = EChannel.Email, Status = status, Error = "Old error" };
        _deliveries.Setup(x => x.GetByMessageIdAsync(request.MessageId, EChannel.Email, It.IsAny<CancellationToken>())).ReturnsAsync(delivery);
        var result = await Service().SendTemplatedEmailAsync(request);
        Assert.Equal(delivery.Id, result.DeliveryId);
        Assert.Equal(EmailDispatchOutcome.Sent, result.Outcome);
        Assert.Null(_saved!.Error);
        _deliveries.Verify(x => x.CreateAsync(It.IsAny<Delivery>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task FailedStatusUpdate_DoesNotReportSuccessfulDelivery()
    {
        _available[("", "en")] = new Template();
        _deliveries.Setup(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Delivery>(), null, Guid.Empty))
            .ReturnsAsync(MethodResponse<Delivery>.Failure(new Error("missing", "Delivery was removed.")));
        var result = await Service().SendTemplatedEmailAsync(Request());
        Assert.Equal(EmailDispatchOutcome.PermanentFailure, result.Outcome);
        Assert.Equal("Delivery was removed.", result.Error);
    }

    private EmailDispatchService Service() => new(_clients.Object, _types.Object, _uow.Object, _senders.Object,
        _templates.Object, _deliveries.Object, _renderer.Object, _sender.Object, NullLogger<EmailDispatchService>.Instance);

    private SendTemplatedEmailRequest Request() => new()
    {
        MessageId = "message-1", Source = "identity-hub", Tenant = "invoice-service", NotificationType = "user-verify",
        ToEmail = "user@example.com", LanguageCode = "et", Content = new TemplateContent()
    };
}
