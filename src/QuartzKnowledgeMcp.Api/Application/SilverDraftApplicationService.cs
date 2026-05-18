using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Silver;

namespace QuartzKnowledgeMcp.Api.Application;

public sealed class SilverDraftApplicationService(SilverDraftService silverDraftService)
{
    public Task<SilverOrganizeResult> OrganizeAsync(
        Guid bronzeId,
        OrganizeBronzeSourceRequest? request,
        CancellationToken cancellationToken = default)
    {
        return silverDraftService.OrganizeAsync(
            bronzeId,
            request?.Mode,
            request?.UseLlm ?? false,
            request?.Preview ?? false,
            cancellationToken);
    }

    public Task<SilverServerDraftListResponse> ListAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return silverDraftService.ListAsync(page, pageSize, cancellationToken);
    }

    public Task<SilverServerDraftDetailResponse?> GetDetailAsync(
        Guid draftId,
        CancellationToken cancellationToken = default)
    {
        return silverDraftService.GetDetailAsync(draftId, cancellationToken);
    }
}