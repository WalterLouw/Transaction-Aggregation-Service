using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Xunit;
using Assert = Xunit.Assert;

namespace UnitTests.Domain.Entities;

public class TransactionTests
{
    [Fact]
    public void Create_ShouldCreateTransaction_WithValidParameters()
    {
        var transaction = Transaction.Create(
            "external-001",
            "mssql-primary",
            249.99m,
            "ZAR",
            DateTimeOffset.UtcNow
        );
        
        Assert.NotEqual(Guid.Empty, transaction.Id);
        Assert.Equal("external-001", transaction.ExternalId);
        Assert.Equal("mssql-primary", transaction.SourceId);
        Assert.Equal(249.99m, transaction.Amount.Amount);
        Assert.Equal("ZAR", transaction.Amount.Currency);
        Assert.Equal(TransactionStatus.Pending, transaction.Status);
    }

    [Xunit.Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WithEmpteExternalId(string externalId)
    {
        var act = () => Transaction.Create(
            externalId,
            "mssql-primary",
            249.99m,
            "ZAR",
            DateTimeOffset.UtcNow
        );   
        
        Assert.Throws<TransactionDomainException>(act);
    }

    [Xunit.Theory]
    [InlineData("ZA")] //Missing last letter
    [InlineData("R")] //Shorthand Rand
    [InlineData("")] //Empty string
    public void Create_ShouldThrow_WithInvalidCurrency(string currency)
    {
        var act = () => Transaction.Create(
            "external-001",
            "mssql-primary",
            249.99m,
            currency,
            DateTimeOffset.UtcNow
        );   
        
        Assert.Throws<TransactionDomainException>(act);
    }

    [Fact]
    public void MarkProcessed_ShouldUpdateStatus()
    {
        var transaction = Transaction.Create(
            "external-001",
            "mssql-primary",
            249.99m,
            "ZAR",
            DateTimeOffset.UtcNow
        );
        
        transaction.MarkProcessed();
        Assert.Equal(TransactionStatus.Processed, transaction.Status);
    }

    [Fact]
    public void Amount_ShouldBeRoundedToTwoDecimalPlaces()
    {
        var transaction = Transaction.Create(
            "external-001",
            "mssql-primary",
            249.989m,
            "ZAR",
            DateTimeOffset.UtcNow
        );
        
        Assert.Equal(249.99m, transaction.Amount.Amount);
    }
}