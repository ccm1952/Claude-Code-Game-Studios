// 该文件由Cursor 自动生成

# Story 003: Chapter 1 Error/Restart Path — TryResolveOrFail / Newest-Wins Pending / Asset Fail Retry Exhaust / RecoverToIdle Restart

> **Epic**: VS Chapter 1
> **Story ID**: vs-chapter-1-003 (S6-04 — Sprint 6 Track C 最后 1 story；S5-2b Sprint 5 [B] split_2 placeholder 派生)
> **Sprint**: 6 (S6-04 — Track C UI Polish + Error)
> **Story Type**: Integration
> **Complexity Points**: 1
> **GDD Requirement**: TR-scene-002 (Error state recovery) + TR-scene-005 (11-step transition robust) + TR-scene-009 (Newest-wins pending semantic per ADR-009 AC-9) + TR-scene-010 (Asset load fail retry/exhaust per ADR-009 MaxLoadRetry=2)
> **ADR References**: ADR-009 V2.x (Scene Lifecycle 6-state machine + Error state + RecoverToIdle + TryResolveOrFail + 11-step + MaxLoadRetry=2 + AC-5 in-flight dedupe + AC-9 newest-wins pending + AC-10 Error 静默丢弃 + AC-11 DrainPending + AC-12 RecoverToIdle) + ADR-027 §1 (ISceneEvent contract — OnRequestSceneChange + OnSceneLoadFailed + OnSceneReady) + ADR-029 V3.0.1 (R2 deficiency-flagged PASS path) + ADR-029 V2.0 §V2-1.b (Interface Method Set Fan-out Check) + ADR-030 §VS Build commit 第 1 项 chapter 1 robustness 补全
> **Status**: **Phase 0 R2 verify ✅ DONE + Phase 1 readiness gate ✅ READY** (2026-05-13 afternoon Session 30 — sender-only narrow scope [A] satisfaction → ready for Phase 2 production code 实施)
> **Created**: 2026-05-13 afternoon (Sprint 6 Session 30 continue — rewrite from sprint-status.yaml S6-04 placeholder per V3.0.1 vendor reality compliant + 5 R3 case wording amend [A] decision)
> **Depends on**: S5-02 ✅ (Chapter 1 end-to-end happy path baseline + chapter 2 fixture placeholder + DevTestState `[main-menu]` mode precedent) + S5-1b ✅ (SceneManager boot pipeline + fixture ChapterDataProvider + spike P4/P5 isolated local SceneManager error path precedent) + S5-1c ✅ (ADR-009 listener-path driver + F4 stub removal + DriveTransitionAsync exception catch 兜底) + S6-07 ✅ (DevTestState `[main-menu]` mode 复用 precedent — V3.0.1 dp8 candidate 阈值 4 + S6-07/S6-08 已 2 spike) + S6-08 ✅ (sender-only narrow scope [A] mode 复用 precedent + ADR-029 V2.0 §V2-1.b R2 deficiency-flagged PASS path 二次实战)

---

## Context

S5-02 Sprint 5 [B] split_2 决策 (2026-05-11) 把 chapter 1 end-to-end story 拆分为 happy path (S5-02 ✅ DONE 2026-05-12) + error/restart path (S5-2b → Sprint 6 backlog placeholder)；Sprint 6 启动后 placeholder 升级为 S6-04 Must Have 1 SP（per Sprint 5 retro [B] split_2 派生）。本 story 完整 close out Sprint 5 [B] split_2 commitment + 补全 ADR-030 §VS Build commit 第 1 项 chapter 1 robustness 验证 (≥3 playtest sessions 前 prerequisite)。

**S6-04 narrow scope [A] 决策 (2026-05-13 afternoon)**：Phase 0 R2 vendor reality verify 实证 vendor SceneManager 全部 error/restart 路径已 production (Error state + RecoverToIdle + TryResolveOrFail + MaxLoadRetry=2 + DriveTransitionAsync exception catch + newest-wins pending + DrainPending)，本 story **0 production code change** — 仅做 R3 PlayMode probe 5 case 验 vendor 行为符合 spec + 暴露 S5-2b Sprint 5 placeholder 描述中的 2 处 spec wording drift (mid-transition cancel ≠ vendor newest-wins pending；repeated rapid debounce ≠ vendor newest-wins overwrite) 通过本 story story rewrite 修正。

### S5-2b Sprint 5 placeholder 描述 vs S6-04 narrow scope [A] amend

| S5-2b 原描述 (Sprint 5 placeholder) | S6-04 amend (vendor reality) | Wording drift type |
|---|---|---|
| unknown chapter Button click (chapter id 99 / -1 / null) → Idle 解锁 | unknown chapter Button click → `TryResolveOrFail` → `OnSceneLoadFailed` + state=Error (不"解锁"，需 `RecoverToIdle()` 显式) | ⚠️ wording drift (Idle 解锁 → Error state) |
| mid-transition cancel → unload 干净 | mid-transition 重复 fire → **newest-wins pending** (AC-9)；vendor 无显式 cancel API；transition 完成后 DrainPending 消费 newest | ⚠️ **wording drift** (cancel → newest-wins pending) |
| asset load fail (fixture chapter id 6 path 不存在) → Error 状态 + restart 路径 | asset load fail (fixture chapter id 99 + bad sceneId) → 2 次 retry exhaust → `OnSceneLoadFailed` + state=Error | ✅ FULLY (描述准确) |
| restart-from-Error state recovery → state machine reset 干净 | `RecoverToIdle()` → state=Idle + `DrainPending`；保留 `_pendingTargetChapterId`；fresh `OnRequestSceneChange(1)` re-fire 路径 | ✅ FULLY (描述准确) |
| repeated rapid OnRequestSceneChange → debounce | repeated rapid → **newest-wins pending overwrite** (AC-9)；vendor 无 time-based debounce | ⚠️ **wording drift** (debounce → newest-wins overwrite) |

**V3.0.1 governance**：Sprint 5 placeholder 2 处 wording drift 通过本 story Phase 1 rewrite per V3.0.1 vendor reality compliant 修正 — sprint backlog placeholder wording drift 阈值阶进 V3.0.1 dp11 candidate (留观察 — 单次不触发 V3.1 trigger)。

### S6-04 Goal Flow (T0 → T7)

