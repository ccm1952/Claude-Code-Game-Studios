<!-- 该文件由Cursor 自动生成 -->

# UI System — 界面系统

> **Status**: Draft
> **Author**: UI/UX Design Agent
> **Last Updated**: 2026-04-16
> **Last Verified**: 2026-04-16
> **Implements Pillar**: 克制表达——UI 应极度精简，不喧宾夺主

## Summary

界面系统是《影子回忆》中所有玩家可见的 2D 信息层的统一管理者。它负责操作提示、章节进度、暂停菜单、提示入口、谜题成功反馈、记忆碎片展示、章节过渡和主菜单/章节选择等全部 UI 面板的生命周期管理、层级排序和输入阻断协调。系统的设计核心是"最小化存在感"——游戏进行时 UI 几乎不可见，只在玩家需要时以光影般的方式浮现，完成职责后立刻消融回画面中。

> **Quick reference** — Layer: `Feature` · Priority: `MVP` · Key deps: `Shadow Puzzle System, Chapter State System`

## Overview

《影子回忆》是一款以光影和记忆为主题的叙事解谜游戏。玩家的注意力应当始终在三维场景中的物件、光源和墙面上的影子上——UI 存在的目的是在必要时提供信息和操作入口，而不是成为视觉焦点。所有 UI 面板基于 TEngine UIModule（UIWindow / UIWidget）构建，通过 ResourceModule 异步加载 Prefab，通过 GameEvent 接收系统事件驱动显隐。UI 设计语言与 Art Bible 一致：低饱和度、纤细线条、柔和渐变动效、圆角矩形。移动端 Touch 优先，严格遵守 Safe Area 规范。

## Player Fantasy

> "界面就像房间里的光——你不注意时它在，注意时已经帮你完成了。"

玩家不应该"使用"界面，而是"被界面服务"。暂停菜单在你需要休息时轻声出现；提示按钮在你困惑时恰好被看见；谜题完成时画面自然地过渡到记忆碎片，而不是弹出一个"恭喜"弹窗。整个体验中，UI 是空气——必须存在，但不应被意识到。

## Detailed Design

### UIWindow 层级体系与遮罩策略

所有 UI 面板在 TEngine UIModule 中注册，通过 SortingOrder 控制渲染层级，通过遮罩层（MaskLayer）控制背景交互和视觉隔离。

**层级定义：**

| Layer | SortingOrder 范围 | 用途 | 代表窗口 |
|-------|------------------|------|---------|
| **Background** | 0-99 | 全屏背景界面，与 3D 场景同级 | MainMenu, ChapterSelect |
| **HUD** | 100-199 | 游戏进行时的常驻/半常驻 UI | GameHUD (含 HintButton Widget) |
| **Popup** | 200-299 | 覆盖在 HUD 之上的弹窗 | PauseMenu, PuzzleCompletePanel, MemoryFragmentPanel |
| **Overlay** | 300-399 | 最高层级，全屏遮盖的过渡/遮罩 | ChapterTransition |
| **System** | 400-499 | 系统级弹窗（网络错误、存档恢复提示） | 预留，MVP 不使用 |

**遮罩策略：**

| 窗口类型 | 遮罩行为 | 遮罩颜色 | 点击遮罩 |
|---------|---------|---------|---------|
| FullScreen (Background) | 无遮罩，自身覆盖全屏 | — | — |
| FullScreen (HUD) | 无遮罩，透明底板，不阻断 3D 交互 | — | — |
| Popup | 半透明遮罩 + 高斯模糊 | `#000000` @ 40% opacity | 可配置：PauseMenu 点击遮罩关闭，PuzzleComplete 不关闭 |
| Overlay | 全屏不透明遮盖 | 随章节色温变化（暖黄→冷灰蓝） | 不可关闭（程序控制生命周期） |

**InputBlocker 协调规则：**

1. Popup 和 Overlay 层级的窗口打开时，自动向 Input System 的 `InputBlocker` 栈 push 阻断令牌
2. 窗口关闭时 pop 令牌，当栈清空后恢复 3D 交互输入
3. HUD 层级窗口不阻断输入——GameHUD 上的按钮通过 UGUI EventSystem 自行处理点击，不影响场景交互
4. 多个 Popup 同时存在时（如 PauseMenu 上再弹 SettingsPanel），栈中有多个令牌，必须全部 pop 后才恢复
5. Popup 弹出时的高斯模糊效果通过 Post-Processing Volume 的 Depth of Field 或自定义 Blur Shader 实现，模糊范围仅 3D 场景，不模糊弹窗本身

### Core Rules

#### 1. GameHUD — 游戏内抬头显示

**类型**: UIWindow (FullScreen)，HUD 层级
**Priority**: MVP
**TEngine 继承**: `UIWindow`

**功能描述**：
GameHUD 是游戏场景中唯一常驻的 UI 面板，但其设计目标是"几乎不可见"。它承载以下子 Widget：

- **HintButton (UIWidget)**: 提示按钮，屏幕右下角，44×44pt 最小触控区
- **PauseButton**: 暂停按钮，屏幕左上角 Safe Area 内
- **ChapterProgress**: 当前章节进度文本 "Ch.N · M/T"，屏幕顶部居中
- **OperationHint**: 操作提示文本，屏幕底部居中
- **SaveIndicator**: 自动保存图标（日记本翻页），屏幕右下角

**交互规则**：

1. GameHUD 在进入谜题场景时通过 UIModule 打开，离开场景时关闭
2. ChapterProgress 文本在进入场景后显示 3 秒，然后 Fade Out 至 0% 不透明度；玩家完成一个谜题后短暂重新显示
3. OperationHint 仅在玩家首次遇到新操作时显示（如首次拖拽、首次旋转），通过存档记录 `shownHints` 标记已展示的提示类型，不重复显示
4. SaveIndicator 在自动保存触发时显示 0.5 秒后 Fade Out，使用 CanvasGroup.alpha Tween
5. HintButton 默认半透明（30% opacity），玩家 30 秒未操作时缓慢提升至 80% opacity 吸引注意；点击后打开 Hint System 的当前层级提示
6. PauseButton 使用纤细线性图标（三条横线），44×44pt 触控区

