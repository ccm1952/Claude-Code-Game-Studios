// 该文件由Cursor 自动生成

# Control Manifest — 影子回忆 (Shadow Memory)

> **Engine**: Unity 2022.3.62f2 LTS
> **Framework**: TEngine 6.0.0 + HybridCLR + YooAsset 2.3.17 + UniTask 2.5.10
> **Generated**: 2026-04-22
> **Authority**: Technical Director

This is the single authoritative rules sheet for all programmers. Every rule traces to a specific ADR, technical preference, or Sprint 0 finding. When in doubt, this document wins.

## ADRs Covered

ADR-001 (TEngine Framework) · ADR-002 (URP Shadow Rendering) · ADR-003 (Mobile-First Platform) · ADR-004 (HybridCLR Assembly Boundary) · ADR-005 (YooAsset Resource Lifecycle) · ~~ADR-006 (GameEvent Protocol)~~ *superseded by ADR-027* · ADR-007 (Luban Config Access) · ADR-008 (Save System) · ADR-009 (Scene Lifecycle) · ADR-010 (Input Abstraction) · ADR-011 (UIWindow Management) · ADR-012 (Shadow Match Algorithm) · ADR-013 (Object Interaction) · ADR-014 (Puzzle State Machine) · ADR-015 (Hint System) · ADR-016 (Narrative Sequence Engine) · ADR-017 (Audio Mix) · ADR-018 (Performance Monitoring) · **ADR-027 (GameEvent Interface Protocol)**

## Sprint 0 Findings Covered

SP-001 (GameEvent Payload) · SP-002 (UIWindow Lifecycle) · SP-003 (YooAsset Package Strategy) · SP-004 (Luban Thread Safety) · SP-005 (WallReceiver Shader) · SP-006 (PuzzleLock Token) · SP-007 (HybridCLR AsyncGPU) · SP-008 (Narrative Sequence Schema) · SP-009 (I2 Localization) · SP-010 (Performance Monitor)

---

## 1. Global Rules

### 1.1 Naming Conventions

| Element | Convention | Example | Source |
|---------|-----------|---------|--------|
| Classes | PascalCase | `ShadowPuzzleManager` | tech-prefs |
| Private fields | camelCase with `_` prefix | `_shadowTarget` | tech-prefs |
| Public properties | PascalCase | `MatchScore` | tech-prefs |
| Events/Callbacks | PascalCase with `On` prefix | `OnPuzzleSolved` | tech-prefs |
| GameEvent interfaces | `I{Domain}Event` PascalCase | `IGestureEvent`, `IShadowRTEvent` | ADR-027 |
| GameEvent methods | `On{Action}` PascalCase | `OnTap(GestureData data)` | ADR-027 |
| UI GameEvent interfaces | `I{PanelName}UI` with `Show/Close` methods | `ILoginUI.ShowLoginUI()` | ADR-027 |
| ~~GameEvent IDs (deprecated)~~ | ~~`Evt_` prefix, PascalCase~~ | ~~`Evt_ShadowMatched`~~ | superseded by ADR-027 |
| Files | PascalCase matching class | `ShadowPuzzleManager.cs` | tech-prefs |
| Scenes/Prefabs | PascalCase | `MainScene.unity` | tech-prefs |
| Constants | UPPER_SNAKE_CASE | `MAX_SHADOW_OBJECTS` | tech-prefs |
| InputBlocker tokens | `"UIPanel_{ClassName}"` for UI panels | `"UIPanel_PauseMenuPanel"` | ADR-011 |
| PuzzleLock tokens | Predefined `InteractionLockerId` constants | `"shadow_puzzle"`, `"narrative"` | SP-006 |

### 1.2 Performance Budgets

| Metric | Mobile Budget | PC Budget | Source |
|--------|:------------:|:---------:|--------|
| Frame budget | 16.67ms (60fps) | 16.67ms (60fps) | ADR-003 |
| Gameplay systems per frame | < 5.5ms | < 5.5ms | ADR-003 |
| Draw calls | < 150 | < 300 | ADR-003 |
| Triangles per frame | < 200K | — | ADR-003 |
| Memory ceiling | 1.5 GB | 4 GB | ADR-003 |
| Texture max resolution | 1024×1024 | 2048×2048 | ADR-003 |
| Audio memory | < 30 MB | — | ADR-017 |
| Scene load time | < 3s | — | ADR-003 |
| APK/IPA initial size | < 200 MB | — | ADR-003 |

#### Per-System Frame Budgets

| System | Budget | Source |
|--------|:------:|--------|
| Input gesture recognition | < 0.5ms | ADR-010 |
| Object Interaction Update (10 objects) | < 1.0ms | ADR-013 |
| Shadow match calculation | < 2.0ms | ADR-012 |
| ShadowRT CPU readback | ≤ 1.5ms | ADR-002 |
| WallReceiver shader GPU time | < 0.5ms | ADR-002 |
| Hint system update | < 0.5ms | ADR-015 |
| UI system update | < 0.5ms | ADR-011 |
| Audio system per frame | < 1.0ms | ADR-017 |
| Narrative sequence per frame | < 1.0ms | ADR-016 |
| Performance monitor per frame | < 0.1ms | ADR-018 |
| Event dispatch per Send | < 0.05ms | ADR-027 (inherits ADR-006) |

### 1.3 Approved Libraries

| Library | Version | Purpose | Source |
|---------|---------|---------|--------|
| TEngine | 6.0.0 | Core framework | ADR-001 |
| HybridCLR | 2.x | C# hot-reload | ADR-004 |
| YooAsset | 2.3.17 | Asset management | ADR-005 |
| UniTask | 2.5.10 | Zero-allocation async/await | tech-prefs |
| DOTween | latest | Tweening animations | ADR-013 |
| Luban | latest | Config table generation | ADR-007 |
| TextMeshPro | built-in | Text rendering | tech-prefs |
| I2 Localization | embedded in TEngine | Multi-language (via `ILocalizationModule`) | SP-009 |

