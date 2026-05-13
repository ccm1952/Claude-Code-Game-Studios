<!-- 该文件由 Cursor 自动生成 -->

# Playtest VS Chapter 1 — Session 1

> **Story**: S6-01 (Sprint 6 Track A Must Have, 2 SP)
> **ADR-030 §VS Build commit**: 第 1/3 次 playtest session
> **Drafted**: 2026-05-13 (Session 31)
> **Played**: TBD（待 Phase 2 manual playtest 实际填）
> **Player**: chen (solo developer)
> **Observer**: Claude Code (实时反馈记录辅助)

---

## §0 概要

chapter 1 第 1 次 internal playtest — Sprint 5 retro AI-4 promote Must Have，**ADR-030 §VS Build commitment 第 1/3 次 playtest** 完成项。

**Status flow**: backlog → phase-1-prep-done (本 file 创建) → ⏳ Phase 2 待 user fresh Unity boot → §1..§8 完整填空 → phase-2-played-done → Phase 3 emergent fixes 评估 → done。

**前置依赖（全 ✅ done 2026-05-13）**:
- S5-02 ✅ Chapter 1 5 系统 happy path (5/5 R3 + 36/36 asserts)
- S5-04 ✅ Art bible v1.1 Accepted (VS art gate)
- S6-04 ✅ Chapter 1 error/restart path (5/5 R3 + 37/37 asserts)
- S6-07 ✅ ui-system-006 main menu UIWindow polish
- S6-08 ✅ ui-system-008 popup queue + auto inputblocker

**Acceptance criteria** (per sprint-status.yaml S6-01 entry):
- [ ] playtest session **≥30 min** continuous
- [ ] reuse session 从 **splash → main menu → chapter 1 完整走通**（fresh Unity boot，1 个 session 内完成全 flow）
- [ ] 记录 **player journey 反馈**：fun loop / pacing / clarity / frustration points
- [ ] evidence 写入本 file §1..§8 完整
- [ ] emergent issues 分类（S0/S1/S2/S3）
- [ ] R3 PlayMode probe **N/A**（manual playtest，无 spike）

---

## §1 Playtest Setup

| 项目 | 值 |
|---|---|
| Unity Editor 版本 | 2022.3.62f2 LTS |
| Build / Scene | `Assets/AssetRaw/Scenes/Chapter_01_Approach.unity` |
| Session reuse 路径 | ✅ fresh Unity boot → Play in Editor → splash → main menu → chapter 1 |
| Player | chen (solo developer) |
| Observer | Claude Code (cursor session 实时记录) |
| Pre-conditions verified | S5-02 ✅ + S5-04 ✅ + S6-04 ✅ + S6-07 ✅ + S6-08 ✅ |
| Player 进入心态 | first-time player（自身 dev 但模拟"忘记设计"的 playtest 心智）|
| Unity Editor state | fresh boot（必须；S6-04 R3 PlayMode 完跑后请关掉 Editor 再重开，避免残留 state 污染 "reuse session" 语义）|
| Duration target | ≥30 min continuous（间隔 ≤ 5 min 算同一 session）|
| 录屏 / 录音 | 可选（如需事后复审 emergent issue） |

**Playtest 起步 checklist**（Phase 2 启动前 user 执行）:
1. [ ] Unity Editor **关掉再 fresh 重开**（reuse session 语义保证）
2. [ ] 确认当前 Build Settings First scene = `Chapter_01_Approach.unity` **或** main menu scene（按 production boot pipeline 起步）
3. [ ] 关掉所有 Project window / Hierarchy / Console（避免 dev tool 干扰沉浸）
4. [ ] Open Console window 仅留 Error 级别可见（subscribe error 用，不主动看）
5. [ ] 计时器开（手机 / 桌面计时 app）
6. [ ] Notes pad 开（playtest 后复盘填本 file §2.1..§2.5）

---

## §2 Player Journey 详细 log（5 阶段）

