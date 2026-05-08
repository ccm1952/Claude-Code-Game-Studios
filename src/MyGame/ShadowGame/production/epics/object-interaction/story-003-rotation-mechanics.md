// 该文件由Cursor 自动生成

# Story 003: Single-Finger Rotation with Snap to Grid

> **Epic**: Object Interaction
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-30 morning (X1 patch v2: §Implementation Notes 修订对齐 framework — InteractableObject **不**实施 IGestureEvent 整接口订阅，改为公开 `HandleRotate(GestureData)` 由 InteractionCoordinator 转发（与 S2-13 OnDrag 同模式 + §设计权衡推荐）；PuzzleConfig 加 `RotationStep`（默认 15f，向下兼容）；详见 `/.claude/memory/problem_2026-04-29_story-impl-notes-vs-framework-drift.md`)

## Context

**GDD**: `design/gdd/object-interaction.md`
**Requirement**: `TR-objint-007`
*(Rotation snap 15°; single-finger rotation as Selected sub-mode)*

**ADR Governing Implementation**: ADR-013（**Accepted**, 2026-04-29）+ ADR-010 (Input) + ADR-027 (GameEvent Interface Protocol)
**ADR Decision Summary**: Rotation 是 `Selected` 状态下的**子模式**（不是独立 state）。`IGestureEvent.OnRotate(GestureData)` 在 `Selected` / `Dragging` 状态下叠加 `transform.eulerAngles.z`；`data.Phase == Ended` 时 snap 到 `rotationStep` 倍数（默认 15°）。`rotationStep` / `snapSpeed` 来自 Luban `TbPuzzle`。Snap OnComplete 派 `IInteractionEvent.OnObjectTransformChanged`。

**Engine**: Unity 2022.3.62f2 LTS + DOTween | **Risk**: LOW
**Engine Notes**:
- `GestureData.AngleDelta` 单位 **弧度/帧**（参 `Assets/GameScripts/HotFix/GameLogic/Input/GestureTypes.cs:36`，正向 = CCW）；本 story 转为度（`data.AngleDelta * Mathf.Rad2Deg`）后累加到 `_accumulatedAngleDeg`（写 `transform.eulerAngles.z` 时由 Unity 内部 wrap 到 [0,360)）。
- Snap：`snappedAngle = Mathf.Round(_accumulatedAngleDeg / rotationStep) * rotationStep`，再 `(snapped % 360 + 360) % 360` 归一化到 `[0, 360)`。
- DOTween 用 `transform.DORotate(target, snapSpeed).SetEase(Ease.OutQuad)`；OnComplete 内 `AccumulatedAngleDeg = snapped` 后派 `IInteractionEvent.OnObjectTransformChanged`。
- **已在格点路径**（`abs(deltaAngle(currentNormalized, snapped)) < 0.01°`）→ 跳过 DOTween，同步走 OnComplete 路径（写 transform + 派事件，保证下游一致）。
- **Listener 模式**：InteractableObject **不**实施 `IGestureEvent` 整接口（与 S2-12/S2-13 同 family — `GameEvent.AddEventListener<TInterface>(this)` API **不存在**）。Rotation 手势由 `InteractionCoordinator.OnRotate` per-event listener 接收并转发到 `_selectedObject.HandleRotate(data)`（与 S2-13 OnDrag 同模式 + §设计权衡推荐：避免 N obj 同时订阅 IGestureEvent 造成 N×派发，遵守 TR-objint-020 ≤1ms 守则）。

**Control Manifest Rules (this layer)**:
- Required: `Rotating 是 Selected 子模式 — 不引发 fsm 状态转换` (ADR-013)
- Required: `所有参数从 Luban 配置读取（rotationStep, snapSpeed from TbPuzzle）` (ADR-013, ADR-007)
- Required: `Grid snap on finger release only — during rotation, object follows gesture freely` (ADR-013)
- Required: `Rotation snap OnComplete 派 IInteractionEvent.OnObjectTransformChanged` (ADR-013, ADR-027)
- Forbidden: `禁止使用 EventId.Evt_RotateGesture / Evt_ObjectTransformChanged 常量` (ADR-006 全废)
- Forbidden: `禁止 ObjectTransformChangedPayload struct` (ADR-027)
- Guardrail: `Object Interaction total update (10 objects) ≤ 1.0ms/frame` (ADR-013)

