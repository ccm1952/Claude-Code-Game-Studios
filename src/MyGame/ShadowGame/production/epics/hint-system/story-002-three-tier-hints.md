// 该文件由Cursor 自动生成

# Story 002 — Three-Tier Hints (Zone Focus → Relationship Hint → Action Intent)

> **Epic**: hint-system
> **Type**: Logic
> **Status**: Ready
> **Priority**: MVP
> **Estimate**: 2d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/hint-system.md` — Core Rules: 提示层级定义; States and Transitions |
| **TR-IDs** | TR-hint-001, TR-hint-005, TR-hint-006 |
| **ADR** | ADR-015 (3-tier escalation, Layer 3 rules, target selection) |
| **Engine** | Unity 2022.3.62f2 LTS / TEngine GameModule.UI |
| **Assembly** | `GameLogic` |

### Control Manifest Rules

- **CM-4.2**: 3层提示: L1 Ambient → L2 Directional → L3 Explicit（按序推进）
- **CM-4.2**: L2 不会在 L1 未触发的情况下直接出现
- **CM-4.2**: L3 仅由玩家按提示按钮触发（不由 triggerScore 自动触发），最多 3 次/谜题
- **CM-4.2**: 提示目标选择：`argmin(anchorScore_i)` where `anchorWeight_i >= minWeight`；平局时选 argmax(weight)
- **CM-4.2 (FORBIDDEN)**: 禁止从 Hint System 向 Shadow Puzzle System 写入任何状态

---

## Acceptance Criteria

1. **AC-001**: L1（Ambient）：选出 anchorScore 最低的物件，发出 `Evt_HintAvailable{layer=1, targetObjectId}`
2. **AC-002**: L2（Directional）：在 L1 触发且冷却结束后才可触发，发出 `Evt_HintAvailable{layer=2, targetObjectId}`
3. **AC-003**: L3（Explicit）：仅在玩家点击提示按钮（`Evt_RequestExplicitHint`）且剩余次数 > 0 时触发；每次使用剩余次数 -1
4. **AC-004**: 目标选择算法：`argmin(anchorScore_i, anchorWeight_i >= minWeight)`；差值 < tieThreshold (0.1) 时选 argmax(anchorWeight)
5. **AC-005**: L3 使用次数 = 0 时发送 `Evt_HintButtonExhausted`（UI 按钮变灰）
6. **AC-006**: matchScore > 0.40（NearMatch 状态）时，L1 **不触发**（玩家已在正确方向）
7. **AC-007**: 谜题 Complete 后，`HintLayerStateMachine` 回到 Idle，所有层级重置
8. **AC-008**: L3 触发时，L1/L2 当前动画立即停止（L3 优先），但 L1/L2 进度保留（回到 Observing 后继续计时）

---

## Implementation Notes

### 层级状态机

```csharp
public enum HintLayerState
{
    Idle,
    Observing,
    Layer1Active,
    Layer2Active,
    Cooldown,
    Layer3Ready,  // 并行 flag，不互斥
    Layer3Active
}
```

### 目标选择算法

```csharp
private int SelectHintTarget(AnchorScore[] anchorScores)
{
    float minScore = float.MaxValue;
    float maxWeight = float.MinValue;
    int targetIdx = -1;

    foreach (var (score, idx) in anchorScores.Select((s, i) => (s, i)))
    {
        if (score.Weight < _config.MinWeight) continue;

        if (score.CombinedScore < minScore - _config.TieThreshold)
        {
            minScore = score.CombinedScore;
            maxWeight = score.Weight;
            targetIdx = idx;
        }
        else if (Mathf.Abs(score.CombinedScore - minScore) <= _config.TieThreshold
                 && score.Weight > maxWeight)
        {
            maxWeight = score.Weight;
            targetIdx = idx;
        }
    }
    return targetIdx;
}
```

### 事件流

```
triggerScore ≥ 1.0 (story-001 触发)
    → L1Active：GameEvent.Send(Evt_HintAvailable, {layer=1, targetObjectId})
    → UI 响应，播放光晕动画

玩家操作 → L1 中断 → Cooldown
冷却结束 + triggerScore ≥ 1.0 (L2参数)
    → L2Active：GameEvent.Send(Evt_HintAvailable, {layer=2, targetObjectId})

玩家按提示按钮 → Evt_RequestExplicitHint
    → 扣减 l3RemainingCount
    → L3Active：GameEvent.Send(Evt_HintAvailable, {layer=3, targetObjectId, hintText})
```

---

## Out of Scope

- 光晕、虚影、箭头的渲染实现（属于 story-004 UI）
- triggerScore 计算（story-001）
- 冷却时间逻辑（story-003）
- Tutorial 暂停（story-001）

---

## QA Test Cases

### TC-001: L1 仅对低 matchScore 触发

**Given**: 谜题在 Active 状态，matchScore=0.42（已进入 NearMatch 阈值）  
**When**: triggerScore ≥ 1.0（故意设置极高 failCount）  
**Then**: L1 不触发（matchScore > 0.40 suppression）

### TC-002: L2 不跳级

**Given**: 谜题在 Observing 状态（L1 未触发）  
**When**: 用 L2 参数计算 triggerScore ≥ 1.0  
**Then**: L2 不触发（L1 未经历过，按序推进规则阻断）

### TC-003: L3 次数限制

**Given**: l3RemainingCount=1  
**When**: 玩家点击提示按钮  
**Then**: L3 触发，l3RemainingCount=0；再次点击发送 `Evt_HintButtonExhausted`，不再触发 L3

### TC-004: 目标选择 argmin

**Given**: 3 个锚点，scores=[0.8, 0.3, 0.5]，weights=[0.5, 0.5, 0.5]  
**When**: 调用 `SelectHintTarget()`  
**Then**: 返回 index=1（score 最低）

### TC-005: 目标选择平局处理

**Given**: 3 个锚点，scores=[0.30, 0.32, 0.7]（前两个差值 0.02 < tieThreshold），weights=[0.5, 1.0, 0.5]  
**When**: 调用 `SelectHintTarget()`  
**Then**: 返回 index=1（平局中选 weight 最高的）

### TC-006: L3 中断 L1

**Given**: 谜题在 Layer1Active 状态（光晕正在播放）  
**When**: 玩家点击提示按钮（Evt_RequestExplicitHint）  
**Then**: L1 中断（Evt_HintCancelled{layer=1}），L3 立即播放；L1 进度保留（返回 Observing 后 idleTimer 继续）

---

## Test Evidence

- **Unit Test**: `tests/unit/HintSystem_ThreeTier_Test.cs`

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| story-001 (Trigger Logic) | Blocking | triggerScore ≥ 1.0 是层级推进的前置条件 |
| story-003 (Cooldown) | Blocking | 层级间冷却控制推进节奏 |
| story-005 (Puzzle Integration) | Interface | `GetAnchorScores()` 用于目标选择 |
| story-004 (Hint Display) | Consumer | 接收 `Evt_HintAvailable` 渲染反馈 |
| EventId.cs (1700-1799) | Code | Evt_HintAvailable, Evt_HintButtonExhausted |
