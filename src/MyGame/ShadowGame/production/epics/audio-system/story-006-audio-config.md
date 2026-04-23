// 该文件由Cursor 自动生成

# Story: Luban TbAudioEvent Config Table Integration

> **Epic**: audio-system
> **Story ID**: audio-system-006
> **Story Type**: Config/Data
> **GDD Requirement**: TR-audio-015 (All SFX config from Luban), TR-audio-003 (SFX variant + pitch)
> **ADR References**: ADR-017 (Audio Mix Strategy), ADR-007 (Luban Config Access), SP-004 (Luban Thread Safety)
> **Sprint**: TBD
> **Status**: Ready

## Context

所有 SFX 事件定义（clip 路径、变体列表、音高、并发上限、空间模式、baseVolume）必须通过 Luban `TbAudioEvent` 配置表驱动——禁止在代码中硬编码任何音频资源路径或参数。本 story 定义 `TbAudioEvent` 表的字段 schema，并验证 Audio System 从 `Tables.Instance.TbAudioEvent.Get(id)` 正确读取所有运行时所需参数。

同时定义 Ducking 默认参数配置（可放在 `TbAudioConfig` 全局配置表）和 Music crossfade 默认时长配置。

## Acceptance Criteria

- [ ] `TbAudioEvent` Luban 表包含以下字段：`Id (string)`, `Variants (string[])`, `BasePitch (float)`, `PitchVariance (float)`, `MaxConcurrent (int)`, `SpatialMode (enum: TwoD/ThreeD)`, `BaseVolume (float)`, `OccasionalMinInterval (float)`, `OccasionalMaxInterval (float)`（后两项仅 Ambient 类型使用）
- [ ] `TbAudioConfig` 全局配置表包含：`DefaultDuckRatio (float = 0.3)`, `DefaultDuckFade (float = 0.5)`, `DefaultCrossfadeDuration (float = 3.0)`, `AmbientBaseVolume (float = 0.6)`
- [ ] 至少定义以下 SFX 事件数据行：`sfx_puzzle_nearmatch`, `sfx_puzzle_perfectmatch`, `sfx_interaction_pickup`, `sfx_interaction_drop`, `sfx_ui_button_click`
- [ ] Audio System 所有 Luban 读取通过 `Tables.Instance.TbAudioEvent.Get(id)` 执行（非 `TbAudioEvent.Get(id)` 直接类调用）
- [ ] `Tables.Instance` 读取仅在主线程执行（UniTask 续体在主线程，满足 SP-004 规范）
- [ ] 读取结果不缓存到持久字段（热路径方法内单次局部缓存除外，符合 ADR-007）
- [ ] `TbAudioEvent.Get(id)` 返回 null 时：调用方记录 Log.Warning 并 fallback（不抛异常）
- [ ] 手动验证：修改 Luban 表中某 SFX 的 pitchVariance，重新生成 GameProto，运行游戏后 SFX 音高变化符合新配置

## Implementation Notes

- Luban 表定义路径：`DataTables/AudioEvent.xlsx`（或 `.json`），GameProto 程序集下 `TbAudioEvent.cs` 自动生成
- **禁止手动编辑 GameProto 程序集中任何生成文件**（Luban 重新生成会覆盖）
- 如需扩展 config 字段派生逻辑，使用 Extension Methods 放在 GameLogic 程序集
- 全局配置表 `TbAudioConfig` 可以是单行表（singleton config pattern in Luban）
- OccasionalMinInterval / MaxInterval 默认值建议：min = 15s, max = 45s
- SpatialMode enum 在 GameProto 中定义，GameLogic 可通过 `using GameProto;` 访问

## Out of Scope

- Luban 构建管线本身（由 DevOps 管理）
- 音频资产（clip 文件）的创建（由 Audio Director 负责）
- Settings 持久化（story-007）

## QA Test Cases

### TC-001: TbAudioEvent 读取返回正确字段
- **Given**: `sfx_puzzle_nearmatch` 条目已在 TbAudioEvent 中定义（BaseVolume = 0.8, MaxConcurrent = 4）
- **When**: `Tables.Instance.TbAudioEvent.Get("sfx_puzzle_nearmatch")`
- **Then**: 返回非 null，BaseVolume == 0.8f，MaxConcurrent == 4

### TC-002: 未知 ID 返回 null
- **Given**: TbAudioEvent 中无 "nonexistent_id" 条目
- **When**: `Tables.Instance.TbAudioEvent.Get("nonexistent_id")`
- **Then**: 返回 null（无 Exception）

### TC-003: TbAudioConfig 全局配置读取
- **Given**: TbAudioConfig 已初始化
- **When**: 读取 DefaultDuckRatio / DefaultCrossfadeDuration
- **Then**: DefaultDuckRatio == 0.3f，DefaultCrossfadeDuration == 3.0f

### TC-004: 配置修改后重新生成生效
- **Given**: TbAudioEvent 中 sfx_interaction_pickup.PitchVariance 从 0.05 改为 0.1，重新生成 GameProto
- **When**: 运行时读取 TbAudioEvent.Get("sfx_interaction_pickup").PitchVariance
- **Then**: PitchVariance == 0.1f

### TC-005: 主线程访问验证（SP-004 合规）
- **Given**: Luban 读取在 UniTask 延续中执行
- **When**: 确认执行线程
- **Then**: 执行上下文在 Unity 主线程（`PlayerLoopTiming` 或主线程检测通过）

## Test Evidence Path

`tests/unit/AudioConfig_Luban_Test.cs`

## Dependencies

- ADR-007: Luban Config Access（访问模式、禁止手动编辑生成代码）
- SP-004: Luban Thread Safety（主线程访问）
- story-001: AudioManager 初始化（Tables.Init() 在步骤 7 完成，Audio init 在步骤 12-15）
- story-003: SFX Layer（消费 TbAudioEvent 配置）
