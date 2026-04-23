// 该文件由Cursor 自动生成

# ADR-013: Object Interaction State Machine & Snap Mechanics

## Status

Proposed

## Date

2026-04-22

## Last Verified

2026-04-22

## Decision Makers

Technical Director, Lead Programmer, Game Designer

## Summary

Object Interaction is the Core Layer system through which players physically manipulate puzzle objects. We adopt a **6-state FSM** (Idle → Selected → Dragging → Snapping → Locked, plus light source states) powered by TEngine `FsmModule`, with **configurable grid snap** via DOTween animations, **fat finger compensation** scaled by device DPI, and **haptic feedback** on snap events. All configuration (gridSize, rotationStep, snapSpeed, bounds) is Luban-driven.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.62f2 (LTS) |
| **Domain** | Core / Input Processing / Physics / Animation |
| **Knowledge Risk** | LOW (Unity Physics, DOTween) / MEDIUM (TEngine FsmModule) |
| **References Consulted** | `object-interaction.md`, `architecture.md` §4.2/§6.2, `input-system.md` |
| **Post-Cutoff APIs Used** | TEngine `FsmModule`, TEngine `GameEvent` |
| **Verification Required** | Sprint 0: confirm FsmModule state machine creation API; verify DOTween tween pooling under frequent snap operations |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-010 (Input Abstraction — provides gesture events consumed by this system) |
| **Enables** | ADR-012 (Shadow Match — consumes `ObjectTransformChanged` and `LightPositionChanged` events) |
| **Blocks** | All object manipulation gameplay; Shadow Puzzle cannot receive transform data without this system |
| **Ordering Note** | ADR-010 must be Accepted; this ADR should reach Accepted before Core Layer Sprint begins |

## Context

### Problem Statement

Players interact with puzzle objects via touch gestures (drag, rotate, pinch). The system must translate gesture input into weighted, grid-snapped object movement that feels like "gently rearranging someone's belongings" (design pillar: 日常即重量). Key challenges:

1. **State complexity**: Objects can be idle, selected, dragging, snapping, or locked by external systems — transitions must be deterministic and cover all edge cases
2. **Snap feel**: Grid snap must feel like "magnetic pull-in" (EaseOutQuad), not hard teleportation
3. **Mobile ergonomics**: Fat finger compensation must scale with device DPI; touch targets must meet Apple HIG 44pt minimum
4. **External locking**: Narrative sequences and PerfectMatch must lock all objects mid-operation without deadlocking the FSM
5. **Performance**: 10 objects on screen, ≤ 1ms total Update cost per frame (TR-objint-020)

### Constraints

- **Single selection**: Only one object selected at a time (MVP)
- **No physics simulation**: Objects follow finger directly — no rigidbody, no inertia, no collision between objects
- **Grid snap only on release**: During drag, object follows finger freely; snap triggers on finger lift
- **Haptic budget**: Haptic feedback must be "extremely restrained" — UIImpactFeedbackGenerator.light for snap, medium for putdown
- **Layer isolation**: This system (Core Layer) consumes Input events (Foundation Layer) and produces transform events consumed by Shadow Puzzle (Feature Layer)

### Requirements

- TR-objint-001: 6-state object FSM
- TR-objint-014/015/016: Events via GameEvent (ObjectTransformChanged, LightPositionChanged, PuzzleLockAll)
- TR-objint-019: Drag response ≤ 16ms (1 frame)
- TR-objint-020: Total system update ≤ 1ms/frame
- TR-objint-022: Haptic feedback on snap

## Decision

**Implement a per-object 6-state FSM using TEngine FsmModule, with DOTween-driven snap animations, DPI-scaled fat finger compensation, and Luban-configurable grid/rotation parameters.**

### Architecture

