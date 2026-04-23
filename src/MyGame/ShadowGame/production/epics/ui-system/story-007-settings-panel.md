// 该文件由Cursor 自动生成

# Story: SettingsPanel Window (Volume, Sensitivity, Language)

> **Epic**: ui-system
> **Story ID**: ui-system-007
> **Story Type**: UI
> **GDD Requirement**: TR-ui-005 (9 UIWindows), TR-ui-003 (Popup auto InputBlocker)
> **ADR References**: ADR-011 (UIWindow Management), ADR-008 (Save System), SP-009 (I2 Localization)
> **Sprint**: TBD
> **Status**: Ready (依赖 ui-system-001)

## Context

SettingsPanel 是玩家配置游戏参数的 Popup 面板（Popup 层），包含：音量控制（master/music/sfx 滑块 + sfx_enabled 开关）、触控灵敏度（Sensitivity 滑块）、语言选择（Dropdown）。所有设置存储于 PlayerPrefs（独立于 save.json），实时生效——调节滑块时即时触发 `Evt_SettingChanged` 事件，Audio System 实时响应。

## Acceptance Criteria

- [ ] `SettingsPanel` 继承 `UIWindow`，注册到 Popup 层（UILayer.Popup，sorting order base = 200）
- [ ] `OnCreate()`：绑定所有控件引用（3 个 Slider，1 个 Toggle，1 个 Dropdown，1 个 Close 按钮）
- [ ] `OnRefresh()`：从 PlayerPrefs 读取当前设置值，初始化所有控件显示状态
- [ ] Master Volume 滑块（0-1）：拖动时即时发送 `Evt_SettingChanged { key="master_volume", value=x }`，并写入 `PlayerPrefs.SetFloat("master_volume", x)`
- [ ] Music Volume 滑块（0-1）：同上，key = `"music_volume"`
- [ ] SFX Volume 滑块（0-1）：同上，key = `"sfx_volume"`
- [ ] SFX Enabled Toggle：切换时发送 `Evt_SettingChanged { key="sfx_enabled", value=bool }`，写入 `PlayerPrefs.SetInt("sfx_enabled", bool ? 1 : 0)`
- [ ] Sensitivity 滑块（范围从 Luban TbInputConfig 读取）：发送 `Evt_SettingChanged { key="sensitivity", value=x }`
- [ ] Language Dropdown：选择语言后调用 `GameModule.Localization.SetLanguage(languageName)`（SP-009 规范，禁止 `using I2.Loc`）
- [ ] Close 按钮：`GameModule.UI.CloseWindow<SettingsPanel>()`
- [ ] Auto InputBlocker：打开时自动 PushBlocker；关闭时自动 PopBlocker
- [ ] Android back button 关闭面板
- [ ] `OnClose()`：`PlayerPrefs.Save()` 确保持久化，注销事件监听

## Implementation Notes

- 类路径：`Assets/GameScripts/HotFix/GameLogic/UI/SettingsPanel.cs`
- Language Dropdown：支持语言列表（如 `["Chinese (Simplified)", "English"]`）从配置读取（非硬编码），通过 `GameModule.Localization.SetLanguage()` 切换（SP-009）
- 控件事件监听使用 `AddUIEvent()` 或 UnityEngine.UI 事件（OnValueChanged）——在 OnCreate 绑定，OnClose 解绑
- Sensitivity 滑块范围从 Luban `TbInputConfig.sensitivityMin` / `sensitivityMax` 读取
- 禁止 `using I2.Loc` 或 `LocalizationManager.CurrentLanguage` 直接访问

## Out of Scope

- 语言切换后 UI 文本的自动更新（story-010 本地化绑定）
- 无障碍设置（settings-accessibility epic）
- 性能质量等级设置（P2，ADR-018）

## QA Test Cases (Visual/UI)

### Setup
- 游戏运行中，PlayerPrefs 无存储值（使用默认值）
- SettingsPanel 通过 ShowWindow 打开

### Verify
- 面板在 Popup 层显示，所有控件可见
- 滑块初始值：master/music/sfx = 1.0（默认），SFX Toggle = on
- 拖动 Music Volume 滑块至 0.5 → Music AudioSource.volume 即时变化
- SFX Toggle 关闭 → SFX 静音，Ambient 继续播放
- 语言切换为 English → UI 文本语言切换（story-010 集成后验证）
- Close 按钮 → 面板关闭，InputBlocker 弹出
- 重新打开 SettingsPanel → 控件值显示为上次设置值（PlayerPrefs 持久化正确）

### Pass
- 所有 Verify 项通过，无控制台 I2 命名空间错误

## Test Evidence Path

`production/qa/evidence/ui-system/settings-panel-evidence.md`

## Dependencies

- ui-system-001: UIModule 基础（Popup 层，Auto InputBlocker）
- audio-system story-007: Volume Control（Evt_SettingChanged 消费方）
- SP-009: I2 Localization（GameModule.Localization.SetLanguage 调用规范）
- ADR-008: PlayerPrefs 作为 Settings 存储（独立于 save.json）
