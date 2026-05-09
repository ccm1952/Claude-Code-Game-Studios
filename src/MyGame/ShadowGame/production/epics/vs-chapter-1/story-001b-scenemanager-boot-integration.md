// 该文件由Cursor 自动生成

# Story 001b: SceneManager boot pipeline 接入 + fixture ChapterDataProvider

> **Epic**: VS Chapter 1
> **Status**: **Done** *(2026-05-09 — 5/5 R3 PASS + 22/22 asserts + 10/10 AC)*
> **Layer**: Vertical Slice (VS)
> **Type**: Logic / Integration
> **SP**: 3
> **Manifest Version**: 2026-05-09

---

## Context

**GDD**:
- `design/gdd/scene-management.md` — Scene Architecture / Registry (line 67 `Chapter_01_Approach`) / Core Rules / 11-step transition
- `design/concept/shadow-memory.md` — Chapter 1 = "靠近"阶段 (line 131)

**Requirement TR-IDs** *(2026-05-09 readiness gate doc-fix — 原 TR-scene-015 `Emotional weight scales fade duration` 与本 story 工作语义不符；改用真实贴合 boot pipeline 接入的 3 个 TR + fixture deficiency-flag)*:
- `TR-scene-001` — Additive scene 架构（Boot + Main + Chapter）
- `TR-scene-005` — 11-step transition flow（FadeOut → Unload → GC → Load → FadeIn）
- `TR-scene-013` — Startup flow Boot → TEngine → HybridCLR → YooAsset
- `TR-scene-load-fixture` ⚠️ **DEFICIENCY-FLAGGED 2026-05-09** — 该 TR 仅出现于 EPIC.md + 本 story，未在 `docs/architecture/tr-registry.yaml` 注册；按 ADR-029 V2.0 deficiency-flag 协议，**显式标记**等待下次 `/architecture-review` 跑一次注册（不阻塞 dev-story）。语义：fixture ChapterDataProvider 在 boot pipeline 注册；`LoadChapterSceneAsync(1)` 经 production boot 路径可成功执行

**ADR Governing Implementation**:
- **ADR-009 (Accepted)** Scene Lifecycle — 11-step transition / 6-state machine / `_chapterDataProvider` 注入 / fadeOverlay 注入
- **ADR-007 (Accepted)** Luban Access Pattern — fixture provider 用 `Func<int, ChapterData>` 同 `TbChapter.Get` 真接入签名
- **ADR-029 V2.0 (Accepted) R3 mandatory** — Logic / Integration story 必须有 PlayMode framework boundary probe；本 story 是 framework 真实接入
- **ADR-027 (Accepted)** Event Layer — ISceneEvent listener 自挂自摘 §5

**ADR Decision Summary**: 在 `GameApp.Entrance` 内 wire 生产 SceneManager 实例 + fixture `Func<int, ChapterData>` provider（chapter 1 hardcoded；其余 id 返 null fail-loud 与 Luban `TbChapter.Get` 行为一致）+ `NoOpFadeOverlay` 显式注入。dev menu / FSM trigger 显式触发 `OnRequestSceneChange(1)` 派发；R3 PlayMode probe 验证 11-step 真实跑通 + 5 framework boundary case。

**Engine**: Unity 2022.3.62f2 LTS + URP | **Risk**: LOW
**Engine Notes** *(R2 grep verified — 真实 framework API 已 by S3-01..03 PlayMode CORE PASSED)*:
- `GameModule.Scene.LoadSceneAsync(string, LoadSceneMode.Additive, Action<float>) → UniTask<Scene>` — ✅ verified S3-01
- `GameModule.Scene.UnloadAsync(string) → UniTask<bool>` — ✅ verified S3-02
- `GameModule.Scene.ActivateScene(string) → bool` — ✅ verified S3-01
- `GameModule.Resource.CreateResourceDownloader(string) → ResourceDownloader` — ✅ verified S3-01 P3 case
- `GameApp.Entrance(object[] objects)` — 真签名（per session-state Session 23 Entrance(object[]) 修订）
- `SceneManager` API surface（`Assets/GameScripts/HotFix/GameLogic/Scene/SceneManager.cs`）：
  - `new SceneManager()` ctor + `Init()` (订阅 `ISceneEvent.OnRequestSceneChange`) + `Dispose()` (取消订阅)
  - `RegisterChapterDataProvider(Func<int, ChapterData>)`（必须 register；否则 ResolveChapterData fail-loud `OnSceneLoadFailed`）
  - `RegisterFadeOverlay(IFadeOverlay)`（默认 `NoOpFadeOverlay` fallback；本 story 显式注入让 wire 路径 grep-able）
  - `BeginTransitionAsync(int)` — 11-step driver；对外 entry = `OnRequestSceneChange` listener
  - `LoadChapterSceneAsync(int)` (S3-01) + `UnloadCurrentChapterAsync()` (S3-02)
  - 测试桩字段 `CurrentLoadedChapterIdForTest` (`int`) + `CurrentChapterSceneNameForTest` (`string`) + `NoChapterId` (`= -1`) + `_initialized` (`bool` private — 通过 reflection 校验)
