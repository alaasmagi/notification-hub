namespace Contracts.Application;

/// <summary>Processes email command deliveries and returns their disposition.</summary>
public interface IEmailCommandProcessor
{
    Task<MessageDisposition> ProcessAsync(
        string routingKey,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default);
}
