// 该文件由Cursor 自动生成

# Architecture Review Report — 影子回忆 (Shadow Memory)

> **Date**: 2026-04-22
> **Reviewer**: Technical Director
> **Review Type**: LEAN (no director panel)
> **Engine**: Unity 2022.3.62f2 LTS
> **Framework**: TEngine 6.0.0 + HybridCLR + YooAsset 2.3.17 + UniTask 2.5.10
> **Baseline**: 212 TRs from `phase0-tr-baseline.md`
> **ADRs Reviewed**: 11 (ADR-001 through ADR-011)
> **Architecture Document**: `docs/architecture/architecture.md` v1.0.0

---

## Verdict

### CONCERNS

The architecture is **structurally sound** — the 5-layer system, 11 P0 ADRs, and comprehensive master architecture document demonstrate serious technical planning. However, **2 cross-ADR conflicts must be resolved before implementation begins**, and several minor budget discrepancies need reconciliation. No Foundation-layer TRs have hard gaps. The 7 unwritten P1 ADRs (012–018) are expected gaps that do not block Sprint 1.

---

## Executive Summary

| Metric | Value |
|--------|------:|
| **Total TRs** | 212 |
| **✅ Covered** | 124 (58.5%) |
| **⚠️ Partial** | 87 (41.0%) |
| **❌ Gap** | 1 (0.5%) |
| **Cross-ADR Conflicts** | 2 critical, 2 moderate, 3 minor |
| **Engine Compatibility Issues** | 0 |
| **Dependency Cycles** | 0 |
| **ADRs Reviewed** | 11 / 11 (all P0) |
| **Unwritten P1 ADRs** | 7 (expected — Feature layer) |
| **Unwritten P2 ADRs** | 8 (expected — Presentation/Optimization) |

### Top 3 Issues

1. **[CRITICAL] Event ID Conflict — ADR-006 vs ADR-010/ADR-011**: ADR-006 allocates Input gesture events at IDs 1000–1004, but ADR-010 defines them at 5001–5005. ADR-011 introduces UI events at 6001–6004, outside ADR-006's allocated range. **Must resolve before `EventId.cs` is created.**

2. **[MODERATE] Performance Budget Discrepancies**: Audio memory budget diverges between ADR-003 (< 80 MB) and TR-audio-012 (< 30 MB). UI prefab memory diverges between ADR-011 (~5–15 MB) and TR-ui-018 (< 5 MB). Save load time diverges between ADR-008 (< 200 ms) and TR-save-015 (< 100 ms). **Must reconcile before Sprint 1 performance gates.**

3. **[MODERATE] Core Layer ADR Gaps**: Object Interaction (22 TRs) and Audio System (15 TRs) are classified as Core Layer in architecture.md but have no dedicated ADRs. The architecture document provides interface definitions that partially cover these, but 26 TRs remain at ⚠️ Partial coverage. **Acceptable if P1 ADRs (013, 017) are written before their respective Sprint starts.**

---

## Phase 2–3: Traceability Matrix

### Methodology

Each of the 212 TRs from `phase0-tr-baseline.md` was checked against:
- All 11 ADR documents (ADR-001 through ADR-011)
- Master architecture document (`architecture.md`) sections: layer map, module ownership, data flow, API boundaries, init order
- `.claude/docs/technical-preferences.md`

Coverage levels:
- **✅ Covered**: Explicitly addressed in an ADR's decision/implementation section, OR fully specified in architecture.md with interface + data flow + performance budget
- **⚠️ Partial**: Mentioned in architecture.md interfaces or data flow, but no formal ADR decision; OR deferred to an unwritten P1/P2 ADR
- **❌ Gap**: Not addressed in any existing document

### Coverage by System

