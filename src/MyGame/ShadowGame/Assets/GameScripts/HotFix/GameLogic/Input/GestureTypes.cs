// 该文件由Cursor 自动生成
using UnityEngine;

namespace GameLogic
{
    public enum GestureType : byte
    {
        None,
        Tap,
        Drag,
        Rotate,
        Pinch,
        LightDrag
    }

    public enum GesturePhase : byte
    {
        None,
        Began,
        Updated,
        Ended,
        Cancelled
    }

    /// <summary>
    /// Zero-allocation gesture data packet produced each frame by the gesture FSMs.
    /// Passed by value — keep fields lean.
    /// </summary>
    public struct GestureData
    {
        public GestureType Type;
        public GesturePhase Phase;
        public Vector2 ScreenPosition;
        public Vector2 Delta;
        /// <summary>Radians per frame, positive = CCW. Only valid for <see cref="GestureType.Rotate"/>.</summary>
        public float AngleDelta;
        /// <summary>Scale ratio this frame (&gt;1 zoom in, &lt;1 zoom out). Only valid for <see cref="GestureType.Pinch"/>.</summary>
        public float ScaleDelta;
        public int TouchCount;
        /// <summary>Reserved for double-tap detection (future use). 1 for a normal tap.</summary>
        public int TapCount;
    }
}
