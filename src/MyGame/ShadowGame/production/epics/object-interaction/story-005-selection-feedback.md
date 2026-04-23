// 该文件由Cursor 自动生成

# Story 005: Selection Visual Feedback (Outline + Scale Bounce)

> **Epic**: Object Interaction
> **Status**: Ready
> **Layer**: Core
> **Type**: Visual/Feel
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/object-interaction.md`
**Requirement**: `TR-objint-002`
*(Selection visual feedback)*

**ADR Governing Implementation**: ADR-013: Object Interaction State Machine
**ADR Decision Summary**: Selected objects receive visual feedback: an outline highlight and a scale-bounce animation on selection. These are purely cosmetic responses to FSM state transitions. They must not affect gameplay logic or collider size.

**Engine**: Unity 2022.3.62f2 LTS + DOTween | **Risk**: LOW
**Engine Notes**: Outline via a secondary material or a URP render feature (post-processing outline). Scale bounce via DOTween `transform.DOPunchScale`. The feedback must not affect the object's physics collider (collider is separate from the visual mesh scale).

**Control Manifest Rules (this layer)**:
- Required: All module access via `GameModule.XXX` static accessors (ADR-001)
- Required: All async operations via UniTask (ADR-001)

---

## Acceptance Criteria

*From GDD `design/gdd/object-interaction.md`, scoped to this story:*

- [ ] When an object transitions to `Selected` state: outline highlight activates and a scale-punch animation plays (DOTween `DOPunchScale` — punch (0.15, 0.15, 0) over 0.2s, vibrato=5)
- [ ] When an object transitions to `Idle` (deselected): outline highlight deactivates; scale returns to `Vector3.one` (if not already)
- [ ] Outline does NOT affect gameplay — no changes to Collider, Rigidbody, or physics layer
- [ ] Scale bounce does NOT affect Collider size — collider remains at its original size throughout
- [ ] Visual feedback correctly reflects state: Idle = no outline; Selected/Dragging = outline active; Snapping = outline active during tween, then deactivates on Idle; Locked = no outline
- [ ] When snap completes and object returns to Idle: scale-settle animation plays (DOTween `DOPunchScale` — small settle punch (0.05, 0.05, 0) over 0.1s)
- [ ] Feedback must work on mid-range mobile GPU (no shader overdraw issues; outline method compatible with URP)

---

## Implementation Notes

*Derived from ADR-013 Implementation Guidelines:*

Feedback is implemented in `InteractableObjectFeedback`, a companion MonoBehaviour that subscribes to the FSM's state change callbacks (via C# events on `InteractableObject`, NOT via `GameEvent` — this is an internal widget-level concern per ADR-011's UIWidget principle applied to gameplay components).

```csharp
// On Selected:
_outlineMaterial.enabled = true;
transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.2f, 5, 0.5f);

// On Idle (deselect):
_outlineMaterial.enabled = false;

// On Snapping complete (returning to Idle):
transform.DOPunchScale(new Vector3(0.05f, 0.05f, 0f), 0.1f, 3, 0.5f);
```

**Outline technique**: Use a secondary renderer with an outline-only material (render last, stencil-based). This is compatible with URP without a custom render feature in MVP. Technical Artist confirms the art bible uses a stylized outline consistent with this approach.

**Collider independence**: The `transform.DOPunchScale` affects `transform.localScale`. The collider (SphereCollider or BoxCollider) will scale with the transform. To prevent this, move the collider to a separate child GameObject that is NOT affected by scale changes — or use a dedicated physics layer and a static collider. Document the decision in code.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: FSM state transitions that trigger feedback
- Story 003: Visual feedback on rotation (out of scope for MVP — rotation has no distinct visual cue beyond the object rotating)
- Art Bible: defining exact outline color, thickness, and animation curves — Design provides these values

---

## QA Test Cases

*Visual/Feel type — manual verification steps:*

- **AC-1**: Outline activates on selection
  - Setup: Launch game; load a chapter with interactable objects; tap an object
  - Verify: Object shows visible outline highlight immediately on tap; other objects have no outline
  - Pass condition: Outline appears within 1 frame of tap; visible from 1m real-world distance on device

- **AC-2**: Scale bounce plays on selection
  - Setup: Tap any interactable object
  - Verify: Object briefly punches outward (scale up) then returns to normal; animation feels snappy and satisfying
  - Pass condition: Animation completes within 0.2s; no lingering scale artefact; scale returns to exactly (1, 1, 1)

- **AC-3**: Feedback clears on deselection
  - Setup: Tap Object A (selected); tap empty space (deselect)
  - Verify: Outline disappears immediately; scale is (1, 1, 1)
  - Pass condition: No outline visible on any object after tap-on-empty

- **AC-4**: Settle punch plays after snap
  - Setup: Drag an object; release finger
  - Verify: After snap animation completes, a small settle pulse is visible on the object
  - Pass condition: Settle animation is subtler than selection bounce; object ends at correct snapped position

- **AC-5**: No feedback in Locked state
  - Setup: Lock all objects via debug tool (simulate `Evt_PuzzleLockAll`)
  - Verify: Tapping objects produces no outline, no bounce
  - Pass condition: Zero visual response to touch when locked

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**:
- `production/qa/evidence/selection-feedback-evidence.md` — manual verification doc with screenshots + sign-off

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (FSM state hooks — must be DONE), Story 004 (snap settle hook — must be DONE)
- Unlocks: Story 007 (multi-object scene — feedback verifies which object is selected)
