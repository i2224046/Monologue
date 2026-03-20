using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class MessageWindowManager : MonoBehaviour
{
    [Header("参照")]
    public GameObject messageWindowPrefab;
    public Transform contentParent;

    [Header("設定")]
    public int maxLines = 50;       // 画面内の最大表示数
    public int minLines = 0;        // 画面内の最小表示数
    public float displayDuration = 10.0f; // 表示時間 (ライフタイム)
    public Vector2 margin = new Vector2(50, 50);

    private string[] allMessages;
    private int nextMessageIndex = 0;
    private float timer;

    // activeWindowsキューは管理用として残すが、自動消滅するため明示的な削除は基本的に不要になる
    // ただし、数が多すぎる場合の安全策として残しておくこともできるが、
    // 今回の要件「10秒ずつ表示」に合わせるため、生成のみを管理する形にする。
    private Queue<GameObject> activeWindows = new Queue<GameObject>();

    void Update()
    {
        // 破棄されたウィンドウをキューから削除
        while (activeWindows.Count > 0 && activeWindows.Peek() == null)
        {
            activeWindows.Dequeue();
        }

        if (allMessages == null || allMessages.Length == 0) return;

        int targetCount = GetTargetWindowCount();
        if (targetCount <= 0) return;

        // ターゲット数を維持するために必要な生成間隔を計算
        // 例: 10個維持したい、1つ10秒持つ -> 1秒に1個生成すればよい (10s / 10 = 1s)
        float spawnInterval = displayDuration / (float)targetCount;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0;
            SpawnNewWindow();
        }
    }

    // ファイル読み込み時に呼ばれる
    public void UpdateMessageWindows(string[] messages)
    {
        if (messages == null) return;

        allMessages = messages.Where(m => !string.IsNullOrWhiteSpace(m)).ToArray();

        if (allMessages.Length == 0) return;

        // 初回ロード時、まだウィンドウがなければ一気に生成するが、
        // すべて同時に消えないようにライフタイムをずらす
        if (activeWindows.Count == 0)
        {
            int targetCount = GetTargetWindowCount();
            float interval = displayDuration / (float)targetCount;

            for (int i = 0; i < targetCount; i++)
            {
                // 残り時間をずらして生成
                // 最初の1個はもうすぐ消える(残り時間わずか)、最後の1個はフルに残る、など分布させる
                // 例: target=10, dur=10. interval=1.
                // i=0: remaining= 1s
                // ...
                // i=9: remaining= 10s
                // ※ 正確には「生成された瞬間」からの寿命を設定するので、
                // ここでは「既に時間が経過した状態」として生成するか、
                // 単に寿命を短く設定して渡すか。
                // MessageWindowAnimationにSetLifetimeで渡すので、渡した時間後に消える。
                // つまり、0番目は interval 秒後に消えてほしい。
                // 9番目は targetCount * interval 秒後に消えてほしい。
                float lifetime = (i + 1) * interval;
                CreateNextWindow(lifetime);
            }
        }
    }

    private void SpawnNewWindow()
    {
        // 定期生成用
        CreateNextWindow(displayDuration);
    }


    private int GetTargetWindowCount()
    {
        if (allMessages == null) return 0;
        // メッセージ数とminLinesの大きい方を取り、maxLinesでキャップする
        // 例: msg=10, min=20, max=50 -> clamp(20, 0, 50) = 20 (ループして表示)
        // 例: msg=10, min=5, max=50 -> clamp(10, 0, 50) = 10 (全メッセージ表示)
        // 例: msg=100, min=5, max=50 -> clamp(100, 0, 50) = 50 (50個表示)
        int desired = Mathf.Max(allMessages.Length, minLines);
        return Mathf.Clamp(desired, 0, maxLines);
    }

    // 次のメッセージを使ってウィンドウを生成する共通処理
    private void CreateNextWindow(float lifetime)
    {
        if (allMessages == null || allMessages.Length == 0) return;

        string msg = allMessages[nextMessageIndex];
        CreateWindow(msg, lifetime);

        // インデックスを進める（ループ）
        nextMessageIndex++;
        if (nextMessageIndex >= allMessages.Length)
        {
            nextMessageIndex = 0;
        }
    }

    private void CreateWindow(string msg, float lifetime)
    {
        GameObject obj = Instantiate(messageWindowPrefab, contentParent);
        // 管理用キューに入れるが、あふれた場合の制御などは別途検討用
        activeWindows.Enqueue(obj);

        var tmp = obj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = msg;

        // ライフタイム設定
        var anim = obj.GetComponent<MessageWindowAnimation>();
        if (anim != null)
        {
            anim.SetLifetime(lifetime);
        }

        RectTransform rect = obj.GetComponent<RectTransform>();
        RectTransform parentRect = contentParent.GetComponent<RectTransform>();
        SetRandomPosition(rect, parentRect.rect.width, parentRect.rect.height);
    }

    private void SetRandomPosition(RectTransform rect, float areaWidth, float areaHeight)
    {
        if (rect == null) return;
        float xRange = (areaWidth / 2) - margin.x;
        float yRange = (areaHeight / 2) - margin.y;
        rect.anchoredPosition = new Vector2(Random.Range(-xRange, xRange), Random.Range(-yRange, yRange));
    }
}