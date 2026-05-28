using System.ComponentModel;
using ModelContextProtocol.Server;
using QuartzKnowledgeMcp.Api.Capabilities;

namespace QuartzKnowledgeMcp.Api.Mcp;

[McpServerToolType]
public sealed class SystemMcpTools(SystemCapabilitiesService capabilitiesService)
{
    [McpServerTool, Description("Dedicated read-only tool for checking system capabilities and feature flags, including knowledge store, LLM organization, embedding, and search support. Use when the user asks what this MCP server can do or whether a capability is enabled. Do not use for health checks, catalog data, search results, or write operations. No list tool is needed before calling this.")]
    public SystemCapabilitiesResponse get_system_capabilities()
    {
        return capabilitiesService.GetCapabilities();
    }
}