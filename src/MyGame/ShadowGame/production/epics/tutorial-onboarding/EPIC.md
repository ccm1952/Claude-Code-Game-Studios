// 该文件由Cursor 自动生成

# Epic: Tutorial & Onboarding

> **Layer**: Presentation
> **GDD**: `design/gdd/tutorial-onboarding.md`
> **Architecture Module**: TutorialController (step-based guidance)
> **Governing ADRs**: ADR-010 (Input/InputFilter)
> **Engine Risk**: LOW
> **Status**: In Progress
> **Stories**: 6 stories created

## Overview

Tutorial & Onboarding 系统负责首次游玩玩家的渐进式操作引导，通过 InputFilter 白名单机制限制每步可用手势，引导玩家依次学习 Tap 选取、Drag 拖拽、Rotate 旋转、Pinch 缩放和 LightDrag 光源操控。系统采用 step-based 结构，每步定义允许的手势集合、提示文本和完成条件。

教学步骤通过 Luban 配置表驱动（`TbTutorialStep`），不可用手势被 InputFilter 静默丢弃（无反馈）。教学期间 Hint System 定时器暂停。教学完成后 `tutorialCompleted` 标志写入 save data，不再触发。优先级排序：Narrative > Tutorial > Hint（架构文档定义的事件级联顺序）。Settings 中提供"操作指南"重播入口。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-010: Input Abstraction (InputFilter) | PushFilter/PopFilter 白名单机制；被过滤手势静默丢弃 | LOW |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|:------------:|
| TR-tutor-001 | TutorialStep config structure | arch.md ⚠️ |
| TR-tutor-002 | InputFilter integration | ADR-010 ✅ |
| TR-tutor-003 | Filtered gestures silent discard | ADR-010 ✅ |
| TR-tutor-004 | 5 tutorial steps | ❌ Deferred to ADR-019 (P2) |
| TR-tutor-005 | tutorialCompleted in save data | ADR-008 ✅ |
| TR-tutor-006 | Tutorial pauses hint timers | ADR-015 ✅ |
| TR-tutor-007 | Priority: Narrative > Tutorial > Hint | ADR-006 ✅ |
| TR-tutor-008 | Step prompt UI | ❌ Deferred to ADR-019 (P2) |
| TR-tutor-009 | Steps from Luban config | ADR-007 ⚠️ |
| TR-tutor-010 | Operation Guide replay in Settings | ❌ Deferred to ADR-019 (P2) |

## Sprint 0 Findings Impact

None — Tutorial system relies on established InputFilter infrastructure (ADR-010) with no identified technical risks.

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from the GDD are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs in `production/qa/evidence/`

## Dependencies

- **input-system**: 使用 InputFilter PushFilter/PopFilter 机制限制可用手势
- **hint-system**: 教学期间暂停 Hint 定时器
- **ui-system**: TutorialOverlay UIWindow 用于显示教学提示
- **chapter-state**: 查询当前谜题确定是否需要触发教学

## Stories

| # | Story | Type | Status | TR Coverage |
|---|-------|------|--------|-------------|
| 001 | [TutorialController — Step-Based State Machine](story-001-tutorial-controller.md) | Logic | Ready | TR-tutor-001, 002, 006, 007 |
| 002 | [Luban TbTutorialStep Config Table Integration](story-002-tutorial-steps-config.md) | Config/Data | Ready | TR-tutor-001, 009 |
| 003 | [InputFilter Whitelist Lock During Tutorial](story-003-input-filter-lock.md) | Logic | Ready | TR-tutor-002, 003 |
| 004 | [TutorialOverlay UIWindow](story-004-tutorial-overlay.md) | UI | Ready | TR-tutor-008 |
| 005 | [Tutorial Step Completion Detection + Auto-Advance](story-005-tutorial-progression.md) | Integration | Ready | TR-tutor-002, 004 |
| 006 | [Skip Tutorial + tutorialCompleted Save Flag](story-006-skip-tutorial.md) | Integration | Ready | TR-tutor-005, 010 |

## Next Step

Run `/dev-story tutorial-onboarding/story-001-tutorial-controller` to begin implementation.
Run `/story-readiness tutorial-onboarding/story-NNN-slug` to validate a story before starting.
