// 该文件由Cursor 自动生成
namespace GameLogic
{
    /// <summary>
    /// Wait effect — 仅消耗时间，无副作用 — 用于 sequence 内插入 hold / pause / pacing 间隔。
    /// <para>Time-driven：OnStart 仅初始化（_elapsed = 0）；OnTick 累加；IsComplete 当 _elapsed >= Duration。</para>
    /// </summary>
    public sealed class WaitEffect : AtomicEffect
    {
        public WaitEffect(WaitEffectConfig config) : base(config) { }

        public override bool IsComplete => _elapsed >= Config.Duration;

        protected override void OnStart() { /* no-op — time-only */ }
    }
}