**视觉规范**：
- 所有文本使用 `#F5F0E8`（柔象牙）@ 60-80% opacity
- 按钮图标使用 `#D4A574`（暖琥珀）@ 30-50% opacity
- 背景完全透明，不使用任何底板或底色
- 所有元素遵守 Safe Area 边距（顶部留出状态栏/刘海/灵动岛，底部留出 Home Indicator）

**UIWidget 结构**：

```
GameHUD (UIWindow, FullScreen)
├── SafeAreaContainer (适配安全区域)
│   ├── TopBar
│   │   ├── PauseButton (左上)
│   │   └── ChapterProgress (顶部居中)
│   ├── BottomBar
│   │   ├── OperationHint (底部居中)
│   │   └── SaveIndicator (右下)
│   └── RightPanel
│       └── HintButton (UIWidget, 右下偏上)
```

#### 2. PauseMenu — 暂停菜单

**类型**: UIWindow (Popup)，Popup 层级
**Priority**: MVP
**TEngine 继承**: `UIWindow`

**功能描述**：
玩家点击 PauseButton 或设备返回键时弹出的暂停菜单。提供三个选项：继续游戏、设置、返回主菜单。

**交互规则**：

1. 打开时：场景时间暂停（`Time.timeScale = 0`），3D 场景高斯模糊，菜单面板从中央 Fade In + 微缩放（0.95→1.0，300ms EaseOutCubic）
2. 点击"继续"：面板 Fade Out（200ms），恢复 `Time.timeScale = 1`，关闭 UIWindow
3. 点击"设置"：在 PauseMenu 之上打开 SettingsPanel（Popup 层级，SortingOrder 更高），PauseMenu 保持但变暗
4. 点击"返回主菜单"：弹出二次确认对话框（"当前进度已自动保存，确认返回？"），确认后触发场景卸载 → 加载 MainMenu
5. 点击遮罩区域等同于点击"继续"（可配置）
6. PauseMenu 打开时 push InputBlocker 令牌，关闭时 pop

**视觉规范**：
- 菜单面板：居中圆角矩形（圆角 12px @1080p），`#2C2C2E` @ 70% opacity 底板
- 三个选项垂直排列，间距 48px，文字使用 `#F5F0E8` @ 90% opacity
- 选项之间使用 `#D4A574` @ 20% opacity 的细分割线（1px）
- 背景遮罩：`#000000` @ 40% opacity + 场景模糊（Blur Radius 8px）
- 无标题栏，无关闭按钮——点击遮罩即关闭

**菜单项布局**：

| 选项 | 图标 | 文本 | 行为 |
|-----|------|-----|------|
| 继续 | ▶ (play) | "继续" / "Continue" | 关闭菜单，恢复游戏 |
| 设置 | ⚙ (gear) | "设置" / "Settings" | 打开 SettingsPanel |
| 主菜单 | ⌂ (home) | "主菜单" / "Main Menu" | 二次确认 → 返回主菜单 |

#### 3. HintButton — 提示按钮

**类型**: UIWidget（嵌入 GameHUD）
**Priority**: MVP
**TEngine 继承**: `UIWidget`

**功能描述**：
Hint System 的入口。作为 GameHUD 的子 Widget 存在，提供玩家主动请求提示的能力。

**交互规则**：

1. 默认状态：圆形灯泡图标，`#D4A574` @ 30% opacity，44×44pt 触控区
2. 当 Hint System 判定玩家可能卡关（30 秒无有效操作）时：图标 opacity 缓慢升至 80%，伴随极轻微的呼吸脉冲动画（scale 1.0↔1.05，周期 2 秒）
3. 点击后：
   - 如果提示系统有可用提示（Tier 1/2/3），发送 `GameEvent: HintRequested`，Hint System 决定显示哪层提示
   - 提示内容直接叠加在 3D 场景中（高亮物件、连线、方向箭头），不通过 UI 弹窗展示
   - 按钮进入 cooldown 状态（10 秒），期间灰显不可点击
4. 在 PerfectMatch / Complete 状态下自动隐藏
5. 在谜题 Idle 状态（玩家尚未操作）时不显示

**视觉规范**：
- 图标：线性灯泡，描边 2px，与全局图标风格一致
- cooldown 期间：图标旋转 45°，变为灰色（`#8B7355` @ 50% opacity），使用环形进度条显示剩余 cooldown
- 无文字标签——纯图标

#### 4. PuzzleCompletePanel — 谜题完成面板

**类型**: UIWindow (Popup)，Popup 层级
**Priority**: MVP
**TEngine 继承**: `UIWindow`

**功能描述**：
当谜题达到 PerfectMatch 并完成吸附动画后短暂显示的成功反馈面板。这不是一个传统的"恭喜通关"弹窗——它是影子成形这一高光时刻的视觉延伸。

**交互规则**：

1. 触发条件：Shadow Puzzle System 发送 `GameEvent: PuzzleCompleted`
2. 不立即弹出——先等待 PerfectMatch 吸附动画（300ms 静止 + 500ms 吸附）完成
3. 打开时：
   - 3D 场景保持可见但冻结（影子定格在完美位置）
   - 画面整体色温微暖 +5（通过 Post-Processing Volume Lerp）
   - 面板从画面底部轻缓上升（500ms EaseOutCubic），背景不使用纯黑遮罩而是使用极淡的暖色渐变（`#D4A574` @ 15% opacity）
4. 面板内容：
   - 谜题名称（如果该谜题有配置 `nameKey`）
   - 一句关系暗示语（如"两只杯子，面对面。"），从 Luban 配置表读取
   - 如果是章节最后一个谜题，显示"章节完成"标识
5. 面板显示 2-3 秒后自动消失（Fade Out 500ms），或玩家点击任意位置提前关闭
6. 关闭后：
   - 若非章末谜题 → 场景自然过渡到下一个谜题的 Idle 状态
   - 若是章末谜题 → 触发 MemoryFragmentPanel 或 ChapterTransition

**视觉规范**：
- 面板位于画面下半部分（不遮挡影子投影区域），高度不超过屏幕的 30%
- 文本：谜题名 24pt，关系暗示语 18pt，`#F5F0E8` @ 90% opacity
- 无按钮、无边框、无关闭图标——极简纯文本浮层
- 背景：从底部向上的线性渐变 `#2C2C2E` @ 0% → 60%，模拟"影子中浮现文字"的效果

