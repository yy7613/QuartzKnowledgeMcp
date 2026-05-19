using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using QuartzKnowledgeMcp.Tests.Infrastructure;

namespace QuartzKnowledgeMcp.Tests.Workflows;

[Collection(ApiTestCollection.Name)]
public class DashboardWorkflowApiTests(ApiTestFactory factory)
{
    [Fact]
    public async Task DashboardEndpoints_ExerciseJapaneseSearchAndInspectorFlow()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var primary = await client.CreatePublishedEntryAsync(
            "jp-dashboard-incident",
            "障害対応レジストリ サーバー",
            "日本語の障害対応手順とレジストリ検索を提供します。",
            "OAuth 2.0",
            "VS Code");
        var related = await client.CreatePublishedEntryAsync(
            "jp-dashboard-sync",
            "障害対応同期 サーバー",
            "日本語の同期手順と監査ログ確認を提供します。",
            "OAuth 2.0",
            "VS Code");

        var primaryUpdateResponse = await client.PutAsJsonAsync($"/api/gold/catalog/{primary.GoldId}", new
        {
            overview = "日本語の障害対応、レジストリ確認、運用引き継ぎを支援するサーバーです。",
            setupGuide = "1. 接続\n2. 認証\n3. 検索",
            references = new[] { "https://example.dev/jp/incident-guide" },
            supportedClients = new[] { "VS Code", "Claude Desktop" },
            updatedBy = "dashboard-e2e"
        });
        var relatedUpdateResponse = await client.PutAsJsonAsync($"/api/gold/catalog/{related.GoldId}", new
        {
            overview = "日本語の障害対応、同期監査、ログ確認を支援するサーバーです。",
            setupGuide = "1. 接続\n2. 認証\n3. 同期確認",
            references = new[] { "https://example.dev/jp/sync-guide" },
            supportedClients = new[] { "VS Code", "Claude Desktop" },
            updatedBy = "dashboard-e2e"
        });
        var primaryTagsResponse = await client.PutAsJsonAsync($"/api/gold/catalog/{primary.GoldId}/tags", new
        {
            tags = new[] { "運用", "日本語", "レジストリ" },
            updatedBy = "dashboard-e2e"
        });
        var relatedTagsResponse = await client.PutAsJsonAsync($"/api/gold/catalog/{related.GoldId}/tags", new
        {
            tags = new[] { "運用", "日本語", "同期" },
            updatedBy = "dashboard-e2e"
        });

        Assert.Equal(HttpStatusCode.OK, primaryUpdateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, relatedUpdateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, primaryTagsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, relatedTagsResponse.StatusCode);

        var summary = await client.GetFromJsonAsync<JsonElement>("/api/dashboard/summary?recentPerStage=5");
        var search = await client.GetFromJsonAsync<JsonElement>("/api/dashboard/search?q=障害対応&stage=gold&tag=運用&freshness=24h&sort=title&limit=10");
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/gold/catalog/{primary.GoldId}");
        var history = await client.GetFromJsonAsync<JsonElement>($"/api/gold/catalog/{primary.GoldId}/history?page=1&pageSize=10");
        var relatedEntries = await client.GetFromJsonAsync<JsonElement>($"/api/gold/catalog/{primary.GoldId}/related?limit=5");

        Assert.Equal(2, summary.GetProperty("gold").GetProperty("totalCount").GetInt32());
        Assert.Equal(2, search.GetProperty("totalCount").GetInt32());

        var searchIds = search.GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid())
            .ToArray();

        Assert.Contains(primary.GoldId, searchIds);
        Assert.Contains(related.GoldId, searchIds);
        Assert.Equal("障害対応レジストリ サーバー", detail.GetProperty("displayName").GetString());
        Assert.True(history.GetProperty("totalCount").GetInt32() >= 3);

        var relatedIds = relatedEntries.GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid())
            .ToArray();

        Assert.Contains(related.GoldId, relatedIds);
    }
}
