// 该文件由Cursor 自动生成

# ADR-014: Puzzle State Machine & Absence Puzzle Variant

## Status

Accepted (Promoted 2026-05-06 — bulk ceremony post Sprint 3 closure / ADR-029 V2.0 review B-1; required to unblock Puzzle State Machine implementation)

## Date

2026-04-22

## Last Verified

2026-04-22

## Decision Makers

Technical Director, Lead Programmer, Game Designer

## Summary

The Shadow Puzzle System requires a formal state machine governing each puzzle's lifecycle from locked to complete, including the Ch.5-specific "absence puzzle" variant where no perfect solution exists. We adopt a **7-state FSM** (Locked → Idle → Active → NearMatch → PerfectMatch → AbsenceAccepted → Complete) where PerfectMatch and AbsenceAccepted are **irreversible terminal transitions** that freeze matchScore calculation. The absence variant uses a `absenceAcceptDelay` timer (5s of inactivity at `≥ maxCompletionScore`) to detect player acceptance. A `tutorialGracePeriod` (3s) prevents premature PerfectMatch triggers immediately after tutorial completion.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.62f2 (LTS) |
| **Domain** | Gameplay / State Machine / Timer |
| **Knowledge Risk** | MEDIUM (TEngine FsmModule, TimerModule) |
| **References Consulted** | `shadow-puzzle-system.md`, `chapter-state-and-save.md`, `architecture.md` §4.3/§6.3/§6.4 |
| **Post-Cutoff APIs Used** | TEngine `FsmModule`, `TimerModule`, `GameEvent` |
| **Verification Required** | Sprint 0: confirm FsmModule supports 7-state machines with conditional guards; verify TimerModule precision for 5s absence delay |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-008 (Save System — puzzle states persisted via IChapterProgress), ADR-012 (Shadow Match — provides matchScore that drives transitions) |
| **Enables** | ADR-015 (Hint System — monitors puzzle state for timer control), ADR-016 (Narrative — triggered by PerfectMatch/AbsenceAccepted events) |
| **Blocks** | Core gameplay loop — without puzzle state transitions, puzzles cannot be completed |
| **Ordering Note** | ADR-008 and ADR-012 must be Accepted first |

## Context

### Problem Statement

Each puzzle in 影子回忆 has a lifecycle that must be precisely managed:

1. **Standard puzzles (Ch.1-4)**: Player manipulates objects until matchScore ≥ 0.85, triggering PerfectMatch → locking → narrative → complete
2. **Absence puzzles (Ch.5)**: No arrangement can reach 0.85; instead, when matchScore stabilizes at `≥ maxCompletionScore` (0.60-0.70) and the player stops interacting for 5s, the system accepts the "imperfect" result
3. **Tutorial integration**: After a tutorial step completes, a 3s grace period prevents accidental PerfectMatch while the player is still in "learning mode"
4. **Irreversibility**: Once PerfectMatch or AbsenceAccepted is reached, the matchScore calculation must freeze — no amount of subsequent object movement can alter the outcome
5. **Cross-chapter difficulty**: Thresholds vary per chapter and per puzzle via Luban config

### Constraints

- State machine must be serializable (via IChapterProgress) for save/load — Active-state puzzles resume correctly after app restart
- PerfectMatch is a one-way door — once entered, matchScore calculation stops (Architecture §5.2)
- Absence puzzles must feel like "acceptance, not failure" — the delay timer distinguishes "still adjusting" from "has accepted"
- Tutorial grace period must not interfere with normal gameplay once expired

### Requirements

- TR-puzzle-005: Full puzzle state machine with all defined states
- TR-puzzle-006: AbsenceAccepted for Ch.5 absence puzzles
- TR-puzzle-014: All state thresholds from Luban config
- TR-save-005: State persistence via IChapterProgress

## Decision

**Implement a 7-state puzzle FSM using TEngine FsmModule with irreversible PerfectMatch/AbsenceAccepted terminals, absence delay timer, tutorial grace period, and cross-chapter difficulty overrides.**

### Architecture

