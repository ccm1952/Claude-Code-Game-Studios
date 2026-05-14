// 该文件由Cursor 自动生成

# Story 004: Chapter 1 Input Pipeline Wiring — Touch/Mouse → GestureRecognizer FSM → GestureDispatcher.Dispatch (production caller)

> **Epic**: VS Chapter 1
> **Story ID**: vs-chapter-1-004 (Sprint 6 emergent fix Track F NEW；S6-01 Phase 2.1 manual playtest NEEDS-WORK 派生)
> **Sprint**: 6 (Track F NEW — chapter 1 production wiring emergent fix)
> **Story Type**: Logic / Integration
> **Complexity Points**: 2-3 (~2-3 hr 估时；ADR-010 部分 scope pull-forward 到 Sprint 6)
> **GDD Requirement**: TR-input-* (待 tr-registry.yaml R2 实证；候选 TR-input-001 Tap gesture sender / TR-input-002 Drag gesture sender)
> **ADR References**: **ADR-010 Input Abstraction**（部分 scope pull-forward 到 Sprint 6 — governance justification = block S6-02/S6-03/S6-10；Sprint 7+ ADR-010 full epic 完成余下 scope multi-touch/Pinch/Rotate + InputBlocker listener-side + Touch 真机 testing + InputManager singleton）+ ADR-027 §3 IGestureEvent contract（OnTap/OnDrag/OnRotate/OnPinch/OnLightDrag — 一手势一方法）+ ADR-029 V3.0.1（R2 deficiency-flagged PASS path 候选 + V3.0.1 dp15 candidate "production caller hit > 0 sniff sub-clause" 试点）+ ADR-030 §VS Build commit 第 1 项 chapter 1 fun loop 验证 prerequisite
> **Status**: 📝 **Draft** (2026-05-14 Session 32 morning continue — emergent fix epic Track F NEW story-004~008 outline approved per [A]，本 story Phase 0 R2 vendor reality verify pending)
> **Created**: 2026-05-14 morning continue (Sprint 6 Session 32 — emergent fix Track F NEW first story)
> **Completed**: ""
> **Depends on**: S5-02 ✅ (Chapter 1 happy path baseline；spike fire mock OnMatchScoreUpdated 简化路径 must be replaced by production wiring) + Sprint 2 SP-013 ✅ (GestureRecognizer FSM + GestureDispatcher.Dispatch + InteractionCoordinator OnTap/OnDrag listener — production code 已 done) + S6-01 Phase 2.1 ⚠️ NEEDS-WORK (V3.0.1 dp15 candidate root cause analysis surface 5 处 wiring gap 中 S0-1)

---

## Context

**S6-01 Phase 2.1 manual playtest NEEDS-WORK 派生 emergent fix Track F 第 1 story (~2-3 hr ADR-010 部分 scope pull-forward)**：

S6-01 Phase 2.1 ~5 min 内 user 报 "Object_01 无法交互，点击拖拽 没响应"，agent root cause analysis 发现 5 处 chapter 1 production wiring 缺口同时存在；其中 **S0-1: Touch/Mouse → GestureDispatcher.Dispatch production caller 0 hit** —— `rg 'GestureDispatcher\.Dispatch' --type cs` 仅 EditMode `GestureDispatchTests.cs` 1 处调用，整条 Touch/Mouse → GestureRecognizer FSM → IGestureEvent.OnTap/OnDrag fire 链 production 0 落地。Sprint 2 SP-013 GestureRecognizer FSM + GestureDispatcher.Dispatch 代码齐全 + EditMode test 通过，但缺 production sender (Touch input pipeline 未接入 GestureRecognizer)。

**V3.0.1 dp15 candidate "EditMode green ≠ production wired sniff" governance 实战首次触发** —— 本 story 是 dp15 候选 sniff sub-clause 试点的 第 1 个 production caller hit > 0 修复 case；story-008 end-to-end-smoke-replay 是 sniff sub-clause 校验 test case (重写 S5-02 spike 不依赖 mock fire OnMatchScoreUpdated)。

