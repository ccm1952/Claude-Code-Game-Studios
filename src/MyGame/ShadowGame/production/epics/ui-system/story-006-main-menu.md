// 该文件由Cursor 自动生成

# Story: MainMenu Window (New Game, Continue, Settings)

> **Epic**: ui-system
> **Story ID**: ui-system-006
> **Story Type**: UI
> **GDD Requirement**: TR-ui-005 (9 UIWindows), TR-ui-016 (UI animations at 60fps)
> **ADR References**: ADR-011 (UIWindow Management), SP-002 (UIWindow Lifecycle)
> **Sprint**: TBD
> **Status**: Ready (依赖 ui-system-001)

## Context

主菜单是游戏的起点（在 Init Order 步骤 16 `ShowMainMenu` 时打开），提供三个按钮：New Game（开始新游戏，覆盖存档）、Continue（从上次存档继续）、Settings（打开设置面板）。MainMenu 显示在 HUD 层，无 InputBlocker（主菜单是最底层，无需阻断更底层的输入）。

Continue 按钮在无有效存档时显示为灰色（不可点击）。

## Acceptance Criteria

- [ ] `MainMenuPanel` 继承 `UIWindow`，注册到 HUD 层（UILayer.HUD，sorting order = 100）
- [ ] `OnCreate()`：绑定 NewGame / Continue / Settings 按钮引用，注册 AddUIEvent
- [ ] `OnRefresh()`：检查存档状态（`ISaveService.HasValidSave()`）；有存档 → Continue 按钮可交互；无存档 → Continue 按钮置灰（interactable = false）
- [ ] New Game 按钮：调用 `ISaveService.DeleteSave()`（清除存档但保留 PlayerPrefs 设置），然后发送 `Evt_RequestSceneChange { chapterId = "chapter1" }`
- [ ] Continue 按钮：发送 `Evt_RequestSceneChange { chapterId = lastSavedChapterId }`
- [ ] Settings 按钮：`GameModule.UI.ShowWindow<SettingsPanel>()`
- [ ] 主菜单背景音乐：发送 `Evt_PlayMusicRequest { clipId = "main_menu_bgm" }` （在 OnRefresh 时发送）
- [ ] 面板入场动画（fade-in 0.3s）在 OnRefresh 后触发，保持 60fps（DOTween 实现）
- [ ] 无 InputBlocker（HUD 层，主菜单无需阻断底层）

## Implementation Notes

- 类路径：`Assets/GameScripts/HotFix/GameLogic/UI/MainMenuPanel.cs`
- New Game 的存档删除需确认 `DeleteSave()` 只清除 save.json/backup，不清除 PlayerPrefs 设置（ADR-008）
- Continue 中 lastSavedChapterId 从 SaveSystem 加载的数据中读取
- 背景音乐 clipId "main_menu_bgm" 必须在 TbAudioEvent 中定义（story-006 audio config）

## Out of Scope

- 存档系统（save-system epic）
- 场景跳转（scene-management epic）
- 新手引导（tutorial-onboarding epic）

## QA Test Cases (Visual/UI)

### Setup: 无存档状态
- 确保 save.json 不存在，启动游戏进入主菜单

### Verify (无存档)
- Continue 按钮显示为灰色，不可点击
- New Game 按钮可点击，主菜单背景音乐开始播放
- 面板入场 fade-in 动画流畅（60fps，无卡顿）

### Setup: 有存档状态
- chapter2 存档已存在

### Verify (有存档)
- Continue 按钮可交互，点击触发 Evt_RequestSceneChange { chapterId = "chapter2" }
- New Game 按钮点击 → 存档清除 → 跳转 chapter1

### Pass
- 所有 Verify 项通过，PlayerPrefs 音量设置在 New Game 后未被清除

## Test Evidence Path

`production/qa/evidence/ui-system/main-menu-evidence.md`

## Dependencies

- ui-system-001: UIModule 基础
- save-system epic: ISaveService.HasValidSave(), DeleteSave()
- audio-system story-001: AudioManager（PlayMusicRequest 事件接收）
- ADR-009: Evt_RequestSceneChange 场景切换
