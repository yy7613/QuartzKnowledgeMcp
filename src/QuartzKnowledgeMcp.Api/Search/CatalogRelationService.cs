using Microsoft.EntityFrameworkCore;
using QuartzKnowledgeMcp.Api.Domain.Ports;
using QuartzKnowledgeMcp.Api.Gold;
using QuartzKnowledgeMcp.Api.Persistence;

namespace QuartzKnowledgeMcp.Api.Search;

public sealed class CatalogRelationService(
    McpKnowledgeDbContext dbContext,
    IRelationProjector relationProjector)
{
    public async Task<RelatedCatalogEntryResultResponse?> GetRelatedAsync(
        Guid entryId,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        var documents = await LoadDocumentsAsync(cancellationToken);
        var source = documents.FirstOrDefault(document => document.EntryId == entryId);

        if (source is null)
        {
            return null;
        }

        var projections = await relationProjector.ProjectAsync(source, documents, limit, cancellationToken);
        return new RelatedCatalogEntryResultResponse(
            projections
                .Select(projection => new RelatedCatalogEntryResponse(
                    projection.EntryId,
                    projection.DisplayName,
                    projection.Overview,
                    projection.SharedTags,
                    projection.SharedTools,
                    projection.SharedClients,
                    projection.Score))
                .ToList());
    }

    private async Task<List<RelationProjectorDocument>> LoadDocumentsAsync(CancellationToken cancellationToken)
    {
        var rows = await dbContext.GoldCatalogEntries
            .AsNoTracking()
            .Select(entry => new
            {
                entry.Id,
                entry.DisplayName,
                entry.Overview,
                entry.TagsJson,
                entry.ToolSummariesJson,
                entry.SupportedClientsJson,
                entry.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new RelationProjectorDocument(
                row.Id,
                row.DisplayName,
                row.Overview,
                GoldCatalogJson.DeserializeList<string>(row.TagsJson),
                GoldCatalogJson.DeserializeList<GoldToolSummaryResponse>(row.ToolSummariesJson)
                    .Select(tool => tool.Name)
                    .ToList(),
                GoldCatalogJson.DeserializeList<string>(row.SupportedClientsJson),
                row.UpdatedAtUtc))
            .ToList();
    }
}