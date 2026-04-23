// 该文件由Cursor 自动生成

# Interaction Patterns — 影子回忆 (Shadow Memory)

> **Version**: 1.0
> **Date**: 2026-04-22
> **Source ADRs**: ADR-010 (Input), ADR-011 (UI), ADR-013 (Object Interaction)
> **Source GDDs**: `input-system.md`, `object-interaction.md`, `ui-system.md`, `tutorial-onboarding.md`
> **Platform**: Mobile (iOS/Android) primary, PC secondary

---

## 1. Global Interaction Principles

| # | Principle | Rationale |
|---|-----------|-----------|
| P1 | **单手可达** | 移动端玩家多为单手持握；所有核心操作用单指完成 |
| P2 | **触觉一致性** | 同类操作在所有场景中产生相同的视觉/触觉反馈 |
| P3 | **静默过滤** | 被阻断/过滤的手势不产生任何反馈（无抖动、无音效、无 Toast） |
| P4 | **渐进披露** | 高级手势（双指旋转、距离调整）仅在教学引导后才被暗示 |
| P5 | **容错优先** | 放大触摸热区（+8dp）、吸附辅助、误操作可撤回 |

---

## 2. Gesture-to-Action Mapping

### 2.1 Game Scene (Puzzle Active)

| Gesture | Target | Action | Feedback | GDD Ref |
|---------|--------|--------|----------|---------|
| **Tap** | 可交互物件 | 选中物件 | 缩放弹跳 (EaseOutBack, 133ms) + 描边高亮 | TR-objint-002 |
| **Tap** | 空白区域 | 取消选中 | 选中物件缩放回原始 | TR-objint-002 |
| **Drag** | 已选中物件 | 拖拽移动（1:1 跟手） | 物件跟随手指，无惯性 | TR-objint-004 |
| **Drag** | 光源（在轨道上） | 沿轨道移动光源 | 光源吸附轨道参数 `t` | TR-objint-009 |
| **Drag→Release** | 物件/光源 | 吸附到最近格点 | EaseOutQuad 插值 + 轻触觉 | TR-objint-005/006 |
| **双指旋转** | 已选中物件 | 旋转物件（15° 步进吸附） | 旋转到位时轻触觉 | TR-objint-007 |
| **双指捏合** | *保留* | 无操作（保留给缩放视角） | — | — |
| **长按** | *保留* | 无操作（避免与 Drag 冲突） | — | — |

### 2.2 UI Panels

| Gesture | Target | Action | Feedback |
|---------|--------|--------|----------|
| **Tap** | 按钮 | 触发按钮事件 | 缩放 + 音效 |
| **Tap** | Panel 背景遮罩 | 关闭当前 Popup（如果允许） | FadeOut 关闭动画 |
| **Swipe** | *无* | UI 不响应滑动（避免与游戏拖拽冲突） | — |
| **Android Back** | 系统 | 返回上一 UI 层级 / 打开暂停菜单 | 对应 Panel 动画 |

### 2.3 PC Input Mapping

| 键鼠操作 | 等效手势 | Notes |
|----------|---------|-------|
| 鼠标左键单击 | Tap | — |
| 鼠标左键拖拽 | Drag | — |
| 鼠标右键拖拽 | Rotate | — |
| 鼠标滚轮 | Pinch (保留) | — |
| 鼠标中键拖拽 | LightDrag | — |
| Esc 键 | Android Back 等效 | 暂停 / 返回 |

---

## 3. Input Gating (Blocker / Filter)

### 3.1 InputBlocker Scenes

当以下 UI 面板打开时，`InputBlocker` 自动压栈，游戏场景不接收任何手势：

| Trigger | Blocker Token | Auto Push/Pop |
|---------|--------------|:-------------:|
| PauseMenu 打开 | `"PauseMenu"` | ✅ ADR-011 |
| SettingsPanel 打开 | `"Settings"` | ✅ ADR-011 |
| ChapterTransition 播放 | `"Transition"` | ✅ ADR-009 |
| Narrative 序列播放 | `"Narrative"` | ✅ ADR-016 |
| PuzzleCompletePanel 显示 | `"PuzzleComplete"` | ✅ ADR-011 |

### 3.2 InputFilter Scenes

教学步骤激活时，`InputFilter` 仅允许指定手势通过：

| Tutorial Step | Allowed Gestures | All Others |
|---------------|-----------------|------------|
| DragObject (Ch.1P1) | `[Tap, Drag]` | 静默丢弃 |
| RotateObject (Ch.1P2) | `[Tap, Drag, Rotate]` | 静默丢弃 |
| SnapHint (Ch.1P3) | `[Tap, Drag]` | 静默丢弃 |
| LightTrack (Ch.2P1) | `[Tap, LightDrag]` | 静默丢弃 |
| DistanceAdjust (Ch.2P2) | `[Tap, Drag]` | 静默丢弃 |

### 3.3 Priority Stack

```
Priority 1 (最高): InputBlocker — 非空时丢弃所有输入
Priority 2:        InputFilter  — 仅白名单手势通过
Priority 3 (最低): Normal       — 所有手势通过
```

Narrative > Tutorial > Hint（优先级链，高优先级打断低优先级）

---

## 4. Feedback Patterns

### 4.1 Visual Feedback

