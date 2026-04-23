// 该文件由Cursor 自动生成

# Story 006 — Absence Hint Handling (Ch.5 Absence Puzzle Special Hint Behavior)

> **Epic**: hint-system
> **Type**: Logic
> **Status**: Ready
> **Priority**: MVP
> **Estimate**: 1d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/hint-system.md` — Edge Cases: 缺席型谜题（第五章）|
| **TR-IDs** | TR-hint-014 |
| **ADR** | ADR-015 (absence puzzle hint content, acceptance mode) |
| **Engine** | Unity 2022.3.62f2 LTS |
| **Assembly** | `GameLogic` |

### Control Manifest Rules

- **CM-4.2**: absence 谜题中 L3 文案切换为"引导接受"内容，不暗示存在完美答案
- **CM-4.2**: 当 `matchScore >= maxCompletionScore` 时，L3 文案进一步切换为"接受鼓励"类型
- **CM-4.2 (FORBIDDEN)**: 禁止 L3 文案引导玩家追求不存在的完美解
- **CM-6**: 所有文案内容来自 Luban `TbHintConfig.absenceHintTexts`
- **CM-4.2**: L1/L2 正常触发，但引导目标为"当前可达到的最优解"而非完美解

---

## Acceptance Criteria

1. **AC-001**: 当 `IShadowPuzzle.IsAbsencePuzzle() == true` 时，L3 提示文案从普通引导切换为缺席专用文案（"也许这个影子本来就不该完整……"等）
2. **AC-002**: 当 `matchScore >= maxCompletionScore` 时，L3 文案进一步切换为"接受鼓励"类型（"也许……这就是它该有的样子"），引导玩家停止操作
3. **AC-003**: 缺席谜题的 L3 提示次数配置从 `TbHintConfig.l3MaxCountAbsence` 读取（默认 2，与标准谜题的 3 次不同）——实际值以 GDD 为准，默认保持 3 次
4. **AC-004**: L1/L2 提示的目标选择仍使用 `argmin(anchorScore)`，但 L2 引导目标为"当前最优解方向"而非"完美答案方向"
5. **AC-005**: 代码审查：absence 模式下不存在指向 perfectMatchThreshold 的引导逻辑
6. **AC-006**: 标准谜题中 `IsAbsencePuzzle()` 返回 false，缺席模式代码路径不激活

---

## Implementation Notes

### 文案切换逻辑

```csharp
private string GetL3HintText(int hintIndex)
{
    if (!_puzzleQuery.IsAbsencePuzzle())
    {
        // 标准文案（来自 TbHintConfig.hintTexts）
        return _config.HintTexts[hintIndex];
    }

    float matchScore = _puzzleQuery.GetCurrentMatchScore();
    var absenceConfig = _config.AbsenceHintConfig;

    if (matchScore >= absenceConfig.AcceptanceThreshold)
    {
        // 接受鼓励文案（matchScore 已达到 maxCompletionScore）
        return absenceConfig.AcceptanceTexts[hintIndex % absenceConfig.AcceptanceTexts.Length];
    }
    else
    {
        // 缺席引导文案（还未达到 maxCompletionScore，引导继续尝试"最优解"）
        return absenceConfig.ExplorationTexts[hintIndex % absenceConfig.ExplorationTexts.Length];
    }
}
```

### L3 次数配置

```csharp
// 从 TbPuzzle/TbHintConfig 读取（isAbsencePuzzle 时使用不同配置）
int maxL3Count = _puzzleQuery.IsAbsencePuzzle()
    ? _config.L3MaxCountAbsence   // Luban 配置，默认 2-3
    : _config.L3MaxCount;         // 标准谜题默认 3
```

### Acceptance Threshold

- `absenceConfig.AcceptanceThreshold` = `TbPuzzle.maxCompletionScore`（与 StateMachine 使用同一值）
- 通过 `IShadowPuzzle` 接口读取（`GetMaxCompletionScore()` 或通过 `TbPuzzle` 配置读取）

---

## Out of Scope

- L1/L2 视觉效果的缺席版本调整（涉及视觉资源，属于 art team）
- 缺席谜题的 Narrative 演出（narrative-event/story-002）
- `maxCompletionScore` 的配置（shadow-puzzle/story-004）

---

## QA Test Cases

### TC-001: 缺席谜题 L3 文案切换

**Given**: `IsAbsencePuzzle()=true`, matchScore=0.4（低于 maxCompletionScore=0.65）  
**When**: 玩家点击提示按钮  
**Then**: 显示缺席探索文案（如"也许这个影子本来就不该完整……"），不包含任何"移到这里就对了"的引导

### TC-002: 接受鼓励文案激活

**Given**: `IsAbsencePuzzle()=true`, matchScore=0.70（≥ maxCompletionScore=0.65）  
**When**: 玩家点击提示按钮  
**Then**: 显示接受鼓励文案（如"也许……这就是它该有的样子"），引导玩家停止操作等待 AbsenceAccepted

### TC-003: 文案不暗示完美解（内容审查）

**Given**: 所有缺席谜题文案文本（从 Luban 导出）  
**When**: 人工审查文案列表  
**Then**: 无任何"再移一点"、"到这个位置"、"完整的影子"等引导完美解的措辞

### TC-004: 标准谜题不受影响

**Given**: `IsAbsencePuzzle()=false`（标准谜题）  
**When**: 玩家点击提示按钮  
**Then**: 显示标准引导文案，不走缺席代码路径

### TC-005: L3 次数独立配置

**Given**: 缺席谜题 `l3MaxCountAbsence=2`，标准谜题 `l3MaxCount=3`  
**When**: 分别在两种谜题中使用 L3  
**Then**: 缺席谜题最多 2 次，标准谜题最多 3 次

---

## Test Evidence

- **Unit Test**: `tests/unit/HintSystem_AbsencePuzzle_Test.cs`
- **Evidence Doc**: `production/qa/evidence/story-006-absence-hint.md`（文案内容审查记录）

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| story-002 (Three-Tier) | Base | L3 触发机制（文案选择在此 story 实现） |
| story-005 (Puzzle Integration) | Interface | `IsAbsencePuzzle()`, `GetCurrentMatchScore()` |
| shadow-puzzle/story-006 | Feature Alignment | maxCompletionScore 值需对齐 |
| Luban TbHintConfig | Config | absenceHintTexts, acceptanceTexts 内容 |