### §2.1 splash → main menu（target 5 min）

**Expected flow**:
- Unity Editor Play → splash 显示 → fade → main menu UIWindow 出现 → 4 button (NewGame / Continue / Settings / Quit) 可见可点

**Observation points**:
- [ ] splash 启动延迟（秒）：TBD
- [ ] splash 视觉表现（logo / 加载提示 / 静默）：TBD
- [ ] splash → main menu transition 流畅度（卡顿 / 跳变 / fade）：TBD
- [ ] main menu BGM 启动观察（淡入 / 立刻 / 静默）：TBD
- [ ] 4 button 视觉一致性（per ui-system-006 polish）：TBD
- [ ] hover / click feedback（per S6-07 main menu polish）：TBD
- [ ] Continue button 状态（如无存档应 disabled）：TBD

**Player notes**: [TBD — 填 player 第一反应 / 不解 / 满意点]

---

### §2.2 main menu → chapter 1 begin（target 1 min）

**Expected flow**:
- NewGame click → 弹出确认 popup（如有）→ confirm → scene transition (fade-out main menu → fade-in chapter 1) → chapter 1 loaded → player 看到 chapter 1 入场画面

**Observation points**:
- [ ] NewGame click 响应延迟：TBD
- [ ] popup queue 行为（per S6-08 auto inputblocker）：TBD（如有 confirm popup）
- [ ] scene transition smoothness（per ADR-009 11 步流程）：TBD
- [ ] loading 时长（asset load + scene activate）：TBD
- [ ] chapter 1 入场 narrative trigger 是否启动（per S5-05 NarrativeSequence 探路）：TBD
- [ ] **进入 chapter 后第一个 5 秒** — player 是否立刻明白要做什么？还是茫然？：TBD

**Player notes**: [TBD — 关键 "first 5 sec orientation" 反馈是 fun loop 核心 indicator]

---

### §2.3 chapter 1 fun loop（target 20+ min — playtest 核心）

**Expected flow**: 5 systems 串通 happy path：
- Object_01_CoffeeMug + Object_02_Book 2 个 interactive object 探索
- shadow match mechanic 触发（投影墙面 + 物件遮挡）
- perfect match → puzzle state Idle → Active → MatchInProgress → Complete 流转
- narrative sequence trigger（atomic effect audio_duck + screen_fade + wait + 字幕 / dialogue）
- audio BGM duck during narrative + recover after

**Observation points** —— **此区是 fun loop 评估核心**:

#### §2.3.1 物件探索阶段

- [ ] 看到 Object_01_CoffeeMug 反应（attention / 兴趣 / 困惑）：TBD
- [ ] 互动尝试（click / drag / approach）：TBD
- [ ] 第一次互动 → reaction（hover highlight / click feedback / sound effect）：TBD
- [ ] 找到 Object_02_Book 难度（视觉提示是否够 / clue 是否清晰）：TBD
- [ ] 物件互动 affordance 是否清晰（看一眼知道 "这是个 puzzle clue"）：TBD

#### §2.3.2 shadow match mechanic

- [ ] shadow projection 投影墙面 visual 表现：TBD
- [ ] match 触发条件（角度 / 距离 / 物件类型）是否直觉：TBD
- [ ] **第一次 perfect match** 体验（瞬间反馈 / 庆祝感 / 还是平淡？）：TBD
- [ ] near match 反馈是否清晰（提示再调整 vs 完全不响应）：TBD
- [ ] 失败 / 错误 match 反馈：TBD

#### §2.3.3 puzzle state 流转

- [ ] Active → MatchInProgress 过渡 player 是否感知：TBD
- [ ] state lock 阶段 player 行为（继续操作被 reject / 主动等待）：TBD
- [ ] Complete state 反馈（庆祝 / 进度 / 静默）：TBD

#### §2.3.4 narrative sequence + audio duck

