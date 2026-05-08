// 该文件由Cursor 自动生成

# Story 001: Scene Manager State Machine + `ISceneEvent` 契约（S2-05）

> **Epic**: Scene Management
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-22
> **Completed**: 2026-04-24 (23 NUnit tests green, 216/216 total EditMode suite passing)
>
> **Revision note (2026-04-23)**: 由 ADR-006 `Evt_RequestSceneChange` / `Evt_SceneReady` + `RequestSceneChangePayload` 协议重写为 ADR-027 接口事件协议。本 story 新建 **`GameLogic/IEvent/ISceneEvent.cs`**（9 方法契约：1 命令 + 8 生命周期）并**在 S2-05 实装 `OnRequestSceneChange` / `OnSceneReady` 两方法**（listener + sender 两侧均就位）；另外 6 个生命周期方法签名冻结、sender 侧留 S2-17 填。Scene epic 内其余 story 已同步到本契约（见 EPIC.md Revision note）。

## Context

**GDD**: `design/gdd/scene-management.md`
**Requirement**: `TR-scene-006`
*(Scene transition mutual exclusion; state machine governs all transition phases)*
*(同时落地 TR-scene-014 接口契约的 S2-05 切片 — 2/9 方法实装，其余冻结签名)*

**ADR Governing Implementation**: ADR-009: Scene Lifecycle + ADR-027: GameEvent Interface Protocol + ADR-001: TEngine 6.0 Framework
**ADR Decision Summary**: Scene Manager 通过正式状态机（Idle → TransitionOut → Unloading → Loading → TransitionIn → Idle + Error）管控所有过渡行为，一次只允许一个过渡进行、最多排 1 个待处理请求。外部系统仅通过 `GameEvent.Get<ISceneEvent>().OnRequestSceneChange(chapterId)` 发起切换（ADR-027 §1 —— 永远不直接调 Scene Manager 方法）。同章请求静默返回 `OnSceneReady(chapterId)` 作确认，不触发过渡。

**Engine**: Unity 2022.3.62f2 LTS + TEngine 6.0.0 | **Risk**: MEDIUM
**Engine Notes**: 状态机用一个独立 `SceneManagerState` 枚举 + 显式 switch 驱动（**不**使用 TEngine `FsmModule` —— TEngine FSM 在 2022.3 下泛型 owner 形态复杂，本 story 的 6 状态机结构简单，直接 if/switch 更透明、测试更好写，ADR-009 原文也只要求"formal state machine"，未强制 FsmModule）。`ISceneEvent` 添加 `[EventInterface(EEventGroup.GroupLogic)]`，Source Generator 自动生成 `ISceneEvent_Gen` + `ISceneEvent_Event` 静态方法 ID 容器。
**Performance**: 状态机操作是冷路径（每章切换 1 次，~5 次/游戏），非帧级热点；接口事件派发 ≤ 0.01ms/call（参考 `IGestureEvent` 实测）；N/A 无需针对性的 hotpath budget。

**Control Manifest Rules (this layer)**:
- Required: `Scene Manager uses state machine: Idle → TransitionOut → Unloading → Loading → TransitionIn → Idle (+ Error)` (ADR-009)
- Required: `Transition mutex: only one transition at a time; max 1 queued request` (ADR-009)
- Required: `Scene transitions triggered exclusively via ISceneEvent.OnRequestSceneChange(int) interface method` — 外部系统不得直接调 Scene Manager 方法 (ADR-027 §1)
- Required: `All module access via GameModule.XXX static accessors` (ADR-001)
- Required: `ISceneEvent 接口方法参数即 payload —— 禁止 XxxPayload struct` (ADR-027 §1 取代 ADR-006 §2 payload 模式)
- Required: `Listener 生命周期严格配对：Init() 中 AddEventListener，Dispose() 中 RemoveEventListener` (ADR-027 §3)
- Forbidden: `Never use ModuleSystem.GetModule<T>()` (ADR-001)
- Forbidden: `Never define Evt_SceneXXX int constants or XxxPayload structs — ADR-006 协议在 Scene 域整体作废` (ADR-027 §1 取代 ADR-006 §2)
- Forbidden: `Never call GameEvent.Get<ISceneEvent>().OnXxx(...) inside the same ISceneEvent method's own listener` — 防再入 (ADR-027 §2 继承 ADR-006 §3 re-entrancy 约束)

