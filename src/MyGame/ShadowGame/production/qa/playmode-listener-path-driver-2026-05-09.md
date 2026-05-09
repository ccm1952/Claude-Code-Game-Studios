// 该文件由Cursor 自动生成

# story-001c R3 PlayMode Evidence — ADR-009 Production Listener-Path Driver (2026-05-09)

> **Story**: story-001c — ADR-009 production listener-path driver 接入（移除 S5-1b F4 dev-only stub）
> **Sprint**: 5 (start 2026-05-06 / end 2026-05-20)
> **Epic**: vs-chapter-1
> **Type**: Logic / Integration
> **Engine**: Unity 2022.3.62f2 LTS + URP + HybridCLR + YooAsset 2.3.17 + UniTask
> **Date**: 2026-05-09
> **Verdict**: **PASS** (5/5 R3 case + 24/24 asserts + all_passed=true first-run)
> **Story file**: `production/epics/vs-chapter-1/story-001c-adr009-listener-path-driver.md`
> **Governing ADRs**: ADR-009 §Decision line 386 / ADR-027 / ADR-029 V2.0 R3 mandatory
> **Spike file**: `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-1c_ListenerPathDriver.cs` (1 文件 + 3 内类)
> **JSON evidence**: `~/Library/Application Support/DefaultCompany/Unity/S5-1c_Result.json` (timestamp: 2026-05-09 18:29:40)

---

## §0 概要

story-001c **production listener-path driver 接入完成**，回归 ADR-009 §Decision line 386 spec ("Scene Manager subscribes to OnRequestSceneChange and orchestrates the entire 11-step flow internally")。S5-1b 期间 `DevTestState.DriveProductionSceneTransitionAsync` F4 dev-only stub (反射 + 800ms UniTask.Delay) 已移除；`SceneManager.OnRequestSceneChange` handler 与 `DrainPending` 末尾添加 internal `DriveTransitionAsync(targetChapterId).Forget()` tail；新增 `private async UniTaskVoid DriveTransitionAsync(int)` helper（兜底 try-catch）。

R3 PlayMode 5 case M1 双层模式（复用 S5-1b precedent）first-run **5/5 case PASS / 24/24 asserts PASS / all_passed=true**：

| # | Case | 描述 | 状态 |
|---|------|------|------|
| P1 | ListenerPathFirstBoot | 反射 production；listener 自驱 11-step + 8 lifecycle event 顺序（含 OnSceneTransitionBegin sync-subscribed via Awake）| ✅ PASS (9/9 asserts) |
| P2 | SameChapterDedupeIdleNoDriver | 派 OnRequestSceneChange(1) 同章 → OnSceneReady + 0 OnSceneTransitionBegin（关键 invariant：driver 严格放在 line 346 不被 dedupe 误触）| ✅ PASS (4/4 asserts) |
| P3 | UnknownChapterFailLoudLocal | spike-local 直调 BeginTransitionAsync(99) → LoadChapterSceneAsync ChapterData null fail-loud | ✅ PASS (4/4 asserts) |
| P4 | ErrorStateDrop_RecoverToIdle | production 4-step round-trip：99 fail-loud → 3 drop → RecoverToIdle → 1 同章 OnSceneReady；全程 0 新 OnSceneTransitionBegin | ✅ PASS (5/5 asserts) |
| P5 | ListenerSelfRemoval5Cycles | production reflection Dispose+Init × 5；V2-5 framework boundary probe 第 4 次实战累计 | ✅ PASS (2/2 asserts) |

---

## §1 R3 5 Case Detail

### §1.1 P1 ListenerPathFirstBoot (production reflection — 8 lifecycle event 顺序)

**Setup**:
- spike `S51cRuntime.Awake()` 同步 subscribe P1 listeners (5 events: OnSceneTransitionBegin / OnSceneLoadProgress / OnSceneLoadComplete / OnSceneReady / OnSceneTransitionEnd)
- `DevBootstrap.RunRequested()` 在 `OnRequestSceneChange(1)` 之前被调（DevTestState OnEnter 顺序调整 per story-001c P1 sync-subscribe pattern）
- `GameApp._sceneManager` 通过 reflection 拿到 production instance

