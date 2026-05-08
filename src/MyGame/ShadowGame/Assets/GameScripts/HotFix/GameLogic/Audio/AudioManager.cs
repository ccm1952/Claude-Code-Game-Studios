// 该文件由Cursor 自动生成
using System;
using System.Globalization;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using AudioType = TEngine.AudioType;
// AudioType 同名解决：TEngine.AudioType (framework 4-channel enum) 与 UnityEngine.AudioType 冲突，显式 alias。

namespace GameLogic
{
    /// <summary>
    /// Audio system facade — 项目层 3-layer 抽象 + ducking + crossfade，对接 TEngine AudioModule 4-channel。
    /// </summary>
    /// <remarks>
    /// <para>设计来源: ADR-017 §A audio mix architecture / architecture.md §6.8 IAudioService。</para>
    /// <para>Layer mapping (per S5-06 dev-story decision):</para>
    /// <list type="bullet">
    ///   <item><description>Ambient → TEngine <c>AudioType.Music</c> channel（与 Music 共享，受 ducking 影响；agentHelperCount=2 per AudioSetting.asset）</description></item>
    ///   <item><description>SFX     → TEngine <c>AudioType.Sound</c> channel（agentHelperCount=4，自动 oldest-cull cap）</description></item>
    ///   <item><description>Music   → TEngine <c>AudioType.Music</c> channel</description></item>
    /// </list>
    /// <para>ADR-028 §1 facade activation gate: <see cref="Initialize"/> 调用前任何 <see cref="PlaySFX(string, Vector3?)"/> /
    /// <see cref="PlayMusic(string, float)"/> 调用 fail-loud (Log.Warning + no-op)；<see cref="Initialize"/> 后正常。</para>
    /// <para>Listener 注册模式: per ADR-027 §5 framework knowledge fact —
    /// 每个 listener handler 独立字段 + null-out + Cleanup null-check guard，避免 TEngine RemoveEventListener 非 idempotent 抛 "Delete handle failed"。</para>
    /// <para>Pause/Resume 简化版 (per S5-06 D3[b]): 调 GameModule.Audio.Enable=false/true (AudioListener.volume mute);
    /// 真严格 pause-from-position 留 Sprint 6+ backlog。</para>
    /// </remarks>
    public sealed class AudioManager : Singleton<AudioManager>, IAudioService
    {
        private const float DefaultAmbientBaseline = 0.6f; // per AC-7 internal baseline，不暴露 Settings UI

        // ==== Listener handler fields (8 IAudioEvent + 1 ISettingsEvent) — ADR-027 §5 null-out pattern ====
        private Action<int, float, float> _onPlaySFX;
        private Action<int, float> _onPlayMusic;
        private Action<float> _onStopMusic;
        private Action<float, float> _onDuckingRequest;
        private Action<float> _onReleaseDucking;
        private Action<AudioLayer, float> _onSetLayerVolume;
        private Action _onAudioPauseRequest;
        private Action _onAudioResumeRequest;
        private Action<string, string> _onSettingChanged;

        // ==== State ====
        private AudioMixLayer _ambient;
        private AudioMixLayer _sfx;
        private AudioMixLayer _music;
        private DuckingController _ducking;
        private IAudioConfigProvider _configProvider;
        private float _masterVolume = 1f;
        private bool _isInitialized;

        // ==== Test hooks ====

        /// <summary>初始化状态（per AC-3 facade activation gate）— 测试钩子。</summary>
        public bool IsInitializedForTest => _isInitialized;

        /// <summary>当前 ducking multiplier — 测试钩子。</summary>
        public float CurrentDuckingMultiplierForTest => _ducking?.CurrentMultiplier ?? 1f;

        /// <summary>当前 ducking 状态 — 测试钩子。</summary>
        public DuckingState CurrentDuckingStateForTest => _ducking?.State ?? DuckingState.Idle;

        /// <summary>测试钩子：注入 mock IAudioConfigProvider（Initialize 前调用）。</summary>
        public void SetConfigProviderForTest(IAudioConfigProvider provider)
        {
            _configProvider = provider;
        }

        /// <summary>测试钩子：直接读 layer baseline（绕过 GetLayerVolume 接口路径）。</summary>
        public float GetLayerBaselineVolumeForTest(AudioLayer layer)
        {
            return GetLayer(layer)?.BaselineVolume ?? 0f;
        }