| System | Layer | TRs | ✅ Covered | ⚠️ Partial | ❌ Gap | Primary ADR(s) |
|--------|-------|:---:|:----------:|:----------:|:-----:|----------------|
| Input System | Foundation | 18 | 18 | 0 | 0 | ADR-010 |
| URP Shadow Rendering | Foundation | 23 | 14 | 9 | 0 | ADR-002 |
| Save System / Chapter State | Foundation | 20 | 19 | 1 | 0 | ADR-008 |
| Scene Management | Foundation | 17 | 15 | 2 | 0 | ADR-009 |
| **Foundation Subtotal** | | **78** | **66** | **12** | **0** | |
| Object Interaction | Core | 22 | 5 | 16 | 1 | *(ADR-013 P1 pending)* |
| UI System | Core | 22 | 11 | 11 | 0 | ADR-011 |
| Audio System | Core | 15 | 5 | 10 | 0 | *(ADR-017 P1 pending)* |
| **Core Subtotal** | | **59** | **21** | **37** | **1** | |
| Shadow Puzzle System | Feature | 14 | 6 | 8 | 0 | *(ADR-012/014 P1 pending)* |
| Hint System | Feature | 17 | 5 | 12 | 0 | *(ADR-015 P1 pending)* |
| Narrative Event System | Feature | 12 | 4 | 8 | 0 | *(ADR-016 P1 pending)* |
| **Feature Subtotal** | | **43** | **15** | **28** | **0** | |
| Tutorial / Onboarding | Presentation | 10 | 5 | 5 | 0 | *(ADR-019 P2 pending)* |
| Settings & Accessibility | Presentation | 8 | 4 | 4 | 0 | *(ADR-020/022 P2 pending)* |
| **Presentation Subtotal** | | **18** | **9** | **9** | **0** | |
| Cross-Cutting (Concept) | All | 14 | 13 | 1 | 0 | ADR-001/003/004/005/006/007 |
| **GRAND TOTAL** | | **212** | **124** | **87** | **1** | |

### Foundation Layer — Detailed Gaps

All 78 Foundation TRs are ✅ or ⚠️. No hard gaps. The 12 ⚠️ items:

| TR ID | Requirement | Status | Notes |
|-------|------------|:------:|-------|
| TR-render-005 | Shadow Distance: 8m configurable | ⚠️ | ADR-002 mentions configurable distance but doesn't specify the 8m default |
| TR-render-008 | Shadow contrast ratio ≥ 3:1 | ⚠️ | Not explicitly in ADR-002; covered by WallReceiver shader params |
| TR-render-013 | Max 2 shadow-casting lights | ⚠️ | Not stated in ADR-002; implementation constraint |
| TR-render-014 | Shadow caster priority ordering | ⚠️ | Not in ADR-002; implementation detail |
| TR-render-018 | Shadow memory ≤ 15MB (Medium) | ⚠️ | ADR-002 specifies ShadowRT at 256KB but not total shadow memory budget |
| TR-render-020 | NearMatch glow must NOT affect ShadowRT | ⚠️ | Specific shader constraint not in ADR-002 |
| TR-render-021 | High-contrast accessibility mode | ⚠️ | ADR-002 explicitly defers to ADR-020 (P2) |
| TR-render-022 | Shadow outline accessibility mode | ⚠️ | ADR-002 explicitly defers to ADR-020 (P2) |
| TR-save-016 | Replay mode for completed chapters | ⚠️ | arch.md `IChapterState.IsReplayMode` exists but no behavioral specification |
| TR-scene-008 | Chapter scene memory ~1000MB | ⚠️ | ADR-009 says "< 1000 MB runtime per chapter" — aligned but measurement method not specified |
| TR-scene-009 | Cached scene load time < 1s | ⚠️ | ADR-009 specifies total transition < 3s but not load-only < 1s |

**Assessment**: No Foundation TR is unaddressed. The ⚠️ items are either implementation details covered by architecture.md interfaces, or accessibility features explicitly deferred to P2 ADRs. **No blocking gaps in Foundation.**

### Core Layer — Detailed Gaps

| TR ID | Requirement | Status | Notes |
|-------|------------|:------:|-------|
| TR-objint-022 | Haptic feedback cross-platform | ❌ | Identified as gap in architecture.md §7.4. No interface, no ADR. Deferred to ADR-025 (P2). |
| TR-objint-001–011 | Object mechanics (raycast, snap, drag, tracks, bounds) | ⚠️ ×11 | arch.md `IObjectInteraction` + `InteractableObject` class defined; detailed algorithms deferred to ADR-013 (P1) |
| TR-objint-017–021 | Object performance + debounce | ⚠️ ×5 | Performance budgets in arch.md §5.1; specifics deferred to ADR-013 |
| TR-audio-002–005, 008–012, 014 | Audio internals (formula, variants, spatial, concurrent, latency, memory) | ⚠️ ×10 | arch.md `IAudioService` defines interface; internals deferred to ADR-017 (P1) |
| TR-ui-009–015, 017, 020–022 | UI implementation details (TimeScale, panel behaviors, blur, accessibility, localization, Android back) | ⚠️ ×11 | Some deferred to ADR-020/022 (P2); others are implementation details within ADR-011's framework |

