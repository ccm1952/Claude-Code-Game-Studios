// 该文件由Cursor 自动生成

# Story 004: Haptic Vibration On/Off Toggle

> **Epic**: Settings & Accessibility
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/settings-accessibility.md`
**Requirement**: `TR-settings-001`

**ADR Governing Implementation**: ADR-008: Save System (PlayerPrefs); ADR-013: Object Interaction
**ADR Decision Summary**: `haptic_enabled` bool 存储在 PlayerPrefs；Object Interaction 系统在触发触觉反馈前检查 `Settings.haptic_enabled`；通过 `Evt_SettingChanged(haptic_enabled)` 广播变更；关闭后立即生效（正在进行的振动不中断，后续触发全部禁止）。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: `haptic_enabled` 存储在 PlayerPrefs（key: `"haptic_enabled"`）；Object Interaction 系统通过 `GameModule.Settings.GetBool(SettingsKey.HapticEnabled)` 查询（每次触发前查询，不缓存）；`Evt_SettingChanged` 广播后各系统自行响应；Control Manifest §3.2 规定"Haptic feedback gated by `Settings.haptic_enabled`"
- Forbidden: 禁止直接调用 `Handheld.Vibrate()` 绕过 haptic_enabled 检查；禁止 haptic_enabled = false 时中断当前正在进行的振动（不作强制停止）
- Platform note: Unity `Handheld.Vibrate()` 是移动端专用 API；PC 平台下为空操作（no-op），`haptic_enabled` 设置对 PC 无实际作用但不产生错误

---

## Acceptance Criteria

- [ ] `SettingsPanel` 中的 `HapticEnabledToggle` 在 `OnRefresh()` 时从 `GameModule.Settings.GetBool(SettingsKey.HapticEnabled, true)` 初始化
- [ ] Toggle 变更时调用 `GameModule.Settings.SetBool(SettingsKey.HapticEnabled, value)` 并广播 `Evt_SettingChanged`
- [ ] 关闭振动后：所有新触发的 Haptic 操作（物件选中、吸附、拼图完成等）不再调用 `Handheld.Vibrate()`
- [ ] 关闭振动后：正在进行中的振动不被中断（仅控制后续触发）
- [ ] Object Interaction 系统在每次触发触觉前通过 `GameModule.Settings.GetBool(SettingsKey.HapticEnabled)` 实时查询（不缓存，保证设置变更即刻生效）
- [ ] 重启游戏后 `haptic_enabled` 设置保持（PlayerPrefs 持久化）
- [ ] PC 平台上 Toggle 显示无异常（`Handheld.Vibrate()` 在 PC 为 no-op）

---

## Implementation Notes

**Object Interaction 中的 Haptic 触发检查（本 Story 定义规范，Object Interaction epic 实现）：**
```csharp
// 触觉反馈调用点（示例：物件选中时）
private void TriggerSelectionHaptic()
{
    if (!GameModule.Settings.GetBool(SettingsKey.HapticEnabled, true)) return;
    Handheld.Vibrate(); // 仅移动端有效，PC 为 no-op
}
```

**Toggle UI 更新：**
```csharp
// SettingsPanel.OnRefresh()
_hapticToggle.isOn = GameModule.Settings.GetBool(SettingsKey.HapticEnabled, true);

// 监听 Toggle 变更
_hapticToggle.onValueChanged.AddListener(v =>
    GameModule.Settings.SetBool(SettingsKey.HapticEnabled, v));
```

**各 Haptic 触发点清单（需在 Object Interaction epic 实现时确保每处都有 haptic_enabled 检查）：**
| 触发点 | 系统 | 触感类型 |
|--------|------|---------|
| 物件被 Tap 选中 | Object Interaction | 轻点（short） |
| 物件吸附到格点 | Object Interaction | 中等（medium） |
| 谜题 PerfectMatch | Shadow Puzzle | 强烈（heavy） |
| 教学步骤完成 | Tutorial | 轻点（short） |

**关于"不中断当前振动"：**
`Handheld.Vibrate()` 在 iOS/Android 上触发的是系统级单次振动，无"停止"API。因此"不中断"是自然行为——设置变更只控制下次调用，无需额外代码。

---

## Out of Scope

- [Story 001]: SettingsManager（PlayerPrefs 存储）
- 各系统 Haptic 触发点的具体实现（各自 epic 负责确保 haptic_enabled 检查）
- 高级触觉反馈（如 iOS Core Haptics API）——MVP 使用基础 `Handheld.Vibrate()`
- [Story 007]: 应用生命周期存档

---

## QA Test Cases

### TC-004-01: 关闭振动后无新触觉触发
**Given** `haptic_enabled = true`，关闭 HapticEnabledToggle（设为 false）
**When** 触摸选中游戏物件（触发物件选中逻辑）
**Then** `Handheld.Vibrate()` 未被调用；设备无振动感

### TC-004-02: 开启振动后恢复触觉
**Given** `haptic_enabled = false`
**When** 开启 HapticEnabledToggle（设为 true），触摸选中物件
**Then** `Handheld.Vibrate()` 被调用；设备振动

### TC-004-03: 设置重启后保持
**Given** 将 `haptic_enabled` 设为 false，重启应用
**When** 打开 SettingsPanel
**Then** `HapticToggle` 显示为 off；振动仍被禁用

### TC-004-04: 事件广播
**Given** 监听 `Evt_SettingChanged`
**When** 关闭 HapticToggle
**Then** `Evt_SettingChanged` 被发送，payload.Key = "haptic_enabled"，payload.BoolValue = false

### TC-004-05: PC 平台 no-op（非崩溃）
**Given** 在 PC Editor 中运行
**When** 开启/关闭 HapticToggle，并触发物件交互
**Then** 无异常、无报错；Toggle 正常显示；`Handheld.Vibrate()` 调用被 Unity 静默处理

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/Settings/HapticToggleTests.cs`
**Status**: [ ] Not yet created

**Test class pattern**:
```csharp
[TestFixture]
public class HapticToggleTests
{
    // Mock ISettingsService, verify haptic_enabled read/write
    // Mock Handheld.Vibrate via wrapper interface
    // Verify Object Interaction checks haptic_enabled before each Vibrate call
}
```

---

## Dependencies

- Depends on: Story 001 (SettingsManager GetBool/SetBool), object-interaction epic (Haptic 触发点的 haptic_enabled 检查)
- Unlocks: Story 007 (应用生命周期存档), accessibility-requirements.md §3.1 触觉相关验收
