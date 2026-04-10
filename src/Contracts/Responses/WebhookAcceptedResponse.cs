namespace Contracts.Responses;

public record WebhookAcceptedResponse
{
    public Guid Id { get; init; }
    public string Status { get; init; }
}