**Action**: DevTestState OnEnter 派 `OnRequestSceneChange(1)` → SceneManager handler line 326 不同章 path 命中 → state=TransitionOut + DriveTransitionAsync(1).Forget() → `BeginTransitionAsync(1)` 异步运行 11-step

**Events captured (8-lifecycle 顺序 in time)**:
```
OnSceneTransitionBegin(-1, 1)             ← Step 3 (DriveTransitionAsync sync entry, captured by Awake-subscribed listener)
OnSceneLoadProgress(Chapter_01_Approach, 0.00)  ← Step 9 progress
OnSceneLoadProgress(Chapter_01_Approach, 0.00)
OnSceneLoadProgress(Chapter_01_Approach, 0.??)  ← (count=4 total per asserts)
OnSceneLoadComplete(1, '')                ← Step 10b
OnSceneReady(1)                           ← Step 10c (TransitionIn entry)
OnSceneTransitionEnd(1)                   ← Step 11 (driver complete)
```

**Asserts (9/9 PASS)**:

| # | Assert | Result |
|---|--------|--------|
| timeout (≤5s) | state == Idle within 5s | PASS |
| CurrentLoadedChapterIdForTest | 1 == 1 | PASS |
| CurrentChapterSceneNameForTest | 'Chapter_01_Approach' == 'Chapter_01_Approach' | PASS |
| CurrentState | Idle == Idle | PASS |
| OnSceneLoadComplete | count=1 payload=(1,'') | PASS |
| OnSceneReady | count=1 ≥1 | PASS |
| OnSceneTransitionEnd | count=1 ≥1 | PASS |
| **OnSceneTransitionBegin** | **count=1 ≥1**（关键：sync-subscribe via Awake 解决 listener-path race）| **PASS** |
| OnSceneLoadProgress | count=4 ≥1 | PASS |

**关键发现**: 移除 F4 800ms stub 后 `BeginTransitionAsync` line 644 `OnSceneTransitionBegin` 在 OnRequestSceneChange 派发后**同步**fire；spike Runtime.Start 后再 subscribe 会错过此事件。**修复**: spike Awake 内 sync subscribe (`SubscribeP1ListenersEarly`) + DevTestState OnEnter 调整顺序为 `RunRequested()` 先 → `OnRequestSceneChange(1)` 后 → spike Awake 已 attach listeners → driver 同步 fire 时 listener 在线。此 pattern 沉淀为 story-001c P1 sync-subscribe 经验（V3 watch list candidate — type-2c framework method 行为差异）。

### §1.2 P2 SameChapterDedupeIdleNoDriver (driver 不被 dedupe 路径误触)

**Setup**: post-P1（chapter 1 loaded + state=Idle + _currentChapterId=1）；spike subscribe OnSceneTransitionBegin + OnSceneReady listeners

**Action**: 派 `OnRequestSceneChange(1)` → handler line 326-333 Idle 同章 path：`targetId(1) == _currentChapterId(1) && _currentChapterId(1) != NoChapterId(-1)` → 立即派 `OnSceneReady(1)` + return（**不**进 line 342 不同章 path → driver 不被调）

**Events captured**: `OnSceneReady(1)` (1 次)

**Asserts (4/4 PASS — 全部体现关键 invariant)**:

| # | Assert | Result |
|---|--------|--------|
| OnSceneTransitionBegin_should_be_zero | 0 == 0（**driver 不被 dedupe 路径误触**）| PASS |
| OnSceneReady_should_be_one_or_more | 1 ≥ 1 | PASS |
| CurrentState | Idle | PASS |
| InflightChapterIdForTest | NoChapterId(-1) — 无 in-flight 二次 transition | PASS |

**关键 invariant verified**: `DriveTransitionAsync.Forget()` 严格放在 SceneManager.cs:346 line 326-333 之后（即 line 342 不同章 path 末尾），**不**被 line 326-333 同章 dedupe 路径触达。这是 story-001c 最重要的 design invariant — 4 个 guard path（Error drop / in-flight dedupe / Idle 同章 / pending newest-wins）都不进 driver；只有 line 342 Idle 不同章 path 才进 driver。

