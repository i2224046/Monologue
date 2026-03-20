# 作品体験フロー

Last Updated: 2026-02-10

本ドキュメントでは、インスタレーション作品「Mono-Logue」の体験フローを、図とテキストで解説します。

---

## 体験フロー全体図

```mermaid
---
config:
  look: neo
  layout: dagre
---
flowchart TB
 subgraph s2["ローカルPC"]
        B["⌨️ Spaceキー押下 / センサー反応"]
        C["📷 webカメラ撮影"]
        D["🔍 画像処理・前処理（OpenCV/rembg）"]
        E["🎁 物体認識・詳細分析（Ollama）"]
  end
 subgraph s3["外部"]
        F["💭 AIがセリフ生成（DeepSeek）"]
  end
    A["📦 物を台に置く"] --> B
    B --> C
    C --> D
    D --> E
    E -- クラウド --> F
    F --> G["👀 メッセージを見る"]
    G --> H["✔️ 体験終了"]

    style B stroke:none
    style C stroke:none
    style D stroke:none
    style E stroke:none
    style F stroke:none
    style A fill:#FFCDD2,stroke-width:1px,stroke-dasharray: 1,stroke:none
    style H stroke-width:1px,stroke-dasharray: 1,fill:#FFCDD2,stroke:none
    style s2 fill:#FFF9C4,stroke:none
    style s3 fill:#BBDEFB,stroke:none
```

---

## フェーズ別 詳細解説

### フェーズ① 待機状態（Waiting）

```mermaid
flowchart LR
    W[待機状態] --> |過去のセリフが<br>画面を流れる| W
    W --> |物が置かれる| NEXT[撮影へ]
```

| 項目 | 内容 |
|------|------|
| **体験者の状態** | 作品の前に立ち、物を置くことを促される |
| **画面表示** | マトリックス風に過去の「モノの言葉」が流れている |
| **BGM** | 環境音楽が静かに再生 |

---

### フェーズ② 撮影（Capture）

```mermaid
flowchart LR
    A[物を台に置く] --> B[Spaceキーを押す]
    B --> C[カメラがキャプチャ]
    C --> D[撮影音が鳴る]
```

| 項目 | 内容 |
|------|------|
| **体験者のアクション** | 台の上に持ち物（スマホ、鍵、財布など）を置き、Spaceキーを押す |
| **システムの動作** | Webカメラが物体を撮影し、画像を保存 (Unity → Python連携) |
| **フィードバック** | 撮影音（シャッター音）で撮影完了を知らせる |

---

### フェーズ③ スキャン・解析（Scanning）

```mermaid
flowchart TB
    A[Unityからの通知（標準入力）] --> B[**YOLO-World**<br>物体検出し画像の切り抜き]
    B --> C[**OpenCV/rembg**<br>色調補正・背景削除]
    C --> D[**ローカルLLM（Ollama）**<br> 詳細分析（状態・形状）]
    D --> E[**クラウドLLM（DeepSeek）**<br>性格付与・セリフ生成]
    E --> F[Unityへの通知（標準出力）]
```

| 項目 | 内容 |
|------|------|
| **画面表示** | 「Scanning...」「Analyzing...」などの演出 |
| **物体認識** | YOLO-Worldで検出後、Ollamaで詳細（形状、古さなど）を分析 |
| **セリフ生成** | DeepSeekにより、物体の状態に基づいた「本音」と「異名」を生成 |
| **音声合成** | ※現在は無効化設定中（COEIROINK連携機能あり） |
| **所要時間** | 約10〜20秒 |

---

### フェーズ④ メッセージ表示（Monologue）

```mermaid
flowchart LR
    A[セリフ表示開始] --> B[タイプライター演出<br>で文字が現れる]
    B --> C[（音声再生）]
    C --> D[ルーン文字エフェクト]
```

| 項目 | 内容 |
|------|------|
| **画面表示** | 生成されたセリフがタイプライターアニメーションで表示 |
| **音声** | ※現在は無効化設定中 |
| **視覚エフェクト** | ルーン文字が浮かび上がる演出 |
| **サブ画面** | 解析結果とセリフがログとして表示 |

---

### フェーズ⑤ 終了・余韻（End）

```mermaid
flowchart LR
    A[表示完了] --> B[余韻の演出]
    B --> C[画面が静かになる]
    C --> D[待機状態に戻る]
```

| 項目 | 内容 |
|------|------|
| **体験者の状態** | セリフを受け止める / 次の物を試す準備 |
| **画面** | 短い余韻を経て、再び待機画面に遷移 |
| **ループ** | 別の物を置けば、また体験が始まる |

---

## 状態遷移図（システム視点）

```mermaid
stateDiagram-v2
    [*] --> Waiting: 起動時
    
    Waiting --> Scanning: Spaceキー / 連携コマンド
    Scanning --> ScanComplete: AI処理完了 (DeepSeek生成完了)
    ScanComplete --> Message: 自動遷移 (NotifyMessageReady)
    Message --> End: 表示時間終了
    End --> Waiting: タイムアウト
    
    note right of Waiting: 過去ログ表示中
    note right of Scanning: 解析演出中
    note right of Message: セリフ表示中
```

---

## 体験の流れ（まとめ）

| 順番 | フェーズ | 体験者 | システム |
|------|---------|--------|----------|
| 1 | 待機 | 作品の前に立つ | 過去のセリフを表示 |
| 2 | 撮影 | 物を置き、Spaceキーを押す | コマンド受信 → 撮影・保存 |
| 3 | 解析 | 待つ | YOLO検出 → Ollama分析 → DeepSeek生成 |
| 4 | 独白 | 見る | セリフ表示（タイプライター） |
| 5 | 終了 | 余韻を味わう | 待機状態へ戻る |

---