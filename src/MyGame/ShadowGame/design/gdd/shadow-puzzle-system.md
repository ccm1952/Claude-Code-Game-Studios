# Shadow Puzzle System — 影子谜题系统

> **Status**: Draft
> **Author**: Game Design Agent
> **Last Updated**: 2026-04-16
> **Last Verified**: 2026-04-16
> **Implements Pillar**: 关系即谜题 / 日常即重量

## Summary

影子谜题系统是《影子回忆》的核心玩法系统。玩家通过在场景中摆放、旋转生活物件并调整光源位置，使多个物件在光照下共同投射出一个有意义的"关系影子"。这套系统的独特之处在于：成功的影子不是某个孤立物体的轮廓，而是多个物件处于正确的关系位置时才能成立的组合投影。

> **Quick reference** — Layer: `Feature` · Priority: `MVP` · Key deps: `Input System (via Object Interaction)`, `Object Interaction System`, `Chapter State System`, `URP Shadow Rendering`

## Overview

玩家面对一个温暖的室内场景，场景中散落着若干生活物件（台灯、杯子、椅子、雨伞等）和一个光源。墙面或地面作为投影接收面。玩家通过触屏拖拽物件位置、双指旋转物件角度、以及移动光源，来改变墙面上的影子组合。当物件之间的关系满足预设条件时，影子会逐渐趋近目标轮廓（NearMatch），最终精确成形（PerfectMatch），触发一段记忆重现的叙事演出。

## Player Fantasy

玩家感受到的是"重新拼出一段已经失去的共同记忆"。不是解开一道数学题的智力满足，而是"原来这两个普通的东西放在一起，就是我们曾经拥有的那个世界的一部分"的情感触动。操作物件时的手感应该是温柔的、有重量的，像是在小心翼翼地整理一个人离开后留下的遗物。

## Detailed Design

### Core Rules

**物件操作规则：**

1. 场景中的可操作物件用高亮轮廓标记，玩家触摸即可选中
2. 选中物件后支持三种操作：
   - **拖拽移动**：在场景允许的平面范围内自由移动位置
   - **旋转**：绕物件自身 Y 轴旋转（部分物件支持 X 轴倾斜）
   - **距离调整**：沿光源方向前后移动，改变投影大小
3. 所有物件操作为离散格点吸附（Snap）而非纯自由连续移动，降低精确操作挫败感
4. 每个谜题有明确的可操作物件集合，非操作物件为固定场景装饰

**光源操作规则：**

1. MVP 阶段每个谜题仅有 1 个光源
2. 光源支持沿预设轨道移动（弧线或直线），不支持完全自由移动
3. 光源位置变化直接影响所有物件的投影位置、大小和方向
4. 第一章不引入光源操作，第二章起逐步开放

**Ch.2 谜题编排约束（逐步引入原则）：**

每次只引入一个新变量，避免同时增加物件数量和光源操控造成难度跳变：
- Ch.2 Puzzle 1-3：3 物件 + 固定光源（先适应多物件空间关系）
- Ch.2 Puzzle 4：2 物件 + 可操作光源（回到简单物件数，专注学习光源操控）— `tut_light` 在此触发
- Ch.2 Puzzle 5-6：3 物件 + 可操作光源（两个维度合并，但玩家已分别熟悉）

**影子匹配判定规则：**

1. 每个谜题有一个预设的目标影子轮廓（由设计师定义）
2. 判定采用"作者预设 + 屏幕空间锚点"混合方案：
   - 每个物件上标记 1-3 个 PuzzleAnchor（关键投影点）
   - 运行时将锚点投影到屏幕空间，计算与目标位置的距离和方向差
   - 同时检查物件之间的相对关系（间距比、角度差、遮挡关系）
3. 匹配程度分三级：
   - **NoMatch** (匹配度 < 40%)：无特殊反馈
   - **NearMatch** (匹配度 40%-85%)：影子轮廓开始发光提示，物件边缘出现微弱指引
   - **PerfectMatch** (匹配度 > 85%)：触发吸附动画和成功演出
4. PerfectMatch 触发后，物件自动吸附到精确位置，播放成功演出

**谜题组织规则：**

