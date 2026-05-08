// 该文件由Cursor 自动生成
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GameLogic;
using TEngine;

namespace ShadowGame.Tests.EditMode.ObjectInteraction
{
    /// <summary>
    /// S2-11 — Rotation Mechanics（HandleRotate + SnapRotation + Coordinator 转发）。
    /// 覆盖 AC-1..AC-7 of <c>story-003-rotation-mechanics.md</c>。
    ///
    /// <para><b>Fixture 模式</b>：MonoBehaviour 测试，须 GameObject + AddComponent + 显式 Initialize
    /// （EditMode 不调 OnEnable，参 conventions.md "测试 fixture 规范" R1）。复用 GridSnapTests 的
    /// <c>TransformDOTweenSnapHelpers.DOCompleteSafe</c> 强制走完 DOTween tween + 触发 OnComplete callback
    /// （EditMode 不自动 tick）。Camera + PuzzleConfigProvider + Listener 计数全 boilerplate。</para>
    ///
    /// <para><b>EditMode 不覆盖</b>：DOTween 精确 duration / Ease 类型曲线 — 留 PlayMode/集成验证（与 S2-09
    /// EaseOutBack / S2-10 EaseOutQuad 同 ADVISORY 处理）。</para>
    /// </summary>
    [TestFixture]
    public class RotationMechanicsTests
    {
        private const int TestObjectId = 42;
        private const int TestPuzzleId = 1;
        private const int CamPixelW = 1024;
        private const int CamPixelH = 1024;

        private GameObject _ioGo;
        private InteractableObject _io;
        private GameObject _cameraGo;
        private Camera _camera;

        // OnObjectTransformChanged listener 计数
        private int _transformChangedCount;
        private int _lastTransformObjectId;
        private Quaternion _lastTransformRotation;
        private PuzzleConfig _stubConfig;

        // ────────── Fixture lifecycle ──────────

        [SetUp]
        public void SetUp()
        {
            GameEvent.EventMgr.Init();
            _ = new IInteractionEvent_Gen(GameEvent.EventMgr.GetDispatcher());
            _ = new IGestureEvent_Gen(GameEvent.EventMgr.GetDispatcher());

            _transformChangedCount = 0;
            _lastTransformObjectId = -1;
            _lastTransformRotation = Quaternion.identity;
            GameEvent.AddEventListener<int, Vector3, Quaternion>(
                IInteractionEvent_Event.OnObjectTransformChanged, OnTransformChangedListener);

            _stubConfig = new PuzzleConfig(
                id: TestPuzzleId,
                interactionBounds: new InteractionBounds(-10f, 10f, -10f, 10f),
                gridSize: 1f,
                snapSpeed: 0.2f,
                rotationStep: 15f);
            InteractableObject.RegisterPuzzleConfigProvider(_ => _stubConfig);

            // Stub Camera
            _cameraGo = new GameObject("StubCamera");
            _camera = _cameraGo.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 5f;
            _camera.transform.position = new Vector3(0, 0, -10f);
            _camera.pixelRect = new Rect(0, 0, CamPixelW, CamPixelH);

            // InteractableObject
            _ioGo = new GameObject("InteractableObject#42");
            _io = _ioGo.AddComponent<InteractableObject>();
            SetPrivateField(_io, "_objectId", TestObjectId);
            SetPrivateField(_io, "_puzzleId", TestPuzzleId);
            SetPrivateField(_io, "_gameplayCamera", _camera);
            _io.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            GameEvent.RemoveEventListener<int, Vector3, Quaternion>(
                IInteractionEvent_Event.OnObjectTransformChanged, OnTransformChangedListener);

            if (_io != null)
            {
                _io.transform.DOCompleteSafe();   // 收尾未完成 tween
                _io.Shutdown();
            }
            if (_ioGo != null) Object.DestroyImmediate(_ioGo);
            if (_cameraGo != null) Object.DestroyImmediate(_cameraGo);

            _io = null;
            _ioGo = null;
            _camera = null;
            _cameraGo = null;

            InteractableObject.ClearPuzzleConfigProviderForTest();
        }

