# API仕様書

## API概要
本 API は、MCP サーバーに関する情報を Bronze / Silver / Gold の 3 層で管理する。
HTTP API として公開しつつ、将来的に MCP サーバーのツールとして 1 対 1 で公開できる粒度で設計する。

- Bronze: 生データの取り込みと保管
- Silver: 正規化された下書きデータの生成
- Gold: 公開用カタログデータの検索、参照、更新
- Agent: LLM を使った自動整理の実行またはプレビュー

本 API は実装上、DB、Embedding、LLM をそれぞれ Port 経由で切り替え可能な前提で設計する。
MVP ではカテゴリ絞り込みは独立パラメータを持たず、`tag` / `tags[]` を使って表現する。

## データ層と状態遷移
| 層 | 主なリソース | 役割 | 次状態 |
|:--|:--|:--|:--|
| Bronze | `BronzeSource` | 生の入力データを失わず保持する | Silver へ正規化 |
| Silver | `SilverServerDraft` | MCP サーバー情報を構造化した下書き | Gold へ公開 |
| Gold | `GoldCatalogEntry` | 検索・参照対象の公開データ | 更新・履歴追記 |

## MCP ツール対応方針
| 想定ツール名 | HTTP エンドポイント | 用途 |
|:--|:--|:--|
| `intake_bronze_source` | `POST /api/bronze/sources` | 生データを投入する |
| `organize_source` | `POST /api/bronze/sources/{bronzeId}:organize` | Bronze から Silver / Gold へ整理する |
| `list_catalog_entries` | `GET /api/gold/catalog` | 公開カタログを一覧取得する |
| `get_catalog_entry` | `GET /api/gold/catalog/{entryId}` | 詳細情報を取得する |
| `search_catalog` | `GET /api/search` | 条件検索を行う |
| `search_catalog_advanced` | `POST /api/search/query` | 複雑な検索条件で検索する |
| `suggest_search_terms` | `GET /api/search/suggestions` | 入力補完候補を取得する |
| `get_search_facets` | `GET /api/search/facets` | 絞り込み候補と件数を取得する |
| `get_related_entries` | `GET /api/gold/catalog/{entryId}/related` | 類似・関連エントリを取得する |
| `get_system_capabilities` | `GET /api/system/capabilities` | 現在有効な機能と Adapter 種別を取得する |
| `update_catalog_tags` | `PUT /api/gold/catalog/{entryId}/tags` | タグを更新する |
| `get_entry_history` | `GET /api/gold/catalog/{entryId}/history` | 更新履歴を取得する |

