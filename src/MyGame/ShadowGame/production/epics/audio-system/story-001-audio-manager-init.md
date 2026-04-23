// 该文件由Cursor 自动生成

# Story: AudioManager Initialization via TEngine AudioModule

> **Epic**: audio-system
> **Story ID**: audio-system-001
> **Story Type**: Logic
> **GDD Requirement**: TR-audio-001 (3 mix layers), TR-audio-002 (volume formula)
> **ADR References**: ADR-017 (Audio Mix Strategy), ADR-001 (TEngine Framework)
> **Sprint**: TBD
> **Status**: Ready

## Context

Audio System 是影子回忆情绪体验的核心基础设施。本 story 建立 `AudioManager` 的初始化骨架——将 TEngine `GameModule.Audio`（AudioModule）封装为项目的 `IAudioService` 接口实现，完成三层音频（Ambient / SFX / Music）的数据结构初始化、音量状态初始化、GameEvent 监听注册，以及 App pause/resume 时的音频状态保存/恢复。

AudioManager 在 Init Order 步骤 12-15（Feature Layer init）中初始化，晚于 `Tables.Init()`（步骤 7）和 `SaveSystem.LoadAsync()`（步骤 11），因此启动时可安全读取 Luban 配置和玩家设置。

## Acceptance Criteria

- [ ] `IAudioService` 接口定义包含：`PlaySFX`, `PlayMusic`, `StopMusic`, `SetDucking`, `ReleaseDucking`, `SetLayerVolume`, `SetMasterVolume`, `PauseAll`, `ResumeAll`
- [ ] `AudioManager` 实现 `IAudioService`，通过 `GameModule.Audio` 访问 TEngine AudioModule（禁止直接创建 AudioSource）
- [ ] 三个 `AudioLayerState` 内部状态对象在 Init 时完成初始化（Ambient / SFX / Music）
- [ ] 初始 masterVolume 从 PlayerPrefs 读取（key: `master_volume`，默认值 1.0）
- [ ] 初始 musicVolume 从 PlayerPrefs 读取（key: `music_volume`，默认值 1.0）
- [ ] 初始 sfxVolume 从 PlayerPrefs 读取（key: `sfx_volume`，默认值 1.0）
- [ ] ambientBaseVolume 内部常量 = 0.6，不存入 PlayerPrefs，不暴露到设置面板
- [ ] `OnApplicationPause(true)` 时调用 `PauseAll()`；`OnApplicationPause(false)` 时调用 `ResumeAll()`
- [ ] GameEvent 监听在 Init 注册（`Evt_AudioDuckingRequest`, `Evt_AudioDuckingRelease`, `Evt_PlaySFXRequest`, `Evt_PlayMusicRequest`, `Evt_SceneTransitionBegin`, `Evt_SceneLoadComplete`, `Evt_SettingChanged`）
- [ ] GameEvent 监听在 Dispose/OnClose 中正确注销，无内存泄漏
- [ ] 单元测试：Init 后 masterVolume = PlayerPrefs 值（或默认 1.0）

## Implementation Notes

- 类路径：`Assets/GameScripts/HotFix/GameLogic/Module/AudioModule/AudioManager.cs`
- 接口路径：`Assets/GameScripts/HotFix/GameLogic/Module/AudioModule/IAudioService.cs`
- 所有模块访问通过 `GameModule.Audio`，禁止 `ModuleSystem.GetModule<T>()`
- 禁止使用 Unity Audio Mixer Groups——ducking 通过 AudioSource volume 操控
- 禁止使用 FMOD / Wwise
- ambientBaseVolume = 0.6 为内部设计基准，是"构成安静本身"的设计哲学——不向玩家暴露
- Event ID 范围：Audio 系统为 1600-1699（ADR-006 分配）
- `Evt_SettingChanged` 监听时：key == `master_volume` → 更新 masterVolume；key == `music_volume` → 更新 musicVolume；key == `sfx_volume` → 更新 sfxVolume

## Out of Scope

- 具体音频播放逻辑（由 story-002 Ambient、story-003 SFX、story-004 Music 负责）
- Ducking 系统实现（story-005）
- Luban TbAudioEvent 配置读取（story-006）
- Settings UI 绑定（story-007）

## QA Test Cases

### TC-001: Init 默认音量加载正确
- **Given**: PlayerPrefs 中无 master_volume / music_volume / sfx_volume 键
- **When**: `AudioManager.Init()` 执行
- **Then**: masterVolume = 1.0f, musicVolume = 1.0f, sfxVolume = 1.0f

### TC-002: Init 从 PlayerPrefs 读取用户设置
- **Given**: PlayerPrefs 已设置 `master_volume = 0.7f`
- **When**: `AudioManager.Init()` 执行
- **Then**: masterVolume = 0.7f

### TC-003: ambientBaseVolume 不存入 PlayerPrefs
- **Given**: `AudioManager.Init()` 完成后
- **When**: 检查 PlayerPrefs.HasKey("ambient_volume") 和 PlayerPrefs.HasKey("ambient_base_volume")
- **Then**: 两个 key 均不存在

### TC-004: App Pause 时音频暂停
- **Given**: `AudioManager` 已初始化
- **When**: `OnApplicationPause(true)` 被触发
- **Then**: `PauseAll()` 被调用；`OnApplicationPause(false)` → `ResumeAll()` 被调用

### TC-005: GameEvent 监听注册/注销
- **Given**: `AudioManager` 初始化后
- **When**: Dispose 被调用
- **Then**: 所有 8 个 GameEvent 监听均被注销（无孤立 listener）

## Test Evidence Path

`tests/unit/AudioManager_Init_Test.cs`

## Dependencies

- ADR-001: TEngine Framework（`GameModule.Audio` 可用）
- ADR-006: GameEvent Protocol（Event ID 1600-1699 分配确认）
- ADR-017: Audio Mix Architecture（IAudioService 接口定义来源）
- Control Manifest §4.3: Audio Mix 规则
