// 该文件由Cursor 自动生成
using System;

namespace GameLogic
{
    /// <summary>
    /// Atomic effect 抽象基类（per ADR-016 §A 12 effect types pluggable pattern）。
    /// <para>生命周期: Start() → Tick(dt) loop until IsComplete → (player 自动 dispose；不显式 Stop)。</para>
    /// <para>Effect 实施约定:</para>
    /// <list type="bullet">
    ///   <item>Fire-and-forget effects (e.g. AudioDucking) — OnStart 派发事件后 IsComplete = true，Tick 不动。</item>
    ///   <item>Time-driven effects (e.g. ScreenFade / Wait) — OnStart 初始化状态，Tick 累加 _elapsed，IsComplete 当 _elapsed >= Duration。</item>
    /// </list>
    /// </summary>
    public abstract class AtomicEffect
    {
        /// <summary>关联配置（同一类型 effect 的参数携带）。</summary>
        public AtomicEffectConfig Config { get; }

        /// <summary>是否已 Start 过（防 double-start；player 内置防御）。</summary>
        public bool IsStarted { get; private set; }

        /// <summary>是否已完成（player 决策何时停 Tick + 进入下一阶段）。</summary>
        public abstract bool IsComplete { get; }

        /// <summary>Effect 已运行时间（秒；OnTick 自动累加）。</summary>
        protected float _elapsed;

        protected AtomicEffect(AtomicEffectConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _elapsed = 0f;
        }

        /// <summary>启动 effect — 派发即时事件 / 初始化状态。Idempotent（多次调用仅首次生效）。</summary>
        public void Start()
        {
            if (IsStarted) return;
            IsStarted = true;
            OnStart();
        }

        /// <summary>每帧推进 effect — 时间累加 + 调用 OnTick hook。</summary>
        public void Tick(float deltaTime)
        {
            if (!IsStarted || IsComplete) return;
            if (deltaTime < 0f) return; // 防御负值

            _elapsed += deltaTime;
            OnTick(deltaTime);
        }

        /// <summary>子类实施 — Start 时一次性副作用（fire event / 初始化插值起点）。</summary>
        protected abstract void OnStart();

        /// <summary>子类可选 override — Tick 期间副作用（插值更新 / 周期性派发）。默认 no-op。</summary>
        protected virtual void OnTick(float deltaTime) { }
    }
}
