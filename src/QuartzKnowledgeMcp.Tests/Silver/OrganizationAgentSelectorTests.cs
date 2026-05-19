using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Domain.Ports;
using QuartzKnowledgeMcp.Api.Silver;

namespace QuartzKnowledgeMcp.Tests.Silver;

public class OrganizationAgentSelectorTests
{
    [Fact]
    public async Task OrganizeAsync_UsesFoundryAgent_WhenConfiguredAndRequested()
    {
        var selector = CreateSelector(
            new FakeFoundryOrganizationClient("""
                {
                  "name": "Foundry MCP Server",
                  "summary": "Organized through the Foundry path.",
                  "tagCandidates": ["foundry", "mcp", "foundry"],
                  "toolDrafts": [
                    { "name": "search-docs", "description": "Searches docs." },
                    { "name": "search-docs", "description": "Duplicate should be removed." },
                    { "name": "sync-issues", "description": "Syncs issues." }
                  ]
                }
                """),
            new FoundryOrganizationOptions
            {
                Enabled = true,
                ProjectEndpoint = "https://example.foundry.azure.com/api/projects/demo",
                Model = "gpt-4.1-mini"
            });

        var result = await selector.OrganizeAsync(
            CreateBronzeSource(),
            new OrganizationAgentRequest(UseLlm: true));

        Assert.True(result.UsedLlm);
        Assert.Equal("Foundry MCP Server", result.Draft.Name);
        Assert.Equal(["foundry", "mcp"], result.Draft.TagCandidates);
        Assert.Equal(2, result.Draft.ToolDrafts.Count);
    }

    [Fact]
    public async Task OrganizeAsync_UsesRuleBased_WhenFoundryIsNotConfigured()
    {
        var selector = CreateSelector(
            new FakeFoundryOrganizationClient(ShouldThrow: true),
            new FoundryOrganizationOptions
            {
                Enabled = false,
                ProjectEndpoint = "",
                Model = ""
            });

        var result = await selector.OrganizeAsync(
            CreateBronzeSource(),
            new OrganizationAgentRequest(UseLlm: true));

        Assert.False(result.UsedLlm);
        Assert.Equal("Acme MCP Server", result.Draft.Name);
        Assert.Single(result.Draft.ToolDrafts);
    }

    [Fact]
    public async Task OrganizeAsync_FallsBackToRuleBased_WhenFoundryThrows()
    {
        var selector = CreateSelector(
            new FakeFoundryOrganizationClient(ShouldThrow: true),
            new FoundryOrganizationOptions
            {
                Enabled = true,
                ProjectEndpoint = "https://example.foundry.azure.com/api/projects/demo",
                Model = "gpt-4.1-mini",
                FallbackToRuleBasedOnError = true
            });

        var result = await selector.OrganizeAsync(
            CreateBronzeSource(),
            new OrganizationAgentRequest(UseLlm: true));

        Assert.False(result.UsedLlm);
        Assert.Equal("Acme MCP Server", result.Draft.Name);
        Assert.Single(result.Draft.ToolDrafts);
    }

    private static OrganizationAgentSelector CreateSelector(
        IFoundryOrganizationClient foundryClient,
        FoundryOrganizationOptions options)
    {
        return new OrganizationAgentSelector(
            new RuleBasedOrganizationAgent(new RuleBasedSilverNormalizer()),
            new MafFoundryOrganizationAgent(foundryClient),
            Options.Create(options),
            NullLogger<OrganizationAgentSelector>.Instance);
    }

    private static BronzeSource CreateBronzeSource()
    {
        return new BronzeSource
        {
            Id = Guid.NewGuid(),
            SourceType = "github-readme",
            SourceUri = "https://github.com/example/acme-mcp-server",
            RawContent = """
                # Acme MCP Server

                Acme MCP Server lets teams search docs and manage GitHub issues from any MCP client.

                ## Tools
                - search-docs: Search the internal knowledge base
                """,
            Status = BronzeSourceStatuses.Imported,
            ImportedAtUtc = new DateTime(2026, 5, 19, 8, 0, 0, DateTimeKind.Utc)
        };
    }

    private sealed class FakeFoundryOrganizationClient(
        string? payload = null,
        bool ShouldThrow = false) : IFoundryOrganizationClient
    {
        public Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
        {
            if (ShouldThrow)
            {
                throw new InvalidOperationException("Foundry client failure for fallback test.");
            }

            return Task.FromResult(payload ?? "{}" );
        }
    }
}