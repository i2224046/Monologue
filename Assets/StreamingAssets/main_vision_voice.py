import time
import os
import json
import random
import logging
import threading
import sys
import cv2
import numpy as np
from datetime import datetime
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

# Import Clients
from ollama_client import OllamaClient
from deepseek_client import DeepSeekClient
# from voice_client import VoiceClient  # TTS無効化
from camera_capture import CameraCapture
from yolo_processor import YOLOProcessor
from rembg import remove
import item_obsessions
from category_mapping import get_display_name

# --- Configuration & Constants ---
WATCHED_EXTENSIONS = {'.jpg', '.jpeg', '.png'}
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
CAPTURE_DIR = os.path.join(SCRIPT_DIR, "capture")
VOICE_DIR = os.path.join(SCRIPT_DIR, "voice")
CONFIG_FILE = os.path.join(SCRIPT_DIR, "config.json")
MESSAGE_PAIRS_FILE = os.path.join(SCRIPT_DIR, "MessagePairs.json")

# Ensure directories exist
os.makedirs(CAPTURE_DIR, exist_ok=True)
os.makedirs(VOICE_DIR, exist_ok=True)
RAW_CAPTURE_DIR = os.path.join(CAPTURE_DIR, "raw")
os.makedirs(RAW_CAPTURE_DIR, exist_ok=True)

# Configure Logging
import sys
logging.basicConfig(
    level=logging.INFO,
    format='[[%(levelname)s]] %(message)s',
    handlers=[logging.StreamHandler(sys.stdout)] # Explicitly print to stdout for Unity
)
logger = logging.getLogger(__name__)

# --- Load Config ---
def load_config():
    try:
        with open(CONFIG_FILE, 'r', encoding='utf-8') as f:
            return json.load(f)
    except Exception as e:
        logger.error(f"Failed to load config.json: {e}")
        return {}

config_data = load_config()
VOICE_VARIANTS = config_data.get("VOICE_VARIANTS", {})
PERSONALITY_PROMPTS = config_data.get("PERSONALITY_PROMPTS", {})
PSYCHOLOGICAL_TRIGGERS = config_data.get("PSYCHOLOGICAL_TRIGGERS", [])

# --- Initialize Clients ---
try:
    ollama_client = OllamaClient()
    deepseek_client = DeepSeekClient()
    # voice_client = VoiceClient()  # TTS無効化
    camera_capture = CameraCapture()
    yolo_processor = YOLOProcessor()
    logger.info("Clients initialized successfully (Hybrid Mode: YOLO + Ollama + DeepSeek + Camera, TTS disabled).")
except Exception as e:
    logger.critical(f"Failed to initialize clients: {e}")
    exit(1)

# --- Processing Lock (連打防止) ---
processing_lock = threading.Lock()
is_processing = False

# --- Logic Helper Functions ---
# --- Logic Helper Functions ---
def determine_persona(analysis_data):
    """
    Determines persona based on the 5-step priority logic.
    Returns (persona_id, role_name_jp)
    """
    state_str = analysis_data.get("state", "Normal").lower()
    shape_str = analysis_data.get("shape", "Other").lower()
    is_machine = analysis_data.get("is_machine", False)
    
    # 1. Old / Dirty -> Old Man (ご長寿)
    if any(x in state_str for x in ["old", "dirty", "broken"]):
        return "lifeline", "ご長寿" # Mapped to deepest male voice available (Lifeline usually has diverse voices or use specific UUID)

    # 2. Sharp / Machine+Black -> Chuuni (中二病)
    # Note: Color is not currently extracted by Ollama in the new prompt, assuming Shape/Machine is enough or add color check back if needed.
    # For now, using Shape=Sharp OR Machine=True logic slightly loosely for Chuuni if not Old.
    if "sharp" in shape_str:  
        return "gatekeeper", "中二病" # Mapped to cool male voice

    # 3. Machine -> Tsundere (ツンデレ)
    if is_machine:
        return "mask", "ツンデレ" # Mapped to sharp female

    # 4. Round -> Yandere (ヤンデレ)
    if "round" in shape_str:
        return "sanctuary", "ヤンデレ" # Mapped to whisper/soft female

    # 5. Default -> Gal (ギャル)
    return "external_brain", "ギャル" # Mapped to energetic female

