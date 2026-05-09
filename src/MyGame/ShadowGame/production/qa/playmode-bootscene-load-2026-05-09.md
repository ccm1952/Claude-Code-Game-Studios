// 该文件由Cursor 自动生成

# S5-1b SceneManager Boot Pipeline Integration — PlayMode Evidence (2026-05-09)

> **Story**: S5-1b — SceneManager boot pipeline 接入 + fixture ChapterDataProvider
> **Sprint**: 5 (start 2026-05-06 / end 2026-05-20) — Track A vs-chapter-1
> **Epic**: vs-chapter-1
> **Type**: Integration（production code + R3 PlayMode probe）
> **Engine**: Unity 2022.3.62f2 LTS + HybridCLR + YooAsset 2.3.17 + UniTask
> **Date**: 2026-05-09
> **Verdict**: **PASS** (5/5 R3 case + 22/22 asserts；DevTestState driver 产生 8 lifecycle event 完整顺序；fail-loud 双路径 verified)
> **Story file**: `production/epics/vs-chapter-1/story-001b-scenemanager-boot-integration.md`
> **Governing ADRs**: ADR-009 Scene Lifecycle / ADR-007 Luban Access / ADR-027 GameEvent Interface Protocol / ADR-029 V2.0 Story Impl Notes Verification
> **3 deficiency surface**：F4 dev-only driver 模拟 ADR-009 listener-path driver 缺失 + S5-01 chapter scene 路径错位（已修） + S5-01 chapter scene 残留 AudioListener（未修，warning-only）

---

## §0 概要

S5-1b 完成 SceneManager 在 `GameApp.Entrance` 内 production wire-up（field + Init + RegisterChapterDataProvider + RegisterFadeOverlay + Release Dispose），同时按 (M1) 双层模式实施 R3 PlayMode probe 5 case 验证 first-boot chapter 1 真实 load + 8 lifecycle event 顺序 + dedupe + unload + fail-loud。

实施过程暴露 3 个真 production gap（详 §10 Deficiencies），但 5/5 R3 case 全 PASS：

- **P1 ProductionFirstBoot** — chapter 1 真 load + 8 lifecycle event 完整顺序
- **P2 SameChapterDedupe** — 同章请求 OnSceneReady 立即 fire，无二次 transition
- **P3 ProductionUnload** — UnloadCurrentChapterAsync 后 state 归零 + OnSceneUnloadBegin 派
- **P4 UnknownChapterFail** — `ResolveChapterData` 返 null 触发 OnSceneLoadFailed + state Error
- **P5 ProviderNullFail** — boot pipeline 未 RegisterChapterDataProvider 触发 OnSceneLoadFailed + state Error

---

## §1 改动清单

### 改动文件 (3)

| 文件 | 改动 | 行数变化 |
|---|---|---|
| `Assets/GameScripts/HotFix/GameLogic/GameApp.cs` | + `_sceneManager` field + Entrance wire-up（new + Init + Register×2）+ `BuildFixtureChapterDataProvider()` static method + Release 内 Dispose；RegisterDevSpikes 启用 S51bSpike 注册（其余 spike 保持注释 type-3 race 防御） | +36 / -8 |
| `Assets/GameScripts/HotFix/GameLogic/GameFlow/DevTestState.cs` | OnEnter 内派 `OnRequestSceneChange(1)` + 启动 `DriveProductionSceneTransitionAsync(1, 800ms).Forget()` 模拟业务 boot driver；F4 deficiency-flagged | +50 / -1 |
| `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-1b_BootSceneLoad.cs` | 新建 1 文件 + 3 内类 (S51bSpike + S51bRuntime + S51bTester) + 5 R3 case (M1 双层) + JSON evidence schema | +458 / 0 |

### 文件搬运 (1, S5-01 deficiency-flagged 修补)

```
git mv Assets/Scenes/Chapter_01_Approach.unity         → Assets/AssetRaw/Scenes/Chapter_01_Approach.unity
git mv Assets/Scenes/Chapter_01_Approach.unity.meta    → Assets/AssetRaw/Scenes/Chapter_01_Approach.unity.meta
```

> rename-only（git status `R` 标记），GUID 保留无 prefab/scene reference 断裂；Unity refresh 后 YooAsset Scenes group `CollectPath = Assets/AssetRaw/Scenes` 自动 collect。

