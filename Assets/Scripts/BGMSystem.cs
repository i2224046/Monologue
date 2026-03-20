using UnityEngine;
using System.Collections;

/// <summary>
/// Waiting状態専用のBGM再生システム。
/// FlowStateを監視し、Waiting開始でフェードイン、Waiting終了でフェードアウト。
/// </summary>
public class BGMSystem : MonoBehaviour
{
    [Header("依存関係")]
    [SerializeField] private FlowManager flowManager;

    [Header("BGM設定")]
    [Tooltip("Waiting状態で再生するBGM")]
    [SerializeField] private AudioClip waitingBGM;

    [Tooltip("BGMの最大音量")]
    [SerializeField, Range(0f, 1f)] private float maxVolume = 0.5f;

    [Header("フェード設定")]
    [Tooltip("フェードイン時間（秒）")]
    [SerializeField] private float fadeInDuration = 1.0f;

    [Tooltip("フェードアウト時間（秒）")]
    [SerializeField] private float fadeOutDuration = 2.0f;

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;
    private bool wasWaiting = false;

    void Awake()
    {
        // AudioSourceをこのGameObjectに追加
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
    }

    void Update()
    {
        if (flowManager == null) return;

        bool isWaiting = flowManager.CurrentState == FlowManager.FlowState.Waiting;

        // Waiting状態に入った
        if (isWaiting && !wasWaiting)
        {
            StartWaitingBGM();
        }
        // Waiting状態から抜けた
        else if (!isWaiting && wasWaiting)
        {
            StopWaitingBGM();
        }

        wasWaiting = isWaiting;
    }

    /// <summary>
    /// BGM再生開始（フェードイン付き）
    /// </summary>
    private void StartWaitingBGM()
    {
        if (waitingBGM == null)
        {
            Debug.LogWarning("[BGMSystem] waitingBGMが設定されていません");
            return;
        }

        Debug.Log("[BGMSystem] Waiting BGM 再生開始");

        // 既存のフェード処理をキャンセル
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        audioSource.clip = waitingBGM;
        audioSource.Play();
        fadeCoroutine = StartCoroutine(FadeVolume(audioSource.volume, maxVolume, fadeInDuration));
    }

    /// <summary>
    /// BGM停止（フェードアウト付き）
    /// </summary>
    private void StopWaitingBGM()
    {
        Debug.Log("[BGMSystem] Waiting BGM フェードアウト開始");

        // 既存のフェード処理をキャンセル
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeOutAndStop());
    }

    /// <summary>
    /// フェードアウト後に停止するコルーチン
    /// </summary>
    private IEnumerator FadeOutAndStop()
    {
        yield return FadeVolume(audioSource.volume, 0f, fadeOutDuration);
        audioSource.Stop();
        Debug.Log("[BGMSystem] Waiting BGM 停止完了");
    }

    /// <summary>
    /// 音量をフェードさせるコルーチン
    /// </summary>
    private IEnumerator FadeVolume(float startVolume, float targetVolume, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // イーズイン・アウトで滑らかに
            t = t * t * (3f - 2f * t);
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        audioSource.volume = targetVolume;
        fadeCoroutine = null;
    }
}
