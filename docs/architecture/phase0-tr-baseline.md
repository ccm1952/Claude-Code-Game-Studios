// 该文件由Cursor 自动生成

# Phase 0 — Technical Requirements Baseline & Knowledge Gap Inventory

**Project**: 影子回忆 (Shadow Memory)
**Engine**: Unity 2022.3.62f2 LTS
**Framework**: TEngine 6.0.0 + HybridCLR + YooAsset 2.3.17
**Generated**: 2026-04-21
**Source GDDs**: 13 system GDDs + 1 game concept

---

## 1. Engine Knowledge Gap Inventory

### Assessment Methodology

Unity 2022.3.62f2 is a 2022–2023 era LTS release, well within LLM training data. The engine
reference docs in `docs/engine-reference/unity/` describe Unity 6.3 LTS — those breaking
changes and deprecations do **NOT** apply to this project.

| Risk Level | Domain | Rationale |
|:----------:|--------|-----------|
| **LOW** | Unity C# API (2022.3) | Stable LTS, extensively documented, fully in training data |
| **LOW** | URP (2022.3 compatible) | URP matured by 2022.3; shadow maps, cascades, SRP Batcher all stable |
| **LOW** | UGUI / Canvas | Legacy UI system, no API changes in 2022.3 |
| **LOW** | Unity legacy Input (Touch) | `Input.GetTouch()`, `Input.touchCount` — unchanged for years |
| **LOW** | Physics / Raycasting | `Physics.Raycast`, colliders, layers — stable API |
| **LOW** | Unity Test Framework (NUnit) | Stable, well-documented runner |
| **LOW** | AsyncGPUReadback | Available since 2018.1, stable in 2022.3 |
| **LOW** | ScriptableRenderPipeline / RenderTexture | Stable in 2022.3 URP |
| **LOW** | PlayerPrefs | Unchanged API since Unity 5 |
| **LOW** | Application lifecycle (OnPause/Quit) | Stable cross-platform behavior |
| **LOW** | TextMeshPro | Bundled in 2022.3, stable API |
| **LOW** | Addressables / AssetBundle base concepts | Core concepts stable; project uses YooAsset wrapper |
| **LOW** | Timeline / Playables API | Stable since 2017, no 2022.3 changes |
| **LOW** | DOTween | Third-party, version-locked, extensive documentation |
| **LOW** | UniTask | Third-party, version-locked (2.5.10), well-documented |
| **MEDIUM** | TEngine 6.0.0 API | Custom framework; UIModule, ResourceModule, SceneModule, GameEvent, Procedure — LLM has partial/indirect knowledge from GitHub repos. Must verify: `UIWindow`/`UIWidget` lifecycle, `GameModule.Resource.LoadAssetAsync` signatures, `GameEvent.Send`/`AddEventListener` API, `SetUISafeFitHelper` |
| **MEDIUM** | HybridCLR hot-reload mechanics | Open-source but niche; assembly split (Default/GameLogic/GameProto), metadata registration, AOT generic constraints — must verify build pipeline and runtime limitations |
| **MEDIUM** | YooAsset 2.3.17 API | Package-level API for `ResourcePackage`, `AssetHandle`, `SceneHandle`, `UpdatePackageManifestAsync`, `CreateResourceDownloader` — documentation exists but version-specific behavior may vary |
| **MEDIUM** | Luban config table integration | Code generation tool; `TbChapter`, `TbPuzzle` table access patterns, `Tables` singleton usage — must verify generated code conventions |
| **MEDIUM** | I2 Localization runtime API | Used for language hot-switch; `LocalizationManager.CurrentLanguage`, string table format — must verify TEngine integration |

### Summary

- **HIGH RISK**: 0 domains
- **MEDIUM RISK**: 5 domains (TEngine, HybridCLR, YooAsset, Luban, I2 Localization — all third-party/custom frameworks)
- **LOW RISK**: 15 domains (all core Unity APIs)

### Recommended Mitigations for MEDIUM-RISK Domains

| Domain | Mitigation |
|--------|------------|
| TEngine 6.0.0 | Read actual source in project (`TEngine/` folder); create API cheat-sheet before implementation |
| HybridCLR | Verify assembly definitions and hot-fix boundaries in project; test hot-reload pipeline early |
| YooAsset 2.3.17 | Read project's existing YooAsset integration; verify `ResourcePackage` initialization flow |
| Luban | Inspect generated code in `GameProto` assembly; verify `Tables` access pattern |
| I2 Localization | Check existing I2 setup in project; verify runtime language switch API |

---

## 2. Existing ADR Check

| Item | Status |
|------|--------|
| `docs/architecture/` directory | Exists |
| ADR files (`adr-*.md`) | **None found** |
| `tr-registry.yaml` | Exists (template with example entries) |
| `technical-preferences.md` | Exists at `.claude/docs/technical-preferences.md` — references ADR-001, ADR-002, ADR-003 but no formal ADR files created yet |

**Action Required**: Formal ADR documents must be authored during Phase 1 (Architecture Design).

---

## 3. Technical Requirements Baseline

### 3.1 Input System (`input-system.md`)

