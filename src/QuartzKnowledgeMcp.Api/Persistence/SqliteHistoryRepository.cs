using Microsoft.EntityFrameworkCore;
using QuartzKnowledgeMcp.Api.Domain.Ports;
using QuartzKnowledgeMcp.Api.Gold;

namespace QuartzKnowledgeMcp.Api.Persistence;

public sealed class SqliteHistoryRepository(McpKnowledgeDbContext dbContext) : IHistoryRepository
{
    public void AddEntryHistory(EntryHistory history)
    {
        dbContext.EntryHistories.Add(history);
    }

    public async Task<IReadOnlyList<EntryHistory>> GetEntryHistoriesAsync(
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.EntryHistories
            .AsNoTracking()
            .Where(history => history.EntryId == entryId)
            .ToListAsync(cancellationToken);
    }
}