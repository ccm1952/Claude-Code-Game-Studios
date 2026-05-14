// 该文件由Cursor 自动生成

# Story 004: Chapter 1 Input Pipeline Wiring — Touch/Mouse → GestureRecognizer FSM → GestureDispatcher.Dispatch (production caller)

> **Epic**: VS Chapter 1
> **Story ID**: vs-chapter-1-004 (Sprint 6 emergent fix Track F NEW；S6-01 Phase 2.1 manual playtest NEEDS-WORK 派生)
> **Sprint**: 6 (Track F NEW — chapter 1 production wiring emergent fix)
> **Story Type**: Logic / Integration
> **Complexity Points**: 2-3 (~2-3 hr 估时；ADR-010 部分 scope pull-forward 到 Sprint 6)
> **GDD Requirement**: TR-input-* (待 tr-registry.yaml R2 实证；候选 TR-input-001 Tap gesture sender / TR-input-002 Drag gesture sender)
> **ADR References**: **ADR-010 Input Abstraction**（部分 scope pull-forward 到 Sprint 6 — governance justification = block S6-02/S6-03/S6-10；Sprint 7+ ADR-010 full epic 完成余下 scope multi-touch/Pinch/Rotate + InputBlocker listener-side + Touch 真机 testing + InputManager singleton）+ ADR-027 §3 IGestureEvent contract（OnTap/OnDrag/OnRotate/OnPinch/OnLightDrag — 一手势一方法）+ ADR-029 V3.0.1（R2 deficiency-flagged PASS path 候选 + V3.0.1 dp15 candidate "production caller hit > 0 sniff sub-clause" 试点）+ ADR-030 §VS Build commit 第 1 项 chapter 1 fun loop 验证 prerequisite
> **Status**: ⚠️ **DEFICIENCY-FLAGGED PASS** ready for Phase 2 transition (2026-05-14 Session 32 afternoon continue — Phase 0/1 R1+R2+R3 readiness gate done per ADR-029 V2.0 §V2-1 R2 DEFICIENCY-FLAGGED PASS path；R1 ✅ PASS / R2 ⚠️ DEFICIENCY-FLAGGED PASS 3 项 deficiency 留 Phase 2 closure / R3 ✅ PASS)
> **Created**: 2026-05-14 morning continue (Sprint 6 Session 32 — emergent fix Track F NEW first story)
> **Completed**: ""
> **Depends on**: S5-02 ✅ (Chapter 1 happy path baseline；spike fire mock OnMatchScoreUpdated 简化路径 must be replaced by production wiring) + Sprint 2 SP-013 ⚠️ **PARTIAL** (FSM 层 ✅ 完成 10 file: SingleFingerFSM.cs + DualFingerFSM.cs + GestureDispatcher.cs + GestureTypes.cs + IInputConfig.cs + InputConfigFromLuban.cs + InputBlocker.cs + InputFilter.cs + IDualFingerConfig.cs + TouchState.cs；Driver 层 ❌ 0：`InputService.cs` 不存在 + `Input.touchCount`/`GetTouch`/`GetMouseButton`/`mousePosition` 全 0 production caller — sprint-status.yaml 标 'DONE' 是 V3.0.1 dp11 同根 sprint backlog placeholder wording drift / **本 story Phase 0/1 readiness gate R2 verify 实证**) + S6-01 Phase 2.1 ⚠️ NEEDS-WORK (V3.0.1 dp15 candidate root cause analysis surface 5 处 wiring gap 中 S0-1)

---

## Context

**S6-01 Phase 2.1 manual playtest NEEDS-WORK 派生 emergent fix Track F 第 1 story (~2-3 hr ADR-010 部分 scope pull-forward)**：

S6-01 Phase 2.1 ~5 min 内 user 报 "Object_01 无法交互，点击拖拽 没响应"，agent root cause analysis 发现 5 处 chapter 1 production wiring 缺口同时存在；其中 **S0-1: Touch/Mouse → GestureDispatcher.Dispatch production caller 0 hit** —— `rg 'GestureDispatcher\.Dispatch' --type cs` 仅 EditMode `GestureDispatchTests.cs` 6 处调用 (line 77/92/103/113/123/134)，整条 Touch/Mouse → SingleFingerFSM/DualFingerFSM → IGestureEvent.OnTap/OnDrag fire 链 production 0 落地。**Sprint 2 SP-013 实际 partial 实施**: FSM 层 ✅ (SingleFingerFSM/DualFingerFSM/GestureDispatcher/GestureTypes/IInputConfig/InputConfigFromLuban/InputBlocker/InputFilter/IDualFingerConfig/TouchState 10 file)，Driver 层 ❌ (InputService Module 不存在 + Touch/Mouse sampling Update Loop 0)。

**V3.0.1 dp15 candidate "EditMode green ≠ production wired sniff" governance 实战首次触发** —— 本 story 是 dp15 候选 sniff sub-clause 试点的第 1 个 production caller hit > 0 修复 case；story-008 end-to-end-smoke-replay 是 sniff sub-clause 校验 test case (重写 S5-02 spike 不依赖 mock fire OnMatchScoreUpdated)。

**V3.0.1 dp11 同根 candidate "sprint backlog placeholder wording drift"** —— Phase 0/1 readiness gate 实证 sprint-status.yaml SP-013 标 "✅ DONE"，但实际 Driver 层未实施。本 story Phase 2 closure 同步 amend Sprint 2 SP-013 entry status 反映 partial 实情 (deficiency closure list 第 2 项)。

### Sprint 6 emergent fix Track F serial 顺序

```
story-004 (本 story, input pipeline wiring) → story-005 (chapter-1-scene-wiring InteractionCoordinator + InteractableObject) → story-006 (gameapp-provider-injection) → story-007 (shadowmatch-production-wire) → story-008 (end-to-end-smoke-replay 重写 S5-02 spike 不 mock fire)
```

