using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class MessageWindowAnimation : MonoBehaviour
{
    [Header("揺れ設定")]
    public float amplitude = 10f;
    public float speed = 1.0f;
    public bool randomOffset = true;

    [Header("フェード設定")]
    public float fadeDuration = 1.0f; // フェードにかかる時間

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 initialPosition;
    private float timeOffset;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        // 初期位置設定
        if (rectTransform != null) initialPosition = rectTransform.anchoredPosition;
        if (randomOffset) timeOffset = Random.Range(0f, 100f);

        // 出現時のフェードイン開始
        StartCoroutine(FadeProcess(0f, 1f));
    }

    void Update()
    {
        // ゆらゆら処理
        if (rectTransform == null) return;
        float time = Time.time * speed + timeOffset;
        float x = Mathf.Sin(time) * amplitude;
        float y = Mathf.Cos(time * 0.8f) * amplitude;
        rectTransform.anchoredPosition = initialPosition + new Vector2(x, y);
    }

    // 外部（Manager）から呼ばれる消失処理
    public void Dismiss()
    {
        StartCoroutine(FadeProcess(1f, 0f, () => Destroy(gameObject)));
    }

    private IEnumerator FadeProcess(float start, float end, System.Action onComplete = null)
    {
        float elapsed = 0f;
        canvasGroup.alpha = start;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, end, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = end;
        onComplete?.Invoke();
    }

    public void SetLifetime(float duration)
    {
        StartCoroutine(LifetimeRoutine(duration));
    }

    private IEnumerator LifetimeRoutine(float duration)
    {
        // ライフタイム待機
        yield return new WaitForSeconds(duration);
        // 時間が来たら消える
        Dismiss();
    }
}