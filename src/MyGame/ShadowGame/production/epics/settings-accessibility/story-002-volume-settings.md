// 该文件由Cursor 自动生成

# Story 002: Master/Music/SFX Volume Sliders with Real-Time Preview

> **Epic**: Settings & Accessibility
> **Status**: Ready
> **Layer**: Presentation
> **Type**: UI
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/settings-accessibility.md`
**Requirement**: `TR-settings-001`, `TR-settings-003`, `TR-settings-008`

**ADR Governing Implementation**: ADR-011: UIWindow Management; ADR-017: Audio Mix
**ADR Decision Summary**: 音量滑块修改立即通知 AudioSystem（通过 `Evt_SettingChanged` 广播，AudioSystem 监听后调用 `IAudioService` 更新音量）；3 个独立音量层：Master / Music / SFX；`sfx_enabled` toggle 控制 SFX 层（Ambient 层**不受影响**）；音量公式 `finalVolume = clipBase × layer × master × ducking`；滑块采用 UGUI Slider 组件。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW

**Control Manifest Rules (Presentation — UIWindow + Audio)**:
- Required: 继承 `UIWindow` 或作为 `UIWidget` 嵌入 SettingsPanel；`SetUISafeFitHelper` 在根 Canvas；CanvasScaler 1920×1080，match 0.5；slider 修改立即通过 `GameModule.Settings.SetFloat()` 写入并广播；AudioSystem 监听 `Evt_SettingChanged` 响应（SettingsPanel 不直接调用 AudioModule）；`sfx_enabled = false` 时 Ambient 层不受影响
- Forbidden: 禁止 SettingsPanel 直接调用 `IAudioService` 或 `GameModule.Audio`（通过 GameEvent 解耦）；禁止 UI Toolkit；禁止同步加载 UI 预制体
- Guardrail: UI 系统每帧 < 0.5ms；最小触摸目标 44dp（Slider handle ≥ 44dp）

---

## Acceptance Criteria

- [ ] `SettingsPanel` UIWindow 包含 3 个音量 Slider：`MasterVolumeSlider`(0-1)、`MusicVolumeSlider`(0-1)、`SfxVolumeSlider`(0-1)
- [ ] 包含 1 个音效开关 Toggle：`SfxEnabledToggle`
- [ ] `OnRefresh()` 时从 `GameModule.Settings.GetFloat/GetBool()` 读取当前值并初始化控件显示
- [ ] 滑块拖动时每帧调用 `GameModule.Settings.SetFloat(SettingsKey.MasterVolume, value)` 写入并广播 `Evt_SettingChanged`
- [ ] 音量变化实时反馈：拖动 Master 滑块时当前播放的音频音量立即变化（通过 AudioSystem 监听 `Evt_SettingChanged` 实现）
- [ ] SFX 开关关闭时：`Evt_SettingChanged(sfx_enabled=false)` 广播；SFX 和 Ambient 层中，只有 SFX 层静音，Ambient 层保持正常音量
- [ ] SFX 开关关闭时 Slider 可视禁用（降低 alpha），提示当前音效已关闭，但不阻止调整数值
- [ ] 滑块拖出组件区域（手指滑出边界）时保持最后有效值，不跳变到 0 或 1
- [ ] 所有 Label 文字通过本地化 key 显示（支持语言切换）
- [ ] 每个 Slider 的 Handle 尺寸 ≥ 44×44dp（符合无障碍要求）

---

## Implementation Notes

**SettingsPanel 架构（嵌套 UIWidget）：**
```
SettingsPanel (UIWindow, Popup 层)
  └── VolumeSettingsWidget (UIWidget)
        ├── MasterVolumeRow (Label + Slider)
        ├── MusicVolumeRow  (Label + Slider)
        ├── SfxVolumeRow    (Label + Slider + 状态提示)
        └── SfxEnabledRow   (Label + Toggle)
```

**Slider 数据绑定（UGUI Slider.onValueChanged）：**
```csharp
_masterSlider.onValueChanged.AddListener(v =>
    GameModule.Settings.SetFloat(SettingsKey.MasterVolume, v));

_sfxToggle.onValueChanged.AddListener(v =>
    GameModule.Settings.SetBool(SettingsKey.SfxEnabled, v));
