using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Procurement.Infrastructure.Messaging;

namespace Procurement.Infrastructure.Persistence.Outbox;

public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();
                var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

                var batch = await db.OutboxMessages
                    .Where(x => x.ProcessedUtc == null)
                    .OrderBy(x => x.OccurredUtc)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var msg in batch)
                {
                    try
                    {
                        // Routing key: procurement.rfq.created kabi
                        var routingKey = InferRoutingKey(msg.Type);

                        await publisher.PublishAsync(routingKey, msg.Payload, msg.Id, stoppingToken);

                        msg.ProcessedUtc = DateTime.UtcNow;
                        msg.Error = null;

                        _logger.LogInformation("OUTBOX -> RABBITMQ published {Id} {RoutingKey}", msg.Id, routingKey);
                    }
                    catch (Exception ex)
                    {
                        msg.Error = ex.Message;
                        _logger.LogError(ex, "OUTBOX publish failed {Id}", msg.Id);
                    }
                }

                if (batch.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox processing loop failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private static string InferRoutingKey(string type)
    {
        // type: "Procurement.Domain.DomainEvents.RfqCreated, Procurement.Domain, ..."
        if (type.Contains("RfqCreated", StringComparison.OrdinalIgnoreCase))
            return "procurement.rfq.created";

        return "procurement.unknown";
    }
}
