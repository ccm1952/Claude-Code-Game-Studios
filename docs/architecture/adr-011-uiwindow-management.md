// 该文件由Cursor 自动生成

# ADR-011: UIWindow Management & Layer Strategy

## Status

Proposed

## Date

2026-04-22

## Last Verified

2026-04-22

## Decision Makers

Technical Director, Lead Programmer, Game Designer

## Summary

《影子回忆 (Shadow Memory)》拥有 9 个已识别的 UI 面板，分布在 5 个层级中。本 ADR 确立基于 TEngine 6.0 UIModule 的 UI 管理策略——所有面板通过 `GameModule.UI.ShowWindow<T>()` / `CloseWindow<T>()` 管理生命周期，5 层级排序系统自动分配 sorting order，Popup 和 Overlay 层自动推入 InputBlocker，弹窗队列限制同时仅显示 1 个 Popup，安全区适配通过 TEngine `SetUISafeFitHelper` 实现，所有 UI prefab 通过 YooAsset 异步加载。

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.62f2 (LTS) |
| **Domain** | UI / UGUI |
| **Knowledge Risk** | MEDIUM — TEngine 6.0 UIModule API（`ShowWindow`、`CloseWindow`、layer registration）可能不在 LLM 训练数据中；必须从项目源码验证 |
| **References Consulted** | Project source (`TEngine/` directory), `docs/engine-reference/unity/VERSION.md`, `.claude/docs/technical-preferences.md`, ADR-001 (TEngine Framework) |
| **Post-Cutoff APIs Used** | `GameModule.UI.ShowWindow<T>()`, `GameModule.UI.CloseWindow<T>()`, TEngine `UIWindow` / `UIWidget` lifecycle callbacks, `SetUISafeFitHelper` |
| **Verification Required** | Sprint 0 spike: confirm UIWindow lifecycle callback ordering (OnCreate → OnRefresh on first open), layer sorting order assignment, `SetUISafeFitHelper` behavior on notched devices |

> **Note**: If Knowledge Risk is MEDIUM or HIGH, this ADR must be re-validated if the
> project upgrades engine versions. Flag it as "Superseded" and write a new ADR.

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-001 (TEngine 6.0 Framework — UIModule, GameEvent, GameModule.Resource) |
| **Enables** | ADR-020 (Accessibility — font scaling, contrast modes in UI), ADR-022 (I2 Localization in UI text) |
| **Blocks** | All 9 UI panel implementations; any system that opens/closes UI windows |
| **Ordering Note** | ADR-001 must reach Accepted status first. This ADR's own acceptance depends on Sprint 0 UIWindow lifecycle spike completion. |

## Context

### Problem Statement

《影子回忆》的 9 个 UI 面板需要一套统一的管理策略来解决以下问题：

1. **层级混乱风险**：9 个面板分布在 5 个层级（Background、HUD、Popup、Overlay、System），手动管理 sorting order 容易产生 z-fighting 和遮挡错误
2. **输入泄漏风险**：当 Popup 或 Overlay 打开时，底层 HUD 和游戏场景仍可接收输入——需要自动推入 InputBlocker 阻断底层输入
3. **弹窗冲突**：PauseMenuPanel、SettingsPanel、ChapterSelectPanel 三个 Popup 可能被同时请求打开，需要队列化管理
4. **移动端适配**：iPhone X 及以上刘海屏设备需要安全区适配，UI 不能被遮挡
5. **资源加载一致性**：所有 UI prefab 必须通过 YooAsset 异步加载（`GameModule.Resource.LoadAssetAsync`），禁止 `Resources.Load`
6. **生命周期不规范**：没有统一的 create/refresh/update/close 生命周期，各面板各自管理初始化和清理逻辑

不解决这些问题，UI 面板实现将各自为政，导致输入泄漏、层级错乱、资源泄漏等问题在后期难以排查。

### Current State

项目处于 Pre-Production 阶段。TEngine 6.0 UIModule 已存在于项目源码中，ADR-001 已确立 TEngine 作为唯一框架。13 个系统 GDD 中有 6 个直接引用 UI 面板（UI System、Settings/Accessibility、Tutorial、Hint、Chapter/Save、Narrative），但尚无任何 UI 面板代码实现。

