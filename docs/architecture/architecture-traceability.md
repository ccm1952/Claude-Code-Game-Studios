// 该文件由Cursor 自动生成

# Architecture Traceability Index — 影子回忆 (Shadow Memory)

> **Version**: 1.0
> **Generated**: 2026-04-22
> **Source**: `phase0-tr-baseline.md` (212 TRs), 18 ADRs (ADR-001 ~ ADR-018), `architecture.md` v1.0
> **Last Review**: `architecture-review-2026-04-22.md`

---

## How to Use This Index

- **"TR → ADR"**: Given a TR ID, find which ADR(s) address it
- **"ADR → TR"**: Given an ADR, find all TRs it covers
- **Coverage**: ✅ = explicitly addressed in ADR decision/implementation, ⚠️ = partial (interface only or deferred to P1/P2), ❌ = gap

---

## 1. TR → ADR Mapping (by System)

### 1.1 Input System (18 TRs) — Primary: ADR-010

| TR ID | Requirement Summary | Coverage | ADR(s) | Notes |
|-------|---------------------|:--------:|--------|-------|
| TR-input-001 | Three-layer input architecture | ✅ | ADR-010 | Core decision |
| TR-input-002 | 5 gesture types with lifecycle | ✅ | ADR-010 | GestureType enum + GesturePhase |
| TR-input-003 | PC input mapping | ✅ | ADR-010 | Deferred to PC phase but architecture supports |
| TR-input-004 | InputBlocker stack-based | ✅ | ADR-010 | IInputService.PushBlocker/PopBlocker |
| TR-input-005 | InputFilter whitelist | ✅ | ADR-010 | IInputService.PushFilter/PopFilter |
| TR-input-006 | Blocker > Filter > Normal priority | ✅ | ADR-010 | Explicit priority chain in decision |
| TR-input-007 | DPI-normalized drag threshold | ✅ | ADR-010 | Formula in implementation guidelines |
| TR-input-008 | Tap timeout unscaledDeltaTime | ✅ | ADR-010 | SingleFingerFSM spec |
| TR-input-009 | Rotation angle delta | ✅ | ADR-010 | DualFingerFSM spec |
| TR-input-010 | Pinch scale with safety guard | ✅ | ADR-010 | minFingerDistance in spec |
| TR-input-011 | Single-finger FSM states | ✅ | ADR-010 | Full state diagram |
| TR-input-012 | Dual-finger FSM mutual exclusion | ✅ | ADR-010 | Explicit lock-until-lift rule |
| TR-input-013 | Max 2 touch points | ✅ | ADR-010 | Slot 0/1 only |
| TR-input-014 | MaxDeltaPerFrame clamp | ✅ | ADR-010 | Performance safeguard |
| TR-input-015 | Pause/resume clears touch state | ✅ | ADR-010 | OnApplicationPause handling |
| TR-input-016 | Gesture processing < 0.5ms | ✅ | ADR-010, ADR-003 | Performance requirement PR-1 |
| TR-input-017 | Thresholds from Luban config | ✅ | ADR-010, ADR-007 | Data-driven via Luban |
| TR-input-018 | Touch sensitivity runtime modify | ✅ | ADR-010 | Multiplier applied to dragThreshold |

### 1.2 URP Shadow Rendering (23 TRs) — Primary: ADR-002

| TR ID | Requirement Summary | Coverage | ADR(s) | Notes |
|-------|---------------------|:--------:|--------|-------|
| TR-render-001 | URP Forward + SRP Batcher | ✅ | ADR-002 | Pipeline selection |
| TR-render-002 | HDR off, MSAA off, SMAA | ✅ | ADR-002 | Quality tier settings |
| TR-render-003 | Shadow map resolution tiers | ✅ | ADR-002 | PC=2048, High=1024, Medium=512 |
| TR-render-004 | Shadow cascades per tier | ✅ | ADR-002 | PC=4, Mobile=2 |
| TR-render-005 | Shadow distance 8m configurable | ⚠️ | ADR-002 | Mentioned but default not specified |
| TR-render-006 | 3 quality tiers + auto-detect | ✅ | ADR-002 | Auto-degradation included |
| TR-render-007 | WallReceiver custom shader | ✅ | ADR-002 | Shader spec in decision |
| TR-render-008 | Shadow contrast ≥ 3:1 | ⚠️ | ADR-002 | Covered by shader params, not explicit |
| TR-render-009 | ShadowSampleCamera + ShadowRT | ✅ | ADR-002 | R8 grayscale RT spec |
| TR-render-010 | AsyncGPUReadback for ShadowRT | ✅ | ADR-002, ADR-012 | CPU sampling pipeline |
| TR-render-011 | Shadow effect interfaces | ✅ | ADR-002, arch.md §6 | SetShadowGlow, FreezeShadow, etc. |
| TR-render-012 | 5 chapter shadow style presets | ✅ | ADR-002 | Per-chapter config |
| TR-render-013 | Max 2 shadow-casting lights | ⚠️ | ADR-002 | Implementation constraint, not explicit |
| TR-render-014 | Shadow caster priority ordering | ⚠️ | ADR-002 | Implementation detail |
| TR-render-015 | Shadow Only rendering mode | ✅ | ADR-002 | Narrative support |
| TR-render-016 | Auto quality degradation | ✅ | ADR-002, ADR-018 | Frame time monitoring |
| TR-render-017 | Draw call budget ≤ 150/40 | ✅ | ADR-002, ADR-003 | Total + shadow allocation |
| TR-render-018 | Shadow memory ≤ 15MB | ⚠️ | ADR-002 | ShadowRT specified, total not |
| TR-render-019 | ShadowRT CPU processing ≤ 1.5ms | ✅ | ADR-002, ADR-012 | AsyncGPUReadback pipeline |
| TR-render-020 | NearMatch glow not affect ShadowRT | ⚠️ | ADR-002 | Shader implementation detail |
| TR-render-021 | High-contrast accessibility mode | ⚠️ | *ADR-020 (P2)* | Explicitly deferred |
| TR-render-022 | Shadow outline accessibility mode | ⚠️ | *ADR-020 (P2)* | Explicitly deferred |
| TR-render-023 | All shadow settings configurable | ✅ | ADR-002 | Quality tier config |

