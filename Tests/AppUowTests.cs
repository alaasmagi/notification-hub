using DataAccess;
using DataAccess.Context;
using DTO.DataAccess;
using Microsoft.EntityFrameworkCore;
using Moq;
using Npgsql;
using Xunit;

namespace Tests;

public class AppUowTests
{
    [Fact]
    public async Task DeliveryUniqueViolation_ReturnsDuplicateAndDetachesInsert()
    {
        var context = new Mock<AppDbContext>(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql().Options) { CallBase = true };
        context.Object.Deliveries.Add(new DeliveryEntity());
        context.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(Failure(PostgresErrorCodes.UniqueViolation, "IX_Deliveries_MessageId_Channel"));
        Assert.False(await new AppUow(context.Object).TrySaveDeliveryAsync());
        Assert.Empty(context.Object.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation, "another_index")]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation, "some_fk")]
    public async Task UnrelatedDatabaseFailure_IsNotAcknowledgedAsDuplicate(string code, string constraint)
    {
        var context = new Mock<AppDbContext>(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql().Options) { CallBase = true };
        context.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(Failure(code, constraint));
        await Assert.ThrowsAsync<DbUpdateException>(() => new AppUow(context.Object).TrySaveDeliveryAsync());
    }

    [Fact]
    public async Task SuccessfulPendingSave_DetachesBeforeBaseRepositoryUpdate()
    {
        var context = new Mock<AppDbContext>(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql().Options) { CallBase = true };
        context.Object.Deliveries.Add(new DeliveryEntity());
        context.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        Assert.True(await new AppUow(context.Object).TrySaveDeliveryAsync());
        Assert.Empty(context.Object.ChangeTracker.Entries());
    }

    private DbUpdateException Failure(string code, string constraint) =>
        new("save failed", new PostgresException("failure", "ERROR", "ERROR", code, constraintName: constraint));
}
