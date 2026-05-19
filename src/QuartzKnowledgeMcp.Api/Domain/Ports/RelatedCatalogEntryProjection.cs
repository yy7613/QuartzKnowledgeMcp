namespace QuartzKnowledgeMcp.Api.Domain.Ports;

public sealed record RelatedCatalogEntryProjection(
    Guid EntryId,
    string DisplayName,
    string Overview,
    IReadOnlyList<string> SharedTags,
    IReadOnlyList<string> SharedTools,
    IReadOnlyList<string> SharedClients,
    decimal Score);