// 该文件由Cursor 自动生成
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GameLogic;
using TEngine;

namespace ShadowGame.Tests.EditMode.ObjectInteraction
{
    /// <summary>
    /// S2-08 — Object Interaction State Machine (ADR-013 + ADR-027).
    /// Covers AC-1..AC-9 of story-001-interaction-state-machine.md.
    ///
    /// <para>Setup follows S2-05 SceneStateMachineTests precedent: reset
    /// <c>GameEvent.EventMgr</c> and manually instantiate the generated
    /// <c>IInteractionEvent_Gen</c> proxy to sidestep the empty
    /// <c>GameEventHelper.g.cs</c> in the test asmdef.</para>
    /// </summary>
    [TestFixture]
    public class InteractableObjectFsmTests
    {
        private const int TestObjectId = 42;

        private InteractableObjectFsm _fsm;
        private List<int> _selectedEvents;
        private List<int> _deselectedEvents;

        // ────────── Fixture lifecycle ──────────

        [SetUp]
        public void SetUp()
        {
            GameEvent.EventMgr.Init();
            _ = new IInteractionEvent_Gen(GameEvent.EventMgr.GetDispatcher());

            _selectedEvents = new List<int>();
            _deselectedEvents = new List<int>();

            GameEvent.AddEventListener<int>(
                IInteractionEvent_Event.OnObjectSelected, OnObjectSelectedHandler);
            GameEvent.AddEventListener<int>(
                IInteractionEvent_Event.OnObjectDeselected, OnObjectDeselectedHandler);

            _fsm = new InteractableObjectFsm(TestObjectId);
        }

        [TearDown]
        public void TearDown()
        {
            GameEvent.RemoveEventListener<int>(
                IInteractionEvent_Event.OnObjectSelected, OnObjectSelectedHandler);
            GameEvent.RemoveEventListener<int>(
                IInteractionEvent_Event.OnObjectDeselected, OnObjectDeselectedHandler);
            _fsm = null;
        }

        private void OnObjectSelectedHandler(int objectId) => _selectedEvents.Add(objectId);
        private void OnObjectDeselectedHandler(int objectId) => _deselectedEvents.Add(objectId);

        // ==================================================================
        // AC-1: 初始状态 Idle + ObjectId 正确
        // ==================================================================

        [Test]
        public void AC1_InitialState_IsIdle_AndObjectIdMatchesCtor()
        {
            Assert.AreEqual(InteractableObjectState.Idle, _fsm.CurrentState,
                "FSM 必须以 Idle 状态启动（ADR-013 §State Transition Rules）。");
            Assert.AreEqual(TestObjectId, _fsm.ObjectId,
                "ObjectId 必须等于构造参数。");
        }

        // ==================================================================
        // AC-2: Idle + OnTapHit → Selected + 派 OnObjectSelected
        // ==================================================================

        [Test]
        public void AC2_OnTapHit_FromIdle_TransitionsToSelected_AndDispatchesOnObjectSelected()
        {
            _fsm.OnTapHit();

            Assert.AreEqual(InteractableObjectState.Selected, _fsm.CurrentState);
            Assert.AreEqual(1, _selectedEvents.Count, "OnObjectSelected 必须派发恰好一次。");
            Assert.AreEqual(TestObjectId, _selectedEvents[0]);
            Assert.AreEqual(0, _deselectedEvents.Count, "OnObjectDeselected 不应派发。");
        }

        // ==================================================================
        // AC-3: Selected + OnDeselect → Idle + 派 OnObjectDeselected
        // ==================================================================

        [Test]
        public void AC3_OnDeselect_FromSelected_TransitionsToIdle_AndDispatchesOnObjectDeselected()
        {
            _fsm.OnTapHit();              // → Selected
            _selectedEvents.Clear();      // 排除 setup 派发，专注 deselect

            _fsm.OnDeselect();

            Assert.AreEqual(InteractableObjectState.Idle, _fsm.CurrentState);
            Assert.AreEqual(1, _deselectedEvents.Count);
            Assert.AreEqual(TestObjectId, _deselectedEvents[0]);
            Assert.AreEqual(0, _selectedEvents.Count, "Deselect 不应派发 OnObjectSelected。");
        }

        // ==================================================================
        // AC-4: 完整 Drag → Snap → Idle 链路
        // ==================================================================

        [Test]
        public void AC4_FullDragSnapIdleCycle_TransitionsCorrectly_NoExtraDispatches()
        {
            _fsm.OnTapHit();              // Idle → Selected
            _fsm.OnDragBegan();           // Selected → Dragging
            Assert.AreEqual(InteractableObjectState.Dragging, _fsm.CurrentState);

            _fsm.OnDragEnded();           // Dragging → Snapping
            Assert.AreEqual(InteractableObjectState.Snapping, _fsm.CurrentState);

            _fsm.OnSnapCompleted();       // Snapping → Idle
            Assert.AreEqual(InteractableObjectState.Idle, _fsm.CurrentState);

            // 链路中只有第一次 OnTapHit 派 OnObjectSelected；其他 transition 无 sender
            Assert.AreEqual(1, _selectedEvents.Count);
            Assert.AreEqual(0, _deselectedEvents.Count,
                "Drag/Snap 链路中不应派发 OnObjectDeselected（落点未定时由 Story 004 派 OnObjectTransformChanged）。");
        }

