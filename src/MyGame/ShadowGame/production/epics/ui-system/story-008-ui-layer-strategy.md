// 该文件由Cursor 自动生成

# Story: UIWindow Layer / Order Management (Normal, Popup, Overlay)

> **Epic**: ui-system
> **Story ID**: ui-system-008
> **Story Type**: Logic
> **GDD Requirement**: TR-ui-002 (5 UI layer levels), TR-ui-003 (Popup/Overlay auto InputBlocker), TR-ui-008 (Popup queue 1 visible)
> **ADR References**: ADR-011 (UIWindow Management), ADR-010 (Input Abstraction), SP-002 (UIWindow Lifecycle)
> **Sprint**: TBD
> **Status**: Ready (依赖 ui-system-001)

## Context

本 story 验证 5 层 UI 分级策略的实际运行时行为——包括多面板同层叠加顺序、Popup + Overlay 共存场景、InputBlocker 多层叠加与正确弹出，以及边界情况（快速连续打开/关闭 Popup）。这是 story-001 骨架的功能验证 story，确保所有层管理规则在真实面板组合下正确运行。

## Acceptance Criteria

- [ ] 同层两个面板先后打开：后打开的 sorting order 更高（显示在上层）
- [ ] Popup + Overlay 同时存在：Overlay sorting order > Popup sorting order
- [ ] System 层（SaveIndicatorPanel）可在任何 Popup/Overlay 之上显示（sorting order 最高）
- [ ] 快速连续打开 2 个 Popup：第 2 个进入队列，第 1 个关闭后自动显示第 2 个——无竞争条件
- [ ] Popup 关闭时若队列非空，自动 dequeue 并打开下一个，InputBlocker 保持连续（无间隙）
- [ ] 双 InputBlocker 叠加验证：同时打开 Popup + Overlay → InputBlocker 栈有 2 个 token → 逐个关闭 → 最后一个关闭后栈清空 → 游戏手势恢复
- [ ] HUD 层（HUDPanel）打开时不推入 InputBlocker，游戏层手势始终穿透
- [ ] `CloseAllPopups()` 工具方法（可选）：关闭所有 Popup 层面板并清空队列
- [ ] 面板关闭顺序验证：Overlay 关闭不触发 Popup Queue dequeue

## Implementation Notes

- 本 story 主要是集成测试和规范验证，代码修改集中在 story-001 骨架（如发现 bug）
- Sorting order 分配方式：`layerBase + openOrder`（同层内第 N 个打开的面板 order = base + N）
- 测试使用 Mock UIWindow 子类（不依赖真实面板资产）以加速测试
- InputBlocker 栈内容可通过 `IInputService.GetBlockerStack()` 或等效 debug API 查询

## Out of Scope

- 具体面板的 UI 内容（由其他 story 负责）
- 安全区适配（story-009）

## QA Test Cases

### TC-001: 同层后打开面板在上层
- **Given**: HUDPanel（HUD 层）已打开
- **When**: 另一个 HUD 层面板打开
- **Then**: 新面板 sorting order > HUDPanel sorting order

### TC-002: Popup + Overlay 共存
- **Given**: PauseMenuPanel（Popup 200）打开
- **When**: ChapterTransitionPanel（Overlay 300）打开
- **Then**: ChapterTransitionPanel sorting order > PauseMenuPanel sorting order，两个面板同时可见

### TC-003: 双 InputBlocker 逐个弹出
- **Given**: PauseMenuPanel（Popup）+ ChapterTransitionPanel（Overlay）同时打开
- **When**: CloseWindow<ChapterTransitionPanel>()
- **Then**: InputBlocker 栈剩 1 个 token（PauseMenu 的）；游戏手势仍阻断

### TC-004: Popup Queue 无竞争条件
- **Given**: PauseMenuPanel 打开中
- **When**: 连续调用 ShowWindow<SettingsPanel>() 3 次
- **Then**: SettingsPanel 仅入队 1 次（重复入队忽略或以最后一次为准，不崩溃）

### TC-005: Overlay 关闭不影响 Popup Queue
- **Given**: PauseMenuPanel 可见，SettingsPanel 在 Popup 队列中，ChapterTransitionPanel（Overlay）可见
- **When**: CloseWindow<ChapterTransitionPanel>()
- **Then**: SettingsPanel 仍在队列中，PauseMenuPanel 仍可见（Overlay 关闭不触发 Popup dequeue）

## Test Evidence Path

`tests/unit/UILayer_Strategy_Test.cs`

## Dependencies

- ui-system-001: UIModule 基础（Popup Queue, Auto InputBlocker 实现）
- ADR-010: Input Abstraction（IInputService.PushBlocker/PopBlocker/GetBlockerStack）
- ADR-011: 层级规范和 Popup Queue 规则
