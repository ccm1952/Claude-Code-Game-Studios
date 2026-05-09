// 该文件由Cursor 自动生成

# Story 001c: ADR-009 production listener-path driver 接入（移除 S5-1b F4 dev-only stub）

> **Epic**: VS Chapter 1
> **Status**: **Done** *(2026-05-09 — 5/5 R3 PASS + 24/24 asserts + 10/10 AC + 1 sync-subscribe race fix iteration)*
> **Layer**: Vertical Slice (VS) — production code spec ↔ impl alignment fix
> **Type**: Logic / Integration
> **SP**: 2
> **Manifest Version**: 2026-05-09

---

## Context

**Trigger**:

S5-1b dev-story (DONE 2026-05-09) PlayMode 实跑暴露 ADR-009 spec ↔ implementation gap：

- **ADR-009 §Decision line 386 spec**：「Scene Manager subscribes to `IChapterStateEvent.OnRequestSceneChange` ... and **orchestrates the entire 11-step flow internally**」
- **SceneManager.cs:341-347 实测**：handler 仅 `_currentChapterId = targetChapterId; TransitionTo(SceneManagerState.TransitionOut); return;` — line 345 inline comment `// Story 002 接管后续 11 步流程` placeholder 标识 driver 缺失，但虚指的"Story 002"实际上**就应该是 SceneManager handler 自身**（spec 字面要求 internal orchestration）
- S5-1b 期间用 `DevTestState.DriveProductionSceneTransitionAsync` (dev-only F4 stub via reflection + UniTask.Delay 800ms) 临时模拟 production driver — 已显式标 TODO

**Goal**: 在 `SceneManager.OnRequestSceneChange` handler 与 `DrainPending` 末尾加 internal driver tail (`DriveTransitionAsync(targetChapterId).Forget()`)，回归 ADR-009 §Decision spec；移除 `DevTestState` F4 stub；S5-02 即可走 clean production driver path。

**GDD**:

- `design/gdd/scene-management.md` — 11-step transition flow / OnRequestSceneChange listener 唯一入口

**Requirement TR-IDs** *(复用 S5-1b 三 TR + 1 deficiency-flagged，per ADR-029 V2.0 deficiency-flag 协议)*:

- `TR-scene-001` — Additive scene 架构（spec align 后路径闭环）
- `TR-scene-005` — 11-step transition flow（driver 入口闭环）
- `TR-scene-013` — Startup flow Boot → TEngine → HybridCLR → YooAsset
- `TR-scene-listener-driver` ⚠️ **DEFICIENCY-FLAGGED 2026-05-09** — 该 TR 仅出现于本 story；按 ADR-029 V2.0 deficiency-flag 协议显式标记等待下次 `/architecture-review` 注册（不阻塞 dev-story）。语义：production `OnRequestSceneChange` handler 与 `DrainPending` 都内联调用 internal driver，不依赖外部 GameFlow / GameApp 层显式驱动

**ADR Governing Implementation**:

- **ADR-009 (Accepted)** §Decision line 386 — **本 story 关键 anchor**：SceneManager subscribes to OnRequestSceneChange and orchestrates 11-step flow internally
- **ADR-029 V2.0 (Accepted) R3 mandatory** — Logic / Integration story 必须 PlayMode framework boundary probe
- **ADR-027 (Accepted)** Event Layer — listener 自挂自摘 §5（Init/Dispose subscribe 路径已 by S5-1b verified）

**Engine Notes** *(R2 grep verified — by S3-01..03 + S5-1b PlayMode CORE PASSED)*:

- `SceneManager.OnRequestSceneChange(int)` private handler — line 309-351 现有 5 个 guard（Error drop / in-flight dedupe / Idle 同章 OnSceneReady / TryResolveOrFail / Non-Idle pending newest-wins）保持不变
- `SceneManager.DrainPending()` private — line 357-382 在 `OnSceneTransitionEnd` 后被 `BeginTransitionAsync` 调用 (line 672)；同样的 transition tail 路径
- `SceneManager.BeginTransitionAsync(int)` public — line 638，11-step driver 已 verified by S3-01..03 spike + S5-1b F4 driver
- `UniTaskVoid` + `.Forget()` 扩展 — `Cysharp.Threading.Tasks` 命名空间已 by SceneManager.cs:3 import；可直接用
- `DevTestState.cs` — 当前持有 `DriveProductionSceneTransitionAsync(int, int)` UniTaskVoid + reflection 拿 `GameApp._sceneManager` 调 `BeginTransitionAsync`；本 story 全部删除

