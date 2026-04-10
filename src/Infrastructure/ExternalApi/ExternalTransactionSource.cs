using System.Net.Http.Headers;
using System.Text.Json;
using Application.Services;
using Infrastructure.ExternalApi.Models;
using Infrastructure.ExternalApi.Options;
using Infrastructure.Http;
using Microsoft.Extensions.Logging;

namespace Infrastructure.ExternalApi;

///<summary>
/// Fetches transactions from a third party HTTP API.
/// </summary>
public class ExternalTransactionSource : HttpTransactionSourceBase
{
    private readonly ExternalApiOptions _options;
    private readonly ILogger<ExternalTransactionSource> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ExternalTransactionSource(
        HttpClient httpClient,
        ExternalApiOptions options,
        ILogger<ExternalTransactionSource> logger) : base(logger, httpClient)
    {
        _options = options;
        _logger = logger;
        HttpClient.BaseAddress = new Uri(_options.BaseUrl);
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", options.ApiKey);
    }

    public override string SourceId => "external-api";

    protected override async Task<IEnumerable<RawTransactionDto>> FetchInternalAsync(
        DateTimeOffset since, CancellationToken ct)
    {
        var results = new List<RawTransactionDto>();
        string? cursor = null;
        var pageCount = 0;
        const int maxPages = 100; // safety cap against infinite loops

        while (pageCount < maxPages)
        {
            var url = BuildUrl(since, cursor);
            var response = await HttpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);
            var page = JsonSerializer.Deserialize<ExternalApiResponse>(content, JsonOptions);

            if (page?.Data is null || page.Data.Count == 0)
                break;

            results.AddRange(page.Data.Select(MapToRaw));
            pageCount++;

            if (!page.HasMore || page.NextCursor is null)
                break;

            cursor = page.NextCursor;
        }

        if (pageCount >= maxPages)
            _logger.LogWarning("External API source hit max page limit ({Max}) for source {SourceId}.", maxPages, SourceId);

        return results;
    }

    private string BuildUrl(DateTimeOffset since, string? cursor)
    {
        var sinceParam = Uri.EscapeDataString(since.ToString("O"));
        var url = $"/v1/transactions?since={sinceParam}&pageSize={_options.PageSize}";

        if (cursor is not null)
            url += $"&cursor={Uri.EscapeDataString(cursor)}";

        return url;
    }

    private RawTransactionDto MapToRaw(ExternalApiTransaction t)
    {
        return new RawTransactionDto()
        {
            ExternalId = t.Id,
            SourceId = SourceId,
            Amount = t.Amount,
            Currency = t.Currency,
            OccurredAt = t.Timestamp,
            Metadata = new Dictionary<string, object>
            {
                ["reference"] = t.Reference ?? string.Empty,
                ["status"] = t.Status ?? string.Empty
            }
        };
    }
}