```
                    ┌────────────────────────────────────────────┐
                    │           Puzzle State Machine               │
                    │           (per active puzzle)                │
                    │                                              │
  ┌──────┐  前序Complete  ┌──────┐  首次操作  ┌────────┐          │
  │Locked│ ─────────────→ │ Idle │ ─────────→ │ Active │          │
  └──────┘                └──────┘            └───┬────┘          │
                                                  │               │
                              matchScore ≥ 0.40   │               │
                                    ┌─────────────┘               │
                                    ▼                             │
                              ┌───────────┐                       │
                              │ NearMatch │←─── matchScore < 0.35 │
                              └─────┬─────┘     (hysteresis)      │
                                    │                             │
                    ┌───────────────┼───────────────┐             │
                    │               │               │             │
          [standard puzzle]  [absence puzzle]       │             │
          matchScore ≥ 0.85  idle ≥ 5s at           │             │
                    │        ≥ maxCompletionScore    │             │
                    ▼               ▼               │             │
            ┌──────────────┐ ┌──────────────────┐   │             │
            │ PerfectMatch │ │ AbsenceAccepted  │   │             │
            │ (freeze score│ │ (freeze score,   │   │             │
            │  irreversible│ │  irreversible)   │   │             │
            └──────┬───────┘ └────────┬─────────┘   │             │
                   │                  │              │             │
                   └──────┬───────────┘              │             │
                          ▼                          │             │
                    ┌──────────┐                     │             │
                    │ Complete │                      │             │
                    └──────────┘                     │             │
                                                    │             │
          tutorialGracePeriod (3s) blocks            │             │
          PerfectMatch/AbsenceAccepted transitions ──┘             │
                    │                                              │
                    └──────────────────────────────────────────────┘
```

### Key Interfaces

```csharp
public enum PuzzleState
{
    Locked, Idle, Active, NearMatch, PerfectMatch, AbsenceAccepted, Complete
}

public interface IPuzzleStateMachine
{
    PuzzleState CurrentState { get; }
    float MatchScore { get; }
    bool IsInGracePeriod { get; }
    bool IsAbsencePuzzle { get; }

    // Transition triggers (called by ShadowMatchCalculator)
    void OnMatchScoreUpdated(float newScore);
    void OnPlayerInteraction();

    // External triggers
    void OnTutorialCompleted();  // starts grace period
    void OnSnapAnimationComplete();  // PerfectMatch → Complete
    void OnAbsenceSequenceComplete();  // AbsenceAccepted → Complete
}
```

### State Transition Details

**Active → NearMatch:**
- Guard: `matchScore ≥ nearMatchThreshold` (default 0.40, per-puzzle override via TbPuzzle)
- Action: Fire `Evt_NearMatchEnter`, enable shadow glow effect

**NearMatch → Active (hysteresis):**
- Guard: `matchScore < nearMatchThreshold - 0.05` (default 0.35)
- Action: Fire `Evt_NearMatchExit`, disable shadow glow

**NearMatch/Active → PerfectMatch (standard puzzle):**
- Guard: `!isAbsencePuzzle && matchScore ≥ perfectMatchThreshold && !isInGracePeriod`
- Action: Freeze matchScore calculation; fire `Evt_PerfectMatch`; fire `Evt_PuzzleLockAll`

**NearMatch/Active → AbsenceAccepted (absence puzzle):**
- Guard: `isAbsencePuzzle && matchScore ≥ maxCompletionScore && idleTime ≥ absenceAcceptDelay && !isInGracePeriod`
- Action: Freeze matchScore calculation; fire `Evt_AbsenceAccepted`; fire `Evt_PuzzleLockAll`

**PerfectMatch → Complete:**
- Guard: Snap animation + narrative sequence finished
- Action: Fire `Evt_PuzzleComplete`; Chapter State advances progression

**AbsenceAccepted → Complete:**
- Guard: Absence narrative sequence finished
- Action: Fire `Evt_PuzzleComplete`; Chapter State advances progression

### Tutorial Grace Period

