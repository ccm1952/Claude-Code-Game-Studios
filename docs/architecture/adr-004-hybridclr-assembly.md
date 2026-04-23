// 该文件由Cursor 自动生成

# ADR-004: HybridCLR Assembly Boundary Rules

## Status

Proposed

## Date

2026-04-22

## Last Verified

2026-04-22

## Decision Makers

Technical Director, Lead Programmer

## Summary

HybridCLR enables hot-updating C# code on mobile without full app store resubmission. The project adopts a three-assembly split — Default (bootstrap), GameLogic (all gameplay code), GameProto (Luban-generated config data) — with strict boundary rules governing what code lives where, how assemblies reference each other, and how AOT generic metadata is registered. Violating these boundaries causes hard runtime crashes on IL2CPP builds.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.62f2 (LTS) |
| **Domain** | Scripting / Hot-Update / Build Pipeline |
| **Knowledge Risk** | HIGH — HybridCLR 2.x internals, AOT generic metadata registration, and Unity IL2CPP interop behavior are likely beyond LLM training data; must verify from HybridCLR docs and project source |
| **References Consulted** | HybridCLR official documentation, TEngine 6.0 `ProcedureLoadAssembly` source, project assembly definitions, `docs/engine-reference/unity/VERSION.md` |
| **Post-Cutoff APIs Used** | `HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly()`, TEngine `ProcedureLoadAssembly` DLL-loading flow, YooAsset raw-file loading for DLL assets |
| **Verification Required** | Sprint 0 spike: confirm three-assembly DLL load on iOS IL2CPP and Android IL2CPP; verify AOT generic metadata registration covers all UniTask/Collection generic instantiations |

> **Note**: If Knowledge Risk is MEDIUM or HIGH, this ADR must be re-validated if the
> project upgrades engine or HybridCLR versions. Flag it as "Superseded" and write a new ADR.

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-001 (TEngine 6.0 Framework — provides ProcedureLoadAssembly boot step and GameModule pattern) |
| **Enables** | ADR-007 (Luban Access Pattern — defines how GameLogic reads config data from GameProto assembly) |
| **Blocks** | All gameplay implementation — assembly structure must be established before any gameplay code is written |
| **Ordering Note** | ADR-001 must reach Accepted first. This ADR must reach Accepted before GameLogic or GameProto assemblies contain any production code. |

## Context

### Problem Statement

《影子回忆 (Shadow Memory)》targets mobile platforms (iOS/Android) where app store review cycles make full binary resubmission costly and slow. HybridCLR provides the ability to hot-update C# assemblies at runtime, but this capability comes with strict constraints:

1. **Assembly boundary**: Code in the Default (AOT-compiled) assembly cannot be hot-updated. Code in hot-update assemblies can be patched without rebuilding the app.
2. **AOT generic limitations**: IL2CPP pre-compiles generic type instantiations. Generic types used in hot-update code but not registered in AOT metadata will cause `MissingMethodException` at runtime — a hard crash with no graceful fallback.
3. **MonoBehaviour serialization**: Unity serializes MonoBehaviour references by assembly-qualified type name. Prefabs embedding hot-update MonoBehaviours in non-hot-update scenes will fail to deserialize after a hot-update.
4. **Cross-assembly coupling**: Incorrect dependency directions between assemblies break the hot-update isolation model, defeating the purpose of the split.

Without clear, enforceable rules about what code goes where, developers will inevitably place code in the wrong assembly, leading to:
- Bootstrap code that can't be hot-fixed when bugs are found
- Runtime crashes from missing AOT generic metadata on IL2CPP devices
- Prefabs that silently lose their MonoBehaviour components after a hot-update
- Config-only patches that unnecessarily force a full logic DLL download

### Current State

The project is in pre-production. TEngine 6.0 (ADR-001) provides the `ProcedureLoadAssembly` boot step that loads hot-update DLLs. Three `.asmdef` files define the assembly boundaries. No production code exists yet in any assembly. This ADR formalizes the rules before code starts being written.

### Constraints

