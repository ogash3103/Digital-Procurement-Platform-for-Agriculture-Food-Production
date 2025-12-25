using Procurement.Domain.DomainEvents;
using SharedKernel;

namespace Procurement.Domain.Aggregates.Rfq;

public sealed class Rfq : AggregateRoot
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DateTime CreatedUtc { get; private set; }

    private Rfq() { } // EF uchun

    private Rfq(Guid id, string title, DateTime createdUtc)
    {
        Id = id;
        Title = title;
        CreatedUtc = createdUtc;
    }

    public static Rfq Create(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));

        var rfq = new Rfq(Guid.NewGuid(), title.Trim(), DateTime.UtcNow);

        // Domain Event
        rfq.Raise(new RfqCreated(rfq.Id, rfq.Title, DateTime.UtcNow));

        return rfq;
    }
}