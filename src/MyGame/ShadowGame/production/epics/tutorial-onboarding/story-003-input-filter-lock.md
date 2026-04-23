// 该文件由Cursor 自动生成

# Story 003: InputFilter Whitelist Lock During Tutorial

> **Epic**: Tutorial & Onboarding
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/tutorial-onboarding.md`
**Requirement**: `TR-tutor-002`, `TR-tutor-003`

**ADR Governing Implementation**: ADR-010: Input Abstraction (InputFilter)
**ADR Decision Summary**: 教学激活时通过 `IInputService.PushFilter(allowedGestures)` 推入白名单过滤器；被过滤的手势被**静默丢弃**（无任何视觉/音频/触觉反馈）；教学完成时通过 `IInputService.PopFilter()` 恢复完整输入；Filter 是单一激活——新 Push 覆盖旧 Filter，而非堆叠；InputBlocker > InputFilter 优先级链。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: InputBlocker > InputFilter > Normal 优先级链（禁止绕过）；`allowedGestures` 数组在 PushFilter 时深拷贝，防止外部修改；`PopFilter()` 时恢复"无过滤"状态（不恢复上一个 filter）；所有阈值来自 Luban `TbTutorialStep.allowedGestures`
- Forbidden: 被过滤手势禁止产生任何反馈（静默丢弃）；禁止直接调用 `Input.GetTouch()` 绕过 InputService；禁止硬编码白名单 GestureType
- Guardrail: Filter check 为 O(n)，n ≤ 5（手势类型数量），不影响 < 0.5ms 预算

---

## Acceptance Criteria

- [ ] 收到 `Evt_TutorialStepStarted(stepId)` 时，从 `TbTutorialStep.Get(stepId).ParseAllowedGestures()` 读取白名单并调用 `GameModule.Input.PushFilter(allowedGestures)`
- [ ] 收到 `Evt_TutorialStepCompleted(stepId)` 时，调用 `GameModule.Input.PopFilter()`
- [ ] 教学期间，白名单外的手势事件（`Evt_Gesture_Tap/Drag/Rotate/Pinch/LightDrag`）**不被分发**到游戏层——通过监听对应事件验证零事件到达
- [ ] 白名单外手势被静默丢弃，无任何视觉反馈、音效、触觉反馈
- [ ] 白名单内手势正常分发，物件响应操作
- [ ] 教学期间演出触发（`Evt_NarrativeSequenceStarted`）时：Filter 保持不变（不 Pop）；InputBlocker 由 Narrative 系统接管（优先级高于 Filter）
- [ ] 演出结束（`Evt_NarrativeSequenceCompleted`）后：Narrative 的 InputBlocker 被 Pop；Filter 仍然有效继续约束
- [ ] `PopFilter()` 后第一次触摸操作立即使用完整手势集合（无延迟）
- [ ] 同一帧内 `PushFilter` 和 `PopFilter` 不产生竞态：教学步骤切换时先 Pop 再 Push（由 TutorialController Story 001 保证顺序）
- [ ] 白名单为空数组时（配置错误兜底），`PushFilter([])` 静默丢弃所有手势，并输出 `Debug.LogWarning`

---

## Implementation Notes

**教学激活序列（TutorialController 内调用，本 Story 验证行为）：**
```csharp
// 进入 Teaching 状态时
void OnStepStarted(int stepId)
{
    var step = Tables.Instance.TbTutorialStep.Get(stepId);
    GestureType[] allowed = step.ParseAllowedGestures();
    
    if (allowed.Length == 0)
        Log.Warning($"[Tutorial] Step {stepId} has empty allowedGestures — all input blocked!");
    
    GameModule.Input.PushFilter(allowed);
    // PushFilter 内部深拷贝数组，外部修改不影响
}

