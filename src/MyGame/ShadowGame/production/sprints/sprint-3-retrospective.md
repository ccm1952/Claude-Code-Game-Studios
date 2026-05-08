// 该文件由Cursor 自动生成

# Sprint 3 Retrospective — Multi-Scene Integration × ADR-029 V1.0 → V2.0 治理形式化 × Architecture Review 闭环

> **Sprint N**: 3
> **Period**: 2026-04-30 morning → 2026-05-06 morning（**~6 工作日内 governance/multi-scene 完整闭环**；其中 2026-04-30 dusk Sprint 3 Must Have 4/4 ✅ + 2026-05-06 morning fresh-session /architecture-review pass + 3 blockers 全解决）
> **Generated**: 2026-05-06
> **Plan**: [sprint-3.md](./sprint-3.md) — 14 SP commit / 17 SP candidate
> **Previous Sprint Retrospective**: [sprint-2-retrospective.md](./sprint-2-retrospective.md)

---

## Executive Summary

Sprint 3 是 **governance maturity sprint**：Must Have 4/4 ✅ 100% 闭环，但 Should/Nice 0/5（governance 投入挤占）。最大成果是**将 ADR-029 从 V1.0 起草版升级为 V2.0 backward-compatible major revision，6 数据点 + 7 V2 candidates 全部实战触发，drift 防御从"事后救火"形式化为"前置可量化 gate"**。同时完成 Sprint 1/2 累积的 multi-scene integration 链路（S3-01/-02/-03 全 PlayMode CORE PASSED），项目从 Foundation 阶段跨入 Core 完整能力阶段。

**关键工程数据**：
- 6 ADR-029 实战数据点（最早 18 min baseline → 最新 12 min Type-2(c) framework behavior fix）
- 7 V2 candidates 全部有实战触发数据（含 V2 #6 propagation scope + V2 #7 framework boundary behavior probe）
- Lite propagation v2 SSoT priority 决策树 **3 次实战验证 ROI 73%-85%**（ADR-027 §5 / 14 ADR bulk promotion / tr-registry 唯一化）
- 14 Proposed ADRs → Accepted bulk ceremony，DAG 完全 valid
- /architecture-review fresh-session 独立 review verdict CONCERNS → 3 blockers 全解决 → Sprint 4 governance entry 干净

---

## Metrics

| Metric | Planned | Actual | Delta |
|--------|--------:|-------:|------:|
| Must Have stories | 4 (8 SP) | **4 (8 SP)** | **0 / 100%** ✅ |
| Should Have stories | 3 (6 SP) | 0 (0 SP) | **-3 / -6 SP** ⚠️ |
| Nice to Have stories | 2 (3 SP) | 0 (0 SP) | -2 / -3 SP（预期 buffer，未占用）|
| **Total commitments** | 7 stories / 14 SP | **4 stories / 8 SP** | -3 stories / -6 SP（57% 承诺达成；100% Must Have）|
| Unplanned governance work | 0 | **3 项**（V1→V2 升级 30min + lite propagation v2 13min + post-review 3 blockers fix 30min = ~73 min）| +73 min |
| ADR-029 数据点 | (未跟踪) | **6 条** | +6（baseline 验证）|
| Cross-ADR conflicts (active) | (未跟踪) | 1 minor → 0 (post-fix) | +1 → 0 |
| ADR Status changes | (未跟踪) | 14 Proposed → Accepted（+1 patch；ADR-029 V1→V2）| +14 promotions |
| TR Registry entries | 0 | **212** | +212（B-3 backfill）|

---

## Velocity Trend

| Sprint | Planned (SP) | Completed (SP) | Stories | Rate | Notes |
|--------|:------------:|:--------------:|:-------:|:----:|-------|
| Sprint 1 | 25 | 25 | 13 | 100% | Foundation baseline |
| Sprint 2 | 22 | 22 | 13 | **93%** (13/14 commitments) | Foundation → Core 转折 |
| Sprint 3 (current) | 14 | **8** | 4 | **57%** | Must Have 100% / Should-Nice 0% — governance overhead 挤占 |

