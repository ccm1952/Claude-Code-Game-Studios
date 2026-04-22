<!-- 该文件由Cursor 自动生成 -->
# Tutorial / Onboarding — 教学引导系统

> **Status**: Draft
> **Author**: UX Design Agent
> **Last Updated**: 2026-04-21
> **Last Verified**: 2026-04-21
> **Implements Pillar**: 克制表达 — 教学融入操作，不用大段文字解释

## Summary

教学引导系统在玩家首次接触每种新操作机制时，通过 UI 文本提示和图片动画引导玩家完成操作。教学期间锁定无关操作（只允许当前教学要求的手势），确保玩家专注学会一个操作后再开放下一个。系统在 Hint System 和 UI System 的基础上扩展，通过配置表定义每章的教学步骤，支持后续新机制（光源操控等）的教学扩展。

> **Quick reference** — Layer: `Presentation` · Priority: `Vertical Slice` · Key deps: `Hint System, UI System, Input System, Chapter State System`

## Overview

《影子回忆》的操作不复杂，但需要教——玩家需要知道可以拖拽物件、双指旋转、物件会吸附到格点。教学系统在每章首次出现新机制时激活，通过屏幕上的提示图片（手势动画）和简短文字告诉玩家"现在试试这样做"，同时通过 InputFilter 限制只允许当前教学步骤要求的手势通过。玩家成功完成操作后教学步骤完成，开放完整操作权限。教学只在首次游玩时出现，通过存档标记已完成的教学步骤。后续可通过 UI 界面文本和图片提示的方式让玩家重新查看引导操作。

## Player Fantasy

**"我不需要读说明书，试一下就会了。"**

进入第一个谜题，屏幕下方出现一个手指拖拽的动画图片和一行文字"拖动物件到新位置"。玩家试着拖动一个杯子——成功了。提示消失了。然后出现双指旋转的提示。转了一下——也会了。整个过程不到 30 秒，没有弹窗、没有教学关卡、没有"点击继续"。到了第二章，光源操控出现时，一个新的手势提示引导玩家沿轨道滑动光源。每次学一个新东西，每次都是"试一下就会了"。

## Detailed Design

### Core Rules

**教学步骤定义：**

1. 每个教学步骤（TutorialStep）包含：
   - `stepId`：唯一标识
   - `chapterId`：在哪个章节触发
   - `triggerCondition`：触发条件（进入章节/选中物件/首次双指触摸等）
   - `requiredGesture`：玩家需要完成的手势类型（Drag/Rotate/LightDrag 等）
   - `allowedGestures`：教学期间允许通过 InputFilter 的手势列表
   - `promptImagePath`：手势动画图片资源路径
   - `promptText`：提示文字（本地化 key）
   - `promptPosition`：提示显示位置（屏幕区域枚举：Bottom/Center/NearObject）
   - `completionCount`：需要成功完成几次才算通过（默认 1）
   - `order`：同章节内的教学顺序

2. 教学步骤在配置表（Luban）中定义，新增教学不需要改代码

**教学计划（MVP + VS）：**

| Step ID | Chapter | Trigger | Gesture | Prompt | Completion |
|---------|---------|---------|---------|--------|------------|
| `tut_drag` | 1 | 首次进入谜题 | Drag | "拖动物件到新位置" + 拖拽手势图 | 1 次成功拖拽 |
| `tut_rotate` | 1 | `tut_drag` 完成后 | Rotate | "双指旋转物件" + 旋转手势图 | 1 次成功旋转 |
| `tut_snap` | 1 | `tut_rotate` 完成后 | Drag（释放触发吸附） | "松开手指，物件会吸附到位" + 吸附演示图 | 1 次释放后吸附 |
| `tut_light` | 2 | Ch.2 第 4 个谜题（首个包含可操作光源的谜题）。**注意**：Ch.2 前 3 个谜题使用固定光源 + 3 物件（让玩家先适应多物件空间关系），第 4 个谜题起引入可操作光源（可回到 2 物件 + 可操作光源，降低同时学习两个新变量的认知负荷） | LightDrag | "沿轨道滑动光源" + 轨道滑动手势图 | 1 次成功移动光源 |
| `tut_distance` | 3 (TBD) | 首次需要距离调整时 | VerticalDrag | "上下滑动调整距离" + 纵向手势图 | 1 次成功距离调整 |

