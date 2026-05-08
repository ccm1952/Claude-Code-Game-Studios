// 该文件由Cursor 自动生成

# Epic: Scene Management

> **Layer**: Core
> **GDD**: `design/gdd/scene-management.md`
> **Architecture Module**: SceneTransitionManager (additive loading, 11-step flow)
> **Governing ADRs**: ADR-009 (Scene Lifecycle), ADR-005 (YooAsset Resource), **ADR-027 (GameEvent Interface Protocol)**
> **Engine Risk**: MEDIUM
> **Status**: Ready
> **Stories**: 6 stories created
>
> **Revision note (2026-04-23)**: 整个 epic 由 ADR-006 `Evt_*` 常量协议重写为 ADR-027 接口事件协议。**单一** `ISceneEvent` 契约承载 1 个命令（`OnRequestSceneChange`）+ 8 个生命周期广播（`OnSceneTransitionBegin` / `OnSceneUnloadBegin` / `OnSceneDownloadProgress` / `OnSceneLoadProgress` / `OnSceneLoadComplete` / `OnSceneReady` / `OnSceneTransitionEnd` / `OnSceneLoadFailed`）。Story 001（S2-05）定义完整 9 方法签名 + 实装 `OnRequestSceneChange` / `OnSceneReady`；其余 6 方法的 sender 侧由 Story 005（S2-17）填充。**事件协议本体已完成：ADR-006 的 EventId 1400-1407 + 9 个 `XxxPayload` struct 方案整体作废；接口方法参数即 payload**。

## Overview

Scene Management 负责影子回忆的异步场景加载/卸载和过渡控制。系统采用 Additive Scene 架构（Boot + Main + Chapter 三层），通过 11 步标准化切换流程（FadeOut → Unload → GC → Load → FadeIn）确保场景过渡的视觉连续性和资源安全性。最多 3 个场景同时驻留内存。

系统基于 TEngine `GameModule.Scene` (SceneModule wrapper) + `GameModule.Resource` (ResourceModule wrapper) 实现（YooAsset 内部封装；S3-01 D5：SceneManager 持有 `_currentChapterSceneName: string` YooAsset location，不缓存 SceneHandle/Scene 引用），响应 `ISceneEvent.OnRequestSceneChange(int)` 接口事件（ADR-027）触发场景切换。切换过程互斥（max queue = 1），支持 2 次重试 + 回退至 MainMenu 的容错机制。Fade overlay 在 60fps 下独立于加载线程运行。所有 Scene 域事件通过单一 `ISceneEvent` 接口派发（`GroupLogic`，Source Generator 生成 `ISceneEvent_Gen` / `ISceneEvent_Event`），供 Audio、UI、Gameplay 等系统响应。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-009: Scene Lifecycle | Additive 场景架构；11 步切换流程；互斥队列；错误恢复机制；SceneManager 持 `_currentChapterSceneName: string` (S3-01 D5 supersedes 原 SceneHandle ownership) | LOW (verified 2026-04-30) |
| ADR-005: YooAsset Resource | 单包策略（SP-003 决策）；异步加载强制；Load/Unload 配对防泄漏 | MEDIUM |
| ADR-027: GameEvent Interface Protocol | 所有跨模块 Scene 事件统一走 `ISceneEvent` 接口方法（Source Generator 自动生成 proxy / dispatcher）；**禁止**使用 ADR-006 式的 `Evt_*` int 常量 + `XxxPayload` struct 模式 | LOW |

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
| TR-scene-014 | 8 scene lifecycle events (Begin/UnloadBegin/DownloadProgress/LoadProgress/LoadComplete/Ready/TransitionEnd/LoadFailed) as `ISceneEvent` 方法 | ADR-009, **ADR-027** ✅ |
| TR-scene-015 | Emotional weight fade duration | ADR-009 ✅ |
| TR-scene-016 | UnloadUnusedAssets + GC.Collect | ADR-009 ✅ |
| TR-scene-017 | Chapter scene identity retention (was: SceneHandle reference retention; refined 2026-04-30 — S3-01 D5: `_currentChapterSceneName: string` cached, framework wrapper handles SceneHandle internally) | ADR-009, ADR-005 ✅ |

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
| 001 | Scene Manager State Machine + `ISceneEvent` 契约（2 实）| Logic | Ready | ADR-009, **ADR-027** |
| 002 | Additive Scene Loading via YooAsset | Integration | Ready | ADR-009, ADR-005, ADR-027 |
| 003 | Mandatory Cleanup Sequence | Integration | Ready | ADR-009, ADR-005, ADR-027 |
| 004 | Transition Mutex with Max-1 Queue | Logic | Ready | ADR-009, ADR-027 |
| 005 | 6 Scene Lifecycle Events Sender Wire-up（Story 001 已冻结签名）| Integration | Ready | ADR-009, **ADR-027** |
| 006 | Luban Scene Name ↔ Chapter ID Mapping | Config/Data | Ready | ADR-009, ADR-007, ADR-027 |

## Next Step

Run `/story-readiness story-001-scene-state-machine` → `/dev-story` to begin implementation. Work through stories in order — each story's `Depends on:` field tells you what must be DONE before starting it.
