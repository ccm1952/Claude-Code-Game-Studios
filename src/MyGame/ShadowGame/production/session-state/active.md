# ShadowGame — Session State

> Last updated: 2026-04-22 (session 14)
> Phase: **Pre-Production** — **Sprint 2 READY TO START**（Plan + QA Plan 完成，SP-011 Spike 前置待执行）
> Next milestone: SP-011 Spike → Track A (Chapter State) → Track B (Scene Mgmt) → Track C (Object Int)

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
- **11 P0 ADRs**（全部 Status: Proposed）:
  - `adr-001-tengine-framework.md` — TEngine 6.0 框架采用
  - `adr-002-urp-shadow-rendering.md` — URP 影子渲染管线
  - `adr-003-mobile-first-platform.md` — 移动端优先平台策略
  - `adr-004-hybridclr-assembly.md` — HybridCLR 程序集边界
  - `adr-005-yooasset-lifecycle.md` — YooAsset 资源加载生命周期
  - `adr-006-gameevent-protocol.md` — GameEvent 通信协议
  - `adr-007-luban-access.md` — Luban 配置表访问模式
  - `adr-008-save-system.md` — 存档系统架构
  - `adr-009-scene-lifecycle.md` — 场景生命周期与 Additive 策略
  - `adr-010-input-abstraction.md` — 输入抽象（手势/阻断/过滤）
  - `adr-011-uiwindow-management.md` — UIWindow 管理与层级策略

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

2. ~~**P0 Architecture Decision Records (11)**~~ ✅ **已完成** — 全部 11 个 P0 ADR 已生成
   - ADR-001 TEngine / ADR-002 URP / ADR-003 Mobile-First / ADR-004 HybridCLR / ADR-005 YooAsset
   - ADR-006 GameEvent / ADR-007 Luban / ADR-008 Save / ADR-009 Scene / ADR-010 Input / ADR-011 UIWindow
   - ~~**P1 待写 (7)**~~ ✅ ADR-012 ~ ADR-018（Feature 层决策，全部已生成）
   - **P2 可延后 (8)**：ADR-019 ~ ADR-026（Presentation 层 / 优化）

3. **Object Interaction 原型 — 真机验证**（可并行）
   - 5+ 人真机测试（iPhone 13 Mini 为主）
   - Feel Acceptance Criteria 验收（见 `design/gdd/object-interaction.md`）

4. **Low 档 URP 优化**（可并行） — 为 Adreno 613 级别设备回调 Soft Shadow → Hard Shadow

5. **iPhone 13 Mini 补充验证**（待设备可用时） — Medium 档 60fps 目标确认

6. ~~**Gate Check**~~ ✅ 已执行 — 判定 **CONCERNS**（5 项缺失工件，需补齐后重新通过）
   - 详见 `docs/architecture/gate-check-pre-production-2026-04-22.md`

7. ~~**Gate 阻塞项修复**~~ ✅ **全部完成**
   - [x] ~~P0: 运行 `/test-setup` 初始化测试框架 + CI/CD~~ ✅
   - [x] ~~P0: 生成 `architecture-traceability.md` 可追溯性索引~~ ✅
   - [x] ~~P1: 解决 Event ID 冲突（ADR-006 vs ADR-010/011）~~ ✅ 3 个冲突全部修复
   - [x] ~~P1: 创建 `design/ux/interaction-patterns.md` + 核心屏幕 UX spec~~ ✅
   - [x] ~~P2: 创建 `design/accessibility-requirements.md`~~ ✅
   - **下一步 → 重新 Gate Check → 进入 Pre-production**

8. ~~**Sprint 0 Spike**~~ ✅ **全部 10 个 findings 完成**
   - 详见 `docs/architecture/findings/SP-001~SP-010`
   - **10/10 全部定稿**；SP-007 (HybridCLR+AsyncGPU) 真机验证 ✅ PASS

9. ~~**Control Manifest**~~ ✅ **已生成** — `docs/architecture/control-manifest.md` (576 行)
   - 覆盖 18 ADR + 10 SP findings，按 Foundation/Core/Feature/Presentation 四层组织

10. ~~**Epics**~~ ✅ **13 个 Epic 全部创建**
    - 详见 `production/epics/index.md`
    - Foundation (2) + Core (4) + Feature (5) + Presentation (2)

