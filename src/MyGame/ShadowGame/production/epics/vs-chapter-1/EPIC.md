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
> **Status**: In-Progress (Sprint 5; 2026-05-09 scope split discussion 完成)
> **Stories**: 2 stories (S5-01 scene asset + S5-1b boot integration)；S5-02 end-to-end 不在本 epic dir

---

## Stories

| Story | Title | Type | Status | SP |
|-------|-------|------|--------|----|
| [story-001](story-001-scene-build.md) | Chapter 1 (靠近) Unity scene 实体构建首版 | Asset | Ready | 3 |
| story-001b *(file 待 readiness 时创建)* | SceneManager boot pipeline 接入 + fixture ChapterDataProvider | Logic / Integration | Backlog | 3 |

---

## Overview

VS Chapter 1 = **ADR-030 §VS Build Commitment 第 1 项的核心交付**。本 epic 收敛 chapter 1 的两件事：

1. **scene asset 实体构建**（story-001）：在 `Assets/Scenes/Chapter_01_Approach.unity` 建好 GameObject 层级 / 灯光 / 材质 / 投影墙面 / 物件 / 光源 / narrative trigger zone stub。Asset Type，不写 production C# 代码。
2. **SceneManager 在生产 boot pipeline 的真实接入**（story-001b）：在 `GameApp.Entrance` 内 `new SceneManager()` + `Init()` + `RegisterChapterDataProvider(fixture)` + `RegisterFadeOverlay(NoOp 兜底)` + 注册 dev menu/FSM state 触发 `LoadChapterSceneAsync(1)`。Logic / Integration Type，含 R3 PlayMode probe (framework boundary mandatory)。

S5-02 端到端串通 5 个 P1 系统使用本 epic 提供的 scene + boot 接入。

### 不在本 epic（明示）

- **S5-02** 端到端串通（独立 sprint-status entry）
- **Luban TbChapter 真接入**（user decision 2026-05-09: post-VS；S5-1b 用 hardcoded fixture provider 兜底）
- **S5-04** art-bible Status: Draft → Accepted（独立 governance story；本 epic story-001 起步用 placeholder URP/Lit material，待 S5-04 sign-off 后 polish）
- **投影 puzzle production 配置注入**（S5-03 ✅ DONE 但本 scene 不实例化具体 puzzle config，留 S5-02 决定）

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
| TR-scene-001 | Additive scene 架构 (BootScene + MainScene + 1 章节) | ADR-009 ✅ | story-001 |
| TR-scene-002 | 章节 scene name = `Chapter_0X_Xxxx` (per scene-management.md line 67) | ADR-009 ✅ | story-001 |
| TR-scene-015 | `_chapterDataProvider` 注入；no hardcoded scene names (Sprint 2 S2-07 落地) | ADR-009 + ADR-007 ✅ | story-001b |
| TR-art-bible-001 | Chapter 1 色温 4000-4500K 柔象牙 + 淡暖黄 (art-bible line 53) | (Art layer constraint) | story-001 |
| TR-objint-002 | Chapter 1 仅 2 个可操作物件 + 1 个固定光源 (object-interaction line 86; concept line 106) | (Design constraint) | story-001 |
| TR-puzzle-001 | 第一章不引入光源操作 (shadow-puzzle-system line 42) | (Design constraint) | story-001 |
| TR-narr-trigger-stub | Chapter 1 ≥1 narrative trigger zone stub (sprint-status notes) | ADR-016 ⚠️ stub only | story-001 |
| TR-scene-load-fixture | fixture ChapterDataProvider 在 boot pipeline 注册；LoadChapterSceneAsync(1) 可成功执行 | ADR-009 + ADR-007 ⚠️ | story-001b |

---

## Sprint 5 Schedule

- **story-001** (S5-01): Sprint 5 (5-06~5-20) — 当前 sprint，即将进入 dev
- **story-001b** (S5-1b): Sprint 5 (5-06~5-20) — story-001 完成后立即接，**S5-02 端到端启动的硬阻塞**

### 风险

| 风险 | 缓解 |
|------|------|
| story-001 unity-mcp batch 中途 Bridge disconnect | failFast=true + 单 batch ≤25 commands 拆分；如 Bridge 中断手动重启后续跑 |
| story-001b R3 PlayMode probe 暴露 framework boundary drift | per ADR-029 V2.0 V2-5；如出现 capture 为新 R3 case + 沉淀 problem memo |
| Luban TbChapter post-VS 接入时 fixture provider migration cost | story-001b 设计时 fixture provider 用 Func\<int, ChapterData\> 同 Luban 真接入签名；migration 仅替换一行 lambda |
