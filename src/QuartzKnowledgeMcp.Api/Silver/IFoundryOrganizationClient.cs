namespace QuartzKnowledgeMcp.Api.Silver;

public interface IFoundryOrganizationClient
{
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);
}