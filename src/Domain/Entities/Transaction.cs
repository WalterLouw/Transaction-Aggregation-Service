using Domain.Enums;
using Domain.Exceptions;
using Domain.Values;

namespace Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public string SourceId { get; private set; } = string.Empty;
    public Money Amount { get; private set; } = default!;
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset IngestedAt { get; private set; }
    public TransactionStatus Status { get; private set; }
    public TransactionCategory Category { get; private set; } = TransactionCategory.Other;
    public Dictionary<string, object> Metadata { get; private set; } = new();

    private Transaction()
    {
    } // EF Core

    public static Transaction Create(
        string externalId,
        string sourceId,
        decimal amount,
        string currency,
        DateTimeOffset occurredAt,
        Dictionary<string, object>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            throw new TransactionDomainException("ExternalId cannot be empty.");

        if (string.IsNullOrWhiteSpace(sourceId))
            throw new TransactionDomainException("SourceId cannot be empty.");

        return new Transaction
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId.Trim(),
            SourceId = sourceId.Trim(),
            Amount = new Money(amount, currency),
            OccurredAt = occurredAt,
            IngestedAt = DateTimeOffset.UtcNow,
            Status = TransactionStatus.Pending,
            Category =  TransactionCategory.Other,
            Metadata = metadata ?? new()
        };
    }

    public void MarkProcessed() => Status = TransactionStatus.Processed;
    public void MarkFailed() => Status = TransactionStatus.Failed;
    public void MarkDuplicate() => Status = TransactionStatus.Duplicate;
    public void Categorise(TransactionCategory category) => Category = category; 
}