### Sprint 6 emergent fix Track F serial 顺序

```
story-004 (本 story, input pipeline wiring) → story-005 (chapter-1-scene-wiring InteractionCoordinator + InteractableObject) → story-006 (gameapp-provider-injection) → story-007 (shadowmatch-production-wire) → story-008 (end-to-end-smoke-replay 重写 S5-02 spike 不 mock fire)
```

story-005 依赖本 story Mouse Tap 能 fire IGestureEvent.OnTap (InteractionCoordinator OnTap listener 才会 trigger)；story-007 依赖 story-005 InteractableObject MonoBehaviour 已挂 (InteractableObject.OnDrag listener 才会被 InteractionCoordinator 转发)；story-008 依赖 story-004~007 全部 done (production wiring 完整 round-trip)。

---

## Goal Flow (T0 → T6)

```
T0  Editor Play start → main.unity boot → GameApp.Init → DevTestState [main-menu] mode → S6-01 PlaytestSpike no-op → ShowMainMenuPanelAsync 显示 main menu UIWindow
T1  user Mouse left button click → GestureRecognizer FSM 接 Mouse input → 200ms debounce + fat finger compensation per ADR-010 → fire GestureType.Tap with screen position
T2  GestureDispatcher.Dispatch(GestureData) → switch case Tap → GameEvent.Get<IGestureEvent>().OnTap(gesture) fire → InteractionCoordinator.OnTap listener (Sprint 2 SP-013 done) 收到 raycast 选取（chapter 1 scene 内 InteractionCoordinator 由 story-005 挂载）
T3  user Mouse left button hold + drag → GestureRecognizer FSM 进 Drag.Began phase → fire GestureData.Began → GestureDispatcher.Dispatch → IGestureEvent.OnDrag(gesture) fire → InteractionCoordinator.OnDrag listener 转发到选中 InteractableObject.OnDragBegan
T4  Mouse drag move → GestureRecognizer FSM Drag.Updated phase → fire periodically → IGestureEvent.OnDrag → InteractableObject.OnDrag.Updated phase 内消费 ScreenPos 缓存
T5  Mouse release → GestureRecognizer FSM Drag.Ended phase → fire 1 final → InteractableObject.OnDragEnded → 物件 transform settle
T6  evidence: PlayMode probe R3 case 1+ verify Mouse Tap → IGestureEvent.OnTap event round-trip + listener 收 GestureData (per S5-02 P1 listener spy precedent)
```

---

## ADR Decision Summary

**ADR-010 Input Abstraction 部分 scope pull-forward** (Sprint 7+ full epic 余下 scope 留)：
- ✅ 本 story scope: Mouse → GestureRecognizer FSM → GestureDispatcher.Dispatch → IGestureEvent.OnTap/OnDrag fire (Editor 内 fun loop 验证最低需求)
- ⚠️ Sprint 7+ scope: Touch input 真机 testing + multi-touch / Pinch / Rotate + Editor Mouse simulate Touch hybrid + InputBlocker listener-side singleton + InputManager class facade

**ADR-027 §3 IGestureEvent contract inherit** — 5 method 已冻结 (Sprint 2 SP-013 done):
- `OnTap(GestureData)` / `OnDrag(GestureData)` / `OnRotate(GestureData)` / `OnPinch(GestureData)` / `OnLightDrag(GestureData)`
- 本 story 仅 wire OnTap + OnDrag (chapter 1 fun loop 必需)；OnRotate/OnPinch/OnLightDrag 留 Sprint 7+

