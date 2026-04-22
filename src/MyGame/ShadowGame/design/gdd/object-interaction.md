<!-- 该文件由Cursor 自动生成 -->
# Object Interaction System — 物件交互系统

> **Status**: Draft
> **Author**: Interaction Design Agent
> **Last Updated**: 2026-04-16
> **Last Verified**: 2026-04-16
> **Implements Pillar**: 日常即重量

## Summary

物件交互系统是《影子回忆》中玩家与场景物件进行物理交互的核心层。它处理物件的选中、拖拽、旋转、距离调整和格点吸附，以及光源沿预设轨道的移动。系统的首要设计目标不是"精确操控"，而是让每一次触碰都传达出"这些普通物件承载着生活的重量"——这是"日常即重量"设计柱的直接体现。

> **Quick reference** — Layer: `Core` · Priority: `MVP` · Key deps: `Input System`

## Overview

玩家在温暖的室内场景中，通过触屏操控散落的生活物件（台灯、杯子、椅子、雨伞等）。手指触碰物件即选中，拖拽可移动位置，双指操作可旋转角度，沿光源方向前后滑动可调整物件与光源的距离（改变投影大小）。所有移动基于离散格点吸附，物件会以柔和的方式"吸入"最近的格点而非硬切跳转。光源可沿设计师预设的弧线或直线轨道移动（第二章起开放）。系统不产生任何判定或评分——它只负责将玩家的触碰意图转化为有重量感的物件运动，具体的影子匹配判定由 Shadow Puzzle System 处理。

## Player Fantasy

"我在整理一个人离开后留下的物品。"

每一件物件都不重，但都承载着什么。拿起一只杯子时，它不是飘起来的——它有陶瓷的重量、桌面的摩擦、放下时的轻响。这不是"操作一个 3D 物体"的工具感，而是"我的手指就是我的手，我正在触碰真实的东西"的身体感。当物件被放在格点上轻柔地吸入正确位置时，那一刻的"咔"不是机械的精确，而是"对，它就应该在这里"的安心感。

这种交互必须是温柔的、不焦虑的——没有时间压力，没有误操作惩罚，没有手忙脚乱。小屏幕上的大拇指也应该感到从容。

## Detailed Design

### Core Rules

**物件选中：**

1. 玩家单指触摸可交互物件的碰撞体即选中该物件
2. 选中判定使用从触点发出的 Raycast，命中层级为 `InteractableObject`
3. 同一时刻只能选中一个物件（单选模式）
4. 选中后物件播放放大动画（EaseOutBack，8 frames），轮廓高亮激活
5. 选中物件的渲染排序提升至最前（避免被其他物件遮挡）
6. 触摸场景空白处或另一个物件时，当前选中物件取消选中
7. 选中判定的触摸区域比物件碰撞体实际范围外扩 8dp（移动端胖手指补偿）

**物件拖拽：**

1. 选中后手指不抬起直接滑动即进入拖拽状态（无最小滑动阈值，零延迟）
2. 物件跟随手指精确移动，**无惯性、无延迟、无加速曲线**
3. 手指位置通过 Raycast 投射到物件所在平面，物件中心跟随投射点
4. 拖拽过程中保持固定的手指-物件偏移量（即手指不会跳到物件中心）
5. 手指抬起即停止移动——物件立即停在当前位置，无惯性滑动
6. 拖拽期间持续触发格点吸附计算（见 Snapping 章节）
7. 拖拽响应延迟 ≤ 16ms（1 frame at 60fps）

**物件旋转：**

1. 双指触碰选中物件时进入旋转模式
2. 默认绕物件自身 Y 轴旋转，旋转角度跟随双指旋转手势
3. 部分物件（由配置表标记）支持 X 轴倾斜——通过双指上下滑动触发
4. 旋转同样基于格点吸附，步进角度为 15°（可配置）
5. 旋转格点吸附使用与位置吸附相同的柔和插值
6. 双指松开后旋转立即停止，无旋转惯性

**距离调整：**

1. 选中物件后，单指沿光源方向（屏幕上下）滑动可调整物件距离
2. 距离调整沿物件-光源连线方向进行，改变物件与光源的间距
3. 靠近光源 → 投影变大；远离光源 → 投影变小
4. 距离同样受格点吸附约束（步进 0.25 units）
5. 距离有最小值和最大值限制，防止物件与光源重叠或离开场景
6. 拖拽与距离调整互斥——系统通过手指滑动方向的主分量判断当前操作类型：横向主导 → 拖拽移动；纵向主导 → 距离调整（阈值角度 35°）

