// 该文件由Cursor 自动生成

# Story 001: Single-Finger Gesture State Machine

> **Epic**: Input System
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/input-system.md`
**Requirement**: `TR-input-001`, `TR-input-011`, `TR-input-008`, `TR-input-014`, `TR-input-015`, `TR-input-016`

**ADR Governing Implementation**: ADR-010: Input Abstraction
**ADR Decision Summary**: 使用 Unity 旧版 Touch API（`Input.GetTouch()`）构建自定义三层输入系统；单指 FSM 负责 Idle→Pending→Tap/Dragging/LongPress 状态转换，所有阈值由 Luban 配置驱动，`tapTimeout` 使用 `Time.unscaledDeltaTime` 累加不受 TimeScale 影响。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW

**Control Manifest Rules (Foundation layer)**:
- Required: 三层架构实现（Raw Touch Sampling → Blocker/Filter Gate → Gesture Recognition）；`GestureData` 为 struct（值类型，热路径零 GC）；所有阈值来自 Luban `TbInputConfig`；DPI 归一化：`baseDragThreshold_mm * Screen.dpi / 25.4`（fallback DPI = 160）；最多跟踪前 2 个触摸点；暂停/切后台时清除所有触摸状态
- Forbidden: 禁止在 `InputService` 之外直接调用 `Input.GetTouch()`；禁止硬编码任何阈值数值；禁止在热路径使用 LINQ 或 string 操作
- Guardrail: 手势识别全帧预算 < 0.5ms（TR-input-016）；`Screen.dpi == 0` 时使用 fallbackDPI = 160

---

## Acceptance Criteria

- [x] `SingleFingerFSM` 实现 4 个状态：`Idle`、`Pending`、`Dragging`、`LongPress` — `SingleFingerFSM.cs` enum + switch
- [x] `Idle → Pending`：检测到 `TouchPhase.Began` 时进入，记录按下位置和 `unscaledTime` — `ProcessIdle()`
- [x] `Pending → Tap`：手指抬起（`TouchPhase.Ended`）且移动距离 < `dragThreshold` 且持续时间 < `tapTimeout` → 触发 Tap 逻辑，回到 `Idle` — `ProcessPending()` + test `Tap_WithinThresholdAndTimeout`
- [x] `Pending → Dragging`：移动距离 > `dragThreshold` → 进入 `Dragging`，触发 `DragBegan` — `ProcessPending()` + test `Pending_ExceedsDragThreshold`
- [x] `Pending → LongPress`：持续时间 > `tapTimeout` 且未移动超过阈值 → 进入 `LongPress`，触发 `LongPressBegan`（预留，MVP 不分发事件） — `ProcessPending()` + test `Pending_ExceedsTapTimeout`
- [x] `Dragging`：每帧触发 `DragUpdated`（含 `delta` = 当前位置 - 上帧位置）；手指抬起时触发 `DragEnded` 并回到 `Idle` — `ProcessDragging()` + test `Pending_ExceedsDragThreshold`（含 Updated + Ended 断言）
- [x] 单帧最大 delta 限制为 `maxDeltaPerFrame`（来自 Luban 配置），超出时 clamp — `ProcessDragging()` + test `Dragging_FastDelta_Clamped`
- [x] `tapTimeout` 使用 `Time.unscaledDeltaTime` 累加，与 `Time.timeScale` 无关 — `AccumulatedTime` 字段 + test `TapTimeout_UsesUnscaledTime`
- [x] `dragThreshold` 计算：`baseDragThreshold_mm * Screen.dpi / 25.4`；`Screen.dpi == 0` 时 fallback `160` — `ComputeDragThreshold()` + test `DragThreshold_ZeroDpi_UsesFallback`
- [x] 应用切后台（`OnApplicationPause(true)`）时，FSM 强制回到 `Idle`，清除所有中间状态 — `ForceReset()` + test `ForceReset_FromDragging_ReturnsToIdle`
- [x] `TouchState` 数组预分配（容量 = 2），无运行时 allocation — `TouchState` / `GestureData` 为 struct + test `TouchState_PreAllocated_NoGCOnHotPath`
- [x] Profiler marker `InputService.Update` 覆盖本 FSM 的 Update 调用；真机上帧耗时 < 0.5ms — `ProfilerMarker` 已插桩；DEFERRED: 真机性能验证在集成测试阶段

---

## Implementation Notes

来自 ADR-010 Implementation Guidelines：

**Touch Sampling（Layer 1）**
- 在 `Update()` 中调用 `Input.GetTouch(i)`，遍历 `Input.touchCount`
- 只跟踪前 2 个触摸点（fingerId 映射到 slot 0/1），忽略第 3+ 触摸
- 维护 `TouchState` struct 数组（pre-allocated, size=2）存储帧间状态

**SingleFingerFSM 状态转换**
```
Idle
  ↓ TouchPhase.Began
Pending  ← 记录: startPos, startTime (unscaledTime), accumulatedDist
  ↓ dist > dragThreshold           ↓ time > tapTimeout     ↓ Ended + dist < drag + time < tap