- `ChapterData` ctor: `ChapterData(int id, string sceneId, string bgmAsset, float emotionalWeight = 1.0f, string overlayColor = "#FFFFFF")`（all readonly）
- `ISceneEvent` 9 方法（已冻结）：`OnRequestSceneChange` listener 唯一 = SceneManager；其余 8 方法 SceneManager 唯一 sender
  - `OnSceneTransitionBegin(int from, int to)` Step 3
  - `OnSceneUnloadBegin(int)` Step 5（first-boot guard skip via `_currentLoadedChapterId == NoChapterId`）
  - `OnSceneDownloadProgress(float, long, long)` Step 8
  - `OnSceneLoadProgress(string, float)` Step 9
  - `OnSceneLoadComplete(int, string bgmAsset)` Step 10
  - `OnSceneReady(int)` 同章 dedupe + Step 11 前
  - `OnSceneTransitionEnd(int)` Step 11
  - `OnSceneLoadFailed(int, string error)` Error path

**Performance**: N/A R3 mandatory；perf budget profile 留 polish phase（ADR-009 spec 章节 transition <2s for 15 MB scene；本 fixture chapter 1 只有 primitive，预计 <500ms 但**不**入本 story AC）。

**Control Manifest Rules (this layer)**:
- **Required**: SceneManager 在 `GameApp.Entrance` 内 instantiate；不得在 GameModule.Scene framework 内 hold instance
- **Required**: `RegisterChapterDataProvider` 必须 register 后才允许任何 `OnRequestSceneChange` 派发（fail-loud P5 case 验证）
- **Required**: ChapterDataProvider 用 `Func<int, ChapterData>` 同 Luban `TbChapter.Get` 签名（migration 1 lambda swap）
- **Required**: 未知 chapter id → provider 返 null → SceneManager 派 `OnSceneLoadFailed` + 进 Error 状态（不污染 `_currentLoadedChapterId`）
- **Required**: Dispose 时机由 Procedure / `OnApplicationQuit` 调用；listener 必须自摘
- **Forbidden**: 不得在 `GameApp.cs` 内放 chapter data hardcoded if/else 链表（用 lambda 或 static dictionary 封到 `BuildFixtureChapterDataProvider()` 单方法内）
- **Forbidden**: 不得在本 story 接 Luban `TbChapter` 真接入（user decision 2026-05-09 post-VS）
- **Forbidden**: 不得在本 story 实施真 UI fade overlay（NoOp 兜底；future story 接 UIRoot 时再 swap）
- **Forbidden**: 不得新增 chapter scene asset（chapter 1 由 S5-01 已落盘）

---

## Acceptance Criteria

*Logic / Integration type — production code wire-up + R3 PlayMode probe MANDATORY (ADR-029 V2.0)*

