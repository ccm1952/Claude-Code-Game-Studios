// 该文件由Cursor 自动生成

# ADR-018: Performance Monitoring & Auto-Degradation Strategy

## Status

Proposed

## Date

2026-04-22

## Last Verified

2026-04-22

## Decision Makers

Technical Director, Lead Programmer, Performance Analyst

## Summary

The project needs a mechanism to enforce performance budgets at runtime and automatically degrade visual quality when budgets are exceeded. We decide on a **centralized PerformanceMonitor module** (rather than per-system self-management) that tracks frame time, shadow render time, and ShadowRT readback time, and executes a tiered auto-degradation strategy: 5 consecutive frames >20ms triggers shadow quality tier drop; 60 consecutive frames <12ms triggers recovery attempt. The monitor is a cross-cutting concern in the Core Layer, consuming data from multiple Foundation/Feature systems.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.62f2 (LTS) |
| **Domain** | Core / Performance / Cross-Cutting |
| **Knowledge Risk** | LOW (Unity Profiler APIs, FrameTiming) |
| **References Consulted** | `urp-shadow-rendering.md`, `architecture.md` §5.1/§7.4/§9 (Open Question #10) |
| **Post-Cutoff APIs Used** | TEngine `GameEvent` |
| **Verification Required** | Sprint 0: confirm `FrameTimingManager` availability on target devices; validate `UniversalRenderPipelineAsset` runtime quality switching on mobile |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-002 (URP Shadow Rendering — the primary degradation target; provides shadow quality tier API) |
| **Enables** | N/A (cross-cutting infrastructure; all systems benefit) |
| **Blocks** | Production performance stability — without this, frame drops persist until manual intervention |
| **Ordering Note** | ADR-002 must be Accepted; this ADR should reach Accepted before Alpha milestone |

## Context

### Problem Statement

Architecture.md §7.4 identifies a coverage gap: "performance budgets as enforcement mechanism — 本文定义了预算数值，但未定义性能监控和自动降级的架构机制." The key architectural question (Open Question #10) is:

> 性能自动降级是由 URP Shadow Rendering 自行管理，还是由一个全局 Performance Monitor 模块统一管理所有系统的降级？

The answer must balance:

1. **Visibility**: Performance issues are cross-cutting — a frame drop might be caused by shadow rendering, audio, particle effects, or a combination. A per-system approach can only see its own contribution.
2. **Coordination**: If shadow rendering degrades independently and audio degrades independently, their combined effect might over-degrade (system looks worse than necessary) or under-degrade (each system thinks it's fine while combined load exceeds budget).
3. **Simplicity**: For a 5-chapter indie game, the degradation strategy should be straightforward and predictable.

### Constraints

- **Performance budgets** (from Architecture §5.1):
  - Total gameplay systems per frame: < 5.5ms
  - Input gesture recognition: < 0.5ms
  - Object Interaction Update: < 1ms
  - Shadow match calculation: < 2ms
  - ShadowRT CPU readback: ≤ 1.5ms
  - Hint Update: < 0.5ms
  - Total frame time target: 16.67ms (60fps)
- **Auto-degradation trigger** (from urp-shadow-rendering.md TR-render-016): 5 consecutive frames > 20ms → drop shadow quality tier
- **Recovery** (from urp-shadow-rendering.md): 60 consecutive frames < 12ms → attempt quality upgrade
- **Player perception**: Degradation should be imperceptible — "玩家不可感知的品质降低"
- **No UI notification for minor degradation**: Only show toast for major tier drops (e.g., Medium → Low)

### Requirements

- TR-render-016: Auto-degradation on sustained frame drops
- TR-concept-002/003/004: Performance budgets enforced
- TR-render-017/018/019: Shadow-specific performance budgets
- Architecture §7.4: Gap — performance monitoring mechanism needed

## Decision

**Implement a centralized PerformanceMonitor as a Core Layer module that observes frame timing across all systems and orchestrates degradation/recovery through the URP Shadow Rendering quality tier API.**

### Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                    PerformanceMonitor (Core Layer)                 │
│                                                                    │
│  ┌────────────────────────────────────────────────────┐          │
│  │              Frame Time Tracker                     │          │
│  │                                                      │          │
│  │  Samples: frameTime, shadowRenderTime, readbackTime  │          │
│  │  Ring buffer: last 60 frames                         │          │
│  │                                                      │          │
│  │  Degradation check:                                  │          │
│  │    if consecutiveOverBudget >= 5:                    │          │
│  │      RequestDegradation()                            │          │
│  │                                                      │          │
│  │  Recovery check:                                     │          │
│  │    if consecutiveUnderTarget >= 60:                  │          │
│  │      RequestRecovery()                               │          │
│  └──────────────────────┬─────────────────────────────┘          │
│                         │                                          │
│  ┌──────────────────────┴─────────────────────────────┐          │
│  │           Degradation Executor                      │          │
│  │                                                      │          │
│  │  Level 0 (Normal):  Current quality tier             │          │
│  │  Level 1 (Mild):    Disable lowest-priority shadow   │          │
│  │                      caster lights                   │          │
│  │  Level 2 (Moderate): Drop shadow quality one tier    │          │
│  │                      (High→Medium or Medium→Low)     │          │
│  │  Level 3 (Severe):  Reduce ShadowRT readback freq   │          │
│  │                      to every-other-frame            │          │
│  │  Level 4 (Critical):Drop to Low tier + reduce        │          │
│  │                      additional light shadows        │          │
│  └──────────────────────────────────────────────────────┘          │
│                                                                    │
│  Monitors (read-only):          Actuates:                          │
│  - Time.deltaTime               - URP Shadow: SetQualityTier()    │
│  - FrameTimingManager           - URP Shadow: SetMaxShadowLights()│
│  - Custom profiler markers      - ShadowRT: SetReadbackFrequency()│
└──────────────────────────────────────────────────────────────────┘
```

### Key Interfaces

```csharp
public enum DegradationLevel { Normal, Mild, Moderate, Severe, Critical }

public interface IPerformanceMonitor
{
    DegradationLevel CurrentLevel { get; }
    float AverageFrameTime { get; }       // rolling 60-frame average
    float PeakFrameTime { get; }          // max in last 60 frames
    bool IsDegraded { get; }

    // Debug
    PerformanceSnapshot GetSnapshot();
}

public struct PerformanceSnapshot
{
    public float FrameTime;
    public float ShadowRenderTime;
    public float ReadbackTime;
    public float MatchCalcTime;
    public DegradationLevel Level;
    public int ConsecutiveOverBudget;
    public int ConsecutiveUnderTarget;
}
```

### Frame Time Monitoring

```csharp
// Per frame (in LateUpdate or end-of-frame callback):
float frameTime = Time.unscaledDeltaTime * 1000f;  // ms

if (frameTime > degradationThresholdMs)  // default: 20ms
    consecutiveOverBudget++;
else
    consecutiveOverBudget = 0;

if (frameTime < recoveryThresholdMs)  // default: 12ms
    consecutiveUnderTarget++;
else
    consecutiveUnderTarget = 0;

if (consecutiveOverBudget >= degradationFrameCount)  // default: 5
{
    ExecuteDegradation();
    consecutiveOverBudget = 0;
}

if (consecutiveUnderTarget >= recoveryFrameCount)  // default: 60
{
    AttemptRecovery();
    consecutiveUnderTarget = 0;
}
```

### Degradation Cascade

When degradation is triggered, the monitor applies fixes in escalating order, one level per trigger:

| Level | Trigger | Action | Expected Savings | Player Perception |
|-------|---------|--------|-----------------|-------------------|
| **Level 1 (Mild)** | 5 frames > 20ms | Disable `ShadowCasterPriority.Ambient` lights | 1-3ms | Invisible — ambient light shadows are decorative |
| **Level 2 (Moderate)** | 5 more frames > 20ms at Level 1 | Drop shadow quality tier (e.g., High→Medium) | 3-5ms | Subtle — shadow edge slightly softer |
| **Level 3 (Severe)** | 5 more frames > 20ms at Level 2 | Reduce ShadowRT readback to every-other-frame | 0.5-1.5ms | Match scoring ~1 frame more latent |
| **Level 4 (Critical)** | 5 more frames > 20ms at Level 3 | Force Low quality tier + max 1 shadow light | 5-8ms | Noticeable but playable — display toast |

### Recovery Strategy

Recovery attempts to reverse degradation one level at a time, with a **verification window**:

```
On AttemptRecovery():
    if currentLevel > Normal:
        currentLevel -= 1
        Apply(currentLevel)
        // Start verification: if next 30 frames stay < 14ms, keep; else revert
        startVerification(30 frames, 14ms threshold)
```

If the verification window fails (frame time exceeds 14ms during the 30-frame window), the system reverts to the previous degradation level and doubles the recovery frame requirement (120 frames instead of 60) before the next attempt. This prevents oscillation.

### Configuration Parameters

| Parameter | Default | Source | Range |
|-----------|---------|--------|-------|
| degradationThresholdMs | 20.0 | config | 16-33 |
| recoveryThresholdMs | 12.0 | config | 8-14 |
| degradationFrameCount | 5 | config | 3-10 |
| recoveryFrameCount | 60 | config | 30-120 |
| recoveryVerificationFrames | 30 | config | 15-60 |
| recoveryVerificationThreshold | 14.0ms | config | 10-16 |
| maxDegradationLevel | Critical | config | Mild-Critical |

### Performance Budget Dashboard (Debug Only)

In development builds, expose a debug overlay showing:
- Current frame time (ms) with color coding (green < 14ms, yellow 14-20ms, red > 20ms)
- Current degradation level
- Shadow quality tier
- Active shadow light count
- ShadowRT readback frequency
- Per-system budget usage (from custom profiler markers)

This overlay is stripped from release builds.

## Alternatives Considered

### Alternative 1: Per-System Self-Management (Decentralized)

- **Description**: Each system (URP Shadow, Narrative, Hint) monitors its own performance and self-degrades when exceeding its local budget
- **Pros**: Systems are self-contained; no central coordinator needed; each system knows best how to degrade itself
- **Cons**: No global visibility — System A might degrade while the actual bottleneck is System B; combined degradation could over-correct; systems can't coordinate (shadow degradation might make match calculation faster, making its own degradation unnecessary); harder to debug cross-system performance issues
- **Rejection Reason**: Performance is a cross-cutting concern. A 20ms frame might be 5ms shadow + 5ms match + 10ms scene loading — no single system sees the full picture. Centralized monitoring with system-specific actuators is the standard industry approach.

### Alternative 2: Static Quality Selection Only (No Runtime Degradation)

- **Description**: Detect device capability at startup, select a quality tier (Low/Medium/High), and never change it during gameplay
- **Pros**: Predictable behavior; no runtime overhead; simpler implementation; no risk of degradation artifacts
- **Cons**: Cannot handle transient load spikes (e.g., complex puzzle with 5+ objects + multiple lights); over-classifies devices (a "Medium" device might handle High for simple puzzles but need Low for complex ones); static selection can't adapt to thermal throttling (very common on mobile — device runs fine for 5 min then GPU clocks down)
- **Rejection Reason**: Mobile thermal throttling is unpredictable and significant — a device that benchmarks as "High" can drop to "Low" performance after 10 minutes of sustained load. Dynamic degradation is essential for consistent 60fps on mobile.

### Alternative 3: Target Frame Rate Adjustment (Drop to 30fps)

- **Description**: Instead of degrading visual quality, drop `Application.targetFrameRate` from 60 to 30 when performance is insufficient
- **Pros**: Preserves visual quality; simple implementation; immediate and guaranteed to reduce GPU load by half
- **Cons**: 30fps makes drag-to-follow feel sluggish (GDD requires 16ms response — only achievable at 60fps); doubles input latency; players notice framerate drops much more than shadow quality changes; this is a game where "跟手" is a core feel requirement
- **Rejection Reason**: The game's core interaction (object drag) requires 60fps for the "手指到哪物件到哪" feel specified in the GDD. Dropping to 30fps would violate the most critical game feel requirement. Visual quality degradation is invisible by comparison.

## Consequences

### Positive

- **Global visibility**: Single monitor sees all systems' contributions to frame time — accurate root cause identification
- **Graceful degradation**: 5-level cascade ensures minimal perceptible quality loss (starts with invisible ambient shadow removal)
- **Anti-oscillation**: Recovery verification window prevents rapid quality flip-flopping
- **Thermal adaptation**: Handles mobile thermal throttling that static quality selection cannot
- **Resolves Open Question #10**: Definitively answers the centralized vs. per-system architecture question

### Negative

- **Central point of failure**: If PerformanceMonitor has a bug, all quality management is affected. Mitigation: fail-safe — if monitor crashes, systems remain at current quality level (no degradation, no recovery)
- **Coupling to URP Shadow APIs**: Degradation executor directly calls shadow quality tier API — changes to URP Shadow system require corresponding updates here
- **Debug overhead**: Performance monitoring itself costs CPU (~0.05ms per frame). On extremely constrained devices, this overhead is meaningful.

### Neutral

- The monitor only actuates shadow-related degradation in the current design. Future systems (particle effects, post-processing) can be added as additional degradation targets without architectural changes.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| `FrameTimingManager` not available on all target devices | MEDIUM | LOW | Fallback to `Time.unscaledDeltaTime` (less precise but functional) |
| URP quality tier runtime switching causes visual glitch | LOW | MEDIUM | Test on all target devices; apply change at frame boundary; consider 1-frame fade |
| Recovery oscillation (degrade → recover → degrade rapidly) | MEDIUM | MEDIUM | Exponential backoff on recovery: double recoveryFrameCount after failed verification |
| PerformanceMonitor overhead itself causes frame drops on ultra-low-end | LOW | LOW | Monitor code is ~0.05ms; can be disabled via config for profiling |
| Custom profiler markers not available in release builds | LOW | LOW | Use `Time.unscaledDeltaTime` for frame time; per-system timing only in development |

## Performance Implications

| Metric | Expected | Budget | Notes |
|--------|----------|--------|-------|
| CPU (monitor per frame) | 0.03-0.05ms | < 0.1ms | Ring buffer update + threshold checks |
| Memory (60-frame ring buffer) | ~2KB | Negligible | 60 × PerformanceSnapshot struct |
| Latency (degradation response) | 5 frames (83ms) | Acceptable | 5 consecutive bad frames before action |
| Latency (recovery response) | 60 frames (1s) + 30 verification | Acceptable | Conservative recovery prevents oscillation |

## Validation Criteria

- [ ] 5 consecutive frames > 20ms (simulated load) triggers Level 1 degradation within 1 frame of 5th bad frame
- [ ] 60 consecutive frames < 12ms triggers recovery attempt; verification window validates improvement
- [ ] Recovery failure (verification exceeds 14ms) correctly reverts to previous level
- [ ] Degradation cascade: Level 1 → 2 → 3 → 4 exercised in test with increasing artificial load
- [ ] Level 4 (Critical) displays toast notification via UI System
- [ ] Recovery exponential backoff: after failed recovery, next attempt requires 120 frames (doubled)
- [ ] Monitor disabled via config flag — system holds at current quality without monitoring overhead
- [ ] No oscillation: 10-minute sustained load test shows stable degradation level (no flip-flopping)
- [ ] Thermal throttling simulation: sustained full-quality rendering for 15 min on iPhone 13 Mini → monitor correctly detects and degrades
- [ ] Debug overlay shows accurate real-time data in development builds; stripped from release
- [ ] URP quality tier switch executes without visual pop or single-frame artifact

## GDD Requirements Addressed

| GDD Document | Requirement | How This ADR Satisfies It |
|-------------|-------------|--------------------------|
| `urp-shadow-rendering.md` | TR-render-016: 5 consecutive frames > 20ms → auto-degrade | Exact trigger: `consecutiveOverBudget >= 5` |
| `urp-shadow-rendering.md` | Degraded state: auto-lower Shadow Map resolution | Level 2: drop shadow quality tier |
| `urp-shadow-rendering.md` | Recovery: frame time recovers → attempt upgrade | 60 frames < 12ms → AttemptRecovery() with verification |
| `urp-shadow-rendering.md` | "玩家不可感知的品质降低" | Level 1 removes only ambient shadows; Level 2 subtle edge softening |
| `architecture.md` §5.1 | Performance budgets: total < 5.5ms gameplay, 16.67ms frame | PerformanceMonitor enforces total frame budget |
| `architecture.md` §7.4 | Gap: performance monitoring mechanism needed | This ADR fills the identified gap |
| `architecture.md` §9 Q#10 | Centralized vs per-system performance management | Decision: Centralized PerformanceMonitor |

## Related

- **Depends On**: ADR-002 (URP Shadow Rendering) — primary degradation target; shadow quality tier API
- **Resolves**: Architecture.md §9 Open Question #10
- **Fills**: Architecture.md §7.4 Coverage Gap — "performance budgets as enforcement mechanism"
- **References**: `urp-shadow-rendering.md` (quality tiers, degradation thresholds, TR-render-016)
- **References**: `architecture.md` §5.1 (performance budget numbers)
