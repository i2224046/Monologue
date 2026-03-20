#!/usr/bin/env python3
"""
ランクB: YOLOによる「注視点」のクロップ テスト用スクリプト

このスクリプトは、カメラを起動して撮影した写真をYOLOで物体検出し、
検出された領域をクロップしてQwen2.5-VLに渡すパイプラインをテストします。

使い方:
    python3 test_yolo_crop.py           # カメラモード（スペースで撮影）
    python3 test_yolo_crop.py <画像パス>  # ファイルモード
    
カメラ操作:
    スペースキー: 撮影して分析
    Qキー: 終了
"""

import os
import sys
import cv2
import numpy as np
import base64
import json
import time
from datetime import datetime
from ultralytics import YOLO

# 出力ディレクトリ
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
OUTPUT_DIR = os.path.join(SCRIPT_DIR, "yolo_test_output")
os.makedirs(OUTPUT_DIR, exist_ok=True)

def load_yolo_model():
    """YOLOv11モデルをロード（初回は自動ダウンロード）"""
    print("[INFO] Loading YOLOv11 model...")
    # YOLOv11n（nano）が最も軽量。必要に応じてyolo11s, yolo11mなどに変更可
    model = YOLO("yolo11n.pt")
    print("[INFO] Model loaded successfully!")
    return model

def detect_and_crop_from_array(model, img):
    """
    YOLOで物体を検出し、最大の物体をクロップ（numpy array版）
    
    Returns:
        cropped_image: クロップされた画像（numpy array）
        detection_info: 検出情報の辞書
    """
    if img is None:
        return None, None
    
    h, w = img.shape[:2]
    
    # YOLO推論
    results = model(img, verbose=False)
    
    # 検出結果を処理
    detections = []
    for result in results:
        boxes = result.boxes
        for box in boxes:
            x1, y1, x2, y2 = box.xyxy[0].cpu().numpy().astype(int)
            conf = float(box.conf[0].cpu().numpy())
            cls_id = int(box.cls[0].cpu().numpy())
            cls_name = model.names[cls_id]
            area = int((x2 - x1) * (y2 - y1))
            
            detections.append({
                "class": cls_name,
                "confidence": conf,
                "bbox": [int(x1), int(y1), int(x2), int(y2)],
                "area": area
            })
    
    if not detections:
        print("[WARN] No objects detected. Using full image.")
        return img, {"message": "No objects detected", "crop_type": "full_image"}
    
    # 最も面積の大きい物体を選択
    largest = max(detections, key=lambda d: d["area"])
    print(f"[INFO] Detected {len(detections)} objects. Selecting largest: {largest['class']} (conf: {largest['confidence']:.2f})")
    
    # クロップ（余白を追加）
    x1, y1, x2, y2 = largest["bbox"]
    
    # 10%のマージンを追加
    margin_x = int((x2 - x1) * 0.1)
    margin_y = int((y2 - y1) * 0.1)
    
    x1 = max(0, x1 - margin_x)
    y1 = max(0, y1 - margin_y)
    x2 = min(w, x2 + margin_x)
    y2 = min(h, y2 + margin_y)
    
    cropped = img[y1:y2, x1:x2]
    
    detection_info = {
        "total_detections": len(detections),
        "all_detections": detections,
        "selected_object": largest["class"],
        "confidence": largest["confidence"],
        "original_bbox": largest["bbox"],
        "crop_bbox": [x1, y1, x2, y2],
        "crop_type": "yolo_detected"
    }
    
    return cropped, detection_info

def draw_detections_on_frame(model, frame):
    """フレームに検出結果を描画して返す"""
    results = model(frame, verbose=False)
    
    annotated = frame.copy()
    for result in results:
        boxes = result.boxes
        for box in boxes:
            x1, y1, x2, y2 = box.xyxy[0].cpu().numpy().astype(int)
            conf = float(box.conf[0].cpu().numpy())
            cls_id = int(box.cls[0].cpu().numpy())
            cls_name = model.names[cls_id]
            
            # バウンディングボックスを描画
            cv2.rectangle(annotated, (x1, y1), (x2, y2), (0, 255, 0), 2)
            label = f"{cls_name}: {conf:.2f}"
            cv2.putText(annotated, label, (x1, y1 - 10), cv2.FONT_HERSHEY_SIMPLEX, 
                       0.6, (0, 255, 0), 2)
    
    return annotated

def analyze_with_ollama(image_data: bytes):
    """Ollamaで画像を分析"""
    try:
        import ollama
        import prompts
        
        image_base64 = base64.b64encode(image_data).decode("utf-8")
        
        print("[INFO] Sending to Ollama (qwen2.5vl:7b)...")
        response = ollama.chat(
            model="qwen2.5vl:7b",
            messages=[{
                "role": "user",
                "content": prompts.ANALYSIS_PROMPT,
                "images": [image_base64]
            }],
            options={
                "temperature": 0.1,
                "num_predict": 512,
                "top_p": 0.9
            }
        )
        
        return response['message']['content']
    except Exception as e:
        print(f"[ERROR] Ollama analysis failed: {e}")
        return None