#### 5. MemoryFragmentPanel — 记忆碎片展示面板

**类型**: UIWindow (Popup)，Popup 层级
**Priority**: MVP
**TEngine 继承**: `UIWindow`

**功能描述**：
在关键谜题（通常是章节终局谜题）完成后展示的叙事内容面板。呈现一段"记忆碎片"——可以是一句话、一张模糊的照片、一段声音的文字描述，或这些元素的组合。

**交互规则**：

1. 触发条件：`GameEvent: NarrativeFragmentReady(fragmentId)`，由 Narrative Event System 在谜题完成演出后发送
2. 打开时：
   - 3D 场景完全冻结，高斯模糊（Blur Radius 12px）
   - 面板从全透明缓慢浮现（800ms EaseOutCubic）
   - 如有文本，使用打字机效果逐字显示（每字 80-120ms），中文标点后有 200ms 额外停顿
3. 面板内容（由 Luban 配置表 `TbMemoryFragment` 定义）：
   - `fragmentType: text` → 纯文本显示
   - `fragmentType: image` → 模糊照片 + 字幕文本
   - `fragmentType: mixed` → 照片在上，文本在下
4. 玩家向上滑动或点击"继续"按钮关闭面板（"继续"按钮在文本显示完毕后才出现，防止跳过）
5. 关闭动画：面板整体 Fade Out（500ms），同时 3D 场景模糊逐渐消散
6. 记忆碎片一旦展示，记录到存档的收集物列表，可在"记忆碎片库"中重新查看（Vertical Slice 功能）

**视觉规范**：
- 面板居中，宽度占屏幕 80%，高度自适应内容（最大 70% 屏幕高度，超出可滚动）
- 文本使用手写体风格字体（或常规字体的 Italic 变体），暗示这是"记忆中的文字"
- 照片使用柔焦 + 轻微色偏（偏暖或偏冷取决于章节色温），不显示锐利清晰的图片
- 底板：`#2C2C2E` @ 80% opacity，圆角 16px
- "继续"按钮位于底部中央，文字 `#D4A574`，无边框，仅文字可点击

#### 6. ChapterTransition — 章节过渡界面

**类型**: UIWindow (FullScreen)，Overlay 层级
**Priority**: MVP
**TEngine 继承**: `UIWindow`

**功能描述**：
章节之间的全屏过渡界面。它不仅是一个加载屏，更是情绪从一个关系阶段到下一个阶段的过渡桥梁。

**交互规则**：

1. 触发条件：`GameEvent: ChapterCompleted(chapterId)` → 章末演出结束 → `ChapterOutroFinished`
2. 打开流程（总时长 3-5 秒，视内容而定）：
   - Phase 1（1s）：场景淡入全屏纯色（颜色 = 当前章节的结束色温映射色）
   - Phase 2（1-2s）：显示下一章标题和主题词（如"第二章 · 共同空间"），居中，大号字，Fade In
   - Phase 3（0.5s）：标题 Fade Out
   - Phase 4（1s）：全屏纯色过渡到下一章的起始色温映射色，然后 Fade Out 显示新场景
3. 在 Phase 2 期间触发后台场景加载（旧场景卸载 + 新场景加载 via ResourceModule/SceneModule）
4. 如果场景加载未完成但 Phase 3 已到：延长 Phase 2 的标题显示时间（添加微弱的光晕呼吸动画），直到场景就绪
5. 全程不可交互——不响应任何触摸输入

**视觉规范**：
- 全屏覆盖，无任何 UI 装饰元素（无进度条、无 loading 图标）
- 章节标题：字号 36pt（@1080p），`#F5F0E8` @ 90%
- 主题词：字号 20pt，`#F5F0E8` @ 60%，位于标题下方 24px
- 全屏底色按章节色彩弧线：
  - Ch.1→2 过渡色：`#F5F0E8` 淡暖 → `#D4A574` 暖琥珀
  - Ch.2→3 过渡色：`#D4A574` 暖琥珀 → `#E8976B` 记忆橙
  - Ch.3→4 过渡色：`#E8976B` 记忆橙 → `#6B7B8D` 静谧蓝（关键情绪转折，过渡时长 +50%）
  - Ch.4→5 过渡色：`#6B7B8D` 静谧蓝 → `#7BA5A0` 释然青

#### 7. MainMenu — 主菜单

**类型**: UIWindow (FullScreen)，Background 层级
**Priority**: MVP
**TEngine 继承**: `UIWindow`

**功能描述**：
游戏启动后的首个界面。以极简方式呈现游戏标题和两个核心入口。

**交互规则**：

1. 打开时机：游戏启动完成（Save System 加载完毕）后打开
2. 菜单项：
   - **继续游戏**（仅在存档存在时显示）：加载存档中的当前章节和谜题场景，直接进入 GameHUD
   - **新游戏**（常驻）：如有存档则弹出确认对话框（"将覆盖现有进度，确认？"），确认后清除存档并从 Chapter 1 开始
   - **章节选择**（仅在至少完成 Chapter 1 后显示）：打开 ChapterSelect 界面
   - **设置**：打开 SettingsPanel
3. 背景场景：一个安静的室内角落（可使用 Chapter 1 的场景预览或专用的主菜单场景），台灯微弱亮着，有极轻的灰尘粒子
4. 菜单项使用列表布局，位于画面左侧偏下（不遮挡背景场景中心的光影），点击无按钮外观——纯文字 + 触摸高亮

**视觉规范**：
- 游戏标题"影子回忆"位于画面上方 1/3，使用特制标题字体（手写风或 Serif 变体），`#F5F0E8` @ 100%
- 英文副标题 "Shadow Memory" 位于中文标题下方 8px，字号更小，`#F5F0E8` @ 50%
- 菜单项：左对齐，字号 22pt，`#F5F0E8` @ 70%，点击/按下时 → 100% opacity + 微缩放 1.05
- 菜单项间距 56px
- 版本号位于屏幕右下角，字号 12pt，`#F5F0E8` @ 30%

#### 8. ChapterSelect — 章节选择

**类型**: UIWindow (FullScreen)，Background 层级
**Priority**: Vertical Slice
**TEngine 继承**: `UIWindow`

