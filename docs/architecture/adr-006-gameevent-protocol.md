// 该文件由Cursor 自动生成

# ADR-006: GameEvent Communication Protocol

## Status

**Superseded by [ADR-027](./adr-027-gameevent-interface-protocol.md)** — 2026-04-23

> **迁移说明**：本 ADR 的 §1（Event ID 100-per-system 分配）与 §2（Payload 约定）已被 **ADR-027** 取代，改用 TEngine `[EventInterface]` + Roslyn Source Generator 方案。
>
> 以下章节仍然有效，由 ADR-027 以接口形式继承：
>
> - §3 Listener Registration/Cleanup Lifecycle（生命周期协议）
> - §4 Multi-Sender Token Protocol（`LockToken.Puzzle` / `LockToken.Narrative` 语义保持）
> - §5 Event Ordering and Delivery Guarantees（同步、主线程、无 re-entrancy、cascade ≤ 3）
> - §6 Event Documentation Convention（Sender/Listener/Cascade XML 文档，目标改为接口方法）
>
> 新增事件一律按 ADR-027 的接口事件协议实施；**禁止**基于本 ADR 的 `public const int Evt_Xxx` 方案添加新事件。Legacy `Evt_Xxx` → 新接口方法映射见 `architecture-traceability.md` 附录。

---

## Original Status（历史）

Proposed

## Date

2026-04-22

## Last Verified

2026-04-22

## Decision Makers

Technical Director, Lead Programmer

## Summary

All inter-module communication in the 5-layer architecture uses TEngine's `GameEvent` int-based event bus. This ADR defines the formal protocol: event ID allocation scheme (100-ID ranges per system), payload conventions (typed payload classes with a static-registry fallback), listener registration/cleanup lifecycle, multi-sender token-based locking for shared events, and event ordering guarantees. The protocol prevents ID collisions across 15 systems while maintaining the event-driven decoupling mandated by ADR-001.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.62f2 (LTS) |
| **Domain** | Core / Event Communication |
| **Knowledge Risk** | MEDIUM — TEngine 6.0 `GameEvent` payload capabilities require Sprint 0 source verification |
| **References Consulted** | ADR-001 (TEngine Framework), project source (`TEngine/` directory), architecture document Section 5.3 |
| **Post-Cutoff APIs Used** | `GameEvent.Send`, `GameEvent.AddEventListener`, `GameEvent.RemoveEventListener` |
| **Verification Required** | Sprint 0 spike: confirm `GameEvent.Send` supports typed object/struct payloads; if not, implement static payload registry fallback |

> **Note**: If Knowledge Risk is MEDIUM or HIGH, this ADR must be re-validated if the
> project upgrades engine versions. Flag it as "Superseded" and write a new ADR.

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-001 (TEngine 6.0 Framework) — GameEvent system adopted there |
| **Enables** | ADR-016 (Narrative Sequence Engine), ADR-024 (Analytics Telemetry) |
| **Blocks** | All inter-module communication implementation; no system may fire cross-module events until this ADR is Accepted |
| **Ordering Note** | ADR-001 must reach Accepted before this ADR can be implemented. Sprint 0 payload verification must complete before acceptance. |

## Context

### Problem Statement

《影子回忆 (Shadow Memory)》's 5-layer architecture (Foundation → Core → Game → Meta → Infrastructure) contains 15 systems that communicate exclusively through TEngine's `GameEvent` int-based event bus (mandated by ADR-001, satisfying TR-concept-013). Without a formal protocol, the following problems emerge:

1. **ID Collisions** — Multiple systems independently choosing event IDs will inevitably produce duplicates, causing silent cross-talk bugs that are extremely difficult to diagnose
2. **Payload Ambiguity** — Without conventions, events carry inconsistent payload formats (raw ints, string tags, boxed objects), leading to runtime cast failures
3. **Listener Leaks** — If systems don't follow a consistent registration/cleanup lifecycle, orphaned listeners accumulate across scene transitions, causing null reference exceptions and memory leaks
4. **Multi-Sender Conflicts** — `Evt_PuzzleLockAll` has two legal senders (Shadow Puzzle system and Narrative system). Without a coordination mechanism, one system's unlock can incorrectly release a lock placed by the other
5. **Ordering Assumptions** — If systems assume events arrive in a specific order but `GameEvent` provides no ordering guarantee, subtle timing bugs will appear under load or across frame boundaries

