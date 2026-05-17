# Phase 03: Silver 正規化最小実装

## 目的
Bronze の生データを、公開前の正規化下書きへ変換できるようにする。

## 最小で動く到達点
Bronze から SilverServerDraft を規則ベースで生成し、保存と取得ができる。

## 実装対象
- `SilverServerDraft` と最小の ToolDraft モデル
- 規則ベース正規化サービス
- Silver 一覧 / 詳細 API
- Bronze から Silver への organize API

## 具体的なタスク
1. README 風テキストから名前、要約、タグ候補を抽出する最小ロジックを実装する
2. `SilverServerDraft` を保存する
3. `POST /api/bronze/sources/{bronzeId}:organize` に Silver 生成モードを追加する
4. `GET /api/silver/server-drafts*` を追加する
5. 変換失敗時のエラー応答を定義する

## 必須ユニットテスト
- 正規化ロジックが既知入力から名前と要約を抽出できること
- 不完全な入力でも最低限の下書きを返せること
- organize 実行時に Bronze が存在しない場合は失敗すること

## 確認コマンド
- `dotnet build`
- `dotnet test`

## 突破基準
- Bronze から Silver 下書きを生成できる
- Silver 下書きの一覧と詳細を取得できる
- LLM なしで deterministic に動く

## スコープ外
- Gold 公開
- 履歴管理
- LLM 整理

## 備考
正規化は将来差し替える前提だが、この時点では単純で再現性の高い規則ベースに固定する。