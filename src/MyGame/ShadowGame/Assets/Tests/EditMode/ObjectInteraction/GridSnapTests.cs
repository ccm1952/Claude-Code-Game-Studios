// 该文件由Cursor 自动生成
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using GameLogic;
using TEngine;

namespace ShadowGame.Tests.EditMode.ObjectInteraction
{
    /// <summary>
    /// S2-10 — Grid Snapping System (ADR-013 §"Grid Snap Mechanics" + ADR-027 IInteractionEvent.OnObjectTransformChanged).
    /// Covers AC-1..AC-8 of <c>story-004-grid-snap.md</c>.
    ///
    /// <para>Setup follows S2-09 DragMechanicsTests precedent: GameEvent.EventMgr.Init()
    /// + 手动实例化 IInteractionEvent_Gen 代理；通过 InteractableObject.RegisterPuzzleConfigProvider
    /// 注入 stub PuzzleConfig；MakeInteractableObject 显式调 io.Initialize()（EditMode 中
    /// PlayerLoop 不驱动 MonoBehaviour OnEnable，与 SceneManager.Init/Dispose 同模式 — X1 patch v3 决议）。</para>
    ///
    /// <para><b>DOTween 测试边界（EditMode 限制）</b>：DOTween 在 EditMode 不自动 tick；
    /// 强制走完 tween 用 <see cref="TransformDOTweenSnapHelpers.DOCompleteSafe"/>（reflection 调
    /// <c>DG.Tweening.ShortcutExtensions.DOComplete(Component, bool)</c>）。已在格点路径
    /// 同步走 OnComplete callback 不依赖 DOTween 推进 — AC-3/AC-4 主路径。Ease 类型 + 精确 duration
    /// 留 PlayMode/集成验证（EditMode 仅验最终位置 + 回调时序 + 派发计数）。</para>
    /// </summary>
    [TestFixture]
    public class GridSnapTests
    {
        private const int TestObjectId = 42;
        private const int TestPuzzleId = 1;

        // Camera 桩参数（同 DragMechanicsTests，保证 ScreenToWorldPoint 计算等价 — S2-10 本身不依赖
        // ScreenToWorldPoint，但 InteractableObject.Initialize 校验 _gameplayCamera != null 走 fail-loud
        // Log.Error；不注入 camera 会让 SetUp 阶段产 ERROR log 触发 Test Runner 视为失败）
        private const int CamPixelW = 1024;
        private const int CamPixelH = 1024;
        private const float CamOrthoSize = 5f;

        private GameObject _cameraGo;
        private Camera _camera;
        private GameObject _ioGo;
        private InteractableObject _io;
        private PuzzleConfig _stubConfig;

        // OnObjectTransformChanged 计数器
        private int _transformChangedCount;
        private int _lastTransformObjectId;
        private Vector3 _lastTransformPos;

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

            _transformChangedCount = 0;
            _lastTransformObjectId = -1;
            _lastTransformPos = Vector3.zero;
            GameEvent.AddEventListener<int, Vector3, Quaternion>(
                IInteractionEvent_Event.OnObjectTransformChanged, OnTransformChangedListener);

            _ioGo = MakeInteractableObject(out _io, objectId: TestObjectId, puzzleId: TestPuzzleId);
        }

        [TearDown]
        public void TearDown()
        {
            GameEvent.RemoveEventListener<int, Vector3, Quaternion>(
                IInteractionEvent_Event.OnObjectTransformChanged, OnTransformChangedListener);
            if (_io != null)
            {
                _io.transform.DOKillSafe();
                _io.Shutdown();
            }
            if (_ioGo != null) Object.DestroyImmediate(_ioGo);
            if (_cameraGo != null) Object.DestroyImmediate(_cameraGo);
            InteractableObject.ClearPuzzleConfigProviderForTest();
        }

        private void OnTransformChangedListener(int objId, Vector3 pos, Quaternion rot)
        {
            _transformChangedCount++;
            _lastTransformObjectId = objId;
            _lastTransformPos = pos;
        }

        // ────────── 工厂 ──────────