### Current State

ADR-001 adopted `GameEvent` as the inter-module communication mechanism and sketched a preliminary `EventId` class with example IDs. The architecture document (Section 5.3) lists all required cross-system events. However, no formal allocation scheme, payload convention, lifecycle protocol, or multi-sender resolution exists.

### Constraints

- **TEngine GameEvent is the only permitted event mechanism** — C# events, ScriptableObject channels, and third-party message buses are forbidden (ADR-001)
- **Int-based IDs** — `GameEvent` uses `int` keys, not strings or enums; the protocol must work within this constraint
- **Mobile performance** — Event dispatch must remain allocation-free on hot paths (no boxing, no GC pressure)
- **HybridCLR assembly boundary** — `EventId` constants and payload classes must reside in the `GameLogic` assembly (hot-updatable) so new events can be added via hot-update
- **15 systems** — The allocation scheme must accommodate all systems in the Systems Index with room for growth

### Open Questions (Resolved in This ADR)

| # | Question | Resolution |
|---|----------|------------|
| OQ-1 (Architecture) | Does `GameEvent.Send` support typed object/struct payloads? | **Assumed YES** — protocol designed for typed payloads. If Sprint 0 verification fails, fallback to static `EventPayload<T>` registry (see Fallback Pattern below) |
| OQ-6 (Architecture) | Should `PuzzleLockAll` use token-based locking to prevent cross-system conflicts? | **YES** — stack-based string tokens on lock/unlock events (see Multi-Sender Protocol below) |

## Decision

**Establish a formal GameEvent communication protocol governing ID allocation, payloads, listener lifecycle, multi-sender coordination, and ordering semantics for all inter-module events in 影子回忆.**

### 1. Event ID Allocation Scheme

Each system receives a reserved range of **100 event IDs**. The centralized `EventId` static class is the single source of truth — no event IDs may be defined outside this class.

```csharp
// File: Assets/GameScripts/HotFix/GameLogic/Event/EventId.cs
// Assembly: GameLogic (hot-updatable)

public static class EventId
{
    // ═══════════════════════════════════════════════════
    // Foundation Layer (per architecture.md §3)
    // ═══════════════════════════════════════════════════

    // Input System: 1000-1099
    public const int Evt_Gesture_Tap          = 1000;
    public const int Evt_Gesture_Drag         = 1001;
    public const int Evt_Gesture_Rotate       = 1002;
    public const int Evt_Gesture_Pinch        = 1003;
    public const int Evt_Gesture_LightDrag    = 1004;

    // Object Interaction: 1100-1199
    public const int Evt_ObjectTransformChanged = 1100;
    public const int Evt_LightPositionChanged   = 1101;
    public const int Evt_ObjectSelected         = 1102;
    public const int Evt_ObjectDeselected       = 1103;

    // ═══════════════════════════════════════════════════
    // Core Layer (per architecture.md §3)
    // ═══════════════════════════════════════════════════

    // Shadow Puzzle: 1200-1299
    public const int Evt_MatchScoreChanged    = 1200;
    public const int Evt_NearMatchEnter       = 1201;
    public const int Evt_NearMatchExit        = 1202;
    public const int Evt_PerfectMatch         = 1203;
    public const int Evt_AbsenceAccepted      = 1204;
    public const int Evt_PuzzleLockAll        = 1205;
    public const int Evt_PuzzleUnlock         = 1206;
    public const int Evt_PuzzleSnapToTarget   = 1207;
    public const int Evt_PuzzleComplete       = 1208;

    // Chapter State: 1300-1399
    public const int Evt_PuzzleStateChanged   = 1300;
    public const int Evt_ChapterComplete      = 1301;
    public const int Evt_RequestSceneChange   = 1302;

    // Scene Management: 1400-1499
    public const int Evt_SceneTransitionBegin    = 1400;
    public const int Evt_SceneUnloadBegin        = 1401;
    public const int Evt_SceneLoadProgress       = 1402;
    public const int Evt_SceneDownloadProgress   = 1403;
    public const int Evt_SceneLoadComplete       = 1404;
    public const int Evt_SceneReady              = 1405;
    public const int Evt_SceneTransitionEnd      = 1406;
    public const int Evt_SceneLoadFailed         = 1407;

    // ═══════════════════════════════════════════════════
    // Feature Layer (per architecture.md §3)
    // ═══════════════════════════════════════════════════

    // Narrative: 1500-1599
    public const int Evt_SequenceComplete     = 1500;
    public const int Evt_LoadNextChapter      = 1501;

    // Audio: 1600-1699
    public const int Evt_AudioDuckingRequest  = 1600;
    public const int Evt_AudioDuckingRelease  = 1601;
    public const int Evt_PlaySFXRequest       = 1602;
    public const int Evt_PlayMusicRequest     = 1603;

    // Hint: 1700-1799
    public const int Evt_HintAvailable        = 1700;
    public const int Evt_HintDismissed        = 1701;

    // ═══════════════════════════════════════════════════
    // Presentation Layer (per architecture.md §3)
    // ═══════════════════════════════════════════════════

    // Tutorial: 1800-1899
    public const int Evt_TutorialStepStarted    = 1800;
    public const int Evt_TutorialStepCompleted  = 1801;

    // Save: 1900-1999
    public const int Evt_SaveComplete         = 1900;

    // Settings: 2000-2099
    public const int Evt_SettingChanged       = 2000;

    // UI System: 2100-2199
    public const int Evt_PanelOpened          = 2100;
    public const int Evt_PanelClosed          = 2101;
    public const int Evt_PopupQueued          = 2102;
    public const int Evt_PopupDequeued        = 2103;

    // ═══════════════════════════════════════════════════
    // Reserved for Future Systems
    // ═══════════════════════════════════════════════════
    // Analytics:  2200-2299  (ADR-024)
    // Debug/Dev:  2300-2399
    // Reserved:   2400-2999
}
```

