using Domain;
using DTO.DataAccess.Mappers;
using DTO.Web.Mappers;
using Xunit;

namespace Tests;

public class MapperTests
{
    [Fact]
    public void Client_RoundTripsThroughBothEdges()
    {
        var original = new Client { Id = Guid.NewGuid(), Source = "identity-hub", DisplayName = "Identity", IsActive = false };
        var persistence = new ClientEntityMapper();
        var web = new ClientDtoMapper();
        Assert.Equivalent(original, persistence.Map(persistence.Map(original)), strict: true);
        Assert.Equivalent(original, web.Map(web.Map(original)), strict: true);
    }

    [Fact]
    public void NotificationType_RoundTripsThroughBothEdges()
    {
        var original = new NotificationType { Id = Guid.NewGuid(), ClientId = Guid.NewGuid(), Key = "invoice", IsActive = false };
        var persistence = new NotificationTypeEntityMapper();
        var web = new NotificationTypeDtoMapper();
        Assert.Equivalent(original, persistence.Map(persistence.Map(original)), strict: true);
        Assert.Equivalent(original, web.Map(web.Map(original)), strict: true);
    }

    [Fact]
    public void EmailSender_RoundTripsThroughBothEdges()
    {
        var original = new EmailSender { Id = Guid.NewGuid(), ClientId = Guid.NewGuid(), Tenant = "tenant", FromAddress = "from@example.com", DisplayName = "Sender", ReplyTo = "reply@example.com", IsActive = false };
        var persistence = new EmailSenderEntityMapper();
        var web = new EmailSenderDtoMapper();
        Assert.Equivalent(original, persistence.Map(persistence.Map(original)), strict: true);
        Assert.Equivalent(original, web.Map(web.Map(original)), strict: true);
    }

    [Fact]
    public void Template_RoundTripsThroughBothEdges()
    {
        var original = new Template { Id = Guid.NewGuid(), NotificationTypeId = Guid.NewGuid(), Channel = EChannel.Email, Tenant = "tenant", LanguageCode = "et", Title = "Hello @Model.Name", Body = "<p>Body</p>", IsActive = false };
        var persistence = new TemplateEntityMapper();
        var web = new TemplateDtoMapper();
        Assert.Equivalent(original, persistence.Map(persistence.Map(original)), strict: true);
        Assert.Equivalent(original, web.Map(web.Map(original)), strict: true);
    }

    [Fact]
    public void Delivery_RoundTripsThroughBothEdges()
    {
        var original = new Delivery { Id = Guid.NewGuid(), MessageId = "message", Source = "identity-hub", Tenant = "tenant", NotificationType = "invoice", Channel = EChannel.Email, Recipient = "user@example.com", Title = "Rendered", Status = EDeliveryStatus.Failed, Error = "Failure", SentAt = DateTime.UtcNow };
        var persistence = new DeliveryEntityMapper();
        var web = new DeliveryDtoMapper();
        Assert.Equivalent(original, persistence.Map(persistence.Map(original)), strict: true);
        Assert.Equivalent(original, web.Map(web.Map(original)), strict: true);
    }

}