---

## Acceptance Criteria

*From GDD `design/gdd/scene-management.md`, scoped to this story:*

### A. `ISceneEvent` 接口契约（epic 基建）

- [x] **AC-1** `Assets/GameScripts/HotFix/GameLogic/IEvent/ISceneEvent.cs` 新建：
  - `namespace GameLogic`
  - 含 `[EventInterface(EEventGroup.GroupLogic)]` 属性
  - 定义 **9 个接口方法**，顺序与命名严格如下（方法参数即 payload）：
    1. `void OnRequestSceneChange(int targetChapterId)` — **S2-05 实装**
    2. `void OnSceneReady(int chapterId)` — **S2-05 实装**
    3. `void OnSceneTransitionBegin(int fromChapterId, int toChapterId)` — S2-17 sender
    4. `void OnSceneUnloadBegin(int chapterId)` — S2-17 sender
    5. `void OnSceneDownloadProgress(float progress, long downloadedBytes, long totalBytes)` — S2-17 sender
    6. `void OnSceneLoadProgress(string sceneName, float progress)` — S2-17 sender
    7. `void OnSceneLoadComplete(int chapterId, string bgmAsset)` — S2-17 sender
    8. `void OnSceneTransitionEnd(int chapterId)` — S2-17 sender
    9. `void OnSceneLoadFailed(int chapterId, string error)` — S2-17 sender
  - 每方法有完整 `<summary>` + `<para>Sender / Listener / Cascade</para>`；S2-17 预留方法标 `[Reserved — S2-17 Story 005]`
- [x] **AC-2** Source Generator 产物存在：反射可见 `GameLogic.ISceneEvent_Gen`（非 abstract, non-generic 类）与 `GameLogic.ISceneEvent_Event`（含 `public static readonly int OnRequestSceneChange / OnSceneReady / ...` 共 9 个 ID 字段，全部非零）

### B. Scene Manager 状态机骨架

- [x] **AC-3** `SceneManagerState` 枚举定义 6 个值：`Idle`, `TransitionOut`, `Unloading`, `Loading`, `TransitionIn`, `Error`；顺序如枚举声明
- [x] **AC-4** `ISceneManager` 接口定义 3 个只读属性：`CurrentState: SceneManagerState`、`CurrentChapterId: int`（初值 -1 = 未加载）、`IsTransitioning: bool`
- [x] **AC-5** `SceneManager` 实现 `ISceneManager`，`Init()` 后初始状态为 `Idle`；`IsTransitioning` 仅在 `Idle` / `Error` 时为 `false`，其余 4 状态为 `true`
- [x] **AC-6** `Init()` 重复调用不重置进行中的过渡：若已初始化且 `CurrentState != Idle`，直接 no-op + `Log.Warning`；若 `Idle / Error`，可重新 wire listener

### C. `OnRequestSceneChange` 处理语义

- [x] **AC-7** **Idle 状态 + 不同章**：`Idle → TransitionOut`，`CurrentChapterId` 更新为目标（同帧可读，transition 流程由 Story 002 驱动落地）
- [x] **AC-8** **Idle 状态 + 同章**（`targetChapterId == CurrentChapterId`，且 `CurrentChapterId != -1`）：状态保持 `Idle`，立即派发 `ISceneEvent.OnSceneReady(chapterId)` 作确认，不进入 TransitionOut
- [x] **AC-9** **非 Idle 状态**（TransitionOut / Unloading / Loading / TransitionIn 任一）：请求进入 `_pendingTargetChapterId` 队列，**最多容纳 1 个**，新请求覆盖旧；状态不变；不派发 OnSceneReady
- [x] **AC-10** **Error 状态**：请求静默丢弃 + `Log.Warning`（恢复到 Idle 再请求），不进入队列；不派发 OnSceneReady

### D. 排队与恢复