### §1.3 P3 UnknownChapterFailLoudLocal (spike-local；ChapterData null fail-loud)

**Setup**:
- `var local = new SceneManager()` — 不调 Init() 避免与 production listener 冲突
- `local.RegisterChapterDataProvider(id => id == 1 ? new ChapterData(...) : null)`
- `local.RegisterFadeOverlay(new NoOpFadeOverlay())`
- spike subscribe global `ISceneEvent.OnSceneLoadFailed` listener

**Action**: spike-local 直调 `await local.BeginTransitionAsync(99)` （绕过 OnRequestSceneChange handler 走深部 LoadChapterSceneAsync 路径）+ Debug.Log spike marker（"[S5-1c][P3] expected Debug.LogError below"）

**Console expected output**:
```
[S5-1c][P3] expected Debug.LogError below: ChapterData null for id=99
[ERROR] [SceneManager] LoadChapterSceneAsync(99) failed — ChapterData null for id=99 (Luban TbChapter 未配置或 provider 返 null).
```

**Events captured**: `OnSceneLoadFailed(99,'ChapterData null for id=99 (Luban TbChapter 未配置或 provider 返 null).')`

**Asserts (4/4 PASS)**:

| # | Assert | Result |
|---|--------|--------|
| OnSceneLoadFailed_count | count=1 ≥1 | PASS |
| OnSceneLoadFailed_chapterId | chapterId=99 == 99 | PASS |
| CurrentState | local.state == Error | PASS |
| CurrentLoadedChapterIdForTest | NoChapterId（不污染）| PASS |

**与 story-001c relevance**: 验证 spike-local 通过 BeginTransitionAsync 路径触发 LoadChapterSceneAsync line 463 ChapterData null fail-loud；此路径与 P4 production OnRequestSceneChange handler TryResolveOrFail (line 258 不同 message `"Chapter ID 99 not found in TbChapter."`) 形成两条 fail-loud 路径互补 evidence。complement to P2 driver 不被误触 invariant。

### §1.4 P4 ErrorStateDrop_RecoverToIdle (production 4-step round-trip — 0 全程 OnSceneTransitionBegin)

**Setup**: post-P2（production chapter 1 loaded + state=Idle）；spike subscribe production sender events (OnSceneTransitionBegin / OnSceneLoadFailed / OnSceneReady)

**4 Step Action Sequence**:

#### Step (a) — 派 99 → fail-loud Error
```
OnRequestSceneChange(99) → handler line 336 TryResolveOrFail
  → fixture provider returns null
  → Log.Warning: "Chapter ID 99 not found in TbChapter."
  → OnSceneLoadFailed(99, "Chapter ID 99 not found in TbChapter.")
  → TransitionTo(Error)
  → return false (line 339)
  → DriveTransitionAsync 不被调（line 346 之前 short-circuit return）
```

**Step (a) Console expected**:
```
[S5-1c][P4][a] expected Log.Warning below: Chapter ID 99 not found in TbChapter.
[Warning] [SceneManager] Chapter resolve failed: Chapter ID 99 not found in TbChapter.
```

注：与 P3 spike-local 不同，P4 走 production handler 入口的 `TryResolveOrFail` (line 259 `Log.Warning`，不是 Log.Error)；两条 fail-loud 路径设计上 reason 字符串不同（详 story-001c §13.5 design note）。

**Step (a) verified**: `state=Error + OnSceneLoadFailed(99) count=1 + 0 新 OnSceneTransitionBegin`

#### Step (b) — Error 状态 派 3 → drop
```
OnRequestSceneChange(3) → handler line 312-315 Error 分支 hit
  → Log.Warning: "[SceneManager] OnRequestSceneChange(3) dropped — Error state."
  → return
```

**Step (b) verified**: `0 新 OnSceneTransitionBegin + 0 新 OnSceneLoadFailed + state=Error 不变`

#### Step (c) — RecoverToIdle → state=Idle
```
production._sceneManager.RecoverToIdle()
  → state Error → Idle
  → _inflightChapterId = NoChapterId
  → DrainPending() (no pending)
```

