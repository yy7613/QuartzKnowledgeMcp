using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Gold;
using QuartzKnowledgeMcp.Api.Silver;

namespace QuartzKnowledgeMcp.Api.Domain.Ports;

public interface IKnowledgeRepository
{
    Task<BronzeSource?> GetBronzeSourceAsync(
        Guid bronzeId,
        CancellationToken cancellationToken = default);

    Task<SilverServerDraft?> GetSilverDraftAsync(
        Guid silverId,
        bool includeToolDrafts,
        CancellationToken cancellationToken = default);

    Task<SilverServerDraft?> GetSilverDraftByBronzeSourceIdAsync(
        Guid bronzeId,
        bool includeToolDrafts,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SilverServerDraft>> GetSilverDraftsAsync(
        bool includeToolDrafts,
        CancellationToken cancellationToken = default);

    void AddSilverDraft(SilverServerDraft draft);

    void RemoveSilverToolDrafts(IEnumerable<SilverToolDraft> toolDrafts);

    Task<GoldCatalogEntry?> GetGoldCatalogEntryAsync(
        Guid entryId,
        CancellationToken cancellationToken = default);

    Task<GoldCatalogEntry?> GetGoldCatalogEntryBySilverDraftIdAsync(
        Guid silverId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoldCatalogProjectionRow>> GetGoldCatalogProjectionRowsAsync(
        CancellationToken cancellationToken = default);

    void AddGoldCatalogEntry(GoldCatalogEntry entry);
}