1. 每章包含 5-8 个谜题，线性推进
2. 每个谜题完成后解锁下一个谜题，不可跳过
3. 每章最后一个谜题为"终局影子"，复杂度和情绪强度最高
4. 完成一章后可自由回放该章任意谜题

**Collectible（收集物）与主线进度的关系：**

收集物（记忆碎片等）仅在谜题完成后的演出阶段或场景探索中被动解锁，不参与谜题匹配度计算、不影响章节解锁条件、不产生任何"你漏掉了什么"的暗示。设计意图：谜题的唯一驱动力是光影匹配的发现感（对齐 P2 "情绪驱动"），收集物是奖励性叙事补充而非进度竞争目标。

### States and Transitions

**谜题级状态机：**

| State | Entry Condition | Exit Condition | Behavior |
|-------|----------------|----------------|----------|
| **Locked** | 前序谜题未完成 | 前序谜题 Complete | 场景不可见/灰显 |
| **Idle** | 前序完成，玩家未进入 | 玩家触摸任意操作物件 | 场景可见，物件初始位置，无交互 |
| **Active** | 玩家首次操作物件 | 进入 NearMatch 或 Complete | 物件可操作，持续计算匹配度 |
| **NearMatch** | 匹配度达到 40% | 匹配度降回 < 35% (回退 Active) 或 达到 85% (进入 PerfectMatch) | 影子轮廓发光提示，轻微震动反馈 |
| **PerfectMatch** | 匹配度 > 85%（非缺席谜题） | 自动吸附完成 → Complete | 物件吸附到精确位置，播放定格动画。**进入 PerfectMatch 后停止 matchScore 计算（不可逆转换）** |
| **AbsenceAccepted** | `isAbsencePuzzle && matchScore >= maxCompletionScore && 在该分数停留 >= absenceAcceptDelay(5s) 无操作` | 缺席专用演出完成 → Complete | 不执行标准吸附，触发 `AbsenceAcceptedEvent` 驱动缺席专用演出 |
| **Complete** | 吸附动画/缺席演出结束 | — | 播放记忆重现演出，解锁下一谜题 |

**章节级状态机：**

| State | Entry Condition | Exit Condition | Behavior |
|-------|----------------|----------------|----------|
| **ChapterLocked** | 前一章未完成 | 前一章完成 | 不可进入 |
| **ChapterActive** | 章节解锁且玩家进入 | 所有谜题 Complete | 逐个解锁并推进谜题 |
| **ChapterComplete** | 所有谜题完成 | — | 播放章末演出，解锁下一章 |

NearMatch → Active 的回退使用 5% 的滞后阈值（hysteresis），避免临界值附近反复触发。

**缺席型谜题（Ch.5）状态转换补充：**

- 缺席型谜题通过 `TbPuzzle.isAbsencePuzzle = true` 标记，`maxCompletionScore < 1.0`（建议 0.60-0.70）
- 由于 `maxCompletionScore < perfectMatchThreshold`，标准的 PerfectMatch 路径永远无法触发
- 替代路径：当 matchScore 稳定在 `>= maxCompletionScore` 且玩家在该分数停留 `>= absenceAcceptDelay`（默认 5s）无任何操作 → 触发 `AbsenceAcceptedEvent`
- AbsenceAccepted 不执行标准物件吸附动画，而是触发缺席专用演出序列（由 Narrative Event System 定义）
- `absenceAcceptDelay` 的 5s 等待是为了确认玩家已主动停止尝试——区分"正在调整中的暂停"和"接受了当前状态"
- PerfectMatch 和 AbsenceAccepted 均为不可逆终态——进入后 matchScore 计算冻结，不响应后续分数变化

### Interactions with Other Systems

**与叙事系统的交互：**
- 输出：每个谜题 Complete 后发送 `PuzzleCompleteEvent(puzzleId, chapterId)`
- 叙事系统接收后触发对应的记忆重现演出（场景变化、环境音变化、光线色温变化）
- 章节 Complete 后发送 `ChapterCompleteEvent(chapterId)`，触发章节过渡演出

**与提示系统的交互：**
- 输出：当前匹配度、玩家操作频率、停留时间、重复失败次数
- 输入：提示系统决定是否显示提示及显示哪一层级的提示
- 提示系统不直接修改物件位置，只提供视觉引导信息

