// 该文件由Cursor 自动生成

# Story 005: Bidirectional Integration with Save System (IChapterProgress)

> **Epic**: Chapter State
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/chapter-state-and-save.md`
**Requirement**: `TR-save-005`
*(IChapterProgress interface decoupling between Chapter State and Save System)*

**ADR Governing Implementation**: ADR-008: Save System + ADR-006: GameEvent Protocol
**ADR Decision Summary**: `IChapterProgress` is the data interface that decouples Chapter State from Save System. At boot: `SaveSystem.LoadAsync()` → `ChapterStateManager.Init(saveData)`. At runtime: Chapter State exposes `GetProgressSnapshot(): IChapterProgress` for Save System to serialize. Neither system has a compile-time reference to the other's implementation class.

**Engine**: Unity 2022.3.62f2 LTS + UniTask 2.5.10 | **Risk**: MEDIUM
**Engine Notes**: Init order is critical (ADR-008): `SaveSystem.Init() → SaveSystem.LoadAsync() → ChapterState.Init(saveData)`. This is guaranteed by boot Step 11 (SaveSystem) → Step 12 (ChapterState). Both steps run in `ProcedureMain` on the main thread before any gameplay begins.

**Control Manifest Rules (this layer)**:
- Required: `Init order: SaveSystem.Init() → SaveSystem.LoadAsync() → ChapterState.Init(saveData)` (ADR-008)
- Required: `All async operations via UniTask` (ADR-001)
- Required: `All inter-module communication via GameEvent int-based event bus` (ADR-001, ADR-006)
- Required: `Register all listeners in Init(); remove all in Dispose()` (ADR-006)
- Forbidden: `Never use Coroutines for async operations` (ADR-008)
- Forbidden: `Never store settings inside the save file` (ADR-008)

---

## Acceptance Criteria

*From GDD `design/gdd/chapter-state-and-save.md`, scoped to this story:*

- [ ] `IChapterProgress` interface defined (in Story 001) is the sole data contract between Chapter State and Save System — no direct class references across the boundary
- [ ] `ChapterStateManager.GetProgressSnapshot()` returns an `IChapterProgress` implementation populated with current runtime state (UnlockedChapterIds[], CompletedChapterIds[], PuzzleEntries[])
- [ ] `SaveManager` calls `GetProgressSnapshot()` (provided as a `Func<IChapterProgress>` delegate registered during boot) to retrieve data for serialization — never accesses `ChapterStateManager` directly
- [ ] At boot: `SaveSystem.LoadAsync()` completes before `ChapterStateManager.Init(saveData)` is called (verified via boot sequence ordering test)
- [ ] `ChapterStateManager.Init(saveData)` is idempotent: if `saveData == null` (fresh game), initialises correctly with Chapter 1 unlocked
- [ ] After Chapter State processes a puzzle/chapter change, it fires `Evt_PuzzleStateChanged` or `Evt_ChapterComplete` (Story 004); `SaveManager` subscribes to these events for auto-save triggers (Save System Story 004)
- [ ] `GetProgressSnapshot()` is synchronous and < 1ms (it reads from in-memory state — no I/O)
- [ ] After a full save→load→init round-trip: all `ChapterId`, `IsUnlocked`, `IsCompleted`, and `PuzzleState` values match the pre-save state exactly

---

## Implementation Notes

*Derived from ADR-008 + ADR-006 Implementation Guidelines:*

**Boot sequence wiring** (in `ProcedureMain`):
```csharp
// Step 11: SaveSystem init + load
await SaveSystem.Init();
var saveData = await SaveSystem.LoadAsync();

// Step 12: ChapterState init with save data
ChapterStateManager.Init(saveData?.ChapterProgress); // null-safe: null = fresh game

// Register snapshot delegate for Save System
SaveSystem.RegisterProgressProvider(() => ChapterStateManager.GetProgressSnapshot());
```

**GetProgressSnapshot implementation**:
```csharp
public IChapterProgress GetProgressSnapshot()
{
    return new ChapterProgressSnapshot {
        UnlockedChapterIds = _chapters
            .Where(c => c.IsUnlocked)
            .Select(c => c.ChapterId)
            .ToArray(),
        CompletedChapterIds = _chapters
            .Where(c => c.IsCompleted)
            .Select(c => c.ChapterId)
            .ToArray(),
        PuzzleEntries = _chapters
            .SelectMany(c => c.Puzzles)
            .Select(p => new PuzzleProgressEntry {
                ChapterId = p.ChapterId,
                PuzzleId = p.PuzzleId,
                State = p.State
            })
            .ToArray()
    };
}
```

**Interface isolation**: `SaveSystem.cs` knows only about `IChapterProgress` and the delegate pattern. `ChapterStateManager.cs` knows only about `IChapterProgress`. The concrete `ChapterProgressSnapshot` class lives in a shared assembly section visible to both.

**Round-trip test**: Serialise current state → clear in-memory state → deserialise → verify all fields match.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Save System Story 001: JSON schema definition (`IChapterProgress` serialization format)
- Save System Story 002: Atomic write + CRC32 (Save System's concern)
- Save System Story 004: Auto-save triggers (Save System subscribes to Chapter State events)

---

## QA Test Cases

- **AC-1**: Round-trip: save → load → init preserves all state
  - Given: Chapter 1 complete; Chapter 2 unlocked; Puzzle 1 complete, Puzzle 2 idle
  - When: `GetProgressSnapshot()` called → serialised → deserialised → `Init(deserialisedData)` called
  - Then: All fields match exactly: Chapter 1 IsCompleted=true, Chapter 2 IsUnlocked=true, Puzzle 1 State=Complete, Puzzle 2 State=Idle
  - Edge cases: fresh game (null save) → Chapter 1 unlocked; all puzzles in initial state

- **AC-2**: Boot order is respected
  - Given: Boot sequence runs
  - When: `ProcedureMain` executes Steps 11 and 12
  - Then: `SaveSystem.LoadAsync()` awaited before `ChapterStateManager.Init()` is called; timing logged in debug
  - Edge cases: if save file is corrupt, LoadAsync returns null → `Init(null)` → fresh game (no crash)

- **AC-3**: GetProgressSnapshot is synchronous and fast
  - Given: Chapter State with 5 chapters and 15 total puzzles
  - When: `GetProgressSnapshot()` called
  - Then: Returns within 1ms; result is a complete snapshot with no I/O or async calls
  - Edge cases: called from inside an event handler — must not deadlock

- **AC-4**: IChapterProgress is the only cross-system contract
  - Given: `SaveSystem.cs` codebase
  - When: Grep for `ChapterStateManager` reference
  - Then: Zero direct references to `ChapterStateManager` in Save System code; only `IChapterProgress` and the delegate
  - Edge cases: likewise, `ChapterStateManager.cs` must not reference `SaveManager` directly

- **AC-5**: SaveManager delegate receives correct snapshot
  - Given: `SaveSystem.RegisterProgressProvider(delegate)` registered
  - When: Auto-save triggers and SaveManager calls the delegate
  - Then: Returned `IChapterProgress` matches current in-memory state of ChapterStateManager exactly
  - Edge cases: calling delegate before ChapterState.Init() — returns empty/default snapshot; no NullReferenceException

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `tests/integration/chapter-state/save_integration_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (IChapterProgress interface), Story 003 (chapter progression data), Story 004 (events that trigger save); Save System Story 001 (save data schema — must be designed concurrently)
- Unlocks: Save System is now able to persist Chapter State fully; full end-to-end gameplay loop can be tested