### 1.3 Chapter State & Save System (20 TRs) — Primary: ADR-008

| TR ID | Requirement Summary | Coverage | ADR(s) | Notes |
|-------|---------------------|:--------:|--------|-------|
| TR-save-001 | 5 chapters sequential unlock | ✅ | ADR-008, arch.md §6.4 | IChapterState interface |
| TR-save-002 | Linear puzzle progression | ✅ | ADR-008 | Sequential unlock logic |
| TR-save-003 | Puzzle completion irreversible | ✅ | ADR-008 | Immutable completion flag |
| TR-save-004 | Chapter State is single authority | ✅ | ADR-008, arch.md §4.2 | Module ownership |
| TR-save-005 | IChapterProgress interface decoupling | ✅ | ADR-008, arch.md §6.5 | Interface contract |
| TR-save-006 | Auto-save triggers | ✅ | ADR-008 | 5 trigger conditions |
| TR-save-007 | Save debounce 1s | ✅ | ADR-008 | Except force-save |
| TR-save-008 | UniTask async I/O | ✅ | ADR-008 | Zero main-thread blocking |
| TR-save-009 | Single slot JSON format | ✅ | ADR-008 | Schema defined |
| TR-save-010 | Atomic write (temp → rename) | ✅ | ADR-008 | Write safety |
| TR-save-011 | Backup .backup.json | ✅ | ADR-008 | Rollback support |
| TR-save-012 | CRC32 checksum | ✅ | ADR-008 | Integrity verification |
| TR-save-013 | Version migration chain | ✅ | ADR-008 | Upgrade functions |
| TR-save-014 | Save file size < 10KB | ✅ | ADR-008 | Size constraint |
| TR-save-015 | Save < 50ms, Load < 100ms | ✅ | ADR-008 | Binding: GDD value (100ms) |
| TR-save-016 | Replay mode for completed chapters | ⚠️ | arch.md §6.4 | IsReplayMode exists, no behavioral spec |
| TR-save-017 | Luban config tables | ✅ | ADR-008, ADR-007 | TbChapter, TbPuzzle |
| TR-save-018 | Save init before gameplay systems | ✅ | ADR-008, arch.md §5.6 | Init order step 8 |
| TR-save-019 | Pause/Quit immediate save | ✅ | ADR-008 | Bypass debounce |
| TR-save-020 | Corrupted save fallback chain | ✅ | ADR-008 | Primary → backup → new game |

### 1.4 Scene Management (17 TRs) — Primary: ADR-009

| TR ID | Requirement Summary | Coverage | ADR(s) | Notes |
|-------|---------------------|:--------:|--------|-------|
| TR-scene-001 | Additive scene architecture | ✅ | ADR-009 | Boot + Main + Chapter |
| TR-scene-002 | Max 3 scenes in memory | ✅ | ADR-009 | Memory constraint |
| TR-scene-003 | Async scene loading (UniTask) | ✅ | ADR-009 | Zero sync loads |
| TR-scene-004 | Always LoadSceneMode.Additive | ✅ | ADR-009 | Forbidden: Single mode |
| TR-scene-005 | Transition flow | ✅ | ADR-009 | FadeOut → Unload → GC → Load → FadeIn |
| TR-scene-006 | Transition mutual exclusion | ✅ | ADR-009 | Max queue = 1 |
| TR-scene-007 | YooAsset on-demand download | ✅ | ADR-009, ADR-005 | ResourcePackage integration |
| TR-scene-008 | Chapter scene memory ~1000MB | ⚠️ | ADR-009 | Aligned but measurement unspecified |
| TR-scene-009 | Cached scene load < 1s | ⚠️ | ADR-009 | Total < 3s specified, not load-only |
| TR-scene-010 | Fade at 60fps during loading | ✅ | ADR-009 | Independent of load thread |
| TR-scene-011 | Memory leak detection | ✅ | ADR-009 | 5-cycle load/unload test |
| TR-scene-012 | Error recovery (retry + fallback) | ✅ | ADR-009 | Retry 2× → MainMenu |
| TR-scene-013 | Startup flow | ✅ | ADR-009, arch.md §5.6 | Boot → TEngine → HybridCLR → YooAsset |
| TR-scene-014 | 8 scene transition GameEvent IDs | ✅ | ADR-009, ADR-006 | EventId 1400-1407 |
| TR-scene-015 | Emotional weight fade duration | ✅ | ADR-009 | Narrative-heavy transitions |
| TR-scene-016 | UnloadUnusedAssets + GC.Collect | ✅ | ADR-009 | After every unload |
| TR-scene-017 | SceneHandle reference retention | ✅ | ADR-009, ADR-005 | YooAsset handle tracking |

### 1.5 Object Interaction (22 TRs) — Primary: ADR-013 (P1)