- **Mobile first**: iOS App Store and Google Play impose review delays; hot-update is essential for rapid bug fixes and config tuning
- **IL2CPP mandatory**: Both iOS and Android release builds use IL2CPP (Unity requirement for iOS, performance requirement for Android)
- **HybridCLR version**: Must stay compatible with HybridCLR 2.x release track that supports Unity 2022.3 LTS
- **Luban codegen**: GameProto assembly content is 100% auto-generated; hand-editing Luban output is forbidden
- **TEngine integration**: `ProcedureLoadAssembly` in the Default assembly must load both GameLogic and GameProto DLLs before `ProcedureMain` hands off to the hot-fix entry point

## Decision

**Adopt a three-assembly split with strict unidirectional dependency rules and mandatory AOT generic metadata registration.** All developers must understand and respect the assembly boundaries defined below.

### Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│  Default Assembly (非热更, BootScene)                             │
│  ├── TEngine RootModule bootstrapper                             │
│  ├── HybridCLR metadata registration (AOT generics)             │
│  ├── YooAsset package initialization                             │
│  └── ProcedureLaunch / ProcedureSplash / ProcedureLoadAssembly   │
├──────────────────────────────────────────────────────────────────┤
│  GameLogic Assembly (热更, 全部 gameplay 代码)                    │
│  ├── All interfaces (IInputService, IShadowPuzzle, etc.)         │
│  ├── All implementations (InputManager, PuzzleManager, etc.)     │
│  ├── All MonoBehaviour components (InteractableObject, etc.)     │
│  ├── GameEvent ID constants (EventId.cs)                         │
│  └── GameEntry / GameApp hot-fix entry point                     │
├──────────────────────────────────────────────────────────────────┤
│  GameProto Assembly (热更, Luban 生成代码)                        │
│  ├── Tables singleton                                            │
│  ├── All TbXXX data classes (TbChapter, TbPuzzle, etc.)          │
│  └── All auto-generated data classes                             │
└──────────────────────────────────────────────────────────────────┘
```

### Dependency Direction

```
Default ──(dynamic load)──▶ GameLogic ──(compile ref)──▶ GameProto
   │                             │                            │
   │  Cannot reference           │  Can read config           │  Cannot reference
   │  GameLogic or GameProto     │  tables from GameProto     │  GameLogic
   │  at compile time            │                            │
   ▼                             ▼                            ▼
  AOT (non-hot-updatable)    Hot-updatable (logic)       Hot-updatable (config)
```

### Assembly Boundary Rules

#### Rule 1: Default Assembly — Bootstrap Only

The Default assembly contains **only** the minimum code needed to boot the application and load hot-update DLLs:

- TEngine `RootModule` bootstrapper and Procedure chain (`ProcedureLaunch`, `ProcedureSplash`, `ProcedureLoadAssembly`)
- HybridCLR AOT metadata registration via `RuntimeApi.LoadMetadataForAOTAssembly()`
- YooAsset package initialization (resource system bootstrap)
- **Nothing else.** No gameplay logic. No Luban table access. No MonoBehaviours beyond the boot scene's root objects.

**Rationale**: Anything in Default cannot be hot-updated. The smaller this assembly, the smaller the attack surface for unfixable bugs.

#### Rule 2: GameLogic Assembly — All Gameplay Code

All hand-written gameplay code lives in GameLogic:

- Interfaces (`IInputService`, `IShadowPuzzle`, `IHintProvider`, etc.)
- Manager/service implementations (`InputManager`, `PuzzleManager`, `NarrativeManager`, etc.)
- All `MonoBehaviour` components (`InteractableObject`, `ShadowCaster`, `PlayerController`, etc.)
- `GameEvent` ID constants (`EventId.cs`)
- The hot-fix entry point (`GameEntry` / `GameApp`) invoked by `ProcedureMain`
- All `UIWindow` and `UIWidget` subclasses

**Rationale**: Centralizing all gameplay in one hot-update assembly enables single-DLL patches for logic bugs. Interfaces and implementations coexist here (rather than extracting interfaces to a shared assembly) to keep the split simple and minimize cross-assembly complexity.

#### Rule 3: GameProto Assembly — Luban Generated Code Only

GameProto contains **exclusively** Luban auto-generated code:

- The `Tables` singleton (config data root)
- All `TbXXX` data table classes (`TbChapter`, `TbPuzzle`, `TbShadowType`, etc.)
- All auto-generated data structs/classes

**FORBIDDEN**: Hand-editing any file in GameProto. If custom logic is needed for config data, write it as extension methods in GameLogic.

**Rationale**: Isolating config data enables config-only hot-updates (new puzzle parameters, balance tuning, narrative text) without re-deploying the logic DLL. Since Luban regenerates these files, hand-edits would be silently destroyed.

#### Rule 4: AOT Generic Metadata Registration

All generic type instantiations used by hot-update code must be registered in the Default assembly's AOT metadata step. This registration must happen **before** hot-update DLLs are loaded.

**Known required registrations** (non-exhaustive, must be maintained as code evolves):

```csharp
// In Default assembly, during ProcedureLoadAssembly
RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HomologousImageMode.SuperSet);