| Event | Visual Response | Duration | Easing |
|-------|----------------|----------|--------|
| 物件选中 | 缩放 1.0 → 1.08 → 1.05 | 133ms (8f@60fps) | EaseOutBack |
| 物件放下 | 缩放回 1.0 | 100ms | EaseOutQuad |
| 格点吸附 | 位置插值到格点 | 80–200ms (距离相关) | EaseOutQuad |
| 旋转吸附 | 角度插值到 15° 步进 | 100ms | EaseOutQuad |
| 边界碰撞 | 位置回弹 | 200ms | EaseOutBack (overshoot 0.3) |
| NearMatch 进入 | 影子发光 (emission) | 渐入 300ms | Linear |
| PerfectMatch | 物件自动吸附到目标 + 全局影子脉冲 | 300–800ms | EaseOutBack |
| 匹配失败退出 NearMatch | 影子发光消退 | 渐出 500ms | Linear |

### 4.2 Audio Feedback

| Event | SFX | Layer | Notes |
|-------|-----|-------|-------|
| 物件选中 | `sfx_select` | SFX | 轻柔的拾取音 |
| 物件放下/吸附 | `sfx_place` | SFX | 物理感放置音 |
| 旋转到位 | `sfx_rotate_snap` | SFX | 细微的卡扣音 |
| 边界碰撞 | `sfx_boundary` | SFX | 柔和的限制音 |
| NearMatch 进入 | `sfx_nearmatch` | SFX | 温暖的共鸣音 |
| PerfectMatch | `sfx_perfectmatch` | SFX → Music ducking | 成就感音效 |

### 4.3 Haptic Feedback (if enabled)

| Event | Haptic Type | Intensity |
|-------|-------------|-----------|
| 格点吸附 | Light Impact | Low |
| 旋转吸附 | Light Impact | Low |
| 边界碰撞 | Medium Impact | Medium |
| PerfectMatch | Success Notification | High |

> 触觉反馈受 `haptic_enabled` 设置控制（`PlayerPrefs`），默认开启。
> 跨平台实现推迟至 ADR-025 (P2)。

---

## 5. Error & Edge Cases

| Situation | System Behavior | Player Perception |
|-----------|----------------|-------------------|
| Tap 到无物件区域 | 取消当前选中 | 选中物件静默消失高亮 |
| Drag 到屏幕边缘 | InteractionBounds 回弹 | 物件弹回边界内 |
| 快速连续 Tap 同一物件 (< 200ms) | Debounce 忽略重复 | 无重复反馈 |
| 3+ 手指触摸 | 忽略第 3+ 手指 | 前 2 指操作继续 |
| 双指旋转中抬起 1 指 | 结束旋转，进入单指 Idle | 旋转停止 |
| 帧率骤降 (> 33ms) | MaxDeltaPerFrame clamp | 物件不会瞬移 |
| App 切后台后恢复 | 清除所有 touch state | 无幽灵拖拽 |
| Narrative 打断操作中 | InputBlocker 压栈 + 物件状态冻结 | 当前拖拽中断，物件停在原位 |

---

## 6. Screen-Specific Interaction Specs

### 6.1 GameHUD (Puzzle Scene)

```
┌─────────────────────────────────────────┐
│  [Pause]                    [Chapter ◆] │  ← HUD Layer (pass-through)
│                                         │
│                                         │
│          ┌─────────────┐                │
│          │   Puzzle     │                │  ← 3D Game World
│          │   Area       │                │     (receives touch input)
│          └─────────────┘                │
│                                         │
│                    [💡 Hint]            │  ← HintButton (HUD, 30% → 80% opacity)
│  [Save ◆]                              │
└─────────────────────────────────────────┘
```

- **HUD 层不阻断游戏输入**：Pause 按钮、Hint 按钮使用 UGUI Button Raycast，未命中时 touch 穿透到 3D 场景
- **Safe Area**：所有 HUD 元素在安全区内（`SetUISafeFitHelper`）
- **HintButton**：闲置 30s 后 opacity 从 30% 渐变到 80%（引导玩家注意）

### 6.2 PauseMenu (Popup Layer)

```
┌─────────────────────────────────────────┐
│  ┌───────────────────────────────────┐  │
│  │         暂停                       │  │  ← Popup Layer
│  │                                   │  │    (InputBlocker active)
│  │   [继续游戏]                      │  │
│  │   [设置]                          │  │
│  │   [章节选择]                      │  │
│  │   [退出]                          │  │
│  └───────────────────────────────────┘  │
│         (背景高斯模糊 / 纯色遮罩)      │
└─────────────────────────────────────────┘
```

- `Time.timeScale = 0`：游戏逻辑暂停
- Music 继续播放（不受 TimeScale 影响）
- Android Back 键 → 关闭暂停菜单（等效点击"继续游戏"）

### 6.3 PuzzleComplete (Overlay Layer)

```
┌─────────────────────────────────────────┐
│                                         │
│          ✦ 完成 ✦                       │  ← Overlay Layer
│                                         │    (InputBlocker active)
│     [记忆碎片文字 typewriter effect]    │    Auto-close 2.5s
│                                         │    Tap-to-close enabled
│                                         │
└─────────────────────────────────────────┘
```

- 自动关闭 2.5s（计时器使用 `Time.unscaledDeltaTime`）
- 允许 Tap 提前关闭
- Typewriter 速度：80–120ms/字符（可配置）

---

## 7. Touch Target Specifications

| Element | Minimum Size | Notes |
|---------|:------------:|-------|
| HUD 按钮 (Pause, Hint) | 44 × 44 pt | Apple HIG compliant |
| 菜单按钮 | 44 × 44 pt | — |
| 可交互物件 | 碰撞体 + 8dp padding | Fat finger compensation |
| 光源轨道 | 轨道宽度 20dp 最小 | 可点击/拖拽区域 |

> `pt` = 逻辑点 (iOS points / Android dp)
> `dp` = density-independent pixels = `px / (dpi / 160)`

---

*End of Interaction Patterns Document*
