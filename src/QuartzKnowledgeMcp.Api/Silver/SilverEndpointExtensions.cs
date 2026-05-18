using System.Diagnostics.CodeAnalysis;
using QuartzKnowledgeMcp.Api.Gold;

namespace QuartzKnowledgeMcp.Api.Silver;

[ExcludeFromCodeCoverage]
public static class SilverEndpointExtensions
{
    public static IEndpointRouteBuilder MapSilverEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/silver/server-drafts");

        group.MapGet("", async (
            SilverDraftService service,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken) =>
        {
            var response = await service.ListAsync(
                page ?? 1,
                pageSize ?? 20,
                cancellationToken);
            return Results.Ok(response);
        })
        .WithName("ListSilverServerDrafts");

        group.MapGet("/{draftId:guid}", async (
            Guid draftId,
            SilverDraftService service,
            CancellationToken cancellationToken) =>
        {
            var response = await service.GetDetailAsync(draftId, cancellationToken);
            return response is null
                ? Results.NotFound()
                : Results.Ok(response);
        })
        .WithName("GetSilverServerDraft");

        group.MapPost("/{silverId:guid}:publish", async (
            Guid silverId,
            PublishSilverDraftRequest? request,
            GoldCatalogService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.PublishAsync(
                    silverId,
                    request?.PublishedBy,
                    cancellationToken);

                return result.Created
                    ? Results.Created($"/api/gold/catalog/{result.Entry.Id}", result.Entry)
                    : Results.Ok(result.Entry);
            }
            catch (SilverDraftNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("PublishSilverServerDraft");

        return endpoints;
    }
}