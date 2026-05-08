// 该文件由Cursor 自动生成

# Story 004: GameEvent Dispatch for Chapter/Puzzle State Changes

> **Epic**: Chapter State
> **Status**: Complete
> **Completed**: 2026-04-23
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-04-22
>
> **Revision note (2026-04-23)**: 全文由 ADR-006（Superseded）协议重写为 ADR-027 接口事件协议。`public const int` + `XxxPayload` struct 的旧写法已废除；所有事件改为 `[EventInterface]` 接口方法签名直接传参。Event ID 由 TEngine Source Generator 编译期 hash 生成，开发者不再手写 ID。`PuzzleLockAll / PuzzleUnlock` 从本 story scope 移出，改由 Object Interaction epic 负责（它们是 Interaction 域语义，不属于 Chapter State 边界）。
>
> **Completion note**: EditMode 手动跑通 — `StateEventsTests` 12/12（AC-1..AC-8 + Integration 全覆盖）+ 回归 Run All **175/175 全绿**（用户确认 2026-04-23）。Source Generator 正确产出 `IChapterStateEvent_Gen` + `IChapterStateEvent_Event`；`ChapterStateEventBridge.Attach/Detach` 完成 4-hook 接线；S2-02 `Init/SetPuzzleState/OnPuzzleComplete` 全部派发点接通，同值赋值守卫生效。

## Context

**GDD**: `design/gdd/chapter-state-and-save.md`
**Requirement**: `TR-save-004`（Chapter State 是章节状态的唯一权威），`TR-save-005`（`IChapterProgress` 接口解耦 Save System）
*(State change events broadcast from ChapterStateManager to all consumers)*

**ADR Governing Implementation**: ADR-027: GameEvent Interface Protocol + ADR-001: TEngine 6.0 Framework
**ADR Decision Summary**: 所有跨模块状态变更通过 TEngine `[EventInterface]` 接口广播。消费者（Narrative、Audio、Save、UI）被动订阅对应接口方法，**不得**直接调用 `ChapterStateManager` 的内部 API。参数通过方法签名直接传值（`int`、`PuzzleStateEnum` 等），不使用 Payload struct。ID 由 Roslyn Source Generator 在编译期按方法签名哈希生成（`RuntimeId.ToRuntimeId(...)`），开发者零维护。

**Engine**: Unity 2022.3.62f2 LTS + TEngine 6.0.0 | **Risk**: LOW
**Engine Notes**: 接口事件派发性能已在 `IGestureEvent` / `IShadowRTEvent` 实测：`GameEvent.Get<T>().OnXxx(...)` 单次 ≤ 0.01ms（远低于 0.05ms 预算），零 GC。Cascade 深度 `OnPuzzleStateChanged → OnChapterComplete → OnRequestSceneChange` 必须 ≤ 3（ADR-027 §5 继承 ADR-006 §5）。引用 `GameLogic` 的测试 asmdef 必须同时引用 `TEngine.Runtime`，否则 `GameEventHelper.g.cs` 编译失败（见 TENGINE-SG-002 教训）。

**Control Manifest Rules (this layer)**:
- Required: `所有跨模块事件通过 [EventInterface] 接口定义`（ADR-027 §1）
- Required: `接口必须声明 [EventInterface(EEventGroup.GroupLogic)] 或 GroupUI`（ADR-027 §1）
- Required: `接口方法的 XML 注释必须含 <para>Sender</para><para>Listener</para><para>Cascade depth</para>`（ADR-027 §6 继承 ADR-006 §6）
- Required: `发送方用 GameEvent.Get<IXxx>().OnXxx(...) 派发`（ADR-027 §1）
- Required: `Listener 在 Init/OnEnter 订阅，在 Dispose/OnLeave 取消订阅`（ADR-027 §3 继承 ADR-006 §3）
- Required: `Cascade 深度 ≤ 3`（ADR-027 §5 继承 ADR-006 §5）
- Required: `GameApp.Entrance 第一行调用 GameEventHelper.Init()`（ADR-027 §8）
- Forbidden: `禁止新增 public const int Evt_Xxx`（ADR-027 §1 明文取代 ADR-006 §1）
- Forbidden: `禁止使用 C# event / delegate / ScriptableObject 做跨模块通信`（ADR-001 继承）
- Forbidden: `禁止在 handler 里对同一接口方法再次 GameEvent.Get<>().OnXxx(...) 触发 re-entrancy`（ADR-027 §5 继承 ADR-006 §5）

---

## Acceptance Criteria

*From GDD `design/gdd/chapter-state-and-save.md`, re-aligned to ADR-027 by 2026-04-23 revision:*

