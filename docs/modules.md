# モジュール設計

## モジュール一覧
| モジュール名 | 責務 | 依存してよいモジュール |
|:--|:--|:--|
| Domain | エンティティ、値オブジェクト、ドメインルール、Port 契約 | なし |
| Application | ユースケース、トランザクション境界、オーケストレーション | Domain |
| Api | HTTP エンドポイント、入力検証、DTO 変換 | Application, Query |
| Query | 検索・参照専用ユースケース | Domain |
| BronzeIngestion | 生データの取り込み、重複判定、Bronze 永続化 | Domain, HistoryTracking |
| SilverNormalization | Bronze から正規化済み下書きを生成 | Domain, HistoryTracking |
| GoldCuration | Silver から公開カタログを生成、更新する | Domain, TagManagement, HistoryTracking |
| AgentOrganization | LLM を使った自動整理を行う | Domain, SilverNormalization, GoldCuration, HistoryTracking |
| RuleBasedOrganization | LLM 無効時の規則ベース整理を行う | Domain, SilverNormalization, GoldCuration, HistoryTracking |
| SearchQuery | Gold カタログの検索、詳細取得を行う | Domain, GraphProjection, EmbeddingIndexing |
| TagManagement | タグの制約検証、全置換更新を行う | Domain, HistoryTracking |
| HistoryTracking | 更新履歴を追記、取得する | Domain |
| GraphProjection | Gold / Silver データから関係グラフを組み立てる | Domain |
| EmbeddingIndexing | Embedding 生成と意味索引を扱う | Domain |
| Infrastructure.Db | DB Adapter、DbContext、Repository 実装 | Domain |
| Infrastructure.Llm | LLM Adapter、MAF / Foundry 連携 | Domain |
| Infrastructure.Embedding | Embedding Adapter、No-op 実装 | Domain |
| Persistence | Infrastructure.Db 内の EF Core 実装詳細 | なし |

## 依存関係の方針
- Api は DB や Agent Framework を直接参照せず、Application 層経由で操作する
- Domain は Port 契約のみを持ち、Infrastructure 実装を知らない
- Application は UseCase 単位で Port を受け取り、Adapter へ依存しない
- Query、HistoryTracking、GraphProjection も DbContext を直接参照せず、Port 契約越しにデータへアクセスする
- Bronze / Silver / Gold は層をまたいで直接更新しない
  - BronzeIngestion は Bronze のみ作成する
  - SilverNormalization は Bronze を読み、Silver を作る
  - GoldCuration は Silver を読み、Gold を作る
- AgentOrganization は直接 DB を好き勝手に更新せず、SilverNormalization と GoldCuration の業務ルールを経由する
- RuleBasedOrganization と AgentOrganization は同じ入出力契約を持ち、設定で差し替え可能にする
- EmbeddingIndexing は検索の補助モジュールであり、検索主系統の成立条件にしてはならない
- HistoryTracking は Gold 更新系処理の横断関心として扱う
- GraphProjection は検索や推薦の補助情報を作るが、MVP の正規データソースにはしない
- Persistence は Infrastructure.Db の内部詳細であり、Application / Query の依存先にしない

