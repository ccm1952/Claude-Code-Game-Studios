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
> - **ADR-010** (Input Abstraction) — *(2026-05-14 Sprint 6 emergent fix Track F partial scope pull-forward)* GestureRecognizer FSM + IGestureEvent contract + Mouse/Touch input pipeline (chapter 1 fun loop minimal scope；Sprint 7+ ADR-010 full epic 余下 multi-touch / Pinch / Rotate / InputBlocker listener-side / 真机 testing)
> - **ADR-012** (Shadow Match Calculation) — *(2026-05-14 Sprint 6 emergent fix Track F)* 光线 + 物件 + 投影墙面 raycast / projection / area overlap algorithm + PerfectMatch threshold ≥0.95 + IPuzzleEvent.OnMatchScoreUpdated/OnPerfectMatch sender
> - **ADR-013** (Object Interaction State Machine) — *(2026-05-14 Sprint 6 emergent fix Track F)* InteractableObject FSM + InteractionCoordinator Inspector 注入 + InteractionLockManager + PuzzleConfigProvider 静态注入约定
> - **ADR-030** (VS-Late Pattern) §"Sprint 5-6 VS Build Commitment" 第 1 项
> - **ADR-029 V2.0 / V3.0.1** (R1/R2/R3 readiness gate + V3.0.1 dp15 sniff sub-clause "production caller hit > 0" 试点 pilot)
>
> **Engine Risk**: LOW (所有依赖 framework 已实施；本 epic 主要是 asset + integration + Sprint 6 emergent fix wiring 工作；ADR-012 ShadowMatchCalculator Sprint 4 SP-018 实施现状 R2.1 verify 关键 unknown)
> **Status**: **⚠️ Sprint 6 emergent fix Track F NEW in_progress** (Sprint 5+6; story-001/-001b/-001c ✅ + story-002 ✅ + story-003 ✅ + **story-004 ✅ Done 2026-05-14** + **story-005 ✅ Done 2026-06-12** + **story-006 ✅ Done 2026-06-15 provider injection R3 6/6 PASS 234ms** + story-007~008 📝 Draft)
> **Stories**: 10 stories (**8 ✅ done — S5-01/-1b/-1c/S5-02/S6-04/S6-13/S6-14/S6-15** + **2 📝 Draft Track F 余下** — story-007 shadowmatch wire + story-008 end-to-end smoke replay) — *Track F 3/5 done；NEXT story-007 ~2-3 hr；block S6-02/S6-03/S6-10 until Track F complete*

---

## Stories

