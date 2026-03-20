using UnityEngine;

/// <summary>
/// Scanning状態時にTextRotatorを制御するコントローラー。
/// FlowManagerの状態を監視し、Scanning開始/終了時にTextRotatorを制御する。
/// </summary>
public class ScanningTextDisplay : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("状態を監視するFlowManager")]
    [SerializeField] private FlowManager flowManager;

    [Tooltip("制御対象のTextRotator")]
    [SerializeField] private TextRotator textRotator;

    private bool wasScanning = false;

    private void Update()
    {
        if (flowManager == null || textRotator == null) return;

        bool isScanning = flowManager.CurrentState == FlowManager.FlowState.Scanning;

        // Scanning状態に入った瞬間
        if (isScanning && !wasScanning)
        {
            textRotator.StartRotation();
        }
        // Scanning状態から抜けた瞬間
        else if (!isScanning && wasScanning)
        {
            textRotator.StopRotation();
        }

        wasScanning = isScanning;
    }
}
