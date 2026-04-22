<!-- 该文件由Cursor 自动生成 -->
# Settings & Accessibility — 设置与无障碍系统

> **Status**: Draft
> **Author**: UX Design Agent
> **Last Updated**: 2026-04-21
> **Last Verified**: 2026-04-21
> **Implements Pillar**: 日常即重量 — 让每个玩家都能以舒适的方式触碰这些物件

## Summary

设置系统管理玩家可调节的偏好选项：音量控制（全局/音乐/音效）、音效开关、触控灵敏度、振动开关、语言本地化选择、帧率选项。所有设置通过 TEngine 的本地化方案支持多语言，设置值持久化到本地存储，修改后实时生效。系统通过 UIModule 提供标准的设置界面，不包含游戏进度相关的功能（存档管理由 Save System 负责）。

> **Quick reference** — Layer: `Presentation` · Priority: `Vertical Slice` · Key deps: `Save System, UI System, Audio System`

## Overview

玩家通过暂停菜单进入设置界面。界面简洁——一页滑动条和开关，没有需要理解的复杂选项。音量可以分别调节全局、音乐和音效；触控灵敏度让小屏幕和大屏幕的操作感一致；振动开关让不喜欢 Haptic 的玩家可以关闭；语言切换在首次启动时自动匹配系统语言，也可以手动切换；帧率选项（30/60fps）让玩家在性能和续航之间选择。设置界面从第一天就支持多语言——这不是"后续本地化"，而是基础架构。

## Player Fantasy

**"打开设置，调两下就好了，不需要翻页。"**

## Detailed Design

### Core Rules

**设置项定义：**

| Setting ID | Category | Type | Default | Range/Options | Description |
|------------|----------|------|---------|---------------|-------------|
| `master_volume` | 音频 | float slider | 1.0 | 0-1 | 全局主音量 |
| `music_volume` | 音频 | float slider | 0.5 | 0-1 | 氛围音乐音量 |
| `sfx_volume` | 音频 | float slider | 0.8 | 0-1 | 音效音量（含环境音） |
| `sfx_enabled` | 音频 | bool toggle | true | on/off | 音效总开关（关闭后静音 SFX + Ambient 层） |
| `haptic_enabled` | 触觉 | bool toggle | true | on/off | 振动反馈开关 |
| `touch_sensitivity` | 操作 | float slider | 1.0 | 0.5-2.0 | 触控灵敏度倍率，影响 dragThreshold 和 fatFingerMargin |
| `language` | 本地化 | enum dropdown | 系统语言 | zh-CN, zh-TW, en, ja, ko (可扩展) | 游戏语言 |
| `target_framerate` | 性能 | enum toggle | 60 | 30 / 60 | 目标帧率 |

**设置存储：**

1. 设置值以 key-value 形式存储在本地（PlayerPrefs 或 TEngine 提供的本地存储方案）
2. 设置与游戏存档分离——重装游戏后设置丢失但不影响游戏进度
3. 每次修改设置时立即写入存储，不需要"保存"按钮
4. 游戏启动时加载设置，若无存储值则使用默认值

**实时生效规则：**

1. 音量修改 → 立即通知 Audio System 更新对应层音量
2. 音效开关 → 立即静音/恢复 SFX 和 Ambient 层
3. 振动开关 → 立即影响后续 Haptic 触发（已在播放的振动不中断）
4. 触控灵敏度 → 立即更新 InteractionConfig 的 dragThreshold 和 fatFingerMargin
5. 语言切换 → 立即刷新当前所有 UI 文本（通过 TEngine 本地化系统的热切换机制）
6. 帧率切换 → 立即设置 `Application.targetFrameRate`

**语言本地化方案：**

1. 使用 TEngine 框架内置的本地化方案
2. 首次启动时读取 `Application.systemLanguage`，映射到支持的语言列表
3. 若系统语言不在支持列表中，默认使用 `en`（英文）
4. 语言切换后所有 UI 文本立即更新，不需要重启
5. 本地化文本存储格式遵循 TEngine 的标准（通常为 JSON 或 CSV 语言表）
6. 新增语言只需添加语言表文件和在支持列表中注册

