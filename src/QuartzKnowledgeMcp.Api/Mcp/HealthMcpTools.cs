using System.ComponentModel;
using ModelContextProtocol.Server;
using QuartzKnowledgeMcp.Api.Health;

namespace QuartzKnowledgeMcp.Api.Mcp;

[McpServerToolType]
public sealed class HealthMcpTools(IHealthStatusService healthStatusService)
{
    [McpServerTool, Description("Returns the current service health status.")]
    public HealthStatus get_health()
    {
        return healthStatusService.GetStatus();
    }
}