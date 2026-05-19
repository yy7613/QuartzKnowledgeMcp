namespace QuartzKnowledgeMcp.Api.Dashboard;

public static class DashboardEndpointExtensions
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/dashboard");

        group.MapGet("/summary", async (
            DashboardService service,
            CancellationToken cancellationToken,
            int recentPerStage = 8) =>
            Results.Ok(await service.GetSummaryAsync(recentPerStage, cancellationToken)))
            .WithName("GetDashboardSummary");

        group.MapGet("/search", async (
            DashboardService service,
            CancellationToken cancellationToken,
            string? q = null,
            string? stage = null,
            string? tag = null,
            string? freshness = null,
            string? sort = null,
            int limit = 30) =>
            Results.Ok(await service.SearchAsync(q, stage, tag, freshness, sort, limit, cancellationToken)))
            .WithName("SearchDashboard");

        return endpoints;
    }
}
