// 该文件由Cursor 自动生成

# ADR-001: TEngine 6.0 Framework Adoption

## Status

Proposed

## Date

2026-04-22

## Last Verified

2026-04-22

## Decision Makers

Technical Director, Lead Programmer

## Summary

The project needs a coherent application framework for module management, UI lifecycle, asset loading, event communication, and hot-update; building these from scratch would cost 3-4 months that an indie team cannot afford. We adopt TEngine 6.0.0 as the core application framework, mandating `GameModule.XXX` static accessors, the Procedure boot chain, `GameEvent` int-based event bus, and `UIWindow`/`UIWidget` UI lifecycle for all game systems.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.62f2 (LTS) |
| **Domain** | Core / Scripting / UI / Audio |
| **Knowledge Risk** | MEDIUM — TEngine 6.0 specifics likely absent from LLM training data; must verify APIs from project source |
| **References Consulted** | Project source (`TEngine/` directory), `docs/engine-reference/unity/VERSION.md`, `.claude/docs/technical-preferences.md` |
| **Post-Cutoff APIs Used** | `GameModule.XXX` static accessors, `GameEvent.Send` / `GameEvent.AddEventListener`, TEngine `UIWindow` / `UIWidget` lifecycle, TEngine Procedure system |
| **Verification Required** | Sprint 0 spike: confirm GameModule accessor pattern, GameEvent payload capabilities, UIWindow callback timing, Procedure chain boot sequence from TEngine 6.0 source |

> **Note**: If Knowledge Risk is MEDIUM or HIGH, this ADR must be re-validated if the
> project upgrades engine versions. Flag it as "Superseded" and write a new ADR.

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None (root decision) |
| **Enables** | ADR-004 (HybridCLR Hot-Update Strategy), ADR-005 (YooAsset Asset Pipeline), ~~ADR-006 (GameEvent Communication)~~ *superseded by ADR-027*, ADR-011 (UIWindow UI Architecture), ADR-027 (GameEvent Interface Protocol) |
| **Blocks** | All Foundation and Core layer implementation; every system in the Systems Index depends on TEngine module patterns |
| **Ordering Note** | This ADR must reach Accepted status before any other ADR can be implemented. Sprint 0 API verification spike must complete before acceptance. |

## Context

### Problem Statement

《影子回忆 (Shadow Memory)》requires a coherent infrastructure layer covering:

1. **Module lifecycle management** — Initializing, accessing, and shutting down cross-cutting services (audio, UI, resources, scenes, timers, FSM)
2. **Boot flow orchestration** — Deterministic startup sequence including HybridCLR assembly loading and YooAsset resource initialization
3. **Inter-module communication** — Decoupled event bus for cross-system messaging without direct references
4. **UI lifecycle** — Standardized window/widget creation, refresh, update, and disposal
5. **Hot-update support** — Loading patched C# assemblies at runtime without a full app update (critical for mobile distribution)
6. **Asset management** — Async, hot-updatable asset loading with proper lifecycle tracking

Without a framework, each of these would need to be designed, implemented, and battle-tested independently. For an indie team, this represents 3-4 months of foundational work before a single puzzle can be prototyped.

### Current State

The project is in pre-production. TEngine 6.0.0 is already present in the project source tree and referenced by all 13 GDD documents. No custom alternative framework exists. The decision is whether to formally commit to TEngine as the canonical framework or evaluate alternatives.

### Constraints

- **Team size**: Indie team — cannot afford multi-month framework development
- **Platform**: Mobile-first (iOS/Android) with future PC — framework must support hot-update for mobile app stores
- **Timeline**: MVP target is 4-6 weeks; framework must be immediately productive
- **HybridCLR dependency**: Hot-update architecture requires framework-level integration with HybridCLR's assembly loading (Default/GameLogic/GameProto split)
- **13 GDDs already reference TEngine**: Switching framework now means rewriting all design documents

### Requirements

