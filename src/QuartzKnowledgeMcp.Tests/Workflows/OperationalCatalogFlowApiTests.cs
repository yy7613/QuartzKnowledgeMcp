using System.Net;
using System.Net.Http.Json;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Gold;
using QuartzKnowledgeMcp.Api.Search;
using QuartzKnowledgeMcp.Api.Silver;
using QuartzKnowledgeMcp.Tests.Infrastructure;

namespace QuartzKnowledgeMcp.Tests.Workflows;

[Collection(ApiTestCollection.Name)]
public class OperationalCatalogFlowApiTests(ApiTestFactory factory)
{
    [Fact]
    public async Task EndToEndWorkflow_ExercisesPrimaryApisAndSearchFeatures()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var bronzeCreateResponse = await client.PostAsJsonAsync("/api/bronze/sources", new
        {
            sourceType = "github-readme",
            sourceUri = "https://github.com/example/operational-flow",
            rawContent = "# Operational Flow Server\n\nOperational Flow Server helps teams search docs and automate workflows from MCP clients.\n\nAuthentication: OAuth 2.0\n\nSupported clients: VS Code, Claude Desktop\n\n## Tools\n- search-docs: Search the docs corpus\n- automate-workflow: Run workflow automation",
            importedBy = "workflow-tester"
        });
        var bronze = await bronzeCreateResponse.Content.ReadFromJsonAsync<BronzeSourceResponse>();

        Assert.Equal(HttpStatusCode.Created, bronzeCreateResponse.StatusCode);
        Assert.NotNull(bronze);

        var bronzeList = await client.GetFromJsonAsync<BronzeSourceListResponse>("/api/bronze/sources?page=1&pageSize=10");
        var bronzeDetail = await client.GetFromJsonAsync<BronzeSourceDetailResponse>($"/api/bronze/sources/{bronze!.Id}");

        Assert.NotNull(bronzeList);
        Assert.Contains(bronzeList.Items, item => item.Id == bronze.Id);
        Assert.NotNull(bronzeDetail);
        Assert.Contains("Operational Flow Server", bronzeDetail.RawContent);

        var organizeResponse = await client.PostAsJsonAsync($"/api/bronze/sources/{bronze.Id}:organize", new
        {
            mode = "silver-draft"
        });
        var silver = await organizeResponse.Content.ReadFromJsonAsync<SilverServerDraftDetailResponse>();

        Assert.Equal(HttpStatusCode.Created, organizeResponse.StatusCode);
        Assert.NotNull(silver);

        var silverList = await client.GetFromJsonAsync<SilverServerDraftListResponse>("/api/silver/server-drafts?page=1&pageSize=10");
        var silverDetail = await client.GetFromJsonAsync<SilverServerDraftDetailResponse>($"/api/silver/server-drafts/{silver!.Id}");

        Assert.NotNull(silverList);
        Assert.Contains(silverList.Items, item => item.Id == silver.Id);
        Assert.NotNull(silverDetail);
        Assert.Equal("Operational Flow Server", silverDetail.Name);

        var publishResponse = await client.PostAsJsonAsync($"/api/silver/server-drafts/{silver.Id}:publish", new
        {
            publishedBy = "workflow-publisher"
        });
        var gold = await publishResponse.Content.ReadFromJsonAsync<GoldCatalogEntryDetailResponse>();

        Assert.Equal(HttpStatusCode.Created, publishResponse.StatusCode);
        Assert.NotNull(gold);

        var goldList = await client.GetFromJsonAsync<GoldCatalogEntryListResponse>("/api/gold/catalog?page=1&pageSize=10");
        var goldDetail = await client.GetFromJsonAsync<GoldCatalogEntryDetailResponse>($"/api/gold/catalog/{gold!.Id}");

        Assert.NotNull(goldList);
        Assert.Contains(goldList.Items, item => item.Id == gold.Id);
        Assert.NotNull(goldDetail);
        Assert.Equal("oauth", goldDetail.AuthenticationType);

        var search = await client.GetFromJsonAsync<CatalogSearchResultResponse>("/api/search?q=workflow&sort=relevance&page=1&pageSize=10");
        var suggestions = await client.GetFromJsonAsync<SearchSuggestionResultResponse>("/api/search/suggestions?q=workflow&limit=5");
        var facets = await client.GetFromJsonAsync<SearchFacetResultResponse>("/api/search/facets?q=workflow");

        Assert.NotNull(search);
        Assert.Contains(search.Items, item => item.Id == gold.Id);
        Assert.NotNull(suggestions);
        Assert.NotEmpty(suggestions.Items);
        Assert.NotNull(facets);
        Assert.Contains(facets.AuthTypes, item => item.Value == "oauth");

        var updateResponse = await client.PutAsJsonAsync($"/api/gold/catalog/{gold.Id}", new
        {
            overview = "Operational flow updated overview.",
            setupGuide = "1. Install\n2. Configure\n3. Run",
            references = new[] { "https://example.dev/operational-guide" },
            supportedClients = new[] { "VS Code", "Claude Desktop", "Cursor" },
            updatedBy = "workflow-editor"
        });
        var tagResponse = await client.PutAsJsonAsync($"/api/gold/catalog/{gold.Id}/tags", new
        {
            tags = new[] { "workflow", "automation", "docs" },
            updatedBy = "workflow-editor"
        });
        var updatedDetail = await client.GetFromJsonAsync<GoldCatalogEntryDetailResponse>($"/api/gold/catalog/{gold.Id}");
        var history = await client.GetFromJsonAsync<EntryHistoryPageResponse>($"/api/gold/catalog/{gold.Id}/history?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, tagResponse.StatusCode);
        Assert.NotNull(updatedDetail);
        Assert.Equal("Operational flow updated overview.", updatedDetail.Overview);
        Assert.Equal(["workflow", "automation", "docs"], updatedDetail.Tags);
        Assert.NotNull(history);
        Assert.Equal(3, history.TotalCount);
    }
}