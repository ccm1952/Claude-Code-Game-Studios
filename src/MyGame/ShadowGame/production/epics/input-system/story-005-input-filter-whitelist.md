// 该文件由Cursor 自动生成

# Story 005: InputFilter Whitelist

> **Epic**: Input System
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/input-system.md`
**Requirement**: `TR-input-005`, `TR-input-006`

**ADR Governing Implementation**: ADR-010: Input Abstraction
**ADR Decision Summary**: InputFilter 为单一激活的白名单过滤——`PushFilter(allowedGestures)` 设置当前允许的手势类型列表，新 push 覆盖旧 filter（不叠加）；`PopFilter()` 恢复无过滤状态；InputFilter 优先级低于 InputBlocker（Blocker 激活时 Filter 逻辑被跳过）；`allowedGestures` 数组在 Push 时深拷贝防止外部修改。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW

**Control Manifest Rules (Foundation layer)**:
- Required: `IInputService` 接口暴露 `PushFilter(GestureType[] allowedGestures)` / `PopFilter()` / `bool IsFiltered { get; }` / `GestureType[] ActiveFilterGestures { get; }`；单一激活 Filter（新 Push 覆盖旧 Filter，不是入栈）；`allowedGestures` 数组 Push 时深拷贝；Filter 检查在 FSM 之后、dispatch 之前（已知手势类型才能判断白名单）；优先级链：Blocker（最高）> Filter > Pass-through（最低）
- Forbidden: 禁止被 Filter 过滤的手势产生任何事件、视觉/音频/触觉反馈；禁止 Filter 影响 Blocker 状态（两者独立）；禁止 Filter 叠加（Push 替换，不入栈）
- Guardrail: Filter check 为 O(n)（n = allowedGestures 长度，最大 5）；TutorialPromptPanel 使用 Filter（非 Blocker），见 ADR-011

---

## Acceptance Criteria

- [x] `IInputService` 接口包含：`PushFilter(GestureType[] allowedGestures)`、`PopFilter()`、`bool IsFiltered`、`GestureType[] ActiveFilterGestures`
- [x] `PushFilter(allowedGestures)`：深拷贝 `allowedGestures` 数组（防止调用方后续修改影响 Filter 状态）；覆盖之前的 Filter（不叠加）；`IsFiltered = true`
- [x] `PopFilter()`：清除 active filter；`IsFiltered = false`；`ActiveFilterGestures` 返回空数组或 null
- [x] Filter 检查时机：在 Gesture FSM 运行后（知道手势类型）、dispatch 前；`allowedGestures` 包含该类型 → 通过；不包含 → 静默丢弃（不发事件，不产生任何反馈）
- [x] Filter 激活时，`InputBlocker.IsBlocked = true` → Filter 逻辑被跳过（Blocker 优先，全量阻断）
- [x] Filter pop 后不恢复上一个 filter（状态直接变为"无过滤"）
- [x] `PushFilter` 覆盖旧 filter：第二次 `PushFilter([Rotate, Pinch])` 替换第一次 `PushFilter([Tap])`，之后只允许 Rotate 和 Pinch
- [x] 被 Filter 过滤的手势：0 个 GameEvent 发出；0 个 haptic feedback 触发；0 个视觉/音频回调（本 Story 验证事件层，haptic 在 Story 007）
- [x] `allowedGestures` 最多包含 5 种手势类型（`GestureType` 枚举成员数 = 5），超出不报错（正常过滤）

---

## Implementation Notes

来自 ADR-010 + GDD Implementation Guidelines：

**数据结构**
```csharp
private GestureType[] _activeFilter = null; // null = no filter
```

**PushFilter 实现**
```csharp
public void PushFilter(GestureType[] allowedGestures)
{
    // Deep copy to prevent external mutation
    _activeFilter = allowedGestures != null
        ? (GestureType[])allowedGestures.Clone()
        : Array.Empty<GestureType>();
}
```

**PopFilter 实现**
```csharp
public void PopFilter()
{
    _activeFilter = null;
}

