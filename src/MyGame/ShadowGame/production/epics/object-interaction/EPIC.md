// 该文件由Cursor 自动生成

# Epic: Object Interaction

> **Layer**: Core
> **GDD**: `design/gdd/object-interaction.md`
> **Architecture Module**: InteractionStateMachine (select, drag, rotate, snap)
> **Governing ADRs**: ADR-013 (Object Interaction State Machine), ADR-010 (Input)
> **Engine Risk**: LOW
> **Status**: Ready
> **Stories**: 7 stories created

## Overview

Object Interaction 是影子回忆"日常即重量"体验支柱的核心载体，负责实现玩家与场景中日常物件和光源的全部物理交互。系统管理每个 InteractableObject 的 6 状态状态机（Idle→Selected→Dragging→Snapping→Locked→Unlocked）和每个 LightSource 的状态机（Fixed→TrackIdle→TrackDragging→TrackSnapping），提供选中、拖拽、旋转、格点吸附、光源轨道控制等操作。

系统订阅 Input System 的手势事件（Tap/Drag/Rotate/Pinch/LightDrag），通过 Raycast 选取物体，并在操作完成时广播 `ObjectTransformChanged` 和 `LightPositionChanged` 事件供 Shadow Puzzle 系统消费。所有交互参数（gridSize、rotationStep、snapSpeed、bounds）均通过 Luban 配置表驱动。需响应 `PuzzleLockAllEvent` / `PuzzleUnlockEvent` 实现谜题锁定/解锁。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-013: Object Interaction State Machine | 物件 6 状态 FSM + 光源 FSM；Raycast 选取；格点吸附；InteractionBounds 回弹；HashSet token 锁防护 | LOW |
| ADR-010: Input Abstraction | 手势事件消费方；Fat finger compensation | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|:------------:|
| TR-objint-001 | Raycast selection on layer | ADR-013 ⚠️ |
| TR-objint-002 | Selection visual feedback | ADR-013 ⚠️ |
| TR-objint-003 | Fat finger compensation | ADR-010, ADR-013 ⚠️ |
| TR-objint-004 | Drag 1:1 tracking | ADR-013 ⚠️ |
| TR-objint-005 | Grid snap formula | ADR-013 ⚠️ |
| TR-objint-006 | Snap interpolation EaseOutQuad | ADR-013 ⚠️ |
| TR-objint-007 | Rotation snap 15° | ADR-013 ⚠️ |
| TR-objint-008 | Distance adjustment along light axis | ADR-013 ⚠️ |
| TR-objint-009 | Light source track (Linear/Arc) | ADR-013 ⚠️ |
| TR-objint-010 | Light track snap step 0.1 | ADR-013 ⚠️ |
| TR-objint-011 | InteractionBounds rebound | ADR-013 ⚠️ |
| TR-objint-012 | Object state machine (6 states) | ADR-013 ✅ |
| TR-objint-013 | Light state machine | ADR-013 ✅ |
| TR-objint-014 | ObjectTransformChanged event | ADR-006, ADR-013 ✅ |
| TR-objint-015 | LightPositionChanged event | ADR-006, ADR-013 ✅ |
| TR-objint-016 | PuzzleLock events | ADR-006 ✅ |
| TR-objint-017 | Drag response ≤ 16ms | ADR-013, ADR-003 ⚠️ |
| TR-objint-018 | All params from Luban | ADR-013, ADR-007 ⚠️ |
| TR-objint-019 | 10 objects ≥ 55fps on iPhone 13 Mini | ADR-013, ADR-003 ⚠️ |
| TR-objint-020 | Update processing < 1ms | ADR-013, ADR-003 ⚠️ |
| TR-objint-021 | 200ms selection debounce | ADR-013 ⚠️ |
| TR-objint-022 | Haptic feedback cross-platform | ❌ Deferred to ADR-025 (P2) |

## Sprint 0 Findings Impact

- **SP-001 (GameEvent Payload)**: 已确认 struct payload 支持，`ObjectTransformChanged(int, Vector3, Quaternion)` 等多参数事件方案有效。
- **SP-006 (PuzzleLockAll Token)**: 决策采用 HashSet token 防护机制，`PushLock(token)` / `PopLock(token)` 防止 Narrative 和 Puzzle 的 unlock 错误配对。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from the GDD are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs in `production/qa/evidence/`

## Dependencies

- **input-system**: 消费 Input System 的手势事件（Tap/Drag/Rotate/Pinch/LightDrag）

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | Object Interaction State Machine | Logic | Ready | ADR-013 |
| 002 | Touch-to-World Drag with Boundary Clamping | Logic | Ready | ADR-013 |
| 003 | Single-Finger Rotation with Snap to Grid | Logic | Ready | ADR-013 |
| 004 | Grid Snapping System | Logic | Ready | ADR-013 |
| 005 | Selection Visual Feedback (Outline + Scale Bounce) | Visual/Feel | Ready | ADR-013 |
| 006 | InteractionLockManager with HashSet Token (SP-006) | Logic | Ready | ADR-013, ADR-006 |
| 007 | Multiple Interactable Objects — Single Selection | Integration | Ready | ADR-013, ADR-010 |

## Next Step

Run `/story-readiness story-001-interaction-state-machine` → `/dev-story` to begin implementation. Work through stories in order — each story's `Depends on:` field tells you what must be DONE before starting it.
