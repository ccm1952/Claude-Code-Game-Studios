// 该文件由Cursor 自动生成

# Story 008: End-to-End Smoke Replay — S5-02 Spike Rewrite (No Mock Fire) + V3.0.1 dp15 Sniff Sub-clause Pilot

> **Epic**: VS Chapter 1
> **Story ID**: vs-chapter-1-008 (Sprint 6 emergent fix Track F NEW；S6-01 Phase 2.1 manual playtest NEEDS-WORK 派生 — V3.0.1 dp15 sniff sub-clause 试点 final pilot)
> **Sprint**: 6 (Track F NEW — chapter 1 production wiring emergent fix)
> **Story Type**: Integration (R3 spike 重写 + V3.0.1 dp15 sniff sub-clause 试点)
> **Complexity Points**: 1 (~1 hr 估时；spike rewrite 不改 production code)
> **GDD Requirement**: 无 GDD TR-ID 直接锚点 — 本 story 是 governance 试点 (V3.0.1 dp15 sniff sub-clause 0 mock fire bypass 校验)
> **ADR References**: **ADR-029 V3.0.1 R3 standard amend candidate "production caller hit > 0 sniff sub-clause"** + ADR-027 §3 IGestureEvent + §4 IInteractionEvent + §5 IPuzzleEvent + ADR-012 ShadowMatchCalculator + ADR-013 ObjectInteraction + ADR-014 PuzzleStateMachine + ADR-016 NarrativeSequence + ADR-017 AudioManager + ADR-030 §VS Build commit
> **Status**: 📝 **Draft** (2026-05-14 Session 32 morning continue — emergent fix epic Track F NEW story-004~008 outline approved per [A]，本 story Phase 0 R2 vendor reality verify pending)
> **Created**: 2026-05-14 morning continue (Sprint 6 Session 32 — emergent fix Track F NEW fifth & final story)
> **Completed**: ""
> **Depends on**: story-004 ✅ (input pipeline) + story-005 ✅ (scene wiring) + story-006 ✅ (provider injection) + story-007 ✅ (ShadowMatch wire) — 本 story 是 Track F serial 最后一个，验证前 4 stories 累计 production wiring 完整性

---

## Context

**S6-01 Phase 2.1 manual playtest NEEDS-WORK 派生 emergent fix Track F 最后 story (~1 hr — V3.0.1 dp15 sniff sub-clause 试点 final pilot)**：

