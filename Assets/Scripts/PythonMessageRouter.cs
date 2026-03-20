using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Pythonからのメッセージを解析し、各コンポーネントに振り分けるルーター。
/// FlowManagerから解析ロジックを分離し、単一責任の原則を実現。
/// </summary>
public class PythonMessageRouter : MonoBehaviour
{
    [Header("State Management")]
    [Tooltip("状態遷移を管理するFlowManager")]
    [SerializeField] private FlowManager flowManager;

    [Header("Panel Controller (自動取得用)")]
    [Tooltip("PanelController - ここからMessageDisplayとRuneSpawnerを自動取得")]
    [SerializeField] private PanelController panelController;

    [Header("Message Display")]
    [Tooltip("メインディスプレイのメッセージ表示（未設定ならPanelControllerから自動取得）")]
    [SerializeField] private PythonMessageTMP pythonMessageDisplay;

    [Header("Sub Display (Optional)")]
    [Tooltip("サブディスプレイ用コントローラー")]
    [SerializeField] private SubPanelController subPanelController;

    [Header("Rune Effect (Optional)")]
    [Tooltip("ルーンエフェクト（未設定ならPanelControllerから自動取得）")]
    [SerializeField] private RuneSpawner runeSpawner;
    [Tooltip("チェックON: 全ログをRuneに転送 / OFF: [[MESSAGE]]のみ")]
    [SerializeField] private bool useAllPythonLogsForRune = false;

    [Header("Scanning Progress (Optional)")]
    [Tooltip("Scanning状態の進捗表示コントローラー")]
    [SerializeField] private ScanningProgressController scanningProgressController;

    [Header("Scanning Content Rotator (Optional)")]
    [Tooltip("Scanning状態のコンテンツローテーター")]
    [SerializeField] private ScanningContentRotator scanningContentRotator;

    // 現在のメッセージとクレジットを保持
    private string currentMessage = "";
    private string currentCredit = "";
    private string currentCharacter = "";

    // RuneSpawner取得前に受信したメッセージをバッファ
    private string pendingRuneMessage = null;

    // FlowManagerへのイベント
    public event Action OnScanStartDetected;
    public event Action OnScanCompleteDetected;

    void Start()
    {
        // PanelControllerからの自動取得を試みる（1フレーム待機）
        if (panelController != null)
        {
            StartCoroutine(TryGetDisplaysFromPanelController());
        }
    }

    private IEnumerator TryGetDisplaysFromPanelController()
    {
        yield return null; // 1フレーム待機（PanelControllerのStart完了を待つ）

        // PythonMessageTMP の自動取得
        if (pythonMessageDisplay == null && panelController != null)
        {
            pythonMessageDisplay = panelController.MessageDisplay;
            if (pythonMessageDisplay != null)
            {
                Debug.Log("[Router] PanelController から PythonMessageTMP を自動取得しました。");
            }
        }

        // RuneSpawner の自動取得
        if (runeSpawner == null && panelController != null)
        {
            runeSpawner = panelController.RuneSpawnerDisplay;
            if (runeSpawner != null)
            {
                Debug.Log("[Router] PanelController から RuneSpawner を自動取得しました。");

                // バッファにメッセージがあれば送信
                if (!string.IsNullOrEmpty(pendingRuneMessage))
                {
                    Debug.Log($"[Router] バッファのメッセージをRuneSpawnerに送信: {pendingRuneMessage}");
                    runeSpawner.SetMessage(pendingRuneMessage);
                    pendingRuneMessage = null;
                }
            }
        }

        // ScanningProgressController の自動取得
        if (scanningProgressController == null && panelController != null)
        {
            scanningProgressController = panelController.ScanningProgressDisplay;
            if (scanningProgressController != null)
            {
                Debug.Log("[Router] PanelController から ScanningProgressController を自動取得しました。");
            }
        }
    }

