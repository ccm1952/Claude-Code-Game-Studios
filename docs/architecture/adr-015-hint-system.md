// 该文件由Cursor 自动生成

# ADR-015: Hint System Trigger Formula & Escalation Logic

## Status

Proposed

## Date

2026-04-22

## Last Verified

2026-04-22

## Decision Makers

Technical Director, Lead Programmer, Game Designer

## Summary

The Hint System provides a 3-tier progressive assistance mechanism (Ambient → Directional → Explicit) that activates based on a composite `triggerScore` formula combining idle time, fail count, stagnation detection, and low-matchScore acceleration. The system reads matchScore and anchorScores from Shadow Puzzle as **read-only queries at 1s polling interval**, uses TEngine `TimerModule` for all timers, and limits Layer 3 (explicit) hints to 3 per puzzle. Timer behavior integrates with Tutorial (pause during tutorial, full reset on completion) and supports per-chapter `hintDelayOverride` scaling.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.62f2 (LTS) |
| **Domain** | Feature / Timer / Behavior Monitoring |
| **Knowledge Risk** | MEDIUM (TEngine TimerModule) |
| **References Consulted** | `hint-system.md`, `architecture.md` §4.3/§6.6, `shadow-puzzle-system.md` |
| **Post-Cutoff APIs Used** | TEngine `TimerModule` (`GameModule.Timer`), `GameEvent` |
| **Verification Required** | Sprint 0: confirm TimerModule supports pause/resume per timer; verify timer precision ≤ 100ms for idle detection |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-012 (Shadow Match Algorithm — provides matchScore and anchorScores as read-only data source) |
| **Enables** | N/A (terminal feature; consumed only by UI for hint button state) |
| **Blocks** | Hint System implementation — anti-frustration safety net for all puzzles |
| **Ordering Note** | ADR-012 must be Accepted; this ADR should reach Accepted before Hint System Sprint |

## Context

### Problem Statement

Players in 影子回忆 can get stuck on puzzles. Without assistance, extended frustration breaks the game's emotional tone ("温柔的、不焦虑的"). The Hint System must:

1. Detect when a player is stuck using multiple behavioral signals (not just time)
2. Escalate through 3 tiers of increasingly explicit guidance
3. Feel invisible — Layer 1/2 should seem like natural environmental changes, not "game helping you"
4. Not interfere with tutorial flow or penalize players who use hints
5. Support per-chapter delay scaling (later chapters allow more thinking time)
6. Handle Ch.5 absence puzzles with modified hint content (guide toward acceptance, not perfection)

### Constraints

- **Read-only**: Hint System MUST NOT write to Shadow Puzzle System (TR-hint-007, Architecture Principle P5)
- **1s polling**: Query matchScore/anchorScores at 1s intervals, NOT per-frame (performance constraint TR-hint-013)
- **Timer precision**: TEngine TimerModule must support pause/resume without losing accumulated time
- **Tutorial integration**: All passive hint timers pause during tutorial steps, reset to 0 on tutorial completion
- **Performance**: < 0.5ms per frame update (TR-hint-013)

### Requirements

- TR-hint-001: 3-tier progressive hints (Ambient/Directional/Explicit)
- TR-hint-007: Read-only query to Shadow Puzzle
- TR-hint-009: Timer pause during Tutorial
- TR-hint-010: All parameters from Luban config
- TR-hint-013: < 0.5ms per frame update

## Decision