```
T0  game boot → S6-07 ✅ main menu shown → user click NewGame → chapter 1 loaded (production happy path baseline 经 S5-02 验证)
T1  Spike P1 — chapter 1 baseline 后 fire OnRequestSceneChange(99) (unknown chapter id)
    → SceneManager.OnRequestSceneChange 内 TryResolveOrFail(99) 走 _chapterDataProvider(99)==null 路径
    → OnSceneLoadFailed(99, "Chapter ID 99 not found in TbChapter.") + TransitionTo(Error)
    → _currentChapterId / _inflightChapterId 不更新 (保留 chapter 1)
    → Assert state=Error + OnSceneLoadFailed fired 1 次 + chapter 1 _currentChapterId 不变
T2  Spike P2 — chapter 1 baseline 后 fire OnRequestSceneChange(2) (start transition to chapter 2)
    → 在 TransitionOut state 中再次 fire OnRequestSceneChange(1) (newest target = chapter 1)
    → AC-9 newest-wins pending：_pendingTargetChapterId = 1 (overwrite null)
    → 等 chapter 2 transition 完成后 (state=Idle) → DrainPending 消费 → 切回 chapter 1
    → Assert pending 期间 PendingTargetChapterIdForTest==1 + transition 完成后 CurrentChapterId==1 (newest wins)
T3  Spike P3 — register fixture provider 多 1 个 chapter id 99 with bad sceneId="NotExistScene_Chapter99"
    → fire OnRequestSceneChange(99) → TryResolveOrFail PASS (provider 返非 null) → 进 TransitionOut
    → BeginTransitionAsync → Step 5-7 unload chapter 1 → Step 8-10 LoadChapterSceneAsync(99)
    → LoadSceneAsync("NotExistScene_Chapter99") throw → catch attempt 1/2 → retry attempt 2/2 → throw
    → MaxLoadRetry=2 exhaust → OnSceneLoadFailed(99, lastError.Message) + TransitionTo(Error)
    → Assert OnSceneLoadFailed fired 1 次 (attempt 2 retry exhaust) + state=Error + error.Message contains "invalid" or "throw"
T4  Spike P4 — P3 后 state=Error + chapter 1 已 unloaded (Step 5-7 in P3 BeginTransitionAsync 已执行)
    → fire OnRequestSceneChange(1) — Error 状态 AC-10 静默丢弃 + warning
    → 调用 RecoverToIdle() → state=Error → Idle + DrainPending (无 pending)
    → fire OnRequestSceneChange(1) (fresh re-attempt)
    → TryResolveOrFail(1) PASS → BeginTransitionAsync(1) → LoadChapterSceneAsync(1) success
    → Assert state path Error → (silent drop) → Idle (via RecoverToIdle) → TransitionOut → ... → Idle + CurrentChapterId==1
T5  Spike P5 — Idle state 下连续 rapid fire 3 次 不同 chapter id (1 → 2 → 1)
    → 第 1 次 fire(1) 同 _currentChapterId=1 → AC-8 静默派 OnSceneReady (无 transition)
    → 第 2 次 fire(2) (Idle) → TryResolveOrFail PASS → TransitionTo(TransitionOut) + DriveTransitionAsync(2)
    → 第 3 次 fire(1) (TransitionOut state) → AC-9 newest-wins pending _pendingTargetChapterId=1 (overwrite null)
    → 等 chapter 2 transition 完成后 (state=Idle) → DrainPending(1) → load chapter 1
    → Assert pending 期间 OnRequestSceneChange 第 3 次 fire 走 AC-9 path (not in-flight dedupe AC-5 因为 newest != inflight)
    → Total: chapter 1 → chapter 2 → chapter 1 transition × 2 (final state CurrentChapterId==1)
T6  Spike WriteResultJson → ~/Library/Application Support/DefaultCompany/Unity/S6-04_Result.json
T7  AC verify + evidence doc + closure
```

---

## ADR Decision Summary

**ADR-009 V2.x 完整 inherit** — 不 amend ADR 仅 verify vendor reality 满足 spec：
- 6-state machine (Idle / TransitionOut / Unloading / Loading / TransitionIn / **Error**) + `RecoverToIdle()` 显式恢复
- `TryResolveOrFail(int chapterId)` — provider null 兼容旧行为返 true；provider != null 且返 null → `OnSceneLoadFailed` + `TransitionTo(Error)`
- `MaxLoadRetry = 2` — `LoadChapterSceneAsync` 2 次 retry exhaust 后 `OnSceneLoadFailed` + `TransitionTo(Error)`
- AC-5: in-flight 去重 (same target during transition → silent drop + Info log)
- AC-8: same target on Idle → silent `OnSceneReady` (no transition)
- AC-9: diff target during transition → newest-wins `_pendingTargetChapterId` overwrite
- AC-10: Error 状态 `OnRequestSceneChange` 静默丢弃 + warning
- AC-11: `DrainPending` on return-to-Idle (also `TryResolveOrFail` 再次 + same target → silent `OnSceneReady`)
- AC-12: Error → Idle 显式 via `RecoverToIdle()`；非 Error 状态调 `RecoverToIdle()` no-op + warning

**ADR-027 §1 ISceneEvent contract inherit** — 9 method 已冻结 (5 sender Scene Manager 唯一 + `OnRequestSceneChange` listener 唯一)：
- `OnRequestSceneChange(int targetChapterId)` — sender Chapter State / Narrative / Pause Menu / Title；listener SceneManager 唯一
- `OnSceneReady(int chapterId)` — sender SceneManager；listener UI / Input / Gameplay
- `OnSceneLoadFailed(int chapterId, string error)` — sender SceneManager；listener UI 错误对话框 ⚠️ 0-production listener (Sprint 7+ ADR-010 InputManager epic 一并 wire)
- 其余 6 sender 仅 verify lifecycle 已激活 (R3 P2/P3/P5 case 触发 OnSceneTransitionBegin/Unload/Load/Complete/TransitionEnd 实证)

**ADR-029 V3.0.1 R2 deficiency-flagged PASS path** — 本 story 二次实战 (S6-08 已首次 deficiency-flagged PASS)：R2.7 IGameFlowEvent.OnReturnToMainMenu 0-production → DEFERRED Sprint 7+ ADR-010 epic boundary (类似 S6-07 R2.6/R2.8 + S6-08 R2.7 模式)。

**ADR-030 §VS Build commit** — 本 story 完成 chapter 1 robustness 验证；S6-01 playtest 1 prerequisite 100% ready (Track C 3/3 done + Track B 2/2 done)。

