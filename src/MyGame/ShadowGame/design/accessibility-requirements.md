// 该文件由Cursor 自动生成

# Accessibility Requirements — 影子回忆 (Shadow Memory)

> **Version**: 1.0
> **Date**: 2026-04-22
> **Target Level**: **Game Accessibility Level A** (equivalent to WCAG 2.1 Level A adapted for interactive media)
> **Platform**: Mobile (iOS/Android) primary, PC secondary
> **Source**: Art Bible §Accessibility, ADR-002, ADR-010, ADR-011, ADR-003, GDD system docs

---

## 1. Accessibility Tier Definition

**Game Accessibility Level A** — 确保核心游戏体验对以下用户群可达：

| Category | Target | Notes |
|----------|--------|-------|
| 视觉 (Visual) | 低视力、色觉异常 | 影子对比度、色盲模式、字体缩放 |
| 运动 (Motor) | 单手操作、精细运动受限 | 触摸热区放大、吸附辅助、灵敏度调节 |
| 认知 (Cognitive) | 注意力/记忆困难 | 渐进提示、无时间压力、可暂停 |
| 听觉 (Auditory) | 听力障碍 | 非听觉依赖设计（核心玩法不依赖音频） |

> **不包含**（Level AA/AAA 延后项）：屏幕阅读器支持、完全键盘导航（PC）、实时字幕

---

## 2. Visual Accessibility

### 2.1 影子对比度 (MVP — Sprint 1)

| Requirement | Target | ADR/GDD Ref | Priority |
|-------------|--------|-------------|:--------:|
| 默认影子对比度 | ≥ 3:1 | TR-render-008, Art Bible | P0 |
| 高对比度模式 | ≥ 8:1 | TR-render-021 | P2 (ADR-020) |
| 影子轮廓辅助模式 | 描边 overlay | TR-render-022 | P2 (ADR-020) |

### 2.2 色觉异常 (Planned — Pre-Alpha)

| Requirement | Description | Priority |
|-------------|-------------|:--------:|
| 色盲友好影子色 | NearMatch/PerfectMatch 反馈不仅靠颜色区分 | P2 |
| 形状+颜色双通道 | 所有状态指示同时使用形状和颜色 | P1 |
| 色彩对比验证 | 章节色板在 Protanopia/Deuteranopia 下验证 | P2 |

### 2.3 字体与文本 (Planned — Pre-Alpha)

| Requirement | Description | GDD Ref | Priority |
|-------------|-------------|---------|:--------:|
| 3 档字体大小 | Standard / Large / Extra-Large | TR-ui-020 | P2 (ADR-020) |
| 最小字号 | ≥ 14sp (标准), ≥ 18sp (大), ≥ 22sp (特大) | — | P2 |
| 所有文本可缩放 | UI 布局在 Extra-Large 下不溢出 | — | P2 |

---

## 3. Motor Accessibility

### 3.1 触摸热区 (MVP — Sprint 1)

| Requirement | Target | ADR/GDD Ref | Priority |
|-------------|--------|-------------|:--------:|
| 按钮最小尺寸 | 44 × 44 pt | TR-ui-019, Apple HIG | P0 |
| Fat finger 补偿 | +8dp 触摸扩展 | TR-objint-003, ADR-010 | P0 |
| 物件 Tap 热区 | 碰撞体 + padding | ADR-013 | P0 |

### 3.2 灵敏度调节 (MVP — Sprint 1)

| Requirement | Description | GDD Ref | Priority |
|-------------|-------------|---------|:--------:|
| 触摸灵敏度滑块 | 修改 dragThreshold 乘数 | TR-input-018, TR-settings-004 | P0 |
| 灵敏度范围 | 0.5× ~ 2.0× | Settings GDD | P0 |
| 实时生效 | 修改后立即应用，无需重启 | TR-settings-003 | P0 |

### 3.3 吸附辅助 (MVP — Sprint 1)

