// 该文件由Cursor 自动生成

# Sprint 4 — P1 ADR Implementation Expand × Carryover 债务清理 × Governance Track C 单列

> **Sprint N**: 4
> **Phase**: Pre-Production（VS slice 准备阶段；Sprint 4-5 双 sprint 累积达 VS playable）
> **Start**: 2026-05-06
> **End (expected)**: 2026-05-20（14 自然日 / 11 工作日；vs Sprint 3 实际 8 工作日完成 8 SP，Sprint 4 计划 ~15 SP，buffer 充裕）
> **Review Mode**: solo（沿 Sprint 3 模式；governance Track C 充分沉淀后 Sprint 5 可考虑升 lean）
> **Previous Sprint**: [sprint-3.md](./sprint-3.md)（4/9 commitments / 57% — Must Have 4/4 ✅，Should/Nice 0/5 governance overhead 吞容量）
> **Retrospective**: [sprint-3-retrospective.md](./sprint-3-retrospective.md)

---

## Sprint Goal

**完成 P1 ADR (014/016/017) implementation expand + 清理 Sprint 1-3 累积 carryover 债务 + governance Track C 单列估算（不再让 governance overhead 吞 stories 容量）**。结束 Sprint 4 后，Puzzle State Machine + Narrative Sequence + Audio Mix 三大 Core 系统具备首批可实施 stories 骨架；Sprint 1-3 carryover (Onboarding doc / Selection feedback / PlayMode batch / UI module / Settings) 全部清理或显式 descope；Sprint 5 起 VS slice (chapter 1) 实体构建工作可启动 — 项目从 Foundation/Core 完整能力阶段跨入 **VS playable 准备阶段**。

---

## Capacity & Estimation Model

> Sprint 1 = 25 SP / 13 stories（100%）；Sprint 2 = 22 SP / 13 stories（93%）；Sprint 3 actual = 8 SP / 4 stories（57% — governance overhead 单 sprint ~125 min 吞 Should/Nice）。

| 指标 | 数值 |
|------|------|
| Sprint 4 承诺（Must + Should）| **8 stories / 15 SP** |
| Nice to Have 延伸 | 2 stories / 3 SP |
| 总候选 | 10 items / 18 SP |
| Buffer | ~17%（含 P1 ADR impl story 不确定性 + Track C governance 实际投入）|

**Velocity 参考**：基于 Sprint 3 实际 8 SP（governance heavy）+ Sprint 1-2 average ~23 SP（impl heavy），Sprint 4 计划 15 SP 是 commitment-focus 中庸路径。**关键 Sprint 3 教训**：governance work 单列 Track C **独立估算**（不再假设"用 Must Have buffer 就够"）。

---

## Tracks Architecture (3 轨)

### Track A — P1 ADR Implementation Expand (Critical Path; Sprint 5 VS 依赖)

**ADR-014 / ADR-016 / ADR-017 都是 2026-05-06 bulk promote 到 Accepted，但内容是 2026-04-22 v1 起草版，需要 implementation expand 才能进 dev-story**。三个 ADR 各自需要补：(1) 详细 Implementation Notes（具体 method 签名 / state machine 实现细节）；(2) 首批 stories 的骨架（在 production/epics/ 内创建 1-2 个 stories）；(3) Sprint 5 VS 依赖的 minimal viable 系统能力。

### Track B — Carryover 债务清理

Sprint 1-3 累积 5 个 carryover：S3-05 Selection Feedback / S3-06 PlayMode batch / S3-07 Onboarding doc（Sprint 1 retro action #3 第 3 次 carryover；硬截止）/ S3-08 UI Module Setup / S3-09 Settings Manager。**Sprint 4 必须实做或显式 descope，不再持续 carry**。

### Track C — Governance & Process（独立估算）

Sprint 3 retro 关键教训：governance work 不能假设 Must Have buffer 够。Track C 单列：VS-skip path ADR + Art bible sign-off + V3 watch list + TODO metric + (review-mode.txt 创建 / docs/onboarding/ 设立等小事)。

---

## Tasks

### Must Have (Critical Path) — 5 items / 9 SP

#### Track A — P1 ADR Implementation Expand