**ADR-029 V3.0.1 R3 standard amend candidate "production caller hit > 0 sniff sub-clause"** —— 本 story 是 V3.0.1 dp15 候选 sniff sub-clause 试点：
- R2 grep verify `rg 'GestureDispatcher\.Dispatch' --type cs Assets/GameScripts/HotFix` production caller > 0 = sniff PASS（本 story 完成后 R2 grep 应见 ≥1 production caller）
- R3 PlayMode probe 1+ case 验 production wiring round-trip（不依赖 spike fire mock OnTap/OnDrag）

---

## Engine Notes

**待 /story-readiness gate Phase 0 R2 vendor reality verify (本 story Status: Draft → Ready)**：
- R2.1 ⚠️ TBD: Sprint 2 SP-013 GestureRecognizer FSM 现状 (`Assets/GameScripts/HotFix/GameLogic/Input/GestureRecognizer.cs` 或类似命名 — 实施完整度 / partial / 0)
- R2.2 ⚠️ TBD: Touch input pipeline 在 Sprint 2 partial vs 0 (`Input.touchCount` / `Input.GetTouch(0)` production caller 现状)
- R2.3 ⚠️ TBD: Mouse input pipeline 在 Sprint 2 partial vs 0 (`Input.GetMouseButtonDown` / `Input.GetMouseButton` production caller 现状)
- R2.4 ⚠️ TBD: GestureDispatcher.Dispatch caller chain (`rg 'GestureDispatcher\.Dispatch' --type cs` 现状已知 0 production，但 Sprint 2 SP-013 是否有"待接入"占位 unblock?)
- R2.5 ⚠️ TBD: ADR-010 spec 边界 — 本 story narrow scope 是否能在 Mouse-only Editor 内可达，还是必须 Touch hybrid 才算完整（per ADR-010 §Decision 文）
- R2.6 ⚠️ TBD: GestureData struct (Sprint 2 SP-013) 字段 list — Tap 必填 ScreenPos / FingerId / Phase 等，Drag 必填 ScreenPos / Phase (Began/Updated/Ended) / Delta etc
- R2.7 ⚠️ TBD: InputConfig POCO struct + Luban TbInput 现状 (S6-07 R2.6 提及 InputConfigFromLuban.InitWithDefaults stub)
- R2.8 ⚠️ TBD: Editor InputSystem package vs legacy Input class 现状（影响 R3 PlayMode probe Mouse simulate API 选择）

---

## Control Manifest Rule References

**待 /story-readiness gate Phase 1 R1 grep audit (从 docs/architecture/control-manifest.md 提取本 story 适用的 Required / Forbidden 规则)**：
- ⚠️ TBD: Required — Mouse input must drive GestureRecognizer FSM (no direct InteractableObject Touch listener bypass per ADR-010)
- ⚠️ TBD: Forbidden — `Camera.main` fallback (per ADR-012 + S5-1c precedent；GestureRecognizer 需 Inspector 注入 camera)
- ⚠️ TBD: Forbidden — `FindObjectsOfType<InteractableObject>` runtime 查找 (per ADR-013 InteractionCoordinator Inspector 预填规则)
- ⚠️ TBD: Required — IGestureEvent.OnTap/OnDrag 走 ADR-027 §3 一手势一方法 contract (no per-event mode mixing)

---

## Acceptance Criteria

