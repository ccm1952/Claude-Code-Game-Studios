// 该文件由Cursor 自动生成

# Story 003 — Hint Cooldown (Layer Cooldown + matchScore Modifier + Luban Config)

> **Epic**: hint-system
> **Type**: Logic
> **Status**: Ready
> **Priority**: MVP
> **Estimate**: 1d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/hint-system.md` — Formulas: Cooldown Duration |
| **TR-IDs** | TR-hint-004, TR-hint-010 |
| **ADR** | ADR-015 (cooldown formula, hintDelayOverride) |
| **Engine** | Unity 2022.3.62f2 LTS / TEngine TimerModule |
| **Assembly** | `GameLogic` |

### Control Manifest Rules

- **CM-4.2**: `actualCooldown = baseCooldown × clamp(1.0 + (matchScore - 0.3) × 0.5, 0.7, 1.3)`
- **CM-4.2**: baseCooldown = 30s（来自 Luban，不硬编码）
- **CM-4.2**: cooldown 期间任何玩家操作不影响冷却倒计时（倒计时持续走完）
- **CM-4.2**: hintDelayOverride 影响 idleThreshold，不影响 cooldown duration
- **CM-6**: 所有参数来自 Luban `TbHintConfig`
- **CM-1.4**: 使用 `GameModule.Timer`，不使用 Coroutine

---

## Acceptance Criteria

1. **AC-001**: `actualCooldown = baseCooldown × cooldownModifier` 计算正确；`cooldownModifier = clamp(1.0 + (matchScore - 0.3) × 0.5, 0.7, 1.3)`
2. **AC-002**: matchScore=0.0 → cooldownModifier=0.85 → actualCooldown=25.5s（baseCooldown=30s）
3. **AC-003**: matchScore=0.30 → cooldownModifier=1.0 → actualCooldown=30s
4. **AC-004**: matchScore=0.60 → cooldownModifier=1.15（clamped at 1.3）→ actualCooldown ≤ 39s
5. **AC-005**: 冷却期间玩家操作不重置冷却倒计时（冷却到底才进 Observing）
6. **AC-006**: 冷却结束后（达到 actualCooldown 时长），状态机回到 Observing，重新评估 triggerScore
7. **AC-007**: baseCooldown 从 `TbHintConfig.baseCooldown` 读取，不存在硬编码 `30f`

---

## Implementation Notes

### 冷却公式（ADR-015）

```csharp
private float CalculateCooldown(float currentMatchScore)
{
    float baseCooldown = _config.BaseCooldown;  // from Luban TbHintConfig
    float modifier = Mathf.Clamp(
        1.0f + (currentMatchScore - 0.3f) * 0.5f,
        0.7f, 1.3f
    );
    return baseCooldown * modifier;
}
```

### 冷却触发时机

```
Layer1Active 被玩家操作打断 → EnterCooldown(currentMatchScore)
Layer2Active 被玩家操作打断 → EnterCooldown(currentMatchScore)

Cooldown 期间：
  - 不响应 triggerScore 评估
  - 不响应玩家操作的计时重置（冷却独立于 idleTimer）

Cooldown 结束 → 回到 Observing
```

### hintDelayOverride vs cooldown 的区别

- `hintDelayOverride` 影响 `effectiveIdleThreshold = idleThreshold × hintDelayOverride`（决定何时触发）
- `cooldown` 影响层级间隔时长（决定 L1 触发后多久可触发 L2）
- 两者完全独立

---

## Out of Scope

- triggerScore 评估（story-001）
- 层级推进逻辑（story-002）
- hintDelayOverride 章节配置（属于 TbHintConfig/TbPuzzle 集成）

---

## QA Test Cases

### TC-001: 低 matchScore 缩短冷却

**Given**: matchScore=0.0, baseCooldown=30s  
**When**: `CalculateCooldown(0.0f)`  
**Then**: 返回 25.5s（0.85 × 30）

### TC-002: 中等 matchScore 标准冷却

**Given**: matchScore=0.30, baseCooldown=30s  
**When**: `CalculateCooldown(0.30f)`  
**Then**: 返回 30.0s（1.0 × 30）

### TC-003: 高 matchScore 延长冷却（上限）

**Given**: matchScore=0.9, baseCooldown=30s  
**When**: `CalculateCooldown(0.9f)`  
**Then**: 返回 39.0s（clamp 到 1.3 × 30）

### TC-004: 操作不打断冷却

**Given**: 已进入 Cooldown（20s 剩余）  
**When**: 玩家移动物件  
**Then**: 冷却倒计时继续（不重置），HintLayerState = Cooldown

### TC-005: Cooldown 结束后 Observing 重置

**Given**: Cooldown 结束  
**When**: 进入 Observing  
**Then**: idleTimer 保留当前值（继续累积），triggerScore 重新评估

---

## Test Evidence

- **Unit Test**: `tests/unit/HintSystem_Cooldown_Test.cs`

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| story-001 (Trigger Logic) | Base | 触发后进入冷却 |
| story-002 (Three-Tier) | Base | L1/L2 中断后触发冷却 |
| Luban TbHintConfig | Config | baseCooldown 参数 |
