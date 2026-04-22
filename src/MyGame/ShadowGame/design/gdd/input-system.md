<!-- 该文件由Cursor 自动生成 -->
# Input System — 输入系统

> **Status**: Draft
> **Author**: System Design Agent
> **Last Updated**: 2026-04-16
> **Last Verified**: 2026-04-16
> **Implements Pillar**: 日常即重量（操作手感是情感体验的基础）

## Summary

输入系统是《影子回忆》的基础设施层，负责将触屏手势和鼠标/键盘操作抽象为统一的交互语义（点击、拖拽、旋转、缩放）。上层系统（Object Interaction、Shadow Puzzle）通过订阅输入事件获取操作数据，无需关心底层输入设备差异。这个系统的设计目标是"让玩家完全忘记它的存在"。

> **Quick reference** — Layer: `Foundation` · Priority: `MVP` · Key deps: `None`

## Overview

玩家在触屏上用手指操作日常物件和光源——单指点击选中，拖拽移动位置，双指旋转角度。输入系统将这些原始触摸数据转化为结构化的手势事件，发送给上层交互系统。系统同时处理手势冲突（一次拖拽是"移动物件"还是"只是点了一下"？两根手指是在"旋转"还是"缩放"？），确保手势识别准确且响应即时。PC 端通过鼠标和键盘映射实现相同操作，开发阶段可直接在 Editor 中调试。

## Player Fantasy

玩家不感知这个系统的存在——操作跟手、自然、无摩擦。

## Detailed Design

### Core Rules

**输入抽象层架构：**

1. 系统采用三层架构：
   - **Raw Input Layer**：直接读取 Unity InputSystem 的触摸/鼠标/键盘数据
   - **Gesture Recognition Layer**：将原始输入识别为手势（Tap / Drag / Rotate / Pinch）
   - **Event Dispatch Layer**：通过 GameEvent 将识别结果广播给上层系统
2. 上层系统只与 Event Dispatch Layer 交互，不直接访问 UnityEngine.Input 或 InputSystem API
3. 所有手势识别在同一帧内完成，不跨帧缓冲（保证 16ms 响应）

**支持的手势：**

1. **Tap（单指点击）**：手指按下后在阈值距离和时间内抬起，输出屏幕坐标
2. **Drag（单指拖拽）**：手指按下并移动超过阈值距离，输出起始位置、当前位置、位移增量（delta）
3. **Rotate（双指旋转）**：两指同时接触并产生角度变化超过阈值，输出旋转角度增量
4. **Pinch（双指缩放）**：两指同时接触并产生间距变化超过阈值，输出缩放比例增量
5. **LightDrag（光源拖拽）**：语义与 Drag 相同，由上层根据当前选中对象类型决定映射为物件拖拽还是光源轨道移动

**手势生命周期：**

每个手势经历三个阶段，每个阶段触发对应事件：
- **Began**：手势识别成功的第一帧
- **Updated**：手势持续中的每一帧
- **Ended**：手指抬起或手势被取消

**PC 端输入映射：**

| 触屏手势 | PC 映射 | 说明 |
|---------|--------|------|
| Tap | 鼠标左键单击 | 点击位置 = 鼠标光标位置 |
| Drag | 鼠标左键按住拖拽 | 拖拽 delta = 鼠标移动量 |
| Rotate | 鼠标右键按住拖拽（水平方向） / 鼠标中键按住拖拽 | 水平拖拽量映射为旋转角度 |
| Pinch | 鼠标滚轮滚动 | 滚轮量映射为缩放比例 |

**输入阻断规则（InputBlocker）：**

1. 当 UI 面板处于激活状态（暂停菜单、设置界面）时，阻断所有游戏手势输入
2. 当演出动画播放中（PerfectMatch 吸附、记忆重现）时，阻断所有游戏手势输入
3. 阻断判定在 Gesture Recognition Layer 之前执行，被阻断时不产生任何手势事件
4. 阻断状态通过 `InputBlocker` 栈管理——多个阻断源可叠加，全部释放后才恢复输入

**输入过滤规则（InputFilter）：**

