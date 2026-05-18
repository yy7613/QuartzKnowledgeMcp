using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Persistence;

namespace QuartzKnowledgeMcp.Api.Silver;

public sealed class SilverDraftService(
    McpKnowledgeDbContext dbContext,
    RuleBasedSilverNormalizer normalizer,
    TimeProvider timeProvider)
{
    public async Task<SilverOrganizeResult> OrganizeAsync(
        Guid bronzeId,
        string? mode,
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

        var bronzeSource = await dbContext.BronzeSources
            .FirstOrDefaultAsync(source => source.Id == bronzeId, cancellationToken);

        if (bronzeSource is null)
        {
            throw new BronzeSourceNotFoundException(bronzeId);
        }

        SilverServerDraftContent normalizedDraft;
        try
        {
            normalizedDraft = normalizer.Normalize(bronzeSource);
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

        var existingDraft = await dbContext.SilverServerDrafts
            .Include(draft => draft.ToolDrafts)
            .FirstOrDefaultAsync(draft => draft.BronzeSourceId == bronzeId, cancellationToken);

        var draft = existingDraft ?? new SilverServerDraft
        {
            Id = Guid.NewGuid(),
            BronzeSourceId = bronzeId
        };

        draft.Name = normalizedDraft.Name;
        draft.Summary = normalizedDraft.Summary;
        draft.TagCandidatesJson = SerializeTags(normalizedDraft.TagCandidates);
        draft.OrganizedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        draft.ToolDrafts.Clear();
        foreach (var (toolDraft, index) in normalizedDraft.ToolDrafts
            .DistinctBy(tool => tool.Name, StringComparer.OrdinalIgnoreCase)
            .Select((tool, index) => (tool, index)))
        {
            draft.ToolDrafts.Add(new SilverToolDraft
            {
                Id = Guid.NewGuid(),
                Name = toolDraft.Name,
                Description = toolDraft.Description,
                Position = index
            });
        }

        if (existingDraft is null)
        {
            dbContext.SilverServerDrafts.Add(draft);
        }

        bronzeSource.Status = BronzeSourceStatuses.Organized;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new SilverOrganizeResult(draft, existingDraft is null);
    }

    public async Task<SilverServerDraftListResponse> ListAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.SilverServerDrafts.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(draft => draft.OrganizedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(draft => new
            {
                draft.Id,
                draft.BronzeSourceId,
                draft.Name,
                draft.Summary,
                draft.TagCandidatesJson,
                ToolCount = draft.ToolDrafts.Count,
                draft.OrganizedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new SilverServerDraftListResponse(
            items.Select(item => new SilverServerDraftResponse(
                item.Id,
                item.BronzeSourceId,
                item.Name,
                item.Summary,
                DeserializeTags(item.TagCandidatesJson),
                item.ToolCount,
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
        var draft = await dbContext.SilverServerDrafts
            .AsNoTracking()
            .Include(serverDraft => serverDraft.ToolDrafts)
            .FirstOrDefaultAsync(serverDraft => serverDraft.Id == id, cancellationToken);

        return draft is null
            ? null
            : ToDetailResponse(draft);
    }

    public static SilverServerDraftDetailResponse ToDetailResponse(SilverServerDraft draft)
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
            draft.OrganizedAtUtc);
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