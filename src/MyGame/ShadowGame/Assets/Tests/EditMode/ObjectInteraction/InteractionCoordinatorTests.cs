// 该文件由Cursor 自动生成
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GameLogic;
using TEngine;

namespace ShadowGame.Tests.EditMode.ObjectInteraction
{
    /// <summary>
    /// S2-13 — InteractionCoordinator（单选语义 + 200ms debounce + Drag 转发 + Lock 清自身引用）。
    /// 覆盖 AC-2/AC-3/AC-6 of <c>story-007-multi-object-scene.md</c> + Drag 转发 + Init/Dispose。
    ///
    /// <para><b>EditMode-only 范围</b>：单选切换语义、debounce 时间窗口、OnDrag Began/Ended 转发、
    /// OnInteractionLockChanged 清引用。<b>不</b>覆盖 Raycast 物理命中（Physics2D 在 EditMode 可调用但 collider
    /// 配置脆弱）— 测试通过 <see cref="InteractionCoordinator.TrySelectObject"/> 公开 API bypass raycast；
    /// fat finger 数学 + 10 object 性能留 PlayMode/device 测试（AC-1/AC-4/AC-5）。</para>
    ///
    /// <para><b>Fixture 模式</b>：MonoBehaviour 测试 — 须 GameObject + AddComponent + 显式 Initialize/Shutdown
    /// （EditMode 不调 OnEnable，参 conventions.md "测试 fixture 规范" R1）。Camera stub + PuzzleConfigProvider +
    /// InputConfigProvider 与 GridSnapTests / DragMechanicsTests 同 boilerplate（R1 强约束）。</para>
    /// </summary>
    [TestFixture]
    public class InteractionCoordinatorTests
    {
        private GameObject _coordGo;
        private InteractionCoordinator _coord;
        private GameObject _cameraGo;
        private Camera _camera;
        private List<GameObject> _ioGos = new List<GameObject>();
        private List<InteractableObject> _ios = new List<InteractableObject>();

        // listener 计数
        private int _onObjectSelectedCount;
        private int _onObjectDeselectedCount;
        private int _lastSelectedId;
        private int _lastDeselectedId;

        // ────────── Fixture lifecycle ──────────

        [SetUp]
        public void SetUp()
        {
            GameEvent.EventMgr.Init();
            _ = new IGestureEvent_Gen(GameEvent.EventMgr.GetDispatcher());
            _ = new IInteractionEvent_Gen(GameEvent.EventMgr.GetDispatcher());
            _ = new ISceneEvent_Gen(GameEvent.EventMgr.GetDispatcher());

            _onObjectSelectedCount = 0;
            _onObjectDeselectedCount = 0;
            _lastSelectedId = -1;
            _lastDeselectedId = -1;

            GameEvent.AddEventListener<int>(IInteractionEvent_Event.OnObjectSelected, OnObjectSelectedListener);
            GameEvent.AddEventListener<int>(IInteractionEvent_Event.OnObjectDeselected, OnObjectDeselectedListener);

            // PuzzleConfigProvider（InteractableObject Initialize 需要）
            InteractableObject.RegisterPuzzleConfigProvider(puzzleId => new PuzzleConfig(
                id: puzzleId,
                interactionBounds: new InteractionBounds(-10f, 10f, -10f, 10f),
                gridSize: 1f,
                snapSpeed: 0.2f));

            // InputConfigProvider
            InteractionCoordinator.RegisterInputConfigProvider(() =>
            {
                var cfg = new InputConfigFromLuban();
                cfg.InitWithDefaults();
                cfg.SetScreenDpi(326f);   // iPhone Retina baseline
                return cfg;
            });

            // Stub Camera（与 DragMechanicsTests / GridSnapTests 一致）
            _cameraGo = new GameObject("StubCamera");
            _camera = _cameraGo.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 5f;
            _camera.transform.position = new Vector3(0, 0, -10f);
        }