- [ ] **AC-1 接口定义**：新增 `Assets/GameScripts/HotFix/GameLogic/IEvent/IChapterStateEvent.cs`，声明 `[EventInterface(EEventGroup.GroupLogic)]`，含 4 个方法：`OnPuzzleStateChanged(int chapterId, int puzzleId, PuzzleStateEnum newState)` / `OnChapterComplete(int chapterId)` / `OnChapterUnlocked(int chapterId)` / `OnGameComplete()`。每个方法有 `<summary>`、`<para>Sender</para>`、`<para>Listener</para>`、`<para>Cascade depth</para>` XML 文档块。
- [ ] **AC-2 Source Generator 产物**：`Library/Bee/artifacts/.../GameLogic.dll` 中包含 `GameLogic.IChapterStateEvent_Gen` 类型；`GameEventHelper.Init()` IL 新增对 `new IChapterStateEvent_Gen(dispatcher)` 的调用。
- [ ] **AC-3 Puzzle 状态变更派发**：`ChapterStateManager` 在每次 `PuzzleProgress.State` 赋值成功后派发 `OnPuzzleStateChanged(chapterId, puzzleId, newState)`。派发点覆盖：(1) `Init` 里首 puzzle 自动 `Locked→Idle` 提升；(2) `SetPuzzleState` guarded setter 成功路径；(3) `OnPuzzleComplete` 的 `Complete` 赋值 + 下一个 puzzle `Locked→Idle` 提升。**同值赋值**（`Idle→Idle` 等未发生改变）**不派发**。
- [ ] **AC-4 章节完成派发幂等**：`OnChapterComplete(chapterId)` 每章节严格触发一次（由 S2-02 已实现的 `chapter.IsCompleted` 守卫承担幂等责任）。replay 重入 `CheckChapterCompletion` 不重复派发。
- [ ] **AC-5 章节解锁派发**：新章节 `IsUnlocked` 从 `false` 翻转为 `true` 时派发 `OnChapterUnlocked(chapterId)` 一次。由 `TryUnlockChapter` 幂等守卫承担（`if (chapter.IsUnlocked) return false;`）。
- [ ] **AC-6 游戏完成派发**：Chapter 5 完成时派发 `OnGameComplete()` 一次；不再尝试派发 `OnChapterUnlocked`（无 Chapter 6）。
- [ ] **AC-7 Cascade 深度 ≤ 3**：链路 `OnPuzzleStateChanged(Complete) → OnChapterComplete → [外部监听者派发 OnRequestSceneChange]` 栈深度 ≤ 3；任何 handler 若需更深级联必须用 `await UniTask.Yield()` 打断同步栈（ADR-027 §5）。
- [ ] **AC-8 Wire-up 连接**：新增桥接类 `ChapterStateEventBridge`（或等效注册点），在 `GameApp.Entrance` 初始化期间将 `ChapterStateManager` 的 4 个 `Action` callback（`OnPuzzleStateChangedCallback` / `OnChapterCompletedCallback` / `OnChapterUnlockedCallback` / `OnGameCompletedCallback`）分别接到 `GameEvent.Get<IChapterStateEvent>().OnXxx(...)`。ChapterStateManager 本身**零 TEngine event 依赖**（由 S2-02 已建立的约定延续）。
- [ ] **AC-9 测试 asmdef 合规**：所有新增测试使用 `GameEvent.EventMgr.Init() + new IChapterStateEvent_Gen(GameEvent.EventMgr.GetDispatcher())` 模式注册（绕过 TENGINE-SG-002 残留的 `GameEventHelper` 空版解析歧义）。

---

## Implementation Notes

*Aligned to ADR-027 §1 接口定义协议、§3 生命周期、§5 顺序与 re-entrancy、§6 文档约定、§8 asmdef 约束:*

### 1. 新接口文件

