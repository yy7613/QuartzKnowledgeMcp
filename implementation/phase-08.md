# Phase 08: DB 差し替え境界の導入

## 目的
永続化実装を差し替え可能にしつつ、既存機能の挙動を維持する。

## 最小で動く到達点
Application は Repository Port 越しに動作し、既定は SQLite Adapter で同じ機能が成立する。

## 実装対象
- `IKnowledgeRepository`
- `IHistoryRepository`
- `IUnitOfWork` または同等の永続化境界
- SQLite Adapter
- テスト用 Fake Repository

## 具体的なタスク
1. Application サービスが参照する Port を定義する
2. EF Core 実装を SQLite Adapter へ閉じ込める
3. DI で Repository 実装を差し替え可能にする
4. テスト用 Fake Repository で Application テストを書き直す
5. 既存 API の振る舞いが変わらないことを確認する

## 必須ユニットテスト
- Application サービスが Fake Repository で動作すること
- Repository Port 契約を満たさない実装で失敗が検出できること
- SQLite Adapter への切り替え後も主要ユースケースが通ること

## 確認コマンド
- `dotnet build`
- `dotnet test`

## 突破基準
- DB 実装を Port 越しに扱える
- SQLite が既定実装として動く
- DB 差し替えの足場ができる

## スコープ外
- LLM Port
- Embedding Port

## 備考
このフェーズで初めて DB 差し替え性を導入するが、機能面では新しい価値を増やしすぎない。