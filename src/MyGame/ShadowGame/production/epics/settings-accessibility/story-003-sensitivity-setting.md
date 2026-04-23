// 该文件由Cursor 自动生成

# Story 003: Touch Sensitivity Adjustment (Input Threshold Integration)

> **Epic**: Settings & Accessibility
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/settings-accessibility.md`
**Requirement**: `TR-settings-004`

**ADR Governing Implementation**: ADR-010: Input Abstraction; ADR-008: Save System (PlayerPrefs)
**ADR Decision Summary**: `touch_sensitivity` 作为倍率修改 Input System 的 `dragThreshold` 和 Object Interaction 的 `fatFingerMargin`；灵敏度范围 0.5×–2.0×；修改后立即生效（下一次触摸操作即使用新值）；通过 `Evt_SettingChanged(touch_sensitivity)` 广播，InputService 和 ObjectInteraction 自行监听并更新配置。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: 灵敏度公式：`effectiveDragThreshold = baseDragThreshold / touchSensitivity`；`effectiveFatFingerMargin = baseFatFingerMargin × touchSensitivity`；基础值来自 Luban `TbInputConfig`；修改立即生效，无需重启；修改通过 `GameModule.Settings.SetFloat()` 写入并广播 `Evt_SettingChanged`
- Forbidden: 禁止硬编码 base 阈值；禁止直接修改 InputService 内部状态（通过事件解耦）；禁止 touch_sensitivity 影响物件移动速度（物件始终 1:1 跟手）
- Guardrail: 灵敏度修改后，第一帧触摸操作即生效（< 1 帧延迟）

---

## Acceptance Criteria

- [ ] `SettingsPanel` 中的 `TouchSensitivitySlider` 范围 0.5–2.0，步进 0.1（或连续）
- [ ] `OnRefresh()` 时从 `GameModule.Settings.GetFloat(SettingsKey.TouchSensitivity, 1.0f)` 初始化滑块
- [ ] 滑块变化时调用 `GameModule.Settings.SetFloat(SettingsKey.TouchSensitivity, value)` 立即写入并触发 `Evt_SettingChanged`
- [ ] `InputService` 监听 `Evt_SettingChanged(touch_sensitivity)`，更新运行时 `dragThreshold`：`effectiveDragThreshold = baseDragThreshold_mm * Screen.dpi / 25.4 / sensitivity`
- [ ] `ObjectInteractionSystem` 监听 `Evt_SettingChanged(touch_sensitivity)`，更新 `fatFingerMargin`：`effectiveFatFingerMargin = baseFatFingerMargin × sensitivity`
- [ ] 滑块最低值 0.5×：拖拽阈值加倍（更难触发），胖手指补偿减半
- [ ] 滑块最高值 2.0×：拖拽阈值减半（更容易触发），胖手指补偿加倍
- [ ] 修改后下一次触摸操作立即使用新阈值（无需重启或场景切换）
- [ ] `touch_sensitivity` 不影响物件拖拽的移动速度（物件位置始终 = 手指位置，不缩放 delta）

---

## Implementation Notes

**InputService 中监听灵敏度变更：**
```csharp
// InputService.Init() 中注册
GameEvent.AddEventListener<SettingChangedPayload>(EventId.Evt_SettingChanged, OnSettingChanged);