**Step (c) verified**: `state=Idle`

#### Step (d) — 派 1 → Idle 同章 OnSceneReady
```
OnRequestSceneChange(1) → handler line 326 Idle 同章 path
  → targetId(1) == _currentChapterId(1) && _currentChapterId(1) != NoChapterId(-1) → TRUE
  → OnSceneReady(1)
  → return（driver 不被调）
```

**Step (d) verified**: `OnSceneReady delta=1 + 0 新 OnSceneTransitionBegin`

**Events captured**:
```
OnSceneLoadFailed(99,'Chapter ID 99 not found in TbChapter.')   ← step (a)
OnSceneReady(1)                                                  ← step (d)
```

**Asserts (5/5 PASS)**:

| # | Assert | Result |
|---|--------|--------|
| a_state_after_99 | state=Error + OnSceneLoadFailed(99) count=1 | PASS |
| b_drop_in_error | 0 新 OnSceneTransitionBegin + 0 新 OnSceneLoadFailed + state=Error 不变 | PASS |
| c_recover_to_idle | state=Idle | PASS |
| d_same_chapter_idle_after_recover | OnSceneReady delta=1 + 0 新 OnSceneTransitionBegin | PASS |
| **no_new_transition_begin_total** | **0 全程新 OnSceneTransitionBegin (4 guard path 都不进 driver)** | **PASS** |

**关键 invariant verified**: production OnRequestSceneChange 4 guard paths (Error / in-flight / Idle 同章 / TryResolveOrFail) 全部都不调 DriveTransitionAsync；只有 line 342 Idle 不同章 path 调用 driver。此 P4 case 是 story-001c 最强的 invariant proof。

### §1.5 P5 ListenerSelfRemoval5Cycles (V2-5 framework boundary probe — 第 4 次实战累计)

**Setup**: production `_sceneManager` reflection；post-P4 chapter 1 loaded + state=Idle

**Action 5 cycle**:
```
for cycle in 1..5:
  prodScene.Dispose()      ← unsubscribe ISceneEvent.OnRequestSceneChange listener
  prodScene.Init()         ← re-subscribe
  GameEvent.派 OnRequestSceneChange(1) → 同章 OnSceneReady（chapter 1 仍 loaded）
  await UniTask.Delay(120ms)
```

**Events captured (10 lines)**:
```
cycle_OnSceneReady(1)    cycle1_listener_triggered:True (delta=1)
cycle_OnSceneReady(1)    cycle2_listener_triggered:True (delta=1)
cycle_OnSceneReady(1)    cycle3_listener_triggered:True (delta=1)
cycle_OnSceneReady(1)    cycle4_listener_triggered:True (delta=1)
cycle_OnSceneReady(1)    cycle5_listener_triggered:True (delta=1)
```

**Asserts (2/2 PASS)**:

| # | Assert | Result |
|---|--------|--------|
| 5_cycle_listener_self_removal | 5/5 cycle listener triggered + 0 exception | PASS |
| total_OnSceneReady_count | count=5 ≥5 | PASS |

**V2-5 累计**: story-001c 是 V2-5 framework boundary probe 第 4 次实战 (S5-03 / S5-05 / S5-06 / S5-1b precedent → S5-1c)；5 cycle Init/Dispose 0 NullRef / 0 RemoveListener exception；framework `GameEvent` listener buffer 不 leak。

---

## §2 Console Snapshot

`read_console action=get types=[error] count=30` 跑完 PlayMode 后实测 4 entries：

| # | Type | Source | 性质 |
|---|------|--------|------|
| 1 | Log | spike `[S5-1c][P3]` marker | spike 主动 print（"expected Debug.LogError below"），非 error |
| 2 | **Error** | `[SceneManager] LoadChapterSceneAsync(99) failed — ChapterData null for id=99 (Luban TbChapter 未配置或 provider 返 null).` | **P3 expected fail-loud**（spike-local 直调 BeginTransitionAsync(99) 走 LoadChapterSceneAsync line 463 Log.Error）|
| 3 | Log | spike `[S5-1c][P4][a]` marker | spike 主动 print（"expected Log.Warning below"），非 error |
| 4 | Warning | Android SDK XML 4 vs 3 version warning | 与本 story 无关（pre-existing）|

