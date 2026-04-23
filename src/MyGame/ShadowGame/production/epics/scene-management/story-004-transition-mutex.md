// 该文件由Cursor 自动生成

# Story 004: Transition Mutex with Max-1 Queue

> **Epic**: Scene Management
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/scene-management.md`
**Requirement**: `TR-scene-006`
*(Transition mutual exclusion; only one transition at a time; max 1 queued request)*

**ADR Governing Implementation**: ADR-009: Scene Lifecycle & Additive Scene Strategy
**ADR Decision Summary**: Requests received during an active transition are queued with a max depth of 1 (newest overwrites previous). The queue is drained when returning to Idle. Same-scene requests are no-ops. This prevents race conditions when systems fire rapid scene change events.

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW
**Engine Notes**: State machine is single-threaded (Unity main thread). All GameEvent handlers run on the main thread. No multi-threading concerns for the mutex itself. However, UniTask `await` points within the 11-step flow create interleaving opportunities — the mutex guard must check state at each re-entry point.

**Control Manifest Rules (this layer)**:
- Required: `Transition mutex: only one transition at a time; max 1 queued request` (ADR-009)
- Required: `Request for the same scene as the current scene is ignored with a SceneReady confirmation` (ADR-009)
- Required: `Register all listeners in Init(); remove all in Dispose()` (ADR-006)
- Forbidden: `Never assume listener invocation order` (ADR-006)

---

## Acceptance Criteria

*From GDD `design/gdd/scene-management.md`, scoped to this story:*

- [ ] Only one chapter transition may execute concurrently — second request during transition is queued
- [ ] Queue depth is exactly 1: if a pending request already exists, the newest request replaces it (not appended)
- [ ] When the current transition completes (returns to `Idle`), pending request is automatically dequeued and begins
- [ ] A request to the same chapter that is currently active is silently discarded — `Evt_SceneReady` fired as confirmation; no full transition
- [ ] A request to the same chapter that is currently being loaded (i.e., `TargetChapterId == in-flight chapterId`) is rejected without queuing (would be a redundant load)
- [ ] Rapid fire of 10 `Evt_RequestSceneChange` events during a transition results in exactly 1 queued request (the 10th) and no state corruption
- [ ] `_pendingRequest` field is `null` when no request is queued; non-null when one is pending

---

## Implementation Notes

*Derived from ADR-009 Implementation Guidelines:*

The mutex lives entirely in the `Evt_RequestSceneChange` handler and the Idle state's `OnEnter` logic:

```csharp
private void OnRequestSceneChange(int eventId, RequestSceneChangePayload payload)
{
    int targetId = payload.TargetChapterId;

    // Same scene as current — no-op
    if (targetId == _currentChapterId && _currentState == SceneManagerState.Idle) {
        GameEvent.Send(EventId.Evt_SceneReady,
            new SceneReadyPayload { ChapterId = targetId });
        return;
    }

    // Same scene as in-flight target — reject silently
    if (targetId == _inflightChapterId && _currentState != SceneManagerState.Idle) {
        return;
    }

    // Idle — begin immediately
    if (_currentState == SceneManagerState.Idle) {
        BeginTransition(payload);
        return;
    }

    // Busy — overwrite pending (max depth = 1)
    _pendingRequest = payload;
}

// Called when transition completes and state returns to Idle
private void OnReturnToIdle()
{
    if (_pendingRequest.HasValue) {
        var next = _pendingRequest.Value;
        _pendingRequest = null;
        BeginTransition(next);
    }
}
```

`_inflightChapterId` tracks the chapter currently being loaded (set in `BeginTransition`, cleared on completion or error). This prevents accepting duplicate requests for the same chapter that is already mid-load.

**Debug logging**: In debug builds, log every received request, queue decision, and dequeue start with `[SceneManager]` prefix.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: FSM state enum and state machine skeleton
- Story 002: Actual async scene loading (`BeginTransition` inner logic)
- Story 005: Lifecycle events beyond `Evt_SceneReady`

---

## QA Test Cases

- **AC-1**: Only one concurrent transition
  - Given: SceneManager is in Loading state
  - When: `Evt_RequestSceneChange{TargetChapterId=3}` is sent
  - Then: Chapter 3 is stored as `_pendingRequest`; no new transition starts; state remains Loading
  - Edge cases: ensure `_currentState` check is atomic within handler

- **AC-2**: Queue overwrites on overflow
  - Given: SceneManager is transitioning; `_pendingRequest = {chapter=2}`
  - When: `Evt_RequestSceneChange{TargetChapterId=5}` is sent
  - Then: `_pendingRequest` becomes `{chapter=5}`; chapter 2 is discarded
  - Edge cases: 10 rapid sends → only 10th survives

- **AC-3**: Pending request drains automatically on Idle
  - Given: SceneManager completes a transition; `_pendingRequest = {chapter=4}`
  - When: State returns to Idle
  - Then: Transition to chapter 4 begins within the same frame; `_pendingRequest` is `null`
  - Edge cases: if `_pendingRequest` points to current chapter, apply same-chapter no-op logic

- **AC-4**: Same-chapter no-op fires Evt_SceneReady
  - Given: SceneManager is Idle; `_currentChapterId == 2`
  - When: `Evt_RequestSceneChange{TargetChapterId=2}` is sent
  - Then: `Evt_SceneReady` fires with `ChapterId=2`; no state change; no transition begins
  - Edge cases: `_currentChapterId == -1` (boot) must not match any valid chapter ID

- **AC-5**: In-flight duplicate request is rejected
  - Given: SceneManager is in Loading state targeting chapter 3; `_inflightChapterId=3`
  - When: `Evt_RequestSceneChange{TargetChapterId=3}` is sent
  - Then: Request is silently discarded; `_pendingRequest` remains unchanged
  - Edge cases: different chapter during loading should still queue normally

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/scene-management/transition_mutex_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (state machine skeleton — must be DONE)
- Unlocks: Story 005 (full integration — mutex must work before event ordering is meaningful)