        [TearDown]
        public void TearDown()
        {
            GameEvent.RemoveEventListener<int>(IInteractionEvent_Event.OnObjectSelected, OnObjectSelectedListener);
            GameEvent.RemoveEventListener<int>(IInteractionEvent_Event.OnObjectDeselected, OnObjectDeselectedListener);

            if (_coord != null)
            {
                _coord.Shutdown();
            }
            foreach (var io in _ios)
            {
                if (io != null) io.Shutdown();
            }

            if (_coordGo != null) Object.DestroyImmediate(_coordGo);
            foreach (var go in _ioGos)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            if (_cameraGo != null) Object.DestroyImmediate(_cameraGo);

            _ios.Clear();
            _ioGos.Clear();
            _coord = null;
            _coordGo = null;
            _camera = null;
            _cameraGo = null;

            InteractableObject.ClearPuzzleConfigProviderForTest();
            InteractionCoordinator.ClearInputConfigProviderForTest();
        }

        private void OnObjectSelectedListener(int objectId)
        {
            _onObjectSelectedCount++;
            _lastSelectedId = objectId;
        }

        private void OnObjectDeselectedListener(int objectId)
        {
            _onObjectDeselectedCount++;
            _lastDeselectedId = objectId;
        }

        // ────────── Helpers ──────────

        private InteractableObject MakeInteractableObject(int objectId, int puzzleId = 1)
        {
            var go = new GameObject($"InteractableObject#{objectId}");
            var io = go.AddComponent<InteractableObject>();
            // 反射写 _objectId / _puzzleId / _gameplayCamera
            SetPrivateField(io, "_objectId", objectId);
            SetPrivateField(io, "_puzzleId", puzzleId);
            SetPrivateField(io, "_gameplayCamera", _camera);
            io.Initialize();

            _ioGos.Add(go);
            _ios.Add(io);
            return io;
        }

        private InteractionCoordinator MakeCoordinator(List<InteractableObject> objs)
        {
            _coordGo = new GameObject("InteractionCoordinator");
            _coord = _coordGo.AddComponent<InteractionCoordinator>();
            _coord.SetGameplayCameraForTest(_camera);
            _coord.SetObjectsForTest(objs);
            _coord.Initialize();
            return _coord;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(field, $"未找到私有字段 {fieldName}");
            field.SetValue(target, value);
        }

        // ==================================================================
        // AC-2: 单选切换语义
        // ==================================================================

        [Test]
        public void AC2_TrySelectObject_FromNullToA_TransitionsAToSelected_DispatchesOnObjectSelected()
        {
            var a = MakeInteractableObject(1);
            MakeCoordinator(new List<InteractableObject> { a });

            bool changed = _coord.TrySelectObject(a);

            Assert.IsTrue(changed);
            Assert.AreSame(a, _coord.CurrentSelectedObject);
            Assert.AreEqual(InteractableObjectState.Selected, a.Fsm.CurrentState);
            Assert.AreEqual(1, _onObjectSelectedCount);
            Assert.AreEqual(1, _lastSelectedId);
            Assert.AreEqual(0, _onObjectDeselectedCount, "首次选取无 deselect");
        }

        [Test]
        public void AC2_TrySelectObject_SwitchAToB_DeselectAThenSelectB()
        {
            var a = MakeInteractableObject(1);
            var b = MakeInteractableObject(2);
            MakeCoordinator(new List<InteractableObject> { a, b });

            _coord.TrySelectObject(a);
            _coord.ResetDebounceForTest();   // 跳过 debounce 验证切换语义
            _onObjectSelectedCount = 0;
            _onObjectDeselectedCount = 0;
            _lastSelectedId = -1;
            _lastDeselectedId = -1;

            bool changed = _coord.TrySelectObject(b);

            Assert.IsTrue(changed);
            Assert.AreSame(b, _coord.CurrentSelectedObject);
            Assert.AreEqual(InteractableObjectState.Idle, a.Fsm.CurrentState, "A 已 deselect");
            Assert.AreEqual(InteractableObjectState.Selected, b.Fsm.CurrentState);
            Assert.AreEqual(1, _onObjectDeselectedCount);
            Assert.AreEqual(1, _lastDeselectedId);
            Assert.AreEqual(1, _onObjectSelectedCount);
            Assert.AreEqual(2, _lastSelectedId);
        }