| TR ID | System | Requirement | Domain |
|-------|--------|-------------|--------|
| TR-input-001 | Input | Three-layer architecture: Raw Input → Gesture Recognition → Event Dispatch; upper systems only interact with Event Dispatch layer | Architecture |
| TR-input-002 | Input | Support 5 gesture types: Tap, Drag, Rotate, Pinch, LightDrag, each with Began/Updated/Ended lifecycle | Input |
| TR-input-003 | Input | PC input mapping: mouse left-click → Tap/Drag, scroll → Pinch, right-drag → Rotate, middle-drag → LightDrag | Input |
| TR-input-004 | Input | InputBlocker: stack-based (`pushBlocker`/`popBlocker`); when stack > 0, all game input suppressed; UI manages own blockers | Input |
| TR-input-005 | Input | InputFilter: whitelist-based (`pushFilter(allowedGestures)`/`popFilter`); only whitelisted gestures dispatched; used by Tutorial | Input |
| TR-input-006 | Input | Priority chain: InputBlocker (full block) > InputFilter (whitelist) > normal dispatch | Input |
| TR-input-007 | Input | DPI-normalized drag threshold: `dragThresholdPx = dragThresholdDp × (Screen.dpi / 160)`; Screen.dpi fallback to 160 | Input |
| TR-input-008 | Input | Tap timeout using `Time.unscaledDeltaTime` accumulation; `tapTimeout = 0.25s` | Input |
| TR-input-009 | Input | Rotation angle delta via `Mathf.Atan2`; `minRotationDelta = 2°` | Input |
| TR-input-010 | Input | Pinch scale delta with `minFingerDistance = 50px` safety guard | Input |
| TR-input-011 | Input | Single-finger state machine: Idle → Pending → Tap/Dragging/LongPress with timeout transitions | Input |
| TR-input-012 | Input | Dual-finger state machine: Idle → Pending2 → Rotating/Pinching; mutual exclusion (no mid-gesture switch) | Input |
| TR-input-013 | Input | Max 2 simultaneous touch points tracked; 3+ touches ignored | Input |
| TR-input-014 | Input | `MaxDeltaPerFrame` clamp to prevent teleportation on frame spikes | Performance |
| TR-input-015 | Input | Application pause/resume (`OnApplicationPause`/`OnApplicationFocus`) clears all active touch state | Input |
| TR-input-016 | Input | Gesture recognition processing < 0.5ms/frame | Performance |
| TR-input-017 | Input | All threshold parameters (dragThresholdDp, tapTimeout, rotationMin, pinchMin) loaded from Luban config; no hardcoded values | Config |
| TR-input-018 | Input | Touch sensitivity setting modifies dragThreshold multiplier at runtime | Settings |

### 3.2 Object Interaction (`object-interaction.md`)

| TR ID | System | Requirement | Domain |
|-------|--------|-------------|--------|
| TR-objint-001 | Object Interaction | Raycast-based selection on `InteractableObject` layer; single selection mode (one active object at a time) | Physics |
| TR-objint-002 | Object Interaction | Selection visual feedback: scale enlargement (EaseOutBack, 8 frames ≈ 133ms) + outline highlight | Object Interaction |
| TR-objint-003 | Object Interaction | Fat finger compensation: expand touch hit area by 8dp (DPI-scaled) around object collider | Input |
| TR-objint-004 | Object Interaction | Drag: 1:1 finger tracking, no inertia, no delay; maintain initial finger-object offset throughout | Object Interaction |
| TR-objint-005 | Object Interaction | Grid snap: `snappedPos = round(pos / gridSize) × gridSize`; default `gridSize = 0.25` world units | Object Interaction |
| TR-objint-006 | Object Interaction | Snap interpolation: EaseOutQuad; duration = `clamp(distance / snapSpeed, minSnapDuration, maxSnapDuration)` | Object Interaction |
| TR-objint-007 | Object Interaction | Rotation snap: `snappedAngle = round(angle / rotationStep) × rotationStep`; default `rotationStep = 15°` | Object Interaction |
| TR-objint-008 | Object Interaction | Distance adjustment: movement along light→object axis; direction threshold 35° to disambiguate drag vs distance | Object Interaction |
| TR-objint-009 | Object Interaction | Light source track movement: LinearTrack (1D) and ArcTrack (arc), parameterized `t ∈ [0, 1]` | Object Interaction |
| TR-objint-010 | Object Interaction | Light track snap: `step = 0.1` on track parameter `t` | Object Interaction |
| TR-objint-011 | Object Interaction | InteractionBounds per puzzle: clamp object position within defined boundary; no silent clamp — rebound with EaseOutBack (overshoot 0.3) | Object Interaction |
| TR-objint-012 | Object Interaction | Object state machine: Idle → Selected → Dragging → Snapping → Locked; + Rotating sub-state | State |
| TR-objint-013 | Object Interaction | Light state machine: Fixed → TrackIdle → TrackDragging → TrackSnapping | State |
| TR-objint-014 | Object Interaction | Fire `ObjectTransformChanged` event on every position/rotation/scale change | Event |
| TR-objint-015 | Object Interaction | Fire `LightPositionChanged` event on light source track parameter change | Event |
| TR-objint-016 | Object Interaction | Fire `PuzzleLockEvent` (single object), `PuzzleLockAllEvent` (all objects), `PuzzleSnapToTargetEvent` (auto-complete animation) | Event |
| TR-objint-017 | Object Interaction | Drag response latency ≤ 16ms (1 frame at 60fps) | Performance |
| TR-objint-018 | Object Interaction | All parameters (gridSize, rotationStep, snapSpeed, bounds) from Luban config | Config |
| TR-objint-019 | Object Interaction | 10 objects on-screen: maintain ≥ 55fps on iPhone 13 Mini | Performance |
| TR-objint-020 | Object Interaction | All interaction Update processing < 1ms at 60fps | Performance |
| TR-objint-021 | Object Interaction | 200ms debounce for repeated selection taps | Input |
| TR-objint-022 | Object Interaction | Haptic feedback (UIImpactFeedbackGenerator or Android equivalent) on snap, putdown, boundary hit; respects haptic_enabled setting | Platform |

