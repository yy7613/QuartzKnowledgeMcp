using Microsoft.Extensions.Options;
using QuartzKnowledgeMcp.Api.Domain.Ports;
using QuartzKnowledgeMcp.Api.Embedding;

namespace QuartzKnowledgeMcp.Api.Search;

public sealed class StructuredRelationProjector(
    IEmbeddingGenerator embeddingGenerator,
    IOptions<EmbeddingOptions> embeddingOptions) : IRelationProjector
{
    public async Task<IReadOnlyList<RelatedCatalogEntryProjection>> ProjectAsync(
        RelationProjectorDocument source,
        IReadOnlyList<RelationProjectorDocument> candidates,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 20);

        IReadOnlyList<float>? sourceEmbedding = null;
        if (embeddingOptions.Value.IsConfigured)
        {
            sourceEmbedding = await embeddingGenerator.GenerateAsync(BuildEmbeddingText(source), cancellationToken);
        }

        var results = new List<RelatedCatalogEntryProjection>();
        foreach (var candidate in candidates)
        {
            if (candidate.EntryId == source.EntryId)
            {
                continue;
            }

            var sharedTags = Intersect(source.Tags, candidate.Tags);
            var sharedTools = Intersect(source.ToolNames, candidate.ToolNames);
            var sharedClients = Intersect(source.SupportedClients, candidate.SupportedClients);
            var structuredScore = (sharedTags.Count * 3m) + (sharedTools.Count * 2m) + sharedClients.Count;

            if (structuredScore <= 0m)
            {
                continue;
            }

            var score = structuredScore;
            if (sourceEmbedding is not null && sourceEmbedding.Count > 0)
            {
                var candidateEmbedding = await embeddingGenerator.GenerateAsync(BuildEmbeddingText(candidate), cancellationToken);
                score += ComputeEmbeddingBoost(sourceEmbedding, candidateEmbedding);
            }

            results.Add(new RelatedCatalogEntryProjection(
                candidate.EntryId,
                candidate.DisplayName,
                candidate.Overview,
                sharedTags,
                sharedTools,
                sharedClients,
                score));
        }

        return results
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    private static IReadOnlyList<string> Intersect(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        return left
            .Where(leftValue => right.Any(rightValue =>
                string.Equals(leftValue, rightValue, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static decimal ComputeEmbeddingBoost(
        IReadOnlyList<float> sourceEmbedding,
        IReadOnlyList<float> candidateEmbedding)
    {
        if (sourceEmbedding.Count == 0
            || sourceEmbedding.Count != candidateEmbedding.Count)
        {
            return 0m;
        }

        double dot = 0;
        double sourceMagnitude = 0;
        double candidateMagnitude = 0;

        for (var index = 0; index < sourceEmbedding.Count; index++)
        {
            dot += sourceEmbedding[index] * candidateEmbedding[index];
            sourceMagnitude += sourceEmbedding[index] * sourceEmbedding[index];
            candidateMagnitude += candidateEmbedding[index] * candidateEmbedding[index];
        }

        if (sourceMagnitude <= 0 || candidateMagnitude <= 0)
        {
            return 0m;
        }

        var cosine = dot / (Math.Sqrt(sourceMagnitude) * Math.Sqrt(candidateMagnitude));
        return Math.Max(0m, (decimal)cosine * 0.25m);
    }

    private static string BuildEmbeddingText(RelationProjectorDocument document)
    {
        return string.Join(
            Environment.NewLine,
            document.DisplayName,
            document.Overview,
            string.Join(", ", document.Tags),
            string.Join(", ", document.ToolNames),
            string.Join(", ", document.SupportedClients));
    }
}