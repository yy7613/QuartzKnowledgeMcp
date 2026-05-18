namespace QuartzKnowledgeMcp.Api.Bronze;

public sealed record CreateBronzeSourceRequest(
    string? SourceType,
    string? SourceUri,
    string? RawContent,
    string? ImportedBy);

public sealed record OrganizeBronzeSourceRequest(
    string? Mode);

public sealed record BronzeSourceResponse(
    Guid Id,
    string SourceType,
    string? SourceUri,
    string Status,
    DateTime ImportedAtUtc);

public sealed record BronzeSourceDetailResponse(
    Guid Id,
    string SourceType,
    string? SourceUri,
    string RawContent,
    string Status,
    string? ImportedBy,
    DateTime ImportedAtUtc);

public sealed record BronzeSourceListResponse(
    IReadOnlyList<BronzeSourceResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record BronzeImportResult(
    BronzeSource Source,
    bool Created);
