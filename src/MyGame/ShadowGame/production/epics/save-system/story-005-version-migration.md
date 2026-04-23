// 该文件由Cursor 自动生成

# Story 005: ISaveMigration Chain (v1→v2→v3→...)

> **Epic**: Save System
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/chapter-state-and-save.md`
**Requirement**: `TR-save-013`
*(Version migration chain)*

**ADR Governing Implementation**: ADR-008: Save System
**ADR Decision Summary**: Save schema versioning uses a sequential migration chain. Each `ISaveMigration` handles one version step (e.g., v1→v2). `SaveManager` applies all necessary migrations in order after a successful load. `SaveData.CURRENT_VERSION` is the authoritative current version number. Migrations are pure functions — no side effects, no I/O.

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW
**Engine Notes**: For MVP (v1), no migration implementations are needed — the chain is empty. The infrastructure must be in place for future versions. `ISaveMigration` is defined in Story 001; this story implements the runner and adds the v1→v2 migration as a placeholder/example for the first schema change.

**Control Manifest Rules (this layer)**:
- Required: `Version migration: sequential chain (v1→v2→v3→vCurrent) via ISaveMigration` (ADR-008)
- Forbidden: `Never hand-edit any file in GameProto` (ADR-007) — migration code lives in GameLogic assembly

---

## Acceptance Criteria

*From GDD `design/gdd/chapter-state-and-save.md`, scoped to this story:*

- [ ] `SaveManager` maintains a `List<ISaveMigration>` ordered by `FromVersion` ascending
- [ ] `ApplyMigrations(SaveData data)` method iterates through migrations whose `FromVersion >= data.version` and < `CURRENT_VERSION`, applying each in order
- [ ] After applying all applicable migrations, `data.version` is updated to `CURRENT_VERSION`
- [ ] If `data.version > CURRENT_VERSION` (save from a newer app version loaded in an older app): log `Debug.LogWarning` and return data as-is (no migration applied — forward compatibility is not guaranteed)
- [ ] If `data.version == CURRENT_VERSION`: no migration needed; `ApplyMigrations` is a pass-through
- [ ] Migration is a pure function: receives `SaveData`, returns new `SaveData` — no static state, no I/O
- [ ] A test migration (v1→v2 stub) is implemented and registered: it adds a placeholder field and increments version; verifiable in unit tests
- [ ] If a migration throws, the exception is caught; `Debug.LogError` logged; original (pre-migration) data returned as fallback

---

## Implementation Notes

*Derived from ADR-008 Implementation Guidelines:*

```csharp
// SaveManager migration runner
private SaveData ApplyMigrations(SaveData data)
{
    if (data.version > SaveData.CURRENT_VERSION) {
        Debug.LogWarning($"[SaveManager] Save version {data.version} > current {SaveData.CURRENT_VERSION}. No migration applied.");
        return data;
    }

    // Order migrations by FromVersion ascending
    var applicable = _migrations
        .Where(m => m.FromVersion >= data.version && m.FromVersion < SaveData.CURRENT_VERSION)
        .OrderBy(m => m.FromVersion);

    foreach (var migration in applicable) {
        try {
            data = migration.Migrate(data);
            data.version = migration.FromVersion + 1;
        } catch (Exception e) {
            Debug.LogError($"[SaveManager] Migration from v{migration.FromVersion} failed: {e.Message}. Using pre-migration data.");
            return data;
        }
    }
    return data;
}

// Registration (in SaveManager constructor or Init):
_migrations = new List<ISaveMigration> {
    // new SaveMigration_V1_To_V2(), // Add when first schema change occurs
};

// Example migration (for testing the framework — remove before shipping):
public class SaveMigration_V1_To_V2_Test : ISaveMigration
{
    public int FromVersion => 1;

    public SaveData Migrate(SaveData old)
    {
        // Example: add a new field with a default value
        // old.someNewField = defaultValue;
        return old;
    }
}
```

**MVP note**: For MVP with `CURRENT_VERSION = 1`, the migrations list is empty. The runner is tested with a mock migration (v1→v2) in unit tests, then the mock is not registered in production. When the schema changes in a future sprint, a real migration is added.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: `ISaveMigration` interface and `SaveData.CURRENT_VERSION` definitions
- Story 003: `LoadAsync` which calls `ApplyMigrations` (the runner lives here but is called from Story 003)

---

## QA Test Cases

- **AC-1**: No migration needed (same version)
  - Given: `SaveData.version == CURRENT_VERSION` (no outdated save)
  - When: `ApplyMigrations(data)` called
  - Then: Data returned unchanged; version unchanged; zero migrations invoked
  - Edge cases: `_migrations` list is empty — no crash, data passes through

- **AC-2**: Single migration applied correctly
  - Given: `data.version = 1`; `CURRENT_VERSION = 2`; `SaveMigration_V1_To_V2` registered
  - When: `ApplyMigrations(data)` called
  - Then: `SaveMigration_V1_To_V2.Migrate(data)` called once; returned data has `version == 2`
  - Edge cases: migration called with null fields in old data (corrupt save partially loaded) — must not throw

- **AC-3**: Multi-step chain (v1→v2→v3)
  - Given: `data.version = 1`; `CURRENT_VERSION = 3`; migrations for v1→v2 and v2→v3 registered
  - When: `ApplyMigrations(data)` called
  - Then: Both migrations applied in order; final `data.version == 3`
  - Edge cases: registering migrations out of order — runner sorts by `FromVersion` ascending

- **AC-4**: Future save version — no migration, warning logged
  - Given: `data.version = 99`; `CURRENT_VERSION = 1`
  - When: `ApplyMigrations(data)` called
  - Then: `Debug.LogWarning` logged; data returned unchanged; no migrations invoked
  - Edge cases: `data.version == CURRENT_VERSION + 1` — still triggers the forward-compat warning

- **AC-5**: Migration exception fallback
  - Given: A migration that throws `InvalidOperationException`
  - When: `ApplyMigrations(data)` processes that migration
  - Then: Exception caught; `Debug.LogError` logged; pre-migration `data` returned (safe fallback)
  - Edge cases: if multiple migrations and mid-chain throws — partial state (already migrated steps) is returned, not reverted

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/save-system/version_migration_test.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (ISaveMigration interface), Story 003 (ApplyMigrations called from LoadAsync)
- Unlocks: Future schema changes can be shipped safely; no save-file breaking changes without a migration