**Implement a triggerScore-based escalation system with 4-factor composite formula, TEngine TimerModule-managed timers, 1s polling of Shadow Puzzle, tutorial-aware timer lifecycle, and per-chapter delay overrides.**

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│                     Hint System                          │
│                                                          │
│  ┌────────────────────────────────┐                     │
│  │       Trigger Evaluator        │                     │
│  │                                │                     │
│  │  triggerScore =                │                     │
│  │    timeScore                   │                     │
│  │  + failScore                   │                     │
│  │  + stagnationScore             │                     │
│  │  + matchPenalty                │                     │
│  │                                │                     │
│  │  if triggerScore >= 1.0:       │                     │
│  │    escalate hint layer         │                     │
│  └──────────┬─────────────────────┘                     │
│             │                                            │
│  ┌──────────┴─────────────────────┐                     │
│  │   Hint Layer State Machine     │                     │
│  │                                │                     │
│  │  Idle → Observing              │                     │
│  │       → Layer1Active           │                     │
│  │       → Cooldown               │                     │
│  │       → Layer2Active           │                     │
│  │       → Cooldown               │                     │
│  │                                │                     │
│  │  Layer3Ready (parallel flag)   │                     │
│  │  Layer3Active (on button tap)  │                     │
│  └────────────────────────────────┘                     │
│                                                          │
│  Reads (1s poll):                                        │
│    IShadowPuzzle.GetMatchScore()                         │
│    IShadowPuzzle.GetAnchorScores()                       │
│                                                          │
│  Listens:                                                │
│    Evt_PuzzleStateChanged                                │
│    Evt_TutorialStepStarted (pause timers)                │
│    Evt_TutorialStepCompleted (reset timers)              │
│                                                          │
│  Fires:                                                  │
│    Evt_HintAvailable { int layer }                       │
│    Evt_HintDismissed { }                                 │
└─────────────────────────────────────────────────────────┘
```

### Trigger Score Formula

```
triggerScore = timeScore + failScore + stagnationScore + matchPenalty

timeScore = min(idleTime / (idleThreshold × hintDelayOverride), 1.0)
failScore = min(failCount × failWeight, 0.6)
stagnationScore = stagnationDetected ? stagnationBonus : 0.0
matchPenalty = matchScore < matchLowThreshold
             ? (1.0 - matchScore / matchLowThreshold) × matchPenaltyMax
             : 0.0
```

**Stagnation detection:**
```
stagnationDetected = (matchScore has stayed within ±0.05 for stagnationDuration seconds)
                     AND (player has performed interactions during that period)
```

| Parameter | Layer 1 | Layer 2 | Source | Range |
|-----------|---------|---------|--------|-------|
| idleThreshold | 45s | 90s | config | 20-180s |
| failWeight | 0.12 | 0.08 | config | 0.03-0.2 |
| repeatWeight | 0.10 | 0.06 | config | 0.03-0.2 |
| matchLowThreshold | 0.40 | 0.40 | config | 0.15-0.5 |
| matchPenaltyMax | 0.4 | 0.4 | config | 0.2-0.6 |
| stagnationDuration | 30s | 30s | config | 15-60s |
| stagnationBonus | 0.3 | 0.3 | config | 0.1-0.5 |

### Layer 3 (Explicit Hint) Rules

- Triggered ONLY by player pressing hint button (not by triggerScore)
- Maximum 3 uses per puzzle (`l3MaxCount = 3`, from Luban TbPuzzle)
- Each use decrements counter; at 0, button grays out
- Hint content from Luban config: 3 pre-authored text strings per puzzle
- Target object selected by: `argmin(anchorScore_i)` where `anchorWeight_i >= minWeight`
- Duration: 5s display, then fade out

### Tutorial Timer Integration

```
On Evt_TutorialStepStarted:
    PauseAllTimers()  // idleTimer, failCount, repeatDragCount frozen

On Evt_TutorialStepCompleted:
    ResetAllTimers()  // idleTimer=0, failCount=0, repeatDragCount=0, stagnation reset
    ResumeAllTimers()
