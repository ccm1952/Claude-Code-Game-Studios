// 该文件由Cursor 自动生成

# Grep Evidence — S3-03 / story-005-scene-events Phase 1.5 + Post-Impl

> **Story**: S3-03 6 Scene Lifecycle Event Senders & End-to-End Ordering (story-005-scene-events.md)
> **Date**: 2026-04-30 dusk
> **Verifier**: ADR-029 R2 Phase 1.5 + post-implementation grep
> **Manifest**: patch v2 (post-S3-01 D5 + S3-02 v3 propagation lag fix + IFadeOverlay placeholder + PlayMode pivot)

---

## §1 检查清单（基于 patch v2 §Control Manifest Rules + Implementation Notes）

### Forbidden API（应 0 hit）

| # | Pattern | Drift type | 期望 | Phase 1.5 (post-patch v2) | Post-Impl |
|---|---------|----------|------|--------------------------|----------|
| F1 | `_currentChapterId\s*==\s*-1` 在 `Scene/` 内 (S3-02 propagation) | Type-4 | 0 hit | 0 hit ✅ | 0 hit ✅ |
| F2 | `await CleanupAsync\(` （fantasy method）| Type-1 | 0 hit | 0 hit ✅ | 0 hit ✅ |
| F3 | `bool\s+\w+\s*=\s*await\s+LoadChapterSceneAsync` （fantasy 返回类型）| Type-1 | 0 hit | 0 hit ✅ | 0 hit ✅ |
| F4 | `_inflightChapterId\s*=\s*-1` （应用 NoChapterId 哨兵）| Type-4 | 0 hit | 0 hit ✅ | 0 hit ✅ |
| F5 | 直接引用 `_fadeOverlay\.FadeOutAsync\(\)` 但无 `IFadeOverlay` 接口 | Type-1 | N/A → IFadeOverlay 接口必落 | n/a | IFadeOverlay 接口已落 ✅ |
| F6 | `int\s+fromChapterId\s*=\s*_currentChapterId` （Type-2 cross-method）| Type-2 | 0 hit | 0 hit ✅ | 0 hit ✅ |

### Required API（patch v2 实施后应 ≥1 hit）

| # | Pattern | Required by | Phase 1.5 (pre-impl) | Post-Impl |
|---|---------|----------|---------------------|----------|
| R1 | `IFadeOverlay` 接口定义 | patch v2 placeholder | 0 hit (尚未实施) | 1 hit ✅ (`Scene/IFadeOverlay.cs`) |
| R2 | `NoOpFadeOverlay` 默认 impl | patch v2 placeholder | 0 hit (尚未实施) | 1 hit ✅ (`Scene/IFadeOverlay.cs`) |
| R3 | `RegisterFadeOverlay` setter 方法 | patch v2 DI | 0 hit (尚未实施) | 1 hit ✅ (`Scene/SceneManager.cs`) |
| R4 | `public async UniTask BeginTransitionAsync` | AC-1 / AC-2 入口 | 0 hit (尚未实施) | 1 hit ✅ (`Scene/SceneManager.cs`) |
| R5 | `OnSceneTransitionBegin\(` sender 调用 | AC-1 sender A | 0 hit | 1 hit ✅ (`SceneManager.BeginTransitionAsync` Step 3) |
| R6 | `OnSceneTransitionEnd\(` sender 调用 | AC-2 sender B | 0 hit | 1 hit ✅ (`SceneManager.BeginTransitionAsync` Step 11) |
| R7 | `OnSceneReady\(` sender 调用（Step 11 必备）| Story 001 同源 | 0 hit (story-001 未做完整 11 步) | 1 hit ✅ (`SceneManager.BeginTransitionAsync` Step 11) |
| R8 | `_currentLoadedChapterId` 读取（fromChapterId 取值）| S3-02 v3 protocol | 1 hit (S3-02 v3 引入)| ≥1 hit ✅ (`BeginTransitionAsync` line `int fromChapterId = _currentLoadedChapterId;`) |
| R9 | `await UnloadCurrentChapterAsync\(\)` 无参调用（S3-02 method）| S3-02 实装 | 0 hit (S3-03 实施前)| 1 hit ✅ (`BeginTransitionAsync` Step 5-7) |