Dragging                         LongPress                  → emit Tap, return Idle
  ↓ TouchPhase.Ended
  emit DragEnded → Idle
```

**delta clamp**
```csharp
Vector2 rawDelta = currentPos - prevPos;
float mag = rawDelta.magnitude;
if (mag > maxDeltaPerFrame)
    rawDelta = rawDelta.normalized * maxDeltaPerFrame;
```

**GestureData 构建（单指部分）**
```csharp
var data = new GestureData
{
    Type       = GestureType.Drag,
    Phase      = GesturePhase.Updated,
    ScreenPosition = currentPos,
    Delta      = clampedDelta,
    TouchCount = 1,
    TapCount   = 0
};
```

**注意**：本 Story 只实现 FSM 状态转换 + GestureData 填充，不负责 GameEvent dispatch（见 Story 003）和 Blocker/Filter 门控（见 Story 004/005）。FSM 输出 candidate gesture，供 Story 003 dispatch。

---

## Out of Scope

- [Story 002]: 双指旋转/缩放 FSM（DualFingerFSM）
- [Story 003]: GameEvent dispatch（将 candidate gesture 广播出去）
- [Story 004]: InputBlocker 栈（在 FSM 之前的门控逻辑）
- [Story 005]: InputFilter 白名单（在 FSM 之后的门控逻辑）
- [Story 006]: Luban 配置表加载（阈值读取封装）

---

## QA Test Cases

### TC-001-01: 正常 Tap 识别
**Given** FSM 处于 Idle 状态，`dragThreshold` = 30px，`tapTimeout` = 0.25s  
**When** 手指按下后在 0.1s 内抬起，总移动距离 = 5px  
**Then** FSM 输出 `GestureType.Tap`，phase = `Ended`，`ScreenPosition` = 按下位置；FSM 回到 Idle

### TC-001-02: Tap 超时不触发（转 LongPress）
**Given** FSM 处于 Idle 状态，`tapTimeout` = 0.25s  
**When** 手指按下后保持静止超过 0.3s（移动距离 < dragThreshold）  
**Then** 在 0.25s 时进入 `LongPress` 状态；最终抬起时不输出 Tap 事件

### TC-001-03: 正常 Drag 识别
**Given** FSM 处于 Pending 状态，`dragThreshold` = 30px  
**When** 手指连续移动，累计距离达到 31px  
**Then** FSM 进入 Dragging，同帧输出 `GestureType.Drag, Phase.Began`；后续每帧输出 `Phase.Updated`；手指抬起时输出 `Phase.Ended`

### TC-001-04: 快速移动不超过 maxDeltaPerFrame
**Given** `maxDeltaPerFrame` = 100px，FSM 处于 Dragging  
**When** 单帧 raw delta = 200px（低帧率场景）  
**Then** 输出 delta 被 clamp 到 100px，方向不变

### TC-001-05: 切后台清除状态
**Given** FSM 处于 Dragging 状态  
**When** `OnApplicationPause(true)` 被调用  
**Then** FSM 强制回到 Idle；无 `DragEnded` 事件输出（静默取消）；切回前台后 FSM 正常响应新触摸

### TC-001-06: Screen.dpi = 0 兜底
**Given** `Screen.dpi` 返回 0（模拟器环境），`baseDragThreshold_mm` = 3.0mm  
**When** 计算 `dragThreshold`  
**Then** 使用 fallback DPI = 160，`dragThreshold` = 3.0 × 160 / 25.4 ≈ 18.9px；不抛异常不返回 NaN

### TC-001-07: tapTimeout 不受 TimeScale 影响
**Given** `Time.timeScale` = 0（游戏暂停），`tapTimeout` = 0.25s  
**When** 手指按下并保持 0.3s（unscaled 时间）  
**Then** LongPress 仍然在 0.25s unscaled 时触发；TimeScale = 0 不影响判定

### TC-001-08: TouchState 预分配无 GC
**Given** 连续 100 帧触摸输入  
**When** 在 Unity Profiler（Deep Profile）中观察 InputService.Update  
**Then** `GC.Alloc` 列显示 0B（boxing 在 dispatch 层，不在 FSM 层）

---

## Test Evidence

**Story Type**: Logic  
**Required evidence**: `Assets/Tests/EditMode/InputSystem/SingleFingerFSMTests.cs`  
**Status**: [x] Created — 8 test functions, 8/8 PASS (Unity Test Runner 2026-04-22)

**Test class pattern**:
```csharp
[TestFixture]
public class SingleFingerFSMTests
{
    // Mock IInputConfig with test values
    // Drive FSM via synthetic TouchState inputs
    // Assert GestureData outputs
}
```

---

## Dependencies

- Depends on: None（Foundation 层首个 story，无上游依赖）
- Unlocks: Story 002（DualFinger FSM 需要知道单指 Dragging 被取消的时机），Story 003（dispatch 需要 FSM 输出的 candidate）
