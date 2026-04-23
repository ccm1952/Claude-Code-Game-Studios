// 该文件由Cursor 自动生成

# ADR-005: YooAsset Resource Loading & Lifecycle Pattern

## Status

Proposed

## Date

2026-04-22

## Last Verified

2026-04-22

## Decision Makers

Technical Director, Lead Programmer

## Summary

Every game system in 影子回忆 (Shadow Memory) loads assets — UI prefabs, audio clips, scene files, timelines, textures. Without a disciplined loading and lifecycle pattern, asset leaks and orphaned handles will accumulate across chapter transitions, eventually exhausting mobile memory budgets. This ADR establishes a single, async-only resource loading pattern built on TEngine's `GameModule.Resource` wrapper around YooAsset 2.3.17, with strict handle-ownership rules and mandatory scene-exit cleanup.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.62f2 (LTS) |
| **Asset System** | YooAsset 2.3.17 |
| **Domain** | Core / Asset Pipeline / Scene Management |
| **Knowledge Risk** | MEDIUM — YooAsset 2.3.17 specifics and TEngine's ResourceModule wrapper are likely absent from LLM training data; must verify APIs from project source |
| **References Consulted** | ADR-001 (TEngine Framework), project source (`TEngine/` directory), YooAsset GitHub repository, `docs/engine-reference/unity/VERSION.md` |
| **Post-Cutoff APIs Used** | `GameModule.Resource.LoadAssetAsync<T>()`, `GameModule.Resource.LoadSceneAsync()`, `GameModule.Resource.UnloadSceneAsync()`, YooAsset `AssetHandle`, `SceneHandle`, `ResourcePackage` |
| **Verification Required** | Sprint 0 spike: confirm `GameModule.Resource` wrapper API signatures, `AssetHandle.Release()` semantics, `SceneHandle` lifecycle, and `ResourcePackage` initialization flow from TEngine 6.0 + YooAsset 2.3.17 source |

> **Note**: If Knowledge Risk is MEDIUM or HIGH, this ADR must be re-validated if the
> project upgrades engine or YooAsset versions. Flag it as "Superseded" and write a new ADR.

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-001 (TEngine 6.0 Framework — `GameModule.Resource` wraps YooAsset) |
| **Enables** | ADR-009 (Scene Lifecycle — uses SceneHandle pattern defined here) |
| **Blocks** | All asset-dependent systems: UI System, Audio System, Narrative System, Shadow Puzzle System, Chapter/Scene transitions |
| **Ordering Note** | ADR-001 must reach Accepted status first. This ADR's validation criteria must pass during Sprint 0 before any asset-loading system can be implemented. |

## Context

### Problem Statement

《影子回忆 (Shadow Memory)》is a narrative puzzle game with chapter-based progression. Each chapter loads a distinct scene with unique UI, audio, shadow objects, timeline sequences, and environmental assets. The game must:

1. **Prevent resource leaks** — Every `LoadAssetAsync` must have a matching `Release()`. A forgotten release is a P0 bug because mobile devices have a hard memory ceiling (~1.5 GB). After 10 chapter transitions without proper cleanup, leaked assets would exhaust available memory.
2. **Support hot-update** — YooAsset's resource package system enables downloading updated assets without a full app store submission. This is critical for the mobile-first strategy (ADR-003) and for post-launch content patches.
3. **Handle scene loading/unloading** — Scene transitions are the highest-risk moment for resource leaks. The loading pattern must ensure SceneHandles are properly held during gameplay and released during transitions, followed by mandatory cleanup.
4. **Enforce clear handle ownership** — When multiple systems load assets concurrently (UI loading a prefab while Audio loads a clip), each system must own and track its own handles independently. Cross-system handle sharing is forbidden.
5. **Be async-only** — Synchronous loading stalls the main thread and causes frame drops on mobile. All loading must be async via UniTask integration (TR-concept-005).

### Current State

TEngine 6.0's `ResourceModule` already wraps YooAsset, exposing `GameModule.Resource` as the access point (established in ADR-001). YooAsset 2.3.17 is present in the project. However, no project-level conventions exist for:
- Handle ownership and tracking per system
- Mandatory cleanup sequences on scene exit
- Forbidden API surface (which Unity/YooAsset APIs are off-limits)
- ResourcePackage strategy (single vs. multiple packages)

### Constraints