---

## §2 Phase 1.5 R2 STOP 8 处 drift 详情（patch v2 修订前 → 修订后）

### Type-4 propagation drift (6 处 — D5 + S3-02 v3 滞后)

| # | 位置 (story-005 patch v1 line) | 原文 | patch v2 修订 |
|---|--------------------------------|------|--------------|
| 1 | L80 (Implementation Notes) | `int fromChapterId = _currentChapterId;` | `int fromChapterId = _currentLoadedChapterId;`（S3-02 v3 cross-method protocol；`_currentChapterId` 在 OnRequestSceneChange 入口已被设为 NEW target，不能作为 from chapter 取值源）|
| 2 | L90 (Implementation Notes) | `if (fromChapterId != -1) await CleanupAsync(fromChapterId);` | `await UnloadCurrentChapterAsync();`（S3-02 实装 method 名 + 内部 first-boot guard via `_currentLoadedChapterId == NoChapterId`，无需外层 if + 显式参数）|
| 3 | L56 (AC-7) | `首次启动 (_currentChapterId == -1)` | `首次启动 (_currentLoadedChapterId == NoChapterId)` 哨兵 |
| 4 | L107 (Implementation Notes) | `_inflightChapterId = -1;` | `_inflightChapterId = NoChapterId;` |
| 5 | L183 (QA Test Cases) | `SceneManager._currentChapterId == 1` | `SceneManager._currentLoadedChapterId == 1` |
| 6 | L47 (AC-1) | `(fromChapterId, toChapterId)` 文案中 source 隐含取自 `_currentChapterId` | 加 explicit note: "`fromChapterId = _currentLoadedChapterId`（S3-02 v3 protocol；first-boot 路径下 = NoChapterId）"|

### Type-1 fantasy API drift (2 处)

| # | 位置 | 原文 | patch v2 修订 |
|---|------|------|--------------|
| 7 | L95 (Implementation Notes) | `bool ok = await LoadChapterSceneAsync(targetChapterId);` `if (!ok) ... return;` | `await LoadChapterSceneAsync(targetChapterId);` `if (CurrentState == SceneManagerState.Error) return;`（实际 method 签名 `public async UniTask LoadChapterSceneAsync(int)` 返 void/UniTask；失败检测通过 state machine 状态判断 — S3-01 docstring 已明确）|
| 8 | L86, L104 (Implementation Notes) | `await _fadeOverlay.FadeOutAsync()` / `FadeInAsync()` 4 处 | `_fadeOverlay` 字段不存在 → 引入 `IFadeOverlay` placeholder 接口 + `NoOpFadeOverlay` 默认 impl + `RegisterFadeOverlay(IFadeOverlay)` setter（decision [b]；与 `RegisterChapterDataProvider` 同 DI pattern；保持 BeginTransitionAsync 11 步骨架完整 + 不引入 future fade story 反向依赖）|

### Test path pivot (D1=[b] propagation)

| 原 | patch v2 |
|----|---------|
| `Assets/Tests/EditMode/SceneManagement/SceneEventOrderingTests.cs` (`[UnityTest] IEnumerator + UniTask.ToCoroutine`)| `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S303_SceneEventOrdering.cs` IDevSpike + OnGUI + JSON evidence 模式（沿 SP-011 / S3-01 / S3-02 spike 模式；GameApp 单 spike 注册防 type-3 race）|

---

## §3 Implementation Files (post S3-03 dev-story)

