// 该文件由Cursor 自动生成

# Story 001: Chapter Data Model + Luban TbChapter Integration

> **Epic**: Chapter State
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/chapter-state-and-save.md`
**Requirement**: `TR-save-004`, `TR-save-017`
*(Chapter State is single authority; Luban config tables TbChapter + TbPuzzle)*

**ADR Governing Implementation**: ADR-007: Luban Config Access + ADR-006: GameEvent Protocol
**ADR Decision Summary**: `ChapterStateManager` is the Single Source of Truth for all chapter and puzzle progress. It initialises from save data on boot, reads static configuration from Luban `TbChapter`/`TbPuzzle`, and exposes runtime state via `ChapterProgress[]` and `PuzzleState[]` arrays. Config reads are main-thread only (SP-004).

**Engine**: Unity 2022.3.62f2 LTS + Luban (latest) | **Risk**: MEDIUM
**Engine Notes**: `TbChapter` and `TbPuzzle` are Luban-generated tables in `GameProto` assembly. Access via `ConfigSystem.Tables.TbChapter.Get(id)`. SP-004 confirmed: all reads must be on the Unity main thread; never in `UniTask.Run()`. `Tables.Init()` completes at boot Step 7 — `ChapterStateManager.Init()` runs at Step 12+, so Tables are always ready.

**Control Manifest Rules (this layer)**:
- Required: `All config reads via Tables.Instance.TbXXX.Get(id)` (ADR-007)
- Required: `Tables.Init() executes at boot Step 7 — MUST complete before ANY system reads config` (ADR-007)
- Required: `Config data objects are read-only after Init()` (ADR-007)
- Required: `Never access Luban Tables from UniTask.Run() (thread pool)` (SP-004)
- Required: `Chapter State is single authority` (ADR-008, TR-save-004)
- Forbidden: `Never hardcode gameplay values` (ADR-007)
- Forbidden: `Never hand-edit any file in GameProto` (ADR-007)

---

## Acceptance Criteria

*From GDD `design/gdd/chapter-state-and-save.md`, scoped to this story:*

- [ ] `ChapterProgress` struct/class defined with fields: `int ChapterId`, `bool IsUnlocked`, `bool IsCompleted`, `PuzzleState[] Puzzles`
- [ ] `PuzzleState` enum defined with values: `Locked`, `Idle`, `Active`, `NearMatch`, `PerfectMatch`, `AbsenceAccepted`, `Complete`
- [ ] `ChapterStateManager` class is the sole owner of `ChapterProgress[5]` (one per chapter)
- [ ] `ChapterStateManager.Init(IChapterProgress saveData)` initialises runtime state from the save data interface; chapters not present in save default to `IsUnlocked=false, IsCompleted=false`
- [ ] Chapter 1 is always unlocked by default (even on fresh save)
- [ ] `TbChapter` provides static chapter metadata: `Id`, `SceneId`, `UnlockCondition`, `BgmAsset`, `EmotionalWeight`
- [ ] `TbPuzzle` provides static puzzle configuration per chapter: `Id`, `ChapterId`, `PuzzleOrder`, `PuzzleType`, `perfectMatchThreshold`, `nearMatchThreshold`
- [ ] `ChapterStateManager.GetChapterProgress(int chapterId)` returns the runtime `ChapterProgress` for that chapter; returns null-safe result for invalid IDs
- [ ] `ChapterStateManager.GetCurrentPuzzleState(int chapterId, int puzzleId)` returns the current `PuzzleState`
- [ ] All Luban reads happen on the main thread (validated in debug builds via `Debug.Assert(Thread.CurrentThread.ManagedThreadId == MainThreadId)`)

---

## Implementation Notes

*Derived from ADR-007 + ADR-008 Implementation Guidelines:*

```csharp
public interface IChapterProgress
{
    int[] UnlockedChapterIds { get; }
    int[] CompletedChapterIds { get; }
    PuzzleProgressEntry[] PuzzleEntries { get; }
}

public struct PuzzleProgressEntry
{
    public int ChapterId;
    public int PuzzleId;
    public PuzzleState State;
}

public class ChapterStateManager
{
    private ChapterProgress[] _chapters = new ChapterProgress[5];

    public void Init(IChapterProgress saveData)
    {
        // Build from Luban TbChapter (all 5 rows)
        for (int i = 1; i <= 5; i++) {
            var config = ConfigSystem.Tables.TbChapter.Get(i);
            _chapters[i - 1] = new ChapterProgress {
                ChapterId = i,
                IsUnlocked = (i == 1), // Chapter 1 always unlocked
                IsCompleted = false,
                Puzzles = BuildPuzzleStates(i)
            };
        }

        // Apply save data overrides
        if (saveData != null) {
            foreach (int id in saveData.UnlockedChapterIds) SetUnlocked(id);
            foreach (int id in saveData.CompletedChapterIds) SetCompleted(id);
            foreach (var entry in saveData.PuzzleEntries) SetPuzzleState(entry);
        }
    }
}
```

**SP-004 enforcement**: The `Init()` call happens in `ProcedureMain` on the main thread. Any `async` method that reads config must NOT be wrapped in `UniTask.Run()`.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: Puzzle unlock ordering within a chapter
- Story 003: Chapter progression (chapter completion → next chapter unlock)
- Story 004: Event dispatch for state changes
- Story 005: Save System integration (IChapterProgress is defined here; bidirectional wire-up is Story 005)

---

## QA Test Cases

- **AC-1**: Fresh init — Chapter 1 unlocked, others locked
  - Given: `Init(null)` called (no save data)
  - When: State inspected
  - Then: Chapter 1 `IsUnlocked == true`; Chapters 2–5 `IsUnlocked == false`; all puzzles in `Locked` state
  - Edge cases: `Init()` called twice must reset to save-data state (idempotent)

- **AC-2**: Save data overrides are applied
  - Given: Save data reports Chapter 1 complete, Chapter 2 unlocked
  - When: `Init(saveData)` called
  - Then: Chapter 1 `IsCompleted == true`; Chapter 2 `IsUnlocked == true`; Chapter 3 still locked
  - Edge cases: save data with invalid chapter ID (99) is silently ignored

- **AC-3**: TbChapter and TbPuzzle are read for all 5 chapters
  - Given: `Tables.Init()` completed (boot step 7)
  - When: `ChapterStateManager.Init()` runs
  - Then: All 5 `TbChapter.Get(i)` calls return non-null; all puzzle rows per chapter are loaded
  - Edge cases: if a TbChapter row is missing, log error and use defaults (do not crash)

- **AC-4**: GetChapterProgress returns correct data
  - Given: Chapter 2 is unlocked in state
  - When: `GetChapterProgress(2)` called
  - Then: Returns `ChapterProgress` with `ChapterId=2`, `IsUnlocked=true`
  - Edge cases: `GetChapterProgress(99)` returns safe null or throws a documented exception

- **AC-5**: Luban reads are main-thread only
  - Given: Debug build
  - When: `Init()` runs
  - Then: `Debug.Assert(Thread.CurrentThread.ManagedThreadId == MainThreadId)` passes at each `TbChapter.Get()` call
  - Edge cases: if called from a thread pool (should never happen) — assertion fires and logs

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/chapter-state/chapter_data_model_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Save System Story 001 (save data schema — `IChapterProgress` interface is defined here and consumed by Save System)
- Unlocks: Story 002 (puzzle ordering), Story 003 (chapter progression), Story 004 (events), Story 005 (save integration)