**功能描述**：
横向滚动的章节选择界面，展示 5 个章节的缩略图和状态。

**交互规则**：

1. 从 MainMenu 或 PauseMenu 进入
2. 5 个章节卡片横向排列，支持左右滑动浏览，当前居中卡片放大突出
3. 每张卡片显示：
   - 章节缩略图（该章代表性场景的截图/手绘，低饱和度处理）
   - 章节编号和名称
   - 状态标记：🔒 Locked / ▶ Current / ✓ Complete
   - 完成章节下方显示谜题列表入口（可展开查看回放列表）
4. 点击 Locked 章节：卡片轻微摇头动画（左右偏转 3°，300ms），不弹提示文字
5. 点击 Current 章节：直接进入当前进度的谜题场景
6. 点击 Complete 章节：展开谜题列表，选择具体谜题进入回放
7. 返回按钮位于左上角（Safe Area 内），返回 MainMenu

**视觉规范**：
- 卡片尺寸：屏幕宽度 60% × 高度 40%，圆角 16px
- 当前居中卡片 scale 1.0，两侧卡片 scale 0.85 + opacity 60%
- Locked 卡片：灰度处理 + 锁图标居中覆盖
- Complete 卡片：右上角金色光环标记（`#D4A574` 发光）
- 背景：与 MainMenu 共享背景场景，或使用纯色渐变

#### 9. SettingsPanel — 设置面板

**类型**: UIWindow (Popup)，Popup 层级
**Priority**: Vertical Slice
**TEngine 继承**: `UIWindow`

**功能描述**：
玩家偏好设置面板，提供音量、触感、辅助功能等选项。

**交互规则**：

1. 从 PauseMenu 或 MainMenu 的"设置"选项打开
2. 设置项分组：
   - **音量**：音乐音量滑杆、音效音量滑杆（0-100%）
   - **触感**：振动反馈开关（仅移动端显示）
   - **辅助功能**：高对比度影子开关、影子描边辅助开关、字号切换（标准/大/超大）、格点吸附力度（弱/标准/强）
   - **语言**：中文/English 切换
3. 所有设置变更实时生效（不需要"应用"按钮），变更同时通过 Save System 持久化
4. 关闭方式：点击左上角返回按钮或点击遮罩区域

**视觉规范**：
- 面板居中，宽度 85% 屏幕，高度自适应（最大 80% 屏幕高度），超出可滚动
- 底板：`#2C2C2E` @ 80% opacity，圆角 12px
- 分组标题：`#D4A574` @ 80%，16pt
- 设置项标签：`#F5F0E8` @ 80%，16pt
- 滑杆轨道：`#F5F0E8` @ 20%，滑块：`#D4A574` @ 100%
- 开关：iOS 风格 Toggle，开启色 `#D4A574`，关闭色 `#8B7355` @ 50%

### States and Transitions

**全局 UI 状态机：**

游戏在任意时刻处于以下 UI 状态之一，决定了哪些 UIWindow 可见：

| State | 可见窗口 | Entry Condition | Exit Condition |
|-------|---------|----------------|----------------|
| **MainMenuState** | MainMenu | 游戏启动 / 从游戏返回主菜单 | 点击"继续游戏"或"新游戏" |
| **ChapterSelectState** | ChapterSelect | 从 MainMenu 进入章节选择 | 选择章节 / 返回 MainMenu |
| **GameplayState** | GameHUD | 进入谜题场景 | 暂停 / 谜题完成 / 返回主菜单 |
| **PausedState** | GameHUD + PauseMenu | 点击暂停按钮 | 继续游戏 / 返回主菜单 |
| **PuzzleCompleteState** | GameHUD + PuzzleCompletePanel | PerfectMatch 完成 | 面板自动消失 |
| **MemoryFragmentState** | MemoryFragmentPanel | Narrative 触发记忆碎片 | 玩家关闭面板 |
| **TransitionState** | ChapterTransition | 章节完成 | 新场景加载完毕 |

**状态转换图：**

```
MainMenuState ──→ GameplayState ──→ PausedState
     │                  │                │
     │                  ▼                │
     │          PuzzleCompleteState      │
     │                  │                │
     │                  ▼                │
     │          MemoryFragmentState      │
     │                  │                │
     │                  ▼                │
     │          TransitionState ──→ GameplayState (下一章)
     │                                   │
     ▼                                   │
ChapterSelectState ──→ GameplayState     │
                                         │
     ◄───────────── MainMenuState ◄──────┘
```

### Interactions with Other Systems

**与 Shadow Puzzle System 的交互：**

| 数据方向 | 内容 | 机制 |
|---------|------|------|
| Puzzle → UI | 谜题状态变更（NearMatch/PerfectMatch/Complete） | `GameEvent: PuzzleStateChanged` |
| Puzzle → UI | 物件选中/操作状态（用于显示操作提示） | `GameEvent: ObjectSelected` / `ObjectReleased` |
| UI → Puzzle | 提示请求 | `GameEvent: HintRequested` |

**与 Chapter State System 的交互：**

| 数据方向 | 内容 | 机制 |
|---------|------|------|
| Chapter → UI | 当前章节/谜题进度 | 查询接口：`ChapterStateSystem.GetCurrentProgress()` |
| Chapter → UI | 章节解锁/完成事件 | `GameEvent: ChapterCompleted` / `ChapterUnlocked` |
| Chapter → UI | 各章节状态列表（用于章节选择界面）| 查询接口：`ChapterStateSystem.GetAllChapterStates()` |

**与 Input System 的交互：**

| 数据方向 | 内容 | 机制 |
|---------|------|------|
| UI → Input | Popup/Overlay 打开时 push InputBlocker 令牌 | `InputBlocker.Push(token)` |
| UI → Input | Popup/Overlay 关闭时 pop InputBlocker 令牌 | `InputBlocker.Pop(token)` |

**与 Save System 的交互：**

| 数据方向 | 内容 | 机制 |
|---------|------|------|
| Save → UI | 存档存在标记（控制 MainMenu "继续游戏"显示）| 查询接口 |
| Save → UI | 设置数据（SettingsPanel 初始值）| 查询接口 |
| UI → Save | 设置变更 | `SaveSystem.SaveSettings()` |
| Save → UI | 自动保存事件（触发 SaveIndicator 显示）| `GameEvent: AutoSaveTriggered` |

