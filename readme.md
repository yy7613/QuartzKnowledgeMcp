# ハーネス概要

## このリポジトリの目的
コーディングエージェントで PoC を段階的に進めるためのハーネスです。
`ideas` -> `docs` -> `implementation` -> `src` の流れで、アイデアを仕様・計画・実装へ整理していきます。

## フォルダ構成と役割
| フォルダ / ファイル | 役割 |
|:--|:--|
| `ideas` | アイデアや要求を書き溜める場所 |
| `docs` | 仕様書・設計ドキュメント |
| `docs/adr` | 技術・設計上の意思決定を記録する ADR |
| `implementation` | フェーズ別の実装計画と完了報告書 |
| `src` | ソースコードとテスト |
| `work` | docs と実装の差異・課題一覧 |
| `rules.md` | このハーネスのルール |
| `readme.md` | このファイル。ハーネスの使い方と現在地 |

## 作業の進め方（ユーザー向け）
1. `ideas/template/idea-template.md` をコピーしてアイデアを書く
2. エージェントに `ideas` を渡し、`docs` へ仕様化させる
3. エージェントに `docs` を渡し、`implementation` へフェーズ計画を作成させる
4. `implementation/phase-XX.md` の計画をエージェントに指示して `src` に実装させる
5. 完了報告書と `work` の内容を確認して次のフェーズへ進む

## エージェントへの指示例
- 仕様化: `ideas/xxx.md` を読んで `docs/spec.md` を作成してください。`rules.md` に従ってください。
- 実装計画: `docs` を読んで `implementation/phase-01.md` を更新してください。`rules.md` に従ってください。
- 実装: `implementation/phase-01.md` の計画に従い `src` に実装してください。`rules.md` に従ってください。

## 現在のフェーズ
Phase 01: スケルトン準備中（`implementation/phase-01.md` を参照）

## テストカバー率の計測方法
採用技術に合わせて記載します。
例: `dotnet test --collect:"XPlat Code Coverage"`
