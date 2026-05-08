// 该文件由Cursor 自动生成
// S5-05 EditMode tests — Narrative Sequence Engine pure logic（config / sort invariant / atomic effect
// Tick driving / Registry dispatch / Luban provider defaults）。
//
// AC coverage scope (EditMode subset):
//   AC-1 INarrativeEvent contract (4 method signature) — verified by 编译通过 (no test needed; SG generates _Event class)
//   AC-2 sequence player ≤ 1 frame trigger latency — PlayMode spike (require GameEvent dispatch)
//   AC-3 3 sequence types branch — Luban provider 默认 3 sequences cover via test #13
//   AC-4 12 atomic effects isolation — 3 chapter 1 MVP cover (audio_duck / screen_fade / wait); 9 placeholder 由 Registry 缺省 → SequenceFailed (PlayMode spike cover)
//   AC-5/AC-6 parallel/sequential effects — sort invariant test #2
//   AC-7/AC-8/AC-9 token locking + InputBlocker — PlayMode spike (require GameEvent fire)
//   AC-10/AC-11 queue + overflow — PlayMode spike (require listener wire-up)
//   AC-12 resource load failure — Registry TryCreate fail test #12 + PlayMode integration
//   AC-13 VideoPlayer memory — PlayMode advisory (Unity Profiler)
//   AC-14 app pause/resume — PlayMode spike
//   AC-15 listener self-removal pattern — PlayMode spike (TestNarrativeScopedFixture documented pattern)

using System.Collections.Generic;
using NUnit.Framework;
using GameLogic;

namespace ShadowGame.Tests.EditMode.NarrativeEvent
{
    /// <summary>
    /// Narrative sequence engine EditMode tests — 13 unit tests cover 配置 / 排序 / 时间推进 / Registry
    /// dispatch / Luban provider 默认值。事件派发 / 监听器交互留 PlayMode S5-05 spike。
    /// </summary>
    [TestFixture]
    public class NarrativeSequenceConfigTests
    {
        // ──────────── NarrativeSequenceConfig POCO ────────────

        [Test]
        public void NarrativeSequenceConfig_Constructor_5Arg_AssignsAllProperties()
        {
            var effects = new AtomicEffectConfig[]
            {
                new WaitEffectConfig(startTime: 0f, duration: 0.5f),
            };

            var config = new NarrativeSequenceConfig(
                id: 42,
                type: NarrativeSequenceType.MemoryReplay,
                effects: effects,
                totalDuration: 1.5f,
                isChapterFinal: true);

            Assert.AreEqual(42, config.Id);
            Assert.AreEqual(NarrativeSequenceType.MemoryReplay, config.Type);
            Assert.AreEqual(1.5f, config.TotalDuration);
            Assert.IsTrue(config.IsChapterFinal);
            Assert.AreEqual(1, config.Effects.Count);
        }

        [Test]
        public void NarrativeSequenceConfig_Constructor_EffectsSortedByStartTime()
        {
            // 故意乱序传入 — ctor 应按 StartTime 升序排列
            var unsorted = new AtomicEffectConfig[]
            {
                new WaitEffectConfig(startTime: 1.5f, duration: 0.5f),
                new WaitEffectConfig(startTime: 0.0f, duration: 0.5f),
                new WaitEffectConfig(startTime: 0.8f, duration: 0.3f),
            };

            var config = new NarrativeSequenceConfig(
                id: 1,
                type: NarrativeSequenceType.MemoryReplay,
                effects: unsorted,
                totalDuration: 2.0f,
                isChapterFinal: false);

            Assert.AreEqual(0.0f, config.Effects[0].StartTime);
            Assert.AreEqual(0.8f, config.Effects[1].StartTime);
            Assert.AreEqual(1.5f, config.Effects[2].StartTime);
        }

        [Test]
        public void NarrativeSequenceConfig_NullEffects_DefaultsToEmptyList()
        {
            var config = new NarrativeSequenceConfig(
                id: 1,
                type: NarrativeSequenceType.MemoryReplay,
                effects: null,
                totalDuration: 0.5f,
                isChapterFinal: false);

            Assert.IsNotNull(config.Effects);
            Assert.AreEqual(0, config.Effects.Count);
        }

        // ──────────── AtomicEffectConfig 子类 constructor ────────────