**与 Narrative Event System 的交互：**

| 数据方向 | 内容 | 机制 |
|---------|------|------|
| Narrative → UI | 记忆碎片内容就绪 | `GameEvent: NarrativeFragmentReady(fragmentId)` |
| Narrative → UI | 章末演出结束 | `GameEvent: ChapterOutroFinished` |

**与 Audio System 的交互：**

| 数据方向 | 内容 | 机制 |
|---------|------|------|
| UI → Audio | 按钮点击音效请求 | `AudioModule.PlaySound(sfxId)` |
| UI → Audio | 暂停时降低 BGM 音量 | `AudioModule.SetMusicVolume(pausedVolume)` |

**与 Hint System 的交互：**

| 数据方向 | 内容 | 机制 |
|---------|------|------|
| UI → Hint | 玩家主动请求提示 | `GameEvent: HintRequested` |
| Hint → UI | 当前可用提示层级（控制 HintButton 外观）| `GameEvent: HintAvailabilityChanged(tier)` |
| Hint → UI | 卡关检测（控制 HintButton 呼吸动画）| `GameEvent: PlayerStuckDetected` |

## Formulas

### UI Animation Duration Scaling（UI 动效时长缩放）

辅助功能中的"减弱动态效果"选项会全局缩放 UI 动画时长：

```
actualDuration = baseDuration * animationScale
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| baseDuration | float | 0.1-3.0s | 各窗口配置 | UI 动画的基础时长 |
| animationScale | float | 0.0-1.0 | settings | 0.0 = 禁用所有动画（立即显隐），1.0 = 标准时长 |

**Expected output range**: 0ms (动画禁用) to 3000ms (最长动画)
**Edge case**: animationScale = 0 时，所有 Tween 跳到终态，CanvasGroup.alpha 直接设为目标值。

### HintButton Opacity Ramp（提示按钮透明度渐变）

```
currentOpacity = baseOpacity + (maxOpacity - baseOpacity) * clamp((idleTime - rampStart) / rampDuration, 0, 1)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| baseOpacity | float | 0.3 | config | 按钮默认不透明度 |
| maxOpacity | float | 0.8 | config | 最大不透明度（吸引注意） |
| idleTime | float | 0-∞s | runtime | 玩家未操作的累计时间 |
| rampStart | float | 30s | config | 开始渐变的空闲时间阈值 |
| rampDuration | float | 10s | config | 从 baseOpacity 到 maxOpacity 的过渡时长 |

**Expected output range**: 0.3 to 0.8
**Edge case**: 玩家在渐变过程中恢复操作 → idleTime 重置为 0，opacity 以相同 rampDuration 回落到 baseOpacity。

### ChapterTransition Duration（章节过渡总时长）

```
totalDuration = phase1 + phase2 + phase3 + phase4

phase2 = max(baseTitleDuration, sceneLoadTime - phase1 - phase3 - phase4)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| phase1 | float | 1.0s | config | 场景淡出至纯色的时长 |
| baseTitleDuration | float | 1.5s | config | 章节标题最短显示时间 |
| phase3 | float | 0.5s | config | 标题淡出时长 |
| phase4 | float | 1.0s | config | 纯色过渡到新场景的时长 |
| sceneLoadTime | float | 0.5-5.0s | runtime | 新场景实际加载耗时 |

**Expected output range**: 3.0s (场景加载极快) to 8.0s+ (场景加载缓慢)
**Edge case**: sceneLoadTime > 5s 时，phase2 期间在章节标题下方追加极低 opacity（20%）的光晕呼吸动画，暗示"加载中"。

### OperationHint Display Logic（操作提示显示逻辑）

```
shouldShow = !shownHints.Contains(hintType) && isFirstOccurrence(hintType)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| hintType | enum | Drag/Rotate/Pinch/LightDrag | runtime | 当前操作的手势类型 |
| shownHints | Set\<enum\> | — | Save System | 已显示过的操作提示类型集合 |

**Expected output**: true (首次该操作) / false (已展示过)
**Edge case**: 存档丢失时 shownHints 为空，所有提示重新显示——这是可接受的行为。

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|----------|------------------|-----------|
| **快速连续弹窗**：PuzzleComplete 尚未关闭时 MemoryFragment 事件到达 | MemoryFragment 进入队列等待，PuzzleComplete 关闭后再弹出 MemoryFragment；保证弹窗依次出现，不叠加 | 避免两个 Popup 同时覆盖导致视觉混乱和触摸穿透 |
| **UI 与 3D 交互的触摸穿透** | GameHUD 上的按钮区域通过 UGUI EventSystem 标记为 Raycast Target，命中按钮的触摸不传递到 3D Raycast 层；Popup 打开时 InputBlocker 完全阻断 3D 输入 | 确保 UI 和 3D 场景的输入互斥 |
| **PauseMenu 打开期间 PuzzleComplete 事件到达** | 事件进入队列，PauseMenu 关闭后检查队列并依次处理 | 暂停期间不弹任何新窗口，保持"暂停状态"的心智模型一致 |
| **ChapterTransition 中玩家按 Home 键** | OnApplicationPause 触发 Save → 恢复后 Transition 继续播放（从中断帧继续）| 过渡动画使用 unscaledTime 不受暂停影响 |
| **快速连点 PauseButton** | 第一次点击打开 PauseMenu 后，按钮进入 0.3 秒 cooldown 不响应后续点击；PauseMenu 关闭动画完成前不响应新的打开请求 | 防止菜单反复开关导致动画叠加 |
| **SettingsPanel 修改字号后当前面板布局溢出** | 字号变更立即触发所有可见面板的 Layout Rebuild；如果内容超出面板区域则启用 ScrollView | 实时预览变更，不需要重启 |
| **MainMenu "新游戏"确认对话框中玩家按 Home 键** | 对话框状态保持，恢复后仍在确认对话框中 | 不因为切后台丢失用户选择 |
| **Safe Area 在不同设备上差异极大**（iPhone 15 Pro 灵动岛 vs iPhone SE 无刘海） | 所有 UI 元素相对于 Unity SafeArea Rect 定位，Prefab 使用 SafeAreaFitter 脚本自动适配 | 统一适配方案 |
| **ChapterTransition 场景加载失败** | 显示简洁的错误提示（"加载失败，请重试"），提供重试按钮；3 次重试失败后返回 MainMenu | 不让玩家卡在空白过渡画面中 |
| **多个 InputBlocker 令牌未正确释放**（代码 bug） | InputBlocker 提供 ForceReset() 安全阀，在返回 MainMenu 时调用一次清空栈 | 防止输入永久锁死 |
| **PuzzleCompletePanel 自动关闭与玩家点击同时发生** | 使用标志位 `isClosing` 防止 Close 逻辑执行两次 | 避免双重关闭导致 UIWindow 状态异常 |
| **低端设备上高斯模糊卡顿** | 检测设备 GPU 能力（SystemInfo.graphicsMemorySize），低端设备降级为半透明黑色遮罩替代模糊效果 | 保证性能优先 |
| **横竖屏切换**（尽管本游戏固定竖屏/横屏） | 游戏锁定为横屏模式（landscape），但在极端情况下（Android split screen）仍能 Safe Area 自适应 | 防御性设计 |

