// 该文件由Cursor 自动生成

# Epic: Save System

> **Layer**: Core
> **GDD**: `design/gdd/chapter-state-and-save.md` (save portion)
> **Architecture Module**: SaveManager (JSON + CRC32, backup chain, migration)
> **Governing ADRs**: ADR-008 (Save System)
> **Engine Risk**: LOW
> **Status**: Ready
> **Stories**: 6 stories created

## Overview

Save System 负责影子回忆所有玩家进度数据的持久化存储，采用单存档槽 JSON 格式。系统实现原子写入（temp → rename）确保写入安全，CRC32 校验验证数据完整性，`.backup.json` 备份文件支持损坏恢复（Primary → Backup → New Game 回退链）。所有 I/O 操作通过 UniTask 异步执行，零主线程阻塞。

系统提供 5 种自动保存触发条件，支持 1 秒 debounce 防止频繁写入（Pause/Quit 时绕过 debounce 立即保存）。Save 文件 < 10KB，Save < 50ms，Load < 100ms。存档 schema 包含版本号，通过 migration chain 函数支持跨版本升级。设置数据独立存储于 PlayerPrefs，不进入 save JSON。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-008: Save System | 单槽 JSON + CRC32；原子写入；备份回退链；5 种自动保存触发；版本迁移；UniTask 异步 I/O | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|:------------:|
| TR-save-006 | Auto-save triggers (5 conditions) | ADR-008 ✅ |
| TR-save-007 | Save debounce 1s (except force-save) | ADR-008 ✅ |
| TR-save-008 | UniTask async I/O | ADR-008 ✅ |
| TR-save-009 | Single slot JSON format | ADR-008 ✅ |
| TR-save-010 | Atomic write (temp → rename) | ADR-008 ✅ |
| TR-save-011 | Backup .backup.json | ADR-008 ✅ |
| TR-save-012 | CRC32 checksum | ADR-008 ✅ |
| TR-save-013 | Version migration chain | ADR-008 ✅ |
| TR-save-014 | Save file size < 10KB | ADR-008 ✅ |
| TR-save-015 | Save < 50ms, Load < 100ms | ADR-008 ✅ |
| TR-save-018 | Save init before gameplay systems | ADR-008 ✅ |
| TR-save-019 | Pause/Quit immediate save | ADR-008 ✅ |
| TR-save-020 | Corrupted save fallback chain | ADR-008 ✅ |

## Sprint 0 Findings Impact

None — Save System uses standard `System.IO` and `PlayerPrefs` APIs with no identified Sprint 0 technical risks.

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from the GDD are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs in `production/qa/evidence/`

## Dependencies

None — Save System is a low-level persistence service. It consumes `IChapterProgress` data from Chapter State, but does not depend on Chapter State's implementation to function (interface-driven decoupling per ADR-008).

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | Save Data Schema (JSON + Versioned Format) | Logic | Ready | ADR-008 |
| 002 | Atomic Write + CRC32 Checksum + Backup File | Logic | Ready | ADR-008 |
| 003 | Load Fallback Chain (Primary → Backup → Fresh Start) | Logic | Ready | ADR-008 |
| 004 | Auto-Save Triggers (5 Conditions + 1s Debounce) | Integration | Ready | ADR-008 |
| 005 | ISaveMigration Chain (v1→v2→v3→...) | Logic | Ready | ADR-008 |
| 006 | Corruption Detection + Recovery Flow | Logic | Ready | ADR-008 |

## Next Step

Run `/story-readiness story-001-save-data-schema` → `/dev-story` to begin implementation. Work through stories in order — each story's `Depends on:` field tells you what must be DONE before starting it.