---

## Engine

Unity 2022.3.62f2 LTS + URP + TEngine 6.2.1 + YooAsset 2.3.17 + UniTask 2.5.10 | **Risk**: LOW (0 production code change — vendor reality verify only)

---

## Engine Notes (R2 grep verified 2026-05-13 afternoon)

**SceneManager.cs vendor API surface (real, verified)**：
- `SceneManager` partial class location: `Assets/GameScripts/HotFix/GameLogic/Scene/SceneManager.cs` (1 file 720 lines)
- `SceneManagerState` enum: `Idle / TransitionOut / Unloading / Loading / TransitionIn / Error` (line 15-23)
- `ISceneManager` interface: `CurrentState` + `CurrentChapterId` + `IsTransitioning` (line 29-39) — `IsTransitioning` 仅 Idle / Error 为 false
- `RecoverToIdle()` public method (line 293-303) — Error→Idle + DrainPending；非 Error 状态 no-op + warning
- `TryResolveOrFail(int chapterId)` private (line 248-263) — provider == null 兼容旧行为返 true；provider != null 且 provider(id)==null → `OnSceneLoadFailed(id, "Chapter ID X not found in TbChapter.")` + `TransitionTo(Error)` 返 false
- `OnRequestSceneChange(int targetChapterId)` handler (line 309-352) — `[EventInterface]` listener；4 path：(1) Error → silent drop + warning AC-10；(2) IsTransitioning + same target → silent drop AC-5；(3) Idle + same target + currentChapterId != NoChapterId → silent `OnSceneReady` AC-8；(4) Idle + diff target → `TryResolveOrFail` → set _currentChapterId/_inflightChapterId → `TransitionTo(TransitionOut)` + `DriveTransitionAsync(target).Forget()`；(5) Non-Idle + diff target → `_pendingTargetChapterId = target` AC-9
- `DrainPending()` private (line 358-385) — same target → silent `OnSceneReady`；diff target → `TryResolveOrFail` → set _currentChapterId + TransitionTo(TransitionOut) + DriveTransitionAsync().Forget()
- `DriveTransitionAsync(int targetChapterId)` private `async UniTaskVoid` (line 405-415) — try-catch await BeginTransitionAsync；catch 兜底 Log.Error
- `BeginTransitionAsync(int targetChapterId)` public `async UniTask` (line 671-706) — 11-step driver：Step 3 OnSceneTransitionBegin → Step 4 FadeOutAsync + TransitionTo(Unloading) → Step 5-7 UnloadCurrentChapterAsync → Step 8-10 TransitionTo(Loading) + LoadChapterSceneAsync → 失败短路 if state==Error return → Step 11 TransitionTo(TransitionIn) + OnSceneReady + FadeInAsync + OnSceneTransitionEnd + TransitionTo(Idle) + _inflightChapterId=NoChapterId + DrainPending
- `LoadChapterSceneAsync(int targetChapterId)` public `async UniTask` (line 448-534) — 2 fail-loud + 2 retry path：(1) provider==null fail-loud → OnSceneLoadFailed + Error；(2) provider(id)==null fail-loud (Luban TbChapter missing) → OnSceneLoadFailed + Error；(3) 2 attempt retry — try CreateResourceDownloader + LoadSceneAsync + ActivateScene；catch 各 attempt；exhaust → OnSceneLoadFailed(lastError.Message) + Error
- `UnloadCurrentChapterAsync()` public `async UniTask` (line 599-637) — first-boot guard via `_currentLoadedChapterId == NoChapterId || string.IsNullOrEmpty(_currentChapterSceneName)`；Step 5 OnSceneUnloadBegin + UniTask.Yield → try UnloadAsync → finally ClearCurrentChapterSceneName + UnloadUnusedAssets + GC
- 测试桩：`CurrentChapterSceneNameForTest` (string) / `CurrentLoadedChapterIdForTest` (int) / `PendingTargetChapterIdForTest` (int?) / `InflightChapterIdForTest` (int) — 全 readonly
- 测试 hook：`AdvanceStateForTest(SceneManagerState)` + `SetCurrentChapterSceneNameForTest(string)` (internal — 同 asmdef test only)

**ISceneEvent.cs vendor contract (R2 grep verified)**：
- 9 method 已冻结 — `OnRequestSceneChange(int)` + `OnSceneReady(int)` + `OnSceneTransitionBegin(int from, int to)` + `OnSceneUnloadBegin(int)` + `OnSceneDownloadProgress(float, long, long)` + `OnSceneLoadProgress(string, float)` + `OnSceneLoadComplete(int, string bgmAsset)` + `OnSceneTransitionEnd(int)` + `OnSceneLoadFailed(int, string error)`
- Line 24 spec wording: "targetChapterId 1..5；同 CurrentChapterId → 静默派发 OnSceneReady；无效值由 Scene Manager 进入 Error 状态" — 与 vendor `TryResolveOrFail` 实现一致

**GameApp.cs fixture provider (R2 grep verified)**：
- `BuildFixtureChapterDataProvider()` (line 66-85) — id switch：1 / 2 → `ChapterData("Chapter_01_Approach")` (chapter 2 MVP placeholder per S5-02 dp6 NEW fix)；其余 id → null fail-loud
- 本 story spike 需要 register 自己的 fixture (chapter 99 with bad sceneId)；通过 spike isolated local `SceneManager` 实例 — S5-1b precedent (line 379+) — `local.RegisterChapterDataProvider(id => id == 99 ? new ChapterData(99, "NotExistScene_Chapter99", "", 1.0f, "#000000") : null)`

**DevTestState.cs `[main-menu]` mode (R2 grep verified)**：
- 当前条件 (line 34): `HasSpike("S5-02") || HasSpike("S6-07") || HasSpike("S6-08")` — 本 story 扩展 +`|| HasSpike("S6-04")` → V3.0.1 dp8 candidate +1 = **4 距阈值 4 触发**（阈值阶进 V3.1 trigger 评估）
- ShowMainMenuPanelAsync (line 67-77) — 走 production UIWindow 路径 `GameModule.UI.ShowUIAsyncAwait<MainMenuPanel>()`；spike subscribe 后 Button.onClick.Invoke()
- S6-04 spike 选择：仍走 main-menu mode (不 pre-dispatch OnRequestSceneChange) — chapter 1 baseline 由 spike P 第一个 case 主动 fire OnRequestSceneChange(1) 加载完成后再进 error path test；保持 5 case sequence 在 cold-boot 与 error path 间清晰