        /// <summary>测试钩子：直接读 layer muted（绕过接口）。</summary>
        public bool GetLayerMutedForTest(AudioLayer layer)
        {
            return GetLayer(layer)?.Muted ?? false;
        }

        /// <summary>
        /// 测试钩子：模拟 ducking lerp 推进时间（不依赖 PlayMode Time.deltaTime；EditMode 单测用）。
        /// </summary>
        public bool StepDuckingLerpForTest(float deltaTime)
        {
            return _ducking?.StepLerpForTest(deltaTime) ?? false;
        }

        // ==== Lifecycle ====

        /// <summary>
        /// 显式初始化 — 订阅 8 IAudioEvent listeners + 1 ISettingsEvent listener；初始化 3 mix layer + ducking。
        /// </summary>
        /// <remarks>
        /// <para>调用时机: GameApp.Entrance 中 ConfigSystem.Load() 之后、StartGameLogic() 之前。</para>
        /// <para>幂等保护: 重复 Initialize() no-op + Log.Warning。</para>
        /// </remarks>
        public void Initialize()
        {
            if (_isInitialized)
            {
                Log.Warning("[AudioManager] Initialize 重复调用，已忽略 (idempotent guard).");
                return;
            }

            // 3-layer state setup (Ambient affectedByDucking=true, SFX=false, Music=true)
            _ambient = new AudioMixLayer(AudioLayer.Ambient, DefaultAmbientBaseline, affectedByDucking: true);
            _sfx = new AudioMixLayer(AudioLayer.SFX, baselineVolume: 1f, affectedByDucking: false);
            _music = new AudioMixLayer(AudioLayer.Music, baselineVolume: 1f, affectedByDucking: true);
            _ducking = new DuckingController();

            if (_configProvider == null)
            {
                var luban = new AudioConfigFromLuban();
                luban.InitWithDefaults();
                _configProvider = luban;
            }

            // Sync framework volume from initial state (master × layer × duck) — pre-subscribe baseline。
            ApplyEffectiveVolumesToFramework();

            // 8 IAudioEvent listeners — per-event mode (per ADR-027 §5)
            _onPlaySFX = HandlePlaySFX;
            _onPlayMusic = HandlePlayMusic;
            _onStopMusic = HandleStopMusic;
            _onDuckingRequest = HandleDuckingRequest;
            _onReleaseDucking = HandleReleaseDucking;
            _onSetLayerVolume = HandleSetLayerVolume;
            _onAudioPauseRequest = HandleAudioPauseRequest;
            _onAudioResumeRequest = HandleAudioResumeRequest;

            GameEvent.AddEventListener<int, float, float>(IAudioEvent_Event.OnPlaySFX, _onPlaySFX);
            GameEvent.AddEventListener<int, float>(IAudioEvent_Event.OnPlayMusic, _onPlayMusic);
            GameEvent.AddEventListener<float>(IAudioEvent_Event.OnStopMusic, _onStopMusic);
            GameEvent.AddEventListener<float, float>(IAudioEvent_Event.OnDuckingRequest, _onDuckingRequest);
            GameEvent.AddEventListener<float>(IAudioEvent_Event.OnReleaseDucking, _onReleaseDucking);
            GameEvent.AddEventListener<AudioLayer, float>(IAudioEvent_Event.OnSetLayerVolume, _onSetLayerVolume);
            GameEvent.AddEventListener(IAudioEvent_Event.OnAudioPauseRequest, _onAudioPauseRequest);
            GameEvent.AddEventListener(IAudioEvent_Event.OnAudioResumeRequest, _onAudioResumeRequest);

            // ISettingsEvent.OnSettingChanged cascade (string, string per framework signature)
            _onSettingChanged = HandleSettingChanged;
            GameEvent.AddEventListener<string, string>(ISettingsEvent_Event.OnSettingChanged, _onSettingChanged);

            _isInitialized = true;
            Log.Info("[AudioManager] Initialized — 3 layers + 8 IAudioEvent listeners + 1 ISettingsEvent listener.");
        }

