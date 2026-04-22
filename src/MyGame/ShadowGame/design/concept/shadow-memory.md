# Game Concept: 影子回忆 (Shadow Memory)

*Created: 2026-04-16*
*Status: Draft*

---

## Elevator Pitch

> 一款通过摆放生活物件与光源、拼出关系影子的叙事解谜游戏。玩家将在失去伴侣后，重新看见那些曾经由两个人共同构成的日常世界。

---

## Core Identity

| Aspect | Detail |
| ---- | ---- |
| **Genre** | 叙事解谜 (Narrative Puzzle) + 氛围探索 |
| **Platform** | Mobile (iOS / Android) 优先，后续扩展 PC (Steam) |
| **Target Audience** | 偏好独立游戏、情绪叙事体验的 18-35 岁玩家 |
| **Player Count** | 单人 |
| **Session Length** | 15-30 分钟 |
| **Monetization** | 付费买断 (Premium) |
| **Estimated Scope** | Medium (6-9 个月) — 5 章，每章 5-8 个谜题 |
| **Comparable Titles** | Projected Dreams, In My Shadow, Shadowmatic, Moncage, Gorogoa |

---

## Core Fantasy

玩家扮演一个正在整理记忆的人。伴侣已经不在了，但生活物件还在——台灯、杯子、椅子、雨伞、窗帘。这些原本普通的东西，在两个人共同生活之后获得了新的意义。

玩家通过操控这些物件与光源，在墙面上拼出由关系构成的影子。每一个成功拼出的影子，都是一段被重新理解的共同记忆。

核心情感承诺：**单个物体不是回忆，关系才是回忆。**

这是一个关于"失去之后重新理解"的游戏。不是关于找回，而是关于看见——看见那些曾经被忽略的日常，如何承载了一段完整的关系。

---

## Unique Hook

**影子不是答案，而是关系的结果。**

与现有影子解谜游戏的关键区别：
- Shadowmatic 要求"旋转物体，匹配目标轮廓"——影子是一个等待被找到的答案
- Projected Dreams 要求"摆出给定的影子图案"——影子是一个拼图目标
- 本作的影子只有在**多个生活碎片处于正确的关系位置**时才能成立——影子是关系的证据

这意味着谜题的核心不是"这个形状像什么"，而是"这些东西之间应该是什么关系"。当影子成立的那一刻，玩家理解的不是一个物体的轮廓，而是两个人曾经共同构成的一个世界的片段。

通过 "and also" 测试：它像 Projected Dreams 的生活物件投影解谜，**而且**每个影子都在表达一段双人关系的痕迹，到了后期，你甚至会遇到因为"另一个人不在了"而永远无法复原的影子。

---

## Player Experience Analysis (MDA Framework)

### Target Aesthetics

| Aesthetic | Priority | How We Deliver It |
| ---- | ---- | ---- |
| **Sensation** | 2 | 温柔的光影变化、生活感的物件质感、安静的环境音 |
| **Fantasy** | 4 | 玩家是一个在记忆中整理共同生活痕迹的人 |
| **Narrative** | 1 | 5 章关系弧线，从靠近到缺席，全程用影子讲述 |
| **Challenge** | 3 | 空间推理与关系理解并重的谜题设计 |
| **Fellowship** | N/A | 单人体验 |
| **Discovery** | 3 | 发现普通物件组合后的情感意义 |
| **Expression** | N/A | 非创造型游戏 |
| **Submission** | 2 | 低压力、安静、可在自己节奏下体验 |

### Key Dynamics

- 玩家会自然地开始在物件之间寻找"关系"，而不只是寻找"形状匹配"
- 玩家会在成功拼出影子时回望物件本身，重新理解它们的情感重量
- 后期玩家会意识到某些影子无法完整还原，产生主动的情绪共鸣
- 玩家会在章节间主动回忆前面的影子，将碎片串成完整的关系理解

### Core Mechanics

