// 该文件由Cursor 自动生成
using NUnit.Framework;
using UnityEngine.TestTools;
using GameLogic;
using TEngine;

namespace ShadowGame.Tests.EditMode.SceneManagement
{
    /// <summary>
    /// S2-06 — Transition Mutex with Max-1 Queue + In-flight Dedup
    /// (story-004-transition-mutex.md, ADR-009 + ADR-027 + ADR-001).
    ///
    /// <para>S2-05 已覆盖基础 max-1 队列（AC-1..AC-9）；本 fixture 补充 S2-06 新规：
    /// (a) <c>_inflightChapterId</c> 字段 + sentinel 共享；
    /// (b) Loading 中重复请求同章节去重；
    /// (c) drain / RecoverToIdle 时 in-flight 清理；
    /// (d) rapid-fire 健壮性。</para>
    ///
    /// <para>Setup 复用 TENGINE-SG-002 fixture 模式（见 SceneStateMachineTests）。</para>
    /// </summary>
    [TestFixture]
    public class TransitionMutexTests
    {
        private SceneManager _sm;
        private int _sceneReadyCount;
        private int _lastSceneReadyChapterId;

        // ────────── Fixture lifecycle ──────────

        [SetUp]
        public void SetUp()
        {
            GameEvent.EventMgr.Init();
            _ = new ISceneEvent_Gen(GameEvent.EventMgr.GetDispatcher());

            _sceneReadyCount = 0;
            _lastSceneReadyChapterId = 0;
            GameEvent.AddEventListener<int>(
                ISceneEvent_Event.OnSceneReady, OnSceneReadyHandler);

            _sm = new SceneManager();
            _sm.Init();
        }

        [TearDown]
        public void TearDown()
        {
            GameEvent.RemoveEventListener<int>(
                ISceneEvent_Event.OnSceneReady, OnSceneReadyHandler);
            _sm?.Dispose();
            _sm = null;
        }

        private void OnSceneReadyHandler(int chapterId)
        {
            _sceneReadyCount++;
            _lastSceneReadyChapterId = chapterId;
        }

        private static void FireRequest(int chapterId) =>
            GameEvent.Get<ISceneEvent>().OnRequestSceneChange(chapterId);

        /// <summary>把状态机推进到 Loading 状态（in-flight = chapterId）。</summary>
        private void EnterLoading(int chapterId)
        {
            FireRequest(chapterId); // Idle → TransitionOut
            _sm.AdvanceStateForTest(SceneManagerState.Unloading);
            _sm.AdvanceStateForTest(SceneManagerState.Loading);
        }

        // ==================================================================
        // §0. NoChapterId 哨兵常量（一致性）
        // ==================================================================

        [Test]
        public void NoChapterId_Constant_Equals_MinusOne()
        {
            Assert.AreEqual(-1, SceneManager.NoChapterId,
                "NoChapterId 哨兵值约定为 -1，外部消费者依赖该值做 'no chapter' 判断。");
        }

        [Test]
        public void InitialState_CurrentAndInflight_AreNoChapterId()
        {
            Assert.AreEqual(SceneManager.NoChapterId, _sm.CurrentChapterId,
                "boot 时无章节加载，CurrentChapterId = NoChapterId。");
            Assert.AreEqual(SceneManager.NoChapterId, _sm.InflightChapterIdForTest,
                "boot 时无 in-flight 请求，InflightChapterId = NoChapterId。");
            Assert.IsNull(_sm.PendingTargetChapterIdForTest,
                "boot 时无 pending 请求。");
        }

        // ==================================================================
        // §1. AC-1: Loading 中收到不同章请求 → pending = new；不开新过渡
        // ==================================================================

        [Test]
        public void AC1_LoadingPlusDifferentChapterRequest_QueuesAsPending()
        {
            EnterLoading(1);
            Assert.AreEqual(SceneManagerState.Loading, _sm.CurrentState);

            FireRequest(3);

            Assert.AreEqual(SceneManagerState.Loading, _sm.CurrentState,
                "已有过渡进行时，新不同章请求不得改变状态。");
            Assert.AreEqual(3, _sm.PendingTargetChapterIdForTest,
                "新请求应进 pending（newest-wins）。");
            Assert.AreEqual(1, _sm.InflightChapterIdForTest,
                "in-flight 仍为 1，不被 pending 更新覆盖。");
        }

