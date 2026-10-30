using System.Dynamic;
using System.Globalization;
using System.Text.Json;
using Base.Contracts.Message;
using Base.Message;
using Contracts.Application;
using Contracts.External.Models;
using Microsoft.Extensions.Logging;

namespace Application;

/// <summary>Validates email commands and determines their delivery disposition.</summary>
public class EmailCommandProcessor : IEmailCommandProcessor
{
    // A consumer written for major "1" must accept "1.0", "1.1", … and ignore unknown fields.
    private const string SupportedContentMajor = "1";

    private static readonly JsonSerializerOptions EnvelopeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IEmailDispatchService _dispatchService;
    private readonly ILogger<EmailCommandProcessor> _logger;

    public EmailCommandProcessor(
        IEmailDispatchService dispatchService,
        ILogger<EmailCommandProcessor> logger)
    {
        _dispatchService = dispatchService;
        _logger = logger;
    }

    public async Task<MessageDisposition> ProcessAsync(
        string routingKey,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default)
    {
        // Malformed envelopes cannot succeed on retry.
        BaseEventEnvelope<JsonElement>? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<BaseEventEnvelope<JsonElement>>(body.Span, EnvelopeOptions);
        }
        catch (JsonException)
        {
            _logger.LogWarning("Rejecting message: envelope could not be parsed. RoutingKey: {RoutingKey}", routingKey);
            return MessageDisposition.RejectNoRequeue;
        }

        if (envelope is null || string.IsNullOrWhiteSpace(envelope.Id))
        {
            _logger.LogWarning("Rejecting message: missing envelope id. RoutingKey: {RoutingKey}", routingKey);
            return MessageDisposition.RejectNoRequeue;
        }

        // The routing key is {source}.{tenant}.{action}; each segment is dot-free, so exactly 3 parts.
        var segments = routingKey.Split('.');
        if (segments.Length != 3)
        {
            _logger.LogWarning(
                "Rejecting message: malformed routing key. RoutingKey: {RoutingKey}, Id: {Id}", routingKey, envelope.Id);
            return MessageDisposition.RejectNoRequeue;
        }

        var keySource = segments[0];
        var keyTenant = segments[1];
        var keyAction = segments[2];

        // The broker authenticates the routing key, not the payload source.
        if (!string.Equals(keySource, envelope.Source, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Rejecting message: routing-key source '{KeySource}' does not match payload source '{PayloadSource}'. Id: {Id}",
                keySource, envelope.Source, envelope.Id);
            return MessageDisposition.RejectNoRequeue;
        }

        if (string.IsNullOrWhiteSpace(keyTenant))
        {
            _logger.LogWarning("Rejecting message: tenant is missing. Id: {Id}, Source: {Source}", envelope.Id, keySource);
            return MessageDisposition.RejectNoRequeue;
        }

        if (!IsContentVersionSupported(envelope.ContentVersion))
        {
            _logger.LogWarning(
                "Rejecting message: unsupported contentVersion '{ContentVersion}'. Id: {Id}, Source: {Source}, Action: {Action}",
                envelope.ContentVersion, envelope.Id, keySource, keyAction);
            return MessageDisposition.RejectNoRequeue;
        }

        if (envelope.Content.ValueKind != JsonValueKind.Object)
        {
            _logger.LogWarning(
                "Rejecting message: content must be a JSON object. Id: {Id}, Source: {Source}, Action: {Action}",
                envelope.Id, keySource, keyAction);
            return MessageDisposition.RejectNoRequeue;
        }

        if (!TryGetString(envelope.Content, "email", out var recipient))
        {
            _logger.LogWarning(
                "Rejecting message: no recipient email. Id: {Id}, Source: {Source}, Action: {Action}",
                envelope.Id, keySource, keyAction);
            return MessageDisposition.RejectNoRequeue;
        }

        TryGetString(envelope.Content, "locale", out var locale);

        // Acknowledge expired commands so invalid codes are never sent.
        if (HasExpired(envelope.Content))
        {
            _logger.LogInformation(
                "Dropping expired message without sending. Id: {Id}, Source: {Source}, Tenant: {Tenant}, Action: {Action}",
                envelope.Id, keySource, keyTenant, keyAction);
            return MessageDisposition.Ack;
        }

        var result = await _dispatchService.SendTemplatedEmailAsync(
            new SendTemplatedEmailRequest
            {
                MessageId = envelope.Id,
                Source = keySource,
                Tenant = keyTenant,
                NotificationType = keyAction,
                ToEmail = recipient,
                LanguageCode = string.IsNullOrWhiteSpace(locale) ? "en" : locale,
                CorrelationId = envelope.Id,
                Content = ConvertContent(envelope.Content)!
            },
            cancellationToken);

        return result.Outcome switch
        {
            EmailDispatchOutcome.Sent => MessageDisposition.Ack,
            EmailDispatchOutcome.AlreadySent => MessageDisposition.Ack,
            EmailDispatchOutcome.Expired => MessageDisposition.Ack,
            EmailDispatchOutcome.TransientFailure => MessageDisposition.RequeueTransient,
            _ => MessageDisposition.RejectNoRequeue
        };
    }

    private static bool IsContentVersionSupported(string? contentVersion)
    {
        if (string.IsNullOrWhiteSpace(contentVersion))
        {
            return false;
        }

        var major = contentVersion.Split('.', 2)[0];
        return string.Equals(major, SupportedContentMajor, StringComparison.Ordinal);
    }

    private static object? ConvertContent(JsonElement content)
    {
        switch (content.ValueKind)
        {
            case JsonValueKind.Object:
                IDictionary<string, object?> model = new ExpandoObject();
                foreach (var property in content.EnumerateObject())
                {
                    model[property.Name] = ConvertContent(property.Value);
                }
                return model;
            case JsonValueKind.Array:
                return content.EnumerateArray().Select(ConvertContent).ToList();
            case JsonValueKind.String:
                return content.GetString();
            case JsonValueKind.Number:
                if (content.TryGetInt64(out var integer)) return integer;
                if (content.TryGetDecimal(out var number)) return number;
                return content.GetDouble();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            default:
                return null;
        }
    }

    private static bool HasExpired(JsonElement content)
    {
        if (content.ValueKind == JsonValueKind.Object &&
            content.TryGetProperty("expiresAt", out var element) &&
            element.ValueKind == JsonValueKind.String &&
            DateTimeOffset.TryParse(element.GetString(), CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var expiresAt))
        {
            return expiresAt <= DateTimeOffset.UtcNow;
        }

        return false;
    }

    private static bool TryGetString(JsonElement content, string propertyName, out string value)
    {
        value = string.Empty;
        if (content.ValueKind == JsonValueKind.Object &&
            content.TryGetProperty(propertyName, out var element) &&
            element.ValueKind == JsonValueKind.String)
        {
            var text = element.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                value = text;
                return true;
            }
        }

        return false;
    }
}
