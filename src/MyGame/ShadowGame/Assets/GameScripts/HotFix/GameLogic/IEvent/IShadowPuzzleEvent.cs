// 该文件由Cursor 自动生成
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// Puzzle State Machine 广播接口（PuzzleStateMachine → HintSystem / NarrativeDirector / AudioFeedback / VFX）。
    /// <para>Sender: PuzzleStateMachine（per ADR-014 §B Production code paths；S5-03 dev-story 创建）</para>
    /// <para>Listener: HintSystem（暂停 hint timer on PerfectMatch / AbsenceAccepted）, NarrativeDirector
    /// （触发 puzzle complete narrative sequence）, AudioFeedback（播放反馈 SFX）, VFX（snap animation trigger）</para>
    /// <para>协议来源：ADR-027 §1 per-event 模式；ADR-014 §A 5 sender 设计。</para>
    /// <para>Cascade depth budget: 2（OnPerfectMatch / OnAbsenceAccepted 可触发 OnPuzzleComplete + IPuzzleLockEvent.OnPuzzleLockAll
    /// + IChapterStateEvent.OnPuzzleStateChanged；合计深度 ≤ 3，per ADR-027 §2 base guardrail）。</para>
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IShadowPuzzleEvent
    {
        /// <summary>
        /// matchScore 跨过 NearMatch 进入阈值（默认 0.40）— 进入 NearMatch 状态。
        /// <para>Listener 用途: 触发 hint 提示动画 / 渐显 outline 强调。</para>
        /// <para>Cascade depth: 0（终端事件；不再触发其他事件）</para>
        /// </summary>
        void OnNearMatchEnter(int puzzleId);

        /// <summary>
        /// matchScore 跌回 NearMatch 退出阈值（默认 0.35，hysteresis ±0.05 防 flicker）— 退回 Active 状态。
        /// <para>Listener 用途: 取消 hint 提示动画 / 收起 outline。</para>
        /// <para>Cascade depth: 0</para>
        /// </summary>
        void OnNearMatchExit(int puzzleId);

        /// <summary>
        /// matchScore ≥ PerfectMatch 阈值（默认 0.85）+ 不在 tutorial grace period — 进入 PerfectMatch（不可逆）。
        /// <para>Sender: 进入 PerfectMatch 状态后立即派发；matchScore frozen on entry。</para>
        /// <para>Listener 用途: NarrativeDirector 触发 puzzle complete sequence；AudioFeedback 播放成就 SFX；
        /// VFX 触发 snap animation；HintSystem pause hint timer。</para>
        /// <para>Cascade depth: 1（监听者可触发 OnPuzzleComplete / IPuzzleLockEvent.OnPuzzleLockAll，合计深度 ≤ 3）</para>
        /// </summary>
        void OnPerfectMatch(int puzzleId, float finalMatchScore);

        /// <summary>
        /// Absence puzzle (Chapter 5) 满足 absence 接受条件 — 进入 AbsenceAccepted（不可逆）。
        /// <para>触发条件: matchScore ≥ maxCompletionScore (0.60-0.70) + 持续 absenceAcceptDelay (5s) + 不在 grace period。</para>
        /// <para>Listener 用途: NarrativeDirector 触发 absence narrative；AudioFeedback 静默渐入。</para>
        /// <para>Cascade depth: 1</para>
        /// </summary>
        void OnAbsenceAccepted(int puzzleId, float finalMatchScore);

        /// <summary>
        /// Puzzle 整体生命周期完成事件（PerfectMatch 或 AbsenceAccepted 后派发；与 Complete 状态对应）。
        /// <para>Sender: PuzzleStateMachine 在 PerfectMatch / AbsenceAccepted 转换后派发（仅一次 per puzzle）。</para>
        /// <para>Listener 用途: ChapterStateManager（上报 puzzle complete 决策 chapter 进度）。</para>
        /// <para>Cascade depth: 2（Listener 可触发 IChapterStateEvent.OnPuzzleStateChanged →
        /// OnChapterComplete，合计深度 ≤ 3）</para>
        /// </summary>
        void OnPuzzleComplete(int puzzleId, PuzzleCompletionType completionType);
    }

    /// <summary>
    /// Puzzle 完成方式分类（per ADR-014 §A 5 sender 设计）。
    /// </summary>
    public enum PuzzleCompletionType
    {
        /// <summary>标准 PerfectMatch 完成（matchScore ≥ 0.85）</summary>
        Perfect = 0,

        /// <summary>Absence 完成（仅 Chapter 5 absence puzzles；matchScore ≥ maxCompletionScore + 5s idle）</summary>
        Absence = 1,
    }
}
