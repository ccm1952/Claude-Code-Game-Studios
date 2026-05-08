// 该文件由Cursor 自动生成

# Story 001: Object Interaction State Machine

> **Epic**: Object Interaction
> **Status**: **Complete** (2026-04-29)
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-29 (X1 全量迁移：ADR-006 Evt_* → ADR-027 IInteractionEvent；6 states → 5 states + Rotating 子模式；MonoBehaviour 拆分为纯 C# FSM + 绑定层)
> **Implementation Date**: 2026-04-29 (17 NUnit 全绿 + grep 协议合规 0 命中 + ADR-028 §3 例外名单锚定合规)

## Context

**GDD**: `design/gdd/object-interaction.md`
**Requirement**: `TR-objint-012`
*(Object state machine — 5 states + Rotating sub-mode in Selected)*

**ADR Governing Implementation**: ADR-013: Object Interaction State Machine（**Accepted**, 2026-04-29）+ ADR-027: GameEvent Interface Protocol
**ADR Decision Summary**: 每个可交互物体由一个 **纯 C# `InteractableObjectFsm`**（5 states：Idle / Selected / Dragging / Snapping / Locked）+ 一个薄 `InteractableObject` MonoBehaviour 绑定层组成。FSM 转换由 `IGestureEvent` / `IInteractionEvent` 监听器驱动，**不**轮询。Rotating 是 Selected 状态下的子模式（rotation 在 Selected 内累加，不引发 transition）。`Locked` 状态由 `IInteractionEvent.OnInteractionLockChanged(true)` 进入（Story 006）。

**Engine**: Unity 2022.3.62f2 LTS + TEngine 6.0.0 | **Risk**: LOW
**Engine Notes**: `InteractableObjectFsm` 是**纯 C# 类**（不继承 MonoBehaviour 也不依赖 `GameModule.Fsm`），与 Scene/SingleFinger/DualFinger FSM 同模式（参 S2-04/S2-05）。`InteractableObject` MonoBehaviour 在 `OnEnable` 中实例化 fsm + 注册 listener，在 `OnDisable` 中取消注册 + 释放 fsm。注册 listener 集中调用 `GameEvent.AddEventListener<IInteractionEvent>(...)` / `<IGestureEvent>(...)`。

**Control Manifest Rules (this layer)**:
- Required: `5-state per-object FSM: Idle / Selected / Dragging / Snapping / Locked` (ADR-013)
- Required: `Rotating 是 Selected 子模式 — 不作为独立 state` (ADR-013)
- Required: `FSM 实施为纯 C# 类（不继承 MonoBehaviour，不依赖 GameModule.Fsm）` (ADR-013, ADR-009 模式)
- Required: `所有 epic 间通信使用 IInteractionEvent 接口（ADR-027）` (ADR-027)
- Required: `Single selection: only one object selected at a time (MVP) — 单选语义在 Story 007 InteractionCoordinator 实施` (ADR-013)
- Required: `No physics simulation: objects follow finger directly — no rigidbody, no inertia` (ADR-013)
- Required: `Register all listeners in OnEnable; remove all in OnDisable` (ADR-027 §3 继承 ADR-006)
- Forbidden: `禁止使用 EventId.Evt_PuzzleLockAll / Evt_ObjectTransformChanged / Evt_ObjectSelected 等 ADR-006 常量（已废弃）`
- Forbidden: `禁止新增 ObjectTransformChangedPayload 等 payload struct — 参数直接放在接口方法签名里`
- Forbidden: `Never call Input.GetTouch() directly outside InputService` (ADR-010)

---

## Acceptance Criteria

*From GDD `design/gdd/object-interaction.md` + ADR-013 §"State Transition Rules"，scoped to this story:*

- [x] `InteractableObjectState` enum 定义 **5 个值**：`Idle`, `Selected`, `Dragging`, `Snapping`, `Locked`（**没有** `Unlocked` 与 `Rotating`）
- [x] `InteractableObjectFsm` 是纯 C# 类（不继承 `MonoBehaviour`，不调用 `GameModule.Fsm`），构造函数接收 `int objectId`；`CurrentState` 默认为 `Idle`
- [x] `InteractableObjectFsm` 暴露触发方法：`OnTapHit() / OnDeselect() / OnDragBegan() / OnDragEnded() / OnSnapCompleted() / OnLockChanged(bool)`
- [x] `InteractableObjectFsm` 暴露 C# event `event Action<InteractableObjectState, InteractableObjectState> StateChanged`（供 Story 005 feedback 订阅，不走 GameEvent）
- [x] 转换规则 1：`Idle + OnTapHit() → Selected`；副作用：派发 `IInteractionEvent.OnObjectSelected(objectId)`
- [x] 转换规则 2：`Selected + OnDeselect() → Idle`；副作用：派发 `IInteractionEvent.OnObjectDeselected(objectId)`
- [x] 转换规则 3：`Selected + OnDragBegan() → Dragging`；无对外 sender 副作用
- [x] 转换规则 4：`Dragging + OnDragEnded() → Snapping`；无对外 sender 副作用（Snap 动画由 Story 004 启动）
- [x] 转换规则 5：`Snapping + OnSnapCompleted() → Idle`；副作用由 Story 004 派发 `OnObjectTransformChanged`（本 story 仅保证 transition 正确）
- [x] 转换规则 6：`Snapping + OnTapHit() → Selected`；副作用：派发 `OnObjectSelected(objectId)`（中断 snap 由 Story 004 调用 `transform.DOKill()`）
- [x] 转换规则 7：`Idle / Selected / Dragging / Snapping + OnLockChanged(true) → Locked`；如曾 `Selected` 派发 `OnObjectDeselected(objectId)` 一次；**不**派发 `OnObjectTransformChanged`（落点未定）
- [x] 转换规则 8：`Locked + OnLockChanged(false) → Idle`；无对外 sender 副作用
- [x] **非法转换**（如 `Idle + OnDragBegan` / `Locked + OnTapHit`）：被静默丢弃 + `Log.Warning`（不抛异常，不进入未知状态）
- [x] `InteractableObject` MonoBehaviour 持有 `Fsm`；`OnEnable` 中 `new InteractableObjectFsm(objectId)` + 注册 `IInteractionEvent` listener（仅 `OnInteractionLockChanged`）；`OnDisable` 中取消注册并 `Fsm = null`
- [x] `InteractableObject.CurrentState` / `ObjectId` 属性透传到 fsm
- [x] **协议合规**：本 story 实装代码中 0 处使用 `EventId.Evt_*` 常量；所有 sender 调用形如 `GameEvent.Get<IInteractionEvent>().OnXxx(...)`

---

## Implementation Notes

*Derived from ADR-013 §"Architecture" + §"State Transition Rules"：*

```csharp
public enum InteractableObjectState { Idle, Selected, Dragging, Snapping, Locked }

public sealed class InteractableObjectFsm
{
    public int ObjectId { get; }
    public InteractableObjectState CurrentState { get; private set; } = InteractableObjectState.Idle;
    public event Action<InteractableObjectState, InteractableObjectState> StateChanged;

    public InteractableObjectFsm(int objectId) { ObjectId = objectId; }

    public void OnTapHit()
    {
        if (CurrentState == InteractableObjectState.Idle ||
            CurrentState == InteractableObjectState.Snapping)
        {
            TransitionTo(InteractableObjectState.Selected);
            GameEvent.Get<IInteractionEvent>().OnObjectSelected(ObjectId);
        }
        else { Log.Warning($"[Fsm#{ObjectId}] OnTapHit ignored in {CurrentState}"); }
    }

    public void OnDeselect()
    {
        if (CurrentState == InteractableObjectState.Selected)
        {
            TransitionTo(InteractableObjectState.Idle);
            GameEvent.Get<IInteractionEvent>().OnObjectDeselected(ObjectId);
        }
    }

    public void OnDragBegan() { /* Selected → Dragging */ }
    public void OnDragEnded() { /* Dragging → Snapping */ }
    public void OnSnapCompleted() { /* Snapping → Idle，sender 副作用由 Story 004 接管 */ }

    public void OnLockChanged(bool isLocked)
    {
        if (isLocked && CurrentState != InteractableObjectState.Locked)
        {
            bool wasSelected = CurrentState == InteractableObjectState.Selected;
            TransitionTo(InteractableObjectState.Locked);
            if (wasSelected)
                GameEvent.Get<IInteractionEvent>().OnObjectDeselected(ObjectId);
        }
        else if (!isLocked && CurrentState == InteractableObjectState.Locked)
        {
            TransitionTo(InteractableObjectState.Idle);
        }
    }

    private void TransitionTo(InteractableObjectState next)
    {
        var prev = CurrentState;
        CurrentState = next;
        StateChanged?.Invoke(prev, next);
        Log.Info($"[InteractableObjectFsm#{ObjectId}] {prev} → {next}");
    }
}
```

```csharp
public sealed class InteractableObject : MonoBehaviour
{
    [SerializeField] private int _objectId;
    public InteractableObjectFsm Fsm { get; private set; }
    public int ObjectId => Fsm?.ObjectId ?? _objectId;
    public InteractableObjectState CurrentState => Fsm?.CurrentState ?? InteractableObjectState.Idle;

    private void OnEnable()
    {
        Fsm = new InteractableObjectFsm(_objectId);
        GameEvent.AddEventListener<IInteractionEvent>(OnInteractionEvent_Bind);
    }

    private void OnDisable()
    {
        GameEvent.RemoveEventListener<IInteractionEvent>(OnInteractionEvent_Bind);
        Fsm = null;
    }

    // 仅监听 OnInteractionLockChanged；其他 sender 由 InteractionCoordinator (Story 007) 调用 fsm 触发方法
    private void OnInteractionEvent_Bind(IInteractionEvent _)
    {
        // listener 实际签名见 Source Generator 生成代码（_Gen / _Event）
        // 占位描述：仅响应 OnInteractionLockChanged → fsm.OnLockChanged(isLocked)
    }
}
```

**InteractionCoordinator 单选语义**：见 Story 007 — 本 story 仅实施单 object FSM。多对象单选由 Coordinator 监听 IGestureEvent.OnTap，对所有 object 做 raycast，对命中者调 `fsm.OnTapHit()`，对当前选中者（如不同）调 `fsm.OnDeselect()`。

**关于 Source Generator 注册形态**：参考 SceneManager.cs（S2-05 实装）— `[EventInterface]` 属性 + `GameEvent.AddEventListener<IInterface>(handler)` + handler 签名由生成代码 `_Gen` 提供。Listener 接收方法名与 interface 方法同名。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: Drag 1:1 跟踪 + 边界 clamp（在 fsm.CurrentState == Dragging 时由 InteractableObject Update 实施）
- Story 003: Rotation 子模式（Selected 内累加 + gesture-end snap）
- Story 004: Grid Snap 动画（Snapping 状态进入时 DOTween；OnComplete → fsm.OnSnapCompleted() + 派发 `OnObjectTransformChanged`）
- Story 005: Visual feedback（订阅 fsm.StateChanged C# event；不走 GameEvent）
- Story 006: InteractionLockManager（HashSet token + 派发 `OnInteractionLockChanged`）
- Story 007: InteractionCoordinator（Raycast + 单选 + 200ms debounce）

---

## QA Test Cases

*EditMode 单元测试，纯 C# `InteractableObjectFsm`，0 GameObject 依赖：*

- **AC-1**: FSM 初始状态为 Idle
  - Given: `var fsm = new InteractableObjectFsm(42);`
  - When: 查询 `fsm.CurrentState`
  - Then: 等于 `InteractableObjectState.Idle`
  - Edge cases: `fsm.ObjectId == 42`

- **AC-2**: Idle + OnTapHit → Selected + 派发 OnObjectSelected
  - Given: fsm 在 Idle
  - When: `fsm.OnTapHit()`
  - Then: state == Selected；GameEvent listener 收到 `OnObjectSelected(42)` 一次

- **AC-3**: Selected + OnDeselect → Idle + 派发 OnObjectDeselected
  - Given: fsm 在 Selected
  - When: `fsm.OnDeselect()`
  - Then: state == Idle；GameEvent listener 收到 `OnObjectDeselected(42)` 一次

- **AC-4**: 完整 Drag → Snap → Idle 链路
  - Given: fsm 在 Selected
  - When: `OnDragBegan()` → `OnDragEnded()` → `OnSnapCompleted()`
  - Then: 依次进入 Dragging / Snapping / Idle；本 story 不验证 `OnObjectTransformChanged`（Story 004 验证）

- **AC-5**: Lock 转换 — 多 from-state 全覆盖
  - For each fromState in {Idle, Selected, Dragging, Snapping}:
    - Given: fsm 在 fromState
    - When: `OnLockChanged(true)`
    - Then: state == Locked
    - 如 fromState == Selected：listener 收到 `OnObjectDeselected(42)` 一次（其他 fromState 不派发）
  - Then: `OnLockChanged(false)` → state == Idle

- **AC-6**: 非法转换静默 + Log.Warning
  - Given: fsm 在 Idle
  - When: `OnDragBegan()`
  - Then: state 仍为 Idle；GameEvent listener 未收到任何派发；Log.Warning 出现一次（用 LogAssert.Expect 验证）
  - Edge: Locked + OnTapHit / OnDragBegan / OnDeselect 均静默 + warning

- **AC-7**: Snapping + OnTapHit → Selected（中断 snap 路径，rule 6）
  - Given: fsm 在 Snapping
  - When: `OnTapHit()`
  - Then: state == Selected；listener 收到 `OnObjectSelected(42)`

- **AC-8**: StateChanged C# event 派发顺序正确
  - Given: 订阅 `fsm.StateChanged += (prev, next) => log.Add((prev, next))`
  - When: Idle → Selected → Dragging → Snapping → Idle
  - Then: log 含 4 条 transition；prev/next 顺序正确

- **AC-9**: 协议合规 grep 验证（手工或 CI）
  - When: `rg "EventId\.Evt_(PuzzleLockAll|PuzzleUnlock|ObjectTransformChanged|ObjectSelected|ObjectDeselected)" Assets/GameScripts/HotFix/`
  - Then: 0 命中（除注释 / TODO）

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/Tests/EditMode/ObjectInteraction/InteractableObjectFsmTests.cs` — **17 NUnit tests** ≥ 9 AC requirement (AC-1..AC-9 全部覆盖 + 多实例 / 幂等 / IInteractionEvent 反射健全性补充测试)，2026-04-29 用户手动 Run All EditMode 全绿
- `production/qa/grep-no-evt-objectinteraction-2026-04-29.md` — grep 证据：0 处 `EventId.Evt_(PuzzleLockAll|PuzzleUnlock|ObjectTransformChanged|ObjectSelected|ObjectDeselected)` 残留 + 验证 `GameEvent.Get<IInteractionEvent>().OnXxx()` 使用模式

**Status**: [x] Complete (2026-04-29) — 17 NUnit 全绿 + grep 证据 + ADR-028 §3 例外名单合规锚定

---

## Dependencies

- Depends on: 无（first story；定义 FSM 骨架 + 协议落点）
- Pre-condition: ADR-013 = **Accepted**（✅ 2026-04-29）；`IInteractionEvent.cs` 已存在并编译过（✅ 2026-04-29）
- Unlocks: Story 002 (DraggingState 内的 1:1 跟踪 + 边界 clamp)，Story 003 (rotation 子模式)，Story 004 (SnappingState DOTween)，Story 005 (visual feedback 订阅 StateChanged)，Story 006 (lock manager 派发 OnInteractionLockChanged)，Story 007 (multi-object coordination)

---

## Implementation Summary (2026-04-29)

### 产出文件

| 文件 | 行数 | 角色 |
|---|---|---|
| `Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/InteractableObjectFsm.cs` | ~150 | 纯 C# FSM：5 enum + 6 trigger 方法 + `event StateChanged` + 8 transition rules + 非法转换 `Log.Warning` |
| `Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/InteractableObject.cs` | ~70 | MonoBehaviour 绑定层：`OnEnable` 创建 fsm + 注册 `IInteractionEvent.OnInteractionLockChanged` listener；`OnDisable` 取消注册 + `Fsm = null`（与 SceneManager / SingleFinger 模式一致） |
| `Assets/Tests/EditMode/ObjectInteraction/InteractableObjectFsmTests.cs` | ~400 | 17 NUnit `[TestFixture]`：AC-1..AC-9 全覆盖 + 多实例隔离 + 幂等 LockChanged + IInteractionEvent_Gen 反射健全性 |
| `Assets/Tests/EditMode/ObjectInteraction/Tests.EditMode.ObjectInteraction.asmdef` | — | NUnit + TEngine.Runtime + GameLogic 引用 |

### 实施关键决策

- **FSM 风格**：trigger reducer（外部调 6 个 trigger 方法，FSM 内部 switch 路由），与 `SceneManager` / `SingleFingerFSM` 同模式；**不**走 TEngine FsmModule（`ADR-028 §3` 已锚定为合规例外）
- **C# `StateChanged` event**：1:1 本地反馈，给 Story 005 visual feedback 订阅；**不**通过 GameEvent 全局派发（`ADR-011` widget 原则）
- **非法转换**：`Log.Warning` 静默 + `LogAssert.Expect` 在测试中显式期望（避免误报）
- **多实例隔离**：每个 `InteractableObject` 持有自己的 fsm 实例（构造时传 `_objectId`），fsm 之间无共享状态；测试中 2 实例并行验证

### 测试结果

- **EditMode Run All**: 用户 2026-04-29 手动运行 → "全绿"（17/17 通过；其他 sprint 测试 0 回归）
- **协议合规 grep**: `production/qa/grep-no-evt-objectinteraction-2026-04-29.md` 0 命中 ADR-006 残留
- **Lint**: `ReadLints` 0 错误

### 治理关联

`X2 治理 session`（2026-04-29 同日）确认本实施合规：
- `ADR-013` Alt 2 v3 修订：列出真实 6 项 FsmModule trade-off（替换"GameObject 依赖"事实错误）
- `ADR-028 §3`：`InteractableObjectFsm` 列入"事件驱动 reducer FSM 例外名单"，与 `SingleFingerFSM` / `SceneManager` 同档