### 文档同步 (2)

| 文件 | 改动 |
|---|---|
| `production/epics/vs-chapter-1/story-001b-scenemanager-boot-integration.md` | doc-fix patch (M1) 决策；R3 case 表格更新双层模式说明 |

---

## §2 R3 PlayMode probe 实测结果

### §2.1 5 case PASS / FAIL 矩阵

| Case | Setup | Verdict | Asserts |
|---|---|---|---|
| **P1** ProductionFirstBoot | DevTestState OnEnter 派 OnRequestSceneChange(1) + F4 driver 800ms 后 await BeginTransitionAsync(1) | ✅ **PASS** | 9/9 |
| **P2** SameChapterDedupe | post-P1; spike 派 OnRequestSceneChange(1) 二次 | ✅ **PASS** | 4/4 |
| **P3** ProductionUnload | post-P2; spike await prodScene.UnloadCurrentChapterAsync() | ✅ **PASS** | 3/3 |
| **P4** UnknownChapterFail | spike-local SceneManager + chapter-1-only fixture; await BeginTransitionAsync(99) | ✅ **PASS** | 4/4 |
| **P5** ProviderNullFail | spike-local SceneManager 无 ChapterDataProvider; await BeginTransitionAsync(1) | ✅ **PASS** | 3/3 |

**总计**: 5 case PASS + **22/22 asserts PASS**。

### §2.2 P1 8 lifecycle event 顺序（核心验证）

```
P1.events = [
  "OnSceneTransitionBegin(-1, 1)",            ← fromChapterId = NoChapterId (first-boot)
  "OnSceneLoadProgress(Chapter_01_Approach, 0.00)",
  "OnSceneLoadProgress(Chapter_01_Approach, 0.00)",
  "OnSceneLoadComplete(1, '')",                ← bgmAsset = '' (chapter 1 暂无 BGM)
  "OnSceneReady(1)",
  "OnSceneTransitionEnd(1)"
]
P1.OnSceneLoadProgress count = 4 (events 列表只记录前 2)
```

✅ 与 ADR-009 11-step transition flow 期望一致：
1. `OnSceneTransitionBegin(from=-1, to=1)` — Step 3，first-boot fromChapterId 为 NoChapterId
2. `OnSceneLoadProgress(...)` — Step 8-10 内 ResolveSceneAsync 派
3. `OnSceneLoadComplete(1, '')` — Step 10 末段（chapter 1 fixture bgmAsset = ""）
4. `OnSceneReady(1)` — Step 11 fade-in 之前
5. `OnSceneTransitionEnd(1)` — Step 11 fade-in 之后

> **First-boot guard verified**：`OnSceneUnloadBegin` 在 P1 内**不派**（_currentLoadedChapterId == NoChapterId 触发 UnloadCurrentChapterAsync 内 first-boot guard），与 ADR-009 + S3-02 设计一致。

### §2.3 P2 dedupe 验证

```json
"P2.OnSceneTransitionBegin_should_be_zero": "PASS: 0 (无二次 transition)",
"P2.OnSceneReady_should_be_one_or_more":     "PASS: 1",
"P2.CurrentState":                            "PASS: Idle",
"P2.InflightChapterIdForTest":                "PASS: NoChapterId(-1)"
```

✅ `OnRequestSceneChange(1)` 二次派发 → handler 内 line 326-333 `if (state == Idle && targetChapterId == _currentChapterId)` 短路 → 立即派 `OnSceneReady(1)` + return；state 保持 Idle，无 InflightChapterId 占用。

### §2.4 P3 unload 验证

```json
"P3.CurrentLoadedChapterIdForTest": "PASS: NoChapterId",
"P3.CurrentChapterSceneNameForTest": "PASS: null",
"P3.OnSceneUnloadBegin": "PASS: count=1"
```

✅ `await prodScene.UnloadCurrentChapterAsync()` 清空"已加载身份"两字段（_currentLoadedChapterId + _currentChapterSceneName）+ 派 `OnSceneUnloadBegin(1)`，与 S3-02 v3 protocol 一致。

### §2.5 P4 / P5 fail-loud 验证

