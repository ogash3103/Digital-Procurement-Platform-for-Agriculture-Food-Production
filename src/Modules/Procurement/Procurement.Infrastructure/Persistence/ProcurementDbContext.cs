using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Procurement.Domain.Aggregates.Rfq;
using Procurement.Infrastructure.Persistence.Outbox;
using SharedKernel;

namespace Procurement.Infrastructure.Persistence;

public sealed class ProcurementDbContext : DbContext
{
    public ProcurementDbContext(DbContextOptions<ProcurementDbContext> options) : base(options) { }

    public DbSet<Rfq> Rfqs => Set<Rfq>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // ✅ RFQ table mapping
        modelBuilder.Entity<Rfq>(b =>
        {
            b.ToTable("rfqs");
            b.HasKey(x => x.Id);
            // kerak bo‘lsa Title va boshqa property mappinglarni ham shu yerga yozasiz
        });


        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("outbox_messages");
            b.HasKey(x => x.Id);

            b.Property(x => x.Id).HasColumnName("Id");
            b.Property(x => x.OccurredUtc).HasColumnName("OccurredUtc");
            b.Property(x => x.Type).HasColumnName("Type");
            b.Property(x => x.Payload).HasColumnName("Payload");   // ✅ shu
            b.Property(x => x.ProcessedUtc).HasColumnName("ProcessedUtc");
            b.Property(x => x.Error).HasColumnName("Error");
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 1) Track qilinayotgan AggregateRoot’lardan domain eventlarni yig‘amiz
        var aggregates = ChangeTracker.Entries()
            .Where(e => e.Entity is AggregateRoot)
            .Select(e => (AggregateRoot)e.Entity)
            .ToList();

        var domainEvents = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        // 2) Domain event -> OutboxMessage (transaction ichida)
        foreach (var ev in domainEvents)
        {
            OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                OccurredUtc = ev.OccurredUtc,
                Type = ev.GetType().AssemblyQualifiedName ?? ev.GetType().FullName ?? "Unknown",
                Payload = JsonSerializer.Serialize(ev, ev.GetType()),
                ProcessedUtc = null
            });
        }

        // 3) Avval db commit, so‘ng events clear
        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var a in aggregates) a.ClearDomainEvents();

        return result;
    }
}
