// 该文件由Cursor 自动生成

# ADR-013: Object Interaction State Machine & Snap Mechanics

## Status

Accepted

## Date

2026-04-22

## Last Verified

2026-04-29

## Revision History

| Date | Change | Notes |
|------|--------|-------|
| 2026-04-22 | Initial draft (Proposed) | 6-state FSM, Evt_* / Payload struct 协议 |
| 2026-04-29 | **Migrated to ADR-027** + Status → **Accepted** | 删除 Unlocked state（5 states + Rotating 作为 Selected 子模式）；事件协议迁移为单一 `IInteractionEvent` 接口；新增 §"Event Protocol (ADR-027 Compliance)" |
| 2026-04-29 (v3) | **修正 Alt 2 事实错误** | 此前 Alt 2 写"FsmModule 强依赖 GameObject"为**错误**（`Fsm<T>.Create` 仅约束 `where T : class`，不强制 MonoBehaviour）。改正为真实约束：`ChangeState` 是 `internal` / 状态切换需在 `FsmState<T>` 内 / `MemoryPool` 共享单例 / `IFsmModule.Update` 经 `RootModule` 驱动；并指向 `ADR-028` 决定的"事件驱动 FSM 自建例外名单" |

## Decision Makers

Technical Director, Lead Programmer, Game Designer

## Summary

Object Interaction is the Core Layer system through which players physically manipulate puzzle objects. We adopt a **5-state per-object FSM** (`Idle → Selected → Dragging → Snapping → Locked`) powered by a pure-C# state machine bound to a thin MonoBehaviour wrapper, with **configurable grid snap** via DOTween animations, **fat finger compensation** scaled by device DPI, and **haptic feedback** on snap events. All inter-system communication uses the **single `IInteractionEvent` interface** (ADR-027) — no `GameEvent.Send(EventId.Evt_*)` constants and no payload structs. All configuration (gridSize, rotationStep, snapSpeed, bounds) is Luban-driven.

