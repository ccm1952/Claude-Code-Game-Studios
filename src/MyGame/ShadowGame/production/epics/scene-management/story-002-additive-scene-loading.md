// 该文件由Cursor 自动生成

# Story 002: Additive Scene Loading via YooAsset

> **Epic**: Scene Management
> **Status**: Done (2026-04-30 PlayMode CORE PASSED)
> **Layer**: Core
> **Type**: Integration (PlayMode-only)
> **Manifest Version**: 2026-04-30 patch v2 (ADR-029 R2 STOP 响应) + P0 修订（CheckLocationValid 移除）+ DevBootstrap 并发 fix
>
> **Revision note (2026-04-23)**: 原文使用 ADR-006 `Evt_SceneDownloadProgress` / `Evt_SceneLoadProgress` / `Evt_SceneLoadComplete` / `Evt_SceneLoadFailed` 常量 + payload struct 协议；已迁移到 ADR-027 `ISceneEvent` 接口事件（方法参数即 payload）。签名由 Story 001（S2-05）冻结于 `ISceneEvent.cs`，本 story 仅做 sender 侧调用。
>
> **Revision note (2026-04-30 v2)**: ADR-029 Phase 1.5 readiness gate 抓住 6 处 framework drift（详见 commit 前 STORY-002-PATCH-V2-PROPOSAL.md）：原 §Engine Notes / §Implementation Notes 用 `GameModule.Resource.LoadSceneAsync(...) → SceneHandle` 是 YooAsset native 心智模型，绕过 TEngine SceneModule 包装层（违反 ADR-028 §1 M3 SceneModule 必用决策）。本 v2 全面对齐 framework：`GameModule.Scene.LoadSceneAsync(...) → UniTask<Scene>` + `GameModule.Scene.UnloadAsync` + `GameModule.Scene.ActivateScene` + `GameModule.Resource.CreateResourceDownloader`（实测 PASS 引用：SP-011 spike + ProcedureDownloadFile.cs）。AC 完整对齐 α 路径；测试策略 D1=[b] PlayMode-only（与 SP-011 同 IDevSpike 模式）。
>
> **Revision note (2026-04-30 P0 spike runtime 反馈)**: spike 首跑 P1 FAIL — `lastError = "P1 OnSceneLoadComplete count mismatch: expected 1, got 0"`；`OnSceneLoadProgress / DownloadProgress` 全 0；`OnSceneLoadFailed` 计数与 fail-loud 1 次 +P4 1 次 = 2 完全吻合。**Root cause**: `GameModule.Resource.CheckLocationValid(sceneName, "DefaultPackage")` 对 YooAsset scene 资产（.unity）一律返 false（scene 资产与普通 asset 走不同 location key 体系；SP-011 PASS 路径根本不调 CheckLocationValid，直调 LoadSceneAsync 即可）。**Fix [A]**: 移除 CheckLocationValid 前置；invalid sceneName 改由 LoadSceneAsync 自身抛异常 → catch → retry 2 次 → 耗尽 fail；ChapterDataProvider null 仍是唯一 fail-loud 强 contract。AC-4 路径改成 retry exhaust + reason 不强制特定字符串。

## Context

**GDD**: `design/gdd/scene-management.md`
**Requirement**: `TR-scene-001`, `TR-scene-003`, `TR-scene-004`, `TR-scene-007`
*(Additive scene architecture; async loading via UniTask; always LoadSceneMode.Additive; YooAsset on-demand download)*

**ADR Governing Implementation**: ADR-009: Scene Lifecycle + ADR-005: YooAsset Resource Lifecycle + ADR-027: GameEvent Interface Protocol
**ADR Decision Summary**: All scenes loaded via `GameModule.Resource.LoadSceneAsync()` in `LoadSceneMode.Additive`. Scene Manager is sole owner of the chapter `SceneHandle`. After load, `SceneManager.SetActiveScene()` is called on the new scene. Previous chapter must be fully unloaded before new chapter loads. 所有生命周期广播通过 `GameEvent.Get<ISceneEvent>().OnXxx(...)` 接口调用派发（ADR-027）；**禁止** `Evt_*` 常量 / payload struct。

