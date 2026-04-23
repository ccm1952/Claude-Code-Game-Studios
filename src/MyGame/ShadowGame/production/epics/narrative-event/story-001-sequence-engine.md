// 该文件由Cursor 自动生成

# Story 001 — Narrative Sequence Engine (Core Playback Loop)

> **Epic**: narrative-event
> **Type**: Logic
> **Status**: Ready
> **Priority**: Vertical Slice
> **Estimate**: 3d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/narrative-event-system.md` — States and Transitions; Playback Algorithm |
| **TR-IDs** | TR-narr-002, TR-narr-008, TR-narr-009 |
| **ADR** | ADR-016 (Narrative Sequence Engine — core playback algorithm, queue, graceful failure) |
| **Finding** | SP-008 — Sequence Chain（nextSequenceId）机制在此 story 的核心引擎中实现 |
| **Engine** | Unity 2022.3.62f2 LTS / UniTask / TEngine GameEvent |
| **Assembly** | `GameLogic` — `Assets/GameScripts/HotFix/GameLogic/GameLogic/NarrativeEvent/` |

### Control Manifest Rules

- **CM-4.1**: 时间排序原子效果序列（`startTime` 字段驱动），支持并行效果
- **CM-4.1**: 所有序列定义来自 Luban 配置表，不硬编码任何序列内容
- **CM-4.1**: 序列队列：最多 3 个待处理；满时丢弃新序列并 `Log.Warning`
- **CM-4.1**: 资源加载失败：跳过该 effect，继续序列，`Log.Warning`
- **CM-1.4**: 异步操作使用 UniTask（`await UniTask.Yield()`），不使用 Coroutine
- **CM-2.2**: 在 `Init()` 注册事件监听，`Dispose()` 中移除
- **CM-4.1 (FORBIDDEN)**: 禁止在 C# 方法中硬编码序列内容

---

## Acceptance Criteria

1. **AC-001**: `NarrativeSequenceEngine.PlaySequence(sequenceId)` 按 `startTime` 排序，执行所有原子效果
2. **AC-002**: 相同 `startTime` 的多个效果在同一帧并行启动
3. **AC-003**: 序列播放期间：`PushBlocker("narrative_seq_{sequenceId}")` + `Send(Evt_PuzzleLockAll, "narrative")` 在序列开始前执行
4. **AC-004**: 序列完成后：`PopBlocker("narrative_seq_{sequenceId}")` + `Send(Evt_PuzzleUnlock, "narrative")` + `Send(Evt_SequenceComplete, sequenceId)` 在序列结束后执行
5. **AC-005**: 序列队列：Playing 期间收到新触发事件 → 入队（FIFO）；队列满（3个）→ 丢弃 + `Log.Warning`
6. **AC-006**: 原子效果资源加载失败 → 跳过该效果，记录 `Log.Warning`，继续后续效果
7. **AC-007**: 序列空（无效果）→ 立即发送 `Evt_SequenceComplete`，不挂起
8. **AC-008**: App pause 期间（`OnApplicationPause(true)`），序列计时器暂停；恢复后继续
9. **AC-009**: 序列引擎每帧更新 CPU 耗时 ≤ 1ms

---

## Implementation Notes

### 核心 PlaySequence 实现（ADR-016）

```csharp
public async UniTask PlaySequence(string sequenceId)
{
    PushBlocker(sequenceId);
    GameEvent.Send(EventId.Evt_PuzzleLockAll, InteractionLockerId.Narrative);

    var config = Tables.Instance.TbNarrativeSequence.Get(sequenceId);
    if (config == null)
    {
        Log.Warning($"[Narrative] Sequence not found: {sequenceId}");
        PopBlocker(sequenceId);
        GameEvent.Send(EventId.Evt_PuzzleUnlock, InteractionLockerId.Narrative);
        return;
    }

    var effects = config.Effects.OrderBy(e => e.StartTime).ToList();
    float elapsed = 0f;
    var activeEffects = new List<IAtomicEffect>();
    var startedEffects = new HashSet<int>();

    while (elapsed < config.TotalDuration || activeEffects.Count > 0)
    {
        float dt = _isPaused ? 0f : Time.deltaTime;

        // 启动到达 startTime 的效果（并行支持）
        for (int i = 0; i < effects.Count; i++)
        {
            if (!startedEffects.Contains(i) && elapsed >= effects[i].StartTime)
            {
                var effect = CreateEffect(effects[i]);
                if (effect != null) { effect.Start(); activeEffects.Add(effect); }
                startedEffects.Add(i);
            }
        }

        foreach (var e in activeEffects) e.Update(dt);
        activeEffects.RemoveAll(e => e.IsComplete);
        elapsed += dt;
        await UniTask.Yield();
    }

    PopBlocker(sequenceId);
    GameEvent.Send(EventId.Evt_PuzzleUnlock, InteractionLockerId.Narrative);
    GameEvent.Send(EventId.Evt_SequenceComplete, sequenceId);
}
```

### 队列管理

```csharp
private readonly Queue<string> _pendingQueue = new(3);