**Performance**: N/A R3 mandatory；listener-path 仅多一次 `Task.Forget()` allocation，与 BeginTransitionAsync 自身的 11-step await chain 量级相比可忽略。

**Control Manifest Rules (this layer)**:

- **Required**: `SceneManager.OnRequestSceneChange` handler 不同章路径（line 344 `TransitionTo(SceneManagerState.TransitionOut)` 之后）必须立即调 `DriveTransitionAsync(_inflightChapterId).Forget();`；driver 调用必须在状态 set 之后保证 11-step 启动时状态正确
- **Required**: `SceneManager.DrainPending` 不同章路径（line 381 `TransitionTo(SceneManagerState.TransitionOut)` 之后）同样调 `DriveTransitionAsync(next).Forget();`
- **Required**: 新增 `private async UniTaskVoid DriveTransitionAsync(int targetChapterId)` helper；await `BeginTransitionAsync(targetChapterId)`；try-catch 仅作兜底（`BeginTransitionAsync` 内部已 fail-loud 协议 → state=Error + OnSceneLoadFailed）
- **Required**: `DevTestState.cs` 0 hit `DriveProductionSceneTransitionAsync` / `Reflection` / `GetField("_sceneManager"`；OnEnter 内仅保留 `OnRequestSceneChange(1)` 派发即可
- **Required**: `SceneManager.cs` line 345 inline comment `// Story 002 接管后续 11 步流程` 替换为 `// listener-path driver 接管 — internal DriveTransitionAsync (story-001c)；与 ADR-009 §Decision line 386 spec align`
- **Forbidden**: 不得改 `OnRequestSceneChange` 现有 5 个 guard（Error drop / in-flight dedupe / Idle 同章 / TryResolveOrFail / Non-Idle pending）；driver 仅作为 transition tail 追加
- **Forbidden**: 不得改 `BeginTransitionAsync` 主体（已 by S3-01..03 + S5-1b verified）
- **Forbidden**: 不得在本 story 实施 GameFlow `SceneTransitionState` FSM state（方案 B 评估 rejected per outline 决策 [A]）
- **Forbidden**: 不得在 GameApp.cs 加 listener handler（方案 C rejected — 协议位置不自然 per ADR-009 §Decision line 386）

---

## Acceptance Criteria

*Logic / Integration type — production code 修改 + R3 PlayMode probe MANDATORY (ADR-029 V2.0)*

