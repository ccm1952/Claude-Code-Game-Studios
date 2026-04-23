// 该文件由Cursor 自动生成

# Story: SFX Layer (Interaction Feedback, Puzzle Events)

> **Epic**: audio-system
> **Story ID**: audio-system-003
> **Story Type**: Logic
> **GDD Requirement**: TR-audio-003 (SFX variant + pitch randomization), TR-audio-004 (3D spatial audio), TR-audio-005 (maxConcurrent + oldest cull), TR-audio-008 (SFX latency ≤ 1 frame)
> **ADR References**: ADR-017 (Audio Mix Strategy), ADR-005 (YooAsset Resource Lifecycle)
> **Sprint**: TBD
> **Status**: Ready

## Context

SFX Layer 提供玩家交互反馈和谜题事件音效。所有 SFX 通过 `Evt_PlaySFXRequest { sfxId, position? }` 事件触发，SFX 层从 Luban `TbAudioEvent` 表读取配置，支持：变体随机选择（避免重复感）、音高随机化（±pitchVariance）、3D 空间定位（worldPosition）、每个 sfxId 并发上限（maxConcurrent = 4），超出时淘汰最旧实例。

SFX 不受 ducking 影响，但受 `sfx_enabled` 切换和 sfxVolume 控制。SFX 延迟要求极严：从事件触发到声音开始播放 ≤ 1 帧（TR-audio-008）。

## Acceptance Criteria

- [ ] `SFXLayer.PlaySFX(string sfxId, Vector3? position)` 实现：从 TbAudioEvent 读取配置，选取随机变体，应用音高随机化，通过 `GameModule.Audio` 播放
- [ ] SFX 变体随机：同一 sfxId 连续播放 5 次，至少出现 2 种不同变体（当 variants.Length > 1 时）
- [ ] 音高随机化：`pitch = basePitch + Random.Range(-pitchVariance, pitchVariance)`，basePitch 和 pitchVariance 均来自 TbAudioEvent
- [ ] 3D 空间音频：当 `position.HasValue == true` 且 `config.SpatialMode == ThreeD` 时，AudioSource 使用 3D 模式播放于 worldPosition
- [ ] 并发上限：同一 sfxId 活跃实例数超过 `maxConcurrent`（默认 4）时，Kill 最旧的实例后再播放新的
- [ ] SFX 延迟 ≤ 1 帧：从 `Evt_PlaySFXRequest` 到 AudioSource.Play() 调用在同帧内完成（无异步等待）
- [ ] `sfx_enabled = false` 时 SFX Layer 静音（volume = 0），所有 AudioSource 保持运行（不 Stop，确保 enable 后可继续而不重启）
- [ ] 音量公式：`finalVolume = clipBaseVolume × sfxVolume × masterVolume`（无 duckingMultiplier）
- [ ] SFX 不受 ducking 影响（ducking 仅影响 Ambient 和 Music）
- [ ] sfxId 不存在于 TbAudioEvent 时：`Log.Warning` 并返回，不抛异常
- [ ] 单元测试：并发限制——第 5 个相同 sfxId 请求触发最旧实例被 Kill

## Implementation Notes

- 类路径：`Assets/GameScripts/HotFix/GameLogic/Module/AudioModule/Layers/SFXLayer.cs`
- 并发跟踪：`Dictionary<string, Queue<AudioSourceHandle>> _activeSFX` 按 sfxId 分组
- 音频剪辑通过 `GameModule.Resource.LoadAssetAsync<AudioClip>()` 加载；SFX 音频较短，可在 PlaySFX 时同步从 TbAudioEvent 的 assetPath 获取——但若需要预加载，在 puzzle Active 状态时提前加载常用 SFX（TR-audio-008 latency 要求）
- 空间音频：调用 TEngine `GameModule.Audio` 的 3D 播放 API；保持对 Camera 引用的缓存（禁止每帧 Camera.main）
- sfx_enabled 通过监听 `Evt_SettingChanged { key="sfx_enabled", value }` 切换静音状态
- SFX AssetHandle 在 AudioSource 播放完成后 Release（完成回调或定时释放）

## Out of Scope

- Luban TbAudioEvent 表结构定义（story-006）
- Settings UI 的 sfx_enabled 切换触发（story-007）
- NearMatch / PerfectMatch 具体 sfxId 映射（story-006 配置驱动）

## QA Test Cases

### TC-001: SFX 变体随机化
- **Given**: TbAudioEvent[sfxId].variants.Length == 3
- **When**: PlaySFX(sfxId) 调用 5 次
- **Then**: 使用的变体 index 中至少出现 2 种不同值

### TC-002: SFX 并发限制
- **Given**: TbAudioEvent[sfxId].maxConcurrent == 4
- **When**: PlaySFX(sfxId) 被调用 5 次，前 4 次仍在播放
- **Then**: 第 5 次调用时最旧实例被停止，新实例开始播放，活跃数保持 ≤ 4

### TC-003: 3D SFX 位置定位
- **Given**: sfxId 配置 SpatialMode = ThreeD，position = (5, 0, 3)
- **When**: PlaySFX(sfxId, new Vector3(5, 0, 3)) 调用
- **Then**: AudioSource.spatialBlend == 1.0f（3D 模式），position 约等于 (5, 0, 3)

### TC-004: sfx_enabled = false 时静音
- **Given**: SFX 正在播放
- **When**: Evt_SettingChanged{key="sfx_enabled", value=false} 触发
- **Then**: 所有 SFX AudioSource.volume == 0，但 isPlaying 状态不变

### TC-005: SFX 音量公式
- **Given**: clipBaseVolume = 0.8, sfxVolume = 0.7, masterVolume = 0.9
- **When**: SFX finalVolume 计算
- **Then**: finalVolume = 0.8 × 0.7 × 0.9 = 0.504f（误差 < 0.001）

### TC-006: 未知 sfxId 不抛异常
- **Given**: TbAudioEvent 中不存在 sfxId = "nonexistent_sfx"
- **When**: PlaySFX("nonexistent_sfx") 调用
- **Then**: 无 Exception，Log.Warning 输出包含 sfxId，方法正常返回

## Test Evidence Path

`tests/unit/SFXLayer_Test.cs`

## Dependencies

- story-001: AudioManager 初始化（masterVolume / sfxVolume 状态可用）
- story-006: TbAudioEvent Luban 配置（变体、音高、maxConcurrent 参数来源）
- ADR-005: YooAsset Resource Lifecycle
- ADR-017: Audio Mix SFX 规范
- Control Manifest §4.3: SFX 不受 ducking 影响
