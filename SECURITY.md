# Security Policy

## Supported versions
現在の security fixes は `main` branch を基準に提供します。リリースタグを切った後は、最新タグと `main` を優先して対応します。

## Reporting a vulnerability
脆弱性の報告は public issue に書かないでください。まず GitHub の Security tab にある private vulnerability reporting を利用してください。

もし repository 側で private report が未設定の場合は、maintainer に private channel で連絡し、再現手順、影響範囲、想定される悪用条件を共有してください。

報告には次の情報を含めてください。
- 対象 endpoint または MCP tool
- 再現手順
- 影響範囲
- 想定する severity
- 可能なら修正案または緩和策

## Response goals
- 受領確認: 3 営業日以内
- 初回トリアージ: 7 営業日以内
- 修正方針または緩和策の共有: 14 営業日以内

## Scope notes
この repository では以下を主な対象とします。
- HTTP API
- MCP tool surface
- dashboard の operator-facing UI
- SQLite persistence と ingest / publish flow

秘密情報、個人情報、実運用 credential は issue や log に含めないでください。
