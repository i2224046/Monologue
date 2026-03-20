using UnityEngine;
using TMPro;
using System.Collections;
using System;

public class TypewriterEffectTMP : MonoBehaviour
{
    public TextMeshProUGUI tmpText;
    public float delay = 0.05f;

    private string fullText;
    private Coroutine typewriterCoroutine;

    /// <summary>
    /// 文字がタイプされるたびに発火するイベント
    /// TypingSoundPlayerなど外部から購読可能
    /// </summary>
    public event Action OnCharacterTyped;

    /// <summary>
    /// タイピングが完了した時に発火するイベント
    /// </summary>
    public event Action OnTypingComplete;

    /// <summary>
    /// 現在タイピング中かどうか
    /// </summary>
    public bool IsTyping { get; private set; }

    void Awake()
    {
        if (tmpText == null)
        {
            tmpText = GetComponent<TextMeshProUGUI>();
        }
    }

    // void Start() { StartDisplay(); } // ← この自動実行ロジックは削除またはコメントアウト

    /// <summary>
    /// 外部から呼ばれ、現在のtmpText.textを元にタイプライター処理を開始する（最初から）
    /// </summary>
    public void StartDisplay()
    {
        StartDisplayFromIndex(0);
    }

    /// <summary>
    /// 指定したインデックスからタイプライター処理を開始する
    /// MessageHistoryDisplayなどで「追加された部分だけ」タイピングする場合に使用
    /// </summary>
    /// <param name="startIndex">タイピングを開始する文字インデックス（この位置までは即座に表示）</param>
    public void StartDisplayFromIndex(int startIndex)
    {
        if (tmpText == null) return;

        // 既存のコルーチンを停止
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        // ★重要: このメソッドが呼ばれた瞬間のテキストを全文として取得
        fullText = tmpText.text;
        
        // 表示を確実に更新
        tmpText.ForceMeshUpdate();

        // startIndexが範囲外の場合は補正
        int totalChars = tmpText.textInfo.characterCount;
        startIndex = Mathf.Clamp(startIndex, 0, totalChars);

        Debug.Log($"[TypewriterEffect] StartDisplayFromIndex: StartIndex={startIndex}, TotalChars={totalChars}");

        // 開始位置まで即座に表示
        tmpText.maxVisibleCharacters = startIndex;

        // コルーチンを開始
        typewriterCoroutine = StartCoroutine(ShowTextFromIndex(startIndex, totalChars));
    }

    /// <summary>
    /// 指定したインデックスからテキストを表示するコルーチン
    /// </summary>
    private IEnumerator ShowTextFromIndex(int startIndex, int totalChars)
    {
        IsTyping = true;

        for (int i = startIndex; i <= totalChars; i++)
        {
            tmpText.maxVisibleCharacters = i;

            // 文字がタイプされたことをイベントで通知（startIndex以降で発火）
            if (i > startIndex)
            {
                OnCharacterTyped?.Invoke();
            }

            if (i >= totalChars)
            {
                break;
            }

            yield return new WaitForSeconds(delay);
        }

        IsTyping = false;
        OnTypingComplete?.Invoke();
    }

    /// <summary>
    /// タイピングを停止する
    /// </summary>
    public void StopTyping()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        IsTyping = false;
    }
}