| # | AC | Verify path |
|---|-----|------|
| AC-1 | Editor Play start 后，user Mouse left button single click on screen → fire `IGestureEvent.OnTap(GestureData)` 1 次（GestureData.ScreenPos = click position） | R3 PlayMode probe P1 case + listener spy assert |
| AC-2 | Editor Play start 后，user Mouse left button hold + move + release → fire `IGestureEvent.OnDrag(GestureData)` 三 phase（Began 1 次 + Updated 多次 + Ended 1 次） | R3 PlayMode probe P2 case + listener spy assert |
| AC-3 | GestureRecognizer FSM 实现 fat finger compensation per ADR-010 spec（screen position offset 容忍 + 200ms debounce minimum gap between consecutive Tap events） | R3 PlayMode probe P3 case rapid Tap × 3 → 仅 first / last fire (200ms debounce filter) |
| AC-4 | `GestureDispatcher.Dispatch(GestureData)` production caller hit > 0（rg verify ≥1 production class 调，不只 EditMode test fixture） | R2 grep audit + production code review |
| AC-5 | PlayMode probe ≥1 R3 case Mouse Tap → IGestureEvent.OnTap event round-trip + listener 收到 GestureData + 全 5/5 asserts PASS | R3 PlayMode probe evidence + JSON dump |
| AC-6 | 0 production unexpected console error/warning during Mouse Tap + Drag interaction（Application.logMessageReceived UnexpectedErrorCount==0） | R3 evidence dump |
| AC-7 | 0 mock fire bypass（spike + production code 不直接 `GameEvent.Get<IGestureEvent>().OnTap(...)` 等 mock fire）—— V3.0.1 dp15 sniff sub-clause 试点 | R2 grep verify + spike code review |

---

## R3 PlayMode Probe Plan（待 /story-readiness gate amend detail）

预计 spike `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-XX_InputPipelineWiring.cs`（spike Id 待 sprint-status.yaml S6-XX 分配；可能复用 vs-chapter-1-004 命名）：

- **P1 MouseSingleTapToOnTapEvent** — InputSimulation Mouse single click → expect IGestureEvent.OnTap fire 1 次 + GestureData.ScreenPos == click position
- **P2 MouseDragThreePhaseFire** — InputSimulation Mouse hold + move + release → expect OnDrag.Began 1 + Updated ≥1 + Ended 1
- **P3 RapidTapDebounceFilter** — InputSimulation rapid 3 click within 100ms → expect OnTap fire ≤2（200ms debounce filter applies）
- **P4 FatFingerCompensationOffset** — InputSimulation Mouse click within tolerance offset → expect IGestureEvent.OnTap.ScreenPos snapped to nearest interactive object screen pos（per ADR-010 spec）
- **P5 NoMockFireBypassVerify** — V3.0.1 dp15 sniff sub-clause 试点 — spike 与 production code 任何路径 0 调 `GameEvent.Get<IGestureEvent>().OnTap(...)` mock fire；listener spy expect 仅自然 input pipeline path fire（assert 0 mock-origin event with origin tag）

---

## R2 Assumptions Validated（待 Phase 0 实证）

| # | Assumption | Verify | Status |
|---|------------|--------|--------|
| R2.1 | Sprint 2 SP-013 GestureRecognizer FSM 已实施完整 | Read GestureRecognizer.cs / Glob `Assets/GameScripts/HotFix/GameLogic/Input/*.cs` | ⚠️ TBD |
| R2.2 | Touch input pipeline production 实施现状 | rg `Input.touchCount\|Input.GetTouch` --type cs | ⚠️ TBD |
| R2.3 | Mouse input pipeline production 实施现状 | rg `Input.GetMouseButton` --type cs | ⚠️ TBD |
| R2.4 | GestureDispatcher.Dispatch production caller chain | rg `GestureDispatcher\.Dispatch` --type cs Assets/GameScripts/HotFix | ⚠️ TBD（已知本次 0 hit；需评估 Sprint 2 SP-013 是否 partial impl 占位）|
| R2.5 | ADR-010 spec narrow scope 边界 | Read ADR-010 §Decision + §Implementation Guidelines | ⚠️ TBD |
| R2.6 | GestureData struct 字段 list | Read GestureData.cs (Sprint 2 SP-013) | ⚠️ TBD |
| R2.7 | InputConfig POCO + Luban TbInput 现状 | Read InputConfigFromLuban.cs + grep TbInput | ⚠️ TBD |
| R2.8 | Editor InputSystem package vs legacy Input | Read Packages/manifest.json + Project Settings | ⚠️ TBD |

---

## V3.0.1 Watch List Hooks

