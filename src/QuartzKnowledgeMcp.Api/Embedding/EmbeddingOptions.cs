namespace QuartzKnowledgeMcp.Api.Embedding;

public sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";

    public bool Enabled { get; set; }

    public string? Provider { get; set; } = "none";

    public bool IsConfigured =>
        Enabled
        && !string.IsNullOrWhiteSpace(Provider)
        && !string.Equals(Provider, "none", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(Provider, "disabled", StringComparison.OrdinalIgnoreCase);

    public string EffectiveProvider => IsConfigured
        ? Provider!.Trim()
        : "none";
}