## Dependencies

| System | Direction | Nature of Dependency |
|--------|-----------|---------------------|
| Shadow Puzzle System | UI depends on Puzzle | 接收谜题状态变更事件驱动 PuzzleComplete / HintButton 行为 |
| Chapter State System | UI depends on Chapter | 获取章节/谜题进度数据驱动 GameHUD、ChapterSelect、ChapterTransition |
| Input System | Bidirectional | UI 通过 InputBlocker 控制游戏输入启停；Input 在 UI 层之下 |
| Save System | Bidirectional | UI 读取存档数据（设置/进度）；UI 写回设置变更 |
| Narrative Event System | UI depends on Narrative | 接收记忆碎片事件驱动 MemoryFragmentPanel |
| Audio System | UI depends on Audio | UI 按钮音效和暂停音量控制 |
| Hint System | Bidirectional | UI 发送提示请求，Hint 返回可用性状态 |
| Tutorial / Onboarding | Tutorial depends on UI | 教程系统基于 UI 组件展示引导信息 |
| Settings & Accessibility | Settings depends on UI | 设置界面由 UI 系统提供 |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|-----------|--------------|------------|-------------------|-------------------|
| hud_fadeout_delay | 3.0s | 1.0-5.0s | ChapterProgress 显示更久，可能干扰沉浸感 | 信息一闪而过，可能错过 |
| hud_element_opacity | 0.7 | 0.3-1.0 | UI 元素更醒目但可能与"隐于体验"冲突 | 更隐蔽但可能难以发现 |
| hint_button_base_opacity | 0.3 | 0.1-0.5 | 提示按钮更可见 | 更隐蔽，依赖呼吸动画吸引注意 |
| hint_button_ramp_start | 30s | 15-60s | 更晚开始吸引注意，给玩家更多自主时间 | 更早提醒提示可用 |
| pause_blur_radius | 8px | 4-16px | 更强的模糊，场景更隐约 | 更弱的模糊，暂停时仍能看清场景 |
| puzzle_complete_display_time | 2.5s | 1.5-4.0s | 成功反馈展示更久，更有仪式感 | 更快推进，节奏更紧凑 |
| typewriter_char_delay | 100ms | 60-150ms | 打字机效果更慢，更有阅读仪式感 | 更快显示，等待时间更短 |
| transition_phase2_base | 1.5s | 1.0-3.0s | 章节标题显示更久 | 过渡更快 |
| popup_mask_opacity | 0.4 | 0.2-0.6 | 遮罩更暗，弹窗更突出 | 遮罩更淡，场景更可见 |
| button_cooldown | 0.3s | 0.1-0.5s | 更长的防连点间隔 | 更灵敏的操作响应 |
| animation_scale | 1.0 | 0.0-1.0 | 标准动画时长 | 快速/禁用动画（辅助功能） |

## Visual/Audio Requirements

| Event | Visual Feedback | Audio Feedback | Priority |
|-------|----------------|---------------|----------|
| PauseMenu 打开 | 场景高斯模糊 + 菜单 FadeIn | 轻微的"翻页"音 | MVP |
| PauseMenu 关闭 | 模糊消散 + 菜单 FadeOut | 轻微的"合上"音 | MVP |
| HintButton 点击 | 按钮脉冲缩放 + opacity 闪亮 | 柔和的叮声 | MVP |
| HintButton 呼吸动画 | scale 1.0↔1.05 呼吸 + opacity 渐升 | 无音效（静默吸引） | MVP |
| PuzzleComplete 弹出 | 面板从底部轻缓上升 + 色温微暖 | 温暖的共鸣余韵 | MVP |
| MemoryFragment 打开 | 场景模糊 + 面板 Fade In + 打字机文本 | 轻柔环境变化音（如遥远的钢琴单音） | MVP |
| MemoryFragment 关闭 | 面板 Fade Out + 场景清晰化 | 轻微的呼吸声消散 | MVP |
| ChapterTransition 全过程 | 全屏色彩过渡 + 标题浮现消隐 | 章节过渡专属音效（弦乐渐变） | MVP |
| 自动保存触发 | SaveIndicator 日记本翻页动画（0.5s） | 无（完全静默保存） | MVP |
| MainMenu 菜单项 Hover/Press | 文字 opacity 70%→100% + scale 1.0→1.05 | 极轻微的 hover 音 | MVP |
| 章节选择滑动 | 卡片缩放切换（当前 1.0，两侧 0.85） | 轻微的滑动音 | Vertical Slice |
| 章节选择点击 Locked | 卡片左右摇头 3° | 低沉的"thud"音（被阻挡感） | Vertical Slice |
| SettingsPanel 滑杆拖动 | 滑块跟随手指 + 轨道填充色变化 | 实时预览音量的背景音乐 | Vertical Slice |
| 确认对话框弹出 | 小面板从中央缩放 FadeIn | 轻微的注意提示音 | MVP |

## Game Feel

### Feel Reference

应该感觉像 **Florence 的 UI** — 界面本身就是叙事的一部分，打开菜单像翻动一本精装书，关闭菜单像合上日记。所有交互都是柔和的、有呼吸感的。**不应该**感觉像一般手游的 UI 那样充满按钮、弹窗和闪烁高亮。

