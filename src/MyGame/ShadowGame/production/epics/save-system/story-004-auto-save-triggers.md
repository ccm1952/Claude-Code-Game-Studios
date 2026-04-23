// 该文件由Cursor 自动生成

# Story 004: Auto-Save Triggers (5 Conditions + 1s Debounce)

> **Epic**: Save System
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/chapter-state-and-save.md`
**Requirement**: `TR-save-006`, `TR-save-007`, `TR-save-018`, `TR-save-019`
*(Auto-save triggers 5 conditions; save debounce 1s; save init before gameplay; Pause/Quit immediate save)*

**ADR Governing Implementation**: ADR-008: Save System
**ADR Decision Summary**: Auto-save triggers on 5 conditions: `Evt_PuzzleStateChanged` (debounced 1s), `Evt_ChapterComplete` (immediate), collectible pickup (1s debounce), `OnApplicationPause(true)` (immediate), `OnApplicationQuit` (immediate). Debounce prevents write storms. Pause/Quit bypass debounce for safety.

**Engine**: Unity 2022.3.62f2 LTS + UniTask 2.5.10 | **Risk**: LOW
**Engine Notes**: `OnApplicationPause` and `OnApplicationQuit` are Unity lifecycle callbacks on a MonoBehaviour. `SaveManager` itself is not a MonoBehaviour — implement `SaveManagerBridge` MonoBehaviour in MainScene that calls `SaveManager.ForceSaveAsync()` on these callbacks. UniTask `CancellationTokenSource` for debounce timer.

**Control Manifest Rules (this layer)**:
- Required: `Auto-save triggers: Evt_PuzzleStateChanged (1s debounce), Evt_ChapterComplete (immediate), collectible pickup (1s debounce), OnApplicationPause(true) (immediate), OnApplicationQuit (immediate)` (ADR-008)
- Required: `All save I/O via UniTask — no synchronous file I/O` (ADR-008)
- Required: `Init order: SaveSystem.Init() → SaveSystem.LoadAsync() → ChapterState.Init(saveData)` (ADR-008)
- Forbidden: `Never use Coroutines for save I/O` (ADR-008)

---

## Acceptance Criteria

*From GDD `design/gdd/chapter-state-and-save.md`, scoped to this story:*

- [ ] `SaveManager` subscribes to `Evt_PuzzleStateChanged` in `Init()`; on receipt, schedules debounced save (1s delay); multiple `Evt_PuzzleStateChanged` within 1s collapse into one save
- [ ] `SaveManager` subscribes to `Evt_ChapterComplete` in `Init()`; on receipt, calls `ForceSaveAsync()` immediately (no debounce)
- [ ] `SaveManagerBridge` MonoBehaviour (in MainScene) implements `OnApplicationPause(bool pause)` and `OnApplicationQuit()`; calls `ForceSaveAsync()` when `pause == true` or on quit
- [ ] `ForceSaveAsync()` bypasses debounce and saves immediately regardless of pending debounce timer
- [ ] Debounce is cancelled and reset if a new trigger arrives within the 1s window
- [ ] `SaveSystem.Init()` completes before any gameplay system initialises (boot Step 11 — `TR-save-018`)
- [ ] Two consecutive `Evt_PuzzleStateChanged` events 0.5s apart result in exactly 1 file write (debounce collapsed)
- [ ] `Evt_ChapterComplete` arriving during a pending debounce: cancels debounce and saves immediately (chapter complete is higher priority)
- [ ] Save listeners are removed in `Dispose()`; `SaveManagerBridge` is destroyed with MainScene (never destroyed while app is running)

---

## Implementation Notes

*Derived from ADR-008 Implementation Guidelines:*

```csharp
public class SaveManager
{
    private CancellationTokenSource _debounceCts;
    private const float DEBOUNCE_SECONDS = 1f;

    public void Init()
    {
        GameEvent.AddListener<PuzzleStateChangedPayload>(
            EventId.Evt_PuzzleStateChanged, OnPuzzleStateChanged);
        GameEvent.AddListener<ChapterCompletePayload>(
            EventId.Evt_ChapterComplete, OnChapterComplete);
    }

