using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuartzKnowledgeMcp.Api.Persistence;

namespace QuartzKnowledgeMcp.Api.Dashboard;

public sealed class DashboardService(
    McpKnowledgeDbContext dbContext,
    TimeProvider timeProvider)
{
    private const int TrendWindowDays = 7;

    public async Task<DashboardSummaryResponse> GetSummaryAsync(
        int recentPerStage = 8,
        CancellationToken cancellationToken = default)
    {
        recentPerStage = Math.Clamp(recentPerStage, 1, 20);
        var generatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        var bronzeRows = await dbContext.BronzeSources
            .AsNoTracking()
            .Select(source => new BronzeRow(
                source.Id,
                source.SourceType,
                source.SourceUri,
                source.RawContent,
                source.Status,
                source.ImportedAtUtc))
            .ToListAsync(cancellationToken);

        var silverRows = await dbContext.SilverServerDrafts
            .AsNoTracking()
            .Select(draft => new SilverRow(
                draft.Id,
                draft.Name,
                draft.Summary,
                draft.TagCandidatesJson,
                draft.OrganizedAtUtc))
            .ToListAsync(cancellationToken);

        var goldRows = await dbContext.GoldCatalogEntries
            .AsNoTracking()
            .Select(entry => new GoldRow(
                entry.Id,
                entry.DisplayName,
                entry.Overview,
                entry.TagsJson,
                entry.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        var bronzeSummary = BuildBronzeSummary(bronzeRows, recentPerStage, generatedAtUtc);
        var silverSummary = BuildSilverSummary(silverRows, recentPerStage, generatedAtUtc);
        var goldSummary = BuildGoldSummary(goldRows, recentPerStage, generatedAtUtc);

        var allTags = goldRows
            .SelectMany(row => DeserializeStringList(row.TagsJson))
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .GroupBy(tag => tag.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new DashboardCountItemResponse(group.First().Trim(), group.Count()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var latestActivityAtUtc = new[]
        {
            bronzeSummary.LatestActivityAtUtc,
            silverSummary.LatestActivityAtUtc,
            goldSummary.LatestActivityAtUtc
        }
        .Where(value => value.HasValue)
        .Select(value => value!.Value)
        .DefaultIfEmpty()
        .Max();

        return new DashboardSummaryResponse(
            generatedAtUtc,
            new DashboardOverviewResponse(
                bronzeRows.Count + silverRows.Count + goldRows.Count,
                allTags.Count,
                latestActivityAtUtc == default ? null : latestActivityAtUtc),
            bronzeSummary,
            silverSummary,
            goldSummary,
            new DashboardTagListResponse(allTags.Count, allTags));
    }

    public async Task<DashboardSearchResultResponse> SearchAsync(
        string? query,
        string? stage = null,
        string? tag = null,
        string? freshness = null,
        string? sort = null,
        int limit = 30,
        CancellationToken cancellationToken = default)
    {
        query = query?.Trim() ?? string.Empty;
        tag = tag?.Trim();
        stage = NormalizeStage(stage);
        freshness = NormalizeFreshness(freshness);
        sort = NormalizeSort(sort);
        limit = Math.Clamp(limit, 1, 100);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hasTextQuery = !string.IsNullOrWhiteSpace(query);

        if (!hasTextQuery && string.IsNullOrWhiteSpace(tag) && freshness is null && stage is null)
        {
            return new DashboardSearchResultResponse(query, stage, tag, freshness, sort, 0, []);
        }

        var pattern = hasTextQuery ? $"%{query}%" : null;
        var items = new List<DashboardSearchItemResponse>();

        if (stage is null or "bronze")
        {
            var bronzeQuery = dbContext.BronzeSources.AsNoTracking();
            if (hasTextQuery)
            {
                bronzeQuery = bronzeQuery.Where(source =>
                    EF.Functions.Like(source.SourceType, pattern!) ||
                    (source.SourceUri != null && EF.Functions.Like(source.SourceUri, pattern!)) ||
                    EF.Functions.Like(source.RawContent, pattern!) ||
                    EF.Functions.Like(source.Status, pattern!));
            }

            var bronzeItems = await bronzeQuery
                .Select(source => new
                {
                    source.Id,
                    source.SourceType,
                    source.SourceUri,
                    source.RawContent,
                    source.Status,
                    source.ImportedAtUtc
                })
                .ToListAsync(cancellationToken);

            items.AddRange(bronzeItems.Select(item => new DashboardSearchItemResponse(
                item.Id,
                "bronze",
                item.SourceUri ?? item.SourceType,
                BuildBronzeSummaryText(item.RawContent),
                item.Status,
                [],
                item.ImportedAtUtc,
                BuildDetailPath("bronze", item.Id),
                BuildFreshnessBucket(item.ImportedAtUtc, now))));
        }

        if (stage is null or "silver")
        {
            var silverQuery = dbContext.SilverServerDrafts.AsNoTracking();
            if (hasTextQuery)
            {
                silverQuery = silverQuery.Where(draft =>
                    EF.Functions.Like(draft.Name, pattern!) ||
                    EF.Functions.Like(draft.Summary, pattern!) ||
                    EF.Functions.Like(draft.TagCandidatesJson, pattern!));
            }

            var silverItems = await silverQuery
                .Select(draft => new
                {
                    draft.Id,
                    draft.Name,
                    draft.Summary,
                    draft.TagCandidatesJson,
                    draft.OrganizedAtUtc
                })
                .ToListAsync(cancellationToken);

            items.AddRange(silverItems.Select(item => new DashboardSearchItemResponse(
                item.Id,
                "silver",
                item.Name,
                item.Summary,
                "draft",
                DeserializeStringList(item.TagCandidatesJson),
                item.OrganizedAtUtc,
                BuildDetailPath("silver", item.Id),
                BuildFreshnessBucket(item.OrganizedAtUtc, now))));
        }

        if (stage is null or "gold")
        {
            var goldQuery = dbContext.GoldCatalogEntries.AsNoTracking();
            if (hasTextQuery)
            {
                goldQuery = goldQuery.Where(entry =>
                    EF.Functions.Like(entry.DisplayName, pattern!) ||
                    EF.Functions.Like(entry.Overview, pattern!) ||
                    EF.Functions.Like(entry.TagsJson, pattern!) ||
                    EF.Functions.Like(entry.SupportedClientsJson, pattern!));
            }

            var goldItems = await goldQuery
                .Select(entry => new
                {
                    entry.Id,
                    entry.DisplayName,
                    entry.Overview,
                    entry.TagsJson,
                    entry.UpdatedAtUtc
                })
                .ToListAsync(cancellationToken);

            items.AddRange(goldItems.Select(item => new DashboardSearchItemResponse(
                item.Id,
                "gold",
                item.DisplayName,
                item.Overview,
                "published",
                DeserializeStringList(item.TagsJson),
                item.UpdatedAtUtc,
                BuildDetailPath("gold", item.Id),
                BuildFreshnessBucket(item.UpdatedAtUtc, now))));
        }

        var filtered = items.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(tag))
        {
            filtered = filtered.Where(item =>
                item.Tags.Any(candidate => string.Equals(candidate, tag, StringComparison.OrdinalIgnoreCase)));
        }

        if (freshness is not null)
        {
            filtered = filtered.Where(item =>
                string.Equals(item.FreshnessBucket, freshness, StringComparison.OrdinalIgnoreCase));
        }

        var filteredList = filtered.ToList();
        var ordered = OrderItems(filteredList, sort)
            .Take(limit)
            .ToList();

        return new DashboardSearchResultResponse(query, stage, tag, freshness, sort, filteredList.Count, ordered);
    }

    private static DashboardStageSummaryResponse BuildBronzeSummary(
        IReadOnlyList<BronzeRow> rows,
        int recentPerStage,
        DateTime now)
    {
        var breakdown = rows
            .GroupBy(row => row.Status, StringComparer.OrdinalIgnoreCase)
            .Select(group => new DashboardCountItemResponse(group.Key, group.Count()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var recentItems = rows
            .OrderByDescending(row => row.ImportedAtUtc)
            .Take(recentPerStage)
            .Select(row => new DashboardRecentItemResponse(
                row.Id,
                row.SourceUri ?? row.SourceType,
                row.SourceType,
                row.Status,
                [],
                row.ImportedAtUtc,
                BuildDetailPath("bronze", row.Id)))
            .ToList();

        return new DashboardStageSummaryResponse(
            "bronze",
            rows.Count,
            rows.Count == 0 ? null : rows.Max(row => row.ImportedAtUtc),
            BuildFreshness(rows.Select(row => row.ImportedAtUtc), now),
            breakdown,
            BuildTrend(rows.Select(row => row.ImportedAtUtc), now),
            recentItems);
    }

    private static DashboardStageSummaryResponse BuildSilverSummary(
        IReadOnlyList<SilverRow> rows,
        int recentPerStage,
        DateTime now)
    {
        var recentItems = rows
            .OrderByDescending(row => row.OrganizedAtUtc)
            .Take(recentPerStage)
            .Select(row => new DashboardRecentItemResponse(
                row.Id,
                row.Name,
                row.Summary,
                "draft",
                DeserializeStringList(row.TagCandidatesJson),
                row.OrganizedAtUtc,
                BuildDetailPath("silver", row.Id)))
            .ToList();

        return new DashboardStageSummaryResponse(
            "silver",
            rows.Count,
            rows.Count == 0 ? null : rows.Max(row => row.OrganizedAtUtc),
            BuildFreshness(rows.Select(row => row.OrganizedAtUtc), now),
            [],
            BuildTrend(rows.Select(row => row.OrganizedAtUtc), now),
            recentItems);
    }

    private static DashboardStageSummaryResponse BuildGoldSummary(
        IReadOnlyList<GoldRow> rows,
        int recentPerStage,
        DateTime now)
    {
        var recentItems = rows
            .OrderByDescending(row => row.UpdatedAtUtc)
            .Take(recentPerStage)
            .Select(row => new DashboardRecentItemResponse(
                row.Id,
                row.DisplayName,
                row.Overview,
                "published",
                DeserializeStringList(row.TagsJson),
                row.UpdatedAtUtc,
                BuildDetailPath("gold", row.Id)))
            .ToList();

        return new DashboardStageSummaryResponse(
            "gold",
            rows.Count,
            rows.Count == 0 ? null : rows.Max(row => row.UpdatedAtUtc),
            BuildFreshness(rows.Select(row => row.UpdatedAtUtc), now),
            [],
            BuildTrend(rows.Select(row => row.UpdatedAtUtc), now),
            recentItems);
    }

    private static IReadOnlyList<DashboardTrendPointResponse> BuildTrend(
        IEnumerable<DateTime> timestamps,
        DateTime now)
    {
        var grouped = timestamps
            .GroupBy(timestamp => DateOnly.FromDateTime(timestamp))
            .ToDictionary(group => group.Key, group => group.Count());
        var endDay = DateOnly.FromDateTime(now);
        var startDay = endDay.AddDays(-(TrendWindowDays - 1));

        return Enumerable.Range(0, TrendWindowDays)
            .Select(offset => startDay.AddDays(offset))
            .Select(day => new DashboardTrendPointResponse(
                day.ToString("yyyy-MM-dd"),
                grouped.GetValueOrDefault(day, 0)))
            .ToList();
    }

    private static DashboardFreshnessResponse BuildFreshness(
        IEnumerable<DateTime> timestamps,
        DateTime now)
    {
        var timestampList = timestamps.ToList();
        var last24Cutoff = now.AddHours(-24);
        var last7Cutoff = now.AddDays(-7);
        var last24 = timestampList.Count(timestamp => timestamp >= last24Cutoff);
        var last7 = timestampList.Count(timestamp => timestamp < last24Cutoff && timestamp >= last7Cutoff);
        var older = timestampList.Count - last24 - last7;

        return new DashboardFreshnessResponse(last24, last7, older);
    }

    private static IReadOnlyList<string> DeserializeStringList(string json)
    {
        return JsonSerializer.Deserialize<List<string>>(json) ?? [];
    }

    private static string BuildBronzeSummaryText(string rawContent)
    {
        var normalized = rawContent
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();

        return normalized.Length <= 160
            ? normalized
            : normalized[..157] + "...";
    }

    private static IEnumerable<DashboardSearchItemResponse> OrderItems(
        IEnumerable<DashboardSearchItemResponse> items,
        string sort)
    {
        return sort switch
        {
            "oldest" => items
                .OrderBy(item => item.TimestampUtc)
                .ThenBy(item => item.Stage, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase),
            "title" => items
                .OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(item => item.TimestampUtc)
                .ThenBy(item => item.Stage, StringComparer.OrdinalIgnoreCase),
            "stage" => items
                .OrderBy(item => item.Stage, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(item => item.TimestampUtc)
                .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase),
            _ => items
                .OrderByDescending(item => item.TimestampUtc)
                .ThenBy(item => item.Stage, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static string BuildDetailPath(string stage, Guid id)
    {
        return stage switch
        {
            "bronze" => $"/api/bronze/sources/{id}",
            "silver" => $"/api/silver/server-drafts/{id}",
            _ => $"/api/gold/catalog/{id}"
        };
    }

    private static string BuildFreshnessBucket(DateTime timestamp, DateTime now)
    {
        var last24Cutoff = now.AddHours(-24);
        var last7Cutoff = now.AddDays(-7);
        if (timestamp >= last24Cutoff)
        {
            return "24h";
        }

        return timestamp >= last7Cutoff
            ? "7d"
            : "older";
    }

    private static string? NormalizeStage(string? stage)
    {
        if (string.IsNullOrWhiteSpace(stage))
        {
            return null;
        }

        return stage.Trim().ToLowerInvariant() switch
        {
            "bronze" => "bronze",
            "silver" => "silver",
            "gold" => "gold",
            _ => null
        };
    }

    private static string? NormalizeFreshness(string? freshness)
    {
        if (string.IsNullOrWhiteSpace(freshness))
        {
            return null;
        }

        return freshness.Trim().ToLowerInvariant() switch
        {
            "24h" => "24h",
            "7d" => "7d",
            "older" => "older",
            _ => null
        };
    }

    private static string NormalizeSort(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return "newest";
        }

        return sort.Trim().ToLowerInvariant() switch
        {
            "oldest" => "oldest",
            "title" => "title",
            "stage" => "stage",
            _ => "newest"
        };
    }

    private sealed record BronzeRow(
        Guid Id,
        string SourceType,
        string? SourceUri,
        string RawContent,
        string Status,
        DateTime ImportedAtUtc);

    private sealed record SilverRow(
        Guid Id,
        string Name,
        string Summary,
        string TagCandidatesJson,
        DateTime OrganizedAtUtc);

    private sealed record GoldRow(
        Guid Id,
        string DisplayName,
        string Overview,
        string TagsJson,
        DateTime UpdatedAtUtc);
}