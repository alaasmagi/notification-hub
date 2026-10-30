using Domain;

namespace Contracts.Application;

public interface IEmailCommandProcessor
{
    Task<MessageDisposition> ProcessAsync(SendTemplatedEmailRequest request,
        CancellationToken cancellationToken = default);
}
