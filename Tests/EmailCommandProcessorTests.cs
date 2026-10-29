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
    [Fact]
    public async Task ExistingEnvelopeAndRoutingKey_MapSourceTenantAndActionWithoutChangingContent()
    {
        SendTemplatedEmailRequest? dispatched = null;
        var dispatch = new Mock<IEmailDispatchService>();
        dispatch.Setup(x => x.SendTemplatedEmailAsync(It.IsAny<SendTemplatedEmailRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SendTemplatedEmailRequest, CancellationToken>((request, _) => dispatched = request)
            .ReturnsAsync(EmailDispatchResult.From(EmailDispatchOutcome.Sent));
        var processor = new EmailCommandProcessor(dispatch.Object, NullLogger<EmailCommandProcessor>.Instance);
        var body = Encoding.UTF8.GetBytes("""
            {"id":"message-1","source":"invoice-service","tenant":"invoice-service","action":"invoice","timestamp":"2026-10-29T20:00:00.000Z","contentVersion":"1.0",
             "content":{"email":"user@example.com","locale":"et","invoiceIdentifier":"INV-1"}}
            """);
        var result = await processor.ProcessAsync("invoice-service.invoice-service.invoice", body);
        Assert.Equal(MessageDisposition.Ack, result);
        Assert.Equal("invoice-service", dispatched!.Source);
        Assert.Equal("invoice-service", dispatched.Tenant);
        Assert.Equal("invoice", dispatched.NotificationType);
        Assert.Equal("message-1", dispatched.MessageId);
        Assert.Equal("user@example.com", Assert.IsType<InvoiceEmailContent>(dispatched.Content).Email);
    }
}