        [Test]
        public void AC2_TrySelectObject_HitNull_DeselectsCurrent()
        {
            var a = MakeInteractableObject(1);
            MakeCoordinator(new List<InteractableObject> { a });

            _coord.TrySelectObject(a);
            _coord.ResetDebounceForTest();
            _onObjectDeselectedCount = 0;

            bool changed = _coord.TrySelectObject(null);

            Assert.IsTrue(changed);
            Assert.IsNull(_coord.CurrentSelectedObject);
            Assert.AreEqual(InteractableObjectState.Idle, a.Fsm.CurrentState);
            Assert.AreEqual(1, _onObjectDeselectedCount);
        }

        [Test]
        public void AC2_TrySelectObject_HitSameObject_NoOp()
        {
            var a = MakeInteractableObject(1);
            MakeCoordinator(new List<InteractableObject> { a });

            _coord.TrySelectObject(a);
            _coord.ResetDebounceForTest();
            _onObjectSelectedCount = 0;
            _onObjectDeselectedCount = 0;

            bool changed = _coord.TrySelectObject(a);

            Assert.IsFalse(changed, "同对象不重选");
            Assert.AreSame(a, _coord.CurrentSelectedObject);
            Assert.AreEqual(0, _onObjectSelectedCount);
            Assert.AreEqual(0, _onObjectDeselectedCount);
        }

        [Test]
        public void AC2_TrySelectObject_HitNullWhenNoneSelected_NoOp()
        {
            var a = MakeInteractableObject(1);
            MakeCoordinator(new List<InteractableObject> { a });

            bool changed = _coord.TrySelectObject(null);

            Assert.IsFalse(changed);
            Assert.IsNull(_coord.CurrentSelectedObject);
            Assert.AreEqual(0, _onObjectDeselectedCount);
        }

        // ==================================================================
        // AC-3: 200ms debounce
        // ==================================================================

        [Test]
        public void AC3_TrySelectObject_WithinDebounce_RejectsSwitch()
        {
            var a = MakeInteractableObject(1);
            var b = MakeInteractableObject(2);
            MakeCoordinator(new List<InteractableObject> { a, b });

            _coord.TrySelectObject(a);   // 设置 _lastSelectionTime = unscaledTime
            _onObjectSelectedCount = 0;
            _onObjectDeselectedCount = 0;

            bool changed = _coord.TrySelectObject(b);   // 同帧/极短时间 → 应被 debounce 拒

            Assert.IsFalse(changed, "200ms 内 second tap 被 debounce 拒");
            Assert.AreSame(a, _coord.CurrentSelectedObject, "A 保持选中");
            Assert.AreEqual(InteractableObjectState.Selected, a.Fsm.CurrentState);
            Assert.AreEqual(InteractableObjectState.Idle, b.Fsm.CurrentState);
            Assert.AreEqual(0, _onObjectSelectedCount);
            Assert.AreEqual(0, _onObjectDeselectedCount);
        }

        [Test]
        public void AC3_TrySelectObject_AfterDebounceReset_AllowsSwitch()
        {
            var a = MakeInteractableObject(1);
            var b = MakeInteractableObject(2);
            MakeCoordinator(new List<InteractableObject> { a, b });

            _coord.TrySelectObject(a);
            _coord.ResetDebounceForTest();   // 模拟 0.2s 已过

            bool changed = _coord.TrySelectObject(b);

            Assert.IsTrue(changed);
            Assert.AreSame(b, _coord.CurrentSelectedObject);
        }