**触控灵敏度实现：**

1. `touch_sensitivity` 作为倍率应用于 InteractionConfig 的关键参数：
   - 实际 dragThreshold = 配置值 / touch_sensitivity（灵敏度高 → 阈值低 → 更容易触发拖拽）
   - 实际 fatFingerMargin = 配置值 × touch_sensitivity（灵敏度高 → 补偿大 → 更容易选中）
2. 灵敏度不影响物件移动速度——移动始终 1:1 跟手

**帧率选项：**

1. 30fps 模式：降低 GPU 负载，延长续航，适合低端设备或省电场景
2. 60fps 模式：流畅操作体验，推荐默认使用
3. 切换后立即调用 `Application.targetFrameRate = value`
4. 物件交互和动画使用 `Time.deltaTime`，不受帧率变化影响

**设置界面布局：**

```
┌─────────────────────────────┐
│          设    置            │
├─────────────────────────────┤
│  🔊 全局音量      ═══●══    │
│  🎵 音乐音量      ══●═══    │
│  🔔 音效音量      ════●═    │
│  🔔 音效开关         [ON]   │
│  📳 振动反馈         [ON]   │
│  👆 触控灵敏度    ═══●══    │
│  🌐 语言         [简体中文]  │
│  ⚡ 帧率            [60]    │
├─────────────────────────────┤
│  📖 操作指南                 │
├─────────────────────────────┤
│        [返 回]               │
└─────────────────────────────┘
```

1. 单页滚动布局，不分 Tab
2. 每个设置项：标签（本地化文本）+ 控件（Slider / Toggle / Dropdown）
3. 底部有"操作指南"入口（链接到 Tutorial 系统的操作说明页）
4. "返回"按钮关闭设置界面，回到暂停菜单

### States and Transitions

**设置界面状态：**

| State | Entry Condition | Exit Condition | Behavior |
|-------|----------------|----------------|----------|
| **Closed** | 默认 / 玩家点击返回 | 暂停菜单中点击"设置" | 设置界面不显示 |
| **Open** | 从暂停菜单打开 | 点击"返回" / 按下返回键 | 显示设置界面，玩家可调节所有设置项 |

### Interactions with Other Systems

**与 Audio System 的交互：**

- 音量修改 → `AudioVolumeChangeEvent(layer, value)`
- 音效开关 → `AudioMuteEvent(SFX, muted)`（仅控制 SFX 层；Ambient 层不受 sfx_enabled 影响，跟随 master_volume 独立控制。设计意图：环境音"构成安静本身"，即使关闭音效也不应让房间完全无声）

**与 Object Interaction System 的交互：**

- 触控灵敏度修改 → 更新 InteractionConfig 中的参数

**与 Input System 的交互：**

- 振动开关 → 设置全局 Haptic 启用标记

**与 UI System 的交互：**

- 设置界面通过 UIModule 的 UIWindow 管理
- 语言切换 → 触发 UI 文本热刷新

**与 Tutorial System 的交互：**

- "操作指南"入口打开 Tutorial 提供的操作说明页

**与 Save System / 本地存储 的交互：**

- 读写设置值到本地存储
- 设置与游戏进度存档分离

**与 TEngine 本地化系统 的交互：**

- 语言切换委托给 TEngine 本地化模块执行
- 首次启动的系统语言检测

## Formulas

### Touch Sensitivity Application — 触控灵敏度应用