```csharp
// Assets/GameScripts/HotFix/GameLogic/IEvent/IChapterStateEvent.cs
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// Chapter State 广播接口（ChapterStateManager → UI / Save / Narrative / Audio）。
    /// <para>Sender: ChapterStateManager（唯一发送方；通过 ChapterStateEventBridge 转发）</para>
    /// <para>Listener: ChapterSelectUI, SaveAutoTrigger, NarrativeDirector, AudioController</para>
    /// <para>协议来源：ADR-027 §1 取代 ADR-006 §1/§2；生命周期 / 顺序保证 / 文档约定继承 ADR-006 §3/§5/§6</para>
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IChapterStateEvent
    {
        /// <summary>
        /// 某个 puzzle 的 <see cref="PuzzleStateEnum"/> 发生了实际改变（同值赋值不派发）。
        /// <para>Sender: ChapterStateManager（Init 首 Idle 提升 / SetPuzzleState 成功路径 / OnPuzzleComplete 的 Complete 赋值与 next 提升）</para>
        /// <para>Listener: ShadowPuzzle FSM, Narrative triggers, SaveManager auto-save, HUD puzzle-state indicator</para>
        /// <para>Cascade depth: 1（监听者可触发 OnChapterComplete → OnRequestSceneChange，合计深度 3，不得再深）</para>
        /// </summary>
        void OnPuzzleStateChanged(int chapterId, int puzzleId, PuzzleStateEnum newState);

        /// <summary>
        /// 某章节全部 puzzle 达到 <see cref="PuzzleStateEnum.Complete"/>，<see cref="ChapterProgress.IsCompleted"/> 翻转为 true。
        /// <para>Sender: ChapterStateManager（CheckChapterCompletion 主路径；每章节严格一次）</para>
        /// <para>Listener: NarrativeDirector（章节总结）, AudioController（片尾 BGM）, SaveManager（auto-save）</para>
        /// <para>Cascade depth: 2（监听者可触发 OnRequestSceneChange，合计 3）</para>
        /// </summary>
        void OnChapterComplete(int chapterId);

        /// <summary>
        /// 新章节 <see cref="ChapterProgress.IsUnlocked"/> 从 false 翻转为 true。
        /// <para>Sender: ChapterStateManager（TryUnlockChapter 成功路径）</para>
        /// <para>Listener: ChapterSelectUI（主菜单解锁提示）, SaveManager</para>
        /// <para>Cascade depth: 1（监听者不得进一步触发事件）</para>
        /// </summary>
        void OnChapterUnlocked(int chapterId);

        /// <summary>
        /// Chapter 5 完成 — 全 5 章通关。本事件触发后不再有 OnChapterUnlocked。
        /// <para>Sender: ChapterStateManager（CheckChapterCompletion 的 Ch.5 分支）</para>
        /// <para>Listener: NarrativeDirector（片尾字幕）, CreditsUI, SaveManager（锁定存档 + 解锁 replay 模式）</para>
        /// <para>Cascade depth: 1（监听者不再派发事件；可 await UniTask 跳离当前栈）</para>
        /// </summary>
        void OnGameComplete();
    }
}
```

### 2. ChapterStateManager 新增 callback + 派发点

```csharp
// 新成员（第 4 个回调）
/// <summary>Story-004 wire-up hook: fired on every real PuzzleState change.
/// See ADR-027 §1 — dispatched as IChapterStateEvent.OnPuzzleStateChanged by
/// ChapterStateEventBridge.</summary>
public Action<int, int, PuzzleStateEnum> OnPuzzleStateChangedCallback;
```

派发点（在既有代码基础上追加 `Invoke`）：

| 位置 | 旧行 | 追加 |
|---|---|---|
| `Init` 首 puzzle 提升 | `first.State = PuzzleStateEnum.Idle;` | `OnPuzzleStateChangedCallback?.Invoke(chapterId, first.PuzzleId, PuzzleStateEnum.Idle);` |
| `SetPuzzleState` 成功返回前 | `puzzle.State = newState;` | `OnPuzzleStateChangedCallback?.Invoke(chapterId, puzzleId, newState);` |
| `OnPuzzleComplete` Complete 赋值 | `puzzle.State = PuzzleStateEnum.Complete;` | `OnPuzzleStateChangedCallback?.Invoke(chapterId, puzzleId, PuzzleStateEnum.Complete);` |
| `OnPuzzleComplete` 下一个 Idle 提升 | `next.State = PuzzleStateEnum.Idle;` | `OnPuzzleStateChangedCallback?.Invoke(chapterId, next.PuzzleId, PuzzleStateEnum.Idle);` |

`SetPuzzleState` 的**同值赋值**（newState == current）是否走派发？按 AC-3"同值不派发"，在 `SetPuzzleState` 入口加守卫 `if (current == newState) return true;`（返回 true 表示"请求合法"，但不触发事件）。

### 3. Wire-up 桥接类

