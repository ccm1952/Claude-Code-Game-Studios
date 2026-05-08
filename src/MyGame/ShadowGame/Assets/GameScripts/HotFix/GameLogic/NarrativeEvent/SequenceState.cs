// 该文件由Cursor 自动生成
namespace GameLogic
{
    /// <summary>
    /// NarrativeSequencePlayer 的运行状态机（per ADR-016 §C 设计）。
    /// </summary>
    public enum SequenceState
    {
        /// <summary>空闲 — 无活动 sequence；可立即接受新请求或 dequeue 队列项。</summary>
        Idle = 0,

        /// <summary>正在播放 — 当前有活动 sequence；新请求被 enqueue（或 overflow 时 OnSequenceFailed）。</summary>
        Playing = 1,

        /// <summary>已暂停 — App pause / OnApplicationPause 触发；timer 暂停，state 保留；resume 后从断点继续（per AC-14）。</summary>
        Paused = 2,
    }
}
