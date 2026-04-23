// 该文件由Cursor 自动生成

# Story 003: Single-Finger Rotation with Snap to Grid

> **Epic**: Object Interaction
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/object-interaction.md`
**Requirement**: `TR-objint-007`
*(Rotation snap 15°; single-finger rotation)*

**ADR Governing Implementation**: ADR-013: Object Interaction State Machine
**ADR Decision Summary**: Objects can be rotated while Selected. Rotation follows a single-finger twist gesture (Rotate gesture event from InputService). On release, rotation snaps to the nearest multiple of `rotationStep` (default 15°) via DOTween. `rotationStep` is configurable per-puzzle via Luban `TbPuzzle`.

**Engine**: Unity 2022.3.62f2 LTS + DOTween | **Risk**: LOW
**Engine Notes**: Rotation gesture event (`Evt_RotateGesture`) from Input System carries a `deltaAngle` in degrees. Apply to `transform.eulerAngles.z` (2D rotation on the Z axis in a 2.5D scene). Snap formula: `snappedAngle = Mathf.Round(rawAngle / rotationStep) * rotationStep`.

**Control Manifest Rules (this layer)**:
- Required: `All parameters (gridSize, rotationStep, snapSpeed, bounds) from Luban config` (ADR-013)
- Required: `Grid snap on finger release only — during drag, object follows finger freely` (ADR-013)
- Guardrail: `Object Interaction total update (10 objects) ≤ 1.0ms/frame` (ADR-013)

---

## Acceptance Criteria

*From GDD `design/gdd/object-interaction.md`, scoped to this story:*

- [ ] `Evt_RotateGesture` event (from InputService) received in `Selected` or `Dragging` state applies a rotation delta to `transform.eulerAngles.z`
- [ ] During active rotation (finger held), rotation follows the gesture delta freely (no snapping)
- [ ] On rotation gesture end (finger released): `snappedAngle = Mathf.Round(currentAngle / rotationStep) * rotationStep`; DOTween animates to snapped angle over `snapSpeed` seconds with EaseOutQuad
- [ ] `rotationStep` (default 15°) is read from Luban `TbPuzzle` config — not hardcoded
- [ ] `snapSpeed` is read from Luban `TbPuzzle` config — not hardcoded
- [ ] Snapped angle is normalised to [0°, 360°) range after snap
- [ ] After snap completes, `Evt_ObjectTransformChanged` fires with `{ int objectId, Vector3 position, Quaternion rotation }`
- [ ] Rotation does NOT apply when object is in `Locked` state

---

## Implementation Notes

*Derived from ADR-013 Implementation Guidelines:*

Rotation is applied in `SelectedState.OnUpdate` (and optionally `DraggingState.OnUpdate` to allow simultaneous drag + rotate):

```csharp
// On Evt_RotateGesture event received:
private void OnRotateGesture(int evtId, GestureData data)
{
    if (_currentState != InteractableObjectState.Selected &&
        _currentState != InteractableObjectState.Dragging) return;

    Vector3 euler = transform.eulerAngles;
    euler.z += data.DeltaAngle;
    transform.eulerAngles = euler;
}

// On gesture end (finger lifted):
private void OnRotateGestureEnd()
{
    float raw = transform.eulerAngles.z;
    float step = _puzzleConfig.RotationStep; // from TbPuzzle
    float snapped = Mathf.Round(raw / step) * step;
    snapped = (snapped % 360f + 360f) % 360f; // normalise to [0, 360)

    transform.DORotate(new Vector3(0, 0, snapped), _puzzleConfig.SnapSpeed)
        .SetEase(Ease.OutQuad)
        .OnComplete(() => {
            GameEvent.Send(EventId.Evt_ObjectTransformChanged,
                new ObjectTransformChangedPayload {
                    ObjectId = _objectId,
                    Position = transform.position,
                    Rotation = transform.rotation
                });
        });
}
```

**Angle accumulation**: Unity's `eulerAngles` wraps at 360°; ensure delta accumulation doesn't cause 359°→1° flips. Use `_accumulatedAngle` float and apply to `localRotation` via `Quaternion.Euler`.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: Drag position mechanics (separate from rotation)
- Story 004: Grid snap for position (same snap-on-release pattern, but for XY position)
- Story 005: Visual feedback during rotation (outline, bounce)

---

## QA Test Cases

- **AC-1**: Rotation follows gesture delta freely while held
  - Given: Object A is Selected; current rotation Z = 30°
  - When: `Evt_RotateGesture` fires with `DeltaAngle = 20°`
  - Then: Object Z rotation becomes ~50° (±1°); no snapping occurs yet
  - Edge cases: negative delta (counter-clockwise); crossing 360°/0° boundary

- **AC-2**: Snap to nearest rotationStep on release
  - Given: Object at Z=47°; `rotationStep=15°`
  - When: Rotate gesture ends
  - Then: Object snaps to 45° (nearest multiple of 15°); DOTween animation plays
  - Edge cases: Z=52.5° → snaps to 60°; Z=7.4° → snaps to 0°/360°

- **AC-3**: Snap uses EaseOutQuad over snapSpeed duration
  - Given: SnapSpeed=0.3s from TbPuzzle
  - When: Snap begins
  - Then: DOTween tween duration = 0.3s; easing = EaseOutQuad
  - Edge cases: if already at snapped angle, skip tween (or play zero-duration tween)

- **AC-4**: Evt_ObjectTransformChanged fires after snap completes
  - Given: Snap animation plays
  - When: Tween OnComplete fires
  - Then: `Evt_ObjectTransformChanged` fires with correct ObjectId, position unchanged, rotation = snapped Quaternion
  - Edge cases: event must fire AFTER animation completes, not during

- **AC-5**: Rotation ignored in Locked state
  - Given: Object in Locked state
  - When: `Evt_RotateGesture` fires
  - Then: Object rotation unchanged; no event sent
  - Edge cases: lock applied mid-rotation — current in-progress delta is applied but no snap occurs

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/object-interaction/rotation_mechanics_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (FSM — must be DONE)
- Unlocks: Story 005 (visual feedback on rotation snap), Story 007 (rotation in multi-object scene)
