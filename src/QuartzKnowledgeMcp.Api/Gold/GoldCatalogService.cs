using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Persistence;
using QuartzKnowledgeMcp.Api.Silver;

namespace QuartzKnowledgeMcp.Api.Gold;

public sealed class GoldCatalogService(
    McpKnowledgeDbContext dbContext,
    TimeProvider timeProvider)
{
    public async Task<GoldPublishResult> PublishAsync(
        Guid silverId,
        string? publishedBy,
        CancellationToken cancellationToken = default)
    {
        var silverDraft = await dbContext.SilverServerDrafts
            .Include(draft => draft.ToolDrafts)
            .FirstOrDefaultAsync(draft => draft.Id == silverId, cancellationToken);

        if (silverDraft is null)
        {
            throw new SilverDraftNotFoundException(silverId);
        }

        var bronzeSource = await dbContext.BronzeSources
            .AsNoTracking()
            .FirstOrDefaultAsync(source => source.Id == silverDraft.BronzeSourceId, cancellationToken);

        var actor = NormalizeActor(publishedBy);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var existingEntry = await dbContext.GoldCatalogEntries
            .FirstOrDefaultAsync(entry => entry.SilverServerDraftId == silverId, cancellationToken);
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
        entry.ToolSummariesJson = JsonSerializer.Serialize(
            silverDraft.ToolDrafts
                .OrderBy(toolDraft => toolDraft.Position)
                .Select(toolDraft => new GoldToolSummaryResponse(
                    toolDraft.Name,
                    toolDraft.Description))
                .ToList());
        entry.ReferencesJson = JsonSerializer.Serialize(BuildReferences(bronzeSource));
        entry.SupportedClientsJson = "[]";
        entry.UpdatedAtUtc = now;
        entry.UpdatedBy = actor;

        if (created)
        {
            dbContext.GoldCatalogEntries.Add(entry);
        }

        dbContext.EntryHistories.Add(new EntryHistory
        {
            Id = Guid.NewGuid(),
            EntryId = entry.Id,
            Action = created ? EntryHistoryActions.Published : EntryHistoryActions.Republished,
            ChangedBy = actor,
            ChangedAtUtc = now,
            Summary = created
                ? "Initial publish from silver draft."
                : "Published entry refreshed from silver draft.",
            UsedLlm = false
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var historyCount = await dbContext.EntryHistories
            .AsNoTracking()
            .CountAsync(history => history.EntryId == entry.Id, cancellationToken);

        return new GoldPublishResult(
            ToDetailResponse(entry, historyCount),
            created);
    }

    public async Task<GoldCatalogEntryDetailResponse?> GetDetailAsync(
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        var entry = await dbContext.GoldCatalogEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(catalogEntry => catalogEntry.Id == entryId, cancellationToken);

        if (entry is null)
        {
            return null;
        }

        var historyCount = await dbContext.EntryHistories
            .AsNoTracking()
            .CountAsync(history => history.EntryId == entryId, cancellationToken);

        return ToDetailResponse(entry, historyCount);
    }

    private static GoldCatalogEntryDetailResponse ToDetailResponse(
        GoldCatalogEntry entry,
        int historyCount)
    {
        return new GoldCatalogEntryDetailResponse(
            entry.Id,
            entry.SilverServerDraftId,
            entry.DisplayName,
            entry.Overview,
            DeserializeJson<List<string>>(entry.TagsJson),
            entry.SetupGuide,
            DeserializeJson<List<GoldToolSummaryResponse>>(entry.ToolSummariesJson),
            DeserializeJson<List<GoldReferenceResponse>>(entry.ReferencesJson),
            DeserializeJson<List<string>>(entry.SupportedClientsJson),
            historyCount,
            entry.PublishedAtUtc,
            entry.PublishedBy,
            entry.UpdatedAtUtc,
            entry.UpdatedBy);
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

    private static T DeserializeJson<T>(string json)
        where T : class, new()
    {
        return JsonSerializer.Deserialize<T>(json)
            ?? new T();
    }

    private static string NormalizeActor(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "system"
            : value.Trim();
    }
}