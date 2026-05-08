// 该文件由Cursor 自动生成
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// ScreenFade effect — 全屏 alpha 插值（fade-in / fade-out / hold）。
    /// <para>Time-driven：OnStart 设 CurrentAlpha = StartAlpha；OnTick 按 _elapsed/Duration 插值；
    /// IsComplete 当 _elapsed >= Duration（最终值锁定 EndAlpha）。</para>
    /// <para>S5-05 阶段：仅记录逻辑 alpha（CurrentAlpha public read-only）；
    /// 实际 UI canvas group 接入留 future Sprint 6+（NarrativeSequencePlayer 不依赖 UI canvas，
    /// PlayMode test 直接 inspect CurrentAlpha verify 时间曲线）。</para>
    /// </summary>
    public sealed class ScreenFadeEffect : AtomicEffect
    {
        /// <summary>当前 alpha 值 [0.0, 1.0]（OnTick 期间持续插值；外部读取 verify 曲线）。</summary>
        public float CurrentAlpha { get; private set; }

        public ScreenFadeEffect(ScreenFadeEffectConfig config) : base(config)
        {
            CurrentAlpha = config.StartAlpha;
        }

        public override bool IsComplete => _elapsed >= Config.Duration;

        protected override void OnStart()
        {
            var cfg = (ScreenFadeEffectConfig)Config;
            CurrentAlpha = cfg.StartAlpha;
        }

        protected override void OnTick(float deltaTime)
        {
            var cfg = (ScreenFadeEffectConfig)Config;
            // Duration 接近 0 防除零 — 任意 _elapsed > 0 直接落 EndAlpha
            var safeDuration = Mathf.Max(0.0001f, cfg.Duration);
            var t = Mathf.Clamp01(_elapsed / safeDuration);
            CurrentAlpha = Mathf.Lerp(cfg.StartAlpha, cfg.EndAlpha, t);
        }
    }
}