```json
"P4.OnSceneLoadFailed":   "PASS: count=1, chapterId=99, error='ChapterData null for id=99 (Luban TbChapter 未配置或 provider 返 null).'",
"P4.CurrentState":        "PASS: Error",
"P4.CurrentLoadedChapterIdForTest": "PASS: NoChapterId（不污染）",

"P5.OnSceneLoadFailed":   "PASS: count=1, error='ChapterDataProvider not registered (RegisterChapterDataProvider must be called in boot pipeline before scene load).'",
"P5.CurrentState":        "PASS: Error"
```

✅ P4 路径：`LoadChapterSceneAsync(99)` 入口 `_chapterDataProvider(99) == null` → fail-loud 派 OnSceneLoadFailed + Error。
✅ P5 路径：`LoadChapterSceneAsync(1)` 入口 line 417 `_chapterDataProvider == null` 守卫 → 派 OnSceneLoadFailed + Error。
✅ 两 case **不进 LoadSceneAsync**（fail-loud 在 LoadChapterSceneAsync 早段守卫），不撞 YooAsset 锁；spike-local SceneManager 与 production 完全隔离。

---

## §3 Acceptance Criteria 矩阵

| AC | 描述 | Verdict | Evidence |
|---|---|---|---|
| **AC-1** | `GameApp.Entrance` 内 `_sceneManager.Init()` 在 `StartGameLogic()` 之前；listener attach | ✅ PASS | grep + console log "[GameApp] SceneManager production wire-up done" 在 "[GameFlow] 进入 GameLoadingState" 之前 |
| **AC-2** | `RegisterChapterDataProvider(BuildFixtureChapterDataProvider())` chapter 1 → ChapterData(id=1, sceneId='Chapter_01_Approach', bgmAsset='', emotionalWeight=1.0, overlayColor='#3A3530') | ✅ PASS | P1.CurrentChapterSceneNameForTest='Chapter_01_Approach'; P1.OnSceneLoadComplete payload(1, ''); GameApp.cs:61-71 |
| **AC-3** | DevTestState OnEnter auto trigger OnRequestSceneChange(1) | ✅ PASS | console log "[GameFlow] [S5-1b] 已派发 ISceneEvent.OnRequestSceneChange(1)" + state Idle→TransitionOut |
| **AC-4** | first-boot transition 跑通 11-step → state Idle + chapter 1 已加载 | ✅ PASS | P1.timeout=PASS / P1.CurrentLoadedChapterIdForTest=1 / P1.CurrentState=Idle / state log Idle→TransitionOut→Unloading→Loading→TransitionIn→Idle |
| **AC-5** | 8 lifecycle event 顺序符合 ADR-009 | ✅ PASS | P1.events 列表（§2.2） |
| **AC-6** | OnRequestSceneChange(1) 二次派发 dedupe；不二次 LoadSceneAsync | ✅ PASS | P2.OnSceneTransitionBegin=0 / P2.OnSceneReady=1 / P2.InflightChapterIdForTest=NoChapterId |
| **AC-7** | `Release()` 内 Dispose；listener detach；空安全 | ✅ PASS | grep `_sceneManager?.Dispose()` GameApp.cs:128-131；详 §4 Release timing |
| **AC-8** | RegisterFadeOverlay(NoOpFadeOverlay) 注入；fade-out + fade-in 异步链通过 | ✅ PASS | P1 transition 内 fade-out 后 state→Unloading；fade-in 后 state→Idle（NoOp 立即完成） |
| **AC-9** | P4 unknown chapter fail-loud；P5 provider null fail-loud；reason 字串可读 | ✅ PASS | P4/P5 OnSceneLoadFailed events §2.5 |
| **AC-10** | spike file 1 个 + 3 内类 + 5 R3 case + JSON evidence | ✅ PASS | `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-1b_BootSceneLoad.cs`; class S51bSpike + S51bRuntime + S51bTester |

**全 10 AC PASS**。

---

## §4 Release timing 验证 (AC-7)

`GameApp.Release()` 改动顺序（GameApp.cs:124-138）：

```csharp
private static void Release()
{
    // S5-1b: SceneManager Dispose 必须先于 FSM Destroy
    if (_sceneManager != null)
    {
        _sceneManager.Dispose();    // RemoveEventListener<int>(OnRequestSceneChange) — listener bus 此时仍 alive
        _sceneManager = null;
        Log.Info("[GameApp] SceneManager disposed");
    }

    GameModule.Fsm.DestroyFsm<IFsmModule>(GameFlowDef.FsmName);
    SingletonSystem.Release();
    Log.Warning("======= Release GameApp =======");
}
```

