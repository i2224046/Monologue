import ollama
import json
import re
import logging
import base64
import os
import prompts

# Configure basic logging
logger = logging.getLogger(__name__)

class OllamaClient:
    def __init__(self, model_name="qwen2.5vl:7b"):
        self.model_name = model_name

    def extract_json(self, text):
        """
        Robust JSON extraction with multiple fallback patterns.
        Based on Qwen2.5-VL output format recommendations.
        """
        try:
            # Pattern 1: ```json ... ``` format (most common)
            match = re.search(r'```json\s*([\[\{].*?[\]\}])\s*```', text, re.DOTALL)
            if match:
                return json.loads(match.group(1))
            
            # Pattern 2: Direct JSON object/array in text
            match = re.search(r'(\{[^{}]*\})', text, re.DOTALL)
            if match:
                return json.loads(match.group(1))
            
            # Pattern 3: Last line contains JSON (fallback)
            lines = text.strip().split('\n')
            for line in reversed(lines):
                line = line.strip()
                if line.startswith('{') and line.endswith('}'):
                    try:
                        return json.loads(line)
                    except json.JSONDecodeError:
                        continue
            
            return None
        except json.JSONDecodeError as e:
            logger.warning(f"JSON parse error: {e}")
            return None
        except Exception as e:
            logger.error(f"Unexpected error in extract_json: {e}")
            return None

    def _normalize_keys(self, data: dict) -> dict:
        """
        Normalize JSON keys to handle LLM output inconsistencies.
        - Strips whitespace from key names
        - Fixes common typos (is_is_machine -> is_machine)
        - Ensures expected keys exist with defaults
        """
        if not data:
            return data
        
        # Strip whitespace from all keys
        normalized = {k.strip(): v for k, v in data.items()}
        
        # Fix common typos
        if "is_is_machine" in normalized and "is_machine" not in normalized:
            normalized["is_machine"] = normalized.pop("is_is_machine")
        
        # Ensure expected keys exist with defaults
        normalized.setdefault("is_machine", False)
        normalized.setdefault("shape", "Other")
        normalized.setdefault("state", "Normal")
        normalized.setdefault("item_name", "Unknown Object")
        normalized.setdefault("item_category", "other")
        normalized.setdefault("confidence", 1.0)
        
        return normalized

    def match_to_known_items(self, detected_name: str, known_items: list) -> str:
        """
        Ollamaを使って検出されたアイテム名を既知リストの最も近い項目にマッチング。
        
        Args:
            detected_name: 画像解析から得られたアイテム名
            known_items: 正規アイテム名のリスト
        
        Returns:
            マッチした正規名、またはマッチしなければ元の名前
        """
        if not detected_name or not known_items:
            return detected_name
        
        items_str = ', '.join(known_items)
        prompt = f"""You detected an object: "{detected_name}"

Which item from this list is the CLOSEST match?
LIST: {items_str}

Rules:
- If the detected object matches or is very similar to an item in the list, reply with ONLY that item name.
- If no item in the list is a close match, reply with exactly "NONE".
- Reply with ONLY the matching item name or "NONE". No explanation.

Common mappings (physical objects):
- "cell phone", "mobile" -> smartphone
- "ballpoint pen", "pencil" -> pen
- "water bottle", "tumbler" -> bottle
- "file", "document", "folder", "binder", "notepad" -> notebook
- "sunglasses" -> glasses
- "earbuds", "airpods" -> headphones

Example:
- "file" -> notebook
- "document" -> notebook
- "ballpoint pen" -> pen
- "water bottle" -> bottle
- "coffee mug" -> NONE
"""
        try:
            response = ollama.chat(
                model=self.model_name,
                messages=[{"role": "user", "content": prompt}],
                options={
                    "temperature": 0,
                    "num_predict": 50
                }
            )
            result = response['message']['content'].strip().lower()
            
            # 結果が既知リストに含まれているか確認
            known_lower = [item.lower() for item in known_items]
            if result in known_lower:
                # 元のケースを保持
                idx = known_lower.index(result)
                matched_name = known_items[idx]
                logger.info(f"[[ITEM MATCH]] '{detected_name}' -> '{matched_name}'")
                return matched_name
            elif result == "none" or "none" in result:
                logger.info(f"[[ITEM MATCH]] '{detected_name}' -> No match in known list")
                return detected_name
            else:
                # 予期しない応答の場合は元の名前を返す
                logger.warning(f"[[ITEM MATCH]] Unexpected response: '{result}', keeping original: '{detected_name}'")
                return detected_name
                
        except Exception as e:
            logger.error(f"[[ITEM MATCH]] Error: {e}, keeping original: '{detected_name}'")
            return detected_name

    def analyze_image(self, image_path: str, yolo_hint: str = None) -> dict:
        """
        Analyzes the image using local Ollama (Vision Model).
        
        Args:
            image_path: Path to the image file
            yolo_hint: Optional hint from YOLO detection (e.g., "cell phone (confidence: 0.89)")
                       If provided, uses ANALYSIS_PROMPT_WITH_HINT template
        
        Note: Image preprocessing (CLAHE, background removal) is now done
              in main_vision_voice.py before saving to capture/
        """
        if not os.path.exists(image_path):
            logger.error(f"Image not found: {image_path}")
            return None

        logger.info(f"Analyzing image (Local Ollama): {os.path.basename(image_path)}")
        if yolo_hint:
            logger.info(f"[[YOLO HINT]] Using detection hint: {yolo_hint}")

        try:
            # 画像をそのまま読み込み（前処理済み）
            with open(image_path, "rb") as f:
                image_data = base64.b64encode(f.read()).decode("utf-8")

            # ヒントがある場合はヒント付きプロンプトを使用
            if yolo_hint:
                prompt_text = prompts.ANALYSIS_PROMPT_WITH_HINT.format(yolo_hint=yolo_hint)
            else:
                prompt_text = prompts.ANALYSIS_PROMPT

            # ストリーミングで進捗を通知
            full_content = ""
            token_count = 0
            progress_interval = 10  # 10トークンごとに進捗通知（頻度UP）
            
            # Ollama処理開始を通知（初期化待ち時間のフィードバック）
            print("[[OLLAMA_START]]")
            import sys
            sys.stdout.flush()
            
            for chunk in ollama.chat(
                model=self.model_name,
                messages=[{
                    "role": "user",
                    "content": prompt_text,
                    "images": [image_data]
                }],
                options={
                    "temperature": 0.1,
                    "num_predict": 1024,
                    "top_p": 0.85,
                    "repeat_penalty": 1.1
                },
                stream=True
            ):
                if 'message' in chunk and 'content' in chunk['message']:
                    full_content += chunk['message']['content']
                    token_count += 1
                    
                    # 一定トークンごとに進捗通知
                    if token_count % progress_interval == 0:
                        print(f"[[OLLAMA_PROGRESS]] {token_count}")
                        import sys
                        sys.stdout.flush()
            
            content = full_content
            analysis_data = self.extract_json(content)
            
            if not analysis_data:
                logger.warning("Local Analysis JSON parsing failed. Using default.")
                return {
                    "is_machine": False, 
                    "shape": "Other", 
                    "state": "Normal", 
                    "item_name": "Unknown Object"
                }
            
            # Normalize keys to handle LLM output inconsistencies
            analysis_data = self._normalize_keys(analysis_data)
            return analysis_data

        except Exception as e:
            logger.error(f"Local Image Analysis Failed: {e}")
            return None