- **Mobile memory ceiling**: 1.5 GB total; asset accumulation across chapter transitions must not approach this limit
- **Async-only mandate**: TR-concept-005 forbids all synchronous asset loading; frame budget is 16.67ms
- **TEngine wrapper requirement**: TR-concept-007 forbids bypassing `GameModule.XXX` accessors; direct YooAsset API calls are also forbidden
- **Hot-update capability**: Asset packages must support incremental download and patching via YooAsset's built-in CDN workflow
- **Single codebase**: Handle tracking pattern must work identically in Editor (simulate mode) and on device (real YooAsset packages)

### Key Concepts

| Concept | Description |
|---------|-------------|
| **ResourcePackage** | YooAsset's package abstraction — a collection of assets bundled together. TEngine initializes this during the Procedure boot chain. |
| **AssetHandle** | A reference-counted handle to a loaded asset. Must be held by the owning system until the asset is no longer needed. Calling `Release()` decrements the reference count; the asset is unloaded when count reaches zero. |
| **SceneHandle** | A specialized handle for loaded scenes. Must be held for the entire duration the scene is active. Released during scene transitions. |
| **GameModule.Resource** | TEngine's static accessor wrapping YooAsset's loading API. The sole entry point for all asset operations. |

## Decision

**Adopt a handle-ownership resource lifecycle pattern using TEngine's `GameModule.Resource` wrapper over YooAsset 2.3.17, with mandatory async loading, per-system handle tracking, and scene-exit cleanup.**

### Core Principles

1. **P3 — Async-First**: All asset loading is async. Zero synchronous load calls permitted.
2. **P4 — Resource Lifecycle Closure**: Every `LoadAssetAsync` must have a corresponding `Release()`. This invariant is enforced at the system level.
3. **Single Owner**: The system that calls `LoadAssetAsync` owns the returned handle and is solely responsible for calling `Release()`.
4. **Scene-Exit Cleanup**: After every scene unload, execute `Resources.UnloadUnusedAssets()` followed by `GC.Collect()`. This is mandatory, not optional.

### ResourcePackage Strategy

**Single ResourcePackage for MVP.** All game assets are bundled into one YooAsset package. This simplifies initialization, hot-update checks, and version management.

> **Open Question**: If asset volume grows significantly (e.g., chapter-specific DLC, localized audio packs), revisit for a multi-package strategy. The handle-ownership pattern defined here is package-agnostic and will not need revision.

### Handle Ownership Map

| System | Asset Types Owned | Lifecycle |
|--------|-------------------|-----------|
| **Scene Management** | SceneHandle | Load on chapter enter → Release on chapter exit |
| **UI System** | UI prefab handles | Load on `UIWindow.OnCreate()` → Release on `UIWindow.OnClose()` |
| **Audio System** | AudioClip handles | Load on first play or scene enter → Release on scene exit or explicit stop |
| **Narrative System** | Timeline / PlayableAsset / video handles | Load on narrative sequence start → Release on sequence end |
| **Shadow Puzzle System** | Shadow object prefab handles | Load on puzzle initialization → Release on puzzle cleanup |
| **Chapter Manager** | Chapter data / config handles | Load on chapter start → Release on chapter end |

### Loading Patterns

```csharp
// ═══════════════════════════════════════════════════════════════
// CORRECT: Async asset load with handle tracking
// ═══════════════════════════════════════════════════════════════
private AssetHandle _hudHandle;

public async UniTask LoadHUD()
{
    _hudHandle = await GameModule.Resource.LoadAssetAsync<GameObject>("UI/HUDPanel");
    var instance = Object.Instantiate(_hudHandle.AssetObject as GameObject);
    // ... bind and use instance ...
}

public void UnloadHUD()
{
    if (_instance != null) Object.Destroy(_instance);
    if (_hudHandle != null)
    {
        _hudHandle.Release();
        _hudHandle = null;
    }
}

// ═══════════════════════════════════════════════════════════════
// CORRECT: Scene loading with SceneHandle
// ═══════════════════════════════════════════════════════════════
private SceneHandle _currentSceneHandle;

public async UniTask LoadChapterScene(string sceneName)
{
    _currentSceneHandle = await GameModule.Resource.LoadSceneAsync(
        sceneName, LoadSceneMode.Additive);
}

public async UniTask UnloadChapterScene()
{
    if (_currentSceneHandle != null)
    {
        await GameModule.Resource.UnloadSceneAsync(_currentSceneHandle);
        _currentSceneHandle = null;
    }
    await Resources.UnloadUnusedAssets();
    GC.Collect();
}

// ═══════════════════════════════════════════════════════════════
// CORRECT: Multiple asset handles tracked in a list
// ═══════════════════════════════════════════════════════════════
private readonly List<AssetHandle> _loadedHandles = new();

public async UniTask<AudioClip> LoadClip(string path)
{
    var handle = await GameModule.Resource.LoadAssetAsync<AudioClip>(path);
    _loadedHandles.Add(handle);
    return handle.AssetObject as AudioClip;
}

public void ReleaseAllClips()
{
    foreach (var handle in _loadedHandles)
    {
        handle.Release();
    }
    _loadedHandles.Clear();
}
```

