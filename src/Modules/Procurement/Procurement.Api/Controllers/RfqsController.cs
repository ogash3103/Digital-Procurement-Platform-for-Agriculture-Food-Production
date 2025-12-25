using Microsoft.AspNetCore.Mvc;
using Procurement.Application.Abstractions;
using Procurement.Application.Commands.CreateRfq;

namespace Procurement.Api.Controllers;

[ApiController]
[Route("rfqs")]
public sealed class RfqsController : ControllerBase
{
    private readonly CreateRfqHandler _createHandler;
    private readonly IRfqRepository _repo;

    public RfqsController(CreateRfqHandler createHandler, IRfqRepository repo)
    {
        _createHandler = createHandler;
        _repo = repo;
    }

    public sealed record CreateRfqRequest(string Title);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRfqRequest request, CancellationToken ct)
    {
        var rfq = await _createHandler.Handle(new CreateRfqCommand(request.Title), ct);
        return Created($"/rfqs/{rfq.Id}", rfq);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var rfq = await _repo.GetByIdAsync(id, ct);
        return rfq is null ? NotFound() : Ok(rfq);
    }
}