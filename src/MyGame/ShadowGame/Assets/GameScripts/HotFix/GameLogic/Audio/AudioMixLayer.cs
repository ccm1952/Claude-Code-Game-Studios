// 该文件由Cursor 自动生成
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 单个 mix layer 的状态封装（baseline volume + mute + ducking 影响标记 + effective 计算）。
    /// </summary>
    /// <remarks>
    /// <para>纯 POCO，不依赖 framework — EditMode 单元测试可直接覆盖 multiplicative 公式 + mute 行为 + ducking
    /// 是否影响。</para>
    /// <para>actual = master × baselineVolume × (mute ? 0 : 1) × (affectedByDucking ? duckMultiplier : 1)。</para>
    /// </remarks>
    public sealed class AudioMixLayer
    {
        /// <summary>层标识（Ambient / SFX / Music）。</summary>
        public AudioLayer Layer { get; }

        /// <summary>是否受 ducking 影响（per ADR-017 §A：Ambient + Music 受影响；SFX 完全不受）。</summary>
        public bool AffectedByDucking { get; }

        /// <summary>baseline 音量 [0.0, 1.0]，由 Settings / 默认值控制。</summary>
        public float BaselineVolume { get; private set; }

        /// <summary>
        /// 是否静音（独立于 BaselineVolume；用于 sfx_enabled = false 等开关切换，不动 BaselineVolume 数值）。
        /// </summary>
        public bool Muted { get; private set; }

        public AudioMixLayer(AudioLayer layer, float baselineVolume, bool affectedByDucking)
        {
            Layer = layer;
            BaselineVolume = Mathf.Clamp01(baselineVolume);
            AffectedByDucking = affectedByDucking;
            Muted = false;
        }

        /// <summary>设置 baseline 音量 — 由 IAudioService.SetLayerVolume / OnSetLayerVolume / Settings cascade 调用。</summary>
        public void SetBaselineVolume(float volume)
        {
            BaselineVolume = Mathf.Clamp01(volume);
        }

        /// <summary>切换静音状态（Settings sfx_enabled / music_enabled 用）。</summary>
        public void SetMuted(bool muted)
        {
            Muted = muted;
        }

        /// <summary>
        /// 计算最终 effective 音量 — multiplicative formula：master × baseline × mute × ducking。
        /// </summary>
        /// <param name="masterVolume">master 总音量 [0.0, 1.0]。</param>
        /// <param name="duckMultiplier">当前 ducking 乘数 [0.0, 1.0]（idle 状态下应为 1.0；本层 AffectedByDucking=false 时调用方应忽略此参数）。</param>
        /// <returns>最终音量 [0.0, 1.0]。</returns>
        public float ComputeEffectiveVolume(float masterVolume, float duckMultiplier)
        {
            if (Muted) return 0f;

            float master = Mathf.Clamp01(masterVolume);
            float duck = AffectedByDucking ? Mathf.Clamp01(duckMultiplier) : 1f;
            return master * BaselineVolume * duck;
        }
    }
}