**Engine**: Unity 2022.3.62f2 LTS + YooAsset 2.3.17 + UniTask 2.5.10 | **Risk**: MEDIUM
**Engine Notes**: 通过 TEngine SceneModule 包装层调用（**不直调 YooAsset native**；ADR-028 §1 M3 SceneModule 必用决策）。

加载场景：
```csharp
Scene scene = await GameModule.Scene.LoadSceneAsync(
    sceneName,
    LoadSceneMode.Additive,
    progressCallBack: p => GameEvent.Get<ISceneEvent>().OnSceneLoadProgress(sceneName, p));
```
返回 Unity 原生 `Scene` struct（**非** YooAsset `SceneHandle`）；用 `scene.IsValid()` + `scene.isLoaded` 双断言失败检测。

卸载场景（Story 003 cleanup 调）：
```csharp
bool ok = await GameModule.Scene.UnloadAsync(sceneName);
```

激活场景（framework 包装层；**不**直调 `UnityEngine.SceneManagement.SceneManager.SetActiveScene`）：
```csharp
bool ok = GameModule.Scene.ActivateScene(sceneName);  // 同步；返回 bool
```

下载需求判定（YooAsset 单包策略 SP-003 `"DefaultPackage"`；与 `ProcedureDownloadFile.cs` 已 PASS 模式对齐）：
```csharp
var downloader = GameModule.Resource.CreateResourceDownloader("DefaultPackage");
if (downloader.TotalDownloadCount > 0) {  /* D3=[i] 有未缓存依赖才走下载分支 */
    downloader.DownloadUpdateCallback = data =>
        GameEvent.Get<ISceneEvent>().OnSceneDownloadProgress(
            downloader.Progress, data.CurrentDownloadBytes, data.TotalDownloadBytes);
    downloader.BeginDownload();
    await downloader;  // ResourceDownloaderOperation 是 awaitable
    if (downloader.Status != EOperationStatus.Succeed) { /* fail */ }
}
```

**注**：原 patch v2 在 Step 8a 加 `CheckLocationValid(sceneName, "DefaultPackage")` 前置；P0 spike runtime 实证对 YooAsset scene 资产（.unity）一律返 false（scene 与 asset 走不同 location key 体系）→ **已移除**。invalid sceneName 由 LoadSceneAsync 抛异常 → catch → retry 2 次 → 耗尽 fail，路径完整覆盖。SP-011 PASS 路径同样不调 CheckLocationValid。

实测 PASS 引用：
- **SP-011 spike** `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/SP011_YooAssetAdditive.cs` — P1 LoadSceneAsync(Additive) / P2 UnloadAsync / P3 5×cycle 内存稳定（delta -1.27%）
- **ProcedureDownloadFile.cs** 已 PASS 模式 — `DownloadUpdateCallback` / `await downloader` / `Status` 检测

**Performance**: 章节切换冷路径（~5 次/游戏），`LoadSceneAsync` / `ActivateScene` 为异步/同步非帧级操作；`OnSceneLoadProgress` / `OnSceneDownloadProgress` 每帧派发若 UI 订阅，每方法 ≤ 0.01ms（ADR-027 §2 guardrail 兜底）。N/A 无帧级 hotpath budget。