- [ ] narrative trigger 触发时机（intrusive / 自然 / 中断 fun）：TBD
- [ ] audio_duck 体验（BGM 下沉是否够明显 / 过头 / 不够）：TBD
- [ ] 字幕 / dialogue 节奏（per S5-05 NarrativeSequence 探路）：TBD
- [ ] narrative 结束后 audio recover 顺畅度：TBD

#### §2.3.5 5 systems 串通 happy path **整体感觉**

- [ ] 5 system 串通是否流畅（per S5-02 mechanical 5/5 R3 PASS 已 verify；这里 **验 fun**）：TBD
- [ ] 中途是否有 "我在做什么 / 接下来做什么" 迷茫：TBD
- [ ] **"想再玩一遍"** 意愿（关键 fun loop indicator）：TBD

**Player notes**: [TBD — 此区填充时考虑写成 narrative 段落而非纯 bullet，便于事后复审情绪曲线]

---

### §2.4 chapter 1 end（target 5 min）

**Expected flow**:
- chapter 1 完成判定触发（puzzle Complete + narrative wrapup）
- outro UI / 反馈 / next chapter 选项

**Observation points**:
- [ ] chapter 1 完成判定时机（合适 / 早 / 迟）：TBD
- [ ] outro 表现（UI / cutscene / 直接 return）：TBD
- [ ] return to main menu vs next chapter 选择 affordance：TBD
- [ ] **完成 chapter 时 player 情绪**（满足 / 累 / 一般 / 想继续）：TBD

**Player notes**: [TBD]

---

### §2.5 Robustness manual 测试（target 2-5 min）

**目的**: S6-04 R3 自动验过 5/5 case，但 manual UX 体验是另一回事。本区验 player 误操作 / 主动测试边界。

**Expected scenarios**:
- 退出 chapter 中途（back button / quit）
- restart from main menu after chapter 1 done
- chapter 中途 alt+tab 切走再回来（Unity Editor 内可能不真实，记可观察项）

**Observation points**:
- [ ] back / quit 行为是否 graceful（S6-08 popup auto inputblocker 保护）：TBD
- [ ] restart from main menu 是否干净（second-play 不残留 state）：TBD
- [ ] 任何 unexpected error 弹出（应 0，per S6-04 5/5 R3 PASS）：TBD

**Player notes**: [TBD]

---

## §3 Fun Loop Assessment（per Sprint 5 retro AI-4 promote criteria）

> 全程 ≥30 min 后填，1-5 评分 + notes。

| 维度 | 评分 1-5 | Notes / 触发场景 |
|---|---|---|
| **Fun moment 出现频次** | TBD | TBD（≥3 次 = 4 分 / ≥5 次 = 5 分） |
| **Pacing 流畅度**（慢 1 / 卡 2 / 平 3 / 流畅 4 / 紧凑 5） | TBD | TBD |
| **Clarity**（无需 dev 协助独立完成度；卡死 1 / 提示后继续 2 / 自摸索 3 / 顺畅 4 / 一气呵成 5） | TBD | TBD |
| **Frustration points**（卡住 / 不解 / 重复尝试次数；多 1 / 中 3 / 无 5）| TBD | TBD |
| **"想再玩一遍" 意愿**（关键 indicator）| TBD | TBD |
| **整体 fun loop 健康度**（≥ 3.5 平均 = PASS playtest 1，可进 playtest 2；< 3.5 = NEEDS-WORK emergent fixes 先做） | TBD avg | TBD |

**Verdict** (待 §3 评分填后判定):
- ⏳ TBD — 评 PASS / NEEDS-WORK / FAIL

---

## §4 Emergent Issues（分级 + Sprint 安排）

> 按严重度 S0/S1/S2/S3 分类；每条 issue 注明触发场景 / 期望 vs 实际 / Sprint 安排 / 关联 dp candidate（如有）。

### §4.1 S0 Critical（block playtest 2 进行）

