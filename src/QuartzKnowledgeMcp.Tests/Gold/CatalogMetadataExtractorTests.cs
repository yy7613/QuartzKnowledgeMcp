using QuartzKnowledgeMcp.Api.Gold;

namespace QuartzKnowledgeMcp.Tests.Gold;

public class CatalogMetadataExtractorTests
{
    [Theory]
    [InlineData("Authentication: None")]
    [InlineData("authentication none")]
    [InlineData("Auth: none")]
    [InlineData("No authentication required")]
    public void DetectAuthenticationType_ReturnsNone_ForExplicitNoneMarkers(string rawContent)
    {
        var result = CatalogMetadataExtractor.DetectAuthenticationType(rawContent);

        Assert.Equal("none", result);
    }

    [Theory]
    [InlineData("Authentication: Bearer Token")]
    [InlineData("Authentication: API Key")]
    [InlineData("Authentication: personal access token")]
    public void DetectAuthenticationType_ReturnsApiKey_ForTokenBasedMarkers(string rawContent)
    {
        var result = CatalogMetadataExtractor.DetectAuthenticationType(rawContent);

        Assert.Equal("api-key", result);
    }
}