**Allocation Rules**:
- Each system owns exactly one 100-ID range; no system may use IDs outside its range
- New events within a range are appended sequentially (no gaps, no reuse of deprecated IDs)
- Deprecated event IDs are commented out with `[Deprecated]` tag and date — never removed, never reassigned
- Adding a new system range requires Technical Director approval and an update to this ADR

### 2. Payload Convention

**Primary Approach: Typed Payload Classes** (pending Sprint 0 verification of `GameEvent.Send` payload support)

Each event that carries data defines a corresponding payload struct. Payload types live alongside `EventId.cs` in the `GameLogic` assembly.

```csharp
// File: Assets/GameScripts/HotFix/GameLogic/Event/EventPayloads.cs

// Prefer struct for small, allocation-free payloads
public struct MatchScoreChangedPayload
{
    public float Score;
    public float Threshold;
    public bool IsNearMatch;
}

public struct PuzzleLockPayload
{
    public string Token;    // "puzzle" or "narrative"
}

public struct SceneLoadProgressPayload
{
    public string SceneName;
    public float Progress;  // 0.0 - 1.0
}

public struct SettingChangedPayload
{
    public string Key;
    public object Value;    // boxed, acceptable for rare settings changes
}

// Usage:
GameEvent.Send(EventId.Evt_MatchScoreChanged, new MatchScoreChangedPayload
{
    Score = 0.95f,
    Threshold = 0.90f,
    IsNearMatch = true
});
```

**Payload Design Rules**:
- Use `struct` for events fired frequently (per-frame or per-gesture) to avoid GC allocation
- Use `class` only for payloads containing reference-type collections or large data
- Every event with payload data MUST have a named payload type — no anonymous `object` parameters, no raw `int` encoding
- Events with no payload data are sent with no arguments: `GameEvent.Send(EventId.Evt_PuzzleComplete)`
- Payload types are named `{EventName}Payload` (e.g., `Evt_MatchScoreChanged` → `MatchScoreChangedPayload`)

**Fallback Pattern: Static Payload Registry** (activated if Sprint 0 verifies `GameEvent.Send` does NOT support typed payloads)

If `GameEvent.Send` only supports `int` parameters or no parameters, payloads are passed through a static registry that the sender writes before dispatch and the receiver reads immediately on receipt:

