// 该文件由Cursor 自动生成

# Story 002: Touch-to-World Drag with Boundary Clamping

> **Epic**: Object Interaction
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-29 evening (X1 patch 3: 加 `Initialize() / Shutdown()` 显式生命周期入口 — EditMode `[Test]` 中 PlayerLoop 不驱动 MonoBehaviour `OnEnable/OnDisable`，须显式调；OnEnable/OnDisable 仅委托。X1 patch 2: InteractableObject 自订阅 IGestureEvent.OnDrag (push) — 与 story-007 §Drag 转发"Updated Phase 由 InteractableObject Update 自行处理"对齐；Coordinator 仅在 Began/Ended 调 fsm trigger；rebound 收敛为 race-fallback 语义)

## Context

**GDD**: `design/gdd/object-interaction.md`
**Requirement**: `TR-objint-004`, `TR-objint-011`, `TR-objint-017`
*(Drag 1:1 tracking; InteractionBounds rebound; drag response ≤ 16ms)*

**ADR Governing Implementation**: ADR-013（**Accepted**, 2026-04-29）+ ADR-010 (Input) + ADR-027 (GameEvent Interface Protocol)
**ADR Decision Summary**: Drag 1:1 跟手指（无 physics、无 inertia）。`transform.position` 在 Update 中直接更新（fsm.CurrentState == Dragging 时）。`InteractionBounds` 来自 Luban `TbPuzzle`，定义 2D 矩形边界；超界 clamp。Drag 结束时如最终位置在界外，DOTween EaseOutBack 回弹（0.25s）。drag 响应 ≤ 16ms（同帧）。

**Engine**: Unity 2022.3.62f2 LTS + DOTween | **Risk**: LOW
**Engine Notes**: drag 数据流为 **push 模式**——`InteractableObject` 在 `OnEnable` 中以 per-method 写法 `GameEvent.AddEventListener<GestureData>(IGestureEvent_Event.OnDrag, OnDrag)` 自订阅 `IGestureEvent.OnDrag`；handler 仅在 `Fsm.CurrentState == Dragging` 且 `data.Phase ∈ {Began, Updated}` 时缓存 `ScreenPos` 到 `_lastDragScreenPos` 并置 `_hasFreshDrag = true`，其他状态/phase 早期 return（state-filter 廉价 enum 检查）。`Update` 中读取缓存 → `Camera.ScreenToWorldPoint` → `Mathf.Clamp` → `transform.position`。**Began/Ended phase 仍由 InteractionCoordinator (Story 007) 调用 `_selectedObject.Fsm.OnDragBegan / OnDragEnded` 来驱动 fsm 转换**（Coordinator 是 Began/Ended 的单一 sender；InteractableObject 不调 fsm trigger）。Screen-to-world 用 `[SerializeField] Camera _gameplayCamera`（场景搭建时拖入；fail-loud 检查 — 禁止 per-frame `Camera.main`）。性能预算：N=10 obj × 60fps × Updated phase = 600 dispatch/sec × `(enum check + ref check)` < 0.05ms（远小于 1ms guardrail）。

**Lifecycle hooks (X1 patch v3)**：`InteractableObject` 提供 `public Initialize() / Shutdown()` 显式生命周期入口（幂等），是组件初始化与收尾的**单一事实源**（`Fsm` 构造、provider 解析、camera 检查、`IInteractionEvent.OnInteractionLockChanged` + `IGestureEvent.OnDrag` listener 注册/反注册、rebound DOKill）。生产路径由 Unity `OnEnable` / `OnDisable` 自动委托（`OnEnable() => Initialize()`、`OnDisable() => Shutdown()`），与 PlayMode 行为完全等价。**EditMode `[Test]`** 中 PlayerLoop 不驱动 MonoBehaviour lifecycle hooks，测试须**显式**调 `io.Initialize()` / `io.Shutdown()`（与 `SceneManager.Init() / Dispose()` 同模式 —— S2-05 已确立此先例）。设计权衡：把生命周期内容从 Unity 私有 hook 抽到 public 方法，零生产开销（hook 仅一行委托），而测试可控性大幅提升。

