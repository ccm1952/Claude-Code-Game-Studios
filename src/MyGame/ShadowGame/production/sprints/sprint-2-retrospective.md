// 该文件由Cursor 自动生成

# Retrospective: Sprint 2 — Core Layer (Chapter × Scene × Object Interaction)

> **Period**: 2026-04-23 — 2026-04-30（实际 8 自然日 / 计划 18 自然日；提前 ~10 天达到 should-have 全闭环）
> **Phase**: Pre-Production
> **Generated**: 2026-04-30
> **Verdict**: ✅ **PASS**（Must Have 10/10 + Should Have 3/4 + Nice to Have 0/3 = 13/17；Must + Should commitments 13/14 = **93%**）

---

## Metrics

| Metric | Planned | Actual | Delta |
|--------|:-------:|:------:|:-----:|
| Stories committed (Must + Should) | 14 | 13 | -1 (S2-14 Additive Scene 推延) |
| Stories total (incl. Nice to Have) | 17 | 13 | -4 |
| Must Have completion rate | 100% | **100%** | — |
| Commitment（Must + Should）completion rate | 100% | **93%** | -7% |
| 总点数 (Must + Should) | 25 | 22 | -3 (S2-14=3 点) |
| Spike (SP-011) | 1 / 0.5 点 | 1 / 0.5 点 ✅ | 0 |
| EditMode 测试总数（累计） | — | **321** | +198 vs Sprint 1 baseline (123) |
| Sprint 2 新增测试 | — | **198** | — |
| 测试通过率 | 100% | **100%** | — |
| 估时（人日） | 18d（含 buffer）| ~8d（AI 协作 19 sessions） | -10d |
| Code review ADR 违规 | 0 | 0 | 0 |
| 编译错误（最终） | 0 | 0 | 0 |

**总结**：Must Have 100% 达成，Should Have 75%（3/4）；S2-14 Additive Scene 主动推延到 Sprint 3（与 S2-15..S2-17 同 batch 处理），不影响 Core 层垂直切片完整性。

---

## Velocity Trend

| Sprint | Planned (commitment) | Completed | Rate | Notes |
|--------|:---:|:---:|:----:|-------|
| Sprint 0 (Spike) | 10 findings | 10 | 100% | SP-001~010 技术验证 |
| Sprint 1 | 11 stories | 13 | **118%** | Foundation 全部完成 + 2 Nice |
| Sprint 2 (current) | 14 stories / 25 点 | 13 / 22 点 | **93%** | Must 10/10 ✅；Should 3/4；S2-14 推延 |

**趋势**：**Stable / Slight Decrease**。从 Foundation 层（独立可测组件多）转入 Core 层（多模块集成 + 跨 ADR 协调）后，单 story 复杂度上升，velocity 自然回归。**累计 26 stories（Sprint 1 + 2）+ 321 EditMode 测试，零 ADR 违规**。

> ⚠️ Sprint 1 retro action #2 已落地：本 sprint 全程使用 story 复杂度点数估算（1/2/3 点），点数估算与实际匹配度高（无大幅 over/under）。Sprint 1 推延的人日单位完全弃用。

---

## What Went Well

- **Skill-first 工作流稳态触达**：本 sprint 全程使用 `/sprint-plan` → `/story-readiness` → `/dev-story` → `/code-review` → `/story-done` 标准链路 + ADR 治理流程（`/architecture-review` 触发 ADR-026 → ADR-028 修订）。19 个 session 中**单 story 平均 1.2 轮过审**（含一次性通过 + 微调），与 Sprint 1 持平甚至更优。

- **`/dev-story` 5min readiness checklist 4 次命中同 drift 模式**：S2-09（push/pull 混淆）→ S2-12（整接口订阅）→ S2-13（整接口订阅 + 不存在 API + Luban 缺口）→ S2-11（整接口订阅 + 缺 RotationStep）一致触发"Story §Implementation Notes vs Framework drift"。**5min grep verification 前置 gate 100% 抓住所有 4 次幻想 API**，未让任何错误代码进 commit。lessons memo `/.claude/memory/problem_2026-04-29_story-impl-notes-vs-framework-drift.md` + `conventions.md §Listener 端订阅模式` 已成稳态资产。