### 1.4 Globally Forbidden APIs

#### Required Patterns

- **All module access via `GameModule.XXX` static accessors** — source: ADR-001
- **All async operations via UniTask** — source: ADR-001, tech-prefs
- **All inter-module communication via `GameEvent` `[EventInterface]` + Source Generator** — source: ADR-001, ADR-027
- **All gameplay values from Luban config tables** — source: ADR-007
- **All asset loading via `GameModule.Resource.LoadAssetAsync<T>()`** — source: ADR-001, ADR-005
- **All UI screens inherit `UIWindow`; reusable components inherit `UIWidget`** — source: ADR-001, ADR-011
- **All events defined as `[EventInterface]` C# interfaces in `GameLogic/IEvent/`** — source: ADR-027
- **Sender uses `GameEvent.Get<T>().OnXxx(payload)`; listener uses `GameEvent.AddEventListener<T>()` / `GameEventMgr.AddEventListener` / `UIWindow.AddUIEvent`** — source: ADR-027
- **Every `LoadAssetAsync` must have a corresponding `Release()`** — source: ADR-005
- **`SetUISafeFitHelper` on all root UI canvases** — source: ADR-003, ADR-011
- **Story 规范中若引用 `Evt_Xxx_Yyy` 命名，实施时按 `architecture-traceability.md` 附录 A 的 EventId → Interface 映射表替换；映射表之外的名称需联系 Architecture Owner 补充** — source: ADR-027

#### Forbidden Approaches

- **Never use `ModuleSystem.GetModule<T>()`** — use `GameModule.XXX` accessors — source: ADR-001
- **Never use synchronous asset loading (`Resources.Load`, `AssetBundle.LoadAsset`)** — use `LoadAssetAsync` via YooAsset — source: ADR-001, ADR-005, tech-prefs
- **Never use Coroutines for async operations** — use UniTask exclusively — source: tech-prefs
- **Never use `GameObject.Find` / `FindObjectOfType` at runtime** — use references or DI — source: tech-prefs
- **Never forget to call `UnloadAsset` after `LoadAssetAsync`** — resource leak — source: tech-prefs, ADR-005
- **Never hardcode gameplay values** — must be data-driven via Luban config tables — source: ADR-007, tech-prefs
- **Never use `LoadSceneMode.Single`** — additive only — source: ADR-009
- **Never use C# events/delegates for cross-module communication** — use GameEvent `[EventInterface]` — source: ADR-027
- **Never use ScriptableObject event channels** — use GameEvent `[EventInterface]` — source: ADR-027
- **Never define new `public const int Evt_Xxx` event IDs** — use `[EventInterface]` interfaces; Source Generator auto-generates IDs — source: ADR-027
- **Never use Unity Audio Mixer Groups** — use TEngine AudioModule directly — source: ADR-017
- **Never use `DontDestroyOnLoad` in chapter scene objects** — persistent objects live in MainScene — source: ADR-009
- **Never call `Input.GetTouch()` directly outside InputService** — use `IInputAdapter` / gesture events — source: ADR-003, ADR-010
- **Never use `Camera.main` per-frame** — cache the camera reference — source: ADR-012
- **Never re-enter `GameEvent.Get<T>().OnXxx()` for the same event inside its own handler** — causes infinite recursion — source: ADR-027 (inherits ADR-006 §5)
- **Never use `using I2.Loc`** — I2 namespace does not exist; use `GameModule.Localization` via `ILocalizationModule` — source: SP-009
- **Never directly call `LocalizationManager`** — use `GameModule.Localization` — source: SP-009

---

## 2. Foundation Layer

### 2.1 TEngine Module System (ADR-001)

#### Required Patterns

- **Access all services via `GameModule.XXX` static accessors** (`GameModule.Audio`, `GameModule.UI`, `GameModule.Resource`, `GameModule.Scene`, `GameModule.Timer`, `GameModule.Fsm`, `GameModule.Localization`) — source: ADR-001
- **Extend boot flow by inserting new Procedures between existing ones** — never bypass the chain with direct initialization in `Awake()` or `Start()` — source: ADR-001
- **Boot sequence: `ProcedureStart → ProcedureSplash → ProcedureLoadAssembly → ProcedureMain`** — each procedure calls `ChangeState<NextProcedure>()` when ready — source: ADR-001
- **UIModule is in the GameLogic hot-fix assembly, NOT TEngine Runtime** — path: `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/` — source: SP-002

#### Forbidden Approaches

- **Never use `ModuleSystem.GetModule<T>()`** — always use `GameModule.XXX` — source: ADR-001

#### Assembly Definition (asmdef) Guardrails

- **Any asmdef referencing `GameLogic` MUST also reference `TEngine.Runtime`** — TEngine's Roslyn Source Generator (`EventInterfaceGenerator`) emits `GameEventHelper.g.cs` into every assembly touching GameLogic; without `TEngine.Runtime` the generated code fails with CS0246 — source: Empirical (SP-007, Story-001 test setup)
- **Minimum test asmdef references**: `["GameLogic", "TEngine.Runtime"]` — source: Empirical

### 2.2 GameEvent Interface Protocol (ADR-027, inherits ADR-006 §3-§6, SP-001)

#### Required Patterns

