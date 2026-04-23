// 该文件由Cursor 自动生成

# SP-008 Findings: 末章合并序列 Luban 配置表达方案

> **Status**: ✅ 设计决策完成
> **Date**: 2026-04-22

## 结论

**采用方案 A: Sequence Chain（双 sequence + nextSequenceId 链接）。**

## 决策分析

| 维度 | 方案 A: Sequence Chain | 方案 B: 合并长 Sequence |
|------|----------------------|----------------------|
| 配置复用性 | ✅ Chapter Transition 可跨章复用 | ❌ 每章需独立配置 |
| 无缝过渡 | ✅ `nextSequenceDelay = 0` 实现零间断 | ✅ 天然无间断 |
| 策划可维护性 | ✅ 模块化，改过渡不影响完成序列 | ⚠️ 长 sequence 编辑困难 |
| 实现复杂度 | ⚠️ 需 chain 逻辑（+0.5d） | ✅ 无额外逻辑 |
| 调试友好度 | ✅ 可单独测试每段 | ❌ 需从头播放 |

### 选择方案 A 的理由

1. **5 章共享 Chapter Transition 模板**: 每章的结束过渡（淡出 → 加载下一章 → 淡入）逻辑一致，只需参数化 `targetChapterId`
2. **末章特殊处理**: 最终章 chain 到 Credits/MainMenu，与普通章过渡配置独立
3. **调试效率**: 可以单独触发 `ch5_chapter_transition` 测试过渡效果

## Luban Schema 设计

### TbNarrativeSequence 新增字段

```lua
-- 新增字段
nextSequenceId = "",         -- 链接下一个 sequence（空 = 不链接）
nextSequenceDelay = 0.0,     -- 链接间隔（秒），0 = 无缝衔接
```

### 配置示例

```lua
-- 普通谜题完成序列（不链接）
{
    id = "ch1_puzzle3_complete",
    effects = { { t=0.0, type="AudioSting", clipId="puzzle_complete" }, ... },
    nextSequenceId = "",
    nextSequenceDelay = 0.0
}

-- 章末最后谜题完成序列（链接到章过渡）
{
    id = "ch3_puzzle5_complete",
    effects = { { t=0.0, type="AudioDucking", ... }, ... },
    nextSequenceId = "ch3_chapter_transition",
    nextSequenceDelay = 0.0
}

-- 章节过渡序列（复用模板）
{
    id = "ch3_chapter_transition",
    effects = {
        { t=0.0, type="ScreenFade", fadeType="out", duration=1.5 },
        { t=1.5, type="LoadScene", targetChapterId=4 },
        { t=3.0, type="ScreenFade", fadeType="in", duration=1.5 }
    },
    nextSequenceId = "",
    nextSequenceDelay = 0.0
}

-- 最终章结束（链接到 Credits）
{
    id = "ch5_puzzle5_complete",
    effects = { ... },
    nextSequenceId = "credits_sequence",
    nextSequenceDelay = 0.0
}
```

## Narrative Engine 实现要求

```csharp
// SequenceComplete 时检查 chain
private void OnSequenceComplete(string sequenceId)
{
    var config = Tables.TbNarrativeSequence.Get(sequenceId);
    if (!string.IsNullOrEmpty(config.NextSequenceId))
    {
        if (config.NextSequenceDelay <= 0)
            PlaySequence(config.NextSequenceId);
        else
            DelayedPlaySequence(config.NextSequenceId, config.NextSequenceDelay);
    }
    else
    {
        GameEvent.Send(EventId.Evt_SequenceComplete, sequenceId);
    }
}
```

## ADR-016 影响

新增 "Sequence Chain" 小节，描述 `nextSequenceId` + `nextSequenceDelay` 机制。