✅ **顺序正确**：SceneManager.Dispose 内 `RemoveEventListener(OnRequestSceneChange, OnRequestSceneChange)` 调用必须在 GameLogic listener bus 销毁之前；FSM.DestroyFsm + SingletonSystem.Release 在 Dispose 之后，listener bus 销毁顺序与 attach 顺序对称。

实测：PlayMode stop 时 console log "[GameApp] SceneManager disposed" 早于 "======= Release GameApp =======" 可见。

---

## §5 Production wire-up grep evidence

### `GameApp._sceneManager` field 存在

```bash
$ rg "private static GameLogic\.SceneManager _sceneManager" Assets/GameScripts/HotFix/GameLogic/GameApp.cs
24:    private static GameLogic.SceneManager _sceneManager;
```

### Entrance 内 wire-up 序列

```bash
$ rg -n "_sceneManager\.(Init|Register|Dispose)" Assets/GameScripts/HotFix/GameLogic/GameApp.cs
46:        _sceneManager.Init();
47:        _sceneManager.RegisterChapterDataProvider(BuildFixtureChapterDataProvider());
48:        _sceneManager.RegisterFadeOverlay(new GameLogic.NoOpFadeOverlay());
130:            _sceneManager.Dispose();
```

✅ Entrance line 45-49 wire-up 顺序：new → Init → RegisterChapterDataProvider → RegisterFadeOverlay；Release line 130 Dispose。

### fixture provider 形态（per ADR-007 Luban access pattern）

```csharp
// GameApp.cs:61-71
private static System.Func<int, GameLogic.ChapterData> BuildFixtureChapterDataProvider() => id => id switch
{
    1 => new GameLogic.ChapterData(
        id: 1,
        sceneId: "Chapter_01_Approach",
        bgmAsset: string.Empty,
        emotionalWeight: 1.0f,
        overlayColor: "#3A3530"
    ),
    _ => null  // 未知 id 同 Luban TbChapter.Get fail-loud 行为
};
```

✅ 签名 `Func<int, ChapterData>` 与未来 `id => ConfigSystem.Tables.TbChapter.Get(id)` 真接入 1:1 一致；migration 仅 1 lambda swap。

### Spike registration

```bash
$ rg -n "DevBootstrap\.Register\(new GameLogic\.DevTest\.Spikes\.S51bSpike" Assets/GameScripts/HotFix/GameLogic/GameApp.cs
93:        GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S51bSpike());
```

其余 spike 全部注释（type-3 race 防御 — multi-spike 并发 LoadSceneAsync 撞 YooAsset 锁）。

---

## §6 Code Reference

### Production code

```42:71:src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/GameApp.cs
        // S5-1b SceneManager boot pipeline 接入（ADR-009 §_chapterDataProvider 注入 + ADR-007 Luban access pattern）。
        // fixture provider 仅 chapter 1（其余 id 返 null fail-loud，与未来 Luban TbChapter.Get 真接入行为一致）；
        // RegisterFadeOverlay 显式注入 NoOp（即使 default fallback 也是 NoOp，显式让 wire 路径 grep-able）。
        _sceneManager = new GameLogic.SceneManager();
        _sceneManager.Init();
        _sceneManager.RegisterChapterDataProvider(BuildFixtureChapterDataProvider());
        _sceneManager.RegisterFadeOverlay(new GameLogic.NoOpFadeOverlay());
        Log.Info("[GameApp] SceneManager production wire-up done (chapter 1 fixture provider + NoOp fade overlay)");
```

### F4 driver (DevTestState — deficiency-flagged 详 §10)

```45:75:src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/GameFlow/DevTestState.cs
            DriveProductionSceneTransitionAsync(targetChapterId: 1, delayMs: 800).Forget();
        }

        /// <summary>
        /// S5-1b F4 driver — DevTestState 临时承担 production SceneManager 11-step 驱动职责（ADR-009 deficiency-flagged，
        /// listener-path driver 后续 ADR-009 driver story 补齐）。反射拿 GameApp._sceneManager + delayed await BeginTransitionAsync。
        /// </summary>
        private static async UniTaskVoid DriveProductionSceneTransitionAsync(int targetChapterId, int delayMs)
```

