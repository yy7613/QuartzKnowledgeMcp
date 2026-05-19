using QuartzKnowledgeMcp.Api.Domain.Ports;

namespace QuartzKnowledgeMcp.Api.Embedding;

public sealed class NoOpEmbeddingGenerator : IEmbeddingGenerator
{
    public Task<IReadOnlyList<float>> GenerateAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<float>>([]);
    }
}