| TR ID | Requirement Summary | Coverage | ADR(s) | Notes |
|-------|---------------------|:--------:|--------|-------|
| TR-objint-001 | Raycast selection on layer | ⚠️ | ADR-013, arch.md §6.2 | Interface defined |
| TR-objint-002 | Selection visual feedback | ⚠️ | ADR-013 | EaseOutBack spec in GDD |
| TR-objint-003 | Fat finger compensation | ⚠️ | ADR-010, ADR-013 | Input provides raw + compensated |
| TR-objint-004 | Drag 1:1 tracking | ⚠️ | ADR-013, arch.md §6.2 | Interface queries defined |
| TR-objint-005 | Grid snap formula | ⚠️ | ADR-013 | Algorithm deferred |
| TR-objint-006 | Snap interpolation EaseOutQuad | ⚠️ | ADR-013 | Animation deferred |
| TR-objint-007 | Rotation snap 15° | ⚠️ | ADR-013 | Algorithm deferred |
| TR-objint-008 | Distance adjustment along light axis | ⚠️ | ADR-013 | Algorithm deferred |
| TR-objint-009 | Light source track (Linear/Arc) | ⚠️ | ADR-013 | Track types deferred |
| TR-objint-010 | Light track snap step 0.1 | ⚠️ | ADR-013 | Config deferred |
| TR-objint-011 | InteractionBounds rebound | ⚠️ | ADR-013 | EaseOutBack spec in GDD |
| TR-objint-012 | Object state machine (6 states) | ✅ | ADR-013 | State machine defined |
| TR-objint-013 | Light state machine | ✅ | ADR-013 | Fixed → TrackIdle → ... |
| TR-objint-014 | ObjectTransformChanged event | ✅ | ADR-006, ADR-013 | EventId 1100 |
| TR-objint-015 | LightPositionChanged event | ✅ | ADR-006, ADR-013 | EventId 1101 |
| TR-objint-016 | PuzzleLock events | ✅ | ADR-006 | EventId 1205-1207 |
| TR-objint-017 | Drag response ≤ 16ms | ⚠️ | ADR-013, ADR-003 | Performance deferred |
| TR-objint-018 | All params from Luban | ⚠️ | ADR-013, ADR-007 | Config integration deferred |
| TR-objint-019 | 10 objects ≥ 55fps on iPhone 13 Mini | ⚠️ | ADR-013, ADR-003 | Performance deferred |
| TR-objint-020 | Update processing < 1ms | ⚠️ | ADR-013, ADR-003 | Performance deferred |
| TR-objint-021 | 200ms selection debounce | ⚠️ | ADR-013 | Implementation detail |
| TR-objint-022 | Haptic feedback cross-platform | ❌ | *ADR-025 (P2)* | Only gap in entire baseline |

### 1.6 UI System (22 TRs) — Primary: ADR-011

| TR ID | Requirement Summary | Coverage | ADR(s) | Notes |
|-------|---------------------|:--------:|--------|-------|
| TR-ui-001 | All UI via TEngine UIModule | ✅ | ADR-011, ADR-001 | UIWindow/UIWidget |
| TR-ui-002 | 5 UI layer levels | ✅ | ADR-011 | 100-interval sorting |
| TR-ui-003 | Popup/Overlay auto InputBlocker | ✅ | ADR-011, ADR-010 | Auto push/pop |
| TR-ui-004 | HUD pass-through to game | ✅ | ADR-011 | Layer design |
| TR-ui-005 | 9 UIWindows defined | ✅ | ADR-011 | Full panel list |
| TR-ui-006 | GameHUD widgets | ✅ | ADR-011 | 5 widgets defined |
| TR-ui-007 | Safe area fitting | ✅ | ADR-011 | SetUISafeFitHelper |
| TR-ui-008 | Popup queue (1 visible) | ✅ | ADR-011 | FIFO queue |
| TR-ui-009 | TimeScale = 0 on PauseMenu | ⚠️ | ADR-011 | Mentioned, not formalized |
| TR-ui-010 | PuzzleCompletePanel auto-close 2.5s | ⚠️ | ADR-011 | Panel behavior deferred |
| TR-ui-011 | Typewriter text effect | ⚠️ | ADR-011 | Widget implementation |
| TR-ui-012 | ChapterTransition 4-phase | ⚠️ | ADR-011, ADR-009 | Transition + scene |
| TR-ui-013 | Gaussian blur fallback | ⚠️ | ADR-011 | Performance tier branching |
| TR-ui-014 | Animation scale accessibility | ⚠️ | *ADR-020 (P2)* | Deferred |
| TR-ui-015 | HintButton opacity ramp | ⚠️ | ADR-011, ADR-015 | Widget behavior |
| TR-ui-016 | UI animations at 60fps | ✅ | ADR-011, ADR-003 | Performance requirement |
| TR-ui-017 | Gaussian blur < 2ms | ⚠️ | ADR-011, ADR-003 | Implementation detail |
| TR-ui-018 | UI prefab memory < 5MB | ✅ | ADR-011, ADR-003 | Binding: GDD value (5MB) |
| TR-ui-019 | Touch target ≥ 44×44pt | ✅ | ADR-011, ADR-003 | Apple HIG |
| TR-ui-020 | Font size presets | ⚠️ | *ADR-020 (P2)* | Deferred |
| TR-ui-021 | All text via localization keys | ⚠️ | *ADR-022 (P2)* | I2 Localization |
| TR-ui-022 | Android back button | ⚠️ | ADR-011 | Platform handling |

### 1.7 Audio System (15 TRs) — Primary: ADR-017 (P1)

