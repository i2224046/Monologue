using UnityEngine;
using System.Collections;

/// <summary>
/// UIのスライドアニメーションを汎用的に実行するスクリプト。
/// RectTransformに対してスライドイン/アウトのアニメーションを行う。
/// </summary>
public class SlideAnimation : MonoBehaviour
{
    public enum SlideDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    [Header("Animation Settings")]
    [Tooltip("アニメーションの方向")]
    [SerializeField] private SlideDirection direction = SlideDirection.Up;

    [Tooltip("アニメーション時間（秒）")]
    [SerializeField] private float duration = 0.3f;

    [Tooltip("移動距離（ピクセル）")]
    [SerializeField] private float distance = 50f;

    [Tooltip("イージング（ease-out風）")]
    [SerializeField] private bool easeOut = true;

    private RectTransform rectTransform;
    private Coroutine currentAnimation;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 指定位置からスライドインするアニメーションを実行
    /// </summary>
    public void SlideIn()
    {
        if (rectTransform == null) return;
        
        // 非アクティブなGameObjectではコルーチンを開始できないためチェック
        if (!gameObject.activeInHierarchy) return;
        
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }

        Vector2 startOffset = GetDirectionVector() * distance;
        currentAnimation = StartCoroutine(AnimateSlide(startOffset, Vector2.zero));
    }

    /// <summary>
    /// 指定方向へスライドアウトするアニメーションを実行
    /// </summary>
    public void SlideOut()
    {
        if (rectTransform == null) return;
        
        // 非アクティブなGameObjectではコルーチンを開始できないためチェック
        if (!gameObject.activeInHierarchy) return;
        
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }

        Vector2 endOffset = GetDirectionVector() * -distance;
        currentAnimation = StartCoroutine(AnimateSlide(Vector2.zero, endOffset));
    }

    /// <summary>
    /// 外部から方向を指定してスライドイン
    /// </summary>
    public void SlideInFrom(SlideDirection dir)
    {
        direction = dir;
        SlideIn();
    }

    private Vector2 GetDirectionVector()
    {
        switch (direction)
        {
            case SlideDirection.Up: return Vector2.up;
            case SlideDirection.Down: return Vector2.down;
            case SlideDirection.Left: return Vector2.left;
            case SlideDirection.Right: return Vector2.right;
            default: return Vector2.zero;
        }
    }

    private IEnumerator AnimateSlide(Vector2 startOffset, Vector2 endOffset)
    {
        Vector2 originalPosition = rectTransform.anchoredPosition - startOffset;
        float elapsed = 0f;

        rectTransform.anchoredPosition = originalPosition + startOffset;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // ease-out補間
            if (easeOut)
            {
                t = 1f - (1f - t) * (1f - t);
            }

            Vector2 currentOffset = Vector2.Lerp(startOffset, endOffset, t);
            rectTransform.anchoredPosition = originalPosition + currentOffset;

            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition + endOffset;
        currentAnimation = null;
    }
}