| Story | Title | Type | Status | SP |
|-------|-------|------|--------|----|
| [story-001](story-001-scene-build.md) | Chapter 1 (靠近) Unity scene 实体构建首版 | Asset | ✅ Done 2026-05-09 | 3 |
| [story-001b](story-001b-scenemanager-boot-integration.md) | SceneManager boot pipeline 接入 + fixture ChapterDataProvider | Logic / Integration | ✅ Done 2026-05-09 | 3 |
| [story-001c](story-001c-adr009-listener-path-driver.md) | ADR-009 production listener-path driver 接入（移除 S5-1b F4 dev-only stub）| Logic / Integration | ✅ Done 2026-05-09 | 2 |
| [story-002](story-002-end-to-end-flow.md) | Chapter 1 end-to-end 5 系统串通可玩（happy path）| Integration | ✅ Done 2026-05-12 | 2 |
| [story-003](story-003-error-restart-path.md) | Chapter 1 error/restart path（unknown chapter TryResolveOrFail / newest-wins pending / asset load fail retry exhaust / RecoverToIdle restart）*(2026-05-13 Sprint 6 S6-04 V3.0.1 vendor reality compliant rewrite + narrow scope [A] 0 production code change — spike only — wording drift amend: mid-transition cancel → newest-wins pending; repeated rapid debounce → newest-wins overwrite)* | Integration | ✅ Done 2026-05-13 | 1 |
| [story-004](story-004-input-pipeline-wiring.md) | Touch/Mouse → GestureRecognizer FSM → GestureDispatcher.Dispatch production wire *(Sprint 6 emergent fix Track F NEW; ADR-010 部分 scope pull-forward; **V3.0.1 dp15 sniff sub-clause 试点 第 1 个 production caller hit > 0 修复 case PASS** ⭐)* | Logic / Integration | ✅ Done 2026-05-14 | 2-3 |
| [story-005](story-005-chapter-1-scene-wiring.md) | Chapter_01_Approach.unity 加 InteractionCoordinator GameObject + Object_01/02 InteractableObject MonoBehaviour 挂载 + child Hitbox2D 2D collider 同存解 [A2] *(Track F S0-2+S0-3; R3 6/6 PASS 252ms)* | Asset / Integration | ✅ Done 2026-06-12 | 1-1.5 |
| [story-006](story-006-gameapp-provider-injection.md) | GameApp.Init 调 InteractableObject.RegisterPuzzleConfigProvider + InteractionCoordinator.RegisterInputConfigProvider *(Track F S0-4; R3 6/6 PASS 234ms)* | Logic | ✅ Done 2026-06-15 | 1 |
| [story-007](story-007-shadowmatch-production-wire.md) | ADR-012 ShadowMatchCalculator listener 物件 transform → 阴影匹配 → IPuzzleEvent.OnMatchScoreUpdated/OnPerfectMatch production fire *(Sprint 6 emergent fix Track F NEW; scope 风险最高 — Sprint 4 SP-018 现状 R2.1 verify 关键 unknown)* | Logic / Integration | 📝 Draft 2026-05-14 | 2-3 |
| [story-008](story-008-end-to-end-smoke-replay.md) | S5-02 spike 重写 — InputSimulation Mouse Tap+Drag → 自然 production wiring round-trip + V3.0.1 dp15 sniff sub-clause 试点 pilot *(Sprint 6 emergent fix Track F NEW; final pilot)* | Integration | 📝 Draft 2026-05-14 | 1 |

---

## Overview

VS Chapter 1 = **ADR-030 §VS Build Commitment 第 1 项的核心交付**。本 epic 收敛 chapter 1 的几件事：

1. **scene asset 实体构建**（story-001）：在 `Assets/AssetRaw/Scenes/Chapter_01_Approach.unity` 建好 GameObject 层级 / 灯光 / 材质 / 投影墙面 / 物件 / 光源 / narrative trigger zone stub。Asset Type，不写 production C# 代码。
2. **SceneManager 在生产 boot pipeline 的真实接入**（story-001b/c）：在 `GameApp.Entrance` 内 `new SceneManager()` + `Init()` + `RegisterChapterDataProvider(fixture)` + `RegisterFadeOverlay(NoOp 兜底)` + 注册 dev menu/FSM state 触发 `LoadChapterSceneAsync(1)`。Logic / Integration Type，含 R3 PlayMode probe (framework boundary mandatory)。
3. **Chapter 1 end-to-end 5 系统串通 happy path**（story-002）：5 个 P1 系统使用本 epic 提供的 scene + boot 接入。
4. **Chapter 1 error/restart path 补全**（story-003 / S6-04）：unknown chapter / newest-wins pending / asset load fail retry exhaust / RecoverToIdle restart。
5. **Sprint 6 emergent fix Track F NEW** (story-004 ~ -008): chapter 1 production wiring 补全 — input pipeline (story-004) + chapter scene wiring (story-005) + GameApp provider injection (story-006) + ShadowMatch production wire (story-007) + end-to-end smoke replay (story-008 dp15 sniff sub-clause pilot)。**派生**自 S6-01 Phase 2.1 manual playtest NEEDS-WORK 揭露 5 处 chapter 1 production wiring gap (Object_01 无法交互 root cause)。Sprint 2-5 五个 sprint 期间 ObjectInteraction system EditMode-green-but-not-wired 反模式直接被 manual playtest 5 min 内穿透 → V3.0.1 dp15 candidate "EditMode green ≠ production wired sniff"。

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
| **ADR-010: Input Abstraction** *(2026-05-14 Sprint 6 partial scope pull-forward)* | GestureRecognizer FSM + IGestureEvent contract (OnTap/OnDrag/OnRotate/OnPinch/OnLightDrag) + Mouse/Touch input pipeline (chapter 1 fun loop minimal Tap+Drag scope；Sprint 7+ multi-touch / Pinch / Rotate / InputBlocker listener-side / 真机 testing) | LOW (Editor Mouse simulate scope；Sprint 7+ epic 余下 scope 风险中) |
| **ADR-012: Shadow Match Calculation** *(2026-05-14 Sprint 6 emergent fix Track F)* | 光线 + 物件 + 投影墙面 raycast / projection / area overlap algorithm + PerfectMatch threshold ≥0.95 + IPuzzleEvent.OnMatchScoreUpdated/OnPerfectMatch sender contract | **MEDIUM** (Sprint 4 SP-018 ShadowMatchCalculator.cs 实施现状 R2.1 verify 关键 unknown — 影响 story-007 scope ~30 min vs ~4-6 hr) |
| **ADR-013: Object Interaction State Machine** *(2026-05-14 Sprint 6 emergent fix Track F)* | InteractableObject FSM (Idle/Selected/Dragging/Locked) + InteractionCoordinator Inspector 注入 _objects/_interactableLayer/_gameplayCamera + InteractionLockManager singleton + PuzzleConfigProvider 静态注入约定 | LOW (Sprint 2 SP-013 production code done) |
| ADR-030: VS-Late Pattern | Sprint 5-6 VS Build Commitment 第 1 项 — chapter 1 (靠近) end-to-end 实体构建；≥3 playtest sessions for fun loop validation | NONE (process) |
| **ADR-029 V2.0 / V3.0.1**: Story Impl Notes Verification | R1 (skim) + R2 (gap probe) + **R3 (PlayMode framework boundary mandatory)** readiness gate；Asset type 适用 R3 N/A reasoned；**V3.0.1 dp15 candidate "production caller hit > 0 sniff sub-clause" 试点 pilot in story-008** | NONE (process) |

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