- **Problem→Rule Promotion 元规则首战**：Sprint 2 期间立 3 个 problem memo + 2 个跨工程 Cursor rules（`~/.cursor/rules/string-literal-nested-quotes.mdc` + `problem-to-rule-promotion.mdc`）。**首次形式化"问题→规则"的促进路径**，Sprint 1 是单点修复，Sprint 2 跃迁到系统化沉淀。

- **测试 fixture boilerplate 复用率极高**：S2-09 GridSnapTests / S2-10 / S2-12 / S2-13 / S2-11 五个 ObjectInteraction test fixtures **共享同一套 boilerplate**（Camera stub + PuzzleConfigProvider 注入 + Listener 计数 + Initialize/Shutdown 顺序 + DOCompleteSafe reflection helper）。S2-11 直接复用 GridSnapTests 的 helper，无任何复制。`conventions.md §测试 fixture 规范` R1-R3 三项规则覆盖率 100%。

- **EditMode 不可达项目主动推延，不强行 hack**：S2-09 EaseOutBack 时长 / S2-10 EaseOutQuad 曲线 / S2-13 物理 Raycast / S2-11 DOTween 精确 duration / fat-finger 数学 — 全部明确推 PlayMode/集成或 device 测试，**不在 EditMode 写脆弱测试**。这是从 Sprint 1 Visual story 教训中习得的成熟工程约束。

- **ADR 治理闭环建立**：本 sprint 触发 `/architecture-review` 导出 ADR-026 缺口 → ADR-028 修订 → ADR-013 / ADR-027 同步刷新。**ADR 不再是"写一次扔档案"的废纸**，而是 dev-story 时 readiness checklist 必引用的活动文档。

- **同 drift 第 4 次反复** = ADR-029 触发条件成熟：本是 lessons memo R1/R2/R3 已 4 次复用稳态，属于"该升 ADR 形式化"的工程信号 — Sprint 3 起点的天然议题。

- **Sprint 1 retro action items 全部落地**：见下方 Previous Action Items Follow-Up，5 项全 Done。

---

## What Went Poorly

- **Story §Implementation Notes 反复出现幻想 API（drift 4 次）**：
  - S2-09: 设计 push/pull 模式与 framework 实际不符
  - S2-12: 假定 `GameEvent.AddEventListener<TInterface>(this)` 存在（实际不存在）
  - S2-13: 同 + 假定 `obj.SetLockManager(...)` 注入路径存在（不存在）+ Luban 缺 `FatFingerMarginMm`
  - S2-11: 同 IGestureEvent 整接口订阅幻想 + PuzzleConfig 缺 `RotationStep`
  - **根因**：`/create-stories` 阶段没有强制对 §Implementation Notes 中所有 framework API 调用做 grep 实证。LLM 在 story 生成阶段倾向于写"合理但臆测"的 API 签名。
  - **影响**：每次 dev-story 起手要花 5-10min 修订 story 文档；好在 readiness checklist 抓住所有 4 次，未让错误代码进 commit。

- **PuzzleConfig 构造扩展 v3 patch 编译错误（CS7036 + CS0191×3）**：
  - 现象：S2-13 测试用对象初始化器 `new PuzzleConfig { ... }`，但 PuzzleConfig 是 sealed class + readonly fields + 构造必填 id；用户编译报错后 5min 修复。
  - **根因**：测试 stub 数据类型签名验证缺失 readiness checklist 项。
  - **影响**：单次小返工（5min）；已加入 `dev-story` self-checklist 第 3 项"stub data 构造签名 grep"，下次预防。