| TR ID | Requirement Summary | Coverage | ADR(s) | Notes |
|-------|---------------------|:--------:|--------|-------|
| TR-audio-001 | 3 mix layers (Ambient/SFX/Music) | ✅ | ADR-017, arch.md §6.8 | IAudioService |
| TR-audio-002 | Volume formula (4 multipliers) | ⚠️ | ADR-017 | Formula in GDD, algorithm in ADR |
| TR-audio-003 | SFX variant + pitch randomization | ⚠️ | ADR-017 | Implementation detail |
| TR-audio-004 | 3D spatial audio for SFX | ⚠️ | ADR-017 | AudioSource positioning |
| TR-audio-005 | maxConcurrent per SFX + oldest cull | ⚠️ | ADR-017 | Pool management |
| TR-audio-006 | Music crossfade 1-5s | ✅ | ADR-017 | Crossfade spec |
| TR-audio-007 | Ducking system | ✅ | ADR-017, ADR-006 | Evt_AudioDuckingRequest |
| TR-audio-008 | SFX latency ≤ 1 frame | ⚠️ | ADR-017, ADR-003 | Performance deferred |
| TR-audio-009 | Ambient starts within 2s | ⚠️ | ADR-017 | Startup timing |
| TR-audio-010 | Ambient occasional sounds | ⚠️ | ADR-017 | Randomized interval |
| TR-audio-011 | Audio CPU < 1ms with 10 sources | ⚠️ | ADR-017, ADR-003 | Performance deferred |
| TR-audio-012 | Audio memory < 30MB | ✅ | ADR-017, ADR-003 | Binding: GDD value (30MB) |
| TR-audio-013 | App pause/resume audio state | ⚠️ | ADR-017 | Pause at exact position |
| TR-audio-014 | Music continues during PauseMenu | ⚠️ | ADR-017 | Ignores TimeScale |
| TR-audio-015 | All SFX config from Luban | ✅ | ADR-017, ADR-007 | Config-driven |

### 1.8 Shadow Puzzle System (14 TRs) — Primary: ADR-012, ADR-014 (P1)

| TR ID | Requirement Summary | Coverage | ADR(s) | Notes |
|-------|---------------------|:--------:|--------|-------|
| TR-puzzle-001 | Multi-anchor weighted scoring | ✅ | ADR-012 | Algorithm formalized |
| TR-puzzle-002 | Per-anchor multiplicative score | ✅ | ADR-012 | pos × dir × vis |
| TR-puzzle-003 | NearMatch hysteresis (0.40/0.35) | ✅ | ADR-012, ADR-014 | State machine thresholds |
| TR-puzzle-004 | PerfectMatch threshold 0.85 | ✅ | ADR-012, ADR-014 | Configurable per puzzle |
| TR-puzzle-005 | Puzzle state machine | ✅ | ADR-014 | Full FSM defined |
| TR-puzzle-006 | AbsenceAccepted for Ch.5 | ⚠️ | ADR-014 | State exists, behavior spec partial |
| TR-puzzle-007 | matchScore temporal smoothing | ⚠️ | ADR-012 | 0.2s sliding window noted |
| TR-puzzle-008 | Tutorial grace period | ⚠️ | ADR-014 | Cross-system interaction |
| TR-puzzle-009 | PerfectMatch snap animation | ⚠️ | ADR-014 | EaseOutBack 0.3-0.8s |
| TR-puzzle-010 | Shadow match calc < 2ms | ✅ | ADR-012, ADR-003 | Performance requirement |
| TR-puzzle-011 | 60fps with < 150 draw calls | ✅ | ADR-003 | Cross-cutting budget |
| TR-puzzle-012 | Collectibles don't affect scoring | ⚠️ | ADR-014 | Decoration only rule |
| TR-puzzle-013 | Chapter-difficulty parameter matrix | ⚠️ | ADR-014, ADR-007 | Config table integration |
| TR-puzzle-014 | Per-puzzle config via Luban | ⚠️ | ADR-014, ADR-007 | TbPuzzle integration |

### 1.9 Hint System (17 TRs) — Primary: ADR-015 (P1)

| TR ID | Requirement Summary | Coverage | ADR(s) | Notes |
|-------|---------------------|:--------:|--------|-------|
| TR-hint-001 | 3-tier progressive hints | ✅ | ADR-015 | L1/L2/L3 defined |
| TR-hint-002 | Trigger score formula | ✅ | ADR-015 | Formula formalized |
| TR-hint-003 | L1/L2 idle thresholds | ✅ | ADR-015 | Configurable per chapter |
| TR-hint-004 | Cooldown with matchScore modifier | ✅ | ADR-015 | cooldownModifier formula |
| TR-hint-005 | L3 max 3 uses per puzzle | ✅ | ADR-015 | Non-renewable |
| TR-hint-006 | Target selection argmin(anchorScore) | ⚠️ | ADR-015 | Algorithm noted, tie-breaking spec |
| TR-hint-007 | Read-only query to puzzle system | ⚠️ | ADR-015, arch.md §6.6 | IHintService interface |
| TR-hint-008 | 1s polling interval | ⚠️ | ADR-015 | Performance constraint |
| TR-hint-009 | Pause during tutorial | ⚠️ | ADR-015 | Cross-system interaction |
| TR-hint-010 | hintDelayOverride per chapter | ⚠️ | ADR-015, ADR-007 | Luban config |
| TR-hint-011 | L1 zero additional draw calls | ⚠️ | ADR-015, ADR-003 | Material animation only |
| TR-hint-012 | L2 rendering < 0.5ms | ⚠️ | ADR-015, ADR-003 | Performance deferred |
| TR-hint-013 | Total update < 0.5ms | ⚠️ | ADR-015, ADR-003 | Performance deferred |
| TR-hint-014 | Absence puzzle hint text | ⚠️ | ADR-015 | Ch.5 special case |
| TR-hint-015 | Timer deltaTime capped 1.0s | ⚠️ | ADR-015 | Frame spike protection |
| TR-hint-016 | App pause: timers pause, 5min reset | ⚠️ | ADR-015 | Background handling |
| TR-hint-017 | Stagnation detection (0.30-0.40) | ⚠️ | ADR-015 | Additional hint score |

