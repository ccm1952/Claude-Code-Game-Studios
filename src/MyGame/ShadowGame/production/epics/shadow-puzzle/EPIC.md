// 该文件由Cursor 自动生成

# Epic: Shadow Puzzle System

> **Layer**: Feature
> **GDD**: `design/gdd/shadow-puzzle-system.md`
> **Architecture Module**: ShadowPuzzleManager (match scoring, state machine, PerfectMatch)
> **Governing ADRs**: ADR-012 (Shadow Match Algorithm), ADR-014 (Puzzle State Machine), ADR-006 (GameEvent)
> **Engine Risk**: LOW
> **Status**: Ready
> **Stories**: 8 stories created

## Stories

| Story | Title | Type | Status | Estimate |
|-------|-------|------|--------|----------|
| [story-001](story-001-puzzle-state-machine.md) | Puzzle State Machine | Logic | Ready | 2d |
| [story-002](story-002-match-score-calculation.md) | Match Score Calculation | Logic | Ready | 2d |
| [story-003](story-003-shadow-rt-sampling.md) | Shadow RT Sampling | Integration | Ready | 2d |
| [story-004](story-004-match-thresholds.md) | Match Thresholds (Luban Config) | Config/Data | Ready | 1d |
| [story-005](story-005-perfect-match-sequence.md) | Perfect Match Sequence | Integration | Ready | 2d |
| [story-006](story-006-absence-puzzle.md) | Absence Puzzle (Ch.5) | Logic | Ready | 2d |
| [story-007](story-007-match-feedback.md) | Match Feedback (Visual/Audio) | Visual/Feel | Ready | 2d |
| [story-008](story-008-puzzle-reset.md) | Puzzle Reset | Logic | Ready | 1d |

## Overview

Shadow Puzzle System 是影子回忆的核心玩法系统，实现"摆放物件 → 调整光源 → 形成影子 → 触发记忆"的核心循环。系统管理谜题状态机（Locked→Idle→Active→NearMatch→PerfectMatch→AbsenceAccepted→Complete），消费 Object Interaction 的 `ObjectTransformChanged` / `LightPositionChanged` 事件和 URP Shadow Rendering 的 ShadowRT 数据，通过多锚点加权评分算法实时计算 matchScore。

匹配评分采用 per-anchor multiplicative scoring（pos × dir × vis），支持 NearMatch 滞后阈值（0.40/0.35 进入/退出）和 PerfectMatch 阈值（0.85，可按谜题配置）。第五章支持特殊的 AbsenceAccepted 状态（残缺型谜题，不可完整还原）。matchScore 采用 0.2s 滑窗时间平滑。所有谜题参数通过 Luban `TbPuzzle` / `TbPuzzleObject` 配置表驱动。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-012: Shadow Match Algorithm | 多锚点加权评分；pos × dir × vis multiplicative；AsyncGPUReadback 采样管线；< 2ms 帧预算 | LOW |
| ADR-014: Puzzle State Machine | 7 状态 FSM；NearMatch 滞后阈值；PerfectMatch snap；AbsenceAccepted Ch.5；tutorial grace period | LOW |
| ADR-006: GameEvent Protocol | PerfectMatchEvent / PuzzleCompleteEvent / MatchScoreChanged / NearMatchEnter/Exit 事件广播 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|:------------:|
| TR-puzzle-001 | Multi-anchor weighted scoring | ADR-012 ✅ |
| TR-puzzle-002 | Per-anchor multiplicative score | ADR-012 ✅ |
| TR-puzzle-003 | NearMatch hysteresis (0.40/0.35) | ADR-012, ADR-014 ✅ |
| TR-puzzle-004 | PerfectMatch threshold 0.85 | ADR-012, ADR-014 ✅ |
| TR-puzzle-005 | Puzzle state machine | ADR-014 ✅ |
| TR-puzzle-006 | AbsenceAccepted for Ch.5 | ADR-014 ⚠️ |
| TR-puzzle-007 | matchScore temporal smoothing | ADR-012 ⚠️ |
| TR-puzzle-008 | Tutorial grace period | ADR-014 ⚠️ |
| TR-puzzle-009 | PerfectMatch snap animation | ADR-014 ⚠️ |
| TR-puzzle-010 | Shadow match calc < 2ms | ADR-012, ADR-003 ✅ |
| TR-puzzle-011 | 60fps with < 150 draw calls | ADR-003 ✅ |
| TR-puzzle-012 | Collectibles don't affect scoring | ADR-014 ⚠️ |
| TR-puzzle-013 | Chapter-difficulty parameter matrix | ADR-014, ADR-007 ⚠️ |
| TR-puzzle-014 | Per-puzzle config via Luban | ADR-014, ADR-007 ⚠️ |

## Sprint 0 Findings Impact

- **SP-007 (HybridCLR + AsyncGPUReadback)**: Shadow Puzzle 的 matchScore 计算依赖 ShadowRT 的 CPU 读回数据。若 HybridCLR 不兼容 AsyncGPUReadback 回调，需通过 `IShadowRTReader` 接口从 Default 程序集获取数据。
- **SP-006 (PuzzleLockAll Token)**: PuzzleLockAllEvent 采用 HashSet token 防护，Shadow Puzzle 和 Narrative 各持有独立 token。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from the GDD are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs in `production/qa/evidence/`

## Dependencies

- **input-system**: 间接依赖（通过 Object Interaction 消费手势事件）
- **object-interaction**: 消费 `ObjectTransformChanged` / `LightPositionChanged` 事件
- **urp-shadow-rendering**: 消费 ShadowRT 像素数据用于 match scoring
- **chapter-state**: 查询谜题配置和解锁状态

## Next Step

Run `/story-readiness shadow-puzzle/story-001-puzzle-state-machine` to validate the first story before implementation begins.