**S5-1b / S5-1c spike error path precedent (R2 grep verified)**：
- S5-1b P4 isolated local SceneManager + register fixture chapter 1 → fire OnRequestSceneChange(99) unknown chapter → assert OnSceneLoadFailed count + chapterId + CurrentState==Error
- S5-1b P5 isolated local + 0-registered provider → fire OnRequestSceneChange(1) → assert OnSceneLoadFailed error contains "not registered" + CurrentState==Error
- Listener cleanup: `Action<int, string> onLF = ...; GameEvent.AddEventListener<int, string>(ISceneEvent_Event.OnSceneLoadFailed, onLF); ... finally { GameEvent.RemoveEventListener<int, string>(ISceneEvent_Event.OnSceneLoadFailed, onLF); }` — listener spy 模式 (S5-05 P3 precedent + S6-08 R3 5 case 全部 listener spy 复用)

---

## Acceptance Criteria (10 AC)

- **AC-1** Spike `S6-04_ErrorRestartPath.cs` 注册 `S604Spike : IDevSpike` + S604Runtime (MonoBehaviour) + S604Tester (pure logic) 1 file 3 inner class (S5-1b/1c/-08/-02/-07/-08 precedent) + `GameApp.RegisterDevSpikes` 切换 S608Spike → S604Spike + `DevTestState.OnEnter` `HasSpike("S6-04")` 加入 `[main-menu]` mode condition
- **AC-2** Spike Awake() 同步 subscribe `IInputBlockerEvent` (verify 0 fire 因 chapter transition 与 popup 无关) + `ISceneEvent.OnSceneLoadFailed` + `OnSceneLoadComplete` + `OnSceneTransitionBegin` + `OnSceneTransitionEnd` + `OnSceneReady` listener spy (per S5-05 P3 + S6-08 5 case precedent)
- **AC-3** Spike fixture chapter ID 99 with bad sceneId `"NotExistScene_Chapter99"` via isolated local `SceneManager` instance + `RegisterChapterDataProvider` 注入 (P3 case 隔离 production GameApp fixture 避免污染)
- **AC-4** R3 P1 case **UnknownChapterTryResolveOrFail** — chapter 1 baseline 后 fire `OnRequestSceneChange(99)` → assert `state=Error` + `OnSceneLoadFailed(99, "Chapter ID 99 not found in TbChapter.")` 触发 1 次 + `CurrentChapterId=1` 不变 (production provider chapter 99 不存在路径)
- **AC-5** R3 P2 case **NewestWinsPendingDuringTransition** — chapter 1 baseline 后 fire `OnRequestSceneChange(2)` (start transition) → 在 TransitionOut/Unloading/Loading state 中 fire `OnRequestSceneChange(1)` → assert `PendingTargetChapterIdForTest=1` 期间 + transition 完成后 `CurrentChapterId=1` (newest wins drain)
- **AC-6** R3 P3 case **AssetLoadFailRetryExhaust** — isolated local SceneManager + fixture chapter 99 with bad sceneId → fire `OnRequestSceneChange(99)` → assert `state=Error` + `OnSceneLoadFailed(99, error)` 触发 1 次 + `error.Message` non-empty + 2 次 retry attempt warning log 触发 (per MaxLoadRetry=2 spec)
- **AC-7** R3 P4 case **RestartFromErrorRecovery** — P3 后 state=Error → fire `OnRequestSceneChange(1)` 走 AC-10 silent drop path (assert warning log) → `RecoverToIdle()` 调用 → assert state Error→Idle → fire `OnRequestSceneChange(1)` (fresh re-attempt) → assert state Idle→TransitionOut→...→Idle + `CurrentChapterId=1`
- **AC-8** R3 P5 case **RapidNewestWinsOverwrite** — Idle state 下连续 rapid fire (1) → (2) → (1) (3 次)：第 1 次 fire(1) 同 currentChapterId silent OnSceneReady (AC-8) → 第 2 次 fire(2) 进 TransitionOut → 第 3 次 fire(1) (TransitionOut state) pending=1 (AC-9 newest-wins) → transition 完成后 DrainPending(1) → final `CurrentChapterId=1`
- **AC-9** Spike JSON evidence dump `~/Library/Application Support/DefaultCompany/Unity/S6-04_Result.json` 18+ assert pass-rate + per-case timing breakdown + `all_passed=true` + total elapsed ≤ 8000ms perf budget (chapter 1 加载 + chapter 2 加载 + chapter 99 retry × 2 + chapter 1 reload + chapter 2 reload + chapter 1 reload)
- **AC-10** Spike `unexpected_error_count = 0` (per S6-07/S6-08 precedent) — expected error/warning log (TryResolveOrFail Log.Warning + LoadChapterSceneAsync retry attempt Log.Warning + OnSceneLoadFailed Log.Error 兜底 + AC-10 silent drop Log.Warning + RecoverToIdle 调用 in Error state) 走 expected pattern allowlist 不算 unexpected

---

## Implementation Notes

**Phase 2 production code 实施清单 (0 production code change — spike + Editor only)**：

1. **`Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-04_ErrorRestartPath.cs`** NEW (~600-700 行)
   - Outer class `S604Spike : IDevSpike` (per S5-1b/1c/-08/-02/-07/-08 1-file 3-inner-class precedent)
   - Inner `S604Runtime : MonoBehaviour` — Awake() 同步 subscribe + S604Tester instantiate；OnDestroy listener cleanup
   - Inner `S604Tester` pure logic — 5 R3 case (P1 UnknownChapterTryResolveOrFail + P2 NewestWinsPendingDuringTransition + P3 AssetLoadFailRetryExhaust + P4 RestartFromErrorRecovery + P5 RapidNewestWinsOverwrite) + JSON evidence dump WriteResultJson
   - Listener spy 模式 (S5-05 P3 + S6-08 5 case precedent) — `int _onSceneLoadFailedCount`, `(int chapterId, string error) _lastLoadFailedPayload`, `List<string> _allTransitionBeginPairs`, etc
   - Reflection helper (per S6-08 GetCurrentPopupType pattern) — 拿 `_state` (via `CurrentState` 已 public 不需要)；`PendingTargetChapterIdForTest` / `InflightChapterIdForTest` 已 public test hook
   - 5 case state cleanup — P2/P5 head/tail `await UniTask.WaitUntil(() => sm.CurrentState == SceneManagerState.Idle)` 确保 inter-case 不污染 (per S6-08 P3 ClearAndClosePopupQueue precedent)
   - JSON evidence dump per S6-08 schema — `all_passed` / `total_push_count` / `total_pop_count` (本 story 替换为 `total_load_failed_count` / `total_load_complete_count` / `total_transition_begin_count` / `total_transition_end_count`) / `unexpected_error_count` / per-case events list + asserts dictionary + elapsed_ms breakdown

