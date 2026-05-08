// 该文件由Cursor 自动生成

# Sprint 4 Retrospective — Balance Restoration × P1 ADR Implementation Expand × Carryover 终结率 60%

> **Sprint N**: 4
> **Period**: 2026-05-06 morning → 2026-05-06 evening（**单日 ~5 小时 burst sprint** — Sprint 3 retro 后立即进入 Sprint 4，governance pause 后高强度 implementation）
> **Generated**: 2026-05-06
> **Plan**: [sprint-4.md](./sprint-4.md) — 15 SP commit / 18 SP candidate
> **Previous Sprint Retrospective**: [sprint-3-retrospective.md](./sprint-3-retrospective.md)

---

## Executive Summary

Sprint 4 是 **balance restoration sprint** — Sprint 3 governance heavy 后的容量再平衡，验证了 Sprint 3 retro 的核心 Process Improvement #2（**Track C governance 独立估算**）。Must Have 5/5 ✅ 100% 闭环，Should Have 2/3 ✅（S4-08 显式 descope → Sprint 5 VS 并轨），Nice 0/2（carryover Sprint 5 候选）。Sprint 4 commit rate **87.5% (7/8)** vs Sprint 3 57%，**+30 percentage points 反弹**，证实"governance 独立估算 + implementation 并行不互吞"模式有效。