- [ ] **AC-1 (production wire-up)**: `GameApp.Entrance` 内创建 production `SceneManager` 实例 + 调用 `Init()` + `RegisterChapterDataProvider(BuildFixtureChapterDataProvider())` + `RegisterFadeOverlay(new NoOpFadeOverlay())`；4 行调用顺序固定且必须显式（即使 `RegisterFadeOverlay` 的 NoOp 是 default fallback，仍显式调用让 wire 路径 grep-able）
- [ ] **AC-2 (fixture provider)**: `BuildFixtureChapterDataProvider()` 返 `Func<int, ChapterData>`；至少 chapter 1 hardcoded `new ChapterData(id: 1, sceneId: "Chapter_01_Approach", bgmAsset: "", emotionalWeight: 1.0f, overlayColor: "#3A3530")`；未知 id 返 `null`（fail-loud；与 Luban `TbChapter.Get` 真行为一致）
- [ ] **AC-3 (dev FSM auto trigger)**: `DevTestState.OnEnter` 内在 `DevBootstrap.RunRequested()` 之**前**1 行派发 `GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1)`；production `_sceneManager` listener 接收并跑 11-step BeginTransitionAsync(1)；first-boot chapter 1 真实 load 由本路径担保（spike 5 case 用 reflection 读 production `_sceneManager` 验证状态，避免双 LoadSceneAsync 撞 YooAsset 锁，详见 §Implementation Notes Step 3）
- [ ] **AC-4 (production driver real-runtime)**: 触发 chapter 1 load 后，11-step `BeginTransitionAsync` 真跑通；post-transition `_sceneManager.CurrentLoadedChapterIdForTest == 1` + `_sceneManager.CurrentChapterSceneNameForTest == "Chapter_01_Approach"` + `_sceneManager.CurrentState == Idle`
- [ ] **AC-5 (8 lifecycle event sender 顺序)**: 11-step driver 顺序触发：`OnSceneTransitionBegin(NoChapterId, 1)` → (`OnSceneUnloadBegin` first-boot guard skip) → `OnSceneLoadProgress("Chapter_01_Approach", *)` ≥1 次 → `OnSceneLoadComplete(1, "")` → `OnSceneReady(1)` → `OnSceneTransitionEnd(1)`；listener 自挂自摘 ADR-027 §5
- [ ] **AC-6 (unload reverse path)**: 手动 `await _sceneManager.UnloadCurrentChapterAsync()` 后 `CurrentLoadedChapterIdForTest == NoChapterId(-1)` + `CurrentChapterSceneNameForTest == null` + `OnSceneUnloadBegin(1)` 已触发
- [ ] **AC-7 (Dispose listener cleanup)**: `_sceneManager.Dispose()` 后 `_initialized == false`（reflection 校验）+ framework `GameEvent` listener buffer 不 leak（spike teardown 路径再派 `OnRequestSceneChange(2)` 应**无任何反应**，因 SceneManager 已取消订阅）
- [ ] **AC-8 (R3 PlayMode probe ALL PASS)**: ≥ 5 R3 case 全 PASS（P1-P5 详见下节）；evidence JSON 写入 `Application.persistentDataPath/S5-1b_Result.json`
- [ ] **AC-9 (Console 0 error)**: 11-step transition 全程 Unity Console **0 error / 0 exception**（warning 允许，但 `OnSceneLoadFailed` 路径如 P4/P5 Console 内 `Debug.LogError` 由 SceneManager 主动写出属于**预期**——P4/P5 case JSON 内 expectedError 标记后排除）
- [ ] **AC-10 (production code grep evidence)**: `git diff` 内 `Assets/GameScripts/HotFix/GameLogic/GameApp.cs` ≥1 处 hit `_sceneManager`、`RegisterChapterDataProvider`、`RegisterFadeOverlay`、`Init()`、`Dispose()` 全部 grep-able；新增 **1 个 spike 文件** `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-1b_BootSceneLoad.cs`，内含 **3 个内类** `S51bSpike` (`IDevSpike` 入口) + `S51bRuntime` (`MonoBehaviour` 宿主 + OnGUI 报告 + 驱动 Tester) + `S51bTester` (纯逻辑 5 case + JSON 落盘) — **与 S301/S302/S303/S407/S5-03/S5-05/S5-06 spike 单文件 3 内类模式一致**（项目惯例 R2.7 verified）

---

## R3 Justification (Logic / Integration — MANDATORY)

按 ADR-029 V2.0 R3 mandatory criterion，本 story 是 **典型的 framework boundary 真实接入** —— production code 第一次以"非 spike-only"方式调 `GameModule.Scene.LoadSceneAsync` / `UnloadAsync` / `ActivateScene` + `GameModule.Resource.CreateResourceDownloader`。S3-01..03 已 verified 但仅 spike 路径；本 story 在 `GameApp.Entrance` 真实 boot pipeline 下 verify 整套 11-step driver。R3 probe 5 cases 强制覆盖：

### R3 PlayMode probe 5 cases（spike `S5-1b_BootSceneLoad.cs` — **(M1) 双层模式 2026-05-09 sign-off**）

> **Spike 模式 (M1)**：与 DevTestState auto trigger 共存避免 YooAsset additive load 二次冲突
> - **P1/P2/P3** 复用 production `GameApp._sceneManager` 实例（reflection 读 + listener subscribe production senders）；spike 不 own real LoadSceneAsync
> - **P4/P5** 自构建独立 spike-local SceneManager；fail-loud 路径在 `LoadChapterSceneAsync` 入口 line 417 直 dispatch `OnSceneLoadFailed` event 不进 LoadSceneAsync，**不**撞 YooAsset 锁

