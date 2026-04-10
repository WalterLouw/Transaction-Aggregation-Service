using Contracts.Responses;
using Domain.Entities;

namespace Application.Mapping;

internal static class TransactionMapper
{
    internal static TransactionResponse ToResponse(Transaction transaction)
    {
        return new TransactionResponse()
        {
            Id = transaction.Id,
            ExternalId = transaction.ExternalId,
            SourceId = transaction.SourceId,
            Amount = transaction.Amount.Amount,
            Currency = transaction.Amount.Currency,
            OccurredAt = transaction.OccurredAt,
            IngestedAt = transaction.IngestedAt,
            Status = transaction.Status.ToString(),
            Category = transaction.Category.ToString(),
            Metadata = transaction.Metadata
        };
    }
}