        private GameObject MakeInteractableObject(out InteractableObject io, int objectId, int puzzleId)
        {
            var go = new GameObject($"test-io#{objectId}");
            go.SetActive(false);
            io = go.AddComponent<InteractableObject>();
            SetPrivateField(io, "_objectId", objectId);
            SetPrivateField(io, "_puzzleId", puzzleId);
            SetPrivateField(io, "_gameplayCamera", _camera);   // ← 不注入会触发 fail-loud Log.Error → SetUp 失败
            SetPrivateField(io, "_dragDepth", 10f);
            go.SetActive(true);
            io.Initialize();   // 显式触发 — EditMode 中 PlayerLoop 不驱动 OnEnable
            return go;
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, $"reflection field {name} not found on {target.GetType()}");
            f.SetValue(target, value);
        }

        // 进 Snapping 状态：fsm 路径 Idle → Selected → Dragging → Snapping
        private static void EnterSnapping(InteractableObjectFsm fsm)
        {
            fsm.OnTapHit();      // Idle → Selected
            fsm.OnDragBegan();   // Selected → Dragging
            fsm.OnDragEnded();   // Dragging → Snapping
        }

        // ==================================================================
        // AC-1: snap 公式正确（round to nearest grid point）
        // ==================================================================

        [Test]
        public void AC1_StartSnap_OffGrid_DOCompletes_LandsOnNearestGridPoint()
        {
            _io.transform.position = new Vector3(1.4f, 2.7f, 0f);
            EnterSnapping(_io.Fsm);

            _io.StartSnap();   // tween 启动（不在格点；DOMove 创建）

            // EditMode DOTween 不自动 tick — 强制走完 tween + 触发 OnComplete callback
            _io.transform.DOCompleteSafe();

            Assert.AreEqual(1.0f, _io.transform.position.x, 0.001f, "1.4 → 1.0 (round)");
            Assert.AreEqual(3.0f, _io.transform.position.y, 0.001f, "2.7 → 3.0 (round)");
            Assert.AreEqual(InteractableObjectState.Idle, _io.Fsm.CurrentState,
                "OnComplete 调 fsm.OnSnapCompleted → Idle");
        }

        [Test]
        public void AC1_StartSnap_NegativeCoords_RoundsCorrectly()
        {
            _io.transform.position = new Vector3(-0.6f, -1.4f, 0f);
            EnterSnapping(_io.Fsm);

            _io.StartSnap();
            _io.transform.DOCompleteSafe();

            Assert.AreEqual(-1.0f, _io.transform.position.x, 0.001f, "-0.6 → -1.0");
            Assert.AreEqual(-1.0f, _io.transform.position.y, 0.001f, "-1.4 → -1.0");
        }

        // ==================================================================
        // AC-3: OnObjectTransformChanged 在 OnComplete 后派发（验中包含最终 pos）
        // ==================================================================

        [Test]
        public void AC3_OnSnapComplete_EmitsObjectTransformChanged_OnceWithFinalPos()
        {
            _io.transform.position = new Vector3(2.3f, 0f, 0f);
            EnterSnapping(_io.Fsm);

            _io.StartSnap();
            _io.transform.DOCompleteSafe();

            Assert.AreEqual(1, _transformChangedCount, "OnObjectTransformChanged 应派发 1 次");
            Assert.AreEqual(TestObjectId, _lastTransformObjectId);
            Assert.AreEqual(2.0f, _lastTransformPos.x, 0.001f, "事件携带 snapped 位置");
            Assert.AreEqual(0f, _lastTransformPos.y, 0.001f);
        }

        [Test]
        public void AC3_StartSnap_DoesNotEmitEvent_BeforeOnComplete()
        {
            _io.transform.position = new Vector3(1.4f, 2.7f, 0f);
            EnterSnapping(_io.Fsm);

            _io.StartSnap();
            // 注意：不 DOComplete — tween 仍在进行

            Assert.AreEqual(0, _transformChangedCount,
                "tween 启动后但未 OnComplete 时**不**应派发 OnObjectTransformChanged");
            Assert.AreEqual(InteractableObjectState.Snapping, _io.Fsm.CurrentState,
                "fsm 仍在 Snapping，未 OnSnapCompleted");
        }

        // ==================================================================
        // AC-4: 已在格点（距离 < epsilon）→ 跳 tween，同步走 OnComplete 路径
        // ==================================================================

        [Test]
        public void AC4_StartSnap_AlreadyAtGrid_SkipsTween_SyncCompletes()
        {
            _io.transform.position = new Vector3(1.0f, 2.0f, 0f);   // 恰好在格点
            EnterSnapping(_io.Fsm);

            _io.StartSnap();
            // 不调 DOCompleteSafe — 应已经同步完成

            Assert.AreEqual(InteractableObjectState.Idle, _io.Fsm.CurrentState,
                "已在格点 → 跳 tween 同步 OnComplete → fsm 应已转 Idle");
            Assert.AreEqual(1, _transformChangedCount,
                "已在格点 → OnObjectTransformChanged 应仍派发 1 次");
            Assert.AreEqual(1.0f, _io.transform.position.x, 0.001f);
            Assert.AreEqual(2.0f, _io.transform.position.y, 0.001f);
        }

        [Test]
        public void AC4_StartSnap_FloatingPointTolerance_TreatedAsAtGrid()
        {
            // 浮点容差路径：(1.0001, 2.0) — round 公式得 (1.0, 2.0)，距离 0.0001 < epsilon 0.001
            _io.transform.position = new Vector3(1.0001f, 2.0f, 0f);
            EnterSnapping(_io.Fsm);

            _io.StartSnap();

            Assert.AreEqual(InteractableObjectState.Idle, _io.Fsm.CurrentState,
                "浮点小偏差 < epsilon → 仍走『已在格点』路径，无 DOMove");
        }

        // ==================================================================
        // AC-5: snap 后置 clamp（snap 计算可能算到 bounds 外格点 → clamp 回 bounds 内）
        // ==================================================================

        [Test]
        public void AC5_StartSnap_OffsetBeyondBounds_ClampsAfterSnap()
        {
            // bounds.MaxX = 3, gridSize = 1。raw=3.6 → snap 计算 = round(3.6) = 4.0 → clamp = 3.0
            _io.transform.position = new Vector3(3.6f, 0f, 0f);
            EnterSnapping(_io.Fsm);

            _io.StartSnap();
            _io.transform.DOCompleteSafe();

            Assert.AreEqual(3.0f, _io.transform.position.x, 0.001f,
                "raw=3.6 → snap 4.0 → 后置 clamp 到 MaxX=3.0");
            Assert.AreEqual(0f, _io.transform.position.y, 0.001f);
        }

        // ==================================================================
        // AC-6: OnTapHit 中断 snap（fsm 转换规则 6：Snapping → Selected；DOKill；不派事件）
        // ==================================================================

        [Test]
        public void AC6_OnTapHit_DuringSnap_DOKillsTween_NoEventDispatched()
        {
            _io.transform.position = new Vector3(1.4f, 2.7f, 0f);
            EnterSnapping(_io.Fsm);

            _io.StartSnap();   // tween 启动（非格点）
            var posMidway = _io.transform.position;   // tween 未推进，仍在 (1.4, 2.7)

            _io.Fsm.OnTapHit();   // Snapping → Selected → OnFsmStateChanged 触发 DOKill(false)

            Assert.AreEqual(InteractableObjectState.Selected, _io.Fsm.CurrentState);
            Assert.AreEqual(posMidway.x, _io.transform.position.x, 0.001f,
                "中断时 transform 停在中间帧位置（DOKill complete:false）");
            Assert.AreEqual(posMidway.y, _io.transform.position.y, 0.001f);
            Assert.AreEqual(0, _transformChangedCount,
                "中断路径**不**派发 OnObjectTransformChanged（落点未定）");
        }

        // ==================================================================
        // AC-7: Lock 中断 snap（fsm 转换规则 7：Snapping → Locked；DOKill；不派事件）
        // ==================================================================

        [Test]
        public void AC7_OnLockChanged_DuringSnap_DOKillsTween_NoEventDispatched()
        {
            _io.transform.position = new Vector3(1.4f, 2.7f, 0f);
            EnterSnapping(_io.Fsm);

            _io.StartSnap();
            var posMidway = _io.transform.position;

            _io.Fsm.OnLockChanged(true);   // Snapping → Locked

            Assert.AreEqual(InteractableObjectState.Locked, _io.Fsm.CurrentState);
            Assert.AreEqual(posMidway.x, _io.transform.position.x, 0.001f);
            Assert.AreEqual(posMidway.y, _io.transform.position.y, 0.001f);
            Assert.AreEqual(0, _transformChangedCount,
                "Lock 中断**不**派发 OnObjectTransformChanged（落点未定）");
        }

        // ==================================================================
        // AC-8: 协议合规 — IInteractionEvent.OnObjectTransformChanged 通过接口分组事件派发
        //         （0 ADR-006 EventId.Evt_ObjectTransformChanged 残留 — grep evidence 文档支撑）
        // ==================================================================

        [Test]
        public void AC8_IInteractionEvent_OnObjectTransformChanged_HasCorrectSignature()
        {
            // 反射验证 IInteractionEvent.OnObjectTransformChanged 签名（AC-8 协议合规配套）
            var method = typeof(IInteractionEvent).GetMethod("OnObjectTransformChanged");
            Assert.IsNotNull(method, "IInteractionEvent.OnObjectTransformChanged 必须存在");

            var ps = method.GetParameters();
            Assert.AreEqual(3, ps.Length, "签名应是 (int, Vector3, Quaternion)");
            Assert.AreEqual(typeof(int), ps[0].ParameterType);
            Assert.AreEqual(typeof(Vector3), ps[1].ParameterType);
            Assert.AreEqual(typeof(Quaternion), ps[2].ParameterType);

            // [EventInterface] 验证（继承自 S2-09 同款套路 — 接口本身已在 GroupLogic）
            var attr = typeof(IInteractionEvent).GetCustomAttribute<EventInterfaceAttribute>();
            Assert.IsNotNull(attr, "IInteractionEvent 必须以 [EventInterface] 标注（ADR-027）");
            Assert.AreEqual(EEventGroup.GroupLogic, attr.EventGroup);
        }

        // ==================================================================
        // 协调点（X1 patch v2 wired）— IsReboundActive == true 时 Update 推迟 StartSnap
        // ==================================================================

        [Test]
        public void Coordination_IsReboundActive_DefersStartSnap_UntilFlagCleared()
        {
            _io.transform.position = new Vector3(1.4f, 2.7f, 0f);
            EnterSnapping(_io.Fsm);

            // 模拟 race-fallback rebound 进行中（用反射置 IsReboundActive 私有 setter — IsReboundActive 是 public getter / private setter）
            var prop = typeof(InteractableObject).GetProperty(
                "IsReboundActive", BindingFlags.Instance | BindingFlags.Public);
            prop.SetValue(_io, true);

            // 调 Update 路由（反射调 private Update 模拟 PlayerLoop 一帧）
            InvokeUpdate(_io);

            Assert.AreEqual(InteractableObjectState.Snapping, _io.Fsm.CurrentState,
                "IsReboundActive == true → Update 中 Snapping case break；StartSnap 不调；fsm 仍 Snapping");
            Assert.IsFalse(GetDidStartSnap(_io),
                "_didStartSnap 应仍为 false（StartSnap 未调）");

            // 清 flag — 模拟 rebound OnComplete
            prop.SetValue(_io, false);

            // 调 Update — 此时 StartSnap 应启动
            InvokeUpdate(_io);

            Assert.IsTrue(GetDidStartSnap(_io),
                "rebound flag 清后下一帧 Update → StartSnap 启动 _didStartSnap = true");
        }

        private static void InvokeUpdate(InteractableObject io)
        {
            var m = typeof(InteractableObject).GetMethod(
                "Update", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(m, "private Update method 应存在");
            m.Invoke(io, null);
        }

        private static bool GetDidStartSnap(InteractableObject io)
        {
            var f = typeof(InteractableObject).GetField(
                "_didStartSnap", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, "_didStartSnap field 应存在");
            return (bool)f.GetValue(io);
        }
    }

    /// <summary>
    /// 测试 helper：DOTween reflection 包装器（避免单测引入 DG.Tweening using 依赖；与 DragMechanicsTests
    /// <c>TransformDOTweenTestHelpers</c> 同套路；额外加 <see cref="DOCompleteSafe"/> 用于 EditMode 强制
    /// 走完 DOTween tween + 触发 OnComplete callback）。
    /// <para>注：本类与 DragMechanicsTests 的 helper 异名（GridSnap 后缀）以避免重复定义；如未来抽公共
    /// 测试 helper 模块再合并。</para>
    /// </summary>
    internal static class TransformDOTweenSnapHelpers
    {
        public static void DOCompleteSafe(this Transform t)
        {
            if (t == null) return;
            try
            {
                System.Type shortcuts = null;
                foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    shortcuts = a.GetType("DG.Tweening.ShortcutExtensions");
                    if (shortcuts != null) break;
                }
                if (shortcuts == null) return;
                // DOComplete(this Component target, bool withCallbacks=true) → int (num completed)
                var m = shortcuts.GetMethod("DOComplete", new[] { typeof(Component), typeof(bool) });
                m?.Invoke(null, new object[] { t, true });
            }
            catch { /* silent — 测试 cleanup 容忍 */ }
        }
    }
}