> **Rotating 不是独立 state**：rotation 在 `Selected` 状态下作为子模式实施（gesture 来时叠加，gesture-end 时 snap），与 GDD §"6-state ... + Rotating" 字面描述对齐为"5 states + Rotating sub-mode"。

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.62f2 (LTS) |
| **Domain** | Core / Input Processing / Animation |
| **Knowledge Risk** | LOW (DOTween, plain C# FSM) |
| **References Consulted** | `object-interaction.md`, `architecture.md` §4.2/§6.2, `input-system.md`, ADR-027 |
| **Post-Cutoff APIs Used** | TEngine `GameEvent` Source Generator (`[EventInterface]`) |
| **Verification Required** | DOTween tween pooling under frequent snap operations (covered by Story 004 perf test) |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-010 (Input Abstraction — provides `IGestureEvent.OnTap/OnDrag/OnRotate`); ADR-027 (GameEvent Interface Protocol — defines `IInteractionEvent` 形态) |
| **Enables** | ADR-012 (Shadow Match — consumes `IInteractionEvent.OnObjectTransformChanged`) |
| **Blocks** | All object manipulation gameplay; Shadow Puzzle cannot receive transform data without this system |
| **Ordering Note** | ADR-010 + ADR-027 must be Accepted (✅ both Accepted as of 2026-04-29); ADR-013 itself **Accepted** as of 2026-04-29, unblocking Object Interaction epic Sprint 2 implementation |

## Context

### Problem Statement

Players interact with puzzle objects via touch gestures (drag, rotate, pinch). The system must translate gesture input into weighted, grid-snapped object movement that feels like "gently rearranging someone's belongings" (design pillar: 日常即重量). Key challenges:

1. **State complexity**: Objects can be idle, selected, dragging, snapping, or locked by external systems — transitions must be deterministic and cover all edge cases
2. **Snap feel**: Grid snap must feel like "magnetic pull-in" (EaseOutQuad), not hard teleportation
3. **Mobile ergonomics**: Fat finger compensation must scale with device DPI; touch targets must meet Apple HIG 44pt minimum
4. **External locking**: Narrative sequences and PerfectMatch must lock all objects mid-operation without deadlocking the FSM
5. **Performance**: 10 objects on screen, ≤ 1ms total Update cost per frame (TR-objint-020)
6. **Test isolation**: FSM must be unit-testable in EditMode without instantiating GameObjects (resolves S2-05/S2-06 lessons learned)

### Constraints

- **Single selection**: Only one object selected at a time (MVP)
- **No physics simulation**: Objects follow finger directly — no rigidbody, no inertia, no collision between objects
- **Grid snap only on release**: During drag, object follows finger freely; snap triggers on finger lift
- **Haptic budget**: Haptic feedback must be "extremely restrained" — UIImpactFeedbackGenerator.light for snap, medium for putdown
- **Layer isolation**: This system (Core Layer) consumes Input events (Foundation Layer) and produces transform events consumed by Shadow Puzzle (Feature Layer)
- **Event protocol**: 必须使用 `IInteractionEvent` 接口（ADR-027）；禁止再添加 `Evt_*` 常量或 `XxxPayload` struct（ADR-006 协议在本 epic 全量作废）

### Requirements

- TR-objint-012: 5-state object FSM (was "6-state"; 文档现修正为 5 states + Rotating 子模式)
- TR-objint-013: Light source FSM（非 MVP，接口位 `OnLightPositionChanged` 已冻结）
- TR-objint-014/015/016: Events via `IInteractionEvent`（ObjectTransformChanged, LightPositionChanged, PuzzleLockAll/Unlock）
- TR-objint-019: 10 objects ≥ 55fps on iPhone 13 Mini
- TR-objint-017: Drag response ≤ 16ms (1 frame)
- TR-objint-020: Total system update ≤ 1ms/frame
- TR-objint-022: Haptic feedback on snap（已 deferred 至 ADR-025 / P2，非本 ADR 范围）

## Decision

**Implement a per-object 5-state FSM as a pure-C# class (`InteractableObjectFsm`) bound to a thin MonoBehaviour wrapper (`InteractableObject`). All inter-system communication uses the single `IInteractionEvent` interface per ADR-027. DOTween-driven snap animations, DPI-scaled fat finger compensation, and Luban-configurable grid/rotation parameters complete the package.**

### Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                    Object Interaction System                       │
│                                                                    │
│  ┌────────────────────────────────────┐                            │
│  │ InteractableObjectFsm (pure C#)    │  ← 单元可测，不依赖 Unity GameObject  │
│  │                                    │                            │
│  │   Idle ──Tap────→ Selected         │                            │
│  │    ↑                ↓ Drag.Began   │                            │
│  │    │              Dragging         │                            │
│  │    │                ↓ Drag.Ended   │                            │
│  │    └── Idle ←── Snapping           │                            │
│  │                                    │                            │
│  │   Any ──Lock=true──→ Locked        │                            │
│  │   Locked ──Lock=false──→ Idle      │                            │
│  └──────────┬─────────────────────────┘                            │
│             │ bound 1:1                                            │
│  ┌──────────┴─────────────────────────┐                            │
│  │ InteractableObject : MonoBehaviour │  ← Update tick / collider / renderer 引用 │
│  │ (绑定层；持 fsm，OnEnable Init)    │                            │
│  └──────────┬─────────────────────────┘                            │
│             │ orchestration                                        │
│  ┌──────────┴─────────────────────────┐                            │
│  │ InteractionCoordinator (Mono)      │  ← 单选语义 + Raycast + 200ms debounce │
│  │ + InteractionLockManager (POCO)    │  ← HashSet<string> token 锁集合       │
│  └────────────────────────────────────┘                            │
│             ▲                                ▼                     │
│  ┌──────────┴─────────────────────────┐  ┌────────────────────────┐│
│  │ Listener: IGestureEvent.OnTap      │  │ Sender: IInteractionEvent ││
│  │           IGestureEvent.OnDrag     │  │   .OnObjectSelected      ││
│  │           IGestureEvent.OnRotate   │  │   .OnObjectDeselected    ││
│  │           ISceneEvent.OnSceneUnloadBegin │  │   .OnObjectTransformChanged ││
│  │           IInteractionEvent.OnRequestPuzzleLockAll │  │   .OnInteractionLockChanged ││
│  │           IInteractionEvent.OnRequestPuzzleUnlock  │  │                          ││
│  └────────────────────────────────────┘  └────────────────────────┘│
└──────────────────────────────────────────────────────────────────┘
```

### Key Interfaces

```csharp
public enum InteractableObjectState { Idle, Selected, Dragging, Snapping, Locked }
// Light state machine（非 MVP，签名预留）
public enum LightState { Fixed, TrackIdle, TrackDragging, TrackSnapping }

// 纯 C# FSM —— 可在 EditMode 单元测试中 new 出来直接驱动
public sealed class InteractableObjectFsm
{
    public int ObjectId { get; }
    public InteractableObjectState CurrentState { get; private set; }

    // 触发方法（由 InteractableObject MonoBehaviour 在 IGestureEvent 回调里调用）
    public void OnTapHit();             // Idle → Selected
    public void OnDeselect();           // Selected → Idle
    public void OnDragBegan();          // Selected → Dragging
    public void OnDragEnded();          // Dragging → Snapping
    public void OnSnapCompleted();      // Snapping → Idle（DOTween OnComplete 回调）
    public void OnLockChanged(bool isLocked);  // Any↔Locked

    // C# event 暴露给 InteractableObjectFeedback（Story 005，无需 GameEvent）
    public event System.Action<InteractableObjectState, InteractableObjectState> StateChanged;
}

// 绑定层 MonoBehaviour
public sealed class InteractableObject : MonoBehaviour
{
    public InteractableObjectFsm Fsm { get; private set; }
    public int ObjectId => Fsm.ObjectId;
    public InteractableObjectState CurrentState => Fsm.CurrentState;
    // OnEnable: new InteractableObjectFsm(...); 注册 IGestureEvent / IInteractionEvent listener
    // OnDisable: 取消注册；Fsm = null
    // Update: 仅在 Dragging / Snapping 时调用 fsm.Tick(deltaTime) —— Idle/Selected/Locked 零开销
}

public interface IObjectInteraction
{
    InteractableObject GetSelectedObject();
    bool IsAnyObjectDragging();
}
```

### Event Protocol (ADR-027 Compliance)

本 epic **唯一**事件接口为 `IInteractionEvent`（位于 `Assets/GameScripts/HotFix/GameLogic/IEvent/IInteractionEvent.cs`）。8 方法分四组：

| # | 方法签名 | 方向 | Sender | Listener | 实装 Story |
|---|---------|-----|--------|---------|----------|
| 1 | `OnObjectSelected(int objectId)` | 通知 | InteractableObjectFsm / InteractionCoordinator | UI / Audio / Analytics | S2-08 / S2-14 |
| 2 | `OnObjectDeselected(int objectId)` | 通知 | InteractableObjectFsm / InteractionCoordinator | UI / Audio | S2-08 / S2-14 |
| 3 | `OnObjectTransformChanged(int, Vector3, Quaternion)` | 通知 | InteractableObjectFsm（Snap/Rotation 完成时） | Shadow Puzzle / Save System / Analytics | S2-10 / S2-11 |
| 4 | `OnRequestPuzzleLockAll(string lockerId)` | 命令 | Narrative / Shadow Puzzle / Tutorial | InteractionLockManager（唯一） | S2-13 |
| 5 | `OnRequestPuzzleUnlock(string lockerId)` | 命令 | 同上 | InteractionLockManager（唯一） | S2-13 |
| 6 | `OnInteractionLockChanged(bool isLocked)` | 通知 | InteractionLockManager（唯一） | InteractableObjectFsm / UI | S2-13（sender） / S2-08（listener） |
| 7 | `OnRequestSnapToTarget(int, Vector3, Quaternion, float)` | 命令 | [Reserved — Narrative epic / S3+] | InteractableObjectFsm | — 接口冻结 |
| 8 | `OnLightPositionChanged(int lightId, float trackT)` | 通知 | [Reserved — LightSource 子系统 / S3+] | Shadow Puzzle / Audio | — 接口冻结 |

**禁止事项**（Control Manifest 守则）：
- ❌ 禁止再使用 `EventId.Evt_PuzzleLockAll` / `Evt_ObjectTransformChanged` / `Evt_ObjectSelected` 等 ADR-006 常量（已废弃）
- ❌ 禁止新增 `ObjectTransformChangedPayload` 等 payload struct（参数直接放在接口方法签名里）
- ❌ 禁止任何 `GameEvent.Send(EventId.Evt_*, ...)` 形态调用——必须 `GameEvent.Get<IInteractionEvent>().OnXxx(...)`
- ✅ Cascade depth ≤ 3（继承 ADR-027 §2 / ADR-006 §5 re-entrancy 约束）

### Grid Snap Mechanics

```
snappedPos.x = round(rawPos.x / gridSize) * gridSize
snappedPos.y = round(rawPos.y / gridSize) * gridSize  // 2D 平面 (XY)；Z 固定
snappedAngle = round(rawAngle / rotationStep) * rotationStep

snapDuration = clamp(distance / snapSpeed, minSnapDuration, maxSnapDuration)
// Animated via DOTween: EaseOutQuad for position, EaseOutQuad for rotation
```

| Parameter | Default | Source | Range |
|-----------|---------|--------|-------|
| gridSize | 0.25 units | TbPuzzle per chapter | 0.1-0.5 |
| rotationStep | 15° | TbPuzzle | 10-45° |
| snapSpeed | 3.0 u/s | config | 2.0-5.0 |
| minSnapDuration | 0.05s | config | 0.03-0.08s |
| maxSnapDuration | 0.15s | config | 0.10-0.25s |

### Fat Finger Compensation

```csharp
float expandedRadius = colliderRadius + fatFingerMargin * (Screen.dpi / referenceDPI);
// referenceDPI = 326 (iPhone 13 Mini)
// fatFingerMargin = 8dp base, scaled by touch_sensitivity setting
```

Ensures minimum touch target ≥ 44pt (Apple HIG) on all supported devices.

### Haptic Feedback Integration

| Event | iOS API | Android API | Duration | Intensity |
|-------|---------|-------------|----------|-----------|
| Grid snap complete | UIImpactFeedbackGenerator.light | VibrationEffect (amplitude 20) | 15ms | Minimal |
| Object putdown | UIImpactFeedbackGenerator.medium | VibrationEffect (amplitude 40) | 30ms | Light |
| Boundary rebound | UIImpactFeedbackGenerator.rigid | VibrationEffect (amplitude 30) | 20ms | Light |
| Light track endpoint | UINotificationFeedbackGenerator.warning | VibrationEffect (amplitude 25) | 25ms | Light |

All haptic gated by `Settings.haptic_enabled`. Low-end Android devices (no VibrationEffect API) gracefully degrade to no haptic. Haptic 实施见 ADR-025（P2）。

### State Transition Rules

每条规则给出**触发器 → 目标状态 → 副作用**：

| # | From | Trigger | To | 副作用（Side Effects） |
|---|------|---------|----|----------------------|
| 1 | `Idle` | `OnTapHit()` (raycast 命中且未被 Locked) | `Selected` | `IInteractionEvent.OnObjectSelected(objectId)` |
| 2 | `Selected` | `OnDeselect()` (Tap 空白 / Tap 别的物体) | `Idle` | `IInteractionEvent.OnObjectDeselected(objectId)` |
| 3 | `Selected` | `OnDragBegan()` (任意手指移动，零阈值) | `Dragging` | （无 sender；旋转手势在 Selected 内叠加，不引发 transition） |
| 4 | `Dragging` | `OnDragEnded()` | `Snapping` | DOTween 启动 snap 动画 |
| 5 | `Snapping` | `OnSnapCompleted()` (DOTween OnComplete) | `Idle` | `IInteractionEvent.OnObjectTransformChanged(objectId, pos, rot)` |
| 6 | `Snapping` | `OnTapHit()` (玩家中途再选) | `Selected` | DOTween Kill；`OnObjectSelected` 重派；位置定格在中间帧 |
| 7 | `Idle` / `Selected` / `Dragging` / `Snapping` | `OnLockChanged(true)` | `Locked` | DOTween Kill（如 in-flight）；如曾 Selected → 派 `OnObjectDeselected`；不派 `OnObjectTransformChanged`（落点未定） |
| 8 | `Locked` | `OnLockChanged(false)` | `Idle` | 无（玩家需重新 tap） |

> **Rotating 子模式**（不是独立 state）：在 `Selected` 中，`IGestureEvent.OnRotate` 累加 `transform.eulerAngles.z`；`OnRotate.Phase==Ended` 时 snap 到 rotationStep 倍数并派发 `OnObjectTransformChanged`，**state 仍为 Selected**。

## Alternatives Considered

### Alternative 1: Physics-Based Interaction (Rigidbody + Colliders)

- **Description**: Use Unity physics (Rigidbody2D/3D) for object movement with configurable drag, angular drag, and collider-based boundary enforcement
- **Pros**: Realistic weight feel; automatic collision handling between objects; built-in boundary enforcement via collider walls
- **Cons**: Adds unpredictable physics behavior (jitter, tunneling) counter to the "direct control" design requirement; physics step timing conflicts with touch-synchronous updates; significant performance overhead for 10+ objects; grid snap requires fighting physics engine
- **Rejection Reason**: GDD explicitly requires "no inertia, no delay, arcade-feel direct control." Physics-based interaction fundamentally conflicts with the "finger to where = object to where" requirement.

### Alternative 2: TEngine FsmModule

> **2026-04-29 修订说明**：此前版本写"强依赖 GameObject"为**事实错误** —— `Fsm<T>.Create` 仅约束 `where T : class`，不强制 `MonoBehaviour`。本节根据 `tengine-module-usage-audit-2026-04-29.md` 重写，**真实**列出 FsmModule 的取舍点。

- **Description**: 将 5 states 实施为 `FsmState<InteractableObject>` 子类，由 `GameModule.Fsm.CreateFsm<>()` 创建并驱动；状态切换在 `FsmState<T>.OnUpdate` 内调 `protected internal ChangeState(IFsm<T>)`
- **Pros**:
  - 与 `GameApp.GameFlow` 6-state Procedure-style FSM 模式一致（项目已有先例）
  - 自带 `OnEnter/OnUpdate/OnLeave/OnDestroy` 多态钩子
  - `IFsmModule` 提供统一管理（`HasFsm/GetFsm/DestroyFsm`）
  - `Fsm<T>` 实例由 `MemoryPool` 复用，理论 GC 友好
- **Cons (真实)**:
  1. **`Fsm.ChangeState` 是 `internal`**：状态切换必须从 `FsmState<T>` 子类内部触发（通过 `protected internal ChangeState(IFsm<T>, Type)` 包装）。**外部代码（如 InteractableObject MonoBehaviour）不能直接调 `OnTapHit() → ChangeState(Selected)`**，必须把 trigger 写入 `owner` 上的 flag/data，再由 `FsmState.OnUpdate` 反查并切换 —— 与本系统 trigger reducer 设计意图相反，绕路。
  2. **per-state class 模式**：5 states = 5 个 `FsmState<InteractableObject>` 子类（IdleState/SelectedState/DraggingState/SnappingState/LockedState），代码量约为当前 enum + switch trigger 实施的 **2-3 倍**，且测试需要每个 state class 单独覆盖 OnEnter/OnUpdate/OnLeave 路径。
  3. **C# `event StateChanged` 无对应**：当前 `InteractableObjectFsm.StateChanged` 提供给 `InteractableObjectFeedback` 1:1 本地订阅（`ADR-011` 视觉反馈层），FsmModule 无内置 event，需在每个 `OnEnter/OnLeave` 内手动派发 `IInteractionEvent`，等价于借用全局事件总线传递本地反馈，违背"局部信号优先"原则。
  4. **多实例区分繁琐**：FsmModule 用 `(Type, name)` 区分 fsm；同一 `InteractableObject` type 多实例必须靠 `name = $"obj_{id}"` 字符串拼接，对 10+ 对象/章节场景的 `HasFsm/GetFsm` 调用引入字符串分配。
  5. **EditMode 测试需手动驱动 `ModuleSystem.Update`**：FsmModule 的 `IUpdateModule.Update` 通常由 `RootModule.Update` 触发，EditMode 测试中需要在 `[SetUp]` 里手动 `new RootModule()` 或 `ModuleSystem.GetModule<IFsmModule>()` + 帧 tick 驱动；当前纯 C# FSM 0 ModuleSystem 依赖。
  6. **Domain reload / play mode 切换的 `MemoryPool` 重置**：`MemoryPool` 是静态全局状态，Editor 多次 enter/exit play mode 之间需要保证清理（`ModuleSystem.Shutdown` 已覆盖，但仍是治理负担）。
- **Rejection Reason**:
  事件驱动 + trigger reducer + 多对象实例 + 1:1 本地反馈，**与 FsmModule 的 procedure-style + per-state class + 全局管理设计意图属于不同取向**。重构成 FsmModule 实施代码量翻倍且语义更绕，**不构成"重复造轮子"**——是**架构选择**。`ADR-028` §3 将本类"事件驱动 FSM 自建"列为正式例外名单（含 `SceneManager` / `InteractableObjectFsm` / `SingleFingerFSM`），与 `GameApp.GameFlow` 走 FsmModule 形成清晰二分。

### Alternative 3: 6-State FSM (含独立 Unlocked / Rotating)

- **Description**: 保留原 GDD 描述的 6-state（Idle/Selected/Dragging/Snapping/Locked/Unlocked），或将 Rotating 列为独立 state
- **Pros**: 字面对齐 GDD 描述
- **Cons**: `Unlocked` 不是状态而是 transition 名（`Locked → Idle` 的过渡）；将其作为独立 state 会引入"Unlocked 与 Idle 等价但允许的输入不同"的伪冗余；Rotating 与 Selected 区别仅在"是否有 rotate 手势进行中"，FSM 复杂度无收益
- **Rejection Reason**: GDD 描述精确性低于 ADR；本 ADR 修订（2026-04-29）将 GDD 表述统一为"5 states + Rotating 子模式 (Selected 内)"

### Alternative 4: Continuous Snap (Snap During Drag, Not On Release)

- **Description**: Object continuously snaps to nearest grid point while being dragged, rather than snapping only on finger release
- **Pros**: Object always on grid — what you see is what you get; no post-release animation needed
- **Cons**: Makes drag feel "sticky" and stepped rather than smooth; GDD explicitly requires "拖拽过程中物件在格点间自由移动"; conflicts with 16ms drag response requirement if snap calculations cause micro-stalls
- **Rejection Reason**: GDD specifies that objects follow finger freely during drag, with snap only on release. Continuous snap was explicitly rejected in the GDD's game feel section.

## Consequences

### Positive

- **Deterministic FSM**: 5 states 全部转换在 §"State Transition Rules" 明确给出，无 undefined transition
- **Smooth feel**: DOTween EaseOutQuad snap creates the "magnetic pull-in" feel described in GDD
- **Mobile-optimized**: DPI-scaled fat finger compensation ensures usability across iPhone SE to iPad
- **Externally controllable**: `OnRequestPuzzleLockAll` / `OnRequestSnapToTarget` 让 Narrative / Puzzle 系统可驱动物体而不绕过 FSM
- **Test isolation**: 纯 C# `InteractableObjectFsm` 在 EditMode 测试中 0 GameObject 依赖，复用 S2-04/05/06 已验证的测试模式
- **协议一致性**: `IInteractionEvent` 与 `ISceneEvent` / `IGestureEvent` / `IShadowRTEvent` / `IChapterStateEvent` / `ISettingsEvent` 同模式（ADR-027 全 epic 推行）

### Negative

- **DOTween dependency**: Adding DOTween as an animation driver — if DOTween has issues, snap animations break. Mitigation: DOTween is mature and battle-tested
- **Single-select limitation**: MVP only supports one selected object at a time. Multi-select would require FSM redesign + IInteractionEvent 扩展 OnObjectsSelected(int[])
- **Haptic platform fragmentation**: Android haptic quality varies widely; some devices may have poor or no haptic response（推迟到 ADR-025）

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| 纯 C# FSM 与 InteractableObject MonoBehaviour 解耦不当导致 lifecycle bug | LOW | MEDIUM | 复用 S2-05 SceneManager 模式（OnEnable Init / OnDisable Dispose）；listener 注册集中 |
| DOTween tween pooling causes memory pressure under rapid snap/cancel cycles | LOW | LOW | Pre-warm tween pool；on Snapping → Selected (rule 6) 强制 `transform.DOKill()`；S2-11 perf 测试覆盖 |
| Fat finger expansion causes accidental selection of adjacent objects | MEDIUM | LOW | Z-depth priority (closer to camera wins)；tunable fatFingerMargin；playtest on smallest target device |
| Locked during Snapping leaves object at intermediate position | LOW | MEDIUM | rule 7 副作用规定：DOTween Kill；object stays at current position；如 Narrative 需要重定位则用 `OnRequestSnapToTarget`（无视 Locked） |
| `OnRequestPuzzleLockAll` 多个 sender 错误配对导致死锁 | MEDIUM | HIGH | InteractionLockManager 用 HashSet<string> token 集合（SP-006 决策）；`InteractionLockerId` 三常量限定合法 lockerId |

## Performance Implications

| Metric | Expected | Budget | Notes |
|--------|----------|--------|-------|
| CPU (per-frame Update, 10 objects) | 0.3-0.5ms | ≤ 1.0ms (TR-objint-020) | Raycast + FSM Tick (Idle/Selected/Locked 零开销) + DOTween tick |
| Memory | ~50KB (10 objects × FSM + collider references) | Negligible | No dynamic allocation during gameplay |
| Drag response | < 16ms | ≤ 16ms (TR-objint-017) | gesture 接收 → `transform.position` 更新；同帧完成 |
| Haptic latency | < 5ms from snap complete | N/A | OS-level haptic API, no Unity overhead |

## Validation Criteria

- [ ] 5 FSM states + 8 transition rules 全部在 EditMode 自动化测试中覆盖（Story 001）
- [ ] Drag response ≤ 16ms verified via Unity Profiler on iPhone 13 Mini（Story 002 / 007）
- [ ] Grid snap animation duration within 50-150ms range for all grid distances（Story 004）
- [ ] Fat finger compensation: 10 consecutive selection attempts on iPhone 13 Mini succeed ≥ 9 times for all object sizes（Story 007）
- [ ] `OnRequestPuzzleLockAll` during Dragging immediately halts object, fires `OnInteractionLockChanged(true)` 一次（Story 006）
- [ ] `OnRequestSnapToTarget` (from Narrative) correctly moves Locked object to precise position（[Reserved] — 接口冻结，验证延后到 Narrative epic）
- [ ] Haptic fires on snap (iOS Taptic Engine verified), can be disabled via settings — **deferred to ADR-025**
- [ ] All parameters (gridSize, rotationStep, snapSpeed, bounds) loaded from Luban — no hardcoded values（Story 002/003/004）
- [ ] 10 objects on screen, continuous dragging: frame time < 1ms for Object Interaction system（Story 007）
- [ ] **协议合规**：grep 扫描 HotFix 代码，确认零 `EventId.Evt_PuzzleLockAll` / `Evt_ObjectTransformChanged` / `Evt_ObjectSelected` 引用（仅允许 IInteractionEvent.OnXxx 调用）

## GDD Requirements Addressed

| GDD Document | Requirement | How This ADR Satisfies It |
|-------------|-------------|--------------------------|
| `object-interaction.md` | "6-state object FSM (Idle/Selected/Dragging/Snapping/Locked + Rotating)" | **修正为 5 states + Rotating 子模式**：纯 C# `InteractableObjectFsm` 5 states；Rotating 在 Selected 内通过 IGestureEvent.OnRotate 累加 |
| `object-interaction.md` | Grid snap with configurable gridSize, rotationStep | Luban TbPuzzle config per chapter；round-to-nearest formula |
| `object-interaction.md` | Fat finger compensation (8dp base, DPI-scaled) | `expandedRadius = colliderRadius + fatFingerMargin * (dpi/326)` |
| `object-interaction.md` | Haptic feedback on snap (UIImpactFeedbackGenerator.light) | Platform-specific haptic integration gated by `haptic_enabled`（推迟到 ADR-025） |
| `object-interaction.md` | DOTween EaseOutQuad snap animation | DOTween sequence: position snap + optional rotation snap |
| `object-interaction.md` | Light source track movement (Fixed/TrackIdle/TrackDragging/TrackSnapping) | 接口位 `IInteractionEvent.OnLightPositionChanged` 已冻结，实施延后 |
| `object-interaction.md` | Boundary clamp with EaseOutBack rebound | Clamp during drag；DOTween EaseOutBack rebound on release if snap target outside bounds |
| `architecture.md` §6.2 | IObjectInteraction interface | Implemented as specified in architecture |

## Related

- **Depends On**: ADR-010 (Input Abstraction) — `IGestureEvent.OnTap/OnDrag/OnRotate` 是本 epic 唯一输入；ADR-027 (GameEvent Interface Protocol) — 定义 `IInteractionEvent` 接口形态
- **Consumed By**: ADR-012 (Shadow Match Algorithm) — receives `IInteractionEvent.OnObjectTransformChanged`
- **Consumed By**: ADR-016 (Narrative Sequence Engine) — sends `IInteractionEvent.OnRequestPuzzleLockAll` / `OnRequestSnapToTarget`
- **References**: `architecture.md` §4.2 (Object Interaction module ownership), §6.2 (IObjectInteraction interface), ADR-009 (纯 C# FSM 模式参考), ADR-027 (interface event protocol)
