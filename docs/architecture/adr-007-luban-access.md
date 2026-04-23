// 该文件由Cursor 自动生成

# ADR-007: Luban Config Table Access Pattern

## Status

Proposed

## Date

2026-04-22

## Last Verified

2026-04-22

## Decision Makers

Technical Director, Lead Programmer

## Summary

All gameplay values in《影子回忆 (Shadow Memory)》must be data-driven (P1: Data-Driven Everything). Luban generates type-safe C# data classes and a `Tables` singleton from Excel/JSON schemas. This ADR standardizes: (1) the access pattern for reading config data at runtime, (2) the initialization timing relative to HybridCLR assembly loading, (3) the thread-safety model for `Tables` in UniTask async contexts, (4) the complete list of config tables derived from GDDs, and (5) the forbidden patterns that hardcode gameplay values.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.62f2 (LTS) |
| **Domain** | Config / Data Pipeline / Code Generation |
| **Knowledge Risk** | MEDIUM — Luban code generation internals and `Tables` singleton initialization API may have evolved beyond LLM training data; must verify from project's Luban output and GameProto assembly |
| **References Consulted** | ADR-004 (HybridCLR Assembly Boundary), architecture document Section 5.1 init order, Luban official documentation, GameProto assembly structure |
| **Post-Cutoff APIs Used** | `Tables.Instance` singleton access, `Tables.Init()` / `Tables.LoadAsync()` (exact init API to be verified Sprint 0), per-table `TbXXX.Get(id)` lookup |
| **Verification Required** | Sprint 0 spike: confirm `Tables` singleton initialization API; measure Init() duration on mid-range mobile device; verify hot-update of GameProto-only DLL produces correct config reload |

> **Note**: If Knowledge Risk is MEDIUM or HIGH, this ADR must be re-validated if the
> project upgrades engine or Luban versions. Flag it as "Superseded" and write a new ADR.

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-004 (HybridCLR Assembly Boundary Rules — GameProto assembly contains all Luban generated code; GameLogic references GameProto at compile time) |
| **Enables** | All Feature layer systems (Shadow Puzzle, Narrative, Hint, Audio, Tutorial, etc.) — they read configs via the pattern defined here |
| **Blocks** | Any system that needs gameplay config data; no system may read Luban tables until this pattern is established |
| **Ordering Note** | ADR-004 must reach Accepted first (assembly boundaries must exist). This ADR must reach Accepted before any system implementation that reads config values. |

## Context

### Problem Statement

《影子回忆》's architecture pillar P1 (Data-Driven Everything) mandates that **all gameplay values** — thresholds, timing, formula coefficients, puzzle configurations, narrative sequences, audio events, hint parameters, tutorial steps — come from Luban-generated config tables. The code layer implements behavior logic only; it must never contain hardcoded numeric constants for gameplay tuning.

Luban generates C# data classes and a `Tables` singleton from Excel/JSON source files. These generated files live in the GameProto assembly (ADR-004). Gameplay code in the GameLogic assembly reads config data at runtime via `Tables.Instance.TbXXX.Get(id)`.

Without a standardized access pattern, the following problems emerge:

1. **Hardcoded values creep in** — Developers embed "temporary" magic numbers (`float threshold = 0.85f;`) that never get replaced with config reads, violating P1 and making designer tuning impossible without code changes
2. **Init timing errors** — If a system reads `Tables.Instance` before `Tables.Init()` completes, it gets null reference exceptions or stale data. The init must happen after HybridCLR loads the GameProto DLL (Step 3 in boot sequence) but before any gameplay system starts
3. **Thread safety ambiguity** — Multiple systems use `async UniTask` methods. Without clarity on whether `Tables` is safe for concurrent reads, developers may add unnecessary synchronization (hurting performance) or skip needed synchronization (causing data races)
4. **Generated code modification** — Developers unfamiliar with Luban may hand-edit generated files, which are silently overwritten on the next Luban regeneration, causing mysterious regressions
5. **Table list drift** — Without an authoritative list of which tables exist and what they contain, duplicate or inconsistent table definitions emerge across GDD documents

### Current State