story-005 依赖本 story Mouse Tap 能 fire IGestureEvent.OnTap (InteractionCoordinator OnTap listener 才会 trigger)；story-007 依赖 story-005 InteractableObject MonoBehaviour 已挂 (InteractableObject.OnDrag listener 才会被 InteractionCoordinator 转发)；story-008 依赖 story-004~007 全部 done (production wiring 完整 round-trip)。

---

## Goal Flow (T0 → T6)

```
T0  Editor Play start → main.unity boot → GameApp.Init → InputService Module 注册 + InputConfigFromLuban.InitWithDefaults() → DevTestState [main-menu] mode → S6-01 PlaytestSpike no-op → ShowMainMenuPanelAsync 显示 main menu UIWindow
T1  user Mouse left button click → MouseToTouchAdapter (Editor only) Update Loop 内 Input.GetMouseButtonDown(0) + Input.mousePosition → 构造 TouchState (FingerId=0 / Phase=Began / CurrentPosition / IsActive=true) → SingleFingerFSM.Update(in ts, Time.unscaledDeltaTime) → AccumulatedTime+TapTimeoutSeconds 内 Phase=Ended → fire GestureData (Type=Tap, Phase=Ended, ScreenPosition, TapCount=1)
T2  GestureDispatcher.Dispatch(in GestureData) → switch case Tap → GameEvent.Get<IGestureEvent>().OnTap(gesture) fire → InteractionCoordinator.OnTap listener (Sprint 2 SP-013 已 listener side production) 收到 raycast 选取（chapter 1 scene 内 InteractionCoordinator 由 story-005 挂载）
T3  user Mouse left button hold + drag → MouseToTouchAdapter Update Loop 持续构 TouchState (Phase=Moved) → SingleFingerFSM AccumulatedDist > DragThresholdPx 进 Dragging state → fire GestureData (Type=Drag, Phase=Began) → GestureDispatcher.Dispatch → IGestureEvent.OnDrag(gesture) fire → InteractionCoordinator.OnDrag listener 转发到选中 InteractableObject.OnDragBegan
T4  Mouse drag move → MouseToTouchAdapter 持续构 TouchState (Phase=Moved) → SingleFingerFSM Dragging state ProcessDragging → 每帧 fire GestureData (Type=Drag, Phase=Updated, Delta) → IGestureEvent.OnDrag periodically fire → InteractableObject.OnDrag.Updated phase 内消费 ScreenPosition + Delta 缓存
T5  Mouse release → MouseToTouchAdapter 构 TouchState (Phase=Ended) → SingleFingerFSM Dragging→Idle 转换 fire GestureData (Type=Drag, Phase=Ended) → InteractableObject.OnDragEnded → 物件 transform settle
T6  evidence: PlayMode probe R3 case 1+ verify Mouse Tap → IGestureEvent.OnTap event round-trip + listener 收 GestureData with ScreenPosition asserted (per S5-02 P1 listener spy precedent)
```

---

## ADR Decision Summary

**ADR-010 Input Abstraction 部分 scope pull-forward + Phase 2.0 spec amend** (Sprint 7+ full epic 余下 scope 留)：
- ✅ 本 story scope: Mouse → MouseToTouchAdapter (Editor only) → SingleFingerFSM → GestureDispatcher.Dispatch → IGestureEvent.OnTap/OnDrag fire (Editor 内 fun loop 验证最低需求；ADR-010 Layer 1 Raw Touch Sampling Driver 层补齐 — Sprint 2 SP-013 partial closure)
- ⚠️ **Phase 2.0 ADR-010 §Implementation Guidelines amend Step 9 "Editor Mouse Adapter"** (R2 NEW finding deficiency closure 第 1 项 — ADR-010 spec **完全没 cover Editor Mouse pipeline**，仅 Touch + PC 后续阶段；本 story narrow scope 必加 Step 9 spec 5-10 行明示 MouseToTouchAdapter Editor Play only + 真机 disable + Sprint 7+ Touch real device testing 时切换到 Touch sampling 路径)
- ⚠️ Sprint 7+ scope: Touch input 真机 testing + multi-touch / Pinch / Rotate + InputBlocker listener-side singleton + InputManager class facade + PC Mouse pipeline (Steam build) — 留 ADR-010 V2 amendment Sprint 7+

**ADR-027 §3 IGestureEvent contract inherit** — 5 method 已冻结 (Sprint 2 SP-013 done):
- `OnTap(GestureData)` / `OnDrag(GestureData)` / `OnRotate(GestureData)` / `OnPinch(GestureData)` / `OnLightDrag(GestureData)`
- 本 story 仅 wire OnTap + OnDrag (chapter 1 fun loop 必需)；OnRotate/OnPinch/OnLightDrag 留 Sprint 7+

**ADR-029 V3.0.1 R3 standard amend candidate "production caller hit > 0 sniff sub-clause"** —— 本 story 是 V3.0.1 dp15 候选 sniff sub-clause 试点：
- R2 grep verify `rg 'GestureDispatcher\.Dispatch' --type cs Assets/GameScripts/HotFix` production caller > 0 = sniff PASS（本 story 完成后 R2 grep 应见 ≥1 production caller in `InputService.Update` 或 `MouseToTouchAdapter.Update`）
- R3 PlayMode probe 1+ case 验 production wiring round-trip（不依赖 spike fire mock OnTap/OnDrag）

**Phase 2 deficiency closure list (3 项 per ADR-029 V2.0 §V2-1.b R2 DEFICIENCY-FLAGGED PASS path)**:
1. ADR-010 §Implementation Guidelines amend Step 9 "Editor Mouse Adapter" (~5-10 行 spec wording amend)
2. sprint-status.yaml Sprint 2 SP-013 entry status amend "DONE → partial-fsm-only-driver-pending" (V3.0.1 dp11 同根 sprint backlog placeholder wording drift closure 第 1 个实战触发)
3. story-004 多处 wording drift propagate fix: "GestureRecognizer FSM" → "SingleFingerFSM/DualFingerFSM" (5 处) + "ScreenPos" → "ScreenPosition" (3 处) + AC-4 caller chain wording 精化 (V3.0.1 Type-5 dp NEW 候选 留观察是否 ≥3 unique dp 触发 V3.x sub-version)

