using Application.Services;

namespace Infrastructure.MockTransactions;

/// <summary>
/// Development-only transaction source that generates realistic fake transactions.
/// Registered automatically when the app runs in the Development environment.
/// Allows Swagger to be used without any real data source connections.
/// </summary>
public class MockTransactionSource : ITransactionSource
{
    public string SourceId => "mock-source";
 
    private static readonly string[] References =
    [
        "Local Restaurant Payment", "Uber Trip", "Netflix Subscription",
        "Amazon Purchase", "Electricity Bill", "Pharmacy Purchase",
        "Coffee Shop", "Hotel Booking", "Spotify Premium",
        "Grocery Store", "Dental Appointment", "Train Ticket",
        "Steam Game Purchase", "Internet Bill", "Sushi Restaurant",
        "Airline Ticket", "Cinema Tickets", "Clothing Store",
        "Doctor Consultation", "Cafe Latte"
    ];
 
    private static readonly string[] Currencies = ["USD", "EUR", "GBP", "ZAR"];
 
    public Task<IEnumerable<RawTransactionDto>> FetchAsync(DateTimeOffset since, CancellationToken ct = default)
    {
        var rng = new Random();
 
        var transactions = Enumerable.Range(1, 20)
            .Select(i =>
            {
                var reference = References[rng.Next(References.Length)];
                var currency = Currencies[rng.Next(Currencies.Length)];
                var amount = Math.Round((decimal)(rng.NextDouble() * 490 + 10), 2);
                var occurredAt = since.AddMinutes(rng.Next(1, (int)(DateTimeOffset.UtcNow - since).TotalMinutes + 1));

                return new RawTransactionDto
                {
                    ExternalId= $"mock-{Guid.NewGuid():N}",
                    SourceId= SourceId,
                    Amount= amount,
                    Currency= currency,
                    OccurredAt= occurredAt,
                    Metadata= new Dictionary<string, object>
                    {
                        ["reference"] = reference,
                        ["status"] = "completed",
                        ["mock"] = "true"
                    }
                };
            });
 
        return Task.FromResult(transactions);
    }
}