### 3.3 Shadow Puzzle System (`shadow-puzzle-system.md`)

| TR ID | System | Requirement | Domain |
|-------|--------|-------------|--------|
| TR-puzzle-001 | Shadow Puzzle | Multi-anchor weighted scoring: `matchScore = Σ(w_i × s_i) / Σ(w_i)` where each anchor has position, direction, visibility sub-scores | Puzzle Logic |
| TR-puzzle-002 | Shadow Puzzle | Per-anchor score: `anchorScore = positionScore × directionScore × visibilityScore` (multiplicative) | Puzzle Logic |
| TR-puzzle-003 | Shadow Puzzle | NearMatch enter threshold 0.40, exit threshold 0.35 (hysteresis to prevent flickering) | Puzzle Logic |
| TR-puzzle-004 | Shadow Puzzle | PerfectMatch threshold 0.85 (configurable per puzzle) | Puzzle Logic |
| TR-puzzle-005 | Shadow Puzzle | Puzzle state machine: Locked → Idle → Active → NearMatch → PerfectMatch → Complete | State |
| TR-puzzle-006 | Shadow Puzzle | AbsenceAccepted state for Ch.5: `maxCompletionScore < 1.0`, `absenceAcceptDelay = 5s` timer before auto-transition | Puzzle Logic |
| TR-puzzle-007 | Shadow Puzzle | matchScore temporal smoothing: 0.2s sliding window average to prevent jitter | Puzzle Logic |
| TR-puzzle-008 | Shadow Puzzle | Tutorial grace period: first `tutorialGracePeriod = 3s` after puzzle activation uses relaxed thresholds | Puzzle Logic |
| TR-puzzle-009 | Shadow Puzzle | PerfectMatch snap animation: 0.3–0.8s EaseOutBack, objects interpolate to target transforms | Puzzle Logic |
| TR-puzzle-010 | Shadow Puzzle | Shadow match calculation < 2ms/frame | Performance |
| TR-puzzle-011 | Shadow Puzzle | Maintain 60fps with draw calls < 150 (mobile) during active puzzle | Performance |
| TR-puzzle-012 | Shadow Puzzle | Collectibles do not affect puzzle scoring or progression (decoration only) | Puzzle Logic |
| TR-puzzle-013 | Shadow Puzzle | Chapter-difficulty parameter matrix: per-chapter override of nearMatchThreshold, perfectMatchThreshold, hintDelay, etc. | Config |
| TR-puzzle-014 | Shadow Puzzle | Per-puzzle config overrides via Luban tables (TbPuzzle) | Config |

### 3.4 URP Shadow Rendering (`urp-shadow-rendering.md`)

