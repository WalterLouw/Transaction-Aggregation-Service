namespace Application.Services;

public interface IWebhookSignatureValidator
{
    /// <summary>
    /// Returns true if the signature header is valid for the given payload and sourceId.
    /// </summary>
    bool IsValid(string sourceId, string signature, byte[] payload);
}