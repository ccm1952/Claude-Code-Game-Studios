// 该文件由Cursor 自动生成

# Sprint 5 Retrospective — VS Chapter 1 Build × ADR-030 §VS Build 第 1 项 100% × ADR-029 V3 Type-5 强制 promote × 9 Action Items 沉淀

> **Sprint N**: 5
> **Period**: 2026-05-06 → 2026-05-12（8 自然日实跑 / 计划 14 自然日；6 day 早收）
> **Generated**: 2026-05-12
> **Plan**: [sprint-5.md](./sprint-5.md) — 18 SP commit (Must + Should) / 21 SP candidate
> **Previous Sprint Retrospective**: [sprint-4-retrospective.md](./sprint-4-retrospective.md)
> **Governing ADR**: [ADR-030 Project Workflow](../../docs/architecture/adr-030-project-workflow-vs-late-pattern.md)（§VS Build commitment 第 1 项 100% 完成 ✅）

---

## Executive Summary

Sprint 5 是 **ADR-030 §VS Build commitment 第 1 项（chapter 1 end-to-end VS slice 实体构建首版可玩）100% 完成** 的 sprint —— 9 Must Have stories 全 ✅，chapter 1 happy path 可从 main menu → click `Start Chapter 1` → 11-step scene transition → InteractableObject (Drag/Rotate) → shadow match → puzzle complete → narrative beat → audio duck → next chapter (chapter 2) 真切换。Sprint 5 同时是 **ADR-029 V3 Type-5 'spec/tooling ↔ reality drift' candidate 首次累计满足强制 promote 阈值** 的 sprint（**6 unique dp 跨 3 stories / 4 子系统** — toolchain / UIModule / IShadowEvent / ISceneEvent — 远超 V3 promote ROI 阈值 ≥3 dp）。

**关键工程数据**：

- **9/9 Must Have ✅**（S5-01 自然 split → S5-1b + S5-1c 实际 9 story；S5-02/-03/-04/-05/-06/-08）
- **0/2 Should Have**（S5-07 第 1 次 playtest + S5-09 Settings 决策 backlog；S5-2b Sprint 6 placeholder by design 不计入 closure）
- **0/2 Nice to Have**（S5-10 DOTween.UniTask + S5-11 metric automation backlog）
- **commit rate 82%**（9/11；S5-2b 由计划即为 Sprint 6 placeholder 不计入）
- **ADR-029 V3 dp 收成**: Type-5 **6 unique dp 强制 promote** ⭐ / Type-6 1 dp 留观察 / Type-8 1 dp 留观察 / Type-9 1 dp 留观察 / Type-2(c) 0
- **TODO/FIXME/HACK**: baseline 15 → end 18（**+3 / +20%**；< 30% healthy threshold）
- **ADR-030 §VS Build commitment 第 1 项 100% 完成** ✅
- **Sprint 4 retro 9 action items 闭环率 7/9 = 77.8%**（5 DONE + 2 PARTIAL + 2 Nice deferred）

---

## Metrics

| Metric | Planned | Actual | Delta |
|--------|--------:|-------:|------:|
| Must Have stories | 7 / 14 SP（plan 起草时 + 2026-05-11 [A] serial split 调整后）| **9 / 19 SP** | **+2 stories / +5 SP**（S5-01 自然 split → S5-1b + S5-1c）|
| Should Have stories | 3 / 4 SP | 0 / 0 SP | -3 / -4 SP |
| Nice to Have | 2 / 3 SP | 0 / 0 SP | -2 / -3 SP |
| **Total (Must + Should excl. S5-2b placeholder) commit** | 9 / 17 SP | **9 / 19 SP** | **0 / +2 SP**（**82% overall；100% Must Have**）|
| Carryover 终结 | 3（S5-04 art bible + S5-08 UI + S5-09 Settings）| 2 终结 + 1 carry (S5-09 第 3 次) | **66.7% closure** |
| ADR-029 V3 watch list dp trigger | 0/5（Sprint 4 baseline）| **9 dp accumulated**（Type-5 6 + Type-6/-8/-9 各 1）| **V3 promote 阈值首次达成 trigger** |
| Sprint calendar duration | 14 day | **8 day 实跑** | -6 day 早收 |
| Burst sessions count | — | ~8（S5-01 split phase ~4 sessions + S5-02 happy path Session 27 #1/#2/#3 + S5-08 narrow scope sessions 26 #3-#6 + S5-04 Session 26 #2 + S5-03/-05/-06 batch closure）| reference |
| TODO/FIXME/HACK | baseline 15 | **end 18** | **+3 / +20%**（< 30% threshold；trend healthy）|
| Compile fix incidents | 0 | ~2 std C# (S5-08 Canvas/GraphicRaycaster + S5-02 spike) | non-drift; vendor framework dependency surface |

---

## Velocity Trend

