using Base.Message.RabbitMQ;
using Contracts.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace External.RabbitMQ;

/// <summary>Consumes email commands with routing-key validation and per-message retry control.</summary>
// The typed consumer does not expose routing keys or per-message retry decisions.
public class EmailEventConsumer : BackgroundService
{
    private readonly RabbitMqConnectionManager _connectionManager;
    private readonly EmailQueueOptions _queueOptions;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailEventConsumer> _logger;

    public EmailEventConsumer(
        RabbitMqConnectionManager connectionManager,
        EmailQueueOptions queueOptions,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailEventConsumer> logger)
    {
        _connectionManager = connectionManager;
        _queueOptions = queueOptions;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await _connectionManager.CreateChannelAsync(cancellationToken: stoppingToken);
        try
        {
            await channel.BasicQosAsync(0, _queueOptions.PrefetchCount, global: false, stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (_, ea) => OnReceivedAsync(channel, ea, stoppingToken);

            // The broker topology is managed outside this application.
            await channel.BasicConsumeAsync(
                _queueOptions.QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "Consuming email commands from queue '{Queue}' (prefetch {Prefetch}).",
                _queueOptions.QueueName, _queueOptions.PrefetchCount);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        finally
        {
            await channel.DisposeAsync();
        }
    }

    private async Task OnReceivedAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IEmailCommandProcessor>();

            var disposition = await processor.ProcessAsync(ea.RoutingKey, ea.Body, stoppingToken);

            switch (disposition)
            {
                case MessageDisposition.Ack:
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
                    break;

                case MessageDisposition.RequeueTransient:
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
                    break;

                case MessageDisposition.RejectNoRequeue:
                default:
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, stoppingToken);
                    break;
            }
        }
        catch (Exception exception)
        {
            // Let the queue delivery limit bound retries; never log message bodies.
            _logger.LogError(
                exception,
                "Unexpected error processing delivery; requeueing. RoutingKey: {RoutingKey}", ea.RoutingKey);

            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
        }
    }
}