        [Test]
        public void AC1_TransitionOutPlusDifferentRequest_QueuesAsPending()
        {
            FireRequest(2); // Idle → TransitionOut，inflight=2
            Assert.AreEqual(SceneManagerState.TransitionOut, _sm.CurrentState);

            FireRequest(7);

            Assert.AreEqual(SceneManagerState.TransitionOut, _sm.CurrentState);
            Assert.AreEqual(7, _sm.PendingTargetChapterIdForTest);
            Assert.AreEqual(2, _sm.InflightChapterIdForTest);
        }

        // ==================================================================
        // §2. AC-2: 队列 newest-wins（max-1）
        // ==================================================================

        [Test]
        public void AC2_PendingExists_NewerRequestOverwrites()
        {
            EnterLoading(1);
            FireRequest(2);
            Assert.AreEqual(2, _sm.PendingTargetChapterIdForTest);

            FireRequest(5);

            Assert.AreEqual(5, _sm.PendingTargetChapterIdForTest,
                "新请求应覆盖旧 pending（max-1 队列，newest-wins）。");
        }

        [Test]
        public void AC2_RapidFire10Requests_OnlyLatestSurvives()
        {
            EnterLoading(99);

            for (int i = 1; i <= 10; i++) FireRequest(i);

            Assert.AreEqual(10, _sm.PendingTargetChapterIdForTest,
                "连发 10 次请求后只有最后一个（10）保留在 pending。");
            Assert.AreEqual(SceneManagerState.Loading, _sm.CurrentState,
                "rapid fire 不应造成状态机错乱。");
            Assert.AreEqual(99, _sm.InflightChapterIdForTest,
                "rapid fire 不应污染 in-flight。");
        }

        // ==================================================================
        // §3. AC-3: drain pending on return-to-Idle
        // ==================================================================

        [Test]
        public void AC3_DrainOnIdle_ConsumesPending_StartsNewTransition()
        {
            EnterLoading(1);
            FireRequest(4);
            Assert.AreEqual(4, _sm.PendingTargetChapterIdForTest);

            // 模拟 Story 002 完成 Loading：依次走完 TransitionIn → Idle
            _sm.AdvanceStateForTest(SceneManagerState.TransitionIn);
            _sm.AdvanceStateForTest(SceneManagerState.Idle);

            Assert.IsNull(_sm.PendingTargetChapterIdForTest,
                "drain 完成 pending 应清空。");
            Assert.AreEqual(SceneManagerState.TransitionOut, _sm.CurrentState,
                "drain 后立即开始下一个过渡。");
            Assert.AreEqual(4, _sm.CurrentChapterId,
                "drain 后 current = pending target。");
            Assert.AreEqual(4, _sm.InflightChapterIdForTest,
                "drain 启动新过渡时立即 set in-flight = pending target。");
        }