public bool IsFiltered => _activeFilter != null;
public GestureType[] ActiveFilterGestures => _activeFilter ?? Array.Empty<GestureType>();
```

**Filter 检查（dispatch 前）**
```csharp
private bool IsGestureAllowedByFilter(GestureType type)
{
    if (!IsFiltered) return true;        // no filter = pass
    if (IsBlocked) return false;         // blocker wins (already handled earlier, belt+suspenders)
    foreach (var allowed in _activeFilter)
        if (allowed == type) return true;
    return false; // not in whitelist → discard silently
}
```

**优先级链集成（Update() 中）**
```csharp
if (IsBlocked) return; // Blocker: discard all (highest priority)
// run FSMs...
foreach (var candidate in _pendingGestures)
{
    if (!IsGestureAllowedByFilter(candidate.Type))
        continue; // Filter: whitelist check (priority 2)
    GameEvent.Send<GestureData>(GetEventId(candidate.Type), candidate); // Pass-through
}
```

**Tutorial 场景示例**（来自 GDD 互操作示例）：
```
Tutorial.PushFilter([Drag])          → 只允许 Drag
Narrative.PushBlocker("Narr_Seq01") → 全量阻断，Filter 暂时无效
Narrative.PopBlocker("Narr_Seq01")  → Filter 恢复（仍只允许 Drag）
Tutorial.PopFilter()                 → 恢复正常通过
```

---

## Out of Scope

- [Story 004]: InputBlocker 实现（独立栈，不在本 Story 中实现）
- [Story 003]: dispatch 层（消费 Filter 结果，不实现 Filter）

---

## QA Test Cases

### TC-005-01: Filter 基本白名单过滤
**Given** `PushFilter([GestureType.Tap])`；监听所有手势事件  
**When** 执行 Drag 操作（移动超过阈值）  
**Then** 0 个 Drag 事件发出；过滤静默（无 warning、无异常、无任何反馈）

### TC-005-02: 白名单内手势正常通过
**Given** `PushFilter([GestureType.Drag])`  
**When** 执行单指 Drag 操作  
**Then** Drag 事件正常发出（Began/Updated/Ended）；Tap 操作被过滤（0 个 Tap 事件）

### TC-005-03: PushFilter 覆盖旧 Filter
**Given** `PushFilter([GestureType.Tap])`（第一次）  
**When** `PushFilter([GestureType.Rotate, GestureType.Pinch])`（第二次）  
**Then** `ActiveFilterGestures` = `[Rotate, Pinch]`；Tap 现在被过滤；Rotate/Pinch 正常通过

### TC-005-04: PopFilter 恢复所有手势
**Given** `PushFilter([GestureType.Tap])`，然后 `PopFilter()`  
**When** 执行 Drag 操作  
**Then** Drag 事件正常发出；`IsFiltered = false`；`ActiveFilterGestures` 为空

### TC-005-05: Blocker 优先于 Filter
**Given** `PushFilter([GestureType.Tap])`（Filter 激活），`PushBlocker("test")`（Blocker 激活）  
**When** 执行 Tap 操作  
**Then** 0 个事件发出（Blocker 全量阻断，即使 Tap 在白名单内）；Pop Blocker 后 → Tap 恢复（Filter 仍激活）

### TC-005-06: allowedGestures 深拷贝保护
**Given** `var arr = new GestureType[] { GestureType.Tap }`，`PushFilter(arr)`  
**When** 外部修改 `arr[0] = GestureType.Drag`  
**Then** `ActiveFilterGestures[0]` 仍为 `GestureType.Tap`（深拷贝有效，外部修改无影响）

### TC-005-07: 空白名单（PushFilter 空数组）
**Given** `PushFilter(new GestureType[0])`（空白名单）  
**When** 执行任意手势  
**Then** 所有手势被过滤（0 个事件）；`IsFiltered = true`；不抛异常

### TC-005-08: Filter 与 Blocker 完整互操作序列
**Given** 以下操作序列：  
1. `PushFilter([Drag])`  
2. `PushBlocker("Narr")`  
3. 执行 Drag → 预期 0 个事件  
4. `PopBlocker("Narr")`  
5. 执行 Drag → 预期 Drag 事件正常  
6. 执行 Tap → 预期 0 个事件（Filter 仍激活）  
7. `PopFilter()`  
8. 执行 Tap → 预期 Tap 事件正常  
**Then** 每步预期均正确

---

## Test Evidence

**Story Type**: Logic  
**Required evidence**: `tests/unit/InputSystem/InputFilterTests.cs`  
**Status**: [x] 10/10 PASS — `InputFilterTests.cs`

**关键测试**：
- Filter 覆盖语义（非叠加）
- Blocker + Filter 互操作序列（TC-005-08 完整序列）
- 深拷贝保护

---

## Dependencies

- Depends on: None（独立的白名单数据结构，无上游 story 依赖）
- Unlocks: Story 003（dispatch 依赖 Filter 门控结果）；Tutorial System 集成可以开始
