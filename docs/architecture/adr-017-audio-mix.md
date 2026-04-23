// 该文件由Cursor 自动生成

# ADR-017: Audio Mix Architecture (3-Layer, Ducking, Crossfade)

## Status

Proposed

## Date

2026-04-22

## Last Verified

2026-04-22

## Decision Makers

Technical Director, Lead Programmer, Audio Director

## Summary

The Audio System manages three independent mix layers (Ambient, SFX, Music) through TEngine `AudioModule`, with a ducking system for narrative sequences, music crossfade between chapters, and a clear separation between player-facing volume controls and internal audio baselines. We establish that `sfx_enabled` controls **only the SFX layer** (not Ambient), `ambientVolume` is an internal design baseline (not player-facing), and all audio playback routes through `IAudioService` interface backed by TEngine's AudioModule.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.62f2 (LTS) |
| **Domain** | Core / Audio |
| **Knowledge Risk** | MEDIUM (TEngine AudioModule — API specifics need source verification) |
| **References Consulted** | `audio-system.md`, `settings-accessibility.md`, `architecture.md` §4.2/§6.8 |
| **Post-Cutoff APIs Used** | TEngine `GameModule.Audio`, `GameEvent` |
| **Verification Required** | Sprint 0: confirm AudioModule supports multiple AudioSource pools; verify ducking implementation via volume manipulation (not Unity Mixer Groups) |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-001 (TEngine Framework — AudioModule is the sole audio service accessor) |
| **Enables** | ADR-016 (Narrative Sequence Engine — sends AudioDucking/SFX requests via events) |
| **Blocks** | All audio playback — SFX, music, ambient |
| **Ordering Note** | ADR-001 must be Accepted; this ADR should reach Accepted before Audio System Sprint |

## Context

### Problem Statement

影子回忆 is an emotionally driven game where sound is atmosphere, not information. The audio architecture must:

1. Manage 3 independent mix layers with separate volume controls and behaviors
2. Support narrative ducking — temporarily lowering ambient/music during memory replay sequences
3. Crossfade music between chapters (each chapter has its own ambient music track)
4. Correctly implement the `sfx_enabled` toggle: it controls SFX-layer sounds only, NOT ambient sounds (ambient "is the silence itself")
5. Keep `ambientVolume` as an internal design baseline (0.6), not exposed to players
6. Route all audio through TEngine AudioModule — no direct Unity AudioSource creation

### Constraints

- **TEngine dependency**: All audio access via `GameModule.Audio` (ADR-001)
- **Mobile performance**: ≤ 1ms/frame CPU for audio management with 10 objects on screen (TR-audio-015)
- **Memory**: Total audio memory ≤ 30MB (all loaded assets)
- **No Unity Audio Mixer**: TEngine AudioModule likely manages AudioSources directly, not through Unity Mixer Groups. Ducking and volume control must be implemented at the AudioSource level.
- **SFX concurrency**: Maximum 4 concurrent SFX (same sound), oldest killed on overflow

### Requirements

- TR-audio-001: 3 independent mix layers (Ambient, SFX, Music)
- TR-audio-007: Ducking system (ratio + fade duration)
- TR-audio-015: All audio config from Luban
- TR-settings-001: Volume controls (master, music, sfx)
- TR-settings-002: Settings separate from save data

## Decision

**Implement a 3-layer audio mix system on TEngine AudioModule with per-layer volume management, event-driven ducking, linear crossfade, and strict layer isolation between SFX and Ambient.**

### Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                      Audio System                              │
│                                                                │
│  ┌──────────────────────────────────────────────────┐        │
│  │               IAudioService                       │        │
│  │                                                    │        │
│  │  PlaySFX(sfxId, position?)                        │        │
│  │  PlayMusic(clipId, crossfadeDuration)             │        │
│  │  StopMusic(fadeDuration)                          │        │
│  │  SetDucking(duckRatio, fadeDuration)              │        │
│  │  ReleaseDucking(fadeDuration)                     │        │
│  │  SetLayerVolume(layer, volume)                    │        │
│  │  SetMasterVolume(volume)                          │        │
│  │  PauseAll() / ResumeAll()                         │        │
│  └──────────────────────┬───────────────────────────┘        │
│                         │                                      │
│  ┌──────────────────────┴───────────────────────────┐        │
│  │              Mix Layer Manager                     │        │
│  │                                                    │        │
│  │  ┌─────────┐  ┌─────────┐  ┌──────────┐         │        │
│  │  │ Ambient  │  │   SFX   │  │  Music   │         │        │
│  │  │ Layer    │  │  Layer  │  │  Layer   │         │        │
│  │  │          │  │         │  │          │         │        │
│  │  │ baseline │  │ pool:4  │  │ 2-source │         │        │
│  │  │ =0.6     │  │ spatial │  │ crossfade│         │        │
│  │  │ loops    │  │ 2D/3D   │  │ per-chap │         │        │
│  │  └─────────┘  └─────────┘  └──────────┘         │        │
│  │                                                    │        │
│  │  Final volume per source:                          │        │
│  │  clipBaseVol × layerVol × masterVol × duckingMul  │        │
│  └────────────────────────────────────────────────────┘        │
│                                                                │
│  Backed by: TEngine GameModule.Audio (AudioModule)             │
└──────────────────────────────────────────────────────────────┘
```

### Volume Calculation

```
finalVolume = clipBaseVolume × layerVolume × masterVolume × duckingMultiplier

