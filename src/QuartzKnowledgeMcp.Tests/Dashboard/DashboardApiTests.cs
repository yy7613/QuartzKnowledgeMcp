using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuartzKnowledgeMcp.Api.Persistence;
using QuartzKnowledgeMcp.Tests.Infrastructure;

namespace QuartzKnowledgeMcp.Tests.Dashboard;

[Collection(ApiTestCollection.Name)]
public class DashboardApiTests(ApiTestFactory factory)
{
    [Fact]
    public async Task GetSummary_ReturnsMedallionCountsFreshnessAndTags()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        await client.CreateBronzeSourceAsync(
            "https://github.com/example/dashboard-bronze-only",
            "# Dashboard Bronze Only\n\nBronze-only record for dashboard summary.");

        var silverBronzeId = await client.CreateBronzeSourceAsync(
            "https://github.com/example/dashboard-silver-only",
            "# Dashboard Silver Only\n\nSilver-only record for dashboard summary.\n\n## Tools\n- inspect-draft: Inspect draft");
        await client.OrganizeBronzeSourceAsync(silverBronzeId);

        await client.CreatePublishedEntryAsync(
            "dashboard-gold-summary",
            "Dashboard Gold Summary",
            "Dashboard Gold Summary helps users inspect medallion metrics.",
            "OAuth 2.0",
            "VS Code");

        var response = await client.GetAsync("/api/dashboard/summary?recentPerStage=5");
        var payload = await response.Content.ReadFromJsonAsync<DashboardSummaryPayload>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(3, payload.Bronze.TotalCount);
        Assert.Equal(2, payload.Silver.TotalCount);
        Assert.Equal(1, payload.Gold.TotalCount);
        Assert.True(payload.Bronze.Freshness.Last24Hours >= 3);
        Assert.Contains(payload.Bronze.Breakdown, item => item.Label == "imported" && item.Count == 1);
        Assert.Contains(payload.Bronze.Breakdown, item => item.Label == "organized" && item.Count == 2);
        Assert.True(payload.Tags.UniqueCount > 0);
        Assert.NotEmpty(payload.Gold.RecentItems);
        Assert.Equal(7, payload.Gold.Trend.Count);
        Assert.Contains(payload.Gold.RecentItems, item => item.DetailPath.StartsWith("/api/gold/catalog/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Search_ReturnsCrossMedallionMatches_AndSupportsStageFilter()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        await client.CreatePublishedEntryAsync(
            "dashboard-cross-stage",
            "Dashboard Cross Stage",
            "Dashboard Cross Stage helps users inspect medallion dashboards.",
            "OAuth 2.0",
            "VS Code");

        var response = await client.GetAsync("/api/dashboard/search?q=Dashboard%20Cross%20Stage&limit=10");
        var payload = await response.Content.ReadFromJsonAsync<DashboardSearchPayload>();
        var goldOnlyResponse = await client.GetAsync("/api/dashboard/search?q=Dashboard%20Cross%20Stage&stage=gold&limit=10");
        var goldOnlyPayload = await goldOnlyResponse.Content.ReadFromJsonAsync<DashboardSearchPayload>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Contains(payload.Items, item => item.Stage == "bronze");
        Assert.Contains(payload.Items, item => item.Stage == "silver");
        Assert.Contains(payload.Items, item => item.Stage == "gold");

        Assert.Equal(HttpStatusCode.OK, goldOnlyResponse.StatusCode);
        Assert.NotNull(goldOnlyPayload);
        Assert.NotEmpty(goldOnlyPayload.Items);
        Assert.All(goldOnlyPayload.Items, item => Assert.Equal("gold", item.Stage));
    }