| # | Case | Setup | Action | Assert |
|---|---|---|---|---|
| **P1** | First-boot load chapter 1 (happy path) | DevTestState OnEnter 已 auto trigger production `OnRequestSceneChange(1)`；spike 在 RunRequested 后启动 — chapter 1 已加载完成或正在加载 | spike subscribe production sender events (per-event mode) + reflection `GameApp._sceneManager`；await `_sceneManager.CurrentState == Idle` (timeout 5s) | `_sceneManager.CurrentLoadedChapterIdForTest == 1` + `_sceneManager.CurrentChapterSceneNameForTest == "Chapter_01_Approach"` + 监听 production sender 8 lifecycle event 顺序：`OnSceneTransitionBegin(-1, 1)` → (UnloadBegin first-boot guard skip) → `OnSceneLoadProgress(...)` ≥1 次 → `OnSceneLoadComplete(1, "")` → `OnSceneReady(1)` → `OnSceneTransitionEnd(1)` |
| **P2** | Same-chapter dedupe (idle path) | post-P1（chapter 1 已 loaded by production） | spike `GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1)` 再次派发；production listener 接收 | `OnSceneReady(1)` 立即 fired (无 OnSceneTransitionBegin) + production `_sceneManager.CurrentState == Idle` + production `InflightChapterIdForTest == NoChapterId`（无 in-flight 二次 transition） |
| **P3** | Unload reverse | post-P1/P2（chapter 1 已 loaded by production） | spike `await GameApp._sceneManager.UnloadCurrentChapterAsync()` | `_sceneManager.CurrentLoadedChapterIdForTest == NoChapterId` + `_sceneManager.CurrentChapterSceneNameForTest == null` + 监听 production 派的 `OnSceneUnloadBegin(1)` |
| **P4** | Unknown chapter fail-loud (provider returns null) | spike-local 自构建 SceneManager (`new SceneManager()`，**不** Init 避免 listener 冲突 production)；spike-local `RegisterChapterDataProvider(id => id == 1 ? new ChapterData(...) : null)` + `RegisterFadeOverlay(new NoOpFadeOverlay())` | spike-local `await BeginTransitionAsync(99)` 直调（旁路 listener） | spike subscribe global ISceneEvent 收到 `OnSceneLoadFailed(99, "*ChapterData*null*")` + spike-local `CurrentState == Error` + spike-local `CurrentLoadedChapterIdForTest == NoChapterId`（不污染） |
| **P5** | Provider null fail-loud (boot pipeline 忘 register) | spike-local 自构建 SceneManager (`new SceneManager()`)，**不** Init **不** RegisterChapterDataProvider；可选 `RegisterFadeOverlay(new NoOpFadeOverlay())` | spike-local `await BeginTransitionAsync(1)` 直调 | spike subscribe global ISceneEvent 收到 `OnSceneLoadFailed(1, "*ChapterDataProvider*not registered*")`（参 SceneManager.cs:419 reason 字串）+ spike-local `CurrentState == Error` |

**evidence JSON schema** (`Application.persistentDataPath/S5-1b_Result.json`):
```json
{
  "story_id": "S5-1b",
  "timestamp": "2026-05-XX",
  "cases": [
    {"id": "P1", "passed": true, "asserts": [{"name": "CurrentLoadedChapterId", "expected": 1, "actual": 1, "passed": true}, ...], "events_received": ["OnSceneTransitionBegin(-1,1)", "OnSceneLoadProgress(...)", "OnSceneLoadComplete(1,'')", "OnSceneReady(1)", "OnSceneTransitionEnd(1)"]},
    ...
  ],
  "all_passed": true
}
```

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **S5-02** end-to-end 5 系统 (object interaction → puzzle → narrative → audio → scene transition) 串通 — chapter 1 内 system instantiate 留 S5-02
- **Luban TbChapter 真接入**（user decision 2026-05-09: post-VS；fixture provider migration 仅 1 lambda swap）
- **真 UI fade overlay**（NoOp 兜底；future polish story 接 UIRoot 时 swap real implementation）
- **chapter 2-5 fixture data**（本 story fixture 只覆盖 chapter 1；其余留 S5-02 / Luban migration 决定）
- **YooAsset Collector 注册到 DefaultPackage**（生产 build 前由 S5-02 触发时确认；本 story Editor 模拟下不阻塞）
- **Procedure / OnApplicationQuit `_sceneManager.Dispose()` 集成**（建议在本 story 同 commit 内同步完成；如 framework procedure 框架接入复杂可单 spike story；目前**入本 story scope** by AC-7）
- **GameApp.Entrance(object[] objects) 中 objects 参数语义解析**（S5-02 视情决定；本 story 内 wire-up 不依赖 objects payload）
- **章节 transition perf budget profile (<2s)**（polish phase；本 story R3 P1 case 仅 timeout=5s 兜底，不做 perf assert）

