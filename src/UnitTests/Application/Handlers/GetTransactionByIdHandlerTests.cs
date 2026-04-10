using Application.Handlers;
using Application.Services;
using Domain.Entities;
using Domain.Exceptions;
using Moq;
using Xunit;
using Xunit.Sdk;
using Assert = Xunit.Assert;

namespace UnitTests.Application.Handlers;

public class GetTransactionByIdHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository;
    private readonly GetTransactionByIdHandler _handler;

    public GetTransactionByIdHandlerTests()
    {
        _transactionRepository = new Mock<ITransactionRepository>();
        _handler = new GetTransactionByIdHandler(_transactionRepository.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnTransactionResponse_WhenTransactionExists()
    {
        var transactionId = Guid.NewGuid();
        var transaction = Transaction.Create(
            "external-001",
            "mssql-primary",
            249.99m,
            "ZAR",
            DateTimeOffset.UtcNow
        );
        
        _transactionRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(transaction);
        
        var result = await _handler.Handle(new GetTransactionByIdQuery(transactionId), CancellationToken.None);
        
        Assert.NotNull(result);
        Assert.Equal("external-001", result.ExternalId);
        Assert.Equal(249.99m, result.Amount);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenTransactionDoesNotExist()
    {
        var transactionId = Guid.NewGuid();
        _transactionRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Transaction?)null);
        
        var exception = await Assert.ThrowsAsync<TransactionNotFoundException>(() => _handler.Handle(new GetTransactionByIdQuery(transactionId), CancellationToken.None));
        
        Assert.Equal($"Transaction '{transactionId}' was not found.", exception.Message);
    }
}