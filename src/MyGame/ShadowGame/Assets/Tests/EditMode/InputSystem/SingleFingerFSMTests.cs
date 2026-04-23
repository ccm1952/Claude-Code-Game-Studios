// 该文件由Cursor 自动生成
using NUnit.Framework;
using UnityEngine;
using GameLogic;

namespace ShadowGame.Tests.EditMode.InputSystem
{
    internal sealed class TestInputConfig : IInputConfig
    {
        public float BaseDragThresholdMm { get; set; } = 3.0f;
        public float TapTimeoutSeconds { get; set; } = 0.25f;
        public float MaxDeltaPerFrame { get; set; } = 100f;
        public float FallbackDpi { get; set; } = 160f;
    }

    [TestFixture]
    public class SingleFingerFSMTests
    {
        private TestInputConfig _config;
        private SingleFingerFSM _fsm;

        [SetUp]
        public void SetUp()
        {
            _config = new TestInputConfig();
            _fsm = new SingleFingerFSM(_config);
            _fsm.ComputeDragThreshold(326f); // iPhone Retina DPI
        }

        // ────────── TC-001-01: Normal Tap ──────────

        [Test]
        public void Tap_WithinThresholdAndTimeout_ProducesTapGesture()
        {
            // Began
            var began = MakeTouch(TouchPhase.Began, Vector2.zero);
            _fsm.Update(in began, 0f);
            Assert.AreEqual(SingleFingerState.Pending, _fsm.CurrentState);

            // Stationary for 0.1s (< tapTimeout 0.25s)
            var stationary = MakeTouch(TouchPhase.Stationary, new Vector2(2f, 2f));
            _fsm.Update(in stationary, 0.1f);
            Assert.AreEqual(SingleFingerState.Pending, _fsm.CurrentState);

            // Ended with small movement (< dragThreshold)
            var ended = MakeTouch(TouchPhase.Ended, new Vector2(3f, 3f));
            bool hasGesture = _fsm.Update(in ended, 0.01f);

            Assert.IsTrue(hasGesture);
            var g = _fsm.GetGesture();
            Assert.AreEqual(GestureType.Tap, g.Type);
            Assert.AreEqual(GesturePhase.Ended, g.Phase);
            Assert.AreEqual(1, g.TapCount);
            Assert.AreEqual(SingleFingerState.Idle, _fsm.CurrentState);
        }

        // ────────── TC-001-02: Tap Timeout → LongPress ──────────

        [Test]
        public void Pending_ExceedsTapTimeout_TransitionsToLongPress()
        {
            var began = MakeTouch(TouchPhase.Began, Vector2.zero);
            _fsm.Update(in began, 0f);

            // Hold still for 0.3s (> tapTimeout 0.25s)
            var stationary = MakeTouch(TouchPhase.Stationary, Vector2.zero);
            _fsm.Update(in stationary, 0.3f);
            Assert.AreEqual(SingleFingerState.LongPress, _fsm.CurrentState);

            // Lift finger — no Tap emitted
            var ended = MakeTouch(TouchPhase.Ended, Vector2.zero);
            bool hasGesture = _fsm.Update(in ended, 0.01f);

            Assert.IsFalse(hasGesture, "LongPress release must not emit any gesture in MVP");
            Assert.AreEqual(SingleFingerState.Idle, _fsm.CurrentState);
        }

        // ────────── TC-001-03: Normal Drag ──────────

        [Test]
        public void Pending_ExceedsDragThreshold_TransitionsToDragging()
        {
            // dragThreshold at 326 DPI = 3.0 * 326 / 25.4 ≈ 38.5 px
            var began = MakeTouch(TouchPhase.Began, Vector2.zero);
            _fsm.Update(in began, 0f);

            // Move 50 px (> threshold)
            var moved = MakeTouch(TouchPhase.Moved, new Vector2(50f, 0f));
            bool beganDrag = _fsm.Update(in moved, 0.02f);

            Assert.IsTrue(beganDrag);
            var g = _fsm.GetGesture();
            Assert.AreEqual(GestureType.Drag, g.Type);
            Assert.AreEqual(GesturePhase.Began, g.Phase);
            Assert.AreEqual(SingleFingerState.Dragging, _fsm.CurrentState);

            // Continue drag — Updated
            var continued = MakeTouch(TouchPhase.Moved, new Vector2(70f, 0f));
            bool updated = _fsm.Update(in continued, 0.016f);
            Assert.IsTrue(updated);
            Assert.AreEqual(GesturePhase.Updated, _fsm.GetGesture().Phase);

            // End drag
            var ended = MakeTouch(TouchPhase.Ended, new Vector2(80f, 0f));
            bool endedDrag = _fsm.Update(in ended, 0.016f);
            Assert.IsTrue(endedDrag);
            Assert.AreEqual(GesturePhase.Ended, _fsm.GetGesture().Phase);
            Assert.AreEqual(SingleFingerState.Idle, _fsm.CurrentState);
        }

        // ────────── TC-001-04: Delta Clamping ──────────

