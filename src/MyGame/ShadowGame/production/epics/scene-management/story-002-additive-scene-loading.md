// 该文件由Cursor 自动生成

# Story 002: Additive Scene Loading via YooAsset

> **Epic**: Scene Management
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/scene-management.md`
**Requirement**: `TR-scene-001`, `TR-scene-003`, `TR-scene-004`, `TR-scene-007`
*(Additive scene architecture; async loading via UniTask; always LoadSceneMode.Additive; YooAsset on-demand download)*

**ADR Governing Implementation**: ADR-009: Scene Lifecycle + ADR-005: YooAsset Resource Lifecycle
**ADR Decision Summary**: All scenes loaded via `GameModule.Resource.LoadSceneAsync()` in `LoadSceneMode.Additive`. Scene Manager is sole owner of the chapter `SceneHandle`. After load, `SceneManager.SetActiveScene()` is called on the new scene. Previous chapter must be fully unloaded before new chapter loads.

**Engine**: Unity 2022.3.62f2 LTS + YooAsset 2.3.17 + UniTask 2.5.10 | **Risk**: MEDIUM
**Engine Notes**: `GameModule.Resource.LoadSceneAsync(sceneName, LoadSceneMode.Additive)` returns a `SceneHandle` (YooAsset). `GameModule.Resource.UnloadSceneAsync(SceneHandle)` releases it. `SceneManager.SetActiveScene()` requires the scene to be fully loaded before calling. Single package strategy (SP-003): `"DefaultPackage"` — no multi-package initialization needed. Verify `SetActiveScene` works correctly on additively loaded YooAsset scenes on real devices.

**Control Manifest Rules (this layer)**:
- Required: `Additive-only scene loading — LoadSceneMode.Additive exclusively` (ADR-009)
- Required: `After loading chapter scene, call SceneManager.SetActiveScene() on the new scene` (ADR-009)
- Required: `Handle-ownership: the system that calls LoadAssetAsync owns the handle` (ADR-005)
- Required: `Null-check handles before Release(); set to null immediately after release` (ADR-005)
- Required: `Never use direct SceneManager.LoadSceneAsync() — use GameModule.Resource.LoadSceneAsync()` (ADR-009)
- Required: `All async operations via UniTask` (ADR-001)
- Forbidden: `Never use LoadSceneMode.Single` (ADR-009)
- Forbidden: `Never use synchronous Resources.Load or AssetBundle.LoadAsset` (ADR-005)
- Forbidden: `Never fire-and-forget a load (losing the handle)` (ADR-005)
- Forbidden: `Never use DontDestroyOnLoad in chapter scene objects` (ADR-009)

---

## Acceptance Criteria

*From GDD `design/gdd/scene-management.md`, scoped to this story:*

- [ ] Scene is loaded via `GameModule.Resource.LoadSceneAsync(sceneName, LoadSceneMode.Additive)` — never `LoadSceneMode.Single`
- [ ] `SceneHandle` is stored in `SceneManager._currentSceneHandle`; no other system holds a reference to it
- [ ] After successful load, `SceneManager.SetActiveScene(newScene)` is called so the chapter's lighting and skybox become active
- [ ] YooAsset resource package status is checked before load (Step 8 of 11-step flow); if assets not downloaded, download runs first with `Evt_SceneDownloadProgress` events fired
- [ ] Load progress is reported via `Evt_SceneLoadProgress` events with `{ string sceneName, float progress }`
- [ ] On successful load, `Evt_SceneLoadComplete` fires with `{ int chapterId, string bgmAsset }` payload
- [ ] On load failure: `Evt_SceneLoadFailed` fires; retry up to `MAX_LOAD_RETRY = 2` times; after all retries exhausted, show error UI
- [ ] Previous chapter scene must be unloaded (Story 003 cleanup complete) before this story's load begins
- [ ] `_currentSceneHandle` is set to `null` after `UnloadSceneAsync()` completes (release guard)
- [ ] Max 3 scenes in memory at any time (BootScene + MainScene + 1 chapter); validated via `SceneManager.sceneCount` assertion in debug builds

---

## Implementation Notes

*Derived from ADR-009 + ADR-005 Implementation Guidelines:*

Execute steps 8–10 of the 11-step flow inside the `Loading` FSM state:

```csharp
// Step 8 — resource package check (SP-003: single DefaultPackage)
var pkg = GameModule.Resource.GetResourcePackage("DefaultPackage");
if (!pkg.CheckLocationValid(sceneName)) {
    var downloader = pkg.CreateResourceDownloader(sceneName, ...);
    // fire Evt_SceneDownloadProgress during download
    await downloader.BeginDownload().ToUniTask();
}

