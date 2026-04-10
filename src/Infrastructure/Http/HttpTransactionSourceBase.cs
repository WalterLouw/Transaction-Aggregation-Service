using Application.Services;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Infrastructure.Http;

public abstract class HttpTransactionSourceBase : ITransactionSource
{
    private readonly ILogger _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    
    protected readonly HttpClient HttpClient;
    public abstract string SourceId { get; }

    protected HttpTransactionSourceBase(ILogger logger, HttpClient httpClient)
    {
        HttpClient = httpClient;
        _logger = logger;
        
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (ex, delay, attempt, _) =>
                    _logger.LogWarning(ex, "Retry {Attempt} for {SourceId} after {Delay}s",
                        attempt, SourceId, delay.TotalSeconds)
            );
    }

    protected abstract Task<IEnumerable<RawTransactionDto>> FetchInternalAsync(DateTimeOffset since,
        CancellationToken ct);
    
    public async Task<IEnumerable<RawTransactionDto>> FetchAsync(DateTimeOffset since, CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching from HTTP source {SourceId} since {Since}", SourceId,since);
        return await _retryPolicy.ExecuteAsync(() => FetchInternalAsync(since, ct));
    }

}