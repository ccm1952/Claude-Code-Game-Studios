// 该文件由Cursor 自动生成

# Story 005 — Perfect Match Sequence (PerfectMatch → PuzzleLockAll → Narrative Handoff)

> **Epic**: shadow-puzzle
> **Type**: Integration
> **Status**: Ready
> **Priority**: MVP
> **Estimate**: 2d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/shadow-puzzle-system.md` — States: PerfectMatch entry; Interactions with Narrative System |
| **TR-IDs** | TR-puzzle-005, TR-puzzle-009 |
| **ADR** | ADR-014 (PerfectMatch transition), ADR-006 (GameEvent), ADR-016 (Narrative trigger) |
| **Finding** | SP-006 — PuzzleLockAll 使用 `HashSet<string>` token，Shadow Puzzle 的 token = `InteractionLockerId.ShadowPuzzle = "shadow_puzzle"` |
| **Engine** | Unity 2022.3.62f2 LTS / TEngine GameEvent |
| **Assembly** | `GameLogic` |

### Control Manifest Rules

- **CM-3.2 (SP-006)**: `Evt_PuzzleLockAll` 携带 `string lockerId` payload，Shadow Puzzle 使用 `InteractionLockerId.ShadowPuzzle`
- **CM-3.2 (SP-006)**: 合法 LockerId 定义在 `InteractionLockerId` 常量类中
- **CM-2.2**: 事件 payload 使用具名 struct（`PerfectMatchPayload`），不使用匿名 object
- **CM-2.2**: `Evt_PerfectMatch` (1203), `Evt_PuzzleLockAll` (1206) 均在 `EventId.cs` 中定义
- **CM-2.2**: 禁止同一事件 ID 在 handler 中递归发送（防无限循环）
- **CM-3.3**: PerfectMatch 进入后 matchScore 计算冻结（不可逆）
- **CM-8**: `PuzzleLockAll` 多发送者测试：lock(shadow_puzzle) → lock(narrative) → unlock(narrative) → 仍锁 → unlock(shadow_puzzle) → 解锁

---

## Acceptance Criteria

1. **AC-001**: 谜题进入 PerfectMatch 状态后，在同一帧内发送 `Evt_PerfectMatch(puzzleId)` 和 `Evt_PuzzleLockAll("shadow_puzzle")`
2. **AC-002**: Narrative Event System 收到 `Evt_PerfectMatch` 后开始序列播放（集成验证）
3. **AC-003**: Narrative System 演出结束后发送 `Evt_PuzzleUnlock("narrative")`，Shadow Puzzle 发送 `Evt_PuzzleUnlock("shadow_puzzle")`；两者都解锁后物件才恢复可交互
4. **AC-004**: Snap 动画（PerfectMatch 吸附）在 PerfectMatch 事件发出后 ≤ 1 帧开始播放
5. **AC-005**: 吸附动画完成回调（`OnSnapAnimationComplete()`）触发后，状态机推进到 Complete
6. **AC-006**: 多发送者 token 测试：Shadow Puzzle lock + Narrative lock 同时存在时，Narrative 先解锁不会导致物件提前解锁
7. **AC-007**: Complete 状态触发 `Evt_PuzzleComplete(puzzleId)` 广播

---

## Implementation Notes

### 事件流程（SP-006 + ADR-014）

```
matchScore ≥ perfectMatchThreshold（未冻结，非保护期）
    ↓
[PuzzleStateMachine] 冻结 matchScore
    ↓ 同一帧
[GameEvent.Send] Evt_PerfectMatch{ puzzleId }
[GameEvent.Send] Evt_PuzzleLockAll{ token = "shadow_puzzle" }
    ↓
[NarrativeSequenceEngine 接收] → 查表 → 发送 Evt_PuzzleLockAll{ "narrative" }
                                       → push InputBlocker("narrative_seq_{id}")
                                       → 开始播放序列（含 ObjectSnap 吸附效果）
    ↓ (序列完成)
[NarrativeSequenceEngine] → GameEvent.Send(Evt_PuzzleUnlock, "narrative")
                          → pop InputBlocker
    ↓
[PuzzleStateMachine] OnSnapAnimationComplete() → 状态转 Complete
[PuzzleStateMachine] → GameEvent.Send(Evt_PuzzleUnlock, "shadow_puzzle")
[PuzzleStateMachine] → GameEvent.Send(Evt_PuzzleComplete, { puzzleId, chapterId })
```

### SP-006 Lock Manager 验证代码（集成测试）

```csharp
// 测试用例验证：
lockManager.PushLock("shadow_puzzle");
lockManager.PushLock("narrative");
Assert.IsTrue(lockManager.IsLocked);       // 两个 token，仍锁

lockManager.PopLock("narrative");
Assert.IsTrue(lockManager.IsLocked);       // shadow_puzzle 仍在，仍锁

lockManager.PopLock("shadow_puzzle");
Assert.IsFalse(lockManager.IsLocked);      // 全部解锁
```

---

## Out of Scope

- Narrative 演出的具体实现（属于 narrative-event epic）
- 物件 Snap 动画的渲染细节（属于 object-interaction epic）
- AbsenceAccepted 的专用演出路径（story-006）

---

## QA Test Cases

### TC-001: PerfectMatch 事件发送时序

**Given**: 谜题在 Active 状态，matchScore 达到 perfectMatchThreshold  
**When**: `OnMatchScoreUpdated(perfectMatchThreshold + 0.01f)` 被调用  
**Then**: 同一帧内 `Evt_PerfectMatch` 和 `Evt_PuzzleLockAll("shadow_puzzle")` 均被发送

### TC-002: 跨系统 Lock 协议（SP-006）

**Given**: Shadow Puzzle 发送 `Evt_PuzzleLockAll("shadow_puzzle")`，随后 Narrative 发送 `Evt_PuzzleLockAll("narrative")`  
**When**: Narrative 发送 `Evt_PuzzleUnlock("narrative")`  
**Then**: 物件仍处于 Locked 状态（shadow_puzzle token 仍在 HashSet 中）

**When**: Shadow Puzzle 发送 `Evt_PuzzleUnlock("shadow_puzzle")`  
**Then**: 物件进入 Unlocked 状态

### TC-003: Complete 状态推进

**Given**: 谜题在 PerfectMatch 状态  
**When**: `OnSnapAnimationComplete()` 被调用  
**Then**: 状态机转换到 Complete，`Evt_PuzzleComplete` 被发送，payload 包含正确的 puzzleId 和 chapterId

### TC-004: 未知 Token 解锁警告

**Given**: `InteractionLockManager` 当前只有 "shadow_puzzle" token  
**When**: 调用 `PopLock("unknown_token")`  
**Then**: 返回 no-op，`Debug.LogWarning` 被触发，`IsLocked` 仍为 true

---

## Test Evidence

- **Integration Test**: `tests/integration/PerfectMatch_Narrative_Handoff_Test.cs`
- **Multi-lock Test**: 验证 SP-006 多发送者场景的专项用例

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| story-001 (StateMachine) | Blocking | PerfectMatch 状态转换逻辑 |
| narrative-event/story-001 | Integration | NarrativeSequenceEngine 接收事件 |
| object-interaction epic | Integration | `Evt_PuzzleLockAll` 接收方 |
| SP-006 findings | Architecture | Token-based lock 实现 |
| EventId.cs (1200-1299) | Code | 所有事件 ID 预定义 |
