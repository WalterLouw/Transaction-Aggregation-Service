using Domain.Exceptions;

namespace Domain.Values;

public record Money
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    public Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
            throw new TransactionDomainException($"'{currency}' is not a valid ISO 4217 currency code.");
 
        Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.ToUpperInvariant().Trim();
    }
    
    public override string ToString() => $"{Amount} {Currency}";
}