**Assessment**: The single ❌ (TR-objint-022 haptic feedback) is a Presentation-layer concern deferred to P2. The 37 ⚠️ items are predominantly waiting for P1 ADRs (013 for Object Interaction, 017 for Audio). Architecture.md provides sufficient interface contracts to begin Sprint 0 spikes. **No blocking gaps in Core given the P1 ADR timeline.**

### Feature & Presentation Layers — Expected Gaps

All 61 Feature/Presentation TRs are covered (24 ✅) or partial (37 ⚠️). The ⚠️ items correspond directly to the 7 unwritten P1 ADRs and 8 unwritten P2 ADRs. This is expected and **does not constitute a failure**.

### Cross-Cutting — Testing Gap

| TR ID | Requirement | Status | Notes |
|-------|------------|:------:|-------|
| TR-concept-011 | Unity Test Framework, 70%+ coverage for gameplay logic | ⚠️ | Mentioned in `technical-preferences.md` but no ADR establishes test architecture, CI integration, or coverage enforcement mechanism |

**Recommendation**: Consider adding testing architecture to the control manifest or a lightweight ADR before Sprint 1 begins.

---

## Phase 4: Cross-ADR Conflict Detection

### 4.1 Conflict Registry

#### CONFLICT-001: Event ID Allocation Mismatch [CRITICAL]

| ADR | Input Gesture Event IDs | Status |
|-----|-------------------------|--------|
| **ADR-006** (GameEvent Protocol) | `Evt_Gesture_Tap = 1000`, `Evt_Gesture_Drag = 1001`, ..., `Evt_Gesture_LightDrag = 1004` | Canonical allocation |
| **ADR-010** (Input Abstraction) | `GestureTap = 5001`, `GestureDrag = 5002`, ..., `GestureLightDrag = 5005` | Conflicting allocation |

**Impact**: If both ADRs are implemented as-written, the Input System will dispatch on IDs 5001–5005 but Object Interaction (consuming per ADR-006) will listen on IDs 1000–1004. **Zero gesture events will be received.** This is a build-time-silent, runtime-fatal bug.

**Resolution**: ADR-006 is the authoritative event protocol. ADR-010 must adopt ADR-006's ID allocation (1000–1004). The `InputEventId` class in ADR-010 should be removed; all event IDs must live in the centralized `EventId` class per ADR-006's Rule.

#### CONFLICT-002: UI Event IDs Outside Allocated Range [MODERATE]

| ADR | UI Event IDs | Allocated Range |
|-----|-------------|-----------------|
| **ADR-006** | No explicit UI event range allocated | Max allocated: 2099 |
| **ADR-011** | `PanelOpened = 6001`, `PanelClosed = 6002`, `PopupQueued = 6003`, `PopupDequeued = 6004` | 6001–6004 (outside any range) |

**Impact**: ADR-011's UI events are outside ADR-006's allocation scheme. While not causing runtime collision (6000+ is unused), it violates ADR-006's centralization principle and creates precedent for uncontrolled ID sprawl.

**Resolution**: Allocate a UI System range in ADR-006's `EventId` class (e.g., 2100–2199 or extend the existing scheme) and move ADR-011's event IDs into it.

#### CONFLICT-003: Layer Naming Inconsistency [MODERATE]

| Document | Layer Names |
|----------|------------|
| **architecture.md** | Platform → Foundation → Core → Feature → Presentation |
| **ADR-006** (`EventId.cs` comments) | Foundation → Core → Game → Meta |

**Impact**: ADR-006's `EventId.cs` comments label Object Interaction (Core layer per architecture.md) under "Foundation Layer", and uses "Game Layer" / "Meta Layer" instead of "Feature" / "Presentation". Developers referencing ADR-006's allocation table will see wrong layer assignments.

**Resolution**: Update ADR-006's `EventId.cs` comments to use canonical layer names from architecture.md.

#### CONFLICT-004: Audio Memory Budget Discrepancy [MINOR]

| Source | Audio Memory Budget |
|--------|:-------------------:|
| **ADR-003** (Mobile-First Platform) | < 80 MB |
| **TR-audio-012** (Audio System GDD) | < 30 MB |

