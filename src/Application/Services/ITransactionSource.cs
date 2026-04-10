namespace Application.Services;

/// <summary>
/// Defines a contract for a transaction data source.
/// Used for each source you want to aggregate from (e.g. SQL databases, external APIs).
/// </summary>
public interface ITransactionSource
{
    public string SourceId { get; }
    Task<IEnumerable<RawTransactionDto>> FetchAsync(DateTimeOffset since, CancellationToken ct = default);
}