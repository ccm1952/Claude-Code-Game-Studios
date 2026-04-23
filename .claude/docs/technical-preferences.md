# Technical Preferences

<!-- Populated by /setup-engine. Updated as the user makes decisions throughout development. -->
<!-- All agents reference this file for project-specific standards and conventions. -->
<!-- Last configured: 2026-04-16 for ShadowGame (影子回忆解谜游戏) -->

## Engine & Language

- **Engine**: Unity 2022.3.62f2 (LTS)
- **Language**: C# (with HybridCLR hot-reload)
- **Rendering**: URP (Universal Render Pipeline)
- **Physics**: Unity Built-in Physics / Physics 2D

## Input & Platform

<!-- Written by /setup-engine. Read by /ux-design, /ux-review, /test-setup, /team-ui, and /dev-story -->
<!-- to scope interaction specs, test helpers, and implementation to the correct input methods. -->

- **Target Platforms**: Mobile (iOS / Android) — PC (Steam) planned for later phase
- **Input Methods**: Touch (primary), Keyboard/Mouse (secondary, for future PC version)
- **Primary Input**: Touch
- **Gamepad Support**: None (initial phase); Partial (PC phase)
- **Touch Support**: Full
- **Platform Notes**: 移动端优先设计；UI 需适配刘海屏/安全区（TEngine `SetUISafeFitHelper`）；后续 PC 版需适配键鼠交互；影子投影渲染需兼顾移动端 GPU 性能

## Naming Conventions

- **Classes**: PascalCase (e.g. `ShadowPuzzleManager`, `LightSourceController`)
- **Variables**: camelCase for private fields with `_` prefix (e.g. `_shadowTarget`); PascalCase for public properties
- **Signals/Events**: PascalCase with `On` prefix for callbacks (e.g. `OnPuzzleSolved`); GameEvent 使用 TEngine `[EventInterface]` C# 接口（ADR-027），接口命名 `I{Domain}Event`（业务）/ `I{PanelName}UI`（UI），方法命名 `On{Action}`；**禁止新增 `public const int Evt_Xxx` 事件 ID**
- **Files**: PascalCase matching class name (e.g. `ShadowPuzzleManager.cs`)
- **Scenes/Prefabs**: PascalCase (e.g. `MainScene.unity`, `ShadowObject.prefab`)
- **Constants**: UPPER_SNAKE_CASE (e.g. `MAX_SHADOW_OBJECTS`)

## Performance Budgets

- **Target Framerate**: 60 FPS (mobile & PC)
- **Frame Budget**: 16.67ms
- **Draw Calls**: < 150 (mobile); < 300 (PC)
- **Memory Ceiling**: 1.5 GB (mobile); 4 GB (PC)

## Testing

- **Framework**: Unity Test Framework (NUnit)
- **Minimum Coverage**: 70% for gameplay logic systems (puzzle mechanics, shadow calculation)
- **Required Tests**: Shadow matching algorithms, puzzle state machines, resource load/unload lifecycle, event dispatch correctness

## Forbidden Patterns

<!-- Patterns that must never appear in this project's codebase -->
- Synchronous asset loading (`Resources.Load`, `AssetBundle.LoadAsset` — use `LoadAssetAsync` via YooAsset)
- Coroutines for async operations (use `UniTask` exclusively)
- Direct module access via `ModuleSystem.GetModule<T>()` (use `GameModule.XXX` accessors)
- Hardcoded gameplay values (must be data-driven via Luban config tables)
- `GameObject.Find` / `FindObjectOfType` at runtime (use references or DI)
- Forgetting to call `UnloadAsset` after `LoadAssetAsync` (resource leak)

## Assembly Definition (asmdef) Rules

<!-- Critical lesson learned: TEngine uses a Roslyn Source Generator (EventInterfaceGenerator) -->
<!-- that runs on ALL assemblies referencing GameLogic. If an asmdef references GameLogic but -->
<!-- not TEngine.Runtime, the generated GameEventHelper.g.cs fails to resolve the TEngine namespace. -->

- **Any asmdef that references `GameLogic` MUST also reference `TEngine.Runtime`**
  - Reason: TEngine's Roslyn Source Generator (`EventInterfaceGenerator`) emits `GameEventHelper.g.cs` into every assembly that transitively touches GameLogic. Without `TEngine.Runtime`, the generated code fails: `CS0246: 'TEngine' could not be found`.
  - This applies to: test asmdefs (`EditModeTests`, `PlayModeTests`), tool asmdefs, editor extensions — anything referencing `GameLogic`.
- **Minimum references for a test asmdef referencing GameLogic**:
  ```json
  "references": ["GameLogic", "TEngine.Runtime"]
  ```
- **If adding a NEW asmdef** that references GameLogic, always check: does it compile with `GameEventHelper.g.cs`? If not, add `TEngine.Runtime`.

## Allowed Libraries / Addons

