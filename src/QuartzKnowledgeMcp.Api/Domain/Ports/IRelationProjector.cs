namespace QuartzKnowledgeMcp.Api.Domain.Ports;

public interface IRelationProjector
{
    Task<IReadOnlyList<RelatedCatalogEntryProjection>> ProjectAsync(
        RelationProjectorDocument source,
        IReadOnlyList<RelationProjectorDocument> candidates,
        int limit = 5,
        CancellationToken cancellationToken = default);
}