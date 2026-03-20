using UnityEngine;

/// <summary>
/// AudioSourceのリアルタイム音量を取得・公開するスクリプト。
/// SubCanvas側のTypingSoundPlayerなどと連携し、
/// MainCanvas側のImageAudioPulseから参照される。
/// </summary>
public class AudioVolumeProvider : MonoBehaviour
{
    [Header("Audio Source Settings")]
    [Tooltip("監視対象のAudioSource（未設定の場合は同じGameObjectから自動取得）")]
    [SerializeField] private AudioSource targetAudioSource;

    [Header("Volume Analysis Settings")]
    [Tooltip("サンプル数（大きいほど精度が上がるが処理負荷も増加、2のべき乗推奨）")]
    [SerializeField] private int sampleSize = 256;

    [Tooltip("音量の平滑化速度（大きいほど素早く反応）")]
    [SerializeField] private float smoothSpeed = 15f;

    [Tooltip("音量がない時の減衰速度")]
    [SerializeField] private float decaySpeed = 8f;

    // サンプルデータ用配列
    private float[] samples;

    // 現在の音量（0〜1程度、外部から読み取り可能）
    public float CurrentVolume { get; private set; }

    // 平滑化された音量（急激な変化を抑えた値）
    public float SmoothedVolume { get; private set; }

    // ピーク検出用
    private float peakVolume = 0f;
    public float PeakVolume => peakVolume;

    // シングルトンアクセス用（オプション）
    private static AudioVolumeProvider instance;
    public static AudioVolumeProvider Instance => instance;

    private void Awake()
    {
        // シングルトン設定（複数ある場合は最初のものを優先）
        if (instance == null)
        {
            instance = this;
        }

        // サンプル配列を初期化
        samples = new float[sampleSize];

        // AudioSourceの自動取得
        if (targetAudioSource == null)
        {
            targetAudioSource = GetComponent<AudioSource>();
        }

        if (targetAudioSource == null)
        {
            Debug.LogWarning("[AudioVolumeProvider] AudioSourceが見つかりません。Inspectorで設定してください。");
        }
    }

    private void Update()
    {
        if (targetAudioSource == null) return;

        // AudioSourceが再生中の場合、出力データを取得
        if (targetAudioSource.isPlaying)
        {
            // 現在の出力データを取得（チャンネル0）
            targetAudioSource.GetOutputData(samples, 0);

            // RMS（二乗平均平方根）で音量を計算
            float sum = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                sum += samples[i] * samples[i];
            }
            float rmsVolume = Mathf.Sqrt(sum / samples.Length);

            // 現在の音量を更新
            CurrentVolume = rmsVolume;

            // ピーク検出
            if (rmsVolume > peakVolume)
            {
                peakVolume = rmsVolume;
            }
            else
            {
                peakVolume = Mathf.Lerp(peakVolume, rmsVolume, Time.deltaTime * 2f);
            }
        }
        else
        {
            // 再生していない場合は減衰
            CurrentVolume = Mathf.Lerp(CurrentVolume, 0f, Time.deltaTime * decaySpeed);
            peakVolume = Mathf.Lerp(peakVolume, 0f, Time.deltaTime * 2f);
        }

        // 平滑化された音量を計算
        SmoothedVolume = Mathf.Lerp(SmoothedVolume, CurrentVolume, Time.deltaTime * smoothSpeed);
    }

    /// <summary>
    /// 監視対象のAudioSourceを動的に設定する
    /// </summary>
    public void SetAudioSource(AudioSource source)
    {
        targetAudioSource = source;
        Debug.Log($"[AudioVolumeProvider] AudioSource を設定: {source?.name ?? "null"}");
    }

    /// <summary>
    /// ピーク値をリセットする
    /// </summary>
    public void ResetPeak()
    {
        peakVolume = 0f;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