- [ ] **AC-1 (driver helper)**: `Assets/GameScripts/HotFix/GameLogic/Scene/SceneManager.cs` 含 `private async UniTaskVoid DriveTransitionAsync(int targetChapterId)` method；try-catch 内 `await BeginTransitionAsync(targetChapterId);` + catch 内 `Log.Error` 兜底
- [ ] **AC-2 (handler tail)**: `OnRequestSceneChange` handler 不同章路径（line ~344-346 区域）末尾立即调 `DriveTransitionAsync(_inflightChapterId).Forget();` — 在 `TransitionTo(SceneManagerState.TransitionOut)` 之后、`return;` 之前；line 345 placeholder 注释更新为 spec align 注释
- [ ] **AC-3 (drain tail)**: `DrainPending` 不同章路径（line ~379-381 区域）末尾同样调 `DriveTransitionAsync(next).Forget();`
- [ ] **AC-4 (F4 stub removed)**: `DevTestState.cs` grep 0 hit `DriveProductionSceneTransitionAsync` / `BindingFlags` / `GetField("_sceneManager"` / `using System.Reflection`（如除此外无其他用）；OnEnter 内仅保留 `OnRequestSceneChange(1)` 派发 + `DevBootstrap.RunRequested()` 调用
- [ ] **AC-5 (ADR amendment)**: `docs/architecture/adr-009-scene-lifecycle.md` 文末 §History 添加 1 条 `2026-05-09 dusk` entry，描述本 story spec ↔ impl alignment fix；不改 §Decision 主体（spec 一直对的，仅 implementation 补齐）
- [ ] **AC-6 (R3 P1 listener-path first-boot)**: spike 派发 `OnRequestSceneChange(1)` → production listener 接收 → `DriveTransitionAsync` 启动 → 8 lifecycle event 顺序触发 (`OnSceneTransitionBegin(-1, 1)` → `OnSceneLoadProgress(...)` ≥1 次 → `OnSceneLoadComplete(1, "")` → `OnSceneReady(1)` → `OnSceneTransitionEnd(1)`) + post-transition `_sceneManager.CurrentLoadedChapterIdForTest == 1` + `_sceneManager.CurrentState == Idle`
- [ ] **AC-7 (R3 P2 same-chapter dedupe Idle path — driver 不进)**: post-P1 chapter 1 已 loaded + state=Idle；派 `OnRequestSceneChange(1)` → handler line 326-333 Idle 同章 path 命中（`targetId == _currentChapterId && _currentChapterId != NoChapterId`）→ `OnSceneReady(1)` 立即派；listener 全程观察 **0 次** 新 `OnSceneTransitionBegin`（driver 严格放在 line 346 line 326-333 之后，不污染 dedupe 路径）。**关键 invariant**：story-001c 的 `DriveTransitionAsync.Forget()` 必须严格放在 Idle 不同章 path 末尾，**不**能被 Idle 同章 / Error / in-flight / pending 各路径误触
- [ ] **AC-8 (R3 P3 unknown chapter fail-loud — driver 不进)**: spike-local SceneManager (`new SceneManager()` 不 Init) + `RegisterChapterDataProvider(id => id == 1 ? ChapterData : null)`；spike-local 直调 `BeginTransitionAsync(99)` → `LoadChapterSceneAsync` 内 line 459-466 `chapterData == null` fail-loud → `OnSceneLoadFailed(99, "*ChapterData*null*")` + state=Error。**与 story-001c relevance**：spike-local 路径模拟 production OnRequestSceneChange(99) 的 fail-loud 行为（即 `TryResolveOrFail` short-circuit short-circuits before line 346 DriveTransitionAsync），但 spike-local 不撞 production state — 与 S5-1b P4 等价复用，complement to P2 invariant
- [ ] **AC-9 (R3 P4 production Error state drop + RecoverToIdle round-trip)**: 复用 production `_sceneManager`（reflection）—（a) 派 `OnRequestSceneChange(99)` → handler line 336 TryResolveOrFail → state=Error + listener 收 `OnSceneLoadFailed(99, ...)`；（b) 在 Error 状态派 `OnRequestSceneChange(3)` → handler line 312-315 Error 分支 drop + Log.Warning + return；（c) reflection 调 `_sceneManager.RecoverToIdle()` → state=Idle；（d) 派 `OnRequestSceneChange(1)` → 同章 OnSceneReady（chapter 1 仍 loaded）。spike listener 全程观察 0 次新 `OnSceneTransitionBegin`（4 个不同 guard 路径都不进 driver）
- [ ] **AC-10 (R3 P5 listener self-removal × 5 cycles)**: production `_sceneManager` reflection Init/Dispose × 5；每 cycle 后再派 `OnRequestSceneChange(1)` 验 listener 仍可触发（同章 OnSceneReady）；5 cycle 后 framework `GameEvent` listener buffer 不 leak + 0 NullRef / RemoveListener exception（V2-5 framework boundary probe 第 4 次实战累计 — S5-03/S5-05/S5-06/S5-1b 之后第 4 次）

---

## R3 Justification (Logic / Integration — MANDATORY)

按 ADR-029 V2.0 R3 mandatory criterion，本 story 是**典型的 spec ↔ impl alignment fix** —— production code 改动小（~10 行新增 SceneManager + ~30 行删除 DevTestState）但 listener-path driver 是 ADR-009 §Decision spec 直接锚点，必须 PlayMode 实证。R3 probe 5 cases 强制覆盖。

### R3 PlayMode probe 5 cases（spike `S5-1c_ListenerPathDriver.cs` — **(M1) dual-layer 模式 复用 S5-1b 模板**）

> **Spike 模式 (M1) 复用 S5-1b precedent**:
> - **P1/P2/P4** 复用 production `GameApp._sceneManager` 实例（reflection 读 + listener subscribe production senders）；spike **不**自构建 SceneManager 也**不**直调 `BeginTransitionAsync`（与 S5-1b 区别：本 story 全程 listener-path 驱动）
> - **P3** 自构建独立 spike-local SceneManager（fail-loud 路径在 `TryResolveOrFail` 入口 short-circuit，不撞 production state）
> - **P5** production `_sceneManager` Init/Dispose × 5 cycle（V2-5 复用）