## Sprint 5+6 Schedule

- **story-001** (S5-01): ✅ Done 2026-05-09 — chapter scene 实体构建首版 8/8 AC PASS
- **story-001b** (S5-1b): ✅ Done 2026-05-09 — SceneManager boot pipeline 接入 + 5/5 R3 PASS + 22/22 asserts；F4 dev-only stub temporarily in DevTestState（待 story-001c 移除）
- **story-001c** (S5-1c): ✅ Done 2026-05-09 — ADR-009 spec ↔ impl alignment fix；SceneManager.OnRequestSceneChange 内置 DriveTransitionAsync(targetChapterId).Forget() 自闭环 listener；F4 dev-only stub in DevTestState 永久移除；ADR-009 §History 加 1 条 amendment entry；R3 PlayMode 5/5 PASS + 24/24 asserts；S5-02 启动前 cleanup 完成
- **story-002** (S5-02): ✅ Done 2026-05-12 — Chapter 1 end-to-end 5 系统 happy path；2 SP；R3 PlayMode 5/5 case PASS + 36/36 asserts + `all_passed=true` first-run after P5 chapter 0 spec drift fix + reflection 实测 `GameModule.Audio.MusicVolume=0.300` ducking ✅；total 3844ms ≪ 10s budget；evidence doc `production/qa/playmode-end-to-end-flow-2026-05-12.md`；Phase 3 暴露 F4 ISceneEvent chapter 0 spec drift → V3 Type-5 dp6 NEW (累计 6 unique dp 远超 promote 阈值 → Sprint 5 retro 强制 promote)；ADR-030 §VS Build commitment 第 1 项 **100% 完成 ✅**；Sprint 5 Track A 收官
- **story-003** (S6-04): ✅ Done 2026-05-13 evening — Chapter 1 error/restart path narrow scope [A] 0 production code change spike-only；1 SP；Phase 0+1+2+3+4+5 一气呵成；5/5 R3 PASS + 37/37 asserts + `all_passed=true` + `unexpected_error_count=0` + total 1867ms < 8s budget；evidence `production/qa/playmode-error-restart-path-2026-05-13.md`；S5-2b placeholder 2 处 wording drift 通过本 story rewrite 修正 (V3.0.1 NEW dp11 candidate sprint backlog placeholder wording drift closure)；V3.0.1 dp8 candidate 阈值 4 触达 (DevTestState [main-menu] mode count = 4)；NEW dp12 candidate isolated local SceneManager + global GameEvent collateral 首次实战暴露 + closure (P3 第 1 跑 production sm collateral Error → P4/P5 FAIL → Phase 3 spike amend RunP3Async(productionSm) 加 collateral RecoverToIdle cleanup → 第 2 跑 5/5 PASS)；ADR-030 §VS Build commit 第 1 项 chapter 1 robustness 补全 closure；VS Chapter 1 epic 4/4 100% 完成 (本时刻看 — 后 Sprint 6 emergent fix Track F NEW story-004~008 派生)

