# Unity Engine — Version Reference

| Field | Value |
|-------|-------|
| **Engine Version** | Unity 2022.3.62f2 (LTS) |
| **Release Date** | 2022 LTS stream (patch 62f2) |
| **Project Pinned** | 2026-04-16 |
| **Last Docs Verified** | 2026-04-16 |
| **LLM Knowledge Cutoff** | May 2025 |

## Knowledge Gap Warning

The LLM's training data covers Unity 2022 LTS well. This project deliberately stays
on the 2022.3 LTS track for stability with TEngine / HybridCLR / YooAsset ecosystem.
**Do NOT suggest Unity 6 APIs** — this project is on 2022.3, not Unity 6.

## Project Technology Stack

| Dependency | Version | Purpose |
|-----------|---------|---------|
| **TEngine** | 6.0.0 | Core framework (modules, procedures, UI, events) |
| **HybridCLR** | latest compatible | C# hot-reload for HotFix assemblies |
| **YooAsset** | 2.3.17 | Asset management, hot-update, resource loading |
| **UniTask** | 2.5.10 | Zero-allocation async/await |
| **DOTween** | SDK | Tweening animations |
| **Luban** | latest | Configuration table generation |
| **URP** | 2022.3 compatible | Universal Render Pipeline |

## Assembly Architecture

| Assembly | Hot-Reloadable | Scope |
|----------|---------------|-------|
| Default (`Assembly-CSharp`) | No | `GameEntry.cs`, `Procedure/*.cs` — bootstrap & resource init |
| `GameLogic` | Yes (HybridCLR) | `HotFix/GameLogic/` — all gameplay code, UI, modules |
| `GameProto` | Yes (HybridCLR) | `HotFix/GameProto/` — Luban configs, protocol definitions |
| `TEngine.Runtime` | No | Framework runtime core |

## Key API Patterns (2022.3 Verified)

- **Async loading**: `YooAssets.LoadAssetAsync<T>()` → must call `handle.Release()` or `UnloadAsset`
- **Module access**: `GameModule.Resource`, `GameModule.Audio`, etc. — NOT `ModuleSystem.GetModule<T>()`
- **UI lifecycle**: `UIWindow.Create()` → `OnCreate` → `OnRefresh` → `OnClose` → `OnDestroy`
- **Events**: `GameEvent.Send(eventId)` / `GameEvent.AddEventListener(eventId, handler)`
- **Async pattern**: `async UniTaskVoid` / `async UniTask` — never Coroutine

## Verified Sources

- Official docs: https://docs.unity3d.com/2022.3/Documentation/Manual/index.html
- Unity 2022 LTS: https://unity.com/releases/2022-lts
- URP docs: https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/index.html
- C# API reference: https://docs.unity3d.com/2022.3/Documentation/ScriptReference/index.html
- HybridCLR: https://hybridclr.doc.code-philosophy.com/
- YooAsset: https://www.yooasset.com/
- UniTask: https://github.com/Cysharp/UniTask
