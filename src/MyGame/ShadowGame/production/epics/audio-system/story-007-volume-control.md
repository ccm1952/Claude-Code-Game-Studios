// 该文件由Cursor 自动生成

# Story: Runtime Volume Control Per Layer (Settings Integration)

> **Epic**: audio-system
> **Story ID**: audio-system-007
> **Story Type**: Integration
> **GDD Requirement**: TR-audio-002 (Volume formula), TR-audio-011 (Audio CPU < 1ms with 10 sources), TR-audio-012 (Audio memory < 30MB)
> **ADR References**: ADR-017 (Audio Mix Strategy), ADR-008 (Save System)
> **Sprint**: TBD
> **Status**: Blocked (依赖 ui-system story-007-settings-panel)

## Context

本 story 将 Audio System 与 Settings 系统打通：Settings 面板的 master/music/sfx 音量滑块和 sfx_enabled 开关变化后，通过 `Evt_SettingChanged` 事件实时更新 AudioManager 内部状态，并持久化到 PlayerPrefs。同时验证 Audio System 满足性能预算（CPU < 1ms / 10 sources，内存 < 30MB）。

Settings 数据存储于 PlayerPrefs（独立于存档系统 save.json），8 个设置项包括 master_volume / music_volume / sfx_volume / sfx_enabled。

## Acceptance Criteria

- [ ] Settings 面板调节 master_volume 滑块 → `Evt_SettingChanged { key="master_volume", value=0.7f }` → AudioManager 立即更新 masterVolume，同帧内所有 AudioSource 音量生效
- [ ] Settings 面板调节 music_volume 滑块 → MusicLayer 音量立即更新
- [ ] Settings 面板调节 sfx_volume 滑块 → SFXLayer 音量立即更新（不影响 Ambient）
- [ ] Settings 面板切换 sfx_enabled = false → SFXLayer 静音；Ambient Layer 不受影响（TR-audio-001 + ADR-017 关键行为）
- [ ] 音量设置通过 PlayerPrefs 持久化（key: `master_volume`, `music_volume`, `sfx_volume`, `sfx_enabled`）
- [ ] 游戏重启后，AudioManager.Init() 从 PlayerPrefs 恢复上次音量设置
- [ ] 音量值范围约束：0.0 ≤ volume ≤ 1.0（滑块端和 PlayerPrefs 写入均进行 Clamp）
- [ ] **性能验证**：10 个活跃 AudioSource 时，Audio System Update 帧耗 ≤ 1ms（Unity Profiler 测量）
- [ ] **内存验证**：全部音频资产加载后总内存 ≤ 30MB（Unity Memory Profiler 测量）
- [ ] 集成测试：Settings 面板打开 → 调节音量 → 关闭面板 → 重启游戏 → 音量恢复为调节后的值

## Implementation Notes

- AudioManager 监听 `Evt_SettingChanged`（已在 story-001 注册），根据 key 分发到对应 layer
- Settings 系统写入 PlayerPrefs 后发送 `Evt_SettingChanged` 事件（由 Settings 系统负责持久化，Audio System 只读取和响应）
- 或者：AudioManager 本身在收到 Evt_SettingChanged 后也写入 PlayerPrefs（双方都写，以最后一次为准）——按项目现有 Settings 系统架构决定，本 story 实现时确认
- 实时音量更新路径：`Evt_SettingChanged` → AudioManager.OnSettingChanged() → 更新 layerVolume/masterVolume → 通知各 Layer 重新计算 finalVolume → 各 Layer 更新持有 AudioSource.volume
- 性能测试在 iPhone 13 Mini（或等效真机）上执行；若无真机，在编辑器 Profiler 中记录 `ProfilerMarker("AudioManager.Update")`

## Out of Scope

- Settings 面板 UI 本身（ui-system story-007）
- PlayerPrefs 读写的 Settings 系统核心逻辑（settings-accessibility epic）
- 性能自动降级（由 PerformanceMonitor / ADR-018 管理）

## QA Test Cases

### TC-001: master_volume 实时生效
- **Given**: 游戏运行中，Music 正在播放
- **When**: `Evt_SettingChanged { key="master_volume", value=0.5f }` 触发
- **Then**: 同帧内 MusicLayer AudioSource.volume 更新为 finalVolume（含新 masterVolume = 0.5）

### TC-002: sfx_enabled=false 不影响 Ambient
- **Given**: Ambient 正在播放，SFX 正在播放
- **When**: `Evt_SettingChanged { key="sfx_enabled", value=false }` 触发
- **Then**: SFX AudioSource.volume == 0；Ambient AudioSource.volume 不变

### TC-003: 设置持久化跨会话
- **Given**: 调节 music_volume = 0.6，PlayerPrefs 已写入
- **When**: 模拟游戏重启（重新 Init AudioManager）
- **Then**: AudioManager.MusicVolume == 0.6f

### TC-004: 音量范围 Clamp
- **Given**: 音量值来自 PlayerPrefs（可能存在超范围值）
- **When**: AudioManager 读取并应用 volume = 1.5f（超出范围）
- **Then**: 实际应用值被 Clamp 至 1.0f

### TC-005: Audio 性能预算合规（Integration Evidence）
- **Setup**: 加载章节场景，触发 10 个 SFX 并发播放，同时 Ambient + Music 运行
- **Verify**: Unity Profiler 中 AudioManager.Update 标记 < 1ms/frame（10 次采样均值）
- **Pass**: 无单帧超出 1ms 预算

## Test Evidence Path

- 单元测试：`tests/unit/AudioManager_VolumeControl_Test.cs`
- 性能证据：`production/qa/evidence/audio-system/audio-perf-budget-evidence.md`

## Dependencies

- story-001: AudioManager 初始化
- story-002: AmbientLayer（验证 sfx_enabled 不影响 Ambient）
- story-003: SFXLayer（sfx_enabled 切换目标）
- story-004: MusicLayer（musicVolume 实时更新目标）
- ui-system story-007: SettingsPanel（触发 Evt_SettingChanged 事件的 UI 端）
- ADR-008: Save System（Settings 存储于 PlayerPrefs，独立于 save.json）
- ADR-017: 音量公式 + 性能预算
