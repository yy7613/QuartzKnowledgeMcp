using QuartzKnowledgeMcp.Api.Domain.Ports;

namespace QuartzKnowledgeMcp.Api.Persistence;

public sealed class EfUnitOfWork(McpKnowledgeDbContext dbContext) : IUnitOfWork
{
    public string ProviderName => dbContext.Database.ProviderName ?? "unknown";

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}