> Expected: 0（S5-02 + S6-04 5/5 R3 已 verify mechanical robustness）

- [ ] TBD（如有，必 Sprint 6 mid 立 emergent fix story）

### §4.2 S1 High（影响 fun loop, 必须 Sprint 6 内 fix）

> 典型: fun loop 阻塞 / clarity 0 / dominant frustration point

- [ ] TBD

### §4.3 S2 Medium（影响 polish, Sprint 6 或 Sprint 7）

> 典型: visual / audio polish / 局部 UX 细节

- [ ] TBD

### §4.4 S3 Low（Sprint 7+ polish phase）

> 典型: nice-to-have / 单次出现 / 个人偏好

- [ ] TBD

### §4.5 dp candidate（governance / process drift, 不是 game bug）

> 典型: ADR-029 V3 / V3.1 trigger 信号 / framework boundary 新观察 / template gap

- [ ] TBD

---

## §5 ADR-030 §VS Build commit 6 项进度验证

| # | 项 | Status | Reference |
|---|---|---|---|
| 1 | chapter 1 build complete | ✅ | S5-01 + S5-1b + S5-1c + S5-02 (5 systems happy path) |
| 2 | Track A production code (5 systems) | ✅ | S5-02 5/5 R3 + 36/36 asserts (2026-05-12) |
| 3 | **≥3 internal playtest sessions** | ⏳ **1/3 done after 本 session 完成** | 本 file (S6-01) / S6-02 / S6-03 |
| 4 | playtest report 综合 | ⏳ Sprint 6 末 | S6-03 末完成 |
| 5 | `/gate-check pre-production` 重跑 | ⏳ Sprint 6 末 | S6-10 |
| 6 | `production/stage.txt` advance Pre-Production → Production | ⏳ Sprint 6 末 post-S6-10 PASS | S6-10 |

---

## §6 Sprint 6 后续影响 / Recommendations

### §6.1 S6-09 art asset 升级时机决策（per [A] AI-6 playtest 后升级）

> 决策依据: playtest 1 反馈是否表明 art quality 是 fun loop 阻碍。

- [ ] **[A1] playtest 1 反馈 art quality 阻碍** → S6-09 必须 playtest 2 前升级（与 art-director 并轨）
- [ ] **[A2] playtest 1 反馈集中 fun loop tuning** → S6-09 延后 playtest 3 后 / Sprint 7（chapter 1 art 用 placeholder 继续 playtest 2/3）
- [ ] **[A3] art-director 协作慢** → S6-09 fallback Sprint 7（per sprint-6.md row 232 risk row）

**Decision**: TBD（依 §3 + §4 反馈 + art-director 排期评估）

### §6.2 Emergent fixes 分配（Sprint 6 内 vs Sprint 7）

- §4.1 S0 → 必 Sprint 6 mid 加 emergent fix story
- §4.2 S1 → Sprint 6 内 fits（playtest 1 ↔ playtest 2 之间）
- §4.3 S2 → Sprint 6 末 fits 或 Sprint 7
- §4.4 S3 → Sprint 7+ polish phase backlog

### §6.3 Playtest 2 起手前置（per S6-02 entry）

- emergent fixes from §4.1 + §4.2 完成（or 接受 known issue）
- S6-09 art asset 状态明确（升 / 不升）
- 同一 Cursor session 内还是 fresh Unity boot？（per S6-02 reuse session 语义）

### §6.4 Sprint 6 retro 待加议题（如本 playtest 触发）

- [ ] 累积 4 已知议题: V3.0.1 dp8 candidate / /vertical-slice vs S6-10 sequencing / dp13 candidate Director gates vs ADR-029 V3 coexistence / ≤5 hr hard rule 2 次 override 反思
- [ ] 本 playtest 1 是否新触发议题: TBD

---

## §7 References

### 7.1 前置依赖 Story Evidence

