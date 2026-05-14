<!-- 该文件由 Cursor 自动生成 -->

# Playtest VS Chapter 1 — Session 1

> **Story**: S6-01 (Sprint 6 Track A Must Have, 2 SP)
> **ADR-030 §VS Build commit**: 第 1/3 次 playtest session
> **Drafted**: 2026-05-13 (Session 31)
> **Played**: 2026-05-14 morning (Session 32) — Phase 2.1 manual playtest 启动 ~5 min 后撞 chapter 1 物件 production wiring gap 主动中止
> **Player**: chen (solo developer)
> **Observer**: Claude Code (实时反馈记录辅助)

---

## §0 概要

chapter 1 第 1 次 internal playtest — Sprint 5 retro AI-4 promote Must Have，**ADR-030 §VS Build commitment 第 1/3 次 playtest**。

**Status flow**: backlog → phase-1-prep-done → phase-2-0-infra-fix-done → ⚠️ **phase-2-1-played-needs-work-emergent-fix-pending** (本次落点 2026-05-14 Session 32) → 待 emergent fix epic story-004~008 完成 → phase-2-2-replay-pending → done。

**verdict**: ⚠️ **NEEDS-WORK** — fun loop 不可评估（chapter 1 物件 production wiring gap 5 处缺口阻塞 fun loop assessment）；但 governance value 巨大：surface **V3.0.1 dp15 candidate "EditMode green ≠ production wired sniff"**（详见 §4.5）+ 暴露 Sprint 2-5 五个 sprint 期间 ObjectInteraction system 整套 EditMode test pass + production 0 wiring 反模式。playtest 1 设计意图 = 揭露 Sprint 6 polish 优先级 → 设计意图达成（虽 fun loop 部分未达 ≥30 min）。

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

#### §2.3.1 物件探索阶段（target 10 min — 实际 0 min，fun loop 在此阶段撞墙）

- [x] 看到 Object_01_CoffeeMug 反应：visual 可见（场景 placeholder Cube primitive；S6-09 art asset 升级未做）
- [x] 互动尝试（click / drag / approach）：⚠️ **完全无响应** — user 原话："Object_01 无法交互，点击拖拽 没响应"
- [x] 第一次互动 → reaction：**0 反馈**（hover highlight / click feedback / sound effect 全 0）
- [x] 找到 Object_02_Book 难度：N/A（未到此阶段，fun loop 在 Object_01 互动失败时已主动中止）
- [x] 物件互动 affordance 是否清晰：N/A — 无任何交互链路存在 → fun loop 完全 broken

**Player notes**（user 原话 + observer 复盘）:
- "Object_01 无法交互，点击拖拽 没响应" — Phase 2.1 启动后 ~5 min 撞墙；尝试 mouse click + drag + 多次重试均 0 反馈。
- Observer root cause（grep + Session 27 #2 R2.3 + unity-mcp 实测交叉验证）: chapter 1 物件 production wiring **5 处缺口同时存在**（详见 §4.1 S0 issues 列表）。

**Observer 复盘 — root cause 5 项 production wiring gap**:

| # | Wiring 位 | 现状 | 检测命令 / 来源 |
|---|-----------|------|----------|
| 1 | Touch/Mouse → GestureRecognizer FSM → `GestureDispatcher.Dispatch(GestureData)` | ❌ **0 production caller**（仅 EditMode test fixture 1 处调用） | `rg 'GestureDispatcher\.Dispatch' --type cs` |
| 2 | `Chapter_01_Approach.unity` 挂 `InteractionCoordinator` GameObject + Inspector 拖入 _objects + Camera + Layer | ❌ | Session 27 #2 unity-mcp `manage_scene get_hierarchy parent=Interactables` 实测 |
| 3 | `Object_01_CoffeeMug` / `Object_02_Book` 挂 `InteractableObject` MonoBehaviour + `_puzzleId` + Interactable layer | ❌ componentTypes `[Transform, MeshFilter, BoxCollider, MeshRenderer]` 缺 InteractableObject | Session 27 #2 unity-mcp 实测 |
| 4 | `GameApp.Entrance` 调 `InteractableObject.RegisterPuzzleConfigProvider(...)` + `InteractionCoordinator.RegisterInputConfigProvider(...)` | ❌ GameApp.cs 0 hit | `rg 'RegisterPuzzleConfigProvider\|RegisterInputConfigProvider' GameApp.cs` |
| 5 | ADR-012 `ShadowMatchCalculator` listener 物件 transform → 阴影匹配 → `IPuzzleEvent.OnMatchScoreUpdated` fire | ❌（S5-02 spike P2 fire mock OnMatchScoreUpdated(1, 0.95) 简化路径绕过） | Session 27 #2 R2.3 RESOLVED 当时决策推迟到 Sprint 6 polish |