### Forbidden Patterns

```csharp
// ╳ FORBIDDEN: Synchronous loading (violates P3: Async-First)
var obj = Resources.Load<GameObject>("SomeAsset");

// ╳ FORBIDDEN: Direct AssetBundle access (bypasses YooAsset lifecycle)
var bundle = AssetBundle.LoadFromFile("path/to/bundle");
var asset = bundle.LoadAsset<GameObject>("SomeAsset");

// ╳ FORBIDDEN: Direct YooAsset API (bypasses TEngine wrapper)
var package = YooAssets.GetPackage("DefaultPackage");
var handle = package.LoadAssetAsync<GameObject>("SomeAsset");

// ╳ FORBIDDEN: Fire-and-forget loading without handle tracking
async void LoadAndForget()
{
    var handle = await GameModule.Resource.LoadAssetAsync<GameObject>("Asset");
    var instance = Object.Instantiate(handle.AssetObject as GameObject);
    // handle is lost — RESOURCE LEAK
}

// ╳ FORBIDDEN: Cross-system handle sharing
// System A loads an asset and passes the handle to System B.
// Only the loading system may release the handle.
```

### Scene Transition Cleanup Sequence

Every scene transition must follow this exact sequence:

```
1. Notify all systems of impending scene exit (via GameEvent)
2. Each system releases its owned AssetHandles
3. Scene Management releases SceneHandle via UnloadSceneAsync()
4. await Resources.UnloadUnusedAssets()
5. GC.Collect()
6. Load next scene via LoadSceneAsync()
```

This sequence is mandatory (TR-scene-016, TR-scene-017). Skipping steps 4-5 will cause asset accumulation across chapter transitions.

## Alternatives Considered

### Alternative 1: TEngine ResourceModule wrapping YooAsset (Chosen)

- **Description**: Use `GameModule.Resource` as the sole asset loading interface, with YooAsset handling the underlying bundle management, hot-update, and caching.
- **Pros**: Consistent with ADR-001's `GameModule.XXX` pattern; integrated with TEngine's Procedure boot chain; YooAsset handles hot-update complexity transparently; single API surface for all asset types.
- **Cons**: Abstraction layer adds indirection; some YooAsset-specific features (e.g., advanced package queries) may require wrapper extensions.
- **Selection Reason**: Aligns with the project's established framework pattern and provides hot-update capability with minimal additional effort.

### Alternative 2: Direct YooAsset API

- **Description**: Bypass TEngine's `ResourceModule` and call YooAsset's `YooAssets.GetPackage()` / `package.LoadAssetAsync()` directly throughout game code.
- **Pros**: Full access to YooAsset's feature set; no wrapper overhead; can use advanced features like package-level queries and custom decryption.
- **Cons**: Bypasses TEngine's module pattern (violates ADR-001); creates two different access patterns in the codebase; initialization must be manually synchronized with TEngine's boot flow.
- **Rejection Reason**: Inconsistent with ADR-001's mandate that all services are accessed via `GameModule.XXX`. Two access patterns in one codebase increases cognitive load and bug risk.

### Alternative 3: Unity Addressables

- **Description**: Replace YooAsset with Unity's built-in Addressable Asset System for async loading, remote content delivery, and reference counting.
- **Pros**: First-party Unity support; extensive documentation; large community; built-in profiling tools; no third-party dependency risk.
- **Cons**: Would require removing YooAsset from the project entirely; TEngine's `ResourceModule` is designed around YooAsset, not Addressables; HybridCLR + Addressables integration is less battle-tested in the Chinese mobile ecosystem; migration cost is prohibitive at this stage.
- **Rejection Reason**: The project is already committed to YooAsset 2.3.17 via TEngine integration. Switching to Addressables would require rewriting TEngine's `ResourceModule`, all GDD asset loading references, and the hot-update pipeline. No net benefit justifies this cost.

### Alternative 4: Raw AssetBundles

