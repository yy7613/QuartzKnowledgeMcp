namespace QuartzKnowledgeMcp.Api.Silver;

public sealed record SilverToolDraftContent(
    string Name,
    string Description);

public sealed record SilverServerDraftContent(
    string Name,
    string Summary,
    IReadOnlyList<string> TagCandidates,
    IReadOnlyList<SilverToolDraftContent> ToolDrafts);

public sealed record SilverToolDraftResponse(
    Guid Id,
    string Name,
    string Description);

public sealed record SilverServerDraftResponse(
    Guid Id,
    Guid BronzeSourceId,
    string Name,
    string Summary,
    IReadOnlyList<string> TagCandidates,
    int ToolCount,
    DateTime OrganizedAtUtc);

public sealed record SilverServerDraftDetailResponse(
    Guid Id,
    Guid BronzeSourceId,
    string Name,
    string Summary,
    IReadOnlyList<string> TagCandidates,
    IReadOnlyList<SilverToolDraftResponse> ToolDrafts,
    DateTime OrganizedAtUtc);

public sealed record SilverServerDraftListResponse(
    IReadOnlyList<SilverServerDraftResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record SilverOrganizeResult(
    SilverServerDraft Draft,
    bool Created);

public sealed record SilverOrganizeErrorResponse(
    string Code,
    string Message,
    Guid BronzeSourceId);