S5-02 spike (`Spikes/S5-02_EndToEndFlow.cs`) Phase 3 第 1 跑当时 P2 Action 设计为 `fire mock OnMatchScoreUpdated(1, 0.95)` 简化路径，绕过 ObjectInteraction → ShadowMatch 整条链路 (Session 27 #2 R2.3 RESOLVED [A] decision — 当时合理：vertical slice happy path 验 5 systems mechanical 串通)。

但 S6-01 Phase 2.1 manual playtest 揭露这个 mock fire 简化路径让 R3 5/5 PASS 掩盖了 5 处 production wiring gap。**V3.0.1 dp15 candidate "EditMode green ≠ production wired sniff"** 提案 ADR-029 V3 R3 standard amend "production caller hit > 0 sniff sub-clause" 防类似 drift。

本 story 是 dp15 sniff sub-clause **试点 final pilot** —— 重写 S5-02 spike 不依赖 mock fire OnMatchScoreUpdated，改用前 4 stories (story-004~007) 累计的 production wiring 完整 round-trip simulate Mouse Tap+drag → match → score → narrative。如试点 5/5 PASS → dp15 promote V3.x sub-version + ADR-029 V3 R3 standard amend 沉淀；如 fail → 回退评估 + sniff sub-clause spec amend。

### S5-02 spike P2 mock fire 路径 vs story-008 production wiring 路径对比

| 阶段 | S5-02 spike P2 (Session 27 #3 done) | story-008 重写 (本 story) |
|---|---|---|
| Setup | chapter 1 baseline + InteractableObject 0 component (S0-3) + ShadowMatchCalculator 0 listener (S0-5) | story-004~007 done — InteractionCoordinator + InteractableObject MonoBehaviour 挂载 + ShadowMatchCalculator listener wired |
| Action | spike 直调 `GameEvent.Get<IShadowPuzzleEvent>().OnMatchScoreUpdated(1, 0.95)` mock fire | spike 用 InputSimulation Mouse drag Object_01 to perfect-match position → 自然 fire OnMatchScoreUpdated chain |
| Assert | listener spy 收到 mock fire 1 次 | listener spy 收到 ShadowMatchCalculator.Calculate 自然 fire ≥3 次 + final score ≥0.95 + OnPerfectMatch fire 1 次 |
| dp15 sniff | ❌ FAIL (mock fire 路径 — 0 production wiring caller) | ✅ PASS (production wiring caller > 0 — InteractableObject.SnapRotation fire OnObjectTransformChanged → ShadowMatchCalculator.Calculate → OnMatchScoreUpdated) |

### Sprint 6 emergent fix Track F serial 最后 story

```
story-004 (input pipeline) → story-005 (scene wiring) → story-006 (provider injection) → story-007 (ShadowMatch wire) → story-008 (本 story — end-to-end smoke replay 试点 dp15 sniff sub-clause)
```

---

## Goal Flow (T0 → T8)

```
T0  story-004~007 全部 ✅ done; chapter 1 production wiring 完整 round-trip
T1  Editor PlayMode start → main.unity boot → DevTestState [main-menu] mode → S6-XX (本 story spike) launch
T2  Spike Awake/Start → subscribe early listeners (S5-02 spike pattern precedent — listener spy 5 events)
T3  Spike runtime → use InputSimulation API (Editor InputSystem package or LegacyInputModule.Simulate) Mouse Tap on NewGame Button → ISceneEvent.OnRequestSceneChange(1) fire → chapter 1 loaded
T4  Spike runtime → use InputSimulation Mouse Tap on Object_01_CoffeeMug screen position → IGestureEvent.OnTap fire → InteractionCoordinator OnTap raycast hit Object_01 → fsm.OnTapHit
T5  Spike runtime → use InputSimulation Mouse hold + drag from Object_01 start pos to perfect-match target pos → IGestureEvent.OnDrag fire 三 phase → InteractableObject.OnDrag → fsm.TransitionTo(Dragging) → transform 更新连续
T6  InteractableObject.SnapRotation OnComplete → fire OnObjectTransformChanged → ShadowMatchCalculator listener → Calculate(...) → fire OnMatchScoreUpdated continuous → 最终 score ≥ 0.95 → fire OnPerfectMatch
T7  PuzzleStateMachine OnPerfectMatch listener → fsm.TransitionTo(Complete) → NarrativeSequencePlayer OnPerfectMatch listener → HandleRequestSequence(1, MemoryReplay) → narrative 触发
T8  Spike WriteResultJson + AC verify (per S5-02 spike P4 reflection precedent — GameModule.Audio.MusicVolume=0.300 ducking + 0 unexpected console error)
```

---

## ADR Decision Summary

**ADR-029 V3.0.1 R3 standard amend candidate "production caller hit > 0 sniff sub-clause"** —— 本 story 是 **正式试点 pilot**：
- 试点 spec 草案: R3 PlayMode probe spike 设计前 + Phase 1 readiness gate 必加 R2 grep verify `rg 'XxxDispatcher\.Yyy' --type cs` 或 `class XxxManager.*MonoBehaviour` production caller > 0 = sniff PASS；= 0 (仅 EditMode test fixture) → 黄牌警告 spike 路径可能 mock 绕过 wiring gap，强制评估补 R3 production wiring sniff case
- 本 story 应用范围: IGestureEvent / IInteractionEvent / IPuzzleEvent (5 sender → 5 listener 完整 round-trip)
- 试点验证标准: spike + production code 任何路径 0 调 `GameEvent.Get<IXxxEvent>().Yyy(...)` mock fire；listener spy expect 仅自然 production wiring 路径 fire (assert origin tag != 'mock')

**ADR-027 §3/§4/§5 inherit** — 本 story 不 amend：所有 IGestureEvent / IInteractionEvent / IPuzzleEvent 走自然 sender 路径 fire (Sprint 2/4/5 done)。

**ADR-012 + ADR-013 + ADR-014 + ADR-016 + ADR-017 inherit** — 本 story 不 amend：依赖 story-004~007 production wiring done。

---

## Engine Notes

**待 /story-readiness gate Phase 0 R2 vendor reality verify**：
- R2.1 ⚠️ TBD: Editor InputSimulation API 现状 — 是否 InputSystem package (com.unity.inputsystem) 提供 `Mouse.current.leftButton.press` simulate / 还是 LegacyInputModule + 反射 mock Input static class
- R2.2 ⚠️ TBD: PlayMode test InputSimulation 时序约束 — frame 内 simulate 是否需 yield return + WaitForFrames (per S5-02 spike P5 chapter 1 → chapter 2 transition wait pattern precedent)
- R2.3 ⚠️ TBD: Object_01 perfect-match target screen position — 本 story spike 需知道 perfect match 在 chapter scene 内的 world position (camera viewport transform)
- R2.4 ⚠️ TBD: Time budget — InputSimulation 5 step (NewGame click → Object_01 Tap → Drag start → Drag continuous → Drag end) 累计时长 < 2s 内
- R2.5 ⚠️ TBD: S5-02 spike (Spikes/S5-02_EndToEndFlow.cs) 当前 P2 case Action / Assert wording (rewrite 范围 verify) — 是 amend 还是另起 spike
- R2.6 ⚠️ TBD: GameApp RegisterDevSpikes 切换 S601PlaytestSpike → 本 story spike (per Phase 2.0 precedent — manual playtest spike 与 R3 spike 互斥)

---

## Control Manifest Rule References

**待 /story-readiness gate Phase 1 R1 grep audit**：
- ⚠️ TBD: Required — Spike 必须用 InputSimulation API simulate Mouse Tap+Drag (NOT 直调 GameEvent fire)
- ⚠️ TBD: Required — listener spy 必须 origin tag 区分 production fire vs mock fire (per V3.0.1 dp15 sniff sub-clause)
- ⚠️ TBD: Forbidden — Spike 直调 `GameEvent.Get<IShadowPuzzleEvent>().OnMatchScoreUpdated(...)` 等任何 mock fire bypass

---

## Acceptance Criteria

| # | AC | Verify path |
|---|-----|------|
| AC-1 | Spike 重写 S5-02 P2 case (or NEW S6-XX_EndToEndSmokeReplay.cs spike — R2.5 决定) — 0 调 `GameEvent.Get<I*Event>().*(...)` mock fire | rg `GameEvent\.Get<I.*Event>\(\)\.(\w+)\(` Spike 文件 verify 0 mock fire |
| AC-2 | Spike 用 InputSimulation API simulate Mouse Tap (NewGame button) → chapter 1 loaded (production OnRequestSceneChange path) | R3 PlayMode probe verify spike code 调 InputSimulation |
| AC-3 | Spike 用 InputSimulation Mouse Tap + Drag on Object_01 → IGestureEvent.OnTap + OnDrag fire 自然链 | R3 listener spy verify origin == 'production' |
| AC-4 | InteractableObject.SnapRotation fire OnObjectTransformChanged 自然链 (NOT spike fire mock) | R3 listener spy verify origin == 'production' + count > 0 |
| AC-5 | ShadowMatchCalculator.Calculate fire OnMatchScoreUpdated 自然链 ≥3 次 + final score ≥0.95 | R3 listener spy + state tracking |
| AC-6 | OnPerfectMatch fire exactly 1 次 (NOT 2 或 0) per puzzle | R3 listener spy + count assert |
| AC-7 | NarrativeSequencePlayer HandleRequestSequence(1, MemoryReplay) trigger → audio_duck (MusicVolume=0.300) per S5-02 P4 precedent | R3 reflection 实测 GameModule.Audio.MusicVolume |
| AC-8 | Total spike round-trip elapsed < 5s (vs S5-02 baseline 3844ms < 10s) | R3 evidence dump |
| AC-9 | 0 unexpected console error/warning during full round-trip | R3 evidence dump UnexpectedErrorCount==0 |
| AC-10 | dp15 sniff sub-clause 试点 verdict: PASS (5/5 R3 case + 0 mock fire bypass + production wiring 完整) → unlocks Sprint 6 retro V3.x sub-version promote 决策 | Sprint 6 retro 议题 6 input |

---

## R3 PlayMode Probe Plan（待 /story-readiness gate amend detail）

预计 spike `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-XX_EndToEndSmokeReplay.cs` (or amend 既有 S5-02 spike — R2.5 决定)：

- **P1 InputSimulationMouseTapNewGame** — InputSimulation Mouse click NewGame button → chapter 1 loaded + state=Idle (production OnRequestSceneChange path)
- **P2 InputSimulationObjectInteractionToShadowMatch** — InputSimulation Mouse Tap + Drag Object_01 to perfect-match position → OnObjectTransformChanged fire + OnMatchScoreUpdated fire ≥3 次 + final score ≥0.95
- **P3 PuzzleStateMachineToNarrative** — P2 finished → OnPerfectMatch fire 1 次 → fsm.Complete → narrative trigger
- **P4 NarrativeSequenceWithAudioDuck** — narrative trigger → reflect GameModule.Audio.MusicVolume (per S5-02 P4 reflection precedent)
- **P5 dp15SniffSubClauseVerify** — verify spike + production code 0 mock fire bypass (origin tag listener spy + assert all events origin=='production')

---

## R2 Assumptions Validated（待 Phase 0 实证）

| # | Assumption | Verify | Status |
|---|------------|--------|--------|
| R2.1 | Editor InputSimulation API 现状 | Read Packages/manifest.json + Editor InputSystem docs | ⚠️ TBD |
| R2.2 | PlayMode InputSimulation 时序 | Read S5-02 spike P5 wait pattern + InputSystem docs | ⚠️ TBD |
| R2.3 | Object_01 perfect-match target screen pos | Read Chapter_01_Approach.unity + ShadowMatchCalculator algorithm | ⚠️ TBD |
| R2.4 | Time budget round-trip < 5s | R3 sample run estimate | ⚠️ TBD |
| R2.5 | S5-02 spike P2 amend vs S6-XX NEW | Read S5-02_EndToEndFlow.cs P2 case | ⚠️ TBD |
| R2.6 | GameApp RegisterDevSpikes 切换 manual playtest spike vs R3 spike 互斥 | Read GameApp.cs | ⚠️ TBD |

---

## V3.0.1 Watch List Hooks

**Type-11 V3.0.1 dp15 candidate "EditMode green ≠ production wired sniff"** — 本 story 是 dp15 候选 sniff sub-clause **正式试点 pilot**：
- 如本 story 5/5 R3 PASS + 0 mock fire bypass → dp15 promote V3.x sub-version + ADR-029 V3 R3 standard amend 沉淀
- 如 R3 fail / mock fire bypass surface → 回退评估 + sniff sub-clause spec amend
- Sprint 6 retro 议题 6 dp15 promote 决策 input

**Type-2 (c) V3 candidate "framework behavior frequency"** — 留观察 — InputSimulation API 时序约束如 frame 内多 simulate 干扰，是否需 amend ADR-010 spec 增 frame budget guard。

**Type-9 V2.0 §V2-1.b "Interface Method Set Fan-out Check"** — 应用 — IGestureEvent + IInteractionEvent + IPuzzleEvent fan-out check verify 5 sender → 5 listener 完整 round-trip 不漏。

---

## Out of Scope（明示）

- ❌ **本 story 不改 story-004~007 production code** (本 story 仅 spike rewrite + R3 verify)
- ❌ **Touch input 真机 simulate** — Sprint 7+ ADR-010 full epic mobile testing；本 story 仅 Editor Mouse simulate
- ❌ **多 chapter end-to-end smoke replay** (chapter 2-5) — chapter 1 limited scope，留 Sprint 7+ chapter 2 epic vertical slice
- ❌ **Save / Load round-trip smoke** — chapter-state epic Sprint 6+ 范畴
- ❌ **dp15 sniff sub-clause spec 文档化** — Sprint 6 retro 议题 6 决策后 ADR-029 V3.0.x sub-version amend (separate governance work，本 story 仅试点 pilot)

---

## Implementation Notes（高层结构，待 Phase 0/1 R2 verify 后 amend 精细）

预计涉及文件：
1. **(R2.5 决定 - 路径 A) Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-02_EndToEndFlow.cs** — amend P2 case (rewrite no mock fire)
2. **(R2.5 决定 - 路径 B) Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-XX_EndToEndSmokeReplay.cs** NEW — fresh spike (与 S5-02 共存，作为 dp15 试点 pilot 独立 evidence)
3. **GameApp.cs** RegisterDevSpikes 切换 S601PlaytestSpike → 本 story spike (or 加 explicit dev menu route — 不强制切换 manual playtest spike)

**0 production C# change (chapter 1 wiring) 决策**：本 story 不改 story-004~007 production code；仅 spike rewrite + R3 verify。

---

## History

- **2026-05-14 morning continue (Session 32)**: Draft 创建（emergent fix epic Track F NEW story-004~008 outline approved per [A]）；Status: Draft；本 story 是 Track F serial 最后 1 story (~1 hr)；V3.0.1 dp15 sniff sub-clause **正式试点 pilot**；试点结果输入 Sprint 6 retro 议题 6 dp15 promote V3.x sub-version 决策。
