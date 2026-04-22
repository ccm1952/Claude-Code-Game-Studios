<!-- 该文件由Cursor 自动生成 -->
# Hint System — 提示系统

> **Status**: Draft
> **Author**: System Design Agent
> **Last Updated**: 2026-04-16
> **Last Verified**: 2026-04-16
> **Implements Pillar**: 克制表达 — 提示融入世界，不打断沉浸

## Summary

提示系统是《影子回忆》的防卡关安全网。它在玩家陷入困境时通过三层渐进引导——环境暗示、方向引导、显式提示——将玩家推向正确方向，而不是告诉答案。前两层完全融入场景氛围（玩家甚至不会意识到自己被帮助了），第三层由玩家主动请求。整套系统的设计目标是让玩家产生"我自己想到了"而非"游戏告诉我了"的感受。

> **Quick reference** — Layer: `Feature` · Priority: `MVP` · Key deps: `Shadow Puzzle System, TimerModule (TEngine), GameEvent (TEngine)`

## Overview

在《影子回忆》中，玩家通过摆放生活物件和调整光源来拼出关系影子。谜题没有时间压力、没有失败惩罚，但当玩家长时间找不到方向时，挫败感会破坏温柔的情绪基调。提示系统负责在不破坏沉浸感的前提下化解卡关——它监听玩家的操作行为和谜题匹配度，在合适的时机释放恰到好处的暗示。环境暗示像是房间里的光线在呼吸；方向引导像是影子在墙上若隐若现地预告自己的形状；只有当玩家主动按下提示按钮时，才会出现直接的文字与箭头指引。

## Player Fantasy

**"我不是被'帮助'了，而是突然'想到'了。"**

玩家在思考时注意到某个物件微微亮了一下、墙上的影子区域似乎闪过一个轮廓，于是灵光一现把杯子挪到了灯旁边——影子成形了。事后回想，玩家记住的是"我发现了"，而不是"游戏提示了我"。这种"无感帮助"是提示系统的最高设计目标。即便是 Layer 3 的显式提示，也应当让玩家感受到的是"一个温柔的建议"而非"被当成需要帮助的人"。

## Detailed Design

### Core Rules

**提示层级定义：**

1. **Layer 1 — 环境暗示 (Ambient Hint)**
   - 相关物件（距离目标位置最远的、或最关键的未就位物件）发出微弱的暖色光晕脉冲
   - 脉冲频率约 3 秒一次，持续 2 个脉冲后消退
   - 视觉上与场景光照融为一体，不带任何 UI 元素
   - 同时最多 1 个物件被暗示

2. **Layer 2 — 方向引导 (Directional Hint)**
   - 目标影子在投影接收面（墙面/地面）上短暂显示虚影轮廓
   - 虚影以极低透明度（alpha 0.08-0.12）出现，持续 2 秒后消散
   - 如果谜题涉及光源操作，光源轨道起始端也会出现微弱光点
   - 虚影只显示目标影子的局部片段（约 30%-50% 轮廓），不暴露完整答案

3. **Layer 3 — 显式提示 (Explicit Hint)**
   - 仅在玩家主动点击提示按钮后触发
   - 显示文字提示（如"试试移动这个杯子到灯的左边"）和方向箭头
   - 箭头从当前物件位置指向目标方向（不指向精确位置，而是大致方向）
   - 文字提示使用第一人称内心独白语气（"好像应该把这个放到那边……"）
   - 每个谜题最多使用 3 次 Layer 3

**触发条件规则：**

1. 无操作计时器（Idle Timer）：玩家未进行任何有效操作（拖拽、旋转、移动光源）时开始计时
2. 任何有效操作立即重置计时器
3. 失败计数器（Fail Counter）：每次操作使匹配度下降超过 0.05 视为一次"偏离操作"
4. 重复拖拽计数器：同一物件被反复拖拽（3 秒内拖拽-放下-拖拽同一物件）累计计数
5. 当前匹配度影响触发阈值——匹配度越低，提示越早出现

**层级之间的推进规则：**

1. Layer 1 → Layer 2 必须经过冷却期（默认 30 秒）
2. Layer 2 不会在 Layer 1 未触发的情况下直接出现（按序推进）
3. Layer 3 独立于 Layer 1/2，始终可通过按钮主动请求
4. 提示按钮在谜题 Active 状态下常驻显示，默认 30% opacity，30 秒未操作后开始渐变至 80% opacity（与 UI System 的 HintButton rampStart=30s 对齐）
5. 每次谜题重新进入（退出后返回）时，所有提示状态重置