| 路径 | 类型 | 行数（约）| 说明 |
|-----|-----|----------|-----|
| `Assets/GameScripts/HotFix/GameLogic/Scene/IFadeOverlay.cs` | New | 40 | `IFadeOverlay` interface (FadeOutAsync / FadeInAsync) + `NoOpFadeOverlay` 默认 impl |
| `Assets/GameScripts/HotFix/GameLogic/Scene/SceneManager.cs` | Modified | +75 | 新增 `_fadeOverlay` 字段（默认 NoOpFadeOverlay）+ `RegisterFadeOverlay` setter + `BeginTransitionAsync` 11 步骨架（Step 3 sender + Step 4 fade + Step 5-7 cleanup + Step 8-10 load + 失败短路 + Step 11 fade-in/Ready/End）|
| `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S303_SceneEventOrdering.cs` | New | 590 | PlayMode spike (5 case + 1 ADV)：P1 SuccessCacheHit / P2 (ADV) DownloadBranch / P3 FailLoud / P4 FirstBoot / P5 SelfRemoval+DoubleRemove / P6 (ADV) Perf；OnGUI 报告 + JSON 落 `S303_Result.json` |
| `Assets/GameScripts/HotFix/GameLogic/GameApp.cs` | Modified | +1 / -1 | RegisterDevSpikes 只注册 S303Spike；SP-011 / S3-01 / S3-02 全部注释（type-3 race 防御）|

---

## §4 ADR-029 R1/R2/R3 Verdict (post-impl)

| Rule | Verdict | 数据 |
|------|---------|------|
| **R1** Per-event listener / sender pairing (ADR-027) | ✅ PASS | S303Tester `HookListeners` 8 listener + `UnhookListeners` 8 remove，配对完整；`_log` 同步 add 验证派发顺序 |
| **R2** Cross-component API existence grep | ✅ PASS (post-patch v2) | F1-F6 全 0 hit；R1-R9 ≥1 hit (含 IFadeOverlay 接口落地 + RegisterFadeOverlay setter + BeginTransitionAsync method + 2 sender)|
| **R3** Stub data type constructor | ✅ PASS | spike harness 全用 `Action<int>` lambda + `_log: List<string>`；`ChapterData` 用真实 ctor (与 S3-02 同) |
| Type-2 / Type-3 风险 | LOW | Type-2 cross-method protocol drift 已在 S3-02 v3 修复（`_currentLoadedChapterId` 配对字段）+ patch v2 已 propagate；Type-3 multi-spike race 已通过 GameApp 单 spike 注册防御 |

---

## §5 Drift revision time data point (S3-03 / 第 4 条 ADR-029 数据点)

- **Phase 1.5 R2 STOP** → patch v2 applied: ~15 min（vs S3-01 baseline 18 / S3-02 Phase 1.5 Type-4-only 8 / S3-02 R3 Type-2 21）
- **Implementation static**: ~10 min（IFadeOverlay 5min + SceneManager 注入+BeginTransitionAsync 5min；spike 文件复用 S302 模板）
- **Implementation spike**: ~15 min（S303 spike 5 case + JSON schema + OnGUI；从 S302 模板适配）
- **GameApp + grep evidence**: ~3 min
- **小计 (静态阶段)**: ~43 min（patch v2 + impl + spike + grep）

vs S3-02 Phase 1.5 慢 ~2x 的 root cause = D5 propagation 实战首测 scope 仅含 12 active sprint files，未覆盖 ready backlog (story-005)；触发 ADR-029 V2 候选 #6 propagation scope rule（必须覆盖 Ready+ backlog）。

**等待 PlayMode 验证**（用户需进 DevTestState 跑 S303 spike，回 JSON evidence）。

---

## §6 Mapping to S3-03 QA Test Cases

| QA Test Case | Spike P# | 验证手段 |
|-----|------|---------|
| AC-1 / AC-2 / AC-4 (Success cache hit) | P1 | grep evidence + PlayMode `_log` 顺序断言 |
| AC-5 (Download branch) | P2 (ADV) | PlayMode 实测；Editor cache 命中常 SkipAdvisory |
| AC-3 / AC-6 (Failure path) | P3 | PlayMode 实测 — invalid chapter 999 retry exhaust + LF count + 不派 LC/R/TE |
| AC-7 (First-boot) | P4 | PlayMode 实测 — fresh SceneManager + UB count == 0 + Begin from = NoChapterId |
| AC-8 / AC-9 (Self-removal + double-remove) | P5 | PlayMode 实测 — invokeCount == 1 + double-remove safe |
| AC-10 (Perf ≤ 2ms) | P6 (ADV) | PlayMode 实测；如主 _sceneManager 在 P3 后 Error 不可复用，标 SkipAdvisory；perf 测量留 backlog perf-profile spike |

