// 该文件由Cursor 自动生成

# Grep Evidence — S2-11 Single-Finger Rotation Protocol Compliance (ADR-006 → ADR-027)

**Date**: 2026-04-30 morning
**Story**: S2-11 Single-Finger Rotation (`production/epics/object-interaction/story-003-rotation-mechanics.md`)
**Scope**: `Assets/GameScripts/HotFix/`

## 验证目标

1. **0** 处使用 `EventId.Evt_RotateGesture` / `Evt_ObjectTransformChanged`（ADR-006 全废）
2. Sender 派发用 `GameEvent.Get<IInteractionEvent>().OnObjectTransformChanged(...)` —— 仅在 SnapRotation OnComplete 派发
3. Listener 端 OnRotate 走 per-event 模式（InteractionCoordinator 端注册；InteractableObject 不订阅 IGestureEvent.OnRotate 自身，由 Coordinator 转发到 HandleRotate）

## 命令 & 结果

```bash
rg "EventId\.Evt_(RotateGesture|ObjectTransformChanged)" \
   src/MyGame/ShadowGame/Assets/GameScripts/HotFix/
# → 0 匹配 ✅

rg "EventId\.Evt_" src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/
# → 0 匹配 ✅（接续 S2-08/S2-09/S2-10/S2-12/S2-13 的 0 命中状态）
```

## Sender 端协议（InteractableObject.SnapRotation）

```csharp
// 同步路径（已在格点）
GameEvent.Get<IInteractionEvent>()
    .OnObjectTransformChanged(Fsm?.ObjectId ?? _objectId, transform.position, transform.rotation);

// 异步路径（DOTween OnComplete）
transform.DORotate(new Vector3(0f, 0f, snapped), _puzzleConfig.SnapSpeed)
    .SetEase(DG.Tweening.Ease.OutQuad)
    .OnComplete(() => {
        AccumulatedAngleDeg = snapped;
        if (Fsm == null) return;
        GameEvent.Get<IInteractionEvent>()
            .OnObjectTransformChanged(Fsm.ObjectId, transform.position, transform.rotation);
    });
```

✅ 派发用 `GameEvent.Get<IInteractionEvent>()` facade（ADR-027 §3 推荐路径），**不**经 `EventId.Evt_*` 常量。

## Listener 端协议（InteractionCoordinator.Initialize）

```csharp
GameEvent.AddEventListener<GestureData>(IGestureEvent_Event.OnRotate, OnRotate);
```

```csharp
private void OnRotate(GestureData data)
{
    if (CurrentSelectedObject == null) return;
    CurrentSelectedObject.HandleRotate(data);
}
```

✅ Per-event 模式（与 S2-12/S2-13 一致）；Coordinator **不**实施 IGestureEvent 接口本身（POCO MonoBehaviour）。

## 测试端协议合规

`RotationMechanicsTests.cs` 16 NUnit：
- AC-7 用 `GameEvent.Get<IGestureEvent>().OnRotate(...)` 端到端 round-trip 验证
- AC-4 用 `OnObjectTransformChanged` listener 计数验证派发时机（OnComplete 后 1 次）
- AC-3 已在格点路径同步派发计数验证（不依赖 DOTween OnComplete）

## 结论

**PASS** — S2-11 实施 0 处 ADR-006 残留；Listener 端 per-event 模式；Coordinator 转发 HandleRotate 与 S2-13 OnDrag 转发对齐；OnObjectTransformChanged sender 单一（InteractableObject.SnapRotation）。