11. ~~**Foundation + Core Stories**~~ ✅ **39 个 Story 已创建**
    - input-system: 8 stories
    - urp-shadow-rendering: 7 stories
    - scene-management: 6 stories
    - object-interaction: 7 stories
    - chapter-state: 5 stories
    - save-system: 6 stories

12. ~~**Feature + Presentation Stories**~~ ✅ **52 个 Story 已创建**
    - shadow-puzzle: 8 stories
    - hint-system: 6 stories
    - narrative-event: 8 stories
    - audio-system: 7 stories
    - ui-system: 10 stories
    - tutorial-onboarding: 6 stories
    - settings-accessibility: 7 stories

13. ~~**Story 001 完成**~~ ✅ `input-system/story-001-gesture-state-machine` — **COMPLETE**
    - Code Review: PASS（零 ADR 违规，GestureData 字段名已对齐 ADR-010）
    - Tests: 8/8 PASS（Unity Test Runner EditMode）
    - AC: 12/12 已验证（真机性能 DEFERRED 到集成阶段）

14. ~~**Story 002 完成**~~ ✅ `input-system/story-002-dual-finger-gestures` — **COMPLETE**
    - Tests: 9/9 PASS（总计 24/24 全绿）
    - AC: 11/11 已验证

15. ~~**Story 003 + 006 完成**~~ ✅ — **COMPLETE**
    - S1-03: EventId（1000–1004）+ GestureDispatcher + 6 测试
    - S1-06: InputConfigFromLuban（DPI+灵敏度+Luban 预留）+ 10 测试
    - 40/40 全绿

16. ~~**Story 004 + 005 完成**~~ ✅ — **COMPLETE**
    - S1-04: InputBlocker（token 栈 + 泄漏检测 + ForcePopAll）+ 9 测试
    - S1-05: InputFilter（单一激活白名单 + 深拷贝 + 覆盖语义）+ 10 测试
    - 59/59 全绿
    - **Input System epic 全部 6 个 Must Have stories 完成！**

17. ~~**S1-07 + S1-09 + S1-11 完成**~~ ✅ — **COMPLETE**
    - S1-07: ShadowRTConfig（三档质量参数 High/Medium/Low）+ 9 测试
    - S1-09: SaveData schema（JSON + IChapterProgress + ISaveMigration）+ 9 测试
    - S1-11: ChapterDataModel（ChapterStateManager + PuzzleStateEnum + IChapterProgress）+ 10 测试
    - 87/87 全绿
    - **Must Have 8/8 全部完成！Should Have 进行中**

18. ~~**S1-10 完成**~~ ✅ — **COMPLETE**
    - SaveManager（原子写入 + CRC32 + 备份 + SemaphoreSlim）+ Crc32 纯函数
    - 13 测试（CRC32 7 + SaveManager 6）
    - 100/100 全绿
    - **Should Have 1/3 完成**

19. ~~**S1-13 完成**~~ ✅ — **COMPLETE**
    - LoadAsync（UniTask 异步 fallback chain）+ RegisterMigration 迁移链
    - 3 新测试（迁移链 + 空文件降级）
    - 103/103 全绿

20. ~~**S1-08 完成**~~ ✅ — **COMPLETE**
    - WallReceiver.shader（纯 HLSL Unlit，SRP Batcher 兼容，CBUFFER 声明）
    - ShadowRenderingModule.cs（Init/Dispose 生命周期 + Glow/Style API）
    - Frame Debugger 确认 SRP Batch ✅
    - 10 测试，113/113 全绿
    - Visual 完整验证（明暗比/GPU 时间）延后到 ShadowRT 集成阶段
    - **Sprint 1 Must Have 8/8 全部完成！**

21. ~~**S1-12 完成**~~ ✅ — **COMPLETE**
    - ShadowRTReadback（AsyncGPUReadback 管线 + 隔帧控制 + 失败降级）
    - EventId_Rendering（Evt_ShadowRT_Updated = 1100）+ ShadowRTData payload
    - 9 测试，122/122 全绿
    - **Sprint 1 全部 13/13 stories 完成！**

