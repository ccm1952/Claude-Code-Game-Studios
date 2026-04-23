// 该文件由Cursor 自动生成

# Epic: Audio System

> **Layer**: Feature
> **GDD**: `design/gdd/audio-system.md`
> **Architecture Module**: AudioManager (via TEngine AudioModule)
> **Governing ADRs**: ADR-017 (Audio Mix Strategy)
> **Engine Risk**: MEDIUM
> **Status**: Ready
> **Stories**: 7 stories created

## Overview

Audio System 是影子回忆情绪体验的核心载体，管理 3 个混音层（Ambient 环境音 / SFX 音效 / Music 背景音乐）的播放、混音和生命周期。系统基于 TEngine AudioModule 构建，扩展支持 4 因子音量公式（masterVolume × layerVolume × duckRatio × fadeMultiplier）、SFX 变体 + 音高随机化、Music crossfade（1-5s）、Ducking 系统和每层独立的 AudioSource 池管理。

系统响应 `AudioDuckingRequest`（来自 Narrative）、`SceneLoadComplete`（场景 BGM 切换）、`SceneTransitionBegin`（淡出音乐）等 GameEvent 事件。SFX 支持 3D 空间音频定位和 maxConcurrent 并发限制（超出时淘汰最旧实例）。所有 SFX 配置通过 Luban 表驱动。App pause/resume 时精确保存/恢复音频播放位置。PauseMenu 打开时 Music 继续播放（忽略 TimeScale）。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-017: Audio Mix Strategy | 3 层混音；4 因子音量公式；SFX 池 + 变体；Music crossfade；Ducking 系统；Luban 配置驱动 | MEDIUM |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|:------------:|
| TR-audio-001 | 3 mix layers (Ambient/SFX/Music) | ADR-017 ✅ |
| TR-audio-002 | Volume formula (4 multipliers) | ADR-017 ⚠️ |
| TR-audio-003 | SFX variant + pitch randomization | ADR-017 ⚠️ |
| TR-audio-004 | 3D spatial audio for SFX | ADR-017 ⚠️ |
| TR-audio-005 | maxConcurrent per SFX + oldest cull | ADR-017 ⚠️ |
| TR-audio-006 | Music crossfade 1-5s | ADR-017 ✅ |
| TR-audio-007 | Ducking system | ADR-017, ADR-006 ✅ |
| TR-audio-008 | SFX latency ≤ 1 frame | ADR-017, ADR-003 ⚠️ |
| TR-audio-009 | Ambient starts within 2s | ADR-017 ⚠️ |
| TR-audio-010 | Ambient occasional sounds | ADR-017 ⚠️ |
| TR-audio-011 | Audio CPU < 1ms with 10 sources | ADR-017, ADR-003 ⚠️ |
| TR-audio-012 | Audio memory < 30MB | ADR-017, ADR-003 ✅ |
| TR-audio-013 | App pause/resume audio state | ADR-017 ⚠️ |
| TR-audio-014 | Music continues during PauseMenu | ADR-017 ⚠️ |
| TR-audio-015 | All SFX config from Luban | ADR-017, ADR-007 ✅ |

## Sprint 0 Findings Impact

None — Audio System builds on TEngine AudioModule which is well-documented. No Sprint 0 spikes directly affect this epic.

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from the GDD are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs in `production/qa/evidence/`

## Dependencies

None — Audio System is an independent service infrastructure with no upstream epic dependencies. It is consumed by Narrative Event, Scene Management, and Settings.

## Stories

| Story ID | Title | Type | GDD Requirements | Status |
|----------|-------|------|-----------------|--------|
| audio-system-001 | AudioManager Initialization via TEngine AudioModule | Logic | TR-audio-001, TR-audio-002 | Ready |
| audio-system-002 | Ambient Sound Layer (Per-Chapter Environmental Audio) | Logic | TR-audio-001, TR-audio-009, TR-audio-010 | Ready |
| audio-system-003 | SFX Layer (Interaction Feedback, Puzzle Events) | Logic | TR-audio-003, TR-audio-004, TR-audio-005, TR-audio-008 | Ready |
| audio-system-004 | Music Layer (BGM Per-Chapter, Crossfade on Transition) | Logic | TR-audio-006, TR-audio-013, TR-audio-014 | Ready |
| audio-system-005 | Audio Ducking System for Narrative Sequences | Logic | TR-audio-007 | Ready |
| audio-system-006 | Luban TbAudioEvent Config Table Integration | Config/Data | TR-audio-015, TR-audio-003 | Ready |
| audio-system-007 | Runtime Volume Control Per Layer (Settings Integration) | Integration | TR-audio-002, TR-audio-011, TR-audio-012 | Blocked (ui-system-007) |

## Next Step

Run `/dev-story audio-system-001` to begin implementation. Stories 001–006 are independent; story-007 is blocked on ui-system-007 (SettingsPanel).
