// 该文件由Cursor 自动生成

# Story 004: InputBlocker Stack

> **Epic**: Input System
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/input-system.md`
**Requirement**: `TR-input-004`, `TR-input-006`, `TR-input-015`

**ADR Governing Implementation**: ADR-010: Input Abstraction
**ADR Decision Summary**: InputBlocker 采用 token 字符串栈管理多个阻断源叠加——栈非空时全量阻断所有手势输入（最高优先级）；`PopBlocker(token)` 必须匹配 token 防止误 pop；token 存活 > 30s 触发 `Debug.LogWarning` 泄漏检测；提供 `ForcePopAllBlockers()` 紧急恢复 API。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW

**Control Manifest Rules (Foundation layer)**:
- Required: `IInputService` 接口暴露 `PushBlocker(string token)` / `PopBlocker(string token)` / `IsBlocked { get; }` / `BlockerCount { get; }`；Blocker 栈使用 pre-allocated `List<string>`（初始容量 4）；`PopBlocker(token)` 若 token 不在列表中，输出 `Debug.LogWarning` 但不抛异常；UIPanel 使用 token 格式 `"UIPanel_{ClassName}"`（ADR-011）；Blocker 检查在 Gesture FSM 之前执行（Layer 2 位置）；token 存活 > 30s 输出 `Debug.LogWarning`
- Forbidden: 禁止在非 `IInputService` 实现中直接修改 Blocker 状态；禁止 Blocker 影响 InputFilter 状态（两者独立）
- Guardrail: Blocker check 为 O(1)（`Count > 0` 比较）；push/pop 操作为 O(n)（n = 活跃 Blocker 数，正常 < 4）

---

## Acceptance Criteria

- [x] `IInputService` 接口包含：`PushBlocker(string token)`、`PopBlocker(string token)`、`bool IsBlocked`、`int BlockerCount`、`void ForcePopAllBlockers()`
- [x] `PushBlocker(token)`：将 token 加入 Blocker 列表；同一 token 可重复 push（计数独立，需匹配次数才完全 pop）
- [x] `PopBlocker(token)`：移除列表中第一个匹配 token；若 token 不存在，输出 `Debug.LogWarning("InputBlocker: attempted to pop token '{token}' which is not in stack")` 但不抛异常；列表清空后 `IsBlocked = false`
- [x] `IsBlocked`：`BlockerCount > 0` 时返回 `true`；InputService.Update() 在 Blocker 非空时跳过 FSM 和 dispatch
- [x] `ForcePopAllBlockers()`：清空 Blocker 列表；输出 `Debug.LogWarning("InputBlocker: ForcePopAllBlockers() called — all blockers cleared")`
- [x] token 泄漏检测：每个 token push 时记录 `Time.realtimeSinceStartup`；在 `Update()` 中检查存活时间 > 30s 的 token → `Debug.LogWarning("InputBlocker: token '{token}' has been active for {duration}s, possible leak")`
- [x] `OnApplicationPause(true)` 时：不自动 pop Blocker（Blocker 由调用方控制），但需继续阻断输入（因为 FSM 已在 Story 001 清除状态）
- [x] Blocker 内部 `List<string>` 预分配容量 = 4，无运行时 allocation（正常场景）
- [x] Blocker push 生效时机：同帧生效（同帧 push，同帧 `IsBlocked = true`）

---

## Implementation Notes

来自 ADR-010 Implementation Guidelines：

**数据结构**
```csharp
// 使用 List<string> 而非 Stack<string>，因为 PopBlocker 需要 token 匹配（非 LIFO 顺序 pop）
private readonly List<string> _blockerTokens = new List<string>(4);
private readonly List<(string token, float pushTime)> _blockerTimestamps = new List<(string, float)>(4);
```

**PushBlocker 实现要点**
```csharp
public void PushBlocker(string token)
{
    _blockerTokens.Add(token);
    _blockerTimestamps.Add((token, Time.realtimeSinceStartup));
}
```

**PopBlocker 实现要点**
```csharp
public void PopBlocker(string token)
{
    int idx = _blockerTokens.LastIndexOf(token); // 后入先出语义
    if (idx < 0)
    {
        Debug.LogWarning($"InputBlocker: attempted to pop token '{token}' not in stack");
        return;
    }
    _blockerTokens.RemoveAt(idx);
    _blockerTimestamps.RemoveAt(idx);
}
```

**泄漏检测（在 Update() 中，每秒轮询一次）**
```csharp
private float _lastLeakCheckTime;
private void CheckBlockerLeaks()
{
    if (Time.realtimeSinceStartup - _lastLeakCheckTime < 1f) return;
    _lastLeakCheckTime = Time.realtimeSinceStartup;
    foreach (var (token, pushTime) in _blockerTimestamps)
    {
        float alive = Time.realtimeSinceStartup - pushTime;
        if (alive > 30f)
            Debug.LogWarning($"InputBlocker: token '{token}' active for {alive:F1}s, possible leak");
    }
}
```

**Update() 门控位置**
```csharp
void Update()
{
    SampleRawTouches();          // Layer 1
    CheckBlockerLeaks();
    if (IsBlocked) return;       // Layer 2: Blocker gate — early exit
    RunGestureFSMs();            // Layer 3
    ApplyInputFilter();          // Layer 2: Filter gate
    DispatchGestureEvents();     // Layer 3 output
}
```

**与 UIWindow 系统集成约定（ADR-011）**：
- Popup/Overlay 层 UIWindow open → `PushBlocker("UIPanel_{ClassName}")`
- UIWindow close → `PopBlocker("UIPanel_{ClassName}")`
- Token 格式已在 Control Manifest §1.1 定义，InputService 无需感知具体面板名称

---

## Out of Scope

- [Story 005]: InputFilter 白名单（独立的 Layer 2 门控，与 Blocker 无关）
- [Story 003]: dispatch 层（消费 `IsBlocked` 的结果，不实现 Blocker）

---

## QA Test Cases

### TC-004-01: 单 Blocker push/pop 基本流程
**Given** InputService 初始化，`BlockerCount = 0`  
**When** `PushBlocker("UIPanel_PauseMenu")`，然后执行手势操作，然后 `PopBlocker("UIPanel_PauseMenu")`  
**Then** Push 后：`IsBlocked = true`，`BlockerCount = 1`，手势事件为 0；Pop 后：`IsBlocked = false`，手势恢复正常

### TC-004-02: 多 Blocker 叠加（必须全部 pop 才恢复）
**Given** `BlockerCount = 0`  
**When** `PushBlocker("UIPanel_Settings")`，`PushBlocker("Narrative_Seq01")`，`PopBlocker("Narrative_Seq01")`  
**Then** 第二次 pop 后：`BlockerCount = 1`，`IsBlocked = true`（Settings 仍阻断）；再 `PopBlocker("UIPanel_Settings")` → `IsBlocked = false`

### TC-004-03: 同一 token 重复 push 需重复 pop
**Given** 同一调用方 push 两次同名 token  
**When** `PushBlocker("duplicate")` x2，然后 `PopBlocker("duplicate")` x1  
**Then** `BlockerCount = 1`，仍然 blocked；再 pop 一次才完全恢复

### TC-004-04: Pop 不存在的 token 不抛异常
**Given** Blocker 列表中只有 `"UIPanel_A"`  
**When** `PopBlocker("non-existent-token")`  
**Then** `BlockerCount` 不变（仍 = 1）；`IsBlocked` 不变；输出 `Debug.LogWarning`；不抛异常

### TC-004-05: ForcePopAllBlockers 紧急恢复
**Given** `BlockerCount = 3`（多个 Blocker 已 push）  
**When** `ForcePopAllBlockers()`  
**Then** `BlockerCount = 0`，`IsBlocked = false`；输出 `Debug.LogWarning`；后续手势正常触发

### TC-004-06: token 泄漏检测（30s 告警）
**Given** `PushBlocker("leak-token")`，不调用 pop  
**When** 等待 31s（或在测试中注入时间）  
**Then** 输出 `Debug.LogWarning` 包含 `"leak-token"` 和存活时长信息

### TC-004-07: Blocker 同帧生效
**Given** 监听 `Evt_Gesture_Tap`，当前无 Blocker  
**When** 同一帧内：`PushBlocker("test")` 后模拟 Tap 触摸  
**Then** 该帧不发出 Tap 事件（Blocker 同帧生效）

### TC-004-08: Blocker 不影响 InputFilter 状态
**Given** InputFilter 激活（只允许 Drag），同时 Blocker push  
**When** Blocker pop 后  
**Then** InputFilter 仍激活（只允许 Drag）；Blocker 与 Filter 状态独立

---

## Test Evidence

**Story Type**: Logic  
**Required evidence**: `tests/unit/InputSystem/InputBlockerTests.cs`  
**Status**: [x] 9/9 PASS — `InputBlockerTests.cs`

**关键测试场景**：
```
lock("A") → lock("B") → unlock("B") → still blocked → unlock("A") → unblocked
(对应 Control Manifest §8 中的 PuzzleLockAll 多发送方测试模式，输入层同理)
```

---

## Dependencies

- Depends on: None（独立的栈数据结构，无上游 story 依赖）
- Unlocks: Story 003（dispatch 依赖 `IsBlocked` 门控结果）
