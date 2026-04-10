using System.Text.Json.Serialization;

namespace Infrastructure.ExternalApi.Models;

/// <summary>
/// Represents the paged response envelope from the third party API.
/// Adjust property names to match the real provider's response shape.
/// </summary>
public class ExternalApiResponse
{
    [JsonPropertyName("data")]
    public List<ExternalApiTransaction> Data { get; set; } = new();
 
    [JsonPropertyName("nextCursor")]
    public string? NextCursor { get; set; }
 
    [JsonPropertyName("hasMore")]
    public bool HasMore { get; set; }
}

/// <summary>
/// Represents a single transaction as returned by the third party API.
/// </summary>
public class ExternalApiTransaction
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
 
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
 
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;
 
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
 
    [JsonPropertyName("reference")]
    public string? Reference { get; set; }
 
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}