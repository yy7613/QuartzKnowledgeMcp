namespace QuartzKnowledgeMcp.Api.Domain.Ports;

public interface ISemanticIndexer
{
    Task IndexAsync(
        SemanticCatalogDocument document,
        CancellationToken cancellationToken = default);
}