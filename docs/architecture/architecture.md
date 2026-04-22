// 该文件由Cursor 自动生成

# 影子回忆 (Shadow Memory) — Master Architecture Document

> **Version**: 1.0.0
> **Date**: 2026-04-22
> **Engine**: Unity 2022.3.62f2 LTS
> **Framework**: TEngine 6.0.0 + HybridCLR + YooAsset 2.3.17 + UniTask 2.5.10
> **Status**: Draft — Pending TD Review
> **Source GDDs**: 13 system GDDs + 1 game concept (see `src/MyGame/ShadowGame/design/gdd/`)
> **TR Baseline**: 212 TRs extracted (see `docs/architecture/phase0-tr-baseline.md`)

---

## Table of Contents

1. [Architecture Principles](#1-architecture-principles)
2. [Engine Knowledge Gap Summary](#2-engine-knowledge-gap-summary)
3. [Phase 1 — System Layer Map](#3-phase-1--system-layer-map)
4. [Phase 2 — Module Ownership Map](#4-phase-2--module-ownership-map)
5. [Phase 3 — Data Flow](#5-phase-3--data-flow)
6. [Phase 4 — API Boundaries](#6-phase-4--api-boundaries)
7. [Phase 5 — ADR Audit & Traceability](#7-phase-5--adr-audit--traceability)
8. [Phase 6 — Missing ADR List](#8-phase-6--missing-adr-list)
9. [Open Questions](#9-open-questions)

---

## 1. Architecture Principles

以下五条原则是本项目所有技术决策的判定标准，优先级从高到低排列：

### P1: 数据驱动 (Data-Driven Everything)

所有 gameplay 数值、谜题配置、演出序列、音频事件均由 Luban 配置表定义，运行时从表中读取。代码只实现行为逻辑，不包含任何具体数值。这确保了策划可以独立调优而无需程序重编译，同时使 HybridCLR 热更新仅需关注逻辑修复而非数据修正。

> 对应 TR: TR-concept-008, TR-input-017, TR-objint-018, TR-puzzle-014, TR-hint-010, TR-narr-003, TR-audio-015, TR-tutor-009

### P2: 事件解耦 (Event-Driven Decoupling)

模块间通信通过 TEngine `GameEvent`（int-based event IDs）实现，禁止跨模块直接方法调用或持有引用。每个模块只依赖事件契约（event ID + payload 类型），不依赖其他模块的具体实现。UI 内部使用 `AddUIEvent` 处理控件事件。

> 对应 TR: TR-concept-013, TR-objint-014/015/016, TR-scene-014

### P3: 异步优先 (Async-First)

所有 I/O 操作（资源加载、场景切换、存档读写）使用 UniTask async/await。禁止 Coroutine、禁止同步资源加载、禁止主线程阻塞文件操作。这是 60fps 帧预算的基本保障。

> 对应 TR: TR-concept-005, TR-concept-006, TR-save-008, TR-scene-003

### P4: 资源闭环 (Resource Lifecycle Closure)

每一次 `LoadAssetAsync` 必须有对应的 `UnloadAsset`。场景卸载后必须调用 `UnloadUnusedAssets()` + `GC.Collect()`。YooAsset `SceneHandle` 必须持有引用直到卸载完成。资源泄漏是 P0 级 bug。

> 对应 TR: TR-concept-010, TR-scene-016, TR-scene-017

### P5: 层级隔离 (Strict Layer Isolation)

系统按五层架构组织，只允许上层依赖下层，禁止反向依赖或同层跨系统直接依赖。唯一例外：同层系统可通过 GameEvent 实现松耦合通信。

> 对应 TR: TR-concept-013, TR-save-005, TR-hint-007

---

## 2. Engine Knowledge Gap Summary

摘自 Phase 0 TR Baseline，供实现时参考：

| Risk | Count | Domains | 缓解策略 |
|:----:|:-----:|---------|---------|
| **HIGH** | 0 | — | — |
| **MEDIUM** | 5 | TEngine 6.0.0, HybridCLR, YooAsset 2.3.17, Luban, I2 Localization | 见下表 |
| **LOW** | 15 | All core Unity 2022.3 APIs | 无需额外验证 |

### MEDIUM-RISK 域缓解计划

| 域 | 影响系统 | 缓解措施 | 优先级 |
|----|---------|---------|--------|
| TEngine 6.0.0 | UI, Audio, Scene, Event 全部 | 阅读项目内 TEngine 源码；建立 API cheat-sheet；优先 spike UIWindow 生命周期和 GameEvent 签名 | Sprint 0 |
| HybridCLR | 所有热更代码 | 验证 Default/GameLogic/GameProto 三程序集边界；测试热更 DLL 加载流程；确认 AOT 泛型限制 | Sprint 0 |
| YooAsset 2.3.17 | Scene Management, 所有资源加载 | 阅读项目现有 YooAsset 集成代码；验证 ResourcePackage 初始化流和 SceneHandle 生命周期 | Sprint 0 |
| Luban | 所有配置读取 | 检查 GameProto 程序集中的生成代码；验证 `Tables` 单例访问模式 | Sprint 0 |
| I2 Localization | Settings, UI text | 检查项目中已有 I2 配置；验证 runtime language switch API | Sprint 1 |

---

## 3. Phase 1 — System Layer Map

### 3.1 五层架构总览

```
┌─────────────────────────────────────────────────────────────────────┐
│                      PRESENTATION LAYER                             │
│  ┌──────────────────┐  ┌─────────────────────┐  ┌───────────────┐  │
│  │ Tutorial/Onboard │  │ Settings/Accessibil. │  │ Analytics [P] │  │
│  └──────────────────┘  └─────────────────────┘  └───────────────┘  │
├─────────────────────────────────────────────────────────────────────┤
│                        FEATURE LAYER                                │
│  ┌──────────────────┐  ┌──────────────┐  ┌───────────────────────┐ │
│  │Shadow Puzzle Sys. │  │ Hint System  │  │ Narrative Event Sys.  │ │
│  └──────────────────┘  └──────────────┘  └───────────────────────┘ │
│  ┌──────────────────┐                                               │
│  │Collectible S. [P]│                                               │
│  └──────────────────┘                                               │
├─────────────────────────────────────────────────────────────────────┤
│                         CORE LAYER                                  │
│  ┌──────────────────┐  ┌──────────────┐  ┌───────────────────────┐ │
│  │Object Interaction│  │Chapter State │  │     UI System         │ │
│  └──────────────────┘  └──────────────┘  └───────────────────────┘ │
│  ┌──────────────────┐                                               │
│  │  Audio System    │                                               │
│  └──────────────────┘                                               │
├─────────────────────────────────────────────────────────────────────┤
│                      FOUNDATION LAYER                               │
│  ┌──────────────────┐  ┌──────────────┐  ┌───────────────────────┐ │
│  │  Input System    │  │ Save System  │  │  Scene Management     │ │
│  └──────────────────┘  └──────────────┘  └───────────────────────┘ │
│  ┌──────────────────┐                                               │
│  │URP Shadow Render │                                               │
│  └──────────────────┘                                               │
├─────────────────────────────────────────────────────────────────────┤
│                       PLATFORM LAYER                                │
│  Unity 2022.3 │ URP │ TEngine 6.0 │ HybridCLR │ YooAsset         │
│  UniTask │ DOTween │ Luban │ PlayerPrefs │ I2 Localization         │
└─────────────────────────────────────────────────────────────────────┘

[P] = Planned / Not Yet Designed
```

### 3.2 各层系统清单与引擎风险

#### PLATFORM LAYER（由引擎和第三方库组成，非项目代码）

| 组件 | 版本 | 引擎风险 | 说明 |
|------|------|:-------:|------|
| Unity Runtime | 2022.3.62f2 | LOW | Stable LTS |
| URP | 2022.3 compat | LOW | Shadow maps, cascades, SRP Batcher |
| TEngine | 6.0.0 | **MEDIUM** | UIModule, AudioModule, ResourceModule, SceneModule, GameEvent, FsmModule, TimerModule, Procedure |
| HybridCLR | latest compatible | **MEDIUM** | Default / GameLogic / GameProto assembly split |
| YooAsset | 2.3.17 | **MEDIUM** | ResourcePackage, AssetHandle, SceneHandle |
| UniTask | 2.5.10 | LOW | Zero-alloc async |
| DOTween | latest | LOW | Tween animations |
| Luban | latest | **MEDIUM** | Config code generation |
| I2 Localization | observed | **MEDIUM** | Runtime language switch |
| PlayerPrefs | Unity built-in | LOW | Settings persistence |

#### FOUNDATION LAYER（无 gameplay 依赖，最底层项目代码）

| System | 职责 | 引擎风险 | 关键 TEngine/Unity API |
|--------|------|:-------:|----------------------|
| **Input System** | 触屏手势抽象、InputBlocker/InputFilter 栈 | LOW | Unity `Input.GetTouch()`, `Input.touchCount` |
| **Save System** | JSON 序列化、CRC32 校验、原子写入 | LOW | `System.IO`, `PlayerPrefs`, UniTask |
| **Scene Management** | 异步场景加载/卸载、fade 过渡 | **MEDIUM** | `GameModule.Resource.LoadSceneAsync`, `GameModule.Scene`, YooAsset `SceneHandle` |
| **URP Shadow Rendering** | 影子贴图、质量分级、ShadowRT 采样 | LOW (Unity URP) / **MEDIUM** (WallReceiver shader) | URP `UniversalRenderPipelineAsset`, `AsyncGPUReadback`, `RenderTexture` |

#### CORE LAYER（依赖 Foundation，提供 gameplay 基础设施）

| System | 职责 | 引擎风险 | 关键 TEngine/Unity API |
|--------|------|:-------:|----------------------|
| **Object Interaction** | 拖拽/旋转/吸附/光源轨道/状态机 | LOW | `Physics.Raycast`, DOTween, FsmModule |
| **Chapter State System** | 章节/谜题进度、运行时数据所有者 | LOW / **MEDIUM** (Luban) | GameEvent, Luban `TbChapter`/`TbPuzzle` |
| **UI System** | UIWindow 管理、9 面板、5 层级 | **MEDIUM** | `GameModule.UI`, UIWindow/UIWidget lifecycle, `SetUISafeFitHelper` |
| **Audio System** | 3 层混音、ducking、crossfade | **MEDIUM** | `GameModule.Audio` (TEngine AudioModule) |

#### FEATURE LAYER（依赖 Core，实现核心玩法）

| System | 职责 | 引擎风险 | 关键依赖 |
|--------|------|:-------:|---------|
| **Shadow Puzzle System** | 匹配评分、谜题状态机、缺席型谜题 | LOW | Object Interaction, URP Shadow Rendering, Chapter State, Input |
| **Hint System** | 3 层渐进提示、触发公式 | LOW | Shadow Puzzle (readonly query), TimerModule, GameEvent |
| **Narrative Event System** | 原子效果、序列播放、Timeline | LOW / **MEDIUM** (Timeline) | Shadow Puzzle, Chapter State, Audio, Input (blocker), UI |
| **Collectible System** [Planned] | 解锁叙事碎片 | LOW | Chapter State, Save, Narrative |

#### PRESENTATION LAYER（依赖 Feature，面向玩家终端体验）

| System | 职责 | 引擎风险 | 关键依赖 |
|--------|------|:-------:|---------|
| **Tutorial / Onboarding** | InputFilter 教学步骤 | LOW | Input (filter), Hint, UI, Chapter State |
| **Settings & Accessibility** | PlayerPrefs 设置、可达性选项 | LOW | Save, UI, Audio, Input |
| **Analytics** [Planned] | 遥测数据 | LOW | Shadow Puzzle, Chapter State |

### 3.3 层间变更说明（相对 systems-index.md）

| 系统 | 原层级 | 新层级 | 变更原因 |
|------|-------|-------|---------|
| Audio System | Feature | **Core** | 被 Narrative, Scene, Settings 多个上层系统依赖；是基础设施而非 gameplay feature |
| UI System | Feature | **Core** | 被 Narrative, Tutorial, Settings, Scene 等上层系统依赖；UIWindow 管理是基础设施 |
| Save System | Core | **Foundation** | 最底层持久化服务，不含任何 gameplay 逻辑；Chapter State 依赖它而非反向 |
| Scene Management | Core | **Foundation** | 最底层场景生命周期管理，所有上层系统在场景之上运行 |

---

## 4. Phase 2 — Module Ownership Map

### 4.1 Foundation Layer

#### Input System

| 维度 | 内容 |
|------|------|
| **Owns** | 触摸状态机 (SingleFinger FSM, DualFinger FSM)、InputBlocker 栈、InputFilter 栈、手势识别参数 |
| **Exposes** | `OnGesture(GestureType, GesturePhase, GestureData)` events via GameEvent; `PushBlocker(token)`/`PopBlocker(token)`; `PushFilter(allowedGestures)`/`PopFilter()` |
| **Consumes** | Unity `Input.GetTouch()` API; Luban config for thresholds (dragThresholdDp, tapTimeout, rotationMin, pinchMin) |
| **Engine APIs** | `Input.touchCount`, `Input.GetTouch(i)` [LOW]; `Screen.dpi` [LOW]; `Time.unscaledDeltaTime` [LOW] |

#### Save System

| 维度 | 内容 |
|------|------|
| **Owns** | Save file (JSON)、backup file、CRC32 checksum、version migration chain |
| **Exposes** | `LoadAsync() → SaveData`; `SaveAsync(SaveData)`; `DeleteSave()`; `HasSave → bool`; `OnSaveComplete` event |
| **Consumes** | `IChapterProgress` (from Chapter State, serialization target); `OnApplicationPause`/`OnApplicationQuit` Unity lifecycle |
| **Engine APIs** | `System.IO.File` via UniTask [LOW]; `PlayerPrefs` (settings only) [LOW]; `Application.persistentDataPath` [LOW] |

#### Scene Management

| 维度 | 内容 |
|------|------|
| **Owns** | 当前场景状态 (Idle/Transitioning/Unloading/Loading/Error)、场景切换队列 (max 1)、SceneHandle 引用、fade overlay |
| **Exposes** | Responds to `RequestSceneChange` event; fires `SceneTransitionBegin`, `SceneUnloadBegin`, `SceneLoadComplete`, `SceneReady`, `SceneTransitionEnd`, `SceneLoadFailed`, `SceneLoadProgress`, `SceneDownloadProgress` events |
| **Consumes** | `RequestSceneChange` event (from Chapter State); Luban `TbChapter.sceneId` mapping; Save System (startup: which chapter to load) |
| **Engine APIs** | `GameModule.Resource.LoadSceneAsync()` [**MEDIUM** — TEngine+YooAsset]; `GameModule.Resource.UnloadSceneAsync()` [**MEDIUM**]; `SceneManager.SetActiveScene()` [LOW]; `Resources.UnloadUnusedAssets()` [LOW]; `GC.Collect()` [LOW] |

#### URP Shadow Rendering

| 维度 | 内容 |
|------|------|
| **Owns** | Shadow maps (per quality tier)、ShadowSampleCamera + ShadowRT (R8 grayscale)、WallReceiver shader state、quality tier state、shadow style presets (5 chapters) |
| **Exposes** | `GetShadowRT() → RenderTexture`; `SetShadowGlow(intensity, color)`; `FreezeShadow(snapshotRT)`; `SetShadowStyle(preset)`; `TransitionShadowStyle(from, to, duration)`; Shadow-Only render mode toggle; quality tier API |
| **Consumes** | Chapter config (shadow style per chapter); quality settings (from Settings system or auto-detect) |
| **Engine APIs** | `UniversalRenderPipelineAsset` [LOW]; `RenderTexture` [LOW]; `AsyncGPUReadback.Request()` [LOW]; Custom `.shader` / `.hlsl` for WallReceiver [**MEDIUM** — custom shader]; `Camera` (ShadowSampleCamera) [LOW] |

### 4.2 Core Layer

#### Object Interaction

| 维度 | 内容 |
|------|------|
| **Owns** | 每个 InteractableObject 的状态机 (Idle→Selected→Dragging→Snapping→Locked)、每个 LightSource 的状态机 (Fixed→TrackIdle→TrackDragging→TrackSnapping)、当前选中对象引用、InteractionBounds |
| **Exposes** | `ObjectTransformChanged(objectId, transform)` event; `LightPositionChanged(lightId, trackT)` event; `PuzzleLockEvent(objectId)` / `PuzzleLockAllEvent` / `PuzzleSnapToTargetEvent(objectId, target)` events; `GetSelectedObject() → InteractableObject` |
| **Consumes** | Input gesture events (Tap, Drag, Rotate, Pinch, LightDrag); `PuzzleLockAllEvent` (from Puzzle or Narrative); `PuzzleUnlockEvent`; Luban config (gridSize, rotationStep, snapSpeed, bounds) |
| **Engine APIs** | `Physics.Raycast()` [LOW]; DOTween [LOW]; FsmModule [**MEDIUM** — TEngine] |

#### Chapter State System

| 维度 | 内容 |
|------|------|
| **Owns** | 运行时章节进度 (`ChapterProgress[]`)、谜题状态 (`PuzzleState[]`)、当前活跃章节 ID、回放模式标志 |
| **Exposes** | `IChapterProgress` data interface (for Save System); `GetCurrentChapter()`, `GetPuzzleState(puzzleId)`, `IsChapterUnlocked(chapterId)`, `GetActivePuzzle()` queries; `ChapterCompleteEvent(chapterId)`, `PuzzleStateChanged(puzzleId, newState)`, `RequestSceneChange(chapterId)` events |
| **Consumes** | `PuzzleCompleteEvent(puzzleId)` (from Shadow Puzzle); `AbsenceAcceptedEvent(puzzleId)` (from Shadow Puzzle); Save System `LoadAsync()` data on init; Luban `TbChapter`, `TbPuzzle` tables |
| **Engine APIs** | GameEvent [**MEDIUM** — TEngine]; Luban Tables singleton [**MEDIUM**] |

#### UI System

| 维度 | 内容 |
|------|------|
| **Owns** | 9 UIWindow 实例生命周期、UI layer sorting (Background/HUD/Popup/Overlay/System)、popup queue、InputBlocker 令牌 (auto push/pop for Popup/Overlay) |
| **Exposes** | `ShowWindow<T>(params)`; `CloseWindow<T>()`; popup queue management; `OnWindowOpened` / `OnWindowClosed` events |
| **Consumes** | `PuzzleStateChanged` (HUD update); `ChapterCompleteEvent` (transition panel); `SceneTransitionBegin/End` (ChapterTransition overlay); `MatchScoreChanged` (optional HUD indicator); `SaveComplete` (SaveIndicator); `HintAvailable` (HintButton opacity ramp) |
| **Engine APIs** | `GameModule.UI` — `UIWindow.Create()`, `UIWindow.Close()`, `UIWidget` lifecycle [**MEDIUM** — TEngine]; `SetUISafeFitHelper` [**MEDIUM** — TEngine]; UGUI Canvas / EventSystem [LOW]; `GameModule.Resource.LoadAssetAsync` for prefabs [**MEDIUM**] |

#### Audio System

| 维度 | 内容 |
|------|------|
| **Owns** | 3 mix layers (Ambient/SFX/Music) 的 volume state 和 AudioSource pools、ducking state、current BGM track reference |
| **Exposes** | `PlaySFX(sfxId, position?)`, `PlayMusic(clipId, crossfadeDuration)`, `StopMusic(fadeDuration)`, `SetDucking(duckRatio, fadeDuration)`, `ReleaseDucking(fadeDuration)`, `SetLayerVolume(layer, volume)`, `PauseAll()`, `ResumeAll()` |
| **Consumes** | `AudioDuckingRequest` (from Narrative); `SceneLoadComplete` with bgmAsset (from Scene); `SceneTransitionBegin` (fade out music); Settings volume values; Luban SFX config tables |
| **Engine APIs** | `GameModule.Audio` [**MEDIUM** — TEngine AudioModule]; `AudioSource` [LOW]; `AudioListener` [LOW] |

### 4.3 Feature Layer

#### Shadow Puzzle System

| 维度 | 内容 |
|------|------|
| **Owns** | 当前谜题状态机 (Locked→Idle→Active→NearMatch→PerfectMatch→AbsenceAccepted→Complete)、matchScore (smoothed)、per-anchor scores、tutorialGracePeriod timer |
| **Exposes** | `GetMatchScore() → float` (read-only); `GetAnchorScores() → AnchorScore[]` (read-only, for Hint); `PerfectMatchEvent(puzzleId)`; `AbsenceAcceptedEvent(puzzleId)`; `PuzzleCompleteEvent(puzzleId, chapterId)`; `MatchScoreChanged(score)` event; `NearMatchEnter`/`NearMatchExit` events |
| **Consumes** | `ObjectTransformChanged` / `LightPositionChanged` (from Object Interaction); ShadowRT data (from URP Shadow Rendering); Chapter State queries (puzzle config, unlock status); Luban `TbPuzzle`, `TbPuzzleObject` |
| **Engine APIs** | `AsyncGPUReadback` (via Shadow Rendering) [LOW]; FsmModule [**MEDIUM**]; GameEvent [**MEDIUM**] |

#### Hint System

| 维度 | 内容 |
|------|------|
| **Owns** | triggerScore 计算状态、idle timer、fail counter、stagnation detector、hint layer state machine (Idle→Observing→L1Active→Cooldown→L2Active→Cooldown→L3Ready)、L3 usage count per puzzle |
| **Exposes** | `GetCurrentHintLayer() → int`; `GetL3UsageCount() → int` (for Narrative optional query); `HintAvailable(layer)` event; `HintDismissed` event |
| **Consumes** | `GetMatchScore()` / `GetAnchorScores()` (readonly query to Shadow Puzzle, 1s polling); `PuzzleStateChanged` events; Tutorial state (pause hint timers during tutorial); Luban hint config (`hintDelayOverride` per chapter) |
| **Engine APIs** | `GameModule.Timer` (TimerModule) [**MEDIUM** — TEngine]; GameEvent [**MEDIUM**] |

#### Narrative Event System

| 维度 | 内容 |
|------|------|
| **Owns** | NarrativeSequence 播放状态 (Idle→Playing→WaitingForTimeline)、effect execution timeline、sequence queue (max 3) |
| **Exposes** | `SequenceCompleteEvent(sequenceId)` event; `IsPlaying → bool` query |
| **Consumes** | `PerfectMatchEvent` / `AbsenceAcceptedEvent` (from Shadow Puzzle); `ChapterCompleteEvent` (from Chapter State); Luban NarrativeSequenceConfig / PuzzleNarrativeMap / ChapterTransitionMap |
| **Engine APIs** | `PlayableDirector` (Timeline) [LOW]; `VideoPlayer` [LOW]; GameEvent [**MEDIUM**]; `GameModule.Resource.LoadAssetAsync` [**MEDIUM**] |

#### Collectible System [Planned]

| 维度 | 内容 |
|------|------|
| **Owns** | 收集品解锁状态 per chapter |
| **Exposes** | `CollectibleUnlocked(id)` event; `GetUnlockedCollectibles()` query |
| **Consumes** | Chapter State (chapter/puzzle completion); Save System (persistence); Narrative events (trigger) |
| **Engine APIs** | GameEvent, Luban |

### 4.4 Presentation Layer

#### Tutorial / Onboarding

| 维度 | 内容 |
|------|------|
| **Owns** | TutorialStep 执行状态、completionCondition 检测、`tutorialCompleted[]` persistence flag |
| **Exposes** | `TutorialStepStarted(stepId)` / `TutorialStepCompleted(stepId)` events |
| **Consumes** | Input `PushFilter` / `PopFilter`; Hint (pause/resume timers); UI (prompt display); Chapter State (step trigger conditions); Save (persisted completion flags); Luban `TbTutorialStep` |
| **Engine APIs** | GameEvent [**MEDIUM**]; Input filter API |

#### Settings & Accessibility

| 维度 | 内容 |
|------|------|
| **Owns** | 8 player settings (master_volume, music_volume, sfx_volume, sfx_enabled, haptic_enabled, touch_sensitivity, language, target_framerate) |
| **Exposes** | `GetSetting(key)`, `SetSetting(key, value)`, `OnSettingChanged(key, value)` event |
| **Consumes** | PlayerPrefs (read/write); Audio (volume apply); Input (sensitivity apply); UI (panel display); I2 Localization (language switch) |
| **Engine APIs** | `PlayerPrefs` [LOW]; `Application.targetFrameRate` [LOW]; `Application.systemLanguage` [LOW]; I2 Localization API [**MEDIUM**] |

#### Analytics [Planned]

| 维度 | 内容 |
|------|------|
| **Owns** | Telemetry event queue |
| **Exposes** | `TrackEvent(name, params)` |
| **Consumes** | Shadow Puzzle events, Chapter State events |
| **Engine APIs** | 待定 |

### 4.5 系统依赖 ASCII 图

```
PRESENTATION                          FEATURE                              CORE                    FOUNDATION
─────────────                         ───────                              ────                    ──────────

 Tutorial ──┬── Input.Filter ──────────────────────────────────────────> Input System
             ├── Hint (pause) ──────> Hint System ──┐
             ├── UI (prompt) ────────────────────────┼──────────────────> UI System
             └── ChapterState (trigger)──────────────┼──────────────────> Chapter State ──┐
                                                     │                                    │
 Settings ──┬── Audio (volume) ──────────────────────┼──────────────────> Audio System     │
             ├── UI (panel) ─────────────────────────┘                                    │
             ├── Input (sensitivity) ──────────────────────────────────> Input System      │
             └── Save (persist) ───────────────────────────────────────────────────────> Save System
                                                                                          │
             Shadow Puzzle ──┬── ObjInteraction (transform events) ─> Obj. Interaction     │
                             ├── URP Shadow (ShadowRT) ────────────────────────────────> URP Shadow Rendering
                             ├── ChapterState (config/unlock) ──────> Chapter State ──────┤
                             └── Input (gesture events) ───────────────────────────────> Input System
                                                                                          │
             Hint ───────────── Shadow Puzzle (readonly query)                             │
                                                                                          │
             Narrative ──────┬── Shadow Puzzle (PerfectMatch evt) ──> (event only)         │
                             ├── ChapterState (ChapterComplete evt)─> Chapter State        │
                             ├── Audio (ducking/sfx) ───────────────> Audio System          │
                             ├── Input (blocker) ──────────────────> Input System           │
                             ├── UI (fade/letterbox) ──────────────> UI System              │
                             └── Scene (LoadNextChapter) ──────────────────────────────> Scene Management
                                                                                          │
                             Chapter State ─── Save (IChapterProgress) ─────────────────> Save System
                             Scene Management ── Chapter State (RequestSceneChange) ────> (event from CS)
                             Scene Management ── YooAsset/ResourceModule ──────────────> [Platform Layer]
```

---

## 5. Phase 3 — Data Flow

### 5.1 Frame Update Path（帧更新路径）

每帧从玩家触摸到 UI 反馈的数据流：

```
[Touch Input]                                                         [per frame]
     │
     ▼
Input System
├── InputBlocker check ─── blocked? → discard
├── InputFilter check ──── filtered? → discard
├── Gesture Recognition (SingleFinger/DualFinger FSM)
│   └── Gesture identified (Tap/Drag/Rotate/Pinch/LightDrag)
│
├── GameEvent.Send(GestureEvent, gestureData)
│
▼
Object Interaction
├── Raycast selection (on Tap)
├── Transform update (on Drag/Rotate)
│   ├── Boundary clamp
│   └── Snap interpolation
├── Light track update (on LightDrag)
│
├── GameEvent.Send(ObjectTransformChanged, objectId, transform)
├── GameEvent.Send(LightPositionChanged, lightId, trackT)
│
▼
Shadow Puzzle System
├── Read ShadowRT via AsyncGPUReadback (每帧或隔帧)
├── Per-anchor score calculation:
│   positionScore × directionScore × visibilityScore
├── Weighted average: matchScore = Σ(w_i × s_i) / Σ(w_i)
├── Temporal smoothing (0.2s sliding window)
├── State machine transition check:
│   Active ←→ NearMatch → PerfectMatch → Complete
│
├── GameEvent.Send(MatchScoreChanged, score)
├── [if NearMatch enter] GameEvent.Send(NearMatchEnter)
│
▼
Hint System (1s polling, NOT per frame)              UI System
├── triggerScore calculation                          ├── HUD: update hint button opacity
├── Layer escalation check                            ├── HUD: NearMatch glow indicator
│                                                     └── (reactions to MatchScoreChanged)
▼
[Visual/Audio feedback on same frame or next frame]
```

**性能约束**:
- Input gesture recognition: < 0.5ms (TR-input-016)
- Object Interaction Update: < 1ms (TR-objint-020)
- Shadow match calculation: < 2ms (TR-puzzle-010)
- ShadowRT CPU readback: ≤ 1.5ms (TR-render-019)
- Hint Update: < 0.5ms (TR-hint-013)
- **Total gameplay systems per frame: < 5.5ms** (within 16.67ms budget)

### 5.2 Puzzle Complete Flow（谜题完成流）

从 PerfectMatch 判定到下一个谜题解锁的完整流程：

```
Shadow Puzzle System                 Narrative Event System               Chapter State
       │                                     │                                │
       │ matchScore >= 0.85                  │                                │
       │ (持续，非瞬时)                       │                                │
       ▼                                     │                                │
  Enter PerfectMatch state                   │                                │
       │                                     │                                │
       ├── freeze matchScore calculation     │                                │
       │                                     │                                │
       ├── GameEvent: PerfectMatchEvent ────>│                                │
       │   { puzzleId }                      │                                │
       │                                     ▼                                │
       │                              Lookup NarrativeSequenceId              │
       │                              from Luban config                       │
       │                                     │                                │
       │                              GameEvent: PuzzleLockAllEvent ────────> │ (also heard by
       │                              Input: PushBlocker(narrative)           │  Object Interaction)
       │                                     │                                │
       │                              Execute atomic effects:                 │
       │                              t=0.0: AudioDucking + ObjectSnap        │
       │                              t=0.5: ColorTemperature                 │
       │                              t=0.8: SFXOneShot                       │
       │                              t=1.0: TextureVideo                     │
       │                              t=5.0: ColorTemperature (restore)       │
       │                              t=5.5: AudioDucking (restore)           │
       │                                     │                                │
       │                              SequenceComplete                        │
       │                              Input: PopBlocker(narrative)            │
       │                              GameEvent: PuzzleUnlockEvent ─────────> │
       │                                     │                                │
       │                                     │                                ▼
       │                                     │                          OnPuzzleComplete(puzzleId)
       │                                     │                          ├── puzzleState → Complete
       │                                     │                          ├── unlock next puzzle
       │                                     │                          ├── auto-save trigger
       │                                     │                          │
       │                                     │                          └── [if last puzzle]
       │                                     │                                ChapterCompleteEvent
       │                                     │                                     │
       │                                     │<── ChapterCompleteEvent ────────────┘
       │                                     │
       │                                     ▼
       │                              Play chapter transition sequence
       │                              (ScreenFade + TimelinePlayable)
       │                                     │
       │                              GameEvent: LoadNextChapterEvent
       │                                     │
       │                                     ▼
       │                              Scene Management handles
       │                              scene transition
       │
```

### 5.3 Event / Signal Communication Map（GameEvent 总表）

所有系统间通信使用 TEngine `GameEvent`（int-based event ID）。以下为完整事件清单：

**Input Events** (dispatched by Input System):

| Event ID 常量 | Payload | 发送者 | 接收者 |
|---------------|---------|-------|-------|
| `Evt_Gesture_Tap` | `{ Vector2 screenPos }` | Input | Object Interaction |
| `Evt_Gesture_Drag` | `{ GesturePhase phase, Vector2 pos, Vector2 delta }` | Input | Object Interaction |
| `Evt_Gesture_Rotate` | `{ GesturePhase phase, float angleDelta }` | Input | Object Interaction |
| `Evt_Gesture_Pinch` | `{ GesturePhase phase, float scaleDelta }` | Input | Object Interaction |
| `Evt_Gesture_LightDrag` | `{ GesturePhase phase, Vector2 pos, Vector2 delta }` | Input | Object Interaction |

**Object Interaction Events**:

| Event ID 常量 | Payload | 发送者 | 接收者 |
|---------------|---------|-------|-------|
| `Evt_ObjectTransformChanged` | `{ int objectId, Vector3 pos, Quaternion rot }` | Object Interaction | Shadow Puzzle |
| `Evt_LightPositionChanged` | `{ int lightId, float trackT }` | Object Interaction | Shadow Puzzle |
| `Evt_ObjectSelected` | `{ int objectId }` | Object Interaction | UI (operation hint) |
| `Evt_ObjectDeselected` | `{ }` | Object Interaction | UI |

**Puzzle Events**:

| Event ID 常量 | Payload | 发送者 | 接收者 |
|---------------|---------|-------|-------|
| `Evt_MatchScoreChanged` | `{ float score }` | Shadow Puzzle | UI, Hint (via polling) |
| `Evt_NearMatchEnter` | `{ int puzzleId }` | Shadow Puzzle | UI, Audio |
| `Evt_NearMatchExit` | `{ int puzzleId }` | Shadow Puzzle | UI, Audio |
| `Evt_PerfectMatch` | `{ int puzzleId }` | Shadow Puzzle | Narrative, Chapter State |
| `Evt_AbsenceAccepted` | `{ int puzzleId }` | Shadow Puzzle | Narrative, Chapter State |
| `Evt_PuzzleLockAll` | `{ }` | Shadow Puzzle / Narrative | Object Interaction |
| `Evt_PuzzleUnlock` | `{ }` | Narrative | Object Interaction |
| `Evt_PuzzleSnapToTarget` | `{ int objectId, Vector3 targetPos, Quaternion targetRot, float duration }` | Narrative | Object Interaction |
| `Evt_PuzzleComplete` | `{ int puzzleId, int chapterId }` | Shadow Puzzle | Chapter State |

**Chapter State Events**:

| Event ID 常量 | Payload | 发送者 | 接收者 |
|---------------|---------|-------|-------|
| `Evt_PuzzleStateChanged` | `{ int puzzleId, PuzzleState newState }` | Chapter State | UI, Hint, Tutorial |
| `Evt_ChapterComplete` | `{ int chapterId }` | Chapter State | Narrative, Scene |
| `Evt_RequestSceneChange` | `{ int targetChapterId }` | Chapter State | Scene Management |

**Scene Events**:

| Event ID 常量 | Payload | 发送者 | 接收者 |
|---------------|---------|-------|-------|
| `Evt_SceneTransitionBegin` | `{ int fromChapterId, int toChapterId }` | Scene Mgmt | UI, Audio |
| `Evt_SceneUnloadBegin` | `{ int chapterId }` | Scene Mgmt | Shadow Puzzle, Narrative |
| `Evt_SceneLoadProgress` | `{ float progress }` | Scene Mgmt | UI |
| `Evt_SceneDownloadProgress` | `{ float progress, long downloadedBytes, long totalBytes }` | Scene Mgmt | UI |
| `Evt_SceneLoadComplete` | `{ int chapterId, string bgmAsset }` | Scene Mgmt | Shadow Puzzle, Audio, Chapter State |
| `Evt_SceneReady` | `{ int chapterId }` | Scene Mgmt | Chapter State |
| `Evt_SceneTransitionEnd` | `{ int chapterId }` | Scene Mgmt | All |
| `Evt_SceneLoadFailed` | `{ int chapterId, string error }` | Scene Mgmt | UI |

**Narrative Events**:

| Event ID 常量 | Payload | 发送者 | 接收者 |
|---------------|---------|-------|-------|
| `Evt_SequenceComplete` | `{ string sequenceId }` | Narrative | Chapter State (for unlock timing) |
| `Evt_LoadNextChapter` | `{ int nextChapterId }` | Narrative | Scene Management (via Chapter State) |

**Audio Events**:

| Event ID 常量 | Payload | 发送者 | 接收者 |
|---------------|---------|-------|-------|
| `Evt_AudioDuckingRequest` | `{ float duckRatio, float fadeDuration }` | Narrative | Audio |
| `Evt_AudioDuckingRelease` | `{ float fadeDuration }` | Narrative | Audio |
| `Evt_PlaySFXRequest` | `{ string sfxId, Vector3? position }` | Various | Audio |
| `Evt_PlayMusicRequest` | `{ string clipId, float crossfadeDuration }` | Scene Mgmt | Audio |

**Hint Events**:

| Event ID 常量 | Payload | 发送者 | 接收者 |
|---------------|---------|-------|-------|
| `Evt_HintAvailable` | `{ int layer }` | Hint | UI (HintButton opacity ramp) |
| `Evt_HintDismissed` | `{ }` | Hint | UI |

**Tutorial Events**:

| Event ID 常量 | Payload | 发送者 | 接收者 |
|---------------|---------|-------|-------|
| `Evt_TutorialStepStarted` | `{ string stepId }` | Tutorial | Hint (pause timers), UI |
| `Evt_TutorialStepCompleted` | `{ string stepId }` | Tutorial | Hint (resume timers), UI |

**Save Events**:

| Event ID 常量 | Payload | 发送者 | 接收者 |
|---------------|---------|-------|-------|
| `Evt_SaveComplete` | `{ }` | Save | UI (SaveIndicator) |

**Settings Events**:

| Event ID 常量 | Payload | 发送者 | 接收者 |
|---------------|---------|-------|-------|
| `Evt_SettingChanged` | `{ string key, object value }` | Settings | Audio, Input, UI (various) |

### 5.4 Save / Load Path（存档加载路径）

```
[App 启动]
     │
     ▼
BootScene
├── TEngine RootModule init
├── HybridCLR: load hot-fix DLLs (GameLogic, GameProto)
├── HybridCLR: register AOT metadata
├── YooAsset: InitializeAsync()
│   ├── UpdatePackageManifestAsync()
│   └── [if update available] → download
│
▼
ProcedureMain (TEngine Procedure)
├── await GameModule.Resource.LoadSceneAsync("MainScene", Additive)
│   → MainScene 常驻: UI Canvas, AudioListener, Camera Rig, Managers
│
├── Settings.Init() ← PlayerPrefs.Get*(keys)
│   → apply volume, sensitivity, framerate, language
│
├── saveData = await SaveSystem.LoadAsync()
│   ├── Read primary file → CRC32 verify
│   │   ├── [OK] → deserialize JSON → SaveData
│   │   └── [FAIL] → try .backup.json
│   │       ├── [OK] → deserialize → SaveData (log warning)
│   │       └── [FAIL] → SaveData = new() (fresh start, notify user)
│   │
│   └── Version migration chain: v1→v2→...→vCurrent
│
├── ChapterState.Init(saveData.chapterProgress)
│   → populate runtime chapter/puzzle states
│
├── UI: Show MainMenu
│   ├── [has save] → "继续游戏" button visible
│   │   → RequestSceneChange(saveData.currentChapterId)
│   └── [no save] → "新游戏" only
│       → RequestSceneChange(1)
│
▼
[Scene Management handles scene load → normal gameplay]
```

**Auto-save 触发点**:
- `PuzzleStateChanged` (debounced 1s)
- `ChapterCompleteEvent` (immediate)
- Collectible pickup (debounced 1s)
- `OnApplicationPause(true)` (immediate, bypass debounce)
- `OnApplicationQuit()` (immediate, bypass debounce)

### 5.5 Scene Transition Flow（场景切换路径）

```
Chapter State                    Scene Manager                  ResourceModule/YooAsset
     │                                │                               │
     │ ── Evt_RequestSceneChange ──> │                               │
     │    { targetChapterId: 3 }     │                               │
     │                                │                               │
     │                         [1] Check mutex                        │
     │                         (if transitioning → queue, max 1)      │
     │                                │                               │
     │                         [2] Evt_SceneTransitionBegin           │
     │                              → UI: start FadeOut overlay       │
     │                              → Audio: music fadeOut             │
     │                                │                               │
     │                         [3] await FadeOut                       │
     │                              (EaseInCubic, 0.8s × emotionWt)   │
     │                                │                               │
     │                         [4] Evt_SceneUnloadBegin               │
     │                              → Puzzle: cleanup runtime data    │
     │                              → Narrative: abort any sequence    │
     │                                │                               │
     │                         [5] await UnloadSceneAsync(handle) ──>│ Unload
     │                                │                               │
     │                         [6] Resources.UnloadUnusedAssets()      │
     │                              + GC.Collect()                     │
     │                                │                               │
     │                         [7] Check resource package ──────────>│
     │                              已缓存? → skip                    │
     │                              未缓存? → DownloadAsync()         │
     │                              → Evt_SceneDownloadProgress       │
     │                                │                               │
     │                         [8] await LoadSceneAsync(Additive) ──>│ Load
     │                                │                               │
     │                         [9] SetActiveScene(new)                │
     │                                │                               │
     │                        [10] Evt_SceneLoadComplete              │
     │                              → Puzzle: init puzzle data        │
     │                              → Audio: switch BGM               │
     │                              → Chapter State: confirm          │
     │                                │                               │
     │ <── Evt_SceneReady ───────────│                               │
     │                                │                               │
     │                        [11] await FadeIn                       │
     │                              (EaseOutCubic, 1.2s × emotionWt) │
     │                                │                               │
     │                        [12] Evt_SceneTransitionEnd             │
     │                              → restore player input            │
```

### 5.6 Initialization Order（启动初始化顺序）

系统初始化必须按以下确定性顺序执行，违反将导致空引用或数据不一致：

```
Boot Phase (BootScene, non-hot-fix):
────────────────────────────────────
  1. Unity Engine init
  2. TEngine RootModule.Init()
     ├── ModuleSystem registration
     ├── ResourceModule init
     ├── UIModule init
     ├── AudioModule init
     ├── SceneModule init
     ├── TimerModule init
     ├── FsmModule init
     └── GameEvent init
  3. HybridCLR: Load hot-fix assemblies
     ├── GameLogic.dll (gameplay code)
     └── GameProto.dll (Luban generated configs)
  4. HybridCLR: Register AOT generic metadata
  5. YooAsset: InitializeAsync(packageName)
  6. YooAsset: UpdatePackageManifestAsync()
     └── [if needed] download resource patches

Hot-Fix Phase (ProcedureMain, GameLogic assembly):
──────────────────────────────────────────────────
  7. Luban: Tables.Init() → load all config tables into memory
  8. MainScene: await LoadSceneAsync("MainScene", Additive)

Foundation Init:
───────────────
  9.  InputSystem.Init() → register gesture recognizers, init blocker/filter stacks
 10.  SaveSystem.Init() → determine save file path
 11.  SaveSystem.LoadAsync() → load save data (or create fresh)

Core Init:
──────────
 12.  ChapterState.Init(saveData) → populate runtime progress from loaded save
 13.  AudioSystem.Init() → create mix layers, init AudioSource pools
 14.  UISystem.Init() → register UIWindow types, preload common prefabs

Presentation Init:
──────────────────
 15.  SettingsSystem.Init() → read PlayerPrefs, apply to Audio/Input/Display
 16.  UISystem.ShowMainMenu() → display main menu

Player Action:
──────────────
 17.  [Player chooses "继续" or "新游戏"]
 18.  ChapterState → Evt_RequestSceneChange(chapterId)
 19.  SceneManager → full transition flow (5.5)
 20.  [Gameplay begins]
```

**关键约束**:
- Step 7 (Luban) 必须在 Step 3 (HybridCLR) 之后——GameProto 在热更 DLL 中
- Step 11 (SaveSystem.Load) 必须在 Step 12 (ChapterState.Init) 之前——CS 需要存档数据
- Step 13 (Audio) 必须在 Step 15 (Settings) 之前——Settings 需要将音量值应用到 Audio
- Step 12 (ChapterState) 必须在 Step 16 (ShowMainMenu) 之前——MainMenu 需要知道是否有存档

---

## 6. Phase 4 — API Boundaries

以下定义关键模块边界的 C# 接口契约。所有接口置于 `GameLogic` 程序集中（热更），实现类注册到 TEngine 模块系统。

### 6.1 IInputService

```csharp
public enum GestureType { Tap, Drag, Rotate, Pinch, LightDrag }
public enum GesturePhase { Began, Updated, Ended, Cancelled }

public struct GestureData
{
    public GestureType Type;
    public GesturePhase Phase;
    public Vector2 ScreenPosition;
    public Vector2 Delta;
    public float AngleDelta;      // Rotate only
    public float ScaleDelta;      // Pinch only
}

public interface IInputService
{
    // Blocker stack — full input suppression
    void PushBlocker(string token);
    void PopBlocker(string token);
    bool IsBlocked { get; }

    // Filter stack — whitelist-based gesture filtering (for Tutorial)
    void PushFilter(GestureType[] allowedGestures);
    void PopFilter();
    bool IsFiltered { get; }

    // Gesture events dispatched via GameEvent (not direct callbacks)
    // Consumers: GameEvent.AddEventListener(Evt_Gesture_Tap, handler)
}
```

### 6.2 IObjectInteraction

```csharp
public interface IObjectInteraction
{
    InteractableObject GetSelectedObject();
    bool IsAnyObjectDragging();

    // Lock/Unlock API (called by Narrative via GameEvent, not directly)
    // Responds to: Evt_PuzzleLockAll, Evt_PuzzleUnlock, Evt_PuzzleSnapToTarget

    // Events (dispatched via GameEvent):
    //   Evt_ObjectTransformChanged { int objectId, Vector3 pos, Quaternion rot }
    //   Evt_LightPositionChanged { int lightId, float trackT }
    //   Evt_ObjectSelected { int objectId }
    //   Evt_ObjectDeselected { }
}

public enum ObjectState { Idle, Selected, Dragging, Snapping, Locked }
public enum LightState { Fixed, TrackIdle, TrackDragging, TrackSnapping }

public class InteractableObject : MonoBehaviour
{
    public int ObjectId { get; }
    public ObjectState CurrentState { get; }
    public void SetLocked(bool locked);
    public void SnapToTarget(Vector3 targetPos, Quaternion targetRot, float duration);
}
```

### 6.3 IShadowPuzzle

```csharp
public struct AnchorScore
{
    public int AnchorId;
    public float PositionScore;    // 0-1
    public float DirectionScore;   // 0-1
    public float VisibilityScore;  // 0 or 1
    public float CombinedScore;    // product
    public float Weight;
}

public enum PuzzleState { Locked, Idle, Active, NearMatch, PerfectMatch, AbsenceAccepted, Complete }

public interface IShadowPuzzle
{
    // Read-only queries (consumed by Hint System at 1s polling interval)
    float GetMatchScore();
    AnchorScore[] GetAnchorScores();
    PuzzleState GetCurrentState();
    int GetActivePuzzleId();

    // Configuration
    bool IsAbsencePuzzle(int puzzleId);
    float GetPerfectMatchThreshold(int puzzleId);

    // Events (dispatched via GameEvent):
    //   Evt_MatchScoreChanged { float score }
    //   Evt_NearMatchEnter { int puzzleId }
    //   Evt_NearMatchExit { int puzzleId }
    //   Evt_PerfectMatch { int puzzleId }
    //   Evt_AbsenceAccepted { int puzzleId }
    //   Evt_PuzzleComplete { int puzzleId, int chapterId }
    //   Evt_PuzzleLockAll { }
}
```

### 6.4 IChapterState

```csharp
public enum ChapterStatus { Locked, Active, Complete }

/// Serialization interface — Save System reads/writes this snapshot.
/// Chapter State owns the runtime state; Save System owns the file.
public interface IChapterProgress
{
    int CurrentChapterId { get; }
    ChapterStatus GetChapterStatus(int chapterId);
    PuzzleState GetPuzzleState(int chapterId, int puzzleIndex);
    bool[] TutorialCompleted { get; }
    Dictionary<int, bool> CollectiblesUnlocked { get; }
    int SaveVersion { get; }
}

public interface IChapterState
{
    // Queries
    IChapterProgress GetProgress();
    int GetCurrentChapterId();
    ChapterStatus GetChapterStatus(int chapterId);
    PuzzleState GetPuzzleState(int puzzleId);
    int GetActivePuzzleId();
    bool IsChapterUnlocked(int chapterId);
    bool IsReplayMode { get; }

    // Commands (via GameEvent, not direct call):
    //   Listens: Evt_PuzzleComplete → advance puzzle progression
    //   Listens: Evt_AbsenceAccepted → handle absence puzzle completion
    //   Fires:   Evt_PuzzleStateChanged { int puzzleId, PuzzleState newState }
    //   Fires:   Evt_ChapterComplete { int chapterId }
    //   Fires:   Evt_RequestSceneChange { int targetChapterId }
}
```

### 6.5 ISaveService

```csharp
public interface ISaveService
{
    UniTask<IChapterProgress> LoadAsync();
    UniTask SaveAsync(IChapterProgress progress);
    UniTask DeleteSave();
    bool HasSave { get; }

    // Auto-save control
    void TriggerAutoSave();                 // debounced (1s min interval)
    void TriggerImmediateSave();            // bypasses debounce (app pause/quit)

    // Events:
    //   Evt_SaveComplete { }
}
```

### 6.6 IHintService

```csharp
public enum HintLayer { None = 0, Ambient = 1, Directional = 2, Explicit = 3 }

public interface IHintService
{
    HintLayer GetCurrentActiveLayer();
    int GetL3UsageCount();                  // max 3 per puzzle
    int GetL3RemainingCount();
    bool IsObserving { get; }

    // Timer control (called by Tutorial system)
    void PauseTimers();
    void ResumeAndResetTimers();

    // Events:
    //   Evt_HintAvailable { int layer }
    //   Evt_HintDismissed { }
}
```

### 6.7 INarrativeEvent

```csharp
public interface INarrativeEvent
{
    bool IsPlaying { get; }
    string CurrentSequenceId { get; }

    // Playback is triggered by GameEvent, not direct call:
    //   Listens: Evt_PerfectMatch → lookup sequence → play
    //   Listens: Evt_AbsenceAccepted → lookup absence sequence → play
    //   Listens: Evt_ChapterComplete → lookup transition sequence → play

    // Events:
    //   Evt_SequenceComplete { string sequenceId }
    //   Evt_LoadNextChapter { int nextChapterId }
    //   Fires: Evt_PuzzleLockAll, Evt_PuzzleUnlock (sequence start/end)
    //   Fires: Evt_PuzzleSnapToTarget (during sequence)
    //   Fires: Evt_AudioDuckingRequest / Release
    //   Fires: Evt_PlaySFXRequest
}
```

### 6.8 IAudioService

```csharp
public enum AudioLayer { Ambient, SFX, Music }

public interface IAudioService
{
    // SFX
    void PlaySFX(string sfxId, Vector3? worldPosition = null);
    void StopAllSFX();

    // Music
    void PlayMusic(string clipId, float crossfadeDuration = 1.0f);
    void StopMusic(float fadeDuration = 1.0f);

    // Ducking (used by Narrative)
    void SetDucking(float duckRatio, float fadeDuration);
    void ReleaseDucking(float fadeDuration);

    // Volume control (used by Settings)
    void SetLayerVolume(AudioLayer layer, float volume);
    float GetLayerVolume(AudioLayer layer);
    void SetMasterVolume(float volume);
    float GetMasterVolume();

    // Lifecycle
    void PauseAll();          // OnApplicationPause(true)
    void ResumeAll();         // OnApplicationPause(false)
}
```

### 6.9 ISceneService

```csharp
public enum SceneTransitionState { Idle, TransitioningOut, Unloading, Loading, TransitioningIn, Error }

public interface ISceneService
{
    SceneTransitionState CurrentState { get; }
    int CurrentChapterSceneId { get; }      // -1 if no chapter scene loaded
    bool IsTransitioning { get; }

    // Scene transition is triggered by GameEvent:
    //   Listens: Evt_RequestSceneChange { int targetChapterId }

    // Events fired (see Section 5.3 for full list):
    //   Evt_SceneTransitionBegin / End
    //   Evt_SceneUnloadBegin
    //   Evt_SceneLoadComplete / Failed / Progress / DownloadProgress
    //   Evt_SceneReady
}
```

---

## 7. Phase 5 — ADR Audit & Traceability

### 7.1 Current ADR Status

| Item | Status |
|------|--------|
| `docs/architecture/` directory | Exists |
| Formal ADR files (`adr-*.md`) | **None exist** |
| `tr-registry.yaml` | Template only (no TRs populated) |
| `technical-preferences.md` | References ADR-001, ADR-002, ADR-003 informally |

### 7.2 Informal Decisions Already Made

以下决策在 `technical-preferences.md` 中被引用但未正式文档化：

| Informal Ref | Decision | Needs Formal ADR? |
|-------------|----------|:------------------:|
| ADR-001 | TEngine 6.0 as core framework with HybridCLR hot-reload | **Yes — CRITICAL** |
| ADR-002 | URP rendering pipeline for mobile-first shadow projection | **Yes — CRITICAL** |
| ADR-003 | Mobile-first, PC-later platform strategy | **Yes — important** |

### 7.3 Representative TR → Architecture Traceability

以下将一部分代表性 TRs 映射到本文档中的架构决策，验证覆盖率：

| TR ID | Requirement Summary | Covered by Architecture Section | Coverage |
|-------|--------------------|---------------------------------|:--------:|
| TR-concept-005 | Forbidden: sync asset loading | P1:P3 (Async-First), P4:ISceneService, P5.3 (all load paths async) | ✅ Full |
| TR-concept-006 | Forbidden: Coroutines | P1:P3 (Async-First) | ✅ Full |
| TR-concept-007 | Forbidden: direct ModuleSystem.GetModule | P1:P5 (Layer Isolation), P4 interfaces | ✅ Full |
| TR-concept-008 | Forbidden: hardcoded gameplay values | P1:P1 (Data-Driven) | ✅ Full |
| TR-concept-010 | Resource lifecycle closure | P1:P4 (Resource Lifecycle) | ✅ Full |
| TR-concept-012 | Assembly structure: Default/GameLogic/GameProto | P5.6 (init order step 3), P2 (Engine Gap) | ✅ Full |
| TR-concept-013 | Event-driven via GameEvent | P1:P2 (Event-Driven), P5.3 (event map) | ✅ Full |
| TR-save-005 | IChapterProgress decouples CS from Save | P4.4 (IChapterState), P4.5 (ISaveService) | ✅ Full |
| TR-save-018 | Save must init before gameplay reads progress | P5.6 (init order steps 10-12) | ✅ Full |
| TR-scene-001 | Additive scene architecture | P5.5, P5.6 (init order), Layer Map | ✅ Full |
| TR-scene-003 | All scene loading async UniTask | P5.5, P6.9 (ISceneService) | ✅ Full |
| TR-scene-014 | 8 scene transition GameEvent IDs | P5.3 (Scene Events table) | ✅ Full |
| TR-input-001 | Three-layer input architecture | P4.1 (IInputService), P5.1 (frame update) | ✅ Full |
| TR-input-004 | InputBlocker stack-based | P4.1 (IInputService.PushBlocker/PopBlocker) | ✅ Full |
| TR-input-005 | InputFilter whitelist-based | P4.1 (IInputService.PushFilter/PopFilter) | ✅ Full |
| TR-puzzle-001 | Multi-anchor weighted scoring | P4.3 (IShadowPuzzle.GetAnchorScores), P5.1 | ✅ Full |
| TR-puzzle-005 | Puzzle state machine | P4.3 (PuzzleState enum) | ✅ Full |
| TR-puzzle-006 | AbsenceAccepted for Ch.5 | P4.3 (IShadowPuzzle), P5.2 (flow) | ✅ Full |
| TR-hint-007 | Read-only query to Shadow Puzzle | P4.3/P4.6 boundary, P2 (Hint Consumes) | ✅ Full |
| TR-hint-009 | Hint timers pause during Tutorial | P4.6 (IHintService.PauseTimers) | ✅ Full |
| TR-narr-002 | Sequence: time-sorted atomic effects | P5.2 (puzzle complete flow), P4.7 | ✅ Full |
| TR-narr-004 | PuzzleLockAll + InputBlocker on sequence | P5.2, P5.3 (event map) | ✅ Full |
| TR-ui-001 | All UI via TEngine UIModule | P4 (all UI through UISystem), P2 (UI ownership) | ✅ Full |
| TR-ui-002 | 5 UI layer levels | P2 (UI System Owns), P3.2 (layer table) | ✅ Full |
| TR-ui-003 | Popup/Overlay auto-push InputBlocker | P2 (UI System Owns), P4.1 (IInputService) | ✅ Full |
| TR-audio-001 | 3 independent mix layers | P4.8 (IAudioService.AudioLayer enum) | ✅ Full |
| TR-audio-007 | Ducking system | P4.8 (SetDucking/ReleaseDucking), P5.3 (events) | ✅ Full |
| TR-settings-001 | 8 player settings | P2 (Settings Owns) | ✅ Full |
| TR-settings-002 | Settings in PlayerPrefs, separate from save | P2 (Settings/Save separation) | ✅ Full |
| TR-tutor-002 | InputFilter integration | P4.1 (IInputService.PushFilter) | ✅ Full |
| TR-tutor-007 | Priority: Narrative > Tutorial > Hint | P5.3 (InputBlocker from Narrative suppresses all), P4.6 (Hint pause) | ✅ Full |

### 7.4 Coverage Gap Analysis

以下 TR 域在本架构文档中**尚未完全覆盖**，需要通过 ADR 或实现规范补充：

| Gap Category | TRs Affected | What's Missing | Resolution |
|-------------|-------------|----------------|------------|
| **Performance budgets as enforcement mechanism** | TR-concept-002/003/004, TR-puzzle-010, TR-render-017/018/019, TR-objint-019/020, TR-input-016, TR-hint-013 | 本文定义了预算数值，但未定义性能监控和自动降级的架构机制 | ADR needed: Performance Monitoring & Auto-Degradation |
| **Quality tier auto-detection** | TR-render-006, TR-render-016 | 架构未定义设备能力检测和动态质量切换的模块归属 | ADR needed: Quality Tier Strategy |
| **HybridCLR assembly boundary details** | TR-concept-012/014 | 哪些类型必须放 Default 程序集 vs GameLogic 的精确清单未定义 | ADR needed: Assembly Boundary Rules |
| **Haptic feedback architecture** | TR-objint-022 | 触觉反馈的跨平台抽象层未在任何接口中定义 | Include in Object Interaction ADR |
| **Accessibility architecture** | TR-render-021/022, TR-ui-014/020 | 高对比度模式、字体缩放、动画缩放的全局架构未定义 | ADR needed: Accessibility Architecture |
| **Collectible System** | N/A (planned) | 未设计 GDD，无 TR | Design before Alpha |
| **Analytics System** | N/A (planned) | 未设计 GDD，无 TR | Design before Alpha |

---

## 8. Phase 6 — Missing ADR List

### 8.1 Must Have Before Coding (Foundation + Core)

这些 ADR 阻塞 Sprint 1 的实现启动：

| ADR ID | Title | 涉及系统 | Priority | 依赖 |
|--------|-------|---------|:--------:|------|
| **ADR-001** | TEngine 6.0 Framework Adoption | All | **P0** | None |
| **ADR-002** | URP Rendering Pipeline for Shadow Projection | URP Shadow Rendering | **P0** | None |
| **ADR-003** | Mobile-First Platform Strategy | All | **P0** | None |
| **ADR-004** | HybridCLR Assembly Boundary Rules | All hot-fix code | **P0** | ADR-001 |
| **ADR-005** | YooAsset Resource Loading & Lifecycle Pattern | Scene Mgmt, all asset loading | **P0** | ADR-001 |
| **ADR-006** | GameEvent Communication Protocol (Event ID allocation, payload conventions) | All inter-module communication | **P0** | ADR-001 |
| **ADR-007** | Luban Config Table Access Pattern | All config-driven systems | **P0** | ADR-004 |
| **ADR-008** | Save System Architecture (format, migration, integrity) | Save System, Chapter State | **P0** | None |
| **ADR-009** | Scene Lifecycle & Additive Scene Strategy | Scene Mgmt | **P0** | ADR-005 |
| **ADR-010** | Input Abstraction (Gesture recognition, Blocker/Filter stacks) | Input System | **P0** | None |
| **ADR-011** | UIWindow Management & Layer Strategy | UI System | **P0** | ADR-001 |

### 8.2 Should Have Before System Is Built (Feature Layer)

这些 ADR 在对应 Feature 系统的 Sprint 开始前完成即可：

| ADR ID | Title | 涉及系统 | Priority | 依赖 |
|--------|-------|---------|:--------:|------|
| **ADR-012** | Shadow Match Algorithm (multi-anchor weighted scoring) | Shadow Puzzle | **P1** | ADR-002 |
| **ADR-013** | Object Interaction State Machine & Snap Mechanics | Object Interaction | **P1** | ADR-010 |
| **ADR-014** | Puzzle State Machine & Absence Puzzle Variant | Shadow Puzzle, Chapter State | **P1** | ADR-008 |
| **ADR-015** | Hint System Trigger Formula & Escalation Logic | Hint System | **P1** | ADR-012 |
| **ADR-016** | Narrative Sequence Engine (atomic effects, timeline, config) | Narrative Event | **P1** | ADR-006, ADR-007 |
| **ADR-017** | Audio Mix Architecture (3-layer, ducking, crossfade) | Audio System | **P1** | ADR-001 |
| **ADR-018** | Performance Monitoring & Auto-Degradation Strategy | All (cross-cutting) | **P1** | ADR-002 |

### 8.3 Can Defer (Presentation Layer, Optimization)

这些 ADR 可在 Vertical Slice 或 Alpha 阶段按需创建：

| ADR ID | Title | 涉及系统 | Priority | 依赖 |
|--------|-------|---------|:--------:|------|
| **ADR-019** | Tutorial Step Engine & InputFilter Integration | Tutorial | **P2** | ADR-010 |
| **ADR-020** | Accessibility Architecture (contrast modes, font scaling, animation scaling) | Settings, UI, Rendering | **P2** | ADR-011, ADR-002 |
| **ADR-021** | Quality Tier Auto-Detection & Dynamic Switching | Rendering, Scene | **P2** | ADR-002, ADR-018 |
| **ADR-022** | I2 Localization Integration & String Management | Settings, UI | **P2** | ADR-011 |
| **ADR-023** | Collectible System Architecture | Collectible (planned) | **P2** | ADR-008 |
| **ADR-024** | Analytics & Telemetry Architecture | Analytics (planned) | **P2** | ADR-006 |
| **ADR-025** | Haptic Feedback Cross-Platform Abstraction | Object Interaction | **P2** | ADR-013 |
| **ADR-026** | Video Playback Strategy for Narrative (H.264 vs VP8, memory) | Narrative | **P2** | ADR-016 |

### 8.4 ADR Summary

| Category | Count | Deadline |
|----------|:-----:|----------|
| Must Have Before Coding (P0) | **11** | Sprint 0 / Sprint 1 planning |
| Should Have Before System Build (P1) | **7** | Before each system's Sprint |
| Can Defer (P2) | **8** | Vertical Slice / Alpha |
| **Total** | **26** | — |

---

## 9. Open Questions

以下问题需要在 Sprint 0 或对应系统实现前解决：

| # | Question | 影响系统 | Owner | 建议解决时间 |
|---|----------|---------|-------|------------|
| 1 | TEngine `GameEvent.Send` 的 payload 支持什么类型？是否支持 struct/class payload，还是只支持 int 参数？需要验证源码。如果不支持复杂 payload，事件通信方案需要调整为 static event bus 或额外的 payload registry。 | All event communication | TD + Lead Programmer | Sprint 0 spike |
| 2 | TEngine `UIWindow` 的生命周期回调（`OnCreate`/`OnRefresh`/`OnUpdate`/`OnClose`）的确切调用时序需要从源码验证。特别是 `OnRefresh` vs `OnCreate` 在首次打开时的行为。 | UI System | Lead Programmer | Sprint 0 spike |
| 3 | YooAsset `ResourcePackage` 的多包初始化策略：项目是否需要多个 ResourcePackage（一个给场景，一个给共享资源），还是单包策略足够？影响 ADR-005 的决策。 | Scene Mgmt, all asset loading | TD | Sprint 0 |
| 4 | Luban 生成代码中 `Tables` 单例的线程安全性——如果在 async 上下文中从多个 UniTask 并发访问，是否需要额外同步？ | All config-driven systems | Lead Programmer | Sprint 0 |
| 5 | WallReceiver 自定义 shader 的 URP 兼容性——需要确认 URP 2022.3 的 ShaderGraph 是否支持自定义 shadow sampling pass，还是必须写纯 HLSL。 | URP Shadow Rendering | Technical Artist | Sprint 0 prototype |
| 6 | `PuzzleLockAllEvent` 有两个合法发送者（Shadow Puzzle 和 Narrative），Object Interaction 不关心发送者——但是否需要一个 sequence number 或 token 来防止错误的 unlock（如 Narrative pop 了 Puzzle 的 lock）？ | Object Interaction, Narrative, Puzzle | TD | ADR-006 讨论 |
| 7 | HybridCLR 对 `AsyncGPUReadback` 回调中 hot-fix 代码的支持是否存在已知限制？ShadowRT 读回在 GPU callback 中执行，需确认 AOT 兼容性。 | URP Shadow Rendering | TD + Engine Programmer | Sprint 0 spike |
| 8 | 章末最后谜题的"合并序列"（记忆重现 + 章节过渡无缝衔接）在 Luban 配置表中如何表达？是两个 sequence 的 chain，还是一个合并的长 sequence？ | Narrative Event | Game Designer + TD | ADR-016 |
| 9 | I2 Localization 与 TEngine 的集成方式——TEngine 是否自带 localization 包装，还是 I2 独立于 TEngine 运行？影响 ADR-022。 | Settings, UI | Lead Programmer | Sprint 1 |
| 10 | 性能自动降级（TR-render-016: 连续 5 帧 > 20ms 降一档）是由 URP Shadow Rendering 自行管理，还是由一个全局 Performance Monitor 模块统一管理所有系统的降级？ | Cross-cutting | TD | ADR-018 |

---

## Appendix A: Forbidden Patterns Quick Reference

供所有开发者日常参考的禁止模式速查表：

| ❌ Forbidden | ✅ Correct Alternative | 对应 TR |
|-------------|----------------------|--------|
| `Resources.Load<T>(path)` | `await GameModule.Resource.LoadAssetAsync<T>(path)` | TR-concept-005 |
| `StartCoroutine(...)` | `async UniTask Method()` | TR-concept-006 |
| `ModuleSystem.GetModule<T>()` | `GameModule.Audio` / `GameModule.UI` / etc. | TR-concept-007 |
| `float threshold = 0.85f;` (hardcoded) | `Tables.TbPuzzle.Get(id).PerfectMatchThreshold` | TR-concept-008 |
| `GameObject.Find("ObjectName")` | Serialized reference / DI / event-based discovery | TR-concept-009 |
| `LoadAssetAsync` without `UnloadAsset` | Always pair load/unload; use `using` pattern where possible | TR-concept-010 |
| `SceneManager.LoadScene(name)` (sync) | `await GameModule.Resource.LoadSceneAsync(name, Additive)` | TR-scene-003 |
| `SceneManager.LoadScene(name, Single)` | Always `LoadSceneMode.Additive` | TR-scene-004 |
| UI text `"确定"` (hardcoded string) | `LocalizationManager.GetTranslation("btn_confirm")` | TR-ui-021 |

## Appendix B: Assembly Boundary Summary

```
┌──────────────────────────────────────────────────────────────────┐
│  Default Assembly (非热更, BootScene)                             │
│  ├── TEngine RootModule bootstrapper                             │
│  ├── HybridCLR metadata registration                            │
│  ├── YooAsset package initialization                             │
│  └── ProcedureLaunch / ProcedureMain entry points                │
├──────────────────────────────────────────────────────────────────┤
│  GameLogic Assembly (热更, 全部 gameplay 代码)                    │
│  ├── All interfaces (IInputService, IShadowPuzzle, etc.)         │
│  ├── All implementations (InputManager, PuzzleManager, etc.)     │
│  ├── All MonoBehaviour components (InteractableObject, etc.)     │
│  ├── GameEvent ID constants (EventId.cs)                         │
│  └── GameEntry / GameApp hot-fix entry point                     │
├──────────────────────────────────────────────────────────────────┤
│  GameProto Assembly (热更, Luban 生成代码)                        │
│  ├── Tables singleton                                            │
│  ├── TbChapter, TbPuzzle, TbPuzzleObject, TbNarrativeSequence   │
│  ├── TbAudioEvent, TbHintConfig, TbTutorialStep, TbSettings     │
│  └── All auto-generated data classes                             │
└──────────────────────────────────────────────────────────────────┘
```

---

*End of Master Architecture Document*
