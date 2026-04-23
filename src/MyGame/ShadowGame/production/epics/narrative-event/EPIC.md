// 该文件由Cursor 自动生成

# Epic: Narrative Event System

> **Layer**: Feature
> **GDD**: `design/gdd/narrative-event-system.md`
> **Architecture Module**: NarrativeSequenceEngine (sequence playback, chain logic)
> **Governing ADRs**: ADR-016 (Narrative Sequence Engine)
> **Engine Risk**: MEDIUM
> **Status**: Ready
> **Stories**: 8 stories created

## Stories

| Story | Title | Type | Status | Estimate |
|-------|-------|------|--------|----------|
| [story-001](story-001-sequence-engine.md) | Sequence Engine (Core Playback) | Logic | Ready | 3d |
| [story-002](story-002-atomic-effects.md) | Atomic Effects (10 types) | Logic | Ready | 3d |
| [story-003](story-003-sequence-chain.md) | Sequence Chain (nextSequenceId) | Integration | Ready | 1d |
| [story-004](story-004-luban-sequence-config.md) | Luban Sequence Config | Config/Data | Ready | 2d |
| [story-005](story-005-puzzle-to-narrative.md) | Puzzle to Narrative Trigger | Integration | Ready | 1d |
| [story-006](story-006-fullscreen-takeover.md) | Full-Screen Takeover (Input Block) | Integration | Ready | 1d |
| [story-007](story-007-chapter-transition.md) | Chapter Transition | Integration | Ready | 2d |
| [story-008](story-008-narrative-audio-sync.md) | Narrative Audio Sync | Visual/Feel | Ready | 2d |

## Overview

Narrative Event System 是影子回忆"克制表达"和"缺席比存在更有力"体验支柱的技术载体，负责将谜题完成和章节推进事件转化为记忆重现演出。系统定义 11 种原子效果类型（色温变化、物件动画、音频切换、屏幕淡入淡出、Timeline 播放等），通过配置表驱动的时间线排列实现演出序列的灵活编排。

系统响应 `PerfectMatchEvent` / `AbsenceAcceptedEvent` / `ChapterCompleteEvent` 事件，查询 Luban `TbNarrativeSequence` / `TbPuzzleNarrativeMap` / `TbChapterTransitionMap` 配置表获取对应序列 ID，按时间排序并行执行原子效果。播放期间通过 `PuzzleLockAllEvent` + `InputBlocker` 锁定交互。支持 Sequence Chain（SP-008 决策）实现末章合并序列无缝衔接。序列队列最多 3 个待处理（drop oldest）。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-016: Narrative Sequence Engine | 11 种原子效果；配置驱动序列编排；Sequence Chain 衔接；queue max 3；资源加载失败 skip + log | MEDIUM |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|:------------:|
| TR-narr-001 | 11 atomic effect types | ADR-016 ✅ |
| TR-narr-002 | Time-sorted parallel effects | ADR-016 ⚠️ |
| TR-narr-003 | Config-table driven sequences | ADR-016, ADR-007 ⚠️ |
| TR-narr-004 | PuzzleLockAll + InputBlocker | ADR-016, ADR-006, ADR-010 ✅ |
| TR-narr-005 | Absence puzzle Ch.5 sequence | ADR-016 ⚠️ |
| TR-narr-006 | Chapter transition Timeline | ADR-016, ADR-009 ⚠️ |
| TR-narr-007 | Chapter-final merged sequence | ADR-016 ⚠️ |
| TR-narr-008 | Queue max 3 pending | ADR-016 ⚠️ |
| TR-narr-009 | Resource load failure resilience | ADR-016 ⚠️ |
| TR-narr-010 | Timeline paths via config | ADR-016, ADR-007 ⚠️ |
| TR-narr-011 | TextureVideo 3-phase alpha | ADR-016 ⚠️ |
| TR-narr-012 | SequenceComplete event | ADR-006, ADR-016 ✅ |

## Sprint 0 Findings Impact

- **SP-001 (GameEvent Payload)**: 已确认 struct payload 支持，Narrative 事件的复杂 payload 方案有效。
- **SP-006 (PuzzleLockAll Token)**: Narrative 作为 PuzzleLockAll 的合法发送者之一，使用独立 token 标识，PopLock 时通过 HashSet 防护防止错误解锁。
- **SP-008 (Merged Sequence Schema)**: 决策采用 Sequence Chain 方案——末章合并序列通过 `nextSequenceId` 字段自动衔接两个独立 sequence，配置复用性高、策划可维护。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from the GDD are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs in `production/qa/evidence/`

## Dependencies

- **shadow-puzzle**: 响应 `PerfectMatchEvent` / `AbsenceAcceptedEvent` 触发记忆重现
- **chapter-state**: 响应 `ChapterCompleteEvent` 触发章节过渡序列
- **audio-system**: 调用 AudioManager 播放演出音频（crossfade、ducking）

## Next Step

Run `/story-readiness narrative-event/story-001-sequence-engine` to validate the first story before implementation begins.
