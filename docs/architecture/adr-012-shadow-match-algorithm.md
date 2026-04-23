// 该文件由Cursor 自动生成

# ADR-012: Shadow Match Algorithm (Multi-Anchor Weighted Scoring)

## Status

Proposed

## Date

2026-04-22

## Last Verified

2026-04-22

## Decision Makers

Technical Director, Lead Programmer, Game Designer

## Summary

The Shadow Puzzle System needs a reliable, performant, and tunable algorithm to determine how closely a player's object arrangement matches the target shadow. We adopt a **multi-anchor weighted scoring** approach where each puzzle object carries 1-3 PuzzleAnchors projected into screen space, scored on position/direction/visibility, then combined via weighted average with 0.2s temporal smoothing. Shadow pixel data is sampled via `AsyncGPUReadback` from the ShadowRT provided by URP Shadow Rendering.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.62f2 (LTS) |
| **Domain** | Gameplay / Rendering / GPU Readback |
| **Knowledge Risk** | LOW — standard Unity APIs (AsyncGPUReadback, Camera projection, RenderTexture) |
| **References Consulted** | `shadow-puzzle-system.md`, `urp-shadow-rendering.md`, `architecture.md` §5.1/§6.3 |
| **Post-Cutoff APIs Used** | TEngine `FsmModule` for puzzle state machine, `GameEvent` for score broadcast |
| **Verification Required** | Sprint 0: confirm `AsyncGPUReadback` stability on low-end Android (Mali-G52); validate CPU processing time ≤ 1.5ms on target device |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-002 (URP Shadow Rendering — provides ShadowRT and `AsyncGPUReadback` pipeline) |
| **Enables** | ADR-014 (Puzzle State Machine — consumes matchScore for state transitions), ADR-015 (Hint System — reads matchScore and anchorScores) |
| **Blocks** | Shadow Puzzle System implementation cannot begin without this algorithm definition |
| **Ordering Note** | ADR-002 must be Accepted first; this ADR should reach Accepted before Shadow Puzzle Sprint begins |

## Context

### Problem Statement

《影子回忆》's core gameplay loop requires comparing the player's current object/light arrangement against a designer-authored target shadow configuration. The algorithm must:

1. Produce a continuous `matchScore ∈ [0.0, 1.0]` reflecting overall similarity
2. Provide per-anchor breakdown for the Hint System to identify which object is furthest from target
3. Handle 2-5 objects with 1-3 anchors each (up to 15 anchors per puzzle)
4. Run within a strict **2ms per-frame budget** (TR-puzzle-010) on iPhone 13 Mini
5. Be tunable via Luban config tables (thresholds, weights, distances) without code changes
6. Support the "absence puzzle" variant where `maxCompletionScore < 1.0`

### Constraints

- **Performance**: Shadow match calculation ≤ 2ms/frame (within 16.67ms total frame budget, sharing with other systems consuming ~3.5ms)
- **ShadowRT readback**: `AsyncGPUReadback` introduces 1-3 frame latency; algorithm must tolerate stale data gracefully
- **Temporal stability**: Score must not flicker during micro-adjustments — requires smoothing
- **Data-driven**: All thresholds from Luban `TbPuzzle` / `TbPuzzleObject` tables (Architecture Principle P1)
- **Decoupled**: Algorithm runs in Shadow Puzzle System (Feature Layer); reads ShadowRT from URP Shadow Rendering (Foundation Layer) via `GetShadowRT()` — no direct rendering dependency

### Requirements

- TR-puzzle-001: Multi-anchor weighted scoring
- TR-puzzle-002: Per-anchor position/direction/visibility decomposition
- TR-puzzle-010: ≤ 2ms frame budget for match calculation
- TR-render-019: ShadowRT CPU readback ≤ 1.5ms
- TR-concept-008: No hardcoded gameplay values

## Decision

