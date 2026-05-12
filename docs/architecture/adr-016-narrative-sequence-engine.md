// 该文件由Cursor 自动生成

# ADR-016: Narrative Sequence Engine

## Status

Accepted (Promoted 2026-05-06 — bulk ceremony post Sprint 3 closure / ADR-029 V2.0 review B-1; required to unblock Narrative Event sprint)

## Date

2026-04-22

## Last Verified

2026-04-22

## Decision Makers

Technical Director, Lead Programmer, Narrative Director

## Summary

The Narrative Event System requires an engine that plays time-sorted sequences of atomic effects (AudioDucking, ColorTemperature, SFXOneShot, CameraShake, ScreenFade, TextureVideo, ObjectSnap, LightIntensity, ShadowFade, ObjectFade) configured entirely through Luban tables. We adopt a **time-sorted sequence playback engine** supporting 3 sequence types (MemoryReplay, ChapterTransition, AbsencePuzzle), with `PuzzleLockAll/Unlock` via token-based locking, `InputBlocker` integration, and a sequence queue (max 3). All sequence definitions are data-driven — no narrative logic in code.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.62f2 (LTS) |
| **Domain** | Feature / Narrative / Timeline / Video |
| **Knowledge Risk** | LOW (PlayableDirector, VideoPlayer) / MEDIUM (TEngine GameEvent, ResourceModule) |
| **References Consulted** | `narrative-event-system.md`, `architecture.md` §4.3/§5.2/§6.7 |
| **Post-Cutoff APIs Used** | TEngine `GameEvent`, `GameModule.Resource.LoadAssetAsync` |
| **Verification Required** | Sprint 0: confirm PlayableDirector integration with additive scenes; verify VideoPlayer memory footprint on mobile |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-027 (GameEvent Interface Protocol — supersedes ADR-006 §1/§2 for event communication), ADR-006 §3-§6 (listener lifecycle + token protocol + ordering), ADR-007 (Luban — sequence config table access) |
| **Enables** | N/A (terminal feature system) |
| **Blocks** | All narrative presentation — memory replays, chapter transitions, absence sequences |
| **Ordering Note** | ADR-027 and ADR-007 must be Accepted; this ADR should reach Accepted before Narrative Sprint. Implementation code must use `INarrativeEvent` / `IPuzzleEvent` / `IPuzzleLockEvent` interfaces per ADR-027. |

## Context

### Problem Statement

When a player completes a puzzle (PerfectMatch) or a chapter, the game presents a cinematic "memory replay" — color temperature shifts, objects snap to emotional positions, SFX plays, and texture videos fade onto walls. These sequences must:

1. Be entirely data-driven — designers iterate on sequences by editing Luban config tables, not code
2. Support parallel effects (multiple effects at the same `startTime`) and sequential effects (ordered by `startTime`)
3. Lock all player interaction during playback (InputBlocker + PuzzleLockAll)
4. Handle 3 distinct sequence types with different emotional characteristics
5. Queue up to 3 sequences if triggers arrive during playback
6. Gracefully handle missing resources (skip effect, continue sequence)

### Constraints

- **Data-driven mandate** (Architecture Principle P1): All sequence content in Luban tables
- **Event-driven triggers** (Architecture Principle P2): Sequences triggered by `Evt_PerfectMatch`, `Evt_AbsenceAccepted`, `Evt_ChapterComplete` — not direct method calls
- **Async resource loading** (Architecture Principle P3): Video clips and Timeline assets loaded via `GameModule.Resource.LoadAssetAsync`
- **Memory budget**: VideoPlayer on mobile uses significant memory; must preload selectively
- **Token-based locking**: `PuzzleLockAll` events from multiple sources (Puzzle, Narrative) require token-based matching to prevent incorrect unlocks

### Requirements

- TR-narr-002: Time-sorted atomic effect sequence
- TR-narr-003: All sequence content from Luban config
- TR-narr-004: PuzzleLockAll + InputBlocker during sequences
- TR-concept-008: No hardcoded narrative content

## Decision

**Implement a data-driven narrative sequence engine with 10 atomic effect types, time-sorted playback, token-based puzzle locking, InputBlocker integration, and a bounded sequence queue.**

### Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                   Narrative Event System                           │
│                                                                    │
│  ┌────────────────────────────────────────────────┐              │
│  │            Sequence Player                      │              │
│  │                                                  │              │
│  │  State: Idle → Playing → WaitingForTimeline      │              │
│  │                                                  │              │
│  │  On trigger event:                               │              │
│  │  1. Lookup sequenceId from Luban mapping table   │              │
│  │  2. Load NarrativeSequenceConfig                 │              │
│  │  3. PushBlocker("narrative") + PuzzleLockAll     │              │
│  │  4. Sort effects by startTime                    │              │
│  │  5. Execute effects as elapsed >= startTime      │              │
│  │  6. On complete: PopBlocker + PuzzleUnlock       │              │
│  └────────────────────┬───────────────────────────┘              │
│                       │                                            │
│  ┌────────────────────┴───────────────────────────┐              │
│  │          Atomic Effect Executors                 │              │
│  │                                                  │              │
│  │  ColorTemperature  │  SFXOneShot  │  AudioDucking│              │
│  │  ObjectSnap        │  ScreenFade  │  TextureVideo│              │
│  │  ShadowFade        │  ObjectFade  │  CameraShake │              │
│  │  LightIntensity    │  Wait        │  Timeline    │              │
│  └──────────────────────────────────────────────────┘              │
│                                                                    │
│  Queue: [Sequence1 (playing)] → [Sequence2 (queued)] → [max 3]   │
└──────────────────────────────────────────────────────────────────┘
```

### Atomic Effect Types

| Effect Type | ID | Parameters | Target System |
|------------|-----|-----------|---------------|
| **AudioDucking** | `audio_duck` | duckRatio, fadeDuration | Audio (via Evt_AudioDuckingRequest) |
| **ColorTemperature** | `color_temp` | targetColor, duration, easing | Scene lights (direct Light component) |
| **SFXOneShot** | `sfx_oneshot` | sfxId, delay, volume | Audio (via Evt_PlaySFXRequest) |
| **CameraShake** | `cam_shake` | intensity, duration, frequency | Main Camera |
| **ScreenFade** | `screen_fade` | fadeColor, fadeInDur, holdDur, fadeOutDur | UI (full-screen overlay) |
| **TextureVideo** | `tex_video` | videoClipPath, targetRenderer, fadeIn, hold, fadeOut, alpha | VideoPlayer + Renderer/UI |
| **ObjectSnap** | `obj_snap` | objectId, targetPos, targetRot, duration, easing | Object Interaction (via Evt_PuzzleSnapToTarget) |
| **LightIntensity** | `light_intensity` | lightId, targetIntensity, duration, easing | Scene Light component |
| **ShadowFade** | `shadow_fade` | anchorId, targetAlpha, duration, easing | URP Shadow Rendering (shader param) |
| **ObjectFade** | `obj_fade` | objectId, targetAlpha, duration, easing | Object material alpha |

### Sequence Types

| Type | Trigger Event | Lock Behavior | Typical Duration | Notes |
|------|--------------|--------------|-----------------|-------|
| **MemoryReplay** | `Evt_PerfectMatch` | PuzzleLockAll + InputBlocker | 5-8s | Standard puzzle completion |
| **ChapterTransition** | `Evt_ChapterComplete` | PuzzleLockAll + InputBlocker + ScreenFade | 8-15s | Includes Timeline playable |
| **AbsencePuzzle** | `Evt_AbsenceAccepted` | PuzzleLockAll + InputBlocker | 5-8s | Cool color temp, ShadowFade, no ObjectSnap |

### Token-Based PuzzleLock

```csharp
// Narrative pushes a named lock token
GameEvent.Send(Evt_PuzzleLockAll, new LockPayload { token = "narrative_seq_001" });
InputService.PushBlocker("narrative_seq_001");