- **All events defined as C# interfaces with `[EventInterface(EEventGroup.GroupXxx)]` attribute** in `Assets/GameScripts/HotFix/GameLogic/IEvent/` — source: ADR-027
- **Grouping convention (project-specific)**: `GroupUI` = UI 通知（面板展开/关闭、弹窗队列），`GroupLogic` = 业务事件（手势、关卡状态、渲染、设置变更） — source: ADR-027 Decision §3
- **Event ID is auto-generated by Roslyn Source Generator (`EventInterfaceGenerator`) as `RuntimeId.ToRuntimeId("{InterfaceName}_Event.{MethodName}")`** — do NOT hand-maintain IDs — source: ADR-027
- **Sender API**: `GameEvent.Get<T>().OnXxx(payload)` where `T` is the interface — source: ADR-027
- **Listener API** (choose by context):
  - `GameEvent.AddEventListener<T>(handler)` — low-level, manual cleanup required
  - `GameEventMgr.AddEventListener<T>(handler)` — scoped, automatic cleanup via `Dispose()`
  - `UIWindow.AddUIEvent<T>(handler)` — UI-scoped, automatic cleanup on panel close
  - `GameEventHelper.RegisterListener<T>(handler)` — 手动统一注册入口 — source: ADR-027 Decision §5
- **Payload rules** (inherit ADR-006 §2): use `struct` for hot-path events; typed parameters only — NO `object`/raw primitives for complex data — source: ADR-027, SP-001
- **Interface methods without payload use no-arg `void OnXxx()` signature** (e.g., `IHintEvent.OnHintDismissed()`) — source: ADR-027
- **Every interface and method must have XML doc comment** with Sender(s), Listener(s), Payload type, and Cascade depth — source: ADR-027 (inherits ADR-006 §6)
- **Register all listeners in `Init()`/`OnCreate()`; remove all in `Dispose()`/`OnDestroy()`** — `GameEventMgr.RemoveAllEventListener()` must be called in `Dispose()` — source: ADR-027 (inherits ADR-006 §3)
- **Scene-scoped listeners must be removed on `ISceneEvent.OnSceneUnloadBegin`** — source: ADR-027 (inherits ADR-006 §3)
- **Cross-event cascade depth must not exceed 3** — break deeper cascades with a one-frame UniTask delay — source: ADR-027 (inherits ADR-006 §5)
- **Multi-sender token protocol for `IPuzzleLockEvent`**: use `LockToken.Puzzle` / `LockToken.Narrative` / `LockToken.Tutorial` constants; lock/unlock token semantics fully inherit ADR-006 §4 — source: ADR-027 Decision §4
- **GameApp.Entrance() must call `GameEventHelper.Init()` on its first line** — initializes Source Generator dispatch table — source: ADR-027 Decision §7
- **Before each release build on iOS, run `HybridCLR → Generate → AOT Generic References`** — required to avoid `ExecutionEngineException` from `GameEvent.Get<T>()` generic instantiation — source: ADR-027 Decision §9

#### Forbidden Approaches

- **Never define `public const int Evt_Xxx` event IDs** — use `[EventInterface]` C# interfaces exclusively — source: ADR-027
- **Never use anonymous `object` parameters or raw `int` encoding for payloads** — use named typed parameters — source: ADR-027 (inherits ADR-006 §2)
- **Never call `GameEvent.Get<T>().OnXxx()` for the same method inside its own handler** (re-entrancy) — source: ADR-027 (inherits ADR-006 §5)
- **Never assume listener invocation order** — listeners for the same interface method may be invoked in any order — source: ADR-027 (inherits ADR-006 §5)
- **Never bypass `[EventInterface]` with raw `GameEvent.Send(hashedInt, ...)`** — breaks IDE navigation, compile-time safety, and type checking — source: ADR-027
- **Never call `GameEvent.Get<T>()` on iOS before verifying AOT generic references are regenerated** — source: ADR-027 Decision §9

### 2.3 HybridCLR Assembly Boundary (ADR-004)

#### Required Patterns

- **Three-assembly split: Default (bootstrap) / GameLogic (all gameplay code) / GameProto (Luban generated code)** — source: ADR-004
- **Dependency direction: Default → (dynamic load) → GameLogic → (compile ref) → GameProto** — source: ADR-004
- **Default assembly contains ONLY boot procedures, HybridCLR AOT metadata registration, and YooAsset package initialization** — source: ADR-004
- **All gameplay code (interfaces, implementations, MonoBehaviours, EventId constants, UIWindow/UIWidget subclasses) resides in GameLogic** — source: ADR-004
- **GameProto assembly contains EXCLUSIVELY Luban auto-generated code** — source: ADR-004
- **Run `HybridCLR/Generate/AOTGenericReference` before every release build** — source: ADR-004
- **Load order: GameProto.dll first, then GameLogic.dll** — source: ADR-004
- **AOT metadata registration must complete BEFORE DLL loading** — source: ADR-004
- **All gameplay prefabs loaded via YooAsset AFTER DLL loading procedure completes** — source: ADR-004
- **Maintain `AOTGenericReferences.cs` in Default assembly** — source: ADR-004

#### Forbidden Approaches

- **Never place gameplay logic in Default assembly** — it cannot be hot-updated — source: ADR-004
- **Never hand-edit any file in GameProto** — Luban regeneration silently overwrites changes — source: ADR-004
- **Never create compile-time references from Default → GameLogic or Default → GameProto** — use `Assembly.Load()` at runtime — source: ADR-004
- **Never create references from GameProto → GameLogic** — config data must not depend on gameplay logic — source: ADR-004
- **Never embed prefabs with GameLogic MonoBehaviours in BootScene (Default assembly)** — they will become "Missing Script" — source: ADR-004
- **Never use `DontDestroyOnLoad` in chapter scenes** — all persistent state in MainScene managers — source: ADR-004, ADR-009

