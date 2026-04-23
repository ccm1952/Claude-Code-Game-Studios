// 该文件由Cursor 自动生成

# Epic: Chapter State

> **Layer**: Core
> **GDD**: `design/gdd/chapter-state-and-save.md` (chapter state portion)
> **Architecture Module**: ChapterStateManager (progression, puzzle ordering)
> **Governing ADRs**: ADR-007 (Luban Config), ADR-006 (GameEvent)
> **Engine Risk**: MEDIUM
> **Status**: Ready
> **Stories**: 5 stories created

## Overview

Chapter State 是影子回忆全局进度的唯一权威来源（Single Source of Truth），负责管理 5 个章节的解锁状态、每章内谜题的顺序推进、当前活跃章节/谜题的运行时数据。系统在启动时从 Save System 加载持久化数据初始化 `ChapterProgress[]` 和 `PuzzleState[]`，运行时响应 `PuzzleCompleteEvent` / `AbsenceAcceptedEvent` 更新进度，并通过 GameEvent 广播 `ChapterCompleteEvent`、`PuzzleStateChanged`、`RequestSceneChange` 等事件。

所有章节和谜题的配置数据（解锁条件、谜题排列、难度参数）通过 Luban 配置表 `TbChapter` 和 `TbPuzzle` 驱动。系统定义 `IChapterProgress` 数据接口供 Save System 序列化，实现与持久化层的解耦。谜题完成状态不可逆转（immutable completion flag）。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-007: Luban Config Access | 通过 Luban Tables 单例只读访问 `TbChapter` / `TbPuzzle` 配置；主线程安全 | MEDIUM |
| ADR-006: GameEvent Protocol | int-based 事件通信；章节/谜题状态变更事件广播 | MEDIUM |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|:------------:|
| TR-save-001 | 5 chapters sequential unlock | ADR-008 ✅ |
| TR-save-002 | Linear puzzle progression | ADR-008 ✅ |
| TR-save-003 | Puzzle completion irreversible | ADR-008 ✅ |
| TR-save-004 | Chapter State is single authority | ADR-008 ✅ |
| TR-save-005 | IChapterProgress interface decoupling | ADR-008 ✅ |
| TR-save-016 | Replay mode for completed chapters | arch.md ⚠️ |
| TR-save-017 | Luban config tables (TbChapter, TbPuzzle) | ADR-008, ADR-007 ✅ |

## Sprint 0 Findings Impact

- **SP-001 (GameEvent Payload)**: 已确认 struct payload 支持，`PuzzleStateChanged(int puzzleId, PuzzleState newState)` 等事件方案有效。
- **SP-004 (Luban Thread Safety)**: 已确认 Luban Tables 在 UniTask 主线程续接中只读访问安全，无需额外同步锁。禁止在 `UniTask.Run` 中读表。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from the GDD are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs in `production/qa/evidence/`

## Dependencies

- **save-system**: 启动时从 SaveManager 加载持久化数据；运行时通过 `IChapterProgress` 接口将数据交给 Save System 序列化

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | Chapter Data Model + Luban TbChapter Integration | Logic | Ready | ADR-007 |
| 002 | Puzzle Unlock/Ordering Within a Chapter | Logic | Ready | ADR-007, ADR-008 |
| 003 | Chapter Completion → Next Chapter Unlock Flow | Logic | Ready | ADR-007, ADR-006 |
| 004 | GameEvent Dispatch for Chapter/Puzzle State Changes | Integration | Ready | ADR-006 |
| 005 | Bidirectional Integration with Save System (IChapterProgress) | Integration | Ready | ADR-008, ADR-006 |

## Next Step

Run `/story-readiness story-001-chapter-data-model` → `/dev-story` to begin implementation. Work through stories in order — each story's `Depends on:` field tells you what must be DONE before starting it.
