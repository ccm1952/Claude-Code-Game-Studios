// 该文件由Cursor 自动生成

# Story 002: Atomic Write + CRC32 Checksum + Backup File

> **Epic**: Save System
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/chapter-state-and-save.md`
**Requirement**: `TR-save-010`, `TR-save-011`, `TR-save-012`, `TR-save-015`
*(Atomic write temp→rename; backup .backup.json; CRC32 checksum; Save < 50ms)*

**ADR Governing Implementation**: ADR-008: Save System
**ADR Decision Summary**: Save writes atomically via temp file → CRC verify → rename. A backup copy is maintained after each successful write. CRC32 checksum stored in a paired `.crc` file. All I/O is async via UniTask. The entire save operation must complete in < 50ms.

**Engine**: Unity 2022.3.62f2 LTS + UniTask 2.5.10 | **Risk**: LOW
**Engine Notes**: `System.IO.File.WriteAllTextAsync` wrapped in UniTask. CRC32 implemented via `System.IO.Hashing.Crc32` (.NET 6+) or a simple lookup-table implementation if not available. Atomic rename: `File.Move(temp, target, overwrite: true)` — available in .NET 5+. Verify `File.Move` overwrite parameter availability in Unity 2022's .NET SDK version.

**Control Manifest Rules (this layer)**:
- Required: `Atomic write: write to .tmp → verify CRC → rename to .json` (ADR-008)
- Required: `All save I/O via UniTask — no synchronous file I/O` (ADR-008)
- Required: `Save file < 10KB, Save < 50ms, Load < 100ms` (ADR-008)
- Forbidden: `Never use Coroutines for save I/O` (ADR-008)
- Forbidden: `Never skip CRC32 verification on load` (ADR-008)

---

## Acceptance Criteria

*From GDD `design/gdd/chapter-state-and-save.md`, scoped to this story:*

- [ ] `SaveManager.SaveAsync(SaveData data)` is a `UniTask` method; it is never called synchronously
- [ ] Write sequence: (1) Serialize `SaveData` to JSON string, (2) Compute CRC32 of JSON bytes, (3) Write JSON to `save.tmp`, (4) Write CRC to `save.crc.tmp`, (5) Rename `save.tmp → save.json`, (6) Rename `save.crc.tmp → save.crc`, (7) Copy `save.json → save.backup.json`, (8) Copy `save.crc → save.backup.crc`
- [ ] If any step fails: log error; do NOT partially overwrite save files (temp files are left for debugging; primary and backup remain intact from last successful write)
- [ ] `SaveAsync` performance: completes in < 50ms on mid-range mobile (measured via `Stopwatch`)
- [ ] CRC32 is computed over the raw UTF-8 bytes of the JSON string (not the file bytes including BOM)
- [ ] The `.crc` file contains only the hex string of the CRC32 value (e.g., `"A3F2C1D4"`)
- [ ] Calling `SaveAsync` concurrently (two callers) must not corrupt the file — use a `SemaphoreSlim(1,1)` guard
- [ ] After successful save, `_lastSaveTimestampUtc` in `SaveData` is updated to `DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()`

---

## Implementation Notes

*Derived from ADR-008 Implementation Guidelines:*

```csharp
public class SaveManager
{
    private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

    public async UniTask SaveAsync(SaveData data)
    {
        await _writeLock.WaitAsync();
        try {
            data.lastSaveTimestampUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string json = JsonUtility.ToJson(data, prettyPrint: false);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            string crc = ComputeCrc32Hex(bytes);

            // Write temp files
            await File.WriteAllTextAsync(SavePaths.Temp, json);
            await File.WriteAllTextAsync(SavePaths.Temp + ".crc", crc);

            // Atomic rename
            File.Move(SavePaths.Temp, SavePaths.Primary, overwrite: true);
            File.Move(SavePaths.Temp + ".crc", SavePaths.PrimaryCrc, overwrite: true);

            // Backup copy
            File.Copy(SavePaths.Primary, SavePaths.Backup, overwrite: true);
            File.Copy(SavePaths.PrimaryCrc, SavePaths.BackupCrc, overwrite: true);
        }
        catch (Exception e) {
            Debug.LogError($"[SaveManager] Save failed: {e.Message}");
        }
        finally {
            _writeLock.Release();
        }
    }

    private string ComputeCrc32Hex(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in data) {
            crc = (crc >> 8) ^ CrcTable[(crc ^ b) & 0xFF];
        }
        return (~crc).ToString("X8");
    }
}
```

**SemaphoreSlim guard**: prevents concurrent writes from race-corrupting temp files. If `debounce` already prevents rapid calls, the semaphore is a safety net, not the primary throttle.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: SaveData schema definition
- Story 003: Load and CRC verification (reading uses the CRC stored here)
- Story 004: Auto-save triggers and debounce (when SaveAsync is called)
- Story 006: Corruption recovery (what happens when CRC verification fails)

---

## QA Test Cases

- **AC-1**: Atomic write — no partial overwrite on failure
  - Given: Simulated I/O failure during step 5 (rename)
  - When: `SaveAsync` throws mid-rename
  - Then: `save.json` retains its last valid content; `save.backup.json` is intact; no truncated file
  - Edge cases: failure at step 3 (write temp) — primary and backup unaffected; temp file may exist (cleanup on next write)

- **AC-2**: CRC32 is correct
  - Given: JSON string `{"version":1}`
  - When: `ComputeCrc32Hex(Encoding.UTF8.GetBytes(json))` called
  - Then: Result matches known CRC32 for that byte sequence (verifiable via standard CRC32 calculator)
  - Edge cases: empty string → CRC32 must return a valid value (not crash)

- **AC-3**: Backup files updated after each successful save
  - Given: First save completes successfully
  - When: `save.backup.json` is read
  - Then: Content matches `save.json`; `save.backup.crc` matches `save.crc`
  - Edge cases: second save — backup reflects second save, not first

- **AC-4**: Save completes within 50ms
  - Given: SaveData with 5 chapters and 15 puzzles fully populated
  - When: `SaveAsync` measured via `Stopwatch`
  - Then: Elapsed time ≤ 50ms on target device
  - Edge cases: concurrent second call during first write — second call waits on semaphore (total time ≤ 100ms)

- **AC-5**: Concurrent write guard prevents race condition
  - Given: Two concurrent `SaveAsync` calls
  - When: Both `await SaveAsync` simultaneously
  - Then: Second call waits for first to complete; both complete without file corruption; final `save.json` has the content of the LATER call
  - Edge cases: first save fails — lock still releases; second call proceeds normally

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/save-system/write_with_integrity_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (SaveData class — must be DONE)
- Unlocks: Story 003 (CRC files written here are verified in load), Story 006 (corruption recovery reads these files)