- [x] **AC-11** 当 `CurrentState` 从任意非 Idle 状态回到 `Idle`（由 Story 002/004 驱动），若 `_pendingTargetChapterId.HasValue`，立即 consume 并开始下一次过渡（进入 `TransitionOut` + clear pending）；同章合并语义同 AC-8
- [x] **AC-12** `Error → Idle` 恢复动作（`RecoverToIdle()`）：重置 `CurrentState = Idle`，保留 `_pendingTargetChapterId` 以便恢复后继续；不得在非 Error 状态调用（no-op + `Log.Warning`）
- [x] **AC-13** 状态转换在 DEBUG 构建下以 `Log.Info("[SceneManager] {from} → {to}")` 形式记录

### E. 生命周期

- [x] **AC-14** `Init()` 订阅 `ISceneEvent_Event.OnRequestSceneChange` 并在 `Dispose()` 取消订阅；调用 `RegisterListener` / `UnregisterListener` 严格配对（ADR-027 §3）
- [x] **AC-15** 测试 asmdef 无 `[EventInterface]` 接口时，`ISceneEvent_Gen` / `ISceneEvent_Event` **不得**被 `EventInterfaceGenerator` 误生成到 `EditModeTests` 编译单元（TENGINE-SG-002 遗留问题防回归：测试 setup 使用 `GameEvent.EventMgr.Init() + new ISceneEvent_Gen(dispatcher)` 工作区模式，**不**依赖 `GameEventHelper.Init()`）

---

## Implementation Notes

*Derived from ADR-009 + ADR-027 + S2-03/S2-04 sibling-story precedent (`IChapterStateEvent` / `ChapterStateEventBridge`):*

### 新建文件 1：`Assets/GameScripts/HotFix/GameLogic/IEvent/ISceneEvent.cs`