```
On TutorialStepCompleted event:
    gracePeriodTimer = tutorialGracePeriod (default 3.0s)
    isInGracePeriod = true

Each frame while isInGracePeriod:
    gracePeriodTimer -= Time.deltaTime
    if gracePeriodTimer <= 0:
        isInGracePeriod = false
```

During grace period: matchScore continues to be calculated and displayed, but PerfectMatch/AbsenceAccepted transitions are blocked. This prevents the scenario where a tutorial's guided actions accidentally place objects in the solution position.

### Absence Puzzle Idle Detection

```
On each frame while isAbsencePuzzle && state == Active/NearMatch:
    if playerInteractedThisFrame:
        absenceIdleTimer = 0
    else if matchScore >= maxCompletionScore:
        absenceIdleTimer += Time.deltaTime
        if absenceIdleTimer >= absenceAcceptDelay:
            transition → AbsenceAccepted
    else:
        absenceIdleTimer = 0
```

| Parameter | Default | Source | Range |
|-----------|---------|--------|-------|
| absenceAcceptDelay | 5.0s | TbPuzzle | 3-8s |
| maxCompletionScore | 0.60-0.70 | TbPuzzle (per absence puzzle) | 0.50-0.80 |
| tutorialGracePeriod | 3.0s | config | 1-5s |

### Cross-Chapter Difficulty Matrix

| Chapter | nearMatchThreshold | perfectMatchThreshold | Notes |
|---------|-------------------|----------------------|-------|
| Ch.1 | 0.40 | 0.85 | Standard — clear shadows, simple puzzles |
| Ch.2 | 0.40 | 0.85 | Standard — introduces light control |
| Ch.3 | 0.40 | 0.85 | Standard — more objects |
| Ch.4 | 0.35 | 0.80 | Shadow style softening compensated by lower thresholds |
| Ch.5 (standard) | 0.35 | 0.78 | Further compensation for heavy penumbra |
| Ch.5 (absence) | 0.35 | N/A (uses maxCompletionScore 0.60-0.70) | Absence puzzles never reach perfectMatchThreshold |

## Alternatives Considered

### Alternative 1: Timed PerfectMatch (Hold Above Threshold for N Seconds)

- **Description**: Require matchScore to remain above perfectMatchThreshold for a sustained duration (e.g., 1s) before triggering PerfectMatch, similar to absence puzzles
- **Pros**: Prevents accidental PerfectMatch from brief score spikes during drag; more consistent with absence puzzle pattern
- **Cons**: Adds latency to the most satisfying moment in the game; temporal smoothing (0.2s) already handles score spikes; makes the "aha moment" feel sluggish rather than instant
- **Rejection Reason**: The GDD explicitly states PerfectMatch should trigger when matchScore crosses threshold (after smoothing), not after a hold period. The 0.2s temporal smoothing in ADR-012 already prevents spike-triggered false positives.

### Alternative 2: No Separate AbsenceAccepted State (Use PerfectMatch with Lower Threshold)

- **Description**: For absence puzzles, simply set perfectMatchThreshold to maxCompletionScore and use the standard PerfectMatch path
- **Pros**: Simpler state machine (6 states instead of 7); no special-case logic
- **Cons**: Loses the crucial "acceptance" mechanic — the 5s idle delay is the core emotional beat of Ch.5 ("player choosing to stop trying"). Without it, absence puzzles feel like easier standard puzzles rather than thematic statements about loss.
- **Rejection Reason**: The absence delay is a core design feature, not a technical convenience. It distinguishes "I reached the threshold while still adjusting" from "I've accepted that this is all I can achieve." This is the emotional climax of Chapter 5.

### Alternative 3: Separate State Machine for Absence Puzzles

- **Description**: Create a completely separate FSM for absence puzzles with its own states and transitions
- **Pros**: Clean separation; no conditional branching in the standard FSM
- **Cons**: Code duplication (Active, NearMatch, Complete states are identical); maintenance burden doubles; two FSMs to test and debug; violates DRY
- **Rejection Reason**: The standard and absence paths share 5 of 7 states. A single FSM with one conditional guard (`isAbsencePuzzle`) is cleaner than maintaining two parallel machines.

