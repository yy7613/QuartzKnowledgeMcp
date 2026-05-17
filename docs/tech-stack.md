# 技術スタック

## 採用技術
| 種別 | 採用 | バージョン | 採用理由 |
|:--|:--|:--|:--|
| 言語 | C# | .NET 10 | EF Core と ASP.NET Core、Microsoft Agent Framework を同一スタックで扱いやすい |
| Web API | ASP.NET Core Minimal API | .NET 10 系 | 初期フェーズで小さく始めやすく、HTTP API と MCP ツール対応の境界を作りやすい |
| アプリケーションフレームワーク | Microsoft Agent Framework | 1.6 系 preview | LLM をオプション機能として追加しやすく、将来的なエージェント拡張に備えられる |
| 永続化 | EF Core | 10.0 系 | DB provider を差し替え可能にしやすい |
| 開発用 DB | SQLite | デフォルト採用 | ローカル開発、OSS 配布、初期セットアップが軽い |
| 検索補助 | QuikGraph | 採用候補 | 関連エントリやグラフ投影をアプリケーション層で扱いやすい |
| テストライブラリ | xUnit | 2.9 系 | .NET 標準的で最小構成を組みやすい |
| ロガー | Microsoft.Extensions.Logging | .NET 標準 | ASP.NET Core と Agent Framework の両方に自然に統合できる |
| Azure 認証 | Azure.Identity | 1.21 系 | Foundry を使う場合の認証基盤として標準的 |
| Foundry 接続 | Azure.AI.Projects | 2.0 系 | Foundry / AI Project 接続を明示的に扱える |

## 差し替えポイント
| 領域 | 契約 | 既定実装 | 代替候補 | 差し替え条件 |
|:--|:--|:--|:--|:--|
| DB | `IKnowledgeRepository`, `IHistoryRepository`, `IUnitOfWork` | EF Core + SQLite | EF Core + PostgreSQL, EF Core + SQL Server | Repository 契約と Migration 方針が維持できること |
| LLM | `IOrganizationAgent` | Microsoft Agent Framework + Foundry | Azure OpenAI, OpenAI 互換, ダミー実装 | 構造化出力契約を満たすこと |
| Embedding | `IEmbeddingGenerator`, `ISemanticIndexer` | 無効化または No-op | `text-embedding-3-small`, `text-embedding-3-large`, OSS Embedding | ベクトル生成と索引登録契約を満たすこと |
| グラフ投影 | `IRelationProjector` | QuikGraph ベース投影 | カスタム投影器, 将来の graph backend | Gold / Silver を正規ソースとすること |

## 推奨実装方針
- ドメイン層は `Port` インターフェースのみを参照し、具象 SDK を知らない構成にする
- Infrastructure 層で DB / LLM / Embedding の Adapter を提供する
- Embedding は feature flag または設定で有効化し、無効時は No-op Adapter を使う
- LLM は同期的な必須依存にせず、整理機能のオプションとして扱う
- 検索の主系統は構造化検索とし、Embedding 検索は拡張経路として扱う
- Adapter 境界を超える DTO は SDK 固有型を露出しない

## 採用方針メモ
- LLM 機能はオプションとし、設定が無い場合でもアプリケーションの主要機能は動作することを優先する
- モデル名はコードに固定せず設定値で受け取る
- `Microsoft.Agents.AI` 系は現時点で prerelease を前提に扱う
- Embedding は今は採用を固定せず、後から `text-embedding-3-small` または `text-embedding-3-large` を追加できるよう分離する
- DB は SQLite を既定とするが、PostgreSQL など他 provider へ差し替え可能な構成を維持する

## MCP 実装参照
- MCP 実装は Microsoft Agent Framework の .NET サンプル `ModelContextProtocol` を第一参照にする
- 基本的な MCP server tools の実装は `Agent_MCP_Server` を参照する
- 保護された MCP server の認可付き実装は `Agent_MCP_Server_Auth` を参照する
- Hosted MCP パターンは `ResponseAgent_Hosted_MCP` を参照する
- サンプルの前提である .NET 10、Azure OpenAI 環境変数、`az login` による認証フローは、MCP 実装時の参考手順として扱う
- 本リポジトリでは model / provider 固定は行わず、MCP tool の接続方法とホスト構成の実装パターンのみを参照する

## 却下した選択肢
- Python 単独実装
	- Microsoft Agent Framework 自体は使えるが、EF Core を中心にした永続化設計と一貫しない
- 専用グラフ DB を MVP から採用
	- 要件に対して初期コストが高く、Bronze / Silver / Gold の整理フローを固める前に複雑化しやすい
- Embedding を MVP 必須にする
	- まずは構造化検索、タグ、履歴、関連エントリで価値を出せるため優先度が低い
- 完全な CRUD コントローラー中心の構成
	- 検索、整理、公開、履歴といった操作中心の API なので、初期は Minimal API の方が適している