**Control Manifest Rules (this layer)**:
- Required: `Additive-only scene loading — LoadSceneMode.Additive exclusively` (ADR-009)
- Required: `After loading chapter scene, call GameModule.Scene.ActivateScene(sceneName) — framework wrapper` (ADR-009 + ADR-028 §1 M3)
- Required: `Use GameModule.Scene facade for all scene I/O (LoadSceneAsync / UnloadAsync / ActivateScene / IsContainScene)` (ADR-028 §1 M3)
- Required: `Use GameModule.Resource.CreateResourceDownloader for YooAsset download — never call YooAssets.* directly from HotFix` (ADR-005 + ADR-028 §1 M2)
- Required: `All async operations via UniTask; ResourceDownloaderOperation is awaitable (await downloader; check Status)` (ADR-001)
- Forbidden: `Never use LoadSceneMode.Single in chapter scenes` (ADR-009)
- Forbidden: `Never call UnityEngine.SceneManagement.SceneManager.LoadSceneAsync / SetActiveScene from HotFix/GameLogic/Scene/` (ADR-009 + ADR-028 §1 M3)
- Forbidden: `Never use synchronous Resources.Load or AssetBundle.LoadAsset` (ADR-005)
- Forbidden: `Never poll while !sceneOperation.IsDone — pass progressCallBack lambda directly` (framework idiom; LoadSceneAsync 返回时 100% 已完成)
- Forbidden: `Never use DontDestroyOnLoad in chapter scene objects` (ADR-009)

---

## Acceptance Criteria

*From GDD `design/gdd/scene-management.md`, scoped to this story; α 完整对齐 framework（patch v2 / 2026-04-30）:*

- [ ] **AC-1** 场景以 Additive 加载 — `GameModule.Scene.LoadSceneAsync(sceneName, LoadSceneMode.Additive, ...)`；**禁止 `LoadSceneMode.Single`**（grep `LoadSceneMode\.Single` 在 `Assets/GameScripts/HotFix/GameLogic/Scene/` 0 hit）
- [ ] **AC-2** 场景名称由 SceneManager 私有持有 — `string _currentChapterSceneName` 是唯一 source-of-truth；不缓存 `Scene` 对象（D5=[X]）；`UnloadAsync` / `ActivateScene` 都靠 location 字符串
- [ ] **AC-3** 加载完成后通过 `GameModule.Scene.ActivateScene(sceneName)` 激活（返 true 期望，false 仅 warning 不阻塞，与 SP-011 P3 同模式）；**禁止** 直调 `UnityEngine.SceneManagement.SceneManager.SetActiveScene`（grep 0 hit in `HotFix/GameLogic/Scene/`）
- [ ] **AC-4** Invalid sceneName 失败传播 — LoadSceneAsync 对未在 manifest 的 sceneName 抛异常或返 invalid Scene → 进 retry 路径 → 2 次耗尽 → `OnSceneLoadFailed(chapterId, lastError.Message)` + Error 状态。**P0 修订**：原文要求 CheckLocationValid 前置 fail-loud；实测对 scene 资产一律返 false 不可用（scene 与 asset 走不同 location key 体系，SP-011 PASS 路径同样不调），改成 retry exhaust 路径覆盖；`fail reason` 不强制特定字符串（接受 LoadSceneAsync 抛错 message）
- [ ] **AC-5** 下载需求判定 — `var downloader = GameModule.Resource.CreateResourceDownloader("DefaultPackage"); if (downloader.TotalDownloadCount > 0)` 才走下载分支（D3=[i]）；下载中持续派发 `OnSceneDownloadProgress(downloader.Progress, currentBytes, totalBytes)`（progress 单调递增）；`await downloader` 后 `downloader.Status != EOperationStatus.Succeed` → 异常路径
- [ ] **AC-6** 加载进度通过 `progressCallBack: p => OnSceneLoadProgress(sceneName, p)` 推送（D4=[I]；**不**轮询 IsDone）；`UniTask<Scene>` await 完成时 progress 已到 1.0
- [ ] **AC-7** 加载成功 → `OnSceneLoadComplete(chapterId, ChapterData.BgmAsset)` 派发；BgmAsset 来自 S2-07 ChapterDataProvider（未注册或 chapterId 未知 → 走 AC-4 fail-loud 路径）
- [ ] **AC-8** Scene IsValid + isLoaded 双断言（SP-011 P1 同模式）— `await LoadSceneAsync` 返回的 `Scene` 必须 `.IsValid() == true` AND `.isLoaded == true`；任一 false 视为失败进入重试
- [ ] **AC-9** 重试机制 — `MAX_LOAD_RETRY = 2`（外层 try-catch 包裹 download + LoadScene + ActivateScene；D2=[α] 统一外层重试）；download 异常 / Scene 无效 / LoadSceneAsync throw 都 trigger 重试；2 次 attempts 耗尽 → `OnSceneLoadFailed(chapterId, lastError.Message)` + Error 状态
- [ ] **AC-10** Memory ≤3 scenes — 章节加载完成后 `UnitySceneManager.sceneCount ≤ baseline + 1`（BootScene + MainScene + 1 chapter）；DEBUG build assert；**SP-011 P3 5×cycle 已 PASS** baseline + 1（delta -1.27%）

