// 该文件由Cursor 自动生成

# Story: Ambient Sound Layer (Per-Chapter Environmental Audio)

> **Epic**: audio-system
> **Story ID**: audio-system-002
> **Story Type**: Logic
> **GDD Requirement**: TR-audio-001 (3 mix layers), TR-audio-009 (Ambient starts within 2s), TR-audio-010 (Ambient occasional sounds)
> **ADR References**: ADR-017 (Audio Mix Strategy), ADR-005 (YooAsset Resource Lifecycle)
> **Sprint**: TBD
> **Status**: Ready

## Context

Ambient Layer 是影子回忆情感氛围的基底——"构成安静本身"。每个章节有独立的环境音轨（循环播放），另有非规律的偶发声音（occasional sounds）增加空间真实感。Ambient Layer 独立于 SFX，`sfx_enabled = false` 时 Ambient **不受影响**，继续以 `ambientBaseVolume × masterVolume × duckingMultiplier` 播放。

Ambient 层通过监听 `Evt_SceneLoadComplete` 事件切换章节环境音轨，在场景转换开始时（`Evt_SceneTransitionBegin`）执行淡出。

## Acceptance Criteria

- [ ] `AmbientLayer` 类实现环境音循环播放，通过 `GameModule.Audio` 管理 AudioSource
- [ ] `Evt_SceneLoadComplete { bgmAsset, ambientAsset }` 触发时，加载并播放对应章节 ambient 音频剪辑
- [ ] Ambient 音频从 `Evt_SceneLoadComplete` 事件触发到声音开始播放，延迟 ≤ 2s（TR-audio-009）
- [ ] `Evt_SceneTransitionBegin` 触发时，Ambient 音频在 0.5s 内淡出
- [ ] 偶发声音（occasional sounds）支持：定义 min/max interval（从 Luban TbAudioEvent 读取），在 interval 范围内随机触发一次偶发音频
- [ ] 音量公式正确：`finalVolume = ambientBaseVolume(0.6) × masterVolume × duckingMultiplier`（无 layerVolume 玩家参数）
- [ ] `sfx_enabled = false` 不影响 Ambient Layer 播放
- [ ] 场景切换时旧 Ambient AssetHandle 在淡出完成后正确 Release（无资源泄漏）
- [ ] 单元测试：音量公式在不同 duckingMultiplier 下计算正确

## Implementation Notes

- 类路径：`Assets/GameScripts/HotFix/GameLogic/Module/AudioModule/Layers/AmbientLayer.cs`
- 使用 `GameModule.Resource.LoadAssetAsync<AudioClip>()` 加载环境音频，保持 handle 用于后续 Release
- Ambient AssetHandle 由 AmbientLayer 自身持有，在音轨切换和场景卸载时 Release
- 偶发声音配置字段示例：`TbAudioEvent.occasionalVariants[]`, `occasionalMinInterval`, `occasionalMaxInterval`
- 淡出通过逐帧更新 AudioSource.volume 实现（UniTask 异步），禁止使用 Unity Audio Mixer
- duckingMultiplier 由 AudioManager 下发（通过 SetDucking / ReleaseDucking 更新），AmbientLayer 不自行管理

## Out of Scope

- Music crossfade（story-004）
- Ducking 系统触发（story-005）
- Settings 持久化（story-007）

## QA Test Cases

### TC-001: Ambient 在场景加载后 2s 内开始播放
- **Given**: `Evt_SceneLoadComplete` 事件包含有效 ambientAsset 路径
- **When**: 事件触发后等待 2s
- **Then**: AmbientLayer 的 AudioSource 开始播放，isPlaying == true

### TC-002: sfx_enabled = false 不影响 Ambient
- **Given**: AmbientLayer 正在播放，sfxVolume 更新通知到达
- **When**: `sfx_enabled` 设为 false（Evt_SettingChanged key = "sfx_enabled", value = false）
- **Then**: AmbientLayer AudioSource.volume 不变

### TC-003: 场景转换时 Ambient 淡出
- **Given**: AmbientLayer 正在播放
- **When**: `Evt_SceneTransitionBegin` 触发
- **Then**: 0.5s 内 AudioSource.volume 降至 0

### TC-004: Ambient 音量公式（ducking 激活）
- **Given**: ambientBaseVolume = 0.6, masterVolume = 0.8, duckingMultiplier = 0.3
- **When**: 计算 finalVolume
- **Then**: finalVolume = 0.6 × 0.8 × 0.3 = 0.144f（误差 < 0.001）

### TC-005: AssetHandle 在音轨切换后 Release
- **Given**: 第一个 Ambient 音轨已加载
- **When**: 新 `Evt_SceneLoadComplete` 触发（新章节）
- **Then**: 旧 AssetHandle.Release() 被调用，旧 handle 设为 null

## Test Evidence Path

`tests/unit/AmbientLayer_Test.cs`

## Dependencies

- story-001: AudioManager 初始化（`IAudioService` 可用，masterVolume 已初始化）
- story-006: TbAudioEvent Luban 配置（occasional sounds 参数来源）
- ADR-005: YooAsset Resource Lifecycle（handle 持有和 Release 规范）
- ADR-017: Audio Mix 音量公式
