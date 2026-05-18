using QuartzKnowledgeMcp.Api.Gold;

namespace QuartzKnowledgeMcp.Api.Domain.Ports;

public interface IHistoryRepository
{
    void AddEntryHistory(EntryHistory history);

    Task<IReadOnlyList<EntryHistory>> GetEntryHistoriesAsync(
        Guid entryId,
        CancellationToken cancellationToken = default);
}