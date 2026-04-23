// 该文件由Cursor 自动生成

# Story 001: Scene Manager State Machine

> **Epic**: Scene Management
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/scene-management.md`
**Requirement**: `TR-scene-006`
*(Scene transition mutual exclusion; state machine governs all transition phases)*

**ADR Governing Implementation**: ADR-009: Scene Lifecycle & Additive Scene Strategy
**ADR Decision Summary**: Scene Manager uses a formal state machine (Idle → TransitionOut → Unloading → Loading → TransitionIn → Idle + Error) to gate all transition behaviour. Only one transition runs at a time; max 1 request queued.

**Engine**: Unity 2022.3.62f2 LTS + TEngine 6.0.0 | **Risk**: MEDIUM
**Engine Notes**: State machine implemented via `TEngine FsmModule` (`GameModule.Fsm`). Each state is a discrete `FsmState<SceneManager>` subclass. Verify `GameModule.Fsm.CreateFsm<SceneManager>()` signature against TEngine 6.0 source before implementation.

**Control Manifest Rules (this layer)**:
- Required: `Scene Manager uses state machine: Idle → TransitionOut → Unloading → Loading → TransitionIn → Idle (+ Error)` (ADR-009)
- Required: `Transition mutex: only one transition at a time; max 1 queued request` (ADR-009)
- Required: `Scene transitions triggered exclusively via GameEvent.Send(Evt_RequestSceneChange, payload)` — external systems never call Scene Manager methods directly (ADR-009)
- Required: `All module access via GameModule.XXX static accessors` (ADR-001)
- Forbidden: `Never use ModuleSystem.GetModule<T>()` (ADR-001)
- Forbidden: `Never call GameEvent.Send for the same event ID inside its own handler` (re-entrancy) (ADR-006)

---

## Acceptance Criteria

*From GDD `design/gdd/scene-management.md`, scoped to this story:*

- [ ] `SceneManagerState` enum defines 6 values: `Idle`, `TransitionOut`, `Unloading`, `Loading`, `TransitionIn`, `Error`
- [ ] Scene Manager starts in `Idle` state on Init; `CurrentState` property is publicly readable via `ISceneManager`
- [ ] `Evt_RequestSceneChange` received in `Idle` → transitions to `TransitionOut`
- [ ] `Evt_RequestSceneChange` received during any non-Idle state → queued (max 1 pending slot; newest overwrites previous)
- [ ] Request for the same chapter as `CurrentChapterId` → ignored with a `Evt_SceneReady` confirmation (no-op)
- [ ] State machine progresses: `TransitionOut → Unloading → Loading → TransitionIn → Idle` on success path
- [ ] State machine progresses to `Error` on load failure; returns to `Idle` on recovery action
- [ ] `IsTransitioning` property returns `true` in all non-Idle, non-Error states
- [ ] When returning to `Idle`, if a pending request exists, immediately begins next transition
- [ ] State transitions logged in debug builds (`Debug.Log`)

---

## Implementation Notes

*Derived from ADR-009 Implementation Guidelines:*

The `SceneManager` class lives in the `GameLogic` hot-fix assembly. It implements `ISceneManager`:

```csharp
public interface ISceneManager
{
    SceneManagerState CurrentState { get; }
    int CurrentChapterId { get; }
    bool IsTransitioning { get; }
}

public enum SceneManagerState
{
    Idle, TransitionOut, Unloading, Loading, TransitionIn, Error
}
```

Internal fields:
- `_currentChapterId: int` (default -1 = no chapter loaded)
- `_pendingRequest: RequestSceneChangePayload?` (nullable; null = no pending)
- `_currentSceneHandle: SceneHandle` (ADR-005 ownership — this story declares the field; Story 002 populates it)

Register `Evt_RequestSceneChange` listener in `Init()`; remove in `Dispose()`. On receipt:
1. If `_currentState == Idle` → begin `TransitionOut` (Story 002 drives the actual flow)
2. If not Idle → store as `_pendingRequest` (overwrite any existing)
3. If same chapter → send `Evt_SceneReady` immediately, discard

On entering `Idle`, check `_pendingRequest`; if non-null, dequeue and process.

**This story only implements the state machine skeleton and mutex logic. The 11-step transition flow steps live in Stories 002–005.**

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: Actual async scene loading via YooAsset; step execution inside each state
- Story 003: Cleanup sequence (UnloadUnusedAssets + GC.Collect)
- Story 005: 8 lifecycle events firing in deterministic order

---

## QA Test Cases

- **AC-1**: State machine starts in Idle; CurrentState readable
  - Given: SceneManager.Init() called, no scene loaded
  - When: CurrentState is queried
  - Then: returns `SceneManagerState.Idle`; `IsTransitioning` returns `false`
  - Edge cases: calling Init() twice should not reset an in-progress transition

- **AC-2**: Request in Idle → TransitionOut
  - Given: State == Idle
  - When: `GameEvent.Send(Evt_RequestSceneChange, payload{TargetChapterId=1})`
  - Then: `CurrentState` becomes `TransitionOut` within the same frame; `IsTransitioning` returns `true`
  - Edge cases: payload with invalid chapter ID should still transition (chapter validity is Story 006's concern)

- **AC-3**: Request during non-Idle → queued; newest overwrites
  - Given: State == Loading
  - When: Send `Evt_RequestSceneChange` with chapter 2, then chapter 3
  - Then: Only chapter 3 is stored as `_pendingRequest`; chapter 2 is discarded
  - Edge cases: sending 10 rapid requests should result in only 1 pending

- **AC-4**: Same-chapter request is a no-op
  - Given: State == Idle; `CurrentChapterId == 2`
  - When: Send `Evt_RequestSceneChange{TargetChapterId=2}`
  - Then: `Evt_SceneReady` is fired; state remains `Idle`; no transition begins
  - Edge cases: first boot where `CurrentChapterId == -1` should never match any valid chapter ID

- **AC-5**: Pending request drains on return to Idle
  - Given: State == Loading; `_pendingRequest = {TargetChapterId=4}`
  - When: State transitions to Idle (simulated via test helper)
  - Then: Transition to chapter 4 begins immediately; `_pendingRequest` is cleared

- **AC-6**: Error state reached; recovery returns to Idle
  - Given: State == Loading
  - When: Simulated load failure
  - Then: State becomes `Error`; `IsTransitioning` returns `false`
  - When: Recovery action triggered (Retry or Return to Main Menu)
  - Then: State returns to `Idle`
  - Edge cases: calling recovery action while not in Error state should be a no-op

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/scene-management/scene_state_machine_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None (first story in epic — defines the skeleton all other stories build on)
- Unlocks: Story 002 (additive scene loading), Story 003 (cleanup sequence), Story 004 (transition mutex detail), Story 005 (scene events)
