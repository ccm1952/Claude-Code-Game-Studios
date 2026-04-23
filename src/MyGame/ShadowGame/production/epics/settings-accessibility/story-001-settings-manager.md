// 该文件由Cursor 自动生成

# Story 001: SettingsManager with PlayerPrefs Persistence

> **Epic**: Settings & Accessibility
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/settings-accessibility.md`
**Requirement**: `TR-settings-001`, `TR-settings-002`, `TR-settings-003`

**ADR Governing Implementation**: ADR-008: Save System (PlayerPrefs for Settings)
**ADR Decision Summary**: 8 项设置值通过 `PlayerPrefs` 存储，完全独立于存档 JSON 文件；设置变更通过 `Evt_SettingChanged(2000)` GameEvent 广播，各系统自行响应；首次启动使用默认值，修改后立即写入 PlayerPrefs；删除存档不影响设置。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW

**Control Manifest Rules (Foundation — Save/Settings)**:
- Required: Settings 存储在 `PlayerPrefs`（不进入 save JSON）；所有 `GameEvent` 使用 Settings 范围 ID（2000-2099）；每个设置变更广播 `Evt_SettingChanged` 携带 `SettingChangedPayload(key, value)`；`DeleteSave()` 不清除 PlayerPrefs；首次启动自动检测系统语言
- Forbidden: 禁止将设置存入 `SaveData` JSON；禁止同步文件 I/O；禁止硬编码默认值（从 Luban `TbDefaultSettings` 或配置常量类读取）
- Guardrail: 修改后立即写入 PlayerPrefs（不防抖，保证实时性）

---

## Acceptance Criteria

- [ ] `SettingsManager` 实现 `ISettingsService` 接口，通过 `GameModule.Settings` 全局访问
- [ ] 管理 8 项设置：`master_volume`(float,default=1.0)、`music_volume`(float,default=0.5)、`sfx_volume`(float,default=0.8)、`sfx_enabled`(bool,default=true)、`haptic_enabled`(bool,default=true)、`touch_sensitivity`(float,0.5-2.0,default=1.0)、`language`(string,default=系统语言→fallback "en")、`target_framerate`(int,30/60,default=60)
- [ ] `GetFloat(string key)`、`GetBool(string key)`、`GetString(string key)`、`GetInt(string key)` 读取方法：PlayerPrefs 有值则返回，无值则返回默认值
- [ ] `SetFloat/SetBool/SetString/SetInt(string key, T value)` 设置方法：立即写入 PlayerPrefs 并广播 `Evt_SettingChanged(key, value)`
- [ ] 首次启动时：检测 `Application.systemLanguage`，映射到 `["zh-CN", "zh-TW", "en", "ja", "ko"]`，无匹配则写入 `"en"` 作为默认值
- [ ] `Evt_SettingChanged` payload 为 `SettingChangedPayload` struct（值类型），包含 `string Key` 和 `object Value`
- [ ] `ISettingsService` 接口暴露 `string[] SupportedLanguages { get; }` 供语言下拉控件使用
- [ ] `SettingsManager.Init()` 在 ProcedureMain 初始化序列中执行，且在任何 UI 系统显示前完成
- [ ] 所有设置 key 定义为 `public const string` 在集中的 `SettingsKey` 常量类中

---

## Implementation Notes

**ISettingsService 接口：**
```csharp
public interface ISettingsService
{
    float GetFloat(string key, float defaultValue);
    bool GetBool(string key, bool defaultValue);
    string GetString(string key, string defaultValue);
    int GetInt(string key, int defaultValue);

    void SetFloat(string key, float value);
    void SetBool(string key, bool value);
    void SetString(string key, string value);
    void SetInt(string key, int value);

