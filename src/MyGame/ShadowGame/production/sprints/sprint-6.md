// 该文件由Cursor 自动生成

# Sprint 6 — VS Validation × ≥3 Playtest Sessions × ADR Governance Amend × Stage Advance Final

> **Sprint N**: 6
> **Phase**: Pre-Production / VS Build（Sprint 5-6 双 sprint VS playable commitment per ADR-030 — **Sprint 6 final sprint**）
> **Start**: 2026-05-13（Session 29+）
> **End (expected)**: 2026-05-27（14 自然日）
> **Mode**: solo（沿 Sprint 3-5 模式；Sprint 7 起 stage advance Production 后再次评估升 lean / full 必要）
> **Previous Sprint**: [sprint-5.md](./sprint-5.md)（**82%** 9/11；Must Have 9/9 ✅；Should Have 0/2 + S5-2b placeholder by design；Nice 0/2 carryover）
> **Retrospective**: [sprint-5-retrospective.md](./sprint-5-retrospective.md)
> **Governing ADR**: [ADR-030 Project Workflow](../../docs/architecture/adr-030-project-workflow-vs-late-pattern.md)（**Sprint 5-6 双 sprint VS commitment Sprint 6 final**）

---

## Sprint Goal

**Sprint 6 是 ADR-030 §VS Build commitment Sprint 5-6 双 sprint 最终 sprint** — chapter 1 经 ≥3 internal playtest 验证 + Type-5 V3.0 promote 衍生 systematic wording amend（AI-2）+ R2 grep 完备性（AI-3）+ UI polish 完整覆盖（ui-006 main menu + ui-008 popup/inputblocker）+ chapter 1 error/restart path 补全 + chapter 1 art asset 升级 art-bible standard + `/gate-check pre-production` PASS + `production/stage.txt`: Pre-Production → **Production** formal advance。Sprint 7+ 起 vs-chapter-2 epic（chapter 2 build first slice）独立 sprint。

**Sprint 6 核心 commitment（来自 Sprint 5 retro AI 1-9 + 用户 priority [A]×6 决策）**：
1. **chapter 1 playtest ≥3 sessions** + playtest report（ADR-030 §VS Build commit 第 1 commit 项 final 收尾；S5-07 deferred）
2. **chapter 1 error/restart path**（S5-2b placeholder Sprint 5 split_2 派生；VS validation 必需）
3. **ADR-011/SP-002/ADR-014/-016/-017/-028 systematic wording amend**（AI-2 = ADR-029 V3.0 §V3-1.b 修复模式实战 propagation）
4. **ADR-029 V2.0 §V2-2 R2 grep 完备性子条增补**（AI-3 = V3.0 §V3-1.b/.c enforcement layer 精化）
5. **ui-system-006 main menu UIWindow polish + ui-system-008 popup/inputblocker robust**（S5-08 narrow scope 后 fallback Must Have）
6. **chapter 1 art asset 升级**（AI-6 Should Have；playtest 后嵌入 week 2-3 per [A] 决策）
7. **`/gate-check pre-production` 重跑 + `stage.txt` 升级**（AI-9；Sprint 6 末；如 FAIL → Sprint 7 buffer per ADR-030 fallback）

---

## Capacity & Estimation Model（Mode-aware Calibration per Sprint 5 retro Process Improvement）

> Sprint 5 SP/min ratio 实测 = ~30-45 min/SP（plan vs actual 偏离 < 25%；mode-aware calibration 见效 per Sprint 4 retro action #5）。Sprint 6 继续 hybrid mode 模式：playtest 类 ~90 min/SP（per Sprint 5 S5-02 实测）+ ADR amend 类 ~30-45 min/SP + ui polish ~45 min/SP + art asset ~45-60 min/SP。

| 指标 | 数值 |
|------|------|
| Calendar Duration | 14 自然日（2026-05-13 → 2026-05-27）|
| Expected Work Session Duration | 6-8 burst sessions × ~2-3 hr/session ≈ ~15-20 hr |
| Burst Capability (single session) | ~15-20 SP burst（Sprint 4 实测 ≈14 min/SP）；but Sprint 6 大部分非 burst-friendly 工作 |
| Sprint 6 承诺（Must + Should）| **10 stories / 19 SP** |
| Nice to Have 延伸 | 2 stories / 3 SP |
| 总候选 | 12 stories / 22 SP |
| Buffer | ~17%（含 playtest 反馈引入 mid-sprint adjustment + V3 catch trigger 30% buffer per Sprint 5 retro AI-1 校准）|

