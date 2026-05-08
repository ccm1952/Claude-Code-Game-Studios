// 该文件由Cursor 自动生成

# Story 007: Multiple Interactable Objects — Single Selection at a Time

> **Epic**: Object Interaction
> **Status**: Complete
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-04-29 night X3 (X3 patch v2: §Implementation Notes 修订对齐 framework — Coordinator 不实施 IGestureEvent/IInteractionEvent 接口本身（per-event 注册）；删除 SetLockManager 注入路径（InteractableObject 自订阅 OnInteractionLockChanged）；FatFingerMargin 落地为 IInputConfig.FatFingerMarginMm（mm 单位）+ InputConfigFromLuban.InitWithDefaults 默认 8mm。详见 `/.claude/memory/problem_2026-04-29_story-impl-notes-vs-framework-drift.md`)

## Context

**GDD**: `design/gdd/object-interaction.md`
**Requirement**: `TR-objint-001`, `TR-objint-003`, `TR-objint-019`, `TR-objint-021`
*(Raycast selection on layer; fat finger compensation; 10 objects ≥ 55fps on iPhone 13 Mini; 200ms selection debounce)*

**ADR Governing Implementation**: ADR-013（**Accepted**, 2026-04-29）+ ADR-010 (Input Abstraction) + ADR-027 (GameEvent Interface Protocol)
**ADR Decision Summary**: `InteractionCoordinator`（chapter scene 级 MonoBehaviour）管理多个 `InteractableObject` 实例，监听 `IGestureEvent.OnTap` 进行 Raycast 选取（命中 `Interactable` 层）。Fat finger compensation 按 DPI 缩放。**单选语义**：任意时刻最多一个 object 处于 `Selected` 或 `Dragging`。200ms debounce 防误触。10 个并发 object ≥ 55fps on iPhone 13 Mini。

**Engine**: Unity 2022.3.62f2 LTS + TEngine 6.0.0 + DOTween | **Risk**: LOW
**Engine Notes**: Raycast 用 `Physics2D.OverlapCircleAll`（2D 优先）+ 专用 `Interactable` LayerMask + 取距离 worldPos 最近的 collider（Z-depth 重叠时）。Fat finger 半径换算：`radiusPx = FatFingerMarginMm * dpi / 25.4`（mm→px），再乘 `worldUnitsPerPixel` 转 world（orthographic camera 下用 `ScreenToWorldPoint(p) - ScreenToWorldPoint(p+1px)` 求差）。`FatFingerMarginMm` 来自 `IInputConfig.FatFingerMarginMm`（默认 8mm，约对应 Apple HIG 44pt 触摸目标半径；Luban 接入时由 `TbInputConfig.FatFingerMarginMm` 列覆盖）。200ms debounce 在 Coordinator 内用 `Time.unscaledTime`。

> **X3 patch v2 修订**：原 §Implementation Notes 用 `class : MonoBehaviour, IGestureEvent` + `GameEvent.AddEventListener<IGestureEvent>(this)` 整接口订阅幻想 + `obj.SetLockManager(_lockManager)` 不存在 API。修订对齐 framework + S2-08/S2-09/S2-12 既有模式：Coordinator 是 POCO MonoBehaviour（不实施 IGestureEvent/IInteractionEvent 接口本身）+ per-event 注册 OnTap/OnDrag/OnInteractionLockChanged + InteractionLockManager 仅作为 Coordinator 私有持久字段（InteractableObject 通过自订阅 OnInteractionLockChanged 获取锁状态，**不**需 LockManager 引用注入）。Provider 注入 IInputConfig 与 InteractableObject.RegisterPuzzleConfigProvider 同模式。EditMode lifecycle 沿用 InteractableObject 的 `Initialize()` / `Shutdown()` 显式入口。