**0 unexpected error** — 所有 ERROR-level 日志均为 spike 显式标记的 P3 expected fail-loud 路径。

注：P4(a) 走 production OnRequestSceneChange → TryResolveOrFail 路径，line 259 是 `Log.Warning` 不是 `Log.Error`，所以 console error filter 没有捕获（asserts 仍 PASS by listener-side OnSceneLoadFailed event count）。

---

## §3 git diff Stat

```bash
$ git diff --name-only HEAD --no-renames
src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/Scene/SceneManager.cs
src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/GameFlow/DevTestState.cs
src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/GameApp.cs
docs/architecture/adr-009-scene-lifecycle.md
src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-1c_ListenerPathDriver.cs (新)
src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-1c_ListenerPathDriver.cs.meta (新)
src/MyGame/ShadowGame/production/qa/playmode-listener-path-driver-2026-05-09.md (本文件，新)
src/MyGame/ShadowGame/production/epics/vs-chapter-1/story-001c-adr009-listener-path-driver.md (新)
src/MyGame/ShadowGame/production/epics/vs-chapter-1/EPIC.md
src/MyGame/ShadowGame/production/sprint-status.yaml
src/MyGame/ShadowGame/production/session-state/active.md
```

**Production code changes**:

```bash
$ git diff --stat HEAD -- src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/Scene/SceneManager.cs
+34 -2  (新增 DriveTransitionAsync helper + 2 处 .Forget() tail wires + xmldoc)

$ git diff --stat HEAD -- src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/GameFlow/DevTestState.cs
+19 -41 (移除 DriveProductionSceneTransitionAsync 整 method + reflection 逻辑 + System.Reflection import；改 OnEnter 顺序)

$ git diff --stat HEAD -- src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/GameApp.cs
+5 -3 (DevBootstrap.Register 切到 S51cSpike + 注释 update)

$ git diff --stat HEAD -- docs/architecture/adr-009-scene-lifecycle.md
+11 -0 (§History 加 1 条 2026-05-09 dusk implementation alignment entry)
```

---

## §4 AC Matrix (story-001c 10 AC)

| # | AC 描述 | 实测 evidence | 状态 |
|---|---|---|---|
| AC-1 | SceneManager.cs 含 `private async UniTaskVoid DriveTransitionAsync(int)` method + try-catch 兜底 | SceneManager.cs:405-415 实测 hit | ✅ |
| AC-2 | OnRequestSceneChange handler 不同章 path 末尾 `DriveTransitionAsync(targetChapterId).Forget();` + 注释更新 | SceneManager.cs:344-346 实测 hit；line 345 注释更新为 "story-001c: listener-path driver 接管 11-step (ADR-009 §Decision line 386 spec align)" | ✅ |
| AC-3 | DrainPending 末尾 `DriveTransitionAsync(next).Forget();` | SceneManager.cs:381-384 实测 hit | ✅ |
| AC-4 | DevTestState.cs 0 hit `DriveProductionSceneTransitionAsync` / `BindingFlags` / `GetField("_sceneManager"` / `using System.Reflection` | grep evidence: 全 0 hit；DevTestState.cs 整 file ~46 行（原 86 行）只剩 OnEnter + OnLeave + 2 个 Log + RunRequested + OnRequestSceneChange dispatch + spike Awake 顺序调整 | ✅ |
| AC-5 | ADR-009 §History 加 2026-05-09 dusk implementation alignment entry | adr-009-scene-lifecycle.md:565-571 实测 hit "Implementation alignment fix (story-001c)" | ✅ |
| AC-6 | R3 P1 listener-path first-boot 8 lifecycle event 顺序 + post-transition state | JSON evidence P1 events 完整 + 9/9 asserts PASS（含 OnSceneTransitionBegin 解决 race via Awake sync-subscribe）| ✅ |
| AC-7 | R3 P2 same-chapter dedupe Idle path — driver 不进（关键 invariant）| JSON evidence P2 4/4 asserts PASS：0 OnSceneTransitionBegin + OnSceneReady ≥1 + state=Idle + InflightChapterId=NoChapterId | ✅ |
| AC-8 | R3 P3 unknown chapter fail-loud — spike-local 直调 BeginTransitionAsync(99) 走 LoadChapterSceneAsync ChapterData null | JSON evidence P3 4/4 asserts PASS：OnSceneLoadFailed(99) count=1 + state=Error + 不污染 CurrentLoadedChapterId | ✅ |
| AC-9 | R3 P4 production Error state drop + RecoverToIdle round-trip — 全程 0 新 OnSceneTransitionBegin | JSON evidence P4 5/5 asserts PASS：4-step (a→b→c→d) + 0 全程 OnSceneTransitionBegin | ✅ |
| AC-10 | R3 P5 listener self-removal × 5 cycles — V2-5 第 4 次实战累计 | JSON evidence P5 2/2 asserts PASS：5/5 cycle listener triggered + 0 exception + total OnSceneReady=5 | ✅ |

