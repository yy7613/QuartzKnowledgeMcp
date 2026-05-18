using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuartzKnowledgeMcp.Api.Application;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Domain.Ports;
using QuartzKnowledgeMcp.Api.Gold;
using QuartzKnowledgeMcp.Api.Persistence;
using QuartzKnowledgeMcp.Api.Silver;

namespace QuartzKnowledgeMcp.Tests.Infrastructure;

internal static class KnowledgeStoreTestFixture
{
    public static IKnowledgeRepository CreateKnowledgeRepository(McpKnowledgeDbContext dbContext)
    {
        return new SqliteKnowledgeRepository(dbContext);
    }

    public static IHistoryRepository CreateHistoryRepository(McpKnowledgeDbContext dbContext)
    {
        return new SqliteHistoryRepository(dbContext);
    }

    public static IUnitOfWork CreateUnitOfWork(McpKnowledgeDbContext dbContext)
    {
        return new EfUnitOfWork(dbContext);
    }

    public static async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    public static async Task<McpKnowledgeDbContext> CreateDbContextAsync(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<McpKnowledgeDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new McpKnowledgeDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        return dbContext;
    }

    public static BronzeIngestionService CreateBronzeService(McpKnowledgeDbContext dbContext)
    {
        return new BronzeIngestionService(
            dbContext,
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 19, 8, 0, 0, TimeSpan.Zero)));
    }

    public static SilverDraftService CreateSilverService(McpKnowledgeDbContext dbContext)
    {
        return new SilverDraftService(
            CreateKnowledgeRepository(dbContext),
            CreateUnitOfWork(dbContext),
            new RuleBasedOrganizationAgent(new RuleBasedSilverNormalizer()),
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 19, 9, 0, 0, TimeSpan.Zero)));
    }

    public static GoldCatalogService CreateGoldService(
        McpKnowledgeDbContext dbContext,
        DateTimeOffset? utcNow = null)
    {
        return new GoldCatalogService(
            CreateKnowledgeRepository(dbContext),
            CreateHistoryRepository(dbContext),
            CreateUnitOfWork(dbContext),
            new FixedTimeProvider(utcNow ?? new DateTimeOffset(2026, 5, 19, 10, 0, 0, TimeSpan.Zero)));
    }

    public static SilverDraftApplicationService CreateSilverApplicationService(McpKnowledgeDbContext dbContext)
    {
        return new SilverDraftApplicationService(CreateSilverService(dbContext));
    }

    public static CatalogCurationApplicationService CreateCatalogCurationApplicationService(
        McpKnowledgeDbContext dbContext,
        DateTimeOffset? utcNow = null)
    {
        return new CatalogCurationApplicationService(CreateGoldService(dbContext, utcNow));
    }

    public static async Task<SilverServerDraft> CreateSilverDraftAsync(McpKnowledgeDbContext dbContext)
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
                - sync-issues: Sync issue state to GitHub
                """,
            ImportedBy: "maintainer"));

        var organize = await silverService.OrganizeAsync(
            bronze.Source.Id,
            SilverOrganizeModes.SilverDraft);

        return organize.Draft;
    }

    public static async Task<PublishedEntrySeed> CreatePublishedEntryAsync(
        McpKnowledgeDbContext dbContext,
        DateTimeOffset? utcNow = null)
    {
        var silverDraft = await CreateSilverDraftAsync(dbContext);
        var appService = CreateCatalogCurationApplicationService(dbContext, utcNow);
        var published = await appService.PublishAsync(silverDraft.Id, new PublishSilverDraftRequest("publisher"));

        return new PublishedEntrySeed(
            silverDraft.Id,
            published.Entry.Id,
            published.Entry.Tags);
    }

    internal sealed record PublishedEntrySeed(
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