## モジュール関係図
```mermaid
flowchart LR
    subgraph Clients[Clients]
        WebUi[Web UI]
        Cli[CLI]
        McpClient[MCP Client]
    end

    subgraph Interface[Interface Layer]
        ApiSurface[HTTP API / MCP Tools]
    end

    subgraph App[Application Layer]
        UseCases[Use Cases]
        SearchQueryNode[Search Query]
        BronzeNode[Bronze Ingestion]
        SilverNode[Silver Normalization]
        GoldNode[Gold Curation]
        RuleOrg[Rule-based Organization]
        AgentOrg[LLM Organization]
        HistoryNode[History Tracking]
        GraphNode[Graph Projection]
        EmbNode[Embedding Indexing]
    end

    subgraph DomainLayer[Domain Layer]
        Entities[Entities / Value Objects]
        Ports[Ports]
    end

    subgraph Infra[Infrastructure Adapters]
        DbAdapter[DB Adapter]
        LlmAdapter[LLM Adapter]
        EmbeddingAdapter[Embedding Adapter]
        NoOpEmbedding[No-op Embedding Adapter]
    end

    subgraph External[External Systems]
        Sqlite[(SQLite)]
        Postgres[(PostgreSQL)]
        Foundry[Microsoft Foundry]
        AzureOpenAI[Azure OpenAI]
        OpenAICompat[OpenAI Compatible]
        Emb3Small[text-embedding-3-small]
        Emb3Large[text-embedding-3-large]
    end

    WebUi --> ApiSurface
    Cli --> ApiSurface
    McpClient --> ApiSurface
    ApiSurface --> UseCases
    ApiSurface --> SearchQueryNode
    UseCases --> BronzeNode
    UseCases --> SilverNode
    UseCases --> GoldNode
    UseCases --> RuleOrg
    UseCases --> AgentOrg
    UseCases --> HistoryNode
    SearchQueryNode --> GraphNode
    SearchQueryNode --> EmbNode
    BronzeNode --> Ports
    SilverNode --> Ports
    GoldNode --> Ports
    RuleOrg --> Ports
    AgentOrg --> Ports
    SearchQueryNode --> Ports
    GraphNode --> Ports
    EmbNode --> Ports
    Ports --> Entities
    DbAdapter -.implements.-> Ports
    LlmAdapter -.implements.-> Ports
    EmbeddingAdapter -.implements.-> Ports
    NoOpEmbedding -.implements.-> Ports
    DbAdapter --> Sqlite
    DbAdapter --> Postgres
    LlmAdapter --> Foundry
    LlmAdapter --> AzureOpenAI
    LlmAdapter --> OpenAICompat
    EmbeddingAdapter --> Emb3Small
    EmbeddingAdapter --> Emb3Large
```

## Port 契約一覧
| Port | 役割 | 実装候補 |
|:--|:--|:--|
| `IKnowledgeRepository` | Bronze / Silver / Gold の永続化 | EF Core + SQLite, EF Core + PostgreSQL |
| `IHistoryRepository` | 履歴の記録と取得 | EF Core 実装 |
| `IUnitOfWork` | トランザクション境界の制御 | EF Core 実装 |
| `IOrganizationAgent` | LLM を使った整理 | MAF + Foundry, Azure OpenAI Adapter |
| `IEmbeddingGenerator` | ベクトル生成 | No-op, Azure Embedding, OSS Embedding |
| `IRelationProjector` | 関連エントリ投影 | QuikGraph, 将来の graph backend |

## 主な責務境界
### BronzeIngestion
- 生入力の受け取り
- 入力ソース種別の記録
- 元テキストの完全保持
- 重複検知用のハッシュ計算

### SilverNormalization
- 名前、概要、認証方式、ツール一覧、リンクの抽出
- ツール単位の下書き生成
- 不完全データの `unknown` 補完

### GoldCuration
- 公開用表示モデルの作成
- タグ制約の検証
- 検索しやすい構造への整形
- 変更時の履歴記録

### AgentOrganization
- LLM が有効な場合のみ起動する
- 構造化出力でタグ提案、要約、関係抽出を返す
- 生成結果は即時反映ではなく、既存の業務ルールで再検証する

### EmbeddingIndexing
- Embedding が有効な場合のみ索引更新を行う
- 無効時は No-op 実装で置き換える
- ベクトル検索を追加しても、既存の構造化検索 API 契約を変えない

### SearchQuery
- キーワード検索を実行する
- 詳細検索用の複合条件を解釈する
- 検索候補サジェストを生成する
- ファセット集計を返す
- 関連エントリを返す

