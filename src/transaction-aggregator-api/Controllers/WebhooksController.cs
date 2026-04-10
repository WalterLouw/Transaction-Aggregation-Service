using Application.Handlers;
using Application.Services;
using Asp.Versioning;
using Contracts.Requests;
using Contracts.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace transaction_aggregator.Controllers;

/// <summary>
/// Receives inbound transaction webhooks from external providers.
/// Validates signatures before processing.
/// </summary>
[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/webhooks")]
public class WebhooksController(
    IMediator mediator,
    IWebhookSignatureValidator signatureValidator,
    ILogger<WebhooksController> logger)
    : ControllerBase
{
    private const string SignatureHeader = "X-Webhook-Signature";

    /// <remarks>
    /// To test this endpoint, generate a valid signature using the secret from appsettings.json:
    ///
    ///     SECRET="your-secret-here"
    ///     PAYLOAD='{"externalId":"webhook-001","sourceId":"provider-a","amount":150.00,"currency":"USD","occurredAt":"2026-04-10T10:00:00Z"}'
    ///     SIGNATURE="sha512=$(echo -n $PAYLOAD | openssl dgst -sha512 -hmac "$SECRET" | awk '{print $2}')"
    ///     curl -X POST "http://localhost:5000/v1/webhooks/provider-a" \
    ///       -H "Content-Type: application/json" \
    ///       -H "X-Webhook-Signature: $SIGNATURE" \
    ///       -d "$PAYLOAD"
    /// </remarks>
    [HttpPost("{sourceId}")]
    [ProducesResponseType(typeof(WebhookAcceptedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Receive(string sourceId, CancellationToken ct)
    {
        Request.EnableBuffering();
        var payload = await ReadBodyAsync(ct);
 
        var signature = Request.Headers[SignatureHeader].FirstOrDefault() ?? string.Empty;
 
        if (!signatureValidator.IsValid(sourceId, signature, payload))
        {
            logger.LogWarning("Rejected webhook from {SourceId} — invalid signature.", sourceId);
            return Unauthorized(new { error = "Invalid or missing webhook signature." });
        }
 
        Request.Body.Position = 0;
 
        var request = await DeserializeBodyAsync<IngestWebhookRequest>(ct);
        if (request is null)
            return BadRequest(new { error = "Invalid request body." });
 
        logger.LogInformation("Webhook accepted from {SourceId}.", sourceId);
 
        var requestWithSource = request with { SourceId = sourceId };
        var result = await mediator.Send(new IngestWebhookCommand(requestWithSource), ct);
 
        return Ok((WebhookAcceptedResponse)result);
    }
 
    private async Task<byte[]> ReadBodyAsync(CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, ct);
        return ms.ToArray();
    }
 
    private async Task<T?> DeserializeBodyAsync<T>(CancellationToken ct)
    {
        return await System.Text.Json.JsonSerializer.DeserializeAsync<T>(
            Request.Body,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            ct);
    }
}
 
 