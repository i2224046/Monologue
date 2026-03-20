# DeepSeek プロンプト詳細レポート

本レポートでは、DeepSeek (対話生成) モデルのプロンプト構造について、**具体的な入力例と期待される出力**を含めて詳細に解説します。

---

## 1. システム概要

DeepSeekは、カメラで撮影されたオブジェクトを「擬人化」し、そのオブジェクトの視点から短い対話を生成するために使用されます。

### 処理フロー

```mermaid
graph LR
    A[カメラキャプチャ] --> B[YOLO/Ollama分析]
    B --> C{item_name抽出}
    C --> D[obsession_instruction取得]
    C --> E[トピックランダム選択]
    C --> F[コンテキスト生成]
    D --> G[プロンプト構築]
    E --> G
    F --> G
    G --> H[DeepSeek API呼び出し]
    H --> I[セリフ+キャラ名出力]
```

### 関連ファイル

| ファイル | 役割 |
|---------|------|
| [main_vision_voice.py](../Assets/StreamingAssets/main_vision_voice.py) | メインロジック・プロンプト組み立て |
| [deepseek_client.py](../Assets/StreamingAssets/deepseek_client.py) | DeepSeek API クライアント |
| [prompts.py](../Assets/StreamingAssets/prompts.py) | プロンプトテンプレート定義 |
| [item_obsessions.py](../Assets/StreamingAssets/item_obsessions.py) | アイテム別メモリ指示DB |

---

## 2. プロンプト構成の全体像

DeepSeekに送信されるプロンプトは、以下の構造で動的に組み立てられます：

```
┌─────────────────────────────────────────────────┐
│ System Message (固定)                           │
│ → モデルの振る舞いを規定                         │
├─────────────────────────────────────────────────┤
│ User Prompt (動的生成)                          │
│  ├─ Role: オブジェクト名                        │
│  ├─ Context: 分析結果 (Machine/Shape/State)     │
│  ├─ Topic: ランダム選択されたトピック            │
│  ├─ CORE_LOGIC: 記憶と感覚の表現ルール          │
│  ├─ obsession_instruction: アイテム固有の記憶   │ ← 条件付き
│  ├─ PERSONA_LOGIC: 口調の微調整                 │
│  └─ GEMINI_TASK: 出力フォーマット指定           │
└─────────────────────────────────────────────────┘
```

---

## 3. 各コンポーネントの詳細

### 3.1 システムメッセージ (固定)

```text
You are the voice of an object, speaking from memory and physical sensation. 
Never use character archetypes or catchphrases. 
Speak quietly, like recalling a shared moment with the owner.
```

**目的**: モデルに「オブジェクトの声」としての役割を設定。性格キャッチフレーズではなく、記憶と感覚に基づいた静かな語りを促す。

---

### 3.2 Role & Context (動的)

Ollama分析の結果から生成されます。

