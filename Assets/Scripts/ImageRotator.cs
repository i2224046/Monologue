using UnityEngine;

public class ImageRotator : MonoBehaviour
{
    // 1秒あたりの回転速度（度）
    public float rotationSpeed = 100f;

    private RectTransform rectTransform;

    void Start()
    {
        // このスクリプトがアタッチされているGameObjectのRectTransformを取得
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        // Z軸を軸として、毎フレーム回転させる
        // Time.deltaTimeを乗算し、フレームレートに依存しないようにする
        rectTransform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }
}