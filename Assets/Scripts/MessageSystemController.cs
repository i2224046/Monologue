using UnityEngine;

public class MessageSystemController : MonoBehaviour
{
    public MessageFileManager fileManager;
    public MessageWindowManager windowManager;

    void Start()
    {
        if (fileManager != null && windowManager != null)
        {
            // イベント接続
            fileManager.OnFileUpdated += windowManager.UpdateMessageWindows;
        }
    }

    void OnDestroy()
    {
        if (fileManager != null && windowManager != null)
        {
            // イベント解除
            fileManager.OnFileUpdated -= windowManager.UpdateMessageWindows;
        }
    }
}