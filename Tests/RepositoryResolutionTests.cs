using DataAccess;
using DataAccess.Context;
using Domain;
using DTO.DataAccess;
using DTO.DataAccess.Mappers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests;

public class RepositoryResolutionTests
{
    [Theory]
    [InlineData(true, "tenant")]
    [InlineData(false, "")]
    public async Task Sender_PrefersActiveTenantOtherwiseShared(bool active, string expectedTenant)
    {
        using var context = Context();
        var clientId = Guid.NewGuid();
        context.EmailSenders = TestSet.Create(
            new EmailSenderEntity { Id = Guid.NewGuid(), ClientId = clientId, Tenant = "", IsActive = true },
            new EmailSenderEntity { Id = Guid.NewGuid(), ClientId = clientId, Tenant = "tenant", IsActive = active },
            new EmailSenderEntity { Id = Guid.NewGuid(), ClientId = Guid.NewGuid(), Tenant = "tenant", IsActive = true });
        var sender = await new EmailSenderRepository(context, new EmailSenderEntityMapper()).GetActiveAsync(clientId, "tenant");
        Assert.Equal(expectedTenant, sender!.Tenant);
        Assert.Equal(clientId, sender.ClientId);
    }

    [Fact]
    public async Task Sender_DoesNotUseInactiveOrOtherTenant()
    {
        using var context = Context();
        var id = Guid.NewGuid();
        context.EmailSenders = TestSet.Create(
            new EmailSenderEntity { ClientId = id, Tenant = "", IsActive = false },
            new EmailSenderEntity { ClientId = id, Tenant = "other", IsActive = true });
        Assert.Null(await new EmailSenderRepository(context, new EmailSenderEntityMapper()).GetActiveAsync(id, "tenant"));
    }

    [Fact]
    public async Task Template_FiltersEveryDimensionAndActiveFlag()
    {
        using var context = Context();
        var id = Guid.NewGuid();
        var expected = new TemplateEntity { Id = Guid.NewGuid(), NotificationTypeId = id, Channel = EChannel.Email, Tenant = "tenant", LanguageCode = "et" };
        context.Templates = TestSet.Create(
            new TemplateEntity { NotificationTypeId = id, Tenant = "tenant", LanguageCode = "et", IsActive = false },
            new TemplateEntity { NotificationTypeId = id, Tenant = "tenant", LanguageCode = "en" },
            new TemplateEntity { NotificationTypeId = id, Tenant = "", LanguageCode = "et" },
            new TemplateEntity { NotificationTypeId = id, Tenant = "tenant", LanguageCode = "et", Channel = (EChannel)1 },
            new TemplateEntity { NotificationTypeId = Guid.NewGuid(), Tenant = "tenant", LanguageCode = "et" }, expected);
        var template = await new TemplateRepository(context, new TemplateEntityMapper()).GetActiveAsync(id, EChannel.Email, "tenant", "et");
        Assert.Equal(expected.Id, template!.Id);
    }

    [Fact]
    public async Task Delivery_LookupUsesBothMessageAndChannel()
    {
        using var context = Context();
        var expected = new DeliveryEntity { Id = Guid.NewGuid(), MessageId = "same", Channel = EChannel.Email };
        context.Deliveries = TestSet.Create(
            new DeliveryEntity { Id = Guid.NewGuid(), MessageId = "same", Channel = (EChannel)1 },
            new DeliveryEntity { Id = Guid.NewGuid(), MessageId = "other", Channel = EChannel.Email }, expected);
        var repository = new DeliveryRepository(context, new DeliveryEntityMapper());
        Assert.Equal(expected.Id, (await repository.GetByMessageIdAsync("same", EChannel.Email))!.Id);
        Assert.NotEqual(expected.Id, (await repository.GetByMessageIdAsync("same", (EChannel)1))!.Id);
        Assert.Null(await repository.GetByMessageIdAsync("missing", EChannel.Email));
    }

    [Fact]
    public async Task ClientAndType_AreScopedAndActive()
    {
        using var context = Context();
        var id = Guid.NewGuid();
        context.Clients = TestSet.Create(
            new ClientEntity { Id = Guid.NewGuid(), Source = "identity-hub.tenant" },
            new ClientEntity { Id = id, Source = "identity-hub" },
            new ClientEntity { Source = "disabled", IsActive = false });
        var clients = new ClientRepository(context, new ClientEntityMapper());
        Assert.Equal(id, (await clients.GetActiveBySourceAsync("identity-hub"))!.Id);
        Assert.Null(await clients.GetActiveBySourceAsync("disabled"));
        context.NotificationTypes = TestSet.Create(
            new NotificationTypeEntity { ClientId = Guid.NewGuid(), Key = "invoice" },
            new NotificationTypeEntity { ClientId = id, Key = "disabled", IsActive = false },
            new NotificationTypeEntity { ClientId = id, Key = "invoice" });
        var types = new NotificationTypeRepository(context, new NotificationTypeEntityMapper());
        Assert.Equal(id, (await types.GetActiveAsync(id, "invoice"))!.ClientId);
        Assert.Null(await types.GetActiveAsync(id, "disabled"));
    }

    private AppDbContext Context() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql().Options);
}
