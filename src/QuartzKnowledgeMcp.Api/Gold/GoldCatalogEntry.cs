namespace QuartzKnowledgeMcp.Api.Gold;

public sealed class GoldCatalogEntry
{
    public Guid Id { get; set; }

    public Guid SilverServerDraftId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string Overview { get; set; } = string.Empty;

    public string TagsJson { get; set; } = "[]";

    public string SetupGuide { get; set; } = string.Empty;

    public string ToolSummariesJson { get; set; } = "[]";

    public string ReferencesJson { get; set; } = "[]";

    public string SupportedClientsJson { get; set; } = "[]";

    public DateTime PublishedAtUtc { get; set; }

    public string PublishedBy { get; set; } = "system";

    public DateTime UpdatedAtUtc { get; set; }

    public string UpdatedBy { get; set; } = "system";
}

public sealed class EntryHistory
{
    public Guid Id { get; set; }

    public Guid EntryId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string ChangedBy { get; set; } = "system";

    public DateTime ChangedAtUtc { get; set; }

    public string Summary { get; set; } = string.Empty;

    public bool UsedLlm { get; set; }
}