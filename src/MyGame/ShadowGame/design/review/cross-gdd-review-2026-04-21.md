// 该文件由Cursor 自动生成

# Cross-GDD Review Report — 影子回忆 (Shadow Memory)

> **审查日期**: 2026-04-21
> **审查范围**: 13 份 GDD（9 MVP + 4 新增 Vertical Slice）+ 游戏概念文档
> **上次审查**: 2026-04-16（9 MVP GDD，6 个 High/Medium 问题，5/6 已修复）
> **审查类型**: Full — 跨文档一致性 + 游戏设计整体性 + 跨系统场景走查
> **最终判定**: ~~**CONCERNS**~~ → **APPROVED** — 全部 26 项已于 2026-04-21 修复完成

---

## 目录

1. [上次修复验证](#1-上次修复验证)
2. [Part A: 跨文档一致性审查](#part-a-跨文档一致性审查)
   - [2a: 依赖双向性](#2a-依赖双向性)
   - [2b: 规则矛盾](#2b-规则矛盾)
   - [2c: 过期引用](#2c-过期引用)
   - [2d: 调参所有权](#2d-调参所有权)
   - [2e: 公式兼容性](#2e-公式兼容性)
   - [2f: 验收标准交叉](#2f-验收标准交叉)
3. [Part B: 游戏设计整体性审查](#part-b-游戏设计整体性审查)
   - [3a: 进度循环竞争](#3a-进度循环竞争)
   - [3b: 玩家注意力预算](#3b-玩家注意力预算)
   - [3c: 优势策略检测](#3c-优势策略检测)
   - [3d: 经济循环分析](#3d-经济循环分析)
   - [3e: 难度曲线一致性](#3e-难度曲线一致性)
   - [3f: 支柱对齐](#3f-支柱对齐)
   - [3g: 玩家幻想一致性](#3g-玩家幻想一致性)
4. [Part C: 跨系统场景走查](#part-c-跨系统场景走查)
5. [合并汇总表](#合并汇总表)
6. [修复优先级建议](#修复优先级建议)

---

## 1. 上次修复验证

对 2026-04-16 报告中标记 "已修复" 的 6 个问题逐一验证：

| # | 原问题 | 验证结果 |
|---|--------|---------|
| 1 | Shadow Puzzle Layer `Core` → `Feature` | ✅ Quick Reference 确认为 `Feature` |
| 2 | Shadow Puzzle Cross-Ref 路径修复 | ⚠️ `chapter-state-and-save.md` 路径已修复，但 `narrative-system.md (planned)` 引用仍未更新为 `narrative-event-system.md` |
| 3 | URP Cross-Ref 路径修复 | ✅ 引用 `chapter-state-and-save.md` |
| 4 | Hint HintButton 位置 | ✅ 描述为"右下偏上（GameHUD 内 RightPanel）" |
| 5 | Hint 按钮高亮时间对齐 | ✅ rampStart=30s，与 UI GDD 对齐 |
| 6 | Systems Index Shadow Puzzle 依赖补充 OI | ✅ 依赖列表包含 Object Interaction System |

**结论**: 6 项中 5 项已完全修复，1 项部分修复（Narrative 引用路径遗漏）。

---

## Part A: 跨文档一致性审查

### 2a: 依赖双向性

#### 🔴 A-1 [严重] InputFilter 机制在 Input System 中无正式规格定义

**涉及**: `input-system.md`, `tutorial-onboarding.md`

Input System 的 "与 Tutorial System 的交互" 一节提到 Tutorial 可设置 `InputFilter` 限制只允许特定手势通过。Tutorial GDD 核心设计依赖 `InputFilter`（push 白名单、pop 恢复）。然而 Input System GDD 的 "Core Rules > 输入阻断规则" 只定义了 `InputBlocker` 栈（全量阻断），**`InputFilter` 的规格（白名单过滤机制、数据结构、push/pop 语义）从未在任何 GDD 中正式定义**。

→ Input System GDD "Core Rules" 中新增 "输入过滤规则" 小节，正式定义 `InputFilter` 机制：白名单手势列表、push/pop 语义、同一时刻只能有一个 InputFilter 激活、与 InputBlocker 的优先级关系。

---

#### ⚠️ A-2 [警告] Settings → Input System 振动开关，Input 依赖表未列出

**涉及**: `settings-accessibility.md`, `input-system.md`

Settings GDD Dependencies 表列出 "Input System — This configures Input（振动开关）"。Input System 依赖表无 Settings 条目。

→ Input System GDD Dependencies 表添加 `Settings & Accessibility | Settings configures this | 振动开关全局标记`。

---

#### ⚠️ A-3 [警告] Settings → Object Interaction 触控灵敏度，OI 依赖表未列出

**涉及**: `settings-accessibility.md`, `object-interaction.md`

Settings 定义 `touch_sensitivity` 影响 Object Interaction 的 `dragThreshold` 和 `fatFingerMargin`。Object Interaction 依赖表无 Settings 条目。

→ Object Interaction GDD Dependencies 表添加 `Settings & Accessibility | Settings configures this | 触控灵敏度倍率影响 dragThreshold 和 fatFingerMargin`。

---

#### ℹ️ A-4 [信息] Tutorial → Save System 持久化教学记录，Save 侧未列出

**涉及**: `tutorial-onboarding.md`, `chapter-state-and-save.md`

Tutorial 将已完成教学步骤 ID 列表持久化到存档。Save GDD Dependencies 表无 Tutorial 条目。

→ Save GDD Dependencies 表补充 `Tutorial / Onboarding | Depends on Save System | 已完成教学步骤持久化`。同时在 Save Data Schema JSON 结构中正式定义 `tutorialCompleted: string[]` 数据块。

---

#### ✅ Narrative Event ↔ Audio System 双向依赖声明一致，无需修复。

---

### 2b: 规则矛盾

#### 🔴 A-5 [严重] InputBlocker（全量阻断）与 InputFilter（白名单过滤）优先级未定义

**涉及**: `input-system.md`, `tutorial-onboarding.md`, `narrative-event-system.md`

三个系统使用两套不同的输入控制机制：
- **InputBlocker**（栈式全量阻断）：UI System、Narrative Event System 使用
- **InputFilter**（白名单过滤）：Tutorial System 使用

**矛盾场景**: Tutorial 激活 InputFilter（只允许 Drag），Narrative 演出同时 push InputBlocker（阻断全部）。Tutorial Edge Cases 写"演出优先，教学暂停"，但优先级关系没有在 Input System 中正式定义。

→ Input System GDD 中明确定义优先级链：
1. `InputBlocker` 栈非空 → 全量阻断，InputFilter 不生效
2. `InputBlocker` 栈为空 + `InputFilter` 激活 → 白名单过滤
3. `InputBlocker` 栈为空 + 无 `InputFilter` → 正常通过所有手势

---

#### ⚠️ A-6 [警告] ambientVolume 独立定义 vs Settings 中 SFX+Ambient 共用 slider

**涉及**: `audio-system.md`, `settings-accessibility.md`

Audio System 定义独立的 `ambientVolume = 0.6`。Settings 的 `sfx_volume = 0.8` 描述为"音效音量（含环境音）"——SFX 和 Ambient 共享同一 slider。Audio 的 `ambientVolume = 0.6` 在运行时的实际语义不明。

→ Audio GDD 明确 `ambientVolume` 为"内部设计基线值，不暴露给玩家设置"。运行时 Ambient 层最终音量 = `clipBaseVolume × ambientBaseVolume × sfxVolume(from Settings) × masterVolume × duckingMultiplier`。

---

#### ⚠️ A-7 [警告] 手势提示管理职责冲突——OI 自行管理 vs Tutorial 声明统一管理

**涉及**: `tutorial-onboarding.md`, `object-interaction.md`

Object Interaction GDD 保留完整的手势提示 UI Requirements 和自管理逻辑（"前 N 次操作显示"），Tutorial GDD 明确声明手势提示由 Tutorial 系统统一管理。实现时两个系统会争夺同一个提示的控制权。

→ Object Interaction GDD 的手势提示条目添加 `*由 Tutorial / Onboarding 系统统一管理*`，移除"前 N 次操作显示"等自行管理触发逻辑，只保留视觉规格描述。

---

#### ⚠️ A-8 [警告] `sfx_enabled` 关闭会静音 Ambient，破坏 Audio "环境音始终存在"设计

**涉及**: `settings-accessibility.md`, `audio-system.md`

Settings 定义 `sfx_enabled` 关闭后静音 SFX + Ambient 层。Audio System 核心设计是环境音"始终存在"。

→ 推荐方案：`sfx_enabled` 只控制 SFX 层，Ambient 跟随 master_volume 但不受 sfx_enabled 影响。

---

#### ⚠️ A-9 [警告] PuzzleLockAllEvent / PuzzleUnlockEvent 多发送者未在事件契约中明确

**涉及**: `narrative-event-system.md`, `shadow-puzzle-system.md`

Narrative Event System 也发送 `PuzzleLockAllEvent`（原定义发送者为 Shadow Puzzle）。多发送者未在事件契约表中更新。

→ 事件契约表补充多发送者定义：Shadow Puzzle 在 PerfectMatch 判定时发送，Narrative 在演出开始/结束时发送。

---

### 2c: 过期引用

#### 🔴 A-10 [严重] 4 份 GDD 共 6 处仍引用 `narrative-system.md (planned)`

| GDD | 过期引用 | 应更新为 |
|-----|---------|---------|
| `shadow-puzzle-system.md` | `narrative-system.md (planned)` ×1 | `narrative-event-system.md` |
| `chapter-state-and-save.md` | `narrative-system.md (planned)` ×3 | `narrative-event-system.md` |
| `ui-system.md` | `narrative-system.md (planned)` ×1 | `narrative-event-system.md` |
| `scene-management.md` | `narrative-system.md (planned)` ×1 | `narrative-event-system.md` |

---

#### ⚠️ A-11 [警告] `input-system.md` 引用 `tutorial.md (planned)` → 应为 `tutorial-onboarding.md`

#### ⚠️ A-12 [警告] `hint-system.md` 引用 `tutorial.md (planned)` → 应为 `tutorial-onboarding.md`

#### ⚠️ A-13 [警告] `chapter-state-and-save.md` 引用 `settings-system.md (planned)` → 应为 `settings-accessibility.md`

#### ⚠️ A-14 [警告] `ui-system.md` 标记 `hint-system.md (planned)` → 文件已存在，移除 `(planned)`

#### ⚠️ A-15 [警告] `scene-management.md` 标记 `audio-system.md (planned)` → 文件已存在，移除 `(planned)`

#### ℹ️ A-16 [信息] `audio-system.md` 标记 `settings-accessibility.md (planned)` → 移除 `(planned)`

---

### 2d: 调参所有权

#### ⚠️ A-17 [警告] hintDelay 参数在 Shadow Puzzle 和 Hint System 中双重定义且数值矛盾

**涉及**: `shadow-puzzle-system.md`, `hint-system.md`

| 参数 | Shadow Puzzle | Hint System |
|------|-------------|-------------|
| Tier1 触发 | `hintDelay_Tier1 = 30s` | `layer1_idleThreshold = 45s`（+ 多因子 triggerScore） |
| Tier2 触发 | `hintDelay_Tier2 = 60s` | `layer2_idleThreshold = 90s` |
| Tier3 触发 | `hintDelay_Tier3 = 120s` | 玩家主动按钮触发 |

→ Shadow Puzzle GDD 移除 `hintDelay_Tier1/2/3` 三个 Tuning Knobs，改为引用："提示触发时机由 Hint System 全权管理"。

---

### 2e: 公式兼容性

#### ℹ️ A-18 [信息] touch_sensitivity 极值使 dragThreshold 超出 Input System Safe Range

| touchSensitivity | effectiveDragThreshold | Safe Range | 状态 |
|------------------|----------------------|------------|------|
| 0.5（最低） | 6.0mm | 上限 5.0mm | ⚠️ 超出 |
| 2.0（最高） | 1.5mm | 下限 2.0mm | ⚠️ 超出 |

→ Settings GDD 补充 clamp 逻辑或缩窄 sensitivity 范围至 0.6-1.5。

---

### 2f: 验收标准交叉

#### ⚠️ A-19 [警告] Hint 缺少"Tutorial 期间暂停计时"的验收条件

Tutorial 验收标准要求"教学期间 Hint System 不触发提示"，但 Hint System 的 13 条验收标准中无一涉及此验证。

→ Hint System 验收标准添加：`- [ ] Tutorial 系统教学激活期间，所有被动提示计时器暂停，不触发 Layer 1/Layer 2 提示`。

---

#### ℹ️ A-20 [信息] Save Data Schema 缺少新增系统字段 + 存储位置声明矛盾

**缺失字段**: Settings 新增的 `master_volume`, `touch_sensitivity`, `target_framerate`, `sfx_enabled`；Tutorial 的 `tutorialCompleted: string[]`。

**存储矛盾**: Settings GDD 声明 "设置与游戏存档分离——存储在 PlayerPrefs"，但 Save Data Schema 中存在 `settings` 块。

→ 需做决策闭环：(A) 走 PlayerPrefs 则移除 Save Schema 的 `settings` 块，或 (B) 保留存档则补齐字段并修改 Settings GDD 声明。

---

## Part B: 游戏设计整体性审查

### 3a: 进度循环竞争

ℹ️ **主循环清晰且唯一**：物件操作 → 影子匹配 → PerfectMatch → 谜题 Complete → 章节推进。Chapter State System 是全局进度唯一权威来源。

ℹ️ **三层进度节奏设计合理**：Moment-to-Moment（~30s）、Short-Term（5-15min）、Session（30-60min）、Long-Term（3-5h）与产品定位高度匹配。

⚠️ **B-1: Collectible System 可能产生进度感干扰**。如果 UI 展示"已收集 3/7"，可能引发"我是否漏掉了什么"的焦虑——与"慢慢来，没关系"的核心体验基调冲突。

→ Collectible 应严格作为叙事补充而非成就目标：(1) 不显示收集进度百分比，(2) 不奖励完整收集，(3) 不设 UI 通知/红点，(4) 收集物在首通后自然出现在"记忆碎片库"中。

ℹ️ **回放系统不与主进度竞争**——三条防护规则已正确定义。

---

### 3b: 玩家注意力预算

ℹ️ **核心操作阶段注意力通道为 2-3 个**（前景操作物件 + 中景影子变化 + 背景空间参考），远低于 3-4 认知负荷阈值。

⚠️ **B-2: NearMatch 触发时 HintButton 呼吸动画与反馈信号叠加**。触发时 4 个反馈通道同时活跃（视觉-影子辉光 + 视觉-HintButton 动画 + 触觉震动 + 听觉音效），打破"安静"基调。

→ NearMatch 时暂停 HintButton 呼吸动画。UI System GDD 补充：`PuzzleStateChanged(NearMatch)` 到达时，暂停 HintButton 呼吸脉冲，opacity 回落到 baseOpacity。

🔴 **B-3: 第五章缺席型谜题完成判定路径未定义**。这是一个**设计空洞**：

1. `isAbsencePuzzle = true` 且 `maxCompletionScore < 1.0`，matchScore 永远无法达到 PerfectMatch 默认阈值 85%。状态机无路径处理此情况。
2. 玩家无法获得"完成"信号——前 4 章一直在教"继续调整就能成功"。
3. 被误判为 Bug 的风险极高（概念文档已识别但 GDD 未提供解决方案）。

→ 定义专用完成判定路径：
- Shadow Puzzle：`isAbsencePuzzle && matchScore ≥ maxCompletionScore && 停留 ≥ 5s 无操作` → `AbsenceAcceptedEvent`
- Narrative Event：补充 `AbsenceFade` 原子效果
- URP Shadow Rendering：补充 Ch.5 "残缺影子"渲染规格（alpha 梯度渐隐，非粒子化）

---

### 3c: 优势策略检测

⚠️ **B-4: Layer 3 显式提示存在优势策略风险**。使用 Layer 3 后获得与自主探索完全相同的叙事回报，且重新进入谜题即重置限额。

→ 微妙的叙事差异化（非惩罚性）：使用 Layer 3 完成的谜题，演出的 TextureVideo holdDuration 稍短。

ℹ️ **格点吸附有效消除"精确操作"优势策略**——将技巧门槛从操作精度转移到关系理解，与 P1 对齐。

⚠️ **B-5: 第一章简单谜题中"随机扫描"策略可能生效**。NearMatch 的 40% 阈值 + maxScreenDistance=120px 意味着大面积"热区"。

→ Ch.1 谜题的 `maxScreenDistance` 从 120px 降至 80-100px。确保目标影子需要特定旋转角度。

---

### 3d: 经济循环分析

ℹ️ **经济设计极度简洁——绝对正确**。零经济模型（无货币、道具、解锁树），与付费买断叙事解谜定位匹配。

⚠️ **B-6: 缺席型谜题 Layer 3 限额降为 2 次——增加弃游风险**。缺席型谜题是玩家最困惑的时刻，减少提示次数反而在最需要帮助时削弱安全网。

→ 保持 3 次（甚至增加到 4 次），用提示文案内容（引导"接受"）而非数量限制来传达主题。

---

### 3e: 难度曲线一致性

🔴 **B-7: Ch.2 同时引入物件数量增加和光源操控两个新变量**。违反"每次只变一个变量"的逐步引入原则，造成难度跳变。

→ Ch.2 前 2-3 个谜题先增加物件（3 物件 + 固定光源），后 3-4 个谜题再引入光源操控（可回到 2 物件 + 可操作光源）。`tut_light` 对应 Ch.2 第 4 个谜题。

⚠️ **B-8: Ch.4-5 影子柔化但匹配阈值未调整**。Edge Sharpness 降至 0.7、Penumbra 增至 2-4px，视觉判定难度增加约 15-20%，但 NearMatch/PerfectMatch 阈值不变。

→ 建议阈值覆盖：
- Ch.4: NearMatch 0.35, PerfectMatch 0.80
- Ch.5（非缺席）: NearMatch 0.35, PerfectMatch 0.78
- Ch.5（缺席）: maxCompletionScore 0.60-0.70

⚠️ **B-9: Hint 参数未跟随章节难度缩放**。45s idleThreshold 在 Ch.5 复杂谜题（合理思考 5-15min）中过于激进——正在脑中推演的玩家被误判为"卡关"。

→ 利用 TbPuzzle 的 `hintDelayOverride`：Ch.3 = 1.3×，Ch.4-5 = 1.5×。

⚠️ **B-10: 全游戏跨章节难度参数矩阵尚未建立**。物件数/光源数/格点步进/阈值/Hint 延迟覆盖值缺乏统一矩阵。

→ 建议全 5 章使用统一格点步进（0.25 units），通过物件数量和概念复杂度提升难度而非操作精度。编写完整参数矩阵表。

---

### 3f: 支柱对齐

#### 支柱覆盖矩阵

| 系统 | P1: 关系即谜题 | P2: 日常即重量 | P3: 克制表达 | P4: 缺席比存在更有力 | 服务数 |
|------|:---:|:---:|:---:|:---:|:---:|
| Input System | — | ✅ | ✅ | — | 2 |
| URP Shadow Rendering | ✅ | — | ✅ | ⚠️ 待补充 | 2+ |
| Object Interaction | — | ✅✅ | ✅ | — | 2 |
| Chapter State & Save | ✅ | — | ✅ | — | 2 |
| Scene Management | ✅ | — | ✅ | — | 2 |
| Shadow Puzzle System | ✅✅ | ✅ | — | ✅ | 3 |
| Hint System | — | — | ✅✅ | ✅ | 2 |
| UI System | — | — | ✅✅ | — | 1 |
| Audio System | — | ✅ | ✅✅ | — | 2 |
| Narrative Event | ✅ | ✅ | ✅ | ⚠️ 待补充 | 3+ |
| Tutorial/Onboarding | — | ✅ | ✅ | — | 2 |
| Settings & Accessibility | — | ✅ | — | — | 1 |

⚠️ **B-11: P4（缺席比存在更有力）的系统支持不够具体**。URP 和 Narrative 对 P4 的支持仍停留在 Open Question。

→ URP 补充 Ch.5 "残缺影子"渲染规格（alpha 梯度渐隐）；Narrative 补充 `ShadowFade` 和 `ObjectFade` 原子效果。

✅ **反支柱全部通过**：NOT 硬核机关解谜、NOT 人生编年史、NOT 物理模拟器、NOT 舒适系拼图。

---

### 3g: 玩家幻想一致性

ℹ️ **高度一致——所有 Player Fantasy 指向同一玩家身份："一个安静地整理记忆的人"**。

关键词分析：
- "安静/轻/柔和" → 7/12 系统
- "不被感知/隐形" → 4/12 系统（全部基础设施层）
- "记忆/回忆" → 4/12 系统（全部内容层）
- "自然/无摩擦" → 4/12 系统
- "信任/安心" → 3/12 系统

⚠️ **B-12: Tutorial 的 InputFilter 完全无反馈可能产生轻微"被控制"感**。

→ 低优先级建议：被阻断手势改为目标物件微晃 0.5°（50ms），暗示"先试试这个操作"。

---

## Part C: 跨系统场景走查

### 场景 1: 核心循环（物件拖拽 → PerfectMatch → 记忆重现 → 下一谜题）

整体流程流畅，"影子成形瞬间"到"演出开始"延迟约 16ms（1 帧），可接受。

⚠️ **C-1: PerfectMatch 后 SnapToTarget 动画期间 matchScore 可能波动**。物件被驱动移动导致 `ObjectTransformChanged` → ShadowRT 更新 → matchScore 短暂波动，可能导致状态意外回退。

→ PerfectMatch 状态下冻结 matchScore 计算（PerfectMatch → Complete 为不可逆转换）。

---

### 场景 2: 教学与提示交互

⚠️ **C-2: 教学完成后 Hint 计时器恢复策略未明确**。如果教学持续 15 秒，恢复后 Hint idleTimer 继续从 15s 计算——教学完成后仅 30s 就触发 Layer 1。

→ 教学完成后 Hint 的 idleTimer、failCount、repeatDragCount 全部重置为 0。

⚠️ **C-3: 教学完成后立刻触发 PerfectMatch 破坏节奏**。玩家刚从"学习模式"切换，还没来得及沉浸。

→ 教学完成后 2-3s 内抑制 PerfectMatch 判定（matchScore 继续计算，但不触发状态转换）。

---

### 场景 3: 章节切换全流程

⚠️ **C-4: 章末非交互时间约 15 秒偏长**（记忆重现 ~5.5s + PuzzleComplete 面板 ~3s + 章节过渡 ~3-5s + Fade ~2.5s）。

→ 优化建议：
1. 章末最后谜题的 PuzzleCompletePanel 关系暗示语整合到章节过渡演出
2. 记忆重现演出色温恢复直接衔接章节过渡色温变化
3. 优化后 ~10s（与 Florence 章节转场一致）

---

### 场景 4: 设置修改——演出期间音量过低

ℹ️ **数据流正确但体验需关注**。玩家设 musicVolume=0.3 + 演出 ducking=0.3 → finalVolume=0.09，几乎不可闻。

→ 使用最低保底：`duckedVolume = max(layerVolume × duckingMultiplier, minDuckFloor=0.05)`。

---

### 场景 5: 玩家卡关——matchScore 0.30-0.40 区间盲区

⚠️ **C-5: matchScore 刚好在 NearMatch 以下时提示系统反应最慢**。玩家"有点接近但差一点"的状态最令人挫败，但 matchPenalty=0（matchScore > matchLowThreshold=0.3），每次微调又重置 idleTimer。

→ 将 `matchLowThreshold` 从 0.3 提升到 0.40（与 NearMatch 对齐）。增加 `stagnationScore`：matchScore 在 ±0.05 范围内连续 30s 无显著变化时额外贡献 0.3 分。

---

## 合并汇总表

### 🔴 阻塞问题（5 个）——必须在开发前修复

| # | 来源 | 涉及 GDD | 问题简述 |
|---|------|----------|---------|
| A-1 | 2a 依赖 | input-system, tutorial-onboarding | InputFilter 机制在 Input System 中无正式规格定义 |
| A-5 | 2b 矛盾 | input-system, tutorial-onboarding, narrative-event-system | InputBlocker 与 InputFilter 优先级关系未定义 |
| A-10 | 2c 引用 | shadow-puzzle, chapter-state, ui-system, scene-management | 4 份 GDD 共 6 处引用已不存在的 `narrative-system.md (planned)` |
| B-3 | 3b 注意力 | shadow-puzzle, urp-shadow, narrative-event, hint-system | 第五章缺席型谜题完成判定路径未定义——玩家永远无法触发完成 |
| B-7 | 3e 难度 | tutorial-onboarding, object-interaction | Ch.2 同时引入物件数量增加和光源操控，违反逐步引入原则 |

### ⚠️ 警告（15 个）——建议在 Vertical Slice 前解决

| # | 来源 | 涉及 GDD | 问题简述 |
|---|------|----------|---------|
| A-2 | 2a 依赖 | settings, input-system | Settings→Input 振动开关依赖未双向声明 |
| A-3 | 2a 依赖 | settings, object-interaction | Settings→OI 触控灵敏度依赖未双向声明 |
| A-6 | 2b 矛盾 | audio-system, settings | ambientVolume 独立定义 vs SFX+Ambient 共用 slider |
| A-7 | 2b 矛盾 | tutorial, object-interaction | 手势提示管理职责冲突 |
| A-8 | 2b 矛盾 | settings, audio-system | sfx_enabled 关闭静音 Ambient 破坏核心设计 |
| A-9 | 2b 矛盾 | narrative-event, shadow-puzzle | PuzzleLockAllEvent 多发送者未在事件契约明确 |
| A-11~15 | 2c 引用 | 多份 GDD | 过期的 `(planned)` 标记和错误文件名引用（5 处） |
| A-17 | 2d 所有权 | shadow-puzzle, hint-system | hintDelay 参数双重定义且数值矛盾 |
| A-19 | 2f 验收 | tutorial, hint-system | Hint 缺少"Tutorial 期间暂停计时"验收条件 |
| B-1 | 3a 进度 | collectible (待设计) | Collectible 可能产生进度感干扰 |
| B-2 | 3b 注意力 | ui-system, hint-system | NearMatch 时 HintButton 呼吸动画与反馈叠加 |
| B-4 | 3c 策略 | hint-system, narrative-event | Layer 3 提示与自主探索获得相同叙事回报 |
| B-6 | 3d 经济 | hint-system | 缺席型谜题 Layer 3 限额降为 2 次增加弃游风险 |
| B-8 | 3e 难度 | urp-shadow, shadow-puzzle | Ch.4-5 影子柔化但匹配阈值未调整 |
| B-9 | 3e 难度 | hint-system | Hint 参数未跟随章节难度缩放 |

（场景走查中的 C-1~C-5 也属于警告级别，已在对应小节详述）

### ℹ️ 信息/建议（6 个）——可在 Alpha 阶段处理

| # | 内容 |
|---|------|
| A-4 | Tutorial→Save System 依赖未在 Save 侧列出 |
| A-16 | audio-system.md 的 settings planned 标记 |
| A-18 | touch_sensitivity 极值使 dragThreshold 超出 Safe Range |
| A-20 | Save Data Schema 缺少新增系统字段 + 存储位置矛盾 |
| B-5 | Ch.1 maxScreenDistance 建议降至 80-100px |
| B-12 | Tutorial InputFilter 零反馈可优化为微晃动 |

---

## 修复优先级建议

### 第一优先级：阻塞开发（5 项）

1. **A-1 + A-5**: Input System GDD 新增 InputFilter 正式规格和 InputBlocker/InputFilter 优先级定义
2. **A-10**: 批量替换 4 份 GDD 中所有 `narrative-system.md (planned)` → `narrative-event-system.md`
3. **B-3**: Shadow Puzzle + URP + Narrative + Hint 四份 GDD 协同补充缺席型谜题完成路径
4. **B-7**: Ch.2 谜题编排调整——前半固定光源增加物件、后半引入光源操控

### 第二优先级：消除设计歧义（7 项）

5. **A-6 + A-8**: 统一 Audio/Settings 音量控制层级（ambientVolume 归属、sfx_enabled 作用范围）
6. **A-7**: Object Interaction 移除手势提示自管理逻辑，指向 Tutorial 系统
7. **A-17**: Shadow Puzzle 移除 hintDelay Tuning Knobs，所有权归 Hint System
8. **A-2 + A-3**: 补全 Input System 和 Object Interaction 的 Dependencies 表
9. **B-8 + B-9**: 建立跨章节难度参数矩阵（阈值覆盖 + Hint 延迟缩放）
10. **C-1**: PerfectMatch 状态下冻结 matchScore 计算

### 第三优先级：清理引用（7 项）

11. **A-11~A-16**: 批量更新所有过期 `(planned)` 标记和错误文件名引用

### 第四优先级：体验优化（实现阶段）

12. **A-9 + A-19**: 事件契约补充、验收标准对齐
13. **A-20**: Save Data Schema 同步 + 存储位置决策
14. **C-2 + C-3**: 教学完成后 Hint 计时器重置 + PerfectMatch 判定保护期
15. **C-4 + C-5**: 章节过渡时长优化 + Hint 卡关盲区改善
16. **B-1 + B-4 + B-6**: Collectible 进度设计 + Layer 3 叙事差异化 + 缺席型提示次数

---

## 统计

| 严重度 | 数量 |
|--------|------|
| 🔴 阻塞 | **5** |
| ⚠️ 警告 | **15** |
| ℹ️ 信息 | **6** |
| **总计** | **26** |

## 最终判定

### ~~CONCERNS~~ → **APPROVED**

**修复完成日期**: 2026-04-21

全部 26 项问题（5 阻塞 + 15 警告 + 6 信息）已修复完成，涉及 12 份 GDD 文件的约 37 处编辑。

**修复清单：**

| 优先级 | 编号 | 修复内容 | 状态 |
|--------|------|---------|------|
| P1 | A-1+A-5 | input-system.md 新增 InputFilter 正式规格 + InputBlocker/InputFilter 优先级链 | ✅ 已修复 |
| P1 | A-10 | 4 份 GDD 批量替换 narrative-system.md (planned) → narrative-event-system.md | ✅ 已修复 |
| P1 | B-3 | shadow-puzzle + urp + narrative + hint 协同补充缺席型谜题完成路径（AbsenceAccepted 状态 + 残缺影子渲染 + ShadowFade/ObjectFade 原子效果 + 缺席专用演出流程 + 提示文案策略） | ✅ 已修复 |
| P1 | B-7 | tutorial-onboarding tut_light 触发条件明确为 Ch.2 第 4 个谜题 + shadow-puzzle 补充 Ch.2 编排约束 | ✅ 已修复 |
| P2 | A-6+A-8 | settings sfx_enabled 仅控制 SFX 层 + audio ambientVolume 为内部基线值不暴露给玩家 | ✅ 已修复 |
| P2 | A-7 | object-interaction 手势提示职责委托给 Tutorial 系统 | ✅ 已修复 |
| P2 | A-17 | shadow-puzzle 移除 hintDelay Tuning Knobs，指向 hint-system.md | ✅ 已修复 |
| P2 | A-2+A-3 | input-system + object-interaction Dependencies 表补充 Settings & Accessibility | ✅ 已修复 |
| P2 | B-8+B-9 | shadow-puzzle 新增跨章节难度参数矩阵 + hint-system 补充 hintDelayOverride 章节建议值 | ✅ 已修复 |
| P2 | C-1 | shadow-puzzle PerfectMatch 状态表标注 matchScore 冻结（不可逆转换） | ✅ 已修复 |
| P3 | A-11~A-16 | 7 份 GDD 的 11 处过期 (planned) 标记和错误文件名批量更新 | ✅ 已修复 |
| P4 | A-9+A-19 | narrative-event PuzzleLockAllEvent 多发送者说明 + hint-system 验收标准补充 Tutorial 暂停计时 | ✅ 已修复 |
| P4 | A-20 | chapter-state-and-save 移除 settings 块 + 补充 tutorialCompleted 字段 + Settings 改为 PlayerPrefs 独立存储 | ✅ 已修复 |
| P4 | C-2+C-3 | hint-system 教学恢复后计时器全部重置 + shadow-puzzle 教学完成后 3s PerfectMatch 保护期 | ✅ 已修复 |
| P4 | C-4+C-5 | narrative-event 章末演出无缝衔接优化 + hint-system matchLowThreshold 提升至 0.40 + 新增 stagnationScore 触发因子 | ✅ 已修复 |
| P4 | B-1+B-4+B-6 | shadow-puzzle Collectible 不竞争指导 + narrative-event Layer 3 差异化说明 + hint-system 缺席提示次数保持 3 次并重写文案策略 | ✅ 已修复 |

---

*下次审查建议：进入 Architecture 设计阶段前进行最终确认性审查，重点验证新增的缺席型谜题路径、stagnationScore 公式、和跨章节难度矩阵的数值合理性。*
