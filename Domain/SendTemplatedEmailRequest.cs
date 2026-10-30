namespace Domain;

public record SendTemplatedEmailRequest
{
    public string MessageId { get; set; } = default!;

    public string Source { get; set; } = default!;

    public string Tenant { get; set; } = default!;

    public string NotificationType { get; set; } = default!;

    public string ToEmail { get; set; } = default!;
    public string LanguageCode { get; set; } = "en";
    public string? CorrelationId { get; set; }

    public TemplateContent Content { get; set; } = new();
    public DateTimeOffset? ExpiresAt { get; set; }

    public bool IsValid() => !string.IsNullOrWhiteSpace(MessageId)
        && !string.IsNullOrWhiteSpace(Source)
        && !string.IsNullOrWhiteSpace(Tenant)
        && !string.IsNullOrWhiteSpace(NotificationType)
        && !string.IsNullOrWhiteSpace(ToEmail);

    public bool HasExpired(DateTimeOffset now) => ExpiresAt <= now;
}
