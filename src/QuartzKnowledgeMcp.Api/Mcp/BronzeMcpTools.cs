using System.ComponentModel;
using ModelContextProtocol.Server;
using QuartzKnowledgeMcp.Api.Application;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Silver;

namespace QuartzKnowledgeMcp.Api.Mcp;

[McpServerToolType]
public sealed class BronzeMcpTools(
    BronzeIngestionService bronzeIngestionService,
    SilverDraftApplicationService silverDraftApplicationService)
{
    [McpServerTool, Description("Imports a bronze source into the knowledge store.")]
    public async Task<BronzeCreateToolResponse> create_bronze_source(
        [Description("Source type such as github-readme.")] string sourceType,
        [Description("Raw source content.")] string rawContent,
        [Description("Source URI.")] string? sourceUri = null,
        [Description("Actor that imported the source.")] string? importedBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = await bronzeIngestionService.ImportAsync(
            new CreateBronzeSourceRequest(sourceType, sourceUri, rawContent, importedBy),
            cancellationToken);

        return new BronzeCreateToolResponse(
            result.Created,
            BronzeIngestionService.ToResponse(result.Source));
    }

    [McpServerTool, Description("Lists bronze sources.")]
    public Task<BronzeSourceListResponse> list_bronze_sources(
        [Description("1-based page number.")] int page = 1,
        [Description("Page size clamped to 1-100.")] int pageSize = 20,
        [Description("Optional bronze status filter.")] string? status = null,
        CancellationToken cancellationToken = default)
    {
        return bronzeIngestionService.ListAsync(page, pageSize, status, cancellationToken);
    }

    [McpServerTool, Description("Gets a bronze source by ID.")]
    public Task<BronzeSourceDetailResponse?> get_bronze_source(
        [Description("Bronze source ID.")] Guid bronzeId,
        CancellationToken cancellationToken = default)
    {
        return bronzeIngestionService.GetDetailAsync(bronzeId, cancellationToken);
    }

    [McpServerTool, Description("Organizes a bronze source into a silver draft.")]
    public async Task<SilverOrganizeToolResponse> organize_bronze_source(
        [Description("Bronze source ID.")] Guid bronzeId,
        [Description("Organize mode. Use silver-draft.")] string? mode = SilverOrganizeModes.SilverDraft,
        [Description("Request LLM-backed organization when configured. Falls back to rule-based when unavailable.")] bool? useLlm = null,
        [Description("When true, returns a draft preview without persisting silver data.")] bool? preview = null,
        CancellationToken cancellationToken = default)
    {
        var result = await silverDraftApplicationService.OrganizeAsync(
            bronzeId,
            new OrganizeBronzeSourceRequest(mode, useLlm, preview),
            cancellationToken);

        return new SilverOrganizeToolResponse(
            result.Created,
            SilverDraftService.ToDetailResponse(result.Draft, result.UsedLlm, result.Preview));
    }
}