### 2.4 YooAsset Resource Loading & Lifecycle (ADR-005, SP-003)

#### Required Patterns

- **Single ResourcePackage strategy for MVP** (`"DefaultPackage"`) — source: ADR-005, SP-003
- **Handle-ownership: the system that calls `LoadAssetAsync` owns the handle and is solely responsible for calling `Release()`** — source: ADR-005
- **Null-check handles before `Release()`; set to `null` immediately after release** — source: ADR-005
- **Scene Transition Cleanup Sequence (mandatory, in order)**: (1) notify systems via `Evt_SceneUnloadBegin`, (2) each system releases owned AssetHandles, (3) Scene Manager releases SceneHandle via `UnloadSceneAsync()`, (4) `Resources.UnloadUnusedAssets()`, (5) `GC.Collect()`, (6) load next scene — source: ADR-005
- **Commonly loaded assets (UI prefabs) held by independent handles, not tied to scene handles** — source: SP-003

#### Forbidden Approaches

- **Never use synchronous `Resources.Load<T>()` or `AssetBundle.LoadAsset()`** — source: ADR-005
- **Never call direct YooAsset API (`YooAssets.GetPackage()`)** — use `GameModule.Resource` wrapper — source: ADR-005
- **Never fire-and-forget a load (losing the handle)** — every load must track its handle for release — source: ADR-005
- **Never share handles across systems** — only the loading system may release — source: ADR-005
- **Never skip `UnloadUnusedAssets()` + `GC.Collect()` between scene transitions** — source: ADR-005, ADR-009

### 2.5 Scene Lifecycle (ADR-009)

#### Required Patterns

- **Additive-only scene loading** — `LoadSceneMode.Additive` exclusively — source: ADR-009
- **MainScene loaded once at boot, never unloaded** — holds all managers, UI, audio, camera — source: ADR-009
- **One chapter scene active at a time** — previous chapter fully unloaded before next loads — source: ADR-009
- **After loading chapter scene, call `SceneManager.SetActiveScene()` on the new scene** for lighting settings — source: ADR-009
- **Scene transitions triggered exclusively via `GameEvent.Send(Evt_RequestSceneChange, payload)`** — external systems never call Scene Manager methods directly — source: ADR-009
- **Scene Manager uses state machine: Idle → TransitionOut → Unloading → Loading → TransitionIn → Idle (+ Error)** — source: ADR-009
- **11-step transition flow is the binding contract** — no step may be skipped — source: ADR-009
- **8 scene lifecycle events (IDs 1400-1407) fired in deterministic order** — source: ADR-009
- **Scene name ↔ chapter ID mapping from Luban `TbChapter.sceneId`** — no hardcoded scene names — source: ADR-009
- **Transition mutex: only one transition at a time; max 1 queued request** — source: ADR-009

#### Forbidden Approaches

- **Never use `LoadSceneMode.Single`** — destroys MainScene — source: ADR-009
- **Never use direct `SceneManager.LoadSceneAsync()`** — use `GameModule.Resource.LoadSceneAsync()` — source: ADR-009
- **Never skip cleanup between scenes** — mandatory `UnloadUnusedAssets()` + `GC.Collect()` — source: ADR-009
- **Never use `DontDestroyOnLoad` in chapter scene objects** — source: ADR-009
- **Never load multiple chapter scenes simultaneously** — source: ADR-009
- **Never hardcode scene names** — resolve from Luban `TbChapter.sceneId` — source: ADR-009

### 2.6 Save System (ADR-008)

#### Required Patterns

- **Save format: Custom JSON + CRC32 checksum + backup file + atomic write** — source: ADR-008
- **File location: `Application.persistentDataPath/` with `save.json`, `save.crc`, `save.backup.json`, `save.backup.crc`** — source: ADR-008
- **Load fallback chain: primary → backup → fresh start** — source: ADR-008
- **Version migration: sequential chain (`v1 → v2 → v3 → vCurrent`)** via `ISaveMigration` — source: ADR-008
- **Auto-save triggers: `Evt_PuzzleStateChanged` (1s debounce), `Evt_ChapterComplete` (immediate), collectible pickup (1s debounce), `OnApplicationPause(true)` (immediate), `OnApplicationQuit` (immediate)** — source: ADR-008
- **All save I/O via UniTask — no synchronous file I/O** — source: ADR-008
- **Atomic write: write to `.tmp` → verify CRC → rename to `.json`** — source: ADR-008
- **Settings (8 items) stored separately via `PlayerPrefs`** — NOT in save file — source: ADR-008
- **`DeleteSave()` clears save files but does NOT clear PlayerPrefs settings** — source: ADR-008
- **Init order: SaveSystem.Init() → SaveSystem.LoadAsync() → ChapterState.Init(saveData)** — source: ADR-008

#### Forbidden Approaches

- **Never use Coroutines for save I/O** — use UniTask — source: ADR-008
- **Never store settings inside the save file** — use PlayerPrefs — source: ADR-008
- **Never skip CRC32 verification on load** — source: ADR-008

### 2.7 Performance Monitoring (ADR-018, SP-010)

#### Required Patterns

- **Global PerformanceMonitor is a single centralized module in Foundation Layer** — not per-system self-management — source: ADR-018, SP-010
- **Degradation trigger: 5 consecutive frames > 20ms → drop quality one level** — source: ADR-018
- **Recovery trigger: 60 consecutive frames < 12ms → attempt one-level upgrade** — source: ADR-018
- **Recovery has a 30-frame verification window at 14ms threshold** — failed verification reverts and doubles recovery requirement — source: ADR-018
- **5-level degradation cascade: Normal → Mild → Moderate → Severe → Critical** — source: ADR-018
- **Quality tier changes broadcast via `Evt_QualityTierChanged` GameEvent** — all systems listen and respond — source: SP-010
- **Settings UI and auto-degradation share the same quality state** — `PerformanceMonitor` is the single owner — source: SP-010
- **Manual quality selection via Settings disables auto-degradation** — source: SP-010