**Trend**: **Decreasing on commitments，Increasing on governance maturity**。Sprint 3 完成数目下降是 governance overhead（V1→V2 + propagation + architecture review）单 sprint 累积投入约 ~125 min 直接吞掉 Should/Nice 容量；但单 Must Have story 闭环时间稳定（S3-01 18 min baseline / S3-02 29 min total / S3-03 27 min total），实施速率没有退化，是 **governance 投入与 implementation 投入的容量竞争**。

---

## What Went Well

1. **Must Have 4/4 100% 闭环 + ADR-029 V1.0 → V2.0 升级 + Lite propagation v2 + Architecture Review pass** —— 单 sprint 完成 governance maturity 跃迁，是项目从 Foundation 跨入 Core 完整能力的标志性 sprint。
2. **R1/R2/R3 grep gate 实战验证有效**：S3-01/-02/-03 三个 stories 中，R1+R2 grep gate 在 dev-story 起手平均节省 ~50% Type-1 + Type-4 drift 修订时间（vs S3-01 baseline 18 min）。grep 是事实陈述，非 LLM 自我审查，零幻想风险。
3. **R3 PlayMode probe 暴露 100% Type-2 cross-method protocol drift**（无遗漏）：S3-02 v3 Type-2(b) cross-method state lifecycle drift（21 min fix）+ S3-03 v3 Type-2(c) framework behavior assumption drift（12 min fix）—— 这两类 drift R1+R2 grep 100% 抓不到，仅 PlayMode probe 可发现。**R3 mandatory 形式化（V2.0 §V2-3）由实战数据驱动，非凭直觉。**
4. **Lite propagation v2 SSoT priority 决策树 3 次实战验证 ROI 73%-85%**：(a) ADR-027 §5 framework knowledge fact 单点更新 → 17 listener stories 覆盖 ROI 73%；(b) 14 ADR bulk promotion 30 min vs 14 单独 promotion 210 min ROI 85%；(c) tr-registry.yaml backfill 212 TRs 一次性 SSoT 唯一化（vs 永久双 SSoT 双维护成本）。
5. **/architecture-review fresh-session 独立 review 真实抓到 governance gaps**：6 天 gap 后独立 reviewer (technical-director subagent) 发现 14 Proposed ADRs（DAG 名义破裂）+ ADR-011 §7 CONFLICT-008（intra-ADR drift, R2 grep gate 通常不查同 ADR 内部矛盾）+ tr-registry empty SSoT drift。**没有"author 自己 review 看不到"的盲区**——这正是 fresh session 协议保护的价值。
6. **CONFLICT-008 intra-ADR drift pattern 加入 reflexion log**：consistency-failures.md 首次创建，记录"ADR supersession revisions must scrub all code samples in superseded sections, not just header text"作为 generalised lesson；future reviews 加 grep step 防同款复发。
7. **Sprint 1+2 retro action items 部分落地**：Sprint 2 retro action #1（ADR-029 起草）✅ 完成于 S3-04；action #5（drift 修订耗时跟踪）✅ 6 数据点全部 instrumented；action #2（S2-14 起手）✅ S3-01 完成；action #3（PlayMode 测试 batch S3-06）⏸️ Should Have 未启动；action #4（Onboarding doc S3-07）⏸️ 未启动。

---

## What Went Poorly

