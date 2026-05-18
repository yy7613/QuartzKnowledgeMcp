using System.Net;
using System.Net.Http.Json;
using QuartzKnowledgeMcp.Tests.Infrastructure;

namespace QuartzKnowledgeMcp.Tests.Gold;

[Collection(ApiTestCollection.Name)]
public class GoldApiTests(ApiTestFactory factory)
{
    [Fact]
    public async Task UpdateCatalog_ReturnsValidationProblem_ForMissingRequiredFields()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var ids = await client.CreatePublishedEntryAsync(
            "gold-update-invalid",
            "Gold Update Invalid",
            "Gold Update Invalid lets users search docs from GitHub.",
            "OAuth 2.0",
            "VS Code");

        var response = await client.PutAsJsonAsync($"/api/gold/catalog/{ids.GoldId}", new
        {
            overview = " ",
            setupGuide = " ",
            references = new[] { "https://example.dev/docs" },
            supportedClients = new[] { "VS Code" },
            updatedBy = "editor"
        });
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetailsPayload>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Contains("overview", problem.Errors.Keys);
        Assert.Contains("setupGuide", problem.Errors.Keys);
    }

    [Fact]
    public async Task ReplaceTags_ReturnsValidationProblem_ForDuplicateTags()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var ids = await client.CreatePublishedEntryAsync(
            "gold-tags-invalid",
            "Gold Tags Invalid",
            "Gold Tags Invalid lets users search docs from GitHub.",
            "OAuth 2.0",
            "VS Code");

        var response = await client.PutAsJsonAsync($"/api/gold/catalog/{ids.GoldId}/tags", new
        {
            tags = new[] { "github", "GitHub" },
            updatedBy = "editor"
        });
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetailsPayload>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Contains("tags", problem.Errors.Keys);
    }

    [Fact]
    public async Task UpdateCatalog_AndReplaceTags_PersistChangesAndHistory()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var ids = await client.CreatePublishedEntryAsync(
            "gold-update-success",
            "Gold Update Success",
            "Gold Update Success lets users search docs from GitHub.",
            "OAuth 2.0",
            "VS Code");

        var updateResponse = await client.PutAsJsonAsync($"/api/gold/catalog/{ids.GoldId}", new
        {
            overview = "Updated overview",
            setupGuide = "1. Install\n2. Configure",
            references = new[] { "https://example.dev/docs" },
            supportedClients = new[] { "VS Code", "Claude Desktop" },
            updatedBy = "editor"
        });
        var tagResponse = await client.PutAsJsonAsync($"/api/gold/catalog/{ids.GoldId}/tags", new
        {
            tags = new[] { "github", "registry" },
            updatedBy = "editor"
        });
        var detail = await client.GetFromJsonAsync<GoldDetailPayload>($"/api/gold/catalog/{ids.GoldId}");
        var history = await client.GetFromJsonAsync<GoldHistoryPayload>($"/api/gold/catalog/{ids.GoldId}/history?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, tagResponse.StatusCode);
        Assert.NotNull(detail);
        Assert.Equal("Updated overview", detail.Overview);
        Assert.Equal(["github", "registry"], detail.Tags);
        Assert.Equal(["VS Code", "Claude Desktop"], detail.SupportedClients);
        Assert.NotNull(history);
        Assert.Equal(2, history.PageSize);
        Assert.Equal(3, history.TotalCount);
        Assert.Equal(2, history.Items.Count);
        Assert.Equal("tags-replaced", history.Items[0].Action);
    }

    [Fact]
    public async Task PublishDetailAndHistory_ReturnExpectedGoldPayloads()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var ids = await client.CreatePublishedEntryAsync(
            "gold-api-test",
            "Gold API Test",
            "Gold API Test lets users search docs from GitHub.",
            "OAuth 2.0",
            "VS Code");
        var detailResponse = await client.GetAsync($"/api/gold/catalog/{ids.GoldId}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<GoldDetailPayload>();
        var history = await client.GetFromJsonAsync<GoldHistoryPayload>($"/api/gold/catalog/{ids.GoldId}/history");

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.NotNull(detail);
        Assert.Equal("Gold API Test", detail.DisplayName);
        Assert.Equal("oauth", detail.AuthenticationType);
        Assert.Equal("VS Code", detail.SupportedClients.Single());
        Assert.NotNull(history);
        Assert.Single(history.Items);
        Assert.Equal("published", history.Items[0].Action);
    }

    private sealed record GoldDetailPayload(
        Guid Id,
        Guid SilverServerDraftId,
        string DisplayName,
        string Overview,
        IReadOnlyList<string> Tags,
        string AuthenticationType,
        string SetupGuide,
        IReadOnlyList<GoldToolPayload> ToolSummaries,
        IReadOnlyList<GoldReferencePayload> References,
        IReadOnlyList<string> SupportedClients,
        int HistoryCount,
        DateTime PublishedAtUtc,
        string PublishedBy,
        DateTime UpdatedAtUtc,
        string UpdatedBy);

    private sealed record GoldToolPayload(string Name, string Description);
    private sealed record GoldReferencePayload(string Label, string Url);
    private sealed record GoldHistoryPayload(IReadOnlyList<GoldHistoryItemPayload> Items, int Page, int PageSize, int TotalCount);
    private sealed record GoldHistoryItemPayload(Guid Id, string Action, string ChangedBy, DateTime ChangedAtUtc, string Summary, bool UsedLlm);
    private sealed record HttpValidationProblemDetailsPayload(IDictionary<string, string[]> Errors);
}