        /// <summary>
        /// 显式关闭 — 取消所有 listener 订阅（null-check guard pattern）+ 停止音频 + 取消 ducking lerp。
        /// </summary>
        /// <remarks>
        /// <para>幂等：未 Initialize 或重复 Shutdown 安全 no-op。</para>
        /// <para>per ADR-027 §5: 每个 handler null-check + RemoveEventListener + null 赋值，避免 TEngine 非 idempotent 抛异常。</para>
        /// </remarks>
        public void Shutdown()
        {
            if (!_isInitialized)
            {
                return;
            }

            // 8 IAudioEvent listeners null-check + RemoveEventListener + null-out
            if (_onPlaySFX != null)
            {
                GameEvent.RemoveEventListener<int, float, float>(IAudioEvent_Event.OnPlaySFX, _onPlaySFX);
                _onPlaySFX = null;
            }
            if (_onPlayMusic != null)
            {
                GameEvent.RemoveEventListener<int, float>(IAudioEvent_Event.OnPlayMusic, _onPlayMusic);
                _onPlayMusic = null;
            }
            if (_onStopMusic != null)
            {
                GameEvent.RemoveEventListener<float>(IAudioEvent_Event.OnStopMusic, _onStopMusic);
                _onStopMusic = null;
            }
            if (_onDuckingRequest != null)
            {
                GameEvent.RemoveEventListener<float, float>(IAudioEvent_Event.OnDuckingRequest, _onDuckingRequest);
                _onDuckingRequest = null;
            }
            if (_onReleaseDucking != null)
            {
                GameEvent.RemoveEventListener<float>(IAudioEvent_Event.OnReleaseDucking, _onReleaseDucking);
                _onReleaseDucking = null;
            }
            if (_onSetLayerVolume != null)
            {
                GameEvent.RemoveEventListener<AudioLayer, float>(IAudioEvent_Event.OnSetLayerVolume, _onSetLayerVolume);
                _onSetLayerVolume = null;
            }
            if (_onAudioPauseRequest != null)
            {
                GameEvent.RemoveEventListener(IAudioEvent_Event.OnAudioPauseRequest, _onAudioPauseRequest);
                _onAudioPauseRequest = null;
            }
            if (_onAudioResumeRequest != null)
            {
                GameEvent.RemoveEventListener(IAudioEvent_Event.OnAudioResumeRequest, _onAudioResumeRequest);
                _onAudioResumeRequest = null;
            }
            if (_onSettingChanged != null)
            {
                GameEvent.RemoveEventListener<string, string>(ISettingsEvent_Event.OnSettingChanged, _onSettingChanged);
                _onSettingChanged = null;
            }

            // 取消正在进行的 ducking lerp (避免后续 await 触发 framework 调用)
            _ducking?.ForceCancel();

            // Stop all audio (best-effort — framework safe-noop if module disabled)
            try
            {
                GameModule.Audio.StopAll(fadeout: false);
            }
            catch (Exception e)
            {
                Log.Warning($"[AudioManager] StopAll on shutdown raised: {e.Message}");
            }

            _isInitialized = false;
            Log.Info("[AudioManager] Shutdown — all listeners unsubscribed; audio stopped.");
        }

        /// <inheritdoc />
        protected override void OnRelease()
        {
            // SingletonSystem.Release() 路径走到这里 — 确保 listener 取消。
            Shutdown();
        }

        // ==== IAudioService — Public API (architecture.md §6.8) ====

        /// <inheritdoc />
        public void PlaySFX(string sfxId, Vector3? worldPosition = null)
        {
            if (!CheckActivationGate(nameof(PlaySFX))) return;
            if (string.IsNullOrEmpty(sfxId))
            {
                Log.Warning("[AudioManager] PlaySFX: sfxId is null/empty.");
                return;
            }

            var cfg = _configProvider.GetSfxConfigByName(sfxId);
            if (cfg == null)
            {
                Log.Warning($"[AudioManager] PlaySFX: SFX '{sfxId}' not found in config.");
                return;
            }
            PlaySfxInternal(cfg, volumeMultiplier: 1f);
        }

        /// <inheritdoc />
        public void StopAllSFX()
        {
            if (!CheckActivationGate(nameof(StopAllSFX))) return;
            try
            {
                GameModule.Audio.Stop(AudioType.Sound, fadeout: false);
            }
            catch (Exception e)
            {
                Log.Warning($"[AudioManager] StopAllSFX raised: {e.Message}");
            }
        }

