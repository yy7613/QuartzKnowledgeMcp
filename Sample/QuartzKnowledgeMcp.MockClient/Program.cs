using System.Net.Http.Json;
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

var baseUrl = ReadOption(args, "--base-url") ?? "http://localhost:5080";
var runHttp = !HasFlag(args, "--mcp-only");
var runMcp = !HasFlag(args, "--http-only");
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    WriteIndented = true
};

Console.WriteLine($"Base URL: {baseUrl}");

if (runHttp)
{
    await RunHttpFlowAsync(baseUrl, jsonOptions);
}

if (runMcp)
{
    await RunMcpFlowAsync(baseUrl, jsonOptions);
}

async Task RunHttpFlowAsync(string baseUrl, JsonSerializerOptions jsonOptions)
{
    Console.WriteLine("== HTTP flow ==");

    using var client = new HttpClient
    {
        BaseAddress = new Uri(baseUrl)
    };

    var health = await ReadJsonAsync(await client.GetAsync("/health"));
    PrintJson("health", health, jsonOptions);

    var capabilities = await ReadJsonAsync(await client.GetAsync("/api/system/capabilities"));
    PrintJson("system.capabilities", capabilities, jsonOptions);

    var bronze = await ReadJsonAsync(await client.PostAsJsonAsync("/api/bronze/sources", new
    {
        sourceType = "github-readme",
        sourceUri = "https://github.com/example/mock-http-server",
        rawContent = "# Mock HTTP MCP Server\n\nMock server for HTTP verification.\n\nAuthentication: OAuth 2.0\n\nSupported clients: VS Code, Claude Desktop\n\n## Tools\n- search-docs: Search docs\n- sync-issues: Sync issues",
        importedBy = "sample-http"
    }));
    PrintJson("bronze.create", bronze, jsonOptions);
    var bronzeId = bronze.GetProperty("id").GetGuid();

    var bronzeList = await ReadJsonAsync(await client.GetAsync("/api/bronze/sources?page=1&pageSize=5"));
    PrintJson("bronze.list", bronzeList, jsonOptions);

    var preview = await ReadJsonAsync(await client.PostAsJsonAsync($"/api/bronze/sources/{bronzeId}:organize", new
    {
        mode = "silver-draft",
        useLlm = true,
        preview = true
    }));
    PrintJson("silver.preview", preview, jsonOptions);

    var bronzeAfterPreview = await ReadJsonAsync(await client.GetAsync($"/api/bronze/sources/{bronzeId}"));
    PrintJson("bronze.detail.after-preview", bronzeAfterPreview, jsonOptions);

    var silver = await ReadJsonAsync(await client.PostAsJsonAsync($"/api/bronze/sources/{bronzeId}:organize", new
    {
        mode = "silver-draft"
    }));
    PrintJson("silver.organize", silver, jsonOptions);
    var silverId = silver.GetProperty("id").GetGuid();

    var silverDetail = await ReadJsonAsync(await client.GetAsync($"/api/silver/server-drafts/{silverId}"));
    PrintJson("silver.detail", silverDetail, jsonOptions);

    var gold = await ReadJsonAsync(await client.PostAsJsonAsync($"/api/silver/server-drafts/{silverId}:publish", new
    {
        publishedBy = "sample-http"
    }));
    PrintJson("gold.publish", gold, jsonOptions);
    var goldId = gold.GetProperty("id").GetGuid();

    var goldList = await ReadJsonAsync(await client.GetAsync("/api/gold/catalog?page=1&pageSize=5"));
    PrintJson("gold.list", goldList, jsonOptions);

    var search = await ReadJsonAsync(await client.GetAsync("/api/search?q=search&sort=relevance&page=1&pageSize=5"));
    PrintJson("search.query", search, jsonOptions);

    var structuredSearch = await ReadJsonAsync(await client.PostAsJsonAsync("/api/search/query", new
    {
        query = "search",
        tags = new[] { "search" },
        sort = "relevance",
        page = 1,
        pageSize = 5
    }));
    PrintJson("search.query.post", structuredSearch, jsonOptions);

    var suggestions = await ReadJsonAsync(await client.GetAsync("/api/search/suggestions?q=search&limit=5"));
    PrintJson("search.suggestions", suggestions, jsonOptions);

    var facets = await ReadJsonAsync(await client.GetAsync("/api/search/facets?q=search"));
    PrintJson("search.facets", facets, jsonOptions);

    var related = await ReadJsonAsync(await client.GetAsync($"/api/gold/catalog/{goldId}/related?limit=5"));
    PrintJson("gold.related", related, jsonOptions);

    var updated = await ReadJsonAsync(await client.PutAsJsonAsync($"/api/gold/catalog/{goldId}", new
    {
        overview = "Updated from sample HTTP flow.",
        setupGuide = "1. Install\n2. Configure\n3. Run",
        references = new[] { "https://example.dev/http-guide" },
        supportedClients = new[] { "VS Code", "Claude Desktop", "Cursor" },
        updatedBy = "sample-http"
    }));
    PrintJson("gold.update", updated, jsonOptions);

    var tags = await ReadJsonAsync(await client.PutAsJsonAsync($"/api/gold/catalog/{goldId}/tags", new
    {
        tags = new[] { "docs", "registry", "automation" },
        updatedBy = "sample-http"
    }));
    PrintJson("gold.tags", tags, jsonOptions);

    var history = await ReadJsonAsync(await client.GetAsync($"/api/gold/catalog/{goldId}/history?page=1&pageSize=10"));
    PrintJson("gold.history", history, jsonOptions);
}

