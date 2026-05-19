using Microsoft.EntityFrameworkCore;
using QuartzKnowledgeMcp.Api.Gold;
using QuartzKnowledgeMcp.Api.Persistence;

namespace QuartzKnowledgeMcp.Api.Search;

public sealed class CatalogSearchService(McpKnowledgeDbContext dbContext)
{
    public Task<CatalogSearchResultResponse> QueryAsync(
        SearchQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(
            request.Query,
            request.Tags,
            request.AuthType,
            request.Client,
            request.Sort,
            request.Page ?? 1,
            request.PageSize ?? 20,
            cancellationToken);
    }

    public async Task<CatalogSearchResultResponse> SearchAsync(
        string? keyword,
        IReadOnlyList<string>? tags,
        string? authType,
        string? client,
        string? sort,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var filtered = await FilterCandidatesAsync(
            keyword,
            tags,
            authType,
            client,
            cancellationToken);

        var ordered = ApplySort(filtered, sort, Tokenize(keyword).Count > 0);
        var totalCount = ordered.Count;
        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(candidate => new CatalogSearchItemResponse(
                candidate.Candidate.Id,
                candidate.Candidate.DisplayName,
                candidate.Candidate.Overview,
                candidate.Candidate.Tags,
                candidate.Candidate.AuthenticationType,
                candidate.Candidate.SupportedClients,
                candidate.Candidate.UpdatedAtUtc,
                candidate.Score))
            .ToList();

        return new CatalogSearchResultResponse(items, page, pageSize, totalCount);
    }

    public async Task<SearchSuggestionResultResponse> GetSuggestionsAsync(
        string? query,
        int limit = 10,
        string? scope = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new SearchSuggestionResultResponse([]);
        }

        limit = Math.Clamp(limit, 1, 20);
        var normalizedQuery = query.Trim();
        var normalizedScope = NormalizeScope(scope);
        var candidates = await LoadCandidatesAsync(cancellationToken);
        var suggestions = new Dictionary<string, SuggestionAccumulator>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            AddSuggestion(suggestions, candidate.DisplayName, "name", normalizedQuery, normalizedScope);

            foreach (var tag in candidate.Tags)
            {
                AddSuggestion(suggestions, tag, "tag", normalizedQuery, normalizedScope);
            }

            foreach (var toolName in candidate.ToolNames)
            {
                AddSuggestion(suggestions, toolName, "tool", normalizedQuery, normalizedScope);
            }

