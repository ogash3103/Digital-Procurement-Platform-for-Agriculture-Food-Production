using Procurement.Application.Abstractions;
using Procurement.Domain.Aggregates.Rfq;

namespace Procurement.Application.Commands.CreateRfq;

public sealed class CreateRfqHandler
{
    private readonly IRfqRepository _repo;

    public CreateRfqHandler(IRfqRepository repo) => _repo = repo;

    public async Task<Rfq> Handle(CreateRfqCommand command, CancellationToken ct)
    {
        var rfq = Rfq.Create(command.Title);

        await _repo.AddAsync(rfq, ct);
        await _repo.SaveChangesAsync(ct);

        return rfq;
    }
}