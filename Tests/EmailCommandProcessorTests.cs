using System.Dynamic;
using System.Text;
using Application;
using Contracts.Application;
using Contracts.External.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Tests;

public class EmailCommandProcessorTests
{
    [Theory]
    [InlineData("invoice")]
    [InlineData("email-user-verify")]
    [InlineData("custom-command")]
    public async Task RoutingKey_MapsAnyCommandAndTenantWithoutChangingContent(string command)
    {
        SendTemplatedEmailRequest? dispatched = null;
        var dispatch = new Mock<IEmailDispatchService>();
        dispatch.Setup(x => x.SendTemplatedEmailAsync(It.IsAny<SendTemplatedEmailRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SendTemplatedEmailRequest, CancellationToken>((request, _) => dispatched = request)
            .ReturnsAsync(EmailDispatchResult.From(EmailDispatchOutcome.Sent));
        var processor = new EmailCommandProcessor(dispatch.Object, NullLogger<EmailCommandProcessor>.Instance);
        var body = Encoding.UTF8.GetBytes("""
            {"id":"message-1","source":"invoice-service","tenant":"payload-tenant","action":"invoice","timestamp":"2026-10-29T20:00:00.000Z","contentVersion":"1.0",
             "content":{"email":"user@example.com","locale":"et","invoiceIdentifier":"INV-1","details":{"enabled":true,"amount":12.5,"count":2,"optional":null},"rows":[{"name":"First"}]}}
            """);
        var result = await processor.ProcessAsync($"invoice-service.invoice-service.{command}", body);
        Assert.Equal(MessageDisposition.Ack, result);
        Assert.Equal("invoice-service", dispatched!.Source);
        Assert.Equal("invoice-service", dispatched.Tenant);
        Assert.Equal(command, dispatched.NotificationType);
        Assert.Equal("message-1", dispatched.MessageId);
        dynamic content = Assert.IsType<ExpandoObject>(dispatched.Content);
        Assert.Equal("user@example.com", (string)content.email);
        Assert.Equal("INV-1", (string)content.invoiceIdentifier);
        Assert.True((bool)content.details.enabled);
        Assert.Equal(12.5m, (decimal)content.details.amount);
        Assert.Equal(2L, (long)content.details.count);
        Assert.Null((object?)content.details.optional);
        Assert.Equal("First", (string)content.rows[0].name);
    }
}
