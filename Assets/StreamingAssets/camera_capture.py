"""
camera_capture.py - カメラ制御モジュール

フリッカー対策付きのカメラ撮影機能を提供。
- 複数フレームの中央値を取ることでフリッカーを軽減
- カメラの露出安定を待ってから撮影
- OBS等の仮想カメラを自動除外（macOS対応）
- OBS仮想カメラプロセスを自動終了（macOS）
"""

import cv2
import numpy as np
import logging
import subprocess
import sys
import time

logger = logging.getLogger(__name__)

# 除外するカメラ名のキーワード（大文字小文字無視）
EXCLUDED_CAMERA_KEYWORDS = ["obs", "virtual", "screen capture"]

# 自動終了するプロセス名のパターン
KILL_PROCESS_PATTERNS = [
    "obs-studio.mac-camera-extension",
    "obs-mac-virtualcam",
]


def kill_virtual_camera_processes():
    """
    OBS仮想カメラ等の競合するプロセスを強制終了
    macOS専用
    """
    if sys.platform != "darwin":
        return
    
    for pattern in KILL_PROCESS_PATTERNS:
        try:
            # pkill -f でパターンマッチしたプロセスを終了
            result = subprocess.run(
                ["pkill", "-f", pattern],
                capture_output=True,
                text=True,
                timeout=5
            )
            if result.returncode == 0:
                logger.info(f"[[CAMERA]] 仮想カメラプロセスを終了: {pattern}")
            # returncode 1 = プロセスが見つからなかった（正常）
        except subprocess.TimeoutExpired:
            logger.warning(f"プロセス終了がタイムアウト: {pattern}")
        except Exception as e:
            logger.warning(f"プロセス終了に失敗: {pattern} - {e}")


# モジュール読み込み時にOBS仮想カメラを終了
kill_virtual_camera_processes()


def get_macos_cameras():
    """
    macOSでカメラデバイス名とインデックスのマッピングを取得
    Returns: list of dict [{"index": int, "name": str}, ...]
    """
    if sys.platform != "darwin":
        return []
    
    try:
        result = subprocess.run(
            ["system_profiler", "SPCameraDataType", "-json"],
            capture_output=True,
            text=True,
            timeout=5
        )
        import json
        data = json.loads(result.stdout)
        cameras = []
        
        if "SPCameraDataType" in data:
            for i, cam in enumerate(data["SPCameraDataType"]):
                name = cam.get("_name", f"Camera {i}")
                cameras.append({"index": i, "name": name})
                logger.info(f"macOS カメラ検出: Index {i} = {name}")
        
        return cameras
    except Exception as e:
        logger.warning(f"macOSカメラ情報の取得に失敗: {e}")
        return []


def find_physical_camera_index(exclude_keywords=EXCLUDED_CAMERA_KEYWORDS):
    """
    仮想カメラを除外して物理カメラのインデックスを検索
    
    OpenCVでカメラをスキャンし、FPSと解像度から物理カメラを判別。
    macOSのカメラ情報も参照して確実に仮想カメラを除外する。
    
    Returns:
        int: 物理カメラのインデックス、見つからない場合は0
    """
    # まずmacOSのカメラ名マッピングを取得（参考情報）
    macos_cameras = get_macos_cameras()
    camera_names = {}
    for cam in macos_cameras:
        camera_names[cam["index"]] = cam["name"]
    
    # OBS仮想カメラのインデックスを特定
    excluded_indices = set()
    for idx, name in camera_names.items():
        name_lower = name.lower()
        if any(kw in name_lower for kw in exclude_keywords):
            excluded_indices.add(idx)
            logger.info(f"[[CAMERA]] 仮想カメラを除外リストに追加: {name} (Index {idx})")
    
    # OpenCVでカメラをスキャン
    logger.info("[[CAMERA]] OpenCVでカメラをスキャン中...")
    candidates = []
    
    # macOSカメラ情報がある場合は、その情報に基づいてスキャン範囲を決定
    if macos_cameras:
        # 検出されたカメラのインデックスのみをスキャン（存在しないデバイスへのアクセスを避ける）
        scan_indices = [cam["index"] for cam in macos_cameras if cam["index"] not in excluded_indices]
        logger.info(f"[[CAMERA]] macOSカメラ情報に基づきスキャン: {scan_indices}")
    else:
        # macOSカメラ情報が取得できない場合はフォールバック（0-2のみ）
        scan_indices = [i for i in range(3) if i not in excluded_indices]
        logger.info(f"[[CAMERA]] フォールバックモード: インデックス {scan_indices} をスキャン")
    
    for i in scan_indices:
        cap = cv2.VideoCapture(i)
        if cap.isOpened():
            fps = cap.get(cv2.CAP_PROP_FPS)
            width = cap.get(cv2.CAP_PROP_FRAME_WIDTH)
            height = cap.get(cv2.CAP_PROP_FRAME_HEIGHT)
            
            # 追加チェック: 実際にフレームが取れるか
            ret, _ = cap.read()
            cap.release()
            
            if not ret:
                logger.info(f"[[CAMERA]] Index {i}: フレーム取得失敗、スキップ")
                continue
            
            name = camera_names.get(i, f"Unknown Camera {i}")
            
            # OBS仮想カメラの特徴: 
            # - FPSが0または非常に低い（30未満）場合がある
            # - 名前に "OBS" や "Virtual" が含まれる
            is_likely_virtual = fps < 5 or any(kw in name.lower() for kw in exclude_keywords)
            
            if is_likely_virtual:
                logger.info(f"[[CAMERA]] Index {i} は仮想カメラの可能性大（FPS={fps}, name={name}）、スキップ")
                continue
            
            logger.info(f"[[CAMERA]] 物理カメラ候補: Index {i} = {name} ({width:.0f}x{height:.0f} @ {fps:.1f}fps)")
            candidates.append({
                "index": i,
                "name": name,
                "fps": fps,
                "width": width,
                "height": height
            })
    
    if not candidates:
        logger.warning("[[CAMERA]] 物理カメラが見つかりません。Index 1を試します。")
        # OBS Virtual Cameraが Index 0 で、物理カメラが Index 1 の場合が多い
        return 1
    
    # 最高解像度のカメラを優先（物理カメラは通常高解像度）
    best = max(candidates, key=lambda c: c["width"] * c["height"])
    logger.info(f"[[CAMERA]] 選択: Index {best['index']} = {best['name']}")
    return best["index"]