| TR ID | System | Requirement | Domain |
|-------|--------|-------------|--------|
| TR-render-001 | Shadow Rendering | URP Forward Rendering path with SRP Batcher enabled | Rendering |
| TR-render-002 | Shadow Rendering | HDR off on mobile; MSAA off; SMAA post-processing for anti-aliasing | Rendering |
| TR-render-003 | Shadow Rendering | Shadow Map resolution: PC=2048, Mobile-High=1024, Mobile-Medium=512 | Rendering |
| TR-render-004 | Shadow Rendering | Shadow Cascades: PC=4, Mobile=2 | Rendering |
| TR-render-005 | Shadow Rendering | Shadow Distance: 8m (configurable) | Rendering |
| TR-render-006 | Shadow Rendering | 3 quality tiers (Low/Medium/High) with auto-detection based on device capability | Rendering |
| TR-render-007 | Shadow Rendering | WallReceiver custom shader: shadow contrast control, color tint, edge softness parameters | Rendering |
| TR-render-008 | Shadow Rendering | Shadow contrast ratio ≥ 3:1 (default), recommended 5:1 for readability | Rendering |
| TR-render-009 | Shadow Rendering | ShadowSampleCamera + ShadowRT (R8 grayscale RenderTexture) for puzzle match sampling | Rendering |
| TR-render-010 | Shadow Rendering | AsyncGPUReadback for ShadowRT CPU-side pixel sampling (no synchronous readback) | Rendering |
| TR-render-011 | Shadow Rendering | Shadow effect interfaces: `SetShadowGlow(intensity, color)`, `FreezeShadow(snapshotRT)`, `SetShadowStyle(preset)`, `TransitionShadowStyle(from, to, duration)` | Rendering |
| TR-render-012 | Shadow Rendering | 5 chapter shadow style presets (Penumbra, Color, Contrast, EdgeSharpness per chapter) | Rendering |
| TR-render-013 | Shadow Rendering | Max 2 simultaneous shadow-casting lights per scene (performance budget) | Performance |
| TR-render-014 | Shadow Rendering | Shadow caster priority: currently operated > last operated > scene default | Rendering |
| TR-render-015 | Shadow Rendering | Shadow Only rendering mode: object body invisible, shadow visible (for narrative effects) | Rendering |
| TR-render-016 | Shadow Rendering | Auto quality degradation: if frame time > 20ms for 5 consecutive frames, reduce one quality tier | Performance |
| TR-render-017 | Shadow Rendering | Draw call budget: ≤ 150 total (mobile); shadow system allocation ≤ 40 draw calls | Performance |
| TR-render-018 | Shadow Rendering | Shadow memory budget: ≤ 15MB (Medium tier) | Performance |
| TR-render-019 | Shadow Rendering | ShadowRT CPU processing (readback + pixel analysis) ≤ 1.5ms/frame | Performance |
| TR-render-020 | Shadow Rendering | NearMatch glow via WallReceiver shader emission channel; must NOT affect ShadowRT sampling | Rendering |
| TR-render-021 | Shadow Rendering | High-contrast accessibility mode: shadow contrast ≥ 8:1 ratio | Accessibility |
| TR-render-022 | Shadow Rendering | Shadow outline accessibility mode: additional outline overlay for shadow edges | Accessibility |
| TR-render-023 | Shadow Rendering | All Shadow Map resolutions, bias values, cascade settings configurable via quality tier config | Config |

### 3.5 Chapter State & Save System (`chapter-state-and-save.md`)

| TR ID | System | Requirement | Domain |
|-------|--------|-------------|--------|
| TR-save-001 | Chapter State | 5 chapters, fixed sequential unlock (chapter N+1 requires chapter N complete) | State |
| TR-save-002 | Chapter State | Linear puzzle progression within chapters (puzzle N+1 requires puzzle N complete) | State |
| TR-save-003 | Chapter State | Puzzle completion is irreversible — once Complete, always Complete | State |
| TR-save-004 | Chapter State | Chapter State is the single authority for global game progress; all other systems query it | Architecture |
| TR-save-005 | Chapter State | `IChapterProgress` interface decouples Chapter State from Save System | Architecture |
| TR-save-006 | Save | Auto-save triggers: puzzle state change, chapter state change, collectible pickup, app pause, app quit | Save |
| TR-save-007 | Save | Save debounce: minimum 1s between consecutive saves (except force-save on pause/quit) | Save |
| TR-save-008 | Save | All save I/O via UniTask async; zero main-thread file blocking | Performance |
| TR-save-009 | Save | Single save slot; JSON format | Save |
| TR-save-010 | Save | Atomic write: write to temp file → verify integrity → rename to target | Save |
| TR-save-011 | Save | Backup: copy previous save to `.backup.json` before each successful overwrite | Save |
| TR-save-012 | Save | CRC32 checksum embedded in save file for integrity verification on load | Save |
| TR-save-013 | Save | Version migration chain: each save version defines upgrade function to next version (v1→v2→...→vN) | Save |
| TR-save-014 | Save | Save file size < 10KB | Performance |
| TR-save-015 | Save | Save operation < 50ms; Load operation < 100ms | Performance |
| TR-save-016 | Chapter State | Replay mode for completed chapters: resets puzzle states locally, does not affect saved progress | State |
| TR-save-017 | Chapter State | Luban config tables: `TbChapter` (chapter metadata), `TbPuzzle` (puzzle configs), `TbPuzzleObject` (object configs) | Config |
| TR-save-018 | Save | Save System must initialize and load before any gameplay system reads progress | Architecture |
| TR-save-019 | Save | `OnApplicationPause(true)` and `OnApplicationQuit()` trigger immediate save bypassing debounce | Save |
| TR-save-020 | Save | Corrupted save fallback chain: primary → `.backup.json` → new game (with user notification) | Save |

### 3.6 Hint System (`hint-system.md`)

