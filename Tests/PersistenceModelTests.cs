using DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests;

public class PersistenceModelTests
{
    [Fact]
    public void Model_MatchesMigrationSnapshot()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql()
            .Options;
        using var context = new AppDbContext(options);

        Assert.False(context.Database.HasPendingModelChanges());
    }
}