```
┌───────────────────────────────────────────────────────────────┐
│                    Object Interaction System                    │
│                                                                │
│  ┌──────────────────────┐     ┌──────────────────────────┐   │
│  │ Per-Object FSM       │     │ Light Source FSM          │   │
│  │ (TEngine FsmModule)  │     │ (TEngine FsmModule)       │   │
│  │                      │     │                            │   │
│  │ Idle ──→ Selected    │     │ Fixed                      │   │
│  │  ↑        ↓     ↓    │     │ TrackIdle ──→ TrackDragging│   │
│  │  └── Snapping ←─┘    │     │    ↑            ↓          │   │
│  │       Dragging        │     │    └── TrackSnapping       │   │
│  │  Locked (any→Locked)  │     └──────────────────────────┘   │
│  └──────────┬───────────┘                                     │
│             │ events                                          │
│  ┌──────────┴───────────────────────────────────────────┐    │
│  │ GameEvent output:                                     │    │
│  │  Evt_ObjectTransformChanged { objectId, pos, rot }    │    │
│  │  Evt_LightPositionChanged { lightId, trackT }         │    │
│  │  Evt_ObjectSelected { objectId }                      │    │
│  │  Evt_ObjectDeselected { }                             │    │
│  └───────────────────────────────────────────────────────┘    │
│             ▲ consumes                                        │
│  ┌──────────┴───────────────────────────────────────────┐    │
│  │ Input Events (from ADR-010):                          │    │
│  │  Evt_Gesture_Tap, Evt_Gesture_Drag,                   │    │
│  │  Evt_Gesture_Rotate, Evt_Gesture_LightDrag            │    │
│  └───────────────────────────────────────────────────────┘    │
└───────────────────────────────────────────────────────────────┘
```

### Key Interfaces

```csharp
public enum ObjectState { Idle, Selected, Dragging, Snapping, Locked }
public enum LightState  { Fixed, TrackIdle, TrackDragging, TrackSnapping }

public class InteractableObject : MonoBehaviour
{
    public int ObjectId { get; }
    public ObjectState CurrentState { get; }

    // External control (via GameEvent listeners)
    public void SetLocked(bool locked);
    public void SnapToTarget(Vector3 targetPos, Quaternion targetRot, float duration);
}

public interface IObjectInteraction
{
    InteractableObject GetSelectedObject();
    bool IsAnyObjectDragging();
}
```

### Grid Snap Mechanics

```
snappedPos.x = round(rawPos.x / gridSize) * gridSize
snappedPos.z = round(rawPos.z / gridSize) * gridSize
snappedAngle = round(rawAngle / rotationStep) * rotationStep

snapDuration = clamp(distance / snapSpeed, minSnapDuration, maxSnapDuration)
// Animated via DOTween: EaseOutQuad for position, EaseOutQuad for rotation
```

| Parameter | Default | Source | Range |
|-----------|---------|--------|-------|
| gridSize | 0.25 units | TbPuzzle per chapter | 0.1-0.5 |
| rotationStep | 15° | TbPuzzle | 10-45° |
| snapSpeed | 3.0 u/s | config | 2.0-5.0 |
| minSnapDuration | 0.05s | config | 0.03-0.08s |
| maxSnapDuration | 0.15s | config | 0.10-0.25s |

### Fat Finger Compensation

```csharp
float expandedRadius = colliderRadius + fatFingerMargin * (Screen.dpi / referenceDPI);
// referenceDPI = 326 (iPhone 13 Mini)
// fatFingerMargin = 8dp base, scaled by touch_sensitivity setting
```

Ensures minimum touch target ≥ 44pt (Apple HIG) on all supported devices.

### Haptic Feedback Integration

| Event | iOS API | Android API | Duration | Intensity |
|-------|---------|-------------|----------|-----------|
| Grid snap complete | UIImpactFeedbackGenerator.light | VibrationEffect (amplitude 20) | 15ms | Minimal |
| Object putdown | UIImpactFeedbackGenerator.medium | VibrationEffect (amplitude 40) | 30ms | Light |
| Boundary rebound | UIImpactFeedbackGenerator.rigid | VibrationEffect (amplitude 30) | 20ms | Light |
| Light track endpoint | UINotificationFeedbackGenerator.warning | VibrationEffect (amplitude 25) | 25ms | Light |

