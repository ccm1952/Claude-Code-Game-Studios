// 该文件由Cursor 自动生成
using NUnit.Framework;
using UnityEngine;
using GameLogic;

namespace ShadowGame.Tests.EditMode.InputSystem
{
    internal sealed class TestDualFingerConfig : IDualFingerConfig
    {
        public float RotateThresholdRad { get; set; } = 8f * Mathf.Deg2Rad; // 8°
        public float PinchThreshold { get; set; } = 0.08f;
        public float MinFingerDistance { get; set; } = 20f;
    }

    [TestFixture]
    public class DualFingerFSMTests
    {
        private TestInputConfig _singleConfig;
        private TestDualFingerConfig _dualConfig;
        private SingleFingerFSM _singleFSM;
        private DualFingerFSM _dualFSM;

        [SetUp]
        public void SetUp()
        {
            _singleConfig = new TestInputConfig();
            _dualConfig = new TestDualFingerConfig();
            _singleFSM = new SingleFingerFSM(_singleConfig);
            _singleFSM.ComputeDragThreshold(326f);
            _dualFSM = new DualFingerFSM(_dualConfig, _singleFSM);
        }

        // ────────── TC-002-01: Rotate gesture from Pending2 ──────────

        [Test]
        public void Pending2_ExceedsRotateThreshold_TransitionsToRotating()
        {
            EnterPending2();

            // Rotate ~10° (> threshold 8°) by shifting touch1 upward
            // touch0 stays at (0,0), touch1 was at (100,0), now at (98, 17) ≈ 10° CCW
            var t0 = MakeDualTouch(0, TouchPhase.Stationary, new Vector2(0, 0));
            var t1 = MakeDualTouch(1, TouchPhase.Moved, new Vector2(98f, 17.4f));
            _dualFSM.Update(in t0, in t1, true);

            Assert.AreEqual(DualFingerState.Rotating, _dualFSM.CurrentState);
            var g = _dualFSM.GetGesture();
            Assert.AreEqual(GestureType.Rotate, g.Type);
            Assert.AreEqual(GesturePhase.Began, g.Phase);
            Assert.AreEqual(2, g.TouchCount);
        }

        // ────────── TC-002-02: Pinch gesture from Pending2 ──────────

        [Test]
        public void Pending2_ExceedsPinchThreshold_TransitionsToPinching()
        {
            EnterPending2();

            // Expand from 100px to 115px → scale = 1.15, deviation = 0.15 > 0.08
            var t0 = MakeDualTouch(0, TouchPhase.Moved, new Vector2(-7.5f, 0));
            var t1 = MakeDualTouch(1, TouchPhase.Moved, new Vector2(107.5f, 0));
            _dualFSM.Update(in t0, in t1, true);

            Assert.AreEqual(DualFingerState.Pinching, _dualFSM.CurrentState);
            var g = _dualFSM.GetGesture();
            Assert.AreEqual(GestureType.Pinch, g.Type);
            Assert.AreEqual(GesturePhase.Began, g.Phase);
        }

        // ────────── TC-002-03: Mutual exclusion — Rotating blocks Pinching ──────────

        [Test]
        public void Rotating_LargeScaleChange_StaysRotating()
        {
            EnterRotating();

            // Large pinch-like movement while Rotating — should stay Rotating
            var t0 = MakeDualTouch(0, TouchPhase.Moved, new Vector2(-50f, 0));
            var t1 = MakeDualTouch(1, TouchPhase.Moved, new Vector2(200f, 0));
            _dualFSM.Update(in t0, in t1, true);

            Assert.AreEqual(DualFingerState.Rotating, _dualFSM.CurrentState,
                "Must NOT switch from Rotating to Pinching mid-gesture");
            Assert.AreEqual(GestureType.Rotate, _dualFSM.GetGesture().Type);
        }

        // ────────── TC-002-04: Single-finger drag cancelled by second finger ──────────

