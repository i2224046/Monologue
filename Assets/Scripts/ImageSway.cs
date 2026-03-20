using UnityEngine;

/// <summary>
/// UI要素を上下または左右にゆらゆら揺らすスクリプト。
/// Sinカーブベースで滑らかな往復運動を行う。
/// </summary>
public class ImageSway : MonoBehaviour
{
    public enum SwayDirection
    {
        Horizontal,  // 左右
        Vertical     // 上下
    }

    public enum StartPosition
    {
        Max,    // 最高地点（上または右）から開始
        Min     // 最低地点（下または左）から開始
    }

    [Header("Sway Settings")]
    [Tooltip("揺れの方向（Horizontal: 左右, Vertical: 上下）")]
    [SerializeField] private SwayDirection direction = SwayDirection.Vertical;

    [Tooltip("揺れの振幅（ピクセル単位）")]
    [SerializeField] private float amplitude = 20f;

    [Tooltip("揺れの速度（1周期にかかる秒数）")]
    [SerializeField] private float duration = 2f;

    [Tooltip("開始位置（Max: 最高地点, Min: 最低地点）")]
    [SerializeField] private StartPosition startPosition = StartPosition.Max;

    [Tooltip("揺れのカーブ（横軸: 0〜1の時間, 縦軸: -1〜1の位置）\n未設定の場合はSinカーブを使用")]
    [SerializeField] private AnimationCurve swayCurve;

    [Tooltip("カスタムカーブを使用する")]
    [SerializeField] private bool useCustomCurve = false;

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private float time = 0f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;

        // デフォルトカーブの設定（緩やかなSin波）
        if (swayCurve == null || swayCurve.keys.Length == 0)
        {
            swayCurve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, Mathf.PI * 2f),     // 開始点
                new Keyframe(0.25f, 1f, 0f, 0f),             // 最大
                new Keyframe(0.5f, 0f, -Mathf.PI * 2f, -Mathf.PI * 2f), // 中央
                new Keyframe(0.75f, -1f, 0f, 0f),            // 最小
                new Keyframe(1f, 0f, Mathf.PI * 2f, 0f)      // 終了点
            );
        }
    }

    private void OnEnable()
    {
        // 有効化時に元の位置を記録
        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
        }

        // 開始位置に応じて初期timeを設定
        // Sinカーブ: time=0.25で最大(1), time=0.75で最小(-1)
        if (startPosition == StartPosition.Max)
        {
            time = 0.25f;  // 最高地点から開始
        }
        else
        {
            time = 0.75f;  // 最低地点から開始
        }
    }

    private void OnDisable()
    {
        // 無効化時に元の位置に戻す
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalPosition;
        }
    }

    private void Update()
    {
        if (duration <= 0f) return;

        // 時間を進める (0〜1にループ)
        time += Time.deltaTime / duration;
        if (time >= 1f)
        {
            time -= 1f;
        }

        // 揺れの値を計算 (-1〜1)
        float swayValue;
        if (useCustomCurve)
        {
            swayValue = swayCurve.Evaluate(time);
        }
        else
        {
            // シンプルなSinカーブ（滑らかな往復）
            swayValue = Mathf.Sin(time * Mathf.PI * 2f);
        }

        // 移動量を計算
        float offset = swayValue * amplitude;

        // 新しい位置を設定
        Vector2 newPosition = originalPosition;
        if (direction == SwayDirection.Horizontal)
        {
            newPosition.x += offset;
        }
        else
        {
            newPosition.y += offset;
        }

        rectTransform.anchoredPosition = newPosition;
    }

    /// <summary>
    /// 振幅を動的に変更する
    /// </summary>
    public void SetAmplitude(float newAmplitude)
    {
        amplitude = newAmplitude;
    }

    /// <summary>
    /// 周期（duration）を動的に変更する
    /// </summary>
    public void SetDuration(float newDuration)
    {
        duration = newDuration;
    }

    /// <summary>
    /// 揺れの方向を動的に変更する
    /// </summary>
    public void SetDirection(SwayDirection newDirection)
    {
        direction = newDirection;
    }

    /// <summary>
    /// 元の位置をリセットする（位置が変わった場合に呼ぶ）
    /// </summary>
    public void ResetOriginalPosition()
    {
        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
        }
    }
}