def process_and_analyze(model, img, base_name):
    """画像を処理してOllamaで分析"""
    print(f"\n[INFO] Processing captured image...")
    
    # 元画像を保存
    original_path = os.path.join(OUTPUT_DIR, f"{base_name}_original.jpg")
    cv2.imwrite(original_path, img)
    print(f"[INFO] Original saved: {original_path}")
    
    # 検出結果を描画して保存
    annotated = draw_detections_on_frame(model, img)
    viz_path = os.path.join(OUTPUT_DIR, f"{base_name}_detections.jpg")
    cv2.imwrite(viz_path, annotated)
    print(f"[INFO] Detection visualization saved: {viz_path}")
    
    # 検出とクロップ
    cropped_img, detection_info = detect_and_crop_from_array(model, img)
    
    if cropped_img is None:
        print("[ERROR] Failed to process image")
        return
    
    # クロップ画像を保存
    crop_path = os.path.join(OUTPUT_DIR, f"{base_name}_cropped.jpg")
    cv2.imwrite(crop_path, cropped_img)
    print(f"[INFO] Cropped image saved: {crop_path}")
    
    # 検出情報を保存
    info_path = os.path.join(OUTPUT_DIR, f"{base_name}_info.json")
    with open(info_path, "w", encoding="utf-8") as f:
        json.dump(detection_info, f, indent=2, ensure_ascii=False)
    
    print("\n" + "="*50)
    print("YOLO Detection Complete!")
    print("="*50)
    print(f"Detected: {detection_info.get('selected_object', 'N/A')}")
    print(f"Confidence: {detection_info.get('confidence', 0):.2f}")
    
    # Ollamaで分析
    print("\n[INFO] Analyzing cropped image with Ollama...")
    _, buffer = cv2.imencode('.jpg', cropped_img)
    analysis_result = analyze_with_ollama(buffer.tobytes())
    
    if analysis_result:
        print("\n" + "="*50)
        print("Ollama Analysis Result:")
        print("="*50)
        print(analysis_result)
        
        # 結果を保存
        analysis_path = os.path.join(OUTPUT_DIR, f"{base_name}_analysis.txt")
        with open(analysis_path, "w", encoding="utf-8") as f:
            f.write(analysis_result)
        print(f"\n[INFO] Analysis result saved: {analysis_path}")
    
    print("\n[DONE] All outputs saved to:", OUTPUT_DIR)

def camera_mode(model):
    """カメラモード: リアルタイムプレビューとスペースで撮影"""
    print("\n" + "="*50)
    print("カメラモード")
    print("="*50)
    print("操作方法:")
    print("  スペースキー: 撮影して分析")
    print("  Qキー: 終了")
    print("="*50 + "\n")
    
    # カメラを開く
    cap = cv2.VideoCapture(0)
    if not cap.isOpened():
        print("[ERROR] カメラを開けませんでした")
        return
    
    # カメラ設定
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 720)
    
    print("[INFO] カメラ起動中... ウィンドウが表示されます")
    
    frame_count = 0
    show_detection = True
    
    while True:
        ret, frame = cap.read()
        if not ret:
            print("[ERROR] フレームを取得できませんでした")
            break
        
        # 5フレームごとに検出結果を更新（パフォーマンス向上）
        if show_detection and frame_count % 5 == 0:
            display_frame = draw_detections_on_frame(model, frame)
        elif show_detection:
            display_frame = display_frame if 'display_frame' in dir() else frame
        else:
            display_frame = frame
        
        # 操作説明を表示
        cv2.putText(display_frame, "SPACE: Capture | Q: Quit", (10, 30), 
                   cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255), 2)
        
        cv2.imshow("YOLO Camera Test - Press SPACE to capture", display_frame)
        
        key = cv2.waitKey(1) & 0xFF
        
        # スペースキーで撮影
        if key == ord(' '):
            timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            base_name = f"capture_{timestamp}"
            print(f"\n[CAPTURE] 撮影しました: {base_name}")
            
            # 撮影した瞬間のフレームを処理
            process_and_analyze(model, frame, base_name)
            print("\n[INFO] カメラに戻ります... (スペースで再撮影, Qで終了)")
        
        # Qで終了
        elif key == ord('q') or key == ord('Q'):
            print("[INFO] 終了します")
            break
        
        frame_count += 1
    
    cap.release()
    cv2.destroyAllWindows()

def file_mode(model, image_path):
    """ファイルモード: 指定した画像を処理"""
    if not os.path.exists(image_path):
        print(f"[ERROR] Image not found: {image_path}")
        sys.exit(1)
    
    print(f"[INFO] Processing file: {image_path}")
    
    img = cv2.imread(image_path)
    if img is None:
        print(f"[ERROR] Failed to load image: {image_path}")
        sys.exit(1)
    
    base_name = os.path.splitext(os.path.basename(image_path))[0]
    process_and_analyze(model, img, base_name)

def main():
    # YOLOモデルをロード
    model = load_yolo_model()
    
    if len(sys.argv) < 2:
        # 引数なし: カメラモード
        camera_mode(model)
    else:
        # 引数あり: ファイルモード
        file_mode(model, sys.argv[1])

if __name__ == "__main__":
    main()
