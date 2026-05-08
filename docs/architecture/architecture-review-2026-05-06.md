// 该文件由Cursor 自动生成

# Architecture Review Report — 影子回忆 (Shadow Memory)

> **Date**: 2026-05-06
> **Reviewer**: Technical Director (fresh session — independent review per ADR-029 V2.0 author cannot review own work)
> **Review Type**: FULL — all 7 phases per `.claude/skills/architecture-review/SKILL.md`
> **Engine**: Unity 2022.3.62f2 LTS
> **Framework**: TEngine 6.0.0 + HybridCLR + YooAsset 2.3.17 + UniTask 2.5.10
> **Inputs**: 21 ADRs (ADR-001..018 + ADR-027..029) + 13 system GDDs + systems-index + engine reference + tr-registry + previous review (2026-04-22) + master architecture document v1.0.0
> **Trigger**: ADR-029 V1.0 → V2.0 promotion (2026-04-30 dusk) + lite propagation v2 to ADR-027 §5 — fresh-session independent re-validation

---

## Verdict

### **CONCERNS** — _structurally sound, blocked by governance hygiene rather than design conflicts_

The architecture is **internally consistent and engine-compatible** for Unity 2022.3.62f2 LTS. All 7 cross-ADR conflicts identified in the 2026-04-22 review are **resolved or formally bound**. ADR-027 supersession (2026-04-23), ADR-013 v3 (2026-04-29), ADR-028 (2026-04-29), ADR-029 V2.0 (2026-04-30) are all coherent and add governance depth without breaking existing decisions. The S3-01 D5 propagation to ADR-005/ADR-009 (2026-04-30) is correctly executed and verified by SP-011 + S3-01 PlayMode CORE PASSED.

**However, three governance gaps must be acknowledged before Sprint 4 plan**:

1. 🔴 **14 of 21 ADRs remain in `Proposed` status** despite Sprint 2/3 implementation actively referencing them (`docs/CLAUDE.md` rule: "stories referencing a Proposed ADR are auto-blocked"). Multiple Accepted ADRs depend on still-Proposed ADRs — DAG technically broken.
2. 🟡 **`tr-registry.yaml` is empty** (template only) while `architecture-traceability.md` carries the de-facto 212-TR registry — process drift between `/architecture-review` skill protocol and project's chosen single source of truth.
3. 🟡 **Single ADR-027 internal-consistency drift** (line 222-224 still references legacy `EventId.ChapterCompleted` const-int sample) — minor, but a reviewer pattern miss.

None of these are design defects; they are governance bookkeeping owed to the architecture itself catching up with sprint execution speed. The recommended Sprint 4 entry pre-action is a **bulk Proposed → Accepted promotion ceremony** for the 14 remaining ADRs (~30 min total), not a content rewrite.

---

## Executive Summary

| Metric | Value | Δ vs 2026-04-22 |
|--------|------:|----------------:|
| **Total TRs (212)** | 212 | unchanged |
| **✅ Covered** | 124 (58.5%) | unchanged |
| **⚠️ Partial** | 87 (41.0%) | unchanged |
| **❌ Gap** | 1 (0.5%) | unchanged |
| **ADRs reviewed** | 21 (was 11) | **+10** (ADR-012..018, ADR-027/028/029) |
| **Cross-ADR conflicts (active)** | 1 minor | -2 critical, -2 moderate, -3 minor (all resolved) |
| **Cross-ADR conflicts (resolved since last review)** | 7 | — |
| **Engine compatibility issues** | 0 | unchanged |
| **Dependency cycles** | 0 | unchanged |
| **Status: Accepted ADRs** | 6 | +5 (ADR-005/009/013/027/028/029) |
| **Status: Proposed ADRs** | 14 | -3 net (added 7-018, removed 005/009/013) |
| **Status: Superseded ADRs** | 1 | +1 (ADR-006) |

### Top 3 Issues (priority order)

1. **🔴 [GOVERNANCE]** **Proposed-ADR backlog blocks dependency DAG**: ADR-005 (Accepted) depends on ADR-001 (Proposed); ADR-009 (Accepted) depends on ADR-005 (✓) + ADR-001 (Proposed); ADR-013 (Accepted) depends on ADR-010 (Proposed) + ADR-027 (✓); ADR-027 (Accepted) depends on ADR-001 + ADR-004 (both Proposed); ADR-028 (Accepted) depends on ADR-001/005/009/013/027 (mix). Per `docs/CLAUDE.md` "Never skip Accepted — stories referencing a Proposed ADR are auto-blocked." **Resolution**: bulk-promote 14 Proposed ADRs to Accepted before Sprint 4 plan. Sprint 0 spike validations are essentially complete (SP-001..SP-011 all in control-manifest), so this is a documentation update, not new work.

2. **🟡 [PROCESS DRIFT]** **`tr-registry.yaml` is empty**: The skill protocol expects TR-IDs to be persisted in `tr-registry.yaml` for cross-skill stable references. Current state: 212 TRs documented in `architecture-traceability.md` § "1. TR → ADR Mapping (by System)", 0 entries in `tr-registry.yaml`. **Resolution**: backfill 212 entries from `architecture-traceability.md` + `phase0-tr-baseline.md` to `tr-registry.yaml`, OR formally amend `/architecture-review` skill expectation to point at `architecture-traceability.md` as the SSoT. Pick one; do not run with both.

3. **🟡 [DRIFT]** **ADR-011 line 222-224 has legacy `EventId.ChapterCompleted` sample** in the Implementation Guidelines code block, contradicting §"8. UI GameEvent 接口（遵循 ADR-027）" earlier in the same ADR. Single drift point; 2-line fix. **Resolution**: replace with `GameEvent.AddEventListener<int>(IChapterStateEvent_Event.OnChapterComplete, OnChapterCompleted)` + apply ADR-027 §5 framework knowledge fact (null-out + null-check guard) per the 2026-04-30 lite propagation v2.

---

## Phase 1 — Inputs Loaded

