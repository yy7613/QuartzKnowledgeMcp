using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using QuartzKnowledgeMcp.Api.Capabilities;
using QuartzKnowledgeMcp.Api.Embedding;
using QuartzKnowledgeMcp.Api.Silver;
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
                ["FoundryOrganization:Enabled"] = "true",
                ["FoundryOrganization:Provider"] = "foundry",
                ["FoundryOrganization:ProjectEndpoint"] = "https://example.foundry.azure.com/api/projects/demo",
                ["FoundryOrganization:Model"] = "gpt-4.1-mini",
                ["Embedding:Enabled"] = "true",
                ["Embedding:Provider"] = "azure-openai"
            })
            .Build();
        var service = new SystemCapabilitiesService(
            new FakeUnitOfWork("Microsoft.EntityFrameworkCore.Sqlite"),
            CreateFoundryOptions(configuration),
            CreateEmbeddingOptions(configuration));

        var capabilities = service.GetCapabilities();

        Assert.Equal("sqlite", capabilities.KnowledgeStore.Provider);
        Assert.True(capabilities.KnowledgeStore.Replaceable);
        Assert.True(capabilities.Llm.Enabled);
        Assert.Equal("foundry", capabilities.Llm.Provider);
        Assert.Equal("gpt-4.1-mini", capabilities.Llm.Model);
        Assert.True(capabilities.Embedding.Enabled);
        Assert.Equal("azure-openai", capabilities.Embedding.Provider);
        Assert.True(capabilities.Search.SupportsStructuredSearch);
        Assert.True(capabilities.Search.SupportsSuggestions);
        Assert.True(capabilities.Search.SupportsFacets);
        Assert.True(capabilities.Search.SupportsRelatedEntries);
        Assert.True(capabilities.Search.SupportsSemanticSearch);
    }

    [Fact]
    public void GetCapabilities_DefaultsMissingFeatureSettings_ToDisabled()
    {
        var configuration = new ConfigurationBuilder().Build();
        var service = new SystemCapabilitiesService(
            new FakeUnitOfWork("Custom.Provider"),
            CreateFoundryOptions(configuration),
            CreateEmbeddingOptions(configuration));

        var capabilities = service.GetCapabilities();

        Assert.Equal("Custom.Provider", capabilities.KnowledgeStore.Provider);
        Assert.False(capabilities.Llm.Enabled);
        Assert.Equal("none", capabilities.Llm.Provider);
        Assert.Equal("none", capabilities.Llm.Model);
        Assert.False(capabilities.Embedding.Enabled);
        Assert.Equal("none", capabilities.Embedding.Provider);
        Assert.True(capabilities.Search.SupportsRelatedEntries);
        Assert.False(capabilities.Search.SupportsSemanticSearch);
    }

    private static IOptions<FoundryOrganizationOptions> CreateFoundryOptions(IConfiguration configuration)
    {
        var options = new FoundryOrganizationOptions();
        configuration.GetSection(FoundryOrganizationOptions.SectionName).Bind(options);
        return Options.Create(options);
    }

    private static IOptions<EmbeddingOptions> CreateEmbeddingOptions(IConfiguration configuration)
    {
        var options = new EmbeddingOptions();
        configuration.GetSection(EmbeddingOptions.SectionName).Bind(options);
        return Options.Create(options);
    }
}