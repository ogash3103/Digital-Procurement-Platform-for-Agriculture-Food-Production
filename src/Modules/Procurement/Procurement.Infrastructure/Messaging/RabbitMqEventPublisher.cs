using System.Text;
using RabbitMQ.Client;

namespace Procurement.Infrastructure.Messaging;

public interface IEventPublisher
{
    Task PublishAsync(string routingKey, string payload, Guid messageId, CancellationToken ct);
}

public sealed class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly RabbitMqOptions _opt;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqEventPublisher(RabbitMqOptions opt) => _opt = opt;

    private void EnsureConnected()
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
            return;

        var factory = new ConnectionFactory
        {
            HostName = _opt.HostName,
            Port = _opt.Port,
            UserName = _opt.UserName,
            Password = _opt.Password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_opt.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
    }

    public Task PublishAsync(string routingKey, string payload, Guid messageId, CancellationToken ct)
    {
        EnsureConnected();

        var props = _channel!.CreateBasicProperties();
        props.Persistent = true;
        props.MessageId = messageId.ToString();
        props.ContentType = "application/json";

        var body = Encoding.UTF8.GetBytes(payload);

        _channel.BasicPublish(_opt.Exchange, routingKey, props, body);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try { _channel?.Close(); } catch { }
        try { _connection?.Close(); } catch { }
    }
}