async Task RunMcpFlowAsync(string baseUrl, JsonSerializerOptions jsonOptions)
{
    Console.WriteLine("== MCP flow ==");

    await using var client = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
    {
        Endpoint = new Uri($"{baseUrl.TrimEnd('/')}/mcp"),
        TransportMode = HttpTransportMode.StreamableHttp
    }));

    var tools = await client.ListToolsAsync();
    Console.WriteLine("mcp.tools: " + string.Join(", ", tools.Select(tool => tool.Name).OrderBy(name => name, StringComparer.OrdinalIgnoreCase)));

    var healthResult = await client.CallToolAsync(
        "get_health",
        new Dictionary<string, object?>(),
        cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.get_health", healthResult, jsonOptions);

    var bronzeCreate = await client.CallToolAsync("create_bronze_source", new Dictionary<string, object?>
    {
        ["sourceType"] = "github-readme",
        ["rawContent"] = "# Mock MCP Tool Server\n\nMock server for MCP verification.\n\nAuthentication: api-key\n\nSupported clients: VS Code, Cline\n\n## Tools\n- fetch-records: Fetch records\n- search-docs: Search docs",
        ["sourceUri"] = "https://github.com/example/mock-mcp-server",
        ["importedBy"] = "sample-mcp"
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.create_bronze_source", bronzeCreate, jsonOptions);

    var searchResult = await client.CallToolAsync("search_catalog", new Dictionary<string, object?>
    {
        ["query"] = "search",
        ["page"] = 1,
        ["pageSize"] = 5
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.search_catalog", searchResult, jsonOptions);

    var suggestionsResult = await client.CallToolAsync("get_search_suggestions", new Dictionary<string, object?>
    {
        ["query"] = "search",
        ["limit"] = 5
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.get_search_suggestions", suggestionsResult, jsonOptions);
}

async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
{
    response.EnsureSuccessStatusCode();
    await using var stream = await response.Content.ReadAsStreamAsync();
    using var document = await JsonDocument.ParseAsync(stream);
    return document.RootElement.Clone();
}

void PrintJson(string label, JsonElement element, JsonSerializerOptions jsonOptions)
{
    Console.WriteLine(label + ":");
    Console.WriteLine(JsonSerializer.Serialize(element, jsonOptions));
}

void PrintToolResult(string label, CallToolResult result, JsonSerializerOptions jsonOptions)
{
    Console.WriteLine(label + ":");
    foreach (var content in result.Content)
    {
        if (content is TextContentBlock textContent)
        {
            Console.WriteLine(textContent.Text);
        }
        else
        {
            Console.WriteLine(JsonSerializer.Serialize(content, jsonOptions));
        }
    }
}

bool HasFlag(string[] args, string flag)
{
    return args.Any(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));
}

string? ReadOption(string[] args, string option)
{
    for (var index = 0; index < args.Length - 1; index++)
    {
        if (string.Equals(args[index], option, StringComparison.OrdinalIgnoreCase))
        {
            return args[index + 1];
        }
    }

    return null;
}