using System.ComponentModel;
using ModelContextProtocol.Server;
using QuartzKnowledgeMcp.Api.Capabilities;

namespace QuartzKnowledgeMcp.Api.Mcp;

[McpServerToolType]
public sealed class SystemMcpTools(SystemCapabilitiesService capabilitiesService)
{
    [McpServerTool, Description("Returns the current system capabilities including knowledge store, LLM, embedding, and search flags.")]
    public SystemCapabilitiesResponse get_system_capabilities()
    {
        return capabilitiesService.GetCapabilities();
    }
}