<!-- Approved third-party dependencies -->
- **TEngine** 6.0.0 — Core framework (modules, procedures, UI, events)
- **HybridCLR** — C# hot-reload for `GameScripts/HotFix/` assemblies
- **YooAsset** 2.3.17 — Asset management, hot-update, resource loading
- **UniTask** 2.5.10 — Zero-allocation async/await for Unity
- **DOTween** — Tweening animations
- **Luban** — Configuration table generation and access
- **TextMeshPro** — Text rendering
- **I2 Localization** — Multi-language support (observed in project)

## Architecture Decisions Log

<!-- Quick reference linking to full ADRs in docs/architecture/ -->
- ADR-001: TEngine 6.0 Framework Adoption → `docs/architecture/adr-001-tengine-framework.md`
- ADR-002: URP Rendering Pipeline for Shadow Projection → `docs/architecture/adr-002-urp-shadow-rendering.md`
- ADR-003: Mobile-First Platform Strategy → `docs/architecture/adr-003-mobile-first-platform.md`
- ADR-004: HybridCLR Assembly Boundary Rules → `docs/architecture/adr-004-hybridclr-assembly.md`
- ADR-005: YooAsset Resource Loading & Lifecycle → `docs/architecture/adr-005-yooasset-lifecycle.md`
- ~~ADR-006: GameEvent Communication Protocol~~ *superseded by ADR-027* → `docs/architecture/adr-006-gameevent-protocol.md`
- ADR-007: Luban Config Table Access Pattern → `docs/architecture/adr-007-luban-access.md`
- ADR-008: Save System Architecture → `docs/architecture/adr-008-save-system.md`
- ADR-009: Scene Lifecycle & Additive Scene Strategy → `docs/architecture/adr-009-scene-lifecycle.md`
- ADR-010: Input Abstraction (Gesture/Blocker/Filter) → `docs/architecture/adr-010-input-abstraction.md`
- ADR-011: UIWindow Management & Layer Strategy → `docs/architecture/adr-011-uiwindow-management.md`
- ADR-012: Shadow Match Algorithm → `docs/architecture/adr-012-shadow-match-algorithm.md`
- ADR-013: Object Interaction State Machine → `docs/architecture/adr-013-object-interaction.md`
- ADR-014: Puzzle State Machine & Absence Puzzle → `docs/architecture/adr-014-puzzle-state-machine.md`
- ADR-015: Hint System Trigger Formula → `docs/architecture/adr-015-hint-system.md`
- ADR-016: Narrative Sequence Engine → `docs/architecture/adr-016-narrative-sequence-engine.md`
- ADR-017: Audio Mix Architecture → `docs/architecture/adr-017-audio-mix.md`
- ADR-018: Performance Monitoring & Auto-Degradation → `docs/architecture/adr-018-performance-monitoring.md`
- ADR-019 ~ ADR-026: Presentation Layer (P2, 可延后)
- **ADR-027: GameEvent Interface Protocol (supersedes ADR-006 §1/§2)** → `docs/architecture/adr-027-gameevent-interface-protocol.md`

## Engine Specialists

<!-- Written by /setup-engine when engine is configured. -->
<!-- Read by /code-review, /architecture-decision, /architecture-review, and team skills -->
<!-- to know which specialist to spawn for engine-specific validation. -->

- **Primary**: Unity C# Specialist (TEngine framework)
- **Language/Code Specialist**: Unity C# Specialist — HotFix assembly code, UniTask async patterns, TEngine module API
- **Shader Specialist**: Unity URP Shader Specialist — Shadow projection shaders, light/shadow rendering, mobile shader optimization
- **UI Specialist**: Unity UGUI / TEngine UIModule Specialist — UIWindow lifecycle, UIWidget binding, safe area fitting
- **Additional Specialists**: Luban Config Specialist (table schema & code generation), YooAsset Build Specialist (asset bundling & hot-update)
- **Routing Notes**: All gameplay code routes through TEngine conventions; wiki-query-agent should be consulted for TEngine API usage (L2+ tasks per ShadowGame CLAUDE.md)

### File Extension Routing

<!-- Skills use this table to select the right specialist per file type. -->

| File Extension / Type | Specialist to Spawn |
|-----------------------|---------------------|
| `.cs` (C# game code) | Unity C# Specialist (TEngine) |
| `.shader` / `.hlsl` / `.shadergraph` | Unity URP Shader Specialist |
| `.prefab` (UI prefabs) | Unity UGUI / TEngine UIModule Specialist |
| `.unity` / `.prefab` (scenes & world prefabs) | Unity Scene Specialist |
| `.asset` / `.mat` / `.renderTexture` | Unity URP Shader Specialist |
| `.asmdef` (assembly definitions) | Unity C# Specialist (TEngine) |
| `.json` / `.bytes` (Luban config) | Luban Config Specialist |
| General architecture review | Primary |