**提示内容选择规则：**

1. 向 Shadow Puzzle System 只读查询当前各物件的 `anchorScore`
2. 选择 `anchorScore` 最低的物件作为提示目标
3. 如果多个物件评分相近（差值 < 0.1），选择 `anchorWeight` 最高的（更关键的）
4. Layer 3 的文字提示内容由 Luban 配置表定义，每个谜题预配置 3 条提示文本

### States and Transitions

| State | Entry Condition | Exit Condition | Behavior |
|-------|----------------|----------------|----------|
| **Idle** | 谜题未进入 Active 状态，或谜题 Complete | — | 所有提示逻辑停止，不监听任何事件 |
| **Observing** | 谜题进入 Active 状态 | 进入 Layer1Active / Layer3Ready / Idle | 启动计时器和行为监听，静默收集数据。无任何可见反馈 |
| **Layer1Active** | 满足 Layer 1 触发条件 | 玩家操作（→ Cooldown）/ 谜题 Complete（→ Idle） | 播放物件光晕脉冲动画。玩家操作后立即停止脉冲并进入冷却 |
| **Layer2Active** | 在 Cooldown 后仍满足 Layer 2 触发条件 | 玩家操作（→ Cooldown）/ 谜题 Complete（→ Idle） | 在投影面显示虚影轮廓。玩家操作后虚影消散并进入冷却 |
| **Layer3Ready** | Layer3 提示按钮可用（按钮高亮）且玩家剩余提示次数 > 0 | 玩家点击提示按钮（→ Layer3Active） | 按钮由低调变为可交互高亮状态。此状态可与 Observing / Cooldown 并行 |
| **Layer3Active** | 玩家点击提示按钮 | 提示动画播放完毕（→ Observing）/ 谜题 Complete（→ Idle） | 显示文字提示 + 方向箭头，持续 5 秒后渐隐。提示次数 -1 |
| **Cooldown** | Layer1Active 或 Layer2Active 被玩家操作打断 | 冷却计时结束（→ Observing） | 所有被动提示暂停，计时 30 秒。期间玩家操作不影响冷却倒计时 |

**状态转移图：**

```
                    ┌──────────────────────────────────────────┐
                    │              谜题 Complete                │
                    │          (任意状态 → Idle)                │
                    └──────────────────────────────────────────┘

┌──────┐  谜题Active  ┌───────────┐  触发条件1  ┌──────────────┐
│ Idle │ ──────────→ │ Observing │ ─────────→ │ Layer1Active │
└──────┘             └───────────┘             └──────┬───────┘
   ↑                    ↑    ↑                        │ 玩家操作
   │                    │    │                        ↓
   │                    │    │    冷却结束     ┌──────────┐
   │                    │    └───────────────│ Cooldown │
   │                    │                     └────┬─────┘
   │                    │                          │ 冷却后仍满足触发条件2
   │                    │     触发条件2             ↓
   │                    │  ┌───────────── ┌──────────────┐
   │                    │  │              │ Layer2Active │
   │                    │  │              └──────┬───────┘
   │                    │  │                     │ 玩家操作
   │                    │  │                     ↓
   │                    │  └──────────────→ Cooldown
   │                    │
   │                    │    Layer3 独立轨道:
   │                    │    ┌────────────┐  点击按钮  ┌──────────────┐
   │                    │    │Layer3Ready │ ────────→ │ Layer3Active │
   │                    │    └────────────┘           └──────┬───────┘
   │                    │         ↑                          │ 播放完毕
   │                    │         └──────────────────────────┘
   │                    │
   └────────────────────┘
```

**关键细节：**
- Layer3Ready 是一个并行状态位（flag），不排斥 Observing / Cooldown / Layer1Active / Layer2Active
- Layer3Active 会中断当前的 Layer1/Layer2 表现（停止光晕/虚影），但不重置它们的进度
- 从 Layer3Active 返回后回到 Observing，被动提示进度保留

### Interactions with Other Systems