```csharp
// 该文件由Cursor 自动生成
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// Scene Management epic 的单一接口事件契约（ADR-027 §1）。
    /// 替代 ADR-006 的 Evt_Scene* 常量 + XxxPayload struct 方案（整体作废）。
    /// </summary>
    /// <remarks>
    /// <para><b>Sender 分工</b>：</para>
    /// <list type="bullet">
    /// <item><c>OnRequestSceneChange</c> — Chapter State / Narrative / Pause Menu 发起；Scene Manager 唯一 listener</item>
    /// <item><c>OnScene*</c>（其余 8 方法）— Scene Manager 唯一 sender；UI / Audio / Gameplay 按需订阅</item>
    /// </list>
    /// <para><b>实装范围</b>：S2-05 Story 001 实装 <c>OnRequestSceneChange</c> + <c>OnSceneReady</c>；
    /// 其余 6 方法签名在本 story 冻结，sender 实现留 S2-17 Story 005。</para>
    /// <para><b>Cascade</b>：所有方法调用不得超过 3 层级联（ADR-027 §2 继承 ADR-006 re-entrancy 约束）。</para>
    /// </remarks>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface ISceneEvent
    {
        /// <summary>请求切换章节场景。</summary>
        /// <param name="targetChapterId">目标章节 ID（1..5；同 CurrentChapterId → 静默派发 OnSceneReady；无效值由 Scene Manager 进入 Error 状态）</param>
        /// <para>Sender: Chapter State / Narrative Event / Pause Menu / Title Screen</para>
        /// <para>Listener: Scene Manager（唯一）</para>
        /// <para>Cascade: 可能触发 OnSceneTransitionBegin / OnSceneReady</para>
        void OnRequestSceneChange(int targetChapterId);

        /// <summary>场景完全就绪，玩家输入可解锁。</summary>
        /// <param name="chapterId">就绪章节 ID</param>
        /// <para>Sender: Scene Manager（同章请求时 + Story 002/005 真实过渡 TransitionIn 结束前）</para>
        /// <para>Listener: UI（关闭 loading 覆盖）, Input（解锁），Gameplay（启用章节逻辑）</para>
        /// <para>Cascade: 可能触发 UI / Audio 后续反应</para>
        void OnSceneReady(int chapterId);

        /// <summary>[Reserved — S2-17 Story 005] 过渡开始，fade out 前的第一个广播。</summary>
        /// <para>Sender: Scene Manager (Step 3 of 11)</para>
        /// <para>Listener: UI（锁输入 + 显示 overlay）, Audio（BGM 渐弱）</para>
        void OnSceneTransitionBegin(int fromChapterId, int toChapterId);

        /// <summary>[Reserved — S2-17 Story 005] 旧场景即将卸载，各系统必须释放 AssetHandle + 自移除 scene-scoped listener。</summary>
        /// <para>Sender: Scene Manager (Step 5 of 11)</para>
        /// <para>Listener: All scene-scoped systems</para>
        void OnSceneUnloadBegin(int chapterId);

        /// <summary>[Reserved — S2-17 Story 005] YooAsset 场景包下载进度（仅首次加载未缓存时触发）。</summary>
        /// <para>Sender: Scene Manager (Step 8 of 11, during download)</para>
        /// <para>Listener: UI（下载进度条）</para>
        void OnSceneDownloadProgress(float progress, long downloadedBytes, long totalBytes);

        /// <summary>[Reserved — S2-17 Story 005] 场景加载进度（0..1）。</summary>
        /// <para>Sender: Scene Manager (Step 9 of 11, during LoadSceneAsync)</para>
        /// <para>Listener: UI（加载进度条）</para>
        void OnSceneLoadProgress(string sceneName, float progress);

        /// <summary>[Reserved — S2-17 Story 005] 场景加载完成（资源就绪，尚未 fade in）。BgmAsset 由 Luban chapter 配置转发。</summary>
        /// <para>Sender: Scene Manager (Step 10 of 11)</para>
        /// <para>Listener: Audio（切换 BGM）</para>
        void OnSceneLoadComplete(int chapterId, string bgmAsset);

        /// <summary>[Reserved — S2-17 Story 005] Fade in 完成，过渡结束（OnSceneReady 之后立即触发）。</summary>
        /// <para>Sender: Scene Manager (Step 11 of 11)</para>
        /// <para>Listener: All（过渡结束确认）</para>
        void OnSceneTransitionEnd(int chapterId);

        /// <summary>[Reserved — S2-17 Story 005] 加载失败；重试已耗尽。</summary>
        /// <para>Sender: Scene Manager (Error path, after MAX_LOAD_RETRY exhausted)</para>
        /// <para>Listener: UI（错误对话框）</para>
        void OnSceneLoadFailed(int chapterId, string error);
    }
}
```

### 新建文件 2：`Assets/GameScripts/HotFix/GameLogic/Scene/SceneManager.cs`

```csharp
// 该文件由Cursor 自动生成
using System;
using TEngine;

namespace GameLogic
{
    public enum SceneManagerState
    {
        Idle, TransitionOut, Unloading, Loading, TransitionIn, Error
    }

    public interface ISceneManager
    {
        SceneManagerState CurrentState { get; }
        int CurrentChapterId { get; }
        bool IsTransitioning { get; }
    }

    public sealed class SceneManager : ISceneManager, IDisposable
    {
        private SceneManagerState _state = SceneManagerState.Idle;
        private int _currentChapterId = -1;
        private int? _pendingTargetChapterId;
        private bool _initialized;

        public SceneManagerState CurrentState => _state;
        public int CurrentChapterId => _currentChapterId;
        public bool IsTransitioning =>
            _state != SceneManagerState.Idle && _state != SceneManagerState.Error;

        public void Init()
        {
            if (_initialized && IsTransitioning)
            {
                Log.Warning("[SceneManager] Init called while transition in progress — ignored.");
                return;
            }

            if (!_initialized)
            {
                GameEvent.AddEventListener<int>(
                    ISceneEvent_Event.OnRequestSceneChange, OnRequestSceneChange);
            }
            _initialized = true;
        }

        public void Dispose()
        {
            if (!_initialized) return;
            GameEvent.RemoveEventListener<int>(
                ISceneEvent_Event.OnRequestSceneChange, OnRequestSceneChange);
            _initialized = false;
        }

        // Test hook — Story 002 将在内部状态流转中调用
        internal void AdvanceStateForTest(SceneManagerState next)
        {
            TransitionTo(next);
            if (next == SceneManagerState.Idle) DrainPending();
        }

        public void RecoverToIdle()
        {
            if (_state != SceneManagerState.Error)
            {
                Log.Warning($"[SceneManager] RecoverToIdle called in {_state}; no-op.");
                return;
            }
            TransitionTo(SceneManagerState.Idle);
            DrainPending();
        }

        private void OnRequestSceneChange(int targetChapterId)
        {
            if (_state == SceneManagerState.Error)
            {
                Log.Warning($"[SceneManager] OnRequestSceneChange({targetChapterId}) dropped — Error state.");
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
                TransitionTo(SceneManagerState.TransitionOut);
                // Story 002 接管后续 11 步流程
                return;
            }

            // Non-Idle（TransitionOut / Unloading / Loading / TransitionIn）
            _pendingTargetChapterId = targetChapterId; // newest wins
        }

        private void DrainPending()
        {
            if (!_pendingTargetChapterId.HasValue) return;
            int next = _pendingTargetChapterId.Value;
            _pendingTargetChapterId = null;

            if (next == _currentChapterId && _currentChapterId != -1)
            {
                GameEvent.Get<ISceneEvent>().OnSceneReady(next);
                return;
            }
            _currentChapterId = next;
            TransitionTo(SceneManagerState.TransitionOut);
        }

        private void TransitionTo(SceneManagerState next)
        {
#if UNITY_EDITOR || DEBUG
            Log.Info($"[SceneManager] {_state} → {next}");
#endif
            _state = next;
        }
    }
}
```