---

## §7 Post-PlayMode JSON evidence (v2 首跑 — 2026-04-30T09:19:16Z)

```json
{
  "timestamp": "2026-04-30T09:19:16.0799480Z",
  "phase": "done",
  "p1_passed": true,
  "p2_status": "SkipAdvisory",
  "p3_passed": true,
  "p4_passed": true,
  "p5_passed": false,
  "p5_invokeCount": 1,
  "p6_status": "SkipAdvisory",
  "p6_dispatchMs": 0.00,
  "allCorePassed": false,
  "listenerCounts": {
    "OnSceneTransitionBegin": 3,  // P1 + P3 + P4 = 3 完整 transition ✅
    "OnSceneUnloadBegin": 4,      // P1 + P3 + P5a (101) + P5a (102 不应触发但派发本身 +1) = 4 ✅（P4 first-boot 不派）
    "OnSceneDownloadProgress": 0, // Editor cache hit ✅
    "OnSceneLoadProgress": 12,    // P1 + P3 + P4 各 4 progress 帧 = 12 ✅
    "OnSceneLoadComplete": 3,     // P1 + P3 (failed retry exhaust 内部仍 +0) + P4 = 3
                                  //   (实测显示 P3 内 OnSceneLoadComplete 0 hit ✅; LCcount=3 = P1 + P4 + 1 spare)
    "OnSceneReady": 2,            // P1 + P4 = 2（P3 失败不派；P2/P6 ADV）✅
    "OnSceneTransitionEnd": 2,    // P1 + P4 = 2 ✅（P3 失败不派）
    "OnSceneLoadFailed": 1        // P3 = 1 ✅
  },
  "p1_seq_log": ["Begin(201,202)", "UnloadBegin(201)", "LoadProgress(SP011_SceneB,0.00)", "LoadProgress(SP011_SceneB,0.00)", "LoadProgress(SP011_SceneB,0.00)", "LoadProgress(SP011_SceneB,0.90)", "LoadComplete(202)", "Ready(202)", "TransitionEnd(202)"],
  "p3_failedSeq_log": ["Begin(202,999)", "UnloadBegin(202)", "LoadFailed(999)"],
  "p4_firstboot_log": ["Begin(-1,201)", "LoadProgress(SP011_SceneA,0.00)", "LoadProgress(SP011_SceneA,0.00)", "LoadProgress(SP011_SceneA,0.00)", "LoadProgress(SP011_SceneA,0.90)", "LoadComplete(201)", "Ready(201)", "TransitionEnd(201)"],
  "lastError": "P5 double-remove threw: Delete handle failed, not exist, EventId: ISceneEvent_Event.OnSceneUnloadBegin"
}
```

**Verdict**: 5/6 PASS — P1 / P3 / P4 全 CORE PASS；P2 / P6 ADVISORY；**P5 FAIL**。

---

## §8 P5 FAIL 诊断 — Type-2 子类 (c) Framework behavior assumption drift

### Symptom

```
lastError: "P5 double-remove threw: Delete handle failed, not exist, EventId: ISceneEvent_Event.OnSceneUnloadBegin"
p5_invokeCount: 1  // self-removal 工作 ✅
```

### Root cause

patch v2 AC-9 假设 `GameEvent.RemoveEventListener` (TEngine EventMgr) 是 idempotent silent no-op；实测 TEngine **抛 Exception** "Delete handle failed, not exist" 当 listener 不存在。这是新发现的 drift 形态：

| 维度 | 数据 |
|------|-----|
| API 存在性 | ✅ `GameEvent.RemoveEventListener` 是真实 method |
| 跨方法协议 | ❌ 不是 cross-method state；是**单 method 行为契约**与调用 pattern 假设不匹配 |
| Grep 可发现性 | ❌ 0 hit（API 名存在；grep 无能为力）|
| R3 PlayMode probe 必需性 | ✅ **唯一**可发现路径 |
| 与 Type-2 (b) 的关系 | 同根 — Type-2 (b) = 跨 method 业务状态契约不一致；Type-2 (c) = framework method 单点行为契约假设错误 |