        /// <inheritdoc />
        public void PlayMusic(string clipId, float crossfadeDuration = 1.0f)
        {
            if (!CheckActivationGate(nameof(PlayMusic))) return;
            if (string.IsNullOrEmpty(clipId))
            {
                Log.Warning("[AudioManager] PlayMusic: clipId is null/empty.");
                return;
            }

            var cfg = _configProvider.GetMusicConfigByName(clipId);
            if (cfg == null)
            {
                Log.Warning($"[AudioManager] PlayMusic: Music '{clipId}' not found in config.");
                return;
            }
            PlayMusicInternal(cfg, crossfadeDuration);
        }

        /// <inheritdoc />
        public void StopMusic(float fadeDuration = 1.0f)
        {
            if (!CheckActivationGate(nameof(StopMusic))) return;
            try
            {
                GameModule.Audio.Stop(AudioType.Music, fadeout: fadeDuration > 0f);
            }
            catch (Exception e)
            {
                Log.Warning($"[AudioManager] StopMusic raised: {e.Message}");
            }
        }

        /// <inheritdoc />
        public void SetDucking(float duckRatio, float fadeDuration)
        {
            if (!CheckActivationGate(nameof(SetDucking))) return;
            _ducking.StartDuckingAsync(duckRatio, fadeDuration).Forget();
            // Lerp tick 期间持续 ApplyEffectiveVolumesToFramework — 启 forget UniTask 监督
            DriveDuckingApplyAsync().Forget();
        }

        /// <inheritdoc />
        public void ReleaseDucking(float fadeDuration)
        {
            if (!CheckActivationGate(nameof(ReleaseDucking))) return;
            _ducking.ReleaseDuckingAsync(fadeDuration).Forget();
            DriveDuckingApplyAsync().Forget();
        }

        /// <inheritdoc />
        public void SetLayerVolume(AudioLayer layer, float volume)
        {
            if (!CheckActivationGate(nameof(SetLayerVolume))) return;
            var l = GetLayer(layer);
            if (l == null) return;
            l.SetBaselineVolume(volume);
            ApplyEffectiveVolumesToFramework();
        }

        /// <inheritdoc />
        public float GetLayerVolume(AudioLayer layer)
        {
            return GetLayer(layer)?.BaselineVolume ?? 0f;
        }

        /// <inheritdoc />
        public void SetMasterVolume(float volume)
        {
            if (!CheckActivationGate(nameof(SetMasterVolume))) return;
            _masterVolume = Mathf.Clamp01(volume);
            try
            {
                GameModule.Audio.Volume = _masterVolume;
            }
            catch (Exception e)
            {
                Log.Warning($"[AudioManager] SetMasterVolume raised: {e.Message}");
            }
            ApplyEffectiveVolumesToFramework();
        }

        /// <inheritdoc />
        public float GetMasterVolume() => _masterVolume;

        /// <inheritdoc />
        public void PauseAll()
        {
            if (!CheckActivationGate(nameof(PauseAll))) return;
            try
            {
                GameModule.Audio.Enable = false;
            }
            catch (Exception e)
            {
                Log.Warning($"[AudioManager] PauseAll raised: {e.Message}");
            }
        }

        /// <inheritdoc />
        public void ResumeAll()
        {
            if (!CheckActivationGate(nameof(ResumeAll))) return;
            try
            {
                GameModule.Audio.Enable = true;
            }
            catch (Exception e)
            {
                Log.Warning($"[AudioManager] ResumeAll raised: {e.Message}");
            }
        }

        // ==== IAudioEvent Listener Handlers ====

        private void HandlePlaySFX(int sfxId, float delay, float volume)
        {
            if (!_isInitialized) return;
            var cfg = _configProvider.GetSfxConfig(sfxId);
            if (cfg == null)
            {
                Log.Warning($"[AudioManager] OnPlaySFX: sfxId={sfxId} not found.");
                return;
            }
            // delay 当前 MVP 不实施实际延迟（chapter 1 不需要）— 留 Sprint 6+ 升级
            PlaySfxInternal(cfg, volumeMultiplier: Mathf.Clamp01(volume));
        }

        private void HandlePlayMusic(int musicClipId, float crossfadeDuration)
        {
            if (!_isInitialized) return;
            var cfg = _configProvider.GetMusicConfig(musicClipId);
            if (cfg == null)
            {
                Log.Warning($"[AudioManager] OnPlayMusic: musicClipId={musicClipId} not found.");
                return;
            }
            PlayMusicInternal(cfg, crossfadeDuration);
        }