### Constraints

- **框架锁定**：ADR-001 确定使用 TEngine UIModule，所有 UI 必须继承 `UIWindow`/`UIWidget`
- **UGUI 锁定**：TEngine UIModule 围绕 UGUI 构建，不支持 UI Toolkit
- **异步加载**：所有资源加载必须通过 YooAsset 异步 API（ADR-001, ADR-005）
- **输入系统集成**：InputBlocker push/pop 必须通过 `IInputService`（ADR-010）
- **事件通信**：跨系统通信必须通过 `GameEvent` `[EventInterface]` C# 接口（ADR-001, **ADR-027** — supersedes ADR-006 §1/§2）。UI 事件使用 `GroupUI` 分组的接口，业务事件使用 `GroupLogic` 分组的接口
- **移动端优先**：iOS/Android 为主目标平台，安全区适配为必选项
- **性能预算**：16.67ms 帧预算（60 FPS），UI prefab 实例化不可阻塞主线程

### Requirements

- **FR-1**: 9 个面板通过 `GameModule.UI.ShowWindow<T>()` / `CloseWindow<T>()` 统一管理
- **FR-2**: 5 层级排序系统，每层分配 100 间隔的 sorting order base
- **FR-3**: Popup 和 Overlay 层面板打开时自动推入 InputBlocker，关闭时自动弹出
- **FR-4**: Popup 队列——同一时间最多 1 个 Popup 可见，多余的进入 FIFO 队列
- **FR-5**: 安全区适配通过 `SetUISafeFitHelper` 应用到根 Canvas
- **FR-6**: UI prefab 全部通过 `GameModule.Resource.LoadAssetAsync` 异步加载
- **FR-7**: UIWindow 生命周期严格遵循 OnCreate → OnRefresh → OnUpdate → OnClose
- **FR-8**: 面板内部交互使用 `UIWindow.AddUIEvent()`；跨系统事件通过 `GameEvent`
- **PR-1**: UI prefab 实例化必须为异步，不阻塞主线程
- **PR-2**: 仅可见 UIWindow 接收 OnUpdate 调用

## Decision

**使用 TEngine 6.0 UIModule 作为唯一 UI 管理方案。** 所有 9 个面板继承 `UIWindow`，通过 5 层级排序、自动 InputBlocker、Popup 队列和安全区适配实现统一管理。

### Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      Game Systems Layer                         │
│  (Shadow Puzzle, Narrative, Chapter, Tutorial, Settings, etc.)  │
│              ↓ ShowWindow / CloseWindow                         │
├─────────────────────────────────────────────────────────────────┤
│                  UIManager (TEngine UIModule)                    │
│  GameModule.UI.ShowWindow<T>()  /  CloseWindow<T>()             │
│                                                                 │
│  ┌───────────────── Layer Stack ──────────────────────┐         │
│  │  Layer 4: System  (400)  — SaveIndicatorPanel      │         │
│  │  Layer 3: Overlay (300)  — ChapterTransition,      │         │
│  │                            MemoryReplay            │         │
│  │  Layer 2: Popup   (200)  — Pause, Settings,        │         │
│  │                            ChapterSelect           │         │
│  │  Layer 1: HUD     (100)  — HUDPanel, Tutorial,     │         │
│  │                            HintPanel               │         │
│  │  Layer 0: Background (0) — (reserved)              │         │
│  └────────────────────────────────────────────────────┘         │
│                                                                 │
│  ┌── Auto InputBlocker ──┐  ┌── Popup Queue ───────┐           │
│  │ Popup → auto push     │  │ Max 1 visible popup  │           │
│  │ Overlay → auto push   │  │ FIFO queue for rest  │           │
│  │ Others → no blocker   │  │ Auto-dequeue on close│           │
│  └───────────────────────┘  └──────────────────────┘           │
├─────────────────────────────────────────────────────────────────┤
│               UIWindow Lifecycle (per panel)                    │
│  OnCreate (first open) → OnRefresh (each show) →               │
│  OnUpdate (per frame, visible only) → OnClose (destroy)        │
├─────────────────────────────────────────────────────────────────┤
│  Safe Area: SetUISafeFitHelper on root Canvas                   │
├───────────────────────┬─────────────────────────────────────────┤
│   YooAsset (prefab)   │   IInputService (ADR-010)               │
│   async load/unload   │   PushBlocker / PopBlocker              │
├───────────────────────┴─────────────────────────────────────────┤
│                    TEngine 6.0 UIModule                          │
├─────────────────────────────────────────────────────────────────┤
│                   Unity 2022.3.62f2 (UGUI)                      │
└─────────────────────────────────────────────────────────────────┘
```

### 9 UIWindow Panel Registry

| Panel | Layer | Sort Order Base | InputBlocker | Notes |
|-------|-------|:---------------:|:------------:|-------|
| HUDPanel | HUD (1) | 100 | No | 游戏中始终可见 |
| TutorialPromptPanel | HUD (1) | 100 | No | 使用 InputFilter 代替 Blocker（仅允许教学指定手势） |
| HintPanel | HUD (1) | 100 | No | 提示面板，不阻断输入 |
| PauseMenuPanel | Popup (2) | 200 | Auto-push | 暂停菜单，阻断底层输入 |
| SettingsPanel | Popup (2) | 200 | Auto-push | 设置面板，阻断底层输入 |
| ChapterSelectPanel | Popup (2) | 200 | Auto-push | 章节选择，阻断底层输入 |
| ChapterTransitionPanel | Overlay (3) | 300 | Auto-push | 全屏章节过渡 |
| MemoryReplayPanel | Overlay (3) | 300 | Auto-push | 全屏记忆回放 |
| SaveIndicatorPanel | System (4) | 400 | No | 存档指示器，不阻断输入 |

### 5 UI Layer Levels

| Level | Name | Sort Order Base | Purpose | Auto InputBlocker |
|:-----:|------|:---------------:|---------|:-----------------:|
| 0 | Background | 0 | 背景元素（预留） | No |
| 1 | HUD | 100 | 游戏 HUD，始终可见 | No |
| 2 | Popup | 200 | 模态弹窗（Pause、Settings、ChapterSelect） | Yes |
| 3 | Overlay | 300 | 全屏覆盖层（Transitions、Replays） | Yes |
| 4 | System | 400 | 系统指示器（Save、Loading） | No |

### Key Interfaces

```csharp
// === UIWindow 基类使用模式 ===

public class HUDPanel : UIWindow
{
    // OnCreate: 首次打开时调用，绑定 UI 引用
    protected override void OnCreate()
    {
        // FindChild / GetComponent for UI references
        // AddUIEvent() for internal widget events
    }

    // OnRefresh: 每次显示时调用（包括首次 OnCreate 之后）
    protected override void OnRefresh()
    {
        // Update display data, refresh counts/status
    }

    // OnUpdate: 每帧调用（仅在可见时）
    protected override void OnUpdate()
    {
        // Per-frame UI logic (animations, timers)
    }

    // OnClose: 关闭/销毁时调用
    protected override void OnClose()
    {
        // Cleanup: unsubscribe events, release references
    }
}

// === Show / Close API ===

// 打开面板
GameModule.UI.ShowWindow<PauseMenuPanel>();

// 关闭面板
GameModule.UI.CloseWindow<PauseMenuPanel>();

// === InputBlocker 自动管理（在 UIWindow 包装层中实现） ===

// Popup/Overlay 层面板 Show 时自动调用:
//   IInputService.PushBlocker("UIPanel_PauseMenuPanel");
// Close 时自动调用:
//   IInputService.PopBlocker("UIPanel_PauseMenuPanel");

// Blocker token 命名规范: "UIPanel_{PanelClassName}"

// === Popup Queue ===

// 当已有 Popup 可见时，新 Popup 请求进入 FIFO 队列
// 当前 Popup 关闭时，自动 dequeue 并显示下一个
// Overlay 不受 Popup 队列限制（Overlay 可与 Popup 共存）

