// 该文件由Cursor 自动生成
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// Puzzle 完成后冻结所有交互的广播接口（PuzzleStateMachine → InteractionLockManager / InputBlocker）。
    /// <para>Sender: PuzzleStateMachine（PerfectMatch / AbsenceAccepted 状态转换后立即派发；per
    /// ADR-014 §A "Lock all interaction on puzzle complete"）</para>
    /// <para>Listener: InteractionLockManager（lock InteractableObject input）, InputBlocker（block 全屏 input）</para>
    /// <para>协议来源：ADR-027 §1 per-event 模式；ADR-014 §A puzzle complete cascade 决策。</para>
    /// <para>Cascade depth budget: 1（terminal lock event；listener 仅锁定 input 不再派发其他事件）。</para>
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IPuzzleLockEvent
    {
        /// <summary>
        /// 当前 puzzle 完成 — 锁定所有交互入口，准备 narrative sequence 接管。
        /// <para>Sender: PuzzleStateMachine（per puzzle 仅触发 1 次；PerfectMatch / AbsenceAccepted 转换后）</para>
        /// <para>Listener 用途: InteractionLockManager 调用 LockAll() 阻断后续 drag/rotate；
        /// InputBlocker 推 block layer。</para>
        /// <para>Cascade depth: 0（terminal）</para>
        /// </summary>
        void OnPuzzleLockAll();
    }
}