def get_voice_uuid(persona_id):
    """
    Maps specific Persona IDs to specific preferred Voice UUIDs/Styles from config.
    """
    # This mapping attempts to pick the best voice from valid config categories
    # "lifeline" (Old) -> 九州そら (Style 685839222 is deep? Actual check needed, defaulting to first valid)
    # "gatekeeper" (Chuuni) -> 剣崎雌雄 (Male)
    # "mask" (Tsundere) -> No.7 (Female)
    # "sanctuary" (Yandere) -> 春日部つむぎ (Soft Female)
    # "external_brain" (Gal) -> 虚音イフ (Female)
    
    variants = VOICE_VARIANTS.get(persona_id, [])
    if not variants:
        # Fallback to any existant
        all_keys = list(VOICE_VARIANTS.keys())
        if all_keys: variants = VOICE_VARIANTS[all_keys[0]]
        
    if variants:
        return random.choice(variants) # Random variation within the persona category
    return None

def _save_message_pair(image_filename, message, credit):
    """
    画像とメッセージのペア情報をMessagePairs.jsonに保存する
    """
    try:
        pairs = []
        if os.path.exists(MESSAGE_PAIRS_FILE):
            with open(MESSAGE_PAIRS_FILE, 'r', encoding='utf-8') as f:
                pairs = json.load(f)
        
        pair_data = {
            "image": image_filename,
            "message": message,
            "credit": credit,
            "timestamp": datetime.now().isoformat()
        }
        pairs.append(pair_data)
        
        # 最新100件に制限
        pairs = pairs[-100:]
        
        with open(MESSAGE_PAIRS_FILE, 'w', encoding='utf-8') as f:
            json.dump(pairs, f, ensure_ascii=False, indent=2)
        
        logger.info(f"[[PAIR_SAVED]] {image_filename} -> {message[:30]}...")
    except Exception as e:
        logger.error(f"Failed to save message pair: {e}")

def process_image(image_path):
    if os.path.basename(image_path).startswith('.'):
        return

    try:
        time.sleep(1.0)
        filename = os.path.basename(image_path)
        logger.info(f"[[STATE_START]] Processing {filename}")
        
        analysis_data = ollama_client.analyze_image(image_path)
        logger.info(f"[[OLLAMA ANALYSIS]] Data: {json.dumps(analysis_data, ensure_ascii=False)}")
        
        _process_analysis(analysis_data, filename)

    except Exception as e:
        logger.error(f"Processing Failed: {e}")
        import traceback
        traceback.print_exc()


def apply_intelligent_brightness(image):
    """
    画像の明るさを自動調整する
    1. ガンマ補正で全体を持ち上げ (gamma=1.5)
    2. 平均輝度が低い場合はさらにベースの明るさを底上げ
    """
    # 1. ガンマ補正（暗部を持ち上げる）
    gamma = 1.5
    invGamma = 1.0 / gamma
    table = np.array([((i / 255.0) ** invGamma) * 255
                      for i in range(256)]).astype("uint8")
    corrected = cv2.LUT(image, table)
    
    # 2. 輝度チェックとベースアップ
    hsv = cv2.cvtColor(corrected, cv2.COLOR_BGR2HSV)
    v = hsv[:, :, 2]
    mean_brightness = np.mean(v)
    
    # 目標輝度（128前後が標準的だが、少し明るめの140を目指す）
    target_brightness = 140.0
    
    if mean_brightness < target_brightness:
        diff = target_brightness - mean_brightness
        # 差分の半分程度を加算して自然に明るくする（最大50程度）
        beta = min(diff * 0.8, 50)
        corrected = cv2.convertScaleAbs(corrected, alpha=1.0, beta=beta)
        logger.info(f"[[PREPROCESS]] Brightness boosted: mean={mean_brightness:.1f} -> +{beta:.1f}")
    
    return corrected