private void OnSettingChanged(SettingChangedPayload payload)
{
    if (payload.Key != SettingsKey.TouchSensitivity) return;
    
    float sensitivity = payload.FloatValue;
    var config = Tables.Instance.TbInputConfig.Get(0); // 全局配置行
    float baseMm = config.BaseDragThreshold_mm;
    float dpi = Screen.dpi > 0 ? Screen.dpi : config.FallbackDPI;
    
    // 更新运行时阈值（不修改 Luban 配置对象，存储为运行时变量）
    _runtimeDragThreshold = baseMm * dpi / 25.4f / sensitivity;
}
```

**ObjectInteractionSystem 中监听灵敏度变更：**
```csharp
private void OnSettingChanged(SettingChangedPayload payload)
{
    if (payload.Key != SettingsKey.TouchSensitivity) return;
    
    float sensitivity = payload.FloatValue;
    var config = Tables.Instance.TbObjectInteractionConfig.Get(0);
    
    _runtimeFatFingerMargin = config.BaseFatFingerMargin * sensitivity;
}
```

**灵敏度应用公式（来自 GDD）：**
```
effectiveDragThreshold   = baseDragThreshold / sensitivity   // 灵敏度高 → 阈值小 → 更易触发拖拽
effectiveFatFingerMargin = baseFatFingerMargin × sensitivity // 灵敏度高 → 补偿大 → 更易选中
```

**注意**：物件 `delta` 在 Object Interaction 层始终为 `1:1 = currentTouchPos - prevTouchPos`（不乘灵敏度系数），灵敏度仅影响**识别阈值**，不影响**移动速度**。

**边界值验证：**
| sensitivity | effectiveDragThreshold (base=3mm, dpi=326) | effectiveFatFingerMargin (base=8dp) |
|-------------|---------------------------------------------|-------------------------------------|
| 0.5×        | 76.8px (2×)                                 | 4dp (0.5×)                          |
| 1.0×        | 38.4px (base)                               | 8dp (base)                          |
| 2.0×        | 19.2px (0.5×)                               | 16dp (2×)                           |

---

## Out of Scope

- [Story 001]: SettingsManager（PlayerPrefs 存储灵敏度值）
- InputService 的 SingleFingerFSM 实现（input-system epic）
- ObjectInteraction 的 Fat Finger 实现（object-interaction epic）
- [Story 007]: 应用生命周期存档

---

## QA Test Cases

### TC-003-01: 灵敏度 2.0× 时更容易触发拖拽
**Given** 灵敏度设为 2.0（最高）
**When** 用极短移动距离（约 10mm）触发拖拽操作
**Then** 拖拽成功触发（阈值变为 base/2）；物件跟随手指移动；速度与 1.0× 相同

### TC-003-02: 灵敏度 0.5× 时短距移动不触发拖拽
**Given** 灵敏度设为 0.5（最低）
**When** 用与 TC-003-01 相同的短移动距离触发操作
**Then** 拖拽不触发（移动距离 < 增大后的阈值）；被识别为 Tap 或 LongPress

### TC-003-03: 灵敏度修改立即生效
**Given** 当前灵敏度为 1.0
**When** 将灵敏度调整为 2.0，立即进行触摸操作
**Then** 第一次触摸即使用新阈值（无需场景切换或重启）

### TC-003-04: 灵敏度不影响移动速度
**Given** 灵敏度分别为 0.5× 和 2.0×
**When** 用相同的手指移动轨迹拖动物件
**Then** 物件在两种灵敏度下移动的距离相同（1:1 跟手）；只有拖拽触发难度不同

### TC-003-05: 灵敏度极端值不崩溃
**Given** 以编程方式将灵敏度设为边界值 0.5 和 2.0
**When** 分别进行正常游戏操作（选中、拖拽、旋转、缩放）
**Then** 所有操作正常响应；无除零异常；无日志 Error

### TC-003-06: 重启后灵敏度保持
**Given** 灵敏度已设为 1.5，关闭应用
**When** 重新启动游戏，打开 SettingsPanel
**Then** 滑块显示 1.5；下一次触摸使用对应阈值

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/Settings/TouchSensitivityTests.cs`
**Status**: [ ] Not yet created

**Test class pattern**:
```csharp
[TestFixture]
public class TouchSensitivityTests
{
    // Mock IInputService, capture runtime dragThreshold values
    // Mock ObjectInteraction, capture fatFingerMargin values
    // Send Evt_SettingChanged events and verify threshold updates
    // Simulate gesture events at boundary thresholds
}
```

---

## Dependencies

- Depends on: Story 001 (SettingsManager), input-system epic story-001 (SingleFingerFSM dragThreshold), object-interaction epic story-005 (Fat Finger selection radius)
- Unlocks: accessibility-requirements.md §3.2 灵敏度调节验收（P0 要求）
