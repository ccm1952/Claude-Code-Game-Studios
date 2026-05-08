// 该文件由Cursor 自动生成
namespace GameLogic
{
    /// <summary>
    /// Puzzle State Machine 7-state enum（per ADR-014 §A state diagram）。
    /// </summary>
    /// <remarks>
    /// State transitions（per ADR-014 §B 10 transitions; AC-2 全部可达）：
    /// <list type="bullet">
    ///   <item><c>Locked</c> → <c>Idle</c>（chapter unlock 时；ChapterStateManager 提升）</item>
    ///   <item><c>Idle</c> → <c>Active</c>（任何 player interaction 触发；OnPlayerInteraction）</item>
    ///   <item><c>Active</c> → <c>NearMatch</c>（matchScore ≥ nearMatchThreshold 0.40）</item>
    ///   <item><c>NearMatch</c> → <c>Active</c>（hysteresis: matchScore &lt; nearMatchThreshold - 0.05 = 0.35）</item>
    ///   <item><c>NearMatch / Active</c> → <c>PerfectMatch</c>（matchScore ≥ perfectMatchThreshold 0.85；非 absence puzzle；非 grace period）</item>
    ///   <item><c>NearMatch / Active</c> → <c>AbsenceAccepted</c>（absence puzzle；matchScore ≥ maxCompletionScore + 5s idle；非 grace period）</item>
    ///   <item><c>PerfectMatch / AbsenceAccepted</c> → <c>Complete</c>（立即派发 OnPuzzleComplete）</item>
    /// </list>
    /// PerfectMatch / AbsenceAccepted 不可逆（matchScore frozen on entry，per AC-3 / AC-4）。
    /// </remarks>
    public enum PuzzleState
    {
        /// <summary>
        /// 章节未解锁；不接受任何交互。
        /// </summary>
        Locked = 0,

        /// <summary>
        /// 章节已解锁但 puzzle 还未被 player 触碰；等待第一次 interaction 提升至 Active。
        /// </summary>
        Idle = 1,

        /// <summary>
        /// Player 正在操作 puzzle；matchScore &lt; nearMatchThreshold（远未匹配）。
        /// </summary>
        Active = 2,

        /// <summary>
        /// matchScore 接近匹配阈值（≥ nearMatchThreshold 0.40，hysteresis ±0.05 防 flicker）；
        /// hint 系统应该激活提示动画。
        /// </summary>
        NearMatch = 3,

        /// <summary>
        /// matchScore ≥ perfectMatchThreshold（0.85）— 完美匹配（不可逆）。matchScore frozen on entry。
        /// </summary>
        PerfectMatch = 4,

        /// <summary>
        /// Absence puzzle（Chapter 5）满足 absence 接受条件（不可逆）。
        /// matchScore ≥ maxCompletionScore (0.60-0.70) + 持续 absenceAcceptDelay (5s)。
        /// </summary>
        AbsenceAccepted = 5,

        /// <summary>
        /// Puzzle 生命周期完成；OnPuzzleComplete 已派发。所有 listener cleanup。
        /// </summary>
        Complete = 6,
    }
}
