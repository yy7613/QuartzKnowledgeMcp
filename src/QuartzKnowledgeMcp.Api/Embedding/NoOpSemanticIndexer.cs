using QuartzKnowledgeMcp.Api.Domain.Ports;

namespace QuartzKnowledgeMcp.Api.Embedding;

public sealed class NoOpSemanticIndexer : ISemanticIndexer
{
    public Task IndexAsync(
        SemanticCatalogDocument document,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}