### 测试新建文件：`Assets/Tests/EditMode/SceneManagement/SceneStateMachineTests.cs`

- `[SetUp]` 复用 TENGINE-SG-002 fixture 模式：`GameEvent.EventMgr.Init() + new ISceneEvent_Gen(GameEvent.EventMgr.GetDispatcher())`（**不**调 `GameEventHelper.Init()`，规避 test asmdef 里空方法体问题）
- 每测新建 `SceneManager` + `Init()`；`[TearDown]` 调 `Dispose()`
- 派发 / 监听 `ISceneEvent` 方法做 AC-7 / AC-8 / AC-9 / AC-11 验证
- 使用 `AdvanceStateForTest(SceneManagerState.Idle)` 模拟 Story 002 真实过渡路径驱动 DrainPending

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002（S2-?）：TransitionOut → Unloading → Loading → TransitionIn 实际 11 步流程；YooAsset `LoadSceneAsync(Additive)` 调用；OnSceneTransitionBegin / OnSceneLoadComplete 等 sender 侧实装
- Story 003：Unloading 阶段的 UnloadUnusedAssets + GC.Collect；OnSceneUnloadBegin sender
- Story 004（S2-06）：与本 story 合并（mutex 逻辑在 `_pendingTargetChapterId` 已覆盖），S2-06 范围收窄为"边界场景更全的排队行为测试 + error recovery 交互"
- Story 005（S2-17）：其余 6 个 `ISceneEvent.On*` lifecycle sender 实装（签名本 story 已冻结）
- Story 006（S2-07）：章节 ID ↔ 场景名映射（Luban `TbChapter.sceneName`）；SceneManager 通过 provider delegate 解析，不直接依赖 Luban

---

## QA Test Cases

- **AC-1 / AC-2**: `ISceneEvent` 契约 + SG 产物
  - Given: `assets-refresh` 完成；EditModeTests 编译成功
  - When: 反射查 `typeof(ISceneEvent_Gen)` + 读 `ISceneEvent_Event.OnRequestSceneChange` / `OnSceneReady` / …（共 9 个 ID 字段）
  - Then: 类型非 null 且非 abstract；9 个 ID 字段值均 `!= 0` 且两两不等
  - Edge cases: 反射 9 个方法签名完全匹配 spec（用 `typeof(ISceneEvent).GetMethods()` 拉 name + parameters）

- **AC-5 / AC-6**: 初始状态 + Init 幂等
  - Given: `new SceneManager()` + `Init()`
  - Then: `CurrentState == Idle`；`CurrentChapterId == -1`；`IsTransitioning == false`
  - When: `AdvanceStateForTest(Loading)` 后再次 `Init()`
  - Then: 状态保持 `Loading`（no-op）+ `Log.Warning` 输出

