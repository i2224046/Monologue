import os
import google.generativeai as genai
from dotenv import load_dotenv

def verify():
    load_dotenv()
    api_key = os.getenv("GEMINI_API_KEY")
    
    if not api_key:
        print("[FAIL] No GEMINI_API_KEY found in .env files or environment.")
        return

    print(f"[INFO] Found API Key: {api_key[:5]}...{api_key[-3:]}")
    
    try:
        genai.configure(api_key=api_key)
        model = genai.GenerativeModel("gemini-flash-latest")
        response = model.generate_content("Hello, are you working?")
        print(f"[SUCCESS] Gemini Responded: {response.text}")
    except Exception as e:
        print(f"[FAIL] API Connection Failed: {e}")

if __name__ == "__main__":
    verify()
