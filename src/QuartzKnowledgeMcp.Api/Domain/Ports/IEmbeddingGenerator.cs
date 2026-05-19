namespace QuartzKnowledgeMcp.Api.Domain.Ports;

public interface IEmbeddingGenerator
{
    Task<IReadOnlyList<float>> GenerateAsync(
        string text,
        CancellationToken cancellationToken = default);
}