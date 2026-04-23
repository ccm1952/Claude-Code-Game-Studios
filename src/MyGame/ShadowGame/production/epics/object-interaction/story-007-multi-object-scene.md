// 该文件由Cursor 自动生成

# Story 007: Multiple Interactable Objects — Single Selection at a Time

> **Epic**: Object Interaction
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/object-interaction.md`
**Requirement**: `TR-objint-001`, `TR-objint-003`, `TR-objint-019`, `TR-objint-021`
*(Raycast selection on layer; fat finger compensation; 10 objects ≥ 55fps on iPhone 13 Mini; 200ms selection debounce)*

**ADR Governing Implementation**: ADR-013: Object Interaction State Machine + ADR-010: Input Abstraction
**ADR Decision Summary**: `InteractionCoordinator` manages a scene with multiple `InteractableObject` instances. Raycast on an `Interactable` layer selects the first hit. Fat finger compensation expands the collider detection radius. Only one object may be Selected or Dragging at any time. 200ms debounce prevents accidental rapid re-selection. 10 concurrent objects must maintain ≥ 55fps on iPhone 13 Mini.

**Engine**: Unity 2022.3.62f2 LTS + TEngine + DOTween | **Risk**: LOW
**Engine Notes**: Raycast uses `Physics2D.RaycastAll` or screen-to-world ray with a dedicated `Interactable` layer mask. Fat finger compensation: expand collider detection radius using `expandedRadius = colliderRadius + fatFingerMargin * (Screen.dpi / 326)` where `fatFingerMargin` is from Luban `TbInputConfig`. 200ms debounce implemented in `InteractionCoordinator` (not in each object).

**Control Manifest Rules (this layer)**:
- Required: `Single selection: only one object selected at a time (MVP)` (ADR-013)
- Required: `Fat finger compensation: expandedRadius = colliderRadius + fatFingerMargin * (Screen.dpi / 326)` (ADR-013)
- Required: `200ms selection debounce` (ADR-013)
- Required: `All parameters from Luban config` (ADR-013)
- Required: `Never use GameObject.Find / FindObjectOfType at runtime` (tech-prefs)
- Guardrail: `Object Interaction total update (10 objects) ≤ 1.0ms/frame` (ADR-013)
- Guardrail: `Drag response ≤ 16ms` (ADR-013)

---

## Acceptance Criteria

*From GDD `design/gdd/object-interaction.md`, scoped to this story:*

- [ ] `InteractionCoordinator` is a MonoBehaviour in the chapter scene that manages all `InteractableObject` instances in the scene via a pre-populated `List<InteractableObject>` (no `FindObjectsOfType` at runtime)
- [ ] Tap Raycast uses a dedicated `Interactable` Unity layer mask — does not accidentally select UI or background elements
- [ ] Fat finger compensation: the effective selection radius is `colliderRadius + (fatFingerMargin * Screen.dpi / 326)` where `fatFingerMargin` comes from Luban `TbInputConfig`; minimum effective hit area ≥ 44pt
- [ ] Only one object may be in `Selected` or `Dragging` state at any time; selecting Object B while Object A is selected causes A to deselect first
- [ ] 200ms debounce: a second Tap on any object within 200ms of the first selection is ignored (prevents accidental double-select)
- [ ] With 10 interactable objects all in `Idle` state, frame time stays ≤ 1.0ms for the interaction system's `Update()` (profiled)
- [ ] With 10 objects and 1 in `Dragging` state, the game maintains ≥ 55fps on iPhone 13 Mini equivalent hardware
- [ ] All object instances in the scene receive `Evt_PuzzleLockAll` / `Evt_PuzzleUnlock` through the coordinator (coordinator forwards to all managed objects)
- [ ] `Evt_ObjectTransformChanged` fires correctly from any of the N objects — consumer systems receive the correct `ObjectId`

---

## Implementation Notes

*Derived from ADR-013 + ADR-010 Implementation Guidelines:*

**InteractionCoordinator** is placed in the chapter scene prefab by designers. Objects are pre-registered:

```csharp
public class InteractionCoordinator : MonoBehaviour
{
    [SerializeField] private List<InteractableObject> _objects;
    private InteractionLockManager _lockManager;
    private InteractableObject _selectedObject;
    private float _lastSelectionTime = -999f;
    private const float DEBOUNCE_SECONDS = 0.2f;

