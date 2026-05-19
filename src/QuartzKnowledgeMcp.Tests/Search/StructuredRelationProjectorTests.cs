using Microsoft.Extensions.Options;
using QuartzKnowledgeMcp.Api.Domain.Ports;
using QuartzKnowledgeMcp.Api.Embedding;
using QuartzKnowledgeMcp.Api.Search;

namespace QuartzKnowledgeMcp.Tests.Search;

public class StructuredRelationProjectorTests
{
    [Fact]
    public async Task ProjectAsync_ComputesRelatedEntries_WhenEmbeddingIsDisabled()
    {
        var projector = new StructuredRelationProjector(
            new NoOpEmbeddingGenerator(),
            Options.Create(new EmbeddingOptions
            {
                Enabled = false,
                Provider = "none"
            }));

        var source = new RelationProjectorDocument(
            Guid.NewGuid(),
            "Registry Search",
            "Search registry entries.",
            ["github", "search"],
            ["search-docs", "sync-issues"],
            ["VS Code", "Claude Desktop"],
            new DateTime(2026, 5, 18, 10, 0, 0, DateTimeKind.Utc));
        var related = new RelationProjectorDocument(
            Guid.NewGuid(),
            "Registry Sync",
            "Sync registry entries.",
            ["github"],
            ["sync-issues"],
            ["VS Code"],
            new DateTime(2026, 5, 18, 11, 0, 0, DateTimeKind.Utc));
        var unrelated = new RelationProjectorDocument(
            Guid.NewGuid(),
            "Slack Helper",
            "Slack helper.",
            ["slack"],
            ["send-message"],
            ["Cursor"],
            new DateTime(2026, 5, 18, 12, 0, 0, DateTimeKind.Utc));

        var results = await projector.ProjectAsync(source, [source, related, unrelated], limit: 5);

        Assert.Single(results);
        Assert.Equal(related.EntryId, results[0].EntryId);
        Assert.Equal(["github"], results[0].SharedTags);
        Assert.Equal(["sync-issues"], results[0].SharedTools);
        Assert.Equal(["VS Code"], results[0].SharedClients);
        Assert.True(results[0].Score > 0m);
    }
}