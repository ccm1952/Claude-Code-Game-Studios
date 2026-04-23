// 该文件由Cursor 自动生成

# SP-006 Findings: PuzzleLockAll 双发送者 Token 防护

> **Status**: ✅ 设计决策完成
> **Date**: 2026-04-22

## 结论

**采用方案 C: 事件 payload 带 lockerId string token。** 结合引用计数实现安全的多发送者 Lock/Unlock。

## 并发场景分析

### 时序梳理

```
正常场景：
  PerfectMatch → Shadow Puzzle 发送 PuzzleLockAll("puzzle")
  → Narrative 查表，接管序列
  → Narrative 发送 PuzzleLockAll("narrative")  ← 可选：Narrative 也要独立锁
  → 序列播放完毕 → Narrative 发送 PuzzleUnlock("narrative")
  → Puzzle 状态清理 → Shadow Puzzle 发送 PuzzleUnlock("puzzle")
  → Object Interaction 完全解锁
```

### 关键发现

Shadow Puzzle 在 PerfectMatch 时发送锁定，Narrative 系统随后接管。两者的 Lock 有重叠期。如果 Narrative 先 Unlock 而 Puzzle 后 Unlock（或反之），需要确保不会提前解锁。

## 方案设计

### Lock Manager (Object Interaction 侧)

```csharp
public class InteractionLockManager
{
    private readonly HashSet<string> _activeLocks = new();

    public bool IsLocked => _activeLocks.Count > 0;

    public void PushLock(string lockerId)
    {
        _activeLocks.Add(lockerId);
    }

    public void PopLock(string lockerId)
    {
        if (!_activeLocks.Remove(lockerId))
            Debug.LogWarning($"[InteractionLock] Unknown locker: {lockerId}");
    }
}
```

### 事件 Payload

```csharp
// Evt_PuzzleLockAll: payload = string lockerId
GameEvent.Send<string>(EventId.Evt_PuzzleLockAll, "shadow_puzzle");
GameEvent.Send<string>(EventId.Evt_PuzzleLockAll, "narrative");

// Evt_PuzzleUnlock: payload = string lockerId  
GameEvent.Send<string>(EventId.Evt_PuzzleUnlock, "narrative");
GameEvent.Send<string>(EventId.Evt_PuzzleUnlock, "shadow_puzzle");
```

### 合法 LockerId 常量

```csharp
public static class InteractionLockerId
{
    public const string ShadowPuzzle = "shadow_puzzle";
    public const string Narrative    = "narrative";
    public const string Tutorial     = "tutorial";  // 预留
}
```

## 为什么不选方案 A（Stack）或方案 B（单发送者）

| 方案 | 否决理由 |
|------|---------|
| A: Stack | 要求严格 LIFO 顺序，但 Narrative 和 Puzzle 的 Unlock 顺序可能不固定 |
| B: 单发送者 | Tutorial 未来也需要发送 Lock，限制过死 |

## ADR-006 / ADR-013 影响

- ADR-006: `Evt_PuzzleLockAll` 和 `Evt_PuzzleUnlock` payload 更新为 `string lockerId`
- ADR-013: Object Interaction State Machine 新增 `InteractionLockManager`（HashSet 引用计数）
