// 该文件由Cursor 自动生成
using System;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 可交互物体的 5 状态枚举（ADR-013 §"Key Interfaces"，**Accepted** 2026-04-29）。
    /// <para>Rotating 不是独立状态：rotation 在 <see cref="Selected"/> 内作为子模式实施
    /// （Story 003）；Unlocked 不是独立状态：它是 <see cref="Locked"/>→<see cref="Idle"/> 的转换名。</para>
    /// </summary>
    public enum InteractableObjectState
    {
        Idle,
        Selected,
        Dragging,
        Snapping,
        Locked,
    }

    /// <summary>
    /// 单个可交互物体的状态机骨架（S2-08 Story 001）。
    /// </summary>
    /// <remarks>
    /// <para><b>实施模式</b>：纯 C# 类（不继承 <c>MonoBehaviour</c>，不依赖 <c>GameModule.Fsm</c>）—— 与
    /// <c>SceneManager</c>（S2-05）/ <c>SingleFingerFSM</c> / <c>DualFingerFSM</c> 同模式，可在 EditMode 单元测试中直接 <c>new</c> 出来驱动，0 GameObject 依赖。</para>
    ///
    /// <para><b>职责</b>（参 ADR-013 §"State Transition Rules"，规则 1..8）：
    /// <list type="number">
    /// <item><description>Idle + <see cref="OnTapHit"/> → Selected；派 <see cref="IInteractionEvent.OnObjectSelected"/></description></item>
    /// <item><description>Selected + <see cref="OnDeselect"/> → Idle；派 <see cref="IInteractionEvent.OnObjectDeselected"/></description></item>
    /// <item><description>Selected + <see cref="OnDragBegan"/> → Dragging（无对外 sender）</description></item>
    /// <item><description>Dragging + <see cref="OnDragEnded"/> → Snapping（无对外 sender；snap 动画由 Story 004 启动）</description></item>
    /// <item><description>Snapping + <see cref="OnSnapCompleted"/> → Idle（OnObjectTransformChanged sender 由 Story 004 接管）</description></item>
    /// <item><description>Snapping + <see cref="OnTapHit"/> → Selected（中断 snap；DOKill 由 Story 004 处理）；派 OnObjectSelected</description></item>
    /// <item><description>{Idle/Selected/Dragging/Snapping} + <see cref="OnLockChanged"/>(true) → Locked；如曾 Selected 派 OnObjectDeselected</description></item>
    /// <item><description>Locked + <see cref="OnLockChanged"/>(false) → Idle（无对外 sender）</description></item>
    /// </list></para>
    ///
    /// <para><b>非法转换处理</b>：所有非上述 8 条之一的输入 → 静默丢弃 + <see cref="Log.Warning"/>，不抛异常，不改状态。</para>
    ///
    /// <para><b>Sender 协议</b>：所有对外通知通过 <c>GameEvent.Get&lt;IInteractionEvent&gt;().OnXxx(...)</c>（ADR-027）；
    /// epic 内 widget-level 反馈通过 <see cref="StateChanged"/> C# event（ADR-011 widget 模式，参 Story 005）。
    /// 禁止使用 <c>EventId.Evt_*</c> 常量（ADR-006 全废）。</para>
    ///
    /// <para><b>使用约定</b>：由 <see cref="InteractableObject"/> MonoBehaviour 在 <c>OnEnable</c> 中
    /// <c>new InteractableObjectFsm(objectId)</c>；触发方法由 <c>InteractionCoordinator</c>（Story 007）+
    /// <see cref="IInteractionEvent.OnInteractionLockChanged"/> listener 调用驱动。</para>
    /// </remarks>
    public sealed class InteractableObjectFsm
    {
        // ------------------------------------------------------------------
        // Identity & State
        // ------------------------------------------------------------------

        /// <summary>物体 ID（Luban TbInteractable 主键；构造时一次性确定）。</summary>
        public int ObjectId { get; }

        /// <summary>当前状态（构造时初始为 <see cref="InteractableObjectState.Idle"/>）。</summary>
        public InteractableObjectState CurrentState { get; private set; } = InteractableObjectState.Idle;

        /// <summary>
        /// 状态变化 C# event（widget-level，不走 GameEvent —— ADR-011 widget 模式）。
        /// <para>签名：<c>(prevState, nextState)</c></para>
        /// <para>Listener: <c>InteractableObjectFeedback</c>（Story 005，outline / scale punch）+ <c>InteractableObject</c>（Story 004 Snapping→其他 时 DOKill）。</para>
        /// </summary>
        public event Action<InteractableObjectState, InteractableObjectState> StateChanged;

        public InteractableObjectFsm(int objectId)
        {
            ObjectId = objectId;
        }

        // ------------------------------------------------------------------
        // Triggers (8 transition rules — see <remarks>)
        // ------------------------------------------------------------------

        /// <summary>
        /// 规则 1 (Idle→Selected) + 规则 6 (Snapping→Selected)。
        /// <para>其他状态调用 → 静默 + warning（规则 7 的 Locked 命中此处）。</para>
        /// </summary>
        public void OnTapHit()
        {
            if (CurrentState == InteractableObjectState.Idle ||
                CurrentState == InteractableObjectState.Snapping)
            {
                TransitionTo(InteractableObjectState.Selected);
                GameEvent.Get<IInteractionEvent>().OnObjectSelected(ObjectId);
                return;
            }
            Log.Warning($"[InteractableObjectFsm#{ObjectId}] OnTapHit ignored in {CurrentState}.");
        }

        /// <summary>
        /// 规则 2 (Selected→Idle)。其他状态调用 → 静默 + warning。
        /// </summary>
        public void OnDeselect()
        {
            if (CurrentState == InteractableObjectState.Selected)
            {
                TransitionTo(InteractableObjectState.Idle);
                GameEvent.Get<IInteractionEvent>().OnObjectDeselected(ObjectId);
                return;
            }
            Log.Warning($"[InteractableObjectFsm#{ObjectId}] OnDeselect ignored in {CurrentState}.");
        }

        /// <summary>
        /// 规则 3 (Selected→Dragging)。其他状态调用 → 静默 + warning。
        /// </summary>
        public void OnDragBegan()
        {
            if (CurrentState == InteractableObjectState.Selected)
            {
                TransitionTo(InteractableObjectState.Dragging);
                return;
            }
            Log.Warning($"[InteractableObjectFsm#{ObjectId}] OnDragBegan ignored in {CurrentState}.");
        }

        /// <summary>
        /// 规则 4 (Dragging→Snapping)。其他状态调用 → 静默 + warning。
        /// </summary>
        public void OnDragEnded()
        {
            if (CurrentState == InteractableObjectState.Dragging)
            {
                TransitionTo(InteractableObjectState.Snapping);
                return;
            }
            Log.Warning($"[InteractableObjectFsm#{ObjectId}] OnDragEnded ignored in {CurrentState}.");
        }

        /// <summary>
        /// 规则 5 (Snapping→Idle)。OnObjectTransformChanged sender 由 Story 004 在调用本方法**前后**派发——
        /// 本 story 仅保证 transition 正确。其他状态调用 → 静默 + warning。
        /// </summary>
        public void OnSnapCompleted()
        {
            if (CurrentState == InteractableObjectState.Snapping)
            {
                TransitionTo(InteractableObjectState.Idle);
                return;
            }
            Log.Warning($"[InteractableObjectFsm#{ObjectId}] OnSnapCompleted ignored in {CurrentState}.");
        }

        /// <summary>
        /// 规则 7 + 规则 8（lock/unlock）。
        /// <list type="bullet">
        /// <item><description><c>isLocked == true</c> 且当前非 Locked → Locked；如 from==Selected 派 <see cref="IInteractionEvent.OnObjectDeselected"/>（**不**派 OnObjectTransformChanged，落点未定）</description></item>
        /// <item><description><c>isLocked == false</c> 且当前 == Locked → Idle（无对外 sender）</description></item>
        /// <item><description>其他组合（已 Locked 再 lock / 非 Locked 再 unlock）→ 幂等 no-op，不 warning（规则化的稳定状态查询）</description></item>
        /// </list>
        /// </summary>
        public void OnLockChanged(bool isLocked)
        {
            if (isLocked)
            {
                if (CurrentState == InteractableObjectState.Locked) return;

                bool wasSelected = CurrentState == InteractableObjectState.Selected;
                TransitionTo(InteractableObjectState.Locked);
                if (wasSelected)
                {
                    GameEvent.Get<IInteractionEvent>().OnObjectDeselected(ObjectId);
                }
                return;
            }

            if (CurrentState == InteractableObjectState.Locked)
            {
                TransitionTo(InteractableObjectState.Idle);
            }
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private void TransitionTo(InteractableObjectState next)
        {
            var prev = CurrentState;
            if (prev == next) return;
            CurrentState = next;
            Log.Info($"[InteractableObjectFsm#{ObjectId}] {prev} → {next}");
            StateChanged?.Invoke(prev, next);
        }
    }
}
