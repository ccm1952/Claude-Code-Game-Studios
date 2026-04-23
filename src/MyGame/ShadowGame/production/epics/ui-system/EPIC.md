// 该文件由Cursor 自动生成

# Epic: UI System

> **Layer**: Feature
> **GDD**: `design/gdd/ui-system.md`
> **Architecture Module**: UI screens (GameHUD, PauseMenu, PuzzleComplete, ChapterSelect, MainMenu, SettingsPanel, HintButton, TutorialOverlay, Credits)
> **Governing ADRs**: ADR-011 (UIWindow Management)
> **Engine Risk**: MEDIUM
> **Status**: Ready
> **Stories**: 10 stories created

## Overview

UI System 负责影子回忆所有玩家界面的管理和呈现，基于 TEngine UIModule 的 UIWindow/UIWidget 架构构建 9 个 UIWindow 面板。系统管理 5 个 UI 层级（Background=100 / HUD=200 / Popup=300 / Overlay=400 / System=500），Popup/Overlay 层自动 push/pop InputBlocker 阻断游戏输入。

9 个面板包括：GameHUD（5 个 widget：HintButton、PuzzleIndicator、SaveIndicator、ChapterTitle、SettingsGear）、PauseMenu、PuzzleCompletePanel（auto-close 2.5s）、ChapterSelectScreen、MainMenuScreen、SettingsPanel、HintButton overlay、TutorialOverlay、Credits。Popup 队列 FIFO（最多 1 个可见）。Safe area 适配通过 `SetUISafeFitHelper` 实现。UI 动画保持 60fps，Gaussian blur 回退策略应对低端设备。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-011: UIWindow Management | 5 层 UI 分级；Popup/Overlay 自动 InputBlocker；9 UIWindow 定义；popup queue FIFO；safe area 适配；UIWindow 生命周期规范 | MEDIUM |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|:------------:|
| TR-ui-001 | All UI via TEngine UIModule | ADR-011, ADR-001 ✅ |
| TR-ui-002 | 5 UI layer levels | ADR-011 ✅ |
| TR-ui-003 | Popup/Overlay auto InputBlocker | ADR-011, ADR-010 ✅ |
| TR-ui-004 | HUD pass-through to game | ADR-011 ✅ |
| TR-ui-005 | 9 UIWindows defined | ADR-011 ✅ |
| TR-ui-006 | GameHUD widgets (5) | ADR-011 ✅ |
| TR-ui-007 | Safe area fitting | ADR-011 ✅ |
| TR-ui-008 | Popup queue (1 visible) | ADR-011 ✅ |
| TR-ui-009 | TimeScale = 0 on PauseMenu | ADR-011 ⚠️ |
| TR-ui-010 | PuzzleCompletePanel auto-close 2.5s | ADR-011 ⚠️ |
| TR-ui-011 | Typewriter text effect | ADR-011 ⚠️ |
| TR-ui-012 | ChapterTransition 4-phase | ADR-011, ADR-009 ⚠️ |
| TR-ui-013 | Gaussian blur fallback | ADR-011 ⚠️ |
| TR-ui-014 | Animation scale accessibility | ❌ Deferred to ADR-020 (P2) |
| TR-ui-015 | HintButton opacity ramp | ADR-011, ADR-015 ⚠️ |
| TR-ui-016 | UI animations at 60fps | ADR-011, ADR-003 ✅ |
| TR-ui-017 | Gaussian blur < 2ms | ADR-011, ADR-003 ⚠️ |
| TR-ui-018 | UI prefab memory < 5MB | ADR-011, ADR-003 ✅ |
| TR-ui-019 | Touch target ≥ 44×44pt | ADR-011, ADR-003 ✅ |
| TR-ui-020 | Font size presets | ❌ Deferred to ADR-020 (P2) |
| TR-ui-021 | All text via localization keys | ❌ Deferred to ADR-022 (P2) |
| TR-ui-022 | Android back button | ADR-011 ⚠️ |

## Sprint 0 Findings Impact

- **SP-002 (UIWindow Lifecycle)**: 已确认生命周期调用时序——首次打开：`OnCreate → OnRefresh`（同帧）；重新打开：仅 `OnRefresh`；`OnUpdate` 仅在 `Visible=true` 时触发。组件引用获取放 `OnCreate`，数据刷新放 `OnRefresh`。
- **SP-009 (I2 Localization)**: 已确认 TEngine 内嵌 I2 Localization 封装。运行时语言切换无需重启场景。TR-ui-021 的本地化键替换在 P2 阶段实施。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from the GDD are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs in `production/qa/evidence/`

## Dependencies

- **chapter-state**: ChapterSelectScreen 和 GameHUD 需要查询章节/谜题进度数据
- **shadow-puzzle**: GameHUD 需要订阅 `MatchScoreChanged` 事件更新 PuzzleIndicator

## Stories

| Story ID | Title | Type | GDD Requirements | Status |
|----------|-------|------|-----------------|--------|
| ui-system-001 | UIModule Initialization + UIWindow Base Class Setup | Logic | TR-ui-001, TR-ui-002 | Ready |
| ui-system-002 | GameHUD Window (Hint Button, Puzzle Progress, Interaction Prompts) | UI | TR-ui-004, TR-ui-006, TR-ui-015 | Ready |
| ui-system-003 | PauseMenu Window (Resume, Settings, Quit) | UI | TR-ui-005, TR-ui-009 | Ready |
| ui-system-004 | PuzzleComplete Window (Score Display, Continue) | UI | TR-ui-005, TR-ui-010 | Ready |
| ui-system-005 | ChapterSelect Window (Chapter List, Lock/Unlock State) | UI | TR-ui-005, TR-ui-003 | Blocked (chapter-state) |
| ui-system-006 | MainMenu Window (New Game, Continue, Settings) | UI | TR-ui-005, TR-ui-016 | Ready |
| ui-system-007 | SettingsPanel Window (Volume, Sensitivity, Language) | UI | TR-ui-005, TR-ui-003 | Ready |
| ui-system-008 | UIWindow Layer/Order Management (Normal, Popup, Overlay) | Logic | TR-ui-002, TR-ui-003, TR-ui-008 | Ready |
| ui-system-009 | Safe Area Fitting for Notch/Rounded Corner Devices | Integration | TR-ui-007 | Ready |
| ui-system-010 | UI Text Localization via ILocalizationModule (SP-009) | Integration | TR-ui-021 | Ready (依赖 001-007) |

## Next Step

Run `/dev-story ui-system-001` to begin implementation. Recommended order: 001 → 008 → 002 → 003 → 004 → 006 → 007 → 009 → 010 → 005 (005 blocked on chapter-state).
