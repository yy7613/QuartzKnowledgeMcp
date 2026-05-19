using QuartzKnowledgeMcp.Api.Application;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Embedding;
using QuartzKnowledgeMcp.Api.Gold;
using QuartzKnowledgeMcp.Api.Silver;
using QuartzKnowledgeMcp.Tests.Infrastructure;

namespace QuartzKnowledgeMcp.Tests.Application;

public class CatalogCurationApplicationServiceFakeRepositoryTests
{
    [Fact]
    public async Task PublishAsync_Works_WithFakeRepository()
    {
        var bronzeId = Guid.NewGuid();
        var silverId = Guid.NewGuid();
        var knowledgeRepository = new FakeKnowledgeRepository();
        var historyRepository = new FakeHistoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        knowledgeRepository.BronzeSources.Add(new BronzeSource
        {
            Id = bronzeId,
            SourceType = "github-readme",
            SourceUri = "https://github.com/example/fake-publish",
            RawContent = "# Fake Publish Server\n\nFake publish server for repository tests.\n\nAuthentication: OAuth 2.0\n\nSupported clients: VS Code\n\n## Tools\n- search-docs: Search docs",
            Status = BronzeSourceStatuses.Organized,
            ImportedAtUtc = new DateTime(2026, 5, 19, 10, 0, 0, DateTimeKind.Utc)
        });
        knowledgeRepository.SilverDrafts.Add(new SilverServerDraft
        {
            Id = silverId,
            BronzeSourceId = bronzeId,
            Name = "Fake Publish Server",
            Summary = "Fake publish server for repository tests.",
            TagCandidatesJson = GoldCatalogJson.Serialize(new[] { "mcp", "search" }),
            OrganizedAtUtc = new DateTime(2026, 5, 19, 11, 0, 0, DateTimeKind.Utc),
            ToolDrafts =
            [
                new SilverToolDraft
                {
                    Id = Guid.NewGuid(),
                    SilverServerDraftId = silverId,
                    Name = "search-docs",
                    Description = "Search docs",
                    Position = 0
                }
            ]
        });

        var service = new CatalogCurationApplicationService(new GoldCatalogService(
            knowledgeRepository,
            historyRepository,
            unitOfWork,
            new NoOpSemanticIndexer(),
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero))));

        var result = await service.PublishAsync(
            silverId,
            new PublishSilverDraftRequest("fake-publisher"));

        Assert.True(result.Created);
        Assert.Single(knowledgeRepository.GoldCatalogEntries);
        Assert.Single(historyRepository.EntryHistories);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        Assert.Equal("fake-publisher", result.Entry.PublishedBy);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}