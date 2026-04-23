// 该文件由Cursor 自动生成

# Epic: Settings & Accessibility

> **Layer**: Presentation
> **GDD**: `design/gdd/settings-accessibility.md`
> **Architecture Module**: SettingsManager (volume, sensitivity, accessibility options)
> **Governing ADRs**: ADR-008 (Save/PlayerPrefs)
> **Engine Risk**: LOW
> **Status**: In Progress
> **Stories**: 7 stories created

## Overview

Settings & Accessibility 系统管理 8 项玩家偏好设置（主音量、音乐音量、音效音量、触摸灵敏度、语言、帧率目标、字幕开关、振动开关），所有设置实时生效无需重启。设置数据独立存储于 PlayerPrefs（不进入 save JSON），通过 `Evt_SettingChanged(2000)` GameEvent 广播变更通知，各系统自行响应。

触摸灵敏度通过 multiplier 修改 Input System 的 dragThreshold。帧率目标支持 30/60fps 切换（`Application.targetFrameRate`）。语言设置通过 I2 Localization 运行时热切换（auto-detect + fallback）。环境音量独立于 SFX 开关控制。辅助功能选项（高对比度模式、阴影轮廓模式、字体缩放、动画缩放）列入 P2 阶段（ADR-020）。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-008: Save System (PlayerPrefs) | 设置数据独立于 save JSON，使用 PlayerPrefs 存储；实时生效通过 GameEvent 广播 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|:------------:|
| TR-settings-001 | 8 player settings | arch.md ✅ |
| TR-settings-002 | PlayerPrefs storage | ADR-008 ✅ |
| TR-settings-003 | Real-time apply, no restart | ADR-006 ✅ |
| TR-settings-004 | Touch sensitivity multiplier | ADR-010 ✅ |
| TR-settings-005 | Language auto-detect + fallback | ❌ Deferred to ADR-022 (P2) |
| TR-settings-006 | Language hot-switch runtime | ❌ Deferred to ADR-022 (P2) |
| TR-settings-007 | Frame rate toggle 30/60fps | ADR-003 ✅ |
| TR-settings-008 | Ambient volume independent of sfx_enabled | ADR-017 ⚠️ |

## Sprint 0 Findings Impact

- **SP-009 (I2 Localization)**: 已确认 TEngine 内嵌 I2 Localization 封装。语言切换 API 和 YooAsset 环境下语言资源加载已验证。但语言功能列入 P2（ADR-022）。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from the GDD are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs in `production/qa/evidence/`

## Dependencies

- **save-system**: PlayerPrefs 基础设施（虽然 Settings 不使用 save JSON，但 Save System 定义了 PlayerPrefs 的使用约定）
- **ui-system**: SettingsPanel UIWindow 用于呈现设置界面
- **audio-system**: 音量设置通过 AudioManager API 实时应用
- **input-system**: 触摸灵敏度通过 Input System 的 dragThreshold modifier 应用

## Stories

| # | Story | Type | Status | TR Coverage |
|---|-------|------|--------|-------------|
| 001 | [SettingsManager with PlayerPrefs Persistence](story-001-settings-manager.md) | Logic | Ready | TR-settings-001, 002, 003 |
| 002 | [Master/Music/SFX Volume Sliders + Real-Time Preview](story-002-volume-settings.md) | UI | Ready | TR-settings-001, 003, 008 |
| 003 | [Touch Sensitivity Adjustment](story-003-sensitivity-setting.md) | Integration | Ready | TR-settings-004 |
| 004 | [Haptic Vibration On/Off Toggle](story-004-haptic-toggle.md) | Logic | Ready | TR-settings-001 |
| 005 | [Runtime Language Switching via ILocalizationModule](story-005-language-switch.md) | Integration | Ready | TR-settings-005, 006 |
| 006 | [Shadow Contrast Boost + Touch Target Scaling](story-006-accessibility-options.md) | Visual/Feel | Ready | TR-settings-001 (accessibility P0) |
| 007 | [Settings Save/Load on App Lifecycle Events](story-007-settings-persistence.md) | Integration | Ready | TR-settings-002, 003 |

## Next Step

Run `/dev-story settings-accessibility/story-001-settings-manager` to begin implementation.
Run `/story-readiness settings-accessibility/story-NNN-slug` to validate a story before starting.