1. Tutorial 系统可通过 `PushInputFilter(allowedGestures: GestureType[])` 设置白名单，只有白名单内的手势类型（Tap/Drag/Rotate/Pinch/LightDrag）可通过，其余手势事件被静默丢弃
2. 通过 `PopInputFilter()` 移除当前过滤器，恢复所有手势通过
3. 同一时刻只能有一个 InputFilter 激活——新的 `PushInputFilter` 覆盖旧 filter
4. 被 InputFilter 过滤的手势不产生任何事件，也不产生任何视觉/音频/触觉反馈

**InputBlocker 与 InputFilter 优先级链：**

| 优先级 | 条件 | 行为 |
|--------|------|------|
| 1（最高） | `InputBlocker` 栈非空 | **全量阻断**——所有手势被阻止，InputFilter 不生效 |
| 2 | `InputBlocker` 栈为空 + `InputFilter` 激活 | **白名单过滤**——仅允许 allowedGestures 内的手势通过 |
| 3（最低） | `InputBlocker` 栈为空 + 无 `InputFilter` | **正常通过**——所有手势事件正常分发 |

> **互操作场景示例**：Tutorial 激活 InputFilter（只允许 Drag）→ Narrative 演出触发 push InputBlocker → 全量阻断生效，InputFilter 暂时无效 → 演出结束 pop InputBlocker → InputFilter 恢复生效（只允许 Drag）→ 教学完成后 PopInputFilter → 正常通过。

### States and Transitions

**手势识别状态机（单指）：**

| State | Entry Condition | Exit Condition | Behavior |
|-------|----------------|----------------|----------|
| **Idle** | 无手指接触 / 手指抬起 | 检测到手指按下 | 不产生任何事件 |
| **Pending** | 手指按下 | 移动距离 > dragThreshold → Dragging；持续时间 > tapTimeout 且未移动 → LongPress；手指抬起且移动距离 < dragThreshold 且时间 < tapTimeout → 触发 Tap 后回到 Idle | 等待判定点击还是拖拽；此阶段不产生 Drag 事件 |
| **Dragging** | Pending 状态下移动超阈值 | 手指抬起 → Idle | 每帧触发 DragUpdated 事件，抬起时触发 DragEnded |
| **LongPress** | Pending 状态下超时未移动 | 手指抬起 → Idle；手指移动 > dragThreshold → Dragging | 触发 LongPressBegan 事件（预留，MVP 不使用） |

**手势识别状态机（双指）：**

| State | Entry Condition | Exit Condition | Behavior |
|-------|----------------|----------------|----------|
| **Idle** | 触摸点 < 2 | 检测到第二根手指按下 | 如果当前单指处于 Dragging，立即取消 Drag（发送 DragCancelled） |
| **Pending2** | 双指同时接触 | 角度变化 > rotateThreshold → Rotating；间距变化 > pinchThreshold → Pinching；一指抬起 → Idle | 等待判定旋转还是缩放 |
| **Rotating** | 角度变化超阈值 | 任一手指抬起 → Idle | 每帧触发 RotateUpdated，锁定缩放判定 |
| **Pinching** | 间距变化超阈值 | 任一手指抬起 → Idle | 每帧触发 PinchUpdated，锁定旋转判定 |

**双指手势互斥规则**：进入 Rotating 后锁定为旋转直到手指抬起，不会中途切换为 Pinching，反之亦然。这避免了旋转和缩放之间的抖动切换。

### Interactions with Other Systems

**与 Object Interaction System 的交互：**
- 输出：Tap 事件（含屏幕坐标，用于物件选中 Raycast）、Drag 事件（含位置和 delta，用于物件移动）、Rotate 事件（含角度 delta，用于物件旋转）、Pinch 事件（含缩放 delta，用于距离调整）
- Object Interaction 负责将屏幕坐标转为世界坐标、处理 Raycast 命中判定、执行格点吸附
- Input System 不知道当前选中了什么物件，也不知道物件是否移动成功

**与 Shadow Puzzle System 的交互：**
- 间接交互：Input System 不直接与 Puzzle 系统通信
- Puzzle 系统通过监听 Object Interaction 的物件移动事件获取数据

**与 UI System 的交互：**
- UI 系统在打开面板时向 `InputBlocker` 栈 push 阻断令牌
- 关闭面板时 pop 令牌，恢复游戏输入
- Input System 在每帧开始时检查 InputBlocker 栈是否为空

