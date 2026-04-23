// 该文件由Cursor 自动生成

# Story 003 — Sequence Chain (nextSequenceId Chain Logic for Chapter Transitions)

> **Epic**: narrative-event
> **Type**: Integration
> **Status**: Ready
> **Priority**: Vertical Slice
> **Estimate**: 1d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/narrative-event-system.md` — 章末最后谜题的演出衔接优化 |
| **TR-IDs** | TR-narr-007 |
| **ADR** | ADR-016 (Sequence Chain — SP-008 decision) |
| **Finding** | SP-008 — 采用方案 A：Sequence Chain。`nextSequenceId` + `nextSequenceDelay` 字段在 `TbNarrativeSequence` 中新增；末章合并序列通过 chain 零间断衔接 |
| **Engine** | Unity 2022.3.62f2 LTS / UniTask |
| **Assembly** | `GameLogic` |

### Control Manifest Rules

- **CM-4.1 (SP-008)**: `nextSequenceId` + `nextSequenceDelay` 字段驱动 chain；`nextSequenceDelay=0` 实现零间断衔接
- **CM-4.1**: Chain 的下一个序列直接调用 `PlaySequence()`（`delay=0`）或 `DelayedPlaySequence()`（`delay>0`）
- **CM-4.1**: 仅当整个 chain 结束（最后一个序列无 nextSequenceId）时，才发送 `Evt_SequenceComplete`
- **CM-4.1 (FORBIDDEN)**: 禁止硬编码序列 ID 或 chain 逻辑在代码中

---

## Acceptance Criteria

1. **AC-001**: 序列完成时检查 `TbNarrativeSequence.nextSequenceId`；若非空，自动播放下一序列（`nextSequenceDelay <= 0` 时立即，> 0 时等待 delay 后播放）
2. **AC-002**: Chain 中间序列完成时**不发送** `Evt_SequenceComplete`；仅当 chain 末尾（`nextSequenceId` 为空）时发送
3. **AC-003**: Chain 期间 InputBlocker 和 PuzzleLockAll 持续保持（不在中间序列结束时解除）
4. **AC-004**: 整个 chain 结束后，统一弹出 InputBlocker 和 PuzzleUnlock（使用首个序列的 token）
5. **AC-005**: Chain 最大深度防护：若超过 10 个序列链（避免无限循环配置错误），停止 chain 并记录 `Log.Error`
6. **AC-006**: `nextSequenceDelay = 0` 时下一序列在同一 Update 帧继续（零间断）

---

## Implementation Notes

### OnSequenceComplete 扩展（SP-008 decision）

```csharp
private async UniTask OnSequenceComplete(string sequenceId, string rootBlockerToken)
{
    var config = Tables.Instance.TbNarrativeSequence.Get(sequenceId);
    
    if (!string.IsNullOrEmpty(config.NextSequenceId) && _chainDepth < 10)
    {
        _chainDepth++;
        if (config.NextSequenceDelay <= 0)
            await PlaySequenceChained(config.NextSequenceId, rootBlockerToken);
        else
        {
            await UniTask.Delay(TimeSpan.FromSeconds(config.NextSequenceDelay));
            await PlaySequenceChained(config.NextSequenceId, rootBlockerToken);
        }
    }
    else
    {
        // Chain 结束（或 nextSequenceId 为空）
        if (_chainDepth >= 10)
            Log.Error($"[Narrative] Sequence chain depth exceeded 10 at: {sequenceId}");
        
        _chainDepth = 0;
        PopBlocker(rootBlockerToken);
        GameEvent.Send(EventId.Evt_PuzzleUnlock, InteractionLockerId.Narrative);
        GameEvent.Send(EventId.Evt_SequenceComplete, _chainRootSequenceId);
    }
}
```

### Chain 中间序列（不解除 Lock）

```csharp
// PlaySequenceChained：不发 Unlock，不弹 Blocker
// 复用根序列的 InputBlocker token（rootBlockerToken）
```

### 配置示例（SP-008 schema）

```
ch3_puzzle5_complete:
  nextSequenceId = "ch3_chapter_transition"
  nextSequenceDelay = 0.0    ← 零间断

ch3_chapter_transition:
  nextSequenceId = ""         ← chain 终点，发送 Evt_SequenceComplete
```

---

## Out of Scope

- `TbNarrativeSequence` 表结构定义（story-004）
- 章节过渡序列的具体效果实现（story-007）

---

## QA Test Cases

### TC-001: 零延迟 chain

**Given**: seq_A.nextSequenceId="seq_B", nextSequenceDelay=0  
**When**: seq_A 完成  
**Then**: seq_B 在同一帧立即开始，无间隙；`Evt_SequenceComplete` 仅在 seq_B 完成后发送

### TC-002: Chain 中间 Lock 不解除

**Given**: chain: seq_A → seq_B  
**When**: seq_A 完成（chain 中间）  
**Then**: InputBlocker 仍激活，`Evt_PuzzleUnlock` 未发送

### TC-003: Chain 末尾 Lock 解除

**Given**: chain: seq_A → seq_B（seq_B.nextSequenceId=""）  
**When**: seq_B 完成  
**Then**: InputBlocker 弹出，`Evt_PuzzleUnlock("narrative")` 发送，`Evt_SequenceComplete(seq_A)` 发送（根序列 ID）

### TC-004: 超深度 chain 防护

**Given**: 配置了 11 个互相链接的序列（形成超长链）  
**When**: 播放首个序列  
**Then**: 第 10 个序列完成时记录 `Log.Error`，chain 停止，Lock 正确解除

### TC-005: nextSequenceDelay > 0

**Given**: seq_A.nextSequenceDelay=2.0s  
**When**: seq_A 完成  
**Then**: 等待 2s 后 seq_B 开始（在等待期间 InputBlocker 仍激活）

---

## Test Evidence

- **Integration Test**: `tests/integration/NarrativeEngine_Chain_Test.cs`

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| story-001 (Sequence Engine) | Base | chain 逻辑是核心引擎的扩展 |
| story-004 (Luban Config) | Blocking | TbNarrativeSequence 需要 nextSequenceId/nextSequenceDelay 字段 |
| SP-008 findings | Architecture | chain 方案设计依据 |