---

## Engine Notes

**Phase 0/1 R2 vendor reality verify ✅ DONE (2026-05-14 Session 32 afternoon — DEFICIENCY-FLAGGED PASS)**：
- R2.1 ⚠️ **PARTIAL — Sprint 2 SP-013 实际 partial 实施**: FSM 层 ✅ 完成 (10 file: SingleFingerFSM.cs + DualFingerFSM.cs + GestureDispatcher.cs + GestureTypes.cs + IInputConfig.cs + InputConfigFromLuban.cs + InputBlocker.cs + InputFilter.cs + IDualFingerConfig.cs + TouchState.cs)；Driver 层 ❌ 0 (`InputService.cs` 不存在 + Touch/Mouse Update Loop 0；ProfilerMarker label "InputService.Update" SingleFingerFSM.cs:28 仅占位)。**Sprint 2 SP-013 sprint-status.yaml 标 "DONE" 是 V3.0.1 dp11 同根 sprint backlog placeholder wording drift** — 本 story Phase 2 closure deficiency closure 第 2 项。
- R2.2 ❌ **CONFIRMED — Touch input pipeline production = 0**: `rg 'Input\.touchCount\|Input\.GetTouch' --type cs Assets/GameScripts` 0 production caller hit；与 dp15 candidate observation 一致。
- R2.3 ❌ **CONFIRMED — Mouse input pipeline production = 0**: `rg 'Input\.GetMouseButton\|Input\.mousePosition' --type cs Assets/GameScripts` 0 production caller hit；与 dp15 candidate observation 一致。
- R2.4 ❌ **CONFIRMED — GestureDispatcher.Dispatch production caller = 0**: `rg 'GestureDispatcher\.Dispatch' --type cs Assets/` 仅 EditMode `Tests/EditMode/InputSystem/GestureDispatchTests.cs` line 77/92/103/113/123/134 共 6 hit；production 0 hit (V3.0.1 dp15 sniff sub-clause baseline)。
- R2.5 ⚠️ **NEW DEFICIENCY — ADR-010 spec gap re Editor Mouse pipeline**: ADR-010 §Decision Layer 1 仅 cover Touch (`Input.GetTouch(i)`)；§Constraints "PC 端 (Steam) 后续阶段适配键鼠"；§Implementation Guidelines 9 config key 含 `pcRotateSensitivity`/`pcScrollSensitivity` 但是占位无实施 spec；**ADR-010 spec 完全没 cover Editor Mouse pipeline**。本 story narrow scope "Editor Mouse simulate Touch via MouseToTouchAdapter" 必须 Phase 2.0 amend ADR-010 §Implementation Guidelines Step 9 "Editor Mouse Adapter" (~5-10 行 spec wording amend) — deficiency closure 第 1 项。
- R2.6 ✅ **CONFIRMED + wording drift NEW** — GestureData struct 字段 list 8 字段 (`Type/Phase/ScreenPosition/Delta/AngleDelta/ScaleDelta/TouchCount/TapCount`) per `Assets/GameScripts/HotFix/GameLogic/Input/GestureTypes.cs:29-42`；本 story §Goal Flow + §AC + §R3 多处用 "ScreenPos" wording drift (实际 `ScreenPosition`) — Phase 2 closure 内 propagate fix (deficiency closure 第 3 项 + V3.0.1 Type-5 dp NEW 留观察)。
- R2.7 ⚠️ **PARTIAL — Luban TbInputConfig pending**: `InputConfigFromLuban.cs` 5 IInputConfig property + 3 IDualFingerConfig property + `InitWithDefaults()` ✅ ready (defaults: 3mm BaseDragThreshold / 0.25s TapTimeout / 100px MaxDeltaPerFrame / 160dpi Fallback / 8mm FatFingerMargin / 8° RotateThreshold / 0.08 PinchThreshold / 20px MinFingerDistance) 来自 ADR-010 §Implementation Guidelines table；`InitFromLuban(7 args)` 占位待 Luban TbInputConfig table 接入 (S5-XX+ TODO 注释)；本 story Phase 2 InputService Module 启动调 InitWithDefaults() 即可 (sufficient for chapter 1 fun loop)，Luban 接入留 Sprint 7+。
- R2.8 ✅ **CONFIRMED — legacy Input class only**: ProjectSettings.asset:918 `activeInputHandler: 0` (= "Input Manager (Old)" legacy `UnityEngine.Input` only；New Input System package 未启用)；与 ADR-010 §Constraints "TEngine 框架未集成 New Input System package" line 68 一致。**关键 implication**: Unity legacy Input + activeInputHandler=0 + Desktop Editor 内 user Mouse click **不天然模拟 Touch event** (`Input.GetTouch()` 在 Desktop Editor 内 0 fire 因为 desktop platform 没 touch device)；必须显式编 MouseToTouchAdapter (Phase 2 production code)。

### Phase 2 production wiring file list (Implementation Notes 大致结构)

1. **InputService.cs NEW** ~80-120 行 — TEngine Module 注册到 GameModule.Input；持有 `SingleFingerFSM` + `DualFingerFSM` + `InputConfigFromLuban`；Update Loop 内调 MouseToTouchAdapter (Editor Play only) 或 Touch Sampling (real device) → 构造 TouchState → fed into FSM → fire GestureDispatcher.Dispatch
2. **MouseToTouchAdapter.cs NEW (Editor Play only)** ~50-80 行 — `#if UNITY_EDITOR` 或 `Application.isEditor` runtime check；Update Loop 内调 `Input.GetMouseButtonDown(0)`/`Input.GetMouseButton(0)`/`Input.GetMouseButtonUp(0)` + `Input.mousePosition` → 构造 TouchState (FingerId=0 / Phase=Began|Moved|Stationary|Ended / CurrentPosition=mousePosition / IsActive=true)；Sprint 7+ Touch 真机 testing 时 disable adapter (Application.isEditor=false 时 fall back 到 Touch sampling)
3. **GameApp.cs Init Phase**: 加 `InputService` 注册到 GameModule (与 SceneManager / AudioManager 同 init phase)；调 InitWithDefaults() 暂用 GDD defaults (Luban TbInputConfig 留 Sprint 7+)
4. **DevTestState.cs**: 不需 amend (InputService 由 GameApp 启动时统一注册)
5. **Spike S6-13_InputPipelineWiring.cs NEW** ~500-700 行 — R3 PlayMode probe 5 case (P1~P5 per V3.0.1 dp15 sniff sub-clause 试点) + listener spy (IGestureEvent_Event.OnTap/OnDrag) + JSON evidence dump WriteResultJson per S6-04/S6-08 precedent
6. **ADR-010 §Implementation Guidelines amend Step 9**: 加 5-10 行 "Editor Mouse Adapter" spec wording (Phase 2.0 deficiency closure 第 1 项)