### 2.8 Input Abstraction (ADR-010)

#### Required Patterns

- **Three-layer architecture: Raw Touch Sampling → Blocker/Filter Gate → Gesture Recognition** — source: ADR-010
- **5 gesture types: Tap, Drag, Rotate, Pinch, LightDrag** — each dispatched via `IGestureEvent.OnXxx(GestureData)` (ADR-027 replaces 1000-1004 const IDs) — source: ADR-010, ADR-027
- **`GestureData` struct as payload (value type, zero GC on hot path)** — source: ADR-010
- **InputBlocker: stack-based with string tokens; `PushBlocker(token)` / `PopBlocker(token)`** — non-empty stack = all input discarded — source: ADR-010
- **InputFilter: single-active, `PushFilter(allowedGestures)` / `PopFilter()`** — whitelist only — source: ADR-010
- **Priority chain: InputBlocker > InputFilter > Normal pass-through** — source: ADR-010
- **All gesture thresholds from Luban `TbInputConfig`** — source: ADR-010
- **DPI normalization: `baseDragThreshold_mm * Screen.dpi / 25.4`** (fallback DPI = 160) — source: ADR-010
- **Max 2 tracked fingers — ignore touch index ≥ 2** — source: ADR-010
- **Dual-finger gestures are mutually exclusive** — once Rotate/Pinch entered, locked until all fingers lifted — source: ADR-010
- **Blocker token leak detection: push alive > 30s → `Debug.LogWarning`** — source: ADR-010

#### Forbidden Approaches

- **Never call `Input.GetTouch()` directly outside InputService** — source: ADR-010
- **Never define gesture events as `public const int`** — use `IGestureEvent` interface (ADR-027) — source: ADR-010, ADR-027

### 2.9 Localization (SP-009)

#### Required Patterns

- **Use `GameModule.Localization` (via `ILocalizationModule`) for all localization access** — source: SP-009
- **Language switching: `GameModule.Localization.SetLanguage("Chinese (Simplified)")` etc.** — source: SP-009
- **UI text localization uses TEngine Localization Localize component** — source: SP-009

#### Forbidden Approaches

- **Never `using I2.Loc`** — I2 namespace does not exist in the project — source: SP-009
- **Never directly `new LocalizationManager()`** — managed by TEngine module lifecycle — source: SP-009
- **Never call `LocalizationManager.CurrentLanguage` directly** — use module interface — source: SP-009

---

## 3. Core Layer

### 3.1 Shadow Match Algorithm (ADR-012)

#### Required Patterns

- **Multi-anchor weighted scoring: `matchScore = Σ(w_i × s_i) / Σ(w_i)`** — source: ADR-012
- **Per-anchor: `anchorScore = positionScore × directionScore × visibilityScore`** (multiplicative — any zero dimension zeros the anchor) — source: ADR-012
- **Temporal smoothing: exponential moving average with 0.2s sliding window** — source: ADR-012
- **ShadowRT sampling via `AsyncGPUReadback` from R8 RenderTexture** — 1-3 frame latency tolerated — source: ADR-012
- **If readback fails or is stale, reuse previous frame's buffer** — source: ADR-012
- **All anchors invisible → force `matchScore = 0`** — source: ADR-012
- **Reset smoothing buffer on puzzle state change** — source: ADR-012
- **All thresholds (perfectMatchThreshold, nearMatchThreshold, maxScreenDistance, etc.) from Luban `TbPuzzle`** — source: ADR-012
- **Cache camera reference — never call `Camera.main` per frame** — source: ADR-012

#### Performance Guardrails

- **Shadow match calculation**: ≤ 2.0ms/frame — source: ADR-012
- **ShadowRT CPU readback**: ≤ 1.5ms — source: ADR-002

### 3.2 Object Interaction (ADR-013, SP-006)

#### Required Patterns

- **6-state FSM per object: Idle → Selected → Dragging → Snapping → Locked** (plus light source states) — via TEngine FsmModule — source: ADR-013
- **Grid snap on finger release only** — during drag, object follows finger freely — source: ADR-013
- **Snap formula: `snappedPos = round(rawPos / gridSize) * gridSize`** — animated via DOTween EaseOutQuad — source: ADR-013
- **Fat finger compensation: `expandedRadius = colliderRadius + fatFingerMargin * (Screen.dpi / 326)`** — minimum touch target ≥ 44pt — source: ADR-013
- **Single selection: only one object selected at a time (MVP)** — source: ADR-013
- **No physics simulation: objects follow finger directly — no rigidbody, no inertia** — source: ADR-013
- **PuzzleLockAll uses `HashSet<string>` token-based locking** — objects locked when set non-empty; unlocked only when set empty — source: SP-006
- **Legal locker IDs as predefined constants**: `InteractionLockerId.ShadowPuzzle = "shadow_puzzle"`, `InteractionLockerId.Narrative = "narrative"`, `InteractionLockerId.Tutorial = "tutorial"` — source: SP-006
- **Unlock with unknown token is a no-op with warning log** — source: ADR-006, SP-006
- **On `Evt_SceneUnloadBegin`, lock token set is force-cleared** — source: ADR-006
- **All parameters (gridSize, rotationStep, snapSpeed, bounds) from Luban config** — source: ADR-013
- **Haptic feedback gated by `Settings.haptic_enabled`** — source: ADR-013

#### Performance Guardrails

