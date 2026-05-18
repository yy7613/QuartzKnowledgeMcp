using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Domain.Gold;
using QuartzKnowledgeMcp.Api.Domain.Ports;
using QuartzKnowledgeMcp.Api.Persistence;
using QuartzKnowledgeMcp.Api.Silver;

namespace QuartzKnowledgeMcp.Api.Gold;

public sealed class GoldCatalogService(
    IKnowledgeRepository knowledgeRepository,
    IHistoryRepository historyRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<GoldCatalogEntryDetailResponse> UpdateAsync(
        Guid entryId,
        UpdateGoldCatalogEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var update = GoldCatalogUpdate.Create(
            request.Overview,
            request.SetupGuide,
            request.References,
            request.SupportedClients);

        return await UpdateAsync(
            entryId,
            update,
            request.UpdatedBy,
            cancellationToken);
    }

    public async Task<GoldCatalogEntryDetailResponse> UpdateAsync(
        Guid entryId,
        GoldCatalogUpdate update,
        string? updatedBy,
        CancellationToken cancellationToken = default)
    {
        var entry = await knowledgeRepository.GetGoldCatalogEntryAsync(entryId, cancellationToken);

        if (entry is null)
        {
            throw new GoldCatalogEntryNotFoundException(entryId);
        }

        var actor = NormalizeActor(updatedBy);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        entry.Overview = update.Overview;
        entry.SetupGuide = update.SetupGuide;
        entry.ReferencesJson = GoldCatalogJson.Serialize(ToReferenceResponses(update.References));
        entry.SupportedClientsJson = GoldCatalogJson.Serialize(update.SupportedClients);
        entry.UpdatedAtUtc = now;
        entry.UpdatedBy = actor;

        historyRepository.AddEntryHistory(ToEntryHistory(
            entry.Id,
            GoldEntryHistoryFactory.CreateCatalogUpdated(actor, now)));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetDetailAsync(entry.Id, cancellationToken))!;
    }

    public async Task<GoldTagUpdateResponse> ReplaceTagsAsync(
        Guid entryId,
        ReplaceGoldCatalogTagsRequest request,
        CancellationToken cancellationToken = default)
    {
        return await ReplaceTagsAsync(
            entryId,
            GoldTagSet.Create(request.Tags),
            request.UpdatedBy,
            cancellationToken);
    }

    public async Task<GoldTagUpdateResponse> ReplaceTagsAsync(
        Guid entryId,
        GoldTagSet tagSet,
        string? updatedBy,
        CancellationToken cancellationToken = default)
    {
        var entry = await knowledgeRepository.GetGoldCatalogEntryAsync(entryId, cancellationToken);

        if (entry is null)
        {
            throw new GoldCatalogEntryNotFoundException(entryId);
        }

        var actor = NormalizeActor(updatedBy);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        entry.TagsJson = GoldCatalogJson.Serialize(tagSet.Values);
        entry.UpdatedAtUtc = now;
        entry.UpdatedBy = actor;

        historyRepository.AddEntryHistory(ToEntryHistory(
            entry.Id,
            GoldEntryHistoryFactory.CreateTagsReplaced(actor, now, tagSet.Values.Count)));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new GoldTagUpdateResponse(entry.Id, tagSet.Values, now, actor);
    }

    public async Task<GoldPublishResult> PublishAsync(
        Guid silverId,
        string? publishedBy,
        CancellationToken cancellationToken = default)
    {
        var silverDraft = await knowledgeRepository.GetSilverDraftAsync(
            silverId,
            includeToolDrafts: true,
            cancellationToken);

        if (silverDraft is null)
        {
            throw new SilverDraftNotFoundException(silverId);
        }

        var bronzeSource = await knowledgeRepository.GetBronzeSourceAsync(
            silverDraft.BronzeSourceId,
            cancellationToken);

        var actor = NormalizeActor(publishedBy);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var existingEntry = await knowledgeRepository.GetGoldCatalogEntryBySilverDraftIdAsync(
            silverId,
            cancellationToken);
        var created = existingEntry is null;

        var entry = existingEntry ?? new GoldCatalogEntry
        {
            Id = Guid.NewGuid(),
            SilverServerDraftId = silverId,
            PublishedAtUtc = now,
            PublishedBy = actor
        };

        entry.DisplayName = silverDraft.Name;
        entry.Overview = silverDraft.Summary;
        entry.TagsJson = silverDraft.TagCandidatesJson;
        entry.SetupGuide = bronzeSource?.SourceUri is null
            ? "Setup guide pending curation."
            : $"See {bronzeSource.SourceUri} for setup details.";
        entry.ToolSummariesJson = GoldCatalogJson.Serialize(
            silverDraft.ToolDrafts
                .OrderBy(toolDraft => toolDraft.Position)
                .Select(toolDraft => new GoldToolSummaryResponse(
                    toolDraft.Name,
                    toolDraft.Description))
                .ToList());
        entry.ReferencesJson = GoldCatalogJson.Serialize(BuildReferences(bronzeSource));
        entry.SupportedClientsJson = GoldCatalogJson.Serialize(
            CatalogMetadataExtractor.DetectSupportedClients(bronzeSource?.RawContent));
        entry.UpdatedAtUtc = now;
        entry.UpdatedBy = actor;

        if (created)
        {
            knowledgeRepository.AddGoldCatalogEntry(entry);
        }

        historyRepository.AddEntryHistory(ToEntryHistory(
            entry.Id,
            GoldEntryHistoryFactory.CreatePublished(actor, now, created)));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var historyCount = (await historyRepository.GetEntryHistoriesAsync(entry.Id, cancellationToken)).Count;

        return new GoldPublishResult(
            ToDetailResponse(
                entry,
                historyCount,
                CatalogMetadataExtractor.DetectAuthenticationType(bronzeSource?.RawContent)),
            created);
    }

    public async Task<GoldCatalogEntryListResponse> ListAsync(
        int page = 1,
        int pageSize = 20,
        string? tag = null,
        string? authType = null,
        string? client = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var entries = await LoadCatalogProjectionsAsync(cancellationToken);
        var filtered = entries
            .Where(entry => MatchesOptionalTag(entry.Tags, tag))
            .Where(entry => MatchesOptionalValue(entry.AuthenticationType, authType))
            .Where(entry => MatchesOptionalClient(entry.SupportedClients, client))
            .OrderByDescending(entry => entry.Entry.UpdatedAtUtc)
            .ThenBy(entry => entry.Entry.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new GoldCatalogEntryListResponse(
            filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(entry => ToSummaryResponse(entry.Entry, entry.AuthenticationType))
                .ToList(),
            page,
            pageSize,
            filtered.Count);
    }

    public async Task<GoldCatalogEntryDetailResponse?> GetDetailAsync(
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        var projection = await LoadCatalogProjectionsAsync(cancellationToken);
        var entry = projection.FirstOrDefault(catalogEntry => catalogEntry.Entry.Id == entryId);

        if (entry is null)
        {
            return null;
        }

        var historyCount = (await historyRepository.GetEntryHistoriesAsync(entryId, cancellationToken)).Count;

        return ToDetailResponse(entry.Entry, historyCount, entry.AuthenticationType);
    }

    public async Task<EntryHistoryPageResponse?> GetHistoryAsync(
        Guid entryId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var exists = await knowledgeRepository.GetGoldCatalogEntryAsync(entryId, cancellationToken) is not null;

        if (!exists)
        {
            return null;
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var histories = await historyRepository.GetEntryHistoriesAsync(entryId, cancellationToken);

        if (histories is null)
        {
            throw new InvalidOperationException("History repository returned null entry histories collection.");
        }

        var items = histories
            .OrderByDescending(history => history.ChangedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(history => new EntryHistoryResponse(
                history.Id,
                history.Action,
                history.ChangedBy,
                history.ChangedAtUtc,
                history.Summary,
                history.UsedLlm))
            .ToList();

        var totalCount = histories.Count;

        return new EntryHistoryPageResponse(items, page, pageSize, totalCount);
    }

    private static GoldCatalogEntryDetailResponse ToDetailResponse(
        GoldCatalogEntry entry,
        int historyCount,
        string authenticationType)
    {
        return new GoldCatalogEntryDetailResponse(
            entry.Id,
            entry.SilverServerDraftId,
            entry.DisplayName,
            entry.Overview,
            GoldCatalogJson.DeserializeList<string>(entry.TagsJson),
            authenticationType,
            entry.SetupGuide,
            GoldCatalogJson.DeserializeList<GoldToolSummaryResponse>(entry.ToolSummariesJson),
            GoldCatalogJson.DeserializeList<GoldReferenceResponse>(entry.ReferencesJson),
            GoldCatalogJson.DeserializeList<string>(entry.SupportedClientsJson),
            historyCount,
            entry.PublishedAtUtc,
            entry.PublishedBy,
            entry.UpdatedAtUtc,
            entry.UpdatedBy);
    }

    private static GoldCatalogEntrySummaryResponse ToSummaryResponse(
        GoldCatalogEntry entry,
        string authenticationType)
    {
        return new GoldCatalogEntrySummaryResponse(
            entry.Id,
            entry.DisplayName,
            entry.Overview,
            GoldCatalogJson.DeserializeList<string>(entry.TagsJson),
            authenticationType,
            GoldCatalogJson.DeserializeList<string>(entry.SupportedClientsJson),
            entry.UpdatedAtUtc);
    }

    private static IReadOnlyList<GoldReferenceResponse> BuildReferences(BronzeSource? bronzeSource)
    {
        return bronzeSource?.SourceUri is null
            ? []
            :
            [
                new GoldReferenceResponse(
                    "source",
                    bronzeSource.SourceUri)
            ];
    }

    private static IReadOnlyList<GoldReferenceResponse> ToReferenceResponses(
        IReadOnlyList<string> references)
    {
        return references
            .Select(reference => new GoldReferenceResponse("reference", reference))
            .ToList();
    }

    private static EntryHistory ToEntryHistory(Guid entryId, GoldEntryHistoryDraft draft)
    {
        return new EntryHistory
        {
            Id = Guid.NewGuid(),
            EntryId = entryId,
            Action = draft.Action,
            ChangedBy = draft.ChangedBy,
            ChangedAtUtc = draft.ChangedAtUtc,
            Summary = draft.Summary,
            UsedLlm = draft.UsedLlm
        };
    }

    private static string NormalizeActor(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "system"
            : value.Trim();
    }

    private async Task<List<GoldCatalogProjection>> LoadCatalogProjectionsAsync(
        CancellationToken cancellationToken)
    {
        var rows = await knowledgeRepository.GetGoldCatalogProjectionRowsAsync(cancellationToken);

        if (rows is null)
        {
            throw new InvalidOperationException("Knowledge repository returned null gold catalog projections collection.");
        }

        return rows
            .Select(row => new GoldCatalogProjection(
                row.Entry,
                CatalogMetadataExtractor.DetectAuthenticationType(row.RawContent),
                GoldCatalogJson.DeserializeList<string>(row.Entry.TagsJson),
                GoldCatalogJson.DeserializeList<string>(row.Entry.SupportedClientsJson)))
            .ToList();
    }

    private static bool MatchesOptionalTag(IReadOnlyList<string> tags, string? tag)
    {
        var normalizedTag = NormalizeOptional(tag);
        return normalizedTag is null
            || tags.Any(value => string.Equals(value, normalizedTag, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesOptionalValue(string value, string? filter)
    {
        var normalizedFilter = NormalizeOptional(filter);
        return normalizedFilter is null
            || string.Equals(value, normalizedFilter, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesOptionalClient(IReadOnlyList<string> supportedClients, string? client)
    {
        var normalizedClient = NormalizeOptional(client);
        return normalizedClient is null
            || supportedClients.Any(value => string.Equals(value, normalizedClient, StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private sealed record GoldCatalogProjection(
        GoldCatalogEntry Entry,
        string AuthenticationType,
        IReadOnlyList<string> Tags,
        IReadOnlyList<string> SupportedClients);
}