using Asp.Versioning;
using Contracts.Requests;
using Contracts.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace transaction_aggregator.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/aggregation")]
public class AggregationController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Exposes endpoints for manually triggering the transaction aggregation pipeline.
    /// In production, the aggregation runs will be scheduled automatically via Hangfire.
    /// </summary>
    [HttpPost("run")]
    [ProducesResponseType(typeof(AggregationRunResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Run(CancellationToken ct)
    {
        var result = await mediator.Send(new RunAggregationRequest(), ct);
        return Ok(result);
    }
}