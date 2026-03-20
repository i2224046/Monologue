# 卒業制作「モノ・ローグ」仕様 v14 と現状のギャップ分析書

**作成日**: 2025/12/11  
**対象仕様書**: v14 (KAFU様式UI・幾何学アクセント採用)

本ドキュメントは、現在のUnity/Python実装状況と、最新のv14仕様書との差異を分析し、仕様に準拠するための解決策を提案するものです。

---

## 1. 現状の実装状況 (Current Status)

++### 1-1. システム構成
- **Python側 (`main_vision_voice.py`)**:
    - **機能**: 画像監視 -> Ollama(画像解析) -> 属性/トーン判定 -> Gemini(対話生成) -> VOICEVOX/COEIROINK(音声合成) というフローは確立されています。
    - **トーン**: `determine_tone` 関数により、アイテムの状態スコアに基づいて「Fresh」「Wise」「Casual」を切り替えるロジックがあり、v14の「モノとの関係性の再発見」というコンセプトに合致しています。
    - **キャラクター**: `config.json` にて "The Nature of Secrets" 等の役割定義がなされており、"Cold Reading" の要素は実装済みです。
- **Unity側 (`FlowManager.cs`, `PanelController.cs`)**:
    - **状態管理**: `Waiting` -> `Scanning` -> `ScanComplete` -> `Message` -> `End` という基本遷移は実装されています。
    - **マルチディスプレイ**: `DisplayManager.cs` および `ActivateSubDisplay.cs` が存在しますが、単に「2枚目を有効化する」だけで、コンテンツの出し分け（アーカイブモニターへの表示など）は未実装です。
    - **UI表示**: `TimelineState` プレハブを切り替える方式ですが、画面全体を切り替えるのみで、「台座（Main）」と「机（Peripheral）」の空間的な書き分けは考慮されていません。

---

## 2. 仕様とのギャップ (Gap Analysis)

### 2-1. 空間構成・投影設計 (Spatial Design)
v14仕様では、1台のプロジェクターで **「中央の傾斜台座（高輝度）」** と **「左右の机天板（低輝度・背景）」** を同時に投影・演出分けする必要があります。
現在の `PanelController` は `mainCanvasRoot` に単一のパネルを表示するのみで、この物理的なレイヤー構造（Layer 1 vs Layer 2）を反映したUI設計になっていません。

### 2-2. 視覚演出・UIデザイン (Visual Identity)
- **Geometric Accent (#9D4EDD)**:
    - 仕様: Idle時は完全モノクロ、Active時（解析・翻訳時）のみ紫色の幾何学アクセントが出現。
    - 現状: コード上で色やシェイプを制御するロジックが見当たりません。プレハブ側での対応が必要ですが、状態遷移時に「色を変える/アクセントを出す」というパラメータの受け渡しが欠如しています。
- **Archive演出 (Sublimation)**:
    - 仕様: テキストが紫色の粒子となって上部の「アーカイブモニター」へ吸い込まれる。
    - 現状: `FlowManager` は `End` 状態になると単に `ShowEndPanel` を呼ぶだけで、テキストの移動やパーティクル演出、サブモニターとの連携がありません。

### 2-3. スクリプトの重複
- `DisplayManager.cs` と `ActivateSubDisplay.cs` が同一の機能（サブディスプレイ有効化）を持っており、冗長です。

---

## 3. 解決策の提案 (Proposed Solutions)

### 3-1. UIキャンバスのレイヤー分割 (Spatial UI)
UnityのUI構造を物理配置に合わせて再構築することを提案します。

1.  **Main Canvas (台座用)**:
    - 画面中央（W300xH300相当のエリア）に配置。
    - 背景色: 白（物理台座）に合わせた高コントラストなテキスト表示。
    - `Layer 1: Sensor Unit` に対応。
2.  **Peripheral Canvas (机用)**:
    - 画面全体（Main Canvasの裏または周囲）。
    - 背景色: 黒。グリッドやシステムUIを薄く表示。
    - `Layer 2: Peripheral` に対応。
3.  **Archive Canvas (サブモニター用)**:
    - `Display 2` に割り当てられたCanvas。
    - `Layer 3: Archive` に対応。

### 3-2. Archive演出の実装 (Archive Manager)
`FlowManager` の `End` ステートにて、以下のステップを実行する `ArchiveEffectManager` を作成することを提案します。

1.  **Text Dissolve**: メイン画面のテキストをメッシュ化し、パーティクルシステムで上方向へ飛ばす。
2.  **Sub-Monitor Reception**: 同時にサブモニター側のCanvasで、パーティクルが集合してログテキストとして定着するアニメーションを再生する。

### 3-3. デザイン実装 (Geometric Accent)
- **GlobalSettings**: アクセントカラー（`#9D4EDD`）を一括管理する `ScriptableObject` または静的クラスを作成。
- **State-Based UI**: `PanelController` の各プレハブ（Scanning, Message）に対し、Active時のみこのアクセントカラーを適用する設定を行う。

---

## 4. 今後の実装タスク (Implementation Plan)

### Phase 1: クリーンアップ & 基盤整備
- [ ] **スクリプト統合**: `DisplayManager.cs` と `ActivateSubDisplay.cs` を統合し、`SystemBootController` として初期化処理をまとめる。
- [ ] **Canvas再構築**: シーン内のCanvasを `SensorCanvas`, `PeripheralCanvas`, `ArchiveCanvas` の3つに分割・配置。

### Phase 2: デザイン適用 (v14 Look)
- [ ] **Idle (Waiting) UI**: モノクロ・グレーのみの静寂なデザインを実装。
- [ ] **Active (Scanning/Message) UI**: `#9D4EDD` の幾何学模様（三角形・ライン）を含むUIプレハブを作成。
- [ ] **周辺演出**: PeripheralCanvasに薄いグリッドや環境演出を追加。

### Phase 3: Archive演出
- [ ] **VFX**: テキストが粒子化して上に登るVisual Effect Graph (またはParticle System)を作成。
- [ ] **連携ロジック**: `FlowManager.cs` の `OnMessageFinished` で `ShowEndPanel` する代わりに、`ArchiveEffectManager.PlaySequence()` を呼ぶように変更。