        private void HandleStopMusic(float fadeDuration)
        {
            if (!_isInitialized) return;
            try { GameModule.Audio.Stop(AudioType.Music, fadeout: fadeDuration > 0f); }
            catch (Exception e) { Log.Warning($"[AudioManager] HandleStopMusic raised: {e.Message}"); }
        }

        private void HandleDuckingRequest(float duckRatio, float fadeDuration)
        {
            if (!_isInitialized) return;
            _ducking.StartDuckingAsync(duckRatio, fadeDuration).Forget();
            DriveDuckingApplyAsync().Forget();
        }

        private void HandleReleaseDucking(float fadeDuration)
        {
            if (!_isInitialized) return;
            _ducking.ReleaseDuckingAsync(fadeDuration).Forget();
            DriveDuckingApplyAsync().Forget();
        }

        private void HandleSetLayerVolume(AudioLayer layer, float volume)
        {
            if (!_isInitialized) return;
            var l = GetLayer(layer);
            if (l == null) return;
            l.SetBaselineVolume(volume);
            ApplyEffectiveVolumesToFramework();
        }

        private void HandleAudioPauseRequest()
        {
            if (!_isInitialized) return;
            try { GameModule.Audio.Enable = false; }
            catch (Exception e) { Log.Warning($"[AudioManager] HandleAudioPauseRequest raised: {e.Message}"); }
        }

        private void HandleAudioResumeRequest()
        {
            if (!_isInitialized) return;
            try { GameModule.Audio.Enable = true; }
            catch (Exception e) { Log.Warning($"[AudioManager] HandleAudioResumeRequest raised: {e.Message}"); }
        }

        // ==== ISettingsEvent Listener — Settings cascade ====

        private void HandleSettingChanged(string key, string value)
        {
            if (!_isInitialized || string.IsNullOrEmpty(key)) return;
            switch (key)
            {
                case "master_volume":
                    if (TryParseFloat(value, out float master)) SetMasterVolume(master);
                    break;
                case "music_volume":
                    if (TryParseFloat(value, out float musicVol)) SetLayerVolume(AudioLayer.Music, musicVol);
                    break;
                case "sfx_volume":
                    if (TryParseFloat(value, out float sfxVol)) SetLayerVolume(AudioLayer.SFX, sfxVol);
                    break;
                case "sfx_enabled":
                    if (TryParseBool(value, out bool sfxEnabled))
                    {
                        var sfxLayer = GetLayer(AudioLayer.SFX);
                        if (sfxLayer != null)
                        {
                            sfxLayer.SetMuted(!sfxEnabled);
                            ApplyEffectiveVolumesToFramework();
                        }
                    }
                    break;
                case "music_enabled":
                    if (TryParseBool(value, out bool musicEnabled))
                    {
                        var musicLayer = GetLayer(AudioLayer.Music);
                        if (musicLayer != null)
                        {
                            musicLayer.SetMuted(!musicEnabled);
                            ApplyEffectiveVolumesToFramework();
                        }
                    }
                    break;
                // ambient_volume 不处理 — per AC-7 internal baseline 不暴露 player Settings UI
            }
        }

        // ==== Internal helpers ====

        private bool CheckActivationGate(string callerApi)
        {
            if (!_isInitialized)
            {
                Log.Warning($"[AudioManager] {callerApi} called before Initialize() (ADR-028 §1 facade activation gate fail-loud) — no-op.");
                return false;
            }
            return true;
        }

        private AudioMixLayer GetLayer(AudioLayer layer)
        {
            return layer switch
            {
                AudioLayer.Ambient => _ambient,
                AudioLayer.SFX => _sfx,
                AudioLayer.Music => _music,
                _ => null,
            };
        }

