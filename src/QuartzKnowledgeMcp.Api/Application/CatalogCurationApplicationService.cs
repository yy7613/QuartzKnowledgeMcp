using QuartzKnowledgeMcp.Api.Domain.Gold;
using QuartzKnowledgeMcp.Api.Gold;

namespace QuartzKnowledgeMcp.Api.Application;

public sealed class CatalogCurationApplicationService(GoldCatalogService goldCatalogService)
{
    public Task<GoldPublishResult> PublishAsync(
        Guid silverId,
        PublishSilverDraftRequest? request,
        CancellationToken cancellationToken = default)
    {
        return goldCatalogService.PublishAsync(
            silverId,
            request?.PublishedBy,
            cancellationToken);
    }

    public Task<GoldCatalogEntryListResponse> ListAsync(
        int page = 1,
        int pageSize = 20,
        string? tag = null,
        string? authType = null,
        string? client = null,
        CancellationToken cancellationToken = default)
    {
        return goldCatalogService.ListAsync(
            page,
            pageSize,
            tag,
            authType,
            client,
            cancellationToken);
    }

    public Task<GoldCatalogEntryDetailResponse?> GetDetailAsync(
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        return goldCatalogService.GetDetailAsync(entryId, cancellationToken);
    }

    public Task<EntryHistoryPageResponse?> GetHistoryAsync(
        Guid entryId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return goldCatalogService.GetHistoryAsync(entryId, page, pageSize, cancellationToken);
    }

    public Task<GoldCatalogEntryDetailResponse> UpdateAsync(
        Guid entryId,
        UpdateGoldCatalogEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var update = GoldCatalogUpdate.Create(
            request.Overview,
            request.SetupGuide,
            request.References,
            request.SupportedClients);

        return goldCatalogService.UpdateAsync(
            entryId,
            update,
            request.UpdatedBy,
            cancellationToken);
    }

    public Task<GoldTagUpdateResponse> ReplaceTagsAsync(
        Guid entryId,
        ReplaceGoldCatalogTagsRequest request,
        CancellationToken cancellationToken = default)
    {
        return goldCatalogService.ReplaceTagsAsync(
            entryId,
            GoldTagSet.Create(request.Tags),
            request.UpdatedBy,
            cancellationToken);
    }
}