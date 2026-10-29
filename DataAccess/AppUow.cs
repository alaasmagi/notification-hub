using Base.DataAccess.EF;
using Contracts.DataAccess;
using DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DataAccess;

public class AppUow : BaseUow<AppDbContext>, IAppUow
{
    private readonly AppDbContext _context;

    public AppUow(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<bool> TrySaveDeliveryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            _context.ChangeTracker.Clear();
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
               { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Deliveries_MessageId_Channel" })
        {
            _context.ChangeTracker.Clear();
            return false;
        }
    }
}
