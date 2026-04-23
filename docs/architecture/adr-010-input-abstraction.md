// 该文件由Cursor 自动生成

# ADR-010: Input Abstraction — Gesture Recognition, Blocker & Filter Stacks

## Status

Proposed

## Date

2026-04-22

## Last Verified

2026-04-22

## Decision Makers

Technical Director, Game Designer, Lead Programmer

## Summary

《影子回忆》需要一个集中式输入系统来抽象原始触摸为 5 种手势，并提供 InputBlocker（全量阻断）和 InputFilter（白名单过滤）两层门控机制。本 ADR 决定使用 Unity 旧版 Touch API（`Input.GetTouch()`）构建自定义三层输入架构——Raw Touch Sampling → Blocker/Filter Gate → Gesture Recognition，手势通过 TEngine GameEvent 分发，所有阈值由 Luban 配置表驱动。

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.62f2 (LTS) |
| **Domain** | Input |
| **Knowledge Risk** | LOW — Unity legacy Touch API (`Input.GetTouch`, `Input.touchCount`) is stable and well-documented within training data |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `.claude/docs/technical-preferences.md` |
| **Post-Cutoff APIs Used** | None — all APIs used are part of the legacy `UnityEngine.Input` class available since Unity 4.x |
| **Verification Required** | Verify `Input.GetTouch()` functions correctly on target devices with Unity 2022.3.62f2; verify `Screen.dpi` availability on all target Android OEMs |

> **Note**: If Knowledge Risk is MEDIUM or HIGH, this ADR must be re-validated if the
> project upgrades engine versions. Flag it as "Superseded" and write a new ADR.

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None |
| **Enables** | ADR-013 (Object Interaction — consumes gesture events), ADR-019 (Tutorial InputFilter integration) |
| **Blocks** | Object Interaction system, Tutorial system, Narrative input blocking |
| **Ordering Note** | Must be Accepted before any system that consumes gesture events can begin implementation |

## Context

### Problem Statement

《影子回忆》的所有玩法操作（移动物件、旋转光源、缩放视角）都依赖触屏手势。当前没有输入基础设施，如果各系统各自直接调用 `Input.GetTouch()` 会导致：

1. 手势识别逻辑重复在多处实现，不一致且难以维护
2. 无法实现集中式输入阻断（UI 面板打开时阻断游戏手势）
3. 无法实现教学模式的手势白名单过滤
4. 手势冲突解决（Tap vs Drag、Rotate vs Pinch）无统一仲裁
5. 延迟预算无集中保障点

不做此决策意味着 Object Interaction、Tutorial、Narrative 三个系统都无法开始实现。

### Current State

项目处于 Pre-Production 阶段，尚无输入系统代码。13 个系统 GDD 中有 4 个直接依赖输入事件（Input System、Object Interaction、Tutorial、Narrative），另有 2 个间接依赖（Shadow Puzzle、UI System）。

### Constraints

- **引擎限制**：项目使用 Unity 2022.3.62f2 LTS，TEngine 框架未集成 New Input System package，采用旧版 Touch API
- **平台约束**：移动端优先（iOS / Android），触摸为主输入；PC 端（Steam）后续阶段适配键鼠
- **性能约束**：60 FPS 目标，16.67ms 帧预算，输入处理必须 < 0.5ms（TR-input-016）
- **框架约束**：所有模块间通信必须通过 TEngine `GameEvent`（int-based event IDs），禁止直接引用（P2 原则）
- **数据驱动约束**：所有阈值参数必须来自 Luban 配置表，禁止硬编码（P1 原则）
- **异步约束**：输入系统是同步密集型——必须在同一帧内完成采样到分发，但不可阻塞主线程超出预算

### Requirements