- **AC-7**: Idle + 不同章 → TransitionOut
  - Given: `_state == Idle`; `_currentChapterId == -1`
  - When: `GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1)`
  - Then: 同帧 `CurrentState == TransitionOut`；`CurrentChapterId == 1`；`IsTransitioning == true`；**不**派发 OnSceneReady
  - Edge cases: 目标章 ID = 99（未知）也照常 transition（章节有效性是 Story 006 职责）

- **AC-8**: Idle + 同章 → no-op + OnSceneReady
  - Given: `_state == Idle`, `_currentChapterId == 2`（用内部测试 API 预置）
  - When: `OnRequestSceneChange(2)`
  - Then: 记录到一个 `ISceneEvent` listener：收到 `OnSceneReady(2)` 恰 1 次；`CurrentState` 保持 `Idle`；`_pendingTargetChapterId` 仍 null
  - Edge cases: 首次开机 `CurrentChapterId == -1` + 请求 -1（极端）应走 Idle + 不同章分支（`_currentChapterId != -1` 守卫）

- **AC-9**: 非 Idle → 排队；最新覆盖最旧
  - Given: `AdvanceStateForTest(Loading)`；发送 `OnRequestSceneChange(2)`, 然后 `OnRequestSceneChange(3)`
  - Then: 内部 `_pendingTargetChapterId == 3`（2 被覆盖）；`CurrentState` 仍 `Loading`；无 OnSceneReady 派发
  - Edge cases: 连续发 10 个请求，最终 `_pendingTargetChapterId` = 最后 1 个

- **AC-10**: Error → drop + Warning
  - Given: `AdvanceStateForTest(Error)`
  - When: `OnRequestSceneChange(3)`
  - Then: `_state == Error`, `_pendingTargetChapterId == null`, 无 OnSceneReady；`Log.Warning` 输出包含 "dropped"

- **AC-11**: pending drain on return-to-Idle
  - Given: `_state == Loading`; 发送 2 次 OnRequestSceneChange(4)→(5)；`_pendingTargetChapterId == 5`
  - When: `AdvanceStateForTest(Idle)`
  - Then: 同帧 `CurrentState == TransitionOut`；`CurrentChapterId == 5`；`_pendingTargetChapterId == null`
  - Edge cases: pending 与 current 同章 → drain 时走 OnSceneReady 分支，状态保持 Idle

- **AC-12**: Error → Idle recovery
  - Given: `AdvanceStateForTest(Error)`
  - When: `RecoverToIdle()`
  - Then: `CurrentState == Idle`；若先在 Error 时攒了 pending（AC-10 场景会 drop），此处仅验证状态转换本身
  - Edge cases: 非 Error 状态调 `RecoverToIdle()` → 状态不变 + `Log.Warning`

- **AC-13**: debug transition log
  - Given: DEBUG build；`LogAssert.Expect` 监听 "[SceneManager]" 前缀
  - When: 触发任意状态转换
  - Then: 恰 1 条 `Log.Info` 匹配 `"[SceneManager] {from} → {to}"` 格式

- **AC-14**: listener lifecycle pairing
  - Given: `Init()` 后；dispatcher 内部持有 1 个 `OnRequestSceneChange` listener
  - When: `Dispose()`
  - Then: 同 listener 已移除（再派发 `OnRequestSceneChange(9)` 不触发任何状态变化）

