using System.ComponentModel;
using ModelContextProtocol.Server;
using QuartzKnowledgeMcp.Api.Search;

namespace QuartzKnowledgeMcp.Api.Mcp;

[McpServerToolType]
public sealed class SearchMcpTools(CatalogSearchService catalogSearchService)
{
    [McpServerTool, Description("Dedicated read-only tool for keyword and filter search over gold catalog entries. Use when the user asks to search, find, filter, or look up catalog entries by text, tag, auth type, supported client, or sort order. Do not use to create, publish, update, replace tags, or retrieve change history. You do not need to call list_gold_catalog first; call get_gold_catalog_entry after this only when the user needs full details for one result.")]
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

    [McpServerTool, Description("Dedicated read-only tool for advanced catalog search using the same request shape as the HTTP POST query endpoint. Use when the user asks for a structured search with multiple filters, paging, and sort behavior in one request. Do not use to create, publish, update, replace tags, or retrieve history. You do not need to call list_gold_catalog first; call get_gold_catalog_entry after this only when full details for one result are needed.")]
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

    [McpServerTool, Description("Dedicated read-only tool for deterministic catalog search suggestions. Use when the user asks for autocomplete, suggestion terms, likely tags, names, tools, or clients before running a search. Do not use to return catalog entries, full entry details, history, updates, publishing, or tag replacement. You do not need to call list_gold_catalog first.")]
    public Task<SearchSuggestionResultResponse> get_search_suggestions(
        [Description("Suggestion query.")] string? query,
        [Description("Maximum suggestions, clamped to 1-20.")] int limit = 10,
        [Description("Optional scope: all, name, tag, tool, or client.")] string? scope = null,
        CancellationToken cancellationToken = default)
    {
        return catalogSearchService.GetSuggestionsAsync(query, limit, scope, cancellationToken);
    }

    [McpServerTool, Description("Dedicated read-only tool for facet counts over the current catalog search filters. Use when the user asks for available filter values, counts by tag, auth type, or client, or wants to refine a search. Do not use to return full catalog entries, create, publish, update, replace tags, or retrieve history. You do not need to call list_gold_catalog first.")]
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