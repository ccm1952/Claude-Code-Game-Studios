// 该文件由Cursor 自动生成

# Story 006: Corruption Detection + Recovery Flow

> **Epic**: Save System
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/chapter-state-and-save.md`
**Requirement**: `TR-save-020`
*(Corrupted save fallback chain)*

**ADR Governing Implementation**: ADR-008: Save System
**ADR Decision Summary**: Corruption is detected at load time via CRC32 mismatch or JSON parse failure. Recovery follows a three-tier fallback: primary → backup → fresh start. If both primary and backup are corrupt, the player is offered a "New Game" option with a confirmation dialog. `DeleteSave()` clears save files but NOT PlayerPrefs settings.

**Engine**: Unity 2022.3.62f2 LTS + UniTask 2.5.10 | **Risk**: LOW
**Engine Notes**: Corruption recovery UI is triggered by `Evt_SaveCorruptionDetected` event (fired in Story 003). UI handling is the UI System's concern — this story defines the event, the `DeleteSave()` method, and the corruption data model. The recovery confirmation UI is out of scope for this epic.

**Control Manifest Rules (this layer)**:
- Required: `Load fallback chain: primary → backup → fresh start` (ADR-008)
- Required: `Never skip CRC32 verification on load` (ADR-008)
- Required: `DeleteSave() clears save files but does NOT clear PlayerPrefs settings` (ADR-008)
- Required: `All save I/O via UniTask — no synchronous file I/O` (ADR-008)
- Forbidden: `Never use Coroutines for save I/O` (ADR-008)
- Forbidden: `Never store settings inside the save file` (ADR-008) — DeleteSave must not touch PlayerPrefs

---

## Acceptance Criteria

*From GDD `design/gdd/chapter-state-and-save.md`, scoped to this story:*

- [ ] Corruption is detected when: (a) CRC32 mismatch on `save.crc` vs file contents, (b) `save.json` is not valid JSON (parse exception), (c) `save.json` or `save.crc` file is missing
- [ ] `Evt_SaveCorruptionDetected` (defined in `EventId.cs`, range 1900–1999) fires when both primary AND backup are corrupt or missing
- [ ] `Evt_SaveCorruptionDetected` payload: `SaveCorruptionPayload { bool PrimaryCorrupt, bool BackupCorrupt, string ErrorMessage }`
- [ ] `SaveManager.DeleteSave()` is an async method (UniTask) that: (1) Deletes `save.json`, `save.crc`, `save.backup.json`, `save.backup.crc`, and `save.tmp` if they exist; (2) Does NOT call `PlayerPrefs.DeleteAll()` or `PlayerPrefs.DeleteKey()` for any settings; (3) Fires `Evt_SaveDeleted` after completion
- [ ] `SaveManager.DeleteSave()` is idempotent: calling it when no save files exist completes without error
- [ ] A save-round-trip test: (1) Write valid save, (2) Corrupt primary CRC manually, (3) `LoadAsync()` → falls back to backup → returns correct data; primary is NOT deleted (backup stays intact for next load)
- [ ] A full-corruption test: (1) Corrupt both saves, (2) `LoadAsync()` → fires `Evt_SaveCorruptionDetected` → returns null; (3) `DeleteSave()` → all 5 files removed; (4) new `SaveAsync()` creates fresh save successfully

---

## Implementation Notes

*Derived from ADR-008 Implementation Guidelines:*

```csharp
// In EventId.cs (1900–1999 range):
/// <summary>
/// Both primary and backup saves are corrupt. Sender: SaveManager. Listeners: UICorruptionDialog.
/// Payload: SaveCorruptionPayload. Cascade: none.
/// </summary>
public const int Evt_SaveCorruptionDetected = 1900;

/// <summary>
/// Save files deleted. Sender: SaveManager. Listeners: ChapterState (reset to fresh).
/// Payload: SaveDeletedPayload. Cascade: ChapterState.Init(null).
/// </summary>
public const int Evt_SaveDeleted = 1901;

