// 该文件由Cursor 自动生成

# Epic: Vertical Slice — Chapter 1 (靠近)

> **Layer**: Vertical Slice (VS)
> **GDD**:
> - `design/concept/shadow-memory.md` (5 章关系弧线 line 131；Chapter 1 为"靠近"阶段；2 物件 + 1 光源 design constraint line 106)
> - `design/gdd/scene-management.md` (Scene Architecture / Registry line 67 — `Chapter_01_Approach`)
> - `design/gdd/object-interaction.md` (line 86 — Chapter 1 光源不可交互)
> - `design/gdd/shadow-puzzle-system.md` (line 42 — Chapter 1 不引入光源操作；line 222 表 Ch.1 维度)
> - `design/art/art-bible.md` (line 53/121/180 — Chapter 1 色温 + 空间风格 + 投影质量)
> - `design/gdd/urp-shadow-rendering.md` (line 117 — Ch.1 投影对比 5:1 + Penumbra 1.0)
>
> **Architecture Module**: VS Build Vertical (cross-system integration; first end-to-end playable slice)
> **Governing ADRs**:
> - **ADR-009** (Scene Lifecycle) — 11-step transition / Additive scene loading / 6-state SceneManager / `_chapterDataProvider` 注入 / fadeOverlay 注入
> - **ADR-030** (VS-Late Pattern) §"Sprint 5-6 VS Build Commitment" 第 1 项
> - **ADR-029 V2.0** (R1/R2/R3 readiness gate)
>
> **Engine Risk**: LOW (所有依赖 framework 已实施；本 epic 主要是 asset + integration 工作)
> **Status**: In-Progress (Sprint 5; story-001 ✅ DONE 2026-05-09 + story-001b ✅ DONE 2026-05-09 + story-001c ✅ DONE 2026-05-09 spec ↔ impl alignment fix + story-002 Draft 2026-05-11 end-to-end happy path + story-003 Sprint 6 placeholder error/restart path)
> **Stories**: 5 stories (S5-01 scene asset ✅ + S5-1b boot integration ✅ + S5-1c listener-path driver fix ✅ + S5-02 end-to-end happy path Draft + S5-2b error/restart placeholder Sprint 6) — *2026-05-11 architecture change: S5-02 contained in this epic dir; S5-02b error/restart path 拆出 S5-2b Sprint 6 backlog*

---

## Stories

| Story | Title | Type | Status | SP |
|-------|-------|------|--------|----|
| [story-001](story-001-scene-build.md) | Chapter 1 (靠近) Unity scene 实体构建首版 | Asset | ✅ Done 2026-05-09 | 3 |
| [story-001b](story-001b-scenemanager-boot-integration.md) | SceneManager boot pipeline 接入 + fixture ChapterDataProvider | Logic / Integration | ✅ Done 2026-05-09 | 3 |
| [story-001c](story-001c-adr009-listener-path-driver.md) | ADR-009 production listener-path driver 接入（移除 S5-1b F4 dev-only stub）| Logic / Integration | ✅ Done 2026-05-09 | 2 |
| [story-002](story-002-end-to-end-flow.md) | Chapter 1 end-to-end 5 系统串通可玩（happy path）| Integration | Draft 2026-05-11 | 2 |
| story-003 (placeholder) | Chapter 1 error/restart path（unknown chapter / mid-transition cancel / asset load fail / restart-from-Error）| Integration | Sprint 6 backlog | 1 |

---

## Overview

VS Chapter 1 = **ADR-030 §VS Build Commitment 第 1 项的核心交付**。本 epic 收敛 chapter 1 的两件事：

1. **scene asset 实体构建**（story-001）：在 `Assets/Scenes/Chapter_01_Approach.unity` 建好 GameObject 层级 / 灯光 / 材质 / 投影墙面 / 物件 / 光源 / narrative trigger zone stub。Asset Type，不写 production C# 代码。
2. **SceneManager 在生产 boot pipeline 的真实接入**（story-001b）：在 `GameApp.Entrance` 内 `new SceneManager()` + `Init()` + `RegisterChapterDataProvider(fixture)` + `RegisterFadeOverlay(NoOp 兜底)` + 注册 dev menu/FSM state 触发 `LoadChapterSceneAsync(1)`。Logic / Integration Type，含 R3 PlayMode probe (framework boundary mandatory)。