        // ==================================================================
        // AC-6: OnInteractionLockChanged 清 CurrentSelectedObject 引用
        // ==================================================================

        [Test]
        public void AC6_OnInteractionLockChanged_True_ClearsCurrentSelectedObject()
        {
            var a = MakeInteractableObject(1);
            MakeCoordinator(new List<InteractableObject> { a });

            _coord.TrySelectObject(a);
            Assert.AreSame(a, _coord.CurrentSelectedObject);
            Assert.AreEqual(InteractableObjectState.Selected, a.Fsm.CurrentState);

            // 通过 LockManager 触发（路径与生产一致）
            _coord.LockManager.PushLock(InteractionLockerId.Narrative);

            Assert.IsNull(_coord.CurrentSelectedObject, "Coordinator 自身已清引用");
            Assert.AreEqual(InteractableObjectState.Locked, a.Fsm.CurrentState,
                "InteractableObject 自订阅 OnInteractionLockChanged → fsm 转 Locked");
            Assert.AreEqual(1, _onObjectDeselectedCount,
                "fsm.OnLockChanged from Selected → 派 OnObjectDeselected 一次（不由 Coordinator 重复派）");
        }

        [Test]
        public void AC6_OnInteractionLockChanged_False_NoEffect()
        {
            var a = MakeInteractableObject(1);
            MakeCoordinator(new List<InteractableObject> { a });

            _coord.TrySelectObject(a);
            int prevSelectedCount = _onObjectSelectedCount;

            // 模拟 unlock 派发（无锁时本应无 transition；这里直接通过 LockManager.PopLock 但 set 已空）
            // 改用直接事件派发以单独测 listener
            GameEvent.Get<IInteractionEvent>().OnInteractionLockChanged(false);

            Assert.AreSame(a, _coord.CurrentSelectedObject, "isLocked=false 不影响当前选中");
            Assert.AreEqual(prevSelectedCount, _onObjectSelectedCount);
        }

        // ==================================================================
        // OnDrag 转发
        // ==================================================================

        [Test]
        public void Drag_OnDragBegan_ForwardsToSelectedFsm_TransitionsToDragging()
        {
            var a = MakeInteractableObject(1);
            MakeCoordinator(new List<InteractableObject> { a });

            _coord.TrySelectObject(a);

            GameEvent.Get<IGestureEvent>().OnDrag(new GestureData
            {
                Phase = GesturePhase.Began,
                ScreenPosition = new Vector2(100, 100)
            });

            Assert.AreEqual(InteractableObjectState.Dragging, a.Fsm.CurrentState);
        }

        [Test]
        public void Drag_OnDragEnded_ForwardsToSelectedFsm_TransitionsToSnapping()
        {
            var a = MakeInteractableObject(1);
            MakeCoordinator(new List<InteractableObject> { a });

            _coord.TrySelectObject(a);
            GameEvent.Get<IGestureEvent>().OnDrag(new GestureData
            {
                Phase = GesturePhase.Began,
                ScreenPosition = new Vector2(100, 100)
            });
            Assert.AreEqual(InteractableObjectState.Dragging, a.Fsm.CurrentState);

            GameEvent.Get<IGestureEvent>().OnDrag(new GestureData
            {
                Phase = GesturePhase.Ended,
                ScreenPosition = new Vector2(100, 100)
            });

            Assert.AreEqual(InteractableObjectState.Snapping, a.Fsm.CurrentState);
        }

        [Test]
        public void Drag_NoSelectedObject_Ignored()
        {
            var a = MakeInteractableObject(1);
            MakeCoordinator(new List<InteractableObject> { a });

            // _selectedObject = null
            GameEvent.Get<IGestureEvent>().OnDrag(new GestureData
            {
                Phase = GesturePhase.Began,
                ScreenPosition = new Vector2(100, 100)
            });

            Assert.AreEqual(InteractableObjectState.Idle, a.Fsm.CurrentState);
        }

