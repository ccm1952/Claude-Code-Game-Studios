// 该文件由Cursor 自动生成

# Story 008: PC Keyboard/Mouse Input Mapping

> **Epic**: Input System
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/input-system.md`
**Requirement**: `TR-input-003`

**ADR Governing Implementation**: ADR-010: Input Abstraction
**ADR Decision Summary**: PC 端通过 `IPcInputAdapter` 将鼠标/键盘操作映射为与触屏相同的 `GestureData` 结构，直接注入手势候选队列，经过相同的 Blocker/Filter/dispatch 路径发出；PC 适配层与触屏 FSM 互斥运行（Editor 和 Standalone 平台使用 PC 适配，移动平台使用 Touch FSM）。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW

**Control Manifest Rules (Foundation layer)**:
- Required: PC 输入必须输出标准 `GestureData`，经过相同 Blocker/Filter 门控路径；PC 映射使用 `UnityEngine.Input`（旧版 API，与触屏保持一致）；`pcRotateSensitivity` 和 `pcScrollSensitivity` 来自 Luban `TbInputConfig`；PC 端：左键拖拽优先，右键旋转被忽略（当两者同时按住）
- Forbidden: 禁止使用 Unity New Input System package（`com.unity.inputsystem`）；禁止在移动平台运行 PC 适配层；PC 端右键拖拽同时按住左键时不产生旋转事件（左键优先）
- Guardrail: PC 端输入延迟目标与触屏相同（≤ 1 帧 16ms）；`Input.GetMouseButtonDown(0)` = 左键按下，`(1)` = 右键，`Input.GetAxis("Mouse ScrollWheel")` = 滚轮

---

## Acceptance Criteria

- [ ] `IPcInputAdapter` 接口（或内部 PC 适配类）在 Unity Editor / Standalone 平台激活，移动平台不运行
- [ ] **鼠标左键单击 → Tap**：`GetMouseButtonDown(0)` + `GetMouseButtonUp(0)` 在短时间内（无拖拽）→ 输出 `GestureType.Tap, Phase.Ended`，`ScreenPosition = Input.mousePosition`
- [ ] **鼠标左键拖拽 → Drag**：`GetMouseButton(0)` 按住并移动 → 输出 `Drag/Began`、`Drag/Updated`（含 `Delta = Input.mouseDelta`）、`Drag/Ended`
- [ ] **鼠标右键拖拽（水平方向）→ Rotate**：`GetMouseButton(1)` 按住并水平移动 → 输出 `Rotate/Updated`，`angleDelta = mouseDeltaX * pcRotateSensitivity`；正数 = 向右移动（顺时针 = 负值 angleDelta）
- [ ] **鼠标滚轮 → Pinch**：`Input.GetAxis("Mouse ScrollWheel")` 非零 → 输出 `Pinch/Updated`，`scaleDelta = 1.0 + scrollDelta * pcScrollSensitivity`
- [ ] **左键和右键同时按住**：左键拖拽优先，右键旋转被忽略（不产生 Rotate 事件）
- [ ] PC 端 Tap 判定阈值与触屏相同规则（鼠标位移 < dragThreshold 且时间 < tapTimeout）
- [ ] PC 端所有映射经过相同 InputBlocker / InputFilter 门控路径
- [ ] PC 端 `pcRotateSensitivity` 和 `pcScrollSensitivity` 来自 `TbInputConfig`（无硬编码）
- [ ] PC 端鼠标操作触发的手势事件与触屏触发的事件**格式完全相同**（同一 `GestureData` struct），上层系统无需区分来源

---

## Implementation Notes

来自 ADR-010 + GDD PC Mapping 表：

**平台路由**
```csharp
// InputService.Init()
#if UNITY_EDITOR || UNITY_STANDALONE
    _inputAdapter = new PcInputAdapter(_config);
#else
    _inputAdapter = new TouchInputAdapter(); // uses SingleFingerFSM + DualFingerFSM