// On sequence complete, pops the matching token
GameEvent.Send(Evt_PuzzleUnlock, new UnlockPayload { token = "narrative_seq_001" });
InputService.PopBlocker("narrative_seq_001");
```

This prevents the scenario described in Architecture §9 Open Question #6: if Puzzle System also sends `PuzzleLockAll`, Narrative's unlock won't accidentally release Puzzle's lock (and vice versa), because tokens must match.

### Sequence Queue

- Maximum 3 queued sequences (`queueMaxSize = 3`, configurable)
- If queue is full when a new trigger arrives, the new sequence is dropped with `Log.Warning`
- Sequences execute FIFO — first triggered, first played
- Each sequence independently manages its own InputBlocker token

### Luban Config Structure

```
NarrativeSequenceConfig:
  sequenceId: string (PK)
  sequenceType: enum (MemoryReplay, ChapterTransition, AbsencePuzzle)
  effects: List<AtomicEffectEntry>

AtomicEffectEntry:
  effectType: string (enum matching effect type IDs)
  startTime: float (seconds from sequence start)
  params: varies by effectType (structured as typed fields, not generic dict)

PuzzleNarrativeMap:
  puzzleId: int → sequenceId: string

ChapterTransitionMap:
  chapterId: int → sequenceId: string
```

### Playback Algorithm

```csharp
async UniTask PlaySequence(NarrativeSequenceConfig config)
{
    PushBlocker(config.SequenceId);
    SendPuzzleLockAll(config.SequenceId);

    var effects = config.Effects.OrderBy(e => e.StartTime);
    float elapsed = 0;
    var activeEffects = new List<IAtomicEffect>();

    while (elapsed < config.TotalDuration || activeEffects.Count > 0)
    {
        foreach (var entry in effects.Where(e => !e.Started && elapsed >= e.StartTime))
        {
            var effect = CreateEffect(entry);
            if (effect != null)
            {
                effect.Start();
                activeEffects.Add(effect);
                entry.Started = true;
            }
        }

        foreach (var effect in activeEffects)
            effect.Update(Time.deltaTime);

        activeEffects.RemoveAll(e => e.IsComplete);
        elapsed += Time.deltaTime;
        await UniTask.Yield();
    }

    PopBlocker(config.SequenceId);
    SendPuzzleUnlock(config.SequenceId);
    GameEvent.Send(Evt_SequenceComplete, config.SequenceId);
}
```

### Resource Loading Strategy

- **Pre-warm**: When puzzle enters Active state, pre-load that puzzle's MemoryReplay sequence's video and audio assets
- **Async load**: `await GameModule.Resource.LoadAssetAsync<VideoClip>(path)` before TextureVideo effect starts
- **Graceful failure**: If any asset fails to load, skip that effect, continue sequence, `Log.Warning`
- **Unload**: After sequence completes, unload video assets (Architecture Principle P4)

## Alternatives Considered

### Alternative 1: Unity Timeline for All Sequences

- **Description**: Author all narrative sequences as Unity Timeline assets with PlayableDirector, using Timeline tracks for each effect type
- **Pros**: Visual authoring in Unity Editor; Timeline tracks handle parallel/sequential timing natively; built-in preview
- **Cons**: Timeline assets are not hot-updatable via HybridCLR (binary assets); designers need Unity Editor access for every change; no Luban integration; each puzzle requires a separate Timeline asset; harder to A/B test different timings
- **Rejection Reason**: Violates Architecture Principle P1 (Data-Driven). Luban config tables can be hot-updated without app recompilation; Timeline cannot. Timeline is still used for ChapterTransition full-screen cutscenes, but not for the more frequently iterated MemoryReplay sequences.

### Alternative 2: Visual Scripting (e.g., NodeCanvas, xNode)

- **Description**: Use a visual scripting graph to define narrative sequences as node-based flows
- **Pros**: Visual authoring; supports complex branching; reusable nodes
- **Cons**: Additional dependency; graph serialization format not compatible with Luban pipeline; overhead for simple time-sorted sequences; learning curve for team
- **Rejection Reason**: Over-engineered for linear time-sorted sequences. Narrative sequences in 影子回忆 are strictly time-based, not branching. A simple sorted list of effects is sufficient and more maintainable.

### Alternative 3: Hardcoded Sequence Methods

- **Description**: Each puzzle/chapter transition has a dedicated C# method (e.g., `PlayPuzzle1MemoryReplay()`) that manually calls effects in sequence
- **Pros**: Maximum control; easy to debug; no config parsing overhead
- **Cons**: Catastrophically violates Data-Driven principle; every content change requires code modification and HybridCLR redeploy; 30+ puzzles × sequence method = massive code surface; designer iteration blocked by programmer availability
- **Rejection Reason**: Directly contradicts Architecture Principle P1. The entire point of the narrative sequence engine is to decouple content from code.

## Consequences

### Positive

- **Hot-updatable content**: Sequence timing and parameters can be changed via Luban config without code changes or HybridCLR redeploy
- **Composable effects**: 10 atomic effect types can be freely combined to create varied emotional experiences per puzzle
- **Token safety**: Token-based locking prevents cross-system unlock conflicts
- **Graceful degradation**: Missing resources skip individual effects without crashing the sequence
- **Queue prevents lost events**: Rapid PerfectMatch triggers (rare but possible) don't lose narrative sequences

### Negative

- **Config complexity**: Designers must author sequences in Luban table format, which is less visual than Timeline. Mitigation: provide a sequence preview tool in Editor.
- **No branching**: Current design only supports linear sequences. If future chapters need conditional branching (e.g., different replay based on hint usage), the engine would need extension.
- **VideoPlayer memory**: Mobile VideoPlayer loads entire clip into memory. For 720p × 5s clips, this is ~10-20MB per video. Must pre-load carefully and unload promptly.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| VideoPlayer memory pressure on low-end mobile | MEDIUM | HIGH | Limit video resolution to 720p; compress to H.264 baseline profile; unload immediately after playback |
| Luban `params` field serialization for varied effect types | MEDIUM | MEDIUM | Use typed sub-tables per effect type rather than generic dict (Open Question #4 from GDD) |
| PlayableDirector in additive scene conflicts with main scene cameras | LOW | MEDIUM | ChapterTransition Timeline uses dedicated Camera override; verify in Sprint 0 |
| Token mismatch between lock and unlock (programmer error) | LOW | HIGH | Unit test: verify every PushBlocker has matching PopBlocker; lint rule for token string consistency |

## Performance Implications

| Metric | Expected | Budget | Notes |
|--------|----------|--------|-------|
| CPU (sequence playback per frame) | 0.1-0.5ms | ≤ 1ms | Effect update loop; most effects are simple lerps |
| CPU (TextureVideo decoding) | 1-3ms | Shared with render budget | VideoPlayer hardware decoding on mobile |
| Memory (per active video) | 10-20MB | Within 1.5GB ceiling | 720p H.264, 5-8s duration |
| Memory (sequence config) | < 50KB total | Negligible | All puzzle sequences combined |
| Load time (pre-warm) | 200-500ms | During Active state, before PerfectMatch | Async, non-blocking |

## Validation Criteria

- [ ] MemoryReplay sequence triggers within 1 frame of PerfectMatch event
- [ ] All 10 atomic effect types execute correctly in isolation and in combination
- [ ] Parallel effects (same startTime) execute simultaneously — verified with AudioDucking + ObjectSnap at t=0
- [ ] InputBlocker active for entire sequence duration — player input verified as blocked
- [ ] PuzzleLockAll/Unlock tokens match correctly — no orphaned locks after sequence
- [ ] Missing video resource: sequence continues without crash, Log.Warning emitted
- [ ] Sequence queue: 3 rapid PerfectMatch triggers → all 3 sequences play in order
- [ ] Queue overflow (4th trigger while 3 queued): dropped with Log.Warning, no crash
- [ ] AbsencePuzzle sequence: ShadowFade executes, no ObjectSnap, cool ColorTemperature verified
- [ ] ChapterTransition: Timeline plays in letterbox mode, ScreenFade transitions correctly
- [ ] All sequence content loaded from Luban config — modifying config changes playback without recompilation
- [ ] VideoPlayer memory released after sequence complete (verified via Unity Profiler)
- [ ] App pause during sequence: timer pauses, resumes correctly on return

## GDD Requirements Addressed

| GDD Document | Requirement | How This ADR Satisfies It |
|-------------|-------------|--------------------------|
| `narrative-event-system.md` | 10 atomic effect types | All 10 types implemented as pluggable effect executors |
| `narrative-event-system.md` | Time-sorted sequence playback from Luban config | Sorted effect list with startTime-based trigger |
| `narrative-event-system.md` | 3 sequence types (MemoryReplay, ChapterTransition, AbsencePuzzle) | sequenceType enum with type-specific behavior |
| `narrative-event-system.md` | PuzzleLockAll/Unlock during sequences | Token-based locking prevents cross-system conflicts |
| `narrative-event-system.md` | InputBlocker integration | PushBlocker/PopBlocker with sequence-scoped token |
| `narrative-event-system.md` | Sequence queue (max 3) | FIFO queue with configurable max size |
| `narrative-event-system.md` | Chapter-final merged sequence | isChapterFinalPuzzle flag selects merged sequence from config |
| `architecture.md` §5.2 | Puzzle Complete Flow | Exact flow: PerfectMatch → Lock → Effects → Unlock → Complete |

## Related

- **Depends On**: ADR-027 (GameEvent Interface Protocol) — all triggers and dispatches via `[EventInterface]` interfaces; legacy `Evt_Xxx` names in this ADR map to interfaces per `architecture-traceability.md` Appendix A
- **Depends On**: ADR-006 §3-§6 (inherited by ADR-027) — listener lifecycle, token protocol, ordering
- **Depends On**: ADR-007 (Luban) — all sequence config from Luban tables
- **Triggers**: ADR-013 (Object Interaction) — sends PuzzleLockAll/SnapToTarget events
- **Triggers**: ADR-017 (Audio Mix) — sends AudioDucking/SFX requests
- **References**: `architecture.md` §4.3 (Narrative Event System ownership), §5.2 (Puzzle Complete Flow), §6.7 (INarrativeEvent interface)

---

## Implementation Expand (Sprint 4 S4-02 — 2026-05-06)

> **Source**: Sprint 4 plan Track A 第二站；本节为 ADR-016 v1 (2026-04-22) 的 Sprint 4 implementation alignment update：(a) ADR-027 interface protocol 替换 legacy `Evt_*` const-int；(b) ADR-029 V2.0 R3 mandatory + §V2-5 framework boundary probe coverage；(c) Sprint 4 implementation file paths；(d) 首批 story 创建索引。沿 S4-01 ADR-014 expand pattern。

### A. ADR-027 Interface Protocol Mapping (replaces legacy `Evt_*` const-int)

ADR-016 v1 文中事件采用 `Evt_PerfectMatch` / `Evt_AbsenceAccepted` / `Evt_ChapterComplete` / `Evt_AudioDuckingRequest` / `Evt_PuzzleSnapToTarget` 等 ADR-006 const-int 命名（已 superseded by ADR-027 2026-04-23）。本节定义 ADR-027 接口协议下的 `INarrativeEvent`：

```csharp
namespace GameLogic
{
    [EventInterface(EEventGroup.GroupLogic)]
    public interface INarrativeEvent
    {
        /// <summary>Sequence player 接收外部 trigger（PerfectMatch / AbsenceAccepted / ChapterComplete）后内部自处理；本方法是外部主动 trigger。
        /// Sender: PuzzleStateMachine / ChapterStateManager / Tutorial（在特殊 case 下）。Listener: NarrativeSequencePlayer.
        /// Cascade depth ≤ 3（per ADR-027 §2 guardrail）：trigger → sequence start → InputBlocker push + PuzzleLockAll cascade。</summary>
        void OnRequestSequence(int triggerSourceId, NarrativeSequenceType sequenceType);

