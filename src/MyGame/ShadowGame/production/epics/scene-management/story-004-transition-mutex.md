// 该文件由Cursor 自动生成

# Story 004: Transition Mutex with Max-1 Queue

> **Epic**: Scene Management
> **Status**: Complete
> **Completed**: 2026-04-29
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-22
>
> **Revision note (2026-04-23)**: 原文使用 ADR-006 `Evt_RequestSceneChange` + `RequestSceneChangePayload` / `Evt_SceneReady` + `SceneReadyPayload` 协议；已迁移到 ADR-027 `ISceneEvent.OnRequestSceneChange(int targetChapterId)` / `OnSceneReady(int chapterId)` 接口方法（Story 001 已冻结并实装两方法）。**本 story 范围已收窄**：Story 001 的 `_pendingTargetChapterId` 已覆盖基础 max-1 队列语义；此处仅扩展 `_inflightChapterId` 去重 + 边界用例更多测试覆盖。

## Context

**GDD**: `design/gdd/scene-management.md`
**Requirement**: `TR-scene-006`
*(Transition mutual exclusion; only one transition at a time; max 1 queued request)*

**ADR Governing Implementation**: ADR-009: Scene Lifecycle & Additive Scene Strategy + ADR-027: GameEvent Interface Protocol
**ADR Decision Summary**: Requests received during an active transition are queued with a max depth of 1 (newest overwrites previous). The queue is drained when returning to Idle. Same-scene requests are no-ops. This prevents race conditions when systems fire rapid scene change events.

**Engine**: Unity 2022.3.62f2 LTS | **Risk**: LOW
**Engine Notes**: State machine is single-threaded (Unity main thread). All GameEvent handlers run on the main thread. No multi-threading concerns for the mutex itself. However, UniTask `await` points within the 11-step flow create interleaving opportunities — the mutex guard must check state at each re-entry point.
**Performance**: N/A — cold path（chapter transition guard only；每章 1 次，~5 次/游戏）；in-flight 去重 + 队列写入均为 O(1) 非帧级操作；`OnRequestSceneChange` / `OnSceneReady` 派发 ≤ 0.01ms（ADR-027 §2 guardrail 兜底）。

**Control Manifest Rules (this layer)**:
- Required: `Transition mutex: only one transition at a time; max 1 queued request` (ADR-009)
- Required: `Request for the same scene as the current scene is ignored with a SceneReady confirmation` (ADR-009)
- Required: `Register all listeners in Init(); remove all in Dispose()` (ADR-027 §3)
- Forbidden: `Never assume listener invocation order` (ADR-027 §2)
- Forbidden: `Never use Evt_* int constants or RequestSceneChangePayload struct — use ISceneEvent.OnRequestSceneChange directly` (ADR-027 §1 取代 ADR-006)

---

## Acceptance Criteria

*From GDD `design/gdd/scene-management.md`, scoped to this story:*

- [x] Only one chapter transition may execute concurrently — second request during transition is queued
- [x] Queue depth is exactly 1: if a pending request already exists, the newest request replaces it (not appended)
- [x] When the current transition completes (returns to `Idle`), pending request is automatically dequeued and begins
- [x] A request to the same chapter that is currently active is silently discarded — `ISceneEvent.OnSceneReady(chapterId)` dispatched as confirmation; no full transition（Story 001 AC-8 已覆盖；本 story 补更多场景测试）
- [x] A request to the same chapter that is currently being loaded (i.e., `targetChapterId == _inflightChapterId`) is rejected without queuing (would be a redundant load) — **本 story 新增**
- [x] Rapid fire of 10 `ISceneEvent.OnRequestSceneChange` calls during a transition results in exactly 1 queued request (the 10th) and no state corruption
- [x] `_pendingTargetChapterId` field is `null` when no request is queued; non-null when one is pending

---

## Implementation Notes

*Derived from ADR-009 Implementation Guidelines:*

Story 001 已实装的 `OnRequestSceneChange(int targetChapterId)` handler 基础版覆盖 Idle/非 Idle/同章/Error 四分支。本 story 扩展为：