- Module access must be type-safe and discoverable (no magic strings, no service locators)
- Boot sequence must be deterministic and extensible (add new init steps without modifying existing ones)
- Event system must support int-based IDs with arbitrary payloads for performance on mobile
- UI framework must handle lifecycle (create → refresh → update → close) with safe area support
- Asset loading must be async-only, wrapping YooAsset, with proper unload tracking
- Framework overhead must stay within mobile performance budgets (16.67ms frame budget, 1.5GB memory ceiling)

## Decision

**Adopt TEngine 6.0.0 as the sole application framework for 影子回忆.** All game systems must integrate through TEngine's module, event, UI, and procedure abstractions.

### Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Game Systems                              │
│  (Shadow Puzzle, Object Interaction, Narrative, Hint, etc.)      │
├──────────┬──────────┬──────────┬──────────┬──────────┬──────────┤
│ GameModule│ GameModule│ GameModule│ GameModule│ GameModule│ GameModule│
│  .Audio   │  .UI      │ .Resource │  .Scene   │  .Timer   │  .Fsm     │
├──────────┴──────────┴──────────┴──────────┴──────────┴──────────┤
│                     GameEvent (int-based bus)                     │
├─────────────────────────────────────────────────────────────────┤
│               TEngine Procedure Chain (Boot Flow)                │
│  ProcedureStart → ProcedureSplash → ProcedureLoadAssembly        │
│                                      → ProcedureMain             │
├─────────────────────────────────────────────────────────────────┤
│               HybridCLR          │         YooAsset              │
│  (Hot-fix assembly loading)      │  (Asset management)           │
├──────────────────────────────────┴──────────────────────────────┤
│                     Unity 2022.3.62f2 (LTS)                      │
└─────────────────────────────────────────────────────────────────┘
```

### Key Interfaces

```csharp
// Module access — ONLY via static accessors
GameModule.Audio.PlaySound("sfx_shadow_match");
GameModule.UI.ShowUI<PuzzleHudWindow>();
GameModule.Resource.LoadAssetAsync<GameObject>("ShadowObject");
GameModule.Scene.LoadScene("Chapter1_Room");
GameModule.Timer.AddTimer(duration, callback);
GameModule.Fsm.CreateFsm<PuzzleState>("PuzzleFsm", states);

// FORBIDDEN: ModuleSystem.GetModule<AudioModule>()

// Event communication — int-based IDs
public static class EventId
{
    public const int ShadowMatched     = 1001;
    public const int PuzzleSolved      = 1002;
    public const int ChapterCompleted  = 1003;
    public const int HintRequested     = 1004;
}

GameEvent.Send(EventId.ShadowMatched, matchData);
GameEvent.AddEventListener(EventId.PuzzleSolved, OnPuzzleSolved);
GameEvent.RemoveEventListener(EventId.PuzzleSolved, OnPuzzleSolved);

// UI lifecycle — UIWindow / UIWidget
public class PuzzleHudWindow : UIWindow
{
    protected override void OnCreate()    { /* bind references */ }
    protected override void OnRefresh()   { /* update display data */ }
    protected override void OnUpdate()    { /* per-frame logic */ }
    protected override void OnClose()     { /* cleanup */ }
}