def process_frame(frame):
    """
    numpy arrayの画像フレームを直接処理する（カメラキャプチャ用）
    YOLOでオブジェクト検出→クロップ→Ollama分析
    """
    global is_processing
    
    if frame is None:
        logger.error("Empty frame received")
        return
    
    try:
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        raw_filename = f"raw_{timestamp}.jpg"
        processed_filename = f"camera_{timestamp}.png"  # PNGで保存（背景透過対応）
        
        logger.info(f"[[STATE_START]] Processing camera frame: {processed_filename}")
        
        # 1. 元画像をraw/に保存
        raw_path = os.path.join(RAW_CAPTURE_DIR, raw_filename)
        cv2.imwrite(raw_path, frame)
        logger.info(f"[[CAPTURE]] Raw image saved: {raw_path}")
        
        # 2. YOLOで検出＆クロップ
        cropped_frame, detection_info = yolo_processor.detect_and_crop(frame)
        logger.info(f"[[YOLO]] Detection: {detection_info.get('detection_count', 0)} objects, type: {detection_info.get('crop_type', 'none')}")
        
        # 2.5 YOLO検出完了をUnityに通知（Phase 2 ローテーション開始トリガー）
        primary_class = detection_info.get("primary_class")
        if primary_class:
            print(f"[[ITEM_IDENTIFIED]] {primary_class}")
            sys.stdout.flush()
        
        # 3. 明るさ調整（暗所対策）
        logger.info("[[PREPROCESS]] Adjusting brightness...")
        bright_frame = apply_intelligent_brightness(cropped_frame)
        
        # 4. CLAHE（コントラスト調整）を適用
        logger.info("[[PREPROCESS]] Applying CLAHE...")
        lab = cv2.cvtColor(bright_frame, cv2.COLOR_BGR2LAB)
        l, a, b = cv2.split(lab)
        clahe = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(8, 8))
        l = clahe.apply(l)
        lab = cv2.merge([l, a, b])
        clahe_frame = cv2.cvtColor(lab, cv2.COLOR_LAB2BGR)
        
        # 5. 背景除去 (rembg)
        logger.info("[[PREPROCESS]] Applying background removal...")
        try:
            _, buffer = cv2.imencode('.png', clahe_frame)
            removed_bg = remove(buffer.tobytes())
            # バイト列からnumpy arrayに復元
            final_frame = cv2.imdecode(np.frombuffer(removed_bg, np.uint8), cv2.IMREAD_UNCHANGED)
            logger.info("[[PREPROCESS]] Background removal successful")
        except Exception as e:
            logger.warning(f"[[PREPROCESS]] Background removal failed: {e}, using CLAHE-only")
            final_frame = clahe_frame
        
        # 6. 最終処理済み画像をcapture/に保存
        processed_path = os.path.join(CAPTURE_DIR, processed_filename)
        cv2.imwrite(processed_path, final_frame)
        logger.info(f"[[CAPTURE]] Final processed image saved: {processed_path}")
        
        # 7. YOLOヒントを生成（cell phone検出時はスキップ）
        # 注意: YOLOは正方形の台を「cell phone」と誤検出しやすいため、
        #       cell phone検出時はヒントを渡さず、Ollamaに純粋に画像判断させる
        yolo_hint = None
        primary_class = detection_info.get("primary_class")
        primary_confidence = detection_info.get("primary_confidence", 0.0)
        detected_classes = detection_info.get("detected_classes", [])
        
        # cell phone / mobile phone をフィルタリング
        SKIP_HINT_CLASSES = ["cell phone", "cellphone", "mobile phone", "smartphone"]
        
        if primary_class:
            if primary_class.lower() in SKIP_HINT_CLASSES:
                logger.info(f"[[YOLO HINT]] SKIPPED: '{primary_class}' detected - likely misidentification of the display stand")
            elif len(detected_classes) > 1:
                # 他の検出からもcell phoneを除外
                other_classes = [c for c in detected_classes if c != primary_class and c.lower() not in SKIP_HINT_CLASSES]
                if other_classes:
                    yolo_hint = f"Primary: {primary_class} (confidence: {primary_confidence:.2f}), also detected: {', '.join(other_classes)}"
                else:
                    yolo_hint = f"{primary_class} (confidence: {primary_confidence:.2f})"
                logger.info(f"[[YOLO HINT]] Generated: {yolo_hint}")
            else:
                yolo_hint = f"{primary_class} (confidence: {primary_confidence:.2f})"
                logger.info(f"[[YOLO HINT]] Generated: {yolo_hint}")
        
        # 8. Ollamaで分析（最終処理済み画像を使用、YOLOヒント付き）
        analysis_data = ollama_client.analyze_image(processed_path, yolo_hint=yolo_hint)
        logger.info(f"[[OLLAMA ANALYSIS]] Data: {json.dumps(analysis_data, ensure_ascii=False)}")
        
        _process_analysis(analysis_data, processed_filename)
        
    except Exception as e:
        logger.error(f"Frame Processing Failed: {e}")
        import traceback
        traceback.print_exc()
    finally:
        is_processing = False


