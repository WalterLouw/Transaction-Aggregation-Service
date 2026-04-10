using Application.Services;
using Contracts.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Handlers;

public abstract record RunAggregationCommand : IRequest<AggregationRunResponse>;

public class RunAggregationHandler(
    IAggregationService aggregationService,
    ILogger<IAggregationService> logger)
    : IRequestHandler<RunAggregationCommand, AggregationRunResponse>
{
    public async Task<AggregationRunResponse> Handle(RunAggregationCommand request, CancellationToken ct)
    {
        logger.LogInformation("Manual aggregation run triggered.");
        return await aggregationService.RunAsync(ct);
    }
}