- **S5-02 chapter 1 5 systems happy path**: `production/qa/playmode-end-to-end-flow-2026-05-12.md` (5/5 R3 + 36/36 asserts)
- **S6-04 error/restart**: `production/qa/playmode-error-restart-path-2026-05-13.md` (5/5 R3 + 37/37 asserts + 1867ms < 8s)
- **S6-07 main menu polish**: `production/qa/playmode-main-menu-polish-2026-05-13.md` (or wherever S6-07 evidence resides)
- **S6-08 popup auto inputblocker**: `production/qa/playmode-popup-auto-blocker-2026-05-13.md`
- **S5-04 art bible sign-off**: `design/art-bible.md` v1.1 Accepted

### 7.2 Governance / ADR

- **ADR-030 §VS Build commitment**: Sprint 5-6 双 sprint final（本 file 是第 1/3 playtest）
- **ADR-029 V3**: governance protocol (本 file 不走 V3 R1/R2/R3, manual playtest 类 N/A — sprint-status.yaml entry notes)
- **Sprint 5 retro AI-4**: S5-07 → S6-01/-02/-03 promote Must Have
- **Sprint 6 plan**: `production/sprints/sprint-6.md` rows 86-88 (S6-01/-02/-03 spec)

### 7.3 Asset / Scene

- Chapter 1 scene: `Assets/AssetRaw/Scenes/Chapter_01_Approach.unity` (29.5KB, last modify 2026-05-09)
- Object_01_CoffeeMug, Object_02_Book (per S5-01 scene 实施)
- Art bible v1.1 standard (per S5-04)

---

## §8 Sign-off

| 项 | 决定 |
|---|---|
| **Playtest verdict** | TBD（待 Phase 2 manual playtest + §3 + §4 填后判定）|
| **PASS criteria** | §3 Fun Loop Assessment 整体均分 ≥ 3.5 + §4.1 S0 = 0 + §4.2 S1 ≤ 3 项 |
| **NEEDS-WORK criteria** | §3 均分 < 3.5 OR §4.1 S0 ≥ 1 OR §4.2 S1 ≥ 4 项 |
| **FAIL criteria** | §4.1 S0 阻塞 fun loop OR Sprint 6 capacity 不够 fix |
| **Next action** | TBD — emergent fixes prioritize + S6-02 起手时机 + S6-09 决策 |
| **Player sign-off** | chen TBD |
| **Observer sign-off** | Claude Code TBD |

---

## Appendix A — Playtest 进行时 Observer playbook（Claude Code 这边的辅助清单）

> 当 user 在 Unity Editor 内实际玩时，Cursor 这边我可以做的（非全部，按需 invoke）:

### A.1 实时提醒（user 可在玩中切回 Cursor 问）

- "现在 Object_01_CoffeeMug 应该看到了吗？" → 我 check S5-01 scene 实施细节回答
- "shadow match 期望从哪个角度触发？" → 我 grep PuzzleStateMachine + shadow projection logic
- "刚刚 audio duck 该是 0.3 还是 0.1？" → 我 grep AudioModule + S5-06 evidence

### A.2 Console error 实时分类

- user 玩时如 Console 弹 error → 截图丢给 Cursor → 我分类 critical / log noise / known issue

### A.3 反馈实时记录

- user 边玩边口述反馈 → 我代写入本 file §2.x player notes（user 不打断节奏）

### A.4 玩结束后整理

- user 玩完口述总反馈 → 我帮整理进 §3 评分 + §4 emergent issues 分级 + §6 recommendations
- 与 sprint-6.md / sprint-status.yaml / active.md 联动 update

---

## Status Log

- **2026-05-13 evening (Session 31)** — file 创建 phase-1-prep-done（本 §0..§8 完整模板就位待 Phase 2 fill）
- ⏳ **TBD Session 32+** — Phase 2 manual playtest ≥ 30 min + §1..§8 完整填空
- ⏳ TBD post-Phase 2 — closure (verdict + emergent fixes + Sprint 6 后续影响决策)
