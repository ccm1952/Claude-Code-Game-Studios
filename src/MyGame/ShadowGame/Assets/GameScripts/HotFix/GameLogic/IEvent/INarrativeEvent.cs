// 该文件由Cursor 自动生成
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// Narrative Sequence Engine 广播接口（NarrativeSequencePlayer → ChapterStateManager / VFX / Audio）。
    /// <para>Sender: NarrativeSequencePlayer（per ADR-016 §B Production code paths；S5-05 dev-story 创建）</para>
    /// <para>Listener: ChapterStateManager（OnSequenceComplete 触发 chapter 进度推进）, VFX / SFX / UI overlay
    /// 各自 atomic effect。</para>
    /// <para>协议来源：ADR-027 §1 取代 ADR-006 const-int 协议；ADR-016 §A 4 method 设计。</para>
    /// <para>Cascade depth budget: 3（per ADR-027 §2 guardrail）：trigger → sequence start → InputBlocker push
    /// → atomic effects fire (各自 cascade ≤ 1)。</para>
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface INarrativeEvent
    {
        /// <summary>
        /// 外部请求播放 narrative sequence — 通过 trigger source ID + sequence type 精确请求。
        /// <para>Sender: 任何想触发 narrative sequence 的系统（PuzzleStateMachine via OnPerfectMatch cascade /
        /// ChapterStateManager via OnChapterComplete cascade / 其他系统 explicit dispatch）</para>
        /// <para>Listener: NarrativeSequencePlayer（resolve config via NarrativeSequenceConfigFromLuban + 启动 sequence
        /// 或 enqueue）</para>
        /// <para>Cascade depth: 1（listener 触发 OnSequenceStart）</para>
        /// </summary>
        /// <param name="triggerSourceId">触发源 ID（puzzleId / chapterId / 其他系统 ID）</param>
        /// <param name="sequenceType">sequence 类型（决定 player 内 branch 分支处理）</param>
        void OnRequestSequence(int triggerSourceId, NarrativeSequenceType sequenceType);

        /// <summary>
        /// Sequence 开始播放（NarrativeSequencePlayer 内部触发）。
        /// <para>Sender: NarrativeSequencePlayer.StartSequence（仅一次 per sequence id）</para>
        /// <para>Listener 用途: VFX / Audio system 进入 narrative mode；ChapterStateManager 标记 narrative 状态。
        /// Cascade: NarrativeSequencePlayer 同时派发 IInputBlockerEvent.OnPushBlocker(token) 锁 raw input。</para>
        /// <para>Cascade depth: 1（cross-system InputBlocker push）</para>
        /// </summary>
        /// <param name="sequenceId">sequence ID（NarrativeSequenceConfig.Id）</param>
        /// <param name="sequenceType">sequence 类型</param>
        void OnSequenceStart(int sequenceId, NarrativeSequenceType sequenceType);

        /// <summary>
        /// Sequence 全部 atomic effects 完成播放（含 totalDuration 走完）。
        /// <para>Sender: NarrativeSequencePlayer.CompleteSequence（仅一次 per sequence id）</para>
        /// <para>Listener 用途: ChapterStateManager 推进 chapter 进度；VFX / Audio system 退出 narrative mode。
        /// Cascade: NarrativeSequencePlayer 同时派发 IInputBlockerEvent.OnPopBlocker(token) 解锁 input。</para>
        /// <para>Cascade depth: 2（cross-system InputBlocker pop + ChapterStateManager 可触发 OnChapterComplete cascade，per ADR-027 §2 guardrail 合计深度 ≤ 3）</para>
        /// </summary>
        /// <param name="sequenceId">sequence ID</param>
        /// <param name="sequenceType">sequence 类型</param>
        void OnSequenceComplete(int sequenceId, NarrativeSequenceType sequenceType);

        /// <summary>
        /// Sequence 失败 — queue overflow / config not found / atomic effect critical failure。
        /// <para>Sender: NarrativeSequencePlayer.HandleRequestSequence (queue full) /
        /// NarrativeSequencePlayer effect dispatch (effect type 未注册).</para>
        /// <para>Listener 用途: ChapterStateManager 决策是否补救（重试 / skip / 阻塞 chapter 进度）；
        /// telemetry / dev log。</para>
        /// <para>Cascade depth: 0（terminal — listener 仅 log/decision，不再派发）</para>
        /// </summary>
        /// <param name="sequenceId">sequence ID（如 unknown 用 -1）</param>
        /// <param name="reason">失败原因 string label（"queue_overflow" / "config_not_found" /
        /// "effect_type_not_implemented" / "resource_load_failed"）</param>
        void OnSequenceFailed(int sequenceId, string reason);
    }

    /// <summary>
    /// Narrative sequence 类型（per ADR-016 §A 3 sequence types 设计）。
    /// </summary>
    public enum NarrativeSequenceType
    {
        /// <summary>记忆回放 — PerfectMatch 触发的标准回忆 sequence（5-8s）</summary>
        MemoryReplay = 0,

        /// <summary>章节切换 — ChapterComplete 触发，含 Timeline playable（8-15s）</summary>
        ChapterTransition = 1,

        /// <summary>缺席 puzzle — Chapter 5 AbsenceAccepted 触发（5-8s, cool color, no ObjectSnap）</summary>
        AbsencePuzzle = 2,
    }
}
