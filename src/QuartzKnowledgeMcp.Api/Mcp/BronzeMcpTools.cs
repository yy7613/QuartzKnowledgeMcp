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
    [McpServerTool, Description("Dedicated MCP write tool for registering exactly one new bronze source in the knowledge store. Use when the user asks to add, import, register, ingest, or create a new source document. Do not use to list, read, organize, update, publish, or delete existing records. You do not need to call list_bronze_sources first unless the user asks to avoid duplicate imports or compare with existing sources.")]
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

    [McpServerTool, Description("Dedicated read-only tool for listing bronze source records with optional paging and status filtering. Use when the user asks what sources exist, wants to find a bronzeId, or asks to check whether a source was already imported. Do not use to create, organize, update, publish, or delete records. Call this before get_bronze_source when the user does not know the target bronzeId.")]
    public Task<BronzeSourceListResponse> list_bronze_sources(
        [Description("1-based page number.")] int page = 1,
        [Description("Page size clamped to 1-100.")] int pageSize = 20,
        [Description("Optional bronze status filter.")] string? status = null,
        CancellationToken cancellationToken = default)
    {
        return bronzeIngestionService.ListAsync(page, pageSize, status, cancellationToken);
    }

    [McpServerTool, Description("Dedicated read-only tool for retrieving one bronze source by bronzeId. Use when the user asks for details, raw content, status, or metadata of a known bronze source. Do not use to list many sources, create a source, or organize a source into a silver draft. If the user does not provide a bronzeId, call list_bronze_sources first.")]
    public Task<BronzeSourceDetailResponse?> get_bronze_source(
        [Description("Bronze source ID.")] Guid bronzeId,
        CancellationToken cancellationToken = default)
    {
        return bronzeIngestionService.GetDetailAsync(bronzeId, cancellationToken);
    }

    [McpServerTool, Description("Dedicated MCP write tool for creating or previewing a silver server draft from one existing bronze source. Use when the user asks to organize, convert, draft, normalize, or prepare a bronze source for catalog curation. Do not use to register a new bronze source, publish to gold, update gold entries, or replace tags. If the user does not provide a bronzeId, call list_bronze_sources first; if they provide an ID, you can call this directly.")]
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