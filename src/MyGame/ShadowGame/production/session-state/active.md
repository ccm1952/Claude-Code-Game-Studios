# ShadowGame — Session State

> Last updated: 2026-04-22 (session 9)
> Phase: **Pre-production 准备 — Architecture v1.0 Draft 完成 / ADR 编写阶段** (workflow-catalog.yaml)
> Next milestone: ADRs (11 P0 + 7 P1) → Gate Check → Pre-production

---

## Project Identity

- **游戏名**: 影子回忆 (Shadow Memory)
- **类型**: 叙事解谜 — 通过摆放生活物件与光源，拼出关系影子
- **核心主题**: 失去伴侣后，重新理解两个人共同构成的日常世界
- **核心设计原则**: "单个物体不是回忆，关系才是回忆"
- **技术栈**: Unity 2022.3.62f2 / URP / TEngine 6.0.0 / HybridCLR / YooAsset 2.3.17 / UniTask 2.5.10
- **目标平台**: Mobile (iOS/Android) 优先，后续 PC

---

## Workflow Progress (Concept Phase)

| Step | Status | Artifact |
|------|--------|----------|
| Engine Setup | DONE | `.claude/docs/technical-preferences.md` |
| Game Concept | DONE | `design/concept/shadow-memory.md` |
| Concept Review | SKIPPED (optional) | — |
| Art Bible | DONE | `design/art/art-bible.md` |
| Systems Map | DONE | `design/gdd/systems-index.md` |

> **Concept 阶段全部必需步骤已完成。** 可进入 Systems Design 阶段。

---

## Completed Artifacts

### 配置文件（工作室级别）
- `/Users/chen/Desktop/Dev/MyGameStudio/CLAUDE.md` — 技术栈已从占位符更新为 Unity 实际配置
- `/Users/chen/Desktop/Dev/MyGameStudio/.claude/docs/technical-preferences.md` — 完整填写（引擎/平台/命名/性能/禁止模式/库/专家路由）
- `/Users/chen/Desktop/Dev/MyGameStudio/docs/engine-reference/unity/VERSION.md` — Unity 2022.3.62f2 版本参考（替换了旧的 Unity 6.3 模板）

### 设计文档（项目级别）
- `design/concept/shadow-memory.md` — 游戏概念文档
- `design/art/art-bible.md` — 美术圣经（色板/风格/物件/环境/影子/后处理/性能/无障碍）
- `design/gdd/systems-index.md` — 系统索引（15 个系统，含依赖图和实现顺序）

### MVP 系统 GDD（全部 APPROVED）
| GDD | 行数 | 覆盖内容 |
|-----|------|---------|
| `design/gdd/input-system.md` | ~364 | 触屏手势/PC 映射/手势冲突/延迟预算 |
| `design/gdd/urp-shadow-rendering.md` | ~429 | 影子渲染方案/性能分级/匹配检测/移动端优化 |
| `design/gdd/object-interaction.md` | ~472 | 拖拽/旋转/吸附/光源轨道/Game Feel |
| `design/gdd/chapter-state-and-save.md` | ~784 | 章节状态机/存档 JSON/IChapterProgress/Luban 配置 |
| `design/gdd/shadow-puzzle-system.md` | ~320 | 核心匹配/状态机/评分公式/调参旋钮 |
| `design/gdd/hint-system.md` | ~416 | 三层提示/触发条件/冷却/缺席谜题处理 |
| `design/gdd/ui-system.md` | ~713 | 9 个 UIWindow/TEngine UIModule 集成/层级策略 |
| `design/gdd/scene-management.md` | ~632 | 场景结构/Additive 加载/YooAsset 热更/内存预算 |