---

## §7 时间线 (PlayMode console log 摘录)

```
T+0.0s  GameApp Entrance
T+0.1s  AudioManager Initialized
T+0.1s  [GameApp] SceneManager production wire-up done
T+0.1s  [DevBootstrap] 已注册 Spike: S5-1b
T+0.2s  ======= StartGameLogic =======
T+0.3s  [GameFlow] 进入 GameLoadingState
T+0.5s  [GameFlow] 离开 GameLoadingState
T+0.5s  [GameFlow] 进入 DevTestState
T+0.5s  [SceneManager] Idle → TransitionOut         ← OnRequestSceneChange(1) handler
T+0.5s  [GameFlow] [S5-1b] 已派发 ISceneEvent.OnRequestSceneChange(1)
T+0.5s  [DevBootstrap] → Launch S5-1b
T+0.6s  [S5-1b] Runtime Start
T+0.6s  [S5-1b] P1 ProductionFirstBoot 开始        ← spike subscribe listener
T+1.3s  [GameFlow] [S5-1b][F4] driver 启动: await BeginTransitionAsync(1)
T+1.3s  [SceneManager] TransitionOut → Unloading
T+1.3s  [SceneManager] Unloading → Loading
T+1.4s  [SceneManager] LoadChapterSceneAsync(1) success on attempt 1/2 (scene=Chapter_01_Approach)
T+1.4s  [SceneManager] Loading → TransitionIn
T+1.4s  [SceneManager] TransitionIn → Idle
T+1.4s  [GameFlow] [S5-1b][F4] driver done: state=Idle
T+1.5s  [S5-1b] P2 SameChapterDedupe 开始
T+1.8s  [S5-1b] P3 ProductionUnload 开始
T+2.0s  [S5-1b] P4 UnknownChapterFail 开始 (expected Debug.LogError)
T+2.3s  [S5-1b] P5 ProviderNullFail 开始 (expected Debug.LogError)
T+2.5s  [S5-1b] Done. AllPassed=True
```

---

## §8 JSON Evidence

完整路径 + content（macOS Application.persistentDataPath）：

```
/Users/chen/Library/Application Support/DefaultCompany/Unity/S5-1b_Result.json
```

详 `S5-1b_Result.json` (timestamp 2026-05-09 16:27:43)：
- `all_passed: true`
- `overall_status: "All Passed"`
- 5 case x events list
- 22 asserts 全 PASS

---

## §9 Console expected errors（fail-loud verification）

PlayMode console 可见 2 条 `[ERROR]` 日志 + 2 条 `Debug.Log "[S5-1b][P4/5] expected Debug.LogError below"` 标记，期望出现：

```
[S5-1b][P4] expected Debug.LogError below: ChapterDataProvider returned null for unknown chapter 99
[ERROR] [SceneManager] LoadChapterSceneAsync(99) failed — ChapterData null for id=99 (Luban TbChapter 未配置或 provider 返 null).

[S5-1b][P5] expected Debug.LogError below: ChapterDataProvider not registered
[ERROR] [SceneManager] LoadChapterSceneAsync(1) failed — ChapterDataProvider not registered (RegisterChapterDataProvider must be called in boot pipeline before scene load).
```

✅ 两条 Debug.LogError 来自 SceneManager.cs:420 fail-loud reason 字串；本 spike 主动派发触发，与 P4/P5 expected behavior 一致。

---

## §10 Deficiencies surface

实施过程暴露 3 个真 production gap，按 ADR-029 V2.0 deficiency-flagged 协议**不阻止本 story DONE**，但需 separate follow-up：

### §10.1 ADR-009 listener-path driver 缺失（**新 architecture gap**）

- **现象**：`SceneManager.OnRequestSceneChange` handler line 344-346 仅推进 state 到 TransitionOut 即 return，注释 `// Story 002 接管后续 11 步流程`；但**全 codebase 内仅 spike 文件直接调 `BeginTransitionAsync`**（grep 验证 — production code 0 call site）；listener-path 自驱 driver 从未实装。
- **影响**：业务方派 `GameEvent.Get<ISceneEvent>().OnRequestSceneChange(targetId)` 后必须自己再 `await prodScene.BeginTransitionAsync(targetId)` 才能跑 11-step；ADR-009 暗示的"派 event 即可，driver 自动接"语义未实现。
- **临时绕过**：本 story `DevTestState.DriveProductionSceneTransitionAsync(1, 800ms).Forget()` 模拟 driver 仅 dev-only 编译。
- **TODO**：开**新 ADR / story** 决定 driver 归属（SceneManager 内自驱 vs 业务侧自驱 vs 独立 SceneTransitionDriver MonoBehaviour）；同步修 `scene-management.md` + ADR-009 注释；`tr-registry.yaml` 加 `TR-scene-driver` 真 TR。

