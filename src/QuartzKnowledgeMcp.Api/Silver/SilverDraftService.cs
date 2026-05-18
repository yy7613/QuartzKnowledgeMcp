using System.Text.Json;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Domain.Ports;
using QuartzKnowledgeMcp.Api.Persistence;

namespace QuartzKnowledgeMcp.Api.Silver;

public sealed class SilverDraftService(
    IKnowledgeRepository knowledgeRepository,
    IUnitOfWork unitOfWork,
    IOrganizationAgent organizationAgent,
    TimeProvider timeProvider)
{
    public async Task<SilverOrganizeResult> OrganizeAsync(
        Guid bronzeId,
        string? mode,
        bool useLlm = false,
        bool preview = false,
        CancellationToken cancellationToken = default)
    {
        if (!SilverOrganizeModes.IsSupported(mode))
        {
            throw new SilverValidationException(new Dictionary<string, string[]>
            {
                ["mode"] =
                [
                    $"mode must be '{SilverOrganizeModes.SilverDraft}'."
                ]
            });
        }

        var bronzeSource = await knowledgeRepository.GetBronzeSourceAsync(bronzeId, cancellationToken);

        if (bronzeSource is null)
        {
            throw new BronzeSourceNotFoundException(bronzeId);
        }

        OrganizationAgentResult organizationResult;
        try
        {
            organizationResult = await organizationAgent.OrganizeAsync(
                bronzeSource,
                new OrganizationAgentRequest(useLlm),
                cancellationToken);
        }
        catch (SilverNormalizationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new SilverNormalizationException(
                "Failed to normalize the bronze source into a silver draft.",
                exception);
        }

        var normalizedDraft = organizationResult.Draft;

        var existingDraft = await knowledgeRepository.GetSilverDraftByBronzeSourceIdAsync(
            bronzeId,
            includeToolDrafts: true,
            cancellationToken);

        var organizedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        if (preview)
        {
            var previewDraft = BuildDraft(
                existingDraft?.Id ?? Guid.NewGuid(),
                bronzeId,
                normalizedDraft,
                organizedAtUtc);

            return new SilverOrganizeResult(
                previewDraft,
                existingDraft is null,
                organizationResult.UsedLlm,
                Preview: true);
        }

        var draft = existingDraft ?? BuildDraft(
            Guid.NewGuid(),
            bronzeId,
            normalizedDraft,
            organizedAtUtc);

        if (existingDraft is not null)
        {
            ApplyDraft(draft, normalizedDraft, organizedAtUtc);
        }

        if (existingDraft is null)
        {
            knowledgeRepository.AddSilverDraft(draft);
        }

        bronzeSource.Status = BronzeSourceStatuses.Organized;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SilverOrganizeResult(draft, existingDraft is null, organizationResult.UsedLlm);
    }

    public async Task<SilverServerDraftListResponse> ListAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var drafts = await knowledgeRepository.GetSilverDraftsAsync(
            includeToolDrafts: true,
            cancellationToken);

        if (drafts is null)
        {
            throw new InvalidOperationException("Knowledge repository returned null silver drafts collection.");
        }

        var items = drafts
            .OrderByDescending(draft => draft.OrganizedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var totalDrafts = await knowledgeRepository.GetSilverDraftsAsync(
            includeToolDrafts: false,
            cancellationToken);

        if (totalDrafts is null)
        {
            throw new InvalidOperationException("Knowledge repository returned null silver drafts collection.");
        }

        var totalCount = totalDrafts.Count;

        return new SilverServerDraftListResponse(
            items.Select(item => new SilverServerDraftResponse(
                item.Id,
                item.BronzeSourceId,
                item.Name,
                item.Summary,
                DeserializeTags(item.TagCandidatesJson),
                item.ToolDrafts.Count,
                item.OrganizedAtUtc))
            .ToList(),
            page,
            pageSize,
            totalCount);
    }

    public async Task<SilverServerDraftDetailResponse?> GetDetailAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var draft = await knowledgeRepository.GetSilverDraftAsync(
            id,
            includeToolDrafts: true,
            cancellationToken);

        return draft is null
            ? null
            : ToDetailResponse(draft);
    }

    public static SilverServerDraftDetailResponse ToDetailResponse(
        SilverServerDraft draft,
        bool usedLlm = false,
        bool preview = false)
    {
        return new SilverServerDraftDetailResponse(
            draft.Id,
            draft.BronzeSourceId,
            draft.Name,
            draft.Summary,
            DeserializeTags(draft.TagCandidatesJson),
            draft.ToolDrafts
                .OrderBy(toolDraft => toolDraft.Position)
                .Select(toolDraft => new SilverToolDraftResponse(
                    toolDraft.Id,
                    toolDraft.Name,
                    toolDraft.Description))
                .ToList(),
            draft.OrganizedAtUtc,
            usedLlm,
            preview);
    }

    private SilverServerDraft BuildDraft(
        Guid draftId,
        Guid bronzeId,
        SilverServerDraftContent normalizedDraft,
        DateTime organizedAtUtc)
    {
        var draft = new SilverServerDraft
        {
            Id = draftId,
            BronzeSourceId = bronzeId
        };

        ApplyDraft(draft, normalizedDraft, organizedAtUtc);
        return draft;
    }

    private void ApplyDraft(
        SilverServerDraft draft,
        SilverServerDraftContent normalizedDraft,
        DateTime organizedAtUtc)
    {
        draft.Name = normalizedDraft.Name;
        draft.Summary = normalizedDraft.Summary;
        draft.TagCandidatesJson = SerializeTags(normalizedDraft.TagCandidates);
        draft.OrganizedAtUtc = organizedAtUtc;

        var normalizedToolDrafts = normalizedDraft.ToolDrafts
            .DistinctBy(tool => tool.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var existingToolDrafts = draft.ToolDrafts
            .OrderBy(toolDraft => toolDraft.Position)
            .ToList();
        var sharedCount = Math.Min(existingToolDrafts.Count, normalizedToolDrafts.Count);

        for (var index = 0; index < sharedCount; index++)
        {
            existingToolDrafts[index].Name = normalizedToolDrafts[index].Name;
            existingToolDrafts[index].Description = normalizedToolDrafts[index].Description;
            existingToolDrafts[index].Position = index;
        }

        if (existingToolDrafts.Count > normalizedToolDrafts.Count)
        {
            var extraToolDrafts = existingToolDrafts
                .Skip(normalizedToolDrafts.Count)
                .ToList();

            knowledgeRepository.RemoveSilverToolDrafts(extraToolDrafts);
            foreach (var extraToolDraft in extraToolDrafts)
            {
                draft.ToolDrafts.Remove(extraToolDraft);
            }
        }

        for (var index = sharedCount; index < normalizedToolDrafts.Count; index++)
        {
            var toolDraft = normalizedToolDrafts[index];
            draft.ToolDrafts.Add(new SilverToolDraft
            {
                Id = Guid.NewGuid(),
                Name = toolDraft.Name,
                Description = toolDraft.Description,
                Position = index
            });
        }
    }

    private static string SerializeTags(IReadOnlyList<string> tags)
    {
        return JsonSerializer.Serialize(tags);
    }

    private static IReadOnlyList<string> DeserializeTags(string json)
    {
        return JsonSerializer.Deserialize<List<string>>(json)
            ?? [];
    }
}