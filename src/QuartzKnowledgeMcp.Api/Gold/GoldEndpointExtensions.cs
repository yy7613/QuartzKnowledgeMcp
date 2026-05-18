using System.Diagnostics.CodeAnalysis;

namespace QuartzKnowledgeMcp.Api.Gold;

[ExcludeFromCodeCoverage]
public static class GoldEndpointExtensions
{
    public static IEndpointRouteBuilder MapGoldEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/gold/catalog");

        group.MapGet("/{entryId:guid}", async (
            Guid entryId,
            GoldCatalogService service,
            CancellationToken cancellationToken) =>
        {
            var response = await service.GetDetailAsync(entryId, cancellationToken);
            return response is null
                ? Results.NotFound()
                : Results.Ok(response);
        })
        .WithName("GetGoldCatalogEntry");

        return endpoints;
    }
}