using QuartzKnowledgeMcp.Api.Domain.Ports;
using QuartzKnowledgeMcp.Api.Embedding;

namespace QuartzKnowledgeMcp.Tests.Embedding;

public class NoOpEmbeddingAdaptersTests
{
    [Fact]
    public async Task GenerateAsync_ReturnsEmptyVector_AndIndexerCompletesWithoutSideEffects()
    {
        var generator = new NoOpEmbeddingGenerator();
        var indexer = new NoOpSemanticIndexer();

        var vector = await generator.GenerateAsync("Acme MCP Server");
        await indexer.IndexAsync(new SemanticCatalogDocument(
            Guid.NewGuid(),
            "Acme MCP Server",
            "Overview",
            ["mcp"],
            ["search-docs"],
            ["VS Code"]));

        Assert.Empty(vector);
    }
}