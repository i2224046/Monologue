"""
yolo_world_classes.py - YOLO-World用クラス定義

YOLO-Worldに渡す事前定義クラスリスト。
日常の持ち物として検出対象となるアイテムを定義。

使用方法:
    from yolo_world_classes import YOLO_WORLD_CLASSES
    model.set_classes(YOLO_WORLD_CLASSES)
"""

# YOLO-World用クラスリスト（英語）
# 日常の持ち物に特化

YOLO_WORLD_CLASSES = [
    # === スマホ・電子機器 ===
    "smartphone",
    "mobile phone",
    "cell phone",
    "camera",
    "digital camera",
    "compact camera",
    "smartwatch",
    "watch",
    "earbuds",
    "headphones",
    "wireless earbuds",
    "charger",
    "power bank",
    "USB cable",
    
    # === 財布・カード類 ===
    "wallet",
    "purse",
    "coin purse",
    "card",
    "credit card",
    "IC card",
    "ID card",
    "business card",
    "card holder",
    "pass case",
    
    # === 鍵・キーホルダー ===
    "key",
    "keys",
    "keychain",
    "key ring",
    "car key",
    "house key",
    
    # === 文房具 ===
    "pen",
    "ballpoint pen",
    "mechanical pencil",
    "pencil",
    "marker",
    "highlighter",
    "pencil case",
    "eraser",
    "pencil lead",
    "notebook",
    "notepad",
    "memo pad",
    "sticky notes",
    
    # === 飲み物・容器 ===
    "plastic bottle",
    "PET bottle",
    "water bottle",
    "tumbler",
    "thermos",
    "bottle",
    "can",
    "drink",
    
    # === 衛生用品・身の回り品 ===
    "tissue",
    "tissue pack",
    "pocket tissue",
    "handkerchief",
    "towel",
    "hand towel",
    "hand warmer",
    "disposable hand warmer",
    "mask",
    "face mask",
    "hand sanitizer",
    "wet wipes",
    
    # === メガネ・アクセサリー ===
    "glasses",
    "eyeglasses",
    "sunglasses",
    "glasses case",
    "lip balm",
    "lipstick",
    "compact mirror",
    
    # === 身だしなみ ===
    "comb",
    "hair brush",
    "hair tie",
    
    # === バッグ・ポーチ ===
    "pouch",
    "makeup pouch",
    "coin pouch",
    "bag",
    "tote bag",
    
    # === その他日用品 ===
    "umbrella",
    "folding umbrella",
    "medicine",
    "pill case",
    "snack",
    "candy",
    "gum",
    "book",
    "fan",
    "portable fan",
]

# 日本語名との対応表（ログ・デバッグ用）
YOLO_CLASS_JP_MAPPING = {
    # 電子機器
    "smartphone": "スマートフォン",
    "mobile phone": "携帯電話",
    "cell phone": "携帯電話",
    "camera": "カメラ",
    "digital camera": "デジタルカメラ",
    "compact camera": "コンパクトカメラ",
    "smartwatch": "スマートウォッチ",
    "watch": "時計",
    "earbuds": "イヤホン",
    "headphones": "ヘッドフォン",
    "charger": "充電器",
    "power bank": "モバイルバッテリー",
    
    # 財布・カード
    "wallet": "財布",
    "purse": "財布",
    "coin purse": "小銭入れ",
    "card": "カード",
    "credit card": "クレジットカード",
    "IC card": "ICカード",
    "ID card": "身分証",
    "business card": "名刺",
    "card holder": "カードホルダー",
    "pass case": "パスケース",
    
    # 鍵
    "key": "鍵",
    "keys": "鍵",
    "keychain": "キーホルダー",
    "key ring": "キーリング",
    
    # 文房具
    "pen": "ペン",
    "ballpoint pen": "ボールペン",
    "mechanical pencil": "シャープペンシル",
    "pencil": "鉛筆",
    "pencil case": "筆箱",
    "eraser": "消しゴム",
    "pencil lead": "シャー芯",
    "notebook": "ノート",
    "notepad": "メモ帳",
    
    # 飲み物
    "plastic bottle": "ペットボトル",
    "PET bottle": "ペットボトル",
    "water bottle": "水筒",
    "tumbler": "タンブラー",
    "thermos": "魔法瓶",
    "bottle": "ボトル",
    "can": "缶",
    
    # 衛生用品
    "tissue": "ティッシュ",
    "tissue pack": "ポケットティッシュ",
    "pocket tissue": "ポケットティッシュ",
    "handkerchief": "ハンカチ",
    "towel": "タオル",
    "hand towel": "ハンドタオル",
    "hand warmer": "カイロ",
    "disposable hand warmer": "使い捨てカイロ",
    "mask": "マスク",
    "face mask": "マスク",
    "hand sanitizer": "消毒液",
    "wet wipes": "ウェットティッシュ",
    
    # メガネ・アクセサリー
    "glasses": "メガネ",
    "sunglasses": "サングラス",
    "glasses case": "メガネケース",
    "lip balm": "リップクリーム",
    "lipstick": "口紅",
    "compact mirror": "コンパクトミラー",
    
    # 身だしなみ
    "comb": "くし",
    "hair brush": "ヘアブラシ",
    "hair tie": "ヘアゴム",
    
    # バッグ
    "pouch": "ポーチ",
    "makeup pouch": "化粧ポーチ",
    "bag": "バッグ",
    "umbrella": "傘",
    "folding umbrella": "折りたたみ傘",
    
    # その他
    "medicine": "薬",
    "pill case": "薬ケース",
    "snack": "お菓子",
    "candy": "飴",
    "gum": "ガム",
    "book": "本",
    "fan": "扇子",
    "portable fan": "ハンディファン",
}
