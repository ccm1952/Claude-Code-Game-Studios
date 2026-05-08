// 该文件由Cursor 自动生成

# Grep Evidence — S2-10 Grid Snap Protocol Compliance (ADR-006 → ADR-027 Migration)

**Date**: 2026-04-29
**Story**: S2-10 Grid Snapping System (`production/epics/object-interaction/story-004-grid-snap.md`)
**Scope**: `Assets/GameScripts/HotFix/`（含 GameLogic + GameProto；不含 TEngine 框架内核）

## 验证目标

确认 S2-10 实施期间：
1. **0** 处使用 `EventId.Evt_ObjectTransformChanged`（ADR-006 全废，整迁移到 `IInteractionEvent.OnObjectTransformChanged` per ADR-027）
2. **0** 处使用 `ObjectTransformChangedPayload` struct（ADR-027 §2 废 payload struct，参数直接传值）
3. **0** 处对 `IInteractionEvent.OnObjectTransformChanged` 用 `EventId.*` 路径替代
4. snap 相关订阅（Shadow Puzzle / Save System listener）一律用 `GameEvent.AddEventListener<int, Vector3, Quaternion>(IInteractionEvent_Event.OnObjectTransformChanged, ...)`

## 命令 & 结果

```bash
# §1 — Evt_ObjectTransformChanged 残留
rg "EventId\.Evt_ObjectTransformChanged" \
   src/MyGame/ShadowGame/Assets/GameScripts/HotFix/
# → 0 匹配 ✅

# §2 — ObjectTransformChangedPayload struct 残留
rg "ObjectTransformChangedPayload" \
   src/MyGame/ShadowGame/Assets/GameScripts/HotFix/
# → 0 匹配 ✅

# §3 — Object Interaction 子目录全部 EventId.Evt_* 残留
rg "EventId\.Evt_" \
   src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/
# → 0 匹配 ✅（接续 S2-08/S2-09 的 0 命中状态）
```

## Sender 协议合规路径（实施实测）

`InteractableObject.OnSnapComplete()` (S2-10 新增) → `Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/InteractableObject.cs:386-389`：

```csharp
private void OnSnapComplete()
{
    if (Fsm == null) return;
    Fsm.OnSnapCompleted();
    GameEvent.Get<IInteractionEvent>()
        .OnObjectTransformChanged(Fsm?.ObjectId ?? _objectId, transform.position, transform.rotation);
}
```

✅ 派发用 `GameEvent.Get<IInteractionEvent>()` facade（ADR-027 §3 推荐路径），**不**经 `EventId.Evt_*` 常量。

## Listener 端（计划 — 本 story 不实装）

| Listener | 实装时机 | 注册形式（强制） |
|---|---|---|
| Shadow Puzzle 重算阴影匹配 | S2 后期 / Shadow Puzzle epic | `GameEvent.AddEventListener<int, Vector3, Quaternion>(IInteractionEvent_Event.OnObjectTransformChanged, OnTransformChanged)` |
| Save System 持久化布局 | S2-04 已注册 IChapterProgress；OnObjectTransformChanged listener 在 Save layer epic 后续 story | 同上 |
| Analytics 埋点 | Analytics epic | 同上 |

**禁止** patterns（ADR-027 + ADR-028 反例）：
- `EventMgr.AddEventListener<ObjectTransformChangedPayload>(EventId.Evt_ObjectTransformChanged, ...)` — 老 ADR-006 风格已废
- 使用 `Get<IInteractionEvent>().OnObjectTransformChanged(...)` 后再 cascade 派发新 IGestureEvent.* — 违反 ADR-027 §2 cascade ≤ 3

## 中断路径不派发证据

S2-10 中断分支 (story-004 AC-6 / AC-7) 在 `OnFsmStateChanged` Snapping → 非 Idle 时仅 `DOKill(false)` 中断 tween，**不**调 `OnSnapComplete`，**不**派发 `OnObjectTransformChanged`：

`InteractableObject.OnFsmStateChanged()` → `Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/InteractableObject.cs:289-296`：

```csharp
if (prev == InteractableObjectState.Snapping && next != InteractableObjectState.Idle)
{
    transform.DOKill(complete: false);
    _didStartSnap = false;
    // 落点未定 — 不派发 IInteractionEvent.OnObjectTransformChanged
}
```

✅ 与 ADR-013 §"Grid Snap Mechanics" "snap 中断后落点未定" 语义对齐；与 InteractableObjectFsm 转换规则 6/7（`Snapping + OnTapHit → Selected` / `Snapping + OnLockChanged(true) → Locked`）配套。

## 结论

**PASS** — S2-10 实施 0 处 ADR-006 残留，0 处 payload struct 残留；与 ADR-027 §1 IInteractionEvent.OnObjectTransformChanged 协议 100% 合规。