**Mode 标注**：
- **Track A (VS Validation Playtest)**: Hybrid mode (~90 min/SP per Sprint 5 S5-02 calibration；含 manual Editor operations)
- **Track B (ADR Governance Amend)**: AI burst mode (~30-45 min/SP)
- **Track C (UI Polish + Error Path)**: Multi-day work mode (~45 min/SP)
- **Track D (Art Asset Upgrade)**: Hybrid mode (~45-60 min/SP; art-director 协作)
- **Track E (Stage Advance Final)**: AI burst mode (~30 min/SP)

---

## Tracks Architecture（5 轨 evolved per Sprint 5 retro Process Improvement）

### Track A — VS Validation（Critical Path; ADR-030 §VS Build commit Sprint 6 final）

chapter 1 经 3 次 internal playtest 完整验证 → playtest report 综合 → gate-check pre-production → stage.txt advance Pre-Production → Production。**Sprint 6 sprint 灵魂**。

### Track B — ADR Governance Amend（Sprint 5 Type-5 V3.0 promote 后续 propagation）

AI-2 systematic wording amend（ADR-011 §G + SP-002 + ADR-014/-016 + ADR-017 §B + ADR-028 §1）+ AI-3 R2 grep 完备性子条增补 + AI-7 V3 watch list 监控 + AI-8 watch list dp ≥2 立即 candidate cycle 改造。**Sprint 6 governance maturity sprint**。

### Track C — UI Polish + Error Path Completion

ui-system-006 main menu UIWindow polish + ui-system-008 popup queue + inputblocker robust + S5-2b chapter 1 error/restart path。**Sprint 6 chapter 1 robustness sprint**。

### Track D — Art Asset Upgrade（per AI-6 medium priority）

chapter 1 art asset 升级 art-bible standard — **playtest 后升级**（per [A] AI-6 决策；playtest 1 → emergent fixes + art asset 升级 → playtest 2）。

### Track E — Stage Advance Final

`/gate-check pre-production` 重跑 + `production/stage.txt` advance Pre-Production → Production。**Sprint 6 收官项**。

---

## Tasks

### Must Have (Critical Path) — 9 items / 16 SP

#### Track A — VS Validation（Sprint 6 critical path 序列）

| ID | Story | Type | Complexity | Depends on | AC 要点 |
|----|-------|:----:|:----------:|------------|---------|
| **S6-01** | **chapter 1 第 1 次 internal playtest session（S5-07a）** | Playtest / QA | **2 SP** | S5-02 ✅ + S6-04 error/restart + S6-07/-08 ui polish | playtest session ≥30 min；reuse session 从 splash → main menu → chapter 1 完整走通；记录 player journey 反馈（fun loop / pacing / clarity / frustration points）；写入 `production/playtests/playtest-vs-chapter-1-session-1-2026-05-XX.md`（per ADR-030 §VS Build commit 第 1/3 次）；R3 PlayMode probe N/A（manual playtest）|
| **S6-02** | **chapter 1 第 2 次 playtest session（S5-07b）** | Playtest / QA | **2 SP** | S6-01 ✅ + emergent fixes from S6-01 + S6-09 art asset 升级（Should）| 同 S6-01 流程；重点验证 playtest 1 emergent fixes 修复效果 + art asset 升级（如 S6-09 已 done）影响；写入 `production/playtests/playtest-vs-chapter-1-session-2-2026-05-XX.md` |
| **S6-03** | **chapter 1 第 3 次 playtest session + playtest report 综合（S5-07c）** | Playtest / QA | **2 SP** | S6-02 ✅ + emergent fixes from S6-02 | 同 S6-01 流程 + 综合 playtest report 写入 `production/playtests/playtest-report-vs-chapter-1-2026-05-XX.md`：含 3 sessions 关键反馈 / fun loop assessment / 建议 art polish / 建议 narrative tweaks / Sprint 7+ recommendations |

#### Track B — ADR Governance Amend（Sprint 5 Type-5 V3.0 promote 后续 propagation）

