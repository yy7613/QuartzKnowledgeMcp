namespace QuartzKnowledgeMcp.Api.Bronze;

public sealed class BronzeSource
{
    public Guid Id { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public string? SourceUri { get; set; }

    public string RawContent { get; set; } = string.Empty;

    public string ContentHash { get; set; } = string.Empty;

    public string Status { get; set; } = BronzeSourceStatuses.Imported;

    public string? ImportedBy { get; set; }

    public DateTime ImportedAtUtc { get; set; }
}