**格点吸附 (Snap)：**

1. 所有物件位置映射到离散格点，基础步进 0.25 units
2. 吸附不是硬切——物件在释放时向最近格点做柔和插值（Snapping 状态，见状态机）
3. 拖拽过程中物件实际位置可在格点间自由移动（跟手），但视觉辅助线（可选）提示最近格点
4. 手指释放时触发吸附动画：物件从当前位置以 `EaseOutQuad` 曲线平滑移动到最近格点
5. 吸附动画时长动态计算：`snapDuration = clamp(distance / snapSpeed, minSnapDuration, maxSnapDuration)`
6. 格点系统支持逐章配置不同步进值（后期章节可使用更精细的网格）

**光源轨道移动：**

1. 光源不可自由移动——只能沿设计师预设的轨道路径滑动
2. 轨道类型：直线段（`LinearTrack`）或弧线段（`ArcTrack`）
3. 玩家触摸光源后，沿轨道方向滑动手指即可移动光源位置
4. 光源在轨道上的位置由归一化参数 `t ∈ [0, 1]` 控制
5. 光源移动同样带格点吸附（步进 0.1 on normalized track）
6. 第一章不开放光源操作——光源固定不可交互
7. 光源位置变化导致全场景影子实时更新

**边界约束：**

1. 每个谜题定义一个可操作的矩形边界（`InteractionBounds`）
2. 物件中心不可超出边界——到达边界后继续拖拽无效果
3. 拖拽释放时若物件因吸附计算被推到边界外，自动回弹至边界内最近格点
4. 回弹使用 `EaseOutBack` 曲线，有轻微过冲然后回正，传达"被轻轻推回来"的感觉
5. 边界不可见——不在场景中渲染边界线，通过物件运动的自然停止暗示边界存在

### States and Transitions

**物件状态机（每个可交互物件独立维护）：**

| State | Entry Condition | Exit Condition | Behavior |
|-------|----------------|----------------|----------|
| **Idle** | 初始状态 / 从其他状态退出 | 手指触碰物件碰撞体 → Selected | 物件静止在当前格点位置，无高亮，可被选中 |
| **Selected** | 从 Idle 经 Raycast 命中进入 | 手指滑动 → Dragging；双指触碰 → Rotating；触摸空白处 → Idle；外部锁定 → Locked | 播放选中放大动画（EaseOutBack 8f），激活轮廓高亮，渲染排序提前 |
| **Dragging** | 从 Selected 经手指滑动进入 | 手指抬起 → Snapping；外部锁定 → Locked | 物件跟随手指精确移动（16ms 响应），实时触发影子更新，边界夹持生效 |
| **Rotating** | 从 Selected 经双指手势进入 | 双指松开 → Snapping；外部锁定 → Locked | 物件绕 Y 轴旋转（可选 X 轴倾斜），旋转角度跟随手势，实时触发影子更新 |
| **Snapping** | 从 Dragging/Rotating 经手指释放进入 | 吸附动画完成 → Idle；手指再次触碰 → Selected（中断吸附） | 物件从当前位置向最近格点做柔和插值移动，播放放下回弹动画（EaseOutBounce 6+4f） |
| **Locked** | 外部系统（Shadow Puzzle）触发锁定 | 外部系统解锁 → Idle | 物件不可交互，忽略所有触摸输入。用于 PerfectMatch 吸附演出、场景过渡等场景 |

**状态转换规则补充：**

- `Selected → Dragging` 无最小滑动距离——手指一移动立即进入 Dragging
- `Dragging → Selected` 不存在——拖拽只能通过释放（→ Snapping）退出
- `Snapping` 过程中手指再次触碰同一物件，立即中断吸附动画跳到 Selected（允许快速连续调整）
- `Locked` 状态只能由外部系统解除，玩家操作无法退出
- 任何状态下收到 `PuzzleLockEvent` 立即转入 Locked（无过渡动画，吸附中也立即中断）

**光源状态机：**

| State | Entry Condition | Exit Condition | Behavior |
|-------|----------------|----------------|----------|
| **Fixed** | 第一章 / 配置为不可移动 | — | 光源固定不可交互，忽略触摸 |
| **TrackIdle** | 第二章起，默认状态 | 手指触碰光源 → TrackDragging | 光源静止在轨道当前位置 |
| **TrackDragging** | 从 TrackIdle 经触摸进入 | 手指抬起 → TrackSnapping | 光源沿轨道跟随手指移动，全场景影子实时更新 |
| **TrackSnapping** | 从 TrackDragging 经释放进入 | 吸附完成 → TrackIdle | 光源吸附到轨道上最近的离散点 |

