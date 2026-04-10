namespace Contracts.Responses;

public record AggregationRunResponse
{
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset CompletedAt { get; init; }
    public int TotalIngested { get; init; }
}