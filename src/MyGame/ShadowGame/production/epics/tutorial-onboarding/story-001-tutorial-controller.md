// 该文件由Cursor 自动生成

# Story 001: TutorialController — Step-Based State Machine

> **Epic**: Tutorial & Onboarding
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/tutorial-onboarding.md`
**Requirement**: `TR-tutor-001`, `TR-tutor-002`, `TR-tutor-006`, `TR-tutor-007`

**ADR Governing Implementation**: ADR-010: Input Abstraction (InputFilter)
**ADR Decision Summary**: TutorialController 通过 `IInputService.PushFilter()` / `PopFilter()` 实现手势白名单限制；教学状态机采用 4 状态设计：Inactive → WaitingTrigger → Teaching → StepComplete；所有步骤配置来自 Luban `TbTutorialStep`。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW

**Control Manifest Rules (Presentation layer)**:
- Required: 所有模块通信通过 `GameModule.XXX` 访问；跨模块通信使用 `GameEvent`（Tutorial 范围 1800-1899）；教学状态机 FSM 通过 TEngine FsmModule 管理；所有事件 ID 在 `EventId` 静态类中统一定义；`Init()` 注册监听器，`Dispose()` 移除监听器；PuzzleLock token 使用预定义常量 `InteractionLockerId.Tutorial = "tutorial"`
- Forbidden: 禁止直接引用其他面板；禁止使用 C# 事件/委托跨模块通信；禁止在 `EventId.cs` 之外定义事件 ID
- GameEvent IDs: `Evt_TutorialStepStarted(1800)`, `Evt_TutorialStepCompleted(1801)`, `Evt_TutorialAllComplete(1802)`

---

## Acceptance Criteria

- [ ] `TutorialController` 实现 4 状态 FSM：`Inactive`、`WaitingTrigger`、`Teaching`、`StepComplete`
- [ ] `Inactive → WaitingTrigger`：收到 `Evt_ChapterLoaded` 且该章节有未完成教学步骤时触发
- [ ] `WaitingTrigger → Teaching`：`triggerCondition` 满足（当前支持：`OnChapterEnter` / `OnPreviousStepDone`）时发送 `Evt_TutorialStepStarted(stepId)`
- [ ] `Teaching → StepComplete`：收到手势完成确认（Story 005 负责），`successCount >= completionCount`，发送 `Evt_TutorialStepCompleted(stepId)`
- [ ] `StepComplete → WaitingTrigger`：有下一步骤时自动进入；`StepComplete → Inactive`：无下一步骤时发送 `Evt_TutorialAllComplete`
- [ ] 进入 `Teaching` 状态时调用 `GameModule.ObjectInteraction.PuzzleLockAll("tutorial")`，防止物件意外交互
- [ ] 离开 `Teaching` 状态时调用 `GameModule.ObjectInteraction.PuzzleUnlockAll("tutorial")`
- [ ] 教学激活期间向 HintSystem 发送 `Evt_TutorialStepStarted`，使 Hint 暂停计时
- [ ] 教学完成后向 HintSystem 发送 `Evt_TutorialStepCompleted`，使 Hint 恢复计时
- [ ] 演出触发（`Evt_NarrativeSequenceStarted`）时教学暂停（隐藏提示、保留 InputFilter），演出结束后恢复
- [ ] 所有步骤通过 `TbTutorialStep` 配置表读取，控制器本身不含任何步骤内容数据

---

## Implementation Notes

**状态机结构：**
```csharp
public enum TutorialState
{
    Inactive,       // 无教学任务
    WaitingTrigger, // 等待触发条件
    Teaching,       // 教学进行中
    StepComplete    // 当前步骤完成，准备下一步
}
```

**核心初始化流程：**
```csharp
public void Init(List<int> completedStepIds)
{
    _completedSteps = new HashSet<int>(completedStepIds);
    GameEvent.AddEventListener<int>(EventId.Evt_ChapterLoaded, OnChapterLoaded);
    GameEvent.AddEventListener<NarrativeStartedPayload>(EventId.Evt_NarrativeSequenceStarted, OnNarrativePause);
    GameEvent.AddEventListener(EventId.Evt_NarrativeSequenceCompleted, OnNarrativeResume);
}
```

**步骤激活（进入 Teaching 状态）：**
- 从 `Tables.Instance.TbTutorialStep.Get(stepId)` 读取步骤配置
- 调用 `IInputService.PushFilter(step.allowedGestures)`
- 调用 `PuzzleLockAll("tutorial")`
- 发送 `Evt_TutorialStepStarted` 通知 TutorialOverlay (Story 004) 和 HintSystem

**步骤完成：**
- 调用 `IInputService.PopFilter()`
- 调用 `PuzzleUnlockAll("tutorial")`
- 将 `stepId` 写入存档 `tutorialCompleted` 列表（通过 SaveSystem）
- 发送 `Evt_TutorialStepCompleted`

**与演出（Narrative）的交互：**
```
收到 Evt_NarrativeSequenceStarted:
  → _wasPaused = true (当前在 Teaching 状态)
  → 发送 Evt_TutorialPaused (UI 隐藏提示，但不 PopFilter)
