namespace Infrastructure.Categorisation.Options;

public class CategorisationOptions
{
    public const string SectionName = "Categorisation";

    // Ordered list of rules. Rules are evaluated in priority order (lowest number first).
    // The first matching rule wins. Transactions that match no rule are assigned "Other".
    public List<CategorisationRuleOptions> Rules { get; init; } = new();
}

public class CategorisationRuleOptions
{

    // The category to assign when this rule matches.
    // Must match a value in TransactionCategory enum e.g. "FoodAndDining".
    public string Category { get; init; } = string.Empty;


    // Keywords to match against metadata values and sourceId (case-insensitive).
    public List<string> Keywords { get; init; } = new();
 

    // Optional minimum transaction amount to match.
    public decimal? MinAmount { get; init; }
 

    // Optional maximum transaction amount to match
    public decimal? MaxAmount { get; init; }
}