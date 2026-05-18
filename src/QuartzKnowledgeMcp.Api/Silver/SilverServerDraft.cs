namespace QuartzKnowledgeMcp.Api.Silver;

public sealed class SilverServerDraft
{
    public Guid Id { get; set; }

    public Guid BronzeSourceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string TagCandidatesJson { get; set; } = "[]";

    public DateTime OrganizedAtUtc { get; set; }

    public List<SilverToolDraft> ToolDrafts { get; set; } = [];
}

public sealed class SilverToolDraft
{
    public Guid Id { get; set; }

    public Guid SilverServerDraftId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Position { get; set; }
}