---

## Acceptance Criteria

*From GDD `design/gdd/object-interaction.md` + ADR-013，scoped to this story:*

- [x] InteractableObject 公开 `HandleRotate(GestureData)`（**不**实施 IGestureEvent 整接口订阅 — 与 S2-12/S2-13 family 一致）；仅在 `Fsm.CurrentState == Selected || Dragging` 时响应；其他状态（Idle / Snapping / Locked）静默忽略 — `InteractableObject.HandleRotate` 守卫
- [x] **持续旋转**（`data.Phase == Began`）：`AccumulatedAngleDeg = transform.eulerAngles.z + data.AngleDelta * Mathf.Rad2Deg`；写 transform — 自由跟手指无 snap — `InteractableObject.HandleRotate` Began case
- [x] **持续旋转**（`data.Phase == Updated`）：`AccumulatedAngleDeg += data.AngleDelta * Mathf.Rad2Deg`；写 transform — 自由跟手指无 snap — `InteractableObject.HandleRotate` Updated case
- [x] **释放 snap**（`data.Phase == Ended`）：`snapped = round(AccumulatedAngleDeg / RotationStep) * RotationStep`；归一化到 `[0, 360)`；`transform.DORotate(new Vector3(0, 0, snapped), SnapSpeed).SetEase(Ease.OutQuad)` — `InteractableObject.SnapRotation`
- [x] **Cancelled phase**：`data.Phase == Cancelled` 与 Ended 等价（同走 SnapRotation 路径）
- [x] **rotationStep**（默认 15°）从 PuzzleConfig 读取（生产从 Luban `TbPuzzle`；S2-11 阶段经 `InteractableObject.RegisterPuzzleConfigProvider` 注入）
- [x] **snapSpeed** 从 PuzzleConfig 读取（与 grid snap S2-10 复用 `PuzzleConfig.SnapSpeed`）
- [x] Rotation snap OnComplete：派 `GameEvent.Get<IInteractionEvent>().OnObjectTransformChanged(Fsm.ObjectId, transform.position, transform.rotation)` — 唯一 sender
- [x] **已在格点路径**：`abs(Mathf.DeltaAngle(currentNormalized, snapped)) < 0.01°` → 跳 DOTween，同步走 OnComplete 路径（保证一致下游）
- [x] **Locked 状态忽略**：`Fsm.CurrentState == Locked` 时 HandleRotate 守卫返回；累加器保留为该刻值，**不** snap，**不**派 OnObjectTransformChanged
- [x] **角度 wrap-around**：用 `AccumulatedAngleDeg : float` public getter 累加（可超 360 / 负），仅 snap 时归一化 — 验证 350°+30° → 累加器 380°，snap → 15°
- [x] **Coordinator 转发**：`InteractionCoordinator.OnRotate` per-event listener → `CurrentSelectedObject.HandleRotate(data)`（与 S2-13 OnDrag 同模式）；无 selected 时 NoOp
- [x] **协议合规**：本 story 实装 0 处使用 `EventId.Evt_RotateGesture` / `Evt_ObjectTransformChanged`（grep evidence `production/qa/grep-no-evt-objectinteraction-rotation-2026-04-30.md`）；sender 用 `GameEvent.Get<IInteractionEvent>().OnObjectTransformChanged(...)` facade

---

## Implementation Notes

*Derived from ADR-013 §"Grid Snap Mechanics"（角度版）+ §"State Transition Rules"（Rotating 子模式）+ S2-13 OnDrag 转发模式：*

