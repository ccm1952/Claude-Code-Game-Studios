// 该文件由Cursor 自动生成

# Story 007: Haptic Feedback Integration

> **Epic**: Input System
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Visual/Feel
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/input-system.md`
**Requirement**: Feel Acceptance Criteria（手感目标）；关联 ADR-013（Object Interaction 中 haptic 门控模式参考）

**ADR Governing Implementation**: ADR-010: Input Abstraction
**ADR Decision Summary**: Haptic feedback 由 InputService 在手势识别关键时刻触发（Tap 确认、DragBegan、RotateBegan），通过全局 `Settings.haptic_enabled` 标记门控——该标记为 PlayerPrefs 存储的用户设置，禁用时 0 次 haptic 调用；Input System 自身不产生视觉/音频反馈，haptic 是本层唯一的感官输出。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW

**Control Manifest Rules (Foundation layer)**:
- Required: Haptic 调用必须通过 `Settings.haptic_enabled`（PlayerPrefs）门控（ADR-013 原则同样适用于 Input 层）；haptic 仅在手势识别关键节点触发（Tap 确认、DragBegan）；haptic 在 InputBlocker 阻断时不触发（被阻断的手势不产生任何反馈）；haptic 在 InputFilter 过滤时不触发
- Forbidden: Input System 不产生任何视觉反馈；Input System 不产生任何音效；被 Filter 过滤的手势不触发任何反馈（包括 haptic）
- Guardrail: Unity `Handheld.Vibrate()` 在 Android 上可用；iOS 使用 `UIImpactFeedbackGenerator`（需 native plugin 或 Unity 内置封装）；PC Editor 下 haptic 调用为空操作（no-op）

---

## Acceptance Criteria

- [ ] `IHapticService` 或静态 `HapticHelper` 封装 haptic 调用，隔离平台差异
- [ ] 触发点：`Tap 确认`（FSM Pending → Tap 判定成功时）、`DragBegan`（Pending → Dragging 转换帧）——各平台使用 Light Impact 级别
- [ ] `Settings.haptic_enabled == false` 时：haptic 方法体为空操作，0 次平台 API 调用
- [ ] InputBlocker 激活时触发的（被阻断的）手势：0 次 haptic 调用
- [ ] InputFilter 过滤的手势：0 次 haptic 调用
- [ ] PC/Editor 平台：haptic 调用为 no-op（不报错，不调用 `Handheld.Vibrate()`）
- [ ] iOS：haptic 时长/强度使用 Light（`UIImpactFeedbackStyle.Light`），不使用 Heavy（避免打扰感）
- [ ] Android：使用 `Handheld.Vibrate()` 或自定义短振动（< 50ms），不使用默认长振动
- [ ] Haptic 调用在手势确认的**同帧**执行（0 帧延迟）
- [ ] 主观评审通过：5 位测试者中 ≥ 4 位认为振动反馈增强了操作感但不影响游戏体验

---

## Implementation Notes

**平台抽象层设计**
```csharp
public interface IHapticService
{
    void TriggerLight();   // Tap confirm, DragBegan
    void TriggerMedium();  // (reserved for RotateBegan or object snap — not MVP)
}

// Mobile implementation
public class MobileHapticService : IHapticService
{
    public void TriggerLight()
    {
#if UNITY_IOS
        // iOS: UIImpactFeedbackGenerator.Light via native bridge
        IOSHapticBridge.TriggerImpact(HapticImpactStyle.Light);
#elif UNITY_ANDROID
        Handheld.Vibrate(); // minimal vibration, < 50ms custom duration if possible
#endif
    }
}

// Editor / PC no-op
public class NullHapticService : IHapticService
{
    public void TriggerLight() { }
    public void TriggerMedium() { }
}
```

**Settings 门控集成**
```csharp
private IHapticService _haptic;

public void Init()
{
    bool hapticEnabled = PlayerPrefs.GetInt("haptic_enabled", 1) == 1;
#if UNITY_EDITOR || UNITY_STANDALONE
    _haptic = new NullHapticService();
#else
    _haptic = hapticEnabled ? (IHapticService)new MobileHapticService() : new NullHapticService();
#endif
    // Also listen for runtime setting change:
    GameEvent.AddEventListener<bool>(EventId.Evt_Settings_HapticEnabledChanged, OnHapticSettingChanged);
}

