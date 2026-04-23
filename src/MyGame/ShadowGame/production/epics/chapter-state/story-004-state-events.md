// 该文件由Cursor 自动生成

# Story 004: GameEvent Dispatch for Chapter/Puzzle State Changes

> **Epic**: Chapter State
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/chapter-state-and-save.md`
**Requirement**: `TR-save-004` (Chapter State is single authority), `TR-save-005` (IChapterProgress interface decoupling)
*(State change events broadcast from ChapterStateManager to all consumers)*

**ADR Governing Implementation**: ADR-006: GameEvent Protocol + ADR-007: Luban Config Access
**ADR Decision Summary**: All chapter and puzzle state transitions are broadcast via `GameEvent`. Consumers (Narrative, Audio, Save System, UI) are passive listeners — they never call `ChapterStateManager` methods directly. SP-001 confirmed struct payloads for frequent events.

**Engine**: Unity 2022.3.62f2 LTS + TEngine 6.0.0 | **Risk**: MEDIUM
**Engine Notes**: Chapter-range events: 1300–1399. SP-001 confirms `GameEvent.Send<T>` with struct payloads is allocation-free. Event cascade depth for `PuzzleComplete → ChapterComplete → RequestSceneChange` must not exceed 3 (ADR-006).

**Control Manifest Rules (this layer)**:
- Required: `All event IDs defined as public const int in centralized EventId static class` (ADR-006)
- Required: `Event ID allocation: Chapter State 1300-1399` (ADR-006)
- Required: `Every EventId constant must have XML doc comment` (ADR-006)
- Required: `Use struct for frequent event payloads` (ADR-006)
- Required: `Cross-event cascade depth must not exceed 3` (ADR-006)
- Required: `Register all listeners in Init(); remove all in Dispose()` (ADR-006)
- Forbidden: `Never define event IDs outside EventId.cs` (ADR-006)
- Forbidden: `Never re-enter GameEvent.Send for the same event ID inside a handler` (ADR-006)
- Guardrail: `Event dispatch per Send ≤ 0.05ms` (ADR-006)

---

## Acceptance Criteria

*From GDD `design/gdd/chapter-state-and-save.md`, scoped to this story:*

- [ ] `EventId.cs` defines in range 1300–1399: `Evt_PuzzleStateChanged`, `Evt_ChapterComplete`, `Evt_ChapterUnlocked`, `Evt_GameComplete`, `Evt_PuzzleLockAll`, `Evt_PuzzleUnlock` — with XML doc comments
- [ ] Named payload structs: `PuzzleStateChangedPayload { int ChapterId, int PuzzleId, PuzzleState NewState }`, `ChapterCompletePayload { int ChapterId }`, `ChapterUnlockedPayload { int ChapterId }`, `GameCompletePayload {}`
- [ ] `Evt_PuzzleStateChanged` fires every time any puzzle's `PuzzleState` changes (including Locked→Idle, Active→NearMatch, etc.)
- [ ] `Evt_ChapterComplete` fires exactly once per chapter completion (guarded by `IsCompleted` flag from Story 003)
- [ ] `Evt_ChapterUnlocked` fires when a new chapter's `IsUnlocked` becomes `true`
- [ ] `Evt_GameComplete` fires when Chapter 5 completes
- [ ] `Evt_PuzzleLockAll` and `Evt_PuzzleUnlock` are in this range and consumed by Object Interaction (Story 006 of that epic)
- [ ] Cascade depth for `Evt_PuzzleStateChanged → Evt_ChapterComplete → Evt_RequestSceneChange` is exactly 3 — no deeper chains
- [ ] `ChapterStateManager` subscribes to `Evt_PuzzleStateChanged` to drive progression logic; it also subscribes to `Evt_SceneLoadComplete` to confirm scene load for the active chapter

---

## Implementation Notes

*Derived from ADR-006 + SP-001 Implementation Guidelines:*

**EventId.cs additions** (1300–1399 range):
```csharp
/// <summary>
/// A puzzle's PuzzleState has changed. Sender: ChapterStateManager. Listeners: ShadowPuzzle, Narrative, SaveManager, UI.
/// Payload: PuzzleStateChangedPayload. Cascade: may trigger ChapterComplete (depth 2).
/// </summary>
public const int Evt_PuzzleStateChanged = 1300;

/// <summary>
/// All puzzles in a chapter are complete. Sender: ChapterStateManager. Listeners: Narrative, Audio, SaveManager.
/// Payload: ChapterCompletePayload. Cascade: may trigger RequestSceneChange (depth 3 max).
/// </summary>
public const int Evt_ChapterComplete = 1301;

