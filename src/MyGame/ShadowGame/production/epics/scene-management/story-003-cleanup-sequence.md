// 该文件由Cursor 自动生成

# Story 003: Mandatory Cleanup Sequence

> **Epic**: Scene Management
> **Status**: Done (2026-04-30 dusk PlayMode CORE PASSED — patch v3 Type-2 fix verified)
> **Layer**: Core
> **Type**: Integration (PlayMode-only — D1=[b] inherited from S3-01)
> **Manifest Version**: 2026-04-30 patch v3 (R3 PlayMode runtime probe 暴露 Type-2 drift — Load/Unload state lifecycle 协议不一致；引入 `_currentLoadedChapterId` 配对字段)
>
> **Revision note (2026-04-23)**: 原文使用 ADR-006 `Evt_SceneUnloadBegin` + `SceneUnloadBeginPayload` 协议；已迁移到 ADR-027 `ISceneEvent.OnSceneUnloadBegin(int chapterId)` 接口方法（签名由 Story 001 冻结）。scene-scoped listener 自移除约束沿用（现语义：listener 在收到 `OnSceneUnloadBegin` 时调 `GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneUnloadBegin, handler)`）。
>
> **Revision note (2026-04-30 patch v2 — ADR-029 R2 STOP)**: S3-01 D5=[X] 决定 SceneManager 不缓存 `SceneHandle` / `Scene` 对象，改用 `_currentChapterSceneName: string` (YooAsset location)。本 story 原文 4 处 fantasy API drift 修订：
> 1. `_currentSceneHandle` (SceneHandle ref) → `_currentChapterSceneName` (string)
> 2. `GameModule.Resource.UnloadSceneAsync(_currentSceneHandle)` → `GameModule.Scene.UnloadAsync(_currentChapterSceneName)` (实际 framework wrapper, `SceneModule.cs:312`)
> 3. `_currentSceneHandle = null` → `ClearCurrentChapterSceneName()` (S3-01 暴露的 internal setter, `SceneManager.cs:463`)
> 4. §Test Evidence: `Tests/EditMode/SceneManagement/CleanupSequenceTests.cs` → S3-01 D1=[b] 决定 Scene 操作 PlayMode-only；本 story pivot 到 `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S302_CleanupSequence.cs` IDevSpike 模式（同 S301）。
>
> 顺手修订：`_currentChapterId == -1` 字面量 → `_currentChapterId == NoChapterId` 哨兵常量（`SceneManager.cs:59,85`）。
>
> ADR-029 drift revision data point: 此次 4 处 type-1 fantasy API + Test Evidence pivot 估约 8 min（baseline S3-01 = 18 min；预期 ≤5min Type-1 + 3min pivot 注脚），第 2 条数据点。
>
> **Revision note (2026-04-30 dusk patch v3 — ADR-029 R3 PlayMode runtime probe 暴露 Type-2 drift)**: PlayMode S302 spike 首次实跑 6/6 cases FAIL，root cause = `LoadChapterSceneAsync` 与 `UnloadCurrentChapterAsync` 对 "current chapter" 状态语义不一致：
> 1. `LoadChapterSceneAsync` 仅 set `_currentChapterSceneName`（D5 cache），不 set `_currentChapterId`
> 2. `_currentChapterId` 仅由 `OnRequestSceneChange` 状态机入口 set，spike 直调路径下永留 `NoChapterId`
> 3. `UnloadCurrentChapterAsync` 守卫 `if (_currentChapterId == NoChapterId || ...)` 永真 → silent return；scene 永不被卸；listener 永不 fire
> 4. 同时暴露生产 driver 路径 bug：driver 通过 `OnRequestSceneChange` 进入时 `_currentChapterId` 已是 NEW target，sender `OnSceneUnloadBegin(_currentChapterId)` 派错 chapter id（应派 OLD）
>
> **v3 修复（SceneManager.cs）**：
> 1. 引入 `private int _currentLoadedChapterId = NoChapterId` 字段，与 `_currentChapterSceneName` 配对作为"已加载身份"单一事实源
> 2. `LoadChapterSceneAsync` 成功路径同步 set 两字段（atomic 配对赋值）
> 3. `ClearCurrentChapterSceneName()` 同步 reset 两字段（atomic 配对清空）
> 4. `UnloadCurrentChapterAsync` guard 检 `_currentLoadedChapterId`，sender 派 `_currentLoadedChapterId`
> 5. 新增 `public int CurrentLoadedChapterIdForTest` 测试 getter
>
> **ADR-029 第 3 数据点**：Type-2 (cross-method protocol) drift 修订时间约 15 min（root cause 诊断 6min + 决策矩阵 4min + 字段 + 4 处更新 + doc 5min）；PlayMode 首跑暴露成本 ~6 min；总 R3 cycle 21 min。**关键 insight**：R1/R2 grep 不能发现 cross-method state lifecycle 协议不一致，**R3 PlayMode probe 是 mandatory**，不是 optional —— 留作 ADR-029 V2 candidate。