---

## Control Manifest Rule References

**Phase 1 R1 grep audit ✅ DONE (2026-05-14 Session 32 afternoon — R1 PASS)**：
- ✅ **Required**: Mouse input must drive SingleFingerFSM/DualFingerFSM via InputService Module (no direct InteractableObject Touch listener bypass per ADR-010 + control-manifest line 116/305 "Never call `Input.GetTouch()` directly outside InputService") — 本 story Phase 2 Implementation 满足 (MouseToTouchAdapter + InputService 内统一调 Input.GetMouseButton/touchCount)
- ✅ **Forbidden**: `Camera.main` per-frame (control-manifest line 117/338 "Cache camera reference — never call `Camera.main` per frame" — source: ADR-012)；本 story 0 Camera 调用 (Mouse pipeline 不查 camera；raycast 在 InteractionCoordinator 层)
- ✅ **Forbidden**: `FindObjectsOfType<InteractableObject>` runtime (per ADR-013 InteractionCoordinator Inspector 预填规则)；本 story 0 FindObjectsOfType 调用 (Mouse pipeline 不查 InteractableObject — fire IGestureEvent.OnTap/OnDrag 通过 GameEvent broadcast，listener 自行响应)
- ✅ **Required**: IGestureEvent.OnTap/OnDrag 走 ADR-027 §3 一手势一方法 contract (no per-event mode mixing)；本 story §Goal Flow + §ADR Decision Summary 已明示 GestureDispatcher.Dispatch switch case 5 路一对一 dispatch
- ✅ **Required**: All gesture thresholds from Luban TbInputConfig (control-manifest line 297 + ADR-010 §Implementation Guidelines)；本 story 用 InputConfigFromLuban.InitWithDefaults() 暂代 Luban (S5-XX+ TODO 已存)；Luban 接入 Sprint 7+
- ✅ **Required**: GameLogic 层 (control-manifest line 183 "All gameplay code resides in GameLogic")；本 story InputService.cs + MouseToTouchAdapter.cs + S6-13 spike 全 GameLogic asmdef ✅
- ✅ **Forbidden**: 任何 listener-side mock fire `GameEvent.Get<IGestureEvent>().OnTap(...)` 直接调 (绕 GestureDispatcher.Dispatch) — V3.0.1 dp15 sniff sub-clause 试点 baseline；本 story Phase 2 spike + production 全 0 mock fire (AC-7 verify)

---

## Acceptance Criteria

| # | AC | Verify path |
|---|-----|------|
| AC-1 | Editor Play start 后，user Mouse left button single click on screen → fire `IGestureEvent.OnTap(GestureData)` 1 次（GestureData.ScreenPosition = click position；TapCount=1；Phase=Ended per SingleFingerFSM EmitTap line 204-216） | R3 PlayMode probe P1 case + listener spy assert |
| AC-2 | Editor Play start 后，user Mouse left button hold + move + release → fire `IGestureEvent.OnDrag(GestureData)` 三 phase（Phase=Began 1 次 + Phase=Updated 多次 + Phase=Ended 1 次） | R3 PlayMode probe P2 case + listener spy assert |
| AC-3 | SingleFingerFSM 实现 drag threshold 距离判定（AccumulatedDist > DragThresholdPx → 进 Dragging state）+ tap timeout 时间判定（AccumulatedTime < TapTimeoutSeconds=0.25s → emit Tap）per ADR-010 spec + InputConfigFromLuban defaults；Tap vs Drag 0 误判（在 dragThreshold 以内 release → Tap；以外 → Drag） | R3 PlayMode probe P3 case rapid Mouse click + drag with delta < dragThresholdPx → expect Tap fire；with delta > dragThresholdPx → expect Drag fire |
| AC-4 | `GestureDispatcher.Dispatch(in GestureData)` production caller hit > 0（rg verify ≥1 production class 调，不只 EditMode test fixture） — **本 AC 是 V3.0.1 dp15 sniff sub-clause 试点的 第 1 个 production caller hit > 0 修复 case**（预期 Phase 2 implementation 后 InputService.Update Loop 内调 GestureDispatcher.Dispatch ≥1 site） | R2 grep audit + production code review |
| AC-5 | PlayMode probe ≥1 R3 case Mouse Tap → IGestureEvent.OnTap event round-trip + listener 收到 GestureData + GestureData.Type=Tap + GestureData.Phase=Ended + GestureData.ScreenPosition asserted ≈ click position（容忍 ±5px tolerance for fat finger compensation） + 全 ≥5 asserts PASS | R3 PlayMode probe evidence + JSON dump |
| AC-6 | 0 production unexpected console error/warning during Mouse Tap + Drag interaction（Application.logMessageReceived UnexpectedErrorCount==0；ExpectedLogSubstrings allowlist InputService Module init Log.Info ≤2 处） | R3 evidence dump |
| AC-7 | 0 mock fire bypass（spike + production code 0 直接 `GameEvent.Get<IGestureEvent>().OnTap(...)` 等 mock fire）—— V3.0.1 dp15 sniff sub-clause 试点 第 1 个 production caller hit > 0 修复 case verify | R2 grep verify (`rg 'GameEvent\.Get<IGestureEvent>' --type cs Assets/GameScripts` production caller list 全 InputService/MouseToTouchAdapter/GestureDispatcher.Dispatch 链) + spike code review |
| AC-8 | Phase 2.0 deficiency closure 第 1 项 — ADR-010 §Implementation Guidelines amend Step 9 "Editor Mouse Adapter" 5-10 行 spec wording 已落档 | ADR-010 read verify |
| AC-9 | Phase 2.0 deficiency closure 第 2 项 — sprint-status.yaml Sprint 2 SP-013 entry status amend "DONE → partial-fsm-only-driver-pending" + V3.0.1 dp11 candidate row append | sprint-status.yaml read verify |
| AC-10 | Phase 2.0 deficiency closure 第 3 项 — story-004 wording drift propagate fix 全部 review (本 amend 已完成大部分；Phase 2 implementation 时复审无残留 "GestureRecognizer FSM" / "ScreenPos" wording) | story-004 grep verify |