### Vertical Slice 系统 GDD（全部 APPROVED）
| GDD | 覆盖内容 |
|-----|---------|
| `design/gdd/audio-system.md` | 三层音频（Ambient/SFX/Music）、ducking、crossfade、配置表驱动 |
| `design/gdd/narrative-event-system.md` | 记忆重现演出、章节过渡、原子效果组合、Timeline 全屏接管、配置表驱动序列 |
| `design/gdd/tutorial-onboarding.md` | 教学步骤配置表、InputFilter 锁定操作、手势图片+文字提示、操作指南 |
| `design/gdd/settings-accessibility.md` | 音量/音效开关/灵敏度/振动/语言/帧率、TEngine 本地化方案、实时生效 |

### 技术原型（URP 阴影 — 初步验证通过）
- `design/prototype/urp-shadow-prototype-validation.md` — 验收报告（含 Editor + 真机数据）
- `Assets/Editor/ShadowPrototype/ShadowPrototypeSetup.cs` — Editor 一键设置脚本（创建 URP Assets + 搭建测试场景）
- `Assets/Scripts/Prototype/ShadowPrototypeManager.cs` — 场景管理 + 质量档位热切换 + 触屏按钮
- `Assets/Scripts/Prototype/SimpleDragController.cs` — 物件拖拽控制器（早期原型，已被 InteractionController 替代）
- `Assets/Scripts/Prototype/ShadowRTCapture.cs` — ShadowRT 采样 + AsyncGPUReadback
- `Assets/Scripts/Prototype/ShadowQualityProfiler.cs` — 性能数据采集 + CSV 导出

### 技术原型（Object Interaction — Editor 开发中）
- `Assets/Scripts/Prototype/InteractionConfig.cs` — ScriptableObject 配置（GDD 全部调参旋钮）
- `Assets/Scripts/Prototype/GridSystem.cs` — 格点吸附 + 边界夹持 + 吸附时长计算
- `Assets/Scripts/Prototype/InteractableObject.cs` — 物件状态机（6 状态）+ 吸附/回弹/缩放动画（独立计时器）
- `Assets/Scripts/Prototype/InteractionController.cs` — 输入处理 → Raycast → 手势识别（单指拖拽 + 双指旋转 + 胖手指补偿）
- `Assets/Scripts/Prototype/InteractionDebugPanel.cs` — 运行时调参面板 + FPS/状态/位置监控 + 边界可视化 + CSV 导出

### Cross-GDD Review
- `design/review/cross-gdd-review-2026-04-16.md` — 9 份 MVP GDD 一致性审查报告
- `design/review/cross-gdd-review-2026-04-21.md` — 13 份 GDD 全量审查报告（26 项修复后 → **APPROVED**）

### Architecture
- `docs/architecture/phase0-tr-baseline.md` — Phase 0 TR 基线（212 TRs + Engine Knowledge Gap Inventory）
- `docs/architecture/architecture.md` — **Master Architecture Document v1.0 Draft**（Phase 1-7 全阶段）
- `docs/architecture/tr-registry.yaml` — TR 注册表模板

### 原始设计素材（只读参考）
- `src/MyGame/Word/影子回忆解谜游戏_竞品调研.md` — 市场定位、差异化、Projected Dreams/Shadowmatic/In My Shadow 分析
- `src/MyGame/Word/影子回忆解谜游戏_主题与框架草案.md` — 核心主题、5 章结构、玩法方向、待补充项
- `src/MyGame/Word/笼中窥梦_策划分析.md` — 核心循环、谜题方法论、叙事设计
- `src/MyGame/Word/笼中窥梦_程序实现方案.md` — 系统分层、判定逻辑、数据结构、MVP 技术方案

---

## Key Design Decisions Made

1. **平台策略**: Mobile 优先，PC 后续 — 影响性能预算（<150 draw calls）和输入设计（Touch 为主）
2. **渲染管线**: URP — 移动端友好，影子投影性能可控
3. **5 章关系弧线结构**: 靠近 → 共同空间 → 共同生活 → 松动 → 缺席与重新理解
4. **4 条 Game Pillars**: 关系即谜题 / 日常即重量 / 克制表达 / 缺席比存在更有力
5. **匹配判定方案**: 作者预设 + 屏幕空间锚点混合（路线 A+B），MVP 阶段先用路线 A
6. **三层提示系统**: 区域关注 → 关系暗示 → 操作意图
7. **谜题状态机**: Locked → Idle → Active → NearMatch → PerfectMatch → Complete

