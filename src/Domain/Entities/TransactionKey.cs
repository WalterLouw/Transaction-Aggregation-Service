namespace Domain.Entities;

/// <summary>
/// Tracks which (ExternalId, SourceId) pairs have already been processed
/// Used for deduplication by the db
/// </summary>
public class TransactionKey
{
    public long Id { get; private set; }
    public string ExternalId { get; private set; }
    public string SourceId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTimeOffset SeenAt { get; private set; }

    private TransactionKey()
    {
    }

    public static TransactionKey Create(string externalId, string sourceId)
    {
        return new TransactionKey()
        {
            ExternalId = externalId,
            SourceId = sourceId,
            SeenAt = DateTimeOffset.UtcNow
        };
    }
}