### 1.10 Narrative Event System (12 TRs) — Primary: ADR-016 (P1)

| TR ID | Requirement Summary | Coverage | ADR(s) | Notes |
|-------|---------------------|:--------:|--------|-------|
| TR-narr-001 | 11 atomic effect types | ✅ | ADR-016 | Full type list |
| TR-narr-002 | Time-sorted parallel effects | ⚠️ | ADR-016 | Sequencer architecture |
| TR-narr-003 | Config-table driven sequences | ⚠️ | ADR-016, ADR-007 | Luban integration |
| TR-narr-004 | PuzzleLockAll + InputBlocker | ✅ | ADR-016, ADR-006, ADR-010 | Event cascade |
| TR-narr-005 | Absence puzzle Ch.5 sequence | ⚠️ | ADR-016 | Special parameters |
| TR-narr-006 | Chapter transition Timeline | ⚠️ | ADR-016, ADR-009 | 21:9 letterbox |
| TR-narr-007 | Chapter-final merged sequence | ⚠️ | ADR-016 | PerfectMatch + transition |
| TR-narr-008 | Queue max 3 pending | ⚠️ | ADR-016 | Drop oldest policy |
| TR-narr-009 | Resource load failure resilience | ⚠️ | ADR-016 | Skip + log + continue |
| TR-narr-010 | Timeline paths via config | ⚠️ | ADR-016, ADR-007 | No hardcoded paths |
| TR-narr-011 | TextureVideo 3-phase alpha | ⚠️ | ADR-016 | fadeIn → hold → fadeOut |
| TR-narr-012 | SequenceComplete event | ✅ | ADR-006, ADR-016 | EventId 1500 |

### 1.11 Tutorial & Onboarding (10 TRs) — Primary: *ADR-019 (P2)*

| TR ID | Requirement Summary | Coverage | ADR(s) | Notes |
|-------|---------------------|:--------:|--------|-------|
| TR-tutor-001 | TutorialStep config structure | ⚠️ | arch.md §4.4 | Module listed |
| TR-tutor-002 | InputFilter integration | ✅ | ADR-010 | PushFilter/PopFilter |
| TR-tutor-003 | Filtered gestures silent discard | ✅ | ADR-010 | No feedback on filtered |
| TR-tutor-004 | 5 tutorial steps | ⚠️ | *ADR-019 (P2)* | Step definitions deferred |
| TR-tutor-005 | tutorialCompleted in save data | ✅ | ADR-008 | Save schema field |
| TR-tutor-006 | Tutorial pauses hint timers | ✅ | ADR-015 | Cross-system rule |
| TR-tutor-007 | Priority: Narrative > Tutorial > Hint | ✅ | ADR-006, arch.md | Event cascade ordering |
| TR-tutor-008 | Step prompt UI | ⚠️ | *ADR-019 (P2)* | UI behavior deferred |
| TR-tutor-009 | Steps from Luban config | ⚠️ | ADR-007 | Config pattern applies |
| TR-tutor-010 | Operation Guide replay in Settings | ⚠️ | *ADR-019 (P2)* | Feature deferred |

### 1.12 Settings & Accessibility (8 TRs) — Primary: *ADR-020/022 (P2)*

| TR ID | Requirement Summary | Coverage | ADR(s) | Notes |
|-------|---------------------|:--------:|--------|-------|
| TR-settings-001 | 8 player settings | ✅ | arch.md §4.4 | Settings list defined |
| TR-settings-002 | PlayerPrefs storage | ✅ | ADR-008 | Separated from save JSON |
| TR-settings-003 | Real-time apply, no restart | ✅ | ADR-006 | Evt_SettingChanged (2000) |
| TR-settings-004 | Touch sensitivity multiplier | ✅ | ADR-010 | dragThreshold modifier |
| TR-settings-005 | Language auto-detect + fallback | ⚠️ | *ADR-022 (P2)* | I2 Localization deferred |
| TR-settings-006 | Language hot-switch runtime | ⚠️ | *ADR-022 (P2)* | I2 Localization deferred |
| TR-settings-007 | Frame rate toggle 30/60fps | ✅ | ADR-003 | Application.targetFrameRate |
| TR-settings-008 | Ambient volume independent of sfx_enabled | ⚠️ | ADR-017 | Audio mix rule |

### 1.13 Cross-Cutting / Concept (14 TRs) — Multiple ADRs

| TR ID | Requirement Summary | Coverage | ADR(s) | Notes |
|-------|---------------------|:--------:|--------|-------|
| TR-concept-001 | Mobile primary, PC secondary | ✅ | ADR-003 | Core platform decision |
| TR-concept-002 | 60fps, 16.67ms frame budget | ✅ | ADR-003 | Performance target |
| TR-concept-003 | Draw calls < 150 mobile / < 300 PC | ✅ | ADR-003 | Rendering budget |
| TR-concept-004 | Memory 1.5GB mobile / 4GB PC | ✅ | ADR-003 | Memory ceiling |
| TR-concept-005 | Forbidden: sync asset loading | ✅ | ADR-005 | YooAsset async only |
| TR-concept-006 | Forbidden: Coroutines | ✅ | ADR-001 | UniTask mandated |
| TR-concept-007 | Forbidden: direct GetModule | ✅ | ADR-001 | GameModule.XXX accessors |
| TR-concept-008 | Forbidden: hardcoded values | ✅ | ADR-007 | Luban config required |
| TR-concept-009 | Forbidden: GameObject.Find | ✅ | ADR-001 | Explicit ban |
| TR-concept-010 | Forbidden: resource leaks | ✅ | ADR-005 | Load/Unload pairing |
| TR-concept-011 | Unity Test Framework, 70% coverage | ⚠️ | arch.md, tech-prefs | No ADR for test architecture |
| TR-concept-012 | Assembly structure (3 assemblies) | ✅ | ADR-004 | Default/GameLogic/GameProto |
| TR-concept-013 | GameEvent int-based communication | ✅ | ADR-006 | Protocol defined |
| TR-concept-014 | HybridCLR: GameLogic is hot-reload | ✅ | ADR-004 | Assembly boundary |

