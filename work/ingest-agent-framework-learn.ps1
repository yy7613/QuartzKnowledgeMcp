param(
    [string]$BaseUrl = "http://localhost:5080"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-JsonApi {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Path,
        [object]$Body
    )

    $params = @{
        Uri = "$BaseUrl$Path"
        Method = $Method
    }

    if ($null -ne $Body) {
        $params.ContentType = "application/json; charset=utf-8"
        $params.Body = $Body | ConvertTo-Json -Depth 10
    }

    Invoke-RestMethod @params
}

function Publish-LearnEntry {
    param(
        [Parameter(Mandatory = $true)][pscustomobject]$Entry
    )

    $bronze = Invoke-JsonApi -Method Post -Path "/api/bronze/sources" -Body @{
        sourceType = "docs-url"
        sourceUri = $Entry.SourceUri
        rawContent = $Entry.RawContent
        importedBy = "copilot-learn-ingest"
    }

    $silver = Invoke-JsonApi -Method Post -Path "/api/bronze/sources/$($bronze.id):organize" -Body @{
        mode = "silver-draft"
    }

    $gold = Invoke-JsonApi -Method Post -Path "/api/silver/server-drafts/$($silver.id):publish" -Body @{
        publishedBy = "copilot-learn-ingest"
    }

    $null = Invoke-JsonApi -Method Put -Path "/api/gold/catalog/$($gold.id)" -Body @{
        overview = $Entry.Overview
        setupGuide = $Entry.SetupGuide
        references = $Entry.References
        supportedClients = $Entry.SupportedClients
        updatedBy = "copilot-learn-curation"
    }

    $null = Invoke-JsonApi -Method Put -Path "/api/gold/catalog/$($gold.id)/tags" -Body @{
        tags = $Entry.Tags
        updatedBy = "copilot-learn-curation"
    }

    [pscustomobject]@{
        Title = $Entry.Title
        BronzeId = $bronze.id
        SilverId = $silver.id
        GoldId = $gold.id
    }
}

