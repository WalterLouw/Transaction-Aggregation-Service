using Application.Services;
using Contracts.Requests;
using Contracts.Responses;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories.Mapping;
using Infrastructure.TransactionDb;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TransactionRepository(TransactionDbContext dbContext) : ITransactionRepository
{
    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.Transactions.FindAsync([id], ct);
    }

    public async Task<PagedResponse<TransactionResponse>> GetPagedAsync(GetTransactionsRequest request, CancellationToken ct = default)
    {
        var query = BuildQuery(request);
        var total = await query.CountAsync(ct);
        
        var items = await query
            .OrderByDescending(t => t.OccurredAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);
        
        var totalPages = (int)Math.Ceiling((double)total / request.PageSize);

        return new PagedResponse<TransactionResponse>()
        {
            Items = items.Select(TransactionMapper.ToResponse),
            TotalCount =  total,
            Page =  request.Page,
            PageSize = request.PageSize,
            TotalPages = totalPages,
            HasNextPage = request.Page < totalPages,
            HasPreviousPage = request.Page > 1
        };
    }
    

    public async Task SaveAsync(Transaction transaction, CancellationToken ct = default)
    {
        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync(ct);  
    }

    public async Task SaveBatchAsync(IEnumerable<Transaction> transactions, CancellationToken ct = default)
    {
        dbContext.Transactions.AddRange(transactions);
        await dbContext.SaveChangesAsync(ct);  
    }

    public async Task<bool> ExistsAsync(string externalId, string sourceId, CancellationToken ct = default)
    {
        return await dbContext.Transactions.AnyAsync(t => t.ExternalId == externalId && t.SourceId == sourceId, ct); 
    }
    
    private IQueryable<Transaction> BuildQuery(GetTransactionsRequest request)
    {
        var query = dbContext.Transactions.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(request.SourceId))
            query = query.Where(t => t.SourceId == request.SourceId);
 
        if (!string.IsNullOrWhiteSpace(request.Currency))
            query = query.Where(t => t.Amount.Currency == request.Currency);
 
        if (request.From.HasValue)
            query = query.Where(t => t.OccurredAt >= request.From.Value);
 
        if (request.To.HasValue)
            query = query.Where(t => t.OccurredAt <= request.To.Value);
 
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<TransactionStatus>(request.Status, out var status))
            query = query.Where(t => t.Status == status);
        
        if (!string.IsNullOrWhiteSpace(request.Category) && Enum.TryParse<TransactionCategory>(request.Category, out var category))
            query = query.Where(t => t.Category == category);
 
        return query;
    }
}