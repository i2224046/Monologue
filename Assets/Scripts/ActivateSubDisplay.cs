using UnityEngine;

public class ActivateSubDisplay : MonoBehaviour
{
    void Start()
    {
        // 接続されているディスプレイの数をログに出力
        Debug.Log("Displays connected: " + Display.displays.Length);

        // 2台以上のディスプレイが接続されている場合
        if (Display.displays.Length > 1)
        {
            // 2台目のディスプレイ（インデックス 1）を有効化する
            // Display.displays[0] はプライマリディスプレイ
            Display.displays[1].Activate();
            Debug.Log("Activated display 2.");
        }
        else
        {
            Debug.Log("Only one display connected. No sub-display to activate.");
        }
    }
}