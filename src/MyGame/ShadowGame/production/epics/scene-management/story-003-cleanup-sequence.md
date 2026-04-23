// 该文件由Cursor 自动生成

# Story 003: Mandatory Cleanup Sequence

> **Epic**: Scene Management
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/scene-management.md`
**Requirement**: `TR-scene-016`, `TR-scene-017`, `TR-scene-011`
*(UnloadUnusedAssets + GC.Collect between transitions; SceneHandle reference retention; memory leak detection)*

**ADR Governing Implementation**: ADR-009: Scene Lifecycle + ADR-005: YooAsset Resource Lifecycle
**ADR Decision Summary**: A mandatory 4-step cleanup sequence (notify → release handles → unload scene → GC) must run between every chapter unload and next chapter load. No step may be skipped. This is the mechanism that prevents asset accumulation on mobile devices.

**Engine**: Unity 2022.3.62f2 LTS + YooAsset 2.3.17 + UniTask 2.5.10 | **Risk**: MEDIUM
**Engine Notes**: `Resources.UnloadUnusedAssets()` returns an `AsyncOperation`; await via `.ToUniTask()`. `GC.Collect()` is synchronous. YooAsset shared-asset handles (UI prefabs, SFX) held by their own systems are NOT released by scene unload — only the SceneHandle is released here. Verified by SP-003: shared asset handles through independent LoadAssetAsync calls are unaffected by scene unload.

**Control Manifest Rules (this layer)**:
- Required: `Scene Transition Cleanup Sequence (mandatory, in order): (1) notify systems via Evt_SceneUnloadBegin, (2) each system releases owned AssetHandles, (3) Scene Manager releases SceneHandle via UnloadSceneAsync(), (4) Resources.UnloadUnusedAssets(), (5) GC.Collect(), (6) load next scene` (ADR-005)
- Required: `Never skip UnloadUnusedAssets() + GC.Collect() between scene transitions` (ADR-005, ADR-009)
- Required: `Null-check handles before Release(); set to null immediately after release` (ADR-005)
- Required: `Scene-scoped listeners must be removed on Evt_SceneUnloadBegin` (ADR-006)
- Forbidden: `Never forget to call UnloadSceneAsync after LoadSceneAsync` (resource leak) (ADR-005)

---

## Acceptance Criteria

*From GDD `design/gdd/scene-management.md`, scoped to this story:*

- [ ] `Evt_SceneUnloadBegin` is fired (Step 5) with `{ int chapterId }` payload before any unload operation begins
- [ ] Scene Manager awaits a one-frame propagation window after `Evt_SceneUnloadBegin` to allow systems to release their handles
- [ ] `GameModule.Resource.UnloadSceneAsync(_currentSceneHandle)` is called (Step 6); `_currentSceneHandle` is set to `null` immediately after
- [ ] `Resources.UnloadUnusedAssets()` is awaited (Step 7)
- [ ] `GC.Collect()` is called synchronously immediately after `UnloadUnusedAssets` completes
- [ ] Steps execute in strict order: SceneUnloadBegin → UnloadSceneAsync → UnloadUnusedAssets → GC.Collect — never rearranged
- [ ] First-boot path (no previous chapter): Steps 5–7 are skipped entirely; jumps directly to Step 8 (download check)
- [ ] After 5 consecutive chapter transitions (Chapter 1→2→3→4→5→1), memory baseline returns within 5% of the pre-test baseline (memory leak detection, TR-scene-011)
- [ ] Commonly-loaded shared assets (UI prefabs, SFX) held by independent handles are NOT released by scene unload — they remain accessible after transition

---

## Implementation Notes

*Derived from ADR-009 + ADR-005 Implementation Guidelines:*

The cleanup sequence executes in the `Unloading` FSM state, between `TransitionOut` completing and `Loading` beginning:

```csharp
// Step 5 — notify all systems (broadcast, not a call chain)
GameEvent.Send(EventId.Evt_SceneUnloadBegin, new SceneUnloadBeginPayload
{
    ChapterId = _currentChapterId
});
await UniTask.Yield(); // one frame for handlers to release their handles

