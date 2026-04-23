// 该文件由Cursor 自动生成

# Epic: Hint System

> **Layer**: Feature
> **GDD**: `design/gdd/hint-system.md`
> **Architecture Module**: HintManager (3-tier progressive hints)
> **Governing ADRs**: ADR-015 (Hint System)
> **Engine Risk**: LOW
> **Status**: Ready
> **Stories**: 6 stories created

## Stories

| Story | Title | Type | Status | Estimate |
|-------|-------|------|--------|----------|
| [story-001](story-001-hint-trigger-logic.md) | Hint Trigger Logic | Logic | Ready | 2d |
| [story-002](story-002-three-tier-hints.md) | Three-Tier Hints | Logic | Ready | 2d |
| [story-003](story-003-hint-cooldown.md) | Hint Cooldown | Logic | Ready | 1d |
| [story-004](story-004-hint-display.md) | Hint Display (UI) | UI | Ready | 2d |
| [story-005](story-005-hint-puzzle-integration.md) | Hint-Puzzle Integration | Integration | Ready | 1d |
| [story-006](story-006-absence-hint-handling.md) | Absence Hint Handling (Ch.5) | Logic | Ready | 1d |

## Overview

Hint System 是影子回忆的防卡关机制，通过 3 层渐进式提示（L1 方向指引 → L2 轮廓叠加 → L3 自动吸附）在玩家遇到困难时提供适度帮助，同时避免破坏谜题发现感。系统持续监测玩家状态（idle 时间、失败次数、匹配度变化趋势），通过 triggerScore 公式计算提示触发时机。

系统以 1 秒轮询间隔只读查询 Shadow Puzzle 的 `GetMatchScore()` / `GetAnchorScores()` 数据，驱动自身的提示层状态机（Idle→Observing→L1Active→Cooldown→L2Active→Cooldown→L3Ready）。L3 自动吸附每谜题限 3 次使用（不可再生）。提示目标选取使用 `argmin(anchorScore)` 算法。所有提示延迟和阈值参数通过 Luban 配置表按章节覆写（`hintDelayOverride`）。教学期间提示定时器暂停。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-015: Hint System | 3 层渐进提示；triggerScore 公式；idle/fail/stagnation 检测；cooldown 配置；L3 使用限制；教学暂停 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|:------------:|
| TR-hint-001 | 3-tier progressive hints | ADR-015 ✅ |
| TR-hint-002 | Trigger score formula | ADR-015 ✅ |
| TR-hint-003 | L1/L2 idle thresholds | ADR-015 ✅ |
| TR-hint-004 | Cooldown with matchScore modifier | ADR-015 ✅ |
| TR-hint-005 | L3 max 3 uses per puzzle | ADR-015 ✅ |
| TR-hint-006 | Target selection argmin(anchorScore) | ADR-015 ⚠️ |
| TR-hint-007 | Read-only query to puzzle system | ADR-015 ⚠️ |
| TR-hint-008 | 1s polling interval | ADR-015 ⚠️ |
| TR-hint-009 | Pause during tutorial | ADR-015 ⚠️ |
| TR-hint-010 | hintDelayOverride per chapter | ADR-015, ADR-007 ⚠️ |
| TR-hint-011 | L1 zero additional draw calls | ADR-015, ADR-003 ⚠️ |
| TR-hint-012 | L2 rendering < 0.5ms | ADR-015, ADR-003 ⚠️ |
| TR-hint-013 | Total update < 0.5ms | ADR-015, ADR-003 ⚠️ |
| TR-hint-014 | Absence puzzle hint text (Ch.5) | ADR-015 ⚠️ |
| TR-hint-015 | Timer deltaTime capped 1.0s | ADR-015 ⚠️ |
| TR-hint-016 | App pause: timers pause, 5min reset | ADR-015 ⚠️ |
| TR-hint-017 | Stagnation detection (0.30-0.40) | ADR-015 ⚠️ |

## Sprint 0 Findings Impact

- **SP-004 (Luban Thread Safety)**: Hint 配置通过 Luban 表读取，已确认主线程只读访问安全。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from the GDD are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs in `production/qa/evidence/`

## Dependencies

- **shadow-puzzle**: 只读查询 `GetMatchScore()` / `GetAnchorScores()` 接口数据

## Next Step

Run `/story-readiness hint-system/story-001-hint-trigger-logic` to validate the first story before implementation begins.
