# プロジェクティブ・ナラティブ導入計画

ユーザーから提示された「プロジェクティブ（投影的）・ナラティブ」の方針を、現在のシステムに統合するための設計案です。

## 1. 基本方針の統合 (Core Integration)

現在の「Specific Vague (具体的に聞こえる曖昧さ)」を、より詩的・感覚的な「Projective Narrative (投影的ナラティブ)」へ昇華させます。

| 項目 | 旧方針 (Specific Vague) | **新方針 (Projective Narrative)** |
| :--- | :--- | :--- |
| **物理現象** | 「重くなったな」 (比喩的) | **「変化」や「気配」への置換**<br>NG: 「熱い」<br>OK: 「驚くほど熱くなる瞬間がある」 (感情か物理か曖昧に) |
| **時間表現** | "That day" (あの日) | **相対的・主観的時間**<br>NG: 「昨日」<br>OK: 「またこの季節が巡ってきた」 (ユーザーの記憶に依存) |
| **画像認識** | 年齢・性別で関係性を推測 | **「傷・使用感」を「性格 (Tone)」へ変換**<br>傷多 = 達観・親密<br>新品 = 好奇心・初々しい |

---

## 2. 実装への変更点

### A. `ollamaRun.py` の画像認識ロジック変更
これまでは「年齢・性別」を見ていましたが、新方針では**「物体の状態（傷、汚れ、新しさ）」**を重視し、それをプロンプトの `Tone` 指示に変換します。

*   **変更前:** `user_appearance` (年齢・性別) を取得
*   **変更後:** `item_condition` (傷、汚れ、使用感) を取得し、以下のパラメータに変換してプロンプトに注入する。
    *   `condition_score` (1: 新品 ～ 5: ボロボロ)
    *   **Score 1-2 (新品):** Tone = "Fresh, Polite, Curious" (初々しい、丁寧)
    *   **Score 3 (普通):** Tone = "Friendly, Casual" (親しみやすい)
    *   **Score 4-5 (古びた):** Tone = "Wise, Intimate, Nostalgic" (達観、親密、懐古的)

### B. `config.json` のプロンプト更新 (Oracle Protocol v2)
各キャラクターの `CORE RULES` を、新方針に合わせて書き換えます。

#### 共通ルール (Universal Rules)
1.  **Abstract the Physical (物理の抽象化):**
    *   Do not say "Hot", "Cold", "Heavy".
    *   Use "Intensity", "Temperature of emotion", "Presence".
2.  **Relativize Time (時間の相対化):**
    *   Do not say "Yesterday", "Recently".
    *   Use "The season returns", "The cycle repeats", "Since we met".
3.  **Projective Questioning (投影的問いかけ):**
    *   Ask open questions that force the user to search their memory.
    *   "Do you remember the temperature of that day?"

#### キャラクター別適用例 (The External Brain - Smartphone)
*   **Old (傷あり):**
    *   「画面の傷が増えるたび、君の『迷い』も刻まれている気がするよ。……それとも、決意の証かな？」
*   **New (新品):**
    *   「まだ私の知らない君がたくさんいるね。……これからどんな『熱』を共有できるのかな？」

---

## 3. 作業手順

1.  **`ollamaRun.py` の修正:**
    *   画像分析プロンプトを「年齢・性別」から「物体の使用感 (Condition)」へ変更。
    *   使用感に基づいて `Tone` を動的に決定するロジックを追加。
2.  **`config.json` の修正:**
    *   `CORE RULES` を「物理の抽象化」「時間の相対化」に書き換え。
    *   各キャラクターの例文 (Few-Shot) を、より抽象的で詩的なものに差し替え。

この方針で実装を進めてよろしいでしょうか？
