using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Domain.Ports;

namespace QuartzKnowledgeMcp.Api.Silver;

public sealed class RuleBasedOrganizationAgent(RuleBasedSilverNormalizer normalizer) : IOrganizationAgent
{
    public Task<OrganizationAgentResult> OrganizeAsync(
        BronzeSource source,
        OrganizationAgentRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new OrganizationAgentResult(
            normalizer.Normalize(source),
            UsedLlm: false));
    }
}