---

## R3 PlayMode Probe Plan

**Spike**: `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-13_InputPipelineWiring.cs` NEW (sprint-status.yaml S6-13 分配)；spike pattern 沿用 S6-04/S6-08 precedent (1 file + 3 inner class S613Spike : IDevSpike + S613Runtime + S613Tester + 5 R3 case + listener spy + JSON evidence dump WriteResultJson)；GameApp.cs RegisterDevSpikes 切换 S601PlaytestSpike → S613Spike Phase 2 implementation；DevTestState `[main-menu]` mode HasSpike(S6-13) +1 = V3.0.1 dp8 candidate +1 (现 5 spike 已 ≥4 阈值触达 Sprint 6 retro 强制评估 V3.1 trigger 候选)。

**5 R3 case Setup/Action/Assert (per ADR-029 V3 R3 standard)**:

- **P1 MouseSingleTapToOnTapEvent** (~5 asserts):
  - **Setup**: InputService Module 启动 + InitWithDefaults() + 注册 IGestureEvent_Event.OnTap listener spy；reset listener spy counter
  - **Action**: 通过 reflection 直接 inject TouchState (FingerId=0, Phase=Began, CurrentPosition=(500, 300), IsActive=true) → SingleFingerFSM.Update → wait 0.1s (< TapTimeoutSeconds=0.25s) → inject TouchState (Phase=Ended, same position) → SingleFingerFSM.Update → expect emit Tap → GestureDispatcher.Dispatch
  - **Assert**: tapFired==true + capturedTap.Type==Tap + capturedTap.Phase==Ended + capturedTap.ScreenPosition==(500,300) (容忍 ±1px) + capturedTap.TapCount==1

- **P2 MouseDragThreePhaseFire** (~7 asserts):
  - **Setup**: 注册 IGestureEvent_Event.OnDrag listener spy
  - **Action**: inject TouchState (Began at (100,100)) → 0.05s 后 inject (Moved to (200,200)) → 多帧 (Moved 增加位移) → inject (Ended at (300,300))
  - **Assert**: dragBeganCount==1 + dragUpdatedCount≥1 + dragEndedCount==1 + dragBeganPosition≈(100,100) + dragEndedPosition≈(300,300) + dragUpdatedDelta 总和 ≈ (200,200) (容忍 ±5px) + 0 unexpected Tap fire

- **P3 TapVsDragThresholdBoundary** (~6 asserts):
  - **Setup**: spy IGestureEvent.OnTap + OnDrag；reset counters；read DragThresholdPx (e.g., 3mm × 96dpi / 25.4 ≈ 11.3 px)
  - **Action 1 (within threshold → Tap)**: inject TouchState Began → Moved with delta=(5,0) (< 11.3px) → Ended within 0.2s → expect Tap fire
  - **Action 2 (over threshold → Drag)**: inject Began → Moved with delta=(20,0) (> 11.3px) → Ended → expect Drag.Began + Drag.Ended fire
  - **Assert**: action1 tapFired==true + action1 dragFired==false + action2 dragBeganFired==true + action2 dragEndedFired==true + action2 tapFired==false + threshold value > 0

- **P4 SingleFingerFSMStateTransitionVerify** (~5 asserts):
  - **Setup**: SingleFingerFSM CurrentState reflection access；reset
  - **Action**: inject Began (state Idle→Pending) → wait < TapTimeoutSeconds → inject Ended (state Pending→Idle 经 EmitTap)
  - **Assert**: 状态序列 Idle→Pending→Idle + EmitTap 1 次 + 0 异常 state transition

- **P5 NoMockFireBypassVerify (dp15 sniff sub-clause 试点)** (~6 asserts):
  - **Setup**: Application.logMessageReceived 监听 + listener spy with origin tag (production caller chain trace)
  - **Action**: 整 spike 与 InputService production code 任何路径 0 直接 `GameEvent.Get<IGestureEvent>().OnTap(...)` mock fire；P5 case 主动尝试 reflection 找 production code 路径
  - **Assert**: rg `GameEvent\.Get<IGestureEvent>` 仅 GestureDispatcher.Dispatch line 26/29/32/35/38 共 5 hit (production-only) + spike 0 直接调 + UnexpectedErrorCount==0 + AC-7 verify PASS + dp15 sniff sub-clause 第 1 个 production caller hit > 0 修复 case PASS + Sprint 6 retro 议题 6 input data point

---

## R2 Assumptions Validated（Phase 0/1 实证 ✅ DONE — DEFICIENCY-FLAGGED PASS）

