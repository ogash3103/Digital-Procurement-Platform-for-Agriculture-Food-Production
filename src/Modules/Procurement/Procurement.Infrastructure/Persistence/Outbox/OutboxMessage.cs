namespace Procurement.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public DateTime OccurredUtc { get; set; }
    public string Type { get; set; } = default!;
    public string Payload { get; set; } = default!;   // <-- shu bo‘lsin
    public DateTime? ProcessedUtc { get; set; }
    public string? Error { get; set; }
}