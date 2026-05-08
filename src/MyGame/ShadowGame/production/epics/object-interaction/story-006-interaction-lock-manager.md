// 该文件由Cursor 自动生成

# Story 006: InteractionLockManager with HashSet Token (SP-006)

> **Epic**: Object Interaction
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-29 night (X1 patch v2: §Implementation Notes 修订 — InteractionLockManager **不**实施 IInteractionEvent/ISceneEvent 接口，改为 POCO + per-event listener 注册；TEngine 不支持"整接口订阅" `GameEvent.AddEventListener<TInterface>(this)` 模式，必须用 `GameEvent.AddEventListener<TArg>(int eventId, Action<TArg> handler)` 签名 — 详见 `/.claude/memory/problem_2026-04-29_story-impl-notes-vs-framework-drift.md`)

## Context

**GDD**: `design/gdd/object-interaction.md`
**Requirement**: `TR-objint-016`
*(PuzzleLock events; multi-sender safe locking)*

**ADR Governing Implementation**: ADR-013（**Accepted**, 2026-04-29）+ ADR-027 (GameEvent Interface Protocol) + SP-006 (Sprint 0 finding)
**ADR Decision Summary**: `InteractionLockManager` 用 `HashSet<string>` token 锁集合：`PushLock(token)` 添加 token，`PopLock(token)` 移除；`IsLocked == (set.Count > 0)`。状态变化（空↔非空）时派发 `IInteractionEvent.OnInteractionLockChanged(bool)` 通知所有 InteractableObjectFsm。三个预定义 lockerId 常量。SP-006 已确认本设计可防止 Narrative 与 Shadow Puzzle 锁定错配。

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW
**Engine Notes**: `InteractionLockManager` 是 POCO（不继承 MonoBehaviour，**不**实施 IInteractionEvent/ISceneEvent 接口本身），由 `InteractionCoordinator`（Story 007）实例化并 `Init()` / `Dispose()`。三个事件用 **per-event 模式**订阅：`GameEvent.AddEventListener<string>(IInteractionEvent_Event.OnRequestPuzzleLockAll, OnRequestPuzzleLockAll)` 等三条。`ISceneEvent.OnSceneUnloadBegin` 用于场景过渡时强制清空 token 集合（防泄漏）。

> **TEngine 整接口订阅 API 不存在的发现（X1 patch v2 修订原因）**：原 §Implementation Notes 用 `GameEvent.AddEventListener<IInteractionEvent>(this)` 风格"整接口订阅"，与 framework 实际 API 不符 —— `EventMgr.RegWrapInterface<T>(T)` 是 sender 端代理注册（让 `_Gen` 类拿 dispatcher），listener 端只支持 `GameEvent.AddEventListener<TArg>(int eventId, Action<TArg> handler)` per-event 签名。本次修订对齐 S2-08/S2-09 InteractableObject 既有模式。

**Control Manifest Rules (this layer)**:
- Required: `PuzzleLockAll uses HashSet<string> token-based locking — objects locked when set non-empty; unlocked only when set empty` (SP-006, ADR-013)
- Required: `Legal locker IDs as predefined constants: InteractionLockerId.ShadowPuzzle="shadow_puzzle", InteractionLockerId.Narrative="narrative", InteractionLockerId.Tutorial="tutorial"` (SP-006)
- Required: `Unlock with unknown token is a no-op with warning log` (ADR-027 §3 / SP-006)
- Required: `On ISceneEvent.OnSceneUnloadBegin, lock token set is force-cleared` (ADR-009)
- Required: `Register all listeners in Init(); remove all in Dispose()` (ADR-027 §3)
- Required: `仅在 set 由空→非空 / 由非空→空 时派发 OnInteractionLockChanged；中间增量不派发` (ADR-013 IInteractionEvent §6)
- Forbidden: `Never use Stack for locking — LIFO ordering is not guaranteed between senders` (SP-006)
- Forbidden: `禁止使用 EventId.Evt_PuzzleLockAll / Evt_PuzzleUnlock / Evt_SceneUnloadBegin 等 ADR-006 常量`

---

## Acceptance Criteria

*From GDD `design/gdd/object-interaction.md` + ADR-027 + SP-006，scoped to this story:*

