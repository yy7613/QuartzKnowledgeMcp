using System.Net;
using System.Net.Http.Json;
using QuartzKnowledgeMcp.Tests.Infrastructure;

namespace QuartzKnowledgeMcp.Tests.Bronze;

[Collection(ApiTestCollection.Name)]
public class BronzeApiTests(ApiTestFactory factory)
{
    [Fact]
    public async Task CreateListAndDetail_ReturnExpectedBronzePayloads()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var bronzeId = await client.CreateBronzeSourceAsync(
            "https://github.com/example/bronze-api-test",
            "# Bronze API Test\n\nBronze API Test raw content.");
        var list = await client.GetFromJsonAsync<BronzeListPayload>("/api/bronze/sources");
        var detailResponse = await client.GetAsync($"/api/bronze/sources/{bronzeId}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<BronzeDetailPayload>();

        Assert.NotNull(list);
        Assert.Single(list.Items);
        Assert.Equal(bronzeId, list.Items[0].Id);
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.NotNull(detail);
        Assert.Equal("github-readme", detail.SourceType);
        Assert.Equal("imported", detail.Status);
    }

    private sealed record BronzeListPayload(IReadOnlyList<BronzeItemPayload> Items, int Page, int PageSize, int TotalCount);
    private sealed record BronzeItemPayload(Guid Id, string SourceType, string? SourceUri, string Status, DateTime ImportedAtUtc);
    private sealed record BronzeDetailPayload(Guid Id, string SourceType, string? SourceUri, string RawContent, string Status, string? ImportedBy, DateTime ImportedAtUtc);
}