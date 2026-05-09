// 该文件由Cursor 自动生成

# ADR-017: Audio Mix Architecture (3-Layer, Ducking, Crossfade)

## Status

Accepted (Promoted 2026-05-06 — bulk ceremony post Sprint 3 closure / ADR-029 V2.0 review B-1; ADR-028 §1 AudioModule activation gate now unblocked)

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

---

## Implementation Expand (Sprint 4 S4-03 — 2026-05-06)

> **Source**: Sprint 4 plan Track A 第三站（收官）；本节为 ADR-017 v1 (2026-04-22) 的 Sprint 4 implementation alignment update：(a) ADR-027 interface protocol；(b) ADR-029 V2.0 R3 mandatory + §V2-5；(c) ADR-028 §1 AudioModule activation gate 真接入；(d) Sprint 4 implementation file paths；(e) 首批 story 创建索引。沿 S4-01/-02 Track A pattern。

### A. ADR-027 Interface Protocol Mapping (replaces legacy `Evt_*` const-int)

ADR-017 v1 文中事件采用 `Evt_AudioDuckingRequest` / `Evt_PlaySFXRequest` / `Evt_MusicCrossfadeRequest` 等 ADR-006 const-int 命名（已 superseded by ADR-027 2026-04-23）。本节定义 ADR-027 接口协议下的 `IAudioEvent`：

```csharp
namespace GameLogic
{
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IAudioEvent
    {
        /// <summary>SFX 一次性播放请求。Sender: gameplay system / NarrativeSequencePlayer atomic effect。
        /// Listener: AudioManager.PlaySFX(sfxId, ...). Cascade: 触发 AudioModule.PlayOneShot.</summary>
        void OnPlaySFX(int sfxId, float delay, float volume);

        /// <summary>Music 切换请求（含 crossfade）。Sender: ChapterStateManager / NarrativeSequencePlayer.
        /// Listener: AudioManager.PlayMusic(clipId, crossfadeDuration). Typical: 章节切换 + chapter-final sequence.</summary>
        void OnPlayMusic(int musicClipId, float crossfadeDuration);

        /// <summary>Music 停止请求（fade out）。Sender: ChapterStateManager / NarrativeSequencePlayer.</summary>
        void OnStopMusic(float fadeDuration);

        /// <summary>Ducking 请求 — 临时降低 Ambient + Music 音量（典型 narrative sequence）。
        /// Sender: NarrativeSequencePlayer atomic effect AudioDucking。
        /// Listener: AudioManager.SetDucking(duckRatio, fadeDuration). Cascade depth ≤ 2.</summary>
        void OnDuckingRequest(float duckRatio, float fadeDuration);

        /// <summary>Ducking 释放 — 恢复正常音量。Sender: NarrativeSequencePlayer sequence 末尾 OR atomic effect end.
        /// Listener: AudioManager.ReleaseDucking(fadeDuration).</summary>
        void OnReleaseDucking(float fadeDuration);

        /// <summary>Layer volume 设置（来自 Settings change 或 narrative special case）。
        /// Sender: SettingsManager OnSettingChanged listener / NarrativeSequencePlayer.
        /// Listener: AudioManager.SetLayerVolume(layer, volume).</summary>
        void OnSetLayerVolume(AudioLayer layer, float volume);

        /// <summary>App pause/resume 信号（Audio system internal 不响应 Unity OnApplicationPause; 由 GameApp 派发）.</summary>
        void OnAudioPauseRequest();
        void OnAudioResumeRequest();
    }

    public enum AudioLayer
    {
        Ambient = 0,
        SFX = 1,
        Music = 2,
    }
}
```

**Migration table** (legacy → ADR-027)：

| Legacy `Evt_*` | ADR-027 Interface Method |
|---------------|--------------------------|
| `Evt_PlaySFXRequest` | `IAudioEvent.OnPlaySFX(int sfxId, float delay, float volume)` |
| `Evt_MusicChange` / `Evt_MusicCrossfadeRequest` | `IAudioEvent.OnPlayMusic(int musicClipId, float crossfadeDuration)` |
| `Evt_StopMusicRequest` | `IAudioEvent.OnStopMusic(float fadeDuration)` |
| `Evt_AudioDuckingRequest` | `IAudioEvent.OnDuckingRequest(float duckRatio, float fadeDuration)` |
| `Evt_ReleaseDuckingRequest` | `IAudioEvent.OnReleaseDucking(float fadeDuration)` |
| `Evt_SettingChanged` (audio sub-channel) | `ISettingsEvent.OnSettingChanged` (existing — ADR-027 §A.5) → IAudioEvent.OnSetLayerVolume cascade |

