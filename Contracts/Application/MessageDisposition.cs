namespace Contracts.Application;

public enum MessageDisposition
{
    Ack,

    RejectNoRequeue,

    RequeueTransient
}