        [Test]
        public void AudioDuckingEffectConfig_Constructor_AssignsLabelAndParams()
        {
            var cfg = new AudioDuckingEffectConfig(startTime: 0f, duration: 0.3f, duckRatio: 0.3f, fadeDuration: 0.5f);

            Assert.AreEqual("audio_duck", cfg.EffectType);
            Assert.AreEqual(0f, cfg.StartTime);
            Assert.AreEqual(0.3f, cfg.Duration);
            Assert.AreEqual(0.3f, cfg.DuckRatio);
            Assert.AreEqual(0.5f, cfg.FadeDuration);
        }

        [Test]
        public void ScreenFadeEffectConfig_Constructor_AssignsLabelAndParams()
        {
            var cfg = new ScreenFadeEffectConfig(startTime: 0.5f, duration: 1.0f, startAlpha: 0f, endAlpha: 1f);

            Assert.AreEqual("screen_fade", cfg.EffectType);
            Assert.AreEqual(0.5f, cfg.StartTime);
            Assert.AreEqual(1.0f, cfg.Duration);
            Assert.AreEqual(0f, cfg.StartAlpha);
            Assert.AreEqual(1f, cfg.EndAlpha);
        }

        [Test]
        public void WaitEffectConfig_Constructor_AssignsLabel()
        {
            var cfg = new WaitEffectConfig(startTime: 1.0f, duration: 2.0f);

            Assert.AreEqual("wait", cfg.EffectType);
            Assert.AreEqual(1.0f, cfg.StartTime);
            Assert.AreEqual(2.0f, cfg.Duration);
        }

        // ──────────── AtomicEffect 时间推进 + IsComplete ────────────

        [Test]
        public void WaitEffect_Tick_AccumulatesTime_IsCompleteWhenElapsedReachesDuration()
        {
            var effect = new WaitEffect(new WaitEffectConfig(startTime: 0f, duration: 1.0f));
            effect.Start();

            Assert.IsTrue(effect.IsStarted);
            Assert.IsFalse(effect.IsComplete);

            effect.Tick(0.5f);
            Assert.IsFalse(effect.IsComplete);

            effect.Tick(0.5f);
            Assert.IsTrue(effect.IsComplete);
        }

        [Test]
        public void WaitEffect_NotStarted_TickDoesNotAccumulate()
        {
            var effect = new WaitEffect(new WaitEffectConfig(startTime: 0f, duration: 1.0f));

            // 不调用 Start — Tick 应被 ignore
            effect.Tick(0.5f);
            effect.Tick(0.5f);

            Assert.IsFalse(effect.IsStarted);
            Assert.IsFalse(effect.IsComplete);
        }

        [Test]
        public void ScreenFadeEffect_AlphaInterpolatesFromStartToEnd()
        {
            var effect = new ScreenFadeEffect(new ScreenFadeEffectConfig(
                startTime: 0f, duration: 1.0f, startAlpha: 0f, endAlpha: 1f));
            effect.Start();
            Assert.AreEqual(0f, effect.CurrentAlpha, 0.01f, "OnStart 应初始化为 StartAlpha");

            effect.Tick(0.25f);
            Assert.AreEqual(0.25f, effect.CurrentAlpha, 0.01f, "Tick 25% 时 alpha 应 ~0.25");

            effect.Tick(0.25f);
            Assert.AreEqual(0.5f, effect.CurrentAlpha, 0.01f, "Tick 50% 时 alpha 应 ~0.5");
        }

        [Test]
        public void ScreenFadeEffect_AlphaClampsAtEndWhenElapsedExceedsDuration()
        {
            var effect = new ScreenFadeEffect(new ScreenFadeEffectConfig(
                startTime: 0f, duration: 0.5f, startAlpha: 0f, endAlpha: 1f));
            effect.Start();
            effect.Tick(2.0f); // _elapsed (2.0) 远超 Duration (0.5) — Tick once 后已 IsComplete

            Assert.IsTrue(effect.IsComplete);
            Assert.AreEqual(1f, effect.CurrentAlpha, 0.01f, "_elapsed > Duration 时 alpha 应 clamp 到 EndAlpha");
        }

        // ──────────── AtomicEffectRegistry ────────────

        [Test]
        public void AtomicEffectRegistry_DefaultsContain3MvpEffectTypes()
        {
            var registry = new AtomicEffectRegistry();

            Assert.IsTrue(registry.Contains("audio_duck"));
            Assert.IsTrue(registry.Contains("screen_fade"));
            Assert.IsTrue(registry.Contains("wait"));
            Assert.IsFalse(registry.Contains("camera_shake"), "camera_shake 留 Sprint 6+ — 不应在 default");
        }

