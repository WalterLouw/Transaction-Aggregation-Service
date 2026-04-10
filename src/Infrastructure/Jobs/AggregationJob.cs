using Application.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs;

public class AggregationJob(
    IAggregationService aggregationService,
    ILogger<AggregationJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Hangfire triggered aggregation job starting.");

        var result = await aggregationService.RunAsync(ct);
        
        logger.LogInformation(
            "Aggregation job complete. Ingested: {Total}, Duration: {Duration}ms",
            result.TotalIngested,
            (result.CompletedAt - result.StartedAt).TotalMilliseconds);
    }
}