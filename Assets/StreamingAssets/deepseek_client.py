import os
import logging
from openai import OpenAI
from dotenv import load_dotenv
import prompts

# Configure basic logging
logger = logging.getLogger(__name__)

class DeepSeekClient:
    def __init__(self, model_name="deepseek-chat"):
        load_dotenv()
        self.api_key = os.getenv("DEEPSEEK_API_KEY")
        if not self.api_key:
            logger.error("DEEPSEEK_API_KEY not found in environment variables.")
            raise ValueError("DEEPSEEK_API_KEY is missing")
        
        self.client = OpenAI(
            api_key=self.api_key, 
            base_url="https://api.deepseek.com"
        )
        self.model_name = model_name
        logger.info(f"DeepSeekClient initialized with model: {self.model_name}")

    def generate_dialogue(self, item_name: str, context_str: str, topic: str, obsession_instruction: str = None) -> str:
        """
        Generates character dialogue using DeepSeek API.
        Matches the interface of GeminiClient for easy swapping.
        """
        
        # Build the structured prompt (Same structure as Gemini)
        full_prompt = (
            f"Role: Personify the object '{item_name}'.\n"
            f"{context_str}\n"
            f"Topic: {topic}\n\n"
            f"{prompts.CORE_LOGIC}\n"
            f"{obsession_instruction if obsession_instruction else ''}\n"
            f"{prompts.PERSONA_LOGIC}\n"
            f"{prompts.GEMINI_TASK}\n" # Keeps the variable name but applies to DeepSeek
        )
        
        logger.info(f"Generating dialogue for: {item_name} (Topic: {topic})")
        logger.info(f"[[DEEPSEEK PROMPT]] Full Prompt:\n{full_prompt}")

        try:
            response = self.client.chat.completions.create(
                model="deepseek-chat",
                messages=[
                    {"role": "system", "content": "You are the voice of an object speaking its mind. Be slightly cheeky and knowing - you've observed everything. Point out habits, drop hints about secrets, or voice gentle complaints. Like an old friend who knows them too well. Never be mean, just playfully honest. Output in Japanese."},
                    {"role": "user", "content": full_prompt},
                ],
                stream=False,
                temperature=1.0 # High creativity
            )
            
            text = response.choices[0].message.content.strip()
            
            # Simple cleanup if the model outputs markdown code blocks
            if text.startswith("```"):
                text = text.replace("```json", "").replace("```", "").strip()
            
            return text

        except Exception as e:
            logger.error(f"Dialogue Generation Failed (DeepSeek): {e}")
            return f"...... by {item_name}"