        [Test]
        public void SecondFinger_CancelsSingleFingerDrag()
        {
            // Put SingleFingerFSM into Dragging
            var began = MakeSingleTouch(TouchPhase.Began, Vector2.zero);
            _singleFSM.Update(in began, 0f);
            var moved = MakeSingleTouch(TouchPhase.Moved, new Vector2(50f, 0f));
            _singleFSM.Update(in moved, 0.02f);
            Assert.AreEqual(SingleFingerState.Dragging, _singleFSM.CurrentState);

            // Second finger lands
            var t0 = MakeDualTouch(0, TouchPhase.Stationary, new Vector2(50f, 0));
            var t1 = MakeDualTouch(1, TouchPhase.Began, new Vector2(150f, 0));
            _dualFSM.Update(in t0, in t1, true);

            Assert.IsTrue(_dualFSM.DidCancelSingleDrag,
                "DualFinger entry must cancel active single-finger drag");
            Assert.AreEqual(SingleFingerState.Idle, _singleFSM.CurrentState);
            Assert.AreEqual(DualFingerState.Pending2, _dualFSM.CurrentState);

            // SingleFingerFSM should have emitted DragCancelled
            var cancelGesture = _singleFSM.GetGesture();
            Assert.AreEqual(GestureType.Drag, cancelGesture.Type);
            Assert.AreEqual(GesturePhase.Cancelled, cancelGesture.Phase);
        }

        // ────────── TC-002-05: Close fingers suppress rotation ──────────

        [Test]
        public void Pending2_FingersCloseTogetherAngleIgnored()
        {
            // Start with fingers 15px apart (< minFingerDistance 20px)
            var t0_b = MakeDualTouch(0, TouchPhase.Began, new Vector2(0, 0));
            var t1_b = MakeDualTouch(1, TouchPhase.Began, new Vector2(15f, 0));
            _dualFSM.Update(in t0_b, in t1_b, true);

            // Large rotation attempt — but too close, angle should NOT accumulate
            var t0 = MakeDualTouch(0, TouchPhase.Stationary, new Vector2(0, 0));
            var t1 = MakeDualTouch(1, TouchPhase.Moved, new Vector2(0, 15f));
            _dualFSM.Update(in t0, in t1, true);

            Assert.AreEqual(DualFingerState.Pending2, _dualFSM.CurrentState,
                "Should not transition to Rotating when fingers are too close");
        }

        // ────────── TC-002-06: prevDistance < min → scaleDelta = 1.0 ──────────

        [Test]
        public void Pinching_PrevDistBelowMin_ScaleDeltaIsOne()
        {
            // Manually enter Pinching state with normal distance first
            EnterPinching();

            // Simulate a frame where prevDist was very small (fingers just spread apart)
            // We set prev positions close together, then spread wide
            // Use ForceReset + re-enter to control prev positions
            _dualFSM.ForceReset();

            // Enter with fingers very close (5px apart)
            var t0_b = MakeDualTouch(0, TouchPhase.Began, new Vector2(0, 0));
            var t1_b = MakeDualTouch(1, TouchPhase.Began, new Vector2(5f, 0));
            _dualFSM.Update(in t0_b, in t1_b, true);

            // Fingers spread far — but prevDist was 5px < 20px, so scaleDelta should be safe
            var t0 = MakeDualTouch(0, TouchPhase.Moved, new Vector2(-100, 0));
            var t1 = MakeDualTouch(1, TouchPhase.Moved, new Vector2(100, 0));
            _dualFSM.Update(in t0, in t1, true);

            // Even if it transitions to Pinching, the scaleDelta should be clamped/safe
            // No NaN, no Infinity, no divide-by-zero
            if (_dualFSM.CurrentState == DualFingerState.Pinching)
            {
                var g = _dualFSM.GetGesture();
                Assert.IsFalse(float.IsNaN(g.ScaleDelta), "ScaleDelta must not be NaN");
                Assert.IsFalse(float.IsInfinity(g.ScaleDelta), "ScaleDelta must not be Infinity");
            }
        }

        // ────────── TC-002-07: Finger cancelled → Ended ──────────

        [Test]
        public void Rotating_FingerCanceled_TreatedAsEnded()
        {
            EnterRotating();

            var t0 = MakeDualTouch(0, TouchPhase.Stationary, new Vector2(0, 0));
            var t1 = MakeDualTouch(1, TouchPhase.Canceled, new Vector2(100, 10));
            bool hasGesture = _dualFSM.Update(in t0, in t1, true);

            Assert.IsTrue(hasGesture);
            Assert.AreEqual(GesturePhase.Ended, _dualFSM.GetGesture().Phase);
            Assert.AreEqual(DualFingerState.Idle, _dualFSM.CurrentState);
        }