**Control Manifest Rules (this layer)**:
- Required: `No physics simulation: objects follow finger directly — no rigidbody, no inertia` (ADR-013)
- Required: `所有参数从 Luban 配置读取（InteractionBounds from TbPuzzle）` (ADR-013, ADR-007)
- Required: `Cache camera reference — never call Camera.main per frame` (ADR-012)
- Required: `Drag response ≤ 16ms (1 frame)` (ADR-013)
- Required: `所有 epic 间通信使用 IGestureEvent / IInteractionEvent 接口（ADR-027）`
- Forbidden: `禁止使用 EventId.Evt_DragGesture / Evt_TapGesture 等 ADR-006 常量`
- Guardrail: `Object Interaction total update (10 objects) ≤ 1.0ms/frame` (ADR-013)

---

## Acceptance Criteria

*From GDD `design/gdd/object-interaction.md` + ADR-013，scoped to this story:*

- [x] InteractableObject `OnEnable` 中以 per-method 写法注册 `IGestureEvent.OnDrag` listener（`GameEvent.AddEventListener<GestureData>(IGestureEvent_Event.OnDrag, OnDrag)`）；`OnDisable` 配对 RemoveEventListener — *实施：通过 `Initialize()` / `Shutdown()` 入口，OnEnable/OnDisable 一行委托；测试 AC8_Shutdown_RemovesListener 验证*
- [x] `OnDrag` handler 早期 return 守卫：`Fsm == null` 或 `Fsm.CurrentState != Dragging` 或 `data.Phase ∉ {Began, Updated}` → 立即 return；其他情况缓存 `_lastDragScreenPos = data.ScreenPosition` 并置 `_hasFreshDrag = true` — *测试：AC4_Locked / AC4_Idle / AC7_DoesNotCacheScreenPos_OnEnded / AC8_MultiInstance*
- [x] InteractableObject `Update` 中：`if (Fsm == null || Fsm.CurrentState != Dragging || !_hasFreshDrag) return;` → `worldPos = _gameplayCamera.ScreenToWorldPoint(new Vector3(_lastDragScreenPos.x, _lastDragScreenPos.y, _dragDepth))` → 记录 `_lastWorldPosUnclamped = worldPos` → `transform.position` 写入 clamped (`Mathf.Clamp(worldPos.x, MinX, MaxX)`，Y 同理，Z 保持) → `_hasFreshDrag = false` — *测试：AC1_FollowsFinger / AC2_ClampsToBounds / AC2_ClampsXAndYIndependently*
- [x] Camera 引用：`[SerializeField] Camera _gameplayCamera`（场景搭建时拖入）；`OnEnable` fail-loud：若 null 调 `Log.Error`（**绝不**fallback `Camera.main`，per ADR-012）；TickDrag 因 `_gameplayCamera == null` 早期 return → drag 不工作但 fsm 仍可被 lock listener 等其他路径正常驱动 — *实装路径与 AC-9 fail-loud 同源（仅 Log.Error 不 disable 组件）；grep evidence: 0 实际 Camera.main 调用*
- [x] **每帧 clamp**（生产主路径）：drag 期间 `transform.position.x = Mathf.Clamp(worldPos.x, bounds.MinX, bounds.MaxX)`（Y 同理）；物体永远不超出 bounds — *测试：AC2 系列*
- [x] **超界 rebound（race-fallback 路径）**：fsm.StateChanged Dragging→Snapping 时检查 `_lastWorldPosUnclamped` 是否在 bounds 外 **AND** `Vector3.Distance(transform.position, clampedFingerPos) > 0.001f`；满足才 `transform.DOMove(clampedFingerPos, 0.25f).SetEase(Ease.OutBack)` 启动；置 `IsReboundActive = true`，OnComplete 时清。**生产主路径中此条件几乎不触发**（每帧 clamp 已让 transform.position == clampedFingerPos）；保留作为 race condition fallback（多帧 skip / 时序竞争）。Story 004 grid snap 在 `IsReboundActive == true` 时推迟启动（参 story-004 §Engine Notes）— *测试：AC3 三测试覆盖启动路径 + 主路径 transform-at-clamped 退化路径 + finger-in-bounds 路径*
- [x] Drag 期间**不** snap to grid — grid snap 仅在 fsm.CurrentState == Snapping 时由 Story 004 触发 — *🟦 DEFERRED to S2-10：本 story scope 只验"drag 期间 fsm 在 Dragging 不在 Snapping"（隐含于 AC-3 测试中 fsm.OnDragEnded 才转 Snapping）；snap 触发本身在 S2-10 实施*
- [x] Drag 响应：从 `IGestureEvent.OnDrag(Updated)` 接收到 `transform.position` 更新 ≤ 16ms（同一 frame 内完成；TickDrag 在 Update 阶段 1×ScreenToWorldPoint + 2×Clamp + 1×assign）— *⚠️ ADVISORY：静态代码评估 pass（路径明显 < 0.05ms）；缺 PlayMode Profiler 真机数据 — 留 Polish 阶段补*
- [x] **InteractionBounds 来源**：`PuzzleConfig` POCO（含 InteractionBounds 子 struct: MinX/MaxX/MinY/MaxY），通过 `InteractableObject.RegisterPuzzleConfigProvider(Func<int, PuzzleConfig>)` 静态注入（**与 S2-07 `RegisterChapterDataProvider` 同模式**，不依赖 Luban TbPuzzle 是否生成）；`OnEnable` 通过 `_puzzleId` 解析 config，fail-loud：provider 未注册或返 null → `Log.Error`（**不**禁用组件 — 解耦 listener 注册 vs 数据可用性，方便测试可观测性 + lock listener 仍可驱动 fsm）；TickDrag 因 `_puzzleConfig == null` 早期 return 等效"drag 不可用" — *测试：AC5_LogsError_WhenProviderNotRegistered / WhenProviderReturnsNull*
- [x] Locked 状态防御：fsm.CurrentState == Locked 时 `OnDrag` 早期 return；`Update` 早期 return（双层守卫）；Coordinator 的 `fsm.OnDragBegan` 调用在 Locked 状态下被 fsm 自身静默丢弃（Story 001 转换规则）— *测试：AC4_OnDrag_IsNoOp_WhenFsmIsLocked / IsIdle*
- [x] **协议合规**：本 story 实装 0 处使用 `EventId.Evt_DragGesture` / `Evt_TapGesture` 等 ADR-006 常量；订阅形如 `GameEvent.AddEventListener<GestureData>(IGestureEvent_Event.OnDrag, ...)` — *测试：AC6_IGestureEvent_HasEventInterfaceAttribute_GroupLogic + grep evidence `production/qa/grep-no-evt-objectinteraction-drag-2026-04-29.md`*
- [x] **显式生命周期入口**：`InteractableObject` 暴露 `public void Initialize()` / `public void Shutdown()`，幂等实现（`Initialize` 在 `Fsm != null` 时直接 return；`Shutdown` 在 `_listenersRegistered == false` 时跳过 listener 反注册）。`OnEnable` / `OnDisable` 仅一行委托。EditMode 测试须显式调 `Initialize() / Shutdown()`（PlayerLoop 在 EditMode 不驱动 hook）— *测试：AC8_Shutdown_RemovesListener 直接验证；AC1..AC8 全部 14 测试通过 io.Initialize() 间接验证 OnEnable 路径完整*

