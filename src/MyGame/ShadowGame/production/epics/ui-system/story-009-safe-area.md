// 该文件由Cursor 自动生成

# Story: Safe Area Fitting for Notch/Rounded Corner Devices

> **Epic**: ui-system
> **Story ID**: ui-system-009
> **Story Type**: Integration
> **GDD Requirement**: TR-ui-007 (Safe area fitting)
> **ADR References**: ADR-011 (UIWindow Management), ADR-003 (Mobile-First Platform)
> **Sprint**: TBD
> **Status**: Ready (依赖 ui-system-001)

## Context

iPhone X 及以上刘海屏设备（和部分 Android 设备）需要安全区适配，确保 UI 关键元素不被刘海、圆角、底部 Home Indicator 遮挡。TEngine 提供 `SetUISafeFitHelper` 工具，应用于根 Canvas 即可自动适配 `Screen.safeArea`。

本 story 确保所有 UIWindow 根 Canvas 均已正确应用 `SetUISafeFitHelper`，并在 5 台以上 iOS/Android 真机上验证显示效果。

## Acceptance Criteria

- [ ] 所有 UIWindow 子类的根 Canvas GameObject 均挂载并启用 `SetUISafeFitHelper` 组件（或在 OnCreate 中以代码方式添加）
- [ ] SaveIndicatorPanel（System 层，通常显示于角落）可配置 `ignoreSafeArea = true`（允许延伸至安全区外角落显示存档图标）
- [ ] 在 iPhone X（或 Simulator）上：HUDPanel 的 HintButton / SettingsGear 不被刘海遮挡
- [ ] 在 iPhone X 底部：Continue 按钮等底部元素不被 Home Indicator 遮挡
- [ ] CanvasScaler 配置：Scale With Screen Size，Reference Resolution = 1920×1080，Match Width/Height = 0.5（来自 ADR-003）
- [ ] 在 16:9（1080p）和 19.5:9（iPhone 14 Pro）两种宽高比下 UI 布局正确，无内容裁切
- [ ] `Screen.safeArea` Fallback：`safeArea.width == 0` 时使用全屏尺寸（防御性处理）

## Implementation Notes

- `SetUISafeFitHelper` 应在 UI Prefab 的根 RectTransform 上配置（编辑器内操作），而非运行时动态添加
- 或通过 UIWindow 基类 OnCreate 统一添加：`GetComponent<Canvas>().gameObject.AddComponent<SetUISafeFitHelper>()`（确认 TEngine API）
- 真机测试设备建议：iPhone X / iPhone 12 Mini / iPhone 14 Pro / Samsung Galaxy S22（或等效 Android 刘海屏）/ iPad（无刘海对比）
- CanvasScaler 在 UI Prefab 中的 Canvas 组件上配置，不在运行时修改

## Out of Scope

- 具体面板内容布局调整（各面板自行负责锚点和 padding）
- Android 厂商定制 ROM 的特殊适配（仅 AOSP 兼容性）

## QA Test Cases (Integration)

### TC-001: iPhone X 刘海区安全（Setup/Verify/Pass）
- **Setup**: 在 iPhone X 或 Simulator（safeArea.y > 0）上构建游戏，打开 HUDPanel
- **Verify**: HintButton 和 SettingsGear 完全在安全区内，不被刘海遮挡；底部面板元素不被 Home Indicator 遮挡
- **Pass**: 视觉检查通过，无 UI 元素截断

### TC-002: 16:9 vs 19.5:9 布局
- **Setup**: 在 1080×1920（16:9）和 1170×2532（19.5:9，iPhone 12 Pro）分辨率下运行
- **Verify**: 所有 UIWindow 的按钮和文本正确显示，无内容裁切，无异常拉伸
- **Pass**: 两种宽高比下截图对比均通过布局检查

### TC-003: safeArea Fallback
- **Setup**: 模拟 `Screen.safeArea = Rect(0,0,0,0)`（异常情况）
- **Verify**: SetUISafeFitHelper 使用全屏尺寸（Screen.width × Screen.height），不报 NullReference
- **Pass**: 无异常抛出，UI 正常显示

### TC-004: SaveIndicatorPanel 可延伸至角落
- **Setup**: SaveIndicatorPanel 配置 ignoreSafeArea = true
- **Verify**: 存档图标显示在屏幕右上角（含刘海区域内），SaveIndicatorPanel 不受安全区收缩影响
- **Pass**: 存档图标在刘海屏设备上于右上角可见

## Test Evidence Path

`production/qa/evidence/ui-system/safe-area-device-evidence.md`（含截图）

## Dependencies

- ui-system-001: UIModule 基础（所有 UIWindow 根 Canvas 已建立）
- ADR-003: Mobile-First（CanvasScaler 配置来源，1920×1080 参考分辨率）
- ADR-011: Safe area 适配规范
