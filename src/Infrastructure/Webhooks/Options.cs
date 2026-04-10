namespace Infrastructure.Webhooks;

public class WebhookOptions
{
    public const string SectionName = "Webhooks";
    public Dictionary<string, string> Secrets { get; init; } = new();
}