        private void OnTransformChangedListener(int objectId, Vector3 pos, Quaternion rot)
        {
            _transformChangedCount++;
            _lastTransformObjectId = objectId;
            _lastTransformRotation = rot;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(field, $"未找到私有字段 {fieldName}");
            field.SetValue(target, value);
        }

        /// <summary>fsm.OnTapHit() 转 Idle→Selected（Rotation 子模式守卫前置条件）。</summary>
        private void EnterSelected()
        {
            _io.Fsm.OnTapHit();
            Assert.AreEqual(InteractableObjectState.Selected, _io.Fsm.CurrentState);
        }

        // ==================================================================
        // AC-1: 持续旋转自由跟手指（Began / Updated phase）
        // ==================================================================

        [Test]
        public void AC1_HandleRotate_BeganPhase_LoadsFromTransform_AccumulatesDelta()
        {
            EnterSelected();
            _io.transform.eulerAngles = new Vector3(0, 0, 30f);   // 预设 30°

            // delta = 20° = 20 * Mathf.Deg2Rad rad
            _io.HandleRotate(new GestureData
            {
                Phase = GesturePhase.Began,
                AngleDelta = 20f * Mathf.Deg2Rad
            });

            Assert.AreEqual(50f, _io.AccumulatedAngleDeg, 0.01f, "Began 从 transform.z=30° 加载 + delta 20° = 50°");
            Assert.AreEqual(50f, _io.transform.eulerAngles.z, 0.1f);
            Assert.AreEqual(0, _transformChangedCount, "持续旋转期间不派 OnObjectTransformChanged");
        }

        [Test]
        public void AC1_HandleRotate_UpdatedPhase_AccumulatesDelta()
        {
            EnterSelected();
            _io.transform.eulerAngles = new Vector3(0, 0, 30f);
            _io.HandleRotate(new GestureData { Phase = GesturePhase.Began, AngleDelta = 0f });
            // 现 AccumulatedAngleDeg=30, transform.z=30

            _io.HandleRotate(new GestureData
            {
                Phase = GesturePhase.Updated,
                AngleDelta = 10f * Mathf.Deg2Rad
            });
            _io.HandleRotate(new GestureData
            {
                Phase = GesturePhase.Updated,
                AngleDelta = 5f * Mathf.Deg2Rad
            });

            Assert.AreEqual(45f, _io.AccumulatedAngleDeg, 0.01f, "30 + 10 + 5 = 45");
            Assert.AreEqual(0, _transformChangedCount);
        }

        [Test]
        public void AC1_HandleRotate_NegativeDelta_DecreasesAngle()
        {
            EnterSelected();
            _io.transform.eulerAngles = new Vector3(0, 0, 60f);
            _io.HandleRotate(new GestureData { Phase = GesturePhase.Began, AngleDelta = 0f });

            _io.HandleRotate(new GestureData
            {
                Phase = GesturePhase.Updated,
                AngleDelta = -25f * Mathf.Deg2Rad
            });

            Assert.AreEqual(35f, _io.AccumulatedAngleDeg, 0.01f, "60 - 25 = 35");
        }

        [Test]
        public void AC1_HandleRotate_WrapAround_AccumulatorBeyond360_NoJump()
        {
            EnterSelected();
            _io.transform.eulerAngles = new Vector3(0, 0, 350f);
            _io.HandleRotate(new GestureData { Phase = GesturePhase.Began, AngleDelta = 0f });

            // 累加 30° 应越过 360° 边界 — 累加器应到 380（不 wrap）；transform.z 由 Unity 内部 wrap 到 [0, 360)
            _io.HandleRotate(new GestureData
            {
                Phase = GesturePhase.Updated,
                AngleDelta = 30f * Mathf.Deg2Rad
            });

            Assert.AreEqual(380f, _io.AccumulatedAngleDeg, 0.01f,
                "累加器持续累加（不 wrap）— 这是 wrap-around 防跳变的关键");
        }

        // ==================================================================
        // AC-2: Ended phase snap 到最近 rotationStep
        // ==================================================================