**与 Tutorial 系统的交互：**
- 教学完成后进入 PerfectMatch 判定保护期（`tutorialGracePeriod = 3s`）：期间 matchScore 继续正常计算，但即使超过阈值也不触发 PerfectMatch/AbsenceAccepted 状态转换
- 设计意图：防止玩家在教学操作过程中"意外"到达正确位置，还未从"学习模式"心理状态切换到"探索模式"就被拉入演出
- 保护期结束后正常判定

**与章节状态系统的交互：**
- 输出：谜题完成状态、当前步骤、已解锁 flags
- 输入：章节解锁状态、可操作物件集合、目标影子配置
- 存档系统读取章节状态实现断点续玩

**与 UI 系统的交互：**
- 物件选中时显示操作提示 UI（旋转/移动图标）
- NearMatch 时显示匹配度进度条（可选，可配置是否显示）
- Complete 时隐藏所有操作 UI，进入演出模式

## Formulas

### Shadow Match Score（影子匹配度计算）

采用多锚点加权评分：

```
matchScore = Σ(anchorWeight_i * anchorScore_i) / Σ(anchorWeight_i)
```

每个锚点的单独评分：

```
anchorScore = positionScore * directionScore * visibilityScore

positionScore = 1.0 - clamp(screenDistance / maxScreenDistance, 0, 1)
directionScore = 1.0 - clamp(angleDelta / maxAngleDelta, 0, 1)
visibilityScore = isVisible ? 1.0 : 0.0
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| screenDistance | float | 0-∞ px | runtime | 锚点投影到屏幕后与目标位置的像素距离 |
| maxScreenDistance | float | 50-200 px | PuzzleLinkConfig | 超过此距离 positionScore 为 0 |
| angleDelta | float | 0-180° | runtime | 锚点朝向投影到屏幕后与目标方向的角度差 |
| maxAngleDelta | float | 15-45° | PuzzleLinkConfig | 超过此角度 directionScore 为 0 |
| anchorWeight | float | 0.1-1.0 | PuzzleAnchor | 该锚点在总评分中的权重 |
| isVisible | bool | true/false | runtime | 锚点是否在相机可见范围内且未被遮挡 |

**Expected output range**: 0.0 (完全不匹配) to 1.0 (完美匹配)

**NearMatch threshold**: 0.40 (进入) / 0.35 (退出，hysteresis)
**PerfectMatch threshold**: 0.85

**Edge case**: 当所有锚点都不可见时 (全部被遮挡)，matchScore 强制为 0，避免误触发。

### Snap Animation（吸附动画插值）

```
currentPos = lerp(startPos, targetPos, easeCurve(t / snapDuration))
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| snapDuration | float | 0.3-0.8s | config | 吸附动画总时长 |
| easeCurve | AnimationCurve | — | config | 使用 EaseOutBack 曲线，有轻微回弹感 |

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|----------|------------------|-----------|
| 所有锚点被同一物件完全遮挡 | matchScore = 0，不触发 NearMatch | 防止不可见状态下误判 |
| 物件被拖拽到场景边界外 | 物件夹在边界内，边缘有回弹缓冲 | 防止物件丢失 |
| 光源移动导致影子超出接收面 | 超出部分影子被裁切，matchScore 降低 | 真实感 + 引导玩家调整 |
| 玩家在 NearMatch 状态长时间不操作 | 30 秒后触发第 1 层提示 | 防卡关 |
| 两个谜题的目标影子意外部分重叠 | 每个谜题独立判定，互不干扰 | 设计师确保场景内不会出现歧义 |
| 第五章"缺席型谜题"缺少关键物件 | 谜题目标影子标记为"不可完整还原"，接受残缺影子作为成功条件 | 主题表达：失去后某些东西无法复原 |
| 玩家快速连续操作导致判定抖动 | 匹配度使用 0.2 秒滑动平均过滤 | 稳定反馈，避免闪烁 |

## Dependencies

