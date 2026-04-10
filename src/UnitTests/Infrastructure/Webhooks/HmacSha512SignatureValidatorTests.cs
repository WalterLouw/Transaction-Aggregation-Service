using System.Security.Cryptography;
using System.Text;
using Infrastructure.Webhooks;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Infrastructure.Webhooks;

public class HmacSha512SignatureValidatorTests
{
    
    private const string SourceId = "provider-a";
    private const string Secret = "test-secret";
 
    private readonly HmacSha512SignatureValidator _validator;
 
    public HmacSha512SignatureValidatorTests()
    {
        var options = new WebhookOptions
        {
            Secrets = new Dictionary<string, string>
            {
                [SourceId] = Secret
            }
        };
 
        _validator = new HmacSha512SignatureValidator(options, NullLogger<HmacSha512SignatureValidator>.Instance);
    }
 
    private static string ComputeSignature(string secret, byte[] payload)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var hash = HMACSHA512.HashData(key, payload);
        return "sha512=" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}