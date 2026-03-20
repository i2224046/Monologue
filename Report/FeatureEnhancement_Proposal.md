# 機能拡張提案書

**作成日:** 2026-02-03  
**対象:** モノ・ローグ（Mono-Logue）

本ドキュメントでは、3つの機能拡張について現状の問題点と実装方法を解説します。

---

## 1. 信頼度に基づく抽象名称表示

### 現状の問題

現在のシステムでは、DeepSeekが「捻った名前（TWISTED_NAME）」を生成して表示しています。

```
いつも何見てるか、私知ってるよ？ by 全部知ってるスマホ
また画面近すぎ、目悪くなるよ by 距離感を知るメガネ
もうちょっときれいに書いてくれない？ by 字の汚さを知るペン
```

この「TWISTED_NAME」は `形容詞 + アイテム名` の形式ですが、元となる `item_name`（Ollamaの認識結果）の精度が低い場合、誤った名前が含まれたまま表示されてしまい、体験の没入感が損なわれます。

**例:** 実際はハンカチなのに「全部見てるスマートフォン」と表示されてしまう

### 解決策

**信頼度が閾値以下の場合、抽象的なカテゴリ名にフォールバック**

| 具体名 | 抽象カテゴリ |
|:---|:---|
| スマートフォン / タブレット / ノートPC | 機械 |
| ハンカチ / タオル / マフラー | 布 |
| 財布 / カバン / ポーチ | 皮革製品 or 袋 |
| ペン / 定規 / ハサミ | 文房具 |
| コップ / 水筒 / マグカップ | 容器 |
| 鍵 / 鍵束 | 金属 |

### 実装方法

#### A. Python側の変更

##### 1. `ollama_client.py` の拡張

Ollamaプロンプトに「confidence」フィールドを追加：

```python
# prompts.py に追加
ANALYSIS_PROMPT_V2 = """
**Step 3: FINAL ANSWER**
Output your conclusion in strict JSON format:
{
  "is_machine": true/false,
  "shape": "Round/Sharp/Square/Other",
  "state": "Old/New/Dirty/Broken/Normal",
  "item_name": "オブジェクト名",
  "item_category": "machine/cloth/container/stationery/leather/metal/other",
  "confidence": 0.0-1.0  // 認識の確信度
}
"""
```

##### 2. `main_vision_voice.py` に抽象化ロジック追加

```python
# category_mapping.py（新規作成）
ABSTRACT_NAMES = {
    "machine": "機械",
    "cloth": "布",
    "container": "容器",
    "stationery": "文房具",
    "leather": "皮革製品",
    "metal": "金属",
    "other": "モノ"
}

def get_display_name(analysis_data, threshold=0.7):
    """信頼度に応じて具体名または抽象名を返す"""
    confidence = analysis_data.get("confidence", 0.5)
    if confidence >= threshold:
        return analysis_data.get("item_name", "モノ")
    else:
        category = analysis_data.get("item_category", "other")
        return ABSTRACT_NAMES.get(category, "モノ")
```

##### 3. `[[CREDIT]]` 出力時に適用

```python
# main_vision_voice.py の _process_analysis() 内
display_name = get_display_name(analysis_data, threshold=0.7)
credit = f"by {display_name}"
print(f"[[CREDIT]] {credit}")
```

#### B. 変更が必要なファイル

| ファイル | 変更内容 |
|:---|:---|
| `prompts.py` | `ANALYSIS_PROMPT` に confidence, item_category 追加 |
| `ollama_client.py` | 新フィールドの正規化処理 |
| `category_mapping.py` | 新規作成：抽象名マッピング辞書 |
| `main_vision_voice.py` | `get_display_name()` 呼び出し追加 |

---

## 2. AI思考中の先行アイテム名表示

### 現状の問題

現在の処理フローでは、セリフ生成完了まで「何を認識したか」がユーザーに伝わりません。 
Scanning画面では処理ログが流れるだけで、待ち時間が長く感じられる問題があります。

```
現在のフロー:
[CAPTURE] → [YOLO検出] → [Ollama分析] → [DeepSeek生成] → [[MESSAGE]] 表示
                                   ↑
                               ここで判明しても表示されない
```

### 解決策

**Ollama分析完了時点で「○○からの声を聞き取っています…」を先行表示**

```
改善後のフロー:
[CAPTURE] → [YOLO検出] → [Ollama分析] → [[ITEM_IDENTIFIED]] 表示
                                              ↓
                        「スマートフォンからの声を聞き取っています…」
                                              ↓
                                       [DeepSeek生成] → [[MESSAGE]] 表示
```

低信頼度時は抽象名を使用：
- 高信頼度: 「スマートフォンからの声を聞き取っています…」
- 低信頼度: 「機械からの声を聞き取っています…」
- 代替案: 「赤い小物からの声を聞き取っています…」（色ベース）

### 実装方法

#### A. Python側の変更

##### 1. 新規タグ `[[ITEM_IDENTIFIED]]` の出力

```python
# main_vision_voice.py の process_frame() 内
# Ollama分析直後に追加
analysis_data = ollama_client.analyze_image(processed_path)
display_name = get_display_name(analysis_data)
print(f"[[ITEM_IDENTIFIED]] {display_name}")  # ← 新規追加
sys.stdout.flush()

# その後、DeepSeek生成へ続行
```

