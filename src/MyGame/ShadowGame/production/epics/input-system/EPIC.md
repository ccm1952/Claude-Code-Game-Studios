// 该文件由Cursor 自动生成

# Epic: Input System

> **Layer**: Foundation
> **GDD**: `design/gdd/input-system.md`
> **Architecture Module**: InputService (gesture recognition, InputBlocker, InputFilter)
> **Governing ADRs**: ADR-010 (Input Abstraction), ADR-003 (Mobile-First Platform)
> **Engine Risk**: LOW
> **Status**: Ready
> **Stories**: 8 stories created (2026-04-22)

## Overview

Input System 是影子回忆的最底层交互入口，负责将原始触屏输入抽象为标准化手势事件（Tap、Drag、Rotate、Pinch、LightDrag），并通过 InputBlocker 栈和 InputFilter 栈为上层系统提供输入优先级控制。所有上层交互系统（Object Interaction、Shadow Puzzle、Tutorial 等）均通过 GameEvent 订阅手势事件，不直接访问 Unity Input API。

该系统采用三层架构：底层触摸状态机（SingleFinger FSM + DualFinger FSM）负责手势识别，中层 InputBlocker/InputFilter 栈负责输入优先级和白名单过滤，上层通过 GameEvent 广播 `OnGesture(GestureType, GesturePhase, GestureData)` 事件。所有阈值参数（dragThreshold、tapTimeout 等）均通过 Luban 配置表驱动，支持运行时通过 Settings 修改触摸灵敏度。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-010: Input Abstraction | 三层输入架构：触摸 FSM → Blocker/Filter 栈 → GameEvent 手势广播；5 种手势类型，DPI 归一化拖拽阈值 | LOW |
| ADR-003: Mobile-First Platform | 手势处理 < 0.5ms 帧预算；触摸优先，PC 映射延后 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|:------------:|
| TR-input-001 | Three-layer input architecture | ADR-010 ✅ |
| TR-input-002 | 5 gesture types with lifecycle | ADR-010 ✅ |
| TR-input-003 | PC input mapping | ADR-010 ✅ |
| TR-input-004 | InputBlocker stack-based | ADR-010 ✅ |
| TR-input-005 | InputFilter whitelist | ADR-010 ✅ |
| TR-input-006 | Blocker > Filter > Normal priority | ADR-010 ✅ |
| TR-input-007 | DPI-normalized drag threshold | ADR-010 ✅ |
| TR-input-008 | Tap timeout unscaledDeltaTime | ADR-010 ✅ |
| TR-input-009 | Rotation angle delta | ADR-010 ✅ |
| TR-input-010 | Pinch scale with safety guard | ADR-010 ✅ |
| TR-input-011 | Single-finger FSM states | ADR-010 ✅ |
| TR-input-012 | Dual-finger FSM mutual exclusion | ADR-010 ✅ |
| TR-input-013 | Max 2 touch points | ADR-010 ✅ |
| TR-input-014 | MaxDeltaPerFrame clamp | ADR-010 ✅ |
| TR-input-015 | Pause/resume clears touch state | ADR-010 ✅ |
| TR-input-016 | Gesture processing < 0.5ms | ADR-010, ADR-003 ✅ |
| TR-input-017 | Thresholds from Luban config | ADR-010, ADR-007 ✅ |
| TR-input-018 | Touch sensitivity runtime modify | ADR-010 ✅ |

## Sprint 0 Findings Impact

- **SP-001 (GameEvent Payload)**: 已确认 `GameEvent.Send<T>` 支持 struct payload（如 `GestureData`），InputService 的手势事件广播方案有效。HybridCLR AOT 泛型待真机验证。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from the GDD are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs in `production/qa/evidence/`

## Dependencies

None — Input System is a Foundation layer epic with no upstream dependencies.

## Stories

| # | Slug | Title | Type | TR-IDs | Status | Unlocks |
|---|------|-------|------|--------|--------|---------|
| 001 | `gesture-state-machine` | Single-Finger Gesture State Machine | Logic | TR-001, TR-011, TR-008, TR-014, TR-015, TR-016 | Ready | 002, 003 |
| 002 | `dual-finger-gestures` | Dual-Finger Gesture State Machine | Logic | TR-002, TR-009, TR-010, TR-012, TR-013, TR-014, TR-016 | Ready | 003 |
| 003 | `gesture-event-dispatch` | Gesture Event Dispatch | Integration | TR-002, TR-007, TR-008, TR-016 | Ready | All consumers |
| 004 | `input-blocker-stack` | InputBlocker Stack | Logic | TR-004, TR-006, TR-015 | Ready | 003 |
| 005 | `input-filter-whitelist` | InputFilter Whitelist | Logic | TR-005, TR-006 | Ready | 003 |
| 006 | `dpi-normalization` | DPI Normalization & Luban Config | Config/Data | TR-007, TR-017, TR-018 | Ready | 001, 002, 008 |
| 007 | `haptic-feedback` | Haptic Feedback Integration | Visual/Feel | Feel Criteria | Ready | — |
| 008 | `pc-input-mapping` | PC Keyboard/Mouse Input Mapping | Integration | TR-003 | Ready | Dev workflow |

### Story Dependency Graph

```
006 (Config) ──→ 001 (SingleFinger FSM)
                   ↓
006 (Config) ──→ 002 (DualFinger FSM) ←── depends on 001
                   ↓
004 (Blocker) ─→ 003 (Dispatch) ←── 005 (Filter), 001, 002
                   ↓
              All upper-layer systems (Object Interaction, Tutorial)

001 ──→ 007 (Haptic)
006 ──→ 008 (PC Mapping) ──→ 003
```

### Implementation Order (Recommended)

1. **006** — 配置加载必须先就绪，其他 story 依赖阈值
2. **001** — 单指 FSM，核心状态机
3. **004** + **005** — Blocker/Filter 可并行实现
4. **002** — 双指 FSM（依赖 001 的 CancelDrag 接口）
5. **003** — Dispatch 集成（所有候选生产者就绪后）
6. **007** — Haptic（在 003 就绪后叠加）
7. **008** — PC 映射（最后，依赖 003 dispatch 路径）

## Next Step

Run `/story-readiness input-system/story-001-gesture-state-machine` to validate the first story before implementation begins.
