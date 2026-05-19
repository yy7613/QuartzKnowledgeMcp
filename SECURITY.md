# Security Policy

## Supported versions
この repository のセキュリティ修正は個人メンテナンス前提のベストエフォートです。原則として `main` branch と最新タグを優先し、古いタグへのバックポートは保証しません。

## Reporting a vulnerability
脆弱性の詳細は公開 issue に書かないでください。GitHub の private vulnerability reporting が有効であれば、まずそちらを利用してください。

もし private vulnerability reporting が未設定の場合は、攻撃手順や秘密情報を公開せず、issue、Discussion、または GitHub profile の連絡先から非公開連絡手段の案内を依頼してください。

報告には次の情報を含めてください。
- 対象 endpoint または MCP tool
- 再現手順
- 影響範囲
- 想定する severity
- 可能なら修正案または緩和策

## Response goals
以下は SLA ではなく目安です。個人プロジェクトのため、状況に応じて前後します。

- 受領確認: できるだけ 7 日以内
- 初回トリアージ: できるだけ 14 日以内
- 修正方針または緩和策の共有: ベストエフォート

## Scope notes
この repository では以下を主な対象とします。
- HTTP API
- MCP tool surface
- ダッシュボード UI
- SQLite persistence と ingest / publish flow

秘密情報、個人情報、実運用 credential は issue や log に含めないでください。
サンプル設定に含まれる API key や Secret 値は無効なプレースホルダーであり、そのまま再利用しないでください。
