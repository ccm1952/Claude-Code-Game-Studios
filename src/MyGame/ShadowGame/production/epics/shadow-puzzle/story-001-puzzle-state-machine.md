// 该文件由Cursor 自动生成

# Story 001 — Puzzle State Machine

> **Epic**: shadow-puzzle
> **Type**: Logic
> **Status**: Ready
> **Priority**: MVP
> **Estimate**: 2d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/shadow-puzzle-system.md` — States and Transitions |
| **TR-IDs** | TR-puzzle-005, TR-puzzle-008, TR-puzzle-013, TR-puzzle-014 |
| **ADR** | ADR-014 (Puzzle State Machine & Absence Puzzle Variant) |
| **Engine** | Unity 2022.3.62f2 LTS / TEngine FsmModule / HybridCLR GameLogic assembly |
| **Assembly** | `GameLogic` — `Assets/GameScripts/HotFix/GameLogic/GameLogic/ShadowPuzzle/` |

### Control Manifest Rules

- **CM-3.3**: 7状态 FSM: Locked → Idle → Active → NearMatch → PerfectMatch → AbsenceAccepted → Complete
- **CM-3.3**: PerfectMatch 和 AbsenceAccepted 为不可逆终态，matchScore 计算冻结
- **CM-3.3**: NearMatch 进入/退出滞后阈值 0.40/0.35（可按谜题配置）
- **CM-3.3**: Tutorial 保护期（3s）阻断 PerfectMatch/AbsenceAccepted 转换
- **CM-3.3**: 所有阈值来自 Luban `TbPuzzle`，不可硬编码
- **CM-2.2**: 所有状态变更事件通过 `GameEvent.Send` 广播（EventId 1200-1299 范围）
- **CM-1.4**: 不使用 `ModuleSystem.GetModule<T>()`，用 `GameModule.Fsm`
- **CM-1.4**: 不使用 Coroutine，异步操作用 UniTask
- **CM-6**: 所有配置值通过 `Tables.Instance.TbPuzzle.Get(puzzleId)` 读取

---

## Acceptance Criteria

1. **AC-001**: FSM 包含全部 7 个状态（Locked/Idle/Active/NearMatch/PerfectMatch/AbsenceAccepted/Complete），所有 10 条状态转换可在自动化测试中独立触发
2. **AC-002**: Locked → Idle 仅在前序谜题 Complete 事件后触发
3. **AC-003**: Idle → Active 在玩家首次操作任意可操作物件时触发
4. **AC-004**: Active ↔ NearMatch 滞后边界正确——matchScore=0.42 进入 NearMatch，降至 0.33 退出回 Active
5. **AC-005**: PerfectMatch 为不可逆——进入后移动物件不改变冻结的 matchScore
6. **AC-006**: Tutorial Grace Period：`OnTutorialCompleted()` 后 3s 内不触发 PerfectMatch/AbsenceAccepted
7. **AC-007**: PuzzleState 枚举可直接序列化到 `IChapterProgress`（存档兼容）
8. **AC-008**: 所有阈值（nearMatchThreshold, perfectMatchThreshold, tutorialGracePeriod）来自 Luban `TbPuzzle`，无硬编码浮点数

---

## Implementation Notes

### 接口定义（ADR-014）

```csharp
public enum PuzzleState
{
    Locked, Idle, Active, NearMatch, PerfectMatch, AbsenceAccepted, Complete
}

public interface IPuzzleStateMachine
{
    PuzzleState CurrentState { get; }
    float MatchScore { get; }
    bool IsInGracePeriod { get; }
    bool IsAbsencePuzzle { get; }

