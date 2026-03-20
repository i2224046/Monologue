using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 画像またはテキストのスライドショーを表示するシステム。
/// 画像スライドとテキストスライドを混在させることが可能。
/// </summary>
public class WaitingSlideshow : MonoBehaviour
{
    /// <summary>
    /// スライドの種類
    /// </summary>
    public enum SlideType
    {
        Image,  // 画像スライド（既存のGameObjectを表示）
        Text    // テキストスライド
    }

    /// <summary>
    /// スライドデータを保持する構造体
    /// </summary>
    [System.Serializable]
    public class SlideData
    {
        [Tooltip("スライドの種類")]
        public SlideType type = SlideType.Image;

        [Tooltip("画像スライド用: 表示するGameObject")]
        public GameObject imageObject;

        [Tooltip("テキストスライド用: 表示するテキスト内容")]
        [TextArea(3, 10)]
        public string textContent;
    }

    [Header("スライドショー設定")]
    [Tooltip("スライドデータの配列")]
    [SerializeField] private SlideData[] slides;

    [Tooltip("スライドの切り替え間隔（秒）")]
    [SerializeField] private float slideInterval = 5.0f;

    [Header("テキスト表示設定")]
    [Tooltip("テキストスライド表示用のTextMeshProUGUI")]
    [SerializeField] private TextMeshProUGUI textDisplay;

    [Tooltip("テキスト表示用の親オブジェクト（CanvasGroupを持つ）")]
    [SerializeField] private GameObject textContainer;

    [Header("フェード設定")]
    [Tooltip("フェード演出を使用するか")]
    [SerializeField] private bool useFade = true;

    [Tooltip("フェード時間（秒）")]
    [SerializeField] private float fadeDuration = 0.5f;

    private int currentIndex = 0;
    private float timer = 0f;

    // フェード用
    private bool isFading = false;
    private float fadeTimer = 0f;
    private CanvasGroup currentCanvasGroup;
    private CanvasGroup nextCanvasGroup;

    void OnEnable()
    {
        // 有効化時に初期化
        currentIndex = 0;
        timer = 0f;
        isFading = false;

        if (slides == null || slides.Length == 0) return;

        // 全てのスライドを非表示にする
        HideAllSlides();

        // 最初のスライドを表示
        ShowSlide(0, true);
    }

    void OnDisable()
    {
        // 無効化時に全て非表示
        HideAllSlides();
    }

    void Update()
    {
        if (slides == null || slides.Length == 0) return;

        // スライドが1枚以下なら切り替え不要
        if (slides.Length <= 1) return;

        // フェード処理中
        if (isFading)
        {
            UpdateFade();
            return;
        }

        // タイマー更新
        timer += Time.deltaTime;

        if (timer >= slideInterval)
        {
            timer = 0f;
            NextSlide();
        }
    }

    /// <summary>
    /// 全てのスライドを非表示にする
    /// </summary>
    private void HideAllSlides()
    {
        // 全ての画像オブジェクトを非表示
        foreach (var slide in slides)
        {
            if (slide.type == SlideType.Image && slide.imageObject != null)
            {
                slide.imageObject.SetActive(false);
                var cg = slide.imageObject.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 0f;
            }
        }

        // テキストコンテナを非表示
        if (textContainer != null)
        {
            textContainer.SetActive(false);
            var cg = textContainer.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0f;
        }
    }

    /// <summary>
    /// 指定したスライドを表示する
    /// </summary>
    private void ShowSlide(int index, bool fullAlpha)
    {
        if (index < 0 || index >= slides.Length) return;

        var slide = slides[index];
        float alpha = fullAlpha ? 1f : 0f;

        if (slide.type == SlideType.Image)
        {
            if (slide.imageObject != null)
            {
                slide.imageObject.SetActive(true);
                var cg = GetOrAddCanvasGroup(slide.imageObject);
                if (cg != null) cg.alpha = alpha;
            }
        }
        else if (slide.type == SlideType.Text)
        {
            if (textContainer != null && textDisplay != null)
            {
                textDisplay.text = slide.textContent;
                textContainer.SetActive(true);
                var cg = GetOrAddCanvasGroup(textContainer);
                if (cg != null) cg.alpha = alpha;
            }
        }
    }

    /// <summary>
    /// 指定したスライドを非表示にする
    /// </summary>
    private void HideSlide(int index)
    {
        if (index < 0 || index >= slides.Length) return;

        var slide = slides[index];

        if (slide.type == SlideType.Image)
        {
            if (slide.imageObject != null)
            {
                slide.imageObject.SetActive(false);
            }
        }
        else if (slide.type == SlideType.Text)
        {
            if (textContainer != null)
            {
                textContainer.SetActive(false);
            }
        }
    }

    /// <summary>
    /// スライドのCanvasGroupを取得
    /// </summary>
    private CanvasGroup GetSlideCanvasGroup(int index)
    {
        if (index < 0 || index >= slides.Length) return null;

        var slide = slides[index];

        if (slide.type == SlideType.Image)
        {
            return slide.imageObject != null ? GetOrAddCanvasGroup(slide.imageObject) : null;
        }
        else if (slide.type == SlideType.Text)
        {
            return textContainer != null ? GetOrAddCanvasGroup(textContainer) : null;
        }

        return null;
    }

    /// <summary>
    /// 次のスライドへ切り替え
    /// </summary>
    private void NextSlide()
    {
        int nextIndex = (currentIndex + 1) % slides.Length;

        if (useFade)
        {
            // フェード用CanvasGroupを取得
            currentCanvasGroup = GetSlideCanvasGroup(currentIndex);
            
            // 次のスライドを表示準備
            ShowSlide(nextIndex, false);
            nextCanvasGroup = GetSlideCanvasGroup(nextIndex);

            // フェード開始
            isFading = true;
            fadeTimer = 0f;
        }
        else
        {
            // 即時切り替え
            HideSlide(currentIndex);
            ShowSlide(nextIndex, true);
            currentIndex = nextIndex;
        }
    }

    /// <summary>
    /// フェード処理の更新
    /// </summary>
    private void UpdateFade()
    {
        fadeTimer += Time.deltaTime;
        float t = Mathf.Clamp01(fadeTimer / fadeDuration);

        // クロスフェード: 現在をフェードアウト、次をフェードイン
        if (currentCanvasGroup != null) currentCanvasGroup.alpha = 1f - t;
        if (nextCanvasGroup != null) nextCanvasGroup.alpha = t;

        if (t >= 1f)
        {
            // フェード完了
            int nextIndex = (currentIndex + 1) % slides.Length;
            HideSlide(currentIndex);
            currentIndex = nextIndex;
            isFading = false;
        }
    }

    /// <summary>
    /// CanvasGroupを取得または追加
    /// </summary>
    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        if (obj == null) return null;
        
        var cg = obj.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = obj.AddComponent<CanvasGroup>();
        }
        return cg;
    }
}