```csharp
// Fallback only — used if GameEvent.Send cannot pass typed payloads
public static class EventPayloadRegistry
{
    private static readonly Dictionary<int, object> _payloads = new();

    public static void Set<T>(int eventId, T payload) where T : struct
    {
        _payloads[eventId] = payload;
    }

    public static T Get<T>(int eventId) where T : struct
    {
        if (_payloads.TryGetValue(eventId, out var obj))
        {
            _payloads.Remove(eventId);
            return (T)obj;
        }
        return default;
    }
}

// Sender:
EventPayloadRegistry.Set(EventId.Evt_MatchScoreChanged, new MatchScoreChangedPayload { ... });
GameEvent.Send(EventId.Evt_MatchScoreChanged);

// Receiver:
void OnMatchScoreChanged()
{
    var payload = EventPayloadRegistry.Get<MatchScoreChangedPayload>(EventId.Evt_MatchScoreChanged);
}
```

> **Warning**: The fallback registry introduces boxing for struct payloads and is not thread-safe. It is acceptable for single-threaded Unity gameplay code but must not be used from background threads (e.g., async loading callbacks). If the fallback is activated, a follow-up ADR will evaluate whether to replace `GameEvent` entirely.

### 3. Listener Registration/Cleanup Lifecycle

All systems follow a strict registration/cleanup protocol to prevent listener leaks:

```
┌─────────────────────────────────────────────────────┐
│               Listener Lifecycle                     │
│                                                      │
│   System.Init()                                      │
│       └── AddEventListener(EventId.X, handler)       │
│                                                      │
│   ┌── Normal Operation ──────────────────────┐       │
│   │   Events dispatched → handlers invoked   │       │
│   └──────────────────────────────────────────┘       │
│                                                      │
│   Evt_SceneUnloadBegin received                      │
│       └── Remove scene-scoped listeners              │
│                                                      │
│   System.Dispose()                                   │
│       └── RemoveEventListener(EventId.X, handler)    │
│                                                      │
│   Post-Dispose Guarantee: zero listeners from system │
└─────────────────────────────────────────────────────┘
```

**Lifecycle Rules**:

| Phase | Action | Enforcement |
|-------|--------|-------------|
| `Init()` | Call `GameEvent.AddEventListener` for all events the system handles | Mandatory — no lazy registration |
| Runtime | Handlers execute on the main thread, synchronously within the `Send` call | By TEngine design |
| `Evt_SceneUnloadBegin` | Systems with scene-scoped listeners must remove them here | Self-enforced per system |
| `Dispose()` | Call `GameEvent.RemoveEventListener` for every listener added in `Init()` | Mandatory — verified by listener leak test |
| Post-Dispose | System must hold zero active listeners | Validated in test suite |

**Scene-Scoped vs Global Listeners**:
- **Global listeners** (e.g., `Evt_SettingChanged`, `Evt_SaveComplete`): registered in `Init()`, removed in `Dispose()` — survive scene transitions
- **Scene-scoped listeners** (e.g., `Evt_ObjectTransformChanged`, `Evt_MatchScoreChanged`): registered in `Init()` or on scene load, removed on `Evt_SceneUnloadBegin` — do not survive scene transitions

**Listener Registration Pattern**:

```csharp
public class ShadowPuzzleSystem : IDisposable
{
    public void Init()
    {
        GameEvent.AddEventListener(EventId.Evt_ObjectTransformChanged, OnObjectTransformChanged);
        GameEvent.AddEventListener(EventId.Evt_PuzzleLockAll, OnPuzzleLockAll);
        GameEvent.AddEventListener(EventId.Evt_SceneUnloadBegin, OnSceneUnloadBegin);
    }

    private void OnSceneUnloadBegin()
    {
        GameEvent.RemoveEventListener(EventId.Evt_ObjectTransformChanged, OnObjectTransformChanged);
        GameEvent.RemoveEventListener(EventId.Evt_MatchScoreChanged, OnMatchScoreChanged);
    }

    public void Dispose()
    {
        GameEvent.RemoveEventListener(EventId.Evt_ObjectTransformChanged, OnObjectTransformChanged);
        GameEvent.RemoveEventListener(EventId.Evt_PuzzleLockAll, OnPuzzleLockAll);
        GameEvent.RemoveEventListener(EventId.Evt_SceneUnloadBegin, OnSceneUnloadBegin);
    }
}
```

> **Safety Note**: `RemoveEventListener` must be safe to call even if the listener was already removed (idempotent). Verify this behavior in Sprint 0 spike.

### 4. Multi-Sender Token Protocol