    string[] SupportedLanguages { get; }
    void InitDefaults(); // 首次启动检测系统语言
}
```

**SettingsKey 常量类：**
```csharp
public static class SettingsKey
{
    public const string MasterVolume     = "master_volume";
    public const string MusicVolume      = "music_volume";
    public const string SfxVolume        = "sfx_volume";
    public const string SfxEnabled       = "sfx_enabled";
    public const string HapticEnabled    = "haptic_enabled";
    public const string TouchSensitivity = "touch_sensitivity";
    public const string Language         = "language";
    public const string TargetFramerate  = "target_framerate";
}
```

**Evt_SettingChanged payload：**
```csharp
public struct SettingChangedPayload
{
    public string Key;
    public float FloatValue;   // 根据 key 类型使用对应字段
    public bool BoolValue;
    public string StringValue;
    public int IntValue;
}
```

**系统语言映射：**
```csharp
private static readonly Dictionary<SystemLanguage, string> LangMap = new()
{
    { SystemLanguage.ChineseSimplified,  "zh-CN" },
    { SystemLanguage.ChineseTraditional, "zh-TW" },
    { SystemLanguage.Japanese,           "ja" },
    { SystemLanguage.Korean,             "ko" },
    { SystemLanguage.English,            "en" },
};

public void InitDefaults()
{
    if (!PlayerPrefs.HasKey(SettingsKey.Language))
    {
        string lang = LangMap.TryGetValue(Application.systemLanguage, out var l) ? l : "en";
        PlayerPrefs.SetString(SettingsKey.Language, lang);
        PlayerPrefs.Save();
    }
}
```

---

## Out of Scope

- [Story 002]: 音量滑块 UI（读取本 story 的 GetFloat，但 UI 渲染不在此）
- [Story 003]: 灵敏度值如何应用到 InputService（本 story 只负责存储和广播）
- [Story 004]: 振动开关如何控制 Haptic（本 story 只负责存储和广播）
- [Story 005]: 语言切换的具体切换逻辑（本 story 只存储语言 key）
- [Story 007]: 应用生命周期存储（InitDefaults 覆盖首次，生命周期覆盖 Pause/Quit）

---

## QA Test Cases

### TC-001-01: 首次启动系统语言检测
**Given** PlayerPrefs 为空（全新安装），设备系统语言为日语
**When** `SettingsManager.Init()` 执行
**Then** `GetString(SettingsKey.Language)` 返回 `"ja"`；PlayerPrefs 中 `language` key 存在

### TC-001-02: 不支持语言 fallback 到英文
**Given** PlayerPrefs 为空，设备系统语言为 `SystemLanguage.Arabic`
**When** `InitDefaults()` 执行
**Then** `GetString(SettingsKey.Language)` 返回 `"en"`

### TC-001-03: 设置写入并广播
**Given** `SettingsManager` 已初始化
**When** `SetFloat(SettingsKey.MasterVolume, 0.7f)` 被调用
**Then** `PlayerPrefs.GetFloat(SettingsKey.MasterVolume)` = 0.7f；`Evt_SettingChanged` 事件被分发，payload.Key = "master_volume"，payload.FloatValue = 0.7f

### TC-001-04: 读取有默认值兜底
**Given** PlayerPrefs 中不存在 `sfx_volume` key
**When** `GetFloat(SettingsKey.SfxVolume, 0.8f)` 被调用
**Then** 返回 0.8f（默认值）；不抛异常

### TC-001-05: 删除存档不影响设置
**Given** `master_volume` = 0.5（已写入 PlayerPrefs），调用 `ISaveService.DeleteSave()`
**When** `GetFloat(SettingsKey.MasterVolume, 1.0f)` 被调用
**Then** 返回 0.5f（PlayerPrefs 未被清除）

### TC-001-06: 多次设置同一 key 不累积
**Given** `master_volume` 初始为 1.0
**When** 连续调用 `SetFloat("master_volume", 0.3f)` 和 `SetFloat("master_volume", 0.7f)`
**Then** `GetFloat("master_volume")` 返回 0.7f（最后一次覆盖）；`Evt_SettingChanged` 被广播 2 次

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/unit/Settings/SettingsManagerTests.cs`
**Status**: [ ] Not yet created

**Test class pattern**:
```csharp
[TestFixture]
public class SettingsManagerTests
{
    // Clear PlayerPrefs before each test
    // Mock Application.systemLanguage via wrapper interface
    // Verify Get/Set round-trip, event broadcast, language detection
}
```

---

## Dependencies

- Depends on: save-system story-001 (ADR-008 定义了 PlayerPrefs 与 SaveData 的分离约定)
- Unlocks: Story 002-007（所有设置功能依赖本 story 的 ISettingsService 基础设施）