---

## 2. ADR → TR Reverse Mapping

| ADR | Title | Priority | TRs Addressed |
|-----|-------|:--------:|---------------|
| ADR-001 | TEngine 6.0 Framework | P0 | TR-concept-006, TR-concept-007, TR-concept-009, TR-ui-001 |
| ADR-002 | URP Shadow Rendering | P0 | TR-render-001 ~ 023 (23 TRs) |
| ADR-003 | Mobile-First Platform | P0 | TR-concept-001 ~ 004, TR-input-016, TR-render-017, TR-puzzle-011, TR-settings-007, + performance budgets across systems |
| ADR-004 | HybridCLR Assembly | P0 | TR-concept-012, TR-concept-014 |
| ADR-005 | YooAsset Lifecycle | P0 | TR-concept-005, TR-concept-010, TR-scene-007, TR-scene-017 |
| ADR-006 | GameEvent Protocol — *superseded by ADR-027* | P0 | TR-concept-013, TR-objint-014 ~ 016, TR-scene-014, TR-narr-012, TR-settings-003 |
| ADR-027 | GameEvent Interface Protocol (supersedes ADR-006 §1/§2) | P0 | TR-concept-013（替换实现）、TR-input-gesture-dispatch、TR-settings-003、TR-shadowrt-readback、TR-objint-016 |
| ADR-007 | Luban Config Access | P0 | TR-concept-008, TR-input-017, TR-objint-018, TR-save-017, TR-audio-015, TR-narr-003/010, TR-tutor-009, TR-hint-010, TR-puzzle-013/014 |
| ADR-008 | Save System | P0 | TR-save-001 ~ 020 (20 TRs), TR-tutor-005, TR-settings-002 |
| ADR-009 | Scene Lifecycle | P0 | TR-scene-001 ~ 017 (17 TRs) |
| ADR-010 | Input Abstraction | P0 | TR-input-001 ~ 018 (18 TRs), TR-tutor-002/003, TR-settings-004, TR-objint-003 |
| ADR-011 | UIWindow Management | P0 | TR-ui-001 ~ 022 (partial, 11 ✅ + 11 ⚠️) |
| ADR-012 | Shadow Match Algorithm | P1 | TR-puzzle-001 ~ 004, TR-puzzle-007, TR-puzzle-010, TR-render-010/019 |
| ADR-013 | Object Interaction | P1 | TR-objint-001 ~ 021 (21 TRs, mostly ⚠️) |
| ADR-014 | Puzzle State Machine | P1 | TR-puzzle-003 ~ 009, TR-puzzle-012 ~ 014 |
| ADR-015 | Hint System | P1 | TR-hint-001 ~ 017 (17 TRs), TR-tutor-006 |
| ADR-016 | Narrative Sequence Engine | P1 | TR-narr-001 ~ 012 (12 TRs) |
| ADR-017 | Audio Mix Architecture | P1 | TR-audio-001 ~ 015 (15 TRs), TR-settings-008 |
| ADR-018 | Performance Monitoring | P1 | TR-render-016, + cross-system degradation triggers |

---

## 3. Coverage Summary

| Layer | TRs | ✅ Covered | ⚠️ Partial | ❌ Gap |
|-------|:---:|:----------:|:----------:|:-----:|
| Foundation | 78 | 66 | 12 | 0 |
| Core | 59 | 21 | 37 | 1 |
| Feature | 43 | 15 | 28 | 0 |
| Presentation | 18 | 9 | 9 | 0 |
| Cross-Cutting | 14 | 13 | 1 | 0 |
| **TOTAL** | **212** | **124 (58.5%)** | **87 (41.0%)** | **1 (0.5%)** |

**Single Gap**: TR-objint-022 (Haptic feedback cross-platform) — deferred to ADR-025 (P2)

---

## 4. Unwritten ADR Coverage (Expected Gaps)

| ADR | Priority | Systems | TRs Waiting |
|-----|:--------:|---------|:-----------:|
| ADR-019 | P2 | Tutorial / Onboarding | 4 |
| ADR-020 | P2 | Accessibility (UI) | 4 |
| ADR-021 | P2 | *(reserved)* | — |
| ADR-022 | P2 | I2 Localization | 3 |
| ADR-023 | P2 | *(reserved)* | — |
| ADR-024 | P2 | Analytics Telemetry | 0 (no TRs yet) |
| ADR-025 | P2 | Haptic Feedback | 1 (TR-objint-022) |
| ADR-026 | P2 | *(reserved)* | — |

---

## 5. Known Conflicts (from Architecture Review)

