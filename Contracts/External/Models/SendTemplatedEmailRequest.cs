namespace Contracts.External.Models;

public class SendTemplatedEmailRequest
{
    public string MessageId { get; set; } = default!;

    public string Source { get; set; } = default!;

    public string Tenant { get; set; } = default!;

    public string NotificationType { get; set; } = default!;

    public string ToEmail { get; set; } = default!;
    public string LanguageCode { get; set; } = "en";
    public string? CorrelationId { get; set; }

    public object Content { get; set; } = default!;
}