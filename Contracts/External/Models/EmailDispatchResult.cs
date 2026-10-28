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
    public Guid? EmailId { get; init; }
    public string? Error { get; init; }

    public static EmailDispatchResult From(EmailDispatchOutcome outcome, Guid? emailId = null, string? error = null)
        => new() { Outcome = outcome, EmailId = emailId, Error = error };
}
