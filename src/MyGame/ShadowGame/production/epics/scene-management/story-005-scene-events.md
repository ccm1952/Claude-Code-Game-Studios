// 该文件由Cursor 自动生成

# Story 005: 6 Scene Lifecycle Event Senders & End-to-End Ordering

> **Epic**: Scene Management
> **Status**: Done (patch v3 applied 2026-04-30 dusk — P5 PlayMode FAIL → AC-9 reformulation + P5 拆 P5a/P5b + TEngine framework behavior fact 文档化)
> **Layer**: Core
> **Type**: Integration (PlayMode-only — D1=[b] inherited from S3-01)
> **Manifest Version**: 2026-04-30 patch v3 (post-PlayMode CORE 5 PASSED + Type-2 子类 (c) Framework behavior assumption drift 第 5 数据点 + ADR-029 V2 候选 #7 framework boundary behavior probe)
>
> **Revision note (2026-04-23)**: 原文定义 8 个 `Evt_Scene*` int 常量（1400–1407）+ 8 个 `XxxPayload` struct（ADR-006 协议）；**整体作废**。新版 story 基于 ADR-027 接口事件协议：`ISceneEvent` 的 9 方法签名在 Story 001（S2-05）已全部冻结，其中 `OnRequestSceneChange` / `OnSceneReady` 已实装。**本 story 范围收窄**为：
> - 实装 Story 002/003 未覆盖的 3 方法 sender — `OnSceneTransitionBegin`、`OnSceneTransitionEnd`（Story 003 已实装 `OnSceneUnloadBegin` sender，Story 002 已实装 `OnSceneDownloadProgress` / `OnSceneLoadProgress` / `OnSceneLoadComplete` / `OnSceneLoadFailed`）
> - 端到端 transition 顺序断言（listener 观察器 + deterministic order 断言）
> - scene-scoped listener 自移除文档模式（旧 ADR-006 §3 语义用 ADR-027 重写）
>
> **Revision note (2026-04-30 dusk patch v2 — ADR-029 R2 STOP 8 处 drift)**: story-005 写于 2026-04-22，早于 S3-01 D5 决策 (2026-04-30 morning) + S3-02 v3 Type-2 cross-method protocol fix (2026-04-30 dusk) — 两次 architecture decision 都未传播到本 story（propagation 实战首测 scope 仅含 active sprint，未含 ready backlog 故漏 story-005）。本 patch v2 修订 8 处：
>
> **Type-4 propagation drift (D5 + S3-02 v3 滞后；6 处)**：
> 1. `int fromChapterId = _currentChapterId` → `int fromChapterId = _currentLoadedChapterId`（S3-02 v3 cross-method protocol fix；`_currentChapterId` 在 `OnRequestSceneChange` 入口已被设为 NEW target，from chapter id 应取自"已加载身份"字段 `_currentLoadedChapterId`）
> 2. `if (fromChapterId != -1) await CleanupAsync(fromChapterId)` → `await UnloadCurrentChapterAsync()`（S3-02 已实装 method 名 + 内部 first-boot guard via `_currentLoadedChapterId == NoChapterId`，无需外层 if + 显式参数）
> 3. AC-7 `首次启动 (_currentChapterId == -1)` → `首次启动 (_currentLoadedChapterId == NoChapterId)` 哨兵
> 4. `_inflightChapterId = -1` → `_inflightChapterId = NoChapterId`
> 5. QA test case 提及 `SceneManager._currentChapterId == 1` → `SceneManager._currentLoadedChapterId == 1`
> 6. AC-1 sender 文案中的 `(fromChapterId, toChapterId)` 仍正确（参数名约定）；但 source 取值改 `_currentLoadedChapterId`
>
> **Type-1 fantasy API drift (2 处)**：
> 7. `bool ok = await LoadChapterSceneAsync(targetChapterId)` → `await LoadChapterSceneAsync(targetChapterId); if (CurrentState == SceneManagerState.Error) return;`（实际 method 签名 `public async UniTask LoadChapterSceneAsync(int)` 返 void/UniTask，**不**是 `UniTask<bool>`；失败检测通过 state machine 状态判断 — S3-01 docstring 已明确）
> 8. `await _fadeOverlay.FadeOutAsync()` / `FadeInAsync()` 4 处 — `_fadeOverlay` 字段不存在，fade UI infra 未实装（应留 future fade story）。本 patch 选 **decision [b]**：注入 `IFadeOverlay` placeholder 接口 + `NoOpFadeOverlay` 默认 impl，保持 BeginTransitionAsync 11 步骨架完整；future fade story 通过 `RegisterFadeOverlay(IFadeOverlay)` setter 替换默认 impl（与 `RegisterChapterDataProvider` 同 DI pattern）
>
> **Test path pivot (D1=[b] propagation)**：原 `Assets/Tests/EditMode/SceneManagement/SceneEventOrderingTests.cs` `[UnityTest] IEnumerator + UniTask.ToCoroutine` → `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S303_SceneEventOrdering.cs` IDevSpike + OnGUI + JSON evidence 模式（沿 S3-01 / S3-02 spike 模式；GameApp 单 spike 注册防 type-3 race）
>
> **ADR-029 第 4 数据点 + V2 候选 #6 触发**：本 patch 修订估约 ~15 min（vs S3-01 baseline 18 / S3-02 Phase 1.5 Type-4-only 8 / S3-02 R3 Type-2 21）— 比 S3-02 Phase 1.5 慢 ~2x，因为同时撞 Type-4 propagation 滞后 + Type-1 fantasy（fade）+ D1=[b] test path propagation。**Root cause**：S3-01 D5 propagation 实战首测 scope 仅含 12 active files (active sprint stories)，未覆盖 ready backlog story-005 → ADR-029 V2 候选 #6 = `propagation scope` rule（应覆盖所有 `Status: Ready` 及以上的 stories，不仅 active sprint）。本 story 是该 V2 候选规则的第一个失败数据点。
>
> **Revision note (2026-04-30 dusk patch v3 — PlayMode P5 FAIL → AC-9 reformulation + Type-2 子类 (c) Framework behavior assumption drift 第 5 数据点)**: PlayMode 首跑 5/6 case PASS (P1/P2 ADV/P3/P4/P6 ADV)，但 **P5 FAIL** — `lastError: "P5 double-remove threw: Delete handle failed, not exist, EventId: ISceneEvent_Event.OnSceneUnloadBegin"`。**Root cause**: patch v2 AC-9 假设 `GameEvent.RemoveEventListener` (TEngine EventMgr) 是 idempotent（"双重移除不抛异常"），但实测 TEngine **抛 exception** "Delete handle failed, not exist" 当 listener 不存在。这是新发现的 **Type-2 cross-method protocol drift 子类 (c) — Framework behavior assumption drift**：API 存在 ✅（不是 fantasy）、但**单 method 行为契约假设错误**（idempotent vs throwing）；grep 0 hit 不可发现，**仅 R3 PlayMode probe 可暴露**。
>
> **patch v3 修订**：
> 1. **Engine Notes 新增 framework knowledge fact**：`TEngine EventMgr 行为契约：GameEvent.RemoveEventListener 在 listener 不存在时抛 Exception "Delete handle failed, not exist"，不是 idempotent silent no-op；recommended pattern = handler 内 self-remove + null-out _handler 字段 + 外部 cleanup 必须 if (_handler != null) check。`
> 2. **Control Manifest 新增 Required**：`Scene-scoped self-removal pattern 必须使用 handler null-out + external null-check guard；禁止 raw double-remove (会抛 TEngine exception)`
> 3. **AC-8 / AC-9 reformulation**：原 AC-9 "双重移除不抛异常 (TEngine EventMgr 语义验证)" → 新 AC-9 "TestSceneScopedFixture documented pattern (handler null-out + Cleanup() 内 null-check guard) 全程无异常 + invokeCount == 1"；AC-8 措辞精化加 "+ null-out _handler"。
> 4. **Implementation Notes §4 self-removal pattern 强化**：原文档已有 `_onUnloadBeginHandler = null` 但未明确说明意图；patch v3 加注 "// 关键：null-out 防 double-remove；TEngine RemoveEventListener 不是 idempotent" + 加补完整 fixture 模板含 `Cleanup()` with `if (_handler != null)` 守卫。
> 5. **QA Test Cases P5 拆 P5a + P5b**：P5a (CORE) self-removal — handler 内 RemoveEventListener；2nd dispatch 不再触发 invokeCount == 1；P5b (CORE) NullOutGuardPattern — `TestSceneScopedFixture` 文档化 fixture：Init → dispatch (handler invoked + null-out) → Cleanup (null-check skip) → 2nd dispatch (not invoked) → 全程**无 exception**。
> 6. **S303 spike 同步更新**：`S303_SceneEventOrdering.cs` 内嵌 `TestSceneScopedFixture` 私有类做 P5b 验证；P5 从 1 case 拆 2 case；JSON schema 增加 `p5a_passed` / `p5a_invokeCount` / `p5b_passed` / `p5b_invokeCount` 字段。
>
> **PlayMode 验证结果 (v3 二跑 — 2026-04-30T09:37:58.6846410Z)**：
> ```json
> {
>   "phase": "done",
>   "p1_passed": true,        // ✅ Begin(201,202) → UnloadBegin(201) → 4× LoadProgress → LoadComplete(202) → Ready(202) → TransitionEnd(202)
>   "p2_status": "SkipAdvisory", // Editor cache hit DPCount=0
>   "p3_passed": true,        // ✅ Begin(202,999) → UnloadBegin(202) → LoadFailed(999); 不派 LC/R/TE; CurrentState=Error
>   "p4_passed": true,        // ✅ Begin(-1,201) → 4× LoadProgress → LoadComplete(201) → Ready(201) → TransitionEnd(201); UnloadBegin 不派
>   "p5_passed": true,        // ✅ legacy aggregate
>   "p5a_passed": true,       // ✅ self-removal invokeCount=1
>   "p5b_passed": true,       // ✅ NullOutGuardPattern invokeCount=1, no exception (v3 fix 验证有效)
>   "p6_status": "SkipAdvisory", // 主 _sceneManager P3 后 Error
>   "allCorePassed": true,    // ✅ CORE 5/5 PASS
>   "listenerCounts": { "TB":3, "UB":6, "DP":0, "LP":12, "LC":3, "R":2, "TE":2, "LF":1 },
>   "lastError": ""
> }
> ```
>
> **listenerCounts 一致性验证** (与 ADR-009 11 步流程 + S3-02 v3 protocol + patch v3 null-out guard 完全一致)：
> - TB=3：P1 + P3 + P4 三次完整 BeginTransitionAsync ✅
> - UB=6：P1+P3 各 1 (BeginTransition 内部派 + S303Tester._onUB 计数) + P5a 2 dispatches + P5b 2 dispatches = 6 ✅
> - DP=0：Editor cache hit（YooAsset 模拟模式） ✅
> - LP=12：P1 prep load 201 (4) + P1 trans 202 (4) + P4 first-boot 201 (4) = 12 ✅
> - LC=3：P1 prep + P1 trans + P4；P3 失败不派 ✅
> - R=2：P1 + P4 — 仅 BeginTransitionAsync Step 11 派；P1 prep LoadChapterSceneAsync 不经 Step 11 ✅
> - TE=2：P1 + P4；P3 失败短路 ✅
> - LF=1：P3 ✅
>
> **Type-2 (c) Framework behavior assumption fix 验证有效** ✅：P5b 通过 `TestSceneScopedFixture` documented pattern (handler null-out + Cleanup null-check guard) 完全无 exception 抛出，证明 patch v3 修订正确。
>
> **ADR-029 第 5 数据点 + V2 候选 #7**：patch v3 修订估约 ~12 min（PlayMode probe 暴露 ~2 min + 决策矩阵 3 min + spike 拆 P5a/P5b 4 min + AC + Engine Notes 3 min）。**Type-2 子类 (c)** Framework behavior assumption drift 是 R3-only 可发现 drift 的第二个具体形态（第一个是 S3-02 v3 的 cross-method state lifecycle）。**V2 候选 #7**：R3 PlayMode probe 必须明确包括 framework boundary behavior 测试（验证 framework method 在异常路径下的实际行为，不只是 cross-method state 协议）。

## Context

**GDD**: `design/gdd/scene-management.md`
**Requirement**: `TR-scene-014`
*(8 scene lifecycle events fire in deterministic order; all listeners notified via single interface)*

**ADR Governing Implementation**: ADR-009: Scene Lifecycle + ADR-027: GameEvent Interface Protocol
**ADR Decision Summary**: 8 个 lifecycle 广播 + 1 个 request 命令共 9 方法全部在 `ISceneEvent`（`GroupLogic`）。每次完整 transition 按 `OnSceneTransitionBegin` → `OnSceneUnloadBegin` → `[OnSceneDownloadProgress]*` → `OnSceneLoadProgress*` → `OnSceneLoadComplete` → `OnSceneReady` → `OnSceneTransitionEnd` 顺序派发；失败路径以 `OnSceneLoadFailed` 替代尾部 `OnSceneLoadComplete → OnSceneReady → OnSceneTransitionEnd`。方法参数即 payload，无 struct。

**Engine**: Unity 2022.3.62f2 LTS + TEngine 6.0.0 + UniTask 2.5.10 | **Risk**: MEDIUM
**Engine Notes**:
- SG 生成的 `ISceneEvent_Gen` 将所有 9 方法 dispatcher 注册到 `GroupLogic`；`GameEvent.Get<ISceneEvent>().OnX(...)` 同步派发，每方法 dispatch ≤ 0.01ms（参考 `IGestureEvent` 实测）。8 lifecycle + 若干 progress 回调总耗时 < 0.4ms（非帧级热点）。
- `SceneManager.LoadChapterSceneAsync(int)` 实际签名 `public async UniTask`（**返 void/UniTask，不是 UniTask<bool>**）；失败检测通过 `if (CurrentState == SceneManagerState.Error) return;`（S3-01 docstring + ADR 已明确，OnSceneLoadFailed 由内部 retry-exhaust 路径派发）。
- `SceneManager.UnloadCurrentChapterAsync()` 实际签名无参数；内部 first-boot guard via `_currentLoadedChapterId == NoChapterId || string.IsNullOrEmpty(_currentChapterSceneName)` → silent return。S3-02 v3 修订过守卫从 `_currentChapterId`（状态机目标）切到 `_currentLoadedChapterId`（已加载身份），`BeginTransitionAsync` 直接 `await UnloadCurrentChapterAsync()` 即可，无需外层 if 守卫或显式 chapter id 参数。
- **`fromChapterId` 取值约定（S3-02 v3 / Type-2 cross-method protocol fix）**：在 `BeginTransitionAsync` 内部，`int fromChapterId = _currentLoadedChapterId;` —— **不要**用 `_currentChapterId`，后者在 `OnRequestSceneChange` 入口已被设为 NEW target。`_currentLoadedChapterId` 是"已加载章节身份"单一事实源（与 `_currentChapterSceneName` atomic 配对，S3-02 v3 修复引入）。
- **IFadeOverlay placeholder pattern**：fade UI infra 未实装；本 story 注入 `IFadeOverlay { UniTask FadeOutAsync(); UniTask FadeInAsync(); }` 接口 + `NoOpFadeOverlay` 默认 impl（两 method 直接 return `UniTask.CompletedTask`）；SceneManager 通过 `RegisterFadeOverlay(IFadeOverlay)` setter 接受替换 impl（与 `RegisterChapterDataProvider` 同 DI 模式）；future fade story 通过 boot pipeline 调 `scene.RegisterFadeOverlay(new UIFadeOverlay(...))` 替换。**保持 BeginTransitionAsync 11 步骨架完整 + 不引入 future story 反向依赖**。
- **TEngine EventMgr 行为契约 fact (patch v3 — PlayMode 实测发现)**：`GameEvent.RemoveEventListener<T>(eventId, handler)` **在 listener 不存在时抛 Exception** "Delete handle failed, not exist"，**不是** idempotent silent no-op。Recommended pattern：
  - Handler 内 self-remove 后必须 `_handler = null` (null-out 字段引用)
  - 外部 cleanup 必须 `if (_handler != null) RemoveEventListener(...)` (null-check guard)
  - **禁止** raw double-remove（不带 null-check 的重复调用）—— 会抛 TEngine exception
  - 同等问题对 ADR-027 §3 scene-scoped listeners 全部适用（不只 ISceneEvent）
- D1=[b] PlayMode-only：BeginTransitionAsync 11 步内嵌 GameModule.Scene + Resources.UnloadUnusedAssets + GameEvent dispatch chain 全部依赖 Unity runtime；EditMode test runner 不可达；evidence 通过 `S303_SceneEventOrdering.cs` IDevSpike 在 PlayMode DevTestState 跑（沿 S3-01 / S3-02 spike 模式）。

**Performance**: 8 生命周期事件每次章节切换总计 ≤ 0.4ms（冷路径，每章 1 次，~5 次/游戏）；`OnSceneLoadProgress` 每帧派发若有订阅，每方法 ≤ 0.01ms；NoOpFadeOverlay 两 method 各 ~0ms（直接 return `UniTask.CompletedTask`）；N/A 无帧级 hotpath budget。

**Control Manifest Rules (this layer)**:
- Required: `All scene-domain cross-module events via ISceneEvent interface methods — 单一契约` (ADR-027 §1)
- Required: `ISceneEvent 置于 GameLogic/IEvent/ 模块；[EventInterface(EEventGroup.GroupLogic)]` (ADR-027 §1)
- Required: `Cross-event cascade depth must not exceed 3` (ADR-027 §2)
- Required: `Scene-scoped listeners must self-remove inside ISceneEvent.OnSceneUnloadBegin handler via GameEvent.RemoveEventListener` (ADR-027 §3)
- Required: `Every ISceneEvent method must carry XML doc with Sender / Listener / Cascade` (ADR-027 §1, Story 001 已落地)
- Required: `BeginTransitionAsync uses _currentLoadedChapterId for fromChapterId source (NOT _currentChapterId; latter is state machine target post-OnRequestSceneChange)` (S3-02 v3 — Type-2 cross-method protocol fix)
- Required: `Failure detection via SceneManager.CurrentState == SceneManagerState.Error after await LoadChapterSceneAsync (LoadChapterSceneAsync returns UniTask, not UniTask<bool>; failed path internally fires OnSceneLoadFailed + transitions to Error)` (S3-01 docstring)
- Required: `Cleanup invocation: await UnloadCurrentChapterAsync() (no parameter; internal first-boot guard via _currentLoadedChapterId == NoChapterId)` (S3-02)
- Required: `IFadeOverlay placeholder injected via RegisterFadeOverlay(IFadeOverlay) setter; default = NoOpFadeOverlay (UniTask.CompletedTask); future fade story replaces impl` (本 story v2)
- Required: `_inflightChapterId reset uses NoChapterId sentinel, never literal -1` (style consistency post-S3-02)
- Required: `Scene-scoped self-removal pattern 必须使用 handler null-out + external null-check guard; 禁止 raw double-remove (TEngine RemoveEventListener 不是 idempotent, 会抛 "Delete handle failed, not exist")` (本 story v3 — Type-2 子类 (c) Framework behavior assumption fix)
- Forbidden: `Never define Evt_Scene* int constants or SceneXxxPayload structs — ADR-006 协议在本 epic 整体作废` (ADR-027 §1)
- Forbidden: `Never use anonymous object parameters or raw int encoding for payloads (参数直接强类型)` (ADR-027 §1)
- Forbidden: `await CleanupAsync(int) — does not exist; use UnloadCurrentChapterAsync()` (Type-1 fantasy API)
- Forbidden: `bool ok = await LoadChapterSceneAsync(...) — fantasy return type; method returns UniTask` (Type-1 fantasy API)
- Forbidden: `int fromChapterId = _currentChapterId — Type-2 cross-method protocol drift (S3-02 v3 fixed); use _currentLoadedChapterId` (S3-02 v3)
- Forbidden: `raw double-remove (RemoveEventListener 之前未 null-check) — TEngine 抛 "Delete handle failed, not exist"` (本 story v3 — Type-2 子类 (c))
- Guardrail: `ISceneEvent per-method dispatch ≤ 0.01ms (p99)` (ADR-027 §2)

---

## Acceptance Criteria

*From GDD `design/gdd/scene-management.md`, scoped to this story:*

### A. Sender 补齐（Story 002/003 未覆盖的 3 方法）

- [x] **AC-1** `SceneManager.BeginTransitionAsync(int targetChapterId)` 在 TransitionOut 之前（对应 11 步流程 Step 3）派发 `GameEvent.Get<ISceneEvent>().OnSceneTransitionBegin(fromChapterId, toChapterId)` 恰 1 次；其中 `fromChapterId = _currentLoadedChapterId`（S3-02 v3 protocol；first-boot 路径下 = `NoChapterId`）— **PlayMode P1/P3/P4 验证 TBCount=3 与 3 次完整 transition 一致 ✅**
- [x] **AC-2** TransitionIn 完成后（对应 Step 11 fade-in 后）派发 `GameEvent.Get<ISceneEvent>().OnSceneTransitionEnd(toChapterId)` 恰 1 次；且先于 `OnSceneTransitionEnd` 必先派发 `OnSceneReady(toChapterId)` — **PlayMode P1 序列 `LoadComplete(202) → Ready(202) → TransitionEnd(202)` 顺序断言 PASS ✅**
- [x] **AC-3** 失败路径（`OnSceneLoadFailed` 已派发；`SceneManager.CurrentState == SceneManagerState.Error`）**不得**继续派发 `OnSceneReady` / `OnSceneTransitionEnd` — **PlayMode P3 序列 `Begin(202,999) → UnloadBegin(202) → LoadFailed(999)` 后无 LC/R/TE PASS ✅**

### B. 端到端顺序（PlayMode spike harness）

- [x] **AC-4** 成功路径（缓存命中，无下载）完整序列为：`OnSceneTransitionBegin` → `OnSceneUnloadBegin` → `OnSceneLoadProgress*` (≥1) → `OnSceneLoadComplete` → `OnSceneReady` → `OnSceneTransitionEnd` — **PlayMode P1 9 项序列 PASS ✅**
- [ ] **AC-5** (ADV) 成功路径（未缓存，需下载）完整序列：`OnSceneTransitionBegin` → `OnSceneUnloadBegin` → `OnSceneDownloadProgress*` (≥1) → `OnSceneLoadProgress*` (≥1) → `OnSceneLoadComplete` → `OnSceneReady` → `OnSceneTransitionEnd`（`OnSceneDownloadProgress` 全部先于任何 `OnSceneLoadProgress`）— **PlayMode P2 SkipAdvisory（DPCount=0; Editor cache hit；advisory，留 backlog standard PlayMode test）**
- [x] **AC-6** 失败路径（MAX_LOAD_RETRY 耗尽）序列：`OnSceneTransitionBegin` → `OnSceneUnloadBegin` → `OnSceneLoadFailed`（之后**不得**有 `OnSceneLoadComplete` / `OnSceneReady` / `OnSceneTransitionEnd`）— **PlayMode P3 PASS ✅**
- [x] **AC-7** 首次启动（`_currentLoadedChapterId == NoChapterId`）：跳过 `OnSceneUnloadBegin`（Story 003 AC-8 first-boot guard 已覆盖），序列为：`OnSceneTransitionBegin(NoChapterId, target)` → (`OnSceneDownloadProgress*`) → `OnSceneLoadProgress*` → `OnSceneLoadComplete` → `OnSceneReady` → `OnSceneTransitionEnd` — **PlayMode P4 PASS ✅ (Begin(-1,201) 起首 + UB count == 0)**

### C. Scene-scoped listener 自移除模式（文档 + 测试 — patch v3 reformulation）

- [x] **AC-8** 以一个 `TestSceneScopedFixture` 演示 self-removal：`Init()` 订阅 `ISceneEvent.OnSceneUnloadBegin` → 在 handler 内调 `GameEvent.RemoveEventListener<int>(...)` **+ 立即 `_handler = null` (null-out 防 double-remove)** → 下次 transition 再派发时该 handler **不再**被触发（invokeCount == 1） — **PlayMode P5a PASS**
- [x] **AC-9 (patch v3 reformulated)** `TestSceneScopedFixture` documented pattern 全程无异常：handler null-out + `Cleanup()` 内 `if (_handler != null) RemoveEventListener(...)` null-check guard → Init → dispatch (handler invoked + null-out) → Cleanup (null-check skip) → 2nd dispatch (not invoked) 全程无 TEngine exception 抛出 — **PlayMode P5b PASS ✅ (v3 二跑 invokeCount=1, no exception)**
  - **patch v2 → v3 修订原因**：v2 原文 "双重移除（handler 内 remove + 外部 cleanup 再次 remove）不抛异常（TEngine EventMgr 语义验证）" 假设 TEngine `RemoveEventListener` 是 idempotent；PlayMode 首跑实测 TEngine **抛 exception** "Delete handle failed, not exist"。v3 把 "double-remove safe" 重新定义为 "documented null-out + null-check guard pattern safe"，更稳健且不依赖 framework idempotency 假设。这是 Type-2 子类 (c) Framework behavior assumption drift 第 1 个数据点。

### D. 性能断言

- [ ] **AC-10** (ADV) 单次完整 transition 的全部 `ISceneEvent` 方法派发总耗时 ≤ 2ms（宽松上限，含 `OnSceneLoadProgress` 多次派发；PlayMode `Stopwatch` 实测 + JSON evidence 落 `p6_dispatchMs`）— **PlayMode P6 SkipAdvisory（主 _sceneManager 在 P3 后处于 Error 不可复用；perf 测量留 backlog perf-profile spike）**

---

## Implementation Notes

*Derived from ADR-009 + ADR-027 Implementation Guidelines:*

### 1. IFadeOverlay placeholder 接口 + NoOpFadeOverlay 默认 impl

新增 `Assets/GameScripts/HotFix/GameLogic/Scene/IFadeOverlay.cs`（与 `IChapterDataProvider` 同 layer，依赖注入模式与 `RegisterChapterDataProvider` 对称）：

```csharp
namespace GameLogic
{
    /// <summary>
    /// 章节切换 fade overlay 抽象层（S2-17 Story 005 / patch v2 placeholder）。
    /// future fade story 通过 SceneManager.RegisterFadeOverlay(IFadeOverlay) 注入真实 UI impl。
    /// </summary>
    public interface IFadeOverlay
    {
        UniTask FadeOutAsync();
        UniTask FadeInAsync();
    }

    /// <summary>占位 impl — 两 method 直接 return UniTask.CompletedTask；future fade story 替换。</summary>
    public sealed class NoOpFadeOverlay : IFadeOverlay
    {
        public UniTask FadeOutAsync() => UniTask.CompletedTask;
        public UniTask FadeInAsync() => UniTask.CompletedTask;
    }
}
```

SceneManager 内部默认实例 + setter 注入（与 `_chapterDataProvider` 对称）：

```csharp
private IFadeOverlay _fadeOverlay = new NoOpFadeOverlay();

public void RegisterFadeOverlay(IFadeOverlay fadeOverlay)
{
    _fadeOverlay = fadeOverlay ?? new NoOpFadeOverlay();
}
```

### 2. SceneManager.BeginTransitionAsync — 11 步流程驱动入口

```csharp
public async UniTask BeginTransitionAsync(int targetChapterId)
{
    // S3-02 v3：fromChapterId 取自"已加载身份"字段，不是状态机目标 _currentChapterId
    int fromChapterId = _currentLoadedChapterId;

    // Step 3: Fire begin（本 story 新增 sender A）
    GameEvent.Get<ISceneEvent>().OnSceneTransitionBegin(fromChapterId, targetChapterId);

    // Step 4: Fade out（IFadeOverlay placeholder — NoOpFadeOverlay 默认 = UniTask.CompletedTask）
    await _fadeOverlay.FadeOutAsync();
    TransitionTo(SceneManagerState.Unloading);

    // Step 5-7: Cleanup 序列（S3-02 实装；内部 first-boot guard via _currentLoadedChapterId == NoChapterId）
    await UnloadCurrentChapterAsync();

    // Step 8-10: Download + Load + Activate（S3-01 实装；含 OnSceneDownloadProgress /
    //           OnSceneLoadProgress / OnSceneLoadComplete / OnSceneLoadFailed sender）
    TransitionTo(SceneManagerState.Loading);
    await LoadChapterSceneAsync(targetChapterId);

    // 失败检测：LoadChapterSceneAsync 失败路径内部已派 OnSceneLoadFailed + TransitionTo(Error)
    if (CurrentState == SceneManagerState.Error)
    {
        // OnSceneReady / OnSceneTransitionEnd 不再派发（AC-3 / AC-6）
        return;
    }

    // Step 11: Fade in + Ready + End
    TransitionTo(SceneManagerState.TransitionIn);
    GameEvent.Get<ISceneEvent>().OnSceneReady(targetChapterId);        // Story 001 AC-8 同源 sender
    await _fadeOverlay.FadeInAsync();
    GameEvent.Get<ISceneEvent>().OnSceneTransitionEnd(targetChapterId); // 本 story 新增 sender B

    TransitionTo(SceneManagerState.Idle);
    _inflightChapterId = NoChapterId;  // S3-02 propagation：使用哨兵常量
    DrainPending();
}
```

本 story 实施工作量：
- **2 行 sender**（TransitionBegin + TransitionEnd）+ 保证 success / failure 路径调用点不越界
- **新增 `IFadeOverlay` + `NoOpFadeOverlay`**（~10 行；placeholder pattern）
- **`RegisterFadeOverlay` setter**（~5 行）
- **`BeginTransitionAsync` 编排骨架**（~30 行；调用 S3-02 `UnloadCurrentChapterAsync` + S3-01 `LoadChapterSceneAsync` + state machine `TransitionTo` + `DrainPending`）
- 大部分验证工作在 PlayMode S303 spike + 端到端 ordering 断言

### 3. PlayMode spike — 测试 harness 推荐结构

`Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S303_SceneEventOrdering.cs` IDevSpike (沿 S3-01 + S3-02 模板)：

```csharp
public class S303Spike : IDevSpike { /* 创建 GameObject + 挂 S303Runtime */ }

public class S303Runtime : MonoBehaviour { /* OnGUI 报告 + Start → S303Tester.RunAllAsync */ }

public class S303Tester
{
    // 8 listener，全 log 到 Log: List<string>，dispatch 同步落顺序
    private readonly List<string> _log = new();

    public void Hook()
    {
        GameEvent.AddEventListener<int, int>(ISceneEvent_Event.OnSceneTransitionBegin,
            (from, to) => _log.Add($"Begin({from},{to})"));
        GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneUnloadBegin,
            chap => _log.Add($"UnloadBegin({chap})"));
        GameEvent.AddEventListener<float, long, long>(ISceneEvent_Event.OnSceneDownloadProgress,
            (p, d, t) => _log.Add($"DownloadProgress({p:F2})"));
        GameEvent.AddEventListener<string, float>(ISceneEvent_Event.OnSceneLoadProgress,
            (n, p) => _log.Add($"LoadProgress({n},{p:F2})"));
        GameEvent.AddEventListener<int, string>(ISceneEvent_Event.OnSceneLoadComplete,
            (c, b) => _log.Add($"LoadComplete({c})"));
        GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneReady,
            c => _log.Add($"Ready({c})"));
        GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneTransitionEnd,
            c => _log.Add($"TransitionEnd({c})"));
        GameEvent.AddEventListener<int, string>(ISceneEvent_Event.OnSceneLoadFailed,
            (c, e) => _log.Add($"LoadFailed({c})"));
    }

    // 5 case：P1 缓存命中成功 / P2 (advisory) 下载分支 / P3 失败 / P4 first-boot / P5 listener self-removal
    // JSON 落 Application.persistentDataPath/S303_Result.json
}
```

`S303` spike 跑完后用 regex / 顺序断言验证 `_log` 序列符合 AC-4..AC-7；JSON 输出 `allCorePassed` / `p1_seq_log` / `p4_firstboot_seq` / `p5_selfRemoval` / `p10_dispatchMs` 等字段。

### 4. Scene-scoped self-removal 推荐模式（patch v3 — null-out + null-check guard 强化）

**重要 framework knowledge**：TEngine `GameEvent.RemoveEventListener` **在 listener 不存在时抛 Exception** "Delete handle failed, not exist"，**不是** idempotent silent no-op（patch v3 PlayMode 实测发现）。Recommended pattern 必须用 handler null-out + 外部 cleanup null-check guard。

```csharp
public sealed class TestSceneScopedFixture
{
    private Action<int> _handler;

    public int InvokeCount { get; private set; }

    public void Init()
    {
        _handler = OnSceneUnloadBegin;
        GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneUnloadBegin, _handler);
    }

    private void OnSceneUnloadBegin(int chapterId)
    {
        InvokeCount++;
        GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneUnloadBegin, _handler);
        _handler = null; // 关键：null-out 防 double-remove；TEngine RemoveEventListener 不是 idempotent
    }

    public void Cleanup()
    {
        if (_handler != null) // 关键：null-check guard 防御 TEngine throw
        {
            GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneUnloadBegin, _handler);
            _handler = null;
        }
    }
}
```

**两点不可省**：
1. Handler 内 `_handler = null` — 防止 Cleanup() 二次 RemoveEventListener
2. Cleanup() 内 `if (_handler != null)` — 防止 OnDisable / system shutdown 路径里的 raw double-remove 抛 exception

### Scene-scoped self-removal 推荐模式

```csharp
// 任何 scene-scoped 系统的 Init() 中
private Action<int> _onUnloadBeginHandler;

public void Init()
{
    _onUnloadBeginHandler = OnSceneUnloadBegin;
    GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneUnloadBegin, _onUnloadBeginHandler);
}

private void OnSceneUnloadBegin(int chapterId)
{
    // ...释放本系统持有的 scene-scoped 资源...
    GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneUnloadBegin, _onUnloadBeginHandler);
    _onUnloadBeginHandler = null;
}
```

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: `ISceneEvent` 契约本身、`OnRequestSceneChange` / `OnSceneReady` sender + listener
- Story 002: `OnSceneDownloadProgress` / `OnSceneLoadProgress` / `OnSceneLoadComplete` / `OnSceneLoadFailed` sender，11 步流程的 Load/Download 段
- Story 003: `OnSceneUnloadBegin` sender + cleanup 序列
- Story 004: mutex + in-flight 去重
- Story 006: Luban 章节 ↔ 场景名解析

---

## QA Test Cases

映射到 `S303_SceneEventOrdering.cs` PlayMode spike 5 cases (P1-P5) + ADVISORY P6 perf:

- **P1 / AC-1 / AC-2 / AC-4** Success cache hit (`SP011_SceneA` chapter id=201 → `SP011_SceneB` chapter id=202)
  - Given: observer hooked；`_sceneManager.LoadChapterSceneAsync(201)` 预热成 `_currentLoadedChapterId == 201`；`_fadeOverlay = NoOpFadeOverlay()`（默认）
  - When: 驱动 `await _sceneManager.BeginTransitionAsync(202)`
  - Then: `_log` 首段 `["Begin(201,202)", "UnloadBegin(201)"]`；中段含 ≥1 `LoadProgress(...)`；末段 `["LoadComplete(202)", "Ready(202)", "TransitionEnd(202)"]`；`Ready(202)` 严格先于 `TransitionEnd(202)`；`Begin(...)` 严格先于其他所有
  - Listener counts (post-P1)：`OnSceneTransitionBegin = 1`, `OnSceneUnloadBegin = 1`, `OnSceneLoadComplete = 1`, `OnSceneReady = 1`, `OnSceneTransitionEnd = 1`

- **P2 (ADVISORY) / AC-5** Download branch — `OnSceneDownloadProgress` 全部先于任何 `OnSceneLoadProgress`
  - dev EditorSimulateMode 缓存命中常导致 `TotalDownloadCount==0`；如 `OnSceneDownloadProgress count == 0`，标 SkipAdvisory（与 S3-01 P3 同模式）
  - Given/When/Then 略；如有 download 走 download → load 顺序断言

- **P3 / AC-3 / AC-6** Failure path (chapter id=999 invalid; retry exhausts → OnSceneLoadFailed)
  - Given: provider 返 `ChapterData(999, "Chapter_999_NotInManifest", ...)`；observer hooked
  - When: 驱动 `await _sceneManager.BeginTransitionAsync(999)`
  - Then: `_log` 含 `Begin(prev, 999)` + `UnloadBegin(prev)`（如 prev != NoChapterId）+ `LoadFailed(999, ...)`；**不含** `LoadComplete(999)` / `Ready(999)` / `TransitionEnd(999)`；`SceneManager.CurrentState == SceneManagerState.Error`

- **P4 / AC-7** First-boot (`_currentLoadedChapterId == NoChapterId`)
  - Given: fresh `SceneManager` + `Init()` + provider；尚未 load 任何 chapter
  - When: 驱动 `await _sceneManager.BeginTransitionAsync(201)`
  - Then: `_log` 首元素 `Begin(-1, 201)`（`-1` = `NoChapterId`）；**不含** `UnloadBegin(...)`（S3-02 first-boot guard 短路）；末段 `LoadComplete / Ready / TransitionEnd` 正常

- **P5a / AC-8** Scene-scoped listener self-removal (patch v3 split)
  - Given: handler 内 `RemoveEventListener<int>(OnSceneUnloadBegin, handler)` 闭包；driver 触发 `OnSceneUnloadBegin(101)`
  - When: 再触发 `OnSceneUnloadBegin(102)`
  - Then: handler 第二次不触发（invokeCount == 1）

- **P5b / AC-9** NullOutGuardPattern (patch v3 split — replaces v2 raw double-remove)
  - Given: `TestSceneScopedFixture` (documented pattern: handler null-out + Cleanup() 内 null-check guard); `fixture.Init()` 订阅
  - When: driver 触发 `OnSceneUnloadBegin(101)` → fixture 调 `Cleanup()` (null-check skip) → driver 再触发 `OnSceneUnloadBegin(102)`
  - Then: 全程**无 exception 抛出**；`fixture.InvokeCount == 1`；`fixture.HandlerIsNull() == true`（post 1st dispatch）
  - **Counter-example (documented but not tested)**: raw double-remove (handler 内 remove + 外部 cleanup raw RemoveEventListener) 会抛 TEngine `Delete handle failed, not exist` — 此即 framework behavior fact，禁止 raw double-remove 模式（Control Manifest Forbidden 规则）

- **P6 (ADVISORY) / AC-10** Perf ≤ 2ms
  - Given: observer hooked；`Stopwatch` 包裹完整 BeginTransitionAsync
  - When: `BeginTransitionAsync(202)` 完成
  - Then: `stopwatch.ElapsedMilliseconds <= 2`（PlayMode dev 实测；NoOpFadeOverlay 几乎 0 开销；如不达标先标 ADVISORY，实机分析）

---

## Test Evidence

**Story Type**: Integration（PlayMode-only — D1=[b] inherited from S3-01）
**Rationale**: BeginTransitionAsync 11 步内嵌 GameModule.Scene.LoadSceneAsync + Resources.UnloadUnusedAssets + GameEvent dispatch chain，全部依赖 Unity runtime；`GameModule.Scene` 静态 facade 不可 stub；EditMode 不可达。**与 SP-011 / S3-01 / S3-02 同 IDevSpike + OnGUI + JSON 落盘模式**。

**Required evidence**:
- `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S303_SceneEventOrdering.cs` — PlayMode spike (6 case + 2 ADVISORY — patch v3 拆 P5)
  - **P1** Success cache hit — 完整 7 sender 派发顺序断言（Begin → UnloadBegin → LoadProgress* → LoadComplete → Ready → TransitionEnd）；listener counts 验证 ✅ PASS
  - **P2** ADVISORY — Download branch（缓存命中常 SkipAdvisory）
  - **P3** Failure path — invalid chapter retry exhaust → OnSceneLoadFailed + 后续 sender 不派；CurrentState == Error ✅ PASS
  - **P4** First-boot — _currentLoadedChapterId == NoChapterId 路径；UnloadBegin 不派 ✅ PASS
  - **P5a** Self-removal — handler 内 RemoveEventListener；2nd dispatch 不触发 invokeCount == 1 ✅ PASS (post-v3)
  - **P5b** NullOutGuardPattern — `TestSceneScopedFixture` documented pattern (null-out + null-check guard) 全程无异常 + invokeCount == 1 ✅ PASS (待 v3 二跑验证)
  - **P6** ADVISORY — Perf ≤ 2ms（主 _sceneManager 在 P3 后 Error 不可复用）
  - **JSON output**: `Application.persistentDataPath/S303_Result.json`（schema patch v3 增 P5a/P5b 字段：`p5a_passed` / `p5a_invokeCount` / `p5b_passed` / `p5b_invokeCount` 替代 v2 单 `p5_*`）
- `production/qa/grep-no-fantasy-api-scene-events-2026-04-30.md` — grep evidence (S3-03 specific)
  - 0 hit `_currentChapterId\s*==\s*-1` in `HotFix/GameLogic/Scene/`（应改 `_currentLoadedChapterId == NoChapterId`）
  - 0 hit `await CleanupAsync\(` in `HotFix/GameLogic/Scene/`（应改 UnloadCurrentChapterAsync()）
  - 0 hit `bool\s+\w+\s*=\s*await\s+LoadChapterSceneAsync` in `HotFix/GameLogic/Scene/`（fantasy 返回类型）
  - 0 hit `_fadeOverlay\.FadeOutAsync\(\)` 字面 patern with no IFadeOverlay declaration（即必须有 IFadeOverlay interface 落地）
  - ≥1 hit `RegisterFadeOverlay` 在 `Scene/SceneManager.cs`（DI setter 必落）
  - ≥1 hit `OnSceneTransitionBegin\(` sender 在 `Scene/SceneManager.cs`（AC-1）
  - ≥1 hit `OnSceneTransitionEnd\(` sender 在 `Scene/SceneManager.cs`（AC-2）

**Smoke test mapping**: 本 story spike 加入 sprint smoke check spike batch（`/smoke-check` 命中 spike list）；GameApp 注册 S303Spike 时保持 SP-011 + S301 + S302 注释关闭（type-3 race 防御；单 spike 模式）。

**Core PASS criteria**: P1 + P3 + P4 + P5a + P5b 全 PASS（patch v3 — 5 cores）；P2 / P6 ADVISORY；`allCorePassed: true` + `lastError: ""`。

**Status (patch v3 — final)**: [x] **Done 2026-04-30 dusk** — v3 PlayMode 二跑 CORE 5/5 PASS (P1/P3/P4/P5a/P5b ✅ + P2/P6 SkipAdvisory)；`allCorePassed: true`；`lastError: ""`。/story-done 闭环。

---

## ADR-029 Phase 1.5 Readiness Verdict (2026-04-30 dusk)

| Rule | Verdict | 数据 |
|------|---------|------|
| **R1** Per-event listener / sender pairing (ADR-027) | ✅ PASS | `ISceneEvent.OnSceneTransitionBegin(int, int)` + `OnSceneTransitionEnd(int)` 2 sender 已在 `IEvent/ISceneEvent.cs` 冻结（Story 001 SG-gen）；本 story 任务为实装 sender；spike harness 用 add/remove pairing 模式 |
| **R2** Cross-component API existence grep | ❌→✅ PASS (post-patch v2) | 8 处 drift 修订完成：(1) `_currentChapterId` → `_currentLoadedChapterId` (S3-02 v3 cross-method protocol)；(2) `CleanupAsync(int)` → `UnloadCurrentChapterAsync()` (S3-02 实装方法)；(3) `bool ok = await LoadChapterSceneAsync` → `if (CurrentState == Error)` (S3-01 fail detection pattern)；(4) `_currentChapterId == -1` (3 处) → `_currentLoadedChapterId == NoChapterId`；(5) `_inflightChapterId = -1` → `_inflightChapterId = NoChapterId`；(6) `_fadeOverlay.FadeXxxAsync` → IFadeOverlay placeholder + NoOpFadeOverlay (decision [b])；(7) EditMode test path → S303_SceneEventOrdering.cs PlayMode spike；(8) AC-7 first-boot 字面 -1 → NoChapterId 哨兵 |
| **R3** Stub data type constructor | ✅ PASS | 无 stub data type 引用（spike harness 全用 `Action<int>` lambda + `_log: List<string>` ） |
| Type-2 / Type-3 风险 | LOW | Type-2 cross-method protocol drift 已在 S3-02 v3 修复（`_currentLoadedChapterId` 配对字段）；本 story 直接复用，无新增风险。Type-3 multi-spike race：本 story 沿 S3-02 单 spike 注册模式（GameApp 仅挂 S303；SP011/S301/S302 全部注释）。|

**Drift revision time data point (S3-03 / 第 4 条)**：估约 ~15 min（vs S3-01 baseline 18 / S3-02 Phase 1.5 Type-4-only 8 / S3-02 R3 Type-2 21）。**慢 vs S3-02 Phase 1.5 ~2x 的 root cause**：S3-01 D5 propagation 实战首测 scope 仅含 12 active sprint files，未覆盖 ready backlog 故未 cover story-005；同时本 story 撞 Type-1 fantasy（fade infra 未实装）+ D1=[b] PlayMode-only test path propagation。

**ADR-029 V2 候选 #6 触发**：本 story 是 propagation scope 漏洞的第一个数据点。V2 应明确 `propagation scope` rule = **"覆盖所有 `Status: Ready` 及以上的 stories（不只 active sprint），D-level 决策落地后立即扫修订"**；如 V2 落地，story-005 的 6 处 Type-4 propagation drift 应 0 复发（仅留 2 处 Type-1 fade fantasy + 测试路径 pivot ~5min）。

**ADR-029 V2 候选 #7 触发 (patch v3 — Type-2 子类 (c))**：PlayMode P5 FAIL 暴露 **Framework behavior assumption drift** — patch v2 假设 `GameEvent.RemoveEventListener` 是 idempotent，实测 TEngine 抛 exception。这是 R3-only 可发现的新 drift 形态（grep 0 hit；仅 PlayMode probe 暴露）。V2 候选 #7 = `R3 PlayMode probe 必须明确包括 framework boundary behavior 测试`（验证 framework method 在 listener-not-exists / null-state / over-limit 等异常路径下的实际行为，不只是业务 cross-method state 协议）。

---

## Dependencies

- Depends on: Story 001 (ISceneEvent 契约 + OnRequestSceneChange/OnSceneReady sender), Story 002 (load senders), Story 003 (cleanup sender) — all must be DONE ✅
  - S3-01 (Story 002) DONE 2026-04-30 morning ✅
  - S3-02 (Story 003) DONE 2026-04-30 dusk ✅ (含 v3 Type-2 cross-method protocol fix — 本 story 依赖 `_currentLoadedChapterId` 字段)
- Unlocks: 下游系统（Audio、UI、ChapterState 等）可以基于完整端到端事件序列做集成；future fade story 通过 `RegisterFadeOverlay(IFadeOverlay)` 替换 NoOpFadeOverlay 默认 impl
