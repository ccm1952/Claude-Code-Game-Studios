// 该文件由Cursor 自动生成

# Story 008 — Puzzle Reset (Reset to Idle on Player Request)

> **Epic**: shadow-puzzle
> **Type**: Logic
> **Status**: Ready
> **Priority**: MVP
> **Estimate**: 1d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/shadow-puzzle-system.md` — States: Idle entry condition; "每次谜题重新进入时，所有提示状态重置" |
| **TR-IDs** | TR-puzzle-005, TR-puzzle-014 |
| **ADR** | ADR-014 (State machine lifecycle), ADR-008 (Save System — Active state resume after app restart) |
| **Engine** | Unity 2022.3.62f2 LTS / TEngine FsmModule |
| **Assembly** | `GameLogic` |

### Control Manifest Rules

- **CM-3.3**: Reset 仅在 Active/NearMatch 状态有效（PerfectMatch/AbsenceAccepted/Complete 不可 Reset）
- **CM-3.3**: Reset 后回到 Idle 状态（不是 Locked），物件恢复初始位置
- **CM-3.1**: 平滑缓冲区在 Reset 时必须清空（`ResetSmoothing()`），防止旧分数污染
- **CM-2.2**: 广播 `Evt_PuzzleStateChanged` 事件
- **CM-2.6**: 存档系统需要在 Reset 后更新谜题状态为 Idle

---

## Acceptance Criteria

1. **AC-001**: 玩家在 Active 或 NearMatch 状态按下 Reset 时，状态机回到 Idle，物件恢复初始配置位置
2. **AC-002**: PerfectMatch / AbsenceAccepted / Complete / Locked 状态调用 Reset 时为 no-op（状态不变，无错误）
3. **AC-003**: Reset 时 matchScore 时间平滑缓冲区清空（`ResetSmoothing()`），下一帧从 0 重新计算
4. **AC-004**: Reset 后广播 `Evt_PuzzleStateChanged(Idle)` 通知其他系统（Hint System 响应后重置自身计时器）
5. **AC-005**: 物件归位动画：使用 DOTween EaseOutQuad，0.3s 内恢复到初始位置
6. **AC-006**: App 重启时，Active 状态的谜题正确恢复（从 `IChapterProgress` 加载 puzzleState=Active，物件从存档位置恢复，不执行 Reset）
7. **AC-007**: 教学（Tutorial）期间 Reset 功能可用（不受 grace period 影响）

---

## Implementation Notes

### Reset 实现

```csharp
public void ResetPuzzle()
{
    // Guard：仅 Active/NearMatch 可 Reset
    if (CurrentState != PuzzleState.Active && CurrentState != PuzzleState.NearMatch)
        return;

    // 1. 物件归位
    foreach (var obj in _puzzleObjects)
        obj.AnimateToInitialPosition(0.3f, Ease.OutQuad);

    // 2. 重置 matchScore 平滑
    _matchCalculator.ResetSmoothing();

    // 3. FSM 回到 Idle
    _fsmModule.ChangeState<IdleState>();

    // 4. 广播状态变更
    GameEvent.Send(EventId.Evt_PuzzleStateChanged, new PuzzleStateChangedPayload
    {
        PuzzleId = _puzzleId,
        NewState = PuzzleState.Idle
    });
}
```

### 存档集成（ADR-008）

- Active 状态下：`IChapterProgress.PuzzleState = Active`，物件位置持久化
- App 重启后，从存档读取 state=Active + 物件位置，直接恢复到 Active 状态（跳过 Idle→Active 的触发逻辑）
- Reset 后：存档更新为 `PuzzleState = Idle`，物件位置清空（1s debounce）

---

## Out of Scope

- UI Reset 按钮的具体样式和位置（属于 UI System）
- Hint System 响应 Reset 事件的计时器重置（属于 hint-system/story-001）
- 存档系统的具体实现（属于 chapter-state epic）

---

## QA Test Cases

### TC-001: Active 状态 Reset

**Given**: 谜题在 Active 状态，物件已被移动到非初始位置  
**When**: 调用 `ResetPuzzle()`  
**Then**: 状态变为 Idle，物件以 0.3s 动画归位，matchScore 平滑缓冲清空，`Evt_PuzzleStateChanged(Idle)` 发送

### TC-002: NearMatch 状态 Reset

**Given**: 谜题在 NearMatch 状态（发光效果激活）  
**When**: 调用 `ResetPuzzle()`  
**Then**: 状态变为 Idle，NearMatch 发光效果消退，物件归位

### TC-003: PerfectMatch 状态 Reset 无效

**Given**: 谜题已达到 PerfectMatch  
**When**: 调用 `ResetPuzzle()`  
**Then**: 状态仍为 PerfectMatch，无任何效果，无错误/异常

### TC-004: 平滑缓冲清空

**Given**: matchScore 已稳定在 0.80，smoothedScore ≈ 0.80  
**When**: 调用 `ResetPuzzle()`，然后立即读取 `GetSmoothedScore()`  
**Then**: 返回值 ≤ 0.05（缓冲已清空，从 0 重新开始）

### TC-005: 存档恢复（不执行 Reset）

**Given**: 存档中 puzzleState=Active，物件在非初始位置  
**When**: App 重启，从存档加载谜题状态  
**Then**: 谜题直接进入 Active 状态，物件在存档位置（未归位），不触发 Reset 流程

---

## Test Evidence

- **Unit Test**: `tests/unit/ShadowPuzzle_Reset_Test.cs`

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| story-001 (StateMachine) | Blocking | Reset 是 FSM 的一个操作 |
| story-002 (MatchScore) | Integration | `ResetSmoothing()` 需要 MatchCalculator 配合 |
| ADR-008 (Save System) | Integration | Reset 后更新存档状态 |
| hint-system/story-001 | Event | 响应 `Evt_PuzzleStateChanged(Idle)` 重置提示计时器 |
| DOTween | Library | 物件归位动画 |