    [Fact]
    public async Task DashboardPage_ReturnsHtmlShell()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/dashboard/index.html");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ナレッジ運用ダッシュボード", html);
        Assert.DoesNotContain("検索、メダリオン別の推移、Gold の履歴と関連情報を PC 画面で一目に追える、Azure portal 風の観測ビューです。", html);
        Assert.Contains("/api/dashboard/summary", html);
        Assert.Contains("/api/dashboard/search", html);
        Assert.Contains("freshness-filter", html);
        Assert.Contains("search-sort", html);
        Assert.Contains("dashboard-tablist", html);
        Assert.Contains("dashboard-tab-analytics", html);
        Assert.Contains("dashboard-tab-inspect", html);
        Assert.Contains("trend-window-toggle", html);
        Assert.Contains("gold-inspector-panel", html);
        Assert.Contains("result-preview-dialog", html);
    }

    [Fact]
    public async Task DashboardRedirect_PreservesQueryString()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/dashboard?q=Browser%20Registry&stage=gold&tag=registry&freshness=24h&sort=newest&inspect=123");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal(
            "/dashboard/index.html?q=Browser%20Registry&stage=gold&tag=registry&freshness=24h&sort=newest&inspect=123",
            response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task DashboardEndpoints_UseDefaultQueryValues_WhenOptionalParametersAreOmitted()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        await client.CreatePublishedEntryAsync(
            "dashboard-default-query-values",
            "Dashboard Default Query Values",
            "Dashboard Default Query Values verifies optional dashboard query parameters.",
            "OAuth 2.0",
            "VS Code");

        var summaryResponse = await client.GetAsync("/api/dashboard/summary");
        var searchResponse = await client.GetAsync("/api/dashboard/search?q=Dashboard%20Default%20Query%20Values");
        var searchPayload = await searchResponse.Content.ReadFromJsonAsync<DashboardSearchPayload>();

        Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        Assert.NotNull(searchPayload);
        Assert.NotEmpty(searchPayload.Items);
    }

    [Fact]
    public async Task Search_SupportsTagFreshnessAndSort_AndIncludesDetailPaths()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var oldEntry = await client.CreatePublishedEntryAsync(
            "dashboard-old-entry",
            "Dashboard Old Entry",
            "Dashboard Old Entry verifies dashboard search filters.",
            "OAuth 2.0",
            "VS Code");
        var newEntry = await client.CreatePublishedEntryAsync(
            "dashboard-new-entry",
            "Dashboard New Entry",
            "Dashboard New Entry verifies dashboard search filters.",
            "OAuth 2.0",
            "VS Code");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<McpKnowledgeDbContext>();
            var oldGold = await dbContext.GoldCatalogEntries.SingleAsync(entry => entry.Id == oldEntry.GoldId);
            var newGold = await dbContext.GoldCatalogEntries.SingleAsync(entry => entry.Id == newEntry.GoldId);

            oldGold.TagsJson = JsonSerializer.Serialize(new[] { "dashboard", "common", "old" });
            oldGold.UpdatedAtUtc = DateTime.UtcNow.AddDays(-10);
            newGold.TagsJson = JsonSerializer.Serialize(new[] { "dashboard", "common", "new" });
            newGold.UpdatedAtUtc = DateTime.UtcNow.AddHours(-1);

            await dbContext.SaveChangesAsync();
        }

        var oldestResponse = await client.GetAsync("/api/dashboard/search?stage=gold&tag=common&sort=oldest&limit=10");
        var oldestPayload = await oldestResponse.Content.ReadFromJsonAsync<DashboardSearchPayload>();
        var freshnessResponse = await client.GetAsync("/api/dashboard/search?stage=gold&tag=common&freshness=24h&sort=newest&limit=10");
        var freshnessPayload = await freshnessResponse.Content.ReadFromJsonAsync<DashboardSearchPayload>();

        Assert.Equal(HttpStatusCode.OK, oldestResponse.StatusCode);
        Assert.NotNull(oldestPayload);
        Assert.Equal("common", oldestPayload.Tag);
        Assert.Equal("oldest", oldestPayload.Sort);
        Assert.Equal(2, oldestPayload.TotalCount);
        Assert.Equal(oldEntry.GoldId, oldestPayload.Items[0].Id);
        Assert.Equal($"/api/gold/catalog/{oldEntry.GoldId}", oldestPayload.Items[0].DetailPath);

        Assert.Equal(HttpStatusCode.OK, freshnessResponse.StatusCode);
        Assert.NotNull(freshnessPayload);
        Assert.Equal("24h", freshnessPayload.Freshness);
        Assert.Single(freshnessPayload.Items);
        Assert.Equal(newEntry.GoldId, freshnessPayload.Items[0].Id);
        Assert.Equal("24h", freshnessPayload.Items[0].FreshnessBucket);
    }

    private sealed record DashboardSummaryPayload(
        DateTime GeneratedAtUtc,
        DashboardOverviewPayload Overview,
        DashboardStagePayload Bronze,
        DashboardStagePayload Silver,
        DashboardStagePayload Gold,
        DashboardTagsPayload Tags);

    private sealed record DashboardOverviewPayload(int TotalObjects, int UniqueTagCount, DateTime? LatestActivityAtUtc);
    private sealed record DashboardStagePayload(
        string Stage,
        int TotalCount,
        DateTime? LatestActivityAtUtc,
        DashboardFreshnessPayload Freshness,
        IReadOnlyList<DashboardCountPayload> Breakdown,
        IReadOnlyList<DashboardTrendPointPayload> Trend,
        IReadOnlyList<DashboardRecentItemPayload> RecentItems);
    private sealed record DashboardFreshnessPayload(int Last24Hours, int Last7Days, int Older);
    private sealed record DashboardCountPayload(string Label, int Count);
    private sealed record DashboardTrendPointPayload(string Day, int Count);
    private sealed record DashboardRecentItemPayload(Guid Id, string Title, string Subtitle, string State, IReadOnlyList<string> Tags, DateTime TimestampUtc, string DetailPath);
    private sealed record DashboardTagsPayload(int UniqueCount, IReadOnlyList<DashboardCountPayload> Items);

    private sealed record DashboardSearchPayload(string Query, string? Stage, string? Tag, string? Freshness, string Sort, int TotalCount, IReadOnlyList<DashboardSearchItemPayload> Items);
    private sealed record DashboardSearchItemPayload(Guid Id, string Stage, string Title, string Summary, string State, IReadOnlyList<string> Tags, DateTime TimestampUtc, string DetailPath, string FreshnessBucket);
}