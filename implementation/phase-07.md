# Phase 07: レイヤ分離の導入

## 目的
ここまでに動いている機能を壊さず、後続の差し替えに耐える構造へ整理する。

## 最小で動く到達点
既存 API の振る舞いを維持したまま、Domain / Application / Infrastructure の責務が分離される。

## 実装対象
- Domain モデルへのルール移動
- Application サービスへのユースケース集約
- Infrastructure への EF Core 実装隔離
- API DTO と Domain モデルの分離

## 具体的なタスク
1. タグ制約、公開ルール、履歴生成ルールを Domain 側へ寄せる
2. organize、publish、tag update を Application サービスへまとめる
3. API 層から EF Core 直接参照を減らす
4. DTO と永続化モデルの責務を見直す
5. 既存テストを壊さずにリファクタリングする

## 必須ユニットテスト
- Domain のタグ制約が API から独立して検証できること
- Application サービスが期待する結果を返すこと
- リファクタ後も主要ユースケースの既存テストが通ること

## 確認コマンド
- `dotnet build`
- `dotnet test`

## 突破基準
- 振る舞いの変更なしにレイヤが整理される
- ドメインルールがテストしやすくなる
- 次フェーズの Port 導入に進める構造になる

## スコープ外
- DB Adapter の差し替え
- LLM / Embedding 抽象

## 備考
このフェーズは機能追加よりも整理が目的だが、必ず動作維持を前提とする。