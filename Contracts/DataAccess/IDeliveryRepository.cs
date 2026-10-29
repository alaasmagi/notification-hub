using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface IDeliveryRepository : IBaseRepository<Delivery>
{
    Task<IReadOnlyList<Delivery>> GetAllForAdminAsync();
    Task<Delivery?> GetForAdminAsync(Guid id);
    Task<Delivery?> GetByMessageIdAsync(string messageId, EChannel channel, CancellationToken cancellationToken = default);
}