```
effectiveDragThreshold = baseDragThreshold / touchSensitivity
effectiveFatFingerMargin = baseFatFingerMargin × touchSensitivity
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| baseDragThreshold | float | 2.0-5.0mm | InteractionConfig | 配置的基础拖拽阈值 |
| baseFatFingerMargin | float | 4-16dp | InteractionConfig | 配置的基础胖手指补偿 |
| touchSensitivity | float | 0.5-2.0 | Settings | 玩家设置的灵敏度倍率 |

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|----------|------------------|-----------|
| 首次启动时系统语言为不支持的语言 | 默认使用英文 | 英文作为通用 fallback |
| 音量滑动过程中手指滑出滑动条 | 保持最后一次有效的音量值 | 防止意外跳到极端值 |
| 快速连续切换语言 | 以最后一次切换为准，中间切换被覆盖 | 无需等待上一次切换完成 |
| 灵敏度设为最低（0.5）时操作不灵敏 | dragThreshold 加倍但仍在可用范围内 | 最低灵敏度仍要可操作 |
| 30fps 模式下物件拖拽 | 物件仍跟手（使用 deltaTime），但视觉流畅度降低 | 功能不受帧率影响 |
| 设置存储写入失败 | 当次生效但下次启动恢复默认值，Log.Warning | 设置丢失不影响游戏功能 |
| 演出期间打开设置 | 不允许（暂停菜单按钮在演出期间隐藏/禁用） | 演出期间 InputBlocker 已激活 |

## Dependencies

| System | Direction | Nature of Dependency |
|--------|-----------|---------------------|
| Audio System | This configures Audio | 音量和开关设置 |
| Object Interaction System | This configures OI | 触控灵敏度 |
| Input System | This configures Input | 振动开关 |
| UI System | This uses UI | 设置界面渲染和交互 |
| Tutorial System | This links to Tutorial | 操作指南入口 |
| Save System / PlayerPrefs | This reads/writes | 设置值持久化 |
| TEngine 本地化系统 | This uses | 语言切换和文本热刷新 |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|-----------|--------------|------------|-------------------|-------------------|
| defaultMasterVolume | 1.0 | 0-1 | 默认更响 | 默认更安静 |
| defaultMusicVolume | 0.5 | 0-1 | 默认音乐更响 | 默认音乐更安静 |
| defaultSfxVolume | 0.8 | 0-1 | 默认音效更响 | 默认音效更安静 |
| defaultTouchSensitivity | 1.0 | 0.5-2.0 | 默认更灵敏 | 默认更迟钝 |
| defaultTargetFramerate | 60 | 30/60 | 默认更流畅 | 默认更省电 |
| minTouchSensitivity | 0.5 | 0.3-0.8 | 最低灵敏度的操作不会太迟钝 | 允许更极端的低灵敏度 |
| maxTouchSensitivity | 2.0 | 1.5-3.0 | 允许更极端的高灵敏度 | 最高灵敏度受限 |

## Acceptance Criteria

- [ ] 所有设置项修改后立即生效，不需要重启游戏
- [ ] 音量滑动条拖动时实时改变音量，可听到变化
- [ ] 音效开关关闭后所有 SFX 和环境音立即静音
- [ ] 振动开关关闭后不再产生任何 Haptic 反馈
- [ ] 触控灵敏度修改后下一次触摸操作即使用新值
- [ ] 语言切换后当前界面所有文本立即更新为新语言
- [ ] 帧率切换后 FPS 立即变化（可通过 Debug Panel 验证）
- [ ] 设置值在重启游戏后保持不变
- [ ] 首次启动时自动匹配系统语言
- [ ] 系统语言不支持时正确 fallback 到英文
- [ ] "操作指南"入口正确展示所有已解锁操作说明
- [ ] 设置界面在各分辨率下布局正常（适配 iPhone SE ~ iPad）

## Open Questions

| Question | Owner | Deadline | Resolution |
|----------|-------|----------|-----------|
| TEngine 本地化系统的热切换机制是否支持 UI Toolkit / UGUI 混用？ | Tech Lead | VS 开发前 | 需要验证 TEngine 版本 |
| 是否需要独立的"环境音音量"设置（当前与 SFX 合并）？ | Game Design | VS 测试阶段 | 先合并，测试后决定是否拆分 |
| iPad 分屏模式下设置界面的适配 | UI / QA | VS 测试阶段 | 待真机验证 |
| 省电模式下是否自动切换到 30fps？ | Tech Lead | VS 真机测试 | 需要检测低电量状态的 API |
