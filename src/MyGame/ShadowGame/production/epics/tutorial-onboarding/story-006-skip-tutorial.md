// 该文件由Cursor 自动生成

# Story 006: Skip Tutorial Option + tutorialCompleted Save Flag

> **Epic**: Tutorial & Onboarding
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/tutorial-onboarding.md`
**Requirement**: `TR-tutor-005`, `TR-tutor-010`

**ADR Governing Implementation**: ADR-008: Save System; ADR-010: Input Abstraction
**ADR Decision Summary**: 已完成的教学步骤 ID 列表持久化到存档 `tutorialCompleted` 字段（JSON 数组）；跳过时将当前章节所有步骤标记为完成并写入存档；Settings 中的"操作指南"入口不重新触发教学 FSM，而是展示静态说明页面。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: `tutorialCompleted` 字段存储在 `SaveData` JSON 中（不在 PlayerPrefs）；存档写入通过 `ISaveService.SaveAsync()` 使用 UniTask；跳过时同时调用 `PopFilter()`（如有）和 `PuzzleUnlockAll("tutorial")`；Save 操作通过 `TriggerImmediateSave()` 立即写入（跳过属于明确的用户操作，不应防抖）
- Forbidden: 禁止同步文件 I/O；禁止在 SaveData 之外存储 tutorialCompleted
- Guardrail: 存档写入异步，不阻塞主线程

---

## Acceptance Criteria

- [ ] `TutorialController` 提供 `SkipCurrentChapterTutorial()` 方法，将当前章节所有未完成步骤标记为已完成
- [ ] 跳过时：`IInputService.PopFilter()` 被调用（清除白名单）；`PuzzleUnlockAll("tutorial")` 被调用；`Evt_TutorialAllComplete` 被发送；`TutorialOverlay` 隐藏
- [ ] 跳过的步骤 ID 全部写入存档 `tutorialCompleted` 数组，通过 `ISaveService.TriggerImmediateSave()` 立即持久化
- [ ] 游戏重启后加载存档时，`tutorialCompleted` 中的步骤不再触发教学
- [ ] 存档损坏导致 `tutorialCompleted` 丢失时，所有教学步骤重新触发（最坏情况降级，不跳过）
- [ ] 第一章完整通关（不跳过）后，存档中包含 `["tut_drag", "tut_rotate", "tut_snap"]`（以 stepKey 存储）
- [ ] Settings 界面中"操作指南"入口展示所有已完成教学步骤的静态说明页（从 `TbTutorialStep` 读取）；不重新触发 TutorialController FSM
- [ ] 操作指南页（`OperationGuideWindow`）为独立 UIWindow，Popup 层（SortOrder base=200），不共享 `TutorialOverlay` 的资源句柄

---

## Implementation Notes

**跳过流程：**
```csharp
public async UniTaskVoid SkipCurrentChapterTutorial()
{
    // 1. 收集当前章节所有步骤
    var steps = Tables.Instance.TbTutorialStep.GetByChapter(_currentChapterId);
    foreach (var step in steps)
        _completedSteps.Add(step.Id);
    
    // 2. 清理输入状态
    if (GameModule.Input.IsFiltered)
        GameModule.Input.PopFilter();
    GameModule.ObjectInteraction.PuzzleUnlockAll("tutorial");
    
    // 3. 进入 Inactive 状态
    _state = TutorialState.Inactive;
    
    // 4. 通知 UI 隐藏
    GameEvent.Send(EventId.Evt_TutorialAllComplete);
    
    // 5. 立即写入存档
    await GameModule.Save.TriggerImmediateSave();
}
```

**存档结构（tutorialCompleted 字段）：**
```json
{
  "tutorialCompleted": ["tut_drag", "tut_rotate", "tut_snap", "tut_light"]
}
```
注意：存储 `stepKey`（字符串），而非 `id`（int），提升可读性和版本迁移便利性。加载时通过 `TbTutorialStep` 查找对应 ID。

**加载时恢复 completedSteps：**
```csharp
// 在 TutorialController.Init(SaveData saveData) 中
_completedSteps = new HashSet<int>();
foreach (var stepKey in saveData.TutorialCompleted)
{
    var stepData = Tables.Instance.TbTutorialStep.GetByKey(stepKey);
    if (stepData != null)
        _completedSteps.Add(stepData.Id);
}
```

**操作指南（OperationGuideWindow）：**
```csharp
public class OperationGuideWindow : UIWindow
{
    protected override void OnRefresh()
    {
        // 从 TbTutorialStep 加载所有步骤说明（不过滤 completedSteps，显示全部）
        var allSteps = Tables.Instance.TbTutorialStep.DataList
            .OrderBy(s => s.ChapterId).ThenBy(s => s.Order);
        RenderGuideItems(allSteps);
    }
}
```
操作指南通过 Settings 面板中的按钮打开：`GameModule.UI.ShowWindow<OperationGuideWindow>()`。

**版本迁移兼容性：**
- 存储 `stepKey` 字符串而非 ID，当配置表新增步骤时老存档玩家不受影响（新步骤不在已完成列表中，会正常触发）

---

## Out of Scope

- [Story 001]: TutorialController 主 FSM（跳过是其公开方法，不是独立模块）
- [Story 004]: TutorialOverlay 的隐藏动画（收到 `Evt_TutorialAllComplete` 后自行处理）
- "重置教程"功能（GDD 明确不提供，只有"操作指南"回看）
- 跳过按钮的 UI 布局（属于 ui-system epic 的暂停菜单设计）
- save-system epic 的存档文件格式（`tutorialCompleted` 字段已在 ADR-008 schema 中定义）

---

## QA Test Cases

### TC-006-01: 跳过后步骤不再触发
**Given** 玩家未完成 `tut_drag`，调用 `SkipCurrentChapterTutorial()`
**When** 重启游戏，加载存档，进入第 1 章
**Then** `tut_drag` 不再触发；`TutorialController` 进入 `Inactive`；无提示 UI 显示

### TC-006-02: 跳过时 InputFilter 被清除
**Given** 步骤 `tut_drag` 正在进行（InputFilter 已激活），用户选择跳过
**When** `SkipCurrentChapterTutorial()` 执行
**Then** `GameModule.Input.IsFiltered` 为 false；所有手势正常响应；物件可被正常交互

### TC-006-03: 正常完成后存档正确写入
**Given** 玩家顺序完成 `tut_drag`、`tut_rotate`、`tut_snap`
**When** 读取存档文件
**Then** `tutorialCompleted` 包含 `["tut_drag", "tut_rotate", "tut_snap"]`

### TC-006-04: 存档损坏时教学重新触发
**Given** 存档 `tutorialCompleted` 字段被损坏（备份也损坏）→ fresh start
**When** 进入第 1 章
**Then** 所有第 1 章教学步骤重新触发；无崩溃

### TC-006-05: 操作指南显示所有步骤
**Given** 玩家已完成第 1 章教学（`tut_drag/rotate/snap` 已完成），第 2 章教学 `tut_light` 未开始
**When** 打开 Settings → 操作指南
**Then** 所有 4 个已知步骤的说明都显示（包括未到达章节的步骤）；无需触发 TutorialController FSM

### TC-006-06: 跳过立即持久化
**Given** 玩家在第 1 章跳过教学
**When** 立即强制退出应用（`OnApplicationQuit`）然后重启
**Then** 重启后 `tutorialCompleted` 仍包含跳过的步骤（不丢失）

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/Tutorial/TutorialSkipAndSaveTests.cs`
**Status**: [ ] Not yet created

**Test class pattern**:
```csharp
[TestFixture]
public class TutorialSkipAndSaveTests
{
    // Mock ISaveService, verify tutorialCompleted writes
    // Simulate skip during active tutorial step
    // Simulate reload with completed steps
    // Verify OperationGuideWindow loads all steps from config
}
```

---

## Dependencies

- Depends on: Story 001 (TutorialController 提供 SkipCurrentChapterTutorial()), Story 002 (TbTutorialStep.GetByChapter), Story 003 (PopFilter 时序), save-system story-001 (SaveData schema), save-system story-002 (ISaveService.TriggerImmediateSave)
- Unlocks: settings-accessibility story-001 (操作指南入口依赖本 story 的 OperationGuideWindow), story-done 条件满足（epic 完整性）