ADR-004 established the three-assembly split (Default / GameLogic / GameProto) and placed all Luban-generated code in the GameProto assembly. The architecture document (Section 5.1) specifies `Tables.Init()` as Step 7 in the init order. Open Question #4 in the architecture document asks about `Tables` thread safety in async contexts. No formal access pattern or table inventory exists yet.

### Constraints

- **P1 pillar**: All gameplay values from config — zero hardcoded gameplay constants in GameLogic
- **GameProto assembly**: Generated code is 100% Luban output; hand-editing is forbidden (ADR-004 Rule 3)
- **HybridCLR boot order**: GameProto DLL must be loaded (Step 3) before `Tables.Init()` (Step 7) can execute
- **Mobile performance**: Config data must be loaded quickly (~100ms budget) and accessed without allocation on hot paths
- **Hot-update**: Config-only changes must be deployable by rebuilding and pushing only the GameProto DLL, without touching GameLogic
- **UniTask async**: All gameplay systems use UniTask for async operations; config reads happen in async method bodies

### Open Questions Resolved

| # | Question | Resolution |
|---|----------|------------|
| OQ-4 (Architecture) | Thread safety of `Tables` singleton in async contexts | **Safe without synchronization** — Tables data is immutable after `Init()`. UniTask runs on Unity's main-thread `SynchronizationContext` by default. No concurrent mutation, no synchronization needed. See Thread Safety section. |

## Decision

**Standardize all config access through Luban's `Tables.Instance.TbXXX.Get(id)` pattern, with strict init ordering, immutable post-init data, and an absolute prohibition on hardcoded gameplay values.**

### 1. Access Pattern

All gameplay systems read config data through the Luban-generated `Tables` singleton:

```csharp
// Standard read: get a specific config entry by ID
var puzzleConfig = Tables.Instance.TbPuzzle.Get(puzzleId);
float threshold = puzzleConfig.PerfectMatchThreshold;
float snapSpeed = puzzleConfig.SnapAnimationDuration;

// Iterate all entries in a table
foreach (var chapter in Tables.Instance.TbChapter.DataList)
{
    // ...
}

// Null-safe pattern for optional lookups
var hintOverride = Tables.Instance.TbHintConfig.GetOrDefault(chapterId);
if (hintOverride != null)
{
    delay *= hintOverride.HintDelayMultiplier;
}
```

**Access Rules**:

| Rule | Description |
|------|-------------|
| **Single entry point** | All config reads go through `Tables.Instance` — no caching `Tables` references in fields, no passing table objects across method boundaries |
| **Read-only** | Config data objects are treated as immutable after `Init()`. Never modify a field on a Luban-generated data object at runtime. |
| **ID-based lookup** | Use `TbXXX.Get(id)` for specific entries. The `id` typically comes from another config table, a save file, or a GameEvent payload. |
| **No reflection** | Access config fields by their generated C# property names, never by string reflection |
| **Extension methods for derived values** | If gameplay logic needs a calculated value from config fields (e.g., `threshold * difficultyMultiplier`), write an extension method in GameLogic — never modify GameProto |

### 2. Initialization Timing

`Tables.Init()` executes as Step 7 in the boot sequence, within `ProcedureMain` (which runs in the GameLogic hot-update assembly):

```
Boot Sequence (from ADR-004):
────────────────────────────
  1. Unity loads BootScene (Default assembly, AOT)
  2. TEngine RootModule.Init()
  3. ProcedureLoadAssembly:
     ├── HybridCLR AOT metadata registration
     ├── Assembly.Load(GameProto.dll)
     └── Assembly.Load(GameLogic.dll)
  4-6. YooAsset init, manifest update, resource patches

Hot-Fix Phase (ProcedureMain, GameLogic assembly):
──────────────────────────────────────────────────
  7. ★ Tables.Init() ← ALL CONFIG TABLES LOADED HERE
  8. MainScene: LoadSceneAsync("MainScene", Additive)
  9-10. Foundation layer init (Input, Object Interaction)
  11. SaveSystem.LoadAsync()
  12-15. Feature/Meta layer init (ChapterState, Audio, Hint, etc.)
  16. ShowMainMenu
```

**Timing Invariants**:

1. `Tables.Init()` MUST complete before ANY system reads config data (Steps 9–16 all depend on Tables)
2. `Tables.Init()` MUST execute AFTER GameProto DLL is loaded (Step 3) — the `Tables` class lives in GameProto
3. `Tables.Init()` is a **synchronous blocking call** during boot — no system initializes concurrently
4. If `Tables.Init()` fails (corrupt data, missing table), boot sequence halts with an error screen — no graceful degradation

