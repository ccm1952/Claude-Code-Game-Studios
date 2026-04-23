// 该文件由Cursor 自动生成

# Story: ChapterSelect Window (Chapter List, Lock/Unlock State)

> **Epic**: ui-system
> **Story ID**: ui-system-005
> **Story Type**: UI
> **GDD Requirement**: TR-ui-005 (9 UIWindows), TR-ui-003 (Popup/Overlay auto InputBlocker)
> **ADR References**: ADR-011 (UIWindow Management), SP-002 (UIWindow Lifecycle)
> **Sprint**: TBD
> **Status**: Blocked (依赖 chapter-state epic)

## Context

章节选择面板（Popup 层），展示所有章节的列表，每个章节条目显示名称、缩略图、完成状态（锁定/未完成/完成星级）。已解锁的章节可点击进入；锁定章节显示锁图标，不可点击。

ChapterSelectPanel 从 `IChapterProgressService` 查询章节解锁状态，通过 `GameEvent` 触发场景切换而非直接调用 Scene Manager。

## Acceptance Criteria

- [ ] `ChapterSelectPanel` 继承 `UIWindow`，注册到 Popup 层（UILayer.Popup，sorting order base = 200）
- [ ] `OnCreate()`：初始化章节条目列表的 UIWidget 引用
- [ ] `OnRefresh()`：从 `IChapterProgressService` 读取所有章节进度，刷新每个条目的：解锁状态图标、章节名称（本地化 key）、完成星级（0-3 星）
- [ ] 锁定章节：条目显示锁图标，AddUIEvent 点击回调无操作（或提示"尚未解锁"）
- [ ] 已解锁章节：点击发送 `Evt_RequestSceneChange { chapterId }` 触发场景跳转
- [ ] 章节数量动态读取（从 Luban `TbChapter` 表，不硬编码数量）
- [ ] Auto InputBlocker：打开时 PushBlocker("UIPanel_ChapterSelectPanel")；关闭时 PopBlocker
- [ ] Android back button 触发时关闭 ChapterSelectPanel
- [ ] `OnClose()`：注销 GameEvent 监听

## Implementation Notes

- 类路径：`Assets/GameScripts/HotFix/GameLogic/UI/ChapterSelectPanel.cs`
- 章节条目使用 `ScrollRect` + `UIWidget`（每个条目一个 widget），动态生成
- 禁止在 OnRefresh 中直接调用 Scene Manager API——通过 GameEvent 解耦
- 章节名称文本使用本地化 key（story-010 绑定），此处先用 placeholder 字符串

## Out of Scope

- 章节进度系统（chapter-state epic）
- 章节解锁逻辑（chapter-state epic）
- 本地化文本绑定（story-010）

## QA Test Cases (Visual/UI)

### Setup
- chapter1 已完成（2星），chapter2 已解锁但未完成，chapter3 锁定
- ChapterSelectPanel 通过 ShowWindow 打开

### Verify
- 面板在 Popup 层显示，三个章节条目可见
- chapter1 条目显示 2 星图标，可点击
- chapter2 条目显示"未完成"状态，可点击
- chapter3 条目显示锁图标，点击无响应（或显示提示）
- chapter1 点击 → 发送 Evt_RequestSceneChange { chapterId = "chapter1" }
- 底层游戏手势被阻断
- Android back button → 面板关闭，InputBlocker 弹出

### Pass
- 所有 Verify 项通过，无控制台错误

## Test Evidence Path

`production/qa/evidence/ui-system/chapter-select-evidence.md`

## Dependencies

- ui-system-001: UIModule 基础
- chapter-state epic: IChapterProgressService（章节解锁状态查询）
- ADR-009: 场景切换 GameEvent（Evt_RequestSceneChange）
- story-010: 本地化绑定（章节名称）
