// 该文件由Cursor 自动生成

# Story 005: 8 Scene Lifecycle Events in Deterministic Order

> **Epic**: Scene Management
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/scene-management.md`
**Requirement**: `TR-scene-014`
*(8 scene transition GameEvent IDs; deterministic order; all listeners notified)*

**ADR Governing Implementation**: ADR-009: Scene Lifecycle + ADR-006: GameEvent Protocol
**ADR Decision Summary**: 8 named scene lifecycle events with IDs 1400–1407 fire in deterministic order during every transition. Events use struct payloads. Scene-scoped listeners must self-remove on `Evt_SceneUnloadBegin`. Events are the sole inter-module communication mechanism for scene transitions — no direct callbacks.

**Engine**: Unity 2022.3.62f2 LTS + TEngine 6.0.0 | **Risk**: MEDIUM
**Engine Notes**: `GameEvent.Send<T>(int eventId, T payload)` dispatches synchronously to all registered listeners. Each dispatch ≤ 0.05ms (ADR-006 budget). Total for 8 events per transition: < 0.4ms. Listeners registered via `GameEvent.AddListener` in their respective system's `Init()`; removed in `Dispose()` or on `Evt_SceneUnloadBegin`.

**Control Manifest Rules (this layer)**:
- Required: `All event IDs defined as public const int in centralized EventId static class` (ADR-006)
- Required: `Event ID allocation: Scene 1400-1499` (ADR-006)
- Required: `Every EventId constant must have XML doc comment with Sender(s), Listener(s), Payload type, and Cascade info` (ADR-006)
- Required: `Scene-scoped listeners must be removed on Evt_SceneUnloadBegin` (ADR-006)
- Required: `Cross-event cascade depth must not exceed 3` (ADR-006)
- Required: `Use struct for frequent event payloads to avoid GC allocation` (ADR-006)
- Forbidden: `Never define event IDs outside EventId.cs` (ADR-006)
- Forbidden: `Never use anonymous object parameters or raw int encoding for payloads` (ADR-006)
- Guardrail: `Event dispatch per Send ≤ 0.05ms` (ADR-006)

---

## Acceptance Criteria

*From GDD `design/gdd/scene-management.md`, scoped to this story:*

- [ ] `EventId.cs` defines 8 scene event constants (1400–1407) with XML doc comments: `Evt_SceneTransitionBegin`, `Evt_SceneUnloadBegin`, `Evt_SceneLoadProgress`, `Evt_SceneDownloadProgress`, `Evt_SceneLoadComplete`, `Evt_SceneReady`, `Evt_SceneTransitionEnd`, `Evt_SceneLoadFailed`
- [ ] Named payload structs defined for each event: `SceneTransitionBeginPayload`, `SceneUnloadBeginPayload`, `SceneLoadProgressPayload`, `SceneDownloadProgressPayload`, `SceneLoadCompletePayload`, `SceneReadyPayload`, `SceneTransitionEndPayload`, `SceneLoadFailedPayload`
- [ ] On successful transition, events fire in this exact order: 1400 → 1401 → [1403] → 1402 → 1404 → 1405 → 1406 (1403 only if download needed)
- [ ] On load failure, events fire: 1400 → 1401 → 1407 (replaces 1404→1405→1406 tail)
- [ ] `Evt_SceneTransitionBegin` (1400) carries `{ int fromChapterId, int toChapterId }`
- [ ] `Evt_SceneUnloadBegin` (1401) carries `{ int chapterId }` (current chapter being unloaded)
- [ ] `Evt_SceneLoadProgress` (1402) carries `{ string sceneName, float progress }` (0–1)
- [ ] `Evt_SceneDownloadProgress` (1403) carries `{ float progress, long downloadedBytes, long totalBytes }`
- [ ] `Evt_SceneLoadComplete` (1404) carries `{ int chapterId, string bgmAsset }`
- [ ] `Evt_SceneReady` (1405) carries `{ int chapterId }`
- [ ] `Evt_SceneTransitionEnd` (1406) carries `{ int chapterId }`
- [ ] `Evt_SceneLoadFailed` (1407) carries `{ int chapterId, string error }`
- [ ] Total event dispatch time for all 8 events < 0.4ms (performance guardrail)
- [ ] Listener registered for `Evt_SceneUnloadBegin` is removed during handling of `Evt_SceneUnloadBegin` (self-cleanup test)

---

## Implementation Notes

*Derived from ADR-009 + ADR-006 Implementation Guidelines:*

**EventId.cs additions** (in the 1400–1499 block):
```csharp
/// <summary>
/// Scene transition starts. Sender: SceneManager. Listeners: UITransitionOverlay, AudioSystem, InputService.
/// Payload: SceneTransitionBeginPayload. Cascade: fires before any async work begins.
/// </summary>
public const int Evt_SceneTransitionBegin = 1400;

