using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 複数のテキストを一定間隔で順番に切り替えて表示する汎用コンポーネント。
/// 外部から StartRotation() / StopRotation() で制御可能。
/// フェードイン/アウト機能付き。
/// </summary>
public class TextRotator : MonoBehaviour
{
    [Header("Display Target")]
    [Tooltip("テキストを表示するTMP")]
    [SerializeField] private TextMeshProUGUI targetTMP;

    [Header("Rotation Settings")]
    [Tooltip("表示するテキストのリスト（順番に切り替わる）")]
    [SerializeField] private string[] messages = new string[] { "スキャン中...", "解析しています...", "もう少しお待ちください..." };

    [Tooltip("各テキストの表示時間（秒）※フェード時間を含む")]
    [SerializeField] private float intervalSeconds = 3.0f;

    [Tooltip("ループするかどうか（falseの場合、最後のテキストで停止）")]
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
    /// テキストの切り替えを開始する
    /// </summary>
    public void StartRotation()
    {
        if (messages == null || messages.Length == 0)
        {
            Debug.LogWarning("[TextRotator] メッセージが設定されていません。");
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

        // 最初のテキストを設定
        if (targetTMP != null)
        {
            targetTMP.text = messages[currentIndex];
            targetTMP.gameObject.SetActive(true);

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
        }

        Debug.Log($"[TextRotator] ローテーション開始: {messages.Length}件のメッセージ / {intervalSeconds}秒間隔");
    }

    /// <summary>
    /// テキストの切り替えを停止し、非表示にする
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

        if (targetTMP != null)
        {
            if (useFade)
            {
                // フェードアウトしてから非表示
                fadeCoroutine = StartCoroutine(FadeOutAndHide());
            }
            else
            {
                targetTMP.gameObject.SetActive(false);
            }
        }

        Debug.Log("[TextRotator] ローテーション停止");
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
            if (currentIndex >= messages.Length)
            {
                if (loop)
                {
                    currentIndex = 0;
                }
                else
                {
                    // ループしない場合は最後のメッセージで停止（非表示にはしない）
                    currentIndex = messages.Length - 1;
                    isRotating = false;
                    Debug.Log("[TextRotator] 最後のメッセージに到達（ループOFF）");
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
                ShowCurrentMessage();
            }
        }
    }

    private void ShowCurrentMessage()
    {
        if (targetTMP != null && messages != null && currentIndex < messages.Length)
        {
            targetTMP.text = messages[currentIndex];
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

        // テキスト切り替え
        ShowCurrentMessage();

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
        
        if (targetTMP != null)
        {
            targetTMP.gameObject.SetActive(false);
        }
        fadeCoroutine = null;
    }
}
