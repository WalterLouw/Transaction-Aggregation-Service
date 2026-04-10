using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Categorisation;
using Infrastructure.Categorisation.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace UnitTests.Infrastructure.Categorisation;

public class CategorisationServiceTests
{
    private static CategorisationService BuildService(
        params CategorisationRuleOptions[] rules)
    {
        var options = new CategorisationOptions { Rules = rules.ToList() };
        return new CategorisationService(options, NullLogger<CategorisationService>.Instance);
    }

    private static Transaction BuildTransaction(
        decimal amount = 100m,
        string sourceId = "source-a",
        Dictionary<string, object>? metadata = null)
    {
        var t = Transaction.Create("ext-001", sourceId, amount, "ZAR", DateTimeOffset.UtcNow, metadata);
        return t;
    }

    [Fact]
    public void Categorise_WhenKeywordMatchesMetadata_ShouldReturnMatchedCategory()
    {
        var service = BuildService(new CategorisationRuleOptions
        {
            Category = "FoodAndDining",
            Keywords = ["restaurant", "cafe"]
        });

        var transaction = BuildTransaction(metadata: new Dictionary<string, object>
        {
            ["reference"] = "Local Restaurant Payment"
        });

        var result = service.Categorise(transaction);

        result.Should().Be(TransactionCategory.FoodAndDining);
    }

    [Fact]
    public void Categorise_WhenKeywordMatchesSourceId_ShouldReturnMatchedCategory()
    {
        var service = BuildService(new CategorisationRuleOptions
        {
            Category = "Travel",
            Keywords = ["airline"]
        });

        var transaction = BuildTransaction(sourceId: "airline-provider");

        var result = service.Categorise(transaction);

        result.Should().Be(TransactionCategory.Travel);
    }

    [Fact]
    public void Categorise_WhenNoRuleMatches_ShouldReturnOther()
    {
        var service = BuildService(new CategorisationRuleOptions
        {
            Category = "FoodAndDining",
            Keywords = ["restaurant"]
        });

        var transaction = BuildTransaction(metadata: new Dictionary<string, object>
        {
            ["reference"] = "Generic Payment"
        });

        var result = service.Categorise(transaction);

        result.Should().Be(TransactionCategory.Other);
    }

    [Fact]
    public void Categorise_WhenNoRulesDefined_ShouldReturnOther()
    {
        var service = BuildService();
        var transaction = BuildTransaction();

        var result = service.Categorise(transaction);

        result.Should().Be(TransactionCategory.Other);
    }

    [Fact]
    public void Categorise_ShouldBeCaseInsensitive()
    {
        var service = BuildService(new CategorisationRuleOptions
        {
            Category = "Healthcare",
            Keywords = ["pharmacy"]
        });

        var transaction = BuildTransaction(metadata: new Dictionary<string, object>
        {
            ["reference"] = "PHARMACY PURCHASE"
        });

        var result = service.Categorise(transaction);

        result.Should().Be(TransactionCategory.Healthcare);
    }

    [Fact]
    public void Categorise_WhenAmountInRange_ShouldMatch()
    {
        var service = BuildService(new CategorisationRuleOptions
        {
            Category = "Shopping",
            MinAmount = 50m,
            MaxAmount = 200m
        });

        var transaction = BuildTransaction(amount: 100m);

        var result = service.Categorise(transaction);

        result.Should().Be(TransactionCategory.Shopping);
    }

    [Fact]
    public void Categorise_WhenAmountBelowMinimum_ShouldNotMatch()
    {
        var service = BuildService(new CategorisationRuleOptions
        {
            Category = "Shopping",
            MinAmount = 200m
        });

        var transaction = BuildTransaction(amount: 50m);

        var result = service.Categorise(transaction);

        result.Should().Be(TransactionCategory.Other);
    }

    [Fact]
    public void Categorise_WhenAmountAboveMaximum_ShouldNotMatch()
    {
        var service = BuildService(new CategorisationRuleOptions
        {
            Category = "FoodAndDining",
            MaxAmount = 50m
        });

        var transaction = BuildTransaction(amount: 100m);

        var result = service.Categorise(transaction);

        result.Should().Be(TransactionCategory.Other);
    }

    [Fact]
    public void Categorise_WithKeywordAndAmountRule_BothMustMatch()
    {
        var service = BuildService(new CategorisationRuleOptions
        {
            Category = "Travel",
            Keywords = ["airline"],
            MinAmount = 100m
        });

        // Keyword matches but amount is too low — should not match
        var transaction = BuildTransaction(
            amount: 50m,
            metadata: new Dictionary<string, object> { ["reference"] = "airline booking" });

        var result = service.Categorise(transaction);

        result.Should().Be(TransactionCategory.Other);
    }
}