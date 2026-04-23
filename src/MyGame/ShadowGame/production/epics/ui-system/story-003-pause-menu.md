// 该文件由Cursor 自动生成

# Story: PauseMenu Window (Resume, Settings, Quit)

> **Epic**: ui-system
> **Story ID**: ui-system-003
> **Story Type**: UI
> **GDD Requirement**: TR-ui-005 (9 UIWindows defined), TR-ui-009 (TimeScale = 0 on PauseMenu)
> **ADR References**: ADR-011 (UIWindow Management), SP-002 (UIWindow Lifecycle)
> **Sprint**: TBD
> **Status**: Ready (依赖 ui-system-001)

## Context

PauseMenu 是玩家在游戏中暂停时打开的模态弹窗（Popup 层），包含三个按钮：Resume（继续游戏）、Settings（打开设置面板）、Quit（返回主菜单）。打开时 `Time.timeScale = 0`（游戏逻辑暂停）；Music 不受 TimeScale 影响继续播放（`AudioSource.ignoreListenerPause = true`，由 Audio System 保证）。PauseMenu 触发 Popup 层 Auto InputBlocker，阻断底层游戏手势。

PauseMenu 通常由 HUD 的 SettingsGear 按钮触发，也可由 Android back button 触发（TR-ui-022）。

## Acceptance Criteria

- [ ] `PauseMenuPanel` 继承 `UIWindow`，注册到 Popup 层（UILayer.Popup，sorting order base = 200）
- [ ] `OnCreate()`：绑定 Resume / Settings / Quit 按钮引用，注册 AddUIEvent 内部点击
- [ ] `OnRefresh()`：无特殊数据刷新（面板内容固定）
- [ ] 打开时（Show）：`Time.timeScale = 0`
- [ ] 关闭时（Close）：`Time.timeScale = 1`
- [ ] Resume 按钮：调用 `GameModule.UI.CloseWindow<PauseMenuPanel>()`
- [ ] Settings 按钮：调用 `GameModule.UI.ShowWindow<SettingsPanel>()`（如已有 Popup 可见则入队）
- [ ] Quit 按钮：显示确认提示（或直接）发送 `Evt_RequestSceneChange { targetChapter = null }` 返回主菜单
- [ ] Auto InputBlocker：面板打开时自动 PushBlocker("UIPanel_PauseMenuPanel")；关闭时自动 PopBlocker
- [ ] Android back button（硬件返回键）触发时等同于 Resume——关闭 PauseMenu（TR-ui-022）
- [ ] Music 在 PauseMenu 打开期间（TimeScale = 0）继续播放（集成验证，非 PauseMenu 代码实现）
- [ ] 面板关闭后 TimeScale 正确恢复（即使通过 Quit 路径退出）

## Implementation Notes

- 类路径：`Assets/GameScripts/HotFix/GameLogic/UI/PauseMenuPanel.cs`
- TimeScale 设置在 OnRefresh（每次打开时）设为 0；OnClose 中恢复为 1
- Android back button 通过监听 `Evt_AndroidBackButton`（或 Input.GetKeyDown(KeyCode.Escape) 包装后的 GameEvent）实现
- Settings 按钮点击后 PauseMenu 不关闭（Settings 作为新 Popup 入队或覆盖显示，取决于 story-001 Popup Queue 实现）
- Quit 确认逻辑：MVP 阶段可直接触发场景切换，不做二次确认弹窗

## Out of Scope

- SettingsPanel 内容（story-007）
- 场景切换实现（scene-management epic）
- Quit 二次确认弹窗（P2 功能）

## QA Test Cases (Visual/UI)

### Setup
- 游戏运行中，HUDPanel 可见
- 触发 PauseMenuPanel 打开（通过测试按钮或 SettingsGear）

### Verify
- PauseMenuPanel 在 Popup 层显示（sorting order ≥ 200），遮盖 HUD 内容
- 打开后 `Time.timeScale == 0f`
- Music 在 timeScale = 0 期间继续播放（不静音、不暂停）
- 底层游戏手势被阻断（点击游戏场景无响应）
- Resume 按钮点击 → 面板关闭，`Time.timeScale == 1f`，游戏手势恢复
- Settings 按钮点击 → SettingsPanel 打开
- Quit 按钮点击 → 跳转主菜单，PauseMenu 关闭，`Time.timeScale == 1f`

### Pass
- 所有 Verify 项通过，无控制台错误，InputBlocker 栈在关闭后为空

## Test Evidence Path

`production/qa/evidence/ui-system/pause-menu-evidence.md`

## Dependencies

- ui-system-001: UIModule 基础（Popup Queue, Auto InputBlocker）
- audio-system story-004: MusicLayer（ignoreListenerPause = true 保证 Music 在 TimeScale=0 时播放）
- ADR-011: Popup 层 Auto InputBlocker
- ADR-009: 场景切换（Quit 按钮触发场景跳转）