        [Test]
        public void AC3_DrainEdge_PendingEqualsCurrent_DispatchesSceneReady_NoNewTransition()
        {
            // 注：通过 ISceneEvent 自然路径 "FireRequest(X) 进过渡 → FireRequest(X) 入 pending" 在 S2-06
            // 引入 in-flight guard 后已不可达（同章请求会被去重）。本测试模拟 Story 002 内部驱动状态机
            // 推到 Loading（保持 inflight=NoChapterId）的情形 —— 即"DrainPending 同章 guard 的防御性代码路径"。
            // 与 S2-05 AC11Edge_PendingSameAsCurrent_DrainsToOnSceneReady_StaysIdle 同模式。

            // 1) 先成功加载 chapter=3 进 Idle（current=3, inflight=NoChapterId）
            FireRequest(3);
            _sm.AdvanceStateForTest(SceneManagerState.Idle);
            Assert.AreEqual(3, _sm.CurrentChapterId);
            Assert.AreEqual(SceneManager.NoChapterId, _sm.InflightChapterIdForTest);
            int sceneReadyBefore = _sceneReadyCount;

            // 2) 直接跳到 Loading（不经 FireRequest，inflight 仍 NoChapterId）
            _sm.AdvanceStateForTest(SceneManagerState.Loading);

            // 3) 此时 FireRequest(3)：in-flight guard（3 == NoChapterId？false）不命中，落 non-Idle → pending=3
            FireRequest(3);
            Assert.AreEqual(3, _sm.PendingTargetChapterIdForTest);

            // 4) drain：DrainPending 内 next=3, current=3 → 同章 OnSceneReady，state 保持 Idle
            _sm.AdvanceStateForTest(SceneManagerState.Idle);

            Assert.AreEqual(SceneManagerState.Idle, _sm.CurrentState,
                "drain 时 pending == current → 静默 OnSceneReady，state 保持 Idle。");
            Assert.AreEqual(sceneReadyBefore + 1, _sceneReadyCount,
                "应正好派发 1 次 OnSceneReady。");
            Assert.AreEqual(3, _lastSceneReadyChapterId,
                "OnSceneReady 携带 current chapter id（drain 路径同章语义）。");
            Assert.IsNull(_sm.PendingTargetChapterIdForTest);
            Assert.AreEqual(SceneManager.NoChapterId, _sm.InflightChapterIdForTest,
                "drain 后未开新过渡 → in-flight 保持 NoChapterId。");
        }

        // ==================================================================
        // §4. AC-4 补充：boot 状态 current=NoChapterId 不被同章 guard 误判
        // ==================================================================

        [Test]
        public void AC4_BootRequestChapter0_DoesNotShortCircuitAsSameChapter()
        {
            // boot：current=NoChapterId(-1)；请求 chapter 0（合法首章）应进过渡而非走"同章 OnSceneReady"
            Assert.AreEqual(SceneManager.NoChapterId, _sm.CurrentChapterId);

            FireRequest(0);

            Assert.AreEqual(0, _sceneReadyCount,
                "boot 状态请求 chapter 0 不得错触发同章 OnSceneReady（_currentChapterId != NoChapterId guard 应过滤）。");
            Assert.AreEqual(SceneManagerState.TransitionOut, _sm.CurrentState,
                "boot 首次合法请求应走 AC-7 新章节过渡分支。");
            Assert.AreEqual(0, _sm.CurrentChapterId);
            Assert.AreEqual(0, _sm.InflightChapterIdForTest);
        }

        // ==================================================================
        // §5. AC-5: in-flight 去重（本 story 核心新增）
        // ==================================================================

        [Test]
        public void AC5_LoadingPlusSameInflightRequest_SilentlyDiscarded()
        {
            EnterLoading(3);
            Assert.AreEqual(3, _sm.InflightChapterIdForTest);
            Assert.IsNull(_sm.PendingTargetChapterIdForTest);

            FireRequest(3);

            Assert.IsNull(_sm.PendingTargetChapterIdForTest,
                "in-flight 同章请求应静默丢弃，不污染 pending。");
            Assert.AreEqual(SceneManagerState.Loading, _sm.CurrentState,
                "in-flight 同章请求不改变状态。");
            Assert.AreEqual(0, _sceneReadyCount,
                "in-flight 同章请求不发 OnSceneReady（与 Idle 同章语义不同）。");
        }

        [Test]
        public void AC5_TransitionOutPlusSameInflightRequest_SilentlyDiscarded()
        {
            FireRequest(5); // Idle → TransitionOut, inflight=5
            Assert.AreEqual(SceneManagerState.TransitionOut, _sm.CurrentState);
            Assert.AreEqual(5, _sm.InflightChapterIdForTest);

            FireRequest(5);

            Assert.IsNull(_sm.PendingTargetChapterIdForTest,
                "TransitionOut 阶段同 in-flight 请求亦静默丢弃。");
            Assert.AreEqual(SceneManagerState.TransitionOut, _sm.CurrentState);
        }

