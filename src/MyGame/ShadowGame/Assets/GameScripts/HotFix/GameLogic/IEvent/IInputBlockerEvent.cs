// 该文件由Cursor 自动生成
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// Input blocker push/pop 广播接口（NarrativeSequencePlayer / 其他系统 → 未来 InputManager listener）。
    /// <para>Sender: NarrativeSequencePlayer（sequence start/end push/pop）, 未来其他需要锁 input 的系统</para>
    /// <para>Listener: 未来 InputManager（per ADR-010 InputBlocker stack semantics — 实现 push/pop token 栈 +
    /// 顶层 token 控制 raw touch input swallow / pass-through）</para>
    /// <para>协议来源：ADR-016 §A line 91-95 token-based InputBlocker 集成；ADR-010 InputBlocker concept
    /// （ADR-010 真实施留 future Sprint 6/7）。</para>
    /// <para>Cascade depth budget: 0（terminal — listener 仅 input swallow，不派发）。</para>
    /// </summary>
    /// <remarks>
    /// **S5-05 dev-story Step 1 deficiency-flagged stub creation 2026-05-08**：
    /// 本文件为 ADR-029 V2.0 §V2-2 R2 deficiency-flagged path 的 framework extension（per
    /// narrative-event/story-001 §Engine Notes "⚠️ Required Framework Extension"）。
    /// 仅声明 contract；listener (InputManager push/pop stack 实现) 留 future ADR-010 Implementation Expand
    /// (Sprint 6/7)。S5-05 阶段 sender (NarrativeSequencePlayer) fire-and-forget 派发 — 无 listener 时无副作用。
    /// </remarks>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IInputBlockerEvent
    {
        /// <summary>
        /// 推入一个 input blocker token — 阻断 raw touch input（栈式）。
        /// <para>Sender: NarrativeSequencePlayer.StartSequence（token = "narrative_seq_&lt;id&gt;"）</para>
        /// <para>Listener 用途（future InputManager）: 维护 token 栈 + 顶层非空时 raw touch / mouse input 全 swallow。
        /// 多 source push 通过 token 区分各自的 lifecycle，避免 pop 错锁。</para>
        /// <para>Cascade depth: 0</para>
        /// </summary>
        /// <param name="token">push 标识 token（unique per source-call；e.g. "narrative_seq_&lt;sequenceId&gt;"）</param>
        void OnPushBlocker(string token);

        /// <summary>
        /// 弹出指定 token 的 input blocker — 解除该 token 对 input 的阻断。
        /// <para>Sender: NarrativeSequencePlayer.CompleteSequence（与 StartSequence push 同 token 配对）</para>
        /// <para>Listener 用途（future InputManager）: 从 token 栈中移除指定 token；若栈空则恢复 raw input pass-through；
        /// 若 token 不存在则 Log.Warning（orphan pop — 编程错误）。</para>
        /// <para>Cascade depth: 0</para>
        /// </summary>
        /// <param name="token">要 pop 的 token（必须与 push 的 token 完全一致）</param>
        void OnPopBlocker(string token);
    }
}
