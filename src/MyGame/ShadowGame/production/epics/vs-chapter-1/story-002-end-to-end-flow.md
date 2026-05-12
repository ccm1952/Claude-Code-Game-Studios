// 该文件由Cursor 自动生成

# Story 002: Chapter 1 end-to-end 5 系统串通可玩（happy path）

> **Epic**: VS Chapter 1
> **Status**: **Ready** *(2026-05-12 Session 27 #1 — /story-readiness gate R1+R2+R3 amendment per 决策 [A]：R1 ✅ 0 forbidden listener pattern / R2 DEFICIENCY-FLAGGED PASS (R2.1 wording drift 修复 + R2.2 wiring gap explicit Required Framework Extension flag 标记 + R2.3 chapter scene 物件 component 留 dev-story Phase 1.5 unity-mcp 实查 + R2.4/R2.5 PASS) / R3 ✅ propagate fix P2/P3 Assert wording 完成)*
> **Layer**: Vertical Slice (VS) — cross-system integration
> **Type**: Integration
> **SP**: 2 *(happy path only — error/restart path 拆 S5-02b backlog Sprint 6)*
> **Manifest Version**: 2026-05-09

---

## Context

**Trigger**:

Sprint 5 Track A 收官 + ADR-030 §VS Build commitment 第 1 项核心交付（~85% → 100%）。前置 6 stories 全 ✅：

- **S5-01** ✅ Chapter 1 (`靠近`) Unity scene 实体构建首版
- **S5-1b** ✅ SceneManager boot pipeline 接入 + fixture ChapterDataProvider
- **S5-1c** ✅ ADR-009 production listener-path driver 接入（F4 dev-only stub 永久移除）
- **S5-03** ✅ S4-01 Puzzle State Machine production code（PuzzleStateMachine + IShadowMatchEvent + IShadowPuzzleEvent + IPuzzleLockEvent 4 contracts；8/8 R3 PASS）
- **S5-05** ✅ S4-02 Narrative Sequence Engine production code（NarrativeSequencePlayer + 3 atomic effects + INarrativeEvent + IInputBlockerEvent；10/10 R3 PASS v2）
- **S5-06** ✅ S4-03 Audio Manager Init production code（AudioManager + AudioMixLayer + DuckingController + IAudioEvent 8 method；10/10 R3 PASS first-run）

本 story 是 Sprint 5 Track A 收官 story，完成后 Sprint 5 Must Have 收官 + Sprint 6 playtest 启动门 unblock。

**Goal**: 跑通 chapter 1 happy path 1 success path 完整可玩 ——

```
T0 main menu visible (UIModule base canvas + 'Start Chapter 1' Button + 'Next Chapter' Button)
T1 user click 'Start Chapter 1' → ISceneEvent.OnRequestSceneChange(1) dispatch
T2 SceneManager.OnRequestSceneChange handler (S5-1c) → DriveTransitionAsync(1).Forget()
T3 BeginTransitionAsync 11-step → OnSceneTransitionEnd(1) fires
T4 chapter 1 scene active；spike auto-drag InteractableObject (Object_01_CoffeeMug)
T5 InteractableObject snap → IShadowMatchEvent.OnNearMatchEnter (chapter 1 puzzle 1 token)
T6 PuzzleStateMachine: Active → NearMatch → PerfectMatch → Complete
T7 IShadowPuzzleEvent.OnPuzzleComplete fires
T8 NarrativeSequencePlayer listener → INarrativeEvent.OnRequestSequence(chapter 1 puzzle 1 sequence)
T9 Sequence start: AudioDuckingEffect + ScreenFadeEffect + WaitEffect 串行
T10 AudioManager listen IAudioEvent.OnDuckingRequest → GameModule.Audio Music ducking real dispatch
T11 INarrativeEvent.OnSequenceComplete fires
T12 user click 'Next Chapter' Button → ISceneEvent.OnRequestSceneChange(0) (chapter 0 = main menu return)
T13 SceneManager unload chapter 1 → OnSceneUnloadBegin → OnSceneTransitionEnd(0) → state=Idle
```

**5 P1 系统**：Scene Mgmt + Object Interaction + Shadow Puzzle + Narrative + Audio。

**GDD**:

- `design/gdd/scene-management.md` — 11-step transition / chapter 1 boot path
- `design/gdd/object-interaction.md` — chapter 1 物件 drag/snap/select
- `design/gdd/shadow-puzzle-system.md` — puzzle near-match → complete
- `design/concept/shadow-memory.md` — chapter 1 "靠近" 关系叙事

**Requirement TR-IDs**:

- `TR-scene-005` — 11-step transition flow（per S5-1c handler-path 自驱）
- `TR-objint-002` — Chapter 1 仅 2 个可操作物件 + 1 个固定光源（per concept.md line 106）
- `TR-puzzle-005/006/008/013/014` — Puzzle State Machine 7 transition + hysteresis + irreversibility（per S5-03 ✅）
- `TR-narr-002/003/005/006/007/008/009/010` — Narrative sequence engine（per S5-05 ✅）
- `TR-audio-001..010` — Audio mix architecture（per S5-06 ✅）
- `TR-vs-chapter-1-end-to-end` ⚠️ **DEFICIENCY-FLAGGED 2026-05-11** — 该 TR 仅出现于本 story 描述跨 5 系统的 chapter 1 success path contract；按 ADR-029 V2.0 deficiency-flag 协议显式标记等待下次 `/architecture-review` 注册（不阻塞 dev-story）

**ADR Governing Implementation**:

- **ADR-009 (Accepted)** Scene Lifecycle — 由 S5-1b/1c verified
- **ADR-013 (Accepted)** Object Interaction FSM — S2-08 ✅ + S4-06 ✅ EditMode
- **ADR-014 (Accepted)** Puzzle State Machine — S5-03 ✅ DONE
- **ADR-016 (Accepted)** Narrative Sequence Engine — S5-05 ✅ DONE
- **ADR-017 (Accepted)** Audio Mix Architecture — S5-06 ✅ DONE
- **ADR-027 (Accepted)** Event Layer — per-event listener pattern + V2-5 listener self-removal
- **ADR-029 V2.0 (Accepted) R3 mandatory** — Integration type 必须 PlayMode probe
- **ADR-030 (Accepted)** §VS Build commitment 第 1 项核心交付（chapter 1 end-to-end + ≥3 playtest sessions Sprint 5-6）
- **ADR-011 (Accepted)** UIModule UGUI — S5-08 prerequisite

**Engine Notes** *(R2 grep 实证完成 2026-05-12 Session 27 #1 per ADR-029 V2.0 /story-readiness gate；5 大块 wiring uncertainty 实测结果见 §Assumptions Validated 表)*:

- `ISceneEvent.OnRequestSceneChange(int)` listener handler 由 S5-1c verified（SceneManager 自闭环 DriveTransitionAsync）
- **⚠️ WORDING DRIFT 修复 (R2.1 Session 27 #1 实测)**: 接口归属错位 — story 原写 `IShadowMatchEvent.OnNearMatchEnter` 但实际：
  - `IShadowMatchEvent` (IShadowMatchEvent.cs:21) 仅含 1 method `OnMatchScoreUpdated(int puzzleId, float newScore)` (sender: future ADR-012 `ShadowMatchCalculator`；MVP 阶段 spike fire mock；listener: PuzzleStateMachine)
  - `OnNearMatchEnter(int puzzleId)` 位于 **`IShadowPuzzleEvent`** (IShadowPuzzleEvent.cs:23)，**`PuzzleStateMachine.cs:228` 是 sender 不是 listener**
  - **实际正确链路**: T5 InteractableObject snap → `IShadowMatchEvent.OnMatchScoreUpdated(puzzleId=1, score)` (MVP spike fire) → `PuzzleStateMachine.cs:119` `AddEventListener<int, float>(IShadowMatchEvent_Event.OnMatchScoreUpdated, _onMatchScoreUpdated)` ✅ listener 已挂 → state transit → 派发 `IShadowPuzzleEvent.OnNearMatchEnter(puzzleId=1)` (PuzzleStateMachine 是 sender)
  - payload 类型: `int puzzleId` 不是 string `token`；本 story 全文 token wording 同步 propagate fix 至 puzzleId
- **❌ WIRING GAP (R2.2 Session 27 #1 实测) → `**Required Framework Extension**` flag**:
  - `IShadowPuzzleEvent.OnPuzzleComplete` listener handler 在 production code **0-hit** (`rg "AddEventListener.*OnPuzzleComplete" Assets/.../HotFix` → 0 matches)
  - `NarrativeSequencePlayer.cs:127-132` 仅 listen `INarrativeEvent.OnRequestSequence`，无 `OnPuzzleComplete` listener
  - **chain 断裂**: PuzzleStateMachine.cs:284 派发 `OnPuzzleComplete` ✅ → ❓ 无 bridge → NarrativeSequencePlayer 等不到 `OnRequestSequence`
  - **Required Framework Extension (本 story dev-story Phase 2 实施)**: 新建 `Assets/GameScripts/HotFix/GameLogic/GameFlow/PuzzleCompleteToNarrativeBridge.cs` (清洁分离 — 推荐路径)，listen `IShadowPuzzleEvent.OnPuzzleComplete(puzzleId, completionType)` + emit `INarrativeEvent.OnRequestSequence(triggerSourceId=puzzleId, NarrativeSequenceType.MemoryReplay)`。备选路径: (B) ChapterStateManager.cs:276 OnPuzzleComplete 内 emit / (C) NarrativeSequencePlayer 直加 listener (violates SRP)。dev-story Phase 2 内 AskQuestion 决策 (A)/(B)/(C)
- `INarrativeEvent.OnRequestSequence` ↔ `NarrativeSequencePlayer.cs:127-132` listener wiring ✅ verified (per S5-05 ✅)
- `IAudioEvent.OnDuckingRequest` ↔ `AudioManager.HandleDuckingRequest` listener wiring（per S5-06 ✅ verified）
- UIModule `Button.onClick` → `GameEvent.Get<ISceneEvent>().OnRequestSceneChange(...)` dispatch — S5-08 ✅ DONE：vendor UIWindow.Button.onClick 路径 via `Button.onClick.AddListener(handler)` + handler 内 `GameEvent.Get<ISceneEvent>().OnRequestSceneChange(targetId)`；具体 hook 在 main menu UIWindow OnCreate 内挂
- **⚠️ R2.3 TBD (留 dev-story Phase 1.5 unity-mcp 实查)**: chapter 1 `Chapter_01_Approach.unity` 内 `Object_01_CoffeeMug` / `Object_02_Book` 是否含 `InteractableObject` MonoBehaviour + puzzle config injection — grep-based readiness gate 无法验 Unity scene 内 component；dev-story Phase 1.5 self-check 阶段用 `unity-mcp manage_gameobject get_components` 实查；如缺失则 dev-story Phase 2 内补 component + puzzle config 注入 (Sprint 5 [A] serial 序列内不产生 prerequisite 调整)

**Performance**: end-to-end success path total time < 10s（含 ~5s scene load + ~5s puzzle/narrative/audio inline）；R3 mandatory Integration type。

**Control Manifest Rules (this layer)**:

- **Required**: main menu Button onClick → `ISceneEvent.OnRequestSceneChange(1)` dispatch → SceneManager handler (S5-1c) → DriveTransitionAsync 11-step
- **Required**: chapter 1 内 InteractableObject (S2-08 ✅) drag/snap → `IShadowMatchEvent.OnMatchScoreUpdated(puzzleId=1, score)` 派发（MVP 阶段 spike fire mock；future ADR-012 ShadowMatchCalculator 取代）→ `PuzzleStateMachine.cs:119` ✅ listener 已挂
- **Required**: PuzzleStateMachine (S5-03 ✅) state transition Active → NearMatch (派发 `IShadowPuzzleEvent.OnNearMatchEnter(puzzleId=1)`) → PerfectMatch (派发 `IShadowPuzzleEvent.OnPerfectMatchEnter`) → Complete (派发 `IShadowPuzzleEvent.OnPuzzleComplete(puzzleId=1, completionType)`)
- **Required (本 story dev-story Phase 2 实施 — R2.2 Required Framework Extension)**: 新 wiring bridge `PuzzleCompleteToNarrativeBridge` (or alt path per dev-story 决策) listen `IShadowPuzzleEvent.OnPuzzleComplete` → 派发 `INarrativeEvent.OnRequestSequence(triggerSourceId=puzzleId=1, NarrativeSequenceType.MemoryReplay)` → NarrativeSequencePlayer.cs:127-132 listener ✅ 已挂 → SequencePlayer 串行播放 atomic effects
- **Required**: AudioManager (S5-06 ✅) listen `IAudioEvent.OnDuckingRequest` / `OnPlayMusic` → real `GameModule.Audio` framework dispatch
- **Required**: 'Next Chapter' Button onClick → `ISceneEvent.OnRequestSceneChange(0)` → SceneManager unload chapter 1 → state=Idle
- **Required**: 5 系统间 cross-system listener wiring **必须**走 production code（S5-08 UIModule + S5-02 内 wiring 路径），**不**在 spike 内手挂 listener handler 模拟 production
- **Forbidden**: 不实施 ui-system-006 完整 main menu UIWindow（留 Sprint 6 polish；本 story 仅 minimal Button × 2 inline impl per 决策 [A]）
- **Forbidden**: 不处理 error/restart path（留 S5-02b backlog Sprint 6 — `story-003-error-restart-path.md` placeholder）
- **Forbidden**: 不写新 5 系统内部 logic（S5-03/-05/-06 ✅ DONE — 本 story 仅 wiring + chapter 1 fixture 注入）
- **Forbidden**: 不实施 multi-chapter 切换（chapter 2-5 留 Sprint 6+）
- **Forbidden**: 不实施 save/load round-trip 与 pause/resume during transition（留 chapter-state epic Sprint 6+）

---

## Acceptance Criteria

*Integration type — cross 5 系统 wiring + R3 PlayMode probe MANDATORY (ADR-029 V2.0)*

- [ ] **AC-1 (UIModule + main menu Button minimal inline)**: S5-08 dev-story DONE 后，vs-chapter-1 chapter 1 入口 main menu base canvas 已挂载 + 2 个 placeholder Button：`Start Chapter 1` Button + `Next Chapter` Button（minimal inline impl per 决策 [A]）；按 ADR-011 UIModule UGUI 接入
- [ ] **AC-2 (main menu trigger → chapter 1 boot)**: `Start Chapter 1` Button onClick → `ISceneEvent.OnRequestSceneChange(1)` dispatch → SceneManager 内 `DriveTransitionAsync(1)` 11-step → spike listener 监听 `OnSceneTransitionEnd(1)` 完成 + `_sceneManager.CurrentLoadedChapterIdForTest == 1` + `CurrentState == Idle`
- [ ] **AC-3 (chapter 1 物件 drag/snap → IShadowMatchEvent.OnMatchScoreUpdated)**: chapter 1 内 InteractableObject prefab 实例（S2-08 + S4-06 ✅）spike 自动 drag → snap → `IShadowMatchEvent.OnMatchScoreUpdated(puzzleId=1, score)` 派发（MVP 阶段 spike fire mock score；future ADR-012 ShadowMatchCalculator production；PuzzleStateConfigFromLuban.InitWithDefaults() puzzle id=1 fixture per S5-03 ✅）→ `PuzzleStateMachine.cs:119` listener `_onMatchScoreUpdated` 接收
- [ ] **AC-4 (PuzzleStateMachine state transition)**: `IShadowMatchEvent.OnMatchScoreUpdated` → PuzzleStateMachine `Active` → `NearMatch` (派发 `IShadowPuzzleEvent.OnNearMatchEnter(puzzleId=1)`) → `PerfectMatch` (派发 `OnPerfectMatchEnter`) → `Complete` (派发 `OnPuzzleComplete(puzzleId=1, completionType)`)；spike snapshot 4 transition + 3 IShadowPuzzleEvent 派发各发生 1 次
- [ ] **AC-5 (PuzzleComplete → Narrative trigger via PuzzleCompleteToNarrativeBridge)**: `IShadowPuzzleEvent.OnPuzzleComplete(puzzleId=1, completionType)` (PuzzleStateMachine.cs:284 派发) → **本 story dev-story Phase 2 新建 wiring bridge `PuzzleCompleteToNarrativeBridge` (或 alt path per dev-story 决策) ✅ listen + emit** → `INarrativeEvent.OnRequestSequence(triggerSourceId=1, NarrativeSequenceType.MemoryReplay)` 派发 → `NarrativeSequencePlayer.cs:127-132` listener `_onRequestSequence` ✅ → `OnSequenceStart` (chapter 1 puzzle 1 sequence id；hardcoded fixture per S5-05 NarrativeSequenceConfigFromLuban ✅)
- [ ] **AC-6 (Narrative → Audio cue)**: chapter 1 sequence atomic effects 串行（per S5-05 ✅）至少含 1 `AudioDuckingEffect` → AudioManager (S5-06 ✅) listener 收 `IAudioEvent.OnDuckingRequest` → real `GameModule.Audio` Music layer ducking dispatch + spike 通过 reflection 验 `AudioManager._music.framework_sound_volume` 衰减比例 ≈ 0.3 (default duck level per S5-06 P4)
- [ ] **AC-7 (Sequence Complete + Next chapter Button → unload)**: `INarrativeEvent.OnSequenceComplete` fires → `Next Chapter` Button 可被 spike 模拟点击 → `ISceneEvent.OnRequestSceneChange(0)` → SceneManager unload chapter 1 → `OnSceneUnloadBegin` → `OnSceneTransitionEnd(0)` → state=Idle (S3-02 ✅)
- [ ] **AC-8 (end-to-end performance)**: success path total time（T0 → T13）< 10s 真实墙钟时间；spike Stopwatch metric collected
- [ ] **AC-9 (console clean)**: R3 PlayMode probe 全程 0 unexpected error / 0 unexpected warning（spike 用 `LogAssert.Expect` 主动标记 expected 项；如无 expected error/warning 则 0/0 实测）
- [ ] **AC-10 (R3 PlayMode probe ALL PASS)**: spike `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-02_EndToEndFlow.cs` 5-6 R3 case 全 PASS + JSON evidence `~/Library/Application Support/.../S5-02_Result.json` `all_passed=true` + `production/qa/playmode-end-to-end-flow-2026-05-XX.md` evidence doc 写完

---

## R3 Justification (Integration — MANDATORY)

按 ADR-029 V2.0 R3 mandatory criterion，本 story 是**典型的 cross-system Integration story** —— 跨 5 个 P1 系统（Scene Mgmt / Object Interaction / Shadow Puzzle / Narrative / Audio）listener wiring + UIModule + InteractableObject prefab + 5-系统单 success path 端到端验证。framework boundary probe 5+ 处，必须 PlayMode 实证（EditMode 不能验 listener-path 链路 + framework dispatch real audio behavior）。

### R3 PlayMode probe 5 cases（spike `S5-02_EndToEndFlow.cs` — **(M1) production reflection 全程**复用 S5-1c precedent）

> **Spike 模式**:
> - 全程**复用 production**：`GameApp._sceneManager` / `GameApp._audioManager` 等 reflection 拿 instance；listener subscribe production sender events；不构建 isolated SceneManager
> - **subscribe 必须在 `Awake()` 而非 `Start()`** per S5-1c lessons memo `problem_2026-05-09_spike-sync-subscribe-race.md`（listener-path 同步 fire event 路径 sync-subscribe race）
> - main menu Button 模拟点击：spike 通过 UIModule reflection 拿 Button 实例 + `Button.onClick.Invoke()` 模拟点击；**不**直接 dispatch `OnRequestSceneChange`（必须走 Button → onClick → dispatch 完整 production 路径）
> - chapter 1 InteractableObject 模拟操作：spike 通过 reflection / scene Find 拿物件实例 + 模拟 input event 触发 InteractableObject FSM Drag → Snap，**不**直接派 `IShadowMatchEvent.OnNearMatchEnter`（必须走 InteractableObject → puzzle config 注入 → near-match check → 派发完整 production 路径）

| # | Case | Setup | Action | Assert |
|---|---|---|---|---|
| **P1** | MainMenuButtonBootChapter1 | spike `Awake()` subscribe production scene events；main menu base canvas + Button 实例已挂载 (S5-08 done prerequisite) | spike `Button.onClick.Invoke()` 模拟点击 'Start Chapter 1' | listener 收到 8 lifecycle event 顺序 (`OnSceneTransitionBegin(-1, 1)` → `OnSceneLoadProgress` ≥1 次 → `OnSceneLoadComplete(1, "")` → `OnSceneReady(1)` → `OnSceneTransitionEnd(1)`) + `_sceneManager.CurrentLoadedChapterIdForTest == 1` + `CurrentState == Idle` |
| **P2** | ObjectInteractionToShadowMatch | post-P1（chapter 1 active）；spike subscribe `IShadowMatchEvent.OnMatchScoreUpdated` (sender path) + `IShadowPuzzleEvent.OnNearMatchEnter` (PuzzleStateMachine sender after state transit)；reflection 拿 chapter 1 内 `Object_01_CoffeeMug` InteractableObject 实例 + 验 puzzle config injected (puzzleId=1) | spike 模拟 InteractableObject Drag → Snap (per S2-08 InteractableObject FSM)；snap target = chapter 1 puzzle 1 expected position；spike 直接 fire mock `IShadowMatchEvent.OnMatchScoreUpdated(puzzleId=1, score=0.5)` (≥ nearMatchThreshold=0.40) | (a) `IShadowMatchEvent.OnMatchScoreUpdated(1, 0.5f)` 派发 ≥1 次 + puzzleId == 1 (int) + score ≥ 0.40f (per PuzzleStateConfigFromLuban id=1 nearMatchThreshold) + (b) PuzzleStateMachine listener `_onMatchScoreUpdated` 已接收 → 派发 `IShadowPuzzleEvent.OnNearMatchEnter(puzzleId=1)` 1 次 |
| **P3** | PuzzleStateTransitionToComplete | post-P2（OnNearMatchEnter fired by PuzzleStateMachine）；spike subscribe `IShadowPuzzleEvent.OnPerfectMatchEnter` + `IShadowPuzzleEvent.OnPuzzleComplete` + reflection 拿 PuzzleStateMachine instance；spike 持续 fire mock `OnMatchScoreUpdated(puzzleId=1, score=0.95)` (≥ perfectMatchThreshold=0.85) 维持时间 ≥ NearMatchHold + PerfectMatchHold timer (per PuzzleStateConfigFromLuban id=1 hardcoded duration) | spike Tick 等 PerfectMatchHold timer expire | PuzzleStateMachine 4 transition (`Active` → `NearMatch` → `PerfectMatch` → `Complete`) + 3 IShadowPuzzleEvent 派发 各 1 次 (`OnNearMatchEnter(1)` post-P2 + `OnPerfectMatchEnter(1)` + `OnPuzzleComplete(1, PerfectMatch)`) + final state == Complete |
| **P4** | NarrativeSequenceWithAudioDuck | post-P3（OnPuzzleComplete fired by PuzzleStateMachine）；spike subscribe `INarrativeEvent.OnRequestSequence` / `OnSequenceStart` / `OnSequenceComplete` / `IAudioEvent.OnDuckingRequest` + reflection 拿 AudioManager Music layer | spike 等 **PuzzleCompleteToNarrativeBridge** (本 story Phase 2 新建) listener 自动响应 `OnPuzzleComplete(puzzleId=1, ...)` → emit `OnRequestSequence(triggerSourceId=1, MemoryReplay)` → NarrativeSequencePlayer.cs:127 listener 接收 → start sequence | (a) `INarrativeEvent.OnRequestSequence(1, MemoryReplay)` 派发 ≥1 次 (bridge emit) + (b) `OnSequenceStart(1)` 派发 + (c) atomic effects 串行播放 (至少含 1 `AudioDuckingEffect` per S5-05 fixture) → `IAudioEvent.OnDuckingRequest(0.3f, fadeDuration)` 派发 ≥1 次 + (d) AudioManager Music layer `framework_sound_volume` 实测 ≈ 0.3 (reflection sample) + (e) `OnSequenceComplete(1)` 派发 |
| **P5** | NextChapterButtonUnloadToIdle | post-P4（OnSequenceComplete fired）；spike subscribe `OnSceneUnloadBegin` / `OnSceneTransitionEnd` | spike 'Next Chapter' `Button.onClick.Invoke()` 模拟点击 | `ISceneEvent.OnRequestSceneChange(0)` dispatch → `OnSceneUnloadBegin(1)` 派发 + `OnSceneTransitionEnd(0)` 派发 + `_sceneManager.CurrentState == Idle` + `CurrentLoadedChapterIdForTest == NoChapterId` (chapter 0 = main menu return; chapter 1 unloaded) |

**evidence JSON schema** (`Application.persistentDataPath/S5-02_Result.json`):

```json
{
  "story_id": "S5-02",
  "timestamp": "2026-05-XX HH:MM:SS",
  "all_passed": true,
  "overall_status": "All Passed",
  "total_time_ms": 8765,
  "cases": [
    {"id": "P1", "passed": true, "duration_ms": 5500, "events": ["OnSceneTransitionBegin(-1,1)", "OnSceneLoadProgress(...)", "OnSceneLoadComplete(1,'')", "OnSceneReady(1)", "OnSceneTransitionEnd(1)"]},
    {"id": "P2", "passed": true, "duration_ms": 800, "events": ["OnMatchScoreUpdated(1, 0.5f)", "OnNearMatchEnter(1)"]},
    {"id": "P3", "passed": true, "duration_ms": 1200, "events": ["state=NearMatch", "state=PerfectMatch", "state=Complete", "OnPerfectMatchEnter(1)", "OnPuzzleComplete(1, PerfectMatch)"]},
    {"id": "P4", "passed": true, "duration_ms": 1500, "events": ["OnRequestSequence(1, MemoryReplay)", "OnSequenceStart(1)", "OnDuckingRequest(0.3f, ...)", "OnSequenceComplete(1)"]},
    {"id": "P5", "passed": true, "duration_ms": 800, "events": ["OnSceneUnloadBegin(1)", "OnSceneTransitionEnd(0)"]}
  ]
}
```

**ADR-029 V2-5 listener self-removal pattern**: 本 story spike Init/Dispose × 1 个 success path（不做 5 cycle 复用 — V2-5 第 4 次实战累计已由 S5-1c 完成）；本 story 重点是 5 系统 wiring 而非 listener self-removal probe。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Error / Restart path** → **S5-02b backlog Sprint 6**（`production/epics/vs-chapter-1/story-003-error-restart-path.md` placeholder；含 unknown-chapter Button click / mid-transition cancel / asset load fail / restart-from-Error 等 4-5 case；1 SP）
- **ui-system-006 完整 main menu UIWindow** — 本 story 仅 minimal Button × 2 inline impl（per 决策 [A]）；完整 main menu UIWindow + LayerStrategy + UIRoot 集成留 Sprint 6 polish
- **ui-system-002 game-hud / -003 pause-menu / -004 puzzle-complete / -005 chapter-select / -007 settings-panel** — 全部留 Sprint 6+
- **Multi-chapter transition (chapter 2-5)** — 留 Sprint 6+
- **Save / Load round-trip** during chapter transition — 留 chapter-state epic Sprint 6+
- **Pause / Resume during scene transition** — 留 polish Sprint 6+
- **Performance multi-frame profiling (FPS / GC / memory delta)** — 留 polish Sprint 6+
- **TextureVideoEffect** (S5-05 partial 留 narrative-event polish Sprint 6+)
- **Localization** (chapter 1 文本写在 fixture 内 hardcoded zh-CN；i18n 留 Sprint 6+)
- **Tutorial onboarding** (chapter 1 tutorial-onboarding flow 不在本 story；留 Sprint 6+)

---

## QA Test Cases

*Integration type — automated PlayMode (R3) + grep-based static evidence + screenshot evidence*

- **AC-1, AC-7 (UIModule + Button) grep evidence**: 自动可验
  - `grep -c "GameModule.UI" Assets/GameScripts/HotFix/GameLogic/.../*.cs` ≥ 1（本 story 内 main menu Button 挂载点）
  - `grep -E "ISceneEvent.*OnRequestSceneChange\((0|1)\)" Assets/GameScripts/HotFix/GameLogic/` ≥ 2（main menu Button + next chapter Button onClick）
  - `glob production/epics/ui-system/story-001-uimodule-setup.md` Status: Done verified
- **AC-2..AC-7 (R3 PlayMode probe)**: spike 内 assert + JSON evidence
- **AC-8 (performance)**: spike Stopwatch metric in JSON evidence
- **AC-9 (console clean)**: `read_console` filter level=Error / level=Warning；R3 path 0 unexpected (LogAssert.Expect 标记除外)
- **AC-10 (R3 ALL PASS)**: `cat ~/Library/Application\ Support/.../S5-02_Result.json | jq .all_passed == true`

**screenshot evidence** (Visual checkpoint): 1+ screenshot of full success path checkpoint，建议 capture：
- (a) main menu state with 'Start Chapter 1' Button visible
- (b) chapter 1 in-progress (puzzle near-match state)
- (c) narrative sequence playing (audio duck active)
- (d) next chapter button visible after sequence complete

---

## Test Evidence

**Story Type**: Integration

**Required evidence**:

- **Spike**: `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-02_EndToEndFlow.cs` — **1 文件 + 3 内类**（`S502Spike : IDevSpike` + `S502Runtime : MonoBehaviour` + `S502Tester` 纯逻辑）；与 S5-1b/1c precedent 一致；M1 全 production reflection 模式
- **JSON evidence**: `~/Library/Application Support/<company>/<product>/S5-02_Result.json`（5 case 全 PASS schema 见上）
- **QA evidence doc**: `production/qa/playmode-end-to-end-flow-2026-05-XX.md` — 含
  - JSON evidence summary table（5 case 全 PASS）
  - Console snapshot (R3 path 0 unexpected error/warning；expected 项 LogAssert.Expect 标记)
  - `git diff --stat HEAD` 改动 evidence（预期：0 production C# diff，除非 R2 readiness gate 暴露 wiring 缺失需补 listener handler / fixture provider 注入；那时是补 wiring 进 production 而非新逻辑）
  - 5 系统 cross-system event 顺序 dump（spike Tester listener 收到的 event log，按时间序）
  - 4 张 screenshot evidence checkpoint
  - AC matrix 10/10
  - Watch List Hooks（如出现 framework boundary drift / wiring deficiency / V3 candidate）
- **Production code review**: lean mode skip LP-CODE-REVIEW gate；本 story 0 production C# diff 预期；如 wiring 缺失需补则走 code-review skill 一次

**Status**: Pending — 待 dev-story 实施。

---

## Dependencies

- **Depends on**:
  - **S5-01 ✅ DONE 2026-05-09** — chapter scene asset
  - **S5-1b ✅ DONE 2026-05-09** — boot pipeline 接入
  - **S5-1c ✅ DONE 2026-05-09** — ADR-009 production listener-path driver
  - **S5-03 ✅ DONE 2026-05-06** — PuzzleStateMachine production code
  - **S5-05 ✅ DONE 2026-05-08** — NarrativeSequencePlayer production code
  - **S5-06 ✅ DONE 2026-05-08** — AudioManager production code
  - **S5-04 (ready-for-dev → Done)** — art-bible sign-off (VS art readiness gate)；串行 prerequisite per Sprint 5 plan [A] serial 决策
  - **S5-08 ⚠️ NOT YET STARTED (hard prerequisite)** — UIModule Setup must promote Must Have AND dev-story DONE 才能 unblock 本 story；per main menu_scope decision [A] minimal inline + Sprint 5 序列 [A] serial（即 Sprint 5 顺序：S5-04 → S5-08 → S5-02 → S5-07）
  - **ADR-029 V2.0 (Accepted) R3 ready** + **ADR-030 (Accepted) §VS Build commitment 第 1 项**
- **Unlocks**:
  - **S5-07** Chapter 1 第 1 次 internal playtest session（Sprint 5 Should Have）
  - **ADR-030 §VS Build commitment 第 1 项 100% 完成** — Sprint 5 Track A 收官
  - **Sprint 5 Must Have 收官** — Track A + Track B + Track C art-bible 全 ✅
  - **Sprint 6 启动** — playtest cycle ≥2 次 + report + gate-check rerun + stage.txt: Pre-Production → Production

---

## Assumptions Validated (R2 — Session 27 #1 /story-readiness gate 实测完成 2026-05-12)

**R2 grep + 静态读 + S5-08 ✅ DONE 后实测**。5 大块 wiring uncertainty 实测结果：

| # | 假设 | 实测 grep evidence | 结果 |
|---|------|------------------|------|
| **R2.1** | `IShadowMatchEvent.OnNearMatchEnter` ↔ `PuzzleStateMachine` listener wiring 已挂 | `rg "OnNearMatchEnter" Assets/.../HotFix` → 接口归属错位：`OnNearMatchEnter` 不在 `IShadowMatchEvent` (该接口仅 `OnMatchScoreUpdated`) 而在 `IShadowPuzzleEvent.cs:23`；`PuzzleStateMachine.cs:228` 是 sender 不是 listener。正确链路: `IShadowMatchEvent.OnMatchScoreUpdated` ↔ `PuzzleStateMachine.cs:119` `AddEventListener<int, float>(IShadowMatchEvent_Event.OnMatchScoreUpdated, _onMatchScoreUpdated)` ✅ listener 已挂 | ⚠️ **WORDING DRIFT 修复** (story 全文 5 处 propagate fix Session 27 #1) |
| **R2.2** | `IShadowPuzzleEvent.OnPuzzleComplete` ↔ `NarrativeSequencePlayer` listener wiring 已挂 | `rg "AddEventListener.*OnPuzzleComplete" Assets/.../HotFix` → **0 hit**；`NarrativeSequencePlayer.cs:127-132` 仅 listen `INarrativeEvent.OnRequestSequence` | ❌ **WIRING GAP** → §Engine Notes `**Required Framework Extension**` flag (本 story dev-story Phase 2 新建 `PuzzleCompleteToNarrativeBridge.cs` 或 alt path) |
| **R2.3** | chapter 1 `Chapter_01_Approach.unity` 内 `Object_01_CoffeeMug` / `Object_02_Book` 已含 `InteractableObject` MonoBehaviour 与 puzzle config injection | grep-based readiness gate 无法验 Unity scene component — 留 dev-story Phase 1.5 self-check `unity-mcp manage_gameobject get_components` 实查 | ⚠️ **TBD** (留 dev-story Phase 1.5) |
| **R2.4** | chapter 1 PuzzleStateConfig 通过 fixture 注入 production code | `PuzzleConfigFromLuban.cs:34` `_configs[1] = new PuzzleStateConfig(id: 1, isAbsencePuzzle: false, nearMatchThreshold: 0.40f, perfectMatchThreshold: 0.85f, ..., tutorialGracePeriod: 3.0f)` ✅；`InitWithDefaults()` chapter 1 puzzle id=1 hardcoded | ✅ PASS |
| **R2.5** | UIModule base canvas + main menu Button 路径 production API | S5-08 ✅ DONE 2026-05-11 verify: `GameModule.UI` Singleton + `ShowUIAsync<T>()` / `CloseUI<T>()` / `HideUI<T>()` API + UIRoot 自动 init + `[Window(UILayer.X, fromResources: true, location: "UI/...")]` attribute pattern (per LogUI.cs:8 precedent) + Button.onClick path via `UIWindow.OnCreate` override 内 `_panel.GetComponentInChildren<Button>()` + `Button.onClick.AddListener(handler)` | ✅ PASS (per S5-08 4/4 R3 PlayMode + 29/29 asserts) |
| **R2.6** | M1 dual-layer pattern 复用 (production reflection 全程) per S5-1c precedent | S5-1b / S5-1c / S5-08 spike PlayMode 5/5 / 5/5 / 4/4 PASS first-run verified | ✅ S5-1b/1c/-08 precedent |
| **R2.7** | spike `Awake()` subscribe 协议 (per S5-1c lessons memo `problem_2026-05-09_spike-sync-subscribe-race.md`) | S5-1c spike Awake() subscribe verified | ✅ S5-1c precedent |
| **R2.8** | Spike "1 文件 + 3 内类" 惯例 | Glob `Spikes/S*.cs` 全部命中（S301..S5-08 9 个 spike 全部一致）| ✅ |

**R2 综合 verdict**: **DEFICIENCY-FLAGGED PASS** (per ADR-029 V2.0 §V2-2 R2 deficiency-flagged path)
- 6/8 ✅ PASS (R2.1 wording drift 在 story 内修复后 wiring 本身 ✅；R2.4 / R2.5 / R2.6 / R2.7 / R2.8 all ✅)
- 1/8 ❌ R2.2 wiring gap → §Engine Notes explicit `**Required Framework Extension**` flag (dev-story Phase 2 实施)
- 1/8 ⚠️ R2.3 TBD → dev-story Phase 1.5 unity-mcp 实查 (可补可不补)

**Reactive cost**: R2 grep + 静态读 ~15 min + story-002 12 sections amend ~30 min ≈ 45 min total。Proactive 价值: avoid dev-story Phase 3 PlayMode 实跑时 surprise wiring gap + interface 归属错位导致 P2/P3 case 直接 fail。

---

## ADR-029 V3 Watch List Hooks

### ⭐ Session 27 #1 实战触发 NEW (2026-05-12 /story-readiness gate R2 实证)

**Type-5 dp4 实战 NEW TRIGGER (spec/tooling ↔ reality drift)** — `IShadowMatchEvent` vs `IShadowPuzzleEvent` 接口归属错位：

- ADR/spec 文档 / story-002 原 wording 假设 `OnNearMatchEnter(string token)` 位于 `IShadowMatchEvent`
- vendor production code 实测: `IShadowMatchEvent` (IShadowMatchEvent.cs:21) **仅含** `OnMatchScoreUpdated(int puzzleId, float newScore)` 1 method；`OnNearMatchEnter(int puzzleId)` 位于 **`IShadowPuzzleEvent`** (IShadowPuzzleEvent.cs:23)
- payload 类型 drift: spec `string token` (e.g. "chapter_1_puzzle_1") ↔ vendor `int puzzleId` (e.g. 1)
- 累计 V3 Type-5 dp 现状: **dp1 S5-01 toolchain silent failure + dp2 S5-08 #4 ShowUI wording + dp3 S5-08 #5 UILayer wording + dp4 S5-02 #1 IShadowMatch/Puzzle interface 归属 + payload 类型 = 4 unique dp**
- **远超 V3 promote ROI 阈值 (≥3 unique dp)** → Sprint 5 retro **必须 promote** 为 ADR-029 V3 正式 candidate Type-5 'spec ↔ reality drift'

**Type-2(c) dp NEW TRIGGER (framework boundary 'OnPuzzleComplete → OnRequestSequence' wiring gap)**:

- ADR-014 / ADR-016 / GDD shadow-puzzle-system / GDD narrative-event-system 隐含假设 OnPuzzleComplete → 触发 narrative sequence 是"自动 chain"
- 实测: PuzzleStateMachine.cs:284 派发 `OnPuzzleComplete` 后无 listener (production code 0-hit `AddEventListener.*OnPuzzleComplete`)；NarrativeSequencePlayer 仅 listen `OnRequestSequence`，**chain 断裂**
- 实质是 ADR 间 contract gap (ADR-014 ↔ ADR-016 双方都不负责 bridge wiring)；本 story dev-story Phase 2 补 wiring bridge
- Sprint 5 retro action item 候选: ADR-014 §G + ADR-016 §G 明示 bridge wiring 责任归属

### 持续监控候选 (本 story R3 PlayMode 实施过程中如出现)

1. **Type-2(c) candidate**: 5 系统 cross-system listener wiring 任何其他 event 顺序 drift（e.g. `OnDuckingRequest` → AudioManager Music ducking real dispatch 不生效；UIModule `Button.onClick` → `OnRequestSceneChange` dispatch 不通）—— framework boundary behavior assumption drift；累计 dp
2. **Type-2(c) candidate**: R2.3 chapter 1 `Object_01_CoffeeMug` / `Object_02_Book` prefab 内不含 `InteractableObject` MonoBehaviour 或 puzzle config injection 缺失（dev-story Phase 1.5 unity-mcp verify 时如发现 → 累计 dp）
3. **Type-5 candidate (dp4 已实战触发)**: `unity-mcp` toolchain 在 dev-story Phase 1.5 verify chapter 1 prefab 状态时如再次撞 silent failure（D1~D4 类型）→ 累计 V3 dp5
4. **Type-6 candidate**: spike subscribe race 如再次出现（main menu Button onClick → OnRequestSceneChange 同步 fire 路径或类似）→ 累计 V3 Type-6 dp2；本 story spike Awake() subscribe + 调用顺序前置已采纳防御
5. **Type-8 candidate (S5-08 #6 dp1)**: UIWindow second show 行为 spec vs vendor drift 是否在本 story main menu UIWindow 'Next Chapter' Button 二次显示场景再次触发 → 累计 V3 Type-8 dp2

如出现以上任一，per ADR-029 V2.0 §V2-7：sprint-status.yaml `watch_list` triggers 内追加 drift type 描述 + 关联 story-002 R3 case 编号 + 沉淀 problem memo 到 `.claude/memory/`。

---

## Sprint 5 Closure Note

本 story DONE 后 Sprint 5 Must Have 全部 ✅ 收官（S5-01/-1b/-1c/-02/-03/-04/-05/-06/-08）；ADR-030 §VS Build commitment 第 1 项 100% 完成；Sprint 5 retro action items：

- ADR-029 V3 candidate 评估 promote（Type-5 'spec/tooling ↔ reality drift' 4 unique dp 远超阈值 → 强制 promote / Type-6 spike subscribe race / Type-8 UIWindow second show 行为 / V3 #8 dp 累计 close-out）
- ADR-014 §G + ADR-016 §G 明示 OnPuzzleComplete → OnRequestSequence bridge wiring 责任归属 (本 story R2.2 wiring gap 表征 contract gap)
- IPuzzleLockEvent 跨 ADR contract design alignment（ADR-014 vs ADR-016 token-based vs parameter-less；S5-05 dp5 surfaced）
- AudioModule activation gate 文档对齐（ADR-028 §1 + ADR-017 §B drift-v1/v2 cleanup）
- ADR-011 §G + SP-002 systematic amendment (UILayer enum 命名 / ShowUI API / 7+2 lifecycle / second show 行为；本 story 累积本 dp4 trigger)
- Sprint 6 启动：≥2 playtest sessions + report + gate-check rerun + stage.txt: Pre-Production → Production

---

## History

### 2026-05-12 Session 27 #1 — /story-readiness gate R1+R2+R3 amendment per 决策 [A] (Status: Draft → Ready)

**Trigger**: 2026-05-11 Session 26 #6 S5-08 ✅ DONE 解锁 S5-02；2026-05-12 Session 27 #1 启动 /story-readiness S5-02 gate。

**R1 ✅ PASS**: 0 hit `AddEventListener<I\w+Event>\(this\)` + 0 hit `class \w+\s*:\s*\w+,\s*I\w+Event` (forbidden per-event listener self-handler pattern)。

**R2 DEFICIENCY-FLAGGED PASS** (per ADR-029 V2.0 §V2-2 R2 deficiency-flagged path):

R2 grep + 静态读 5 大块 wiring uncertainty 实测：
1. **R2.1 ⚠️ WORDING DRIFT (5 处 fix)**: 接口归属错位 — `OnNearMatchEnter` 不在 `IShadowMatchEvent` 而在 `IShadowPuzzleEvent`；payload `string token` ↔ `int puzzleId` 类型 drift；正确链路 `IShadowMatchEvent.OnMatchScoreUpdated` ↔ `PuzzleStateMachine.cs:119` listener ✅。
2. **R2.2 ❌ WIRING GAP** → **Required Framework Extension** flag：`OnPuzzleComplete` listener 0-hit；NarrativeSequencePlayer 仅 listen `OnRequestSequence`；dev-story Phase 2 新建 `PuzzleCompleteToNarrativeBridge.cs` (推荐路径 [A]) 或 alt path。
3. **R2.3 ⚠️ TBD** → dev-story Phase 1.5 unity-mcp `manage_gameobject get_components` 实查 chapter 1 物件 component。
4. **R2.4 ✅ PASS**: `PuzzleConfigFromLuban.cs:34` puzzle id=1 chapter 1 hardcoded fixture verified。
5. **R2.5 ✅ PASS**: S5-08 ✅ DONE 后 UIModule API 通路 (GameModule.UI + ShowUIAsync API + UIRoot + Button.onClick) all verified。

**R3 ✅ propagate fix**: P2/P3/P4 case Assert wording 同步修复 (5 处 propagate from R2.1)；JSON evidence schema events 同步更新；R3 spike pattern (M1 + 1 file + 3 inner class) per S5-1b/1c/-08 precedent ✅。

**story-002 amend 12 sections (Session 27 #1)**:
1. **Status**: Draft → Ready
2. **Engine Notes** — R2.1 wording drift 修复块 + R2.2 Required Framework Extension flag (含 wiring bridge 3 候选路径) + R2.3 TBD note
3. **Control Manifest Rules** — Required 3 项 wording propagate fix (`OnMatchScoreUpdated`/PuzzleStateMachine sender/`PuzzleCompleteToNarrativeBridge` bridge)
4. **AC-3** 重写: `OnMatchScoreUpdated(puzzleId=1, score)` → PuzzleStateMachine listener `_onMatchScoreUpdated`
5. **AC-4** 重写: 4 transition + 3 IShadowPuzzleEvent 派发 (OnNearMatchEnter / OnPerfectMatchEnter / OnPuzzleComplete)
6. **AC-5** 重写: PuzzleCompleteToNarrativeBridge wiring + NarrativeSequencePlayer.cs:127-132 listener ✅
7. **R3 P2 case** Setup/Action/Assert 改 OnMatchScoreUpdated wording + spike fire mock score
8. **R3 P3 case** Setup/Action/Assert 改 OnMatchScoreUpdated 持续 fire + 3 IShadowPuzzleEvent 派发 verify
9. **R3 P4 case** Setup/Action/Assert 改 bridge listener 路径
10. **JSON evidence schema** P1-P5 events 同步 wording fix
11. **R2 Assumptions Validated 表** 8 row 实测结果填入 (R2.1 ⚠️ WORDING DRIFT / R2.2 ❌ WIRING GAP / R2.3 TBD / R2.4 ✅ / R2.5 ✅ / R2.6-8 ✅)
12. **V3 Watch List Hooks** dp4 NEW + Type-2(c) wiring gap dp + dp5 Type-8 monitor + History Session 27 #1 entry

**ADR-029 V3 dp 累计 (本 story 贡献)**:
- Type-5 'spec ↔ reality drift' **dp4 实战 NEW** = S5-01 dp1 + S5-08 dp2 + S5-08 dp3 + **S5-02 dp4** = 4 unique dp **远超 promote 阈值 (≥3)** → Sprint 5 retro **强制 promote**
- Type-2(c) wiring gap dp1 NEW (ADR-014 ↔ ADR-016 contract gap) — ADR-014/-016 §G amend 高优 retro action item

**Reactive cost**: R2 grep + 静态读 ~15 min + story-002 12 sections amend ~30 min ≈ 45 min total。

**Proactive 价值**: avoid dev-story Phase 3 PlayMode 实跑时 P2/P3/P4 case 直接 fail (interface 归属错位 + wiring gap 双叠加 fail) 预估 saved cost ~60 min。

**Sign-off**: R1 ✅ PASS + R2 DEFICIENCY-FLAGGED PASS + R3 ✅ propagate fix → ADR-029 verdict **DEFICIENCY-FLAGGED PASS** (acceptable per V2.0 §V2-2)；主 verdict **READY**。Status: Draft → Ready。

**Audit Trail Cross-references**:
- 上一 Session 26 #6 commit: 43b0880 (S5-08 done) + 9f7e928 (UIWindow Canvas lessons memo)
- 本 amend 之 commit: 待 Phase 5 commit
- 实测 grep evidence:
  - `IShadowPuzzleEvent.cs:23` OnNearMatchEnter(int) define
  - `IShadowMatchEvent.cs:21-33` OnMatchScoreUpdated(int, float) define
  - `PuzzleStateMachine.cs:119` AddEventListener<int, float>(IShadowMatchEvent_Event.OnMatchScoreUpdated, _onMatchScoreUpdated)
  - `PuzzleStateMachine.cs:228` GameEvent.Get<IShadowPuzzleEvent>().OnNearMatchEnter(_puzzleId) sender
  - `PuzzleStateMachine.cs:284` GameEvent.Get<IShadowPuzzleEvent>().OnPuzzleComplete sender
  - `NarrativeSequencePlayer.cs:127-132` AddEventListener for OnRequestSequence only
  - `PuzzleConfigFromLuban.cs:34` puzzle id=1 chapter 1 hardcoded
  - `S5-08 ✅ DONE 2026-05-11` UIModule API 通路 + 4/4 R3 PASS