All haptic gated by `Settings.haptic_enabled`. Low-end Android devices (no VibrationEffect API) gracefully degrade to no haptic.

### State Transition Rules

- `Idle → Selected`: Raycast hit on InteractableObject collider (with fat finger expansion)
- `Selected → Dragging`: Any finger movement (zero threshold — instant transition)
- `Selected → Idle`: Tap on blank area or another object
- `Dragging → Snapping`: Finger release — calculate nearest grid point, start DOTween animation
- `Snapping → Idle`: DOTween animation complete
- `Snapping → Selected`: Finger re-touches same object (interrupts snap animation, object stays at current interpolated position)
- `Any → Locked`: `Evt_PuzzleLockAll` received — immediate transition, no animation
- `Locked → Idle`: `Evt_PuzzleUnlock` received

## Alternatives Considered

### Alternative 1: Physics-Based Interaction (Rigidbody + Colliders)

- **Description**: Use Unity physics (Rigidbody2D/3D) for object movement with configurable drag, angular drag, and collider-based boundary enforcement
- **Pros**: Realistic weight feel; automatic collision handling between objects; built-in boundary enforcement via collider walls
- **Cons**: Adds unpredictable physics behavior (jitter, tunneling) counter to the "direct control" design requirement; physics step timing conflicts with touch-synchronous updates; significant performance overhead for 10+ objects; grid snap requires fighting physics engine
- **Rejection Reason**: GDD explicitly requires "no inertia, no delay, arcade-feel direct control." Physics-based interaction fundamentally conflicts with the "finger to where = object to where" requirement.

### Alternative 2: Custom State Machine (No FsmModule)

- **Description**: Implement FSM with plain C# enums and switch statements instead of TEngine FsmModule
- **Pros**: Zero framework dependency; slightly lower overhead; full control over state storage
- **Cons**: Loses TEngine's FSM debugging tools; must manually implement enter/exit callbacks, state history, and validation; diverges from project-wide TEngine convention (ADR-001)
- **Rejection Reason**: TEngine FsmModule is already adopted project-wide (ADR-001). Using a different FSM pattern for one system creates inconsistency. FsmModule's overhead is negligible (< 0.01ms per state check).

### Alternative 3: Continuous Snap (Snap During Drag, Not On Release)

- **Description**: Object continuously snaps to nearest grid point while being dragged, rather than snapping only on finger release
- **Pros**: Object always on grid — what you see is what you get; no post-release animation needed
- **Cons**: Makes drag feel "sticky" and stepped rather than smooth; GDD explicitly requires "拖拽过程中物件在格点间自由移动"; conflicts with 16ms drag response requirement if snap calculations cause micro-stalls
- **Rejection Reason**: GDD specifies that objects follow finger freely during drag, with snap only on release. Continuous snap was explicitly rejected in the GDD's game feel section.

## Consequences

### Positive

- **Deterministic FSM**: Every possible input/event is handled by the FSM — no undefined states or transitions
- **Smooth feel**: DOTween EaseOutQuad snap creates the "magnetic pull-in" feel described in GDD
- **Mobile-optimized**: DPI-scaled fat finger compensation ensures usability across iPhone SE to iPad
- **Externally controllable**: Locked state and SnapToTarget allow Narrative and Puzzle systems to drive objects without bypassing the FSM
- **Consistent with framework**: Uses TEngine FsmModule per ADR-001

### Negative

