<!-- 该文件由Cursor 自动生成 -->
# Cross-GDD Review Report — 影子回忆 MVP 系统

> **审查日期**: 2026-04-16
> **审查范围**: 9 份 MVP 系统 GDD
> **审查维度**: 事件契约 / 接口一致性 / 性能预算 / TEngine 规范 / 依赖关系

---

## 审查结论

| 维度 | 结果 | 关键问题数 |
|------|------|-----------|
| GameEvent 事件名对齐 | ⚠️ 存在不一致 | 5 |
| 接口/数据结构/枚举一致性 | ⚠️ 存在冲突 | 4 |
| 性能预算 | ✅ 基本一致 | 1 |
| TEngine 模块使用规范 | ✅ 合规 | 0 |
| 依赖关系 vs Systems Map | ⚠️ 小偏差 | 2 |

**总评**: 各 GDD 质量较高，核心设计思路一致，但由于 7 个 Agent 并行编写，存在 **事件命名不统一** 和 **部分接口定义细节冲突** 需要统一。下文逐项列出问题及修复建议。

---

## 1. GameEvent 事件名对齐审查

### 1.1 谜题状态变更事件 — 命名不一致

| GDD | 使用的事件名 | 描述 |
|-----|------------|------|
| Shadow Puzzle | `PuzzleCompleteEvent(puzzleId, chapterId)` | 谜题完成 |
| Chapter State | `PuzzleStateChangeRequest` / `PuzzleStateChanged` | 状态变更请求/确认 |
| Hint System | `PuzzleStateChangedEvent` | 监听谜题状态 |
| UI System | `PuzzleStateChanged` (无 Event 后缀) | 驱动 UI 变化 |

**问题**: Shadow Puzzle 用 `PuzzleCompleteEvent`，Chapter State 用 `PuzzleStateChanged`，Hint System 加了 `Event` 后缀。三处命名不统一。

**✅ 统一建议**:
- 请求: `PuzzleStateChangeRequest`（Shadow Puzzle → Chapter State）
- 确认/广播: `PuzzleStateChanged`（Chapter State → 全体，无 Event 后缀）
- 完成专用: `PuzzleCompleted`（Chapter State 广播，当状态变为 Complete 时）
- 章节完成: `ChapterCompleted`（Chapter State 广播）

### 1.2 物件操作事件 — 命名和载荷不一致

| GDD | 事件名 | 载荷 |
|-----|-------|------|
| Object Interaction | `ObjectTransformChanged(objectId, position, rotation)` | 位置+旋转 |
| Hint System | `ObjectOperatedEvent` | 未明确载荷 |
| UI System | `ObjectSelected` / `ObjectReleased` | 选中/释放 |

**问题**: Object Interaction 定义 `ObjectTransformChanged`，但 Hint System 监听的是 `ObjectOperatedEvent`，名字不匹配。

**✅ 统一建议**:
- `ObjectSelected(objectId)` — 物件选中
- `ObjectReleased(objectId)` — 物件释放
- `ObjectTransformChanged(objectId, position, rotation)` — 物件变换
- Hint System 监听 `ObjectTransformChanged` 即可，移除 `ObjectOperatedEvent`

### 1.3 光源事件 — 缺失统一定义

| GDD | 事件名 |
|-----|-------|
| Object Interaction | `LightPositionChanged(lightId, trackParameter)` |
| Scene Management | 未提及光源事件 |
| URP Shadow Rendering | 未提及光源事件 |

**问题**: 光源位置变更事件只在 Object Interaction 中定义，URP Shadow Rendering 需要响应此事件但未提及。

**✅ 修复**: URP Shadow Rendering 的"光源移动时阴影必须同帧或下一帧更新"规则应明确说明监听 `LightPositionChanged` 事件。

### 1.4 场景事件 — 基本一致但有细微差异

| GDD | 事件名 |
|-----|-------|
| Scene Management | `RequestSceneChange` / `SceneTransitionBegin` / `SceneUnloadBegin` / `SceneLoadComplete` / `SceneReady` / `SceneTransitionEnd` |
| Chapter State | `SceneLoaded` (监听) |
| UI System | 未提及 `SceneTransitionBegin` |

**问题**: Chapter State 使用 `SceneLoaded`，但 Scene Management 发送的是 `SceneReady`（含义更准确但名字不一致）。

**✅ 统一建议**: 使用 `SceneReady`，Chapter State 中的 `SceneLoaded` 改为 `SceneReady`。

### 1.5 Hint 系统事件 — 双方命名略有出入

| GDD | 事件名 | 方向 |
|-----|-------|------|
| Hint System | `HintTriggeredEvent` / `HintConsumedEvent` / `HintButtonStateEvent` | Hint → 外部 |
| UI System | `HintRequested` / `HintAvailabilityChanged` / `PlayerStuckDetected` | UI ↔ Hint |