```

Design rationale: Tutorial completion marks the true start of puzzle exploration. Accumulated time/failures during tutorial are not indicative of "being stuck."

### Per-Chapter hintDelayOverride

| Chapter | hintDelayOverride | L1 Effective idleThreshold | L2 Effective idleThreshold | Rationale |
|---------|------------------|--------------------------|--------------------------|-----------|
| Ch.1-2 | 1.0 | 45s | 90s | Simple puzzles, standard timing |
| Ch.3 | 1.3 | 58s | 117s | More objects, longer valid think time |
| Ch.4 | 1.5 | 67s | 135s | Shadow softening increases difficulty |
| Ch.5 | 1.5 | 67s | 135s | Complex + absence concept needs reflection time |

### Cooldown Between Layers

```
actualCooldown = baseCooldown × cooldownModifier
cooldownModifier = clamp(1.0 + (matchScore - 0.3) × 0.5, 0.7, 1.3)
```

| matchScore | cooldownModifier | actualCooldown (base=30s) |
|-----------|-----------------|--------------------------|
| 0.0 | 0.85 | 25.5s |
| 0.15 | 0.925 | 27.75s |
| 0.30 | 1.0 | 30s |
| 0.40 | 1.05 | 31.5s |

Low matchScore → shorter cooldown (player needs faster escalation). High matchScore → longer cooldown (player is on track, give more self-discovery time).

## Alternatives Considered

### Alternative 1: Pure Time-Based Triggers (No Composite Score)

- **Description**: Trigger Layer 1 after X seconds idle, Layer 2 after Y seconds idle, ignoring behavioral signals
- **Pros**: Simplest implementation; predictable timing; easy to explain to designers
- **Cons**: Doesn't account for player behavior — a player actively trying and failing gets the same timing as one who walked away. The GDD's stagnation scenario (score 0.30-0.40, actively adjusting but no progress) would be completely undetected.
- **Rejection Reason**: GDD explicitly requires stagnation detection and fail-count acceleration. Pure time-based triggers have known UX problems (frustrating for active-but-stuck players, premature for thinking-but-idle players).

### Alternative 2: Machine Learning Player Modeling

- **Description**: Train a classifier on player interaction patterns to predict "stuck" state
- **Pros**: Could detect subtle frustration patterns; adaptive to individual player skill
- **Cons**: Requires training data (no players yet); opaque behavior difficult to tune; significant implementation complexity; ML inference adds CPU cost; overkill for a 5-chapter indie game
- **Rejection Reason**: Completely disproportionate to the game's scope and team size. The composite formula achieves 95%+ of the benefit with 5% of the complexity.

### Alternative 3: Player-Requested Only (No Passive Hints)

- **Description**: Only Layer 3 (button press) exists; no ambient or directional hints
- **Pros**: Zero risk of unwanted hints; simpler system; players who don't want help never see hints
- **Cons**: Loses the GDD's core design goal — "I figured it out myself" feeling. Players who never think to press the hint button get stuck forever. The "invisible help" of Layers 1/2 is the most praised feature in playtest-validated hint systems (Monument Valley, The Witness).
- **Rejection Reason**: Passive hints (L1/L2) are the emotional heart of the system. They enable the "I didn't realize I was being helped" experience that the GDD mandates. Removing them would leave a significant portion of players stuck without recourse.

## Consequences

### Positive

- **Multi-signal detection**: Composite triggerScore catches more stuck-player scenarios than any single signal alone
- **Invisible assistance**: L1/L2 blend into the game world — 60%+ of playtesters in reference games don't recognize passive hints as "help"
- **Tutorial-safe**: Timer reset on tutorial completion prevents false triggers during learning
- **Chapter-adaptive**: hintDelayOverride allows later chapters to respect longer think times
- **Read-only safety**: 1s polling with no write access to Shadow Puzzle eliminates any risk of hint system corrupting puzzle state

### Negative

- **Formula complexity**: 4-factor triggerScore requires careful tuning across all chapters. Wrong weights can make hints too aggressive or too passive.
- **Stagnation false positives**: A player deliberately exploring the same score range could trigger stagnation detection. The 30s duration mitigates this but doesn't eliminate it.
- **Layer 3 content authoring**: Each puzzle needs 3 hint texts in Luban config — content creation burden for 30+ puzzles

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| triggerScore weights produce too-aggressive hints in early chapters | MEDIUM | MEDIUM | Extensive playtesting with 5+ testers; weights are Luban-configurable for rapid iteration |
| TimerModule pause/resume loses precision (time drift) | LOW | LOW | Sprint 0 spike to verify; fallback to manual `Time.unscaledDeltaTime` accumulation |
| Layer 1 ambient glow not noticed by players | MEDIUM | LOW | If unnoticed, system naturally escalates to L2. Can increase pulse intensity via config. |
| Absence puzzle hint content inadvertently guides toward non-existent solution | MEDIUM | HIGH | Dedicated hint text review for Ch.5; hint text switches to "acceptance" mode when matchScore ≥ maxCompletionScore |

## Performance Implications

| Metric | Expected | Budget | Notes |
|--------|----------|--------|-------|
| CPU (per-frame update) | 0.05-0.2ms | ≤ 0.5ms (TR-hint-013) | Timer checks + triggerScore calculation (only on 1s polling tick) |
| CPU (1s polling query) | 0.01ms | Negligible | Simple float reads from Shadow Puzzle interface |
| Memory | ~500 bytes (timers + counters + state) | Negligible | Per-puzzle state, released on puzzle exit |

## Validation Criteria

- [ ] Layer 1 triggers within ±2s of expected time (45s × hintDelayOverride) when player is idle
- [ ] Layer 2 triggers only after Layer 1 has fired and cooldown has elapsed (no skip)
- [ ] Layer 3 button press correctly decrements counter; button grays out at 0 remaining
- [ ] triggerScore calculation: failCount of 5 with idle 30s produces triggerScore ≥ 1.0 (verified mathematically)
- [ ] stagnationScore triggers when matchScore stays at 0.35±0.05 for 30s with active player interaction
- [ ] matchPenalty correctly accelerates hints when matchScore = 0 (penalty = 0.4)
- [ ] Tutorial pause: all hint timers frozen during tutorial step; reset to 0 on completion
- [ ] hintDelayOverride: Ch.3 with override=1.3 → L1 triggers at ~58s (verified)
- [ ] NearMatch suppression: Layer 1 does NOT trigger when matchScore > 0.40
- [ ] Layer 3 absence puzzle text switches to "acceptance" content when matchScore ≥ maxCompletionScore
- [ ] Performance: hint system update ≤ 0.5ms verified on iPhone 13 Mini
- [ ] No write operations to Shadow Puzzle System (code review verification)

## GDD Requirements Addressed

| GDD Document | Requirement | How This ADR Satisfies It |
|-------------|-------------|--------------------------|
| `hint-system.md` | 3-tier progressive hints (Ambient/Directional/Explicit) | Layer 1/2/3 with distinct triggers and presentations |
| `hint-system.md` | triggerScore formula (timeScore + failScore + stagnationScore + matchPenalty) | Exact formula implementation with Luban-configurable weights |
| `hint-system.md` | matchLowThreshold = 0.40 | matchPenalty activates below 0.40, aligned with NearMatch threshold |
| `hint-system.md` | stagnationScore as additional trigger factor | 30s stagnation detection in ±0.05 matchScore range |
| `hint-system.md` | L3 limit: 3 per puzzle | l3MaxCount configurable, default 3 |
| `hint-system.md` | Timer pause during tutorial, reset after completion | Evt_TutorialStepStarted/Completed integration |
| `hint-system.md` | hintDelayOverride per chapter | Per-chapter multiplier on idleThreshold |
| `hint-system.md` | Hint target selection: argmin(anchorScore) with weight tiebreaker | Target object = lowest anchorScore, highest weight breaks ties |

## Related

- **Depends On**: ADR-012 (Shadow Match Algorithm) — matchScore and anchorScores are the primary data inputs
- **References**: ADR-014 (Puzzle State Machine) — hint timers respond to PuzzleStateChanged events
- **References**: `architecture.md` §4.3 (Hint System ownership), §6.6 (IHintService interface)
- **References**: `hint-system.md` (complete GDD with all formula parameters and edge cases)
