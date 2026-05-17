# Phase 12: 検索拡張と関係投影

## 目的
検索体験を仕様レベルまで広げ、構造化検索を中心に関連性機能を追加する。

## 最小で動く到達点
高度検索、検索補完、ファセット、related entries、関係投影が動き、Embedding が無効でも有用な検索結果を返せる。

## 実装対象
- `POST /api/search/query`
- `GET /api/search/suggestions`
- `GET /api/search/facets`
- `GET /api/gold/catalog/{entryId}/related`
- `IRelationProjector` と最小の関係スコアリング

## 具体的なタスク
1. 複合条件検索 DTO と Query サービスを追加する
2. タグ、名称、ツール名に基づく補完候補を返す
3. 現在条件に対するファセット件数を返す
4. 共有タグ、共有ツール、共有クライアントから related entries を算出する
5. Embedding 有効時のみ補助スコアを合算できる拡張点を用意する

## 必須ユニットテスト
- 高度検索が複数条件を正しく組み合わせること
- 補完候補が deterministic な順で返ること
- ファセット件数が検索条件に一致すること
- Embedding 無効時でも related entries が計算できること

## 確認コマンド
- `dotnet build`
- `dotnet test`

## 突破基準
- 仕様書で定義した検索 API 群が揃う
- 検索の主系統が構造化検索として成立する
- 関係投影が DB と LLM に過剰依存しない

## スコープ外
- 専用グラフ DB
- Embedding のみを主軸にした検索体験

## 備考
関連エントリはまず構造化情報だけで成立させ、Embedding は補助点数に留める。