## クラス図
```mermaid
classDiagram
    class BronzeSource {
        +Guid Id
        +string SourceType
        +string RawContent
        +string SourceUri
        +string Status
        +DateTime ImportedAtUtc
    }

    class SilverServerDraft {
        +Guid Id
        +Guid BronzeSourceId
        +string Name
        +string Summary
        +string AuthenticationType
        +DateTime NormalizedAtUtc
    }

    class SilverToolDraft {
        +Guid Id
        +Guid ServerDraftId
        +string Name
        +string Description
    }

    class GoldCatalogEntry {
        +Guid Id
        +Guid SilverServerDraftId
        +string DisplayName
        +string Overview
        +string SetupGuide
        +DateTime PublishedAtUtc
    }

    class EntryTag {
        +Guid EntryId
        +string Value
    }

    class EntryHistory {
        +Guid Id
        +Guid EntryId
        +string Action
        +string ChangedBy
        +DateTime ChangedAtUtc
        +string Summary
        +bool UsedLlm
    }

    class BronzeIngestionService {
        +ImportAsync(request) BronzeSource
    }

    class SilverNormalizationService {
        +NormalizeAsync(bronzeId) SilverServerDraft
    }

    class GoldCurationService {
        +PublishAsync(silverId) GoldCatalogEntry
        +UpdateAsync(entryId, request) GoldCatalogEntry
    }

    class IOrganizationAgent {
        +OrganizeAsync(request) OrganizationResult
    }

    class RuleBasedOrganizationAgent {
        +OrganizeAsync(request) OrganizationResult
    }

    class MafFoundryOrganizationAgent {
        +OrganizeAsync(request) OrganizationResult
    }

    class SearchService {
        +SearchAsync(query) CatalogSearchResult
        +AdvancedSearchAsync(query) CatalogSearchResult
        +SuggestAsync(query, scope, limit) SearchSuggestionResult
        +GetFacetsAsync(query) SearchFacetResult
        +GetRelatedAsync(entryId, strategy, limit) RelatedCatalogEntry[]
        +GetDetailAsync(entryId) GoldCatalogEntry
    }

    class TagService {
        +ReplaceTagsAsync(entryId, tags) TagUpdateResult
    }

    class HistoryService {
        +AppendAsync(entryId, action) void
        +ListAsync(entryId, page, pageSize) EntryHistory[]
    }

    class GraphProjectionService {
        +BuildAsync(entryId) EntryGraph
    }

    class EmbeddingIndexService {
        +IndexAsync(entryId) void
        +SearchSimilarAsync(query) SimilaritySearchResult
    }

    class IKnowledgeRepository
    class IHistoryRepository
    class IUnitOfWork
    class IEmbeddingGenerator
    class IRelationProjector

    class McpKnowledgeDbContext {
        +DbSet~BronzeSource~ BronzeSources
        +DbSet~SilverServerDraft~ SilverServerDrafts
        +DbSet~SilverToolDraft~ SilverToolDrafts
        +DbSet~GoldCatalogEntry~ GoldCatalogEntries
        +DbSet~EntryTag~ EntryTags
        +DbSet~EntryHistory~ EntryHistories
    }

    BronzeSource --> SilverServerDraft
    SilverServerDraft --> SilverToolDraft
    SilverServerDraft --> GoldCatalogEntry
    GoldCatalogEntry --> EntryTag
    GoldCatalogEntry --> EntryHistory
    RuleBasedOrganizationAgent ..|> IOrganizationAgent
    MafFoundryOrganizationAgent ..|> IOrganizationAgent
    EmbeddingIndexService --> IEmbeddingGenerator
    GraphProjectionService --> IRelationProjector
    BronzeIngestionService --> IKnowledgeRepository
    SilverNormalizationService --> IKnowledgeRepository
    GoldCurationService --> IKnowledgeRepository
    HistoryService --> IHistoryRepository
    SearchService --> IKnowledgeRepository
    GraphProjectionService --> IKnowledgeRepository
    GoldCurationService --> TagService
    GoldCurationService --> HistoryService
```
