namespace QuartzKnowledgeMcp.Api.Gold;

public sealed record UpdateGoldCatalogEntryRequest(
    string? Overview,
    string? SetupGuide,
    IReadOnlyList<string>? References,
    IReadOnlyList<string>? SupportedClients,
    string? UpdatedBy);

public sealed record ReplaceGoldCatalogTagsRequest(
    IReadOnlyList<string>? Tags,
    string? UpdatedBy);

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
    string AuthenticationType,
    string SetupGuide,
    IReadOnlyList<GoldToolSummaryResponse> ToolSummaries,
    IReadOnlyList<GoldReferenceResponse> References,
    IReadOnlyList<string> SupportedClients,
    int HistoryCount,
    DateTime PublishedAtUtc,
    string PublishedBy,
    DateTime UpdatedAtUtc,
    string UpdatedBy);

public sealed record GoldCatalogEntrySummaryResponse(
    Guid Id,
    string DisplayName,
    string Overview,
    IReadOnlyList<string> Tags,
    string AuthenticationType,
    IReadOnlyList<string> SupportedClients,
    DateTime UpdatedAtUtc);

public sealed record GoldCatalogEntryListResponse(
    IReadOnlyList<GoldCatalogEntrySummaryResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record EntryHistoryResponse(
    Guid Id,
    string Action,
    string ChangedBy,
    DateTime ChangedAtUtc,
    string Summary,
    bool UsedLlm);

public sealed record EntryHistoryPageResponse(
    IReadOnlyList<EntryHistoryResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record GoldTagUpdateResponse(
    Guid EntryId,
    IReadOnlyList<string> Tags,
    DateTime UpdatedAtUtc,
    string UpdatedBy);

public sealed record GoldPublishResult(
    GoldCatalogEntryDetailResponse Entry,
    bool Created);