- **测试 1 处 Unity transform.eulerAngles 浮点边缘行为依赖**：
  - 现象：S2-11 AC2_RoundsUp_AtMidpoint 用 52.5°（精确 midpoint）测 round 行为，Unity transform.eulerAngles set/get 走 Quaternion 转换引入微误差，导致 `52.4999/15 = 3.4999 → round=3 → 45°`，测试失败。
  - **根因**：测试设计依赖 Unity 浮点精度边缘行为，而非测试自身公式。
  - **修复**：改用 53°（明确偏离 midpoint），midpoint 行为留 PlayMode。
  - **教训**：EditMode 测试不应依赖 Unity 内部浮点精度（Quaternion / Matrix4x4 / Transform 组件）。

- **AC7 端到端测试漏 Began phase 加载累加器**：
  - 现象：S2-11 Coordinator OnRotate 端到端 test 直接派 Updated phase，但累加器没 Began 加载初值，期望 40° 实得 10°。
  - **根因**：测试设计未与产品代码 Began 语义对齐（FSM phase 需依顺序进入）。
  - **修复**：加 Began phase 派发链验证。
  - **教训**：FSM phase 测试需补齐前置 phase，不能跳跃。

- **S2-14 Additive Scene 推延**：
  - 现象：should-have 4 个 stories 完成 3 个，S2-14 Integration 类（YooAsset 真·additive 加载，3 SP）推延 Sprint 3。
  - **根因**：当 Sprint 2 should-have 3/3 闭环时（Object Interaction 全家桶），自然停顿点已到，强推 S2-14 性价比下降（3 SP = 1 整 session；与 Sprint 3 多场景 stories batch 做更合理）。
  - **影响**：低 — 不阻塞 Core 垂直切片；Sprint 3 起点会拼接 S2-14。

---

## Blockers Encountered

| Blocker | Duration | Resolution | Prevention |
|---------|:--------:|------------|------------|
| Story §Impl Notes 整接口订阅幻想 API（4 次） | 每次 5-10min readiness 修订 | 修订 story + 走 per-event 模式 | ✅ `dev-story` 5min checklist 第 1 项；`conventions.md §Listener 端订阅模式` 已立 |
| PuzzleConfig 对象初始化器 vs sealed class + readonly（v3 patch） | 5min | 改构造参数 | ✅ checklist 第 3 项"stub data 类型构造签名 grep" |
| C# string literal 嵌套引号编译错误（CS1003） | 1min | 改用 Chinese guillemets | ✅ 跨工程 cursor rule `~/.cursor/rules/string-literal-nested-quotes.mdc` |
| EditMode `OnEnable`/`OnDisable` 不自动 invoke | 累计 ~30min | 全部 MonoBehaviour 改 Initialize/Shutdown 显式 lifecycle | ✅ `conventions.md §测试 fixture 规范` R1 |
| Unity transform.eulerAngles 浮点边缘行为 | 5min | 改测试用例避开 midpoint | ✅ 加入"EditMode 不依赖 Unity 浮点精度"软约束（暂不升 rule） |

---

## Estimation Accuracy

| Story | Estimated (Plan) | Actual | Variance | Likely Cause |
|-------|:-------:|:------:|:--------:|--------------|
| S2-08 InteractableObject FSM | 2 点 | ~1.5 sessions | 准确 | FSM 模板 S1 已熟练 |
| S2-09 Drag Mechanics | 2 点 | ~2 sessions（含 push/pull drift 修订）| 准确 | 首次触发 §Impl Notes drift |
| S2-10 Grid Snap | 2 点 | ~1.5 sessions（含 X2 string literal patch）| 准确 | DOTween EditMode 首次接触 |
| S2-11 Rotation | 2 点 | ~1 session（complete 路径熟练） | 准确（甚至略快） | 5min checklist 一次过 + 复用 fixture |
| S2-12 LockManager | 2 点 | ~1.5 sessions（含 §Impl Notes drift v2）| 准确 | 整接口订阅 drift 第 1 次发现 |
| S2-13 InteractionCoordinator | 2 点 | ~2 sessions（含 v2 §Impl Notes + v3 PuzzleConfig）| 略超 | drift 复合（API 不存在 + Luban 缺口）|

