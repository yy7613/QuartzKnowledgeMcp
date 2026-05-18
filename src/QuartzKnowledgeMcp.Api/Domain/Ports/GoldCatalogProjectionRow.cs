using QuartzKnowledgeMcp.Api.Gold;

namespace QuartzKnowledgeMcp.Api.Domain.Ports;

public sealed record GoldCatalogProjectionRow(
    GoldCatalogEntry Entry,
    string? RawContent);