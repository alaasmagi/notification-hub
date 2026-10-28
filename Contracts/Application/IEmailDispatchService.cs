using Contracts.External.Models;

namespace Contracts.Application;

public interface IEmailDispatchService
{
    Task<EmailDispatchResult> SendTemplatedEmailAsync(SendTemplatedEmailRequest request,
        CancellationToken cancellationToken = default);
}