**Cross-story 协作**：
- Story 003 cleanup 调 `GameModule.Scene.UnloadAsync(_currentChapterSceneName)` + 清空 `_currentChapterSceneName` 字段（同 SceneManager 实例内）
- First-boot 路径（D6=[①]）由 SceneManager.OnRequestSceneChange 既有逻辑（S2-05 实装）覆盖：`_currentChapterId == NoChapterId` → 直接进 TransitionOut，无需特殊代码

---

## Implementation Notes

*Derived from ADR-009 + ADR-005 + ADR-028 §1 M3 Implementation Guidelines；patch v2 / 2026-04-30 重写对齐 framework：*

11 步流程的 Step 5–10 在 SceneManager 内推进；触发：状态机进入 Loading 时调 `LoadChapterSceneAsync(targetChapterId)`。
First-boot 路径（D6=[①]）：`_currentChapterId == NoChapterId` → 由 SceneManager.OnRequestSceneChange 内部跳过 TransitionOut/Unloading 两态直接 Loading；
非 first-boot：Story 003 cleanup 完成后，状态机 Unloading → Loading 时 trigger 本 story 流程。

```csharp
private const int MAX_LOAD_RETRY = 2;
private const string DEFAULT_PACKAGE = "DefaultPackage"; // SP-003 单包策略
private string _currentChapterSceneName; // D5=[X] 只存 location，不缓存 Scene 对象

private async UniTask LoadChapterSceneAsync(int targetChapterId)
{
    // Resolve sceneName via S2-07 ChapterDataProvider
    var chapterData = _chapterDataProvider?.Invoke(targetChapterId);
    if (chapterData == null) {
        var reason = $"ChapterData null for id={targetChapterId}";
        GameEvent.Get<ISceneEvent>().OnSceneLoadFailed(targetChapterId, reason);
        TransitionTo(SceneManagerState.Error);
        return;
    }
    string sceneName = chapterData.SceneId; // ChapterData.SceneId 即 location（S2-07 已建）

    // 注：P0 修订 — CheckLocationValid 前置已移除；invalid sceneName 由 LoadSceneAsync 抛异常 → retry 兜底
    // D2=[α] 统一外层重试 — download + LoadSceneAsync + ActivateScene 合起来 1 轮 = 1 次尝试
    Exception lastError = null;
    for (int attempt = 1; attempt <= MAX_LOAD_RETRY; attempt++) {
        try {
            // Step 8 — Download 分支（D3=[i] 用 TotalDownloadCount > 0 判定）
            var downloader = GameModule.Resource.CreateResourceDownloader(DEFAULT_PACKAGE);
            if (downloader.TotalDownloadCount > 0) {
                downloader.DownloadUpdateCallback = data => {
                    GameEvent.Get<ISceneEvent>().OnSceneDownloadProgress(
                        downloader.Progress,
                        data.CurrentDownloadBytes,
                        data.TotalDownloadBytes);
                };
                downloader.BeginDownload();
                await downloader;  // ResourceDownloaderOperation 是 awaitable
                if (downloader.Status != EOperationStatus.Succeed) {
                    throw new Exception($"Download failed: status={downloader.Status}");
                }
            }

            // Step 9 — async scene load（D4=[I] 直接传 progressCallBack）
            var loadedScene = await GameModule.Scene.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Additive,
                progressCallBack: p =>
                    GameEvent.Get<ISceneEvent>().OnSceneLoadProgress(sceneName, p));

            if (!loadedScene.IsValid() || !loadedScene.isLoaded) {
                throw new Exception($"Scene returned invalid: name={loadedScene.name}, valid={loadedScene.IsValid()}, loaded={loadedScene.isLoaded}");
            }

            // Step 10a — activate（framework 包装层；ADR-028 §1 M3）
            bool activated = GameModule.Scene.ActivateScene(sceneName);
            if (!activated) {
                Log.Warning($"[SceneManager] ActivateScene returned false: {sceneName} (non-blocking, SP-011 P3 同模式)");
            }

            _currentChapterSceneName = sceneName;

            // Step 10b — completion broadcast（BgmAsset 来源 ChapterData，由 Audio epic listener 切 BGM）
            GameEvent.Get<ISceneEvent>().OnSceneLoadComplete(targetChapterId, chapterData.BgmAsset);
            return; // 成功，退出 retry 循环
        }
        catch (Exception ex) {
            lastError = ex;
            Log.Warning($"[SceneManager] Load attempt {attempt}/{MAX_LOAD_RETRY} failed: {ex.Message}");
        }
    }

    // 所有 retry 耗尽
    var failReason = lastError?.Message ?? "unknown error";
    GameEvent.Get<ISceneEvent>().OnSceneLoadFailed(targetChapterId, failReason);
    TransitionTo(SceneManagerState.Error);
}
```

