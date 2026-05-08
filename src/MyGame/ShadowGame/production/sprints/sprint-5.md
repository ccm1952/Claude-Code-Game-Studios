// 该文件由Cursor 自动生成

# Sprint 5 — VS Chapter 1 Build 启动 × P1 ADR Dev-Story 实施 × Carryover Promote 决策

> **Sprint N**: 5
> **Phase**: Pre-Production / VS Build（Sprint 5-6 双 sprint VS playable commitment per ADR-030）
> **Start**: 2026-05-06 evening (Session 22 末延伸 OR Session 23)
> **End (expected)**: 2026-05-20（14 自然日 / 但实际 burst session 节奏）
> **Mode**: solo（沿 Sprint 3-4 模式；Sprint 6 末再次评估升 lean / full 必要）
> **Previous Sprint**: [sprint-4.md](./sprint-4.md)（**87.5%** 7/8 / Must Have 5/5 ✅；Should Have 2/3 ✅ + S4-08 descoped；Nice 0/2 carryover）
> **Retrospective**: [sprint-4-retrospective.md](./sprint-4-retrospective.md)
> **Governing ADR**: [ADR-030 Project Workflow](../../docs/architecture/adr-030-project-workflow-vs-late-pattern.md)（Sprint 5-6 双 sprint VS commitment）

---

## Sprint Goal

**Sprint 5-6 双 sprint VS Build commitment 启动 — Sprint 5 核心目标是 chapter 1 (`外升孔`) end-to-end VS slice 实体构建首版可玩 + Sprint 4 Track A 三个 P1 ADR (014/016/017) story-001 framework 进入 production code 实施 + Sprint 3→4 carryover (S4-09/-10) promote/descope 决策落地 + S4-08 art bible sign-off 与 chapter 1 art asset 实施同步并轨**。结束 Sprint 5 后：chapter 1 应能从 splash → chapter 1 scene load → 至少 1 个 puzzle 互动 → narrative beat → audio cue → ready-for-next-chapter 端到端可玩；Sprint 6 进 ≥2 次 playtest sessions + playtest report + `/gate-check pre-production` 重跑。

---

## Capacity & Estimation Model（Mode-aware Calibration per Sprint 4 retro action #5）

> Sprint 4 SP/min ratio = ~14 min/SP **burst mode**；Sprint 5 主体工作（实体 Unity scene + asset + 多文件协调 + 多次 PlayMode iteration）**预期 ratio 回升至 30-60 min/SP**（标准 multi-day work 模式）。

| 指标 | 数值 |
|------|------|
| Calendar Duration | 14 自然日 (2026-05-06 → 2026-05-20) |
| Expected Work Session Duration | 4-6 burst sessions × ~3 hr/session ≈ ~15-20 hr |
| Burst Capability (single session) | ~15-20 SP burst（Sprint 4 实测 ≈14 min/SP）；but Sprint 5 大部分非 burst-friendly 工作 |
| Sprint 5 承诺（Must + Should）| **8 stories / ~16 SP** |
| Nice to Have 延伸 | 2 stories / ~3 SP |
| 总候选 | 10 stories / ~19 SP |
| Buffer | ~17%（含 VS build 实体场景搭建不确定性 + 第一次 playtest 反馈引入的 mid-sprint adjustment 时间）|

**Mode 标注**：
- **Track A (VS Build)**: Hybrid mode (some burst-friendly framework setup + some multi-day Unity scene work；预期 30-50 min/SP)
- **Track B (P1 ADR dev-story)**: Hybrid mode (production code 比 framework 慢；预期 30-45 min/SP)
- **Track C (Governance)**: AI burst mode (沿 Sprint 4 模式；~14 min/SP)

---

## Tracks Architecture (3 轨 evolved per Sprint 4 retro Process Improvement #1)

### Track A — VS Chapter 1 Build (Critical Path; ADR-030 Sprint 5-6 commitment)

