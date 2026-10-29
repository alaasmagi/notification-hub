using DataAccess.Context;
using DTO.DataAccess;
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
    [Fact]
    public void Model_UsesNotificationSchemaAndRestrictsConfigurationDeletion()
    {
        using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql().Options);
        foreach (var entity in context.Model.GetEntityTypes())
            Assert.Equal("notification", entity.GetSchema());
        foreach (var entity in new[] { typeof(TemplateEntity), typeof(NotificationTypeEntity), typeof(EmailSenderEntity) })
            Assert.Equal(DeleteBehavior.Restrict, Assert.Single(context.Model.FindEntityType(entity)!.GetForeignKeys()).DeleteBehavior);
        Assert.Empty(context.Model.FindEntityType(typeof(DeliveryEntity))!.GetForeignKeys());
    }

    [Fact]
    public void Model_EnforcesChannelScopedIdempotencyAndConfigurationKeys()
    {
        using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql().Options);
        var keys = new Dictionary<Type, string[]>
        {
            [typeof(ClientEntity)] = ["Source"],
            [typeof(NotificationTypeEntity)] = ["ClientId", "Key"],
            [typeof(EmailSenderEntity)] = ["ClientId", "Tenant"],
            [typeof(TemplateEntity)] = ["NotificationTypeId", "Channel", "Tenant", "LanguageCode"],
            [typeof(DeliveryEntity)] = ["MessageId", "Channel"]
        };
        foreach (var (type, properties) in keys)
        {
            var index = Assert.Single(context.Model.FindEntityType(type)!.GetIndexes(), x => x.IsUnique);
            Assert.Equal(properties, index.Properties.Select(x => x.Name));
        }
    }
}
