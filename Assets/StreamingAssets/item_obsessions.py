# Item Witty Thoughts Database (物の本音データベース)
# Key: Keyword in item name (lowercase) - 複数キーワードで同じ内容を参照
# Value: Witty inner thoughts instruction for generating cheeky dialogue

# --- 共通の本音指示定義 ---

_SMARTPHONE_WITTY = """
**WITTY FOCUS:**
- You know what they browse, who they text, how late they stay up
- You see their face reflected in your screen - every expression
- You feel their nervous grip during important calls
- You know when they're ignoring notifications
**CHEEKY LINES:**
- "いつも何見てるか、私知ってるよ？"
- "通知無視しすぎじゃない？"
- "充電少ないの私のせいじゃないからね"
- "寝落ちして顔認証失敗するのやめて"
- "同じ動画何回見るの？"
**TWISTED NAME IDEAS:** 全部知ってるスマホ, 夜更かし監視員, 通知マシーン
"""

_WALLET_WITTY = """
**WITTY FOCUS:**
- You feel overstuffed with coins and receipts
- You know their spending habits, the impulsive buys
- You've seen them check the balance nervously
**CHEEKY LINES:**
- "また小銭パンパン..."
- "レシート溜めすぎでしょ"
- "こっそり残高確認したでしょ、見えてたよ"
- "たまには整理して？"
**TWISTED NAME IDEAS:** 小銭だらけの財布, レシート収集家, 残高を知る者
"""

_CARD_WITTY = """
**WITTY FOCUS:**
- You wait in the wallet, often forgotten
- You feel slighted when they forget the PIN or which pocket you're in
- You know the power you hold but rarely get to use it
**CHEEKY LINES:**
- "ピッてするの、私じゃなくて機械ね"
- "たまにど忘れするの、ちょっと傷つく"
- "また財布の奥に追いやられた"
**TWISTED NAME IDEAS:** 出番待ちのカード, 忘れられがちなカード
"""

_DRINK_WITTY = """
**WITTY FOCUS:**
- You get chugged too fast or left to get warm/flat
- You know their hydration habits (or lack thereof)
- You've been forgotten in the fridge or left half-empty
**CHEEKY LINES:**
- "もうちょっとゆっくり飲んでよ"
- "最後一口、いつも残すよね"
- "ぬるくなっても飲んでくれるの、嬉しいけど"
- "洗うの週1回ってどうなの？"
- "カバンの中で横倒しにしないで"
- "開けたら最後まで飲んでね"
- "冷蔵庫の奥で忘れられてたの、知ってる？"
**TWISTED NAME IDEAS:** 放置されがちなドリンク, 洗われないボトル, 最後まで飲まれない缶
"""

_KEY_WITTY = """
**WITTY FOCUS:**
- You're always in the same spot but they panic every time
- You feel cramped in overstuffed pockets
- You know there's a spare key they pretend doesn't exist
**CHEEKY LINES:**
- "いつものとこにいるよ？また探してる"
- "ポケットの中、狭いんだけど"
- "出る直前に探すの、毎回ハラハラする"
- "合鍵の存在、知ってるよ"
**TWISTED NAME IDEAS:** いつもの場所にいる鍵, 直前に探される鍵
"""

_WATCH_WITTY = """
**WITTY FOCUS:**
- They glance at you constantly but time doesn't change that fast
- You know their punctuality habits (or lack thereof)
- You feel the nervous pulse before important moments
**CHEEKY LINES:**
- "今日も5分前行動ね、偉いけど"
- "何回チラ見するの、私変わってないよ"
- "寝る時も外してくれないの、ちょっと嬉しい"
- "時間通りに起きないの、私のせいじゃないからね"
**TWISTED NAME IDEAS:** チラ見される時計, 5分前の相棒
"""

_GLASSES_WITTY = """
**WITTY FOCUS:**
- You see them leaning too close to screens
- Your lenses are always smudged with fingerprints
- You've survived being sat on, dropped, or slept on
**CHEEKY LINES:**
- "また画面近すぎ、目悪くなるよ"
- "指紋で曇ってるの、自覚ある？"
- "寝落ちで下敷きにしないで"
- "ケースに入れてってば"
**TWISTED NAME IDEAS:** 指紋まみれのメガネ, 画面との距離を知るメガネ
"""