## Context

**GDD**: `design/gdd/scene-management.md`
**Requirement**: `TR-scene-016`, `TR-scene-017`, `TR-scene-011`
*(UnloadUnusedAssets + GC.Collect between transitions; SceneHandle reference retention; memory leak detection)*

**ADR Governing Implementation**: ADR-009: Scene Lifecycle + ADR-005: YooAsset Resource Lifecycle + ADR-027: GameEvent Interface Protocol
**ADR Decision Summary**: A mandatory 4-step cleanup sequence (notify → release handles → unload scene → GC) must run between every chapter unload and next chapter load. No step may be skipped. This is the mechanism that prevents asset accumulation on mobile devices.

**Engine**: Unity 2022.3.62f2 LTS + YooAsset 2.3.17 + UniTask 2.5.10 | **Risk**: MEDIUM
**Engine Notes**:
- `Resources.UnloadUnusedAssets()` 返 `AsyncOperation`；await 路径 `var op = Resources.UnloadUnusedAssets(); await op.ToUniTask();`（SP-011 实证 `SP011_YooAssetAdditive.cs:375-376`，UniTask 提供 `AsyncOperation.ToUniTask()` extension）。
- `GC.Collect()` 同步调用；本场景在 fade overlay 黑屏期间 ~50ms pause 不可感知（ADR-009 决策允许）。
- `GameModule.Scene.UnloadAsync(string location)` 是 framework wrapper（`SceneModule.cs:312`），返 `UniTask<bool>`；以 sceneName (YooAsset location) 为参，**不是** `UnloadSceneAsync(handle)`。S3-01 P2 切换路径已实证（`SceneManager.cs:424`）。
- YooAsset shared-asset handles（UI prefabs, SFX 通过独立 `LoadAssetAsync` 持有的）NOT 由 scene unload 释放 — Scene Manager 只释放当前 scene 资源；其他系统的 handles 走自己的 release 路径。SP-003 verified.
- D1=[b] PlayMode-only：cleanup 序列涉及 `Resources.UnloadUnusedAssets` + `GC.Collect` + `GameModule.Scene.UnloadAsync` 全部依赖 Unity runtime；EditMode test runner 不可行；evidence 通过 `S302_CleanupSequence.cs` IDevSpike 在 PlayMode DevTestState 跑（沿用 S3-01 模式）。

**Performance**: Unloading 阶段冷路径（每章切换 1 次，~5 次/游戏）；`UnloadUnusedAssets` 异步 + `GC.Collect` 同步（~50ms pause 在 fade overlay 黑屏期间，ADR-009 决策允许）；`OnSceneUnloadBegin` 单次派发 ≤ 0.01ms（ADR-027 §2 guardrail 兜底）。5-cycle 内存回归测试为 AC-5 覆盖（非 perf budget，是 leak detection）。N/A 无帧级 hotpath budget。