| ID | Story | Type | Complexity | Depends on | AC 要点 |
|----|-------|:----:|:----------:|------------|---------|
| **S6-05** | **ADR-011 §G + SP-002 + ADR-014/-016/-017/-028 systematic wording amend（AI-2）** | ADR / Governance | **3 SP** | ADR-029 V3.0 ✅（commit 40a426d 2026-05-12）| 6 ADR/SP file wording amend 对齐 vendor 实际 wording / lifecycle / interface 归属 / API range（per ADR-029 V3.0 §V3-1.b 修复模式）：(1) ADR-011 §G UIWindow lifecycle spec amend (vendor ShowUI + 7 lifecycle method + OnDestroy + UILayer enum)；(2) SP-002 wording amend（同 ADR-011 §G）；(3) ADR-014 ↔ ADR-016 IPuzzleLockEvent contract design alignment（Sprint 5 S5-05 framework_drift_surfaced dp5 line 1160）；(4) ADR-017 §B + ADR-028 §1 AudioModule Initialize/Activate wording 对齐（drift-v2-(a) supersede commit 02d8e7d 已部分修订；本 sprint 完整 close）；每 file amend 独立 R2 grep verify + commit；commit message 引 V3.0 §V3-1.b propagation；预期 ~3 hr / 6 file amend |
| **S6-06** | **ADR-029 V2.0 §V2-2 R2 grep 完备性子条增补（AI-3）** | ADR / Governance | **1 SP** | ADR-029 V3.0 ✅ | §V2-2 R2 子条增补 `R2 grep 需先 read 接口文件 → list method 集 → grep 每 method listener 集 → 再判 chain`（V3.0 §V3-1.b/.c enforcement layer 精化）；不升 V3.1 stay V3.0（minor doc amend per V3.0 minor revision pattern）；§History +1 entry；预期 ~30-45 min |

#### Track C — UI Polish + Error Path Completion

| ID | Story | Type | Complexity | Depends on | AC 要点 |
|----|-------|:----:|:----------:|------------|---------|
| **S6-07** | **ui-system-006 main menu UIWindow polish** | UI / Integration | **2 SP** | S5-08 ✅ narrow scope + ADR-011 §G amend (S6-05) | main menu UIWindow lifecycle 完整（OnInit/OnRefresh/OnUpdate/OnAdded/OnRemoved/OnPause/OnDestroy 7 hook）+ 4 button group（New Game / Continue / Settings placeholder / Quit）+ fade-in animation + BGM start hook；replace S5-02 2 inline button base；R3 PlayMode probe 5 case（lifecycle order / 4 button click path / fade-in animation）|
| **S6-08** | **ui-system-008 popup queue + inputblocker robust** | UI / Integration | **2 SP** | S6-07 ✅ + ADR-011 §G amend | Popup Queue Manager + Auto-Dequeue + Auto InputBlocker + Overlay limit + 双 InputBlocker 叠加；9 AC + 5 R3 PlayMode probe TC（per ui-system-008 story-008 framework）；M1 dual-layer 全 production reflection |
| **S6-04** | **chapter 1 error/restart path（S5-2b）** | Integration | **1 SP** | S5-02 ✅ + S6-07 ui-006 | 4-5 R3 PlayMode probe case：(1) unknown chapter Button click（chapter id 99 / -1 / null）→ Idle 解锁（不进 Error）；(2) mid-transition cancel（chapter id 1 transition 中点 Quit 按钮）→ unload 干净；(3) asset load fail（fixture chapter id 6 path 不存在）→ Error 状态 + restart 路径；(4) restart-from-Error state recovery（Error 后点 main menu 回 splash）→ state machine reset 干净；(5) repeated rapid OnRequestSceneChange（race condition）→ debounce 验证；写 `production/epics/vs-chapter-1/story-003-error-restart-path.md` |

#### Track E — Stage Advance Final

| ID | Story | Type | Complexity | Depends on | AC 要点 |
|----|-------|:----:|:----------:|------------|---------|
| **S6-10** | **`/gate-check pre-production` 重跑 + `stage.txt` 升级（AI-9）** | Governance / Gate | **1 SP** | S6-03 playtest report ✅ + ADR-030 §VS Build commit 全 6 项 done | `/gate-check pre-production` expected PASS（含 6 项 commit check：chapter 1 build ✅ + Track A production code ✅ + ≥3 playtest ✅ + playtest report ✅ + gate-check rerun ✅ + stage advance ✅）；`production/stage.txt`: Pre-Production → **Production**；如 FAIL → 加 Sprint 7 buffer + retro 评估 sprint 6 真正 gap（per ADR-030 §VS Build commit fallback）|

**Must Have 总计**: 9 stories / 16 SP（Track A: S6-01/-02/-03 = 6 SP / Track B: S6-05/-06 = 4 SP / Track C: S6-07/-08/-04 = 5 SP / Track E: S6-10 = 1 SP）

### Should Have — 1 item / 3 SP + 1 decision