- **Object Interaction total update (10 objects)**: ≤ 1.0ms/frame — source: ADR-013
- **Drag response**: ≤ 16ms (1 frame) — source: ADR-013

### 3.3 Puzzle State Machine (ADR-014)

#### Required Patterns

- **7-state FSM: Locked → Idle → Active → NearMatch → PerfectMatch → AbsenceAccepted → Complete** — source: ADR-014
- **PerfectMatch and AbsenceAccepted are irreversible terminal transitions** — matchScore calculation freezes — source: ADR-014
- **NearMatch entry/exit hysteresis: entry at 0.40, exit at 0.35** (5% band, configurable per puzzle) — source: ADR-014
- **Absence puzzle detection: `matchScore ≥ maxCompletionScore` AND `idleTime ≥ absenceAcceptDelay` (default 5s) AND player not interacting** — source: ADR-014
- **Tutorial grace period (3s): blocks PerfectMatch/AbsenceAccepted transitions after tutorial step completion** — source: ADR-014
- **PuzzleState enum maps directly to IChapterProgress serialization** — source: ADR-014
- **Per-chapter threshold overrides from Luban `TbPuzzle`** — source: ADR-014

---

## 4. Feature Layer

### 4.1 Narrative Sequence Engine (ADR-016, SP-008)

#### Required Patterns

- **Time-sorted atomic effect sequence playback** — 10 effect types (AudioDucking, ColorTemperature, SFXOneShot, CameraShake, ScreenFade, TextureVideo, ObjectSnap, LightIntensity, ShadowFade, ObjectFade) — source: ADR-016
- **All sequence definitions in Luban tables** — no narrative logic in code — source: ADR-016
- **3 sequence types: MemoryReplay, ChapterTransition, AbsencePuzzle** — source: ADR-016
- **`PuzzleLockAll` + `InputBlocker` during all sequences** — with token-based matching per sequence ID — source: ADR-016
- **Sequence chain: `nextSequenceId` + `nextSequenceDelay` in config enables zero-gap sequence linking** — source: SP-008
- **Sequence queue: max 3; FIFO; overflow dropped with `Log.Warning`** — source: ADR-016
- **Graceful resource failure: missing asset → skip effect, continue sequence, `Log.Warning`** — source: ADR-016
- **Pre-warm video/audio assets when puzzle enters Active state** — source: ADR-016
- **Unload video assets after sequence completes** — source: ADR-016

#### Forbidden Approaches

- **Never hardcode sequence content in C# methods** — source: ADR-016

### 4.2 Hint System (ADR-015)

#### Required Patterns

- **3-tier progressive hints: Ambient (L1) → Directional (L2) → Explicit (L3)** — source: ADR-015
- **Composite trigger formula: `triggerScore = timeScore + failScore + stagnationScore + matchPenalty`** — escalates at `triggerScore ≥ 1.0` — source: ADR-015
- **Read matchScore/anchorScores from Shadow Puzzle as read-only queries at 1s polling interval** — NOT per-frame — source: ADR-015
- **Layer 3: player-triggered only (button press), max 3 uses per puzzle** — source: ADR-015
- **Hint target selection: `argmin(anchorScore_i)` where `anchorWeight_i >= minWeight`** — source: ADR-015
- **Tutorial integration: pause all hint timers on `Evt_TutorialStepStarted`; reset to 0 on `Evt_TutorialStepCompleted`** — source: ADR-015
- **Per-chapter `hintDelayOverride` scaling**: Ch.1-2 = 1.0, Ch.3 = 1.3, Ch.4-5 = 1.5 — source: ADR-015
- **All parameters from Luban `TbHintConfig`** — source: ADR-015

#### Forbidden Approaches

- **Never write to Shadow Puzzle System from Hint System** — read-only access only — source: ADR-015

#### Performance Guardrails

- **Hint system update**: < 0.5ms/frame — source: ADR-015

### 4.3 Audio Mix (ADR-017)

#### Required Patterns

- **3 independent mix layers: Ambient, SFX, Music** — each with independent volume — source: ADR-017
- **Volume formula: `finalVolume = clipBaseVolume × layerVolume × masterVolume × duckingMultiplier`** — source: ADR-017
- **`sfx_enabled` toggle controls ONLY the SFX layer — Ambient layer is UNAFFECTED** — source: ADR-017
- **`ambientBaseVolume = 0.6` is an internal design baseline — NOT player-facing, NOT in PlayerPrefs** — source: ADR-017
- **Ducking affects Ambient + Music layers; SFX is NOT affected by ducking** — source: ADR-017
- **Music crossfade: dual-AudioSource strategy for seamless chapter transitions** (default 3.0s) — source: ADR-017
- **SFX concurrency: max 4 per sfxId; oldest killed on overflow** — source: ADR-017
- **All audio playback routes through `IAudioService` backed by TEngine AudioModule** — source: ADR-017
- **All SFX event definitions from Luban `TbAudioEvent`** — source: ADR-017
- **Audio system is fully reactive via GameEvent listeners** — never polls — source: ADR-017

#### Forbidden Approaches

- **Never use Unity Audio Mixer Groups** — ducking via AudioSource volume manipulation — source: ADR-017
- **Never create AudioSources directly** — use TEngine AudioModule — source: ADR-017
- **Never integrate FMOD or Wwise** — source: ADR-017

---

## 5. Presentation Layer

### 5.1 URP Shadow Rendering (ADR-002, SP-005)

#### Required Patterns