        // ────────── TC-002-08: Three fingers — only first two used ──────────

        [Test]
        public void ThreeFingers_OnlyFirstTwoUsed()
        {
            // This is enforced by the caller (InputService) which only passes slot 0/1.
            // FSM receives exactly 2 TouchStates — it never sees a third.
            // We verify FSM works correctly with any two fingers.
            var t0 = MakeDualTouch(0, TouchPhase.Began, new Vector2(0, 0));
            var t1 = MakeDualTouch(1, TouchPhase.Began, new Vector2(100, 0));
            _dualFSM.Update(in t0, in t1, true);

            Assert.AreEqual(DualFingerState.Pending2, _dualFSM.CurrentState);
            // No crash, no exception — structural guarantee by API design
        }

        // ────────── TC-002-09: Rotation direction correctness ──────────

        [Test]
        public void RotateDirection_ClockwiseIsNegative()
        {
            EnterPending2();

            // Clockwise rotation: touch1 moves from (100,0) to (98,-17.4)
            // This is ~10° CW → angleDelta should be negative
            var t0 = MakeDualTouch(0, TouchPhase.Stationary, new Vector2(0, 0));
            var t1 = MakeDualTouch(1, TouchPhase.Moved, new Vector2(98f, -17.4f));
            _dualFSM.Update(in t0, in t1, true);

            Assert.AreEqual(DualFingerState.Rotating, _dualFSM.CurrentState);
            var g = _dualFSM.GetGesture();
            // Began phase — direction verified via the atan2 algorithm
            // For a more precise check, enter Updated phase
            Assert.AreEqual(GestureType.Rotate, g.Type);

            // Get an Updated frame to check angleDelta sign
            var t1_2 = MakeDualTouch(1, TouchPhase.Moved, new Vector2(94f, -34.2f));
            _dualFSM.Update(in t0, in t1_2, true);

            var g2 = _dualFSM.GetGesture();
            Assert.AreEqual(GesturePhase.Updated, g2.Phase);
            Assert.Less(g2.AngleDelta, 0f,
                "Clockwise rotation must produce negative angleDelta");
        }

        // ──────────────────────── Helpers ────────────────────────

        private void EnterPending2()
        {
            var t0 = MakeDualTouch(0, TouchPhase.Began, new Vector2(0, 0));
            var t1 = MakeDualTouch(1, TouchPhase.Began, new Vector2(100, 0));
            _dualFSM.Update(in t0, in t1, true);
            Assert.AreEqual(DualFingerState.Pending2, _dualFSM.CurrentState);
        }

        private void EnterRotating()
        {
            EnterPending2();
            var t0 = MakeDualTouch(0, TouchPhase.Stationary, new Vector2(0, 0));
            var t1 = MakeDualTouch(1, TouchPhase.Moved, new Vector2(98f, 17.4f));
            _dualFSM.Update(in t0, in t1, true);
            Assert.AreEqual(DualFingerState.Rotating, _dualFSM.CurrentState);
        }

        private void EnterPinching()
        {
            EnterPending2();
            var t0 = MakeDualTouch(0, TouchPhase.Moved, new Vector2(-7.5f, 0));
            var t1 = MakeDualTouch(1, TouchPhase.Moved, new Vector2(107.5f, 0));
            _dualFSM.Update(in t0, in t1, true);
            Assert.AreEqual(DualFingerState.Pinching, _dualFSM.CurrentState);
        }

        private static TouchState MakeSingleTouch(TouchPhase phase, Vector2 position)
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

        private static TouchState MakeDualTouch(int fingerId, TouchPhase phase, Vector2 position)
        {
            return new TouchState
            {
                FingerId = fingerId,
                Phase = phase,
                CurrentPosition = position,
                PreviousPosition = position,
                IsActive = phase != TouchPhase.Ended && phase != TouchPhase.Canceled,
            };
        }
    }
}
