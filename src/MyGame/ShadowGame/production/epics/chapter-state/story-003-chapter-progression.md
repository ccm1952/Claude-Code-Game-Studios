// 该文件由Cursor 自动生成

# Story 003: Chapter Completion → Next Chapter Unlock Flow

> **Epic**: Chapter State
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/chapter-state-and-save.md`
**Requirement**: `TR-save-001`, `TR-save-004`
*(5 chapters sequential unlock; Chapter State is single authority)*

**ADR Governing Implementation**: ADR-007: Luban Config Access + ADR-006: GameEvent Protocol
**ADR Decision Summary**: When all puzzles in a chapter reach `Complete`, the chapter itself is marked `IsCompleted=true` and the next chapter's `IsUnlocked` is set to `true`. Chapter 5 completion is the game end condition. `TbChapter.UnlockCondition` defines the unlock rule (by default: previous chapter complete). Chapter completion fires `Evt_ChapterComplete`.

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW
**Engine Notes**: Chapter unlock is purely data-driven — no hardcoded "chapter 2 unlocks after chapter 1" logic. The unlock condition reads from `TbChapter.UnlockCondition`. For MVP, `UnlockCondition = "prev_chapter_complete"` for all chapters 2–5.

**Control Manifest Rules (this layer)**:
- Required: `All config reads via Tables.Instance.TbXXX.Get(id)` (ADR-007)
- Required: `Chapter State is single authority` (TR-save-004)
- Forbidden: `Never hardcode gameplay values` (ADR-007)

---

## Acceptance Criteria

*From GDD `design/gdd/chapter-state-and-save.md`, scoped to this story:*

- [ ] When the last puzzle in a chapter transitions to `Complete`, `ChapterStateManager` checks if all chapter puzzles are `Complete`
- [ ] If all puzzles are `Complete`: mark chapter `IsCompleted = true` and fire `Evt_ChapterComplete` (Story 004 handles dispatch)
- [ ] After `Evt_ChapterComplete`, find the next chapter (ID + 1) and set `IsUnlocked = true` (if chapter N+1 exists)
- [ ] Chapter 5 completion: `IsCompleted = true` for Chapter 5; no next chapter to unlock; fire `Evt_GameComplete` event
- [ ] `TbChapter.UnlockCondition` is read to determine unlock prerequisites — no hardcoded "chapter N+1 = N complete" logic
- [ ] Chapters 1–5 can only be unlocked in sequence (Chapter 3 cannot unlock before Chapter 2 is complete)
- [ ] Re-completing a chapter (replay mode, TR-save-016) does not re-fire `Evt_ChapterComplete` if already `IsCompleted`

---

## Implementation Notes

*Derived from ADR-008 + ADR-006 Implementation Guidelines:*

```csharp
private void CheckChapterCompletion(int chapterId)
{
    var chapter = GetChapterProgress(chapterId);

    // All puzzles complete?
    bool allDone = chapter.Puzzles.All(p => p.State == PuzzleState.Complete);
    if (!allDone) return;

    // Already marked complete?
    if (chapter.IsCompleted) return;

    chapter.IsCompleted = true;
    // Story 004 dispatches Evt_ChapterComplete

    // Unlock next chapter
    var nextConfig = ConfigSystem.Tables.TbChapter.Get(chapterId + 1);
    if (nextConfig != null) {
        var nextChapter = GetChapterProgress(chapterId + 1);
        if (nextChapter != null && !nextChapter.IsUnlocked) {
            // Verify unlock condition (from TbChapter.UnlockCondition)
            if (EvaluateUnlockCondition(nextConfig.UnlockCondition)) {
                nextChapter.IsUnlocked = true;
                // Story 004 dispatches Evt_ChapterUnlocked
            }
        }
    } else {
        // Chapter 5 — game complete
        // Story 004 dispatches Evt_GameComplete
    }
}

private bool EvaluateUnlockCondition(string condition)
{
    // MVP: only "prev_chapter_complete" is supported
    return condition == "prev_chapter_complete";
    // Future: add additional condition types here
}
```

`CheckChapterCompletion` is called from `OnPuzzleComplete` (Story 002) after unlocking the next puzzle or detecting no next puzzle remains.

**Replay guard**: `if (chapter.IsCompleted) return;` prevents re-firing on replay mode. Replay mode (TR-save-016) is an observation-only mode — no puzzle state changes propagate.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: Puzzle ordering and per-puzzle completion (triggers `CheckChapterCompletion`)
- Story 004: GameEvent dispatch for `Evt_ChapterComplete`, `Evt_ChapterUnlocked`, `Evt_GameComplete`
- Story 005: Persisting chapter completion to Save System

---

## QA Test Cases

- **AC-1**: All puzzles complete → chapter complete
  - Given: Chapter 1; Puzzles 1, 2, 3 all in `Complete` state
  - When: `CheckChapterCompletion(1)` called
  - Then: `Chapter1.IsCompleted == true`; `Chapter2.IsUnlocked == true`
  - Edge cases: only 2 of 3 puzzles complete — chapter NOT marked complete

- **AC-2**: Sequential unlock (Chapter 3 requires Chapter 2)
  - Given: Chapter 1 complete; Chapter 2 not yet complete; Chapter 3 locked
  - When: State inspected
  - Then: Chapter 3 `IsUnlocked == false`
  - When: Chapter 2 completes
  - Then: Chapter 3 `IsUnlocked == true`
  - Edge cases: attempting to manually set Chapter 3 unlocked before Chapter 2 complete — rejected

- **AC-3**: Chapter 5 completion fires Evt_GameComplete
  - Given: Chapter 5; all puzzles complete
  - When: `CheckChapterCompletion(5)` called
  - Then: `Chapter5.IsCompleted == true`; no chapter 6 unlock attempt; `Evt_GameComplete` dispatched (Story 004)
  - Edge cases: `TbChapter.Get(6)` returns null — handled gracefully (null check before unlock)

- **AC-4**: Re-completion is idempotent
  - Given: Chapter 2 `IsCompleted == true`
  - When: `CheckChapterCompletion(2)` called again (replay scenario)
  - Then: No duplicate `Evt_ChapterComplete` fires; Chapter 3 `IsUnlocked` state unchanged
  - Edge cases: rapid double-call within one frame — second call is a no-op

- **AC-5**: Unlock condition evaluated from TbChapter
  - Given: `TbChapter.Get(3).UnlockCondition == "prev_chapter_complete"` for Chapter 3
  - When: Chapter 2 completes
  - Then: `EvaluateUnlockCondition("prev_chapter_complete")` returns true; Chapter 3 unlocked
  - Edge cases: unknown condition string → `EvaluateUnlockCondition` returns `false`, logs warning, chapter not unlocked

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/chapter-state/chapter_progression_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (data model), Story 002 (puzzle completion triggers chapter check)
- Unlocks: Story 004 (events for chapter/puzzle state), Story 005 (save integration — chapter completion triggers auto-save)
