using Base.Domain;

namespace Domain;

public class SenderIdentity : BaseEntity
{
    public Guid ClientId { get; set; }
    public string EmailType { get; set; } = default!;

    // Empty tenant selects the shared fallback branding.
    public string Tenant { get; set; } = string.Empty;
    public string FromAddress { get; set; } = default!; 
    public string DisplayName { get; set; } = default!;
    public string? ReplyTo { get; set; }
    public bool IsActive { get; set; } = true;
}