// Per-layer volume sources:
ambientLayerVolume = ambientBaseVolume (0.6, internal) × masterVolume × duckingMultiplier
sfxLayerVolume     = sfxVolume (player setting) × masterVolume  // NOT affected by ducking
musicLayerVolume   = musicVolume (player setting) × masterVolume × duckingMultiplier
```

| Variable | Source | Range | Notes |
|----------|--------|-------|-------|
| clipBaseVolume | Luban AudioConfig | 0-1 | Per-clip design baseline |
| ambientBaseVolume | Internal constant | 0.6 | NOT player-facing; "构成安静本身" |
| sfxVolume | Player Settings | 0-1 | Controlled by `sfx_volume` slider |
| musicVolume | Player Settings | 0-1 | Controlled by `music_volume` slider |
| masterVolume | Player Settings | 0-1 | Controlled by `master_volume` slider |
| duckingMultiplier | Runtime state | 0-1 | 1.0 normal, 0.3 during narrative ducking |

### sfx_enabled Behavior (Critical Design Decision)

```
sfx_enabled = false:
  - SFX layer: ALL sounds muted (object interaction, puzzle feedback, UI clicks)
  - Ambient layer: UNAFFECTED — continues playing at ambientBaseVolume × masterVolume
  - Music layer: UNAFFECTED

sfx_enabled = true:
  - All layers play normally
```

**Design rationale** (from audio-system.md): Ambient sounds "构成安静本身" — they ARE the silence of the room. Even when a player turns off "sound effects," the room should not go dead silent. The ambient layer provides the emotional baseline; SFX provides interactive feedback. They serve different purposes and must be independently controllable.

### Ducking System

```csharp
void SetDucking(float duckRatio, float fadeDuration)
{
    // Affected layers: Ambient, Music
    // NOT affected: SFX (narrative SFX plays DURING ducking)
    targetDuckingMultiplier = duckRatio;
    duckFadeSpeed = (1.0f - duckRatio) / fadeDuration;
    isDucking = true;
}

void ReleaseDucking(float fadeDuration)
{
    targetDuckingMultiplier = 1.0f;
    duckFadeSpeed = (1.0f - currentDuckingMultiplier) / fadeDuration;
    isDucking = false;
}

// Per-frame update:
duckingMultiplier = Mathf.MoveTowards(duckingMultiplier, targetDuckingMultiplier, duckFadeSpeed * Time.deltaTime);
```

| Parameter | Default | Source | Range |
|-----------|---------|--------|-------|
| defaultDuckRatio | 0.3 | config | 0.1-0.6 |
| defaultDuckFade | 0.5s | config | 0.2-1.5s |

### Music Crossfade

Two AudioSource strategy for seamless crossfade:

```
Chapter change detected (via Evt_SceneLoadComplete with bgmAsset):

sourceA (current): volume fades out over crossfadeDuration
sourceB (next):    volume fades in over crossfadeDuration

After crossfade:
  sourceA.Stop(); sourceA becomes available for next crossfade
  sourceA and sourceB swap roles