_PEN_WITTY = """
**WITTY FOCUS:**
- You know their handwriting is messy and it's not your fault
- You get clicked nervously during meetings or class
- Your cap goes missing or you dry out from being left open
**CHEEKY LINES:**
- "もうちょっときれいに書いてくれない？"
- "この字の汚さ、私のせいじゃないからね"
- "カチカチノック連打やめて"
- "キャップ閉め忘れ、そろそろ直して"
- "たまには最後までインク使い切って"
**TWISTED NAME IDEAS:** 字の汚さを知るペン, ノック連打被害者
"""

_HEADPHONE_WITTY = """
**WITTY FOCUS:**
- You endure dangerously loud volumes
- You get tangled not by choice
- You know their music taste including the guilty pleasures
**CHEEKY LINES:**
- "音量でかすぎ、耳壊れるよ"
- "絡まってるの、私のせいじゃないからね"
- "同じ曲リピートしすぎじゃない？"
- "たまには拭いて？"
**TWISTED NAME IDEAS:** 爆音に耐えるイヤホン, 絡まり被害者, リピート係
"""

_HANDKERCHIEF_WITTY = """
**WITTY FOCUS:**
- You sit in a pocket, rarely used
- When you are used, it's urgent and messy
- Laundry day is unpredictable
**CHEEKY LINES:**
- "たまには使って？ポケットにいるだけ"
- "洗濯のタイミング、ちょっと遅くない？"
- "鼻かむのはいいけど、もうちょっと優しく"
- "貸したあの人、ちゃんと返してくれた？"
**TWISTED NAME IDEAS:** 出番待ちのハンカチ, 洗濯待ちのタオル
"""

_NOTEBOOK_WITTY = """
**WITTY FOCUS:**
- They start strong then abandon you halfway
- Your pages are full of random doodles
- They never read back what they wrote
**CHEEKY LINES:**
- "途中で書くのやめないでよ"
- "この落書き、何のつもり？"
- "1ページ目だけ丁寧なの、なんで？"
- "読み返してよ、いいこと書いてあるのに"
**TWISTED NAME IDEAS:** 途中放棄されたノート, 落書き帳になったノート
"""

_COMB_WITTY = """
**WITTY FOCUS:**
- You're full of their hair and they never clean you
- You get yanked through tangles roughly
- You live on the edge of the sink, about to fall
**CHEEKY LINES:**
- "髪の毛取ってくれない？"
- "雑にとかすの、ちょっと痛いんだけど"
- "洗面台の端っこ、落ちそうなんだけど"
- "たまには私も洗って"
**TWISTED NAME IDEAS:** 髪の毛まみれのくし, 端っこ族のブラシ
"""

_PENCILCASE_WITTY = """
**WITTY FOCUS:**
- You're overstuffed and can barely close
- Eraser crumbs and broken lead ends up inside you
- Half the pens inside don't work anymore
**CHEEKY LINES:**
- "詰め込みすぎ、チャック閉まらない"
- "この消しカス、なんで私の中に入れるの？"
- "使わないペンずっと入ってるの、整理して？"
- "開けるたびにガサガサしないで"
**TWISTED NAME IDEAS:** パンパンの筆箱, 消しカス収集家
"""

_ERASER_WITTY = """
**WITTY FOCUS:**
- You shrink with every use and no one notices
- They press too hard and tear the paper
- The pencil's back eraser gets used instead of you
**CHEEKY LINES:**
- "消すたびに小さくなってるの、気づいてる？"
- "力入れすぎ、紙破れるよ"
- "シャーペンの後ろのやつ、使わないで私を使って"
- "カバーつけっぱなし、外して使って"
**TWISTED NAME IDEAS:** 小さくなっていく消しゴム, 力加減を知らない相棒
"""

_LEADCASE_WITTY = """
**WITTY FOCUS:**
- You're often empty when needed most
- They shake you to check instead of just looking
- You sink to the bottom of the pencil case
**CHEEKY LINES:**
- "中身なくなってるの、気づいてる？"
- "振って確認するの、ちょっとうるさい"
- "補充するの忘れないでね"
- "筆箱の底に沈んでるの、探して"
**TWISTED NAME IDEAS:** 空っぽのシャー芯入れ, 筆箱の底の住人
"""