        /// <summary>
        /// 把 3-layer effective volume 套用到 TEngine framework — Music + Ambient 共享 MusicVolume slot；SFX 独立 SoundVolume slot。
        /// </summary>
        /// <remarks>
        /// <para>Music + Ambient mapping: framework MusicVolume = max(MusicLayer.effective, AmbientLayer.effective)。
        /// Chapter 1 MVP 简化 — 真正同时播放 Ambient + Music 时取较大值；Sprint 6+ 升级独立 channel。</para>
        /// </remarks>
        private void ApplyEffectiveVolumesToFramework()
        {
            if (_ambient == null || _sfx == null || _music == null || _ducking == null) return;

            float duckMul = _ducking.CurrentMultiplier;
            float ambientEff = _ambient.ComputeEffectiveVolume(_masterVolume, duckMul);
            float musicEff = _music.ComputeEffectiveVolume(_masterVolume, duckMul);
            float sfxEff = _sfx.ComputeEffectiveVolume(_masterVolume, duckMultiplier: 1f);

            float musicChannelVolume = Mathf.Max(ambientEff, musicEff);

            try
            {
                // 关键：framework MusicVolume / SoundVolume 内部 Mathf.Clamp 到 [0.0001, 1.0] (避免 -∞ dB)；
                // EffectiveVolume = 0 时 framework 内部 Clamp 到 0.0001 仍 ≈ -80dB silent — 实测可接受。
                GameModule.Audio.MusicVolume = musicChannelVolume;
                GameModule.Audio.SoundVolume = sfxEff;
            }
            catch (Exception e)
            {
                Log.Warning($"[AudioManager] ApplyEffectiveVolumesToFramework raised: {e.Message}");
            }
        }

        /// <summary>
        /// Ducking lerp 期间持续把 multiplier 套到 framework — UniTask 弱后台跑直到 ducking 不再 active。
        /// </summary>
        private async UniTaskVoid DriveDuckingApplyAsync()
        {
            // 多次重入合并 — 仅当前 driver 跑 lerp 完整 lifecycle 即可
            int maxFrames = 3600; // 60s 上限保护
            int frame = 0;
            while (_isInitialized && _ducking != null && _ducking.IsActive && frame < maxFrames)
            {
                ApplyEffectiveVolumesToFramework();
                await UniTask.Yield(PlayerLoopTiming.Update);
                frame++;
            }
            // Final apply (lerp 完成或 cancel 后)
            if (_isInitialized) ApplyEffectiveVolumesToFramework();
        }

        private void PlaySfxInternal(SfxConfig cfg, float volumeMultiplier)
        {
            // variant 随机选择
            string clipPath = cfg.ClipPath;
            if (cfg.Variants != null && cfg.Variants.Count > 1)
            {
                int idx = UnityEngine.Random.Range(0, cfg.Variants.Count);
                clipPath = cfg.Variants[idx];
            }
            // pitch 随机化（注意：framework Play API 不直接支持 pitch；项目层若需 pitch 真控制需扩 AudioAgent.Pitch
            // 后续；MVP 阶段仅在 config 层定义，实际不 apply pitch — 留 Sprint 6+ pitch 真接入）
            float effectiveVolume = Mathf.Clamp01(cfg.Volume * volumeMultiplier);
            try
            {
                GameModule.Audio.Play(AudioType.Sound, clipPath, bLoop: false, volume: effectiveVolume, bAsync: true, bInPool: false);
            }
            catch (Exception e)
            {
                Log.Warning($"[AudioManager] PlaySfxInternal raised for clip '{clipPath}': {e.Message}");
            }
        }

        private void PlayMusicInternal(MusicConfig cfg, float crossfadeDuration)
        {
            // chapter 1 MVP 简化版 crossfade: 调 framework Play 启动新 track；framework Music channel agentHelperCount=2
            // 自动用第二个 agent 接力（实测 framework 是否真"crossfade" 留 PlayMode P9 验证）。
            // crossfadeDuration ≈ 0 时不显示传给 framework — framework Play 默认无 fade。
            try
            {
                GameModule.Audio.Play(AudioType.Music, cfg.ClipPath, bLoop: true, volume: cfg.Volume, bAsync: true, bInPool: false);
            }
            catch (Exception e)
            {
                Log.Warning($"[AudioManager] PlayMusicInternal raised for clip '{cfg.ClipPath}': {e.Message}");
            }
        }

        private static bool TryParseFloat(string value, out float result)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        private static bool TryParseBool(string value, out bool result)
        {
            if (bool.TryParse(value, out result)) return true;
            // 兼容 "0"/"1" 数值表示
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
            {
                result = n != 0;
                return true;
            }
            result = false;
            return false;
        }
    }
}
