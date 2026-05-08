// 该文件由Cursor 自动生成
using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 多 sender 安全的可交互物体锁管理器（S2-12 / SP-006 / ADR-013 §"Risks"）。
    /// </summary>
    /// <remarks>
    /// <para><b>实施模式</b>：纯 C# POCO（不继承 MonoBehaviour，不实施 <c>IInteractionEvent</c> /
    /// <c>ISceneEvent</c> 接口本身）—— 与 <see cref="SceneManager"/>（S2-05）/ <see cref="InteractableObjectFsm"/>
    /// （S2-08）同模式，可在 EditMode 单测中直接 <c>new</c> 出来 + 调 <see cref="Init"/> / <see cref="Dispose"/>。</para>
    ///
    /// <para><b>设计修订（X1）</b>：本类**不**实施 <c>IInteractionEvent</c> / <c>ISceneEvent</c> 接口（尽管
    /// story-006 §Implementation Notes 旧版如此写）。原因：TEngine 的 <c>GameEvent.AddEventListener</c> 仅支持
    /// per-event 订阅签名 <c>(int eventId, Action&lt;TArg&gt; handler)</c>，**不**支持"整接口订阅"模式
    /// <c>AddEventListener&lt;TInterface&gt;(this)</c>（<c>EventMgr.RegWrapInterface&lt;T&gt;</c> 是 sender 端的代理注册，**不**是 listener 端 API）。
    /// 详见 <c>/.claude/memory/problem_2026-04-29_story-impl-notes-vs-framework-drift.md</c>。</para>
    ///
    /// <para><b>职责</b>（参 ADR-013 §"State Transition Rules" 规则 7/8 + ADR-027 §"OnInteractionLockChanged"）：
    /// <list type="number">
    /// <item><description>HashSet&lt;string&gt; token 锁集合：<c>PushLock</c> 添 token / <c>PopLock</c> 移 token；
    /// <see cref="IsLocked"/> = (set.Count > 0)（SP-006 关键设计 — 防 LIFO 错配）</description></item>
    /// <item><description>派发 <c>IInteractionEvent.OnInteractionLockChanged(bool)</c> **仅在** set "空↔非空" transition 时；
    /// 中间增量（已锁定时再 push / 仍有其他 token 时 pop）幂等无派发</description></item>
    /// <item><description>订阅 3 个事件作为命令 / 生命周期入口：
    /// <list type="bullet">
    /// <item><c>IInteractionEvent.OnRequestPuzzleLockAll(string)</c> → <see cref="PushLock"/></item>
    /// <item><c>IInteractionEvent.OnRequestPuzzleUnlock(string)</c> → <see cref="PopLock"/></item>
    /// <item><c>ISceneEvent.OnSceneUnloadBegin(int)</c> → 强清 token 集合（防泄漏）</item>
    /// </list></description></item>
    /// <item><description><c>PopLock</c> 未知 token：<c>Log.Warning</c> 列举 <see cref="InteractionLockerId.All"/>，no-op，不抛</description></item>
    /// </list></para>
    ///
    /// <para><b>使用约定</b>：由 <c>InteractionCoordinator</c>（S2-13）在自身 <c>Init</c> 中
    /// <c>new InteractionLockManager()</c> 并调 <see cref="Init"/>，在 <c>Dispose</c> 中调 <see cref="Dispose"/>。
    /// <see cref="InteractableObject"/>（S2-08+）通过自身 <c>IInteractionEvent.OnInteractionLockChanged</c>
    /// listener 接收锁状态变化并调 <c>fsm.OnLockChanged(isLocked)</c>。</para>
    ///
    /// <para><b>禁止</b>：使用 <c>Stack&lt;string&gt;</c> 替代 HashSet（LIFO 顺序在多 sender 并发下不可靠 — SP-006）。</para>
    /// </remarks>
    public sealed class InteractionLockManager
    {
        private readonly HashSet<string> _activeLocks = new HashSet<string>();
        private bool _listenersRegistered;

        /// <summary>当前是否锁定（<c>_activeLocks.Count &gt; 0</c>）。</summary>
        public bool IsLocked => _activeLocks.Count > 0;

        /// <summary>当前锁定 token 数量（test-only diagnostic；生产代码请用 <see cref="IsLocked"/>）。</summary>
        public int ActiveLockCount => _activeLocks.Count;

        // ------------------------------------------------------------------ Lifecycle

        /// <summary>
        /// 注册 3 个事件 listener。幂等（重复调用 no-op）。
        /// </summary>
        public void Init()
        {
            if (_listenersRegistered) return;

            GameEvent.AddEventListener<string>(
                IInteractionEvent_Event.OnRequestPuzzleLockAll, OnRequestPuzzleLockAll);
            GameEvent.AddEventListener<string>(
                IInteractionEvent_Event.OnRequestPuzzleUnlock, OnRequestPuzzleUnlock);
            GameEvent.AddEventListener<int>(
                ISceneEvent_Event.OnSceneUnloadBegin, OnSceneUnloadBegin);

            _listenersRegistered = true;
        }

        /// <summary>
        /// 注销所有 listener + 清 token 集合（不派发 OnInteractionLockChanged，因 manager 即将销毁）。幂等。
        /// </summary>
        public void Dispose()
        {
            if (_listenersRegistered)
            {
                GameEvent.RemoveEventListener<string>(
                    IInteractionEvent_Event.OnRequestPuzzleLockAll, OnRequestPuzzleLockAll);
                GameEvent.RemoveEventListener<string>(
                    IInteractionEvent_Event.OnRequestPuzzleUnlock, OnRequestPuzzleUnlock);
                GameEvent.RemoveEventListener<int>(
                    ISceneEvent_Event.OnSceneUnloadBegin, OnSceneUnloadBegin);
                _listenersRegistered = false;
            }

            _activeLocks.Clear();
        }

        // ------------------------------------------------------------------ Public API（供 Coordinator + 单测 bypass listener）

        /// <summary>
        /// 添加 lock token。如 set 由"空 → 非空"，派发 <c>OnInteractionLockChanged(true)</c>；
        /// 否则幂等无派发（含重复 push 同 token 的去重路径）。
        /// </summary>
        public void PushLock(string lockerId)
        {
            if (string.IsNullOrEmpty(lockerId))
            {
                Log.Warning("[InteractionLock] PushLock 收到空 lockerId — 已忽略");
                return;
            }

            bool wasEmpty = _activeLocks.Count == 0;
            bool added = _activeLocks.Add(lockerId);
            if (added && wasEmpty)
            {
                GameEvent.Get<IInteractionEvent>().OnInteractionLockChanged(true);
            }
        }

        /// <summary>
        /// 移除 lock token。
        /// <list type="bullet">
        /// <item>未知 token（不在 set 中）→ <c>Log.Warning</c> 列出合法 ID，no-op，**不**派发</item>
        /// <item>移除后 set 由"非空 → 空"→ 派发 <c>OnInteractionLockChanged(false)</c></item>
        /// <item>移除后仍有其他 token → 幂等无派发</item>
        /// </list>
        /// </summary>
        public void PopLock(string lockerId)
        {
            if (!_activeLocks.Remove(lockerId))
            {
                Log.Warning(
                    $"[InteractionLock] Unknown locker: '{lockerId}'. Valid IDs: {string.Join(", ", InteractionLockerId.All)}");
                return;
            }

            if (_activeLocks.Count == 0)
            {
                GameEvent.Get<IInteractionEvent>().OnInteractionLockChanged(false);
            }
        }

        // ------------------------------------------------------------------ Listeners (per-event 模式 — 见 §Engine Notes 修订说明)

        /// <summary>
        /// <c>IInteractionEvent.OnRequestPuzzleLockAll</c> listener —— 委托到 <see cref="PushLock"/>。
        /// </summary>
        private void OnRequestPuzzleLockAll(string lockerId) => PushLock(lockerId);

        /// <summary>
        /// <c>IInteractionEvent.OnRequestPuzzleUnlock</c> listener —— 委托到 <see cref="PopLock"/>。
        /// </summary>
        private void OnRequestPuzzleUnlock(string lockerId) => PopLock(lockerId);

        /// <summary>
        /// <c>ISceneEvent.OnSceneUnloadBegin</c> listener —— 强清泄漏的 token；如清掉非空集合则派
        /// <c>OnInteractionLockChanged(false)</c> 一次（Locked → Idle 转换）。
        /// </summary>
        /// <param name="chapterId">即将卸载的 chapter（仅用于诊断日志）</param>
        private void OnSceneUnloadBegin(int chapterId)
        {
            if (_activeLocks.Count == 0) return;   // 无锁 → 无 warning，无派发

            int leakedCount = _activeLocks.Count;
            Log.Warning(
                $"[InteractionLock] Force-clearing {leakedCount} leaked lock(s) on scene unload (chapter={chapterId}). Leaked: [{string.Join(", ", _activeLocks)}]");
            _activeLocks.Clear();
            GameEvent.Get<IInteractionEvent>().OnInteractionLockChanged(false);
        }
    }
}