**与 Tutorial System 的交互：**
- 输出：Tutorial 可订阅任意手势事件，用于检测玩家是否完成了教学操作
- Tutorial 通过 `PushInputFilter(allowedGestures)` 设置白名单过滤器，限制只允许特定手势通过（例如教学阶段只允许 Drag，禁止 Rotate）
- 教学步骤完成后 Tutorial 调用 `PopInputFilter()` 移除过滤器
- InputFilter 与 InputBlocker 共存时遵循上方优先级链——InputBlocker 全量阻断优先级更高
- Tutorial 激活 InputFilter 期间如果 Narrative 演出 push InputBlocker，Tutorial 教学暂停（隐藏提示但不标记完成），演出结束后恢复

## Formulas

### Drag Threshold（拖拽判定阈值）

```
isDrag = (touchMovedDistance > dragThreshold)
```

触摸移动距离使用屏幕像素，并根据屏幕 DPI 归一化，确保不同设备上物理移动距离一致：

```
dragThreshold = baseDragThreshold_mm * screenDPI / 25.4
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| baseDragThreshold_mm | float | 2.0-5.0 mm | config | 物理距离阈值，手指移动超过此距离判定为拖拽 |
| screenDPI | float | 160-480 | runtime (Screen.dpi) | 当前设备屏幕 DPI |
| 25.4 | const | — | — | 毫米到英寸换算常数 |
| touchMovedDistance | float | 0-∞ px | runtime | 手指从按下点到当前位置的屏幕像素距离 |

**Expected output range**: iPhone 13 Mini (476 DPI) → ~37px; iPad (264 DPI) → ~21px; PC (96 DPI) → ~8px

**Edge case**: 若 `Screen.dpi` 返回 0（模拟器或异常设备），使用 fallbackDPI = 160。

### Tap Timeout（点击判定时间窗口）

```
isTap = (touchDuration < tapTimeout) && (touchMovedDistance < dragThreshold)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| tapTimeout | float | 0.15-0.4 s | config | 手指按下到抬起的最大时间，超时不判定为点击 |
| touchDuration | float | 0-∞ s | runtime | 手指按下到抬起经过的时间 |

**Expected output**: 正常点击 ~50-150ms，长按 > 300ms。
**Edge case**: 若设备帧率极低（< 20fps），单帧 delta 可能 > 50ms，tapTimeout 使用 unscaledDeltaTime 累加避免被 TimeScale 影响。

### Rotation Angle Delta（双指旋转角度计算）

```
angleDelta = atan2(cross(prevDir, currDir), dot(prevDir, currDir))

prevDir = normalize(prevTouch1 - prevTouch0)
currDir = normalize(currTouch1 - currTouch0)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| prevTouch0, prevTouch1 | Vector2 | screen space | runtime | 上一帧两个触摸点的屏幕位置 |
| currTouch0, currTouch1 | Vector2 | screen space | runtime | 当前帧两个触摸点的屏幕位置 |
| angleDelta | float | -π to π rad | calculated | 帧间旋转角度，正值为逆时针 |

**Expected output range**: 正常操作中每帧 |angleDelta| < 0.1 rad (~5.7°)

**Edge case**: 当两指间距 < minFingerDistance (20px) 时，方向向量不稳定，此时忽略旋转输入，防止抖动。

### Rotate Threshold（旋转识别阈值）

```
isRotate = abs(accumulatedAngle) > rotateThreshold
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| accumulatedAngle | float | 0-∞ rad | runtime | 从双指开始到当前累计的角度变化绝对值 |
| rotateThreshold | float | 5-15° | config | 累计旋转超过此角度才正式识别为旋转手势 |

### Pinch Scale Delta（双指缩放比例计算）

```
scaleDelta = currDistance / prevDistance

currDistance = length(currTouch1 - currTouch0)
prevDistance = length(prevTouch1 - prevTouch0)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| currDistance | float | > 0 px | runtime | 当前帧两指间屏幕距离 |
| prevDistance | float | > 0 px | runtime | 上一帧两指间屏幕距离 |
| scaleDelta | float | 0.8-1.2 per frame | calculated | 帧间缩放比例，> 1 为放大，< 1 为缩小 |

**Expected output range**: 正常操作中 0.95-1.05/帧

**Edge case**: 当 prevDistance < minFingerDistance (20px)，强制 scaleDelta = 1.0，防止除零或极端缩放。

### Pinch Threshold（缩放识别阈值）

```
isPinch = abs(accumulatedScale - 1.0) > pinchThreshold
accumulatedScale = currentDistance / initialDistance
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| initialDistance | float | > 0 px | runtime | 双指开始时的初始间距 |
| pinchThreshold | float | 0.05-0.15 | config | 缩放比例偏离 1.0 超过此值才识别为缩放手势 |

