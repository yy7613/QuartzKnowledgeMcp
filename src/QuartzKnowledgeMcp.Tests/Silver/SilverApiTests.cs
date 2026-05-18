using System.Net;
using System.Net.Http.Json;
using QuartzKnowledgeMcp.Tests.Infrastructure;

namespace QuartzKnowledgeMcp.Tests.Silver;

[Collection(ApiTestCollection.Name)]
public class SilverApiTests(ApiTestFactory factory)
{
    [Fact]
    public async Task OrganizeListAndDetail_ReturnExpectedSilverPayloads()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var bronzeId = await client.CreateBronzeSourceAsync(
            "https://github.com/example/silver-api-test",
            "# Silver API Test\n\nSilver API Test lets users search GitHub docs.\n\n## Tools\n- search-docs: Search docs");
        var silverId = await client.OrganizeBronzeSourceAsync(bronzeId);
        var list = await client.GetFromJsonAsync<SilverListPayload>("/api/silver/server-drafts");
        var detailResponse = await client.GetAsync($"/api/silver/server-drafts/{silverId}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<SilverDetailPayload>();

        Assert.NotNull(list);
        Assert.Single(list.Items);
        Assert.Equal(silverId, list.Items[0].Id);
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.NotNull(detail);
        Assert.Equal("Silver API Test", detail.Name);
        Assert.Single(detail.ToolDrafts);
        Assert.Contains("github", detail.TagCandidates);
        Assert.False(detail.UsedLlm);
        Assert.False(detail.Preview);
    }

    [Fact]
    public async Task OrganizePreview_ReturnsDraftWithoutPersisting()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var bronzeId = await client.CreateBronzeSourceAsync(
            "https://github.com/example/silver-api-preview-test",
            "# Silver Preview Test\n\nPreview should not persist.\n\n## Tools\n- preview-docs: Preview docs");

        var organizeResponse = await client.PostAsJsonAsync($"/api/bronze/sources/{bronzeId}:organize", new
        {
            mode = "silver-draft",
            useLlm = true,
            preview = true
        });
        var preview = await organizeResponse.Content.ReadFromJsonAsync<SilverDetailPayload>();
        var list = await client.GetFromJsonAsync<SilverListPayload>("/api/silver/server-drafts");
        var bronze = await client.GetFromJsonAsync<BronzeDetailPayload>($"/api/bronze/sources/{bronzeId}");

        Assert.Equal(HttpStatusCode.OK, organizeResponse.StatusCode);
        Assert.NotNull(preview);
        Assert.True(preview.Preview);
        Assert.False(preview.UsedLlm);
        Assert.NotNull(list);
        Assert.Empty(list.Items);
        Assert.NotNull(bronze);
        Assert.Equal("imported", bronze.Status);
    }

    private sealed record SilverListPayload(IReadOnlyList<SilverItemPayload> Items, int Page, int PageSize, int TotalCount);
    private sealed record SilverItemPayload(Guid Id, Guid BronzeSourceId, string Name, string Summary, IReadOnlyList<string> TagCandidates, int ToolCount, DateTime OrganizedAtUtc);
    private sealed record SilverDetailPayload(Guid Id, Guid BronzeSourceId, string Name, string Summary, IReadOnlyList<string> TagCandidates, IReadOnlyList<SilverToolPayload> ToolDrafts, DateTime OrganizedAtUtc, bool UsedLlm, bool Preview);
    private sealed record SilverToolPayload(Guid Id, string Name, string Description);
    private sealed record BronzeDetailPayload(Guid Id, string SourceType, string? SourceUri, string RawContent, string Status, string? ImportedBy, DateTime ImportedAtUtc);
}