        [Test]
        public void AC2_SnapRotation_AtNonStepAngle_RoundsToNearestStep()
        {
            EnterSelected();
            _io.transform.eulerAngles = new Vector3(0, 0, 47f);
            _io.HandleRotate(new GestureData { Phase = GesturePhase.Began, AngleDelta = 0f });
            // 现 AccumulatedAngleDeg = 47

            _io.HandleRotate(new GestureData { Phase = GesturePhase.Ended });
            _io.transform.DOCompleteSafe();

            Assert.AreEqual(45f, _io.AccumulatedAngleDeg, 0.01f, "47 → round(47/15)*15 = 45");
            Assert.AreEqual(45f, _io.transform.eulerAngles.z, 0.1f);
        }

        [Test]
        public void AC2_SnapRotation_AboveMidpoint_RoundsUp()
        {
            // 注：用 53° 而非精确 midpoint 52.5° — Unity transform.eulerAngles set 后内部走
            // Quaternion 转换再 get 会引入浮点误差（52.5 ≈ 52.4999... → round(3.4999)=3 → 45），
            // midpoint 边缘行为依赖 Unity Quaternion 实际精度，留 PlayMode 验证；EditMode
            // 用 53° 明确表达"超过半步应 round up"语义（53/15=3.533 → round=4 → 60）。
            EnterSelected();
            _io.transform.eulerAngles = new Vector3(0, 0, 53f);
            _io.HandleRotate(new GestureData { Phase = GesturePhase.Began, AngleDelta = 0f });

            _io.HandleRotate(new GestureData { Phase = GesturePhase.Ended });
            _io.transform.DOCompleteSafe();

            Assert.AreEqual(60f, _io.AccumulatedAngleDeg, 0.01f, "53 → round(3.533)*15 = 60");
        }

        [Test]
        public void AC2_SnapRotation_FromSmallAngle_RoundsToZero()
        {
            EnterSelected();
            _io.transform.eulerAngles = new Vector3(0, 0, 7.4f);
            _io.HandleRotate(new GestureData { Phase = GesturePhase.Began, AngleDelta = 0f });

            _io.HandleRotate(new GestureData { Phase = GesturePhase.Ended });
            _io.transform.DOCompleteSafe();

            Assert.AreEqual(0f, _io.AccumulatedAngleDeg, 0.01f, "7.4 → round(0.49)*15 = 0");
        }

        [Test]
        public void AC2_SnapRotation_AccumulatorOver360_NormalizesAfterSnap()
        {
            EnterSelected();
            _io.transform.eulerAngles = new Vector3(0, 0, 350f);
            _io.HandleRotate(new GestureData { Phase = GesturePhase.Began, AngleDelta = 0f });
            _io.HandleRotate(new GestureData
            {
                Phase = GesturePhase.Updated,
                AngleDelta = 30f * Mathf.Deg2Rad
            });
            // 累加器 = 380°；snap → round(380/15)*15 = 375 → 归一化 (375 % 360) = 15

            _io.HandleRotate(new GestureData { Phase = GesturePhase.Ended });
            _io.transform.DOCompleteSafe();

            Assert.AreEqual(15f, _io.AccumulatedAngleDeg, 0.01f, "归一化到 [0, 360)");
        }

        // ==================================================================
        // AC-3 / AC-4: OnObjectTransformChanged 在 OnComplete 后派发
        // ==================================================================

        [Test]
        public void AC4_SnapRotation_OnComplete_DispatchesObjectTransformChanged()
        {
            EnterSelected();
            _io.transform.eulerAngles = new Vector3(0, 0, 47f);
            _io.HandleRotate(new GestureData { Phase = GesturePhase.Began, AngleDelta = 0f });

            _io.HandleRotate(new GestureData { Phase = GesturePhase.Ended });
            _io.transform.DOCompleteSafe();

            Assert.AreEqual(1, _transformChangedCount, "OnObjectTransformChanged 应派发 1 次");
            Assert.AreEqual(TestObjectId, _lastTransformObjectId);
            Assert.AreEqual(45f, _lastTransformRotation.eulerAngles.z, 0.1f, "事件携带 snapped 旋转");
        }

