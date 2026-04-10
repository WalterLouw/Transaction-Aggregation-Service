using System.Security.Cryptography;
using System.Text;
using Application.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Webhooks;

public class HmacSha512SignatureValidator(
    WebhookOptions options,
    ILogger<HmacSha512SignatureValidator> logger)
    : IWebhookSignatureValidator
{
    private const string SignaturePrefix = "sha512=";

    public bool IsValid(string sourceId, string signature, byte[] payload)
    {
        if (!options.Secrets.TryGetValue(sourceId, out var secret))
        {
            logger.LogWarning("No webhook secret configured for source {SourceId}.", sourceId);
            return false;
        }
 
        if (string.IsNullOrWhiteSpace(signature))
        {
            logger.LogWarning("Missing signature header for source {SourceId}.", sourceId);
            return false;
        }
 
        if (!signature.StartsWith(SignaturePrefix, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Signature for {SourceId} is missing the '{Prefix}' prefix.", sourceId, SignaturePrefix);
            return false;
        }
 
        var providedHash = signature[SignaturePrefix.Length..];
 
        var key = Encoding.UTF8.GetBytes(secret);
        var computedHash = Convert.ToHexString(HMACSHA512.HashData(key, payload)).ToLowerInvariant();
 
        // Constant-time comparison to prevent timing attacks
        var isValid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash),
            Encoding.UTF8.GetBytes(providedHash.ToLowerInvariant())
        );
 
        if (!isValid)
            logger.LogWarning("Invalid webhook signature for source {SourceId}.", sourceId);
 
        return isValid;
    }
}