def _process_analysis(analysis_data, filename):
    """
    分析結果を処理してセリフ生成・音声合成を行う（共通処理）
    """
    persona_id, role_name = determine_persona(analysis_data)
    
    item_name_raw = analysis_data.get("item_name", "Object")
    
    # 信頼度に基づいて表示名を決定（低信頼度の場合は抽象カテゴリ名を使用）
    display_name = get_display_name(analysis_data, threshold=0.9)
    
    # アイテム名を正規化（既知リストとのマッチング）- 具体名の場合のみ
    if display_name == item_name_raw:
        item_name = ollama_client.match_to_known_items(
            item_name_raw, 
            item_obsessions.CANONICAL_ITEMS
        )
    else:
        # 抽象名が使われる場合はそのまま使用
        item_name = display_name
        logger.info(f"[[CONFIDENCE]] Low confidence detected, using abstract name: {item_name}")
    
    is_machine_str = str(analysis_data.get("is_machine", False))
    shape_val = analysis_data.get("shape", "Unknown")
    state_val = analysis_data.get("state", "Normal")

    obsession_instruction = item_obsessions.get_obsession_instruction(item_name)
    
    # context_str = f"Context: Machine={is_machine_str}, Shape={shape_val}, State={state_val}."
    context_str = "" # ユーザー要望により、item_name以外の情報をカット（過去のシステムの名残削除）
    
    import prompts 
    topic = random.choice(prompts.TOPIC_LIST)
    
    # TTS無効化: voice_settings取得とCOEIROINK表示をスキップ
    # voice_settings = get_voice_uuid(persona_id)
    # if voice_settings:
    #     logger.info(f"[[CREDIT]] COERIOINK: {voice_settings['name']} (Role: {role_name})")
    # else:
    #     logger.warning("[[CREDIT]] No voice settings found.")
    voice_settings = None  # TTS無効化のためNoneに設定

    full_text = deepseek_client.generate_dialogue(
        item_name,
        context_str,
        topic,
        obsession_instruction
    )
    
    logger.info(f"[[DEEPSEEK RAW]] {full_text}")

    import re
    match = re.search(r'(.*)(?:\s+by\s+)(.*)', full_text, re.DOTALL)
    
    if match:
        speech_text = match.group(1).strip()
        role_suffix = match.group(2).strip()
        logger.info(f"[[CHARACTER]] {role_suffix}")
    else:
        speech_text = full_text.split('by')[0].strip()
        logger.info("[[CHARACTER]] ")

    logger.info(f"[[MESSAGE]] {speech_text}")

    # ペア情報を記録（画像とメッセージの対応）
    character_name = role_suffix if match else role_name
    # TTS無効化: COEIROINKクレジットを表示しない
    credit_str = f"by {character_name}"
    _save_message_pair(filename, speech_text, credit_str)

    if len(speech_text) < 2:
        logger.error("Speech text too short/empty.")
        return

    # TTS無効化: 音声合成処理をスキップ
    # if voice_settings:
    #     audio_data = voice_client.synthesis(
    #         speech_text, 
    #         voice_settings['uuid'], 
    #         voice_settings['style']
    #     )
    #
    #     if audio_data:
    #         base_name = os.path.splitext(filename)[0]
    #         wav_filename = f"{base_name}.wav"
    #         wav_path = os.path.join(VOICE_DIR, wav_filename)
    #         
    #         try:
    #             with open(wav_path, "wb") as f:
    #                 f.write(audio_data)
    #             logger.info(f"[[STATE_COMPLETE]] Saved audio to {wav_filename}")
    #         except Exception as e:
    #             logger.error(f"File Write Failed: {e}")
    #     else:
    #         logger.error("[[STATE_COMPLETE]] Audio Gen Failed")
    # else:
    #     logger.error("[[STATE_COMPLETE]] No voice settings")
    
    logger.info("[[STATE_COMPLETE]] Processing finished (TTS disabled)")

