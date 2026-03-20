using UnityEngine;

/// <summary>
/// Scanning状態中の鼓動演出を制御するコントローラー。
/// 1. 時間経過で漸近的に加速（最大値に近づくが到達しない）
/// 2. イベントで段階的にジャンプ（AdvancePhase）
/// 両方を組み合わせて使用。
/// </summary>
public class ScanningProgressController : MonoBehaviour
{
    [Header("Heart Animation")]
    [Tooltip("鼓動演出を行うImageHeart")]
    [SerializeField] private ImageHeart imageHeart;

    [Header("Asymptotic Acceleration (時間ベース漸近加速)")]
    [Tooltip("開始BPM")]
    [SerializeField] private float startBPM = 50f;

    [Tooltip("最大BPM（到達しないが近づく）")]
    [SerializeField] private float maxBPM = 150f;

    [Tooltip("開始スケール")]
    [SerializeField] private float startScale = 1.05f;

    [Tooltip("最大スケール")]
    [SerializeField] private float maxScale = 1.30f;

    [Tooltip("開始音量")]
    [SerializeField] private float startVolume = 0.3f;

    [Tooltip("最大音量")]
    [SerializeField] private float maxVolume = 0.8f;

    [Tooltip("半減期（秒）- この時間で残り距離の半分進む")]
    [SerializeField] private float halfLife = 8f;

    [Header("Event Boost (イベント時のジャンプ)")]
    [Tooltip("AdvancePhase時に追加するBPM")]
    [SerializeField] private float boostBPM = 15f;

    [Tooltip("AdvancePhase時に追加するスケール")]
    [SerializeField] private float boostScale = 0.03f;

    [Tooltip("AdvancePhase時に追加する音量")]
    [SerializeField] private float boostVolume = 0.05f;

    private float currentBPM;
    private float currentScale;
    private float currentVolume;
    private float elapsedTime = 0f;
    private bool isActive = false;

    private void OnEnable()
    {
        // Scanning状態に入るたびにリセット
        currentBPM = startBPM;
        currentScale = startScale;
        currentVolume = startVolume;
        elapsedTime = 0f;
        isActive = true;

        ApplyValues();
        Debug.Log($"[ScanningProgress] 開始: BPM {startBPM} → {maxBPM} (半減期 {halfLife}秒)");
    }

    private void OnDisable()
    {
        isActive = false;
    }

    private void Update()
    {
        if (!isActive || imageHeart == null) return;

        elapsedTime += Time.deltaTime;

        // 指数減衰による漸近的加速
        // 公式: current = max - (max - start) * 0.5^(t / halfLife)
        // 時間が経つほど最大値に近づくが、到達することはない
        float decay = Mathf.Pow(0.5f, elapsedTime / halfLife);

        float targetBPM = maxBPM - (maxBPM - startBPM) * decay;
        float targetScale = maxScale - (maxScale - startScale) * decay;
        float targetVolume = maxVolume - (maxVolume - startVolume) * decay;

        // 現在値を更新（イベントブーストによる上乗せを維持）
        currentBPM = Mathf.Max(currentBPM, targetBPM);
        currentScale = Mathf.Max(currentScale, targetScale);
        currentVolume = Mathf.Max(currentVolume, targetVolume);

        ApplyValues();
    }

    /// <summary>
    /// 次のフェーズに進む（イベントブースト）
    /// </summary>
    public void AdvancePhase()
    {
        // 現在値に即座にブーストを追加
        currentBPM = Mathf.Min(currentBPM + boostBPM, maxBPM);
        currentScale = Mathf.Min(currentScale + boostScale, maxScale);
        currentVolume = Mathf.Min(currentVolume + boostVolume, maxVolume);

        ApplyValues();
        Debug.Log($"[ScanningProgress] Boost! BPM {currentBPM:F0}, Scale {currentScale:F2}");
    }

    /// <summary>
    /// 特定のフェーズにジャンプ（互換性のため維持）
    /// </summary>
    public void SetPhase(int phase)
    {
        // 複数回ブースト
        for (int i = 0; i < phase; i++)
        {
            AdvancePhase();
        }
    }

    private void ApplyValues()
    {
        if (imageHeart == null) return;

        imageHeart.SetBPM(currentBPM);
        imageHeart.SetScaleRange(1.0f, currentScale);
        imageHeart.SetVolume(currentVolume);
    }

    /// <summary>
    /// 現在のBPMを取得
    /// </summary>
    public float CurrentBPM => currentBPM;
}
