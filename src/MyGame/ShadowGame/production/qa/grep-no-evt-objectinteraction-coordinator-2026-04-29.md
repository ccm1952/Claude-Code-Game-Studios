// 该文件由Cursor 自动生成

# Grep Evidence — S2-13 InteractionCoordinator Protocol Compliance (ADR-006 → ADR-027)

**Date**: 2026-04-29 night X3
**Story**: S2-13 InteractionCoordinator (`production/epics/object-interaction/story-007-multi-object-scene.md`)
**Scope**: `Assets/GameScripts/HotFix/`

## 验证目标

1. **0** 处使用 `EventId.Evt_TapGesture` / `Evt_DragGesture` / `Evt_RotateGesture` / `Evt_PuzzleLockAll` / `Evt_PuzzleUnlock` / `Evt_ObjectTransformChanged` / `Evt_ObjectSelected` / `Evt_ObjectDeselected`（ADR-006 全废）
2. Coordinator 端：sender 走 `GameEvent.Get<IXxxEvent>().OnYyy(...)`（**仅**通过 fsm 间接派发；Coordinator 自身不派发 IInteractionEvent）
3. Listener 端：per-event 模式 `GameEvent.AddEventListener<TArg>(IXxxEvent_Event.OnYyy, handler)`（与 S2-12 ListenerManager 同模式）

## 命令 & 结果

```bash
rg "EventId\.Evt_(TapGesture|DragGesture|RotateGesture|PuzzleLockAll|PuzzleUnlock|ObjectTransformChanged|ObjectSelected|ObjectDeselected)" \
   src/MyGame/ShadowGame/Assets/GameScripts/HotFix/
# → 0 匹配 ✅

rg "EventId\.Evt_" src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/
# → 0 匹配 ✅（接续 S2-08/S2-09/S2-10/S2-12 的 0 命中状态）
```

## InteractionCoordinator Listener 端协议

```csharp
// InteractionCoordinator.Initialize() — per-event 模式
GameEvent.AddEventListener<GestureData>(IGestureEvent_Event.OnTap, OnTap);
GameEvent.AddEventListener<GestureData>(IGestureEvent_Event.OnDrag, OnDrag);
GameEvent.AddEventListener<bool>(IInteractionEvent_Event.OnInteractionLockChanged, OnInteractionLockChanged);
```

✅ 与 S2-12 InteractionLockManager 同模式；**不**用 `class : IGestureEvent + AddEventListener<TInterface>(this)` 整接口订阅幻想（TEngine 不支持）。

## InteractionCoordinator Sender 端协议

Coordinator **不**直接派发任何 IInteractionEvent —— 所有 OnObjectSelected / OnObjectDeselected 由 InteractableObjectFsm 在 OnTapHit / OnDeselect / OnLockChanged 内部派（S2-08 已实施）。Coordinator 仅做 **fsm trigger**：
- `_selectedObject.Fsm.OnTapHit()` → fsm 内部派 OnObjectSelected
- `_selectedObject.Fsm.OnDeselect()` → fsm 内部派 OnObjectDeselected
- `_selectedObject.Fsm.OnDragBegan() / OnDragEnded()` → fsm transition（无对外事件派发）

✅ 避免双 sender 风险（参 ADR-027 §"Sender 唯一性约束"）。

## 测试端协议合规

`InteractionCoordinatorTests.cs` 17 NUnit：
- `Protocol_TrySelectObject_DoesNotDispatchEventsItself_RelyOnFsm` 显式验证 Coordinator **不**自派 OnObjectSelected/Deselected，所有计数源自 fsm 内部派
- `AC6_OnInteractionLockChanged_True_*` 验证 fsm.OnLockChanged from Selected 派 OnObjectDeselected 一次（不重复）
- 所有 fixture 用 `GameEvent.Get<IGestureEvent>().OnDrag(...)` / `GameEvent.Get<IInteractionEvent>().OnInteractionLockChanged(...)` 端到端 round-trip

## 结论

**PASS** — S2-13 实施 0 处 ADR-006 残留；Coordinator listener 走 per-event 模式与 S2-12 / S2-08 一致；Coordinator 不重复派 IInteractionEvent（fsm 是唯一 sender）。
