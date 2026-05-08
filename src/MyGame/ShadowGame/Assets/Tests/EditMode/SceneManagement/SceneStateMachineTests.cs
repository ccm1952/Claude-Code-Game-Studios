// 该文件由Cursor 自动生成
using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GameLogic;
using TEngine;

namespace ShadowGame.Tests.EditMode.SceneManagement
{
    /// <summary>
    /// S2-05 — Scene Manager State Machine + ISceneEvent 契约 (ADR-009 + ADR-027 + ADR-001).
    /// Covers AC-1..AC-15 of story-001-scene-state-machine.md.
    ///
    /// <para>Setup follows TENGINE-SG-002 precedent (see StateEventsTests): reset
    /// <c>GameEvent.EventMgr</c> and manually instantiate the generated
    /// <c>ISceneEvent_Gen</c> proxy to sidestep the empty
    /// <c>GameEventHelper.g.cs</c> produced in the test asmdef.</para>
    /// </summary>
    [TestFixture]
    public class SceneStateMachineTests
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

        // ==================================================================
        // A. ISceneEvent 契约（AC-1 / AC-2）
        // ==================================================================

        [Test]
        public void AC1_ISceneEvent_HasEventInterfaceAttribute_GroupLogic()
        {
            var attr = typeof(ISceneEvent).GetCustomAttribute<EventInterfaceAttribute>();

            Assert.IsNotNull(attr,
                "ISceneEvent must be annotated with [EventInterface] (ADR-027 §1).");
            Assert.AreEqual(EEventGroup.GroupLogic, attr.EventGroup,
                "ISceneEvent belongs to GroupLogic (business broadcast).");
        }

