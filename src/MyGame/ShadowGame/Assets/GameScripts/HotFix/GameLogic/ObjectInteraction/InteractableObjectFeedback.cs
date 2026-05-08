// 该文件由Cursor 自动生成
using System;
using DG.Tweening;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Selected 视觉反馈 (Outline + Scale Bounce + Snap Settle Punch)。
    /// </summary>
    /// <remarks>
    /// <para><b>Sprint 4 S4-06 (Sprint 2→3→4 第 2 次 carryover)</b>：Visual Polish 起步 story；TR-objint-002.</para>
    /// <para><b>架构定位</b>：widget-internal concern (per ADR-011 widget 模式)。订阅
    /// <see cref="InteractableObjectFsm.StateChanged"/> C# event 而**不**走 GameEvent /
    /// IInteractionEvent — 避免污染全局事件总线 + 单实例隔离。</para>
    /// <para><b>Lifecycle</b>：<see cref="OnEnable"/> 订阅；<see cref="OnDisable"/> 取消订阅；
    /// 沿 ADR-013 v3 InteractableObject Initialize/Shutdown 显式生命周期 pattern。</para>
    /// <para><b>Collider 独立性</b>：Outline + Scale punch 仅作用于 <see cref="_visualMesh"/> 子节点；
    /// Collider 必须在父 GameObject (InteractableObject) 或独立子节点，不受 visualMesh.localScale 影响。</para>
    /// <para><b>Tween params (per Sprint 1 Visual story 教训：抽参数层做 EditMode 单测)</b>:</para>
    /// <list type="bullet">
    /// <item>Idle → Selected: <c>DOPunchScale((0.15, 0.15, 0), 0.2s, vibrato=5, elasticity=0.5)</c></item>
    /// <item>Snapping → Idle (settle): <c>DOPunchScale((0.05, 0.05, 0), 0.1s, vibrato=3, elasticity=0.5)</c> — 比选中弹动弱</item>
    /// <item>Selected → Idle (cancel) / Locked: outline off + scale = (1,1,1) reset；no animation</item>
    /// </list>
    /// </remarks>
    [RequireComponent(typeof(Transform))]
    public sealed class InteractableObjectFeedback : MonoBehaviour
    {
        // Tween parameters — 抽参数层 (per Sprint 1 Visual story 教训)；可被 EditMode unit test 验
        public const float SelectPunchMagnitude = 0.15f;
        public const float SelectPunchDuration = 0.2f;
        public const int SelectPunchVibrato = 5;
        public const float SelectPunchElasticity = 0.5f;

        public const float SnapSettlePunchMagnitude = 0.05f;
        public const float SnapSettlePunchDuration = 0.1f;
        public const int SnapSettlePunchVibrato = 3;
        public const float SnapSettlePunchElasticity = 0.5f;

        [SerializeField] private InteractableObject _target;          // 同 GameObject 或父节点
        [SerializeField] private Renderer _outlineRenderer;           // 单独 outline material 渲染器
        [SerializeField] private Transform _visualMesh;               // 受 scale punch 影响的子节点（与 collider 分离）

        // 显式 subscription state — sprint 3 retro lesson: track subscription with bool flag
        // (即使 OnDisable 应该自动 unsubscribe，仍 explicit guard 防 double subscription)
        private bool _isSubscribed;

        /// <summary>暴露给测试 — 当前订阅状态。</summary>
        public bool IsSubscribedForTest => _isSubscribed;

        /// <summary>
        /// 显式 Initialize 入口 (沿 ADR-013 v3 InteractableObject Initialize/Shutdown 教训)。
        /// EditMode test 中 PlayerLoop 不驱动 OnEnable，必须 explicit 调用此 method。
        /// </summary>
        public void Initialize()
        {
            SubscribeIfNeeded();
        }

        /// <summary>
        /// 显式 Shutdown 入口 — 与 Initialize 配对；OnDisable 也调此 path 以确保唯一 unsubscribe entry。
        /// </summary>
        public void Shutdown()
        {
            UnsubscribeIfNeeded();
            // 安全 reset visual state
            if (_visualMesh != null)
            {
                _visualMesh.DOKill();
                _visualMesh.localScale = Vector3.one;
            }
            if (_outlineRenderer != null)
            {
                _outlineRenderer.enabled = false;
            }
        }

        private void OnEnable()
        {
            SubscribeIfNeeded();
        }

        private void OnDisable()
        {
            UnsubscribeIfNeeded();
        }

        private void SubscribeIfNeeded()
        {
            if (_isSubscribed) return;  // 防 double subscription
            if (_target == null || _target.Fsm == null) return;

            _target.Fsm.StateChanged += HandleStateChanged;
            _isSubscribed = true;
        }

        private void UnsubscribeIfNeeded()
        {
            if (!_isSubscribed) return;
            if (_target == null || _target.Fsm == null) { _isSubscribed = false; return; }

            _target.Fsm.StateChanged -= HandleStateChanged;
            _isSubscribed = false;
        }

        private void HandleStateChanged(InteractableObjectState prev, InteractableObjectState next)
        {
            switch (next)
            {
                case InteractableObjectState.Selected:
                    EnableOutline(true);
                    PlaySelectPunch();
                    break;

                case InteractableObjectState.Idle:
                    EnableOutline(false);
                    if (prev == InteractableObjectState.Snapping)
                    {
                        PlaySnapSettlePunch();
                    }
                    else
                    {
                        ResetScale();
                    }
                    break;

                case InteractableObjectState.Locked:
                    EnableOutline(false);
                    KillTweenAndResetScale();
                    break;

                case InteractableObjectState.Dragging:
                case InteractableObjectState.Snapping:
                    // 保持 outline 开（Selected 状态进入时已开），无新动画
                    break;
            }
        }

        private void EnableOutline(bool enable)
        {
            if (_outlineRenderer != null)
            {
                _outlineRenderer.enabled = enable;
            }
        }

        private void PlaySelectPunch()
        {
            if (_visualMesh == null) return;
            _visualMesh.DOKill();
            _visualMesh.DOPunchScale(
                punch: new Vector3(SelectPunchMagnitude, SelectPunchMagnitude, 0f),
                duration: SelectPunchDuration,
                vibrato: SelectPunchVibrato,
                elasticity: SelectPunchElasticity);
        }

        private void PlaySnapSettlePunch()
        {
            if (_visualMesh == null) return;
            _visualMesh.DOKill();
            _visualMesh.DOPunchScale(
                punch: new Vector3(SnapSettlePunchMagnitude, SnapSettlePunchMagnitude, 0f),
                duration: SnapSettlePunchDuration,
                vibrato: SnapSettlePunchVibrato,
                elasticity: SnapSettlePunchElasticity);
        }

        private void ResetScale()
        {
            if (_visualMesh == null) return;
            _visualMesh.DOKill();
            _visualMesh.localScale = Vector3.one;
        }

        private void KillTweenAndResetScale()
        {
            if (_visualMesh == null) return;
            _visualMesh.DOKill();
            _visualMesh.localScale = Vector3.one;
        }
    }
}
