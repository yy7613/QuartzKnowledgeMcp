using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Silver;

namespace QuartzKnowledgeMcp.Api.Domain.Ports;

public interface IOrganizationAgent
{
    Task<OrganizationAgentResult> OrganizeAsync(
        BronzeSource source,
        OrganizationAgentRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record OrganizationAgentRequest(bool UseLlm);

public sealed record OrganizationAgentResult(
    SilverServerDraftContent Draft,
    bool UsedLlm);