### Interactions with Other Systems

**与 Input System 的交互（上游）：**

- 输入：触摸开始/移动/结束事件、手指数量、手指位置序列
- 本系统消费 Input System 提供的手势识别结果（单指拖拽、双指旋转）
- Input System 负责手势类型判定（拖拽 vs 旋转 vs 点击），本系统仅接收已识别的手势数据
- 职责边界：Input System 管"手指做了什么"，Object Interaction 管"物件怎么响应"

**与 Shadow Puzzle System 的交互（下游）：**

- 输出：物件位置变更事件 `ObjectTransformChanged(objectId, position, rotation)`
- 输出：光源位置变更事件 `LightPositionChanged(lightId, trackParameter)`
- 输入：接收 `PuzzleLockEvent(objectId)` 将指定物件锁定
- 输入：接收 `PuzzleLockAllEvent` 将所有物件锁定（PerfectMatch 吸附演出）
- 输入：接收 `PuzzleSnapToTargetEvent(objectId, targetTransform)` 驱动物件自动吸附到精确位置
- 本系统不知道匹配度——它只负责移动物件并广播变更，匹配判定完全由 Shadow Puzzle System 处理

**与 UI System 的交互：**

- 输出：物件选中/取消选中事件，用于 UI 显示操作提示图标
- 输出：当前操作模式（拖拽/旋转/距离调整），用于 UI 切换提示样式
- 当 UI 遮罩层激活（暂停菜单等），本系统暂停所有交互

**与 Hint System 的交互：**

- 输出：玩家操作频率、物件移动轨迹数据（用于判断是否卡关）
- 输入：Hint System 可请求高亮指定物件（Hint Tier 3 的方向箭头由 Hint System 自行渲染）

## Formulas

### Grid Snap Position — 格点吸附位置计算

```
snappedPos.x = round(rawPos.x / gridSize) * gridSize
snappedPos.y = rawPos.y  // Y 轴（高度）不吸附，物件在放置平面上
snappedPos.z = round(rawPos.z / gridSize) * gridSize
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| rawPos | Vector3 | 场景范围内 | runtime | 手指释放时物件的实际世界坐标 |
| gridSize | float | 0.1-0.5 | PuzzleConfig (Luban) | 格点步进大小，默认 0.25 units |
| snappedPos | Vector3 | 场景范围内 | calculated | 吸附后的目标世界坐标 |

**Expected output**: 以 gridSize 为步进的离散坐标集合。

### Snap Interpolation — 吸附插值动画

```
t_normalized = clamp(elapsed / snapDuration, 0, 1)
eased_t = EaseOutQuad(t_normalized)    // = 1 - (1 - t)²
currentPos = lerp(releasePos, snappedPos, eased_t)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| elapsed | float | 0 - snapDuration | runtime | 吸附动画已播放时长 |
| snapDuration | float | 0.05-0.15s | calculated | 动态计算的吸附时长 |
| releasePos | Vector3 | 场景范围内 | runtime | 手指释放时物件的实际位置 |
| snappedPos | Vector3 | 场景范围内 | calculated | 目标格点位置 |
| eased_t | float | 0-1 | calculated | 缓动后的归一化进度 |

**snapDuration 动态计算**：

```
distance = length(releasePos - snappedPos)
snapDuration = clamp(distance / snapSpeed, minSnapDuration, maxSnapDuration)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| snapSpeed | float | 2.0-5.0 u/s | config | 吸附移动速度 |
| minSnapDuration | float | 0.05s | config | 最短吸附时长（距离极近时避免瞬移感） |
| maxSnapDuration | float | 0.15s | config | 最长吸附时长（距离远时避免拖沓） |

**Expected output**: 柔和的减速曲线移动，总时长 50-150ms，距离越短越快。

**Edge case**: 当 releasePos 恰好在格点上（distance ≈ 0），跳过吸附动画直接进入 Idle。

### Rotation Snap — 旋转吸附

```
snappedAngle = round(rawAngle / rotationStep) * rotationStep
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| rawAngle | float | 0-360° | runtime | 手势释放时物件的实际旋转角度 |
| rotationStep | float | 10-45° | PuzzleConfig (Luban) | 旋转步进角度，默认 15° |

