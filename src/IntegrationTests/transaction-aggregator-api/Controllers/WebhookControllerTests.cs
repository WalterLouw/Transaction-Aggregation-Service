using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Contracts.Responses;
using FluentAssertions;
using Infrastructure.TransactionDb;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.transaction_aggregator_api.Controllers;

[Collection(ApiCollection.Name)]
public class WebhooksControllerTests(ApiFactory factory) : IAsyncLifetime
{
    private const string SourceId = "test-source";
    private const string Secret = "test-secret";

    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => Task.CompletedTask;
 
    public async Task DisposeAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
        db.Transactions.RemoveRange(db.Transactions);
        db.TransactionKeys.RemoveRange(db.TransactionKeys);
        await db.SaveChangesAsync();
    }
 
    private static (HttpContent content, string signature) BuildSignedPayload(object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var payloadBytes = Encoding.UTF8.GetBytes(json);
        var key = Encoding.UTF8.GetBytes(Secret);
        var hash = HMACSHA512.HashData(key, payloadBytes);
        var signature = "sha512=" + Convert.ToHexString(hash).ToLowerInvariant();
 
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return (content, signature);
    }
 
    private static object BuildPayload(string externalId = "ext-001") => new
    {
        externalId,
        sourceId = SourceId,
        amount = 150.00,
        currency = "ZAR",
        occurredAt = DateTimeOffset.UtcNow
    };
 
    [Fact]
    public async Task Receive_WithValidSignature_ShouldReturn200AndAccepted()
    {
        var (content, signature) = BuildSignedPayload(BuildPayload());
 
        var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/webhooks/{SourceId}")
        {
            Content = content
        };
        request.Headers.Add("X-Webhook-Signature", signature);
 
        var response = await _client.SendAsync(request);
 
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WebhookAcceptedResponse>();
        result!.Status.Should().Be("Accepted");
        result.Id.Should().NotBe(Guid.Empty);
    }
 
    [Fact]
    public async Task Receive_ShouldReturn401_WithInvalidSignature()
    {
        var (content, _) = BuildSignedPayload(BuildPayload("ext-002"));
 
        var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/webhooks/{SourceId}")
        {
            Content = content
        };
        request.Headers.Add("X-Webhook-Signature", "sha512=invalidsignature");
 
        var response = await _client.SendAsync(request);
 
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
 
    [Fact]
    public async Task Receive_ShouldReturn401_WithMissingSignature()
    {
        var (content, _) = BuildSignedPayload(BuildPayload("ext-003"));
 
        var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/webhooks/{SourceId}")
        {
            Content = content
        };
        // No signature header
 
        var response = await _client.SendAsync(request);
 
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
 
    [Fact]
    public async Task Receive_ShouldReturnDuplicateStatus_WhenDuplicate()
    {
        var payload = BuildPayload("ext-004");
        var (content1, signature1) = BuildSignedPayload(payload);
        var (content2, signature2) = BuildSignedPayload(payload);
 
        var request1 = new HttpRequestMessage(HttpMethod.Post, $"api/v1/webhooks/{SourceId}") { Content = content1 };
        request1.Headers.Add("X-Webhook-Signature", signature1);
        await _client.SendAsync(request1);
 
        var request2 = new HttpRequestMessage(HttpMethod.Post, $"api/v1/webhooks/{SourceId}") { Content = content2 };
        request2.Headers.Add("X-Webhook-Signature", signature2);
        var response2 = await _client.SendAsync(request2);
 
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response2.Content.ReadFromJsonAsync<WebhookAcceptedResponse>();
        result!.Status.Should().Be("Duplicate");
    }
}
 