| ID | Story | Type | Complexity | Depends on | 说明 |
|----|-------|:----:|:----------:|------------|------|
| **S6-09** | **chapter 1 art asset 升级 walk through art-bible standard（AI-6）** | Art / Integration | **3 SP** | S5-04 ✅ + art-director 协作 | replace chapter 1 scene placeholder Cube primitive → 概念美术 asset（≥1 投影墙面 + ≥1 可操作物件 + ≥1 narrative trigger zone）；per art-bible.md V1.1 standard；**与 playtest 时机协调 per [A] 决策**：S6-01 playtest 1 后 + S6-09 升级 + S6-02 playtest 2 嵌入；如 art-director 协作慢 → S6-09 fallback Sprint 7 |
| **AI-5** | **S5-09 Settings Manager descope→backlog decision（per [A] hard rule 触发）** | Carryover / Decision | **0 SP** (decision-only) | Sprint 5 retro AI-5 + hard rule 触发（第 3 次 carry → Sprint 6 hard rule） | per [A] 用户决策：descope→backlog；rationale: 不影响 chapter 1-5 fun loop；Settings UI 独立产品所需 polish phase 再实施；sprint-status.yaml S5-09 status: backlog (descoped via hard rule)；Sprint 6 plan §Carryover 处理表记入；Sprint 6 retro §Closed 记入 |

**Should Have 总计**: 1 stories / 3 SP（+ 1 decision-only 0 SP）

### Nice to Have — 2 items / 3 SP

| ID | Story | Type | Complexity | Depends on | 说明 |
|----|-------|:----:|:----------:|------------|------|
| **S6-11** | **DOTween.UniTask 集成包评估 + 引入决策（S5-10 carry）** | Tooling / Decision | **1 SP** | Sprint 4 retro action #4 + Sprint 5 retro AI-4 carryover Nice | 评估 Sprint 6 narrative effect / audio fade / ui animation 是否需要精确 await 语义；如需要走 引入流程；如不需要保持 UniTask.WaitUntil 模式；写 ADR-031 候选（如决策非 trivial）|
| **S6-12** | **Sprint metric baseline setup + V3 watch list 自动化（S5-11 carry）** | Governance / Tooling | **2 SP** | Sprint 4 retro action #9 + Sprint 5 retro AI-9 carryover Nice | sprint start checklist 落地：sprint plan phase 0 添加 "metric baseline grep" step；TODO/FIXME/HACK + ADR Status counts + Phase 1.5 success rate 三 metric 自动 measure；写入 `production/sprint-status.yaml::sprint_metric_snapshot` 字段 |

**Nice to Have 总计**: 2 stories / 3 SP

---

## Carryover from Sprint 5

| Task | Reason | Times Carried | Sprint 6 Status | New Estimate |
|------|--------|:-------------:|:---------------:|:------------:|
| chapter 1 ≥3 playtest sessions → S6-01/-02/-03 | S5-07 Sprint 5 0/2 Should backlog（ADR-030 §≥3 playtest commit 硬要求）| **1** | Must Have ×3 sessions | 6 SP (2×3) |
| chapter 1 error/restart → S6-04 (S5-2b) | Sprint 5 [B] split_2 placeholder by design | **1** | Must Have | 1 SP |
| Settings Manager → backlog | S5-09 第 3 次 carryover hard rule trigger（Sprint 3 Nice → Sprint 4 Nice → Sprint 5 Should backlog → Sprint 6 hard rule per [A] decision）| **3** | **descope→backlog**（AI-5 决策 [A]；hard rule satisfied）| 0 SP (decision-only) |
| DOTween.UniTask → S6-11 | S5-10 Sprint 5 0/2 Nice backlog | **2** | Nice continue carry | 1 SP |
| metric automation → S6-12 | S5-11 Sprint 5 0/2 Nice backlog | **2** | Nice continue carry | 2 SP |

**Sprint 6 carryover 处理**：3 carryover stories（playtest ×3 + error/restart + Settings hard rule close）+ 2 carryover Nice（DOTween / metric）；S5-09 hard rule 一次性 close 不持续 carry。**Sprint 5 retro AI-5 hard rule 模式实战收效 — 4 sprint cycle 终结 Settings carryover 累积**。

---

## Sprint 6 Action Items 实现矩阵（Sprint 5 retro 9 项映射）

| Sprint 5 Action Item | Sprint 6 实现路径 | Story / Track |
|----------------------|---------------------|---------------|
| **AI-1**: ADR-029 V3 Type-5 promote final amend | ✅ **DONE EARLY**（commit 40a426d 2026-05-12 evening；retro 内完成，先于 Sprint 6 plan）| (closed early in retro 内) |
| **AI-2**: ADR-011/SP-002/ADR-014/-016/-017/-028 systematic wording amend | ✅ S6-05 Must Have | Track B |
| **AI-3**: ADR-029 §V2-2 R2 grep 完备性子条增补 | ✅ S6-06 Must Have | Track B |
| **AI-4**: S5-07 第 1 次 playtest promote Must Have | ✅ S6-01/-02/-03 Must Have ×3 | Track A |
| **AI-5**: S5-09 Settings hard rule 决策 | ✅ descope→backlog decision-only（per [A]）| Carryover |
| **AI-6**: chapter 1 art asset 升级 walk art-bible | ✅ S6-09 Should Have（per [A] playtest 后再升级）| Track D |
| **AI-7**: V3 watch list Type-6/-8/-9 监控 | ✅ 嵌入 dev-story 起手 + sprint 末 retro 监控 + dp ≥2 立即 V3 candidate（per AI-8）| (process-only) |
| **AI-8**: watch list dp ≥2 立即 candidate cycle 改造 | ✅ 嵌入 sprint mid review checklist + V3 catch trigger（同 sprint amend pattern 如 Sprint 6 触发 → 类 AI-1 same-sprint pattern）| (process-only) |
| **AI-9**: `/gate-check pre-production` 重跑 + stage.txt 升级 | ✅ S6-10 Must Have | Track E |