| # | Assumption | Verify | Status |
|---|------------|--------|--------|
| R2.1 | Sprint 2 SP-013 SingleFingerFSM/DualFingerFSM 已实施完整 | Glob `Assets/GameScripts/HotFix/GameLogic/Input/*.cs` 实证 10 file FSM 层 + Driver 层 0 | ⚠️ **PARTIAL** (FSM ✅ + Driver ❌) — V3.0.1 dp11 同根 sprint backlog placeholder wording drift；Phase 2 closure deficiency 第 2 项 sprint-status.yaml SP-013 amend |
| R2.2 | Touch input pipeline production 实施现状 | rg `Input.touchCount\|Input.GetTouch` --type cs Assets/GameScripts | ❌ **CONFIRMED 0 production caller** — dp15 candidate observation 实证 |
| R2.3 | Mouse input pipeline production 实施现状 | rg `Input.GetMouseButton\|Input.mousePosition` --type cs Assets/GameScripts | ❌ **CONFIRMED 0 production caller** — dp15 candidate observation 实证 |
| R2.4 | GestureDispatcher.Dispatch production caller chain | rg `GestureDispatcher\.Dispatch` --type cs Assets/ | ❌ **CONFIRMED 0 production caller** (仅 EditMode `Tests/EditMode/InputSystem/GestureDispatchTests.cs` 6 hit) — V3.0.1 dp15 sniff sub-clause baseline |
| R2.5 | ADR-010 spec 边界 — Editor Mouse pipeline 是否 cover | Read ADR-010 §Decision + §Constraints + §Implementation Guidelines | ⚠️ **NEW DEFICIENCY** — ADR-010 spec 完全没 cover Editor Mouse pipeline；Phase 2 closure deficiency 第 1 项 ADR-010 §Implementation Guidelines amend Step 9 "Editor Mouse Adapter" 5-10 行 spec wording |
| R2.6 | GestureData struct 字段 list | Read GestureTypes.cs:29-42 实证 8 字段 | ✅ **CONFIRMED + wording drift** (本 story §Goal Flow + §AC + §R3 多处用 "ScreenPos" wording drift；Phase 2 closure deficiency 第 3 项 propagate fix；本 amend 已修正大部分；V3.0.1 Type-5 dp NEW 留观察 ≥3 unique dp 触发 V3.x sub-version) |
| R2.7 | InputConfig POCO + Luban TbInput 现状 | Read InputConfigFromLuban.cs (5 IInputConfig + 3 IDualFingerConfig property + InitWithDefaults() 8 default values + InitFromLuban 7 args 占位) | ⚠️ **PARTIAL** — InitWithDefaults() ✅ ready (sufficient chapter 1 fun loop)；Luban TbInputConfig 接入留 Sprint 7+ |
| R2.8 | Editor InputSystem package vs legacy Input | Read ProjectSettings.asset:918 `activeInputHandler: 0` (= legacy Input Manager Old；New Input System 未启用) | ✅ **CONFIRMED legacy Input only** — Mouse click 在 Desktop Editor 内不天然模拟 Touch；必须显式编 MouseToTouchAdapter Phase 2 production code |

**R2 Verdict**: ⚠️ **DEFICIENCY-FLAGGED PASS** per ADR-029 V2.0 §V2-1 R2 DEFICIENCY-FLAGGED PASS path — 6/8 ✅ + 2/8 ⚠️ PARTIAL (R2.1 Driver 0 + R2.7 Luban pending) + 1/8 ⚠️ NEW DEFICIENCY (R2.5 ADR-010 spec gap) — 共 3 项 deficiency 留 Phase 2 closure (与 §ADR Decision Summary deficiency closure list 同步)。

---

## V3.0.1 Watch List Hooks

**Type-11 V3.0.1 dp15 candidate "EditMode green ≠ production wired sniff"** — 本 story 是 dp15 候选 sniff sub-clause **第 1 个 production caller hit > 0 修复 case** ✅ **READINESS GATE BASELINE ESTABLISHED**：
- R2.4 grep verify ✅ confirm baseline (production 0 caller / EditMode 6 caller)；Phase 2 implementation 后 R2 grep 应见 `InputService.Update` Loop 内 ≥1 production GestureDispatcher.Dispatch caller
- R3 P5 NoMockFireBypassVerify case = sniff sub-clause 试点 production wiring 完整性校验
- Sprint 6 retro 议题 6 dp15 promote V3.x sub-version 决策依据之一（如本 story Phase 2 done + story-008 end-to-end-smoke-replay 试点成功 → promote）

**Type-11 V3.0.1 dp16 NEW candidate "ADR spec gap re Editor-only path"** ⚠️ **NEW (本 story Phase 0/1 readiness gate R2.5 实证触发)**：
- ADR-010 §Decision Layer 1 仅 cover Touch (`Input.GetTouch(i)`)；§Constraints "PC 端 (Steam) 后续阶段适配键鼠"；§Implementation Guidelines 9 config key 含 PC sensitivity 占位但无实施 spec —— **ADR-010 spec 完全没 cover Editor Mouse pipeline (Editor Play 内必经路径，非 production runtime)**
- 与 dp15 同根但**深一层**：dp15 = "EditMode green ≠ production wired" (production runtime caller hit > 0)；dp16 = "ADR spec gap re Editor-only auxiliary code path" (spec 没 cover 但 Editor 验证必需)
- Sprint 7+ 评估应用范围：所有 Editor-only 工具链 (Device Simulator / 各种 Editor adapter / Test fixture-side)；如 ≥3 unique dp 触发 promote V3.x sub-version + ADR 写作 standard amend "ADR §Implementation Guidelines 必含 Editor-only path 章节如适用"
- Sprint 6 retro 议题 6 关联议题 (与 dp15 同根 NEW 议题 7 候选)

**Type-10 V3.0.1 dp14 candidate "playtest infrastructure pattern gap"** — 本 story 不直接触发 dp14（PlaytestSpike pattern Phase 2.0 已 fix）；PlayMode probe spike 沿用 R3 standard 自动 RunAllAsync 模式（与 manual playtest 节奏不冲突，因为 PlayMode probe 与 manual playtest 是 separate session）。