            foreach (var supportedClient in candidate.SupportedClients)
            {
                AddSuggestion(suggestions, supportedClient, "client", normalizedQuery, normalizedScope);
            }
        }

        var items = suggestions.Values
            .Select(accumulator => new SearchSuggestionItemResponse(
                accumulator.Value,
                accumulator.Type,
                accumulator.Score))
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Type, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Value, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();

        return new SearchSuggestionResultResponse(items);
    }

    public async Task<SearchFacetResultResponse> GetFacetsAsync(
        string? keyword,
        IReadOnlyList<string>? tags,
        string? authType,
        string? client,
        CancellationToken cancellationToken = default)
    {
        var filtered = await FilterCandidatesAsync(
            keyword,
            tags,
            authType,
            client,
            cancellationToken);
        var candidates = filtered.Select(item => item.Candidate).ToList();

        return new SearchFacetResultResponse(
            ToFacetItems(candidates.SelectMany(candidate => candidate.Tags)),
            ToFacetItems(candidates.Select(candidate => candidate.AuthenticationType)),
            ToFacetItems(candidates.SelectMany(candidate => candidate.SupportedClients)));
    }

    private async Task<List<SearchCandidate>> LoadCandidatesAsync(CancellationToken cancellationToken)
    {
        var rows = await (
            from entry in dbContext.GoldCatalogEntries.AsNoTracking()
            join silver in dbContext.SilverServerDrafts.AsNoTracking()
                on entry.SilverServerDraftId equals silver.Id
            join bronze in dbContext.BronzeSources.AsNoTracking()
                on silver.BronzeSourceId equals bronze.Id
            select new
            {
                entry.Id,
                entry.DisplayName,
                entry.Overview,
                entry.TagsJson,
                entry.ToolSummariesJson,
                entry.SupportedClientsJson,
                entry.UpdatedAtUtc,
                bronze.RawContent
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new SearchCandidate(
                row.Id,
                row.DisplayName,
                row.Overview,
                GoldCatalogJson.DeserializeList<string>(row.TagsJson),
                CatalogMetadataExtractor.DetectAuthenticationType(row.RawContent),
                GoldCatalogJson.DeserializeList<string>(row.SupportedClientsJson),
                GoldCatalogJson.DeserializeList<GoldToolSummaryResponse>(row.ToolSummariesJson)
                    .Select(tool => tool.Name)
                    .ToList(),
                row.UpdatedAtUtc))
            .ToList();
    }

    private static IReadOnlyList<SearchCandidateWithScore> ApplySort(
        IReadOnlyList<SearchCandidateWithScore> candidates,
        string? sort,
        bool hasKeyword)
    {
        var normalizedSort = NormalizeOptional(sort) ?? (hasKeyword ? "relevance" : "updated-desc");

        return normalizedSort switch
        {
            "name-asc" => candidates
                .OrderBy(candidate => candidate.Candidate.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(candidate => candidate.Candidate.UpdatedAtUtc)
                .ToList(),
            "updated-desc" => candidates
                .OrderByDescending(candidate => candidate.Candidate.UpdatedAtUtc)
                .ThenBy(candidate => candidate.Candidate.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            _ => candidates
                .OrderByDescending(candidate => candidate.Score)
                .ThenByDescending(candidate => candidate.Candidate.UpdatedAtUtc)
                .ThenBy(candidate => candidate.Candidate.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private async Task<List<SearchCandidateWithScore>> FilterCandidatesAsync(
        string? keyword,
        IReadOnlyList<string>? tags,
        string? authType,
        string? client,
        CancellationToken cancellationToken)
    {
        var normalizedTags = (tags ?? [])
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .ToList();
        var normalizedAuthType = NormalizeOptional(authType);
        var normalizedClient = NormalizeOptional(client);
        var tokens = Tokenize(keyword);

        var candidates = await LoadCandidatesAsync(cancellationToken);
        return candidates
            .Where(candidate => MatchesTags(candidate.Tags, normalizedTags))
            .Where(candidate => normalizedAuthType is null
                || string.Equals(candidate.AuthenticationType, normalizedAuthType, StringComparison.OrdinalIgnoreCase))
            .Where(candidate => normalizedClient is null
                || candidate.SupportedClients.Any(value => string.Equals(value, normalizedClient, StringComparison.OrdinalIgnoreCase)))
            .Select(candidate => new SearchCandidateWithScore(candidate, ComputeScore(candidate, tokens)))
            .Where(candidate => tokens.Count == 0 || candidate.Score > 0)
            .ToList();
    }

    private static decimal ComputeScore(SearchCandidate candidate, IReadOnlyList<string> tokens)
    {
        if (tokens.Count == 0)
        {
            return 0m;
        }

        decimal score = 0;
        foreach (var token in tokens)
        {
            if (candidate.DisplayName.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                score += 3m;
            }

            if (candidate.Overview.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                score += 2m;
            }

            if (candidate.Tags.Any(tag => tag.Contains(token, StringComparison.OrdinalIgnoreCase)))
            {
                score += 1m;
            }

            if (candidate.ToolNames.Any(toolName => toolName.Contains(token, StringComparison.OrdinalIgnoreCase)))
            {
                score += 1m;
            }
        }

        return score;
    }

    private static bool MatchesTags(IReadOnlyList<string> candidateTags, IReadOnlyList<string> tags)
    {
        return tags.Count == 0
            || tags.All(tag => candidateTags.Any(candidateTag =>
                string.Equals(candidateTag, tag, StringComparison.OrdinalIgnoreCase)));
    }

    private static List<string> Tokenize(string? keyword)
    {
        return string.IsNullOrWhiteSpace(keyword)
            ? []
            : keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(token => token.Trim())
                .ToList();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLowerInvariant();
    }

    private static string NormalizeScope(string? scope)
    {
        var normalized = NormalizeOptional(scope);
        return normalized is "name" or "tag" or "tool" or "client"
            ? normalized
            : "all";
    }

    private static void AddSuggestion(
        IDictionary<string, SuggestionAccumulator> suggestions,
        string value,
        string type,
        string query,
        string scope)
    {
        if ((scope != "all" && !string.Equals(scope, type, StringComparison.OrdinalIgnoreCase))
            || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var score = ComputeSuggestionScore(value, query, type);
        if (score <= 0)
        {
            return;
        }

        var key = $"{type}:{value.Trim().ToLowerInvariant()}";
        if (suggestions.TryGetValue(key, out var existing))
        {
            suggestions[key] = existing with
            {
                Score = Math.Max(existing.Score, score),
                Occurrences = existing.Occurrences + 1
            };
            return;
        }

        suggestions[key] = new SuggestionAccumulator(value.Trim(), type, score, 1);
    }

    private static decimal ComputeSuggestionScore(string value, string query, string type)
    {
        if (value.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 1.00m + TypeBonus(type);
        }

        if (value.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return 0.70m + TypeBonus(type);
        }

        return 0m;
    }

    private static decimal TypeBonus(string type)
    {
        return type switch
        {
            "name" => 0.08m,
            "tag" => 0.06m,
            "tool" => 0.04m,
            "client" => 0.02m,
            _ => 0m
        };
    }

    private static IReadOnlyList<SearchFacetItemResponse> ToFacetItems(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new SearchFacetItemResponse(group.First(), group.Count()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed record SearchCandidate(
        Guid Id,
        string DisplayName,
        string Overview,
        IReadOnlyList<string> Tags,
        string AuthenticationType,
        IReadOnlyList<string> SupportedClients,
        IReadOnlyList<string> ToolNames,
        DateTime UpdatedAtUtc);

    private sealed record SearchCandidateWithScore(
        SearchCandidate Candidate,
        decimal Score);

    private sealed record SuggestionAccumulator(
        string Value,
        string Type,
        decimal Score,
        int Occurrences);
}