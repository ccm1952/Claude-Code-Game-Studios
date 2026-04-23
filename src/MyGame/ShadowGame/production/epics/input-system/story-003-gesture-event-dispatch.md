// 该文件由Cursor 自动生成

# Story 003: Gesture Event Dispatch

> **Epic**: Input System
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Integration
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/input-system.md`
**Requirement**: `TR-input-002`, `TR-input-007`, `TR-input-008`, `TR-input-016`

**ADR Governing Implementation**: ADR-010: Input Abstraction
**ADR Decision Summary**: 手势通过 TEngine `GameEvent.Send<GestureData>(int eventId, GestureData data)` 分发，5 种手势各有独立 event ID（1000–1004），定义在集中式 `EventId` 静态类中；`GestureData` 为 struct 值类型；每种手势均有 `Began / Updated / Ended / Cancelled` 四个生命周期阶段。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW

**Control Manifest Rules (Foundation layer)**:
- Required: 所有事件 ID 定义在集中式 `EventId` 静态类（ADR-006）；Input 系统分配范围 1000–1099（Tap=1000, Drag=1001, Rotate=1002, Pinch=1003, LightDrag=1004）；`GestureData` struct 作为 payload；每个 EventId 常量必须有 XML doc comment（Sender、Listener、Payload 类型、Cascade info）；热路径零 GC allocation
- Forbidden: 禁止在 `EventId.cs` 以外定义手势事件 ID；禁止使用匿名 `object` 参数传递 payload；禁止在同一事件 ID handler 内重入 `GameEvent.Send`；禁止使用 C# event/delegate 跨模块通信
- Guardrail: SP-001 已确认 `GameEvent.Send<GestureData>` 支持 struct payload；HybridCLR AOT 泛型待真机验证（SP-007）；每次 `Send` 的单次 dispatch 耗时 < 0.05ms

---

## Acceptance Criteria

- [ ] `EventId.cs` 中新增 5 个手势事件 ID 常量（含 XML doc comment）：`Evt_Gesture_Tap = 1000`，`Evt_Gesture_Drag = 1001`，`Evt_Gesture_Rotate = 1002`，`Evt_Gesture_Pinch = 1003`，`Evt_Gesture_LightDrag = 1004`
- [ ] `InputService.Update()` 在 Blocker/Filter 门控通过后，对每个 candidate gesture 调用 `GameEvent.Send<GestureData>(eventId, data)`
- [ ] Tap 事件：`Phase = GesturePhase.Ended`，`ScreenPosition` = 触摸位置，`TapCount = 1`（双击时 = 2，预留）
- [ ] Drag 事件：`Began` / `Updated` / `Ended` / `Cancelled` 四个 phase 均正确发出，`Delta` = 帧间 clamp 后位移
- [ ] Rotate 事件：`Began` / `Updated` / `Ended` 正确发出，`AngleDelta` = 当帧旋转弧度
- [ ] Pinch 事件：`Began` / `Updated` / `Ended` 正确发出，`ScaleDelta` = 当帧缩放比例
- [ ] LightDrag 事件：与 Drag 语义相同，类型标记为 `GestureType.LightDrag`；判定逻辑委托给 Object Interaction 层（当前 MVP：InputService 不自行发出 LightDrag，该 event ID 预留）
- [ ] 当 InputBlocker 栈非空时，所有手势事件**不发出**（零泄漏）
- [ ] 当 InputFilter 激活时，白名单之外的手势类型**不发出**（零泄漏）
- [ ] HybridCLR AOT 泛型：在 `AOTGenericReferences.cs` 中注册 `GameEvent.Send<GestureData>` 所需的泛型实例

---

## Implementation Notes

来自 ADR-010 + ADR-006 Implementation Guidelines：

**EventId 常量定义（在 EventId.cs 中追加）**
```csharp
/// <summary>
/// 触摸 Tap 手势事件。
/// Sender: InputService
/// Listener: ObjectInteractionSystem, TutorialSystem
/// Payload: GestureData (Type=Tap, Phase=Ended, ScreenPosition, TapCount)
/// Cascade depth: 1 (listener may send Evt_ObjectSelected)
/// </summary>
public const int Evt_Gesture_Tap = 1000;

/// <summary>
/// 单指拖拽手势事件（Began/Updated/Ended/Cancelled）。
/// Sender: InputService
/// Listener: ObjectInteractionSystem, TutorialSystem
/// Payload: GestureData (Type=Drag, Phase, ScreenPosition, Delta)
/// Cascade depth: 1
/// </summary>
public const int Evt_Gesture_Drag = 1001;

// Evt_Gesture_Rotate = 1002 (AngleDelta, Phase)
// Evt_Gesture_Pinch  = 1003 (ScaleDelta, Phase)
// Evt_Gesture_LightDrag = 1004 (reserved, MVP not dispatched)
```

**单帧 dispatch 时序（在 InputService.Update 末尾）**
```
1. Sample raw touches (Layer 1)
2. Check InputBlocker → if blocked, return early (no dispatch)
3. Run SingleFingerFSM + DualFingerFSM → produce candidate GestureData list
4. For each candidate:
   a. Check InputFilter → if filtered, skip
   b. GameEvent.Send<GestureData>(eventId, gestureData)
