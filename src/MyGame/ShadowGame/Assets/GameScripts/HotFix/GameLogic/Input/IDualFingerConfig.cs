// 该文件由Cursor 自动生成
namespace GameLogic
{
    /// <summary>
    /// Thresholds specific to dual-finger gesture recognition.
    /// Implement alongside <see cref="IInputConfig"/> on the same config class;
    /// values sourced from Luban <c>TbInputConfig</c> at runtime.
    /// </summary>
    public interface IDualFingerConfig
    {
        /// <summary>Cumulative angle (radians) before a rotate gesture is recognised.</summary>
        float RotateThresholdRad { get; }

        /// <summary>Cumulative scale deviation from 1.0 before a pinch gesture is recognised.</summary>
        float PinchThreshold { get; }

        /// <summary>
        /// Minimum pixel distance between two fingers. Below this distance,
        /// angle and scale deltas are suppressed to avoid jitter.
        /// </summary>
        float MinFingerDistance { get; }
    }
}
