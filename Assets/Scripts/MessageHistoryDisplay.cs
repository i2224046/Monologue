using UnityEngine;
using TMPro;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Message.txt の内容を表示するスクリプト。
/// Waiting 状態でランダムなメッセージを無限にタイピング表示する。
/// TypewriterEffectTMPを使用してタイピングを統一。
/// </summary>
[RequireComponent(typeof(TypewriterEffectTMP))]
public class MessageHistoryDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    [Tooltip("表示する最大行数")]
    [SerializeField] private int maxLines = 10;

    [Tooltip("次の行を表示するまでの待機時間（秒）")]
    [SerializeField] private float nextLineInterval = 2.0f;

    [Header("File Settings")]
    [Tooltip("読み込むファイル名")]
    [SerializeField] private string fileName = "Message.txt";

    // ファイルパス
    private string FilePath => Path.Combine(Application.streamingAssetsPath, fileName);

    private Coroutine loopCoroutine;
    private List<string> displayedLines = new List<string>();
    
    // TypewriterEffectTMPへの参照
    private TypewriterEffectTMP typewriterEffect;
    private TextMeshProUGUI historyText;

    void Awake()
    {
        typewriterEffect = GetComponent<TypewriterEffectTMP>();
        if (typewriterEffect != null)
        {
            historyText = typewriterEffect.tmpText;
        }
    }

    /// <summary>
    /// ランダムメッセージの表示ループを開始する
    /// </summary>
    public void ShowHistory()
    {
        if (typewriterEffect == null || historyText == null)
        {
            Debug.LogError("[MessageHistoryDisplay] TypewriterEffectTMP または TextMeshProUGUI が見つかりません。");
            return;
        }

        // 既存のループがあれば停止
        StopLoop();

        loopCoroutine = StartCoroutine(RandomLogLoop());
    }

    /// <summary>
    /// ランダムな行を読み込んでタイピング表示するループ
    /// </summary>
    private IEnumerator RandomLogLoop()
    {
        // 初期クリア
        historyText.text = "";
        historyText.maxVisibleCharacters = 0;
        displayedLines.Clear();

        // ファイル読み込み
        string[] allLines = ReadAllLines();
        if (allLines == null || allLines.Length == 0)
        {
            Debug.LogWarning("[MessageHistoryDisplay] 表示するメッセージがありません。");
            yield break;
        }

        Debug.Log($"[MessageHistoryDisplay] 表示ループ開始: {allLines.Length} 件のメッセージ候補");

        while (true)
        {
            // 1. ランダムに行を選択
            string randomLine = allLines[Random.Range(0, allLines.Length)];
            
            // [[INFO]] 等のタグ処理（必要に応じて）
            string processedLine = randomLine.Replace("[[INFO]] ", "");

            // 2. リストに追加
            displayedLines.Add(processedLine);
            if (displayedLines.Count > maxLines)
            {
                displayedLines.RemoveAt(0); // 古い行を削除
            }

            // 3. テキスト更新
            string fullText = string.Join("\n", displayedLines);
            historyText.text = fullText;
            
            // 4. タイピングアニメーション（TypewriterEffectTMPを使用）
            // 現在の表示文字数を計算（最後の行以外）
            int previousLength = 0;
            if (displayedLines.Count > 1)
            {
                // 最後の行を除いたテキストの長さを取得
                string prevText = string.Join("\n", displayedLines.Take(displayedLines.Count - 1));
                previousLength = prevText.Length + 1; // +1 は改行コード分
            }

            // TypewriterEffectTMPでタイピング開始（途中から）
            typewriterEffect.StartDisplayFromIndex(previousLength);

            // タイピング完了を待つ
            while (typewriterEffect.IsTyping)
            {
                yield return null;
            }

            // 5. 待機
            yield return new WaitForSeconds(nextLineInterval);
        }
    }

    private string[] ReadAllLines()
    {
        if (!File.Exists(FilePath)) return null;
        try
        {
            return File.ReadAllLines(FilePath)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MessageHistoryDisplay] 読み込みエラー: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 履歴表示を停止しクリアする
    /// </summary>
    public void HideHistory()
    {
        StopLoop();
        if (typewriterEffect != null)
        {
            typewriterEffect.StopTyping();
        }
        if (historyText != null)
        {
            historyText.text = "";
        }
        Debug.Log("[MessageHistoryDisplay] 表示を終了しました。");
    }

    private void StopLoop()
    {
        if (loopCoroutine != null)
        {
            StopCoroutine(loopCoroutine);
            loopCoroutine = null;
        }
    }
}

