namespace Application.Services;

/// <summary>
/// Checks for duplication
/// Marks records as seen on first pass
/// </summary>
public interface IDeduplicationService
{
    Task<bool> IsDuplicateAsync(string externalId, string sourceId, CancellationToken ct = default);
    Task MarkAsSeenAsync(string externalId, string sourceId, CancellationToken ct = default);
}