##### 2. （オプション）色情報の追加

```python
# prompts.py の ANALYSIS_PROMPT に追加
"dominant_color": "red/blue/green/yellow/black/white/brown/gray/other"
```

#### B. Unity側の変更

##### 1. `PythonMessageRouter.cs` にハンドラ追加

```csharp
// PythonMessageRouter.cs
if (line.Contains("[[ITEM_IDENTIFIED]]"))
{
    var itemName = line.Replace("[[ITEM_IDENTIFIED]]", "").Trim();
    HandleItemIdentified(itemName);
}

private void HandleItemIdentified(string itemName)
{
    string message = $"{itemName}からの声を聞き取っています…";
    scanningTextDisplay?.SetText(message);
}
```

##### 2. `ScanningTextDisplay.cs` の拡張

現在のログ表示からメッセージ表示への切り替え処理を追加。

#### C. 変更が必要なファイル

| ファイル | 変更内容 |
|:---|:---|
| `main_vision_voice.py` | `[[ITEM_IDENTIFIED]]` 出力追加 |
| `prompts.py` | （オプション）dominant_color 追加 |
| `PythonMessageRouter.cs` | 新タグのハンドリング追加 |
| `ScanningTextDisplay.cs` | メッセージ表示処理追加 |

---

## 3. 待機中画像のスマートランダム表示

### 現状の問題

`QuoteCardDisplay.cs` は `MessagePairs.json` からランダムに画像とメッセージのペアを表示しますが：

1. **最新のペアが優先されない** — 直前の体験の印象が薄れる
2. **同じペアが連続表示される可能性** — 体験の多様性が損なわれる

### 解決策

**スマートシャッフルアルゴリズムの導入**

1. 最初は常に**最新のペア**を表示
2. 残りはシャッフルしてキューに格納
3. 全て表示し終わったら再シャッフル（一周保証）

```
表示順序イメージ:
[最新] → [ランダムA] → [ランダムB] → [ランダムC] → ... → [全表示完了]
                                                            ↓
                                                    [再シャッフル]
                                                            ↓
[最新] → [新ランダム順] → ...
```

### 実装方法

#### `QuoteCardDisplay.cs` の変更

```csharp
// 追加フィールド
private List<MessagePairData> shuffledQueue;
private int currentQueueIndex = 0;
private bool isFirstDisplay = true;

// LoadMessagesComplete後に呼び出し
private void InitializeSmartShuffle()
{
    if (messagePairs == null || messagePairs.Count == 0) return;
    
    // 日時でソート（最新が先頭）
    var sorted = messagePairs.OrderByDescending(p => p.timestamp).ToList();
    
    // 最新を除いた残りをシャッフル
    var rest = sorted.Skip(1).ToList();
    ShuffleList(rest);
    
    // キュー構築：最新 + シャッフル済み
    shuffledQueue = new List<MessagePairData> { sorted[0] };
    shuffledQueue.AddRange(rest);
    
    currentQueueIndex = 0;
}

private void ShuffleList<T>(List<T> list)
{
    // Fisher-Yates シャッフル
    for (int i = list.Count - 1; i > 0; i--)
    {
        int j = UnityEngine.Random.Range(0, i + 1);
        (list[i], list[j]) = (list[j], list[i]);
    }
}

private MessagePairData GetNextPair()
{
    if (shuffledQueue == null || shuffledQueue.Count == 0)
        return null;
    
    var pair = shuffledQueue[currentQueueIndex];
    currentQueueIndex++;
    
    // 一周したら再シャッフル
    if (currentQueueIndex >= shuffledQueue.Count)
    {
        InitializeSmartShuffle();
    }
    
    return pair;
}
```

#### タイムスタンプの追加

`MessagePairs.json` に保存時刻を追加する必要があります：

```json
[
  {
    "image": "camera_001.png",
    "message": "セリフ内容",
    "credit": "by キャラ名",
    "timestamp": "2026-02-03T17:30:00"
  }
]
```

##### `main_vision_voice.py` の `_save_message_pair()` 修正

```python
from datetime import datetime

def _save_message_pair(image_path, message, credit):
    pair = {
        "image": os.path.basename(image_path),
        "message": message,
        "credit": credit,
        "timestamp": datetime.now().isoformat()
    }
    # ... 既存の保存処理
```

#### 変更が必要なファイル

| ファイル | 変更内容 |
|:---|:---|
| `QuoteCardDisplay.cs` | スマートシャッフルロジック全面改修 |
| `MessagePairData.cs` | timestamp フィールド追加 |
| `main_vision_voice.py` | JSON保存時に timestamp 追加 |

---

## 実装優先度と難易度

| 機能 | 優先度 | 難易度 | 工数目安 |
|:---|:---|:---|:---|
| 3. スマートランダム | 高 | 低 | 1時間 |
| 1. 信頼度による抽象名称 | 中 | 中 | 2〜3時間 |
| 2. 先行表示 | 中 | 中 | 2時間 |

> **推奨**: 機能3 → 機能1 → 機能2 の順で実装

---

## 関連ドキュメント

- [CSharpScriptLogic.md](./CSharpScriptLogic.md) — Unity側スクリプト詳細
- [PythonScriptLogic.md](./PythonScriptLogic.md) — Python側スクリプト詳細
- [WorkflowDiagram.md](./WorkflowDiagram.md) — システムワークフロー図
