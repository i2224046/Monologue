"""
category_mapping.py - 信頼度に基づく抽象名称マッピング

Ollamaの認識confidence値が閾値以下の場合、
具体的なitem_nameの代わりに抽象カテゴリ名を使用する。
"""

# 抽象カテゴリ名マッピング
ABSTRACT_NAMES = {
    "machine": "機械",
    "cloth": "布",
    "container": "容器",
    "stationery": "文房具",
    "leather": "皮革製品",
    "metal": "金属",
    "other": "モノ"
}

def get_display_name(analysis_data: dict, threshold: float = 0.7) -> str:
    """
    信頼度に応じて具体名または抽象名を返す
    
    Args:
        analysis_data: Ollamaからの分析結果（confidence, item_name, item_category含む）
        threshold: 具体名を使用する信頼度の閾値（デフォルト: 0.7）
    
    Returns:
        表示用の名前（具体名または抽象カテゴリ名）
    """
    confidence = analysis_data.get("confidence", 0.5)
    
    if confidence >= threshold:
        # 信頼度が閾値以上 → 具体名を使用
        return analysis_data.get("item_name", "モノ")
    else:
        # 信頼度が閾値未満 → 抽象カテゴリ名にフォールバック
        category = analysis_data.get("item_category", "other")
        return ABSTRACT_NAMES.get(category, "モノ")
