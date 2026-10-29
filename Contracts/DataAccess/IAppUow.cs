using Base.Contracts.DataAccess;

namespace Contracts.DataAccess;

public interface IAppUow : IBaseUow
{
    Task<bool> TrySaveDeliveryAsync(CancellationToken cancellationToken = default);
}