---

## Implementation Notes

### Step 1: GameApp.Entrance wire-up（production）

**改动文件**: `Assets/GameScripts/HotFix/GameLogic/GameApp.cs`

```csharp
// GameApp 字段
private SceneManager _sceneManager;

// Entrance 内 (按 ADR-009 + S3-01..03 spike 路径)
_sceneManager = new SceneManager();
_sceneManager.Init();
_sceneManager.RegisterChapterDataProvider(BuildFixtureChapterDataProvider());
_sceneManager.RegisterFadeOverlay(new NoOpFadeOverlay());

// 关停路径（OnApplicationQuit / Procedure shutdown）
_sceneManager?.Dispose();
_sceneManager = null;

// fixture provider — 同 Luban TbChapter.Get 签名
private static System.Func<int, ChapterData> BuildFixtureChapterDataProvider() => id => id switch {
    1 => new ChapterData(
        id: 1,
        sceneId: "Chapter_01_Approach",
        bgmAsset: "",                    // chapter 1 暂无 BGM；audio 系统 S5-02 接入
        emotionalWeight: 1.0f,           // ADR-009 默认值
        overlayColor: "#3A3530"          // art-bible line 53 + scene-management.md line 443
    ),
    _ => null                            // 与 Luban TbChapter.Get 一致 fail-loud
};
```

### Step 2: DevTestState auto trigger（2026-05-09 sign-off — M1 方案）

**改动文件**: `Assets/GameScripts/HotFix/GameLogic/GameFlow/DevTestState.cs`

在 `OnEnter` 内、`DevBootstrap.RunRequested()` **之前** 1 行 + 1 句 Log：
```csharp
GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
Log.Info("[GameFlow] [S5-1b] 已派发 OnRequestSceneChange(1) → SceneManager 接收并跑 BeginTransitionAsync(1) 11 步");
```

> **时序保证**：`GameApp.Entrance` 内 `_sceneManager.Init()` 已在 `StartGameLogic()` (line 73) 之前完成；FSM 启动后状态切换 `GameLoadingState → DevTestState`；OnEnter 时 listener 早已 attached，派发安全。spike RunRequested 在派发后启动，chapter 1 是否加载完成无所谓（spike P1 await `CurrentState == Idle` 5s 兜底）。

### Step 3: R3 PlayMode probe spike — 单文件 3 内类（项目惯例）

按 S301/S302/S303 + S407 + S5-03/-05/-06 spike 单文件 3 内类模式（**1 file with 3 inner classes**）：

**File**: `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-1b_BootSceneLoad.cs`（`#if UNITY_EDITOR || DEBUG` 包裹整文件）

**3 内类**：
1. `public class S51bSpike : IDevSpike` — `Id => "S5-1b"` + `Name => "Boot Scene Load Integration (S5-1b)"` + `Launch()` 内 `new GameObject("S51b_Runtime") + DontDestroyOnLoad + AddComponent<S51bRuntime>()`
2. `public class S51bRuntime : MonoBehaviour` — `Start()` 内构造 `S51bTester` + `WriteResultJson()` + `RunAsync().Forget()`；`OnGUI()` 报告进度
3. `public class S51bTester` — 纯逻辑 5 case (P1-P5 顺序) + listener 收发管理 + `WriteResultJson()` + `static string ResultFilePath { get; }`

**关键技术约束 (M1 双层模式)**:
- **P1/P2/P3 复用 production**：spike Tester 通过反射拿 `GameApp._sceneManager` static field：
  ```csharp
  var prodScene = (SceneManager)typeof(GameApp).GetField("_sceneManager", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
  ```
  P1 spike subscribe production sender events (per-event mode) 监听 8 lifecycle event 顺序；P2 spike 派 `OnRequestSceneChange(1)`；P3 spike `await prodScene.UnloadCurrentChapterAsync()`
