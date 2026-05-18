namespace QuartzKnowledgeMcp.Api.Capabilities;

public sealed record KnowledgeStoreCapabilities(
    string Provider,
    bool Replaceable);

public sealed record LlmCapabilities(
    bool Enabled,
    string Provider,
    string Model,
    bool Replaceable);

public sealed record EmbeddingCapabilities(
    bool Enabled,
    string Provider,
    bool Replaceable);

public sealed record SearchCapabilities(
    bool SupportsStructuredSearch,
    bool SupportsSuggestions,
    bool SupportsFacets,
    bool SupportsRelatedEntries,
    bool SupportsSemanticSearch);

public sealed record SystemCapabilitiesResponse(
    KnowledgeStoreCapabilities KnowledgeStore,
    LlmCapabilities Llm,
    EmbeddingCapabilities Embedding,
    SearchCapabilities Search);