- **Description**: Manage AssetBundle loading, unloading, dependency tracking, and caching manually without YooAsset or Addressables.
- **Pros**: Maximum control over bundle layout, loading order, and memory; zero third-party overhead.
- **Cons**: Enormous implementation burden — dependency resolution, variant management, caching, versioning, download management, and error recovery must all be built from scratch; high bug risk; no hot-update management out of the box.
- **Rejection Reason**: Reinventing what YooAsset already provides. The effort-to-benefit ratio is extremely unfavorable for an indie team.

## Consequences

### Positive

- **Consistent async loading**: All asset operations go through a single API (`GameModule.Resource`), making code reviews and static analysis straightforward
- **Handle-based lifecycle prevents leaks**: Explicit handle ownership makes it clear who is responsible for releasing each asset; forgotten releases are detectable via profiling
- **Hot-update capable**: YooAsset's ResourcePackage supports incremental asset updates via CDN without app store resubmission — critical for mobile-first strategy (ADR-003)
- **Scene-exit cleanup prevents accumulation**: Mandatory `UnloadUnusedAssets()` + `GC.Collect()` after every scene unload ensures a clean memory state for the next chapter
- **Testable invariant**: "Every LoadAssetAsync has a matching Release" is a verifiable property that can be checked via automated profiling and custom editor tooling

### Negative

- **Manual handle tracking burden**: Every system must maintain its own handle list/references; this is boilerplate that adds code volume and maintenance cost
- **Release() forgetting is a P0 bug**: A single forgotten `Release()` call creates a silent resource leak that may only manifest after multiple chapter transitions; this failure mode is insidious because it doesn't cause an immediate crash
- **YooAsset version lock**: The project is locked to YooAsset 2.3.17; upgrading requires re-verifying all wrapper APIs and potentially updating TEngine's `ResourceModule`
- **Single-package limitation**: The MVP single-package strategy may become a bottleneck if the game's asset volume grows to require chapter-specific or locale-specific packages
- **Abstraction cost**: TEngine's wrapper hides some YooAsset capabilities; if advanced features are needed later, the wrapper must be extended

### Neutral

- The handle-ownership map formalizes what would be informal conventions anyway — it adds documentation overhead but prevents ambiguity
- Scene-exit cleanup adds ~200-500ms to each transition; this is acceptable for a narrative puzzle game where transitions include fade effects

## Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| `GameModule.Resource` wrapper API differs from assumed signatures | HIGH | MEDIUM | Sprint 0 spike: verify `LoadAssetAsync<T>()`, `LoadSceneAsync()`, `UnloadSceneAsync()`, and `AssetHandle.Release()` against TEngine 6.0 source |
| Forgotten `Release()` calls cause silent memory leaks | HIGH | HIGH | Custom Editor tool that tracks outstanding handles per system; Memory Profiler checks after each chapter transition during QA |
| YooAsset 2.3.17 behavior differs from documentation / LLM assumptions | MEDIUM | MEDIUM | Sprint 0 spike: verify AssetHandle reference counting, SceneHandle lifecycle, and ResourcePackage initialization against YooAsset source |
| Single ResourcePackage becomes too large for practical hot-update | LOW | MEDIUM | Monitor package size; if it exceeds 500 MB, split into chapter-specific packages (the handle-ownership pattern is package-agnostic) |
| `Resources.UnloadUnusedAssets()` takes too long on mobile | LOW | MEDIUM | Profile on target devices; if > 500ms, investigate incremental unloading or background thread offloading |
| Scene-exit cleanup sequence is not followed by a system | MEDIUM | HIGH | Enforce via base class / interface that mandates cleanup; scene transition manager validates all systems report "cleaned up" before proceeding |

## Performance Implications

| Metric | Expected Impact | Budget | Notes |
|--------|----------------|--------|-------|
| **CPU (loading)** | Non-blocking; async loads do not stall main thread | 16.67ms frame budget | Load operations execute over multiple frames via UniTask |
| **CPU (cleanup)** | ~200-500ms per scene transition for `UnloadUnusedAssets()` + `GC.Collect()` | Acceptable during fade transition | Hidden behind scene transition visual effects |
| **Memory (handles)** | Negligible overhead (~64 bytes per handle reference) | 1,500 MB mobile ceiling | Handle tracking list memory is trivial compared to asset memory |
| **Memory (assets)** | Per-chapter asset footprint depends on content; cleanup ensures no cross-chapter accumulation | Target: < 400 MB active assets per chapter | Profiled per chapter during QA |
| **Network (hot-update)** | Download size depends on changed assets; YooAsset handles differential downloads | Target: < 50 MB per patch | YooAsset's built-in diffing minimizes download size |
| **Load Time** | Scene load time depends on asset count and size | Target: < 3s per chapter load | Can be hidden behind loading screen / narrative transition |

