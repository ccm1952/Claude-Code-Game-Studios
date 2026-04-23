// 该文件由Cursor 自动生成
namespace GameLogic
{
    /// <summary>
    /// Runtime-tunable thresholds for the input gesture system.
    /// Implement this interface on a ScriptableObject or config class;
    /// inject into FSMs at construction time so no threshold is ever hardcoded.
    /// </summary>
    public interface IInputConfig
    {
        /// <summary>
        /// Minimum finger travel in millimetres before a touch is classified as a drag.
        /// Converted to pixels at runtime using the device DPI.
        /// </summary>
        float BaseDragThresholdMm { get; }

        /// <summary>
        /// Maximum time in seconds a touch may remain below the drag threshold
        /// and still be recognised as a tap.  Uses unscaled time.
        /// </summary>
        float TapTimeoutSeconds { get; }

        /// <summary>
        /// Upper limit on per-frame pointer delta (pixels).
        /// Prevents physics-breaking spikes from swipe noise or frame drops.
        /// </summary>
        float MaxDeltaPerFrame { get; }

        /// <summary>
        /// DPI assumed when <c>Screen.dpi</c> returns 0 (e.g. in the Editor).
        /// </summary>
        float FallbackDpi { get; }
    }
}