## Consequences

### Positive

- **Complete lifecycle coverage**: All 7 states map directly to GDD specifications — no gaps or ambiguities
- **Irreversible terminals**: PerfectMatch/AbsenceAccepted freezing matchScore prevents the "undo success" anti-pattern
- **Emotional design support**: Absence delay timer enables the Ch.5 "acceptance" mechanic without code changes — just config
- **Tutorial safety**: Grace period prevents frustrating accidental completions during learning
- **Save-compatible**: PuzzleState enum maps directly to IChapterProgress serialization

### Negative

- **7-state complexity**: More states = more transitions to test. Total transition count: 10 (manageable but requires thorough test coverage)
- **Absence detection fragility**: The 5s idle timer could trigger falsely if a player pauses to think (not interacting but not yet "accepting"). Mitigation: 5s is deliberately longer than typical thinking pauses (2-3s); can be tuned via config
- **Grace period edge case**: If a player solves the puzzle during the grace period, they must wait up to 3s for the transition. Mitigation: Grace period only starts after tutorial — by that point, players are unlikely to instantly solve

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| Absence idle timer triggers during legitimate "thinking" pause | MEDIUM | MEDIUM | 5s delay is generous; playtest with 10+ testers to validate. Adjustable via `absenceAcceptDelay` config. |
| Tutorial grace period feels like "input lag" to skilled players | LOW | LOW | Only affects tutorials (first few minutes); 3s is brief. Can reduce to 1s if feedback indicates frustration. |
| Save/load during PerfectMatch snap animation causes inconsistent state | LOW | HIGH | Save persists PuzzleState as PerfectMatch; on load, skip to Complete if narrative sequence was in progress |
| FsmModule doesn't support conditional transition guards | MEDIUM | MEDIUM | Implement guards in the state's OnUpdate callback with manual `ChangeState<>()` calls |

## Performance Implications

| Metric | Expected | Budget | Notes |
|--------|----------|--------|-------|
| CPU (state machine update per puzzle) | < 0.05ms | Part of 2ms match budget | FSM state check + timer update |
| CPU (absence idle detection) | < 0.01ms | Negligible | Simple float comparison per frame |
| Memory | ~200 bytes per puzzle state | Negligible | Enum + timers + flags |

## Validation Criteria

- [ ] All 7 states reachable and all 10 transitions exercisable in automated test
- [ ] PerfectMatch at 0.85 triggers correctly — verified with designer-approved object placement
- [ ] PerfectMatch is irreversible — moving objects after PerfectMatch does not change frozen score
- [ ] AbsenceAccepted triggers after 5s idle at ≥ maxCompletionScore — verified with timer precision ±0.1s
- [ ] AbsenceAccepted does NOT trigger if player interacts within the 5s window (timer resets)
- [ ] Tutorial grace period blocks PerfectMatch for exactly 3s after tutorial completion
- [ ] NearMatch → Active hysteresis: score oscillating at 0.38-0.42 does not cause rapid state flicker
- [ ] Save during PerfectMatch state → load → resumes correctly (either at PerfectMatch awaiting narrative, or Complete)
- [ ] Per-chapter threshold overrides from TbPuzzle applied correctly
- [ ] Absence puzzle with maxCompletionScore=0.65 never triggers standard PerfectMatch path

## GDD Requirements Addressed

| GDD Document | Requirement | How This ADR Satisfies It |
|-------------|-------------|--------------------------|
| `shadow-puzzle-system.md` | 6-state puzzle FSM + AbsenceAccepted | 7-state FSM (Locked/Idle/Active/NearMatch/PerfectMatch/AbsenceAccepted/Complete) |
| `shadow-puzzle-system.md` | PerfectMatch irreversible (freeze matchScore) | Explicit freeze on PerfectMatch/AbsenceAccepted entry |
| `shadow-puzzle-system.md` | AbsenceAccepted for Ch.5 (5s idle at maxCompletionScore) | absenceAcceptDelay timer with interaction reset |
| `shadow-puzzle-system.md` | tutorialGracePeriod (3s buffer) | Grace period flag blocks terminal transitions |
| `shadow-puzzle-system.md` | Cross-chapter difficulty matrix | Per-puzzle threshold overrides via TbPuzzle |
| `shadow-puzzle-system.md` | NearMatch hysteresis (0.40 entry / 0.35 exit) | 5% hysteresis band configurable per puzzle |
| `chapter-state-and-save.md` | Puzzle state persistence via IChapterProgress | PuzzleState enum directly serializable |

