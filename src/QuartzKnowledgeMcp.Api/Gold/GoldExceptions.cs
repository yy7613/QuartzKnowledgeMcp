namespace QuartzKnowledgeMcp.Api.Gold;

public sealed class SilverDraftNotFoundException(Guid silverId)
    : Exception($"Silver draft '{silverId}' was not found.")
{
    public Guid SilverId { get; } = silverId;
}

public static class EntryHistoryActions
{
    public const string Published = "published";
    public const string Republished = "republished";
}