| System | Direction | Nature of Dependency |
|--------|-----------|---------------------|
| Chapter State System | This depends on | 需要章节解锁状态和谜题配置数据 |
| Hint System | Hint depends on this | 提供匹配度和玩家行为数据 |
| Narrative Event System | Narrative depends on this | 接收谜题/章节完成事件触发演出 |
| Save System | Bidirectional | 读取/写入谜题完成状态 |
| Input System | This depends on | 接收触摸/拖拽/旋转输入 |
| URP Rendering | This depends on | 依赖 URP 实时阴影和投影纹理 |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|-----------|--------------|------------|-------------------|-------------------|
| maxScreenDistance | 120 px | 50-200 px | 更容易匹配，降低难度 | 更难匹配，需要更精确操作 |
| maxAngleDelta | 30° | 15-45° | 方向容错更大 | 方向要求更严格 |
| nearMatchThreshold | 0.40 | 0.30-0.55 | 更晚出现提示，体验更"干净" | 更早出现提示，降低挫败感 |
| perfectMatchThreshold | 0.85 | 0.75-0.95 | 需要更精确才能成功 | 更容易成功 |
| snapDuration | 0.5s | 0.3-0.8s | 吸附动画更慢更仪式感 | 吸附更快更爽脆 |
| *提示触发时机* | — | — | *由 Hint System 全权管理，参见 `hint-system.md` 的 triggerScore 公式和 Tuning Knobs* | — |
| gridSnapSize | 0.25 units | 0.1-0.5 units | 更粗的网格，更易操作 | 更精细但更难精确到位 |
| absenceAcceptDelay | 5s | 3-8s | 需要更长的"接受"等待时间 | 更快触发缺席完成判定 |

### 跨章节难度参数矩阵（建议值）

各章节通过 TbPuzzle 配置表的覆盖字段实现差异化。建议全 5 章使用统一格点步进（0.25 units），通过物件数量和概念复杂度提升难度而非操作精度（符合反支柱"NOT 硬核机关解谜"）。

| 维度 | Ch.1 靠近 | Ch.2 共同空间 | Ch.3 共同生活 | Ch.4 松动 | Ch.5 缺席 |
|------|----------|-------------|-------------|---------|---------|
| 物件数 | 2→3 | 3（前半）/ 2-3（后半） | 3→4 | 4 | 5+ |
| 光源数 | 1（固定） | 1（固定→可操作） | 1（可操作） | 1-2（可操作） | 1-2（可操作） |
| gridSnapSize | 0.25 | 0.25 | 0.25 | 0.25 | 0.25 |
| nearMatchThreshold | 0.40（默认） | 0.40 | 0.40 | 0.35 | 0.35 |
| perfectMatchThreshold | 0.85（默认） | 0.85 | 0.85 | 0.80 | 0.78（非缺席）/ N/A（缺席） |
| maxCompletionScore | N/A | N/A | N/A | N/A | 0.60-0.70（缺席谜题） |
| maxScreenDistance | 80-100px（简单谜题缩小热区抑制随机扫描） | 120px | 120px | 120px | 120px |
| hintDelayOverride | 1.0（默认） | 1.0 | 1.3 | 1.5 | 1.5 |

> **设计意图**：Ch.4-5 影子风格柔化（Edge Sharpness 0.7、Penumbra 2-4px）导致视觉判定难度增加约 15-20%，nearMatch/perfectMatch 阈值降低约 5-7% 作为补偿。Ch.1 的 maxScreenDistance 缩小以抑制 2 物件简单谜题中的"随机扫描"策略。
| matchScoreSmoothing | 0.2s | 0.1-0.5s | 更平滑但反应略迟 | 更灵敏但可能闪烁 |

## Visual/Audio Requirements