**8/9 AI 直接对应 stories；2/9 AI 嵌入 sprint workflow process-only；1/9 AI（AI-1）已 DONE EARLY 先于 plan**。**retro → action 转化 cycle 持续运转**。

### "if descoped, where does it go" 路径预标（per Sprint 4 retro action #6 + Sprint 5 实施有效）

| Story | If descoped | Sprint 7 fallback |
|-------|-------------|:-----------------:|
| S6-01/-02/-03 playtest ×3 | **不可 descope**（ADR-030 §VS Build commit final 项；如 Sprint 6 时间不够 → 加 Sprint 7 buffer 或缩小 playtest scope per session）| n/a |
| S6-04 error/restart | **不可 descope**（Sprint 5 placeholder by design 已 1 sprint 延期；VS validation 必需）| n/a |
| S6-05 ADR systematic wording amend | descope 接受 → split Sprint 7（V3.0 propagation 不影响 playtest；governance debt 容延后）| Sprint 7 |
| S6-06 R2 grep 完备性增补 | descope 接受 → Sprint 7 hot-fix during dev-story | Sprint 7 |
| S6-07 ui-006 main menu polish | descope 接受 → playtest 用 S5-08 narrow scope inline button base | backlog |
| S6-08 ui-008 popup/inputblocker | descope 接受 → playtest 不依赖 popup queue robust | backlog |
| S6-10 gate-check + stage advance | **不可 descope**（ADR-030 §VS Build commit final 项；如 FAIL → Sprint 7 buffer）| n/a |
| S6-09 art asset 升级（Should）| descope 接受 → playtest 仍用 placeholder（per Sprint 5 retro What Went Poorly #6 风险已 acknowledged）| Sprint 7 |
| S6-11 DOTween.UniTask（Nice）| descope 接受 → 沿用 UniTask.WaitUntil 模式 | Sprint 7+ |
| S6-12 metric automation（Nice）| descope 接受 → manual grep + sprint start checklist 手动即可 | Sprint 7+ |

---

## Critical Path

```
Sprint 6 dependency chain（按 [A]×6 priority decisions）:

  Track B (并行 first 3-5 day):
    S6-05 ADR systematic wording amend (3 SP)
    S6-06 R2 grep 完备性增补 (1 SP)
       
  Track C (并行 first week):
    S6-07 ui-006 main menu polish (2 SP)
       ↓
    S6-08 ui-008 popup/inputblocker (2 SP)
    S6-04 error/restart (1 SP)
       ↓
  Track A — VS Validation 序列 (sprint mid → end):
    S6-01 playtest 1 (2 SP)
       ↓
    [emergent fixes + S6-09 art asset upgrade Should]
       ↓
    S6-02 playtest 2 (2 SP)
       ↓
    [emergent fixes]
       ↓
    S6-03 playtest 3 + playtest report (2 SP)
       ↓
  Track E final:
    S6-10 gate-check pre-production + stage.txt advance (1 SP)

最长依赖链: S6-07 → S6-08/-04 → S6-01 → S6-02 → S6-03 → S6-10 = 6 stories / 10 SP
   (≈ critical path 单线工作日 ~6-8 sessions × 2-3 hr ≈ ~15-20 hr)

并行轨:
  Track B (S6-05/-06) 与 Track C/A 并行（governance vs VS validation 不冲突）
  Track D S6-09 (art asset) 嵌入 playtest 1 ↔ playtest 2 之间
  Nice (S6-11/-12) 全 Sprint 弹性 fit
```