```

| Parameter | Default | Source | Range |
|-----------|---------|--------|-------|
| crossfadeDuration | 3.0s | config | 1.0-5.0s |

### SFX Variant and Spatial System

```csharp
void PlaySFX(string sfxId, Vector3? worldPosition = null)
{
    var config = Tables.TbAudioEvent.Get(sfxId);
    if (config == null) { Log.Warning(...); return; }

    // Variant selection
    var clipPath = config.Variants[Random.Range(0, config.Variants.Length)];

    // Concurrency check
    if (activeSFXCount[sfxId] >= config.MaxConcurrent)
        KillOldest(sfxId);

    // Pitch randomization
    float pitch = config.BasePitch + Random.Range(-config.PitchVariance, config.PitchVariance);

    // Spatial mode
    if (worldPosition.HasValue && config.SpatialMode == SpatialMode.ThreeD)
        PlaySpatial(clipPath, worldPosition.Value, pitch, config.Volume);
    else
        Play2D(clipPath, pitch, config.Volume);
}
```

| Parameter | Default | Source | Range |
|-----------|---------|--------|-------|
| sfxMaxConcurrent | 4 | Luban AudioConfig per sfxId | 2-8 |
| pitchVariance | 0.05 | Luban AudioConfig per sfxId | 0-0.15 |

### Event Communication

**Listens to:**

| Event | Action |
|-------|--------|
| `Evt_AudioDuckingRequest { duckRatio, fadeDuration }` | SetDucking() |
| `Evt_AudioDuckingRelease { fadeDuration }` | ReleaseDucking() |
| `Evt_PlaySFXRequest { sfxId, position }` | PlaySFX() |
| `Evt_PlayMusicRequest { clipId, crossfadeDuration }` | PlayMusic() with crossfade |
| `Evt_SceneTransitionBegin` | Music fade out |
| `Evt_SceneLoadComplete { bgmAsset }` | Music crossfade to new track |
| `Evt_SettingChanged { key, value }` | Update layer volumes |
| `Evt_NearMatchEnter` | Play `sfx_puzzle_nearmatch` |
| `Evt_PerfectMatch` | Play `sfx_puzzle_perfectmatch` |

**Does NOT listen to:**
- Object Interaction events directly — SFX triggers come through `Evt_PlaySFXRequest` from any system

## Alternatives Considered

### Alternative 1: Unity Audio Mixer Groups

- **Description**: Use Unity's built-in Audio Mixer with Mixer Groups for Ambient/SFX/Music, Snapshots for ducking, and exposed parameters for volume control
- **Pros**: Visual mixer editing in Unity; Snapshot-based ducking is elegant; built-in DSP effects (reverb, EQ); native crossfade support
- **Cons**: Audio Mixer is a binary asset — not hot-updatable via HybridCLR; TEngine AudioModule may not integrate with Mixer Groups; Mixer Snapshots can't be driven by Luban config; adds a Unity-specific dependency that bypasses TEngine's audio abstraction
- **Rejection Reason**: TEngine AudioModule is the mandated audio service (ADR-001). Introducing Unity Audio Mixer would create a parallel audio system, causing confusion about "who owns volume." Ducking via AudioSource volume manipulation is simpler and Luban-configurable.

### Alternative 2: FMOD / Wwise Integration

- **Description**: Use professional audio middleware (FMOD or Wwise) for all audio management, replacing TEngine AudioModule
- **Pros**: Industry-standard mixing; adaptive audio; superior spatial audio; built-in ducking/crossfade; powerful authoring tools
- **Cons**: Significant licensing cost for an indie project; native plugin integration complexity with HybridCLR; overrides TEngine's AudioModule entirely; team learning curve; overkill for a game with minimal audio complexity
- **Rejection Reason**: Cost and complexity disproportionate to the game's audio needs (3 layers, basic ducking, simple crossfade). TEngine AudioModule handles all required functionality.

### Alternative 3: Single Volume Slider (No Per-Layer Control)

- **Description**: Provide only a master volume slider; no separate music/SFX controls
- **Pros**: Simplest UI; fewer settings to confuse players; single volume state
- **Cons**: Players who want ambient but not SFX (or vice versa) cannot customize; accessibility concern — some players need to mute music for cognitive reasons while keeping SFX for gameplay feedback; contradicts GDD's explicit 3-slider design
- **Rejection Reason**: GDD and settings-accessibility.md explicitly require separate master/music/SFX controls. Player audio preferences are highly individual; forcing a single slider is an accessibility failure.

## Consequences

### Positive

- **Layer independence**: Each audio layer can be controlled, ducked, and muted independently — maximum player control
- **Ambient preservation**: `sfx_enabled` toggle correctly preserves ambient atmosphere when SFX is disabled
- **Smooth transitions**: Crossfade and ducking use gradual volume ramping — no abrupt audio changes
- **Event-driven**: Audio System is fully reactive — it never polls; all triggers come via GameEvent
- **Hot-updatable config**: SFX event definitions, variants, and parameters in Luban — designers iterate without code changes

### Negative

- **TEngine AudioModule dependency**: All audio routing through TEngine's module — if AudioModule has limitations (e.g., max AudioSource count), the system inherits them
- **No DSP effects**: Without Unity Audio Mixer, reverb/EQ must be baked into audio clips or handled by custom processing. For this game's minimalist audio design, this is acceptable.
- **Manual crossfade**: Implementing crossfade with two AudioSources requires manual management (source swapping, state tracking)

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| TEngine AudioModule doesn't support multiple AudioSource pools | MEDIUM | HIGH | Sprint 0 spike: verify AudioModule API. Fallback: create custom AudioSource pool wrapper |
| Ducking + player volume change during narrative sequence causes jarring audio | LOW | MEDIUM | Take minimum of ducked volume and player setting; player setting always wins |
| Music crossfade causes memory spike (two clips loaded simultaneously) | LOW | LOW | Music clips are ~2-5MB each; momentary double-load within 1.5GB budget |
| 3D SFX attenuation sounds wrong on mobile speakers (mono output) | MEDIUM | LOW | Test on device; provide fallback 2D mode for devices detected as mono output |

## Performance Implications

| Metric | Expected | Budget | Notes |
|--------|----------|--------|-------|
| CPU (audio system per frame) | 0.1-0.3ms | ≤ 1.0ms | Volume updates + ducking interpolation + concurrency management |
| CPU (10 simultaneous SFX) | 0.2-0.5ms | Part of 1ms budget | AudioSource updates managed by TEngine |
| Memory (all audio assets loaded) | 15-25MB | ≤ 30MB | .ogg compressed, 44.1kHz |
| Memory (AudioSource pool) | ~100KB | Negligible | Pre-allocated pool of ~20 AudioSources |

## Validation Criteria

- [ ] 3 independent mix layers functional: Ambient/SFX/Music each independently controllable
- [ ] `sfx_enabled = false` mutes SFX layer; Ambient layer continues unaffected
- [ ] ambientVolume (0.6) is internal — no player-facing control; not stored in PlayerPrefs
- [ ] Master volume affects all layers multiplicatively
- [ ] Ducking: SetDucking(0.3, 0.5s) reduces Ambient+Music to 30% over 0.5s; SFX unaffected
- [ ] ReleaseDucking restores volumes smoothly over specified duration
- [ ] Music crossfade: chapter change produces seamless transition with no audible gap or overlap artifacts
- [ ] SFX variant: same sfxId plays different clip variants (verified 5 consecutive plays ≠ all identical)
- [ ] SFX concurrency: 5th concurrent play of same sfxId kills oldest instance
- [ ] App pause/resume: all audio pauses cleanly, resumes from same point with no pop/click
- [ ] Volume set to 0: layer silent but AudioSources still playing (resume without restart)
- [ ] All SFX event IDs and parameters from Luban config — no hardcoded audio references
- [ ] Performance: audio system update ≤ 1ms with 10 objects on screen, verified on iPhone 13 Mini

## GDD Requirements Addressed

| GDD Document | Requirement | How This ADR Satisfies It |
|-------------|-------------|--------------------------|
| `audio-system.md` | 3 independent layers (Ambient/SFX/Music) | Three-layer architecture with independent volume and ducking behavior |
| `audio-system.md` | Ducking system (ratio + fade duration) | SetDucking/ReleaseDucking with interpolated duckingMultiplier |
| `audio-system.md` | Music crossfade between chapters | Dual-source crossfade triggered by Evt_SceneLoadComplete |
| `audio-system.md` | sfx_enabled controls only SFX layer | Explicit layer isolation: sfx_enabled flag only affects SFX AudioSources |
| `audio-system.md` | ambientVolume is internal baseline (0.6) | Internal constant, not exposed via Settings UI |
| `settings-accessibility.md` | Volume controls: master, music, sfx | IAudioService.SetLayerVolume / SetMasterVolume mapped to Settings |
| `settings-accessibility.md` | sfx_enabled as toggle | Toggle gates SFX layer play/mute |
| `architecture.md` §6.8 | IAudioService interface | Implemented as specified |

## Related

- **Depends On**: ADR-001 (TEngine Framework) — AudioModule is the underlying audio service
- **Consumed By**: ADR-016 (Narrative Sequence Engine) — sends ducking/SFX events during sequences
- **References**: `architecture.md` §4.2 (Audio System ownership), §5.3 (Audio Events table), §6.8 (IAudioService)
- **References**: `audio-system.md` (full GDD), `settings-accessibility.md` (player-facing volume controls)