**Adopt a multi-anchor weighted scoring algorithm with AsyncGPUReadback-based shadow sampling and 0.2s temporal smoothing.**

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                   Shadow Puzzle System                        │
│  ┌─────────────────────────────────────────────────────┐    │
│  │              ShadowMatchCalculator                    │    │
│  │                                                       │    │
│  │  Per Frame:                                           │    │
│  │  1. Read ShadowRT (via AsyncGPUReadback callback)     │    │
│  │  2. For each PuzzleAnchor:                            │    │
│  │     a. Project anchor world pos → screen space        │    │
│  │     b. Compare vs target screen pos → positionScore   │    │
│  │     c. Compare projected direction → directionScore   │    │
│  │     d. Sample ShadowRT at anchor → visibilityScore    │    │
│  │     e. anchorScore = pos × dir × vis                  │    │
│  │  3. matchScore = Σ(w_i × s_i) / Σ(w_i)              │    │
│  │  4. Apply temporal smoothing (0.2s sliding window)    │    │
│  │  5. Broadcast Evt_MatchScoreChanged                   │    │
│  └────────────────────────┬────────────────────────────┘    │
│                           │ reads                            │
├───────────────────────────┼──────────────────────────────────┤
│  URP Shadow Rendering     │                                  │
│  ┌────────────────────────┴────────────────────────┐        │
│  │ ShadowSampleCamera → ShadowRT (R8, 1024×1024)  │        │
│  │ AsyncGPUReadback.Request() → CPU pixel buffer    │        │
│  └─────────────────────────────────────────────────┘        │
└─────────────────────────────────────────────────────────────┘
```

### Key Interfaces

```csharp
public struct AnchorScore
{
    public int AnchorId;
    public float PositionScore;    // 0-1
    public float DirectionScore;   // 0-1
    public float VisibilityScore;  // 0 or 1
    public float CombinedScore;    // positionScore × directionScore × visibilityScore
    public float Weight;           // from Luban config
}

public class ShadowMatchCalculator
{
    // Core scoring pipeline
    public float CalculateMatchScore(
        PuzzleAnchor[] anchors,
        AnchorTarget[] targets,
        NativeArray<byte> shadowRTData,
        int rtWidth, int rtHeight
    );

    // Per-anchor breakdown (consumed by Hint System at 1s polling)
    public AnchorScore[] GetAnchorScores();

    // Temporal smoothing
    public float GetSmoothedScore();
}
```

### Scoring Formula

**Per-anchor score:**

```
anchorScore_i = positionScore_i × directionScore_i × visibilityScore_i