## エンドポイント一覧
| メソッド | パス | 概要 | 主な入力 | 主な出力 |
|:--|:--|:--|:--|:--|
| POST | `/api/bronze/sources` | 生データを Bronze 層へ登録する | `sourceType`, `sourceUri`, `rawContent`, `importedBy` | `BronzeSource` |
| GET | `/api/bronze/sources` | Bronze データ一覧を取得する | `page`, `pageSize`, `status` | `BronzeSource[]` |
| GET | `/api/bronze/sources/{bronzeId}` | Bronze データ詳細を取得する | `bronzeId` | `BronzeSourceDetail` |
| POST | `/api/bronze/sources/{bronzeId}:organize` | Bronze から Silver / Gold へ整理する | `mode`, `targetLayer`, `useLlm`, `reasoningMode` | `OrganizationResult` |
| GET | `/api/silver/server-drafts` | Silver 下書き一覧を取得する | `page`, `pageSize`, `bronzeId` | `SilverServerDraft[]` |
| GET | `/api/silver/server-drafts/{silverId}` | Silver 下書き詳細を取得する | `silverId` | `SilverServerDraftDetail` |
| POST | `/api/silver/server-drafts/{silverId}:publish` | Silver 下書きを Gold に公開する | `silverId`, `publishedBy` | `GoldCatalogEntry` |
| GET | `/api/gold/catalog` | Gold カタログ一覧を取得する | `page`, `pageSize`, `tag`, `authType`, `client` | `GoldCatalogEntrySummary[]` |
| GET | `/api/gold/catalog/{entryId}` | Gold カタログ詳細を取得する | `entryId` | `GoldCatalogEntryDetail` |
| PUT | `/api/gold/catalog/{entryId}` | Gold カタログ本体を更新する | `overview`, `setupGuide`, `references`, `supportedClients` | `GoldCatalogEntryDetail` |
| PUT | `/api/gold/catalog/{entryId}/tags` | タグを全置換で更新する | `tags[]` | `TagUpdateResult` |
| GET | `/api/gold/catalog/{entryId}/related` | 類似または関連する Gold エントリを取得する | `entryId`, `limit`, `strategy` | `RelatedCatalogEntry[]` |
| GET | `/api/system/capabilities` | 有効な LLM / Embedding / Search capabilities を返す | なし | `SystemCapabilities` |
| GET | `/api/gold/catalog/{entryId}/history` | 更新履歴を取得する | `entryId`, `page`, `pageSize` | `EntryHistoryPage` |
| GET | `/api/search` | キーワード、タグ、認証条件、クライアント条件で検索する | `q`, `tags[]`, `authType`, `client`, `page`, `pageSize` | `CatalogSearchResult` |
| POST | `/api/search/query` | 複雑な条件をリクエストボディで検索する | `CatalogSearchQuery` | `CatalogSearchResult` |
| GET | `/api/search/suggestions` | 検索キーワード候補を返す | `q`, `limit`, `scope` | `SearchSuggestionResult` |
| GET | `/api/search/facets` | 絞り込み候補と件数を返す | `q`, `tags[]`, `authType`, `client` | `SearchFacetResult` |

## 主要エンドポイント詳細
### POST `/api/bronze/sources`
Bronze 層に生データを取り込む。

#### リクエスト
```json
{
	"sourceType": "manual",
	"sourceUri": "https://github.com/example/mcp-server",
	"rawContent": "# Example MCP Server\n...",
	"importedBy": "yuki"
}
```

#### バリデーション
- `sourceType` は `manual`, `github-readme`, `docs-url`, `json-import` のいずれか
- `rawContent` は必須
- 同一 `sourceUri` と同一内容を完全重複登録する場合は重複扱いにできる

#### レスポンス
```json
{
	"id": "9d4329cf-6cc4-4fea-9f0e-7bf1487680d9",
	"sourceType": "manual",
	"sourceUri": "https://github.com/example/mcp-server",
	"status": "imported",
	"importedAtUtc": "2026-05-17T09:10:00Z"
}
```

### POST `/api/bronze/sources/{bronzeId}:organize`
Bronze データをもとに Silver または Gold まで整理する。

#### リクエスト
```json
{
	"mode": "commit",
	"targetLayer": "gold",
	"useLlm": true,
	"reasoningMode": "balanced"
}
```

#### 挙動
- `useLlm=false` の場合は規則ベース整理を行う
- `useLlm=true` でも LLM が未設定または一時的に利用不可な場合は規則ベース整理へフォールバックし、レスポンスの `usedLlm` は `false` にする
- `mode=preview` の場合は永続化せず提案内容のみ返す
- `targetLayer=silver` の場合は Silver 下書きで停止する

#### レスポンス
```json
{
	"bronzeSourceId": "9d4329cf-6cc4-4fea-9f0e-7bf1487680d9",
	"usedLlm": true,
	"targetLayer": "gold",
	"silverDraftId": "4b58cb9c-5473-4300-aaf3-c4aa1c1f7792",
	"goldEntryId": "65dbcc38-2f26-4c99-8a8e-5e3cedaf01a3",
	"proposedTags": ["registry", "search", "github"],
	"changeSummary": "README を解析し、公開用エントリを生成しました。"
}
```

