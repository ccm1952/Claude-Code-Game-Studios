// 该文件由Cursor 自动生成

# Story: UIModule Initialization + UIWindow Base Class Setup

> **Epic**: ui-system
> **Story ID**: ui-system-001
> **Story Type**: Logic
> **GDD Requirement**: TR-ui-001 (All UI via TEngine UIModule), TR-ui-002 (5 UI layer levels)
> **ADR References**: ADR-011 (UIWindow Management), ADR-001 (TEngine Framework), SP-002 (UIWindow Lifecycle)
> **Sprint**: TBD
> **Status**: Ready

## Context

UI System 的基础骨架。本 story 建立：UILayer 枚举（5 层）与 sorting order 映射、UIWindow 基类使用规范的代码文档、Popup 队列管理器的初始化。UIModule 本身由 TEngine 提供，位于 `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/`（GameLogic 热更程序集，非 TEngine Runtime）。

本 story **不实现任何具体面板**，只建立所有面板共用的基础约定和 Popup Queue 骨架。

## Acceptance Criteria

- [ ] `UILayer` 枚举定义：`Background = 0`, `HUD = 1`, `Popup = 2`, `Overlay = 3`, `System = 4`，每层 sorting order base = `layer × 100`
- [ ] Popup Queue Manager 实现：`Queue<Type>` 维护待显示 Popup 队列，`ShowWindow<T>()` 时若已有 Popup 可见则入队
- [ ] Popup Queue Auto-Dequeue：Popup 面板关闭时，自动 dequeue 下一个 Popup 并调用 `ShowWindow`
- [ ] Auto InputBlocker 机制：`ShowWindow<T>()` 包装层检测目标面板的 layer；若为 Popup(2) 或 Overlay(3)，自动 `IInputService.PushBlocker("UIPanel_{ClassName}")`；`CloseWindow<T>()` 时自动 `PopBlocker`
- [ ] InputBlocker token 命名规范：`"UIPanel_{PanelClassName}"`（与 ADR-010 对齐）
- [ ] Overlay 层面板不受 Popup 队列限制（可与 Popup 共存）
- [ ] `UIWindow` 生命周期文档注释：`OnCreate`（首次）/ `OnRefresh`（每次打开）/ `OnUpdate`（可见帧）/ `OnClose`（清理），与 SP-002 确认的时序一致
- [ ] UIModule 所在程序集路径记录在代码注释中（防止团队错误地在 TEngine Runtime 中查找）
- [ ] 单元测试：Popup Queue 工作——打开第二个 Popup 时入队，第一个关闭后自动显示第二个

## Implementation Notes

- UIModule 路径：`Assets/GameScripts/HotFix/GameLogic/Module/UIModule/`（GameLogic 热更程序集）
- 禁止使用 `ModuleSystem.GetModule<T>()`，所有访问通过 `GameModule.UI`
- Popup Queue Manager 可作为 UIModule 扩展类（partial class）或独立 helper，避免修改 TEngine 核心代码
- Auto InputBlocker 通过 UIModule 的 Show/Close 回调点注入，或通过 UIWindow 基类 override 实现
- `IInputService.PushBlocker` / `PopBlocker` 对应 ADR-010 定义的接口

## Out of Scope

- 任何具体 UIWindow 面板实现（由 story-002 到 story-007 负责）
- Safe area 适配（story-009）
- 本地化绑定（story-010）

## QA Test Cases

### TC-001: UILayer Sorting Order 映射
- **Given**: UILayer 枚举初始化
- **When**: 查询各层 sorting order base
- **Then**: Background=0, HUD=100, Popup=200, Overlay=300, System=400

### TC-002: Popup Queue 入队行为
- **Given**: PauseMenuPanel 已打开（Popup 层可见）
- **When**: `GameModule.UI.ShowWindow<SettingsPanel>()` 调用
- **Then**: SettingsPanel 进入队列，不立即显示；PauseMenu 仍可见

### TC-003: Popup Queue Auto-Dequeue
- **Given**: SettingsPanel 在队列中等待，PauseMenuPanel 可见
- **When**: `GameModule.UI.CloseWindow<PauseMenuPanel>()` 调用
- **Then**: SettingsPanel 自动显示，Popup 队列清空

### TC-004: Overlay 不受 Popup Queue 限制
- **Given**: PauseMenuPanel 已打开（Popup 层）
- **When**: `GameModule.UI.ShowWindow<ChapterTransitionPanel>()` 调用（Overlay 层）
- **Then**: ChapterTransitionPanel 立即显示，不进入队列

### TC-005: Auto InputBlocker Push/Pop
- **Given**: 无 InputBlocker 激活
- **When**: `ShowWindow<PauseMenuPanel>()` → 验证 Blocker → `CloseWindow<PauseMenuPanel>()` → 验证 Blocker
- **Then**: Show 后 InputBlocker 栈包含 `"UIPanel_PauseMenuPanel"`；Close 后栈清空

## Test Evidence Path

`tests/unit/UIModule_PopupQueue_Test.cs`

## Dependencies

- ADR-001: TEngine Framework（UIModule 是 TEngine 核心模块）
- ADR-010: Input Abstraction（IInputService.PushBlocker/PopBlocker API）
- ADR-011: UIWindow Management（层级和队列设计来源）
- SP-002: UIWindow Lifecycle（OnCreate/OnRefresh 时序验证）