## Implementation Guidelines

1. **Base class for asset-owning systems**: Create a `AssetOwnerBase` (or equivalent interface) that provides `RegisterHandle()` and `ReleaseAllHandles()` methods. All systems that load assets should inherit from or implement this pattern.

2. **Handle null-check before Release**: Always null-check handles before calling `Release()`. A released handle should be set to `null` immediately to prevent double-release.

3. **Scene transition manager enforcement**: The scene transition manager (ADR-009) must:
   - Broadcast a "scene exiting" event via `GameEvent`
   - Wait for all systems to confirm cleanup completion
   - Execute `UnloadUnusedAssets()` + `GC.Collect()`
   - Only then proceed to load the next scene

4. **Editor tooling**: Build a custom Editor window that displays:
   - Currently outstanding AssetHandles per system
   - Handle creation time and asset path
   - Warning for handles older than the current scene lifetime

5. **Static analysis rule**: Add a code review checklist item and (if feasible) a Roslyn analyzer rule: "Every `LoadAssetAsync` call site must have a corresponding `Release()` in the same class."

## Validation Criteria

- [ ] Sprint 0 spike: Confirm `GameModule.Resource.LoadAssetAsync<T>()` returns a handle with `.AssetObject` and `.Release()` as expected
- [ ] Sprint 0 spike: Confirm `GameModule.Resource.LoadSceneAsync()` returns a SceneHandle that can be passed to `UnloadSceneAsync()`
- [ ] Sprint 0 spike: Confirm `AssetHandle.Release()` decrements reference count and asset is unloaded when count reaches zero
- [ ] Sprint 0 spike: Confirm single ResourcePackage initializes correctly during TEngine Procedure boot chain
- [ ] Zero resource leaks after 10 consecutive chapter transitions (verified via Unity Memory Profiler)
- [ ] Zero synchronous `Resources.Load<T>()` or `AssetBundle.LoadAsset()` calls in entire codebase (verified via automated grep / Roslyn analyzer)
- [ ] SceneHandle properly held during scene lifetime and released during transitions (verified via Editor tooling)
- [ ] Hot-update asset download + load works on real mobile device (iOS and Android)
- [ ] Scene-exit cleanup (`UnloadUnusedAssets()` + `GC.Collect()`) completes within 500ms on target mobile device

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| `design/gdd/game-concept.md` | Core | TR-concept-005: Forbidden sync asset loading | All loading is async via `GameModule.Resource.LoadAssetAsync<T>()`; `Resources.Load<T>()` and `AssetBundle.LoadAsset()` are explicitly forbidden |
| `design/gdd/game-concept.md` | Core | TR-concept-010: Resource lifecycle closure | Handle-ownership pattern mandates every `LoadAssetAsync` has a corresponding `Release()`; enforced via base class, code review, and Editor tooling |
| `design/gdd/scene-management.md` | Scene | TR-scene-003: Async scene loading via UniTask | `GameModule.Resource.LoadSceneAsync()` returns a SceneHandle via UniTask-based async flow |
| `design/gdd/scene-management.md` | Scene | TR-scene-016: UnloadUnusedAssets after scene unload | Mandatory step 4 in Scene Transition Cleanup Sequence |
| `design/gdd/scene-management.md` | Scene | TR-scene-017: GC.Collect after scene unload | Mandatory step 5 in Scene Transition Cleanup Sequence |

## Related

- **Depends On**: ADR-001 (TEngine 6.0 Framework) — `GameModule.Resource` is the TEngine module wrapper this ADR builds upon
- **Enables**: ADR-009 (Scene Lifecycle) — Scene loading/unloading patterns and SceneHandle management defined here are consumed by the scene lifecycle architecture
- **Cross-Reference**: ADR-003 (Mobile-First Platform) — Hot-update capability via YooAsset is a key enabler for the mobile distribution strategy
- **References**: `.claude/docs/technical-preferences.md` — Forbidden sync loading and required cleanup patterns align with this ADR
- **References**: `src/MyGame/ShadowGame/design/gdd/systems-index.md` — TEngine Integration Map documents per-system asset loading responsibilities
