// 该文件由Cursor 自动生成
using UnityEngine;

namespace GameLogic
{
    public enum DualFingerState : byte
    {
        Idle,
        Pending2,
        Rotating,
        Pinching
    }

    /// <summary>
    /// Two-finger gesture FSM: Idle → Pending2 → Rotating | Pinching → Idle.
    /// Mutual exclusion: once Rotating or Pinching is entered, the gesture is
    /// locked until all fingers lift. All thresholds sourced from
    /// <see cref="IDualFingerConfig"/> (Luban-backed at runtime).
    /// </summary>
    public class DualFingerFSM
    {
        private DualFingerState _state;
        private readonly IDualFingerConfig _config;
        private readonly SingleFingerFSM _singleFinger;

        private Vector2 _prevTouch0;
        private Vector2 _prevTouch1;
        private float _accumulatedAngle;
        private float _accumulatedScale;

        private GestureData _pendingGesture;
        private bool _hasGesture;

        // When entering Pending2 from a single-finger drag, we emit DragCancelled
        // through the SingleFingerFSM. This flag lets callers check both FSMs.
        private bool _didCancelSingleDrag;

        public DualFingerState CurrentState => _state;
        public bool DidCancelSingleDrag => _didCancelSingleDrag;

        public DualFingerFSM(IDualFingerConfig config, SingleFingerFSM singleFinger)
        {
            _config = config;
            _singleFinger = singleFinger;
            _state = DualFingerState.Idle;
        }

        /// <summary>
        /// Advance the FSM by one frame with two-finger input.
        /// <paramref name="touch0"/> and <paramref name="touch1"/> are the two tracked fingers.
        /// <paramref name="hasTwoFingers"/> indicates whether two fingers are currently active.
        /// Returns <c>true</c> when a gesture was produced; retrieve via <see cref="GetGesture"/>.
        /// </summary>
        public bool Update(in TouchState touch0, in TouchState touch1, bool hasTwoFingers)
        {
            _hasGesture = false;
            _didCancelSingleDrag = false;

            switch (_state)
            {
                case DualFingerState.Idle:
                    ProcessIdle(in touch0, in touch1, hasTwoFingers);
                    break;
                case DualFingerState.Pending2:
                    ProcessPending2(in touch0, in touch1, hasTwoFingers);
                    break;
                case DualFingerState.Rotating:
                    ProcessRotating(in touch0, in touch1, hasTwoFingers);
                    break;
                case DualFingerState.Pinching:
                    ProcessPinching(in touch0, in touch1, hasTwoFingers);
                    break;
            }

            return _hasGesture;
        }

        public GestureData GetGesture() => _pendingGesture;

        public void ForceReset()
        {
            _state = DualFingerState.Idle;
            _accumulatedAngle = 0f;
            _accumulatedScale = 1f;
            _hasGesture = false;
        }

        // ──────────────────────── State processors ────────────────────────

        private void ProcessIdle(in TouchState touch0, in TouchState touch1, bool hasTwoFingers)
        {
            if (!hasTwoFingers)
                return;

            if (touch1.Phase == TouchPhase.Began || touch0.Phase == TouchPhase.Began)
            {
                _didCancelSingleDrag = _singleFinger.CancelDrag();

                _prevTouch0 = touch0.CurrentPosition;
                _prevTouch1 = touch1.CurrentPosition;
                _accumulatedAngle = 0f;
                _accumulatedScale = 1f;
                _state = DualFingerState.Pending2;
            }
        }