**Init Implementation** (in ProcedureMain):

```csharp
// ProcedureMain.OnEnter() — runs in GameLogic assembly after DLL load
private async UniTaskVoid StartGameAsync()
{
    // Step 7: Load all config tables
    Tables.Instance.Init(LoadConfigBytes);
    
    // Steps 8+: Safe to read config from here on
    await LoadMainSceneAsync();
    InitFoundationSystems();
    await InitFeatureSystems();
    // ...
}

// Adapter: Luban needs a byte-loader; we route through YooAsset
private byte[] LoadConfigBytes(string tableName)
{
    var handle = GameModule.Resource.LoadRawFileSync($"Config/{tableName}");
    return handle.GetRawFileData();
}
```

### 3. Thread Safety Model

**Tables data is immutable after Init(). No synchronization is needed for reads.**

| Property | Guarantee | Explanation |
|----------|-----------|-------------|
| **Immutability** | YES | `Tables.Init()` populates all data structures once. No runtime mutations. No write operations after boot. |
| **UniTask safety** | YES | UniTask uses Unity's `SynchronizationContext`, so `await` continuations resume on the main thread. Even though code reads "async", config reads execute sequentially on the main thread. |
| **Multi-threaded reads** | SAFE (but unnecessary) | Since data is immutable and all fields are initialized before any reader starts, concurrent reads from background threads (e.g., `UniTask.RunOnThreadPool`) are safe by the Java Memory Model's "effectively immutable" principle. However, no gameplay system should need background-thread config access. |
| **Hot-reload safety** | NOT SUPPORTED at runtime | `Tables.Init()` runs once at boot. If GameProto is hot-updated, the app must restart (or re-run `Tables.Init()` in a controlled reload sequence). Runtime re-init of Tables while systems are reading is NOT safe. |

**Resolves Open Question #4**: No additional synchronization mechanism is needed. UniTask async reads are safe because (a) Tables is immutable post-init, and (b) UniTask continuations execute on the main thread.

### 4. Config Table Inventory

Complete list of Luban-generated tables derived from GDD requirements:

| Table | Source GDD | Purpose | Key Fields |
|-------|-----------|---------|------------|
| **TbChapter** | game-concept, chapter-state | Chapter definitions, scene mappings | `chapterId`, `sceneName`, `bgmAsset`, `puzzleIds[]`, `unlockCondition` |
| **TbPuzzle** | shadow-puzzle-system | Per-puzzle config, thresholds, anchor weights | `puzzleId`, `perfectMatchThreshold`, `nearMatchThreshold`, `anchorWeights[]`, `snapAnimationDuration` |
| **TbPuzzleObject** | object-interaction | Per-object puzzle data | `objectId`, `puzzleId`, `objectType`, `gridSize`, `rotationStep`, `snapSpeed`, `interactionBounds` |
| **TbNarrativeSequence** | narrative-event-system | Narrative sequence configs, atomic effects | `sequenceId`, `triggerEvent`, `effects[]`, `lockInput`, `duckAudio` |
| **TbChapterTransition** | narrative-event-system | Chapter transition sequence configs | `fromChapterId`, `toChapterId`, `transitionSequenceId`, `mergeWithPuzzle` |
| **TbAudioEvent** | audio-system | SFX/Music event definitions | `audioId`, `clipAsset`, `layer`, `volume`, `variants[]`, `cooldown` |
| **TbHintConfig** | hint-system | Hint timing and threshold overrides per chapter | `chapterId`, `hintDelayMultiplier`, `l1Threshold`, `l2Threshold`, `l3MaxUses` |
| **TbTutorialStep** | tutorial-onboarding | Tutorial step definitions, trigger conditions | `stepId`, `chapterId`, `triggerCondition`, `allowedGestures[]`, `promptText`, `completionCondition` |
| **TbSettings** | settings-accessibility | Default setting values | `settingKey`, `defaultValue`, `minValue`, `maxValue`, `settingType` |
| **TbInputConfig** | input-system | Gesture recognition thresholds | `gestureType`, `dragThresholdDp`, `tapTimeout`, `rotationMinDegrees`, `pinchMinDistance` |
| **TbShadowStyle** | shadow-puzzle-system | Per-chapter shadow visual presets | `chapterId`, `shadowColor`, `shadowOpacity`, `blurRadius`, `renderTextureScale` |

