// 该文件由Cursor 自动生成

# Story 007: Settings Save/Load on App Lifecycle Events

> **Epic**: Settings & Accessibility
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/settings-accessibility.md`
**Requirement**: `TR-settings-002`, `TR-settings-003`

**ADR Governing Implementation**: ADR-008: Save System (PlayerPrefs lifecycle)
**ADR Decision Summary**: 设置通过 PlayerPrefs 存储，Unity 在 `OnApplicationQuit` 和部分平台的 `OnApplicationPause(true)` 时自动 flush PlayerPrefs；为确保 iOS/Android 后台切换时设置不丢失，需主动调用 `PlayerPrefs.Save()` 并在应用恢复时重新读取设置并应用到所有子系统。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW

**Control Manifest Rules**:
- Required: `OnApplicationPause(true)` 时调用 `PlayerPrefs.Save()`（确保 iOS/Android 后台 flush）；`OnApplicationQuit` 时调用 `PlayerPrefs.Save()`；应用恢复（`OnApplicationPause(false)`）时重新广播所有设置的 `Evt_SettingChanged`（确保 AudioSystem、InputService 等重新同步设置）；初始化时加载所有设置并立即广播一次（Startup Apply）
- Forbidden: 禁止异步文件 I/O（PlayerPrefs 自身是同步的，在 Pause/Quit 回调中直接调用）；禁止在游戏功能逻辑中直接读取 PlayerPrefs（统一通过 `GameModule.Settings`）
- Guardrail: `PlayerPrefs.Save()` 可能有短暂 I/O 阻塞（<5ms），在 Pause/Quit 回调时可接受

---

## Acceptance Criteria

- [ ] `SettingsManager.Init()` 完成后立即执行 **Startup Apply**：广播所有 8 个设置的 `Evt_SettingChanged` 事件，使 AudioSystem、InputService、ObjectInteraction 等立即同步当前设置值
- [ ] 游戏进入后台（`OnApplicationPause(true)`）时调用 `PlayerPrefs.Save()`，确保设置数据被 flush 到磁盘
- [ ] 应用从后台恢复（`OnApplicationPause(false)`）时：重新读取 PlayerPrefs 中的所有设置值，并重新广播 `Evt_SettingChanged`（处理系统可能清除缓存的极端情况）
- [ ] 游戏退出（`OnApplicationQuit`）时调用 `PlayerPrefs.Save()`
- [ ] 设置在以下场景后保持：进入后台 → 保留在后台 5 分钟 → 恢复
- [ ] 设置在以下场景后保持：iOS 系统内存压力导致应用被后台终止 → 重启 → 设置值与关闭前一致
- [ ] 所有 8 项设置的默认值在 `SettingsKey` 对应的 `DefaultValues` 常量中集中定义
- [ ] `SettingsManager.ApplyAllSettings()` 公开方法：遍历所有 key，读取 PlayerPrefs 并广播 `Evt_SettingChanged`，供外部（如 Startup 和 Resume）统一调用

---

## Implementation Notes

**Startup Apply 流程（在 ProcedureMain 初始化序列中）：**
```csharp
// SettingsManager.Init() 末尾
InitDefaults();      // 首次启动检测系统语言（Story 001）
ApplyAllSettings();  // 广播所有当前设置值
```

**ApplyAllSettings 实现：**
```csharp
public void ApplyAllSettings()
{
    BroadcastSetting(SettingsKey.MasterVolume,     GetFloat(SettingsKey.MasterVolume, 1.0f));
    BroadcastSetting(SettingsKey.MusicVolume,      GetFloat(SettingsKey.MusicVolume, 0.5f));
    BroadcastSetting(SettingsKey.SfxVolume,        GetFloat(SettingsKey.SfxVolume, 0.8f));
    BroadcastSetting(SettingsKey.SfxEnabled,       GetBool(SettingsKey.SfxEnabled, true));
    BroadcastSetting(SettingsKey.HapticEnabled,    GetBool(SettingsKey.HapticEnabled, true));
    BroadcastSetting(SettingsKey.TouchSensitivity, GetFloat(SettingsKey.TouchSensitivity, 1.0f));
    BroadcastSetting(SettingsKey.Language,         GetString(SettingsKey.Language, "en"));
    BroadcastSetting(SettingsKey.TargetFramerate,  GetInt(SettingsKey.TargetFramerate, 60));
}

private void BroadcastSetting(string key, float value) =>
    GameEvent.Send(EventId.Evt_SettingChanged, new SettingChangedPayload { Key = key, FloatValue = value });
