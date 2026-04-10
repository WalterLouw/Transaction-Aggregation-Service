using Application.Handlers;
using Application.Services;
using Contracts.Requests;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace UnitTests.Application.Handlers;

public class IngestWebhookHandlerTests
{
    private readonly Mock<ITransactionRepository> _mockTransactionRepository;
    private readonly Mock<IDeduplicationService> _mockDeduplicationService;
    private readonly Mock<ILogger<IngestWebhookHandler>> _mockLogger;
    private readonly IngestWebhookHandler _handler;

    public IngestWebhookHandlerTests()
    {
        _mockTransactionRepository = new Mock<ITransactionRepository>();
        _mockDeduplicationService = new Mock<IDeduplicationService>();
        _mockLogger = new Mock<ILogger<IngestWebhookHandler>>();

        _handler = new IngestWebhookHandler(
            _mockTransactionRepository.Object,
            _mockDeduplicationService.Object,
            _mockLogger.Object);
    }

    private static IngestWebhookCommand BuildCommand(
        string externalId = "external-001",
        string sourceId = "mssql-primary") =>
        new(new IngestWebhookRequest
        {
            ExternalId = externalId,
            SourceId = sourceId,
            Amount = 249.99m,
            Currency = "ZAR",
            OccurredAt = DateTimeOffset.UtcNow
        });

    [Fact]
    public async Task Handle_ShouldSaveAndReturnAccepted_WhenNewTransaction()
    {
        _mockDeduplicationService.Setup(x =>
                x.IsDuplicateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

        Assert.Equal("Accepted", result.Status);
        Assert.NotEqual(Guid.Empty, result.Id);

        _mockTransactionRepository.Verify(repository => repository.SaveAsync(It.IsAny<Transaction>()), Times.Once);
        _mockDeduplicationService.Verify(service => service.MarkAsSeenAsync("external-001", "mssql-primary"),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnDuplicateStatus_WhenDuplicateTransaction()
    {
        _mockDeduplicationService.Setup(x =>
                x.IsDuplicateAsync("external-001", "mssql-primary", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

        Assert.Equal("Duplicate", result.Status);
        Assert.Equal(Guid.Empty, result.Id);

        _mockTransactionRepository.Verify(repository => repository.SaveAsync(It.IsAny<Transaction>()), Times.Never);
        _mockDeduplicationService.Verify(service => service.MarkAsSeenAsync("external-001", "mssql-primary"),
            Times.Never);
    } 
}