/// <summary>
/// A new chapter has been unlocked. Sender: ChapterStateManager. Listeners: MainMenu UI, SaveManager.
/// Payload: ChapterUnlockedPayload. Cascade: none.
/// </summary>
public const int Evt_ChapterUnlocked = 1302;

/// <summary>
/// All chapters complete — game finished. Sender: ChapterStateManager. Listeners: Narrative, Credits, SaveManager.
/// Payload: GameCompletePayload. Cascade: none.
/// </summary>
public const int Evt_GameComplete = 1303;

/// <summary>
/// Lock all interactable objects. Sender: ShadowPuzzle, Narrative, Tutorial. Listeners: InteractionLockManager.
/// Payload: string lockerId. Cascade: none.
/// </summary>
public const int Evt_PuzzleLockAll = 1304;

/// <summary>
/// Release a lock on interactable objects. Sender: ShadowPuzzle, Narrative, Tutorial. Listeners: InteractionLockManager.
/// Payload: string lockerId. Cascade: none.
/// </summary>
public const int Evt_PuzzleUnlock = 1305;
```

**Dispatch points in ChapterStateManager**:
- After any `puzzle.State = newState`: `GameEvent.Send(Evt_PuzzleStateChanged, payload)`
- After `chapter.IsCompleted = true`: `GameEvent.Send(Evt_ChapterComplete, payload)`
- After `nextChapter.IsUnlocked = true`: `GameEvent.Send(Evt_ChapterUnlocked, payload)`
- On Chapter 5 completion: `GameEvent.Send(Evt_GameComplete, payload)`

**Cascade depth guard**: Chapter completion may lead to scene change request. The chain is: `Evt_PuzzleStateChanged(depth1) → Evt_ChapterComplete(depth2) → Evt_RequestSceneChange(depth3)`. Any handler that needs to go deeper must use `await UniTask.Yield()` to break the sync chain.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001–003: Logic that determines when to fire events (data model and progression)
- Story 005: Save System subscribing to these events for auto-save triggers
- Shadow Puzzle epic: publishing `Evt_PuzzleStateChanged` from the puzzle FSM (that's Shadow Puzzle's responsibility; Chapter State manager only receives and re-broadcasts chapter-level consequences)

---

## QA Test Cases

- **AC-1**: Evt_PuzzleStateChanged fires on every state change
  - Given: A test listener registered for `Evt_PuzzleStateChanged`
  - When: Puzzle 1 transitions from `Idle` to `Active`
  - Then: Listener receives payload with `ChapterId=1, PuzzleId=1, NewState=Active`
  - Edge cases: same state transition twice (Idle→Idle) should NOT fire (no change occurred)

- **AC-2**: Evt_ChapterComplete fires exactly once
  - Given: Chapter 1; all puzzles in `Complete` state
  - When: Last puzzle completion triggers `CheckChapterCompletion`
  - Then: `Evt_ChapterComplete` fires exactly once with `ChapterId=1`
  - When: Chapter 1 is re-visited (replay mode)
  - Then: `Evt_ChapterComplete` does NOT fire again
  - Edge cases: verify listener receives payload within same frame as puzzle completion

- **AC-3**: Evt_ChapterUnlocked fires for newly unlocked chapters
  - Given: Chapter 2 not yet unlocked; Chapter 1 completes
  - When: `ChapterStateManager` processes chapter completion
  - Then: `Evt_ChapterUnlocked` fires with `ChapterId=2`
  - Edge cases: Chapter 5 completion does not trigger `Evt_ChapterUnlocked` (no Chapter 6)

- **AC-4**: Cascade depth does not exceed 3
  - Given: A cascade chain: `Evt_PuzzleStateChanged → Evt_ChapterComplete → Evt_RequestSceneChange`
  - When: The chain is triggered
  - Then: No event at depth 4 is synchronously dispatched in the same call stack
  - Edge cases: manually count via debug stack trace; any depth ≥ 4 must use `UniTask.Yield()` break

- **AC-5**: All EventId constants have correct IDs and XML doc comments
  - Given: `EventId.cs` compiled
  - When: Inspect constants in 1300–1399 range
  - Then: 6 constants defined (`Evt_PuzzleStateChanged=1300` through `Evt_PuzzleUnlock=1305`); each has `<summary>` with Sender, Listeners, Payload, Cascade fields
  - Edge cases: no duplicate IDs within 1300–1399; no gaps that shadow existing global IDs

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `tests/integration/chapter-state/state_events_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (data model), Story 002 (puzzle ordering triggers events), Story 003 (chapter progression triggers events)
- Unlocks: Story 005 (Save System subscribes to these events for auto-save), Shadow Puzzle epic (consumes `Evt_PuzzleStateChanged`)
