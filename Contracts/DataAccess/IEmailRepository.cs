using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface IEmailRepository : IBaseRepository<Email>
{
    Task<IReadOnlyList<Email>> GetAllForAdminAsync();
    Task<Email?> GetForAdminAsync(Guid id);

    Task<Email?> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken = default);

    Task<Email?> TryCreatePendingAsync(Email email, CancellationToken cancellationToken = default);

    Task<int> UpdateDeliveryStatusAsync(
        Guid id,
        EEmailStatus status,
        string? subject,
        DateTime? sentAt,
        CancellationToken cancellationToken = default);
}