---

## Implementation Notes

*Derived from ADR-013 §"Architecture" + Story 007 §Drag 转发约束（Coordinator 仅 Began/Ended）— X1 patch v3 实际代码（已与 `Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/InteractableObject.cs` 字节级一致）：*

```csharp
public sealed class InteractableObject : MonoBehaviour
{
    [SerializeField] private int _objectId = -1;
    [SerializeField] private int _puzzleId = -1;
    [SerializeField] private float _dragDepth = 10f;
    [SerializeField] private Camera _gameplayCamera;

    private static Func<int, PuzzleConfig> _puzzleConfigProvider;
    public static void RegisterPuzzleConfigProvider(Func<int, PuzzleConfig> p) => _puzzleConfigProvider = p;

    public InteractableObjectFsm Fsm { get; private set; }
    public bool IsReboundActive { get; private set; }   // Story 004 协调点
    public Vector3 LastWorldPosUnclamped { get; private set; }

    private PuzzleConfig _puzzleConfig;
    private Vector2 _lastDragScreenPos;
    private bool _hasFreshDrag;
    private bool _listenersRegistered;

    private const float ReboundDurationSeconds = 0.25f;
    private const float ReboundDistanceEpsilon = 0.001f;

    // —— Lifecycle (X1 patch v3): public Initialize/Shutdown 是单一事实源，OnEnable/OnDisable 一行委托。
    //    EditMode [Test] 中 PlayerLoop 不驱动 MonoBehaviour hook，测试须显式调 io.Initialize() / io.Shutdown()。
    public void Initialize()
    {
        if (Fsm != null) return;   // 幂等

        Fsm = new InteractableObjectFsm(_objectId);
        Fsm.StateChanged += OnFsmStateChanged;

        // fail-loud 仅 Log.Error，不 disable 组件（解耦 listener 注册 vs 数据可用性）
        ResolvePuzzleConfig();
        if (_gameplayCamera == null)
            Log.Error($"[InteractableObject#{_objectId}] _gameplayCamera 未配置 — drag 不可用");

        GameEvent.AddEventListener<bool>(IInteractionEvent_Event.OnInteractionLockChanged, OnInteractionLockChanged);
        GameEvent.AddEventListener<GestureData>(IGestureEvent_Event.OnDrag, OnDrag);
        _listenersRegistered = true;
    }

    public void Shutdown()
    {
        if (_listenersRegistered)
        {
            GameEvent.RemoveEventListener<bool>(IInteractionEvent_Event.OnInteractionLockChanged, OnInteractionLockChanged);
            GameEvent.RemoveEventListener<GestureData>(IGestureEvent_Event.OnDrag, OnDrag);
            _listenersRegistered = false;
        }
        if (IsReboundActive) { transform.DOKill(complete: false); IsReboundActive = false; }
        if (Fsm != null) { Fsm.StateChanged -= OnFsmStateChanged; Fsm = null; }
        _hasFreshDrag = false;
    }

    private void OnEnable() => Initialize();
    private void OnDisable() => Shutdown();
    private void Update() => TickDrag();

    // testability: TickDrag 由 EditMode test 直接 invoke（public 因 asmdef 间 internal 不可见，沿用 S2-05 SceneManager 先例）
    public void TickDrag()
    {
        if (Fsm == null || Fsm.CurrentState != InteractableObjectState.Dragging) return;
        if (!_hasFreshDrag) return;
        if (_gameplayCamera == null || _puzzleConfig == null) return;   // fail-loud 后 drag 静默失效

        var worldPos = _gameplayCamera.ScreenToWorldPoint(
            new Vector3(_lastDragScreenPos.x, _lastDragScreenPos.y, _dragDepth));
        LastWorldPosUnclamped = worldPos;

        var b = _puzzleConfig.InteractionBounds;
        var pos = transform.position;
        pos.x = Mathf.Clamp(worldPos.x, b.MinX, b.MaxX);
        pos.y = Mathf.Clamp(worldPos.y, b.MinY, b.MaxY);
        transform.position = pos;
        _hasFreshDrag = false;
    }

    // IGestureEvent.OnDrag per-method listener（push 模式；state + phase 双过滤早期 return）
    private void OnDrag(GestureData data)
    {
        if (Fsm == null || Fsm.CurrentState != InteractableObjectState.Dragging) return;
        if (data.Phase != GesturePhase.Updated && data.Phase != GesturePhase.Began) return;
        _lastDragScreenPos = data.ScreenPosition;
        _hasFreshDrag = true;
    }

    private void OnFsmStateChanged(InteractableObjectState prev, InteractableObjectState next)
    {
        if (prev == InteractableObjectState.Dragging && next == InteractableObjectState.Snapping)
            TryStartRebound();
    }

    // Race-fallback rebound：生产主路径上 transform.position 已每帧 clamp == clampedFingerPos，
    // 距离永远 < epsilon → 跳过 DOMove。仅在多帧 skip / 时序竞争时触发兜底视觉弹回。
    private void TryStartRebound()
    {
        if (_puzzleConfig == null) return;
        var b = _puzzleConfig.InteractionBounds;
        bool fingerOutOfBounds =
            LastWorldPosUnclamped.x < b.MinX || LastWorldPosUnclamped.x > b.MaxX ||
            LastWorldPosUnclamped.y < b.MinY || LastWorldPosUnclamped.y > b.MaxY;
        if (!fingerOutOfBounds) return;

        var pos = transform.position;
        var clamped = new Vector3(
            Mathf.Clamp(LastWorldPosUnclamped.x, b.MinX, b.MaxX),
            Mathf.Clamp(LastWorldPosUnclamped.y, b.MinY, b.MaxY),
            pos.z);
        if (Vector3.Distance(pos, clamped) < ReboundDistanceEpsilon) return;   // 主路径退化分支

        IsReboundActive = true;
        transform.DOMove(clamped, ReboundDurationSeconds)
            .SetEase(DG.Tweening.Ease.OutBack)   // 显式限定，避开 TEngine.Ease 命名空间冲突
            .OnComplete(() => IsReboundActive = false);
    }

    private bool ResolvePuzzleConfig()
    {
        if (_puzzleConfigProvider == null)
        {
            Log.Error($"[InteractableObject#{_objectId}] PuzzleConfigProvider 未注册");
            return false;
        }
        _puzzleConfig = _puzzleConfigProvider(_puzzleId);
        if (_puzzleConfig == null)
        {
            Log.Error($"[InteractableObject#{_objectId}] PuzzleConfigProvider 对 puzzleId={_puzzleId} 返回 null");
            return false;
        }
        return true;
    }
}
```

