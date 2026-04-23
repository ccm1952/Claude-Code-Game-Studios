// 该文件由Cursor 自动生成
using UnityEngine;
using Unity.Profiling;

namespace GameLogic
{
    public enum SingleFingerState : byte
    {
        Idle,
        Pending,
        Dragging,
        LongPress
    }

    /// <summary>
    /// Deterministic state machine that converts raw per-finger <see cref="TouchState"/>
    /// updates into structured <see cref="GestureData"/> packets.
    /// <para>
    /// Transitions:
    /// <c>Idle → Pending → Tap|Dragging|LongPress → Idle</c>
    /// </para>
    /// All thresholds sourced from <see cref="IInputConfig"/> (Luban-backed at runtime).
    /// Zero heap allocation on the hot path.
    /// </summary>
    public class SingleFingerFSM
    {
        static readonly ProfilerMarker s_UpdateMarker =
            new ProfilerMarker("InputService.Update");

        private SingleFingerState _state;
        private TouchState _tracked;
        private readonly IInputConfig _config;
        private float _dragThresholdPx;
        private GestureData _pendingGesture;
        private bool _hasGesture;

        public SingleFingerState CurrentState => _state;

        public SingleFingerFSM(IInputConfig config)
        {
            _config = config;
            _state = SingleFingerState.Idle;
            _hasGesture = false;
        }

        /// <summary>
        /// Recompute the pixel-space drag threshold from physical millimetres + device DPI.
        /// Call once at startup and again if display settings change.
        /// </summary>
        public void ComputeDragThreshold(float screenDpi)
        {
            float dpi = screenDpi > 0f ? screenDpi : _config.FallbackDpi;
            _dragThresholdPx = _config.BaseDragThresholdMm * dpi / 25.4f;
        }

        /// <summary>
        /// Advance the FSM by one frame. Returns <c>true</c> when a gesture was produced;
        /// retrieve it via <see cref="GetGesture"/>.
        /// </summary>
        /// <param name="touch">Current-frame touch snapshot (passed by ref to avoid copy cost).</param>
        /// <param name="unscaledDeltaTime"><c>Time.unscaledDeltaTime</c> — immune to <c>Time.timeScale</c>.</param>
        public bool Update(in TouchState touch, float unscaledDeltaTime)
        {
            s_UpdateMarker.Begin();
            _hasGesture = false;

            switch (_state)
            {
                case SingleFingerState.Idle:
                    ProcessIdle(in touch);
                    break;
                case SingleFingerState.Pending:
                    ProcessPending(in touch, unscaledDeltaTime);
                    break;
                case SingleFingerState.Dragging:
                    ProcessDragging(in touch);
                    break;
                case SingleFingerState.LongPress:
                    ProcessLongPress(in touch);
                    break;
            }

            s_UpdateMarker.End();
            return _hasGesture;
        }

        /// <summary>
        /// Retrieve the gesture emitted by the most recent <see cref="Update"/> call.
        /// Only valid when <see cref="Update"/> returned <c>true</c>.
        /// </summary>
        public GestureData GetGesture() => _pendingGesture;

        /// <summary>
        /// Slam the FSM back to <see cref="SingleFingerState.Idle"/> and discard all
        /// in-flight state. Used when the app pauses, scene changes, or InputBlocker activates.
        /// No events are emitted (silent cancel).
        /// </summary>
        public void ForceReset()
        {
            _state = SingleFingerState.Idle;
            _tracked = default;
            _hasGesture = false;
        }

        /// <summary>
        /// Cancel an in-progress drag and emit <see cref="GesturePhase.Cancelled"/>.
        /// Called by <c>DualFingerFSM</c> when a second finger lands while this FSM
        /// is in the <see cref="SingleFingerState.Dragging"/> state.
        /// Returns <c>true</c> if a DragCancelled gesture was emitted.
        /// </summary>
        public bool CancelDrag()
        {
            if (_state != SingleFingerState.Dragging)
                return false;

            EmitGesture(GestureType.Drag, GesturePhase.Cancelled,
                _tracked.CurrentPosition, Vector2.zero);
            _state = SingleFingerState.Idle;
            _tracked = default;
            return true;
        }

