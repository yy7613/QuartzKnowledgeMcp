# Phase 02: Medallion 基盤と整理エージェント

## 目的
MCP ナレッジベースのデータを Bronze / Silver / Gold の段階で管理できる基盤を作る。
あわせて、LLM を設定した場合のみ自動整理を行う Microsoft Agent Framework ベースのエージェント機能を実装する。

## 実装対象
- .NET ベースのアプリケーションプロジェクト
- EF Core + SQLite の永続化基盤
- Bronze / Silver / Gold のデータモデル
- 更新履歴モデル
- LLM 無効時の規則ベース整理サービス
- LLM 有効時の Agent Framework 整理サービス
- 動作確認用の最小 API

## 具体的なタスク
1. `src` に .NET Web API プロジェクトとソリューションを作成する
2. EF Core と SQLite を導入し、DB provider 差し替え可能な構成にする
3. Bronze / Silver / Gold と更新履歴のエンティティ、DbContext、初期設定を実装する
4. Bronze 取り込みから Silver / Gold 生成までのアプリケーションサービスを実装する
5. LLM 無効時に動く規則ベース整理を実装する
6. LLM 有効時に Microsoft Agent Framework と Foundry 設定で動く整理エージェントを実装する
7. 最小限の API エンドポイントとテストを追加する
8. `dotnet build` とテストで検証する

## 突破基準
- アプリケーションがビルド成功する
- Bronze / Silver / Gold のデータが保存できる
- 更新履歴が記録される
- LLM 無効時でも整理処理が動作する
- LLM 設定がある場合のみエージェント機能が有効化される

## スコープ外
- Embedding を用いたベクトル検索の本実装
- 専用グラフ DB の導入
- 高度な UI 実装
- 外部サイトからの自動収集

## 備考
- モデル名はコードに固定せず設定値から受け取る
- Foundry 認証がない環境でもアプリ本体は動作するようにする
- Agent Framework の Foundry 連携はオプション機能として分離する
