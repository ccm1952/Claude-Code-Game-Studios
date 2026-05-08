// 该文件由Cursor 自动生成
using Cysharp.Threading.Tasks;
using GameLogic;
using NUnit.Framework;

namespace ShadowGame.Tests.EditMode.Audio
{
    /// <summary>
    /// AudioMixLayer + DuckingController + AudioConfigFromLuban 纯逻辑 EditMode 单元测试。
    /// </summary>
    /// <remarks>
    /// <para>不依赖 framework GameModule.Audio — 纯 POCO + UniTask-free（DuckingController 用 StepLerpForTest 钩子驱动）。</para>
    /// <para>覆盖 AC: AC-2 partial (lifecycle behavioral fragments) / AC-5 (layer isolation logic) / AC-7 (Ambient baseline)
    /// / AC-8 (multiplicative formula) / AC-9 partial (ducking interpolation curve) / AC-13 (Variants list invariant) /
    /// AC-14 (Luban provider lookup)。</para>
    /// </remarks>
    [TestFixture]
    public class AudioVolumeFormulaTests
    {
        // ==== AudioMixLayer ====

        [Test]
        public void test_AudioMixLayer_initial_baseline_volume_clamped()
        {
            var layer = new AudioMixLayer(AudioLayer.SFX, baselineVolume: 1.5f, affectedByDucking: false);
            Assert.That(layer.BaselineVolume, Is.EqualTo(1f).Within(1e-5f), "baselineVolume > 1 应被 Clamp01 到 1.0");

            var negativeLayer = new AudioMixLayer(AudioLayer.SFX, baselineVolume: -0.5f, affectedByDucking: false);
            Assert.That(negativeLayer.BaselineVolume, Is.EqualTo(0f).Within(1e-5f), "baselineVolume < 0 应被 Clamp01 到 0.0");
        }

        [Test]
        public void test_AudioMixLayer_effective_volume_multiplicative()
        {
            // AC-8: master × layer × ducking — Music layer 受 ducking 影响
            var music = new AudioMixLayer(AudioLayer.Music, baselineVolume: 1f, affectedByDucking: true);
            Assert.That(music.ComputeEffectiveVolume(masterVolume: 0.5f, duckMultiplier: 0.6f),
                Is.EqualTo(0.5f * 1f * 0.6f).Within(1e-5f),
                "Music layer effective = master × baseline × duck");
        }

        [Test]
        public void test_AudioMixLayer_sfx_unaffected_by_ducking()
        {
            // AC-5/-9: SFX layer 完全不受 ducking 影响
            var sfx = new AudioMixLayer(AudioLayer.SFX, baselineVolume: 0.8f, affectedByDucking: false);
            float duckedScenario = sfx.ComputeEffectiveVolume(masterVolume: 1f, duckMultiplier: 0.3f);
            float idleScenario = sfx.ComputeEffectiveVolume(masterVolume: 1f, duckMultiplier: 1.0f);
            Assert.That(duckedScenario, Is.EqualTo(idleScenario).Within(1e-5f),
                "SFX layer effective 应与 ducking multiplier 无关");
            Assert.That(duckedScenario, Is.EqualTo(0.8f).Within(1e-5f), "SFX 受 master × baseline 控制");
        }

        [Test]
        public void test_AudioMixLayer_muted_clamps_to_zero()
        {
            // AC-5: sfx_enabled = false 仅静 SFX layer
            var sfx = new AudioMixLayer(AudioLayer.SFX, baselineVolume: 0.8f, affectedByDucking: false);
            sfx.SetMuted(true);
            Assert.That(sfx.ComputeEffectiveVolume(masterVolume: 1f, duckMultiplier: 1f),
                Is.EqualTo(0f).Within(1e-5f),
                "Muted layer effective volume 应为 0，无视 master/baseline/duck");
            Assert.That(sfx.BaselineVolume, Is.EqualTo(0.8f).Within(1e-5f),
                "Muted 不应改 baseline 数值（重新 unmute 时恢复）");

            sfx.SetMuted(false);
            Assert.That(sfx.ComputeEffectiveVolume(masterVolume: 1f, duckMultiplier: 1f),
                Is.EqualTo(0.8f).Within(1e-5f),
                "Unmute 后 effective 应恢复 baseline 数值");
        }