**Table Design Rules**:

- Each table has a clear owner GDD document that defines its schema
- Table schemas are authored in Excel/JSON and processed by Luban's code generator
- Adding a new table requires updating this ADR's inventory and regenerating GameProto
- Tables use `int` primary keys by default; string keys only when semantically necessary (e.g., `TbSettings.settingKey`)

### 5. Forbidden Patterns

```csharp
// ══════════════════════════════════════════════════════════════
// ❌ FORBIDDEN: Hardcoded gameplay values
// ══════════════════════════════════════════════════════════════

float threshold = 0.85f;                    // hardcoded threshold
float snapDuration = 0.3f;                  // hardcoded timing
int maxHintUses = 3;                        // hardcoded limit
float dragThreshold = 10f;                  // hardcoded input param

// ══════════════════════════════════════════════════════════════
// ✅ CORRECT: All values from Luban config
// ══════════════════════════════════════════════════════════════

float threshold = Tables.Instance.TbPuzzle.Get(puzzleId).PerfectMatchThreshold;
float snapDuration = Tables.Instance.TbPuzzle.Get(puzzleId).SnapAnimationDuration;
int maxHintUses = Tables.Instance.TbHintConfig.Get(chapterId).L3MaxUses;
float dragThreshold = Tables.Instance.TbInputConfig.Get(gestureType).DragThresholdDp;
```

```csharp
// ══════════════════════════════════════════════════════════════
// ❌ FORBIDDEN: Hand-editing Luban generated files
// ══════════════════════════════════════════════════════════════

// In GameProto/TbPuzzle.cs (GENERATED FILE):
public float PerfectMatchThreshold { get; set; }  // added setter — FORBIDDEN

// ══════════════════════════════════════════════════════════════
// ✅ CORRECT: Extension method in GameLogic
// ══════════════════════════════════════════════════════════════

// In GameLogic/Extensions/PuzzleConfigExtensions.cs:
public static class PuzzleConfigExtensions
{
    public static float GetAdjustedThreshold(this TbPuzzle config, float difficulty)
    {
        return config.PerfectMatchThreshold * difficulty;
    }
}
```

```csharp
// ══════════════════════════════════════════════════════════════
// ❌ FORBIDDEN: Caching Tables reference in a field
// ══════════════════════════════════════════════════════════════

private TbPuzzle _cachedConfig;  // stale reference risk on hot-reload

void Init()
{
    _cachedConfig = Tables.Instance.TbPuzzle.Get(puzzleId);  // ❌
}

// ══════════════════════════════════════════════════════════════
// ✅ CORRECT: Read through Tables.Instance each time
// ══════════════════════════════════════════════════════════════

void DoSomething()
{
    var config = Tables.Instance.TbPuzzle.Get(puzzleId);  // ✅ fresh read
    float t = config.PerfectMatchThreshold;
}
```

> **Exception**: For per-frame hot-path access (e.g., match score computation running every frame), caching a local variable within a single method scope is acceptable — but never across frames or in a persistent field.

### 6. Extension Method Convention

When derived values or cross-table lookups are needed, use C# extension methods in the GameLogic assembly:

```csharp
// File: Assets/GameScripts/HotFix/GameLogic/Config/ConfigExtensions.cs
// Assembly: GameLogic

public static class ChapterConfigExtensions
{
    public static TbPuzzle[] GetPuzzleConfigs(this TbChapter chapter)
    {
        return chapter.PuzzleIds
            .Select(id => Tables.Instance.TbPuzzle.Get(id))
            .ToArray();
    }
}

public static class NarrativeConfigExtensions
{
    public static TbNarrativeSequence GetPostPuzzleSequence(this TbPuzzle puzzle)
    {
        return Tables.Instance.TbNarrativeSequence.GetOrDefault(puzzle.PuzzleId);
    }
}
```

**Convention**: Extension methods on config types live in `GameLogic/Config/ConfigExtensions.cs` (or split by domain: `PuzzleConfigExtensions.cs`, `NarrativeConfigExtensions.cs`). This keeps all "logic on top of data" in the hot-updatable GameLogic assembly.