### PUT `/api/gold/catalog/{entryId}`
Gold エントリの本文情報を更新する。
タグはこの API では更新せず、専用のタグ更新 API で扱う。

#### リクエスト
```json
{
	"overview": "GitHub 上の MCP サーバーを検索するためのサーバーです。",
	"setupGuide": "1. package を install する\n2. config を設定する",
	"references": [
		"https://github.com/example/mcp-server",
		"https://example.dev/docs"
	],
	"supportedClients": ["VS Code", "Claude Desktop"]
}
```

#### バリデーション
- `overview` は必須
- `setupGuide` は必須
- `references` は 1 件以上を推奨し、空文字を含めない
- `supportedClients` は重複を含めない

#### 挙動
- 更新時は `updatedAtUtc` を更新する
- 変更内容は履歴に `catalog-updated` として記録する
- タグ更新は行わない

#### レスポンス
```json
{
	"id": "65dbcc38-2f26-4c99-8a8e-5e3cedaf01a3",
	"displayName": "GitHub MCP Registry",
	"overview": "GitHub 上の MCP サーバーを検索するためのサーバーです。",
	"tags": ["search", "github", "registry"],
	"setupGuide": "1. package を install する\n2. config を設定する",
	"references": [
		"https://github.com/example/mcp-server",
		"https://example.dev/docs"
	],
	"supportedClients": ["VS Code", "Claude Desktop"],
	"historyCount": 3
}
```

### PUT `/api/gold/catalog/{entryId}/tags`
Gold エントリのタグを更新する。タグは全置換とする。

#### リクエスト
```json
{
	"tags": ["search", "github", "registry"]
}
```

#### バリデーション
- タグ件数は 1 件以上 5 件以下
- 同一タグの重複は禁止
- 空文字は禁止

#### レスポンス
```json
{
	"entryId": "65dbcc38-2f26-4c99-8a8e-5e3cedaf01a3",
	"tags": ["search", "github", "registry"],
	"updatedAtUtc": "2026-05-17T09:20:00Z"
}
```

### GET `/api/gold/catalog/{entryId}/history`
Gold エントリの更新履歴を取得する。

#### レスポンス
```json
{
	"items": [
		{
			"id": "a4a7c589-64fd-488f-8bc8-c2c3e4b64d9d",
			"action": "tags-replaced",
			"changedBy": "yuki",
			"changedAtUtc": "2026-05-17T09:20:00Z",
			"summary": "タグを 3 件に更新しました。"
		}
	],
	"page": 1,
	"pageSize": 20,
	"totalCount": 1
}
```

### GET `/api/search`
公開カタログに対して検索を行う。

#### クエリパラメータ
- `q`: キーワード。名前、概要、ツール名を検索対象に含む
- `tags`: タグの AND 条件。MVP ではカテゴリ絞り込みもこの項目で表現する
- `authType`: `none`, `api-key`, `oauth`, `unknown`
- `client`: 対応クライアント名
- `sort`: `relevance`, `updated-desc`, `name-asc`
- `includeDrafts`: 管理用検索時のみ。既定は `false`
- `page`, `pageSize`: ページング

#### 用途
- 一般的な検索一覧画面
- MCP ツールからの単発検索
- CLI や UI からの簡易検索

### POST `/api/search/query`
複雑な条件をリクエストボディで受け取る高度検索 API。
GET のクエリ文字列で表現しづらい条件を扱う。

#### リクエスト
```json
{
	"keyword": "github registry",
	"tags": ["github", "registry"],
	"authTypes": ["none", "oauth"],
	"supportedClients": ["Claude Desktop", "VS Code"],
	"toolNames": ["search", "list"],
	"updatedAfterUtc": "2026-01-01T00:00:00Z",
	"sort": "relevance",
	"page": 1,
	"pageSize": 20
}
```

