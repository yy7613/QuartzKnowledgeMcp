using System.Net;
using System.Net.Http.Json;
using QuartzKnowledgeMcp.Tests.Infrastructure;

namespace QuartzKnowledgeMcp.Tests.Capabilities;

[Collection(ApiTestCollection.Name)]
public class SystemApiTests(ApiTestFactory factory)
{
    [Fact]
    public async Task GetCapabilities_ReturnsRuntimeFlags()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/system/capabilities");
        var payload = await response.Content.ReadFromJsonAsync<SystemCapabilitiesPayload>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("sqlite", payload.KnowledgeStore.Provider);
        Assert.False(payload.Llm.Enabled);
        Assert.Equal("none", payload.Llm.Provider);
        Assert.False(payload.Embedding.Enabled);
        Assert.True(payload.Search.SupportsStructuredSearch);
        Assert.True(payload.Search.SupportsSuggestions);
        Assert.True(payload.Search.SupportsFacets);
        Assert.False(payload.Search.SupportsRelatedEntries);
        Assert.False(payload.Search.SupportsSemanticSearch);
    }

    private sealed record SystemCapabilitiesPayload(
        KnowledgeStorePayload KnowledgeStore,
        LlmPayload Llm,
        EmbeddingPayload Embedding,
        SearchPayload Search);

    private sealed record KnowledgeStorePayload(string Provider, bool Replaceable);
    private sealed record LlmPayload(bool Enabled, string Provider, string Model, bool Replaceable);
    private sealed record EmbeddingPayload(bool Enabled, string Provider, bool Replaceable);
    private sealed record SearchPayload(bool SupportsStructuredSearch, bool SupportsSuggestions, bool SupportsFacets, bool SupportsRelatedEntries, bool SupportsSemanticSearch);
}