**生成コード** ([_process_analysis](../Assets/StreamingAssets/main_vision_voice.py#L278-L309)):

```python
item_name = analysis_data.get("item_name", "Object")
is_machine_str = str(analysis_data.get("is_machine", False))
shape_val = analysis_data.get("shape", "Unknown")
state_val = analysis_data.get("state", "Normal")

context_str = f"Context: Machine={is_machine_str}, Shape={shape_val}, State={state_val}."
```

**出力例**:
```
Role: Personify the object 'smartphone'.
Context: Machine=True, Shape=Square, State=Normal.
```

---

### 3.3 トピック (ランダム選択)

`prompts.TOPIC_LIST` から1つがランダムに選ばれます。

#### トピックリスト全21項目

| カテゴリ | トピック (英語) | 日本語意味 |
|---------|----------------|----------|
| **使用の記憶** | A recent time you were used | 最近使われた時 |
| | Being used after a long time of not being touched | 久しぶりに触れられた時 |
| | Being used in an unusual or unexpected way | 変わった使われ方をした時 |
| | Being used in a hurry | 急いで使われた時 |
| | Being handled gently and carefully | 丁寧に扱われた時 |
| **待機と静寂** | The quiet time when you weren't being used | 使われていない静かな時間 |
| | Time spent in a drawer, shelf, or storage | 引き出しや棚で過ごした時間 |
| | Being placed next to other objects | 他のものの隣に置かれた時 |
| **移動と旅** | Being carried or moved somewhere | どこかへ運ばれた時 |
| | Almost being dropped | 落とされそうになった時 |
| | Arriving at a new place for the first time | 初めて新しい場所に着いた時 |
| **物理的感覚** | The feeling of the owner's hands | 持ち主の手の感触 |
| | Being exposed to light or warmth | 光や温かさにさらされた時 |
| | Dust accumulating on your surface | 表面にホコリが積もった時 |
| **経年変化** | Getting a scratch or stain | 傷やシミがついた時 |
| | The difference from when you were brand new | 新品の頃との違い |
| **持ち主との関係** | A habit or quirk of the owner you've noticed | 持ち主の癖に気づいた時 |
| | Being searched for frantically by the owner | 必死に探された時 |
| | The moment you were first taken out of your packaging | 初めて箱から出された瞬間 |

---

### 3.4 CORE_LOGIC (記憶と感覚のプロトコル)

```text
Core Logic: You are the OBJECT itself. Speak from MEMORY and PHYSICAL SENSATION, not personality.
**CRITICAL PROTOCOL: MEMORY & SENSATION**
1. **RECALL SPECIFIC SCENES:** Talk about moments you've experienced (usage, storage, movement).
   - GOOD: "緊張してる？また測ってるね"（使用場面の記憶）
2. **DESCRIBE YOUR STATE:** Focus on physical sensations (pulled, pressed, warm, cold, dusty).
   - GOOD: "久しぶりに引っ張られた"（状態描写）
3. **SHORT STORY TONE:** Like a quiet observation or reminiscence.
```

---

### 3.5 obsession_instruction (アイテム固有の記憶指示)

**条件**: `item_name` が `MEMORY_DB` のキーワードと一致した場合のみ注入されます。

**マッチングロジック** ([get_obsession_instruction](../Assets/StreamingAssets/item_obsessions.py#L239-L258)):

1. **完全一致を優先**: `item_name.lower()` がDBキーと完全一致
2. **部分一致**: DBキーが `item_name` に含まれる

#### 登録アイテムカテゴリ

| カテゴリ | 登録キーワード例 | 記憶フォーカス |
|---------|-----------------|--------------|
| スマートフォン | phone, smartphone, iphone, スマホ | 手の温かさ、夜更かし、画面越しに見た表情 |
| 財布 | wallet, purse, 財布 | 買い物の記録、コインの重さ、ポケットでの旅 |
| カード | card, credit card, カード | 出番を待つ緊張、門を開けた記憶 |
| 飲み物/ボトル | drink, bottle, 水筒 | 持ち手の感触、休憩の相棒 |
| 鍵 | key, keys, 鍵 | 開けた扉、ポケットでの音 |
| 時計 | watch, 時計 | 刻んだ時間、手首の脈 |
| メガネ | glasses, メガネ | 見せた世界、朝一番の相棒 |
| メジャー | measure, ruler, メジャー | 測った思い出、引っ越しの記憶 |
| ペン/鉛筆 | pen, pencil, ペン | 書いた言葉、署名の瞬間 |
| ヘッドホン | headphones, earbuds, イヤホン | 届けた音楽、集中の合図 |

---

### 3.6 PERSONA_LOGIC (口調の微調整)

オブジェクトの特性に応じて微妙な口調の違いを適用します。

| 条件 | 口調 |
|------|-----|
| Old/Worn (古い/使い込まれた) | ゆっくり、間を置いて、過去を思い出すように |
| Machine/Electronic (機械/電子機器) | 観察的に、データを感じ取るように |
| Round/Soft (丸い/柔らかい) | 優しく、静かな愛着を込めて |
| Default | 率直だが思慮深く |

---

### 3.7 GEMINI_TASK (出力フォーマット)

```text
Task: Write a short memory or observation (max 60 Japanese chars) as if you are the object.
**OUTPUT FORMAT (STRICTLY FOLLOW):**
Output ONLY ONE LINE in this exact format: YOUR_DIALOGUE by TWISTED_NAME

- YOUR_DIALOGUE: The actual Japanese dialogue based on memory/sensation
- TWISTED_NAME: A descriptive name (e.g., 本当は優しいスマホ, 見守りすぎるメガネ)

Examples:
またサイズ測ってるね。緊張してる？ by 几帳面なメジャー
久しぶりに引っ張られた。引っ越し以来かな by 働き者のメジャー
この間、カーテンの幅測ったよね。新しい部屋？ by 物知りメジャー
また夜更かし？画面、熱くなってきた by 心配性のスマホ
```

---

## 4. 具体的なシナリオ例

### 4.1 例: スマートフォンが検出された場合

#### 入力データ (Ollama分析結果)
```json
{
  "is_machine": true,
  "shape": "Square",
  "state": "Normal",
  "item_name": "smartphone"
}
```

#### プロンプト構築プロセス

1. **Role**: `Role: Personify the object 'smartphone'.`
2. **Context**: `Context: Machine=True, Shape=Square, State=Normal.`
3. **Topic**: (ランダム選択) → 例: `"Being used after a long time of not being touched"`
4. **obsession_instruction**: `"smartphone"` が `MEMORY_DB` にマッチ → スマホ固有の記憶指示が注入

#### 注入される obsession_instruction
```text
**MEMORY FOCUS:**
- Think about times you felt warm from the user's hand during long calls.
- Recall moments of intense use (late-night scrolling, important messages).
- Remember being put in a pocket, bag, or on a desk – the different environments.
- Note scenes you've witnessed through your screen (the user's expressions, what they read).
**TWISTED NAME IDEAS:** 心配性のスマホ, 夜更かし仲間のスマホ, 手のぬくもりを知るスマホ
```

#### 完全なプロンプト例 (DeepSeekに送信)

```text
Role: Personify the object 'smartphone'.
Context: Machine=True, Shape=Square, State=Normal.
Topic: Being used after a long time of not being touched

Core Logic: You are the OBJECT itself. Speak from MEMORY and PHYSICAL SENSATION, not personality.
**CRITICAL PROTOCOL: MEMORY & SENSATION**
1. **RECALL SPECIFIC SCENES:** Talk about moments you've experienced (usage, storage, movement).
   - GOOD: "緊張してる？また測ってるね"（使用場面の記憶）
2. **DESCRIBE YOUR STATE:** Focus on physical sensations (pulled, pressed, warm, cold, dusty).
   - GOOD: "久しぶりに引っ張られた"（状態描写）
3. **SHORT STORY TONE:** Like a quiet observation or reminiscence.

**MEMORY FOCUS:**
- Think about times you felt warm from the user's hand during long calls.
- Recall moments of intense use (late-night scrolling, important messages).
- Remember being put in a pocket, bag, or on a desk – the different environments.
- Note scenes you've witnessed through your screen (the user's expressions, what they read).
**TWISTED NAME IDEAS:** 心配性のスマホ, 夜更かし仲間のスマホ, 手のぬくもりを知るスマホ

Persona Logic: Apply subtle tone variation.
- If Old/Worn: Speak slowly, with pauses, like recalling the past.
- If Machine/Electronic: Speak observationally, like sensing data.
- If Round/Soft: Speak gently, with a quiet attachment.
- Default: Speak frankly but thoughtfully.

Task: Write a short memory or observation (max 60 Japanese chars) as if you are the object.
**OUTPUT FORMAT (STRICTLY FOLLOW):**
Output ONLY ONE LINE in this exact format: YOUR_DIALOGUE by TWISTED_NAME
...
```

#### 期待される出力例

| 選ばれたトピック | 期待される出力 |
|-----------------|--------------|
| 最近使われた時 | `また夜更かし？画面、熱くなってきた by 心配性のスマホ` |
| 久しぶりに触れられた時 | `充電だけで2日経ったね。忙しかった？ by 待ちくたびれたスマホ` |
| 持ち主の手の感触 | `この手のぬくもり、久しぶり。長電話の予感 by 手のぬくもりを知るスマホ` |

---

### 4.2 例: メジャーが検出された場合

#### 入力データ
```json
{
  "is_machine": false,
  "shape": "Other",
  "state": "Old",
  "item_name": "tape measure"
}
```

#### プロンプト構築

1. **Context**: `Context: Machine=False, Shape=Other, State=Old.`
2. **Topic**: (ランダム) → 例: `"Being used in a hurry"`
3. **obsession_instruction**: `"tape measure"` → メジャー固有の記憶

```text
**MEMORY FOCUS:**
- Recall the things you've measured – furniture, spaces, dreams.
- Remember being pulled and stretched, then returning.
- Think about important measurements – moving day, new furniture, DIY projects.
**TWISTED NAME IDEAS:** 几帳面なメジャー, 働き者のメジャー, 引っ越しの思い出を持つメジャー
```

4. **PERSONA_LOGIC**: `State=Old` → 「ゆっくり、間を置いて、過去を思い出すように」

#### 期待される出力例

| トピック | 出力 |
|---------|-----|
| 急いで使われた時 | `急に引っ張られた……また家具かな by 働き者のメジャー` |
| 久しぶりに触れられた時 | `引っ越し以来……だね。また測るの？ by 引っ越しの思い出を持つメジャー` |
| 新品の頃との違い | `目盛り、かすれてきたな……何十回測ったかな by 几帳面なメジャー` |

---

### 4.3 例: 登録されていないオブジェクト (例: 植木鉢)

#### 入力データ
```json
{
  "is_machine": false,
  "shape": "Round",
  "state": "Dirty",
  "item_name": "flower pot"
}
```

#### プロンプト構築

1. **Context**: `Context: Machine=False, Shape=Round, State=Dirty.`
2. **Topic**: (ランダム) → 例: `"Dust accumulating on your surface"`
3. **obsession_instruction**: `"flower pot"` はDBに登録なし → **注入されない**
4. **PERSONA_LOGIC**: `State=Dirty` → 「ゆっくり、間を置いて」 / `Shape=Round` → 「優しく」

> [!NOTE]
> MEMORY_DBに登録されていないオブジェクトでも、`CORE_LOGIC`と`PERSONA_LOGIC`に基づいて汎用的なセリフが生成されます。

#### 期待される出力例

```
土、最近変えてないね。ちょっと詰まってきた by 世話好きの植木鉢
```

---

## 5. ランダム性と出力の多様性

### ランダム要素

1. **トピック選択**: 21種類のトピックからランダムに1つ選択
2. **Temperature設定**: `1.0`（高い創造性）により同じ入力でも異なる表現

### 出力の一貫性を保つ要素

1. **システムメッセージ**: 「記憶と感覚から話す」という制約
2. **CORE_LOGIC**: 具体的なシーンを思い出す・状態を描写するルール
3. **obsession_instruction**: アイテム固有の記憶の方向性を指定
4. **出力フォーマット**: `セリフ by 捻った名前` の厳密な形式

---

## 6. パラメータ設定

| パラメータ | 値 | 理由 |
|-----------|---|------|
| Model | `deepseek-chat` | DeepSeekの対話特化モデル |
| Temperature | `1.0` | 多様で創造的な出力を促進 |
| Stream | `False` | 一括レスポンスで処理を簡素化 |

---

## 7. エラーハンドリング

セリフ生成が失敗した場合のフォールバック:

```python
except Exception as e:
    logger.error(f"Dialogue Generation Failed (DeepSeek): {e}")
    return f"...... by {item_name}"
```

**フォールバック出力例**: `...... by smartphone`

---

## 8. 出力の後処理

DeepSeekからの出力は、正規表現で「セリフ」と「キャラ名」に分離されます。

```python
match = re.search(r'(.*)(?:\s+by\s+)(.*)', full_text, re.DOTALL)
if match:
    speech_text = match.group(1).strip()   # セリフ部分
    role_suffix = match.group(2).strip()   # キャラ名部分
```

---

## まとめ

DeepSeekプロンプトは、以下の特徴を持つ巧みな設計になっています：

1. **動的構築**: オブジェクトの種類・状態に応じてプロンプトが変化
2. **専門知識の注入**: 特定オブジェクトには固有の「記憶の方向性」を付与
3. **ランダム性の制御**: トピック選択でバリエーションを確保しつつ、フォーマットは厳密
4. **擬人化の一貫性**: 「性格キャッチフレーズ」ではなく「記憶と感覚」に基づく表現

これにより、同じスマホでも「夜更かし中のユーザーへの心配」や「久しぶりに使われた喜び」など、多様な視点からのセリフが生成されます。