1. **物件摆放与旋转** — 在限定空间内调整生活物件的位置、角度、距离关系
2. **光源操控** — 移动单一光源的位置和方向，改变投影结果
3. **影子匹配判定** — 当物件组合在光照下形成目标影子轮廓时，触发记忆重现
4. **章节叙事推进** — 每解开一组谜题，关系状态推进一步，世界氛围随之变化

---

## Player Motivation Profile

### Primary Psychological Needs Served

| Need | How This Game Satisfies It | Strength |
| ---- | ---- | ---- |
| **Autonomy** | 玩家自主选择观察角度和摆放策略，无固定解法路径 | Supporting |
| **Competence** | 从简单双物件组合到复杂多物件关系，持续感受理解力增长 | Core |
| **Relatedness** | 通过影子重建的关系感受人与人之间的连接，虽无 NPC 对话但情感共鸣强烈 | Core |

### Player Type Appeal

- [x] **Explorers** — 发现物件组合的情感意义，探索每章新的关系维度
- [x] **Achievers** — 完成每章谜题、收集隐藏记忆碎片
- [ ] **Socializers** — 单人体验，但完成后的分享/讨论欲望强
- [ ] **Killers/Competitors** — 非竞争型

### Flow State Design

- **Onboarding curve**: 第一章仅有 2 个物件 + 1 个光源，教会玩家"两个东西可以组成一个有意义的影子"
- **Difficulty scaling**: 物件数量渐增（2→3→5+）、光源操作渐进引入、后期出现"缺席"反预期机制
- **Feedback clarity**: NearMatch 时影子轮廓发光提示接近正确；PerfectMatch 时播放记忆重现演出
- **Recovery from failure**: 无死亡/惩罚，三层渐进提示系统，鼓励探索而非惩罚失败

---

## Core Loop

### Moment-to-Moment (30 seconds)

观察场景中的生活物件 → 拖拽/旋转物件调整位置 → 观察墙面上影子的变化 → 感受影子轮廓是否接近某个有意义的形状。

核心满足感：**"我移了一下杯子，影子突然看起来像什么了。"**

### Short-Term (5-15 minutes)

一个完整谜题：理解场景中物件的潜在关系 → 尝试不同摆放组合 → 接近正确时获得 NearMatch 提示 → 完成影子匹配 → 观看记忆重现片段 → 场景转换到下一个谜题。

### Session-Level (30-60 minutes)

完成一章内的 5-8 个谜题。每章围绕一种关系状态（靠近/共处/共同生活/松动/缺席），体验关系从建立到变化的完整弧线片段。章末有一个更复杂的"终局影子"收束本章主题。

### Long-Term Progression

5 章完整的关系弧线：靠近 → 共同空间 → 共同生活 → 松动 → 缺席与重新理解。每章解锁新的操作维度，影子意象从具体走向抽象。隐藏收集物（照片/物件/声音片段）补充叙事细节。

### Retention Hooks

- **Curiosity**: 每章结尾的影子意象暗示下一段关系状态，驱动玩家想知道"后来怎么了"
- **Investment**: 对两个人共同生活的情感投入越来越深，不想半途而废
- **Mastery**: 后期谜题的关系理解要求更高，玩家想证明自己"真的懂了"

---

## Game Pillars

### Pillar 1: 关系即谜题

谜题的核心不是空间推理的难度，而是对"两个人如何共同构成一个世界"的理解。每个谜题都必须在机制层面表达一种关系状态。

*Design test*: 如果一个谜题很巧妙但和关系主题无关，我们选择不做它。

### Pillar 2: 日常即重量

物件必须是生活中常见的——台灯、杯子、椅子、雨伞。不使用奇幻、科幻或抽象几何元素。情感重量来自"这些东西本来很普通，但在共同生活之后变得珍贵"。

*Design test*: 如果一个物件需要解释才能理解它是什么，不用它。

### Pillar 3: 克制表达

不使用大段文字、旁白、或直接的情感宣泄。让影子、光线、空间关系和玩家的操作本身承载叙事。安静、温柔、有留白。

*Design test*: 如果不用文字也能传达，就不加文字。

### Pillar 4: 缺席比存在更有力

