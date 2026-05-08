// 该文件由Cursor 自动生成
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Ducking 状态枚举 — 控制器内部状态机。
    /// </summary>
    public enum DuckingState
    {
        /// <summary>无 ducking，multiplier = 1.0。</summary>
        Idle = 0,

        /// <summary>正在 fade-down 到 duckRatio。</summary>
        FadingDown = 1,

        /// <summary>已稳定在 duckRatio（持续直到 ReleaseDucking）。</summary>
        Held = 2,

        /// <summary>正在 fade-up 回到 1.0。</summary>
        FadingUp = 3,
    }

    /// <summary>
    /// Ducking 控制器 — UniTask 时序插值，独立于 framework 便于 EditMode 单元测试。
    /// </summary>
    /// <remarks>
    /// <para>纯函数模式：内部维护 <see cref="CurrentMultiplier"/> [0.0, 1.0]；调用方（AudioManager）
    /// 在 <c>Apply()</c> 委托里把 multiplier 套到 framework 音量（Music + Ambient layer effective 公式）。</para>
    /// <para>EditMode 测试通过 <see cref="StepLerpForTest"/> 直接驱动时间，不依赖 PlayMode。</para>
    /// <para>设计来源：ADR-017 §A ducking system；framework 层无 ducking API，项目层补。</para>
    /// </remarks>
    public sealed class DuckingController
    {
        private const float MinPositiveFadeDuration = 0.001f; // 1ms — 用于"瞬时"的视为 instant 切换的阈值

        private DuckingState _state = DuckingState.Idle;
        private float _currentMultiplier = 1f;
        private float _targetMultiplier = 1f;
        private float _lerpStartMultiplier = 1f;
        private float _lerpDuration;
        private float _lerpElapsed;
        private float _heldRatio = 1f; // 最近一次 ducking 的目标 ratio
        private CancellationTokenSource _activeLerpCts;

        /// <summary>当前 ducking 状态。</summary>
        public DuckingState State => _state;

        /// <summary>当前 ducking 乘数 [0.0, 1.0]，1.0 = 无 ducking。</summary>
        public float CurrentMultiplier => _currentMultiplier;

        /// <summary>是否正在 ducking 状态（含 fade in/out）。</summary>
        public bool IsActive => _state != DuckingState.Idle;

        /// <summary>
        /// 启动 ducking — fade-down 到 duckRatio 后保持。
        /// </summary>
        /// <param name="duckRatio">目标乘数 [0.0, 1.0]（典型 0.3）。</param>
        /// <param name="fadeDuration">fade-down 时长（秒；&lt; 1ms 视为 instant）。</param>
        /// <param name="cancellationToken">取消 token（外部主动 cancel 时停止 lerp）。</param>
        public async UniTask StartDuckingAsync(float duckRatio, float fadeDuration,
            CancellationToken cancellationToken = default)
        {
            duckRatio = Mathf.Clamp01(duckRatio);
            _heldRatio = duckRatio;

            CancelActiveLerp();
            _lerpStartMultiplier = _currentMultiplier;
            _targetMultiplier = duckRatio;
            _lerpDuration = Mathf.Max(0f, fadeDuration);
            _lerpElapsed = 0f;
            _state = DuckingState.FadingDown;

            if (_lerpDuration < MinPositiveFadeDuration)
            {
                _currentMultiplier = duckRatio;
                _state = DuckingState.Held;
                return;
            }

            _activeLerpCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await RunLerpAsync(_activeLerpCts.Token);
            if (_state == DuckingState.FadingDown)
            {
                _state = DuckingState.Held;
            }
        }

        /// <summary>
        /// 释放 ducking — fade-up 回到 1.0。
        /// </summary>
        public async UniTask ReleaseDuckingAsync(float fadeDuration,
            CancellationToken cancellationToken = default)
        {
            CancelActiveLerp();
            _lerpStartMultiplier = _currentMultiplier;
            _targetMultiplier = 1f;
            _lerpDuration = Mathf.Max(0f, fadeDuration);
            _lerpElapsed = 0f;
            _state = DuckingState.FadingUp;

            if (_lerpDuration < MinPositiveFadeDuration)
            {
                _currentMultiplier = 1f;
                _state = DuckingState.Idle;
                return;
            }

            _activeLerpCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await RunLerpAsync(_activeLerpCts.Token);
            if (_state == DuckingState.FadingUp)
            {
                _state = DuckingState.Idle;
            }
        }

        /// <summary>
        /// 强制取消正在进行的 lerp，保持当前 multiplier 不变 — 用于 AudioManager.Shutdown。
        /// </summary>
        public void ForceCancel()
        {
            CancelActiveLerp();
        }

        /// <summary>
        /// 测试钩子：手动推进 lerp 时间 deltaTime（不依赖 PlayMode 的 Time.deltaTime）。
        /// </summary>
        /// <returns>推进后是否仍在 lerp（false = 已到达 target / Idle）。</returns>
        public bool StepLerpForTest(float deltaTime)
        {
            if (_state != DuckingState.FadingDown && _state != DuckingState.FadingUp)
            {
                return false;
            }
            _lerpElapsed += Mathf.Max(0f, deltaTime);
            if (_lerpDuration <= 0f || _lerpElapsed >= _lerpDuration)
            {
                _currentMultiplier = _targetMultiplier;
                _state = (_state == DuckingState.FadingDown) ? DuckingState.Held : DuckingState.Idle;
                return false;
            }
            float t = _lerpElapsed / _lerpDuration;
            _currentMultiplier = Mathf.Lerp(_lerpStartMultiplier, _targetMultiplier, t);
            return true;
        }

        /// <summary>
        /// 测试钩子：直接重置到 idle 状态（fixture teardown 用）。
        /// </summary>
        public void ResetForTest()
        {
            CancelActiveLerp();
            _state = DuckingState.Idle;
            _currentMultiplier = 1f;
            _targetMultiplier = 1f;
            _lerpStartMultiplier = 1f;
            _lerpDuration = 0f;
            _lerpElapsed = 0f;
            _heldRatio = 1f;
        }

        private async UniTask RunLerpAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _lerpElapsed < _lerpDuration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                if (cancellationToken.IsCancellationRequested) return;

                _lerpElapsed += Time.unscaledDeltaTime;
                if (_lerpElapsed >= _lerpDuration)
                {
                    _currentMultiplier = _targetMultiplier;
                    return;
                }
                float t = _lerpElapsed / _lerpDuration;
                _currentMultiplier = Mathf.Lerp(_lerpStartMultiplier, _targetMultiplier, t);
            }
        }

        private void CancelActiveLerp()
        {
            if (_activeLerpCts != null)
            {
                _activeLerpCts.Cancel();
                _activeLerpCts.Dispose();
                _activeLerpCts = null;
            }
        }
    }
}
