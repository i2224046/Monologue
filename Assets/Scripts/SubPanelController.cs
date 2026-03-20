using UnityEngine;
using TMPro;

/// <summary>
/// サブディスプレイ用コントローラー。
/// 表示エリアを統一し、フローに応じて表示方式を切り替える。
/// - Message フロー: logItemPrefab を使用して表示（タイプライター効果付き）
/// - その他のフロー: 単一 TMP にテキスト表示
/// </summary>
public class SubPanelController : MonoBehaviour
{
    [Header("Unified Display Area")]
    [Tooltip("統一表示エリア（ログとステータス両方に使用）")]
    [SerializeField] private TextMeshProUGUI unifiedDisplayTMP;

    [Header("Message Log (Prefab Mode)")]
    [Tooltip("Message 用ログアイテムのプレハブ（TextMeshProUGUIを持つ）")]
    [SerializeField] private GameObject logItemPrefab;

    [Tooltip("logItemPrefab を追加するコンテナ")]
    [SerializeField] private Transform prefabContainer;

    // 現在の表示モード
    private enum DisplayMode { Status, Message }
    private DisplayMode currentMode = DisplayMode.Status;

    // 生成されたプレハブインスタンス
    private GameObject currentPrefabInstance;

    // 保存されたメッセージ（FlowState.Message 時に表示）
    private string pendingMessage = "";
    private string pendingCredit = "";
    private bool hasPendingMessage = false;

    private void Start()
    {
        // 初期状態: ステータスモード
        SwitchToStatusMode();
    }

    /// <summary>
    /// ステータス（Pythonログ）を更新する。Scanning 等で使用。
    /// 単一 TMP にテキストを直接表示する。
    /// </summary>
    /// <param name="status">表示するログ文字列（空文字でクリア）</param>
    public void SetStatus(string status)
    {
        // Message モード中は SetStatus を無視（プレハブを保護）
        if (currentMode == DisplayMode.Message)
        {
            Debug.Log("[SubPanelController] Message モード中のため SetStatus を無視");
            return;
        }

        // Message モードの場合は Status モードに切り替え
        if (currentMode != DisplayMode.Status)
        {
            SwitchToStatusMode();
        }

        if (unifiedDisplayTMP != null)
        {
            if (string.IsNullOrEmpty(status))
            {
                unifiedDisplayTMP.text = "";
            }
            else
            {
                // 既存テキストに追記（新しいログが上）
                if (string.IsNullOrEmpty(unifiedDisplayTMP.text))
                {
                    unifiedDisplayTMP.text = status;
                }
                else
                {
                    // 最大行数を制限（5行程度）
                    string[] lines = unifiedDisplayTMP.text.Split('\n');
                    if (lines.Length >= 5)
                    {
                        // 古い行を削除
                        string trimmed = string.Join("\n", lines, 0, 4);
                        unifiedDisplayTMP.text = status + "\n" + trimmed;
                    }
                    else
                    {
                        unifiedDisplayTMP.text = status + "\n" + unifiedDisplayTMP.text;
                    }
                }
            }
        }
    }

    /// <summary>
    /// ログエントリを保存する。Message フローで使用。
    /// 実際の表示は ShowMessage() で行う。
    /// </summary>
    /// <param name="message">メッセージ本文</param>
    /// <param name="credit">クレジット情報（CV名など）</param>
    public void AddLogEntry(string message, string credit = "")
    {
        Debug.Log($"[SubPanelController] メッセージ保存: {message} / Credit: {credit}");

        // メッセージを保存（表示は ShowMessage() で行う）
        pendingMessage = message;
        pendingCredit = credit;
        hasPendingMessage = true;
    }