**教学流程：**

1. 玩家进入章节 → 系统检查该章节是否有未完成的教学步骤
2. 按 `order` 顺序取第一个未完成的步骤
3. 检查 `triggerCondition` 是否满足
4. 条件满足 → 激活教学：
   a. 通过 InputFilter 只允许 `allowedGestures`（锁定其他操作）
   b. 在 `promptPosition` 显示提示图片和文字
   c. 如果 `promptPosition` = NearObject，提示跟随目标物件
5. 玩家成功执行 `requiredGesture` 达到 `completionCount` 次
6. 教学步骤完成 → 移除 InputFilter → 隐藏提示 → 标记步骤完成（写入存档）
7. 检查下一个教学步骤，如有则继续，否则教学结束

**InputFilter 锁定规则：**

1. 教学激活时，push 一个 InputFilter 到 Input System
2. InputFilter 白名单 = `allowedGestures`，其他手势被静默吞掉（不产生事件）
3. 被阻断的手势不产生任何视觉/音频反馈（玩家不会感觉到被"阻止"，只是其他操作"没反应"）
4. 教学完成时 pop InputFilter，恢复完整操作

**提示 UI 规范：**

1. 提示由两部分组成：手势动画图片（循环播放）+ 一行文字
2. 显示在屏幕安全区域内，不遮挡核心操作区域
3. 提示出现时有淡入动画（0.3s），消失时有淡出动画（0.3s）
4. 提示文字使用本地化 key，支持多语言
5. 手势图片为预渲染的序列帧动画或 Sprite Animation

**与 Hint System 的关系：**

1. Tutorial 和 Hint System 是独立但互补的系统
2. Tutorial 教"如何操作"（手势教学），Hint 教"往哪里放"（谜题引导）
3. 教学期间 Hint System 暂停计时（不在教学时触发提示）
4. 教学完成后 Hint System 恢复正常工作
5. Object Interaction GDD 中定义的手势提示（前 N 次操作显示）由 Tutorial 系统统一管理，不再由 Object Interaction 自行处理

**重看引导：**

1. 不提供独立的"重置教程"按钮
2. 设置界面中提供"操作指南"入口，展示所有已解锁操作的文本说明和图片
3. 操作指南内容也从配置表读取，与教学步骤共享手势图片资源

### States and Transitions

**教学系统状态机：**

| State | Entry Condition | Exit Condition | Behavior |
|-------|----------------|----------------|----------|
| **Inactive** | 当前章节无未完成教学 / 教学全部完成 | 进入新章节且有未完成教学步骤 | 不显示任何提示，不施加 InputFilter |
| **WaitingTrigger** | 有未完成教学步骤但触发条件未满足 | triggerCondition 满足 → Teaching | 监听触发条件事件 |
| **Teaching** | 触发条件满足 | 玩家完成 requiredGesture × completionCount → StepComplete | 显示提示 UI，InputFilter 激活 |
| **StepComplete** | 教学步骤完成 | 有下一步 → WaitingTrigger；无下一步 → Inactive | 淡出提示，移除 InputFilter，标记步骤完成 |

### Interactions with Other Systems

**与 Input System 的交互：**

- 教学激活时 push InputFilter（白名单手势列表）
- 教学完成时 pop InputFilter
- 监听手势事件判断玩家是否完成了要求的操作

**与 Hint System 的交互：**

- 教学激活时通知 Hint System 暂停计时
- 教学完成时通知 Hint System 恢复
- 共享"手势是否已被使用"的统计数据

**与 UI System 的交互：**

- 提示 UI 通过 UIModule 的 UIWidget 显示
- 提示位置、动画、文字由 UI System 渲染

**与 Chapter State System 的交互：**

