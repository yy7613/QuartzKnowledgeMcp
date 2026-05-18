using System.ComponentModel;
using ModelContextProtocol.Server;
using QuartzKnowledgeMcp.Api.Application;
using QuartzKnowledgeMcp.Api.Gold;
using QuartzKnowledgeMcp.Api.Silver;

namespace QuartzKnowledgeMcp.Api.Mcp;

[McpServerToolType]
public sealed class CatalogMcpTools(
    SilverDraftApplicationService silverDraftApplicationService,
    CatalogCurationApplicationService catalogCurationApplicationService)
{
    [McpServerTool, Description("Lists silver drafts.")]
    public Task<SilverServerDraftListResponse> list_silver_server_drafts(
        [Description("1-based page number.")] int page = 1,
        [Description("Page size clamped to 1-100.")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return silverDraftApplicationService.ListAsync(page, pageSize, cancellationToken);
    }

    [McpServerTool, Description("Gets a silver draft by ID.")]
    public Task<SilverServerDraftDetailResponse?> get_silver_server_draft(
        [Description("Silver draft ID.")] Guid draftId,
        CancellationToken cancellationToken = default)
    {
        return silverDraftApplicationService.GetDetailAsync(draftId, cancellationToken);
    }

    [McpServerTool, Description("Publishes a silver draft into the gold catalog.")]
    public async Task<GoldPublishToolResponse> publish_silver_server_draft(
        [Description("Silver draft ID.")] Guid silverId,
        [Description("Actor that publishes the draft.")] string? publishedBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = await catalogCurationApplicationService.PublishAsync(
            silverId,
            new PublishSilverDraftRequest(publishedBy),
            cancellationToken);

        return new GoldPublishToolResponse(result.Created, result.Entry);
    }

    [McpServerTool, Description("Lists gold catalog entries.")]
    public Task<GoldCatalogEntryListResponse> list_gold_catalog(
        [Description("1-based page number.")] int page = 1,
        [Description("Page size clamped to 1-100.")] int pageSize = 20,
        [Description("Optional tag filter.")] string? tag = null,
        [Description("Optional authentication type filter.")] string? authType = null,
        [Description("Optional client filter.")] string? client = null,
        CancellationToken cancellationToken = default)
    {
        return catalogCurationApplicationService.ListAsync(
            page,
            pageSize,
            tag,
            authType,
            client,
            cancellationToken);
    }

    [McpServerTool, Description("Gets a gold catalog entry by ID.")]
    public Task<GoldCatalogEntryDetailResponse?> get_gold_catalog_entry(
        [Description("Gold catalog entry ID.")] Guid entryId,
        CancellationToken cancellationToken = default)
    {
        return catalogCurationApplicationService.GetDetailAsync(entryId, cancellationToken);
    }

    [McpServerTool, Description("Updates editable fields on a gold catalog entry.")]
    public Task<GoldCatalogEntryDetailResponse> update_gold_catalog_entry(
        [Description("Gold catalog entry ID.")] Guid entryId,
        [Description("Updated overview text.")] string overview,
        [Description("Updated setup guide text.")] string setupGuide,
        [Description("Optional reference URLs.")] string[]? references = null,
        [Description("Optional supported client names.")] string[]? supportedClients = null,
        [Description("Actor that performs the update.")] string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        return catalogCurationApplicationService.UpdateAsync(
            entryId,
            new UpdateGoldCatalogEntryRequest(
                overview,
                setupGuide,
                references,
                supportedClients,
                updatedBy),
            cancellationToken);
    }

    [McpServerTool, Description("Replaces tags on a gold catalog entry.")]
    public Task<GoldTagUpdateResponse> replace_gold_catalog_tags(
        [Description("Gold catalog entry ID.")] Guid entryId,
        [Description("Replacement tags. Must contain between 1 and 5 values.")] string[] tags,
        [Description("Actor that performs the tag update.")] string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        return catalogCurationApplicationService.ReplaceTagsAsync(
            entryId,
            new ReplaceGoldCatalogTagsRequest(tags, updatedBy),
            cancellationToken);
    }

    [McpServerTool, Description("Gets paged history for a gold catalog entry.")]
    public Task<EntryHistoryPageResponse?> get_gold_catalog_history(
        [Description("Gold catalog entry ID.")] Guid entryId,
        [Description("1-based page number.")] int page = 1,
        [Description("Page size clamped to 1-100.")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return catalogCurationApplicationService.GetHistoryAsync(entryId, page, pageSize, cancellationToken);
    }
}