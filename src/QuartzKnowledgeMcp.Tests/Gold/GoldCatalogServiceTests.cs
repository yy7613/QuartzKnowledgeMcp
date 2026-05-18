using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Gold;
using QuartzKnowledgeMcp.Api.Persistence;
using QuartzKnowledgeMcp.Api.Silver;

namespace QuartzKnowledgeMcp.Tests.Gold;

public class GoldCatalogServiceTests
{
    [Fact]
    public async Task UpdateAsync_UpdatesEditableFieldsOnly_AndKeepsTags()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var published = await CreatePublishedEntryAsync(dbContext);
        var service = CreateService(dbContext, new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero));

        var result = await service.UpdateAsync(
            published.EntryId,
            new UpdateGoldCatalogEntryRequest(
                "Updated overview",
                "1. Install\n2. Configure",
                ["https://example.dev/docs"],
                ["VS Code", "Claude Desktop"],
                "editor"));
        var stored = await dbContext.GoldCatalogEntries.SingleAsync();

        Assert.Equal("Updated overview", stored.Overview);
        Assert.Equal("editor", stored.UpdatedBy);
        Assert.Equal(published.InitialTags, GoldCatalogJson.DeserializeList<string>(stored.TagsJson));
        Assert.Equal("Updated overview", result.Overview);
        Assert.Equal(["VS Code", "Claude Desktop"], result.SupportedClients);
    }

    [Fact]
    public async Task UpdateAsync_AppendsHistory_WhenCatalogUpdated()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var published = await CreatePublishedEntryAsync(dbContext);
        var service = CreateService(dbContext, new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero));

        var result = await service.UpdateAsync(
            published.EntryId,
            new UpdateGoldCatalogEntryRequest(
                "Updated overview",
                "Updated setup",
                ["https://example.dev/docs"],
                ["VS Code"],
                "editor"));
        var histories = await dbContext.EntryHistories
            .OrderBy(history => history.ChangedAtUtc)
            .ToListAsync();

        Assert.Equal(2, histories.Count);
        Assert.Equal(EntryHistoryActions.CatalogUpdated, histories[^1].Action);
        Assert.Equal(2, result.HistoryCount);
    }

    [Fact]
    public async Task UpdateAsync_RejectsMissingOverview()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var published = await CreatePublishedEntryAsync(dbContext);
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<GoldValidationException>(() =>
            service.UpdateAsync(
                published.EntryId,
                new UpdateGoldCatalogEntryRequest(
                    " ",
                    "Updated setup",
                    ["https://example.dev/docs"],
                    ["VS Code"],
                    "editor")));

        Assert.Contains("overview", exception.Errors.Keys);
    }

    [Fact]
    public async Task ReplaceTagsAsync_RejectsTooManyTags()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var published = await CreatePublishedEntryAsync(dbContext);
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<GoldValidationException>(() =>
            service.ReplaceTagsAsync(
                published.EntryId,
                new ReplaceGoldCatalogTagsRequest(
                    ["one", "two", "three", "four", "five", "six"],
                    "editor")));

        Assert.Contains("tags", exception.Errors.Keys);
    }

    [Fact]
    public async Task ReplaceTagsAsync_RejectsDuplicateTags()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var published = await CreatePublishedEntryAsync(dbContext);
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<GoldValidationException>(() =>
            service.ReplaceTagsAsync(
                published.EntryId,
                new ReplaceGoldCatalogTagsRequest(
                    ["github", "GitHub"],
                    "editor")));

        Assert.Contains("tags", exception.Errors.Keys);
    }

    [Fact]
    public async Task ReplaceTagsAsync_AppendsHistory_WhenTagsUpdated()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var published = await CreatePublishedEntryAsync(dbContext);
        var service = CreateService(dbContext, new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero));

        var result = await service.ReplaceTagsAsync(
            published.EntryId,
            new ReplaceGoldCatalogTagsRequest(["github", "registry"], "editor"));
        var histories = await dbContext.EntryHistories
            .OrderBy(history => history.ChangedAtUtc)
            .ToListAsync();

        Assert.Equal(["github", "registry"], result.Tags);
        Assert.Equal(2, histories.Count);
        Assert.Equal(EntryHistoryActions.TagsReplaced, histories[^1].Action);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsPagedItems()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var published = await CreatePublishedEntryAsync(dbContext);
        var service = CreateService(dbContext, new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero));
        await service.UpdateAsync(
            published.EntryId,
            new UpdateGoldCatalogEntryRequest(
                "Updated overview",
                "Updated setup",
                ["https://example.dev/docs"],
                ["VS Code"],
                "editor"));
        await service.ReplaceTagsAsync(
            published.EntryId,
            new ReplaceGoldCatalogTagsRequest(["github", "registry"], "editor"));

        var page = await service.GetHistoryAsync(published.EntryId, page: 2, pageSize: 2);

        Assert.NotNull(page);
        Assert.Equal(2, page.Page);
        Assert.Equal(2, page.PageSize);
        Assert.Equal(3, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal(EntryHistoryActions.Published, page.Items[0].Action);
    }

    [Fact]
    public async Task PublishAsync_CreatesGoldEntry_WhenSilverDraftExists()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var silverDraft = await CreateSilverDraftAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.PublishAsync(silverDraft.Id, "publisher");
        var storedEntry = await dbContext.GoldCatalogEntries.SingleAsync();

        Assert.True(result.Created);
        Assert.Equal(silverDraft.Id, storedEntry.SilverServerDraftId);
        Assert.Equal("Acme MCP Server", storedEntry.DisplayName);
        Assert.Equal("publisher", storedEntry.PublishedBy);
        Assert.Equal(1, result.Entry.HistoryCount);
    }

    [Fact]
    public async Task PublishAsync_AppendsHistory_WhenEntryAlreadyExists()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var silverDraft = await CreateSilverDraftAsync(dbContext);
        var service = CreateService(dbContext);

        var first = await service.PublishAsync(silverDraft.Id, "first-publisher");
        var second = await service.PublishAsync(silverDraft.Id, "second-publisher");
        var histories = await dbContext.EntryHistories
            .OrderBy(history => history.ChangedAtUtc)
            .ToListAsync();
        var storedEntry = await dbContext.GoldCatalogEntries.SingleAsync();

        Assert.True(first.Created);
        Assert.False(second.Created);
        Assert.Equal(2, histories.Count);
        Assert.Equal(EntryHistoryActions.Published, histories[0].Action);
        Assert.Equal(EntryHistoryActions.Republished, histories[1].Action);
        Assert.Equal("second-publisher", storedEntry.UpdatedBy);
        Assert.Equal(2, second.Entry.HistoryCount);
    }

    [Fact]
    public async Task PublishAsync_Throws_WhenSilverDraftDoesNotExist()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<SilverDraftNotFoundException>(() =>
            service.PublishAsync(Guid.NewGuid(), "publisher"));
    }

    private static async Task<PublishedEntrySeed> CreatePublishedEntryAsync(McpKnowledgeDbContext dbContext)
    {
        var silverDraft = await CreateSilverDraftAsync(dbContext);
        var service = CreateService(dbContext);
        var published = await service.PublishAsync(silverDraft.Id, "publisher");

        return new PublishedEntrySeed(
            silverDraft.Id,
            published.Entry.Id,
            published.Entry.Tags);
    }

    private static async Task<SilverServerDraft> CreateSilverDraftAsync(McpKnowledgeDbContext dbContext)
    {
        var bronzeService = CreateBronzeService(dbContext);
        var silverService = CreateSilverService(dbContext);
        var bronze = await bronzeService.ImportAsync(new CreateBronzeSourceRequest(
            SourceType: "github-readme",
            SourceUri: "https://github.com/example/acme-mcp-server",
            RawContent: """
                # Acme MCP Server

                Acme MCP Server lets teams search docs and manage GitHub issues from any MCP client.

                ## Tools
                - search-docs: Search the internal knowledge base
                """,
            ImportedBy: "maintainer"));

        var organize = await silverService.OrganizeAsync(
            bronze.Source.Id,
            SilverOrganizeModes.SilverDraft);

        return organize.Draft;
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

    private static BronzeIngestionService CreateBronzeService(McpKnowledgeDbContext dbContext)
    {
        return new BronzeIngestionService(
            dbContext,
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 18, 8, 0, 0, TimeSpan.Zero)));
    }

    private static SilverDraftService CreateSilverService(McpKnowledgeDbContext dbContext)
    {
        return new SilverDraftService(
            dbContext,
            new RuleBasedSilverNormalizer(),
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 18, 9, 0, 0, TimeSpan.Zero)));
    }

    private static GoldCatalogService CreateService(
        McpKnowledgeDbContext dbContext,
        DateTimeOffset? utcNow = null)
    {
        return new GoldCatalogService(
            dbContext,
            new FixedTimeProvider(utcNow ?? new DateTimeOffset(2026, 5, 18, 10, 0, 0, TimeSpan.Zero)));
    }

    private sealed record PublishedEntrySeed(
        Guid SilverDraftId,
        Guid EntryId,
        IReadOnlyList<string> InitialTags);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}