// Step 9 — async scene load
var handle = await GameModule.Resource.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
_currentSceneHandle = handle;
// fire Evt_SceneLoadProgress events during load

// Step 10 — activate
UnityEngine.SceneManagement.SceneManager.SetActiveScene(handle.SceneObject);
GameEvent.Send(EventId.Evt_SceneLoadComplete, new SceneLoadCompletePayload
{
    ChapterId = targetChapterId,
    BgmAsset = chapterConfig.BgmAsset
});
```

**Handle ownership rule (ADR-005)**: `_currentSceneHandle` is private to `SceneManager`. It is the only place `LoadSceneAsync` is called for chapters. Release in Story 003's cleanup step:
```csharp
await GameModule.Resource.UnloadSceneAsync(_currentSceneHandle);
_currentSceneHandle = null; // immediately after release
```

**First-boot special case**: If `_currentChapterId == -1` (no previous scene), skip unload/cleanup steps (Steps 5–7); jump directly to Step 8.

**Error handling**:
```csharp
for (int attempt = 0; attempt < MAX_LOAD_RETRY; attempt++) {
    try { /* load */ break; }
    catch { if (attempt == MAX_LOAD_RETRY - 1) FireLoadFailed(); }
}
```

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: State machine skeleton and mutex — must be DONE first
- Story 003: Cleanup sequence (notify → release → unload → GC.Collect) — runs before this story's load
- Story 005: 8 lifecycle events other than `Evt_SceneLoadProgress`, `Evt_SceneDownloadProgress`, `Evt_SceneLoadComplete`, `Evt_SceneLoadFailed`
- Story 006: Resolving `sceneName` from `TbChapter.sceneId` (Luban mapping)

---

## QA Test Cases

- **AC-1**: Scene loads additively (never Single)
  - Given: Scene Manager in Loading state; target chapter = 1
  - When: LoadSceneAsync is called
  - Then: `SceneManager.sceneCount` increases by 1; `LoadSceneMode.Single` is never used (verified by grep test)
  - Edge cases: verifying no `LoadSceneMode.Single` string exists in codebase

- **AC-2**: SceneHandle owned exclusively by SceneManager
  - Given: Chapter 1 scene loaded successfully
  - When: `_currentSceneHandle` is inspected
  - Then: Non-null, valid, and held only by SceneManager
  - Edge cases: after unload, `_currentSceneHandle == null`

- **AC-3**: SetActiveScene called after load
  - Given: Chapter 2 loaded additively
  - When: Load completes
  - Then: `UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Chapter_02_SharedSpace"` (or equivalent)
  - Edge cases: if SetActiveScene throws (scene not fully loaded), transition to Error state

- **AC-4**: Load failure → retry → Evt_SceneLoadFailed after max retries
  - Given: Simulated load failure every attempt
  - When: LoadSceneAsync is called
  - Then: Retries exactly `MAX_LOAD_RETRY` times, then fires `Evt_SceneLoadFailed` with chapterId and error message
  - Edge cases: retry count resets on next legitimate load request

- **AC-5**: Download runs before load when package not cached
  - Given: Resource package not downloaded (simulated)
  - When: Transition to chapter triggered
  - Then: `Evt_SceneDownloadProgress` events fire with increasing progress; `Evt_SceneLoadProgress` events fire after download; no load attempt before download completes
  - Edge cases: download timeout after 30s → Error state

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `tests/integration/scene-management/additive_scene_loading_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (scene state machine — must be DONE)
- Unlocks: Story 003 (cleanup sequence uses `_currentSceneHandle`), Story 005 (events use loading steps)