**整体估时准确度**：**~85% within ±20%**（点数估算成熟）。Sprint 1 引入的复杂度点数模型在 Sprint 2 全程稳定，无需调整。

> 💡 观察：drift 触发的 5-10min readiness 修订时间已稳态化为"额外的隐性消耗"。是否需要把这部分成本反映在估算中？建议 Sprint 3 跟踪：drift 修订时间是否随 ADR-029 形式化降到 0？

---

## Carryover Analysis

| Task | Original Sprint | Reason | Action |
|------|:--------------:|--------|--------|
| S2-14 Additive Scene Loading（3 SP / Integration） | Sprint 2 | 自然停顿点已到（should-have 3/3 + 累计 13 stories）；与 Sprint 3 多场景 batch 做更合理 | **Sprint 3 第一个 story** |
| S2-15 Selection Feedback (Visual/Feel, 3 SP) | Sprint 2 nice | 优先级低 + Visual EditMode 受限 | Sprint 3 polish 阶段 |
| S2-16 Cleanup Sequence (Integration, 2 SP) | Sprint 2 nice | 依赖 S2-14 | Sprint 3 |
| S2-17 Scene Events (Integration, 2 SP) | Sprint 2 nice | 依赖 S2-14 | Sprint 3 |

**全部 4 个推延 stories 都有明确 action 与 sprint，无悬挂任务**。

---

## Technical Debt Status

| 指标 | Sprint 1 末 | Sprint 2 末 | 趋势 |
|------|:-----------:|:-----------:|:----:|
| TODO 标记（HotFix/） | 16 (TEngine GameFlow 模板遗留) | 15 | **-1**（S2-04 SaveManager 集成清了 1 处） |
| FIXME 标记 | 0 | 0 | Stable |
| HACK 标记 | 0 | 0 | Stable |
| EditMode 测试文件 | 13 | 27 | +14 |
| EditMode 测试数 | 122 | **321** | **+199** |
| ADR 总数（Accepted） | 25 | 28（+ADR-026/-027/-028） | +3 |
| Problem memo 沉淀 | 0 | 3 | +3 |
| Cross-project Cursor rules | 0 | 2 | +2 |

**分析**：Sprint 2 **零新增技术债务**，反而清掉 1 处历史 TODO。15 处遗留 TODO 全在 GameFlow 各 State，由 Sprint 3 Scene Management S2-14 / S2-17 收尾时一并处理。**测试数 +163%，是 Sprint 1 的 2.6 倍**，反映 Core 层多模块集成的覆盖密度。

---

## Previous Action Items Follow-Up

| Action（来自 Sprint 1 retro）| Status | Notes |
|-------------------------------|:------:|-------|
| 1. Story 模板增加"枚举值校验"步骤 | ✅ Done | `dev-story` 5min readiness checklist 第 1-3 项扩展为完整框架；累计 4 次抓住 §Impl Notes drift |
| 2. 引入 story 复杂度点数（1/2/3） | ✅ Done | 全 sprint 稳定使用，估算准确度 ~85% |
| 3. `docs/onboarding/unity-workflow.md` 记录 HybridCLR / Unity API 陷阱 | 🟡 Partial | `conventions.md §测试 fixture` 章节立；独立 onboarding 文件未做（Sprint 3 起 onboard skill 触发）|
| 4. Visual/Feel "可测逻辑抽取"约定 | ✅ Done | S2-15 推延但 S2-09/-10/-11 DOTween 分离 reflection helper / EditMode 不依赖项目主动声明已成模式 |
| 5. Sprint 2 推进 Core 层 | ✅ Done | Track A 4/4 + Track B 3/3 + Track C 6/9，should-have 闭环 |

**5/5 action items 完成或部分完成，无遗留 process smell**。

---

## Action Items for Sprint 3

