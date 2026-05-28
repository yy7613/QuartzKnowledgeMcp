using System.ComponentModel;
using ModelContextProtocol.Server;
using QuartzKnowledgeMcp.Api.Health;

namespace QuartzKnowledgeMcp.Api.Mcp;

[McpServerToolType]
public sealed class HealthMcpTools(IHealthStatusService healthStatusService)
{
    [McpServerTool, Description("Dedicated read-only tool for checking the MCP service health status. Use when the user asks whether the service is running, healthy, reachable, or what environment/component is responding. Do not use for system feature flags, catalog data, search, or write operations. No list tool is needed before calling this.")]
    public HealthStatus get_health()
    {
        return healthStatusService.GetStatus();
    }
}