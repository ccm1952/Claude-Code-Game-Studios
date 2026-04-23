// 该文件由Cursor 自动生成

# Story 002: Touch-to-World Drag with Boundary Clamping

> **Epic**: Object Interaction
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/object-interaction.md`
**Requirement**: `TR-objint-004`, `TR-objint-011`, `TR-objint-017`
*(Drag 1:1 tracking; InteractionBounds rebound; drag response ≤ 16ms)*

**ADR Governing Implementation**: ADR-013: Object Interaction State Machine
**ADR Decision Summary**: Drag follows the finger 1:1 in world space. Object position is updated via `transform.position` directly in `Update()` — no physics. `InteractionBounds` defines a rectangular boundary; if drag moves the object outside, it rebounds to the nearest in-bounds position. Drag response must be ≤ 16ms (1 frame).

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW
**Engine Notes**: Screen-to-world conversion via `Camera.ScreenToWorldPoint(touchPos)`. Camera reference must be cached — never call `Camera.main` per-frame. `InteractionBounds` from Luban `TbPuzzle` config. Fat finger compensation handled by Input System (ADR-010) before this system receives events.

**Control Manifest Rules (this layer)**:
- Required: `No physics simulation: objects follow finger directly — no rigidbody, no inertia` (ADR-013)
- Required: `All parameters (gridSize, rotationStep, snapSpeed, bounds) from Luban config` (ADR-013)
- Required: `Cache camera reference — never call Camera.main per frame` (ADR-012)
- Required: `Drag response ≤ 16ms (1 frame)` (ADR-013)
- Guardrail: `Object Interaction total update (10 objects) ≤ 1.0ms/frame` (ADR-013)

---

## Acceptance Criteria

*From GDD `design/gdd/object-interaction.md`, scoped to this story:*

- [ ] During drag, object's world position tracks the finger position 1:1 (within the same frame the gesture event arrives)
- [ ] Screen-to-world conversion is correct at any screen DPI using the cached main camera
- [ ] `InteractionBounds` (from Luban `TbPuzzle`) defines a 2D rectangle in world space; object cannot be dragged outside it
- [ ] If drag gesture end position is outside `InteractionBounds`, object rebounds (springs) to the nearest in-bounds position via DOTween EaseOutBack animation over 0.25s
- [ ] Drag does NOT snap to grid while finger is held — grid snap only on release (Story 004)
- [ ] Drag response: from gesture event receipt to `transform.position` update ≤ 16ms (1 frame)
- [ ] `InteractionBounds` values (`minX, maxX, minY, maxY`) are read from Luban `TbPuzzle` config — not hardcoded
- [ ] When object is in `Locked` state, drag events are silently ignored (no position update)

---

## Implementation Notes

*Derived from ADR-013 Implementation Guidelines:*

Inside `DraggingState.OnUpdate(IFsm<InteractableObject> fsm, float elapseSeconds, float realElapseSeconds)`:

```csharp
// Receive drag delta from InputService via GameEvent (Evt_DragGesture)
// Convert touch position to world space:
Vector3 worldPos = _cachedCamera.ScreenToWorldPoint(
    new Vector3(dragEvent.ScreenPosition.x, dragEvent.ScreenPosition.y, _dragDepth));

// Apply 1:1 tracking (no lag, no smoothing)
fsm.Owner.transform.position = new Vector3(worldPos.x, worldPos.y, fsm.Owner.transform.position.z);
```

**Boundary clamping** (applied every frame during drag):
```csharp
var bounds = _puzzleConfig.InteractionBounds;
Vector3 pos = fsm.Owner.transform.position;
pos.x = Mathf.Clamp(pos.x, bounds.MinX, bounds.MaxX);
pos.y = Mathf.Clamp(pos.y, bounds.MinY, bounds.MaxY);
fsm.Owner.transform.position = pos;
```

**Rebound animation** (on drag end, if position was outside bounds):
```csharp
if (isOutsideBounds) {
    fsm.Owner.transform.DOMove(clampedPosition, 0.25f).SetEase(Ease.OutBack);
}
```

**Camera caching**: Cache in `InteractableObject.Awake()` or a scene-init step:
```csharp
_cachedCamera = Camera.main; // ONLY in Awake/Start — never per-frame
```

**Drag depth**: `_dragDepth` is the Z distance from camera to the gameplay plane. Typically `Mathf.Abs(camera.transform.position.z - object.transform.position.z)`.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: FSM state transitions (DraggingState class is declared there)
- Story 003: Rotation during drag (separate gesture axis)
- Story 004: Grid snap on release (SnappingState handles that)

---

## QA Test Cases

- **AC-1**: Position tracks finger 1:1 within same frame
  - Given: Object A in Dragging state; touch at screen position (100, 200)
  - When: Next frame renders
  - Then: Object's world position matches `Camera.ScreenToWorldPoint(100, 200, dragDepth)` within ±0.01 world units
  - Edge cases: rapid drag across the full screen in 5 frames — no missed frames or lag accumulation

- **AC-2**: Boundary clamping prevents exit
  - Given: `InteractionBounds.MaxX = 3.0f`; object at X=2.9f
  - When: Drag moves finger to world X=4.5f
  - Then: Object X stays clamped at 3.0f; does not exceed boundary
  - Edge cases: diagonal drag into corner — both X and Y are clamped independently

- **AC-3**: Rebound animation fires on out-of-bounds release
  - Given: Object dragged to a position outside bounds and finger released
  - When: DraggingState detects drag-end outside bounds
  - Then: DOTween animation plays EaseOutBack to nearest in-bounds position over 0.25s
  - Edge cases: if object is IN bounds on release, no rebound plays

- **AC-4**: No drag while Locked
  - Given: Object in Locked state
  - When: Drag gesture event fires
  - Then: Object position unchanged; no DOTween spawned
  - Edge cases: lock applied mid-drag — object stops at current position (no snap)

- **AC-5**: Bounds values come from Luban config
  - Given: TbPuzzle row for puzzle ID 1 has `MinX=-5`, `MaxX=5`, `MinY=-3`, `MaxY=3`
  - When: Chapter 1 loads and objects initialize
  - Then: All objects clamp within those exact bounds; no hardcoded boundary values in C#
  - Edge cases: missing TbPuzzle row must log error and use a safe default fallback

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/object-interaction/drag_mechanics_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (DraggingState FSM — must be DONE)
- Unlocks: Story 004 (grid snap — builds on drag endpoint), Story 007 (multi-object drag coordination)