### Sprint 6 Emergent Fix Track F NEW (2026-05-14 派生 — chapter 1 production wiring 5 处 gap)

派生背景: S6-01 Phase 2.1 manual playtest ⚠️ NEEDS-WORK — Object_01 无法交互 root cause = chapter 1 production wiring 5 处缺口同时存在 (Sprint 2-5 五个 sprint EditMode-green-but-not-wired drift 直接被 manual playtest 5 min 内穿透)。**V3.0.1 dp15 candidate "EditMode green ≠ production wired sniff"** Sprint 6 retro 议题 6 (promote 优先级 高于议题 5 dp14)。

- **story-004** input-pipeline-wiring (✅ **Done 2026-05-14**): Touch/Mouse → SingleFingerFSM → GestureDispatcher.Dispatch production wire；ADR-010 部分 scope pull-forward；**SP 实际 2-3 (~5-5.5 hr 含 readiness + Phase 2 + R3 fix + Phase 4 evidence + Phase 5 closure；governance debt 第 4 次连续 [A] override ≤5 hr hard rule 触发 Sprint 6 retro 议题 5 STOP THE LINE)**；R3 第 2 跑 **5/5 PASS + 27/27 asserts + 0 unexpected error + 391ms ≪ 5s budget**；**V3.0.1 dp15 sniff sub-clause 试点 第 1 个 production caller hit > 0 修复 case PASS** ⭐；V3.0.1 dp16 closure 第 1 个实战触发 (ADR-010 Step 9 NEW)；V3.0.1 dp11 closure 第 1 个实战触发 (sprint-status.yaml SP-013 amend)；V3.0.1 dp17 discussed and declined；evidence `production/qa/playmode-input-pipeline-wiring-2026-05-14.md`；NEXT story-005
- **story-005** chapter-1-scene-wiring (✅ **Done 2026-06-12**): InteractionCoordinator + 2× InteractableObject + child Hitbox2D Drift D [A2]；R3 **6/6 PASS + 27/27 asserts + 252ms**；evidence `production/qa/playmode-chapter-1-scene-wiring-2026-05-15.md`；0 production logic C# change
- **story-006** gameapp-provider-injection (✅ **Done 2026-06-15**): RegisterPuzzleConfigProvider + RegisterInputConfigProvider + BuildFixture helpers；R3 **6/6 PASS + 19/19 asserts + 234ms**；evidence `production/qa/playmode-gameapp-provider-injection-2026-06-12.md`；S0-4 closure；NEXT story-007
- **story-007** shadowmatch-production-wire (📝 Draft 2026-05-14): ADR-012 ShadowMatchCalculator listener 物件 transform → IPuzzleEvent.OnMatchScoreUpdated/OnPerfectMatch production fire；**SP 估时 2-3 (~2-3 hr — scope 风险最高，Sprint 4 SP-018 现状 R2.1 verify 关键 unknown 决定 ~30 min vs ~4-6 hr)**
- **story-008** end-to-end-smoke-replay (📝 Draft 2026-05-14): S5-02 spike 重写 — InputSimulation Mouse Tap+Drag → 自然 production wiring round-trip + V3.0.1 dp15 sniff sub-clause **正式试点 pilot**；**SP 估时 1 (~1 hr)**；试点结果输入 Sprint 6 retro 议题 6 dp15 promote V3.x sub-version 决策