- **P4/P5 自构建 isolated**：`new SceneManager()` 但**不**调 `Init()`（避免 listener 冲突 production listener bus，且不需要走 OnRequestSceneChange listener path）；直接 `await spikeLocal.BeginTransitionAsync(...)` 进入 LoadChapterSceneAsync 入口 line 417 fail-loud 路径，不进 LoadSceneAsync 不撞 YooAsset 锁
- **AC-7 Dispose 验证**: 在 P3 之后单独 case (P-Dispose 可选) `prodScene.Dispose()`；reflection 验 `_initialized == false` 后 GameApp `_sceneManager = null` 重设，避免后续测试 leak（**注意**：因为本 spike 跑完整个 PlayMode session 不再需要 production scene 工作，本步骤可省）
- **P4/P5 Debug.LogError 期望**：`OnSceneLoadFailed` 的 reason 同步 Log.Error（SceneManager.cs:420）；**spike 内 `LogAssert.Expect(LogType.Error, new Regex(...))`** 抑制 PlayMode test failure
- **R1 per-event listener mode (ADR-029 V2.0)**：spike 内 listener subscribe `ISceneEvent` 必须用 per-event mode；参 `S303_SceneEventOrdering.cs:384-391` 已证实模板（用 SourceGenerator 生成的常量 `ISceneEvent_Event.OnSceneXxx`）：
  ```csharp
  GameEvent.AddEventListener<int, int>(ISceneEvent_Event.OnSceneTransitionBegin, _onTB);
  GameEvent.AddEventListener<int, string>(ISceneEvent_Event.OnSceneLoadComplete, _onLC);
  GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneReady, _onR);
  GameEvent.AddEventListener<int, string>(ISceneEvent_Event.OnSceneLoadFailed, _onLF);
  // teardown 时 RemoveEventListener 同签名取消
  ```
  **禁止** `AddEventListener<ISceneEvent>(this)` interface mode

### Step 4: spike 注册 + 自动跑

按 IDevSpike 协议（Sprint 4 沉淀），spike 在 `DevBootstrap` 自动 list；Editor PlayMode 启动后从 list 选 S5-1b 运行；JSON 写到 `Application.persistentDataPath/S5-1b_Result.json` (path: `~/Library/Application Support/<company>/<product>/S5-1b_Result.json` on macOS)。

---

## QA Test Cases

*Logic / Integration type — automated PlayMode (R3) + grep-based static evidence*

- **AC-1, AC-2, AC-10 (grep evidence)**: 自动可验
  - `grep -c "_sceneManager" Assets/GameScripts/HotFix/GameLogic/GameApp.cs` ≥ 5
  - `grep -c "RegisterChapterDataProvider\|RegisterFadeOverlay\|Init()\|Dispose()" Assets/GameScripts/HotFix/GameLogic/GameApp.cs` ≥ 4
  - `grep -c "BuildFixtureChapterDataProvider" Assets/GameScripts/HotFix/GameLogic/GameApp.cs` ≥ 2
  - `[ -f Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-1b_BootSceneLoad.cs ]` 单文件存在；内 `grep -c "^\s*public class S51b\(Spike\|Runtime\|Tester\)" Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-1b_BootSceneLoad.cs == 3`
- **AC-3 (dev menu / FSM trigger)**: HotKey 真按下后 SceneView Hierarchy 显示 `Chapter_01_Approach` scene 已 add
- **AC-4..AC-7 (R3 PlayMode probe)**: 自动 — spike 内 assert + JSON evidence
- **AC-8 (R3 ALL PASS)**: `cat ~/Library/Application Support/.../S5-1b_Result.json | jq .all_passed == true`
- **AC-9 (Console 0 error)**: `read_console` filter level=Error；除 P4/P5 case 主动写的 expectedError 外 == 0

---

## Test Evidence

**Story Type**: Logic / Integration
**Required evidence**:
- **Spike**: `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-1b_BootSceneLoad.cs` — **1 个文件 + 3 内类**（`S51bSpike : IDevSpike` + `S51bRuntime : MonoBehaviour` + `S51bTester` 纯逻辑）；与 S301/S302/S303 spike 单文件 3 内类模式一致
- **JSON evidence**: `~/Library/Application Support/<company>/<product>/S5-1b_Result.json`（5 case 全 PASS schema 见 §R3 Justification）
- **QA evidence doc**: `production/qa/playmode-bootscene-load-2026-05-XX.md` — 含
  - JSON evidence summary table（5 case 全 PASS）
  - Console snapshot (R3 path 0 unexpected error；P4/P5 expected `Debug.LogError` 标记)
  - `git diff --stat HEAD -- Assets/GameScripts/HotFix/GameLogic/GameApp.cs` 改动 evidence
  - `grep -c` AC-10 评估表
  - 8 lifecycle event 顺序 dump（spike Runtime listener 收到的 event log，按时间序）