- [x] `InteractionLockManager` 类实施 `PushLock(string token)` 和 `PopLock(string token)` 方法（公开 API 供 Coordinator 直接调用 + 单测 bypass listener）
- [x] `IsLocked` 属性返回 `_activeLocks.Count > 0`（额外暴露 `ActiveLockCount` 供测试诊断）
- [x] `InteractionLockerId` 静态类定义 3 个常量：`ShadowPuzzle = "shadow_puzzle"`, `Narrative = "narrative"`, `Tutorial = "tutorial"`；并提供 `static readonly string[] All`
- [x] `Init()` 用 **per-event** 模式注册 3 个 listener：`GameEvent.AddEventListener<string>(IInteractionEvent_Event.OnRequestPuzzleLockAll, OnRequestPuzzleLockAll)` / `OnRequestPuzzleUnlock` / `GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneUnloadBegin, OnSceneUnloadBegin)` —— **修订**：原 `AddEventListener<IInteractionEvent>(this)` 整接口订阅 API 在 TEngine 不存在
- [x] `Dispose()` 取消所有注册 + 清空 set（不派 OnInteractionLockChanged，因即将销毁）；幂等
- [x] private listener `OnRequestPuzzleLockAll(string)` → 委托 `PushLock`
- [x] private listener `OnRequestPuzzleUnlock(string)` → 委托 `PopLock`
- [x] private listener `OnSceneUnloadBegin(int)`：如 `_activeLocks.Count > 0` → `Log.Warning("[InteractionLock] Force-clearing N leaked lock(s) on scene unload (chapter=...)")`；然后 `_activeLocks.Clear()` + 派 `OnInteractionLockChanged(false)`
- [x] **Lock 状态变化派发**：仅在 set "空↔非空" transition 时派发；中间增量幂等无派发
- [x] **不**实施 `IInteractionEvent` / `ISceneEvent` 接口本身（POCO + per-event listener 即可）— 修订自原"接口契约要求全实施"
- [x] `PopLock` 未知 token：`Log.Warning` 列出 `InteractionLockerId.All` 内所有合法 ID + no-op + 不派
- [x] **多 sender 测试场景**：覆盖 `PushLock(shadow) → PushLock(narrative) → PopLock(narrative) → IsLocked==true → PopLock(shadow) → IsLocked==false`；OnInteractionLockChanged 仅 2 次（边界 transition）
- [x] 重复 PushLock 同 token：HashSet 去重；OnInteractionLockChanged 不重复派发
- [x] **协议合规**：0 `EventId.Evt_*` 残留；sender 调用形如 `GameEvent.Get<IInteractionEvent>().OnInteractionLockChanged(...)`
- [x] **防御性**：null/empty token push → `Log.Warning` + ignore（X1 patch v2 加固）

---

## Implementation Notes

*Derived from SP-006 + ADR-013 §"Risks" + ADR-027（X1 patch v2 已修订对齐 framework 实际能力）：*

```csharp
using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// POCO，**不**实施 IInteractionEvent/ISceneEvent 接口（per-event listener 即可）
    public sealed class InteractionLockManager
    {
        private readonly HashSet<string> _activeLocks = new HashSet<string>();
        private bool _listenersRegistered;
        public bool IsLocked => _activeLocks.Count > 0;
        public int ActiveLockCount => _activeLocks.Count;   // test diagnostic

        public void Init()
        {
            if (_listenersRegistered) return;   // 幂等
            GameEvent.AddEventListener<string>(
                IInteractionEvent_Event.OnRequestPuzzleLockAll, OnRequestPuzzleLockAll);
            GameEvent.AddEventListener<string>(
                IInteractionEvent_Event.OnRequestPuzzleUnlock, OnRequestPuzzleUnlock);
            GameEvent.AddEventListener<int>(
                ISceneEvent_Event.OnSceneUnloadBegin, OnSceneUnloadBegin);
            _listenersRegistered = true;
        }

        public void Dispose()
        {
            if (_listenersRegistered)
            {
                GameEvent.RemoveEventListener<string>(
                    IInteractionEvent_Event.OnRequestPuzzleLockAll, OnRequestPuzzleLockAll);
                GameEvent.RemoveEventListener<string>(
                    IInteractionEvent_Event.OnRequestPuzzleUnlock, OnRequestPuzzleUnlock);
                GameEvent.RemoveEventListener<int>(
                    ISceneEvent_Event.OnSceneUnloadBegin, OnSceneUnloadBegin);
                _listenersRegistered = false;
            }
            _activeLocks.Clear();
        }

        // 公开 API（供 Coordinator + 单测 bypass listener）
        public void PushLock(string lockerId)
        {
            if (string.IsNullOrEmpty(lockerId)) {
                Log.Warning("[InteractionLock] PushLock 收到空 lockerId — 已忽略");
                return;
            }
            bool wasEmpty = _activeLocks.Count == 0;
            bool added = _activeLocks.Add(lockerId);
            if (added && wasEmpty)
                GameEvent.Get<IInteractionEvent>().OnInteractionLockChanged(true);
        }

        public void PopLock(string lockerId)
        {
            if (!_activeLocks.Remove(lockerId)) {
                Log.Warning(
                    $"[InteractionLock] Unknown locker: '{lockerId}'. Valid IDs: {string.Join(", ", InteractionLockerId.All)}");
                return;
            }
            if (_activeLocks.Count == 0)
                GameEvent.Get<IInteractionEvent>().OnInteractionLockChanged(false);
        }

        // private listener — per-event 注册（参 Engine Notes X1 patch v2 修订说明）
        private void OnRequestPuzzleLockAll(string lockerId) => PushLock(lockerId);
        private void OnRequestPuzzleUnlock(string lockerId) => PopLock(lockerId);
        private void OnSceneUnloadBegin(int chapterId)
        {
            if (_activeLocks.Count == 0) return;
            int leaked = _activeLocks.Count;
            Log.Warning(
                $"[InteractionLock] Force-clearing {leaked} leaked lock(s) on scene unload (chapter={chapterId}). Leaked: [{string.Join(", ", _activeLocks)}]");
            _activeLocks.Clear();
            GameEvent.Get<IInteractionEvent>().OnInteractionLockChanged(false);
        }
    }

    public static class InteractionLockerId
    {
        public const string ShadowPuzzle = "shadow_puzzle";
        public const string Narrative    = "narrative";
        public const string Tutorial     = "tutorial";
        public static readonly string[] All = { ShadowPuzzle, Narrative, Tutorial };
    }
}
```