        [Test]
        public void AC4_SnapRotation_BeforeOnComplete_NoEventDispatched()
        {
            EnterSelected();
            _io.transform.eulerAngles = new Vector3(0, 0, 47f);
            _io.HandleRotate(new GestureData { Phase = GesturePhase.Began, AngleDelta = 0f });

            _io.HandleRotate(new GestureData { Phase = GesturePhase.Ended });
            // 不调 DOCompleteSafe — tween 未完成

            Assert.AreEqual(0, _transformChangedCount, "tween 未 OnComplete 时不派事件");
        }

        [Test]
        public void AC3_SnapRotation_AlreadyAtStep_SyncCompletes_DispatchesOnce()
        {
            EnterSelected();
            _io.transform.eulerAngles = new Vector3(0, 0, 45f);   // 已是 15° 倍数
            _io.HandleRotate(new GestureData { Phase = GesturePhase.Began, AngleDelta = 0f });

            _io.HandleRotate(new GestureData { Phase = GesturePhase.Ended });
            // 不调 DOCompleteSafe — 同步路径已派发

            Assert.AreEqual(1, _transformChangedCount, "已在格点 → 同步走 OnComplete → 派发 1 次");
            Assert.AreEqual(45f, _io.AccumulatedAngleDeg, 0.01f);
            Assert.AreEqual(45f, _io.transform.eulerAngles.z, 0.1f);
        }

        // ==================================================================
        // AC-5: Cancelled phase 等价 Ended
        // ==================================================================

        [Test]
        public void AC5_HandleRotate_CancelledPhase_TriggersSnap_SameAsEnded()
        {
            EnterSelected();
            _io.transform.eulerAngles = new Vector3(0, 0, 47f);
            _io.HandleRotate(new GestureData { Phase = GesturePhase.Began, AngleDelta = 0f });

            _io.HandleRotate(new GestureData { Phase = GesturePhase.Cancelled });
            _io.transform.DOCompleteSafe();

            Assert.AreEqual(45f, _io.AccumulatedAngleDeg, 0.01f, "Cancelled 等价 Ended → snap 到 45°");
            Assert.AreEqual(1, _transformChangedCount);
            Assert.AreEqual(45f, _lastTransformRotation.eulerAngles.z, 0.1f);
        }

        // ==================================================================
        // AC-6: Locked 状态忽略
        // ==================================================================

        [Test]
        public void AC6_HandleRotate_LockedState_Ignored()
        {
            EnterSelected();
            _io.transform.eulerAngles = new Vector3(0, 0, 30f);
            // 转 Locked
            _io.Fsm.OnLockChanged(true);
            Assert.AreEqual(InteractableObjectState.Locked, _io.Fsm.CurrentState);

            _io.HandleRotate(new GestureData
            {
                Phase = GesturePhase.Updated,
                AngleDelta = 30f * Mathf.Deg2Rad
            });

            Assert.AreEqual(30f, _io.transform.eulerAngles.z, 0.1f, "Locked → 旋转不变");
            Assert.AreEqual(0f, _io.AccumulatedAngleDeg, 0.01f, "Locked → 累加器未启动（默认 0）");
            Assert.AreEqual(0, _transformChangedCount);
        }

        [Test]
        public void AC6_HandleRotate_Began_ThenLocks_NoSnap_NoEvent()
        {
            EnterSelected();
            _io.transform.eulerAngles = new Vector3(0, 0, 47f);
            _io.HandleRotate(new GestureData { Phase = GesturePhase.Began, AngleDelta = 0f });
            // AccumulatedAngleDeg = 47

            // 中途 Lock
            _io.Fsm.OnLockChanged(true);

            // Lock 后 Ended phase 不应触发 snap
            _io.HandleRotate(new GestureData { Phase = GesturePhase.Ended });

            Assert.AreEqual(47f, _io.AccumulatedAngleDeg, 0.01f, "累加值保留（不 snap 不归一化）");
            Assert.AreEqual(47f, _io.transform.eulerAngles.z, 0.1f);
            Assert.AreEqual(0, _transformChangedCount, "Locked 中 Ended phase 不派 OnObjectTransformChanged");
        }