// Boot flow — Procedure chain
// ProcedureStart → ProcedureSplash → ProcedureLoadAssembly → ProcedureMain
// Each procedure calls ChangeState<NextProcedure>() when ready
```

### Implementation Guidelines

1. **Module access**: Always use `GameModule.XXX` static accessors. The pattern `ModuleSystem.GetModule<T>()` is **forbidden** (TR-concept-007). Enforce via code review and static analysis.
2. **Boot flow**: Extend the Procedure chain by inserting new procedures between existing ones. Never bypass the chain with direct initialization in `Awake()` or `Start()`.
3. **Event IDs**: Define all event IDs as `public const int` in a centralized `EventId` static class. Use ranges to partition by system (1000-1999 puzzle, 2000-2999 narrative, etc.).
4. **UI patterns**: Every game screen inherits `UIWindow`. Reusable components within a screen inherit `UIWidget`. Use `SetUISafeFitHelper` for notch/safe area adaptation on mobile.
5. **Async only**: All asset loading via `GameModule.Resource.LoadAssetAsync<T>()`. Synchronous `Resources.Load` is forbidden. Pair every `LoadAssetAsync` with `UnloadAsset` to prevent resource leaks.
6. **Assembly split**: Respect the HybridCLR assembly boundary — `Default` (framework, non-hot-updateable), `GameLogic` (hot-fix gameplay code), `GameProto` (data definitions).

## Alternatives Considered

### Alternative 1: GameFramework (GF)

- **Description**: The original open-source Unity game framework that TEngine evolved from. Provides module registration, procedures, data tables, events, and resource management.
- **Pros**: Larger community, more documentation, longer track record, proven in shipped Chinese mobile titles
- **Cons**: Heavier API surface; less streamlined HybridCLR integration; older event system design; requires more boilerplate for module registration
- **Estimated Effort**: Similar setup time, but 1-2 weeks additional effort to integrate HybridCLR cleanly
- **Rejection Reason**: TEngine 6.0 is a lighter, modernized fork with first-class HybridCLR support. Since TEngine is already in the project source tree and referenced by all GDDs, switching to GF would require rewriting all design documents for no net benefit.

### Alternative 2: Custom Framework

- **Description**: Build a bespoke module system, event bus, UI lifecycle, and boot sequence tailored exactly to the project's needs.
- **Pros**: Maximum control; no unnecessary abstractions; can be optimized for the specific game; no external dependency risk
- **Cons**: 3-4 months of foundational development before gameplay work can begin; requires deep Unity framework expertise; higher long-term maintenance burden; delays MVP by an unacceptable margin
- **Estimated Effort**: 3-4x the effort of adopting TEngine
- **Rejection Reason**: Not justified for an indie team with a 4-6 week MVP timeline. The flexibility benefit does not outweigh the time cost.

### Alternative 3: No Framework (Direct Unity APIs)

- **Description**: Use Unity's built-in systems directly — MonoBehaviour lifecycle, ScriptableObject events, Addressables for assets, custom singletons for services.
- **Pros**: Zero learning curve for Unity developers; no framework lock-in; simplest initial setup
- **Cons**: Leads to coupling between systems; no standardized module lifecycle; inconsistent patterns across developers; no built-in hot-update path; each system reinvents service discovery
- **Estimated Effort**: Initially lower, but compounds as systems grow — estimated 2x maintenance cost by Alpha
- **Rejection Reason**: The project requires hot-update (HybridCLR), standardized UI lifecycle, and event-driven communication. Without a framework, each of these would become a custom one-off solution, creating exactly the kind of inconsistency and coupling that frameworks prevent.

## Consequences

### Positive

- **Consistent module access**: All 13 game systems access services through the same `GameModule.XXX` pattern — no singletons, no service locators, no magic strings
- **Built-in hot-update**: HybridCLR integration is pre-wired; the Procedure chain handles assembly loading transparently
- **Proven UI lifecycle**: `UIWindow`/`UIWidget` provide create → refresh → update → close semantics, reducing UI bugs from missing cleanup
- **Event decoupling**: `GameEvent` int-based bus allows systems to communicate without compile-time dependencies, critical for the modular architecture
- **Fast time-to-gameplay**: Framework is already in the project; developers can start building puzzle mechanics immediately instead of building infrastructure
- **Safe area handling**: `SetUISafeFitHelper` addresses mobile notch/safe area without custom per-device logic

### Negative

- **Framework lock-in**: All game code depends on TEngine abstractions; migrating away would require rewriting every module access, event subscription, and UI class
- **Learning curve**: Team members unfamiliar with TEngine must learn its conventions before contributing (estimated 1-2 days ramp-up)
- **Knowledge risk**: LLM assistants may generate incorrect TEngine API calls; all AI-generated code involving TEngine must be verified against project source
- **Abstraction constraints**: TEngine's `GameEvent` uses int IDs (not strongly-typed); easy to subscribe to wrong event ID. Mitigation: centralized `EventId` class with documentation
- **Update dependency**: If TEngine 6.0 has bugs, we must either fix them in our fork or wait for upstream patches

### Neutral

- TEngine's module set aligns with (but does not exactly match) the project's needs — some systems (Input, Save, Shadow Rendering) require custom implementation outside TEngine
- The Procedure boot chain adds structure but also ceremony to the startup sequence

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| TEngine 6.0 API differs from AI-generated assumptions | HIGH | MEDIUM | Sprint 0 spike: verify all `GameModule` accessors, `GameEvent` payloads, and `UIWindow` callbacks against source code |
| `GameEvent` int-based IDs cause silent event mismatch bugs | MEDIUM | MEDIUM | Centralized `EventId` class with reserved ranges per system; unit tests for event dispatch/receive |
| TEngine upstream abandonment (no future updates) | LOW | LOW | Project forks TEngine source; all needed features are already present in 6.0 |
| HybridCLR assembly boundary causes unexpected compilation errors | MEDIUM | HIGH | Sprint 0 spike: verify Default/GameLogic/GameProto split compiles correctly with TEngine module references |
| Framework overhead exceeds mobile performance budget | LOW | HIGH | Benchmark module lookup, event dispatch, and UI lifecycle on target devices during Sprint 0 |

## Performance Implications

| Metric | Before (No Framework) | Expected After (TEngine) | Budget |
|--------|----------------------|--------------------------|--------|
| CPU (frame time) | 0ms framework overhead | < 0.5ms (module lookup is cached, event dispatch is direct) | 16.67ms total |
| Memory | 0MB framework | ~2-5 MB (module instances, event registry, UI stack) | 1,500 MB mobile ceiling |
| Load Time | Immediate | +~500ms (Procedure chain: HybridCLR init + YooAsset init + module registration) | < 5s cold start |
| Network | N/A | N/A (framework has no networking component) | N/A |

## Migration Plan

This is a greenfield adoption — no existing systems need migration. The integration steps are:

1. **Verify TEngine 6.0 source** — Confirm `GameModule` accessor pattern, `GameEvent` API signatures, and `UIWindow` lifecycle callbacks match the assumptions in this ADR and all 13 GDDs
2. **Establish EventId registry** — Create `EventId.cs` with reserved ID ranges per system (1000-1999 puzzle, 2000-2999 narrative, 3000-3999 chapter, etc.)
3. **Configure Procedure chain** — Verify `ProcedureStart → ProcedureSplash → ProcedureLoadAssembly → ProcedureMain` flow works with current HybridCLR and YooAsset versions
4. **Create UI base classes** — Establish project-specific `UIWindow` / `UIWidget` subclass conventions (naming, folder structure, safe area integration)
5. **Build first system on TEngine** — Implement Input System (simplest Foundation layer system) to validate the full pattern end-to-end
6. **Document verified APIs** — Update `docs/engine-reference/` with confirmed TEngine 6.0 API signatures for AI assistant accuracy

**Rollback plan**: If Sprint 0 spike reveals fundamental incompatibilities (e.g., GameModule pattern doesn't work as expected, HybridCLR integration is broken), evaluate GameFramework (GF) as Alternative 1. The cost of switching at Sprint 0 is ~1 week; after Sprint 1 it becomes prohibitive.

## Validation Criteria

- [ ] Sprint 0 spike: Confirm `GameModule.Audio`, `GameModule.UI`, `GameModule.Resource`, `GameModule.Scene`, `GameModule.Timer`, `GameModule.Fsm` accessor pattern works from TEngine 6.0 source
- [ ] Sprint 0 spike: Confirm `GameEvent.Send(int, params)` and `GameEvent.AddEventListener(int, handler)` support struct/class payloads
- [ ] Sprint 0 spike: Confirm `UIWindow` lifecycle callback ordering — `OnCreate` fires once, `OnRefresh` fires on each show, `OnUpdate` fires per-frame while visible, `OnClose` fires on hide/destroy
- [ ] Sprint 0 spike: Confirm Procedure chain (`ProcedureStart → ProcedureSplash → ProcedureLoadAssembly → ProcedureMain`) completes successfully with HybridCLR 2.x and YooAsset 2.3.17
- [ ] Sprint 0 spike: Framework overhead benchmark — module lookup < 0.1ms, event dispatch < 0.05ms per event, memory footprint < 5MB on target mobile device
- [ ] Sprint 0 spike: Confirm Default/GameLogic/GameProto assembly split compiles and hot-loads correctly through TEngine's procedure chain

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| All 13 GDDs (systems-index.md) | All | TEngine module access pattern (`GameModule.XXX`) | Mandates `GameModule.XXX` as the sole module access pattern; forbids `ModuleSystem.GetModule<T>()` |
| `design/gdd/game-concept.md` | Core | TR-concept-007: Forbidden `ModuleSystem.GetModule<T>()` | Explicit prohibition in Implementation Guidelines §1; enforced via code review |
| `design/gdd/game-concept.md` | Core | TR-concept-013: Event-driven communication via `GameEvent` | `GameEvent` int-based bus is the mandated inter-module communication mechanism |
| `design/gdd/ui-system.md` | UI | TR-ui-001: All UI via TEngine UIModule | All game screens must inherit `UIWindow`; components inherit `UIWidget`; managed via `GameModule.UI` |
| `design/gdd/audio-system.md` | Audio | Audio playback via `GameModule.Audio` | AudioModule is the sole audio service accessor |
| `design/gdd/scene-management.md` | Scene | Scene loading via TEngine `ResourceModule` + `SceneModule` | `GameModule.Resource` and `GameModule.Scene` are mandated for all scene operations |
| `design/gdd/hint-system.md` | Hint | Timer-based hint triggers via `TimerModule` | `GameModule.Timer` provides timer lifecycle management |
| `design/gdd/shadow-puzzle-system.md` | Puzzle | Puzzle FSM via `FsmModule` | `GameModule.Fsm` provides state machine creation and management |
| `design/gdd/chapter-state-and-save.md` | Chapter/Save | State change notification via `GameEvent` | Chapter state transitions broadcast via `GameEvent` int-based IDs |
| `design/gdd/narrative-event-system.md` | Narrative | Event-driven narrative triggers | Narrative system subscribes to puzzle/chapter events via `GameEvent.AddEventListener` |
| `design/gdd/settings-accessibility.md` | Settings | Settings UI via UIModule | Settings screens managed through `UIWindow` lifecycle |
| `design/gdd/tutorial-onboarding.md` | Tutorial | Tutorial UI overlays via UIModule | Tutorial prompts use `UIWidget` components within `UIWindow` |
| `design/gdd/input-system.md` | Input | N/A (TEngine has no input module) | Input System is custom-built; this ADR does not constrain it |

> **Foundational scope**: This ADR is the root architectural decision. Every other ADR and every game system depends on TEngine's module pattern, event bus, and UI lifecycle established here.

## Related

- **Enables**: ADR-004 (HybridCLR Hot-Update Strategy) — TEngine's `ProcedureLoadAssembly` is the integration point
- **Enables**: ADR-005 (YooAsset Asset Pipeline) — `GameModule.Resource` wraps YooAsset; asset loading patterns depend on this ADR
- **Enables**: ADR-027 (GameEvent Interface Protocol) — Detailed `[EventInterface]` Source Generator conventions build on the `GameEvent` system adopted here. (ADR-006 的 const int ID 分配方案已被 ADR-027 取代；ADR-006 §3-§6 的生命周期/token/顺序/文档协议仍由 ADR-027 继承。)
- **Enables**: ADR-011 (UIWindow UI Architecture) — Window/widget hierarchy and lifecycle conventions extend this ADR's UI decision
- **References**: `.claude/docs/technical-preferences.md` — Forbidden patterns and allowed libraries align with this ADR
- **References**: `src/MyGame/ShadowGame/design/gdd/systems-index.md` — TEngine Integration Map section documents per-system module usage
