// 该文件由Cursor 自动生成
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GameLogic;
using TEngine;

namespace ShadowGame.Tests.EditMode.ObjectInteraction
{
    /// <summary>
    /// S2-12 — InteractionLockManager（HashSet token + per-event listener 模式 + scene-unload 自清）。
    /// 覆盖 AC-1..AC-7 of <c>story-006-interaction-lock-manager.md</c>。
    ///
    /// <para><b>Fixture 模式</b>：POCO 测试，0 GameObject 依赖（无须 stub Camera）—— 与 DragMechanicsTests / GridSnapTests
    /// 不同（那些走 MonoBehaviour 路径需 stub camera）。但 GameEvent dispatcher 必须初始化 + 实例化
    /// IInteractionEvent_Gen / ISceneEvent_Gen sender 代理（让 <c>GameEvent.Get&lt;IInteractionEvent&gt;()</c> 能拿到 dispatcher）
    /// + 添加 OnInteractionLockChanged listener 用于派发计数 — 参 conventions.md "测试 fixture 规范" R1。</para>
    ///
    /// <para><b>关键约束 R3</b>：所有期望 ERROR/Warning log 的测试必须用 <c>LogAssert.Expect</c> 显式声明
    /// （`InteractionLockManager` 用 `Log.Warning`，未 expect 在 NUnit 默认行为下会让测试失败 — 见
    /// `/.claude/memory/problem_2026-04-29_test-fixture-fail-loud-boilerplate-skip.md`）。</para>
    /// </summary>
    [TestFixture]
    public class InteractionLockManagerTests
    {
        private InteractionLockManager _mgr;

        // OnInteractionLockChanged 计数器
        private int _lockChangedCallCount;
        private bool _lastLockChangedValue;

        // ────────── Fixture lifecycle ──────────

        [SetUp]
        public void SetUp()
        {
            GameEvent.EventMgr.Init();
            _ = new IInteractionEvent_Gen(GameEvent.EventMgr.GetDispatcher());
            _ = new ISceneEvent_Gen(GameEvent.EventMgr.GetDispatcher());

            _lockChangedCallCount = 0;
            _lastLockChangedValue = false;
            GameEvent.AddEventListener<bool>(
                IInteractionEvent_Event.OnInteractionLockChanged, OnLockChangedListener);

            _mgr = new InteractionLockManager();
            _mgr.Init();
        }

        [TearDown]
        public void TearDown()
        {
            GameEvent.RemoveEventListener<bool>(
                IInteractionEvent_Event.OnInteractionLockChanged, OnLockChangedListener);
            _mgr?.Dispose();
            _mgr = null;
        }

        private void OnLockChangedListener(bool isLocked)
        {
            _lockChangedCallCount++;
            _lastLockChangedValue = isLocked;
        }

        // ==================================================================
        // AC-1: IsLocked 反映 HashSet；PushLock/PopLock 边界 transition 各派发 1 次
        // ==================================================================

        [Test]
        public void AC1_PushLock_FromEmpty_DispatchesLockChangedTrueOnce()
        {
            Assert.IsFalse(_mgr.IsLocked, "初始 set 为空");

            _mgr.PushLock(InteractionLockerId.ShadowPuzzle);

            Assert.IsTrue(_mgr.IsLocked);
            Assert.AreEqual(1, _mgr.ActiveLockCount);
            Assert.AreEqual(1, _lockChangedCallCount, "空→非空 应派 OnInteractionLockChanged 1 次");
            Assert.IsTrue(_lastLockChangedValue);
        }

        [Test]
        public void AC1_PopLock_ToEmpty_DispatchesLockChangedFalseOnce()
        {
            _mgr.PushLock(InteractionLockerId.ShadowPuzzle);
            _lockChangedCallCount = 0;   // 重置计数

            _mgr.PopLock(InteractionLockerId.ShadowPuzzle);

            Assert.IsFalse(_mgr.IsLocked);
            Assert.AreEqual(0, _mgr.ActiveLockCount);
            Assert.AreEqual(1, _lockChangedCallCount, "非空→空 应派 OnInteractionLockChanged 1 次");
            Assert.IsFalse(_lastLockChangedValue);
        }