        // ==================================================================
        // AC-7: Coordinator 端到端转发（OnRotate listener → CurrentSelectedObject.HandleRotate）
        // ==================================================================

        [Test]
        public void AC7_Coordinator_OnRotate_ForwardsToSelected_HandleRotate()
        {
            EnterSelected();
            _io.transform.eulerAngles = new Vector3(0, 0, 30f);

            // 创建 Coordinator + 注入 InputConfig + 选中 _io
            InteractionCoordinator.RegisterInputConfigProvider(() =>
            {
                var cfg = new InputConfigFromLuban();
                cfg.InitWithDefaults();
                return cfg;
            });
            var coordGo = new GameObject("InteractionCoordinator");
            var coord = coordGo.AddComponent<InteractionCoordinator>();
            coord.SetGameplayCameraForTest(_camera);
            coord.SetObjectsForTest(new List<InteractableObject> { _io });
            coord.Initialize();

            try
            {
                // _io 在 Selected 状态；coord 需要把 _io 设为 CurrentSelectedObject
                // 注意：fsm 已经是 Selected（EnterSelected 内 fsm.OnTapHit），但 coord.CurrentSelectedObject == null
                // 直接用 TrySelectObject 让 coord 持有引用（fsm 已在 Selected，会触发 fsm.OnTapHit 守卫忽略 — 但 coord 的引用更新仍发生）
                coord.ResetDebounceForTest();
                coord.TrySelectObject(_io);
                Assert.AreSame(_io, coord.CurrentSelectedObject);

                // 端到端通过 GameEvent 派发 OnRotate；先 Began 加载 transform.z=30 到累加器，
                // 再 Updated 累加 10° → 累加器 40°。两次都验证 Coordinator listener 转发链。
                GameEvent.Get<IGestureEvent>().OnRotate(new GestureData
                {
                    Phase = GesturePhase.Began,
                    AngleDelta = 0f
                });
                Assert.AreEqual(30f, _io.AccumulatedAngleDeg, 0.1f,
                    "Began 通过 Coordinator 转发 → _io.HandleRotate → 加载 transform.z=30°");

                GameEvent.Get<IGestureEvent>().OnRotate(new GestureData
                {
                    Phase = GesturePhase.Updated,
                    AngleDelta = 10f * Mathf.Deg2Rad
                });

                Assert.AreEqual(40f, _io.AccumulatedAngleDeg, 0.1f,
                    "Updated 通过 Coordinator 转发 → _io.HandleRotate → 累加 30 + 10 = 40°");
            }
            finally
            {
                coord.Shutdown();
                Object.DestroyImmediate(coordGo);
                InteractionCoordinator.ClearInputConfigProviderForTest();
            }
        }

        [Test]
        public void Coordinator_OnRotate_NoSelected_NoOp()
        {
            EnterSelected();
            _io.transform.eulerAngles = new Vector3(0, 0, 30f);

            InteractionCoordinator.RegisterInputConfigProvider(() =>
            {
                var cfg = new InputConfigFromLuban();
                cfg.InitWithDefaults();
                return cfg;
            });
            var coordGo = new GameObject("InteractionCoordinator");
            var coord = coordGo.AddComponent<InteractionCoordinator>();
            coord.SetGameplayCameraForTest(_camera);
            coord.SetObjectsForTest(new List<InteractableObject> { _io });
            coord.Initialize();

            try
            {
                // 不调 TrySelectObject — coord.CurrentSelectedObject == null
                Assert.IsNull(coord.CurrentSelectedObject);

                GameEvent.Get<IGestureEvent>().OnRotate(new GestureData
                {
                    Phase = GesturePhase.Updated,
                    AngleDelta = 30f * Mathf.Deg2Rad
                });

                Assert.AreEqual(30f, _io.transform.eulerAngles.z, 0.1f,
                    "无 CurrentSelectedObject → Coordinator OnRotate 不转发；_io 旋转不变");
            }
            finally
            {
                coord.Shutdown();
                Object.DestroyImmediate(coordGo);
                InteractionCoordinator.ClearInputConfigProviderForTest();
            }
        }
    }
}