| # | Case | Setup | Action | Assert |
|---|---|---|---|---|
| **P1** | First-boot via listener-path | DevTestState `OnRequestSceneChange(1)` 派发 + spike subscribe production sender events | spike await production `_sceneManager.CurrentState == Idle` (timeout 5s) | 8 lifecycle event 顺序：`OnSceneTransitionBegin(-1, 1)` → (UnloadBegin first-boot guard skip) → `OnSceneLoadProgress(...)` ≥1 次 → `OnSceneLoadComplete(1, "")` → `OnSceneReady(1)` → `OnSceneTransitionEnd(1)` + production `_sceneManager.CurrentLoadedChapterIdForTest == 1` + `_sceneManager.CurrentState == Idle` |
| **P2** | Same-chapter dedupe Idle path — driver 不进 | post-P1（chapter 1 已 loaded + state=Idle）| spike `GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1)` 派发；production listener 接收 | line 326-333 Idle 同章 path：`OnSceneReady(1)` 立即触发 + spike listener 全程观察 **0 次** 新 `OnSceneTransitionBegin`（关键 invariant：driver `DriveTransitionAsync.Forget()` 严格放在 line 346 仅 Idle 不同章 path 末尾，不被 dedupe 路径误触）+ production `_sceneManager.CurrentState == Idle` + `InflightChapterIdForTest == NoChapterId`（无 in-flight 二次 transition）|
| **P3** | Unknown chapter fail-loud (spike-local) — driver 不进 | spike-local SceneManager (`new SceneManager()` 不 Init 避免 listener 冲突)；`RegisterChapterDataProvider(id => id == 1 ? new ChapterData(...) : null)` + `RegisterFadeOverlay(new NoOpFadeOverlay())`；listener subscribe global ISceneEvent (`OnSceneLoadFailed`) | spike-local 直调 `await BeginTransitionAsync(99)` + `LogAssert.Expect(LogType.Error, regex)` | spike listener 收到 `OnSceneLoadFailed(99, "*ChapterData*null*")` + spike-local `CurrentState == Error` + spike-local `CurrentLoadedChapterIdForTest == NoChapterId`（不污染）|
| **P4** | Production Error state drop + RecoverToIdle round-trip | 复用 production `_sceneManager`（reflection）；post-P2 chapter 1 仍 loaded + state=Idle；listener subscribe production sender events | (a) 派 `OnRequestSceneChange(99)` → production handler line 336 TryResolveOrFail → state=Error + listener 收 `OnSceneLoadFailed(99,...)`；(b) 派 `OnRequestSceneChange(3)` → handler line 312-315 Error 分支 drop + Log.Warning + return；(c) reflection 调 `production._sceneManager.RecoverToIdle()` → state=Idle；(d) 派 `OnRequestSceneChange(1)` → 同章 OnSceneReady（chapter 1 仍 loaded）| 全程 listener 观察 **0 次新 `OnSceneTransitionBegin`**（4 个不同 guard 路径都不进 driver）+ step (a) 后 state=Error + step (c) 后 state=Idle + step (d) 后 OnSceneReady 至少 1 次 |
| **P5** | Production listener self-removal × 5 cycles | reflection 拿 production `GameApp._sceneManager`；P4 完成后 chapter 1 loaded + state=Idle | 5 cycle：reflection 调 `_sceneManager.Dispose()` + 立即 `Init()` 重新 register listener；每 cycle 后派 `OnRequestSceneChange(1)` 验 listener 仍触发（同章 OnSceneReady）| 每次 cycle 后 listener 仍可 trigger + 5 cycle 后 framework `GameEvent` listener buffer 不 leak + 0 NullRef / 0 RemoveListener exception（V2-5 framework boundary probe 第 4 次累计 S5-03/S5-05/S5-06/S5-1b）|

**evidence JSON schema** (`Application.persistentDataPath/S5-1c_Result.json`):

