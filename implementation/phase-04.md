# Phase 04: Gold 公開と履歴の基礎

## 目的
Silver 下書きを公開用の Gold エントリへ昇格させ、変更履歴を残す。

## 最小で動く到達点
Silver から Gold への publish が動き、初回公開と更新履歴が保存される。

## 実装対象
- `GoldCatalogEntry` エンティティ
- `EntryHistory` エンティティ
- publish サービス
- Gold 詳細取得 API

## 具体的なタスク
1. Silver から Gold へ必要項目を写像する
2. 初回 publish 時に履歴を 1 件記録する
3. `POST /api/silver/server-drafts/{silverId}:publish` を追加する
4. `GET /api/gold/catalog/{entryId}` を追加する
5. 更新時刻と更新者の最小管理を入れる

## 必須ユニットテスト
- publish 実行で Gold が生成されること
- publish 実行で履歴が追記されること
- 存在しない Silver を publish すると失敗すること

## 確認コマンド
- `dotnet build`
- `dotnet test`

## 突破基準
- Bronze -> Silver -> Gold の最短フローが動く
- 履歴が保存される
- Gold 詳細を取得できる

## スコープ外
- Gold 一覧検索
- タグ更新 API
- LLM 利用

## 備考
このフェーズで初めて業務上の主データが完成する。以後の検索は Gold を正とする。