**Expected output**: 以 rotationStep 为步进的离散角度集合（0°, 15°, 30°, ... 345°）。

### Boundary Clamp & Rebound — 边界夹持与回弹

拖拽过程中的实时夹持：

```
clampedPos.x = clamp(rawPos.x, bounds.xMin + objectRadius, bounds.xMax - objectRadius)
clampedPos.z = clamp(rawPos.z, bounds.zMin + objectRadius, bounds.zMax - objectRadius)
```

释放后若因吸附被推出边界的回弹：

```
if (snappedPos outside bounds):
    reboundTarget = clampToBounds(snappedPos)
    t_normalized = clamp(elapsed / reboundDuration, 0, 1)
    eased_t = EaseOutBack(t_normalized, overshoot=0.3)
    // EaseOutBack(t, s) = 1 + (s+1) * (t-1)³ + s * (t-1)²
    currentPos = lerp(snappedPos, reboundTarget, eased_t)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| bounds | Rect | 谜题特定 | PuzzleConfig (Luban) | 当前谜题的可操作矩形边界 |
| objectRadius | float | 0.05-0.3 | ObjectConfig | 物件碰撞半径，防止物件中心到边界但视觉已越界 |
| reboundDuration | float | 0.12-0.2s | config | 回弹动画时长 |
| overshoot | float | 0.2-0.5 | config | EaseOutBack 过冲系数，控制回弹的"弹性感" |

**Expected output**: 物件向边界内回弹，带有轻微的过冲和回正，传达"被柔和推回"的触感。

**Edge case**: 物件同时在 X 和 Z 方向越界时，两轴独立回弹，共享相同的 eased_t 进度。

### Light Track Position — 光源轨道位置

```
worldPos = trackPath.Evaluate(t)    // t ∈ [0, 1], trackPath 是预设的曲线
snappedT = round(t / trackStep) * trackStep
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| t | float | 0-1 | runtime | 手指在轨道方向上的归一化投影位置 |
| trackStep | float | 0.05-0.2 | PuzzleConfig (Luban) | 轨道上的离散步进，默认 0.1 |
| trackPath | Spline/Bezier | — | 场景 Prefab | 设计师在 Unity 编辑器中摆放的轨道路径 |

**Expected output**: 光源沿轨道以 trackStep 为步进的离散位置集合。

### Fat Finger Compensation — 胖手指补偿

