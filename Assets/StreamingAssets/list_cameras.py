"""
list_cameras.py - 接続されているカメラデバイスを一覧表示
"""
import cv2
import subprocess
import sys

def list_cameras_opencv():
    """OpenCVで検出可能なカメラをリストアップ"""
    print("=== OpenCV カメラ検出 ===")
    available_cameras = []
    
    for i in range(10):  # 0-9 までチェック
        cap = cv2.VideoCapture(i)
        if cap.isOpened():
            # カメラの情報を取得
            width = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
            height = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
            fps = cap.get(cv2.CAP_PROP_FPS)
            
            # テストフレームを取得
            ret, frame = cap.read()
            frame_ok = "OK" if ret and frame is not None else "NG"
            
            print(f"  Index {i}: {width}x{height} @ {fps}fps [Frame: {frame_ok}]")
            available_cameras.append(i)
            cap.release()
    
    if not available_cameras:
        print("  検出されたカメラがありません")
    
    return available_cameras

def list_cameras_mac():
    """macOS固有: system_profilerでカメラ名を取得"""
    print("\n=== macOS カメラデバイス一覧 ===")
    try:
        result = subprocess.run(
            ["system_profiler", "SPCameraDataType"],
            capture_output=True,
            text=True
        )
        print(result.stdout)
    except Exception as e:
        print(f"  エラー: {e}")

if __name__ == "__main__":
    print("カメラデバイス検索中...\n")
    
    cameras = list_cameras_opencv()
    
    if sys.platform == "darwin":  # macOS
        list_cameras_mac()
    
    print("\n=== 推奨設定 ===")
    if cameras:
        print(f"利用可能なカメラインデックス: {cameras}")
        if len(cameras) > 1:
            print(f"OBSを避けるには、インデックス 1 以降を試してください")
            print(f"例: Unityから 'CAPTURE 1' を送信")
    else:
        print("カメラが見つかりませんでした")
