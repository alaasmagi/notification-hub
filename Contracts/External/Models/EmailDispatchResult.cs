namespace Contracts.External.Models;

public enum EmailDispatchOutcome
{
    Sent,

    AlreadySent,

    Expired,

    TransientFailure,

    PermanentFailure
}

public class EmailDispatchResult
{
    public required EmailDispatchOutcome Outcome { get; init; }
    public Guid? DeliveryId { get; init; }
    public string? Error { get; init; }

    public static EmailDispatchResult From(EmailDispatchOutcome outcome, Guid? deliveryId = null, string? error = null)
        => new() { Outcome = outcome, DeliveryId = deliveryId, Error = error };
}