| Sprint | Planned (SP) | Completed (SP) | Stories | Commit Rate | Notes |
|--------|:------------:|:--------------:|:-------:|:-----------:|-------|
| Sprint 1 | 25 | 25 | 13 | 100% | Foundation baseline |
| Sprint 2 | 22 | 22 | 13 | **93%** | Foundation → Core 转折 |
| Sprint 3 | 14 | 8 | 4 | **57%** | governance maturity sprint（V1→V2 升级吞 Should/Nice）|
| Sprint 4 | 15 | 14 | 7 | **87.5%** | balance restoration sprint（Track C 独立估算验证）|
| Sprint 5 (current) | 17 | **19** | **9** | **82%**（Must Have 100%）| **VS chapter 1 build sprint + V3 Type-5 promote unlock** |

**Trend**: **stable + Must Have 全产出**。Sprint 5 commit rate 82% 略低于 Sprint 4 87.5%，但 Must Have **100% 全闭环**，Should/Nice 全 backlog（per ADR-030 §VS Build commitment 第 1 项优先级 + plan §Capacity §17% buffer 显式预留 + Should/Nice 多为 carryover Nice 类目）。Sprint 5 同时是 V3 governance maturity 真正"产出 V3 candidate" 的 sprint（vs Sprint 3-4 V3 watch list catch 信号弱）。

**SP/min ratio 实测**：~30-45 min/SP（vs Sprint 4 burst 14 min/SP，vs sprint-5.md §Capacity 预期 30-60 min/SP hybrid mode）—— **plan vs actual 偏离 < 25%**（vs Sprint 4 burst 28x 系统偏离）。**Sprint 4 retro Process Improvement #4-5 (mode-aware estimation) 落地见效**。

**单 story SP/min 对比**：

| Story | Planned SP | Actual time | SP/min ratio |
|-------|:----------:|:-----------:|:------------:|
| S5-01 chapter 1 Unity scene 实体 (narrow scope after split) | 3 SP | ~90 min | 30 min/SP |
| S5-1b SceneManager boot + fixture provider | 3 SP（split 派生）| ~70 min | 23 min/SP |
| S5-1c ADR-009 listener-path driver alignment | 2 SP（split 派生）| ~60 min | 30 min/SP |
| S5-02 chapter 1 end-to-end happy path | 2 SP | ~180 min（3 sessions：#1 readiness ~45 + #2 readiness re-verify + Phase 1.5 ~60 + #3 Phase 2-5 ~75）| **90 min/SP** ⚠️ over est |
| S5-03 / S5-05 / S5-06 Track B production code each | 2 SP each | ~30-45 min each | 15-22 min/SP |
| S5-04 art bible Draft → Accepted | 1 SP | ~25 min | 25 min/SP |
| S5-08 UIModule Setup narrow scope | 2 SP | ~120 min（4 sessions：#3 narrow ~30 + #4 R2 evidence ~25 + #5 R3 collision ~25 + #6 dev-story ~40）| 60 min/SP |

**Sprint 5 burst-vs-multi-session 混合模式确认**：Track B production code (S5-03/-05/-06) 复用 Sprint 4 framework pattern 走 burst (15-22 min/SP)；wording drift propagate 类工作 (S5-02/-08) 拉至 60-90 min/SP；scene asset + 系统集成 (S5-01/-1b/-1c) 居中 (23-30 min/SP)。

---

## What Went Well

1. **chapter 1 end-to-end happy path 一次跑通 5/5 R3** (after F4 P5 chapter 0 spec drift fix) — S5-02 Phase 3 第 1 跑暴露 `ISceneEvent.OnRequestSceneChange(0)` chapter 0 spec drift (F4) → 用户决策 [A] (chapter 2 placeholder = chapter 1 scene reuse) → Phase 3 第 2 跑 **5/5 PASS + 36/36 asserts + 3844ms ≪ 10s budget**。**M1 dual-layer 全 production reflection 第 2 次实战收益**（S5-1c precedent 复用）：spike P1-P3 production singleton reflection (`GameApp._sceneManager`) + GameModule.Audio.MusicVolume 实测 = 0.300 ducking + 0 unexpected console error/warning。

