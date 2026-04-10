using System.Data;
using Application.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Sql;

public class MssqlTransactionSource(
    string connectionString,
    ILogger<MssqlTransactionSource> logger)
    : SqlTransactionSourceBase(logger)
{
    public override string SourceId => "mssql-primary";

    protected override IDbConnection CreateConnection()
    {
        return new SqlConnection(connectionString);
    }

    protected override string BuildQuery(DateTimeOffset since)
    {
        return @"
            SELECT
                TransactionId as ExternalId,
                Amount,
                Currency,
                CreatedAt as OccurredAt,
                Reference
            FROM dbo.Transactions
            WHERE CreatedAt >= @since
            ORDER BY CreatedAt ASC 
        ";
    }

    protected override RawTransactionDto MapRow(dynamic row)
    {
        return new RawTransactionDto()
        {
            ExternalId = row.ExternalId.ToString(),
            SourceId = SourceId,
            Amount = (decimal)row.Amount,
            Currency = row.Currency,
            OccurredAt = (DateTimeOffset)row.OccurredAt,
            Metadata = new()
            {
                ["Reference"] = row.Reference ?? string.Empty,
            }
        };
    }
}