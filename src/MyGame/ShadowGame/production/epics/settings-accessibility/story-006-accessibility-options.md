// 该文件由Cursor 自动生成

# Story 006: Shadow Contrast Boost + Touch Target Scaling

> **Epic**: Settings & Accessibility
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Visual/Feel
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/settings-accessibility.md`
**Requirement**: `TR-settings-001` (P1 accessibility options within MVP scope)

**ADR Governing Implementation**: ADR-002: URP Shadow Rendering; ADR-003: Mobile-First Platform; ADR-011: UIWindow Management
**ADR Decision Summary**: 高对比度模式（对比度 ≥ 8:1）和字体缩放 3 档属于 ADR-020（P2）；MVP 范围内的无障碍交付为：(1) 默认影子对比度 ≥ 3:1（渲染设计约束，不是运行时切换）；(2) 所有触摸热区 ≥ 44dp（UI 硬性规范）；本 Story 验证这两项 P0 要求是否在设置界面和全游戏 UI 中正确实现。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW

**Control Manifest Rules (Presentation — UIWindow)**:
- Required: 最小触摸目标 44×44dp（Apple HIG baseline），建议 48dp；最小文字 14sp body，12sp captions；CanvasScaler: Scale With Screen Size, 1920×1080, match width 0.5；`SetUISafeFitHelper` 在所有根 Canvas
- Performance Budget: UI 系统每帧 < 0.5ms
- Accessibility Reference: `design/accessibility-requirements.md` §2.1（影子对比度）、§3.1（触摸热区）

---

## Acceptance Criteria

- [ ] SettingsPanel 中所有可交互控件（Slider handle、Toggle、Button）的触摸热区 ≥ 44×44dp
- [ ] `TutorialOverlay`、`HUDPanel`、`PauseMenuPanel` 中所有可交互按钮 ≥ 44dp
- [ ] 所有 UI 文字 ≥ 14sp（body）、≥ 12sp（caption），在 SettingsPanel 验证通过
- [ ] 默认影子对比度（WallReceiver shader + 场景光照）测量值 ≥ 3:1（通过截图颜色拾取验证）
- [ ] 在 iPhone SE（375×667 逻辑像素）上：SettingsPanel 无元素重叠，无文字截断，无按钮被安全区遮挡
- [ ] 在 iPad（768×1024 逻辑像素）上：SettingsPanel 布局正常，控件不过小也不过大
- [ ] Android Back 键在 SettingsPanel 打开时正确触发"返回"操作（关闭 SettingsPanel）
- [ ] 屏幕方向锁定为横屏，竖屏不应用

---

## Implementation Notes

**触摸热区验证方法：**
UGUI 中通过 RectTransform 尺寸控制实际触摸区域，可与视觉大小不同（使用 `CanvasRenderer` + 透明拓展区域）。

检查方法：
1. 在 Scene 视图中选中每个 Button/Toggle/Slider Handle
2. 查看 RectTransform.rect.width 和 height（单位 px @ reference 1920×1080）
3. 换算到 dp：`dpSize = pixelSize × (referenceWidth / screenWidth)`；在 375pt iPhone SE 上，44pt ≈ 44 × (375/1920) × 1920 / 375 = 需要具体计算

CanvasScaler 在 1920×1080 参考分辨率下，44dp 对应约 **88px（1920 宽度参考）**。

**影子对比度验证方法：**
1. 截取第 1 章谜题场景（默认光照）
2. 使用颜色拾取工具采样：影子最暗区域 vs 墙面最亮区域
3. 计算 WCAG 对比度：`(L1 + 0.05) / (L2 + 0.05)`，其中 L = 相对亮度
4. 目标 ≥ 3:1

**Android Back 键实现：**
```csharp
// SettingsPanel.OnUpdate()（仅在 visible 时执行）
protected override void OnUpdate()
{
    if (Input.GetKeyDown(KeyCode.Escape))  // Android Back 键
        GameModule.UI.CloseWindow<SettingsPanel>();
}
```

**屏幕方向锁定（在 ProjectSettings 中设置，不在运行时代码中）：**
- Project Settings → Player → Default Orientation: Landscape Left（或 Auto Rotation 仅允许 Landscape）

---

## Out of Scope

- 高对比度模式（P2, ADR-020）— 不在本 Story 实现
- 字体缩放 3 档（P2, ADR-020）
- 色盲友好模式（P2）
- VoiceOver/TalkBack 支持（P2）
- 影子轮廓辅助模式（P2）

---

## QA Test Cases

### TC-006-01: SettingsPanel 触摸热区验证
**Setup** 在 Unity Editor 中打开 SettingsPanel prefab
**Verify** 检查每个 Slider handle、Toggle、Button 的 RectTransform 尺寸
**Pass** 所有可交互元素 RectTransform 宽高 ≥ 88px（对应 1920 参考分辨率下的 44dp）

### TC-006-02: 文字最小尺寸验证
**Setup** 检查 SettingsPanel 所有 TMP_Text 组件的 font size
**Verify** body 文字 font size ≥ 28px（对应 14sp）；caption 文字 ≥ 24px（对应 12sp）（参考 1920×1080）
**Pass** 所有文字符合最小尺寸要求

### TC-006-03: iPhone SE 布局验证
**Setup** 切换 Game View 分辨率为 667×375（iPhone SE 横屏）
**Verify** 显示 SettingsPanel
**Pass** 所有控件可见；无重叠；无截断；安全区内；Back 按钮可触摸

### TC-006-04: iPad 布局验证
**Setup** 切换 Game View 分辨率为 1024×768（iPad 横屏）
**Verify** 显示 SettingsPanel
**Pass** 控件间距合理；Slider 长度适中；文字大小适中（不过大）

### TC-006-05: 默认影子对比度 ≥ 3:1
**Setup** 在第 1 章谜题场景中截图（默认光照设置）
**Verify** 使用颜色拾取工具分析影子区域与墙面区域
**Pass** WCAG 对比度计算结果 ≥ 3:1

### TC-006-06: Android Back 键关闭 SettingsPanel
**Setup** 在 Android 真机（或 Editor 模拟）打开 SettingsPanel
**Verify** 按 Android Back 键（或 Escape 键）
**Pass** SettingsPanel 关闭，返回暂停菜单；无崩溃

### TC-006-07: 横屏锁定
**Setup** 在真机上旋转设备为竖屏
**Verify** 游戏画面响应
**Pass** 游戏保持横屏布局；不跟随设备旋转

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**: `production/qa/evidence/settings/accessibility-visual-evidence.md`
**Status**: [ ] Not yet created

**Evidence checklist**:
- [ ] 截图：SettingsPanel 在 iPhone SE 分辨率下的完整布局
- [ ] 截图：SettingsPanel 在 iPad 分辨率下的完整布局
- [ ] 截图/数据：触摸热区尺寸测量记录（每个可交互元素）
- [ ] 截图：影子对比度测量（场景截图 + 颜色采样数据）
- [ ] 截图：文字尺寸验证（每个文字层级的 font size 截图）
- [ ] 截图/录屏：Android Back 键测试记录

---

## Dependencies

- Depends on: Story 001 (SettingsManager 提供设置基础), ui-system epic (UIWindow 布局), urp-shadow-rendering epic (默认影子对比度由渲染系统决定)
- Unlocks: accessibility-requirements.md §7 MVP Sprint 1-3 验收条件满足