    void OnMatchScoreUpdated(float newScore);
    void OnPlayerInteraction();
    void OnTutorialCompleted();
    void OnSnapAnimationComplete();
    void OnAbsenceSequenceComplete();
}
```

### 关键实现点

1. 使用 TEngine `GameModule.Fsm` 创建每谜题独立 FSM 实例
2. 状态转换 Guard 在各 State 的 `OnUpdate` 里实现（因 FsmModule 不支持条件守卫）
3. NearMatch 滞后：entry=nearMatchThreshold, exit=nearMatchThreshold-0.05（均从 TbPuzzle 读取）
4. PerfectMatch/AbsenceAccepted 进入时：`_calculationFrozen = true`；之后 `OnMatchScoreUpdated` 为 no-op
5. 状态变更时广播 `GameEvent.Send(EventId.Evt_PuzzleStateChanged, payload)` (ID 1200 范围)
6. PerfectMatch 进入时同时发送 `Evt_PuzzleLockAll`（token = `InteractionLockerId.ShadowPuzzle`）

### GameEvent IDs（EventId 1200-1299）

```csharp
// EventId.cs 中定义
public const int Evt_PuzzleStateChanged  = 1200;
public const int Evt_NearMatchEnter      = 1201;
public const int Evt_NearMatchExit       = 1202;
public const int Evt_PerfectMatch        = 1203;
public const int Evt_AbsenceAccepted     = 1204;
public const int Evt_PuzzleComplete      = 1205;
public const int Evt_PuzzleLockAll       = 1206;
public const int Evt_PuzzleUnlock        = 1207;
```

---

## Out of Scope

- matchScore 的实际计算逻辑（story-002）
- AbsenceAccepted 状态的详细判定（story-006）
- 视觉/音频反馈（story-007）
- PerfectMatch → Narrative 移交序列（story-005）

---

## QA Test Cases

### TC-001: 全状态覆盖

**Given**: 一个 `PuzzleStateMachine` 实例，isAbsencePuzzle=false，所有阈值来自 mock TbPuzzle  
**When**: 依序调用状态转换触发器  
**Then**: 经路径 Locked→Idle→Active→NearMatch→PerfectMatch→Complete，CurrentState 在每步正确

### TC-002: NearMatch 滞后防抖

**Given**: 谜题在 Active 状态，nearMatchThreshold=0.40，exit=0.35  
**When**: matchScore 从 0.38 上升到 0.41（进入 NearMatch），再下降到 0.37（未出）、再下降到 0.34（退出）  
**Then**: 仅在 0.41 时进入 NearMatch，在 0.34 时退出；0.37 时不触发退出

### TC-003: PerfectMatch 不可逆

**Given**: 谜题刚进入 PerfectMatch 状态  
**When**: 调用 `OnMatchScoreUpdated(0.1f)`（模拟物件被移走）  
**Then**: CurrentState 仍为 PerfectMatch，MatchScore 返回冻结值 ≥ perfectMatchThreshold

### TC-004: Tutorial Grace Period

**Given**: 谜题在 Active 状态，匹配度为 0.90（高于阈值）  
**When**: `OnTutorialCompleted()` 后 1.5s 再调用 `OnMatchScoreUpdated(0.90f)`  
**Then**: IsInGracePeriod=true，状态不转换为 PerfectMatch

**When**: Grace Period 结束（3.0s 后）再调用 `OnMatchScoreUpdated(0.90f)`  
**Then**: 正常转换为 PerfectMatch

### TC-005: Luban 配置读取

**Given**: TbPuzzle 中 puzzle_001 配置 nearMatchThreshold=0.35  
**When**: 创建该谜题的 StateMachine  
**Then**: 进入 NearMatch 的阈值为 0.35（而非默认 0.40）

---

## Test Evidence

- **Unit Test**: `tests/unit/ShadowPuzzle_StateMachine_Test.cs`
- **Coverage**: 7 states × 10 transitions × 3 edge cases (grace period, freeze, hysteresis)

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| ADR-012 (ShadowMatchCalculator) | Story-002 | 提供 matchScore 驱动状态转换 |
| ADR-014 (FSM design) | Architecture | 状态机结构定义 |
| Luban TbPuzzle | Config | nearMatchThreshold, perfectMatchThreshold, tutorialGracePeriod |
| EventId.cs | Code | 需要先定义 1200-1299 范围的事件 ID |
| IChapterProgress | Interface | 序列化接口，story 实现前需确认字段 |
