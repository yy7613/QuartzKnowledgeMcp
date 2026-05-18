namespace QuartzKnowledgeMcp.Api.Gold;

public sealed class SilverDraftNotFoundException(Guid silverId)
    : Exception($"Silver draft '{silverId}' was not found.")
{
    public Guid SilverId { get; } = silverId;
}

public sealed class GoldCatalogEntryNotFoundException(Guid entryId)
    : Exception($"Gold catalog entry '{entryId}' was not found.")
{
    public Guid EntryId { get; } = entryId;
}

public sealed class GoldValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("Gold validation failed.")
{
    public IDictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>(errors);
}

public static class EntryHistoryActions
{
    public const string Published = "published";
    public const string Republished = "republished";
    public const string CatalogUpdated = "catalog-updated";
    public const string TagsReplaced = "tags-replaced";
}