        // ==================================================================
        // AC-5: Lock 转换 — 多 from-state 全覆盖
        // ==================================================================

        [Test]
        public void AC5_OnLockChangedTrue_FromIdle_TransitionsToLocked_NoDeselectDispatch()
        {
            _fsm.OnLockChanged(true);

            Assert.AreEqual(InteractableObjectState.Locked, _fsm.CurrentState);
            Assert.AreEqual(0, _deselectedEvents.Count,
                "from=Idle 时不派 OnObjectDeselected（仅 from=Selected 才派）。");
        }

        [Test]
        public void AC5_OnLockChangedTrue_FromSelected_TransitionsToLocked_DispatchesOnObjectDeselected()
        {
            _fsm.OnTapHit();              // Idle → Selected
            _selectedEvents.Clear();

            _fsm.OnLockChanged(true);

            Assert.AreEqual(InteractableObjectState.Locked, _fsm.CurrentState);
            Assert.AreEqual(1, _deselectedEvents.Count, "from=Selected 时必须派 OnObjectDeselected 一次。");
            Assert.AreEqual(TestObjectId, _deselectedEvents[0]);
        }

        [Test]
        public void AC5_OnLockChangedTrue_FromDragging_TransitionsToLocked_NoDispatches()
        {
            _fsm.OnTapHit();
            _fsm.OnDragBegan();           // → Dragging
            _selectedEvents.Clear();

            _fsm.OnLockChanged(true);

            Assert.AreEqual(InteractableObjectState.Locked, _fsm.CurrentState);
            Assert.AreEqual(0, _deselectedEvents.Count);
            Assert.AreEqual(0, _selectedEvents.Count);
        }

        [Test]
        public void AC5_OnLockChangedTrue_FromSnapping_TransitionsToLocked_NoTransformChanged()
        {
            _fsm.OnTapHit();
            _fsm.OnDragBegan();
            _fsm.OnDragEnded();           // → Snapping
            _selectedEvents.Clear();

            _fsm.OnLockChanged(true);

            Assert.AreEqual(InteractableObjectState.Locked, _fsm.CurrentState);
            // 落点未定，本 story 不验 OnObjectTransformChanged（Story 004 验）；这里只验 deselected 不被错派
            Assert.AreEqual(0, _deselectedEvents.Count,
                "from=Snapping（非 Selected）不派 OnObjectDeselected。");
        }

        [Test]
        public void AC5_OnLockChangedFalse_FromLocked_TransitionsToIdle_NoDispatches()
        {
            _fsm.OnLockChanged(true);     // → Locked

            _fsm.OnLockChanged(false);

            Assert.AreEqual(InteractableObjectState.Idle, _fsm.CurrentState);
            Assert.AreEqual(0, _selectedEvents.Count);
            Assert.AreEqual(0, _deselectedEvents.Count);
        }

        // ==================================================================
        // AC-6: 非法转换静默 + Log.Warning（不抛、不改状态、不派 sender）
        // ==================================================================

        [Test]
        public void AC6_OnDragBegan_FromIdle_IsSilentlyDropped_WithWarning_NoDispatch()
        {
            LogAssert.Expect(LogType.Warning,
                new Regex(@"\[InteractableObjectFsm#42\] OnDragBegan ignored in Idle"));

            _fsm.OnDragBegan();

            Assert.AreEqual(InteractableObjectState.Idle, _fsm.CurrentState);
            Assert.AreEqual(0, _selectedEvents.Count);
            Assert.AreEqual(0, _deselectedEvents.Count);
        }

        [Test]
        public void AC6_OnTapHit_FromLocked_IsSilentlyDropped_WithWarning()
        {
            _fsm.OnLockChanged(true);     // → Locked

            LogAssert.Expect(LogType.Warning,
                new Regex(@"\[InteractableObjectFsm#42\] OnTapHit ignored in Locked"));

            _fsm.OnTapHit();

            Assert.AreEqual(InteractableObjectState.Locked, _fsm.CurrentState,
                "Locked 状态对所有 gesture 触发免疫。");
            Assert.AreEqual(0, _selectedEvents.Count);
        }

        [Test]
        public void AC6_OnDeselect_FromLocked_IsSilentlyDropped_WithWarning()
        {
            _fsm.OnLockChanged(true);

            LogAssert.Expect(LogType.Warning,
                new Regex(@"\[InteractableObjectFsm#42\] OnDeselect ignored in Locked"));

            _fsm.OnDeselect();

            Assert.AreEqual(InteractableObjectState.Locked, _fsm.CurrentState);
            Assert.AreEqual(0, _deselectedEvents.Count);
        }

