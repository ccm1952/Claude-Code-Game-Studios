// 该文件由Cursor 自动生成

# Story 005: Runtime Language Switching via ILocalizationModule

> **Epic**: Settings & Accessibility
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/settings-accessibility.md`
**Requirement**: `TR-settings-005`, `TR-settings-006` (P1 — validated by SP-009)

**ADR Governing Implementation**: ADR-008: Save System (PlayerPrefs for language); SP-009: I2 Localization
**ADR Decision Summary**: 语言设置存储在 PlayerPrefs（`language` key）；语言切换通过 `GameModule.Localization.SetLanguage(languageCode)` 实现（TEngine 封装的 I2 Localization）；切换后所有 UI 文本立即热更新，无需重启；禁止直接使用 I2 命名空间或 `LocalizationManager`（SP-009 验证）。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW (SP-009 已验证 TEngine I2 Localization 封装可用)

**Control Manifest Rules (SP-009)**:
- Required: 使用 `GameModule.Localization`（`ILocalizationModule`）访问所有本地化功能；语言切换：`GameModule.Localization.SetLanguage("Chinese (Simplified)")` 等；UI 文本本地化使用 TEngine Localization Localize 组件
- Forbidden: 禁止 `using I2.Loc`（I2 命名空间不存在于本项目）；禁止直接调用 `LocalizationManager`；禁止直接调用 `LocalizationManager.CurrentLanguage`

---

## Acceptance Criteria

- [ ] `SettingsPanel` 中的 `LanguageDropdown`（或 Button 组）显示支持的语言列表：简体中文、繁体中文、English、日本語、한국어
- [ ] `OnRefresh()` 时从 `GameModule.Settings.GetString(SettingsKey.Language, "zh-CN")` 读取当前语言并高亮对应选项
- [ ] 用户选择语言时：`GameModule.Settings.SetString(SettingsKey.Language, langCode)` 写入 PlayerPrefs；调用 `GameModule.Localization.SetLanguage(I2LangName(langCode))` 执行热切换；广播 `Evt_SettingChanged(language, langCode)`
- [ ] 语言切换后当前打开的所有 UIWindow 文本**立即**更新（同帧或下一帧，通过 TEngine Localize 组件自动刷新）
- [ ] 语言切换后新打开的 UIWindow 使用新语言（通过 `GameModule.Localization.GetString(key)` 读取时自动使用当前语言）
- [ ] `langCode` → I2 Language Name 映射表在代码中集中定义（`LocalizationMapping` 常量类）
- [ ] 快速连续切换语言（如 2 次切换在 1 帧内）以最后一次为准，不崩溃
- [ ] 不支持的 `langCode` fallback 到 `"en"` 并记录 `Log.Warning`

---

## Implementation Notes

**langCode → I2 Language Name 映射（SP-009 已验证 TEngine 内 I2 语言名称）：**
```csharp
public static class LocalizationMapping
{
    private static readonly Dictionary<string, string> CodeToI2Name = new()
    {
        { "zh-CN", "Chinese (Simplified)" },
        { "zh-TW", "Chinese (Traditional)" },
        { "en",    "English" },
        { "ja",    "Japanese" },
        { "ko",    "Korean" },
    };

    public static string GetI2Name(string langCode)
    {
        if (CodeToI2Name.TryGetValue(langCode, out var name)) return name;
        Log.Warning($"[Localization] Unknown lang code: {langCode}, fallback to English");
        return "English";
    }
}
```

**语言切换流程（SettingsPanel 中调用）：**
```csharp
private void OnLanguageSelected(string langCode)
{
    // 1. 写入 PlayerPrefs
    GameModule.Settings.SetString(SettingsKey.Language, langCode);
    
    // 2. 通知 TEngine I2 Localization 模块热切换
    string i2Name = LocalizationMapping.GetI2Name(langCode);
    GameModule.Localization.SetLanguage(i2Name);
    
    // 3. Evt_SettingChanged 已在 SetString 内广播，UI 通过 TEngine Localize 组件自动刷新
}
```

**UI 文本本地化（所有 UIWindow 使用）：**
```csharp
// 方式 1: TEngine Localize 组件（挂在 TextMeshPro GameObject 上，自动响应语言切换）
// 方式 2: 手动查询
string text = GameModule.Localization.GetString("settings_master_volume");
```

**首次启动语言检测（Story 001 中实现，本 Story 集成验证）：**
```
Application.systemLanguage → LangMap → SetString(language, code) → SetLanguage(i2Name)
```

**注意**：YooAsset 环境下语言资源加载已由 SP-009 验证可用（语言文件通过 YooAsset 资源包管理）。

---

## Out of Scope

- [Story 001]: SettingsManager（PlayerPrefs 语言 key 存储）
- 语言资源文件的制作（本地化 CSV/JSON 文件由文案负责）
- YooAsset 语言资源包的分包策略（语言 P2 阶段决策 ADR-022）
- RTL 语言支持（阿拉伯语等 P2）

---

## QA Test Cases

### TC-005-01: 语言切换后文本立即更新
**Given** 游戏当前显示简体中文，SettingsPanel 打开
**When** 选择语言 "English"
**Then** SettingsPanel 内所有文本立即变为英文；游戏其他已打开 UIWindow 文本也立即更新（同帧或下一帧）

### TC-005-02: 重启后语言保持
**Given** 切换语言为日语，关闭应用
**When** 重新启动游戏
**Then** 游戏以日语启动；`PlayerPrefs.GetString("language")` = "ja"

### TC-005-03: 首次启动自动匹配系统语言
**Given** 全新安装，设备系统语言为韩语
**When** 首次启动游戏
**Then** UI 以韩语显示；SettingsPanel 中语言选项高亮韩语

### TC-005-04: 不支持语言 fallback 英文
**Given** 设备系统语言为 Arabic
**When** 首次启动
**Then** UI 以英文显示；`Log.Warning` 包含 fallback 信息

### TC-005-05: 快速连续切换不崩溃
**Given** SettingsPanel 打开
**When** 快速依次点击：简体中文 → 英文 → 日文（同帧内）
**Then** 最终语言为日文；无崩溃；无异常日志

### TC-005-06: 新打开窗口使用当前语言
**Given** 语言已切换为英文，SettingsPanel 关闭
**When** 打开 PauseMenuPanel（一个新 UIWindow）
**Then** PauseMenuPanel 显示英文文本；不显示旧语言

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/Settings/LanguageSwitchTests.cs`
**Status**: [ ] Not yet created

**Test class pattern**:
```csharp
[TestFixture]
public class LanguageSwitchTests
{
    // Mock ILocalizationModule, verify SetLanguage called with correct I2 name
    // Mock ISettingsService, verify PlayerPrefs key written
    // Simulate rapid language switches, verify no crash
    // Verify UI text update on next OnRefresh
}
```

---

## Dependencies

- Depends on: Story 001 (SettingsManager GetString/SetString), SP-009 验证结论 (ILocalizationModule API), ui-system epic (TEngine Localize 组件)
- Unlocks: 所有 UIWindow 的多语言文本显示；tutorial-onboarding story-004 (提示文字本地化)