    /// <summary>
    /// PythonLauncherから呼び出されるメイン処理
    /// </summary>
    public void OnPythonOutput(string line)
    {
        if (string.IsNullOrEmpty(line)) return;

        Debug.Log($"[Router受信] {line}");

        // デバッグ用: 全ログをRuneSpawnerに転送
        if (useAllPythonLogsForRune)
        {
            if (runeSpawner != null)
            {
                runeSpawner.SetMessage(line);
            }
            else
            {
                // まだ取得されていない場合はバッファに保存
                pendingRuneMessage = line;
                Debug.Log($"[Router] RuneSpawner未取得のためバッファに保存: {line}");
            }
        }

        // タグ解析と振り分け
        // [[CAPTURE_DONE]]: キャプチャ完了 → Scanning状態へ遷移
        if (line.Contains("[[CAPTURE_DONE]]"))
        {
            HandleScanStart(line);
        }
        else if (line.Contains("[[STATE_START]]") || line.Contains("Analyzing image (Local Ollama):"))
        {
            // 処理開始ログ（Scanning遷移は[[CAPTURE_DONE]]で行うため、ここでは遷移しない）
            HandleOtherLog(line);
        }
        else if (line.Contains("[[OLLAMA_START]]"))
        {
            // Ollama処理開始（初期化待ち）→ フェーズ進行
            if (scanningProgressController != null) scanningProgressController.AdvancePhase();
        }
        else if (line.Contains("[[OLLAMA_PROGRESS]]"))
        {
            // Ollamaストリーミング進捗 → フェーズ進行
            if (scanningProgressController != null) scanningProgressController.AdvancePhase();
        }
        else if (line.Contains("[[OLLAMA ANALYSIS]]"))
        {
            // Ollama分析完了 → フェーズ進行
            if (scanningProgressController != null) scanningProgressController.AdvancePhase();
            HandleOtherLog(line);
        }
        else if (line.Contains("[[DEEPSEEK"))
        {
            // DeepSeek処理開始 → フェーズ進行
            if (scanningProgressController != null) scanningProgressController.AdvancePhase();
            HandleOtherLog(line);
        }
        else if (line.Contains("[[CHARACTER]]"))
        {
            HandleCharacter(line);
        }
        else if (line.Contains("[[CREDIT]]"))
        {
            HandleCredit(line);
        }
        else if (line.Contains("[[MESSAGE]]"))
        {
            HandleMessage(line);
        }
        else if (line.Contains("[[ITEM_IDENTIFIED]]"))
        {
            HandleItemIdentified(line);
        }
        else if (IsDeepSeekApiResponse(line))
        {
            // DeepSeek API レスポンスを検出
            if (line.Contains("200 OK"))
            {
                HandleScanComplete(line);
            }
            else
            {
                // 4xx / 5xx エラーの場合
                HandleApiError(line);
            }
        }
        else
        {
            HandleOtherLog(line);
        }
    }

    /// <summary>
    /// Pythonエラー処理
    /// </summary>
    public void OnPythonError(string errorMessage)
    {
        Debug.LogError($"[Router] Pythonエラー: {errorMessage}");
        if (flowManager != null)
        {
            flowManager.OnPythonError(errorMessage);
        }
    }

    // --- 個別ハンドラ ---

    private void HandleScanStart(string line)
    {
        // サブディスプレイにログ転送
        if (subPanelController != null) subPanelController.SetStatus(line);

        // FlowManagerに状態遷移を通知
        OnScanStartDetected?.Invoke();
        if (flowManager != null) flowManager.NotifyScanStart();
    }

    private void HandleItemIdentified(string line)
    {
        string itemName = line.Replace("[[ITEM_IDENTIFIED]]", "").Trim();
        Debug.Log($"[Router] アイテム識別完了: {itemName}");

        // フェーズ進行（YOLO完了）
        if (scanningProgressController != null) scanningProgressController.AdvancePhase();

        // ScanningContentRotator に通知 → Phase 2 ローテーション開始
        if (scanningContentRotator != null)
        {
            scanningContentRotator.SetItemIdentified(itemName);
        }
    }

    private void HandleCharacter(string line)
    {
        string charBody = line.Replace("[[CHARACTER]]", "").Replace("[[INFO]]", "").Trim();
        Debug.Log($"[Router] キャラ名受信: {charBody}");
        currentCharacter = charBody;
        
        // キャラ名が存在すれば「by キャラクター名」を設定
        if (!string.IsNullOrEmpty(currentCharacter))
        {
            currentCredit = $"by {currentCharacter}";
            Debug.Log($"[Router] クレジット構築: {currentCredit}");
            
            if (pythonMessageDisplay != null)
            {
                pythonMessageDisplay.SetCredit(currentCredit);
            }
        }
    }

    private void HandleCredit(string line)
    {
        // [[INFO]] タグも除去する
        string creditBody = line.Replace("[[CREDIT]]", "").Replace("[[INFO]]", "").Trim();
        
        // (Role: ...) 部分を除去
        creditBody = System.Text.RegularExpressions.Regex.Replace(creditBody, @"\s*\(Role:\s*[^)]*\)", "").Trim();
        
        // キャラ名がある場合は結合
        if (!string.IsNullOrEmpty(currentCharacter))
        {
            currentCredit = $"by {currentCharacter}｜{creditBody}";
        }
        else
        {
            currentCredit = creditBody;
        }
        
        Debug.Log($"[Router] クレジット受信: {currentCredit}");

        if (pythonMessageDisplay != null)
        {
            pythonMessageDisplay.SetCredit(currentCredit);
        }
    }

