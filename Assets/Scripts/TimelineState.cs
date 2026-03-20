using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 各ステート（Waiting, Scanningなど）のUIプレハブにつけるスクリプト。
/// 表示された瞬間にTimelineを再生するなどの制御を行う。
/// </summary>
public class TimelineState : MonoBehaviour
{
    [SerializeField] private PlayableDirector director;

    /// <summary>
    /// このステートに入るときに呼ばれる
    /// </summary>
    public void Enter()
    {
        gameObject.SetActive(true);

        if (director != null)
        {
            // 開始位置に戻して再生
            director.time = 0;
            director.Play();
        }
    }

    /// <summary>
    /// このステートから出るときに呼ばれる
    /// </summary>
    public void Exit()
    {
        if (director != null)
        {
            director.Stop();
        }
        gameObject.SetActive(false);
    }
}