```

**注意**：SettingsPanel **不直接调用** `GameModule.Audio`。AudioSystem 监听 `Evt_SettingChanged`：
```csharp
// AudioSystem 内（不在本 Story 实现）
GameEvent.AddEventListener<SettingChangedPayload>(EventId.Evt_SettingChanged, payload => {
    if (payload.Key == SettingsKey.MasterVolume)
        _audioService.SetMasterVolume(payload.FloatValue);
    else if (payload.Key == SettingsKey.SfxEnabled && !payload.BoolValue)
        _audioService.MuteSfxLayer(); // Ambient 不受影响！
});
```

**sfx_enabled 关闭时的视觉禁用：**
```csharp
private void UpdateSfxSliderInteractable(bool sfxEnabled)
{
    _sfxVolumeSlider.interactable = sfxEnabled;
    var canvasGroup = _sfxVolumeSlider.GetComponent<CanvasGroup>();
    canvasGroup.alpha = sfxEnabled ? 1.0f : 0.5f;
}
```

**音量公式（来自 ADR-017）：**
```
finalVolume = clipBaseVolume × layerVolume × masterVolume × duckingMultiplier
```
SettingsPanel 控制 `masterVolume`、`layerVolume(music/sfx)` 的输入值；具体乘法在 AudioSystem 内。

---

## Out of Scope

- [Story 001]: SettingsManager（PlayerPrefs 存储）
- AudioSystem 对 volume 变更的响应实现（在 audio-system epic 中）
- [Story 003]: 触控灵敏度 Slider（独立 story）
- [Story 007]: 应用生命周期存档（OnPause/Quit 时确保 PlayerPrefs 已写入）

---

## QA Test Cases

### TC-002-01: 滑块拖动实时改变音量
**Setup** 打开 SettingsPanel，当前有音乐播放，`master_volume` = 1.0
**Verify** 拖动 MasterVolumeSlider 到 0.3
**Pass** 音乐音量立即降低（可听到变化）；`PlayerPrefs.GetFloat("master_volume")` = 0.3；UI 滑块位置 = 30%

### TC-002-02: 关闭 sfx_enabled 不影响 Ambient 音
**Setup** 场景中同时有 SFX 音效和 Ambient 背景音在播放
**Verify** 点击 SfxEnabledToggle 关闭 SFX
**Pass** SFX 音效静音；Ambient 背景音继续播放；`sfx_enabled = false` 写入 PlayerPrefs

### TC-002-03: 重新打开 SettingsPanel 恢复控件值
**Setup** 设置 `music_volume = 0.3`，关闭 SettingsPanel，重新打开
**Verify** `OnRefresh()` 被调用
**Pass** MusicVolumeSlider 位置为 30%；值正确从 PlayerPrefs 加载

### TC-002-04: SFX 关闭时 Slider 视觉禁用
**Setup** 打开 SettingsPanel
**Verify** 关闭 SfxEnabledToggle
**Pass** SfxVolumeSlider alpha = 0.5（视觉变暗）；Slider 仍可触摸调整数值；数值写入不阻止

### TC-002-05: 滑块边界值不越界
**Setup** 拖动 MasterVolumeSlider 到最左端（0.0）
**Verify** 继续向左拖
**Pass** 值保持 0.0，不变负数；音频系统不崩溃

### TC-002-06: 44dp 触摸目标
**Setup** 在实际设备（iPhone 15）上测试
**Verify** 使用大手指尝试点击 Slider handle
**Pass** 每个 Slider handle 可被轻松触摸（视觉尺寸 ≥ 44pt）；无误触

### TC-002-07: 语言切换后标签更新
**Setup** SettingsPanel 处于中文显示状态
**Verify** 切换语言到英文（通过 Story 005 语言切换）
**Pass** "全局音量" → "Master Volume" 等标签立即更新

---

## Test Evidence

**Story Type**: UI
**Required evidence**: `production/qa/evidence/settings/volume-settings-evidence.md`
**Status**: [ ] Not yet created

**Evidence checklist**:
- [ ] 截图：SettingsPanel 初始状态（各分辨率）
- [ ] 截图：SFX 关闭时 Slider 视觉降级状态
- [ ] 录屏：滑块拖动时音量实时变化
- [ ] 截图：iPhone SE / iPhone 15 / iPad 分辨率下布局
- [ ] 触摸目标测试截图（44dp 验证）

---

## Dependencies

- Depends on: Story 001 (SettingsManager PlayerPrefs), ui-system epic (UIWindow/UIWidget), audio-system epic (IAudioService 监听 Evt_SettingChanged)
- Unlocks: Story 007 (应用暂停时需要 PlayerPrefs 已有有效值)
