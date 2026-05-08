// 该文件由Cursor 自动生成
using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 单条 SFX 静态配置（POCO，sealed + readonly per ADR-029 R3 mandatory）。
    /// </summary>
    /// <remarks>
    /// <para>chapter 1 MVP hardcoded defaults；future Luban TbAudio.Sfx 表 schema 生成后从真表读。</para>
    /// </remarks>
    public sealed class SfxConfig
    {
        /// <summary>Luban Sfx 表 id（int 主键）。</summary>
        public readonly int Id;

        /// <summary>字符串 id（用于 IAudioService.PlaySFX(string) 公共 API）。</summary>
        public readonly string Name;

        /// <summary>资源路径（YooAsset asset path；GameModule.Audio.Play 第二参数）。</summary>
        public readonly string ClipPath;

        /// <summary>baseline 音量 [0.0, 1.0]。</summary>
        public readonly float Volume;

        /// <summary>pitch 随机化中心值（典型 1.0）。</summary>
        public readonly float PitchBase;

        /// <summary>pitch 随机化半宽（典型 0.05 → 范围 [0.95, 1.05]）。</summary>
        public readonly float PitchVariance;

        /// <summary>variant 资源路径列表（含 ClipPath 自身在内 ≥1）；连续 PlaySFX 时随机挑选避免重复。</summary>
        public readonly IReadOnlyList<string> Variants;

        public SfxConfig(int id, string name, string clipPath, float volume,
            float pitchBase, float pitchVariance, IReadOnlyList<string> variants)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new System.ArgumentException("SFX name cannot be null/empty.", nameof(name));
            }
            if (string.IsNullOrEmpty(clipPath))
            {
                throw new System.ArgumentException("SFX clipPath cannot be null/empty.", nameof(clipPath));
            }
            Id = id;
            Name = name;
            ClipPath = clipPath;
            Volume = UnityEngine.Mathf.Clamp01(volume);
            PitchBase = pitchBase;
            PitchVariance = UnityEngine.Mathf.Max(0f, pitchVariance);
            Variants = variants ?? new[] { clipPath };
        }
    }

    /// <summary>
    /// 单条 Music 静态配置。
    /// </summary>
    public sealed class MusicConfig
    {
        public readonly int Id;
        public readonly string Name;
        public readonly string ClipPath;
        public readonly float Volume;

        public MusicConfig(int id, string name, string clipPath, float volume)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new System.ArgumentException("Music name cannot be null/empty.", nameof(name));
            }
            if (string.IsNullOrEmpty(clipPath))
            {
                throw new System.ArgumentException("Music clipPath cannot be null/empty.", nameof(clipPath));
            }
            Id = id;
            Name = name;
            ClipPath = clipPath;
            Volume = UnityEngine.Mathf.Clamp01(volume);
        }
    }

    /// <summary>
    /// Audio 配置 provider 接口 — 解耦 AudioManager 与 Luban 真表绑定。
    /// </summary>
    /// <remarks>
    /// <para>测试 fixture 可注入 mock 实现绕过真表。</para>
    /// </remarks>
    public interface IAudioConfigProvider
    {
        SfxConfig GetSfxConfig(int sfxId);
        SfxConfig GetSfxConfigByName(string sfxName);
        bool TryResolveSfxName(string sfxName, out int sfxId);

        MusicConfig GetMusicConfig(int musicClipId);
        MusicConfig GetMusicConfigByName(string musicName);
        bool TryResolveMusicName(string musicName, out int musicClipId);
    }

    /// <summary>
    /// Audio 配置 provider — chapter 1 MVP hardcoded defaults。
    /// </summary>
    /// <remarks>
    /// <para>沿 <see cref="NarrativeEvent.NarrativeSequenceConfigFromLuban"/> + ShadowPuzzle.PuzzleStateConfigFromLuban 模式。</para>
    /// <para>future S5-XX+ Luban TbAudio 真表 schema 生成后通过 <see cref="InitFromLuban"/> 替换。</para>
    /// <para>Sprint 5 S5-06 dev-story preventive readiness check #2 deficiency stub (per ADR-029 V2.0 §V2-2 R2 deficiency-flagged path)。</para>
    /// </remarks>
    public sealed class AudioConfigFromLuban : IAudioConfigProvider
    {
        private readonly Dictionary<int, SfxConfig> _sfxById = new Dictionary<int, SfxConfig>();
        private readonly Dictionary<string, int> _sfxNameToId = new Dictionary<string, int>();
        private readonly Dictionary<int, MusicConfig> _musicById = new Dictionary<int, MusicConfig>();
        private readonly Dictionary<string, int> _musicNameToId = new Dictionary<string, int>();
        private bool _initialized;

        /// <summary>
        /// 初始化 chapter 1 MVP defaults — 3 SFX + 1 Music。
        /// </summary>
        /// <remarks>
        /// <para>资源路径用 placeholder（"Audio/SFX/..." / "Audio/Music/..."）；真 AudioClip 资产由 art asset 阶段填入。</para>
        /// <para>Spike + EditMode 测试不依赖真 AudioClip — 仅验证 provider lookup 行为 + AudioManager 状态机。</para>
        /// </remarks>
        public void InitWithDefaults()
        {
            if (_initialized) return;

            RegisterSfx(new SfxConfig(
                id: 1,
                name: "ui_click",
                clipPath: "Audio/SFX/ui_click",
                volume: 0.8f,
                pitchBase: 1.0f,
                pitchVariance: 0.05f,
                variants: new[] { "Audio/SFX/ui_click", "Audio/SFX/ui_click_alt" }));

            RegisterSfx(new SfxConfig(
                id: 2,
                name: "object_pickup",
                clipPath: "Audio/SFX/object_pickup",
                volume: 0.7f,
                pitchBase: 1.0f,
                pitchVariance: 0.05f,
                variants: new[] { "Audio/SFX/object_pickup" }));

            RegisterSfx(new SfxConfig(
                id: 3,
                name: "object_snap",
                clipPath: "Audio/SFX/object_snap",
                volume: 0.9f,
                pitchBase: 1.0f,
                pitchVariance: 0.05f,
                variants: new[] { "Audio/SFX/object_snap", "Audio/SFX/object_snap_var2" }));

            RegisterMusic(new MusicConfig(
                id: 100,
                name: "chapter1_ambient",
                clipPath: "Audio/Music/chapter1_ambient",
                volume: 0.6f));

            _initialized = true;
        }

        /// <summary>
        /// future production path — Luban TbAudio.tbres 生成后激活；当前 stub fallback InitWithDefaults。
        /// </summary>
        public void InitFromLuban()
        {
            // TODO[S5-XX+]: 留 future Luban TbAudio schema expand
            // 实际实现伪码：
            //   foreach (var row in ConfigSystem.Instance.Tables.TbAudio.Sfx.DataList)
            //       RegisterSfx(new SfxConfig(row.Id, row.Name, row.ClipPath, row.Volume, row.PitchBase, row.PitchVariance, row.Variants));
            //   foreach (var row in ConfigSystem.Instance.Tables.TbAudio.Music.DataList)
            //       RegisterMusic(new MusicConfig(row.Id, row.Name, row.ClipPath, row.Volume));
            //   _initialized = true;
            InitWithDefaults();
        }

        /// <summary>
        /// 测试辅助：注入自定义 SFX 配置（绕过 default；测试 fixture / 单测 mock 用）。
        /// </summary>
        public void RegisterSfx(SfxConfig config)
        {
            if (config == null) return;
            _sfxById[config.Id] = config;
            _sfxNameToId[config.Name] = config.Id;
            if (!_initialized) _initialized = true;
        }

        /// <summary>
        /// 测试辅助：注入自定义 Music 配置。
        /// </summary>
        public void RegisterMusic(MusicConfig config)
        {
            if (config == null) return;
            _musicById[config.Id] = config;
            _musicNameToId[config.Name] = config.Id;
            if (!_initialized) _initialized = true;
        }

        /// <inheritdoc />
        public SfxConfig GetSfxConfig(int sfxId)
        {
            if (!_initialized) InitWithDefaults();
            return _sfxById.TryGetValue(sfxId, out var cfg) ? cfg : null;
        }

        /// <inheritdoc />
        public SfxConfig GetSfxConfigByName(string sfxName)
        {
            if (!_initialized) InitWithDefaults();
            if (string.IsNullOrEmpty(sfxName)) return null;
            return _sfxNameToId.TryGetValue(sfxName, out var id) ? _sfxById[id] : null;
        }

        /// <inheritdoc />
        public bool TryResolveSfxName(string sfxName, out int sfxId)
        {
            if (!_initialized) InitWithDefaults();
            if (string.IsNullOrEmpty(sfxName))
            {
                sfxId = 0;
                return false;
            }
            return _sfxNameToId.TryGetValue(sfxName, out sfxId);
        }

        /// <inheritdoc />
        public MusicConfig GetMusicConfig(int musicClipId)
        {
            if (!_initialized) InitWithDefaults();
            return _musicById.TryGetValue(musicClipId, out var cfg) ? cfg : null;
        }

        /// <inheritdoc />
        public MusicConfig GetMusicConfigByName(string musicName)
        {
            if (!_initialized) InitWithDefaults();
            if (string.IsNullOrEmpty(musicName)) return null;
            return _musicNameToId.TryGetValue(musicName, out var id) ? _musicById[id] : null;
        }

        /// <inheritdoc />
        public bool TryResolveMusicName(string musicName, out int musicClipId)
        {
            if (!_initialized) InitWithDefaults();
            if (string.IsNullOrEmpty(musicName))
            {
                musicClipId = 0;
                return false;
            }
            return _musicNameToId.TryGetValue(musicName, out musicClipId);
        }

        /// <summary>测试用：清空所有注册。</summary>
        public void ClearForTest()
        {
            _sfxById.Clear();
            _sfxNameToId.Clear();
            _musicById.Clear();
            _musicNameToId.Clear();
            _initialized = false;
        }
    }
}