`Evt_PuzzleLockAll` and `Evt_PuzzleUnlock` have two legal senders: the Shadow Puzzle system (locks during snap-to-target animation) and the Narrative system (locks during cutscenes). A naive boolean lock creates a race condition where one system's unlock releases the other's lock.

**Solution: Stack-based String Token Locking**

```
┌─────────────────────────────────────────────────────────┐
│              Token Lock Stack                            │
│                                                          │
│  Narrative sends PuzzleLockAll(token: "narrative")       │
│      Stack: ["narrative"]                                │
│      → All objects locked                                │
│                                                          │
│  Puzzle sends PuzzleLockAll(token: "puzzle")             │
│      Stack: ["narrative", "puzzle"]                      │
│      → All objects still locked                          │
│                                                          │
│  Puzzle sends PuzzleUnlock(token: "puzzle")              │
│      Stack: ["narrative"]                                │
│      → All objects STILL locked (narrative hasn't        │
│        released yet)                                     │
│                                                          │
│  Narrative sends PuzzleUnlock(token: "narrative")        │
│      Stack: []                                           │
│      → All objects unlocked                              │
└─────────────────────────────────────────────────────────┘
```

**Token Protocol Rules**:

1. **Legal tokens** are predefined constants — no arbitrary strings at runtime:
   ```csharp
   public static class LockToken
   {
       public const string Puzzle   = "puzzle";
       public const string Narrative = "narrative";
   }
   ```
2. The receiving system (Object Interaction) maintains a `HashSet<string>` of active lock tokens
3. Objects are locked if the token set is non-empty; unlocked only when the set is empty
4. A `PuzzleUnlock` with a token not in the set is a **no-op with a warning log** — never an exception
5. A `PuzzleLockAll` with a token already in the set is a **no-op with a warning log** — tokens are not reference-counted
6. On `Evt_SceneUnloadBegin`, the lock token set is force-cleared (scene transition always resets lock state)

**Implementation**:

```csharp
public class ObjectInteractionLockManager
{
    private readonly HashSet<string> _activeLockTokens = new();

    public void OnPuzzleLockAll(PuzzleLockPayload payload)
    {
        if (!_activeLockTokens.Add(payload.Token))
        {
            Debug.LogWarning($"[EventProtocol] Duplicate lock token: {payload.Token}");
            return;
        }
        SetAllObjectsInteractable(false);
    }

    public void OnPuzzleUnlock(PuzzleLockPayload payload)
    {
        if (!_activeLockTokens.Remove(payload.Token))
        {
            Debug.LogWarning($"[EventProtocol] Unlock for unknown token: {payload.Token}");
            return;
        }
        if (_activeLockTokens.Count == 0)
        {
            SetAllObjectsInteractable(true);
        }
    }

    public void OnSceneUnloadBegin()
    {
        _activeLockTokens.Clear();
    }
}
```

### 5. Event Ordering and Delivery Guarantees

| Guarantee | Status | Notes |
|-----------|--------|-------|
| **Synchronous delivery** | YES | `GameEvent.Send` invokes all listeners synchronously before returning to the caller |
| **Main-thread only** | YES | All `Send` calls must originate from the main Unity thread |
| **Listener invocation order** | NO GUARANTEE | Listeners for the same event ID may be invoked in any order; systems must not depend on inter-listener ordering |
| **Cross-event ordering** | CALLER CONTROLLED | If System A sends Event X then Event Y, listeners for X complete before Y's listeners begin (synchronous guarantee) |
| **Delivery during Send** | FORBIDDEN | A listener must NOT call `GameEvent.Send` for the same event ID it is handling (re-entrancy) — this causes infinite recursion |
| **Cross-event cascading** | ALLOWED WITH CAUTION | A listener for Event X may send Event Y (different ID); cascade depth should not exceed 3 to prevent debugging difficulty |

**Cascade Depth Convention**:
```
Evt_PerfectMatch → Evt_PuzzleComplete → Evt_ChapterComplete   ← depth 2, acceptable
Evt_PerfectMatch → Evt_PuzzleComplete → Evt_ChapterComplete → Evt_LoadNextChapter  ← depth 3, maximum
```

Cascades deeper than 3 must be broken with a one-frame delay (`GameModule.Timer` or coroutine yield) and documented in the sending system's code.

### 6. Event Documentation Convention