    private void HandleMessage(string line)
    {
        // [[INFO]] タグも除去する
        string messageBody = line.Replace("[[MESSAGE]]", "").Replace("[[INFO]]", "").Trim();
        Debug.Log($"[Router] メッセージ受信: {messageBody}");

        currentMessage = messageBody;

        // メインディスプレイ
        if (pythonMessageDisplay != null)
        {
            pythonMessageDisplay.ReceiveMessage(messageBody);
        }

        // RuneSpawner (PanelControllerから最新のインスタンスを取得)
        RuneSpawner activeRuneSpawner = runeSpawner;
        if (activeRuneSpawner == null && panelController != null)
        {
            activeRuneSpawner = panelController.RuneSpawnerDisplay;
        }

        if (activeRuneSpawner != null)
        {
            activeRuneSpawner.SetMessage(messageBody);
        }
        else
        {
            // インスタンスがない場合はバッファ(自動取得用)
            // ※ただしPanelControllerがインスタンスを生成するタイミングで
            //   バッファを渡すロジックが必要になる可能性がある
            pendingRuneMessage = messageBody;
        }

        // サブディスプレイ（過去ログをクリアして新規追加）
        if (subPanelController != null)
        {
            subPanelController.ClearArchive();
            subPanelController.AddLogEntry(messageBody, currentCredit);
        }

        // ファイル書き込み
        MessageFileWriter.Write(messageBody, currentCredit);

        // ★メッセージ受信完了をFlowManagerに通知 → Message状態へ遷移
        if (flowManager != null)
        {
            flowManager.NotifyMessageReady();
        }
    }

    /// <summary>
    /// DeepSeek APIへのHTTPレスポンスかどうかを判定
    /// </summary>
    private bool IsDeepSeekApiResponse(string line)
    {
        return line.Contains("https://api.deepseek.com/chat/completions") && line.Contains("HTTP");
    }

    /// <summary>
    /// API通信エラー時の処理（4xx/5xxエラー）
    /// </summary>
    private void HandleApiError(string line)
    {
        Debug.LogWarning($"[Router] DeepSeek APIエラー検出: {line}");
        
        // サブディスプレイにエラー表示
        if (subPanelController != null) subPanelController.SetStatus($"[API Error] {line}");
        
        // FlowManagerにエラーを通知（Waiting状態に戻る）
        if (flowManager != null) flowManager.OnPythonError($"DeepSeek API Error: {line}");
    }

    private void HandleScanComplete(string line)
    {
        // サブディスプレイにログ転送
        if (subPanelController != null) subPanelController.SetStatus(line);

        // FlowManagerに状態遷移を通知
        OnScanCompleteDetected?.Invoke();
        if (flowManager != null) flowManager.NotifyScanComplete();
        // ※ForceSpawnRuneNowは削除: メッセージはHandleMessageで処理される
    }

    private void ForceSpawnRuneNow()
    {
        Debug.Log("[Router] ForceSpawnRuneNow: 開始");

        RuneSpawner activeRuneSpawner = null;
        if (panelController != null) activeRuneSpawner = panelController.RuneSpawnerDisplay;

        if (activeRuneSpawner == null)
        {
            Debug.LogWarning("[Router] PanelControllerからRuneSpawnerを取得できませんでした。Inspector設定またはシーン検索を試みます。");
            
            // 1. Inspector設定 / 自動取得キャッシュ
            activeRuneSpawner = runeSpawner;

            // 2. それでもなければシーン全体から検索 (IncludeInactive=true)
            if (activeRuneSpawner == null)
            {
                activeRuneSpawner = FindFirstObjectByType<RuneSpawner>(FindObjectsInactive.Include);
                if (activeRuneSpawner != null)
                {
                    Debug.Log("[Router] シーン内から RuneSpawner を発見しました (Fallback Search)");
                    // キャッシュ更新
                    runeSpawner = activeRuneSpawner;
                }
            }
        }

        if (activeRuneSpawner == null)
        {
            string pcStatus = (panelController == null) ? "null" : "assigned";
            Debug.LogError($"[Router] エラー: RuneSpawnerが見つかりません (PanelController={pcStatus})");
        }

        if (activeRuneSpawner != null)
        {
            // メッセージ決定: pendingがあればそれ、なければcurrent
            string msgToUse = !string.IsNullOrEmpty(pendingRuneMessage) ? pendingRuneMessage : currentMessage;

            Debug.Log($"[Router] 強制実行: Target={activeRuneSpawner.name}, Message='{msgToUse}', Pending='{pendingRuneMessage}', Current='{currentMessage}'");

            if (!string.IsNullOrEmpty(msgToUse))
            {
                activeRuneSpawner.SetMessage(msgToUse);
            }
            else
            {
                Debug.LogWarning("[Router] 強制実行しようとしましたが、メッセージがありません。");
            }
        }
    }

    private void HandleOtherLog(string line)
    {
        // Scanning/ScanComplete中のみサブディスプレイに転送
        // FlowManagerに現在の状態を問い合わせる
        if (flowManager != null && flowManager.IsInScanningPhase)
        {
            if (subPanelController != null) subPanelController.SetStatus(line);
        }
    }
}
