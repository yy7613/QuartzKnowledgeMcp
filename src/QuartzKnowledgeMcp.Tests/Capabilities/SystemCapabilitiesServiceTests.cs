using Microsoft.Extensions.Configuration;
using QuartzKnowledgeMcp.Api.Capabilities;
using QuartzKnowledgeMcp.Tests.Infrastructure;

namespace QuartzKnowledgeMcp.Tests.Capabilities;

public class SystemCapabilitiesServiceTests
{
    [Fact]
    public void GetCapabilities_ReturnsConfiguredProviders_AndSearchFlags()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Llm:Enabled"] = "true",
                ["Llm:Provider"] = "foundry",
                ["Llm:Model"] = "configured-at-runtime",
                ["Embedding:Enabled"] = "false",
                ["Embedding:Provider"] = "none"
            })
            .Build();
        var service = new SystemCapabilitiesService(
            configuration,
            new FakeUnitOfWork("Microsoft.EntityFrameworkCore.Sqlite"));

        var capabilities = service.GetCapabilities();

        Assert.Equal("sqlite", capabilities.KnowledgeStore.Provider);
        Assert.True(capabilities.KnowledgeStore.Replaceable);
        Assert.True(capabilities.Llm.Enabled);
        Assert.Equal("foundry", capabilities.Llm.Provider);
        Assert.Equal("configured-at-runtime", capabilities.Llm.Model);
        Assert.False(capabilities.Embedding.Enabled);
        Assert.Equal("none", capabilities.Embedding.Provider);
        Assert.True(capabilities.Search.SupportsStructuredSearch);
        Assert.True(capabilities.Search.SupportsSuggestions);
        Assert.True(capabilities.Search.SupportsFacets);
        Assert.False(capabilities.Search.SupportsRelatedEntries);
        Assert.False(capabilities.Search.SupportsSemanticSearch);
    }

    [Fact]
    public void GetCapabilities_DefaultsMissingFeatureSettings_ToDisabled()
    {
        var configuration = new ConfigurationBuilder().Build();
        var service = new SystemCapabilitiesService(
            configuration,
            new FakeUnitOfWork("Custom.Provider"));

        var capabilities = service.GetCapabilities();

        Assert.Equal("Custom.Provider", capabilities.KnowledgeStore.Provider);
        Assert.False(capabilities.Llm.Enabled);
        Assert.Equal("none", capabilities.Llm.Provider);
        Assert.Equal("none", capabilities.Llm.Model);
        Assert.False(capabilities.Embedding.Enabled);
        Assert.Equal("none", capabilities.Embedding.Provider);
    }
}