        // ==================================================================
        // Init / Dispose 防御
        // ==================================================================

        [Test]
        public void Init_Idempotent_DoubleCallSafe()
        {
            var a = MakeInteractableObject(1);
            MakeCoordinator(new List<InteractableObject> { a });

            _coord.Initialize();   // 第二次
            _coord.Initialize();

            // 走一次正常流程验未坏
            bool ok = _coord.TrySelectObject(a);
            Assert.IsTrue(ok);
            Assert.AreEqual(1, _onObjectSelectedCount, "重复 Init 不应重复注册导致多派");
        }

        [Test]
        public void Shutdown_ClearsListenersAndState()
        {
            var a = MakeInteractableObject(1);
            MakeCoordinator(new List<InteractableObject> { a });

            _coord.TrySelectObject(a);
            _coord.Shutdown();

            // Drag 事件 → Coordinator listener 已 remove，不应转发
            GameEvent.Get<IGestureEvent>().OnDrag(new GestureData
            {
                Phase = GesturePhase.Began,
                ScreenPosition = new Vector2(100, 100)
            });
            Assert.AreEqual(InteractableObjectState.Selected, a.Fsm.CurrentState,
                "Shutdown 后 Coordinator 不再转发 OnDrag — fsm 保持 Selected");
            Assert.IsNull(_coord.CurrentSelectedObject);
        }

        [Test]
        public void Shutdown_Idempotent_DoubleCallSafe()
        {
            MakeCoordinator(new List<InteractableObject>());
            _coord.Shutdown();
            _coord.Shutdown();   // 不应抛
        }

        [Test]
        public void SnapComplete_OnObjectTransformChanged_ClearsStaleSelection_AllowsReTap()
        {
            var a = MakeInteractableObject(1);
            MakeCoordinator(new List<InteractableObject> { a });

            _coord.TrySelectObject(a);
            a.Fsm.OnDragBegan();
            a.Fsm.OnDragEnded();
            Assert.AreEqual(InteractableObjectState.Snapping, a.Fsm.CurrentState);
            Assert.AreSame(a, _coord.CurrentSelectedObject);

            a.Fsm.OnSnapCompleted();
            GameEvent.Get<IInteractionEvent>().OnObjectTransformChanged(
                1, a.transform.position, a.transform.rotation);

            Assert.IsNull(_coord.CurrentSelectedObject, "snap 落定后应释放悬挂选中引用");
            Assert.AreEqual(InteractableObjectState.Idle, a.Fsm.CurrentState);

            _coord.ResetDebounceForTest();
            Assert.IsTrue(_coord.TrySelectObject(a), "同物件应可再次 Tap 选中");
            Assert.AreEqual(InteractableObjectState.Selected, a.Fsm.CurrentState);
        }

        // ==================================================================
        // 协议合规：fsm.OnTapHit / OnDeselect 内部派发，Coordinator 不重复
        // ==================================================================

        [Test]
        public void Protocol_TrySelectObject_DoesNotDispatchEventsItself_RelyOnFsm()
        {
            var a = MakeInteractableObject(1);
            var b = MakeInteractableObject(2);
            MakeCoordinator(new List<InteractableObject> { a, b });

            _coord.TrySelectObject(a);
            Assert.AreEqual(1, _onObjectSelectedCount, "fsm.OnTapHit 派 OnObjectSelected 一次");

            _coord.ResetDebounceForTest();
            _coord.TrySelectObject(b);

            Assert.AreEqual(2, _onObjectSelectedCount, "fsm.OnTapHit(B) 派 OnObjectSelected 第 2 次");
            Assert.AreEqual(1, _onObjectDeselectedCount, "fsm.OnDeselect(A) 派 OnObjectDeselected 一次");
        }
    }
}
