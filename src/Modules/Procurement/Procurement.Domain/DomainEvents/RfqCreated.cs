using SharedKernel;

namespace Procurement.Domain.DomainEvents;

public sealed record RfqCreated(Guid RfqId, string Title, DateTime OccurredUtc) : IDomainEvent;