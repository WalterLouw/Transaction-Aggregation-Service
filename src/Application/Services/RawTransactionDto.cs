namespace Application.Services;

/// <summary>
/// Raw transaction date transfer object
/// </summary>
public class RawTransactionDto
{
    public string ExternalId { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    
}