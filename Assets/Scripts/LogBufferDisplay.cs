using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// ログのバッファ管理と表示を担当するスクリプト。
/// 最大行数を超えた場合は古いログから削除される。
/// </summary>
public class LogBufferDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    [Tooltip("ログを表示するTMP")]
    [SerializeField] private TextMeshProUGUI displayText;

    [Tooltip("最大表示行数")]
    [SerializeField] private int maxLines = 5;

    [Tooltip("行間の改行数")]
    [SerializeField] private int lineSpacing = 1;

    [Header("Animation (Optional)")]
    [Tooltip("スライドアニメーション（未設定でも動作）")]
    [SerializeField] private SlideAnimation slideAnimation;

    // ログバッファ（先頭が古い、末尾が新しい）
    private Queue<string> logBuffer = new Queue<string>();

    private void Start()
    {
        // 初期化時にテキストをクリア
        if (displayText != null)
        {
            displayText.text = "";
        }
    }

    /// <summary>
    /// ログを追加する。最大行数を超えた場合は古いログが削除される。
    /// </summary>
    /// <param name="log">追加するログ文字列</param>
    public void AddLog(string log)
    {
        if (string.IsNullOrEmpty(log)) return;

        // バッファに追加
        logBuffer.Enqueue(log);

        // 最大行数を超えた場合は古いログを削除
        while (logBuffer.Count > maxLines)
        {
            logBuffer.Dequeue();
        }

        // テキストを更新
        UpdateDisplay();

        // アニメーション実行（設定されている場合）
        if (slideAnimation != null)
        {
            slideAnimation.SlideIn();
        }
    }

    /// <summary>
    /// バッファの内容をTMPに反映する
    /// </summary>
    private void UpdateDisplay()
    {
        if (displayText == null) return;

        string separator = new string('\n', lineSpacing);
        displayText.text = string.Join(separator, logBuffer);
    }

    /// <summary>
    /// ログバッファをクリアする
    /// </summary>
    public void ClearLogs()
    {
        logBuffer.Clear();
        if (displayText != null)
        {
            displayText.text = "";
        }
    }

    /// <summary>
    /// 現在のログ数を取得
    /// </summary>
    public int LogCount => logBuffer.Count;
}