辅助参考：**Gris 的 UI 极简主义** — 游戏中几乎看不到 UI，暂停菜单是唯一的显式界面，其出现和消失都是画面的一部分而非覆盖层。

反面参考：**Candy Crush / 原神式 UI** — 满屏图标、红点通知、弹窗轰炸。这恰恰是本作要避免的。

### Input Responsiveness

| Action | Max Input-to-Response Latency (ms) | Frame Budget (at 60fps) | Notes |
|--------|-----------------------------------|------------------------|-------|
| 按钮点击视觉反馈 | 50ms | 3 frames | 按下瞬间即变 opacity/scale |
| PauseMenu 开始出现 | 100ms | 6 frames | 允许 blur 计算延迟 |
| PauseMenu 完全展开 | 400ms | 24 frames | 包含 Fade+Scale 动画 |
| PuzzleComplete 面板出现 | 500ms | 30 frames | 刻意延迟，匹配吸附动画节奏 |
| ChapterTransition 开始 | 100ms | 6 frames | 事件到达后立即开始淡出 |
| MemoryFragment 面板出现 | 800ms | 48 frames | 缓慢浮现，匹配叙事节奏 |
| SettingsPanel 设置生效 | 0ms | 0 frames | 滑杆/开关的值变更立即生效 |

### Animation Feel Targets

| Animation | Startup Frames | Active Frames | Recovery Frames | Feel Goal | Notes |
|-----------|---------------|--------------|----------------|-----------|-------|
| 按钮 Press 缩放 | 0 | 3 | 6 | 即时反馈，弹回 | EaseOutBack |
| PauseMenu FadeIn | 0 | 18 | 0 | 安静地出现 | EaseOutCubic |
| PauseMenu FadeOut | 0 | 12 | 0 | 迅速消隐 | EaseInCubic |
| PuzzleComplete 上升 | 0 | 30 | 0 | 轻缓浮现 | EaseOutCubic |
| MemoryFragment 浮现 | 0 | 48 | 0 | 像记忆慢慢浮上来 | EaseOutQuad |
| MemoryFragment 消隐 | 0 | 30 | 0 | 如梦境消散 | EaseInQuad |
| ChapterTransition Phase1 | 0 | 60 | 0 | 世界缓缓暗去 | Linear |
| ChapterTransition Phase4 | 0 | 60 | 0 | 新世界缓缓亮起 | Linear |
| HintButton 呼吸脉冲 | 0 | 120 (循环) | 0 | 微弱、不扰人 | Sine Wave |
| SaveIndicator 闪现 | 3 | 15 | 12 | 低调，一闪而过 | FadeIn-Hold-FadeOut |

### Impact Moments

本系统不包含强 Impact Moments——UI 系统的"高光时刻"由它服务的系统（Shadow Puzzle 的 PerfectMatch、Narrative 的记忆重现）提供。UI 的角色是"不添噪声地传递那些时刻"。

| Impact Type | Duration (ms) | Effect Description | Configurable? |
|-------------|--------------|-------------------|---------------|
| PuzzleComplete 色温微暖 | 500ms | Post-Processing Color Temperature +5，配合面板出现 | Yes |
| MemoryFragment 场景模糊 | 800ms | 场景从清晰到模糊，注意力从场景转移到文字 | Yes |
| ChapterTransition 全屏色彩 | 3000-5000ms | 整个画面浸入章节色温，是视觉上"翻篇"的仪式 | Yes |

### Weight and Responsiveness Profile

- **Weight**: 轻盈如纸。UI 没有重量感——面板是"浮现"和"消融"的，不是"滑入"和"弹出"的。
- **Player control**: 高度控制。暂停可随时打开/关闭，MemoryFragment 可主动跳过（文字显示完毕后）。ChapterTransition 是唯一不可控的——但它是叙事节奏的一部分。
- **Snap quality**: 柔和过渡。没有任何硬切、闪烁或弹性动画。所有状态变化都是渐变的。
- **Acceleration model**: 缓入缓出。所有动画使用 Ease 曲线，没有线性运动（ChapterTransition 的 Fade 除外，线性 Fade 更符合"光线渐变"的物理感）。
- **Failure texture**: 不存在"UI 失败"。按钮 cooldown 通过灰显传达，Locked 章节通过摇头动画传达——都是温和的"还不行哦"，不是红叉或报错。

### Feel Acceptance Criteria

- [ ] 测试者在谜题操作过程中不会说"UI 挡住了我的视线"
- [ ] 没有测试者主动寻找暂停按钮超过 3 秒——按钮位置直觉可见
- [ ] PuzzleComplete 到 MemoryFragment 的过渡被形容为"自然"或"流畅"，不是"弹窗"
- [ ] 章节过渡被形容为"像翻了一页"而非"在等加载"
- [ ] HintButton 的呼吸动画被 70%+ 的测试者在卡关时注意到
- [ ] 所有按钮在 iPhone 13 Mini 上可被单拇指舒适触达（无需双手操作）
- [ ] 辅助功能测试者能够在不依赖默认字号的情况下顺利完成游戏
- [ ] 60fps 设备上所有 UI 动画无可感知掉帧

## UI Requirements

| Information | Display Location | Update Frequency | Condition |
|-------------|-----------------|-----------------|-----------|
| 当前章节/谜题进度 | GameHUD 顶部居中 | 场景进入时 + 谜题完成时 | 显示 3 秒后 FadeOut，谜题完成后短暂重新显示 |
| 操作方式提示 | GameHUD 底部居中 | 首次使用新操作时 | 仅首次显示，不重复 |
| 提示按钮 | GameHUD 右下 | 常驻（opacity 变化） | 谜题 Active 时显示，Idle/Complete 时隐藏 |
| 暂停入口 | GameHUD 左上 | 常驻 | 谜题场景中始终可见 |
| 自动保存标记 | GameHUD 右下 | 保存事件触发时 | 0.5 秒闪现 |
| 暂停菜单选项 | PauseMenu 居中 | 打开时 | 暂停状态下 |
| 谜题完成信息 | PuzzleComplete 底部 | 事件触发时 | PerfectMatch 后 |
| 记忆碎片内容 | MemoryFragment 居中 | 事件触发时 | 叙事事件后 |
| 章节标题 | ChapterTransition 居中 | 章节切换时 | 过渡期间 |
| 菜单选项 | MainMenu 左下 | 启动时 | 主菜单状态 |
| 章节列表及状态 | ChapterSelect 横向 | 进入时 | 章节选择状态 |
| 设置项 | SettingsPanel 居中 | 打开时 | 设置状态 |

