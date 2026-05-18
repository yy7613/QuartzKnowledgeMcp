using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Gold;
using QuartzKnowledgeMcp.Api.Silver;

namespace QuartzKnowledgeMcp.Api.Mcp;

public sealed record BronzeCreateToolResponse(
    bool Created,
    BronzeSourceResponse Source);

public sealed record SilverOrganizeToolResponse(
    bool Created,
    SilverServerDraftDetailResponse Draft);

public sealed record GoldPublishToolResponse(
    bool Created,
    GoldCatalogEntryDetailResponse Entry);