    void Start() {
        _lockManager = new InteractionLockManager();
        _lockManager.Init();
        foreach (var obj in _objects) obj.SetLockManager(_lockManager);
    }

    void OnDestroy() { _lockManager.Dispose(); }

    // Called when Evt_TapGesture fires (from InputService)
    private void OnTapGesture(int evtId, GestureData data) {
        if (_lockManager.IsLocked) return;

        // Debounce check
        if (Time.unscaledTime - _lastSelectionTime < DEBOUNCE_SECONDS) return;

        // Raycast with fat finger compensation
        var hit = RaycastWithFatFinger(data.ScreenPosition);
        if (hit == null) {
            _selectedObject?.Deselect();
            _selectedObject = null;
            return;
        }

        if (hit == _selectedObject) return; // already selected

        _selectedObject?.Deselect();
        _selectedObject = hit;
        _selectedObject.Select();
        _lastSelectionTime = Time.unscaledTime;
    }

    private InteractableObject RaycastWithFatFinger(Vector2 screenPos) {
        float margin = ConfigSystem.Tables.TbInputConfig.FatFingerMargin;
        float expandedRadius = /* collider radius */ + margin * Screen.dpi / 326f;
        // Use OverlapCircle or expanded BoxCast on Interactable layer
        ...
    }
}
```

**Pre-populate `_objects`**: Designer assigns `InteractableObject` references in the Inspector. At runtime, `Start()` validates no nulls. Never use `FindObjectsOfType` — this is forbidden per tech-prefs.

**Performance**: Each `InteractableObject.Update()` only runs its FSM tick. In Idle state, FSM tick is near-zero cost (just polling for gesture events via pre-cached listener). Profile target: 10 objects × idle tick < 1ms.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001–006: Individual object FSM, drag, rotation, snap, feedback, lock — must all be DONE
- Light source interaction (deferred — ADR-013 mentions light FSM but it is not in MVP Core stories)

---

## QA Test Cases

- **AC-1**: Raycast selects correct object on Interactable layer only
  - Given: Chapter scene with 5 `InteractableObject` instances; UI elements on UI layer
  - When: Tap gesture fires at screen position overlapping Object A
  - Then: Object A enters Selected; no UI element is accidentally selected
  - Edge cases: tap on overlapping objects (Z-depth order) — frontmost wins

- **AC-2**: Single-selection enforcement
  - Given: Object A is Selected
  - When: Tap gesture fires at Object B
  - Then: Object A → Idle (deselect); Object B → Selected; `_selectedObject == B`
  - Edge cases: rapid 3 taps on 3 different objects — only 3rd is selected; first two properly deselected

- **AC-3**: 200ms debounce prevents rapid re-selection
  - Given: Object A selected at time T
  - When: Tap fires at Object B at T + 0.15s (within debounce window)
  - Then: Tap ignored; Object A remains Selected; Object B not selected
  - When: Tap fires at Object B at T + 0.25s (outside debounce)
  - Then: Normal selection — Object B becomes Selected

- **AC-4**: Fat finger compensation minimum hit area
  - Given: `fatFingerMargin` from TbInputConfig; `Screen.dpi` = 326 (standard Retina)
  - When: Tap within `expandedRadius` of Object C's center but outside `colliderRadius`
  - Then: Object C is selected (fat finger compensation applied)
  - Pass condition: effective hit area ≥ 44pt on any device DPI

- **AC-5**: 10-object performance on target hardware
  - Given: Chapter scene with 10 `InteractableObject` instances; 1 in Dragging state
  - When: 60 consecutive frames rendered on iPhone 13 Mini equivalent
  - Then: Frame rate ≥ 55fps; interaction system Update() ≤ 1.0ms profiled; drag response ≤ 16ms
  - Edge cases: all 10 objects in Snapping simultaneously (DOTween stress) — no frame rate cliff

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `tests/integration/object-interaction/multi_object_scene_test.cs` — must exist and pass
- Performance test results logged in `production/qa/evidence/object-interaction-perf-evidence.md`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Stories 001–006 (all must be DONE — this is the integration story that ties them together)
- Unlocks: Shadow Puzzle epic (consumes `Evt_ObjectTransformChanged` from multiple objects)