```json
{
  "story_id": "S5-1c",
  "timestamp": "2026-05-XX",
  "cases": [
    {"id": "P1", "passed": true, "asserts": [{"name": "CurrentLoadedChapterId", "expected": 1, "actual": 1, "passed": true}, ...], "events_received": ["OnSceneTransitionBegin(-1,1)", ...]},
    ...
  ],
  "all_passed": true
}
```

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **GameFlow `SceneTransitionState` FSM state** — outline 决策 rejected (方案 B)；本 story 走方案 A SceneManager 内自闭
- **`OnRequestSceneChange` 重入 queue 升级** — 当前靠 `_state` guard + `_pendingTargetChapterId` newest-wins；queue 升级留 polish phase（如 S5-02 实测发现限制再起 follow-up）
- **`async void` listener handler 异常审计协议** — 与项目内 IInputBlockerEvent / ISettingsEvent / IAudioEvent listener 模式一致（Sprint 5 三系统先例），统一 review 留 Sprint 5/6 retro
- **S5-02 end-to-end 5 系统串通**（main menu → chapter button → chapter 1 → ...）— S5-02 启动后由本 story driver 路径 unblock
- **ADR-009 §Decision 主体修订** — 不改 spec（一直对的）；仅 §History 加 1 条 implementation alignment entry

---

## QA Test Cases

*Logic / Integration type — automated PlayMode (R3) + grep-based static evidence*

- **AC-1, AC-2, AC-3 (grep evidence)**: 自动可验
  - `grep -c "DriveTransitionAsync" Assets/GameScripts/HotFix/GameLogic/Scene/SceneManager.cs` ≥ 3（method 定义 + 2 处 .Forget() 调用）
  - `grep -c "private async UniTaskVoid DriveTransitionAsync" Assets/GameScripts/HotFix/GameLogic/Scene/SceneManager.cs` == 1
  - `grep -A 3 "TransitionTo(SceneManagerState.TransitionOut)" Assets/GameScripts/HotFix/GameLogic/Scene/SceneManager.cs` 应在 OnRequestSceneChange + DrainPending 各显示 1 处 `DriveTransitionAsync(...).Forget();` 紧随其后
- **AC-4 (F4 stub removed grep)**: 自动可验
  - `grep -c "DriveProductionSceneTransitionAsync" Assets/GameScripts/HotFix/GameLogic/GameFlow/DevTestState.cs` == 0
  - `grep -c "BindingFlags\|GetField\(\"_sceneManager\"\|System.Reflection" Assets/GameScripts/HotFix/GameLogic/GameFlow/DevTestState.cs` == 0
- **AC-5 (ADR amendment)**: `grep "2026-05-09.*story-001c\|listener-path driver" docs/architecture/adr-009-scene-lifecycle.md` ≥ 1
- **AC-6..AC-10 (R3 PlayMode probe)**: 自动 — spike 内 assert + JSON evidence
- **R3 ALL PASS**: `cat ~/Library/Application Support/.../S5-1c_Result.json | jq .all_passed == true`
- **Console 0 unexpected error**: `read_console` filter level=Error；除 P3 case 主动写的 expectedError 外 == 0

---

## Test Evidence

**Story Type**: Logic / Integration
**Required evidence**:

- **Spike**: `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-1c_ListenerPathDriver.cs` — **1 个文件 + 3 内类**（`S51cSpike : IDevSpike` + `S51cRuntime : MonoBehaviour` + `S51cTester` 纯逻辑）；与 S5-1b/S301-S303 spike 单文件 3 内类模式一致
- **JSON evidence**: `~/Library/Application Support/<company>/<product>/S5-1c_Result.json`（5 case 全 PASS schema 见上）
- **QA evidence doc**: `production/qa/playmode-listener-path-driver-2026-05-XX.md` — 含
  - JSON evidence summary table（5 case 全 PASS）
  - Console snapshot (R3 path 0 unexpected error；P3 expected `Debug.LogError` 标记)
  - `git diff --stat HEAD` 改动 evidence（SceneManager.cs ~10 行 + DevTestState.cs ~30 行 + ADR-009 §History +1 条）
  - 8 lifecycle event 顺序 dump（spike Tester listener 收到的 event log，按时间序）
  - F4 stub removal verification — grep 全 0 hit
- **Production code review**: SceneManager.cs + DevTestState.cs diff 走 code-review skill (Lead Programmer agent) 一次

**Status**: Pending — 待 dev-story 实施。

---

## Dependencies

- **Depends on**:
  - **S5-1b ✅ DONE 2026-05-09** — boot pipeline 接入 + F4 dev-only stub（本 story 移除）
  - **S3-01..03 ✅ CORE PASSED** — 11-step driver framework boundary 已 verified
  - **S2-05/S2-07 ✅** — SceneManager state machine + TryResolveOrFail fail-loud 协议
  - **ADR-009 (Accepted)** §Decision line 386 + **ADR-029 V2.0 (Accepted) R3 ready**
