namespace Contracts.Responses;

public record TransactionResponse
{
    public Guid Id { get; init; } 
    public string ExternalId { get; init; }
    public string SourceId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public DateTimeOffset IngestedAt { get; init; }
    public string Status { get; init; }
    public string Category { get; init; }
    public Dictionary<string, object> Metadata { get; init; }
}