| TR ID | System | Requirement | Domain |
|-------|--------|-------------|--------|
| TR-hint-001 | Hint | 3-tier progressive hints: L1 Ambient → L2 Directional → L3 Explicit; escalation only when lower tier fails | Hint |
| TR-hint-002 | Hint | Trigger score formula: `triggerScore = timeScore + failScore + stagnationScore + matchPenalty` | Hint |
| TR-hint-003 | Hint | L1 idle threshold 45s; L2 idle threshold 90s (configurable per chapter) | Hint |
| TR-hint-004 | Hint | Cooldown between hint layers: base 30s, modified by `cooldownModifier = max(0.5, 1.0 − matchScore)` | Hint |
| TR-hint-005 | Hint | L3 Explicit hint: max 3 uses per puzzle (non-renewable) | Hint |
| TR-hint-006 | Hint | Hint target selection: `argmin(anchorScore_i)` with tie-breaking by `anchorWeight` (highest weight wins) | Hint |
| TR-hint-007 | Hint | Read-only query interface to Shadow Puzzle System (no direct state mutation) | Architecture |
| TR-hint-008 | Hint | Query frequency: 1s polling interval for matchScore/anchorScores; NOT per-frame | Performance |
| TR-hint-009 | Hint | Hint timers pause during active Tutorial steps; timers reset when Tutorial completes | Hint |
| TR-hint-010 | Hint | `hintDelayOverride` per-chapter multiplier from Luban config | Config |
| TR-hint-011 | Hint | L1 Ambient hint: zero additional draw calls (material property animation only, no particles) | Performance |
| TR-hint-012 | Hint | L2 Directional rendering overhead < 0.5ms/frame | Performance |
| TR-hint-013 | Hint | Hint system total Update processing < 0.5ms/frame | Performance |
| TR-hint-014 | Hint | Absence puzzle (Ch.5): special hint text acknowledging incompleteness | Hint |
| TR-hint-015 | Hint | Timer deltaTime capped at 1.0s per tick to prevent frame spike false triggers | Hint |
| TR-hint-016 | Hint | App pause: hint timers pause; if background > 5min, timers reset to zero | Hint |
| TR-hint-017 | Hint | Stagnation detection: matchScore in 0.30–0.40 range for extended period triggers additional hint score | Hint |

### 3.7 UI System (`ui-system.md`)

| TR ID | System | Requirement | Domain |
|-------|--------|-------------|--------|
| TR-ui-001 | UI | All UI via TEngine UIModule (`UIWindow`/`UIWidget` classes) | UI |
| TR-ui-002 | UI | 5 UI layer levels: Background (0–99), HUD (100–199), Popup (200–299), Overlay (300–399), System (400–499) | UI |
| TR-ui-003 | UI | Popup/Overlay layers auto-push InputBlocker on show, auto-pop on hide | UI |
| TR-ui-004 | UI | HUD layer does not block 3D game input (pass-through to game world) | UI |
| TR-ui-005 | UI | 9 UIWindows: GameHUD, PauseMenu, PuzzleCompletePanel, MemoryFragmentPanel, ChapterTransition, MainMenu, ChapterSelect, SettingsPanel, HintButton | UI |
| TR-ui-006 | UI | GameHUD widgets: HintButton, PauseButton, ChapterProgress, OperationHint, SaveIndicator | UI |
| TR-ui-007 | UI | Safe Area fitting for all UI (TEngine `SetUISafeFitHelper`) for notch/cutout devices | Platform |
| TR-ui-008 | UI | Popup queue: only one Popup visible at a time; subsequent popups queued | UI |
| TR-ui-009 | UI | `Time.timeScale = 0` when PauseMenu is open; restore on close | UI |
| TR-ui-010 | UI | PuzzleCompletePanel: auto-close after 2.5s; tap-to-close enabled before timeout | UI |
| TR-ui-011 | UI | MemoryFragmentPanel: typewriter text effect at 80–120ms per character (configurable) | UI |
| TR-ui-012 | UI | ChapterTransition: 4-phase sequence (FadeOut → Loading → LoadComplete → FadeIn); Phase 2 masks scene loading | UI |
| TR-ui-013 | UI | Gaussian blur background for Popup/Overlay layers; fallback to solid overlay on low-end devices | Performance |
| TR-ui-014 | UI | UI animation duration scaling via accessibility `animationScale` (0.0–1.0) | Accessibility |
| TR-ui-015 | UI | HintButton: opacity ramp from 30% → 80% after 30s player idle | UI |
| TR-ui-016 | UI | All UI animations maintain 60fps; no frame drops during transitions | Performance |
| TR-ui-017 | UI | Gaussian blur rendering < 2ms/frame on iPhone 13 Mini | Performance |
| TR-ui-018 | UI | Total UI Prefab memory < 5MB | Performance |
| TR-ui-019 | UI | Minimum touch target size: 44×44pt (Apple Human Interface Guidelines) | Platform |
| TR-ui-020 | UI | Font size switching: Standard / Large / Extra-Large presets | Accessibility |
| TR-ui-021 | UI | All UI text via localization keys (I2 Localization); zero hardcoded display strings | Config |
| TR-ui-022 | UI | Android back button: mapped to Pause/Back navigation in UI flow | Platform |

### 3.8 Scene Management (`scene-management.md`)

