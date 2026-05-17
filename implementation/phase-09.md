# Phase 09: LLM 抽象と能力照会

## 目的
LLM 利用をアプリケーションの任意機能として扱える形に整える。

## 最小で動く到達点
Rule-based 整理が `IOrganizationAgent` 越しに動き、preview と `GET /api/system/capabilities` が使え、LLM 未設定時は rule-based へフォールバックできる。

## 実装対象
- `IOrganizationAgent`
- `RuleBasedOrganizationAgent`
- capabilities 解決サービス
- `GET /api/system/capabilities`
- organize preview モード

## 具体的なタスク
1. organize 処理の呼び出し境界を `IOrganizationAgent` に揃える
2. 既存の規則ベース整理を `RuleBasedOrganizationAgent` として実装する
3. LLM の有無、Embedding の有無、DB provider を返す capabilities API を追加する
4. preview モードでは永続化せず結果だけ返す
5. `useLlm=true` でも LLM が未設定または一時利用不可な場合は rule-based にフォールバックし、`usedLlm=false` を返す

## 必須ユニットテスト
- capabilities が設定状態を正しく返すこと
- preview モードで永続化が行われないこと
- LLM 未設定時または利用不可時に rule-based agent が選択されること

## 確認コマンド
- `dotnet build`
- `dotnet test`

## 突破基準
- LLM なしで従来フローが維持される
- capabilities API で実行時能力を確認できる
- LLM 利用要求時のフォールバック方針が固定される
- organize の呼び出し境界が抽象化される

## スコープ外
- 実 LLM 接続
- Embedding 実装

## 備考
ここでは本物の LLM はまだ繋がない。まずオプション機能としての枠だけを作る。