**Control Manifest Rules (this layer)**:
- Required: `Single selection: only one object selected at a time (MVP)` (ADR-013)
- Required: `Fat finger compensation: expandedRadius = colliderRadius + fatFingerMargin * (Screen.dpi / 326)` (ADR-013)
- Required: `200ms selection debounce` (ADR-013)
- Required: `所有参数从 Luban 配置读取（fatFingerMargin from TbInputConfig）` (ADR-013, ADR-007)
- Required: `所有 epic 间通信使用 IInteractionEvent / IGestureEvent 接口（ADR-027）`
- Required: `Never use GameObject.Find / FindObjectsOfType at runtime` (tech-prefs)
- Forbidden: `禁止使用 EventId.Evt_TapGesture / Evt_PuzzleLockAll / Evt_PuzzleUnlock 等 ADR-006 常量`
- Guardrail: `Object Interaction total update (10 objects) ≤ 1.0ms/frame` (ADR-013)
- Guardrail: `Drag response ≤ 16ms` (ADR-013)

---

## Acceptance Criteria

*From GDD `design/gdd/object-interaction.md` + ADR-013，scoped to this story:*

- [x] `InteractionCoordinator` 是 MonoBehaviour，通过 `[SerializeField] private List<InteractableObject> _objects` 在 Inspector 预填（**禁** `FindObjectsOfType`）
- [x] `Initialize()` 中（`OnEnable` 委托）：per-event 注册 `OnTap` / `OnDrag` / `OnInteractionLockChanged` listener；`_objects` 含 null → `Log.Error` 跳过；`_gameplayCamera` 未配置 → `Log.Error`（fail-loud）
- [x] `Shutdown()` 中（`OnDisable` 委托）：取消所有 listener + `_lockManager?.Dispose()` + `CurrentSelectedObject = null`；幂等
- [x] Tap Raycast 仅命中 `[SerializeField] LayerMask _interactableLayer`（用 `Physics2D.OverlapCircleAll`）
- [x] Fat finger 半径换算：`radiusPx = IInputConfig.FatFingerMarginMm * dpi / 25.4`；再乘 `worldUnitsPerPixel` 转 world；`FatFingerMarginMm` 默认 8mm（Apple HIG 44pt 触摸目标半径）
- [x] **单选语义**（公开 API `TrySelectObject(InteractableObject hit)`）：
  - hit==null → 当前选中走 `Fsm.OnDeselect()`（fsm 内部派 OnObjectDeselected）；clear `CurrentSelectedObject`
  - hit==CurrentSelectedObject → no-op，return false
  - 其他：当前选中（如 fsm.Selected）走 `Fsm.OnDeselect()`；新选中走 `Fsm.OnTapHit()`（fsm 内部派 OnObjectSelected）
- [x] **200ms debounce**：`Time.unscaledTime - _lastSelectionTime < 0.2f` → 整体拒绝（return false，不调任何 fsm trigger，不派任何事件）
- [x] **Drag 转发**：`OnDrag` listener 中 `Phase==Began → Fsm.OnDragBegan` / `Phase==Ended → Fsm.OnDragEnded`；`Updated` 由 InteractableObject 自身消费（push 模式，S2-09）
- [x] **Lock 转发修订**：Coordinator **自身**订阅 `OnInteractionLockChanged`（per-event），`isLocked==true` → `CurrentSelectedObject = null`（清自己持有的引用，避免悬挂指针）；fsm 转 Locked + 派 OnObjectDeselected 由 InteractableObject 自订阅完成（S2-08）
- [x] **协议合规**：本 story 实装 0 处使用 `EventId.Evt_*`；listener 用 per-event 模式（与 S2-12 InteractionLockManager / S2-08 InteractableObject 一致）；Coordinator **不**自派任何 IInteractionEvent（只调 fsm trigger 让 fsm 派）
- [x] **修订**：原 AC "obj.SetLockManager(_lockManager)" 删除 — InteractableObject 无 SetLockManager API，通过 OnInteractionLockChanged 事件路径解耦（X3 patch v2）
- [ ] **性能**（PlayMode/device 测试，留 ADVISORY）：10 个 InteractableObject 全部 Idle 时本系统 Update ≤ 1.0ms；10 个 object 1 个 Dragging on iPhone 13 Mini ≥ 55fps
- [ ] **Evt_ObjectTransformChanged 多 sender**（PlayMode 测试，留 ADVISORY）：3 个 object 同时 snap 完成 listener 收到 3 次（每个 objectId 各异）— 已在 S2-10 单 object 路径闭环（多 sender 重复 N 次即可）

---

## Implementation Notes

