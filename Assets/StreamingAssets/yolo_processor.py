"""
yolo_processor.py - YOLOオブジェクト検出とクロップ処理

YOLO26による物体検出を行い、検出結果をOllamaへのヒントとして提供。

検出数に応じたクロップロジック:
- 0検出: 元画像をそのまま返す
- 1検出: そのオブジェクトをクロップ（マージン付き）
- 2+検出: 全オブジェクトを含む最小バウンディングボックスでクロップ
"""

import cv2
import numpy as np
import logging
from ultralytics import YOLO

# YOLO-World用クラス定義をインポート
from yolo_world_classes import YOLO_WORLD_CLASSES

logger = logging.getLogger(__name__)


class YOLOProcessor:
    """YOLO26によるオブジェクト検出とクロップ処理"""
    
    def __init__(self, model_name="yolov8s-worldv2.pt", confidence_threshold=0.25, margin_ratio=0.1):
        """
        Args:
            model_name: 使用するYOLOモデル名 (デフォルト: yolo11n.pt)
            confidence_threshold: 検出の信頼度閾値
            margin_ratio: クロップ時のマージン比率 (0.1 = 10%)
        """
        self.model_name = model_name
        self.confidence_threshold = confidence_threshold
        self.margin_ratio = margin_ratio
        self.model = None
        self._is_initialized = False
    
    def initialize(self):
        """モデルを初期化（初回のみ）"""
        if self._is_initialized:
            return True
        
        logger.info(f"[YOLO-World] Loading model: {self.model_name}")
        try:
            self.model = YOLO(self.model_name)
            # YOLO-World: カスタムクラスを設定
            if "world" in self.model_name.lower():
                self.model.set_classes(YOLO_WORLD_CLASSES)
                logger.info(f"[YOLO-World] Set {len(YOLO_WORLD_CLASSES)} custom classes")
            self._is_initialized = True
            logger.info("[YOLO-World] Model loaded successfully")
            return True
        except Exception as e:
            logger.error(f"[YOLO] Failed to load model: {e}")
            return False
    
    def detect_and_crop(self, image: np.ndarray) -> tuple:
        """
        画像内のオブジェクトを検出し、適切にクロップする
        
        Args:
            image: 入力画像 (BGR形式のnumpy array)
        
        Returns:
            tuple: (cropped_image, detection_info)
                - cropped_image: クロップ済み画像
                - detection_info: 検出情報の辞書
        """
        if not self._is_initialized:
            if not self.initialize():
                return image, {"error": "Model initialization failed", "detection_count": 0}
        
        h, w = image.shape[:2]
        
        # YOLO推論
        logger.info("[YOLO] Running detection...")
        results = self.model(image, conf=self.confidence_threshold, verbose=False)
        
        # 検出結果を収集
        detections = []
        for result in results:
            boxes = result.boxes
            if boxes is not None:
                for box in boxes:
                    x1, y1, x2, y2 = map(int, box.xyxy[0].tolist())
                    conf = float(box.conf[0])
                    cls_id = int(box.cls[0])
                    cls_name = self.model.names[cls_id]
                    
                    detections.append({
                        "x1": x1, "y1": y1, "x2": x2, "y2": y2,
                        "confidence": conf,
                        "class_id": cls_id,
                        "class_name": cls_name,
                        "area": (x2 - x1) * (y2 - y1)
                    })
        
        detection_count = len(detections)
        logger.info(f"[YOLO] Detected {detection_count} objects")
        
        # 検出数に応じた処理
        if detection_count == 0:
            # 検出なし: 元画像をそのまま使用
            logger.info("[YOLO-World] No objects detected, using original image")
            return image, {
                "detection_count": 0,
                "crop_type": "none",
                "message": "No objects detected",
                "detected_classes": [],
                "primary_class": None,
                "primary_confidence": 0.0
            }
        
        elif detection_count == 1:
            # 単一検出: そのオブジェクトをクロップ
            det = detections[0]
            cropped, crop_box = self._crop_with_margin(image, det["x1"], det["y1"], det["x2"], det["y2"])
            logger.info(f"[YOLO-World] Single object crop: {det['class_name']} ({det['confidence']:.2f})")
            return cropped, {
                "detection_count": 1,
                "crop_type": "single",
                "detections": detections,
                "crop_box": crop_box,
                "detected_classes": [det["class_name"]],
                "primary_class": det["class_name"],
                "primary_confidence": det["confidence"]
            }
        
        else:
            # 複数検出: 全オブジェクトを含む統合バウンディングボックス
            min_x1 = min(d["x1"] for d in detections)
            min_y1 = min(d["y1"] for d in detections)
            max_x2 = max(d["x2"] for d in detections)
            max_y2 = max(d["y2"] for d in detections)
            
            cropped, crop_box = self._crop_with_margin(image, min_x1, min_y1, max_x2, max_y2)
            class_names = [d["class_name"] for d in detections]
            
            # 最も信頼度の高い検出を primary とする
            primary_det = max(detections, key=lambda d: d["confidence"])
            
            logger.info(f"[YOLO-World] Multi-object crop: {class_names}")
            return cropped, {
                "detection_count": detection_count,
                "crop_type": "multi",
                "detections": detections,
                "crop_box": crop_box,
                "detected_classes": class_names,
                "primary_class": primary_det["class_name"],
                "primary_confidence": primary_det["confidence"]
            }
    
    def _crop_with_margin(self, image: np.ndarray, x1: int, y1: int, x2: int, y2: int) -> tuple:
        """
        マージンを含めてクロップ
        
        Returns:
            tuple: (cropped_image, crop_box_dict)
        """
        h, w = image.shape[:2]
        
        # バウンディングボックスのサイズ
        box_w = x2 - x1
        box_h = y2 - y1
        
        # マージンを計算
        margin_x = int(box_w * self.margin_ratio)
        margin_y = int(box_h * self.margin_ratio)
        
        # マージンを適用（画像境界を超えないように）
        crop_x1 = max(0, x1 - margin_x)
        crop_y1 = max(0, y1 - margin_y)
        crop_x2 = min(w, x2 + margin_x)
        crop_y2 = min(h, y2 + margin_y)
        
        cropped = image[crop_y1:crop_y2, crop_x1:crop_x2]
        
        crop_box = {
            "x1": crop_x1, "y1": crop_y1,
            "x2": crop_x2, "y2": crop_y2,
            "width": crop_x2 - crop_x1,
            "height": crop_y2 - crop_y1
        }
        
        return cropped, crop_box
    
    def draw_detections(self, image: np.ndarray, detections: list) -> np.ndarray:
        """デバッグ用: 検出結果を画像に描画"""
        output = image.copy()
        for det in detections:
            x1, y1, x2, y2 = det["x1"], det["y1"], det["x2"], det["y2"]
            label = f"{det['class_name']} {det['confidence']:.2f}"
            cv2.rectangle(output, (x1, y1), (x2, y2), (0, 255, 0), 2)
            cv2.putText(output, label, (x1, y1 - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 2)
        return output


# モジュールテスト用
if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO, format='%(message)s')
    
    print("=== YOLO Processor テスト ===")
    
    processor = YOLOProcessor()
    
    # テスト画像を読み込み
    import sys
    if len(sys.argv) > 1:
        img = cv2.imread(sys.argv[1])
        if img is not None:
            cropped, info = processor.detect_and_crop(img)
            print(f"Detection Info: {info}")
            cv2.imwrite("yolo_cropped.jpg", cropped)
            print("Saved: yolo_cropped.jpg")
        else:
            print(f"Failed to load: {sys.argv[1]}")
    else:
        print("Usage: python yolo_processor.py <image_path>")
