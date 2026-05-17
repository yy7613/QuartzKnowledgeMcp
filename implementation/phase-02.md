# Phase 02: Bronze 取り込み最小実装

## 目的
生データを失わず保存できる Bronze 層を先に成立させる。

## 最小で動く到達点
BronzeSource を SQLite に保存し、登録と取得が API 経由で動く。

## 実装対象
- `BronzeSource` エンティティ
- EF Core + SQLite の最小永続化
- Bronze 登録 API
- Bronze 一覧 / 詳細 API

## 具体的なタスク
1. `BronzeSource` の最小項目を定義する
2. DbContext と初回 Migration を作る
3. Bronze 登録サービスを実装する
4. `POST /api/bronze/sources` と `GET /api/bronze/sources*` を追加する
5. 重複判定は完全一致レベルの簡易判定に留める

## 必須ユニットテスト
- Bronze 登録時に必須項目が検証されること
- 同一内容の重複判定が期待通りに動くこと
- Bronze 詳細取得が識別子で成功 / 失敗を返し分けること

## 確認コマンド
- `dotnet build`
- `dotnet test`

## 突破基準
- Bronze の登録、一覧、詳細取得が動く
- SQLite で永続化できる
- 単純な重複抑止が機能する

## スコープ外
- Silver 変換
- Gold 公開
- 高度な入力ソース自動収集

## 備考
この段階では EF Core への直接依存を許容し、まず保存の成功体験を作る。