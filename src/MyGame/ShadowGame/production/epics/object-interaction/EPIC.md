// 该文件由Cursor 自动生成

# Epic: Object Interaction

> **Layer**: Core
> **GDD**: `design/gdd/object-interaction.md`
> **Architecture Module**: InteractionStateMachine (select, drag, rotate, snap)
> **Governing ADRs**: ADR-013 (Object Interaction State Machine, **Accepted** 2026-04-29), ADR-027 (GameEvent Interface Protocol, **Accepted**), ADR-010 (Input Abstraction)
> **Engine Risk**: LOW
> **Status**: Ready
> **Stories**: 7 stories created
> **Last Migrated**: 2026-04-29 (X1: ADR-006 Evt_* / Payload struct → ADR-027 IInteractionEvent；6 states → 5 states + Rotating 子模式；MonoBehaviour 拆分为纯 C# Fsm + 绑定层)

## Overview

Object Interaction 是影子回忆"日常即重量"体验支柱的核心载体，负责实现玩家与场景中日常物件和光源的全部物理交互。系统管理每个 InteractableObject 的 **5 states 状态机**（Idle → Selected → Dragging → Snapping → Locked；Rotating 是 Selected 子模式），实施为**纯 C# `InteractableObjectFsm`** + 薄 `InteractableObject` MonoBehaviour 绑定层（与 Scene/SingleFinger/DualFinger FSM 同模式）。LightSource FSM（Fixed/TrackIdle/TrackDragging/TrackSnapping）非 MVP，接口位 `IInteractionEvent.OnLightPositionChanged` 已冻结。

系统消费 `IGestureEvent`（Tap/Drag/Rotate；ADR-010 / Sprint 1 已落地），通过 `InteractionCoordinator` 做 Raycast 选取 + 单选语义 + 200ms debounce，并在 snap/rotation 完成时通过 `IInteractionEvent.OnObjectTransformChanged` 广播给 Shadow Puzzle / Save System。所有交互参数（gridSize、rotationStep、snapSpeed、bounds）通过 Luban `TbPuzzle` 配置驱动。`InteractionLockManager` 监听 `IInteractionEvent.OnRequestPuzzleLockAll` / `OnRequestPuzzleUnlock`（来自 Narrative / Shadow Puzzle）+ `ISceneEvent.OnSceneUnloadBegin`（场景过渡时强清 token），通过 `OnInteractionLockChanged` 通知所有 InteractableObjectFsm。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| **ADR-013: Object Interaction State Machine** (**Accepted** 2026-04-29) | 5 states 纯 C# FSM + Rotating 子模式；Raycast 选取；格点吸附；InteractionBounds 回弹；HashSet token 锁防护 | LOW |
| **ADR-027: GameEvent Interface Protocol** (**Accepted**) | 单一 `IInteractionEvent` 接口替代 ADR-006 `Evt_*` 常量 + Payload struct；接口签名见 `Assets/GameScripts/HotFix/GameLogic/IEvent/IInteractionEvent.cs` | LOW |
| **ADR-010: Input Abstraction** | 手势事件消费方（`IGestureEvent.OnTap/OnDrag/OnRotate`）；Fat finger compensation | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|:------------:|
| TR-objint-001 | Raycast selection on layer | ADR-013 ⚠️ |
| TR-objint-002 | Selection visual feedback | ADR-013 ⚠️ |
| TR-objint-003 | Fat finger compensation | ADR-010, ADR-013 ⚠️ |
| TR-objint-004 | Drag 1:1 tracking | ADR-013 ⚠️ |
| TR-objint-005 | Grid snap formula | ADR-013 ⚠️ |
| TR-objint-006 | Snap interpolation EaseOutQuad | ADR-013 ⚠️ |
| TR-objint-007 | Rotation snap 15° (Selected 子模式) | ADR-013 ⚠️ |
| TR-objint-008 | Distance adjustment along light axis | ADR-013 ⚠️ |
| TR-objint-009 | Light source track (Linear/Arc) | ADR-013 ⚠️ |
| TR-objint-010 | Light track snap step 0.1 | ADR-013 ⚠️ |
| TR-objint-011 | InteractionBounds rebound | ADR-013 ⚠️ |
| TR-objint-012 | **5-state object FSM** + Rotating 子模式（修订 2026-04-29） | ADR-013 ✅ |
| TR-objint-013 | Light state machine（接口位 `IInteractionEvent.OnLightPositionChanged` 已冻结，实施延后） | ADR-013 ✅ |
| TR-objint-014 | `IInteractionEvent.OnObjectTransformChanged`（替代 Evt_ObjectTransformChanged） | ADR-013, ADR-027 ✅ |
| TR-objint-015 | `IInteractionEvent.OnLightPositionChanged`（[Reserved]） | ADR-013, ADR-027 ✅ |
| TR-objint-016 | `IInteractionEvent.OnRequestPuzzleLockAll/Unlock` + `OnInteractionLockChanged`（替代 Evt_PuzzleLockAll/Unlock） | ADR-013, ADR-027 ✅ |
| TR-objint-017 | Drag response ≤ 16ms | ADR-013, ADR-003 ⚠️ |
| TR-objint-018 | All params from Luban | ADR-013, ADR-007 ⚠️ |
| TR-objint-019 | 10 objects ≥ 55fps on iPhone 13 Mini | ADR-013, ADR-003 ⚠️ |
| TR-objint-020 | Update processing < 1ms | ADR-013, ADR-003 ⚠️ |
| TR-objint-021 | 200ms selection debounce | ADR-013 ⚠️ |
| TR-objint-022 | Haptic feedback cross-platform | ❌ Deferred to ADR-025 (P2) |

## Sprint 0 Findings Impact

- **SP-001 (GameEvent Payload)**: 旧方案（`struct payload` 多参数）已**废弃**；ADR-027 改为单一接口方法签名直接传参（`OnObjectTransformChanged(int, Vector3, Quaternion)`），效果等价但不再依赖 payload struct。
- **SP-006 (PuzzleLockAll Token)**: 决策保留——HashSet token 防护机制；但事件协议升级为 `IInteractionEvent.OnRequestPuzzleLockAll(string lockerId)` / `OnRequestPuzzleUnlock(string lockerId)` + `OnInteractionLockChanged(bool)` 通知。`InteractionLockerId` 三常量保留（`shadow_puzzle` / `narrative` / `tutorial`）。

## Definition of Done

This epic is complete when:
- All 7 stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from the GDD are verified
- All Logic and Integration stories have passing test files in `Assets/Tests/EditMode/ObjectInteraction/`
- All Visual/Feel and UI stories have evidence docs in `production/qa/evidence/`
- **协议合规 grep** 通过：`rg "EventId\.Evt_(PuzzleLockAll|PuzzleUnlock|ObjectTransformChanged|ObjectSelected|ObjectDeselected|TapGesture|DragGesture|RotateGesture|SceneUnloadBegin)" Assets/GameScripts/HotFix/` 输出 0 命中（除注释 / TODO）

## Dependencies

- **input-system epic**：消费 `IGestureEvent`（已 active 2026-04-?）
- **scene-management epic**：`InteractionLockManager` 订阅 `ISceneEvent.OnSceneUnloadBegin`（已 active 2026-04-29）

## Stories

| # | Story | Type | Status | Governing ADRs |
|---|-------|------|--------|----------------|
| 001 | Object Interaction State Machine（**5 states + Rotating 子模式 + 纯 C# Fsm + 绑定层**） | Logic | Ready | ADR-013, ADR-027 |
| 002 | Touch-to-World Drag with Boundary Clamping | Logic | Ready | ADR-013, ADR-010, ADR-027 |
| 003 | Single-Finger Rotation with Snap to Grid（Selected 子模式） | Logic | Ready | ADR-013, ADR-010, ADR-027 |
| 004 | Grid Snapping System | Logic | Ready | ADR-013, ADR-027 |
| 005 | Selection Visual Feedback (Outline + Scale Bounce) — 走 fsm.StateChanged C# event | Visual/Feel | Ready | ADR-013, ADR-011 |
| 006 | InteractionLockManager with HashSet Token (SP-006) — 实施 IInteractionEvent + ISceneEvent listener | Logic | Ready | ADR-013, ADR-027 |
| 007 | Multiple Interactable Objects — Single Selection — InteractionCoordinator | Integration | Ready | ADR-013, ADR-010, ADR-027 |

## Next Step

Run `/story-readiness story-001-interaction-state-machine` → `/dev-story` to begin implementation. Work through stories in order — each story's `Depends on:` field tells you what must be DONE before starting it. **建议执行顺序**：001 → 002 → 003 → 004 → 005 → 006 → 007（与 dependency graph 拓扑序一致）。

> **协议合规守则**：所有 7 stories 已迁移到 `IInteractionEvent` + `IGestureEvent`，**禁止**在实施代码中引入 `EventId.Evt_*` 常量或 `XxxPayload` struct。在 `/code-review` 阶段会用 grep 强制校验。
