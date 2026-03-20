using UnityEngine;

/// <summary>
/// Scanning状態中に表示するテキストと画像のペアを保持するデータクラス。
/// Inspectorで設定可能。
/// </summary>
[System.Serializable]
public class ScanningContent
{
    [Tooltip("表示するテキスト")]
    public string message;

    [Tooltip("表示する画像（nullの場合は画像なし）")]
    public Sprite image;
}
