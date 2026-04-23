// 该文件由Cursor 自动生成

# Story 001: Object Interaction State Machine

> **Epic**: Object Interaction
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/object-interaction.md`
**Requirement**: `TR-objint-012`
*(Object state machine with 6 states)*

**ADR Governing Implementation**: ADR-013: Object Interaction State Machine
**ADR Decision Summary**: Each interactable object has a 6-state FSM (Idle → Selected → Dragging → Snapping → Locked → Unlocked) implemented via TEngine FsmModule. State transitions are driven by gesture events from the Input System. No physics simulation — objects follow finger directly.

**Engine**: Unity 2022.3.62f2 LTS + TEngine 6.0.0 + DOTween | **Risk**: LOW
**Engine Notes**: Use `GameModule.Fsm.CreateFsm<InteractableObject>()` per object instance. Each state is a `FsmState<InteractableObject>` subclass. `Locked` state is entered via `InteractionLockManager` (Story 006) — the FSM does not poll lock status per-frame; it transitions on event receipt.

**Control Manifest Rules (this layer)**:
- Required: `6-state FSM per object: Idle → Selected → Dragging → Snapping → Locked via TEngine FsmModule` (ADR-013)
- Required: `Single selection: only one object selected at a time (MVP)` (ADR-013)
- Required: `No physics simulation: objects follow finger directly — no rigidbody, no inertia` (ADR-013)
- Required: `All module access via GameModule.XXX static accessors` (ADR-001)
- Forbidden: `Never call Input.GetTouch() directly outside InputService` (ADR-010)

---

## Acceptance Criteria

*From GDD `design/gdd/object-interaction.md`, scoped to this story:*

- [ ] `InteractableObjectState` enum defines 6 values: `Idle`, `Selected`, `Dragging`, `Snapping`, `Locked`, `Unlocked`
- [ ] `InteractableObject` MonoBehaviour creates its FSM in `OnEnable` via `GameModule.Fsm`; destroys it in `OnDisable`
- [ ] `Idle → Selected`: triggered when a Tap gesture Raycast hits this object AND no other object is currently selected
- [ ] `Selected → Idle`: triggered when a Tap gesture hits empty space (deselect) or another object becomes Selected
- [ ] `Selected → Dragging`: triggered on receiving a Drag gesture start event while in Selected state
- [ ] `Dragging → Snapping`: triggered on finger release (Drag gesture end) — grid snap animation begins (Story 004)
- [ ] `Snapping → Idle`: triggered when snap animation completes; fires `ObjectTransformChanged` event
- [ ] `Idle/Selected/Dragging → Locked`: triggered by `Evt_PuzzleLockAll` event; object becomes uninteractable
- [ ] `Locked → Idle`: triggered by `Evt_PuzzleUnlock` event (when `InteractionLockManager.IsLocked` returns false after pop)
- [ ] `CurrentState` is publicly readable for debugging and other systems
- [ ] Only one `InteractableObject` can be in `Selected` or `Dragging` state at any given moment (enforced by a scene-level `InteractionCoordinator` singleton)

---

## Implementation Notes

*Derived from ADR-013 Implementation Guidelines:*

`InteractionCoordinator` is a scene-level manager (lives in chapter scene, NOT MainScene) that tracks `_selectedObject: InteractableObject`. When a new object receives a Tap:
1. If `_selectedObject` is not null, tell it to transition to `Idle` first
2. Set `_selectedObject` to the tapped object; transition it to `Selected`

```csharp
public class InteractableObject : MonoBehaviour
{
    private IFsm<InteractableObject> _fsm;

    void OnEnable() {
        _fsm = GameModule.Fsm.CreateFsm(this,
            new IdleState(), new SelectedState(), new DraggingState(),
            new SnappingState(), new LockedState(), new UnlockedState());
        _fsm.Start<IdleState>();
    }

    void OnDisable() {
        GameModule.Fsm.DestroyFsm(_fsm);
    }

    public InteractableObjectState CurrentState
        => _fsm?.CurrentState is FsmState<InteractableObject> s ? s.StateType : InteractableObjectState.Idle;
}
```

**No rigidbody**: `InteractableObject` must NOT have a `Rigidbody` or `Rigidbody2D`. Position is set directly via `transform.position` in the Dragging state (Story 002).

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: Drag mechanics (position update in DraggingState.OnUpdate)
- Story 003: Rotation mechanics (rotation update in SelectedState)
- Story 004: Grid snap animation in SnappingState
- Story 005: Visual feedback (outline, bounce) for state transitions
- Story 006: `InteractionLockManager` (the FSM transitions on Evt_PuzzleLockAll, but the lock data structure lives in Story 006)

---

## QA Test Cases

- **AC-1**: FSM starts in Idle
  - Given: `InteractableObject.OnEnable()` called
  - When: `CurrentState` queried
  - Then: Returns `InteractableObjectState.Idle`
  - Edge cases: disabling and re-enabling must restart in Idle

- **AC-2**: Tap selects object; second tap deselects
  - Given: Object A in Idle; no other selected object
  - When: Tap Raycast hits Object A
  - Then: Object A → Selected
  - When: Tap hits empty space
  - Then: Object A → Idle
  - Edge cases: Tap on Object A while already Selected must not restart transition

- **AC-3**: Only one object selected at a time
  - Given: Object A is Selected
  - When: Tap Raycast hits Object B
  - Then: Object A → Idle; Object B → Selected; `InteractionCoordinator._selectedObject == B`
  - Edge cases: 3 rapid taps on 3 different objects — final state: only 3rd is Selected

- **AC-4**: Drag → Snapping → Idle cycle
  - Given: Object A is Selected
  - When: Drag gesture starts
  - Then: Object A → Dragging
  - When: Drag gesture ends (finger released)
  - Then: Object A → Snapping
  - When: Snap animation completes
  - Then: Object A → Idle; `Evt_ObjectTransformChanged` fires

- **AC-5**: PuzzleLockAll → Locked; PuzzleUnlock → Idle
  - Given: Object A in Dragging state
  - When: `Evt_PuzzleLockAll` fires
  - Then: Object A → Locked; no further input accepted
  - When: `Evt_PuzzleUnlock` fires (and `InteractionLockManager.IsLocked == false`)
  - Then: Object A → Idle
  - Edge cases: object in Snapping state when lock arrives must finish snap before entering Locked

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/object-interaction/interaction_state_machine_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None (first story — defines the FSM skeleton)
- Unlocks: Story 002 (DraggingState logic), Story 003 (rotation in SelectedState), Story 004 (SnappingState), Story 005 (visual feedback hooks), Story 006 (lock integration), Story 007 (multi-object coordination)