// Step 6 — unload chapter scene
if (_currentSceneHandle != null) {
    await GameModule.Resource.UnloadSceneAsync(_currentSceneHandle);
    _currentSceneHandle = null;
}

// Step 7 — mandatory cleanup (never optional, never skipped)
await Resources.UnloadUnusedAssets().ToUniTask();
GC.Collect();
```

**First-boot guard**:
```csharp
if (_currentChapterId == -1) {
    // No previous scene — skip unload/cleanup
    goto LoadingPhase;
}
```

**Why GC.Collect() is synchronous here**: This runs behind the fade overlay. The player sees a black screen. The ~50ms GC pause is invisible and acceptable. Do NOT move GC to a background thread.

**SP-003 shared asset note**: UI prefabs loaded via `GameModule.Resource.LoadAssetAsync()` by UIModule retain their own handles. `UnloadUnusedAssets()` will not release them because their reference count > 0. This is correct and expected. The Scene Manager only releases the `SceneHandle` — each system releases its own handles.

**Memory leak test**: Run 5 consecutive chapter transitions in an automated integration test; snapshot memory before and after each cycle using `UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()`; assert that the final baseline is within 5% of the initial baseline.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: State machine FSM state transitions (Unloading state is declared there)
- Story 002: LoadSceneAsync and SetActiveScene (happens after this cleanup)
- Story 005: `Evt_SceneTransitionBegin` and `Evt_SceneTransitionEnd` (other lifecycle events)

---

## QA Test Cases

- **AC-1**: Cleanup sequence order is deterministic
  - Given: Chapter 1 is loaded; transition to Chapter 2 triggered
  - When: SceneManager enters Unloading state
  - Then: Events/operations fire in order: `Evt_SceneUnloadBegin` → `UnloadSceneAsync` → `UnloadUnusedAssets` → `GC.Collect`; each operation completes before the next begins
  - Edge cases: any exception in `UnloadSceneAsync` must still trigger `UnloadUnusedAssets` + `GC.Collect` (cleanup must not be skippable on error)

- **AC-2**: `_currentSceneHandle` is null after unload
  - Given: `_currentSceneHandle` is a valid SceneHandle for Chapter 1
  - When: `UnloadSceneAsync` completes
  - Then: `_currentSceneHandle == null`
  - Edge cases: calling `UnloadSceneAsync` with a null handle must be guarded (no NullReferenceException)

- **AC-3**: First-boot skip (no previous chapter)
  - Given: `_currentChapterId == -1`; `_currentSceneHandle == null`
  - When: Transition triggered (new game)
  - Then: `Evt_SceneUnloadBegin` is NOT fired; `UnloadSceneAsync` is NOT called; scene transitions directly to Loading state
  - Edge cases: ChapterId -1 must never match a valid chapter config entry

- **AC-4**: Shared asset handles survive scene transition
  - Given: UIModule holds a handle for a UI prefab loaded via LoadAssetAsync
  - When: Chapter scene is unloaded and cleanup runs
  - Then: The UI prefab handle remains valid; `GameModule.Resource.LoadAssetAsync` not needed again for the same prefab
  - Edge cases: if UIModule explicitly releases its handle during `Evt_SceneUnloadBegin`, the asset is correctly cleaned up

- **AC-5**: 5-cycle memory leak test
  - Given: Game starts from fresh; initial memory baseline recorded
  - When: 5 consecutive chapter transitions run (1→2→3→4→5→1)
  - Then: Memory after each full cycle returns within 5% of the initial baseline
  - Edge cases: GC.Collect() must be called after each UnloadUnusedAssets(); test fails if either is absent

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `tests/integration/scene-management/cleanup_sequence_test.cs` — must exist and pass (includes 5-cycle memory leak test)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (state machine — must be DONE), Story 002 (SceneHandle lifecycle — must be DONE)
- Unlocks: Story 005 (full 8-event integration — cleanup events are part of the full event flow)
