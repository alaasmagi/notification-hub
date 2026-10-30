using Contracts.Application;
using Domain;
using Microsoft.Extensions.Logging;

namespace Application;

public class EmailCommandProcessor : IEmailCommandProcessor
{
    private readonly IEmailDispatchService _dispatchService;
    private readonly ILogger<EmailCommandProcessor> _logger;

    public EmailCommandProcessor(IEmailDispatchService dispatchService, ILogger<EmailCommandProcessor> logger)
    {
        _dispatchService = dispatchService;
        _logger = logger;
    }

    public async Task<MessageDisposition> ProcessAsync(SendTemplatedEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.IsValid())
        {
            _logger.LogWarning("Rejecting invalid email command. MessageId: {MessageId}", request.MessageId);
            return MessageDisposition.RejectNoRequeue;
        }

        if (request.HasExpired(DateTimeOffset.UtcNow))
        {
            _logger.LogInformation("Dropping expired email command. MessageId: {MessageId}", request.MessageId);
            return MessageDisposition.Ack;
        }

        var result = await _dispatchService.SendTemplatedEmailAsync(request, cancellationToken);
        return result.Outcome switch
        {
            EmailDispatchOutcome.Sent or EmailDispatchOutcome.AlreadySent or EmailDispatchOutcome.Expired => MessageDisposition.Ack,
            EmailDispatchOutcome.TransientFailure => MessageDisposition.RequeueTransient,
            _ => MessageDisposition.RejectNoRequeue
        };
    }
}