**Impact**: ADR-003 allows 2.67× more audio memory than the GDD specifies. Systems designed to ADR-003's budget may violate the GDD target.

**Resolution**: Reconcile to 30 MB as the binding constraint (GDD is more specific). Update ADR-003's asset pipeline table.

#### CONFLICT-005: UI Prefab Memory Discrepancy [MINOR]

| Source | UI Prefab Memory Budget |
|--------|:-----------------------:|
| **ADR-011** (Performance Implications) | ~5–15 MB |
| **TR-ui-018** (UI System GDD) | < 5 MB |

**Impact**: ADR-011's expected range (5–15 MB) exceeds the GDD's strict < 5 MB target.

**Resolution**: The < 5 MB GDD target is the binding constraint. ADR-011 should note this as a hard budget and plan for aggressive atlas optimization.

#### CONFLICT-006: Save Load Time Discrepancy [MINOR]

| Source | Load Time Budget |
|--------|:----------------:|
| **ADR-008** (Save System) | < 200 ms |
| **TR-save-015** (Save GDD) | < 100 ms |

**Impact**: ADR-008 allows 2× the GDD target. Implementations tested against ADR-008's budget may fail the GDD requirement.

**Resolution**: Adopt < 100 ms as the binding constraint for load, < 50 ms for save (per GDD). ADR-008's < 200 ms can remain as a worst-case mobile budget.

#### CONFLICT-007: Shadow Draw Call Budget [MINOR]

| Source | Shadow Draw Calls |
|--------|:-----------------:|
| **ADR-002** (Performance Budgets) | < 20 additional from ShadowSampleCamera |
| **TR-render-017** (Rendering GDD) | Shadow system allocation ≤ 40 |

**Impact**: Not a true conflict — ADR-002's "< 20" refers specifically to the ShadowSampleCamera's overhead, while TR-render-017's "≤ 40" includes the full shadow system (main shadow map + sample camera). However, the metrics are measuring different scopes without explicit reconciliation.

**Resolution**: Clarify in ADR-002 that the ≤ 40 total shadow draw call budget (TR-render-017) is the binding constraint, of which < 20 is the ShadowSampleCamera allocation.

### 4.2 Dependency Graph & Implementation Order

```
Level 0 (No dependencies — implement first):
  ADR-001  TEngine Framework Adoption
  ADR-002  URP Rendering Pipeline
  ADR-003  Mobile-First Platform Strategy
  ADR-008  Save System Architecture
  ADR-010  Input Abstraction

Level 1 (Depends on Level 0):
  ADR-004  HybridCLR Assembly Boundaries      → depends on ADR-001
  ADR-005  YooAsset Resource Lifecycle         → depends on ADR-001
  ADR-006  GameEvent Communication Protocol    → depends on ADR-001
  ADR-011  UIWindow Management & Layer Strategy → depends on ADR-001

Level 2 (Depends on Level 1):
  ADR-007  Luban Config Table Access Pattern   → depends on ADR-004
  ADR-009  Scene Lifecycle & Additive Strategy  → depends on ADR-001, ADR-005, ADR-006
```

**Topological sort result** (recommended acceptance order):

1. ADR-001 (root — all others depend on it)
2. ADR-002, ADR-003, ADR-008, ADR-010 (independent Level 0)
3. ADR-004, ADR-005, ADR-006, ADR-011 (Level 1)
4. ADR-007, ADR-009 (Level 2)

**Cycles**: ✅ **None detected.** The dependency graph is a clean DAG. The Chapter State ↔ Save System circular dependency identified in `systems-index.md` is resolved by the `IChapterProgress` interface (ADR-008 + architecture.md §6.4).

---

## Phase 5: Engine Compatibility

### 5.1 Engine Version Consistency

| ADR | Stated Engine Version | Match? |
|-----|----------------------|:------:|
| ADR-001 | Unity 2022.3.62f2 (LTS) | ✅ |
| ADR-002 | Unity 2022.3.62f2 LTS | ✅ |
| ADR-003 | Unity 2022.3.62f2 LTS | ✅ |
| ADR-004 | Unity 2022.3.62f2 (LTS) | ✅ |
| ADR-005 | Unity 2022.3.62f2 (LTS) | ✅ |
| ADR-006 | Unity 2022.3.62f2 (LTS) | ✅ |
| ADR-007 | Unity 2022.3.62f2 (LTS) | ✅ |
| ADR-008 | Unity 2022.3.62f2 LTS | ✅ |
| ADR-009 | Unity 2022.3.62f2 (LTS) | ✅ |
| ADR-010 | Unity 2022.3.62f2 (LTS) | ✅ |
| ADR-011 | Unity 2022.3.62f2 (LTS) | ✅ |

