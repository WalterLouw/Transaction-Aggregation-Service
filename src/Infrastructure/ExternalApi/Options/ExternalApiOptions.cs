namespace Infrastructure.ExternalApi.Options;

public class ExternalApiOptions
{
    public const string SectionName = "ExternalApi";

    //Base url of third party transactions
    public string BaseUrl { get; init; } = string.Empty;

    //Bearer token api key sent on each request 
    public string ApiKey { get; init; } = string.Empty;

    //max number of results per page from external api
    public int PageSize { get; init; }
}  