// === 事件通信 ===

// 面板内部 widget 事件
AddUIEvent(buttonId, OnButtonClicked);

// 跨系统事件（通过 GameEvent）
GameEvent.AddEventListener(EventId.ChapterCompleted, OnChapterCompleted);
GameEvent.RemoveEventListener(EventId.ChapterCompleted, OnChapterCompleted);
```

### Implementation Guidelines

**1. Layer 注册与 Sorting Order**

每个 UIWindow 子类在定义时声明所属 layer level。UIModule 根据 layer level 的 sort order base（0/100/200/300/400）自动分配 Canvas sorting order。同层级内多个面板的 sorting order 按打开顺序递增（+1），保证后打开的面板在上层。

**2. Auto InputBlocker 机制**

在 UIWindow 的 Show/Close 包装层中实现自动 InputBlocker 逻辑：

- Show 时：检查面板所属 layer，若为 Popup(2) 或 Overlay(3)，调用 `IInputService.PushBlocker("UIPanel_{ClassName}")`
- Close 时：检查面板所属 layer，若为 Popup(2) 或 Overlay(3)，调用 `IInputService.PopBlocker("UIPanel_{ClassName}")`
- Token 格式统一为 `"UIPanel_{ClassName}"`，确保 push/pop 配对

特殊情况：TutorialPromptPanel 虽在 HUD 层（不自动推 Blocker），但通过 `IInputService.PushFilter()` 实现白名单过滤（ADR-010 / ADR-019）。

**3. Popup Queue 实现**

- 维护 `Queue<Type>` 作为待显示 Popup 队列
- `ShowWindow<T>()` 调用时，检查当前是否有 Popup 层面板可见：
  - 若无 → 直接显示
  - 若有 → 入队，等待当前 Popup 关闭
- `CloseWindow<T>()` 关闭 Popup 后，检查队列是否非空：
  - 若非空 → dequeue 并显示下一个
- Overlay 层不受 Popup 队列限制——Overlay 和 Popup 可以同时存在（如 ChapterTransition 覆盖在 PauseMenu 之上）

**4. Safe Area 适配**

- 在根 Canvas 上调用 TEngine `SetUISafeFitHelper`
- 确保所有 UI 内容不延伸到刘海区、底部指示条区域
- System 层面板（SaveIndicatorPanel）可配置为忽略安全区（显示在角落）

**5. Prefab 异步加载**

- 所有 UI prefab 通过 `GameModule.Resource.LoadAssetAsync<GameObject>()` 加载
- 首次 `ShowWindow<T>()` 触发异步加载 → 实例化 → OnCreate → OnRefresh
- 再次 `ShowWindow<T>`（已加载过）→ 直接 OnRefresh（TEngine 内部管理实例缓存）
- `CloseWindow<T>()` 默认隐藏实例（不销毁），频繁面板（HUDPanel）保持池化
- `Resources.Load` 禁止使用（ADR-001）

**6. UIWindow 生命周期约定**

| Callback | 触发时机 | 典型用途 |
|----------|---------|---------|
| `OnCreate()` | 首次打开，prefab 实例化后 | 绑定 UI 引用、注册内部事件 |
| `OnRefresh()` | 每次显示时（含首次 OnCreate 之后） | 刷新数据、更新显示状态 |
| `OnUpdate()` | 每帧（仅可见时） | 动画、倒计时等帧级逻辑 |
| `OnClose()` | 关闭/销毁时 | 注销事件、释放引用、清理状态 |

> **Open Question**: TEngine `UIWindow` 在首次打开时的回调顺序——本 ADR 假设为 `OnCreate → OnRefresh`。需要 Sprint 0 spike 从 TEngine 6.0 源码验证实际行为。如果实际为 `OnCreate` only（首次不调用 `OnRefresh`），需在 `OnCreate` 末尾手动调用刷新逻辑。

**7. 事件通信规范**

- **面板内部**：使用 `UIWindow.AddUIEvent<TEvent>(handler)` 处理按钮点击等交互（自动随面板生命周期清理）
- **跨系统通信**：使用 `GameEvent.Get<IXxxEvent>().OnYyy(payload)` 发送、`GameEvent.AddEventListener<IXxxEvent_Event>(handler)` 监听（ADR-027）
  - 面板打开/关闭事件：`GameEvent.Get<IUILifecycleEvent>().OnPanelOpened(panelType)`（`[EventInterface(EEventGroup.GroupUI)]`）
  - 游戏状态变更：监听 `IChapterStateEvent_Event.OnChapterCompleted` 等接口方法刷新 UI
- 禁止面板之间直接引用——通过 GameEvent 接口解耦

**8. UI GameEvent 接口（遵循 ADR-027）**

```csharp
// 文件: Assets/GameScripts/HotFix/GameLogic/IEvent/IUILifecycleEvent.cs
using TEngine;