**关键 governance 发现**: Sprint 2 ObjectInteraction system 代码齐全（5 production class + EditMode test 全 pass），但 chapter 1 production wiring 整套 0 落地；S5-02 spike fire mock 简化路径让 R3 5/5 PASS 掩盖 wiring gap，**直到 S6-01 manual playtest 第一次穿透 production 层**才 surface。详见 §4.5 dp15 candidate。

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

> 实际 playtest 时长 ~5 min（撞 chapter 1 物件 production wiring gap 主动中止）；fun loop 部分整体不可评估。

| 维度 | 评分 1-5 | Notes / 触发场景 |
|---|---|---|
| **Fun moment 出现频次** | **不可评估** | 物件 0 交互 → 0 fun moment 触发；不属"分数低"，是"评估前提缺失" |
| **Pacing 流畅度** | **不可评估** | splash → main menu → chapter 1 进入流程 OK（~30 sec），但进 chapter 1 后 0 互动 → pacing 链路断 |
| **Clarity** | **不可评估** | 玩家不知"做什么"是因为**根本没法做任何事**（不是 clarity gap，是 mechanics 0 wired）|
| **Frustration points** | 1（max frustration） | "Object_01 无法交互，点击拖拽 没响应"反复尝试失败 → frustration 满值；但**不属于 game design 失误**，属 production wiring gap |
| **"想再玩一遍" 意愿** | **不可评估** | 没有"玩"过完整 fun loop，无意愿可评 |
| **整体 fun loop 健康度** | **不可评估 → NEEDS-WORK** | < 3.5 触发条件；emergent fix epic story-004~008 完成后必须 replay 重测 |

**Verdict**: ⚠️ **NEEDS-WORK** — fun loop 不可评估（不是评估出来 fail，是评估前提（chapter 1 物件可交互）不存在）。

**注释**：playtest 1 真正的 governance value 不在 §3 fun loop 评分，而在 §4.1 S0 + §4.5 dp15 candidate —— 5 min 内穿透 production 层、surface ObjectInteraction system 跨 5 sprint EditMode-green-but-not-wired 反模式。这种发现远比 fun loop 微调有价值。

---

## §4 Emergent Issues（分级 + Sprint 安排）

> 按严重度 S0/S1/S2/S3 分类；每条 issue 注明触发场景 / 期望 vs 实际 / Sprint 安排 / 关联 dp candidate（如有）。

### §4.1 S0 Critical（block playtest 2 进行）

> Expected: 0（S5-02 + S6-04 5/5 R3 已 verify mechanical robustness）—— **实际触发 5 项 production wiring gap**（同一 root cause，分 5 个 emergent fix story 修复，对应 vs-chapter-1 epic story-004~008）

