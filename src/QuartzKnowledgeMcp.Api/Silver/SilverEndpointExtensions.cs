using System.Diagnostics.CodeAnalysis;
using QuartzKnowledgeMcp.Api.Application;
using QuartzKnowledgeMcp.Api.Gold;

namespace QuartzKnowledgeMcp.Api.Silver;

[ExcludeFromCodeCoverage]
public static class SilverEndpointExtensions
{
    public static IEndpointRouteBuilder MapSilverEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/silver/server-drafts");

        group.MapGet("", async (
            SilverDraftApplicationService service,
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
            SilverDraftApplicationService service,
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
            CatalogCurationApplicationService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.PublishAsync(
                    silverId,
                    request,
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