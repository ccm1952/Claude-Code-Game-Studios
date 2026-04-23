// 该文件由Cursor 自动生成

# Story 005 — Puzzle to Narrative (PerfectMatchEvent → Lookup Sequence → Play)

> **Epic**: narrative-event
> **Type**: Integration
> **Status**: Ready
> **Priority**: Vertical Slice
> **Estimate**: 1d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/narrative-event-system.md` — 记忆重现演出流程（典型）|
| **TR-IDs** | TR-narr-004, TR-narr-012 |
| **ADR** | ADR-016 (trigger event flow), ADR-006 (GameEvent protocol) |
| **Finding** | SP-006 — Narrative System 是 `Evt_PuzzleLockAll` 的第二个合法发送者 |
| **Engine** | Unity 2022.3.62f2 LTS / TEngine GameEvent |
| **Assembly** | `GameLogic` |

### Control Manifest Rules

- **CM-4.1**: 响应 `Evt_PerfectMatch` 触发记忆重现；响应 `Evt_AbsenceAccepted` 触发缺席序列
- **CM-4.1**: PuzzleLock + InputBlocker 在序列开始前激活（story-001 封装）
- **CM-2.2**: 序列完成后发送 `Evt_SequenceComplete`（EventId 1500 范围）
- **CM-2.2**: `Init()` 中注册监听，`Dispose()` 中移除
- **CM-4.1**: 谜题无映射配置 → 直接发送 `Evt_PuzzleUnlock`（跳过演出）

---

## Acceptance Criteria

1. **AC-001**: `NarrativeSequenceEngine` 监听 `Evt_PerfectMatch`；收到后查询 `TbPuzzleNarrativeMap.Get(puzzleId)` 获取 sequenceId
2. **AC-002**: 若存在映射，调用 `TryEnqueue(sequenceId)` 启动序列（含 PuzzleLock + InputBlocker）
3. **AC-003**: 若无映射（谜题未配置演出）→ 直接发送 `Evt_PuzzleUnlock("narrative")`，谜题正常推进 Complete
4. **AC-004**: 同时监听 `Evt_AbsenceAccepted`，查询 absence 专用序列（来自 `TbPuzzleNarrativeMap` 或独立 `TbAbsenceSequenceMap`）
5. **AC-005**: `Evt_PerfectMatch` 收到后 ≤ 1 帧开始执行序列（TR-narr-004）
6. **AC-006**: `Evt_SequenceComplete` 发送后，Shadow Puzzle StateMachine 的 `OnSnapAnimationComplete()` 被调用（验证下游正确响应）

---

## Implementation Notes

### 事件监听初始化

```csharp
public void Init()
{
    GameEvent.AddEventListener<PerfectMatchPayload>(EventId.Evt_PerfectMatch, OnPerfectMatch);
    GameEvent.AddEventListener<AbsenceAcceptedPayload>(EventId.Evt_AbsenceAccepted, OnAbsenceAccepted);
}

private void OnPerfectMatch(PerfectMatchPayload payload)
{
    var map = Tables.Instance.TbPuzzleNarrativeMap.Get(payload.PuzzleId);
    if (map == null)
    {
        // 无配置，直接解锁（开发阶段谜题可能未配置演出）
        Log.Warning($"[Narrative] No sequence for puzzle {payload.PuzzleId}, skipping.");
        GameEvent.Send(EventId.Evt_PuzzleUnlock, InteractionLockerId.Narrative);
        return;
    }
    TryEnqueue(map.SequenceId);
}
```

### Pre-warm 资源（ADR-016 建议）

```csharp
// 谜题进入 Active 状态时预加载序列资源
private void OnPuzzleActive(PuzzleStateChangedPayload payload)
{
    if (payload.NewState == PuzzleState.Active)
    {
        var map = Tables.Instance.TbPuzzleNarrativeMap.Get(payload.PuzzleId);
        if (map != null)
            PreWarmSequenceAssets(map.SequenceId);
    }
}
```

---

## Out of Scope

- 序列的具体播放实现（story-001）
- 缺席序列的内容（story-002 中 ShadowFade 效果）
- 章节 Complete 触发（story-007）

---

## QA Test Cases

### TC-001: PerfectMatch → 序列启动（≤ 1 帧）

**Given**: puzzle_id=1001 在 TbPuzzleNarrativeMap 中有映射  
**When**: `Evt_PerfectMatch{puzzleId=1001}` 发送  
**Then**: 在同一帧或下一帧，`NarrativeSequenceEngine.PlaySequence` 被调用，`Evt_PuzzleLockAll("narrative")` 发送

### TC-002: 无映射谜题不卡死

**Given**: puzzle_id=9999 在 TbPuzzleNarrativeMap 中无记录  
**When**: `Evt_PerfectMatch{puzzleId=9999}` 发送  
**Then**: `Evt_PuzzleUnlock("narrative")` 立即发送，`Log.Warning` 记录，谜题正常推进 Complete

### TC-003: AbsenceAccepted 使用缺席序列

**Given**: puzzle_id=5001（Ch.5 缺席谜题）在映射中有 absence 专用序列  
**When**: `Evt_AbsenceAccepted{puzzleId=5001}` 发送  
**Then**: 缺席专用序列（含 ShadowFade, 冷色温）被播放

### TC-004: 序列完成后 StateMachine 推进

**Given**: 序列 "ch1_puzzle1_complete" 播放完成  
**When**: `Evt_SequenceComplete` 发送  
**Then**: Shadow Puzzle StateMachine 收到信号，`OnSnapAnimationComplete()` 被调用，状态推进到 Complete

---

## Test Evidence

- **Integration Test**: `tests/integration/PerfectMatch_Narrative_Trigger_Test.cs`（与 shadow-puzzle story-005 联合测试）

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| story-001 (Sequence Engine) | Base | `TryEnqueue()` API |
| story-004 (Luban Config) | Blocking | TbPuzzleNarrativeMap 表 |
| shadow-puzzle/story-005 | Integration | Evt_PerfectMatch 发送方 |
| shadow-puzzle/story-006 | Integration | Evt_AbsenceAccepted 发送方 |