- **AC-15**: TENGINE-SG-002 防回归
  - Given: EditModeTests.asmdef（没有 `[EventInterface]` 接口）
  - When: 查看 `Library/Bee/.../EditModeTests.dll` 或 `Temp/obj/EditModeTests/SourceGenerator/`
  - Then: **无** `GameEventHelper.g.cs` 或 `ISceneEvent_Gen.g.cs` 被生成到 EditModeTests 编译单元（由 `GameLogic.asmdef` 独占生成）
  - Note: 此 AC 主要靠 SetUp 模式"绕过 GameEventHelper.Init()"生效，验证现有 SG 逻辑不误生成

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/Tests/EditMode/SceneManagement/SceneStateMachineTests.cs` — must exist and pass (预计 12-14 NUnit tests 覆盖 AC-1..AC-15)

**Status**: [x] Created & Passing — `SceneStateMachineTests.cs` 含 23 NUnit tests，全绿；完整 EditMode suite 216/216 通过。

---

## Dependencies

- Depends on: None（Scene epic 第一个 story；本 story 同时建 `ISceneEvent` 接口基础设施，后续 story-002..006 均依赖）
- Unlocks: Story 002（additive scene loading）, Story 003（cleanup sequence），Story 005（6 lifecycle senders），Story 006（Luban chapter mapping）

---

## Implementation Log (2026-04-24)

### 实际落地文件

| 路径 | 角色 | 行数 | 说明 |
|---|---|---|---|
| `Assets/GameScripts/HotFix/GameLogic/IEvent/ISceneEvent.cs` | 接口契约 | 92 | 9 方法 + `[EventInterface(EEventGroup.GroupLogic)]` + sender/listener/cascade XML doc |
| `Assets/GameScripts/HotFix/GameLogic/Scene/SceneManager.cs` | 状态机实装 | 210 | 6 态 + pending 队列 + `Init/Dispose/RecoverToIdle` + test hook |
| `Assets/Tests/EditMode/SceneManagement/SceneStateMachineTests.cs` | 测试 | 413 | 23 NUnit tests 覆盖 AC-1..AC-15 |

### 偏离 Implementation Notes 的点（均为改进，不是降级）

1. **`AdvanceStateForTest` / `PendingTargetChapterIdForTest` 由 `internal` 改 `public`**
   - 原因：EditModeTests asmdef 与 GameLogic asmdef 分离，`internal` 跨 asmdef 不可见，引入 `InternalsVisibleTo` 属于新增 assembly 级机制（跨 Story 风险）。
   - 退路：沿用 sibling `ChapterStateManager` 公开测试桩的先例（`OnPuzzleStateChangedCallback` 等），通过 `ForTest` 后缀命名明示非生产 API，并在 XML doc 中写明"生产代码不得调用"。
   - 影响：无功能差异；Story 002 位于同 asmdef，访问策略不变。

2. **修复 `GameLogic.SceneManager` 与 `UnityEngine.SceneManagement.SceneManager` 的符号冲突**
   - 问题：新建的 `GameLogic.SceneManager` 在命名空间内遮蔽 Unity 原生同名类，导致 `SP011_YooAssetAdditive.cs` 和 `SingletonSystem.cs` 编译失败（CS0117 `sceneCount` / `LoadScene` 不存在）。
   - 修复：两文件各加 `using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;` 别名，将 7 个 Unity 原生调用点改为 `UnitySceneManager.XXX`。
   - 影响：仅影响遗留引用点，语义不变；未来 HotFix 代码若使用 Unity 原生 scene API 需沿用该别名约定（应加入 control manifest 下一轮更新）。

### 测试验证

- `assets-refresh` → OK（无编译错误）
- `tests-run` EditMode → **216/216 绿**（其中本 story 新增 23 tests，全部通过）
- `LogAssert.Expect` 用于 AC-13（状态转换日志）、AC-6（Init 再调 Warning）、AC-10（Error drop Warning）、AC-12（非 Error 态 RecoverToIdle Warning）
- TENGINE-SG-002 fixture 模式（`GameEvent.EventMgr.Init() + new ISceneEvent_Gen(dispatcher)`）复用成功，未触发 `GameEventHelper.g.cs` 在 EditModeTests 误生成（AC-15）

### 遗留动作（不阻塞本 Story 完成）

- Control Manifest 下一轮更新加入规则：「HotFix 代码引用 `UnityEngine.SceneManagement.SceneManager` 必须使用 `UnitySceneManager` 别名，避免与 `GameLogic.SceneManager` 冲突」。
- Story 002（additive 11 步流程）将替换 `AdvanceStateForTest` 为真实状态推进路径；彼时本 hook 仍保留供单元测试使用。
