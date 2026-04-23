// 该文件由Cursor 自动生成

# Story 002: Dual-Finger Gesture State Machine

> **Epic**: Input System
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/input-system.md`
**Requirement**: `TR-input-002`, `TR-input-009`, `TR-input-010`, `TR-input-012`, `TR-input-013`, `TR-input-014`, `TR-input-016`

**ADR Governing Implementation**: ADR-010: Input Abstraction
**ADR Decision Summary**: 使用 `DualFingerFSM` 实现双指手势识别（Idle→Pending2→Rotating/Pinching），双指手势互斥——一旦进入 Rotating 或 Pinching 状态，锁定直到所有手指抬起；双指间距 < `minFingerDistance`（20px）时忽略角度/缩放输入防止抖动。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW

**Control Manifest Rules (Foundation layer)**:
- Required: 双指手势互斥锁定（进入 Rotating/Pinching 后不可中途切换）；最多跟踪前 2 个触摸点，忽略 index ≥ 2；双指间距 < `minFingerDistance`（20px）时强制 `scaleDelta = 1.0`、忽略旋转输入；所有阈值来自 Luban `TbInputConfig`；单指 Dragging 检测到第二根手指时发送 `DragCancelled` 并进入双指 Pending2
- Forbidden: 禁止在 Rotating/Pinching 状态中途切换对方手势；禁止直接 `Input.GetTouch()` 在 `InputService` 之外；禁止硬编码旋转/缩放阈值
- Guardrail: 手势识别全帧预算 < 0.5ms（TR-input-016）；旋转角度计算使用 atan2(cross, dot)，非简单角度差分

---

## Acceptance Criteria

- [ ] `DualFingerFSM` 实现 4 个状态：`Idle`、`Pending2`、`Rotating`、`Pinching`
- [ ] `Idle → Pending2`：检测到第二根手指按下时进入；若当前单指处于 Dragging，立即发送 `DragCancelled` 事件
- [ ] `Pending2 → Rotating`：累计角度变化 `abs(accumulatedAngle) > rotateThreshold` → 进入 Rotating，触发 `RotateBegan`
- [ ] `Pending2 → Pinching`：累计缩放偏离 `abs(accumulatedScale - 1.0) > pinchThreshold` → 进入 Pinching，触发 `PinchBegan`
- [ ] `Rotating` 互斥锁定：进入后每帧触发 `RotateUpdated`（含 `angleDelta`），不切换到 Pinching；任一手指抬起→ `RotateEnded`，回到 Idle
- [ ] `Pinching` 互斥锁定：进入后每帧触发 `PinchUpdated`（含 `scaleDelta`），不切换到 Rotating；任一手指抬起→ `PinchEnded`，回到 Idle
- [ ] 旋转角度计算：`angleDelta = atan2(cross(prevDir, currDir), dot(prevDir, currDir))`，正值 = 逆时针
- [ ] 缩放比例计算：`scaleDelta = currDistance / prevDistance`
- [ ] 双指间距 < `minFingerDistance`（20px）时：忽略旋转输入（不累积 angle）；强制 `scaleDelta = 1.0`
- [ ] `prevDistance < minFingerDistance` 时强制 `scaleDelta = 1.0`，防止除零
- [ ] 三指或更多触摸时，只使用前两个 fingerId，忽略后续
- [ ] 手指滑出屏幕（`TouchPhase.Canceled`）视同 `TouchPhase.Ended` 处理

---

## Implementation Notes

来自 ADR-010 Implementation Guidelines：

**DualFingerFSM 状态转换**
```
Idle
  ↓ 第二根手指 TouchPhase.Began
  （若单指 Dragging → 先 emit DragCancelled）
Pending2  ← 记录: initialDistance, initialAngle, accumulatedAngle=0, accumulatedScale=1.0
  ↓ abs(accumulatedAngle) > rotateThreshold     ↓ abs(accumulatedScale-1.0) > pinchThreshold
Rotating                                       Pinching
  ↓ 任一手指 Ended/Canceled                     ↓ 任一手指 Ended/Canceled
  emit RotateEnded → Idle                       emit PinchEnded → Idle
```

**旋转角度算法**
```csharp
Vector2 prevDir = (prevTouch1 - prevTouch0).normalized;
Vector2 currDir = (currTouch1 - currTouch0).normalized;
float cross = prevDir.x * currDir.y - prevDir.y * currDir.x;
float dot   = Vector2.Dot(prevDir, currDir);
float angleDelta = Mathf.Atan2(cross, dot); // radians, positive = CCW
```

**缩放安全保障**
```csharp
float prevDist = Vector2.Distance(prevTouch0, prevTouch1);
float currDist = Vector2.Distance(currTouch0, currTouch1);
float scaleDelta = (prevDist < minFingerDistance) ? 1.0f : (currDist / prevDist);
```

**与 SingleFingerFSM 协调**：DualFingerFSM 持有对 SingleFingerFSM 的引用，在进入 Pending2 时检查单指状态并请求取消。实现方式：SingleFingerFSM 暴露 `CancelDrag()` 方法，由 DualFingerFSM 调用。

**GestureData 构建（双指部分）**
```csharp
// Rotate
var data = new GestureData
{
    Type        = GestureType.Rotate,
    Phase       = GesturePhase.Updated,
    AngleDelta  = angleDelta,    // radians
    TouchCount  = 2,
    ScreenPosition = midpoint    // 两指中点
};

