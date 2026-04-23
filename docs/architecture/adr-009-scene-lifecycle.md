// 该文件由Cursor 自动生成

# ADR-009: Scene Lifecycle & Additive Scene Strategy

## Status

Proposed

## Date

2026-04-22

## Last Verified

2026-04-22

## Decision Makers

Technical Director, Lead Programmer

## Summary

《影子回忆 (Shadow Memory)》uses a 5-chapter structure where each chapter is a distinct Unity scene with unique environments, lighting, and puzzles. This ADR establishes an additive-only scene lifecycle strategy: a persistent MainScene holds all managers, UI, audio, and camera infrastructure, while chapter scenes are loaded/unloaded one at a time through an 11-step async transition flow with 8 lifecycle events, mandatory inter-scene cleanup, and YooAsset hot-update download integration.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.62f2 (LTS) |
| **Asset System** | YooAsset 2.3.17 via TEngine ResourceModule |
| **Domain** | Core / Scene Management |
| **Knowledge Risk** | MEDIUM — TEngine SceneModule wrapper and YooAsset SceneHandle lifecycle specifics may not be in LLM training data; must verify against project source |
| **References Consulted** | ADR-001 (TEngine Framework), ADR-005 (YooAsset Resource Loading & Lifecycle), ADR-027 (GameEvent Interface Protocol — supersedes ADR-006 §1/§2 for event ID allocation; ADR-006 §3-§6 lifecycle/token/ordering still inherited), project source (`TEngine/` directory), `design/gdd/scene-management.md`, `docs/engine-reference/unity/VERSION.md` |
| **Post-Cutoff APIs Used** | `GameModule.Resource.LoadSceneAsync()`, `GameModule.Resource.UnloadSceneAsync()`, `SceneManager.SetActiveScene()`, `Resources.UnloadUnusedAssets()`, YooAsset `SceneHandle`, `ResourcePackage` download API |
| **Verification Required** | Sprint 0 spike: confirm `SetActiveScene()` works correctly on additively loaded scenes; confirm YooAsset resource package download status query API; confirm `UnloadSceneAsync(SceneHandle)` fully releases scene references |

> **Note**: If Knowledge Risk is MEDIUM or HIGH, this ADR must be re-validated if the
> project upgrades engine or YooAsset versions. Flag it as "Superseded" and write a new ADR.

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-001 (TEngine 6.0 Framework — `GameModule.Resource` and `GameModule.Scene` wrappers), ADR-005 (YooAsset Resource Loading & Lifecycle — SceneHandle ownership and cleanup patterns) |
| **Enables** | All chapter-based gameplay implementation; Narrative sequence system (scene-aware transitions); Tutorial system (first-chapter onboarding flow) |
| **Blocks** | Scene transition implementation; chapter-based gameplay; any system that reacts to scene lifecycle events (Shadow Puzzle init/cleanup, Audio BGM switching, Narrative sequence gating) |
| **Ordering Note** | ADR-005 must reach Accepted first — this ADR consumes the SceneHandle ownership pattern and cleanup sequence defined there. ADR-027 must also be Accepted — all 8 scene events use the `[EventInterface]` protocol defined there (implementation will expose `ISceneEvent` / `ISceneUI` interfaces; see §3.4 below). ADR-006 §3-§6 inherited by ADR-027 (lifecycle, multi-sender token, ordering). |

## Context

### Problem Statement

《影子回忆》is a narrative puzzle game with 5 chapters, each occupying a separate Unity scene with distinct environments, interactive objects, lighting setups, and puzzle configurations. The game needs a scene management strategy that:

1. **Preserves persistent infrastructure** — UI Canvas, AudioListener, Camera Rig, and all system managers must survive chapter transitions without `DontDestroyOnLoad` hacks that scatter persistent objects across the scene hierarchy
2. **Enforces single-chapter memory** — Mobile devices have a hard memory ceiling (~1.5 GB). At any moment only one chapter scene may be in memory alongside the persistent MainScene. Cross-chapter asset accumulation is a P0 risk.
3. **Supports async-only loading** — TR-concept-005 forbids synchronous loading. Scene transitions must be fully async via UniTask without stalling the main thread.
4. **Coordinates multi-system transitions** — Scene transitions affect at least 6 systems (Puzzle, Narrative, Audio, UI, Input, Chapter State). Each needs targeted lifecycle events to perform cleanup or initialization at the correct phase of the transition.
5. **Integrates YooAsset hot-update** — Chapter scene resource packages may need to be downloaded on first access. The transition flow must accommodate a download step with progress reporting before the actual scene load.
6. **Handles errors gracefully** — A failed scene load must not leave the game in a broken state. The persistent MainScene must remain functional as a fallback, and the player must have a clear recovery path.

### Current State

