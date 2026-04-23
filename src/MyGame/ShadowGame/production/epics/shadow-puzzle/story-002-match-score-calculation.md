// 该文件由Cursor 自动生成

# Story 002 — Match Score Calculation

> **Epic**: shadow-puzzle
> **Type**: Logic
> **Status**: Ready
> **Priority**: MVP
> **Estimate**: 2d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/shadow-puzzle-system.md` — Formulas: Shadow Match Score |
| **TR-IDs** | TR-puzzle-001, TR-puzzle-002, TR-puzzle-007, TR-puzzle-010 |
| **ADR** | ADR-012 (Shadow Match Algorithm — Multi-Anchor Weighted Scoring) |
| **Engine** | Unity 2022.3.62f2 LTS / Camera.WorldToScreenPoint / NativeArray |
| **Assembly** | `GameLogic` — `Assets/GameScripts/HotFix/GameLogic/GameLogic/ShadowPuzzle/` |

### Control Manifest Rules

- **CM-3.1**: 多锚点加权评分 `matchScore = Σ(w_i × s_i) / Σ(w_i)`
- **CM-3.1**: Per-anchor 乘法 `anchorScore = positionScore × directionScore × visibilityScore`
- **CM-3.1**: 时间平滑：指数移动平均，0.2s 滑窗
- **CM-3.1**: 所有锚点不可见时强制 `matchScore = 0`
- **CM-3.1**: 状态变更时重置平滑缓冲区
- **CM-3.1**: 所有阈值来自 Luban `TbPuzzle`，不可硬编码
- **CM-1.4 (FORBIDDEN)**: 禁止每帧调用 `Camera.main`，必须缓存相机引用
- **CM-3.1**: shadowRT 读回失败时复用上一帧缓冲区

---

## Acceptance Criteria

1. **AC-001**: `ShadowMatchCalculator.CalculateMatchScore()` 在 15 个锚点时单次调用耗时 ≤ 0.8ms（在目标设备 iPhone 13 Mini 上验证）
2. **AC-002**: `anchorScore_i = positionScore_i × directionScore_i × visibilityScore_i`，乘法正确（任一分量为0则该锚点得0分）
3. **AC-003**: `matchScore = Σ(w_i × s_i) / Σ(w_i)` 加权平均，权重为0时不参与分母
4. **AC-004**: 所有锚点 visibilityScore=0 时，`matchScore` 强制返回 0.0（防止幽灵得分）
5. **AC-005**: 时间平滑：`smoothingFactor = 1 - exp(-dt / 0.2)`，exponential moving average 实现正确
6. **AC-006**: 状态切换时（如进入 Active），`ResetSmoothing()` 清空历史值防止旧分数污染
7. **AC-007**: `GetAnchorScores()` 返回每个锚点的 `AnchorScore` 结构体，供 HintSystem 1s 轮询消费
8. **AC-008**: 相机引用在 Awake/Init 时缓存，不调用 `Camera.main` 热路径

---

## Implementation Notes

### 核心数据结构（ADR-012）

```csharp
public struct AnchorScore
{
    public int AnchorId;
    public float PositionScore;    // 0-1
    public float DirectionScore;   // 0-1
    public float VisibilityScore;  // 0 or 1
    public float CombinedScore;    // positionScore × directionScore × visibilityScore
    public float Weight;           // from Luban TbPuzzleObject
}

public class ShadowMatchCalculator
{
    public float CalculateMatchScore(
        PuzzleAnchor[] anchors,
        AnchorTarget[] targets,
        NativeArray<byte> shadowRTData,
        int rtWidth, int rtHeight
    );

    public AnchorScore[] GetAnchorScores();
    public float GetSmoothedScore();
    public void ResetSmoothing();
}
```

### 评分公式

```
positionScore  = 1.0 - clamp(screenDistance / maxScreenDistance, 0, 1)
directionScore = 1.0 - clamp(angleDelta / maxAngleDelta, 0, 1)
visibilityScore = shadowRTSample >= visibilityThreshold ? 1.0 : 0.0

rawMatchScore = Σ(anchorWeight_i × anchorScore_i) / Σ(anchorWeight_i)

smoothingFactor = 1.0 - exp(-deltaTime / smoothingWindow)
matchScore = lerp(previousMatchScore, rawMatchScore, smoothingFactor)
```

### 关键实现要点

1. 缓存 `Camera _camera` 引用，在 `Init()` 中赋值
2. visibilityScore 使用 `NativeArray<byte>` 像素采样（从 story-003 的 AsyncGPUReadback 获取）；若数据未就绪则复用上帧
3. 所有参数（maxScreenDistance, maxAngleDelta, visibilityThreshold, smoothingWindow）来自 `Tables.Instance.TbPuzzle.Get(puzzleId)`
4. 全锚点不可见判断：`if (Σ(visibilityScore_i) == 0) return 0f`（在加权平均前）
5. `AnchorScore` 为 struct（值类型），`GetAnchorScores()` 返回 struct 数组减少 GC

---

## Out of Scope

- AsyncGPUReadback 管线（story-003）
- 状态机集成（story-001）
- 阈值配置从 Luban 加载的完整集成（story-004）
- NearMatch/PerfectMatch 视觉反馈（story-007）

---

## QA Test Cases

### TC-001: 基础加权平均

**Given**: 2 个锚点，anchor[0] score=0.8 weight=1.0，anchor[1] score=0.4 weight=0.5  
**When**: 调用 `CalculateMatchScore`（shadowRT 全部可见）  
**Then**: rawMatchScore = (0.8×1.0 + 0.4×0.5) / (1.0+0.5) = 0.667（误差 ≤ 0.001）

### TC-002: 乘法短路

**Given**: 1 个锚点，positionScore=1.0, directionScore=1.0, visibilityScore=0.0  
**When**: 计算 anchorScore  
**Then**: anchorScore = 0.0（visibilityScore=0 使整体为0）

### TC-003: 全锚点不可见强制归零

**Given**: 3 个锚点，所有 visibilityScore = 0.0  
**When**: 调用 `CalculateMatchScore`  
**Then**: 返回 matchScore = 0.0

### TC-004: 时间平滑收敛

**Given**: `ResetSmoothing()` 后，rawMatchScore 稳定为 1.0  
**When**: 连续更新 1 秒（0.2s 窗口，60fps）  
**Then**: `GetSmoothedScore()` ≥ 0.99（完全收敛）

### TC-005: 平滑缓冲区重置

**Given**: 谜题运行中 matchScore=0.8  
**When**: 调用 `ResetSmoothing()`，然后 rawMatchScore=0.1  
**Then**: 第一帧 `GetSmoothedScore()` 从 0.1 开始（不从 0.8 继续衰减）

### TC-006: 相机缓存验证（代码审查）

**Given**: `ShadowMatchCalculator` 源代码  
**When**: 搜索 `Camera.main`  
**Then**: 在 `Update/CalculateMatchScore` 路径中不存在 `Camera.main` 调用

---

## Test Evidence

- **Unit Test**: `tests/unit/ShadowPuzzle_MatchScore_Test.cs`（已存在，需扩展 TC-003-006）
- **Performance**: 在 EditMode 测试中验证 15 锚点 × 1000 次迭代 < 800μs/次

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| story-003 (ShadowRT Sampling) | Blocking | 需要 `NativeArray<byte>` shadowRT 数据 |
| story-004 (Match Thresholds) | Blocking | maxScreenDistance 等参数来自 Luban TbPuzzle |
| PuzzleAnchor ScriptableObject | Data | 设计师预配置的锚点数据 |
| AnchorTarget | Data | 目标锚点位置（来自章节配置） |
