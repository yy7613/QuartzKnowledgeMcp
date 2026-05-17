using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Persistence;

namespace QuartzKnowledgeMcp.Tests.Bronze;

public class BronzeIngestionServiceTests
{
    [Fact]
    public async Task ImportAsync_RejectsMissingRequiredFields()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<BronzeValidationException>(() =>
            service.ImportAsync(new CreateBronzeSourceRequest(
                SourceType: "manual",
                SourceUri: null,
                RawContent: " ",
                ImportedBy: null)));

        Assert.Contains("rawContent", exception.Errors.Keys);
    }

    [Fact]
    public async Task ImportAsync_RejectsUnsupportedSourceType()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<BronzeValidationException>(() =>
            service.ImportAsync(new CreateBronzeSourceRequest(
                SourceType: "rss-feed",
                SourceUri: null,
                RawContent: "# Example",
                ImportedBy: null)));

        Assert.Contains("sourceType", exception.Errors.Keys);
    }

    [Fact]
    public async Task ImportAsync_ReturnsExistingSource_WhenSourceUriAndContentMatch()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = CreateService(dbContext);
        var request = new CreateBronzeSourceRequest(
            SourceType: "manual",
            SourceUri: " https://github.com/example/mcp-server ",
            RawContent: "# Example MCP Server",
            ImportedBy: "yuki");

        var first = await service.ImportAsync(request);
        var second = await service.ImportAsync(request);
        var sourceCount = await dbContext.BronzeSources.CountAsync();

        Assert.True(first.Created);
        Assert.False(second.Created);
        Assert.Equal(first.Source.Id, second.Source.Id);
        Assert.Equal(1, sourceCount);
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsSource_WhenIdExists()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = CreateService(dbContext);
        var import = await service.ImportAsync(new CreateBronzeSourceRequest(
            SourceType: "github-readme",
            SourceUri: "https://github.com/example/readme",
            RawContent: "# README",
            ImportedBy: "maintainer"));

        var detail = await service.GetDetailAsync(import.Source.Id);

        Assert.NotNull(detail);
        Assert.Equal(import.Source.Id, detail.Id);
        Assert.Equal("# README", detail.RawContent);
        Assert.Equal("github-readme", detail.SourceType);
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsNull_WhenIdDoesNotExist()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = CreateService(dbContext);

        var detail = await service.GetDetailAsync(Guid.NewGuid());

        Assert.Null(detail);
    }

    [Fact]
    public async Task ListAsync_ReturnsPagedSources_WithOptionalStatusFilter()
    {
        await using var connection = await OpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = CreateService(dbContext);
        var imported = await service.ImportAsync(new CreateBronzeSourceRequest(
            SourceType: "manual",
            SourceUri: "https://example.dev/one",
            RawContent: "one",
            ImportedBy: null));
        var errored = await service.ImportAsync(new CreateBronzeSourceRequest(
            SourceType: "manual",
            SourceUri: "https://example.dev/two",
            RawContent: "two",
            ImportedBy: null));
        errored.Source.Status = "error";
        dbContext.BronzeSources.Update(errored.Source);
        await dbContext.SaveChangesAsync();

        var response = await service.ListAsync(
            page: 0,
            pageSize: 0,
            status: " imported ");

        Assert.Equal(1, response.Page);
        Assert.Equal(1, response.PageSize);
        Assert.Equal(1, response.TotalCount);
        Assert.Equal(imported.Source.Id, response.Items.Single().Id);
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

    private static BronzeIngestionService CreateService(McpKnowledgeDbContext dbContext)
    {
        return new BronzeIngestionService(
            dbContext,
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 17, 13, 0, 0, TimeSpan.Zero)));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
