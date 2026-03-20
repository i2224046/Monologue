using UnityEngine;

/// <summary>
/// TypewriterEffectTMPと連携し、タイピング音を再生するスクリプト
/// 同じGameObjectにアタッチして使用する
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class TypingSoundPlayer : MonoBehaviour
{
    [Header("音声設定")]
    [Tooltip("タイピング音のAudioClip（一音）")]
    public AudioClip typingSound;

    [Range(0f, 1f)]
    [Tooltip("タイピング音の音量")]
    public float volume = 0.5f;

    [Header("再生設定")]
    [Tooltip("タイピング音の再生を有効にするか")]
    public bool enableTypingSound = true;

    [Tooltip("ピッチのランダム幅（0で固定、0.1で±0.1の範囲でランダム）")]
    [Range(0f, 0.3f)]
    public float pitchVariation = 0.05f;

    [Header("参照")]
    [Tooltip("連携するTypewriterEffectTMP（空の場合は同じGameObjectから自動取得）")]
    public TypewriterEffectTMP typewriterEffect;

    private AudioSource audioSource;

    void Awake()
    {
        // AudioSourceの取得または作成
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // AudioSourceの設定
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        // TypewriterEffectTMPの自動取得
        if (typewriterEffect == null)
        {
            typewriterEffect = GetComponent<TypewriterEffectTMP>();
        }
    }

    void OnEnable()
    {
        // イベント購読
        if (typewriterEffect != null)
        {
            typewriterEffect.OnCharacterTyped += HandleCharacterTyped;
        }

        // ★追加: AudioVolumeProviderにAudioSourceを登録
        if (AudioVolumeProvider.Instance != null && audioSource != null)
        {
            AudioVolumeProvider.Instance.SetAudioSource(audioSource);
            Debug.Log("[TypingSoundPlayer] AudioVolumeProvider に AudioSource を登録しました。");
        }
    }

    void OnDisable()
    {
        // イベント解除
        if (typewriterEffect != null)
        {
            typewriterEffect.OnCharacterTyped -= HandleCharacterTyped;
        }

        // ★追加: AudioVolumeProviderからAudioSourceを解除
        if (AudioVolumeProvider.Instance != null)
        {
            AudioVolumeProvider.Instance.SetAudioSource(null);
            Debug.Log("[TypingSoundPlayer] AudioVolumeProvider から AudioSource を解除しました。");
        }
    }

    /// <summary>
    /// 文字がタイプされた時に呼ばれるハンドラ
    /// </summary>
    private void HandleCharacterTyped()
    {
        PlayTypingSound();
    }

    /// <summary>
    /// タイピング音を再生する
    /// </summary>
    public void PlayTypingSound()
    {
        if (!enableTypingSound) return;
        if (typingSound == null) return;
        if (audioSource == null) return;

        // ピッチにランダム性を追加（同じ音が続くと単調になるため）
        float randomPitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        audioSource.pitch = randomPitch;

        // 音量を設定して再生
        audioSource.PlayOneShot(typingSound, volume);
    }

    /// <summary>
    /// 音量を設定する（外部から呼び出し可能）
    /// </summary>
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
    }

    /// <summary>
    /// タイピング音のオン/オフを切り替える
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        enableTypingSound = enabled;
    }
}