**Type-11 V3.0.1 dp15 candidate "EditMode green ≠ production wired sniff"** — 本 story 是 dp15 候选 sniff sub-clause **第 1 个 production caller hit > 0 修复 case**（GestureDispatcher.Dispatch 0 production caller → fix 后 ≥1 production caller）：
- 本 story 完成后 R2 grep verify `rg 'GestureDispatcher\.Dispatch' --type cs Assets/GameScripts/HotFix` production caller > 0 = sniff PASS
- 本 story PlayMode probe AC-7 0 mock fire bypass = sniff sub-clause 试点 production wiring 完整性校验
- Sprint 6 retro 议题 6 dp15 promote V3.x sub-version 决策依据之一（如本 story + story-008 end-to-end-smoke-replay 试点成功 → promote）

**Type-10 V3.0.1 dp14 candidate "playtest infrastructure pattern gap"** — 本 story 不直接触发 dp14（PlaytestSpike pattern Phase 2.0 已 fix）；PlayMode probe spike 沿用 R3 standard 自动 RunAllAsync 模式（与 manual playtest 节奏不冲突，因为 PlayMode probe 与 manual playtest 是 separate session）。

**Type-5 V3.0.1 candidate "spec/tooling ↔ reality drift"** — 留观察 R2 verify 阶段是否 surface ADR-010 spec wording drift（如 ADR-010 §Implementation Guidelines 200ms debounce 描述与 Sprint 2 SP-013 实施不一致）。

---

## Out of Scope（明示）

- ❌ **Multi-touch / Pinch / Rotate / LightDrag** — Sprint 7+ ADR-010 full epic（chapter 1 fun loop minimal 仅需 Tap + Drag）
- ❌ **Touch input 真机 testing** — Sprint 7+ mobile build prerequisite（Editor Mouse simulate 满足 Sprint 6 fun loop 验证）
- ❌ **InputBlocker listener-side singleton + InputManager class facade** — Sprint 7+ ADR-010 full epic（S5-05/S6-08 sender-side narrow scope 已 closure）
- ❌ **Editor Mouse simulate Touch hybrid pattern** — Sprint 7+ ADR-010 epic（Editor Mouse pipeline + Touch pipeline 各自独立 production caller，不强制 hybrid）
- ❌ **Settings menu input remap** — S5-09 backlog（Sprint 7+ Production stage 后 polish phase）

---

## Implementation Notes（高层结构，待 Phase 0/1 R2 verify 后 amend 精细）

预计涉及文件：
1. **GestureRecognizer.cs** (Sprint 2 SP-013 partial) — Update 调 GestureRecognizer FSM Drive(Update Loop) production wiring，确保 Mouse input 驱动 FSM
2. **GameApp.cs** Initialize() — 加 GestureRecognizer Update Loop 注册（per S5-1b SceneManager Init precedent）
3. **InputManager.cs / InputDriver.cs** (NEW or amend) — Mouse production driver（Update Loop 内 Input.GetMouseButtonDown/Up + Input.mousePosition 转 GestureData fire）
4. **DevTestState.cs** (可能 amend) — `[main-menu]` mode 内 GestureRecognizer Update Loop 启动（如不在 GameApp Init 阶段统一启动）
5. **Spike S6-XX_InputPipelineWiring.cs** NEW — R3 PlayMode probe 5 case + listener spy + JSON evidence dump

**0 vendor patch 决策**：本 story 不改 TEngine vendor (Assets/TEngine/) — 所有改动限定 GameLogic 层。

---

## History

- **2026-05-14 morning continue (Session 32)**: Draft 创建（emergent fix epic Track F NEW story-004~008 outline approved per [A]）；Status: Draft；Phase 0 R2 vendor reality verify pending；本 story 是 V3.0.1 dp15 sniff sub-clause 试点第 1 个 production caller hit > 0 修复 case；ADR-010 部分 scope pull-forward governance justification = block S6-02/S6-03/S6-10。
