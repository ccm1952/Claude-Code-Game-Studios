// 该文件由Cursor 自动生成

# Story 005: Tutorial Step Completion Detection + Auto-Advance

> **Epic**: Tutorial & Onboarding
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/tutorial-onboarding.md`
**Requirement**: `TR-tutor-002`, `TR-tutor-004`

**ADR Governing Implementation**: ADR-010: Input Abstraction; ADR-006: GameEvent Protocol
**ADR Decision Summary**: 手势完成检测通过订阅 `Evt_Gesture_XXX` GameEvent 实现；完成判定：`successCount >= completionCount`（从 `TbTutorialStep` 读取）；判定在同一帧内完成（≤ 1 帧延迟）；`Evt_TutorialStepCompleted` 触发后由 Story 001 的 TutorialController 负责推进到下一步骤。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: 监听 5 个手势 GameEvent（1000-1004）；只有 `GesturePhase.Ended` 才计入完成次数（不计 Began/Updated）；在 `Init()` 注册监听器，`Dispose()` 移除；`Evt_TutorialStepStarted` 时重置 `successCount`；`Evt_SceneUnloadBegin` 时清理所有状态
- Forbidden: 禁止直接调用 `Input.GetTouch()`；禁止在处理器中重入同一事件
- Guardrail: 判定逻辑 < 0.1ms/帧（纯计数逻辑，无 Physics/LINQ）

---

## Acceptance Criteria

- [ ] `TutorialProgressionDetector` 订阅所有 5 个手势事件（`Evt_Gesture_Tap/Drag/Rotate/Pinch/LightDrag`）
- [ ] 只有当 `gesture.Phase == GesturePhase.Ended` 且 `gesture.Type == activeStep.requiredGestureType` 时，`successCount` 递增 1
- [ ] `successCount >= activeStep.completionCount` 时，立即（同帧内）发送 `Evt_TutorialStepCompleted(stepId)`
- [ ] 收到 `Evt_TutorialStepStarted(stepId)` 时，重置 `successCount = 0`，激活对应步骤的检测
- [ ] 收到 `Evt_TutorialStepCompleted` 后，停止对该步骤的手势监听（等待 Story 001 触发下一步骤的 `Evt_TutorialStepStarted`）
- [ ] `tut_snap` 步骤（`requiredGesture = Drag`）：完成判定为拖拽结束后物件触发了吸附动画（需监听 `Evt_ObjectSnapped`，而非纯 Drag 完成）
- [ ] `NearObject` 位置步骤：检测到 `requiredGesture` 完成时，同时发送 `Evt_TutorialObjectPosition(worldPos)`，供 TutorialOverlay 更新提示位置
- [ ] 教学未激活（`Inactive` 状态）时，手势事件正常传递不被干扰（不计入任何 `successCount`）
- [ ] 完成判定延迟 ≤ 1 帧（在收到 `Evt_Gesture_XXX` 的同一帧内发出 `Evt_TutorialStepCompleted`）

---

## Implementation Notes

**核心检测逻辑：**
```csharp
public class TutorialProgressionDetector
{
    private TbTutorialStepData _activeStep;
    private int _successCount;
    private bool _isActive;

    public void Init()
    {
        GameEvent.AddEventListener<GestureData>(EventId.Evt_Gesture_Tap, OnGesture);
        GameEvent.AddEventListener<GestureData>(EventId.Evt_Gesture_Drag, OnGesture);
        GameEvent.AddEventListener<GestureData>(EventId.Evt_Gesture_Rotate, OnGesture);
        GameEvent.AddEventListener<GestureData>(EventId.Evt_Gesture_Pinch, OnGesture);
        GameEvent.AddEventListener<GestureData>(EventId.Evt_Gesture_LightDrag, OnGesture);
        
        GameEvent.AddEventListener<int>(EventId.Evt_TutorialStepStarted, OnStepStarted);
        GameEvent.AddEventListener(EventId.Evt_SceneUnloadBegin, OnSceneUnload);
    }

    private void OnGesture(GestureData data)
    {
        if (!_isActive || _activeStep == null) return;
        if (data.Phase != GesturePhase.Ended) return;
        if (data.Type != _activeStep.RequiredGestureType) return;
        
        // tut_snap 特殊处理：等待 Evt_ObjectSnapped（下方单独处理）
        if (_activeStep.StepKey == "tut_snap") return;
        
        _successCount++;
        CheckCompletion();
    }

    private void CheckCompletion()
    {
        if (_successCount >= _activeStep.CompletionCount)
        {
            _isActive = false;
            GameEvent.Send(EventId.Evt_TutorialStepCompleted, _activeStep.Id);
        }
    }
}
```

**tut_snap 特殊判定（吸附完成）：**
```csharp
// 补充监听 Object Interaction 的吸附事件
GameEvent.AddEventListener<int>(EventId.Evt_ObjectSnapped, OnObjectSnapped);