**与 Shadow Puzzle System 的交互（只读查询）：**
- 查询接口：`IPuzzleStateQuery`
  - `GetCurrentMatchScore() → float`：当前总匹配度 [0.0, 1.0]
  - `GetAnchorScores() → Dictionary<string, float>`：各物件锚点评分
  - `GetOperableObjects() → List<ObjectId>`：当前可操作物件列表
  - `GetPuzzlePhase() → PuzzlePhase`：当前谜题阶段（Active/NearMatch 等）
  - `IsAbsencePuzzle() → bool`：是否为缺席型谜题
- 数据流向：单向（Hint ← Puzzle），Hint 不写入 Puzzle 任何状态
- 查询频率：每 1 秒轮询一次（非每帧），降低性能开销

**与 TimerModule (TEngine) 的交互：**
- 使用 `GameModule.Timer` 创建和管理以下计时器：
  - 无操作计时器（Idle Timer）
  - 冷却计时器（Cooldown Timer）
  - 提示按钮延迟显示计时器
- 计时器在谜题 Complete 或退出时统一销毁

**与 GameEvent (TEngine) 的交互：**
- 监听事件：
  - `PuzzleStateChangedEvent`：谜题状态变更（Active/NearMatch/Complete）
  - `ObjectOperatedEvent`：物件操作（拖拽/旋转/放下）
  - `MatchScoreChangedEvent`：匹配度变化
- 发送事件：
  - `HintTriggeredEvent(hintLayer, targetObjectId)`：提示触发（供 Analytics 采集）
  - `HintConsumedEvent(puzzleId, layer3Count)`：Layer 3 使用（供存档记录）

**与 UI System 的交互：**
- Layer 3 提示按钮由 UI System 管理显示/隐藏/高亮状态
- Hint System 通过 `HintButtonStateEvent` 通知 UI 按钮状态变更
- UI 按钮点击通过 `RequestExplicitHintEvent` 回调 Hint System

**与 Tutorial / Onboarding 的交互：**
- 教学激活时：Tutorial 通知 Hint System 暂停所有计时器（idleTimer、failCount、repeatDragCount 冻结）
- 教学完成后恢复时：Hint System 收到 Tutorial 恢复通知，**idleTimer、failCount、repeatDragCount 全部重置为 0**（不延续教学期间的累积值）。设计意图：教学刚完成时玩家才开始真正探索谜题，从教学期间累积的时间不应算作"卡关"
- 教学期间 Hint Layer 1/2 不触发（计时器已暂停）

**与 Narrative Event System 的交互（预留）：**
- Hint 不直接与叙事系统交互
- 但 Layer 1/2 的视觉效果需要与当前场景氛围保持一致（光晕颜色、虚影风格）
- 叙事系统控制的场景色温变化会影响 Hint 视觉参数（通过共享的场景氛围配置）

## Formulas

### Hint Trigger Threshold — 提示触发阈值

Layer 1 和 Layer 2 的触发采用综合评分机制，当 `triggerScore ≥ 1.0` 时触发对应层级：

```
triggerScore = timeScore + failScore + stagnationScore + matchPenalty
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| timeScore | float | 0.0-1.0 | runtime | 无操作时间贡献分，`min(idleTime / idleThreshold, 1.0)` |
| failScore | float | 0.0-0.6 | runtime | 偏离操作贡献分，`min(failCount * failWeight, 0.6)` |
| stagnationScore | float | 0.0-0.3 | runtime | 重复拖拽贡献分，`min(repeatDragCount * repeatWeight, 0.3)` |
| matchPenalty | float | 0.0-0.4 | runtime | 低匹配度加速项（见下方公式） |

**Expected output range**: 0.0 to 2.3（但触发阈值为 1.0）

**子公式 — matchPenalty（匹配度惩罚/加速项）：**

```
matchPenalty = matchScore < matchLowThreshold
             ? (1.0 - matchScore / matchLowThreshold) * matchPenaltyMax
             : 0.0
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| matchScore | float | 0.0-1.0 | Shadow Puzzle System | 当前总匹配度 |
| matchLowThreshold | float | 0.40 | config | 低于此值视为"远离答案"（与 NearMatch 阈值对齐，确保所有未进入 NearMatch 的状态都获得加速） |
| matchPenaltyMax | float | 0.4 | config | matchPenalty 最大值 |

