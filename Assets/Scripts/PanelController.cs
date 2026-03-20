using UnityEngine;

/// <summary>
/// UI Panelの表示/非表示の切り替え機能のみを担当するスクリプト。
/// 「いつ」切り替えるかは、他のスクリプト（フロー制御用）が決定する。
/// </summary>
public class PanelController : MonoBehaviour
{


    [Header("Main Canvas Settings")]
    [Tooltip("全てのパネルの親となるCanvasのTransform")]
    [SerializeField] private Transform mainCanvasRoot;

    [Header("Timeline State Prefabs")]
    // これらはシーン上のオブジェクトではなく、Projectウィンドウのプレハブをアサインする
    [SerializeField] private TimelineState prefabWaiting;
    [SerializeField] private TimelineState prefabScanning;
    [SerializeField] private TimelineState prefabScanComplete;
    [SerializeField] private TimelineState prefabMessage;
    [SerializeField] private TimelineState prefabEnd;

    // 実際に生成したインスタンスを保持する変数
    private TimelineState instanceWaiting;
    private TimelineState instanceScanning;
    private TimelineState instanceScanComplete;
    private TimelineState instanceMessage;
    private TimelineState instanceEnd;

    // ★追加: Message プレハブ内の PythonMessageTMP を取得するプロパティ
    public PythonMessageTMP MessageDisplay { get; private set; }

    // ★追加: ScanComplete プレハブ内の RuneSpawner を取得するプロパティ
    public RuneSpawner RuneSpawnerDisplay { get; private set; }

    // ★追加: Scanning プレハブ内の ScanningProgressController を取得するプロパティ
    public ScanningProgressController ScanningProgressDisplay { get; private set; }

    [Header("Skip Settings")]
    [Tooltip("ScanComplete Panelの表示をスキップするか")]
    [SerializeField] private bool skipScanComplete = false;
    [Tooltip("End Panelの表示をスキップするか")]
    [SerializeField] private bool skipEnd = false;

    private void Awake()
    {
        // プレハブからインスタンスを生成し、初期状態は非表示(Exit)にしておく
        if (mainCanvasRoot == null)
        {
            Debug.LogError("PanelController: MainCanvasRoot is not assigned!");
            return;
        }

        instanceWaiting = SetupInstance(prefabWaiting, "State_Waiting");
        instanceScanning = SetupInstance(prefabScanning, "State_Scanning");
        instanceScanComplete = SetupInstance(prefabScanComplete, "State_ScanComplete");
        instanceMessage = SetupInstance(prefabMessage, "State_Message");
        instanceEnd = SetupInstance(prefabEnd, "State_End");

        // ★追加: Message プレハブ内の PythonMessageTMP を取得
        if (instanceMessage != null)
        {
            MessageDisplay = instanceMessage.GetComponentInChildren<PythonMessageTMP>(true);
            if (MessageDisplay != null)
            {
                Debug.Log("[PanelController] PythonMessageTMP を検出しました。");
            }
            else
            {
                Debug.LogWarning("[PanelController] Message プレハブ内に PythonMessageTMP が見つかりません。");
            }
        }

        // ★追加: ScanComplete プレハブ内の RuneSpawner を取得
        if (instanceScanComplete != null)
        {
            RuneSpawnerDisplay = instanceScanComplete.GetComponentInChildren<RuneSpawner>(true);
            if (RuneSpawnerDisplay != null)
            {
                Debug.Log("[PanelController] RuneSpawner を検出しました。");
            }
            else
            {
                Debug.LogWarning("[PanelController] ScanComplete プレハブ内に RuneSpawner が見つかりません。");
            }
        }

        // ★追加: Scanning プレハブ内の ScanningProgressController を取得
        if (instanceScanning != null)
        {
            ScanningProgressDisplay = instanceScanning.GetComponentInChildren<ScanningProgressController>(true);
            if (ScanningProgressDisplay != null)
            {
                Debug.Log("[PanelController] ScanningProgressController を検出しました。");
            }
            else
            {
                Debug.LogWarning("[PanelController] Scanning プレハブ内に ScanningProgressController が見つかりません。");
            }
        }

        // 初期状態として待機画面を表示（FlowManagerがStartでChangeStateを呼ぶが、念のため）
        // HideAllPanels(); // SetupInstanceですでに非表示になっているので不要
    }

    /// <summary>
    /// プレハブを生成し、初期設定を行うヘルパー関数
    /// </summary>
    private TimelineState SetupInstance(TimelineState prefab, string name)
    {
        if (prefab == null) return null;

        TimelineState instance = Instantiate(prefab, mainCanvasRoot);
        instance.name = name;
        
        // RectTransformのズレを補正（画面いっぱいに広げるなど、必要に応じて）
        RectTransform rt = instance.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            // 必要ならanchorMin/Maxなども設定するが、プレハブ側で設定済みと仮定
        }

        instance.Exit(); // 最初は非表示
        return instance;
    }

    /// <summary>
    /// 全ての管理対象パネルを非表示(Exit)にする
    /// </summary>
    private void HideAllPanels()
    {
        if(instanceWaiting != null) instanceWaiting.Exit();
        if(instanceScanning != null) instanceScanning.Exit();
        if(instanceScanComplete != null) instanceScanComplete.Exit();
        if(instanceMessage != null) instanceMessage.Exit();
        if(instanceEnd != null) instanceEnd.Exit();
    }

    // --- ここから下は、他のスクリプトから呼び出して使用する ---

    /// <summary>
    /// 待機 (Waiting) Panel のみを表示する
    /// </summary>
    public void ShowWaitingPanel()
    {
        HideAllPanels();
        if (instanceWaiting != null) instanceWaiting.Enter();
    }

    /// <summary>
    /// スキャン中 (Scanning) Panel のみを表示する
    /// </summary>
    public void ShowScanningPanel()
    {
        HideAllPanels();
        if (instanceScanning != null) instanceScanning.Enter();
    }

    /// <summary>
    /// スキャン完了 (ScanComplete) Panel が表示されるかどうかを事前にチェック。
    /// FlowManager が状態遷移前にスキップ判定を行うために使用。
    /// </summary>
    /// <returns>表示される場合は true、スキップ設定の場合は false</returns>
    public bool WillShowScanCompletePanel()
    {
        return !skipScanComplete;
    }

    /// <summary>
    /// スキャン完了 (ScanComplete) Panel を表示する。
    /// スキップ設定が有効な場合は表示せず false を返す。
    /// </summary>
    /// <returns>パネルが表示された場合は true、スキップされた場合は false</returns>
    public bool ShowScanCompletePanel()
    {
        HideAllPanels();
        if (skipScanComplete)
        {
            return false; // スキップ
        }

        if (instanceScanComplete != null) instanceScanComplete.Enter();
        return true; // 表示
    }

    /// <summary>
    /// メッセージ (Message) Panel のみを表示する
    /// </summary>
    public void ShowMessagePanel()
    {
        HideAllPanels();
        if (instanceMessage != null) instanceMessage.Enter();
    }

    /// <summary>
    /// 終了 (End) Panel を表示する。
    /// スキップ設定が有効な場合は表示せず false を返す。
    /// </summary>
    /// <returns>パネルが表示された場合は true、スキップされた場合は false</returns>
    public bool ShowEndPanel()
    {
        HideAllPanels();
        if (skipEnd)
        {
            return false; // スキップ
        }

        if (instanceEnd != null) instanceEnd.Enter();
        return true; // 表示
    }
}