namespace GameLogic
{
    [EventInterface(EEventGroup.GroupUI)]
    public interface IUILifecycleEvent
    {
        void OnPanelOpened(UIPanelType panelType);
        void OnPanelClosed(UIPanelType panelType);
        void OnPopupQueued(PopupRequest request);
        void OnPopupDequeued(PopupRequest request);
    }

    // 每个具体面板的开关事件：
    // I{PanelName}UI.Show{PanelName}UI(...) / Close{PanelName}UI()
    [EventInterface(EEventGroup.GroupUI)]
    public interface ILoginUI
    {
        void ShowLoginUI();
        void CloseLoginUI();
    }
}
```

> **注意**: 本 ADR 不再定义独立的 `UIEventId` 常量类。所有 UI 事件以 `[EventInterface(EEventGroup.GroupUI)]` 接口形式声明；Event ID 由 TEngine Roslyn Source Generator 自动生成。legacy `Evt_Panel*` 映射见 `architecture-traceability.md` 附录 A。

## Alternatives Considered

### Alternative 1: TEngine UIModule (chosen)

- **Description**: 使用 TEngine 6.0 内置 UIModule，基于 `UIWindow`/`UIWidget` 继承体系，`GameModule.UI` 统一管理
- **Pros**: 与 TEngine 框架深度集成；内置生命周期管理（OnCreate/OnRefresh/OnUpdate/OnClose）；layer sorting 机制已内建；与 YooAsset 资源加载无缝对接；团队无需学习额外框架
- **Cons**: 锁定于 TEngine 的 UIWindow 模式；OnCreate/OnRefresh 回调时序需验证；UGUI 在复杂面板上性能不如 UI Toolkit
- **Estimated Effort**: 1x（基线）
- **Selection Reason**: ADR-001 已确定 TEngine 为项目框架，UIModule 是自然延伸。InputBlocker 自动管理和 Popup 队列作为薄层封装即可实现

### Alternative 2: Custom UI Manager on Raw UGUI

- **Description**: 不使用 TEngine UIModule，基于 UGUI Canvas 自建 UI 管理器，包括生命周期管理、layer sorting、prefab 加载
- **Pros**: 完全控制 UI 管理逻辑；不受 TEngine UIWindow 约束；可精确定制生命周期行为
- **Cons**: 需要重新实现 TEngine UIModule 已提供的所有功能（生命周期、layer sorting、prefab 池化）；与 TEngine 其他模块（Resource、Event）集成需额外工作；违反 ADR-001 统一框架原则；估计 3-4 周额外开发
- **Estimated Effort**: 3x
- **Rejection Reason**: ADR-001 已确定 TEngine 为唯一框架，自建 UI 管理器等于放弃 TEngine UIModule 的所有内建能力。额外开发成本与 indie 团队时间预算不符

### Alternative 3: UI Toolkit (Unity)

- **Description**: 使用 Unity 官方 UI Toolkit（USS/UXML），结合 C# 数据绑定构建响应式 UI
- **Pros**: 现代 CSS-like 样式系统；性能优于 UGUI 在复杂布局场景；Unity 官方长期支持方向；强类型数据绑定
- **Cons**: TEngine UIModule 完全围绕 UGUI 构建，无法兼容 UI Toolkit；采用 UI Toolkit 意味着放弃 TEngine UIModule；Unity 2022.3 中 UI Toolkit runtime 功能仍不完整（缺少部分移动端特性）；团队需要学习全新 UI 范式
- **Estimated Effort**: 4x（需重建整个 UI 基础设施）
- **Rejection Reason**: 与 TEngine 框架不兼容是致命问题。UI Toolkit 在 Unity 2022.3 LTS 中的 runtime 支持尚未完全成熟，且本项目 UI 复杂度（9 个面板）不足以证明迁移成本合理

### Alternative 4: Third-Party UI Framework (MVVM/MVC)

- **Description**: 集成第三方 MVVM 或 MVC UI 框架（如 Loxodon Framework、UIMan），在架构模式层面管理 UI
- **Pros**: 强制数据绑定和视图分离；便于单元测试 ViewModel；适合复杂数据驱动 UI
- **Cons**: 与 TEngine UIWindow 模式冲突——两套生命周期管理共存会造成混乱；引入外部依赖；本项目 9 个面板复杂度较低，MVVM 架构收益有限；团队需要学习额外模式
- **Estimated Effort**: 2.5x
- **Rejection Reason**: 与 TEngine UIWindow 生命周期模式直接冲突。9 个面板的规模不足以证明 MVVM 架构的引入成本。简单场景使用简单方案

## Consequences

### Positive

- **统一生命周期**：所有 9 个面板遵循相同的 OnCreate → OnRefresh → OnUpdate → OnClose 流程，减少遗漏清理导致的 bug
- **自动 InputBlocker**：Popup 和 Overlay 层面板自动管理 InputBlocker push/pop，消除手动管理的输入泄漏风险
- **层级排序确定性**：5 层级 × 100 间隔的 sorting order 系统消除 z-fighting，后打开面板自动在上层
- **弹窗队列**：同一时间最多 1 个 Popup 可见，避免多个弹窗叠加造成的 UX 混乱
- **异步加载**：UI prefab 通过 YooAsset 异步加载，不阻塞主线程，支持 hot-update
- **安全区覆盖**：`SetUISafeFitHelper` 统一处理所有刘海屏适配，无需每个面板单独处理
- **与 ADR-010 集成**：InputBlocker token 命名规范（`"UIPanel_{ClassName}"`）与 ADR-010 定义的 `IInputService` API 自然对接

### Negative

- **锁定 TEngine UIWindow 模式**：所有面板必须继承 `UIWindow`，无法使用纯代码生成 UI 或 UI Toolkit
- **OnCreate/OnRefresh 时序假设风险**：本 ADR 假设首次打开时 `OnCreate → OnRefresh` 顺序调用——如实际行为不同，需修改每个面板的初始化逻辑
- **UGUI 性能天花板**：如后期面板复杂度增加（大量动态列表、复杂布局），UGUI rebatch 可能成为瓶颈
- **Popup 队列可能延迟关键信息**：如果 PauseMenu 打开时触发 SettingsPanel 请求，Settings 会被排队——需确保 UX 层面可接受

### Neutral

- Popup 队列仅影响 Popup 层（Level 2），Overlay 和 System 层面板不受队列限制
- TutorialPromptPanel 在 HUD 层但使用 InputFilter 而非 InputBlocker——这是设计意图，与 ADR-010/ADR-019 对齐
- Background 层（Level 0）当前无面板使用，预留给未来背景 UI 元素

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| UIWindow OnCreate/OnRefresh 回调顺序与假设不符 | MEDIUM | HIGH | Sprint 0 spike 从 TEngine 6.0 源码验证；如不符，在 OnCreate 末尾手动调用刷新逻辑 |
| InputBlocker 自动 push/pop 配对失败（关闭面板时 token 不匹配） | LOW | HIGH | Token 使用固定格式 `"UIPanel_{ClassName}"`，在 auto-pop 时强制使用相同 token；开发阶段添加 Debug.LogWarning 泄漏检测（ADR-010 已定义 30s 超时报警） |
| Popup 队列导致重要提示被延迟显示 | LOW | MEDIUM | UX 设计时确保不会在 Popup 打开状态下触发另一个关键 Popup；System 层面板（SaveIndicator）不受队列影响 |
| 安全区适配在部分 Android 厂商定制 ROM 上失效 | LOW | MEDIUM | `SetUISafeFitHelper` + 手动 `Screen.safeArea` fallback；上线前在 5+ Android 设备上验证 |
| UGUI Canvas rebatch 在复杂面板上导致帧耗突增 | LOW | MEDIUM | UI 设计遵循 UGUI 优化原则：减少 Canvas 层级嵌套、动静分离、合理 atlas 分组；Profiler 监控 Canvas.BuildBatch |
| 异步 prefab 加载首次打开面板时出现可感知延迟 | MEDIUM | LOW | 高频面板（HUDPanel）在场景加载时预加载；低频面板（Settings）接受首次短暂延迟（< 200ms） |

## Performance Implications

| Metric | Before (No UI) | Expected After | Budget |
|--------|---------------|---------------|--------|
| CPU (frame time) — UI Update | 0ms | < 0.5ms（仅可见面板 OnUpdate） | 16.67ms total |
| Memory — UI instances | 0 | ~5-15 MB（9 个面板 prefab + Canvas + textures） | 1,500 MB mobile ceiling |
| Load Time — First panel open | N/A | < 200ms（async prefab load + instantiate） | Non-blocking |
| GC Allocation — per frame | 0 | ~0（OnUpdate 无分配；事件分发可能 1 次装箱） | Minimize |

**性能保障措施：**

- 仅可见 UIWindow 接收 OnUpdate 调用，隐藏面板零 CPU 消耗
- 高频面板（HUDPanel）在场景加载时预加载并常驻内存，避免反复加载/卸载
- 低频面板（Settings、ChapterSelect）首次打开时异步加载，关闭时隐藏（不销毁），可配置为卸载释放内存
- UGUI Canvas 动静分离：频繁更新的 UI 元素（计时器、动画）放在独立 sub-Canvas，减少 rebatch 范围
- Profiler marker `UIManager.Update` 标记，便于帧级监控

## Migration Plan

本系统为新建，无需迁移现有代码。

1. **Step 1**: 定义 UILayer 枚举和 sorting order 映射配置
2. **Step 2**: 实现 UIWindow 包装层——在 Show/Close 中注入 auto InputBlocker 逻辑
3. **Step 3**: 实现 Popup Queue 管理器（`Queue<Type>` + dequeue-on-close hook）
4. **Step 4**: 配置根 Canvas + `SetUISafeFitHelper` 安全区适配
5. **Step 5**: 创建 9 个 UIWindow 子类骨架（空面板 + 正确 layer 注册）
6. **Step 6**: 逐个面板实现 OnCreate/OnRefresh/OnUpdate/OnClose 逻辑
7. **Step 7**: 集成 InputBlocker 自动管理与 ADR-010 `IInputService` 验证
8. **Step 8**: 真机测试安全区适配（iPhone X/11/12/13/14/15 + Android 刘海屏设备）

**Rollback plan**: 如 Sprint 0 spike 发现 TEngine UIModule 无法满足 layer sorting 或 lifecycle 需求，可回退到 Alternative 2（Custom UI Manager on Raw UGUI），但需承担 3x 开发成本。应在 Sprint 0 结束前做出判定。

## Validation Criteria

- [ ] 全部 9 个面板在各自指定层级正确显示，无 z-fighting
- [ ] 同层级多面板打开时，后打开面板 sorting order 更高（显示在上层）
- [ ] Popup 队列正确工作：打开第二个 Popup 时自动排队，前一个关闭后自动显示下一个
- [ ] InputBlocker 自动管理验证：打开 PauseMenuPanel → 底层游戏手势完全阻断 → 关闭 PauseMenu → 游戏手势恢复
- [ ] InputBlocker 多层叠加验证：Popup + Overlay 同时打开 → 两个 Blocker 同时存在 → 逐个关闭 → 最后一个关闭后 Blocker 栈为空
- [ ] TutorialPromptPanel 使用 InputFilter（非 Blocker）——仅允许教学指定手势
- [ ] 安全区适配正确：iPhone X/11/12/13/14/15 刘海屏设备上 UI 不被遮挡
- [ ] UI prefab 全部通过 YooAsset 异步加载，无 `Resources.Load` 调用
- [ ] UIWindow 生命周期回调顺序验证（Sprint 0 spike）：首次打开 OnCreate → OnRefresh，再次打开仅 OnRefresh
- [ ] SaveIndicatorPanel（System 层）可在任何 Popup/Overlay 之上显示
- [ ] 面板关闭后无事件泄漏（GameEvent listener 在 OnClose 中正确注销）
- [ ] HUDPanel 预加载后首次显示无可感知延迟

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| `design/gdd/ui-system.md` | UI | TR-ui-001: All UI via TEngine UIModule | 所有 9 个面板通过 `GameModule.UI.ShowWindow<T>()` / `CloseWindow<T>()` 管理 |
| `design/gdd/ui-system.md` | UI | TR-ui-002 ~ 006: 5 UI layer levels | 5 层级排序系统（Background/HUD/Popup/Overlay/System），sort order base 0/100/200/300/400 |
| `design/gdd/ui-system.md` | UI | TR-ui-007 ~ 015: 9 UI panels | 全部 9 个面板注册到正确层级，InputBlocker 和 InputFilter 配置如表 |
| `design/gdd/ui-system.md` | UI | TR-ui-016: Popup queue (max 1 visible) | FIFO 队列管理，同时仅 1 个 Popup 可见 |
| `design/gdd/ui-system.md` | UI | TR-ui-017: Safe area handling | `SetUISafeFitHelper` 应用到根 Canvas |
| `design/gdd/ui-system.md` | UI | TR-ui-018: Auto InputBlocker for Popup/Overlay | Popup(2) 和 Overlay(3) 层自动 push/pop InputBlocker |
| `design/gdd/ui-system.md` | UI | TR-ui-019: UI prefab async loading | 全部通过 `GameModule.Resource.LoadAssetAsync` 加载，禁止 `Resources.Load` |
| `design/gdd/ui-system.md` | UI | TR-ui-020: UIWindow lifecycle | OnCreate → OnRefresh → OnUpdate → OnClose 标准生命周期 |
| `design/gdd/ui-system.md` | UI | TR-ui-021: UI event communication | 内部事件 `AddUIEvent()`，跨系统事件 `GameEvent` |
| `design/gdd/input-system.md` | Input | TR-input-004: InputBlocker push/pop by UI panels | UI 面板 auto InputBlocker 使用 ADR-010 定义的 `IInputService.PushBlocker/PopBlocker` |
| `design/gdd/settings-accessibility.md` | Settings | Settings UI management | SettingsPanel 在 Popup 层，通过 UIWindow 生命周期管理 |
| `design/gdd/tutorial-onboarding.md` | Tutorial | Tutorial UI overlay | TutorialPromptPanel 在 HUD 层，使用 InputFilter（非 Blocker） |
| `design/gdd/hint-system.md` | Hint | Hint display panel | HintPanel 在 HUD 层，不阻断输入 |
| `design/gdd/chapter-state-and-save.md` | Chapter | Chapter transition UI | ChapterTransitionPanel 在 Overlay 层，auto InputBlocker |

## Related

- **Depends On**: ADR-001 (TEngine 6.0 Framework) — UIModule 是 TEngine 核心模块
- **Integrates With**: ADR-010 (Input Abstraction) — auto InputBlocker 使用 `IInputService.PushBlocker/PopBlocker`
- **Enables**: ADR-020 (Accessibility) — font scaling、contrast modes 需在 UIWindow 面板中实现
- **Enables**: ADR-022 (I2 Localization) — UI 文本本地化需在 UIWindow 面板中集成
- **Architectural context**: `docs/architecture/architecture.md` — UI System 位于 Core Layer
- **GDD source**: `src/MyGame/ShadowGame/design/gdd/ui-system.md`
- **Technical preferences**: `.claude/docs/technical-preferences.md` — 确认使用 UGUI + TEngine UIModule