2. **ADR-029 V3 Type-5 candidate 6 unique dp 强制 promote 阈值首次累计达成** ⭐ — Sprint 5 累计：dp1 (S5-01 toolchain silent failure 2026-05-09) + dp2 (S5-08 ShowUI wording drift 2026-05-11 #4) + dp3 (S5-08 UILayer wording drift 2026-05-11 #5) + dp4 (S5-02 IShadowMatch/Puzzle interface 归属 drift 2026-05-12 #1) + dp5 (S5-02 OnPerfectMatch wording drift 2026-05-12 #2) + dp6 (S5-02 ISceneEvent chapter 0 spec drift 2026-05-12 #3) = **6 unique dp 跨 3 stories / 4 subsystems** 远超 V3 promote ROI 阈值 (≥3 unique dp)。**ADR-029 V3 watch list 监控模式实战收效** — governance maturity 真正"产出" V3 candidate 而不仅是 V2 hot-fix；Sprint 6 AI-1 强制 promote final amend。

3. **VS chapter 1 build chunks 自然分裂 + adaptive scope split 模式第 1 次实战成功** — S5-01 plan 3 SP "chapter 1 scene 实体构建" 在实际跑过程中自然分裂为 3 stories：S5-01（Unity scene asset 构建 + lighting + GameObject hierarchy via unity-mcp batch_execute）+ S5-1b（SceneManager boot pipeline 接入 + fixture ChapterDataProvider）+ S5-1c（ADR-009 production listener-path driver + dev-only stub 移除）。**这是 "按工作边界自然 split 而不是预先 over-design" 模式第 1 次实战** — 每个 sub-story 独立 close + R3 PASS + 互相 unblock 紧凑；同 pattern：S5-02 plan 2 SP 拆 happy path 2 SP + S5-2b error/restart 1 SP Sprint 6 backlog placeholder（plan 起草时 [B] split_2 决策预设）。

4. **Track A pattern 第 5+ 次复用 + V2-5 listener self-removal pattern 5 次实战累计** — Sprint 4 framework expand pattern → Sprint 5 production code pattern 复用：S4-01/-02/-03 framework → S5-03/-05/-06 production code（28/28 R3 PASS first-run 一次跑通；3 stories 各 ~30-45 min）。V2-5 listener self-removal pattern 5 次实战（S5-03/S5-05/S5-06/S5-1b/S5-1c），sync-subscribe race 模式识别成熟（Type-6 candidate 即源于此 pattern 第 4 次实战暴露）。

5. **S5-04 art bible Draft → Accepted 一次过** — art-bible.md V1.0 → V1.1 / 4-dim review PASS + 1 minor inconsistency close（物件库 "书籍/相框" Ch.3-5 → Ch.1-5）；VS art readiness gate 一次 open；解锁 S5-08 dev-story per [A] serial 序列。**Sprint 4 retro action #2 art bible sign-off 闭环成功（第 2 次 carryover 终结；累计 4 sprint cycle 终结）**。

6. **S5-08 UIModule Setup 第 3 次 carryover promote 终结 + V3 Type-8 dp1 NEW** — Sprint 3→4→5 三 sprint carryover；Sprint 5 [D'] narrow scope promote Must Have + 4/4 R3 PASS first-run + 29/29 asserts + V3 Type-8 candidate 'UIWindow second show 行为 vendor 销毁后重建 vs spec 隐藏后重显示' dp1 NEW；ui-system-008 cover full UI infrastructure Sprint 6 polish；ui-system-006 main menu UIWindow Sprint 6 polish。**Sprint 4 retro action #3 hard rule promote/descope 第 2 次 carryover 决策合规** + ADR-011 §G UIWindow 行为 spec amend 候选 Sprint 5 retro 衍生 action item AI-2。

7. **TEngine vendor sync 2026-05-09 (commit c5f8952) + tengine-dev skill migration + wiki-query-agent 删除** — Sprint 5 中段对齐上游 TEngine 6.2.1，sync 6.2.1 P0/P1 fixes + AI workflow 从 wiki-query-agent 迁移到 tengine-dev skill。同 ADR-027 §5 framework knowledge fact lite propagation 模式实战；后续 references 同步走 ff72824 commit cleanup。

8. **mode-aware SP estimation calibration 一次见效** — sprint-5.md §Capacity 预期 ratio 30-60 min/SP，实际 ratio ~30-45 min/SP，**plan vs actual 偏离 < 25%**（vs Sprint 4 burst session ratio 偏离 28x）。Sprint 4 retro Process Improvement #4-5 落地见效 — calendar duration 14 day vs work session duration 8 day 区分清晰，无 plan 起草 vs actual 解读混乱。

9. **dual-layer fixture provider 模式成立** — S5-1b 阶段 `BuildFixtureChapterDataProvider` 用于 spike scene loading；S5-02 阶段 chapter 2 placeholder 扩展同 fixture pattern（chapter 1 scene reuse for transition test）— **MVP simplification mode**：用 fixture 替代 Luban data 让 spike 闭环可控；当 Luban Chapter table ready 再切换。Sprint 6 可复用 chapter 2 build / chapter 1 art asset 升级路径。

---

## What Went Poorly

1. **Should Have 0/2 + Nice 0/2 全 backlog**（Sprint 4 Pattern 重复）— S5-07 第 1 次 playtest + S5-09 Settings 决策 + S5-10 DOTween.UniTask + S5-11 metric automation 全部未启动。Must Have 9/9 ✅ 但 sprint 资源都集中在 Must Have，Should/Nice 没有进入。**第 1 个反模式重复**：Sprint 4 retro 同样 0/2 Nice 又一次 carryover；Sprint 5 同样 0/2 + 0/2。**Sprint 6 plan action**：S5-07 playtest 必须 must-have（ADR-030 §≥3 playtest commitment 硬要求）；S5-09 第 3 次 carryover hard rule 触发（必须 promote Must Have 或 descope→backlog with rationale）；S5-10/-11 评估是否真需要（S5-11 metric automation 当前 manual grep ROI 可接受）。

2. **F4 P5 chapter 0 spec drift 实战暴露 = ADR-029 V3 watch list Type-5 dp6 NEW** — S5-02 Phase 3 第 1 跑 P5 `NextChapterButtonUnloadToIdle` FAIL：MainMenuPanel.OnNextChapterButtonClicked() 派 `ISceneEvent.OnRequestSceneChange(0)`（chapter 0 = unload to Idle 原假设），vendor 实际 `ISceneEvent.cs:24` 注释 `targetChapterId 1-5 valid; 0 not supported`。SceneManager 进 Error state 而非 Idle 解锁状态。修复路径 [A]：chapter 2 placeholder reuse chapter 1 scene + OnRequestSceneChange(2) 走真 chapter→chapter transition。**根因**：MainMenuPanel.cs 起草时未读 `ISceneEvent.cs` 注释 + spec wording drift 假设 chapter 0 = unload 合法；**反映** spec/expectation ↔ reality drift 同源模式（ADR-029 V3 Type-5 candidate 分支 B 扩展：spec ↔ vendor API range drift）。Reactive cost: ~15 min fix + V3 dp6 累计 + retro promote 推进。

3. **Type-5 V3 candidate 累计 6 dp 跨 11 day / 6 stories 才正式 promote** — Type-5 候选实际从 Sprint 5 S5-01（2026-05-09 toolchain silent failure dp1）起累计，到 Sprint 5 末（2026-05-12）6 dp 才达成强制 promote 阈值。**累计 11 day 跨 6 stories（S5-01 / S5-08 / S5-02）→ V3 promote action 滞后**。如果 dp1（S5-01 2026-05-09）即时识别为同源模式 → ADR-029 V3 amend，可能 dp2-dp6 部分实战可避免（ADR-011/SP-002 wording drift propagate cost 累计 ~3 hr 跨 5+ stories amend）。**Sprint 5 retro Process Improvement candidate**：watch list dp ≥2 起立即 V3 candidate 评估 + Sprint mid review 不必等 retro 才 promote（AI-8 衍生）。

4. **wording drift / spec ↔ vendor drift / spec ↔ production code drift 同源高频** — Sprint 5 Type-5 candidate 6 dp 中 5 dp 是 spec/document ↔ reality（vendor / production code / vendor API range）wording drift。ADR-011 / SP-002 / ADR-014 / ADR-016 / ADR-017 / ADR-028 多 ADR wording 与 vendor 实际不一致。**反映** ADR/SP framework time wording 与 production code 演化脱节。Sprint 6 action item：ADR-011 §G + SP-002 systematic wording amend（Type-5 promote 衍生 AI-2）+ ADR-014 ↔ ADR-016 IPuzzleLockEvent contract design alignment（S5-05 framework_drift_surfaced dp5 line 1160）+ ADR-017 §B + ADR-028 §1 AudioModule Initialize/Activate wording 对齐（drift-v2-(a) supersede commit 02d8e7d 已部分修订）。

5. **R2 grep 完备性 meta-drift Type-9 dp1 NEW** — S5-02 /story-readiness Session 27 #2 阶段，R2 grep `IShadowMatchEvent.OnNearMatchEnter` listener 错位为 `OnMatchScoreUpdated`：原 R2 grep 仅按 method 名 `OnNearMatchEnter` 搜，未先 read `IShadowMatchEvent.cs` interface 文件 → 假设 method 归属错位（method 实际位于 `IShadowPuzzleEvent`，且 `IShadowMatchEvent` 仅含 `OnMatchScoreUpdated`）。Sprint 5 retro action item AI-3：ADR-029 V2.0 §V2-2 R2 子条增补 `R2 grep 需先 read 接口文件 → list method 集 → grep 每 method listener 集 → 再判 chain`；预期避免 Type-9 dp2+ 实战重上演。**(planned) sprint_5_retro_action_item entry 已在 sprint-status.yaml line 1474 占位**。

6. **S5-04 art bible Status Draft → Accepted 但 chapter 1 art asset 升级未实施** — art-bible.md V1.1 Accepted 2026-05-11 + chapter 1 scene (S5-01) 仍使用 placeholder asset（text-based / generic shapes / Cube primitives）。**Plan 中已预标 risk 为 Low**（placeholder → 后续升级）但 Sprint 5 期间未实施升级；chapter 1 视觉品质 = MVP 占位水平。**Sprint 6 action item AI-6**：chapter 1 art asset 升级走 art-bible standard（per S5-04 sign-off open 后流程）。**风险**：第 1 次 playtest (S5-07) 美术品质影响玩家 first impression，可能误导 fun loop 反馈。

---

## Blockers Encountered

| Blocker | Duration | Resolution | Prevention |
|---------|:--------:|------------|------------|
| **S5-01 unity-mcp toolchain silent failure (D1-D4 4 类)** | ~30 min（Phase 5 实施 → Phase 6 evidence collection 阶段才暴露 silent fail；修复 batch +2 + lighting bug fix +1 + screenshot 重抓 +2）| toolchain swap unity-bridge → CoplayDev/unity-mcp v9.6.x (commit 6c9b014) + lessons memo `problem_2026-05-09_unity-bridge-to-coplaydev-switch.md` + `problem_2026-05-09_image-preview-confirmation-bias.md` | ADR-029 V3 Type-5 candidate dp1；Sprint 5 retro 强制 promote 后纳入 V3 watch list 自动监控 |
| **S5-08 R2.2 / R2.3 / R2.10 / R2.11 spec ↔ vendor wording drift（4 处）** | ~55 min（R2 evidence collection ~25 min + story-001 amend ~30 min）| story-001 amend 12+ sections 对齐 vendor wording（ShowUI / 7 lifecycle / OnDestroy / UILayer vendor enum + UILayerExtensions helper）| ADR-029 V3 Type-5 dp2 + dp3；Sprint 5 retro promote 后 ADR-011 / SP-002 systematic amend (AI-2) |
| **S5-02 Session 27 #1-#3 三连 wording drift + chapter 0 spec drift** | ~60 min（R2 grep verify ~15 min + 12 sections amend × 3 ~90 min + Phase 3 P5 fail debug + chapter 2 fixture 扩展 ~15 min）| Session 27 #1 R2 wording propagate fix + Session 27 #2 OnPerfectMatch propagate fix + Session 27 #3 chapter 2 placeholder + OnRequestSceneChange(2) | dp4 + dp5 + dp6 三连 V3 Type-5 累计；Sprint 5 retro 强制 promote (AI-1) |
| **S5-1c sync-subscribe race after F4 stub removal** | ~10 min（DevTestState.OnEnter 调用顺序调整 + spike subscribe 时机 Start() → Awake() 上调）| listener self-removal pattern 第 4 次实战（S5-03/S5-05/S5-06/S5-1b/S5-1c）；lessons memo `problem_2026-05-09_sync-subscribe-race.md` | ADR-029 V3 Type-6 candidate 'spike subscribe race after sync-fire listener event' dp1；Sprint 5 retro 评估 promote (AI-7 留观察) |
| **S5-08 R3 P3 vendor UIWindow second show 行为 drift** | ~10 min（spike P3 case Assert 调整）| ADR-029 V3 Type-8 candidate dp1 'UIWindow second show 行为 vendor 销毁后重建 vs spec OnRefresh-only' | Sprint 5 retro 评估 + ADR-011 §G UIWindow 显示/隐藏行为 spec amend 候选 (AI-2 衍生) |
| **S5-02 R2 grep 完备性 meta-drift** | ~20 min（R2 重做 + IShadowMatchEvent vs IShadowPuzzleEvent interface 归属判定 + 5 处 wording propagate fix）| story-002 R2.2 wiring gap 误判撤回 + R2.3 RESOLVED + lessons memo `problem_2026-05-12_r2-grep-completeness-interface-method-set.md` | ADR-029 V3 Type-9 candidate 'R2 grep 完备性' meta-drift dp1；Sprint 5 retro promote V2.0 §V2-2 子条增补 (AI-3) |

**总 blocker 时间**: ~3 hr cumulative drift + amend cost。**Sprint 5 是 "ADR-029 V3 watch list 大丰收 sprint"** — V3 catch 模式实战 catch 多 drift type（Type-5 ×6 + Type-6/-8/-9 各 1 = 9 dp 跨 4 type）；**governance pay-off 显著**（Sprint 3 V2.0 升级 → Sprint 4-5 V3 candidate accumulation → Sprint 5 retro V3 promote）— 这正是 ADR-029 V2.0 §V2 升级时的预设 ROI 实现。

---

## Estimation Accuracy

| Story | Estimated SP | Actual time | Variance | Likely Cause |
|-------|:------------:|:-----------:|:--------:|--------------|
| S5-01 chapter 1 Unity scene 实体构建（narrow scope after split）| 3 SP | ~90 min | -on est（3 SP ≈ 120-180 min budget; ratio 50-66%）| scene asset 构建实际比 framework 工作慢；unity-mcp toolchain silent failure +30 min overhead；Sprint 5 主要 calibration 数据点 |
| S5-1b SceneManager boot + fixture provider | 3 SP（split 派生）| ~70 min | -on est | boot pipeline production code 反射 + fixture pattern 第 1 次实战 |
| S5-1c ADR-009 listener-path driver alignment | 2 SP（split 派生）| ~60 min | -on est | sync-subscribe race fix + dev-only stub 永久移除 + ADR §History amend |
| **S5-02 chapter 1 end-to-end happy path** | 2 SP | ~180 min（3 sessions 累积）| **+50% over est**（2 SP ≈ 60-120 min budget）| 3-session 累积 wording drift propagate fix + P5 chapter 0 spec drift fix；实测 SP/min ratio 90 min/SP；wording drift cost dominant |
| S5-03 / S5-05 / S5-06 Track B production code each | 2 SP each | ~30-45 min each | -on est（2 SP ≈ 60-120 min budget）| Sprint 4 framework pattern 复用；first-run R3 PASS 一次跑过 |
| S5-04 art bible Draft → Accepted | 1 SP | ~25 min | -on est | 4-dim review walk-through 顺畅 + 1 minor inconsistency 单点修订 |
| S5-08 UIModule Setup narrow scope | 2 SP | ~120 min（4 sessions 累积）| -on est | narrow scope 后 4-session 累积；spec ↔ vendor wording drift 多次 amend |

**Overall estimation accuracy**: 8/9 stories 实际投入 ≤ SP budget；**1/9 stories (S5-02) 超 est 50%** 因 3-session wording drift propagate。**System under-estimation 持续** 但 S5-02 暴露 burst session 模式遇 V3 catch trigger 时（wording drift 3 连）累积 cost 高。

**Sprint 6 SP estimation calibration**：

- VS chapter 1 polish + playtest 类工作（S5-07 / S5-08 fallback ui-system-006/-008 / chapter 2 build）保持 30-45 min/SP base
- **V3 catch trigger 类工作**（spec ↔ vendor amend / ADR §History amend）**+30% SP buffer**（per Sprint 5 S5-02 实测；如 dp2+ 起立即 V3 candidate ROI 收益高）
- Playtest 类工作（S5-07）独立 ratio 校准 — 30 min playtest + 30-60 min report writing = ~90 min/SP per 1 session

---

## Carryover Analysis

| Task | Original Sprint | Times Carried | Sprint 5 Status | Sprint 6 Action |
|------|:---------------:|:-------------:|:---------------:|:---------------:|
| Art bible sign-off → S5-04 | Sprint 4 plan + descope | **2** | ✅ DONE | closed |
| UI Module Setup → S5-08 | Sprint 3 Nice → Sprint 4 Nice → Sprint 5 Should → Sprint 5 Must (promote) | **3** | ✅ DONE | closed（full UI infrastructure → ui-system-008/006 Sprint 6）|
| Settings Manager → S5-09 | Sprint 3 Nice → Sprint 4 Nice → Sprint 5 Should backlog | **3** | ⏳ carryover → Sprint 6 hard rule | **必须 Must Have 或 descope→backlog（per Sprint 4 retro hard rule）** |
| Chapter 1 第 1 次 playtest → S5-07 | Sprint 5 plan | **1** | ⏳ carryover → Sprint 6 | **必须 Must Have**（ADR-030 §≥3 playtest sessions 硬要求；Sprint 6 ≥2 sessions 凑齐 ≥3 commit）|
| Chapter 1 error/restart → S5-2b | Sprint 5 plan placeholder | 1（placeholder by design）| ⏳ Sprint 6 Must / Should | Sprint 6 plan 起草时 promote 决定 priority |
| DOTween.UniTask 评估 → S5-10 | Sprint 4 retro action | **1** | ⏳ carryover Sprint 6 Nice | accept Nice carry；如 Sprint 6 narrative effect/audio fade 实际遇 await 需求 → promote |
| metric automation → S5-11 | Sprint 4 retro action | **1** | ⏳ carryover Sprint 6 Nice | accept Nice carry；当前 manual grep 仍可接受 |

**Sprint 5 carryover 终结率 2/3 = 66.7%**（S5-04 + S5-08 closed；S5-09 第 3 次 carryover hard rule 触发）— 与 Sprint 4 60% 同等水平 + 1 项 hard rule 触发（S5-09）。**Sprint 6 必须处理 S5-09 hard rule + S5-07 + S5-2b 三 item**。

---

## Technical Debt Status

- **Current TODO/FIXME/HACK total**: **18 条**（HotFix module GameLogic only）
- **Sprint 4 baseline**: 15 条
- **Trend**: **+3 / +20%**（within healthy < +30% threshold；不触发 tech debt sprint 候选）
- **Distribution**:
  - `LevelEndState.cs`: 4（GameFlow placeholder transitions）
  - `GameplayState.cs`: 5（state machine TODOs）
  - `LevelLoadingState.cs`: 3
  - `GameLobbyState.cs`: 2
  - `GameLoadingState.cs`: 1
  - `PuzzleConfigFromLuban.cs`: 1（Luban integration placeholder）
  - `NarrativeSequenceConfigFromLuban.cs`: 1（同上）
  - `AudioConfigFromLuban.cs`: 1（同上）
- **新增主因**: Luban data integration placeholders（3 TODO；chapter 1 仍用 fixture pattern）+ GameFlow state 转换 placeholder（剩 P5 部分逻辑沿用 fixture）
- **Sprint 6 trend tracking**: 持续监控；如 Luban Chapter data ready 后 3 个 FromLuban TODO 应清除；GameFlow placeholder TODO 留 chapter 2-5 实施时清除

---

## Previous Action Items Follow-Up（from Sprint 4 retro 9 action items）

| Sprint 4 Action Item | Status | Notes |
|----------------------|:------:|-------|
| **AI-1**: VS chapter 1 build 启动 | ✅ DONE | S5-01 split（3 stories: S5-01/S5-1b/S5-1c）+ S5-02 happy path 5/5 R3 PASS；ADR-030 §VS Build commitment 第 1 项 100% 完成 |
| **AI-2**: S4-08 art bible sign-off | ✅ DONE | S5-04 art-bible.md V1.0 → V1.1 Accepted 2026-05-11；4-dim review PASS + 1 minor inconsistency close |
| **AI-3**: S4-09/-10 promote/descope | ⚠️ PARTIAL | S4-09 → S5-08 promote Must Have ✅ DONE；S4-10 → S5-09 Should backlog（**Sprint 5 未决策；第 3 次 carryover → Sprint 6 hard rule**）|
| **AI-4**: DOTween.UniTask 集成包评估 | ⏸ DEFERRED | S5-10 backlog；Sprint 5 narrative/audio 实际未遇 await 阻塞 → 沿用 UniTask.WaitUntil 模式 → Sprint 6 carry |
| **AI-5**: plan 区分 calendar vs work session | ✅ DONE | sprint-5.md §Capacity mode-aware ratio 实施；plan vs actual 偏离 < 25%（大改善 vs Sprint 4 28x）|
| **AI-6**: 每 Should/Nice 预标 if-descope 路径 | ✅ DONE | sprint-5.md §"if descoped" 表实施；Sprint 5 末 S5-07/-09/-10/-11 全 backlog 按表 fallback Sprint 6 |
| **AI-7**: TODO/FIXME/HACK trend tracking | ✅ DONE | baseline 15 → end 18 / +20%（within threshold）|
| **AI-8**: ADR-029 V3 watch list 持续监控 | ✅ DONE (RICH SIGNAL) | Type-5 6 dp 强制 promote / Type-6/-8/-9 各 1 dp 留观察 / Type-2(c) 0；governance maturity 实战收益丰富 |
| **AI-9**: metric baseline setup 入 sprint start checklist | ⏸ DEFERRED | S5-11 backlog；当前 manual grep 仍可接受；Sprint 6 carry Nice |

**Sprint 4 retro action items 闭环率**: **7/9 = 77.8%**（5 DONE + 1 PARTIAL DONE + 1 PARTIAL未决 + 2 Nice deferred）。略低于 Sprint 3 retro → Sprint 4 闭环率（87.5%），但 Must Have 类 action items（1/2/5/6/7/8）全 100% 闭环；deferred 2 项均为 Nice tooling 类。**retro → action 转化 cycle 持续运转**。

---

## Action Items for Sprint 6

| # | Action | Owner | Priority | Deadline |
|---|--------|-------|:--------:|:--------:|
| **AI-1** | **ADR-029 V3 Type-5 'spec/tooling ↔ reality drift' 强制 promote final amend** — 6 unique dp 已远超阈值；起草 ADR-029 V3 §V3-1 Type-5 candidate → official；split (a) toolchain silent failure / (b) spec wording drift / (c) spec API range drift 三分支 or unified；amend ADR-029 §History entry + sprint-status.yaml Type-5 trigger 升级 stable | AI | **High** | Sprint 6 first 3 day |
| **AI-2** | **ADR-011 §G + SP-002 + ADR-014/-016 IPuzzleLockEvent contract + ADR-017 §B + ADR-028 §1 systematic wording amend** — Type-5 promote 衍生；ADR/SP framework time wording 与 production code reality 对齐；预期 ~6 ADR/SP file amend / ~3 hr | AI | **High** | Sprint 6 first 5 day |
| **AI-3** | **ADR-029 V2.0 §V2-2 R2 grep 完备性子条增补** — Type-9 dp1 衍生；增 `R2 grep 需先 read 接口文件 → list method 集 → grep 每 method listener 集 → 再判 chain`；预期避免 Type-9 dp2+ 实战 | AI | **High** | Sprint 6 first 3 day |
| **AI-4** | **S5-07 第 1 次 internal playtest session promote Must Have** — ADR-030 §≥3 playtest sessions commitment 第 1 次硬要求；30 min playtest + 30-60 min report writing；写入 `production/playtests/playtest-vs-chapter-1-2026-05-XX.md`；Sprint 6 also commit ≥2 sessions（per ADR-030 §≥3 total）| AI / 用户 | **High** | Sprint 6 mid（week 1）|
| **AI-5** | **S5-09 Settings Manager 第 3 次 carryover hard rule 决策** — promote Must Have OR descope→backlog with rationale；不允许第 4 次 carry；如 descope 写明"不影响 chapter 1-5 fun loop"理由 | AI | **High** | Sprint 6 plan 起草前 |
| **AI-6** | **chapter 1 art asset 升级 walk through art bible standard** — S5-04 sign-off open 后流程；replace placeholder Cube primitive → 概念美术 asset；与 S5-07 playtest 时机协调（playtest 前升级 vs 之后）| AI / art-director | Medium | Sprint 6 week 2 |
| **AI-7** | **ADR-029 V3 Type-6 / Type-8 / Type-9 candidate 观察期 dp 累计监控** — 各 1 dp 留观察；如 Sprint 6 任一类 dp2+ 触发 → 立即 V3 candidate 评估（不必等 retro）| AI | Medium | Sprint 6 全程 |
| **AI-8** | **watch list dp ≥2 起立即 V3 candidate 评估 + Sprint mid review promote cycle 改造** — Sprint 5 Type-5 dp1（2026-05-09）→ dp6（2026-05-12）累计 11 day 才正式 promote；如 dp1 即识别同源 → dp2-dp6 部分实战可避免；改造 watch list workflow | AI | Medium | Sprint 6 first 5 day |
| **AI-9** | **`/gate-check pre-production` 重跑 + `production/stage.txt`: Pre-Production → Production formal advance** — per ADR-030 §VS Build commitment 第 ≥3 playtest 后；Sprint 6 末 gate-check expected PASS（如 FAIL → 加 Sprint 7 buffer）| AI | **High** | Sprint 6 末（after ≥3 playtest）|

---

## Process Improvements

1. **V3 candidate accumulation cycle 加速** — Sprint 5 Type-5 6 dp 跨 11 day / 6 stories 才 promote；优化为"dp1 即识别同源 + dp2 即 V3 candidate 评估 + 不等 retro 才 promote"。**预期收益**：未来同源 drift type 在 dp2-dp3 即可 V3 amend 阻断累积，避免 ADR/SP wording drift 跨多 story propagate 反复 amend（Sprint 5 实测 5+ stories 累积 amend cost ~3 hr）。AI-8 衍生 process 改造。

2. **Story split adaptive pattern 形式化** — Sprint 5 S5-01 → S5-1b → S5-1c（自然边界 split）；S5-02 → S5-02 happy path + S5-2b error/restart（plan 阶段 [B] split 预设）。新 plan 模板：每 ≥3 SP story 起草时即评估"是否自然分裂成 sub-stories" + "if-then 拆 trigger 条件"。Sprint 5 split 后 R3 PASS first-run 一次跑过率高。**预期 Sprint 6 chapter 2 + S5-2b 实施时复用**。

3. **Carryover hard rule 强制执行 Sprint 5 验证有效 + Sprint 6 触发** — Sprint 4 retro Process Improvement #3（≥3 carry 触发 hard rule）在 Sprint 5 S5-09 第 3 次 carry 时触发；Sprint 6 plan 起草时 S5-09 必须 promote Must Have OR descope→backlog。**模式有效**。

4. **art bible sign-off + chapter art asset 升级独立 action item** — Sprint 4 retro action #2 art bible sign-off Sprint 5 完成（S5-04）；but chapter 1 art asset 升级未在 Sprint 5 实施。**反映** sign-off 是 "gate open" 不是 "asset 升级 trigger"；下个 sprint 还需独立 asset 升级 stories。Sprint 6 plan 必须独立列 chapter 1 art asset 升级 action item（AI-6）。

5. **Spec ↔ reality alignment systematic amend cadence** — Sprint 5 Type-5 promote 衍生 ADR-011 / SP-002 / ADR-014/-016 / ADR-017 / ADR-028 systematic amend。**新 process**：每次 ADR/SP wording amend 时 +1 grep "this ADR ↔ production code wording" 完整 check，避免 amend N 次后下次仍 drift。

---

## Summary

Sprint 5 是 **ADR-030 §VS Build commitment 第 1 项 100% 完成 sprint** + **ADR-029 V3 Type-5 candidate 6 unique dp 强制 promote sprint** — 两个 milestone 一并达成。chapter 1 end-to-end happy path 真切走通（main menu → 11-step scene transition → InteractableObject → puzzle → narrative → audio duck → next chapter）；governance maturity 实战收益丰厚（V3 watch list catch 9 dp 跨 4 type）。Should/Nice 0/4 全 backlog 反映 sprint 容量集中在 Must Have + critical path（per ADR-030 优先级 + plan §Capacity buffer 显式预留）；Sprint 6 必须 absorb S5-07 + S5-09 hard rule + ADR-029 V3 promote final + ADR-011/SP-002 systematic amend 4 大 action items。

**单 sprint 单一最重要改进**：**ADR-029 V3 Type-5 candidate dp1 → dp6 累计 11 day 才正式 promote** — Sprint 6 起改造为 `dp2 即立项 candidate + Sprint mid review promote` cycle，加速 V3 catch + spec/reality alignment governance maturity。

**Sprint 5 final score**: ✅ **PASS — ADR-030 §VS Build 第 1 项 100% + V3 governance maturity 显著沉淀**（Must Have 100% / commit rate 82% / chapter 1 happy path 可玩 / V3 Type-5 强制 promote unlock / 9 action items 衍生 Sprint 6）。

---

## Next Steps

- `/sprint-plan sprint-6` 起草 Sprint 6 plan：包括 (1) S5-07 ≥3 playtest 第 1 次 promote Must Have + Sprint 6 ≥2 sessions = ADR-030 §≥3 commit 全闭环；(2) S5-09 hard rule 决策；(3) ADR-029 V3 Type-5 promote final amend (AI-1)；(4) ADR-011/SP-002/ADR-014-016/ADR-017/ADR-028 systematic wording amend (AI-2)；(5) chapter 2 build OR chapter 1 art asset 升级 OR error/restart S5-2b 实施 路径决策；(6) `/gate-check pre-production` 重跑 + `production/stage.txt` 升级 (AI-9)
- ADR-029 V3 watch list Sprint 6 起继续监控 Type-6 / Type-8 / Type-9 dp 累积；如 dp2+ 触发立即 V3 candidate 评估（不等 retro；AI-8）
- TODO/FIXME/HACK Sprint 6 末 retro 再次评估；当前 baseline 18 healthy（< 30% threshold）
- art bible sign-off 已开 gate；Sprint 6 walk chapter 1 art asset upgrade（AI-6）