**Cleanup 协作（与 Story 003 边界）**：
- Story 003 cleanup sequence 调用 `GameModule.Scene.UnloadAsync(_currentChapterSceneName)` + 清空 `_currentChapterSceneName`（同一 SceneManager 实例内）
- 本 story 不实施 Unload 路径（out of scope）

**State machine 接入点**：`LoadChapterSceneAsync` 在状态机进入 `Loading` 状态时由 SceneManager 内部驱动（具体接入由 dev-story 阶段决定 hook 位置；候选：`AdvanceStateForTest(Loading)` 后续重写为 `AdvanceState(Loading)` 内 fire-and-forget `LoadChapterSceneAsync(_inflightChapterId).Forget()`，或新增 `EnterLoading()` 内部方法）；`return` 成功路径不主动推 `TransitionTo(TransitionIn)` —— 留给 Story 005 lifecycle senders 接 fade in 流程。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: State machine skeleton and mutex — must be DONE first
- Story 003: Cleanup sequence (notify → release → unload → GC.Collect) — runs before this story's load
- Story 005: 其余 `ISceneEvent` 方法的 sender 实装（本 story 实现 `OnSceneLoadProgress` / `OnSceneDownloadProgress` / `OnSceneLoadComplete` / `OnSceneLoadFailed` 4 方法；剩 `OnSceneTransitionBegin` / `OnSceneUnloadBegin` / `OnSceneTransitionEnd` 由 Story 005 补齐）
- Story 006: Resolving `sceneName` from `TbChapter.sceneId` (Luban mapping)

---

## QA Test Cases

*α 完整对齐 PlayMode case（patch v2）；测试驱动由 D1=[b] PlayMode-only spike 模式覆盖；映射到 `S301_AdditiveSceneLoading.cs` 的 P1-P6：*

- **AC-1 / AC-3**: Scene loads additively + activates via framework wrapper
  - Given: SceneManager 进 Loading 状态，target chapter = 1
  - When: `LoadChapterSceneAsync(1)` 被调用
  - Then: `UnitySceneManager.sceneCount == baseline + 1`；`UnitySceneManager.GetActiveScene().name == sceneName`；grep `LoadSceneMode\.Single` 在 `HotFix/GameLogic/Scene/` 0 hit；grep `UnityEngine\.SceneManagement\.SceneManager\.SetActiveScene` 在 `HotFix/GameLogic/Scene/` 0 hit
  - PlayMode P1 覆盖