**问题**: Hint 发送 `HintButtonStateEvent`，UI 监听 `HintAvailabilityChanged`，名字不同但含义相同。

**✅ 统一建议**: 统一为 `HintAvailabilityChanged(tier, remainingCount)`。移除 `HintButtonStateEvent`。`PlayerStuckDetected` 由 Hint System 发送，UI 监听（名字可保留）。

---

## 2. 接口/数据结构/枚举一致性审查

### 2.1 谜题状态枚举 — NearMatch 阈值不一致

| GDD | NearMatch 进入 | NearMatch 退出(滞后) | PerfectMatch |
|-----|---------------|---------------------|-------------|
| Shadow Puzzle | 40% | 35% (hysteresis 5%) | 85% |
| Chapter State | 40% | 35% | 85% |
| Hint System | 引用 NearMatch = 0.40 | — | — |
| Luban TbPuzzle | `nearMatchThreshold` 可覆盖 (0.30-0.55) | — | `perfectMatchThreshold` 可覆盖 (0.75-0.95) |

**结论**: ✅ 基本一致。Luban 的覆盖范围兼容默认值。

### 2.2 PuzzleStateEnum — 定义位置和值一致性

| GDD | 定义 |
|-----|------|
| Chapter State | `Locked=0, Idle=1, Active=2, NearMatch=3, PerfectMatch=4, Complete=5` |
| Shadow Puzzle | Locked / Idle / Active / NearMatch / PerfectMatch / Complete (无数值) |

**结论**: ✅ Chapter State 给出了明确的枚举定义，Shadow Puzzle 描述一致。

### 2.3 InputBlocker 接口 — 实现细节不一致

| GDD | 描述 |
|-----|------|
| Input System | `InputBlocker` 栈管理，push/pop 令牌 |
| UI System | `InputBlocker.Push(token)` / `InputBlocker.Pop(token)` |
| Object Interaction | 未提及 InputBlocker，说"当 UI 遮罩层激活，本系统暂停所有交互" |

**问题**: Object Interaction 未明确说明通过 InputBlocker 机制暂停。应该是 Input System 的 InputBlocker 阻断手势事件后，Object Interaction 自然收不到事件。

**✅ 修复**: Object Interaction GDD 中应明确："UI 遮罩层激活时的暂停通过 Input System 的 InputBlocker 实现，本系统无需额外处理——当手势事件被阻断时，Object Interaction 自然无法接收到操作。"

### 2.4 Shadow Puzzle 的 Layer 标注 — 与 Systems Map 不一致

| 文档 | Shadow Puzzle Layer |
|------|-------------------|
| Systems Map | Gameplay (Feature) |
| Shadow Puzzle GDD Quick Reference | `Core` |

**问题**: Shadow Puzzle 自称 Layer: `Core`，但 Systems Map 将其归类为 `Gameplay` (Feature Layer)。

**✅ 修复**: Shadow Puzzle GDD 的 Quick Reference 应改为 `Feature`，因为它依赖 Foundation 层（Input + URP）和 Core 层（Chapter State），本身是 Feature 层系统。

---

## 3. 性能预算审查

### 3.1 帧时间预算分配 — 总和验证

| 系统 | 每帧预算 | 来源 |
|------|---------|------|
| Input System 手势识别 | < 0.5ms | Input GDD |
| Object Interaction Update | < 1ms | OI GDD |
| Shadow RT 采样 + CPU 处理 | < 1.5ms | URP GDD |
| Shadow Match 匹配计算 | < 0.5ms (含在 2ms 总预算内) | Shadow Puzzle + URP |
| Hint System 更新 | < 0.5ms | Hint GDD |
| UI 动画/布局 | 未明确 | — |
| URP 渲染 (含 Shadow Map) | ~10-12ms | 隐含 |

**总计约 14-16ms**（60fps = 16.67ms）

**结论**: ⚠️ 预算略紧。建议为 UI 系统明确 < 1ms 的帧预算，留约 1.5ms 安全余量。

### 3.2 Draw Call 预算 — 一致

| 来源 | 目标 |
|------|------|
| URP GDD | ≤ 131 (Medium 档) / 150 硬上限 |
| Shadow Puzzle | < 150 draw calls |
| Art Bible | < 150 draw calls (mobile) |
| Technical Preferences | < 150 draw calls |

**结论**: ✅ 全局一致。

### 3.3 Shadow 采样 + 匹配计算预算

| 来源 | 预算 |
|------|------|
| URP GDD | Shadow RT 采样 ≤ 1.5ms，总计 ≤ 2ms |
| Shadow Puzzle | 匹配判定每帧 < 2ms |

