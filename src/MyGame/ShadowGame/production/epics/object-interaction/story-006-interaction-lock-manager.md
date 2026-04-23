// 该文件由Cursor 自动生成

# Story 006: InteractionLockManager with HashSet Token (SP-006)

> **Epic**: Object Interaction
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/object-interaction.md`
**Requirement**: `TR-objint-016`
*(PuzzleLock events; multi-sender safe locking)*

**ADR Governing Implementation**: ADR-013: Object Interaction State Machine + ADR-006: GameEvent Protocol
**ADR Decision Summary**: `InteractionLockManager` uses a `HashSet<string>` token-based lock set. `PushLock(token)` adds a token; `PopLock(token)` removes it. Objects are locked when the set is non-empty. This prevents mismatched unlock ordering between Narrative and Shadow Puzzle systems. Three predefined locker IDs. SP-006 confirmed the design.

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW
**Engine Notes**: `InteractionLockManager` is a service class (not MonoBehaviour). It lives in the `GameLogic` assembly. It subscribes to `Evt_PuzzleLockAll` and `Evt_PuzzleUnlock` events. On `Evt_SceneUnloadBegin`, the lock token set is force-cleared to prevent stale locks across scene transitions.

**Control Manifest Rules (this layer)**:
- Required: `PuzzleLockAll uses HashSet<string> token-based locking — objects locked when set non-empty; unlocked only when set empty` (SP-006, ADR-013)
- Required: `Legal locker IDs as predefined constants: InteractionLockerId.ShadowPuzzle="shadow_puzzle", InteractionLockerId.Narrative="narrative", InteractionLockerId.Tutorial="tutorial"` (SP-006)
- Required: `Unlock with unknown token is a no-op with warning log` (ADR-006, SP-006)
- Required: `On Evt_SceneUnloadBegin, lock token set is force-cleared` (ADR-006)
- Required: `Register all listeners in Init(); remove all in Dispose()` (ADR-006)
- Forbidden: `Never use Stack for locking — LIFO ordering is not guaranteed between senders` (SP-006)

---

## Acceptance Criteria

*From GDD `design/gdd/object-interaction.md`, scoped to this story:*

- [ ] `InteractionLockManager` class implements `PushLock(string token)` and `PopLock(string token)` methods
- [ ] `IsLocked` property returns `true` when `_activeLocks.Count > 0`; `false` when empty
- [ ] `InteractionLockerId` static class defines 3 constants: `ShadowPuzzle = "shadow_puzzle"`, `Narrative = "narrative"`, `Tutorial = "tutorial"`
- [ ] `Evt_PuzzleLockAll` (with string lockerId payload) calls `PushLock(lockerId)`
- [ ] `Evt_PuzzleUnlock` (with string lockerId payload) calls `PopLock(lockerId)`
- [ ] `PopLock` with an unknown token logs `Debug.LogWarning` and is otherwise a no-op (does not throw)
- [ ] Multi-sender test: `PushLock("shadow_puzzle")` → `PushLock("narrative")` → `PopLock("narrative")` → `IsLocked == true` → `PopLock("shadow_puzzle")` → `IsLocked == false`
- [ ] On `Evt_SceneUnloadBegin`, `_activeLocks` is force-cleared via `_activeLocks.Clear()`; `Debug.LogWarning` if cleared count > 0 (leaked locks)
- [ ] All `InteractableObject` FSMs read `InteractionLockManager.IsLocked` to decide whether to accept input

---

## Implementation Notes

*Derived from SP-006 + ADR-013 Implementation Guidelines:*

```csharp
public class InteractionLockManager
{
    private readonly HashSet<string> _activeLocks = new HashSet<string>();

    public bool IsLocked => _activeLocks.Count > 0;

    public void PushLock(string lockerId)
    {
        _activeLocks.Add(lockerId);
    }

    public void PopLock(string lockerId)
    {
        if (!_activeLocks.Remove(lockerId))
        {
            Debug.LogWarning($"[InteractionLock] Unknown locker: '{lockerId}'. Valid IDs: {string.Join(", ", InteractionLockerId.All)}");
        }
    }