    public void Dispose()
    {
        GameEvent.RemoveListener<PuzzleStateChangedPayload>(
            EventId.Evt_PuzzleStateChanged, OnPuzzleStateChanged);
        GameEvent.RemoveListener<ChapterCompletePayload>(
            EventId.Evt_ChapterComplete, OnChapterComplete);
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
    }

    private void OnPuzzleStateChanged(int evtId, PuzzleStateChangedPayload payload)
        => ScheduleDebouncedSave();

    private void OnChapterComplete(int evtId, ChapterCompletePayload payload)
        => ForceSaveAsync().Forget();

    private void ScheduleDebouncedSave()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();
        DebouncedSaveInternalAsync(_debounceCts.Token).Forget();
    }

    private async UniTaskVoid DebouncedSaveInternalAsync(CancellationToken ct)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(DEBOUNCE_SECONDS), cancellationToken: ct);
        if (!ct.IsCancellationRequested) await SaveAsync(_progressProvider());
    }

    public async UniTask ForceSaveAsync()
    {
        _debounceCts?.Cancel(); // cancel any pending debounced save
        await SaveAsync(_progressProvider());
    }
}

// In MainScene:
public class SaveManagerBridge : MonoBehaviour
{
    private void OnApplicationPause(bool pause)
    {
        if (pause) SaveManager.Instance.ForceSaveAsync().Forget();
    }
    private void OnApplicationQuit() => SaveManager.Instance.ForceSaveAsync().Forget();
}
```

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: `SaveAsync` internal implementation (I/O mechanics)
- Chapter State Story 004: Events that trigger auto-save (this story only listens to them)

---

## QA Test Cases

- **AC-1**: 1s debounce collapses multiple events
  - Given: `Evt_PuzzleStateChanged` fires at T=0, T=0.3s, T=0.7s
  - When: Time advances to T=1.7s (1s after last event)
  - Then: Exactly 1 `SaveAsync` call total; no save at T=1s (would be 0.7s+1s=1.7s)
  - Edge cases: event at T=0.9s resets timer to T=0.9+1s=1.9s

- **AC-2**: ForceSaveAsync bypasses debounce
  - Given: Debounced save pending (timer at 0.5s of 1s)
  - When: `Evt_ChapterComplete` fires
  - Then: Pending debounce cancelled; `SaveAsync` called immediately (< 1 frame delay)
  - Edge cases: `ForceSaveAsync` called from `OnApplicationPause` while a debounced save is mid-await — cancels cleanly

- **AC-3**: OnApplicationPause triggers immediate save
  - Given: `SaveManagerBridge.OnApplicationPause(true)` called
  - When: Method executes
  - Then: `ForceSaveAsync()` is called; save completes before Unity suspends (best-effort — test on device)
  - Edge cases: `OnApplicationPause(false)` (resume) — no save triggered

- **AC-4**: SaveSystem.Init() order is respected
  - Given: Boot sequence runs
  - When: Boot log inspected
  - Then: `SaveSystem.Init()` timestamp < `SaveSystem.LoadAsync()` completion timestamp < `ChapterState.Init()` timestamp
  - Edge cases: if ChapterState.Init() is called before LoadAsync completes — Assert.Fail in debug build

- **AC-5**: Listeners removed on Dispose
  - Given: `SaveManager.Dispose()` called (scene unload / app shutdown)
  - When: `Evt_PuzzleStateChanged` fires after Dispose
  - Then: No `SaveAsync` call; no NullReferenceException; listener is not invoked
  - Edge cases: `Dispose()` called with pending debounce — CancellationToken cancelled, no file write

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `tests/integration/save-system/auto_save_triggers_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (SaveData schema), Story 002 (SaveAsync implementation — must be DONE)
- Depends on: Chapter State Story 004 (events that trigger auto-save — must be DONE)
- Unlocks: Story 006 (corruption recovery uses ForceSaveAsync), full game loop end-to-end test