**Sprint 5 阶段**: chapter 1 实体构建首版 + 5 核心系统（Scene Mgmt / Object Interaction / Shadow Puzzle / Narrative / Audio）端到端串通到可玩状态。**Sprint 6 阶段**: ≥2 playtest sessions + report + gate-check rerun + stage 调整。

### Track B — P1 ADR Dev-Story 实施 (Sprint 4 Track A framework → Production Code)

**S4-01 / S4-02 / S4-03 story-001 framework** 已在 Sprint 4 完成（ADR-014/-016/-017 expand + story 各 ~520 lines framework）；Sprint 5 进入 production code 实施。**与 Track A 紧耦合**：Track A chapter 1 build 需要 S4-01 puzzle / S4-02 narrative / S4-03 audio 的 production code 才能跑通 end-to-end。

### Track C — Governance & Carryover Decisions（独立估算 ⚠️）

**Sprint 5 governance 工作量低**（Sprint 3→4 大头已完成；Sprint 5 主要是 art bible sign-off + carryover decision + metric baseline + V3 watch list 监控嵌入）。**S4-09/-10 必须做 promote / descope 决策**（per Sprint 4 retro action #3 — 不允许第 2 次 carryover 再次 Nice）。

---

## Tasks

### Must Have (Critical Path) — 5 items / 11 SP

#### Track A — VS Chapter 1 Build（Sprint 5 核心 Critical Path）

| ID | Story | Type | Complexity | Depends on | AC 要点 |
|----|-------|:----:|:----------:|------------|---------|
| **S5-01** | **Chapter 1 (`外升孔`) Unity scene 实体构建首版** + scene asset (prefab + lighting + UI) | Asset / Integration | **3 SP** | S3-01 ✅（Additive Scene Loading） + S4-01..03 ✅（framework）+ Art bible（S5-04）| chapter 1 Unity scene file ready 在 Assets/GameMain/Scenes/Chapter01.unity；含 baseline lighting setup + 投影墙面 + 至少 1 个可操作物件（per shadow-puzzle GDD MVP §单关卡 3m×3m×2.5m） + ≥1 光源 + ≥1 narrative trigger zone；scene 通过 SceneManager.LoadChapterSceneAsync 可 load |
| **S5-02** | **Chapter 1 end-to-end 5 系统串通可玩** | Integration | **3 SP** | S5-01 + S5-03/-05/-06 | end-to-end flow: Splash/MainMenu → Chapter 1 scene load → object interaction (Drag/Rotate per S2-08 ✅) → shadow match (per S5-03 puzzle production code) → narrative beat (per S5-05) → audio cue (per S5-06) → "下一章" UI button (即 transition trigger) → SceneManager.UnloadChapterSceneAsync (S3-02 ✅) → ready；至少 1 个完整 success path 可走 |

#### Track B — P1 ADR Dev-Story 实施（VS chapter 1 依赖前置）

| ID | Story | Type | Complexity | Depends on | AC 要点 |
|----|-------|:----:|:----------:|------------|---------|
| **S5-03** | **S4-01 Puzzle State Machine production code 实施** (`production/epics/shadow-puzzle/story-001-puzzle-state-machine.md` framework → code) | Logic / Gameplay | **2 SP** | S4-01 ✅ framework + ADR-014 ✅ + ADR-027 ✅ | story-001 16 ACs production code: PuzzleStateMachine.cs / PuzzleState.cs / PuzzleConfig.cs / IShadowPuzzleEvent.cs；ADR-029 V2.0 R1/R2 grep gate + R3 PlayMode probe (10 cases per story-001 §QA Test Cases) + Phase 1.5 readiness PASS |
| **S5-05** | **S4-02 Narrative Sequence Engine production code 实施 (chapter 1 部分 atomic effects)** | Logic / Narrative | **2 SP** | S4-02 ✅ framework + ADR-016 ✅ + ADR-017 ✅ (audio dependency) | chapter 1 narrative sequence MVP：≥3 atomic effects (text reveal / camera focus / object highlight) production code；INarrativeEvent.cs；R3 PlayMode probe ≥3 case；Phase 1.5 readiness PASS；剩 9 atomic effects + chapter 2-5 留 Sprint 6+ |
| **S5-06** | **S4-03 Audio Manager Init production code 实施 (3 mix layers + AudioModule activation)** | Logic / Audio | **2 SP** | S4-03 ✅ framework + ADR-017 ✅ + ADR-028 ✅ | AudioManager.cs / AudioMixLayer.cs / IAudioEvent.cs production code；GameApp.Entrance 调用 GameModule.Audio.Activate()（per ADR-028 §1）；3 mix layers volume 独立 + ducking 基础；R3 PlayMode probe (story-001 18 ACs 中 ≥8 CORE)；Phase 1.5 readiness PASS |

