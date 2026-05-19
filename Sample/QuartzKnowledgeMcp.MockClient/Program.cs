using System.Net.Http.Json;
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

var baseUrl = ReadOption(args, "--base-url") ?? "http://localhost:5080";
var qualityOnly = HasFlag(args, "--quality-only");
var runQuality = qualityOnly || HasFlag(args, "--quality-check");
var runHttp = !HasFlag(args, "--mcp-only") && !qualityOnly;
var runMcp = !HasFlag(args, "--http-only") && !qualityOnly;
var seedCount = ReadIntOption(args, "--seed-count") ?? 24;
var inspectionLoops = ReadIntOption(args, "--inspection-loops") ?? 24;
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

if (runQuality)
{
    await RunMcpQualityFlowAsync(baseUrl, jsonOptions, seedCount, inspectionLoops);
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
        rawContent = "# 運用 Search MCP Server\n\n日本語を含む HTTP 検証用のモックサーバーです。search と issue 同期の流れを確認します。\n\nAuthentication: OAuth 2.0\n\nSupported clients: VS Code, Claude Desktop\n\n## Tools\n- search-docs: 社内ドキュメントを検索する\n- sync-issues: issue 状態を同期する",
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
        overview = "Sample HTTP flow から更新された日本語の運用サマリーです。",
        setupGuide = "1. サービスを起動\n2. 認証を設定\n3. 検索を実行",
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
    var runId = Guid.NewGuid().ToString("N")[..8];

    await using var client = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
    {
        Endpoint = new Uri($"{baseUrl.TrimEnd('/')}/mcp"),
        TransportMode = HttpTransportMode.StreamableHttp
    }));

    var tools = await client.ListToolsAsync();
    Console.WriteLine("mcp.tools: " + string.Join(", ", tools.Select(tool => tool.Name).OrderBy(name => name, StringComparer.OrdinalIgnoreCase)));

    var capabilitiesResult = await client.CallToolAsync(
        "get_system_capabilities",
        new Dictionary<string, object?>(),
        cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.get_system_capabilities", capabilitiesResult, jsonOptions);

    var healthResult = await client.CallToolAsync(
        "get_health",
        new Dictionary<string, object?>(),
        cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.get_health", healthResult, jsonOptions);

    var bronzeCreate = await client.CallToolAsync("create_bronze_source", new Dictionary<string, object?>
    {
        ["sourceType"] = "github-readme",
        ["rawContent"] = $"# Mock MCP Tool Server {runId}\n\n日本語を含む MCP 検証サーバー run {runId} です。records 取得とドキュメント検索の確認に使います。\n\nAuthentication: api-key\n\nSupported clients: VS Code, Cline\n\n## Tools\n- fetch-records: レコードを取得する\n- search-docs: ドキュメントを検索する",
        ["sourceUri"] = $"https://github.com/example/mock-mcp-server-{runId}",
        ["importedBy"] = "sample-mcp"
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.create_bronze_source", bronzeCreate, jsonOptions);

    var bronzeCreateJson = ParseToolResultJson(bronzeCreate);
    var bronzeId = bronzeCreateJson.GetProperty("source").GetProperty("id").GetGuid();

    var bronzeListResult = await client.CallToolAsync("list_bronze_sources", new Dictionary<string, object?>
    {
        ["page"] = 1,
        ["pageSize"] = 20
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.list_bronze_sources", bronzeListResult, jsonOptions);

    var bronzeDetailResult = await client.CallToolAsync("get_bronze_source", new Dictionary<string, object?>
    {
        ["bronzeId"] = bronzeId
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.get_bronze_source", bronzeDetailResult, jsonOptions);

    var previewResult = await client.CallToolAsync("organize_bronze_source", new Dictionary<string, object?>
    {
        ["bronzeId"] = bronzeId,
        ["mode"] = "silver-draft",
        ["useLlm"] = true,
        ["preview"] = true
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.organize_bronze_source.preview", previewResult, jsonOptions);

    var bronzeAfterPreviewResult = await client.CallToolAsync("get_bronze_source", new Dictionary<string, object?>
    {
        ["bronzeId"] = bronzeId
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.get_bronze_source.after_preview", bronzeAfterPreviewResult, jsonOptions);

    var silverListAfterPreviewResult = await client.CallToolAsync("list_silver_server_drafts", new Dictionary<string, object?>
    {
        ["page"] = 1,
        ["pageSize"] = 100
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.list_silver_server_drafts.after_preview", silverListAfterPreviewResult, jsonOptions);

    EnsurePreviewDidNotPersist(
        ParseToolResultJson(previewResult),
        ParseToolResultJson(bronzeAfterPreviewResult),
        ParseToolResultJson(silverListAfterPreviewResult),
        bronzeId,
        "mcp.organize_bronze_source.preview");

    var organizeResult = await client.CallToolAsync("organize_bronze_source", new Dictionary<string, object?>
    {
        ["bronzeId"] = bronzeId,
        ["mode"] = "silver-draft"
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.organize_bronze_source", organizeResult, jsonOptions);

    var organizeJson = ParseToolResultJson(organizeResult);
    var silverId = organizeJson.GetProperty("draft").GetProperty("id").GetGuid();

    var silverDetailResult = await client.CallToolAsync("get_silver_server_draft", new Dictionary<string, object?>
    {
        ["draftId"] = silverId
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.get_silver_server_draft", silverDetailResult, jsonOptions);

    var silverListResult = await client.CallToolAsync("list_silver_server_drafts", new Dictionary<string, object?>
    {
        ["page"] = 1,
        ["pageSize"] = 100
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.list_silver_server_drafts", silverListResult, jsonOptions);

    EnsureSearchResult(ParseToolResultJson(silverListResult), silverId, "mcp.list_silver_server_drafts");

    var publishResult = await client.CallToolAsync("publish_silver_server_draft", new Dictionary<string, object?>
    {
        ["silverId"] = silverId,
        ["publishedBy"] = "sample-mcp"
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.publish_silver_server_draft", publishResult, jsonOptions);

    var publishJson = ParseToolResultJson(publishResult);
    var goldId = publishJson.GetProperty("entry").GetProperty("id").GetGuid();

    var updatedResult = await client.CallToolAsync("update_gold_catalog_entry", new Dictionary<string, object?>
    {
        ["entryId"] = goldId,
        ["overview"] = "Sample MCP flow から更新された日本語の運用サマリーです。",
        ["setupGuide"] = "1. サービスを起動\n2. 認証を設定\n3. サンプル検索を実行",
        ["references"] = new[] { "https://example.dev/mcp-guide" },
        ["supportedClients"] = new[] { "VS Code", "Cline" },
        ["updatedBy"] = "sample-mcp"
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.update_gold_catalog_entry", updatedResult, jsonOptions);

    var tagsResult = await client.CallToolAsync("replace_gold_catalog_tags", new Dictionary<string, object?>
    {
        ["entryId"] = goldId,
        ["tags"] = new[] { "mock", "search", "automation" },
        ["updatedBy"] = "sample-mcp"
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.replace_gold_catalog_tags", tagsResult, jsonOptions);

    var searchResult = await client.CallToolAsync("search_catalog", new Dictionary<string, object?>
    {
        ["query"] = "Mock",
        ["page"] = 1,
        ["pageSize"] = 5
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.search_catalog", searchResult, jsonOptions);
    EnsureSearchResult(ParseToolResultJson(searchResult), goldId, "mcp.search_catalog");

    var advancedSearchResult = await client.CallToolAsync("search_catalog_advanced", new Dictionary<string, object?>
    {
        ["query"] = "Mock",
        ["client"] = "VS Code",
        ["sort"] = "relevance",
        ["page"] = 1,
        ["pageSize"] = 5
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.search_catalog_advanced", advancedSearchResult, jsonOptions);
    EnsureSearchResult(ParseToolResultJson(advancedSearchResult), goldId, "mcp.search_catalog_advanced");

    var suggestionsResult = await client.CallToolAsync("get_search_suggestions", new Dictionary<string, object?>
    {
        ["query"] = "Mock",
        ["limit"] = 5
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.get_search_suggestions", suggestionsResult, jsonOptions);
    EnsureItemsPresent(ParseToolResultJson(suggestionsResult), "mcp.get_search_suggestions");

    var facetsResult = await client.CallToolAsync("get_search_facets", new Dictionary<string, object?>
    {
        ["query"] = "Mock"
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.get_search_facets", facetsResult, jsonOptions);
    EnsureFacetValues(ParseToolResultJson(facetsResult), "api-key", "VS Code", "mcp.get_search_facets");

    var goldListResult = await client.CallToolAsync("list_gold_catalog", new Dictionary<string, object?>
    {
        ["page"] = 1,
        ["pageSize"] = 100
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.list_gold_catalog", goldListResult, jsonOptions);
    EnsureSearchResult(ParseToolResultJson(goldListResult), goldId, "mcp.list_gold_catalog");

    var goldDetailResult = await client.CallToolAsync("get_gold_catalog_entry", new Dictionary<string, object?>
    {
        ["entryId"] = goldId
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.get_gold_catalog_entry", goldDetailResult, jsonOptions);
    EnsureDetail(ParseToolResultJson(goldDetailResult), goldId, 0);

    var historyResult = await client.CallToolAsync("get_gold_catalog_history", new Dictionary<string, object?>
    {
        ["entryId"] = goldId,
        ["page"] = 1,
        ["pageSize"] = 10
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.get_gold_catalog_history", historyResult, jsonOptions);
    EnsureHistory(ParseToolResultJson(historyResult), 0);

    var relatedResult = await client.CallToolAsync("get_related_entries", new Dictionary<string, object?>
    {
        ["entryId"] = goldId,
        ["limit"] = 5
    }, cancellationToken: CancellationToken.None);
    PrintToolResult("mcp.get_related_entries", relatedResult, jsonOptions);
}

async Task RunMcpQualityFlowAsync(string baseUrl, JsonSerializerOptions jsonOptions, int seedCount, int inspectionLoops)
{
    Console.WriteLine("== MCP quality flow ==");

    seedCount = Math.Max(20, seedCount);
    inspectionLoops = Math.Max(20, inspectionLoops);

    await using var client = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
    {
        Endpoint = new Uri($"{baseUrl.TrimEnd('/')}/mcp"),
        TransportMode = HttpTransportMode.StreamableHttp
    }));

    async Task<JsonElement> CallToolJsonAsync(string toolName, Dictionary<string, object?> arguments)
    {
        var result = await client.CallToolAsync(toolName, arguments, cancellationToken: CancellationToken.None);
        return ParseToolResultJson(result);
    }

    var toolNames = (await client.ListToolsAsync())
        .Select(tool => tool.Name)
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    var requiredTools = new[]
    {
        "get_system_capabilities",
        "create_bronze_source",
        "organize_bronze_source",
        "publish_silver_server_draft",
        "update_gold_catalog_entry",
        "replace_gold_catalog_tags",
        "get_gold_catalog_entry",
        "get_gold_catalog_history",
        "search_catalog",
        "search_catalog_advanced",
        "get_search_suggestions",
        "get_search_facets",
        "get_related_entries"
    };
    var missingTools = requiredTools.Where(tool => !toolNames.Contains(tool)).ToArray();

    if (missingTools.Length > 0)
    {
        throw new InvalidOperationException("Missing required MCP tools: " + string.Join(", ", missingTools));
    }

    var seeds = Enumerable.Range(1, seedCount)
        .Select(CreateQualitySeed)
        .ToList();
    var seededEntries = new List<SeededEntry>();

    foreach (var seed in seeds)
    {
        var created = await CallToolJsonAsync("create_bronze_source", new Dictionary<string, object?>
        {
            ["sourceType"] = "github-readme",
            ["rawContent"] = BuildLargeRawContent(seed),
            ["sourceUri"] = seed.SourceUri,
            ["importedBy"] = "quality-mcp"
        });
        var bronzeId = created.GetProperty("source").GetProperty("id").GetGuid();

        var preview = await CallToolJsonAsync("organize_bronze_source", new Dictionary<string, object?>
        {
            ["bronzeId"] = bronzeId,
            ["mode"] = "silver-draft",
            ["useLlm"] = true,
            ["preview"] = true
        });
        var bronzeAfterPreview = await CallToolJsonAsync("get_bronze_source", new Dictionary<string, object?>
        {
            ["bronzeId"] = bronzeId
        });
        var silverListAfterPreview = await CallToolJsonAsync("list_silver_server_drafts", new Dictionary<string, object?>
        {
            ["page"] = 1,
            ["pageSize"] = 100
        });

        EnsurePreviewDidNotPersist(
            preview,
            bronzeAfterPreview,
            silverListAfterPreview,
            bronzeId,
            $"quality preview seed {seed.Index}");

        var organized = await CallToolJsonAsync("organize_bronze_source", new Dictionary<string, object?>
        {
            ["bronzeId"] = bronzeId,
            ["mode"] = "silver-draft"
        });
        var silverId = organized.GetProperty("draft").GetProperty("id").GetGuid();

        var published = await CallToolJsonAsync("publish_silver_server_draft", new Dictionary<string, object?>
        {
            ["silverId"] = silverId,
            ["publishedBy"] = "quality-mcp"
        });
        var goldId = published.GetProperty("entry").GetProperty("id").GetGuid();

        await CallToolJsonAsync("update_gold_catalog_entry", new Dictionary<string, object?>
        {
            ["entryId"] = goldId,
            ["overview"] = BuildUpdatedOverview(seed),
            ["setupGuide"] = BuildSetupGuide(seed),
            ["references"] = new[]
            {
                seed.SourceUri,
                $"https://example.dev/quality/{seed.ClusterTag}/{seed.Index:D2}/guide",
                $"https://example.dev/quality/{seed.ClusterTag}/{seed.Index:D2}/ops"
            },
            ["supportedClients"] = seed.SupportedClients,
            ["updatedBy"] = "quality-mcp"
        });

        await CallToolJsonAsync("replace_gold_catalog_tags", new Dictionary<string, object?>
        {
            ["entryId"] = goldId,
            ["tags"] = seed.Tags,
            ["updatedBy"] = "quality-mcp"
        });

        seededEntries.Add(new SeededEntry(seed, bronzeId, silverId, goldId));

        if (seed.Index % 5 == 0 || seed.Index == seedCount)
        {
            Console.WriteLine($"mcp.quality.seeded: {seed.Index}/{seedCount}");
        }
    }

    var loopResults = new List<QualityLoopResult>();

    for (var loopIndex = 1; loopIndex <= inspectionLoops; loopIndex++)
    {
        var entry = seededEntries[(loopIndex - 1) % seededEntries.Count];

        try
        {
            var capabilities = await CallToolJsonAsync("get_system_capabilities", new Dictionary<string, object?>());
            var search = await CallToolJsonAsync("search_catalog", new Dictionary<string, object?>
            {
                ["query"] = entry.Seed.Query,
                ["page"] = 1,
                ["pageSize"] = 10
            });
            var advanced = await CallToolJsonAsync("search_catalog_advanced", new Dictionary<string, object?>
            {
                ["query"] = entry.Seed.Query,
                ["tags"] = new[] { entry.Seed.ClusterTag },
                ["authType"] = entry.Seed.AuthFilter,
                ["client"] = entry.Seed.SupportedClients[0],
                ["sort"] = "relevance",
                ["page"] = 1,
                ["pageSize"] = 10
            });
            var suggestions = await CallToolJsonAsync("get_search_suggestions", new Dictionary<string, object?>
            {
                ["query"] = entry.Seed.Query,
                ["limit"] = 5
            });
            var facets = await CallToolJsonAsync("get_search_facets", new Dictionary<string, object?>
            {
                ["query"] = entry.Seed.Query,
                ["client"] = entry.Seed.SupportedClients[0]
            });
            var detail = await CallToolJsonAsync("get_gold_catalog_entry", new Dictionary<string, object?>
            {
                ["entryId"] = entry.GoldId
            });
            var history = await CallToolJsonAsync("get_gold_catalog_history", new Dictionary<string, object?>
            {
                ["entryId"] = entry.GoldId,
                ["page"] = 1,
                ["pageSize"] = 10
            });
            var related = await CallToolJsonAsync("get_related_entries", new Dictionary<string, object?>
            {
                ["entryId"] = entry.GoldId,
                ["limit"] = 5
            });

            EnsureCapabilities(capabilities, loopIndex);
            EnsureSearchResult(search, entry.GoldId, $"search_catalog round {loopIndex}");
            EnsureSearchResult(advanced, entry.GoldId, $"search_catalog_advanced round {loopIndex}");
            EnsureItemsPresent(suggestions, $"get_search_suggestions round {loopIndex}");
            EnsureFacetCoverage(facets, entry.Seed, loopIndex);
            EnsureDetail(detail, entry.GoldId, loopIndex);
            EnsureHistory(history, loopIndex);
            EnsureRelatedEntries(related, entry.GoldId, loopIndex);

            loopResults.Add(new QualityLoopResult(loopIndex, entry.Seed.DisplayName, true, null));
        }
        catch (Exception ex)
        {
            loopResults.Add(new QualityLoopResult(loopIndex, entry.Seed.DisplayName, false, ex.Message));
        }

        if (loopIndex % 5 == 0 || loopIndex == inspectionLoops)
        {
            var passed = loopResults.Count(result => result.Passed);
            var failed = loopResults.Count - passed;
            Console.WriteLine($"mcp.quality.loop: {loopIndex}/{inspectionLoops} passed={passed} failed={failed}");
        }
    }

    var finalPassed = loopResults.Count(result => result.Passed);
    var finalFailed = loopResults.Count - finalPassed;
    var summary = new
    {
        seedCount = seededEntries.Count,
        inspectionLoops,
        passed = finalPassed,
        failed = finalFailed,
        failures = loopResults.Where(result => !result.Passed).Take(5).ToList(),
        sampleGoldIds = seededEntries.Take(5).Select(entry => entry.GoldId).ToList()
    };

    Console.WriteLine("mcp.quality.summary:");
    Console.WriteLine(JsonSerializer.Serialize(summary, jsonOptions));

    if (finalFailed > 0)
    {
        throw new InvalidOperationException($"MCP quality flow completed with {finalFailed} failed loop(s).");
    }
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

JsonElement ParseToolResultJson(CallToolResult result)
{
    var text = result.Content
        .OfType<TextContentBlock>()
        .Select(content => content.Text)
        .FirstOrDefault(content => !string.IsNullOrWhiteSpace(content));

    if (string.IsNullOrWhiteSpace(text))
    {
        throw new InvalidOperationException("Tool result did not contain JSON text.");
    }

    using var document = JsonDocument.Parse(text);
    return document.RootElement.Clone();
}

Guid? TryGetFirstItemId(CallToolResult result)
{
    var json = ParseToolResultJson(result);
    if (!json.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
    {
        return null;
    }

    foreach (var item in items.EnumerateArray())
    {
        if (item.TryGetProperty("id", out var idProperty))
        {
            return idProperty.GetGuid();
        }
    }

    return null;
}

void EnsureCapabilities(JsonElement capabilities, int loopIndex)
{
    var search = capabilities.GetProperty("search");
    if (!search.GetProperty("supportsStructuredSearch").GetBoolean() ||
        !search.GetProperty("supportsSuggestions").GetBoolean() ||
        !search.GetProperty("supportsFacets").GetBoolean() ||
        !search.GetProperty("supportsRelatedEntries").GetBoolean())
    {
        throw new InvalidOperationException($"Loop {loopIndex}: capabilities search flags are incomplete.");
    }
}

void EnsureSearchResult(JsonElement response, Guid expectedId, string label)
{
    if (!response.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
    {
        throw new InvalidOperationException($"{label}: missing items array.");
    }

    var matched = items.EnumerateArray().Any(item => item.GetProperty("id").GetGuid() == expectedId);
    if (!matched)
    {
        throw new InvalidOperationException($"{label}: expected entry was not returned.");
    }
}

void EnsureItemsPresent(JsonElement response, string label)
{
    if (!response.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
    {
        throw new InvalidOperationException($"{label}: no items returned.");
    }
}

void EnsureFacetCoverage(JsonElement response, QualitySeed seed, int loopIndex)
{
    var tags = response.GetProperty("tags");
    var clients = response.GetProperty("clients");
    var tagMatched = tags.EnumerateArray().Any(item => string.Equals(item.GetProperty("value").GetString(), seed.ClusterTag, StringComparison.OrdinalIgnoreCase));
    var clientMatched = clients.EnumerateArray().Any(item => string.Equals(item.GetProperty("value").GetString(), seed.SupportedClients[0], StringComparison.OrdinalIgnoreCase));

    if (!tagMatched || !clientMatched)
    {
        throw new InvalidOperationException($"Loop {loopIndex}: facet response did not include expected tag/client coverage.");
    }
}

void EnsureFacetValues(JsonElement response, string expectedAuthType, string expectedClient, string label)
{
    var authMatched = response.GetProperty("authTypes")
        .EnumerateArray()
        .Any(item => string.Equals(item.GetProperty("value").GetString(), expectedAuthType, StringComparison.OrdinalIgnoreCase));
    var clientMatched = response.GetProperty("clients")
        .EnumerateArray()
        .Any(item => string.Equals(item.GetProperty("value").GetString(), expectedClient, StringComparison.OrdinalIgnoreCase));

    if (!authMatched || !clientMatched)
    {
        throw new InvalidOperationException($"{label}: facet response did not include expected auth/client values.");
    }
}

void EnsurePreviewDidNotPersist(JsonElement preview, JsonElement bronzeDetail, JsonElement silverList, Guid bronzeId, string label)
{
    if (!preview.GetProperty("draft").GetProperty("preview").GetBoolean())
    {
        throw new InvalidOperationException($"{label}: preview organize response was not marked as preview.");
    }

    var bronzeStatus = bronzeDetail.GetProperty("status").GetString();
    if (!string.Equals(bronzeStatus, "imported", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException($"{label}: bronze status changed during preview.");
    }

    if (!silverList.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
    {
        throw new InvalidOperationException($"{label}: silver list did not return an items array.");
    }

    var persisted = items.EnumerateArray().Any(item =>
        item.TryGetProperty("bronzeSourceId", out var itemBronzeId) &&
        itemBronzeId.GetGuid() == bronzeId);

    if (persisted)
    {
        throw new InvalidOperationException($"{label}: preview organize persisted a silver draft.");
    }
}

void EnsureDetail(JsonElement detail, Guid expectedId, int loopIndex)
{
    if (detail.GetProperty("id").GetGuid() != expectedId)
    {
        throw new InvalidOperationException($"Loop {loopIndex}: gold detail returned a different entry.");
    }
}

void EnsureHistory(JsonElement history, int loopIndex)
{
    var totalCount = history.GetProperty("totalCount").GetInt32();
    if (totalCount < 3)
    {
        throw new InvalidOperationException($"Loop {loopIndex}: expected at least 3 history records.");
    }
}

void EnsureRelatedEntries(JsonElement response, Guid entryId, int loopIndex)
{
    if (!response.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
    {
        throw new InvalidOperationException($"Loop {loopIndex}: related entries were empty.");
    }

    var containsSelf = items.EnumerateArray().Any(item => item.GetProperty("id").GetGuid() == entryId);
    if (containsSelf)
    {
        throw new InvalidOperationException($"Loop {loopIndex}: related entries contained the source entry itself.");
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

int? ReadIntOption(string[] args, string option)
{
    var raw = ReadOption(args, option);
    return int.TryParse(raw, out var value) ? value : null;
}

QualitySeed CreateQualitySeed(int index)
{
    var clusters = new[]
    {
        new { Tag = "registry", Query = "registry", Japanese = "レジストリ運用", Tools = new[] { "search-registry", "sync-registry", "search-docs", "resolve-entry" } },
        new { Tag = "workflow", Query = "workflow", Japanese = "ワークフロー自動化", Tools = new[] { "run-workflow", "dispatch-job", "search-docs", "sync-issues" } },
        new { Tag = "retrieval", Query = "retrieval", Japanese = "検索取得", Tools = new[] { "query-index", "fetch-records", "search-docs", "build-snippets" } },
        new { Tag = "governance", Query = "governance", Japanese = "ガバナンス監査", Tools = new[] { "audit-config", "review-policy", "search-docs", "report-risk" } },
        new { Tag = "ops", Query = "ops", Japanese = "運用監視", Tools = new[] { "check-health", "tail-logs", "search-docs", "restart-job" } },
        new { Tag = "analytics", Query = "analytics", Japanese = "分析ダッシュボード", Tools = new[] { "search-dashboards", "aggregate-usage", "search-docs", "export-metrics" } }
    };
    var authProfiles = new[]
    {
        new { Label = "OAuth 2.0", Filter = "oauth" },
        new { Label = "API Key", Filter = "api-key" },
        new { Label = "Bearer Token", Filter = "api-key" },
        new { Label = "None", Filter = "none" }
    };
    var clientProfiles = new[]
    {
        new[] { "VS Code", "Claude Desktop" },
        new[] { "VS Code", "Cursor" },
        new[] { "Claude Desktop", "Cline" },
        new[] { "VS Code", "Claude Desktop", "Cursor" }
    };

    var cluster = clusters[(index - 1) % clusters.Length];
    var auth = authProfiles[(index - 1) % authProfiles.Length];
    var clients = clientProfiles[(index - 1) % clientProfiles.Length];
    var displayName = $"品質ループ MCP サーバー {index:D2} {cluster.Japanese}";
    var tags = new[] { "mcp", cluster.Tag, cluster.Japanese, "quality", auth.Filter, "日本語" };

    return new QualitySeed(
        index,
        displayName,
        cluster.Tag,
        cluster.Japanese,
        cluster.Query,
        auth.Label,
        auth.Filter,
        clients,
        cluster.Tools,
        tags,
        $"https://example.dev/quality/{cluster.Tag}/{index:D2}");
}

string BuildLargeRawContent(QualitySeed seed)
{
    var repeatedSections = string.Join("\n\n", Enumerable.Range(1, 6).Select(section =>
        $"### 運用シナリオ {section}\n{seed.DisplayName} は {seed.ClusterTag} / {seed.JapaneseLabel} 領域の反復検査を扱います。operator が {seed.Query} を検索し、{seed.ToolNames[0]} と {seed.ToolNames[1]} を使って調査する実務シナリオを round {seed.Index:D2} 向けに記述しています。"));

    return $"# {seed.DisplayName}\n\n{seed.DisplayName} は {seed.JapaneseLabel} を扱う MCP サーバーです。local catalog ingestion、search、related entry 検証を現実的な文量でストレスするために使います。\n\nAuthentication: {seed.AuthLine}\n\nSupported clients: {string.Join(", ", seed.SupportedClients)}\n\n## Overview\nこのサーバーは日本語を多めに含む品質検査データです。MCP metadata を厚く保持し、search behavior を確認し、更新を繰り返した後でも related entries が見つかることを確かめます。content を大きめにして ingestion と normalization が実運用に近いサイズを通るようにしています。\n\n## Tools\n- {seed.ToolNames[0]}: {seed.JapaneseLabel} の主要操作\n- {seed.ToolNames[1]}: {seed.JapaneseLabel} の補助操作\n- {seed.ToolNames[2]}: 共通ドキュメント検索\n- {seed.ToolNames[3]}: エスカレーションと一括処理の支援\n\n## Setup\n1. サーバーパッケージと runtime 前提条件を配置する。\n2. 認証、routing、logging を設定する。\n3. {seed.SupportedClients[0]} から接続し、{seed.Query} を含むサンプル検索を実行する。\n4. health、ingestion、search 結果を確認する。\n\n## Operational Notes\n{repeatedSections}\n\n## References\n- {seed.SourceUri}\n- https://example.dev/quality/{seed.ClusterTag}/reference\n- https://example.dev/quality/{seed.ClusterTag}/operations\n";
}

string BuildUpdatedOverview(QualitySeed seed)
{
    return $"{seed.DisplayName} は {seed.JapaneseLabel} 向けに調整した curated quality sample であり、反復 inspection loop を通じて MCP search、history、related-entry behavior を検証します。";
}

string BuildSetupGuide(QualitySeed seed)
{
    return string.Join("\n", new[]
    {
        $"1. {seed.DisplayName} を起動する",
        $"2. {seed.AuthLine} で認証する",
        $"3. {seed.SupportedClients[0]} から接続する",
        $"4. {seed.ToolNames[0]} と {seed.ToolNames[2]} を使って {seed.Query} を検証する"
    });
}

sealed record QualitySeed(
    int Index,
    string DisplayName,
    string ClusterTag,
    string JapaneseLabel,
    string Query,
    string AuthLine,
    string AuthFilter,
    string[] SupportedClients,
    string[] ToolNames,
    string[] Tags,
    string SourceUri);

sealed record SeededEntry(
    QualitySeed Seed,
    Guid BronzeId,
    Guid SilverId,
    Guid GoldId);

sealed record QualityLoopResult(
    int Loop,
    string DisplayName,
    bool Passed,
    string? Failure);