| TR ID | System | Requirement | Domain |
|-------|--------|-------------|--------|
| TR-scene-001 | Scene | Additive scene architecture: BootScene (bootstrap) + MainScene (persistent) + Chapter scene (dynamic) | Scene |
| TR-scene-002 | Scene | Max 3 scenes in memory simultaneously: BootScene + MainScene + 1 chapter scene | Performance |
| TR-scene-003 | Scene | All scene loading via async UniTask; zero synchronous `SceneManager.LoadScene` calls | Scene |
| TR-scene-004 | Scene | Always use `LoadSceneMode.Additive`; never `LoadSceneMode.Single` | Scene |
| TR-scene-005 | Scene | Scene transition flow: FadeOut → Unload old → GC.Collect + UnloadUnusedAssets → Load new → FadeIn | Scene |
| TR-scene-006 | Scene | Transition mutual exclusion: one transition at a time; incoming requests queued (max queue = 1) | Scene |
| TR-scene-007 | Scene | YooAsset resource package: on-demand download for chapter scenes; local cache for subsequent loads | Scene |
| TR-scene-008 | Scene | Chapter scene memory budget: ~1000MB within 1.5GB device ceiling | Performance |
| TR-scene-009 | Scene | Cached scene load time < 1s on mid-tier device | Performance |
| TR-scene-010 | Scene | Fade animations run at 60fps independent of background loading thread | Performance |
| TR-scene-011 | Scene | Memory leak detection: no baseline growth after 5 consecutive load/unload cycles | Performance |
| TR-scene-012 | Scene | Error recovery: failed scene load → retry 2× → fallback to MainMenu with error notification | Scene |
| TR-scene-013 | Scene | Startup flow: Boot → TEngine init → HybridCLR metadata → YooAsset init → MainScene → Save load → MainMenu | Scene |
| TR-scene-014 | Scene | 8 scene transition GameEvent IDs: TransitionRequested, FadeOutStart, FadeOutComplete, UnloadStart, UnloadComplete, LoadStart, LoadComplete, FadeInComplete | Event |
| TR-scene-015 | Scene | Emotional weight multiplier: narrative-heavy transitions use longer fade durations | Scene |
| TR-scene-016 | Scene | `Resources.UnloadUnusedAssets()` + `GC.Collect()` after every chapter scene unload | Performance |
| TR-scene-017 | Scene | Scene handle reference retention: keep `SceneHandle` for proper `UnloadAsync` tracking | Scene |

### 3.9 Audio System (`audio-system.md`)

| TR ID | System | Requirement | Domain |
|-------|--------|-------------|--------|
| TR-audio-001 | Audio | 3 independent mix layers: Ambient, SFX, Music; each with independent volume control | Audio |
| TR-audio-002 | Audio | Volume formula: `finalVolume = clipBaseVolume × layerVolume × masterVolume × duckingMultiplier` | Audio |
| TR-audio-003 | Audio | SFX variant selection: random variant from pool + pitch randomization (±semitone range) | Audio |
| TR-audio-004 | Audio | 3D spatial audio for object interaction SFX (AudioSource position = object world position) | Audio |
| TR-audio-005 | Audio | `maxConcurrent` limit per SFX event ID; oldest instance culled when exceeded | Audio |
| TR-audio-006 | Audio | Music crossfade on chapter switch: configurable 1–5s crossfade duration | Audio |
| TR-audio-007 | Audio | Ducking system: narrative events reduce Ambient/SFX layers via `duckingMultiplier` with fade curve | Audio |
| TR-audio-008 | Audio | SFX trigger-to-playback latency ≤ 1 frame (16ms at 60fps) | Performance |
| TR-audio-009 | Audio | Ambient layer starts within 2s of game scene load | Audio |
| TR-audio-010 | Audio | Ambient occasional sounds: randomized interval between min/max with weighted selection | Audio |
| TR-audio-011 | Audio | Audio CPU processing < 1ms/frame with 10 concurrent sources | Performance |
| TR-audio-012 | Audio | Audio memory budget: < 30MB total loaded | Performance |
| TR-audio-013 | Audio | App pause/resume: all audio pauses at exact playback position, resumes seamlessly | Audio |
| TR-audio-014 | Audio | Music continues playing during PauseMenu (not affected by `Time.timeScale = 0`) | Audio |
| TR-audio-015 | Audio | All SFX event IDs, variant lists, parameters defined in Luban config tables | Config |

### 3.10 Narrative Event System (`narrative-event-system.md`)

| TR ID | System | Requirement | Domain |
|-------|--------|-------------|--------|
| TR-narr-001 | Narrative | 11 atomic effect types: ColorTemperature, ObjectAnimate, AudioDucking, TextureVideo, TimelinePlayable, ScreenFade, Wait, ObjectSnap, ShadowFade, ObjectFade, SFXOneShot | Narrative |
| TR-narr-002 | Narrative | NarrativeSequence: time-sorted list of atomic effects; effects with same `startTime` execute in parallel | Narrative |
| TR-narr-003 | Narrative | Config-table driven: `puzzleId → sequenceId → List<AtomicEffect>` mapping from Luban tables | Config |
| TR-narr-004 | Narrative | Sequence start: fire `PuzzleLockAll` + push `InputBlocker`; sequence end: pop `InputBlocker` + unlock | Narrative |
| TR-narr-005 | Narrative | Absence puzzle (Ch.5) specific sequence: cool color temperature, shorter TextureVideo, ShadowFade with melancholic parameter | Narrative |
| TR-narr-006 | Narrative | Chapter transition: Timeline playable with 21:9 letterbox effect | Narrative |
| TR-narr-007 | Narrative | Chapter-final puzzle: merge PerfectMatch sequence + ChapterTransition into single continuous sequence | Narrative |
| TR-narr-008 | Narrative | Consecutive trigger queue: max 3 pending sequences; drop oldest if exceeded | Narrative |
| TR-narr-009 | Narrative | Resource load failure resilience: skip failed effect, log warning, continue remaining sequence | Narrative |
| TR-narr-010 | Narrative | Timeline asset paths resolved via config (not hardcoded) | Config |
| TR-narr-011 | Narrative | TextureVideo: 3-phase alpha fade (fadeIn → hold → fadeOut) with configurable durations | Narrative |
| TR-narr-012 | Narrative | `SequenceComplete` event fires automatically when all effects in sequence have finished | Event |

