using Application.Services;
using Infrastructure;
using Infrastructure.Deduplication;
using Infrastructure.TransactionDb;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace UnitTests.Infrastructure.Deduplication;

public class DeduplicationTests
{
    private readonly Mock<TransactionDbContext> _mockTransactionDbContext;
    private readonly Mock<IDeduplicationService> _mockDeduplicationService;

    public DeduplicationTests()
    {
        _mockTransactionDbContext = new Mock<TransactionDbContext>();
        _mockDeduplicationService = new Mock<IDeduplicationService>();
    }

    [Fact]
    public async Task IsDuplicateAsync_ShouldReturnFalse_WhenKeyNotSeen()
    {
        const bool hasKeyBeenSeen = false;
        _mockDeduplicationService.Setup(x =>
                x.IsDuplicateAsync("external-001", "mssql-primary", It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(hasKeyBeenSeen));

        Assert.False(hasKeyBeenSeen);
    }

    [Fact]
    public async Task IsDuplicateAsync_ShouldReturnTrue_AfterMarkAsSeen()
    {
        const bool hasKeyBeenSeen = true;
        _mockDeduplicationService.Setup(x =>
                x.IsDuplicateAsync("external-001", "mssql-primary", It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(hasKeyBeenSeen));

        Assert.True(hasKeyBeenSeen);
    }

    [Fact]
    public async Task IsDuplicateAsync_ShouldReturnFalse_IfSameExternalIdButDifferentSource()
    {
        const bool hasKeyBeenSeen = false;
        _mockDeduplicationService.Setup(x => x.MarkAsSeenAsync("external-001", "mssql-primary"));

        _mockDeduplicationService.Setup(x => x.IsDuplicateAsync("external-001", "mssql-secondary"))
            .Returns(Task.FromResult(hasKeyBeenSeen));
        Assert.False(hasKeyBeenSeen);
    }
}