S5-02 端到端串通 5 个 P1 系统使用本 epic 提供的 scene + boot 接入。

### 不在本 epic（明示）

- **Luban TbChapter 真接入**（user decision 2026-05-09: post-VS；S5-1b 用 hardcoded fixture provider 兜底）
- **S5-04** art-bible Status: Draft → Accepted（独立 governance story；本 epic story-001 起步用 placeholder URP/Lit material，待 S5-04 sign-off 后 polish）
- **S5-08** UIModule Setup（ui-system epic 独立；本 story-002 hard prerequisite — 必须 S5-08 dev-story DONE 才 unblock S5-02 dev-story；per Sprint 5 序列 [A] serial：S5-04 → S5-08 → S5-02 → S5-07）
- **ui-system-006 完整 main menu UIWindow** + **-002 game-hud / -003 pause-menu / -004 puzzle-complete / -005 chapter-select / -007 settings-panel** — 本 epic story-002 main menu 仅 minimal Button × 2 inline impl per 决策 [A]，完整 UIWindow 留 Sprint 6+
- **多 chapter 切换 (chapter 2-5)** — 留 Sprint 6+
- **Save / Load round-trip / Pause / Resume during scene transition** — 留 chapter-state epic Sprint 6+
- **2026-05-11 architecture change**: 原 EPIC.md 列 "S5-02 不在本 epic dir" 已 supersede — S5-02 现在物理放在本 epic dir per Sprint 5 序列协调与 sprint-status.yaml line 862 file path 一致；本块描述更新 reflect 当前架构

---

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-009: Scene Lifecycle | 11-step transition / Additive scene loading / mandatory cleanup / 6-state machine / `_chapterDataProvider` 注入 / fadeOverlay 注入 | LOW |
| ADR-030: VS-Late Pattern | Sprint 5-6 VS Build Commitment 第 1 项 — chapter 1 (靠近) end-to-end 实体构建；≥3 playtest sessions for fun loop validation | NONE (process) |
| ADR-029 V2.0: Story Impl Notes Verification | R1 (skim) + R2 (gap probe) + **R3 (PlayMode framework boundary mandatory)** readiness gate；Asset type 适用 R3 N/A reasoned | NONE (process) |

---

## GDD Requirements

| TR-ID | Requirement | ADR Coverage | Story |
|-------|-------------|:------------:|-------|
| TR-scene-001 | Additive scene 架构 (BootScene + MainScene + 1 章节) | ADR-009 ✅ | story-001 + story-001b |
| TR-scene-002 | 章节 scene name = `Chapter_0X_Xxxx` (per scene-management.md line 67) | ADR-009 ✅ | story-001 |
| TR-scene-005 | 11-step transition flow（FadeOut → Unload → GC → Load → FadeIn） | ADR-009 ✅ | story-001b |
| TR-scene-013 | Startup flow Boot → TEngine → HybridCLR → YooAsset | ADR-009 ✅ | story-001b |
| TR-art-bible-001 | Chapter 1 色温 4000-4500K 柔象牙 + 淡暖黄 (art-bible line 53) | (Art layer constraint) | story-001 |
| TR-objint-002 | Chapter 1 仅 2 个可操作物件 + 1 个固定光源 (object-interaction line 86; concept line 106) | (Design constraint) | story-001 |
| TR-puzzle-001 | 第一章不引入光源操作 (shadow-puzzle-system line 42) | (Design constraint) | story-001 |
| TR-narr-trigger-stub | Chapter 1 ≥1 narrative trigger zone stub (sprint-status notes) | ADR-016 ⚠️ stub only | story-001 |
| TR-scene-load-fixture | fixture ChapterDataProvider 在 boot pipeline 注册；LoadChapterSceneAsync(1) 可成功执行 | ADR-009 + ADR-007 ⚠️ deficiency-flagged | story-001b |
| TR-scene-listener-driver | OnRequestSceneChange handler internal driver 闭环 — orchestrate 11-step internally per ADR-009 §Decision line 386 | ADR-009 ⚠️ deficiency-flagged | story-001c |