*Derived from ADR-013 §"Architecture" + ADR-010 + ADR-027 + S2-12 InteractionLockManager（X3 patch v2 已对齐 framework 实际能力）：*

```csharp
[DisallowMultipleComponent]
public sealed class InteractionCoordinator : MonoBehaviour
{
    [SerializeField] private List<InteractableObject> _objects = new();
    [SerializeField] private LayerMask _interactableLayer;
    [SerializeField] private Camera _gameplayCamera;

    public InteractableObject CurrentSelectedObject { get; private set; }
    public bool IsLocked => _lockManager?.IsLocked ?? false;
    public InteractionLockManager LockManager => _lockManager;
    public const float DebounceSeconds = 0.2f;

    private InteractionLockManager _lockManager;
    private IInputConfig _inputConfig;
    private float _lastSelectionTime = -999f;
    private bool _listenersRegistered;

    // Provider 注入（与 InteractableObject.RegisterPuzzleConfigProvider 同模式）
    private static Func<IInputConfig> _inputConfigProvider;
    public static void RegisterInputConfigProvider(Func<IInputConfig> provider) => _inputConfigProvider = provider;
    public static void ClearInputConfigProviderForTest() => _inputConfigProvider = null;

    public void Initialize()
    {
        if (_lockManager != null) return;   // 幂等

        _lockManager = new InteractionLockManager();
        _lockManager.Init();

        ResolveInputConfig();   // fail-loud + InitWithDefaults fallback

        for (int i = 0; i < _objects.Count; i++)
            if (_objects[i] == null) Log.Error($"[InteractionCoordinator] _objects[{i}] is null");

        if (_gameplayCamera == null)
            Log.Error("[InteractionCoordinator] _gameplayCamera 未配置 — Tap raycast 不可用");

        // per-event 注册（X3 patch v2 修订 — 不实施 IGestureEvent/IInteractionEvent 接口）
        GameEvent.AddEventListener<GestureData>(IGestureEvent_Event.OnTap, OnTap);
        GameEvent.AddEventListener<GestureData>(IGestureEvent_Event.OnDrag, OnDrag);
        GameEvent.AddEventListener<bool>(IInteractionEvent_Event.OnInteractionLockChanged, OnInteractionLockChanged);
        _listenersRegistered = true;
    }

    public void Shutdown()
    {
        if (_listenersRegistered)
        {
            GameEvent.RemoveEventListener<GestureData>(IGestureEvent_Event.OnTap, OnTap);
            GameEvent.RemoveEventListener<GestureData>(IGestureEvent_Event.OnDrag, OnDrag);
            GameEvent.RemoveEventListener<bool>(IInteractionEvent_Event.OnInteractionLockChanged, OnInteractionLockChanged);
            _listenersRegistered = false;
        }
        _lockManager?.Dispose();
        _lockManager = null;
        CurrentSelectedObject = null;
    }

    private void OnEnable() => Initialize();
    private void OnDisable() => Shutdown();

    /// 单选切换决策（公开供测试 bypass raycast）。
    public bool TrySelectObject(InteractableObject hit)
    {
        if (IsLocked) return false;
        if (Time.unscaledTime - _lastSelectionTime < DebounceSeconds) return false;

        if (hit == null)
        {
            if (CurrentSelectedObject == null) return false;
            if (CurrentSelectedObject.Fsm?.CurrentState == InteractableObjectState.Selected)
                CurrentSelectedObject.Fsm.OnDeselect();   // fsm 内部派 OnObjectDeselected
            CurrentSelectedObject = null;
            _lastSelectionTime = Time.unscaledTime;
            return true;
        }
        if (hit == CurrentSelectedObject) return false;

        if (CurrentSelectedObject?.Fsm?.CurrentState == InteractableObjectState.Selected)
            CurrentSelectedObject.Fsm.OnDeselect();
        CurrentSelectedObject = hit;
        hit.Fsm?.OnTapHit();   // fsm 内部派 OnObjectSelected
        _lastSelectionTime = Time.unscaledTime;
        return true;
    }

    private void OnTap(GestureData data)
    {
        if (data.Phase != GesturePhase.Ended) return;
        if (_gameplayCamera == null) return;
        var hit = RaycastWithFatFinger(data.ScreenPosition);
        TrySelectObject(hit);
    }

    private void OnDrag(GestureData data)
    {
        if (CurrentSelectedObject?.Fsm == null) return;
        switch (data.Phase)
        {
            case GesturePhase.Began: CurrentSelectedObject.Fsm.OnDragBegan(); break;
            case GesturePhase.Ended: CurrentSelectedObject.Fsm.OnDragEnded(); break;
            // Updated 由 InteractableObject 自身消费（push 模式，S2-09）
        }
    }

    private void OnInteractionLockChanged(bool isLocked)
    {
        // isLocked=true → 清自己引用；fsm 转 Locked 由 InteractableObject 自订阅完成
        if (isLocked) CurrentSelectedObject = null;
    }

    // Raycast 实施（公开供测试，但单测 bypass 用 TrySelectObject）
    public InteractableObject RaycastWithFatFinger(Vector2 screenPos)
    {
        if (_gameplayCamera == null || _inputConfig == null) return null;
        float zDepth = -_gameplayCamera.transform.position.z;
        var worldA = _gameplayCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, zDepth));
        var worldB = _gameplayCamera.ScreenToWorldPoint(new Vector3(screenPos.x + 1f, screenPos.y, zDepth));
        float worldPerPx = Mathf.Abs(worldB.x - worldA.x);
        float dpi = Screen.dpi > 0f ? Screen.dpi : _inputConfig.FallbackDpi;
        float radiusPx = _inputConfig.FatFingerMarginMm * dpi / 25.4f;   // mm → px
        float radiusWorld = radiusPx * worldPerPx;

        var hits = Physics2D.OverlapCircleAll((Vector2)worldA, radiusWorld, _interactableLayer);
        // 取距离 worldA 最近且属于 _objects 列表的 InteractableObject —— 见源码
        // ...
    }
}
```

