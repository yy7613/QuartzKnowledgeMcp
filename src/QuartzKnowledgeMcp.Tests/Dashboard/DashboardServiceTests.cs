using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Dashboard;
using QuartzKnowledgeMcp.Api.Silver;
using QuartzKnowledgeMcp.Tests.Infrastructure;

namespace QuartzKnowledgeMcp.Tests.Dashboard;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_IncludesJapaneseTagsAndRecentGoldItems()
    {
        await using var connection = await KnowledgeStoreTestFixture.OpenConnectionAsync();
        await using var dbContext = await KnowledgeStoreTestFixture.CreateDbContextAsync(connection);

        var published = await KnowledgeStoreTestFixture.CreatePublishedEntryAsync(
            dbContext,
            new DateTimeOffset(2026, 5, 19, 9, 0, 0, TimeSpan.Zero));

        var goldEntry = await dbContext.GoldCatalogEntries.SingleAsync(entry => entry.Id == published.EntryId);
        goldEntry.DisplayName = "障害対応ナレッジ サーバー";
        goldEntry.Overview = "日本語の運用手順とレジストリ検索をまとめたサーバーです。";
        goldEntry.TagsJson = JsonSerializer.Serialize(new[] { "運用", "障害対応", "日本語" });
        goldEntry.UpdatedAtUtc = new DateTime(2026, 5, 19, 9, 30, 0, DateTimeKind.Utc);
        await dbContext.SaveChangesAsync();

        var service = new DashboardService(
            dbContext,
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero)));

        var summary = await service.GetSummaryAsync(recentPerStage: 5);

        Assert.Equal(7, summary.Gold.Trend.Count);
        Assert.Contains(summary.Tags.Items, item => item.Label == "運用" && item.Count == 1);
        Assert.Contains(summary.Gold.RecentItems, item =>
            item.Title == "障害対応ナレッジ サーバー" &&
            item.DetailPath == $"/api/gold/catalog/{published.EntryId}");
    }

    [Fact]
    public async Task SearchAsync_SupportsJapaneseTagBrowse_AndTitleSort()
    {
        await using var connection = await KnowledgeStoreTestFixture.OpenConnectionAsync();
        await using var dbContext = await KnowledgeStoreTestFixture.CreateDbContextAsync(connection);

        var firstId = await CreatePublishedEntryAsync(
            dbContext,
            slug: "dashboard-service-first",
            name: "01 日本語運用ガイド",
            summary: "日本語の運用手順と監査の流れをまとめたガイドです。",
            publishedAtUtc: new DateTimeOffset(2026, 5, 19, 10, 0, 0, TimeSpan.Zero));
        var secondId = await CreatePublishedEntryAsync(
            dbContext,
            slug: "dashboard-service-second",
            name: "02 日本語障害対応",
            summary: "日本語の障害対応とレジストリ確認手順をまとめたガイドです。",
            publishedAtUtc: new DateTimeOffset(2026, 5, 19, 10, 30, 0, TimeSpan.Zero));

        var entries = await dbContext.GoldCatalogEntries
            .Where(entry => entry.Id == firstId || entry.Id == secondId)
            .OrderBy(entry => entry.Id)
            .ToListAsync();

        var firstEntry = entries.Single(entry => entry.Id == firstId);
        firstEntry.TagsJson = JsonSerializer.Serialize(new[] { "運用", "日本語", "監査" });
        firstEntry.UpdatedAtUtc = new DateTime(2026, 5, 19, 10, 40, 0, DateTimeKind.Utc);

        var secondEntry = entries.Single(entry => entry.Id == secondId);
        secondEntry.TagsJson = JsonSerializer.Serialize(new[] { "運用", "日本語", "レジストリ" });
        secondEntry.UpdatedAtUtc = new DateTime(2026, 5, 19, 11, 10, 0, DateTimeKind.Utc);

        await dbContext.SaveChangesAsync();

        var service = new DashboardService(
            dbContext,
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero)));

        var result = await service.SearchAsync(
            query: null,
            stage: "gold",
            tag: "日本語",
            freshness: "24h",
            sort: "title",
            limit: 10);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal("title", result.Sort);
        Assert.Equal(["01 日本語運用ガイド", "02 日本語障害対応"], result.Items.Select(item => item.Title).ToArray());
        Assert.All(result.Items, item => Assert.Equal("gold", item.Stage));
        Assert.All(result.Items, item => Assert.Equal("24h", item.FreshnessBucket));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }

    private static async Task<Guid> CreatePublishedEntryAsync(
        QuartzKnowledgeMcp.Api.Persistence.McpKnowledgeDbContext dbContext,
        string slug,
        string name,
        string summary,
        DateTimeOffset publishedAtUtc)
    {
        var bronzeService = KnowledgeStoreTestFixture.CreateBronzeService(dbContext);
        var silverService = KnowledgeStoreTestFixture.CreateSilverService(dbContext);
        var goldService = KnowledgeStoreTestFixture.CreateGoldService(dbContext, utcNow: publishedAtUtc);

        var bronze = await bronzeService.ImportAsync(new CreateBronzeSourceRequest(
            SourceType: "github-readme",
            SourceUri: $"https://github.com/example/{slug}",
            RawContent: $"# {name}\n\n{summary}\n\nAuthentication: OAuth 2.0\n\nSupported clients: VS Code\n\n## Tools\n- search-docs: 日本語ドキュメントを検索する",
            ImportedBy: "dashboard-service-test"));
        var silver = await silverService.OrganizeAsync(bronze.Source.Id, SilverOrganizeModes.SilverDraft);
        var published = await goldService.PublishAsync(silver.Draft.Id, "dashboard-service-test");
        return published.Entry.Id;
    }
}
