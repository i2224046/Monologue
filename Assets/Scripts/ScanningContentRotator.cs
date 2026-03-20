using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Scanning状態中にテキストと画像をペアで順番に切り替えて表示するコンポーネント。
/// 外部から StartRotation() / StopRotation() で制御可能。
/// フェードイン/アウト機能付き。
/// </summary>
public class ScanningContentRotator : MonoBehaviour
{
    [Header("Display Targets")]
    [Tooltip("テキストを表示するTMP")]
    [SerializeField] private TextMeshProUGUI targetTMP;

    [Tooltip("画像を表示するImage")]
    [SerializeField] private Image targetImage;

    [Header("Content Settings")]
    [Tooltip("表示するコンテンツのリスト（テキスト＋画像ペア）")]
    [SerializeField] private ScanningContent[] contents;

    [Header("Rotation Settings")]
    [Tooltip("各コンテンツの表示時間（秒）※フェード時間を含む")]
    [SerializeField] private float intervalSeconds = 3.0f;

    [Tooltip("ループするかどうか（falseの場合、最後のコンテンツで停止）")]
    [SerializeField] private bool loop = true;

    [Header("Fade Settings")]
    [Tooltip("フェードイン/アウトを有効にする")]
    [SerializeField] private bool useFade = true;

    [Tooltip("フェードにかかる時間（秒）")]
    [SerializeField] private float fadeDuration = 0.5f;

    private int currentIndex = 0;
    private float timer = 0f;
    private bool isRotating = false;
    private Coroutine fadeCoroutine;

    /// <summary>
    /// コンテンツの切り替えを開始する
    /// </summary>
    public void StartRotation()
    {
        if (contents == null || contents.Length == 0)
        {
            Debug.LogWarning("[ScanningContentRotator] コンテンツが設定されていません。");
            return;
        }

        // 既存のフェードを停止
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        currentIndex = 0;
        timer = 0f;
        isRotating = true;

        // 最初のコンテンツを設定
        ShowCurrentContent();
        SetTargetsActive(true);

        // フェードイン開始
        if (useFade)
        {
            SetAlpha(0f);
            fadeCoroutine = StartCoroutine(FadeIn());
        }
        else
        {
            SetAlpha(1f);
        }

        Debug.Log($"[ScanningContentRotator] ローテーション開始: {contents.Length}件のコンテンツ / {intervalSeconds}秒間隔");
    }

    /// <summary>
    /// コンテンツの切り替えを停止し、非表示にする
    /// </summary>
    public void StopRotation()
    {
        isRotating = false;

        // 既存のフェードを停止
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (useFade)
        {
            // フェードアウトしてから非表示
            fadeCoroutine = StartCoroutine(FadeOutAndHide());
        }
        else
        {
            SetTargetsActive(false);
        }

        Debug.Log("[ScanningContentRotator] ローテーション停止");
    }

    /// <summary>
    /// 現在ローテーション中かどうか
    /// </summary>
    public bool IsRotating => isRotating;

    private void Update()
    {
        if (!isRotating) return;

        timer += Time.deltaTime;

        if (timer >= intervalSeconds)
        {
            timer = 0f;
            currentIndex++;

            // ループ処理
            if (currentIndex >= contents.Length)
            {
                if (loop)
                {
                    currentIndex = 0;
                }
                else
                {
                    // ループしない場合は最後のコンテンツで停止（非表示にはしない）
                    currentIndex = contents.Length - 1;
                    isRotating = false;
                    Debug.Log("[ScanningContentRotator] 最後のコンテンツに到達（ループOFF）");
                    return;
                }
            }

            // フェードで切り替え
            if (useFade)
            {
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                }
                fadeCoroutine = StartCoroutine(FadeOutAndIn());
            }
            else
            {
                ShowCurrentContent();
            }
        }
    }

    private void ShowCurrentContent()
    {
        if (contents == null || currentIndex >= contents.Length) return;

        ScanningContent content = contents[currentIndex];

        // テキスト更新
        if (targetTMP != null)
        {
            targetTMP.text = content.message ?? "";
        }

        // 画像更新
        if (targetImage != null)
        {
            if (content.image != null)
            {
                targetImage.sprite = content.image;
                targetImage.enabled = true;
            }
            else
            {
                // 画像がnullの場合は非表示
                targetImage.enabled = false;
            }
        }
    }

    private void SetTargetsActive(bool active)
    {
        if (targetTMP != null)
        {
            targetTMP.gameObject.SetActive(active);
        }
        if (targetImage != null)
        {
            targetImage.gameObject.SetActive(active);
        }
    }

    // --- フェード処理 ---

    private void SetAlpha(float alpha)
    {
        if (targetTMP != null)
        {
            Color c = targetTMP.color;
            c.a = alpha;
            targetTMP.color = c;
        }
        if (targetImage != null)
        {
            Color c = targetImage.color;
            c.a = alpha;
            targetImage.color = c;
        }
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }
        SetAlpha(1f);
        fadeCoroutine = null;
    }

    private IEnumerator FadeOutAndIn()
    {
        // フェードアウト
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(1f - Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }
        SetAlpha(0f);

        // コンテンツ切り替え
        ShowCurrentContent();

        // フェードイン
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }
        SetAlpha(1f);
        fadeCoroutine = null;
    }

    private IEnumerator FadeOutAndHide()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(1f - Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }
        SetAlpha(0f);

        SetTargetsActive(false);
        fadeCoroutine = null;
    }
}