**Track F 总估时**: ~7-9 hr Sprint 6 本周 split 2-3 daily session 嵌入；**block S6-02 playtest 2 + S6-03 + S6-10 gate-check pre-production stage advance**；replay manual playtest after Track F done 才进 S6-02。

### 风险

| 风险 | 缓解 |
|------|------|
| story-001 unity-mcp batch 中途 Bridge disconnect | failFast=true + 单 batch ≤25 commands 拆分；如 Bridge 中断手动重启后续跑（已 mitigated, story-001 ✅） |
| story-001b R3 PlayMode probe 暴露 framework boundary drift | per ADR-029 V2.0 V2-5；如出现 capture 为新 R3 case + 沉淀 problem memo（已 mitigated, S5-1b ✅ + 3 deficiency surfaced：ADR-009 driver gap → story-001c / S5-01 path 错位 ✅ resolved / S5-01 AudioListener residual ✅ resolved by Phase A2）|
| Luban TbChapter post-VS 接入时 fixture provider migration cost | story-001b fixture provider 用 Func\<int, ChapterData\> 同 Luban 真接入签名；migration 仅替换一行 lambda（仍待 post-VS） |
| story-001c async void listener handler 异常逃逸到 Unity log | 与项目内 IInputBlockerEvent / ISettingsEvent / IAudioEvent 现有 listener 模式一致；BeginTransitionAsync 内部已 fail-loud 协议（state=Error + OnSceneLoadFailed）；story-001c DriveTransitionAsync catch 仅兜底；统一 review 留 Sprint 5/6 retro |
| story-002 5 大块 wiring uncertainty (R2.1~R2.5) — chapter 1 listener handler 是否已挂 / prefab InteractableObject / puzzle config injection / UIModule API 路径 | /story-readiness gate 阶段强制 R2 grep 实证；任意 0-hit 走 ADR-029 V2.0 deficiency-flag PASS 路径补 production wiring；R2.5 必待 S5-08 done 才能 verify（Sprint 5 serial 序列保证）|
| story-002 5 系统 cross-system event 顺序 drift (Type-2(c) candidate) | R3 spike P1-P5 顺序 assert + spike Tester listener event log dump；如 drift 累计为 V3 #2 Type-5 候选 dp / V3 #6 Type-6 候选 dp |
| **story-004 ADR-010 部分 scope pull-forward 风险** (Sprint 7+ ADR-010 full epic 余下 scope 多 + chapter 1 fun loop 验证最低 Mouse-only 是否可达) | governance justification = block S6-02/S6-03/S6-10；Sprint 7+ ADR-010 full epic 余下 scope (multi-touch / Pinch / Rotate / InputBlocker listener-side / 真机 testing) 留独立 epic；如 R2.5 verify 揭露本 story narrow scope 不可达 → split 评估 + Sprint 7 buffer |
| **story-007 ShadowMatchCalculator Sprint 4 SP-018 现状 unknown** (R2.1 verify 关键 — 决定 scope ~30 min 仅 verify vs ~4-6 hr 算法实施) | Phase 0 R2.1 优先 verify；如 scope 暴涨 ~4-6 hr → 评估 split (story-007a algorithm impl + story-007b production listener wire) → Sprint 7 buffer 触发 |
| **story-008 dp15 sniff sub-clause 试点 pilot 失败风险** (如 InputSimulation API 时序约束太严苛 / production wiring 完整性校验失败) | 如试点 fail → 回退评估 + sniff sub-clause spec amend；Sprint 6 retro 议题 6 dp15 promote V3.x sub-version 决策 input；governance value 达成 (即使试点 fail 也 surface ADR-029 V3 R3 standard 完备性边界 — 不 promote 但 dp 候选保留至 V4 trigger 评估) |
| **emergent fix Track F 总估时 ~7-9 hr 时间风险** (Sprint 6 截止 2026-05-27 + 余下 S6-02/-03 playtest + S6-09 art + S6-10 gate-check) | story-007 R2.1 verify 后 scope adjust；如 Track F 总投入 >> ~7-9 hr → 部分 stories 推 Sprint 7 buffer；replay manual playtest 必先 done (block S6-02)；Sprint 6 retro 评估 V3.1 trigger + S6-09 推 [A2] 路径释放 capacity |