---

## Open Questions (Awaiting Decision)

| Question | Context | Impact |
|----------|---------|--------|
| 伴侣离去的呈现方式 | 隐喻暗示 vs 关键时刻直接表达 | 影响叙事系统和章节 4-5 设计 |
| 玩家身份 | "整理记忆的人" vs "在回忆中行走的人" | 影响 UI 视角和叙事框架 |
| 每章终局大谜题 | 是否有更复杂的章节收束谜题 | 影响谜题数量和难度曲线 |
| 是否使用文字 | 完全无文本 vs 极少量物件标注 | 影响叙事表达和本地化 |
| ~~影子渲染方案~~ | **已确认：URP 实时阴影**（原型验证通过） | ~~关闭~~ |

---

## Next Actions (Prioritized)

> **GDD 全部 APPROVED，进入 Architecture 设计 + Pre-production 准备阶段。**

1. ~~**Master Architecture 设计**~~ ✅ **已完成** — `docs/architecture/architecture.md` v1.0 Draft
   - 5 层架构 / 15 系统模块所有权 / 6 条数据流 / 9 个 C# 接口 / 完整 GameEvent 总表
   - 26 个 ADR 识别（11 P0 + 7 P1 + 8 P2）/ 10 个 Open Questions
   - 30 TR 代表性覆盖验证 / 7 个覆盖缺口已标注

2. **Architecture Decision Records (ADRs)** ← **下一步**
   - **P0 必须先写 (11)**：ADR-001 ~ ADR-011（Foundation + Core 层决策，Sprint 1 启动前完成）
   - **P1 系统构建前写 (7)**：ADR-012 ~ ADR-018（Feature 层决策）
   - **P2 可延后 (8)**：ADR-019 ~ ADR-026（Presentation 层 / 优化）

3. **Object Interaction 原型 — 真机验证**（可并行）
   - 5+ 人真机测试（iPhone 13 Mini 为主）
   - Feel Acceptance Criteria 验收（见 `design/gdd/object-interaction.md`）

4. **Low 档 URP 优化**（可并行） — 为 Adreno 613 级别设备回调 Soft Shadow → Hard Shadow

5. **iPhone 13 Mini 补充验证**（待设备可用时） — Medium 档 60fps 目标确认

6. **Gate Check** — 全部 ADR 完成后执行 `/gate-check pre-production`

7. 全部通过 → 进入 **Pre-production** 阶段（Sprint Planning / Story Creation）

### 已完成 ✓
- ~~Render Pipeline Converter~~ — UIRoot URP 兼容、材质转换、0 问题
- ~~Vertical Slice 系统设计~~ — 4 份 VS GDD 全部完成
- ~~GDD 全面审查~~ — 13 份 GDD 全量审查，26 项修复后 APPROVED
- ~~Master Architecture~~ — v1.0 Draft，Phase 0-7 全阶段完成

---

## Technical Context (Quick Reference)

- **Unity 工程路径**: `src/MyGame/ShadowGame/`
- **启动入口**: `Assets/GameScripts/GameEntry.cs` → Procedure 链 → HotFix 热更加载 → `GameApp.cs`
- **热更程序集**: `GameLogic.asmdef`（游戏逻辑）/ `GameProto.asmdef`（Luban 配置）
- **非热更代码**: `GameEntry.cs` + `Procedure/*.cs`（启动流程）
- **Wiki 知识库**: `repowiki/zh/content/`（TEngine 框架文档，通过 wiki-query-agent 查询）
- **编码红线**: UniTask（禁 Coroutine）/ GameModule.XXX（禁直接 GetModule）/ 必须释放资源 / 禁硬编码数值