Every event ID in `EventId.cs` must have a documentation comment specifying:

```csharp
/// <summary>
/// Fired when the shadow match score changes during puzzle interaction.
/// </summary>
/// <remarks>
/// Payload: <see cref="MatchScoreChangedPayload"/>
/// Sender(s): ShadowPuzzleSystem
/// Listener(s): PuzzleHudWindow, HintSystem
/// Cascade: None
/// </remarks>
public const int Evt_MatchScoreChanged = 1200;
```

This serves as the contract between sender and receivers, preventing undocumented coupling.

## Alternatives Considered

### Alternative 1: TEngine GameEvent with Allocated Ranges (Chosen)

- **Description**: Use TEngine's built-in `GameEvent` int-based bus with a centralized `EventId` class partitioned into 100-ID ranges per system
- **Pros**: Simple; zero additional dependencies; proven pattern in TEngine ecosystem; int IDs are fast to compare and dispatch; works within HybridCLR hot-update boundary
- **Cons**: Requires manual maintenance of `EventId.cs`; int IDs are not self-documenting (mitigated by naming convention and XML docs); no compile-time guarantee that sender and receiver agree on payload type
- **Why Chosen**: Minimal complexity, fits the existing framework, satisfies all requirements

### Alternative 2: C# Events/Delegates

- **Description**: Systems expose C# `event Action<T>` delegates; other systems subscribe directly
- **Pros**: Fully type-safe; compile-time verification of payload types; IDE auto-complete for event signatures
- **Cons**: Creates direct compile-time references between systems, violating the event-driven decoupling principle (P2). System A must `using SystemB` to subscribe to B's events. This undermines the 5-layer architecture's isolation boundaries and makes hot-update more complex (both assemblies must be updated simultaneously).
- **Estimated Effort**: Lower initial setup, but increasing coupling cost over time
- **Rejection Reason**: Violates P2 (Event-Driven Decoupling) and creates cross-module compile-time dependencies

### Alternative 3: ScriptableObject Event Channels

- **Description**: Each event is a `ScriptableObject` asset. Systems reference the SO in the Inspector to fire or listen.
- **Pros**: Unity-native; visible in Inspector; designer-friendly; fully decoupled at compile time
- **Cons**: Each event requires an asset file (15+ systems × multiple events = significant asset proliferation); no integration with TEngine's existing event infrastructure; SO references don't survive HybridCLR assembly reload cleanly; additional indirection layer adds small but measurable overhead per dispatch
- **Estimated Effort**: 2-3x more setup than Alternative 1; ongoing asset management overhead
- **Rejection Reason**: Adds overhead and complexity without benefit over TEngine's built-in system; poor HybridCLR compatibility

### Alternative 4: MessagePipe / UniRx Observable Messaging

- **Description**: Use a third-party reactive messaging library (MessagePipe, UniRx, or R3) for strongly-typed pub/sub
- **Pros**: Fully type-safe; supports async; supports filtering and transformation; powerful for complex event flows
- **Cons**: Heavy dependency (MessagePipe ~50KB, UniRx ~200KB); learning curve; another abstraction layer on top of TEngine; must verify HybridCLR compatibility; over-engineered for this project's needs (most events are simple fire-and-forget)
- **Estimated Effort**: 1-2 weeks to integrate and verify HybridCLR compatibility
- **Rejection Reason**: TEngine already provides a sufficient event system; adding a reactive framework introduces unnecessary complexity and dependency risk for a project with simple messaging needs

## Consequences

### Positive

- **No ID collisions**: 100-ID ranges per system with centralized `EventId.cs` make collisions impossible
- **Clear ownership**: Each event's sender(s) and listener(s) are documented in XML comments, creating an explicit communication contract
- **Stack-based multi-sender locking**: Token protocol on `PuzzleLockAll`/`PuzzleUnlock` eliminates the cross-system unlock race condition
- **Allocation-free hot path**: Struct payloads with synchronous dispatch produce zero GC pressure on frequent events
- **Hot-updatable**: `EventId.cs` and payload classes live in the `GameLogic` assembly, so new events can be added via HybridCLR hot-update without a full app release
- **Debuggable**: Event ID ranges make it trivial to identify which system an event belongs to from its numeric value alone

### Negative