**问题**: URP 说"采样 1.5ms + 匹配 0.5ms = 2ms"，Shadow Puzzle 说"匹配判定 < 2ms"——后者似乎将采样时间也包含在内。

**✅ 澄清**: 两者指的是同一个预算，不冲突。建议统一表述为"Shadow RT 采样 + 匹配度计算合计 ≤ 2ms/帧"。

---

## 4. TEngine 模块使用规范审查

| 检查项 | 结果 | 说明 |
|--------|------|------|
| 异步优先 (UniTask) | ✅ | 所有 GDD 明确使用 async UniTask，无 Coroutine |
| GameModule.XXX 访问 | ✅ | Scene Management 用 `GameModule.Resource`，Hint 用 `GameModule.Timer` |
| 资源释放 | ✅ | Scene Management 明确 `UnloadUnusedAssets()` + `GC.Collect()` |
| GameEvent 解耦 | ✅ | 模块间通信统一走 GameEvent |
| UIWindow / UIWidget | ✅ | UI GDD 正确继承 UIWindow/UIWidget |
| 热更边界 | ✅ | Scene Management 正确区分 BootScene(非热更) vs 其他(热更) |

**结论**: ✅ 全部合规，无违规使用。

---

## 5. 依赖关系 vs Systems Map 审查

### 5.1 Shadow Puzzle 缺少对 Object Interaction 的显式依赖

| 文档 | 依赖列表 |
|------|---------|
| Systems Map | Input System, Chapter State System, URP Shadow Rendering |
| Shadow Puzzle GDD | Chapter State, Hint, Narrative, Save, Input, URP Rendering |

**问题**: Systems Map 中 Shadow Puzzle 未列出对 Object Interaction 的依赖，但实际上 Shadow Puzzle 通过 Object Interaction 接收物件移动事件（`ObjectTransformChanged`）。

**✅ 修复**: Systems Map 的 Shadow Puzzle 依赖应补充 `Object Interaction System`。

### 5.2 Save System 被 URP Shadow Rendering 遗漏

URP 的 Settings/Accessibility 交互提到"接收质量档位切换请求"，而档位持久化需要 Save System。但 URP GDD 的 Dependencies 表中未列出 Save System。

**✅ 修复**: 这是间接依赖（通过 Settings），不需要修改 URP GDD——Settings 系统负责持久化，URP 只接收运行时配置变更。无需修复。

---

## 6. 其他发现

### 6.1 Cross-Reference 路径不一致

| GDD | 引用路径 | 实际路径 |
|-----|---------|---------|
| URP Shadow Rendering | `design/gdd/chapter-state.md` | `design/gdd/chapter-state-and-save.md` |
| Shadow Puzzle | `design/gdd/chapter-state.md` / `design/gdd/save-system.md` | `design/gdd/chapter-state-and-save.md` |
| Hint System | `design/gdd/shadow-puzzle-system.md` | ✅ 正确 |

**问题**: URP 和 Shadow Puzzle 的 Cross-Reference 中仍使用旧路径 `chapter-state.md` 和 `save-system.md`，实际文件已合并为 `chapter-state-and-save.md`。

**✅ 修复**: 更新 URP 和 Shadow Puzzle GDD 中的 Cross-Reference 路径。

### 6.2 HintButton 位置描述冲突

| GDD | HintButton 位置 |
|-----|----------------|
| Hint System | 屏幕右上角 |
| UI System | 屏幕右下偏上 (GameHUD 内 RightPanel) |

**问题**: Hint GDD 说"右上角"，UI GDD 说"右下偏上"。

**✅ 修复**: 以 UI GDD 为准（它有更详细的布局定义），Hint GDD 中修改为"右下偏上"。

### 6.3 Hint Layer 3 触发方式描述差异

| GDD | 描述 |
|-----|------|
| Hint System | "玩家主动点击提示按钮后触发"，按钮"前 60 秒内呈半透明低调状态" |
| UI System | HintButton "默认半透明 30%，30 秒无操作时升至 80%" |

**问题**: Hint GDD 说按钮 60s 后高亮，UI GDD 说 30s 后开始渐变。

**✅ 修复**: 统一为 UI GDD 的行为（30s 开始渐变到 80%），因为 Hint GDD 中的 60s 是"提示按钮完全高亮可用"的时间，可理解为 30s 开始渐变 + 10s 渐变完成 ≈ 40s 完全高亮。建议统一数值：`rampStart = 30s, rampDuration = 10s`（即 40s 完全高亮），Hint GDD 相应修改。

---

## 7. 统一事件名契约表（建议标准）