| ID | Severity | Description | Resolution Status |
|----|:--------:|-------------|:-----------------:|
| CONFLICT-001 | CRITICAL | Event ID: ADR-006 (1000-1004) vs ADR-010 (5001-5005) | **RESOLVED** — ADR-010 updated to use ADR-006 IDs |
| CONFLICT-002 | MODERATE | UI Event IDs: ADR-011 (6001-6004) outside ADR-006 range | **RESOLVED** — UI System range 2100-2199 allocated in ADR-006 |
| CONFLICT-003 | MODERATE | Layer naming: ADR-006 uses non-canonical names | **RESOLVED** — Updated to Foundation/Core/Feature/Presentation |
| CONFLICT-004 | MINOR | Audio memory: ADR-003 (80MB) vs GDD (30MB) | Binding: 30MB (GDD) |
| CONFLICT-005 | MINOR | UI prefab memory: ADR-011 (5-15MB) vs GDD (5MB) | Binding: 5MB (GDD) |
| CONFLICT-006 | MINOR | Save load time: ADR-008 (200ms) vs GDD (100ms) | Binding: 100ms (GDD) |
| CONFLICT-007 | MINOR | Shadow draw calls: scope clarification needed | ≤ 40 total, < 20 ShadowSampleCamera |

---

## Appendix A — ADR-006 EventId → ADR-027 Interface Mapping

> **用途**：在 story 文件、GDD、老代码 comment 中查到 `Evt_Xxx` 命名时，按下表替换为新接口方法。**禁止**提交包含 `public const int Evt_Xxx` 的新代码。
>
> **状态**：ADR-006 §1 的 ID 分配已被 ADR-027 取代；ID 由 Source Generator 按 `"{InterfaceName}_Event.{MethodName}"` 编译期哈希生成，不再手动分配。
>
> **映射表之外的名称**：请联系 Architecture Owner 补充；不要猜测映射。

### A.1 Input System（原 1000–1099）

| 原 EventId（ADR-006） | ADR-027 接口方法 | 分组 | 状态 |
|---------------------|-----------------|------|------|
| `Evt_Gesture_Tap` (1000) | `IGestureEvent.OnTap(GestureData data)` | GroupLogic | 已实现 |
| `Evt_Gesture_Drag` (1001) | `IGestureEvent.OnDrag(GestureData data)` | GroupLogic | 已实现 |
| `Evt_Gesture_Rotate` (1002) | `IGestureEvent.OnRotate(GestureData data)` | GroupLogic | 已实现 |
| `Evt_Gesture_Pinch` (1003) | `IGestureEvent.OnPinch(GestureData data)` | GroupLogic | 已实现 |
| `Evt_Gesture_LightDrag` (1004) | `IGestureEvent.OnLightDrag(GestureData data)` | GroupLogic | 已实现 |

### A.2 Object Interaction（原 1100–1199）

| 原 EventId | ADR-027 接口方法 | 分组 | 状态 |
|-----------|-----------------|------|------|
| `Evt_ObjectTransformChanged` (1100) | `IObjectInteractionEvent.OnObjectTransformChanged(...)` | GroupLogic | 待 Object Interaction sprint 落地 |
| `Evt_LightPositionChanged` (1101) | `IObjectInteractionEvent.OnLightPositionChanged(...)` | GroupLogic | 待落地 |
| `Evt_ObjectSelected` (1102) | `IObjectInteractionEvent.OnObjectSelected(...)` | GroupLogic | 待落地 |
| `Evt_ObjectDeselected` (1103) | `IObjectInteractionEvent.OnObjectDeselected(...)` | GroupLogic | 待落地 |

### A.3 Shadow Puzzle（原 1200–1299）

| 原 EventId | ADR-027 接口方法 | 分组 | 状态 |
|-----------|-----------------|------|------|
| `Evt_MatchScoreChanged` (1200) | `IPuzzleEvent.OnMatchScoreChanged(MatchScoreChangedPayload)` | GroupLogic | 待落地 |
| `Evt_NearMatchEnter` (1201) | `IPuzzleEvent.OnNearMatchEnter()` | GroupLogic | 待落地 |
| `Evt_NearMatchExit` (1202) | `IPuzzleEvent.OnNearMatchExit()` | GroupLogic | 待落地 |
| `Evt_PerfectMatch` (1203) | `IPuzzleEvent.OnPerfectMatch()` | GroupLogic | 待落地 |
| `Evt_AbsenceAccepted` (1204) | `IPuzzleEvent.OnAbsenceAccepted()` | GroupLogic | 待落地 |
| `Evt_PuzzleLockAll` (1205) | `IPuzzleLockEvent.OnPuzzleLockAll(PuzzleLockPayload)` | GroupLogic | 待落地（ADR-027 Decision §4） |
| `Evt_PuzzleUnlock` (1206) | `IPuzzleLockEvent.OnPuzzleUnlock(PuzzleLockPayload)` | GroupLogic | 待落地（ADR-027 Decision §4） |
| `Evt_PuzzleSnapToTarget` (1207) | `IPuzzleEvent.OnPuzzleSnapToTarget(...)` | GroupLogic | 待落地 |
| `Evt_PuzzleComplete` (1208) | `IPuzzleEvent.OnPuzzleComplete()` | GroupLogic | 待落地 |

### A.4 Chapter State（原 1300–1399）

| 原 EventId | ADR-027 接口方法 | 分组 | 状态 |
|-----------|-----------------|------|------|
| `Evt_PuzzleStateChanged` (1300) | `IChapterEvent.OnPuzzleStateChanged(...)` | GroupLogic | 待落地 |
| `Evt_ChapterComplete` (1301) | `IChapterEvent.OnChapterComplete(int chapterId)` | GroupLogic | 待落地 |
| `Evt_RequestSceneChange` (1302) | `ISceneEvent.OnRequestSceneChange(...)` | GroupLogic | 待落地 |

### A.5 Scene Management（原 1400–1407）