// Generic types that MUST have AOT instantiations registered:
// - UniTask<T> for all T used in async gameplay code
// - List<T> for all config data types (TbChapter, TbPuzzle, etc.)
// - Dictionary<TKey, TValue> for all config lookup patterns
// - Action<T>, Func<T, TResult> for event callbacks
// - Nullable<T> for value-type config fields
```

**Enforcement**: Maintain a `AOTGenericReferences.cs` file in the Default assembly. HybridCLR's build pipeline can auto-generate this via `HybridCLR/Generate/AOTGenericReference`. Run this generation step before every release build.

**Failure mode**: Missing registration → `MissingMethodException` or `ExecutionEngineException` at runtime on IL2CPP devices. This is a **hard crash** with no recovery. It will not manifest in the Unity Editor (which uses Mono, not IL2CPP).

#### Rule 5: Cross-Assembly Reference Direction

| Source | → Target | Allowed? | Mechanism |
|--------|----------|----------|-----------|
| Default | → GameLogic | **NO** (compile-time) | Dynamic load via `Assembly.Load()` at runtime |
| Default | → GameProto | **NO** (compile-time) | Dynamic load via `Assembly.Load()` at runtime |
| GameLogic | → GameProto | **YES** | Standard `.asmdef` reference; reads `Tables` singleton |
| GameLogic | → Default | **NO** | Would create circular dependency |
| GameProto | → GameLogic | **NO** | Config data must not depend on gameplay logic |
| GameProto | → Default | **NO** | Config data must not depend on bootstrap code |

**Rationale**: Unidirectional dependency (Default → loads → GameLogic → references → GameProto) ensures each assembly can be independently hot-updated. If GameProto referenced GameLogic, a config-only update could break if GameLogic's API changed.

#### Rule 6: MonoBehaviour Prefab Loading Constraint

All `MonoBehaviour` subclasses live in the GameLogic assembly (Rule 2). Prefabs that reference these MonoBehaviours **must** be loaded via YooAsset at runtime — they **cannot** be embedded directly in scenes that belong to the Default assembly (BootScene).

```
✅ CORRECT:
  BootScene (Default) → ProcedureLoadAssembly loads GameLogic DLL
                       → ProcedureMain loads GameScene via YooAsset
                       → GameScene prefabs reference GameLogic MonoBehaviours
                       → MonoBehaviours resolve correctly (DLL already loaded)

❌ INCORRECT:
  BootScene (Default) → Embeds prefab with GameLogic MonoBehaviour
                       → Unity tries to deserialize before DLL is loaded
                       → MonoBehaviour component becomes "Missing Script"
