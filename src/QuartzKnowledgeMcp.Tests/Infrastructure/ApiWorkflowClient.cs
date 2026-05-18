using System.Net;
using System.Net.Http.Json;

namespace QuartzKnowledgeMcp.Tests.Infrastructure;

public static class ApiWorkflowClient
{
    public static async Task<Guid> CreateBronzeSourceAsync(
        this HttpClient client,
        string sourceUri,
        string rawContent,
        string sourceType = "github-readme",
        string importedBy = "api-tester")
    {
        var response = await client.PostAsJsonAsync("/api/bronze/sources", new
        {
            sourceType,
            sourceUri,
            rawContent,
            importedBy
        });
        var payload = await response.Content.ReadFromJsonAsync<IdPayload>();

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created,
            $"Unexpected status code: {response.StatusCode}");
        Assert.NotNull(payload);
        return payload.Id;
    }

    public static async Task<Guid> OrganizeBronzeSourceAsync(this HttpClient client, Guid bronzeId)
    {
        var response = await client.PostAsJsonAsync($"/api/bronze/sources/{bronzeId}:organize", new
        {
            mode = "silver-draft"
        });
        var payload = await response.Content.ReadFromJsonAsync<IdPayload>();

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created,
            $"Unexpected status code: {response.StatusCode}");
        Assert.NotNull(payload);
        return payload.Id;
    }

    public static async Task<Guid> PublishSilverDraftAsync(
        this HttpClient client,
        Guid silverId,
        string publishedBy = "api-tester")
    {
        var response = await client.PostAsJsonAsync($"/api/silver/server-drafts/{silverId}:publish", new
        {
            publishedBy
        });
        var payload = await response.Content.ReadFromJsonAsync<IdPayload>();

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created,
            $"Unexpected status code: {response.StatusCode}");
        Assert.NotNull(payload);
        return payload.Id;
    }

    public static async Task<PublishedEntryIds> CreatePublishedEntryAsync(
        this HttpClient client,
        string slug,
        string name,
        string summary,
        string authenticationHint,
        params string[] supportedClients)
    {
        var bronzeId = await client.CreateBronzeSourceAsync(
            $"https://github.com/example/{slug}",
            BuildRawContent(name, summary, authenticationHint, supportedClients));
        var silverId = await client.OrganizeBronzeSourceAsync(bronzeId);
        var goldId = await client.PublishSilverDraftAsync(silverId);
        return new PublishedEntryIds(bronzeId, silverId, goldId);
    }

    private static string BuildRawContent(
        string name,
        string summary,
        string authenticationHint,
        IReadOnlyList<string> supportedClients)
    {
        var clientsLine = supportedClients.Count == 0
            ? string.Empty
            : $"Supported clients: {string.Join(", ", supportedClients)}\n\n";

        return $"# {name}\n\n{summary}\n\nAuthentication: {authenticationHint}\n\n{clientsLine}## Tools\n- search-docs: Search the docs corpus";
    }

    public sealed record PublishedEntryIds(
        Guid BronzeId,
        Guid SilverId,
        Guid GoldId);

    private sealed record IdPayload(Guid Id);
}