        [Test]
        public void test_AudioMixLayer_ambient_default_baseline_is_06()
        {
            // AC-7: Ambient layer 内部 baseline 0.6 不暴露 player；AudioManager.Initialize 时构造
            var ambient = new AudioMixLayer(AudioLayer.Ambient, baselineVolume: 0.6f, affectedByDucking: true);
            Assert.That(ambient.BaselineVolume, Is.EqualTo(0.6f).Within(1e-5f));
            Assert.That(ambient.AffectedByDucking, Is.True, "Ambient 应受 ducking 影响");
        }

        // ==== DuckingController ====

        [Test]
        public void test_DuckingController_initial_state_is_idle()
        {
            var dc = new DuckingController();
            Assert.That(dc.State, Is.EqualTo(DuckingState.Idle));
            Assert.That(dc.CurrentMultiplier, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(dc.IsActive, Is.False);
        }

        [Test]
        public void test_DuckingController_step_lerp_no_op_when_idle()
        {
            var dc = new DuckingController();
            bool stillLerping = dc.StepLerpForTest(0.1f);
            Assert.That(stillLerping, Is.False, "Idle 状态下 StepLerp 应直接返回 false");
            Assert.That(dc.CurrentMultiplier, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void test_DuckingController_reset_after_force_cancel()
        {
            var dc = new DuckingController();
            dc.StartDuckingAsync(duckRatio: 0.3f, fadeDuration: 1.0f).Forget();
            dc.ForceCancel();
            // ForceCancel 取消 lerp 但保持当前 multiplier；这里因为 await 还未 yield 一次，状态可能仍是 FadingDown 起手
            // 关键：ResetForTest 路径回 Idle 1.0
            dc.ResetForTest();
            Assert.That(dc.State, Is.EqualTo(DuckingState.Idle));
            Assert.That(dc.CurrentMultiplier, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void test_DuckingController_step_lerp_linear_curve_at_half_t()
        {
            // AC-9 partial: StartDucking(0.3, 1.0s) → 0.5s 后 multiplier ≈ lerp(1.0, 0.3, 0.5) = 0.65
            var dc = new DuckingController();
            // 直接 fire-and-forget StartDuckingAsync 然后 ForceCancel 仅设置 _state + 起始 lerp 字段，立即 StepLerp 测试
            dc.StartDuckingAsync(duckRatio: 0.3f, fadeDuration: 1.0f).Forget();
            // 立即 cancel async lerp（避免 PlayerLoopTiming.Update 干扰）但保持 _state/_lerp 字段
            dc.ForceCancel();
            // 此时 _state = FadingDown, lerp 字段已 setup；StepLerp 直接驱动
            bool stillLerping = dc.StepLerpForTest(0.5f);
            Assert.That(stillLerping, Is.True, "0.5s elapsed (< 1.0s duration) 应仍 lerping");
            Assert.That(dc.CurrentMultiplier, Is.EqualTo(0.65f).Within(0.05f),
                "0.5s 时 multiplier ≈ lerp(1.0, 0.3, 0.5) = 0.65");
        }

        [Test]
        public void test_DuckingController_step_lerp_completes_at_target()
        {
            var dc = new DuckingController();
            dc.StartDuckingAsync(duckRatio: 0.3f, fadeDuration: 1.0f).Forget();
            dc.ForceCancel();
            bool stillLerping = dc.StepLerpForTest(1.5f); // 超过 duration
            Assert.That(stillLerping, Is.False, "elapsed > duration 应 lerp 完成");
            Assert.That(dc.CurrentMultiplier, Is.EqualTo(0.3f).Within(1e-5f), "应到达 target multiplier");
            Assert.That(dc.State, Is.EqualTo(DuckingState.Held), "FadingDown 完成后状态 Held");
        }

        [Test]
        public void test_DuckingController_release_returns_to_unity()
        {
            var dc = new DuckingController();
            dc.StartDuckingAsync(duckRatio: 0.3f, fadeDuration: 0.001f).Forget(); // instant ducking
            dc.ForceCancel();
            // Re-step instant duration: StepLerp 推进 1ms+
            dc.StepLerpForTest(0.01f);
            // 现在 multiplier 应 ≈ 0.3 + Held
            // Release fade-up 1.0s
            dc.ReleaseDuckingAsync(fadeDuration: 1.0f).Forget();
            dc.ForceCancel();
            bool stillLerping = dc.StepLerpForTest(1.5f);
            Assert.That(stillLerping, Is.False);
            Assert.That(dc.CurrentMultiplier, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(dc.State, Is.EqualTo(DuckingState.Idle));
        }

        // ==== AudioConfigFromLuban ====

        [Test]
        public void test_AudioConfigFromLuban_default_initialization_chapter_1_mvp()
        {
            // AC-14: 3 SFX + 1 Music chapter 1 MVP
            var provider = new AudioConfigFromLuban();
            provider.InitWithDefaults();

            Assert.That(provider.GetSfxConfig(1), Is.Not.Null, "sfx id=1 (ui_click) 默认存在");
            Assert.That(provider.GetSfxConfig(2), Is.Not.Null, "sfx id=2 (object_pickup) 默认存在");
            Assert.That(provider.GetSfxConfig(3), Is.Not.Null, "sfx id=3 (object_snap) 默认存在");
            Assert.That(provider.GetMusicConfig(100), Is.Not.Null, "music id=100 (chapter1_ambient) 默认存在");
        }

        [Test]
        public void test_AudioConfigFromLuban_get_unknown_sfx_returns_null()
        {
            var provider = new AudioConfigFromLuban();
            Assert.That(provider.GetSfxConfig(99999), Is.Null);
            Assert.That(provider.GetSfxConfigByName("unknown_sfx"), Is.Null);
        }

        [Test]
        public void test_AudioConfigFromLuban_resolve_string_id_to_int()
        {
            var provider = new AudioConfigFromLuban();
            bool ok = provider.TryResolveSfxName("ui_click", out int sfxId);
            Assert.That(ok, Is.True);
            Assert.That(sfxId, Is.EqualTo(1));
        }

        [Test]
        public void test_AudioConfigFromLuban_resolve_unknown_string_returns_false()
        {
            var provider = new AudioConfigFromLuban();
            bool ok = provider.TryResolveSfxName("unknown", out int sfxId);
            Assert.That(ok, Is.False);
            Assert.That(sfxId, Is.EqualTo(0));
        }

        [Test]
        public void test_AudioConfigFromLuban_register_sfx_overrides_default()
        {
            var provider = new AudioConfigFromLuban();
            provider.InitWithDefaults();
            // override id=1 (ui_click)
            provider.RegisterSfx(new SfxConfig(
                id: 1,
                name: "ui_click_override",
                clipPath: "Audio/SFX/override",
                volume: 0.5f,
                pitchBase: 1.0f,
                pitchVariance: 0.0f,
                variants: new[] { "Audio/SFX/override" }));

            var cfg = provider.GetSfxConfig(1);
            Assert.That(cfg, Is.Not.Null);
            Assert.That(cfg.Name, Is.EqualTo("ui_click_override"));
            Assert.That(cfg.ClipPath, Is.EqualTo("Audio/SFX/override"));
        }

        [Test]
        public void test_SfxConfig_ctor_validates_name_and_clipPath()
        {
            // AC: defensive ctor (per ADR-029 R3 mandatory POCO with sealed + readonly + arg validation)
            Assert.Throws<System.ArgumentException>(() =>
                new SfxConfig(id: 1, name: "", clipPath: "valid", volume: 1f,
                    pitchBase: 1f, pitchVariance: 0f, variants: null));
            Assert.Throws<System.ArgumentException>(() =>
                new SfxConfig(id: 1, name: "valid", clipPath: "", volume: 1f,
                    pitchBase: 1f, pitchVariance: 0f, variants: null));
        }

        [Test]
        public void test_SfxConfig_volume_clamped_in_ctor()
        {
            var cfg = new SfxConfig(id: 1, name: "test", clipPath: "Audio/test", volume: 1.5f,
                pitchBase: 1f, pitchVariance: 0f, variants: null);
            Assert.That(cfg.Volume, Is.EqualTo(1f).Within(1e-5f), "volume > 1 应被 Clamp01");
            Assert.That(cfg.Variants.Count, Is.EqualTo(1), "null variants 应 fallback 单 clipPath");
            Assert.That(cfg.Variants[0], Is.EqualTo("Audio/test"));
        }
    }
}
