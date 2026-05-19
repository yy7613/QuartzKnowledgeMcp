using Microsoft.EntityFrameworkCore;
using QuartzKnowledgeMcp.Api.Gold;
using QuartzKnowledgeMcp.Api.Persistence;
using QuartzKnowledgeMcp.Tests.Infrastructure;

namespace QuartzKnowledgeMcp.Tests.Application;

public class CatalogCurationApplicationServiceTests
{
    [Fact]
    public async Task PublishAsync_CreatesGoldEntry_FromRequestBoundary()
    {
        await using var connection = await KnowledgeStoreTestFixture.OpenConnectionAsync();
        await using var dbContext = await KnowledgeStoreTestFixture.CreateDbContextAsync(connection);
        var silverDraft = await KnowledgeStoreTestFixture.CreateSilverDraftAsync(dbContext);
        var service = KnowledgeStoreTestFixture.CreateCatalogCurationApplicationService(dbContext);

        var result = await service.PublishAsync(
            silverDraft.Id,
            new PublishSilverDraftRequest("app-publisher"));
        var storedEntry = await dbContext.GoldCatalogEntries.SingleAsync();

        Assert.True(result.Created);
        Assert.Equal("app-publisher", storedEntry.PublishedBy);
        Assert.Equal(silverDraft.Id, result.Entry.SilverServerDraftId);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntry_ThroughApplicationBoundary()
    {
        await using var connection = await KnowledgeStoreTestFixture.OpenConnectionAsync();
        await using var dbContext = await KnowledgeStoreTestFixture.CreateDbContextAsync(connection);
        var published = await KnowledgeStoreTestFixture.CreatePublishedEntryAsync(dbContext);
        var service = KnowledgeStoreTestFixture.CreateCatalogCurationApplicationService(
            dbContext,
            utcNow: new DateTimeOffset(2026, 5, 19, 11, 0, 0, TimeSpan.Zero));

        var result = await service.UpdateAsync(
            published.EntryId,
            new UpdateGoldCatalogEntryRequest(
                "Updated by application service",
                "1. Install\n2. Configure",
                ["https://example.dev/application"],
                ["VS Code", "Claude Desktop"],
                "app-editor"));

        Assert.Equal("Updated by application service", result.Overview);
        Assert.Equal(["VS Code", "Claude Desktop"], result.SupportedClients);
        Assert.Equal(2, result.HistoryCount);
    }

    [Fact]
    public async Task ReplaceTagsAsync_RejectsNullTags_ThroughApplicationBoundary()
    {
        await using var connection = await KnowledgeStoreTestFixture.OpenConnectionAsync();
        await using var dbContext = await KnowledgeStoreTestFixture.CreateDbContextAsync(connection);
        var published = await KnowledgeStoreTestFixture.CreatePublishedEntryAsync(dbContext);
        var service = KnowledgeStoreTestFixture.CreateCatalogCurationApplicationService(dbContext);

        var exception = await Assert.ThrowsAsync<GoldValidationException>(() =>
            service.ReplaceTagsAsync(
                published.EntryId,
                new ReplaceGoldCatalogTagsRequest(null, "app-editor")));

        Assert.Contains("tags", exception.Errors.Keys);
    }
}