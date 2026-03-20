using UnityEngine;
using TMPro;
using System.IO;

// セリフ用TMPと、タイプライターエフェクトは必須
[RequireComponent(typeof(TextMeshProUGUI))]
[RequireComponent(typeof(TypewriterEffectTMP))]
public class PythonMessageTMP : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField, Tooltip("メインのセリフを表示するTMP（自動取得）")]
    private TextMeshProUGUI textMessageTMP; // メインのセリフ

    [SerializeField, Tooltip("★ここに追加したCreditTextをドラッグ＆ドロップしてください")]
    public TextMeshProUGUI creditTextTMP;   // ★追加：クレジット表示用

    private string currentMessage = "";
    private string currentCredit = "";
    
    // Member variables
    private TypewriterEffectTMP typewriterEffect;
    private string logFilePath;

    void Awake()
    {
        // メインのTMPは同じオブジェクトにある前提
        if (textMessageTMP == null)
        {
            textMessageTMP = GetComponent<TextMeshProUGUI>();
        }
        typewriterEffect = GetComponent<TypewriterEffectTMP>();
        
        // Log file setup...
        if (string.IsNullOrEmpty(logFilePath))
        {
            logFilePath = Path.Combine(Application.streamingAssetsPath, "Message.txt");
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
        }
    }

    void OnEnable()
    {
        // パネルが表示されたタイミングで、保持しているテキストを反映
        if (textMessageTMP != null) textMessageTMP.text = currentMessage;
        if (creditTextTMP != null) creditTextTMP.text = currentCredit;

        Debug.Log($"[PythonMessageTMP] OnEnable: Restoring Text -> Main='{currentMessage}', Credit='{currentCredit}'");
    }

    // ★追加：クレジット情報をセットする関数
    // Pythonからの [[CREDIT]] ... を受け取ったらこれを呼ぶ
    public void SetCredit(string creditInfo)
    {
        Debug.Log($"[PythonMessageTMP] クレジット更新リクエスト: {creditInfo}");
        currentCredit = creditInfo; // 内部変数に保存

        if (creditTextTMP != null)
        {
            creditTextTMP.text = currentCredit;
            Debug.Log($"[PythonMessageTMP] テキスト更新完了: {creditTextTMP.text}");
        }
    }

    // 既存：メッセージ受信関数
    public void ReceiveMessage(string messageLine)
    {
        currentMessage = messageLine; // 内部変数に保存

        // Log writing is improved to be safe
        WriteToFile(messageLine);

        if (textMessageTMP != null)
        {
            textMessageTMP.text = currentMessage;
        }
    }

    private void WriteToFile(string text)
    {
        // Awakeが呼ばれていない（非アクティブの）場合でも書き込めるように初期化チェック
        if (string.IsNullOrEmpty(logFilePath))
        {
            logFilePath = Path.Combine(Application.streamingAssetsPath, "Message.txt");
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
        }

        try
        {
            File.AppendAllText(logFilePath, text + System.Environment.NewLine);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to write to log file: {e.Message}");
        }
    }

    public void StartTypewriter()
    {
        if (typewriterEffect != null)
        {
            typewriterEffect.StartDisplay();
        }
    }
}