using Application.Handlers;
using Application.Services;
using Asp.Versioning;
using Contracts.Requests;
using Contracts.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace transaction_aggregator.Controllers;

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

    /// <summary>
    /// Receive an inbound transaction webhook from an external provider.
    /// Validates the signature before processing.
    /// </summary>
    [HttpPost("{sourceId}")]
    [ProducesResponseType(typeof(WebhookAcceptedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Receive(string sourceId, CancellationToken ct)
    {
        // Buffer the body so we can both validate the signature and deserialize
        Request.EnableBuffering();
        var payload = await ReadBodyAsync(ct);

        var signature = Request.Headers[SignatureHeader].FirstOrDefault() ?? string.Empty;

        if (!signatureValidator.IsValid(sourceId, signature, payload))
        {
            logger.LogWarning("Rejected webhook from {SourceId} — invalid signature.", sourceId);
            return Unauthorized(new { error = "Invalid or missing webhook signature." });
        }

        // Rewind so the model binder can deserialize the body
        Request.Body.Position = 0;

        var request = await DeserializeBodyAsync<IngestWebhookRequest>(ct);
        if (request is null)
            return BadRequest(new { error = "Invalid request body." });

        logger.LogInformation("Webhook accepted from {SourceId}.", sourceId);

        var requestWithSource = request with { SourceId = sourceId };
        var result = await mediator.Send(new IngestWebhookCommand(requestWithSource), ct);

        return Ok(result);
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