- [x] **S0-1: Touch/Mouse → GestureDispatcher.Dispatch production caller 0 hit** — `IGestureEvent.OnTap` 在 production 0 sender；EditMode `GestureDispatchTests.cs` 1 处调用 → 整条 input → gesture → event 链 production 0 落地；Sprint 7+ ADR-010 InputManager epic 部分 scope **必须 pull-forward 到 Sprint 6** 作为 emergent fix 前提。**Sprint 安排**: Sprint 6 emergent fix track F **story-004 input-pipeline-wiring** (~2-3 hr)。**关联 dp candidate**: dp15 NEW（详 §4.5）。
- [x] **S0-2: Chapter_01_Approach.unity 未挂 InteractionCoordinator GameObject** — 无 InteractionCoordinator MonoBehaviour 节点；Sprint 5 S5-01 scene build 时 wiring gap，S5-02 spike fire mock OnMatchScoreUpdated 简化路径绕过，未触发此 gap surface。**Sprint 安排**: emergent fix track F **story-005 chapter-1-scene-wiring** (~1-1.5 hr) 含 Object MonoBehaviour 挂载（S0-3）。
- [x] **S0-3: Object_01_CoffeeMug / Object_02_Book 缺 InteractableObject MonoBehaviour** — Session 27 #2 unity-mcp 实测 componentTypes `[Transform, MeshFilter, BoxCollider, MeshRenderer]` (Object_01) / `[Transform, MeshFilter, CapsuleCollider, MeshRenderer]` (Object_02)；当时决策推迟到 Sprint 6 polish，但 Sprint 6 stories 未排此项。**Sprint 安排**: 与 S0-2 合并到 emergent fix track F **story-005**。
- [x] **S0-4: GameApp.Entrance 未调 RegisterPuzzleConfigProvider + RegisterInputConfigProvider** — `InteractableObject.RegisterPuzzleConfigProvider(Func<int, PuzzleConfig>)` + `InteractionCoordinator.RegisterInputConfigProvider(Func<IInputConfig>)` 静态注入入口 GameApp.cs 0 调用；即使 InteractableObject MonoBehaviour 挂载完成也会因 provider null 立即 fail-loud Log.Error 退化 fallback。**Sprint 安排**: emergent fix track F **story-006 gameapp-provider-injection** (~0.5 hr)。
- [x] **S0-5: ShadowMatchCalculator (ADR-012) listener 物件 transform → 阴影匹配 → OnMatchScoreUpdated production 0 wire** — S5-02 spike P2 直接 fire mock `OnMatchScoreUpdated(1, 0.95)` 绕过 ObjectInteraction → ShadowMatch 整条链路；fun loop replay 必须依赖此 wire 完整。**Sprint 安排**: emergent fix track F **story-007 shadowmatch-production-wire** (~2-3 hr)。

**S0 Total scope**: ~7-9 hr（story-004 ~ -007）+ **story-008 end-to-end-smoke-replay** (~1 hr) S5-02 spike 重写不依赖 mock fire；总 ~7-9 hr Sprint 6 本周 split 2-3 daily session 嵌入。详见 §6.2。

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

- ✅ **V3.0.1 dp14 candidate NEW (2026-05-14 Session 32)** — **playtest infrastructure pattern gap** —
  S6-01 Phase 2 起步时 surface：当前所有 DevSpike 都是 R3 PlayMode test spike（Launch() 内 AddComponent
  MonoBehaviour 自动 RunAllAsync 跑 R3 case + assert），与 manual playtest 节奏完全不兼容（spike fire(99)
  UnknownChapter / fire(2) chapter switch / Error state recovery 等会打断用户手动 click NewGame 进 chapter 1）。
  S6-01 Phase 1 prep 盲点：仅想到 evidence doc 模板，没想到 spike RunAllAsync 与 manual playtest 兼容性问题。
  - **Fix (Phase 2.0)**: 新建 `S6-01_PlaytestHoldMode.cs` (S601PlaytestSpike : IDevSpike, Launch() no-op) +
    DevTestState `[main-menu]` mode HasSpike list +1 → 5 个 spike 远超原阈值 4 + GameApp.RegisterDevSpikes
    切换 S604Spike → S601PlaytestSpike + dp8 candidate +1 (=5)。
  - **Sprint 7+ 复用**: S6-02 / S6-03 / 未来 chapter 2 playtest 等所有 manual playtest session 复用此 spike pattern。
  - **Sprint 6 retro 议题 (NEW)**: 评估 promote 为 ADR-029 V3 正式 dp + create standard PlaytestMode pattern doc
    (与 V3.0.1 dp8 DevTestState central mode-dispatch refactor 候选 关联 — 是否合并提案)。

