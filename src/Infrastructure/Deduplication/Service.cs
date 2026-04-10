using Application.Services;
using Microsoft.Extensions.Logging;
using Domain.Entities;
using Infrastructure.TransactionDb;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Deduplication;

/// <summary>
/// Database deduplication service.
///
/// Checks and records (ExternalId, SourceId) pairs in the TransactionKeys table.
/// A unique index on that table acts as the final safety net — even if two
/// concurrent aggregation runs slip past the in-memory check, the DB will reject the second insert
/// </summary>
public class DeduplicationService(
    TransactionDbContext dbContext,
    ILogger<DeduplicationService> logger)
    : IDeduplicationService
{
    public async Task<bool> IsDuplicateAsync(string externalId, string sourceId, CancellationToken ct = default)
    {
        return await dbContext.TransactionKeys
            .AnyAsync(x => x.ExternalId == externalId && x.SourceId == sourceId, ct);
    }

    public async Task MarkAsSeenAsync(string externalId, string sourceId, CancellationToken ct = default)
    {
        var key = TransactionKey.Create(externalId, sourceId);
        dbContext.TransactionKeys.Add(key);
        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // A concurrent run already marked this as seen — this is safe to ignore.
            logger.LogDebug(
                "Deduplication key already exists for {ExternalId} / {SourceId} — likely a concurrent run.",
                externalId, sourceId);
 
            // Detach the conflicting entry so the DbContext stays usable
            dbContext.Entry(key).State = EntityState.Detached;
        }
    }
    
    /// <summary>
    /// Postgres unique constraint violations surface as a specific error code.
    /// </summary>
    private bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // Npgsql error code for unique_violation
        const string PostgresUniqueViolationCode = "23505";
 
        return ex.InnerException?.Message.Contains(PostgresUniqueViolationCode) == true
               || ex.InnerException?.GetType().Name == "PostgresException"
               && ex.InnerException.Message.Contains("unique constraint");
    }
}