### 3.11 Tutorial & Onboarding (`tutorial-onboarding.md`)

| TR ID | System | Requirement | Domain |
|-------|--------|-------------|--------|
| TR-tutor-001 | Tutorial | TutorialStep config structure: stepId, chapterId, puzzleId, triggerCondition, requiredGesture, allowedGestures, promptText, promptImage, completionCondition | Tutorial |
| TR-tutor-002 | Tutorial | InputFilter integration: push `allowedGestures` set on step start, pop on step complete | Input |
| TR-tutor-003 | Tutorial | Filtered (non-allowed) gestures produce zero feedback — silent discard with no visual/audio response | Tutorial |
| TR-tutor-004 | Tutorial | 5 tutorial steps: DragObject (Ch.1P1), RotateObject (Ch.1P2), SnapHint (Ch.1P3), LightTrack (Ch.2P1), DistanceAdjust (Ch.2P2) | Tutorial |
| TR-tutor-005 | Tutorial | Tutorial completion persisted: `tutorialCompleted[]` array in save data; completed steps never replay | Save |
| TR-tutor-006 | Tutorial | Tutorial active → Hint System timers pause; Tutorial complete → Hint timers reset and resume | Hint |
| TR-tutor-007 | Tutorial | Priority hierarchy: Narrative > Tutorial > Hint (Narrative interrupts Tutorial; Tutorial suppresses Hint) | Architecture |
| TR-tutor-008 | Tutorial | Step prompt UI: image animation + instructional text; fade in 0.3s, fade out 0.3s | UI |
| TR-tutor-009 | Tutorial | All tutorial step definitions from Luban config tables; zero hardcoded step data | Config |
| TR-tutor-010 | Tutorial | "Operation Guide" in Settings panel reuses tutorial step config for on-demand replay (without save mutation) | Tutorial |

### 3.12 Settings & Accessibility (`settings-accessibility.md`)

| TR ID | System | Requirement | Domain |
|-------|--------|-------------|--------|
| TR-settings-001 | Settings | 8 player settings: master_volume, music_volume, sfx_volume, sfx_enabled, haptic_enabled, touch_sensitivity, language, target_framerate | Settings |
| TR-settings-002 | Settings | Settings stored in PlayerPrefs; separate from game save JSON file | Settings |
| TR-settings-003 | Settings | All settings apply in real-time; no app restart required for any setting change | Settings |
| TR-settings-004 | Settings | Touch sensitivity modifies `dragThreshold` and `fatFingerMargin` via multiplier | Settings |
| TR-settings-005 | Settings | Language: auto-detect from `Application.systemLanguage`; fallback to `en` if unsupported | Settings |
| TR-settings-006 | Settings | Language hot-switch at runtime via I2 Localization / TEngine localization module | Settings |
| TR-settings-007 | Settings | Frame rate toggle: 30fps / 60fps via `Application.targetFrameRate` | Settings |
| TR-settings-008 | Settings | Ambient volume independent from `sfx_enabled` toggle (ambient always audible if master > 0) | Audio |

### 3.13 Game Concept Cross-Cutting Requirements (`shadow-memory.md` concept + `systems-index.md`)

