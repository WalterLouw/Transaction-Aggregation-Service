namespace Contracts.Requests;

public record IngestWebhookRequest
{
    public string SourceId { get; init; }
    public string ExternalId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public  Dictionary<string, object> Metadata { get; init; } = null;
}