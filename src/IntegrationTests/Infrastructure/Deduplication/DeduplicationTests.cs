using FluentAssertions;
using Infrastructure.Deduplication;
using Infrastructure.TransactionDb;
using IntegrationTests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;

namespace IntegrationTests.Infrastructure.Deduplication;

[Collection(PostgresCollection.Name)]
public class DeduplicationTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private TransactionDbContext _db = default!;
    private DeduplicationService _service = default!;

    public Task InitializeAsync()
    {
        _db = fixture.CreateDbContext();
        _service = new DeduplicationService(_db, NullLogger<DeduplicationService>.Instance);
        return Task.CompletedTask;
    }
 
    public async Task DisposeAsync()
    {
        // Clean up keys inserted during this test so tests stay isolated
        _db.TransactionKeys.RemoveRange(_db.TransactionKeys);
        await _db.SaveChangesAsync();
        await _db.DisposeAsync();
    }
 
    [Fact]
    public async Task IsDuplicateAsync_ShouldReturnFalse_WhenKeyNotSeen()
    {
        var result = await _service.IsDuplicateAsync("ext-001", "source-a");
        result.Should().BeFalse();
    }
 
    [Fact]
    public async Task IsDuplicateAsync_ShouldReturnTrue_AfterMarkAsSeen()
    {
        await _service.MarkAsSeenAsync("ext-001", "source-a");
 
        var result = await _service.IsDuplicateAsync("ext-001", "source-a");
        result.Should().BeTrue();
    }
 
    [Fact]
    public async Task IsDuplicateAsync_ShouldReturnFalse_WhenSameExternalIdDifferentSource()
    {
        await _service.MarkAsSeenAsync("ext-001", "source-a");
 
        // Same ExternalId but different SourceId is NOT a duplicate
        var result = await _service.IsDuplicateAsync("ext-001", "source-b");
        result.Should().BeFalse();
    }
 
    [Fact]
    public async Task MarkAsSeenAsync_ShouldNotThrow_WhenCalledTwiceWithSameKey()
    {
        await _service.MarkAsSeenAsync("ext-001", "source-a");
 
        // Second call should handle the unique constraint gracefully
        var act = async () => await _service.MarkAsSeenAsync("ext-001", "source-a");
        await act.Should().NotThrowAsync();
    }
}
 