// 该文件由Cursor 自动生成
namespace GameLogic
{
    /// <summary>
    /// Atomic effect 配置抽象基类（per ADR-016 §A 12 atomic effects 设计）。
    /// <para>每个具体 effect type 一个子类（保留 12 type label，本 Sprint 5 chapter 1 MVP 仅实现
    /// AudioDucking / ScreenFade / Wait 3 类的 concrete config；其余 9 类用 placeholder subclass —
    /// dispatch 时若 config 类型未注册到 AtomicEffectRegistry 则 fire OnSequenceFailed("effect_type_not_implemented")
    /// + skip + 继续 sequence per AC-12 resilience）。</para>
    /// <para>Sealed + readonly POCO 模式（per ADR-029 V2.0 R3 mandatory）—
    /// 测试用 constructor (new XxxConfig(...)) 不用 object initializer。</para>
    /// </summary>
    public abstract class AtomicEffectConfig
    {
        /// <summary>Effect 类型 label（e.g. "audio_duck" / "screen_fade" / "wait"）— 用于 AtomicEffectRegistry dispatch。</summary>
        public string EffectType { get; }

        /// <summary>Sequence 内启动时间偏移（秒，相对 sequence 开始）— time-sorted 排序键。</summary>
        public float StartTime { get; }

        /// <summary>Effect 持续时长（秒）— Tick 累加 _elapsed 跨过此值时 IsComplete = true（具体由 effect 实施类决定）。</summary>
        public float Duration { get; }

        protected AtomicEffectConfig(string effectType, float startTime, float duration)
        {
            EffectType = effectType ?? string.Empty;
            StartTime = startTime;
            Duration = duration;
        }
    }

    // ============================================================
    // Chapter 1 MVP — 3 concrete effect configs (S5-05 dev-story 实施)
    // ============================================================

    /// <summary>
    /// AudioDucking effect config — 触发 IAudioEvent.OnDuckingRequest 降低背景音量。
    /// <para>Effect type: "audio_duck"；Fire-and-forget — Execute 后立即 IsComplete = true。</para>
    /// </summary>
    public sealed class AudioDuckingEffectConfig : AtomicEffectConfig
    {
        /// <summary>Ducking 后音量倍数 [0.0, 1.0]。</summary>
        public float DuckRatio { get; }

        /// <summary>Fade 时长（秒）— 由 listener AudioManager 内部 driving，effect 不实测。</summary>
        public float FadeDuration { get; }

        public AudioDuckingEffectConfig(float startTime, float duration, float duckRatio, float fadeDuration)
            : base("audio_duck", startTime, duration)
        {
            DuckRatio = duckRatio;
            FadeDuration = fadeDuration;
        }
    }

    /// <summary>
    /// ScreenFade effect config — fade alpha curve（fade-in / fade-out / hold）。
    /// <para>Effect type: "screen_fade"；Tick driving alpha 插值；IsComplete 当 _elapsed >= Duration。</para>
    /// </summary>
    public sealed class ScreenFadeEffectConfig : AtomicEffectConfig
    {
        /// <summary>起始 alpha [0.0, 1.0]（0=透明, 1=黑屏）。</summary>
        public float StartAlpha { get; }

        /// <summary>结束 alpha [0.0, 1.0]。</summary>
        public float EndAlpha { get; }

        public ScreenFadeEffectConfig(float startTime, float duration, float startAlpha, float endAlpha)
            : base("screen_fade", startTime, duration)
        {
            StartAlpha = startAlpha;
            EndAlpha = endAlpha;
        }
    }

    /// <summary>
    /// Wait effect config — 仅消耗时间，无副作用 — 用于 sequence 内插入 hold / pause / pacing 间隔。
    /// <para>Effect type: "wait"；Tick 仅累加 _elapsed；IsComplete 当 _elapsed >= Duration。</para>
    /// </summary>
    public sealed class WaitEffectConfig : AtomicEffectConfig
    {
        public WaitEffectConfig(float startTime, float duration)
            : base("wait", startTime, duration)
        {
        }
    }

    // ============================================================
    // 9 placeholder configs — Sprint 6+ 完整实施
    // ============================================================
    // ColorTemperature / SFXOneShot / CameraShake / TextureVideo / ObjectSnap /
    // LightIntensity / ShadowFade / ObjectFade / Timeline
    //
    // 当 NarrativeSequenceConfigFromLuban 给出未注册的 EffectType label 时，
    // NarrativeSequencePlayer 通过 AtomicEffectRegistry.TryDispatch 返回 false →
    // 派发 INarrativeEvent.OnSequenceFailed(seqId, "effect_type_not_implemented:<label>") +
    // Log.Warning + skip 该 effect 继续 sequence（与 AC-12 resource_load_failed 同 resilience 模式）。
    //
    // Sprint 6+ dev-story 实施时新增 9 个 sealed class : AtomicEffectConfig 子类即可，
    // 无需修改 NarrativeSequencePlayer / AtomicEffectRegistry — 只需 Registry.Register(label, factory) 注册。
    // ============================================================
}
