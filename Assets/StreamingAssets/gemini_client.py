import os
import google.generativeai as genai
import json
import logging
from dotenv import load_dotenv
import prompts

# Configure basic logging
logger = logging.getLogger(__name__)

class GeminiClient:
    def __init__(self, model_name="gemini-2.5-flash-lite"):
        load_dotenv()
        self.api_key = os.getenv("GEMINI_API_KEY")
        if not self.api_key:
            logger.error("GEMINI_API_KEY not found in environment variables.")
            raise ValueError("GEMINI_API_KEY is missing")
        
        genai.configure(api_key=self.api_key)
        self.model = genai.GenerativeModel(model_name)
        self.model_name = model_name
        logger.info(f"GeminiClient initialized with model: {self.model_name}")

    def generate_dialogue(self, item_name: str, context_str: str, topic: str, obsession_instruction: str = None) -> str:
        """
        Generates character dialogue based on the new textual prompt format.
        """
        
        # Build the structured prompt
        full_prompt = (
            f"Role: Personify the object '{item_name}'.\n"
            f"{context_str}\n"
            f"Topic: {topic}\n\n"
            f"{prompts.CORE_LOGIC}\n"
            f"{obsession_instruction if obsession_instruction else ''}\n"
            f"{prompts.PERSONA_LOGIC}\n"
            f"{prompts.GEMINI_TASK}\n"
        )
        
        logger.info(f"Generating dialogue for: {item_name} (Topic: {topic})")
        logger.info(f"[[GEMINI PROMPT]] Full Prompt:\n{full_prompt}")

        try:
            response = self.model.generate_content(
                full_prompt,
                generation_config=genai.GenerationConfig(
                    temperature=0.9 # Slightly higher for creativity
                )
            )
            
            text = response.text.strip()
            # Simple cleanup if the model outputs markdown code blocks
            if text.startswith("```"):
                text = text.replace("```json", "").replace("```", "").strip()
            
            return text

        except Exception as e:
            logger.error(f"Dialogue Generation Failed: {e}")
            return f"...... by {item_name}"
