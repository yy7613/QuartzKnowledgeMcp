# ADR-002: DB、Embedding、LLM を差し替え可能にする

## ステータス
採用

## 背景
OSS として公開する以上、特定クラウドや特定プロバイダーに閉じた構成は利用者を狭める。
また、Embedding や LLM はコスト、可用性、品質、契約条件によって変更されうる。

## 決定内容
- DB は Repository Port 越しに扱い、既定は SQLite、将来的な PostgreSQL などへの差し替えを許容する
- LLM は `IOrganizationAgent` 契約で扱い、既定は Microsoft Agent Framework + Foundry Adapter とする
- Embedding は `IEmbeddingGenerator` 契約で扱い、既定は disabled または No-op とする

## 検討した選択肢
- DB / LLM / Embedding を直接 SDK 呼び出しする
  - 実装は速いが、差し替えコストが高い
- DB のみ抽象化する
  - AI 依存の入れ替えに弱い
- 3 領域すべてを Port で抽象化する
  - 設計コストはかかるが、目的に合致する

## 影響・トレードオフ
- Port と Adapter の定義が必要になる
- 機能追加時に契約設計の検討が増える
- 一方で、運用環境ごとに構成を差し替えやすくなる