namespace QuartzKnowledgeMcp.Api.Search;

public sealed record SearchQueryRequest(
    string? Query,
    IReadOnlyList<string>? Tags,
    string? AuthType,
    string? Client,
    string? Sort,
    int? Page,
    int? PageSize);

public sealed record CatalogSearchItemResponse(
    Guid Id,
    string DisplayName,
    string Overview,
    IReadOnlyList<string> Tags,
    string AuthenticationType,
    IReadOnlyList<string> SupportedClients,
    DateTime UpdatedAtUtc,
    decimal Score);

public sealed record CatalogSearchResultResponse(
    IReadOnlyList<CatalogSearchItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record SearchSuggestionItemResponse(
    string Value,
    string Type,
    decimal Score);

public sealed record SearchSuggestionResultResponse(
    IReadOnlyList<SearchSuggestionItemResponse> Items);

public sealed record SearchFacetItemResponse(
    string Value,
    int Count);

public sealed record SearchFacetResultResponse(
    IReadOnlyList<SearchFacetItemResponse> Tags,
    IReadOnlyList<SearchFacetItemResponse> AuthTypes,
    IReadOnlyList<SearchFacetItemResponse> Clients);

public sealed record RelatedCatalogEntryResponse(
    Guid Id,
    string DisplayName,
    string Overview,
    IReadOnlyList<string> SharedTags,
    IReadOnlyList<string> SharedTools,
    IReadOnlyList<string> SharedClients,
    decimal Score);

public sealed record RelatedCatalogEntryResultResponse(
    IReadOnlyList<RelatedCatalogEntryResponse> Items);