        [Test]
        public void AtomicEffectRegistry_TryCreate_UnknownLabel_ReturnsFalseAndNullEffect()
        {
            var registry = new AtomicEffectRegistry();
            // 自造一个 placeholder config 用未注册 label — 模拟 9 placeholder 类型未 implement 场景
            var unknownConfig = new WaitEffectConfig(startTime: 0f, duration: 1f);
            // 强行 swap label — 通过反射不便，这里改用 Unregister 后再 TryCreate 模拟"未注册"
            registry.Unregister("wait");
            var ok = registry.TryCreate(unknownConfig, out var effect);

            Assert.IsFalse(ok, "未注册 label 应返回 false");
            Assert.IsNull(effect, "失败时 effect 应为 null");
        }

        [Test]
        public void AtomicEffectRegistry_TryCreate_KnownLabel_ReturnsCorrectEffectType()
        {
            var registry = new AtomicEffectRegistry();
            var waitCfg = new WaitEffectConfig(startTime: 0f, duration: 1f);
            var fadeCfg = new ScreenFadeEffectConfig(startTime: 0f, duration: 1f, startAlpha: 0f, endAlpha: 1f);

            Assert.IsTrue(registry.TryCreate(waitCfg, out var waitEff));
            Assert.IsInstanceOf<WaitEffect>(waitEff);

            Assert.IsTrue(registry.TryCreate(fadeCfg, out var fadeEff));
            Assert.IsInstanceOf<ScreenFadeEffect>(fadeEff);
        }

        // ──────────── NarrativeSequenceConfigFromLuban provider ────────────

        [Test]
        public void NarrativeSequenceConfigFromLuban_InitWithDefaults_Provides3MvpSequences()
        {
            var provider = new NarrativeSequenceConfigFromLuban();
            provider.InitWithDefaults();

            Assert.IsTrue(provider.HasConfig(triggerSourceId: 1, sequenceType: NarrativeSequenceType.MemoryReplay));
            Assert.IsTrue(provider.HasConfig(triggerSourceId: 51, sequenceType: NarrativeSequenceType.AbsencePuzzle));
            Assert.IsTrue(provider.HasConfig(triggerSourceId: 1, sequenceType: NarrativeSequenceType.ChapterTransition));

            var memReplay = provider.Resolve(triggerSourceId: 1, sequenceType: NarrativeSequenceType.MemoryReplay);
            Assert.IsNotNull(memReplay);
            Assert.AreEqual(NarrativeSequenceType.MemoryReplay, memReplay.Type);
            Assert.GreaterOrEqual(memReplay.Effects.Count, 3, "MemoryReplay 应含至少 3 atomic effects per chapter 1 MVP");

            var absence = provider.Resolve(triggerSourceId: 51, sequenceType: NarrativeSequenceType.AbsencePuzzle);
            Assert.IsNotNull(absence);
            Assert.AreEqual(NarrativeSequenceType.AbsencePuzzle, absence.Type);

            var transition = provider.Resolve(triggerSourceId: 1, sequenceType: NarrativeSequenceType.ChapterTransition);
            Assert.IsNotNull(transition);
            Assert.IsTrue(transition.IsChapterFinal, "ChapterTransition 应标 IsChapterFinal=true");
        }

        [Test]
        public void NarrativeSequenceConfigFromLuban_RegisterByTrigger_OverridesAndResolves()
        {
            var provider = new NarrativeSequenceConfigFromLuban();
            var custom = new NarrativeSequenceConfig(
                id: 999,
                type: NarrativeSequenceType.MemoryReplay,
                effects: new AtomicEffectConfig[] { new WaitEffectConfig(0f, 0.1f) },
                totalDuration: 0.1f,
                isChapterFinal: false);

            provider.RegisterByTrigger(triggerSourceId: 7, type: NarrativeSequenceType.MemoryReplay, config: custom);

            var resolved = provider.Resolve(triggerSourceId: 7, sequenceType: NarrativeSequenceType.MemoryReplay);
            Assert.IsNotNull(resolved);
            Assert.AreEqual(999, resolved.Id);
            Assert.AreEqual(0.1f, resolved.TotalDuration);

            // 未注册的 (7, AbsencePuzzle) 应仍 null
            Assert.IsNull(provider.Resolve(triggerSourceId: 7, sequenceType: NarrativeSequenceType.AbsencePuzzle));
        }
    }
}