```csharp
// Assets/GameScripts/HotFix/GameLogic/ChapterState/ChapterStateEventBridge.cs
namespace GameLogic
{
    /// <summary>
    /// 把 ChapterStateManager 的 4 个 C# Action callback 接到
    /// IChapterStateEvent 接口事件派发。遵循 ADR-027：ChapterStateManager 自身
    /// 对 TEngine event 零依赖，只暴露 Action hook；桥接层负责把 hook 转成
    /// GameEvent.Get&lt;IChapterStateEvent&gt;().OnXxx(...) 调用。
    /// </summary>
    public static class ChapterStateEventBridge
    {
        public static void Attach(ChapterStateManager mgr)
        {
            if (mgr == null) return;
            mgr.OnPuzzleStateChangedCallback = (c, p, s) =>
                GameEvent.Get<IChapterStateEvent>().OnPuzzleStateChanged(c, p, s);
            mgr.OnChapterCompletedCallback = id =>
                GameEvent.Get<IChapterStateEvent>().OnChapterComplete(id);
            mgr.OnChapterUnlockedCallback = id =>
                GameEvent.Get<IChapterStateEvent>().OnChapterUnlocked(id);
            mgr.OnGameCompletedCallback = () =>
                GameEvent.Get<IChapterStateEvent>().OnGameComplete();
        }

        public static void Detach(ChapterStateManager mgr)
        {
            if (mgr == null) return;
            mgr.OnPuzzleStateChangedCallback = null;
            mgr.OnChapterCompletedCallback = null;
            mgr.OnChapterUnlockedCallback = null;
            mgr.OnGameCompletedCallback = null;
        }
    }
}
```

`Attach` 的调用点：未来 `GameApp.Entrance` 或 ChapterState bootstrap 里，**在** `ChapterStateManager.Init(...)` **之后**、业务开始前调一次。S2-03 不引入 bootstrap 主流程改动（由 S2-04 save integration 或更晚的 story 负责挂点）；bridge 类本身是独立可单测的。

### 4. Cascade 深度保证（AC-7）

`OnPuzzleStateChanged` 的 listener 若触发 `OnChapterComplete`（通过 `CheckChapterCompletion`，但**注意**：在 ADR-027 架构下，`OnChapterComplete` 不由 listener 触发，而是由 `ChapterStateManager.OnPuzzleComplete` 末尾的 `CheckChapterCompletion` 同栈触发 —— 也就是在**同一个**原始调用栈里 sequential 派发）。级联为：

```
ChapterStateManager.OnPuzzleComplete(c, p)  // user action → depth 0
  ├── puzzle.State = Complete
  ├── dispatcher.OnPuzzleStateChanged(c, p, Complete)  // depth 1
  │       └── [无 handler 再派发新事件]
  ├── next.State = Idle
  ├── dispatcher.OnPuzzleStateChanged(c, p+1, Idle)    // depth 1
  │       └── [无 handler 再派发新事件]
  └── CheckChapterCompletion(c)
        └── chapter.IsCompleted = true
        └── dispatcher.OnChapterComplete(c)             // depth 1
              └── ExternalListener.OnRequestSceneChange // depth 2
                    └── (handler) 必须 UniTask.Yield()  // depth 3 最大
```

本 story scope 内不引入 `OnRequestSceneChange`（Scene Management epic 负责）。测试只需验证深度 ≤ 2。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 001 / 002 / 003**：决定**何时**派发的业务逻辑（数据层 + 进度推进）。S2-03 只做**如何**派发。
- **Story 005（S2-04）Save System Integration**：Save System 作为 `IChapterStateEvent` 的 listener 订阅 `OnChapterComplete` / `OnGameComplete` 触发 auto-save。S2-03 不写订阅端。
- **Shadow Puzzle epic**：puzzle FSM 自身的状态机细节（Idle → Active → NearMatch → PerfectMatch）不在 S2-03 范围；只关心"`PuzzleStateEnum` 变了就通过 `SetPuzzleState` 告诉 ChapterStateManager"的契约。
- **PuzzleLockAll / PuzzleUnlock**：原 Story 004 AC-7 列出的 `Evt_PuzzleLockAll / Evt_PuzzleUnlock` 是**对象交互锁**（InteractionLockManager）域的信号，不属于 Chapter State 权威边界。迁移到 Object Interaction epic 的独立 story（对应 `IInteractionLockEvent` 接口）。
- **ChapterStateManager 订阅外部事件**：原 AC-9 的 "订阅 Evt_PuzzleStateChanged 驱动进度" 已由 S2-02 `OnPuzzleComplete` 的内部方法调用路径取代（单权威 + 零 feedback loop 更简洁）；"订阅 Evt_SceneLoadComplete" 属于 Scene Management epic。

---

## QA Test Cases

所有测试文件位于 `Assets/Tests/EditMode/ChapterState/StateEventsTests.cs`。参考 `GestureDispatchTests` 的 SetUp 模式（TENGINE-SG-002 教训）：