**Sprint 6 critical path 注意点**：
1. **S6-05 ADR amend 与 S6-07 ui-006 polish 依赖关系**：S6-07 polish 主要参考 ADR-011 §G amend 后的 vendor 实际 lifecycle wording；如 S6-05 ADR-011 §G amend 先 done → S6-07 起手即依据 amended ADR；如并行 → S6-07 起手用 S5-08 已知 vendor wording（无 ADR amend dependency hard block）
2. **S6-01 playtest 1 起手条件**：S6-04 error/restart + S6-07 ui-006 + S6-08 ui-008 三 Must Have 完成 → chapter 1 robustness 完整状态可 playtest；如 S6-08 popup robust 未完 → playtest 1 不依赖 popup queue（不 block）
3. **S6-09 art asset 与 playtest 时机协调（per [A]）**：playtest 1 → emergent fixes 评估 → 决定 S6-09 是否 playtest 2 前升级（如反馈表明 art 是 player journey 阻碍 → 必须升级；如反馈集中 fun loop → S6-09 可延后 playtest 3 后或 Sprint 7）
4. **S6-10 gate-check 是 Sprint 6 收尾项**：必须在 S6-03 playtest report 完成后；如 gate-check FAIL → Sprint 7 buffer 处理 emergent items（per ADR-030 fallback）

---

## Risks

| Risk | Probability | Impact | Mitigation |
|------|:-----------:|:------:|------------|
| **chapter 1 playtest 反馈 fun loop 重大调整** | Medium | High（gate-check FAIL → Sprint 7 buffer）| Sprint 5 末 playtest 0/2 是 Sprint 6 critical risk；playtest 1 即识别问题 → playtest 2 试改 → playtest 3 二次验证；Sprint 6 mid review 评估 fun loop signal；如 playtest 1-2 即识别 fun loop bug → Sprint 6 加 emergent fix sub-stories |
| **AI-2 systematic wording amend 估高扩展（6 file × 30 min 估 3 hr 但实际可能扩展）** | Medium | Medium | 起 sub-tracking：每 file amend 独立 R2 grep verify + commit；如 ADR-014/-016 IPuzzleLockEvent design alignment 需 ADR amend ≥ 1 hr → Sprint mid review 切分 Sprint 7 部分；ADR-029 V3.0 §V3-1.b 修复模式 ROI tracking |
| **Art asset 升级 与 playtest 时机协调（per AI-6 [A] playtest 后升级）** | Medium | Medium | playtest 1 后 emergent fixes 评估 + S6-09 art asset 升级决策 + playtest 2 嵌入；如 art-director 协作慢 → S6-09 fallback Sprint 7 + playtest 2/3 仍用 placeholder（chapter 1 art quality 不阻 fun loop validation）|
| **`/gate-check pre-production` FAIL（fun loop 不达标 / VS scope 不完整）** | Low | High（stage 升级延后 + Sprint 7 plan 大改）| ADR-030 §VS Build commit 接受 → Sprint 7 buffer；retro 评估 sprint 6 真正 gap；Sprint 5 chapter 1 happy path 5/5 R3 PASS + 0 console error 已是 baseline robust signal |
| **Sprint 5 ratio calibration 持续：playtest 类 ~90 min/SP（S5-02 实测）与 estimation align** | Low | Low | 本 plan §Capacity 已用 90 min/SP for playtest；ADR amend 类 30-45 min/SP；ui polish 类 ~45 min/SP；与 Sprint 5 actual ratio 一致 |
| **ADR-029 V3 watch list Type-6/-8/-9 dp 累积中途又触发新 V3.1 amend** | Medium | Medium | per AI-8 改造：dp ≥2 立即 V3 candidate 评估；不等 retro；如 Sprint 6 触发 → Sprint 6 mid V3.1 amend（类 AI-1 same-sprint pattern；commit 40a426d 已立 precedent）；Sprint 6 不再积累至 retro 才 promote |
| **Sprint 6 capacity 19 SP commit 接近 Sprint 5 actual 19 SP 实测**| Medium | Medium | 与 Sprint 5 trend 一致；如 mid-sprint 实际 ratio 偏离 > 25% → Should Have S6-09 移 Sprint 7 + 调整 commit 17 SP；buffer 17% 内有伸缩余地 |

---

## Dependencies on External Factors

- **art-director subagent / art-bible 协作**（S6-09）— playtest 1-2 之间嵌入；如 art-director 协作慢 → S6-09 fallback Sprint 7
- **Unity Editor manual operations**（S6-01/-02/-03 playtest sessions + S6-07/-08 UI manual verify）— 每 session 在 Unity Editor 内实际玩 ≥30 min；manual operations 拉低 burst session SP/min ratio（playtest hybrid mode ~90 min/SP per Sprint 5 calibration）
- **vendor TEngine 6.2.1**（Sprint 5 sync 2026-05-09 commit c5f8952）— 当前已 stable；如 vendor patch 升级 → V3.0 §V3-1.b 修复模式 R2 verify 模式有效拦截

---

## Definition of Done for Sprint 6