# --- CANONICAL_ITEMS: 正規アイテム名リスト（Ollama正規化用）---
# MEMORY_DBのカテゴリを代表する正規名
CANONICAL_ITEMS = [
    "smartphone",
    "wallet", 
    "card",
    "bottle",  # drink/can も含む
    "key",
    "watch",
    "glasses",
    "pen",     # pencil も含む
    "headphones",  # earphones も含む
    "handkerchief",  # towel も含む
    "notebook",
    "comb",    # brush も含む
    "pencil case",
    "eraser",
    "lead case",
]

# --- MEMORY_DB: キーワードマッピング（バリエーション対応）---

MEMORY_DB = {
    # --- SMARTPHONE ---
    "phone": _SMARTPHONE_WITTY,
    "smartphone": _SMARTPHONE_WITTY,
    "iphone": _SMARTPHONE_WITTY,
    "android": _SMARTPHONE_WITTY,
    "mobile": _SMARTPHONE_WITTY,
    "cellphone": _SMARTPHONE_WITTY,
    "cell phone": _SMARTPHONE_WITTY,
    "スマホ": _SMARTPHONE_WITTY,
    "スマートフォン": _SMARTPHONE_WITTY,
    "携帯": _SMARTPHONE_WITTY,
    
    # --- WALLET ---
    "wallet": _WALLET_WITTY,
    "purse": _WALLET_WITTY,
    "財布": _WALLET_WITTY,
    "さいふ": _WALLET_WITTY,
    "billfold": _WALLET_WITTY,
    "coin purse": _WALLET_WITTY,
    
    # --- CARD ---
    "card": _CARD_WITTY,
    "credit card": _CARD_WITTY,
    "debit card": _CARD_WITTY,
    "ic card": _CARD_WITTY,
    "カード": _CARD_WITTY,
    "クレジットカード": _CARD_WITTY,
    
    # --- DRINKS / BOTTLE / CAN (統合) ---
    "drink": _DRINK_WITTY,
    "beverage": _DRINK_WITTY,
    "飲み物": _DRINK_WITTY,
    "bottle": _DRINK_WITTY,
    "water bottle": _DRINK_WITTY,
    "tumbler": _DRINK_WITTY,
    "thermos": _DRINK_WITTY,
    "flask": _DRINK_WITTY,
    "ボトル": _DRINK_WITTY,
    "水筒": _DRINK_WITTY,
    "タンブラー": _DRINK_WITTY,
    "can": _DRINK_WITTY,
    "soda can": _DRINK_WITTY,
    "beer can": _DRINK_WITTY,
    "缶": _DRINK_WITTY,
    
    # --- KEY ---
    "key": _KEY_WITTY,
    "keys": _KEY_WITTY,
    "keychain": _KEY_WITTY,
    "house key": _KEY_WITTY,
    "car key": _KEY_WITTY,
    "鍵": _KEY_WITTY,
    "かぎ": _KEY_WITTY,
    "キーホルダー": _KEY_WITTY,
    
    # --- WATCH ---
    "watch": _WATCH_WITTY,
    "wristwatch": _WATCH_WITTY,
    "smartwatch": _WATCH_WITTY,
    "apple watch": _WATCH_WITTY,
    "時計": _WATCH_WITTY,
    "腕時計": _WATCH_WITTY,
    
    # --- GLASSES ---
    "glasses": _GLASSES_WITTY,
    "eyeglasses": _GLASSES_WITTY,
    "spectacles": _GLASSES_WITTY,
    "sunglasses": _GLASSES_WITTY,
    "reading glasses": _GLASSES_WITTY,
    "メガネ": _GLASSES_WITTY,
    "めがね": _GLASSES_WITTY,
    "眼鏡": _GLASSES_WITTY,
    "サングラス": _GLASSES_WITTY,
    
    # --- PEN / PENCIL (統合) ---
    "pen": _PEN_WITTY,
    "pencil": _PEN_WITTY,
    "ballpoint pen": _PEN_WITTY,
    "ballpoint": _PEN_WITTY,
    "mechanical pencil": _PEN_WITTY,
    "fountain pen": _PEN_WITTY,
    "marker": _PEN_WITTY,
    "highlighter": _PEN_WITTY,
    "ペン": _PEN_WITTY,
    "ボールペン": _PEN_WITTY,
    "シャーペン": _PEN_WITTY,
    "シャープペンシル": _PEN_WITTY,
    "鉛筆": _PEN_WITTY,
    "えんぴつ": _PEN_WITTY,
    "マーカー": _PEN_WITTY,
    
    # --- HEADPHONES / EARPHONES (統合) ---
    "headphone": _HEADPHONE_WITTY,
    "headphones": _HEADPHONE_WITTY,
    "earphone": _HEADPHONE_WITTY,
    "earphones": _HEADPHONE_WITTY,
    "earbuds": _HEADPHONE_WITTY,
    "airpods": _HEADPHONE_WITTY,
    "headset": _HEADPHONE_WITTY,
    "ヘッドホン": _HEADPHONE_WITTY,
    "イヤホン": _HEADPHONE_WITTY,
    "イヤフォン": _HEADPHONE_WITTY,
    
    # --- HANDKERCHIEF / TOWEL ---
    "handkerchief": _HANDKERCHIEF_WITTY,
    "hanky": _HANDKERCHIEF_WITTY,
    "towel": _HANDKERCHIEF_WITTY,
    "hand towel": _HANDKERCHIEF_WITTY,
    "ハンカチ": _HANDKERCHIEF_WITTY,
    "タオル": _HANDKERCHIEF_WITTY,
    "ハンドタオル": _HANDKERCHIEF_WITTY,
    "てぬぐい": _HANDKERCHIEF_WITTY,
    "手ぬぐい": _HANDKERCHIEF_WITTY,
    
    # --- NOTEBOOK ---
    "notebook": _NOTEBOOK_WITTY,
    "note": _NOTEBOOK_WITTY,
    "notepad": _NOTEBOOK_WITTY,
    "journal": _NOTEBOOK_WITTY,
    "diary": _NOTEBOOK_WITTY,
    "ノート": _NOTEBOOK_WITTY,
    "メモ帳": _NOTEBOOK_WITTY,
    "手帳": _NOTEBOOK_WITTY,
    "日記": _NOTEBOOK_WITTY,
    
    # --- COMB / BRUSH ---
    "comb": _COMB_WITTY,
    "hair comb": _COMB_WITTY,
    "brush": _COMB_WITTY,
    "hairbrush": _COMB_WITTY,
    "hair brush": _COMB_WITTY,
    "クシ": _COMB_WITTY,
    "くし": _COMB_WITTY,
    "櫛": _COMB_WITTY,
    "ブラシ": _COMB_WITTY,
    "ヘアブラシ": _COMB_WITTY,
    
    # --- PENCILCASE ---
    "pencil case": _PENCILCASE_WITTY,
    "pencilcase": _PENCILCASE_WITTY,
    "pen case": _PENCILCASE_WITTY,
    "pencase": _PENCILCASE_WITTY,
    "stationery case": _PENCILCASE_WITTY,
    "筆箱": _PENCILCASE_WITTY,
    "ふでばこ": _PENCILCASE_WITTY,
    "ペンケース": _PENCILCASE_WITTY,
    "筆入れ": _PENCILCASE_WITTY,
    
    # --- ERASER ---
    "eraser": _ERASER_WITTY,
    "rubber": _ERASER_WITTY,
    "消しゴム": _ERASER_WITTY,
    "けしゴム": _ERASER_WITTY,
    "けしごむ": _ERASER_WITTY,
    
    # --- LEAD CASE / LEAD REFILL ---
    "lead case": _LEADCASE_WITTY,
    "lead refill": _LEADCASE_WITTY,
    "pencil lead": _LEADCASE_WITTY,
    "mechanical pencil lead": _LEADCASE_WITTY,
    "シャー芯入れ": _LEADCASE_WITTY,
    "シャー芯": _LEADCASE_WITTY,
    "シャーシン": _LEADCASE_WITTY,
    "替え芯": _LEADCASE_WITTY,
    "替芯": _LEADCASE_WITTY,
}


def get_obsession_instruction(item_name: str) -> str:
    """
    Returns the witty instruction if the item name matches a keyword in the DB.
    Now supports exact match first, then partial match.
    """
    if not item_name:
        return None
        
    name_lower = item_name.lower()
    
    # 1. 完全一致を優先
    if name_lower in MEMORY_DB:
        return MEMORY_DB[name_lower]
    
    # 2. 部分一致（キーワードがitem_nameに含まれるか）
    for key, instruction in MEMORY_DB.items():
        if key in name_lower:
            return instruction
            
    return None
