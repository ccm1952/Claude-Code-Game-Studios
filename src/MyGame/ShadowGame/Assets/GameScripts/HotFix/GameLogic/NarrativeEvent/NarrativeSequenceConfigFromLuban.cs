// 该文件由Cursor 自动生成
// S5-05 dev-story Step 1 deficiency-flagged interim impl per ADR-029 V2.0 §V2-2
//   背景: narrative-event/story-001 §Engine Notes "⚠️ Required Framework Extension" — Luban TbNarrativeSequence
//   真表未生成；本 provider 用 hardcoded MVP defaults，chapter 1 至少 1 个 sequence 配置可用 +
//   chapter 5 absence + sample chapter transition。
//   Production 升级路径：future ADR-016 Luban tooling 生成 TbNarrativeSequence.tbres → InitFromLuban() 替换 InitWithDefaults()。

using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// Narrative sequence 配置 provider — Luban 接入版（S5-05 interim 用 hardcoded MVP defaults；
    /// future ADR-016 + Luban tooling 生成 TbNarrativeSequence 后通过 <see cref="InitFromLuban"/> 真表加载）。
    /// </summary>
    /// <remarks>
    /// <para>沿 <c>PuzzleStateConfigFromLuban</c> pattern：default + Luban 双重模式；测试用 <see cref="InitWithDefaults"/>，
    /// production 用 <see cref="InitFromLuban"/>（Luban TbNarrativeSequence 真表生成后激活）。</para>
    /// <para>Key: (triggerSourceId &lt;&lt; 8) | sequenceType — 同 puzzle 不同 sequence type 各自独立 config。</para>
    /// </remarks>
    public sealed class NarrativeSequenceConfigFromLuban : INarrativeSequenceConfigProvider
    {
        private readonly Dictionary<long, NarrativeSequenceConfig> _configs = new Dictionary<long, NarrativeSequenceConfig>();
        private bool _initialized;

        /// <summary>
        /// MVP hardcoded defaults — Sprint 5 interim impl（chapter 1 至少 3 个 sequence 配置可用）。
        /// </summary>
        public void InitWithDefaults()
        {
            _configs.Clear();

            // ============================================================
            // Sequence #1: chapter 1 MemoryReplay (puzzleId=1 PerfectMatch 触发)
            //   3 atomic effects: AudioDucking @ t=0 + ScreenFade(in/out) @ t=0 / t=1.5 + Wait @ t=0.5
            //   TotalDuration: 2.0s（PlayMode test 友好）
            // ============================================================
            var memoryReplayCh1 = new NarrativeSequenceConfig(
                id: 100,
                type: NarrativeSequenceType.MemoryReplay,
                effects: new AtomicEffectConfig[]
                {
                    new AudioDuckingEffectConfig(startTime: 0f,    duration: 0.3f, duckRatio: 0.3f, fadeDuration: 0.3f),
                    new ScreenFadeEffectConfig  (startTime: 0f,    duration: 0.5f, startAlpha: 0f,  endAlpha: 1f),     // fade-in
                    new WaitEffectConfig        (startTime: 0.5f,  duration: 1.0f),                                    // hold black
                    new ScreenFadeEffectConfig  (startTime: 1.5f,  duration: 0.5f, startAlpha: 1f,  endAlpha: 0f),     // fade-out
                },
                totalDuration: 2.0f,
                isChapterFinal: false);
            RegisterByTrigger(triggerSourceId: 1, type: NarrativeSequenceType.MemoryReplay, config: memoryReplayCh1);

            // ============================================================
            // Sequence #2: chapter 5 AbsencePuzzle (puzzleId=51 AbsenceAccepted 触发)
            //   2 atomic effects: AudioDucking + Wait（cool, no ObjectSnap per ADR-016 §A line 131）
            //   TotalDuration: 1.5s
            // ============================================================
            var absenceCh5 = new NarrativeSequenceConfig(
                id: 510,
                type: NarrativeSequenceType.AbsencePuzzle,
                effects: new AtomicEffectConfig[]
                {
                    new AudioDuckingEffectConfig(startTime: 0f, duration: 0.3f, duckRatio: 0.2f, fadeDuration: 0.3f),
                    new WaitEffectConfig        (startTime: 0f, duration: 1.5f),
                },
                totalDuration: 1.5f,
                isChapterFinal: false);
            RegisterByTrigger(triggerSourceId: 51, type: NarrativeSequenceType.AbsencePuzzle, config: absenceCh5);

            // ============================================================
            // Sequence #3: ChapterTransition chapter 1 → 2 (chapterId=1 ChapterComplete 触发)
            //   3 atomic effects: ScreenFade(in) + Wait(hold) + ScreenFade(out) — chapter-final 标志
            //   TotalDuration: 3.0s
            // ============================================================
            var transitionCh1 = new NarrativeSequenceConfig(
                id: 1000,
                type: NarrativeSequenceType.ChapterTransition,
                effects: new AtomicEffectConfig[]
                {
                    new ScreenFadeEffectConfig(startTime: 0f,   duration: 0.8f, startAlpha: 0f, endAlpha: 1f),
                    new WaitEffectConfig      (startTime: 0.8f, duration: 1.4f),
                    new ScreenFadeEffectConfig(startTime: 2.2f, duration: 0.8f, startAlpha: 1f, endAlpha: 0f),
                },
                totalDuration: 3.0f,
                isChapterFinal: true);
            RegisterByTrigger(triggerSourceId: 1, type: NarrativeSequenceType.ChapterTransition, config: transitionCh1);

            _initialized = true;
        }

        /// <summary>
        /// Future production path — Luban TbNarrativeSequence.tbres 生成后激活；当前 stub fallback InitWithDefaults。
        /// </summary>
        public void InitFromLuban()
        {
            // TODO[S5-XX+]: 留 future ADR-016 + Luban tooling expand
            // 实际实现伪码：
            //   foreach (var row in ConfigSystem.Instance.Tables.TbNarrativeSequence.DataList)
            //       var effects = row.Effects.Select(BuildAtomicEffectConfig).ToArray();
            //       var cfg = new NarrativeSequenceConfig(row.Id, row.Type, effects, row.Duration, row.IsChapterFinal);
            //       RegisterByTrigger(row.TriggerSourceId, row.Type, cfg);
            //   _initialized = true;
            InitWithDefaults();
        }

        /// <inheritdoc />
        public NarrativeSequenceConfig Resolve(int triggerSourceId, NarrativeSequenceType sequenceType)
        {
            if (!_initialized) InitWithDefaults();
            var key = MakeKey(triggerSourceId, sequenceType);
            return _configs.TryGetValue(key, out var cfg) ? cfg : null;
        }

        /// <inheritdoc />
        public bool HasConfig(int triggerSourceId, NarrativeSequenceType sequenceType)
        {
            if (!_initialized) InitWithDefaults();
            return _configs.ContainsKey(MakeKey(triggerSourceId, sequenceType));
        }

        /// <summary>
        /// 测试辅助：注入自定义 (triggerSourceId, type) → config 映射（绕过 default；测试 fixture 用）。
        /// </summary>
        public void RegisterByTrigger(int triggerSourceId, NarrativeSequenceType type, NarrativeSequenceConfig config)
        {
            if (!_initialized) _initialized = true;
            _configs[MakeKey(triggerSourceId, type)] = config;
        }

        private static long MakeKey(int triggerSourceId, NarrativeSequenceType type)
        {
            // 64-bit composite key: 高 32 位 = triggerSourceId, 低 32 位 = type ordinal
            return ((long)triggerSourceId << 32) | (uint)type;
        }
    }
}