当匹配度为 0 时，`matchPenalty = 0.4`（最大加速）；匹配度 ≥ 0.40 时，`matchPenalty = 0`（不加速）。

**补充触发因子 — stagnationScore（停滞加速项）：**

专门针对"matchScore 在 0.30-0.40 区间持续微调但无显著进展"的卡关状态：

```
stagnationScore = (matchScore 在 ±0.05 范围内连续 stagnationDuration 秒无显著变化)
               ? stagnationBonus
               : 0.0
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| stagnationDuration | float | 30s | config | matchScore 需要在 ±0.05 范围内停滞多久才触发 |
| stagnationBonus | float | 0.3 | config | 停滞时额外贡献到 triggerScore 的分数 |

> **设计意图**：解决 matchScore 0.30-0.40 区间的提示盲区——此时 matchPenalty=0（分数不够低），idleTimer 被操作持续重置，failScore 也不高。stagnationScore 检测的是"有操作但没实质进展"，与 idleTimer 检测的"完全无操作"互补。

**Layer 1 默认参数：**

| Parameter | Value | Description |
|-----------|-------|-------------|
| idleThreshold | 45s | 无操作多久贡献满分 |
| failWeight | 0.12 | 每次偏离操作的权重 |
| repeatWeight | 0.1 | 每次重复拖拽的权重 |

**Layer 2 默认参数：**

| Parameter | Value | Description |
|-----------|-------|-------------|
| idleThreshold | 90s | 较长的等待时间 |
| failWeight | 0.08 | 权重略低（已给过 Layer 1 暗示） |
| repeatWeight | 0.06 | 权重略低 |

**Edge case**：如果 `matchScore > 0.4`（已进入 NearMatch），Layer 1 不触发（玩家已在正确方向上）。

**hintDelayOverride 章节缩放：**

TbPuzzle 配置表的 `hintDelayOverride` 字段为各章节延迟提供乘数。实际 idleThreshold = 默认 idleThreshold × hintDelayOverride。建议值：

| 章节 | hintDelayOverride | Layer 1 实际 idleThreshold | Layer 2 实际 idleThreshold | 理由 |
|------|------------------|--------------------------|--------------------------|------|
| Ch.1-2 | 1.0（默认） | 45s | 90s | 简单谜题，标准延迟 |
| Ch.3 | 1.3 | 58s | 117s | 物件数增加，合理思考时间 3-8min |
| Ch.4 | 1.5 | 67s | 135s | 影子柔化增加视觉判定难度 |
| Ch.5 | 1.5 | 67s | 135s | 复杂谜题 + 缺席概念需要更多思考空间 |

> **设计意图**：后期章节玩家可能在脑中推演多物件关系而非动手操作，45s idleThreshold 会误判为"卡关"。延长延迟保护"我自己想到了"的体验。

### Cooldown Duration — 冷却时间

冷却时间在 Layer1 → Layer2 之间是固定值，但可根据玩家整体表现微调：

```
actualCooldown = baseCooldown * cooldownModifier

cooldownModifier = clamp(1.0 + (matchScore - 0.3) * 0.5, 0.7, 1.3)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| baseCooldown | float | 30s | config | 基础冷却时间 |
| cooldownModifier | float | 0.7-1.3 | runtime | 基于匹配度的修正系数 |
| matchScore | float | 0.0-1.0 | Shadow Puzzle System | 当前匹配度 |

**Expected output range**: 21s（匹配度极低时缩短冷却）到 39s（匹配度较高时延长冷却）

**设计意图**：匹配度很低意味着玩家完全没有方向，应更快给出下一层提示；匹配度接近 NearMatch 说明玩家在正确轨道上，可以多给一些自主探索时间。

### Hint Target Selection — 提示目标选择

