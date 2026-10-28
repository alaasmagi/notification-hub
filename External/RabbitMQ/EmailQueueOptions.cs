namespace External.RabbitMQ;

/// <summary>Configures consumption from an externally managed email queue.</summary>
public class EmailQueueOptions
{
    public string QueueName { get; set; } = default!;

    public ushort PrefetchCount { get; set; } = 10;
}