**All 11 ADRs reference the correct engine version.** ✅

### 5.2 Deprecated API Usage

No ADR references any API deprecated in Unity 2022.3. All APIs used (`Input.GetTouch()`, `Physics.Raycast()`, `AsyncGPUReadback`, `SceneManager.SetActiveScene()`, `Resources.UnloadUnusedAssets()`, `PlayerPrefs`, UGUI Canvas) are stable in the 2022.3 LTS track.

**Note**: The engine-reference docs in `docs/engine-reference/unity/` describe Unity 6.3 LTS. The Phase 0 baseline correctly identified that those breaking changes do NOT apply to this project. No ADR references Unity 6 APIs. ✅

### 5.3 MEDIUM Risk Domain Coverage

| MEDIUM Risk Domain | Addressed By | Coverage Quality |
|-------------------|-------------|:----------------:|
| **TEngine 6.0.0** | ADR-001 (comprehensive), Sprint 0 spike planned | ✅ Strong |
| **HybridCLR** | ADR-004 (comprehensive), Sprint 0 spike planned | ✅ Strong |
| **YooAsset 2.3.17** | ADR-005 (comprehensive), Sprint 0 spike planned | ✅ Strong |
| **Luban** | ADR-007 (comprehensive), Sprint 0 spike planned | ✅ Strong |
| **I2 Localization** | ⚠️ **No ADR**. Deferred to ADR-022 (P2). | ⚠️ Unaddressed |

**I2 Localization Gap**: This is the only MEDIUM-risk domain without architectural coverage. ADR-022 is P2 (Can Defer), but the architecture.md mentions I2 as the localization solution and TR-ui-021 requires all UI text via localization keys. If localization is needed for Vertical Slice (not just Alpha), ADR-022 should be elevated to P1.

### 5.4 Stale Version References

No stale version references found. All dependency versions are consistent:

| Dependency | Version in ADRs | Version in VERSION.md | Match? |
|-----------|----------------|----------------------|:------:|
| TEngine | 6.0.0 | 6.0.0 | ✅ |
| HybridCLR | "latest compatible" | "latest compatible" | ✅ |
| YooAsset | 2.3.17 | 2.3.17 | ✅ |
| UniTask | 2.5.10 | 2.5.10 | ✅ |
| DOTween | "latest" / "SDK" | "SDK" | ✅ |

---

## Phase 6: Architecture Document Coverage

### 6.1 System Coverage in Architecture Layers

All 15 systems from `systems-index.md` appear in `architecture.md`:

| # | System | systems-index Layer | architecture.md Layer | In Layer Map? | In Module Ownership? | In API Boundary? |
|---|--------|--------------------|-----------------------|:-------------:|:--------------------:|:----------------:|
| 1 | Input System | Foundation | Foundation | ✅ | ✅ §4.1 | ✅ §6.1 |
| 2 | URP Shadow Rendering | Foundation | Foundation | ✅ | ✅ §4.1 | ✅ (in ADR-002) |
| 3 | Save System | Core¹ | Foundation² | ✅ | ✅ §4.1 | ✅ §6.5 |
| 4 | Scene Management | Core¹ | Foundation² | ✅ | ✅ §4.1 | ✅ §6.9 |
| 5 | Object Interaction | Core¹ | Core | ✅ | ✅ §4.2 | ✅ §6.2 |
| 6 | Chapter State | Core¹ | Core | ✅ | ✅ §4.2 | ✅ §6.4 |
| 7 | UI System | Feature¹ | Core² | ✅ | ✅ §4.2 | — (in ADR-011) |
| 8 | Audio System | Feature¹ | Core² | ✅ | ✅ §4.2 | ✅ §6.8 |
| 9 | Shadow Puzzle System | Feature | Feature | ✅ | ✅ §4.3 | ✅ §6.3 |
| 10 | Hint System | Feature | Feature | ✅ | ✅ §4.3 | ✅ §6.6 |
| 11 | Narrative Event System | Feature | Feature | ✅ | ✅ §4.3 | ✅ §6.7 |
| 12 | Collectible System | Feature | Feature [Planned] | ✅ | ✅ §4.3 | — (planned) |
| 13 | Tutorial / Onboarding | Presentation | Presentation | ✅ | ✅ §4.4 | — |
| 14 | Settings & Accessibility | Presentation | Presentation | ✅ | ✅ §4.4 | — |
| 15 | Analytics | Presentation | Presentation [Planned] | ✅ | ✅ §4.4 | — |