**Control Manifest Rules (this layer)**:
- Required: `Scene Transition Cleanup Sequence (mandatory, in order): (1) notify systems via ISceneEvent.OnSceneUnloadBegin(_currentChapterId), (2) await UniTask.Yield() one frame for handlers to release owned AssetHandles, (3) GameModule.Scene.UnloadAsync(_currentChapterSceneName) + ClearCurrentChapterSceneName(), (4) Resources.UnloadUnusedAssets() awaited via .ToUniTask(), (5) GC.Collect() (synchronous), (6) advance to Loading state for next scene load` (ADR-005, ADR-009, ADR-027)
- Required: `Never skip UnloadUnusedAssets() + GC.Collect() between scene transitions — must run even on UnloadAsync exception (try-finally)` (ADR-005, ADR-009)
- Required: `Use ClearCurrentChapterSceneName() to clear the location string immediately after UnloadAsync (S3-01 暴露的 internal setter); v3 同步清 _currentLoadedChapterId (atomic 配对); do not write to _currentChapterSceneName / _currentLoadedChapterId directly` (S3-01 D5 + S3-02 v3)
- Required: `UnloadCurrentChapterAsync 守卫与 sender chapter id 来源 = _currentLoadedChapterId (已加载身份)，**不是** _currentChapterId (状态机目标)。两字段语义不同：前者由 LoadChapterSceneAsync 成功路径 set，后者由 OnRequestSceneChange 入口 set。Cleanup 序列只关心"是否有已加载场景需要卸"，与状态机推进解耦` (S3-02 v3 / ADR-029 R3)
- Required: `Scene-scoped listeners must be removed on ISceneEvent.OnSceneUnloadBegin (self-removal via GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneUnloadBegin, handler) in handler body)` (ADR-027 §3)
- Required: `First-boot guard uses NoChapterId sentinel (SceneManager.cs:59,85), never literal -1` (style consistency)
- Forbidden: `Never call GameModule.Resource.UnloadSceneAsync — does not exist; use GameModule.Scene.UnloadAsync(string)` (ADR-029 R2)
- Forbidden: `Never cache SceneHandle / Scene reference fields on SceneManager — only the location string` (S3-01 D5=[X])

---

## Acceptance Criteria

*From GDD `design/gdd/scene-management.md`, scoped to this story:*

- [x] **AC-1 dispatch**: `ISceneEvent.OnSceneUnloadBegin(_currentLoadedChapterId)` is dispatched via `GameEvent.Get<ISceneEvent>()` (Step 5) before any unload operation begins ✅ P1+P4 listener UB=8 (P1+P2+P4=3 + P6×5=5)
- [x] **AC-2 frame yield**: Scene Manager awaits `UniTask.Yield()` one frame after `OnSceneUnloadBegin` to allow systems to release their owned AssetHandles ✅ static guarantee in `UnloadCurrentChapterAsync` (line 525)
- [x] **AC-3 unload + clear**: `await GameModule.Scene.UnloadAsync(_currentChapterSceneName)` is called (Step 6); `ClearCurrentChapterSceneName()` is invoked immediately after to set `_currentChapterSceneName` + `_currentLoadedChapterId` to `null/NoChapterId` (do not write fields directly) ✅ P1+P2+P4 sceneName cleared; P4 even on error path
- [x] **AC-4 unload assets**: `await Resources.UnloadUnusedAssets().ToUniTask()` (Step 7) — must complete before next step ✅ static guarantee + P6 5-cycle 1.36% delta 印证
- [x] **AC-5 GC**: `GC.Collect()` is called synchronously immediately after `UnloadUnusedAssets` completes (Step 8) ✅ static guarantee + memNow < memBaseline 印证
- [x] **AC-6 strict order**: Steps execute in strict order: `OnSceneUnloadBegin → UniTask.Yield → GameModule.Scene.UnloadAsync + ClearCurrentChapterSceneName → Resources.UnloadUnusedAssets → GC.Collect` — never rearranged ✅ static guarantee (try-finally + await chain) + P1 unloadBeginTick(8641) < postGcTick(1833063) 印证
- [x] **AC-7 cleanup never skipped on error**: If `UnloadAsync` throws or returns false, `Resources.UnloadUnusedAssets` + `GC.Collect` + `ClearCurrentChapterSceneName` still run (try-finally guard); 异常 propagate（state machine 推进 by driver） ✅ P4 testhook 注入 invalid sceneName + sceneName cleared=true
- [x] **AC-8 first-boot skip**: First-boot path (`_currentLoadedChapterId == NoChapterId` 哨兵 OR `_currentChapterSceneName == null/empty`): Steps 5–8 are skipped entirely; FSM 由 driver 推进 ✅ P3 fresh SceneManager listener 0/0 守恒
- [x] **AC-9 5-cycle memory leak**: After 5 consecutive chapter transitions (A→B→A→B→A), memory baseline returns within 5% of the pre-test baseline (memory leak detection, TR-scene-011) ✅ P6 delta=1.36%
- [ ] **AC-10 shared asset survival**: Commonly-loaded shared assets (UI prefabs, SFX) held by independent `LoadAssetAsync` handles are NOT released by scene unload — they remain accessible after transition — **ADVISORY (P5 SkipAdvisory)**：缺独立 testable asset；shared-asset 守恒 verified by code review (SP-003 ADR-005)；后续标准 PlayMode test 覆盖（backlog）