> **设计权衡 (X1 patch v2)**：drag 数据流改回 push 模式（InteractableObject 自订阅 IGestureEvent.OnDrag），原因：
> - **AC 一致性**：与 story-007 §Drag 转发"Updated Phase 由 InteractableObject Update 自行处理"对齐（之前 pull-via-Coordinator 写法与 story-007 矛盾）
> - **解耦 S2-09 / S2-13**：S2-09 不再依赖 InteractionCoordinator (S2-13) 的"GestureData 缓存"私下 API；只依赖 fsm 触发方法（Began/Ended 仍由 Coordinator 调用）
> - **性能预算充足**：10 obj × 60fps × Updated phase = 600 dispatch/sec × `(enum check + ref check)` < 0.05ms（远小于 1ms guardrail）；state-filter 早期 return 廉价
> - **职责清晰**：Coordinator 负责 Began/Ended (fsm transition driver)；InteractableObject 负责 Updated (position consumer)；不存在共享缓存的隐藏耦合

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: FSM 状态转换（DraggingState 已声明，本 story 仅实施 Update 中的 1:1 跟踪 + clamp）
- Story 003: rotation 子模式（drag + rotate 可同时启用，但 rotation 数据流独立）
- Story 004: grid snap on release（rebound 完成 / 直接 OnDragEnded → Snapping → 由 Story 004 接管）

