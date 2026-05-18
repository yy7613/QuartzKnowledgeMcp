using Microsoft.EntityFrameworkCore;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Domain.Ports;
using QuartzKnowledgeMcp.Api.Gold;
using QuartzKnowledgeMcp.Api.Silver;

namespace QuartzKnowledgeMcp.Api.Persistence;

public sealed class SqliteKnowledgeRepository(McpKnowledgeDbContext dbContext) : IKnowledgeRepository
{
    public Task<BronzeSource?> GetBronzeSourceAsync(
        Guid bronzeId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.BronzeSources
            .FirstOrDefaultAsync(source => source.Id == bronzeId, cancellationToken);
    }

    public async Task<SilverServerDraft?> GetSilverDraftAsync(
        Guid silverId,
        bool includeToolDrafts,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SilverServerDrafts.AsQueryable();

        if (includeToolDrafts)
        {
            query = query.Include(draft => draft.ToolDrafts);
        }

        return await query.FirstOrDefaultAsync(draft => draft.Id == silverId, cancellationToken);
    }

    public async Task<SilverServerDraft?> GetSilverDraftByBronzeSourceIdAsync(
        Guid bronzeId,
        bool includeToolDrafts,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SilverServerDrafts.AsQueryable();

        if (includeToolDrafts)
        {
            query = query.Include(draft => draft.ToolDrafts);
        }

        return await query.FirstOrDefaultAsync(draft => draft.BronzeSourceId == bronzeId, cancellationToken);
    }

    public async Task<IReadOnlyList<SilverServerDraft>> GetSilverDraftsAsync(
        bool includeToolDrafts,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SilverServerDrafts
            .AsNoTracking()
            .AsQueryable();

        if (includeToolDrafts)
        {
            query = query.Include(draft => draft.ToolDrafts);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public void AddSilverDraft(SilverServerDraft draft)
    {
        dbContext.SilverServerDrafts.Add(draft);
    }

    public void RemoveSilverToolDrafts(IEnumerable<SilverToolDraft> toolDrafts)
    {
        dbContext.SilverToolDrafts.RemoveRange(toolDrafts);
    }

    public Task<GoldCatalogEntry?> GetGoldCatalogEntryAsync(
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.GoldCatalogEntries
            .FirstOrDefaultAsync(entry => entry.Id == entryId, cancellationToken);
    }

    public Task<GoldCatalogEntry?> GetGoldCatalogEntryBySilverDraftIdAsync(
        Guid silverId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.GoldCatalogEntries
            .FirstOrDefaultAsync(entry => entry.SilverServerDraftId == silverId, cancellationToken);
    }

    public async Task<IReadOnlyList<GoldCatalogProjectionRow>> GetGoldCatalogProjectionRowsAsync(
        CancellationToken cancellationToken = default)
    {
        return await (
            from entry in dbContext.GoldCatalogEntries.AsNoTracking()
            join silver in dbContext.SilverServerDrafts.AsNoTracking()
                on entry.SilverServerDraftId equals silver.Id
            join bronze in dbContext.BronzeSources.AsNoTracking()
                on silver.BronzeSourceId equals bronze.Id
            select new GoldCatalogProjectionRow(entry, bronze.RawContent))
            .ToListAsync(cancellationToken);
    }

    public void AddGoldCatalogEntry(GoldCatalogEntry entry)
    {
        dbContext.GoldCatalogEntries.Add(entry);
    }
}