| Event | Visual Feedback | Audio Feedback | Priority |
|-------|----------------|---------------|----------|
| 物件选中 | 轮廓高亮 + 轻微放大 | 柔和 click 音 | MVP |
| 物件拖拽中 | 影子实时跟随变化 | 轻微摩擦/滑动音 | MVP |
| 物件旋转中 | 影子实时旋转 | 微弱的转动音 | MVP |
| 光源移动 | 全场影子方向/大小变化 | 光线嗡鸣音轻微变调 | Vertical Slice |
| NearMatch 进入 | 影子轮廓边缘发出柔和暖光 | 轻微的音调升起 | MVP |
| NearMatch 退出 | 暖光消退 | 音调回落 | MVP |
| PerfectMatch 触发 | 物件吸附 + 影子定格 + 光线集中 | 满足感的共鸣音 + 轻微风铃 | MVP |
| 记忆重现演出 | 影子中浮现记忆画面 + 场景色温变暖 | 章节主题旋律片段 | Vertical Slice |
| 章末完成 | 长时间的影子定格 + 渐暗过渡 | 完整章节音乐收束 | Vertical Slice |
| 提示 Tier 1 | 可疑区域微光闪烁 | 无/极轻微 | MVP |
| 提示 Tier 2 | 两个相关物件间出现淡线连接 | 轻微提示音 | Vertical Slice |
| 提示 Tier 3 | 物件移动方向箭头 | 引导音 | Vertical Slice |

## Game Feel

### Feel Reference

应该感觉像 **Unpacking 的物件放置** — 物件有真实的重量感和触感，放到正确位置时有清晰的"咔嗒"满足感，但过程中是温柔的、不焦虑的。**不应该**感觉像 Tetris 那样紧张和精确要求高。

影子成形的那一刻应该感觉像 **Gorogoa 的画面拼合** — 突然间无关的碎片变成一个整体，有"啊哈"的认知愉悦加上情感触动。

### Input Responsiveness

| Action | Max Input-to-Response Latency (ms) | Frame Budget (at 60fps) | Notes |
|--------|-----------------------------------|------------------------|-------|
| 物件选中 | 50ms | 3 frames | 即时高亮反馈 |
| 物件拖拽 | 16ms | 1 frame | 必须与手指位置同步 |
| 物件旋转 | 16ms | 1 frame | 旋转与手势同步 |
| 光源移动 | 33ms | 2 frames | 可略有延迟，影子更新计算量大 |
| 影子更新 | 33ms | 2 frames | 允许 1 帧延迟以换取移动端性能 |
| NearMatch 判定 | 200ms | 12 frames | 使用滑动平均，刻意降低灵敏度 |

### Animation Feel Targets

| Animation | Startup Frames | Active Frames | Recovery Frames | Feel Goal | Notes |
|-----------|---------------|--------------|----------------|-----------|-------|
| 物件选中放大 | 0 | 8 | 0 | 即时响应，轻微弹性 | EaseOutBack |
| 物件放下回弹 | 0 | 6 | 4 | 轻落，有触地感 | EaseOutBounce 轻微 |
| PerfectMatch 吸附 | 0 | 18-30 | 12 | 温柔滑入，轻微回弹 | EaseOutBack |
| NearMatch 发光 | 6 | 持续 | 12 (退出时) | 柔和渐显渐隐 | 线性渐变 |
| 记忆重现展开 | 30 | 60-120 | 30 | 缓慢展开，有呼吸感 | 自定义曲线 |

### Impact Moments

| Impact Type | Duration (ms) | Effect Description | Configurable? |
|-------------|--------------|-------------------|---------------|
| PerfectMatch 定格 | 300ms | 所有物件和影子短暂静止，然后缓慢吸附 | Yes |
| 记忆重现高潮 | 500ms | 影子中浮现的画面达到最清晰时，轻微时间减速 | Yes |
| 章末影子完成 | 800ms | 长时间静止 + 画面缓慢变暗 | Yes |
| 触感震动 (NearMatch) | 50ms | 轻微震动，只在移动端 | Yes |
| 触感震动 (PerfectMatch) | 150ms | 中等震动 + 渐弱尾音 | Yes |

### Weight and Responsiveness Profile

- **Weight**: 中等偏轻。物件有真实感的重量（不像纸片一样飘），但操作不费力。像是在桌面上轻轻推动一个陶瓷杯子。
- **Player control**: 高度控制。物件跟随手指精确移动，无惯性延迟。放手后物件立即停止（不滑动）。
- **Snap quality**: 柔和的 snap。格点吸附是轻柔的"吸入"而非硬切，NearMatch 到 PerfectMatch 的过渡是渐进的。
- **Acceleration model**: 即时响应（arcade feel）。物件移动没有加速度曲线，手指到哪物件到哪。
- **Failure texture**: 宽容的。没有惩罚、没有计时、没有失败画面。放错位置只是影子看起来不对，鼓励继续尝试。提示系统确保玩家不会卡太久。

