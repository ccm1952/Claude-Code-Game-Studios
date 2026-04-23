// 该文件由Cursor 自动生成
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Per-finger touch snapshot passed into gesture FSMs each frame.
    /// Struct keeps allocation off the heap; the FSM copies it into its
    /// internal buffer and maintains the accumulator fields across frames.
    ///
    /// Usage example:
    /// <code>
    ///   var ts = new TouchState
    ///   {
    ///       FingerId        = touch.fingerId,
    ///       Phase           = touch.phase,
    ///       CurrentPosition = touch.position,
    ///       IsActive        = true,
    ///   };
    ///   fsm.Update(in ts, Time.unscaledDeltaTime);
    /// </code>
    /// </summary>
    public struct TouchState
    {
        public int FingerId;

        /// <summary>Screen position where this touch first made contact.</summary>
        public Vector2 StartPosition;

        public Vector2 PreviousPosition;
        public Vector2 CurrentPosition;

        /// <summary>Populated from <c>Time.unscaledTime</c> at touch-began.</summary>
        public float StartTime;

        /// <summary>Running sum of <c>Time.unscaledDeltaTime</c> while active.
        /// Maintained by the FSM, not the caller.</summary>
        public float AccumulatedTime;

        /// <summary>Running sum of per-frame screen-space travel distance (pixels).
        /// Maintained by the FSM, not the caller.</summary>
        public float AccumulatedDist;

        public bool IsActive;

        /// <summary>
        /// Current Unity touch phase.  Required by the FSM to detect Began/Ended
        /// transitions; mirrors <c>UnityEngine.Touch.phase</c>.
        /// </summary>
        public TouchPhase Phase;
    }
}
