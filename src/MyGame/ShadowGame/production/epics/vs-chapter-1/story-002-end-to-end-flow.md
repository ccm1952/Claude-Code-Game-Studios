// 该文件由Cursor 自动生成

# Story 002: Chapter 1 end-to-end 5 系统串通可玩（happy path）

> **Epic**: VS Chapter 1
> **Status**: **Draft** *(待 /story-readiness gate 通过转 Ready)*
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

**Engine Notes** *(R2 待 /story-readiness gate 实测；本 story 5 大块 wiring uncertainty 见 §Assumptions)*:

- `ISceneEvent.OnRequestSceneChange(int)` listener handler 由 S5-1c verified（SceneManager 自闭环 DriveTransitionAsync）
- `IShadowMatchEvent.OnNearMatchEnter` ↔ `PuzzleStateMachine.HandleNearMatchEnter` listener wiring ⚠️ 需 R2 grep verify production listener handler 已挂
- `IShadowPuzzleEvent.OnPuzzleComplete` ↔ `NarrativeSequencePlayer` listener wiring ⚠️ 需 R2 grep verify
- `INarrativeEvent.OnRequestSequence` ↔ `NarrativeSequencePlayer.HandleRequestSequence` listener wiring（per S5-05 ✅ verified）
- `IAudioEvent.OnDuckingRequest` ↔ `AudioManager.HandleDuckingRequest` listener wiring（per S5-06 ✅ verified）
- UIModule `Button.onClick` → `GameEvent.Get<ISceneEvent>().OnRequestSceneChange(...)` dispatch — S5-08 dev-story 实施时确定具体 hook 路径
- chapter 1 `Chapter_01_Approach.unity` 内 `Object_01_CoffeeMug` / `Object_02_Book` 是否含 InteractableObject MonoBehaviour 与 puzzle config 注入 ⚠️ 需 R2 verify（S5-01 evidence doc §10b 待补：物件 prefab vs scene-instance 状态）

**Performance**: end-to-end success path total time < 10s（含 ~5s scene load + ~5s puzzle/narrative/audio inline）；R3 mandatory Integration type。

**Control Manifest Rules (this layer)**:

