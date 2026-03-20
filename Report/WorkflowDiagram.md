# Mono-Logue ワークフロー図

システム全体の処理フローをMermaid図で可視化します。

---

## 全体アーキテクチャ

```mermaid
graph LR
    subgraph Unity
        User[ユーザー] -->|Space| CT[captureTrigger]
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
        Voice[voice_client.py]
    end
```

---

## 画像処理フロー

```mermaid
flowchart TD
    A[CAPTURE コマンド受信] --> B[カメラ撮影]
    B --> C[5フレーム捨て]
    C --> D[5フレーム取得]
    D --> E[中央値合成]
    E --> F[raw/に保存]
    
    F --> G[YOLO検出]
    G --> H{検出数}
    H -->|0| I[元画像使用]
    H -->|1| J[単一クロップ]
    H -->|2+| K[統合クロップ]
    
    I --> L[CLAHE適用]
    J --> L
    K --> L
    
    L --> M[背景除去]
    M --> N[capture/に保存]
    N --> O[Ollama分析]
```

---

## 分析〜音声生成フロー

```mermaid
sequenceDiagram
    participant Cam as カメラ
    participant YOLO as YOLO
    participant Pre as 前処理
    participant Ollama as Ollama
    participant DS as DeepSeek
    participant TTS as COEIROINK
    participant Unity as Unity

    Cam->>YOLO: 撮影画像
    YOLO->>Pre: クロップ済み画像
    Pre->>Ollama: 前処理済み画像
    Ollama->>DS: 分析JSON
    DS->>TTS: セリフテキスト
    TTS->>Unity: 音声ファイル
    Unity->>Unity: 再生＆表示
```

---

## 状態遷移図

```mermaid
stateDiagram-v2
    [*] --> Waiting
    
    Waiting --> Scanning: CAPTURE送信
    Scanning --> ScanComplete: DeepSeek完了
    ScanComplete --> Message: 自動遷移
    Message --> End: 音声再生完了
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
│   ├── raw/           # 元画像
│   └── camera_*.png   # 最終画像
├── voice/             # 音声ファイル
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
