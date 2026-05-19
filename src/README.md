# src

アプリケーションコードとテストはこのディレクトリに配置します。

正式な solution は `QuartzKnowledgeMcp.slnx` です。

## ダッシュボード

API を起動した後は `/dashboard` で人間向けの参照画面を開けます。

- `/api/dashboard/summary` : bronze / silver / gold の件数、鮮度、最近の更新、タグ一覧、7 日 trend、detail path
- `/api/dashboard/search` : medallion 横断の検索 API。`stage` / `tag` / `freshness` / `sort` を利用可能
- `/dashboard` : clickable tag filter、freshness filter、sort selector、detail link を持つ参照 UI
- Gold Inspector : gold result から detail / history / related を同一画面で確認可能
- Trend toggle : 3d / 7d を切り替えて stage trend の観測粒度を変更可能
- State persistence : query string と localStorage の両方で検索条件、trend、inspect 対象を復元可能