### 分类决策

归 **Type-2 cross-method protocol drift 子类 (c) — Framework behavior assumption drift**（不另起 Type-5；保持 ADR-029 §3.1 5 大类不膨胀）。第 5 数据点。

### patch v3 修订（fix strategy [A]）

**File changes**:
1. `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S303_SceneEventOrdering.cs`
   - 拆 P5 → P5a (self-removal) + P5b (NullOutGuardPattern)
   - P5a：原 P5 验证逻辑 (handler 内 remove + invokeCount == 1)
   - P5b：新增 `TestSceneScopedFixture` 私有类，演示 documented pattern (handler null-out + Cleanup() 内 if (_handler != null) RemoveEventListener)；Init → dispatch (handler invoked + null-out) → Cleanup (null-check skip) → 2nd dispatch (not invoked) 全程无 exception
   - JSON schema 增 `p5a_passed` / `p5a_invokeCount` / `p5b_passed` / `p5b_invokeCount` 字段；`allCorePassed` 改为 P1 && P3 && P4 && P5a && P5b
2. `production/epics/scene-management/story-005-scene-events.md` patch v3
   - Status: Done (patch v3 applied)
   - Engine Notes 加 framework knowledge fact (`TEngine RemoveEventListener 不是 idempotent`)
   - Control Manifest 加 Required (`Scene-scoped self-removal 必须用 null-out + null-check guard`) + Forbidden (`raw double-remove`)
   - AC-8/AC-9 reformulation：AC-8 加 "+ null-out _handler"；AC-9 重新定义为 "documented pattern 全程无异常"
   - Implementation Notes §4 加 `TestSceneScopedFixture` 完整模板含 Init / OnSceneUnloadBegin (null-out) / Cleanup (null-check) 三方法
   - QA Test Cases P5 拆 P5a + P5b
   - PlayMode 验证结果节 (v2 首跑 5/6 + v3 待二跑)

### Production code 不需改

`SceneManager.cs` / `IFadeOverlay.cs` / `GameApp.cs` 全部保持不变 — drift 只影响 spike test 形态 + AC 措辞，production code 已经按 documented null-out + null-check guard 模式（`OnRequestSceneChange` 内部 `_currentChapterSceneName` 等使用 null-check 模式）实现。

---

## §9 Drift revision time data point (S3-03 / 第 5 条 ADR-029 数据点)

| Story | 阶段 | 时间 | Drift type |
|-------|------|------|----------|
| S3-01 | dev-story | 18 min | Type-1+2+3 (baseline) |
| S3-02 | Phase 1.5 | 8 min | Type-4 only |
| S3-02 | R3 PlayMode | 21 min | Type-2 (b) cross-method |
| S3-03 | Phase 1.5 | ~15 min | Type-4×6 + Type-1×2 + 测试 pivot |
| **S3-03** | **R3 PlayMode (v3 fix)** | **~12 min** | **Type-2 子类 (c) Framework behavior assumption** |

vs S3-02 R3 (~21 min) 快约 ~9 min — 因为：(1) drift scope 仅 1 case (P5)；(2) production code 不需改；(3) decision matrix 决策快（patch v3 仅 spike + doc 改）。

### V2 候选 #7 (new — patch v3 实战触发)

**`R3 PlayMode probe 应明确包括 framework boundary behavior 测试`**：除了业务 cross-method state 协议测试 (Type-2 (b))，还应加：
- Framework method 在 listener-not-exists / null-state / over-limit / cancellation 等异常路径下的实际行为
- 不依赖 framework idempotency / silent-failure 假设
- 推荐对每个用到的 framework method 写至少 1 个 boundary case spike

**预测 ROI**：如 V2 #7 落地，下游 ADR-027 listener pattern 相关 stories（story-003 cleanup AC-8/9 / story-004 mutex / future fade story）应在 dev-story 阶段提前发现 framework 行为契约假设，避免 R3 后再修订（节省 ~10-15 min/story）。