```

**Rationale**: Unity resolves MonoBehaviour type references at scene load time. If the hot-update DLL containing the type hasn't been loaded yet, Unity strips the component as "missing". All gameplay scenes and prefabs must load through YooAsset after the DLL-loading procedure completes.

### Boot Sequence Integration

```
App Launch
  │
  ├─ 1. Unity loads BootScene (Default assembly, AOT-compiled)
  ├─ 2. TEngine RootModule.Init() — registers framework modules
  ├─ 3. ProcedureLaunch — minimal app init (screen orientation, frame rate)
  ├─ 4. ProcedureSplash — show splash screen
  ├─ 5. ProcedureLoadAssembly:
  │     ├─ 5a. YooAsset init + resource package update check
  │     ├─ 5b. Download GameLogic.dll + GameProto.dll via YooAsset
  │     ├─ 5c. HybridCLR AOT metadata registration
  │     ├─ 5d. Assembly.Load(GameProto) then Assembly.Load(GameLogic)
  │     └─ 5e. Reflection-invoke GameEntry.Start() in GameLogic
  ├─ 6. ProcedureMain (now running in hot-update code)
  │     ├─ 6a. Luban Tables.Init() — load config data
  │     ├─ 6b. Register GameModule services (puzzle, narrative, etc.)
  │     └─ 6c. Load first gameplay scene via YooAsset
  └─ 7. Gameplay begins
