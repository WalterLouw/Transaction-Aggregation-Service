namespace Contracts.Requests;

public record GetTransactionsRequest
{
    public string? SourceId { get; init; } = null;
    public string? Currency { get; init; } = null;
    public DateTimeOffset? From { get; init; } = null;
    public DateTimeOffset? To { get; init; } = null;
    public string? Status { get; init; } = null;
    public string? Category { get; init; } = null;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}