- **Unlocks**:
  - **F4 dev-only stub 永久移除** — `DevTestState.cs` 干净（dev-only 路径回归仅 `OnRequestSceneChange(1)` 派发）
  - **S5-02** end-to-end 5 系统串通 — main menu trigger / chapter button → `OnRequestSceneChange` → 自驱 11 步；无 F4 stub carry

---

## Assumptions Validated (R2 — 2026-05-09 readiness gate ✅ 8/8 PASS)

R2 grep verify 在 readiness gate 阶段（2026-05-09）逐条实证完成，全部 ✅：

| # | 假设 | 实测 grep evidence | 结果 |
|---|------|-------------------|------|
| **R2.1** | ADR-009 §Decision line 386 spec 仍 authoritative | ADR-009.md:7 `Accepted (2026-04-22) | Updated 2026-04-30 — §SceneHandle Ownership superseded by S3-01 D5; ... ADR core decisions (additive-only, 11-step flow, mandatory cleanup, MainScene persistence) remain authoritative`；本 story relevant section（§Decision line 386 listener-path orchestration）无 superseded | ✅ |
| **R2.2** | `OnRequestSceneChange` handler 现有 5 guard 完整 | SceneManager.cs:309-351 5 guard 已 Read 完整：(a) `_state == Error` 静默 drop+warning (line 312-315) (b) in-flight dedupe `targetId == _inflightChapterId` Log.Info ignored (line 320-323) (c) Idle 同章 `targetId == _currentChapterId && _currentChapterId != NoChapterId` → OnSceneReady (line 326-333) (d) `TryResolveOrFail` short-circuit (line 336-339) (e) Non-Idle `_pendingTargetChapterId = targetId` newest-wins (line 350) | ✅ 5 guard intact |
| **R2.3** | `BeginTransitionAsync` 内部 fail-loud 协议已 verified | SceneManager.cs:638-673 已 Read：line 657-662 `if (_state == SceneManagerState.Error) return;` 显式失败短路；`LoadChapterSceneAsync` 内 line 421-422 + line 431-432 `OnSceneLoadFailed(...) + TransitionTo(Error)` 实证；`TryResolveOrFail` line 260-261 同协议 | ✅ |
| **R2.4** | `Cysharp.Threading.Tasks` ns 已 import | SceneManager.cs:3 `using Cysharp.Threading.Tasks;` 实证 hit | ✅ |
| **R2.5** | M1 dual-layer pattern 适用 | S5-1b spike `S5-1b_BootSceneLoad.cs` 5 case (P1-P3 production reflection + P4-P5 isolated local) PlayMode 5/5 PASS first-run 实证 working | ✅ S5-1b precedent |
| **R2.6** | `LogAssert.Expect` 对 fail-loud Log.Error 有效 | S5-1b P4/P5 实测 LogAssert.Expect 路径 OK；evidence doc §10 verified | ✅ S5-1b precedent |
| **R2.7** | Spike "1 文件 + 3 内类" 惯例 | Glob `Spikes/*.cs` 全部命中：S301_AdditiveSceneLoading.cs / S302_CleanupSequence.cs / S303_SceneEventOrdering.cs / S407_PlayModeBatch.cs / S5-03_PuzzleStateMachine.cs / S5-05_NarrativeSequenceEngine.cs / S5-06_AudioMixArchitecture.cs / S5-1b_BootSceneLoad.cs 全部一致 | ✅ |
| **R2.8** | `DevTestState.cs` `System.Reflection` 移除后无残留 | DevTestState.cs:10 `using System.Reflection;` + line 53 `BindingFlags.NonPublic\|BindingFlags.Static` 是**仅有**用法 — 全部用于 line 49-76 F4 stub method `DriveProductionSceneTransitionAsync`；F4 stub 删除后 `using System.Reflection;` 可一并 remove | ✅ 可清空 |

**R2 8/8 PASS** — readiness gate 一次过；无 deficiency-flagged path 触发；dev-story 阶段无遗留 R2 待复确认项。

---

## ADR-029 V3 Watch List Hooks

本 story R3 实施过程中如出现以下情况应 capture 为新 drift type 候选并写入 sprint-status.yaml watch list：