- **Manual registry maintenance**: `EventId.cs` must be updated manually; there is no auto-generation from GDD documents. Risk of drift between design docs and code.
- **Payload type agreement is by convention, not compiler**: Sender and receiver must agree on the payload struct type through documentation, not compile-time interface. A mismatch causes a runtime `InvalidCastException`.
- **Fallback pattern adds complexity**: If Sprint 0 reveals `GameEvent.Send` doesn't support typed payloads, the `EventPayloadRegistry` fallback introduces boxing, a global mutable dictionary, and thread-safety concerns.
- **Int IDs are not self-documenting**: A log message showing `GameEvent.Send(1203)` requires looking up `EventId.cs` to interpret. Mitigation: debug builds include an ID→name lookup for logging.

### Neutral

- The 100-ID-per-system allocation is generous for most systems (Audio uses 4 of 100 IDs) but could theoretically be exhausted for a complex system. Reallocation would require a new ADR.
- Synchronous dispatch is both a strength (predictable ordering within a single `Send` chain) and a constraint (long-running handlers block the entire event chain).

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| `GameEvent.Send` doesn't support typed payloads | MEDIUM | HIGH | Sprint 0 source verification; fallback `EventPayloadRegistry` pattern designed and ready to implement |
| Listener leak after scene transition | MEDIUM | MEDIUM | Mandatory `Evt_SceneUnloadBegin` cleanup; automated leak detection test in CI |
| Cascade depth exceeds 3, causing debugging difficulty | LOW | MEDIUM | Convention documented; code review flag on `GameEvent.Send` calls inside event handlers |
| Re-entrant `Send` (handler sends same event ID) | LOW | HIGH | Documented as FORBIDDEN; detectable via debug-build assertion in a thin `GameEvent.Send` wrapper |
| Token mismatch on lock/unlock (typo in token string) | LOW | MEDIUM | Predefined `LockToken` constants; code review; runtime warning log on unknown token |
| EventId.cs drift from GDD documents | MEDIUM | LOW | Validation criterion: all events in architecture Section 5.3 must map to `EventId` constants; automated check in CI |

## Performance Implications

| Metric | Expected | Budget | Notes |
|--------|----------|--------|-------|
| **Event dispatch latency** | < 0.05ms per Send | 16.67ms frame budget | O(n) where n = listeners, typically 1-3 per event |
| **Memory (EventId)** | ~0 bytes runtime | 1,500 MB mobile ceiling | Static `const int` fields compile to inline literals |
| **Memory (listener registry)** | ~2-4 KB | 1,500 MB mobile ceiling | Dictionary<int, List<Action>>; ~40 event IDs × ~2 listeners avg |
| **Memory (payload structs)** | 0 persistent | 1,500 MB mobile ceiling | Stack-allocated, freed on handler return |
| **GC allocation (struct payloads)** | 0 per dispatch | 0 target on hot path | Value-type payloads avoid heap allocation |
| **GC allocation (fallback registry)** | 1 boxing per dispatch | Acceptable if infrequent | Only relevant if fallback is activated |

## Migration Plan

This is a greenfield protocol — no existing event system needs migration. Implementation steps:

1. **Sprint 0: Verify `GameEvent.Send` payload API** — Read TEngine 6.0 source to confirm whether `Send` accepts typed payloads or only int params. Result determines primary vs. fallback path.
2. **Create `EventId.cs`** — Implement the full ID allocation table in `Assets/GameScripts/HotFix/GameLogic/Event/EventId.cs`
3. **Create `EventPayloads.cs`** — Define all payload structs (or `EventPayloadRegistry.cs` if fallback is needed)
4. **Create `LockToken.cs`** — Define multi-sender token constants
5. **Implement listener leak detection** — Debug-build utility that tracks Add/Remove calls and reports orphaned listeners on scene transition
6. **Build first event flow** — Input System → Object Interaction → Shadow Puzzle event chain as end-to-end validation
7. **Document verified API** — Update `docs/engine-reference/` with confirmed `GameEvent` method signatures

**Rollback plan**: If `GameEvent` proves fundamentally inadequate (neither typed payloads nor the static registry fallback are workable), escalate to Technical Director for re-evaluation. The fallback to ScriptableObject event channels (Alternative 3) is the next best option, requiring ~1 week to implement.

## Validation Criteria

