using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Categorisation.Options;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Categorisation;

/// <summary>
/// Categorizes transactions by evaluating a prioritized list of rules.
/// Rules are matched against metadata values, sourceId, and amount ranges.
/// </summary>
public class CategorisationService : ICategorisationService
{
    private readonly IReadOnlyList<CategorisationRuleOptions> _rules;
    private readonly ILogger<CategorisationService> _logger;

    public CategorisationService(
        CategorisationOptions options,
        ILogger<CategorisationService> logger)
    {
        // Pre-sort by priority so evaluation is always in the right order
        _rules = options.Rules
            .OrderBy(x => x.Category)
            .ToList()
            .AsReadOnly();

        _logger = logger;
    }


    public TransactionCategory Categorise(Transaction transaction)
    {
        foreach (var rule in _rules)
        {
            if (Matches(rule, transaction))
            {
                if (Enum.TryParse<TransactionCategory>(rule.Category, ignoreCase: true, out var category))
                {
                    _logger.LogDebug(
                        "Transaction {Id} categorized as {Category} by rule '{RuleCategory}'",
                        transaction.Id, category, rule.Category);
 
                    return category;
                }
                
                _logger.LogWarning(
                    "Rule category '{Category}' does not match any TransactionCategory value — skipping.",
                    rule.Category);
            }
        }

        return TransactionCategory.Other;
    }

    private static bool Matches(CategorisationRuleOptions rule, Transaction transaction)
    {
        //Amount range to check
        if (rule.MinAmount.HasValue && transaction.Amount.Amount < rule.MinAmount.Value) 
            return false;
        
        if (rule.MaxAmount.HasValue && transaction.Amount.Amount > rule.MaxAmount.Value) 
            return false;
        
        //Match keyword against sourceId and metadata
        if (rule.Keywords.Count > 0)
        {
            var searchTargets = BuildSearchTargets(transaction);

            var hasKeyWordMatch = rule.Keywords.Any(keyword =>
                searchTargets.Any(target =>
                    target.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
            
            if (!hasKeyWordMatch) return false;
        }
        
        return true;
    }

    public static IEnumerable<string> BuildSearchTargets(Transaction transaction)
    {
        yield return transaction.SourceId;

        foreach (var value in transaction.Metadata.Values)
        {
            if (value is string str)
            {
                yield return str;
            }
        }
    }
}