    public void Init()
    {
        GameEvent.AddListener<string>(EventId.Evt_PuzzleLockAll, OnPuzzleLockAll);
        GameEvent.AddListener<string>(EventId.Evt_PuzzleUnlock, OnPuzzleUnlock);
        GameEvent.AddListener<SceneUnloadBeginPayload>(EventId.Evt_SceneUnloadBegin, OnSceneUnloadBegin);
    }

    public void Dispose()
    {
        GameEvent.RemoveListener<string>(EventId.Evt_PuzzleLockAll, OnPuzzleLockAll);
        GameEvent.RemoveListener<string>(EventId.Evt_PuzzleUnlock, OnPuzzleUnlock);
        GameEvent.RemoveListener<SceneUnloadBeginPayload>(EventId.Evt_SceneUnloadBegin, OnSceneUnloadBegin);
    }

    private void OnPuzzleLockAll(int evtId, string lockerId) => PushLock(lockerId);
    private void OnPuzzleUnlock(int evtId, string lockerId) => PopLock(lockerId);

    private void OnSceneUnloadBegin(int evtId, SceneUnloadBeginPayload payload)
    {
        if (_activeLocks.Count > 0)
            Debug.LogWarning($"[InteractionLock] Force-clearing {_activeLocks.Count} leaked locks on scene unload.");
        _activeLocks.Clear();
    }
}

public static class InteractionLockerId
{
    public const string ShadowPuzzle = "shadow_puzzle";
    public const string Narrative    = "narrative";
    public const string Tutorial     = "tutorial";
    public static readonly string[] All = { ShadowPuzzle, Narrative, Tutorial };
}
```

`InteractionLockManager` is instantiated by `InteractionCoordinator` (scene-level manager from Story 001) and injected into `InteractableObject` instances.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: FSM `Locked` state (the state exists; this story adds the manager that gates the transition)
- Narrative System: defining when `Evt_PuzzleLockAll` is sent (that's the Narrative epic's responsibility)

---

## QA Test Cases

- **AC-1**: IsLocked reflects HashSet state
  - Given: `_activeLocks` is empty
  - When: `IsLocked` queried
  - Then: `false`
  - When: `PushLock("shadow_puzzle")` called
  - Then: `IsLocked == true`
  - When: `PopLock("shadow_puzzle")` called
  - Then: `IsLocked == false`
  - Edge cases: `PushLock` with same token twice → set has 1 entry (HashSet deduplication); `PopLock` removes it → empty

- **AC-2**: Multi-sender token test (SP-006 critical scenario)
  - Given: Two senders lock concurrently
  - When: `PushLock("shadow_puzzle")` → `PushLock("narrative")`
  - Then: `IsLocked == true`; `_activeLocks.Count == 2`
  - When: `PopLock("narrative")`
  - Then: `IsLocked == true`; `_activeLocks.Count == 1` (shadow_puzzle still holds)
  - When: `PopLock("shadow_puzzle")`
  - Then: `IsLocked == false`; `_activeLocks.Count == 0`
  - Edge cases: reverse order (`PopLock("shadow_puzzle")` first) — same final result

- **AC-3**: Unknown token pop is no-op with warning
  - Given: `_activeLocks = {"shadow_puzzle"}`
  - When: `PopLock("unknown_system")`
  - Then: `_activeLocks` unchanged; `Debug.LogWarning` called; no exception thrown
  - Edge cases: PopLock on empty set with unknown token — also no-op + warning

- **AC-4**: Force-clear on Evt_SceneUnloadBegin
  - Given: `_activeLocks = {"shadow_puzzle", "narrative"}` (simulated leak)
  - When: `Evt_SceneUnloadBegin` fires
  - Then: `_activeLocks.Count == 0`; `Debug.LogWarning` logs "2 leaked locks"
  - Edge cases: if no locks held, no warning logged (only warn on actual leak)

- **AC-5**: Objects respect IsLocked
  - Given: `InteractionLockManager.IsLocked == true`
  - When: Player taps an interactable object
  - Then: Object FSM does not transition out of `Locked` state; no visual selection feedback
  - Edge cases: lock applied while object mid-drag — drag immediately stops

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/object-interaction/interaction_lock_manager_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (FSM Locked state — must be DONE)
- Unlocks: Story 007 (multi-object scene — lock manager coordinates all objects)
