using Microsoft.EntityFrameworkCore;
using Procurement.Application.Abstractions;
using Procurement.Domain.Aggregates.Rfq;

namespace Procurement.Infrastructure.Persistence;

public sealed class RfqRepository : IRfqRepository
{
    private readonly ProcurementDbContext _db;

    public RfqRepository(ProcurementDbContext db) => _db = db;

    public async Task AddAsync(Rfq rfq, CancellationToken ct)
        => await _db.Rfqs.AddAsync(rfq, ct);

    public async Task<Rfq?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _db.Rfqs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}