## Related

- **Depends On**: ADR-008 (Save System) — serialization of puzzle state
- **Depends On**: ADR-012 (Shadow Match Algorithm) — matchScore that drives all transitions
- **Enables**: ADR-015 (Hint System) — monitors PuzzleState for timer pause/resume
- **Enables**: ADR-016 (Narrative Sequence Engine) — PerfectMatch/AbsenceAccepted events trigger sequences
- **References**: `architecture.md` §4.3 (Shadow Puzzle System ownership), §5.2 (Puzzle Complete Flow)

---

## Implementation Expand (Sprint 4 S4-01 — 2026-05-06)

> **Source**: Sprint 4 plan Track A first story；本节为 ADR-014 v1 (2026-04-22) 的 Sprint 4 implementation alignment update：(a) ADR-027 interface protocol 替换 legacy const-int；(b) ADR-029 V2.0 R3 mandatory + §V2-5 framework boundary probe coverage；(c) Sprint 4 implementation file paths；(d) 首批 story 创建索引。

### A. ADR-027 Interface Protocol Mapping (replaces legacy `Evt_*` const-int)

ADR-014 v1 文中事件采用 `Evt_NearMatchEnter` / `Evt_PerfectMatch` / `Evt_PuzzleComplete` 等 ADR-006 const-int 命名（已 superseded by ADR-027 2026-04-23）。本节定义 ADR-027 接口协议下的 `IShadowPuzzleEvent`：

```csharp
namespace GameLogic
{
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IShadowPuzzleEvent
    {
        /// <summary>NearMatch 状态进入（matchScore 跨 nearMatchThreshold）。Sender: PuzzleStateMachine。
        /// Listener: ShadowGlowEffect / HintSystem / AudioFeedback。</summary>
        void OnNearMatchEnter(int puzzleId);

        /// <summary>NearMatch 状态退出（hysteresis：matchScore 跌破 nearMatchThreshold - 0.05）。</summary>
        void OnNearMatchExit(int puzzleId);

        /// <summary>PerfectMatch — 标准 puzzle 完成路径（matchScore ≥ perfectMatchThreshold；不可逆）。
        /// Cascade: 触发 IPuzzleLockEvent.OnPuzzleLockAll + INarrativeEvent.OnRequestSequence(puzzleId, "perfect")。</summary>
        void OnPerfectMatch(int puzzleId, float finalMatchScore);

        /// <summary>AbsenceAccepted — Ch.5 absence puzzle 完成路径（idle ≥ absenceAcceptDelay at ≥ maxCompletionScore；不可逆）。
        /// Cascade: 触发 IPuzzleLockEvent.OnPuzzleLockAll + INarrativeEvent.OnRequestSequence(puzzleId, "absence")。</summary>
        void OnAbsenceAccepted(int puzzleId, float finalMatchScore);

        /// <summary>PuzzleComplete — narrative sequence 完成后 Chapter State 推进进度。
        /// Sender: PuzzleStateMachine OnSnap/AbsenceSequenceComplete。Listener: ChapterStateManager / SaveManager。</summary>
        void OnPuzzleComplete(int puzzleId, PuzzleCompletionType completionType);
    }

    public enum PuzzleCompletionType
    {
        Perfect = 0,
        Absence = 1,
    }
}
```

**Migration table** (legacy → ADR-027)：

