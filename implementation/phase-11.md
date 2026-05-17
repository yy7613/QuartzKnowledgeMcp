# Phase 11: Embedding 抽象と No-op 実装

## 目的
Embedding を使う将来拡張点を先に設けつつ、既定では未使用のまま安全に運用できるようにする。

## 最小で動く到達点
`IEmbeddingGenerator` と `ISemanticIndexer` が導入され、既定では No-op 実装で既存機能がそのまま動く。

## 実装対象
- `IEmbeddingGenerator`
- `ISemanticIndexer`
- No-op Embedding 実装
- publish 後の索引更新フック
- capabilities への Embedding 状態追加

## 具体的なタスク
1. Embedding と索引更新の Port を定義する
2. 既定実装として No-op Adapter を用意する
3. Gold publish や更新時に索引更新フックを差し込む
4. capabilities API に Embedding 状態を反映する
5. Embedding 未設定時は追加コストなしで終了する

## 必須ユニットテスト
- No-op Adapter が副作用なしで完了すること
- publish 後に索引更新フックが呼ばれること
- Embedding 無効時でも検索主機能が壊れないこと

## 確認コマンド
- `dotnet build`
- `dotnet test`

## 突破基準
- Embedding Port が導入される
- 既定構成は No-op で安定動作する
- Embedding の有効 / 無効が API から確認できる

## スコープ外
- ベクトル検索本実装
- 類似度ランキングの本格統合

## 備考
Embedding を必須化しないことがこのフェーズの最重要条件である。