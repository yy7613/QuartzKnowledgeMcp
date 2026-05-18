namespace QuartzKnowledgeMcp.Api.Capabilities;

public static class SystemEndpointExtensions
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/system/capabilities", (SystemCapabilitiesService service) =>
            Results.Ok(service.GetCapabilities()))
            .WithName("GetSystemCapabilities");

        return endpoints;
    }
}