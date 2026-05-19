using Microsoft.Extensions.Options;
using QuartzKnowledgeMcp.Api.Embedding;
using QuartzKnowledgeMcp.Api.Domain.Ports;
using QuartzKnowledgeMcp.Api.Silver;

namespace QuartzKnowledgeMcp.Api.Capabilities;

public sealed class SystemCapabilitiesService(
    IUnitOfWork unitOfWork,
    IOptions<FoundryOrganizationOptions> foundryOptions,
    IOptions<EmbeddingOptions> embeddingOptions)
{
    public SystemCapabilitiesResponse GetCapabilities()
    {
        var llmEnabled = foundryOptions.Value.IsConfigured;
        var llmProvider = llmEnabled ? foundryOptions.Value.EffectiveProvider : "none";
        var llmModel = llmEnabled ? foundryOptions.Value.EffectiveModel : "none";

        var embeddingEnabled = embeddingOptions.Value.IsConfigured;
        var embeddingProvider = embeddingEnabled ? embeddingOptions.Value.EffectiveProvider : "none";

        return new SystemCapabilitiesResponse(
            new KnowledgeStoreCapabilities(NormalizeKnowledgeStoreProvider(unitOfWork.ProviderName), Replaceable: true),
            new LlmCapabilities(llmEnabled, NormalizeFeatureProvider(llmProvider), llmModel, Replaceable: true),
            new EmbeddingCapabilities(embeddingEnabled, NormalizeFeatureProvider(embeddingProvider), Replaceable: true),
            new SearchCapabilities(
                SupportsStructuredSearch: true,
                SupportsSuggestions: true,
                SupportsFacets: true,
                SupportsRelatedEntries: true,
                SupportsSemanticSearch: embeddingEnabled));
    }

    private static string NormalizeKnowledgeStoreProvider(string? providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return "unknown";
        }

        if (providerName.Contains("sqlite", StringComparison.OrdinalIgnoreCase))
        {
            return "sqlite";
        }

        if (providerName.Contains("inmemory", StringComparison.OrdinalIgnoreCase))
        {
            return "in-memory";
        }

        return providerName.Trim();
    }

    private static string NormalizeFeatureProvider(string provider)
    {
        return string.IsNullOrWhiteSpace(provider)
            ? "none"
            : provider.Trim();
    }
}