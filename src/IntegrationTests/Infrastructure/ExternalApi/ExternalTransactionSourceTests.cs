using System.Net;
using System.Text.Json;
using Application.Services;
using FluentAssertions;
using Infrastructure.AggregationService;
using Infrastructure.Categorisation;
using Infrastructure.Categorisation.Options;
using Infrastructure.Deduplication;
using Infrastructure.ExternalApi;
using Infrastructure.ExternalApi.Models;
using Infrastructure.ExternalApi.Options;
using Infrastructure.Repositories;
using Infrastructure.TransactionDb;
using IntegrationTests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace IntegrationTests.Infrastructure.ExternalApi;

[Collection(PostgresCollection.Name)]
public class ExternalTransactionSourceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private WireMockServer _mockServer = default!;
    private TransactionDbContext _db = default!;
    private AggregationService _aggregationService = default!;
    private ExternalTransactionSource _source = default!;

    public Task InitializeAsync()
    {
        // Start WireMock on a random available port
        _mockServer = WireMockServer.Start();

        var options = new ExternalApiOptions
        {
            BaseUrl = _mockServer.Url!,
            ApiKey = "test-api-key",
            PageSize = 10
        };

        var httpClient = new HttpClient();
        _source = new ExternalTransactionSource(
            httpClient, options, NullLogger<ExternalTransactionSource>.Instance);

        _db = fixture.CreateDbContext();

        var repository = new TransactionRepository(_db);
        var deduplication = new DeduplicationService(_db, NullLogger<DeduplicationService>.Instance);

        var categorisationOptions = new CategorisationOptions
        {
            Rules =
            [
                new CategorisationRuleOptions { Category = "FoodAndDining", Keywords = ["restaurant", "cafe"] },
                new CategorisationRuleOptions { Category = "Travel", Keywords = ["airline", "hotel"] },
                new CategorisationRuleOptions { Category = "Healthcare", Keywords = ["pharmacy", "medical"] }
            ]
        };
 
        var categorisation = new CategorisationService(
            categorisationOptions, NullLogger<CategorisationService>.Instance);
        
        _aggregationService = new AggregationService(
            sources: [_source],
            deduplication: deduplication,
            repository: repository,
            categorisation: categorisation,
            logger: NullLogger<AggregationService>.Instance
        );

        return Task.CompletedTask;
    }
    
    public async Task DisposeAsync()
    {
        _mockServer.Stop();
        _mockServer.Dispose();

        _db.Transactions.RemoveRange(_db.Transactions);
        _db.TransactionKeys.RemoveRange(_db.TransactionKeys);
        await _db.SaveChangesAsync();
        await _db.DisposeAsync();
    }

    private void SetupMockPage(
        List<ExternalApiTransaction> transactions,
        string? nextCursor = null,
        bool hasMore = false,
        string? cursor = null)
    {
        var requestBuilder = Request.Create()
            .WithPath("/v1/transactions")
            .UsingGet();

        if (cursor is not null)
            requestBuilder = requestBuilder.WithParam("cursor", cursor);

        var response = new ExternalApiResponse()
        {
            Data = transactions,
            NextCursor = nextCursor,
            HasMore = hasMore
        };

        _mockServer
            .Given(requestBuilder)
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(response)));
    }

    private static ExternalApiTransaction BuildTransaction(string id, decimal amount = 100m, string? reference = null) => new()
    {
        Id = id,
        Amount = amount,
        Currency = "ZAR",
        Timestamp = DateTimeOffset.UtcNow,
        Reference =  reference ?? $"ref-{id}",
        Status = "completed"
    };

    
    [Fact]
    public async Task FetchAsync_ShouldReturnAllTransactions_WhenSinglePage()
    {
        var transactions = Enumerable.Range(1, 3)
            .Select(i => BuildTransaction($"ext-{i:000}"))
            .ToList();

        SetupMockPage(transactions, hasMore: false);

        var results = await _source.FetchAsync(DateTimeOffset.UtcNow.AddHours(-1));

        results.Should().HaveCount(3);
        results.Select(r => r.ExternalId).Should().BeEquivalentTo(["ext-001", "ext-002", "ext-003"]);
        results.Should().AllSatisfy(r =>
        {
            var dto = (RawTransactionDto)r;
            dto.SourceId.Should().Be("external-api");
        });
    }

    [Fact]
    public async Task FetchAsync_ShouldFollowCursorAndReturnAll_WhenMultiplePages()
    {
        var page1 = Enumerable.Range(1, 3).Select(i => BuildTransaction($"ext-p1-{i:000}")).ToList();
        var page2 = Enumerable.Range(1, 3).Select(i => BuildTransaction($"ext-p2-{i:000}")).ToList();

        SetupMockPage(page1, nextCursor: "cursor-abc", hasMore: true);
        SetupMockPage(page2, nextCursor: null, hasMore: false, cursor: "cursor-abc");

        var results = await _source.FetchAsync(DateTimeOffset.UtcNow.AddHours(-1));

        results.Should().HaveCount(6);
        results.Select(r => r.ExternalId).Should().Contain(["ext-p1-001", "ext-p2-001"]);
    }

    [Fact]
    public async Task FetchAsync_ShouldReturnEmpty_WhenEmptyResponse()
    {
        SetupMockPage([], hasMore: false);

        var results = await _source.FetchAsync(DateTimeOffset.UtcNow.AddHours(-1));

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task FetchAsync_ShouldMapMetadataCorrectly()
    {
        SetupMockPage([BuildTransaction("ext-001")], hasMore: false);

        var results = (await _source.FetchAsync(DateTimeOffset.UtcNow.AddHours(-1))).ToList();

        results.Should().HaveCount(1);
        
        var metadata = results[0].Metadata as Dictionary<string, object>;
        metadata.Should().NotBeNull();
        metadata!.Should().ContainKey("reference");
        metadata!.Should().ContainKey("status");
        metadata!["reference"].Should().Be("ref-ext-001");
        metadata!["status"].Should().Be("completed");
    }

    [Fact]
    public async Task AggregationService_ShouldPersistTransactions_WithExternalApiSource()
    {
        var transactions = Enumerable.Range(1, 5)
            .Select(i => BuildTransaction($"agg-ext-{i:000}", i * 50m))
            .ToList();

        SetupMockPage(transactions, hasMore: false);

        var result = await _aggregationService.RunAsync();

        result.TotalIngested.Should().Be(5);

        var saved = _db.Transactions.Where(t => t.SourceId == "external-api").ToList();
        saved.Should().HaveCount(5);
        saved.Select(t => t.ExternalId).Should().BeEquivalentTo(transactions.Select(t => t.Id));
    }

    [Fact]
    public async Task AggregationService_ShouldDeduplicateTransactions_WhenRanTwice()
    {
        var transactions = Enumerable.Range(1, 3)
            .Select(i => BuildTransaction($"dedup-ext-{i:000}"))
            .ToList();

        SetupMockPage(transactions, hasMore: false);

        // First run — should ingest all 3
        var firstRun = await _aggregationService.RunAsync();
        firstRun.TotalIngested.Should().Be(3);

        // Second run with same transactions — all should be deduplicated
        var secondRun = await _aggregationService.RunAsync();
        secondRun.TotalIngested.Should().Be(0);

        _db.Transactions.Count(t => t.SourceId == "external-api" && t.ExternalId.StartsWith("dedup-ext-"))
            .Should().Be(3);
    }
    
    
    [Fact]
    public async Task AggregationService_ShouldCategoriseDuringIngestion()
    {
        var transactions = new List<ExternalApiTransaction>
        {
            BuildTransaction("cat-001", reference: "Local Restaurant Payment"),
            BuildTransaction("cat-002", reference: "Airline Ticket Booking"),
            BuildTransaction("cat-003", reference: "Generic Payment")
        };
 
        SetupMockPage(transactions, hasMore: false);
 
        await _aggregationService.RunAsync();
 
        var saved = _db.Transactions
            .Where(t => t.ExternalId.StartsWith("cat-"))
            .ToList();
 
        saved.Should().HaveCount(3);
        saved.First(t => t.ExternalId == "cat-001").Category.Should().Be(Domain.Enums.TransactionCategory.FoodAndDining);
        saved.First(t => t.ExternalId == "cat-002").Category.Should().Be(Domain.Enums.TransactionCategory.Travel);
        saved.First(t => t.ExternalId == "cat-003").Category.Should().Be(Domain.Enums.TransactionCategory.Other);
    }

    [Fact]
    public async Task FetchAsync_ShouldThrow_WhenApiReturns500()
    {
        _mockServer
            .Given(Request.Create().WithPath("/v1/transactions").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.InternalServerError));

        var act = async () => await _source.FetchAsync(DateTimeOffset.UtcNow.AddHours(-1));

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}