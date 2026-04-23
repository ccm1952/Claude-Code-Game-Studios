// 该文件由Cursor 自动生成

# Story 004: Grid Snapping System

> **Epic**: Object Interaction
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/object-interaction.md`
**Requirement**: `TR-objint-005`, `TR-objint-006`
*(Grid snap formula; snap interpolation EaseOutQuad)*

**ADR Governing Implementation**: ADR-013: Object Interaction State Machine
**ADR Decision Summary**: Grid snap occurs on finger release only (not during drag). Formula: `snappedPos = round(rawPos / gridSize) * gridSize`. Animated via DOTween EaseOutQuad over `snapSpeed` seconds. `gridSize` and `snapSpeed` are Luban config values.

**Engine**: Unity 2022.3.62f2 LTS + DOTween | **Risk**: LOW
**Engine Notes**: Snap applies to both X and Y independently. Snap animation runs in `SnappingState`. After snap completes, FSM transitions to `Idle` and `Evt_ObjectTransformChanged` fires.

**Control Manifest Rules (this layer)**:
- Required: `Grid snap on finger release only — during drag, object follows finger freely` (ADR-013)
- Required: `Snap formula: snappedPos = round(rawPos / gridSize) * gridSize — animated via DOTween EaseOutQuad` (ADR-013)
- Required: `All parameters (gridSize, rotationStep, snapSpeed, bounds) from Luban config` (ADR-013)
- Guardrail: `Object Interaction total update (10 objects) ≤ 1.0ms/frame` (ADR-013)

---

## Acceptance Criteria

*From GDD `design/gdd/object-interaction.md`, scoped to this story:*

- [ ] Grid snap formula: `snappedX = Mathf.Round(rawX / gridSize) * gridSize` (and same for Y)
- [ ] Snap animation: DOTween moves object from current position to snapped position over `snapSpeed` seconds with EaseOutQuad easing
- [ ] `gridSize` and `snapSpeed` are read from Luban `TbPuzzle` — not hardcoded
- [ ] Snap occurs in `SnappingState` — no snap during active drag (`DraggingState`)
- [ ] After snap animation completes: FSM transitions `Snapping → Idle`; `Evt_ObjectTransformChanged` fires with final snapped position and current rotation
- [ ] If snapped position == current position (already on grid), skip animation and transition immediately to Idle
- [ ] Snap respects `InteractionBounds`: snapped target position is additionally clamped to bounds if the snap calculation lands outside (from Story 002's bounds data)
- [ ] If DOTween tween is interrupted (e.g., object re-selected during snap), tween is killed and object stays at current position

---

## Implementation Notes

*Derived from ADR-013 Implementation Guidelines:*

```csharp
public class SnappingState : FsmState<InteractableObject>
{
    protected override void OnEnter(IFsm<InteractableObject> fsm)
    {
        var pos = fsm.Owner.transform.position;
        float gs = _puzzleConfig.GridSize; // from TbPuzzle
        float snappedX = Mathf.Round(pos.x / gs) * gs;
        float snappedY = Mathf.Round(pos.y / gs) * gs;

        // Clamp to bounds after snap calculation
        var bounds = _puzzleConfig.InteractionBounds;
        snappedX = Mathf.Clamp(snappedX, bounds.MinX, bounds.MaxX);
        snappedY = Mathf.Clamp(snappedY, bounds.MinY, bounds.MaxY);

        Vector3 snappedPos = new Vector3(snappedX, snappedY, pos.z);

        // Skip tween if already snapped
        if (Vector3.Distance(pos, snappedPos) < 0.001f) {
            OnSnapComplete(fsm);
            return;
        }

        fsm.Owner.transform
            .DOMove(snappedPos, _puzzleConfig.SnapSpeed)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => OnSnapComplete(fsm));
    }

    private void OnSnapComplete(IFsm<InteractableObject> fsm)
    {
        GameEvent.Send(EventId.Evt_ObjectTransformChanged,
            new ObjectTransformChangedPayload {
                ObjectId = fsm.Owner.ObjectId,
                Position = fsm.Owner.transform.position,
                Rotation = fsm.Owner.transform.rotation
            });
        fsm.ChangeState<IdleState>();
    }
}
```

**Tween interruption**: In `SnappingState.OnLeave()`, call `fsm.Owner.transform.DOKill()` to cancel any running snap tween when the state is exited early.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: Boundary clamping during active drag
- Story 003: Rotation snap (same pattern but for angle)
- Story 005: Visual "settle" feedback when snap completes

---

## QA Test Cases

- **AC-1**: Snap formula is correct
  - Given: `gridSize = 1.0`; object position = (1.4, 2.7, 0)
  - When: Finger released; SnappingState entered
  - Then: Object animates to (1.0, 3.0, 0)
  - Edge cases: negative positions — (-0.6, -1.4) → (-1.0, -1.0); exactly on grid (1.0, 2.0) → no animation

- **AC-2**: Animation uses EaseOutQuad over snapSpeed
  - Given: `snapSpeed = 0.2s` from TbPuzzle
  - When: Snap animation plays
  - Then: Tween duration = 0.2s; easing = EaseOutQuad (verify via DOTween inspector or mock)
  - Edge cases: `snapSpeed = 0` must not cause a Division-by-zero; treat as immediate snap

- **AC-3**: Evt_ObjectTransformChanged fires after snap
  - Given: Snap completes to (2.0, 1.0, 0)
  - When: DOTween OnComplete fires
  - Then: `Evt_ObjectTransformChanged` payload has `Position = (2.0, 1.0, 0)`; Rotation unchanged from pre-snap
  - Edge cases: event fires AFTER animation, not when SnappingState is entered

- **AC-4**: Already on grid skips animation
  - Given: Object at exactly (1.0, 2.0, 0); `gridSize = 1.0`
  - When: Finger released
  - Then: No DOTween tween spawned; FSM transitions directly to Idle; `Evt_ObjectTransformChanged` still fires
  - Edge cases: floating-point tolerance: (1.0001, 2.0, 0) should still snap to (1.0, 2.0, 0)

- **AC-5**: Snap post-clamp respects bounds
  - Given: `gridSize=1.0`; `MaxX=3.0`; finger released at X=3.4 (snaps to 3.0 mathematically, then clamp)
  - When: Snap calculation runs
  - Then: Snapped X = 3.0 (within bounds); no out-of-bounds final position
  - Edge cases: if snap would land at 4.0 but bounds max is 3.5 → snaps to 3.0 (nearest in-bounds)

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/object-interaction/grid_snap_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (FSM SnappingState declared), Story 002 (bounds clamping values)
- Unlocks: Story 007 (multi-object scene — snap is needed to verify final positions)
