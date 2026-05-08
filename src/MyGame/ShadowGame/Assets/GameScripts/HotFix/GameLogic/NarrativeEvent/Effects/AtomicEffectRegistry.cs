// 该文件由Cursor 自动生成
using System;
using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// Atomic effect 工厂注册表 — string label → effect 实例工厂（per ADR-016 §A pluggable pattern）。
    /// <para>S5-05 chapter 1 MVP 默认注册 3 类：audio_duck / screen_fade / wait。</para>
    /// <para>Sprint 6+ 扩展时通过 <see cref="Register"/> 添加 9 个剩余 effect type label →
    /// NarrativeSequencePlayer 不需修改即可识别新类型；未注册类型由 <see cref="TryCreate"/> 返回 false 让
    /// player 派发 OnSequenceFailed("effect_type_not_implemented:&lt;label&gt;") + skip。</para>
    /// </summary>
    public sealed class AtomicEffectRegistry
    {
        private readonly Dictionary<string, Func<AtomicEffectConfig, AtomicEffect>> _factories =
            new Dictionary<string, Func<AtomicEffectConfig, AtomicEffect>>();

        public AtomicEffectRegistry()
        {
            RegisterDefaults();
        }

        /// <summary>S5-05 chapter 1 MVP 默认 3 effect types。</summary>
        public void RegisterDefaults()
        {
            Register("audio_duck", cfg => new AudioDuckingEffect((AudioDuckingEffectConfig)cfg));
            Register("screen_fade", cfg => new ScreenFadeEffect((ScreenFadeEffectConfig)cfg));
            Register("wait", cfg => new WaitEffect((WaitEffectConfig)cfg));
        }

        /// <summary>注册 effect type label → 工厂（Sprint 6+ 扩展点）。</summary>
        public void Register(string label, Func<AtomicEffectConfig, AtomicEffect> factory)
        {
            if (string.IsNullOrEmpty(label)) throw new ArgumentException("label must not be null or empty", nameof(label));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            _factories[label] = factory;
        }

        /// <summary>取消注册（测试 / hot reload 场景）。</summary>
        public void Unregister(string label)
        {
            if (string.IsNullOrEmpty(label)) return;
            _factories.Remove(label);
        }

        /// <summary>是否含指定 label 的工厂。</summary>
        public bool Contains(string label) =>
            !string.IsNullOrEmpty(label) && _factories.ContainsKey(label);

        /// <summary>
        /// 尝试根据 config 创建 effect 实例。
        /// </summary>
        /// <returns>true = 成功创建（effect != null）；false = label 未注册或 config null（effect = null）。</returns>
        public bool TryCreate(AtomicEffectConfig config, out AtomicEffect effect)
        {
            if (config == null || string.IsNullOrEmpty(config.EffectType))
            {
                effect = null;
                return false;
            }
            if (_factories.TryGetValue(config.EffectType, out var factory))
            {
                effect = factory(config);
                return effect != null;
            }
            effect = null;
            return false;
        }
    }
}