## Alternatives Considered

### Alternative 1: Luban with Tables Singleton (Chosen)

- **Description**: Luban auto-generates C# data classes and a `Tables` singleton from Excel/JSON schemas. Runtime access via `Tables.Instance.TbXXX.Get(id)`. All generated code in the GameProto assembly.
- **Pros**: Type-safe at compile time; Excel-editable by designers without code knowledge; auto-generated code eliminates boilerplate; config-only hot-update via GameProto DLL; O(1) dictionary lookup at runtime; established pattern in Chinese game dev ecosystem
- **Cons**: Requires Luban toolchain in the build pipeline; Excel schema maintenance overhead; generated code can be large for many tables; regeneration step adds to iteration time
- **Why Chosen**: Best fit for P1 pillar (data-driven everything), HybridCLR hot-update architecture (config-only patches), and team workflow (designers edit Excel, Luban generates code)

### Alternative 2: Unity ScriptableObjects

- **Description**: Each config type is a `ScriptableObject`. Designers edit values in the Unity Inspector.
- **Pros**: Unity-native; Inspector editing; serialization built-in; works with Addressables
- **Cons**: Manual creation of each SO — no code generation; no type-safe ID-based lookup (must wire references manually or use a custom registry); harder to hot-update (Addressable SO changes require asset bundle rebuild, not just a DLL swap); Excel import requires custom tooling; bulk editing 100+ puzzle configs in Inspector is painful
- **Estimated Effort**: 3-4x more setup and ongoing maintenance than Luban
- **Rejection Reason**: Lacks code generation, poor bulk editing UX for designers, and inferior hot-update story compared to Luban's GameProto DLL approach

### Alternative 3: JSON Config Files (Hand-Parsed)

- **Description**: Config data stored as JSON files, parsed at runtime with a JSON library (Newtonsoft.Json or System.Text.Json).
- **Pros**: Human-readable; language-agnostic; easy to diff in version control
- **Cons**: No compile-time type safety — typos in field names cause runtime errors; manual parsing boilerplate for every table; no code generation; JSON parsing is slower than Luban's pre-compiled binary format; no Excel editing workflow for designers
- **Estimated Effort**: 2x more runtime code, significantly less safe
- **Rejection Reason**: No type safety eliminates the primary benefit of a config system; manual parsing is error-prone and expensive to maintain

### Alternative 4: XML Configuration

- **Description**: Config data in XML files, parsed with `System.Xml` or `XmlSerializer`.
- **Pros**: Strongly structured; schema validation via XSD
- **Cons**: Verbose format (3-5x larger than equivalent JSON); slow to parse on mobile; no code generation without additional tooling; poor designer ergonomics (XML editing is error-prone); legacy pattern rarely used in modern game dev
- **Estimated Effort**: Similar to JSON but with worse runtime performance
- **Rejection Reason**: All downsides of JSON plus verbose format and slower parsing; no advantages for this project

## Consequences

### Positive

- **Designer autonomy**: Game designers and level designers can tune all gameplay values (thresholds, timing, puzzle difficulty, hint parameters) by editing Excel sheets and running Luban regeneration — zero code changes, zero programmer involvement
- **Hot-update for config**: Rebuilding only GameProto.dll (~50KB–200KB) enables rapid config-only hot-updates deployed to players without app store review. Balance patches can ship in minutes, not days.
- **Type-safe access**: `Tables.Instance.TbPuzzle.Get(id).PerfectMatchThreshold` is fully typed — IDE auto-complete, compile-time error on typo, refactoring support
- **Consistent pattern**: All 11 config tables follow the same `Tables.Instance.TbXXX.Get(id)` pattern. Any developer can read any system's config without learning a new API.
- **P1 pillar enforcement**: The "no hardcoded values" rule is code-reviewable — any literal `float`/`int` for a gameplay parameter is an immediate code review flag

### Negative