## Cross-References

| This Document References | Target GDD | Specific Element Referenced | Nature |
|--------------------------|-----------|----------------------------|--------|
| 谜题状态变更驱动 PuzzleComplete 和 HintButton | `design/gdd/shadow-puzzle-system.md` | PuzzleStateChanged 事件、NearMatch/PerfectMatch 阈值 | State trigger |
| 章节/谜题进度数据驱动 HUD 和 ChapterSelect | `design/gdd/chapter-state-and-save.md` | ChapterStateSystem 查询接口、ChapterCompleted 事件 | Data dependency |
| InputBlocker 栈控制 3D 输入启停 | `design/gdd/input-system.md` | InputBlocker push/pop 机制 | State trigger |
| 自动保存事件驱动 SaveIndicator | `design/gdd/chapter-state-and-save.md` | AutoSaveTriggered 事件 | State trigger |
| 记忆碎片内容由 Narrative 提供 | `design/gdd/narrative-event-system.md` | NarrativeFragmentReady 事件 | Data dependency |
| HintButton 行为由 Hint System 控制 | `design/gdd/hint-system.md` | HintAvailabilityChanged / PlayerStuckDetected 事件 | Data dependency |
| UI 色彩和动效规范来自 Art Bible | `design/art/art-bible.md` | UI Art Standards 章节、章节色彩弧线 | Rule dependency |
| Settings 偏好存储由 Save System 管理 | `design/gdd/chapter-state-and-save.md` | SaveSystem.SaveSettings() 接口 | Ownership handoff |

## Acceptance Criteria

### 核心功能

- [ ] GameHUD 在谜题场景中正常显示，所有子 Widget 位于 Safe Area 内
- [ ] PauseMenu 可通过 PauseButton 正确打开/关闭，Time.timeScale 正确切换
- [ ] HintButton 在玩家 30 秒不操作后 opacity 从 30% 升至 80%，恢复操作后回落
- [ ] PuzzleCompletePanel 在 PerfectMatch 吸附动画后正确弹出，2.5 秒后自动关闭
- [ ] MemoryFragmentPanel 正确展示文本/图片内容，打字机效果按配置节奏逐字显示
- [ ] ChapterTransition 正确执行四阶段过渡，场景加载期间标题持续显示
- [ ] MainMenu 根据存档状态正确显示/隐藏"继续游戏"和"章节选择"
- [ ] ChapterSelect 正确展示 5 章状态，Locked 章节不可进入

### 层级与交互

- [ ] Popup 窗口打开时 3D 场景输入完全阻断（InputBlocker 生效）
- [ ] HUD 层级按钮不阻断 3D 场景的触摸交互（除按钮自身区域外）
- [ ] 多个 Popup 叠加时，InputBlocker 栈正确管理（全部关闭后才恢复输入）
- [ ] 快速连续弹窗（PuzzleComplete → MemoryFragment）通过队列依次展示

### 性能与适配

- [ ] 所有 UI 动画在 60fps 设备上无掉帧
- [ ] 高斯模糊效果在 iPhone 13 Mini 上 < 2ms/帧（或低端设备自动降级）
- [ ] UI Prefab 总内存 < 5MB（压缩后）
- [ ] Safe Area 在 iPhone 15 Pro（灵动岛）和 iPhone SE（无刘海）上均正确适配
- [ ] 所有可点击区域最小 44×44pt
- [ ] 字号切换（标准/大/超大）后所有面板布局不溢出

### 无硬编码

- [ ] 所有动画时长、opacity 值、颜色值通过配置加载（Luban 或 ScriptableObject）
- [ ] 所有文本通过本地化键引用，无硬编码字符串
- [ ] 章节过渡色彩跟随 Art Bible 章节色彩弧线配置，可独立调整

## Open Questions

| Question | Owner | Deadline | Resolution |
|----------|-------|----------|-----------|
| PauseMenu 的高斯模糊使用 URP Depth of Field 还是自定义 Blur Shader？前者消耗 Post-Processing Pass，后者需额外开发 | TA / Tech Lead | MVP 原型阶段 | 建议原型先用 CanvasGroup + 半透明黑遮罩（零开发成本），Vertical Slice 升级为 Blur |
| MemoryFragmentPanel 的"手写体字体"是否需要定制？还是使用现有字体的 Italic/Light 变体？ | Art Director | Vertical Slice | MVP 使用思源黑体 Light + 斜体模拟，后续评估定制字体需求 |
| 章节过渡期间的加载是否需要异步 Addressable 预加载（在 Phase 1 就开始加载）？ | Tech Lead | MVP | 建议 Phase 1 开始加载，Phase 2 标题显示期间完成——利用过渡动画遮掩加载时间 |
| "记忆碎片库"（已收集碎片的重新查看入口）放在哪？MainMenu？ChapterSelect？独立面板？ | Game Design | Vertical Slice | 暂不纳入 MVP，Vertical Slice 阶段决定入口位置 |
| 是否需要 FPS/Debug Overlay（开发期显示帧率、drawcalls）？如果需要，使用独立的 UIWindow 还是 IMGUI？ | Tech Lead | MVP | 建议使用独立 UIWindow 放在 System 层级，仅 Development Build 可见 |
| ChapterSelect 是否支持从"当前活跃章节"直接进入（快捷入口），还是只能从 MainMenu 访问？ | Game Design | Vertical Slice | 暂定仅 MainMenu，后续根据测试反馈决定是否增加快捷入口 |
| Android 返回键（Back Button）的行为映射：优先关闭最顶层 Popup？还是直接打开/关闭 PauseMenu？ | Game Design / Tech Lead | MVP | 建议：有 Popup 则关闭 Popup，无 Popup 则打开 PauseMenu，MainMenu 状态下弹出退出确认 |
