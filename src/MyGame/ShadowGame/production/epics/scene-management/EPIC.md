// 该文件由Cursor 自动生成

# Epic: Scene Management

> **Layer**: Core
> **GDD**: `design/gdd/scene-management.md`
> **Architecture Module**: SceneTransitionManager (additive loading, 11-step flow)
> **Governing ADRs**: ADR-009 (Scene Lifecycle), ADR-005 (YooAsset Resource)
> **Engine Risk**: MEDIUM
> **Status**: Ready
> **Stories**: 6 stories created

## Overview

Scene Management 负责影子回忆的异步场景加载/卸载和过渡控制。系统采用 Additive Scene 架构（Boot + Main + Chapter 三层），通过 11 步标准化切换流程（FadeOut → Unload → GC → Load → FadeIn）确保场景过渡的视觉连续性和资源安全性。最多 3 个场景同时驻留内存。

系统基于 TEngine ResourceModule + SceneModule 和 YooAsset SceneHandle 实现，响应 `RequestSceneChange` GameEvent 触发场景切换。切换过程互斥（max queue = 1），支持 2 次重试 + 回退至 MainMenu 的容错机制。Fade overlay 在 60fps 下独立于加载线程运行。所有场景切换事件通过 GameEvent 广播（EventId 1400-1407），供 Audio、UI 等系统响应。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-009: Scene Lifecycle | Additive 场景架构；11 步切换流程；互斥队列；错误恢复机制；SceneHandle 引用追踪 | MEDIUM |
| ADR-005: YooAsset Resource | 单包策略（SP-003 决策）；异步加载强制；Load/Unload 配对防泄漏 | MEDIUM |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|:------------:|
| TR-scene-001 | Additive scene architecture | ADR-009 ✅ |
| TR-scene-002 | Max 3 scenes in memory | ADR-009 ✅ |
| TR-scene-003 | Async scene loading (UniTask) | ADR-009 ✅ |
| TR-scene-004 | Always LoadSceneMode.Additive | ADR-009 ✅ |
| TR-scene-005 | Transition flow (FadeOut→Unload→GC→Load→FadeIn) | ADR-009 ✅ |
| TR-scene-006 | Transition mutual exclusion | ADR-009 ✅ |
| TR-scene-007 | YooAsset on-demand download | ADR-009, ADR-005 ✅ |
| TR-scene-008 | Chapter scene memory ~1000MB | ADR-009 ⚠️ |
| TR-scene-009 | Cached scene load < 1s | ADR-009 ⚠️ |
| TR-scene-010 | Fade at 60fps during loading | ADR-009 ✅ |
| TR-scene-011 | Memory leak detection (5-cycle test) | ADR-009 ✅ |
| TR-scene-012 | Error recovery (retry + fallback) | ADR-009 ✅ |
| TR-scene-013 | Startup flow (Boot→TEngine→HybridCLR→YooAsset) | ADR-009 ✅ |
| TR-scene-014 | 8 scene transition GameEvent IDs | ADR-009, ADR-006 ✅ |
| TR-scene-015 | Emotional weight fade duration | ADR-009 ✅ |
| TR-scene-016 | UnloadUnusedAssets + GC.Collect | ADR-009 ✅ |
| TR-scene-017 | SceneHandle reference retention | ADR-009, ADR-005 ✅ |

## Sprint 0 Findings Impact

- **SP-003 (YooAsset Package Strategy)**: 决策采用单包策略，ProcedureInitPackage 配置无需变更。场景卸载后共享资源（UI prefabs、SFX）不被误卸载已验证。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from the GDD are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs in `production/qa/evidence/`

## Dependencies

- **save-system**: Startup flow 需要 SaveManager 提供"上次所在章节"数据以决定加载哪个场景
- **chapter-state**: 场景→章节映射通过 ChapterStateManager 和 Luban `TbChapter.sceneId` 获取

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | Scene Manager State Machine | Logic | Ready | ADR-009 |
| 002 | Additive Scene Loading via YooAsset | Integration | Ready | ADR-009, ADR-005 |
| 003 | Mandatory Cleanup Sequence | Integration | Ready | ADR-009, ADR-005 |
| 004 | Transition Mutex with Max-1 Queue | Logic | Ready | ADR-009 |
| 005 | 8 Scene Lifecycle Events in Deterministic Order | Integration | Ready | ADR-009, ADR-006 |
| 006 | Luban Scene Name ↔ Chapter ID Mapping | Config/Data | Ready | ADR-009, ADR-007 |

## Next Step

Run `/story-readiness story-001-scene-state-machine` → `/dev-story` to begin implementation. Work through stories in order — each story's `Depends on:` field tells you what must be DONE before starting it.
