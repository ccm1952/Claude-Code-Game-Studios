// 该文件由Cursor 自动生成

# Story: Audio Ducking System for Narrative Sequences

> **Epic**: audio-system
> **Story ID**: audio-system-005
> **Story Type**: Logic
> **GDD Requirement**: TR-audio-007 (Ducking system)
> **ADR References**: ADR-017 (Audio Mix Strategy), ADR-006 (GameEvent Protocol), ADR-016 (Narrative Sequence Engine)
> **Sprint**: TBD
> **Status**: Ready

## Context

Ducking 系统允许叙事序列（Narrative Sequence Engine）在记忆回放等情感高潮时段临时压低环境音和背景音乐，为叙事 SFX 腾出听觉空间。

Ducking 通过 `GameEvent` 事件触发（Narrative 层发送，Audio 层接收）：
- `Evt_AudioDuckingRequest { duckRatio, fadeDuration }` → 在 fadeDuration 内将 duckingMultiplier 渐变到 duckRatio
- `Evt_AudioDuckingRelease { fadeDuration }` → 在 fadeDuration 内将 duckingMultiplier 渐变回 1.0

**受影响层**：Ambient + Music（最终音量 × duckingMultiplier）  
**不受影响层**：SFX（叙事 SFX 在 ducking 期间正常播放）

## Acceptance Criteria

- [ ] `DuckingController` 维护 `_currentDuckingMultiplier`（运行时值）和 `_targetDuckingMultiplier`（目标值）
- [ ] 收到 `Evt_AudioDuckingRequest { duckRatio = 0.3, fadeDuration = 0.5 }` 时：在 0.5s 内将 `_currentDuckingMultiplier` 从当前值渐变至 0.3
- [ ] 收到 `Evt_AudioDuckingRelease { fadeDuration = 0.5 }` 时：在 0.5s 内将 `_currentDuckingMultiplier` 渐变回 1.0
- [ ] 渐变使用 `Mathf.MoveTowards`（非 Lerp），保证线性且不会过冲
- [ ] 每帧更新 `_currentDuckingMultiplier` 后，立即通知 AmbientLayer 和 MusicLayer 应用新值
- [ ] Ducking 期间 SFX Layer 的音量不受 `_currentDuckingMultiplier` 影响
- [ ] 叠加 ducking：收到第二个 `Evt_AudioDuckingRequest` 时（当前仍在 ducking 中），以新 duckRatio 为目标重新渐变（不累加，取新目标）
- [ ] Ducking 渐变过程在场景卸载时安全取消（无挂起的 task 或 coroutine）
- [ ] 默认参数（defaultDuckRatio = 0.3，defaultDuckFade = 0.5s）来自 Luban 配置（TbAudioEvent 或专用配置表）
- [ ] 单元测试：SetDucking(0.3, 0.5) 后 0.5s，duckingMultiplier 近似 0.3（误差 < 0.01）

## Implementation Notes

- 类路径：`Assets/GameScripts/HotFix/GameLogic/Module/AudioModule/DuckingController.cs`
- 逐帧更新在 AudioManager.OnUpdate() 中调用（不使用独立 MonoBehaviour）
- `MoveTowards` 计算：`duckSpeed = Mathf.Abs(current - target) / fadeDuration`
- 通知 AmbientLayer/MusicLayer 方式：通过 AudioManager 持有的 layer 引用直接调用 `SetDuckingMultiplier(float)`（内部 API，非 GameEvent）
- 禁止使用 Unity Audio Mixer Snapshot——ducking 通过 AudioSource.volume 操控

## Out of Scope

- 叙事序列的 ducking 请求发送（由 Narrative Sequence Engine / ADR-016 负责）
- Ducking 配置表 schema 设计（story-006）

## QA Test Cases

### TC-001: Ducking 渐变正确（0 → 目标值）
- **Given**: duckingMultiplier = 1.0
- **When**: SetDucking(duckRatio=0.3, fadeDuration=0.5s) 调用后等待 0.5s
- **Then**: duckingMultiplier ≈ 0.3（误差 < 0.01）

### TC-002: ReleaseDucking 恢复
- **Given**: duckingMultiplier = 0.3（ducking 激活中）
- **When**: ReleaseDucking(fadeDuration=0.5s) 调用后等待 0.5s
- **Then**: duckingMultiplier ≈ 1.0（误差 < 0.01）

### TC-003: SFX 不受 ducking 影响
- **Given**: duckingMultiplier 从 1.0 降至 0.3
- **When**: SFXLayer.GetEffectiveVolume() 计算
- **Then**: SFX 音量不包含 duckingMultiplier 因子（公式中无此项）

### TC-004: 叠加 ducking 请求以新目标重新渐变
- **Given**: 正在从 1.0 渐变至 0.3（进行中）
- **When**: 收到新 Evt_AudioDuckingRequest { duckRatio = 0.1, fadeDuration = 1.0 }
- **Then**: 渐变目标切换为 0.1，从当前中间值继续渐变（不从 1.0 重新开始）

### TC-005: 场景卸载时 ducking 安全停止
- **Given**: Ducking 渐变进行中
- **When**: Evt_SceneUnloadBegin 触发
- **Then**: 渐变取消，duckingMultiplier 重置为 1.0，无挂起 async task

## Test Evidence Path

`tests/unit/DuckingController_Test.cs`

## Dependencies

- story-001: AudioManager 初始化（OnUpdate 调用链）
- story-002: AmbientLayer（接收 duckingMultiplier 更新）
- story-004: MusicLayer（接收 duckingMultiplier 更新）
- ADR-016: Narrative Sequence Engine（ducking 请求发送方）
- ADR-017: Ducking 参数规范
