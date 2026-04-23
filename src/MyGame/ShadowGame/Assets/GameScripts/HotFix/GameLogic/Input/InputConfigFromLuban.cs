// 该文件由Cursor 自动生成
using UnityEngine;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// Runtime implementation of <see cref="IInputConfig"/> and <see cref="IDualFingerConfig"/>.
    /// At startup, populated from Luban <c>TbInputConfig</c>. Supports runtime sensitivity
    /// changes via <see cref="ISettingsEvent.OnTouchSensitivityChanged"/>（ADR-027 接口事件协议，
    /// 取代 ADR-006 `Evt_Settings_TouchSensitivityChanged`）。监听注册由 SettingsSystem 侧完成：
    /// <c>GameEventMgr.AddEventListener&lt;ISettingsEvent_Event&gt;(inputConfig.OnSensitivityChanged)</c>。
    /// <para>
    /// Until Luban tables are wired, call <see cref="InitWithDefaults"/> to use
    /// GDD-specified default values (matching TbInputConfig schema defaults).
    /// </para>
    /// </summary>
    public sealed class InputConfigFromLuban : IInputConfig, IDualFingerConfig
    {
        private float _baseDragThresholdMm;
        private float _tapTimeoutSeconds;
        private float _maxDeltaPerFrame;
        private float _fallbackDpi;
        private float _rotateThresholdRad;
        private float _pinchThreshold;
        private float _minFingerDistance;

        private float _sensitivityMultiplier = 1f;
        private float _dragThresholdPx;
        private float _screenDpi;

        // IInputConfig — values are sensitivity-adjusted where applicable
        public float BaseDragThresholdMm => _baseDragThresholdMm;
        public float TapTimeoutSeconds => _tapTimeoutSeconds;
        public float MaxDeltaPerFrame => _maxDeltaPerFrame;
        public float FallbackDpi => _fallbackDpi;

        // IDualFingerConfig
        public float RotateThresholdRad => _rotateThresholdRad;
        public float PinchThreshold => _pinchThreshold;
        public float MinFingerDistance => _minFingerDistance;

        /// <summary>Pre-computed pixel-space drag threshold (DPI + sensitivity applied).</summary>
        public float DragThresholdPx => _dragThresholdPx;

        /// <summary>Current sensitivity multiplier ∈ [0.5, 2.0].</summary>
        public float SensitivityMultiplier => _sensitivityMultiplier;

        /// <summary>
        /// Populate from GDD defaults. Call this until Luban tables are integrated.
        /// </summary>
        public void InitWithDefaults()
        {
            _baseDragThresholdMm = 3.0f;
            _tapTimeoutSeconds = 0.25f;
            _maxDeltaPerFrame = 100f;
            _fallbackDpi = 160f;
            _rotateThresholdRad = 8f * Mathf.Deg2Rad;
            _pinchThreshold = 0.08f;
            _minFingerDistance = 20f;
            _sensitivityMultiplier = 1f;
            _screenDpi = Screen.dpi;
            RecalculateDerivedValues();
        }

        /// <summary>
        /// Populate from a Luban TbInputConfig row.
        /// Call after <c>Tables.Init()</c> completes.
        /// </summary>
        public void InitFromLuban(
            float baseDragThresholdMm,
            float tapTimeout,
            float rotateThresholdDeg,
            float pinchThreshold,
            float minFingerDistance,
            float maxDeltaPerFrame,
            float fallbackDpi)
        {
            _baseDragThresholdMm = baseDragThresholdMm;
            _tapTimeoutSeconds = tapTimeout;
            _maxDeltaPerFrame = maxDeltaPerFrame;
            _fallbackDpi = fallbackDpi;
            _rotateThresholdRad = rotateThresholdDeg * Mathf.Deg2Rad;
            _pinchThreshold = pinchThreshold;
            _minFingerDistance = minFingerDistance;
            _sensitivityMultiplier = 1f;
            _screenDpi = Screen.dpi;
            RecalculateDerivedValues();
        }

        /// <summary>
        /// Handle Settings touch sensitivity change. Clamps to [0.5, 2.0] and
        /// recalculates derived thresholds immediately (same frame).
        /// 签名与 <see cref="ISettingsEvent.OnTouchSensitivityChanged"/> 一致，
        /// 可作为 <c>GameEventMgr.AddEventListener&lt;ISettingsEvent_Event&gt;</c> 的直接 handler。
        /// </summary>
        public void OnSensitivityChanged(float multiplier)
        {
            _sensitivityMultiplier = Mathf.Clamp(multiplier, 0.5f, 2.0f);
            RecalculateDerivedValues();
        }

        /// <summary>
        /// Recalculate <see cref="DragThresholdPx"/> from physical mm, DPI, and sensitivity.
        /// Call when DPI or sensitivity changes.
        /// </summary>
        public void RecalculateDerivedValues()
        {
            float dpi = _screenDpi > 0f ? _screenDpi : _fallbackDpi;
            _dragThresholdPx = _baseDragThresholdMm * dpi / 25.4f * _sensitivityMultiplier;
        }

        /// <summary>Override the cached DPI (useful for testing or display changes).</summary>
        public void SetScreenDpi(float dpi)
        {
            _screenDpi = dpi;
            RecalculateDerivedValues();
        }
    }
}