`InteractionLockManager` 由 `InteractionCoordinator`（Story 007）`Init` 中实例化并 `Init()`，`Dispose` 中 `Dispose()`。InteractableObjectFsm（Story 001）通过自身 `IInteractionEvent.OnInteractionLockChanged` listener 接收锁状态变化并调 `fsm.OnLockChanged(isLocked)` —— 此 listener 在 S2-08 InteractableObject 中已 wired。

> **X1 patch v2 修订原因**：原 §Implementation Notes 用 `class InteractionLockManager : IInteractionEvent, ISceneEvent` + `GameEvent.AddEventListener<TInterface>(this)` 模式，与 TEngine 实际 listener API 不符（TEngine 仅支持 per-event 签名 `AddEventListener<TArg>(int eventId, Action<TArg> handler)`；`EventMgr.RegWrapInterface<T>` 是 sender 端代理）。修订对齐 S2-08/S2-09 既有 InteractableObject 模式。详见 `/.claude/memory/problem_2026-04-29_story-impl-notes-vs-framework-drift.md`。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: InteractableObjectFsm 的 Locked state（state 已存在；本 story 提供锁数据来源 + 派发 OnInteractionLockChanged）
- Story 007: InteractionCoordinator 实例化 LockManager + 注入到每个 InteractableObject
- Narrative System: 定义何时派发 `IInteractionEvent.OnRequestPuzzleLockAll`（属于 Narrative epic 责任）
- Shadow Puzzle System: 定义 `OnRequestPuzzleLockAll` sender 时机（属于 Shadow Puzzle epic 责任）

---

## QA Test Cases

*EditMode 单元测试，POCO `InteractionLockManager`，0 GameObject 依赖：*

- **AC-1**: IsLocked 反映 HashSet
  - Given: `_activeLocks` 为空
  - When: 查询 `IsLocked`
  - Then: `false`
  - When: `PushLock("shadow_puzzle")`
  - Then: `IsLocked == true`；GameEvent listener 收到 `OnInteractionLockChanged(true)` 一次
  - When: `PopLock("shadow_puzzle")`
  - Then: `IsLocked == false`；listener 收到 `OnInteractionLockChanged(false)` 一次
  - Edge: 同 token 重复 push（HashSet 去重）→ Count==1 → 不重复派发 OnInteractionLockChanged

- **AC-2**: 多 sender token 测试（SP-006 关键场景）
  - Given: 两个 sender 并发锁
  - When: `PushLock("shadow_puzzle")` → `PushLock("narrative")`
  - Then: `IsLocked == true`；`Count == 2`；OnInteractionLockChanged(true) 仅派发 1 次（在第一次 PushLock 时）
  - When: `PopLock("narrative")`
  - Then: `IsLocked == true`（仍持 shadow_puzzle）；OnInteractionLockChanged 不再派发
  - When: `PopLock("shadow_puzzle")`
  - Then: `IsLocked == false`；OnInteractionLockChanged(false) 派发 1 次
  - Edge: 反向顺序（先 pop shadow_puzzle）— 最终结果一致；中间 listener 派发次数也一致