2. **`Assets/GameScripts/HotFix/GameLogic/GameApp.cs`** amend (~3 行 change)
   - `RegisterDevSpikes()` 内：`GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S604Spike());` (取代 S608Spike 注释)
   - Comment block 加 S6-04 active spike note (per S6-07/S6-08 precedent — `// S6-08 已在 Sprint 6 ✅ DONE...` + `// S6-04 当前 active spike ...`)

3. **`Assets/GameScripts/HotFix/GameLogic/GameFlow/DevTestState.cs`** amend (~1 行 change)
   - line 34 condition 扩展：`if (DevTest.DevBootstrap.HasSpike("S5-02") || DevTest.DevBootstrap.HasSpike("S6-07") || DevTest.DevBootstrap.HasSpike("S6-08") || DevTest.DevBootstrap.HasSpike("S6-04"))` — V3.0.1 dp8 candidate +1 = 4 (达阈值；Sprint 6 retro 评估是否 promote V3.1 trigger)
   - Log scope 复用 `[main-menu]` 不需 rename (S6-07 已 generalize)

**Phase 3 R3 PlayMode execution**：
- `manage_editor` action=`enter_play_mode` → wait `S604Spike` `IsCompleted=true`：通过 spike 内 `runner.IsCompleted` 触发 → `manage_editor` action=`exit_play_mode`
- 读 `~/Library/Application Support/DefaultCompany/Unity/S6-04_Result.json` 验 `all_passed=true` + `unexpected_error_count=0` + 18+ asserts PASS

**Phase 4 evidence doc**：
- `production/qa/playmode-error-restart-path-2026-05-13.md` NEW ~400 行 8 sections (§0..§8 per S6-08 precedent)
  - §0 概要 + Verdict + Total elapsed
  - §1 R3 5 case detail (P1-P5 each with Setup + Action + Assert + Events captured + Elapsed)
  - §2 R2 7/7 closure 表
  - §3 AC 10/10 verify 表
  - §4 V3.0.1 Watch List Hooks closure + dp11 candidate evaluation (sprint backlog placeholder wording drift)
  - §5 Sprint 6 Track C closure insight (3/3 done + Track C 100% complete)
  - §6 Files changed
  - §7 References
  - §8 Verdict + Sign-off

---

## R3 PlayMode Probe (5 case detail)

### P1 UnknownChapterTryResolveOrFail

**Setup**:
- Chapter 1 baseline already loaded via main-menu MainMenuPanel.NewGame Button click (production GameApp fixture provider chapter 1)
- Subscribe `ISceneEvent_Event.OnSceneLoadFailed` listener spy (capture chapterId + error)
- `var sm = GameLogic.GameModule.Scene` (production SceneManager 实例 — 经 GameApp.Entrance wired)

**Action**:
```csharp
GameEvent.Get<ISceneEvent>().OnRequestSceneChange(99);
await UniTask.DelayFrame(2);
```

**Assert** (5 asserts):
- `state_after_fire_99 == Error` (vendor TryResolveOrFail 路径 transition 到 Error)
- `OnSceneLoadFailed_count == 1` (期望 1 次 fire)
- `OnSceneLoadFailed_chapterId == 99` (payload 准确)
- `OnSceneLoadFailed_error contains "Chapter ID 99 not found"` (vendor warning message format align)
- `CurrentChapterId == 1` (unchanged — _currentChapterId 不更新 per spec line 244)

**Cleanup**: 调用 `sm.RecoverToIdle()` 让 P2 从 Idle state 开始

### P2 NewestWinsPendingDuringTransition

**Setup**:
- Chapter 1 baseline (chapter 1 已经在 P1 cleanup 后 Idle state — 仍 currentChapterId=1)
- Subscribe `OnSceneTransitionBegin/End/Ready/LoadComplete` listener spy

**Action**:
```csharp
GameEvent.Get<ISceneEvent>().OnRequestSceneChange(2); // start chapter 2 transition
await UniTask.DelayFrame(1); // ensure TransitionOut state 进入
GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1); // during transition newest target = chapter 1
int pendingDuringTransition = sm.PendingTargetChapterIdForTest ?? -999;
await UniTask.WaitUntil(() => sm.CurrentState == GameLogic.SceneManagerState.Idle);
```

**Assert** (5 asserts):
- `pending_during_transition == 1` (newest-wins overwrite)
- `final_state == Idle`
- `final_currentChapterId == 1` (newest wins after drain — chapter 1 reload)
- `OnSceneTransitionBegin fired 2 times` (chapter 1→2 + chapter 2→1 — drain pending 触发 second transition)
- `OnSceneTransitionEnd fired 2 times` (对称)

**Cleanup**: ensure final state Idle + currentChapterId=1 (P3 starts with isolated local — production state 不重要)

### P3 AssetLoadFailRetryExhaust

**Setup**:
- 创建 isolated local SceneManager (NOT production GameApp.Scene) — `var local = new GameLogic.SceneManager(); local.Init();`
- Register fixture provider: `local.RegisterChapterDataProvider(id => id == 99 ? new ChapterData(99, "NotExistScene_Chapter99", "", 1.0f, "#000000") : null);`
- Subscribe `OnSceneLoadFailed` listener spy (新 listener instance — isolated)

**Action**:
```csharp
Debug.Log("[S6-04][P3] expected Debug.LogWarning ×2 below: attempt 1/2 failed + attempt 2/2 failed");
Debug.Log("[S6-04][P3] expected Debug.LogError below: all 2 attempts exhausted");
GameEvent.Get<ISceneEvent>().OnRequestSceneChange(99); // local SceneManager 接收
await UniTask.WaitUntil(() => local.CurrentState == GameLogic.SceneManagerState.Error, timeout: 5000ms);
```