¹ Layer per `systems-index.md` original classification
² Layer **changed** by architecture.md §3.3 with documented rationale

**Layer Reclassifications** (architecture.md §3.3):

| System | Original Layer | New Layer | Rationale | Assessment |
|--------|---------------|-----------|-----------|:----------:|
| Audio System | Feature → **Core** | Multiple upper-layer systems depend on it | ✅ Sound |
| UI System | Feature → **Core** | Depended on by Narrative, Tutorial, Settings, Scene | ✅ Sound |
| Save System | Core → **Foundation** | Lowest-level persistence, no gameplay logic | ✅ Sound |
| Scene Management | Core → **Foundation** | Lowest-level lifecycle, all systems run on top | ✅ Sound |

All reclassifications are well-justified and documented. ✅

### 6.2 Data Flow Completeness

Architecture.md defines 6 data flow diagrams:

| Flow | Section | Completeness |
|------|---------|:------------:|
| Frame Update Path (Touch → Puzzle → UI) | §5.1 | ✅ Complete with performance budgets per stage |
| Puzzle Complete Flow (PerfectMatch → Narrative → Scene) | §5.2 | ✅ Complete with event cascade |
| Event/Signal Communication Map | §5.3 | ✅ 39 events across 10 event groups |
| Save/Load Path (Boot → Load → Init) | §5.4 | ✅ Complete with auto-save triggers |
| Scene Transition Flow (11-step sequence) | §5.5 | ✅ Complete with event mapping |
| Initialization Order (20 steps) | §5.6 | ✅ Complete with critical ordering constraints |

**Missing Data Flow**: No explicit "Settings Change Propagation" flow showing how a settings change (e.g., volume, sensitivity) cascades through affected systems. Currently covered implicitly by `Evt_SettingChanged` in §5.3, but a diagram would improve clarity.

### 6.3 API Boundary Completeness

| Interface | Defined In | Methods | Events | Assessment |
|-----------|-----------|:-------:|:------:|:----------:|
| `IInputService` | arch.md §6.1 | 6 | 5 (via GameEvent) | ✅ Complete |
| `IObjectInteraction` | arch.md §6.2 | 2 queries | 4 events | ⚠️ Missing mutation APIs (SetLocked, SnapToTarget on InteractableObject class, not interface) |
| `IShadowPuzzle` | arch.md §6.3 | 5 queries + 2 config | 7 events | ✅ Complete |
| `IChapterState` | arch.md §6.4 | 7 queries | 3 events | ✅ Complete |
| `ISaveService` | arch.md §6.5 | 5 | 1 event | ✅ Complete |
| `IHintService` | arch.md §6.6 | 4 queries + 2 timer control | 2 events | ✅ Complete |
| `INarrativeEvent` | arch.md §6.7 | 2 queries | 6 events | ✅ Complete |
| `IAudioService` | arch.md §6.8 | 10 | 4 events (via GameEvent) | ✅ Complete |
| `ISceneService` | arch.md §6.9 | 3 queries | 8 events | ✅ Complete |

**Missing Interfaces**: No `ITutorialService`, `ISettingsService`, or `IAnalyticsService`. These are Presentation layer — acceptable to defer.

**`IObjectInteraction` Note**: The interface only exposes read-only queries (`GetSelectedObject()`, `IsAnyObjectDragging()`). Mutation operations (`SetLocked`, `SnapToTarget`) are on the `InteractableObject` MonoBehaviour class and triggered via GameEvent, not through the interface. This is architecturally consistent with P2 (Event-Driven Decoupling) but means the interface is incomplete as a service contract. ADR-013 should formalize this.

---

## Phase 7: Verdict & Recommendations

### Verdict: CONCERNS

The architecture passes the structural completeness bar. The 11 P0 ADRs cover all Foundation layer systems and one Core layer system (UI). The master architecture document provides comprehensive layer maps, data flows, API boundaries, and initialization ordering. The 5 architecture principles are well-defined and consistently applied.

