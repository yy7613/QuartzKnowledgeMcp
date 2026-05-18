using System.Net.Http.Json;
using QuartzKnowledgeMcp.Tests.Infrastructure;

namespace QuartzKnowledgeMcp.Tests.Search;

[Collection(ApiTestCollection.Name)]
public class SearchApiTests(ApiTestFactory factory)
{
    [Fact]
    public async Task ListCatalogEntries_FiltersByTagAuthTypeAndClient()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        await client.CreatePublishedEntryAsync(
            "search-list-one",
            "GitHub Search Server",
            "Search GitHub repositories and docs.",
            "OAuth 2.0",
            "VS Code");
        await client.CreatePublishedEntryAsync(
            "search-list-two",
            "Slack Helper",
            "Slack workflow helper.",
            "API key",
            "Claude Desktop");

        var response = await client.GetFromJsonAsync<GoldListPayload>("/api/gold/catalog?tag=github&authType=oauth&client=VS%20Code");

        Assert.NotNull(response);
        Assert.Single(response.Items);
        Assert.Equal("GitHub Search Server", response.Items[0].DisplayName);
        Assert.Equal("oauth", response.Items[0].AuthenticationType);
    }

    [Fact]
    public async Task SearchCatalog_FiltersAndSortsResults()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        await client.CreatePublishedEntryAsync(
            "search-api-one",
            "Registry Search",
            "Search registry entries from GitHub.",
            "OAuth 2.0",
            "VS Code");
        await client.CreatePublishedEntryAsync(
            "search-api-two",
            "Slack Search",
            "Search Slack archives.",
            "No authentication required",
            "Claude Desktop");

        var response = await client.GetFromJsonAsync<SearchPayload>("/api/search?q=search&tags=github&authType=oauth&client=VS%20Code&sort=relevance");

        Assert.NotNull(response);
        Assert.Single(response.Items);
        Assert.Equal("Registry Search", response.Items[0].DisplayName);
        Assert.True(response.Items[0].Score > 0m);
    }

    [Fact]
    public async Task SearchSuggestions_ReturnExpectedScopedCandidates()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        await client.CreatePublishedEntryAsync(
            "suggest-api-one",
            "GitHub Search Registry",
            "Search registry entries from GitHub.",
            "OAuth 2.0",
            "VS Code");
        await client.CreatePublishedEntryAsync(
            "suggest-api-two",
            "GitHub Sync",
            "Sync registry entries from GitHub.",
            "OAuth 2.0",
            "VS Code");

        var response = await client.GetFromJsonAsync<SearchSuggestionsPayload>("/api/search/suggestions?q=git&limit=5&scope=name");

        Assert.NotNull(response);
        Assert.Equal(2, response.Items.Count);
        Assert.All(response.Items, item => Assert.Equal("name", item.Type));
        Assert.Equal("GitHub Search Registry", response.Items[0].Value);
    }

    [Fact]
    public async Task SearchFacets_ReturnCountsForCurrentConditions()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        await client.CreatePublishedEntryAsync(
            "facet-api-one",
            "GitHub Search Registry",
            "Search registry entries from GitHub.",
            "OAuth 2.0",
            "VS Code");
        await client.CreatePublishedEntryAsync(
            "facet-api-two",
            "GitHub Browser",
            "Browse registry entries from GitHub.",
            "API key",
            "Claude Desktop");

        var response = await client.GetFromJsonAsync<SearchFacetsPayload>("/api/search/facets?q=git&tags=github");

        Assert.NotNull(response);
        Assert.Contains(response.Tags, item => item.Value == "github" && item.Count == 2);
        Assert.Contains(response.AuthTypes, item => item.Value == "oauth" && item.Count == 1);
        Assert.Contains(response.Clients, item => item.Value == "VS Code" && item.Count == 1);
    }

    private sealed record GoldListPayload(IReadOnlyList<GoldListItemPayload> Items, int Page, int PageSize, int TotalCount);
    private sealed record GoldListItemPayload(Guid Id, string DisplayName, string Overview, IReadOnlyList<string> Tags, string AuthenticationType, IReadOnlyList<string> SupportedClients, DateTime UpdatedAtUtc);
    private sealed record SearchPayload(IReadOnlyList<SearchItemPayload> Items, int Page, int PageSize, int TotalCount);
    private sealed record SearchItemPayload(Guid Id, string DisplayName, string Overview, IReadOnlyList<string> Tags, string AuthenticationType, IReadOnlyList<string> SupportedClients, DateTime UpdatedAtUtc, decimal Score);
    private sealed record SearchSuggestionsPayload(IReadOnlyList<SearchSuggestionItemPayload> Items);
    private sealed record SearchSuggestionItemPayload(string Value, string Type, decimal Score);
    private sealed record SearchFacetsPayload(IReadOnlyList<SearchFacetItemPayload> Tags, IReadOnlyList<SearchFacetItemPayload> AuthTypes, IReadOnlyList<SearchFacetItemPayload> Clients);
    private sealed record SearchFacetItemPayload(string Value, int Count);
}