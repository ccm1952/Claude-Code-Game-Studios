// 该文件由Cursor 自动生成
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// Shadow match algorithm 广播接口（ShadowMatchCalculator → PuzzleStateMachine / Hint / VFX）。
    /// <para>Sender: ShadowMatchCalculator（per ADR-012 — Sprint 5 时仅 stub contract；production publisher
    /// 留 future ADR-012 Implementation Expand）</para>
    /// <para>Listener: PuzzleStateMachine（subscribe via per-event AddEventListener&lt;int, float&gt;
    /// 在 Initialize 时；Cleanup 用 ADR-027 §5 null-out + null-check guard pattern）</para>
    /// <para>协议来源：ADR-027 §1 取代 ADR-006 const-int 协议。</para>
    /// <para>S5-03 dev-story Step 1 deficiency-flagged stub creation 2026-05-06：本文件为 ADR-029 V2.0 §V2-2
    /// R2 deficiency-flagged path 的 framework extension，仅声明 contract 不带 production publisher impl；
    /// MVP 阶段 PlayMode spike 内 fire mock event verify FSM transitions（per
    /// shadow-puzzle/story-001 §Engine Notes "⚠️ Required Framework Extension"）。</para>
    /// <para>Cascade depth budget: 1（PuzzleStateMachine 监听后可触发 IShadowPuzzleEvent.OnNearMatchEnter /
    /// OnPerfectMatch；后续 cascade ≤ 3，per ADR-027 §2 base guardrail）。</para>
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IShadowMatchEvent
    {
        /// <summary>
        /// 单 puzzle 的 matchScore 实测变化（每帧最多一次；shadow rendering result → match calculation 后派发）。
        /// <para>Sender: ShadowMatchCalculator（future production；MVP 阶段 PlayMode spike 内 fire mock）</para>
        /// <para>Listener: PuzzleStateMachine（per-puzzleId 过滤 + 触发 EvaluateTransitions）</para>
        /// <para>Frequency: 每帧 ≤ 1 次 per puzzle（per TR-puzzle-010 计算 budget &lt; 2ms/frame）</para>
        /// <para>Cascade depth: 1</para>
        /// </summary>
        /// <param name="puzzleId">puzzle 唯一 ID</param>
        /// <param name="newScore">最新 matchScore [0.0, 1.0] (post-temporal-smoothing per TR-puzzle-007)</param>
        void OnMatchScoreUpdated(int puzzleId, float newScore);
    }
}