- **FR-1**: 识别 5 种手势：Tap、Drag、Rotate、Pinch、LightDrag
- **FR-2**: 提供 InputBlocker 栈式全量阻断（多阻断源可叠加）
- **FR-3**: 提供 InputFilter 白名单过滤（教学步骤隔离）
- **FR-4**: 优先级链：InputBlocker > InputFilter > Normal Pass-through
- **FR-5**: 手势通过 GesturePhase 生命周期管理（Began / Updated / Ended / Cancelled）
- **FR-6**: 双指手势互斥——进入 Rotate 后锁定直到手指抬起，不中途切换为 Pinch
- **FR-7**: Fat finger compensation——2D 偏移补偿手指中心到物件视觉中心的误差
- **PR-1**: 手势识别 < 0.5ms/帧（TR-input-016）
- **PR-2**: 热路径零 GC allocation

## Decision

使用 Unity 旧版 Touch API（`Input.GetTouch()` / `Input.touchCount`）构建自定义三层输入系统，通过 `IInputService` 接口暴露 Blocker/Filter API，手势事件通过 TEngine `GameEvent` 分发（每种手势一个 event ID）。

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   Game Systems Layer                     │
│  (Object Interaction, Shadow Puzzle, Tutorial, etc.)     │
│              ↑ Subscribe via GameEvent                   │
├─────────────────────────────────────────────────────────┤
│              Layer 3: Gesture Recognition                │
│  ┌────────────────────┐  ┌─────────────────────┐        │
│  │  SingleFinger FSM  │  │  DualFinger FSM     │        │
│  │  Idle → Pending →  │  │  Idle → Pending2 →  │        │
│  │  Tap / Dragging    │  │  Rotating / Pinching│        │
│  └────────────────────┘  └─────────────────────┘        │
│              ↓ Recognized gestures                       │
│  ┌──────────────────────────────────────────────┐       │
│  │  GestureData dispatch via GameEvent (5 IDs)  │       │
│  │  Tap / Drag / Rotate / Pinch / LightDrag     │       │
│  └──────────────────────────────────────────────┘       │
├─────────────────────────────────────────────────────────┤
│              Layer 2: Blocker / Filter Gate              │
│                                                         │
│  Priority 1 (highest): InputBlocker Stack               │
│    if (blockerStack.Count > 0) → DISCARD ALL            │
│                                                         │
│  Priority 2: InputFilter                                │
│    if (activeFilter != null)                             │
│      → only allowedGestures pass, rest discarded        │
│                                                         │
│  Priority 3 (lowest): Normal Pass-through               │
│    → all gestures proceed to Layer 3                    │
├─────────────────────────────────────────────────────────┤
│              Layer 1: Raw Touch Sampling                 │
│                                                         │
│  Input.GetTouch(i) per frame, i ∈ [0, touchCount)      │
│  Track: TouchPhase (Began/Moved/Stationary/Ended/       │
│         Cancelled), fingerId, position, deltaPosition   │
│  DPI normalization for threshold calculation             │
│  Max 2 tracked fingers (ignore touch index ≥ 2)         │
└─────────────────────────────────────────────────────────┘
```

**数据流时序（单帧内）：**

```
Update()
  ├─ 1. Sample raw touches (Layer 1)
  ├─ 2. Check InputBlocker stack
  │     └─ if non-empty → skip to end (all input discarded)
  ├─ 3. Run gesture FSMs (Layer 3) → produce candidate gestures
  ├─ 4. Check InputFilter against each candidate
  │     └─ if filtered → discard gesture, no event
  └─ 5. Dispatch surviving gestures via GameEvent (Layer 3 output)
```

> **注意**：Layer 2 的检查在逻辑上分两处执行——Blocker 检查在 FSM 之前（避免无用计算），Filter 检查在 FSM 之后（需要知道手势类型才能判断白名单）。

### Key Interfaces

```csharp
// === Enums ===

public enum GestureType
{
    Tap,
    Drag,
    Rotate,
    Pinch,
    LightDrag
}

public enum GesturePhase
{
    Began,
    Updated,
    Ended,
    Cancelled
}

// === Core Data ===

public struct GestureData
{
    public GestureType Type;
    public GesturePhase Phase;
    public Vector2 ScreenPosition;   // primary touch position (screen pixels)
    public Vector2 Delta;            // frame delta (screen pixels)
    public float AngleDelta;         // Rotate only: radians, positive = CCW
    public float ScaleDelta;         // Pinch only: >1 zoom in, <1 zoom out
    public int TouchCount;           // 1 for single-finger, 2 for dual-finger
    public int TapCount;             // for Tap: 1=single, 2=double (reserved)
}

