using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageFader : MonoBehaviour
{
    [Tooltip("フェードインにかかる時間（秒）")]
    [SerializeField] private float fadeDuration = 1.0f;

    [Tooltip("フェードイン開始までの遅延時間（秒）")]
    [SerializeField] private float startDelay = 0.0f; // 追加

    private Image image;
    private Coroutine currentFadeCoroutine;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    private void OnEnable()
    {
        // 既存のコルーチンがあれば停止
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }

        // 遅延付きフェードインコルーチンを開始
        currentFadeCoroutine = StartCoroutine(StartFadeWithDelay());
    }

    // 追加: 遅延を処理するコルーチン
    private IEnumerator StartFadeWithDelay()
    {
        // 開始時はアルファ値を0（透明）に設定
        SetAlpha(0f);

        // 設定された遅延時間待機
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        // 実際のフェードイン処理（FadeInコルーチン）を開始し、それが終わるのを待つ
        yield return StartCoroutine(FadeIn());

        // 全てのプロセスが完了したらコルーチン参照をnullにする
        currentFadeCoroutine = null;
    }

    private IEnumerator FadeIn()
    {
        // SetAlpha(0f); // StartFadeWithDelayに移動

        float timer = 0f;

        // durationが0以下の場合は即座にアルファを1にする
        if (fadeDuration <= 0f)
        {
            SetAlpha(1f);
            // currentFadeCoroutine = null; // StartFadeWithDelayの最後に移動
            yield break;
        }

        // 時間をかけてアルファ値を1（不透明）に変更
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(timer / fadeDuration);
            SetAlpha(alpha);
            yield return null;
        }

        // 確実にアルファ値を1にする
        SetAlpha(1f);
        // currentFadeCoroutine = null; // StartFadeWithDelayの最後に移動
    }

    private void SetAlpha(float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}