**Assert** (4 asserts):
- `local_state == Error` (retry exhaust path)
- `OnSceneLoadFailed_count >= 1` (期望 1 次最终 fail；retry 期间不算)
- `OnSceneLoadFailed_chapterId == 99` (payload)
- `OnSceneLoadFailed_error non-empty` (vendor lastError.Message — may contain "valid" / "throw" / "not loaded")

**Cleanup**: `local.Dispose()` (取消 listener 订阅)

### P4 RestartFromErrorRecovery

**Setup**:
- P1 cleanup 后 production sm state=Idle + chapter 1 loaded (P3 用 isolated local 不污染 production)
- 第 1 部分 — 故意进入 Error: fire OnRequestSceneChange(99) (production fixture 99 不存在) → state=Error
- Subscribe `OnSceneTransitionBegin/End` + `OnSceneLoadFailed` listener spy

**Action**:
```csharp
// Part A: enter Error state
GameEvent.Get<ISceneEvent>().OnRequestSceneChange(99);
await UniTask.DelayFrame(2);
// Part B: Error state silent drop test
Debug.Log("[S6-04][P4] expected Debug.LogWarning below: AC-10 OnRequestSceneChange dropped in Error state");
GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1); // AC-10 silent drop
await UniTask.DelayFrame(1);
int stateAfterErrorDropFire = (int)sm.CurrentState; // 仍 Error
// Part C: RecoverToIdle + re-fire
sm.RecoverToIdle();
int stateAfterRecover = (int)sm.CurrentState; // Idle
GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
await UniTask.WaitUntil(() => sm.CurrentState == GameLogic.SceneManagerState.Idle, timeout: 8000ms);
```

**Assert** (4 asserts):
- `state_after_fire_99 == Error` (Part A)
- `state_after_error_drop_fire == Error` (Part B — AC-10 silent drop 不改变状态)
- `state_after_recover == Idle` (Part C — RecoverToIdle 成功)
- `final_currentChapterId == 1` (Part C — fresh re-attempt success)

**Cleanup**: state Idle + chapter 1 loaded (供 P5 用)

### P5 RapidNewestWinsOverwrite

**Setup**:
- P4 cleanup 后 production sm state=Idle + chapter 1 loaded
- Subscribe `OnSceneTransitionBegin/End` listener spy

**Action**:
```csharp
// Fire 1: same target → AC-8 silent OnSceneReady (no transition)
GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
await UniTask.DelayFrame(1);
int onSceneReadyCountAfterFire1 = _onSceneReadyCount;
// Fire 2: diff target → start transition to chapter 2
GameEvent.Get<ISceneEvent>().OnRequestSceneChange(2);
await UniTask.DelayFrame(1);
// Fire 3: rapid during transition → newest-wins pending = 1
GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
int pendingAfterFire3 = sm.PendingTargetChapterIdForTest ?? -999;
await UniTask.WaitUntil(() => sm.CurrentState == GameLogic.SceneManagerState.Idle, timeout: 8000ms);
```

**Assert** (5 asserts):
- `onSceneReady_count_after_fire1 == 1` (AC-8 silent same-target path 触发 OnSceneReady)
- `pending_after_fire3 == 1` (newest-wins overwrite during transition)
- `final_state == Idle`
- `final_currentChapterId == 1` (newest wins drain → chapter 1 reload)
- `OnSceneTransitionBegin fired 2 times` (chapter 1→2 + chapter 2→1)

**Cleanup**: final state Idle + chapter 1 loaded

---

## R2 Assumptions Validated (7 R2 表 + R2.7 DEFERRED + R2.8 + R2.9 TBD Phase 2)

| R2 | Assumption | Verdict | Verify path |
|---|---|---|---|
| **R2.1** | SceneManager `SceneManagerState.Error` 是 6 状态之一 + `RecoverToIdle()` 显式恢复 method | ✅ FULLY | grep verify SceneManager.cs:22 enum + line 293-303 RecoverToIdle |
| **R2.2** | `TryResolveOrFail(id)` provider != null 且返 null → `OnSceneLoadFailed(id, "Chapter ID X not found in TbChapter.")` + state=Error | ✅ FULLY | grep verify SceneManager.cs:248-263 |
| **R2.3** | `LoadChapterSceneAsync` 2 次 retry (MaxLoadRetry=2) → exhaust 后 `OnSceneLoadFailed(id, lastError.Message)` + state=Error | ✅ FULLY | grep verify SceneManager.cs:74 const + line 476-528 retry loop + line 530-533 exhaust |
| **R2.4** | AC-9 newest-wins pending overwrite during transition (`_pendingTargetChapterId = newest target`) | ✅ FULLY | grep verify SceneManager.cs:350-352 + line 358-385 DrainPending |
| **R2.5** | AC-10 Error 状态 `OnRequestSceneChange` 静默丢弃 + warning | ✅ FULLY | grep verify SceneManager.cs:312-316 |
| **R2.6** | AC-8 Idle + same target + currentChapterId != NoChapterId → silent `OnSceneReady` (no transition) | ✅ FULLY | grep verify SceneManager.cs:329-333 + line 365-369 DrainPending 内同语义 |
| **R2.7** | IGameFlowEvent.OnReturnToMainMenu spec/contract | ❌ **0-production** — 没有 IGameFlowEvent.cs；ISceneEvent.OnRequestSceneChange 仅支持 1..5 (line 24)；MainMenuPanel.Quit = Application.Quit；S5-02 dp6 NEW 已记录 | ⚠️ **DEFERRED Sprint 7+ ADR-010 InputManager epic 或 Sprint 7+ ChapterSelect epic 一并** (epic boundary cleanup per S6-07 R2.8/S6-08 R2.7 [D] mixed strategy precedent) |
| **R2.8** | Spike 5 case 顺序 cleanup 防互污染 (chapter 1 baseline → P1 unknown → cleanup → P2 newest-wins → cleanup → ...) | ⚠️ TBD Phase 2 | spike state cleanup pattern per S6-08 P3 ClearAndClosePopupQueue precedent — P1 后 RecoverToIdle；P2 后 WaitUntil Idle；P3 用 isolated local 不污染；P4/P5 顺序 chain |
| **R2.9** | DevTestState `[main-menu]` mode 扩 `HasSpike(S6-04)` V3.0.1 dp8 candidate +1 = 4 distinct spike (S5-02 + S6-07 + S6-08 + S6-04) — 触发阈值 4 V3.1 trigger 评估 | ⚠️ TBD Phase 2 | DevTestState.cs:34 1-line amend；Sprint 6 retro 评估是否 promote V3.1 trigger pattern (per S6-08 NEW dp10 candidate partial class scoped scope 模式 一同评估) |