- **DOTween dependency**: Adding DOTween as an animation driver — if DOTween has issues, snap animations break. Mitigation: DOTween is mature and battle-tested
- **Single-select limitation**: MVP only supports one selected object at a time. Multi-select would require FSM redesign
- **Haptic platform fragmentation**: Android haptic quality varies widely; some devices may have poor or no haptic response

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| TEngine FsmModule API differs from assumptions | MEDIUM | MEDIUM | Sprint 0 spike to verify FSM creation, state callbacks, and transition API |
| DOTween tween pooling causes memory pressure under rapid snap/cancel cycles | LOW | LOW | Pre-warm tween pool; monitor allocation with Unity Profiler |
| Fat finger expansion causes accidental selection of adjacent objects | MEDIUM | LOW | Z-depth priority (closer to camera wins); tunable fatFingerMargin; playtest on smallest target device |
| Locked state during Snapping leaves object at intermediate position | LOW | MEDIUM | On PuzzleLockAll, kill active DOTween, object stays at current position. SnapToTarget can reposition if needed. |

## Performance Implications

| Metric | Expected | Budget | Notes |
|--------|----------|--------|-------|
| CPU (per-frame Update, 10 objects) | 0.3-0.5ms | ≤ 1.0ms (TR-objint-020) | Raycast + FSM update + DOTween tick |
| Memory | ~50KB (10 objects × FSM + collider references) | Negligible | No dynamic allocation during gameplay |
| Haptic latency | < 5ms from snap complete | N/A | OS-level haptic API, no Unity overhead |

## Validation Criteria

- [ ] All 6 FSM states and transitions exercised in automated test — no dead states or illegal transitions
- [ ] Drag response ≤ 16ms verified via Unity Profiler on iPhone 13 Mini
- [ ] Grid snap animation duration within 50-150ms range for all grid distances
- [ ] Fat finger compensation: 10 consecutive selection attempts on iPhone 13 Mini succeed ≥ 9 times for all object sizes
- [ ] PuzzleLockAll during Dragging immediately halts object — no residual movement
- [ ] SnapToTarget (from Narrative) correctly moves Locked object to precise position
- [ ] Haptic fires on snap (iOS Taptic Engine verified), can be disabled via settings
- [ ] All parameters (gridSize, rotationStep, snapSpeed, bounds) loaded from Luban — no hardcoded values
- [ ] 10 objects on screen, continuous dragging: frame time < 1ms for Object Interaction system

## GDD Requirements Addressed

| GDD Document | Requirement | How This ADR Satisfies It |
|-------------|-------------|--------------------------|
| `object-interaction.md` | 6-state object FSM (Idle/Selected/Dragging/Snapping/Locked + Rotating) | FsmModule with 6 states; Rotating handled as sub-mode of Selected |
| `object-interaction.md` | Grid snap with configurable gridSize, rotationStep | Luban TbPuzzle config per chapter; round-to-nearest formula |
| `object-interaction.md` | Fat finger compensation (8dp base, DPI-scaled) | `expandedRadius = colliderRadius + fatFingerMargin * (dpi/326)` |
| `object-interaction.md` | Haptic feedback on snap (UIImpactFeedbackGenerator.light) | Platform-specific haptic integration gated by `haptic_enabled` |
| `object-interaction.md` | DOTween EaseOutQuad snap animation | DOTween sequence: position snap + optional rotation snap |
| `object-interaction.md` | Light source track movement (Fixed/TrackIdle/TrackDragging/TrackSnapping) | Separate FsmModule FSM for light sources |
| `object-interaction.md` | Boundary clamp with EaseOutBack rebound | Clamp during drag; DOTween EaseOutBack rebound on release if snap target is outside bounds |
| `architecture.md` §6.2 | IObjectInteraction interface | Implemented as specified in architecture |

## Related

- **Depends On**: ADR-010 (Input Abstraction) — gesture events are the sole input to this system
- **Consumed By**: ADR-012 (Shadow Match Algorithm) — receives `Evt_ObjectTransformChanged` / `Evt_LightPositionChanged`
- **Consumed By**: ADR-016 (Narrative Sequence Engine) — sends `Evt_PuzzleLockAll` / `Evt_PuzzleSnapToTarget`
- **References**: `architecture.md` §4.2 (Object Interaction module ownership), §6.2 (IObjectInteraction interface)
