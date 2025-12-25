using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Logistics.Api.Messaging;

public sealed class ProcurementEventsConsumer : BackgroundService
{
    private readonly ILogger<ProcurementEventsConsumer> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    // RabbitMQ config (dev)
    private const string HostName = "localhost";
    private const int Port = 5672;
    private const string UserName = "agri";
    private const string Password = "agri_pwd";

    // Topology
    private const string Exchange = "agri.events";
    private const string QueueName = "logistics.procurement-events";
    private const string RoutingKey = "procurement.*.*"; // procurement.rfq.created kabi

    // Consumer tuning
    private const ushort PrefetchCount = 10; // batch/parallelism uchun

    public ProcurementEventsConsumer(ILogger<ProcurementEventsConsumer> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // doimiy retry loop: RabbitMQ down bo‘lsa ham app yiqilmaydi
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                StartRabbitMqTopologyAndConsume(stoppingToken);

                // consume background: cancellation bo‘lguncha kutib turamiz
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // normal shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ consumer crashed. Retrying in 3s...");
                SafeClose();
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }

    private void StartRabbitMqTopologyAndConsume(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = HostName,
            Port = Port,
            UserName = UserName,
            Password = Password,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true, // client auto-recovery
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };

        _connection = factory.CreateConnection(clientProvidedName: "logistics-api-consumer");
        _channel = _connection.CreateModel();

        // exchange & queue declare (idempotent)
        _channel.ExchangeDeclare(exchange: Exchange, type: ExchangeType.Topic, durable: true, autoDelete: false);

        _channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _channel.QueueBind(queue: QueueName, exchange: Exchange, routingKey: RoutingKey);

        // prefetch: bir vaqtning o‘zida nechta msg in-flight
        _channel.BasicQos(prefetchSize: 0, prefetchCount: PrefetchCount, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageReceivedAsync;

        _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

        _logger.LogInformation(
            "RabbitMQ consumer started. Exchange={Exchange} Queue={Queue} RoutingKey={RoutingKey}",
            Exchange, QueueName, RoutingKey);
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        if (_channel is null || !_channel.IsOpen)
            return;

        try
        {
            var messageId = ea.BasicProperties?.MessageId;
            var routingKey = ea.RoutingKey;
            var contentType = ea.BasicProperties?.ContentType;

            var body = Encoding.UTF8.GetString(ea.Body.ToArray());

            _logger.LogInformation(
                "LOGISTICS CONSUMED routingKey={RoutingKey} msgId={MessageId} contentType={ContentType} payload={Payload}",
                routingKey, messageId, contentType, body);

            // TODO: bu yerda real business logic:
            // - routingKey == "procurement.rfq.created" => ShipmentPlanning boshlash
            // - idempotency: messageId bo‘yicha processed_messages jadvalida tekshirish (keyingi bosqichda qilamiz)

            _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LOGISTICS consume failed. Will requeue message.");

            try
            {
                // Requeue=true => keyinroq qayta keladi
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
            catch (Exception nackEx)
            {
                _logger.LogError(nackEx, "Failed to NACK message.");
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping RabbitMQ consumer...");
        SafeClose();
        return base.StopAsync(cancellationToken);
    }

    private void SafeClose()
    {
        try { _channel?.Close(); } catch { /* ignore */ }
        try { _connection?.Close(); } catch { /* ignore */ }
        _channel?.Dispose();
        _connection?.Dispose();
        _channel = null;
        _connection = null;
    }

    public override void Dispose()
    {
        SafeClose();
        base.Dispose();
    }
}