#endif
```

**PC 映射表（来自 GDD）**
| 触屏手势 | PC 映射 | 说明 |
|---------|--------|------|
| Tap | 左键单击 | 鼠标位置 = Tap ScreenPosition |
| Drag | 左键按住拖拽 | Delta = 帧间鼠标位移 |
| Rotate | 右键按住拖拽（水平） | `angleDelta = mouseDeltaX * pcRotateSensitivity` |
| Pinch | 滚轮滚动 | `scaleDelta = 1.0 + scrollDelta * pcScrollSensitivity` |

**PcInputAdapter.Update() 伪代码**
```csharp
public GestureData? Update(float dragThreshold, float tapTimeout)
{
    bool leftDown  = Input.GetMouseButton(0);
    bool rightDown = Input.GetMouseButton(1);
    Vector2 mousePos = Input.mousePosition;
    Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    float scroll = Input.GetAxis("Mouse ScrollWheel");

    // Priority: left drag > right rotate
    if (leftDown)
    {
        UpdateTapDragFSM(mousePos, mouseDelta, dragThreshold, tapTimeout);
        // returns Tap, Drag/Began/Updated/Ended candidate
    }
    else if (rightDown && !leftDown)
    {
        float angleDelta = -mouseDelta.x * _config.PcRotateSensitivity; // negative = CW
        return new GestureData
        {
            Type       = GestureType.Rotate,
            Phase      = GesturePhase.Updated,
            AngleDelta = angleDelta,
            TouchCount = 0 // PC has no touch count concept
        };
    }

    if (Mathf.Abs(scroll) > 0.001f)
    {
        float scaleDelta = 1.0f + scroll * _config.PcScrollSensitivity;
        return new GestureData
        {
            Type       = GestureType.Pinch,
            Phase      = GesturePhase.Updated,
            ScaleDelta = scaleDelta,
            TouchCount = 0
        };
    }

    return null; // no gesture this frame
}
```

**注意**：PC 端 Rotate 和 Pinch 仅有 `Updated` phase（无 `Began`/`Ended`），因为鼠标无明确"手势开始/结束"语义。如果上层系统需要 `Began`/`Ended`，PC 适配层需要跟踪按键状态添加 phase 判定——MVP 先实现 `Updated` only。

**dragThreshold 在 PC 端**：使用相同的 DPI 归一化阈值（`Screen.dpi` 在 PC 上通常为 96），确保物理移动距离一致。

---

## Out of Scope

- [Story 001/002]: 触屏 FSM（移动平台专用）
- [Story 006]: `pcRotateSensitivity` / `pcScrollSensitivity` 的 Luban 加载（Story 006 已覆盖）
- [Story 003]: dispatch 路径（PC 映射候选进入同一 dispatch 流程）

---

## QA Test Cases

### TC-008-01: 鼠标左键单击产生 Tap
**Given** Unity Editor；监听 `Evt_Gesture_Tap`  
**When** 鼠标左键快速单击（无移动，< tapTimeout）  
**Then** 收到 1 个 `GestureData`：`Type=Tap, Phase=Ended, ScreenPosition=鼠标位置`；不收到 Drag 事件

### TC-008-02: 鼠标左键拖拽产生 Drag
**Given** Unity Editor；监听 `Evt_Gesture_Drag`  
**When** 按住左键并移动鼠标超过 dragThreshold  
**Then** 收到 `Drag/Began` → `Drag/Updated`（移动期间） → `Drag/Ended`（松开时）；Delta 方向与鼠标移动方向一致

### TC-008-03: 鼠标右键水平移动产生 Rotate
**Given** Unity Editor；监听 `Evt_Gesture_Rotate`；`pcRotateSensitivity = 0.005`  
**When** 按住右键，鼠标水平向右移动 20px/帧  
**Then** 收到 `Rotate/Updated`，`angleDelta ≈ -0.1 rad`（向右 = 顺时针 = 负值）；不收到 Pinch 或 Drag 事件

### TC-008-04: 滚轮产生 Pinch
**Given** Unity Editor；监听 `Evt_Gesture_Pinch`；`pcScrollSensitivity = 0.1`  
**When** 向上滚动滚轮（`scrollDelta = 0.2`）  
**Then** 收到 `Pinch/Updated`，`scaleDelta = 1.0 + 0.2 * 0.1 = 1.02`（放大）；向下滚 → `scaleDelta < 1.0`

### TC-008-05: 左键优先（同时按左右键）
**Given** 监听所有手势事件  
**When** 同时按住鼠标左键（拖拽）和右键  
**Then** 收到 Drag 事件；不收到 Rotate 事件（左键优先规则）

### TC-008-06: PC 端手势经过 InputBlocker 门控
**Given** `PushBlocker("test")`  
**When** 鼠标左键单击  
**Then** 0 个手势事件发出（Blocker 对 PC 输入同样有效）

### TC-008-07: PC 端 GestureData 格式与触屏相同
**Given** PC 和移动端分别触发 Tap 事件  
**When** 比较两个 GestureData 的字段类型和含义  
**Then** 字段完全相同（`Type`、`Phase`、`ScreenPosition`、`Delta`、`AngleDelta`、`ScaleDelta`、`TouchCount`、`TapCount`）；上层监听者代码无需 `#if UNITY_EDITOR` 分支

### TC-008-08: PC 端不在移动平台运行
**Given** 移动平台构建（iOS/Android）  
**When** 检查 `PcInputAdapter` 实例是否创建  
**Then** `_inputAdapter` 为 `TouchInputAdapter` 实例；`PcInputAdapter` 不存在于运行时；Touch FSM 正常运行

---

## Test Evidence

**Story Type**: Integration  
**Required evidence**: `tests/integration/InputSystem/PcInputMappingTests.cs`  
**Status**: [ ] Not yet created

**测试环境要求**：必须在 Unity Editor（Windows/macOS）中运行；需要模拟 `Input.GetMouseButton`、`Input.GetAxis` 的测试代理（Mock 或 PlayMode 集成测试）

---

## Dependencies

- Depends on: Story 003（dispatch 路径，PC 候选进入同一 dispatch 流），Story 006（`pcRotateSensitivity`、`pcScrollSensitivity` 配置）
- Unlocks: PC 端开发调试工作流（策划和开发者在 Editor 中使用鼠标测试手势逻辑）