- **AC-3**: 未知 token pop 是 no-op + warning
  - Given: `_activeLocks = {"shadow_puzzle"}`
  - When: `PopLock("unknown_system")`
  - Then: `_activeLocks` 不变；`Log.Warning` 调用一次（用 `LogAssert.Expect` 验证）；不抛异常；OnInteractionLockChanged **不**派发
  - Edge: 空 set + 未知 token pop — 同样 no-op + warning

- **AC-4**: ISceneEvent.OnSceneUnloadBegin 强清
  - Given: `_activeLocks = {"shadow_puzzle", "narrative"}`（模拟泄漏）
  - When: 通过 `GameEvent.Get<ISceneEvent>().OnSceneUnloadBegin(1)` 派发（或直接调用接口方法）
  - Then: `_activeLocks.Count == 0`；`Log.Warning` 含 "2 leaked locks"；OnInteractionLockChanged(false) 派发 1 次
  - Edge: 无锁时强清 — 无 warning，无派发（仅在实际泄漏时才警告）

- **AC-5**: InteractableObjectFsm 响应 OnInteractionLockChanged
  - Given: InteractableObjectFsm A 在 Selected 状态；订阅了 IInteractionEvent listener
  - When: LockManager.PushLock("shadow_puzzle") 触发派发 OnInteractionLockChanged(true)
  - Then: A.fsm.OnLockChanged(true) 被调用 → A.state == Locked（参见 Story 001 转换规则 7）
  - Edge: 锁定中再次 PushLock 其他 token（如 narrative）— OnInteractionLockChanged 不重复派发，A 保持 Locked

- **AC-6**: 通过事件接口（非直接 API）的端到端路径
  - Given: 配置完整的 LockManager + listener
  - When: `GameEvent.Get<IInteractionEvent>().OnRequestPuzzleLockAll("narrative")` 派发
  - Then: LockManager `_activeLocks` 含 "narrative"；OnInteractionLockChanged(true) 派发
  - When: `GameEvent.Get<IInteractionEvent>().OnRequestPuzzleUnlock("narrative")`
  - Then: `_activeLocks` 空；OnInteractionLockChanged(false) 派发