        // ==================================================================
        // AC-7: Snapping + OnTapHit → Selected（中断 snap 路径，rule 6）
        // ==================================================================

        [Test]
        public void AC7_OnTapHit_FromSnapping_TransitionsToSelected_DispatchesOnObjectSelected()
        {
            _fsm.OnTapHit();
            _fsm.OnDragBegan();
            _fsm.OnDragEnded();           // → Snapping
            _selectedEvents.Clear();

            _fsm.OnTapHit();

            Assert.AreEqual(InteractableObjectState.Selected, _fsm.CurrentState);
            Assert.AreEqual(1, _selectedEvents.Count,
                "中断 snap 时再选必须派 OnObjectSelected 一次（与 Idle→Selected 同语义）。");
            Assert.AreEqual(TestObjectId, _selectedEvents[0]);
        }

        // ==================================================================
        // AC-8: StateChanged C# event 派发顺序正确
        // ==================================================================

        [Test]
        public void AC8_StateChanged_FullCycle_FiresInExpectedOrderWithCorrectPrevNext()
        {
            var transitions = new List<(InteractableObjectState prev, InteractableObjectState next)>();
            _fsm.StateChanged += (prev, next) => transitions.Add((prev, next));

            _fsm.OnTapHit();              // Idle → Selected
            _fsm.OnDragBegan();           // Selected → Dragging
            _fsm.OnDragEnded();           // Dragging → Snapping
            _fsm.OnSnapCompleted();       // Snapping → Idle

            var expected = new[]
            {
                (InteractableObjectState.Idle,     InteractableObjectState.Selected),
                (InteractableObjectState.Selected, InteractableObjectState.Dragging),
                (InteractableObjectState.Dragging, InteractableObjectState.Snapping),
                (InteractableObjectState.Snapping, InteractableObjectState.Idle),
            };

            Assert.AreEqual(expected.Length, transitions.Count,
                "StateChanged 必须派发恰好 4 次（每条 transition 一次）。");
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i].Item1, transitions[i].prev, $"transition {i} prev 错误。");
                Assert.AreEqual(expected[i].Item2, transitions[i].next, $"transition {i} next 错误。");
            }
        }

        // ==================================================================
        // AC-9: 协议合规 — 在测试层验证 IInteractionEvent 仍以 [EventInterface] 注册到 GroupLogic，
        //         并且 fsm 类型在 sender 调用路径中确实经过 GameEvent.Get<IInteractionEvent>() 而非 Evt_*。
        //         （硬性 grep 由 evidence 文档兜底，本测试做反射 sanity check。）
        // ==================================================================

        [Test]
        public void AC9_IInteractionEvent_HasEventInterfaceAttribute_GroupLogic()
        {
            var attr = typeof(IInteractionEvent).GetCustomAttribute<EventInterfaceAttribute>();

            Assert.IsNotNull(attr,
                "IInteractionEvent 必须以 [EventInterface] 标注（ADR-027 §1）。");
            Assert.AreEqual(EEventGroup.GroupLogic, attr.EventGroup,
                "IInteractionEvent 属于 GroupLogic（业务广播，与 ISceneEvent / IGestureEvent 同组）。");
        }

        // ==================================================================
        // 边界用例补充：幂等性 + ObjectId 多实例隔离
        // ==================================================================

        [Test]
        public void Idempotent_OnLockChangedTrue_AlreadyLocked_NoExtraDispatch()
        {
            _fsm.OnLockChanged(true);

            _fsm.OnLockChanged(true);     // 二次 lock，幂等

            Assert.AreEqual(InteractableObjectState.Locked, _fsm.CurrentState);
            Assert.AreEqual(0, _selectedEvents.Count);
            Assert.AreEqual(0, _deselectedEvents.Count);
        }

        [Test]
        public void Idempotent_OnLockChangedFalse_NotLocked_NoTransition()
        {
            _fsm.OnLockChanged(false);    // Idle 时 unlock，幂等 no-op

            Assert.AreEqual(InteractableObjectState.Idle, _fsm.CurrentState);
            Assert.AreEqual(0, _selectedEvents.Count);
            Assert.AreEqual(0, _deselectedEvents.Count);
        }

        [Test]
        public void MultiInstance_DispatchesCarryCorrectObjectId()
        {
            var fsm2 = new InteractableObjectFsm(99);

            _fsm.OnTapHit();
            fsm2.OnTapHit();

            Assert.AreEqual(2, _selectedEvents.Count);
            CollectionAssert.AreEquivalent(new[] { TestObjectId, 99 }, _selectedEvents,
                "两个 fsm 实例的 sender 必须各自携带正确 ObjectId（不串）。");
        }
    }
}