| ID | Story | Type | Complexity | Depends on | AC 要点 |
|----|-------|:----:|:----------:|------------|---------|
| **S4-01** | **ADR-014 Puzzle State Machine implementation expand** + create `production/epics/shadow-puzzle/story-001-puzzle-state-machine.md` | ADR / Story | **2 SP** | ADR-008 ✅ + ADR-012 ✅ | 把 ADR-014 v1 (2026-04-22) 起草版扩展实施细节：state machine FSM 完整 (含 NearMatch hysteresis 0.40/0.35 + PerfectMatch threshold 0.85 + AbsenceAccepted Ch.5 状态)；首批 story (story-001) 框架（ADR-029 V2.0 §V2-3 R3 mandatory；§V2-5 framework boundary probe checklist）；TR 覆盖：TR-puzzle-005..009/012-014（关 9 ⚠️ TRs）|
| **S4-02** | **ADR-016 Narrative Sequence Engine implementation expand** + create `production/epics/narrative-event/story-001-sequence-engine.md` | ADR / Story | **2 SP** | ADR-027 ✅ + ADR-007 ✅ | ADR-016 内容 expand：sequencer architecture 详细化 (parallel effects / queue max 3 / drop oldest / resource load failure resilience)；首批 story 骨架；TR 覆盖：TR-narr-002/003/005..011（关 8 ⚠️ TRs）|
| **S4-03** | **ADR-017 Audio Mix Architecture implementation expand** + create `production/epics/audio-system/story-001-audio-manager-init.md` | ADR / Story | **2 SP** | ADR-001 ✅ | ADR-017 内容 expand：3 mix layers (Ambient/SFX/Music) + volume formula + ducking + crossfade；ADR-028 §1 AudioModule activation 真接入；首批 story 骨架；TR 覆盖：TR-audio-002..005/008..011/013/014（关 10 ⚠️ TRs）+ TR-settings-008 |

#### Track C — Governance（独立估算 ⚠️）

