using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Persistence;
using QuartzKnowledgeMcp.Api.Silver;

namespace QuartzKnowledgeMcp.Tests.Silver;

public class SilverDraftServiceTests
{
    [Fact]
    public async Task OrganizeAsync_Throws_WhenBronzeSourceDoesNotExist()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<BronzeSourceNotFoundException>(() =>
            service.OrganizeAsync(Guid.NewGuid(), SilverOrganizeModes.SilverDraft));
    }

    [Fact]
    public async Task OrganizeAsync_CreatesAndListsSilverDraft_WhenBronzeExists()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var bronzeService = CreateBronzeService(dbContext);
        var silverService = CreateService(dbContext);
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
        var list = await silverService.ListAsync();
        var detail = await silverService.GetDetailAsync(organize.Draft.Id);
        var storedBronze = await dbContext.BronzeSources.SingleAsync();

        Assert.True(organize.Created);
        Assert.Equal(BronzeSourceStatuses.Organized, storedBronze.Status);
        Assert.Single(list.Items);
        Assert.NotNull(detail);
        Assert.Equal("Acme MCP Server", detail.Name);
        Assert.Single(detail.ToolDrafts);
        Assert.Equal(bronze.Source.Id, detail.BronzeSourceId);
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

    private static SilverDraftService CreateService(McpKnowledgeDbContext dbContext)
    {
        return new SilverDraftService(
            dbContext,
            new RuleBasedSilverNormalizer(),
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 18, 9, 0, 0, TimeSpan.Zero)));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}