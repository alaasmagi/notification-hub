namespace Contracts.External.Models;

/// <summary>Carries the sender identity exposed to email templates.</summary>
public class EmailBranding
{
    public string Tenant { get; init; } = default!;
    public string FromAddress { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public string? ReplyTo { get; init; }
}