#### Track C — Governance（独立估算 ⚠️ per Sprint 4 retro Process Improvement #1）

| ID | Item | Type | Complexity | Depends on | AC 要点 |
|----|------|:----:|:----------:|------------|---------|
| **S5-04** | **S4-08 carryover — Art bible AD-ART-BIBLE 正式 sign-off** | Art / Governance | **1 SP** | S4-08 descoped → Sprint 5 + ADR-030 §VS Build commitment | art-director 走 `/art-bible-review` 或 AD-ART-BIBLE pass；art-bible.md Status: Draft → Accepted 2026-05-XX；as **VS art readiness gate**；与 S5-01 chapter 1 art asset 协调（如 sign-off 通过则 chapter 1 art asset spec 直接走 art-bible 标准；如 NEEDS REVISION 则 chapter 1 art asset 临时用 placeholder 等 Sprint 5 末 sign-off 后再升级）|

**Must Have 总计**: 5 stories / 11 SP（Track A 6 SP + Track B 6 SP + Track C 1 SP；注 Track A 含 dependency 来自 Track B 故按 critical path 计 Track A 6 + Track B 部分 ad hoc 入 Track A 串通 = Track A "effective" 12 SP但分 stories 算）

### Should Have — 3 items / 5 SP

| ID | Story | Type | Complexity | Depends on | 说明 |
|----|-------|:----:|:----------:|------------|------|
| **S5-07** | **chapter 1 第 1 次 internal playtest session + 反馈整理** | Playtest / QA | **2 SP** | S5-02 ✅（end-to-end 可玩） | playtest session ≥30 min；reuse session 从 splash → chapter 1 完整走通；记录 player journey 反馈；写入 `production/playtests/playtest-vs-chapter-1-2026-05-XX.md`（per ADR-030 §VS Build commitment 第 ≥3 项 第 1/3 次）|
| **S5-08** | **S4-09 carryover decision — UI Module Setup promote OR descope** | Integration / Governance | **2 SP** | S3-08 ✅ origin + ADR-011 ✅ | per Sprint 4 retro action #3 hard rule：第 2 次 carryover 必须 promote (做实施) OR descope (移 backlog with rationale)；如 promote 实施 UIModule init + base canvas + at least HUD root；如 descope 写明理由（推断 chapter 1 UI 需求是否 critical path） |
| **S5-09** | **S4-10 carryover decision — Settings Manager promote OR descope** | Logic / Governance | **1 SP** | S3-09 ✅ origin + ADR-008 ✅ | 同 S5-08 hard rule；如 promote 实施 SettingsManager 单例 + persistence；如 descope 写明理由 |

**Should Have 总计**: 3 stories / 5 SP

### Nice to Have — 2 items / 3 SP

