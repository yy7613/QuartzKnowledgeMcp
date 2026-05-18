using QuartzKnowledgeMcp.Api.Domain.Gold;
using QuartzKnowledgeMcp.Api.Gold;

namespace QuartzKnowledgeMcp.Tests.Gold;

public class GoldDomainRulesTests
{
    [Fact]
    public void GoldTagSet_Create_NormalizesValues_AndPreservesOrder()
    {
        var tagSet = GoldTagSet.Create([" docs ", "Registry", "automation "]);

        Assert.Equal(["docs", "Registry", "automation"], tagSet.Values);
    }

    [Fact]
    public void GoldTagSet_Create_RejectsCaseInsensitiveDuplicates()
    {
        var exception = Assert.Throws<GoldValidationException>(() =>
            GoldTagSet.Create(["docs", "Docs"]));

        Assert.Contains("tags", exception.Errors.Keys);
    }

    [Fact]
    public void GoldTagSet_Create_RejectsOutOfRangeCounts()
    {
        var tooFew = Assert.Throws<GoldValidationException>(() => GoldTagSet.Create([]));
        var tooMany = Assert.Throws<GoldValidationException>(() =>
            GoldTagSet.Create(["one", "two", "three", "four", "five", "six"]));

        Assert.Contains("tags", tooFew.Errors.Keys);
        Assert.Contains("tags", tooMany.Errors.Keys);
    }

    [Fact]
    public void GoldCatalogUpdate_Create_TrimsValues_AndDefaultsCollections()
    {
        var update = GoldCatalogUpdate.Create(
            " updated overview ",
            " setup guide ",
            null,
            null);

        Assert.Equal("updated overview", update.Overview);
        Assert.Equal("setup guide", update.SetupGuide);
        Assert.Empty(update.References);
        Assert.Empty(update.SupportedClients);
    }

    [Fact]
    public void GoldCatalogUpdate_Create_RejectsDuplicateSupportedClients()
    {
        var exception = Assert.Throws<GoldValidationException>(() =>
            GoldCatalogUpdate.Create(
                "overview",
                "setup",
                ["https://example.dev/docs"],
                ["VS Code", "vs code"]));

        Assert.Contains("supportedClients", exception.Errors.Keys);
    }

    [Fact]
    public void GoldEntryHistoryFactory_CreatePublished_UsesExpectedActionAndSummary()
    {
        var published = GoldEntryHistoryFactory.CreatePublished("publisher", new DateTime(2026, 5, 19, 10, 0, 0, DateTimeKind.Utc), true);
        var republished = GoldEntryHistoryFactory.CreatePublished("publisher", new DateTime(2026, 5, 19, 10, 0, 0, DateTimeKind.Utc), false);

        Assert.Equal(EntryHistoryActions.Published, published.Action);
        Assert.Equal("Initial publish from silver draft.", published.Summary);
        Assert.Equal(EntryHistoryActions.Republished, republished.Action);
        Assert.Equal("Published entry refreshed from silver draft.", republished.Summary);
    }

    [Fact]
    public void GoldEntryHistoryFactory_CreateTagsReplaced_UsesTagCountInSummary()
    {
        var draft = GoldEntryHistoryFactory.CreateTagsReplaced(
            "editor",
            new DateTime(2026, 5, 19, 10, 0, 0, DateTimeKind.Utc),
            3);

        Assert.Equal(EntryHistoryActions.TagsReplaced, draft.Action);
        Assert.Equal("Tags replaced with 3 values.", draft.Summary);
    }
}