# --- Watcher Class (既存のファイル監視も維持) ---
class ImageHandler(FileSystemEventHandler):
    def __init__(self):
        self.last_processed = {}

    def on_created(self, event):
        if event.is_directory: return
        filename = os.path.basename(event.src_path)
        ext = os.path.splitext(filename)[1].lower()
        
        # camera_で始まるファイルは process_frame() で既に処理済みなので無視
        if filename.startswith('camera_'):
            logger.info(f"Skipping camera-captured file (already processed): {filename}")
            return
        
        if ext in WATCHED_EXTENSIONS:
            now = time.time()
            if filename in self.last_processed:
                if now - self.last_processed[filename] < 2.0:
                    logger.info(f"Skipping duplicate event for {filename}")
                    return
            
            self.last_processed[filename] = now
            time.sleep(1.0) 
            process_image(event.src_path)

# --- stdin Listener (Unity からのコマンド受信) ---
def stdin_listener():
    """
    stdinからのコマンドを監視するスレッド
    Unity側から送られてくるコマンドを処理
    """
    global is_processing, camera_capture
    
    logger.info("[[STDIN]] Listener started - waiting for commands...")
    
    for line in sys.stdin:
        cmd = line.strip().upper()
        
        if cmd.startswith("CAPTURE"):
            logger.info("[[STDIN]] CAPTURE command received")
            
            # 処理中なら無視
            if is_processing:
                logger.warning("[[STDIN]] Already processing, ignoring CAPTURE")
                continue
            
            # 引数解析 (CAPTURE <index>)
            target_index = None
            parts = cmd.split()
            if len(parts) > 1 and parts[1].isdigit():
                target_index = int(parts[1])
                logger.info(f"[[CAPTURE]] Targeted Camera Index: {target_index}")
            
            with processing_lock:
                is_processing = True
            
            try:
                # カメラ初期化（インデックス指定があれば再初期化）
                if target_index is not None:
                    if camera_capture.camera_index != target_index:
                        logger.info(f"[[CAPTURE]] Switching camera index: {camera_capture.camera_index} -> {target_index}")
                        camera_capture.release()
                        # Unity側で指定されたインデックスを使用（auto_detect無効）
                        camera_capture = CameraCapture(camera_index=target_index, auto_detect=False)
                
                if not camera_capture._is_initialized:
                    camera_capture.initialize()
                
                # フリッカー対策付きキャプチャ
                logger.info("[[CAPTURE]] Starting stabilized capture...")
                frame = camera_capture.capture_with_stabilization()
                
                if frame is not None:
                    # キャプチャ完了をUnityに通知（Scanning状態への遷移トリガー）
                    logger.info("[[CAPTURE_DONE]]")
                    # 別スレッドで処理（stdinリスナーをブロックしない）
                    process_thread = threading.Thread(
                        target=process_frame, 
                        args=(frame,),
                        daemon=True
                    )
                    process_thread.start()
                else:
                    logger.error("[[CAPTURE]] Failed to capture frame")
                    is_processing = False
                    
            except Exception as e:
                logger.error(f"[[CAPTURE]] Error: {e}")
                is_processing = False
        
        elif cmd == "QUIT":
            logger.info("[[STDIN]] QUIT command received, shutting down...")
            break
        
        elif cmd:
            logger.info(f"[[STDIN]] Unknown command: {cmd}")

if __name__ == "__main__":
    print("--- Hybrid AI Object Voice System (v9.0: Camera + Ollama + DeepSeek) ---")
    logger.info("--- Hybrid AI Object Voice System (v9.0: Camera + Ollama + DeepSeek) ---")
    
    # ファイル監視（既存機能を維持）
    event_handler = ImageHandler()
    observer = Observer()
    observer.schedule(event_handler, CAPTURE_DIR, recursive=False)
    observer.start()
    logger.info(f"File watcher started on: {CAPTURE_DIR}")
    
    # stdin リスナーを別スレッドで開始
    stdin_thread = threading.Thread(target=stdin_listener, daemon=True)
    stdin_thread.start()
    logger.info("stdin listener started")
    
    try:
        while stdin_thread.is_alive():
            time.sleep(0.5)
    except KeyboardInterrupt:
        logger.info("Keyboard interrupt received")
    finally:
        observer.stop()
        camera_capture.release()
        logger.info("Cleanup complete")
    
    observer.join()