**ADR-027 §5 ⚠️ Framework knowledge fact applies**: `IAudioEvent` listener (AudioManager) 必须 handler null-out + Cleanup null-check guard。

### B. ADR-028 §1 AudioModule Activation Gate (本 ADR 解锁)

> 🔧 **Amendment 2026-05-09 (post Sprint 5 S5-06 done)**：本 §B 全节中 `GameModule.Audio.Activate()` 调用与"activation gate"是 v1 设计假设，已被 **drift-v2-(a) supersede**。
>
> **真实 framework 行为**：`AudioModule.OnInit()` 框架内已自动调用 `Initialize(Settings.AudioSetting.audioGroupConfigs)`（详见 `Assets/TEngine/Runtime/Module/AudioModule/AudioModule.cs:322-326`）。**业务侧禁止**手动调用：
> - ❌ `GameModule.Audio.Activate()` — IAudioModule 接口不存在此 API
> - ❌ `GameModule.Audio.Initialize(...)` — 重复 init，业务侧不应再调
>
> **业务侧 GameApp.Entrance 真实写法**：仅调 `AudioManager.Instance.Initialize()`（项目层 facade，订阅 IAudioEvent / ISettingsEvent listeners）。
>
> **演化链**（保留作决策史）：
> 1. v1 设计（本 §B 原文）：`GameModule.Audio.Activate()` 占位 — 假设 framework 有 activation gate 概念
> 2. drift-v1（Sprint 5 S5-06 readiness check #2 / 2026-05-06）：实测发现 IAudioModule 不存在 `Activate()`，真接入是 `Initialize(AudioGroupConfig[], Transform, AudioMixer)` — ADR-028 line 103 已记录
> 3. drift-v2-(a)（Sprint 5 S5-06 dev-story v3 / 2026-05-08）：实测 `AudioModule.OnInit()` 已自动 Initialize，业务侧禁止手动 Initialize — 当前现行约束
>
> **真相源**：
> - `src/MyGame/ShadowGame/.claude/skills/tengine-dev/references/modules.md` 「drift-v2-(a) ✅ 现行约定」
> - `src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/GameApp.cs:35-37`（实际代码）
> - `production/qa/playmode-audio-mix-architecture-2026-05-08.md`（PlayMode 10/10 实证）
> - `.cursor/rules/shadowgame-tengine.mdc`（无相关硬规则；framework 自动 init 不需要 vendor patch 红线兜底）
>
> 本 §B 原文（含 line 421 `Activate()` 调用、line 488 R3 P8 case 描述）保留作决策史 audit，不修改原文以维护 ADR 决策史完整性。

ADR-028 §1 explicitly defers AudioModule activation 到 "ADR-017 Accept + Audio Sprint 接入"。**ADR-017 已 Accepted 2026-05-06**；本节是 ADR-028 §1 gate 解锁 reference：

```csharp
// GameApp.Entrance() — 当 AudioManager 接入时（Sprint 4-5 dev-story）
public static void Entrance(object[] objects)
{
    GameEventHelper.Init();
    // ... existing init ...

    // ADR-028 §1 AudioModule activation (post-ADR-017 ✅ 2026-05-06)
    GameModule.Audio.Activate();   // ← 解锁此调用
    AudioManager.Instance.Initialize();  // 启动 AudioManager（IAudioService impl）
    // ...
}
```

**注**: `GameModule.Audio.Activate()` API 名是占位（取决于 TEngine AudioModule 真实 API）；Sprint 4-5 dev-story 时验证。

### C. Production Code Paths

```
Assets/GameScripts/HotFix/GameLogic/
├── Audio/                                              # 新建目录 (Sprint 4-5 起)
│   ├── AudioManager.cs                                 # IAudioService impl 主体
│   ├── AudioMixLayer.cs                                # Per-layer state (Ambient/SFX/Music)
│   ├── AudioConfig.cs                                  # readonly POCO (TbAudio 投影；audioId / volume / 3d / pitch / variant)
│   ├── AudioConfigFromLuban.cs                         # provider impl
│   └── DuckingController.cs                            # Ducking interpolation logic
└── IEvent/
    └── IAudioEvent.cs                                  # 新建 (8 method + EEventGroup.GroupLogic)
```

