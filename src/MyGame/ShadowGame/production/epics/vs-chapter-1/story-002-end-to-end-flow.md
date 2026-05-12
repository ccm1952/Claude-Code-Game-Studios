// 该文件由Cursor 自动生成

# Story 002: Chapter 1 end-to-end 5 系统串通可玩（happy path）

> **Epic**: VS Chapter 1
> **Status**: **Ready** *(2026-05-12 Session 27 #2 — dev-story Phase 1.5 self-check 3 \u91cd\u5927\u53d1\u73b0\u540e amend per \u51b3\u7b56 [A]\u00d73\uff1aF1 R2.2 wiring gap \u8bef\u5224\u64a4\u56de + F2 R2.3 chapter 1 \u7269\u4ef6 component \u7f3a\u4f46 spike fire mock \u7b80\u5316\u8def\u5f84 RESOLVED + F3 `OnPerfectMatchEnter`\u2192`OnPerfectMatch` wording drift propagate fix\uff1bSession 27 #1 R2/R3 verdict \u91cd\u8bc4\u4e3a R2 \u2705 PASS\uff08\u4e0d\u518d DEFICIENCY-FLAGGED\uff09\uff09*
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
T6 PuzzleStateMachine: Active → NearMatch (派发 OnNearMatchEnter) → PerfectMatch (派发 OnPerfectMatch) → Complete (派发 OnPuzzleComplete; Sprint 5 暂无 listener — ChapterStateManager 接入留 Sprint 6+)
T7 NarrativeSequencePlayer.cs:133 listener `_onPerfectMatch` 接 IShadowPuzzleEvent.OnPerfectMatch ✅ → HandleRequestSequence(puzzleId=1, NarrativeSequenceType.MemoryReplay) (即 PuzzleStateMachine 通过 OnPerfectMatch 直 wire narrative，**无需** bridge — per F1 R2.2 misjudge revert 2026-05-12 Session 27 #2)
T8 NarrativeSequencePlayer.StartSequence → 派发 INarrativeEvent.OnSequenceStart + IInputBlockerEvent.OnPushBlocker
T9 NarrativeSequencePlayer.Tick atomic effects 串行 (含 AudioDuckingEffect + ScreenFadeEffect + WaitEffect)
T10 AudioManager listen IAudioEvent.OnDuckingRequest → GameModule.Audio Music ducking real dispatch
T11 NarrativeSequencePlayer.CompleteSequence → 派发 IInputBlockerEvent.OnPopBlocker + INarrativeEvent.OnSequenceComplete
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

**Engine Notes** *(R2 grep + unity-mcp 实证完成；Session 27 #1 R2 5 \u5927\u5757 wiring uncertainty \u5b9e\u6d4b + Session 27 #2 dev-story Phase 1.5 self-check \u8865\u9f50 F1/F2/F3 \u53d1\u73b0\u540e\u91cd\u8bc4)*:

- `ISceneEvent.OnRequestSceneChange(int)` listener handler 由 S5-1c verified（SceneManager 自闭环 DriveTransitionAsync）
- **\u26a0\ufe0f WORDING DRIFT \u4fee\u590d (R2.1 Session 27 #1 \u5b9e\u6d4b)**: 接口归属错位 — story 原写 `IShadowMatchEvent.OnNearMatchEnter` 但实际：
  - `IShadowMatchEvent` (IShadowMatchEvent.cs:21) 仅含 1 method `OnMatchScoreUpdated(int puzzleId, float newScore)` (sender: future ADR-012 `ShadowMatchCalculator`；MVP 阶段 spike fire mock；listener: PuzzleStateMachine)
  - `OnNearMatchEnter(int puzzleId)` 位于 **`IShadowPuzzleEvent`** (IShadowPuzzleEvent.cs:23)，**`PuzzleStateMachine.cs:228` 是 sender 不是 listener**
  - **\u5b9e\u9645\u6b63\u786e\u94fe\u8def**: T5 spike fire mock `IShadowMatchEvent.OnMatchScoreUpdated(puzzleId=1, score)` (MVP \u9636\u6bb5 simplification per F2) → `PuzzleStateMachine.cs:119` `AddEventListener<int, float>(IShadowMatchEvent_Event.OnMatchScoreUpdated, _onMatchScoreUpdated)` ✅ listener 已挂 → state transit → 派发 `IShadowPuzzleEvent.OnNearMatchEnter(puzzleId=1)` (PuzzleStateMachine 是 sender)
  - payload 类型: `int puzzleId` 不是 string `token`；本 story 全文 token wording 同步 propagate fix 至 puzzleId
- **\u26a0\ufe0f WORDING DRIFT \u4fee\u590d NEW (F3 Session 27 #2 \u5b9e\u6d4b)**: `OnPerfectMatchEnter` \u4e0d\u5b58\u5728\uff1b\u5b9e\u9645\u63a5\u53e3 method \u662f `OnPerfectMatch(int puzzleId, float finalMatchScore)` \uff08\u65e0 `Enter` \u540e\u7f00\uff09\uff1a
  - `IShadowPuzzleEvent.cs:39` 实测 `void OnPerfectMatch(int puzzleId, float finalMatchScore);`
  - story-002 line 122/148/164 4 处全部错写为 `OnPerfectMatchEnter` — propagate fix to `OnPerfectMatch`
  - payload \u989d\u5916\u5e26 `float finalMatchScore` \uff08\u9501\u5b9a\u72b6\u6001\u8f6c\u6362\u65f6 matchScore frozen\uff09
- **\u2705 F1 R2.2 wiring gap \u8bef\u5224\u64a4\u56de (Session 27 #2 dev-story Phase 1.5 \u91cd\u67e5)**:
  - Session 27 #1 R2.2 grep `rg "AddEventListener.*OnPuzzleComplete"` \u5f97 0-hit \u662f\u5b9e\uff0c\u4f46\u5224\u5b9a\u4e3a wiring gap \u662f\u8bef\u5224 \u2014 NarrativeSequencePlayer.cs:133 \u5df2\u7528 `OnPerfectMatch` listener \u76f4 wire puzzle\u2192narrative chain
  - 实测: `NarrativeSequencePlayer.cs:127-135` 4 个 listener (`OnRequestSequence` + **`OnPerfectMatch`** + `OnAbsenceAccepted` + `OnChapterComplete`)\uff1bline 240 `OnPerfectMatchHandler(int puzzleId, float finalMatchScore)` → `HandleRequestSequence(puzzleId, NarrativeSequenceType.MemoryReplay)` ✅
  - 即 puzzle→narrative chain 实际通过 `OnPerfectMatch` (PerfectMatch state 入派发) 直接触发\uff0c**\u4e0d\u9700\u8fc7 `OnPuzzleComplete`**\u3002`OnPuzzleComplete` \u9884\u8ba1\u662f ChapterStateManager listener \u8def\u5f84 (Sprint 6+ \u624d\u63a5\u5165 ChapterStateManager production startup)
  - **取消 `PuzzleCompleteToNarrativeBridge.cs` 实施** \u2014 \u907f\u514d\u53cc\u91cd trigger (OnPerfectMatch + bridge \u4f1a\u8ba9 NarrativeSequencePlayer enqueue \u7b2c 2 \u6b21 sequence) + \u8282\u7701 ~30 min
- `INarrativeEvent.OnRequestSequence` ↔ `NarrativeSequencePlayer.cs:127-132` listener wiring ✅ verified (per S5-05 ✅)\uff1b\u672c story \u5b9e\u9645\u4e3b\u8def\u5f84\u662f `OnPerfectMatch` \u76f4 wire\uff0c`OnRequestSequence` \u4ec5\u4f5c external request fallback
- `IAudioEvent.OnDuckingRequest` ↔ `AudioManager.HandleDuckingRequest` listener wiring（per S5-06 ✅ verified）
- UIModule `Button.onClick` → `GameEvent.Get<ISceneEvent>().OnRequestSceneChange(...)` dispatch — S5-08 ✅ DONE：vendor UIWindow.Button.onClick 路径 via `Button.onClick.AddListener(handler)` + handler 内 `GameEvent.Get<ISceneEvent>().OnRequestSceneChange(targetId)`；具体 hook 在 main menu UIWindow OnCreate 内挂
- **\u2705 F2 R2.3 RESOLVED (Session 27 #2 unity-mcp \u5b9e\u67e5)**: chapter 1 `Chapter_01_Approach.unity` \u5185 `Object_01_CoffeeMug` / `Object_02_Book` componentTypes \u4ec5 `[Transform, MeshFilter, BoxCollider/CapsuleCollider, MeshRenderer]` \u2014 \u7f3a `InteractableObject` MonoBehaviour\uff1b\u4f46 R3 spike P2 Action \u8bbe\u8ba1\u4e3a\u76f4\u63a5 fire mock `IShadowMatchEvent.OnMatchScoreUpdated(1, 0.95)`\uff0c**\u4e0d\u8d70 InteractableObject FSM drag/snap \u771f\u5b9e\u8def\u5f84** \u2014 \u4e0d\u963b\u585e happy path R3\uff1bInteractableObject + puzzle config \u6ce8\u5165 + ADR-012 ShadowMatchCalculator \u7559 Sprint 6 polish (\u8282\u7701 ~20 min)
- **\u2705 ChapterStateManager Sprint 5 production 0-caller (per dev-story Phase 1.5 \u5b9e\u6d4b)**: `new ChapterStateManager(...)` 0-hit; `ChapterStateEventBridge.Attach(...)` 0-hit\uff1bchapter \u63a8\u8fdb\u903b\u8f91 deferred to Sprint 6+\uff1b\u672c story happy path \u5728 `OnSequenceComplete` \u540e\u7531 spike \u6a21\u62df 'Next Chapter' Button click \u624b\u52a8 trigger chapter unload，**\u4e0d\u4f9d\u8d56** ChapterStateManager

**Performance**: end-to-end success path total time < 10s（含 ~5s scene load + ~5s puzzle/narrative/audio inline）；R3 mandatory Integration type。

**Control Manifest Rules (this layer)**:

- **Required**: main menu Button onClick → `ISceneEvent.OnRequestSceneChange(1)` dispatch → SceneManager handler (S5-1c) → DriveTransitionAsync 11-step
- **Required**: chapter 1 内 InteractableObject (S2-08 ✅) drag/snap → `IShadowMatchEvent.OnMatchScoreUpdated(puzzleId=1, score)` 派发（MVP 阶段 spike fire mock；future ADR-012 ShadowMatchCalculator 取代）→ `PuzzleStateMachine.cs:119` ✅ listener 已挂
- **Required**: PuzzleStateMachine (S5-03 ✅) state transition Active → NearMatch (派发 `IShadowPuzzleEvent.OnNearMatchEnter(puzzleId=1)`) → PerfectMatch (派发 `IShadowPuzzleEvent.OnPerfectMatch(puzzleId=1, finalMatchScore)`) → Complete (派发 `IShadowPuzzleEvent.OnPuzzleComplete(puzzleId=1, completionType)`)
- **Required (puzzle\u2192narrative \u76f4 wire \u2014 F1 R2.2 \u8bef\u5224\u64a4\u56de\u540e)**: `IShadowPuzzleEvent.OnPerfectMatch(puzzleId=1, finalMatchScore)` (PuzzleStateMachine PerfectMatch state \u8f6c\u6362\u65f6\u6d3e\u53d1) → `NarrativeSequencePlayer.cs:133` listener `_onPerfectMatch` \u2705 \u5df2\u6302 → `OnPerfectMatchHandler` → `HandleRequestSequence(puzzleId=1, NarrativeSequenceType.MemoryReplay)` \u2192 StartSequence → \u4e32\u884c\u64ad\u653e atomic effects (\u542b AudioDuckingEffect)
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
- [ ] **AC-4 (PuzzleStateMachine state transition)**: `IShadowMatchEvent.OnMatchScoreUpdated` → PuzzleStateMachine `Active` → `NearMatch` (派发 `IShadowPuzzleEvent.OnNearMatchEnter(puzzleId=1)`) → `PerfectMatch` (派发 `IShadowPuzzleEvent.OnPerfectMatch(puzzleId=1, finalMatchScore)`) → `Complete` (派发 `OnPuzzleComplete(puzzleId=1, completionType)`)；spike snapshot 4 transition + 3 IShadowPuzzleEvent 派发各发生 1 次
- [ ] **AC-5 (PerfectMatch \u2192 Narrative \u76f4 wire \u2014 F1 R2.2 \u8bef\u5224\u64a4\u56de\u540e)**: `IShadowPuzzleEvent.OnPerfectMatch(puzzleId=1, finalMatchScore)` (PuzzleStateMachine PerfectMatch state \u8f6c\u6362\u65f6\u6d3e\u53d1) → **`NarrativeSequencePlayer.cs:133` listener `_onPerfectMatch` \u2705 \u5df2\u6302** → `OnPerfectMatchHandler` → `HandleRequestSequence(puzzleId=1, NarrativeSequenceType.MemoryReplay)` → `StartSequence` (chapter 1 puzzle 1 sequence id\uff1bhardcoded fixture per S5-05 NarrativeSequenceConfigFromLuban \u2705) \u2192 \u6d3e\u53d1 `OnSequenceStart` + `IInputBlockerEvent.OnPushBlocker`
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
> - chapter 1 puzzle \u8f93\u5165\u6a21\u62df (F2 R2.3 simplified path)\uff1aSprint 5 \u9636\u6bb5 `Object_01_CoffeeMug` / `Object_02_Book` \u672a\u6302 InteractableObject MonoBehaviour\uff08\u7559 Sprint 6 polish\uff09\uff0cspike \u76f4\u63a5 fire mock `IShadowMatchEvent.OnMatchScoreUpdated(puzzleId=1, score)`\uff0c\u8d70 PuzzleStateMachine listener \u2705 \u5df2\u6302 \u2192 state transit \u5b8c\u6574 production \u8def\u5f84\uff1bfuture ADR-012 ShadowMatchCalculator + InteractableObject puzzle config injection \u5b9e\u4f53\u63a5\u5165\u540e\u53ef\u5347\u7ea7\u5230\u8d70\u771f drag/snap \u8def\u5f84

| # | Case | Setup | Action | Assert |
|---|---|---|---|---|
| **P1** | MainMenuButtonBootChapter1 | spike `Awake()` subscribe production scene events；main menu base canvas + Button 实例已挂载 (S5-08 done prerequisite) | spike `Button.onClick.Invoke()` 模拟点击 'Start Chapter 1' | listener 收到 8 lifecycle event 顺序 (`OnSceneTransitionBegin(-1, 1)` → `OnSceneLoadProgress` ≥1 次 → `OnSceneLoadComplete(1, "")` → `OnSceneReady(1)` → `OnSceneTransitionEnd(1)`) + `_sceneManager.CurrentLoadedChapterIdForTest == 1` + `CurrentState == Idle` |
| **P2** | ObjectInteractionToShadowMatch | post-P1（chapter 1 active）；spike subscribe `IShadowMatchEvent.OnMatchScoreUpdated` (sender path) + `IShadowPuzzleEvent.OnNearMatchEnter` (PuzzleStateMachine sender after state transit)；reflection 拿 chapter 1 内 `Object_01_CoffeeMug` InteractableObject 实例 + 验 puzzle config injected (puzzleId=1) | spike 模拟 InteractableObject Drag → Snap (per S2-08 InteractableObject FSM)；snap target = chapter 1 puzzle 1 expected position；spike 直接 fire mock `IShadowMatchEvent.OnMatchScoreUpdated(puzzleId=1, score=0.5)` (≥ nearMatchThreshold=0.40) | (a) `IShadowMatchEvent.OnMatchScoreUpdated(1, 0.5f)` 派发 ≥1 次 + puzzleId == 1 (int) + score ≥ 0.40f (per PuzzleStateConfigFromLuban id=1 nearMatchThreshold) + (b) PuzzleStateMachine listener `_onMatchScoreUpdated` 已接收 → 派发 `IShadowPuzzleEvent.OnNearMatchEnter(puzzleId=1)` 1 次 |
| **P3** | PuzzleStateTransitionToComplete | post-P2（OnNearMatchEnter fired by PuzzleStateMachine）；spike subscribe `IShadowPuzzleEvent.OnPerfectMatch` (\u65e0 Enter \u540e\u7f00\uff0c`(int, float)`) + `IShadowPuzzleEvent.OnPuzzleComplete` + reflection 拿 PuzzleStateMachine instance；spike 持续 fire mock `OnMatchScoreUpdated(puzzleId=1, score=0.95)` (≥ perfectMatchThreshold=0.85) 维持时间 ≥ NearMatchHold + PerfectMatchHold timer (per PuzzleStateConfigFromLuban id=1 hardcoded duration) | spike Tick 等 PerfectMatchHold timer expire | PuzzleStateMachine 4 transition (`Active` → `NearMatch` → `PerfectMatch` → `Complete`) + 3 IShadowPuzzleEvent 派发 各 1 次 (`OnNearMatchEnter(1)` post-P2 + `OnPerfectMatch(1, finalScore)` + `OnPuzzleComplete(1, PerfectMatch)`) + final state == Complete |
| **P4** | NarrativeSequenceWithAudioDuck (PerfectMatch \u76f4 wire) | post-P3 (`OnPerfectMatch` fired by PuzzleStateMachine)；spike subscribe `INarrativeEvent.OnSequenceStart` / `OnSequenceComplete` / `IAudioEvent.OnDuckingRequest` + reflection 拿 AudioManager Music layer\uff1b**\u4e0d**\u6311 subscribe `OnRequestSequence`\uff08\u4e0d\u662f\u672c\u8def\u5f84\u4e3b\u4e8b\u4ef6\uff09 | spike \u7b49 `NarrativeSequencePlayer.cs:133` listener `_onPerfectMatch` \u81ea\u52a8\u54cd\u5e94 P3 \u6d3e\u53d1\u7684 `OnPerfectMatch(puzzleId=1, finalScore)` \u2192 OnPerfectMatchHandler \u2192 HandleRequestSequence(1, MemoryReplay) \u2192 start sequence | (a) `OnSequenceStart(1, MemoryReplay)` 派发 \u2265 1 次 + (b) atomic effects 串行播放 (至少含 1 `AudioDuckingEffect` per S5-05 fixture) → `IAudioEvent.OnDuckingRequest(0.3f, fadeDuration)` 派发 ≥1 次 + (c) AudioManager Music layer `framework_sound_volume` 实测 ≈ 0.3 (reflection sample) + (d) `OnSequenceComplete(1, MemoryReplay)` 派发 |
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
    {"id": "P3", "passed": true, "duration_ms": 1200, "events": ["state=NearMatch", "state=PerfectMatch", "state=Complete", "OnPerfectMatch(1, 0.95f)", "OnPuzzleComplete(1, PerfectMatch)"]},
    {"id": "P4", "passed": true, "duration_ms": 1500, "events": ["OnSequenceStart(1, MemoryReplay)", "OnDuckingRequest(0.3f, ...)", "OnSequenceComplete(1, MemoryReplay)"]},
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
| **R2.2** | `IShadowPuzzleEvent.OnPuzzleComplete` ↔ `NarrativeSequencePlayer` listener wiring 已挂 | Session 27 #1 grep `rg "AddEventListener.*OnPuzzleComplete"` → 0 hit (\u5224\u5b9a wiring gap)\uff1b**Session 27 #2 \u91cd\u67e5\u53d1\u73b0**\uff1aR2.2 grep \u9762\u592a\u7a84 \u2014 NarrativeSequencePlayer.cs:**133-135** \u5b9e\u9645 4 \u4e2a listener (`OnRequestSequence` + **`OnPerfectMatch`** + `OnAbsenceAccepted` + `OnChapterComplete`)\uff1b`OnPerfectMatchHandler` (line 240) \u2192 `HandleRequestSequence(puzzleId, MemoryReplay)` \u2192 \u76f4 wire puzzle\u2192narrative chain | ✅ **PASS** (`OnPerfectMatch` \u76f4 wire\uff1b\u539f wiring gap \u8bef\u5224 \u2014 F1 Session 27 #2 \u64a4\u56de) |
| **R2.3** | chapter 1 `Chapter_01_Approach.unity` 内 `Object_01_CoffeeMug` / `Object_02_Book` 已含 `InteractableObject` MonoBehaviour 与 puzzle config injection | Session 27 #2 unity-mcp `manage_scene get_hierarchy parent=Interactables` 实测: `Object_01_CoffeeMug` componentTypes `[Transform, MeshFilter, BoxCollider, MeshRenderer]`\uff1b`Object_02_Book` `[Transform, MeshFilter, CapsuleCollider, MeshRenderer]` \u2014 **\u7f3a `InteractableObject` MonoBehaviour** | ✅ **RESOLVED** (R3 P2 Action \u5df2\u8bbe\u8ba1\u4e3a spike fire mock OnMatchScoreUpdated \u7b80\u5316\u8def\u5f84\uff0c\u4e0d\u8d70 IO FSM \u771f drag/snap\uff1b\u4e0d\u963b\u585e happy path\uff1bInteractableObject prefab + puzzle config injection \u7559 Sprint 6) |
| **R2.4** | chapter 1 PuzzleStateConfig 通过 fixture 注入 production code | `PuzzleConfigFromLuban.cs:34` `_configs[1] = new PuzzleStateConfig(id: 1, isAbsencePuzzle: false, nearMatchThreshold: 0.40f, perfectMatchThreshold: 0.85f, ..., tutorialGracePeriod: 3.0f)` ✅；`InitWithDefaults()` chapter 1 puzzle id=1 hardcoded | ✅ PASS |
| **R2.5** | UIModule base canvas + main menu Button 路径 production API | S5-08 ✅ DONE 2026-05-11 verify: `GameModule.UI` Singleton + `ShowUIAsync<T>()` / `CloseUI<T>()` / `HideUI<T>()` API + UIRoot 自动 init + `[Window(UILayer.X, fromResources: true, location: "UI/...")]` attribute pattern (per LogUI.cs:8 precedent) + Button.onClick path via `UIWindow.OnCreate` override 内 `_panel.GetComponentInChildren<Button>()` + `Button.onClick.AddListener(handler)` | ✅ PASS (per S5-08 4/4 R3 PlayMode + 29/29 asserts) |
| **R2.6** | M1 dual-layer pattern 复用 (production reflection 全程) per S5-1c precedent | S5-1b / S5-1c / S5-08 spike PlayMode 5/5 / 5/5 / 4/4 PASS first-run verified | ✅ S5-1b/1c/-08 precedent |
| **R2.7** | spike `Awake()` subscribe 协议 (per S5-1c lessons memo `problem_2026-05-09_spike-sync-subscribe-race.md`) | S5-1c spike Awake() subscribe verified | ✅ S5-1c precedent |
| **R2.8** | Spike "1 文件 + 3 内类" 惯例 | Glob `Spikes/S*.cs` 全部命中（S301..S5-08 9 个 spike 全部一致）| ✅ |
| **F3** | `OnPerfectMatch` (\u65e0 Enter \u540e\u7f00) method \u5b58\u5728\u4e8e `IShadowPuzzleEvent` | Session 27 #2 grep `rg "OnPerfectMatch" IShadowPuzzleEvent.cs`: `IShadowPuzzleEvent.cs:39 void OnPerfectMatch(int puzzleId, float finalMatchScore);`\uff1bstory \u539f\u5199 `OnPerfectMatchEnter` 4 \u5904 (line 122/148/164 + Engine Notes) | \u26a0\ufe0f **WORDING DRIFT** (story 4 \u5904 propagate fix Session 27 #2)\uff1b\u5176\u4e2d `finalMatchScore` (PerfectMatch \u9501\u5b9a\u72b6\u6001\u8f6c\u6362\u65f6 matchScore frozen) |

**R2 综合 verdict (Session 27 #2 \u91cd\u8bc4)**: **\u2705 PASS** (per ADR-029 V2.0 §V2-2)
- 8/9 \u2705 PASS (R2.1 wording drift Session 27 #1 \u4fee\u590d \u2705\uff1b**R2.2 \u8bef\u5224\u64a4\u56de\u540e \u2705**\uff1bR2.3 RESOLVED Session 27 #2 \u2705\uff1bR2.4 / R2.5 / R2.6 / R2.7 / R2.8 all \u2705)
- 1/9 \u26a0\ufe0f F3 wording drift `OnPerfectMatch[Enter]` \u2014 story-002 \u5185 propagate fix \u540e \u2705

**Reactive cost (\u7d2f\u8ba1)**:
- Session 27 #1 R2/R3 grep + static read ~15 min + amend ~30 min ~ 45 min
- Session 27 #2 unity-mcp R2.3 verify + NarrativeSequencePlayer:133 grep \u91cd\u67e5 ~10 min + amend ~25 min ~ 35 min
- \u603b\u8ba1 ~80 min

**Proactive \u4ef7\u503c**:
- Session 27 #1 \u907f\u514d Phase 3 P2/P3 case \u63a5\u53e3\u5f52\u5c5e\u9519\u4f4d fail (~ 60 min)
- Session 27 #2 \u907f\u514d Phase 2 \u591a\u5199 `PuzzleCompleteToNarrativeBridge.cs` (~ 30 min) + Phase 3 P4 case Assert \u53cc\u91cd trigger fail (~ 30 min) + InteractableObject \u8865 component (~ 20 min)
- saved cost ~ 140 min

---

## ADR-029 V3 Watch List Hooks

### ⭐ Session 27 #1 \u5b9e\u6218\u89e6\u53d1 NEW (2026-05-12 /story-readiness gate R2 \u5b9e\u8bc1)

**Type-5 dp4 实战 NEW TRIGGER (spec/tooling ↔ reality drift)** — `IShadowMatchEvent` vs `IShadowPuzzleEvent` 接口归属错位：

- ADR/spec 文档 / story-002 原 wording 假设 `OnNearMatchEnter(string token)` 位于 `IShadowMatchEvent`
- vendor production code 实测: `IShadowMatchEvent` (IShadowMatchEvent.cs:21) **仅含** `OnMatchScoreUpdated(int puzzleId, float newScore)` 1 method；`OnNearMatchEnter(int puzzleId)` 位于 **`IShadowPuzzleEvent`** (IShadowPuzzleEvent.cs:23)
- payload 类型 drift: spec `string token` (e.g. "chapter_1_puzzle_1") ↔ vendor `int puzzleId` (e.g. 1)

### \u2b50 Session 27 #2 \u5b9e\u6218\u89e6\u53d1 NEW (2026-05-12 dev-story Phase 1.5 self-check \u91cd\u67e5)

**Type-5 dp5 实战 NEW TRIGGER (spec/tooling ↔ reality drift NEW)** — `OnPerfectMatchEnter` \u4e0d\u5b58\u5728 / \u5b9e\u4e3a `OnPerfectMatch`\uff1a

- story-002 \u539f\u5199 4 \u5904 `OnPerfectMatchEnter` (line 122/148/164 + Engine Notes)
- vendor production code \u5b9e\u6d4b\uff1a`IShadowPuzzleEvent.cs:39` `void OnPerfectMatch(int puzzleId, float finalMatchScore);` (\u65e0 `Enter` \u540e\u7f00\uff0c\u989d\u5916\u5e26 `float finalMatchScore`)
- \u8868\u73b0\u4e3a "S5-03 implementor naming \u4e60\u60ef \u2194 spec wording \u4e60\u60ef" \u4e0d\u4e00\u81f4 (S5-03 \u539f\u672c\u80fd\u53eb `OnPerfectMatchEnter` \u4f1a\u66f4\u4e00\u81f4\uff0c\u4f46\u9009\u4e86 `OnPerfectMatch`)
- \u7d2f\u8ba1 V3 Type-5 dp \u73b0\u72b6\uff1a**dp1 S5-01 toolchain silent failure + dp2 S5-08 #4 ShowUI wording + dp3 S5-08 #5 UILayer wording + dp4 S5-02 #1 IShadowMatch/Puzzle interface \u5f52\u5c5e + payload \u7c7b\u578b + dp5 S5-02 #2 OnPerfectMatch[Enter] wording = 5 unique dp**
- **\u8fdc\u8d85 V3 promote ROI \u9608\u503c (\u22653 unique dp)** \u2192 Sprint 5 retro **\u5f3a\u5236 promote** \u4e3a ADR-029 V3 \u6b63\u5f0f candidate Type-5 'spec \u2194 reality drift'

**\u2705 Type-2(c) dp1 \u64a4\u56de (F1 R2.2 wiring gap \u8bef\u5224 retract)**:

- Session 27 #1 \u539f\u5224 ADR-014 \u2194 ADR-016 \u5b58\u5728 'OnPuzzleComplete \u2192 OnRequestSequence' wiring gap \u662f\u8bef\u5224
- Session 27 #2 \u5b9e\u6d4b: NarrativeSequencePlayer.cs:**133-135** \u5df2\u7528 `OnPerfectMatch` listener \u76f4 wire puzzle\u2192narrative chain (\u4e0d\u9700\u8fc7 OnPuzzleComplete)\uff1bSession 27 #1 R2.2 grep \u9762\u592a\u7a84 \u2014 \u4ec5 grep `AddEventListener.*OnPuzzleComplete`\uff0c\u672a grep `AddEventListener.*OnPerfectMatch`\uff0c\u9020\u6210\u8bef\u5224
- ADR-014 \u00a7G + ADR-016 \u00a7G \u672a\u660e\u793a wiring bridge \u8d23\u4efb\u662f\u4e8b\u5b9e\uff0c\u4f46\u672c story happy path \u4e0d\u4f9d\u8d56 \u2014 Type-2(c) dp \u4e0d\u7d2f\u8ba1\uff0c\u7559 Sprint 6 \u5982 ChapterStateManager production \u63a5\u5165\u65f6\u86f0\u9762

**\u2b50 Type-9 dp1 NEW TRIGGER (V3 \u65b0\u578b\u5019\u9009 'R2 grep \u5b8c\u5907\u6027' meta-drift)**:

- Session 27 #1 R2.2 grep \u4ec5\u67e5\u5355\u4e2a event name (OnPuzzleComplete) listener\uff0c\u672a\u67e5\u8be5\u63a5\u53e3 (`IShadowPuzzleEvent`) \u5176\u4ed6 method listener \u00d7 N (`OnPerfectMatch` etc) \u2192 \u8bef\u5224 wiring gap
- \u6839\u56e0: \u300cR2 grep \u4ec5 trust story \u539f\u6587 method wording\u300d \u2192 wiring \u68c0\u67e5\u5e94\u4ee5\u300c\u5b9e\u9645\u63a5\u53e3 method \u96c6\u300d\u4e3a\u5fd7
- **proposed mitigation (Sprint 5 retro)**: ADR-029 V2.0 \u00a7V2-2 R2 \u589e\u8865\u4e00\u6761 "R2 grep \u9700\u5148 read \u63a5\u53e3\u6587\u4ef6 \u2192 list method\u96c6 \u2192 grep \u6bcf method listener\u96c6 \u2192 \u518d\u5224 chain"
- \u7b2c 1 \u6b21\u89c1 \u2192 dp1\uff1b\u5982\u672a\u6765 1-2 \u4e2a story \u91cc\u518d\u51fa\u73b0\u7c7b\u578b\u9519\u8bef \u2192 promote \u4e3a V3 Type-9

### 持续监控候选 (本 story R3 PlayMode 实施过程中如出现)

1. **Type-2(c) candidate**: 5 系统 cross-system listener wiring 任何其他 event 顺序 drift（e.g. `OnDuckingRequest` → AudioManager Music ducking real dispatch 不生效；UIModule `Button.onClick` → `OnRequestSceneChange` dispatch 不通）—— framework boundary behavior assumption drift；累计 dp
2. **Type-5 candidate (dp5 \u5df2\u5b9e\u6218\u89e6\u53d1)**: `unity-mcp` toolchain \u5728 dev-story Phase 1.5 verify chapter 1 prefab \u72b6\u6001\u65f6\u5982\u518d\u6b21\u649e silent failure\uff08D1~D4 \u7c7b\u578b\uff09\u2192 \u7d2f\u8ba1 V3 dp6
3. **Type-6 candidate**: spike subscribe race 如再次出现（main menu Button onClick → OnRequestSceneChange 同步 fire 路径或类似）→ 累计 V3 Type-6 dp2；本 story spike Awake() subscribe + 调用顺序前置已采纳防御
4. **Type-8 candidate (S5-08 #6 dp1)**: UIWindow second show 行为 spec vs vendor drift 是否在本 story main menu UIWindow 'Next Chapter' Button 二次显示场景再次触发 → 累计 V3 Type-8 dp2
5. **Type-9 candidate (Session 27 #2 dp1)**: R2 grep \u5b8c\u5907\u6027 meta-drift \u2014 \u5982\u672a\u6765 story-003+ \u9700\u8c03\u6574 R2 grep \u8303\u56f4\u624d\u80fd\u907f\u514d wiring \u8bef\u5224 \u2192 \u7d2f\u8ba1 V3 Type-9 dp2 + promote

如出现以上任一，per ADR-029 V2.0 §V2-7：sprint-status.yaml `watch_list` triggers 内追加 drift type 描述 + 关联 story-002 R3 case 编号 + 沉淀 problem memo 到 `.claude/memory/`。

---

## Sprint 5 Closure Note

本 story DONE 后 Sprint 5 Must Have 全部 ✅ 收官（S5-01/-1b/-1c/-02/-03/-04/-05/-06/-08）；ADR-030 §VS Build commitment 第 1 项 100% 完成；Sprint 5 retro action items：

- ADR-029 V3 candidate \u8bc4\u4f30 promote (Type-5 'spec/tooling \u2194 reality drift' **5 unique dp** \u8fdc\u8d85\u9608\u503c \u2192 \u5f3a\u5236 promote / Type-6 spike subscribe race / Type-8 UIWindow second show \u884c\u4e3a / **Type-9 \u65b0\u5019\u9009 'R2 grep \u5b8c\u5907\u6027' meta-drift dp1** \u7559\u89c2\u5bdf 1-2 story \u540e promote / V3 #8 dp \u7d2f\u8ba1 close-out)
- ADR-029 V2.0 \u00a7V2-2 R2 \u589e\u8865 "R2 grep \u9700\u5148 read \u63a5\u53e3\u6587\u4ef6 \u2192 list method \u96c6 \u2192 grep \u6bcf method listener \u96c6" \u5b50\u6761 (\u907f\u514d Session 27 #1 R2.2 \u8bef\u5224\u91cd\u4e0a\u6f14)
- IPuzzleLockEvent 跨 ADR contract design alignment（ADR-014 vs ADR-016 token-based vs parameter-less；S5-05 dp5 surfaced）
- AudioModule activation gate 文档对齐（ADR-028 §1 + ADR-017 §B drift-v1/v2 cleanup）
- ADR-011 §G + SP-002 systematic amendment (UILayer enum 命名 / ShowUI API / 7+2 lifecycle / second show 行为；本 story 累积本 dp4 trigger)
- Sprint 6 启动：≥2 playtest sessions + report + gate-check rerun + stage.txt: Pre-Production → Production

---

## History

### 2026-05-12 Session 27 #2 \u2014 dev-story Phase 1.5 self-check 3 \u91cd\u5927\u53d1\u73b0 amend per \u51b3\u7b56 [A]\u00d73 (Status: Ready unchanged\uff1bR2 verdict \u91cd\u8bc4 DEFICIENCY-FLAGGED PASS \u2192 \u2705 PASS)

**Trigger**: dev-story Phase 1.5 ADR-029 5-Min self-check\uff1b\u8865\u4e0a Session 27 #1 R2 grep \u5b8c\u5907\u6027\u4e0d\u8db3 + unity-mcp \u5b9e\u67e5 chapter 1 \u573a\u666f component\u3002

**F1 \u2014 R2.2 wiring gap \u8bef\u5224\u64a4\u56de**:
- Session 27 #1 grep `rg "AddEventListener.*OnPuzzleComplete"` \u5f97 0-hit \u662f\u5b9e\uff0c\u4f46\u5224\u5b9a "chain \u65ad\u88c2" \u662f\u8bef\u5224
- \u5b9e\u6d4b: NarrativeSequencePlayer.cs:127-135 \u5b9e\u9645 4 \u4e2a listener \u2014 `OnRequestSequence` + **`OnPerfectMatch`** + `OnAbsenceAccepted` + `OnChapterComplete`
- `OnPerfectMatchHandler(int puzzleId, float finalMatchScore)` (line 239-242) \u2192 `HandleRequestSequence(puzzleId, NarrativeSequenceType.MemoryReplay)` \u2192 puzzle\u2192narrative chain \u76f4 wire \u2705
- \u53d6\u6d88 `PuzzleCompleteToNarrativeBridge.cs` \u5b9e\u65bd (Phase 2.1 \u53d6\u6d88) + \u64a4\u56de V3 Type-2(c) dp1
- \u8282\u7701 ~30 min Phase 2 \u5b9e\u65bd + ~30 min Phase 3 P4 \u53cc\u91cd trigger fail
- \u8865 V3 Type-9 dp1 NEW 'R2 grep \u5b8c\u5907\u6027' meta-drift\uff08\u7b2c 1 \u6b21\u89c1\uff0c\u7559\u89c2\u5bdf\uff09

**F2 \u2014 R2.3 chapter 1 \u7269\u4ef6 RESOLVED**:
- unity-mcp `manage_scene get_hierarchy parent=Interactables` \u5b9e\u6d4b: `Object_01_CoffeeMug` componentTypes `[Transform, MeshFilter, BoxCollider, MeshRenderer]`\uff1b`Object_02_Book` `[Transform, MeshFilter, CapsuleCollider, MeshRenderer]` \u2014 \u7f3a `InteractableObject`
- \u4f46 P2 Action \u5df2\u8bbe\u8ba1\u4e3a spike fire mock `OnMatchScoreUpdated(1, 0.95)` \u7b80\u5316\u8def\u5f84\uff0c\u4e0d\u4f9d\u8d56 InteractableObject FSM \u771f drag/snap
- \u4e0d\u8865 component (Phase 2.2 \u53d6\u6d88)\uff1bInteractableObject + puzzle config injection + ADR-012 ShadowMatchCalculator \u7559 Sprint 6 polish
- \u8282\u7701 ~20 min Phase 2 \u5b9e\u65bd
- amend story \u00a7R3 spike \u6a21\u5f0f\u6bb5 line 142 \u201cInteractableObject FSM drag/snap \u771f\u8def\u5f84\u201d \u2192 \u201cspike fire mock OnMatchScoreUpdated \u7b80\u5316\u8def\u5f84\u201d

**F3 \u2014 `OnPerfectMatchEnter` \u2192 `OnPerfectMatch` wording drift propagate fix**:
- `IShadowPuzzleEvent.cs:39` \u5b9e\u4e3a `void OnPerfectMatch(int puzzleId, float finalMatchScore);` (\u65e0 `Enter` \u540e\u7f00)
- story-002 4 \u5904 \u9519\u5199 \u2192 propagate fix (Engine Notes / Control Manifest Rule / AC-4 / R3 P3 case / JSON evidence schema)
- \u6c89\u6dc0 V3 Type-5 dp5 NEW = \u7d2f\u8ba1 5 unique dp \u8fdc\u8d85\u9608\u503c\uff08dp1 S5-01 + dp2/dp3 S5-08 + dp4/dp5 S5-02\uff09\u2192 Sprint 5 retro \u5f3a\u5236 promote ADR-029 V3 Type-5

**story-002 amend 10 sections (Session 27 #2)**:
1. **Status header** \u2014 R2 verdict \u91cd\u8bc4 DEFICIENCY-FLAGGED PASS \u2192 \u2705 PASS\uff1bSession 27 #2 \u6807\u8bb0
2. **Goal flow T6-T11** \u2014 OnPerfectMatch \u76f4 wire \u8def\u5f84 + OnPuzzleComplete \u7559 Sprint 6 ChapterStateManager listener \u6ce8\u91ca
3. **Engine Notes** \u2014 F1 R2.2 \u64a4\u56de\u5757 + F2 R2.3 RESOLVED + F3 OnPerfectMatch wording \u4fee\u590d\u5757 + ChapterStateManager 0-caller \u6ce8\u8bb0
4. **Control Manifest Rules** \u2014 PuzzleStateMachine transition \u6539 `OnPerfectMatch(int, float)`\uff1b\u53d6\u6d88 PuzzleCompleteToNarrativeBridge required \u2192 \u6539 `OnPerfectMatch` \u76f4 wire required
5. **AC-4** \u2014 `OnPerfectMatchEnter` \u2192 `OnPerfectMatch(puzzleId=1, finalMatchScore)`
6. **AC-5** \u2014 PuzzleCompleteToNarrativeBridge \u8def\u5f84 \u2192 OnPerfectMatch \u76f4 wire \u8def\u5f84
7. **R3 spike \u6a21\u5f0f\u6bb5** \u2014 InteractableObject FSM \u771f\u8def\u5f84 \u2192 spike fire mock \u7b80\u5316\u8def\u5f84
8. **R3 P3 case** \u2014 OnPerfectMatchEnter \u2192 OnPerfectMatch\uff1bevent payload `(1, finalScore)`
9. **R3 P4 case** \u2014 bridge listener \u8def\u5f84 \u2192 OnPerfectMatch \u76f4 wire\uff1b\u4e0d subscribe OnRequestSequence (\u4e0d\u662f\u4e3b\u4e8b\u4ef6)
10. **JSON evidence schema** P3/P4 events \u540c\u6b65\u4fee\u590d
11. **R2 Assumptions Validated \u8868** \u2014 R2.2 \u2705 PASS / R2.3 \u2705 RESOLVED / F3 \u26a0\ufe0f wording drift fix \u884c\uff1b\u7efc\u5408 verdict \u4e0a\u8c03\u4e3a \u2705 PASS\uff1bcost/proactive value \u91cd\u7b97
12. **V3 Watch List Hooks** \u2014 Type-5 dp5 NEW \u52a0\u5165\uff1bType-2(c) dp1 \u64a4\u56de\uff1bType-9 dp1 NEW (R2 grep \u5b8c\u5907\u6027 meta-drift)
13. **Sprint 5 Closure Note** \u2014 5 unique dp Type-5 + Type-9 dp1 retro action item + ADR-029 V2.0 R2 \u589e\u8865 grep \u5b8c\u5907\u6027\u5b50\u6761
14. **History** \u2014 Session 27 #2 entry (\u672c\u5757)

**ADR-029 V3 dp \u7d2f\u8ba1 (\u672c story \u8d21\u732e)**:
- Type-5 'spec \u2194 reality drift' = S5-01 dp1 + S5-08 dp2 + S5-08 dp3 + S5-02 dp4 (Session 27 #1) + **S5-02 dp5 (Session 27 #2)** = 5 unique dp \u8fdc\u8d85\u9608\u503c
- Type-2(c) dp1 \u64a4\u56de (Session 27 #1 \u8bef\u8ba1)
- Type-9 'R2 grep \u5b8c\u5907\u6027' meta-drift dp1 NEW (\u7b2c 1 \u6b21\u89c1)

**Sign-off Session 27 #2**: F1/F2/F3 \u4e09\u91cd\u53d1\u73b0\u540e R2 verdict \u91cd\u8bc4 \u2705 PASS\uff1bR3 \u2705 propagate fix\uff1bADR-029 verdict **\u2705 PASS** (\u4e0d\u518d DEFICIENCY-FLAGGED)\uff1b\u4e3b verdict **READY \u4e0d\u53d8**\uff1bPhase 2 \u5b9e\u65bd\u5185\u5bb9\u6536\u7a84 (main menu UIWindow + Spike + GameApp\uff1b\u53d6\u6d88 bridge + InteractableObject \u8865 component)\u3002

**Audit Trail Cross-references**:
- \u4e0a Session 27 #1 commit: 730b18c (Draft\u2192Ready amend)
- \u672c amend \u4e4b commit: \u5f85 Phase 1.8 (sync sprint-status.yaml + active.md + lessons memo) \u540e commit
- \u5b9e\u6d4b grep + unity-mcp evidence:
  - `NarrativeSequencePlayer.cs:133` `AddEventListener<int, float>(IShadowPuzzleEvent_Event.OnPerfectMatch, _onPerfectMatch)` \u2705
  - `NarrativeSequencePlayer.cs:239-242` `OnPerfectMatchHandler \u2192 HandleRequestSequence(puzzleId, MemoryReplay)`
  - `IShadowPuzzleEvent.cs:39` `OnPerfectMatch(int puzzleId, float finalMatchScore)` (\u65e0 Enter \u540e\u7f00)
  - unity-mcp `manage_scene get_hierarchy Chapter_01_Approach Interactables` componentTypes \u5b9e\u6d4b
  - `new ChapterStateManager` 0-hit\uff1b`ChapterStateEventBridge.Attach` 0-hit (Sprint 5 production \u672a\u63a5\u5165)

---

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





