// 该文件由Cursor 自动生成

# ADR-003: Mobile-First Platform Strategy

## Status

Proposed

## Date

2026-04-22

## Last Verified

2026-04-22

## Decision Makers

Technical Director, Creative Director (platform vision), Lead Programmer

## Summary

The project needs a unified platform strategy to set performance budgets, input
method priorities, UI scaling rules, and asset pipeline decisions across all
systems. **Mobile (iOS/Android) is the primary target platform**; all design and
performance baselines derive from mobile hardware constraints. PC/Steam is a
future adaptation phase, not a simultaneous development target.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.62f2 LTS |
| **Domain** | Core / Input / Rendering / UI |
| **Knowledge Risk** | LOW — Unity 2022.3 LTS is well within training data |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `.claude/docs/technical-preferences.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Verify ASTC texture support on target Android minimum spec (Adreno 613); verify `Screen.safeArea` behavior on notch devices |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None — this is a foundational platform decision |
| **Enables** | All system ADRs (they reference mobile performance budgets defined here) |
| **Blocks** | None directly — this is an informational foundation that all other ADRs consume |
| **Ordering Note** | Should be accepted before any system ADR that specifies performance budgets or input assumptions |

## Context

### Problem Statement

影子回忆 (Shadow Memory) is a narrative puzzle game built around physical object
manipulation and shadow projection. Without a clear platform strategy, each
system ADR will make ad-hoc assumptions about performance budgets, input methods,
and asset formats — leading to inconsistent constraints, wasted optimization
effort, and a codebase that is neither good on mobile nor good on PC.

The core gameplay — dragging, rotating, and positioning physical objects to cast
shadows — is inherently a touch-first interaction. This makes the platform
priority decision non-negotiable: the input method that best serves the core
fantasy must be the design baseline.

### Current State

No platform strategy document exists. Performance budgets are informally
referenced in `technical-preferences.md` but lack formal rationale, tier
definitions, or validation targets. GDDs reference "mobile-first" in passing
but do not define what that means for each system's implementation.

### Constraints

- **Hardware floor**: Must run at 60fps on mid-range mobile devices (iPhone 12 / Adreno 613 class)
- **Team size**: Indie team — cannot maintain two parallel platform codepaths during initial development
- **Framework**: TEngine 6.0.0 + YooAsset 2.3.17 already committed (ADR-001); asset pipeline must align
- **Rendering**: URP committed (ADR-002); mobile shader budget is the binding constraint
- **App store requirements**: iOS App Store and Google Play submission requirements (safe area, permissions, etc.)

### Requirements

- Defined performance budgets (frame time, draw calls, memory) with mobile as baseline
- Tiered quality settings (Low / Medium / High) mapped to real hardware targets
- Input strategy: touch primary, keyboard/mouse secondary (future)
- UI scaling rules: safe area, minimum touch targets, orientation support
- Asset pipeline rules: compression formats, resolution limits, streaming strategy
- Clear separation point for PC adaptation phase

## Decision

**Mobile is the primary target platform.** All design, performance, and
interaction decisions use mobile hardware as the constraining baseline.
PC/Steam is a future adaptation phase that relaxes constraints upward —
it never introduces constraints that conflict with the mobile baseline.

### Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Platform Abstraction Layer                     │
│                                                                   │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────────────┐ │
│  │ Input Adapter │   │ Quality Tier │   │ Asset Format Resolver│ │
│  │  (Touch/K+M)  │   │  (Low/Med/Hi)│   │  (ASTC/ETC2/DXT)    │ │
│  └──────┬───────┘   └──────┬───────┘   └──────────┬───────────┘ │
│         │                  │                       │              │
│         ▼                  ▼                       ▼              │
│  ┌──────────────────────────────────────────────────────────────┐│
│  │              Gameplay Systems (Platform-Agnostic)             ││
│  │   Shadow Puzzle · Object Interaction · Narrative · UI        ││
│  └──────────────────────────────────────────────────────────────┘│
│         │                  │                       │              │
│         ▼                  ▼                       ▼              │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────────────┐ │
│  │ iOS / Android │   │   PC / Steam  │   │  Editor (Dev Only)  │ │
│  │  (Phase 1)    │   │  (Phase 2)    │   │                     │ │
│  └──────────────┘   └──────────────┘   └──────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### Performance Budgets

#### Mobile Baseline (Phase 1 — binding constraint)

| Metric | Budget | Notes |
|--------|--------|-------|
| **Frame budget** | 16.67ms (60fps) | Non-negotiable; 30fps option exposed in settings |
| **Gameplay systems per frame** | < 5.5ms | Leaves ~11ms for rendering + engine overhead |
| **Draw calls** | < 150 | URP SRP Batcher enabled; static batching for scene geometry |
| **Triangles per frame** | < 200K | Target for mid-range mobile GPU |
| **Memory ceiling** | 1.5 GB | Total app memory including OS overhead |
| **Texture budget** | Max 1024×1024 per asset | ETC2 (Android) / ASTC (iOS) compression mandatory |
| **Audio memory** | < 80 MB | Compressed Vorbis/AAC; streaming for music tracks |
| **Scene load time** | < 3s | Async loading via YooAsset; loading screen with progress |
| **APK/IPA size** | < 200 MB initial | YooAsset hot-update for additional content |

#### PC Adaptation (Phase 2 — relaxed upward)

| Metric | Budget | Notes |
|--------|--------|-------|
| **Frame budget** | 16.67ms (60fps) | Same target; headroom for higher quality |
| **Draw calls** | < 300 | More generous but still budgeted |
| **Memory ceiling** | 4 GB | Higher-res textures, uncompressed audio option |
| **Texture budget** | Up to 2048×2048 | DXT5/BC7 compression; higher mip levels |
| **Shadow quality** | Higher resolution shadow maps as default | Mobile Medium ≈ PC Low |

#### Quality Tier Definitions

| Tier | Target Hardware | Shadow Resolution | Texture Res | Post-Processing | Draw Calls |
|------|----------------|-------------------|-------------|-----------------|------------|
| **Low** | Adreno 613 / Mali-G52 | 512×512 | 512 max | Off | < 100 |
| **Medium** | A14 (iPhone 12) / SD 870 | 1024×1024 | 1024 max | Bloom only | < 150 |
| **High** | A15+ / SD 8 Gen 1+ / PC | 2048×2048 | 2048 max | Bloom + Vignette + Color Grading | < 300 |

### Input Strategy

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| **Primary input** | Touch — `UnityEngine.Input.GetTouch()` | Core gameplay is drag/rotate physical objects; touch is the natural modality |
| **Input System package** | NOT adopted | Unity's new Input System adds ~200KB+ overhead, abstraction layers, and action map complexity; legacy Input API is sufficient for our gesture set (Tap/Drag/Rotate/Pinch) |
| **Gesture recognition** | Custom three-layer architecture (see `input-system.md` GDD) | Raw Input → Gesture Recognition → Event Dispatch; all resolved within single frame |
| **PC input (Phase 2)** | Adapter pattern overlay | `IInputAdapter` interface; `TouchInputAdapter` (Phase 1) and `MouseKeyboardInputAdapter` (Phase 2) implement the same gesture contract |
| **Editor/Dev input** | Mouse simulates touch | Allows PC-based development without a device; mouse left-click = tap, drag = drag, scroll = pinch |

### UI Strategy

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| **Safe area** | TEngine `SetUISafeFitHelper` on all root canvases | Handles notch/cutout/home indicator across iOS and Android |
| **Orientation** | Landscape primary; portrait not required | Shadow projection gameplay requires horizontal screen space |
| **Min touch target** | 44×44dp (iOS HIG baseline) | Apple HIG specifies 44pt; Material Design specifies 48dp; we use 44dp as minimum, prefer 48dp |
| **UI scaling** | `CanvasScaler` — Scale With Screen Size, reference 1920×1080, match width 0.5 | Balances phone vs tablet aspect ratios |
| **Text sizing** | Minimum 14sp body, 12sp captions | Readable on 5.5" screens at arm's length |

### Asset Pipeline

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| **Asset management** | YooAsset 2.3.17 | Hot-update capable; aligns with ADR-001 (TEngine framework) |
| **Texture compression (iOS)** | ASTC 4×4 (high quality) / ASTC 6×6 (backgrounds) | Best quality-per-bit on Apple GPU; all modern iOS devices support ASTC |
| **Texture compression (Android)** | ETC2 (baseline); ASTC where GPU supports it | ETC2 is mandatory OpenGL ES 3.0 format; ASTC is preferred when available |
| **Texture compression (PC)** | DXT5 / BC7 (Phase 2) | Standard desktop formats; higher fidelity |
| **Max texture resolution** | 1024×1024 (mobile); 2048×2048 (PC) | Keeps VRAM usage under budget on mobile |
| **Audio format** | Vorbis (Android/PC) / AAC (iOS) | Compressed; streaming for music >30s |
| **Audio sample rate** | 44.1kHz for music; 22.05kHz for SFX | Saves memory; SFX don't need full bandwidth |
| **Sprite atlasing** | SpriteAtlas per UI screen; max 2048×2048 atlas | Reduces draw calls for UI elements |

### Key Interfaces

```csharp
// Input adapter contract — enables platform-specific input without
// changing gameplay code
public interface IInputAdapter
{
    bool TryGetTap(out Vector2 screenPos);
    bool TryGetDragDelta(out Vector2 delta, out Vector2 currentPos);
    bool TryGetRotation(out float angleDelta);
    bool TryGetPinch(out float scaleDelta);
    void SetSensitivity(float multiplier);
}