private void OnObjectSnapped(int objectId)
{
    if (!_isActive || _activeStep?.StepKey != "tut_snap") return;
    _successCount++;
    CheckCompletion();
}
```

**NearObject 位置更新：**
```csharp
// 当 promptPosition == NearObject 且手势更新时
if (_activeStep.PromptPosition == PromptPosition.NearObject)
{
    var worldPos = GetSelectedObjectPosition(); // 从 Object Interaction 查询
    GameEvent.Send(EventId.Evt_TutorialObjectPosition, worldPos);
}
```

**与 Hint System 的集成：**
- `Evt_TutorialStepStarted` → HintSystem 暂停所有计时器（HintSystem 自行监听，本 story 不直接调用）
- `Evt_TutorialStepCompleted` → HintSystem 恢复并重置计时器为 0（HintSystem 自行监听）
- 集成由 ADR-015 中 Hint System 的监听规则保障，本 story 无需额外代码

---

## Out of Scope

- [Story 001]: TutorialController 状态机（接收 `Evt_TutorialStepCompleted` 后推进到下一步骤）
- [Story 003]: InputFilter 确保被过滤手势不会到达本 Detector
- [Story 004]: 提示 UI 的显示与位置更新（只消费 `Evt_TutorialObjectPosition`）
- [Story 006]: 跳过逻辑（不走正常完成检测流程）
- HintSystem 的暂停/恢复（由 HintSystem 自行监听教学事件实现）

---

## QA Test Cases

### TC-005-01: 单次完成触发步骤完成
**Given** 步骤 `tut_drag`（completionCount=1, requiredGesture=Drag）已激活
**When** `Evt_Gesture_Drag(Phase=Ended)` 被分发
**Then** `Evt_TutorialStepCompleted(stepId=101)` 在同一帧内被发送；`successCount` 重置

### TC-005-02: Phase.Updated 不计入完成次数
**Given** 步骤 `tut_drag` 已激活
**When** 收到 `Evt_Gesture_Drag(Phase=Updated)`（拖拽进行中）
**Then** `successCount` 不增加；`Evt_TutorialStepCompleted` 不被发送

### TC-005-03: 错误手势类型不计入
**Given** 步骤 `tut_rotate`（requiredGesture=Rotate）已激活
**When** 收到 `Evt_Gesture_Drag(Phase=Ended)`（拖拽完成）
**Then** `successCount` 不增加（requiredGesture 不匹配）

### TC-005-04: tut_snap 等待吸附事件
**Given** 步骤 `tut_snap` 已激活
**When** 收到 `Evt_Gesture_Drag(Phase=Ended)`
**Then** `successCount` 不增加；等待 `Evt_ObjectSnapped` 才计入；`Evt_ObjectSnapped` 到达后 `successCount` 增加并触发完成

### TC-005-05: 步骤切换时 successCount 重置
**Given** 步骤 `tut_drag` 中 `successCount = 0`
**When** `Evt_TutorialStepStarted(102)`（tut_rotate）被发送
**Then** `successCount` 重置为 0；Detector 开始监听 Rotate 手势完成

### TC-005-06: 非激活状态时手势不干扰
**Given** TutorialController 处于 `Inactive` 状态（无活跃步骤）
**When** 玩家完成任意手势
**Then** `Evt_TutorialStepCompleted` 不被发送；游戏正常运行

### TC-005-07: 场景卸载时清理状态
**Given** 步骤检测正在进行中
**When** `Evt_SceneUnloadBegin` 发送
**Then** `_isActive = false`；`_activeStep = null`；所有 GameEvent 监听器被移除

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/Tutorial/TutorialProgressionTests.cs`
**Status**: [ ] Not yet created

**Test class pattern**:
```csharp
[TestFixture]
public class TutorialProgressionTests
{
    // Integration test: TutorialController + ProgressionDetector + InputService mock
    // Simulate gesture events end-to-end
    // Verify step completion → auto-advance → next step activation
}
```

---

## Dependencies

- Depends on: Story 001 (TutorialController 发送 Evt_TutorialStepStarted), Story 002 (TbTutorialStep requiredGestureType), Story 003 (InputFilter 确保手势已通过白名单), input-system (Evt_Gesture_XXX 手势事件), object-interaction epic (Evt_ObjectSnapped)
- Unlocks: Story 006 (完成检测正常工作后才能验证跳过场景); hint-system integration (HintSystem 暂停依赖这些事件)
