namespace Procurement.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public string HostName { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "agri";
    public string Password { get; init; } = "agri_pwd";

    public string Exchange { get; init; } = "agri.events";
}