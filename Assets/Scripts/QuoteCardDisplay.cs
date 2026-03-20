using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Waiting状態で画像と名言のペアを表示するコンポーネント。
/// MessagePairs.jsonから読み込み、ランダムに切り替え表示する。
/// </summary>
public class QuoteCardDisplay : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("画像を表示するRawImage")]
    [SerializeField] private RawImage quoteImage;

    [Tooltip("名言テキスト")]
    [SerializeField] private TextMeshProUGUI quoteText;

    [Tooltip("著者名テキスト（by OO）")]
    [SerializeField] private TextMeshProUGUI authorText;

    [Tooltip("吹き出し背景画像（Waiting状態のみ表示）")]
    [SerializeField] private Image speechBubbleImage;

    [Header("Settings")]
    [Tooltip("次のカードに切り替えるまでの時間（秒）")]
    [SerializeField] private float displayInterval = 5.0f;

    // ファイルパス
    private string MessagePairsPath => Path.Combine(Application.streamingAssetsPath, "MessagePairs.json");
    private string CaptureDir => Path.Combine(Application.streamingAssetsPath, "capture");

    // ペアデータリスト
    private List<MessagePairData> pairs = new List<MessagePairData>();
    private Coroutine displayCoroutine;

    /// <summary>
    /// 名言カード表示を開始する
    /// </summary>
    public void ShowQuoteCard()
    {
        // データ読み込み
        LoadMessagePairs();

        if (pairs.Count == 0)
        {
            Debug.LogWarning("[QuoteCardDisplay] 表示するペアデータがありません。");
            ClearDisplay();
            return;
        }

        // 既存のコルーチンを停止
        StopLoop();

        // 吹き出し背景を表示
        if (speechBubbleImage != null) speechBubbleImage.gameObject.SetActive(true);

        // 表示ループ開始
        displayCoroutine = StartCoroutine(DisplayLoop());
        Debug.Log($"[QuoteCardDisplay] 表示開始: {pairs.Count}件のペアデータ");
    }

    /// <summary>
    /// 名言カード表示を停止する
    /// </summary>
    public void HideQuoteCard()
    {
        StopLoop();
        ClearDisplay();
        // 吹き出し背景を非表示
        if (speechBubbleImage != null) speechBubbleImage.gameObject.SetActive(false);
        Debug.Log("[QuoteCardDisplay] 表示停止");
    }

    private void LoadMessagePairs()
    {
        pairs.Clear();

        if (!File.Exists(MessagePairsPath))
        {
            Debug.Log("[QuoteCardDisplay] MessagePairs.jsonが見つかりません。");
            return;
        }

        try
        {
            string json = File.ReadAllText(MessagePairsPath);
            // JSONは配列形式なので、ラッパーを使わずに直接パース
            MessagePairData[] loadedPairs = JsonHelper.FromJson<MessagePairData>(json);
            if (loadedPairs != null)
            {
                pairs.AddRange(loadedPairs);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[QuoteCardDisplay] JSONパースエラー: {e.Message}");
        }
    }

    private IEnumerator DisplayLoop()
    {
        while (true)
        {
            // ランダムにペアを選択
            int index = Random.Range(0, pairs.Count);
            MessagePairData pair = pairs[index];

            // 表示を更新
            DisplayPair(pair);

            // 待機
            yield return new WaitForSeconds(displayInterval);
        }
    }

    private void DisplayPair(MessagePairData pair)
    {
        // テキスト表示
        if (quoteText != null)
        {
            quoteText.text = $"「{pair.message}」";
        }

        if (authorText != null)
        {
            authorText.text = pair.credit;
        }

        // 画像読み込み
        if (quoteImage != null && !string.IsNullOrEmpty(pair.image))
        {
            string imagePath = Path.Combine(CaptureDir, pair.image);
            StartCoroutine(LoadImageAsync(imagePath));
        }
    }

    private IEnumerator LoadImageAsync(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[QuoteCardDisplay] 画像が見つかりません: {path}");
            yield break;
        }

        // ファイルから読み込み
        byte[] imageData = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        if (tex.LoadImage(imageData))
        {
            quoteImage.texture = tex;
            // RawImageの比率に合わせてクリッピング
            ApplyAspectFitCrop(tex);
        }
        yield return null;
    }

    /// <summary>
    /// RawImageのアスペクト比に合わせて、画像を中央からクリッピングする
    /// </summary>
    private void ApplyAspectFitCrop(Texture2D tex)
    {
        if (quoteImage == null || tex == null) return;

        // RawImageのサイズ
        RectTransform rt = quoteImage.GetComponent<RectTransform>();
        float rawWidth = rt.rect.width;
        float rawHeight = rt.rect.height;

        // 画像のサイズ
        float texWidth = tex.width;
        float texHeight = tex.height;

        // アスペクト比
        float rawAspect = rawWidth / rawHeight;
        float texAspect = texWidth / texHeight;

        Rect uvRect;

        if (texAspect > rawAspect)
        {
            // 画像が横長 → 左右をクリッピング
            float scale = rawAspect / texAspect;
            float offset = (1f - scale) / 2f;
            uvRect = new Rect(offset, 0f, scale, 1f);
        }
        else
        {
            // 画像が縦長（または同じ） → 上下をクリッピング
            float scale = texAspect / rawAspect;
            float offset = (1f - scale) / 2f;
            uvRect = new Rect(0f, offset, 1f, scale);
        }

        quoteImage.uvRect = uvRect;
    }

    private void ClearDisplay()
    {
        if (quoteText != null) quoteText.text = "";
        if (authorText != null) authorText.text = "";
        if (quoteImage != null) quoteImage.texture = null;
    }

    private void StopLoop()
    {
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
        }
    }
}

/// <summary>
/// JSON配列をパースするためのヘルパークラス
/// </summary>
public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        // Unity's JsonUtility doesn't support top-level arrays directly
        // Wrap the array in an object
        string wrappedJson = "{\"items\":" + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrappedJson);
        return wrapper?.items;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] items;
    }
}