### Feel Acceptance Criteria

- [ ] 物件拖拽感觉"跟手"——无可感知延迟
- [ ] PerfectMatch 那一刻让测试者发出"哦~"或类似的满足反应
- [ ] 没有测试者形容操作为"滑腻"、"卡顿"或"精确要求太高"
- [ ] 影子的实时变化让测试者自然地"一边拖一边看墙面"
- [ ] 提示系统出现时，测试者感觉被帮助而非被打断

## UI Requirements

| Information | Display Location | Update Frequency | Condition |
|-------------|-----------------|-----------------|-----------|
| 可操作物件标记 | 物件上方 | 场景加载时 | 进入谜题时，操作后渐隐 |
| 操作方式提示 | 屏幕下方 | 首次交互时 | 仅前几次操作显示，可关闭 |
| 章节进度 | 屏幕角落 | 谜题切换时 | 当前章 N/M 谜题 |
| 提示按钮 | 屏幕角落 | 常驻 | 玩家主动请求提示时 |
| 匹配度指示 | 不显示数值 | — | 通过影子轮廓发光程度间接传达 |
| 暂停/设置菜单 | 屏幕角落 | 常驻 | 标准暂停功能 |

## Cross-References

| This Document References | Target GDD | Specific Element Referenced | Nature |
|--------------------------|-----------|----------------------------|--------|
| 谜题完成触发叙事演出 | `design/gdd/narrative-event-system.md` | 记忆重现事件链 | State trigger |
| 匹配度和行为数据驱动提示 | `design/gdd/hint-system.md` | 提示层级触发条件 | Data dependency |
| 谜题状态影响存档 | `design/gdd/chapter-state-and-save.md` | 谜题完成 flag / IChapterProgress | Ownership handoff |
| 章节解锁控制谜题可用性 | `design/gdd/chapter-state-and-save.md` | 章节完成状态 | Rule dependency |
| 物件操作事件 | `design/gdd/object-interaction.md` | ObjectTransformChanged 驱动匹配计算 | Data dependency |

## Acceptance Criteria

- [ ] 3 个 MVP 谜题可在移动端流畅运行 (60fps, < 150 draw calls)
- [ ] 双物件基础组合谜题：80% 测试者在无提示下 2 分钟内完成
- [ ] 三物件关系补完谜题：60% 测试者在无提示下 5 分钟内完成
- [ ] NearMatch 反馈被 70%+ 测试者正确理解为"接近正确"
- [ ] PerfectMatch 吸附动画在所有测试者中被评价为"满足"或"自然"
- [ ] 提示系统的三层结构覆盖 95%+ 的卡关情况
- [ ] "缺席型谜题"（第五章）被测试者理解为主题表达而非 bug
- [ ] 所有匹配阈值和提示延迟通过配置表调节，无硬编码
- [ ] 物件操作在 iPhone 13 Mini（小屏）上仍然舒适可用
- [ ] Performance: 影子匹配判定每帧计算完成在 2ms 以内

## Open Questions

| Question | Owner | Deadline | Resolution |
|----------|-------|----------|-----------|
| MVP 阶段的影子判定采用路线 A（纯作者预设）还是 A+B 混合？ | Tech Lead | 原型完成前 | 原型阶段先用路线 A 快速搭建，验证手感后再补充屏幕空间锚点 |
| 物件操作是否需要支持 Z 轴高度调整（抬起/放下）？ | Game Design | 原型阶段 | 待原型验证是否增加谜题深度还是增加操作复杂度 |
| 影子渲染使用 URP 实时阴影还是投影纹理（Projector）模拟？ | TA / Graphics | 原型阶段 | 需在移动端验证性能 |
| 第四章"松动"的影子偏移如何实现？锚点动态漂移？噪声扰动？ | Game Design | Vertical Slice | 需要原型验证哪种方式情绪表达最准确 |
| 多选物件同时操作是否在 scope 内？ | Game Design | MVP | 建议 MVP 仅单选，后续评估 |
| 格点吸附的网格尺寸是否需要逐章调整？ | Game Design | Alpha | 可能后期章节需要更精细的网格支持更复杂关系 |
