using Procurement.Domain.Aggregates.Rfq;

namespace Procurement.Application.Abstractions;

public interface IRfqRepository
{
    Task AddAsync(Rfq rfq, CancellationToken ct);
    Task<Rfq?> GetByIdAsync(Guid id, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}