#### 用途
- UI の詳細検索フォーム
- 将来的な MCP クライアント向けの複合条件検索
- 監査や棚卸しのための絞り込み

### GET `/api/search/suggestions`
検索入力中の補完候補を返す。

#### クエリパラメータ
- `q`: 入力途中の文字列
- `limit`: 最大件数。既定は 10
- `scope`: `all`, `name`, `tag`, `tool`, `client`

#### レスポンス
```json
{
	"items": [
		{
			"value": "github",
			"type": "tag",
			"score": 0.98
		},
		{
			"value": "GitHub MCP Registry",
			"type": "name",
			"score": 0.92
		}
	]
}
```

### GET `/api/search/facets`
現在の検索条件に対して、さらに絞り込める候補と件数を返す。

#### クエリパラメータ
- `q`: キーワード
- `tags`: 既に適用中のタグ
- `authType`: 既に適用中の認証条件
- `client`: 既に適用中のクライアント条件

#### レスポンス
```json
{
	"tags": [
		{ "value": "github", "count": 12 },
		{ "value": "search", "count": 8 }
	],
	"authTypes": [
		{ "value": "none", "count": 10 },
		{ "value": "oauth", "count": 4 }
	],
	"clients": [
		{ "value": "VS Code", "count": 7 },
		{ "value": "Claude Desktop", "count": 5 }
	]
}
```

### GET `/api/gold/catalog/{entryId}/related`
指定エントリと関連度の高いエントリを返す。
関連度は共有タグ、共有クライアント、共有ツール、GraphProjection の近傍情報などから算出する。

#### クエリパラメータ
- `limit`: 最大件数。既定は 5
- `strategy`: `hybrid`, `tag-overlap`, `tool-overlap`, `graph-neighbors`

#### レスポンス
```json
[
	{
		"entryId": "f3721ae3-4e3a-47ea-b3c0-3319a865f8dc",
		"displayName": "GitHub Search MCP",
		"score": 0.83,
		"reasons": ["shared-tag:github", "shared-tool:search"]
	}
]
```

### GET `/api/system/capabilities`
実行環境で有効な機能と Adapter 種別を返す。
クライアントはこの API を使って、LLM 整理や Embedding 類似検索が有効かを判定できる。

#### レスポンス
```json
{
	"knowledgeStore": {
		"provider": "sqlite",
		"replaceable": true
	},
	"llm": {
		"enabled": true,
		"provider": "foundry",
		"model": "configured-at-runtime",
		"replaceable": true
	},
	"embedding": {
		"enabled": false,
		"provider": "none",
		"replaceable": true
	},
	"search": {
		"supportsStructuredSearch": true,
		"supportsSuggestions": true,
		"supportsFacets": true,
		"supportsRelatedEntries": true,
		"supportsSemanticSearch": false
	}
}
```

## DTO 仕様
### BronzeSource
| 項目 | 型 | 説明 |
|:--|:--|:--|
| `id` | `Guid` | Bronze レコード識別子 |
| `sourceType` | `string` | 入力種別 |
| `sourceUri` | `string?` | 元ソース URL |
| `status` | `string` | `imported`, `organized`, `error` |
| `importedAtUtc` | `datetime` | 取り込み時刻 |

### SilverServerDraft
| 項目 | 型 | 説明 |
|:--|:--|:--|
| `id` | `Guid` | Silver 下書き識別子 |
| `bronzeSourceId` | `Guid` | 元 Bronze レコード |
| `name` | `string` | 正規化されたサーバー名 |
| `summary` | `string` | 要約 |
| `authenticationType` | `string` | 認証方式 |
| `toolDrafts` | `array` | ツール下書き一覧 |