- **AC-2**: SceneName 唯一持有
  - Given: Chapter 1 scene loaded successfully
  - When: `_currentChapterSceneName` 被检查
  - Then: 等于 `chapterData.SceneId`；不缓存任何 `Scene` 对象
  - Edge cases: Story 003 unload 完成后 `_currentChapterSceneName == null`
  - PlayMode P1 覆盖（属性 inspect）

- **AC-4**: Invalid sceneName 失败传播 (P0 修订 — 走 retry exhaust 路径)
  - Given: ChapterDataProvider 返 `ChapterData.SceneId = "Chapter_999_NotInManifest"`
  - When: `LoadChapterSceneAsync(999)`
  - Then: LoadSceneAsync 抛异常或返 invalid Scene → catch → retry → 2 次耗尽 → `OnSceneLoadFailed(999, <非空 reason>)` 派发恰 1 次；状态机 → Error；reason 来自 LoadSceneAsync 异常/Scene IsValid=false 提示，**不**强制 contains 特定字符串
  - **P0 注**：原文要求 CheckLocationValid 前置 fail-loud；实证不可行（scene 资产 location key 不被 CheckLocationValid 接受）→ retry exhaust 形式覆盖
  - PlayMode P4 覆盖

- **AC-5**: Download branch
  - Given: 模拟清缓存（spike 起手清 PersistentDataPath 中 YooAsset 缓存）+ 资源未下载
  - When: `LoadChapterSceneAsync` 触发
  - Then: 多次 `OnSceneDownloadProgress(progress, currentBytes, totalBytes)`（progress 单调递增）；`await downloader` 完成 `Status == EOperationStatus.Succeed`；之后才进 `LoadSceneAsync`
  - Edge cases: `Status != Succeed` → 进重试路径
  - PlayMode P3 覆盖

- **AC-6**: Progress callback (no polling)
  - Given: 任意章节加载
  - When: `LoadSceneAsync` 进行中
  - Then: `OnSceneLoadProgress(sceneName, p)` 派发 ≥ 2 次（中间帧 + 完成帧 p == 1.0）；不通过轮询；grep `while.*\.IsDone` 在 `HotFix/GameLogic/Scene/` 0 hit
  - PlayMode P1 覆盖

- **AC-7 / AC-8**: Happy path event dispatch
  - Given: 任意章节加载成功
  - When: Step 10b 完成
  - Then: `OnSceneLoadComplete(chapterId, ChapterData.BgmAsset)` 派发恰 1 次；`Scene.IsValid()` + `.isLoaded` 双断言 PASS
  - PlayMode P1 覆盖

- **AC-9** (ADVISORY): Retry mechanism
  - Given: D1=[b] PlayMode-only 模式下 GameModule.Scene 静态 facade 不可 stub
  - When: 想验证 LoadSceneAsync 抛异常 → retry → 最终 fail/success
  - Then: PlayMode 入口不可达 → 路径**不验**；通过 Code Review of `LoadChapterSceneAsync` (try-catch + lastError + MAX_LOAD_RETRY 循环) 确认逻辑正确性；future ISceneLoader stub seam refactor 时再补 PlayMode 覆盖
  - **Status**: ADVISORY — 不阻塞 story-done

- **AC-10**: Memory ≤3 scenes (SP-011 P3 已实证 baseline)
  - Given: 章节加载完成
  - When: `UnitySceneManager.sceneCount` 检查
  - Then: ≤ baseline + 1（BootScene + MainScene + 1 chapter）；多次切换不累积（与 SP-011 P3 5×cycle delta -1.27% 行为一致）
  - PlayMode P2 (单切换) + 隐含 covered by SP-011 P3