$entries = @(
    [pscustomobject]@{
        Title = "Microsoft Agent Framework Your First Agent"
        SourceUri = "https://learn.microsoft.com/en-us/agent-framework/get-started/your-first-agent"
        RawContent = @'
# Microsoft Agent Framework Your First Agent

Step 1 in the Microsoft Agent Framework getting-started path. The tutorial shows the smallest C# path to create an agent, run a prompt, and stream a response.

Authentication: OAuth 2.0, DefaultAzureCredential, ManagedIdentityCredential, or API key depending on the backing service.

## Summary
- Install prerelease packages Azure.AI.Projects, Azure.Identity, and Microsoft.Agents.AI.Foundry.
- Read AZURE_OPENAI_ENDPOINT and AZURE_OPENAI_DEPLOYMENT_NAME from environment variables.
- Create an AIProjectClient from the project endpoint and DefaultAzureCredential.
- Convert the project client into an AIAgent with AsAIAgent.
- Run the agent with RunAsync for a simple response.
- Stream output with RunStreamingAsync for incremental UI updates.
- The Learn page warns that DefaultAzureCredential is convenient for development but a specific credential such as ManagedIdentityCredential is safer for production.

## CSharp Notes
- var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
- var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini"
- AIAgent agent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential()).AsAIAgent(model: deploymentName, instructions: "You are a friendly assistant. Keep your answers brief.", name: "HelloAgent")
- Console.WriteLine(await agent.RunAsync("What is the largest city in France?"))
- await foreach (var update in agent.RunStreamingAsync("Tell me a one-sentence fun fact.")) { Console.Write(update); }

## Packages
- Azure.AI.Projects --prerelease
- Azure.Identity
- Microsoft.Agents.AI.Foundry --prerelease

## Related Pages
- Overview
- Step 2 Add Tools
- Agents overview
- Providers overview

## Tools
- AIProjectClient: connects to the Foundry project endpoint
- AsAIAgent: creates the agent runtime wrapper
- RunAsync: one-shot execution
- RunStreamingAsync: streamed execution
'@
        Overview = "C# で最短の Agent Framework エージェントを作る入門ページです。AIProjectClient と DefaultAzureCredential から AIAgent を生成し、RunAsync と RunStreamingAsync の両方で最初の対話を試す流れを整理しています。運用面では DefaultAzureCredential の代わりに ManagedIdentityCredential など明示的な資格情報を検討する点も押さえています。"
        SetupGuide = "1. Azure.AI.Projects、Azure.Identity、Microsoft.Agents.AI.Foundry を追加する。`n2. AZURE_OPENAI_ENDPOINT と AZURE_OPENAI_DEPLOYMENT_NAME を設定する。`n3. AIProjectClient と DefaultAzureCredential から AsAIAgent を呼び出す。`n4. RunAsync で単発実行し、必要なら RunStreamingAsync でストリーミング応答を確認する。"
        References = @(
            "https://learn.microsoft.com/en-us/agent-framework/get-started/your-first-agent",
            "https://learn.microsoft.com/en-us/agent-framework/get-started/add-tools",
            "https://learn.microsoft.com/en-us/agent-framework/agents/",
            "https://learn.microsoft.com/en-us/agent-framework/agents/providers/",
            "https://github.com/microsoft/agent-framework/tree/main/dotnet/samples/01-get-started/01_hello_agent"
        )
        SupportedClients = @(
            "VS Code"
        )
        Tags = @(
            "agent-framework",
            "getting-started",
            "first-agent",
            "csharp",
            "入門"
        )
    },
    [pscustomobject]@{
        Title = "Microsoft Agent Framework Agent Types"
        SourceUri = "https://learn.microsoft.com/en-us/agent-framework/agents/"
        RawContent = @'
# Microsoft Agent Framework Agent Types

The agents overview explains the shared runtime model, the AIAgent abstraction, simple agents backed by inference services, custom agents, and proxies for remote agents.

Authentication: OAuth 2.0, OpenID Connect, API key, or bearer token depending on the provider you connect to.

## Default Runtime Model
- Agent Framework uses a structured runtime loop that coordinates user interaction, model inference, and tool execution.
- The runtime is designed to be deterministic around orchestration even when the model makes dynamic decisions.
- The page highlights responsible AI, third-party system risk, and production validation responsibilities.

## Simple Agents
- Any Microsoft.Extensions.AI.IChatClient implementation can be wrapped by ChatClientAgent.
- Built-in capabilities include function calling, multi-turn conversations, service-provided tools such as MCP and code execution, and structured outputs.
- ChatClientAgent is the simplest abstraction when an inference service already exposes IChatClient.

## Custom And Remote Agents
- AIAgent is the common base type for all agents.
- Custom agents can subclass AIAgent for full control over behavior.
- Remote agent protocols such as A2A are supported through proxy AIAgent implementations.

## SDK Options
- Foundry models can be reached through Azure.AI.OpenAI, OpenAI SDK, or Azure.AI.Inference.
- Foundry Agents use Azure.AI.Projects with Microsoft.Agents.AI.Foundry.
- Azure OpenAI, OpenAI, Anthropic, and Foundry Anthropic are all called out with different endpoint and credential options.
- The page recommends careful use of DefaultAzureCredential in production and suggests specific credentials such as ManagedIdentityCredential where possible.

## CSharp Notes
- var agent = new ChatClientAgent(chatClient, instructions: "You are a helpful assistant")
- OpenAIClient client = new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions)
- OpenAIClient client = new OpenAIClient(new BearerTokenPolicy(new DefaultAzureCredential(), "https://ai.azure.com/.default"), clientOptions)
- AIAgent agent = new AIProjectClient(new Uri(serviceUrl), new DefaultAzureCredential()).AsAIAgent(model: deploymentName, instructions: "You are good at telling jokes.", name: "Joker")

## Provider Families
- Foundry Agent Service
- Foundry Models via ChatCompletion or Responses
- Azure OpenAI ChatCompletion and Responses
- OpenAI ChatCompletion and Responses
- Anthropic and Foundry Anthropic
- Any other IChatClient implementation