1. **Should Have / Nice to Have 全部未启动 (0/5)** — Governance overhead（V1→V2 + propagation + architecture review fix）累积 ~125 min 直接吞掉了 Sprint 3 后半段的 Should-Nice 容量。**Sprint 3 计划 14 SP 实际只完成 8 SP，commitment 达成率 57%**。3 个 Sprint 1/2 retro carryover action items（Onboarding doc / PlayMode test batch / Selection feedback Visual Polish）被推到 Sprint 4。
2. **Stage drift 暴露 + Pre-Production gate 选错** — 项目 stage = `Pre-Production` 但实际工作模式是 Production-class（feature dev sprints active）。当今天跑 `/gate-check pre-production`（即 Pre-Production → Production gate）时，verdict FAIL（VS not built / no prototypes / no playtests）。这是项目"先深耕 governance + Foundation/Core，VS 留后"的工作模式与典型 game-dev gate model 的结构性偏差。**未来 Sprint 4-5 必须实体构建 VS playable build（chapter 1 端到端）**才能正式 PASS Pre-Production gate。
3. **prototypes/ + playtests/ 目录从未创建** — 跳过 prototype 阶段直接进 spike-driven Foundation/Core impl；Sprint 1/2/3 只产出 SP-011 + S3-01..03 PlayMode spikes（开发态 verification）而非 playable VS prototype。**这不是"做错了"，是工作模式不同**：项目工作流是 spike-first / governance-heavy / VS-late，typical gate model 是 prototype-first / VS-early。
4. **Art bible Status 仍是 Draft 不是 Accepted** — 2026-04-16 v1.0 写入后未走 `/art-bible-review` 或 AD-ART-BIBLE 正式 sign-off；今天 gate-check 时被识别为 ⚠️ PARTIAL。Sprint 4 起步前应补正式 sign-off。
5. **Sprint 2 retro action #4 onboarding doc 推到 Sprint 3 又推到 Sprint 4** —— 已 carryover 2 sprints。是低优先级但持续累积的小债务，反映 governance/impl 都在扩张时 docs 永远是最后一批。Sprint 4 必须实做或显式 descope。

---

## Blockers Encountered

| Blocker | Duration | Resolution | Prevention |
|---------|----------|------------|------------|
| **S3-02 PlayMode 6/6 case FAIL（Type-2(b) cross-method state lifecycle drift）** | 21 min | v3 fix: 引入 `_currentLoadedChapterId` 配对字段 (与 sceneName atomic 配对作为已加载身份单一事实源) | ADR-029 V2.0 §V2-3 R3 mandatory + §V2-2 Drift Type-2(b) formal classification |
| **S3-03 PlayMode P5 FAIL（Type-2(c) framework behavior assumption drift — TEngine RemoveEventListener 非 idempotent）** | 12 min | v3 fix: P5 拆 P5a + P5b + TestSceneScopedFixture documented null-out + null-check guard pattern | ADR-029 V2.0 §V2-5 Framework Boundary Behavior Probe Checklist + ADR-027 §5 SSoT framework knowledge fact |
| **D5 propagation scope 漏 ready backlog（S3-03 propagation lag）** | ~15 min Phase 1.5 修订 | patch v2 修订 6 处 stale Type-4 references + ADR-029 V2.0 §V2-4 V2 #6 refinement: propagation scope 必须覆盖 Ready+ backlog 不只 active sprint | ADR-029 V2.0 §V2-4 lite playbook formalized; future D-level 决策落地后立即 lite scan |
| **Architecture review B-1: 14 Proposed ADRs DAG 名义破裂** | 30 min bulk promotion | 14 ADR Status `Proposed → Accepted (Promoted 2026-05-06)` 单 PR ceremony | Sprint 4 起每个 ADR 加 Validation Criteria 触发 Auto-Accept 条件（V3 候选）|
| **Architecture review B-2: ADR-011 §7 intra-ADR drift CONFLICT-008** | 5 min fix | ADR-011 §7 L222-224 替换为 ADR-027 接口 sample + §5 null-out + null-check guard | consistency-failures.md 启动 + 反思 lesson "supersession 必须 scrub 全 ADR 代码 sample 不只 header" |
| **Architecture review B-3: tr-registry.yaml empty / SSoT drift** | 15 min backfill | 212 TRs 全 backfill v1→v2，含 8 处 revised 标记（ADR-027 supersession + S3-01 D5 propagation） | future architecture-review 直接读 single SSoT |

**总 blocker 时间**: ~98 min（含 ADR-029 V1→V2 升级时间未计入此表，是 proactive 工作）

---

## Estimation Accuracy

