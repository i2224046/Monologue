using System.Collections;
using System.IO;
using UnityEngine;
using System.Collections.Generic; // List のために残していますが、必須ではありません

public class WebCamCapture : MonoBehaviour
{
    WebCamTexture webCamTexture;

    [Header("Capture Settings")]
    public string saveSubFolder = "capture";

    [Header("Device Settings")]
    [Tooltip("この名前のカメラを優先的に使用します。")]
    // ★ 優先カメラ名を指定し、デフォルトをOBSに設定
    public string preferredCameraName = "OBS Virtual Camera";

    [Tooltip("カメラの初期化を待つ最大時間（秒）")]
    public float initializationTimeout = 10.0f;

    private bool isCameraReady = false;

    IEnumerator Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("利用可能なウェブカメラがありません。");
            yield break;
        }

        Debug.Log("--- 利用可能なカメラデバイス ---");
        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log($"Index {i}: {devices[i].name}");
        }
        Debug.Log("-------------------------------");

        string deviceName = ""; // 使用するデバイス名

        // --- ★ 優先カメラ名での検索 (除外リストのロジックから変更) ---
        if (!string.IsNullOrEmpty(preferredCameraName))
        {
            bool found = false;
            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i].name == preferredCameraName)
                {
                    deviceName = devices[i].name;
                    found = true;
                    Debug.Log($"優先カメラ '{deviceName}' (Index {i}) を選択しました。");
                    break;
                }
            }
            if (!found)
            {
                Debug.LogError($"指定されたカメラ '{preferredCameraName}' が見つかりません。利用可能なカメラを確認してください。");
                yield break; // 見つからなければ処理を中断
            }
        }
        else
        {
            // 優先カメラ名が空欄の場合は、0番目を使用 (フォールバック)
            Debug.LogWarning("優先カメラ名が指定されていません。Index 0 のカメラを使用します。");
            deviceName = devices[0].name;
        }
        // --- ★ ここまで ---

        // --- 初期化処理 ---
        webCamTexture = new WebCamTexture(deviceName);
        webCamTexture.Play();
        Debug.Log($"使用中のカメラ: {deviceName}。初期化待機中...");

        // (macOS対策の初期化待機はそのまま)
        float startTime = Time.time;
        while (webCamTexture.width <= 160 && (Time.time - startTime) < initializationTimeout)
        {
            yield return null;
        }

        if ((Time.time - startTime) >= initializationTimeout)
        {
            Debug.LogError($"カメラの初期化がタイムアウトしました（{initializationTimeout}秒）。");
            webCamTexture.Stop();
            yield break;
        }

        if (!webCamTexture.isPlaying)
        {
            Debug.LogError("カメラの再生に失敗しました。");
            yield break;
        }

        Debug.Log($"カメラ初期化完了。解像度: {webCamTexture.width}x{webCamTexture.height}");
        isCameraReady = true;
    }

    /// <summary>
    /// キャプチャを実行し、リサイズせずに保存する
    /// </summary>
    public void CaptureAndSave()
    {
        if (!isCameraReady || !webCamTexture.isPlaying)
        {
            Debug.LogWarning("カメラの準備ができていません（初期化中または失敗）。");
            return;
        }

        // (以降の処理は変更なし)
        RenderTexture rt = RenderTexture.GetTemporary(webCamTexture.width, webCamTexture.height);
        Graphics.Blit(webCamTexture, rt);

        Texture2D originalTexture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        originalTexture.ReadPixels(new Rect(0, 0, webCamTexture.width, webCamTexture.height), 0, 0);
        originalTexture.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        byte[] bytes = originalTexture.EncodeToPNG();
        Destroy(originalTexture);

        string saveDirectory = Path.Combine(Application.streamingAssetsPath, saveSubFolder);

        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        string filePath = Path.Combine(saveDirectory, "capture_" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".png");

        File.WriteAllBytes(filePath, bytes);
        Debug.Log("Saved to: " + filePath);
    }
}