```
expandedRadius = colliderRadius + fatFingerMargin * (screenDPI / referenceDPI)
hitTest = sphereCast(touchRay, expandedRadius)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| colliderRadius | float | 物件特定 | ObjectConfig | 物件原始碰撞体半径 |
| fatFingerMargin | float | 8-16 dp | config | 基础胖手指补偿值 |
| screenDPI | float | 160-480 | runtime | 当前设备屏幕 DPI |
| referenceDPI | float | 326 | const | 参考 DPI（iPhone 13 Mini） |

**Expected output**: 在小屏 / 高 DPI 设备上自动扩大触碰判定范围。

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|----------|------------------|-----------|
| 两个物件碰撞体重叠，触摸命中两者 | 选中距离相机更近（Z 值更小）的物件 | 符合视觉直觉——看到的在前面的先被选中 |
| 拖拽中手指移出屏幕 | 等同手指抬起——物件停在最后位置 → Snapping | 防止物件失控 |
| 拖拽中收到来电/切后台 | 等同手指抬起——保存当前物件位置 → Snapping | 应用生命周期保护 |
| 极快速连续点击同一物件 | 防抖处理：200ms 内的重复触碰合并为同一选中事件 | 防止状态机快速翻转 |
| 物件吸附目标格点被另一物件占据 | 允许重叠——不做物件间碰撞排斥 | 影子谜题需要物件可重叠（两只杯子可以紧挨） |
| Snapping 过程中手指再次触碰 | 中断吸附动画，物件留在当前插值位置 → Selected | 允许快速连续微调 |
| 光源拖拽到轨道端点继续滑动 | 光源停在端点，不回弹、不循环 | 轨道有明确起止，不应循环 |
| 设备帧率低于 30fps | 物件位置直接跳到手指位置，跳过插值 | 低帧率下插值会造成明显滞后，宁可跳帧不可拖后 |
| 超小屏幕（iPhone SE 级别） | fatFingerMargin 自动增大到 16dp | 确保最小可交互区域 ≥ 44pt (Apple HIG) |
| 物件正在 Locked 状态时收到 Snap 吸附指令 | 执行吸附——Locked 只阻止玩家输入，不阻止系统驱动的动画 | PerfectMatch 吸附需要在 Locked 后驱动物件归位 |
| 同时双指旋转 + 单指在空白处点击 | 旋转操作优先级高于空白处点击，忽略空白处的取消选中 | 防止旋转时误触空白导致丢失选中 |

## Dependencies

| System | Direction | Nature of Dependency |
|--------|-----------|---------------------|
| Input System | This depends on | 接收手势识别结果（触摸/拖拽/旋转） |
| Shadow Puzzle System | Shadow Puzzle depends on this | 消费物件位置变更事件，驱动匹配度计算；反向发送锁定/吸附指令 |
| UI System | UI depends on this | 接收选中/操作模式事件，显示操作提示 |
| Hint System | Hint depends on this | 读取玩家操作数据用于卡关检测 |
| PuzzleConfig (Luban) | This depends on | 读取格点步进、边界范围、轨道路径等配置 |
| Settings & Accessibility | Settings configures this | 触控灵敏度倍率影响 dragThreshold 和 fatFingerMargin |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|-----------|--------------|------------|-------------------|-------------------|
| gridSize | 0.25 units | 0.1-0.5 units | 更粗网格，操作更容易但定位精度下降 | 更细网格，定位更精确但移动端操作挫败感上升 |
| rotationStep | 15° | 10-45° | 旋转更粗，操作更快但角度选择少 | 旋转更细，精确但移动端微调困难 |
| snapSpeed | 3.0 u/s | 2.0-5.0 u/s | 吸附更快更爽脆 | 吸附更慢更柔和 |
| minSnapDuration | 0.05s | 0.03-0.08s | 近距离吸附也有可感知的动画 | 近距离吸附几乎瞬移 |
| maxSnapDuration | 0.15s | 0.10-0.25s | 远距离吸附更慢更仪式感 | 远距离吸附更快但可能感觉"飞"过去 |
| fatFingerMargin | 8 dp | 4-16 dp | 更容易选中物件但可能误选相邻物件 | 选中更精确但小屏操作困难 |
| reboundDuration | 0.15s | 0.10-0.25s | 回弹更慢更柔和 | 回弹更快更"硬" |
| reboundOvershoot | 0.3 | 0.0-0.8 | 回弹更有弹性/活泼 | 回弹更克制/无过冲 |
| trackStep | 0.1 | 0.05-0.2 | 轨道步进更粗，光源位置选择少 | 轨道步进更细，可微调光源 |
| selectScaleMultiplier | 1.05 | 1.02-1.10 | 选中放大更明显 | 选中放大更微妙 |
| bounceAmplitude | 0.02 | 0.01-0.05 | 放下回弹更明显 | 放下回弹更克制 |
| dragDirectionThreshold | 35° | 25-45° | 更容易触发距离调整（纵向敏感） | 更容易触发平面拖拽（横向敏感） |

## Visual/Audio Requirements

| Event | Visual Feedback | Audio Feedback | Priority |
|-------|----------------|---------------|----------|
| 物件选中 | 轮廓高亮（白色描边 2px）+ 放大 1.05x（EaseOutBack 8f） | 柔和 click 音（陶瓷/木质材质变体） | MVP |
| 物件拖拽中 | 物件底部投下轻微阴影增强浮起感 | 极轻微摩擦音（依材质变体：木桌面/布面/玻璃） | MVP |
| 物件格点吸附 | 吸入动画（EaseOutQuad 50-150ms） | 极轻微"咔"音（满足感微触发） | MVP |
| 物件放下回弹 | 回弹动画（EaseOutBounce 6+4f）+ 轮廓高亮消退 | 轻放音（材质变体：瓷碰桌面 / 金属碰木头） | MVP |
| 物件旋转中 | 物件实时旋转 + 影子实时跟随 | 极轻微转轴音 | MVP |
| 光源移动 | 光源沿轨道移动 + 全场影子实时变化 | 电灯移动的轻微嗡鸣（音调随位置变化） | VS |
| 物件到达边界 | 无视觉特效（不显示边界线） | 极轻微的"闷"反馈音 | MVP |
| 物件边界回弹 | 轻微弹性回弹（EaseOutBack 120-200ms） | 轻微弹性音 | MVP |
| 物件被锁定 | 高亮消退 + 不再响应触摸 | 无额外音效 | MVP |
| 物件被系统吸附（PerfectMatch） | 物件以 EaseOutBack 滑入目标位置（18-30f） | 满足感共鸣音（由 Shadow Puzzle System 触发） | MVP |
| 触碰不可交互物件 | 无反应（零反馈，不干扰） | 无 | MVP |
| 触碰空白处取消选中 | 当前选中物件高亮消退 + 缩回原始大小 | 无 | MVP |

## Game Feel

> 本章节是本 GDD 的核心——Object Interaction System 直接承载"日常即重量"设计柱。
> 如果物件的触感不对，整个游戏的情绪基底就不成立。

### Feel Reference

**正向参考**：**Unpacking（拆包）的物件放置**。
具体借鉴的质感：拿起物件时有轻微的"提起"感（放大 + 阴影变化），拖拽时物件稳定跟随手指不飘忽，放在正确位置时有清晰的"咔嗒"满足感（格点吸附 + 音效），整个过程是温柔的、不焦虑的、像在真的整理一个房间。

**次要参考**：**Monument Valley 的触控质感**。
具体借鉴的质感：触摸即响应的零延迟感、操作空间的简洁克制（一个手指就能完成大部分操作）、手感比精确度更重要。

**反向参考（NOT this）**：
- **不是** Tetris / 俄罗斯方块——没有时间压力、没有精确要求高的放置判定、没有"放错就惩罚"
- **不是** 3D 建模软件——没有多自由度操控、没有坐标轴小部件（Gizmo）、没有需要学习的操作模式
- **不是** 物理沙盒（如 Human Fall Flat）——没有惯性、没有甩飞、没有物理碰撞的混乱

### Input Responsiveness

| Action | Max Input-to-Response Latency (ms) | Frame Budget (at 60fps) | Notes |
|--------|-----------------------------------|------------------------|-------|
| 物件选中高亮 | 50ms | 3 frames | 触摸到高亮可见的总延迟 |
| 物件拖拽跟随 | 16ms | 1 frame | **硬指标**——物件位置在下一渲染帧必须更新到手指位置 |
| 物件旋转跟随 | 16ms | 1 frame | 旋转角度在下一渲染帧必须更新 |
| 物件释放 → 吸附开始 | 16ms | 1 frame | 手指抬起到吸附动画第一帧 |
| 格点吸附完成 | 50-150ms | 3-9 frames | 从释放到物件到达格点的总时长 |
| 放下回弹动画 | 167ms | 10 frames (6+4) | 包含弹跳全过程 |
| 光源移动 → 影子更新 | 33ms | 2 frames | 可比物件多 1 帧，影子计算更重 |
| 边界回弹 | 16ms | 1 frame | 到达边界瞬间开始回弹，无等待 |

### Animation Feel Targets

| Animation | Startup Frames | Active Frames | Recovery Frames | Feel Goal | Notes |
|-----------|---------------|--------------|----------------|-----------|-------|
| 物件选中放大 | 0 | 8 | 0 | 即时响应，轻微弹性过冲 | EaseOutBack，scale 1.0 → 1.05，过冲到 1.07 再回落 |
| 物件放下回弹 | 0 | 6 | 4 | 轻落，有"触地"的真实感 | EaseOutBounce，scale 1.05 → 1.0，在 1.0 附近微弹两次 |
| 格点吸附移动 | 0 | 3-9（动态） | 0 | "吸入"感——先快后慢 | EaseOutQuad，距离远则帧数多，距离近则帧数少 |
| 边界回弹 | 0 | 7-12 | 0 | 被"轻轻推回来" | EaseOutBack(overshoot=0.3)，有微妙过冲 |
| 光源轨道吸附 | 0 | 4-6 | 0 | 略带磁性的吸入 | EaseOutQuad，比物件吸附稍快 |
| PerfectMatch 系统吸附 | 0 | 18-30 | 12 | 温柔滑入，仪式感 | EaseOutBack(overshoot=0.5)，由 Shadow Puzzle System 触发 |
| 取消选中缩回 | 0 | 6 | 0 | 安静地退回原状 | EaseOutQuad，无弹性，平滑缩小 |

### Impact Moments

| Impact Type | Duration (ms) | Effect Description | Configurable? |
|-------------|--------------|-------------------|---------------|
| 格点吸附微震动 | 15ms | 物件到达格点时设备产生极轻微 Haptic（UIImpactFeedbackGenerator.light） | Yes |
| 物件放下触感 | 30ms | 物件放下回弹最低点时产生轻微 Haptic（UIImpactFeedbackGenerator.medium） | Yes |
| 边界回弹触感 | 20ms | 物件触碰边界时的阻尼 Haptic（UIImpactFeedbackGenerator.rigid） | Yes |
| 光源到达端点 | 25ms | 光源到达轨道终点时的"到底"触感（UINotificationFeedbackGenerator.warning） | Yes |
| PerfectMatch 系统吸附触感 | 150ms | 物件被系统吸附归位时的持续渐弱 Haptic（由 Shadow Puzzle 触发） | Yes |

> **注意**：所有 Haptic 反馈均为 iOS 的 Taptic Engine / Android 的 VibrationEffect。Haptic 强度必须非常克制——这是一个安静的游戏，震动应该像"物件放在桌面上的微弱传导"，绝不是手柄游戏的爆炸反馈。

### Weight and Responsiveness Profile

- **Weight**: 轻中等。物件不是纸片也不是铅球——像是在桌面上轻推一只陶瓷杯或一个台灯底座。有重量的暗示（放下时的回弹、格点吸附时的"咔"）但操作不费力。关键区别：重量感来自**反馈动画和音效**，而非**操作延迟或惯性**。物件绝不该因为"有重量"而拖后于手指。

- **Player control**: 极高。手指到哪物件到哪，零加速度曲线，零惯性。放手即停，绝无滑动。玩家在每一个瞬间都完全控制物件位置。唯一的"失控"瞬间是手指释放后的格点吸附——但吸附距离极短（最多 0.125 units，即半个格点对角线）且时间极短（50-150ms），玩家不会感觉失控。

- **Snap quality**: 柔和的磁性吸入。不是"啪"的硬切（像 Photoshop 网格吸附），而是"嗖——咔"的两段式：先快速接近（EaseOutQuad 的快起始段），然后在最后几个像素减速"吸入"。配合极轻微的 Haptic 和"咔"音，传达"对，就是这个位置"。

- **Acceleration model**: 纯即时响应（arcade feel）。物件移动没有任何加速/减速曲线——拖拽时手指速度就是物件速度。唯一使用缓动的地方是**自动动画**（吸附、回弹、选中放大），玩家直接操控的部分全部是线性 1:1 映射。

- **Failure texture**: 极度宽容。没有错误状态，没有"放错位置"的惩罚反馈（无红色闪烁、无错误音效、无震屏）。物件放在任何格点上都是合法的——只是影子看起来还不太对。整个系统的态度是"慢慢来，没关系"。即使拖到边界外也只是被温柔地推回来。

### Feel Acceptance Criteria

- [ ] 物件拖拽跟手——5 人真机测试中无人用"延迟"、"拖后"描述操作感受
- [ ] 格点吸附被测试者感知为"柔和吸入"——无人用"跳跃"、"卡顿"形容
- [ ] 物件放下的回弹被感知为"轻放在桌面上"——无人用"弹飞"、"砸下去"形容
- [ ] iPhone 13 Mini（5.4 inch 屏幕）上单手操作 3 分钟，测试者不报告手指疲劳或误触
- [ ] 测试者能在无教程情况下发现拖拽和旋转操作（5 分钟内）
- [ ] Haptic 反馈被感知为"微妙的触感"——无人形容为"震动太强"或"烦人"
- [ ] 边界回弹的感觉是"被轻轻挡住"——无人感觉"撞墙"或"卡住了"
- [ ] 光源轨道操作被测试者在首次引导后理解为"沿固定路线移动"——无人尝试自由移动
- [ ] 在 iPhone 13 Mini 上所有可交互物件均可被稳定选中（10 次尝试 ≥ 9 次首触命中）
- [ ] 连续 5 分钟拖拽操作，帧率不低于 55fps（iPhone 13 Mini 标准）

## UI Requirements

| Information | Display Location | Update Frequency | Condition |
|-------------|-----------------|-----------------|-----------|
| 可交互物件标记 | 物件上方微弱光点 | 场景加载时出现，首次操作后 3 秒消退 | 仅教学/首次进入谜题时 |
| 拖拽手势提示 | 屏幕底部居中 | 首次选中物件时 | *由 Tutorial / Onboarding 系统统一管理*（参见 `tutorial-onboarding.md` 的 `tut_drag` 步骤） |
| 旋转手势提示 | 屏幕底部居中 | 首次双指操作时 | *由 Tutorial / Onboarding 系统统一管理*（参见 `tut_rotate` 步骤） |
| 距离调整提示 | 屏幕底部居中 | 首次纵向滑动时 | *由 Tutorial / Onboarding 系统统一管理*（参见 `tut_distance` 步骤） |
| 光源操作提示 | 光源附近 | 第二章首次出现光源操作时 | *由 Tutorial / Onboarding 系统统一管理*（参见 `tut_light` 步骤） |
| 操作模式图标 | 选中物件旁（右上方 offset） | 操作模式切换时 | 物件被选中时显示当前可执行操作的小图标 |

> **无常驻 HUD**：不在屏幕上显示任何格点网格、坐标、角度数值。操作提示是临时的、低侵入的、看几次就消失的。

## Cross-References

| This Document References | Target GDD | Specific Element Referenced | Nature |
|--------------------------|-----------|----------------------------|--------|
| 物件位置变更驱动影子匹配计算 | `design/gdd/shadow-puzzle-system.md` | `matchScore` 公式的输入数据（锚点投影位置） | Data dependency |
| PerfectMatch 触发物件锁定 | `design/gdd/shadow-puzzle-system.md` | PerfectMatch 状态转换 → 发送 PuzzleLockAllEvent | State trigger |
| PerfectMatch 吸附动画参数 | `design/gdd/shadow-puzzle-system.md` | `snapDuration`（0.3-0.8s）和 EaseOutBack 曲线规格 | Rule dependency |
| 格点步进值 gridSize | `design/gdd/shadow-puzzle-system.md` | Tuning Knobs 中 `gridSnapSize = 0.25 units` | Data dependency |
| 玩家操作数据驱动提示触发 | `design/gdd/hint-system.md` | 操作频率、停留时间阈值 | Ownership handoff |
| 物件材质变体影响拾取/放置音效 | `design/art/art-bible.md` | 物件材质原则、物件建模规范 | Rule dependency |
| 触碰判定扩大遵循 Apple HIG 44pt 最小触碰区 | External: Apple Human Interface Guidelines | Minimum touch target size | Rule dependency |

## Acceptance Criteria

- [ ] 物件拖拽 input-to-visual 延迟 ≤ 16ms（通过高速摄像机或 Unity Profiler 验证）
- [ ] 格点吸附动画时长在 50-150ms 范围内且曲线为减速类型
- [ ] 物件选中放大动画恰好 8 帧（EaseOutBack 曲线）
- [ ] 物件放下回弹动画恰好 10 帧（6 active + 4 recovery）
- [ ] iPhone 13 Mini 上所有可交互物件（10 种）均可在 3 次内选中
- [ ] 所有物件操作在 60fps 下 Update 计算耗时 < 1ms
- [ ] 拖拽中物件不穿越场景边界——边界夹持 100% 有效
- [ ] 光源仅能沿轨道移动，无法脱离轨道
- [ ] 旋转步进值、格点步进值、边界范围均从 Luban 配置表读取，无硬编码
- [ ] 状态机覆盖所有定义的 6 个状态和所有合法转换——无死锁状态
- [ ] 所有物件交互在低帧率（30fps）下仍可操作，无功能性问题
- [ ] Performance: 10 个物件同时在场，拖拽操作不低于 55fps（iPhone 13 Mini）

## Open Questions

| Question | Owner | Deadline | Resolution |
|----------|-------|----------|-----------|
| 距离调整的交互方式——当前设计为纵向滑动，是否需要独立的 UI 控件（如滑杆）辅助？ | Interaction Design | 原型阶段 | 先用纯手势原型测试，若测试者难以发现纵向滑动再补充 UI 控件 |
| 双指旋转在单手操作时不可用——是否需要为单手模式提供替代旋转操作？ | Interaction Design | 原型阶段 | 可能方案：长按弹出旋转盘 / 物件两侧的旋转按钮 |
| 物件选中后是否需要轻微"提起"效果（Y 轴微抬 + 阴影扩散）来增强重量感？ | Interaction Design / Art | 原型阶段 | 需要和 Art 确认是否影响影子投影的准确性 |
| Android 端 Haptic 是否能达到 iOS Taptic Engine 同等精细度？低端机是否关闭？ | Tech Lead | MVP 前 | 需要调研 Android VibrationEffect API 覆盖率，低端机可能降级为视觉反馈 |
| 物件间是否需要最小间距约束防止完全重叠？ | Game Design | 原型阶段 | 当前设计允许完全重叠——需验证影子谜题是否需要此限制 |
| 格点可视化辅助（如极淡的网格线）是否对操作有帮助还是会干扰画面美感？ | Game Design / Art | 原型阶段 | 先无格点可视化测试，若测试者频繁找不到"整"的位置再补充 |