**Type-5 V3.0.1 candidate "spec/tooling ↔ reality drift"** — Phase 0/1 实证触发 **留观察候选** (~~未触发~~)：
- 本 story 自身 wording drift ≥2 处 ("GestureRecognizer FSM" → "SingleFingerFSM/DualFingerFSM" 5 处 + "ScreenPos" → "ScreenPosition" 3 处) — 本 amend 已 propagate fix 大部分；如 Phase 2 implementation 时 review 仍发现残留 "GestureRecognizer FSM" / "ScreenPos" wording → V3.0.1 Type-5 dp NEW 累计计数 +1
- Sprint 5 retro 已 promote V3 Type-5 6 unique dp；本 story 自身 wording drift 不重复计数 (因 amend 即 closure)；如 ≥3 unique dp 同根 (S5-08 dp2/-3 + S5-02 dp4/-5/-6 + 本 story dp NEW 累计 ≥3) Sprint 6 retro 评估 promote V3.x sub-version

**Type-8 V3.0.1 dp1 candidate "DevTestState [main-menu] mode 复用阈值阶进"** — Phase 2 implementation 后 spike S6-13 加入 = +1 spike count → 5 (S5-02 + S6-07 + S6-08 + S6-04 + S6-13)；Sprint 6 retro 强制评估 V3.1 trigger pattern (`IDevSpike.IsMainMenuMode` flag + DevTestState central mode-dispatch refactor ~30-50 行) vs 保持显式 HasSpike list。

---

## Out of Scope（明示）

- ❌ **Multi-touch / Pinch / Rotate / LightDrag** — Sprint 7+ ADR-010 full epic（chapter 1 fun loop minimal 仅需 Tap + Drag）
- ❌ **Touch input 真机 testing** — Sprint 7+ mobile build prerequisite（Editor MouseToTouchAdapter 满足 Sprint 6 fun loop 验证）
- ❌ **InputBlocker listener-side singleton + InputManager class facade** — Sprint 7+ ADR-010 full epic（S5-05/S6-08 sender-side narrow scope 已 closure）
- ❌ **PC Mouse pipeline (Steam build) production driver** — 留 Sprint 7+ ADR-010 V2 amendment (本 story narrow scope 仅 Editor Play 验 chapter 1 fun loop；不 pull-forward PC Steam 整套 PC Mouse pipeline)
- ❌ **DualFingerFSM Update Loop wiring** — Sprint 7+ multi-touch full epic (本 story 仅 wire SingleFingerFSM；DualFingerFSM 留 ADR-010 V2)
- ❌ **InputBlocker listener-side runtime check (PushBlocker/PopBlocker stack consumed by InputService Update Loop)** — 仅 sender-side fire 已 by S6-08 cover；listener-side `if (blockerStack.Count > 0) discard input` 留 Sprint 7+ InputManager epic
- ❌ **Luban TbInputConfig 接入** — InitWithDefaults() 暂用 GDD defaults 满足 chapter 1；Luban 接入留 Sprint 7+ (`InitFromLuban` 已存占位)
- ❌ **Settings menu input remap (touch sensitivity slider)** — S5-09 backlog（Sprint 7+ Production stage 后 polish phase；InputConfigFromLuban.OnSensitivityChanged hook 已存）

---

## Implementation Notes（Phase 0/1 R2 verify 后 amend 精细 ✅）

**预计涉及文件 (~3-5 hr Phase 2 implementation 估时)**：

1. **`Assets/GameScripts/HotFix/GameLogic/Input/InputService.cs` NEW** ~80-120 行
   - TEngine Module pattern (复用 SceneManager / AudioManager 模式)；注册到 GameModule.Input (待 GameModule 暴露 Input accessor 或 沿用 Singleton<InputService> pattern per InputBlocker.cs 既有 precedent)
   - 持有 `SingleFingerFSM` + `DualFingerFSM` + `InputConfigFromLuban`
   - 核心 `Update()` Loop: 调 MouseToTouchAdapter (Editor Play only) 或 Touch sampling (real device) → 构造 TouchState → fed into FSM → if FSM emits gesture → GestureDispatcher.Dispatch
   - InputBlocker 检查 (FSM 之前) + InputFilter 检查 (FSM 之后) per ADR-010 §Architecture Layer 2 hierarchy；本 story narrow scope 仅 wire blocker check 与 FSM；filter 检查留 Sprint 7+ Tutorial epic
   - `OnSensitivityChanged` hook (复用 InputConfigFromLuban.OnSensitivityChanged)；初版可选不 wire 留 Sprint 7+

2. **`Assets/GameScripts/HotFix/GameLogic/Input/MouseToTouchAdapter.cs` NEW** ~50-80 行
   - `#if UNITY_EDITOR` 或 `Application.isEditor` runtime check (Sprint 7+ Touch 真机 testing 时 disable adapter；fallback 到 Touch sampling)
   - Update Loop 内调 `Input.GetMouseButtonDown(0)` → emit TouchState (Phase=Began) / `Input.GetMouseButton(0)` → emit TouchState (Phase=Moved/Stationary based on delta) / `Input.GetMouseButtonUp(0)` → emit TouchState (Phase=Ended) + `Input.mousePosition` → CurrentPosition
   - 单指模式 (FingerId=0)；多指 (Pinch/Rotate Editor) 留 Sprint 7+
   - 0 GC allocation (TouchState struct + reusable buffer)

3. **`Assets/GameScripts/HotFix/GameLogic/GameApp.cs`** Init Phase amend
   - InitializeInputService() 调用 (与 SceneManager / AudioManager 同 init phase)
   - InitWithDefaults() 暂用 GDD defaults (Luban TbInputConfig 留 Sprint 7+)
   - dp14 candidate / dp16 candidate 注释引用 (governance trail)

4. **`Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-13_InputPipelineWiring.cs` NEW** ~500-700 行
   - 1 file + 3 inner class (S613Spike : IDevSpike + S613Runtime : MonoBehaviour + S613Tester) 沿用 S6-04/S6-08 precedent
   - 5 R3 case (P1~P5 per §R3 PlayMode Probe Plan section detail)
   - listener spy (IGestureEvent_Event.OnTap / OnDrag) + Application.logMessageReceived UnexpectedErrorCount + ExpectedLogSubstrings allowlist (~5-10 InputService Module init Log.Info pattern)
   - reflection helper 直接 inject TouchState into SingleFingerFSM (绕过 MouseToTouchAdapter Editor Mouse hardware dependency；R3 Editor automation reliable)
   - JSON evidence dump WriteResultJson per `~/Library/Application Support/DefaultCompany/Unity/S6-13_Result.json` schema

