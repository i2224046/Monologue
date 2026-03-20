using UnityEngine;
using System.Diagnostics;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text;

public class PythonLauncher : MonoBehaviour, IDisposable
{
    [Header("Dependencies")]
    [Tooltip("メッセージを振り分けるRouter")]
    [SerializeField] private PythonMessageRouter messageRouter;

    private Process pythonProcess;
    private StreamWriter stdinWriter;  // stdinへの書き込み用
    private string pythonExecutablePath = "/opt/homebrew/bin/python3.11";
    private static readonly Queue<string> resultQueue = new Queue<string>();

    // Start() で起動する
    void Start()
    {
        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            UnityEngine.Debug.LogWarning("Pythonプロセスは既に実行中です。");
            return;
        }

        string scriptPath = Path.Combine(Application.streamingAssetsPath, "main_vision_voice.py");
        string workingDirectory = Path.Combine(Application.streamingAssetsPath);

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = pythonExecutablePath,
            Arguments = scriptPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,  // stdin書き込みを有効化
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        try
        {
            pythonProcess = new Process();
            pythonProcess.StartInfo = startInfo;

            // 標準出力のイベントハンドラ
            pythonProcess.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    lock (resultQueue)
                    {
                        resultQueue.Enqueue(args.Data);
                    }
                }
            };
            // 標準エラーのイベントハンドラ
            pythonProcess.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    UnityEngine.Debug.LogError("Python Error: " + args.Data);
                }
            };

            pythonProcess.Start();
            pythonProcess.BeginOutputReadLine();
            pythonProcess.BeginErrorReadLine();
            
            // stdin書き込み用のStreamWriterを取得
            stdinWriter = pythonProcess.StandardInput;
            stdinWriter.AutoFlush = true;  // 即座にフラッシュ

            UnityEngine.Debug.Log($"Pythonプロセスを開始しました: {scriptPath}");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Pythonプロセスの起動に失敗: {e.Message}");
            // 起動失敗時もRouterに通知
            if (messageRouter != null)
            {
                messageRouter.OnPythonError("起動失敗: " + e.Message);
            }
        }
    }

    void Update()
    {
        // メインスレッドでキューを処理
        while (resultQueue.Count > 0)
        {
            string line = null;
            lock (resultQueue)
            {
                if (resultQueue.Count > 0)
                {
                    line = resultQueue.Dequeue();
                }
            }

            if (line != null)
            {
                // Routerが設定されていれば転送
                if (messageRouter != null)
                {
                    messageRouter.OnPythonOutput(line);
                }
                else
                {
                    // Routerがない場合はデバッグログ
                    UnityEngine.Debug.Log("Python: " + line);
                }
            }
        }
    }

    /// <summary>
    /// Pythonプロセスにコマンドを送信する
    /// </summary>
    /// <param name="command">送信するコマンド（例: "CAPTURE", "QUIT"）</param>
    public void SendCommand(string command)
    {
        if (stdinWriter != null && pythonProcess != null && !pythonProcess.HasExited)
        {
            try
            {
                stdinWriter.WriteLine(command);
                UnityEngine.Debug.Log($"[PythonLauncher] コマンド送信: {command}");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[PythonLauncher] コマンド送信失敗: {e.Message}");
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning("[PythonLauncher] Pythonプロセスが実行されていないため、コマンドを送信できません。");
        }
    }

    /// <summary>
    /// キャプチャコマンドを送信する（外部から呼び出し用）
    /// </summary>
    public void SendCaptureCommand()
    {
        SendCommand("CAPTURE");
    }

    void OnApplicationQuit()
    {
        KillProcess();
    }

    void OnDestroy()
    {
        KillProcess();
    }
    
    public void Dispose()
    {
        KillProcess();
    }
    
    private void KillProcess()
    {
        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            try
            {
                // まずQUITコマンドを送信して graceful shutdown を試みる
                if (stdinWriter != null)
                {
                    stdinWriter.WriteLine("QUIT");
                    stdinWriter.Close();
                    stdinWriter = null;
                }
                
                // 少し待ってから強制終了
                if (!pythonProcess.WaitForExit(2000))
                {
                    pythonProcess.Kill();
                    UnityEngine.Debug.Log("Pythonプロセスを強制終了しました。");
                }
                else
                {
                    UnityEngine.Debug.Log("Pythonプロセスが正常に終了しました。");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Pythonプロセスの終了に失敗: {e.Message}");
            }
            pythonProcess.Dispose();
            pythonProcess = null;
        }
    }
}