Loaded **21 ADRs** + **13 GDDs** + **systems-index.md** + **engine reference (Unity 2022.3.62f2 LTS)** + **tr-registry.yaml** (empty template) + **`docs/architecture/architecture.md` v1.0.0** + **`docs/architecture/architecture-traceability.md` v1.0** + **`docs/architecture/control-manifest.md`** + **`docs/architecture/phase0-tr-baseline.md`** (212 TRs) + previous review report `architecture-review-2026-04-22.md` + Session 21 RESUME ANCHOR (2026-04-30 dusk) + `production/sprint-status.yaml`.

`docs/consistency-failures.md`: not present.

### ADR Status Snapshot (2026-05-06)

| Status | Count | ADRs |
|--------|------:|------|
| **Accepted** | 6 | ADR-005, ADR-009, ADR-013, ADR-027, ADR-028, ADR-029 V2.0 |
| **Superseded** | 1 | ADR-006 (by ADR-027) |
| **Proposed** | 14 | ADR-001, ADR-002, ADR-003, ADR-004, ADR-007, ADR-008, ADR-010, ADR-011, ADR-012, ADR-014, ADR-015, ADR-016, ADR-017, ADR-018 |

### GDD Status Snapshot

All 13 GDDs are in `Status: Draft`. Previous-review TR baseline (212) intact; no GDD wording revisions appear since 2026-04-21 except `scene-management.md` Core Rules §"场景加载/卸载规则" updated 2026-04-30 to reflect S3-01 D5 (string-name pattern, `GameModule.Scene` API correction).

---

## Phase 2 — Technical Requirements Baseline

The 212-TR baseline from `phase0-tr-baseline.md` (generated 2026-04-21) and the corresponding `architecture-traceability.md` mapping remain the authoritative requirements set. **No new TRs were extracted in this review** — all 212 TRs are already accounted for, and the GDDs have not introduced new requirements since the previous review.

**TR Registry consistency**: `docs/architecture/tr-registry.yaml` is **empty** (template only with example entries, 0 active entries). The 212 TRs are tracked instead in `architecture-traceability.md`. This is a process drift the skill should either accept (treat `architecture-traceability.md` as canonical) or backfill.

### Per-system TR counts (unchanged from prior review)

| System | TRs | ✅ | ⚠️ | ❌ |
|--------|----:|---:|---:|---:|
| Input | 18 | 18 | 0 | 0 |
| URP Shadow Rendering | 23 | 14 | 9 | 0 |
| Save / Chapter State | 20 | 19 | 1 | 0 |
| Scene Management | 17 | 15 | 2 | 0 |
| Object Interaction | 22 | 5 | 16 | 1 |
| UI System | 22 | 11 | 11 | 0 |
| Audio System | 15 | 5 | 10 | 0 |
| Shadow Puzzle System | 14 | 6 | 8 | 0 |
| Hint System | 17 | 5 | 12 | 0 |
| Narrative Event System | 12 | 4 | 8 | 0 |
| Tutorial / Onboarding | 10 | 5 | 5 | 0 |
| Settings & Accessibility | 8 | 4 | 4 | 0 |
| Cross-cutting (Concept) | 14 | 13 | 1 | 0 |
| **TOTAL** | **212** | **124** | **87** | **1** |

---

## Phase 3 — Traceability Matrix

The full 212-TR → ADR matrix is preserved in `docs/architecture/architecture-traceability.md` and is **valid as of 2026-05-06**. Spot-checked the following high-risk rows:

| TR | GDD | Coverage | ADR(s) | Verified |
|----|-----|:--------:|--------|:--------:|
| TR-input-001..018 | input-system.md | 18/18 ✅ | ADR-010 (Proposed) + ADR-027 §A.1 (Accepted) | ✅ |
| TR-render-019 (ShadowRT CPU ≤ 1.5ms) | urp-shadow-rendering.md | ✅ | ADR-002 (Proposed) + ADR-012 (Proposed) | ✅ |
| TR-scene-014 (8 scene events) | scene-management.md | ✅ | ADR-009 (Accepted) + ADR-027 §A.5 (Accepted) | ✅ |
| TR-scene-017 (chapter scene identity) | scene-management.md | ✅ | ADR-009 + ADR-005 — **revised** to "_currentChapterSceneName: string" pattern (S3-01 D5 propagation 2026-04-30) | ✅ |
| TR-objint-022 (haptic feedback) | object-interaction.md | ❌ | _ADR-025 (P2) pending_ | ✅ (only gap) |
| TR-concept-013 (event-driven) | game-concept.md | ✅ | ADR-027 (replaces ADR-006 §1/§2) | ✅ |

**Single gap**: `TR-objint-022` (Haptic feedback cross-platform) — deferred to ADR-025 (P2 backlog). No regression vs 2026-04-22.

### Coverage delta since 2026-04-22

The 11→21 ADR expansion (ADR-012..018 + ADR-027/028/029) **did not increase coverage**. ADR-012..018 were already in the prior matrix's "expected gap" backlog and they fill those gaps; the matrix totals remain unchanged because those TRs were already counted as ⚠️ Partial. Adding ADR-012..018 graduated some ⚠️ to ✅ during the previous review's Phase 5 — this is reflected in the 124/87/1 totals.

ADR-027/028/029 are governance ADRs that don't add TR coverage; they refine **how** existing coverage is implemented (events, modules, story authoring).

**Phase 3b (RTM) skipped**: This review is `full` mode, not `rtm` mode. RTM extension (Story → Test linkage) deferred to a dedicated `/architecture-review rtm` invocation post-Sprint 4 when more stories are Done.

---

## Phase 4 — Cross-ADR Conflict Detection

### Resolved since 2026-04-22 (7 prior conflicts)