### PC Rotate Mapping（PC 端旋转映射）

```
angleDelta = mouseDeltaX * pcRotateSensitivity
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| mouseDeltaX | float | -∞ to ∞ px | runtime | 鼠标水平移动像素量 |
| pcRotateSensitivity | float | 0.002-0.01 rad/px | config | 鼠标移动到旋转角度的换算系数 |

### PC Pinch Mapping（PC 端缩放映射）

```
scaleDelta = 1.0 + scrollDelta * pcScrollSensitivity
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| scrollDelta | float | -3 to 3 | runtime | 鼠标滚轮滚动量（Unity 标准化值） |
| pcScrollSensitivity | float | 0.05-0.2 | config | 滚轮到缩放比例的换算系数 |

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|----------|------------------|-----------|
| 拖拽过程中第二根手指落下 | 取消当前 Drag（发送 DragCancelled），进入双指 Pending 状态 | 避免拖拽和旋转/缩放同时生效导致物件跳跃 |
| 双指旋转中一根手指抬起 | 立即结束旋转（发送 RotateEnded），剩余手指不自动进入拖拽 | 防止旋转结束瞬间意外触发拖拽 |
| 双指旋转中一根手指滑出屏幕 | 视为手指抬起，结束旋转 | 触摸丢失等同于释放 |
| 手指在 UI 按钮上按下后滑入游戏区域 | 不触发游戏手势，UI 保持事件捕获直到手指抬起 | UI 交互优先，避免误操作 |
| 在游戏区域按下后手指滑到 UI 区域 | 游戏手势继续跟踪直到手指抬起 | 手势一旦开始归属游戏，不中途转移 |
| 三指或更多手指同时触摸 | 只识别前两个触摸点，忽略后续触摸点 | 简化处理，游戏不需要三指手势 |
| 设备帧率极低（< 20fps）导致触摸位置跳跃 | Drag delta 使用帧间差值，单帧最大 delta 限制为 maxDeltaPerFrame | 防止低帧率时物件瞬移 |
| 快速连续 Tap（双击） | 两次独立的 Tap 事件，间隔 < doubleTapWindow 时附带 tapCount=2 | 预留双击语义，MVP 不使用但不丢弃数据 |
| 演出播放中玩家疯狂点击 | InputBlocker 栈生效，所有触摸被吞掉，不产生事件 | 防止演出中意外操作 |
| Screen.dpi 返回 0 | 使用 fallbackDPI = 160 | 兜底处理模拟器和异常设备 |
| 应用切到后台再切回 | 清除所有活跃触摸状态，回到 Idle | 防止残留触摸导致幽灵拖拽 |
| PC 端同时按住左键和右键 | 左键拖拽优先，右键旋转被忽略 | 避免同时拖拽+旋转的混乱操作 |

## Dependencies

| System | Direction | Nature of Dependency |
|--------|-----------|---------------------|
| Object Interaction System | OI depends on this | 消费 Tap/Drag/Rotate/Pinch 事件进行物件操作 |
| Shadow Puzzle System | SP indirectly depends on this | 通过 Object Interaction 间接获取输入 |
| UI System | Bidirectional | UI 通过 InputBlocker 控制游戏输入启停；Input 在 UI 层之下 |
| Tutorial System | Tutorial depends on this | 订阅手势事件检测教学进度；通过 InputFilter 限制可用手势 |
| Settings & Accessibility | Settings configures this | 振动开关全局标记（haptic_enabled）控制 Haptic 反馈启停 |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|-----------|--------------|------------|-------------------|-------------------|
| baseDragThreshold_mm | 3.0 mm | 2.0-5.0 mm | 需要更大移动才触发拖拽，减少误拖拽但降低灵敏度 | 更灵敏但容易把点击误判为拖拽 |
| tapTimeout | 0.25 s | 0.15-0.4 s | 允许更慢的点击，但拖拽响应延迟增加 | 拖拽响应更快，但快速点击可能被漏判 |
| rotateThreshold | 8° | 5-15° | 需要更明显旋转才触发，减少误触发 | 更灵敏但可能在放大手势中误判为旋转 |
| pinchThreshold | 0.08 | 0.05-0.15 | 需要更明显缩放才触发 | 更灵敏但可能在旋转手势中误判为缩放 |
| minFingerDistance | 20 px | 10-40 px | 更大的双指安全距离，减少极端角度抖动 | 允许更近的双指操作 |
| pcRotateSensitivity | 0.005 rad/px | 0.002-0.01 rad/px | 鼠标旋转更快 | 鼠标旋转更精细 |
| pcScrollSensitivity | 0.1 | 0.05-0.2 | 滚轮缩放更剧烈 | 滚轮缩放更精细 |
| maxDeltaPerFrame | 100 px | 50-200 px | 允许更大的帧间跳跃 | 限制更严，低帧率下物件移动更平滑但可能"追不上"手指 |
| fallbackDPI | 160 | 96-326 | 备用 DPI 越高，阈值越大 | 备用 DPI 越低，阈值越小 |

