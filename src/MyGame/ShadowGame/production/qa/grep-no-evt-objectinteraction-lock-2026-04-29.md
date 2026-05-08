// 该文件由Cursor 自动生成

# Grep Evidence — S2-12 InteractionLockManager Protocol Compliance (ADR-006 → ADR-027 Migration)

**Date**: 2026-04-29
**Story**: S2-12 InteractionLockManager (`production/epics/object-interaction/story-006-interaction-lock-manager.md`)
**Scope**: `Assets/GameScripts/HotFix/`（含 GameLogic + GameProto；不含 TEngine 框架内核）

## 验证目标

确认 S2-12 实施期间：
1. **0** 处使用 `EventId.Evt_PuzzleLockAll` / `Evt_PuzzleUnlock` / `Evt_SceneUnloadBegin`（ADR-006 全废）
2. **0** 处使用 `PuzzleLockAllPayload` / `PuzzleUnlockPayload` / `SceneUnloadBeginPayload` struct（ADR-027 §2 废 payload struct）
3. 所有 sender 派发用 `GameEvent.Get<IInteractionEvent>().OnXxx(...)` / `GameEvent.Get<ISceneEvent>().OnXxx(...)`
4. Listener 端用 per-event 模式 `GameEvent.AddEventListener<TArg>(IXxxEvent_Event.OnYyy, handler)`

## 命令 & 结果

```bash
# §1 — Evt_PuzzleLockAll / Evt_PuzzleUnlock / Evt_SceneUnloadBegin 残留
rg "EventId\.Evt_(PuzzleLockAll|PuzzleUnlock|SceneUnloadBegin)" \
   src/MyGame/ShadowGame/Assets/GameScripts/HotFix/
# → 0 匹配 ✅

# §2 — Payload struct 残留
rg "(PuzzleLockAllPayload|PuzzleUnlockPayload|SceneUnloadBeginPayload)" \
   src/MyGame/ShadowGame/Assets/GameScripts/HotFix/
# → 0 匹配 ✅

# §3 — Object Interaction 子目录全部 EventId.Evt_* 残留
rg "EventId\.Evt_" \
   src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/
# → 0 匹配 ✅（接续 S2-08/S2-09/S2-10 的 0 命中状态）
```

## Sender / Listener 协议合规路径（实测）

### Sender 端

```csharp
// InteractionLockManager.PushLock / PopLock / OnSceneUnloadBegin
GameEvent.Get<IInteractionEvent>().OnInteractionLockChanged(true | false);
```

✅ 派发用 `GameEvent.Get<IInteractionEvent>()` facade（ADR-027 §3 推荐路径），**不**经 `EventId.Evt_*` 常量。

### Listener 端（per-event 模式）

```csharp
// InteractionLockManager.Init() 注册 3 个 listener
GameEvent.AddEventListener<string>(
    IInteractionEvent_Event.OnRequestPuzzleLockAll, OnRequestPuzzleLockAll);
GameEvent.AddEventListener<string>(
    IInteractionEvent_Event.OnRequestPuzzleUnlock, OnRequestPuzzleUnlock);
GameEvent.AddEventListener<int>(
    ISceneEvent_Event.OnSceneUnloadBegin, OnSceneUnloadBegin);
```

✅ Per-event 模式（X1 patch v2 修订对齐 framework 实际能力）；**不**用 `class : IInteractionEvent, ISceneEvent` + `AddEventListener<TInterface>(this)` 整接口订阅风格（TEngine 不支持）。

## 测试端协议合规

`InteractionLockManagerTests.cs` 13 NUnit：
- AC-3/AC-4/PushLock_NullOrEmpty 用 `LogAssert.Expect(LogType.Warning, regex)` 显式声明预期 warning
- AC-6 用 `GameEvent.Get<IInteractionEvent>().OnRequestPuzzleLockAll(...)` 端到端 round-trip 验证
- AC-4 用 `GameEvent.Get<ISceneEvent>().OnSceneUnloadBegin(1)` 模拟 sender（S2-05 sender 尚未实装；接口签名冻结）

## 结论

**PASS** — S2-12 实施 0 处 ADR-006 残留，0 处 payload struct 残留；Listener 端 per-event 模式与 ADR-027 §3 + 既有 InteractableObject (S2-08+) 模式一致。
