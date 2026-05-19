using System.Diagnostics.CodeAnalysis;

namespace QuartzKnowledgeMcp.Api.Search;

[ExcludeFromCodeCoverage]
public static class SearchEndpointExtensions
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/search");

        group.MapGet("", async (
            CatalogSearchService service,
            string? q,
            string[]? tags,
            string? authType,
            string? client,
            string? sort,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken) =>
        {
            var response = await service.SearchAsync(
                q,
                tags,
                authType,
                client,
                sort,
                page ?? 1,
                pageSize ?? 20,
                cancellationToken);
            return Results.Ok(response);
        })
        .WithName("SearchCatalog");

        group.MapPost("/query", async (
            SearchQueryRequest request,
            CatalogSearchService service,
            CancellationToken cancellationToken) =>
        {
            var response = await service.QueryAsync(request, cancellationToken);
            return Results.Ok(response);
        })
        .WithName("QuerySearchCatalog");

        group.MapGet("/suggestions", async (
            CatalogSearchService service,
            string? q,
            int? limit,
            string? scope,
            CancellationToken cancellationToken) =>
        {
            var response = await service.GetSuggestionsAsync(
                q,
                limit ?? 10,
                scope,
                cancellationToken);
            return Results.Ok(response);
        })
        .WithName("GetSearchSuggestions");

        group.MapGet("/facets", async (
            CatalogSearchService service,
            string? q,
            string[]? tags,
            string? authType,
            string? client,
            CancellationToken cancellationToken) =>
        {
            var response = await service.GetFacetsAsync(
                q,
                tags,
                authType,
                client,
                cancellationToken);
            return Results.Ok(response);
        })
        .WithName("GetSearchFacets");

        return endpoints;
    }
}