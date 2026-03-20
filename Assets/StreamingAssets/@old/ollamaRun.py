import ollama
import base64
import time
import os
import json
import requests
import sys
import random
import re
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

# --- 設定: モデル ---
MODEL_NAME = 'qwen3-vl:8b'
WATCHED_EXTENSIONS = {'.jpg', '.jpeg', '.png'}

# --- 設定: 音声合成 API ---
API_SERVER = "http://127.0.0.1:50032"

# --- 設定: 音声ライブラリ & プロンプト (Load from config.json) ---
CONFIG_FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "config.json")

def load_config():
    try:
        with open(CONFIG_FILE, 'r', encoding='utf-8') as f:
            return json.load(f)
    except Exception as e:
        print(f"[[ERROR]] Failed to load config.json: {e}", flush=True)
        return {}

config_data = load_config()

VOICE_VARIANTS = config_data.get("VOICE_VARIANTS", {})
PERSONALITY_PROMPTS = config_data.get("PERSONALITY_PROMPTS", {})
PSYCHOLOGICAL_TRIGGERS = config_data.get("PSYCHOLOGICAL_TRIGGERS", [])

# --- パス設定 ---
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
CAPTURE_DIR = os.path.join(SCRIPT_DIR, "capture")
VOICE_DIR = os.path.join(SCRIPT_DIR, "voice")

os.makedirs(CAPTURE_DIR, exist_ok=True)
os.makedirs(VOICE_DIR, exist_ok=True)

# --- 関数群 ---

def synthesis(text: str, speaker_uuid: str, style_id: int):
    if not text or len(text) == 0:
        print("[[ERROR]] TTS: Input text is empty. Skipping.", flush=True)
        return None

    # COEIROINK API仕様準拠: 2段階処理
    prosody_payload = {"text": text}
    print(f"[[DEBUG_TTS]] Step 1: Estimating Prosody for '{text[:15]}...'", flush=True)
    
    try:
        r1 = requests.post(
            f"{API_SERVER}/v1/estimate_prosody", 
            json=prosody_payload,
            timeout=10
        )
        r1.raise_for_status()
        prosody_data = r1.json()
        
        if 'detail' not in prosody_data:
            print(f"[[ERROR]] TTS Step 1 Failed: No 'detail' in response", flush=True)
            return None
            
    except Exception as e:
        print(f"[[ERROR]] TTS Step 1 (Prosody) Error: {e}", flush=True)
        return None

    query = {
        "speakerUuid": speaker_uuid,
        "styleId": style_id,
        "text": text,
        "speedScale": 1.0,
        "volumeScale": 1.0,
        "prosodyDetail": prosody_data['detail'],
        "pitchScale": 0.0,
        "intonationScale": 1.0,
        "prePhonemeLength": 0.1,
        "postPhonemeLength": 0.5,
        "outputSamplingRate": 24000,
    }
    
    print(f"[[DEBUG_TTS]] Step 2: Synthesizing...", flush=True)
    try:
        response = requests.post(
            f"{API_SERVER}/v1/synthesis",
            headers={"Content-Type": "application/json"},
            data=json.dumps(query),
            timeout=20
        )
        response.raise_for_status()
        print(f"[[DEBUG_TTS]] Success. Size: {len(response.content)} bytes", flush=True)
        return response.content
    except Exception as e:
        print(f"[[ERROR]] TTS Step 2 (Synthesis) Failed: {e}", flush=True)
        if hasattr(e, 'response') and e.response is not None:
             print(f"[[ERROR]] Server Msg: {e.response.text}", flush=True)
        return None

def extract_json(text):
    try:
        match = re.search(r'```json\s*(\{.*?\})\s*```', text, re.DOTALL)
        if match: return json.loads(match.group(1))
        match = re.search(r'\{.*\}', text, re.DOTALL)
        if match: return json.loads(match.group(0))
        return None
    except Exception:
        return None

