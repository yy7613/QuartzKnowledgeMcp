using QuartzKnowledgeMcp.Api.Silver;

namespace QuartzKnowledgeMcp.Tests.Silver;

public class FoundrySilverDraftResponseTests
{
    [Fact]
    public void ToSilverDraftContent_NormalizesValues_DeduplicatesAndDefaultsToolDescription()
    {
        var response = new FoundrySilverDraftResponse(
            Name: "  Acme   MCP   Server  ",
            Summary: "  Helps   teams   search docs   and sync issues.  ",
            TagCandidates: [" docs ", "mcp", "DOCS", "   ", "issues"],
            ToolDrafts:
            [
                new FoundrySilverToolDraftResponse("  search-docs  ", "  Searches   docs.  "),
                new FoundrySilverToolDraftResponse("search-docs", "duplicate entry"),
                new FoundrySilverToolDraftResponse("sync-issues", "   "),
                new FoundrySilverToolDraftResponse("   ", "ignored")
            ]);

        var content = response.ToSilverDraftContent();

        Assert.Equal("Acme MCP Server", content.Name);
        Assert.Equal("Helps teams search docs and sync issues.", content.Summary);
        Assert.Equal(["docs", "mcp", "issues"], content.TagCandidates);
        Assert.Equal(2, content.ToolDrafts.Count);
        Assert.Equal("search-docs", content.ToolDrafts[0].Name);
        Assert.Equal("Searches docs.", content.ToolDrafts[0].Description);
        Assert.Equal("sync-issues", content.ToolDrafts[1].Name);
        Assert.Equal("Description pending AI organization.", content.ToolDrafts[1].Description);
    }
}