// 该文件由Cursor 自动生成

# Epics Index — 影子回忆 (Shadow Memory)

> **Last Updated**: 2026-04-22
> **Engine**: Unity 2022.3.62f2 LTS
> **Framework**: TEngine 6.0.0 + HybridCLR + YooAsset 2.3.17 + UniTask 2.5.10
> **Total TRs**: 212 (124 ✅ / 87 ⚠️ / 1 ❌)

## Epic Overview

| # | Epic | Layer | System | GDD | ADR Coverage | Engine Risk | Status |
|---|------|-------|--------|-----|:------------:|:-----------:|--------|
| 1 | [input-system](input-system/EPIC.md) | Foundation | Input System | input-system.md | ADR-010, ADR-003 | LOW | Ready |
| 2 | [urp-shadow-rendering](urp-shadow-rendering/EPIC.md) | Foundation | URP Shadow Rendering | urp-shadow-rendering.md | ADR-002, ADR-018 | MEDIUM | Ready |
| 3 | [scene-management](scene-management/EPIC.md) | Core | Scene Management | scene-management.md | ADR-009, ADR-005 | MEDIUM | Ready |
| 4 | [object-interaction](object-interaction/EPIC.md) | Core | Object Interaction | object-interaction.md | ADR-013, ADR-010 | LOW | Ready |
| 5 | [chapter-state](chapter-state/EPIC.md) | Core | Chapter State | chapter-state-and-save.md | ADR-007, ADR-006 | MEDIUM | Ready |
| 6 | [save-system](save-system/EPIC.md) | Core | Save System | chapter-state-and-save.md | ADR-008 | LOW | Ready |
| 7 | [shadow-puzzle](shadow-puzzle/EPIC.md) | Feature | Shadow Puzzle | shadow-puzzle-system.md | ADR-012, ADR-014, ADR-006 | LOW | Ready |
| 8 | [hint-system](hint-system/EPIC.md) | Feature | Hint System | hint-system.md | ADR-015 | LOW | Ready |
| 9 | [narrative-event](narrative-event/EPIC.md) | Feature | Narrative Event | narrative-event-system.md | ADR-016 | MEDIUM | Ready |
| 10 | [audio-system](audio-system/EPIC.md) | Feature | Audio System | audio-system.md | ADR-017 | MEDIUM | Ready |
| 11 | [ui-system](ui-system/EPIC.md) | Feature | UI System | ui-system.md | ADR-011 | MEDIUM | Ready |
| 12 | [tutorial-onboarding](tutorial-onboarding/EPIC.md) | Presentation | Tutorial & Onboarding | tutorial-onboarding.md | ADR-010 | LOW | Ready |
| 13 | [settings-accessibility](settings-accessibility/EPIC.md) | Presentation | Settings & Accessibility | settings-accessibility.md | ADR-008 | LOW | Ready |

## Layer Summary

| Layer | Epics | TR Coverage | MEDIUM+ Risk Epics |
|-------|:-----:|:-----------:|:------------------:|
| Foundation | 2 | 41 TRs | 1 (urp-shadow-rendering) |
| Core | 4 | 76 TRs | 2 (scene-management, chapter-state) |
| Feature | 5 | 80 TRs | 3 (narrative-event, audio-system, ui-system) |
| Presentation | 2 | 18 TRs | 0 |
| **Total** | **13** | **215 TR refs** | **6** |

## Dependency Graph

```
Foundation (no upstream dependencies)
├── input-system ←────────────────────────────────────┐
├── urp-shadow-rendering ←───────────────────────┐    │
                                                  │    │
Core (depends on Foundation)                      │    │
├── save-system (no epic deps)                    │    │
├── chapter-state → save-system                   │    │
├── scene-management → save-system, chapter-state │    │
├── object-interaction → input-system             │    │
                                                  │    │
Feature (depends on Core)                         │    │
├── shadow-puzzle → object-interaction, urp-shadow-rendering, chapter-state
├── hint-system → shadow-puzzle                        │
├── narrative-event → shadow-puzzle, chapter-state, audio-system
├── audio-system (no epic deps)                        │
├── ui-system → chapter-state, shadow-puzzle           │
                                                       │
Presentation (depends on Feature)                      │
├── tutorial-onboarding → input-system, hint-system, ui-system, chapter-state
├── settings-accessibility → save-system, ui-system, audio-system, input-system
```

## Sprint 0 Spike Impact Map

| Spike | Affected Epics | Status |
|-------|---------------|--------|
| SP-001 GameEvent Payload | input-system, object-interaction, chapter-state, narrative-event | ✅ Confirmed |
| SP-002 UIWindow Lifecycle | ui-system | ✅ Confirmed |
| SP-003 YooAsset Package | scene-management | ✅ Decided: Single package |
| SP-004 Luban Thread Safety | chapter-state, hint-system | ✅ Confirmed: Main-thread safe |
| SP-005 WallReceiver Shader | urp-shadow-rendering | ✅ Decided: Pure HLSL |
| SP-006 PuzzleLockAll Token | object-interaction, shadow-puzzle, narrative-event | ✅ Decided: HashSet token |
| SP-007 HybridCLR+AsyncGPU | urp-shadow-rendering, shadow-puzzle | ⏳ Needs device verification |
| SP-008 Merged Sequence | narrative-event | ✅ Decided: Sequence Chain |
| SP-009 I2 Localization | ui-system, settings-accessibility | ✅ Confirmed: TEngine wrapper |
| SP-010 Performance Monitor | urp-shadow-rendering | ✅ Decided: Global PerformanceMonitor |

## Excluded Systems (Not Yet Epics)

| System | Reason | Expected Phase |
|--------|--------|---------------|
| Collectible System | Not yet designed | Alpha |
| Analytics | Not yet designed | Alpha |

## Next Steps

1. Run `/create-stories [epic-slug]` for each epic to generate implementable story files
2. Resolve SP-007 (HybridCLR + AsyncGPUReadback) before implementing urp-shadow-rendering stories
3. Prioritize Foundation → Core → Feature → Presentation layer order for sprint planning
