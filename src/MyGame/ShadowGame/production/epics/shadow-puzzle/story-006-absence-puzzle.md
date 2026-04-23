// 该文件由Cursor 自动生成

# Story 006 — Absence Puzzle (AbsenceAccepted State — Chapter 5)

> **Epic**: shadow-puzzle
> **Type**: Logic
> **Status**: Ready
> **Priority**: MVP
> **Estimate**: 2d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/shadow-puzzle-system.md` — AbsenceAccepted state, Ch.5 absence puzzles |
| **TR-IDs** | TR-puzzle-006 |
| **ADR** | ADR-014 (Absence Puzzle Variant — AbsenceAccepted state, 5s idle timer) |
| **Engine** | Unity 2022.3.62f2 LTS / TEngine TimerModule |
| **Assembly** | `GameLogic` |

### Control Manifest Rules

- **CM-3.3**: Absence puzzle 通过 `TbPuzzle.isAbsencePuzzle = true` + `maxCompletionScore < 1.0` 标记
- **CM-3.3**: AbsenceAccepted 转换 guard：`isAbsencePuzzle && matchScore ≥ maxCompletionScore && idleTime ≥ absenceAcceptDelay && !isInGracePeriod`
- **CM-3.3**: AbsenceAccepted 为不可逆终态——进入后 matchScore 计算冻结
- **CM-3.3**: 由于 `maxCompletionScore < perfectMatchThreshold`，标准 PerfectMatch 路径永远不触发
- **CM-2.2**: `Evt_AbsenceAccepted(puzzleId)` 使用 EventId 1204
- **CM-6**: absenceAcceptDelay 和 maxCompletionScore 从 Luban `TbPuzzle` 读取
- **CM-1.4**: 不使用 Coroutine 实现定时器，使用 TEngine `GameModule.Timer`

---

## Acceptance Criteria

1. **AC-001**: `isAbsencePuzzle=true` 谜题中，matchScore 达到 `maxCompletionScore`（如 0.65）后等待 5s 无操作，触发 AbsenceAccepted
2. **AC-002**: 5s 等待期间玩家有任何交互（拖拽/旋转）会重置 `absenceIdleTimer` 为 0
3. **AC-003**: AbsenceAccepted 是不可逆的——触发后移动物件不会取消（matchScore 已冻结）
4. **AC-004**: 标准 PerfectMatch 路径（threshold=0.85）在 absence 谜题中**永远不触发**（因 maxCompletionScore < perfectMatchThreshold）
5. **AC-005**: AbsenceAccepted 进入时广播 `Evt_AbsenceAccepted(puzzleId)` 和 `Evt_PuzzleLockAll("shadow_puzzle")`
6. **AC-006**: `absenceAcceptDelay` 从 `TbPuzzle.absenceAcceptDelay` 读取（默认 5s，范围 3-8s），不硬编码
7. **AC-007**: `maxCompletionScore` 在 absence 谜题中为 0.60-0.70，标准谜题中该字段不影响判定
8. **AC-008**: Tutorial Grace Period 同样阻断 AbsenceAccepted 转换（与 PerfectMatch 行为一致）

---

## Implementation Notes

### Absence Idle Detection 算法（ADR-014）

```csharp
// 在 PuzzleStateMachine.OnUpdate() 中，当 isAbsencePuzzle && (state == Active || state == NearMatch)
if (_playerInteractedThisFrame)
{
    _absenceIdleTimer = 0f;
}
else if (_matchScore >= _config.MaxCompletionScore)
{
    _absenceIdleTimer += Time.deltaTime;
    if (_absenceIdleTimer >= _config.AbsenceAcceptDelay && !IsInGracePeriod)
    {
        TransitionTo(PuzzleState.AbsenceAccepted);
        FreezeMatchScore();
        GameEvent.Send(EventId.Evt_AbsenceAccepted, puzzleId);
        GameEvent.Send(EventId.Evt_PuzzleLockAll, InteractionLockerId.ShadowPuzzle);
    }
}
else
{
    _absenceIdleTimer = 0f;  // 分数下降，重置计时器
}
```

### 关键设计注意点

1. `absenceIdleTimer` 在**分数低于 maxCompletionScore 时也重置**——确保玩家没有把分数稳定在目标分数才开始计时
2. `_playerInteractedThisFrame` 标志由 `OnPlayerInteraction()` 在每帧重置前设置（响应 `Evt_ObjectOperated`）
3. 标准 PerfectMatch 的 guard：`!isAbsencePuzzle && matchScore >= perfectMatchThreshold`——absence 谜题永远不会进这里
4. AbsenceAccepted 的演出（ShadowFade、冷色温）由 Narrative System 通过 `Evt_AbsenceAccepted` 驱动，不在此 story 实现

---

## Out of Scope

- 缺席谜题的 Narrative 演出（narrative-event/story-002）
- 缺席谜题的 Hint 特殊文案（hint-system/story-006）
- Ch.5 难度参数（已在 story-004 中通过 TbPuzzle 覆盖）

---

## QA Test Cases

### TC-001: 正常 AbsenceAccepted 触发

**Given**: isAbsencePuzzle=true, maxCompletionScore=0.65, absenceAcceptDelay=5.0s  
**When**: matchScore 上升到 0.70，玩家停止操作 5.1s  
**Then**: 进入 AbsenceAccepted，`Evt_AbsenceAccepted` 被发送

### TC-002: 操作中断重置计时器

**Given**: 谜题在 Active 状态，matchScore=0.70，已等待 4.5s  
**When**: 玩家在第 4.5s 操作了一次物件  
**Then**: absenceIdleTimer 重置为 0，需再等待 5s 才能触发

### TC-003: 分数下降重置计时器

**Given**: matchScore=0.70，计时 3s  
**When**: matchScore 降至 0.55（低于 maxCompletionScore）  
**Then**: absenceIdleTimer 重置为 0

### TC-004: Absence 谜题不触发标准 PerfectMatch

**Given**: isAbsencePuzzle=true, maxCompletionScore=0.65, perfectMatchThreshold=0.85  
**When**: matchScore 达到 0.90（理论上高于 perfectMatchThreshold）  
**Then**: 不触发 PerfectMatch（guard `!isAbsencePuzzle` 阻断）；absenceIdleTimer 正常运行

### TC-005: Grace Period 阻断

**Given**: absence 谜题，matchScore=0.70，grace period 激活  
**When**: 玩家停止操作 6s（超过 absenceAcceptDelay）  
**Then**: 不触发 AbsenceAccepted（IsInGracePeriod=true 阻断）

### TC-006: AbsenceAccepted 不可逆

**Given**: 谜题已进入 AbsenceAccepted  
**When**: 玩家移动物件（matchScore 变为 0.3）  
**Then**: 状态仍为 AbsenceAccepted，matchScore 保持冻结值

---

## Test Evidence

- **Unit Test**: `tests/unit/ShadowPuzzle_AbsencePuzzle_Test.cs`

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| story-001 (StateMachine) | Base | AbsenceAccepted 是 FSM 第 6 个状态 |
| story-004 (Luban Config) | Blocking | maxCompletionScore, absenceAcceptDelay, isAbsencePuzzle 来自 TbPuzzle |
| narrative-event/story-002 | Integration | `Evt_AbsenceAccepted` 触发缺席演出 |
| hint-system/story-006 | Feature | 缺席谜题专用提示文案 |