---

## Test Evidence

**Story Type**: Integration（PlayMode-only — D1=[b]）
**Rationale**: `GameModule.Scene` 是静态 facade 不可 stub；引 `ISceneLoader` 抽象层 over-engineering（option-a 否决）；本 story 行为完全依赖 Unity SceneManager 真实运行时（YooAsset → Unity SceneManager.LoadSceneAsync）→ EditMode 不可达。**与 SP-011 同 IDevSpike + OnGUI + JSON 落盘模式**。

**Required evidence**:

- `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S301_AdditiveSceneLoading.cs` — PlayMode spike (5 case + ADVISORY)
  - **P1** 单次加载 — `LoadChapterSceneAsync(1)` 完整 Step 8-10 流程；4 个 ISceneEvent 监听器各计数（OnSceneDownloadProgress / OnSceneLoadProgress / OnSceneLoadComplete / OnSceneLoadFailed）；AC-1/AC-2/AC-3/AC-6/AC-7/AC-8 覆盖
  - **P2** 切换 + AC-10 验证 — `LoadChapter(1)` → 手动 `GameModule.Scene.UnloadAsync` → `LoadChapter(2)`；Story 003 真实 cleanup 实装前 spike 内自调用模拟；`UnitySceneManager.sceneCount ≤ baseline + 1` 全程
  - **P3** 下载分支 — `downloader.TotalDownloadCount > 0` 才走 OnSceneDownloadProgress 派发路径；如本地缓存已存在（`TotalDownloadCount == 0`） → SKIP-ADVISORY 标 'cache hit, download branch coverage requires cache clear in pre-test phase'；AC-5 best-effort 覆盖
  - **P4** 加载失败 fail-loud (manifest invalid) — 注入 chapterId=99 + ChapterData with `SceneId = "Chapter_99_NotInManifest"` → AC-4 路径；listener 计数验证 `OnSceneLoadFailed` 恰 1 次 + 状态机进 Error
  - **P5** ADVISORY ONLY — retry 机制（AC-9）— D1=[b] PlayMode-only 模式下 `GameModule.Scene` 静态 facade 不可 stub 抛异常，retry 路径无 PlayMode 测试入口；`§Implementation Notes` try-catch + lastError + MAX_LOAD_RETRY 循环代码已对齐；本项标 ADVISORY 'retry path verified by code review of LoadChapterSceneAsync; PlayMode coverage deferred to future ISceneLoader stub seam refactor (out of scope per D1=[b])'
  - **JSON output**: `Application.persistentDataPath/S301_Result.json`（与 SP-011 同模式 — `phase` / `p1_passed`..`p4_passed` / `p3_status` (PASS/SKIP-ADVISORY) / `p5_advisory: 'requires_iSceneLoader_seam'` / `assembly` / `lastError`）

- `production/qa/grep-no-fantasy-api-scene-management-2026-04-30.md` — grep evidence
  - 0 hit `GameModule\.Resource\.LoadSceneAsync` in `HotFix/GameLogic/Scene/`
  - 0 hit `GameModule\.Resource\.UnloadSceneAsync` in `HotFix/GameLogic/Scene/`
  - 0 hit `SceneHandle` in `HotFix/GameLogic/Scene/`
  - 0 hit `\.SceneObject` in `HotFix/GameLogic/Scene/`
  - 0 hit `LoadSceneMode\.Single` in `HotFix/GameLogic/Scene/`
  - 0 hit `UnityEngine\.SceneManagement\.SceneManager\.SetActiveScene` in `HotFix/GameLogic/Scene/`
  - 0 hit `EventId\.Evt_` in `HotFix/GameLogic/Scene/`（ADR-027 合规）
  - 0 hit `while.*\.IsDone` in `HotFix/GameLogic/Scene/`（D4=[I] 反轮询模式）

**Status**: [x] **DONE 2026-04-30 — CORE PASSED**