**性能要点**：
- `_objects.Count` 一般 ≤ 10；遍历 + collider distance 检测 < 0.1ms
- `CurrentSelectedObject` 缓存避免每帧 raycast；只在 Tap 事件帧 raycast
- InteractableObject.Update 仅在 `CurrentState == Dragging` 时做 1:1 跟踪（Story 002）；Idle / Selected / Locked / Snapping 全部走 0 Update（DOTween 自驱动）

**X3 patch v2 修订原因**：原 §Implementation Notes 用 `class : MonoBehaviour, IGestureEvent` + `GameEvent.AddEventListener<TInterface>(this)` 整接口订阅幻想（TEngine 不支持，与 S2-12 同 drift）+ `obj.SetLockManager(_lockManager)` 不存在 API（InteractableObject 自订阅 OnInteractionLockChanged 已在 S2-08 实施）。修订对齐 framework + 既有 S2-08/S2-09/S2-12 模式，详见 `/.claude/memory/problem_2026-04-29_story-impl-notes-vs-framework-drift.md`。

**Pre-populate `_objects`**：Designer 在 Inspector 拖入 InteractableObject 引用；`Initialize` 校验 null 元素 + Log.Error 跳过（不抛）。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001-006: 单 object FSM, drag, rotation, snap, feedback, lock — must all be DONE
- Light source interaction (deferred — `IInteractionEvent.OnLightPositionChanged` 接口已冻结但实施延后)

---

## QA Test Cases

- **AC-1**: Raycast 仅命中 Interactable layer
  - Given: chapter scene 5 个 InteractableObject + 部分 UI 元素（UI layer）
  - When: Tap 命中 Object A 屏幕坐标（同位置 UI 元素也存在）
  - Then: Object A → Selected；UI 元素未被任何 fsm 触发
  - Edge: Z-depth 重叠：取最近 collider；其他 object 维持 Idle

- **AC-2**: 单选切换
  - Given: A.state == Selected
  - When: Tap 命中 B
  - Then: A.state == Idle（OnObjectDeselected(A.Id) 派发一次）；B.state == Selected（OnObjectSelected(B.Id) 派发一次）；`_selectedObject == B`
  - Edge: 3 次连续 Tap A→B→C（间隔 > 200ms）：最终仅 C 选中，A/B 派发各 1 次 deselect