// Pinch
var data = new GestureData
{
    Type        = GestureType.Pinch,
    Phase       = GesturePhase.Updated,
    ScaleDelta  = scaleDelta,    // >1 放大 <1 缩小
    TouchCount  = 2,
    ScreenPosition = midpoint
};
```

**注意**：本 Story 只实现 FSM 状态转换 + GestureData 填充，GameEvent dispatch 见 Story 003。

---

## Out of Scope

- [Story 001]: 单指 FSM（SingleFingerFSM）
- [Story 003]: GameEvent dispatch for Rotate/Pinch events
- [Story 006]: Luban 配置加载（`rotateThreshold`、`pinchThreshold`、`minFingerDistance`）

---

## QA Test Cases

### TC-002-01: 旋转手势识别（从 Pending2 到 Rotating）
**Given** `rotateThreshold` = 8°（0.14 rad），`pinchThreshold` = 0.08；两指同时接触，初始间距 100px  
**When** 两指产生 10° 累计角度变化，间距变化 < 8px（约 8%）  
**Then** FSM 进入 Rotating，输出 `GestureType.Rotate, Phase.Began`；后续每帧输出 `Phase.Updated`（含正确 `angleDelta`）

### TC-002-02: 缩放手势识别（从 Pending2 到 Pinching）
**Given** `rotateThreshold` = 8°，`pinchThreshold` = 0.08；两指同时接触，初始间距 100px  
**When** 两指向外扩展到 115px（scale 累计 = 1.15，偏离 > 0.08），角度变化 < 5°  
**Then** FSM 进入 Pinching，输出 `GestureType.Pinch, Phase.Began`；`scaleDelta` = currDist/prevDist

### TC-002-03: 旋转与缩放互斥锁定
**Given** FSM 已进入 Rotating  
**When** 两指间距突然变化超过 pinchThreshold  
**Then** FSM 保持 Rotating，不切换到 Pinching；每帧仍输出 Rotate 事件，不输出 Pinch 事件

### TC-002-04: 单指 Drag 被双指取消
**Given** 单指 FSM 处于 Dragging 状态  
**When** 第二根手指按下  
**Then** 当帧输出 `GestureType.Drag, Phase.Cancelled`；SingleFingerFSM 回到 Idle；DualFingerFSM 进入 Pending2

### TC-002-05: 双指间距过小忽略旋转
**Given** 两指间距 = 15px（< minFingerDistance = 20px）  
**When** 两指产生 20° 角度变化  
**Then** `angleDelta` 被忽略（不累积到 `accumulatedAngle`）；FSM 不进入 Rotating

### TC-002-06: prevDistance 接近零时缩放安全保障
**Given** 上帧 `prevDistance` = 5px（< 20px）  
**When** 计算 `scaleDelta`  
**Then** `scaleDelta = 1.0f`（强制），不产生除零异常，不输出极端缩放数据

### TC-002-07: 手指滑出屏幕视为抬起
**Given** FSM 处于 Rotating 状态  
**When** 其中一根手指 `TouchPhase.Canceled`  
**Then** 立即输出 `RotateEnded`；FSM 回到 Idle；剩余手指不自动进入单指 Drag

### TC-002-08: 三指触摸只用前两个
**Given** 同时有 3 根手指在屏幕  
**When** FSM 处理触摸数据  
**Then** 只使用 fingerId 最小的前两个触摸点；第三个触摸点被忽略；不抛异常

### TC-002-09: 旋转方向正确性
**Given** touch0 在左，touch1 在右，两指顺时针旋转 10°  
**When** 计算 `angleDelta`  
**Then** `angleDelta < 0`（负值 = 顺时针）；使用 atan2(cross, dot) 算法

---

## Test Evidence

**Story Type**: Logic  
**Required evidence**: `tests/unit/InputSystem/DualFingerFSMTests.cs`  
**Status**: [ ] Not yet created

**Test class pattern**:
```csharp
[TestFixture]
public class DualFingerFSMTests
{
    // Mock synthetic dual-touch inputs (Vector2 positions per frame)
    // Assert Rotate/Pinch GestureData outputs
    // Test mutual exclusion scenarios
}
```

---

## Dependencies

- Depends on: Story 001（DualFingerFSM 需要调用 `SingleFingerFSM.CancelDrag()`）
- Unlocks: Story 003（dispatch 需要 DualFinger 输出的 Rotate/Pinch candidates）
