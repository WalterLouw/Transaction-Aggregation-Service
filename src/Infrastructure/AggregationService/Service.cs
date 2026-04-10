using Application.Services;
using Contracts.Responses;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.AggregationService;

public class AggregationService : IAggregationService
{
    private readonly IEnumerable<ITransactionSource> _sources;
    private readonly IDeduplicationService _deduplication;
    private readonly ITransactionRepository _repository;
    private readonly ILogger<AggregationService> _logger;
    private readonly ICategorisationService _categorisation;

    public AggregationService(
        IEnumerable<ITransactionSource> sources,
        IDeduplicationService deduplication,
        ITransactionRepository repository,
        ILogger<AggregationService> logger,
        ICategorisationService categorisation)
    {
        _sources = sources;
        _deduplication = deduplication;
        _repository = repository;
        _logger = logger;
        _categorisation = categorisation;
    }

    public async Task<AggregationRunResponse> RunAsync(CancellationToken ct = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        
        //Default sliding window that moves forward with each async run
        var since = startedAt - TimeSpan.FromMinutes(15);
        var totalIngested = 0;

        _logger.LogInformation("Aggregation run completed. Sources: {Count}", totalIngested);

        foreach (var source in _sources)
        {
            totalIngested += await ProcessSourceAsync(source, since, ct);
        }

        var completedAt = DateTimeOffset.UtcNow;
        _logger.LogInformation("Aggregation run completed. Sources: {Count}", totalIngested);

        return new AggregationRunResponse()
        {
            StartedAt = startedAt,
            TotalIngested = totalIngested,
            CompletedAt = completedAt,
        };
    }

    private async Task<int> ProcessSourceAsync(ITransactionSource source, DateTimeOffset since, CancellationToken ct)
    {
        try
        {
            var rawItems = await source.FetchAsync(since, ct);
            var toSave = new List<Transaction>();

            foreach (var item in rawItems)
            {
                if (await _deduplication.IsDuplicateAsync(item.ExternalId, item.SourceId, ct)) continue;

                var transaction = Transaction.Create(
                    externalId: item.ExternalId,
                    sourceId: item.SourceId,
                    amount: item.Amount,
                    currency: item.Currency,
                    occurredAt: item.OccurredAt,
                    metadata: item.Metadata
                );

                // Categorize during ingestion before persisting
                var category = _categorisation.Categorise(transaction);
                transaction.Categorise(category);
                transaction.MarkProcessed();
                await _deduplication.MarkAsSeenAsync(item.ExternalId, item.SourceId, ct);
                toSave.Add(transaction);
            }

            if (toSave.Count > 0) await _repository.SaveBatchAsync(toSave, ct);
            
            _logger.LogInformation("Saved {Count} transactions from {SourceId}", toSave.Count, source.SourceId);
            return toSave.Count;
        }
        catch (Exception ex)
        {
            // Isolate failures — one bad source should not halt the others
            _logger.LogError(ex, "Failed to process source {SourceId}", source.SourceId);
            return 0;
        }
    }
}