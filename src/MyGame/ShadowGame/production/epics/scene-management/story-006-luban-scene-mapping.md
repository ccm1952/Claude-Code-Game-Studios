// 该文件由Cursor 自动生成

# Story 006: Luban Scene Name ↔ Chapter ID Mapping

> **Epic**: Scene Management
> **Status**: Ready
> **Layer**: Core
> **Type**: Config/Data
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/scene-management.md`
**Requirement**: `TR-scene-015` (Luban-driven scene registry; no hardcoded scene names)

**ADR Governing Implementation**: ADR-009: Scene Lifecycle + ADR-007: Luban Config Access
**ADR Decision Summary**: Scene name ↔ chapter ID mapping is read from Luban `TbChapter.sceneId`. No hardcoded scene names exist anywhere in code. This allows hot-update of scene mappings post-launch. Scene Manager reads the mapping before every scene load and resolves the asset path from the config.

**Engine**: Unity 2022.3.62f2 LTS + Luban (latest) | **Risk**: LOW
**Engine Notes**: `TbChapter` is a Luban-generated table in the `GameProto` assembly. Access via `ConfigSystem.Tables.TbChapter.Get(chapterId)`. Returns a `ChapterData` object with at minimum: `int Id`, `string SceneId`, `string BgmAsset`, `float EmotionalWeight`. SP-004 confirmed: all reads on main thread are safe; never read inside `UniTask.Run()`.

**Control Manifest Rules (this layer)**:
- Required: `All config reads via Tables.Instance.TbXXX.Get(id)` (ADR-007)
- Required: `Tables.Init() executes at boot Step 7 (ProcedureMain) — MUST complete before ANY system reads config` (ADR-007)
- Required: `Scene name ↔ chapter ID mapping from Luban TbChapter.sceneId — no hardcoded scene names` (ADR-009)
- Required: `Config data objects are read-only after Init() — never modify a field on a Luban-generated data object at runtime` (ADR-007)
- Forbidden: `Never hardcode gameplay values` (ADR-007)
- Forbidden: `Never access Luban Tables from UniTask.Run() (thread pool)` (SP-004)
- Forbidden: `Never cache config table references in persistent fields` (ADR-007)
- Forbidden: `Never hand-edit any file in GameProto` (ADR-007)

---

## Acceptance Criteria

*From GDD `design/gdd/scene-management.md`, scoped to this story:*

- [ ] `TbChapter` table contains at minimum these fields per chapter row: `Id (int)`, `SceneId (string)`, `BgmAsset (string)`, `EmotionalWeight (float)`, `OverlayColor (Color/string)`
- [ ] `SceneManager` resolves `sceneName` by calling `ConfigSystem.Tables.TbChapter.Get(chapterId).SceneId` before every `LoadSceneAsync` call — no hardcoded scene names in any C# file
- [ ] If `TbChapter.Get(chapterId)` returns null (unknown chapter ID), Scene Manager transitions to `Error` state and fires `Evt_SceneLoadFailed` with descriptive error message
- [ ] `EmotionalWeight` field from `TbChapter` is used to scale fade durations: `fadeOutDuration = FADE_BASE_OUT * emotionalWeight` and `fadeInDuration = FADE_BASE_IN * emotionalWeight`
- [ ] `BgmAsset` field is forwarded in `Evt_SceneLoadComplete` payload for the Audio system to switch music
- [ ] `OverlayColor` field is used by the Transition Overlay Canvas for chapter-specific fade color (resolves from config, not hardcoded)
- [ ] A grep/static analysis scan of the entire codebase finds zero hardcoded scene name strings (e.g., `"Chapter_01_Approach"`, `"Chapter_02_SharedSpace"`)

---

## Implementation Notes

*Derived from ADR-007 + ADR-009 Implementation Guidelines:*

Scene name resolution is a one-liner in `BeginTransition`:
```csharp
private async UniTask BeginTransition(RequestSceneChangePayload request)
{
    var chapterData = ConfigSystem.Tables.TbChapter.Get(request.TargetChapterId);
    if (chapterData == null) {
        GameEvent.Send(EventId.Evt_SceneLoadFailed, new SceneLoadFailedPayload {
            ChapterId = request.TargetChapterId,
            Error = $"Chapter ID {request.TargetChapterId} not found in TbChapter"
        });
        _currentState = SceneManagerState.Error;
        return;
    }
    string sceneName = chapterData.SceneId;
    float fadeOut = FADE_BASE_OUT * chapterData.EmotionalWeight;
    float fadeIn  = FADE_BASE_IN  * chapterData.EmotionalWeight;
    // ... continue with transition
}
```

**Luban table definition** (`TbChapter.xlsx` or equivalent — do NOT hand-edit the generated `TbChapter.cs`):
| Field | Type | Notes |
|-------|------|-------|
| Id | int | Chapter number (1–5) |
| SceneId | string | Unity scene name (e.g., `"Chapter_01_Approach"`) |
| BgmAsset | string | YooAsset address for music clip |
| EmotionalWeight | float | 0.8–1.5 — multiplier for fade duration |
| OverlayColor | string | Hex color string for transition overlay |

**No caching of table references**: read via `ConfigSystem.Tables.TbChapter.Get(id)` each time (exception: within a single method call for performance-hot paths — but BeginTransition is not per-frame).

**SP-004 note**: `BeginTransition` is called from the main thread (GameEvent handler). The `Get()` call is safe. Never wrap it in `UniTask.Run()`.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: The actual `LoadSceneAsync` call using the resolved `sceneName`
- Story 005: Events that carry BgmAsset in their payloads (already implemented there)
- Art Bible: defining what the chapter overlay colors should be — the config value is provided by Design

---

## QA Test Cases

*Config/Data type — smoke check against loaded data:*

- **AC-1**: TbChapter rows exist and are valid
  - Setup: Game starts; `Tables.Init()` completes
  - Verify: `ConfigSystem.Tables.TbChapter.Get(1)` returns non-null; `SceneId` is non-empty string; `EmotionalWeight` is in range [0.5, 2.0]
  - Pass condition: All 5 chapter rows return valid `SceneId` values; no nulls or empty strings

- **AC-2**: No hardcoded scene names in codebase
  - Setup: Run grep on all `.cs` files in `Assets/GameScripts/`
  - Verify: No file contains string literals matching pattern `"Chapter_0[1-5]_"` or known scene names
  - Pass condition: Zero matches

- **AC-3**: Unknown chapter ID → Error state + Evt_SceneLoadFailed
  - Setup: Request scene change with chapterId = 99 (not in TbChapter)
  - Verify: Scene Manager enters Error state; `Evt_SceneLoadFailed` fires with `ChapterId=99` and non-empty error message
  - Pass condition: No NullReferenceException; graceful error message; UI recoverable

- **AC-4**: EmotionalWeight scales fade duration
  - Setup: Chapter 1 has `EmotionalWeight=0.8`; Chapter 5 has `EmotionalWeight=1.5`
  - Verify: Fade-out duration for Chapter 1 = `0.8 × 0.8s = 0.64s`; for Chapter 5 = `1.5 × 0.8s = 1.2s`
  - Pass condition: Actual tween duration matches formula within ±0.05s tolerance

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**:
- Smoke check pass: `production/qa/smoke-critical-paths.md` updated with config validation steps

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 (state machine), Story 002 (BeginTransition uses resolved sceneName)
- Unlocks: No story is directly blocked by this — it refines Story 002's implementation with data-driven scene names
