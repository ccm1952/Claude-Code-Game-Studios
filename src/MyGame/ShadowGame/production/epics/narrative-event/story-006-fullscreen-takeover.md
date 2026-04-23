// 该文件由Cursor 自动生成

# Story 006 — Full-Screen Narrative Mode (Input Blocking + PuzzleLockAll During Sequences)

> **Epic**: narrative-event
> **Type**: Integration
> **Status**: Ready
> **Priority**: Vertical Slice
> **Estimate**: 1d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/narrative-event-system.md` — 触发条件 3/4/5: PuzzleLockAll, InputBlocker |
| **TR-IDs** | TR-narr-004 |
| **ADR** | ADR-016 (token-based puzzle lock + InputBlocker integration), SP-006 (PuzzleLockAll token) |
| **Engine** | Unity 2022.3.62f2 LTS / TEngine InputModule（ADR-010 InputBlocker） |
| **Assembly** | `GameLogic` |

### Control Manifest Rules

- **CM-2.8**: InputBlocker 是栈式结构：`PushBlocker(token)` / `PopBlocker(token)`；非空栈 = 所有输入被丢弃
- **CM-3.2 (SP-006)**: `Evt_PuzzleLockAll` payload 携带 `string lockerId`；Narrative 使用 `InteractionLockerId.Narrative = "narrative"`
- **CM-3.2**: `Evt_PuzzleUnlock` 与 `Evt_PuzzleLockAll` 使用相同 token
- **CM-8**: 每个 `PushBlocker` 必须有对应的 `PopBlocker`（测试验证）
- **CM-2.8**: Blocker token 泄漏检测：push 存活 > 30s → `Debug.LogWarning`
- **CM-2.2**: 场景卸载时（`Evt_SceneUnloadBegin`）强制清除 lock token set

---

## Acceptance Criteria

1. **AC-001**: 序列开始时：`InputService.PushBlocker("narrative_seq_{sequenceId}")` + `GameEvent.Send(Evt_PuzzleLockAll, "narrative")`
2. **AC-002**: 序列期间：玩家触摸/手势输入被完全阻断（验证 InputBlocker 生效）
3. **AC-003**: 序列结束时：`InputService.PopBlocker("narrative_seq_{sequenceId}")` + `GameEvent.Send(Evt_PuzzleUnlock, "narrative")`
4. **AC-004**: Chain 中间序列不解除 Blocker（复用根序列 token，story-003 集成）
5. **AC-005**: 多发送者 lock 测试：Shadow Puzzle lock("shadow_puzzle") + Narrative lock("narrative") 同时存在，分别解锁不提前解除
6. **AC-006**: 序列异常中止（如 UniTask cancel）时，Blocker 和 Lock 仍被正确清理（try/finally 保证）
7. **AC-007**: 场景卸载（`Evt_SceneUnloadBegin`）时，若序列仍在播放，强制取消并清理 Blocker/Lock

---

## Implementation Notes

### try/finally 保证清理

```csharp
public async UniTask PlaySequence(string sequenceId)
{
    string blockerToken = $"narrative_seq_{sequenceId}";
    InputService.PushBlocker(blockerToken);
    GameEvent.Send(EventId.Evt_PuzzleLockAll, InteractionLockerId.Narrative);
    
    try
    {
        // ... 序列播放逻辑 ...
    }
    finally
    {
        // 即使 UniTask 被取消也执行
        InputService.PopBlocker(blockerToken);
        GameEvent.Send(EventId.Evt_PuzzleUnlock, InteractionLockerId.Narrative);
    }
}
```

### 场景卸载强制取消

```csharp
private CancellationTokenSource _cts;

private void OnSceneUnload(object _)
{
    _cts?.Cancel();  // 触发 UniTask 取消，finally 块执行清理
    _cts = null;
}
```

### InputBlocker 泄漏防护（来自 ADR-010）

- Token 格式：`"narrative_seq_{sequenceId}"`（唯一，防止误弹）
- 框架自动检测：token push 存活 > 30s 时 `Debug.LogWarning`

---

## Out of Scope

- 具体 UI 遮罩层（ScreenFadeEffect — story-002 中实现）
- 章节过渡的全屏黑边电影模式（story-007）
- InputBlocker 的底层实现（属于 input-system epic）

---

## QA Test Cases

### TC-001: 输入被阻断验证

**Setup**: 序列播放中，向输入系统发送 Tap 手势  
**Verify**: 该 Tap 事件不被任何 gameplay 系统处理（被 InputBlocker 过滤）  
**Pass**: 在 InputService 日志中确认手势被丢弃（Blocker 非空）

### TC-002: 序列结束后输入恢复

**Setup**: 等待序列正常完成  
**Verify**: 发送 Tap 手势，验证 gameplay 系统正常响应  
**Pass**: 物件可以被选中/拖拽

### TC-003: SP-006 多发送者 Lock

**Given**: shadow_puzzle lock + narrative lock 同时激活  
**When**: 仅 narrative 解锁  
**Then**: 物件仍处于 Locked 状态（shadow_puzzle token 仍在）

**When**: shadow_puzzle 也解锁  
**Then**: 物件完全解锁

### TC-004: try/finally 清理保证

**Setup**: 在序列播放中途通过 CancellationToken 强制取消  
**Verify**: 取消后等待一帧  
**Pass**: `InputService.BlockerStack.Count == 0`（Blocker 已弹出）；`Evt_PuzzleUnlock("narrative")` 被发送

### TC-005: 场景卸载清理

**Setup**: 序列播放中，发送 `Evt_SceneUnloadBegin`  
**Verify**: 等待一帧  
**Pass**: 序列停止，Blocker 清空，Lock 解除；无 `Log.Warning` 关于 token 泄漏

---

## Test Evidence

- **Integration Test**: `tests/integration/NarrativeEngine_InputBlock_Test.cs`（含 SP-006 多发送者测试）

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| story-001 (Sequence Engine) | Base | `PlaySequence()` 是此 story 的扩展点 |
| story-003 (Sequence Chain) | Integration | Chain 期间 Blocker 不解除 |
| ADR-010 (InputBlocker) | Architecture | `PushBlocker/PopBlocker` API |
| SP-006 (PuzzleLock Token) | Architecture | `InteractionLockerId.Narrative` |
| object-interaction epic | Integration | `Evt_PuzzleLockAll/Unlock` 接收方 |