private void TryEnqueue(string sequenceId)
{
    if (_isPlaying)
    {
        if (_pendingQueue.Count >= _config.QueueMaxSize)
            Log.Warning($"[Narrative] Queue full, dropping sequence: {sequenceId}");
        else
            _pendingQueue.Enqueue(sequenceId);
    }
    else
    {
        _ = PlaySequence(sequenceId);
    }
}
```

---

## Out of Scope

- 具体原子效果类型实现（story-002）
- Sequence Chain 逻辑（story-003）
- Luban 配置集成（story-004）
- PerfectMatch → Narrative 事件触发（story-005）

---

## QA Test Cases

### TC-001: 序列正常播放

**Given**: 一个序列包含 3 个效果（startTime=0, 1, 2s）  
**When**: `PlaySequence("test_seq")` 被调用  
**Then**: 效果按时间顺序在各自 startTime 开始，序列结束时发送 `Evt_SequenceComplete`

### TC-002: 并行效果

**Given**: 2 个效果 startTime=0.0（同时）  
**When**: 序列开始  
**Then**: 两个效果在同一帧调用 `Start()`，并行执行

### TC-003: 队列溢出警告

**Given**: 序列正在播放中  
**When**: 快速触发 4 次新序列  
**Then**: 前 2 次入队，第 3 次入队（队列满），第 4 次触发 `Log.Warning` 并丢弃

### TC-004: 资源加载失败不崩溃

**Given**: 序列中某效果的资源路径无效  
**When**: 序列播放到该效果  
**Then**: 跳过该效果（不崩溃），记录 `Log.Warning`，后续效果正常执行，序列正常完成

### TC-005: 空序列立即完成

**Given**: 序列没有任何效果（effects 列表为空）  
**When**: `PlaySequence("empty_seq")`  
**Then**: 立即发送 `Evt_SequenceComplete`，Blocker 和 Lock 正确弹出

### TC-006: App Pause 暂停计时

**Given**: 序列在第 2s 处暂停应用  
**When**: 暂停后 3s 恢复  
**Then**: 序列从第 2s 继续（不跳过暂停期间的效果），时间轴正确

---

## Test Evidence

- **Unit Test**: `tests/unit/NarrativeEngine_Playback_Test.cs`

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| story-002 (Atomic Effects) | Blocking | `IAtomicEffect` 接口实现 |
| story-004 (Luban Config) | Blocking | `TbNarrativeSequence` 配置读取 |
| SP-006 (PuzzleLock Token) | Architecture | Narrative token = `InteractionLockerId.Narrative` |
| EventId.cs (1500-1599) | Code | `Evt_SequenceComplete`, `Evt_PuzzleLockAll/Unlock` |
| InputBlocker (ADR-010) | Integration | `PushBlocker/PopBlocker` |