所有 8 个 scene transition 事件统一放入 `ISceneEvent`（GroupLogic）：

| 原 EventId | ADR-027 接口方法 | 状态 |
|-----------|-----------------|------|
| `Evt_SceneTransitionBegin` (1400) | `ISceneEvent.OnSceneTransitionBegin(string sceneName)` | 待落地 |
| `Evt_SceneUnloadBegin` (1401) | `ISceneEvent.OnSceneUnloadBegin(string sceneName)` | 待落地（监听者退订点） |
| `Evt_SceneLoadProgress` (1402) | `ISceneEvent.OnSceneLoadProgress(string sceneName, float progress)` | 待落地 |
| `Evt_SceneDownloadProgress` (1403) | `ISceneEvent.OnSceneDownloadProgress(string sceneName, float progress)` | 待落地 |
| `Evt_SceneLoadComplete` (1404) | `ISceneEvent.OnSceneLoadComplete(string sceneName)` | 待落地 |
| `Evt_SceneReady` (1405) | `ISceneEvent.OnSceneReady(string sceneName)` | 待落地 |
| `Evt_SceneTransitionEnd` (1406) | `ISceneEvent.OnSceneTransitionEnd(string sceneName)` | 待落地 |
| `Evt_SceneLoadFailed` (1407) | `ISceneEvent.OnSceneLoadFailed(string sceneName, string reason)` | 待落地 |

### A.6 Narrative（原 1500–1599）

| 原 EventId | ADR-027 接口方法 | 状态 |
|-----------|-----------------|------|
| `Evt_SequenceComplete` (1500) | `INarrativeEvent.OnSequenceComplete(int sequenceId)` | 待落地 |
| `Evt_LoadNextChapter` (1501) | `INarrativeEvent.OnLoadNextChapter(int chapterId)` | 待落地 |

### A.7 Audio（原 1600–1699）

| 原 EventId | ADR-027 接口方法 | 状态 |
|-----------|-----------------|------|
| `Evt_AudioDuckingRequest` (1600) | `IAudioEvent.OnAudioDuckingRequest(float duckRatio, float fadeDuration)` | 待落地 |
| `Evt_AudioDuckingRelease` (1601) | `IAudioEvent.OnAudioDuckingRelease(float fadeDuration)` | 待落地 |
| `Evt_PlaySFXRequest` (1602) | `IAudioEvent.OnPlaySFXRequest(string sfxId)` | 待落地 |
| `Evt_PlayMusicRequest` (1603) | `IAudioEvent.OnPlayMusicRequest(string musicId)` | 待落地 |

### A.8 Hint（原 1700–1799）

| 原 EventId | ADR-027 接口方法 | 状态 |
|-----------|-----------------|------|
| `Evt_HintAvailable` (1700) | `IHintEvent.OnHintAvailable(int level)` | 待落地 |
| `Evt_HintDismissed` (1701) | `IHintEvent.OnHintDismissed()` | 待落地 |

### A.9 Tutorial（原 1800–1899）

| 原 EventId | ADR-027 接口方法 | 状态 |
|-----------|-----------------|------|
| `Evt_TutorialStepStarted` (1800) | `ITutorialEvent.OnTutorialStepStarted(int stepId)` | 待落地（同属 ADR-019 Tutorial ADR 范围） |
| `Evt_TutorialStepCompleted` (1801) | `ITutorialEvent.OnTutorialStepCompleted(int stepId)` | 待落地 |

### A.10 Save（原 1900–1999）

| 原 EventId | ADR-027 接口方法 | 状态 |
|-----------|-----------------|------|
| `Evt_SaveComplete` (1900) | `ISaveEvent.OnSaveComplete()` | 待落地 |

### A.11 Settings（原 2000–2099）

| 原 EventId | ADR-027 接口方法 | 状态 |
|-----------|-----------------|------|
| `Evt_SettingChanged` (2000) | `ISettingsEvent.OnSettingChanged(string key, string value)` | 待落地 |
| `Evt_Settings_TouchSensitivityChanged` (2050) | `ISettingsEvent.OnTouchSensitivityChanged(float multiplier)` | 已实现 |

### A.12 UI System（原 2100–2199）

UI 域事件使用 `GroupUI` 分组，一个面板一个接口（例 `ILoginUI`，已有先例）：

| 原 EventId | ADR-027 接口方法 | 分组 | 状态 |
|-----------|-----------------|------|------|
| `Evt_PanelOpened` (2100) | `I{PanelName}UI.Show{PanelName}UI()` | GroupUI | 按面板逐个落地 |
| `Evt_PanelClosed` (2101) | `I{PanelName}UI.Close{PanelName}UI()` | GroupUI | 按面板逐个落地 |
| `Evt_PopupQueued` (2102) | `IUIWindowEvent.OnPopupQueued(string panelName)` | GroupUI | 待落地 |
| `Evt_PopupDequeued` (2103) | `IUIWindowEvent.OnPopupDequeued(string panelName)` | GroupUI | 待落地 |

### A.13 Rendering（原 1100 / 非标 — 挤占 Object Interaction range）

| 原 EventId | ADR-027 接口方法 | 状态 |
|-----------|-----------------|------|
| `Evt_ShadowRT_Updated` (1100) | `IShadowRTEvent.OnShadowRTUpdated(ShadowRTData data)` | 已实现 |

> **注**：ADR-006 原范围表中 1100 同时被 Object Interaction 与 Rendering (`Evt_ShadowRT_Updated`) 占用，属于协议冲突之一。ADR-027 切换到接口模型后不再有范围概念，两者独立命名不冲突。

---

*End of Architecture Traceability Index*