However, **the 2 critical conflicts must be resolved** before `EventId.cs` is created (which blocks all inter-module communication):

### Blocking Issues (Must Fix Before Sprint 1)

| # | Issue | Severity | Resolution |
|---|-------|:--------:|------------|
| B-1 | Event ID conflict ADR-006 vs ADR-010 (Input gesture IDs 1000 vs 5001) | CRITICAL | ADR-010 must adopt ADR-006's allocation. Remove `InputEventId` class from ADR-010. |
| B-2 | Event ID conflict ADR-006 vs ADR-011 (UI events 6001+ outside range) | MODERATE | Allocate UI System event range in ADR-006's `EventId` class. |

### Recommended Fixes (Should Fix Before Sprint 1)

| # | Issue | Severity | Resolution |
|---|-------|:--------:|------------|
| R-1 | Layer naming in ADR-006 `EventId.cs` comments | MODERATE | Update to canonical names: Foundation / Core / Feature / Presentation |
| R-2 | Audio memory budget: ADR-003 (80 MB) vs GDD (30 MB) | MINOR | Adopt 30 MB as binding constraint |
| R-3 | UI prefab memory: ADR-011 (5–15 MB) vs GDD (5 MB) | MINOR | Adopt < 5 MB as binding constraint |
| R-4 | Save load time: ADR-008 (200 ms) vs GDD (100 ms) | MINOR | Adopt < 100 ms as binding constraint |
| R-5 | Shadow draw call scope clarification in ADR-002 | MINOR | Clarify ≤ 40 total, < 20 for ShadowSampleCamera |
| R-6 | I2 Localization risk (MEDIUM) with no ADR | MINOR | Evaluate if ADR-022 should be elevated from P2 to P1 if VS needs localization |

### Expected Gaps (Acceptable — Track for P1/P2)

| ADR | System(s) | Priority | When Needed |
|-----|-----------|:--------:|-------------|
| ADR-012 | Shadow Match Algorithm | P1 | Before Shadow Puzzle Sprint |
| ADR-013 | Object Interaction State Machine | P1 | Before Object Interaction Sprint |
| ADR-014 | Puzzle State Machine & Absence | P1 | Before Shadow Puzzle Sprint |
| ADR-015 | Hint Trigger Formula & Escalation | P1 | Before Hint System Sprint |
| ADR-016 | Narrative Sequence Engine | P1 | Before Narrative Sprint |
| ADR-017 | Audio Mix Architecture | P1 | Before Audio Sprint |
| ADR-018 | Performance Monitoring & Auto-Degradation | P1 | Before Vertical Slice |
| ADR-019–026 | Presentation & Optimization (8 ADRs) | P2 | Before Alpha |

### Overall Assessment

| Dimension | Grade | Notes |
|-----------|:-----:|-------|
| Traceability (TR → ADR) | B+ | 124/212 fully covered; all Foundation TRs addressed; Core gaps are P1-expected |
| Cross-ADR Consistency | B- | 2 critical ID conflicts + 5 minor budget discrepancies need reconciliation |
| Engine Compatibility | A | All 11 ADRs on correct engine version; no deprecated APIs; MEDIUM risks addressed |
| Architecture Document | A | Comprehensive: 5-layer map, 6 data flows, 9 API interfaces, 20-step init order |
| Dependency Structure | A | Clean DAG, no cycles, clear topological order |
| Readiness for Sprint 1 | B | Structurally ready; blocked only by CONFLICT-001/002 resolution |

### Next Steps

1. **[Immediate]** Resolve CONFLICT-001: Update ADR-010 to use ADR-006's event ID allocation
2. **[Immediate]** Resolve CONFLICT-002: Allocate UI event range in ADR-006, update ADR-011
3. **[Immediate]** Fix ADR-006 layer naming to match architecture.md
4. **[Sprint 0]** Reconcile the 4 minor performance budget discrepancies
5. **[Sprint 0]** Complete all Sprint 0 validation criteria across 11 ADRs (TEngine API verification, HybridCLR device test, YooAsset SceneHandle test)
6. **[Sprint 0]** Decide if I2 Localization ADR-022 needs P1 elevation
7. **[Per Sprint]** Write P1 ADRs (012–018) before each system's implementation sprint begins

---

*End of Architecture Review Report*