---

## Implementation Notes

*Derived from ADR-009 + ADR-005 Implementation Guidelines, patched 2026-04-30 to align with framework wrappers and S3-01 D5=[X]:*

The cleanup sequence executes in the `Unloading` FSM state, between `TransitionOut` completing and `Loading` beginning. Recommended impl: introduce `private async UniTask UnloadCurrentChapterAsync()` on `SceneManager`, called by state-machine driver before `LoadChapterSceneAsync`.

```csharp
// SceneManager.cs (S3-02)
private async UniTask UnloadCurrentChapterAsync()
{
    // First-boot guard — no previous chapter loaded
    if (_currentChapterId == NoChapterId || string.IsNullOrEmpty(_currentChapterSceneName))
    {
        // AC-8: Steps 5-8 全 skip; 调用方直接推进到 Loading
        return;
    }

    int unloadingChapterId = _currentChapterId;
    string unloadingSceneName = _currentChapterSceneName;

    // Step 5 — notify all systems via ISceneEvent 接口广播（参数即 payload, ADR-027 sender 模式）
    GameEvent.Get<ISceneEvent>().OnSceneUnloadBegin(unloadingChapterId);

    // AC-2 — one-frame yield 给 handlers 释放自己持有的 AssetHandle
    await UniTask.Yield();

    try
    {
        // Step 6 — unload chapter scene; framework wrapper（不是 GameModule.Resource）
        bool unloaded = await GameModule.Scene.UnloadAsync(unloadingSceneName);
        if (!unloaded)
        {
            Log.Warning($"[SceneManager] UnloadAsync({unloadingSceneName}) returned false; cleanup continues.");
        }
    }
    finally
    {
        // S3-01 D5 暴露的 internal setter；不直接写 _currentChapterSceneName 字段
        ClearCurrentChapterSceneName();

        // AC-7 — Step 7 / Step 8 永不跳过（即使 UnloadAsync throw 也跑）
        var op = Resources.UnloadUnusedAssets();
        await op.ToUniTask();
        GC.Collect();
    }
}
```

**First-boot guard 语义**：使用 `_currentChapterId == NoChapterId` 哨兵常量（`SceneManager.cs:59,85`），不用裸字面量 `-1`。`_currentChapterSceneName` 同步检查防御 ChapterId 已 set 但 Scene 加载半失败的边角态（保守：只要 sceneName 缺则视作 first-boot 路径）。

**Why GC.Collect() is synchronous here**: This runs behind the fade overlay. The player sees a black screen. The ~50ms GC pause is invisible and acceptable. Do NOT move GC to a background thread.

**SP-003 shared asset note (AC-10)**: UI prefabs loaded via `GameModule.Resource.LoadAssetAsync()` by UIModule retain their own handles. `UnloadUnusedAssets()` will not release them because their reference count > 0. This is correct and expected. The Scene Manager only releases the chapter scene — each system releases its own handles in its `OnSceneUnloadBegin` listener.

**Memory leak test (AC-9)**: Run 5 consecutive chapter transitions in an automated PlayMode spike; snapshot memory before and after each cycle using `UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()`; assert that the final baseline is within 5% of the initial baseline. SP-011 P3 已实证此模式（`SP011_YooAssetAdditive.cs:375-385`）— 直接复用 baseline / postCycle / delta 结构。

**State machine 接入**：本 story 暴露 `UnloadCurrentChapterAsync()` 给 SceneManager Unloading-state driver 调用；driver 推进顺序 `TransitionOut → Unloading (调本方法) → Loading (调 LoadChapterSceneAsync from S3-01) → TransitionIn → Idle`。driver 本身的状态推进由 Story 005 lifecycle senders 负责（OnSceneTransitionBegin / End 等），本 story 不实装。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: State machine FSM state transitions (Unloading state is declared there)
- Story 002: LoadSceneAsync and SetActiveScene (happens after this cleanup)
- Story 005: `ISceneEvent.OnSceneTransitionBegin` / `OnSceneTransitionEnd` sender 实装（本 story 实现 `OnSceneUnloadBegin` sender）

