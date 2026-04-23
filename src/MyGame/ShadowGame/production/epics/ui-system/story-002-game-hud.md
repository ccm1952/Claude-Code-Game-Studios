// 该文件由Cursor 自动生成

# Story: GameHUD Window (Hint Button, Puzzle Progress, Interaction Prompts)

> **Epic**: ui-system
> **Story ID**: ui-system-002
> **Story Type**: UI
> **GDD Requirement**: TR-ui-004 (HUD pass-through to game), TR-ui-006 (GameHUD widgets ×5), TR-ui-015 (HintButton opacity ramp)
> **ADR References**: ADR-011 (UIWindow Management), SP-002 (UIWindow Lifecycle)
> **Sprint**: TBD
> **Status**: Ready (依赖 ui-system-001)

## Context

GameHUD 是游戏进行时始终可见的 HUD 面板（HUD 层，sorting order base = 100），包含 5 个 UIWidget：HintButton（提示按钮）、PuzzleIndicator（谜题进度）、SaveIndicator（存档状态）、ChapterTitle（章节标题）、SettingsGear（设置按钮快捷入口）。

HUD 面板**不阻断游戏输入**（HUD 层无 auto InputBlocker）——玩家手势穿透 HUD 直达游戏场景。HintButton 有透明度渐变（opacity ramp）：提示可用时淡入，冷却中时淡出。HUDPanel 在场景加载时**预加载**，不等到首次 ShowWindow 才加载。

## Acceptance Criteria

- [ ] `HUDPanel` 继承 `UIWindow`，注册到 HUD 层（UILayer.HUD，sorting order = 100）
- [ ] `OnCreate()`：绑定 5 个 Widget 引用（HintButton、PuzzleIndicator、SaveIndicator、ChapterTitle、SettingsGear）
- [ ] `OnRefresh()`：刷新当前章节标题文本、谜题进度百分比
- [ ] HUDPanel 在章节场景加载时预加载（不等首次 ShowWindow 触发异步加载）
- [ ] HUD 无 InputBlocker——玩家 Tap/Drag 手势穿透 HUDPanel 到达游戏层
- [ ] HintButton opacity ramp：监听 `Evt_HintCooldownTick { remaining, total }` 事件，根据 remaining/total 比值更新 HintButton CanvasGroup.alpha（冷却中 → 淡出到 0.3；可用 → 淡入到 1.0）
- [ ] PuzzleIndicator 订阅 `Evt_MatchScoreChanged { score }` 事件，实时更新进度显示
- [ ] SaveIndicator 订阅 `Evt_SaveBegin` / `Evt_SaveComplete` 事件，显示/隐藏存档图标
- [ ] SettingsGear 按钮点击 → `GameModule.UI.ShowWindow<SettingsPanel>()`
- [ ] 面板关闭（OnClose）时正确注销所有 GameEvent 监听
- [ ] 触摸目标尺寸 ≥ 44×44pt（HintButton 和 SettingsGear）

## Implementation Notes

- 类路径：`Assets/GameScripts/HotFix/GameLogic/UI/HUDPanel.cs`
- 预加载：在 scene load complete 处理中调用 `GameModule.UI.ShowWindow<HUDPanel>()` 后立即调用 `CloseWindow<HUDPanel>()`（或使用 TEngine 的 preload API，如存在）
- HintButton opacity ramp 在 OnUpdate 中根据 _hintCooldownRatio 更新 CanvasGroup.alpha（使用 UniTask 或 DOTween 平滑过渡）
- `AddUIEvent()` 处理按钮内部点击，GameEvent 处理跨系统数据更新
- 禁止在 OnUpdate 中直接读取系统状态（通过事件推送模式）

## Out of Scope

- HintPanel 的提示内容显示（hint-system epic）
- PuzzleIndicator 的具体分数计算（shadow-puzzle epic）
- SettingsPanel 内容（story-007）

## QA Test Cases (Visual/UI)

### Setup
- 章节场景加载完成，HUDPanel 显示
- PuzzleIndicator 可见，HintButton 可见，SettingsGear 可见

### Verify
- HUDPanel 在 HUD 层正确显示，sorting order = 100
- 游戏场景 Tap/Drag 手势不被 HUDPanel 拦截（穿透测试）
- HintButton 触摸目标区域 ≥ 44pt × 44pt（检查 RectTransform 尺寸）
- `Evt_MatchScoreChanged { score = 0.75f }` 触发后 PuzzleIndicator 显示 75%
- HintButton 在冷却中 alpha 降至 ≤ 0.3；冷却结束后 alpha 升至 1.0
- SettingsGear 点击 → SettingsPanel 打开（Popup 层）

### Pass
- 所有 Verify 项通过，无控制台错误

## Test Evidence Path

`production/qa/evidence/ui-system/hud-panel-evidence.md`

## Dependencies

- ui-system-001: UIModule 基础（HUD 层注册、ShowWindow API）
- hint-system epic: `Evt_HintCooldownTick` 事件定义
- shadow-puzzle epic: `Evt_MatchScoreChanged` 事件定义
- save-system epic: `Evt_SaveBegin` / `Evt_SaveComplete` 事件定义
- ADR-011: UIWindow 层级和生命周期
