import sys
import os

# Add the directory containing ollamaRun.py to sys.path
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

try:
    import ollamaRun
    print("ollamaRunのインポートに成功しました")
    
    print(f"VOICE_VARIANTS keys: {list(ollamaRun.VOICE_VARIANTS.keys())}")
    print(f"PERSONALITY_PROMPTS keys: {list(ollamaRun.PERSONALITY_PROMPTS.keys())}")
    print(f"PSYCHOLOGICAL_TRIGGERS length: {len(ollamaRun.PSYCHOLOGICAL_TRIGGERS)}")
    
    if len(ollamaRun.VOICE_VARIANTS) > 0 and len(ollamaRun.PERSONALITY_PROMPTS) > 0 and len(ollamaRun.PSYCHOLOGICAL_TRIGGERS) > 0:
        print("設定の読み込みに成功しました。")
    else:
        print("設定の読み込みに失敗したか、空です。")
        
except Exception as e:
    print(f"ollamaRunのインポートエラー: {e}")
