// 该文件由Cursor 自动生成

# Story 005 — Hint-Puzzle Integration (Read-Only Query Interface)

> **Epic**: hint-system
> **Type**: Integration
> **Status**: Ready
> **Priority**: MVP
> **Estimate**: 1d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/hint-system.md` — Interactions: 与 Shadow Puzzle System 的交互（只读查询）|
| **TR-IDs** | TR-hint-007, TR-hint-008, TR-hint-009 |
| **ADR** | ADR-015 (read-only query mandate, 1s polling, tutorial pause/reset) |
| **Engine** | Unity 2022.3.62f2 LTS / TEngine GameEvent |
| **Assembly** | `GameLogic` |

### Control Manifest Rules

- **CM-4.2 (FORBIDDEN)**: **禁止从 Hint System 向 Shadow Puzzle System 写入任何状态**
- **CM-4.2**: 查询频率：1s 轮询间隔（非每帧）
- **CM-4.2**: Tutorial 集成：`Evt_TutorialStepStarted` → 暂停所有计时器；`Evt_TutorialStepCompleted` → 重置计时器为 0
- **CM-2.2**: 在 `Init()` 注册监听，在 `Dispose()` 移除监听
- **CM-2.2**: 场景卸载前移除监听（`Evt_SceneUnloadBegin`）

---

## Acceptance Criteria

1. **AC-001**: `IShadowPuzzle` 接口定义只读查询方法：
   - `GetCurrentMatchScore() → float`
   - `GetAnchorScores() → AnchorScore[]`
   - `GetPuzzlePhase() → PuzzleState`
   - `IsAbsencePuzzle() → bool`
2. **AC-002**: HintSystem 通过 `IShadowPuzzle` 接口查询，不直接引用 `ShadowPuzzleManager` 具体类
3. **AC-003**: 查询频率验证：在 Unity Profiler 中确认 `GetCurrentMatchScore()` 每秒调用一次（±0.1s）
4. **AC-004**: Tutorial 暂停：收到 `Evt_TutorialStepStarted` 时，idleTimer/failCount/stagnationTimer 全部冻结（不累积）
5. **AC-005**: Tutorial 重置：收到 `Evt_TutorialStepCompleted` 时，idleTimer=0, failCount=0, stagnationTimer=0（不延续教学期间值）
6. **AC-006**: 谜题 Complete 后（`Evt_PuzzleComplete`），HintSystem 进入 Idle，所有监听事件不再响应谜题数据
7. **AC-007**: 代码审查验证：HintSystem 代码中不存在 `ShadowPuzzleManager` 的直接字段引用或方法调用

---

## Implementation Notes

### IShadowPuzzle 接口（定义在 GameLogic）

```csharp
/// <summary>
/// Read-only interface for Hint System to query puzzle state.
/// Hint System MUST NOT write through this interface.
/// </summary>
public interface IShadowPuzzle
{
    float GetCurrentMatchScore();
    AnchorScore[] GetAnchorScores();
    PuzzleState GetPuzzlePhase();
    bool IsAbsencePuzzle();
}
```

### HintSystem 初始化

```csharp
public class HintManager
{
    private IShadowPuzzle _puzzleQuery;  // 注入，不 new

    public void Init(IShadowPuzzle puzzleQuery)
    {
        _puzzleQuery = puzzleQuery;
        GameEvent.AddEventListener(EventId.Evt_TutorialStepStarted, OnTutorialStarted);
        GameEvent.AddEventListener(EventId.Evt_TutorialStepCompleted, OnTutorialCompleted);
        GameEvent.AddEventListener(EventId.Evt_PuzzleComplete, OnPuzzleComplete);
        GameEvent.AddEventListener(EventId.Evt_SceneUnloadBegin, OnSceneUnload);
    }

    public void Dispose()
    {
        GameEvent.RemoveEventListener(EventId.Evt_TutorialStepStarted, OnTutorialStarted);
        GameEvent.RemoveEventListener(EventId.Evt_TutorialStepCompleted, OnTutorialCompleted);
        GameEvent.RemoveEventListener(EventId.Evt_PuzzleComplete, OnPuzzleComplete);
        GameEvent.RemoveEventListener(EventId.Evt_SceneUnloadBegin, OnSceneUnload);
    }

    private void OnTutorialStarted(object _) => PauseAllTimers();
    private void OnTutorialCompleted(object _) => ResetAllTimers();
    private void OnSceneUnload(object _) => Dispose();
}
```

---

## Out of Scope

- `IShadowPuzzle` 的具体实现（在 shadow-puzzle epic 的 story-001/002 中）
- Chapter-specific hintDelayOverride 值（在 TbPuzzle 配置中）

---

## QA Test Cases

### TC-001: 只读接口无写操作（代码审查）

**Given**: HintSystem 的全部源文件  
**When**: 搜索 `ShadowPuzzleManager`、`SetMatchScore`、`OnMatchScoreUpdated` 等写操作方法名  
**Then**: 不存在任何对 Shadow Puzzle 写操作的调用

### TC-002: Tutorial 暂停

**Given**: idleTimer=30s，HintSystem 在 Observing 状态  
**When**: 发送 `Evt_TutorialStepStarted`，等待 15s  
**Then**: idleTimer 仍为 30s（计时冻结）；`Evt_HintAvailable` 不被发送

### TC-003: Tutorial 完成重置

**Given**: 教学期间累积了 idleTimer=60s（尽管计时器被暂停但有残留）  
**When**: 发送 `Evt_TutorialStepCompleted`  
**Then**: idleTimer=0, failCount=0（从 0 重新开始计时）

### TC-004: 谜题 Complete 后 HintSystem 静默

**Given**: 谜题已 Complete，HintSystem 进入 Idle  
**When**: 强制调用 `PollPuzzleState()`  
**Then**: 不执行任何 triggerScore 评估，不发送任何提示事件

### TC-005: 监听器无泄漏（事件系统）

**Given**: 谜题开始（HintSystem Init），完成，然后场景卸载  
**When**: 场景卸载后，在 Unity Event System 检查残留监听器  
**Then**: HintSystem 的所有监听器已移除（无事件泄漏）

---

## Test Evidence

- **Integration Test**: `tests/integration/HintSystem_PuzzleIntegration_Test.cs`

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| shadow-puzzle/story-001 | Interface Provider | 实现 `IShadowPuzzle` |
| shadow-puzzle/story-002 | Interface Provider | 提供 `GetAnchorScores()` 数据 |
| story-001 (Trigger Logic) | Consumer | 通过接口读取 matchScore |
| EventId.cs (1800-1899) | Code | Tutorial 事件 ID |
