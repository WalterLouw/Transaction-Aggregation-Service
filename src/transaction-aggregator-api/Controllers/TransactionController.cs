using Application.Handlers;
using Asp.Versioning;
using Contracts.Requests;
using Contracts.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace transaction_aggregator.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/transactions")]
public class TransactionController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Get a single transaction by id
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetTransactionByIdQuery(id), ct);
        return Ok((TransactionResponse)result);
    }

    /// <summary>
    /// Get a paged and filtered list of transactions.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<TransactionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] GetTransactionsRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new GetTransactionsQuery(request), ct);
        return Ok((PagedResponse<TransactionResponse>)result);
    }
}