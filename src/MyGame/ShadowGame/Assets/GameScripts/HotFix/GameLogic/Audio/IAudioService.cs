// 该文件由Cursor 自动生成
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Audio system 公共服务接口 — 项目层 facade（per architecture.md §6.8）。
    /// </summary>
    /// <remarks>
    /// <para>实施: <see cref="AudioManager"/> singleton。</para>
    /// <para>使用模式：</para>
    /// <list type="bullet">
    ///   <item><description>事件路径（推荐 cross-system 通信）：派发 IAudioEvent → AudioManager listener。</description></item>
    ///   <item><description>API 路径（同模块内 / 流程明确）：直接 <c>AudioManager.Instance.PlaySFX(...)</c>。</description></item>
    /// </list>
    /// <para>层映射：Ambient / SFX / Music → TEngine 4-channel 内部映射 (per <see cref="AudioLayer"/> 文档)。</para>
    /// </remarks>
    public interface IAudioService
    {
        // ==== SFX ====

        /// <summary>
        /// 通过字符串 id 触发 SFX 播放（architecture API）。
        /// </summary>
        /// <param name="sfxId">SFX 字符串 id（与 Luban TbAudio Sfx 表 name 字段对齐）；内部解析到 int sfxId。</param>
        /// <param name="worldPosition">3D 空间位置（可选；null = 2D 播放）。</param>
        void PlaySFX(string sfxId, Vector3? worldPosition = null);

        /// <summary>停止所有正在播放的 SFX。</summary>
        void StopAllSFX();

        // ==== Music ====

        /// <summary>
        /// 切换背景音乐 — 支持 crossfade。
        /// </summary>
        /// <param name="clipId">Music 字符串 id（与 Luban TbAudio Music 表 name 字段对齐）。</param>
        /// <param name="crossfadeDuration">crossfade 时长（秒；默认 1.0）。</param>
        void PlayMusic(string clipId, float crossfadeDuration = 1.0f);

        /// <summary>
        /// 停止当前 Music（带 fade-out）。
        /// </summary>
        /// <param name="fadeDuration">fade-out 时长（秒；默认 1.0）。</param>
        void StopMusic(float fadeDuration = 1.0f);

        // ==== Ducking (used by Narrative) ====

        /// <summary>
        /// 启动 ducking — 降低 Ambient + Music 音量到 duckRatio。
        /// </summary>
        /// <param name="duckRatio">ducking 后音量倍数 [0.0, 1.0]。</param>
        /// <param name="fadeDuration">fade-down 时长（秒）。</param>
        void SetDucking(float duckRatio, float fadeDuration);

        /// <summary>
        /// 释放 ducking — 恢复到 ducking 前 baseline。
        /// </summary>
        /// <param name="fadeDuration">fade-up 时长（秒）。</param>
        void ReleaseDucking(float fadeDuration);

        // ==== Volume control (used by Settings) ====

        /// <summary>
        /// 设置某 layer 的 baseline 音量。
        /// </summary>
        void SetLayerVolume(AudioLayer layer, float volume);

        /// <summary>读取某 layer 当前 baseline 音量。</summary>
        float GetLayerVolume(AudioLayer layer);

        /// <summary>设置 master 总音量。</summary>
        void SetMasterVolume(float volume);

        /// <summary>读取 master 总音量。</summary>
        float GetMasterVolume();

        // ==== Lifecycle ====

        /// <summary>
        /// 暂停所有音频（OnApplicationPause(true) / PauseMenu 触发）。
        /// </summary>
        /// <remarks>
        /// 简化版 D3[b]: 调 GameModule.Audio.Enable=false；真严格 pause-from-position 留 Sprint 6+ 升级。
        /// </remarks>
        void PauseAll();

        /// <summary>
        /// 恢复所有音频（OnApplicationPause(false) / PauseMenu 关闭触发）。
        /// </summary>
        void ResumeAll();
    }
}
