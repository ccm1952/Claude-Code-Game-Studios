// 该文件由Cursor 自动生成

# ADR-030: Project Workflow Decision — Foundation/Core-First, VS-Late Pattern

## Status

**Accepted** — 2026-05-06 (Sprint 4 S4-04；Sprint 3 retro Process Improvement #2 落地)

## Date

2026-05-06

## Last Verified

2026-05-06

## Decision Makers

Technical Director, Producer, Lead Programmer

## Summary

本项目工作流采用 **Foundation/Core-First, VS-Late Pattern** —— 与典型 game-dev gate model 的 prototype-first / VS-early 顺序不同：先深耕 Foundation/Core 层 architecture + governance + system implementation（Sprint 1-3 完成 Foundation + Core 部分系统），再在 Sprint 5+ 实体构建 Vertical Slice (chapter 1 端到端)。本 ADR 显式 acknowledge 该 stage drift（`production/stage.txt` 仍 `Pre-Production` 但工作模式已是 Production-class），定义 Sprint 5-6 VS Build commitment + risk mitigation，并调整 `/gate-check pre-production` 重跑路径。

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.62f2 LTS — 实际为引擎无关 process-level decision |
| **Domain** | Process Governance / Project Workflow |
| **Knowledge Risk** | NONE — 基于 Sprint 1-3 实战 + Sprint 3 retro Process Improvement #2 + 2026-05-06 `/gate-check pre-production` verdict FAIL 数据 |
| **References Consulted** | `docs/architecture/architecture-review-2026-05-06.md` (review verdict CONCERNS;3 blockers fixed)；`production/sprints/sprint-3-retrospective.md` Process Improvement #2 ("Stage drift 是 architecture/governance heavy 项目的常态，不是缺陷")；`production/stage.txt` (Pre-Production)；`/gate-check pre-production` 2026-05-06 verdict FAIL data |
| **Post-Cutoff APIs Used** | None — process-level decision |
| **Verification Required** | Sprint 5-6 末跑 `/gate-check pre-production` 重跑预期 PASS（VS playable + ≥3 playtest sessions + `production/playtests/` reports） |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None (process-level decision; orthogonal to all engine/system ADRs) |
| **Updates** | ADR-029 V2.0 §V2-7 (Sprint 4-6 V3 watch list 增加 VS-skip path tracking) |
| **Enables** | (1) Sprint 5-6 VS Build commitment formal commitment；(2) `/gate-check` 在 VS-late pattern 项目下的应用调整；(3) Future stage drift 显式决策 reference |
| **Blocks** | `/gate-check pre-production` 重跑 PASS 必须等 Sprint 5-6 VS build + ≥3 playtest sessions 完成 |
| **Ordering Note** | 与 Sprint 4-6 sprint plans 同步生效 |

## Context

### Problem Statement

`/gate-check pre-production` (Pre-Production → Production gate) 在 2026-05-06 fresh session 跑 verdict **FAIL** —— 主要 blockers 是：

1. **No prototypes/** 目录 — 项目跳过 prototype 阶段
2. **No playtests/** 目录 — VS 未构建无法跑 playtest
3. **Vertical Slice playable build 不存在** — Sprint 1-3 完成的是 Foundation + Core 系统层 stories（input gestures / save / object interaction / scene management），无 end-to-end VS playable
4. Vertical Slice Validation 4 项 3 项 FAIL — 因 VS 不存在；per skill 规则 任一 FAIL = automatic FAIL

但与此同时：
- 21 ADRs Accepted（governance maturity）
- ADR-029 V1.0 → V2.0 升级（6 数据点 + 7 V2 candidates 全部触发）
- Lite propagation v2 ROI 73%-85%（3 次实战）
- Sprint 1-3 累计 13 + 13 + 4 = 30 stories DONE（含 Foundation 全 epic + Core 部分 epic）
- Track A Sprint 4 已完成 3 P1 ADR impl expand (Puzzle / Narrative / Audio)
- Sprint 5 VS Build chapter 1 全部 P1 ADR 依赖 framework ready

**核心矛盾**：`/gate-check pre-production` 的标准 game-dev gate model 假设是 prototype-first / VS-early 顺序（Pre-Production 阶段做 VS validation 后才能进 Production 实施 sprints），但本项目是 governance/Foundation 优先 → impl sprints 不断 → VS 后置（spike-driven verification 替代 prototype + 实施 sprints 验证 governance + VS 在 Foundation/Core ready 后再构建）。

**Sprint 3 retro Process Improvement #2 已捕捉**：
> "Stage drift 是 architecture / governance heavy 项目的常态，不是缺陷：项目 'governance + Foundation/Core 优先 → VS 后置' 的工作模式与典型 game-dev gate model 不对齐。Sprint 4 起做 VS-skip path ADR 显式 acknowledge。**Lesson**：skill protocols 是参考不是真理；项目可以基于自身 risk profile 做不同的 stage 顺序。"

### Constraints

- **不破坏 ADR-029 governance framework**：本 ADR 是补充而非替换；ADR-029 R1/R2/R3 readiness gate + V2.0 §V2-3 R3 mandatory 仍是核心 governance 路径
- **不修改 `/gate-check` skill**：skill 是参考；不强制项目 fork skill。本 ADR 是 project-level 决策 layer，跑 `/gate-check pre-production` 时显式 acknowledge FAIL 是 expected 而非 blocking
- **不放弃 VS 建设**：Sprint 5-6 必须实体构建 chapter 1 VS playable build + ≥3 playtest sessions
- **保持 stage.txt 真实性**：Sprint 5-6 VS 完成后调整 stage 字段反映真实状态
- **保持 critical hands-on validation**：Sprint 5-6 VS build 前的 ADR + Story framework 必须经过完整 ADR-029 V2.0 R1/R2/R3 readiness gate 验证

### Requirements

- 显式 acknowledge stage drift；提供 future contributors / reviewers 一份明确 reference 解释为什么 stage.txt: Pre-Production 但实际工作模式是 Production-class
- 定义 Sprint 5-6 VS Build commitment：chapter slice 选择 + scope + playtest commitment
- 定义 risk mitigation：VS 后置可能引入的 risks + 已 instrumented 的 mitigation
- 定义 `/gate-check pre-production` 重跑路径：当 VS playable + playtests done 后重跑 expected PASS

## Decision

**Adopt Foundation/Core-First, VS-Late Pattern as official project workflow; Sprint 5-6 commit to building chapter 1 VS playable + 3+ internal playtest sessions; `/gate-check pre-production` rerun expected PASS post Sprint 6.**

### Workflow Pattern Definition

```
Standard game-dev gate model (PROTOTYPE-FIRST / VS-EARLY):
  Concept → Systems Design → Technical Setup → [PROTOTYPE-FIRST]
    └── Build prototypes/ ASAP → Pre-Production VS Build → Pre-Production VALIDATE → Production sprints

Project actual workflow (FOUNDATION/CORE-FIRST / VS-LATE):
  Concept → Systems Design → Technical Setup → [SPIKE-DRIVEN VERIFICATION]
    └── SP-001..SP-011 spike validation (替代 prototype) → Sprint 1-3 Foundation/Core impl + governance
        └── Sprint 4 P1 ADR impl expand (Track A) + carryover debt 清理
            └── Sprint 5-6 VS Build chapter 1 + ≥3 playtest sessions
                └── /gate-check pre-production 重跑 expected PASS → Production stage formal advance
```

### Sprint 5-6 VS Build Commitment

| Item | Sprint | Owner | 验收 |
|------|:------:|-------|------|
| Chapter 1 (`外升孔`) end-to-end VS slice 实体构建 | Sprint 5 | Lead Programmer + GameDesigner | scene load → puzzle interaction → shadow match → narrative beat → chapter transition end-to-end 可玩 |
| Sprint 4 Track A 三系统 (Puzzle / Narrative / Audio) production code 实施 | Sprint 5 | Lead Programmer | per Sprint 4 framework stories (S4-01/-02/-03 story-001 各)；R3 PlayMode probe 全 PASS |
| ≥3 internal playtest sessions | Sprint 5-6 | Producer + GameDesigner | sessions documented in `production/playtests/`；Core loop fun validated |
| Playtest report | Sprint 6 | Producer | `production/playtests/playtest-vs-chapter-1-2026-XX-XX.md`；含 player journey 反馈 + adjustment recommendations |
| `/gate-check pre-production` 重跑 | Sprint 6 末 | Tech Director | verdict expected PASS；如仍 FAIL → 加 Sprint 7 buffer |
| `production/stage.txt` 调整 | Sprint 6 末 (post-PASS) | Tech Director | Pre-Production → Production formal advance |

### Stage.txt Adjustment Strategy

**Current**: `production/stage.txt` = `Pre-Production`
**Adjustment** (post Sprint 6 VS Build + gate-check PASS): `Pre-Production` → `Production`

注：Sprint 5 中途**保持** `Pre-Production`（VS-late pattern 显式 acknowledge）；不在 Sprint 5 中途升级 stage.txt 以避免 "Production stage 但无 VS validation" 的不一致状态。

### Risk Mitigation Inventory (现有 + 本 ADR 新增)

| Risk | Mitigation 状态 | Sprint 4-6 监控 |
|------|----------------|------------------|
| **VS-late pattern 导致核心循环 fun 未验证累积投入大** | ADR-029 V2.0 R3 PlayMode probe + governance ROI = ×2 unsafe deploys avoided (S3-02 R3 + S3-03 R3)；spike-driven 验证替代 prototype | Sprint 5 第一 playtest session 立即跑 |
| **Foundation/Core impl 与未验证的 fun loop 假设 misaligned** | ADR-014/-016/-017 v1 起草是基于 GDD 完整设计；Sprint 5 VS 后及时 retro 调整 | Sprint 5 retro action items |
| **`/gate-check pre-production` 持续 FAIL 阻塞 Sprint 4-5 推进** | 本 ADR 显式 acknowledge FAIL 是 expected；不阻塞 Sprint 4-5 工作；Sprint 6 重跑前才作正式 gate | Sprint 6 末 gate-check rerun |
| **Sprint 6 VS build 实际比 estimated 慢** | Sprint 4-5 plans 含 buffer + Track A pattern velocity 验证 25 min/story；Sprint 6 plan 时基于 Sprint 5 实际数据 calibrate | Sprint 6 plan 时 estimate review |
| **Stage drift 长期化导致团队混乱** | 本 ADR 明确 Sprint 6 末 stage.txt 调整 commitment；超过 Sprint 7 仍未 PASS → 触发深层项目检讨 | 半 sprint 监控 |

## Alternatives Considered

### Alternative 1: Strict adherence to standard game-dev gate model

- **Description**: 暂停所有 Foundation/Core 实施，先在 Sprint 4 实体构建 chapter 1 VS prototype 作为 standard Pre-Production 阶段标志，然后才继续 Foundation/Core impl
- **Pros**: 与典型 gate model 完全对齐；early VS validation 降低 fun loop 风险
- **Cons**: 浪费 Sprint 1-3 累积的 Foundation/Core 投入（~30 stories）；prototype + production code 双重维护成本；governance maturity（ADR-029 V2.0 + ADR-027 supersession 等）需等 prototype 完成才能继续
- **Rejection Reason**: 项目已经在 Sprint 3 节点深耕 governance + Foundation/Core layer；切回 prototype-first 等于丢弃实战已验证的工作模式。spike-driven verification + ADR-029 V2.0 governance 在 R3 mandatory 下可视为 "verification-equivalent of prototype" — 本质区别是 spike validates **technical correctness**，prototype validates **fun loop**。fun loop 验证留 Sprint 5-6 VS Build 阶段集中做，而非 sprint 1 就启动

### Alternative 2: Skip VS Build entirely; treat Foundation/Core as production directly

- **Description**: 不在 Sprint 5-6 构建 chapter 1 VS；直接在 Sprint 5-6 开始所有 chapters 1-5 的 implementation；不专门做 playtest 阶段
- **Pros**: 更快进入 full Production；无 VS overhead
- **Cons**: 无 fun loop validation 直接进 5 chapter 实施 = 极高风险；如 chapter 1 fun 不及格则 chapters 2-5 都需返工；GDC 经验数据：跳过 VS 是 #1 production failure cause（per `/gate-check skill` quote）
- **Rejection Reason**: 不接受。VS Build 是必要的 fun loop validation 阶段，不能完全跳过。本 ADR 仅是把 VS 时机从 Pre-Production 阶段推到 Sprint 5-6（后期 Pre-Production / 早期 Production overlap）

### Alternative 3: Continue current pattern without formal ADR

- **Description**: 不写本 ADR；继续 Sprint 4-5 推进；让 stage drift 隐式存在
- **Pros**: 节省 ~30 min ADR 起草时间
- **Cons**: future contributors / reviewer (含 LLM agent) 看 stage.txt = Pre-Production + `/gate-check pre-production` FAIL 会困惑；governance 缺一环；Sprint 3 retro Process Improvement #2 落地不彻底
- **Rejection Reason**: governance maturity 已是项目核心特征（ADR-029 V2.0 等）；隐式决策与项目风格不一致。本 ADR 显式化 + reference 文档化 + future stage adjust commitment = 治理一致性

## Consequences

### Positive

- **明确 Sprint 5-6 VS Build commitment** — 避免 VS 持续推后 sprint
- **`/gate-check` skill 重跑路径明确** — Sprint 6 末预期 PASS，stage.txt 同步升 Production
- **Stage drift 显式化** — future contributors / LLM agent 看本 ADR 可立即理解 project workflow rationale
- **Sprint 3 retro Process Improvement #2 落地** — recurring carryover smell 终结的 governance 一致性
- **Risk mitigation inventory 完整** — 5 risks 全部已映射到 mitigation + 监控触发条件
- **保持 ADR-029 V2.0 governance 框架** — VS-late pattern 是 process layer decision，不影响 R1/R2/R3 + R3 mandatory 等核心 gate

### Negative

- **VS fun loop validation 推到 Sprint 5-6** — 如 fun loop 假设错误，Sprint 1-4 累积 ~34 stories 部分需返工。已有 mitigation：Sprint 5 第一 playtest session 立即跑 + Sprint 5 retro 调整
- **`/gate-check pre-production` 在 Sprint 4-5 持续 FAIL** — 这是 expected verdict 但 future LLM agent 可能误判为 critical blocker。已有 mitigation：本 ADR + active.md Session 22 显式 acknowledge
- **Stage.txt 与 sprint plan 实际工作不对齐** — 跑 Production-class sprints 但 stage 仍 Pre-Production；已 mitigation：Sprint 6 末 stage 升 Production 一次性同步

### Risks

| Risk | Probability | Impact | Mitigation |
|------|:-----------:|:------:|------------|
| Sprint 5 chapter 1 VS build 实际超 estimated 投入 (e.g., > 30 SP) | Medium | High | Sprint 5 plan 含 buffer ≥ 25%；如超 → Sprint 6 加 buffer or 减 Should/Nice scope |
| Sprint 5 第一 playtest session 暴露 core loop fun 不及格 | Medium | High | 立即 Sprint 5 retro action items；Sprint 6 调整 GDD + ADR + impl；不接受推到 Sprint 7+ |
| Sprint 6 末 `/gate-check pre-production` 重跑仍 FAIL | Low | Medium | 加 Sprint 7 buffer；触发深层检讨；可能修订本 ADR 或起 V2 |
| 本 ADR 决策被 future contributor 误读为 "可永久 skip VS" | Low | High | 本 ADR §"Decision" + §"Sprint 5-6 VS Build Commitment" 明确写 "build VS in Sprint 5-6"；不允许永久 skip |
| Future game-dev gate skill 升级版与本 ADR 决策冲突 | Low | Medium | 本 ADR 是 project-level decision；skill 是参考；如冲突优先本 ADR；按需 V2 修订 |

## Performance Implications

- **CPU/Memory**: None — process-level decision，零运行时成本
- **Process Cost**:
  - 本 ADR 起草 ~30 min（一次性）
  - Sprint 6 末 `/gate-check pre-production` 重跑 ~10-15 min
  - stage.txt 调整 ~1 min
  - Sprint 5-6 VS build commitment：估 ~25-40 SP 总投入（依 chapter 1 复杂度）
- **Net savings**: 避免 Sprint 1-4 阻塞在 prototype-first 标准路径 = 节省 ~50-80 SP（基于 Sprint 1-3 实际投入）

## Migration Plan

### Sprint 4 (现状 — 2026-05-06)

1. **本 ADR 写入 + Accepted** ✅
2. **Update active.md Session 22** — 文档化本 ADR 落地 (continued #8)
3. **Update sprint-4.md** — S4-04 status: ready-for-dev → done
4. **Update sprint-status.yaml** — S4-04 closure summary
5. **Update ADR-029 V2.0 §V2-7 V3 watch list** — 加 VS-skip path tracking 触发条件（Sprint 6 末 gate-check rerun PASS rate）

### Sprint 5 (2026-05-XX)

1. Sprint 5 plan 起草时 explicit reference 本 ADR 的 VS Build commitment
2. Track A 实际实施 (S4-01/-02/-03 story-001 production code + tests)
3. chapter 1 (`外升孔`) VS slice 实体构建 — scene load → puzzle → shadow match → narrative → audio → chapter transition
4. 第一 playtest session 立即跑（不等 build "完美"）
5. Sprint 5 retro 反馈 fun loop validation 数据

### Sprint 6 (2026-06-XX)

1. 完成 ≥3 playtest sessions
2. Playtest report 写入 `production/playtests/playtest-vs-chapter-1-2026-06-XX.md`
3. `/gate-check pre-production` 重跑 — 预期 PASS
4. `production/stage.txt`: Pre-Production → Production formal advance
5. Sprint 6 retro action items 含 stage advance + 后续 Production sprint 规划

### Sprint 7+ (post-VS / Production)

1. 按 standard Production phase 推进 chapters 2-5 实施
2. 标准 `/sprint-plan` + `/qa-plan` + `/team-qa` Production gate flow

## Validation Criteria

- [x] 本 ADR Accepted 2026-05-06 ✅
- [ ] Sprint 5 plan 引用本 ADR Sprint 5 VS Build commitment
- [ ] Sprint 5 末完成 chapter 1 VS playable build
- [ ] Sprint 5 末完成 ≥1 playtest session
- [ ] Sprint 6 末完成 ≥3 playtest sessions cumulative
- [ ] Sprint 6 末 `/gate-check pre-production` 重跑 verdict PASS
- [ ] Sprint 6 末 `production/stage.txt`: Pre-Production → Production
- [ ] ADR-029 V2.0 §V2-7 V3 watch list 加 VS-skip path tracking entry

## GDD Requirements Addressed

| GDD Document | Requirement | How This ADR Addresses It |
|-------------|-------------|--------------------------|
| N/A — process-level decision | N/A | 本 ADR 不直接对应 GDD requirement；它解决项目工作模式与典型 game-dev gate model 的 alignment 问题 |

间接受益：所有 GDD（13 systems）都通过本 ADR 的 VS-late pattern 受益 — Foundation/Core 系统先实施 + governance 验证后，VS Build 时再用真实施的 systems 跑端到端 fun loop validation，比 prototype-only validation 更接近 production reality。

## Related Decisions

- **References**: ADR-029 V2.0 §V2-7 V3 Watch List (本 ADR 加新 trigger condition: VS-skip path Sprint 6 gate rerun PASS rate)
- **References**: `production/sprints/sprint-3-retrospective.md` Process Improvement #2
- **References**: `docs/architecture/architecture-review-2026-05-06.md` (B-1/B-2/B-3 全 resolved)
- **References**: `production/sprints/sprint-4.md` (Sprint 4 plan Track A/B/C 三轨架构)
- **Updates**: ADR-029 V2.0 §V2-7 (加 VS-skip path V3 watch entry)

## History

- **2026-04-30**: Sprint 3 retro Process Improvement #2 首次明确"Stage drift 是 architecture/governance heavy 项目的常态"
- **2026-05-06 morning**: `/gate-check pre-production` 跑出 verdict FAIL；Sprint 4 plan 列入 S4-04 VS-skip path ADR 起草任务
- **2026-05-06 afternoon**: 本 ADR (ADR-030) 起草 + Accepted；Sprint 4 Must Have 5/5 ✅
