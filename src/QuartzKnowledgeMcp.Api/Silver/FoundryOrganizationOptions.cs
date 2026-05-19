namespace QuartzKnowledgeMcp.Api.Silver;

public sealed class FoundryOrganizationOptions
{
    public const string SectionName = "FoundryOrganization";

    public bool Enabled { get; set; }

    public string? Provider { get; set; } = "foundry";

    public string? ProjectEndpoint { get; set; }

    public string? Model { get; set; }

    public string? AgentName { get; set; }

    public string? Instructions { get; set; }

    public bool FallbackToRuleBasedOnError { get; set; } = true;

    public bool IsConfigured =>
        Enabled
        && Uri.TryCreate(ProjectEndpoint, UriKind.Absolute, out _)
        && !string.IsNullOrWhiteSpace(Model);

    public string EffectiveProvider => IsConfigured
        ? string.IsNullOrWhiteSpace(Provider) ? "foundry" : Provider.Trim()
        : "none";

    public string EffectiveModel => IsConfigured
        ? Model!.Trim()
        : "none";

    public string EffectiveAgentName => string.IsNullOrWhiteSpace(AgentName)
        ? "QuartzKnowledgeOrganizer"
        : AgentName.Trim();

    public string EffectiveInstructions => string.IsNullOrWhiteSpace(Instructions)
        ? "You organize MCP server documentation into a concise silver draft. Return only valid JSON matching the requested schema."
        : Instructions.Trim();
}