- ⭐ **V3.0.1 dp15 candidate NEW (2026-05-14 Session 32 — promote 优先级 高于 dp14)** —
  **"EditMode green ≠ production wired sniff"** —
  S6-01 Phase 2.1 manual playtest ~5 min 内穿透 production 层 surface：
  Sprint 2 ObjectInteraction system 代码齐全（5 production class — InteractableObject + InteractableObjectFsm +
  InteractableObjectFeedback + InteractionCoordinator + InteractionLockManager + EditMode test 全 pass），
  但 chapter 1 production wiring **整套 0 落地**（5 处缺口同时存在 — 详见 §4.1 S0-1~S0-5）；
  S5-02 spike fire mock `OnMatchScoreUpdated(1, 0.95)` 简化路径让 R3 5/5 PASS 掩盖整套 wiring gap。
  - **governance 重量**: 这是 ADR-029 V3 R3 standard "PlayMode probe spike" 验证可信度边界 ——
    spike PASS ≠ "system production wired" ≠ "chapter scene 内可玩"；EditMode test pass 同样不代表 production caller hit > 0。
  - **跨 sprint drift**: Sprint 2 → Sprint 3 → Sprint 4 → Sprint 5 → Sprint 6 五个 sprint 期间，
    ObjectInteraction system 一直处于 *EditMode test green + production 0 wiring* 状态，未被任何 R2 / R3 / smoke check 揭穿，
    直到 S6-01 manual playtest 第一次穿透。
  - **proposed ADR-029 V3 R3 amend "production caller hit > 0 sniff" sub-clause** —
    R3 PlayMode spike 设计前 + Phase 1 readiness gate 必加 R2 grep verify：
    `rg 'XxxDispatcher\.Yyy' --type cs` 或 `class XxxManager.*MonoBehaviour` production caller > 0 = sniff PASS；
    = 0（仅 EditMode test fixture 调）→ 黄牌警告 spike 路径可能 mock 绕过 wiring gap，强制评估补 R3 production wiring sniff case。
  - **Sprint 7+ 影响**: 适用于全部跨 sender → listener 链路系统（`ShadowMatchCalculator`, `NarrativeSequencePlayer`,
    `ChapterStateManager`, `InputManager` 未来 epic, etc.）—— sniff sub-clause 防止下一个 system 重蹈覆辙。
  - **Sprint 6 retro 议题 (NEW 议题 6)**: 评估 promote V3.x sub-version + R3 standard amend
    "production caller hit > 0 sniff sub-clause" + 与 dp14 'playtest infrastructure pattern gap' 关联评估
    （dp14 是 manual playtest 工具链 gap，dp15 是 EditMode-vs-production 验证可信度 gap，governance 层级不同但同根 — 都是
    "spike/test PASS 不等于 production 可玩"反模式）。
- [ ] TBD（其他 playtest replay after emergent fix 中发现）

---

## §5 ADR-030 §VS Build commit 6 项进度验证

| # | 项 | Status | Reference |
|---|---|---|---|
| 1 | chapter 1 build complete | ✅ | S5-01 + S5-1b + S5-1c + S5-02 (5 systems happy path) |
| 2 | Track A production code (5 systems) | ✅ | S5-02 5/5 R3 + 36/36 asserts (2026-05-12) |
| 3 | **≥3 internal playtest sessions** | ⏳ **1/3 partial done after 本 session — fun loop 不达 ≥30 min 但 governance value 满足**（surface dp15 candidate + 5 处 wiring gap → emergent fix epic 触发）；replay after story-004~008 done 后再判定是否需重计 1/3 | 本 file (S6-01 partial) / S6-02 (after emergent fix) / S6-03 |
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

### §6.2 Emergent fixes 分配（Sprint 6 内 vs Sprint 7）— **Track F NEW (chapter 1 production wiring emergent fix)**

> 沿用 `production/epics/vs-chapter-1/` epic 加 story-004~008（不另起 epic；governance log 入 active.md Session 32 + sprint-status.yaml updated row + Sprint 6 retro 议题 +2）。