ADR-005 established the handle-ownership resource lifecycle pattern and defined the Scene Transition Cleanup Sequence (6 steps). ADR-027 (supersedes ADR-006 §1/§2) mandates that all inter-module events use `[EventInterface]` C# interfaces — the 8 scene lifecycle events will therefore be exposed as `ISceneEvent` interface methods (e.g. `OnSceneTransitionBegin`, `OnSceneLoadProgress`, `OnSceneReady`), with IDs auto-generated by the TEngine Roslyn Source Generator. The scene-management GDD (`design/gdd/scene-management.md`) provides the full design specification including the transition flow, state machine, memory budgets, fade timing, and edge cases. However, no architectural decision has been made to:
- Formalize the additive-only loading mandate with its specific MainScene persistent architecture
- Define the complete 11-step transition flow as a binding contract
- Specify the Scene Manager's state machine and mutex semantics
- Establish the relationship between the 8 GameEvent lifecycle events and the transition steps
- Set error recovery and fallback behavior as architectural requirements

### Constraints

- **Mobile memory ceiling**: 1.5 GB total; only BootScene + MainScene + 1 chapter scene permitted in memory simultaneously
- **Async-only mandate**: TR-concept-005 forbids all synchronous scene/asset loading; frame budget is 16.67ms
- **TEngine wrapper requirement**: TR-concept-007 forbids bypassing `GameModule.XXX` accessors; direct `SceneManager.LoadSceneAsync()` and `YooAssets` calls are forbidden
- **GameEvent-only communication**: ADR-027 mandates all inter-module communication through `GameEvent` with the `[EventInterface]` + Source Generator scheme (supersedes ADR-006 §1/§2's const int allocation); ADR-006 §3-§6 lifecycle/token/ordering protocols are inherited unchanged
- **HybridCLR assembly boundary**: Scene Manager implementation resides in the `GameLogic` assembly (hot-updatable); scene configuration is driven by Luban `TbChapter.sceneId`
- **No DontDestroyOnLoad**: Chapter scenes must be self-contained — no chapter-scene objects may use `DontDestroyOnLoad`. All persistent objects live in MainScene.

### Requirements

- Additive scene loading exclusively — `LoadSceneMode.Single` is forbidden
- MainScene loaded once at boot, never unloaded for the lifetime of the application
- One chapter scene active at any time; previous chapter fully unloaded before next loads
- 8 lifecycle events fired in deterministic order during transitions
- Mandatory memory cleanup (`UnloadUnusedAssets` + `GC.Collect`) between every chapter transition
- YooAsset resource package download check before scene load; progress reporting via GameEvent
- Scene transition total time < 3s for locally cached scenes (including fade animations)
- Error recovery: failed load → error UI → retry or return to main menu; MainScene remains functional

## Decision

**Adopt an additive-only scene lifecycle with a persistent MainScene, single-chapter-at-a-time loading, an 11-step async transition flow, 8 lifecycle events, and mandatory inter-scene cleanup.**

### Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Unity Process                             │
│                                                                  │
│  BootScene (Build Settings only scene, non-hot-update)           │
│    └── TEngine RootModule init → HybridCLR → ProcedureMain      │
│                                                                  │
│  MainScene (persistent, Additive, hot-updatable)                 │
│    ├── UI Canvas ─────────── all UIWindows (ADR-011)             │
│    ├── AudioListener ─────── AudioSource pools                   │
│    ├── Camera Rig ────────── Main Camera + ShadowSampleCamera    │
│    ├── Manager GameObjects ─ all system managers                 │
│    ├── EventSystem ───────── Unity EventSystem singleton         │
│    └── Transition Overlay ── fullscreen Canvas for fade effects  │
│                                                                  │
│  Chapter Scene (Additive, one at a time, hot-updatable)          │
│    ├── Chapter_01_Approach ── environment, lights, puzzle anchors │
│    ├── Chapter_02_SharedSpace                                    │
│    ├── Chapter_03_SharedLife                                      │
│    ├── Chapter_04_Loosening                                       │
│    └── Chapter_05_Absence                                        │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │              Scene Manager (in MainScene)                    │ │
│  │                                                              │ │
│  │  State Machine: Idle → TransitionOut → Unloading →           │ │
│  │                 Loading → TransitionIn → Idle                │ │
│  │                          ↘ Error → Idle                      │ │
│  │                                                              │ │
│  │  Holds: _currentSceneHandle (SceneHandle, ADR-005)           │ │
│  │  Holds: _pendingRequest (max 1 queued request)               │ │
│  │  Fires: 8 scene lifecycle events via ISceneEvent (ADR-027)  │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### Additive-Only Loading Rules

1. **MainScene** is loaded once during `ProcedureMain` via `GameModule.Resource.LoadSceneAsync("MainScene", LoadSceneMode.Additive)`. It is never unloaded for the lifetime of the application.
2. **Chapter scenes** are loaded via `GameModule.Resource.LoadSceneAsync(sceneName, LoadSceneMode.Additive)`. Only one chapter scene may exist at any time.
3. After loading a chapter scene, `SceneManager.SetActiveScene()` is called on the new scene so that its lighting settings, skybox, and default instantiation target are applied.
4. `LoadSceneMode.Single` is **forbidden** project-wide. A Roslyn analyzer or code review checklist item must flag any use.
5. No chapter-scene object may call `DontDestroyOnLoad()`. All persistent state lives in MainScene managers.

### Scene Manager State Machine

```
                 Evt_RequestSceneChange
                        │
                        ▼
┌──────┐  ──────────>  ┌────────────────┐
│ Idle │               │ TransitionOut  │  (Fade Out + lock input)
└──────┘  <──────────  └───────┬────────┘
    ▲         error            │ fade complete
    │         recovery         ▼
    │                   ┌────────────────┐
    │                   │   Unloading    │  (SceneUnloadBegin + unload + cleanup)
    │                   └───────┬────────┘
    │                           │ cleanup complete
    │                           ▼
    │                   ┌────────────────┐
    │       ┌───────    │    Loading     │  (download check + load + SetActive)
    │       │  error    └───────┬────────┘
    │       ▼                   │ load complete
    │  ┌─────────┐              ▼
    │  │  Error  │       ┌────────────────┐
    │  └────┬────┘       │ TransitionIn   │  (SceneReady + Fade In)
    │       │ recover    └───────┬────────┘
    └───────┘                    │ fade complete
              ◄──────────────────┘ → Idle
```

**Mutex Rules**:
- Only one transition may execute at a time
- Requests received during a transition are queued (max queue depth = 1; new request overwrites pending)
- The queue is checked when returning to Idle; if pending, immediately begin next transition
- Request for the same scene as the current scene is ignored with a `SceneReady` confirmation

### 11-Step Transition Flow

This is the binding contract for all scene transitions. Each step must execute in order; no step may be skipped.

```
Step  Action                                          Event Fired
────  ──────────────────────────────────────────────  ──────────────────────────
 1    Receive Evt_RequestSceneChange(targetChapterId)  (incoming event)
 2    Mutex check: if transitioning → queue (max 1)    —
 3    Lock input; begin transition                     Evt_SceneTransitionBegin
      → UI starts fade overlay
      → Audio starts music fadeout
 4    Await FadeOut animation                          —
      (EaseInCubic, ~0.8s × emotionalWeight)
 5    Notify systems to cleanup                        Evt_SceneUnloadBegin
      → Puzzle: release runtime data
      → Narrative: abort active sequences
      → Systems: remove scene-scoped listeners
 6    Await UnloadSceneAsync(_currentSceneHandle)      —
      via GameModule.Resource
 7    Mandatory cleanup:                               —
      Resources.UnloadUnusedAssets()
      + GC.Collect()
 8    Check resource package status for target scene   Evt_SceneDownloadProgress
      → If not downloaded: await download              (progress updates)
      → If downloaded: skip
 9    Await LoadSceneAsync(targetScene, Additive)      Evt_SceneLoadProgress
      via GameModule.Resource                          (progress updates)
10    SceneManager.SetActiveScene(new scene)           Evt_SceneLoadComplete
      → Pass bgmAsset info in payload                  (with bgmAsset)
      → Puzzle: initialize chapter data
      → Audio: switch BGM
11    Signal scene ready; begin fade in                Evt_SceneReady
      Await FadeIn (EaseOutCubic, ~1.2s × weight)
      Unlock input                                    Evt_SceneTransitionEnd
```

**First Boot Special Case**: Steps 5–7 are skipped when no previous chapter scene is loaded (first chapter load after boot or new game). The flow proceeds directly from step 4 to step 8.

**Error Handling**: If `LoadSceneAsync` fails at step 9:
1. Fire `Evt_SceneLoadFailed` with chapter ID and error message
2. Retry up to `MAX_LOAD_RETRY` (default: 2) times
3. If all retries fail: show error UI with "Retry" and "Return to Main Menu" options
4. Transition to Error state; MainScene remains fully functional
5. On recovery (user presses Retry or Return): transition back to Idle

### 8 Scene Lifecycle Events

All events use the GameEvent Interface Protocol (ADR-027). The Scene Management module exposes its 8 lifecycle signals through an `ISceneEvent` interface (method-per-event, `[EventInterface(EEventGroup.GroupLogic)]`); Event IDs are auto-generated by TEngine Roslyn Source Generator (`RuntimeId.ToRuntimeId("ISceneEvent.OnXxx")`). Legacy `Evt_Scene*` → `ISceneEvent.OnXxx` mapping: see `architecture-traceability.md` 附录 A.

| Event | ID | When Fired | Payload | Key Consumers |
|-------|-----|-----------|---------|---------------|
| `Evt_SceneTransitionBegin` | 1400 | Step 3: transition starts | `{ int fromChapterId, int toChapterId }` | UI (fade overlay), Audio (music fadeout), Input (lock) |
| `Evt_SceneUnloadBegin` | 1401 | Step 5: before unload | `{ int chapterId }` | Puzzle (cleanup), Narrative (abort), all systems (remove scene-scoped listeners) |
| `Evt_SceneLoadProgress` | 1402 | Step 9: during load | `{ string sceneName, float progress }` | UI (loading indicator) |
| `Evt_SceneDownloadProgress` | 1403 | Step 8: during download | `{ float progress, long downloadedBytes, long totalBytes }` | UI (download progress bar) |
| `Evt_SceneLoadComplete` | 1404 | Step 10: load finished | `{ int chapterId, string bgmAsset }` | Puzzle (init), Audio (BGM switch), Chapter State (confirm) |
| `Evt_SceneReady` | 1405 | Step 11: before fade in | `{ int chapterId }` | Chapter State (scene confirmed ready) |
| `Evt_SceneTransitionEnd` | 1406 | Step 11: after fade in | `{ int chapterId }` | Input (unlock), all systems (normal operation resumes) |
| `Evt_SceneLoadFailed` | 1407 | Error: load failure | `{ int chapterId, string error }` | UI (error dialog with retry) |

**Event ordering guarantee**: Events fire in the exact order listed above during a normal transition (1400 → 1401 → 1403? → 1402 → 1404 → 1405 → 1406). `Evt_SceneDownloadProgress` only fires if a download is needed. `Evt_SceneLoadFailed` only fires on error and replaces the normal 1404→1405→1406 tail.

### Key Interfaces

```csharp
// Scene Manager public API — resides in GameLogic assembly
public interface ISceneManager
{
    SceneManagerState CurrentState { get; }
    int CurrentChapterId { get; }
    bool IsTransitioning { get; }
}

public enum SceneManagerState
{
    Idle,
    TransitionOut,
    Unloading,
    Loading,
    TransitionIn,
    Error
}

// Scene transition is triggered exclusively via GameEvent:
// GameEvent.Send(EventId.Evt_RequestSceneChange, new RequestSceneChangePayload
// {
//     TargetChapterId = 3
// });

// Scene Manager is a listener, not a callable service.
// Other systems interact with it only through GameEvents.
```

### SceneHandle Ownership

Per ADR-005, the Scene Manager is the sole owner of the chapter scene's `SceneHandle`:

```csharp
// Scene Manager holds exactly one SceneHandle at a time
private SceneHandle _currentSceneHandle;
private int _currentChapterId = -1;

// Load: acquire handle
_currentSceneHandle = await GameModule.Resource.LoadSceneAsync(
    sceneName, LoadSceneMode.Additive);

// Unload: release handle
await GameModule.Resource.UnloadSceneAsync(_currentSceneHandle);
_currentSceneHandle = null;
```

The `SceneHandle` is held for the entire duration a chapter scene is active. It is released only during the Unloading step. No other system may hold or release the SceneHandle — this is the Scene Manager's exclusive responsibility.

### Fade Transition Specification

| Parameter | Value | Source |
|-----------|-------|--------|
| `FADE_BASE_OUT` | 0.8s | `scene-management.md` tuning knob |
| `FADE_BASE_IN` | 1.2s | `scene-management.md` tuning knob |
| `emotionalWeight` | 0.8–1.5 per chapter | Luban `TbChapter` config |
| Fade Out curve | `EaseInCubic` | Slow start, fast finish (like closing eyes) |
| Fade In curve | `EaseOutCubic` | Fast start, slow finish (like opening eyes) |
| Overlay color | Chapter-specific (see GDD) | Art Bible emotional color mapping |
| Minimum black screen | 0.5s | Prevents flicker on fast loads |

Fade animations are driven by the Transition Overlay Canvas in MainScene. They must maintain 60fps regardless of background loading activity. The fade coroutine/tween operates independently of the async load pipeline.

### Startup Flow

```
[App Launch]
    │
    ▼
BootScene (Unity auto-loads, non-hot-update)
    ├── TEngine RootModule init
    ├── HybridCLR load hot-update DLLs
    ├── YooAsset init + check for updates
    │     ├── Updates available → download patches (show progress UI)
    │     └── No updates → continue
    │
    ▼
ProcedureMain
    ├── await LoadSceneAsync("MainScene", Additive)
    │     → MainScene persistent scene ready
    │     → Scene Manager initializes (state = Idle, no chapter loaded)
    │
    ├── Read save data (Save System)
    │     ├── Save exists → get currentChapterId
    │     └── No save → currentChapterId = 1
    │
    ├── Show main menu UI
    │     ├── "Continue" → GameEvent.Send(Evt_RequestSceneChange, chapterId)
    │     └── "New Game" → GameEvent.Send(Evt_RequestSceneChange, 1)
    │
    ▼
[Normal game loop — Scene Manager handles transition]
```

### Forbidden Patterns

```csharp
// ╳ FORBIDDEN: Single scene loading (destroys MainScene)
SceneManager.LoadScene("Chapter01", LoadSceneMode.Single);

// ╳ FORBIDDEN: Direct Unity SceneManager (bypasses TEngine/YooAsset)
SceneManager.LoadSceneAsync("Chapter01", LoadSceneMode.Additive);

// ╳ FORBIDDEN: DontDestroyOnLoad in chapter scenes
void Start() { DontDestroyOnLoad(gameObject); } // in chapter scene object

// ╳ FORBIDDEN: Multiple chapter scenes loaded simultaneously
await GameModule.Resource.LoadSceneAsync("Chapter02", LoadSceneMode.Additive);
// without first unloading Chapter01

// ╳ FORBIDDEN: Skipping cleanup between scenes
await GameModule.Resource.UnloadSceneAsync(_currentSceneHandle);
// Missing: Resources.UnloadUnusedAssets() + GC.Collect()
await GameModule.Resource.LoadSceneAsync(nextScene, LoadSceneMode.Additive);

// ╳ FORBIDDEN: Synchronous scene operations
SceneManager.LoadScene("Chapter01"); // sync load

// ╳ FORBIDDEN: External systems calling Scene Manager directly
sceneManager.LoadChapter(3); // must use GameEvent: Evt_RequestSceneChange
```

### Implementation Guidelines

1. **Scene Manager as GameEvent listener**: The Scene Manager subscribes to `IChapterStateEvent.OnRequestSceneChange` (interface method — ADR-027) in `Init()` and orchestrates the entire 11-step flow internally. External systems never call Scene Manager methods directly — all communication is through GameEvent interfaces (ADR-027; legacy doc sometimes refers to this as `Evt_RequestSceneChange`, see 附录 A 映射).

2. **State machine enforcement**: The Scene Manager's `CurrentState` property gates all behavior. Incoming `RequestSceneChange` events are rejected (queued) unless state is `Idle`. State transitions are logged in debug builds for diagnostics.

3. **Fade animation independence**: The fade overlay animation must run on a separate update path (e.g., `CanvasGroup.alpha` driven by unscaled time) that is unaffected by async loading. Never tie fade animation to loading progress.

4. **Chapter-scene self-containment**: Each chapter scene's design must be fully self-contained. All environment, lighting, objects, and puzzle anchors are within the scene hierarchy. No references to objects in other chapter scenes. Lighting settings are baked per-scene and activated via `SetActiveScene()`.

5. **Luban-driven scene registry**: Scene name ↔ chapter ID mapping is read from `TbChapter.sceneId` (Luban config). No hardcoded scene names in code. This enables hot-update of scene mappings.

6. **Download timeout and retry**: Resource package download uses a 30s timeout. On timeout or failure, the Scene Manager transitions to Error state and fires `Evt_SceneLoadFailed`. The UI presents retry and return-to-menu options.

## Alternatives Considered

### Alternative 1: Additive Loading with Persistent MainScene (Chosen)

- **Description**: A single persistent MainScene holds all infrastructure (UI, Audio, Camera, Managers). Chapter scenes are loaded additively one at a time, with the previous chapter fully unloaded and cleaned up before the next loads.
- **Pros**: Clean lifecycle with explicit ownership; persistent managers survive transitions without `DontDestroyOnLoad`; only one chapter in memory at a time; deterministic cleanup point between chapters; `SetActiveScene` cleanly switches lighting/render context; compatible with YooAsset hot-update per-chapter packages.
- **Cons**: Must manage `SetActiveScene` correctly or lighting/instantiation defaults break; chapter scenes must be fully self-contained (no cross-scene references); slight architectural overhead of maintaining the persistent/transient scene split.
- **Why Chosen**: Cleanest separation of concerns; minimal memory footprint; proven pattern for mobile chapter-based games; directly supports the hot-update requirement.

### Alternative 2: Single Scene Loading (LoadSceneMode.Single)

- **Description**: Each chapter transition uses `LoadSceneMode.Single`, which destroys all current scenes and loads the new one. Persistent objects survive via `DontDestroyOnLoad`.
- **Pros**: Simpler conceptual model; Unity handles cleanup automatically; no need to manage additive scene hierarchy.
- **Cons**: `DontDestroyOnLoad` scatters persistent objects into a hidden Unity scene, making them difficult to inspect and manage. Destroys the MainScene on every transition, requiring re-creation or `DontDestroyOnLoad` for every manager, UI canvas, camera, and audio listener. Order-of-initialization bugs are common when `DontDestroyOnLoad` objects interact with newly-loaded scene objects. Incompatible with TEngine's manager lifecycle.
- **Estimated Effort**: Lower initial setup, significantly higher debugging and maintenance cost.
- **Rejection Reason**: `DontDestroyOnLoad` anti-pattern creates an unmanageable persistent object hierarchy. Destroys TEngine's structured manager lifecycle. Not viable for production mobile games with complex persistent UI.

### Alternative 3: Scene Streaming (Addressable Subscenes)

- **Description**: Break each chapter into multiple small subscenes (room sections, object groups, lighting zones) and stream them in/out based on player progress within a chapter.
- **Pros**: Enables seamless transitions within chapters; finer-grained memory control; supports open-world-style exploration if design evolves.
- **Cons**: Massive architectural complexity for a game with 5 discrete, self-contained rooms. Each chapter is a single bounded space — there is nothing to stream. Subscene boundaries create lighting seam issues. Requires careful dependency management between subscenes. YooAsset package granularity must match subscene boundaries.
- **Estimated Effort**: 3-5x more than Alternative 1, with ongoing maintenance burden.
- **Rejection Reason**: Overkill. The game's 5-chapter structure is inherently discrete, not continuous. Each chapter is a single room — subscene streaming adds complexity without benefit.

### Alternative 4: Multi-Scene Editing (All Scenes Loaded)

- **Description**: Load all chapter scenes at boot and toggle visibility/activity instead of loading/unloading.
- **Pros**: Instant chapter transitions (no load time); simplifies transition logic to scene activation toggle.
- **Cons**: All 5 chapter scenes (~90+ MB combined) in memory simultaneously, plus MainScene. Exceeds mobile memory budget. Startup time increases dramatically. Lighting conflicts between multiple active scenes. Incompatible with hot-update (all chapters must be downloaded at boot).
- **Estimated Effort**: Lower transition logic, but memory optimization work would exceed savings.
- **Rejection Reason**: Memory-prohibitive on mobile devices. A 2 GB device would be at capacity with all scenes loaded, leaving no room for runtime allocations.

## Consequences

### Positive

- **Clean manager persistence**: UI, Audio, Camera, and all managers live in MainScene and survive all transitions without any special lifecycle code. No `DontDestroyOnLoad` needed.
- **Deterministic memory management**: The mandatory cleanup step (UnloadUnusedAssets + GC.Collect) between every chapter transition creates a well-defined memory baseline. Memory profiling can verify each chapter's isolated footprint.
- **8 lifecycle events enable loose coupling**: Systems react to transition phases through GameEvents rather than direct Scene Manager callbacks. Adding a new system that needs scene awareness requires only subscribing to the relevant events — zero Scene Manager changes.
- **Hot-update capable**: Each chapter scene is a separate YooAsset resource package. Players download only the chapters they need. Post-launch scene updates can be pushed without a full app store submission.
- **Error isolation**: A failed chapter load never corrupts the persistent MainScene. The player always has a recovery path (retry or return to menu) because the UI and menu infrastructure remain intact.
- **Testable invariants**: "Only one chapter scene loaded at a time", "8 events fire in order", and "cleanup runs between every transition" are all verifiable properties.

### Negative

- **SetActiveScene management complexity**: Forgetting to call `SetActiveScene` after loading a chapter scene causes lighting to use MainScene settings (incorrect). Forgetting to handle the case where SetActiveScene fails (scene not yet fully loaded) causes an exception. This is a subtle bug source.
- **Chapter scene self-containment discipline**: Scene designers must ensure no chapter-scene object references objects in MainScene or other chapter scenes. Cross-scene references become null after unload, causing silent bugs. Requires design-time validation tooling.
- **Transition duration floor**: The mandatory fade + cleanup + load sequence has a minimum duration of ~2s even for fast loads. This is intentional (narrative pacing) but means transitions can never be "instant", which may feel slow during debugging or repeated playtesting.
- **Queue depth limitation**: Only one pending request is queued (new overwrites old). In theory, a rapid sequence of chapter-skip commands could lose intermediate requests. Acceptable for this game's linear chapter progression.

### Neutral

- The 11-step flow is more complex than a naive "unload → load" approach, but each step exists to satisfy a specific GDD or technical requirement. The complexity is inherent, not accidental.
- `SetActiveScene` is a Unity-specific concern. If the project ever migrates engines, this concept may not have a 1:1 equivalent, but the overall additive architecture pattern is engine-agnostic.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| `SetActiveScene` not applied → wrong lighting on chapter scene | MEDIUM | MEDIUM | Step 10 of flow explicitly sets active scene; automated test validates lighting source matches expected chapter |
| Memory leak if cleanup step (7) is skipped or incomplete | HIGH | HIGH | Cleanup is mandatory in the flow — never optional. Memory Profiler check after every chapter transition in QA. ADR-005's handle-ownership pattern ensures SceneHandle release. |
| SceneHandle not properly released → scene remains in memory | MEDIUM | HIGH | Scene Manager is sole SceneHandle owner (ADR-005); `_currentSceneHandle` set to null after unload; debug assertion on non-null handle during load |
| YooAsset download fails mid-transition → stuck in Loading state | MEDIUM | MEDIUM | 30s download timeout; max 2 retries; Error state with user-facing recovery UI; MainScene remains functional |
| Fade animation stutters during heavy async loading | LOW | MEDIUM | Fade driven by unscaled time on UI Canvas, independent of scene loading; loading is async and should not block main thread; profile on target devices |
| Chapter scene contains `DontDestroyOnLoad` object → leaked persistent object | LOW | HIGH | Code review + static analysis rule: no `DontDestroyOnLoad` calls in chapter scene scripts; runtime warning in debug builds |
| Race condition: rapid requests during transition → state corruption | MEDIUM | HIGH | Mutex check at step 2; single-threaded state machine; max 1 queued request; request during non-Idle state is queued, not processed |
| MainScene load fails at boot → game cannot start | LOW | CRITICAL | Fatal error: show native UI prompt ("Game data corrupted, please reinstall"); log crash report; no recovery possible (MainScene is foundational infrastructure) |

## Performance Implications

| Metric | Expected Impact | Budget | Notes |
|--------|----------------|--------|-------|
| **Scene transition total** | 2–3s (local cache, including fades) | < 3s target, < 5s acceptable | Fade Out ~0.8s + cleanup ~0.3s + load ~0.5s + Fade In ~1.2s |
| **Scene transition (first download)** | 3–10s (WiFi) | < 10s target | Download time is network-dependent; progress UI keeps player informed |
| **Post-unload cleanup** | ~200–500ms | < 500ms | `Resources.UnloadUnusedAssets()` + `GC.Collect()`; hidden behind fade overlay |
| **Memory per chapter scene** | 15–22 MB scene package; ~200–400 MB runtime (with instantiated objects and textures) | < 1000 MB runtime per chapter | Only one chapter in memory at a time |
| **Memory (MainScene persistent)** | ~60 MB | ~60 MB | UI, Audio pools, Camera, Managers — constant baseline |
| **Fade animation CPU** | < 0.5ms per frame | 16.67ms frame budget | CanvasGroup.alpha lerp — negligible |
| **Event dispatch (8 events)** | < 0.4ms total per transition | 16.67ms frame budget | ~0.05ms per `GameEvent.Get<ISceneEvent>().OnXxx()` (ADR-027, Source-Generator hashed int under the hood); events are fire-and-forget |

## Migration Plan

This is a greenfield architecture — no existing scene management system requires migration. Implementation steps:

1. **Sprint 0: Verify engine APIs** — Confirm `SceneManager.SetActiveScene()` works on additively loaded YooAsset scenes; confirm `UnloadSceneAsync(SceneHandle)` returns a completable task; confirm resource package download status query API
2. **Implement Scene Manager state machine** — `SceneManagerState` enum, mutex logic, queue management
3. **Implement 11-step transition flow** — Wire each step with the corresponding `ISceneEvent` interface method (ADR-027; legacy names map per `architecture-traceability.md` 附录 A)
4. **Implement Transition Overlay** — CanvasGroup-based fade with EaseInCubic/EaseOutCubic curves, chapter-specific overlay colors from Luban config
5. **Implement error handling** — Error state, retry logic, fallback to main menu
6. **Implement startup flow** — BootScene → MainScene load → save data read → first chapter load
7. **Integration test** — Full chapter transition cycle: load Chapter 1 → transition to Chapter 2 → verify cleanup → verify memory baseline

**Rollback plan**: If additive scene management proves unworkable with TEngine/YooAsset (e.g., `SetActiveScene` conflicts with TEngine's internal scene tracking), fall back to `LoadSceneMode.Single` with `DontDestroyOnLoad` for critical managers (Alternative 2). This is a significant architectural regression but preserves basic functionality. Estimated rollback effort: 1 week.

## Validation Criteria

- [ ] No memory leaks after 10 consecutive chapter transitions (verified via Unity Memory Profiler: memory baseline returns to within 5% after each transition)
- [ ] SceneHandle properly released on every unload (`_currentSceneHandle == null` after Unloading step; no orphaned SceneHandles in YooAsset's internal tracking)
- [ ] All 8 events fire in correct order during a normal transition: 1400 → 1401 → [1403] → 1402 → 1404 → 1405 → 1406
- [ ] `Evt_SceneLoadFailed` fires on load failure; error UI appears; retry successfully loads scene
- [ ] Hot-update download → scene load works on real mobile device (iOS and Android)
- [ ] MainScene managers survive all transitions intact (UI Canvas, AudioListener, Camera Rig, EventSystem all functional after 10 transitions)
- [ ] Only one chapter scene in memory at any time (verified via `SceneManager.sceneCount` assertion in debug builds)
- [ ] `SetActiveScene` correctly applied — new chapter scene's lighting is active (visual verification + automated lightmap reference check)
- [ ] Zero `LoadSceneMode.Single` calls in entire codebase (verified via automated grep / Roslyn analyzer)
- [ ] Zero `DontDestroyOnLoad` calls in chapter scene scripts (verified via automated grep)
- [ ] Fade animations maintain 60fps during loading (verified via frame time profiler on target device)
- [ ] Scene transition total < 3s for locally cached scenes on mid-range mobile device
- [ ] No hardcoded scene names in code — all scene names resolved from Luban `TbChapter.sceneId`

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| `design/gdd/scene-management.md` | Scene | TR-scene-001: Additive scene loading only | Additive-Only Loading Rules: `LoadSceneMode.Single` is forbidden; all scenes loaded via `LoadSceneMode.Additive` |
| `design/gdd/scene-management.md` | Scene | TR-scene-002: Persistent MainScene never unloaded | MainScene loaded once at boot, never unloaded; holds all persistent infrastructure |
| `design/gdd/scene-management.md` | Scene | TR-scene-003: Async scene loading via UniTask | All loading through `GameModule.Resource.LoadSceneAsync()` — fully async, zero synchronous calls |
| `design/gdd/scene-management.md` | Scene | TR-scene-004: SetActiveScene for lighting | Step 10 of transition flow: `SceneManager.SetActiveScene(new scene)` after load |
| `design/gdd/scene-management.md` | Scene | TR-scene-005: One chapter scene at a time | Previous chapter fully unloaded (steps 5–7) before new chapter loads (steps 8–9) |
| `design/gdd/scene-management.md` | Scene | TR-scene-006: Scene transition mutex | Step 2: mutex check; max 1 queued request; state machine prevents concurrent transitions |
| `design/gdd/scene-management.md` | Scene | TR-scene-007: YooAsset resource package check before load | Step 8: check resource package status; download if needed; fire progress events |
| `design/gdd/scene-management.md` | Scene | TR-scene-008: Fade overlay transitions | Steps 3–4 (fade out) and step 11 (fade in) with EaseInCubic/EaseOutCubic curves |
| `design/gdd/scene-management.md` | Scene | TR-scene-009: Chapter-specific overlay colors | Overlay color driven by Luban config per chapter, matching Art Bible emotional color mapping |
| `design/gdd/scene-management.md` | Scene | TR-scene-010: Input lock during transition | Step 3 fires `Evt_SceneTransitionBegin` (input lock); step 11 fires `Evt_SceneTransitionEnd` (input unlock) |
| `design/gdd/scene-management.md` | Scene | TR-scene-011: Download progress UI | `Evt_SceneDownloadProgress` (step 8) carries `{ progress, downloadedBytes, totalBytes }` for UI display |
| `design/gdd/scene-management.md` | Scene | TR-scene-012: Load failure error UI with retry | Error handling: `Evt_SceneLoadFailed` → error UI → retry up to MAX_LOAD_RETRY → fallback to main menu |
| `design/gdd/scene-management.md` | Scene | TR-scene-013: Load progress reporting | `Evt_SceneLoadProgress` (step 9) carries `{ sceneName, progress }` |
| `design/gdd/scene-management.md` | Scene | TR-scene-014: 8 scene lifecycle events | 8 events defined as `ISceneEvent.OnXxx` methods per ADR-027 (Source-Generator-hashed IDs); fired at documented steps; legacy `Evt_Scene*` names remain valid via 附录 A 映射 |
| `design/gdd/scene-management.md` | Scene | TR-scene-015: Luban-driven scene registry | Scene name ↔ chapter ID mapping from `TbChapter.sceneId`; no hardcoded scene names |
| `design/gdd/scene-management.md` | Scene | TR-scene-016: UnloadUnusedAssets after scene unload | Step 7: mandatory `Resources.UnloadUnusedAssets()` in cleanup sequence |
| `design/gdd/scene-management.md` | Scene | TR-scene-017: GC.Collect after scene unload | Step 7: mandatory `GC.Collect()` immediately after UnloadUnusedAssets |
| `design/gdd/game-concept.md` | Core | TR-concept-005: No sync loading | All scene operations are async via `GameModule.Resource` — synchronous loading is explicitly forbidden |
| `design/gdd/game-concept.md` | Core | TR-concept-010: Resource lifecycle closure | SceneHandle ownership by Scene Manager (ADR-005 pattern); mandatory cleanup between transitions ensures no resource accumulation |

## Related

- **Depends On**: ADR-001 (TEngine 6.0 Framework) — `GameModule.Resource` and `GameModule.Scene` wrappers provide the underlying scene loading capability
- **Depends On**: ADR-005 (YooAsset Resource Loading & Lifecycle) — SceneHandle ownership pattern, cleanup sequence, and handle-release semantics consumed directly by this ADR
- **Depends On**: ADR-027 (GameEvent Interface Protocol) — 8 scene lifecycle events expose as `ISceneEvent` interface methods; Event IDs auto-generated by Roslyn Source Generator. (ADR-006 §1/§2 superseded; §3/§4/§5/§6 lifecycle/token/ordering inherited.)
- **Cross-Reference**: ADR-003 (Mobile-First Platform) — Memory budgets and mobile constraints that drive the single-chapter-at-a-time requirement
- **Enables**: All chapter-based gameplay systems — Shadow Puzzle, Narrative, Audio BGM switching, Tutorial first-chapter flow
- **References**: `design/gdd/scene-management.md` — Primary design source for transition flow, state machine, memory budgets, fade timing, and edge cases
- **References**: `design/art/art-bible.md` — Chapter-specific overlay colors from the emotional color mapping
- **References**: `src/MyGame/ShadowGame/design/gdd/systems-index.md` — Scene Management system placement in the 5-layer architecture