```

**应用生命周期处理（SettingsManager MonoBehaviour 或 AppLifecycleHandler）：**
```csharp
private void OnApplicationPause(bool pauseStatus)
{
    if (pauseStatus)
    {
        // 切后台：立即 flush（iOS/Android 后台时间窗很短）
        PlayerPrefs.Save();
    }
    else
    {
        // 恢复前台：重新同步设置
        ApplyAllSettings();
    }
}

private void OnApplicationQuit()
{
    PlayerPrefs.Save();
}
```

**target_framerate 的 Startup Apply：**
```csharp
// AudioSystem、InputService 通过 Evt_SettingChanged 响应
// target_framerate 由 SettingsManager 自身在 BroadcastSetting 时处理
GameEvent.AddEventListener<SettingChangedPayload>(EventId.Evt_SettingChanged, payload => {
    if (payload.Key == SettingsKey.TargetFramerate)
        Application.targetFrameRate = payload.IntValue;
});
```

**初始化顺序（与 Control Manifest §9 对齐）：**
```
Step 9: Input System init
Step 10: SaveSystem.Init() + SaveSystem.LoadAsync()
Step 11: ChapterState.Init(saveData)
Step 12: AudioSystem.Init()            ← 此时还未接收设置
Step 13: SettingsManager.Init()        ← ApplyAllSettings() 广播
           ↑ 在 Step 12 之后，确保 AudioSystem 已注册监听器
Step 14: UI ShowMainMenu
```

---

## Out of Scope

- [Story 001-006]: 各具体设置的读写和实时生效（本 story 只关注 lifecycle 持久化）
- SaveData JSON 的存档（`tutorialCompleted` 等在 save-system epic 处理）
- 云存档同步（P2 后续功能）

---

## QA Test Cases

### TC-007-01: 启动时所有设置自动应用
**Given** PlayerPrefs 中 `master_volume = 0.3`，启动游戏
**When** ProcedureMain 完成初始化（SettingsManager.Init() 执行后）
**Then** AudioSystem 已将 master volume 设为 0.3（通过 Evt_SettingChanged 响应）；`Application.targetFrameRate` = PlayerPrefs 中的值；InputService dragThreshold 使用正确灵敏度

### TC-007-02: 后台切换设置持久化
**Given** `music_volume = 0.6`，应用切到后台（`OnApplicationPause(true)`）
**When** 设备保持后台 5 分钟后恢复
**Then** `PlayerPrefs.GetFloat("music_volume")` = 0.6；AudioSystem 音乐音量恢复为 0.6

### TC-007-03: 应用被 iOS 后台终止后重启
**Given** `touch_sensitivity = 1.5`，iOS 系统内存压力终止应用
**When** 用户重新启动游戏
**Then** `GetFloat(SettingsKey.TouchSensitivity)` = 1.5；InputService 阈值使用 1.5×；SettingsPanel 滑块显示 1.5

### TC-007-04: 应用恢复时重新广播设置
**Given** 应用在后台（理论上设置未变），恢复前台
**When** `OnApplicationPause(false)` 触发
**Then** `Evt_SettingChanged` 被广播 8 次（每个设置 key 一次）；AudioSystem 等系统重新同步

### TC-007-05: 应用退出时写入 PlayerPrefs
**Given** 在游戏中将 `sfx_enabled` 改为 false
**When** 调用 `Application.Quit()`（或模拟 OnApplicationQuit）
**Then** `PlayerPrefs.Save()` 被调用；再次启动后 `sfx_enabled = false`

### TC-007-06: 全部 8 项设置有默认值兜底
**Given** 清空所有 PlayerPrefs（模拟全新安装）
**When** `SettingsManager.Init()` 执行后调用每个 GetXxx
**Then** 返回值均为 GDD 中定义的默认值（master=1.0, music=0.5, sfx=0.8, sfxEnabled=true, haptic=true, sensitivity=1.0, language=系统语言, framerate=60）

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/integration/Settings/SettingsPersistenceTests.cs`
**Status**: [ ] Not yet created

**Test class pattern**:
```csharp
[TestFixture]
public class SettingsPersistenceTests
{
    // Mock PlayerPrefs via IPlayerPrefsProvider interface
    // Simulate OnApplicationPause(true/false) callbacks
    // Verify PlayerPrefs.Save() called at correct lifecycle events
    // Verify Evt_SettingChanged broadcast count and values on startup
    // Verify all 8 settings have correct defaults on fresh install
}
```

---

## Dependencies

- Depends on: Story 001 (SettingsManager base), Story 002-005 (各设置监听 Evt_SettingChanged), audio-system epic (AudioSystem 监听并应用 volume settings), input-system epic (sensitivity 应用)
- Unlocks: Epic 完成（所有 7 个 stories 完成后 settings-accessibility epic DoD 满足）