**实施决策（v2 patch）**：采纳 §设计权衡推荐 — Coordinator 转发，**不**实施 IGestureEvent 整接口（与 S2-12/S2-13 family — `GameEvent.AddEventListener<TInterface>(this)` 不存在；详见 `/.claude/memory/problem_2026-04-29_story-impl-notes-vs-framework-drift.md`）。

```csharp
// InteractableObject.cs（S2-11 扩展）
public sealed class InteractableObject : MonoBehaviour
{
    public float AccumulatedAngleDeg { get; private set; }   // public getter — wrap-around 累加器，测试可读
    private const float RotationSnapEpsilonDeg = 0.01f;

    public void HandleRotate(GestureData data)              // 由 InteractionCoordinator 转发
    {
        if (Fsm == null) return;
        var st = Fsm.CurrentState;
        if (st != InteractableObjectState.Selected && st != InteractableObjectState.Dragging) return;

        switch (data.Phase)
        {
            case GesturePhase.Began:
                AccumulatedAngleDeg = transform.eulerAngles.z + data.AngleDelta * Mathf.Rad2Deg;
                transform.eulerAngles = new Vector3(0f, 0f, AccumulatedAngleDeg);
                break;
            case GesturePhase.Updated:
                AccumulatedAngleDeg += data.AngleDelta * Mathf.Rad2Deg;
                transform.eulerAngles = new Vector3(0f, 0f, AccumulatedAngleDeg);
                break;
            case GesturePhase.Ended:
            case GesturePhase.Cancelled:
                SnapRotation();
                break;
        }
    }

    public void SnapRotation()                              // public — 测试可直接调
    {
        float step = _puzzleConfig.RotationStep;
        float raw = AccumulatedAngleDeg;
        float snapped = Mathf.Round(raw / step) * step;
        snapped = (snapped % 360f + 360f) % 360f;

        // 已在格点 → 同步 OnComplete 路径（保证一致下游）
        float currentNorm = (transform.eulerAngles.z % 360f + 360f) % 360f;
        if (Mathf.Abs(Mathf.DeltaAngle(currentNorm, snapped)) < RotationSnapEpsilonDeg)
        {
            AccumulatedAngleDeg = snapped;
            transform.eulerAngles = new Vector3(0f, 0f, snapped);
            GameEvent.Get<IInteractionEvent>()
                .OnObjectTransformChanged(Fsm?.ObjectId ?? _objectId, transform.position, transform.rotation);
            return;
        }

        transform.DORotate(new Vector3(0f, 0f, snapped), _puzzleConfig.SnapSpeed)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                AccumulatedAngleDeg = snapped;
                if (Fsm == null) return;   // OnDisable 已清防御
                GameEvent.Get<IInteractionEvent>()
                    .OnObjectTransformChanged(Fsm.ObjectId, transform.position, transform.rotation);
            });
    }
}
```

```csharp
// InteractionCoordinator.cs（S2-11 增 OnRotate listener）
public sealed class InteractionCoordinator : MonoBehaviour
{
    public void Initialize()
    {
        // ... existing OnTap / OnDrag listeners
        GameEvent.AddEventListener<GestureData>(IGestureEvent_Event.OnRotate, OnRotate);
    }

    private void OnRotate(GestureData data)
    {
        if (CurrentSelectedObject == null) return;
        CurrentSelectedObject.HandleRotate(data);
    }
}
```

```csharp
// PuzzleConfig.cs（S2-11 加 RotationStep，向下兼容默认 15f）
public sealed class PuzzleConfig
{
    public readonly float RotationStep;   // 新增

    public PuzzleConfig(int id, InteractionBounds interactionBounds,
        float gridSize = 1.0f, float snapSpeed = 0.2f, float rotationStep = 15f)
    { /* ... */ }
}
```

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: Drag 位置机制（与 rotation 解耦，但同时启用：drag 期间也允许双指 rotate）
- Story 004: 位置 grid snap（同 snap-on-release 模式但作用于 XY）
- Story 005: 旋转视觉反馈（MVP 不区分 — rotation 视觉就是 mesh 自旋）