游戏后半段的力量来自"某些影子再也无法像从前那样成立"。这不是bug，而是主题表达的核心。失去不是游戏的结局，而是重新理解的起点。

*Design test*: 如果第五章和第一章的解谜感受完全一样，说明主题没有落地。

### Anti-Pillars

- **NOT 硬核机关解谜**: 不追求复杂机关和高难度空间推理，那会稀释情绪主题
- **NOT 人生编年史**: 不做从出生到死亡的完整人生叙事，聚焦两人关系的核心片段
- **NOT 物理模拟器**: 不追求真实光影物理的技术炫耀，服务体验而非写实
- **NOT 舒适系拼图**: 不做纯放松的影子拼图消遣，情绪密度要高于 Projected Dreams

---

## Inspiration and References

| Reference | What We Take From It | What We Do Differently | Why It Matters |
| ---- | ---- | ---- | ---- |
| Projected Dreams | 生活物件 + 投影 + 温暖叙事的可行性验证 | 从"匹配目标轮廓"转向"拼出关系结构"；情绪密度更高 | 证明品类成立，用户接受度已验证 |
| Shadowmatic | 影子轮廓识别的基础乐趣 | 不做纯 puzzle，影子承载关系意义而非单纯视觉惊喜 | 验证影子成形机制的玩法基底 |
| In My Shadow | 影子 + 情绪叙事 + 记忆主题 | 不做平台解谜；聚焦伴侣关系而非家庭/个人成长 | 证明"影子作为情绪机制"被市场接受 |
| Moncage | 视觉机制承担叙事意义的哲学 | 载体从立方体视角拼接转为光源投影关系 | 机制叙事统一的最佳实践参考 |
| Gorogoa | 无文字、图像拼合推动叙事的方法论 | 用 3D 光影投影替代 2D 图像拼合 | 低文本叙事可行性参考 |

**Non-game inspirations**:
- 建筑师 Peter Zumthor 的"氛围"理论——空间本身如何承载情绪
- 是枝裕和电影中的日常细节叙事——通过生活碎片而非戏剧冲突讲述关系
- 摄影中的"负空间"概念——缺席如何定义存在

---

## Target Player Profile

| Attribute | Detail |
| ---- | ---- |
| **Age range** | 18-35 |
| **Gaming experience** | Casual to Mid-core；熟悉手机游戏，偶尔玩 PC 独立游戏 |
| **Time availability** | 碎片化时间（通勤/睡前 15-30 分钟），周末可能一次玩完一章 |
| **Platform preference** | 手机为主，部分玩家偏好 iPad 或 PC |
| **Current games they play** | Monument Valley, Florence, Unpacking, Gris, Alto's Odyssey |
| **What they're looking for** | 安静但不无聊的体验；有情感深度但不沉重；好看、好听、好触感的独立游戏 |
| **What would turn them away** | 过高的操作难度；大段阅读文本；需要长时间集中注意力；缺乏明确反馈的盲目试错 |

---

## Technical Considerations

| Consideration | Assessment |
| ---- | ---- |
| **Engine** | Unity 2022.3.62f2 (LTS)，搭配 TEngine 6.0.0 框架 |
| **Key Technical Challenges** | 移动端实时影子投影的性能优化；影子匹配判定的手感调校；URP 下影子渲染质量与性能平衡 |
| **Art Style** | 3D 半写实风格，克制温暖的色调，强调轮廓清晰度 |
| **Art Pipeline Complexity** | Medium — 需要定制生活物件模型（轮廓清晰、影子可读），场景数量适中 |
| **Audio Needs** | Moderate — 环境音为主 + 关键时刻的情绪音效 + 每章独立氛围音乐 |
| **Networking** | None |
| **Content Volume** | 5 章 x 5-8 谜题 = 25-40 个谜题；15-20 种生活物件；5 个主场景 + 变体；预计 3-5 小时总游玩时长 |
| **Procedural Systems** | 无程序化生成，全部人工设计（谜题质量优先于数量） |

---

## Risks and Open Questions

