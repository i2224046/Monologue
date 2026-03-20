# Mono-Logue ワークフロー図

Last Updated: 2026-02-10

システム全体の処理フローをMermaid図で可視化します。

---

## 全体アーキテクチャ

```mermaid
graph LR
    subgraph Unity
        User[ユーザー] -->|Space Key| CT[captureTrigger]
        CT -->|CAPTURE idx| PL[PythonLauncher]
        PL <-->|stdin/stdout| Python
        PL --> Router[PythonMessageRouter]
        Router --> FM[FlowManager]
        Router --> UI[UI Components]
    end
    
    subgraph Python
        Main[main_vision_voice.py]
        Cam[camera_capture.py]
        YOLO[yolo_processor.py]
        Ollama[ollama_client.py]
        DeepSeek[deepseek_client.py]
        Voice["voice_client.py (Disabled)"]
    end
```

---

## 画像処理フロー

```mermaid
flowchart TD
    A["CAPTURE コマンド受信"] --> B["カメラ撮影 (camera_capture.py)"]
    B --> C["露出安定待ち (Warmup)"]
    C --> D["5フレーム連写"]
    D --> E["中央値合成 (フリッカー対策)"]
    E --> F["raw/に保存 (元画像)"]
    
    F --> G["YOLO検出 (yolo_processor.py)"]
    G --> H{検出数}
    H -->|0| I[元画像を使用]
    H -->|1| J[単一オブジェクトクロップ]
    H -->|2+| K[全オブジェクト統合クロップ]
    
    I --> L["明度補正 & CLAHE"]
    J --> L
    K --> L
    
    L --> M["背景除去 (rembg)"]
    M --> N["capture/に保存 (最終画像)"]
    N --> O[Ollama詳細分析へ]
```

---

## 分析〜音声生成フロー

```mermaid
sequenceDiagram
    participant Main as MainLoop
    participant Cam as Camera
    participant YOLO as YOLO
    participant Pre as Preprocess
    participant Ollama as Ollama
    participant DS as DeepSeek
    participant Unity as Unity

    Main->>Cam: Capture (Stabilized)
    Cam-->>Main: Raw Frame
    Main->>YOLO: Detect & Crop
    YOLO-->>Main: Cropped Frame
    Main->>Pre: Brightness/CLAHE/rembg
    Pre-->>Main: Final Image
    Main->>Ollama: Analyze (Vision)
    Ollama-->>Main: JSON (State/Shape)
    Main->>DS: Generate Dialogue (Persona Logic)
    DS-->>Main: Text (Dialog & TwistedName)
    Main->>Unity: [[MESSAGE]] Text
    Note over Main,Unity: TTS (COEIROINK) is currently disabled
    Unity->>Unity: Typewriter Display
```

---

## 状態遷移図

```mermaid
stateDiagram-v2
    [*] --> Waiting
    
    Waiting --> Scanning: Space Key (CAPTURE)
    Scanning --> ScanComplete: DeepSeek完了
    ScanComplete --> Message: 自動遷移 (NotifyMessageReady)
    Message --> End: 表示時間終了
    End --> Waiting: タイムアウト
    
    note right of Waiting: 過去ログ表示
    note right of Scanning: スキャン演出
    note right of Message: メッセージ表示
```

---

## フォルダ構造

```
StreamingAssets/
├── capture/
│   ├── raw/           # 元画像 (タイムスタンプ付き)
│   └── camera_*.png   # 最終画像 (背景透過・補正済み)
├── voice/             # 音声ファイル (現在は生成されません)
├── main_vision_voice.py
├── camera_capture.py
├── yolo_processor.py
├── ollama_client.py
├── deepseek_client.py
├── voice_client.py
└── prompts.py
```

---

## 関連ドキュメント

- [ScriptArchitecture.md](./ScriptArchitecture.md) - Unity C#スクリプト構造
- [PythonScripts.md](./PythonScripts.md) - Pythonモジュール詳細