- [ ] Must Have 9/9 完成（S6-01..08/-10）— playtest ×3 + ADR amend + ui polish + error/restart + gate-check + stage advance
- [ ] chapter 1 经 ≥3 internal playtest sessions（累计 Sprint 5+6 = 3 sessions per ADR-030 §VS Build commit）
- [ ] playtest report 综合写入 `production/playtests/playtest-report-vs-chapter-1-2026-05-XX.md`
- [ ] `/gate-check pre-production` PASS（如 FAIL → Sprint 7 buffer + retro 评估）
- [ ] `production/stage.txt`: Pre-Production → **Production** formal advance
- [ ] ADR-011 §G + SP-002 + ADR-014/-016/-017/-028 systematic wording amend 完毕（6 file，per V3.0 §V3-1.b 修复模式）
- [ ] ADR-029 V2.0 §V2-2 R2 grep 完备性子条增补（stay V3.0；V3.0 §V3-1.b/.c enforcement layer 精化）
- [ ] ui-system-006 main menu UIWindow polish + ui-system-008 popup/inputblocker robust 全 R3 PlayMode probe PASS
- [ ] chapter 1 error/restart 4-5 R3 case PASS（S6-04 story-003-error-restart-path.md 完成）
- [ ] Should Have S6-09 art asset 升级 完成 OR descope rationale（per playtest 反馈）
- [ ] AI-5 S5-09 descope→backlog decision 入 sprint-status.yaml（hard rule satisfied）
- [ ] QA plan exists（`production/qa/qa-plan-sprint-6-2026-05-XX.md`）
- [ ] Sprint 6 retrospective 写入
- [ ] sprint-status.yaml metric snapshot 更新（如 S6-12 完成则自动；否则手动）
- [ ] ADR-029 V3 watch list Sprint 6 末评估：Type-6/-8/-9 dp 累积情况；如 dp ≥2 立即 V3.1 amend（per AI-8）
- [ ] No S1 or S2 bugs in chapter 1 fun loop（post playtest 3 fixes）

---

## Sprint 6 主题候选 → Sprint 7 衔接

完成 Sprint 6 后，Sprint 7 主目标是 **Production stage 起步 chapter 2 build**（假设 Sprint 6 stage advance ✅）：
- **vs-chapter-2 epic 启动**（chapter 2 first slice build per ADR-030 §VS Build commit chapter 2-5；reuse Sprint 5 chapter 1 build pattern: scene asset + SceneManager dispatch + 5 系统 production code）
- **chapter 1 polish backlog 清除**（剩余 art asset 升级 / Settings backlog 重评估 / S6 emergent items）
- **ADR governance maturity 继续**：Type-6/-8/-9 V3 watch list dp 累积监控；ADR-029 V4 升级条件 5 项观察（per V3.0 §V3-5）
- **stage advance Production 后**：QA process 进 polish phase；可启 lean review mode（per Sprint 6 plan §Mode 重评估）

**风险接受 path**：如 Sprint 6 末 gate-check FAIL（fun loop 重大调整）→ 加 Sprint 7 buffer，stage 升级延后；与 ADR-030 §VS Build commit fallback 一致。

---

## QA Plan Status

⚠️ **Sprint 6 plan 起草时 QA plan 尚未生成**。本 plan 写入后立即跑 `/qa-plan sprint-6` 生成 QA plan（per Sprint 5 流程：plan → qa-plan → dev-story 起步）。

> **Scope check**: S6-04 error/restart 是 Sprint 5 placeholder 派生 Sprint 6 实施（per Sprint 5 [B] split_2，非 scope creep）；S6-05 systematic wording amend 是 ADR governance debt 不是 scope creep；Sprint 6 整体 scope 与 ADR-030 §VS Build commit 一致（无 scope creep；S6-09 art asset 升级是 Sprint 5 retro AI-6 medium priority 衍生）。

---

## ADR-029 V3 Watch List（Sprint 6 持续监控 per Sprint 5 retro AI-7 + AI-8）

| 触发条件 | Sprint 5 baseline | Sprint 6 监控 + AI-8 dp ≥2 立即 candidate cycle |
|---------|-------------------|----------------|
| Type-5（V3.0 official 2026-05-12）| ✅ promote done | 持续监控；如 Sprint 6 新 dp（任一分支 a/b/c）→ V3.1 amend（minor amend stay V3）|
| Type-6 spike subscribe race after sync-fire | 1 dp 留观察（S5-1c 2026-05-09）| dp ≥2 立即 V3 candidate 评估（per AI-8）|
| Type-8 UIWindow second show vendor 销毁后重建 | 1 dp 留观察（S5-08 2026-05-11 R3 P3）| dp ≥2 立即 V3 candidate 评估；ADR-011 §G amend (S6-05) 应一并 cover wording spec |
| Type-9 R2 grep 完备性 meta-drift | 1 dp 留观察（S5-02 2026-05-12 Session 27 #2）| dp ≥2 立即 V3 candidate 评估；AI-3 S6-06 已部分 amend（R2 完备性子条）|
| Type-1 drift > 5 min | 0 min Sprint 5 | 跟踪 |
| Lite propagation v2 ROI < 50% | N/A | 跟踪；Sprint 6 S6-05 ADR amend 是 ADR-029 V3.0 §V3-1.b 修复模式 propagation 大头，ROI track |
| Multi-spike sequential 后仍 race | 0 | 跟踪 |
| **V4 升级触发条件 5 项**（per V3.0 §V3-5）| baseline 0（V3.0 promote 2026-05-12）| Sprint 6 末评估：是否 1/5 触发 → 起 V4 candidate |