        private void ProcessPending2(in TouchState touch0, in TouchState touch1, bool hasTwoFingers)
        {
            if (!hasTwoFingers || IsFingerEnded(touch0) || IsFingerEnded(touch1))
            {
                _state = DualFingerState.Idle;
                return;
            }

            Vector2 curr0 = touch0.CurrentPosition;
            Vector2 curr1 = touch1.CurrentPosition;
            float currDist = Vector2.Distance(curr0, curr1);

            if (currDist >= _config.MinFingerDistance)
            {
                float prevDist = Vector2.Distance(_prevTouch0, _prevTouch1);
                float angleDelta = ComputeAngleDelta(_prevTouch0, _prevTouch1, curr0, curr1);

                _accumulatedAngle += angleDelta;

                if (prevDist >= _config.MinFingerDistance)
                {
                    float frameScale = currDist / prevDist;
                    _accumulatedScale *= frameScale;
                }
            }

            if (Mathf.Abs(_accumulatedAngle) > _config.RotateThresholdRad)
            {
                _state = DualFingerState.Rotating;
                Vector2 mid = (curr0 + curr1) * 0.5f;
                EmitGesture(GestureType.Rotate, GesturePhase.Began, mid, 0f, 1f);
                _prevTouch0 = curr0;
                _prevTouch1 = curr1;
                return;
            }

            if (Mathf.Abs(_accumulatedScale - 1f) > _config.PinchThreshold)
            {
                _state = DualFingerState.Pinching;
                Vector2 mid = (curr0 + curr1) * 0.5f;
                EmitGesture(GestureType.Pinch, GesturePhase.Began, mid, 0f, _accumulatedScale);
                _prevTouch0 = curr0;
                _prevTouch1 = curr1;
                return;
            }

            _prevTouch0 = curr0;
            _prevTouch1 = curr1;
        }

        private void ProcessRotating(in TouchState touch0, in TouchState touch1, bool hasTwoFingers)
        {
            if (!hasTwoFingers || IsFingerEnded(touch0) || IsFingerEnded(touch1))
            {
                Vector2 mid = (_prevTouch0 + _prevTouch1) * 0.5f;
                EmitGesture(GestureType.Rotate, GesturePhase.Ended, mid, 0f, 1f);
                _state = DualFingerState.Idle;
                return;
            }

            Vector2 curr0 = touch0.CurrentPosition;
            Vector2 curr1 = touch1.CurrentPosition;
            float currDist = Vector2.Distance(curr0, curr1);

            float angleDelta = 0f;
            if (currDist >= _config.MinFingerDistance)
                angleDelta = ComputeAngleDelta(_prevTouch0, _prevTouch1, curr0, curr1);

            Vector2 midpoint = (curr0 + curr1) * 0.5f;
            EmitGesture(GestureType.Rotate, GesturePhase.Updated, midpoint, angleDelta, 1f);

            _prevTouch0 = curr0;
            _prevTouch1 = curr1;
        }

        private void ProcessPinching(in TouchState touch0, in TouchState touch1, bool hasTwoFingers)
        {
            if (!hasTwoFingers || IsFingerEnded(touch0) || IsFingerEnded(touch1))
            {
                Vector2 mid = (_prevTouch0 + _prevTouch1) * 0.5f;
                EmitGesture(GestureType.Pinch, GesturePhase.Ended, mid, 0f, 1f);
                _state = DualFingerState.Idle;
                return;
            }

            Vector2 curr0 = touch0.CurrentPosition;
            Vector2 curr1 = touch1.CurrentPosition;
            float prevDist = Vector2.Distance(_prevTouch0, _prevTouch1);
            float currDist = Vector2.Distance(curr0, curr1);

            float scaleDelta = (prevDist < _config.MinFingerDistance) ? 1f : (currDist / prevDist);

            Vector2 midpoint = (curr0 + curr1) * 0.5f;
            EmitGesture(GestureType.Pinch, GesturePhase.Updated, midpoint, 0f, scaleDelta);

            _prevTouch0 = curr0;
            _prevTouch1 = curr1;
        }

        // ──────────────────────── Helpers ────────────────────────

        private static float ComputeAngleDelta(Vector2 prev0, Vector2 prev1,
            Vector2 curr0, Vector2 curr1)
        {
            Vector2 prevDir = (prev1 - prev0).normalized;
            Vector2 currDir = (curr1 - curr0).normalized;
            float cross = prevDir.x * currDir.y - prevDir.y * currDir.x;
            float dot = Vector2.Dot(prevDir, currDir);
            return Mathf.Atan2(cross, dot);
        }

        private static bool IsFingerEnded(in TouchState touch)
        {
            return touch.Phase == TouchPhase.Ended || touch.Phase == TouchPhase.Canceled;
        }

        private void EmitGesture(GestureType type, GesturePhase phase,
            Vector2 midpoint, float angleDelta, float scaleDelta)
        {
            _pendingGesture = new GestureData
            {
                Type = type,
                Phase = phase,
                ScreenPosition = midpoint,
                Delta = Vector2.zero,
                AngleDelta = angleDelta,
                ScaleDelta = scaleDelta,
                TouchCount = 2,
                TapCount = 0
            };
            _hasGesture = true;
        }
    }
}
