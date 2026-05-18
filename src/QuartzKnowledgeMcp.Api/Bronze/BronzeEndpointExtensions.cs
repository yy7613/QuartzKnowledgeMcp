using System.Diagnostics.CodeAnalysis;
using QuartzKnowledgeMcp.Api.Application;
using QuartzKnowledgeMcp.Api.Silver;

namespace QuartzKnowledgeMcp.Api.Bronze;

[ExcludeFromCodeCoverage]
public static class BronzeEndpointExtensions
{
    public static IEndpointRouteBuilder MapBronzeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/bronze/sources");

        group.MapPost("", async (
            CreateBronzeSourceRequest request,
            BronzeIngestionService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.ImportAsync(request, cancellationToken);
                var response = BronzeIngestionService.ToResponse(result.Source);

                return result.Created
                    ? Results.Created($"/api/bronze/sources/{response.Id}", response)
                    : Results.Ok(response);
            }
            catch (BronzeValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
            }
        })
        .WithName("CreateBronzeSource");

        group.MapGet("", async (
            BronzeIngestionService service,
            int? page,
            int? pageSize,
            string? status,
            CancellationToken cancellationToken) =>
        {
            var response = await service.ListAsync(
                page ?? 1,
                pageSize ?? 20,
                status,
                cancellationToken);
            return Results.Ok(response);
        })
        .WithName("ListBronzeSources");

        group.MapGet("/{bronzeId:guid}", async (
            Guid bronzeId,
            BronzeIngestionService service,
            CancellationToken cancellationToken) =>
        {
            var response = await service.GetDetailAsync(bronzeId, cancellationToken);
            return response is null
                ? Results.NotFound()
                : Results.Ok(response);
        })
        .WithName("GetBronzeSource");

        group.MapPost("/{bronzeId:guid}:organize", async (
            Guid bronzeId,
            OrganizeBronzeSourceRequest? request,
            SilverDraftApplicationService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.OrganizeAsync(
                    bronzeId,
                    request,
                    cancellationToken);
                var response = SilverDraftService.ToDetailResponse(
                    result.Draft,
                    result.UsedLlm,
                    result.Preview);

                return !result.Preview && result.Created
                    ? Results.Created($"/api/silver/server-drafts/{response.Id}", response)
                    : Results.Ok(response);
            }
            catch (SilverValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
            }
            catch (BronzeSourceNotFoundException)
            {
                return Results.NotFound();
            }
            catch (SilverNormalizationException exception)
            {
                return Results.UnprocessableEntity(new SilverOrganizeErrorResponse(
                    "silver_normalization_failed",
                    exception.Message,
                    bronzeId));
            }
        })
        .WithName("OrganizeBronzeSource");

        return endpoints;
    }
}