- **AC-7**: 协议合规 grep
  - When: `rg "EventId\.Evt_(PuzzleLockAll|PuzzleUnlock|SceneUnloadBegin)" Assets/GameScripts/HotFix/`
  - Then: 0 命中

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/Tests/EditMode/ObjectInteraction/InteractionLockManagerTests.cs` — **EXISTS, 13 NUnit 全绿**
- `production/qa/grep-no-evt-objectinteraction-lock-2026-04-29.md` — 协议合规 grep 证据（0 `EventId.Evt_PuzzleLockAll/Evt_PuzzleUnlock/Evt_SceneUnloadBegin` 残留）

**Status**: [x] Complete — 13 NUnit tests pass

| 测试 | 覆盖 AC |
|---|---|
| `AC1_PushLock_FromEmpty_DispatchesLockChangedTrueOnce` | AC-1（空→非空 → 派 1 次）|
| `AC1_PopLock_ToEmpty_DispatchesLockChangedFalseOnce` | AC-1（非空→空 → 派 1 次）|
| `AC1_DuplicatePushLock_SameToken_NoDuplicateDispatch` | AC-1 Edge（HashSet 去重）|
| `AC2_MultiSender_TwoTokens_OnlyEdgeTransitionsDispatch` | AC-2（多 sender 边界 transition 才派发）|
| `AC2_MultiSender_ReverseOrderPop_SameResult` | AC-2 Edge（反向顺序 pop 一致）|
| `AC3_PopLock_UnknownToken_LogsWarningNoOp` | AC-3（未知 token + LogAssert.Expect Warning）|
| `AC3_PopLock_FromEmpty_UnknownToken_LogsWarningNoDispatch` | AC-3 Edge（空 set + 未知 token）|
| `AC4_OnSceneUnloadBegin_LeakedLocks_ClearsAndDispatchesFalse` | AC-4（leak warning + 强清 + 1 次派发）|
| `AC4_OnSceneUnloadBegin_NoLocks_NoWarningNoDispatch` | AC-4 Edge（无锁时不 warning 不派发）|
| `AC6_GameEventInterfacePath_RoundTrip` | AC-6（GameEvent 接口端到端）|
| `Init_Idempotent_DoubleCallSafe` | 防御（重复 Init 不重复注册）|
| `Dispose_RemovesListeners_FurtherEventsIgnored` | 防御（Dispose 后 GameEvent 路径不驱动）|
| `Dispose_Idempotent_DoubleCallSafe` | 防御（重复 Dispose 不抛）|
| `PushLock_NullOrEmpty_LogsWarningIgnored` | 防御（null/empty token 拦截，X1 patch v2 加固）|

**注**：AC-5 InteractableObjectFsm 响应 OnInteractionLockChanged 已在 S2-08 `InteractableObjectFsmTests` 验证（fsm.OnLockChanged 转换规则 7/8）；本 story 不重复测试。

---

## Dependencies

- Depends on: Story 001（InteractableObjectFsm 的 Locked state + OnLockChanged(bool) 触发方法 — **DONE** S2-08）
- Pre-condition: ADR-013 = **Accepted**（✅ 2026-04-29）；`IInteractionEvent.cs` 已存在（✅ 2026-04-29）；`ISceneEvent.cs` 已 active（✅ S2-05；OnSceneUnloadBegin 接口签名冻结，sender 留 S2-17 — 测试用 GameEvent.Get<ISceneEvent>().OnSceneUnloadBegin(...) 模拟）
- Unlocks: Story 007（multi-object scene — Coordinator 实例化 LockManager 并注入到所有 object）

---

## Completion Notes

**Completed**: 2026-04-29 night（Sprint 2 should-have 1/3 闭环；解锁 S2-13 InteractionCoordinator）

### Acceptance Criteria 状态总览

| 类别 | 数量 | 备注 |
|---|---|---|
| 全部通过 | 14 / 14 | 含 X1 patch v2 修订后的 AC 全集（含原 11 + per-event 模式 + null/empty 防御 + Init/Dispose 幂等 + ActiveLockCount 诊断属性 + 不实施接口本身的修订）|

### 实施代码

- `Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/InteractionLockerId.cs` — new file
- `Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/InteractionLockManager.cs` — new file（POCO，per-event listener，HashSet token，`Log.Warning` 边界处理）
- `Assets/Tests/EditMode/ObjectInteraction/InteractionLockManagerTests.cs` — new file（13 NUnit tests）

### 资料 / 工程文档同步

- `production/qa/grep-no-evt-objectinteraction-lock-2026-04-29.md` — new file
- `production/sprint-status.yaml` — S2-12 → done
- `production/session-state/active.md` — Session Extract 追加
- `.claude/memory/problem_2026-04-29_story-impl-notes-vs-framework-drift.md` — new file（沉淀 story Implementation Notes 与 framework 实际能力 drift 的反复模式）
- `src/MyGame/ShadowGame/.claude/skills/tengine-dev/references/conventions.md` — 已加 "## Listener 端订阅模式" 章节（per-event 唯一支持模式 + 反例）

### 已解决的 Deviations

- §Implementation Notes 原模板用 `class InteractionLockManager : IInteractionEvent, ISceneEvent` + `AddEventListener<TInterface>(this)` 风格 → 与 framework 实际能力不符（TEngine 仅支持 per-event 订阅）→ 修订（X1 patch v2，已同步刷新）
- 防御性加固：null/empty lockerId 处理 + Init/Dispose 幂等性 + ActiveLockCount 诊断 — 这些原 §Implementation Notes 未明示，本次实施按 ADR-027 §3 防御性 listener 注册原则补齐

### 留作后续 Story / Polish

- **AC-5** InteractableObjectFsm 响应 OnInteractionLockChanged 已在 S2-08 `InteractableObjectFsmTests` 闭环（转换规则 7/8）；本 story 不重复
- **真实 sender** OnRequestPuzzleLockAll / OnRequestPuzzleUnlock 实装由 Narrative epic / Shadow Puzzle epic 后续 story 接管
- **真实 sender** OnSceneUnloadBegin 实装由 S2-17 Story 005 接管（接口签名冻结期）

### Patches 历史

- 2026-04-29 night X1 patch v1：实施 InteractionLockerId + InteractionLockManager + 13 NUnit
- 2026-04-29 night X2 patch v2：§Engine Notes + §Implementation Notes 修订（per-event 模式取代整接口订阅，对齐 TEngine 实际能力 + 加 conventions.md "Listener 端订阅模式" 防复发）