- **Required**: main menu Button onClick → `ISceneEvent.OnRequestSceneChange(1)` dispatch → SceneManager handler (S5-1c) → DriveTransitionAsync 11-step
- **Required**: chapter 1 内 InteractableObject (S2-08 ✅) drag/snap → `IShadowMatchEvent.OnNearMatchEnter`（chapter 1 puzzle 1 token）
- **Required**: PuzzleStateMachine (S5-03 ✅) state transition Active → NearMatch → PerfectMatch → Complete → 派发 `IShadowPuzzleEvent.OnPuzzleComplete`
- **Required**: NarrativeSequencePlayer (S5-05 ✅) listen `IShadowPuzzleEvent.OnPuzzleComplete` → 派发 `INarrativeEvent.OnRequestSequence(chapter 1 sequence id)` → SequencePlayer 串行播放 atomic effects
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
- [ ] **AC-3 (chapter 1 物件 drag/snap → IShadowMatchEvent)**: chapter 1 内 InteractableObject prefab 实例（S2-08 + S4-06 ✅）spike 自动 drag → snap → `IShadowMatchEvent.OnNearMatchEnter` 派发 (chapter 1 puzzle 1 token；PuzzleStateConfigFromLuban chapter 1 puzzle 1 fixture per S5-03 ✅)
- [ ] **AC-4 (PuzzleStateMachine state transition)**: `IShadowMatchEvent.OnNearMatchEnter` → PuzzleStateMachine `Active` → `NearMatch` → `PerfectMatch` → `Complete`（5 个 state transitions；per S5-03 ✅ ADR-014 DONE）；spike snapshot 5 transition 各发生 1 次
- [ ] **AC-5 (PuzzleComplete → Narrative trigger)**: `IShadowPuzzleEvent.OnPuzzleComplete` (S5-03 派发) → NarrativeSequencePlayer listener handler → `INarrativeEvent.OnRequestSequence` 派发 → `OnSequenceStart` (chapter 1 puzzle 1 sequence id；hardcoded fixture per S5-05 NarrativeSequenceConfigFromLuban ✅)
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
| **P2** | ObjectInteractionToShadowMatch | post-P1（chapter 1 active）；spike subscribe `IShadowMatchEvent.OnNearMatchEnter`；reflection 拿 chapter 1 内 `Object_01_CoffeeMug` InteractableObject 实例 + 验 puzzle config 已 inject | spike 模拟 InteractableObject Drag → Snap (per S2-08 InteractableObject FSM)；snap target = chapter 1 puzzle 1 expected position | `IShadowMatchEvent.OnNearMatchEnter(token)` 派发 ≥1 次 + token == "chapter_1_puzzle_1" (per S5-03 PuzzleStateConfigFromLuban hardcoded chapter 1) |
| **P3** | PuzzleStateTransitionToComplete | post-P2（NearMatchEnter fired）；spike subscribe `IShadowPuzzleEvent.OnPuzzleComplete` + reflection 拿 PuzzleStateMachine instance；spike 持续模拟 InteractableObject snap 维持时间 ≥ NearMatchHold + PerfectMatchHold timer (per S5-03 hardcoded duration) | spike Tick 等 PerfectMatchHold timer expire | PuzzleStateMachine 5 transition 各发生 1 次 (`Active` → `NearMatch` → `PerfectMatch` → `Complete`) + `IShadowPuzzleEvent.OnPuzzleComplete(chapter_1_puzzle_1)` 派发 ≥1 次 + final state == Complete |
| **P4** | NarrativeSequenceWithAudioDuck | post-P3（OnPuzzleComplete fired）；spike subscribe `INarrativeEvent.OnSequenceStart` / `OnSequenceComplete` / `IAudioEvent.OnDuckingRequest` + reflection 拿 AudioManager Music layer | spike 等 NarrativeSequencePlayer listener 自动响应 OnPuzzleComplete | `INarrativeEvent.OnRequestSequence(chapter_1_puzzle_1_sequence)` 派发 + `OnSequenceStart` 派发 + `IAudioEvent.OnDuckingRequest(0.3, ...)` 派发 ≥1 次 + AudioManager Music layer `framework_sound_volume` ≈ 0.3 (samples sample) + `OnSequenceComplete` 派发 |
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
    {"id": "P1", "passed": true, "duration_ms": 5500, "events": ["OnSceneTransitionBegin(-1,1)", ...]},
    {"id": "P2", "passed": true, "duration_ms": 800, "events": ["OnNearMatchEnter('chapter_1_puzzle_1')"]},
    {"id": "P3", "passed": true, "duration_ms": 1200, "events": ["state=NearMatch", "state=PerfectMatch", "state=Complete", "OnPuzzleComplete(...)"]},
    {"id": "P4", "passed": true, "duration_ms": 1500, "events": ["OnRequestSequence(...)", "OnSequenceStart(...)", "OnDuckingRequest(0.3,...)", "OnSequenceComplete(...)"]},
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

## Assumptions Validated (R2 — TBD readiness gate phase)

R2 grep verify 待 `/story-readiness` gate 时实证。**5 大块 wiring uncertainty 必须 R2 阶段 grep 实证**（不实证不写 dev-story）：

| # | 假设 | 实测 grep evidence (待) | 结果 |
|---|------|----------------------|------|
| **R2.1** | `IShadowMatchEvent.OnNearMatchEnter` ↔ `PuzzleStateMachine.HandleXxx` listener wiring 已挂在 production code | `rg "AddEventListener.*OnNearMatchEnter" Assets/GameScripts/HotFix/GameLogic/ShadowPuzzle/` ≥ 1 hit | TBD |
| **R2.2** | `IShadowPuzzleEvent.OnPuzzleComplete` ↔ `NarrativeSequencePlayer` listener wiring 已挂 | `rg "AddEventListener.*OnPuzzleComplete" Assets/GameScripts/HotFix/GameLogic/NarrativeEvent/` ≥ 1 hit | TBD |
| **R2.3** | chapter 1 `Chapter_01_Approach.unity` 内 `Object_01_CoffeeMug` / `Object_02_Book` 已含 `InteractableObject` MonoBehaviour 与 puzzle config injection | unity-mcp `manage_gameobject get_components` for Object_01/02 + check InteractableObject component | TBD |
| **R2.4** | chapter 1 PuzzleStateConfig 通过 ChapterDataProvider / Luban / fixture 注入 production code | `rg "chapter_1_puzzle_1\|chapter 1.*puzzle.*config" Assets/GameScripts/HotFix/GameLogic/` ≥ 1 hit + read S5-03 PuzzleStateConfigFromLuban hardcoded chapter 1 lookup | TBD |
| **R2.5** | UIModule base canvas 接入 main menu Button 路径 production API（`GameModule.UI.SetUIRoot` / `AddPanel` 等） | read `production/epics/ui-system/story-001-uimodule-setup.md` AC + `rg "GameModule.UI.*Button\|UIRoot\|SetCanvas" Assets/GameScripts/HotFix/GameLogic/` ≥ 1 hit | TBD（待 S5-08 done）|
| **R2.6** | M1 dual-layer pattern 复用 (production reflection 全程) per S5-1c precedent | S5-1c spike `S5-1c_ListenerPathDriver.cs` PlayMode 5/5 PASS first-run (after sync-subscribe race fix) verified | ✅ S5-1c precedent |
| **R2.7** | spike `Awake()` subscribe 协议 (per S5-1c lessons memo `problem_2026-05-09_spike-sync-subscribe-race.md`) | S5-1c spike Awake() subscribe verified | ✅ S5-1c precedent |
| **R2.8** | Spike "1 文件 + 3 内类" 惯例 | Glob `Spikes/S*.cs` 全部命中（S301..S5-1c 8 个 spike 全部一致）| ✅ |

