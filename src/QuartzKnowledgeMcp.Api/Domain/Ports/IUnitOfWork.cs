namespace QuartzKnowledgeMcp.Api.Domain.Ports;

public interface IUnitOfWork
{
    string ProviderName { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}