- **URP native directional light cascade shadow map + custom WallReceiver Shader + ShadowSampleCamera** — source: ADR-002
- **WallReceiver shader: pure HLSL custom Unlit shader** — NOT ShaderGraph — source: ADR-002, SP-005
- **ShadowRT: R8 (8-bit grayscale), 512×512, ~256KB memory** — source: ADR-002
- **AsyncGPUReadback: `NativeArray<byte>` data only valid in callback — consume immediately or copy** — source: ADR-002
- **3 quality tiers: High (2048 shadow map, 4 cascades, soft shadows), Medium (1024, 2 cascades, soft), Low (512, 1 cascade, hard)** — source: ADR-002
- **Chapter visual style presets via Luban config — transition via DOTween lerp 1.2s** — source: ADR-002
- **ShadowSampleCamera uses `CameraType.Game` + independent render target — not added to Camera Stack** — source: ADR-002
- **Shadow quality tier is a passive listener of `Evt_QualityTierChanged`** — does not self-manage frame timing — source: SP-010
- **WallReceiver shader must be SRP Batcher compatible (use CBUFFER declarations)** — source: ADR-002
- **WallReceiver shader `#include` uses URP package relative paths** — source: ADR-002

#### Performance Guardrails

- **ShadowRT CPU readback**: ≤ 1.5ms — source: ADR-002
- **Shadow pass additional draw calls**: < 20 — source: ADR-002
- **ShadowRT memory**: ~256 KB — source: ADR-002
- **WallReceiver shader GPU time**: < 0.5ms — source: ADR-002
- **Auto-degradation response**: ≤ 5 frames (~83ms) — source: ADR-002

### 5.2 UIWindow Management (ADR-011, SP-002)

#### Required Patterns

- **All 9 panels managed via `GameModule.UI.ShowWindow<T>()` / `CloseWindow<T>()`** — source: ADR-011
- **5 UI layer levels with sorting order bases**: Background(0), HUD(100), Popup(200), Overlay(300), System(400) — source: ADR-011
- **Popup and Overlay layers auto-push InputBlocker on open, auto-pop on close** — token format `"UIPanel_{ClassName}"` — source: ADR-011
- **Popup queue: max 1 visible Popup at a time; FIFO queue for excess** — Overlay not subject to queue — source: ADR-011
- **UIWindow lifecycle: `OnCreate` (first open only) → `OnRefresh` (each show, including first) → `OnUpdate` (per frame, visible only) → `OnClose`/`OnDestroy` (cleanup)** — source: ADR-011, SP-002
- **`OnUpdate` executes ONLY when `IsPrepare == true && Visible == true`** — hidden panels have zero CPU cost — source: SP-002
- **TutorialPromptPanel uses InputFilter (not InputBlocker)** — HUD layer, whitelist-only — source: ADR-011
- **All UI prefab loading via `GameModule.Resource.LoadAssetAsync`** — source: ADR-011
- **High-frequency panels (HUDPanel) pre-loaded at scene load; closed panels hidden (not destroyed)** — source: ADR-011
- **Internal widget events: `UIWindow.AddUIEvent()`; cross-system events: `GameEvent`** — source: ADR-011
- **Safe area: `SetUISafeFitHelper` on root Canvas** — source: ADR-011
- **CanvasScaler: Scale With Screen Size, reference 1920×1080, match width 0.5** — source: ADR-003
- **Minimum touch target: 44×44dp (Apple HIG baseline), prefer 48dp** — source: ADR-003
- **Minimum text: 14sp body, 12sp captions** — source: ADR-003

#### Forbidden Approaches

- **Never use `Resources.Load` for UI prefabs** — source: ADR-011
- **Never reference panels across systems directly** — use GameEvent for decoupling — source: ADR-011
- **Never use UI Toolkit** — TEngine UIModule is UGUI-based — source: ADR-011

---

## 6. Luban Config Access (ADR-007, SP-004)

### Required Patterns

- **All config reads via `Tables.Instance.TbXXX.Get(id)`** — single access pattern for all 11+ tables — source: ADR-007
- **`Tables.Init()` executes at boot Step 7 (ProcedureMain) — MUST complete before ANY system reads config** — source: ADR-007
- **Config data objects are read-only after `Init()`** — never modify a field on a Luban-generated data object at runtime — source: ADR-007
- **Extension methods for derived config values in GameLogic assembly** — never modify GameProto — source: ADR-007
- **Tables is safe for UniTask async reads** — UniTask continuations execute on main thread; no synchronization needed — source: ADR-007, SP-004
- **Tables is NOT a singleton**: it's a regular class held by `ConfigSystem` — access via `ConfigSystem`'s property — source: SP-004

### Forbidden Approaches

- **Never hardcode gameplay values** (`float threshold = 0.85f;` etc.) — all from Luban config — source: ADR-007
- **Never hand-edit Luban generated files in GameProto** — regeneration overwrites silently — source: ADR-007
- **Never cache config table references in persistent fields** — always read through `Tables.Instance` each time (exception: single method scope for per-frame hot-path) — source: ADR-007
- **Never access Luban Tables from `UniTask.Run()` (thread pool)** — main thread only — source: SP-004
- **Never use string reflection to access config fields** — use generated C# property names — source: ADR-007

---

## 7. Platform & Build Rules (ADR-003, ADR-004)

### Required Patterns

- **Mobile is the primary target platform** — all design and performance baselines from mobile constraints — source: ADR-003
- **Input abstraction from day one** — `IInputAdapter` interface; gameplay code never directly calls `Input.GetTouch()` — source: ADR-003
- **Quality tier auto-detection on first launch** (GPU model, RAM, thermal state) → Low/Medium/High — source: ADR-003
- **Texture compression: ASTC (iOS), ETC2 baseline / ASTC preferred (Android), DXT5/BC7 (PC Phase 2)** — source: ADR-003
- **Audio: Vorbis (Android/PC) / AAC (iOS); streaming for music >30s; 44.1kHz music, 22.05kHz SFX** — source: ADR-003
- **Sprite atlasing: SpriteAtlas per UI screen, max 2048×2048 atlas** — source: ADR-003
- **Orientation: landscape primary; portrait not required** — source: ADR-003
- **IL2CPP mandatory for both iOS and Android release builds** — source: ADR-004
- **HybridCLR: run `AOTGenericReference` generation before every release build** — CI must fail if unregistered generics detected — source: ADR-004
- **AsyncGPUReadback callback in hot-fix DLL: verified as HIGH risk for HybridCLR AOT; if real device fails, move callback to Default assembly behind `IShadowRTReader` interface** — source: SP-007

