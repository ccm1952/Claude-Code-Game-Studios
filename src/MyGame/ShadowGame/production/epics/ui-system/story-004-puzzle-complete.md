// 该文件由Cursor 自动生成

# Story: PuzzleComplete Window (Score Display, Continue)

> **Epic**: ui-system
> **Story ID**: ui-system-004
> **Story Type**: UI
> **GDD Requirement**: TR-ui-005 (9 UIWindows), TR-ui-010 (PuzzleCompletePanel auto-close 2.5s)
> **ADR References**: ADR-011 (UIWindow Management), SP-002 (UIWindow Lifecycle)
> **Sprint**: TBD
> **Status**: Ready (依赖 ui-system-001)

## Context

谜题完成后显示的结果面板（Popup 层），展示匹配分数（星级或百分比）、本关用时，并提供"继续"按钮进入下一章节。面板在显示 2.5s 后**自动关闭**（TR-ui-010），无需玩家主动点击；玩家也可提前点击"继续"按钮跳过等待。

PuzzleCompletePanel 为 Popup 层，自动推入 InputBlocker 阻断游戏输入，防止玩家在结果展示期间误触场景。

## Acceptance Criteria

- [ ] `PuzzleCompletePanel` 继承 `UIWindow`，注册到 Popup 层（UILayer.Popup，sorting order base = 200）
- [ ] `OnRefresh(userDatas)` 接收谜题结果数据：matchScore（0-1），puzzleTime（秒），星级（1-3）
- [ ] 面板显示：matchScore 百分比文本、星级图标（根据 score 阈值从 Luban 读取）、用时文本
- [ ] 自动关闭：面板打开后 2.5s 自动触发关闭（使用 UniTask delay，禁止 Coroutine）
- [ ] "继续"按钮：提前点击时取消定时关闭，立即关闭面板并触发下一章节加载
- [ ] 自动关闭后：发送 `Evt_PuzzleCompleteAcknowledged`，触发章节推进逻辑
- [ ] Auto InputBlocker：面板打开时自动 PushBlocker；关闭时自动 PopBlocker（无论自动还是手动关闭）
- [ ] `OnClose()`：取消未完成的 UniTask delay（防止面板关闭后 task 继续执行），注销 GameEvent 监听
- [ ] 分数展示动画：百分比数字从 0 滚动到最终值（0.8s 动画，UniTask 或 DOTween 实现）

## Implementation Notes

- 类路径：`Assets/GameScripts/HotFix/GameLogic/UI/PuzzleCompletePanel.cs`
- 2.5s 自动关闭：`CancellationTokenSource _closeCts` 在 OnRefresh 时创建，OnClose 时 Cancel 并 Dispose
- `await UniTask.Delay(TimeSpan.FromSeconds(2.5f), cancellationToken: _closeCts.Token).ContinueWith(Close);`
- matchScore 阈值（1星/2星/3星）从 Luban 配置读取（禁止硬编码 0.5f、0.75f 等）
- OnRefresh 被调用时重置 _closeCts（防止上次 task 残留）

## Out of Scope

- 章节推进逻辑本身（chapter-state epic）
- 分数计算（shadow-puzzle epic）

## QA Test Cases (Visual/UI)

### Setup
- 谜题完成，PuzzleCompletePanel 通过 `ShowWindow<PuzzleCompletePanel>(matchScore=0.82f, puzzleTime=67s)` 打开

### Verify
- 面板在 Popup 层显示，遮盖 HUD
- 分数区域显示 "82%"（或等效星级评分）
- 用时区域显示 "01:07"
- 底层游戏手势被阻断
- 2.5s 后面板自动关闭，InputBlocker 弹出
- "继续"按钮点击后立即关闭，2.5s 定时器被取消
- 分数滚动动画从 0% 到 82% 在约 0.8s 内完成

### Pass
- 所有 Verify 项通过，面板关闭后无 UniTask 异常或泄漏

## Test Evidence Path

`production/qa/evidence/ui-system/puzzle-complete-evidence.md`

## Dependencies

- ui-system-001: UIModule 基础（Popup Queue, Auto InputBlocker）
- shadow-puzzle epic: `Evt_PuzzleStateChanged` / matchScore 数据结构
- ADR-011: PuzzleCompletePanel auto-close 2.5s