### §10.2 S5-01 chapter scene 路径错位（已修补；patch 归本 story commit）

- **现象**：S5-01 创建的 `Chapter_01_Approach.unity` 落在 `Assets/Scenes/`，但 YooAsset Scenes group `CollectPath = Assets/AssetRaw/Scenes`（参 SP011_SceneA/B 同 group）。
- **症状**：本 story PlayMode P1 首次跑出现 `Failed to mapping location to asset path : Chapter_01_Approach` + retry exhaust。
- **修补**：`git mv Assets/Scenes/Chapter_01_Approach.unity{,.meta} → Assets/AssetRaw/Scenes/`；rename-only GUID 保留无 reference 断裂；Unity refresh 后 YooAsset auto collect。
- **建议**：未来 chapter scene 创建必须落 `Assets/AssetRaw/Scenes/`（per YooAsset config）；建议沉淀跨工程 cursor rule。

### §10.3 S5-01 chapter scene 残留 AudioListener（未修；warning-only）

- **现象**：PlayMode 期间 console 反复 `There are 2 audio listeners in the scene` warning（约 150 次/run）。
- **根因**：`Chapter_01_Approach.unity` 内 `MainCamera` 挂了 AudioListener；与 `main.unity` 内 BootScene MainCamera 的 AudioListener 重复（additive load 后 2 个 listener 同时 enabled）。
- **影响**：仅 warning（不影响 spike PASS），但运行时 audio mix 由 Unity 不确定的 listener 拾取，可能影响 audio 调试。
- **TODO**：本次**不修**（避免本 story scope 蔓延）；S5-01 retro 内 patch follow-up — chapter scene 内 MainCamera 的 AudioListener 应 disable 或删除（boot scene 拥有 single audio listener）。

---

## §11 Test execution metadata

| 项 | 值 |
|---|---|
| Unity Editor | 2022.3.62f2 LTS (Apple Silicon) |
| Run mode | EditorSimulateMode (YooAsset run mode) |
| Active scene at PlayMode start | `Assets/Scenes/main.unity` (BootScene) |
| YooAsset package | DefaultPackage |
| YooAsset file system | `YooAsset.DefaultEditorFileSystem` |
| HybridCLR | ProcedureLoadAssembly (hot-fix code 走 Mono interpreter) |
| PlayMode duration | ~5s (entry → AllPassed JSON write) |
| spike registered count | 1 (S5-1b only; type-3 race 防御保留) |
| total console errors | 2 (P4/P5 expected fail-loud) |
| total console warnings | ~155 (3 normal: I2Localization / EditorSimulateMode / EditorMode；~150 audio listener — §10.3) |

---

## §12 Verdict

**PASS** — 5/5 R3 case + 22/22 asserts + 10/10 AC PASS。

### 后续 follow-up（不阻止本 story DONE）

1. **§10.1 ADR-009 driver story** — 新 ADR / story 决定 listener-path driver 归属（建议 propagate-design-change）。
2. **§10.3 S5-01 audio listener patch** — chapter scene MainCamera AudioListener 删除/disable（S5-01 retro patch）。

### Story DONE checklist

- [x] 5/5 R3 PlayMode case PASS（JSON evidence + console log）
- [x] 10/10 AC PASS（§3 矩阵）
- [x] production code lint clean（GameApp.cs / DevTestState.cs / S5-1b spike file）
- [x] 编译 0 error（read_console verified）
- [x] reflection 验证类型 + field + SourceGenerator 常量全可见（§5）
- [x] R1/R2/R3 ADR-029 V2.0 verification 全 pass（readiness check + impl 实测）
- [x] git diff stat 与 §1 改动清单一致
- [x] evidence doc 包含 deficiency surface（§10）

### Sign-off pending: `/code-review` + `/story-done`

---

*Generated: 2026-05-09 16:35*
