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
- **Signals/Events**: PascalCase with `On` prefix for callbacks (e.g. `OnPuzzleSolved`); int constants for `GameEvent` (e.g. `EventId.ShadowMatched`)
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
- ADR-001: TEngine as core framework — modular architecture with HybridCLR hot-reload
- ADR-002: URP rendering pipeline — mobile-first with shadow projection requirements
- ADR-003: Mobile-first, PC-later platform strategy
- [Use /architecture-decision to create formal ADR documents]

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