| Legacy `Evt_*` | ADR-027 Interface Method |
|---------------|--------------------------|
| `Evt_NearMatchEnter` | `IShadowPuzzleEvent.OnNearMatchEnter(int puzzleId)` |
| `Evt_NearMatchExit` | `IShadowPuzzleEvent.OnNearMatchExit(int puzzleId)` |
| `Evt_PerfectMatch` | `IShadowPuzzleEvent.OnPerfectMatch(int puzzleId, float finalMatchScore)` |
| `Evt_AbsenceAccepted` | `IShadowPuzzleEvent.OnAbsenceAccepted(int puzzleId, float finalMatchScore)` |
| `Evt_PuzzleComplete` | `IShadowPuzzleEvent.OnPuzzleComplete(int puzzleId, PuzzleCompletionType)` |
| `Evt_PuzzleLockAll` | `IPuzzleLockEvent.OnPuzzleLockAll()` (existing — ADR-013) |

**ADR-027 §5 ⚠️ Framework knowledge fact applies**: 任何订阅 `IShadowPuzzleEvent` 方法的 listener 必须 handler 内 self-remove + `_handler = null` (null-out) + 外部 cleanup `if (_handler != null) RemoveEventListener(...)` (null-check guard)。详见 ADR-027 §5。

### B. Production Code Paths

```
Assets/GameScripts/HotFix/GameLogic/
├── ShadowPuzzle/                                       # 新建目录 (Sprint 4 起)
│   ├── PuzzleStateMachine.cs                           # 7-state FSM 主体（IPuzzleStateMachine impl）
│   ├── PuzzleState.cs                                  # PuzzleState enum + PuzzleCompletionType enum
│   ├── PuzzleConfig.cs                                 # readonly POCO (TbPuzzle 投影；perfectMatchThreshold / nearMatchThreshold / absenceAcceptDelay / maxCompletionScore / isAbsencePuzzle)
│   └── PuzzleConfigFromLuban.cs                        # IPuzzleConfigProvider impl 读 ConfigSystem.Tables.TbPuzzle
└── IEvent/
    └── IShadowPuzzleEvent.cs                           # 新建 (5 method + EEventGroup.GroupLogic)
```

**Initialize / Shutdown lifecycle pattern** (per ADR-013 v3 教训 + Sprint 2 retro)：

```csharp
public sealed class PuzzleStateMachine : IPuzzleStateMachine
{
    private Action<int> _onMatchScoreUpdated;  // ADR-027 §5 null-out + null-check guard
    // ... fields ...

    public void Initialize(int puzzleId, PuzzleConfig config)
    {
        // 注册 listener using ADR-027 per-event pattern
        _onMatchScoreUpdated = OnMatchScoreUpdatedHandler;
        GameEvent.AddEventListener<int, float>(IShadowMatchEvent_Event.OnMatchScoreUpdated, _onMatchScoreUpdated);
        // FSM init to PuzzleState.Locked (then Chapter State unlocks 通过 OnPuzzleUnlock event)
    }

    public void Shutdown()
    {
        // Cleanup with null-check guard (ADR-027 §5 framework knowledge fact)
        if (_onMatchScoreUpdated != null)
        {
            GameEvent.RemoveEventListener<int, float>(IShadowMatchEvent_Event.OnMatchScoreUpdated, _onMatchScoreUpdated);
            _onMatchScoreUpdated = null;
        }
        // Stop timers / unfreeze / persist current state via IChapterProgress
    }
}
```

### C. ADR-029 V2.0 R3 Mandatory Coverage (R3 PlayMode probe requirements)

Per ADR-029 V2.0 §V2-3 R3 mandatory + §V2-5 framework boundary behavior probe checklist：

