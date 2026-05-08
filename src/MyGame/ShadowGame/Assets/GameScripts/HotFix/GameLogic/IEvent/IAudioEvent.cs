// 该文件由Cursor 自动生成
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 音频系统 facade 抽象的 3 个混音层。
    /// </summary>
    /// <remarks>
    /// <para>项目层抽象：3 个用户可见层（Ambient 环境声 / SFX 音效 / Music 背景音乐）。</para>
    /// <para>Framework 层映射（per AudioManager S5-06 dev-story）：</para>
    /// <list type="bullet">
    ///   <item><description>Ambient → TEngine <c>AudioType.Music</c> channel（与 Music 共享，受 ducking 影响）</description></item>
    ///   <item><description>SFX     → TEngine <c>AudioType.Sound</c> channel（不受 ducking 影响）</description></item>
    ///   <item><description>Music   → TEngine <c>AudioType.Music</c> channel</description></item>
    /// </list>
    /// <para>设计来源：ADR-017 §A audio mix architecture / architecture.md §6.8 IAudioService。</para>
    /// </remarks>
    public enum AudioLayer
    {
        /// <summary>环境声层（per GDD "ambient is silence itself" — 多数时段静音 + 偶发音效）。</summary>
        Ambient = 0,

        /// <summary>音效层（玩家交互反馈、UI 点击、object pickup/snap 等）。</summary>
        SFX = 1,

        /// <summary>背景音乐层（章节 BGM、过场音乐 etc）。</summary>
        Music = 2,
    }

    /// <summary>
    /// Audio system 广播接口（多 source → AudioManager listener；ADR-017 §A 设计）。
    /// </summary>
    /// <remarks>
    /// <para>Sender: NarrativeSequencePlayer（OnDuckingRequest）/ Settings 系统（OnSetLayerVolume cascade via
    /// ISettingsEvent → AudioManager 内部转发）/ 未来 puzzle / UI / chapter 各系统 SFX / Music 触发。</para>
    /// <para>Listener: AudioManager（Sprint 5 S5-06 production code 已实施 — 8 method full contract）。</para>
    /// <para>协议来源：ADR-017 §A audio mix architecture；ADR-016 §A line 75-91 ducking 触发示例。</para>
    /// <para>Cascade depth budget: 0（terminal — AudioManager listener 仅本地音量调整 / 播放调用，不再派发其它事件）。</para>
    /// <para>Listener 注册模式：per ADR-027 §5 framework knowledge fact —
    /// handler null-out + Cleanup null-check guard，避免 TEngine RemoveEventListener 非 idempotent 抛 "Delete handle failed"。</para>
    /// </remarks>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IAudioEvent
    {
        /// <summary>
        /// 请求播放音效（SFX layer）。
        /// </summary>
        /// <remarks>
        /// <para>Sender: 任意业务方（puzzle / UI / object interaction）；fire-and-forget。</para>
        /// <para>Listener (AudioManager): AudioConfigFromLuban 解析 sfxId → clipPath → variant 选择 → pitch 随机 →
        /// GameModule.Audio.Play(AudioType.Sound, ...)；Sound channel maxChannel=4 自动 oldest-cull cap。</para>
        /// </remarks>
        /// <param name="sfxId">Luban TbAudio Sfx 表 id（chapter 1 MVP hardcoded 默认值；future 真表替换）。</param>
        /// <param name="delay">延迟播放（秒；0 = 立即）。</param>
        /// <param name="volume">单 source 音量倍数 [0.0, 1.0]（typical 1.0；与 layer/master/ducking 多重相乘）。</param>
        void OnPlaySFX(int sfxId, float delay, float volume);

        /// <summary>
        /// 请求播放背景音乐（Music layer）— 支持 crossfade 平滑过渡。
        /// </summary>
        /// <remarks>
        /// <para>Sender: chapter 切换 / narrative 段落起手；fire-and-forget。</para>
        /// <para>Listener (AudioManager): 切换当前 Music AudioAgent；crossfadeDuration > 0 时在 Music channel
        /// 双 agent 间交叠（per AudioSetting.asset Music agentHelperCount=2 配置）；crossfadeDuration ≈ 0 时 hard-cut。</para>
        /// </remarks>
        /// <param name="musicClipId">Luban TbAudio Music 表 id。</param>
        /// <param name="crossfadeDuration">crossfade 时长（秒；典型 1.0；0 = hard-cut）。</param>
        void OnPlayMusic(int musicClipId, float crossfadeDuration);

        /// <summary>
        /// 请求停止当前 Music 播放（带 fade-out）。
        /// </summary>
        /// <param name="fadeDuration">fade-out 时长（秒；典型 1.0；0 = 立即）。</param>
        void OnStopMusic(float fadeDuration);

        /// <summary>
        /// 请求 audio ducking — narrative sequence / VO 期间降低背景（Ambient + Music）音量。
        /// </summary>
        /// <remarks>
        /// <para>Sender: NarrativeSequencePlayer.AudioDuckingEffect（fire-and-forget；effect Tick 阶段不实测 fade
        /// 完成 — listener AudioManager 内部 driving fade 时序）。</para>
        /// <para>Listener (AudioManager): DuckingController.StartDucking — UniTask 时序插值修改 GameModule.Audio.MusicVolume
        /// 到 baseline × duckRatio；fadeDuration 后稳定；后续触发 OnReleaseDucking 才 fade-up。</para>
        /// <para>SFX layer 完全不受 ducking 影响（per ADR-017 §A）。</para>
        /// </remarks>
        /// <param name="duckRatio">ducking 后音量倍数 [0.0, 1.0]（典型 0.3 = 30% 原音量）。</param>
        /// <param name="fadeDuration">fade-down 时长（秒；典型 0.3-1.0）。</param>
        void OnDuckingRequest(float duckRatio, float fadeDuration);

        /// <summary>
        /// 请求释放 ducking — 恢复 Ambient/Music 到 ducking 前 baseline。
        /// </summary>
        /// <param name="fadeDuration">fade-up 时长（秒；典型 0.3-1.0）。</param>
        void OnReleaseDucking(float fadeDuration);

        /// <summary>
        /// 请求设置某 layer 的 baseline 音量（Settings UI / debug）。
        /// </summary>
        /// <remarks>
        /// <para>Sender: Settings 系统 cascade（ISettingsEvent.OnSettingChanged → AudioManager 内部转发）/ debug 工具。</para>
        /// <para>Ambient layer 的 volume 通常不通过此事件设置（AC-7 internal baseline 0.6 不暴露 player）。</para>
        /// </remarks>
        /// <param name="layer">目标 layer。</param>
        /// <param name="volume">layer baseline 音量 [0.0, 1.0]。</param>
        void OnSetLayerVolume(AudioLayer layer, float volume);

        /// <summary>
        /// 请求暂停所有音频（OnApplicationPause / 暂停菜单触发）。
        /// </summary>
        /// <remarks>
        /// <para>Sender: GameApp 生命周期 / PauseMenu UI。</para>
        /// <para>Listener (AudioManager): 简化版 D3[b] — 调 GameModule.Audio.Enable=false（AudioListener.volume=0）；
        /// 真严格 pause-from-position 留 Sprint 6+ backlog 升级（per S5-06 dev-story 决策 D3[b]）。</para>
        /// </remarks>
        void OnAudioPauseRequest();

        /// <summary>
        /// 请求恢复所有音频（OnApplicationFocus / 暂停菜单关闭触发）。
        /// </summary>
        void OnAudioResumeRequest();
    }
}
