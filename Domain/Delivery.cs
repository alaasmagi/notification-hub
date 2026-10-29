using Base.Domain;

namespace Domain;

public class Delivery : BaseEntity
{
    public string MessageId { get; set; } = default!;
    public string Source { get; set; } = default!;
    public string Tenant { get; set; } = string.Empty;
    public string NotificationType { get; set; } = default!;
    public EChannel Channel { get; set; }
    public string Recipient { get; set; } = default!;
    public string? Title { get; set; }
    public EDeliveryStatus Status { get; set; }
    public string? Error { get; set; }
    public DateTime? SentAt { get; set; }
}
