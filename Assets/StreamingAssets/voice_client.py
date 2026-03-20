import requests
import json
import logging

# Configure basic logging
logger = logging.getLogger(__name__)

class VoiceClient:
    def __init__(self, api_server="http://127.0.0.1:50032"):
        self.api_server = api_server

    def synthesis(self, text: str, speaker_uuid: str, style_id: int) -> bytes:
        if not text or len(text) == 0:
            logger.error("TTS: Input text is empty. Skipping.")
            return None

        # COEIROINK API Specification: Two-step process
        prosody_payload = {"text": text}
        logger.info(f"TTS Step 1: Estimating Prosody for '{text[:15]}...'")
        
        try:
            r1 = requests.post(
                f"{self.api_server}/v1/estimate_prosody", 
                json=prosody_payload,
                timeout=10
            )
            r1.raise_for_status()
            prosody_data = r1.json()
            
            if 'detail' not in prosody_data:
                logger.error("TTS Step 1 Failed: No 'detail' in response")
                return None
                
        except Exception as e:
            logger.error(f"TTS Step 1 (Prosody) Error: {e}")
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
        
        logger.info("TTS Step 2: Synthesizing...")
        try:
            # Retry logic for synthesis (handling intermittent 500 errors)
            max_retries = 3
            for attempt in range(max_retries):
                try:
                    response = requests.post(
                        f"{self.api_server}/v1/synthesis",
                        headers={"Content-Type": "application/json"},
                        data=json.dumps(query),
                        timeout=30  # Increased timeout slightly
                    )
                    response.raise_for_status()
                    break # Success
                except requests.exceptions.RequestException as e:
                    if attempt < max_retries - 1:
                        logger.warning(f"TTS Step 2 Failed (Attempt {attempt+1}/{max_retries}). Retrying in 1s... Error: {e}")
                        import time
                        time.sleep(1)
                    else:
                        raise e # Re-raise on last failure
            logger.info(f"Success. Size: {len(response.content)} bytes")
            return response.content
        except Exception as e:
            logger.error(f"TTS Step 2 (Synthesis) Failed: {e}")
            if hasattr(e, 'response') and e.response is not None:
                 logger.error(f"Server Msg: {e.response.text}")
            return None
