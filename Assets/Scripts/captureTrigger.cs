using UnityEngine;

/// <summary>
/// キャプチャトリガー - スペースキーでPythonにキャプチャコマンドを送信
/// Python側でカメラ撮影とフリッカー対策を行う
/// </summary>
public class captureTrigger : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PythonLauncher pythonLauncher;
    [SerializeField] private FlowManager flowManager;

    [Header("Input Settings")]
    public KeyCode captureKey = KeyCode.Space;
    
    [Header("Cooldown Settings")]
    [Tooltip("連打防止のクールダウン時間（秒）")]
    [SerializeField] private float captureCooldown = 3.0f;
    
    // 最後のキャプチャ時刻
    private float lastCaptureTime = -999f;

    void Update()
    {
        // 指定されたキーが押された場合
        if (Input.GetKeyDown(captureKey))
        {
            TriggerCapture();
        }
    }
    
    [Header("Camera Logic")]
    [Tooltip("検索するカメラ名の一部（例: VID:1133）")]
    [SerializeField] private string targetCameraKeyword = "VID:1133";
    
    [Tooltip("キーワードが見つからない場合に使用するカメラインデックス")]
    [SerializeField] private int fallbackCameraIndex = 1;  // OBSを避けるため1をデフォルトに
    
    [Tooltip("これらのキーワードを含むカメラは強制的に除外（仮想カメラ対策）")]
    [SerializeField] private string[] excludeKeywords = new string[] { "OBS", "Virtual", "Screen Capture" };
    
    /// <summary>
    /// カメラ名が除外リストに含まれているかチェック
    /// </summary>
    private bool IsExcludedCamera(string cameraName)
    {
        foreach (string keyword in excludeKeywords)
        {
            if (cameraName.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }
        return false;
    }
    
    [ContextMenu("Debug: カメラ一覧を表示")]
    private void DebugListCameras()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        Debug.Log($"=== 接続中のカメラデバイス ({devices.Length}台) ===");
        for (int i = 0; i < devices.Length; i++)
        {
            bool excluded = IsExcludedCamera(devices[i].name);
            bool isTarget = devices[i].name.Contains(targetCameraKeyword);
            string marker = excluded ? " !EXCLUDED!" : (isTarget ? " ★TARGET★" : "");
            Debug.Log($"  Index {i}: {devices[i].name}{marker}");
        }
        Debug.Log($"現在のフォールバック: Index {fallbackCameraIndex}");
        Debug.Log($"除外キーワード: {string.Join(", ", excludeKeywords)}");
    }

    /// <summary>
    /// キャプチャをトリガーする（外部からも呼び出し可能）
    /// </summary>
    public void TriggerCapture()
    {
        // FlowStateがWaiting以外なら無視
        if (flowManager != null && flowManager.CurrentState != FlowManager.FlowState.Waiting)
        {
            Debug.Log($"[CaptureTrigger] Waiting以外のため無視 (現在: {flowManager.CurrentState})");
            return;
        }
        
        // クールダウンチェック
        if (Time.time - lastCaptureTime < captureCooldown)
        {
            return;
        }
        
        if (pythonLauncher == null) return;
        
        lastCaptureTime = Time.time;
        
        // カメラのインデックスを検索
        int cameraIndex = FindTargetCameraIndex();
        
        // コマンド送信 "CAPTURE <index>"
        string command = $"CAPTURE {cameraIndex}";
        Debug.Log($"[CaptureTrigger] 送信コマンド: {command} (Target: {targetCameraKeyword})");
        
        pythonLauncher.SendCommand(command);
        // ※Scanningへの遷移はPython側からの[[CAPTURE_DONE]]通知で行う
    }
    
    /// <summary>
    /// 目標のカメラ名を含むデバイスのインデックスを検索
    /// </summary>
    private int FindTargetCameraIndex()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0) return fallbackCameraIndex;
        
        int firstValidIndex = -1;  // 除外されていない最初のカメラ
        
        for (int i = 0; i < devices.Length; i++)
        {
            // 除外チェック（OBS等の仮想カメラを除外）
            if (IsExcludedCamera(devices[i].name))
            {
                Debug.Log($"[CaptureTrigger] 除外: {devices[i].name} (Index {i})");
                continue;
            }
            
            // 除外されていない最初のカメラを記録
            if (firstValidIndex < 0)
            {
                firstValidIndex = i;
            }
            
            // ターゲットキーワードが含まれているかチェック
            if (devices[i].name.Contains(targetCameraKeyword))
            {
                Debug.Log($"[CaptureTrigger] カメラ発見: {devices[i].name} (Index {i})");
                return i;
            }
        }
        
        // ターゲットが見つからない場合、除外されていない最初のカメラを使用
        if (firstValidIndex >= 0)
        {
            Debug.LogWarning($"[CaptureTrigger] キーワード '{targetCameraKeyword}' を含むカメラが見つかりません。有効な最初のカメラ(Index {firstValidIndex})を使用します。");
            return firstValidIndex;
        }
        
        Debug.LogWarning($"[CaptureTrigger] 有効なカメラが見つかりません。フォールバック({fallbackCameraIndex})を使用します。");
        return fallbackCameraIndex;
    }
    
    /// <summary>
    /// クールダウン残り時間を取得
    /// </summary>
    public float CooldownRemaining => Mathf.Max(0, captureCooldown - (Time.time - lastCaptureTime));
}