收到 Evt_NarrativeSequenceCompleted:
  → if _wasPaused: 恢复 Teaching 状态，发送 Evt_TutorialResumed
```

**注意**：`TutorialController` 纯逻辑层，不直接操作 UI，通过 GameEvent 通知 `TutorialOverlay`（Story 004）。

---

## Out of Scope

- [Story 002]: Luban `TbTutorialStep` 配置表结构定义
- [Story 003]: InputFilter push/pop 的具体时序和白名单内容
- [Story 004]: TutorialOverlay UIWindow 的显示与动画
- [Story 005]: 手势完成检测（successCount 累计与通知）
- [Story 006]: 跳过教学 / 存档标记写入

---

## QA Test Cases

### TC-001-01: 章节加载触发教学
**Given** 玩家存档中 `tutorialCompleted` 为空，第 1 章有 3 个未完成教学步骤
**When** `Evt_ChapterLoaded(chapterId=1)` 被发送
**Then** Controller 进入 `WaitingTrigger` 状态，等待 `tut_drag` 步骤的触发条件

### TC-001-02: 触发条件满足后进入 Teaching
**Given** Controller 在 `WaitingTrigger`，`tut_drag` 的 `triggerCondition` = `OnChapterEnter`
**When** Chapter enter 触发检查
**Then** Controller 进入 `Teaching` 状态；`Evt_TutorialStepStarted(stepId="tut_drag")` 被发送；InputFilter 已激活（Story 003 验证）

### TC-001-03: 步骤完成后自动推进
**Given** Controller 在 `Teaching`（步骤 `tut_drag`）
**When** 收到步骤完成通知（`successCount=1 >= completionCount=1`）
**Then** 进入 `StepComplete`，发送 `Evt_TutorialStepCompleted("tut_drag")`；随后检测到有下一步骤 `tut_rotate`，进入 `WaitingTrigger`

### TC-001-04: 所有步骤完成
**Given** Controller 完成最后一个教学步骤
**When** 进入 `StepComplete` 且无更多步骤
**Then** 发送 `Evt_TutorialAllComplete`；Controller 进入 `Inactive`；InputFilter 已 Pop；PuzzleUnlock 已调用

### TC-001-05: 演出暂停教学
**Given** Controller 在 `Teaching` 状态
**When** `Evt_NarrativeSequenceStarted` 被发送
**Then** InputFilter 保持（不 Pop）；发送 `Evt_TutorialPaused`；演出结束后收到 `Evt_NarrativeSequenceCompleted` → 恢复 Teaching 状态

### TC-001-06: 已完成步骤不重新触发
**Given** `completedStepIds = ["tut_drag"]`，第 1 章有步骤 `tut_drag` 和 `tut_rotate`
**When** `Evt_ChapterLoaded(chapterId=1)` 触发
**Then** `tut_drag` 被跳过；直接从 `tut_rotate` 开始等待触发

### TC-001-07: 章节切换时清理教学状态
**Given** Controller 在 `Teaching` 状态（第 1 章）
**When** `Evt_SceneUnloadBegin` 被发送（场景切换）
**Then** InputFilter 被 Pop；PuzzleLock 被释放；Controller 进入 `Inactive`；所有事件监听器被移除

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/Tutorial/TutorialControllerTests.cs`
**Status**: [ ] Not yet created

**Test class pattern**:
```csharp
[TestFixture]
public class TutorialControllerTests
{
    // Mock IInputService, ISaveService, Tables (TbTutorialStep)
    // Drive state via synthetic GameEvent sends
    // Assert state transitions and event outputs
}
```

---

## Dependencies

- Depends on: input-system story-005 (InputFilter API), save-system story-001 (tutorialCompleted list), chapter-state epic (Evt_ChapterLoaded)
- Unlocks: Story 003 (InputFilter lock detail), Story 005 (progression detection feeds back to this FSM)
