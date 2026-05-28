using System.ComponentModel;
using ModelContextProtocol.Server;
using QuartzKnowledgeMcp.Api.Application;
using QuartzKnowledgeMcp.Api.Gold;
using QuartzKnowledgeMcp.Api.Silver;
using QuartzKnowledgeMcp.Api.Search;

namespace QuartzKnowledgeMcp.Api.Mcp;

[McpServerToolType]
public sealed class CatalogMcpTools(
    SilverDraftApplicationService silverDraftApplicationService,
    CatalogCurationApplicationService catalogCurationApplicationService,
    CatalogRelationService catalogRelationService)
{
    [McpServerTool, Description("Dedicated read-only tool for listing silver server drafts with paging. Use when the user asks what drafts exist, wants to find a silver draft ID, or needs to choose a draft before publishing. Do not use to create a draft from bronze, publish a draft, update a gold entry, or replace tags. Call this before get_silver_server_draft or publish_silver_draft_to_gold_catalog_via_mcp when the user does not know the silverId.")]
    public Task<SilverServerDraftListResponse> list_silver_server_drafts(
        [Description("1-based page number.")] int page = 1,
        [Description("Page size clamped to 1-100.")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return silverDraftApplicationService.ListAsync(page, pageSize, cancellationToken);
    }

    [McpServerTool, Description("Dedicated read-only tool for retrieving one silver server draft by draftId. Use when the user asks for draft details, normalized content, tool drafts, or publish readiness of a known silver draft. Do not use to list drafts, create drafts from bronze, publish to gold, or edit gold catalog entries. If the user does not provide a draftId, call list_silver_server_drafts first.")]
    public Task<SilverServerDraftDetailResponse?> get_silver_server_draft(
        [Description("Silver draft ID.")] Guid draftId,
        CancellationToken cancellationToken = default)
    {
        return silverDraftApplicationService.GetDetailAsync(draftId, cancellationToken);
    }

    [McpServerTool, Description("Dedicated MCP write tool for publishing exactly one existing silver draft into the gold catalog. Use when the user asks to publish, promote, approve, or add a silver draft to the gold catalog. Do not use to create a silver draft from bronze, list drafts, read gold entries, update gold content, or replace tags. If the user does not provide a silverId, call list_silver_server_drafts first; if they provide an ID, you can call this directly.")]
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

    [McpServerTool, Description("Dedicated read-only tool for listing gold catalog entries with paging and optional tag, authType, and client filters. Use when the user asks what catalog entries exist, wants to browse the catalog, or needs to find an entryId before reading history, related entries, updates, or tag replacement. Do not use to publish, update, or replace tags. Call this before get_gold_catalog_entry, update_gold_catalog_entry_via_mcp, replace_gold_catalog_tags_via_mcp, get_gold_catalog_history, or get_related_entries when the user does not know the entryId.")]
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

    [McpServerTool, Description("Dedicated read-only tool for retrieving one gold catalog entry by entryId. Use when the user asks for full details, overview, setup guide, references, supported clients, tags, or metadata of a known catalog entry. Do not use to list entries, publish a draft, update content, replace tags, get history, or get related entries. If the user does not provide an entryId, call list_gold_catalog first.")]
    public Task<GoldCatalogEntryDetailResponse?> get_gold_catalog_entry(
        [Description("Gold catalog entry ID.")] Guid entryId,
        CancellationToken cancellationToken = default)
    {
        return catalogCurationApplicationService.GetDetailAsync(entryId, cancellationToken);
    }

    [McpServerTool, Description("Dedicated MCP write tool for updating editable content fields on one existing gold catalog entry: overview, setupGuide, references, and supportedClients. Use when the user asks to edit, revise, correct, or update catalog entry content. Do not use to create entries, publish silver drafts, replace the tag set, list entries, read history, or delete records. If the user does not provide an entryId, call list_gold_catalog first; call get_gold_catalog_entry first when you need the current content to preserve unchanged fields.")]
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

    [McpServerTool, Description("Dedicated MCP write tool for replacing the entire tag set on one existing gold catalog entry. Use when the user asks to set, replace, retag, or overwrite tags. Do not use for partial tag add/remove unless you first determine the final full tag set, and do not use to update overview, setup guide, references, or supported clients. If the user does not provide an entryId, call list_gold_catalog first; call get_gold_catalog_entry first when you need the current tags to compute the replacement set.")]
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

    [McpServerTool, Description("Dedicated read-only tool for retrieving paged change history for one gold catalog entry. Use when the user asks what changed, who changed it, audit history, or previous catalog revisions for a known entry. Do not use to read the current entry details, update content, replace tags, publish drafts, or list all entries. If the user does not provide an entryId, call list_gold_catalog first.")]
    public Task<EntryHistoryPageResponse?> get_gold_catalog_history(
        [Description("Gold catalog entry ID.")] Guid entryId,
        [Description("1-based page number.")] int page = 1,
        [Description("Page size clamped to 1-100.")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return catalogCurationApplicationService.GetHistoryAsync(entryId, page, pageSize, cancellationToken);
    }

    [McpServerTool, Description("Dedicated read-only tool for finding gold catalog entries related to one known entry using structured-first relation scoring. Use when the user asks for similar, related, comparable, or neighboring MCP servers or catalog entries. Do not use for keyword search, catalog listing, history, publishing, updating, or tag replacement. If the user does not provide an entryId, call list_gold_catalog or search_catalog first to identify the source entry.")]
    public Task<RelatedCatalogEntryResultResponse?> get_related_entries(
        [Description("Gold catalog entry ID.")] Guid entryId,
        [Description("Maximum related entries, clamped by the underlying service.")] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        return catalogRelationService.GetRelatedAsync(entryId, limit, cancellationToken);
    }
}