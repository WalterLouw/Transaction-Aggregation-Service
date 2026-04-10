using Contracts.Responses;

namespace Application.Services;

/// <summary>
/// Fetches from all registered sources, deduplication, categorization, and persistence.
/// </summary>
public interface IAggregationService
{
    Task<AggregationRunResponse> RunAsync(CancellationToken ct = default);
}