# --- メイン処理 ---
def process_image(image_path):
    if os.path.basename(image_path).startswith('.'):
        return

    try:
        time.sleep(1.0)
        filename = os.path.basename(image_path)
        print(f"[[STATE_START]] Processing {filename}", flush=True)
        
        # 1. 画像解析
        with open(image_path, "rb") as f:
            image_data = base64.b64encode(f.read()).decode("utf-8")

        analysis_prompt = """
Describe the main object and the user in the image in detail.
Then, based on the object's condition and type, assign a "Personality ID".

Personality IDs:
- "external_brain": Smartphone, Watch, Tablet.
- "lifeline": Bottle, Drink, Food, Medicine.
- "gatekeeper": Wallet, Key, Money, Card.
- "muse": Pen, Notebook, Camera, Laptop.
- "sanctuary": Earphone, Plush, Handkerchief.
- "mask": Cosmetic, Mirror, Accessory, Glasses.
- "observer": Others.

Output strict JSON:
{
  "description": "Short description of visual details",
  "item_name": "Name of object",
  "item_condition": "Description of scratches, dirt, usage, or newness",
  "condition_score": "1 (New/Clean) to 5 (Old/Damaged)",
  "personality_id": "ID"
}
"""
        analysis_response = ollama.chat(
            model=MODEL_NAME,
            messages=[{
                "role": "user",
                "content": analysis_prompt,
                "images": [image_data]
            }],
            options={"temperature": 0.2}
        )
        
        analysis_data = extract_json(analysis_response['message']['content'])
        if not analysis_data:
            print("[[WARNING]] Analysis JSON Failed. Using default.")
            analysis_data = {"item_name": "Object", "description": "Unknown", "item_condition": "Unknown", "condition_score": 3, "personality_id": "observer"}
        
        # Determine Tone from Condition
        try:
            score = int(analysis_data.get("condition_score", 3))
        except:
            score = 3
            
        tone_instruction = ""
        if score <= 2:
            tone_instruction = "Tone: Fresh, Polite, Curious (New Item). Act as if you just met the world."
        elif score >= 4:
            tone_instruction = "Tone: Wise, Intimate, Nostalgic (Old Item). Act as if you have known the user for years."
        else:
            tone_instruction = "Tone: Friendly, Casual (Used Item). Act as a reliable partner."
        
        # Mapping Logic
        pid = analysis_data.get("personality_id", "observer")
        item_name = analysis_data.get("item_name", "Object")
        item_lower = item_name.lower()
        
        # Keyword Fallback / Override
        if any(x in item_lower for x in ['phone', 'smart', 'watch', 'tablet', 'screen']): pid = 'external_brain'
        elif any(x in item_lower for x in ['bottle', 'water', 'drink', 'food', 'snack', 'candy', 'medicine']): pid = 'lifeline'
        elif any(x in item_lower for x in ['wallet', 'key', 'card', 'money', 'coin', 'purse']): pid = 'gatekeeper'
        elif any(x in item_lower for x in ['pen', 'pencil', 'note', 'book', 'laptop', 'camera']): pid = 'muse'
        elif any(x in item_lower for x in ['earphone', 'headphone', 'plush', 'toy', 'tissue', 'handkerchief', 'cigarette']): pid = 'sanctuary'
        elif any(x in item_lower for x in ['cosmetic', 'makeup', 'mirror', 'glass', 'ring', 'necklace', 'jewelry']): pid = 'mask'
        
        if pid not in VOICE_VARIANTS: pid = "observer"
        voice_settings = random.choice(VOICE_VARIANTS[pid])
        
        print(f"[[CREDIT]] COERIOINK: {voice_settings['name']}", flush=True)

        # 2. セリフ生成
        trigger = random.choice(PSYCHOLOGICAL_TRIGGERS)
        prompt_text = PERSONALITY_PROMPTS[pid]
        
        # 【修正】 situation変数エラーを解消し、心理的トリガーを使用する構造へ統合
        dialogue_prompt = f"""
{prompt_text}

# Context Data
Visual Context: {analysis_data['description']}
Item Condition: {analysis_data['item_condition']}
{tone_instruction}

# Psychological Trigger (Hidden Theme)
{trigger}

# Task
1. Combine the "Visual Context" with the "Trigger".
2. Perform a "Cold Reading" using the "Specific Vague" technique.
3. **STRICTLY** follow the CORE RULES defined above.
4. Output a short Japanese spoken line (Max 60 chars).

Output strict JSON format:
{{
  "thought_process": "How to connect visual '{analysis_data['description']}' with trigger '{trigger}'",
  "dialogue": "Japanese spoken line"
}}
"""
        speech_response = ollama.chat(
            model=MODEL_NAME,
            messages=[{"role": "user", "content": dialogue_prompt}],
            options={"temperature": 0.8, "top_p": 0.9}
        )
        
        raw_text = speech_response['message']['content']
        
        speech_text = ""
        res_json = extract_json(raw_text)
        
        if res_json and "dialogue" in res_json:
            speech_text = res_json["dialogue"]
        else:
            match = re.search(r'"dialogue":\s*"(.*?)"', raw_text)
            if match:
                speech_text = match.group(1)
            else:
                print("[[WARNING]] Dialogue extraction failed!", flush=True)
                speech_text = "..." 

        speech_text = re.sub(r'["「」]', '', speech_text).strip()
        print(f"[[MESSAGE]] {speech_text}", flush=True)

        if len(speech_text) < 2:
            print("[[ERROR]] Speech text too short. Skipping TTS.", flush=True)
            return

        # 3. 音声合成 & 保存
        audio_data = synthesis(speech_text, voice_settings['uuid'], voice_settings['style'])

        if audio_data:
            base_name = os.path.splitext(filename)[0]
            wav_filename = f"{base_name}.wav"
            wav_path = os.path.join(VOICE_DIR, wav_filename)
            
            try:
                with open(wav_path, "wb") as f:
                    f.write(audio_data)
                print(f"[[STATE_COMPLETE]] Saved to {wav_filename}", flush=True)
            except Exception as e:
                print(f"[[ERROR]] File Write Failed: {e}", flush=True)
        else:
            print("[[STATE_COMPLETE]] Audio Gen Failed", flush=True)

    except Exception as e:
        print(f"[[ERROR]] Processing Failed: {e}", flush=True)
        import traceback
        traceback.print_exc()

# --- 監視クラス ---
class ImageHandler(FileSystemEventHandler):
    def on_created(self, event):
        if event.is_directory: return
        filename = os.path.basename(event.src_path)
        ext = os.path.splitext(filename)[1].lower()
        if ext in WATCHED_EXTENSIONS:
            time.sleep(1.0) 
            process_image(event.src_path)

if __name__ == "__main__":
    print("--- AI Object Voice System (v5.2: Psychological + TTS Fix) ---")
    print(f"[[SYSTEM]] Monitoring: {CAPTURE_DIR}", flush=True)
    
    event_handler = ImageHandler()
    observer = Observer()
    observer.schedule(event_handler, CAPTURE_DIR, recursive=False)
    observer.start()

    try:
        while True: time.sleep(1)
    except KeyboardInterrupt:
        observer.stop()
    observer.join()