1. **Type-2(c) candidate**: `BeginTransitionAsync` 内部 fail-loud 协议在 listener-path 入口下行为与 spike 直调路径不一致（如 state=Error 时机差异）— 本 story 实测如 spike P3/P4 行为偏移期望则为 framework boundary behavior assumption drift
2. **Type-5 candidate (per S5-01 dp1 promote 候选)**: `async void` listener handler 异常逃逸到 Unity log 时**栈无法 trace 到 OnRequestSceneChange 派发方** — 如 PlayMode 实测发现，则 promote 为 V3 #8 dp5 数据点（与 S5-05 dp5 IPuzzleLockEvent contract design alignment 同类 framework method signature 路径）
3. **Type-6 candidate**: F4 stub 移除后，spike listener 自挂自摘协议（V2-5 第 4 次实战）如出现新 leak 类型（5 cycle 之外的 long-running session）则 promote 为新 watch list trigger

如出现以上任一，per ADR-029 V2.0 §V2-7：sprint-status.yaml `watch_list` triggers 内追加 drift type 描述 + 关联 story-001c R3 case 编号 + 沉淀 problem memo 到 `.claude/memory/`。

---

## ADR-009 §History Amendment Plan

本 story 仅在 ADR-009 文末 §History 添加 1 条新 entry，**不改 §Decision 主体**：

```
- **2026-05-09 dusk** — Implementation alignment fix (story-001c)：
  SceneManager.OnRequestSceneChange handler 与 DrainPending 末尾添加 internal
  DriveTransitionAsync(targetChapterId).Forget() tail，移除 line 345 inline comment
  "// Story 002 接管后续 11 步流程" placeholder。ADR §Decision line 386 spec 文字
  ("Scene Manager subscribes to OnRequestSceneChange and orchestrates the entire
  11-step flow internally") 现在与 implementation 1:1 alignment。Verified by
  story-001c R3 PlayMode 5/5 PASS。S5-1b 临时 F4 dev-only stub
  (DevTestState.DriveProductionSceneTransitionAsync via reflection) 已移除。
  **No spec changes** — 仅 implementation 补齐（ADR §Decision 一直 authoritative，
  S5-1b dev-story 暴露 spec ↔ impl gap 而非 spec gap）。
```

---

## Completion Notes (2026-05-09)

- **R3 PlayMode 实证**: 5/5 case PASS、24/24 assert PASS、0 unexpected error / 0 unexpected warning（1 expected warning：unknown chapter 99 fail-loud；1 expected error：bad-asset path）。详见 `production/qa/playmode-listener-path-driver-2026-05-09.md`。
- **AC Matrix**: 10/10 ✅（AC-1~AC-10 详见 evidence §4）。
- **Implementation diff**:
  - `SceneManager.cs` +30/-2（`DriveTransitionAsync` 新增 + `OnRequestSceneChange`/`DrainPending` 各 1 处 tail integration）
  - `DevTestState.cs` -32/+5（F4 reflection stub 完全移除 + OnEnter 顺序调整 to fix sync-subscribe race）
  - `S5-1c_ListenerPathDriver.cs` +335（新 spike，1 file 3 inner class，M1 dual-layer）
  - `GameApp.cs` ±6（spike 注册 S5-1b → S5-1c）
  - `adr-009-scene-lifecycle.md` +9（§History 新增 2026-05-09 dusk entry）
- **Sync-subscribe race iteration**: 实施过程发现 `OnRequestSceneChange(1)` 在 `S51cRuntime.Start()` subscribe 前同步 fire `OnSceneTransitionBegin`，导致 P1 漏 capture。修复：(a) `DevTestState.OnEnter` 调用顺序 `RunRequested()` → `OnRequestSceneChange(1)`；(b) spike subscribe 时机从 `Start()` 上调到 `Awake()`（Awake 在 `AddComponent` 同步 return 时执行）。已沉淀 `Watch List Hooks` Type-2(c) 候选（详见 evidence §6）。
- **F4 stub 永久移除**: `DevTestState.cs` 仅保留 `OnEnter` 内 `OnRequestSceneChange(1)` 派发 + `DevBootstrap.RunRequested()` 触发，无任何 reflection / Production singleton 内部 method 直调。
- **ADR-009 spec ↔ impl 1:1 对齐**: §Decision line 386 ("Scene Manager subscribes to OnRequestSceneChange and orchestrates the entire 11-step flow internally") 实施完成；§History 已加 2026-05-09 dusk amendment entry。

