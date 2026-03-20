using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AudioVolumeProviderから音量データを取得し、
/// UI Imageに波形的な視覚効果（スケール、透明度、色相変化など）を適用するスクリプト。
/// MainCanvasのMessage Prefab内のImage要素にアタッチして使用。
/// </summary>
[RequireComponent(typeof(Image))]
public class ImageAudioPulse : MonoBehaviour
{
    [Header("Audio Source Reference")]
    [Tooltip("音量データを提供するAudioVolumeProvider（未設定ならシングルトンを使用）")]
    [SerializeField] private AudioVolumeProvider volumeProvider;

    [Header("Effect Settings")]
    [Tooltip("エフェクト全体の感度（音量をどれだけ増幅するか）")]
    [SerializeField] private float sensitivity = 10f;

    [Tooltip("エフェクトの反応速度")]
    [SerializeField] private float responseSpeed = 12f;

    [Header("Scale Effect")]
    [Tooltip("スケールエフェクトを有効にする")]
    [SerializeField] private bool enableScale = true;

    [Tooltip("最小スケール")]
    [SerializeField] private float minScale = 1.0f;

    [Tooltip("最大スケール")]
    [SerializeField] private float maxScale = 1.2f;

    [Header("Alpha Effect")]
    [Tooltip("透明度エフェクトを有効にする")]
    [SerializeField] private bool enableAlpha = false;

    [Tooltip("最小透明度")]
    [SerializeField] private float minAlpha = 0.7f;

    [Tooltip("最大透明度")]
    [SerializeField] private float maxAlpha = 1.0f;

    [Header("Color Effect")]
    [Tooltip("色相シフトエフェクトを有効にする")]
    [SerializeField] private bool enableColorShift = false;

    [Tooltip("色相シフトの強度（0〜1）")]
    [SerializeField] private float colorShiftIntensity = 0.1f;

    [Header("Glow Effect")]
    [Tooltip("グローエフェクトを有効にする（Materialが必要）")]
    [SerializeField] private bool enableGlow = false;

    [Tooltip("グローの最小強度")]
    [SerializeField] private float minGlow = 0f;

    [Tooltip("グローの最大強度")]
    [SerializeField] private float maxGlow = 1f;

    // 内部参照
    private Image targetImage;
    private RectTransform rectTransform;
    private Color originalColor;
    private Vector3 originalScale;
    private Material originalMaterial;
    private Material instanceMaterial;

    // 現在のエフェクト値
    private float currentEffectValue = 0f;

    private void Awake()
    {
        targetImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        originalColor = targetImage.color;
        originalScale = rectTransform.localScale;

        // グローエフェクト用にマテリアルをインスタンス化
        if (enableGlow && targetImage.material != null)
        {
            originalMaterial = targetImage.material;
            instanceMaterial = new Material(originalMaterial);
            targetImage.material = instanceMaterial;
        }
    }

    private void OnEnable()
    {
        // AudioVolumeProviderが未設定ならシングルトンを使用
        if (volumeProvider == null)
        {
            volumeProvider = AudioVolumeProvider.Instance;
            if (volumeProvider != null)
            {
                Debug.Log("[ImageAudioPulse] AudioVolumeProvider をシングルトンから取得しました。");
            }
        }
    }

    private void Update()
    {
        // AudioVolumeProviderがない場合はエフェクトなし
        if (volumeProvider == null)
        {
            // 元の状態に戻す
            ResetToOriginal();
            return;
        }

        // 音量を取得し、感度を適用
        float targetValue = volumeProvider.SmoothedVolume * sensitivity;
        targetValue = Mathf.Clamp01(targetValue);

        // 現在のエフェクト値を平滑化
        currentEffectValue = Mathf.Lerp(currentEffectValue, targetValue, Time.deltaTime * responseSpeed);

        // 各エフェクトを適用
        ApplyScaleEffect();
        ApplyAlphaEffect();
        ApplyColorShiftEffect();
        ApplyGlowEffect();
    }

    private void ApplyScaleEffect()
    {
        if (!enableScale) return;

        float scale = Mathf.Lerp(minScale, maxScale, currentEffectValue);
        rectTransform.localScale = originalScale * scale;
    }

    private void ApplyAlphaEffect()
    {
        if (!enableAlpha) return;

        float alpha = Mathf.Lerp(minAlpha, maxAlpha, currentEffectValue);
        Color c = targetImage.color;
        c.a = alpha;
        targetImage.color = c;
    }

    private void ApplyColorShiftEffect()
    {
        if (!enableColorShift) return;

        // HSVで色相をシフト
        Color.RGBToHSV(originalColor, out float h, out float s, out float v);
        h += currentEffectValue * colorShiftIntensity;
        h = h % 1f; // 色相を0〜1に収める
        Color shiftedColor = Color.HSVToRGB(h, s, v);
        shiftedColor.a = targetImage.color.a; // 透明度は維持
        targetImage.color = shiftedColor;
    }

    private void ApplyGlowEffect()
    {
        if (!enableGlow || instanceMaterial == null) return;

        float glow = Mathf.Lerp(minGlow, maxGlow, currentEffectValue);
        
        // 一般的なグローシェーダープロパティ名を試す
        if (instanceMaterial.HasProperty("_GlowIntensity"))
        {
            instanceMaterial.SetFloat("_GlowIntensity", glow);
        }
        else if (instanceMaterial.HasProperty("_EmissionIntensity"))
        {
            instanceMaterial.SetFloat("_EmissionIntensity", glow);
        }
    }

    private void ResetToOriginal()
    {
        if (enableScale)
        {
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, originalScale, Time.deltaTime * responseSpeed);
        }

        if (enableAlpha)
        {
            Color c = targetImage.color;
            c.a = Mathf.Lerp(c.a, originalColor.a, Time.deltaTime * responseSpeed);
            targetImage.color = c;
        }

        currentEffectValue = 0f;
    }

    private void OnDisable()
    {
        // 無効化時に元の状態に戻す
        if (rectTransform != null)
        {
            rectTransform.localScale = originalScale;
        }
        if (targetImage != null)
        {
            targetImage.color = originalColor;
        }
    }

    private void OnDestroy()
    {
        // インスタンス化したマテリアルを破棄
        if (instanceMaterial != null)
        {
            Destroy(instanceMaterial);
        }
    }

    // --- 外部からの設定用メソッド ---

    /// <summary>
    /// AudioVolumeProviderを動的に設定する
    /// </summary>
    public void SetVolumeProvider(AudioVolumeProvider provider)
    {
        volumeProvider = provider;
    }

    /// <summary>
    /// 感度を動的に変更する
    /// </summary>
    public void SetSensitivity(float newSensitivity)
    {
        sensitivity = newSensitivity;
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
}
