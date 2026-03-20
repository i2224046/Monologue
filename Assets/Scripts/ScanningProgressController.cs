using UnityEngine;

/// <summary>
/// Scanning状態中の鼓動演出を制御するコントローラー。
/// 時間経過でBPMが加速していく。
/// </summary>
public class ScanningProgressController : MonoBehaviour
{
    [Header("Heart Animation")]
    [Tooltip("鼓動演出を行うImageHeart")]
    [SerializeField] private ImageHeart imageHeart;

    [Header("BPM Acceleration Settings")]
    [Tooltip("開始時のBPM")]
    [SerializeField] private float minBPM = 40f;

    [Tooltip("最大BPM")]
    [SerializeField] private float maxBPM = 140f;

    [Tooltip("最小BPMから最大BPMに到達するまでの時間（秒）")]
    [SerializeField] private float accelerationDuration = 10f;

    private float elapsedTime = 0f;
    private bool isAccelerating = false;

    private void OnEnable()
    {
        // Scanning状態に入るたびにリセット＆加速開始
        elapsedTime = 0f;
        isAccelerating = true;

        if (imageHeart != null)
        {
            imageHeart.SetBPM(minBPM);
        }

        Debug.Log($"[ScanningProgress] 開始: BPM {minBPM} → {maxBPM} ({accelerationDuration}秒)");
    }

    private void OnDisable()
    {
        // Scanning終了時に停止
        isAccelerating = false;
    }

    private void Update()
    {
        if (!isAccelerating || imageHeart == null) return;

        // 経過時間を更新
        elapsedTime += Time.deltaTime;

        // 0〜1の進捗率を計算
        float progress = Mathf.Clamp01(elapsedTime / accelerationDuration);

        // BPMを線形補間で計算
        float currentBPM = Mathf.Lerp(minBPM, maxBPM, progress);

        // ImageHeartに反映
        imageHeart.SetBPM(currentBPM);

        // 最大に到達したらログ出力（1回だけ）
        if (progress >= 1f && isAccelerating)
        {
            Debug.Log($"[ScanningProgress] 最大BPM {maxBPM} に到達");
            isAccelerating = false; // これ以上更新しない
        }
    }
}
