using QuartzKnowledgeMcp.Api.Gold;

namespace QuartzKnowledgeMcp.Api.Domain.Gold;

public sealed record GoldEntryHistoryDraft(
    string Action,
    string ChangedBy,
    DateTime ChangedAtUtc,
    string Summary,
    bool UsedLlm);

public static class GoldEntryHistoryFactory
{
    public static GoldEntryHistoryDraft CreatePublished(string actor, DateTime changedAtUtc, bool created)
    {
        return new GoldEntryHistoryDraft(
            created ? EntryHistoryActions.Published : EntryHistoryActions.Republished,
            actor,
            changedAtUtc,
            created
                ? "Initial publish from silver draft."
                : "Published entry refreshed from silver draft.",
            false);
    }

    public static GoldEntryHistoryDraft CreateCatalogUpdated(string actor, DateTime changedAtUtc)
    {
        return new GoldEntryHistoryDraft(
            EntryHistoryActions.CatalogUpdated,
            actor,
            changedAtUtc,
            "Catalog content updated.",
            false);
    }

    public static GoldEntryHistoryDraft CreateTagsReplaced(string actor, DateTime changedAtUtc, int tagCount)
    {
        return new GoldEntryHistoryDraft(
            EntryHistoryActions.TagsReplaced,
            actor,
            changedAtUtc,
            $"Tags replaced with {tagCount} values.",
            false);
    }
}