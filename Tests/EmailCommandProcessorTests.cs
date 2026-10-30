using Application;
using Contracts.Application;
using Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Tests;

public class EmailCommandProcessorTests
{
    [Theory]
    [InlineData(EmailDispatchOutcome.Sent, MessageDisposition.Ack)]
    [InlineData(EmailDispatchOutcome.AlreadySent, MessageDisposition.Ack)]
    [InlineData(EmailDispatchOutcome.TransientFailure, MessageDisposition.RequeueTransient)]
    [InlineData(EmailDispatchOutcome.PermanentFailure, MessageDisposition.RejectNoRequeue)]
    public async Task DomainCommand_IsDispatchedWithoutCommandAllowlist(EmailDispatchOutcome outcome, MessageDisposition expected)
    {
        var request = Request();
        var dispatch = new Mock<IEmailDispatchService>();
        dispatch.Setup(x => x.SendTemplatedEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmailDispatchResult.From(outcome));
        var processor = new EmailCommandProcessor(dispatch.Object, NullLogger<EmailCommandProcessor>.Instance);
        Assert.Equal(expected, await processor.ProcessAsync(request));
        dispatch.Verify(x => x.SendTemplatedEmailAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExpiredOrInvalidCommand_DoesNotDispatch(bool expired)
    {
        var request = Request();
        if (expired) request.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        else request.ToEmail = "";
        var dispatch = new Mock<IEmailDispatchService>();
        var processor = new EmailCommandProcessor(dispatch.Object, NullLogger<EmailCommandProcessor>.Instance);
        Assert.Equal(expired ? MessageDisposition.Ack : MessageDisposition.RejectNoRequeue, await processor.ProcessAsync(request));
        dispatch.VerifyNoOtherCalls();
    }

    private static SendTemplatedEmailRequest Request() => new()
    {
        MessageId = "message-1", Source = "identity-hub", Tenant = "food-app",
        NotificationType = "any-database-command", ToEmail = "user@example.com"
    };
}
