<!-- 该文件由Cursor 自动生成 -->

# Object Interaction 原型 — 验收检查表

> **Status**: **READY FOR TESTING**
> **Created**: 2026-04-16
> **Target**: GDD `design/gdd/object-interaction.md` Acceptance Criteria + Feel Acceptance Criteria

---

## 原型覆盖范围

| 功能 | 纳入 | 说明 |
|------|------|------|
| 单指拖拽 | ✅ | Raycast + 平面投影 + 跟手 |
| 格点吸附 | ✅ | EaseOutQuad 50-150ms |
| 选中放大动画 | ✅ | EaseOutBack 8f, scale 1.05x |
| 放下回弹动画 | ✅ | Bounce 10f |
| 双指旋转 | ✅ | Y 轴 15° 步进 |
| 边界约束 + 回弹 | ✅ | EaseOutBack rebound |
| 胖手指补偿 | ✅ | SphereCast + DPI 缩放 |
| 距离调整 | ❌ | Open Question，后续验证 |
| 光源轨道移动 | ❌ | 第二章功能 |
| Haptic 反馈 | ❌ | 需真机迭代 |
| 音效 | ❌ | 原型只验手感 |

---

## P0 — 核心手感

| # | Criteria | 通过标准 | 结果 |
|---|----------|---------|------|
| 1 | **拖拽跟手** | 物件在下一渲染帧跟随手指，无可感知延迟 | ☐ |
| 2 | **格点吸附柔和** | 释放后物件"吸入"格点，无人用"跳跃"形容 | ☐ |
| 3 | **放下回弹** | 回弹被感知为"轻放桌面"，无人用"弹飞"形容 | ☐ |
| 4 | **选中反馈** | 触碰后物件立即放大+高亮，响应 ≤ 50ms | ☐ |
| 5 | **零惯性** | 手指抬起物件立即停止，无滑动 | ☐ |

## P1 — 交互完整性

| # | Criteria | 通过标准 | 结果 |
|---|----------|---------|------|
| 6 | **双指旋转** | 旋转跟随手势，释放后吸附到 15° 步进 | ☐ |
| 7 | **边界约束** | 拖拽到边界物件停止，释放后回弹 | ☐ |
| 8 | **胖手指** | 小屏上所有物件首触命中率 ≥ 90% (10 次/物件) | ☐ |
| 9 | **单选** | 同一时刻只有一个物件被选中 | ☐ |
| 10 | **空白取消** | 触碰空白处取消选中 | ☐ |

## P2 — 性能

| # | Criteria | 通过标准 | 结果 |
|---|----------|---------|------|
| 11 | **帧率** | 拖拽过程中 ≥ 55fps (iPhone 13 Mini 标准) | ☐ |
| 12 | **拖拽响应** | Input-to-visual ≤ 16ms (CSV 数据验证) | ☐ |
| 13 | **5 分钟稳定性** | 连续操作 5 分钟无帧率退化 | ☐ |

## P3 — 真机体验

| # | Criteria | 通过标准 | 结果 |
|---|----------|---------|------|
| 14 | **无教程可发现** | 测试者 5 分钟内自行发现拖拽和旋转 | ☐ |
| 15 | **手指疲劳** | iPhone 13 Mini 单手操作 3 分钟无疲劳报告 | ☐ |
| 16 | **参数可调** | 通过 TUNE 面板实时调整参数，效果立即可见 | ☐ |

---

## 调参记录

| 参数 | GDD 默认值 | 调优后值 | 原因 |
|------|-----------|---------|------|
| gridSize | 0.25 | — | — |
| rotationStep | 15° | — | — |
| snapSpeed | 3.0 | — | — |
| selectScaleMultiplier | 1.05 | — | — |
| bounceAmplitude | 0.02 | — | — |
| fatFingerMarginDp | 8 | — | — |
| reboundOvershoot | 0.3 | — | — |

---

## 文件清单

| 文件 | 用途 |
|------|------|
| `Assets/Scripts/Prototype/InteractionConfig.cs` | 所有可调参数的 ScriptableObject |
| `Assets/Scripts/Prototype/InteractableObject.cs` | 物件状态机 + 吸附 + 动画 |
| `Assets/Scripts/Prototype/InteractionController.cs` | 输入 → Raycast → 手势识别 → 物件调度 |
| `Assets/Scripts/Prototype/GridSystem.cs` | 格点计算 + 边界约束 |
| `Assets/Scripts/Prototype/InteractionDebugPanel.cs` | OnGUI 调试面板 + CSV 导出 |
| `Assets/Editor/ShadowPrototype/ShadowPrototypeSetup.cs` | Editor 菜单 "3. Setup Interaction System" |
| `Assets/Settings/Prototype/InteractionConfig.asset` | 运行 Editor 脚本后生成的配置资产 |
