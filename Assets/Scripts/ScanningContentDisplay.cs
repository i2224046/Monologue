using UnityEngine;

/// <summary>
/// Scanning状態時にScanningContentRotatorを制御するコントローラー。
/// FlowManagerの状態を監視し、Scanning開始/終了時にScanningContentRotatorを制御する。
/// </summary>
public class ScanningContentDisplay : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("状態を監視するFlowManager")]
    [SerializeField] private FlowManager flowManager;

    [Tooltip("制御対象のScanningContentRotator")]
    [SerializeField] private ScanningContentRotator contentRotator;

    private bool wasScanning = false;

    private void Update()
    {
        if (flowManager == null || contentRotator == null) return;

        bool isScanning = flowManager.CurrentState == FlowManager.FlowState.Scanning;

        // Scanning状態に入った瞬間
        if (isScanning && !wasScanning)
        {
            contentRotator.StartRotation();
        }
        // Scanning状態から抜けた瞬間
        else if (!isScanning && wasScanning)
        {
            contentRotator.StopRotation();
        }

        wasScanning = isScanning;
    }
}