- **Luban regeneration step**: Every config schema change requires running the Luban code generator before Unity can compile. This adds ~5-15 seconds to the schema-change iteration loop.
- **Excel schema maintenance**: The Excel source files must be version-controlled alongside code. Schema errors (wrong column type, missing required field) produce Luban generation errors that require understanding the Luban toolchain to fix.
- **Generated code volume**: 11 tables produce a significant amount of generated C# code in GameProto. This increases compile time slightly (~2-5s) and the GameProto DLL size.
- **No caching of config references**: The "always read through `Tables.Instance`" rule (for hot-reload safety) means slightly more indirection than a cached field reference, though the runtime cost is negligible (dictionary lookup).

### Neutral

- `Tables.Init()` loading all tables at boot is a one-time cost during splash screen — not perceived by the player
- The extension method convention for derived values adds a small amount of code organization overhead but keeps the GameProto/GameLogic boundary clean

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Luban version incompatibility with HybridCLR-generated GameProto DLL | LOW | CRITICAL | Pin Luban version in build pipeline; verify Luban output compiles as hot-update DLL in Sprint 0 device test; maintain rollback to last known-good Luban version |
| Large table count impacts `Tables.Init()` boot time beyond 100ms budget | LOW | MEDIUM | Measure Init() on mid-range device in Sprint 0; if exceeded, implement lazy loading for non-essential tables (TbSettings, TbTutorialStep can load on-demand) |
| Developer hardcodes gameplay values despite policy | MEDIUM | MEDIUM | Code review gate: any literal numeric constant in gameplay code is flagged; CI static analysis rule: grep GameLogic for suspicious `= \d+\.?\d*f;` patterns |
| Developer hand-edits Luban generated files in GameProto | LOW | HIGH | `.gitignore` or pre-commit hook warns on manual GameProto changes; Luban regeneration in CI overwrites all generated files; ADR-004 Rule 3 prohibits hand-editing |
| Excel schema diverges from GDD requirements (table missing fields) | MEDIUM | MEDIUM | Table inventory in this ADR serves as source of truth; design review checks GDD requirements against table schemas; validation criterion includes cross-reference check |
| Hot-update of GameProto requires `Tables.Init()` re-run but app is mid-gameplay | LOW | HIGH | Config hot-update requires app restart (or return to title screen → re-init). Runtime re-init while systems are reading is explicitly NOT supported. Clear UX: "New config available, restart to apply." |

## Performance Implications

| Metric | Expected | Budget | Notes |
|--------|----------|--------|-------|
| **Tables.Init() duration** | 50-100ms | < 200ms | One-time boot cost; loads all 11 tables from binary data; runs during splash screen |
| **Per-access latency** | < 0.001ms | 16.67ms frame budget | `Dictionary<int, T>.TryGetValue()` — O(1) hash lookup, no allocation |
| **Memory (all tables)** | 500KB – 2MB | 1,500 MB mobile ceiling | Depends on content volume; 11 tables with ~500 entries each ≈ 1MB |
| **GameProto DLL size** | 200KB – 500KB | CDN patch budget | Config-only hot-update download size; order of magnitude smaller than GameLogic |
| **Luban generation time** | 5-15s | Developer iteration time | Runs on developer machine or CI; not a runtime cost |
| **GC allocation (runtime reads)** | 0 per read | 0 target on hot path | Config data is pre-loaded; field access is direct property read on pre-allocated objects |

## Migration Plan

This is a greenfield setup — no existing config system to migrate. Establishment steps:

