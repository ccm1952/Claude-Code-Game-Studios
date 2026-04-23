// 该文件由Cursor 自动生成
namespace GameLogic
{
    /// <summary>
    /// Payload for <see cref="IShadowRTEvent.OnShadowRTUpdated"/>（ADR-027 接口事件协议，取代 ADR-006 `Evt_ShadowRT_Updated`）。
    /// Carries a copy of the ShadowRT pixel buffer for CPU-side match scoring.
    /// </summary>
    public struct ShadowRTData
    {
        /// <summary>R8 pixel data (0 = full shadow, 255 = fully lit). Length = Width * Height.</summary>
        public byte[] Pixels;
        public int Width;
        public int Height;
        /// <summary>Frame number when this readback was initiated.</summary>
        public int FrameNumber;
        /// <summary>True if this is stale data from the previous frame's cache (readback error fallback).</summary>
        public bool IsStaleCache;
    }
}
