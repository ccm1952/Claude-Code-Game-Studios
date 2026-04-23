// 该文件由Cursor 自动生成

# Story 001: Save Data Schema (JSON + Versioned Format)

> **Epic**: Save System
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/chapter-state-and-save.md`
**Requirement**: `TR-save-009`, `TR-save-013`, `TR-save-014`
*(Single slot JSON format; version migration chain; save file size < 10KB)*

**ADR Governing Implementation**: ADR-008: Save System
**ADR Decision Summary**: Single save slot in JSON format with embedded version number. Schema includes chapter progress, puzzle states, and metadata. Version field enables the migration chain (ISaveMigration). Settings are stored separately in PlayerPrefs — NOT in this JSON. File size target < 10KB.

**Engine**: Unity 2022.3.62f2 LTS + Newtonsoft.Json (or Unity's JsonUtility) | **Risk**: LOW
**Engine Notes**: `Application.persistentDataPath` for file location. File names: `save.json`, `save.crc`, `save.backup.json`, `save.backup.crc`. Unity's `JsonUtility` supports `[Serializable]` structs. Newtonsoft.Json is preferred for better null handling and versioning. Confirm which JSON library is in the project before implementation.

**Control Manifest Rules (this layer)**:
- Required: `Save format: Custom JSON + CRC32 checksum + backup file + atomic write` (ADR-008)
- Required: `File location: Application.persistentDataPath/ with save.json, save.crc, save.backup.json, save.backup.crc` (ADR-008)
- Required: `Version migration: sequential chain via ISaveMigration` (ADR-008)
- Required: `Settings (8 items) stored separately via PlayerPrefs — NOT in save file` (ADR-008)
- Required: `DeleteSave() clears save files but does NOT clear PlayerPrefs settings` (ADR-008)
- Forbidden: `Never store settings inside the save file` (ADR-008)

---

## Acceptance Criteria

*From GDD `design/gdd/chapter-state-and-save.md`, scoped to this story:*

- [ ] `SaveData` class is defined as a `[Serializable]` plain C# class (not MonoBehaviour, not ScriptableObject)
- [ ] `SaveData` contains: `int version` (current schema version), `string playerId` (GUID, generated on first save), `long lastSaveTimestampUtc`, `ChapterProgressData chapterProgress`
- [ ] `ChapterProgressData` contains: `int[] unlockedChapterIds`, `int[] completedChapterIds`, `PuzzleProgressEntry[] puzzleEntries`
- [ ] `PuzzleProgressEntry` contains: `int chapterId`, `int puzzleId`, `int stateOrdinal` (serialized as int, not enum string, for migration safety)
- [ ] `SaveData` serializes to JSON via `JsonUtility.ToJson()` or Newtonsoft.Json; the serialized output for a complete 5-chapter playthrough is < 10KB
- [ ] `SaveData.version` is a `public const int CURRENT_VERSION = 1` (increment with each schema breaking change)
- [ ] `ISaveMigration` interface defined: `int FromVersion { get; }`, `SaveData Migrate(SaveData old)`
- [ ] `SaveManager` maintains a list of `ISaveMigration` implementations ordered by `FromVersion`; applies them sequentially on load if saved version < `CURRENT_VERSION`
- [ ] Settings are NOT in `SaveData` — they are in `PlayerPrefs` (verified by grep: no `Settings` fields in `SaveData.cs`)

---

## Implementation Notes

*Derived from ADR-008 Implementation Guidelines:*

```csharp
[Serializable]
public class SaveData
{
    public static readonly int CURRENT_VERSION = 1;

    public int version = CURRENT_VERSION;
    public string playerId = System.Guid.NewGuid().ToString();
    public long lastSaveTimestampUtc;
    public ChapterProgressData chapterProgress = new ChapterProgressData();
}

[Serializable]
public class ChapterProgressData
{
    public int[] unlockedChapterIds = new int[0];
    public int[] completedChapterIds = new int[0];
    public PuzzleProgressEntry[] puzzleEntries = new PuzzleProgressEntry[0];
}

[Serializable]
public struct PuzzleProgressEntry
{
    public int chapterId;
    public int puzzleId;
    public int stateOrdinal; // int ordinal, not enum name, for migration safety
}

public interface ISaveMigration
{
    int FromVersion { get; }
    SaveData Migrate(SaveData old);
}
```

**File paths**:
```csharp
public static class SavePaths
{
    private static string Base => Application.persistentDataPath;
    public static string Primary    => Path.Combine(Base, "save.json");
    public static string PrimaryCrc => Path.Combine(Base, "save.crc");
    public static string Backup     => Path.Combine(Base, "save.backup.json");
    public static string BackupCrc  => Path.Combine(Base, "save.backup.crc");
    public static string Temp       => Path.Combine(Base, "save.tmp");
}
```

**Size estimate**: 5 chapters × ~5 bytes + 15 puzzles × ~20 bytes + metadata ~100 bytes = ~500 bytes total. Far below 10KB.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: Atomic write + CRC32 checksum (uses this schema but handles I/O)
- Story 003: Load fallback chain (uses this schema's version field)
- Story 005: Migration implementations (uses ISaveMigration defined here)

---

## QA Test Cases

- **AC-1**: SaveData serializes to valid JSON < 10KB
  - Given: A fully populated `SaveData` with 5 chapters complete, 15 puzzles complete
  - When: `JsonUtility.ToJson(saveData)` called
  - Then: Output is valid JSON; `Encoding.UTF8.GetByteCount(json) < 10240`
  - Edge cases: empty save (new game) — must also be valid JSON with version field

- **AC-2**: Version field defaults to CURRENT_VERSION
  - Given: `new SaveData()` created
  - When: `version` field inspected
  - Then: `version == SaveData.CURRENT_VERSION`
  - Edge cases: CURRENT_VERSION must be a positive integer ≥ 1

- **AC-3**: Settings are absent from SaveData
  - Given: `SaveData.cs` source file
  - When: Grep for common settings fields (volume, sfx_enabled, language, haptic)
  - Then: Zero matches — no settings fields in SaveData class
  - Edge cases: grep must include all nested classes (ChapterProgressData, PuzzleProgressEntry)

- **AC-4**: ISaveMigration interface is correctly defined
  - Given: `ISaveMigration.cs` compiled
  - When: A test class implements `ISaveMigration`
  - Then: Must implement `int FromVersion { get; }` and `SaveData Migrate(SaveData)` — compile error if missing
  - Edge cases: `Migrate` must accept null-safe SaveData (corrupted old saves may have missing fields)

- **AC-5**: stateOrdinal correctly maps to PuzzleState enum
  - Given: `PuzzleState.Complete == 6` (ordinal)
  - When: `PuzzleProgressEntry.stateOrdinal = 6` serialized and deserialized
  - Then: Re-cast to `(PuzzleState)6` == `PuzzleState.Complete`
  - Edge cases: unknown ordinal value in save file (e.g., from future version) → defaults to `PuzzleState.Locked` on load

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/save-system/save_data_schema_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Chapter State Story 001 (IChapterProgress interface — defines the data shape this schema serializes)
- Unlocks: Story 002 (atomic write uses this class), Story 003 (load uses this class), Story 005 (migration uses this class)