### GoldCatalogEntryDetail
| 項目 | 型 | 説明 |
|:--|:--|:--|
| `id` | `Guid` | Gold エントリ識別子 |
| `displayName` | `string` | 表示名 |
| `overview` | `string` | 概要 |
| `tags` | `string[]` | 1 件以上 5 件以下 |
| `setupGuide` | `string` | セットアップ手順 |
| `toolSummaries` | `array` | ツール概要 |
| `references` | `array` | 参照リンク |
| `supportedClients` | `array` | 対応クライアント |
| `historyCount` | `int` | 履歴件数 |

### CatalogSearchQuery
| 項目 | 型 | 説明 |
|:--|:--|:--|
| `keyword` | `string?` | 検索キーワード |
| `tags` | `string[]?` | AND 条件のタグ |
| `authTypes` | `string[]?` | 許容する認証方式 |
| `supportedClients` | `string[]?` | 対応クライアント条件 |
| `toolNames` | `string[]?` | ツール名条件 |
| `updatedAfterUtc` | `datetime?` | 更新日時の下限 |
| `sort` | `string?` | 並び順 |
| `page` | `int` | ページ番号 |
| `pageSize` | `int` | 1 ページ件数 |

### SearchSuggestionResult
| 項目 | 型 | 説明 |
|:--|:--|:--|
| `items[].value` | `string` | 候補文字列 |
| `items[].type` | `string` | `name`, `tag`, `tool`, `client` |
| `items[].score` | `decimal` | 候補スコア |

### SearchFacetResult
| 項目 | 型 | 説明 |
|:--|:--|:--|
| `tags[]` | `FacetItem[]` | タグ候補と件数 |
| `authTypes[]` | `FacetItem[]` | 認証方式候補と件数 |
| `clients[]` | `FacetItem[]` | クライアント候補と件数 |

### RelatedCatalogEntry
| 項目 | 型 | 説明 |
|:--|:--|:--|
| `entryId` | `Guid` | 関連エントリ識別子 |
| `displayName` | `string` | 表示名 |
| `score` | `decimal` | 関連度スコア |
| `reasons` | `string[]` | 関連理由 |

### EntryHistoryPage
| 項目 | 型 | 説明 |
|:--|:--|:--|
| `items` | `EntryHistory[]` | 履歴一覧 |
| `page` | `int` | 現在ページ |
| `pageSize` | `int` | ページサイズ |
| `totalCount` | `int` | 総件数 |

### SystemCapabilities
| 項目 | 型 | 説明 |
|:--|:--|:--|
| `knowledgeStore.provider` | `string` | 現在の DB provider |
| `knowledgeStore.replaceable` | `bool` | 差し替え可能か |
| `llm.enabled` | `bool` | LLM 整理が有効か |
| `llm.provider` | `string` | LLM provider 名 |
| `llm.model` | `string` | 使用モデル名または runtime configured |
| `embedding.enabled` | `bool` | Embedding が有効か |
| `embedding.provider` | `string` | Embedding provider 名 |
| `search.supportsSemanticSearch` | `bool` | Embedding ベース検索が有効か |

## 共通仕様
### 認証方式
- MVP ではローカル利用または信頼済み環境を前提とし、認証なしで開始する
- 公開環境では API キーまたは Bearer Token を後付け可能な構成にする

### エラーレスポンスの形式
`application/problem+json` に準拠する。

```json
{
	"type": "https://example.dev/problems/tag-validation-error",
	"title": "Tag validation failed",
	"status": 400,
	"detail": "tags must contain between 1 and 5 unique values.",
	"traceId": "00-..."
}
```

### ページネーションの方式
- `page` は 1 始まり
- `pageSize` の既定値は 20
- `pageSize` の最大値は 100

### 日時と識別子の形式
- 識別子は `Guid` を使用する
- 日時は UTC の ISO 8601 形式で返す

### 更新履歴の記録ルール
- Gold エントリの作成、更新、タグ変更時に履歴を追加する
- Silver から Gold への publish 時も履歴を追加する
- エージェントによる自動整理では `usedLlm=true/false` を履歴に残せるようにする