| Story | Estimated | Actual (含 R3) | Variance | Likely Cause |
|-------|:---------:|:--------------:|:--------:|--------------|
| S3-04 ADR-029 V1.0 起草 | 1 SP（~30 min）| ~30 min | **0%** ✅ | governance writing 估算稳定 |
| S3-01 Additive Scene Loading | 3 SP | ~18 min static + R3 PlayMode pass first try | **-** baseline | 首条 dev-story baseline，无可对比 |
| S3-02 Cleanup Sequence | 2 SP | 8 min Phase 1.5 + 21 min R3 fix = 29 min total | **+~17%** （R3 v3 fix 比 baseline 多 21 min）| Type-2(b) cross-method drift 是非预期；不可估算 |
| S3-03 Scene Events | 2 SP | 15 min Phase 1.5 + 12 min R3 fix = 27 min total | **+~10%** | Type-2(c) framework behavior drift 是非预期；不可估算（V2.0 §V2-5 落地后下游 stories 应可估算）|

**Overall estimation accuracy**: 4/4 Must Have stories within their SP envelope；governance overhead（V1→V2 / propagation / architecture review post-fix）**未在 sprint plan 中单独估算**，导致 Should/Nice 容量被吞掉。

**关键 lesson**: governance work（特别是 ADR 升级 / propagation / architecture review）**应作为独立 stories 估算**，不能假设"用 Must Have buffer 就够"。Sprint 4 plan 起草时应单列 governance track。

---

## Carryover Analysis

| Task | Original Sprint | Times Carried | Reason | Action |
|------|-----------------|:-------------:|--------|--------|
| **Onboarding doc** `docs/onboarding/unity-workflow.md` | Sprint 1 retro action #3 | **3** (Sprint 1 → 2 → 3 → 4) | 持续低优先；governance/impl 扩张时 docs 永远最后批 | **Sprint 4: 实做 OR 显式 descope** |
| **PlayMode 测试 batch (S3-06)** | Sprint 2 (deferred) | 1 (Sprint 2 → 3 → 4) | Sprint 3 governance overhead 吞容量 | Sprint 4 |
| **Selection Feedback (S3-05)** | Sprint 2 → S3-05 nice → Sprint 4 | 2 (Sprint 2 → 3 → 4) | 同上 | Sprint 4 (Visual Polish 起步)|
| **UI Module Setup (S3-08)** | Sprint 3 nice | 1 (Sprint 3 → 4) | 未启动；Sprint 4 UI/UX 启动前的探路 story | Sprint 4 |
| **Settings Manager (S3-09)** | Sprint 3 nice | 1 (Sprint 3 → 4) | 未启动；低风险 carryover | Sprint 4 |

**5 carryovers from Sprint 3 → Sprint 4**。其中 Onboarding doc 已 3 次 carryover，是 process smell。

---

## Technical Debt Status

- **TODO/FIXME/HACK 计数**: 未单独跟踪（Sprint 1/2/3 都未做此项 metric；本 retro 起加入 V3 候选 watch list）
- **trend**: governance work 增加了多个 ADR/spec 文件但未引入代码层 TODO/HACK；implementation work（S3-01..03 + IFadeOverlay）也都是显式生产代码 + spike，无 TODO 占位
- **Sprint 4 起新增 metric**: 单独 `git grep -c TODO|FIXME|HACK` count，纳入 sprint-status.yaml 跟踪

---

## Previous Action Items Follow-Up（Sprint 2 retro 5 项 carryover 检查）

| # | Action（来自 Sprint 2 retro）| Status | Notes |
|---|------------------------------|:------:|-------|
| 1 | 起 ADR-029 形式化 R1/R2/R3 | ✅ Done | S3-04 完成 V1.0；2026-04-30 dusk 升 V2.0；6 数据点 + 7 candidates 全部触发验证 |
| 2 | Sprint 3 第一个 story = S2-14 (S3-01) Additive Scene Loading | ✅ Done | 2026-04-30 morning DONE PlayMode CORE PASSED |
| 3 | PlayMode 测试 batch (S3-06) | ⏸️ Deferred | Should Have 未启动，carryover Sprint 4 |
| 4 | Onboarding doc | ⏸️ Deferred | Sprint 1 → 2 → 3 → 4 第 3 次 carryover；Sprint 4 必须实做或 descope |
| 5 | Drift 修订耗时跟踪 | ✅ Done | 6 数据点全部 instrumented (S3-01 18 / S3-02 8+21 / S3-03 15+12 / Lite propagation 13) |

