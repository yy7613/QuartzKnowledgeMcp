using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Silver;

namespace QuartzKnowledgeMcp.Tests.Silver;

public class RuleBasedSilverNormalizerTests
{
    private readonly RuleBasedSilverNormalizer _normalizer = new();

    [Fact]
    public void Normalize_ExtractsNameSummaryTagsAndTools_FromMarkdownReadme()
    {
        var source = new BronzeSource
        {
            SourceType = "github-readme",
            SourceUri = "https://github.com/example/acme-mcp-server",
            RawContent = """
                # Acme MCP Server

                Acme MCP Server lets teams search docs and manage GitHub issues from any MCP client.

                ## Tools
                - search-docs: Search the internal knowledge base
                - sync-issues: Synchronize GitHub issues into the workspace
                """
        };

        var result = _normalizer.Normalize(source);

        Assert.Equal("Acme MCP Server", result.Name);
        Assert.Equal(
            "Acme MCP Server lets teams search docs and manage GitHub issues from any MCP client.",
            result.Summary);
        Assert.Contains("mcp", result.TagCandidates);
        Assert.Contains("github", result.TagCandidates);
        Assert.Contains("search", result.TagCandidates);
        Assert.Equal(2, result.ToolDrafts.Count);
        Assert.Equal("search-docs", result.ToolDrafts[0].Name);
        Assert.Equal("Search the internal knowledge base", result.ToolDrafts[0].Description);
    }

    [Fact]
    public void Normalize_ReturnsFallbackDraft_WhenInputIsIncomplete()
    {
        var source = new BronzeSource
        {
            SourceType = "manual",
            SourceUri = "https://example.com/minimal-server",
            RawContent = "   "
        };

        var result = _normalizer.Normalize(source);

        Assert.Equal("Minimal Server", result.Name);
        Assert.Equal("Summary pending normalization.", result.Summary);
        Assert.Contains("mcp", result.TagCandidates);
        Assert.Empty(result.ToolDrafts);
    }
}