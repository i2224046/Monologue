using UnityEngine;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class InputExitHandler : MonoBehaviour
{
    // Xキー連続押しの判定用
    private int xPressCount = 0;
    private float lastXPressTime = 0f;
    private const float triplePressThreshold = 0.5f; // 連続とみなす秒数

    // 削除対象の画像拡張子
    private readonly string[] imageExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".tga" };
    // 削除対象の音声拡張子（必要に応じて追加してください）
    private readonly string[] audioExtensions = { ".wav", ".mp3", ".ogg" };

    void Update()
    {
        // ESCキーで終了
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitApplication();
        }

        // Xキーの3回連続押し判定
        if (Input.GetKeyDown(KeyCode.X))
        {
            float currentTime = Time.time;

            if (currentTime - lastXPressTime <= triplePressThreshold)
            {
                xPressCount++;
            }
            else
            {
                // 時間が空きすぎたらリセット
                xPressCount = 1;
            }

            lastXPressTime = currentTime;

            if (xPressCount >= 3)
            {
                Debug.Log("Xキーが3回押されました。ファイルを削除して終了します。");

                // 画像の削除
                DeleteFiles("capture", imageExtensions);
                DeleteFiles("capture/raw", imageExtensions); // raw画像も削除
                // 音声の削除
                DeleteFiles("voice", audioExtensions);
                // ペアデータの削除
                DeleteMessagePairsJson();

                QuitApplication();
                xPressCount = 0; // リセット
            }
        }
    }

    /// <summary>
    /// 指定したフォルダ内の特定の拡張子を持つファイルを削除する
    /// </summary>
    /// <param name="folderName">StreamingAssets以下のフォルダ名</param>
    /// <param name="targetExtensions">削除対象の拡張子配列</param>
    private void DeleteFiles(string folderName, string[] targetExtensions)
    {
        string targetPath = Path.Combine(Application.streamingAssetsPath, folderName);

        if (!Directory.Exists(targetPath))
        {
            Debug.LogWarning($"ディレクトリが存在しません: {targetPath}");
            return;
        }

        try
        {
            // 許可された拡張子を持つファイルのみを検索
            var files = Directory.GetFiles(targetPath)
                .Where(file => targetExtensions.Contains(Path.GetExtension(file).ToLower()));

            int deleteCount = 0;
            foreach (string file in files)
            {
                File.Delete(file);
                deleteCount++;
            }
            Debug.Log($"フォルダ '{folderName}' : {deleteCount} 件のファイルを削除しました。");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"フォルダ '{folderName}' のファイル削除中にエラーが発生しました: {e.Message}");
        }
    }

    /// <summary>
    /// MessagePairs.jsonを削除する
    /// </summary>
    private void DeleteMessagePairsJson()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "MessagePairs.json");

        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                Debug.Log("MessagePairs.json を削除しました。");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"MessagePairs.json の削除中にエラーが発生しました: {e.Message}");
            }
        }
        else
        {
            Debug.Log("MessagePairs.json は存在しません。");
        }
    }

    /// <summary>
    /// アプリケーションを終了する
    /// </summary>
    private void QuitApplication()
    {
#if UNITY_EDITOR
        // Unityエディタの場合
        EditorApplication.isPlaying = false;
#else
        // ビルド後のアプリケーションの場合
        Application.Quit();
#endif
    }
}