## Visual/Audio Requirements

| Event | Visual Feedback | Audio Feedback | Priority |
|-------|----------------|---------------|----------|
| Tap（命中物件） | 由 Object Interaction 层处理高亮 | 由 Object Interaction 层处理音效 | — |
| Drag 进行中 | 由 Object Interaction 层处理物件跟随 | 由 Object Interaction 层处理滑动音 | — |
| Rotate 进行中 | 由 Object Interaction 层处理旋转视觉 | 由 Object Interaction 层处理转动音 | — |
| 手势被 InputBlocker 阻断 | 无视觉反馈（静默吞掉） | 无音效 | MVP |

> Input System 作为基础设施层，自身不产生任何视觉或音频反馈。所有面向玩家的反馈由上层系统（Object Interaction、UI）负责。

## Game Feel

### Feel Reference

应该感觉像 **iOS 原生手势操作**（照片应用的双指缩放旋转）— 指哪到哪，零延迟，没有任何需要"学习"的操作方式。**不应该**感觉像手势快捷方式应用那样需要记忆特定手势组合。

辅助参考：**Monument Valley 的触摸操控** — 单指操作为主，简洁直觉，手指离开后物件立即静止（无惯性滑动）。

### Input Responsiveness

| Action | Max Input-to-Response Latency (ms) | Frame Budget (at 60fps) | Notes |
|--------|-----------------------------------|------------------------|-------|
| Tap 事件发出 | 0ms (手指抬起的同一帧) | 0 frames | Tap 判定在手指抬起帧立即完成 |
| Drag 首次事件 | 16ms | 1 frame | 手指移动超阈值的下一帧立即发出 DragBegan |
| Drag 持续更新 | 16ms | 1 frame | 每帧同步更新，不跳帧 |
| Rotate 首次事件 | 16ms | 1 frame | 角度超阈值的下一帧立即发出 RotateBegan |
| Pinch 首次事件 | 16ms | 1 frame | 缩放超阈值的下一帧立即发出 PinchBegan |
| InputBlocker 生效 | 0ms | 0 frames | push 阻断令牌的同一帧立即生效 |

### Animation Feel Targets

> Input System 不直接驱动动画。手势的视觉表现由 Object Interaction 层负责。此处不列出动画帧数据。

### Weight and Responsiveness Profile

- **Weight**: 无重量感。输入系统是透明的传递层，手势数据零滤波、零平滑地传递给上层。任何"重量感"都是 Object Interaction 层叠加的。
- **Player control**: 完全控制。原始输入数据 1:1 传递，不做任何预测或自动修正。
- **Snap quality**: 不涉及。格点吸附由 Object Interaction 层处理。Input 层输出纯连续数据。
- **Acceleration model**: 无加速度。delta 直接等于帧间位移差，不做加速度曲线处理。
- **Failure texture**: 不涉及。输入层没有"失败"概念，只有"识别"或"未识别"。手势冲突解决对玩家透明。

### Feel Acceptance Criteria

