using UnityEngine;
using System.IO;
using System;

public class MessageFileManager : MonoBehaviour
{
    public string fileName = "Message.txt";
    public event Action<string[]> OnFileUpdated;

    private string filePath;
    private DateTime lastWriteTime;
    private float timer;

    void Start()
    {
        filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        ReadFile(); // 初回読み込み
    }

    void Update()
    {
        // 1秒ごとに更新チェック
        timer += Time.deltaTime;
        if (timer < 1.0f) return;
        timer = 0;

        if (File.Exists(filePath))
        {
            DateTime currentWriteTime = File.GetLastWriteTime(filePath);
            if (currentWriteTime != lastWriteTime)
            {
                ReadFile();
            }
        }
    }

    private void ReadFile()
    {
        if (!File.Exists(filePath)) return;

        try
        {
            lastWriteTime = File.GetLastWriteTime(filePath);
            string[] lines = File.ReadAllLines(filePath);
            OnFileUpdated?.Invoke(lines);
        }
        catch (Exception e)
        {
            Debug.LogError($"読み込みエラー: {e.Message}");
        }
    }
}