---

## QA Test Cases

*EditMode 单元 + PlayMode 集成：*

- **AC-1**: position 1:1 跟手指
  - Given: fsm.state == Dragging；touch screen=(100, 200)
  - When: 下一帧渲染（InteractableObject Update 执行）
  - Then: transform.position 与 `_gameplayCamera.ScreenToWorldPoint((100,200,dragDepth))` 误差 ≤ 0.01 world units
  - Edge: 5 帧内 rapid drag 跨全屏 — 无丢帧、无 lag 累计

- **AC-2**: 边界 clamp 防出界
  - Given: bounds.MaxX = 3.0；object 在 X=2.9
  - When: 手指 drag 到 world X=4.5
  - Then: object X 被 clamp 在 3.0；不超出 bounds
  - Edge: 对角 drag 进 corner — X 与 Y 独立 clamp

- **AC-3**: 超界 rebound（race-fallback 路径）
  - Given: 模拟时序竞争 — `_lastWorldPosUnclamped = (4.5, 0, 0)`（finger 在外）AND `transform.position = (2.0, 0, 0)`（在 bounds 内某非边界点；模拟一帧 Update 没跑导致未及时 clamp）；MaxX=3.0
  - When: fsm.OnDragEnded 触发 fsm.StateChanged Dragging→Snapping
  - Then: `IsReboundActive == true`；`DOMove(clamped=(3.0,0,0), 0.25f, OutBack)` 启动；OnComplete 后 `IsReboundActive == false`
  - Edge 1（生产主路径）: transform.position 每帧 clamp 已 == clamped finger pos（距离 < 0.001）→ 跳过 DOMove，IsReboundActive 保持 false
  - Edge 2（释放时 in-bounds）: `_lastWorldPosUnclamped` 在 bounds 内 → 跳过 DOMove，IsReboundActive 保持 false