private void OnHapticSettingChanged(bool enabled)
{
#if !UNITY_EDITOR && !UNITY_STANDALONE
    _haptic = enabled ? (IHapticService)new MobileHapticService() : new NullHapticService();
#endif
}
```

**触发位置（在 FSM 内）**
```csharp
// SingleFingerFSM
case FsmState.Pending when isTap:
    _haptic.TriggerLight();    // Tap confirm
    EmitTapCandidate(...);
    break;

case FsmState.Pending when isDrag:
    _haptic.TriggerLight();    // DragBegan
    TransitionTo(FsmState.Dragging);
    break;
```

**Blocker/Filter 安全保障**：haptic 触发点在 FSM 内部（候选生成阶段），但 Blocker 检查在 FSM 之前——若 Blocker 激活，FSM 根本不运行，haptic 不会触发。Filter 检查在 dispatch 前；但 haptic 是在 FSM 内触发的（候选生成时）。

> **设计决策**：haptic 应该跟随 dispatch 还是跟随 FSM 候选生成？
> 建议：haptic 跟随 **dispatch**（Filter 通过后才触发），确保被 Filter 过滤的手势 0 次 haptic。
> 实现调整：将 haptic 调用移到 dispatch 循环内，在 `GameEvent.Send()` 之前、Filter 检查通过后。

---

## Out of Scope

- [Story 001/002]: FSM 状态转换本身（haptic 是叠加的感官层，不影响 FSM 逻辑）
- [Object Interaction Story]: 物件吸附（snap）haptic——由 Object Interaction 层触发，不在 Input System 层

---

## QA Test Cases

### TC-007-01: Tap 确认触发 haptic
**Setup** 真机（iPhone 13 Mini）；`Settings.haptic_enabled = true`  
**Verify** 执行单指快速 Tap → 手机在 Tap 确认的同帧产生轻微振动  
**Pass** 振动感觉即时、轻柔；测试者无需提示能感知

### TC-007-02: DragBegan 触发 haptic
**Setup** 真机；`haptic_enabled = true`  
**Verify** 单指按下后缓慢移动超过 dragThreshold → 产生轻微振动反馈  
**Pass** 振动时机与"手势确认"感觉一致，不早也不晚

### TC-007-03: haptic_enabled = false 零振动
**Setup** 真机；Settings 中关闭触感反馈（`haptic_enabled = false`）  
**Verify** 执行 Tap、DragBegan 操作  
**Pass** 手机 0 次振动；无任何感知

### TC-007-04: Blocked 手势 0 次 haptic
**Setup** 真机；`haptic_enabled = true`；push InputBlocker  
**Verify** 执行任意手势操作  
**Pass** 0 次振动（被阻断的操作没有任何反馈）

### TC-007-05: Filtered 手势 0 次 haptic
**Setup** 真机；`PushFilter([GestureType.Tap])`  
**Verify** 执行 Drag 操作（在 Filter 黑名单中）  
**Pass** 0 次振动；Drag 被静默丢弃

### TC-007-06: PC/Editor 无振动 API 调用
**Setup** Unity Editor  
**Verify** 执行 Tap 操作  
**Pass** 无 Console 错误；无 `Handheld.Vibrate()` 相关异常；NullHapticService 正常工作

### TC-007-07: 运行时设置切换立即生效
**Setup** 真机；`haptic_enabled = true`  
**Verify** 执行 Tap（有振动）→ 游戏内切换设置为 OFF（`GameEvent.Send(Evt_Settings_HapticEnabledChanged, false)`）→ 再次 Tap  
**Pass** 第二次 Tap 无振动；切换无需重启

### TC-007-08: 主观手感评审
**Setup** 5 位外部测试者，真机，不提前告知 haptic 设置  
**Verify** 让测试者正常游玩 5 分钟，之后询问"操作时有振动反馈吗？感觉如何？"  
**Pass** ≥ 4/5 测试者认为振动自然、增强了操作感；0/5 认为振动令人烦躁

---

## Test Evidence

**Story Type**: Visual/Feel  
**Required evidence**: `tests/evidence/input-system/story-007-haptic-feel-review.md`

**Evidence template**:
```markdown
# Story 007 Haptic Feel Review
Date: [日期]
Device: [设备型号]
Testers: [人数]
haptic_enabled=true 测试结果: [Pass/Fail]
haptic_enabled=false 测试结果: [Pass/Fail]
主观评审: [X/5 通过]
备注: [...]
```

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001（FSM 状态转换节点），Story 004（Blocker 门控），Story 005（Filter 门控）
- Unlocks: 无直接解锁，但 haptic 是手感验收（Feel Acceptance Criteria）的必要条件