| ID | Item | Type | Complexity | Depends on | AC 要点 |
|----|------|:----:|:----------:|------------|---------|
| **S4-04** | **VS-skip path ADR (ADR-030)** — Project Workflow Decision: Foundation/Core-First, VS-Late Pattern | Governance / ADR | **2 SP** | Sprint 3 retro Process Improvement #2 | 起草新 ADR 显式 acknowledge stage drift；说明项目"governance + Foundation/Core 优先 → VS 后置"模式与典型 game-dev gate model 不对齐的合理性 + 风险接受；调整 stage.txt 路径；包括 Sprint 4-5 VS-build commitment + risk mitigation |
| **S4-05** | **Onboarding doc** `docs/onboarding/unity-workflow.md` (Sprint 1 retro action #3 第 3 次 carryover — **硬截止**) | Docs | **1 SP** | — | HybridCLR Installer + Unity API 自动更新 + DOTween EditMode + Visual EditMode 受限 + 测试 fixture boilerplate + ADR-029 V2.0 R1/R2/R3 readiness gate + Lite propagation v2 + spike-driven workflow 实操合一；新成员 (人 / AI agent) onboard 文档 |

**Must Have 总计**: 5 stories / 9 SP（Track A 6 SP + Track C 3 SP）

### Should Have — 3 items / 6 SP

| ID | Story | Type | Complexity | Depends on | 说明 |
|----|-------|:----:|:----------:|------------|------|
| **S4-06** | `object-interaction/story-005-selection-feedback` | Visual/Feel | **3 SP** | S2-08 ✅ + ADR-013 ✅ | Outline shader + Scale bounce；Visual Polish 起步；Sprint 1 Visual story 教训：抽参数层做 EditMode 单测，shader 本身手动 Editor 验证（**carryover Sprint 2→3→4 第 2 次**；Sprint 4 必做或 descope）|
| **S4-07** | **PlayMode 测试 batch** (S3-06 carryover) | QA / PlayMode | **2 SP** | S2-09/-10/-11/-13 ✅ | DOTween 精确 duration（EaseOutBack / EaseOutQuad）+ Raycast 物理 + fat-finger 数学 + 10 obj 性能；同一 PlayMode session 集中验（**carryover Sprint 2→3→4 第 2 次**；Sprint 4 必做或 descope）|
| **S4-08** | **Art bible AD-ART-BIBLE 正式 sign-off** | Art / Governance | **1 SP** | art-director review | art-director 走 `/art-bible-review` 或 AD-ART-BIBLE pass；art-bible.md Status: Draft → Accepted；Sprint 5 VS 起步前必备 |

**Should Have 总计**: 3 stories / 6 SP

### Nice to Have — 2 items / 3 SP

| ID | Story | Type | Complexity | Depends on | 说明 |
|----|-------|:----:|:----------:|------------|------|
| **S4-09** | `ui-system/story-001-uimodule-setup` (S3-08 carryover) | Integration | **2 SP** | ADR-011 ✅ | UI 系统初始化；Sprint 5 UI/UX 启动前的探路 story（**carryover Sprint 3→4 第 1 次**）|
| **S4-10** | `settings-accessibility/story-001-settings-manager` (S3-09 carryover) | Logic | **1 SP** | ADR-008 ✅ | Settings 单例 + 持久化；与 SaveManager 已有集成路径（**carryover Sprint 3→4 第 1 次**）|

**Nice to Have 总计**: 2 stories / 3 SP

---

## Carryover from Sprint 3

| Task | Reason | Times Carried | Sprint 4 Status | New Estimate |
|------|--------|:-------------:|:---------------:|:------------:|
| Onboarding doc → S4-05 | Sprint 1 retro action #3；3 次 carryover；硬截止 | **3** | Must Have / High | 1 SP（不变）|
| Selection Feedback → S4-06 | S3-05 Should Have 未启动；governance overhead 吞容量 | 2 | Should Have | 3 SP（不变）|
| PlayMode batch → S4-07 | S3-06 Should Have 未启动 | 2 | Should Have | 2 SP（不变）|
| UI Module Setup → S4-09 | S3-08 Nice 未启动 | 1 | Nice | 2 SP（不变）|
| Settings Manager → S4-10 | S3-09 Nice 未启动 | 1 | Nice | 1 SP（不变）|

**5 carryovers 全部排入 Sprint 4**。S4-05 Onboarding 已第 3 次 carryover → Must Have 硬截止处理。

---

## Sprint 4 Action Items 实现矩阵（Sprint 3 retro 8 项映射）

| Sprint 3 Action Item | Sprint 4 实现路径 | Story / Track |
|----------------------|-------------------|---------------|
| 1. Sprint 4 plan + governance Track C 独立估算 | ✅ 本 plan 已执行 | (本 plan) |
| 2. VS-skip path ADR OR stage drift 显式决策 | S4-04 ADR-030 起草 | Must Have / Track C |
| 3. Onboarding doc 强制处理（第 3 次 carryover）| S4-05 Onboarding doc | Must Have / Track C |
| 4. Art bible AD-ART-BIBLE 正式 sign-off | S4-08 sign-off pass | Should Have |
| 5. TODO/FIXME/HACK metric 启动跟踪 | sprint-status.yaml 加字段；本 sprint 末 retro 加 trend section（无独立 story）| 嵌入 Sprint 闭环工作 |
| 6. ADR-029 V3 watch list 监控 | 嵌入 dev-story 起手 + sprint 末 retro 监控 5 触发条件（无独立 story）| 嵌入 Sprint 闭环工作 |
| 7. PlayMode 测试 batch (S3-06)| S4-07 | Should Have |
| 8. Selection Feedback (S3-05) | S4-06 | Should Have |

**6/8 action items 直接对应 stories；2 项嵌入 sprint workflow 不立独立 story**。

---

## Critical Path

```
Track C (governance) - 独立轨，Track A 并行：
  S4-04 VS-skip ADR (2 SP) ── 解锁 stage 显式决策
  S4-05 Onboarding doc (1 SP) ── carryover 硬截止

Track A (P1 ADR impl expand) - 三个 ADR 可并行：
  S4-01 ADR-014 Puzzle State Machine impl expand (2 SP)
  S4-02 ADR-016 Narrative Sequence impl expand   (2 SP)
  S4-03 ADR-017 Audio Mix impl expand              (2 SP)
        ↓ ↓ ↓
  Sprint 5 VS chapter 1 build 依赖（需要 014+016+017 minimal viable 能力）

Track B (carryover) - 与 Track A 并行：
  S4-06 Selection Feedback (3 SP) ← Visual Polish 起步
  S4-07 PlayMode batch (2 SP)     ← Sprint 2 testing 债务清理
  S4-08 Art bible sign-off (1 SP) ← Sprint 5 VS art readiness

最长依赖链: 0 (并行性最高 sprint)
```

**关键观察**：Sprint 4 是 Sprint 3 governance heavy 之后的 **balance restoration sprint** —— 三轨并行均衡分配，governance 占 3/15 SP (~20%)，impl 占 6/15 SP (~40%)，carryover 占 6/15 SP (~40%)。

---

## Risks

| Risk | Probability | Impact | Mitigation |
|------|:-----------:|:------:|------------|
| **P1 ADR impl expand 遇 Type-2 (b/c) drift** | High | 中（ADR-029 V2.0 R3 mandatory 已 instrumented，发现 → 修订路径成熟）| ADR-029 V2.0 §V2-3 + §V2-5 自动 cover；预留 Type-2 修订时间 |
| Onboarding doc 仍 carryover (第 4 次)| Low | 高（process smell 持续累积）| Must Have 硬截止 + 显式 descope alternative |
| Track C governance 时间预估再次超出 | Medium | 中（Sprint 3 V1→V2 + propagation + review fix ~125 min 实际）| Track C 单列估算 + Sprint 末 retro 校准 |
| Art bible sign-off blocked by external review | Low | 低 | Should Have；如 sign-off 不通过留 Sprint 5 |
| ADR-014/016/017 expand 互相依赖发现 | Medium | 中 | Sprint 4 mid review；如发现 dependency 提前调度 |

---

## Dependencies on External Factors

- 无 external dependency（项目 self-contained Sprint）

---

## Definition of Done for Sprint 4

- [ ] Must Have 5/5 完成（S4-01..05）
- [ ] Track A 三个 P1 ADR (014/016/017) 都 expand + 首批 story 骨架建立
- [ ] Track C VS-skip path ADR (S4-04) Accepted；stage 显式决策落地
- [ ] Onboarding doc (S4-05) 写入 docs/onboarding/unity-workflow.md
- [ ] Should Have 至少 2/3 完成（理想 3/3 — Selection Feedback / PlayMode batch / Art bible sign-off）
- [ ] 5 carryovers 全部清理（实做或 descope）
- [ ] QA plan exists（`production/qa/qa-plan-sprint-4-2026-05-06.md`）
- [ ] sprint-status.yaml 加 TODO/FIXME/HACK metric 字段
- [ ] ADR-029 V3 watch list 5 触发条件 sprint 末检查
- [ ] Sprint 4 retrospective 写入

---

## Sprint 4 主题候选 → Sprint 5 衔接

完成 Sprint 4 后，Sprint 5 起 **Vertical Slice Build (chapter 1 端到端)**：
- 选 chapter 1 (`靠近`) 作为 VS slice
- 串通：scene load (S3 完成) → object interaction & rotation (S2 完成) → shadow match (Sprint 4 ADR-014 ready) → narrative beat (Sprint 4 ADR-016 ready) → audio (Sprint 4 ADR-017 ready) → chapter transition (S3 完成)
- ≥3 internal playtest sessions
- Playtest report 写入 `production/playtests/`
- VS Validation 4 项全 PASS → /gate-check pre-production 重跑应 PASS

---

**下一步**: `/qa-plan sprint` — 生成 Sprint 4 QA plan（Production → Polish gate 必备）；之后 `/story-readiness scene-management/story-001..005`（如有新 stories 框架立完）→ `/dev-story` 实施起步。

---

## ADR-029 V3 Watch List（Sprint 4 起监控）

| 触发条件 | Sprint 3 baseline | Sprint 4 监控 |
|---------|-------------------|---------------|
| Type-1 drift > 5 min | 各 story Phase 1.5 ≤ 15 min（含 Type-4 propagation）| 跟踪 |
| 新 drift type 出现（非 Type-1/2/3/4）| 0 | 跟踪 |
| Type-2 (c) framework behavior frequency > 2/sprint | 1 (S3-03 v3) | 跟踪 |
| Lite propagation v2 ROI < 50% | 73%-85% (3 次实测) | 跟踪 |
| Multi-spike sequential 后仍 race | 0（S3-04 起单 spike 模式 + 注释关闭其他 spike）| 跟踪 |

任一命中 → 立 V3 起草 candidate；Sprint 末 retro 集中评估。
