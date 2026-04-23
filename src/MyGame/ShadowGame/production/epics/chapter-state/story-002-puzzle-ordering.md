// 该文件由Cursor 自动生成

# Story 002: Puzzle Unlock/Ordering Within a Chapter

> **Epic**: Chapter State
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/chapter-state-and-save.md`
**Requirement**: `TR-save-002`, `TR-save-003`
*(Linear puzzle progression; puzzle completion is irreversible)*

**ADR Governing Implementation**: ADR-007: Luban Config Access + ADR-008: Save System
**ADR Decision Summary**: Within each chapter, puzzles progress linearly in the order defined by `TbPuzzle.PuzzleOrder`. Completing puzzle N unlocks puzzle N+1. Completion is irreversible — `PuzzleState.Complete` is a terminal state that cannot be reversed. `TbPuzzle` defines the ordering per chapter.

**Engine**: Unity 2022.3.62f2 LTS + Luban | **Risk**: LOW
**Engine Notes**: Puzzle ordering is fully driven by `TbPuzzle.PuzzleOrder` (int field, 1-based per chapter). No ordering logic should be hardcoded. SP-004: all `TbPuzzle` reads on main thread.

**Control Manifest Rules (this layer)**:
- Required: `All config reads via Tables.Instance.TbXXX.Get(id)` (ADR-007)
- Required: `Puzzle completion irreversible` (TR-save-003)
- Required: `PuzzleState enum maps directly to IChapterProgress serialization` (ADR-014)
- Forbidden: `Never hardcode gameplay values` (ADR-007)
- Forbidden: `Never access Luban Tables from UniTask.Run()` (SP-004)

---

## Acceptance Criteria

*From GDD `design/gdd/chapter-state-and-save.md`, scoped to this story:*

- [ ] Within a chapter, puzzles are ordered by `TbPuzzle.PuzzleOrder` (ascending integer) — first puzzle starts in `Idle`, all subsequent in `Locked`
- [ ] When puzzle N transitions to `Complete`, puzzle N+1 transitions from `Locked` to `Idle` (becomes available)
- [ ] `PuzzleState.Complete` is irreversible — no code path transitions a puzzle out of `Complete`; any such attempt is a no-op with `Debug.LogWarning`
- [ ] `PuzzleState.PerfectMatch` and `PuzzleState.AbsenceAccepted` are also irreversible (these are pre-complete terminal states from ADR-014)
- [ ] `ChapterStateManager.OnPuzzleComplete(int chapterId, int puzzleId)` is the single method that processes puzzle completion logic
- [ ] `GetActivePuzzle(int chapterId)` returns the first puzzle in `Idle` state for the chapter (the currently active puzzle); returns null if chapter complete
- [ ] Puzzle ordering is read from `TbPuzzle.PuzzleOrder` per chapter — multiple puzzles per chapter supported

---

## Implementation Notes

*Derived from ADR-007 + ADR-008 + ADR-014 Implementation Guidelines:*

```csharp
public void OnPuzzleComplete(int chapterId, int puzzleId)
{
    var chapter = GetChapterProgress(chapterId);
    var puzzle = chapter.GetPuzzle(puzzleId);

    // Guard: irreversibility
    if (puzzle.State == PuzzleState.Complete) {
        Debug.LogWarning($"[ChapterState] Puzzle {puzzleId} already Complete — ignoring.");
        return;
    }

    // Mark complete
    puzzle.State = PuzzleState.Complete;

    // Find next puzzle by order
    var nextPuzzle = chapter.Puzzles
        .Where(p => p.PuzzleOrder == puzzle.PuzzleOrder + 1)
        .FirstOrDefault();

    if (nextPuzzle != null && nextPuzzle.State == PuzzleState.Locked) {
        nextPuzzle.State = PuzzleState.Idle;
        // Story 004 dispatches the state-change event
    } else {
        // No next puzzle — chapter may be complete (Story 003 handles this)
    }
}
```

**Irreversibility guard for all terminal states**:
```csharp
private bool IsTerminalState(PuzzleState state) =>
    state == PuzzleState.PerfectMatch ||
    state == PuzzleState.AbsenceAccepted ||
    state == PuzzleState.Complete;
```

Any `SetPuzzleState(puzzleId, newState)` call must check `IsTerminalState(current)` first and reject with a warning.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: `ChapterProgress` and `PuzzleState` data model (must be DONE)
- Story 003: Chapter completion logic (all puzzles Complete → chapter Complete)
- Story 004: Event dispatch for puzzle state changes

---

## QA Test Cases

- **AC-1**: First puzzle starts Idle; rest start Locked
  - Given: Chapter 1 freshly loaded; 3 puzzles in order 1, 2, 3
  - When: State inspected
  - Then: Puzzle 1 state == `Idle`; Puzzle 2 state == `Locked`; Puzzle 3 state == `Locked`
  - Edge cases: chapter with 1 puzzle — that puzzle starts Idle immediately

- **AC-2**: Completing puzzle N unlocks puzzle N+1
  - Given: Chapter 1; Puzzle 1 in Active state
  - When: `OnPuzzleComplete(1, 1)` called
  - Then: Puzzle 1 state == `Complete`; Puzzle 2 state == `Idle`; Puzzle 3 state == `Locked`
  - Edge cases: completing last puzzle — no N+1; state remains as-is (Story 003 handles chapter completion)

- **AC-3**: Complete state is irreversible
  - Given: Puzzle 1 state == `Complete`
  - When: `SetPuzzleState(1, PuzzleState.Idle)` attempted
  - Then: State remains `Complete`; `Debug.LogWarning` is logged
  - Edge cases: attempt to set to any non-Complete state from Complete — all rejected

- **AC-4**: PerfectMatch and AbsenceAccepted are also irreversible
  - Given: Puzzle 2 state == `PerfectMatch`
  - When: `SetPuzzleState(2, PuzzleState.Idle)` attempted
  - Then: State remains `PerfectMatch`; warning logged
  - Edge cases: transition from PerfectMatch → Complete is allowed (it's the only valid next state)

- **AC-5**: GetActivePuzzle returns correct puzzle
  - Given: Chapter 2; Puzzle 1 = Complete; Puzzle 2 = Idle; Puzzle 3 = Locked
  - When: `GetActivePuzzle(2)` called
  - Then: Returns Puzzle 2 (first Idle)
  - Edge cases: all puzzles Complete → returns null (chapter is done)

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/chapter-state/puzzle_ordering_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (chapter data model — must be DONE)
- Unlocks: Story 003 (chapter progression triggers after last puzzle completes), Story 004 (events include puzzle state changes)