---

## QA Test Cases

*Mapped to PlayMode spike `S302_CleanupSequence.cs`（D1=[b]）；每条 case 对应 spike 内 P{N}_xxx 方法。*

- **AC-Order (P1)**: Cleanup sequence order is deterministic
  - Given: Chapter 1 is loaded（`_currentChapterId=1`, `_currentChapterSceneName="SP011_SceneA"`）；OnSceneUnloadBegin listener 已挂；spike 提前 LoadSceneAsync(SP011_SceneA) Additive
  - When: 调 `SceneManager.UnloadCurrentChapterAsync()`
  - Then: 事件/操作顺序：`OnSceneUnloadBegin(1)` 先派 → 一帧后 → `Scene.UnloadAsync("SP011_SceneA")` 完成 → `Resources.UnloadUnusedAssets` 完成 → `GC.Collect` 完成；每步完成后才进下一步（spike 在 listener / await 后插入时间戳验证）
  - Edge: 顺序违反 → spike PASS=false（如 GC 比 UnloadUnusedAssets 先跑）

- **AC-SceneNameNull (P2)**: `_currentChapterSceneName` is null after unload (D5=[X] 字段语义)
  - Given: `_currentChapterSceneName="SP011_SceneA"`（前一 chapter 加载成功）
  - When: `UnloadCurrentChapterAsync()` 完成
  - Then: `CurrentChapterSceneNameForTest == null`（通过 S3-01 暴露的 testhook 读取）；同时 Unity sceneCount baseline 守恒（等于 entry baseline，不是 +1）
  - Edge: `ClearCurrentChapterSceneName` 未调 → 字段非 null → spike fail；调 `UnloadAsync` 时 sceneName 是 `null` 必须保护（`UnloadCurrentChapterAsync` 入口 `string.IsNullOrEmpty` 守卫）

- **AC-FirstBoot (P3)**: First-boot skip (no previous chapter)
  - Given: 全新 SceneManager；`_currentChapterId == NoChapterId`；`_currentChapterSceneName == null`
  - When: 调 `UnloadCurrentChapterAsync()`（spike 直接调，模拟 driver 第一次进 Unloading 的场景）
  - Then: 不派 `OnSceneUnloadBegin`（spike listener 计数 = 0）；不调 `Scene.UnloadAsync`（无场景加载状态）；不抛异常；方法直接 return
  - Edge: NoChapterId 哨兵值不能与任何合法 chapterId 撞（约定 = -1，但代码用常量名）

- **AC-CleanupOnError (P4)**: Cleanup never skipped on UnloadAsync exception
  - Given: 已加载 chapter；spike 注入异常路径（试 unload 一个不存在的 sceneName，eg `_currentChapterSceneName = "Chapter_999_NotInManifest"` testhook 强写）
  - When: 调 `UnloadCurrentChapterAsync()`
  - Then: `Resources.UnloadUnusedAssets` + `GC.Collect` 仍跑完（spike 在 try-finally 各步插日志验证执行）；`ClearCurrentChapterSceneName` 仍调（finally 内）；方法抛异常或派 OnSceneLoadFailed（具体由 dev-story 决策）但 cleanup side effect 不漏
  - Edge: 不允许 cleanup 因 try 提前 return 而 skip Step 7-8

- **AC-SharedAsset (P5 advisory)**: Shared asset handles survive scene transition
  - Given: spike 在 P1 之前通过 `GameModule.Resource.LoadAssetAsync<GameObject>("ui_prefab_test", "DefaultPackage")` 持有一个独立 handle
  - When: cleanup 序列跑完
  - Then: handle 仍 IsValid；UI prefab 仍可读（`handle.AssetObject != null`）；`UnloadUnusedAssets` 因 ref count > 0 不释放
  - Edge: 若所选测试资产不存在于 manifest（`ui_prefab_test` 占位），P5 标 ADVISORY 或选已知存在的 SP-011 资产；不阻塞 core PASS