```csharp
private void OnRequestSceneChange(int targetChapterId)
{
    if (_state == SceneManagerState.Error)
    {
        Log.Warning($"[SceneManager] Request({targetChapterId}) dropped — Error state.");
        return;
    }

    // ★ 本 story 新增：in-flight 去重
    if (targetChapterId == _inflightChapterId && IsTransitioning)
    {
        Log.Info($"[SceneManager] Request({targetChapterId}) ignored — already in-flight.");
        return;
    }

    if (_state == SceneManagerState.Idle)
    {
        if (targetChapterId == _currentChapterId && _currentChapterId != -1)
        {
            GameEvent.Get<ISceneEvent>().OnSceneReady(targetChapterId);
            return;
        }
        _currentChapterId = targetChapterId;
        _inflightChapterId = targetChapterId; // ★ 新增
        TransitionTo(SceneManagerState.TransitionOut);
        return;
    }

    _pendingTargetChapterId = targetChapterId; // newest wins
}
```

`_inflightChapterId` tracks the chapter currently being loaded (set in `OnRequestSceneChange` / `DrainPending`, cleared on `RecoverToIdle` / Loading 完成 —— Loading 完成清理由 Story 002 落地时做）。本 story 仅添加字段 + 去重 guard + 测试；不动 transition 内部流程。

**Debug logging**: In debug builds, log every received request, queue decision, and dequeue start with `[SceneManager]` prefix.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: FSM state enum and state machine skeleton
- Story 002: Actual async scene loading (`BeginTransition` inner logic)
- Story 005: Lifecycle events 除 `OnSceneReady` 外的 sender 实装

---

## QA Test Cases

- **AC-1**: Only one concurrent transition
  - Given: SceneManager is in Loading state
  - When: `GameEvent.Get<ISceneEvent>().OnRequestSceneChange(3)` is dispatched
  - Then: Chapter 3 is stored as `_pendingTargetChapterId`; no new transition starts; state remains Loading
  - Edge cases: ensure `_state` check runs inside handler body (no race since main-thread)

- **AC-2**: Queue overwrites on overflow
  - Given: SceneManager is transitioning; `_pendingTargetChapterId == 2`
  - When: `OnRequestSceneChange(5)` is dispatched
  - Then: `_pendingTargetChapterId == 5`; chapter 2 is discarded
  - Edge cases: 10 rapid calls → only 10th survives

- **AC-3**: Pending request drains automatically on Idle
  - Given: SceneManager completes a transition; `_pendingTargetChapterId == 4`
  - When: State returns to Idle (via `AdvanceStateForTest(Idle)` or real Story 002 flow)
  - Then: Transition to chapter 4 begins within the same frame; `_pendingTargetChapterId == null`
  - Edge cases: if `_pendingTargetChapterId` 指向 current chapter, apply same-chapter no-op (`OnSceneReady`) logic

- **AC-4**: Same-chapter no-op dispatches `OnSceneReady`
  - Given: SceneManager is Idle; `_currentChapterId == 2`
  - When: `OnRequestSceneChange(2)` is dispatched
  - Then: Listener 收到 `OnSceneReady(2)` 一次；no state change；no transition begins
  - Edge cases: `_currentChapterId == -1` (boot) must not match any valid chapter ID

- **AC-5**: In-flight duplicate request is rejected
  - Given: SceneManager is in Loading state targeting chapter 3; `_inflightChapterId == 3`
  - When: `OnRequestSceneChange(3)` is dispatched
  - Then: Request is silently discarded; `_pendingTargetChapterId` remains unchanged；`Log.Info` 含 "in-flight"
  - Edge cases: different chapter during loading should still queue normally

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/Tests/EditMode/SceneManagement/TransitionMutexTests.cs` — must exist and pass

**Status**: [x] **Created and passing** — 18 tests covering AC-1..AC-5 + 边界（NoChapterId 哨兵 / boot guard / drain 同章防御性路径 / RecoverToIdle 清理）；EditMode Run All **234/234 全绿**（S2-05 baseline 216 + S2-06 18，零回归）

---

## Implementation Log (2026-04-29)

### 实施（1 改 1 新）

**`Assets/GameScripts/HotFix/GameLogic/Scene/SceneManager.cs`** —— 改：
- 新增 `public const int NoChapterId = -1;`（哨兵集中化）
- 新增 `private int _inflightChapterId = NoChapterId;` + `public int InflightChapterIdForTest => _inflightChapterId;`
- 替换 3 处魔法数字 `-1` → `NoChapterId`（`_currentChapterId` 初始 / AC-8 同章 guard / DrainPending 同章 guard）
- `OnRequestSceneChange` 在 Error guard 之后、Idle 分支之前插入 in-flight guard：
  ```csharp
  if (IsTransitioning && targetChapterId == _inflightChapterId)
  {
      Log.Info($"[SceneManager] OnRequestSceneChange({targetChapterId}) ignored — already in-flight.");
      return;
  }
  ```
- Idle 不同章过渡 + DrainPending 启动新过渡时各 `_inflightChapterId = next`
- `AdvanceStateForTest(Idle)` / `RecoverToIdle()` 在 `DrainPending()` 调用之前清 `_inflightChapterId = NoChapterId`（先清后 drain；DrainPending 启新过渡时立即重赋）

**`Assets/Tests/EditMode/SceneManagement/TransitionMutexTests.cs`** —— 新建（18 tests）：
- §0 NoChapterId 哨兵常量一致性（2）
- §1 AC-1 队列基础（Loading / TransitionOut 各 1）
- §2 AC-2 newest-wins + rapid-fire 10 (2)
- §3 AC-3 drain 启新过渡 + drain 同章防御性路径 (2)
- §4 AC-4 boot guard：current=NoChapterId 不被同章 guard 误判 (1)
- §5 AC-5 in-flight 去重 4 个场景（Loading / TransitionOut / 不同章不被拦 / rapid-fire 同章全丢）
- §6 in-flight set/clear 时机 5 个（set on Idle 新过渡 / drain 无 pending 清 / drain 有 pending 重置 / RecoverToIdle 清 / RecoverToIdle 后再请求同章可走 OnSceneReady）

### 设计决策（讨论后拍板）

| ID | 选项 | 决策 | 理由 |
|---|---|---|---|
| D1 | inflight 字段类型 | **B**（`int + -1` sentinel） | 用户额外要求："允许多 sentinel 共享 + 必须命名常量化"。当前 `NoChapterId = -1` 一个常量覆盖"无 currentChapter / 无 inflight"两个语义；未来如需细分（Error 中断降级）再加 `NoChapterId_*` 同前缀变体 |
| D2 | inflight 清理时机 | **A**（`AdvanceStateForTest(Idle)` + `RecoverToIdle()` 都清） | `AdvanceStateForTest` 是测试 / Story 002 共用 seam，到达 Idle 就代表"完整过渡完成"，清 inflight 是该语义的一部分；Story 002 落地时同样在该路径清，不越权 |
| D3 | in-flight 同章请求回应 | **A**（静默丢弃 + Log.Info） | `OnSceneReady` 在 ADR-027 里语义是"chapter 已就绪可玩"，正在 Loading 时还没就绪发它会污染契约；caller 等真正的 lifecycle sender 即可 |

### 偏离 Implementation Notes 的点（改进，非降级）

无新增实质偏离；story 原 Implementation Notes 提及"Loading 完成清理由 Story 002 落地时做"，本次为可测性把"Loading 完成清理"逻辑放到了 `AdvanceStateForTest(Idle)`（与 `RecoverToIdle` 对称）。Story 002 落地时只需在内部"Loading 完成"路径加同义清理，与本 story 行为一致。

### 验证

- ReadLints ✅ 零错误（`SceneManager.cs` + `TransitionMutexTests.cs`）
- assets-refresh ✅ 编译干净，`isCompiling=false`，`isUpdating=false`
- Console 0 Error / 0 Exception
- EditMode 手动 Run All **234/234 全绿**（S2-04 baseline 187 + S2-05 +23 + S2-06 +18 = 228；加上期间累计 +6）；首轮 1 个失败（`AC3_DrainEdge_PendingEqualsCurrent_DispatchesSceneReady_NoNewTransition`）—— 根因：S2-06 in-flight guard 引入后 "FireRequest(7)→FireRequest(2)" 路径制造的 pending=2 与 current=7 不再相等，drain 走"不同章"分支；修复方式：照抄 S2-05 `AC11Edge_PendingSameAsCurrent_DrainsToOnSceneReady_StaysIdle` 的 `AdvanceStateForTest(Loading)` 绕过法，并在测试注释里说明"DrainPending 同章 guard 是防御性代码路径"。

### 遗留

- `DrainPending` 内 "next == current" 同章 guard 在 S2-06 后变为**生产路径不可达的防御性代码**（任何 ISceneEvent 自然路径都会被 in-flight guard 提前拦掉）。保留它无害，覆盖 Story 002 内部驱动状态机的非典型路径，下一次架构审视时可考虑标 `[Obsolete]` 或合并语义

---

## Dependencies

- Depends on: Story 001 (state machine skeleton — must be DONE)
- Unlocks: Story 005 (full integration — mutex must work before event ordering is meaningful)
