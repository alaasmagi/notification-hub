using Base.Contracts.DataAccess;
using Domain;

namespace Contracts.DataAccess;

public interface IEmailSenderRepository : IBaseRepository<EmailSender>
{
    Task<IReadOnlyList<EmailSender>> GetAllForAdminAsync();
    Task<EmailSender?> GetForAdminAsync(Guid id);
    Task<EmailSender?> GetActiveAsync(Guid clientId, string tenant);
}