- **AC-MemoryLeak5Cycle (P6)**: 5-cycle memory leak test
  - Given: spike 启动时 `_baselineMemory = Profiler.GetTotalAllocatedMemoryLong()`
  - When: 跑 5 次 `LoadChapterSceneAsync(c) → UnloadCurrentChapterAsync()` 循环（c 用 SP011_SceneA / B / 同包 chapters，testhook 设 chapterDataProvider）
  - Then: 第 5 次循环后 `delta = abs(postMemory - baseline) / baseline ≤ 5%`
  - Edge: 容忍 1 cycle 临时内存峰值；只校验稳态 baseline；GC.Collect 缺失会让 delta 远超 5% → 自然 fail；与 SP-011 P3 同模式（`SP011_YooAssetAdditive.cs:375-385`）

---

## Test Evidence

**Story Type**: Integration (PlayMode-only — D1=[b] inherited from S3-01)
**Required evidence (PlayMode spike sequence)**:
- `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S302_CleanupSequence.cs` — `IDevSpike` + `Runtime` + `Tester` 模式（参 `S301_AdditiveSceneLoading.cs` template）
- 注册位置 `GameApp.RegisterDevSpikes`（与 S3-01 同；保持 SP-011 仍注释关闭，type-3 race 防御）
- JSON 落地 `Application.persistentDataPath/S302_Result.json`（schema：`p1Order` / `p2SceneNameNull` / `p3FirstBoot` / `p4CleanupOnError` / `p5SharedAsset` / `p6MemoryLeak5Cycle` + `allCorePassed` + `listenerCounts.OnSceneUnloadBegin` + delta% 数据点）
- Manual evidence file: `production/qa/spike-S302-cleanup-result-2026-04-30.md`（spike JSON 输出 + Unity console 截图 + 内存 delta 数据点）
- **Core PASS criteria**: P1+P2+P3+P4+P6 全 PASS；P5 SharedAsset 可 ADVISORY（取决于 manifest 内是否有可用 ui_prefab_test 资产，dev 阶段可用 SP-011 内同包 prefab 替代）

**Smoke test mapping**: 本 story spike 加入 sprint smoke check spike batch（`/smoke-check` 命中 spike list）。

**Status**: [x] **DONE 2026-04-30 dusk — CORE PASSED**

### PlayMode 验证结果（2026-04-30 dusk final run，post-v3 修复）

| Case | Result | 数据点 |
|------|--------|--------|
| **P1** Order | ✅ PASS | `unloadBeginTick=8641` < `postGcTick=1833063`（序列正确，stopwatch ticks 单调递增）；`OnSceneUnloadBegin(201)` 一次派发；sceneCount → baseline (1)；sceneName cleared |
| **P2** SceneNameNull | ✅ PASS | `LoadChapterSceneAsync(202)` → SP011_SceneB；`UnloadCurrentChapterAsync` 后 `_currentChapterSceneName == null` + sceneCount → baseline |
| **P3** FirstBoot | ✅ PASS | fresh `SceneManager` `_currentLoadedChapterId == NoChapterId` → guard 短路；`OnSceneUnloadBegin` count 不变；`OnSceneLoadComplete` count 不变；sceneCount 守恒 |
| **P4** CleanupOnError | ✅ PASS | testhook 写 `_currentChapterSceneName="Chapter_999_NotInManifest"`，但 `_currentLoadedChapterId` 仍 = CHAPTER_1 → guard 通过；`UnloadAsync(invalid)` 抛/返 false；`p4Probes.unloadBeginFired=true`（sender 仍 fire 派 CHAPTER_1）；`p4Probes.sceneNameCleared=true`（finally 块 `ClearCurrentChapterSceneName` 仍跑） |
| **P5** SharedAsset | SkipAdvisory | 缺独立 testable asset；shared-asset 守恒 verified by code review (SP-003 ADR-005)；后续标准 PlayMode test 覆盖（backlog） |
| **P6** MemoryLeak 5-cycle | ✅ PASS | 5 轮 SP011_SceneA↔SceneB 切换；末轮 GC 后 **delta = 1.36%** << 5% tolerance |

**Listener counts (matching expected payloads)**：
- `OnSceneUnloadBegin = 8`（P1+P2+P4 = 3 派发 + P6 5-cycle = 5 派发 → **8** ✅）
- `OnSceneLoadComplete = 8`（P1+P2+P4 = 3 加载 + P6 5-cycle = 5 加载 → **8** ✅）
- `lastUnloadBeginChapterId = 201`（最后一次卸 CHAPTER_1 — 即 P6 cycle 5）
- `lastCompletedChapterId = 201`（最后一次加载 CHAPTER_1 — P6 cycle 5）