✅ R2.1~R2.6 FULLY (6/7 vendor 已 production 实证)；R2.7 ⚠️ DEFERRED Sprint 7+ epic boundary；R2.8 + R2.9 ⚠️ TBD Phase 2 实施时 verify。R2 verdict **DEFICIENCY-FLAGGED PASS** per ADR-029 V2.0 §V2-1 R2 path (类似 S6-08 R2.7 + S6-07 R2.6+R2.8 模式 — S6-04 R2.7 第 3 次实战 epic boundary cleanup [D] mixed strategy precedent)。

---

## V3.0.1 Watch List Hooks

| Type | Candidate | S6-04 trigger 评估 |
|---|---|---|
| Type-5 | dp7 NEW `protected virtual/override` UIBase lifecycle visibility modifier | 不触发 — 本 story 0 UIWindow override |
| Type-5 | dp9 NEW popup queue first enqueue immediate show vs priority sort distinction | 不触发 — 本 story 0 popup 操作 |
| Type-5 | **dp11 candidate** sprint backlog placeholder wording drift (S5-2b → S6-04 narrow scope amend 2 处 wording drift: mid-transition cancel → newest-wins pending; repeated rapid debounce → newest-wins overwrite) | ⚠️ 留观察 — 单次不触发 V3.1 trigger；Sprint 6 retro 评估 (本 story rewrite + sprint-status.yaml entry wording amend 同步) |
| Type-8 | dp1 UIWindow second show 行为 vendor 销毁后重建 | 不触发 — 本 story 0 UIWindow 操作 |
| Type-9 | dp1 closure absorbed V2.0 §V2-1.b R2 Interface Method Set Fan-out Check | ✅ 应用 — R2.6 ISceneEvent.OnRequestSceneChange listener verify (SceneManager 唯一 listener — Fan-out closure) + R2.1 SceneManagerState enum 6 状态 verify (实证 enum 全套覆盖) |
| dp8 candidate | DevTestState `[main-menu]` mode 复用 阈值阶进 | ⚠️ **TRIGGERED** — 本 story 加入 dp8 candidate +1 = 4 distinct spike (S5-02 + S6-07 + S6-08 + S6-04) 达阈值 4 → Sprint 6 retro **强制评估 promote** V3.1 trigger pattern (DevTestState central mode-dispatch refactor 候选 — 类似 GameApp.RegisterDevSpikes pattern) |
| dp10 candidate | UIModule partial class scoped scope 模式 (S6-08 InputBlocker + PopupQueue 同模式) | 不触发 — 本 story 0 UIModule operation；UIModule partial class count 仍 = 2 (距阈值 5 还差 3) |
| **NEW dp11 candidate** | Sprint backlog placeholder wording drift detection (S5-2b → S6-04 rewrite 揭露 vendor reality wording drift) — 启示：未来 Sprint backlog placeholder 写入时 R2 grep verify vendor source 优先级提升 | 单次启动 — Sprint 6 retro 评估 promote V3.1 trigger candidate |

---

## Phase 0 R2 Verify ✅ DONE + Phase 1 Readiness Gate ✅ READY

- ✅ R1 readiness gate — 0 forbidden listener pattern (本 story spike subscribe listeners + production code 0 change；listener spy 端是 verify spy 非 production code 不计 R1 违规)
- ✅ R2 readiness gate — DEFICIENCY-FLAGGED PASS:
  - R2.1~R2.6 ✅ FULLY (SceneManagerState.Error + RecoverToIdle + TryResolveOrFail + MaxLoadRetry=2 + newest-wins pending AC-9 + Error AC-10 silent drop + AC-8 same-target silent OnSceneReady — vendor source 实证)
  - R2.7 ⚠️ DEFERRED Sprint 7+ ADR-010 InputManager epic 或 Sprint 7+ ChapterSelect epic 边界 (IGameFlowEvent 0-production — 不阻 Phase 2)
  - R2.8 + R2.9 ⚠️ TBD Phase 2 实施时确认 (5 case state cleanup pattern + DevTestState `[main-menu]` mode dp8 candidate +1 = 4 distinct spike 触发阈值)
- ✅ R3 readiness gate — 5 R3 PlayMode probe case 完整 spec (P1 UnknownChapterTryResolveOrFail + P2 NewestWinsPendingDuringTransition + P3 AssetLoadFailRetryExhaust + P4 RestartFromErrorRecovery + P5 RapidNewestWinsOverwrite) + listener spy 模式 + isolated local SceneManager pattern + JSON evidence schema + 18+ asserts + ≤8s perf budget

✅ R2 deficiency flag **R2.7 DEFERRED + R2.8 + R2.9 TBD** 等 Phase 2 实施时 verify — per ADR-029 V2.0 §V2-1 R2 DEFICIENCY-FLAGGED PASS path (类似 S6-08 R2.7 DEFERRED + R2.8/R2.9 TBD 路径) ✅ READY for Phase 2 transition。

**Status**: Phase 0 R2 verify ✅ DONE + Phase 1 readiness gate ✅ READY → ready for Phase 2 production code 实施 (0 production code change — spike only)。

---

## Dependencies

### Story Dependencies (✅ all done)

- **S5-02 ✅** (Chapter 1 end-to-end happy path baseline + chapter 2 MVP placeholder fixture + DevTestState `[main-menu]` mode precedent + S5-02 dp6 NEW chapter 0 spec drift retrospective)
- **S5-1b ✅** (SceneManager boot pipeline 接入 + fixture ChapterDataProvider + spike P4/P5 isolated local SceneManager error path precedent + listener spy on `ISceneEvent.OnSceneLoadFailed`)
- **S5-1c ✅** (ADR-009 listener-path driver 接入 + F4 stub removal + `DriveTransitionAsync` exception catch 兜底 — 本 story P3 retry exhaust 路径走 BeginTransitionAsync 内部 LoadChapterSceneAsync fail + state=Error 是 listener-path driver 正确路径)
- **S6-07 ✅** (DevTestState `[main-menu]` mode 复用 precedent — 本 story 加入 dp8 candidate)
- **S6-08 ✅** (sender-only narrow scope [A] mode 复用 precedent + ADR-029 V2.0 §V2-1.b R2 deficiency-flagged PASS path 二次实战 + R2 表完整模板 + V3.0.1 Watch List Hooks 完整模板 — 本 story 第 3 次实战 R2 deficiency-flagged PASS path)