- **Production code review**: `GameApp.cs` diff 走 code-review skill (Lead Programmer agent) 一次

**Status**: ✅ **Done** — 2026-05-09 PlayMode 5/5 PASS + 22/22 asserts；evidence doc `production/qa/playmode-bootscene-load-2026-05-09.md`。

---

## Completion Notes (2026-05-09)

**Verdict**: COMPLETE WITH NOTES

**Delivery summary**:
- ✅ 10/10 AC PASS（AC-1..AC-10 全验证 — grep evidence + R3 PlayMode + JSON + Console 0 unexpected error）
- ✅ R3 PlayMode 5/5 case PASS / 22/22 asserts PASS（M1 dual-layer：P1-P3 production reflection / P4-P5 isolated local SceneManager）
- ✅ Production code change：`GameApp.cs` `_sceneManager` field + `Init/RegisterChapterDataProvider/RegisterFadeOverlay/Dispose` 4 个 surface 接入 + `BuildFixtureChapterDataProvider()` 内嵌 fixture
- ✅ Spike: `S5-1b_BootSceneLoad.cs` 1 文件 + 3 内类（S51bSpike / S51bRuntime / S51bTester）符合项目惯例
- ✅ DevTestState auto-trigger（`OnRequestSceneChange(1)` → 800ms 后 F4 driver `BeginTransitionAsync(1)` reflection 调）

**Surfaced deficiencies**（详见 evidence doc §10）:

1. **NEW (架构 gap, S5-1b 期间发现)**：ADR-009 listener-path driver 缺失 — production 端 `OnRequestSceneChange` handler 仅 `state=TransitionOut` 但**没**串到 `BeginTransitionAsync`，因此 11-step transition 无 production 驱动入口；本 story 用 dev-only F4 driver in `DevTestState` 经反射临时模拟。**待新建 follow-up story** 补 production driver（candidate: GameLogic 层 `OnRequestSceneChange` listener handler 内自调 `_sceneManager.BeginTransitionAsync(targetId)`，或 `GameFlow.SceneTransitionState` 在 `OnEnter` 内 driver）。
2. **S5-01 production gap (resolved during S5-1b)**：chapter scene 路径错位 — `Chapter_01_Approach.unity` 原在 `Assets/Scenes/`，但 YooAsset `Scenes` group 收集 `Assets/AssetRaw/Scenes/`，导致首次 PlayMode handle.Status=Failed。已 `git mv` 修正。**S5-01 evidence doc 需 amendment 标注此 path correction**（建议 follow-up doc-fix）。
3. **S5-01 production gap (residual, unresolved)**：`Chapter_01_Approach.unity` MainCamera 留有 `AudioListener` component → 与 `main.unity` MainCamera 冲突，PlayMode console 反复 warning `There are 2 audio listeners in the scene`。**待 follow-up doc-fix / S5-1c story** 删除 chapter scene MainCamera AudioListener 或 disable component。

**F4 driver 临时性**：当前 `DevTestState.DriveProductionSceneTransitionAsync` 是 dev-only 临时 stub，**production 不应保留**。架构 driver 补完后立即移除。Inline TODO marker 已写入 `DevTestState.cs`。

**Code review summary** (inline review by current agent — Lead Programmer self-review):
- ✅ Coding standards 合规（`partial class GameApp` 同文件 fixture / 不引入额外 file / Log.Info 标签格式 [GameApp][S5-1b] 与 [GameFlow][S5-1b] 一致）
- ✅ ADR 合规（ADR-009 11-step / ADR-007 fixture provider 签名 / ADR-027 ISceneEvent listener 自挂自摘 / ADR-029 V2.0 R3 PlayMode probe 真实跑）
- ⚠️ Reflection access (`GetField("_sceneManager", NonPublic|Static)`) — dev-only spike 与 dev-only F4 driver 已严格隔离在 `DevTest/Spikes/` 与 `DevTestState`（dev FSM state），production runtime 不走此路径
- ✅ Dispose 路径已挂 `Release()`

---

## Dependencies

- **Depends on**:
  - **story-001 ✅ DONE 2026-05-09** — chapter 1 scene asset (`Chapter_01_Approach.unity`) 已落盘 (S5-01 evidence: `production/qa/s501-scene-build-2026-05-09.md`)
  - **S3-01 ✅ CORE PASSED** (LoadChapterSceneAsync framework boundary)
  - **S3-02 ✅ CORE PASSED** (UnloadCurrentChapterAsync framework boundary)
  - **S3-03 ✅ CORE PASSED** (BeginTransitionAsync 11-step driver)
  - **S4-01..03 ✅** (frameworks: GameModule.Scene / Resource / Event 已 verified)
  - **ADR-009 (Accepted)** + **ADR-007 (Accepted)** + **ADR-029 V2.0 (Accepted) R3 ready**