**R2 PASS 路径**: 8/8 ✅ → 写 dev-story；任意 R2 0-hit → DEFICIENCY-FLAGGED PASS 路径（ADR-029 V2.0 deficiency-flag 协议）—— dev-story 阶段先补 wiring（production listener handler 挂 / chapter 1 prefab 含 InteractableObject + puzzle config / ...）然后跑 R3。

**5 大 wiring uncertainty 中 R2.5 必待 S5-08 done 才能 verify**（UIModule Setup dev-story 后才有 production API 路径可 grep）。Sprint 5 serial 序列保证此先后。

---

## ADR-029 V3 Watch List Hooks

本 story R3 实施过程中如出现以下情况应 capture 为新 drift type 候选并写入 sprint-status.yaml watch list：

1. **Type-2(c) candidate**: 5 系统 cross-system listener wiring 任何 event 顺序 drift（e.g. `OnPuzzleComplete` → `OnRequestSequence` 不 fires；`OnDuckingRequest` → AudioManager Music ducking real dispatch 不生效；UIModule `Button.onClick` → `OnRequestSceneChange` dispatch 不通）—— framework boundary behavior assumption drift；累计 dp 数据点
2. **Type-2(c) candidate**: chapter 1 `Object_01_CoffeeMug` / `Object_02_Book` prefab 内不含 `InteractableObject` MonoBehaviour 或 puzzle config injection 缺失（S5-01 closure_summary line 805 列出 GameObject 名但是否 component 完整 ⚠️ R2.3 待 verify）
3. **Type-5 candidate (S5-01 dp1 promote 候选累计)**: `unity-mcp` toolchain 在 R2 阶段 verify chapter 1 prefab 状态时如再次撞 silent failure（D1~D4 类型）→ 累计 V3 #2 dp1 数据点
4. **Type-6 candidate (S5-1c lessons memo promote 候选累计)**: spike subscribe race 如再次出现（main menu Button onClick → OnRequestSceneChange 同步 fire 路径或类似）→ 累计 V3 #6 dp1 数据点；本 story spike Awake() subscribe + 调用顺序前置已采纳防御

如出现以上任一，per ADR-029 V2.0 §V2-7：sprint-status.yaml `watch_list` triggers 内追加 drift type 描述 + 关联 story-002 R3 case 编号 + 沉淀 problem memo 到 `.claude/memory/`。

---

## Sprint 5 Closure Note

本 story DONE 后 Sprint 5 Must Have 全部 ✅ 收官（S5-01/-1b/-1c/-02/-03/-04/-05/-06/-08）；ADR-030 §VS Build commitment 第 1 项 100% 完成；Sprint 5 retro action items：

- ADR-029 V3 candidate 评估 promote（Type-5 tooling silent failure / Type-6 spike subscribe race / V3 #8 dp 累计 close-out）
- IPuzzleLockEvent 跨 ADR contract design alignment（ADR-014 vs ADR-016 token-based vs parameter-less；S5-05 dp5 surfaced）
- AudioModule activation gate 文档对齐（ADR-028 §1 + ADR-017 §B drift-v1/v2 cleanup）
- Sprint 6 启动：≥2 playtest sessions + report + gate-check rerun + stage.txt: Pre-Production → Production