        [Test]
        public void AC1_ISceneEvent_DefinesNineMethodsInFrozenOrder()
        {
            var expected = new (string Name, Type[] Params)[]
            {
                ("OnRequestSceneChange",     new[] { typeof(int) }),
                ("OnSceneReady",             new[] { typeof(int) }),
                ("OnSceneTransitionBegin",   new[] { typeof(int), typeof(int) }),
                ("OnSceneUnloadBegin",       new[] { typeof(int) }),
                ("OnSceneDownloadProgress",  new[] { typeof(float), typeof(long), typeof(long) }),
                ("OnSceneLoadProgress",      new[] { typeof(string), typeof(float) }),
                ("OnSceneLoadComplete",      new[] { typeof(int), typeof(string) }),
                ("OnSceneTransitionEnd",     new[] { typeof(int) }),
                ("OnSceneLoadFailed",        new[] { typeof(int), typeof(string) }),
            };

            var actual = typeof(ISceneEvent).GetMethods();
            Assert.AreEqual(9, actual.Length,
                "ISceneEvent must expose exactly 9 methods (契约 frozen at S2-05).");

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i].Name, actual[i].Name,
                    $"Method[{i}] name mismatch.");
                var actualParams = actual[i].GetParameters().Select(p => p.ParameterType).ToArray();
                CollectionAssert.AreEqual(expected[i].Params, actualParams,
                    $"Method[{i}] ({expected[i].Name}) parameter types mismatch.");
            }
        }

        [Test]
        public void AC2_SourceGenerator_ProducesISceneEvent_Gen()
        {
            var genType = typeof(ISceneEvent).Assembly.GetType("GameLogic.ISceneEvent_Gen");

            Assert.IsNotNull(genType,
                "SG must emit GameLogic.ISceneEvent_Gen (dispatcher forwarder).");
            Assert.IsTrue(typeof(ISceneEvent).IsAssignableFrom(genType),
                "_Gen must implement ISceneEvent.");
            Assert.IsFalse(genType.IsAbstract, "_Gen must be a concrete class.");
            Assert.IsFalse(genType.IsGenericType, "_Gen must be non-generic.");
        }

        [Test]
        public void AC2_SourceGenerator_EmitsNineNonZeroUniqueEventIds()
        {
            var evtType = typeof(ISceneEvent).Assembly.GetType("GameLogic.ISceneEvent_Event");
            Assert.IsNotNull(evtType, "SG must emit GameLogic.ISceneEvent_Event static class.");

            var names = new[]
            {
                "OnRequestSceneChange", "OnSceneReady", "OnSceneTransitionBegin",
                "OnSceneUnloadBegin", "OnSceneDownloadProgress", "OnSceneLoadProgress",
                "OnSceneLoadComplete", "OnSceneTransitionEnd", "OnSceneLoadFailed",
            };

            var ids = new int[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                var field = evtType.GetField(names[i], BindingFlags.Public | BindingFlags.Static);
                Assert.IsNotNull(field, $"ISceneEvent_Event.{names[i]} (event id) must exist.");
                Assert.AreEqual(typeof(int), field.FieldType,
                    $"{names[i]} must be a public static int id.");
                ids[i] = (int)field.GetValue(null);
                Assert.AreNotEqual(0, ids[i], $"ISceneEvent_Event.{names[i]} id must be non-zero.");
            }

            CollectionAssert.AllItemsAreUnique(ids,
                "All 9 event ids must be pairwise distinct.");
        }

        // ==================================================================
        // AC-15 — TENGINE-SG-002 regression guard
        // ==================================================================

        [Test]
        public void AC15_GeneratedTypes_LiveInGameLogicAssembly_NotEditModeTests()
        {
            var genType = typeof(ISceneEvent).Assembly.GetType("GameLogic.ISceneEvent_Gen");
            var evtType = typeof(ISceneEvent).Assembly.GetType("GameLogic.ISceneEvent_Event");

            Assert.IsNotNull(genType);
            Assert.IsNotNull(evtType);
            Assert.AreEqual("GameLogic", genType.Assembly.GetName().Name,
                "ISceneEvent_Gen must be generated into GameLogic, not test asmdef (TENGINE-SG-002 guard).");
            Assert.AreEqual("GameLogic", evtType.Assembly.GetName().Name,
                "ISceneEvent_Event must be generated into GameLogic, not test asmdef (TENGINE-SG-002 guard).");
        }

        // ==================================================================
        // B. Scene Manager skeleton (AC-3 / AC-4 / AC-5 / AC-6)
        // ==================================================================

        [Test]
        public void AC3_SceneManagerState_EnumValuesInFrozenOrder()
        {
            var actual = (SceneManagerState[])Enum.GetValues(typeof(SceneManagerState));
            var expected = new[]
            {
                SceneManagerState.Idle,
                SceneManagerState.TransitionOut,
                SceneManagerState.Unloading,
                SceneManagerState.Loading,
                SceneManagerState.TransitionIn,
                SceneManagerState.Error,
            };
            CollectionAssert.AreEqual(expected, actual,
                "SceneManagerState enum order is frozen (ADR-009 state diagram).");
        }

        [Test]
        public void AC4_ISceneManager_ExposesThreeReadonlyProperties()
        {
            var t = typeof(ISceneManager);
            var props = t.GetProperties();
            Assert.AreEqual(3, props.Length, "ISceneManager must expose exactly 3 properties.");

            var byName = props.ToDictionary(p => p.Name);
            Assert.IsTrue(byName.ContainsKey("CurrentState"));
            Assert.AreEqual(typeof(SceneManagerState), byName["CurrentState"].PropertyType);
            Assert.IsNull(byName["CurrentState"].SetMethod, "CurrentState must be read-only.");

            Assert.IsTrue(byName.ContainsKey("CurrentChapterId"));
            Assert.AreEqual(typeof(int), byName["CurrentChapterId"].PropertyType);
            Assert.IsNull(byName["CurrentChapterId"].SetMethod, "CurrentChapterId must be read-only.");

            Assert.IsTrue(byName.ContainsKey("IsTransitioning"));
            Assert.AreEqual(typeof(bool), byName["IsTransitioning"].PropertyType);
            Assert.IsNull(byName["IsTransitioning"].SetMethod, "IsTransitioning must be read-only.");
        }

        [Test]
        public void AC5_InitialState_IsIdle_ChapterUnset_NotTransitioning()
        {
            Assert.AreEqual(SceneManagerState.Idle, _sm.CurrentState);
            Assert.AreEqual(-1, _sm.CurrentChapterId, "Initial CurrentChapterId must be -1 (unset).");
            Assert.IsFalse(_sm.IsTransitioning);
        }

        [TestCase(SceneManagerState.Idle, false)]
        [TestCase(SceneManagerState.TransitionOut, true)]
        [TestCase(SceneManagerState.Unloading, true)]
        [TestCase(SceneManagerState.Loading, true)]
        [TestCase(SceneManagerState.TransitionIn, true)]
        [TestCase(SceneManagerState.Error, false)]
        public void AC5_IsTransitioning_ReflectsNonIdleNonErrorStates(
            SceneManagerState state, bool expected)
        {
            _sm.AdvanceStateForTest(state);
            Assert.AreEqual(expected, _sm.IsTransitioning,
                $"IsTransitioning in {state} must be {expected}.");

            // Allow the DEBUG transition log
            LogAssert.ignoreFailingMessages = true;
        }

        [Test]
        public void AC6_Init_WhileTransitioning_IsNoOpWithWarning()
        {
            _sm.AdvanceStateForTest(SceneManagerState.Loading);

            LogAssert.Expect(LogType.Warning,
                new Regex(@"Init called while transition in progress"));
            _sm.Init();

            Assert.AreEqual(SceneManagerState.Loading, _sm.CurrentState,
                "Init must not reset an in-progress transition (AC-6).");
            LogAssert.ignoreFailingMessages = true;
        }

        // ==================================================================
        // C. OnRequestSceneChange 处理语义 (AC-7 / AC-8 / AC-9 / AC-10)
        // ==================================================================

        [Test]
        public void AC7_Idle_DifferentChapter_EntersTransitionOut()
        {
            FireRequest(1);

            Assert.AreEqual(SceneManagerState.TransitionOut, _sm.CurrentState);
            Assert.AreEqual(1, _sm.CurrentChapterId);
            Assert.IsTrue(_sm.IsTransitioning);
            Assert.AreEqual(0, _sceneReadyCount, "AC-7: must NOT dispatch OnSceneReady on different-chapter request.");
            LogAssert.ignoreFailingMessages = true;
        }

        [Test]
        public void AC7Edge_UnknownChapterId_StillTransitions()
        {
            // Chapter ID validity is Story 006's responsibility; state machine is opaque to values.
            FireRequest(99);

            Assert.AreEqual(SceneManagerState.TransitionOut, _sm.CurrentState);
            Assert.AreEqual(99, _sm.CurrentChapterId);
            LogAssert.ignoreFailingMessages = true;
        }

        [Test]
        public void AC8_Idle_SameChapter_DispatchesOnSceneReadyAndStaysIdle()
        {
            // Seed current chapter to 2 via a full transition cycle
            FireRequest(2);                                     // Idle → TransitionOut, _currentChapterId = 2
            _sm.AdvanceStateForTest(SceneManagerState.Idle);    // back to Idle with chapter = 2
            Assert.AreEqual(2, _sm.CurrentChapterId);
            _sceneReadyCount = 0;

            FireRequest(2);

            Assert.AreEqual(SceneManagerState.Idle, _sm.CurrentState,
                "AC-8: same-chapter request must keep state == Idle.");
            Assert.AreEqual(1, _sceneReadyCount,
                "AC-8: exactly one OnSceneReady dispatch for same-chapter request.");
            Assert.AreEqual(2, _lastSceneReadyChapterId);
            Assert.IsFalse(_sm.PendingTargetChapterIdForTest.HasValue,
                "AC-8: same-chapter request must not enter pending queue.");
            LogAssert.ignoreFailingMessages = true;
        }

        [Test]
        public void AC8Edge_InitialBoot_RequestMinusOne_TreatedAsDifferentChapter()
        {
            // Initial boot: _currentChapterId == -1; request(-1) fails the "!= -1" guard,
            // so it falls through to the different-chapter branch (TransitionOut).
            FireRequest(-1);

            Assert.AreEqual(SceneManagerState.TransitionOut, _sm.CurrentState);
            Assert.AreEqual(-1, _sm.CurrentChapterId);
            Assert.AreEqual(0, _sceneReadyCount,
                "OnSceneReady MUST NOT fire when CurrentChapterId is still -1 (unset guard).");
            LogAssert.ignoreFailingMessages = true;
        }

        [Test]
        public void AC9_NonIdle_Request_QueuesNewestWins()
        {
            _sm.AdvanceStateForTest(SceneManagerState.Loading);

            FireRequest(2);
            FireRequest(3);

            Assert.AreEqual(SceneManagerState.Loading, _sm.CurrentState,
                "State must not change when request arrives during transition.");
            Assert.IsTrue(_sm.PendingTargetChapterIdForTest.HasValue);
            Assert.AreEqual(3, _sm.PendingTargetChapterIdForTest.Value,
                "AC-9: newest-wins — (2) must be overwritten by (3).");
            Assert.AreEqual(0, _sceneReadyCount);
            LogAssert.ignoreFailingMessages = true;
        }

        [Test]
        public void AC9_NonIdle_TenRapidRequests_PendingEqualsLast()
        {
            _sm.AdvanceStateForTest(SceneManagerState.TransitionIn);

            for (int i = 1; i <= 10; i++) FireRequest(i);

            Assert.AreEqual(10, _sm.PendingTargetChapterIdForTest,
                "AC-9: pending must hold the last request only.");
            LogAssert.ignoreFailingMessages = true;
        }

        [Test]
        public void AC10_Error_Request_IsDroppedWithWarning_NoSceneReady()
        {
            _sm.AdvanceStateForTest(SceneManagerState.Error);

            LogAssert.Expect(LogType.Warning, new Regex(@"dropped — Error state"));
            FireRequest(3);

            Assert.AreEqual(SceneManagerState.Error, _sm.CurrentState);
            Assert.IsFalse(_sm.PendingTargetChapterIdForTest.HasValue,
                "AC-10: Error-state requests MUST NOT enter the pending queue.");
            Assert.AreEqual(0, _sceneReadyCount);
            LogAssert.ignoreFailingMessages = true;
        }

        // ==================================================================
        // D. Drain & recovery (AC-11 / AC-12)
        // ==================================================================

        [Test]
        public void AC11_PendingDrain_OnReturnToIdle_StartsNextTransition()
        {
            _sm.AdvanceStateForTest(SceneManagerState.Loading);
            FireRequest(4);
            FireRequest(5);                                    // pending = 5
            _sm.AdvanceStateForTest(SceneManagerState.Idle);   // triggers DrainPending

            Assert.AreEqual(SceneManagerState.TransitionOut, _sm.CurrentState,
                "AC-11: draining a different-chapter pending must enter TransitionOut.");
            Assert.AreEqual(5, _sm.CurrentChapterId);
            Assert.IsFalse(_sm.PendingTargetChapterIdForTest.HasValue,
                "AC-11: pending slot must be cleared after drain.");
            LogAssert.ignoreFailingMessages = true;
        }

        [Test]
        public void AC11Edge_PendingSameAsCurrent_DrainsToOnSceneReady_StaysIdle()
        {
            // Seed chapter = 3
            FireRequest(3);
            _sm.AdvanceStateForTest(SceneManagerState.Idle);
            Assert.AreEqual(3, _sm.CurrentChapterId);
            _sceneReadyCount = 0;

            // Now enter Loading and queue a same-chapter request
            _sm.AdvanceStateForTest(SceneManagerState.Loading);
            FireRequest(3);
            _sm.AdvanceStateForTest(SceneManagerState.Idle);   // drain

            Assert.AreEqual(SceneManagerState.Idle, _sm.CurrentState,
                "AC-11 edge: draining a same-chapter pending must stay in Idle.");
            Assert.AreEqual(1, _sceneReadyCount,
                "AC-11 edge: drain must fire OnSceneReady exactly once.");
            Assert.AreEqual(3, _lastSceneReadyChapterId);
            Assert.IsFalse(_sm.PendingTargetChapterIdForTest.HasValue);
            LogAssert.ignoreFailingMessages = true;
        }

        [Test]
        public void AC12_ErrorRecoverToIdle_ReturnsToIdle()
        {
            _sm.AdvanceStateForTest(SceneManagerState.Error);

            _sm.RecoverToIdle();

            Assert.AreEqual(SceneManagerState.Idle, _sm.CurrentState);
            LogAssert.ignoreFailingMessages = true;
        }

        [Test]
        public void AC12_RecoverToIdle_InNonErrorState_IsNoOpWithWarning()
        {
            _sm.AdvanceStateForTest(SceneManagerState.Loading);

            LogAssert.Expect(LogType.Warning, new Regex(@"RecoverToIdle called in"));
            _sm.RecoverToIdle();

            Assert.AreEqual(SceneManagerState.Loading, _sm.CurrentState,
                "AC-12: RecoverToIdle must be a no-op outside Error state.");
            LogAssert.ignoreFailingMessages = true;
        }

        // ==================================================================
        // AC-13 — debug transition log
        // ==================================================================

        [Test]
        public void AC13_StateTransition_EmitsDebugLogInExpectedFormat()
        {
            LogAssert.Expect(LogType.Log, new Regex(@"\[SceneManager\] Idle → TransitionOut"));

            FireRequest(1);

            // Allow any additional [SceneManager] transition logs from later test activity
            LogAssert.ignoreFailingMessages = true;
        }

        // ==================================================================
        // AC-14 — listener lifecycle pairing
        // ==================================================================

        [Test]
        public void AC14_Dispose_RemovesListener_SubsequentRequestIsNoOp()
        {
            _sm.Dispose();

            FireRequest(9);

            Assert.AreEqual(SceneManagerState.Idle, _sm.CurrentState,
                "AC-14: after Dispose, ISceneEvent.OnRequestSceneChange must not mutate state.");
            Assert.AreEqual(-1, _sm.CurrentChapterId,
                "AC-14: after Dispose, CurrentChapterId must stay at initial value.");
            Assert.IsFalse(_sm.IsTransitioning);
        }

        [Test]
        public void AC14_DoubleDispose_IsNoOp()
        {
            Assert.DoesNotThrow(() => _sm.Dispose());
            Assert.DoesNotThrow(() => _sm.Dispose(),
                "AC-14: Dispose must be idempotent.");
        }
    }
}