**Memory profile**：
- `memoryBaselineBytes = 241,159,577` (~230 MB)
- `memoryNowBytes = 235,138,064` (~224 MB)
- 末态比 baseline **少 ~6 MB**（`Resources.UnloadUnusedAssets() + GC.Collect` 释放有效）

**JSON output**: `Application.persistentDataPath/S302_Result.json` —
`allCorePassed: true`, `lastError: ""`, `statusText: "CORE PASSED (P5=SkipAdvisory; delta=1.36%)"`, `phase: "done"`

### 实施过程的 drift 数据点（ADR-029 第 2 + 第 3 条）

1. **Phase 1.5 readiness — Type-4 propagation drift**：S3-01 D5 决策传播滞后 4 处 type-1 fantasy API；patch v2 修订 ~**8 min**（vs S3-01 baseline 18 min ~55% 节省）— 第 2 数据点
2. **R3 PlayMode runtime — Type-2 cross-method protocol drift**：grep gate 全 PASS，但 PlayMode 首跑 6/6 FAIL；root cause = `LoadChapterSceneAsync` 仅 set `_currentChapterSceneName`，`_currentChapterId` 仅由 `OnRequestSceneChange` set；`UnloadCurrentChapterAsync` 用前者守卫但 spike 直调路径下永留 `NoChapterId` → silent return；同时暴露生产 driver 路径 sender 派错 chapter id bug。v3 修复引入 `_currentLoadedChapterId` 配对字段（atomic 配对 sceneName 作为已加载身份单一事实源）；总耗 **21 min**（首跑 6 + 决策矩阵 4 + fix + doc 11）— **第 3 数据点**

**累计 dev-story drift 修订**：~29 min（8 min Type-4 + 21 min Type-2）；R3 暴露的 Type-2 drift 验证 **R3 PlayMode probe 是 mandatory（不可跳）** — 已入 ADR-029 V2 候选。

### post-v3 grep evidence (final)

`production/qa/grep-no-fantasy-api-cleanup-2026-04-30.md` §1 + §2 + **§2.5 R3 PlayMode probe 暴露 Type-2 drift**：
- §1 8 项 forbidden / fantasy API：**0 hit** ✅
- §2 4 项 framework correct usage：UnloadAsync = 9 / chapterName setter = 14 / UnloadUnusedAssets = 7 / OnSceneUnloadBegin = 8（全 ≥1）✅
- §2.5 v3 新字段：`_currentLoadedChapterId` = 6 hit；`CurrentLoadedChapterIdForTest` = 1 hit ✅

---

## ADR-029 Phase 1.5 Readiness Verdict (2026-04-30)

| Rule | Verdict | 数据 |
|------|---------|------|
| **R1** Per-event listener / sender pairing (ADR-027) | ✅ PASS | story 用 sender 模式，与 SceneManager 现有 sender 路径一致；listener-side 责任落在订阅系统 |
| **R2** Cross-component API existence grep | ❌→✅ PASS (post-patch v2) | 4 处 type-1 fantasy API 已修订：`_currentSceneHandle` → `_currentChapterSceneName`；`GameModule.Resource.UnloadSceneAsync` → `GameModule.Scene.UnloadAsync`；`null` 直写 → `ClearCurrentChapterSceneName()`；EditMode test → PlayMode spike |
| **R3** Stub data type constructor | ✅ PASS | 无 stub data type 引用 |
| Type-2 / Type-3 风险 | LOW | `Scene.UnloadAsync` S3-01 P2 实证 PASS；spike sequential 调度（SP-011 已注销）|

**Drift revision time data point (S3-02)**: 估约 8 min（baseline S3-01 = 18 min；预期 ≤5min Type-1 + 3min Test Evidence pivot 注脚），ADR-029 第 2 条数据点。

---

## Dependencies

- Depends on: Story 001 (state machine — DONE), Story 002 / S3-01 (Loading phase + `ClearCurrentChapterSceneName()` setter + `_currentChapterSceneName` field — DONE 2026-04-30)
- Unlocks: Story 005 (full 8-event integration — cleanup events 是完整事件流的一部分)
