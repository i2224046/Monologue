using UnityEngine;
using System.IO;

/// <summary>
/// Message.txt にメッセージを追記する静的ユーティリティクラス。
/// MonoBehaviourではないため、Inspectorでの割り当て不要。
/// </summary>
public static class MessageFileWriter
{
    private static readonly string FILE_NAME = "Message.txt";

    /// <summary>
    /// メッセージを Message.txt に追記する
    /// </summary>
    /// <param name="message">メッセージ本文</param>
    /// <param name="credit">クレジット情報（省略可）</param>
    public static void Write(string message, string credit = "")
    {
        if (string.IsNullOrEmpty(message)) return;

        try
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, FILE_NAME);
            string entry = string.IsNullOrEmpty(credit)
                ? $"[[INFO]] {message}"
                : $"[[INFO]] {message} ({credit})";

            File.AppendAllText(filePath, entry + "\n");
            Debug.Log($"[MessageFileWriter] 追記完了: {entry}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MessageFileWriter] 書き込みエラー: {e.Message}");
        }
    }

    /// <summary>
    /// ファイルをクリアする
    /// </summary>
    public static void Clear()
    {
        try
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, FILE_NAME);
            File.WriteAllText(filePath, "");
            Debug.Log("[MessageFileWriter] ファイルをクリアしました。");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MessageFileWriter] クリアエラー: {e.Message}");
        }
    }
}