| Requirement | Description | Priority |
|-------------|-------------|:--------:|
| 格点吸附 | 自动对齐到 0.25 世界单位 | P0 |
| 旋转吸附 | 自动对齐到 15° 步进 | P0 |
| 吸附强度可调 | *未定义 — 考虑 Pre-Alpha 加入* | P2 |

### 3.4 单手操作

| Requirement | Description | Priority |
|-------------|-------------|:--------:|
| 核心操作单指完成 | Tap + Drag 覆盖主要交互 | P0 |
| 双指旋转非必需 | 无需双指即可完成关卡（旋转可用其他方式触发） | P1 |

---

## 4. Cognitive Accessibility

| Requirement | Description | Priority |
|-------------|-------------|:--------:|
| 无时间压力 | 所有谜题无倒计时限制 | P0 |
| 随时可暂停 | PauseMenu 随时可调出 | P0 |
| 渐进提示系统 | 3 层提示逐步引导（L1 环境 → L2 方向 → L3 显式） | P0 |
| 教学步骤可重放 | Settings → 操作指南可随时回看 | P1 |
| 进度保存频繁 | 自动存档在每次关键状态变化时触发 | P0 |
| 章节可重玩 | 已完成章节可随时重新体验 | P1 |
| UI 动画可减弱 | `animationScale` 设置 (0.0–1.0) | P2 (ADR-020) |

---

## 5. Auditory Accessibility

| Requirement | Description | Priority |
|-------------|-------------|:--------:|
| 核心玩法不依赖音频 | 影子匹配纯视觉判定 | P0 (inherent design) |
| 独立音量控制 | Master / Music / SFX 三路独立 | P0 |
| SFX 开关 | sfx_enabled 可关闭所有音效 | P0 |
| Ambient 独立于 SFX | sfx_enabled 关闭不影响环境音 | P0 |
| 视觉反馈替代音频 | 所有音效触发的交互同时有视觉反馈 | P0 |

---

## 6. Platform-Specific

### 6.1 iOS

| Requirement | Description | Priority |
|-------------|-------------|:--------:|
| VoiceOver 基本兼容 | UI 按钮可被 VoiceOver 读取 | P2 |
| Dynamic Type 适配 | 系统字号变化时 UI 正确缩放 | P2 |
| 安全区适配 | 刘海屏/灵动岛区域不遮挡 UI | P0 |

### 6.2 Android

| Requirement | Description | Priority |
|-------------|-------------|:--------:|
| TalkBack 基本兼容 | UI 按钮可被 TalkBack 读取 | P2 |
| 返回键导航 | Android Back 键正确映射到 UI 返回 | P0 |
| 屏幕方向锁定 | 锁定为横屏（避免意外旋转） | P0 |

---

## 7. Implementation Roadmap

| Phase | Requirements | Timeline |
|-------|-------------|----------|
| **MVP (Sprint 1–3)** | 影子对比度 ≥ 3:1, 触摸热区 44pt, Fat finger, 灵敏度调节, 吸附辅助, 无时间压力, 渐进提示, 三路音量, 安全区, Android Back | Pre-Production |
| **Pre-Alpha** | 形状+颜色双通道, 单手操作验证, 教学重放, UI 动画缩放 | Production Sprint 4–6 |
| **Alpha** | 高对比度模式, 色盲友好, 字体缩放 3 档, 影子轮廓辅助 | Polish |
| **Beta** | VoiceOver/TalkBack 基础, Dynamic Type, 吸附强度可调 | Release Prep |

---

## 8. Testing Requirements

| Test Type | Description | Frequency |
|-----------|-------------|-----------|
| 对比度检查 | 影子对比度 ≥ 3:1 (默认) / ≥ 8:1 (高对比度) | 每章完成后 |
| 色盲模拟 | Protanopia + Deuteranopia 模式下所有视觉反馈可辨识 | Pre-Alpha |
| 单手测试 | 仅用拇指完成整章所有谜题 | 每章完成后 |
| 触摸热区验证 | 所有可交互元素 ≥ 44pt | Sprint Review |
| 灵敏度极端值 | 在 0.5× 和 2.0× 灵敏度下完成谜题 | 每个交互变更后 |

---

*End of Accessibility Requirements*
