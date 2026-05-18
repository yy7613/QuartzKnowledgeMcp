using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Persistence;
using QuartzKnowledgeMcp.Api.Silver;
using QuartzKnowledgeMcp.Tests.Infrastructure;

namespace QuartzKnowledgeMcp.Tests.Silver;

public class SilverDraftServiceTests
{
    [Fact]
    public async Task OrganizeAsync_Throws_WhenBronzeSourceDoesNotExist()
    {
        await using var connection = await KnowledgeStoreTestFixture.OpenConnectionAsync();
        await using var dbContext = await KnowledgeStoreTestFixture.CreateDbContextAsync(connection);
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<BronzeSourceNotFoundException>(() =>
            service.OrganizeAsync(Guid.NewGuid(), SilverOrganizeModes.SilverDraft));
    }

    [Fact]
    public async Task OrganizeAsync_CreatesAndListsSilverDraft_WhenBronzeExists()
    {
        await using var connection = await KnowledgeStoreTestFixture.OpenConnectionAsync();
        await using var dbContext = await KnowledgeStoreTestFixture.CreateDbContextAsync(connection);
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
        Assert.False(organize.UsedLlm);
        Assert.Equal(BronzeSourceStatuses.Organized, storedBronze.Status);
        Assert.Single(list.Items);
        Assert.NotNull(detail);
        Assert.Equal("Acme MCP Server", detail.Name);
        Assert.Single(detail.ToolDrafts);
        Assert.Equal(bronze.Source.Id, detail.BronzeSourceId);
    }

    [Fact]
    public async Task OrganizeAsync_Preview_DoesNotPersistChanges()
    {
        await using var connection = await KnowledgeStoreTestFixture.OpenConnectionAsync();
        await using var dbContext = await KnowledgeStoreTestFixture.CreateDbContextAsync(connection);
        var bronzeService = CreateBronzeService(dbContext);
        var silverService = CreateService(dbContext);
        var bronze = await bronzeService.ImportAsync(new CreateBronzeSourceRequest(
            SourceType: "github-readme",
            SourceUri: "https://github.com/example/acme-mcp-server-preview",
            RawContent: """
                # Preview MCP Server

                Preview MCP Server organizes documentation for local testing.

                ## Tools
                - preview-docs: Preview normalized documentation
                """,
            ImportedBy: "maintainer"));

        var preview = await silverService.OrganizeAsync(
            bronze.Source.Id,
            SilverOrganizeModes.SilverDraft,
            useLlm: true,
            preview: true);
        var storedBronze = await dbContext.BronzeSources.SingleAsync();

        Assert.True(preview.Preview);
        Assert.False(preview.UsedLlm);
        Assert.Equal(BronzeSourceStatuses.Imported, storedBronze.Status);
        Assert.Empty(await dbContext.SilverServerDrafts.ToListAsync());
        Assert.Equal("Preview MCP Server", preview.Draft.Name);
        Assert.Single(preview.Draft.ToolDrafts);
    }

    [Fact]
    public async Task OrganizeAsync_FallsBackToRuleBased_WhenLlmIsRequested()
    {
        await using var connection = await KnowledgeStoreTestFixture.OpenConnectionAsync();
        await using var dbContext = await KnowledgeStoreTestFixture.CreateDbContextAsync(connection);
        var bronzeService = CreateBronzeService(dbContext);
        var silverService = CreateService(dbContext);
        var bronze = await bronzeService.ImportAsync(new CreateBronzeSourceRequest(
            SourceType: "github-readme",
            SourceUri: "https://github.com/example/acme-mcp-server-llm-fallback",
            RawContent: "# LLM Fallback Server\n\nUsed to validate rule-based fallback.\n\n## Tools\n- search-docs: Search docs",
            ImportedBy: "maintainer"));

        var organize = await silverService.OrganizeAsync(
            bronze.Source.Id,
            SilverOrganizeModes.SilverDraft,
            useLlm: true);

        Assert.True(organize.Created);
        Assert.False(organize.UsedLlm);
        Assert.False(organize.Preview);
    }

    [Fact]
    public async Task OrganizeAsync_UpdatesExistingDraft_WhenBronzeAlreadyOrganized()
    {
        await using var connection = await KnowledgeStoreTestFixture.OpenConnectionAsync();
        await using var dbContext = await KnowledgeStoreTestFixture.CreateDbContextAsync(connection);
        var bronzeService = CreateBronzeService(dbContext);
        var silverService = CreateService(dbContext);
        var bronze = await bronzeService.ImportAsync(new CreateBronzeSourceRequest(
            SourceType: "github-readme",
            SourceUri: "https://github.com/example/acme-mcp-server-repeat",
            RawContent: """
                # Acme MCP Server

                Acme MCP Server lets teams search docs and manage GitHub issues from any MCP client.

                ## Tools
                - search-docs: Search the internal knowledge base
                - sync-issues: Sync issue state to GitHub
                """,
            ImportedBy: "maintainer"));

        var first = await silverService.OrganizeAsync(bronze.Source.Id, SilverOrganizeModes.SilverDraft);
        var second = await silverService.OrganizeAsync(bronze.Source.Id, SilverOrganizeModes.SilverDraft);
        var stored = await dbContext.SilverServerDrafts
            .Include(draft => draft.ToolDrafts)
            .SingleAsync();

        Assert.True(first.Created);
        Assert.False(second.Created);
        Assert.Equal(first.Draft.Id, second.Draft.Id);
        Assert.Equal(2, stored.ToolDrafts.Count);
        Assert.Equal([0, 1], stored.ToolDrafts.OrderBy(toolDraft => toolDraft.Position).Select(toolDraft => toolDraft.Position).ToArray());
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
            KnowledgeStoreTestFixture.CreateKnowledgeRepository(dbContext),
            KnowledgeStoreTestFixture.CreateUnitOfWork(dbContext),
            new RuleBasedOrganizationAgent(new RuleBasedSilverNormalizer()),
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