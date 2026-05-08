// 该文件由Cursor 自动生成
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// AudioDucking effect — 派发 IAudioEvent.OnDuckingRequest 触发背景音量降低。
    /// <para>Fire-and-forget：OnStart 派发事件后立即 IsComplete = true（fade 时序由 listener AudioManager 内部 driving）。</para>
    /// </summary>
    public sealed class AudioDuckingEffect : AtomicEffect
    {
        private bool _fired;

        public AudioDuckingEffect(AudioDuckingEffectConfig config) : base(config) { }

        public override bool IsComplete => _fired;

        protected override void OnStart()
        {
            var cfg = (AudioDuckingEffectConfig)Config;
            // S5-05 阶段 IAudioEvent 仅 stub (OnDuckingRequest 1 method)；S5-06 production code 后扩展到 8 method full contract.
            GameEvent.Get<IAudioEvent>().OnDuckingRequest(cfg.DuckRatio, cfg.FadeDuration);
            _fired = true;
        }
    }
}