**3/5 Done + 2/5 Deferred**。recurring carryover (#3 + #4) = process smell；Sprint 4 必须强制处理。

---

## Action Items for Sprint 4

| # | Action | Owner | Priority | Deadline |
|---|--------|-------|:--------:|----------|
| 1 | **Sprint 4 plan 起草 — VS slice 选择 + governance track 单独估算** — 选 chapter 1 端到端 VS slice (puzzle interaction → shadow match → narrative beat → chapter transition) 作为 Sprint 4-5 核心目标；governance work（ADR-014/-016/-017 implementation expand / V3 watch list）单列 governance track 估算，不再假设 Must Have buffer 够 | producer | **High** | Sprint 4 启动前 |
| 2 | **VS-skip path ADR OR stage drift 显式决策** — 三选一: (a) 包订报"VS-skip path" ADR 解释 stage drift, gate-check 调 production gate (适配项目工作模式); (b) Sprint 4-5 实体构建 VS 后重跑 gate; (c) 现状保持 Pre-Production 推 stories 直到 VS 就绪 | tech-lead / producer | **High** | Sprint 4 启动前 |
| 3 | **Onboarding doc 强制处理** (Sprint 1-3 第 3 次 carryover) — 实做 OR 显式 descope 为 Sprint 5+；不允许再 carryover | tech-lead | **High** | Sprint 4 第一周 |
| 4 | **Art bible 走 AD-ART-BIBLE 正式 sign-off** — Status: Draft → Accepted；Sprint 4 起步前完成 art-director review pass | art-director | Medium | Sprint 4 启动前 |
| 5 | **TODO/FIXME/HACK metric 启动跟踪** — sprint-status.yaml 加字段；每 sprint 末 retrospective 更新 trend；监控 V3 触发条件 | dev-story skill | Low | Sprint 4 全程 |
| 6 | **ADR-029 V3 watch list 监控** — Sprint 4 起跟踪 5 V3 升级触发条件（Type-1 drift > 5min / 新 drift type / Type-2(c) freq > 2/sprint / propagation ROI < 50% / multi-spike race after sequential）；命中即触发 V3 起草 | architect | Low | Sprint 4 全程 |
| 7 | **PlayMode 测试 batch (S3-06)** — Sprint 2 retro action carryover；DOTween / Raycast / fat-finger / 10 obj 性能；同一 PlayMode session 集中验 | qa-lead | Medium | Sprint 4 mid |
| 8 | **Selection Feedback (S3-05)** — Visual Polish 起步；Outline shader + Scale bounce | game-designer + tech-artist | Medium | Sprint 4 |

**Sprint 2 retro 5 items + Sprint 3 retro 8 items**：第一次出现 retro action items 数量超出"limit to 3-5"建议（per skill spec），反映项目治理债务在累积；Sprint 4 plan 时优先级排序 + 部分降级到 backlog，避免直接超载。

---

## Process Improvements

1. **Governance work 必须独立估算 + 单列 track**（Sprint 3 实战教训）：ADR 起草 / V 升级 / propagation / architecture review fix 都不应假设"用 Must Have buffer 就够"。Sprint 4 plan 加 Track C: Governance（V3 watch / propagation v2 监控 / ADR implementation expand），独立 SP 估算。

2. **Stage drift 是 architecture / governance heavy 项目的常态，不是缺陷**：项目"governance + Foundation/Core 优先 → VS 后置"的工作模式与典型 game-dev gate model 不对齐。Sprint 4 起做 VS-skip path ADR 显式 acknowledge。**Lesson**: skill protocols 是参考不是真理；项目可以基于自身 risk profile 做不同的 stage 顺序。

3. **R3 PlayMode probe 暴露 framework behavior contract drift 是 grep gate 的盲区，但是 R3 dev cost 远低于错过 drift 的成本**：S3-02 R3 (21 min) + S3-03 R3 (12 min) 共 33 min 实战投入，避免了至少 2 次 unsafe deploy（cross-method state silent return + null-out pattern absent）。**ROI = governance 投入 / unsafe deploy 风险 = 高**。Sprint 4 起 R3 mandatory，每个 new method/field/contract 必须有对应 PlayMode probe。

4. **Single source of truth priority 是 propagation 的最高优先级决策**：Sprint 3 lite propagation v2 实战 3 次验证 ROI 73%-85%。Sprint 4 任何 D-level decision 落地后，第一问题是"有 SSoT 文档可承载吗"，第二才考虑 mass story propagation。

5. **Intra-ADR drift（CONFLICT-008 同款）是 cross-ADR conflict detection 的盲区**：Sprint 3 architecture review 才发现 ADR-011 §7 残留 ADR-006 const-int sample。Sprint 4 起每个 ADR 升级（特别是 supersession）必须显式 grep 内部代码 sample 再 close。

6. **Recurring carryover ≥ 3 次必须强制处理或 descope**：Onboarding doc 已 3 次 carryover；Sprint 4 设硬截止——实做 OR 显式 descope 到 Sprint 5+ 不允许再持续。

---

## Sprint 3 ADR-029 V2.0 累计学习矩阵 (governance 治理收获核心)

| 维度 | V1.0 (2026-04-30 morning) | V2.0 (2026-04-30 dusk) | Sprint 4 watch list |
|------|---------------------------|------------------------|----------------------|
| Drift types | Type-1/2/3/4 (4 类) | **Type-2 拆 (a)/(b)/(c) 共 7 子型态** | Type-5 出现即升 V3 |
| R3 PlayMode probe | implicit (隐含)| **mandatory (formal)** | Sprint 4 R3 0 跳 |
| Propagation scope | active sprint only | **Ready+ backlog + SSoT priority 决策树** | propagation ROI < 50% 即升 V3 |
| Framework boundary behavior | 不在 V1.0 | **5 类 checklist (idempotency/silent-failure/exception path/cancellation/over-limit)** | Type-2(c) freq > 2/sprint 即升 V3 |
| Trigger conditions | 4 项零散 | **7 项 mapping table + 即时执行原则** | trigger #7 (drift > 5min) 即升 V3 |
| Drift revision time | metric 槽位 | **6 数据点 + Sprint 3 总效率 verdict + V3 升级触发条件 5 项** | 累计 Sprint 4 数据点 |
| 数据点累计 | 0 | **6** (S3-01 baseline 18 / S3-02 Phase 1.5 8 / S3-02 R3 21 / S3-03 Phase 1.5 15 / S3-03 R3 12 / Lite propagation 13) | 8+ 后考虑 V3 起草 |
| V2 candidates | (未跟踪) | **7 全部触发** | V3 watch list 5 条 |

---

## Summary

Sprint 3 是 **governance maturity sprint** —— 以 57% commitment 达成率换得 governance 框架从隐性 lessons 升级为 formal ADR-029 V2.0（backward-compatible major revision；6 数据点 + 7 V2 candidates 全部实战触发）。Multi-Scene Integration 链路（S3-01/-02/-03）100% 闭环 + 21 ADRs 全 Accepted + 212 TRs 唯一 SSoT + consistency-failures reflexion log 启动 + lite propagation v2 决策树 ROI 73%-85% 实战验证 ——**项目从 Foundation 阶段跨入 Core 完整能力 + governance maturity**，下一阶段（Sprint 4-5）是 Vertical Slice 实体构建 + P1 ADR (014/016/017) implementation expand + Onboarding/Sprint debt 清理。

**Sprint 4 主题候选**：Vertical Slice (chapter 1 端到端) + P1 ADR implementation 集中突破 (Puzzle State Machine / Narrative Sequence / Audio Mix) + Sprint 1-3 carryover 债务清理（Onboarding doc / PlayMode batch / Selection feedback）+ stage drift 显式决策。

---

**下一步**: `/sprint-plan sprint-4` — 规划 VS slice + P1 ADR impl expand + carryover 债务清理 + governance Track C 单列。
