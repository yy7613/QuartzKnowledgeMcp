using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Domain.Ports;
using QuartzKnowledgeMcp.Api.Gold;
using QuartzKnowledgeMcp.Api.Silver;

namespace QuartzKnowledgeMcp.Tests.Infrastructure;

internal sealed class FakeKnowledgeRepository : IKnowledgeRepository
{
    public List<BronzeSource> BronzeSources { get; } = [];

    public List<SilverServerDraft> SilverDrafts { get; } = [];

    public List<GoldCatalogEntry> GoldCatalogEntries { get; } = [];

    public bool ReturnNullSilverDraftsCollection { get; set; }

    public bool ReturnNullGoldProjectionRows { get; set; }

    public Task<BronzeSource?> GetBronzeSourceAsync(
        Guid bronzeId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(BronzeSources.FirstOrDefault(source => source.Id == bronzeId));
    }

    public Task<SilverServerDraft?> GetSilverDraftAsync(
        Guid silverId,
        bool includeToolDrafts,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SilverDrafts.FirstOrDefault(draft => draft.Id == silverId));
    }

    public Task<SilverServerDraft?> GetSilverDraftByBronzeSourceIdAsync(
        Guid bronzeId,
        bool includeToolDrafts,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SilverDrafts.FirstOrDefault(draft => draft.BronzeSourceId == bronzeId));
    }

    public Task<IReadOnlyList<SilverServerDraft>> GetSilverDraftsAsync(
        bool includeToolDrafts,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<SilverServerDraft>>(ReturnNullSilverDraftsCollection
            ? null!
            : SilverDrafts.ToList());
    }

    public void AddSilverDraft(SilverServerDraft draft)
    {
        SilverDrafts.Add(draft);
    }

    public void RemoveSilverToolDrafts(IEnumerable<SilverToolDraft> toolDrafts)
    {
        var toRemove = toolDrafts.ToList();
        foreach (var draft in SilverDrafts)
        {
            draft.ToolDrafts.RemoveAll(toolDraftDraft => toRemove.Any(item => item.Id == toolDraftDraft.Id));
        }
    }

    public Task<GoldCatalogEntry?> GetGoldCatalogEntryAsync(
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GoldCatalogEntries.FirstOrDefault(entry => entry.Id == entryId));
    }

    public Task<GoldCatalogEntry?> GetGoldCatalogEntryBySilverDraftIdAsync(
        Guid silverId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GoldCatalogEntries.FirstOrDefault(entry => entry.SilverServerDraftId == silverId));
    }

    public Task<IReadOnlyList<GoldCatalogProjectionRow>> GetGoldCatalogProjectionRowsAsync(
        CancellationToken cancellationToken = default)
    {
        if (ReturnNullGoldProjectionRows)
        {
            return Task.FromResult<IReadOnlyList<GoldCatalogProjectionRow>>(null!);
        }

        var rows = (
            from entry in GoldCatalogEntries
            join silver in SilverDrafts on entry.SilverServerDraftId equals silver.Id
            join bronze in BronzeSources on silver.BronzeSourceId equals bronze.Id
            select new GoldCatalogProjectionRow(entry, bronze.RawContent))
            .ToList();

        return Task.FromResult<IReadOnlyList<GoldCatalogProjectionRow>>(rows);
    }

    public void AddGoldCatalogEntry(GoldCatalogEntry entry)
    {
        GoldCatalogEntries.Add(entry);
    }
}

internal sealed class FakeHistoryRepository : IHistoryRepository
{
    public List<EntryHistory> EntryHistories { get; } = [];

    public bool ReturnNullEntryHistories { get; set; }

    public void AddEntryHistory(EntryHistory history)
    {
        EntryHistories.Add(history);
    }

    public Task<IReadOnlyList<EntryHistory>> GetEntryHistoriesAsync(
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<EntryHistory>>(ReturnNullEntryHistories
            ? null!
            : EntryHistories.Where(history => history.EntryId == entryId).ToList());
    }
}

internal sealed class FakeUnitOfWork(string providerName = "fake") : IUnitOfWork
{
    public int SaveChangesCalls { get; private set; }

    public string ProviderName { get; } = providerName;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalls++;
        return Task.FromResult(1);
    }
}