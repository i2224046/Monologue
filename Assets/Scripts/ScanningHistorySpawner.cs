using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Scanning状態中に過去のMessagePairsデータをウィンドウとして段階的に表示する。
/// ウィンドウの出現間隔は指数減衰で漸近的に加速する。
/// </summary>
public class ScanningHistorySpawner : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("HistoryWindow プレハブ（RawImage + TMP + TMP + CanvasGroup + MessageWindowAnimation）")]
    [SerializeField] private GameObject windowPrefab;

    [Header("Spawn Area")]
    [Tooltip("ウィンドウを生成する親コンテナ")]
    [SerializeField] private RectTransform spawnContainer;

    [Header("Asymptotic Acceleration (指数減衰間隔)")]
    [Tooltip("最初のウィンドウ出現間隔（秒）")]
    [SerializeField] private float startInterval = 5.0f;

    [Tooltip("最小間隔（漸近先、到達しない）")]
    [SerializeField] private float minInterval = 1.0f;

    [Tooltip("半減期（秒）- この時間で残り距離の半分縮まる")]
    [SerializeField] private float halfLife = 10.0f;

    [Header("Limits")]
    [Tooltip("同時表示最大ウィンドウ数")]
    [SerializeField] private int maxWindows = 6;

    [Header("Spawn Position")]
    [Tooltip("ランダム配置のマージン（端からの余白ピクセル）")]
    [SerializeField] private float positionMargin = 50f;

    // 内部状態
    private List<MessagePairData> pairs = new List<MessagePairData>();
    private List<GameObject> activeWindows = new List<GameObject>();
    private Coroutine spawnCoroutine;
    private int spawnIndex = 0;

    private string MessagePairsPath => Path.Combine(Application.streamingAssetsPath, "MessagePairs.json");
    private string CaptureDir => Path.Combine(Application.streamingAssetsPath, "capture");

    /// <summary>
    /// ウィンドウ生成を開始する
    /// </summary>
    public void StartSpawning()
    {
        // 既存を停止
        StopSpawning();

        // データ読み込み
        LoadMessagePairs();

        if (pairs.Count == 0)
        {
            Debug.Log("[ScanningHistorySpawner] MessagePairs.json にデータがありません。スキップします。");
            return;
        }

        // シャッフル
        ShuffleList(pairs);
        spawnIndex = 0;

        // 生成コルーチン開始
        spawnCoroutine = StartCoroutine(SpawnLoop());
        Debug.Log($"[ScanningHistorySpawner] 開始: {pairs.Count}件のデータ (間隔 {startInterval}s → {minInterval}s, 半減期 {halfLife}s)");
    }

    /// <summary>
    /// ウィンドウ生成を停止し、全ウィンドウをフェードアウトして破棄する
    /// </summary>
    public void StopSpawning()
    {
        // コルーチン停止
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        // 全ウィンドウをDismiss
        foreach (var window in activeWindows)
        {
            if (window != null)
            {
                MessageWindowAnimation anim = window.GetComponent<MessageWindowAnimation>();
                if (anim != null)
                {
                    anim.Dismiss();
                }
                else
                {
                    Destroy(window);
                }
            }
        }
        activeWindows.Clear();

        Debug.Log("[ScanningHistorySpawner] 停止・全ウィンドウ破棄");
    }

    private IEnumerator SpawnLoop()
    {
        float elapsedTime = 0f;

        while (true)
        {
            // 指数減衰による間隔計算
            // interval = minInterval + (startInterval - minInterval) * 0.5^(t / halfLife)
            float decay = Mathf.Pow(0.5f, elapsedTime / halfLife);
            float currentInterval = minInterval + (startInterval - minInterval) * decay;

            // 待機
            yield return new WaitForSeconds(currentInterval);
            elapsedTime += currentInterval;

            // 最大数チェック: 古いウィンドウを削除
            while (activeWindows.Count >= maxWindows)
            {
                DismissOldest();
            }

            // ウィンドウ生成
            SpawnWindow();
        }
    }

    private void SpawnWindow()
    {
        if (windowPrefab == null || spawnContainer == null) return;

        // 次のデータを取得（ループ）
        MessagePairData pair = pairs[spawnIndex];
        spawnIndex = (spawnIndex + 1) % pairs.Count;

        // プレハブ生成
        GameObject window = Instantiate(windowPrefab, spawnContainer);

        // 既存の履歴ウィンドウより手前、他のUI要素より奥に配置
        int insertIndex = 0;
        foreach (var w in activeWindows)
        {
            if (w != null)
            {
                int sibIdx = w.transform.GetSiblingIndex();
                if (sibIdx >= insertIndex) insertIndex = sibIdx + 1;
            }
        }
        window.transform.SetSiblingIndex(insertIndex);

        // ランダム位置に配置
        RectTransform windowRT = window.GetComponent<RectTransform>();
        if (windowRT != null)
        {
            Vector2 containerSize = spawnContainer.rect.size;
            // ウィンドウのスケール考慮後のサイズ（概算）
            float windowWidth = windowRT.rect.width * windowRT.localScale.x;
            float windowHeight = windowRT.rect.height * windowRT.localScale.y;

            float halfW = (containerSize.x - windowWidth - positionMargin * 2f) / 2f;
            float halfH = (containerSize.y - windowHeight - positionMargin * 2f) / 2f;

            // 範囲が負にならないよう保護
            halfW = Mathf.Max(halfW, 0f);
            halfH = Mathf.Max(halfH, 0f);

            float x = Random.Range(-halfW, halfW);
            float y = Random.Range(-halfH, halfH);
            windowRT.anchoredPosition = new Vector2(x, y);
        }

        // UI要素にデータを設定
        SetWindowContent(window, pair);

        activeWindows.Add(window);
        Debug.Log($"[ScanningHistorySpawner] ウィンドウ生成: {pair.credit} ({activeWindows.Count}/{maxWindows})");
    }

    private void SetWindowContent(GameObject window, MessagePairData pair)
    {
        // メッセージテキスト（最初に見つかったTMP）
        TextMeshProUGUI[] tmps = window.GetComponentsInChildren<TextMeshProUGUI>(true);

        if (tmps.Length >= 1)
        {
            // 1つ目のTMP: メッセージ
            tmps[0].text = $"「{pair.message}」";
        }
        if (tmps.Length >= 2)
        {
            // 2つ目のTMP: credit
            tmps[1].text = pair.credit ?? "";
        }

        // 画像（RawImage）
        RawImage rawImage = window.GetComponentInChildren<RawImage>(true);
        if (rawImage != null && !string.IsNullOrEmpty(pair.image))
        {
            string imagePath = Path.Combine(CaptureDir, pair.image);
            StartCoroutine(LoadImageAsync(rawImage, imagePath));
        }
    }

    private IEnumerator LoadImageAsync(RawImage rawImage, string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[ScanningHistorySpawner] 画像が見つかりません: {path}");
            if (rawImage != null) rawImage.enabled = false;
            yield break;
        }

        byte[] imageData = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        if (tex.LoadImage(imageData))
        {
            if (rawImage != null)
            {
                rawImage.texture = tex;
                rawImage.enabled = true;
            }
        }
        yield return null;
    }

    private void DismissOldest()
    {
        if (activeWindows.Count == 0) return;

        // nullチェック（既にDismissで破棄済みの場合）
        CleanupNullWindows();

        if (activeWindows.Count == 0) return;

        GameObject oldest = activeWindows[0];
        activeWindows.RemoveAt(0);

        if (oldest != null)
        {
            MessageWindowAnimation anim = oldest.GetComponent<MessageWindowAnimation>();
            if (anim != null)
            {
                anim.Dismiss();
            }
            else
            {
                Destroy(oldest);
            }
        }
    }

    private void CleanupNullWindows()
    {
        activeWindows.RemoveAll(w => w == null);
    }

    private void LoadMessagePairs()
    {
        pairs.Clear();

        if (!File.Exists(MessagePairsPath))
        {
            Debug.Log("[ScanningHistorySpawner] MessagePairs.json が見つかりません。");
            return;
        }

        try
        {
            string json = File.ReadAllText(MessagePairsPath);
            MessagePairData[] loadedPairs = JsonHelper.FromJson<MessagePairData>(json);
            if (loadedPairs != null)
            {
                pairs.AddRange(loadedPairs);
            }
            Debug.Log($"[ScanningHistorySpawner] {pairs.Count}件のペアデータを読み込みました。");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ScanningHistorySpawner] JSONパースエラー: {e.Message}");
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void OnDisable()
    {
        StopSpawning();
    }
}
