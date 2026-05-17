# Phase 10: MAF / Foundry Adapter 導入

## 目的
Microsoft Agent Framework を使った整理を、設定時のみ有効になる Adapter として導入する。

## 最小で動く到達点
LLM 設定がある環境では MAF / Foundry Adapter を経由して organize でき、未設定環境ではアプリ全体がそのまま動く。

## 実装対象
- `MafFoundryOrganizationAgent`
- Foundry 設定オプション
- 構造化出力 DTO
- agent 選択ロジック
- LLM エラー時のフォールバック方針

## 具体的なタスク
1. MAF / Foundry を使う Adapter を実装する
2. モデル名やプロジェクト設定をコードに固定せず設定から受け取る
3. LLM 出力を構造化 DTO に写像する
4. 設定あり / なしで agent の切り替えを実装する
5. 接続失敗時は rule-based fallback か明示的失敗のどちらかを統一する
6. MCP host / tool wiring が必要になった場合は Microsoft Agent Framework `ModelContextProtocol` sample の該当パターンを参照して実装する

## 必須ユニットテスト
- LLM 出力 DTO への写像が崩れないこと
- 設定の有無で agent 選択が変わること
- LLM 失敗時の fallback 方針が期待通りに動くこと

## 確認コマンド
- `dotnet build`
- `dotnet test`

## 突破基準
- LLM 設定時のみ MAF Adapter が有効になる
- モデル選択が runtime configuration に従う
- LLM 未設定でもアプリ本体は壊れない

## スコープ外
- Embedding ベクトル生成
- 高度な評価パイプライン

## 備考
外部認証がない環境でも開発を止めないことを優先する。MCP 実装は `ModelContextProtocol` sample を第一参照とし、特に `Agent_MCP_Server` と `ResponseAgent_Hosted_MCP` の構成差を確認してから着手する。