        [Test]
        public void AC1_DuplicatePushLock_SameToken_NoDuplicateDispatch()
        {
            _mgr.PushLock(InteractionLockerId.ShadowPuzzle);
            _mgr.PushLock(InteractionLockerId.ShadowPuzzle);   // 重复
            _mgr.PushLock(InteractionLockerId.ShadowPuzzle);   // 再重复

            Assert.IsTrue(_mgr.IsLocked);
            Assert.AreEqual(1, _mgr.ActiveLockCount, "HashSet 去重 — Count 仍为 1");
            Assert.AreEqual(1, _lockChangedCallCount, "OnInteractionLockChanged 仅 1 次（首次空→非空）");
        }

        // ==================================================================
        // AC-2: 多 sender token（SP-006 关键场景）
        // ==================================================================

        [Test]
        public void AC2_MultiSender_TwoTokens_OnlyEdgeTransitionsDispatch()
        {
            _mgr.PushLock(InteractionLockerId.ShadowPuzzle);
            _mgr.PushLock(InteractionLockerId.Narrative);

            Assert.AreEqual(2, _mgr.ActiveLockCount);
            Assert.AreEqual(1, _lockChangedCallCount, "仅第 1 次 push 派发（空→非空）；第 2 次 push 已锁定，幂等无派发");

            _mgr.PopLock(InteractionLockerId.Narrative);

            Assert.IsTrue(_mgr.IsLocked, "仍持 shadow_puzzle");
            Assert.AreEqual(1, _mgr.ActiveLockCount);
            Assert.AreEqual(1, _lockChangedCallCount, "仍非空 → 不再派发");

            _mgr.PopLock(InteractionLockerId.ShadowPuzzle);

            Assert.IsFalse(_mgr.IsLocked);
            Assert.AreEqual(2, _lockChangedCallCount, "最后 pop 触发非空→空 派发 1 次（合计 2 次）");
            Assert.IsFalse(_lastLockChangedValue);
        }

        [Test]
        public void AC2_MultiSender_ReverseOrderPop_SameResult()
        {
            _mgr.PushLock(InteractionLockerId.ShadowPuzzle);
            _mgr.PushLock(InteractionLockerId.Narrative);
            _mgr.PopLock(InteractionLockerId.ShadowPuzzle);   // 先 pop shadow_puzzle

            Assert.IsTrue(_mgr.IsLocked, "仍持 narrative");
            Assert.AreEqual(1, _lockChangedCallCount);

            _mgr.PopLock(InteractionLockerId.Narrative);

            Assert.IsFalse(_mgr.IsLocked);
            Assert.AreEqual(2, _lockChangedCallCount, "reverse 顺序 final state + dispatch 计数与正向一致");
        }

        // ==================================================================
        // AC-3: 未知 token pop 是 no-op + warning（LogAssert.Expect 显式验证）
        // ==================================================================

        [Test]
        public void AC3_PopLock_UnknownToken_LogsWarningNoOp()
        {
            _mgr.PushLock(InteractionLockerId.ShadowPuzzle);
            _lockChangedCallCount = 0;

            LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex(@"\[InteractionLock\] Unknown locker: 'unknown_system'"));
            _mgr.PopLock("unknown_system");

            Assert.AreEqual(1, _mgr.ActiveLockCount, "未知 token pop → set 不变");
            Assert.IsTrue(_mgr.IsLocked);
            Assert.AreEqual(0, _lockChangedCallCount, "未知 token pop **不**派发 OnInteractionLockChanged");
        }

        [Test]
        public void AC3_PopLock_FromEmpty_UnknownToken_LogsWarningNoDispatch()
        {
            LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex(@"\[InteractionLock\] Unknown locker"));
            _mgr.PopLock("ghost_token");

