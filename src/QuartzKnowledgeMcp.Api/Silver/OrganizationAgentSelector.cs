using Microsoft.Extensions.Options;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Domain.Ports;

namespace QuartzKnowledgeMcp.Api.Silver;

public sealed class OrganizationAgentSelector(
    RuleBasedOrganizationAgent ruleBasedAgent,
    MafFoundryOrganizationAgent foundryAgent,
    IOptions<FoundryOrganizationOptions> foundryOptions,
    ILogger<OrganizationAgentSelector> logger) : IOrganizationAgent
{
    public async Task<OrganizationAgentResult> OrganizeAsync(
        BronzeSource source,
        OrganizationAgentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.UseLlm || !foundryOptions.Value.IsConfigured)
        {
            return await ruleBasedAgent.OrganizeAsync(source, request, cancellationToken);
        }

        try
        {
            return await foundryAgent.OrganizeAsync(source, request, cancellationToken);
        }
        catch (Exception exception) when (foundryOptions.Value.FallbackToRuleBasedOnError)
        {
            logger.LogWarning(
                exception,
                "Foundry organization failed for source {BronzeSourceId}. Falling back to rule-based organization.",
                source.Id);

            return await ruleBasedAgent.OrganizeAsync(
                source,
                request with { UseLlm = false },
                cancellationToken);
        }
    }
}