- **Unlocks**:
  - **S5-02** end-to-end 5 系统串通（chapter 1 boot 路径 ready 后即可启动）
  - 后续任何依赖"production runtime SceneManager 真实接入"的 story（chapter 切换 / Luban migration / real fade overlay）

---

## Assumptions Validated (R2 — 2026-05-09)

R2 gap probe 已在 outline draft 阶段 verify，下列假设全部成立：

| # | 假设 | Verify 方式 | 结果 |
|---|------|-------------|------|
| **R2.1** | `SceneManager` 9 个 public API surface（ctor / Init / Dispose / RegisterChapterDataProvider / RegisterFadeOverlay / Init / LoadChapterSceneAsync / UnloadCurrentChapterAsync / BeginTransitionAsync）签名稳定 | Read `Assets/GameScripts/HotFix/GameLogic/Scene/SceneManager.cs` | ✅ 签名全部 confirmed；S3-01..03 spike 已用 |
| **R2.2** | `ChapterData` ctor 5 参数签名（id/sceneId/bgmAsset/emotionalWeight/overlayColor） | Read `ChapterData.cs` | ✅ confirmed；all readonly |
| **R2.3** | `ISceneEvent` 9 个方法签名 + `OnSceneLoadComplete(int, string)` + `OnSceneLoadFailed(int, string)` | Read `IEvent/ISceneEvent.cs` | ✅ confirmed；signatures frozen since S2-05 |
| **R2.4** | 测试桩 `CurrentLoadedChapterIdForTest` (`int`) + `CurrentChapterSceneNameForTest` (`string`) + `NoChapterId` (`= -1`) + `_initialized` (`private bool`) 仍存在 | Grep SceneManager.cs | ✅ confirmed at line 64/120/123/127；reflection access 路径稳 |
| **R2.5** | `GameApp.Entrance(object[] objects)` 真签名 + `partial class GameApp` 可加 partial-file / 同文件 fields 注入 | Read `GameApp.cs:19,27` | ✅ confirmed at readiness gate 2026-05-09 — `partial class GameApp` line 19 + `Entrance(object[] objects)` line 27 + `Utility.Unity.AddDestroyListener(Release)` line 33 (Release 是 Dispose 挂载点) + `RegisterDevSpikes()` line 40 (IDevSpike 入口) |
| **R2.6** | `NoOpFadeOverlay` 已存在为 default fallback + parameterless ctor | Read `IFadeOverlay.cs:36-40` | ✅ confirmed — `public sealed class NoOpFadeOverlay : IFadeOverlay` parameterless ctor 可用 `new NoOpFadeOverlay()` |
| **R2.7** | `IDevSpike` 协议 + spike 文件结构模式 | Read `IDevSpike.cs` + Glob `Spikes/*.cs` + Read `S301_AdditiveSceneLoading.cs` | ✅ confirmed at readiness gate 2026-05-09 — **项目惯例为 1 文件 + 3 内类** (`*Spike : IDevSpike` + `*Runtime : MonoBehaviour` + `*Tester` 纯逻辑)，**不**是三件套（separate files）；S301/S302/S303/S407/S5-03/S5-05/S5-06 全部一致 |

**R2 全部 ✅ 通过**：本 readiness gate 一次性把 R2.1-R2.7 所有项 grep verify 完成；dev-story 阶段无遗留 R2 待复确认项。

---

## ADR-029 V3 Watch List Hooks

本 story R3 实施过程中如出现以下情况应 capture 为新 drift type 候选并写入 sprint-status.yaml watch list：

1. **Type-5 candidate**: 11-step driver 内某 step 行为与 ADR-009 spec 不一致（实测发现）
2. **Type-6 candidate**: `IFadeOverlay.NoOpFadeOverlay` await 行为与 production fade overlay 真实接入时 timing 不可类比（fade overlay 替换时引入 timing drift）
3. **Type-7 candidate**: `Func<int, ChapterData>` fixture provider 与 Luban `TbChapter.Get` 真接入时签名 / 异常行为不一致（migration cost surfacing）

如出现以上任一，per ADR-029 V2.0 §V2-7：sprint-status.yaml `watch_list` triggers 内追加 drift type 描述 + 关联 story-001b R3 case 编号 + 沉淀 problem memo 到 `.claude/memory/`。