- **AC-3**: 200ms debounce
  - Given: A 被选中，时间 = T
  - When: Tap B at T + 0.15s
  - Then: 整体忽略；A 保持 Selected；B 未被选中；listener 未收到任何派发
  - When: Tap B at T + 0.25s
  - Then: 正常切换 A→B
  - Edge: timeScale=0 时 `Time.unscaledTime` 仍正常推进

- **AC-4**: Fat finger 最小 hit area 44pt
  - Given: `fatFingerMargin = 8` (TbInputConfig)；`Screen.dpi = 326`
  - When: Tap 距 Object C 中心 < (colliderRadius + 8) 但 > colliderRadius
  - Then: C 被选中
  - Pass: 任意 DPI 下有效 hit 区 ≥ 44pt（在 dpi=460 等高 DPI 设备上 expandedRadius 按比例放大）

- **AC-5**: 10 object 性能
  - Given: chapter scene 10 个 InteractableObject；其中 1 个 Dragging
  - When: 60 帧连续渲染 on iPhone 13 Mini 等价硬件
  - Then: ≥ 55fps；本系统 Update ≤ 1.0ms profiled；Drag 响应 ≤ 16ms
  - Edge: 10 个同时 Snapping（DOTween 压力）：no frame rate cliff

- **AC-6**: OnInteractionLockChanged(true) 清 _selectedObject
  - Given: A 被选中
  - When: `IInteractionEvent.OnInteractionLockChanged(true)` 派发
  - Then: A.fsm 自己 → Locked（Story 001 转换 7）；Coordinator `_selectedObject = null`；OnObjectDeselected(A.Id) 派发一次（由 fsm.OnLockChanged 内部，**不**由 Coordinator 重复派发）

- **AC-7**: 多 sender OnObjectTransformChanged
  - Given: 3 个 object 同时 Snap 完成（DOTween OnComplete 同帧）
  - When: 3 次 fsm.OnSnapCompleted 触发 → Story 004 派 3 次 OnObjectTransformChanged
  - Then: listener 收到 3 次，objectId 各异；下游 Shadow Puzzle 收到 3 次重算