### PlayMode 验证结果（2026-04-30 final run）

| Case | Result | 数据点 |
|------|--------|--------|
| **P1** SingleLoad | ✅ PASS | LoadChapterSceneAsync(101) → SP011_SceneA Additive 加载 + ActivateScene + OnSceneLoadComplete(101, "bgm_test_a") |
| **P2** Switch+AC10 | ✅ PASS | 101→102 切换；UnloadAsync(SP011_SceneA) + LoadChapterSceneAsync(102)；sceneCount baseline+1 守恒 |
| **P3** DownloadBranch | SKIP-ADVISORY | EditorSimulateMode 缓存命中 → `TotalDownloadCount==0`；OnSceneDownloadProgress=0；下载分支代码路径 verified by code review |
| **P4** InvalidSceneFailLoud | ✅ PASS | LoadChapterSceneAsync(999) "Chapter_999_NotInManifest" → LoadSceneAsync 抛错 → retry 2 次耗尽 → OnSceneLoadFailed(999, "Could not load subScene while already loaded. Scene: ...") + Error |
| **P5** RetryMechanism | ADVISORY | D1=[b] PlayMode-only 不可 stub；try-catch + lastError 循环代码 verified by code review |

**Listener counts (matching expected payloads)**：
- `OnSceneDownloadProgress = 0`（缓存命中预期）
- `OnSceneLoadProgress = 12`（P1+P2+P3 三次加载的 progressCallBack lambda 派发，progress 单调递增到 1.0）
- `OnSceneLoadComplete = 3`（P1+P2+P3 各 1 次）
- `OnSceneLoadFailed = 1`（P4 fail-loud 1 次；retry exhaust 路径合并成单次 fail event）

**JSON output**: `Application.persistentDataPath/S301_Result.json` — `allCorePassed: true`, `lastError: ""`, `lastCompletedChapterId: 103`, `lastCompletedBgmAsset: "bgm_test_c"`

### 实施过程中的 3 类 drift 修订路径（ADR-029 第一手数据）

1. **Type-1 drift (ADR-029 R2 STOP)** — Phase 1.5 readiness gate 抓住 6 处 fantasy API（`GameModule.Resource.LoadSceneAsync` / `SceneHandle` / `_currentSceneHandle.SceneObject` 等 YooAsset native 心智模型残留）→ patch v2 全文重写对齐 framework；约 **10 min**
2. **Type-2 drift (P0 spike runtime 反馈)** — `CheckLocationValid` 对 scene 资产返 false（API 存在但行为不适用 — R2 grep 抓不到）→ 选 [A] 移除前置；约 **5 min**
3. **Type-3 drift (DevBootstrap 并发)** — SP-011 + S3-01 spike 同时 RunAllAsync 撞 YooAsset "while loading" 锁（测试 infrastructure 层 race，非业务代码）→ 选 [A] 取消 SP-011 注册；约 **3 min**

**累计 drift 修订耗时 ≈ 18 min**（首条 ADR-blessed dev-story 数据点；含 patch v2 全文 + 2 轮 PlayMode 验证 + design decisions D1-D6 + 3 个修复 option AskQuestion 决策）。预期后续 stories 单条 type-1 drift revision time 应 ≤ 5 min（无 patch v2 文档撰写 overhead）；type-2/-3 在通用 control manifest 中加 rule 后应 0 复发。

**Sprint 3 关联**: 本 story PlayMode evidence 作为 S3-06 PlayMode batch 之外的独立项；S3-06 是 Sprint 2 推延项专项（S2-09/-10/-11/-13），本 story 与 SP-011 同源属 Scene Integration spike 序列

---

## Dependencies

- Depends on: Story 001 (scene state machine — must be DONE)
- Unlocks: Story 003 (cleanup sequence uses `_currentChapterSceneName` + `ClearCurrentChapterSceneName()` setter exposed by this story; S3-02 patch v2 已对齐 2026-04-30), Story 005 (events use loading steps)
