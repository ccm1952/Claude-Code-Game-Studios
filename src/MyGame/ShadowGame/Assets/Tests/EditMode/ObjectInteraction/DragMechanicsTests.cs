// 该文件由Cursor 自动生成
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
    /// S2-09 — Touch-to-World Drag with Boundary Clamping (ADR-013 + ADR-027).
    /// Covers AC-1..AC-8 of story-002-drag-mechanics.md.
    ///
    /// <para>Setup follows S2-08 InteractableObjectFsmTests precedent: reset
    /// <c>GameEvent.EventMgr</c> 并手动实例化 <c>IInteractionEvent_Gen</c> + <c>IGestureEvent_Gen</c>
    /// 代理（绕过 EditModeTests.asmdef 中空的 GameEventHelper）。</para>
    ///
    /// <para><b>Camera 桩</b>：orthographic + 1024×1024 pixelRect + size=5 + aspect=1 → 屏幕中心 (512, 512) 对应 world (0, 0)，
    /// 屏幕右中 (768, 512) 对应 world (2.5, 0)（线性映射，便于断言）。</para>
    ///
    /// <para><b>Rebound 测试边界</b>：DOTween 在 EditMode 不自动 tick；测试只验 <see cref="InteractableObject.IsReboundActive"/>
    /// 翻转 (启动) — 不验 OnComplete 自动收敛 (留给 PlayMode / 用户手动 Run)。</para>
    ///
    /// <para><b>Lifecycle 注入（X1 patch v3）</b>：EditMode 测试中 Unity PlayerLoop 不驱动
    /// MonoBehaviour <c>OnEnable</c> / <c>OnDisable</c>，故 <see cref="MakeInteractableObject"/> 显式调
    /// <see cref="InteractableObject.Initialize"/>，<c>AC8_OnDisable</c> 显式调 <see cref="InteractableObject.Shutdown"/>。</para>
    /// </summary>
    [TestFixture]
    public class DragMechanicsTests
    {
        private const int TestObjectId = 42;
        private const int TestPuzzleId = 1;

        // Camera 桩参数（与上文断言匹配）
        private const int CamPixelW = 1024;
        private const int CamPixelH = 1024;
        private const float CamOrthoSize = 5f;

        private GameObject _cameraGo;
        private Camera _camera;
        private GameObject _ioGo;
        private InteractableObject _io;
        private PuzzleConfig _stubConfig;

        // ────────── Fixture lifecycle ──────────

        [SetUp]
        public void SetUp()
        {
            GameEvent.EventMgr.Init();
            _ = new IInteractionEvent_Gen(GameEvent.EventMgr.GetDispatcher());
            _ = new IGestureEvent_Gen(GameEvent.EventMgr.GetDispatcher());

            _stubConfig = new PuzzleConfig(
                id: TestPuzzleId,
                interactionBounds: new InteractionBounds(-3f, 3f, -3f, 3f),
                gridSize: 1f,
                snapSpeed: 0.2f);
            InteractableObject.RegisterPuzzleConfigProvider(_ => _stubConfig);

            _cameraGo = new GameObject("test-cam");
            _camera = _cameraGo.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = CamOrthoSize;
            _camera.aspect = (float)CamPixelW / CamPixelH;
            _camera.pixelRect = new Rect(0, 0, CamPixelW, CamPixelH);
            _cameraGo.transform.position = new Vector3(0f, 0f, -10f);

            _ioGo = MakeInteractableObject(out _io, objectId: TestObjectId, puzzleId: TestPuzzleId);
        }

        [TearDown]
        public void TearDown()
        {
            if (_io != null) _io.transform.DOKillSafe();
            if (_ioGo != null) Object.DestroyImmediate(_ioGo);
            if (_cameraGo != null) Object.DestroyImmediate(_cameraGo);

            InteractableObject.ClearPuzzleConfigProviderForTest();
        }

        // ====== 工厂 ======

        private GameObject MakeInteractableObject(out InteractableObject io, int objectId, int puzzleId)
        {
            // 注：EditMode 测试不走 Unity PlayerLoop，OnEnable 不会自动触发 — 故先用 SetActive(false)
            // 注入 [SerializeField] fields，再显式调 io.Initialize() 模拟 PlayMode 的 OnEnable 时序。
            var go = new GameObject($"test-io#{objectId}");
            go.SetActive(false);
            io = go.AddComponent<InteractableObject>();
            SetPrivateField(io, "_objectId", objectId);
            SetPrivateField(io, "_puzzleId", puzzleId);
            SetPrivateField(io, "_gameplayCamera", _camera);
            SetPrivateField(io, "_dragDepth", 10f);
            go.SetActive(true);
            io.Initialize();   // ← 关键：显式触发，绕开 EditMode 中 OnEnable 不自动调用的限制
            return go;
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, $"reflection field {name} not found on {target.GetType()}");
            f.SetValue(target, value);
        }

        private static void DispatchOnDrag(Vector2 screenPos, GesturePhase phase)
        {
            var data = new GestureData
            {
                Type = GestureType.Drag,
                Phase = phase,
                ScreenPosition = screenPos,
            };
            GameEvent.Get<IGestureEvent>().OnDrag(data);
        }

        private static void EnterDragging(InteractableObjectFsm fsm)
        {
            fsm.OnTapHit();        // Idle → Selected
            fsm.OnDragBegan();     // Selected → Dragging
        }

        // ==================================================================
        // AC-1: Drag position 1:1 跟手指（Camera.ScreenToWorldPoint 验证）
        // ==================================================================

        [Test]
        public void AC1_TickDrag_FollowsFinger_ViaScreenToWorldPoint()
        {
            EnterDragging(_io.Fsm);

            var screenPos = new Vector2(768f, 512f);   // 中右；orthographic → world (2.5, 0, 0)
            DispatchOnDrag(screenPos, GesturePhase.Updated);

            _io.TickDrag();

            var expectedWorld = _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
            Assert.AreEqual(expectedWorld.x, _io.transform.position.x, 0.01f,
                "transform.x 必须与 Camera.ScreenToWorldPoint(screenPos.x) 在 0.01 误差内一致。");
            Assert.AreEqual(expectedWorld.y, _io.transform.position.y, 0.01f,
                "transform.y 同理。");
        }

        // ==================================================================
        // AC-2: 边界 clamp（X / Y 独立）
        // ==================================================================

        [Test]
        public void AC2_TickDrag_ClampsToBounds_WhenFingerOutOfBounds()
        {
            EnterDragging(_io.Fsm);

            // 屏幕极右 → world ~+5 (orthoSize=5)；bounds.MaxX=3 → 应 clamp 到 3
            DispatchOnDrag(new Vector2(1024f, 512f), GesturePhase.Updated);
            _io.TickDrag();

            Assert.AreEqual(_stubConfig.InteractionBounds.MaxX, _io.transform.position.x, 0.01f,
                "X 超界 → clamp 到 MaxX。");
            Assert.AreEqual(0f, _io.transform.position.y, 0.01f, "Y 在中心 → 不变。");
        }

        [Test]
        public void AC2_TickDrag_ClampsXAndYIndependently_WhenCornerOut()
        {
            EnterDragging(_io.Fsm);

            // 屏幕右下角外 → world (~+5, ~-5)；bounds [-3,3]² → clamp 到 (3, -3)
            DispatchOnDrag(new Vector2(1024f, 0f), GesturePhase.Updated);
            _io.TickDrag();

            Assert.AreEqual(3f, _io.transform.position.x, 0.01f);
            Assert.AreEqual(-3f, _io.transform.position.y, 0.01f);
        }

        // ==================================================================
        // AC-3: Race-fallback rebound 启动条件
        // ==================================================================

        [Test]
        public void AC3_Rebound_ActivatesOnDraggingToSnapping_WhenFingerOutOfBoundsAndTransformInside()
        {
            EnterDragging(_io.Fsm);

            // 模拟 race：finger 在 bounds 外 (4.5, 0, 0)，但 transform 还卡在 bounds 内非边界点 (2, 0, 0)
            _io.transform.position = new Vector3(2f, 0f, 0f);
            _io.SetLastWorldPosUnclampedForTest(new Vector3(4.5f, 0f, 0f));

            _io.Fsm.OnDragEnded();   // Dragging → Snapping

            Assert.AreEqual(InteractableObjectState.Snapping, _io.Fsm.CurrentState);
            Assert.IsTrue(_io.IsReboundActive,
                "Race-fallback rebound 必须启动：finger 在 bounds 外且 transform 距 clampedFingerPos > epsilon。");
        }

        [Test]
        public void AC3_Rebound_DoesNotActivate_WhenTransformAlreadyAtClampedFingerPos_MainPath()
        {
            EnterDragging(_io.Fsm);

            // 生产主路径：每帧 clamp 已让 transform 在 bounds 边界 (3, 0)；finger 在外 (5, 0)
            _io.transform.position = new Vector3(3f, 0f, 0f);
            _io.SetLastWorldPosUnclampedForTest(new Vector3(5f, 0f, 0f));

            _io.Fsm.OnDragEnded();

            Assert.IsFalse(_io.IsReboundActive,
                "生产主路径：transform 已在 clampedFingerPos 上（距离 < epsilon）→ 跳过 DOMove。");
        }

        [Test]
        public void AC3_Rebound_DoesNotActivate_WhenFingerInBounds()
        {
            EnterDragging(_io.Fsm);

            _io.transform.position = new Vector3(0f, 0f, 0f);
            _io.SetLastWorldPosUnclampedForTest(new Vector3(1f, 1f, 0f));   // 在 bounds 内

            _io.Fsm.OnDragEnded();

            Assert.IsFalse(_io.IsReboundActive,
                "release 时 finger 在 bounds 内 → 不需 rebound。");
        }

        // ==================================================================
        // AC-4: Locked 状态 OnDrag 不响应（双层守卫：handler + TickDrag）
        // ==================================================================

        [Test]
        public void AC4_OnDrag_IsNoOp_WhenFsmIsLocked()
        {
            _io.Fsm.OnLockChanged(true);   // → Locked

            var posBefore = _io.transform.position;
            DispatchOnDrag(new Vector2(800f, 800f), GesturePhase.Updated);
            _io.TickDrag();

            Assert.AreEqual(posBefore, _io.transform.position,
                "Locked 状态下 OnDrag 早期 return；transform 不变。");
        }

        [Test]
        public void AC4_OnDrag_IsNoOp_WhenFsmIsIdle()
        {
            // 默认 Idle；不 EnterDragging
            var posBefore = _io.transform.position;
            DispatchOnDrag(new Vector2(800f, 800f), GesturePhase.Updated);
            _io.TickDrag();

            Assert.AreEqual(posBefore, _io.transform.position,
                "Idle 状态下 OnDrag handler 早期 return；transform 不变。");
        }

        // ==================================================================
        // AC-5: PuzzleConfigProvider 注入 + fail-loud（Log.Error；不 disable component，
        //         Fsm 保持非 null；TickDrag 因 _puzzleConfig 为 null 早期 return 等效"drag 不工作"）
        // ==================================================================

        [Test]
        public void AC5_OnEnable_LogsError_WhenProviderNotRegistered()
        {
            InteractableObject.ClearPuzzleConfigProviderForTest();

            LogAssert.Expect(LogType.Error,
                new Regex(@"\[InteractableObject#7\] PuzzleConfigProvider 未注册"));

            var go = MakeInteractableObject(out var io, objectId: 7, puzzleId: 99);

            Assert.IsNotNull(io.Fsm, "fail-loud 仅 Log.Error，不影响 Fsm 初始化（Fsm 仍 non-null）。");
            Assert.IsNull(GetPrivateField<PuzzleConfig>(io, "_puzzleConfig"),
                "_puzzleConfig 仍为 null（fail-loud 实际生效；TickDrag 早期 return 等效 drag 不可用）。");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void AC5_OnEnable_LogsError_WhenProviderReturnsNull()
        {
            InteractableObject.RegisterPuzzleConfigProvider(_ => null);

            LogAssert.Expect(LogType.Error,
                new Regex(@"\[InteractableObject#8\] PuzzleConfigProvider 对 puzzleId=99 返回 null"));

            var go = MakeInteractableObject(out var io, objectId: 8, puzzleId: 99);

            Assert.IsNotNull(io.Fsm);
            Assert.IsNull(GetPrivateField<PuzzleConfig>(io, "_puzzleConfig"));

            Object.DestroyImmediate(go);
        }

        private static T GetPrivateField<T>(object target, string name) where T : class
        {
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, $"reflection field {name} not found on {target.GetType()}");
            return f.GetValue(target) as T;
        }

        // ==================================================================
        // AC-6: 协议合规 — 反射验证 IGestureEvent 注册
        // ==================================================================

        [Test]
        public void AC6_IGestureEvent_HasEventInterfaceAttribute_GroupLogic()
        {
            var attr = typeof(IGestureEvent).GetCustomAttribute<EventInterfaceAttribute>();

            Assert.IsNotNull(attr,
                "IGestureEvent 必须以 [EventInterface] 标注（ADR-027）。");
            Assert.AreEqual(EEventGroup.GroupLogic, attr.EventGroup,
                "IGestureEvent 属于 GroupLogic（手势属于业务广播）。");
        }

        // ==================================================================
        // AC-7: Began phase 也缓存 ScreenPos（drag 起手时立即跟随）
        // ==================================================================

        [Test]
        public void AC7_OnDrag_CachesScreenPos_OnBeganPhase()
        {
            EnterDragging(_io.Fsm);

            DispatchOnDrag(new Vector2(640f, 512f), GesturePhase.Began);
            _io.TickDrag();

            Assert.AreNotEqual(Vector3.zero, _io.transform.position,
                "Began phase 也应缓存 ScreenPos；TickDrag 写入 transform。");
        }

        [Test]
        public void AC7_OnDrag_DoesNotCacheScreenPos_OnEndedOrCancelledPhase()
        {
            EnterDragging(_io.Fsm);

            // 先建立 baseline
            DispatchOnDrag(new Vector2(768f, 512f), GesturePhase.Updated);
            _io.TickDrag();
            var posAfterUpdated = _io.transform.position;

            // Ended phase 不应再缓存 ScreenPos（fsm 仍在 Dragging — Coordinator 通常先调 fsm.OnDragEnded 转 Snapping，
            // 但即使 phase=Ended 而 fsm 仍 Dragging，本 handler 的设计是只接受 Began/Updated）
            DispatchOnDrag(new Vector2(0f, 0f), GesturePhase.Ended);
            _io.TickDrag();

            Assert.AreEqual(posAfterUpdated, _io.transform.position,
                "Ended phase 应被 handler 早期 return；transform 不再变化。");
        }

        // ==================================================================
        // AC-8: 多实例隔离 + listener 反注册
        // ==================================================================

        [Test]
        public void AC8_MultiInstance_OnlyDraggingInstanceConsumesOnDrag()
        {
            var goB = MakeInteractableObject(out var ioB, objectId: 99, puzzleId: TestPuzzleId);

            EnterDragging(_io.Fsm);
            // ioB 保持 Idle

            DispatchOnDrag(new Vector2(768f, 512f), GesturePhase.Updated);
            _io.TickDrag();
            ioB.TickDrag();

            Assert.AreNotEqual(Vector3.zero, _io.transform.position, "A (Dragging) 应消费。");
            Assert.AreEqual(Vector3.zero, ioB.transform.position, "B (Idle) 应被 handler 早期 return。");

            Object.DestroyImmediate(goB);
        }

        [Test]
        public void AC8_Shutdown_RemovesListener_DispatchAfterShutdownHasNoEffect()
        {
            EnterDragging(_io.Fsm);

            _io.Shutdown();   // 显式触发反注册（EditMode 中 SetActive(false) 不调 OnDisable）
            Assert.IsNull(_io.Fsm, "Shutdown 后 Fsm 应为 null（验证 listener 反注册路径已执行）。");

            var posBefore = _io.transform.position;

            // 派发一次；不应触发任何 listener
            // （正面验证：不抛异常 + 不影响 _ioGo transform.position 即可）
            DispatchOnDrag(new Vector2(768f, 512f), GesturePhase.Updated);

            Assert.AreEqual(posBefore, _io.transform.position,
                "Shutdown 后 dispatch OnDrag 不应再写入 transform（listener 已反注册）。");

            _io = null;   // TearDown 仍 DestroyImmediate _ioGo
        }
    }

    /// <summary>
    /// 测试 helper：DOTween DOKill 包装器，避免单测引入 DG.Tweening using 依赖。
    /// </summary>
    internal static class TransformDOTweenTestHelpers
    {
        /// <summary>
        /// Safe DOKill — 通过反射调用 DOTween 的 DOKill 扩展（避免在 EditModeTests.asmdef 显式 reference DOTween dll）。
        /// 如果 DOTween 未加载或方法找不到，silent skip（测试 cleanup 容忍）。
        /// </summary>
        public static void DOKillSafe(this Transform t)
        {
            if (t == null) return;
            try
            {
                var asm = System.AppDomain.CurrentDomain.GetAssemblies();
                System.Type shortcuts = null;
                foreach (var a in asm)
                {
                    shortcuts = a.GetType("DG.Tweening.ShortcutExtensions");
                    if (shortcuts != null) break;
                }
                if (shortcuts == null) return;
                var m = shortcuts.GetMethod("DOKill", new[] { typeof(Component), typeof(bool) });
                m?.Invoke(null, new object[] { t, false });
            }
            catch { /* silent — 测试 cleanup 容忍 */ }
        }
    }
}
