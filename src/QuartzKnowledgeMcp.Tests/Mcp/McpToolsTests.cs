using Microsoft.Data.Sqlite;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using QuartzKnowledgeMcp.Api.Application;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Gold;
using QuartzKnowledgeMcp.Api.Health;
using QuartzKnowledgeMcp.Api.Mcp;
using QuartzKnowledgeMcp.Api.Search;
using QuartzKnowledgeMcp.Api.Silver;
using QuartzKnowledgeMcp.Tests.Infrastructure;

namespace QuartzKnowledgeMcp.Tests.Mcp;

public class McpToolsTests
{
    [Fact]
    public void HealthTool_ReturnsHealthStatus()
    {
        var now = new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero);
        var healthService = new HealthStatusService(
            new HealthCheckOptions("ok", "QuartzKnowledgeMcp.Api"),
            new TestHostEnvironment("Test"),
            new FixedTimeProvider(now));
        var tool = new HealthMcpTools(healthService);

        var result = tool.get_health();

        Assert.Equal("ok", result.Status);
        Assert.Equal("Test", result.Environment);
        Assert.Equal(now, result.CheckedAtUtc);
    }

    [Fact]
    public async Task BronzeTools_CreateListGetAndOrganize_WorkAsExpected()
    {
        await using var connection = await KnowledgeStoreTestFixture.OpenConnectionAsync();
        await using var dbContext = await KnowledgeStoreTestFixture.CreateDbContextAsync(connection);
        var tool = new BronzeMcpTools(
            KnowledgeStoreTestFixture.CreateBronzeService(dbContext),
            KnowledgeStoreTestFixture.CreateSilverApplicationService(dbContext));

        var created = await tool.create_bronze_source(
            "github-readme",
            "# MCP Bronze Tool\n\nTool-driven bronze flow.\n\n## Tools\n- search-docs: Search docs",
            "https://github.com/example/mcp-bronze-tool",
            "mcp-tester");
        var listed = await tool.list_bronze_sources();
        var detail = await tool.get_bronze_source(created.Source.Id);
        var organized = await tool.organize_bronze_source(created.Source.Id);

        Assert.True(created.Created);
        Assert.Contains(listed.Items, item => item.Id == created.Source.Id);
        Assert.NotNull(detail);
        Assert.Equal("MCP Bronze Tool", organized.Draft.Name);
        Assert.Single(organized.Draft.ToolDrafts);
    }

    [Fact]
    public async Task CatalogTools_PublishUpdateTagsAndHistory_WorkAsExpected()
    {
        await using var connection = await KnowledgeStoreTestFixture.OpenConnectionAsync();
        await using var dbContext = await KnowledgeStoreTestFixture.CreateDbContextAsync(connection);
        var silverDraft = await KnowledgeStoreTestFixture.CreateSilverDraftAsync(dbContext);
        var tool = new CatalogMcpTools(
            KnowledgeStoreTestFixture.CreateSilverApplicationService(dbContext),
            KnowledgeStoreTestFixture.CreateCatalogCurationApplicationService(
                dbContext,
                utcNow: new DateTimeOffset(2026, 5, 19, 13, 0, 0, TimeSpan.Zero)));

        var silverList = await tool.list_silver_server_drafts();
        var silverDetail = await tool.get_silver_server_draft(silverDraft.Id);
        var published = await tool.publish_silver_server_draft(silverDraft.Id, "mcp-publisher");
        var goldList = await tool.list_gold_catalog();
        var updated = await tool.update_gold_catalog_entry(
            published.Entry.Id,
            "Updated from MCP tools",
            "1. Install\n2. Run",
            ["https://example.dev/mcp-tools"],
            ["VS Code", "Claude Desktop"],
            "mcp-editor");
        var tags = await tool.replace_gold_catalog_tags(
            published.Entry.Id,
            ["docs", "automation"],
            "mcp-editor");
        var history = await tool.get_gold_catalog_history(published.Entry.Id, page: 1, pageSize: 10);

        Assert.Contains(silverList.Items, item => item.Id == silverDraft.Id);
        Assert.NotNull(silverDetail);
        Assert.True(published.Created);
        Assert.Contains(goldList.Items, item => item.Id == published.Entry.Id);
        Assert.Equal("Updated from MCP tools", updated.Overview);
        Assert.Equal(["docs", "automation"], tags.Tags);
        Assert.NotNull(history);
        Assert.Equal(3, history.TotalCount);
    }

    [Fact]
    public async Task SearchTools_SearchSuggestionsAndFacets_WorkAsExpected()
    {
        await using var connection = await KnowledgeStoreTestFixture.OpenConnectionAsync();
        await using var dbContext = await KnowledgeStoreTestFixture.CreateDbContextAsync(connection);
        await KnowledgeStoreTestFixture.CreatePublishedEntryAsync(dbContext);
        var searchService = new CatalogSearchService(dbContext);
        var tool = new SearchMcpTools(searchService);

        var search = await tool.search_catalog(query: "search", page: 1, pageSize: 10);
        var suggestions = await tool.get_search_suggestions("search", limit: 5);
        var facets = await tool.get_search_facets(query: "search");

        Assert.NotEmpty(search.Items);
        Assert.NotEmpty(suggestions.Items);
        Assert.Contains(facets.AuthTypes, item => item.Value == "unknown" || item.Value == "oauth");
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "QuartzKnowledgeMcp.Tests";

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}