- **AC-4**: Locked 状态无 drag
  - Given: fsm.state == Locked
  - When: Coordinator 派 OnDrag（Updated phase）
  - Then: object position 不变（fsm.OnDragBegan 在 Locked 静默；fsm.CurrentState 不为 Dragging；Update 早期 return）
  - Edge: 锁定 mid-drag — object 停在当前位置（无 snap，无 rebound）

- **AC-5**: bounds 来自 PuzzleConfigProvider（避免硬依赖 Luban TbPuzzle 是否生成）
  - Given: 测试注入 stub provider 返 `PuzzleConfig` 含 MinX=-5, MaxX=5, MinY=-3, MaxY=3
  - When: InteractableObject OnEnable 调 ResolvePuzzleConfig
  - Then: `_puzzleConfig` 非 null；TickDrag clamp 用注入的 bounds；C# 中 0 硬编码边界值
  - Edge 1: provider 未注册 → `Log.Error`（LogAssert.Expect 验证）；`Fsm` 仍 non-null；`_puzzleConfig` 仍 null → TickDrag 早期 return（drag 不可用）
  - Edge 2: provider 返 null → `Log.Error`；同 Edge 1 状态

- **AC-6**: 协议合规 grep
  - When: `rg "EventId\.Evt_(DragGesture|TapGesture)" Assets/GameScripts/HotFix/`
  - Then: 0 命中
  - When: `rg "Camera\.main" Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/`
  - Then: 0 命中（_gameplayCamera 通过 Inspector 注入，不允许 fallback）

- **AC-7**: Locked 时不响应 OnDrag
  - Given: fsm.OnLockChanged(true) → state == Locked
  - When: 派发 IGestureEvent.OnDrag with Phase=Updated
  - Then: `_hasFreshDrag` 保持 false；`transform.position` 不变；TickDrag 也提前 return（双层守卫）

