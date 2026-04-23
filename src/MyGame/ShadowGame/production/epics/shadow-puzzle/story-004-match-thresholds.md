// 该文件由Cursor 自动生成

# Story 004 — Match Thresholds (Luban Config)

> **Epic**: shadow-puzzle
> **Type**: Config / Data
> **Status**: Ready
> **Priority**: MVP
> **Estimate**: 1d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/shadow-puzzle-system.md` — Tuning Knobs & Cross-chapter Difficulty Matrix |
| **TR-IDs** | TR-puzzle-003, TR-puzzle-004, TR-puzzle-013, TR-puzzle-014 |
| **ADR** | ADR-012 (parameter table), ADR-014 (per-chapter thresholds), ADR-007 (Luban access) |
| **Engine** | Unity 2022.3.62f2 LTS / Luban `TbPuzzle` / HybridCLR GameProto assembly |
| **Assembly** | `GameProto` (Luban 生成) + `GameLogic` (扩展方法) |

### Control Manifest Rules

- **CM-6**: 所有配置读取通过 `Tables.Instance.TbPuzzle.Get(id)`
- **CM-6**: `Tables.Init()` 在 ProcedureMain Step 7 完成，之后所有系统才能读取
- **CM-6**: 不可手动编辑 GameProto 中的 Luban 生成文件
- **CM-6**: 派生计算值（如 nearMatchExit = nearMatchThreshold - 0.05）使用 GameLogic 中的扩展方法
- **CM-1.4 (FORBIDDEN)**: 禁止硬编码任何阈值浮点数（如 `0.85f`、`0.40f`）

---

## Acceptance Criteria

1. **AC-001**: `TbPuzzle` 表包含以下字段，且均可从 `puzzle.nearMatchThreshold` 等 C# 属性访问：
   - `nearMatchThreshold` (float, 0.30-0.55, default 0.40)
   - `perfectMatchThreshold` (float, 0.75-0.95, default 0.85)
   - `maxScreenDistance` (float, 50-200px, default 120)
   - `maxAngleDelta` (float, 15-45°, default 30)
   - `visibilityThreshold` (int, 64-192, default 128)
   - `smoothingWindow` (float, 0.1-0.5s, default 0.2)
   - `absenceAcceptDelay` (float, 3-8s, default 5.0)
   - `maxCompletionScore` (float, 0.50-0.80, 仅缺席谜题有效)
   - `hintDelayOverride` (float, 1.0/1.3/1.5 per chapter)
   - `isAbsencePuzzle` (bool)
2. **AC-002**: 跨章节难度矩阵验证——Ch.1 nearMatchThreshold=0.40, Ch.4 nearMatchThreshold=0.35，Ch.5 非缺席 perfectMatchThreshold=0.78
3. **AC-003**: `ShadowMatchCalculator` 和 `PuzzleStateMachine` 在初始化时从 TbPuzzle 读取参数，不存在硬编码浮点数
4. **AC-004**: `nearMatchExitThreshold` 派生计算 = `nearMatchThreshold - 0.05`，通过扩展方法实现，不硬编码
5. **AC-005**: 修改 Luban 配置表后不需要重新编译（仅重新生成 `GameProto.dll` + YooAsset 热更新）

---

## Implementation Notes

### Luban 表结构（`TbPuzzle` schema）

```lua
-- 在 Luban schema 文件中定义（不手动编辑生成代码）
table PuzzleConfig {
    int     id;
    bool    isAbsencePuzzle;
    float   nearMatchThreshold;          -- default 0.40
    float   perfectMatchThreshold;       -- default 0.85; N/A for absence
    float   maxScreenDistance;           -- default 120 px
    float   maxAngleDelta;               -- default 30 deg
    int     visibilityThreshold;         -- default 128 (of 255)
    float   smoothingWindow;             -- default 0.2s
    float   absenceAcceptDelay;          -- default 5.0s
    float   maxCompletionScore;          -- default 0.0 (0 = non-absence)
    float   hintDelayOverride;           -- default 1.0
    float   tutorialGracePeriod;         -- default 3.0s
}
```

### 扩展方法（GameLogic assembly，不修改 GameProto）

```csharp
// PuzzleConfigExtensions.cs
public static class PuzzleConfigExtensions
{
    public static float GetNearMatchExitThreshold(this PuzzleConfig config)
        => config.NearMatchThreshold - 0.05f;

    public static bool IsValidForAbsence(this PuzzleConfig config)
        => config.IsAbsencePuzzle && config.MaxCompletionScore > 0f;
}
```

### 配置读取模式（热路径外缓存）

```csharp
// Init 时缓存，不在每帧调用 Tables.Instance
private PuzzleConfig _config;

public void Init(int puzzleId)
{
    _config = Tables.Instance.TbPuzzle.Get(puzzleId);
    // 仅在方法作用域内缓存，禁止跨帧持久化 config 引用
}
```

---

## Out of Scope

- `TbPuzzleObject`（单个锚点权重，属于 designer 资产配置）
- `TbHintConfig`（提示系统配置，属于 hint-system epic）
- 运行时修改配置（Luban 是只读的）

---

## QA Test Cases

### TC-001: 默认值覆盖（Ch.1 谜题）

**Given**: `TbPuzzle` 中 Ch.1 谜题的配置  
**When**: 读取参数  
**Then**: nearMatchThreshold=0.40, perfectMatchThreshold=0.85, maxScreenDistance 在 80-100px 范围内

### TC-002: Ch.4 难度矩阵

**Given**: `TbPuzzle` 中 Ch.4 谜题的配置  
**When**: 读取参数  
**Then**: nearMatchThreshold=0.35, perfectMatchThreshold=0.80

### TC-003: Ch.5 缺席谜题配置

**Given**: `TbPuzzle` 中标记为 `isAbsencePuzzle=true` 的谜题  
**When**: 读取参数  
**Then**: isAbsencePuzzle=true, maxCompletionScore 在 0.60-0.70 范围，perfectMatchThreshold=N/A（不使用）

### TC-004: 无硬编码静态分析

**Given**: `ShadowMatchCalculator.cs` 和 `PuzzleStateMachine.cs`  
**When**: 在代码中搜索浮点数字面量（`0.85f`, `0.40f`, `0.35f`, `0.20f`）  
**Then**: 在阈值相关代码路径中不出现任何此类字面量（仅允许出现在测试代码中）

### TC-005: 扩展方法派生计算

**Given**: 某谜题 nearMatchThreshold=0.40  
**When**: 调用 `config.GetNearMatchExitThreshold()`  
**Then**: 返回 0.35（0.40 - 0.05）

---

## Test Evidence

- **Unit Test**: `tests/unit/ShadowPuzzle_LubanConfig_Test.cs`（验证各章配置读取）
- **Static Analysis**: CI grep 规则验证无硬编码阈值

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| Luban 工具链 | Build | 需要生成 `TbPuzzle` C# 访问代码 |
| ADR-007 (Luban Access) | Architecture | 配置读取规范 |
| story-001 (StateMachine) | Consumer | 读取 nearMatchThreshold, perfectMatchThreshold |
| story-002 (MatchScore) | Consumer | 读取 maxScreenDistance, maxAngleDelta, smoothingWindow |
| story-006 (AbsencePuzzle) | Consumer | 读取 isAbsencePuzzle, maxCompletionScore, absenceAcceptDelay |