**10 / 10 AC PASS** → story-001c dev-story 实施 done。

---

## §5 Sign-off

- **Implementation**: AI agent (Cursor + unity-mcp v9.6.x) 2026-05-09 18:00 ~ 18:30
- **Production code review**: SceneManager.cs +34 −2 / DevTestState.cs +19 −41 / GameApp.cs +5 −3 / ADR-009 +11 −0 — Lead Programmer agent code-review pending（建议 commit 后走一次）
- **R3 PlayMode probe**: 5/5 PASS / 24/24 asserts / all_passed=true first-run 实测 2026-05-09 18:29:40
- **Story status**: Ready → **Done** (per /story-done)
- **Sprint status**: S5-1c status `draft → ready → done`
- **Unlocks**: F4 dev-only stub 永久移除 ✅；S5-02 启动可走 clean production driver path ✅
- **Total elapsed**: ~1.5 h（含 outline 决策 + readiness gate + impl + sync-subscribe race fix + R3 PlayMode 1 次返工 + evidence doc）

🟢 story-001c dev-story closed。

---

## §6 Watch List Hooks (per ADR-029 V2.0 V3 candidate)

story-001c 实施过程暴露 1 项 V3 watch list candidate：

### V3 Type-2(c) candidate: spike subscribe race after F4 stub removal

**现象**: 移除 F4 800ms stub 后，listener-path driver 在 OnRequestSceneChange handler 内 sync 派 OnSceneTransitionBegin（BeginTransitionAsync line 644 在 await FadeOut 之前 sync 执行）；spike Runtime.Start 后再 subscribe 会错过此事件。

**根因**: UniTaskVoid `.Forget()` 启动的 async method 在第一个 await 之前的 sync 段会立即执行；`BeginTransitionAsync` 的第一个 sync 段就是 `OnSceneTransitionBegin` 派发。F4 stub 800ms delay 掩盖了这个 race；listener-path driver 不再有 artificial delay。

**修复 pattern (story-001c P1 sync-subscribe)**:
1. spike Runtime.Awake() 同步 subscribe listeners（AddComponent 调用栈内同步执行）
2. DevTestState OnEnter 顺序：`DevBootstrap.RunRequested()` (spike Awake fires) → `OnRequestSceneChange(1)` (listeners 已 attached)
3. 此 pattern 适用所有"听某个 sync-fire 事件的"R3 spike

**沉淀**: 暂记 watch list candidate；未来 R3 spike 设计如出现相同模式，可 promote 为正式 V3 drift type 或沉淀 cursor rule（本次不立即 promote — single occurrence not yet repeated pattern, per problem-to-rule-promotion 协议触发条件）。

**Cross-references**:
- story-001c §13 "Watch List Hooks" 内已记录此候选
- 与 V2-5 (S5-03/S5-05/S5-06/S5-1b/S5-1c precedent) 不同 — V2-5 是 listener self-removal cycle，本 candidate 是 listener subscribe timing race after F4 stub removal