5. **`Assets/GameScripts/HotFix/GameLogic/GameApp.cs`** RegisterDevSpikes amend
   - S601PlaytestSpike → S613Spike 切换 (S6-01 注释行保留作 manual playtest 复跑入口)
   - dp14/dp16 candidate 注释引用

6. **`Assets/GameScripts/HotFix/GameLogic/DevTest/DevTestState.cs`** amend
   - `[main-menu]` mode HasSpike list +1 = 5 (S5-02 + S6-07 + S6-08 + S6-04 + **S6-13**) — V3.0.1 dp8 candidate **阈值 4 已远超达** Sprint 6 retro 强制评估 V3.1 trigger DevTestState central mode-dispatch refactor 候选

7. **`docs/architecture/adr-010-input-abstraction.md` amend Step 9** ~5-10 行
   - §Implementation Guidelines 节末加 "**9. Editor Mouse Adapter (Sprint 6 emergent fix Track F NEW pull-forward)**" 段
   - Spec wording: "Editor Play (development workflow only) 内必备 MouseToTouchAdapter 把 Mouse click → 模拟 TouchState fed into SingleFingerFSM；Sprint 7+ Touch 真机 testing 时 disable adapter；adapter 不进入 production runtime build"
   - V3.0.1 dp16 candidate "ADR spec gap re Editor-only path" closure 第 1 个实战触发；governance trail § Last Verified 字段 amend + § History entry append

8. **`src/MyGame/ShadowGame/production/sprint-status.yaml`** amend
   - Sprint 2 SP-013 entry status amend "DONE → partial-fsm-only-driver-pending-sprint-6-emergent-fix"
   - V3.0.1 dp11 candidate row append closure 第 1 个实战触发记录
   - V3.0.1 dp16 candidate row append (NEW Type-11 同根但深一层)

**0 vendor patch 决策**：本 story 不改 TEngine vendor (Assets/TEngine/) — 所有改动限定 GameLogic 层。

---

## History

- **2026-05-14 morning continue (Session 32)**: Draft 创建（emergent fix epic Track F NEW story-004~008 outline approved per [A]）；Status: Draft；Phase 0 R2 vendor reality verify pending；本 story 是 V3.0.1 dp15 sniff sub-clause 试点第 1 个 production caller hit > 0 修复 case；ADR-010 部分 scope pull-forward governance justification = block S6-02/S6-03/S6-10。
- **2026-05-14 afternoon (Session 32 batch readiness gate continue per [A])**: Phase 0/1 R1+R2+R3 readiness gate 完整跑通 (~45-60 min)；R1 ✅ PASS (forbidden listener pattern grep audit 7 项全 compliant — InputService Module pattern 不引入 Camera.main / FindObjectsOfType / Input.GetTouch outside InputService 等 forbidden) + R2 ⚠️ **DEFICIENCY-FLAGGED PASS** per ADR-029 V2.0 §V2-1.b path (6/8 ✅ + 2/8 ⚠️ PARTIAL R2.1 Driver 0 + R2.7 Luban pending + 1/8 ⚠️ NEW DEFICIENCY R2.5 ADR-010 spec gap re Editor Mouse pipeline) + R3 ✅ PASS (5 R3 case Setup/Action/Assert + JSON schema spec consistency + stub type construction signatures 全 production-ready)；Status: Draft → DEFICIENCY-FLAGGED PASS ready for Phase 2 transition；amend 13 sections (Status header + Depends on + §Context + §Goal Flow T0~T6 wording propagate fix MouseToTouchAdapter + SingleFingerFSM + ScreenPosition + §ADR Decision Summary 加 Phase 2 deficiency closure list 3 项 + §Engine Notes R2.1~R2.8 verdict 实证 update 含 Phase 2 production wiring file list 6 项 + §Control Manifest Rule References ⚠️ TBD → ✅ verified 7 项 + §Acceptance Criteria 7→10 (AC-8/-9/-10 deficiency closure verification) + §R3 PlayMode Probe Plan 5 case Setup/Action/Assert detail + §R2 Assumptions Validated 表 + §V3.0.1 Watch List Hooks **Type-11 dp16 NEW candidate 'ADR spec gap re Editor-only path'** 触发 (与 dp15 同根但深一层) + Type-5 wording drift 留观察 + Type-8 dp1 阈值 5 触达 + §Out of Scope 加 PC Mouse pipeline + DualFingerFSM + InputBlocker listener-side + Luban TbInputConfig 等 4 项 + §Implementation Notes 8 项文件清单精化 + §History entry append)；**3 项 Phase 2 deficiency closure**: (1) ADR-010 §Implementation Guidelines amend Step 9 'Editor Mouse Adapter' ~5-10 行 spec wording (V3.0.1 dp16 NEW candidate 第 1 个实战触发 closure) + (2) sprint-status.yaml Sprint 2 SP-013 status amend 'DONE → partial-fsm-only-driver-pending-sprint-6-emergent-fix' (V3.0.1 dp11 sprint backlog placeholder wording drift candidate 第 1 个实战触发 closure) + (3) story-004 wording drift propagate fix 残留 review (本 amend 已修正大部分 'GestureRecognizer FSM' / 'ScreenPos' wording；Phase 2 implementation 时复审无残留)；**Phase 2 transition prerequisite**: user 决定立即启动 Phase 2 Implementation (产 InputService.cs + MouseToTouchAdapter.cs + Spike S6-13 + ADR-010 amend Step 9 + sprint-status.yaml SP-013 amend) ~3-5 hr vs 收尾本 session (governance debt 防累积 ≤5 hr hard rule)；本 readiness gate amend 投入 ~45-60 min；本 session 总投入 ~3-3.5 hr (Track F batch ~50 min + readiness gate ~50 min + 本 amend ~30 min ≈ ~3.5 hr) 接近 ≤5 hr hard rule 边界但仍合规。
