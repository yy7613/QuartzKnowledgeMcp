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

    private static GoldCatalogService CreateService(McpKnowledgeDbContext dbContext)
    {
        return new GoldCatalogService(
            dbContext,
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 18, 10, 0, 0, TimeSpan.Zero)));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}