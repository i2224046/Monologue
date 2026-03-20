using UnityEngine;
using TMPro;

public class RuneBehavior : MonoBehaviour
{
    private Transform target; // 吸い込まれる先
    private float timeAlive;
    private Vector3 startPosition; // 開始位置を記憶
    
    // 動きの調整パラメータ
    public float expandSpeed = 2.0f;          // 拡散速度
    public Vector2 expandRadius = new Vector2(2.0f, 0.8f);  // 楕円の半径（X横, Y縦）
    public float suctionSpeed = 5.0f;         // 吸い込み速度
    public float lifeTime = 2.0f;             // 何秒で吸い込まれきるか

    private Vector3 expandDirection; // 拡散方向（ランダムな角度、楕円形）
    private TMP_Text tmpText;

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        
        // ランダムな角度で拡散方向を決定（楕円形）
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        // X軸とY軸に異なる半径を適用して楕円形にする
        expandDirection = new Vector3(
            Mathf.Cos(angle) * expandRadius.x,
            Mathf.Sin(angle) * expandRadius.y,
            0
        );
        
        // 少し回転させてルーン文字っぽさを出す
        transform.Rotate(0, 0, Random.Range(-15f, 15f));
    }

    // スポーナーから呼ばれる初期化関数
    public void Initialize(string charText, Transform targetTransform)
    {
        if (tmpText != null)
        {
            tmpText.text = charText;
        }
        target = targetTransform;
        startPosition = transform.position;
    }

    void Update()
    {
        if (target == null) { Destroy(gameObject); return; }

        timeAlive += Time.deltaTime;
        float t = Mathf.Clamp01(timeAlive / lifeTime);

        // ターゲットへのベクトル
        Vector3 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;

        // ターゲットに十分近づいたら消滅
        if (distance < 0.1f)
        {
            Destroy(gameObject);
        }
        else
        {
            // === 2段階の動き ===
            
            // 動き1: 楕円形拡散（最初は強く、徐々に弱まる）
            // Ease-out: 最初は速く、徐々に減速
            float expandT = 1f - Mathf.Pow(1f - Mathf.Clamp01(t * 2f), 2f);
            Vector3 expandOffset = expandDirection * expandT;
            
            // 動き2: 目標点への吸い込み（徐々に強くなる）
            // Ease-in: 最初は遅く、徐々に加速
            float suctionT = t * t * t; // 3乗でより緩やかにスタート
            Vector3 targetPosition = Vector3.Lerp(startPosition + expandOffset, target.position, suctionT);
            
            // 位置を直接設定（Lerpで滑らかに）
            transform.position = Vector3.Lerp(transform.position, targetPosition, suctionSpeed * Time.deltaTime);
        }
    }
}