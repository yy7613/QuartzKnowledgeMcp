using System.Diagnostics.CodeAnalysis;
using QuartzKnowledgeMcp.Api.Application;

namespace QuartzKnowledgeMcp.Api.Gold;

[ExcludeFromCodeCoverage]
public static class GoldEndpointExtensions
{
    public static IEndpointRouteBuilder MapGoldEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/gold/catalog");

        group.MapGet("", async (
            CatalogCurationApplicationService service,
            int? page,
            int? pageSize,
            string? tag,
            string? authType,
            string? client,
            CancellationToken cancellationToken) =>
        {
            var response = await service.ListAsync(
                page ?? 1,
                pageSize ?? 20,
                tag,
                authType,
                client,
                cancellationToken);
            return Results.Ok(response);
        })
        .WithName("ListGoldCatalogEntries");

        group.MapGet("/{entryId:guid}", async (
            Guid entryId,
            CatalogCurationApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var response = await service.GetDetailAsync(entryId, cancellationToken);
            return response is null
                ? Results.NotFound()
                : Results.Ok(response);
        })
        .WithName("GetGoldCatalogEntry");

        group.MapPut("/{entryId:guid}", async (
            Guid entryId,
            UpdateGoldCatalogEntryRequest request,
            CatalogCurationApplicationService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await service.UpdateAsync(entryId, request, cancellationToken);
                return Results.Ok(response);
            }
            catch (GoldValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
            }
            catch (GoldCatalogEntryNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("UpdateGoldCatalogEntry");

        group.MapPut("/{entryId:guid}/tags", async (
            Guid entryId,
            ReplaceGoldCatalogTagsRequest request,
            CatalogCurationApplicationService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await service.ReplaceTagsAsync(entryId, request, cancellationToken);
                return Results.Ok(response);
            }
            catch (GoldValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
            }
            catch (GoldCatalogEntryNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("ReplaceGoldCatalogTags");

        group.MapGet("/{entryId:guid}/history", async (
            Guid entryId,
            CatalogCurationApplicationService service,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken) =>
        {
            var response = await service.GetHistoryAsync(
                entryId,
                page ?? 1,
                pageSize ?? 20,
                cancellationToken);
            return response is null
                ? Results.NotFound()
                : Results.Ok(response);
        })
        .WithName("GetGoldCatalogEntryHistory");

        return endpoints;
    }
}