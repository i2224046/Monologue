# 処理フロー分析レポート
**日時**: 2026-01-06 16:28:26 (ログ内ファイル名より推定)

---

## タイムテーブル（シーケンス順）

※ログにタイムスタンプがないため、イベント順序のみを記載します。

| 順序 | イベント | FlowState |
|------|----------|-----------|
| 1 | **アプリ起動・初期化** | - |
| 2 | `RuneSpawner` / `PanelController` / `SubPanelController` 起動 | - |
| 3 | Pythonプロセス開始 (`main_vision_voice.py`) | - |
| 4 | **🔄 FlowState → Waiting** | **⬤ Waiting** |
| 5 | Router: Init Messages (DeepSeek, Camera, YOLO, Ollama) | Waiting |
| 6 | Router: "Clients initialized successfully" (Hybrid Mode) | Waiting |
| 7 | **キャプチャトリガー** (Camera Index 0 発見) | Waiting |
| 8 | コマンド送信: `CAPTURE 0` | Waiting |
| 9 | **🔄 FlowState → Scanning** | **⬤ Scanning** |
| 10 | Camera: 撮影 & 処理 (1280x720, Stabilized) | Scanning |
| 11 | YOLO: 1 object detected "cell phone" (0.66) | Scanning |
| 12 | Preprocess: CLAHE / BG Removal Success | Scanning |
| 13 | Ollama: Analysis "Smartphone" (Sharp, Normal) | Scanning |
| 14 | DeepSeek: Prompt送信 (Memory/Sensation) | Scanning |
| 15 | DeepSeek: 回答受信 "隣の鍵が鳴るたび、少し揺れるんだ。" | Scanning |
| 16 | **🔄 FlowState → ScanComplete** | **⬤ ScanComplete** |
| 17 | Router: キャラ名受信 "静かな記憶を持つもの" | ScanComplete |
| 18 | Router: メッセージ受信 & 保存 | ScanComplete |
| 19 | **🔄 FlowState → Message** | **⬤ Message** |
| 20 | SubPanelController: ShowMessage (Typewriter開始) | Message |
| 21 | **❌ TTS Error**: Connection refused (Port 50032) | Message |
| 22 | TTS失敗 ("Audio Gen Failed") | Message |
| 23 | **🔄 FlowState → End** | **⬤ End** |
| 24 | **🔄 FlowState → Waiting** | **⬤ Waiting** |
| 25 | QuoteCardDisplay: カード表示 (1件) | Waiting |
| 26 | Xキー3回押下 → ファイル削除 & 終了 | Waiting |

---

## FlowState遷移サマリー

```
[Start]
  ↓
⬤ Waiting
  ↓  (Capture Trigger)
⬤ Scanning      ← CAPTUREコマンド送信
  ↓  (DeepSeek完了)
⬤ ScanComplete  ← "ScanComplete"検知
  ↓  (MessageReady)
⬤ Message       ← 表示開始 (TTS失敗)
  ↓  (完了)
⬤ End
  ↓  (即時)
⬤ Waiting       ← QuoteCard表示
```

---

## 処理結果・エラー分析

| コンポーネント | 結果 | 詳細 |
|----------------|------|------|
| **Camera** | ✅ 成功 | Index 0 (USB Camera) を正常に認識・撮影。OBSは無視されました。 |
| **YOLO** | ✅ 成功 | "cell phone" を検出。 |
| **Ollama** | ✅ 成功 | "Smartphone" として認識。形状:Sharp, 状態:Normal。 |
| **DeepSeek** | ✅ 成功 | コンテキストに沿った台詞生成に成功。<br>出力: "隣の鍵が鳴るたび、少し揺れるんだ。 by 静かな記憶を持つもの" |
| **TTS** | ❌ 失敗 | **Connection refused (Port 50032)**。<br>音声合成サーバーに接続できず、音声は再生されませんでした。 |
| **UI表示** | ✅ 成功 | TypewriterEffectによりテキストは正常に表示されました。 |

---

## 改善が必要な点

1.  **TTSサーバー接続エラー**: ローカルのTTSサーバー(Port 50032)が起動していないか、接続が拒否されています。Python側のTTSサーバー起動処理を確認する必要があります。
2.  **QuoteCardDisplay**: 起動直後に `MessagePairs.json` が見つからないエラーが出ていますが、これは初回起動時であれば正常な挙動です（終了時に削除されているため）。