        [Test]
        public void AC5_LoadingPlusDifferentChapterRequest_NotBlockedByInflightGuard()
        {
            EnterLoading(3);
            FireRequest(5);

            Assert.AreEqual(5, _sm.PendingTargetChapterIdForTest,
                "不同章请求应正常入 pending，in-flight guard 仅匹配同章。");
            Assert.AreEqual(3, _sm.InflightChapterIdForTest,
                "不同章请求不得修改 in-flight。");
        }

        [Test]
        public void AC5_RapidFireSameInflight_NoneSurvivesInPending()
        {
            EnterLoading(3);

            for (int i = 0; i < 10; i++) FireRequest(3);

            Assert.IsNull(_sm.PendingTargetChapterIdForTest,
                "10 次同 in-flight 重复请求应全部静默丢弃。");
            Assert.AreEqual(SceneManagerState.Loading, _sm.CurrentState);
            Assert.AreEqual(0, _sceneReadyCount);
        }

        // ==================================================================
        // §6. in-flight set / clear 时机
        // ==================================================================

        [Test]
        public void Inflight_SetOnIdleNewTransition()
        {
            Assert.AreEqual(SceneManager.NoChapterId, _sm.InflightChapterIdForTest);

            FireRequest(8);

            Assert.AreEqual(8, _sm.InflightChapterIdForTest,
                "Idle 不同章请求 → 立即 set in-flight = target。");
        }

        [Test]
        public void Inflight_ClearedByAdvanceStateForTestIdle_WhenNoPending()
        {
            EnterLoading(3);
            Assert.AreEqual(3, _sm.InflightChapterIdForTest);

            _sm.AdvanceStateForTest(SceneManagerState.TransitionIn);
            _sm.AdvanceStateForTest(SceneManagerState.Idle);

            Assert.AreEqual(SceneManager.NoChapterId, _sm.InflightChapterIdForTest,
                "走完 Idle、无 pending → in-flight 清回 NoChapterId。");
        }

        [Test]
        public void Inflight_ResetByAdvanceStateForTestIdle_WhenPendingExists()
        {
            EnterLoading(3);
            FireRequest(9); // pending=9

            _sm.AdvanceStateForTest(SceneManagerState.TransitionIn);
            _sm.AdvanceStateForTest(SceneManagerState.Idle);

            Assert.AreEqual(9, _sm.InflightChapterIdForTest,
                "drain 启动新过渡时立即重置 in-flight = 新 target（顺序：先清，后 DrainPending 重赋）。");
            Assert.AreEqual(SceneManagerState.TransitionOut, _sm.CurrentState);
        }

        [Test]
        public void Inflight_ClearedByRecoverToIdle()
        {
            EnterLoading(4);
            _sm.AdvanceStateForTest(SceneManagerState.Error);

            // Error 状态下记录的 inflight 仍为 4（Error 进入前的最后值）
            Assert.AreEqual(4, _sm.InflightChapterIdForTest);

            _sm.RecoverToIdle();

            Assert.AreEqual(SceneManagerState.Idle, _sm.CurrentState);
            Assert.AreEqual(SceneManager.NoChapterId, _sm.InflightChapterIdForTest,
                "RecoverToIdle 视为 Loading 中断 → 清 in-flight。");
        }

        [Test]
        public void Inflight_AfterRecoverToIdle_RequestSameChapter_NotBlocked()
        {
            // 验证 RecoverToIdle 清了 in-flight 后，原 chapter 可以重新被请求
            EnterLoading(4);
            _sm.AdvanceStateForTest(SceneManagerState.Error);
            _sm.RecoverToIdle();

            // 此时 current=4（Idle 时进过渡就已 set），重新请求 4 应走"Idle 同章 OnSceneReady"路径
            int before = _sceneReadyCount;
            FireRequest(4);

            Assert.AreEqual(before + 1, _sceneReadyCount,
                "RecoverToIdle 后 current=4 仍存在 → 重请求 4 走 Idle 同章 OnSceneReady 路径。");
            Assert.AreEqual(SceneManagerState.Idle, _sm.CurrentState,
                "Idle 同章请求不开过渡。");
        }
    }
}
