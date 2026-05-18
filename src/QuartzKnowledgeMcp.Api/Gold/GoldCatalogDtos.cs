namespace QuartzKnowledgeMcp.Api.Gold;

public sealed record PublishSilverDraftRequest(
    string? PublishedBy);

public sealed record GoldToolSummaryResponse(
    string Name,
    string Description);

public sealed record GoldReferenceResponse(
    string Label,
    string Url);

public sealed record GoldCatalogEntryDetailResponse(
    Guid Id,
    Guid SilverServerDraftId,
    string DisplayName,
    string Overview,
    IReadOnlyList<string> Tags,
    string SetupGuide,
    IReadOnlyList<GoldToolSummaryResponse> ToolSummaries,
    IReadOnlyList<GoldReferenceResponse> References,
    IReadOnlyList<string> SupportedClients,
    int HistoryCount,
    DateTime PublishedAtUtc,
    string PublishedBy,
    DateTime UpdatedAtUtc,
    string UpdatedBy);

public sealed record GoldPublishResult(
    GoldCatalogEntryDetailResponse Entry,
    bool Created);