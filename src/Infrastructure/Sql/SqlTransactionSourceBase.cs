using System.Data;
using Application.Services;
using Dapper;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Sql;

public abstract class SqlTransactionSourceBase(ILogger logger) : ITransactionSource
{
    public abstract string SourceId { get; }
    
    protected abstract IDbConnection CreateConnection();
    protected abstract string BuildQuery(DateTimeOffset since);
    protected abstract RawTransactionDto MapRow(dynamic row);

    public async Task<IEnumerable<dynamic>> FetchAsync(DateTimeOffset since, CancellationToken ct = default)
    {
        logger.LogInformation("Fetching from SQL source {SourceId} since {Since}", SourceId, since);

        try
        {
            using var connection = CreateConnection();
            var rows = await connection.QueryAsync(BuildQuery(since), new { Since = since });
            var results = rows.Select(r => MapRow(r)).ToList();

            logger.LogInformation("Fetched {Count} rows from {SourceId}", results.Count, SourceId);
            return results;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SQL ingestion failed for {SourceId}", SourceId);
            throw;
        }
    }
}