| 必备 R3 case | 触发 case | spike 路径 |
|-------------|----------|-----------|
| **State machine 7 transitions** (业务 happy path) | 完整跑 Locked → Idle → Active → NearMatch → PerfectMatch → Complete (standard); 单独跑 absence 路径 | `S401_PuzzleStateMachine.cs` IDevSpike P1/P2 |
| **Hysteresis edge** (业务) | matchScore 振荡在 0.38-0.42 不导致 NearMatch ↔ Active 反复 | P3 |
| **Irreversibility** (业务) | PerfectMatch 后 score 变化不影响 frozen state | P4 |
| **Tutorial grace period** (业务) | 3s 内 PerfectMatch transition 被 block + 3s 后正常 | P5 |
| **Absence idle timer** (业务) | 5s idle at ≥ maxCompletionScore 触发 AbsenceAccepted；4.9s idle 不触发；中途 interaction reset timer | P6 |
| **Save/load round-trip** (cross-method state) | PerfectMatch 状态 save → load 后状态一致（IChapterProgress serialization） | P7 |
| **Listener self-removal pattern** (framework boundary §V2-5 idempotency) | Subscribe IShadowPuzzleEvent → Handler 内 self-remove + null-out → 外部 Cleanup null-check skip → 全程无 TEngine "Delete handle failed" exception | P8 |
| **TimerModule precision** (framework boundary) | absenceAcceptDelay 5s timer 实测精度 ±0.1s | P9 (advisory) |
| **FsmModule conditional guard** (framework boundary) | TEngine FsmModule 是否支持 conditional transition guards (Risk #4 mitigation) | P10 (advisory) |

**Spike 文件路径**: `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S401_PuzzleStateMachine.cs`

### D. Story-001 Framework

**首批 story 创建**: `production/epics/shadow-puzzle/story-001-puzzle-state-machine.md`

Story scope：
- Implement 7-state FSM with all 10 transitions
- Implement IShadowPuzzleEvent interface + 5 sender 派发 (ADR-027)
- Tutorial grace period + Absence idle timer
- Hysteresis 防 flicker
- Save/load round-trip via IChapterProgress
- Cross-chapter difficulty matrix from TbPuzzle (Luban)

**TR coverage** (closes 9 ⚠️ TRs)：
- TR-puzzle-005 Puzzle state machine ⚠️→✅
- TR-puzzle-006 AbsenceAccepted Ch.5 ⚠️→✅
- TR-puzzle-007 matchScore temporal smoothing (handled by ADR-012; this story consume) ⚠️→partial
- TR-puzzle-008 Tutorial grace period ⚠️→✅
- TR-puzzle-009 PerfectMatch snap animation (handled by IShadowPuzzleEvent.OnPerfectMatch listener; this story is sender) ⚠→partial
- TR-puzzle-012 Collectibles 不影响 scoring (decoration only — orthogonal to FSM) ⚠️→OK (依赖 ShadowMatchCalculator implementation)
- TR-puzzle-013 Chapter-difficulty parameter matrix ⚠→✅
- TR-puzzle-014 Per-puzzle config via Luban TbPuzzle ⚠→✅
- TR-save-005 IChapterProgress serialization ⚠→ partial (依赖 ADR-008 实施)

**Sprint 4 deliverable**: ADR-014 expanded ✅ + Story-001 framework created ✅。actual implementation 留 future Sprint 4-5 dev-story (per Sprint 4 plan §Track A — S4-01 范围是 ADR + story 框架，不是 production code 实施)。

### E. Validation Criteria Update (V2.0 alignment)

V1 §Validation Criteria 10 项保持有效，本节新增 R3-mandatory 验证：

- [ ] R3 PlayMode probe `S401_PuzzleStateMachine.cs` 8 CORE cases (P1-P8) PASS + 2 advisory cases (P9-P10) optional
- [ ] ADR-027 §5 framework knowledge fact compliance (P8 verifies)
- [ ] ADR-029 V2.0 §V2-5 framework boundary behavior coverage (P9 + P10 advisory)
- [ ] Story-001 dev-story Phase 1.5 R1/R2/R3 grep gate PASS
- [ ] Story-001 finishes with /story-done verdict APPROVED

**Bulk promotion stamp**: ADR-014 Status: `Accepted (Promoted 2026-05-06 — bulk ceremony post Sprint 3 closure / ADR-029 V2.0 review B-1; required to unblock Puzzle State Machine implementation)`. Implementation Expand done 2026-05-06 — Sprint 4 S4-01.