| Story | 内容 | 估时 | GDD/ADR 锚 | 关键 AC |
|-------|------|------|-----------|---------|
| **story-004 input-pipeline-wiring** | Touch/Mouse → GestureRecognizer FSM → `GestureDispatcher.Dispatch(GestureData)` production wire | 2-3 hr | ADR-010 Input Abstraction（Sprint 7+ epic 部分 scope pull-forward；governance justification = block S6-02 playtest 2） | Editor Play 时 Mouse click 能 fire `IGestureEvent.OnTap`；PlayMode probe 1 case 验 Mouse Tap → event round-trip |
| **story-005 chapter-1-scene-wiring** | Chapter_01_Approach.unity 加 InteractionCoordinator GameObject + Inspector 拖入 _objects + Camera + Layer + Object_01/02 加 InteractableObject MonoBehaviour + 配 _puzzleId + Interactable layer | 1-1.5 hr | ADR-013 Object Interaction FSM + ADR-027 GameEvent | unity-mcp `manage_scene get_hierarchy` 验组件齐全；Object layer = Interactable |
| **story-006 gameapp-provider-injection** | GameApp.Entrance 调 `InteractableObject.RegisterPuzzleConfigProvider(...)` + `InteractionCoordinator.RegisterInputConfigProvider(...)` | 0.5 hr | ADR-013 §Architecture + Sprint 2 SP-013 注入约定 | GameApp.cs 见 2 处 Register；PlayMode probe 启动后 `InteractableObject._puzzleConfig != null` |
| **story-007 shadowmatch-production-wire** | ADR-012 ShadowMatchCalculator listener 物件 transform → 阴影匹配 → `IPuzzleEvent.OnMatchScoreUpdated` fire（production，不再 spike fire mock） | 2-3 hr | ADR-012 Shadow Match Calculation | 拖动 Object_01 接近正确位置 → score 变化；score=0.95 fire `OnPerfectMatch` → narrative trigger（manual playtest replay 验） |
| **story-008 end-to-end-smoke-replay** | S5-02 spike 重写：去除 mock fire OnMatchScoreUpdated，改用 production wiring simulate Tap+drag → match → score → narrative 完整 round-trip | 1 hr | ADR-029 V3 R3 standard + V3.0.1 dp15 candidate "production caller hit > 0 sniff" 试点 | R3 1 case 验 production wiring 不依赖 mock 也能 happy path 走通 |
| **总计** | | **~7-9 hr** | | Sprint 6 本周 split 2-3 daily session 嵌入 |

**S1/S2/S3 安排**（不适用 — 本次 playtest 未到能 surface S1/S2/S3 的 fun loop 阶段；replay after emergent fix 时再启 §4.2/§4.3/§4.4 评估）。

### §6.3 Playtest 2 起手前置（per S6-02 entry）

- emergent fixes from §4.1 + §4.2 完成（or 接受 known issue）
- S6-09 art asset 状态明确（升 / 不升）
- 同一 Cursor session 内还是 fresh Unity boot？（per S6-02 reuse session 语义）

### §6.4 Sprint 6 retro 待加议题（如本 playtest 触发）

- [ ] 累积 4 已知议题: V3.0.1 dp8 candidate / /vertical-slice vs S6-10 sequencing / dp13 candidate Director gates vs ADR-029 V3 coexistence / ≤5 hr hard rule 2 次 override 反思
- ✅ **NEW 议题 5 (2026-05-14 Session 32 morning)**: V3.0.1 dp14 candidate NEW 'playtest infrastructure pattern gap' —
  评估 promote ADR-029 V3 正式 dp + standard PlaytestMode pattern 文档化（Sprint 7+ 所有 playtest 复用基线）；
  与 dp8 DevTestState central mode-dispatch refactor 关联 —— 是否合并 V3.1 trigger 提案。