- [ ] 物件拖拽与手指位置同步——肉眼看不出任何延迟
- [ ] 双指旋转感觉自然——测试者无需教学即可直觉操作
- [ ] 点击与拖拽不会互相误判——快速点击不触发拖拽，缓慢拖拽不触发点击
- [ ] 双指旋转与双指缩放不会互相混淆——旋转时不会突然缩放，反之亦然
- [ ] 从 UI 操作切回游戏操作时无任何卡顿或延迟
- [ ] PC 端鼠标操作感觉与触屏同等自然，不像"模拟触屏"

## UI Requirements

| Information | Display Location | Update Frequency | Condition |
|-------------|-----------------|-----------------|-----------|
| 操作方式提示（拖拽/旋转图标） | 屏幕下方中央 | 首次使用该手势时显示 | 由 Tutorial System 控制显示时机和内容；Input System 只提供"该手势是否曾被使用"标记 |

> Input System 自身不管理任何 UI 元素。操作提示 UI 由 Tutorial / UI System 根据 Input System 的手势使用统计数据决定。

## Cross-References

| This Document References | Target GDD | Specific Element Referenced | Nature |
|--------------------------|-----------|----------------------------|--------|
| Drag/Rotate/Pinch 事件驱动物件操作 | `design/gdd/object-interaction.md` | 物件选中/移动/旋转接口 | Data dependency |
| InputBlocker 由 UI 系统控制 | `design/gdd/ui-system.md` | UI 面板激活状态 | State trigger |
| 手势使用统计供教程判断 | `design/gdd/tutorial-onboarding.md` | 教学步骤完成条件 | Data dependency |
| 物件拖拽响应延迟要求 (16ms) | `design/gdd/shadow-puzzle-system.md` | Input Responsiveness 表 — 物件拖拽 16ms | Rule dependency |
| 物件选中响应延迟要求 (50ms) | `design/gdd/shadow-puzzle-system.md` | Input Responsiveness 表 — 物件选中 50ms | Rule dependency |

## Acceptance Criteria

- [ ] Touch 手势识别（Tap/Drag/Rotate/Pinch）在 iPhone 13 Mini 上准确运行
- [ ] 快速连续 Tap（间隔 > 100ms）100% 被正确识别，不误判为 Drag
- [ ] 缓慢拖拽（速度 < 10px/s 但距离 > 阈值）100% 被正确识别为 Drag，不误判为 Tap
- [ ] 双指旋转和双指缩放在真机上互不干扰——10 次连续旋转操作中不出现缩放误判
- [ ] Drag 事件从手指移动超阈值到事件发出 ≤ 1 帧（16ms at 60fps）
- [ ] InputBlocker 生效时 0 个手势事件泄漏到游戏层
- [ ] PC 端鼠标左键拖拽、右键旋转、滚轮缩放均可正常触发对应手势事件
- [ ] 应用切后台再切回后，无残留触摸导致的幽灵拖拽
- [ ] Screen.dpi 为 0 时系统正常运行（fallback 生效）
- [ ] Performance: 手势识别逻辑每帧 < 0.5ms（Profiler 验证）
- [ ] 所有阈值参数通过配置文件加载，无硬编码

## Open Questions

| Question | Owner | Deadline | Resolution |
|----------|-------|----------|-----------|
| 是否需要支持长按手势（LongPress）？目前 MVP 无使用场景，但状态机已预留 | Game Design | MVP 原型阶段 | 待确认是否有长按选中+弹出菜单的需求 |
| 小屏幕（iPhone 13 Mini）上手指遮挡问题的应对方案由哪层负责？偏移显示逻辑放在 Input 层还是 Object Interaction 层？ | Tech Lead | MVP 原型阶段 | 建议 Object Interaction 层处理，Input 层只输出原始触点位置 |
| 是否使用 Unity 的 New Input System (com.unity.inputsystem) 还是旧版 UnityEngine.Input？ | Tech Lead | 实现前确认 | 建议 New Input System——支持更好的多点触控和设备抽象，但需确认与 TEngine 无冲突 |
| 双指手势的 rotateThreshold 和 pinchThreshold 最优值需要真机调参 | QA / Game Design | MVP 真机测试阶段 | 当前值基于经验估算，需 5+ 人真机测试后确定 |
| Pinch 手势是否纳入 MVP？Shadow Puzzle GDD 中物件距离调整为"可选" | Game Design | MVP scope 确认 | 建议 MVP 包含 Pinch 识别但标记为可配置关闭，降低后续接入成本 |