```csharp
[SetUp]
public void SetUp()
{
    GameEvent.EventMgr.Init();
    _ = new IChapterStateEvent_Gen(GameEvent.EventMgr.GetDispatcher());
    _mgr = new ChapterStateManager();
    ChapterStateEventBridge.Attach(_mgr);
    // ... 注册测试 listener 捕获 dispatcher 发出的事件
}
```

- **AC-1/AC-2 接口与 SG 产物**：
  - Given: project 编译完成
  - When: reflect `typeof(IChapterStateEvent).GetCustomAttribute<EventInterfaceAttribute>()`
  - Then: 返回非 null，且 `Group == EEventGroup.GroupLogic`
  - Given: 编译后的 GameLogic.dll
  - When: 通过 `Assembly.Load` + `Type.GetType("GameLogic.IChapterStateEvent_Gen, GameLogic")`
  - Then: 类型存在，构造函数签名含 `IEventDispatcher` 参数
- **AC-3 OnPuzzleStateChanged 每次变更触发**：
  - Given: Init 初始（Ch.1 首 puzzle 提升到 Idle）
  - Then: listener 收到 `(chapterId=1, puzzleId=1, newState=Idle)` 一次
  - When: `SetPuzzleState(1, 1, Active)`
  - Then: listener 再收到一次
  - When: `SetPuzzleState(1, 1, Active)` 重复调用（同值）
  - Then: listener 不再收到
  - When: `OnPuzzleComplete(1, 1)` → puzzle 1 Complete + puzzle 2 Idle 提升
  - Then: listener 收到 2 次（`(1,1,Complete)` + `(1,2,Idle)`），顺序严格
- **AC-4 OnChapterComplete 幂等**：
  - Given: Ch.1 3 puzzles 全部完成，listener 收到 `OnChapterComplete(1)` 一次
  - When: 再次 `CheckChapterCompletion(1)`（replay）
  - Then: listener 不再收到 `OnChapterComplete`（S2-02 的 `IsCompleted` 守卫承担）
- **AC-5 OnChapterUnlocked**：
  - Given: Ch.1 刚完成
  - Then: listener 收到 `OnChapterUnlocked(2)` 一次
  - When: 再 `TryUnlockChapter(2)`（幂等调用）
  - Then: listener 不再收到
- **AC-6 OnGameComplete**：
  - Given: Ch.1-4 已 complete（save data 注入），Ch.5 3 puzzles 全部完成
  - Then: listener 收到 `OnChapterComplete(5)` 一次 + `OnGameComplete()` 一次；**不**收到 `OnChapterUnlocked(6)`
- **AC-7 Cascade 深度**（轻量验证）：
  - Given: listener 订阅 `OnPuzzleStateChanged`，handler 内记录 `System.Diagnostics.StackTrace` 深度
  - When: `OnPuzzleComplete(1, 3)`（触发 `PuzzleStateChanged + ChapterComplete` 同栈）
  - Then: `OnChapterComplete` handler 栈中能追溯到 `OnPuzzleComplete` 调用，级联深度 ≤ 2（本 story scope 内没有第 3 层）
  - Edge cases: 深度 ≥ 4 的断言需等 Scene Management listener 接入后补；S2-03 只建立 baseline
- **AC-8 Bridge 正确挂接**：
  - Given: new `ChapterStateManager`；new listener via `IChapterStateEvent_Gen`
  - When: `ChapterStateEventBridge.Attach(mgr)` 后驱动 `mgr.OnPuzzleComplete(1, 1)`
  - Then: listener 被调用（证明 4 个 callback 都挂上了）
  - When: `Detach` 后再触发
  - Then: listener 不再被调用（证明 null 赋值生效）

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/Tests/EditMode/ChapterState/StateEventsTests.cs` — must exist and pass

**Status**: [ ] Not yet created

---

## Dependencies

- **Depends on**:
  - Story 002（S2-01）puzzle 顺序与 `OnPuzzleComplete` 的内部调用链
  - Story 003（S2-02）章节完成 / 解锁 / 游戏完成的 3 个 C# Action callback
  - ADR-027 Accepted（2026-04-23 ✅）
  - `IGestureEvent_Gen` + `GestureDispatcher` 实现模板（即本项目现行 `[EventInterface]` + `GameEvent.Get<T>()` 派发样例）
- **Unlocks**:
  - Story 005（S2-04）Save System 作为 `IChapterStateEvent` listener 订阅 auto-save 触发
  - Shadow Puzzle epic / Narrative epic / Audio epic 所有下游 listener
