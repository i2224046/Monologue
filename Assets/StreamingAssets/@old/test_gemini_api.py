import os
import google.generativeai as genai
import json

from dotenv import load_dotenv

# --- 設定 ---
# .envファイルから環境変数を読み込む
load_dotenv()

# 1. Google AI Studio (https://aistudio.google.com/) でAPIキーを取得してください。
# 2. .envファイルを作成し、GEMINI_API_KEY="あなたのキー" を記述してください。
API_KEY = os.getenv("GEMINI_API_KEY")

if not API_KEY:
    print("Error: API Keyが見つかりません。.envファイルを作成し、GEMINI_API_KEYを設定してください。")
    # フォールバック（テスト用）
    # API_KEY = "YOUR_API_KEY_HERE" 


# モデル名 (最新のFlashモデルなどを指定)
MODEL_NAME = "gemini-flash-latest" 

def test_dialogue_generation():
    if API_KEY == "YOUR_API_KEY_HERE":
        print("Error: API Keyが設定されていません。スクリプト内の API_KEY を書き換えるか、環境変数を設定してください。")
        return

    genai.configure(api_key=API_KEY)
    model = genai.GenerativeModel(MODEL_NAME)

    # --- テスト用データ (ollamaRun.pyの画像分析結果を模倣) ---
    # 実際には画像分析から得られるデータ
    mock_analysis_data = {
        "description": "A vintage leather wallet resting on a wooden table, slightly worn at the edges.",
        "item_name": "Leather Wallet",
        "item_condition": "Worn edges, soft leather, well-used",
        "condition_score": 4,
        "personality_id": "gatekeeper" # 財布
    }
    
    # 心理的トリガー (コールドリーディング)
    mock_trigger = "You are currently struggling with a choice between stability and a new challenge."
    
    # 性格プロンプト (config.jsonから取得する想定)
    # ここでは gatekeeper (財布) の例
    personality_prompt = """
    You are a 'Gatekeeper'. You manage the user's resources and boundaries.
    You are serious, protective, and sometimes strict about spending.
    """

    # トーン設定
    tone_instruction = "Tone: Wise, Intimate, Nostalgic (Old Item). Act as if you have known the user for years."

    # --- プロンプト構築 ---
    prompt = f"""
    {personality_prompt}

    # Context Data
    Visual Context: {mock_analysis_data['description']}
    Item Condition: {mock_analysis_data['item_condition']}
    {tone_instruction}

    # Psychological Trigger (Hidden Theme)
    {mock_trigger}

    # Task
    1. Combine the "Visual Context" with the "Trigger".
    2. Perform a "Cold Reading" using the "Specific Vague" technique.
    3. Output a short Japanese spoken line (Max 60 chars).
    
    Output strict JSON format:
    {{
      "thought_process": "思考プロセス（日本語）: 視覚情報とトリガーをどう結びつけたか",
      "dialogue": "日本語のセリフ"
    }}
    """

    print(f"--- Sending Request to {MODEL_NAME} ---")
    print(f"Input Context: {mock_analysis_data['item_name']} ({mock_analysis_data['item_condition']})")
    
    try:
        response = model.generate_content(
            prompt,
            generation_config=genai.GenerationConfig(
                response_mime_type="application/json",
                temperature=0.8
            )
        )
        
        print("\n--- Response ---")
        print(response.text)
        
        # JSONパース確認
        try:
            data = json.loads(response.text)
            print(f"\n[Extracted Dialogue]: {data.get('dialogue')}")
        except json.JSONDecodeError:
            print("\n[Error] JSON parsing failed.")

    except Exception as e:
        print(f"\n[Error] API Request failed: {e}")

if __name__ == "__main__":
    test_dialogue_generation()
