using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class MessageVoicePlayer : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private FlowManager flowManager; // ★追加
    [SerializeField] private string fileExtension = "*.wav"; // 必要に応じて .mp3 等に変更

    private FileSystemWatcher watcher;
    private Queue<string> playQueue = new Queue<string>();
    private object lockObject = new object();
    private bool isPlaying = false;

    void Start()
    {
        string voicePath = Path.Combine(Application.streamingAssetsPath, "voice");

        if (!Directory.Exists(voicePath))
        {
            Directory.CreateDirectory(voicePath);
        }

        StartWatcher(voicePath);
    }

    private void StartWatcher(string path)
    {
        watcher = new FileSystemWatcher(path);
        watcher.Filter = fileExtension;
        watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime;

        // 生成イベントの購読
        watcher.Created += OnFileCreated;

        watcher.EnableRaisingEvents = true;
    }

    // 別スレッドから呼ばれるため、キューにパスを積むのみとする
    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        lock (lockObject)
        {
            playQueue.Enqueue(e.FullPath);
        }
    }

    void Update()
    {
        // 再生中なら次の処理を行わない
        if (isPlaying) return;

        string nextPath = null;

        lock (lockObject)
        {
            if (playQueue.Count > 0)
            {
                nextPath = playQueue.Dequeue();
            }
        }

        if (!string.IsNullOrEmpty(nextPath))
        {
            StartCoroutine(PlayVoice(nextPath));
        }
    }

    IEnumerator PlayVoice(string path)
    {
        isPlaying = true;

        // Mac/Windowsのローカルファイルロードには file:// が必要
        string url = "file://" + path;

        // 生成直後はファイルロックがかかっている可能性があるためごく短時間待機
        yield return new WaitForSeconds(0.1f);

        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr);

                // クリップ名を設定（デバッグ用）
                clip.name = Path.GetFileName(path);

                audioSource.clip = clip;

                // ★追加: FlowManagerに時間を通知 (clip.length)
                if (flowManager != null)
                {
                    flowManager.SetMessageDuration(clip.length);
                }

                // ★修正: FlowStateがMessageになるまで待機してから再生
                if (flowManager != null)
                {
                    Debug.Log("[MessageVoicePlayer] Message状態になるまで待機中...");
                    while (flowManager.CurrentState != FlowManager.FlowState.Message)
                    {
                        yield return null;
                    }
                    Debug.Log("[MessageVoicePlayer] Message状態検出、再生開始");
                }

                audioSource.Play();

                // 再生終了まで待機
                yield return new WaitForSeconds(clip.length);
            }
            else
            {
                Debug.LogError($"Load Error: {uwr.error} : {url}");
            }
        }

        isPlaying = false;
    }

    private void OnDestroy()
    {
        if (watcher != null)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Created -= OnFileCreated;
            watcher.Dispose();
        }
    }
}