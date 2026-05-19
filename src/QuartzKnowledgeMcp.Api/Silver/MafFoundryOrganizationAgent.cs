using System.Text.Json;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Domain.Ports;

namespace QuartzKnowledgeMcp.Api.Silver;

public sealed class MafFoundryOrganizationAgent(IFoundryOrganizationClient foundryClient) : IOrganizationAgent
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<OrganizationAgentResult> OrganizeAsync(
        BronzeSource source,
        OrganizationAgentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var payload = await foundryClient.CompleteAsync(
            BuildPrompt(source),
            cancellationToken);
        var response = JsonSerializer.Deserialize<FoundrySilverDraftResponse>(payload, SerializerOptions)
            ?? throw new SilverNormalizationException("Foundry response could not be deserialized into the expected draft schema.");

        return new OrganizationAgentResult(
            response.ToSilverDraftContent(),
            UsedLlm: true);
    }

    private static string BuildPrompt(BronzeSource source)
    {
        return $$"""
You are organizing MCP server documentation into a silver draft.

Return JSON only with these fields:
- name: string
- summary: string
- tagCandidates: string[]
- toolDrafts: array of { name: string, description: string }

Constraints:
- name should be concise and user-facing
- summary should be one short paragraph, max 240 chars
- tagCandidates should contain short lowercase tags without duplicates
- toolDrafts should only include meaningful tools inferred from the source

Bronze source type: {{source.SourceType}}
Bronze source uri: {{source.SourceUri ?? "unknown"}}

Raw content:
{{source.RawContent}}
""";
    }
}