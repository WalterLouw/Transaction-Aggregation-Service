using Contracts.Requests;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Repositories;
using Infrastructure.TransactionDb;
using IntegrationTests.Fixtures;
namespace IntegrationTests.Infrastructure.Repositories;

[Collection(PostgresCollection.Name)]
public class TransactionRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private TransactionDbContext _db = default!;
    private TransactionRepository _repository = default!;

    public Task InitializeAsync()
    {
        _db = fixture.CreateDbContext();
        _repository = new TransactionRepository(_db);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _db.Transactions.RemoveRange(_db.Transactions);
        await _db.SaveChangesAsync();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistTransaction()
    {
        var transaction = Transaction.Create("ext-001", "source-a", 100m, "ZAR", DateTimeOffset.UtcNow);

        await _repository.SaveAsync(transaction);

        var saved = await _repository.GetByIdAsync(transaction.Id);
        saved.Should().NotBeNull();
        saved!.ExternalId.Should().Be("ext-001");
        saved.Amount.Amount.Should().Be(100m);
        saved.Amount.Currency.Should().Be("ZAR");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveBatchAsync_ShouldPersistAllTransactions()
    {
        var transactions = Enumerable.Range(1, 5)
            .Select(i => Transaction.Create($"ext-{i:000}", "source-a", i * 10m, "ZAR", DateTimeOffset.UtcNow))
            .ToList();

        await _repository.SaveBatchAsync(transactions);

        var query = new GetTransactionsRequest()
        {
            SourceId = "source-a",
            PageSize = 10
        };
        var result = await _repository.GetPagedAsync(query);

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldFilterBySourceId()
    {
        await _repository.SaveAsync(Transaction.Create("ext-001", "source-a", 100m, "ZAR", DateTimeOffset.UtcNow));
        await _repository.SaveAsync(Transaction.Create("ext-002", "source-b", 200m, "ZAR", DateTimeOffset.UtcNow));

        var query = new GetTransactionsRequest()
        {
            SourceId = "source-a"
        };
        var result = await _repository.GetPagedAsync(query);

        result.TotalCount.Should().Be(1);
        result.Items.First().SourceId.Should().Be("source-a");
    }

    [Fact]
    public async Task GetPagedAsync_ShouldFilterByCurrency()
    {
        await _repository.SaveAsync(Transaction.Create("ext-001", "source-a", 100m, "ZAR", DateTimeOffset.UtcNow));
        await _repository.SaveAsync(Transaction.Create("ext-002", "source-a", 200m, "EUR", DateTimeOffset.UtcNow));

        var query = new GetTransactionsRequest()
        {
            Currency = "ZAR",
        };
        var result = await _repository.GetPagedAsync(query);

        result.TotalCount.Should().Be(1);
        result.Items.First().Currency.Should().Be("ZAR");
    }

    [Fact]
    public async Task GetPagedAsync_ShouldFilterByDateRange()
    {
        var now = DateTimeOffset.UtcNow;
        await _repository.SaveAsync(Transaction.Create("ext-001", "source-a", 100m, "ZAR", now.AddDays(-2)));
        await _repository.SaveAsync(Transaction.Create("ext-002", "source-a", 200m, "ZAR", now.AddDays(-1)));
        await _repository.SaveAsync(Transaction.Create("ext-003", "source-a", 300m, "ZAR", now));

        var query = new GetTransactionsRequest()
        {
            From = now.AddDays(-1).AddHours(-1)
        };
        var result = await _repository.GetPagedAsync(query);

        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldPageCorrectly()
    {
        var transactions = Enumerable.Range(1, 10)
            .Select(i =>
                Transaction.Create($"ext-{i:000}", "source-a", i * 10m, "ZAR", DateTimeOffset.UtcNow.AddMinutes(-i)))
            .ToList();

        await _repository.SaveBatchAsync(transactions);

        var page1 = await _repository.GetPagedAsync(new GetTransactionsRequest()
        {
            Page = 1,
            PageSize = 3,
        });
        var page2 = await _repository.GetPagedAsync(new GetTransactionsRequest()
        {
            Page = 1,
            PageSize = 3,
        });

        page1.Items.Should().HaveCount(3);
        page2.Items.Should().HaveCount(3);
        page1.TotalCount.Should().Be(10);
        page1.TotalPages.Should().Be(4);
        page1.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenTransactionExists()
    {
        await _repository.SaveAsync(Transaction.Create("ext-001", "source-a", 100m, "ZAR", DateTimeOffset.UtcNow));

        var exists = await _repository.ExistsAsync("ext-001", "source-a");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenTransactionDoesNotExist()
    {
        var exists = await _repository.ExistsAsync("nonexistent", "source-a");
        exists.Should().BeFalse();
    }
}