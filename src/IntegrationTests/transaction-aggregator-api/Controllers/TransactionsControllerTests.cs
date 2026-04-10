using System.Net;
using System.Net.Http.Json;
using Contracts.Responses;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.TransactionDb;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.transaction_aggregator_api.Controllers;

[Collection(ApiCollection.Name)]
public class TransactionsControllerTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => Task.CompletedTask;
 
    public async Task DisposeAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
        db.Transactions.RemoveRange(db.Transactions);
        await db.SaveChangesAsync();
    }
 
    private async Task SeedTransactionAsync(string externalId, string sourceId, decimal amount, string currency)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
        db.Transactions.Add(Transaction.Create(externalId, sourceId, amount, currency, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
    }
 
    [Fact]
    public async Task GetById_ShouldReturn200_WhenExists()
    {
        await SeedTransactionAsync("ext-001", "source-a", 100m, "ZAR");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
        var id = db.Transactions.First().Id;

        var response = await _client.GetAsync($"api/v1/transactions/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
 
    [Fact]
    public async Task GetById_ShouldReturn404_WhenNotFound()
    {
        var response = await _client.GetAsync($"api/v1/transactions/{Guid.NewGuid()}");
        var body = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Body: {body}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
 
    [Fact]
    public async Task GetPaged_ShouldReturnAllTransactions()
    {
        await SeedTransactionAsync("ext-001", "source-a", 100m, "ZAR");
        await SeedTransactionAsync("ext-002", "source-a", 200m, "ZAR");
 
        var response = await _client.GetAsync("api/v1/transactions");
 
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<TransactionResponse>>();
        result!.TotalCount.Should().BeGreaterThanOrEqualTo(2);
    }
 
    [Fact]
    public async Task GetPaged_ShouldFilterBySourceId()
    {
        await SeedTransactionAsync("ext-001", "source-a", 100m, "ZAR");
        await SeedTransactionAsync("ext-002", "source-b", 200m, "ZAR");
 
        var response = await _client.GetAsync("api/v1/transactions?sourceId=source-a");
 
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<TransactionResponse>>();
        result!.Items.Should().OnlyContain(t => t.SourceId == "source-a");
    }
 
    [Fact]
    public async Task GetPaged_ShouldFilterByCurrency()
    {
        await SeedTransactionAsync("ext-001", "source-a", 100m, "ZAR");
        await SeedTransactionAsync("ext-002", "source-a", 200m, "ZAR");
 
        var response = await _client.GetAsync("api/v1/transactions?currency=ZAR");
 
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<TransactionResponse>>();
        result!.Items.Should().OnlyContain(t => t.Currency == "ZAR");
    }
}
 