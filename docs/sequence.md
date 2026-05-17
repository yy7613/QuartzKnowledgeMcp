# シーケンス図

## Bronze 取り込みから Gold 公開まで
```mermaid
sequenceDiagram
    actor User
    participant API
    participant BronzeService as BronzeIngestionService
    participant Organizer as OrganizationService
    participant RuleAgent as RuleBasedOrganizationAgent
    participant LlmAgent as MafFoundryOrganizationAgent
    participant SilverService as SilverNormalizationService
    participant GoldService as GoldCurationService
    participant HistoryService
    participant DB as McpKnowledgeDbContext

    User->>API: POST /api/bronze/sources
    API->>BronzeService: ImportAsync(request)
    BronzeService->>DB: BronzeSource を保存
    DB-->>BronzeService: BronzeSource
    BronzeService-->>API: BronzeSource
    API-->>User: 201 Created

    User->>API: POST /api/bronze/sources/{id}:organize
    API->>Organizer: OrganizeAsync(bronzeId, useLlm, targetLayer)

    alt LLM 有効かつ設定済み
        Organizer->>LlmAgent: OrganizeAsync(request)
        LlmAgent-->>Organizer: 要約、タグ案、構造化結果
    else LLM 無効または未設定
        Organizer->>RuleAgent: OrganizeAsync(request)
        RuleAgent-->>Organizer: 規則ベースの整理結果
    end

    Organizer->>SilverService: NormalizeAsync(bronzeId, organizationResult)
    SilverService->>DB: SilverServerDraft を保存
    DB-->>SilverService: SilverServerDraft

    alt targetLayer = gold
        Organizer->>GoldService: PublishAsync(silverId)
        GoldService->>DB: GoldCatalogEntry と Tag を保存
        GoldService->>HistoryService: AppendAsync(entryId, "published")
        HistoryService->>DB: EntryHistory を保存
        DB-->>GoldService: GoldCatalogEntry
        GoldService-->>API: GoldCatalogEntry
    else targetLayer = silver
        SilverService-->>API: SilverServerDraft
    end

    API-->>User: OrganizationResult
```

## 公開カタログの検索と履歴参照
```mermaid
sequenceDiagram
    actor User
    participant API
    participant SearchService
    participant TagService
    participant HistoryService
    participant DB as McpKnowledgeDbContext

    User->>API: GET /api/search?q=registry&tags=github
    API->>SearchService: SearchAsync(query)
    SearchService->>DB: GoldCatalogEntry を検索
    DB-->>SearchService: 検索結果
    SearchService-->>API: CatalogSearchResult
    API-->>User: 200 OK

    User->>API: PUT /api/gold/catalog/{id}/tags
    API->>TagService: ReplaceTagsAsync(entryId, tags)
    TagService->>DB: Tag を全置換
    TagService->>HistoryService: AppendAsync(entryId, "tags-replaced")
    HistoryService->>DB: EntryHistory を保存
    API-->>User: 200 OK

    User->>API: GET /api/gold/catalog/{id}/history
    API->>HistoryService: ListAsync(entryId, page, pageSize)
    HistoryService->>DB: 履歴を取得
    DB-->>HistoryService: EntryHistoryPage
    HistoryService-->>API: EntryHistoryPage
    API-->>User: 200 OK
```

## LLM と Embedding が有効な場合の整理フロー
```mermaid
sequenceDiagram
    actor Maintainer
    participant API
    participant Organizer as AgentOrganization
    participant LlmAdapter as LLM Adapter
    participant SilverService as SilverNormalizationService
    participant GoldService as GoldCurationService
    participant EmbIndex as EmbeddingIndexService
    participant HistoryService
    participant DB as Knowledge Store
    participant LLM as Replaceable LLM
    participant EMB as Replaceable Embedding Model

    Maintainer->>API: POST /api/bronze/sources/{id}:organize useLlm=true
    API->>Organizer: OrganizeAsync(bronzeId)
    Organizer->>LlmAdapter: Summarize / Tag / Relate
    LlmAdapter->>LLM: 構造化出力要求
    LLM-->>LlmAdapter: JSON 結果
    LlmAdapter-->>Organizer: OrganizationResult
    Organizer->>SilverService: NormalizeAsync(...)
    SilverService->>DB: Silver 保存
    Organizer->>GoldService: PublishAsync(...)
    GoldService->>DB: Gold 保存

    alt Embedding enabled
        GoldService->>EmbIndex: IndexAsync(entryId)
        EmbIndex->>EMB: ベクトル生成
        EMB-->>EmbIndex: Embedding vector
        EmbIndex->>DB: 索引保存
    else Embedding disabled
        GoldService-->>Organizer: No-op
    end

    GoldService->>HistoryService: AppendAsync(entryId, "organized-with-llm")
    HistoryService->>DB: 履歴保存
    API-->>Maintainer: 200 OK
```