---

## QA Test Cases

*EditMode 单元测试 — 用 mock InteractableObject + 时间步进 + 直接调 HandleRotate：*

- **AC-1**: 旋转自由跟手指
  - Given: fsm.state == Selected；当前 rotation Z = 30°
  - When: HandleRotate({Phase=Updated, AngleDelta=20°/Rad2Deg = 0.349rad})
  - Then: rotation Z ≈ 50°（±0.1°）；无 snap
  - Edge: 负 delta（顺时针）；越过 360°/0° 不出现跳变

- **AC-2**: Ended phase snap 到最近 rotationStep
  - Given: object Z = 47°；rotationStep = 15°
  - When: HandleRotate({Phase=Ended})
  - Then: 启动 DOTween；目标 = 45°
  - Edge: Z=52.5° → 60°；Z=7.4° → 0°

- **AC-3**: snap 用 EaseOutQuad over snapSpeed
  - Given: snapSpeed = 0.3s from TbPuzzle
  - When: snap 启动
  - Then: tween duration == 0.3s；ease == EaseOutQuad
  - Edge: 当前角度已是 step 倍数 → 仍可走 0 时长 tween 或直接 OnComplete 路径（实施任选其一）

- **AC-4**: OnObjectTransformChanged 在 OnComplete 后派发
  - Given: snap 自然完成 to 90°
  - When: tween OnComplete
  - Then: listener 收到 `OnObjectTransformChanged(objectId, position 不变, rotation = Quaternion.Euler(0,0,90))` 一次
  - Edge: 派发**仅**在 OnComplete；handle Updated phase 时不派发

- **AC-5**: Cancelled phase 等价 Ended
  - Given: object Z = 47°
  - When: HandleRotate({Phase=Cancelled})
  - Then: snap 启动到 45°；OnComplete 后派 OnObjectTransformChanged
  - Pass: 手势异常中断后角度仍归位到合法格点

- **AC-6**: Locked 状态忽略
  - Given: fsm.state == Locked
  - When: HandleRotate({Phase=Updated, AngleDelta=任意})
  - Then: rotation 不变；不启动 snap；listener 0 派发
  - Edge: 旋转手势进行中 fsm 转 Locked — 当前 _accumulatedAngleDeg 保留但不 snap，不派 OnObjectTransformChanged

- **AC-7**: 协议合规 grep
  - When: `rg "EventId\.Evt_(RotateGesture|ObjectTransformChanged)" Assets/GameScripts/HotFix/`
  - Then: 0 命中

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/Tests/EditMode/ObjectInteraction/RotationMechanicsTests.cs` — 16 NUnit tests covering AC-1..AC-7
- `production/qa/grep-no-evt-objectinteraction-rotation-2026-04-30.md` — 0 命中 EventId.Evt_*

**Status**: [x] 16 NUnit tests ✅ (AC1×4, AC2×4, AC3×1, AC4×2, AC5×1, AC6×2, AC7×2)

**EditMode 不覆盖（推 PlayMode/集成）**：
- DOTween 精确 duration（0.2s tween 时长） — 与 S2-09 EaseOutBack / S2-10 EaseOutQuad 同 ADVISORY 处理
- Ease 类型曲线（OutQuad 在 0~1 时间区间的实际曲线形态）
- 多指 + Rotate 同时手势（双指捏合旋转）— 留 Sprint 3 多指手势集成 story

---

## Dependencies

- Depends on: Story 001（fsm + Selected/Dragging state — must be DONE）
- Pre-condition: ADR-013 = **Accepted**（✅ 2026-04-29）；`IInteractionEvent.cs` 已存在（✅ 2026-04-29）；`IGestureEvent` 已 active（✅ Sprint 1）
- Unlocks: Story 005（旋转视觉反馈，MVP 暂不区分），Story 007（multi-object scene 中 rotation 与 drag 同时验证）