## Tools
- ChatClientAgent
- AIAgent
- IChatClient
- A2A proxy agents
- Azure AI Projects SDK
- OpenAI SDK
'@
        Overview = "Agent Framework のエージェント種別と共通抽象をまとめたページです。ChatClientAgent と AIAgent の役割、IChatClient ベースの simple agent、A2A 経由の remote agent、Foundry や Azure OpenAI を含む provider 別の SDK 選択肢を横断的に確認できます。実運用では third-party system の扱いと DefaultAzureCredential の本番利用に注意が必要です。"
        SetupGuide = "1. 使いたい推論バックエンドと SDK を選ぶ。`n2. IChatClient があるなら ChatClientAgent、完全制御が必要なら AIAgent 継承を選ぶ。`n3. provider ごとの endpoint と credential を設定する。`n4. 関数呼び出し、MCP、structured outputs、multi-turn history の必要性に応じて agent を拡張する。"
        References = @(
            "https://learn.microsoft.com/en-us/agent-framework/agents/",
            "https://learn.microsoft.com/en-us/agent-framework/agents/providers/custom",
            "https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai#the-ichatclient-interface",
            "https://learn.microsoft.com/en-us/agent-framework/agents/running-agents",
            "https://github.com/microsoft/agent-framework/blob/main/TRANSPARENCY_FAQS.md"
        )
        SupportedClients = @()
        Tags = @(
            "agent-framework",
            "agents",
            "aiagent",
            "chatclientagent",
            "a2a"
        )
    },
    [pscustomobject]@{
        Title = "Microsoft Agent Framework Workflows"
        SourceUri = "https://learn.microsoft.com/en-us/agent-framework/workflows/"
        RawContent = @'
# Microsoft Agent Framework Workflows

The workflows overview explains graph-based orchestration for AI agents and business processes with explicit execution control.

Authentication: OAuth 2.0 or API key depending on the agents and external services used inside the workflow.

## Overview
- Workflows combine AI agents, functions, human approvals, and external APIs into an explicit execution graph.
- The page positions workflows as the choice for well-defined multi-step processes where developers need strict control over execution order.

## Agent Versus Workflow
- Agents are LLM-driven and choose steps dynamically based on context and tools.
- Workflows define the execution path explicitly and can include agents as components.
- Workflows are intended for business processes, external integrations, and human-in-the-loop control.

## Key Features
- Type safety with validation to reduce runtime routing errors.
- Flexible graph control flow with executors and edges.
- Conditional routing, parallel processing, and dynamic execution paths.
- External request/response integration and human-in-the-loop support.
- Checkpointing for long-running server-side execution and recovery.
- Multi-agent orchestration patterns including sequential, concurrent, hand-off, and magentic flows.

## Workflow APIs
- Functional Workflow API is Python experimental and models logic as async functions with workflow and step decorators.
- Workflow Builder and Execution models workflows as directed graphs with WorkflowBuilder, executors, and edges.
- Both APIs support events, streaming, HITL, and checkpoints.
- The docs recommend starting with the functional style when native control flow is enough, and moving to WorkflowBuilder when strict type-validated routing is needed.

## Core Concepts
- Executors are units of work that can be agents or custom code.
- Edges move messages between executors and can include conditions.
- Events provide lifecycle and execution observability.
- Workflow Builder and Execution manages supersteps and streaming or non-streaming execution.

## Samples
- C# sample in dotnet/samples/03-workflows
- Python sample in python/samples/03-workflows

## Tools
- WorkflowBuilder
- Executors
- Edges
- Events
- Checkpoints
- RequestInfoExecutor for HITL
'@
        Overview = "ワークフローを Agent Framework で明示的に組み立てるための中核資料です。agent と workflow の責務差分、type-safe な executors と edges、checkpointing、human-in-the-loop、sequential・concurrent・hand-off・magentic などの実行パターンを整理しています。固定トポロジーや厳密なルーティング制御が必要なケースで特に有用です。"
        SetupGuide = "1. agent で足りるか、workflow で明示制御すべきかを切り分ける。`n2. executor と edge を設計し、型付きメッセージの流れを決める。`n3. checkpointing と human-in-the-loop の境界を定義する。`n4. 必要に応じて WorkflowBuilder ベースのグラフへ落とし込み、samples を起点に実装する。"
        References = @(
            "https://learn.microsoft.com/en-us/agent-framework/workflows/",
            "https://learn.microsoft.com/en-us/agent-framework/workflows/workflows",
            "https://learn.microsoft.com/en-us/agent-framework/workflows/executors",
            "https://github.com/microsoft/agent-framework/tree/main/dotnet/samples/03-workflows",
            "https://github.com/microsoft/agent-framework/tree/main/python/samples/03-workflows"
        )
        SupportedClients = @(
            "Azure Functions"
        )
        Tags = @(
            "agent-framework",
            "workflows",
            "workflow-builder",
            "ワークフロー",
            "hitl"
        )
    },
    [pscustomobject]@{
        Title = "Microsoft Agent Framework Integrations"
        SourceUri = "https://learn.microsoft.com/en-us/agent-framework/integrations/"
        RawContent = @'
# Microsoft Agent Framework Integrations

The integrations overview lists the main integration surfaces around Agent Framework, including hosted agents, UI frameworks, memory providers, RAG providers, and vector stores.

Authentication: API key, OAuth 2.0, and provider-specific credentials depending on the integration target.

## Hosted And UI Integrations
- Microsoft Foundry Hosted Agents are listed as a primary hosted integration.
- UI integrations called out on the page include AG UI, Agent Framework Dev UI, and Purview, all marked preview.

## State And Memory
- Chat History Providers: In-Memory released, Cosmos DB preview.
- Memory AI Context Providers: Chat History Memory Provider released.
- Retrieval Augmented Generation AI Context Providers: Neo4j GraphRAG preview and Text Search released.

## Vector Stores
- The framework relies on Microsoft.Extensions.VectorData.Abstractions for a unified vector store programming model.
- Listed vector stores include Azure AI Search, Cosmos DB MongoDB vCore, Cosmos DB NoSQL, Couchbase, Elasticsearch, In-Memory, MongoDB, Neon Serverless Postgres, Oracle, Pinecone, Postgres, Qdrant, Redis, SQL Server, SQLite, and Weaviate.
- The page explicitly warns that not every connector is maintained by Microsoft and that provider quality, licensing, and SDK support must be reviewed.

## Operational Guidance
- Vector stores can be used for RAG and memory persistence.
- The docs point to vector databases guidance for embedding generation and vector or hybrid search.
- Azure Functions Durable is positioned as the next step for integration scenarios.

## Tools
- Microsoft.Extensions.VectorData.Abstractions
- In-Memory Chat History Provider
- Cosmos DB Chat History Provider
- Chat History Memory Provider
- Neo4j GraphRAG Provider
- Text Search Provider
'@
        Overview = "Integrations ページは、Agent Framework の統合ポイントと周辺接続面を俯瞰する資料です。Foundry Hosted Agents、AG UI や Agent Framework Dev UI、chat history provider、memory/RAG context provider、そして Azure AI Search や SQLite を含む多数の vector store 実装を一覧できます。RAG と記憶の設計、connector の保守主体、SDK の公式サポート状況まで確認したいときに有効です。"
        SetupGuide = "1. Hosted agent、UI、memory、RAG、vector store のどの層を統合したいかを決める。`n2. Chat history provider と memory provider の責務を分けて選定する。`n3. VectorData abstractions を前提に、対象ストアの保守主体と SDK 対応状況を確認する。`n4. 長時間実行や orchestration が必要なら Azure Functions Durable 方向も合わせて検討する。"
        References = @(
            "https://learn.microsoft.com/en-us/agent-framework/integrations/",
            "https://learn.microsoft.com/en-us/azure/ai-foundry/agents/concepts/hosted-agents",
            "https://learn.microsoft.com/en-us/dotnet/ai/vector-stores/overview",
            "https://learn.microsoft.com/en-us/agent-framework/integrations/azure-functions",
            "https://github.com/microsoft-foundry/foundry-samples/tree/main/samples/python/hosted-agents/agent-framework"
        )
        SupportedClients = @(
            "AG UI",
            "Agent Framework Dev UI",
            "Purview",
            "Azure Functions"
        )
        Tags = @(
            "agent-framework",
            "integrations",
            "rag",
            "azure-functions",
            "sqlite"
        )
    }
)

$results = foreach ($entry in $entries) {
    Publish-LearnEntry -Entry $entry
}

$search = Invoke-JsonApi -Method Get -Path "/api/dashboard/search?q=Agent%20Framework&stage=gold&limit=10&sort=updated"
$workflowSearch = Invoke-JsonApi -Method Get -Path "/api/dashboard/search?q=%E3%83%AF%E3%83%BC%E3%82%AF%E3%83%95%E3%83%AD%E3%83%BC&stage=gold&limit=10&sort=updated"
$related = Invoke-JsonApi -Method Get -Path "/api/gold/catalog/$($results[2].GoldId)/related?limit=5"

Write-Host "Published entries:"
$results | Format-Table -AutoSize

Write-Host "`nAgent Framework search titles:"
$search.items | Select-Object displayName, stage, updatedAtUtc | Format-Table -AutoSize

Write-Host "`nJapanese workflow search titles:"
$workflowSearch.items | Select-Object displayName, stage, updatedAtUtc | Format-Table -AutoSize

Write-Host "`nRelated to workflows entry:"
$related.items | Select-Object displayName, tags | Format-Table -AutoSize