- 监听章节切换事件 → 检查新章节的教学步骤
- 读取已完成教学步骤列表（从存档）

**与 Save System 的交互：**

- 已完成的教学步骤 ID 列表持久化到存档
- 存档加载时恢复已完成列表

**与 Settings 的交互：**

- "操作指南"UI 读取教学配置表展示所有操作说明

## Formulas

### Completion Check — 完成判定

```
isStepComplete = successCount >= step.completionCount
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| successCount | int | 0-N | runtime | 玩家在当前教学步骤中成功执行目标手势的次数 |
| completionCount | int | 1-5 | config | 配置的完成所需次数 |

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|----------|------------------|-----------|
| 教学期间演出触发（PerfectMatch） | 演出优先，教学暂停（提示隐藏但不标记完成），演出结束后恢复教学 | 演出优先级高于教学 |
| 教学期间应用切后台 | 教学状态保持，切回后继续 | 不丢失教学进度 |
| 玩家在教学期间触发非白名单手势 | 静默吞掉，无任何反馈 | 避免"被拒绝"的感觉 |
| 存档损坏导致已完成教学列表丢失 | 重新触发所有教学 | 重看教学比跳过教学安全 |
| 配置表中某教学步骤的图片资源缺失 | 只显示文字，不显示图片 | 文字足够传达操作信息 |
| 新版本添加了新的教学步骤 | 老存档玩家在到达对应章节时自动触发新教学 | 新教学步骤不在已完成列表中 |
| 教学步骤要求的操作在当前谜题中不可执行 | 跳过该步骤，标记为"延迟" | 等待可执行时再触发 |

## Dependencies

| System | Direction | Nature of Dependency |
|--------|-----------|---------------------|
| Input System | This controls Input | InputFilter push/pop |
| Hint System | Bidirectional | 教学暂停/恢复 Hint 计时；共享手势统计 |
| UI System | This uses UI | 提示 UI 的显示和动画 |
| Chapter State System | CS triggers this | 章节切换检查教学步骤 |
| Save System | Bidirectional | 读写已完成教学步骤列表 |
| Settings | Settings uses this | 操作指南复用教学配置和资源 |
| Luban 配置表 | This reads from | 教学步骤定义 |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|-----------|--------------|------------|-------------------|-------------------|
| promptFadeInDuration | 0.3s | 0.1-0.5s | 提示出现更慢 | 提示出现更快 |
| promptFadeOutDuration | 0.3s | 0.1-0.5s | 提示消失更慢 | 提示消失更快 |
| promptDelayAfterTrigger | 0.5s | 0-2.0s | 触发后等更久才显示提示 | 触发后立即显示 |
| gestureAnimLoopInterval | 2.0s | 1.0-4.0s | 手势动画循环更慢 | 手势动画循环更快 |

## Acceptance Criteria

- [ ] 第一章首次进入时按顺序展示拖拽→旋转→吸附三步教学
- [ ] 教学期间只有白名单手势可操作，其他手势被静默屏蔽
- [ ] 玩家完成要求操作后教学步骤立即完成（≤ 1 帧延迟）
- [ ] 教学完成后 InputFilter 移除，所有操作立即可用
- [ ] 已完成的教学步骤在重启游戏后不会再次出现（存档标记有效）
- [ ] 第二章进入时自动触发光源操控教学
- [ ] 教学期间 Hint System 不触发提示
- [ ] 所有教学步骤、提示文字、图片资源从配置表读取，无硬编码
- [ ] 提示文字支持多语言切换
- [ ] 设置界面中"操作指南"正确显示所有已解锁操作

## Open Questions

| Question | Owner | Deadline | Resolution |
|----------|-------|----------|-----------|
| 手势提示图片是用序列帧动画还是 Lottie/Spine 动画？ | Art / Tech | VS 制作前 | 取决于美术产出流程 |
| 距离调整教学在第几章引入？ | Game Design | VS 设计阶段 | 取决于谜题设计进度 |
| 教学期间是否需要高亮目标物件引导注意力？ | UX Design | VS 测试阶段 | 先不高亮，测试后决定 |