- **AC-8**: 多实例隔离 + listener 注册/反注册
  - Given: 2 个 InteractableObject A/B 同时 OnEnable
  - When: 派发 OnDrag；后 B.OnDisable
  - Then: A 收到（A.fsm 在 Dragging 时缓存）；B disposed 后再派发 OnDrag — B 不响应（已 RemoveEventListener）

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/Tests/EditMode/ObjectInteraction/DragMechanicsTests.cs` — must exist and pass (≥ 8 NUnit tests; EditMode 通过 stub orthographic Camera + TickDrag 直接 invoke 验证 clamp 数学；rebound DOTween 用 `DOTween.ManualUpdate` 推进或仅验 IsReboundActive flag)
- `production/qa/grep-no-evt-objectinteraction-drag-<date>.md` — grep 证据（0 EventId.Evt_DragGesture/TapGesture 残留 + 0 Camera.main 在 ObjectInteraction 子目录）

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001 / **S2-08**（DraggingState + OnDragBegan/OnDragEnded 触发方法 — DONE 2026-04-29）
- Pre-condition: ADR-013 = **Accepted**（✅ 2026-04-29）；`IInteractionEvent.cs` 已存在（✅ 2026-04-29）；`IGestureEvent` 已 active（✅ Sprint 1）
- **不**依赖 S2-13（InteractionCoordinator）：本 story 只要求 fsm 自身的 Dragging state 可以从外部驱动（在 EditMode 测试中通过手动调 fsm.OnTapHit + OnDragBegan 模拟）；S2-13 在 production 路径上负责调用 fsm.OnDragBegan/OnDragEnded（Began/Ended 转发）
- Unlocks: Story 004 / S2-10（grid snap — IsReboundActive 协调点 + bounds 数据共享 IPuzzleConfig），Story 007 / S2-13（multi-object drag coordination — Coordinator 不再需要 InteractableObject 私下 SetDragGestureData API）

---

## Completion Notes

**Completed**: 2026-04-29
**Criteria**: 12/12 passing（AC-7 DEFERRED to S2-10：grid snap 触发由 S2-10 实施；AC-8 ADVISORY：静态代码评估 pass，PlayMode Profiler 真机 profile 留 Polish 阶段）
**Deviations**:
- ADVISORY: §Implementation Notes 代码示例（X1 patch v3 同步刷新完成）— 由收尾时一并更新到当前实施版本
- ADVISORY: AC-8 drag 响应 ≤ 16ms 仅静态评估 pass，未 PlayMode Profile — 路径明显 < 0.05ms（远小于 1ms guardrail），留 Polish 阶段补 profile
- OUT OF SCOPE（合理）: X1 patch v2 时改了 story-004.md（IsReboundActive 协调点）+ X1 内 push/pull 矛盾修复 — 设计层 patch 而非 implementation scope creep，已在 Manifest Version 显式记录
**Test Evidence**:
- Logic test: `Assets/Tests/EditMode/ObjectInteraction/DragMechanicsTests.cs` — 14 NUnit EditMode tests（AC-1..AC-8 + AC8_Shutdown 全覆盖；用户 2026-04-29 night 确认 Run All 全绿）
- Grep evidence: `production/qa/grep-no-evt-objectinteraction-drag-2026-04-29.md`（0 EventId.Evt_DragGesture/TapGesture 实际调用 + 0 Camera.main 实际调用 in ObjectInteraction 子目录）
**Code Review**: Skipped — Lean mode（LP-CODE-REVIEW gate 仅在 Full review 模式启用）
**X1 Patch History**:
- v1（初稿）: pull 模式，Coordinator 缓存 GestureData → InteractableObject 拉
- v2（2026-04-29 evening）: 改 push 模式 self-subscribe IGestureEvent.OnDrag — 与 story-007 §Drag 转发对齐；解耦 S2-13；rebound 收敛为 race-fallback；加 PuzzleConfig POCO + Provider 注入
- v3（2026-04-29 night）: 加 `public Initialize()/Shutdown()` 显式生命周期入口（幂等）— 修复 EditMode `[Test]` 中 Unity PlayerLoop 不驱动 MonoBehaviour OnEnable/OnDisable 导致 13/14 NUnit fail 的根因；与 S2-05 SceneManager.Init/Dispose 同模式