// Quality tier contract — systems query this to select LOD/budget
public interface IQualityTierProvider
{
    QualityTier CurrentTier { get; }  // Low, Medium, High
    int MaxDrawCalls { get; }
    int MaxShadowResolution { get; }
    int MaxTextureResolution { get; }
    bool PostProcessingEnabled { get; }
}

public enum QualityTier { Low, Medium, High }
```

### Implementation Guidelines

1. **All gameplay systems must respect mobile budgets by default.** If a system
   needs more than its allocated frame time, it must implement LOD or quality
   scaling — not assume PC headroom.

2. **Input abstraction from day one.** Even though Phase 1 only ships touch,
   the `IInputAdapter` interface must exist so gameplay code never directly
   calls `Input.GetTouch()`. This makes the PC adapter a drop-in addition.

3. **Quality tier selection is automatic.** On first launch, the game profiles
   the device (GPU model, RAM, thermal state) and selects Low/Medium/High.
   Players can override in settings. `IQualityTierProvider` is injected into
   systems that need to scale their budgets.

4. **Asset variants via YooAsset tags.** Texture assets are tagged `mobile-sd`,
   `mobile-hd`, `pc-hd`. YooAsset's variant system loads the correct set based
   on the active quality tier. No runtime re-compression.

5. **Test on target hardware, not just Editor.** Sprint 0 must establish device
   testing on iPhone 12 (Medium tier baseline) and a low-end Android device
   (Adreno 613 class). Editor profiling is necessary but not sufficient.

## Alternatives Considered

### Alternative 1: PC-First, Mobile Port

- **Description**: Design for keyboard/mouse on PC; port to touch later
- **Pros**: Easier development iteration (test in Editor); no mobile performance constraints during prototyping
- **Cons**: Touch interaction is the core gameplay — retrofitting drag/rotate to touch is a known anti-pattern that produces poor mobile UX; performance problems surface late; UI must be redesigned for smaller screens
- **Estimated Effort**: Same initial, +50% for mobile port rework
- **Rejection Reason**: The core fantasy — physically manipulating objects to cast shadows — is fundamentally a touch interaction. Designing for mouse first would compromise the primary experience.

### Alternative 2: Simultaneous Platform Development

- **Description**: Build iOS, Android, and PC in parallel from Sprint 1
- **Pros**: All platforms ship at once; no "port" phase
- **Cons**: Doubles QA matrix (3 platforms × quality tiers); input system must handle all modalities from day one; asset pipeline complexity increases; indie team lacks bandwidth
- **Estimated Effort**: +80% ongoing effort across all sprints
- **Rejection Reason**: Team size makes this impractical. Sequential platform development (mobile → PC) is the standard indie approach for touch-first games.

### Alternative 3: Mobile-Only (No PC Version)

- **Description**: Ship only on iOS and Android; no PC version planned
- **Pros**: Simplest scope; no platform abstraction needed; all optimization effort focused
- **Cons**: Forfeits PC/Steam market (significant revenue for premium puzzle games); limits potential audience; game concept genuinely works well on both platforms
- **Estimated Effort**: -20% (saves abstraction layer and PC adaptation phase)
- **Rejection Reason**: The PC market is valuable for premium narrative puzzle games (Limbo, Inside, Gorogoa all performed well on PC). The abstraction cost is low, and the PC adaptation phase is bounded.

## Consequences

### Positive

- Every system has a single, clear performance target (mobile budget) — no ambiguity
- Touch-optimized interaction design serves the core gameplay fantasy directly
- Mobile market reach (iOS + Android) for initial launch
- Low abstraction cost: `IInputAdapter` and `IQualityTierProvider` are simple interfaces
- YooAsset's hot-update capability enables post-launch content delivery on mobile

### Negative

- PC version requires a dedicated adaptation sprint (input mapping, UI layout, quality tier extension)
- Visual features are constrained by mobile GPU capabilities (shadow resolution, post-processing)
- Cannot leverage PC-only rendering features (volumetric lighting, high-res shadows) in Phase 1 design
- Editor development uses mouse-simulated-touch, which is imperfect for gesture tuning

### Neutral

- Quality tier system adds a small amount of architectural complexity but is standard practice
- Asset variants require YooAsset configuration per-tier but align with existing pipeline

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| Low-end Android devices can't maintain 60fps | Medium | High | Quality tier system with Low preset; auto-detect drops to 30fps target; profile on Adreno 613 in Sprint 0 |
| Touch gesture recognition has latency issues on cheap Android devices | Low | Medium | Budget 1ms max for gesture recognition; test on budget hardware early; fallback to simpler gesture set |
| Mobile memory ceiling (1.5GB) exceeded by shadow rendering + scene assets | Medium | Medium | YooAsset unload discipline; shadow atlas pooling; memory profiler gate in CI |
| PC adaptation phase takes longer than estimated | Low | Low | Abstraction interfaces are in place from day one; PC phase is additive, not refactoring |
| ASTC not supported on target Android minimum spec | Low | High | ETC2 as fallback is already in the asset pipeline; verify on Adreno 613 during Sprint 0 |

## Performance Implications

| Metric | Before (No Strategy) | Expected After | Budget |
|--------|---------------------|---------------|--------|
| CPU (frame time) | Undefined | < 16.67ms | 16.67ms |
| Gameplay systems | Undefined | < 5.5ms | 5.5ms |
| Draw calls | Undefined | < 150 (mobile) | 150 (mobile) / 300 (PC) |
| Memory | Undefined | < 1.5 GB (mobile) | 1.5 GB (mobile) / 4 GB (PC) |
| Load Time | Undefined | < 3s per scene | 3s |
| APK/IPA Size | Undefined | < 200 MB initial | 200 MB |

## Migration Plan

This is a greenfield decision — no existing systems to migrate. However:

1. **Sprint 0**: Establish device testing pipeline (iPhone 12 + low-end Android); verify ASTC/ETC2 support; profile empty scene frame time
2. **Sprint 0**: Implement `IInputAdapter` (touch) and `IQualityTierProvider` with auto-detection
3. **All sprints**: Each system implementation must pass mobile budget gate before merge
4. **PC Phase**: Add `MouseKeyboardInputAdapter`; extend quality tiers; add PC-specific asset variants

**Rollback plan**: If mobile proves unviable as primary target (unlikely given the genre), the `IInputAdapter` abstraction allows pivoting to PC-first with touch as secondary. Quality tier system works bidirectionally.

## Validation Criteria

- [ ] iPhone 12 (A14 Bionic) sustains 60fps in gameplay scene with Medium quality tier
- [ ] iPhone 13 Mini sustains 60fps with Medium tier (stretch target — verify in Sprint 0)
- [ ] Low-end Android (Adreno 613 class) sustains 60fps with Low quality tier
- [ ] Draw calls stay below 150 in most complex gameplay scene (mobile build)
- [ ] Total app memory stays below 1.5 GB during peak gameplay (mobile build)
- [ ] Touch gesture response latency < 1 frame (< 16.67ms from touch event to gameplay response)
- [ ] Scene load time < 3 seconds on iPhone 12
- [ ] APK initial download < 200 MB
- [ ] `IInputAdapter` interface exists and all gameplay code uses it (no direct `Input.GetTouch()` calls outside adapter)
- [ ] Quality tier auto-detection correctly selects Low/Medium/High on test devices

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| `design/gdd/input-system.md` | Input | Touch as primary input; three-layer architecture (Raw → Gesture → Event) | Defines touch as primary; `IInputAdapter` enables the three-layer split; PC keyboard/mouse deferred to Phase 2 adapter |
| `design/gdd/input-system.md` | Input | "所有手势识别在同一帧内完成" (single-frame gesture recognition) | 5.5ms gameplay budget leaves room for sub-1ms gesture processing within 16.67ms frame |
| `design/gdd/settings-accessibility.md` | Settings | `touch_sensitivity` slider (0.5–2.0) | `IInputAdapter.SetSensitivity()` exposes this; mobile baseline ensures it's tested on real touch hardware |
| `design/gdd/settings-accessibility.md` | Settings | `target_framerate` toggle (30/60) | Performance budget defines 60fps as target with 30fps fallback; quality tier Low may default to 30fps on weakest hardware |
| `design/gdd/ui-system.md` | UI | Safe area handling via TEngine `SetUISafeFitHelper` | Mandated on all root canvases; verified on notch devices |
| `design/gdd/ui-system.md` | UI | Touch target minimum sizing | 44dp minimum (Apple HIG); 48dp preferred (Material Design) |
| `design/gdd/ui-system.md` | UI | CanvasScaler configuration | Scale With Screen Size, 1920×1080 reference, match 0.5 |
| `design/gdd/urp-shadow-rendering.md` | Rendering | Shadow rendering must fit mobile GPU budget | Quality tier system scales shadow resolution (512/1024/2048) per hardware class |
| `design/gdd/scene-management.md` | Scene | Async scene loading | < 3s load time budget; YooAsset async loading mandated |
| `design/gdd/audio-system.md` | Audio | Compressed audio with streaming | Vorbis/AAC compression; streaming for music >30s; 80MB audio memory budget |

## Related

- **ADR-001** (TEngine Framework) — YooAsset asset pipeline and TEngine UI module are the implementation vehicles for this ADR's asset and UI strategies
- **ADR-002** (URP Rendering) — Shadow quality tiers defined here constrain the URP shadow rendering configuration
- `.claude/docs/technical-preferences.md` — Performance budgets defined here are the authoritative source; technical-preferences.md should reference this ADR