/// <summary>
/// Chapter scene about to be unloaded. Sender: SceneManager. Listeners: ALL scene-scoped systems.
/// Payload: SceneUnloadBeginPayload. Cascade: systems must release AssetHandles and remove scene-scoped listeners.
/// </summary>
public const int Evt_SceneUnloadBegin = 1401;

// ... (continue for 1402–1407)
```

**Payload structs** (value types for GC-free dispatch):
```csharp
public struct SceneTransitionBeginPayload
{
    public int FromChapterId;
    public int ToChapterId;
}
// ... one struct per event
```

**Transition sequence fire points** (map to 11-step flow from ADR-009):
- Step 3: Fire `Evt_SceneTransitionBegin`
- Step 5: Fire `Evt_SceneUnloadBegin`
- Step 8 (download): Fire `Evt_SceneDownloadProgress` repeatedly during download
- Step 9 (load): Fire `Evt_SceneLoadProgress` repeatedly during load
- Step 10: Fire `Evt_SceneLoadComplete`
- Step 11 (before fade-in): Fire `Evt_SceneReady`
- Step 11 (after fade-in): Fire `Evt_SceneTransitionEnd`
- Error path: Fire `Evt_SceneLoadFailed` replacing tail

**Self-removal pattern** (for scene-scoped listeners):
```csharp
// In ShadowPuzzleManager.Init():
GameEvent.AddListener(EventId.Evt_SceneUnloadBegin, OnSceneUnloadBegin);

private void OnSceneUnloadBegin(int eventId, SceneUnloadBeginPayload payload) {
    // Release scene-specific resources
    GameEvent.RemoveListener(EventId.Evt_SceneUnloadBegin, OnSceneUnloadBegin);
}
```

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: Actual async load operations that trigger progress events
- Story 003: Cleanup operations triggered by `Evt_SceneUnloadBegin`
- Story 004: Mutex logic (transition guard)

---

## QA Test Cases

- **AC-1**: Events fire in deterministic order (success path)
  - Given: SceneManager transitions from chapter 1 to chapter 2 (assets cached, no download)
  - When: Full transition completes
  - Then: Event log records in order: 1400, 1401, 1402 (with progress updates), 1404, 1405, 1406 — no out-of-order occurrences
  - Edge cases: if download is needed, 1403 appears between 1401 and 1402

- **AC-2**: Events fire in deterministic order (failure path)
  - Given: SceneManager transitions but load fails
  - When: Max retries exhausted
  - Then: Event log records: 1400, 1401, 1407 — no 1404/1405/1406 after 1407
  - Edge cases: 1401 must still fire even on failure path (systems must clean up)

- **AC-3**: Payload fields are correctly populated
  - Given: Transitioning from chapter 2 to chapter 3
  - When: `Evt_SceneTransitionBegin` (1400) fires
  - Then: Payload has `FromChapterId == 2` and `ToChapterId == 3`
  - When: `Evt_SceneLoadComplete` (1404) fires
  - Then: Payload has `ChapterId == 3` and non-empty `BgmAsset` string (from TbChapter config)
  - Edge cases: first-boot transition has `FromChapterId == -1`

- **AC-4**: Scene-scoped listener self-removes on Evt_SceneUnloadBegin
  - Given: A test system registers a scene-scoped listener for `Evt_SceneUnloadBegin`
  - When: `Evt_SceneUnloadBegin` fires
  - Then: Listener is invoked once; when `Evt_SceneUnloadBegin` fires again (next transition), the same listener is NOT invoked
  - Edge cases: double-remove must not throw; listener count must be 0 after self-removal

- **AC-5**: All 8 EventId constants exist in EventId.cs with correct ID values
  - Given: EventId.cs is compiled
  - When: Reflection or static analysis enumerates EventId constants in range 1400–1407
  - Then: 8 constants exist with correct integer values; no gaps or duplicates
  - Edge cases: no EventId in range 1408–1499 is accidentally assigned to a non-scene event

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `tests/integration/scene-management/scene_events_order_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (state machine), Story 002 (load steps), Story 003 (cleanup step) — all must be DONE
- Unlocks: Downstream systems (Audio, UI, ChapterState) that consume these events can be tested end-to-end