// === Service Interface ===

public interface IInputService
{
    // --- Blocker API (stack-based, token-keyed) ---
    void PushBlocker(string token);
    void PopBlocker(string token);
    bool IsBlocked { get; }
    int BlockerCount { get; }

    // --- Filter API (single-active, stack-replaceable) ---
    void PushFilter(GestureType[] allowedGestures);
    void PopFilter();
    bool IsFiltered { get; }
    GestureType[] ActiveFilterGestures { get; }
}
```

**GameEvent 接口（遵循 ADR-027 `[EventInterface]` 协议，取代原 ADR-006 §1 const int 分配）：**

```csharp
// 文件: Assets/GameScripts/HotFix/GameLogic/IEvent/IGestureEvent.cs
using TEngine;

namespace GameLogic
{
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IGestureEvent
    {
        void OnTap(GestureData data);
        void OnDrag(GestureData data);
        void OnRotate(GestureData data);
        void OnPinch(GestureData data);
        void OnLightDrag(GestureData data);
    }
}
```

Sender（`GestureDispatcher`）：`GameEvent.Get<IGestureEvent>().OnTap(data)`
Listener：`GameEvent.AddEventListener<IGestureEvent_Event>(OnTap)` 或 `GameEventMgr.AddEventListener` 或 `UIWindow.AddUIEvent`。

> **注意**: 本 ADR 不再允许 `public const int` 事件 ID。所有手势事件统一通过 `IGestureEvent` 接口派发；Event ID 由 Roslyn Source Generator 在编译期生成。参见 ADR-027。

每个事件的 payload 为 `GestureData` struct（值类型，无 GC allocation）。

### Implementation Guidelines

**1. Module 注册**

InputService 作为 TEngine Module 注册，通过 `GameModule.Input`（或 `ModuleSystem` accessor）全局访问。生命周期跟随 `GameModule` 初始化/销毁。

**2. Touch Sampling**

- 在 `Update()` 中调用 `Input.GetTouch(i)`，遍历 `Input.touchCount`
- 只跟踪前 2 个触摸点（`fingerId` 映射到 slot 0/1），忽略第 3+ 触摸
- 维护 `TouchState` struct 数组（pre-allocated, size=2）存储帧间状态

**3. Gesture FSM 实现**

- **SingleFingerFSM**：Idle → Pending → (Tap | Dragging | LongPress)
  - `dragThreshold` = `baseDragThreshold_mm * Screen.dpi / 25.4`（Screen.dpi=0 时 fallback 160）
  - `tapTimeout` 使用 `Time.unscaledDeltaTime` 累加，不受 TimeScale 影响
- **DualFingerFSM**：Idle → Pending2 → (Rotating | Pinching)
  - 互斥锁定：一旦进入 Rotating 或 Pinching，直到所有手指抬起才释放
  - 双指间距 < `minFingerDistance`(20px) 时忽略旋转/缩放输入

**4. LightDrag 判定**

LightDrag 在手势识别层与 Drag 相同。上层 Object Interaction 根据当前选中对象类型将 Drag 事件映射为普通拖拽或光源轨道移动。Input System 通过 `GestureType.LightDrag` 区分——判定依据由上层通过回调或状态查询提供（具体交互协议见 ADR-013）。

**5. Fat Finger Compensation**

- 触摸位置向物件视觉中心偏移补偿
- 补偿量 = f(finger_radius, object_bounds)，具体算法在 Object Interaction 层实现
- Input System 仅提供原始触摸位置 + 补偿后位置两个字段供上层选择使用

**6. Blocker/Filter 实现**

- `InputBlocker`：`Stack<string>` 或 `List<string>`，token 为调用方标识（如 `"UIPanel_Settings"`, `"Narrative_Seq01"`）
  - `PopBlocker(token)` 必须匹配 token，防止误 pop 其他模块的 blocker
  - 安全措施：如果 Pop 的 token 不在栈中，输出警告日志但不抛异常
- `InputFilter`：单一激活，新 Push 覆盖旧 Filter
  - `allowedGestures` 数组在 Push 时深拷贝，防止外部修改
  - Pop 时恢复"无过滤"状态（不恢复上一个 filter）

**7. 零 Allocation 策略**

- `GestureData` 为 struct（值类型）
- `TouchState` 数组 pre-allocated（容量=2）
- Blocker stack 使用 pre-allocated `List<string>`（初始容量 4）
- GameEvent dispatch 使用 `GameEvent.Send(int eventId, object args)` —— `GestureData` 装箱为 `object` 是唯一 allocation 点，可后续优化为泛型 event 或 unsafe reinterpret 如需要

**8. 配置表（Luban）**

| Config Key | Type | Default | Description |
|---|---|---|---|
| `baseDragThreshold_mm` | float | 3.0 | 拖拽判定物理距离阈值 (mm) |
| `tapTimeout` | float | 0.25 | 点击判定最大时间窗口 (s) |
| `rotateThreshold` | float | 8.0 | 旋转识别累计角度阈值 (°) |
| `pinchThreshold` | float | 0.08 | 缩放识别比例偏离阈值 |
| `minFingerDistance` | float | 20.0 | 双指最小安全间距 (px) |
| `maxDeltaPerFrame` | float | 100.0 | 单帧最大位移限制 (px) |
| `fallbackDPI` | float | 160.0 | Screen.dpi=0 时的备用值 |
| `pcRotateSensitivity` | float | 0.005 | PC 鼠标旋转灵敏度 (rad/px) |
| `pcScrollSensitivity` | float | 0.1 | PC 滚轮缩放灵敏度 |

## Alternatives Considered

### Alternative 1: Unity New Input System Package

- **Description**: 使用 `com.unity.inputsystem` 包，通过 Action Maps 定义输入绑定，利用内置 Touch interaction 和 gesture recognition
- **Pros**: 跨平台抽象能力强；内置 Action Maps 支持 rebinding；社区文档丰富；天然支持多设备切换
- **Cons**: 对本项目过重（仅需触摸输入）；TEngine 框架未围绕 New Input System 构建，集成需额外适配工作；引入包级依赖增加升级风险；每帧 overhead 略高于直接 `Input.GetTouch()`；Blocker/Filter 机制仍需自行实现
- **Estimated Effort**: 1.5x（需要额外的 TEngine 适配层）
- **Rejection Reason**: 收益不匹配复杂度——本项目仅面向移动触摸 + 后续 PC 键鼠，不需要 gamepad、VR controller 等跨设备抽象。且核心需求（Blocker/Filter 门控）New Input System 也不提供，仍需自建

### Alternative 2: Third-Party Gesture Library (Lean Touch / TouchScript)

- **Description**: 集成成熟的第三方手势识别库，在其基础上封装 Blocker/Filter 层
- **Pros**: 手势识别经过大量项目验证；减少自研 FSM 开发时间
- **Cons**: 引入外部依赖（许可证、维护风险、升级兼容性）；库的手势识别逻辑是黑盒，难以定制 Tap/Drag 阈值判定行为；与 TEngine GameEvent 分发机制需要桥接层；可能包含大量项目不需要的功能（swipe、multi-tap combo 等）
- **Estimated Effort**: 0.8x（识别层节省时间，但集成适配消耗时间）
- **Rejection Reason**: 对核心输入路径引入不可控外部依赖，风险过高。手势识别逻辑本身不复杂（2 个 FSM），自研的可控性和调试便利性更重要

### Alternative 3: 各系统直接调用 Input.GetTouch()

- **Description**: 不建立集中输入层，Object Interaction / Tutorial / UI 各自在需要时读取触摸数据
- **Pros**: 零抽象 overhead；实现最简单
- **Cons**: 手势识别逻辑重复（Tap/Drag 阈值判定至少需要在 3 处实现）；无法实现集中式 InputBlocker（UI 打开时每个系统都要自行检查）；InputFilter 教学白名单无处安放；手势冲突解决无统一仲裁；违反单一职责原则和 P2 事件解耦原则
- **Estimated Effort**: 0.5x（初始快，但维护成本指数增长）
- **Rejection Reason**: 根本无法满足 InputBlocker/InputFilter 的集中管控需求，也违反项目架构原则

## Consequences

### Positive

- 集中式输入管控：UI、Narrative、Tutorial 都通过统一的 Blocker/Filter API 控制输入，行为可预测
- 手势识别一致性：所有系统消费相同的手势事件，不会出现同一触摸在不同系统中被识别为不同手势
- 可测试性强：`IInputService` 接口可 mock，单元测试可模拟 blocker/filter 场景
- 教学隔离干净：Tutorial 通过 `PushFilter` 限制手势范围，无需入侵 Object Interaction 代码
- 数据驱动：所有阈值来自 Luban 配置表，策划可独立调优无需重编译

### Negative

- 必须自行维护 2 个手势识别 FSM（SingleFinger + DualFinger），无内置库支持
- PC 适配需要额外的输入映射层（鼠标/键盘 → 手势事件），增加后续工作量
- `GestureData` 通过 `GameEvent.Send(int, object)` 分发时存在一次装箱 allocation（可后续优化）

### Neutral

- Input System 不知道上层选中了什么物件——这是设计意图，保持层级隔离
- LightDrag 的语义判定推迟到 Object Interaction 层——Input System 只负责传递标记

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| Fat finger compensation 在小屏设备上效果不佳 | Medium | Medium | 预留多档补偿配置（Luban 表），真机测试 5.4"-6.7" 全覆盖 |
| 双指旋转/缩放在低端设备上 touch reporting 不稳定 | Low | Medium | minFingerDistance 安全阈值 + 方向稳定性检测，异常帧忽略输入 |
| GestureData 装箱 allocation 在高频触摸场景下产生 GC 压力 | Low | Low | 监控 Profiler；如触发 GC spike，切换为泛型 event 或 static buffer |
| Blocker token 泄漏（push 后忘记 pop）导致输入永久锁死 | Medium | High | 开发阶段 Debug.LogWarning 超时检测（blocker 存活 > 30s 报警）；提供 `ForcePopAllBlockers()` 紧急恢复 API |
| tapTimeout 和 dragThreshold 在不同 DPI 设备上手感差异大 | Medium | Medium | DPI 归一化公式确保物理距离一致；tapTimeout 不受 DPI 影响；上线前 5+ 设备真机调参 |

## Performance Implications

| Metric | Before | Expected After | Budget |
|--------|--------|---------------|--------|
| CPU (frame time) — Input processing | 0ms (no system) | < 0.5ms | 0.5ms (TR-input-016) |
| Memory — Runtime | 0 | ~2KB (touch states + blocker stack + filter state + FSM state) | Negligible |
| Memory — Config tables | 0 | ~1KB (Luban input config row) | Negligible |
| GC Allocation / frame | 0 | 0-1 boxing per gesture dispatch (40B) | Zero target; boxing tolerated at MVP |

**性能保障措施：**

- Touch sampling 和 FSM 更新均为 O(1) 操作（最多 2 touches × constant FSM transitions）
- Blocker check 为 O(1)（stack count 比较）
- Filter check 为 O(n)（n = allowedGestures 数组长度，最大 5）
- 无 Physics query、无 string 操作、无 LINQ 在热路径中
- Profiler marker `InputService.Update` 标记，便于帧级监控

## Migration Plan

本系统为新建，无需迁移现有代码。

1. **Step 1**: 创建 `IInputService` 接口和 `GestureData`/`GestureType`/`GesturePhase` 定义
2. **Step 2**: 实现 `InputService` Module（注册到 TEngine `GameModule`）
3. **Step 3**: 实现 Raw Touch Sampling（Layer 1）+ InputBlocker/InputFilter API（Layer 2）
4. **Step 4**: 实现 SingleFingerFSM 和 DualFingerFSM（Layer 3）
5. **Step 5**: 集成 GameEvent dispatch + Luban 配置表加载
6. **Step 6**: 编写单元测试（Mock touch input → 验证手势识别 + Blocker/Filter 行为）
7. **Step 7**: 真机测试（5.4"-6.7" 屏幕，iOS + Android）

**Rollback plan**: 由于是新建系统且上层系统尚未实现，回滚仅需删除 InputService 相关代码文件。如 ADR 被 Rejected，可重新评估 Alternative 1（New Input System）。

## Validation Criteria

- [ ] 5 种手势（Tap / Drag / Rotate / Pinch / LightDrag）在 iPhone 13 Mini + Android 中端机上识别准确率 > 95%
- [ ] 快速连续 Tap（间隔 > 100ms）100% 正确识别，0% 误判为 Drag
- [ ] 双指旋转与缩放互不干扰——10 次连续操作中 0 次错误切换
- [ ] InputBlocker 生效时 0 个手势事件泄漏到游戏层
- [ ] InputFilter 白名单外的手势 0 事件、0 反馈（视觉/音频/触觉）
- [ ] 手势识别延迟 < 0.5ms（Unity Profiler 验证，TR-input-016）
- [ ] 热路径零 GC allocation（Profiler deep profile 验证，boxing 除外）
- [ ] 所有阈值参数从 Luban 配置表加载，代码中无硬编码数值
- [ ] Blocker token 泄漏检测：push 后 30s 未 pop 触发 Debug.LogWarning
- [ ] Fat finger compensation 在 5.4" 至 6.7" 屏幕上效果可接受（主观评审）
- [ ] 应用切后台再切回后无残留触摸状态

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| `design/gdd/input-system.md` | Input System | TR-input-001: 三层架构（Raw Input / Gesture Recognition / Event Dispatch） | 自定义三层架构完全对齐 GDD 设计 |
| `design/gdd/input-system.md` | Input System | TR-input-002 ~ 006: 5 种手势识别（Tap / Drag / Rotate / Pinch / LightDrag） | GestureType 枚举覆盖全部 5 种，每种手势有独立 GameEvent ID |
| `design/gdd/input-system.md` | Input System | TR-input-007 ~ 008: 手势生命周期（Began / Updated / Ended） | GesturePhase 枚举 + 每个 FSM 状态转换时发出对应 phase 事件 |
| `design/gdd/input-system.md` | Input System | TR-input-009 ~ 010: InputBlocker 栈式全量阻断 | `PushBlocker(token)` / `PopBlocker(token)` 栈 API，非空即阻断 |
| `design/gdd/input-system.md` | Input System | TR-input-011 ~ 013: InputFilter 白名单过滤 | `PushFilter(allowedGestures)` / `PopFilter()` API，单一激活覆盖 |
| `design/gdd/input-system.md` | Input System | TR-input-014: Blocker > Filter > Normal 优先级链 | Layer 2 检查顺序硬编码为 Blocker → Filter → Pass-through |
| `design/gdd/input-system.md` | Input System | TR-input-015: 双指手势互斥 | FSM 进入 Rotating/Pinching 后锁定，直到手指全部抬起 |
| `design/gdd/input-system.md` | Input System | TR-input-016: 手势识别 < 0.5ms/帧 | O(1) 算法 + Profiler marker 保障 |
| `design/gdd/input-system.md` | Input System | TR-input-017: 所有阈值配置化 | 全部阈值来自 Luban 配置表 |
| `design/gdd/tutorial-onboarding.md` | Tutorial | TR-tutor-002: InputFilter 限制教学步骤手势 | Tutorial 通过 `IInputService.PushFilter()` 设置白名单 |
| `design/gdd/narrative-event-system.md` | Narrative | TR-narr-004: 演出期间 InputBlocker 阻断 | Narrative 序列开始时 `PushBlocker("Narrative_XXX")`，结束时 Pop |

## Related

- **Enables**: ADR-013 (Object Interaction) — 消费本 ADR 定义的 5 种手势事件
- **Enables**: ADR-019 (Tutorial InputFilter) — 使用本 ADR 的 `PushFilter` / `PopFilter` API
- **Architectural context**: `docs/architecture/architecture.md` — Input System 位于 Foundation Layer
- **GDD source**: `src/MyGame/ShadowGame/design/gdd/input-system.md`
- **Technical preferences**: `.claude/docs/technical-preferences.md` — 确认使用 Unity legacy Touch API