// 离开 Teaching 状态时
void OnStepCompleted(int stepId)
{
    GameModule.Input.PopFilter();
}
```

**各教学步骤的白名单示例（来自配置表）：**
| stepKey      | allowedGestures（白名单）       | 屏蔽的手势                  |
|--------------|---------------------------------|-----------------------------|
| tut_drag     | `[Tap, Drag]`                   | Rotate, Pinch, LightDrag    |
| tut_rotate   | `[Tap, Drag, Rotate]`           | Pinch, LightDrag            |
| tut_snap     | `[Tap, Drag]`                   | Rotate, Pinch, LightDrag    |
| tut_light    | `[Tap, LightDrag]`              | Drag, Rotate, Pinch         |

**注意**：`tut_drag` 和 `tut_snap` 允许 Tap，因为玩家需要先 Tap 选中物件才能拖拽。`tut_rotate` 允许 Drag 以便玩家在旋转前先选中并可以拖拽（不打断已选中物件的状态）。

**InputFilter 与 InputBlocker 优先级确认：**
```
帧内输入处理顺序:
1. InputBlocker 检查 → 非空则丢弃所有输入
2. 手势 FSM 识别 → 产生候选手势
3. InputFilter 检查 → 不在白名单则丢弃
4. 分发幸存手势到 GameEvent
```
演出期间 InputBlocker 激活 → 步骤 1 已丢弃所有输入 → Filter 不参与判断但保持状态。

---

## Out of Scope

- [Story 001]: TutorialController FSM 状态机（何时调用 Push/Pop）
- [Story 002]: 白名单 GestureType 的来源（配置表解析）
- [Story 005]: 判断玩家是否完成了所需手势（Filter 只控制哪些手势可通过，不判断完成）
- InputBlocker 的实现（属于 input-system epic）

---

## QA Test Cases

### TC-003-01: 白名单外手势被静默丢弃
**Given** 教学步骤 `tut_drag` 激活，白名单 = `[Tap, Drag]`
**When** 玩家执行双指旋转（`GestureType.Rotate`）
**Then** `Evt_Gesture_Rotate` 事件**不被分发**；物件不旋转；无音效；无触觉反馈；无任何 UI 变化

### TC-003-02: 白名单内手势正常通过
**Given** 教学步骤 `tut_drag` 激活，白名单 = `[Tap, Drag]`
**When** 玩家执行 Tap 操作
**Then** `Evt_Gesture_Tap` 正常分发；物件可被选中

### TC-003-03: 步骤完成后 Filter 移除
**Given** `tut_drag` 步骤完成，`PopFilter()` 已调用
**When** 玩家执行双指旋转（`GestureType.Rotate`）
**Then** `Evt_Gesture_Rotate` 正常分发；物件响应旋转

### TC-003-04: 演出期间 Filter 保持
**Given** 教学步骤 `tut_drag` 激活，InputFilter = `[Tap, Drag]`
**When** `Evt_NarrativeSequenceStarted` 触发（Narrative 推入 InputBlocker）
**Then** 演出结束后：Narrative 的 Blocker 被移除；Filter `[Tap, Drag]` 仍然有效；旋转手势仍被过滤

### TC-003-05: 空白名单警告
**Given** 某教学步骤 `allowedGestures` 配置为空
**When** `PushFilter([])` 被调用
**Then** `Debug.LogWarning` 输出包含步骤 ID；所有手势被丢弃（等价于全量阻断但不影响 Blocker 栈）

### TC-003-06: 步骤切换时 Filter 无缝更新
**Given** `tut_drag`（白名单 `[Tap,Drag]`）完成，`tut_rotate`（白名单 `[Tap,Drag,Rotate]`）激活
**When** 步骤切换完成后玩家执行旋转
**Then** 旋转手势正常通过（新 Filter 已生效）；无帧间空档期

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/Tutorial/TutorialInputFilterTests.cs`
**Status**: [ ] Not yet created

**Test class pattern**:
```csharp
[TestFixture]
public class TutorialInputFilterTests
{
    // Mock IInputService, track PushFilter/PopFilter calls
    // Verify whitelist content matches TbTutorialStep config
    // Simulate gesture events and verify discard/pass behavior
}
```

---

## Dependencies

- Depends on: input-system story-005 (InputFilter API 实现), Story 002 (ParseAllowedGestures), Story 001 (TutorialController 发送 Evt_TutorialStepStarted/Completed)
- Unlocks: Story 005 (手势完成检测需要先确认手势可以通过 Filter)