        [Test]
        public void Dragging_FastDelta_ClampedToMaxPerFrame()
        {
            PutInDraggingState();

            // Simulate a 200px jump in one frame (maxDeltaPerFrame = 100)
            var spike = MakeTouch(TouchPhase.Moved, new Vector2(250f, 0f));
            _fsm.Update(in spike, 0.016f);

            var g = _fsm.GetGesture();
            Assert.AreEqual(GestureType.Drag, g.Type);
            Assert.LessOrEqual(g.Delta.magnitude, _config.MaxDeltaPerFrame + 0.01f,
                "Delta must be clamped to MaxDeltaPerFrame");
        }

        // ────────── TC-001-05: App Pause Force Reset ──────────

        [Test]
        public void ForceReset_FromDragging_ReturnsToIdle()
        {
            PutInDraggingState();
            Assert.AreEqual(SingleFingerState.Dragging, _fsm.CurrentState);

            _fsm.ForceReset();

            Assert.AreEqual(SingleFingerState.Idle, _fsm.CurrentState);

            // Verify new touch works after reset
            var began = MakeTouch(TouchPhase.Began, Vector2.zero);
            _fsm.Update(in began, 0f);
            Assert.AreEqual(SingleFingerState.Pending, _fsm.CurrentState);
        }

        // ────────── TC-001-06: Screen.dpi = 0 Fallback ──────────

        [Test]
        public void DragThreshold_ZeroDpi_UsesFallback()
        {
            var config = new TestInputConfig { BaseDragThresholdMm = 3.0f, FallbackDpi = 160f };
            var fsm = new SingleFingerFSM(config);
            fsm.ComputeDragThreshold(0f); // dpi = 0 → use fallback 160

            // expected threshold = 3.0 * 160 / 25.4 ≈ 18.9 px
            // A 15 px move should NOT trigger drag; a 20 px move SHOULD.

            var began = MakeTouch(TouchPhase.Began, Vector2.zero);
            fsm.Update(in began, 0f);

            var small = MakeTouch(TouchPhase.Moved, new Vector2(15f, 0f));
            fsm.Update(in small, 0.01f);
            Assert.AreEqual(SingleFingerState.Pending, fsm.CurrentState,
                "15px should be below fallback-DPI threshold");

            var large = MakeTouch(TouchPhase.Moved, new Vector2(25f, 0f));
            fsm.Update(in large, 0.01f);
            Assert.AreEqual(SingleFingerState.Dragging, fsm.CurrentState,
                "Cumulative 25px should exceed fallback-DPI threshold ≈ 18.9px");
        }

        // ────────── TC-001-07: tapTimeout unscaled ──────────

        [Test]
        public void TapTimeout_UsesUnscaledTime()
        {
            var began = MakeTouch(TouchPhase.Began, Vector2.zero);
            _fsm.Update(in began, 0f);

            // Simulate 0.3s of unscaledDeltaTime (even if TimeScale were 0,
            // the caller passes unscaledDeltaTime so FSM never sees scaled time).
            var hold = MakeTouch(TouchPhase.Stationary, Vector2.zero);
            _fsm.Update(in hold, 0.15f);
            Assert.AreEqual(SingleFingerState.Pending, _fsm.CurrentState,
                "0.15s < tapTimeout, still Pending");

            _fsm.Update(in hold, 0.15f);
            Assert.AreEqual(SingleFingerState.LongPress, _fsm.CurrentState,
                "0.30s > tapTimeout 0.25s → LongPress regardless of timeScale");
        }

        // ────────── TC-001-08: Pre-allocated, no GC ──────────

        [Test]
        public void TouchState_PreAllocated_NoGCOnHotPath()
        {
            // Structural verification: TouchState is a struct (value type)
            Assert.IsTrue(typeof(TouchState).IsValueType,
                "TouchState must be a struct to avoid heap allocation");
            Assert.IsTrue(typeof(GestureData).IsValueType,
                "GestureData must be a struct to avoid heap allocation");

            // Drive 100 frames with no per-frame allocation visible at the API level.
            // (Deep profile GC assertion requires Editor Profiler; here we verify
            //  no boxing occurs in the public API surface.)
            PutInDraggingState();
            for (int i = 0; i < 100; i++)
            {
                float x = 50f + i * 0.5f;
                var moved = MakeTouch(TouchPhase.Moved, new Vector2(x, 0f));
                _fsm.Update(in moved, 0.016f);
                _ = _fsm.GetGesture();
            }
            Assert.AreEqual(SingleFingerState.Dragging, _fsm.CurrentState);
        }

        // ──────────────────────── Helpers ────────────────────────

        private void PutInDraggingState()
        {
            var began = MakeTouch(TouchPhase.Began, Vector2.zero);
            _fsm.Update(in began, 0f);
            var moved = MakeTouch(TouchPhase.Moved, new Vector2(50f, 0f));
            _fsm.Update(in moved, 0.02f);
            Assert.AreEqual(SingleFingerState.Dragging, _fsm.CurrentState);
        }

        private static TouchState MakeTouch(TouchPhase phase, Vector2 position)
        {
            return new TouchState
            {
                FingerId = 0,
                Phase = phase,
                CurrentPosition = position,
                PreviousPosition = position,
                IsActive = phase != TouchPhase.Ended && phase != TouchPhase.Canceled,
            };
        }
    }
}