**Sprint 6 watch list 新模式**：per AI-8 改造，**dp ≥2 立即 V3 candidate 评估**（不等 retro）。Sprint 5 Type-5 dp1 → dp6 累计 11 day 才 promote 的滞后模式不再重演。Sprint 6 如任一 Type-6/-8/-9 dp2 触发 → 立即 V3.1 amend（类 AI-1 same-sprint pattern）。

---

## Tech Debt Metric Snapshot（Sprint 6 启动 baseline per Sprint 5 retro Process Improvement）

> Sprint 5 end baseline = **18 条** TODO/FIXME/HACK in HotFix module（+3 / +20% from Sprint 4 baseline 15；within healthy < 30% threshold）。Sprint 6 起步保持监控；末次 retro 评估 delta。

| Metric | Sprint 5 end | Sprint 6 start | Sprint 6 end | Delta |
|--------|:------:|:------:|:------:|:------:|
| TODO 数 | (含在 18 中) | (待 measure) | (Sprint 6 末 measure) | TBD |
| FIXME 数 | (含在 18 中) | (待 measure) | (Sprint 6 末 measure) | TBD |
| HACK 数 | (含在 18 中) | (待 measure) | (Sprint 6 末 measure) | TBD |
| **Total HotFix module** | **18** | (待 measure) | (Sprint 6 末 measure) | TBD |

**Sprint 6 start checklist 1.0**: dev-story 起手前先跑 `Grep pattern 'TODO|FIXME|HACK' Assets/GameScripts/HotFix/`，记入 sprint-status.yaml::tech_debt_metric。**S6-12 Nice 实施后此 step 自动化**（per Sprint 4 retro action #9 + Sprint 5 retro AI-9 carry）。

**Sprint 6 期望 trend**：
- **如 S6-04 error/restart 完整实施** → 清除 GameFlow placeholder state 部分 TODO（chapter 1 部分；剩 chapter 2-5 留 Sprint 7+）
- **如 ADR-011 §G UIWindow lifecycle amend** → ui-system-006/-008 production code 内 UIWindow lifecycle TODO 可清
- **预期 Sprint 6 end**：TODO/FIXME/HACK ~16-20（with -2 ~ +2 swing 内 healthy）

---

## ADR-030 §VS Build Commitment Sprint 6 进度跟踪

| ADR-030 §Sprint 5-6 VS Build Commitment 项 | Sprint 6 目标 | 完成判定 |
|---------------------------------------------|---------------|---------|
| Chapter 1 (`靠近`) end-to-end VS slice 实体构建 | ✅ Sprint 5 已 100% | Sprint 5 done |
| Sprint 4 Track A 三系统 production code 实施 | ✅ Sprint 5 已 100% | Sprint 5 done |
| ≥3 internal playtest sessions | ⏳ Sprint 6 完成 3/3（S6-01/-02/-03）| sessions documented in `production/playtests/` |
| Playtest report | ⏳ Sprint 6 末（S6-03）| `playtest-report-vs-chapter-1-2026-05-XX.md` |
| `/gate-check pre-production` 重跑 | ⏳ Sprint 6 末（S6-10）| PASS expected |
| `production/stage.txt` 升级 | ⏳ Sprint 6 末 post-PASS（S6-10）| Pre-Production → **Production** |

**Sprint 6 ADR-030 commit 实施进度**: 目标 **100% 全 6 项完成**（Sprint 5 已完成 2/6 + Sprint 6 完成剩余 4/6）；如 gate-check FAIL → Sprint 7 buffer per ADR-030 fallback。

---

**下一步**: `/qa-plan sprint-6` — 生成 Sprint 6 QA plan（Production → Polish gate 必备 + playtest QA validation）；之后 `/story-readiness` 各 story → `/dev-story` 起步实施。Sprint 6 critical path 起步建议：**Track B AI-2/-3（S6-05/-06）+ Track C ui polish（S6-07/-08）+ S6-04 error/restart 并行 first week → 准备 S6-01 playtest 1 conditions**。