        // ──────────────────────── State processors ────────────────────────

        private void ProcessIdle(in TouchState touch)
        {
            if (!touch.IsActive || touch.Phase != TouchPhase.Began)
                return;

            _tracked = touch;
            _tracked.StartPosition = touch.CurrentPosition;
            _tracked.PreviousPosition = touch.CurrentPosition;
            _tracked.AccumulatedTime = 0f;
            _tracked.AccumulatedDist = 0f;
            _state = SingleFingerState.Pending;
        }

        private void ProcessPending(in TouchState touch, float unscaledDeltaTime)
        {
            _tracked.AccumulatedTime += unscaledDeltaTime;

            float frameDist = Vector2.Distance(touch.CurrentPosition, _tracked.PreviousPosition);
            _tracked.AccumulatedDist += frameDist;
            _tracked.PreviousPosition = touch.CurrentPosition;
            _tracked.CurrentPosition = touch.CurrentPosition;

            if (_tracked.AccumulatedDist > _dragThresholdPx)
            {
                _state = SingleFingerState.Dragging;
                EmitGesture(GestureType.Drag, GesturePhase.Began,
                    touch.CurrentPosition, Vector2.zero);
                return;
            }

            if (touch.Phase == TouchPhase.Ended || touch.Phase == TouchPhase.Canceled)
            {
                if (_tracked.AccumulatedTime < _config.TapTimeoutSeconds
                    && _tracked.AccumulatedDist <= _dragThresholdPx)
                {
                    EmitTap(touch.CurrentPosition);
                }
                _state = SingleFingerState.Idle;
                return;
            }

            if (_tracked.AccumulatedTime >= _config.TapTimeoutSeconds)
            {
                _state = SingleFingerState.LongPress;
            }
        }

        private void ProcessDragging(in TouchState touch)
        {
            if (touch.Phase == TouchPhase.Ended || touch.Phase == TouchPhase.Canceled)
            {
                EmitGesture(GestureType.Drag, GesturePhase.Ended,
                    touch.CurrentPosition, Vector2.zero);
                _state = SingleFingerState.Idle;
                return;
            }

            Vector2 rawDelta = touch.CurrentPosition - _tracked.PreviousPosition;
            float mag = rawDelta.magnitude;
            if (mag > _config.MaxDeltaPerFrame)
                rawDelta = rawDelta.normalized * _config.MaxDeltaPerFrame;

            _tracked.PreviousPosition = touch.CurrentPosition;
            _tracked.CurrentPosition = touch.CurrentPosition;

            EmitGesture(GestureType.Drag, GesturePhase.Updated,
                touch.CurrentPosition, rawDelta);
        }

        private void ProcessLongPress(in TouchState touch)
        {
            if (touch.Phase == TouchPhase.Ended || touch.Phase == TouchPhase.Canceled)
            {
                _state = SingleFingerState.Idle;
            }
        }

        // ──────────────────────── Helpers ────────────────────────

        private void EmitTap(Vector2 position)
        {
            _pendingGesture = new GestureData
            {
                Type = GestureType.Tap,
                Phase = GesturePhase.Ended,
                ScreenPosition = position,
                Delta = Vector2.zero,
                TouchCount = 1,
                TapCount = 1
            };
            _hasGesture = true;
        }

        private void EmitGesture(GestureType type, GesturePhase phase,
            Vector2 position, Vector2 delta)
        {
            _pendingGesture = new GestureData
            {
                Type = type,
                Phase = phase,
                ScreenPosition = position,
                Delta = delta,
                TouchCount = 1,
                TapCount = 0
            };
            _hasGesture = true;
        }
    }
}