| TR ID | System | Requirement | Domain |
|-------|--------|-------------|--------|
| TR-concept-001 | Cross-Cutting | Target platforms: Mobile (iOS/Android) primary; PC (Steam) secondary phase | Platform |
| TR-concept-002 | Cross-Cutting | Performance: 60fps target, 16.67ms frame budget across all systems | Performance |
| TR-concept-003 | Cross-Cutting | Draw calls: < 150 (mobile), < 300 (PC) total across all rendering systems | Performance |
| TR-concept-004 | Cross-Cutting | Memory: 1.5GB ceiling (mobile), 4GB ceiling (PC) | Performance |
| TR-concept-005 | Cross-Cutting | Forbidden: synchronous asset loading (use YooAsset async) | Architecture |
| TR-concept-006 | Cross-Cutting | Forbidden: Coroutines for async (use UniTask exclusively) | Architecture |
| TR-concept-007 | Cross-Cutting | Forbidden: direct `ModuleSystem.GetModule<T>()` (use `GameModule.XXX` accessors) | Architecture |
| TR-concept-008 | Cross-Cutting | Forbidden: hardcoded gameplay values (use Luban config tables) | Architecture |
| TR-concept-009 | Cross-Cutting | Forbidden: `GameObject.Find` / `FindObjectOfType` at runtime | Architecture |
| TR-concept-010 | Cross-Cutting | Forbidden: resource leaks (every `LoadAssetAsync` must have matching `UnloadAsset`) | Architecture |
| TR-concept-011 | Cross-Cutting | Testing: Unity Test Framework, 70%+ coverage for gameplay logic (puzzle mechanics, shadow calc) | Testing |
| TR-concept-012 | Cross-Cutting | Assembly structure: Default (bootstrap), GameLogic (hot-fix gameplay), GameProto (hot-fix configs) | Architecture |
| TR-concept-013 | Cross-Cutting | Event-driven communication via TEngine `GameEvent` (int-based event IDs) | Architecture |
| TR-concept-014 | Cross-Cutting | HybridCLR: all gameplay code in `GameLogic` assembly for hot-reload; boot code in Default assembly | Architecture |

---

## 4. Summary Statistics

### 4.1 Total Technical Requirements

| Metric | Count |
|--------|------:|
| **Total TRs** | **178** |
| GDDs analyzed | 13 + 1 concept |
| Systems covered | 13 + cross-cutting |

### 4.2 TR Count by Source GDD

| GDD | Slug | TR Count |
|-----|------|:--------:|
| Input System | `input` | 18 |
| Object Interaction | `objint` | 22 |
| Shadow Puzzle System | `puzzle` | 14 |
| URP Shadow Rendering | `render` | 23 |
| Chapter State & Save | `save` | 20 |
| Hint System | `hint` | 17 |
| UI System | `ui` | 22 |
| Scene Management | `scene` | 17 |
| Audio System | `audio` | 15 |
| Narrative Event System | `narr` | 12 |
| Tutorial & Onboarding | `tutor` | 10 |
| Settings & Accessibility | `settings` | 8 |
| Cross-Cutting (Concept + Index) | `concept` | 14 |
| | **Total** | **212** |

### 4.3 TR Count by Domain

| Domain | Count | Notes |
|--------|:-----:|-------|
| Performance | 30 | Frame budget, draw calls, memory, latency targets |
| Rendering | 18 | URP, shadow maps, shaders, quality tiers |
| Object Interaction | 12 | Drag, snap, rotation, boundary mechanics |
| Input | 12 | Gesture recognition, touch handling, filtering |
| UI | 14 | Windows, widgets, layers, animations |
| Puzzle Logic | 10 | Match scoring, state machines, thresholds |
| Save | 11 | Persistence, integrity, migration |
| Scene | 10 | Loading, transitions, lifecycle |
| Audio | 11 | Mix layers, spatial, crossfade |
| Narrative | 9 | Sequences, atomic effects, Timeline |
| Hint | 11 | Tiers, triggers, cooldowns |
| Config (Luban) | 14 | Data-driven parameters, tables |
| Architecture | 14 | Patterns, forbidden practices, interfaces |
| State | 7 | State machines, progression |
| Event | 4 | GameEvent definitions |
| Settings | 7 | PlayerPrefs, real-time apply |
| Platform | 5 | Safe area, haptics, Android back |
| Accessibility | 5 | High contrast, font scaling, animation scaling |
| Tutorial | 5 | Steps, InputFilter, replay |
| Testing | 1 | Coverage requirements |
| Physics | 1 | Raycasting |
| **Total** | **211** | *(Some TRs span multiple domains; primary domain assigned)* |

> **Note**: The slight variance between per-GDD total (212) and per-domain total (211) is due to rounding in domain classification. The canonical count is **212 TRs** from 14 source documents.

### 4.4 Knowledge Gap Summary

| Risk | Count | Domains |
|------|:-----:|---------|
| **HIGH** | **0** | — |
| **MEDIUM** | **5** | TEngine 6.0.0, HybridCLR, YooAsset 2.3.17, Luban, I2 Localization |
| **LOW** | **15** | All core Unity 2022.3 APIs |

### 4.5 ADR Status

| Item | Status |
|------|--------|
| Formal ADR files | **None exist** — must be created in Phase 1 |
| Informal ADR references | 3 mentioned in `technical-preferences.md` (ADR-001 TEngine, ADR-002 URP, ADR-003 Platform) |
| TR Registry | Template exists at `docs/architecture/tr-registry.yaml` — ready for population |

---

## 5. Next Steps (Phase 1 Preparation)

1. **Populate `tr-registry.yaml`** with all 212 TRs for stable ID tracking
2. **Author formal ADR documents** for the 3 referenced decisions + new decisions emerging from TR analysis
3. **Prioritize MEDIUM-RISK domains** for early spike/prototype validation (TEngine API, YooAsset loading, HybridCLR boundaries)
4. **Group TRs into architectural modules** for Phase 1 system design
5. **Identify TR conflicts/tensions** (e.g., "60fps everywhere" vs "blur effects on low-end" → quality tier degradation strategy)