```
targetObject = argmin_i(anchorScore_i) where anchorWeight_i >= minWeight

// 如果多个物件评分相近，选择权重最高的
if |anchorScore_best - anchorScore_second| < tieThreshold:
    targetObject = argmax(anchorWeight among tied objects)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| anchorScore_i | float | 0.0-1.0 | Shadow Puzzle System | 各物件的锚点评分 |
| anchorWeight_i | float | 0.1-1.0 | PuzzleAnchor 配置 | 各物件的权重 |
| minWeight | float | 0.1 | config | 忽略权重过低的装饰物件 |
| tieThreshold | float | 0.1 | config | 评分差值小于此值视为"相近" |

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|----------|------------------|-----------|
| **缺席型谜题（第五章）** | `IsAbsencePuzzle() == true` 时：Layer 1/2 正常触发但引导目标为"当前可达到的最优解（maxCompletionScore）"而非完美解；Layer 3 提示次数保持 3 次（与标准谜题一致），但文案完全重写为引导"接受"的内容（如"也许这个影子本来就不该完整……"、"有些东西走了之后，位置就空了……"、"不是所有影子都能被拼回来……"），不暗示存在更好答案。当 matchScore >= maxCompletionScore 时，Layer 3 文案切换为鼓励类型（如"也许……这就是它该有的样子"），引导玩家停止操作以触发 AbsenceAccepted 判定 | 缺席型谜题是玩家最困惑的时刻——前 4 章建立的"继续尝试就能成功"心理模型在此被颠覆。应通过提示**内容**而非**数量限制**来传达主题，保持安全网不削弱 |
| **玩家频繁切换应用（后台/前台）** | 每次 `OnApplicationPause(true)` 时暂停所有计时器；`OnApplicationPause(false)` 时恢复计时器但扣除超过 5 分钟的部分（即后台超 5 分钟视为"新的开始"，计时器重置为 0） | 移动端玩家频繁中断是常态；长时间离开后回来不应立刻收到提示（需要重新熟悉场景） |
| **玩家在 Layer1 提示期间进行了操作但匹配度反而下降了** | 操作重置计时器，但 failCount +1。如果操作后匹配度下降超过 0.1，额外 failCount +1（重大偏离） | 玩家"响应了暗示但方向错了"，需要更快推进到 Layer 2 |
| **Layer 3 提示次数用完后玩家仍然卡关** | Layer 1/2 继续正常触发（不受 Layer 3 限制）；Layer 2 的虚影范围从 30%-50% 扩大到 50%-70% | Layer 3 限额防止过度依赖，但不能完全放弃帮助 |
| **玩家已进入 NearMatch 状态** | Layer 1 不触发（玩家已在正确方向）；Layer 2 修改为只引导剩余未到位的物件；Layer 3 提示文本切换为"几乎对了"类型的鼓励而非方向指引 | NearMatch 说明玩家已理解大方向，过度提示反而破坏成就感 |
| **谜题只有 1 个可操作物件** | Layer 1 仍然触发（光晕提示该物件需要移动）；Layer 2 直接显示目标位置的方向暗示（因为物件选择没有悬念）；Layer 3 提示聚焦于操作方式（"试试旋转"而非"移动这个"） | 简单谜题的提示应聚焦"怎么操作"而非"操作哪个" |
| **玩家在提示动画播放期间退出谜题** | 立即停止所有提示动画和计时器，状态回到 Idle。不记录未完成的提示为"已使用" | 干净退出，不消耗提示次数 |
| **Layer 1 和 Layer 3 同时触发（玩家在光晕脉冲时按下提示按钮）** | Layer 3 优先，立即停止 Layer 1 光晕，显示 Layer 3 内容 | 主动请求优先于被动暗示 |
| **网络/性能卡顿导致计时器跳跃** | 计时器每帧增量封顶为 1 秒（`deltaTime = min(Time.unscaledDeltaTime, 1.0)`） | 防止一帧跳过大量时间导致提示连续触发 |

## Dependencies

| System | Direction | Nature of Dependency |
|--------|-----------|---------------------|
| Shadow Puzzle System | This depends on (只读) | 查询匹配度、物件锚点评分、谜题阶段、缺席型谜题标记 |
| TimerModule (TEngine) | This depends on | 使用计时器管理无操作计时、冷却计时 |
| GameEvent (TEngine) | Bidirectional | 监听谜题/操作事件，发送提示触发事件 |
| UI System | This triggers UI | 通知提示按钮状态变更，接收按钮点击回调 |
| Luban Config | This depends on | 读取 Layer 3 文字提示内容、各谜题的提示参数覆盖 |
| Tutorial / Onboarding | Onboarding depends on this | 教学系统复用 Hint 的 Layer 1 机制引导首次操作 |
| Analytics (预留) | Analytics depends on this | 采集提示触发频率、Layer 3 使用次数等数据 |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|-----------|--------------|------------|-------------------|-------------------|
| layer1_idleThreshold | 45s | 20-90s | 玩家有更多自主探索时间，但卡关时间也更长 | 更早给出暗示，减少挫败感但可能让观察型玩家觉得多余 |
| layer2_idleThreshold | 90s | 45-180s | 更长的自主期，但可能导致弃玩 | 更快引导，但可能在玩家快要自己想到时打断思路 |
| failWeight_L1 | 0.12 | 0.05-0.2 | 操作错误更快触发提示 | 操作错误对提示的影响更小 |
| failWeight_L2 | 0.08 | 0.03-0.15 | Layer 2 对错误操作更敏感 | 错误操作更多地由时间来触发 Layer 2 |
| repeatWeight | 0.1 | 0.03-0.2 | 反复拖拽同一物件更快触发提示 | 重复操作不太影响提示节奏 |
| matchLowThreshold | 0.3 | 0.15-0.5 | 更多玩家被视为"远离答案"，提示更积极 | 只有完全迷失的玩家才会获得加速提示 |
| matchPenaltyMax | 0.4 | 0.2-0.6 | 低匹配度对提示加速效果更强 | 匹配度对提示节奏影响更温和 |
| baseCooldown | 30s | 15-60s | Layer 之间间隔更长，体验更安静 | 连续提示层级更紧凑 |
| layer3_maxCount | 3 | 1-5 | 允许更多主动提示，降低卡关率 | 限制依赖，鼓励自主探索 |
| layer3_absenceMaxCount | 2 | 1-3 | 缺席型谜题的主动提示上限 | 更少提示强化"无解"的情绪体验 |
| hintButton_fadeInDelay | 60s | 30-120s | 提示按钮更晚变为高亮，鼓励先自己尝试 | 更早显示可用，降低发现提示功能的门槛 |
| ghostOutline_coverage | 0.3-0.5 | 0.2-0.7 | 虚影显示更完整，引导更直接 | 虚影更模糊，保留更多探索空间 |
| ambientPulse_interval | 3s | 2-5s | 光晕脉冲更频繁，更容易注意到 | 脉冲更稀疏，更隐蔽但可能被忽略 |
| ambientPulse_count | 2 | 1-4 | 更多脉冲次数增加被注意到的概率 | 更少脉冲更隐蔽 |

## Visual/Audio Requirements

| Event | Visual Feedback | Audio Feedback | Priority |
|-------|----------------|---------------|----------|
| Layer 1 光晕脉冲 | 目标物件边缘发出暖色光晕（与场景灯光同色系），alpha 从 0 渐变到 0.15 再回到 0，周期 3s | 无声（完全融入环境） | MVP |
| Layer 1 光晕消退 | 光晕在 0.5s 内渐隐至 0 | 无声 | MVP |
| Layer 2 虚影出现 | 投影面上出现目标影子的半透明局部轮廓（alpha 0.08-0.12），带轻微边缘模糊 | 极轻微的环境音变化（如远处风声微弱变调），几乎不可感知 | MVP |
| Layer 2 虚影消散 | 虚影在 1s 内从边缘向中心溶解消失 | 环境音恢复 | MVP |
| Layer 3 提示出现 | 目标物件上方显示柔和的方向箭头（手绘风格，非锐利 UI），箭头带微弱呼吸动画；屏幕下方浮现内心独白文字 | 柔和的单音提示音（钢琴单键或音叉） | MVP |
| Layer 3 提示消失 | 箭头和文字在 1s 内渐隐 | 无声 | MVP |
| 提示按钮由低调变高亮 | 按钮从 alpha 0.3 渐变到 1.0，轻微放大动画 | 无声 | MVP |
| Layer 3 次数耗尽 | 提示按钮变灰并缩小，tooltip 显示"已无更多提示" | 无声 | Vertical Slice |

## Game Feel

### Feel Reference

提示出现的感觉应该像 **《纪念碑谷》中的乌鸦 Totem** — 当你卡住时，它会温和地出现在正确的位置附近，你不会觉得被"教"了，而是觉得"哦，原来这里有个线索"。

**反面参考**：不应该像《Zelda》系列 Navi 的"Hey! Listen!"——频繁打断、明确告知、让人产生被催促感。

环境暗示应该感觉像 **《Limbo》中场景光线的微妙变化** — 你事后才意识到那个光线变化其实是在引导你。

### Input Responsiveness

| Action | Max Input-to-Response Latency (ms) | Frame Budget (at 60fps) | Notes |
|--------|-----------------------------------|------------------------|-------|
| 提示按钮点击 | 100ms | 6 frames | 包含按钮动画启动 |
| Layer 1 光晕开始 | 500ms | 30 frames | 刻意延迟，渐变出现不突兀 |
| Layer 2 虚影开始 | 300ms | 18 frames | 轻微延迟，像影子自然浮现 |
| Layer 3 箭头出现 | 200ms | 12 frames | 响应稍快（玩家主动请求） |
| 提示被操作打断消失 | 150ms | 9 frames | 快速消退，不遮挡操作反馈 |

### Animation Feel Targets

| Animation | Startup Frames | Active Frames | Recovery Frames | Feel Goal | Notes |
|-----------|---------------|--------------|----------------|-----------|-------|
| Layer 1 光晕脉冲 | 30 (0.5s 渐入) | 60 (1s 峰值) | 30 (0.5s 渐出) | 像呼吸一样自然 | 使用 SmoothStep 曲线 |
| Layer 2 虚影浮现 | 18 (0.3s 渐入) | 120 (2s 显示) | 60 (1s 溶解) | 像记忆在墙上闪现 | 边缘模糊 → 中心溶解 |
| Layer 3 箭头 | 12 (0.2s) | 300 (5s 持续) | 60 (1s 渐隐) | 柔和的指引，不急促 | 箭头带微弱呼吸缩放 |
| 提示按钮高亮化 | 60 (1s 渐变) | 持续 | 0 | 不经意间变亮 | 线性渐变 |

### Impact Moments

| Impact Type | Duration (ms) | Effect Description | Configurable? |
|-------------|--------------|-------------------|---------------|
| Layer 2 虚影最清晰瞬间 | 200ms | 虚影 alpha 短暂升至 0.15（比正常高 25%）然后回落 | Yes |
| Layer 3 文字出现 | 300ms | 文字从模糊到清晰，模拟"想起来了"的感觉 | Yes |
| 提示后玩家成功 | — | 不做任何特殊处理（与正常成功一致，不让玩家觉得"是提示帮的"） | N/A |

### Weight and Responsiveness Profile

- **Weight**: 极轻。提示是空气一样的存在——出现时不引起注意力的突变，消失时不留痕迹。
- **Player control**: 高度可控。Layer 1/2 可在设置中关闭（默认开启）；Layer 3 完全由玩家主动触发。
- **Snap quality**: 柔和渐变，没有任何硬切或突变。所有提示效果使用 SmoothStep 或类似缓动曲线。
- **Acceleration model**: 渐入渐出（simulation feel）。提示不会突然出现或消失，始终有过渡。
- **Failure texture**: 无失败感。提示次数用完不显示任何负面反馈，只是按钮安静地变灰。

### Feel Acceptance Criteria

- [ ] Layer 1 光晕被 60%+ 测试者在事后问卷中描述为"自然的光线变化"而非"游戏提示"
- [ ] Layer 2 虚影被 70%+ 测试者理解为"影子该去的方向"
- [ ] Layer 3 文字不被任何测试者形容为"打断"或"多余"
- [ ] 使用 Layer 1/2 后成功的玩家中，80%+ 认为是"自己想到的"
- [ ] 提示按钮在需要时能被 90%+ 测试者找到
- [ ] 缺席型谜题中，提示内容不引导玩家追求不存在的完美解
- [ ] Tutorial 系统教学激活期间，所有被动提示计时器暂停，不触发 Layer 1/Layer 2 提示
- [ ] 教学完成后 Hint 计时器从 0 重新开始，不延续教学期间的累积时间

## UI Requirements

| Information | Display Location | Update Frequency | Condition |
|-------------|-----------------|-----------------|-----------|
| 提示按钮 | 屏幕右下偏上（GameHUD 内 RightPanel） | 状态变更时 | 谜题 Active 后 30s 开始从半透明渐变到可交互高亮（rampStart=30s, rampDuration=10s） |
| Layer 3 剩余次数 | 提示按钮角标（小圆点） | 使用时 | 剩余 ≥ 1 时显示数字，用完后按钮变灰 |
| Layer 3 文字提示 | 屏幕下方 1/4 区域，半透明黑底 | 触发时 | Layer 3 触发后显示 5 秒，渐隐消失 |
| Layer 3 方向箭头 | 目标物件上方/旁边 | 触发时 | 与文字提示同步出现和消失 |
| 冷却指示器 | 不显示 | — | 冷却是隐藏机制，玩家不感知 |
| 提示关闭选项 | 设置菜单内 | — | 允许关闭 Layer 1/2 被动提示（Layer 3 按钮始终可用） |

## Cross-References

| This Document References | Target GDD | Specific Element Referenced | Nature |
|--------------------------|-----------|----------------------------|--------|
| 查询当前匹配度和锚点评分 | `design/gdd/shadow-puzzle-system.md` | `matchScore` 输出值、`anchorScore` 输出值 | Data dependency |
| 匹配度 NearMatch 阈值（0.40）用于抑制 Layer 1 | `design/gdd/shadow-puzzle-system.md` | NearMatch threshold (0.40) | Rule dependency |
| 缺席型谜题标记 | `design/gdd/shadow-puzzle-system.md` | IsAbsencePuzzle 属性 | Data dependency |
| 谜题状态变更事件 | `design/gdd/shadow-puzzle-system.md` | PuzzleStateChangedEvent | State trigger |
| 物件操作事件 | `design/gdd/shadow-puzzle-system.md` | ObjectOperatedEvent | State trigger |
| 提示触发数据供分析 | `design/gdd/analytics.md` (planned) | 提示使用率统计 | Ownership handoff |
| Layer 1 机制复用于教学 | `design/gdd/tutorial-onboarding.md` | 首次引导流程 | Rule dependency |
| Layer 3 文字配置 | Luban 配置表 | HintTextConfig | Data dependency |

## Acceptance Criteria

- [ ] 三层提示在 MVP 的 3 个谜题中均可正确触发和显示
- [ ] Layer 1 光晕在移动端不引起额外 draw call（使用 material property 而非独立粒子）
- [ ] Layer 2 虚影渲染开销 < 0.5ms/帧
- [ ] 无操作 45 秒后 Layer 1 在 ±2 秒内触发（计时器精度验证）
- [ ] Layer 3 显式提示最多使用 3 次后按钮正确变灰
- [ ] 缺席型谜题使用特殊提示文案，不暗示存在完美答案
- [ ] 玩家切后台超过 5 分钟后返回，计时器正确重置
- [ ] Cooldown 期间不触发任何被动提示
- [ ] 提示系统不向 Shadow Puzzle System 写入任何状态（只读验证）
- [ ] 所有阈值和时间参数通过 Luban 配置表驱动，无硬编码
- [ ] 提示按钮在 iPhone 13 Mini（小屏）上可触达且不遮挡操作区域
- [ ] Performance: 提示系统每帧更新完成在 0.5ms 以内
- [ ] 在 NearMatch 状态下 Layer 1 正确抑制不触发

## Open Questions

| Question | Owner | Deadline | Resolution |
|----------|-------|----------|-----------|
| Layer 2 虚影使用投影纹理还是 UI 叠加层渲染？需与 URP Shadow Rendering 的技术方案对齐 | TA / Graphics | URP 方案确定后 | 待 URP Shadow Rendering 原型结果决定 |
| Layer 1 光晕颜色是否随章节氛围变化？（如第四章使用冷色调） | Art Director | Vertical Slice | 建议是——每章定义 hintTintColor 配置项 |
| Layer 3 的文字提示是否需要本地化？MVP 阶段只做中文还是同步英文？ | Producer | MVP 前 | 建议 MVP 中文 only，Alpha 加入英文 |
| 是否需要提供"完全关闭所有提示"的硬核模式？ | Game Design | Vertical Slice | 建议 Vertical Slice 作为设置选项加入 |
| Analytics 需要采集哪些提示相关数据？层级触发次数、触发到操作的时间间隔、使用提示后的成功率？ | Data Analyst | Alpha | 待 Analytics 系统设计时一并定义 |
| 第五章多个缺席型谜题之间，提示系统是否需要记住"玩家已经理解缺席概念"并调整文案？ | Game Design / Narrative | Alpha | 建议第五章第二个缺席谜题起减少提示频率和文案量 |
