// 该文件由Cursor 自动生成

# Story 003: Load Fallback Chain (Primary → Backup → Fresh Start)

> **Epic**: Save System
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/chapter-state-and-save.md`
**Requirement**: `TR-save-020`, `TR-save-015`
*(Corrupted save fallback chain; Load < 100ms)*

**ADR Governing Implementation**: ADR-008: Save System
**ADR Decision Summary**: Load attempts primary save first; if CRC fails or file missing, falls back to backup; if both fail, starts fresh. All I/O is async via UniTask. CRC32 verification is mandatory on every load. Migration chain runs after successful load if version < current.

**Engine**: Unity 2022.3.62f2 LTS + UniTask 2.5.10 | **Risk**: LOW
**Engine Notes**: `System.IO.File.ReadAllTextAsync` wrapped in UniTask. File existence check via `File.Exists()`. CRC32 computed identically to Story 002 for consistency.

**Control Manifest Rules (this layer)**:
- Required: `Load fallback chain: primary → backup → fresh start` (ADR-008)
- Required: `Never skip CRC32 verification on load` (ADR-008)
- Required: `All save I/O via UniTask — no synchronous file I/O` (ADR-008)
- Forbidden: `Never use Coroutines for save I/O` (ADR-008)

---

## Acceptance Criteria

*From GDD `design/gdd/chapter-state-and-save.md`, scoped to this story:*

- [ ] `SaveManager.LoadAsync()` returns `SaveData?` (nullable — null means fresh start)
- [ ] Load sequence: (1) If `save.json` exists → read + verify CRC32 → if valid, return deserialized `SaveData`. (2) If primary missing or CRC invalid → attempt `save.backup.json` with same CRC check. (3) If backup also missing or invalid → return `null` (fresh start)
- [ ] CRC32 verification: read `save.crc`; recompute CRC from loaded JSON bytes; if mismatch → treat as corrupt
- [ ] After successful load: apply migration chain if `saveData.version < SaveData.CURRENT_VERSION` (Story 005 handles migrations)
- [ ] `LoadAsync` performance: completes in < 100ms on target device (measured via Stopwatch)
- [ ] On fallback to backup: `Debug.LogWarning("[SaveManager] Primary save corrupt; falling back to backup.")`
- [ ] On fallback to fresh start: `Debug.LogWarning("[SaveManager] Both saves corrupt; starting fresh.")`; optionally fire `Evt_SaveCorruptionDetected` event for UI notification
- [ ] Partial file (file exists but is not valid JSON) is treated the same as CRC failure (caught by JSON parse exception → fallback)

---

## Implementation Notes

*Derived from ADR-008 Implementation Guidelines:*

```csharp
public async UniTask<SaveData?> LoadAsync()
{
    var sw = Stopwatch.StartNew();

    SaveData? result = await TryLoadFile(SavePaths.Primary, SavePaths.PrimaryCrc);
    if (result != null) {
        Debug.Log($"[SaveManager] Loaded primary save in {sw.ElapsedMilliseconds}ms");
        return ApplyMigrations(result.Value);
    }

    Debug.LogWarning("[SaveManager] Primary save corrupt; falling back to backup.");
    result = await TryLoadFile(SavePaths.Backup, SavePaths.BackupCrc);
    if (result != null) {
        Debug.LogWarning("[SaveManager] Loaded backup save.");
        return ApplyMigrations(result.Value);
    }

    Debug.LogWarning("[SaveManager] Both saves corrupt; starting fresh.");
    GameEvent.Send(EventId.Evt_SaveCorruptionDetected, new SaveCorruptionPayload());
    return null;
}

private async UniTask<SaveData?> TryLoadFile(string jsonPath, string crcPath)
{
    if (!File.Exists(jsonPath) || !File.Exists(crcPath)) return null;

    try {
        string json = await File.ReadAllTextAsync(jsonPath);
        string storedCrc = (await File.ReadAllTextAsync(crcPath)).Trim();
        string computedCrc = ComputeCrc32Hex(Encoding.UTF8.GetBytes(json));

        if (storedCrc != computedCrc) return null;

        return JsonUtility.FromJson<SaveData>(json);
    } catch (Exception e) {
        Debug.LogWarning($"[SaveManager] Load error for {jsonPath}: {e.Message}");
        return null;
    }
}
```

`ApplyMigrations` is implemented in Story 005 — `LoadAsync` calls it but doesn't implement it here.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: How the CRC files are written (this story only reads them)
- Story 005: ISaveMigration chain (this story calls `ApplyMigrations` but does not implement it)
- Story 006: UI/user-facing corruption recovery flow (this story only fires the event)

---

## QA Test Cases

- **AC-1**: Valid primary save loads correctly
  - Given: `save.json` exists with valid JSON; `save.crc` matches
  - When: `LoadAsync()` called
  - Then: Returns non-null `SaveData`; `version` and all fields match saved data
  - Edge cases: file exists but is empty → JSON parse fails → treats as corrupt

- **AC-2**: CRC mismatch on primary → falls back to backup
  - Given: `save.json` exists; `save.crc` contains wrong CRC value
  - When: `LoadAsync()` called
  - Then: Primary rejected; backup attempted; `Debug.LogWarning` about primary corruption
  - Edge cases: backup also has wrong CRC → fresh start

- **AC-3**: Missing primary file → falls back to backup
  - Given: `save.json` does not exist; `save.backup.json` exists and valid
  - When: `LoadAsync()` called
  - Then: Backup loaded successfully; returns valid `SaveData`
  - Edge cases: both files missing → returns `null`

- **AC-4**: Both corrupt → fresh start + event
  - Given: Both `save.json` and `save.backup.json` have CRC mismatches
  - When: `LoadAsync()` called
  - Then: Returns `null`; `Evt_SaveCorruptionDetected` fires; `Debug.LogWarning` logged
  - Edge cases: event listener count = 0 → no crash (GameEvent.Send is fire-and-forget)

- **AC-5**: Load completes within 100ms
  - Given: Valid primary save file exists (< 10KB)
  - When: `LoadAsync()` timed via `Stopwatch`
  - Then: `elapsed ≤ 100ms` on target device
  - Edge cases: backup fallback (reads two files) still ≤ 100ms

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/save-system/load_fallback_chain_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (SaveData schema), Story 002 (CRC files format)
- Unlocks: Story 005 (migration called from LoadAsync), Story 006 (corruption events used in recovery UI)