1. **Design Excel schemas** — For each table in the inventory, create the Excel source file with column definitions matching GDD requirements. Start with TbPuzzle and TbChapter as validation targets.
2. **Configure Luban pipeline** — Set up Luban config (`luban.conf`) specifying: input directory (Excel files), output directory (GameProto assembly source), code target (C#), data target (binary or JSON).
3. **Run initial generation** — Execute Luban code generator to produce `Tables.cs` and all `TbXXX.cs` data classes in the GameProto assembly folder.
4. **Implement Tables.Init() in ProcedureMain** — Write the `LoadConfigBytes` adapter that routes Luban's data loading through YooAsset's raw file API (or embedded resources for Editor mode).
5. **Verify boot sequence** — Confirm `Tables.Init()` completes at Step 7, before any system reads config. Test in Unity Editor first, then on IL2CPP device build.
6. **Implement first config consumer** — Shadow Puzzle system reads `TbPuzzle` config to validate the end-to-end pattern: Excel → Luban → GameProto → Tables.Instance → GameLogic consumer.
7. **Add remaining tables** — Incrementally add all 11 tables as their consuming systems are implemented across sprints.

**Rollback plan**: If Luban proves incompatible with the HybridCLR/GameProto assembly setup during Sprint 0, fall back to Alternative 2 (ScriptableObjects) for MVP tables (TbPuzzle, TbChapter). The cost of this fallback is 2-3 days of manual SO creation and a loss of the config-only hot-update capability. If config-only hot-update is deemed essential, evaluate a JSON-based alternative (Alternative 3) with a custom code generator.

## Validation Criteria

- [ ] Sprint 0: `Tables.Init()` completes successfully in Unity Editor with at least TbPuzzle and TbChapter populated
- [ ] Sprint 0: `Tables.Init()` completes in < 200ms on mid-range mobile device (Snapdragon 6-series / A14)
- [ ] Sprint 0: `Tables.Instance.TbPuzzle.Get(testId)` returns correct data matching Excel source
- [ ] Sprint 0: Luban regeneration produces no compile errors in GameProto assembly
- [ ] Sprint 0: Hot-update test — modify TbPuzzle Excel → regenerate GameProto only → push via YooAsset → verify app loads new config values without GameLogic change
- [ ] Sprint 0: No hardcoded gameplay constants in any GameLogic source file (static analysis scan)
- [ ] CI: Luban generation step runs in build pipeline; generation failure blocks the build
- [ ] CI: Static analysis flags literal numeric constants in gameplay code that should be config-driven
- [ ] All 11 tables in the inventory have corresponding Excel schemas and produce valid generated code
- [ ] Extension methods for derived config values compile in GameLogic and access GameProto types correctly

## GDD Requirements Addressed

| GDD Document | Requirement ID | Description | How This ADR Satisfies It |
|-------------|----------------|-------------|--------------------------|
| `design/gdd/game-concept.md` | TR-concept-008 | Forbidden: hardcoded gameplay values | Defines `Tables.Instance` access pattern as the sole source of gameplay values; hardcoded constants are a forbidden pattern |
| `design/gdd/input-system.md` | TR-input-017 | All input thresholds from config | `TbInputConfig` table provides `dragThresholdDp`, `tapTimeout`, `rotationMinDegrees`, `pinchMinDistance` |
| `design/gdd/object-interaction.md` | TR-objint-018 | Object interaction params from config | `TbPuzzleObject` table provides `gridSize`, `rotationStep`, `snapSpeed`, `interactionBounds` per object |
| `design/gdd/shadow-puzzle-system.md` | TR-puzzle-014 | Puzzle thresholds from config | `TbPuzzle` table provides `perfectMatchThreshold`, `nearMatchThreshold`, `anchorWeights`, `snapAnimationDuration` |
| `design/gdd/hint-system.md` | TR-hint-010 | Hint timing from config | `TbHintConfig` table provides `hintDelayMultiplier`, `l1Threshold`, `l2Threshold`, `l3MaxUses` per chapter |
| `design/gdd/narrative-event-system.md` | TR-narr-003 | Narrative sequences from config | `TbNarrativeSequence` and `TbChapterTransition` tables provide sequence definitions and chapter transition mappings |
| `design/gdd/audio-system.md` | TR-audio-015 | Audio events from config | `TbAudioEvent` table provides SFX/Music event definitions, clip references, volume, variants, cooldown |
| `design/gdd/tutorial-onboarding.md` | TR-tutor-009 | Tutorial steps from config | `TbTutorialStep` table provides step definitions, trigger conditions, allowed gestures, completion conditions |

## Related

- **Depends On**: ADR-004 (HybridCLR Assembly Boundary Rules) — GameProto assembly structure; GameLogic → GameProto compile-time reference direction
- **Enables**: All Feature layer systems — Shadow Puzzle, Narrative, Hint, Audio, Tutorial, Chapter State, Settings, and future systems all read config via this pattern
- **Resolves**: Architecture Document Open Question #4 — `Tables` thread safety in UniTask async contexts
- **References**: Architecture Document Section 5.1 — Init order (Tables.Init at Step 7)
- **References**: Architecture Document Section P1 — Data-Driven Everything pillar definition
- **References**: `docs/architecture/phase0-tr-baseline.md` — TR requirement traceability for all config-related requirements