        /// <summary>Sequence 开始播放（内部 NarrativeSequencePlayer 触发）。Cascade: 触发 IPuzzleLockEvent.OnPuzzleLockAll + IInputBlockerEvent.OnPushBlocker.</summary>
        void OnSequenceStart(int sequenceId, NarrativeSequenceType sequenceType);

        /// <summary>Sequence 完成播放，所有 atomic effects 执行完毕。Cascade: 触发 IPuzzleLockEvent.OnPuzzleUnlock + IInputBlockerEvent.OnPopBlocker.</summary>
        void OnSequenceComplete(int sequenceId, NarrativeSequenceType sequenceType);

        /// <summary>Sequence 因 resource load failure / queue overflow 等被跳过 / 截断。Listener: ChapterStateManager / SaveManager (确保 progression 不阻塞).</summary>
        void OnSequenceFailed(int sequenceId, string reason);
    }

    public enum NarrativeSequenceType
    {
        MemoryReplay = 0,       // PerfectMatch 后的回忆 sequence
        ChapterTransition = 1,  // 章节 final puzzle 完成 + chapter transition
        AbsencePuzzle = 2,      // Ch.5 AbsenceAccepted 后的 absence sequence
    }
}
```

**Migration table** (legacy → ADR-027)：

| Legacy `Evt_*` | ADR-027 Interface Method |
|---------------|--------------------------|
| `Evt_PerfectMatch` (trigger) | `IShadowPuzzleEvent.OnPerfectMatch` (S4-01)；`INarrativeSequencePlayer.OnRequestSequence` (本 ADR) |
| `Evt_AbsenceAccepted` (trigger) | `IShadowPuzzleEvent.OnAbsenceAccepted` (S4-01) → `INarrativeEvent.OnRequestSequence` (本 ADR) |
| `Evt_ChapterComplete` (trigger) | `IChapterStateEvent.OnChapterComplete` (existing) → `INarrativeEvent.OnRequestSequence` (本 ADR) |
| `Evt_SequenceComplete` (sender) | `INarrativeEvent.OnSequenceComplete(int sequenceId, NarrativeSequenceType)` |
| `Evt_AudioDuckingRequest` (effect) | `IAudioEvent.OnDuckingRequest(float duckRatio, float fadeDuration)` (defined in ADR-017 expand) |
| `Evt_PlaySFXRequest` (effect) | `IAudioEvent.OnPlaySFX(int sfxId, float delay, float volume)` (defined in ADR-017 expand) |
| `Evt_PuzzleSnapToTarget` (effect) | `IObjectInteractionEvent.OnSnapToTarget(int objectId, Vector3 pos, Quaternion rot, float duration, EaseType easing)` (defined in ADR-013) |
| `Evt_PuzzleLockAll` (cascade) | `IPuzzleLockEvent.OnPuzzleLockAll(PuzzleLockPayload payload)` (contract source ADR-027 §4) ⚠️ **Deprecated production override** Sprint 5 S5-05 — production 实际通过 `IInputBlockerEvent.OnPushBlocker(string token)` single-layer pattern 实现 lock effect |
| `Evt_PuzzleUnlock` (cascade) | `IPuzzleLockEvent.OnPuzzleUnlock(PuzzleLockPayload payload)` (contract source ADR-027 §4) ⚠️ **Deprecated production override** Sprint 5 S5-05 — production 实际通过 `IInputBlockerEvent.OnPopBlocker(string token)` single-layer pattern 实现 unlock effect |

> ⚠️ **IPuzzleLockEvent contract drift note** (V3.0 §V3-1.b Type-5 dp5 + Sprint 6 S6-05 amend 2026-05-13): legacy reference 写作 `(existing — ADR-013)` + `(string token)` signature 均是 spec wording drift — IPuzzleLockEvent 实际定义在 **ADR-027 §4** (multi-sender token stack 协议)，签名是 `OnPuzzleLockAll(PuzzleLockPayload payload)` / `OnPuzzleUnlock(PuzzleLockPayload payload)`（不是 `(string token)`）。本 ADR cascade comment (§A line 337/340 `IPuzzleLockEvent.OnPuzzleLockAll` reference) 仅作 event name 占位，**不携带 signature 信息**；signature ground truth 见 ADR-027 §4。**Production reality drift**：Sprint 5 S5-05 选用 InputBlocker single-layer 替代 multi-sender token stack pattern；NarrativeSequencePlayer impl 时按 ADR-026 + S5-05 reality 通过 `IInputBlockerEvent.OnPushBlocker / OnPopBlocker` 实现 lock cascade，**不直接 publish IPuzzleLockEvent 接口事件**。IPuzzleLockEvent 接口完整 deprecation 决策推到 Sprint 7+ polish phase 与 ADR-027 §4 一并 systematic review。

**ADR-027 §5 ⚠️ Framework knowledge fact applies**: 任何订阅 `INarrativeEvent` 方法的 listener 必须 handler 内 self-remove + `_handler = null` (null-out) + 外部 cleanup `if (_handler != null) RemoveEventListener(...)` (null-check guard)。

### B. Production Code Paths

```
Assets/GameScripts/HotFix/GameLogic/
├── NarrativeEvent/                                              # 新建目录 (Sprint 4-5 起)
│   ├── NarrativeSequencePlayer.cs                                # Sequence Player 主体（INarrativeSequencePlayer impl）
│   ├── NarrativeSequenceConfig.cs                                # readonly POCO (TbNarrativeSequence 投影)
│   ├── NarrativeSequenceConfigFromLuban.cs                       # provider impl
│   ├── AtomicEffect.cs                                            # base abstract class
│   ├── Effects/
│   │   ├── AudioDuckingEffect.cs
│   │   ├── ColorTemperatureEffect.cs
│   │   ├── SFXOneShotEffect.cs
│   │   ├── CameraShakeEffect.cs
│   │   ├── ScreenFadeEffect.cs
│   │   ├── TextureVideoEffect.cs
│   │   ├── ObjectSnapEffect.cs
│   │   ├── LightIntensityEffect.cs
│   │   ├── ShadowFadeEffect.cs
│   │   ├── ObjectFadeEffect.cs
│   │   ├── WaitEffect.cs
│   │   └── TimelineEffect.cs                                      # PlayableDirector 包装
│   └── SequenceQueue.cs                                            # FIFO queue max 3 + drop oldest 策略
└── IEvent/
    └── INarrativeEvent.cs                                          # 新建 (4 method + EEventGroup.GroupLogic)