| ID | Severity | Resolution | How |
|----|:--------:|:----------:|-----|
| CONFLICT-001 | CRITICAL | ✅ RESOLVED | ADR-006 §1 (1000-1004) and ADR-010 (5001-5005) **both deprecated** by ADR-027 (2026-04-23). Event IDs no longer hand-allocated; Source Generator hashes them from `IGestureEvent_Event.OnTap` symbol. Verified in ADR-010 line 202+ ("遵循 ADR-027 `[EventInterface]` 协议，取代原 ADR-006 §1 const int 分配") and ADR-027 §A.1 mapping table. |
| CONFLICT-002 | MODERATE | ✅ RESOLVED | UI Event ID range concern obsolete — ADR-027 has no "ranges". UI events use `GroupUI` interfaces, no manual ID allocation. ADR-011 §"8. UI GameEvent 接口（遵循 ADR-027）" updated. |
| CONFLICT-003 | MODERATE | ✅ RESOLVED | Layer naming canonicalized to Foundation/Core/Feature/Presentation across all docs. Verified in `architecture.md` §3.1 + control-manifest §1.3. |
| CONFLICT-004 | MINOR | ✅ BOUND | Audio memory binding constraint = 30 MB (GDD). Reflected in control-manifest §1.2 ("Audio memory < 30 MB — source: ADR-017"). |
| CONFLICT-005 | MINOR | ✅ BOUND | UI prefab memory binding = < 5 MB (GDD). Reflected in TR-ui-018. |
| CONFLICT-006 | MINOR | ✅ BOUND | Save load < 100 ms (GDD), save < 50 ms. Reflected in TR-save-015. |
| CONFLICT-007 | MINOR | ✅ BOUND | Shadow draw call: ≤ 40 total (TR-render-017), < 20 ShadowSampleCamera. Architecture.md §5.1 documents both budgets. |

### Newly checked pair-set (210 pairs across 21 ADRs)

Spot-checked the 7 user-flagged high-risk pairs + a focused conflict scan. All clean except one minor drift below.

#### Conflict (or near-conflict) inventory — 2026-05-06

##### CONFLICT-008: ADR-011 internal sample drift [MINOR]

**Type**: Pattern conflict within a single ADR (intra-ADR, not cross-ADR)

**ADR-011 §"7. 通信约束" (Implementation Guidelines example block, lines 217–225)** still shows:

```csharp
// 跨系统事件（通过 GameEvent）
GameEvent.AddEventListener(EventId.ChapterCompleted, OnChapterCompleted);
GameEvent.RemoveEventListener(EventId.ChapterCompleted, OnChapterCompleted);
```

This contradicts **ADR-011 §"8. UI GameEvent 接口（遵循 ADR-027）"** (later in the same ADR), which mandates `[EventInterface]` interfaces and explicitly states "本 ADR 不再定义独立的 `UIEventId` 常量类". The §"8" header explicitly references ADR-027. The §7 sample is leftover from the 2026-04-22 draft.

**Impact**: Developers reading ADR-011 §7 first will copy the legacy const-int pattern, violating ADR-027 and triggering an ADR-029 R1 readiness STOP at story implementation time.

**Resolution options**:
1. **(Recommended)** Rewrite §7 sample to the ADR-027 form:
   ```csharp
   GameEvent.AddEventListener<int>(IChapterStateEvent_Event.OnChapterComplete, OnChapterCompleted);
   // ... in cleanup, with ADR-027 §5 framework knowledge fact:
   if (_handler != null) {
       GameEvent.RemoveEventListener<int>(IChapterStateEvent_Event.OnChapterComplete, _handler);
       _handler = null;
   }
   ```
2. Add a comment `// LEGACY ADR-006 ID — superseded by ADR-027 (see §8 below)` and keep the line as historical context.

#### ADR-027 §5 framework knowledge fact propagation review (user request)

The lite-propagation v2 added a ⚠️ Framework knowledge fact section (TEngine `RemoveEventListener` non-idempotent + null-out + null-check guard pattern) into **ADR-027 §5 Lifecycle 协议表 only**. Verified as the **single source of truth (SSoT)** per ADR-029 V2.0 §V2-4 propagation scope rule (a) — cross-cutting framework knowledge fact, ROI 73%.

**Cross-ADR check**:

| ADR | Pattern that would have benefited from the fact | Conflict? |
|-----|------------------------------------------------|:---------:|
| ADR-009 §"Scene Lifecycle / 8 listener events" | Scene listeners must follow same pattern when Phase 1.5 of Story 003+ propagates | Compatible — points to ADR-027 §5 by reference |
| ADR-013 §"Event Protocol (ADR-027 Compliance)" | Object interaction subscribers (gesture / lock events) | Compatible |
| ADR-016 §"Listeners" (in narrative implementation) | Sequence subscriber lifecycle | Compatible — Depends-On already includes ADR-027 |
| ADR-015 (Hint), ADR-017 (Audio) — also subscribe to events | Listener self-removal pattern | Compatible (ADR-027 inherited via dep) |

**No conflict introduced.** The propagation correctly used the SSoT pattern; downstream ADRs inherit the contract via their `Depends On: ADR-027` line.

#### ADR-029 V2.0 §V2-3 R3 mandatory rule — cross-check

**Rule**: "R3 PlayMode probe is mandatory readiness gate, not optional." Applies to all stories before close.

**Check**: Does any other ADR contradict this by saying "EditMode test is sufficient" / "skip PlayMode for X reason"?