```

**HybridCLR AOT 注册**
```csharp
// AOTGenericReferences.cs (Default assembly)
// 需要添加：
// GameEvent.Send<GestureData>(int, GestureData)
// GameEvent.AddEventListener<GestureData>(int, Action<GestureData>)
```

**SP-001 注意事项**：`GameEvent.Send<GestureData>` 为单参数泛型（int eventType + TArg1），SP-001 已确认可行。`GestureData` 在 Send 时被装箱为 `object` 是唯一 allocation 点（约 40B/次），属于 MVP 可接受范围，后续可优化为 `Send<TArg1>` 的泛型版本避免装箱。

---

## Out of Scope

- [Story 001]: SingleFingerFSM candidate 生产
- [Story 002]: DualFingerFSM candidate 生产
- [Story 004]: InputBlocker 门控实现（本 story 只消费其结果）
- [Story 005]: InputFilter 门控实现（本 story 只消费其结果）
- [Story 008]: PC 端 keyboard/mouse 输入映射（同样输出 GestureData，走相同 dispatch 路径）

---

## QA Test Cases

### TC-003-01: Tap 事件正确发出
**Given** InputService 已初始化，无 Blocker/Filter；监听 `Evt_Gesture_Tap`  
**When** 单指快速 Tap（距离 < dragThreshold，时间 < tapTimeout）  
**Then** 收到 1 个 `GestureData`：`Type=Tap, Phase=Ended, TapCount=1, ScreenPosition` 与触摸位置匹配；不收到 Drag 事件

### TC-003-02: Drag 三阶段事件完整性
**Given** 监听 `Evt_Gesture_Drag`  
**When** 单指拖拽：按下（移动超阈值）→ 持续移动 3 帧 → 抬起  
**Then** 收到事件序列：`Drag/Began`（1次）→ `Drag/Updated`（3次）→ `Drag/Ended`（1次）；共 5 个事件，顺序正确

### TC-003-03: DragCancelled 在双指接入时发出
**Given** 单指正在 Dragging，监听 `Evt_Gesture_Drag`  
**When** 第二根手指落下  
**Then** 收到 `Drag/Cancelled` 事件；之后不再收到 Drag 事件，开始收到 Rotate 或 Pinch 事件

### TC-003-04: Rotate 事件在 Blocked 时零泄漏
**Given** InputBlocker 栈非空（Blocker 已 push），监听 `Evt_Gesture_Rotate`  
**When** 执行双指旋转操作  
**Then** 收到 0 个手势事件（Rotate/Pinch/Tap/Drag 均无）；Blocker pop 后，后续手势恢复正常

### TC-003-05: InputFilter 过滤效果（只允许 Tap）
**Given** InputFilter 激活，`allowedGestures = [GestureType.Tap]`；监听所有手势事件  
**When** 执行单指 Drag 操作，再执行单指 Tap 操作  
**Then** Drag 事件：收到 0 个；Tap 事件：收到 1 个 `Tap/Ended`

### TC-003-06: 5 个 EventId 常量值正确且唯一
**Given** `EventId.cs` 已更新  
**When** 读取各常量值  
**Then** `Evt_Gesture_Tap=1000`, `Evt_Gesture_Drag=1001`, `Evt_Gesture_Rotate=1002`, `Evt_Gesture_Pinch=1003`, `Evt_Gesture_LightDrag=1004`；无重复 ID；均有 XML doc comment

### TC-003-07: Pinch ScaleDelta 值域正确
**Given** 监听 `Evt_Gesture_Pinch`  
**When** 两指从 100px 张开到 120px（单帧）  
**Then** 收到 `Pinch/Updated`，`ScaleDelta ≈ 1.2`（允许浮点误差 ±0.01）

### TC-003-08: AOT 泛型不在真机崩溃
**Given** HybridCLR 编译后的 IL2CPP 包，已在 `AOTGenericReferences.cs` 注册 `GameEvent.Send<GestureData>`  
**When** 在真机上执行任意手势（集成测试，Sprint 0 SP-007 验证窗口）  
**Then** 无 `ExecutionEngineException`；手势事件正常触发上层 listener

---

## Test Evidence

**Story Type**: Integration  
**Required evidence**: `tests/integration/InputSystem/GestureDispatchIntegrationTests.cs`  
**Status**: [ ] Not yet created

**Test class pattern**:
```csharp
[TestFixture]
public class GestureDispatchIntegrationTests
{
    // Use InputService with mocked IInputConfig
    // Drive synthetic TouchState sequences
    // Subscribe to GameEvent and assert received GestureData
    // Test Blocker/Filter integration at dispatch level
}
```

---

## Dependencies

- Depends on: Story 001（SingleFingerFSM），Story 002（DualFingerFSM），Story 004（InputBlocker 门控），Story 005（InputFilter 门控）
- Unlocks: 所有消费手势事件的上层系统（Object Interaction、Tutorial）可以开始实现
