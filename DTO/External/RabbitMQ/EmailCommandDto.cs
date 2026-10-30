using System.Text.Json;
using System.Text.Json.Serialization;

namespace DTO.External.RabbitMQ;

public record EmailCommandDto
{
    [JsonIgnore]
    public string RoutingKey { get; init; } = "";
    [JsonPropertyName("messageId")]
    public string MessageId { get; init; } = "";
    [JsonPropertyName("source")]
    public string Source { get; init; } = "";
    [JsonPropertyName("tenant")]
    public string Tenant { get; init; } = "";
    [JsonPropertyName("action")]
    public string Action { get; init; } = "";
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; }
    [JsonPropertyName("contentVersion")]
    public string ContentVersion { get; init; } = "";
    [JsonPropertyName("content")]
    public EmailContentDto? Content { get; init; }
}

public record EmailContentDto
{
    [JsonPropertyName("to")]
    public string To { get; init; } = "";
    [JsonPropertyName("languageCode")]
    public string? LanguageCode { get; init; }
    [JsonPropertyName("model")]
    public JsonElement Model { get; init; }
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; init; }
}
