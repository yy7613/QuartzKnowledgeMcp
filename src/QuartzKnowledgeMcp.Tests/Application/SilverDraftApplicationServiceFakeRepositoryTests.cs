using QuartzKnowledgeMcp.Api.Application;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Silver;
using QuartzKnowledgeMcp.Tests.Infrastructure;

namespace QuartzKnowledgeMcp.Tests.Application;

public class SilverDraftApplicationServiceFakeRepositoryTests
{
    [Fact]
    public async Task OrganizeAsync_Works_WithFakeRepository()
    {
        var bronzeId = Guid.NewGuid();
        var knowledgeRepository = new FakeKnowledgeRepository();
        var unitOfWork = new FakeUnitOfWork();
        knowledgeRepository.BronzeSources.Add(new BronzeSource
        {
            Id = bronzeId,
            SourceType = "github-readme",
            SourceUri = "https://github.com/example/fake-organize",
            RawContent = "# Fake Organize Server\n\nFake organize server for repository tests.\n\n## Tools\n- search-docs: Search docs",
            Status = BronzeSourceStatuses.Imported,
            ImportedAtUtc = new DateTime(2026, 5, 19, 8, 0, 0, DateTimeKind.Utc)
        });

        var service = new SilverDraftApplicationService(new SilverDraftService(
            knowledgeRepository,
            unitOfWork,
            new RuleBasedOrganizationAgent(new RuleBasedSilverNormalizer()),
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 19, 9, 0, 0, TimeSpan.Zero))));

        var result = await service.OrganizeAsync(
            bronzeId,
            new OrganizeBronzeSourceRequest(SilverOrganizeModes.SilverDraft));

        Assert.True(result.Created);
        Assert.Single(knowledgeRepository.SilverDrafts);
        Assert.Equal(BronzeSourceStatuses.Organized, knowledgeRepository.BronzeSources[0].Status);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task ListAsync_Throws_WhenRepositoryViolatesCollectionContract()
    {
        var knowledgeRepository = new FakeKnowledgeRepository
        {
            ReturnNullSilverDraftsCollection = true
        };

        var service = new SilverDraftApplicationService(new SilverDraftService(
            knowledgeRepository,
            new FakeUnitOfWork(),
            new RuleBasedOrganizationAgent(new RuleBasedSilverNormalizer()),
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 19, 9, 0, 0, TimeSpan.Zero))));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ListAsync());

        Assert.Contains("silver drafts collection", exception.Message);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}