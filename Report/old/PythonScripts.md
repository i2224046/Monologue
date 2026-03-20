# Mono-Logue Pythonスクリプト構造

本ドキュメントでは、`Assets/StreamingAssets` 内のPythonスクリプトの機能と役割をまとめます。

## スクリプト一覧

### コアモジュール

| スクリプト | 役割 |
| :--- | :--- |
| **main_vision_voice.py** | メインオーケストレーター。stdin監視、カメラ撮影、YOLO、前処理、Ollama分析、DeepSeekセリフ生成、音声合成を統合 |
| **camera_capture.py** | フリッカー対策付きカメラ撮影モジュール（5フレームウォームアップ + 5フレーム中央値合成） |
| **yolo_processor.py** | YOLOv11によるオブジェクト検出とクロップ処理（複数オブジェクト時は統合バウンディングボックス） |

---

### クライアントモジュール

| スクリプト | 役割 |
| :--- | :--- |
| **ollama_client.py** | ローカルOllama（qwen2.5vl:7b）への画像分析リクエスト |
| **deepseek_client.py** | DeepSeek APIへのセリフ生成リクエスト |
| **voice_client.py** | COEIROINK（ローカルTTS）への音声合成リクエスト |

---

### 設定・プロンプト

| スクリプト | 役割 |
| :--- | :--- |
| **prompts.py** | 分析プロンプト（CoT形式）、トピックリスト、ペルソナロジック |
| **item_obsessions.py** | アイテム別「執着」指示の定義 |
| **config.json** | 音声設定（VOICE_VARIANTS）、パーソナリティ設定 |

---

## フォルダ構造

```
StreamingAssets/
├── capture/
│   ├── raw/           # 処理前の元画像
│   └── camera_*.png   # 最終処理済み画像（YOLO+CLAHE+背景除去）
├── voice/             # 生成された音声ファイル
└── Message.txt        # 生成されたメッセージのログ
```

---

## 画像処理パイプライン

1. **カメラ撮影** (`camera_capture.py`)
   - 5フレーム捨て（露出安定）
   - 5フレーム取得 → 中央値合成（フリッカー除去）

2. **YOLO検出** (`yolo_processor.py`)
   - 0検出: 元画像をそのまま使用
   - 1検出: 単一オブジェクトをクロップ（10%マージン）
   - 2+検出: 全オブジェクトを含む統合バウンディングボックス

3. **前処理** (`main_vision_voice.py`)
   - CLAHE: コントラスト調整
   - rembg: 背景除去

4. **分析** (`ollama_client.py`)
   - 最終処理済み画像をOllamaに送信
   - CoT形式で観察→推論→JSON出力

---

## 依存ライブラリ

```bash
pip install ultralytics opencv-python rembg onnxruntime ollama watchdog
```

---

## 関連ドキュメント

- [ScriptArchitecture.md](./ScriptArchitecture.md) - Unity C#スクリプト構造
- [WorkflowDiagram.md](./WorkflowDiagram.md) - 全体ワークフロー図