22. ~~**Sprint 1 Retrospective**~~ ✅ — **COMPLETE**
    - `production/sprints/sprint-1-retrospective.md`
    - 承诺 11，实际 **13/13**（Must 8 + Should 3 + Nice 2），超额 +18%
    - 122 EditMode 测试全绿，零 ADR 违规，零新增技术债务
    - 5 条 Action Items for Sprint 2（Top: story 复杂度点数替代人日 / story 模板加枚举校验）

23. ~~**Sprint 2 Plan**~~ ✅ — **CREATED**
    - `production/sprints/sprint-2.md` + `production/sprint-status.yaml`
    - 承诺 14 stories / 25 点（Must 10 + Should 4），Nice 3 / 7 点延伸
    - **SP-011 YooAsset Additive Scene Spike 前置**（0.5 点，MEDIUM 风险预先消化）
    - 临界路径：Chapter State 闭环 → Scene Mgmt 骨架 → Object Interaction 最小可交互
    - 三轨并行：Chapter 6点 ‖ Scene 4.5点 ‖ ObjectInt 6点

24. ~~**Sprint 2 QA Plan**~~ ✅ — **DRAFT**
    - `production/qa/qa-plan-sprint-2-2026-04-22.md`
    - 17 stories + SP-011 全覆盖：15 EditMode / 3 PlayMode / 4 manual evidence / 3 playtest
    - 应用 Sprint 1 Retro Actions：
      - #1 枚举/字段 grep 校验前置（story 实施前）
      - #4 Visual stories 参数层抽测（S2-15 先测 SelectionFeedbackConfig）
    - Luban 配置兜底策略（in-memory fixture）已就位

### 已完成 ✓
- ~~Render Pipeline Converter~~ — UIRoot URP 兼容、材质转换、0 问题
- ~~Vertical Slice 系统设计~~ — 4 份 VS GDD 全部完成
- ~~GDD 全面审查~~ — 13 份 GDD 全量审查，26 项修复后 APPROVED
- ~~Master Architecture~~ — v1.0 Draft，Phase 0-7 全阶段完成
- ~~P0 ADRs (11)~~ — 全部 11 个 Foundation + Core 层 ADR 已生成（Status: Proposed）
- ~~P1 ADRs (7)~~ — 全部 7 个 Feature 层 ADR 已生成（Status: Proposed）
- ~~Architecture Review~~ — 判定 CONCERNS（212 TRs: 124 ✅ / 87 ⚠️ / 1 ❌ / 2 CRITICAL 冲突）
- ~~Gate Check~~ — Technical Setup → Pre-Production 判定 CONCERNS（5 项缺失工件）
- ~~Sprint 0 Spike Plan~~ — 10 个技术验证任务 / 28h / SP-001/002 已预解决
- ~~Gate 阻塞项修复~~ — 测试框架 + Traceability Index + Event ID 冲突 × 3 + UX 交互规范 + 无障碍需求
  - ~~Gate Check v2~~ — **PASS**，Technical Setup → Pre-Production 通过
  - ~~Sprint 0 Spike~~ — 10 findings（SP-001~010），9 个已定稿，SP-007 待真机验证
  - ~~Control Manifest~~ — 576 行程序员规则表，覆盖全部 18 ADR + 10 findings
  - ~~Epics~~ — 13 个 Epic 文件 + index（Foundation 2 + Core 4 + Feature 5 + Presentation 2）
  - ~~Foundation + Core Stories~~ — 39 个 Story 文件（含完整 AC、QA Test Cases、Implementation Notes）
  - ~~Feature + Presentation Stories~~ — 52 个 Story 文件（全部 13 个 epic 合计 **91 Stories**）

---

## Technical Context (Quick Reference)

- **Unity 工程路径**: `src/MyGame/ShadowGame/`
- **启动入口**: `Assets/GameScripts/GameEntry.cs` → Procedure 链 → HotFix 热更加载 → `GameApp.cs`
- **热更程序集**: `GameLogic.asmdef`（游戏逻辑）/ `GameProto.asmdef`（Luban 配置）
- **非热更代码**: `GameEntry.cs` + `Procedure/*.cs`（启动流程）
- **Wiki 知识库**: `repowiki/zh/content/`（TEngine 框架文档，通过 wiki-query-agent 查询）
- **编码红线**: UniTask（禁 Coroutine）/ GameModule.XXX（禁直接 GetModule）/ 必须释放资源 / 禁硬编码数值
