using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Embedding;
using QuartzKnowledgeMcp.Api.Gold;
using QuartzKnowledgeMcp.Api.Persistence;
using QuartzKnowledgeMcp.Api.Search;
using QuartzKnowledgeMcp.Api.Silver;
using QuartzKnowledgeMcp.Tests.Infrastructure;

namespace QuartzKnowledgeMcp.Tests.Search;

public class CatalogSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_MatchesNameAndOverview()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        await SeedPublishedEntryAsync(dbContext, "match-name", "GitHub Search Registry", "Search GitHub registry entries.", "OAuth 2.0", ["VS Code"], new DateTimeOffset(2026, 5, 18, 10, 0, 0, TimeSpan.Zero));
        await SeedPublishedEntryAsync(dbContext, "match-overview", "Slack Helper", "Search Slack workspace messages.", "API key", ["Claude Desktop"], new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero));

        var service = new CatalogSearchService(dbContext);
        var result = await service.SearchAsync("search", [], null, null, "relevance");

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.True(item.Score > 0m));
    }

    [Fact]
    public async Task SearchAsync_AppliesTagAndCondition()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        await SeedPublishedEntryAsync(dbContext, "tag-both", "Azure Search", "Catalog helper.", "OAuth 2.0", ["VS Code"], new DateTimeOffset(2026, 5, 18, 10, 0, 0, TimeSpan.Zero), includeAzureKeyword: true);
        await SeedPublishedEntryAsync(dbContext, "tag-azure", "Azure Helper", "Catalog helper.", "OAuth 2.0", ["VS Code"], new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero), includeSearchKeyword: false, includeAzureKeyword: true);
        await SeedPublishedEntryAsync(dbContext, "tag-search", "Search Helper", "Catalog helper.", "OAuth 2.0", ["VS Code"], new DateTimeOffset(2026, 5, 18, 12, 0, 0, TimeSpan.Zero), includeGithubKeyword: false);

        var service = new CatalogSearchService(dbContext);
        var result = await service.SearchAsync(null, ["azure", "search"], null, null, null);

        Assert.Single(result.Items);
        Assert.Equal("Azure Search", result.Items[0].DisplayName);
    }

    [Fact]
    public async Task QueryAsync_ComposesMultipleConditions_FromRequestDto()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        await SeedPublishedEntryAsync(dbContext, "query-match", "Azure Search", "Search GitHub registry entries.", "OAuth 2.0", ["VS Code"], new DateTimeOffset(2026, 5, 18, 10, 0, 0, TimeSpan.Zero), includeAzureKeyword: true);
        await SeedPublishedEntryAsync(dbContext, "query-miss", "Azure Browser", "Browse GitHub registry entries.", "API key", ["Claude Desktop"], new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero), includeSearchKeyword: false, includeAzureKeyword: true);

        var service = new CatalogSearchService(dbContext);
        var result = await service.QueryAsync(new SearchQueryRequest(
            Query: "search",
            Tags: ["azure"],
            AuthType: "oauth",
            Client: "VS Code",
            Sort: "relevance",
            Page: 1,
            PageSize: 10));

        Assert.Single(result.Items);
        Assert.Equal("Azure Search", result.Items[0].DisplayName);
    }

    [Fact]
    public async Task SearchAsync_AppliesAuthAndClientFilters()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        await SeedPublishedEntryAsync(dbContext, "oauth-vscode", "OAuth Entry", "GitHub search registry.", "OAuth 2.0", ["VS Code"], new DateTimeOffset(2026, 5, 18, 10, 0, 0, TimeSpan.Zero));
        await SeedPublishedEntryAsync(dbContext, "apikey-claude", "ApiKey Entry", "GitHub search registry.", "API key", ["Claude Desktop"], new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero));

        var service = new CatalogSearchService(dbContext);
        var result = await service.SearchAsync(null, [], "oauth", "VS Code", null);

        Assert.Single(result.Items);
        Assert.Equal("OAuth Entry", result.Items[0].DisplayName);
        Assert.Equal("oauth", result.Items[0].AuthenticationType);
    }

    [Fact]
    public async Task SearchAsync_AppliesStableSortingAndPaging()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        await SeedPublishedEntryAsync(dbContext, "sort-b", "Bravo", "GitHub search registry.", "OAuth 2.0", ["VS Code"], new DateTimeOffset(2026, 5, 18, 10, 0, 0, TimeSpan.Zero));
        await SeedPublishedEntryAsync(dbContext, "sort-a", "Alpha", "GitHub search registry.", "OAuth 2.0", ["VS Code"], new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero));
        await SeedPublishedEntryAsync(dbContext, "sort-c", "Charlie", "GitHub search registry.", "OAuth 2.0", ["VS Code"], new DateTimeOffset(2026, 5, 18, 12, 0, 0, TimeSpan.Zero));

        var service = new CatalogSearchService(dbContext);
        var page1 = await service.SearchAsync(null, [], null, null, "name-asc", page: 1, pageSize: 2);
        var page2 = await service.SearchAsync(null, [], null, null, "name-asc", page: 2, pageSize: 2);

        Assert.Equal(["Alpha", "Bravo"], page1.Items.Select(item => item.DisplayName).ToArray());
        Assert.Equal(["Charlie"], page2.Items.Select(item => item.DisplayName).ToArray());
    }

    [Fact]
    public async Task GetSuggestionsAsync_ReturnsDeterministicItems_WithClampedLimit()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        await SeedPublishedEntryAsync(dbContext, "suggest-one", "GitHub Search Registry", "Search GitHub registry entries.", "OAuth 2.0", ["VS Code"], new DateTimeOffset(2026, 5, 18, 10, 0, 0, TimeSpan.Zero));
        await SeedPublishedEntryAsync(dbContext, "suggest-two", "GitHub Sync", "Sync GitHub issues.", "OAuth 2.0", ["VS Code"], new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero), includeSearchKeyword: false);

        var service = new CatalogSearchService(dbContext);
        var result = await service.GetSuggestionsAsync("git", limit: 0, scope: "all");

        Assert.Single(result.Items);
        Assert.Equal("GitHub Search Registry", result.Items[0].Value);
        Assert.Equal("name", result.Items[0].Type);
    }

    [Fact]
    public async Task GetSuggestionsAsync_ReturnsEmpty_WhenQueryIsBlank()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        await SeedPublishedEntryAsync(dbContext, "suggest-empty", "GitHub Search Registry", "Search GitHub registry entries.", "OAuth 2.0", ["VS Code"], new DateTimeOffset(2026, 5, 18, 10, 0, 0, TimeSpan.Zero));

        var service = new CatalogSearchService(dbContext);
        var result = await service.GetSuggestionsAsync("   ");

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetFacetsAsync_ReturnsCountsMatchingCurrentFilters()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        await SeedPublishedEntryAsync(dbContext, "facet-one", "GitHub Search", "Search GitHub registry entries.", "OAuth 2.0", ["VS Code"], new DateTimeOffset(2026, 5, 18, 10, 0, 0, TimeSpan.Zero));
        await SeedPublishedEntryAsync(dbContext, "facet-two", "GitHub Browser", "Browse GitHub registry entries.", "API key", ["VS Code", "Claude Desktop"], new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero), includeSearchKeyword: false);

        var service = new CatalogSearchService(dbContext);
        var result = await service.GetFacetsAsync("git", ["github"], null, null);

        Assert.Contains(result.Tags, item => item.Value.Equals("github", StringComparison.OrdinalIgnoreCase) && item.Count == 2);
        Assert.Contains(result.AuthTypes, item => item.Value == "oauth" && item.Count == 1);
        Assert.Contains(result.AuthTypes, item => item.Value == "api-key" && item.Count == 1);
        Assert.Contains(result.Clients, item => item.Value == "VS Code" && item.Count == 2);
    }

    private static async Task SeedPublishedEntryAsync(
        McpKnowledgeDbContext dbContext,
        string slug,
        string name,
        string summary,
        string authenticationHint,
        IReadOnlyList<string> supportedClients,
        DateTimeOffset timestamp,
        bool includeGithubKeyword = true,
        bool includeSearchKeyword = true,
        bool includeAzureKeyword = false)
    {
        var bronzeService = new BronzeIngestionService(dbContext, new FixedTimeProvider(timestamp.AddMinutes(-2)));
        var silverService = new SilverDraftService(
            KnowledgeStoreTestFixture.CreateKnowledgeRepository(dbContext),
            KnowledgeStoreTestFixture.CreateUnitOfWork(dbContext),
            new RuleBasedOrganizationAgent(new RuleBasedSilverNormalizer()),
            new FixedTimeProvider(timestamp.AddMinutes(-1)));
        var goldService = new GoldCatalogService(
            KnowledgeStoreTestFixture.CreateKnowledgeRepository(dbContext),
            KnowledgeStoreTestFixture.CreateHistoryRepository(dbContext),
            KnowledgeStoreTestFixture.CreateUnitOfWork(dbContext),
            new NoOpSemanticIndexer(),
            new FixedTimeProvider(timestamp));

        var summaryKeywords = new List<string>();
        if (includeGithubKeyword)
        {
            summaryKeywords.Add("GitHub");
        }

        if (includeAzureKeyword)
        {
            summaryKeywords.Add("Azure");
        }

        if (includeSearchKeyword)
        {
            summaryKeywords.Add("search");
        }

        var bronze = await bronzeService.ImportAsync(new CreateBronzeSourceRequest(
            SourceType: "github-readme",
            SourceUri: $"https://github.com/example/{slug}",
            RawContent: BuildRawContent(name, summary, authenticationHint, supportedClients, summaryKeywords),
            ImportedBy: "tester"));
        var silver = await silverService.OrganizeAsync(bronze.Source.Id, SilverOrganizeModes.SilverDraft);
        await goldService.PublishAsync(silver.Draft.Id, "tester");
    }

    private static string BuildRawContent(
        string name,
        string summary,
        string authenticationHint,
        IReadOnlyList<string> supportedClients,
        IReadOnlyList<string> keywords)
    {
        var clientsLine = supportedClients.Count == 0
            ? string.Empty
            : $"Supported clients: {string.Join(", ", supportedClients)}\n\n";
        var keywordLine = keywords.Count == 0
            ? string.Empty
            : $"Keywords: {string.Join(", ", keywords)}\n\n";
        var includeSearchKeyword = keywords.Any(keyword =>
            string.Equals(keyword, "search", StringComparison.OrdinalIgnoreCase));
        var toolLine = includeSearchKeyword
            ? "- search-docs: Search the docs corpus"
            : "- docs-browser: Browse the docs corpus";

        return $"# {name}\n\n{summary}\n\nAuthentication: {authenticationHint}\n\n{clientsLine}{keywordLine}## Tools\n{toolLine}";
    }

    private static async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<McpKnowledgeDbContext> CreateDbContextAsync(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<McpKnowledgeDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new McpKnowledgeDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        return dbContext;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}