**关键工程数据**：
- Track A (P1 ADR Implementation Expand) 100% — ADR-014/-016/-017 三个 Core 系统 implementation expand + 三个 story-001 framework ready (~1510 lines doc + framework，Sprint 5 VS chapter 1 全 P1 ADR 依赖 ready)
- Track C (Governance) 100% — ADR-030 VS-skip path + Onboarding doc (652 lines, Sprint 1 retro action #3 第 3 次 carryover **硬截止** closed)
- Carryover 终结率 **60% (3/5)** — Onboarding + Selection Feedback + PlayMode batch 三大债务全清；剩 2 Nice (UI Module / Settings) 留 Sprint 5
- ADR-029 V3 watch list **5/5 PASS 全 sprint** — Type-1 drift 0 min（仅 std C# compile fix）/ 无新 drift type / Type-2(c) framework behavior 0 (Sprint 3 baseline 1 不再增长) / Lite propagation N/A / Multi-spike race 0
- S4-07 PlayMode batch ALL PASSED 4/4 — Sprint 2→3→4 第 2 次 carryover **TERMINATED**

---

## Metrics

| Metric | Planned | Actual | Delta |
|--------|--------:|-------:|------:|
| Must Have stories | 5 (9 SP) | **5 (9 SP)** | **0 / 100%** ✅ |
| Should Have stories | 3 (6 SP) | **2 (5 SP)** | -1 / -1 SP（S4-08 explicit descope）|
| Nice to Have stories | 2 (3 SP) | 0 (0 SP) | -2 / -3 SP（预期 carryover Sprint 5）|
| **Total commitments (Must+Should)** | 8 stories / 15 SP | **7 stories / 14 SP** | -1 / -1 SP（**87.5% commitment 达成**；100% Must Have）|
| Carryover 终结 | 5 carryover | **3 终结** + 2 carry | **60% closure rate** |
| ADR-029 V3 watch list 触发 | 0/5 (新 baseline) | **0/5 (5/5 PASS)** | maturity 持续 ↑ |
| Compile fix incidents | 0 | 2 std C# (CS0664 + CS1061; ~3 min total) | non-drift; spike framework dependency |
| Track A SP delivered | 6 SP | **6 SP** ✅ | 100% (ADR-014/-016/-017 all expanded + story-001 frameworks) |
| Track C SP delivered | 3 SP | **3 SP** ✅ | 100% (ADR-030 + Onboarding) |
| Sprint duration (calendar) | 14 day plan | **0.5 day burst** (single afternoon ~5 hr) | **-13.5 days early** |
| Session 22 total投入 | — | ~302 min ≈ 5 hr | reference |
| TODO/FIXME/HACK baseline | (待 setup) | **15 条** (HotFix module) | Sprint 4 baseline established |

---

## Velocity Trend

| Sprint | Planned (SP) | Completed (SP) | Stories | Commit Rate | Notes |
|--------|:------------:|:--------------:|:-------:|:-----------:|-------|
| Sprint 1 | 25 | 25 | 13 | 100% | Foundation baseline |
| Sprint 2 | 22 | 22 | 13 | **93%** | Foundation → Core 转折 |
| Sprint 3 | 14 | 8 | 4 | **57%** | governance maturity sprint（V1→V2 升级吞 Should/Nice）|
| Sprint 4 (current) | 15 | **14** | **7** | **87.5%** | balance restoration sprint（Track C 独立估算验证有效）|

**Trend**: **Reversal + recovery**。Sprint 3 57% → Sprint 4 87.5% 反弹 +30 pts；but **不是回到 Sprint 1-2 100%/93% 水平**——其中差距代表 governance maturity 沉淀的"持续成本"（Track C 单 sprint 占 ~20% 容量），是合理的 trade-off。

**单 story 投入对比**：

| Story | Planned SP | Actual time | SP/min ratio |
|-------|:----------:|:-----------:|:------------:|
| S4-01 ADR-014 expand + story-001 | 2 SP | ~25 min | 12.5 min/SP |
| S4-02 ADR-016 expand + story-001 | 2 SP | ~25 min | 12.5 min/SP |
| S4-03 ADR-017 expand + story-001 | 2 SP | ~25 min | 12.5 min/SP |
| S4-04 ADR-030 VS-skip path | 2 SP | ~30 min | 15 min/SP |
| S4-05 Onboarding doc 652 lines | 1 SP | ~25 min | 25 min/SP |
| S4-06 Selection Feedback impl + tests | 3 SP | ~30 min | 10 min/SP |
| S4-07 PlayMode batch spike + verify | 2 SP | ~33 min (含 compile fix 3 min) | 16.5 min/SP |

**SP/min ratio 平均 ~14 min/SP** — 极快 burst 模式（Track A pattern reuse 拉低均值；S4-05 docs writing 拉高均值）。**Sprint 5 plan 不应假设这个速率持续** — VS chapter 1 build 涉及实体 Unity scene + asset + 多文件协调，预期 SP/min ratio 将回到 30-60 min/SP 区间。

---

## What Went Well

1. **Sprint 3 retro Process Improvement #2 (Track C 独立估算) 完全验证有效**。Sprint 3 governance 占 ~125 min 但因未独立估算挤占了 Should/Nice (commit rate 57%)；Sprint 4 governance 改 Track C 独立估算 ~75 min（ADR-030 30 + Onboarding 25 + 余项嵌入 = ~75 min），不再吞 Should Have，commit rate 反弹至 87.5%。**这是 retro action item 落地见效的教科书例子**。

2. **Track A pattern 三次 reuse 形成模板**。S4-01 ADR-014 + S4-02 ADR-016 + S4-03 ADR-017 三个 stories 复用同一 pattern：(§A ADR-027 Interface Protocol Mapping + §B Production Code Paths + §C ADR-029 V2.0 R3 Mandatory Coverage + §D Story-001 Framework reference + §E Validation Criteria Update + story-001 framework with 16-18 ACs + Phase 1.5 Readiness all PASS)。三次实施时间均 ~25 min，**pattern reuse 极大降低 cognitive load**，S4-03 (Track A 收官) 投入与 S4-01 (Track A 起步) 完全一致，无 fatigue 衰减。

3. **R1/R2/R3 grep gate 在 Track A 三个 stories 全程 PASS**（无 STOP）。ADR-014/-016/-017 expand + 三个 story-001 framework 全部走 ADR-029 V2.0 R1/R2/R3 grep gate + Phase 1.5 readiness verdict，0 个 R2 STOP / 0 个 Type-1 fantasy API drift。**Sprint 3 baseline 学到的"writing time = grep + design pattern internalize"在 Sprint 4 持续有效**。

4. **S4-04 ADR-030 VS-skip path 把 Sprint 3 stage drift 形式化** — Sprint 3 retro 提到的"项目实际是 Foundation/Core-First, VS-Late Pattern" 在 Sprint 4 落地为 Accepted ADR (~440 lines)。**Sprint 6 末 stage.txt: Pre-Production → Production 触发条件 formalize**；不再"模糊处理 stage drift"。

5. **S4-05 Onboarding doc 第 3 次 carryover 硬截止 closed** — Sprint 1 retro action #3 经 Sprint 1→2→3→4 三次 carryover，Sprint 4 Must Have 硬截止处理。最终 652 lines / 18 sections 一次成型，预计 onboard 时间 30-60 min。**反映了"持续累积的小债务最终爆破"模式**——Sprint 4 处理 25 min vs 累计 4 sprint 的 carryover 决策成本，**长期来看延期成本远超即时成本**（Sprint 5 retro action 应建立"硬截止规则"防止再次累积 4 sprint）。

6. **S4-06 Visual Polish 二段式闭环模式验证有效** — Visual/Feel story partial completion 模式：EditMode 单测覆盖参数层 + Editor manual 待 Sprint 5 VS chapter scene ready 后做。InteractableObjectFeedback 7 EditMode tests 通过 + manual evidence placeholder 写入。**避免"Visual story 因 manual 部分阻塞 Sprint closure"的反模式**。

7. **S4-07 PlayMode batch 4/4 ALL PASSED 单次跑过** — Sprint 2→3→4 第 2 次 carryover 终结。spike framework 一次 compile + run 通过（中间 2 处 std C# fix 不计 ADR-029 drift）。P3 fat-finger formula error **0.0000px PERFECT** + P4 perf **100x headroom** 都是超预期结果。**Sprint 1-2 推延的 4 个验证项 (S2-09/-10/-11/-13) 全部清零**。

8. **ADR-029 V3 watch list 5/5 PASS 全 sprint** — Type-1 drift 0 min（仅 std C# compile fix）/ 无新 drift type / Type-2(c) framework behavior 0（Sprint 3 baseline 1 不再增长）/ Lite propagation v2 N/A / Multi-spike race 0。**governance maturity 沉淀后真正"产出 watch list 不再触发"的稳态**——这是 ADR-029 V2.0 升级的核心目标。

---

## What Went Poorly

1. **Nice to Have 0/2 又一次 carryover** — S4-09 UI Module Setup + S4-10 Settings Manager 仍未启动，将进入 Sprint 5 carryover。这是 Sprint 3→4 第 1 次 carryover；如 Sprint 5 又未做将进入第 2 次。**模式重复**：Nice 总是 last-priority，sprint commit 5 hr burst 模式下不会有"余力"做 Nice。**Sprint 5 plan action**：要么提为 Should Have / Must Have，要么显式 descope→backlog。永远 "Nice to Have" 是 process smell。

2. **DOTween.UniTask 集成包 dependency 缺失在 spike 期暴露** — S4-07 P1 计划用 `tween.AsyncWaitForCompletion()`，编译报 CS1061。本质是项目依赖链缺失，但当前 fix（用 `UniTask.WaitUntil` 主循环轮询）作为 workaround 可接受。**Sprint 5 起步前应评估是否引入 DOTween.UniTask 集成包**（影响 Sprint 5 VS chapter 1 中 narrative effect / audio fade / interactable feedback 多个 DOTween 用法的精确 await 语义）。

3. **TODO/FIXME/HACK baseline 现在才 set (15 条)** — Sprint 3 retro action item AI-5 决议 Sprint 4 启动时设 baseline；但实际 Sprint 4 启动直接进 dev-story 没先 setup metric，到 retro 阶段才 grep。**process gap**：metric setup 应该是 sprint start checklist 的一部分，而不是依赖 retro 阶段补救。Sprint 5 plan 应在 sprint start phase 显式列 "metric baseline setup" 步骤。

4. **Sprint 4 14 day plan vs 实际 0.5 day burst** — calendar plan 14 day 实际 single afternoon 5 hr 完成 87.5% commit。**plan 估算偏离 28x**。原因：Sprint 4 是 AI agent burst session，不是人类多日 sprint。**Sprint 5 plan 应区分"calendar duration"与"work session duration"两个概念**，避免 plan/actual 系统性偏离。

5. **S4-08 显式 descope 但没在 Sprint 4 plan 中预先列 contingency** — 选择 [C] descope 是 sprint 末决定，不是 plan 起草时预先标记 "if-then" 路径。**reactive 而非 proactive descope**。Sprint 5 plan 应每个 Should/Nice 故事预标"if descoped, where does it go"路径。

---

## Blockers Encountered

| Blocker | Duration | Resolution | Prevention |
|---------|:--------:|------------|------------|
| **S4-07 CS0664 literal type mismatch** | ~1 min | `const float ToleranceMs = 50.0` → `const double ToleranceMs = 50.0` | 编程时统一 numeric 类型 (源 vs 目标比较类型一致) |
| **S4-07 CS1061 missing extension method** | ~2 min | `tween.AsyncWaitForCompletion()` → `UniTask.WaitUntil(() => tween.IsComplete())` | 用 dependency 前确认包已引入；DOTween.UniTask 集成包 Sprint 5 起评估 |
| **S4-08 art-director sign-off vs Sprint 5 VS art readiness 时序冲突** | sprint 末讨论 | 用户 [C] 拍板 descope → Sprint 5 并轨 | Sprint 5 plan 应将 "art bible sign-off + chapter 1 art asset" 同 sprint 并轨设计 |

**总 blocker 时间**: ~3 min compile + sprint end descope decision ~2 min = ~5 min。**Sprint 4 实质 zero-blocker sprint**（vs Sprint 3 21+12 = 33 min Type-2 drift fix）。

---

## Estimation Accuracy

| Task | Estimated SP | Actual time | Variance | Likely Cause |
|------|:------------:|:-----------:|:--------:|--------------|
| S4-05 Onboarding doc | 1 SP | 25 min | -on est (1 SP ≈ 30 min budget) | 5 sprint 累积想法一次释放，writing 比 fresh thinking 快 |
| S4-06 Selection Feedback | 3 SP | 30 min | **-67% under est (3 SP ≈ 90 min budget)** | EditMode-only 模式跳过 manual evidence 实际验证；S2 教训复用 |
| S4-07 PlayMode batch | 2 SP | 33 min | **-45% under est (2 SP ≈ 60 min budget)** | spike pattern 复用（SP-011/S301..303）+ Stopwatch+JSON output 模板成熟 |
| Track A 三个 stories (S4-01..03) | 2 SP each | 25 min each | -on est (2 SP ≈ 50 min budget; ratio 50%) | pattern reuse 完美收益 |
| S4-04 ADR-030 | 2 SP | 30 min | -on est (50% under) | 原创 ADR 比 expand 略慢但仍快 |

**Overall estimation accuracy**: 7/7 stories 实际投入 < SP budget。**System under-estimation**：所有 stories 都比 estimate 快完成。**反映 burst session 高效模式**，但也反映 SP estimation 没考虑 "AI agent burst vs human multi-day" 的 mode 差异。

**Sprint 5 SP estimation adjustment**：
- VS chapter 1 build 类工作（实体 Unity scene + asset + 多文件协调 + 多次 PlayMode iteration）应**主动 +50% SP buffer**，不能复用 Sprint 4 burst 速率
- pattern reuse 类工作（继续 expand ADRs）保持 Sprint 4 速率
- Visual/Feel story 二段式（EditMode + Editor manual）保持 Sprint 4 模式，但 manual 部分 SP 单独算入 carry sprint

---

## Carryover Analysis

| Task | Original Sprint | Times Carried | Sprint 4 Status | Sprint 5 Action |
|------|:---------------:|:-------------:|:---------------:|:---------------:|
| Onboarding doc → S4-05 | Sprint 1 retro action #3 | **3** | ✅ DONE | closed |
| Selection Feedback → S4-06 | Sprint 2 (was S2-15) | **2** | ✅ DONE (EditMode + manual placeholder) | manual evidence Sprint 5 VS chapter ready 后做 |
| PlayMode batch → S4-07 | Sprint 2 (was S2-16, Sprint 2 retro #3) | **2** | ✅ DONE 4/4 PASSED | closed |
| UI Module Setup → S4-09 | Sprint 3 (was S3-08) | 1 | ⏳ carryover Sprint 5 | **必须 promote 为 Should Have or descope; 不再 Nice** |
| Settings Manager → S4-10 | Sprint 3 (was S3-09) | 1 | ⏳ carryover Sprint 5 | 同上 |
| Art bible sign-off → S4-08 | Sprint 4 plan | **1 (descope)** | descoped→Sprint 5 | Sprint 5 必含 (VS art readiness gate) |

**Sprint 4 carryover 终结率 60% (3/5)** — Sprint 3 0% → Sprint 4 60% 大幅改善。剩 2 Nice 必须在 Sprint 5 处理（promote / 或 descope）；3 carryover (Sprint 5 + S4-08) 进入 Sprint 5。

---

## Technical Debt Status

- **Current TODO/FIXME/HACK total**: **15 条**（HotFix module）— Sprint 4 baseline established
- **Sprint 3 baseline**: not tracked (Sprint 4 第一次 set baseline)
- **Trend**: **baseline established, future trend tracking 起步**

**Sprint 4 处理的 carryover/debt**:
- ✅ Sprint 1 retro action #3 onboarding doc（3 次 carryover 硬截止）
- ✅ Sprint 2 retro action #3 PlayMode batch (S2-09/-10/-11/-13 推延项)
- ✅ Sprint 2 was S2-15 Selection Feedback Visual Polish

**新增 Sprint 4 期间 TODO/FIXME/HACK**: 0 条新增（Track A 三个 frameworks 中没有 TODO/FIXME 标记 — 因为 framework 状态本身是 Sprint 5 dev-story 实施 placeholder）

**Sprint 5 trend tracking**: Sprint 5 起每 dev-story 完成时检查 TODO/FIXME/HACK delta，sprint retro 集中评估。

---

## Previous Action Items Follow-Up（from Sprint 3 retro 8 action items）

| Action Item (from Sprint 3 retro) | Status | Notes |
|-----------------------------------|:------:|-------|
| **AI-1**: Sprint 4 plan + governance Track C 独立估算 | ✅ DONE | sprint-4.md 三轨架构 + Track C 独立 SP 实施 |
| **AI-2**: VS-skip path ADR (ADR-030) | ✅ DONE | S4-04 Accepted 2026-05-06 |
| **AI-3**: Onboarding doc 强制处理（第 3 次 carryover 硬截止）| ✅ DONE | S4-05 652 lines 闭环 |
| **AI-4**: Art bible AD-ART-BIBLE 正式 sign-off | ⏸ DESCOPED → Sprint 5 | descope decision 2026-05-06 (Session 22 #12) |
| **AI-5**: TODO/FIXME/HACK metric 启动跟踪 | ✅ DONE (baseline established) | 15 条 HotFix baseline 已 set；Sprint 5 trend tracking 起步 |
| **AI-6**: ADR-029 V3 watch list 监控 | ✅ DONE | 5/5 PASS 全 sprint，无新 drift 信号 |
| **AI-7**: PlayMode 测试 batch (S3-06 carryover) | ✅ DONE | S4-07 ALL PASSED 4/4 |
| **AI-8**: Selection Feedback (S3-05 Visual Polish) | ✅ DONE | S4-06 EditMode + manual placeholder |

**Sprint 3 retro action items 闭环率**: **7/8 完成 + 1 descoped → Sprint 5** — 行动力 87.5% (vs Sprint 2 retro action items 进 Sprint 3 闭环率 ~60%)。**retro → action 转化 cycle 持续优化**。

---

## Action Items for Sprint 5

| # | Action | Owner | Priority | Deadline |
|---|--------|-------|:--------:|:--------:|
| 1 | **VS chapter 1 build 启动**（chapter 1 端到端 playable scene + assets + 系统串联）— Sprint 5-6 双 sprint commitment per ADR-030 | AI | High | Sprint 5 first half |
| 2 | **S4-08 art bible sign-off** — art-director review，VS art readiness gate；与 chapter 1 art asset 实施同步迭代 | AI / art-director subagent | High | Sprint 5 mid |
| 3 | **S4-09 UI Module Setup + S4-10 Settings Manager carryover 决策** — 第 2 次 carryover 入 Sprint 5；必须 promote (Should Have / Must Have) 或显式 descope→backlog；不再 "Nice to Have" | AI | High | Sprint 5 plan 起草前 |
| 4 | **DOTween.UniTask 集成包评估** — Sprint 5 多处 DOTween + UniTask 用法（narrative effects / audio fade / interactable feedback）；评估引入与否 | AI | Medium | Sprint 5 first 3 stories 后 |
| 5 | **Sprint 5 plan 区分 "calendar duration" vs "work session duration"** — 避免 plan 估算 28x 偏离；burst session 模式 vs human multi-day 模式 explicit 标注 | AI | Medium | Sprint 5 plan 起草时 |
| 6 | **Sprint 5 每 Should/Nice story 预标 "if descoped, where does it go"** — proactive descope 路径，避免 reactive sprint 末决定 | AI | Medium | Sprint 5 plan 起草时 |
| 7 | **TODO/FIXME/HACK trend tracking** — Sprint 4 baseline 15；Sprint 5 末 retro 评估 delta；如 +30% 触发 tech debt sprint 候选 | AI | Low | Sprint 5 end retro |
| 8 | **ADR-029 V3 watch list 持续监控** — Sprint 5 是否触发新 drift 类型（特别是 VS chapter 1 build 引入的实体 Unity scene 类 drift）| AI | Low | Sprint 5 全程 + retro |
| 9 | **metric baseline setup 入 sprint start checklist** — Sprint 5 起在 plan 阶段就 set 当前 baseline（TODO/FIXME/HACK + ADR Status counts + Phase 1.5 success rate）；避免 metric 滞后 | AI | Low | Sprint 5 start |

---

## Process Improvements

1. **Track C 独立估算 mode 持续保留** — Sprint 3→4 验证有效，Sprint 5 持续应用。Sprint 5 Track C 候选项: art bible sign-off / sprint metric baseline setup / V3 watch list 监控嵌入；估算 ~3 SP（vs Sprint 4 实际 ~3 SP，符合 trend）。

2. **Pattern reuse 优先策略** — Track A pattern (ADR expand + story-001 framework) 三次复用证实有效。Sprint 5 VS chapter 1 build 应识别可复用 pattern：(a) PlayMode spike pattern (S301..S407) for runtime probe / (b) story-001 framework pattern for new epic stories / (c) Initialize/Shutdown lifecycle pattern (S2-08 / S4-06)。proactively reuse 优先于 from-scratch 设计。

3. **Carryover hard-deadline rule 形式化** — Sprint 1 retro action #3 onboarding doc 4 sprint carryover 暴露的"小 item 永远 last priority" 反模式。新规则：**任何 carryover ≥ 3 次必须 promote 为下一 sprint Must Have，或显式 descope→backlog (with rationale)，不允许第 4 次 carry**。Sprint 5 plan 起草时执行 (S4-09/-10 当前 1 次 carryover; if carry to Sprint 6 进入 2 次警告; Sprint 7 carry 第 3 次触发 hard deadline rule)。

4. **Sprint plan 区分 calendar vs work session** — Sprint 4 calendar 14 day vs actual 0.5 day burst 偏离 28x。新 plan 模板应有：(a) calendar duration（外部 commit）；(b) expected work session duration（实际工作量估算）；(c) burst capability（单 session 能完成的 SP 上限）。**避免后期看 plan 与 actual 系统偏离的解读混乱**。

5. **AI burst session mode 做 SP estimation calibration** — 当前 SP estimation 默认假设人类工作模式；AI burst session 实际 ratio ~14 min/SP 远超人类。Sprint 5 起在 plan 中标注 mode："AI burst" / "Hybrid" / "Human multi-day"，每个 mode 用不同 SP/min ratio 校准。**这是项目当前实际工作流的 transparency 必需**。

---

## Summary

Sprint 4 是 **Sprint 3 retro action item 落地见效的教科书 sprint** —— Track C 独立估算把 commit rate 从 57% 拉回 87.5%，Carryover 终结率从 0% 拉到 60%，Action item 闭环率从 ~60% 拉到 87.5%。同时 P1 Core 系统全部 framework ready (ADR-014/-016/-017)，Sprint 5 VS chapter 1 build 依赖全部就位。

**单 sprint 单一最重要改进**：将 Sprint 4 的 "Track C 独立估算 + pattern reuse + carryover 终结率高优先级" 三件套作为 Sprint 5+ 默认运营模式，**而不是回到 Sprint 1-2 单一 implementation 模式**。Sprint 5 即将进入 VS chapter 1 build 实体构建，工作模式将从"governance + framework"切换到"实体 scene + asset + 多文件协调"，但三件套的核心思想（容量分配 + 复用优先 + 债务清理硬截止）应保持。

**Sprint 4 final score**: ✅ **PASS — Balance Restoration 目标达成**（commit rate +30 pts / carryover terminated 60% / action items 87.5% / governance maturity 持续 / Sprint 5 VS dependencies ready）。

---

## Next Steps

- `/sprint-plan sprint-5` 起草 Sprint 5 plan：包括 (1) VS chapter 1 build commitment per ADR-030 / (2) S4-08 art bible sign-off 并轨 / (3) S4-09/-10 carryover 决策 / (4) Sprint 5 三轨架构 evolved (Track A: VS build / Track B: P1 ADR dev-story 实施 / Track C: governance lite)
- ADR-029 V3 watch list Sprint 5 起继续监控；如 Sprint 5 触发新 drift type（特别是 Unity scene/asset 类 drift），起草 V3 candidate
- TODO/FIXME/HACK Sprint 5 末再次 grep；trend = baseline 15 → Sprint 5 N，评估 delta