- **AC-8**: 协议合规 grep
  - When: `rg "EventId\.Evt_(TapGesture|DragGesture|RotateGesture|PuzzleLockAll|PuzzleUnlock|ObjectTransformChanged|ObjectSelected|ObjectDeselected)" Assets/GameScripts/HotFix/`
  - Then: 0 命中

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/Tests/EditMode/ObjectInteraction/InteractionCoordinatorTests.cs` — **EXISTS, 17 NUnit 全绿**
- `production/qa/grep-no-evt-objectinteraction-coordinator-2026-04-29.md` — 协议合规 grep 证据
- `production/qa/object-interaction-perf-evidence-<date>.md` — **DEFERRED**（PlayMode/device 测试，留 Polish 阶段）

**Status**: [x] Complete (EditMode 部分) + [ ] Deferred (PlayMode 性能 ADVISORY)

| 测试 | 覆盖 |
|---|---|
| `AC2_TrySelectObject_FromNullToA_*` | AC-2 首次选取 → fsm.OnTapHit + OnObjectSelected |
| `AC2_TrySelectObject_SwitchAToB_*` | AC-2 切换 → OnObjectDeselected(A) + OnObjectSelected(B) |
| `AC2_TrySelectObject_HitNull_*` | AC-2 命中空白 → A.OnDeselect + clear |
| `AC2_TrySelectObject_HitSameObject_NoOp` | AC-2 同对象 no-op，0 派发 |
| `AC2_TrySelectObject_HitNullWhenNoneSelected_NoOp` | AC-2 边界 |
| `AC3_TrySelectObject_WithinDebounce_RejectsSwitch` | AC-3 debounce 拒绝 |
| `AC3_TrySelectObject_AfterDebounceReset_AllowsSwitch` | AC-3 debounce 解除后切换 |
| `AC6_OnInteractionLockChanged_True_*` | AC-6 lock → CurrentSelectedObject=null + fsm.Locked + OnObjectDeselected 一次 |
| `AC6_OnInteractionLockChanged_False_NoEffect` | AC-6 unlock 不影响 |
| `Drag_OnDragBegan_*` | Drag 转发 Began → fsm.Dragging |
| `Drag_OnDragEnded_*` | Drag 转发 Ended → fsm.Snapping |
| `Drag_NoSelectedObject_Ignored` | Drag 无选中时忽略 |
| `Init_Idempotent_DoubleCallSafe` | lifecycle 防御 |
| `Shutdown_ClearsListenersAndState` | Shutdown 后不响应 OnDrag |
| `Shutdown_Idempotent_DoubleCallSafe` | 幂等 |
| `Protocol_TrySelectObject_DoesNotDispatchEventsItself_RelyOnFsm` | 协议合规：Coordinator 不自派 |
| Coordinator+InteractableObject 端到端协调（含 fsm 转换 + 事件派发计数） | 覆盖完整 |

**EditMode 不覆盖**：Raycast 物理命中 / fat finger 数学 / 10 object 性能 — 推 Polish PlayMode/device 测试（与 S2-09 EaseOutBack / S2-10 Ease+duration 同 ADVISORY 处理）。

---

## Dependencies

- Depends on: Stories 001–006（全部 **DONE**：S2-08/S2-09/S2-10/S2-12）— 这是把所有 piece 串起来的 integration story
- Pre-condition: ADR-013 = **Accepted**（✅ 2026-04-29）；`IInteractionEvent.cs` / `IGestureEvent` 已存在（✅）；`InteractionLockManager` 已存在（✅ S2-12）
- Unlocks: Shadow Puzzle epic（消费 `OnObjectTransformChanged` 多 sender）；Sprint 2 should-have 进度 2/3

---

## Completion Notes

**Completed**: 2026-04-29 night X3 patch（Sprint 2 should-have 2/3 ✅；解锁 Shadow Puzzle epic 多 sender 端到端测试场景）

### Acceptance Criteria 状态总览

| 类别 | 数量 | 备注 |
|---|---|---|
| EditMode 全部通过 | 13 / 13 | AC-2/AC-3/AC-6 + Drag 转发 + 协议合规 + lifecycle 防御 |
| ADVISORY (PlayMode 留) | 2 | 性能（10 obj ≥ 55fps + Update ≤ 1ms）+ 多 sender OnObjectTransformChanged 端到端 |

### 实施代码

- `Assets/GameScripts/HotFix/GameLogic/Input/IInputConfig.cs` — 加 `FatFingerMarginMm` 属性
- `Assets/GameScripts/HotFix/GameLogic/Input/InputConfigFromLuban.cs` — 加 `_fatFingerMarginMm` 字段 + InitWithDefaults 默认 8mm + InitFromLuban overload（fatFingerMarginMm 默认 8f，向下兼容旧 callsite）
- `Assets/Tests/EditMode/InputSystem/SingleFingerFSMTests.cs` — `TestInputConfig` 加 `FatFingerMarginMm`（接口扩展协同）
- `Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/InteractionCoordinator.cs` — new file（POCO MonoBehaviour，per-event listener，TrySelectObject + Raycast helper）
- `Assets/Tests/EditMode/ObjectInteraction/InteractionCoordinatorTests.cs` — new file（17 NUnit）

### 资料 / 工程文档同步

- `production/qa/grep-no-evt-objectinteraction-coordinator-2026-04-29.md` — new file
- `production/sprint-status.yaml` — S2-13 → done
- `production/session-state/active.md` — Session Extract 追加

### 已解决的 Deviations / X3 patches 历史

- **X3 patch v1**：实施 IInputConfig.FatFingerMarginMm + InputConfigFromLuban 扩展 + TestInputConfig 同步 + InteractionCoordinator + 17 NUnit + grep evidence
- **X3 patch v2**：§Engine Notes + §Implementation Notes + AC 修订（per-event listener 取代整接口订阅幻想 + 删 SetLockManager 注入路径，Coordinator 不实施 IGestureEvent/IInteractionEvent 接口本身）

### 留作后续 Story / Polish

- 性能验证：PlayMode device build / Profiler 帧时数据 / 10 object stress
- Raycast 物理命中 + fat finger 数学：PlayMode 集成测试（CollidersInScene + camera + dpi 矩阵）
- Luban TbInputConfig.FatFingerMarginMm 列接入：Luban schema 加列时回填，对接 `InitFromLuban` 新参数
