namespace QuartzKnowledgeMcp.Api.Domain.Ports;

public sealed record SemanticCatalogDocument(
    Guid EntryId,
    string DisplayName,
    string Overview,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> ToolNames,
    IReadOnlyList<string> SupportedClients);