```

**Critical ordering**: Step 5c (AOT metadata) must complete **before** 5d (DLL load). Step 5d must load GameProto before GameLogic (GameLogic references GameProto types). Step 6a (Tables.Init) must complete before any system reads config data.

## Alternatives Considered

### Alternative 1: Three-Assembly Split (Chosen)

- **Description**: Default (bootstrap) / GameLogic (all gameplay) / GameProto (Luban config)
- **Pros**: Clear separation of concerns; independent hot-update of logic vs config; minimal non-updatable code; matches TEngine's recommended structure
- **Cons**: Must maintain AOT generic metadata; developers must know which assembly they're coding in; GameLogic ↔ GameProto boundary requires discipline
- **Chosen because**: Best balance of hot-update granularity, simplicity, and maintenance cost for an indie team

### Alternative 2: Two-Assembly Split (Default + Single HotFix)

- **Description**: Merge GameLogic and GameProto into a single hot-update assembly
- **Pros**: Simpler — only two assemblies; no cross-hot-update-assembly reference concerns; fewer `.asmdef` files to manage
- **Cons**: Config-only changes (balance tuning, new puzzle data) require downloading the entire gameplay DLL; Luban-generated code mixed with hand-written code makes the assembly boundary less clean; larger DLL download for minor config patches
- **Estimated Effort**: Slightly less initial setup, but increases ongoing patch sizes
- **Rejection Reason**: For a mobile game that will frequently tune config data post-launch, the ability to push a small GameProto-only patch (KBs) instead of a full logic+config patch (MBs) is a significant user experience and bandwidth advantage

### Alternative 3: Single Assembly (No Hot-Update)

- **Description**: All code in the Default assembly, compiled AOT, no HybridCLR
- **Pros**: Simplest build pipeline; no AOT generic concerns; no assembly boundary complexity; faster cold start (no DLL loading step)
- **Cons**: Every bug fix and config change requires full app store resubmission; 1-7 day review cycles on iOS; cannot respond quickly to player feedback or critical bugs
- **Rejection Reason**: Unacceptable for mobile distribution. The project's GDDs explicitly require hot-update capability (TR-concept-014).

### Alternative 4: IL2CPP Without HybridCLR (Standard Unity AOT)

- **Description**: Use Unity's standard IL2CPP compilation with Addressable code (asset-only hot-update, no C# hot-update)
- **Pros**: No HybridCLR dependency; simpler build pipeline; no AOT generic registration burden; Unity officially supports this path
- **Cons**: Cannot hot-update C# logic; only assets (textures, audio, ScriptableObjects) can be patched remotely; gameplay bug fixes still require app store resubmission
- **Rejection Reason**: Asset-only hot-update is insufficient. The project needs to hot-fix gameplay logic (puzzle solvers, narrative triggers, interaction handlers) without resubmission.

## Consequences

### Positive

- **Independent hot-update granularity**: Logic bugs → patch GameLogic.dll only (~500KB–2MB). Config tuning → patch GameProto.dll only (~50KB–200KB). No unnecessary downloads.
- **Clear code organization**: Every developer knows exactly where their code goes — bootstrap in Default, gameplay in GameLogic, generated config in GameProto
- **Minimal non-updatable surface**: Only boot procedures live in the non-updatable Default assembly; even the game's entry point (`GameEntry`) is hot-updatable
- **Fast iteration for config changes**: Luban regeneration + GameProto rebuild + push to CDN enables config updates in minutes, not days
- **Type safety within assemblies**: GameLogic has a compile-time reference to GameProto, so config table access is fully typed — no reflection or string-based lookups

### Negative

- **AOT generic maintenance burden**: Developers must ensure every generic type instantiation used in hot-update code is registered in AOT metadata. Forgetting a registration causes a runtime crash that only manifests on device (not in Editor)
- **Prefab loading constraint**: All gameplay prefabs must go through YooAsset. Dragging a GameLogic MonoBehaviour into a Default assembly scene is a silent time bomb
- **Assembly awareness required**: Developers must understand the three-assembly model. New team members may accidentally place code in the wrong assembly
- **Editor vs Device divergence**: The Unity Editor uses Mono (all code is JIT-compiled), so AOT generic issues and assembly boundary violations are invisible during development. Bugs only surface on IL2CPP device builds
- **Build pipeline complexity**: The build must: (1) generate AOT generic references, (2) build Default as AOT, (3) build GameLogic + GameProto as interpretable DLLs, (4) package DLLs as YooAsset raw files

### Neutral

- DLL loading adds ~200-500ms to boot time (occurs during splash screen, not perceived by player)
- HybridCLR interpreter mode for new code paths has a ~2-5x performance penalty vs AOT; this only affects newly added methods not present in the original build — acceptable for logic fixes, not for tight inner loops

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| Missing AOT generic registration causes runtime crash on device | HIGH | CRITICAL | Run `HybridCLR/Generate/AOTGenericReference` before every release build; add IL2CPP device test to CI; maintain manual checklist of known generic types |
| Developer places gameplay code in Default assembly | MEDIUM | HIGH | Code review gate; `.asmdef` structure makes it a compile error to reference GameLogic types from Default; document assembly rules in onboarding |
| Prefab references GameLogic MonoBehaviour from Default scene | MEDIUM | HIGH | BootScene contains zero gameplay prefabs (enforced by review); all gameplay scenes loaded via YooAsset after DLL load |
| GameProto → GameLogic accidental reference | LOW | HIGH | `.asmdef` does not include GameLogic as a reference for GameProto; compile error if violated |
| HybridCLR version incompatibility with Unity 2022.3.62f2 | LOW | CRITICAL | Pin HybridCLR version; test on target devices during Sprint 0; maintain rollback plan to last known-good HybridCLR version |
| DLL download fails on poor mobile network | MEDIUM | MEDIUM | YooAsset provides retry + resume; show progress UI during ProcedureLoadAssembly; cache DLLs locally after successful download |
| HybridCLR interpreter performance insufficient for hot-patched code | LOW | MEDIUM | Critical inner loops (rendering, physics) remain in Default assembly if needed; gameplay logic is not performance-critical enough to matter |

## Performance Implications

| Metric | Impact | Budget | Notes |
|--------|--------|--------|-------|
| Boot time | +200–500ms | < 5s cold start | DLL loading during ProcedureLoadAssembly; occurs behind splash screen |
| Runtime CPU | ~0ms for AOT-registered paths; 2–5x for interpreter-only paths | 16.67ms per frame | Hot-patched methods use interpreter; original methods remain AOT-speed |
| Memory | +2–5MB | 1,500MB mobile ceiling | GameLogic DLL (~1–2MB) + GameProto DLL (~0.5–1MB) + metadata (~0.5–1MB) |
| Patch download | GameLogic: ~500KB–2MB; GameProto: ~50KB–200KB | Per-patch CDN cost | Config-only patches are an order of magnitude smaller |
| Build time | +30–60s | N/A | AOT generic reference generation + HybridCLR build steps added to pipeline |

## Migration Plan

This is a greenfield setup — no existing code to migrate. Establishment steps:

1. **Create `.asmdef` files** — Define `GameLogic.asmdef` and `GameProto.asmdef` with correct reference directions (GameLogic → GameProto, neither → Default)
2. **Configure HybridCLR** — Install HybridCLR package; configure `HybridCLRSettings` to mark GameLogic and GameProto as hot-update assemblies
3. **Implement AOT metadata registration** — In `ProcedureLoadAssembly`, add `RuntimeApi.LoadMetadataForAOTAssembly()` calls for all supplementary metadata DLLs
4. **Implement DLL loading** — In `ProcedureLoadAssembly`, load GameProto.dll then GameLogic.dll via `Assembly.Load(byte[])`
5. **Create GameEntry** — Minimal entry point in GameLogic that `ProcedureMain` invokes via reflection after DLLs are loaded
6. **Generate initial AOTGenericReferences** — Run `HybridCLR/Generate/AOTGenericReference` to establish baseline
7. **Verify on device** — Build IL2CPP for Android and iOS; confirm DLL load, AOT generics, and MonoBehaviour resolution all work

**Rollback plan**: If HybridCLR proves unstable on target devices during Sprint 0, fall back to Alternative 2 (two-assembly split) as a simpler configuration. If hot-update is fundamentally unworkable, fall back to Alternative 4 (asset-only updates) and accept the constraint on logic patching. The cost of this fallback escalates sharply after Sprint 1.

## Validation Criteria

- [ ] Sprint 0: Three `.asmdef` files compile successfully with correct reference directions
- [ ] Sprint 0: `ProcedureLoadAssembly` loads both DLLs and invokes `GameEntry.Start()` in Unity Editor
- [ ] Sprint 0: AOT generic reference generation completes without errors
- [ ] Sprint 0: IL2CPP build for Android loads DLLs and runs `GameEntry.Start()` without crashes
- [ ] Sprint 0: IL2CPP build for iOS loads DLLs and runs `GameEntry.Start()` without crashes
- [ ] Sprint 0: Config-only hot-update test — modify GameProto, rebuild only GameProto.dll, push via YooAsset, verify app loads new config without GameLogic change
- [ ] Sprint 0: Logic-only hot-update test — modify GameLogic, rebuild only GameLogic.dll, push via YooAsset, verify app loads new logic without GameProto change
- [ ] Sprint 0: Verify all generic types used in test code (`UniTask<bool>`, `List<TbChapter>`, `Dictionary<int, TbPuzzle>`) resolve correctly on IL2CPP device
- [ ] Sprint 0: Boot time with DLL loading < 500ms on mid-range target device (Snapdragon 6-series / A14 equivalent)
- [ ] CI: `HybridCLR/Generate/AOTGenericReference` runs as a pre-build step and fails the build if new unregistered generics are detected

## GDD Requirements Addressed

| GDD Document | Requirement ID | Description | How This ADR Satisfies It |
|-------------|----------------|-------------|--------------------------|
| `design/gdd/game-concept.md` | TR-concept-012 | Assembly structure: Default / GameLogic / GameProto | Defines the three-assembly architecture with strict boundary rules |
| `design/gdd/game-concept.md` | TR-concept-014 | HybridCLR hot-update strategy | Formalizes DLL loading sequence, AOT metadata registration, and hot-update granularity |
| Architecture Document | Phase 3, Steps 3–4 | Init order: HybridCLR load + AOT registration | Boot sequence integration section documents exact ordering within ProcedureLoadAssembly |

## Related

- **Depends On**: ADR-001 (TEngine 6.0 Framework) — `ProcedureLoadAssembly` is defined by TEngine's Procedure chain
- **Enables**: ADR-007 (Luban Access Pattern) — Defines how GameLogic reads `Tables` singleton from GameProto assembly
- **Blocks**: All gameplay implementation — no production code should be written until assembly boundaries are established and verified
- **References**: `docs/engine-reference/unity/VERSION.md` — Unity 2022.3.62f2 IL2CPP behavior
- **References**: `.claude/docs/technical-preferences.md` — Assembly placement rules