> **2026-05-09 readiness doc-fix**: 原表内 `TR-scene-015` (`_chapterDataProvider` 注入) 与 `tr-registry.yaml:576` 实际 requirement (`Emotional weight scales fade duration`) 语义不符；改用 TR-scene-001 / TR-scene-005 / TR-scene-013 真实贴合 boot pipeline 接入工作。`TR-scene-load-fixture` 与 `TR-scene-listener-driver` 仍未在 registry 注册，按 ADR-029 V2.0 deficiency-flag 协议显式标记，待下次 `/architecture-review` 落册。

---

## Sprint 5 Schedule

- **story-001** (S5-01): ✅ Done 2026-05-09 — chapter scene 实体构建首版 8/8 AC PASS
- **story-001b** (S5-1b): ✅ Done 2026-05-09 — SceneManager boot pipeline 接入 + 5/5 R3 PASS + 22/22 asserts；F4 dev-only stub temporarily in DevTestState（待 story-001c 移除）
- **story-001c** (S5-1c): ✅ Done 2026-05-09 — ADR-009 spec ↔ impl alignment fix；SceneManager.OnRequestSceneChange 内置 DriveTransitionAsync(targetChapterId).Forget() 自闭环 listener；F4 dev-only stub in DevTestState 永久移除；ADR-009 §History 加 1 条 amendment entry；R3 PlayMode 5/5 PASS + 24/24 asserts；S5-02 启动前 cleanup 完成
- **story-002** (S5-02): Draft 2026-05-11 — Chapter 1 end-to-end 5 系统 happy path；2 SP；hard prerequisite = S5-04 art-bible sign-off + S5-08 UIModule Setup dev-story DONE；按 Sprint 5 序列 [A] serial：S5-04 → S5-08 → S5-02 → S5-07；待 /story-readiness gate（5 大块 wiring R2 实证）通过后转 Ready
- **story-003** (S5-2b placeholder): Sprint 6 backlog — Chapter 1 error/restart path（unknown chapter / mid-transition cancel / asset load fail / restart-from-Error）；1 SP；本 sprint 不写 file，仅 sprint-status.yaml entry placeholder

### 风险

| 风险 | 缓解 |
|------|------|
| story-001 unity-mcp batch 中途 Bridge disconnect | failFast=true + 单 batch ≤25 commands 拆分；如 Bridge 中断手动重启后续跑（已 mitigated, story-001 ✅） |
| story-001b R3 PlayMode probe 暴露 framework boundary drift | per ADR-029 V2.0 V2-5；如出现 capture 为新 R3 case + 沉淀 problem memo（已 mitigated, S5-1b ✅ + 3 deficiency surfaced：ADR-009 driver gap → story-001c / S5-01 path 错位 ✅ resolved / S5-01 AudioListener residual ✅ resolved by Phase A2）|
| Luban TbChapter post-VS 接入时 fixture provider migration cost | story-001b fixture provider 用 Func\<int, ChapterData\> 同 Luban 真接入签名；migration 仅替换一行 lambda（仍待 post-VS） |
| story-001c async void listener handler 异常逃逸到 Unity log | 与项目内 IInputBlockerEvent / ISettingsEvent / IAudioEvent 现有 listener 模式一致；BeginTransitionAsync 内部已 fail-loud 协议（state=Error + OnSceneLoadFailed）；story-001c DriveTransitionAsync catch 仅兜底；统一 review 留 Sprint 5/6 retro |
| story-002 5 大块 wiring uncertainty (R2.1~R2.5) — chapter 1 listener handler 是否已挂 / prefab InteractableObject / puzzle config injection / UIModule API 路径 | /story-readiness gate 阶段强制 R2 grep 实证；任意 0-hit 走 ADR-029 V2.0 deficiency-flag PASS 路径补 production wiring；R2.5 必待 S5-08 done 才能 verify（Sprint 5 serial 序列保证）|
| story-002 5 系统 cross-system event 顺序 drift (Type-2(c) candidate) | R3 spike P1-P5 顺序 assert + spike Tester listener event log dump；如 drift 累计为 V3 #2 Type-5 候选 dp / V3 #6 Type-6 候选 dp |
