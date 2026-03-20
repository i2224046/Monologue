using UnityEngine;

/// <summary>
/// Scanning状態時にScanningHistorySpawnerを制御するコントローラー。
/// FlowManagerの状態を監視し、Scanning開始/終了時にSpawnerを制御する。
/// ScanningContentDisplay.cs と同じパターン。
/// </summary>
public class ScanningHistoryDisplay : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("状態を監視するFlowManager")]
    [SerializeField] private FlowManager flowManager;

    [Tooltip("制御対象のScanningHistorySpawner")]
    [SerializeField] private ScanningHistorySpawner historySpawner;

    private bool wasScanning = false;

    private void Update()
    {
        if (flowManager == null || historySpawner == null) return;

        bool isScanning = flowManager.CurrentState == FlowManager.FlowState.Scanning;

        // Scanning状態に入った瞬間
        if (isScanning && !wasScanning)
        {
            historySpawner.StartSpawning();
        }
        // Scanning状態から抜けた瞬間
        else if (!isScanning && wasScanning)
        {
            historySpawner.StopSpawning();
        }

        wasScanning = isScanning;
    }
}
