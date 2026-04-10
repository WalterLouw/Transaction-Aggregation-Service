using Contracts.Requests;
using Contracts.Responses;
using Domain.Entities;

namespace Application.Services;

/// <summary>
/// Main transaction interface
/// </summary>
public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResponse<TransactionResponse>> GetPagedAsync(GetTransactionsRequest request, CancellationToken ct = default);
    Task SaveAsync(Transaction transaction, CancellationToken ct = default);
    Task SaveBatchAsync(IEnumerable<Transaction> transactions, CancellationToken ct = default);
    Task<bool> ExistsAsync(string externalId, string sourceId, CancellationToken ct = default);
}