| 事件名 | 发送者 | 接收者 | Payload |
|--------|-------|-------|---------|
| `PuzzleStateChangeRequest` | Shadow Puzzle | Chapter State | `{ puzzleId, targetState }` |
| `PuzzleStateChanged` | Chapter State | All | `{ puzzleId, chapterId, oldState, newState }` |
| `PuzzleCompleted` | Chapter State | Narrative, UI, Analytics | `{ puzzleId, chapterId }` |
| `ChapterCompleted` | Chapter State | Narrative, UI, Scene, Analytics | `{ chapterId }` |
| `ChapterOutroFinished` | Narrative | Chapter State | `{ chapterId }` |
| `ObjectSelected` | Object Interaction | UI, Hint | `{ objectId }` |
| `ObjectReleased` | Object Interaction | UI | `{ objectId }` |
| `ObjectTransformChanged` | Object Interaction | Shadow Puzzle, Hint | `{ objectId, position, rotation }` |
| `LightPositionChanged` | Object Interaction | Shadow Puzzle, URP Rendering | `{ lightId, trackParameter }` |
| `PuzzleLockEvent` | Shadow Puzzle | Object Interaction | `{ objectId }` |
| `PuzzleLockAllEvent` | Shadow Puzzle | Object Interaction | `{}` |
| `PuzzleSnapToTargetEvent` | Shadow Puzzle | Object Interaction | `{ objectId, targetTransform }` |
| `MatchScoreChanged` | Shadow Puzzle | Hint, (UI indirect) | `{ puzzleId, score }` |
| `HintTriggered` | Hint System | Analytics | `{ hintLayer, targetObjectId }` |
| `HintAvailabilityChanged` | Hint System | UI | `{ tier, remainingCount }` |
| `HintRequested` | UI | Hint System | `{}` |
| `PlayerStuckDetected` | Hint System | UI | `{}` |
| `RequestSceneChange` | Chapter State | Scene Manager | `{ targetChapterId }` |
| `SceneTransitionBegin` | Scene Manager | UI, Audio | `{ fromChapterId, toChapterId }` |
| `SceneUnloadBegin` | Scene Manager | Shadow Puzzle, Narrative | `{ chapterId }` |
| `SceneLoadProgress` | Scene Manager | UI | `{ progress }` |
| `SceneLoadComplete` | Scene Manager | Shadow Puzzle, Audio, Chapter State | `{ chapterId, bgmAsset }` |
| `SceneReady` | Scene Manager | Chapter State | `{ chapterId }` |
| `SceneTransitionEnd` | Scene Manager | All | `{ chapterId }` |
| `SceneLoadFailed` | Scene Manager | UI | `{ chapterId, error }` |
| `NarrativeFragmentReady` | Narrative | UI | `{ fragmentId }` |
| `AutoSaveTriggered` | Save System | UI | `{}` |

---

## 8. 需修复的关键项清单

| # | 严重度 | 文件 | 修复内容 | 状态 |
|---|--------|------|---------|------|
| 1 | 🔴 High | shadow-puzzle-system.md | Quick Reference Layer 从 `Core` 改为 `Feature` | ✅ 已修复 |
| 2 | 🔴 High | shadow-puzzle-system.md | Cross-Reference 路径 `chapter-state.md` / `save-system.md` → `chapter-state-and-save.md` | ✅ 已修复 |
| 3 | 🔴 High | urp-shadow-rendering.md | Cross-Reference 路径 `chapter-state.md` → `chapter-state-and-save.md` | ✅ 已修复 |
| 4 | 🟡 Medium | hint-system.md | HintButton 位置 "右上角" → "右下偏上（GameHUD 内 RightPanel）" | ✅ 已修复 |
| 5 | 🟡 Medium | hint-system.md | 提示按钮高亮时间从 60s → 与 UI GDD 对齐 (rampStart=30s) | ✅ 已修复 |
| 6 | 🟡 Medium | systems-index.md | Shadow Puzzle 依赖补充 `Object Interaction System` | ✅ 已修复 |
| 7 | 🟢 Low | 各 GDD | 事件名统一为本报告第 7 节契约表（实现阶段统一即可） | 📋 实现阶段处理 |

---

## 9. 结论与建议

1. **核心设计高度一致** — 9 份 GDD 在游戏柱、手感目标、状态机设计、性能预算方面达成了良好共识
2. **事件名需统一** — 第 7 节的事件契约表应作为实现阶段的权威参考，写入代码规范
3. **Critical Path 验证** — 以下路径涉及 4+ 个系统的协作，建议优先在原型中端到端验证：
   - **物件拖拽 → 影子更新 → 匹配判定 → NearMatch 反馈**: Input → OI → Shadow Puzzle + URP → UI
   - **PerfectMatch → 物件锁定 → 吸附 → 完成 → 下一谜题**: Shadow Puzzle → OI → Chapter State → Scene/UI
4. **修复上述 6 个 High/Medium 问题后，MVP GDD 即可进入 Approved 状态**
