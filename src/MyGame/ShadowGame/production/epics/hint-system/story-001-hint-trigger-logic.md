// 该文件由Cursor 自动生成

# Story 001 — Hint Trigger Logic (Timer + MatchScore Based Activation)

> **Epic**: hint-system
> **Type**: Logic
> **Status**: Ready
> **Priority**: MVP
> **Estimate**: 2d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/hint-system.md` — Formulas: Hint Trigger Threshold |
| **TR-IDs** | TR-hint-002, TR-hint-003, TR-hint-008, TR-hint-013, TR-hint-015, TR-hint-016, TR-hint-017 |
| **ADR** | ADR-015 (Hint System — triggerScore formula, timer lifecycle) |
| **Engine** | Unity 2022.3.62f2 LTS / TEngine TimerModule |
| **Assembly** | `GameLogic` — `Assets/GameScripts/HotFix/GameLogic/GameLogic/HintSystem/` |

### Control Manifest Rules

- **CM-4.2**: `triggerScore = timeScore + failScore + stagnationScore + matchPenalty`；`triggerScore ≥ 1.0` 时触发
- **CM-4.2**: 1s 轮询间隔查询 Shadow Puzzle（非每帧），性能预算 ≤ 0.5ms/帧
- **CM-4.2**: 所有参数来自 Luban `TbHintConfig`
- **CM-1.4**: 使用 TEngine `GameModule.Timer`（不用 Coroutine）
- **CM-4.2**: deltaTime 每帧封顶 1.0s（防止一帧跳过大量时间）
- **CM-4.2**: App pause：计时器暂停；后台超 5 分钟则重置

---

## Acceptance Criteria

1. **AC-001**: `triggerScore = timeScore + failScore + stagnationScore + matchPenalty` 公式实现正确（各分量精度 ±0.001）
2. **AC-002**: `timeScore = min(idleTime / (idleThreshold × hintDelayOverride), 1.0)` 正确，不超过 1.0
3. **AC-003**: `failScore = min(failCount × failWeight, 0.6)` 上限 0.6 正确
4. **AC-004**: `matchPenalty = (matchScore < matchLowThreshold) ? (1.0 - matchScore / matchLowThreshold) × matchPenaltyMax : 0.0`
5. **AC-005**: stagnation 检测：matchScore 在 ±0.05 范围内连续 30s 且有交互操作 → `stagnationScore = stagnationBonus (0.3)`
6. **AC-006**: `triggerScore ≥ 1.0` 时，escalate 到下一层提示
7. **AC-007**: `deltaTime` 每帧 `min(Time.unscaledDeltaTime, 1.0f)` 封顶防止时间跳跃
8. **AC-008**: App pause（`OnApplicationPause(true)`）→ 所有计时器暂停；恢复后后台超 5 分钟则重置计时器为 0
9. **AC-009**: 1s 轮询间隔：触发逻辑不在每帧 Update 中执行，而是每 1s 一次

---

## Implementation Notes

### 触发评分公式（ADR-015）

```csharp
private float CalculateTriggerScore(int layer)
{
    var cfg = GetLayerConfig(layer);
    float effectiveIdleThreshold = cfg.IdleThreshold * _hintDelayOverride;

    float timeScore        = Mathf.Min(_idleTimer / effectiveIdleThreshold, 1.0f);
    float failScore        = Mathf.Min(_failCount * cfg.FailWeight, 0.6f);
    float stagnationScore  = _stagnationDetected ? cfg.StagnationBonus : 0f;
    float matchPenalty     = _matchScore < cfg.MatchLowThreshold
        ? (1.0f - _matchScore / cfg.MatchLowThreshold) * cfg.MatchPenaltyMax
        : 0f;

    return timeScore + failScore + stagnationScore + matchPenalty;
}
```

### 1s 轮询策略

```csharp
private float _pollTimer = 0f;
private const float POLL_INTERVAL = 1.0f;

void Update()
{
    float dt = Mathf.Min(Time.unscaledDeltaTime, 1.0f);
    _idleTimer += dt;
    _stagnationTimer += dt;

    _pollTimer += dt;
    if (_pollTimer >= POLL_INTERVAL)
    {
        _pollTimer = 0f;
        PollPuzzleState();   // 读取 matchScore / anchorScores
        EvaluateTrigger();   // 计算 triggerScore
    }
}
```

### App Pause 处理

```csharp
void OnApplicationPause(bool paused)
{
    if (paused)
    {
        _pauseStartTime = Time.realtimeSinceStartup;
        PauseAllTimers();
    }
    else
    {
        float pauseDuration = Time.realtimeSinceStartup - _pauseStartTime;
        if (pauseDuration > 300f)   // 5 分钟
            ResetAllTimers();
        else
            ResumeAllTimers();
    }
}
```

---

## Out of Scope

- 3层提示的具体展示（story-002）
- 提示冷却逻辑（story-003）
- Luban 配置集成（story-005 的集成验证）
- Tutorial 暂停/重置（story-001 中包含计时器 API，story-005 中集成事件监听）

---

## QA Test Cases

### TC-001: 纯时间触发（L1）

**Given**: idleThreshold=45s, hintDelayOverride=1.0, failCount=0, matchScore=0.5（无 penalty）, stagnation=false  
**When**: idleTimer = 46s（超过阈值）  
**Then**: triggerScore = 1.0 + 0 + 0 + 0 = 1.0，L1 应触发

### TC-002: matchPenalty 加速

**Given**: idleTimer=30s, idleThreshold=45s, matchScore=0.0, matchPenaltyMax=0.4  
**When**: 计算 triggerScore  
**Then**: timeScore ≈ 0.667, matchPenalty = 0.4 → triggerScore ≈ 1.067 ≥ 1.0（触发）

### TC-003: stagnation 检测

**Given**: matchScore 在 0.35±0.03 范围内波动 31s，期间有物件操作记录  
**When**: 轮询  
**Then**: stagnationDetected=true, stagnationScore=0.3（加速提示）

### TC-004: deltaTime 封顶

**Given**: 模拟一帧 `Time.unscaledDeltaTime = 5.0s`（如帧率突降）  
**When**: Update 执行  
**Then**: 实际累加到计时器的值 ≤ 1.0s（不超过 5s 的跳跃）

### TC-005: App Pause 后台超 5 分钟重置

**Given**: idleTimer=40s（接近 L1 触发）  
**When**: `OnApplicationPause(true)` 持续 301s 后恢复  
**Then**: idleTimer 重置为 0，failCount=0，stagnation 重置

### TC-006: 1s 轮询间隔验证

**Given**: 谜题处于 Active 状态  
**When**: 在 Unity Profiler 中观察 HintSystem.Update 中的 `PollPuzzleState()` 调用频率  
**Then**: 每秒调用一次（±0.1s 精度）

---

## Test Evidence

- **Unit Test**: `tests/unit/HintSystem_TriggerScore_Test.cs`

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| story-005 (Puzzle Integration) | Interface | 通过 `IShadowPuzzle` 读取 matchScore |
| story-003 (Cooldown) | Consumer | triggerScore 触发后进入冷却 |
| Luban TbHintConfig | Config | idleThreshold, failWeight 等参数 |
| ADR-015 | Architecture | triggerScore 公式定义 |