- [ ] Sprint 0 spike: Confirm `GameEvent.Send` payload capabilities (typed payloads or int-only)
- [ ] No duplicate event IDs in `EventId.cs` (compile-time: duplicate `const int` values produce compiler warning; CI check for unique values)
- [ ] All events listed in architecture document Section 5.3 have corresponding `EventId` constants (automated cross-reference check)
- [ ] Listener leak test: after scene transition, no system holds orphaned listeners (automated test)
- [ ] Multi-sender lock/unlock token test: verify correct stack behavior — lock(A) → lock(B) → unlock(A) → still locked → unlock(B) → unlocked
- [ ] Multi-sender edge cases: duplicate lock(A) → warning log; unlock(unknown) → warning log; scene unload → force clear
- [ ] Cascade depth: no event chain exceeds depth 3 in any implemented system (code review + static analysis)
- [ ] Performance: event dispatch benchmark < 0.05ms per `Send` on target mobile device
- [ ] Event documentation: every `EventId` constant has XML doc comment with Sender(s), Listener(s), Payload type, and Cascade info

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| `design/gdd/game-concept.md` | Core | TR-concept-013: Event-driven communication via `GameEvent` | Formalizes the `GameEvent` protocol with ID allocation, payloads, lifecycle, and ordering rules |
| `design/gdd/object-interaction.md` | Object Interaction | TR-objint-014: Object transform events | `Evt_ObjectTransformChanged` (1100), `Evt_LightPositionChanged` (1101) |
| `design/gdd/object-interaction.md` | Object Interaction | TR-objint-015: Object selection events | `Evt_ObjectSelected` (1102), `Evt_ObjectDeselected` (1103) |
| `design/gdd/object-interaction.md` | Object Interaction | TR-objint-016: Lock/unlock interaction events | `Evt_PuzzleLockAll` (1205), `Evt_PuzzleUnlock` (1206) with token-based multi-sender protocol |
| `design/gdd/scene-management.md` | Scene | TR-scene-014: 8 scene transition event IDs | `Evt_SceneTransitionBegin` (1400) through `Evt_SceneLoadFailed` (1407) — 8 IDs covering full transition lifecycle |
| `design/gdd/narrative-event-system.md` | Narrative | TR-narr-004: PuzzleLockAll/Unlock events from Narrative | Narrative is a legal sender for `Evt_PuzzleLockAll`/`Evt_PuzzleUnlock` with token `"narrative"` |
| `design/gdd/shadow-puzzle-system.md` | Shadow Puzzle | Puzzle completion event chain | `Evt_PerfectMatch` → `Evt_PuzzleComplete` → `Evt_ChapterComplete` cascade documented |
| `design/gdd/hint-system.md` | Hint | Hint availability notification | `Evt_HintAvailable` (1700), `Evt_HintDismissed` (1701) |
| `design/gdd/audio-system.md` | Audio | Audio ducking and playback requests | `Evt_AudioDuckingRequest` (1600), `Evt_AudioDuckingRelease` (1601), `Evt_PlaySFXRequest` (1602), `Evt_PlayMusicRequest` (1603) |
| `design/gdd/tutorial-onboarding.md` | Tutorial | Tutorial step tracking events | `Evt_TutorialStepStarted` (1800), `Evt_TutorialStepCompleted` (1801) |
| `design/gdd/settings-accessibility.md` | Settings | Settings change notification | `Evt_SettingChanged` (2000) |
| `design/gdd/chapter-state-and-save.md` | Chapter/Save | Save completion notification | `Evt_SaveComplete` (1900) |

## Related

- **Depends On**: ADR-001 (TEngine 6.0 Framework) — `GameEvent` system adopted there; this ADR extends with formal protocol
- **Enables**: ADR-016 (Narrative Sequence Engine) — Narrative system subscribes to puzzle events and sends lock/unlock tokens per this protocol
- **Enables**: ADR-024 (Analytics Telemetry) — Analytics system listens to key gameplay events defined here for telemetry recording
- **Updates**: ADR-001 Section "Event IDs" — The preliminary `EventId` example in ADR-001 is superseded by the complete allocation table in this ADR
- **References**: Architecture Document Section 5.3 — Master event list that this ADR's `EventId` class must match
- **References**: `src/MyGame/ShadowGame/design/gdd/systems-index.md` — Systems Index defining the 15 systems that communicate via this protocol