**Initialize / Shutdown lifecycle pattern** (8 listener × null-out + null-check guard)：

```csharp
public sealed class AudioManager : IAudioService
{
    private Action<int, float, float> _onPlaySFX;
    private Action<int, float> _onPlayMusic;
    private Action<float> _onStopMusic;
    private Action<float, float> _onDuckingRequest;
    private Action<float> _onReleaseDucking;
    private Action<AudioLayer, float> _onSetLayerVolume;
    private Action _onAudioPause;
    private Action _onAudioResume;

    public void Initialize()
    {
        // 8 IAudioEvent listeners + 1 ISettingsEvent listener (cross-system cascade)
        _onPlaySFX = HandlePlaySFX;
        GameEvent.AddEventListener<int, float, float>(IAudioEvent_Event.OnPlaySFX, _onPlaySFX);
        // ... 其余 7 listeners 同模式注册 ...
    }

    public void Shutdown()
    {
        // 8 null-check guards (per ADR-027 §5)
        if (_onPlaySFX != null) { GameEvent.RemoveEventListener<...>(..., _onPlaySFX); _onPlaySFX = null; }
        // ... 其余 7 同模式 ...
        // Stop all AudioSources + release Music crossfade temp source
    }
}
```

### D. ADR-029 V2.0 R3 Mandatory Coverage (R3 PlayMode probe requirements)

Per ADR-029 V2.0 §V2-3 R3 mandatory + §V2-5 framework boundary behavior probe checklist：

| 必备 R3 case | 触发 case | spike 路径 |
|-------------|----------|-----------|
| **3-layer volume isolation** (业务 happy path) | SetLayerVolume(Ambient, 0.6); SetLayerVolume(SFX, 0); 验 Ambient layer 仍有声 / SFX silent / Music 不受影响 | `S403_AudioMixArchitecture.cs` IDevSpike P1 |
| **Master + per-layer volume multiplicative** (业务) | SetMasterVolume(0.5) + SetLayerVolume(SFX, 0.8) → 实际播放 SFX 0.4 (multiplicative) | P2 |
| **Ducking + release smooth interpolation** (业务) | SetDucking(0.3, 0.5s) → 0.5s 内 Ambient/Music 平滑降至 30%；SFX 不受影响；ReleaseDucking 平滑恢复 | P3 |
| **Music crossfade (no gap/overlap artifacts)** (业务 + framework boundary) | PlayMusic(track1) → PlayMusic(track2, 1.0s crossfade) → 1s 内 track1 fade out / track2 fade in；audible 测试 verify (advisory) | P4 |
| **SFX concurrency cap + oldest cull** (业务) | 4 concurrent same sfxId → OK；5th → kills oldest instance | P5 |
| **Cross-method state — Settings change cascade** (Type-2(b) cross-method) | ISettingsEvent.OnSettingChanged → AudioManager.SetLayerVolume → AudioMixLayer.volume field updated | P6 |
| **Listener self-removal pattern** (§V2-5 idempotency) | AudioManager Initialize → 8 listeners subscribe → Shutdown → 8 null-check unsubscribe → 全程无 TEngine "Delete handle failed" exception | P7 |
| **AudioModule activation gate** (Type-2(a) framework facade behavior) | GameModule.Audio.Activate() 前调 PlaySFX → fail-loud or no-op 行为；activation 后 PlaySFX 正常 | P8 |
| **App pause/resume** (§V2-5 cancellation/silent-failure) | OnApplicationPause → all AudioSources pause；resume 后从断点继续 | P9 |
| **Volume = 0 不停 AudioSource** (业务) | SetLayerVolume(SFX, 0) → AudioSource 仍 playing (silent)；SetLayerVolume(SFX, 0.8) → 立即恢复无 restart | P10 |

**Spike 文件路径**: `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S403_AudioMixArchitecture.cs`

### E. Story-001 Framework

**首批 story 创建**: `production/epics/audio-system/story-001-audio-manager-init.md`

