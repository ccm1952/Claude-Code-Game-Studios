// 该文件由Cursor 自动生成

# Story: Music Layer (BGM Per-Chapter, Crossfade on Transition)

> **Epic**: audio-system
> **Story ID**: audio-system-004
> **Story Type**: Logic
> **GDD Requirement**: TR-audio-006 (Music crossfade 1-5s), TR-audio-013 (App pause/resume audio state), TR-audio-014 (Music continues during PauseMenu)
> **ADR References**: ADR-017 (Audio Mix Strategy), ADR-005 (YooAsset Resource Lifecycle)
> **Sprint**: TBD
> **Status**: Ready

## Context

Music Layer 管理每章节的背景音乐（BGM），通过双 AudioSource 策略实现无缝 crossfade（默认 3.0s）。章节切换时，当前 source 淡出的同时新 source 淡入，crossfade 完成后旧 source 停止并回收为备用。PauseMenu 打开时 Music **不暂停**（游戏 TimeScale 设为 0，但 AudioSource.ignoreListenerPause = true 保持播放）。

Music 受 ducking 影响（Ambient + Music 均被压制），受 masterVolume 和 musicVolume 控制，但 SFX 不影响 Music。

## Acceptance Criteria

- [ ] `MusicLayer` 维护两个 AudioSource（sourceA / sourceB），交替用于 crossfade
- [ ] `Evt_SceneLoadComplete { bgmAsset, crossfadeDuration? }` 触发时，执行 crossfade：当前 source 淡出，新 source 加载并淡入
- [ ] Crossfade 时长从 Luban 配置读取（默认 3.0s，范围 1.0-5.0s）
- [ ] Crossfade 完成后：旧 source.Stop()，旧 source 成为下次 crossfade 的备用 source
- [ ] `Evt_SceneTransitionBegin` 触发时：当前播放的 source 在 crossfadeDuration 内淡出（为下个场景腾出听觉空间）
- [ ] PauseMenu 打开（TimeScale = 0）时：Music 继续播放，不受 TimeScale 影响（TR-audio-014）
- [ ] App pause 时（`OnApplicationPause(true)`）：保存当前播放位置；App resume 时从保存位置继续播放，无 pop/click
- [ ] 音量公式：`finalVolume = clipBaseVolume × musicVolume × masterVolume × duckingMultiplier`
- [ ] Music crossfade 过程中 ducking 变化时：两个 source 的 duckingMultiplier 均同步更新
- [ ] crossfade 期间旧 source 的 AssetHandle 不提前 Release（等 crossfade 完成后释放）
- [ ] 单元测试：crossfade 期间总音量（sourceA + sourceB）近似保持不变（非线性 crossfade 除外）

## Implementation Notes

- 类路径：`Assets/GameScripts/HotFix/GameLogic/Module/AudioModule/Layers/MusicLayer.cs`
- 双 AudioSource 声明为成员，在 Init 时通过 `GameModule.Audio` 请求
- `AudioSource.ignoreListenerPause = true`：确保 TimeScale = 0 时音乐继续
- `AudioSource.loop = true`：BGM 循环播放
- Crossfade 通过 UniTask（禁止 Coroutine）实现逐帧 volume 插值
- 防止 crossfade 重叠：新 Evt_SceneLoadComplete 到达时，如正在 crossfade，取消当前 crossfade task 并立即以新音轨开始新 crossfade
- 音频 clip 编码规范：BGM 使用 streaming（> 30s clips），44.1kHz，Vorbis（Android/PC）/ AAC（iOS）

## Out of Scope

- Ducking 系统本身（story-005）——MusicLayer 只是接收 duckingMultiplier 并应用
- 音频资源格式配置（ADR-003 平台规范）
- Settings 持久化（story-007）

## QA Test Cases

### TC-001: Music Crossfade 正常切换
- **Given**: sourceA 正在播放 chapter1_bgm
- **When**: `Evt_SceneLoadComplete { bgmAsset = "chapter2_bgm", crossfadeDuration = 3.0f }` 触发
- **Then**: 3.0s 后 sourceA.volume == 0，sourceB.isPlaying == true，sourceA.isPlaying == false（已 Stop）

### TC-002: Music 在 TimeScale = 0 时继续播放
- **Given**: Music 正在播放
- **When**: `Time.timeScale = 0`（模拟 PauseMenu 打开）
- **Then**: AudioSource.isPlaying == true，不受 TimeScale 影响

### TC-003: App Pause/Resume 播放位置保留
- **Given**: Music 播放至 45.3s
- **When**: OnApplicationPause(true) → OnApplicationPause(false)
- **Then**: Resume 后播放位置 ≈ 45.3s（误差 < 0.1s）

### TC-004: Music 音量公式（ducking 激活）
- **Given**: clipBaseVolume = 1.0, musicVolume = 0.8, masterVolume = 1.0, duckingMultiplier = 0.3
- **When**: MusicLayer finalVolume 计算
- **Then**: finalVolume ≈ 0.24f

### TC-005: Crossfade 期间 ducking 变化同步
- **Given**: Crossfade 进行中（sourceA 淡出，sourceB 淡入）
- **When**: duckingMultiplier 从 1.0 变为 0.3
- **Then**: sourceA 和 sourceB 均立即应用新 duckingMultiplier（不等待 crossfade 完成）

### TC-006: Crossfade 期间旧 AssetHandle 不提前释放
- **Given**: chapter1 AssetHandle 持有中，触发切换到 chapter2
- **When**: Crossfade 进行中（sourceA 仍在淡出）
- **Then**: chapter1 AssetHandle 不在 crossfade 期间被 Release

## Test Evidence Path

`tests/unit/MusicLayer_Test.cs`

## Dependencies

- story-001: AudioManager 初始化（masterVolume / musicVolume / duckingMultiplier 状态）
- story-005: Ducking 系统（duckingMultiplier 来源）
- ADR-005: YooAsset Resource Lifecycle（AssetHandle 管理）
- ADR-017: Music crossfade 规范