class CameraCapture:
    """マニュアル撮影モード対応のカメラキャプチャクラス"""
    
    def __init__(self, camera_index=None, width=9999, height=9999, auto_detect=True, 
                 exposure=-30, contrast=35, saturation=None, brightness=15,
                 gain=0, white_balance=None):
        """
        マニュアル撮影モード: 全ての自動調整を無効にして手動で設定
        
        Args:
            camera_index: カメラデバイスのインデックス（Noneで自動検出）
            width: キャプチャ幅
            height: キャプチャ高さ
            auto_detect: Trueの場合、仮想カメラを除外して物理カメラを自動検出
            exposure: 露出/シャッタースピード（負の値=速いシャッター=暗い。デフォルト: -12）
            contrast: コントラスト（低いとのっぺり。デフォルト: 35）
            saturation: 彩度（低いと色が薄い。Noneでデフォルト）
            brightness: 明るさ（デフォルト: 35）
            gain: ゲイン/ISO感度（高いと明るいがノイズ増加。Noneでデフォルト）
            white_balance: ホワイトバランス色温度（2000-10000K程度。Noneでデフォルト）
        """
        if camera_index is None and auto_detect:
            self.camera_index = find_physical_camera_index()
            logger.info(f"カメラ自動検出: Index {self.camera_index}")
        else:
            self.camera_index = camera_index if camera_index is not None else 0
            
        self.width = width
        self.height = height
        self.exposure = exposure
        self.contrast = contrast
        self.saturation = saturation
        self.brightness = brightness
        self.gain = gain
        self.white_balance = white_balance
        self.cap = None
        self._is_initialized = False
    
    def initialize(self):
        """カメラを初期化（マニュアルモード）"""
        if self._is_initialized:
            return True
        
        logger.info(f"カメラ初期化中... (index={self.camera_index})")
        logger.info("[[CAMERA]] マニュアル撮影モード: 自動調整OFF")
        self.cap = cv2.VideoCapture(self.camera_index)
        
        if not self.cap.isOpened():
            logger.error("カメラを開けませんでした")
            return False
        
        # 解像度設定
        self.cap.set(cv2.CAP_PROP_FRAME_WIDTH, self.width)
        self.cap.set(cv2.CAP_PROP_FRAME_HEIGHT, self.height)
        
        # === マニュアルモード: 全ての自動調整を無効化 ===
        
        # 1. 自動露出OFF → 手動シャッタースピード
        self.cap.set(cv2.CAP_PROP_AUTO_EXPOSURE, 0.25)
        self.cap.set(cv2.CAP_PROP_EXPOSURE, self.exposure)
        logger.info(f"  シャッタースピード(露出): {self.exposure}")
        
        # 2. ゲイン（ISO感度相当）
        if self.gain is not None:
            self.cap.set(cv2.CAP_PROP_GAIN, self.gain)
            logger.info(f"  ゲイン(ISO): {self.gain}")
        
        # 3. 自動ホワイトバランスOFF → 手動色温度
        if self.white_balance is not None:
            self.cap.set(cv2.CAP_PROP_AUTO_WB, 0)  # 自動WB OFF
            self.cap.set(cv2.CAP_PROP_WB_TEMPERATURE, self.white_balance)
            logger.info(f"  ホワイトバランス: {self.white_balance}K")
        
        # 4. コントラスト
        if self.contrast is not None:
            self.cap.set(cv2.CAP_PROP_CONTRAST, self.contrast)
            logger.info(f"  コントラスト: {self.contrast}")
        
        # 5. 彩度
        if self.saturation is not None:
            self.cap.set(cv2.CAP_PROP_SATURATION, self.saturation)
            logger.info(f"  彩度: {self.saturation}")
        
        # 6. 明るさ
        if self.brightness is not None:
            self.cap.set(cv2.CAP_PROP_BRIGHTNESS, self.brightness)
            logger.info(f"  明るさ: {self.brightness}")
        
        # 実際の設定値を取得してログ出力
        actual_w = int(self.cap.get(cv2.CAP_PROP_FRAME_WIDTH))
        actual_h = int(self.cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
        fps = self.cap.get(cv2.CAP_PROP_FPS)
        actual_exposure = self.cap.get(cv2.CAP_PROP_EXPOSURE)
        actual_contrast = self.cap.get(cv2.CAP_PROP_CONTRAST)
        actual_saturation = self.cap.get(cv2.CAP_PROP_SATURATION)
        actual_brightness = self.cap.get(cv2.CAP_PROP_BRIGHTNESS)
        logger.info(f"カメラ初期化完了: {actual_w}x{actual_h} @ {fps}fps")
        logger.info(f"  露出: {actual_exposure}, コントラスト: {actual_contrast}, 彩度: {actual_saturation}, 明るさ: {actual_brightness}")
        
        # カメラウェイクアップ待機（スリープ復帰対策）
        logger.info("[[CAMERA]] カメラ安定化待機中...")
        time.sleep(0.5)
        
        # 初回フレーム取得でカメラを完全に起動
        for attempt in range(3):
            ret, _ = self.cap.read()
            if ret:
                logger.info("[[CAMERA]] カメラ準備完了")
                break
            time.sleep(0.2)
        else:
            logger.warning("[[CAMERA]] 初回フレーム取得に失敗（継続）")
        
        self._is_initialized = True
        return True

    
    def capture_with_stabilization(self, warmup_frames=5, capture_frames=5):
        """
        フリッカー対策付きの撮影
        
        Args:
            warmup_frames: 露出安定のために捨てるフレーム数
            capture_frames: 中央値計算に使うフレーム数
        
        Returns:
            numpy.ndarray: キャプチャした画像（BGR形式）
        """
        if not self._is_initialized:
            if not self.initialize():
                return None
        
        logger.info(f"撮影開始: ウォームアップ{warmup_frames}フレーム, 撮影{capture_frames}フレーム")
        
        max_retries = 2
        for retry in range(max_retries + 1):
            # 1. ウォームアップ（露出安定待ち）
            warmup_success = 0
            for i in range(warmup_frames):
                ret, _ = self.cap.read()
                if ret:
                    warmup_success += 1
                else:
                    logger.warning(f"ウォームアップフレーム{i}の取得失敗")
            
            # ウォームアップが全て失敗した場合はカメラを再初期化
            if warmup_success == 0 and retry < max_retries:
                logger.warning(f"[[CAMERA]] ウォームアップ全失敗、カメラ再初期化を試行 ({retry + 1}/{max_retries})")
                self.cap.release()
                time.sleep(0.5)
                self.cap = cv2.VideoCapture(self.camera_index)
                if not self.cap.isOpened():
                    logger.error("カメラ再オープン失敗")
                    continue
                time.sleep(0.3)
                continue
            
            # 2. 複数フレームを取得
            frames = []
            for i in range(capture_frames):
                ret, frame = self.cap.read()
                if ret:
                    frames.append(frame.astype(np.float32))
                else:
                    logger.warning(f"キャプチャフレーム{i}の取得失敗")
            
            if len(frames) > 0:
                break  # 成功
            
            if retry < max_retries:
                logger.warning(f"[[CAMERA]] キャプチャ失敗、リトライ ({retry + 1}/{max_retries})")
                time.sleep(0.3)
        
        if len(frames) == 0:
            logger.error("フレームを取得できませんでした（リトライ後も失敗）")
            return None
        
        # 3. 中央値を計算（フリッカー除去）
        # 中央値は外れ値に強く、蛍光灯のフリッカーに効果的
        median_frame = np.median(np.stack(frames), axis=0).astype(np.uint8)
        
        logger.info(f"撮影完了: {len(frames)}フレームから合成")
        return median_frame
    
    def capture_single(self):
        """
        単純な1フレームキャプチャ（高速だがフリッカー対策なし）
        """
        if not self._is_initialized:
            if not self.initialize():
                return None
        
        ret, frame = self.cap.read()
        if ret:
            return frame
        return None
    
    def release(self):
        """カメラリソースを解放"""
        if self.cap is not None:
            self.cap.release()
            self._is_initialized = False
            logger.info("カメラリソースを解放しました")
    
    def __enter__(self):
        self.initialize()
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        self.release()


# モジュールテスト用
if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO, format='%(message)s')
    
    print("=== カメラキャプチャテスト ===")
    
    with CameraCapture() as cam:
        print("\n1. フリッカー対策キャプチャ...")
        frame = cam.capture_with_stabilization()
        if frame is not None:
            cv2.imwrite("test_stabilized.jpg", frame)
            print(f"   保存完了: test_stabilized.jpg ({frame.shape})")
        
        print("\n2. 単純キャプチャ...")
        frame = cam.capture_single()
        if frame is not None:
            cv2.imwrite("test_single.jpg", frame)
            print(f"   保存完了: test_single.jpg ({frame.shape})")
    
    print("\n=== テスト完了 ===")