### Forbidden Approaches

- **Never design for PC headroom** — if a system needs more than its mobile budget, implement LOD/quality scaling — source: ADR-003
- **Never use Unity New Input System package** — legacy Touch API sufficient; TEngine not integrated with it — source: ADR-010

---

## 8. Testing Requirements

### Required Patterns

- **Framework: Unity Test Framework (NUnit)** — source: tech-prefs
- **Minimum coverage: 70% for gameplay logic** (puzzle mechanics, shadow calculation) — source: tech-prefs
- **Required test subjects: shadow matching algorithms, puzzle state machines, resource load/unload lifecycle, event dispatch correctness** — source: tech-prefs
- **Zero resource leaks after 10 consecutive chapter transitions** (verified via Unity Memory Profiler) — source: ADR-005, ADR-009
- **Zero synchronous load calls in entire codebase** (verified via automated grep / Roslyn analyzer) — source: ADR-005
- **Zero `LoadSceneMode.Single` in codebase** — source: ADR-009
- **Zero `DontDestroyOnLoad` in chapter scene scripts** — source: ADR-009
- **Every `LoadAssetAsync` site must have a corresponding `Release()` in same class** — source: ADR-005
- **PuzzleLockAll multi-sender token test: lock(A) → lock(B) → unlock(A) → still locked → unlock(B) → unlocked** — source: ADR-027 (inherits ADR-006 §4)
- **Event listener leak test: after scene transition, no system holds orphaned listeners** — source: ADR-027 (inherits ADR-006 §3)
- **Save system round-trip: serialize → deserialize → assert all fields equal** — source: ADR-008
- **Save corruption fallback: corrupt primary → loads backup; corrupt both → fresh start** — source: ADR-008

---

## 9. Quick Reference: Init Order

From Architecture §5.6 and ADR-004/ADR-007:

```
[Default Assembly — AOT]
 1. Unity loads BootScene
 2. TEngine RootModule.Init()
 3. ProcedureLaunch → screen orientation, frame rate
 4. ProcedureSplash → splash screen
 5. ProcedureLoadAssembly:
    5a. YooAsset init + resource package update check
    5b. Download GameLogic.dll + GameProto.dll
    5c. HybridCLR AOT metadata registration
    5d. Assembly.Load(GameProto) then Assembly.Load(GameLogic)
    5e. Reflection-invoke GameEntry.Start()

[GameLogic Assembly — Hot-Fix]
 6. ProcedureMain
 7. Tables.Init() ← ALL CONFIG TABLES LOADED
 8. LoadSceneAsync("MainScene", Additive)
 9-10. Foundation layer init (Input, Object Interaction)
11. SaveSystem.LoadAsync()
12-15. Feature/Meta layer init (ChapterState, Audio, Hint, etc.)
16. ShowMainMenu
```

---

## 10. Quick Reference: GameEvent Interface Catalog

> 旧的 100-ID per-system 分配表已由 ADR-027 取代。Event ID 由 Source Generator 自动生成；开发者按 domain interface 组织事件。下表为 ADR-027 接口清单（GroupUI / GroupLogic）。详细 EventId→Interface 映射见 `architecture-traceability.md` 附录 A。

### GroupLogic（业务事件）

| Interface | Domain | Layer | ADR |
|-----------|--------|-------|-----|
| `IGestureEvent` | Input（手势分发） | Foundation | ADR-010 + ADR-027 |
| `IObjectInteractionEvent` | Object Interaction | Core | ADR-013 + ADR-027 |
| `IPuzzleEvent` | Shadow Puzzle（匹配状态） | Core | ADR-014 + ADR-027 |
| `IPuzzleLockEvent` | Puzzle Lock Token 协议 | Core | ADR-027（继承 ADR-006 §4） |
| `IChapterEvent` | Chapter State | Core | ADR-027 |
| `ISceneEvent` | Scene Lifecycle（8 个生命周期事件合并） | Foundation | ADR-009 + ADR-027 |
| `INarrativeEvent` | Narrative Sequence | Feature | ADR-016 + ADR-027 |
| `IAudioEvent` | Audio（Ducking / SFX / Music） | Feature | ADR-017 + ADR-027 |
| `IHintEvent` | Hint | Feature | ADR-015 + ADR-027 |
| `ITutorialEvent` | Tutorial Step | Presentation | ADR-019 (placeholder) + ADR-027 |
| `ISaveEvent` | Save 完成通知 | Presentation | ADR-008 + ADR-027 |
| `ISettingsEvent` | Settings 变更（含 TouchSensitivity） | Presentation | ADR-010 + ADR-027 |
| `IShadowRTEvent` | Shadow RenderTexture 更新 | Presentation | ADR-002 + ADR-027 |

### GroupUI（UI 通知）

| Interface | Domain | Layer | ADR |
|-----------|--------|-------|-----|
| `I{PanelName}UI`（逐面板） | UIWindow 展开/关闭 | Presentation | ADR-011 + ADR-027 |
| `IUIWindowEvent` | Popup 队列通知 | Presentation | ADR-011 + ADR-027 |

---

*End of Control Manifest*