```

**Initialize / Shutdown lifecycle pattern** (per ADR-013 v3 + ADR-027 §5)：

```csharp
public sealed class NarrativeSequencePlayer : INarrativeSequencePlayer
{
    private Action<int, NarrativeSequenceType> _onRequestSequence; // ADR-027 §5 null-out

    public void Initialize()
    {
        _onRequestSequence = OnRequestSequenceHandler;
        GameEvent.AddEventListener<int, NarrativeSequenceType>(INarrativeEvent_Event.OnRequestSequence, _onRequestSequence);
        // 同时订阅 IShadowPuzzleEvent.OnPerfectMatch / OnAbsenceAccepted / IChapterStateEvent.OnChapterComplete
    }

    public void Shutdown()
    {
        if (_onRequestSequence != null) // null-check guard
        {
            GameEvent.RemoveEventListener<int, NarrativeSequenceType>(INarrativeEvent_Event.OnRequestSequence, _onRequestSequence);
            _onRequestSequence = null;
        }
        // Stop active sequence; release VideoPlayer + Timeline assets
    }
}
```

### C. ADR-029 V2.0 R3 Mandatory Coverage (R3 PlayMode probe requirements)

Per ADR-029 V2.0 §V2-3 R3 mandatory + §V2-5 framework boundary behavior probe checklist：

| 必备 R3 case | 触发 case | spike 路径 |
|-------------|----------|-----------|
| **Standard sequence playback** (业务 happy path) | MemoryReplay sequence 全 atomic effects 顺序 + 时序触发 | `S402_NarrativeSequenceEngine.cs` IDevSpike P1 |
| **Parallel effects** (业务) | 同 startTime 多 effects 同时触发 (e.g., AudioDucking + ObjectSnap at t=0) | P2 |
| **Sequence queue** (业务) | 3 rapid trigger → 3 sequences 顺序播放；4th trigger drop with warning | P3 |
| **Resource load failure resilience** (业务 + framework boundary) | invalid videoClipPath → effect skip + Log.Warning + sequence 继续 | P4 |
| **PuzzleLockAll/Unlock token** (cross-method state Type-2(b))| sequence 触发 lock token = "narrative_seq_<id>"；unlock 时 token 匹配 → safe；orphan token check → 0 残留 | P5 |
| **InputBlocker push/pop** (cross-method) | sequence 期间 InputBlocker active；外部 input event 被 swallow；sequence 结束后 unblock | P6 |
| **VideoPlayer memory release** (framework boundary) | sequence 包含 TextureVideo → 完成后 Memory profile 不 leak | P7 (advisory — Unity Profiler 接入复杂) |
| **App pause/resume during sequence** (framework boundary §V2-5 cancellation/silent-failure) | sequence 中途 OnApplicationPause → timer 暂停；OnApplicationFocus → resume；effects 时序保持 | P8 |
| **Listener self-removal pattern** (§V2-5 idempotency) | Subscribe INarrativeEvent → null-out + null-check guard 全程无 TEngine "Delete handle failed" exception | P9 |
| **Queue overflow handling** (§V2-5 over-limit) | 5th trigger when queue full (3) → drop with INarrativeEvent.OnSequenceFailed + Log.Warning；queue state 不 corrupt | P10 |

**Spike 文件路径**: `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S402_NarrativeSequenceEngine.cs`

### D. Story-001 Framework

**首批 story 创建**: `production/epics/narrative-event/story-001-sequence-engine.md`

Story scope：
- Implement `INarrativeEvent` interface + 4 method + sender 派发 (ADR-027)
- Implement NarrativeSequencePlayer 主体（IPlayer impl + sequence type dispatching + queue management）
- Implement 12 atomic effects pluggable executors (10 types + Wait + Timeline)
- Token-based PuzzleLockAll/Unlock cascade
- InputBlocker push/pop integration
- Sequence queue FIFO max 3 + drop oldest 策略
- Resource load failure resilience (skip + log + continue)
- Luban TbNarrativeSequence integration via NarrativeSequenceConfigFromLuban provider

**TR coverage** (closes 8 ⚠️ TRs)：
- TR-narr-002 Time-sorted parallel effects ⚠️→✅
- TR-narr-003 Config-table driven sequences ⚠️→✅
- TR-narr-005 Absence puzzle Ch.5 sequence ⚠️→✅
- TR-narr-006 Chapter transition Timeline ⚠️→✅
- TR-narr-007 Chapter-final merged sequence ⚠️→✅
- TR-narr-008 Queue max 3 + drop oldest ⚠️→✅
- TR-narr-009 Resource load failure resilience ⚠️→✅
- TR-narr-010 Timeline paths via config ⚠️→✅
- TR-narr-011 TextureVideo 3-phase alpha ⚠️→ partial（依赖 Effects/TextureVideoEffect.cs 实施）

**Sprint 4 deliverable**: ADR-016 expanded ✅ + Story-001 framework created ✅。actual implementation 留 future Sprint 4-5 dev-story（per Sprint 4 plan §Track A — S4-02 范围是 ADR + story 框架）。

### E. Validation Criteria Update (V2.0 alignment)

V1 §Validation Criteria 13 项保持有效，本节新增 R3-mandatory 验证：

- [ ] R3 PlayMode probe `S402_NarrativeSequenceEngine.cs` 8 CORE cases (P1-P6 + P8/P9/P10) PASS + 2 advisory cases (P7 video memory + 1 reserved)
- [ ] ADR-027 §5 framework knowledge fact compliance (P9 verifies)
- [ ] ADR-029 V2.0 §V2-5 framework boundary behavior coverage (P7/P8/P10 三类 boundary)
- [ ] Story-001 dev-story Phase 1.5 R1/R2/R3 grep gate PASS
- [ ] Story-001 finishes with /story-done verdict APPROVED

**Bulk promotion stamp**: ADR-016 Status: `Accepted (Promoted 2026-05-06 — bulk ceremony post Sprint 3 closure / ADR-029 V2.0 review B-1; required to unblock Narrative Event sprint)`. Implementation Expand done 2026-05-06 — Sprint 4 S4-02.
