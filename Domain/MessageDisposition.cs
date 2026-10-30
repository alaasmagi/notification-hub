namespace Domain;

public enum MessageDisposition
{
    Ack,

    RejectNoRequeue,

    RequeueTransient
}