### Out-of-Scope Story Dependencies (Sprint 7+ Production stage / ADR-010 / ChapterSelect Epic)

- **IGameFlowEvent.OnReturnToMainMenu spec/contract** — Sprint 7+ ChapterSelect epic 或 ADR-010 InputManager epic 一并新增；MainMenuPanel return path 留 Sprint 7+ (本 story R2.7 DEFERRED epic boundary cleanup)
- **ISceneEvent.OnRequestSceneChange(0)** spec 扩展 — chapter 0 = main menu return — 留 Sprint 7+ spec evolution (本 story 不修改 ISceneEvent.cs)
- **真实 chapter 2-5 art asset** — Sprint 7+ Production stage polish phase (本 story 仅复用 chapter 1 scene asset for chapter 2 placeholder per S5-02 dp6 NEW)

### Framework Dependencies (✅ all verified Phase 1)

- `SceneManager` (vendor) — `SceneManagerState.Error` + `RecoverToIdle()` + `TryResolveOrFail()` private + `LoadChapterSceneAsync` 2 retry + `BeginTransitionAsync` 11-step + `DriveTransitionAsync` exception catch + AC-5/-8/-9/-10/-11/-12 ✅ all production
- `ISceneEvent` (vendor) — 9 method 已冻结 — `OnRequestSceneChange` + `OnSceneLoadFailed` + `OnSceneReady` + `OnSceneTransitionBegin/End` + 4 其余 sender ✅ all production
- `ChapterData` (vendor) — ctor `(int id, string sceneId, string bgmAsset, float emotionalWeight, string overlayColor)` all readonly ✅ verified
- `GameModule.Scene` (TEngine vendor) — `LoadSceneAsync` + `UnloadAsync` + `ActivateScene` + `CreateResourceDownloader` ✅ all verified S3-01..03
- `DevBootstrap.HasSpike(id)` ✅ verified S5-02 + S6-07
- `IDevSpike` interface + `DevBootstrap.Register()` ✅ verified
- `TEngine.Log.Info/Warning/Error` ✅
- `Cysharp.Threading.Tasks.UniTask.DelayFrame/WaitUntil` ✅

---

## Out of Scope

- **Production code change** — 本 story 0 production code change (R3 PlayMode probe verify only)；本 story 通过 spike + Editor 验 vendor reality 符合 spec 即收
- **真实 chapter 2-5 art asset** — Sprint 7+ Production stage polish
- **IGameFlowEvent epic + MainMenuPanel return path** — Sprint 7+ ChapterSelect epic 或 ADR-010 InputManager epic 一并
- **ISceneEvent.OnRequestSceneChange(0)** spec 扩展 — Sprint 7+ spec evolution
- **真 cancel API (vendor 无 cancel — 只 newest-wins pending)** — vendor 设计选择不引入 cancel；本 story 不 propose 新增 — S5-2b placeholder 描述中的 "mid-transition cancel" wording drift 通过本 rewrite 纠正为 "newest-wins pending"
- **真 debounce (vendor 无 time-based debounce — 只 newest-wins overwrite)** — vendor 设计选择不引入 time-based debounce；本 story 不 propose 新增 — S5-2b placeholder 描述中的 "repeated rapid debounce" wording drift 通过本 rewrite 纠正为 "newest-wins overwrite"

---

## History

| Date | Action | Notes |
|---|---|---|
| 2026-05-11 | S5-2b placeholder 派生 Sprint 5 [B] split_2 决策 | S5-02 happy path 2 SP + S5-2b error/restart 1 SP Sprint 6 backlog placeholder；本 sprint 不写 file 仅 sprint-status.yaml entry placeholder |
| 2026-05-13 morning | S6-04 Sprint 6 启动 placeholder upgrade Must Have | sprint-status.yaml status: backlog → ready-for-dev；name: "chapter 1 error/restart path (S5-2b Sprint 5 placeholder by design)" |
| 2026-05-13 afternoon (Session 30 continue) | **Phase 0 R2 vendor reality verify ✅ DONE + Phase 1 readiness gate ✅ READY** | 7 R2 finding 实证：(R2.1) SceneManagerState.Error + RecoverToIdle production；(R2.2) TryResolveOrFail unknown chapter → OnSceneLoadFailed + state=Error production；(R2.3) LoadChapterSceneAsync MaxLoadRetry=2 retry exhaust path production；(R2.4) AC-9 newest-wins pending overwrite during transition production；(R2.5) AC-10 Error 状态 silent drop production；(R2.6) AC-8 Idle + same target silent OnSceneReady production；(R2.7) ❌ IGameFlowEvent.OnReturnToMainMenu 0-production DEFERRED Sprint 7+。Narrow scope [A] 决策 ~1 SP 0 production code change：5 R3 PlayMode probe case 完整 spec — P1 UnknownChapterTryResolveOrFail + P2 NewestWinsPendingDuringTransition + P3 AssetLoadFailRetryExhaust + P4 RestartFromErrorRecovery + P5 RapidNewestWinsOverwrite。S5-2b placeholder 描述 2 处 wording drift 揭露并 amend：(1) "mid-transition cancel" → "newest-wins pending" per AC-9；(2) "repeated rapid debounce" → "newest-wins overwrite" per AC-9。V3.0.1 Watch List Hooks：dp8 candidate +1 = 4 distinct spike (S5-02 + S6-07 + S6-08 + S6-04) **触发阈值 4 — Sprint 6 retro 强制评估 promote V3.1 trigger**；**NEW dp11 candidate** sprint backlog placeholder wording drift 留观察。Phase 1 readiness gate R1+R2+R3 verdict ✅ **DEFICIENCY-FLAGGED PASS** ready for Phase 2 transition (R2.7 DEFERRED + R2.8/R2.9 TBD Phase 2)。Files (4): 本 story-003-error-restart-path.md NEW (vs-chapter-1 epic dir, story-001/-001b/-001c/-002 sibling) + EPIC.md amend (story-003 row Status: Sprint 6 backlog → Phase 1 ready) + sprint-status.yaml S6-04 entry status: ready-for-dev → phase-1-readiness-gate-done + active.md 同步。Phase 0+1 投入 ~30-45 min (Session 30 continue — 本 session 已 ~4-5 hr 经 user [C] override CLAUDE.md ≤5 hr hard rule)。 |
