using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface INotificationTypeRepository : IBaseRepository<NotificationType>
{
    Task<IReadOnlyList<NotificationType>> GetAllForAdminAsync();
    Task<NotificationType?> GetForAdminAsync(Guid id);
    Task<NotificationType?> GetActiveAsync(Guid clientId, string key);
}
