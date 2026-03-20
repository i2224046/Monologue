using UnityEngine;

/// <summary>
/// 画像とメッセージのペアデータ（Python側のMessagePairs.jsonと対応）
/// </summary>
[System.Serializable]
public class MessagePairData
{
    public string image;
    public string message;
    public string credit;
    public string timestamp;
}

/// <summary>
/// MessagePairs.jsonのルートオブジェクト
/// </summary>
[System.Serializable]
public class MessagePairList
{
    public MessagePairData[] pairs;
}
