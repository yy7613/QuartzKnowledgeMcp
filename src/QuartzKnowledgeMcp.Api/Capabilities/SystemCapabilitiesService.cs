using QuartzKnowledgeMcp.Api.Domain.Ports;

namespace QuartzKnowledgeMcp.Api.Capabilities;

public sealed class SystemCapabilitiesService(
    IConfiguration configuration,
    IUnitOfWork unitOfWork)
{
    public SystemCapabilitiesResponse GetCapabilities()
    {
        var llmProvider = FindConfiguredValue("Llm:Provider", "Organization:Llm:Provider") ?? "none";
        var llmEnabled = FindConfiguredFlag("Llm:Enabled", "Organization:Llm:Enabled")
            ?? !IsDisabledProvider(llmProvider);
        var llmModel = llmEnabled
            ? FindConfiguredValue("Llm:Model", "Organization:Llm:Model") ?? "configured-at-runtime"
            : "none";

        var embeddingProvider = FindConfiguredValue("Embedding:Provider", "Search:Embedding:Provider") ?? "none";
        var embeddingEnabled = FindConfiguredFlag("Embedding:Enabled", "Search:Embedding:Enabled")
            ?? !IsDisabledProvider(embeddingProvider);

        return new SystemCapabilitiesResponse(
            new KnowledgeStoreCapabilities(NormalizeKnowledgeStoreProvider(unitOfWork.ProviderName), Replaceable: true),
            new LlmCapabilities(llmEnabled, NormalizeFeatureProvider(llmProvider), llmModel, Replaceable: true),
            new EmbeddingCapabilities(embeddingEnabled, NormalizeFeatureProvider(embeddingProvider), Replaceable: true),
            new SearchCapabilities(
                SupportsStructuredSearch: true,
                SupportsSuggestions: true,
                SupportsFacets: true,
                SupportsRelatedEntries: false,
                SupportsSemanticSearch: embeddingEnabled));
    }

    private string? FindConfiguredValue(params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private bool? FindConfiguredFlag(params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = configuration[key];
            if (bool.TryParse(value, out var parsed))
            {
                return parsed;
            }
        }

        return null;
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

    private static bool IsDisabledProvider(string provider)
    {
        return string.IsNullOrWhiteSpace(provider)
            || string.Equals(provider, "none", StringComparison.OrdinalIgnoreCase)
            || string.Equals(provider, "disabled", StringComparison.OrdinalIgnoreCase);
    }
}