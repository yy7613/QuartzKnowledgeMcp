using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Silver;
using QuartzKnowledgeMcp.Tests.Infrastructure;

namespace QuartzKnowledgeMcp.Tests.Application;

public class SilverDraftApplicationServiceTests
{
    [Fact]
    public async Task OrganizeAsync_CreatesDraft_ThroughApplicationBoundary()
    {
        await using var connection = await KnowledgeStoreTestFixture.OpenConnectionAsync();
        await using var dbContext = await KnowledgeStoreTestFixture.CreateDbContextAsync(connection);
        var bronzeService = KnowledgeStoreTestFixture.CreateBronzeService(dbContext);
        var service = KnowledgeStoreTestFixture.CreateSilverApplicationService(dbContext);
        var bronze = await bronzeService.ImportAsync(new CreateBronzeSourceRequest(
            "github-readme",
            "https://github.com/example/silver-app-test",
            "# Silver App Test\n\nSilver App Test helps search docs.\n\n## Tools\n- search-docs: Search docs",
            "app-tester"));

        var result = await service.OrganizeAsync(
            bronze.Source.Id,
            new OrganizeBronzeSourceRequest(SilverOrganizeModes.SilverDraft));
        var detail = await service.GetDetailAsync(result.Draft.Id);

        Assert.True(result.Created);
        Assert.NotNull(detail);
        Assert.Equal("Silver App Test", detail.Name);
        Assert.Single(detail.ToolDrafts);
    }
}