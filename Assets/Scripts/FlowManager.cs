using UnityEngine;

/// <summary>
/// 体験フローの状態(シーケンス)を管理する。
/// ※リファクタリング後：状態管理とパネル制御のみを担当。
/// メッセージ解析はPythonMessageRouterが担当。
/// </summary>
public class FlowManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PanelController panelController;
    [SerializeField] private PythonMessageTMP pythonMessageDisplay;

    [Header("Sub Display (Optional)")]
    [Tooltip("サブディスプレイ用コントローラー。未設定でも動作します。")]
    [SerializeField] private SubPanelController subPanelController;

    [Tooltip("名言カード表示。未設定でも動作します。")]
    [SerializeField] private QuoteCardDisplay quoteCardDisplay;

    // 各状態の固定表示時間（秒）
    private const float STATE_DURATION = 10.0f;
    private float pendingMessageDuration = -1.0f;

    public enum FlowState
    {
        Waiting,
        Scanning,
        ScanComplete,
        Message,
        End
    }

    private FlowState currentState;

    /// <summary>
    /// 現在Scanning/ScanComplete中かどうか（PythonMessageRouterが参照）
    /// </summary>
    public bool IsInScanningPhase => currentState == FlowState.Scanning || currentState == FlowState.ScanComplete;

    /// <summary>
    /// 現在の状態を取得
    /// </summary>
    public FlowState CurrentState => currentState;

    void Start()
    {
        // 初期状態として待機画面を表示
        ChangeState(FlowState.Waiting);
    }

    // --- 外部からの状態遷移通知 ---

    /// <summary>
    /// スキャン開始を通知（PythonMessageRouterから呼び出される）
    /// </summary>
    public void NotifyScanStart()
    {
        if (currentState == FlowState.Waiting)
        {
            Debug.Log("[FlowManager] 検知: Scanning開始");
            ChangeState(FlowState.Scanning);
        }
    }

    // ScanCompleteスキップ時にScanning状態を維持するフラグ
    private bool isScanCompleteSkipped = false;

    /// <summary>
    /// スキャン完了を通知（PythonMessageRouterから呼び出される）
    /// </summary>
    public void NotifyScanComplete()
    {
        if (currentState == FlowState.Scanning)
        {
            Debug.Log("[FlowManager] 検知: ScanComplete");
            
            // パネルがスキップ設定の場合は状態を変更しない
            if (panelController != null && !panelController.WillShowScanCompletePanel())
            {
                Debug.Log("[FlowManager] ScanCompleteパネルがスキップ設定のため、Scanning状態を維持");
                isScanCompleteSkipped = true;
                // 状態は変更せず、Message受信を待つ
                return;
            }
            
            isScanCompleteSkipped = false;
            ChangeState(FlowState.ScanComplete);
        }
    }

    /// <summary>
    /// メッセージ受信完了を通知（PythonMessageRouterから呼び出される）
    /// ScanComplete状態（またはスキップ時のScanning状態）からMessage状態へ遷移
    /// </summary>
    public void NotifyMessageReady()
    {
        // ScanCompleteがスキップされた場合、Scanning状態からも遷移可能
        if (currentState == FlowState.ScanComplete || 
            (currentState == FlowState.Scanning && isScanCompleteSkipped))
        {
            Debug.Log("[FlowManager] 検知: MessageReady → Message状態へ遷移");
            isScanCompleteSkipped = false;
            ChangeState(FlowState.Message);
        }
        else
        {
            Debug.LogWarning($"[FlowManager] NotifyMessageReady: 現在の状態({currentState})がScanComplete/Scanning(skipped)ではないため無視");
        }
    }

    /// <summary>
    /// Pythonエラー時の処理
    /// </summary>
    public void OnPythonError(string errorMessage)
    {
        Debug.LogError($"[FlowManager] Pythonエラー: {errorMessage}");
        ChangeState(FlowState.Waiting);
    }

    /// <summary>
    /// 外部（MessageVoicePlayerなど）からMessage状態の表示時間を指定
    /// </summary>
    public void SetMessageDuration(float clipLength)
    {
        float newDuration = clipLength + 2.0f;
        Debug.Log($"[FlowManager] MessageDuration 更新: 音声{clipLength}s -> 表示{newDuration}s");

        if (currentState == FlowState.Message)
        {
            CancelInvoke(nameof(OnMessageFinished));
            Invoke(nameof(OnMessageFinished), newDuration);
        }
        else
        {
            pendingMessageDuration = newDuration;
        }
    }

    // --- 状態遷移 ---

    private void ChangeState(FlowState newState)
    {
        CancelInvoke();
        currentState = newState;
        Debug.Log($"--- FlowState 変更: {currentState} ---");

        if (currentState == FlowState.Scanning)
        {
            pendingMessageDuration = -1.0f;
        }

        switch (currentState)
        {
            case FlowState.Waiting:
                panelController.ShowWaitingPanel();
                if (subPanelController != null) subPanelController.HideMessage();
                if (quoteCardDisplay != null) quoteCardDisplay.ShowQuoteCard();
                break;

            case FlowState.Scanning:
                panelController.ShowScanningPanel();
                if (quoteCardDisplay != null) quoteCardDisplay.HideQuoteCard();
                break;

            case FlowState.ScanComplete:
                panelController.ShowScanCompletePanel();
                // ※自動遷移は削除: [[MESSAGE]]受信時にNotifyMessageReady()で遷移する
                break;

            case FlowState.Message:
                panelController.ShowMessagePanel();
                if (subPanelController != null) subPanelController.ShowMessage();
                if (pythonMessageDisplay != null)
                {
                    pythonMessageDisplay.StartTypewriter();
                }

                float duration = STATE_DURATION;
                if (pendingMessageDuration > 0)
                {
                    duration = pendingMessageDuration;
                    pendingMessageDuration = -1.0f;
                    Debug.Log($"[FlowManager] 音声時間適用: {duration}s");
                }
                Invoke(nameof(OnMessageFinished), duration);
                break;

            case FlowState.End:
                if (subPanelController != null) subPanelController.SetStatus("");
                bool endDisplayed = panelController.ShowEndPanel();
                float endDelay = endDisplayed ? 5.0f : 0.1f;
                Invoke(nameof(OnEndFinished), endDelay);
                break;
        }
    }

    // --- Invokeコールバック ---

    private void OnCompleteFinished()
    {
        ChangeState(FlowState.Message);
    }

    private void OnMessageFinished()
    {
        ChangeState(FlowState.End);
    }

    private void OnEndFinished()
    {
        ChangeState(FlowState.Waiting);
    }
}