- ⭐ **NEW 议题 6 (2026-05-14 Session 32 morning — promote 优先级 高于议题 5)**:
  **V3.0.1 dp15 candidate NEW 'EditMode green ≠ production wired sniff'** —
  评估 promote V3.x sub-version + ADR-029 V3 R3 standard amend "production caller hit > 0 sniff sub-clause"
  （rg `XxxDispatcher\.Yyy` / `class XxxManager` production caller > 0 = sniff PASS；= 0 仅 EditMode test fixture 调
  → 黄牌警告 spike 路径可能 mock 绕过 wiring gap）；评估与 dp14 关联（同根 "spike/test PASS 不等于 production 可玩"反模式
  但层级不同 — dp14 manual playtest 工具链 gap，dp15 EditMode-vs-production 验证可信度 gap）；
  评估应用范围（ShadowMatchCalculator / NarrativeSequencePlayer / ChapterStateManager / 未来 InputManager epic 等）。
- [ ] 本 playtest 1 实际玩中其他议题: TBD（emergent fix done 后 replay 时填）

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
| **Playtest verdict** | ⚠️ **NEEDS-WORK** — fun loop 不可评估（5 处 production wiring gap 阻塞）；governance value 满足（dp15 candidate NEW + 揭露 5 sprint EditMode-green-but-not-wired 反模式） |
| **PASS criteria** | §3 Fun Loop Assessment 整体均分 ≥ 3.5 + §4.1 S0 = 0 + §4.2 S1 ≤ 3 项 |
| **NEEDS-WORK criteria** | §3 均分 < 3.5 OR §4.1 S0 ≥ 1 OR §4.2 S1 ≥ 4 项 → **§4.1 S0 = 5 项 触发 NEEDS-WORK** |
| **FAIL criteria** | §4.1 S0 阻塞 fun loop OR Sprint 6 capacity 不够 fix → 不触发 FAIL（Sprint 6 capacity ~7-9 hr emergent fix 可吸收，本周 split 2-3 daily session） |
| **Next action** | (1) 立 emergent fix epic vs-chapter-1 story-004~008（Track F NEW，~7-9 hr，Sprint 6 本周）；(2) story-004~008 done 后启 Phase 2.2 replay manual playtest（reuse session 语义重测）；(3) replay PASS 后才进 S6-02；(4) S6-09 art asset 升级时机不变（playtest 1 反馈不足以判定 art 是否 fun loop 阻碍 → 推 [A2] 路径直到 replay session）；(5) Sprint 6 retro 议题 +2 = 6 项（dp14 + dp15 NEW）。 |
| **Player sign-off** | chen 2026-05-14 Session 32 (NEEDS-WORK acknowledged + emergent fix 决策 [A] 批准) |
| **Observer sign-off** | Claude Code 2026-05-14 Session 32 (root cause analysis + 5 处 wiring gap rg 验证 + dp15 candidate proposal 落档) |

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
- **2026-05-14 morning (Session 32)** — **Phase 2.0 playtest infrastructure fix ✅ DONE** — 起步时 surface
  V3.0.1 dp14 candidate NEW 'playtest infrastructure pattern gap'：所有现有 DevSpike 是 R3 PlayMode test spike
  (RunAllAsync 自动跑 case + assert) 与 manual playtest 节奏不兼容；新建 S6-01_PlaytestHoldMode.cs
  (S601PlaytestSpike : IDevSpike, Launch() no-op) + DevTestState [main-menu] mode HasSpike list +1
  (S5-02/S6-07/S6-08/S6-04/S6-01-playtest = 5 远超原阈值 4) + GameApp.RegisterDevSpikes 切换
  S604Spike → S601PlaytestSpike + dp8 candidate +1=5 + dp14 候选 NEW + Sprint 6 retro 议题 +1=5。
  Phase 2.0 投入 ~15-20 min；Sprint 7+ 所有 playtest session 复用此 spike pattern。
- ⏳ **NEXT (Session 32 接续)** — Phase 2.1 manual playtest ≥ 30 min + §1..§8 完整填空
- ⏳ TBD post-Phase 2.1 — closure (verdict + emergent fixes + Sprint 6 后续影响决策)
