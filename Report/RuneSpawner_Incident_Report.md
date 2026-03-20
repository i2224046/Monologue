# RuneSpawner 消失問題に関する原因レポート

## 概要
FlowManagerのリファクタリング後、`RuneSpawner`（ルーン文字エフェクト）が表示されなくなる不具合が発生した。調査の結果、スクリプトの実行順序（Execution Order）とオブジェクトのインスタンス化タイミングに関連する競合（Race Condition）が原因であることが判明した。

## 発生事象
1. アプリケーション起動時、`Waiting` パネルが表示されない（2回目以降は表示される）。
2. `ScanComplete` 状態に遷移しても、ルーン文字エフェクト（RuneSpawner）が動作しない。
3. ログに「RuneSpawnerが見つかりません」または「スポーンしません（条件未達）」と出力される。

## 原因分析

### 1. Waitingパネルが表示されない問題
**原因:** `Start()` メソッドの実行順序競合
- `FlowManager.Start()` が `PanelController.Start()` よりも先に実行されてしまっていた。
- `FlowManager` は開始時に `ChangeState(Waiting)` を呼び出し、パネルを表示しようとする。
- しかし、`PanelController` はまだ `Start()` しておらず、プレハブのインスタンス化（`instanceWaiting` の生成）が完了していなかった。
- 結果、`ShowWaitingPanel()` 内で `null` チェックにより処理がスキップされた。

### 2. RuneSpawner が動作しない問題
**原因:** 参照の喪失と初期化タイミングのズレ
- リファクタリングにより、RuneSpawnerへの参照ロジックを `FlowManager`（Inspector直接参照）から `PythonMessageRouter`（PanelController経由での動的取得）に変更した。
- `PythonMessageRouter` は `Start` 時に `PanelController` から `RuneSpawner` の参照を取得しようとするが、上記同様 `PanelController` の初期化が完了していないため `null` となることがあった。
- また、`ScanComplete` パネルへの遷移時、パネルが表示（Active化）されるタイミングと、メッセージを送信（SetMessage）するタイミングにズレが生じた。
    - パネル表示（OnEnable）時点ではまだメッセージが届いていない → 「スポーンしません」ログ
    - メッセージ到着時、Routerが保持しているRuneSpawner参照が `null` または無効であったため、`SetMessage` が失敗した。

## 解決策

### 1. 初期化タイミングの適正化
- `PanelController.cs` の初期化メソッドを `Start()` から `Awake()` に変更。
- これにより、他のスクリプトの `Start()` が呼ばれる前に、確実に全てのパネルインスタンス（`instanceWaiting`, `instanceScanComplete` 等）が生成されるようになった。
- **結果:** `FlowManager` が起動直後にパネルを操作しても正常に動作するようになった。

### 2. Routerロジックの堅牢化（Fallbackの実装）
- `PythonMessageRouter.cs` に以下のロジックを追加：
    1. **強制同期実行:** `ScanComplete` 遷移時に、コルーチンによる1フレーム待機をやめ、即座にメッセージを適用するように変更（タイミング問題を解消）。
    2. **シーン全体検索（Fallback）:** `PanelController` 経由で `RuneSpawner` が取得できなかった場合、`FindFirstObjectByType<RuneSpawner>(FindObjectsInactive.Include)` を使用してシーン内から強制的に見つけ出すロジックを追加。
    3. **INFOタグ除去:** Pythonログの `[[INFO]]` タグが重複して付与される問題を修正し、綺麗なメッセージが渡るようにした。

## 結論
Unityにおける `Start()` の実行順序は保証されないため、依存関係のある初期化（特に `Instantiate` を伴うもの）は `Awake()` で行うか、明示的な初期化フローを組む必要がある。今回は `Awake()` への移動と、参照取得失敗時のフォールバック検索を組み合わせることで、堅牢なシステムとなった。
