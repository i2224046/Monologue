using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Imageに鼓動のような点滅・スケーリング演出を行うスクリプト。
/// AnimationCurveで緩急を調整可能。鼓動に合わせて音声も再生。
/// </summary>
[RequireComponent(typeof(Image))]
public class ImageHeart : MonoBehaviour
{
    [Header("Pulse Settings")]
    [Tooltip("1分あたりの鼓動回数 (BPM)")]
    [SerializeField] private float bpm = 60f;

    [Tooltip("鼓動のカーブ (横軸: 0〜1の時間, 縦軸: 0〜1の強度)")]
    [SerializeField] private AnimationCurve pulseCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2f),      // 開始: ゆっくりスタート
        new Keyframe(0.15f, 1f, 0f, 0f),   // ピーク: 急上昇
        new Keyframe(0.4f, 0.3f, -1f, -1f), // 少し戻る
        new Keyframe(0.5f, 0.5f, 2f, 0f),  // 2回目の軽い拍動
        new Keyframe(1f, 0f, -2f, 0f)      // 終了: ゆっくり戻る
    );

    [Header("Scale Animation")]
    [Tooltip("スケールアニメーションを有効にする")]
    [SerializeField] private bool enableScale = true;

    [Tooltip("最小スケール")]
    [SerializeField] private float minScale = 1.0f;

    [Tooltip("最大スケール")]
    [SerializeField] private float maxScale = 1.15f;

    [Header("Alpha Animation")]
    [Tooltip("透明度アニメーションを有効にする")]
    [SerializeField] private bool enableAlpha = true;

    [Tooltip("最小透明度")]
    [SerializeField] private float minAlpha = 0.5f;

    [Tooltip("最大透明度")]
    [SerializeField] private float maxAlpha = 1.0f;

    [Header("Sound Settings")]
    [Tooltip("鼓動音を有効にする")]
    [SerializeField] private bool enableSound = true;

    [Tooltip("鼓動音のAudioClip（短いwavファイル）")]
    [SerializeField] private AudioClip heartbeatClip;

    [Tooltip("音量 (0〜1)")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.5f;

    [Tooltip("音声を再生するAudioSource（未設定なら自動生成）")]
    [SerializeField] private AudioSource audioSource;

    private Image targetImage;
    private RectTransform rectTransform;
    private Color originalColor;
    private Vector3 originalScale;
    private float time = 0f;
    private bool soundPlayedThisBeat = false;

    private void Awake()
    {
        targetImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        originalColor = targetImage.color;
        originalScale = rectTransform.localScale;

        // AudioSourceが未設定なら自動生成
        if (audioSource == null && enableSound)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void Update()
    {
        // BPMから1拍の周期を計算 (秒)
        float beatDuration = 60f / bpm;

        // 前フレームの時間を記録
        float prevTime = time;

        // 時間を進める (0〜1にループ)
        time += Time.deltaTime / beatDuration;
        if (time >= 1f)
        {
            time -= 1f;
            soundPlayedThisBeat = false; // 新しい拍動開始時にリセット
        }

        // カーブから値を取得 (0〜1)
        float pulse = pulseCurve.Evaluate(time);

        // 鼓動のピーク時（time=0.15付近）に音声再生
        if (enableSound && !soundPlayedThisBeat && time >= 0.1f && prevTime < 0.1f)
        {
            PlayHeartbeatSound();
            soundPlayedThisBeat = true;
        }

        // スケールアニメーション
        if (enableScale)
        {
            float scale = Mathf.Lerp(minScale, maxScale, pulse);
            rectTransform.localScale = originalScale * scale;
        }

        // 透明度アニメーション
        if (enableAlpha)
        {
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, pulse);
            Color c = originalColor;
            c.a = alpha;
            targetImage.color = c;
        }
    }

    private void PlayHeartbeatSound()
    {
        if (audioSource != null && heartbeatClip != null)
        {
            audioSource.PlayOneShot(heartbeatClip, volume);
        }
    }

    /// <summary>
    /// BPMを動的に変更する
    /// </summary>
    public void SetBPM(float newBpm)
    {
        bpm = newBpm;
    }

    /// <summary>
    /// スケール範囲を動的に変更する
    /// </summary>
    public void SetScaleRange(float min, float max)
    {
        minScale = min;
        maxScale = max;
    }

    /// <summary>
    /// 透明度範囲を動的に変更する
    /// </summary>
    public void SetAlphaRange(float min, float max)
    {
        minAlpha = min;
        maxAlpha = max;
    }

    /// <summary>
    /// 音量を動的に変更する
    /// </summary>
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
    }
}