Story scope：
- Implement `IAudioEvent` interface + 8 method + sender 派发 (ADR-027)
- Implement AudioManager (IAudioService impl + 3-layer mix + ducking + crossfade)
- 8 listener subscribe with null-out + null-check guard pattern (ADR-027 §5)
- ADR-028 §1 AudioModule activation gate 接入（GameApp.Entrance 时）
- AudioConfigFromLuban provider 接入 TbAudio
- Settings cross-system cascade (ISettingsEvent.OnSettingChanged → IAudioEvent.OnSetLayerVolume)

**TR coverage** (closes 10 ⚠️ TRs)：
- TR-audio-002 Volume formula (4 multipliers) ⚠️→✅
- TR-audio-003 SFX variant + pitch randomization ⚠️→✅
- TR-audio-004 3D spatial audio ⚠️→ partial（依赖 AudioSource positioning impl）
- TR-audio-005 maxConcurrent + oldest cull ⚠️→✅
- TR-audio-008 SFX latency ≤ 1 frame ⚠→✅
- TR-audio-009 Ambient starts within 2s ⚠→ partial（依赖 scene load 时 trigger）
- TR-audio-010 Ambient occasional sounds ⚠→ partial
- TR-audio-011 Audio CPU < 1ms with 10 sources ⚠→✅
- TR-audio-013 App pause/resume ⚠→✅
- TR-audio-014 Music continues during PauseMenu ⚠→✅
- TR-settings-008 Ambient volume independent of sfx_enabled ⚠→✅

**Sprint 4 deliverable**: ADR-017 expanded ✅ + Story-001 framework created ✅。actual implementation 留 future Sprint 4-5 dev-story。

### F. Validation Criteria Update (V2.0 alignment)

V1 §Validation Criteria 12 项保持有效，本节新增 R3-mandatory 验证：

- [ ] R3 PlayMode probe `S403_AudioMixArchitecture.cs` 8 CORE cases (P1/P2/P3/P5/P6/P7/P8/P9/P10) PASS + 2 advisory cases (P4 audible crossfade verify + 1 reserved)
- [ ] ADR-027 §5 framework knowledge fact compliance (P7 verifies — 8 listeners null-out + null-check guard)
- [ ] ADR-029 V2.0 §V2-5 framework boundary behavior coverage (P8 activation gate + P9 pause/resume)
- [ ] ADR-028 §1 AudioModule activation gate 解锁 verified (P8)
- [ ] Story-001 dev-story Phase 1.5 R1/R2/R3 grep gate PASS
- [ ] Story-001 finishes with /story-done verdict APPROVED

**Bulk promotion stamp**: ADR-017 Status: `Accepted (Promoted 2026-05-06 — bulk ceremony post Sprint 3 closure / ADR-029 V2.0 review B-1; ADR-028 §1 AudioModule activation gate now unblocked)`. Implementation Expand done 2026-05-06 — Sprint 4 S4-03.

### G. Track A Sprint 4 Closure (S4-01 + S4-02 + S4-03 完成)

S4-03 是 Sprint 4 Track A "P1 ADR impl expand" 收官 story。Track A 累计：

- S4-01 ADR-014 Puzzle State Machine ✅ (2026-05-06)
- S4-02 ADR-016 Narrative Sequence Engine ✅ (2026-05-06)
- S4-03 ADR-017 Audio Mix Architecture ✅ (2026-05-06)

**Sprint 5 VS slice (chapter 1) 全部 P1 ADR 依赖现已 ready**：
- Puzzle State Machine ✅ → 实现章节 puzzle 完整 lifecycle
- Narrative Sequence Engine ✅ → 实现 PerfectMatch / chapter-final / absence narrative beats
- Audio Mix Architecture ✅ → 实现章节 ambient + SFX + music + ducking 配套

**Sprint 5 VS Build 主要组件依赖图**：

```
Sprint 5 VS Build (chapter 1 端到端):
├── scene-management/SceneManager (S3-01..03 ✅) — chapter scene load/unload
├── object-interaction (S2-08..13 ✅) — drag/rotate/grid-snap
├── shadow-puzzle (S4-01 framework ready ⏳ impl)
├── narrative-event (S4-02 framework ready ⏳ impl)
└── audio-system (S4-03 framework ready ⏳ impl + ADR-028 §1 unblocked)
```
