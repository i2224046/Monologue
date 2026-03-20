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

    [Header("Phase 2 Templates (Item Identified)")]
    [Tooltip("アイテム識別後に表示するメッセージテンプレート（{0}にアイテム名が入る）")]
    [SerializeField] private string[] itemIdentifiedTemplates = new string[]
    {
        "{0}に耳を傾けています...",
        "{0}に触れて鼓動を感じ取ってください..."
    };

    private int currentIndex = 0;
    private float timer = 0f;
    private bool isRotating = false;
    private Coroutine fadeCoroutine;
    
    // Phase 2 用の動的コンテンツ
    private string[] phase2Messages = null;
    private int phase2Index = 0;
    private bool isPhase2 = false;

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
        
        // Phase 2 リセット
        isPhase2 = false;
        phase2Messages = null;
        phase2Index = 0;
    }

    /// <summary>
    /// 現在ローテーション中かどうか
    /// </summary>
    public bool IsRotating => isRotating;

    /// <summary>
    /// Phase 2: アイテム識別完了時に呼び出し、アイテム名入りローテーションを開始
    /// </summary>
    public void SetItemIdentified(string itemName)
    {
        if (itemIdentifiedTemplates == null || itemIdentifiedTemplates.Length == 0)
        {
            Debug.LogWarning("[ScanningContentRotator] Phase 2 テンプレートが設定されていません。");
            return;
        }

        // テンプレートにアイテム名を埋め込んでPhase 2メッセージを生成
        phase2Messages = new string[itemIdentifiedTemplates.Length];
        for (int i = 0; i < itemIdentifiedTemplates.Length; i++)
        {
            phase2Messages[i] = string.Format(itemIdentifiedTemplates[i], itemName);
        }

        // Phase 2 モードを有効化
        isPhase2 = true;
        phase2Index = 0;
        timer = 0f;

        // 最初のPhase 2メッセージを表示
        ShowPhase2Content();

        Debug.Log($"[ScanningContentRotator] Phase 2 開始: {itemName} ({phase2Messages.Length}件のメッセージ)");
    }

    private void Update()
    {
        if (!isRotating) return;

        timer += Time.deltaTime;

        if (timer >= intervalSeconds)
        {
            timer = 0f;

            // Phase 2 モードの場合
            if (isPhase2 && phase2Messages != null && phase2Messages.Length > 0)
            {
                phase2Index++;
                if (phase2Index >= phase2Messages.Length)
                {
                    phase2Index = 0; // ループ
                }

                if (useFade)
                {
                    if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                    fadeCoroutine = StartCoroutine(FadeOutAndInPhase2());
                }
                else
                {
                    ShowPhase2Content();
                }
                return;
            }

            // Phase 1 (通常モード)
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

    private void ShowPhase2Content()
    {
        if (phase2Messages == null || phase2Index >= phase2Messages.Length) return;

        if (targetTMP != null)
        {
            targetTMP.text = phase2Messages[phase2Index];
        }

        // Phase 2 では画像を非表示
        if (targetImage != null)
        {
            targetImage.enabled = false;
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

    private IEnumerator FadeOutAndInPhase2()
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

        // Phase 2 コンテンツ切り替え
        ShowPhase2Content();

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