### Design Risks

- 玩家可能把"关系影子"理解成普通的"轮廓匹配"，差异化表达不成功
- "缺席导致影子不成立"可能被感受为挫败/bug 而非主题表达
- 生活物件是否能支撑 3-5 小时的玩法新鲜度，避免后期重复感
- 5 章叙事弧线在无文本表达下是否能被大多数玩家理解

### Technical Risks

- 移动端 URP 实时影子渲染的性能限制（目标 < 150 draw calls）
- 影子匹配判定的手感——阈值太宽失去挑战感，太窄造成精确旋转挫败
- 物件操作在触屏上的交互体验——拖拽/旋转精度与手指遮挡问题

### Market Risks

- "伴侣离去后的回忆"主题可能被部分玩家视为过于沉重
- 影子解谜品类虽有验证但仍属小众，付费意愿的天花板有限
- Projected Dreams 已占据"生活物件 + 温柔影子解谜"认知，需主动拉开差距

### Scope Risks

- 每个谜题需要人工设计和反复调试，内容生产效率可能低于预期
- 美术资产（物件模型 + 场景 + 影子效果）制作周期较长
- 5 章结构可能在中后期发现前期谜题设计需要重做

### Open Questions

- 伴侣离去的呈现方式——隐喻式暗示 vs 某个关键时刻的直接表达？
- 玩家在世界中的具体身份——"整理记忆的人" vs "在回忆中行走的人"？
- 每章是否有一个明确的"终局大谜题"收束章节？
- 是否使用任何文字（物件旁的便签、日期标记等极少量文本）？
- 影子判定是采用真实 3D 投影计算还是作者预设的目标区域？（技术原型阶段验证）
- 章节之间如何转场？场景切换 vs 同一空间的渐变？

---

## MVP Definition

**Core hypothesis**: 玩家能理解"多个生活物件共同构成一个有意义的关系影子"，并在成功时感受到情感共鸣而不仅是 puzzle 满足感。

**Required for MVP**:
1. 1 个场景（温暖的室内空间）+ 1 个固定光源
2. 3-5 个生活物件（台灯、杯子、椅子等），支持拖拽和旋转
3. 3 个完整谜题：双物件基础组合 → 三物件关系补完 → 一个"缺少某物件时影子不成立"的变体
4. NearMatch 提示反馈（影子轮廓接近时发光/震动）
5. PerfectMatch 成功演出（影子定格 + 简短的环境叙事变化）
6. 基础的三层提示系统

**Explicitly NOT in MVP**:
- 完整 5 章叙事
- 多光源操控
- 收集物系统
- 音乐和完整音效
- 多平台适配

### Scope Tiers

| Tier | Content | Features | Timeline |
| ---- | ---- | ---- | ---- |
| **MVP** | 1 场景 + 3 谜题 | 核心操作 + 匹配判定 + 基础提示 | 4-6 周 |
| **Vertical Slice** | 第一章完整（5-8 谜题）+ 章末演出 | 叙事推进 + 章节状态 + 完整提示 | 8-12 周 |
| **Alpha** | 全 5 章框架 + 占位内容 | 全部系统 + 收集物 + 存档 | 20-28 周 |
| **Full Vision** | 全 5 章完整内容 + 打磨 | 全系统 + 音效 + 多平台 + 本地化 | 32-40 周 |

---

## Next Steps

- [x] 编写核心系统 GDD：Shadow Puzzle System (`design/gdd/shadow-puzzle-system.md`) — 2026-04-16 完成
- [ ] 编写 Art Bible (`design/art/art-bible.md`) — Concept 阶段必需
- [ ] 编写 Systems Map (`design/gdd/systems-index.md`) — Concept 阶段必需
- [ ] 基于 MVP 定义搭建技术原型
- [ ] 完成 5 章结构表（每章：核心情绪/终局影子意象/关键物件/章节结尾方式）
- [ ] 整理首批 5-8 种谜题模板
- [ ] 定义原型验证的三个核心问题测试方案
- [ ] 输出美术方向 Mood Board