- ADR-013 (Object Interaction) §"Validation Criteria" mentions 17 NUnit EditMode tests as evidence — but these are **for the FSM**, not for framework integration. Story 001 (FSM) is FSM-pure, no framework boundary calls. Compatible — does **not** contradict R3 rule, because R3 only mandates PlayMode for "new method / new field / new contract" landing (§V2-6 trigger #4). Internal FSM logic is not a framework boundary.
- ADR-014 (Puzzle State Machine) §"Sprint 0 Spike" mentions Editor-only validation for the state graph. Same logic — internal state machine is not a framework boundary. Compatible.
- All Accepted ADRs that touch framework boundaries (ADR-005/009/013/027) explicitly reference Sprint validation criteria that include PlayMode spikes (SP-007 HybridCLR, SP-011 YooAsset, S3-01..03 PlayMode CORE PASSED). Compatible.

**No conflict.**

#### ADR-029 V2.0 §V2-4 propagation v2 — cross-check vs ADR-005 / ADR-009 framework knowledge fact opportunities

**Rule**: When a framework knowledge fact is found, propagate to the relevant framework ADR's SSoT slot.

**Inventory of "framework knowledge fact slots" by ADR**:

| Framework | ADR | SSoT slot | Currently populated? |
|-----------|-----|-----------|:--------------------:|
| TEngine GameEvent | ADR-027 §5 Lifecycle | `RemoveEventListener` non-idempotent + null-out pattern | ✅ (2026-04-30) |
| TEngine GameModule.Scene | ADR-009 §"Engine Compatibility" + §"Scene Handle Update" | API correction (`GameModule.Scene` not `GameModule.Resource`) + `_currentChapterSceneName: string` not `SceneHandle` | ✅ (2026-04-30, S3-01 D5) |
| YooAsset | ADR-005 §"Scene Loading Update" | scene-handle pattern superseded; `CheckLocationValid` returns false for scene assets (Type-2(a)) | ✅ (2026-04-30) |
| TEngine GameModule.UI | ADR-011 | UIWindow lifecycle quirks (TEngine doesn't preload `Refresh` etc.) | ⚠️ NOT YET (open question OQ-2 in arch.md §9) |
| Luban | ADR-007 | `Tables` singleton thread safety in async context | ⚠️ NOT YET (open question OQ-4 in arch.md §9) |
| HybridCLR | ADR-004 | AOT generic limitations + `[EventInterface]` AOT references requirement | ⚠️ Partially (ADR-027 §Migration step 11.5 covers `[EventInterface]` AOT) |

**Recommendation**: Sprint 4 should opportunistically capture framework knowledge facts encountered during UI sprint (ADR-011 SSoT slot) and Luban access (ADR-007 SSoT slot). This is a **future-pattern observation**, not a conflict.

### ADR Dependency Graph

#### Topological order

```
Foundation (no deps):
  ADR-001 [PROPOSED]  TEngine 6.0 Framework
  ADR-002 [PROPOSED]  URP Shadow Rendering
  ADR-003 [PROPOSED]  Mobile-First Platform
  ADR-008 [PROPOSED]  Save System
  ADR-010 [PROPOSED]  Input Abstraction
  ADR-029 [ACCEPTED V2.0]  Story Impl Notes Verification (process governance, depends on ADR-027 only)

Level 1 (deps on Foundation):
  ADR-004 [PROPOSED]  HybridCLR Assembly      → ADR-001 [PROPOSED]
  ADR-005 [ACCEPTED]  YooAsset Lifecycle      → ADR-001 [PROPOSED]   ⚠️ INVALID — Accepted depends on Proposed
  ADR-006 [SUPERSEDED] GameEvent (legacy)
  ADR-011 [PROPOSED]  UIWindow Management     → ADR-001 [PROPOSED]
  ADR-012 [PROPOSED]  Shadow Match Algorithm  → ADR-002 [PROPOSED]
  ADR-015 [PROPOSED]  Hint System             → ADR-012 [PROPOSED]
  ADR-017 [PROPOSED]  Audio Mix               → ADR-001 [PROPOSED]
  ADR-018 [PROPOSED]  Performance Monitoring  → ADR-002 [PROPOSED]
  ADR-027 [ACCEPTED]  GameEvent Interface     → ADR-001 [PROPOSED] + ADR-004 [PROPOSED]   ⚠️ INVALID

Level 2:
  ADR-007 [PROPOSED]  Luban Config Access     → ADR-004 [PROPOSED]
  ADR-009 [ACCEPTED]  Scene Lifecycle         → ADR-001 [PROPOSED] + ADR-005 [ACCEPTED]   ⚠️ PARTIAL INVALID
  ADR-013 [ACCEPTED]  Object Interaction      → ADR-010 [PROPOSED] + ADR-027 [ACCEPTED]   ⚠️ PARTIAL INVALID
  ADR-014 [PROPOSED]  Puzzle State Machine    → ADR-008 [PROPOSED] + ADR-012 [PROPOSED]
  ADR-016 [PROPOSED]  Narrative Sequence      → ADR-027 [ACCEPTED] + ADR-007 [PROPOSED]
  ADR-028 [ACCEPTED]  TEngine Module Usage    → ADR-001/005/009/013/027 (mixed)            ⚠️ MULTIPLE INVALID
```

**Cycles**: ✅ None detected. The DAG is clean (Chapter State ↔ Save System resolved by `IChapterProgress` per arch.md §6.4).

**Unresolved dependencies**: 5 Accepted ADRs depend on Proposed ADRs:
- ADR-005 → ADR-001 (Proposed)
- ADR-009 → ADR-001 (Proposed)
- ADR-013 → ADR-010 (Proposed)
- ADR-027 → ADR-001 + ADR-004 (both Proposed)
- ADR-028 → ADR-001/004/etc.

Per `docs/CLAUDE.md`:
> "Never skip Accepted — stories referencing a Proposed ADR are auto-blocked"

This is a **bookkeeping problem, not a content problem**. Sprint 0 verification work that the Proposed ADRs are waiting on is empirically complete (SP-001..SP-011 listed in control-manifest §"Sprint 0 Findings Covered"; SP-007/SP-011 explicitly closed in Sprint 2/3 stories). The promotion ceremony is overdue.

---

## Phase 5 — Engine Compatibility Cross-Check

**Engine**: Unity 2022.3.62f2 (LTS) — pinned 2026-04-16 per `docs/engine-reference/unity/VERSION.md`.

### 5.1 Version Consistency

All 21 ADRs that mention an engine version reference Unity 2022.3.62f2 LTS. ✅

| Engine reference doc | Notes |
|----------------------|-------|
| `VERSION.md` | Project pinned 2022.3.62f2; LLM cutoff May 2025; "Do NOT suggest Unity 6 APIs" |
| `breaking-changes.md` | Documents Unity 6.3 breaking changes (forward-looking, not applicable to 2022.3) |
| `deprecated-apis.md` | Documents Unity 6.3 deprecations (forward-looking) |
| `current-best-practices.md` | Unity 6 best practices — explicitly NOT applicable |
| `phase0-tr-baseline.md` §1 | Confirms "those breaking changes and deprecations do NOT apply to this project" |

### 5.2 Post-Cutoff API Consistency

Cross-checked `Post-Cutoff APIs Used` fields across all ADRs that have them:

| API | Used in | Conflict? |
|-----|---------|:---------:|
| `GameModule.Resource.LoadAssetAsync<T>()` | ADR-001, ADR-005, ADR-011 | Compatible |
| `GameModule.Scene.LoadSceneAsync(string, LoadSceneMode, Action<float>) → UniTask<Scene>` | ADR-009 (CORRECTED 2026-04-30 from fantasy `GameModule.Resource.LoadSceneAsync`) | ✅ Verified by S3-01 PlayMode |
| `GameModule.Scene.UnloadAsync(string) → UniTask<bool>` | ADR-009 | ✅ Verified |
| `GameModule.Scene.ActivateScene(string) → bool` | ADR-009 | ✅ Verified |
| `[EventInterface(EEventGroup)]` + Source Generator | ADR-027 | Verified by `ILoginUI.cs` exemplar |
| `GameEventHelper.Init()` | ADR-027 | Verified — required first line of `GameApp.Entrance` |
| `GameEvent.Get<T>()` / `GameEvent.AddEventListener<TArg>(...)` | ADR-027, all event-using ADRs | Compatible |
| `GameEvent.RemoveEventListener<TArg>(...)` — NON-IDEMPOTENT | ADR-027 §5 ⚠️ Framework knowledge fact (2026-04-30) | Compatible (single SSoT) |
| `AsyncGPUReadback.Request()` | ADR-002, ADR-012 | Stable in 2022.3 |
| `Resources.UnloadUnusedAssets()` + `GC.Collect()` | ADR-005, ADR-009 | Stable |
| `Input.GetTouch()` / `Input.touchCount` | ADR-010 | Stable in 2022.3 (still default — Unity 6 deprecates but 2022.3 stays) |

**No conflicts.** All post-cutoff APIs are project-source-verified or covered by Sprint 0 spike findings.

### 5.3 Deprecated API Check

Grep across all 21 ADRs for APIs listed in `deprecated-apis.md`:

| Deprecated API | Found in ADRs? | Notes |
|----------------|:--------------:|-------|
| `Input.GetKeyDown` etc. | No (custom Touch handling only — `Input.GetTouch()`) | The 2022.3 LTS allows old Input class; Unity 6 deprecates. ADR-010 stays on legacy Input intentionally (rejected New Input System in Alt 2 evaluation). Compatible. |
| `Resources.Load<T>()` | Listed only in **forbidden patterns** (3 ADRs + control-manifest) | ✅ Correctly listed as anti-pattern |
| `StartCoroutine(...)` | Listed only in **forbidden patterns** | ✅ Correctly listed as anti-pattern |
| `Canvas` (UGUI) | Used (ADR-011 explicitly chooses UGUI over UI Toolkit) | UGUI is "Deprecated but supported" in Unity 6 docs but **fully primary in 2022.3** + TEngine UIModule built around it. ✅ Correct decision per ADR-011 Alt 3 rejection. |
| `ComponentSystem` / `IComponentData` (DOTS) | Not used | Project does not use DOTS/ECS |
| `RenderGraph` / `RecordRenderGraph` | Not used | RenderGraph API is Unity 6+ only — correctly absent from ADR-002 |
| `UIDocument` / UI Toolkit | Not used (explicitly rejected in ADR-011 Alt 3) | ✅ |
| `Addressables.LoadAssetAsync` | Not used (project uses YooAsset, not Addressables) | ✅ |

**Result**: 0 actual deprecated-API usages. All "deprecated" mentions are in forbidden-patterns reference lists. ✅

### 5.4 Missing Engine Compatibility Sections

| ADR | Has Engine Compatibility section? |
|-----|:---------------------------------:|
| ADR-001..018 | ✅ All present |
| ADR-027 | ✅ |
| ADR-028 | ✅ |
| ADR-029 | ✅ (process-level, marked "engine-agnostic") |

**No blind spots.** ✅

### 5.5 Engine Specialist Consultation (skill phase 5 step 2)

Per skill: spawn primary engine specialist (`unity-specialist`) for second opinion. **Decision: skipped in this run** — the parent agent's instructions explicitly state "You are running as a subagent under a parent agent. Do not spawn additional subagents unless requested by the user or by your instructions." Recommendation: parent agent may launch unity-specialist with the ADRs that have `Post-Cutoff APIs Used` (ADR-001/005/009/010/011/012/013/027) as a follow-up sweep. Expected confirmation; low likelihood of new findings given the 2022.3 LTS maturity + S3-01 PlayMode CORE PASSED + SP-007/SP-011 closure.

### Engine Audit Result

```
Engine: Unity 2022.3.62f2 LTS
ADRs with Engine Compatibility section: 21 / 21 total
Deprecated API References (actual): 0
Stale Version References: 0
Post-Cutoff API Conflicts: 0
Engine compatibility: ✅ PASS (no findings)
```

---

## Phase 5b — GDD Revision Flags (Architecture → GDD Feedback)

**No HIGH RISK engine findings from Phase 5** (all 0 — Unity 2022.3 LTS is stable LLM-trained territory; no Unity 6 API leakage; framework knowledge facts captured at the ADR layer per ADR-029 V2.0 §V2-4 SSoT rule).

**No GDD revision flags — all GDD assumptions are consistent with verified engine behaviour.**

The 2026-04-30 S3-01 D5 propagation already revised `scene-management.md` Core Rules (lines 75–88) to reflect the verified `GameModule.Scene` API surface. No further GDD revisions identified.

---

## Phase 6 — Architecture Document Coverage

`docs/architecture/architecture.md` v1.0.0 (1222 lines, 2026-04-22) covers all 15 systems from `systems-index.md`. Spot-checked:

| Systems index entry | architecture.md layer | API boundary section | OK? |
|---------------------|------------------------|----------------------|:---:|
| Input System | Foundation §3.1, §4.1 | §6.1 IInputService | ✅ |
| URP Shadow Rendering | Foundation | §3.2 (ADR-002 binding) | ✅ |
| Save System | Foundation (reclassified from Core) | §6.5 ISaveService | ✅ |
| Scene Management | Foundation (reclassified from Core) | §6.9 ISceneService | ✅ (footnote: D5 update on Owns row §4.1 reflects 2026-04-30 propagation) |
| Object Interaction | Core §4.2 | §6.2 IObjectInteraction | ✅ |
| Chapter State System | Core | §6.4 IChapterState + IChapterProgress | ✅ |
| UI System | Core (reclassified from Feature) | (in ADR-011) | ✅ |
| Audio System | Core (reclassified from Feature) | §6.8 IAudioService | ✅ |
| Shadow Puzzle System | Feature | §6.3 IShadowPuzzle | ✅ |
| Hint System | Feature | §6.6 IHintService | ✅ |
| Narrative Event System | Feature | §6.7 INarrativeEvent | ✅ |
| Collectible System | Feature [Planned] | _(planned)_ | ✅ Acknowledged |
| Tutorial / Onboarding | Presentation §4.4 | _(deferred to ADR-019 P2)_ | ✅ |
| Settings & Accessibility | Presentation | _(in ADR-020/022 P2)_ | ✅ |
| Analytics | Presentation [Planned] | _(deferred)_ | ✅ Acknowledged |

**Layer reclassifications** (architecture.md §3.3): all 4 reclassifications well-justified — Audio/UI promoted to Core, Save/Scene demoted to Foundation. Documented with rationale.

**Data Flow Coverage** (§5):
- §5.1 Frame Update Path — ✅ complete
- §5.2 Puzzle Complete Flow — ✅ complete
- §5.3 Event/Signal Communication Map — ✅ complete (39 events; updated 2026-04-23 with ADR-027 supersession note in callout)
- §5.4 Save / Load Path — ✅ complete
- §5.5 Scene Transition Flow (11 steps) — ✅ complete; line 615 was updated 2026-04-30 to S3-01 D5 (`GameModule.Scene.LoadSceneAsync("MainScene", Additive, null)`)
- §5.6 Initialization Order (20 steps) — ✅ complete

**API Boundaries** (§6): 9 service interfaces defined. **Missing**: `ITutorialService`, `ISettingsService`, `IAnalyticsService` (Presentation layer — acceptable to defer; aligns with deferred ADR-019/020/022/024).

**Architecture document orphans**: None. All systems in arch.md trace back to systems-index.md.

**No architecture document coverage gaps.** ✅

---

## Phase 7 — Output Report

(This section, as the final report, is the document you are reading.)

---

## Coverage Gaps (no ADR exists)

❌ **TR-objint-022** — `object-interaction.md` → Object Interaction → Haptic feedback cross-platform abstraction
   - Suggested ADR: `/architecture-decision Haptic Feedback Cross-Platform Abstraction`
   - Domain: Mobile platform abstraction
   - Engine Risk: LOW (iOS UIImpactFeedbackGenerator + Android Vibrator are stable APIs)
   - Priority: P2 (deferred to ADR-025 placeholder)
   - **Status unchanged** since 2026-04-22

---

## Cross-ADR Conflicts

### Active

#### CONFLICT-008: ADR-011 §7 Implementation Guidelines — legacy `EventId.ChapterCompleted` sample [MINOR]

**Type**: Pattern conflict (intra-ADR)

**ADR-011 §7 (~lines 222-224)**:
```csharp
GameEvent.AddEventListener(EventId.ChapterCompleted, OnChapterCompleted);
GameEvent.RemoveEventListener(EventId.ChapterCompleted, OnChapterCompleted);
```

**ADR-011 §"8. UI GameEvent 接口（遵循 ADR-027）"** (later in same ADR): mandates `[EventInterface]` + `IXxxEvent_Event` symbols, no const-int IDs.

**ADR-027 §5 Lifecycle ⚠️ Framework knowledge fact (2026-04-30)**: `RemoveEventListener` is non-idempotent; required null-out + null-check guard pattern.

**Impact**: Developers reading ADR-011 §7 first will copy the legacy pattern (auto-blocked at ADR-029 R1 readiness gate, costing 5-10 min drift per story). Also misses the framework knowledge fact entirely.

**Resolution options**:
1. **(Recommended)** Replace lines 222-224 with the ADR-027-compliant form including the null-out + null-check guard pattern. Add a `// see ADR-027 §5` comment.
2. Strikethrough the lines and add `// LEGACY ADR-006 — superseded by ADR-027 §"8" below`.

### Resolved (since 2026-04-22 review)

CONFLICT-001 through CONFLICT-007 — all resolved. See Phase 4 above for full resolution notes.

---

## ADR Dependency Order

### Recommended Implementation Order (topologically sorted)

```
Foundation (no deps):
  1. ADR-001  TEngine 6.0 Framework Adoption                    [PROPOSED — must promote]
  2. ADR-002  URP Rendering Pipeline                            [PROPOSED — must promote]
  3. ADR-003  Mobile-First Platform Strategy                    [PROPOSED — must promote]
  4. ADR-008  Save System Architecture                          [PROPOSED — must promote]
  5. ADR-010  Input Abstraction                                 [PROPOSED — must promote; ADR-013 dep blocked]
  6. ADR-029  Story Impl Notes Verification (process)           [ACCEPTED V2.0]

Depends on Foundation:
  7. ADR-004  HybridCLR Assembly Boundaries     (req: ADR-001)  [PROPOSED]
  8. ADR-005  YooAsset Resource Lifecycle        (req: ADR-001)  [ACCEPTED — depends on Proposed ⚠️]
  9. ADR-006  GameEvent Communication Protocol   — SUPERSEDED by ADR-027
 10. ADR-011  UIWindow Management & Layer        (req: ADR-001)  [PROPOSED]
 11. ADR-012  Shadow Match Algorithm             (req: ADR-002)  [PROPOSED]
 12. ADR-015  Hint System                         (req: ADR-012)  [PROPOSED]
 13. ADR-017  Audio Mix Architecture              (req: ADR-001)  [PROPOSED]
 14. ADR-018  Performance Monitoring              (req: ADR-002)  [PROPOSED]
 15. ADR-027  GameEvent Interface Protocol        (req: ADR-001+004)  [ACCEPTED — depends on Proposed ⚠️]

Level 2:
 16. ADR-007  Luban Config Access                 (req: ADR-004)  [PROPOSED]
 17. ADR-009  Scene Lifecycle                     (req: ADR-001+005)  [ACCEPTED — partial Proposed dep ⚠️]
 18. ADR-013  Object Interaction                  (req: ADR-010+027)  [ACCEPTED — partial Proposed dep ⚠️]
 19. ADR-014  Puzzle State Machine                (req: ADR-008+012)  [PROPOSED]
 20. ADR-016  Narrative Sequence Engine           (req: ADR-007+027)  [PROPOSED]
 21. ADR-028  TEngine Module Usage Policy         (req: ADR-001+005+009+013+027)  [ACCEPTED — multiple Proposed deps ⚠️]
```

### Unresolved Dependencies (governance violations)

⚠️ ADR-005 depends on ADR-001 — but ADR-001 is still Proposed.
⚠️ ADR-009 depends on ADR-001 (Proposed) + ADR-005 (Accepted) — partial.
⚠️ ADR-013 depends on ADR-010 (Proposed) + ADR-027 (Accepted) — partial.
⚠️ ADR-027 depends on ADR-001 (Proposed) + ADR-004 (Proposed) — fully Proposed deps.
⚠️ ADR-028 depends on ADR-001/004 (both Proposed) + ADR-005/009/013/027 — multiple Proposed.

### Cycles

✅ **None detected.** Chapter State ↔ Save System cycle resolved by `IChapterProgress` interface (arch.md §6.4 / ADR-008).

---

## GDD Revision Flags

**None — all GDD assumptions are consistent with verified engine behaviour.**

Notable: `scene-management.md` Core Rules already reflect the 2026-04-30 S3-01 D5 propagation (`GameModule.Scene` corrected from `GameModule.Resource` fantasy API; `_currentChapterSceneName: string` pattern documented). No further GDD revisions required from this review.

---

## Engine Compatibility Issues

**None.** See Phase 5 above. 21/21 ADRs with Engine Compatibility section; 0 deprecated APIs in actual usage; 0 Unity 6+ API leakage; 0 stale version references; 0 post-cutoff API conflicts.

---

## Architecture Document Coverage

`docs/architecture/architecture.md` (v1.0.0) covers all 15 systems from `systems-index.md`. Layer reclassifications documented and well-justified. 6 data flows complete. 9 API boundary interfaces defined (3 Presentation interfaces deferred to P2 ADRs as intended). No orphan architecture sections; no missing systems.

**One minor recommendation** (cosmetic, not blocking): `architecture.md` §"7.1 Current ADR Status" still says "Formal ADR files (`adr-*.md`) | **None exist**" — this was true at v1.0.0 generation (2026-04-22 morning, before ADRs were drafted) but is now stale. The 21 ADRs all exist. Update §7.1 next time arch.md is revised.

---

## Verdict: CONCERNS

### Why CONCERNS (not PASS, not FAIL)

- **Why not PASS**: 14 ADRs in Proposed status while production code references them; 4 Accepted ADRs depend on Proposed ADRs (DAG bookkeeping inconsistency); empty `tr-registry.yaml` while 212 TRs are live in `architecture-traceability.md`; 1 minor intra-ADR drift (CONFLICT-008).
- **Why not FAIL**: All conflicts identified in 2026-04-22 review are resolved or formally bound; engine compatibility is clean; coverage matrix unchanged at 124/87/1; no new design defects; no dependency cycles; ADR-027/028/029 all internally consistent and add governance depth without breaking existing decisions; the S3-01 D5 propagation is correctly executed and verified by SP-011 + S3-01 PlayMode.

The architecture is **structurally PASS**. The CONCERNS are governance hygiene issues that the project itself surfaced via ADR-029 V2.0 (which is precisely about catching this kind of drift).

---

## Blocking Issues (must resolve before PASS)

### B-1 [GOVERNANCE — must do before Sprint 4 plan]

Promote 14 Proposed ADRs to Accepted (or to Superseded as appropriate).

| ADR | Suggested next status | Justification for promotion |
|-----|:----------------------|------------------------------|
| ADR-001 (TEngine Framework) | Accepted | Sprint 0 spike effectively complete via SP-001/SP-002 + 2 sprints of code execution; foundational dep for 5 already-Accepted ADRs |
| ADR-002 (URP Shadow Rendering) | Accepted | Sprint 0 spike SP-005 (WallReceiver) closed; ShadowRT pipeline planned for upcoming sprint |
| ADR-003 (Mobile-First Platform) | Accepted | Performance budgets in active use across all ADRs |
| ADR-004 (HybridCLR Assembly) | Accepted | Sprint 0 spike SP-007 closed; production code already enforces boundary |
| ADR-007 (Luban Access) | Accepted | Spike SP-004 closed; ADR-013/-029 already cite as Accepted-equivalent |
| ADR-008 (Save System) | Accepted | Required for Sprint 4 chapter-state epic (4 stories: puzzle-ordering / chapter-progression / state-events / save-integration) |
| ADR-010 (Input Abstraction) | Accepted | S2 input epic delivered; current Status drift blocks ADR-013 dependency check |
| ADR-011 (UIWindow Management) | **Accepted with patch** (fix CONFLICT-008 first) | UI sprint upcoming |
| ADR-012 (Shadow Match Algorithm) | Accepted | Will block Shadow Puzzle implementation |
| ADR-014 (Puzzle State Machine) | Accepted | Same reason |
| ADR-015 (Hint System) | Accepted | Same reason |
| ADR-016 (Narrative Sequence) | Accepted | Same reason |
| ADR-017 (Audio Mix) | Accepted | ADR-028 §1 explicitly defers AudioModule activation to "ADR-017 Accept + Sprint 3 Audio 接入" — but Sprint 3 closed without it; reconcile |
| ADR-018 (Performance Monitoring) | Accepted | SP-010 closed; required for VS gate |

**Promotion ceremony scope**: ~30 minutes — single PR updating 14 Status fields, with `git commit -m "chore(adr): bulk Proposed → Accepted promotion ceremony post-Sprint 3"`.

### B-2 [GOVERNANCE — also before Sprint 4 plan]

Resolve **CONFLICT-008**: fix ADR-011 §7 lines 222-224 to use ADR-027 interface form + ADR-027 §5 framework knowledge fact pattern. ~5 min fix.

### B-3 [PROCESS — before next architecture-review run]

Reconcile `tr-registry.yaml` ←→ `architecture-traceability.md` SSoT:
- **Option A** (recommended): backfill 212 TRs from `architecture-traceability.md` + `phase0-tr-baseline.md` to `tr-registry.yaml`. ~45 min one-time investment, then both files maintained in tandem.
- **Option B**: amend `/architecture-review` skill's Phase 2 instructions to read `architecture-traceability.md` as canonical, mark `tr-registry.yaml` as deprecated.

Pick one and document the choice in a 1-page ADR or update to the skill SSoT docs.

---

## Required ADRs (prioritised)

### Top 3 ADR Gaps for Phase 9 Handoff

These are the ADRs that — once written — would close the most TR gaps. **None of these are blockers for Sprint 4** (Sprint 4 candidates per session-state RESUME ANCHOR: chapter-state epic / multi-scene narrative / urp-shadow-rendering / ui-system).

1. **ADR-014 (Puzzle State Machine & Absence Puzzle Variant)** — P1, depends on ADR-008 + ADR-012 (both promote-pending). Needed before Shadow Puzzle implementation sprint. **Closes ~9 ⚠️ TRs in Shadow Puzzle System** (TR-puzzle-005..009, 012..014).

2. **ADR-016 (Narrative Sequence Engine)** — P1, depends on ADR-027 (Accepted) + ADR-007 (promote-pending). Needed before Narrative Event sprint. **Closes ~8 ⚠️ TRs in Narrative Event System** (TR-narr-002, 003, 005..011).

3. **ADR-017 (Audio Mix Architecture)** — P1, depends on ADR-001. Needed before Audio sprint. ADR-028 §1 ties this to "AudioModule activation" gate. **Closes ~10 ⚠️ TRs in Audio System** (TR-audio-002..005, 008..011, 013, 014).

### Other gaps (P2 — Vertical Slice / Alpha)

- ADR-019 (Tutorial Step Engine) — P2; closes 4 ⚠️ TRs
- ADR-020 (Accessibility Architecture) — P2; closes 4 ⚠️ TRs
- ADR-022 (I2 Localization) — P2; closes 3 ⚠️ TRs (consider P1 elevation if VS needs localization)
- ADR-024 (Analytics Telemetry) — P2; no TRs yet (planned system)
- ADR-025 (Haptic Feedback) — P2; closes the **single ❌ gap** TR-objint-022

---

## Phase 9 — Handoff (per skill protocol)

### Immediate actions (in priority order)

1. **[Highest priority]** Resolve B-1 (bulk Proposed → Accepted promotion). Single PR, ~30 min.
2. **[Next]** Resolve B-2 (CONFLICT-008 ADR-011 §7 patch — 2 lines). ~5 min.
3. **[Next]** Resolve B-3 (TR registry SSoT decision + backfill if Option A). ~45 min if Option A; ~10 min if Option B.
4. **[Sprint 4 entry]** Run `/gate-check pre-production` after B-1/B-2/B-3 are done.
5. **[Per Sprint 4 epic]** Author next P1 ADRs as their epic begins (ADR-014 before puzzle epic; ADR-016 before narrative epic; ADR-017 before audio epic).

### Gate guidance

> **When B-1/B-2/B-3 are resolved, run `/gate-check pre-production` to advance.**

The current Pre-Production gate is **structurally** passable today; only the governance hygiene (B-1/B-2/B-3) needs to clear.

### Rerun trigger

> **Re-run `/architecture-review` after each new P1 ADR (ADR-014/016/017) is written to verify coverage improves.** Recommended cadence per skill: after each ADR finalised, run `coverage` mode (lighter) to quickly confirm no regression; run `full` mode at end of Sprint 4 / before VS gate.

---

## Session State Update Slot

If this report is accepted and written, append to `production/session-state/active.md`:

```
## Session Extract — /architecture-review 2026-05-06
- Verdict: CONCERNS
- Requirements: 212 total — 124 covered, 87 partial, 1 gap (TR-objint-022 unchanged)
- New TR-IDs registered: None (212 unchanged; tr-registry backfill pending)
- Conflicts resolved since prior review: 7 (CONFLICT-001..007)
- Active conflicts: 1 (CONFLICT-008 — ADR-011 §7 intra-ADR drift, MINOR)
- GDD revision flags: None
- Top ADR gaps: ADR-014 (Puzzle State Machine) / ADR-016 (Narrative Sequence) / ADR-017 (Audio Mix)
- Blocking issues: B-1 (14 ADR Proposed→Accepted promotion) / B-2 (ADR-011 §7 patch) / B-3 (tr-registry SSoT decision)
- Report: docs/architecture/architecture-review-2026-05-06.md (pending write approval)
```

---

*End of Architecture Review Report*