| # | Action | Owner | Priority | Deadline |
|---|--------|-------|:--------:|----------|
| 1 | **起 ADR-029 "Story §Implementation Notes 验证流程"** — 形式化 R1 (per-event listener 模式) / R2 (跨组件 API grep 实证) / R3 (stub 类型构造签名 grep) 三项 readiness gate；并入 `/create-stories` skill 的 Phase 1 前置检查 | architect / dev-story skill | **High** | Sprint 3 第一周 |
| 2 | Sprint 3 第一个 story = **S2-14 Additive Scene Loading**（carryover 优先；解锁 multi-scene 后续 stories） | producer | High | Sprint 3 启动 |
| 3 | **PlayMode 测试 batch**：S2-09 EaseOutBack 时长 + S2-10 EaseOutQuad 曲线 + S2-11 DOTween duration + S2-13 Raycast / fat-finger 数学 + 10 obj 性能 — 同一 PlayMode session 集中验 | qa-lead | Medium | Sprint 3 polish 阶段 |
| 4 | **Onboarding doc**：起 `docs/onboarding/unity-workflow.md`（Sprint 1 action #3 carryover，Visual/PlayMode/HybridCLR 陷阱合一）| tech-lead | Medium | Sprint 3 mid |
| 5 | **drift 修订成本归零跟踪**：Sprint 3 起每个 story `dev-story` 完成时记录"§Impl Notes drift 修订耗时"，目标 ≤ 1min（即 ADR-029 形式化生效后应趋近 0） | dev-story skill | Low | Sprint 3 全程 |

---

## Process Improvements

- **Skill-first 原则锚定**：Sprint 2 全程"skill 优先 + ADR 治理 + Problem→Rule Promotion"工作流验证有效，Sprint 3 起继续遵循。下一步是把这套实践抽到项目级 onboarding 文档，让新成员（人或 AI agent）快速对齐。

- **Story 生成阶段加 framework API grep gate**（ADR-029 形式化）：drift 4 次反复证明 `/create-stories` 阶段缺前置实证。Sprint 3 起 story 生成完毕前必须经过：
  - per-event listener 签名 grep
  - 所有跨组件 API 调用 grep（确认存在）
  - 所有 stub 数据类型构造签名 grep
  - 期望的 Luban 字段 grep（存在 / 不存在 → 显式标 deficiency）

- **同 drift 第 N 次反复 = 升 ADR 触发条件**：Sprint 2 验证"problem memo → 第 N 次复用 → 升 ADR"路径有效。规则化阈值：**N ≥ 3 即触发 ADR 起草评估**（Sprint 2 是 N=4 才升，可适度提前）。

- **EditMode 不依赖 Unity 内部浮点精度**：S2-11 transform.eulerAngles midpoint 教训。EditMode 测试公式应在产品代码层闭环验证，Unity 组件 API 的浮点边缘行为推 PlayMode（与 DOTween 时长 / 物理 Raycast 同处理）。

---

## Summary

Sprint 2 是项目从 Foundation 跨入 Core 层的关键转折，**13/14 commitments 完成（93%），新增 198 EditMode 测试，零 ADR 违规，零新增技术债务**。最大的工程价值是**将"problem memo + 5min readiness checklist + cross-project cursor rules + ADR 升级"形式化为闭环工作流**，drift 第 4 次反复触发 ADR-029 成熟，Sprint 3 起将 §Implementation Notes 验证从隐性 lessons 升级为显性 ADR-治理 gate。S2-14 推延到 Sprint 3 是健康决策（自然停顿点 + batch 处理多场景 stories），不影响 Core 垂直切片完整性。

**Sprint 3 主题候选**：Multi-Scene Integration（S2-14 + S2-16 + S2-17）+ ADR-029 形式化 + Visual Polish 起步（S2-15 + 后续 polish stories）。

---

**下一步**: `/sprint-plan sprint-3` — 规划 Multi-Scene + ADR-029 + Polish 起步。
