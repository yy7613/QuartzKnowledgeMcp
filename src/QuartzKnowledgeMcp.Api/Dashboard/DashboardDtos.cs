namespace QuartzKnowledgeMcp.Api.Dashboard;

public sealed record DashboardOverviewResponse(
    int TotalObjects,
    int UniqueTagCount,
    DateTime? LatestActivityAtUtc);

public sealed record DashboardFreshnessResponse(
    int Last24Hours,
    int Last7Days,
    int Older);

public sealed record DashboardCountItemResponse(
    string Label,
    int Count);

public sealed record DashboardTrendPointResponse(
    string Day,
    int Count);

public sealed record DashboardRecentItemResponse(
    Guid Id,
    string Title,
    string Subtitle,
    string State,
    IReadOnlyList<string> Tags,
    DateTime TimestampUtc,
    string DetailPath);

public sealed record DashboardStageSummaryResponse(
    string Stage,
    int TotalCount,
    DateTime? LatestActivityAtUtc,
    DashboardFreshnessResponse Freshness,
    IReadOnlyList<DashboardCountItemResponse> Breakdown,
    IReadOnlyList<DashboardTrendPointResponse> Trend,
    IReadOnlyList<DashboardRecentItemResponse> RecentItems);

public sealed record DashboardTagListResponse(
    int UniqueCount,
    IReadOnlyList<DashboardCountItemResponse> Items);

public sealed record DashboardSummaryResponse(
    DateTime GeneratedAtUtc,
    DashboardOverviewResponse Overview,
    DashboardStageSummaryResponse Bronze,
    DashboardStageSummaryResponse Silver,
    DashboardStageSummaryResponse Gold,
    DashboardTagListResponse Tags);

public sealed record DashboardSearchItemResponse(
    Guid Id,
    string Stage,
    string Title,
    string Summary,
    string State,
    IReadOnlyList<string> Tags,
    DateTime TimestampUtc,
    string DetailPath,
    string FreshnessBucket);

public sealed record DashboardSearchResultResponse(
    string Query,
    string? Stage,
    string? Tag,
    string? Freshness,
    string Sort,
    int TotalCount,
    IReadOnlyList<DashboardSearchItemResponse> Items);