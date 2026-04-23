// 该文件由Cursor 自动生成

# Story 004 — Luban Sequence Config (TbNarrativeSequence Integration)

> **Epic**: narrative-event
> **Type**: Config / Data
> **Status**: Ready
> **Priority**: Vertical Slice
> **Estimate**: 2d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/narrative-event-system.md` — 配置表驱动 |
| **TR-IDs** | TR-narr-003, TR-narr-010 |
| **ADR** | ADR-016 (Luban config structure), ADR-007 (Luban access patterns), SP-008 (nextSequenceId schema) |
| **Engine** | Unity 2022.3.62f2 LTS / Luban / HybridCLR GameProto assembly |
| **Assembly** | `GameProto` (生成) + `GameLogic` (读取) |

### Control Manifest Rules

- **CM-6**: 所有序列内容通过 Luban 配置表，不硬编码
- **CM-6**: 不手动编辑 GameProto 中 Luban 生成的文件
- **CM-6**: 配置读取通过 `Tables.Instance.TbNarrativeSequence.Get(sequenceId)` 等接口
- **CM-4.1 (FORBIDDEN)**: 禁止在代码中定义任何演出时序内容
- **CM-6**: Tables 在主线程访问（不在 UniTask.Run 线程池中）

---

## Acceptance Criteria

1. **AC-001**: `TbNarrativeSequence` 表定义（包含 SP-008 新增字段）：
   - `sequenceId: string` (PK)
   - `sequenceType: enum {MemoryReplay, ChapterTransition, AbsencePuzzle}`
   - `effects: List<AtomicEffectEntry>`（含 `effectType`, `startTime`, 各效果参数）
   - `nextSequenceId: string`（SP-008 新增，空=不链接）
   - `nextSequenceDelay: float`（SP-008 新增，0=无缝衔接）
   - `totalDuration: float`（所有效果结束时间的最大值）
2. **AC-002**: `TbPuzzleNarrativeMap` 表：`puzzleId (int) → sequenceId (string)` 映射
3. **AC-003**: `TbChapterTransitionMap` 表：`chapterId (int) → sequenceId (string)` 映射
4. **AC-004**: `AtomicEffectEntry` 使用**类型化子表**（不使用 generic dict），每种 effectType 有独立参数字段（Open Question #4 解决方案）
5. **AC-005**: 修改 Luban 配置后不需要重新编译 GameLogic.dll（仅重新生成 GameProto.dll + YooAsset 热更新）
6. **AC-006**: 不存在的 sequenceId 查询 → 返回 null（不抛异常）；调用方处理 null 情况

---

## Implementation Notes

### TbNarrativeSequence schema 设计

```lua
-- NarrativeSequence 主表
bean SequenceConfig {
    string  sequenceId;           -- PK
    SequenceType sequenceType;    -- enum
    float   totalDuration;        -- 所有 effect 的最大 startTime+duration
    string  nextSequenceId;       -- SP-008 chain
    float   nextSequenceDelay;    -- SP-008 delay (0=零间断)
    list<AtomicEffectEntry> effects;
}

-- 类型化 effect entry（解决 Open Question #4）
bean AtomicEffectEntry {
    EffectType  effectType;
    float       startTime;
    // 各类型的 union/variant 参数字段
    // 推荐使用多个 nullable 字段，每种类型只填对应字段
    AudioDuckingParams  audioDuckParams;   // nullable
    ColorTempParams     colorTempParams;   // nullable
    // ... 等 10 种类型
}
```

### 访问模式（避免每帧 Tables 查询）

```csharp
// 序列播放前一次性加载，不在 Update 中重复查询
var sequenceConfig = Tables.Instance.TbNarrativeSequence.Get(sequenceId);
if (sequenceConfig == null)
{
    Log.Warning($"[Narrative] No sequence found for id: {sequenceId}");
    return;
}
```

### PuzzleNarrativeMap 查询

```csharp
// 响应 Evt_PerfectMatch 时查映射
var puzzleMap = Tables.Instance.TbPuzzleNarrativeMap.Get(puzzleId);
if (puzzleMap != null)
    TryEnqueue(puzzleMap.SequenceId);
```

---

## Out of Scope

- 序列内容的具体参数数值（由策划填写 Luban 表）
- Timeline 资源路径管理（属于 YooAsset 资产管理）
- 运行时修改配置（Luban 只读）

---

## QA Test Cases

### TC-001: 序列配置读取

**Given**: `TbNarrativeSequence` 中 id="ch1_puzzle1_complete" 的记录  
**When**: `Tables.Instance.TbNarrativeSequence.Get("ch1_puzzle1_complete")`  
**Then**: 返回非 null 对象，`effects.Count > 0`，`sequenceType = MemoryReplay`

### TC-002: SP-008 chain 字段

**Given**: `TbNarrativeSequence` 中 id="ch3_puzzle5_complete"  
**When**: 读取 `nextSequenceId`  
**Then**: 返回 "ch3_chapter_transition"，`nextSequenceDelay = 0.0`

### TC-003: PuzzleNarrativeMap 映射

**Given**: `TbPuzzleNarrativeMap` 中配置了 puzzleId=1001 → sequenceId="ch1_puzzle1_complete"  
**When**: `Tables.Instance.TbPuzzleNarrativeMap.Get(1001)`  
**Then**: 返回 "ch1_puzzle1_complete"

### TC-004: 不存在 ID 返回 null（不崩溃）

**Given**: `TbNarrativeSequence` 中不存在 id="invalid_id"  
**When**: `Tables.Instance.TbNarrativeSequence.Get("invalid_id")`  
**Then**: 返回 null，不抛出异常

### TC-005: 无硬编码 sequenceId（代码审查）

**Given**: NarrativeSequenceEngine 的所有源文件  
**When**: 搜索字符串字面量（如 "ch1_puzzle1_complete"）  
**Then**: 不存在任何硬编码的 sequenceId 字符串（sequenceId 只从事件 payload 或 config 表查询获得）

---

## Test Evidence

- **Unit Test**: `tests/unit/NarrativeEngine_LubanConfig_Test.cs`

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| Luban 工具链 | Build | 生成 TbNarrativeSequence, TbPuzzleNarrativeMap, TbChapterTransitionMap |
| SP-008 schema 设计 | Architecture | nextSequenceId/nextSequenceDelay 字段 |
| story-001 (Sequence Engine) | Consumer | 读取 TbNarrativeSequence |
| story-003 (Sequence Chain) | Consumer | 读取 nextSequenceId/nextSequenceDelay |
| story-005 (Puzzle to Narrative) | Consumer | 读取 TbPuzzleNarrativeMap |