positionScore  = 1.0 - clamp(screenDistance / maxScreenDistance, 0, 1)
directionScore = 1.0 - clamp(angleDelta / maxAngleDelta, 0, 1)
visibilityScore = shadowRTSample >= visibilityThreshold ? 1.0 : 0.0
```

**Weighted average:**

```
rawMatchScore = Σ(anchorWeight_i × anchorScore_i) / Σ(anchorWeight_i)
```

**Temporal smoothing (exponential moving average within 0.2s window):**

```
smoothingFactor = 1.0 - exp(-deltaTime / smoothingWindow)
matchScore = lerp(previousMatchScore, rawMatchScore, smoothingFactor)
```

| Parameter | Default | Source | Range |
|-----------|---------|--------|-------|
| maxScreenDistance | 120 px | TbPuzzle per chapter | 50-200 px |
| maxAngleDelta | 30° | TbPuzzle per chapter | 15-45° |
| visibilityThreshold | 128 (of 255) | config | 64-192 |
| smoothingWindow | 0.2s | config | 0.1-0.5s |
| PerfectMatch threshold | 0.85 | TbPuzzle | 0.75-0.95 |
| NearMatch entry | 0.40 | TbPuzzle | 0.30-0.55 |
| NearMatch exit (hysteresis) | 0.35 | TbPuzzle | NearMatch entry - 0.05 |

### ShadowRT Sampling Pipeline

1. URP Shadow Rendering renders `ShadowSampleCamera` → `ShadowRT` (R8 grayscale, 1024×1024 at Medium tier)
2. `AsyncGPUReadback.Request(shadowRT)` is issued each frame (or every other frame on low-end)
3. Callback receives `NativeArray<byte>` — no GC allocation
4. `ShadowMatchCalculator` samples the pixel buffer at projected anchor positions
5. If readback fails or is stale, reuse previous frame's buffer (graceful degradation)

### Cross-Chapter Difficulty Matrix Integration

Per architecture.md and shadow-puzzle-system.md, each chapter overrides scoring parameters via `TbPuzzle`:

| Dimension | Ch.1 | Ch.2 | Ch.3 | Ch.4 | Ch.5 |
|-----------|------|------|------|------|------|
| maxScreenDistance | 80-100px | 120px | 120px | 120px | 120px |
| nearMatchThreshold | 0.40 | 0.40 | 0.40 | 0.35 | 0.35 |
| perfectMatchThreshold | 0.85 | 0.85 | 0.85 | 0.80 | 0.78 (non-absence) |
| maxCompletionScore | N/A | N/A | N/A | N/A | 0.60-0.70 (absence) |

Ch.4-5 shadow style softening (Edge Sharpness 0.7, Penumbra 2-4px) increases visual difficulty by ~15-20%; threshold reductions of 5-7% compensate.

### Implementation Guidelines

1. **AsyncGPUReadback callback**: Process in the callback on the render thread, copy results to a double-buffered `NativeArray<byte>`. The main-thread calculator reads the front buffer.
2. **Screen-space projection**: Use `Camera.main.WorldToScreenPoint()` for anchor positions. Cache the camera reference — don't call `Camera.main` every frame.
3. **Multiplicative scoring**: `positionScore × directionScore × visibilityScore` ensures any single failure dimension (e.g., occluded anchor) zeros out that anchor's contribution.
4. **Edge case — all anchors invisible**: Force `matchScore = 0` when `Σ(visibilityScore_i) == 0`. Prevents false positives from weighted average of zero-weighted terms.
5. **Temporal smoothing reset**: On puzzle state change (e.g., entering Active), reset the smoothing buffer to avoid carrying stale scores from a previous state.

## Alternatives Considered

### Alternative 1: Pure Pixel-Comparison (Image Diff)

- **Description**: Render current shadow and target shadow to two RTs, compute pixel-by-pixel difference (MSE or SSIM)
- **Pros**: Captures the exact visual similarity players see; no manual anchor placement needed
- **Cons**: Extremely expensive on mobile (two RT renders + full-resolution comparison); sensitive to shadow edge softness changes between chapters; not decomposable per-object (Hint System needs per-object scores); SSIM on 1024×1024 exceeds 2ms budget
- **Estimated Effort**: Higher GPU cost, lower designer effort
- **Rejection Reason**: Performance budget violation on mobile; no per-object breakdown for Hint System; shadow style changes between chapters would require recalibration of the diff threshold per chapter

### Alternative 2: Pure Transform Comparison (No Shadow Sampling)

- **Description**: Compare object transforms (position, rotation) directly against target transforms, ignoring actual shadow appearance
- **Pros**: Zero GPU cost; deterministic; trivial to implement and debug
- **Cons**: Disconnects the scoring from what players actually see (the shadow); changes in light position affect shadow but not object transforms; breaks the "shadow is the puzzle" design pillar; doesn't account for occlusion
- **Estimated Effort**: Lowest implementation cost
- **Rejection Reason**: Fundamentally misaligned with the game's core concept — players interact with shadows, not object transforms. A player could have objects in "correct" positions but with wrong shadow due to light position.

### Alternative 3: Hybrid with ShadowRT Region Matching

- **Description**: Use anchor-based scoring for coarse alignment, then refine with a localized ShadowRT region comparison (small patches around each anchor)
- **Pros**: Best of both worlds — fast coarse pass plus visual verification; catches edge cases where anchors align but shadow shape differs
- **Cons**: Double the complexity; patch comparison adds ~0.5-1ms per anchor; diminishing returns for puzzles with ≤ 3 objects; harder to tune two scoring systems simultaneously
- **Estimated Effort**: 2x implementation effort vs chosen approach
- **Rejection Reason**: Over-engineering for MVP. The multiplicative anchor scoring with visibility sampling already captures the key failure modes. Can revisit in Alpha if playtesting reveals scoring inaccuracies.

## Consequences

### Positive

- **Performant**: Anchor-based scoring with pre-projected positions runs well within 2ms budget even on low-end mobile
- **Decomposable**: Per-anchor scores directly feed Hint System's target selection algorithm (argmin anchorScore)
- **Tunable**: All parameters externalized to Luban tables — designers can adjust difficulty per-chapter without programmer involvement
- **Stable**: Temporal smoothing prevents score flickering during micro-adjustments, maintaining the "gentle, non-anxious" feel
- **Multiplicative safety**: Visibility check prevents invisible anchors from contributing phantom scores

### Negative

- **Anchor placement overhead**: Designers must manually place 1-3 PuzzleAnchors per object and define target positions — labor-intensive for 30+ puzzles
- **Indirect measurement**: Scoring is a proxy for visual similarity, not the actual visual. Edge cases where anchors align but the shadow shape differs slightly won't be caught
- **AsyncGPUReadback latency**: 1-3 frame delay means the visibility score reflects a slightly stale shadow state. Temporal smoothing mitigates but doesn't eliminate this

### Neutral

- Smoothing window (0.2s) adds intentional latency to NearMatch/PerfectMatch detection — this is a design feature (matches GDD's "NearMatch 判定 200ms" specification), not a technical limitation

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| AsyncGPUReadback unstable on low-end Android | MEDIUM | HIGH | Fallback to CPU readback (higher latency) or skip visibility check (use position/direction only) |
| Anchor placement errors produce incorrect scoring | MEDIUM | MEDIUM | Provide editor tooling to visualize anchor projections and score breakdown in real-time during level design |
| Temporal smoothing causes delayed PerfectMatch detection | LOW | MEDIUM | 0.2s window means max 0.2s delay; acceptable per GDD spec. Can reduce to 0.1s if too sluggish. |
| Per-chapter threshold tuning requires extensive playtesting | HIGH | LOW | Initial values from GDD's difficulty matrix; systematic playtest protocol with 5+ testers per chapter |
| HybridCLR AOT limitations in AsyncGPUReadback callback | LOW | HIGH | Sprint 0 spike: verify callback executes in hot-fix code correctly (Open Question #7 from architecture.md) |

## Performance Implications

| Metric | Expected | Budget | Notes |
|--------|----------|--------|-------|
| CPU (match calculation) | 0.3-0.8ms | ≤ 2.0ms (shared with ShadowRT processing) | 15 anchors × (projection + sampling + scoring) |
| CPU (ShadowRT readback processing) | 0.5-1.5ms | ≤ 1.5ms (TR-render-019) | NativeArray traversal, no GC |
| GPU (ShadowRT render) | Included in URP shadow budget | Covered by ADR-002 | ShadowSampleCamera orthographic render |
| Memory | ~1MB (ShadowRT) + ~2KB (anchor data) | Within 1.5GB ceiling | ShadowRT allocation managed by URP Shadow Rendering |
| Latency | 1-3 frames (AsyncGPUReadback) + 0.2s (smoothing) | Acceptable per GDD | Total: ~250ms from object move to stable score update |

## Validation Criteria

- [ ] Match calculation completes within 2ms on iPhone 13 Mini (Medium quality tier) with 15 anchors
- [ ] `AsyncGPUReadback` completes within 2 frames on iPhone 13 Mini; graceful fallback tested on Mali-G52
- [ ] PerfectMatch threshold (0.85) triggers correctly when designer-verified "correct" arrangement is placed
- [ ] NearMatch entry (0.40) and exit (0.35) hysteresis prevents flickering at boundary — verified with 5 min continuous operation
- [ ] Temporal smoothing eliminates visible score flickering during slow object drag (< 0.5 unit/s)
- [ ] All anchors occluded → matchScore forced to 0 (edge case coverage)
- [ ] Per-chapter threshold overrides from TbPuzzle correctly applied and produce expected difficulty progression
- [ ] Hint System can read `GetAnchorScores()` at 1s polling interval without performance impact
- [ ] Score reset on puzzle state change verified — no stale score carryover

## GDD Requirements Addressed

| GDD Document | Requirement | How This ADR Satisfies It |
|-------------|-------------|--------------------------|
| `shadow-puzzle-system.md` | TR-puzzle-001: Multi-anchor weighted scoring | Core algorithm: `matchScore = Σ(w_i × s_i) / Σ(w_i)` |
| `shadow-puzzle-system.md` | TR-puzzle-002: Per-anchor decomposition (position/direction/visibility) | Each anchor scored as `positionScore × directionScore × visibilityScore` |
| `shadow-puzzle-system.md` | TR-puzzle-010: ≤ 2ms match calculation budget | Algorithm designed for < 1ms anchor scoring + ≤ 1.5ms readback |
| `shadow-puzzle-system.md` | 0.2s sliding average for score smoothing | Exponential moving average with 0.2s window |
| `shadow-puzzle-system.md` | PerfectMatch threshold 0.85, NearMatch 0.40/0.35 hysteresis | Thresholds from Luban TbPuzzle, not hardcoded |
| `shadow-puzzle-system.md` | Cross-chapter difficulty matrix | Per-chapter overrides via TbPuzzle config fields |
| `urp-shadow-rendering.md` | TR-render-019: ShadowRT CPU readback ≤ 1.5ms | AsyncGPUReadback + NativeArray processing |
| `urp-shadow-rendering.md` | ShadowRT R8 grayscale format | Visibility threshold comparison on R8 (0-255) data |
| `hint-system.md` | TR-hint-007: Read-only query to Shadow Puzzle | `GetAnchorScores()` returns per-anchor breakdown without write access |

## Related

- **Depends On**: ADR-002 (URP Shadow Rendering) — provides ShadowRT, AsyncGPUReadback infrastructure, shadow quality tiers
- **Enables**: ADR-014 (Puzzle State Machine) — matchScore drives state transitions (Active↔NearMatch→PerfectMatch)
- **Enables**: ADR-015 (Hint System) — anchorScores feed hint target selection
- **References**: `architecture.md` §5.1 (Frame Update Path), §6.3 (IShadowPuzzle interface)
- **References**: `shadow-puzzle-system.md` (Formulas section, Cross-chapter difficulty matrix)
- **References**: `urp-shadow-rendering.md` (Shadow RT Sampling, AsyncGPUReadback)