| ID | Story | Type | Complexity | Depends on | 说明 |
|----|-------|:----:|:----------:|------------|------|
| **S5-10** | **DOTween.UniTask 集成包评估 + 引入决策** (per Sprint 4 retro action #4) | Tooling / Decision | **1 SP** | Sprint 4 S4-07 expose | 评估 Sprint 5 narrative effects / audio fade / interactable feedback 多个 DOTween 用法是否需要精确 await 语义；如需要走 引入流程；如不需要保持 UniTask.WaitUntil 模式；写 ADR-031 候选（如决策非 trivial）|
| **S5-11** | **Sprint metric baseline setup + V3 watch list 自动化** (per Sprint 4 retro action #9) | Governance / Tooling | **2 SP** | Sprint 4 retro Process Improvement | sprint start checklist 落地：sprint plan phase 0 添加 "metric baseline grep" step；TODO/FIXME/HACK + ADR Status counts + Phase 1.5 success rate 三 metric 自动 measure；写入 `production/sprint-status.yaml::sprint_metric_snapshot` 字段 |

**Nice to Have 总计**: 2 stories / 3 SP

---

## Carryover from Sprint 4

| Task | Reason | Times Carried | Sprint 5 Status | New Estimate |
|------|--------|:-------------:|:---------------:|:------------:|
| Art bible sign-off → S5-04 | S4-08 explicit descope→Sprint 5 (ADR-030 VS art readiness gate) | **2** (Sprint 4 plan + Sprint 4 descope) | Must Have / Track C | 1 SP（不变）|
| UI Module Setup → S5-08 | S3-08 was Nice → Sprint 4 was Nice (S4-09) → Sprint 5 第 2 次 carryover; **per hard rule promote 或 descope**| **2** | **Should Have** (promoted from Nice) | 2 SP（不变）|
| Settings Manager → S5-09 | S3-09 was Nice → Sprint 4 was Nice (S4-10) → Sprint 5 第 2 次 carryover; **per hard rule promote 或 descope**| **2** | **Should Have** (promoted from Nice) | 1 SP（不变）|

**3 Sprint 4→5 carryover 全部 promote 处理**（per Sprint 4 retro Process Improvement #3 carryover hard-deadline rule）：
- S5-04 进 Must Have（VS art readiness gate）
- S5-08/-09 promote Nice → Should Have（第 2 次 carryover；如 Sprint 5 仍未做 → Sprint 6 第 3 次将触发 hard rule = 必须 Must Have 或 descope→backlog）

---

## Sprint 5 Action Items 实现矩阵（Sprint 4 retro 9 项映射）

| Sprint 4 Action Item | Sprint 5 实现路径 | Story / Track |
|----------------------|-------------------|---------------|
| 1. VS chapter 1 build 启动 | ✅ Track A 全 (S5-01/-02 + 依赖 Track B) | Track A |
| 2. S4-08 art bible sign-off | ✅ S5-04 Must Have | Track C |
| 3. S4-09/-10 promote/descope 决策 | ✅ S5-08/-09 Should Have promote (第 2 次 carryover) | Track C |
| 4. DOTween.UniTask 集成包评估 | ✅ S5-10 Nice | Nice |
| 5. plan 区分 calendar vs work session | ✅ 本 plan §Capacity 已实施 (mode-aware ratio) | (本 plan) |
| 6. 每 Should/Nice 预标 if-descope 路径 | ✅ 本 plan 各 story §如 descope where to go | (本 plan, 见 §"if descoped" 标注 below) |
| 7. TODO/FIXME/HACK trend tracking | ✅ S5-11 Nice + sprint 末 retro 评估 | Nice |
| 8. ADR-029 V3 watch list 持续监控 | ✅ 嵌入 dev-story 起手 + sprint 末 retro 监控 | 嵌入 |
| 9. metric baseline setup 入 sprint start checklist | ✅ S5-11 Nice | Nice |

**8/9 action items 直接对应 stories；1 项嵌入 sprint workflow 不立独立 story**。

### "if descoped, where does it go" 路径预标 (per Sprint 4 retro action #6)

| Story | If descoped | Sprint 6 fallback |
|-------|------------|:-----------------:|
| S5-01 chapter 1 scene 实体构建 | **不可 descope**（Sprint 5-6 VS Build commitment 之 Sprint 5 核心；如 Sprint 5 时间不够 → 加 Sprint 5 buffer 或缩小 chapter 1 scope）| n/a |
| S5-02 5 系统串通 | **不可 descope**（同上 critical path）| n/a |
| S5-03/-05/-06 P1 ADR production | **不可 descope**（Track A chapter 1 build 必须依赖）| n/a |
| S5-04 art bible sign-off | 如 descope 第 3 次 carryover → Sprint 6 hard rule promote Must Have | Sprint 6 |
| S5-07 第 1 次 playtest | descope 接受；Sprint 6 集中 ≥3 sessions（ADR-030 commit 5-6 双 sprint ≥3 即可）| Sprint 6 |
| S5-08 UIModule | descope → backlog（rationale: chapter 1 MVP 用 GameObject UI 替代）| backlog |
| S5-09 Settings | descope → backlog（rationale: 不影响 chapter 1 fun loop）| backlog |
| S5-10 DOTween.UniTask 评估 | descope 接受 → 沿用 UniTask.WaitUntil 模式 | Sprint 6+ |
| S5-11 metric automation | descope 接受 → 手动 grep | Sprint 6+ |

---

## Critical Path

```
Sprint 5 dependency chain:

  S5-04 art bible sign-off (1 SP)
       ↓
  S5-01 chapter 1 scene 实体构建 (3 SP)         ← 受 S5-04 art readiness 影响
       ↓
  ────────── 并行 P1 ADR dev-story ──────────
  S5-03 Puzzle production (2 SP)               <─ 依赖 ADR-014 ✅ + S4-01 framework ✅
  S5-05 Narrative production (2 SP)            <─ 依赖 ADR-016 ✅ + S4-02 framework ✅
  S5-06 Audio production (2 SP)                <─ 依赖 ADR-017 ✅ + S4-03 framework ✅ + ADR-028 ✅
       ↓
  S5-02 5 系统串通 chapter 1 end-to-end 可玩 (3 SP)  ← Sprint 5 高潮
       ↓
  S5-07 第 1 次 playtest session (2 SP)        ← Should Have，Sprint 6 续做 ≥2

最长依赖链: S5-04 → S5-01 → (S5-03/-05/-06 并行) → S5-02 → S5-07 = 6 stories / 13 SP

并行轨:
  Track C governance + carryover decisions (S5-08/-09/-10/-11) 与 Track A/B 并行
```

**Sprint 5 critical path 注意点**：
1. **S5-04 art bible sign-off 不是 hard blocker** — chapter 1 scene 可先用 placeholder asset 启动，sign-off 通过后再升级；但 art bible "Status: Draft" 时 chapter 1 final-quality assets 不应直接确认
2. **S5-01 不需等所有 Track B 完成** — scene 可先以 stub state 启动（empty scene + lighting + 占位物件），Track B production code 接入后逐步增加场景内容
3. **S5-02 是 Sprint 5 高潮** — 需 S5-01 + S5-03/-05/-06 全 PASS 后才能跑通 end-to-end；如 5 系统中 1 个 production code 卡住 → S5-02 部分 descope（end-to-end with mock for missing 系统）

---

## Risks

| Risk | Probability | Impact | Mitigation |
|------|:-----------:|:------:|------------|
| **VS chapter 1 实体构建 estimation under-shoot** | High | High（Sprint 5 critical path）| Sprint 5 mid review；S5-01 拆 sub-stories 必要时；ADR-030 commit 是 Sprint 5-6 双 sprint，Sprint 5 不必 100% 完成 chapter 1 |
| **Track B P1 ADR production code 遇 Type-2(b/c) drift** | Medium | Medium（ADR-029 V2.0 R3 mandatory 已 instrumented；发现-修订路径成熟）| ADR-029 V2.0 §V2-3 + §V2-5 自动 cover；预留 Type-2 drift 修订时间 ~30 min/story |
| **Burst session SP estimation 在 Sprint 5 失效** | High | Medium（plan 估算可能与实际偏离）| 本 plan §Capacity 已用 mode-aware ratio；Sprint 5 mid 实际跑过 1-2 stories 后 calibrate 实际 SP/min ratio |
| **Art bible review iterative review (sign-off NEEDS REVISION)** | Medium | Low | Sprint 5 用 placeholder art (text-based / generic shapes) 启动 chapter 1；正式 art asset 随 sign-off 完成升级 |
| **chapter 1 fun loop 第一次 playtest 即不及格** | Medium | High（如 fun loop 需重设计 → Sprint 6 plan 大改）| 本身 ADR-030 已 instrumented：Sprint 5 第 1 次 playtest 即识别问题；Sprint 6 加 buffer 应对 fun loop 调整 |
| **Sprint 5 仍累积 carryover (S5-08/-09 第 3 次)** | Medium | Medium | 本 plan promote Nice → Should Have；如 Sprint 5 仍未做 → Sprint 6 hard rule 强制 Must Have 或 descope→backlog |

---

## Dependencies on External Factors

- **art-director subagent / art bible review** (S5-04) — 影响 chapter 1 art asset 升级时机
- **Unity Editor manual operations** (S5-01 / S5-07) — 实体 scene 搭建 + playtest 必须在 Unity Editor 内手动操作；burst session 模式中混入 manual 操作会拉低 SP/min ratio

---

## Definition of Done for Sprint 5

- [ ] Must Have 5/5 完成（S5-01..06 全）— chapter 1 端到端可玩 + 3 P1 ADR production code + art bible sign-off
- [ ] chapter 1 (`外升孔`) Unity scene 实体构建完成；可从 splash → chapter 1 → ≥1 puzzle → narrative beat → next chapter ready 完整走通
- [ ] S4-01/-02/-03 三个 story-001 production code 实施 + ADR-029 V2.0 Phase 1.5 readiness PASS + R3 PlayMode probe ≥3 case PASS each
- [ ] Should Have 至少 2/3 完成（S5-07 第 1 次 playtest + S5-08/-09 carryover decision 落地，theoretical 3/3）
- [ ] art-bible.md Status: Draft → Accepted（S5-04 sign-off pass）
- [ ] S5-08/-09 第 2 次 carryover 处理：promote 实施完毕 OR descope→backlog with rationale
- [ ] QA plan exists（`production/qa/qa-plan-sprint-5-2026-05-XX.md`）
- [ ] Sprint 5 retrospective 写入
- [ ] sprint-status.yaml metric snapshot 更新（如 S5-11 完成则自动；否则手动）
- [ ] ADR-029 V3 watch list 持续监控：Sprint 5 是否触发新 drift type（特别 Unity scene/asset 类）

---

## Sprint 5 主题候选 → Sprint 6 衔接

完成 Sprint 5 后，Sprint 6 主目标是 **VS Validation + Production stage advance**：
- Sprint 6 ≥2 次 internal playtest sessions（与 Sprint 5 S5-07 凑齐 ADR-030 §≥3 commit）
- chapter 1 fun loop 调整 based on playtest feedback
- `/gate-check pre-production` 重跑 → expected PASS
- `production/stage.txt`: Pre-Production → Production formal advance
- chapter 2 start (Sprint 7+ candidate)

**风险接受 path**：如 Sprint 6 末 gate-check 仍 FAIL（fun loop 重大调整 / VS scope 不达标）→ 加 Sprint 7 buffer，stage 升级延后；与 ADR-030 §VS Build commitment 一致。

---

## QA Plan Status

⚠️ **Sprint 5 plan 起草时 QA plan 尚未生成**。本 plan 写入后立即跑 `/qa-plan sprint-5` 生成 QA plan（per Sprint 4 流程：plan → qa-plan → dev-story 起步）。

> **Scope check**: 本 sprint 三个 stories (S5-08/-09 carryover) 涉及 Sprint 3 Nice 范畴 promote 至 Should Have。如 Sprint 5 实际进入实施时识别 scope 与原 GDD 差异，跑 `/scope-check ui-system` + `/scope-check settings-accessibility`。

---

## ADR-029 V3 Watch List（Sprint 5 持续监控 per Sprint 4 retro action #8）

| 触发条件 | Sprint 4 baseline | Sprint 5 监控 |
|---------|-------------------|---------------|
| Type-1 drift > 5 min | 0 min（仅 std C# fix）| 跟踪；Sprint 5 实体 Unity scene 工作可能引入 Unity 类 drift |
| 新 drift type 出现 | 0 | **Sprint 5 重点关注：实体 scene + asset + Inspector 类 drift（Type-5 candidate？）** |
| Type-2 (c) framework behavior > 2/sprint | 0 (Sprint 3 baseline 1) | 跟踪 |
| Lite propagation v2 ROI < 50% | N/A (Sprint 4 无 propagation) | 跟踪；Sprint 5 production code 可能触发 |
| Multi-spike sequential 后仍 race | 0 | 跟踪 |

**Sprint 5 新关注点**：实体 Unity scene + asset + Inspector workflow 类 drift（如 prefab GUID drift / scene reference drift / asset import setting drift）—— 这些 R1/R2 grep 难抓，可能引入 Type-5 候选。如发现 → 起 V3 candidate 立项。

---

## Tech Debt Metric Snapshot (Sprint 5 启动 baseline per Sprint 4 retro action #9)

> Sprint 4 baseline 时 set 完毕 (15 条 TODO/FIXME/HACK in HotFix module)；Sprint 5 起步保持监控；末次 retro 评估 delta。

| Metric | Sprint 4 baseline | Sprint 5 start (= Sprint 4 end) | Sprint 5 end | Delta |
|--------|:------:|:------:|:------:|:------:|
| TODO 数 | (含在 15 中) | (待 measure) | (Sprint 5 末 measure) | TBD |
| FIXME 数 | (含在 15 中) | (待 measure) | (Sprint 5 末 measure) | TBD |
| HACK 数 | (含在 15 中) | (待 measure) | (Sprint 5 末 measure) | TBD |
| **Total HotFix module** | **15** | (待 measure) | (Sprint 5 末 measure) | TBD |

**Sprint 5 start checklist 1.0**: dev-story 起手前先跑 `rg -tcs -c 'TODO|FIXME|HACK' Assets/GameScripts/HotFix/`，记入 sprint-status.yaml::tech_debt_metric。**S5-11 nice 实施后此 step 自动化**。

---

**下一步**: `/qa-plan sprint-5` — 生成 Sprint 5 QA plan（Production → Polish gate 必备 + chapter 1 playtest QA validation）；之后 `/story-readiness shadow-puzzle/story-001`、`narrative-event/story-001`、`audio-system/story-001` 三个并行 → `/dev-story` 起步实施。

---

## ADR-030 §VS Build Commitment Sprint 5 进度跟踪

| ADR-030 §Sprint 5-6 VS Build Commitment 项 | Sprint 5 目标 | 完成判定 |
|---------------------------------------------|---------------|---------|
| Chapter 1 (`外升孔`) end-to-end VS slice 实体构建 | ✅ S5-01 + S5-02 | end-to-end 可玩 |
| Sprint 4 Track A 三系统 production code 实施 | ✅ S5-03 + S5-05 + S5-06 | R3 PlayMode probe 全 PASS |
| ≥3 internal playtest sessions | ⏳ Sprint 5 完成 1/3（S5-07）；Sprint 6 完成 2/3 | sessions documented in `production/playtests/` |
| Playtest report | Sprint 6 | Sprint 6 末 |
| `/gate-check pre-production` 重跑 | Sprint 6 末 | Sprint 6 |
| `production/stage.txt` 调整 | Sprint 6 末 (post-PASS) | Sprint 6 |

**Sprint 5 ADR-030 commitment 实施进度**: **目标 ~50%**（chapter 1 build + Track A production code + 1/3 playtest sessions）— Sprint 6 末完成另一半（≥2 playtest + report + gate rerun + stage advance）。