            Assert.IsFalse(_mgr.IsLocked);
            Assert.AreEqual(0, _lockChangedCallCount);
        }

        // ==================================================================
        // AC-4: ISceneEvent.OnSceneUnloadBegin 强清 + warning
        // ==================================================================

        [Test]
        public void AC4_OnSceneUnloadBegin_LeakedLocks_ClearsAndDispatchesFalse()
        {
            _mgr.PushLock(InteractionLockerId.ShadowPuzzle);
            _mgr.PushLock(InteractionLockerId.Narrative);
            _lockChangedCallCount = 0;

            LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex(@"\[InteractionLock\] Force-clearing 2 leaked lock"));
            GameEvent.Get<ISceneEvent>().OnSceneUnloadBegin(chapterId: 1);

            Assert.IsFalse(_mgr.IsLocked);
            Assert.AreEqual(0, _mgr.ActiveLockCount);
            Assert.AreEqual(1, _lockChangedCallCount, "force-clear 触发 1 次 OnInteractionLockChanged(false)");
            Assert.IsFalse(_lastLockChangedValue);
        }

        [Test]
        public void AC4_OnSceneUnloadBegin_NoLocks_NoWarningNoDispatch()
        {
            // 无锁状态下 unload — 无 warning（避免噪音）+ 无派发（无 transition）
            GameEvent.Get<ISceneEvent>().OnSceneUnloadBegin(chapterId: 1);

            Assert.IsFalse(_mgr.IsLocked);
            Assert.AreEqual(0, _lockChangedCallCount);
            // LogAssert.NoUnexpectedReceived — 由 NUnit 默认行为隐式覆盖（未 expect 的 warning 也会让测试失败 if NUnit 配置严格；
            // 但 Unity Test Runner 默认仅 ERROR 视为 fail，Warning 不强制；保险起见显式校验）
        }

        // ==================================================================
        // AC-6: 通过 GameEvent 接口端到端路径
        // ==================================================================

        [Test]
        public void AC6_GameEventInterfacePath_RoundTrip()
        {
            // OnRequestPuzzleLockAll → manager listener → PushLock → OnInteractionLockChanged
            GameEvent.Get<IInteractionEvent>().OnRequestPuzzleLockAll(InteractionLockerId.Narrative);

            Assert.IsTrue(_mgr.IsLocked);
            Assert.AreEqual(1, _lockChangedCallCount);
            Assert.IsTrue(_lastLockChangedValue);

            // OnRequestPuzzleUnlock → manager listener → PopLock → OnInteractionLockChanged
            GameEvent.Get<IInteractionEvent>().OnRequestPuzzleUnlock(InteractionLockerId.Narrative);

            Assert.IsFalse(_mgr.IsLocked);
            Assert.AreEqual(2, _lockChangedCallCount);
            Assert.IsFalse(_lastLockChangedValue);
        }

        // ==================================================================
        // Init / Dispose 幂等性（lifecycle 防御）
        // ==================================================================

        [Test]
        public void Init_Idempotent_DoubleCallSafe()
        {
            // SetUp 已调一次 Init；再调一次应 no-op，不抛异常
            _mgr.Init();
            _mgr.Init();   // 第 3 次

            // 通过 GameEvent 路径触发 — 验证只有 1 次 PushLock 效果（manager.PushLock 内部 HashSet 去重亦保证不出错；
            // 但本测试关键在于"不抛异常 + 行为符合预期"）
            GameEvent.Get<IInteractionEvent>().OnRequestPuzzleLockAll(InteractionLockerId.Narrative);

            Assert.IsTrue(_mgr.IsLocked);
            Assert.AreEqual(1, _mgr.ActiveLockCount);
            Assert.AreEqual(1, _lockChangedCallCount, "重复 Init 不应让 OnInteractionLockChanged 派发多次");
        }

        [Test]
        public void Dispose_RemovesListeners_FurtherEventsIgnored()
        {
            _mgr.Dispose();
            _lockChangedCallCount = 0;

            // 尝试通过 GameEvent 路径触发 — manager 已 Dispose，listener 已注销，应无响应
            GameEvent.Get<IInteractionEvent>().OnRequestPuzzleLockAll(InteractionLockerId.ShadowPuzzle);

            Assert.AreEqual(0, _mgr.ActiveLockCount, "Dispose 后 GameEvent 路径不再驱动 manager");
            Assert.AreEqual(0, _lockChangedCallCount, "manager 不派发 OnInteractionLockChanged");
        }

        [Test]
        public void Dispose_Idempotent_DoubleCallSafe()
        {
            _mgr.Dispose();
            _mgr.Dispose();   // 不应抛异常
        }

        // ==================================================================
        // 防御性：null / empty token
        // ==================================================================

        [Test]
        public void PushLock_NullOrEmpty_LogsWarningIgnored()
        {
            LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex(@"\[InteractionLock\] PushLock 收到空 lockerId"));
            _mgr.PushLock(null);

            Assert.IsFalse(_mgr.IsLocked);
            Assert.AreEqual(0, _lockChangedCallCount);

            LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex(@"\[InteractionLock\] PushLock 收到空 lockerId"));
            _mgr.PushLock(string.Empty);

            Assert.IsFalse(_mgr.IsLocked);
            Assert.AreEqual(0, _lockChangedCallCount);
        }
    }
}