// DeleteSave implementation:
public async UniTask DeleteSave()
{
    var filesToDelete = new[] {
        SavePaths.Primary, SavePaths.PrimaryCrc,
        SavePaths.Backup, SavePaths.BackupCrc,
        SavePaths.Temp
    };

    foreach (var path in filesToDelete) {
        if (File.Exists(path)) {
            File.Delete(path); // Sync delete is fine for small files post-save
        }
    }

    // IMPORTANT: Do NOT touch PlayerPrefs
    // PlayerPrefs.DeleteAll(); // ← FORBIDDEN — settings survive DeleteSave

    GameEvent.Send(EventId.Evt_SaveDeleted, new SaveDeletedPayload());
    await UniTask.Yield(); // Ensure event is dispatched before return
}
```

**Why primary is not deleted on backup fallback**: Keeping the corrupted primary for a session allows post-mortem debugging. On the next successful `SaveAsync`, primary is overwritten with fresh data. Backup is promoted on next successful write.

**Safe corruption detection boundary**: Corruption is detected in `TryLoadFile` (Story 003). This story adds the event and `DeleteSave`. The recovery UI (confirmation dialog) is the UI System's responsibility and out of scope here.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 003: `TryLoadFile` which detects corruption and returns null (the detection mechanism)
- UI System: The confirmation dialog that fires `DeleteSave()` on player confirmation
- Story 002: Write integrity that prevents corruption from the write side

---

## QA Test Cases

- **AC-1**: Corruption detection: CRC mismatch
  - Given: `save.json` valid; `save.crc` contains wrong checksum
  - When: `LoadAsync()` called
  - Then: Primary rejected; backup attempted; `Debug.LogWarning` logged
  - Edge cases: also test JSON parse failure (truncated file, invalid encoding)

- **AC-2**: Both corrupt → Evt_SaveCorruptionDetected fires
  - Given: Both `save.json` and `save.backup.json` have CRC mismatches
  - When: `LoadAsync()` called
  - Then: `Evt_SaveCorruptionDetected` fires with `PrimaryCorrupt=true, BackupCorrupt=true`; `LoadAsync` returns null
  - Edge cases: `Evt_SaveCorruptionDetected` fires exactly once (not on each corrupt file individually)

- **AC-3**: DeleteSave removes all 5 files, not PlayerPrefs
  - Given: `save.json`, `save.crc`, `save.backup.json`, `save.backup.crc`, `save.tmp` all exist; PlayerPrefs has `volume=0.8`
  - When: `DeleteSave()` called
  - Then: All 5 files deleted; `File.Exists(SavePaths.Primary) == false`; `PlayerPrefs.GetFloat("volume") == 0.8f` (unchanged)
  - Edge cases: only some files exist — deletes only those that exist; no exception on missing file

- **AC-4**: DeleteSave is idempotent
  - Given: No save files exist
  - When: `DeleteSave()` called twice
  - Then: No exception; `Evt_SaveDeleted` fires on each call (or only on first — document the choice)
  - Edge cases: `DeleteSave()` called while a `SaveAsync` is in progress — define behavior (cancel save? wait? document)

- **AC-5**: Full corruption recovery round-trip
  - Given: Valid save exists for Chapter 1 complete
  - When: (1) Manually corrupt both CRC files, (2) `LoadAsync()` → null + event, (3) `DeleteSave()`, (4) New game starts (ChapterState.Init(null)), (5) Progress to Chapter 1 puzzle complete, (6) Auto-save triggers, (7) `LoadAsync()` next session
  - Then: New save reflects Chapter 1 progress correctly; no residual corrupt files
  - Edge cases: if app crashes between step 3 (DeleteSave) and step 6 (new save) — next boot has no save files → fresh game (correct)

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/save-system/corruption_recovery_test.cs` — must exist and pass (includes full round-trip test)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (SaveData schema), Story 002 (write creates the files being deleted), Story 003 (detection fires the event in this story's EventId range)
- Unlocks: Full save system is now complete; all 5 auto-save + recovery paths verified
