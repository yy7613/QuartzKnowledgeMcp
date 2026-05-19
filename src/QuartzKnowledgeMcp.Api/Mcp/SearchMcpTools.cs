using System.ComponentModel;
using ModelContextProtocol.Server;
using QuartzKnowledgeMcp.Api.Search;

namespace QuartzKnowledgeMcp.Api.Mcp;

[McpServerToolType]
public sealed class SearchMcpTools(CatalogSearchService catalogSearchService)
{
    [McpServerTool, Description("Runs structured catalog search.")]
    public Task<CatalogSearchResultResponse> search_catalog(
        [Description("Optional keyword query.")] string? query = null,
        [Description("Optional AND tag filters.")] string[]? tags = null,
        [Description("Optional authentication type filter.")] string? authType = null,
        [Description("Optional supported client filter.")] string? client = null,
        [Description("Sort order such as relevance, updated-desc, or name-asc.")] string? sort = null,
        [Description("1-based page number.")] int page = 1,
        [Description("Page size clamped to 1-100.")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return catalogSearchService.SearchAsync(
            query,
            tags,
            authType,
            client,
            sort,
            page,
            pageSize,
            cancellationToken);
    }

    [McpServerTool, Description("Runs advanced catalog search using the same request shape as the HTTP POST query endpoint.")]
    public Task<CatalogSearchResultResponse> search_catalog_advanced(
        [Description("Optional keyword query.")] string? query = null,
        [Description("Optional AND tag filters.")] string[]? tags = null,
        [Description("Optional authentication type filter.")] string? authType = null,
        [Description("Optional supported client filter.")] string? client = null,
        [Description("Sort order such as relevance, updated-desc, or name-asc.")] string? sort = null,
        [Description("1-based page number.")] int page = 1,
        [Description("Page size clamped to 1-100.")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return catalogSearchService.QueryAsync(
            new SearchQueryRequest(query, tags, authType, client, sort, page, pageSize),
            cancellationToken);
    }

    [McpServerTool, Description("Returns deterministic search suggestions.")]
    public Task<SearchSuggestionResultResponse> get_search_suggestions(
        [Description("Suggestion query.")] string? query,
        [Description("Maximum suggestions, clamped to 1-20.")] int limit = 10,
        [Description("Optional scope: all, name, tag, tool, or client.")] string? scope = null,
        CancellationToken cancellationToken = default)
    {
        return catalogSearchService.GetSuggestionsAsync(query, limit, scope, cancellationToken);
    }

    [McpServerTool, Description("Returns search facet counts for the current filters.")]
    public Task<SearchFacetResultResponse> get_search_facets(
        [Description("Optional keyword query.")] string? query = null,
        [Description("Optional AND tag filters.")] string[]? tags = null,
        [Description("Optional authentication type filter.")] string? authType = null,
        [Description("Optional supported client filter.")] string? client = null,
        CancellationToken cancellationToken = default)
    {
        return catalogSearchService.GetFacetsAsync(query, tags, authType, client, cancellationToken);
    }
}