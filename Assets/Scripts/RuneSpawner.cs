using UnityEngine;
using System.Collections;

public class RuneSpawner : MonoBehaviour
{
    [Header("設定")]
    [TextArea] public string message = "MAGIC"; // 飛ばす文字（デフォルト値、Pythonから上書きされる）
    public GameObject runePrefab; // 文字プレハブ
    public Transform enchantTable; // 吸い込まれる先
    
    [Header("タイミング調整")]
    public float charInterval = 0.1f; // "1文字"が出る間隔
    public float loopInterval = 2.0f; // "単語全体"を繰り返す間隔
    public bool loopEnabled = true;   // ループのオン/オフ
    
    [Header("開始条件")]
    [Tooltip("ON: パネル表示時にデフォルトメッセージで自動開始\nOFF: Pythonからメッセージ受信時のみ開始（推奨）")]
    public bool autoStartOnEnable = false;

    private Coroutine spawnCoroutine;
    private bool hasPendingMessage = false; // 外部からメッセージを受け取ったフラグ

    void OnEnable()
    {
        Debug.Log($"[RuneSpawner] OnEnable: autoStartOnEnable={autoStartOnEnable}, hasPendingMessage={hasPendingMessage}, message='{message}'");
        
        // autoStartOnEnable がON、または Pythonからメッセージを受け取った場合のみ開始
        if (autoStartOnEnable || hasPendingMessage)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Debug.Log($"[RuneSpawner] OnEnable: スポーン開始します");
                StartSpawning();
            }
            hasPendingMessage = false; // フラグをリセット
        }
        else
        {
            Debug.Log($"[RuneSpawner] OnEnable: スポーンしません（条件未満たたず）");
        }
    }

    void OnDisable()
    {
        StopSpawning();
    }

    /// <summary>
    /// Pythonからのメッセージを受け取って、ルーン文字として生成開始
    /// </summary>
    public void SetMessage(string newMessage)
    {
        Debug.Log($"[RuneSpawner] SetMessage呼び出し: newMessage='{newMessage}', activeInHierarchy={gameObject.activeInHierarchy}");
        
        if (string.IsNullOrEmpty(newMessage)) return;
        
        message = newMessage;
        Debug.Log($"[RuneSpawner] メッセージ更新完了: message='{message}'");
        
        // GameObjectがアクティブな場合のみ開始
        // 非アクティブの場合はOnEnableで開始される
        if (gameObject.activeInHierarchy)
        {
            Debug.Log($"[RuneSpawner] アクティブなので即座に開始");
            StartSpawning();
        }
        else
        {
            // 非アクティブの場合はフラグを立てておく
            hasPendingMessage = true;
            Debug.Log($"[RuneSpawner] 非アクティブなのでhasPendingMessage=trueに設定");
        }
    }

    /// <summary>
    /// ルーン文字の生成を開始
    /// </summary>
    public void StartSpawning()
    {
        Debug.Log($"[RuneSpawner] StartSpawning: activeInHierarchy={gameObject.activeInHierarchy}");
        
        // 非アクティブなGameObjectではコルーチンを開始できない
        if (!gameObject.activeInHierarchy)
        {
            Debug.Log($"[RuneSpawner] StartSpawning: 非アクティブのためスキップ");
            return;
        }
        
        StopSpawning();
        Debug.Log($"[RuneSpawner] StartSpawning: コルーチン開始します - message='{message}'");
        spawnCoroutine = StartCoroutine(AutoSpawnLoop());
    }

    /// <summary>
    /// ルーン文字の生成を停止
    /// </summary>
    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    IEnumerator AutoSpawnLoop()
    {
        do
        {
            // 1. 文字列を生成する
            yield return StartCoroutine(SpawnStringRoutine());

            // 2. 次のセットが出るまで待機
            if (loopEnabled)
            {
                yield return new WaitForSeconds(loopInterval);
            }
        } while (loopEnabled);
    }

    IEnumerator SpawnStringRoutine()
    {
        Debug.Log($"[RuneSpawner] SpawnStringRoutine開始: 表示するメッセージ = '{message}'");
        char[] characters = message.ToCharArray();

        foreach (char c in characters)
        {
            if (c == ' ') continue; // 空白は飛ばす

            // 出現位置を少しランダムに（World座標）
            Vector3 spawnPos = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
            
            // Canvas配下に生成（親を指定）+ World座標で位置設定
            GameObject runeObj = Instantiate(runePrefab, spawnPos, Quaternion.identity, transform.parent);
            
            RuneBehavior runeScript = runeObj.GetComponent<RuneBehavior>();
            if (runeScript != null)
            {
                runeScript.Initialize(c.ToString(), enchantTable);
            }

            // 次の文字が出るまで少し待つ
            yield return new WaitForSeconds(charInterval);
        }
    }
}