    /// <summary>
    /// FlowState.Message 時に呼び出される。
    /// 保存されたメッセージをプレハブで表示し、タイプライター効果を開始する。
    /// </summary>
    public void ShowMessage()
    {
        Debug.Log($"[SubPanelController] ShowMessage: hasPending={hasPendingMessage}, message='{pendingMessage}'");

        if (!hasPendingMessage)
        {
            Debug.LogWarning("[SubPanelController] ShowMessage: 保存されたメッセージがありません");
            return;
        }

        // Message モードに切り替え
        SwitchToMessageMode();

        // プレハブが設定されている場合
        if (logItemPrefab != null && prefabContainer != null)
        {
            // 既存のプレハブインスタンスを削除（単一表示）
            ClearPrefabInstance();

            // 新しいインスタンスを生成
            currentPrefabInstance = Instantiate(logItemPrefab, prefabContainer);
            
            // プレハブ内のTMPを取得してテキスト設定
            TextMeshProUGUI tmp = currentPrefabInstance.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                string displayText = string.IsNullOrEmpty(pendingCredit) 
                    ? pendingMessage 
                    : $"{pendingMessage}\n<size=70%><color=#888888>{pendingCredit}</color></size>";
                tmp.text = displayText;

                // タイプライター効果を開始
                TypewriterEffectTMP typewriter = currentPrefabInstance.GetComponentInChildren<TypewriterEffectTMP>();
                if (typewriter != null)
                {
                    typewriter.StartDisplay();
                    Debug.Log("[SubPanelController] タイプライター効果を開始");
                }
                else
                {
                    Debug.Log("[SubPanelController] TypewriterEffectTMP が見つかりません（即座に表示）");
                }
            }
        }
        // フォールバック: 統一TMPに表示
        else if (unifiedDisplayTMP != null)
        {
            string entry = string.IsNullOrEmpty(pendingCredit) 
                ? pendingMessage 
                : $"{pendingMessage} ({pendingCredit})";
            unifiedDisplayTMP.text = entry;
        }
        else
        {
            Debug.LogWarning("[SubPanelController] 表示手段がありません。unifiedDisplayTMP または logItemPrefab を設定してください。");
        }

        // 保存メッセージをクリア
        hasPendingMessage = false;
    }

    /// <summary>
    /// メッセージ表示を終了し、ステータスモードに戻る。
    /// FlowState が Waiting に戻るときに呼び出される。
    /// </summary>
    public void HideMessage()
    {
        Debug.Log("[SubPanelController] HideMessage: メッセージ表示を終了");
        SwitchToStatusMode();
    }

    /// <summary>
    /// アーカイブをクリアする（デバッグ用）
    /// </summary>
    public void ClearArchive()
    {
        ClearPrefabInstance();

        if (unifiedDisplayTMP != null)
        {
            unifiedDisplayTMP.text = "";
        }

        // 保存メッセージもクリア
        pendingMessage = "";
        pendingCredit = "";
        hasPendingMessage = false;

        Debug.Log("[SubPanelController] アーカイブをクリアしました。");
    }

    /// <summary>
    /// ステータスモードに切り替え（TMP表示）
    /// </summary>
    private void SwitchToStatusMode()
    {
        currentMode = DisplayMode.Status;

        // プレハブインスタンスを削除
        ClearPrefabInstance();

        // TMP を表示
        if (unifiedDisplayTMP != null) unifiedDisplayTMP.gameObject.SetActive(true);

        // テキストをクリア
        if (unifiedDisplayTMP != null) unifiedDisplayTMP.text = "";

        Debug.Log("[SubPanelController] ステータスモードに切り替え");
    }

    /// <summary>
    /// メッセージモードに切り替え（プレハブ表示）
    /// </summary>
    private void SwitchToMessageMode()
    {
        currentMode = DisplayMode.Message;

        // TMP を非表示（プレハブを表示するため）
        if (unifiedDisplayTMP != null) unifiedDisplayTMP.gameObject.SetActive(false);

        Debug.Log("[SubPanelController] メッセージモードに切り替え");
    }

    /// <summary>
    /// プレハブインスタンスを削除
    /// </summary>
    private void ClearPrefabInstance()
    {
        if (currentPrefabInstance != null)
        {
            Destroy(currentPrefabInstance);
            currentPrefabInstance = null;
        }
    }
}
