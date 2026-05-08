// 该文件由Cursor 自动生成

# Story 004: Grid Snapping System

> **Epic**: Object Interaction
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Manifest Version**: 2026-04-29 night (S2-10 实施完成 — round/clamp + 已在格点跳过 + DOMove EaseOutQuad + OnComplete fsm.OnSnapCompleted + OnObjectTransformChanged + 中断 DOKill + IsReboundActive 协调；Initialize/Shutdown 显式生命周期延续 X1 patch v3 模式；fixture stub camera 注入修补 — 详见 `/.claude/memory/problem_2026-04-29_test-fixture-fail-loud-boilerplate-skip.md`)

## Context

**GDD**: `design/gdd/object-interaction.md`
**Requirement**: `TR-objint-005`, `TR-objint-006`
*(Grid snap formula; snap interpolation EaseOutQuad)*

**ADR Governing Implementation**: ADR-013（**Accepted**, 2026-04-29）+ ADR-027 (GameEvent Interface Protocol)
**ADR Decision Summary**: Grid snap 仅在 finger 释放时触发（drag 过程中 object 自由跟随手指）。公式：`snappedPos = round(rawPos / gridSize) * gridSize`。DOTween EaseOutQuad over `snapSpeed` 秒。`gridSize` 与 `snapSpeed` 来自 Luban `TbPuzzle`。Snap 完成时 fsm 调 `OnSnapCompleted()`（Story 001 转换规则 5）+ 派发 `IInteractionEvent.OnObjectTransformChanged(objectId, finalPos, rotation)`。

**Engine**: Unity 2022.3.62f2 LTS + DOTween | **Risk**: LOW
**Engine Notes**: Snap 在 InteractableObject.Update 中检测 `fsm.CurrentState == Snapping` 时启动一次性 DOTween（用 `_didStartSnap` flag 防重启）；OnComplete 回调里调 `fsm.OnSnapCompleted()` + 派发 `OnObjectTransformChanged`。Snap 中断（Snapping→Selected by 转换规则 6）时 `transform.DOKill(complete: false)`，object 停在中间帧位置。**Rebound 协调（S2-09 race-fallback）**：`if (Fsm.CurrentState == Snapping && !_didStartSnap && !IsReboundActive) StartSnap();` —— 当 S2-09 race-fallback rebound 正在进行时（`IsReboundActive == true`），grid snap 推迟启动；rebound `OnComplete` 在 `_isReboundActive = false` 后下一帧 Update，grid snap 自然接管（无需显式回调）。生产主路径 `IsReboundActive` 长期为 false，本协调对 release-in-bounds 的常规流程零开销。

**Control Manifest Rules (this layer)**:
- Required: `Grid snap on finger release only — during drag, object follows finger freely` (ADR-013)
- Required: `Snap formula: snappedPos = round(rawPos / gridSize) * gridSize — animated via DOTween EaseOutQuad` (ADR-013)
- Required: `所有参数从 Luban 配置读取（gridSize, snapSpeed from TbPuzzle）` (ADR-013, ADR-007)
- Required: `Snap OnComplete 派发 IInteractionEvent.OnObjectTransformChanged(objectId, finalPos, rot)` (ADR-013, ADR-027)
- Required: `Snap 中断时 transform.DOKill(complete: false) — object 停在中间帧` (ADR-013 转换规则 6)
- Forbidden: `禁止使用 EventId.Evt_ObjectTransformChanged 常量` (ADR-006 全废)
- Forbidden: `禁止 ObjectTransformChangedPayload struct — 参数直接传 (objectId, position, rotation)` (ADR-027)
- Guardrail: `Object Interaction total update (10 objects) ≤ 1.0ms/frame` (ADR-013)

---

## Acceptance Criteria

*From GDD `design/gdd/object-interaction.md` + ADR-013，scoped to this story:*

- [x] Grid snap 公式：`snappedX = Mathf.Round(rawX / gridSize) * gridSize`（Y 同理；2D 平面）
- [x] Snap 动画：DOTween 移动 object 从 currentPos 到 snappedPos，时长 `snapSpeed` 秒，EaseOutQuad —— **ADVISORY**：Ease 类型 + 精确 duration 在 EditMode 通过 `DOComplete` 强制走完后**等价验证**（最终位置正确即代表 tween 配置正确生效）；精确的 ease 曲线 + duration 时序留 PlayMode 集成验证（同 S2-09 EaseOutBack rebound 处理）
- [x] `gridSize` / `snapSpeed` 从 Luban `TbPuzzle` 读取，**不**硬编码 —— 通过 `PuzzleConfig` POCO + `PuzzleConfigProvider` 注入（Luban 表未生成期间的兼容路径，与 S2-09 共用）
- [x] Snap **仅**在 fsm.CurrentState 进入 `Snapping` 时启动；Drag 过程中**不**进行 snap 计算 —— `Update` 中按 `switch (Fsm.CurrentState)` 路由 + `_didStartSnap` flag 防重启
- [x] Snap OnComplete 回调：调 `fsm.OnSnapCompleted()`（state: Snapping → Idle）+ 派发 `IInteractionEvent.OnObjectTransformChanged`
- [x] **位置已在格点**（dist < 0.001 epsilon）：跳过 DOTween，立即调 OnComplete 路径
- [x] Snap 后置 clamp：snappedPos 计算后再 `Mathf.Clamp` 到 InteractionBounds（覆盖 raw=3.6 → snap=4.0 → clamp=3.0 等场景）
- [x] **中断处理**：fsm 在 Snapping 状态收到 `OnTapHit()` → `transform.DOKill(complete: false)`；object 停在中间帧；fsm → Selected；**不**派 OnObjectTransformChanged
- [x] **Lock 中断**：fsm 在 Snapping 状态收到 `OnLockChanged(true)` → DOKill；fsm → Locked；**不**派 OnObjectTransformChanged
- [x] **协议合规**：0 `EventId.Evt_ObjectTransformChanged` 残留；调用形如 `GameEvent.Get<IInteractionEvent>().OnObjectTransformChanged(...)` —— grep evidence: `production/qa/grep-no-evt-objectinteraction-snap-2026-04-29.md`

---

## Implementation Notes

*Derived from ADR-013 §"Grid Snap Mechanics" + Story 001 fsm 转换规则 5：*

```csharp
public sealed class InteractableObject : MonoBehaviour
{
    private bool _didStartSnap;
    // [...] (Story 001/002/003 部分)

    private void Update()
    {
        if (Fsm == null) return;
        switch (Fsm.CurrentState)
        {
            case InteractableObjectState.Dragging:
                TickDrag();   // Story 002: 1:1 跟踪 + 边界 clamp
                break;
            case InteractableObjectState.Snapping:
                if (IsReboundActive) break;   // S2-09 race-fallback rebound 优先；待其完成
                if (!_didStartSnap) StartSnap();
                break;
            default:
                _didStartSnap = false;
                break;
        }
    }

    private void StartSnap()
    {
        _didStartSnap = true;
        var pos = transform.position;
        float gs = _puzzleConfig.GridSize;
        float snappedX = Mathf.Round(pos.x / gs) * gs;
        float snappedY = Mathf.Round(pos.y / gs) * gs;

        var bounds = _puzzleConfig.InteractionBounds;
        snappedX = Mathf.Clamp(snappedX, bounds.MinX, bounds.MaxX);
        snappedY = Mathf.Clamp(snappedY, bounds.MinY, bounds.MaxY);

        var snappedPos = new Vector3(snappedX, snappedY, pos.z);

        if (Vector3.Distance(pos, snappedPos) < 0.001f) {
            OnSnapComplete();
            return;
        }
        transform.DOMove(snappedPos, _puzzleConfig.SnapSpeed)
            .SetEase(Ease.OutQuad)
            .OnComplete(OnSnapComplete);
    }

    private void OnSnapComplete()
    {
        Fsm.OnSnapCompleted();
        GameEvent.Get<IInteractionEvent>()
            .OnObjectTransformChanged(Fsm.ObjectId, transform.position, transform.rotation);
    }

    // Snap 中断由 fsm 转换路径触发 transform.DOKill(complete: false)
    // 通过订阅 fsm.StateChanged 在 Snapping → 其他 状态时立即 DOKill
}
```

**Snap 中断细节**：在 `OnEnable` 中订阅 `Fsm.StateChanged += (prev, next) => { if (prev == Snapping && next != Idle) transform.DOKill(complete: false); }`。next == Idle 表示自然完成（DOTween OnComplete 已触发）；其他 next（Selected/Locked）表示中断。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: Drag 过程的 1:1 跟踪 + 边界 clamp
- Story 003: 旋转 snap（同 snap-on-release 模式但作用于角度）
- Story 005: snap 完成后 settle 视觉反馈（订阅 fsm.StateChanged Snapping→Idle）

---

## QA Test Cases

*EditMode 单元测试 — 用 mock InteractableObject 或 PlayMode + GameObject + 时间步进：*

- **AC-1**: snap 公式正确
  - Given: gridSize = 1.0；object 位置 = (1.4, 2.7, 0)
  - When: 进入 Snapping 状态后第一帧 Update
  - Then: DOTween tween 启动，目标 = (1.0, 3.0, 0)
  - Edge: 负坐标 (-0.6, -1.4) → (-1.0, -1.0)；已在格点 (1.0, 2.0) → 跳过 tween，直接 OnComplete

- **AC-2**: 动画用 EaseOutQuad over snapSpeed
  - Given: snapSpeed = 0.2s from TbPuzzle
  - When: snap 启动
  - Then: tween duration == 0.2s；ease == EaseOutQuad
  - Edge: snapSpeed = 0 时不发生 div-by-zero — 走立即 snap 路径

- **AC-3**: OnObjectTransformChanged 在 OnComplete 后派发
  - Given: snap 自然完成 to (2.0, 1.0, 0)
  - When: DOTween OnComplete 触发
  - Then: fsm 状态 == Idle；listener 收到 `OnObjectTransformChanged(objectId, (2.0,1.0,0), <rotation 不变>)` 一次
  - Edge: 派发**仅**在 OnComplete 时；snap 启动时不派发

- **AC-4**: 已在格点跳过动画
  - Given: object 在 (1.0, 2.0, 0)；gridSize = 1.0
  - When: 进入 Snapping
  - Then: 0 个 DOTween tween 启动；fsm 直接 → Idle；OnObjectTransformChanged 仍派发一次
  - Edge: 浮点容差 (1.0001, 2.0, 0) 应也判定为已在格点 → 直接 snap 完成路径

- **AC-5**: snap 后置 clamp
  - Given: gridSize=1.0；MaxX=3.0；finger 释放 at X=3.4（理论 snap → 3.0；正好在 bounds 内）
  - When: snap 计算
  - Then: snappedX = 3.0
  - Edge: 如 raw=3.6 → snap=4.0 → clamp 到 3.0（取在 bounds 内的最近格点）

- **AC-6**: OnTapHit 中断 snap（转换规则 6）
  - Given: snap tween 进行中（state=Snapping）
  - When: fsm.OnTapHit()
  - Then: tween 被 DOKill(complete: false)；object position 停在中间帧（**不**到 snappedPos）；state == Selected；OnObjectTransformChanged **不**派发；OnObjectSelected 派发一次

- **AC-7**: Lock 中断 snap（转换规则 7）
  - Given: state == Snapping
  - When: fsm.OnLockChanged(true)
  - Then: tween DOKill；state == Locked；OnObjectTransformChanged **不**派发；OnObjectDeselected **不**派发（因 Snapping 不是 Selected）

- **AC-8**: 协议合规 grep
  - When: `rg "EventId\.Evt_ObjectTransformChanged" Assets/GameScripts/HotFix/`
  - Then: 0 命中

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `Assets/Tests/EditMode/ObjectInteraction/GridSnapTests.cs` — **EXISTS, 11 NUnit 全绿（用户 2026-04-29 night Run All 确认）**
- `production/qa/grep-no-evt-objectinteraction-snap-2026-04-29.md` — 协议合规 grep 证据（0 `EventId.Evt_ObjectTransformChanged` + 0 `ObjectTransformChangedPayload` + 0 EventId.Evt_* 在 ObjectInteraction/ 子目录）

**Status**: [x] Complete — 11 NUnit tests pass

| 测试 | 覆盖 AC |
|---|---|
| `AC1_StartSnap_OffGrid_DOCompletes_LandsOnNearestGridPoint` | AC-1（snap 公式 round + DOTween OnComplete 走完 + tween 启动） |
| `AC1_StartSnap_NegativeCoords_RoundsCorrectly` | AC-1（负坐标） |
| `AC3_OnSnapComplete_EmitsObjectTransformChanged_OnceWithFinalPos` | AC-3（派发计数 + 携带 final pos） |
| `AC3_StartSnap_DoesNotEmitEvent_BeforeOnComplete` | AC-3（tween 未 OnComplete 时**不**派事件 + fsm 仍 Snapping） |
| `AC4_StartSnap_AlreadyAtGrid_SkipsTween_SyncCompletes` | AC-4（已在格点 → 同步 OnComplete） |
| `AC4_StartSnap_FloatingPointTolerance_TreatedAsAtGrid` | AC-4（浮点容差 0.0001 < epsilon） |
| `AC5_StartSnap_OffsetBeyondBounds_ClampsAfterSnap` | AC-5（raw 3.6 → snap 4.0 → clamp 3.0） |
| `AC6_OnTapHit_DuringSnap_DOKillsTween_NoEventDispatched` | AC-6（fsm 转换规则 6 + DOKill + 0 事件） |
| `AC7_OnLockChanged_DuringSnap_DOKillsTween_NoEventDispatched` | AC-7（fsm 转换规则 7 + DOKill + 0 事件） |
| `AC8_IInteractionEvent_OnObjectTransformChanged_HasCorrectSignature` | AC-8（反射签名 + `[EventInterface(GroupLogic)]`） |
| `Coordination_IsReboundActive_DefersStartSnap_UntilFlagCleared` | 协调点（reflection 调 private Update 模拟 PlayerLoop 一帧） |

---

## Dependencies

- Depends on: Story 001（Snapping state + OnSnapCompleted/OnLockChanged 触发方法 — **DONE** S2-08），Story 002（InteractionBounds clamp 数据来源 — **DONE** S2-09）
- Pre-condition: ADR-013 = **Accepted**（✅ 2026-04-29）；`IInteractionEvent.cs` 已存在（✅ 2026-04-29）
- Unlocks: Story 005（settle 反馈订阅 Snapping→Idle 转换），Story 007（multi-object scene — snap 是验证最终位置的前提）

---

## Completion Notes

**Completed**: 2026-04-29 night（Track C must-have 3/3 闭环）

### Acceptance Criteria 状态总览

| 类别 | 数量 | 备注 |
|---|---|---|
| 全部通过 | 9 / 10 | AC-1, AC-3..AC-8, AC-10（协议合规） |
| ADVISORY | 1 / 10 | AC-2 ease 曲线 + 精确 duration 留 PlayMode；EditMode 用 `DOComplete` 强制走完等价验证最终位置 |
| 配置注入路径 | 通过 | `gridSize` / `snapSpeed` 来自 `PuzzleConfig` POCO（与 S2-09 共用 provider 注入；Luban `TbPuzzle` 表未生成期间的兼容路径） |

### 实施代码

- `Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/InteractableObject.cs` — modified（加 `_didStartSnap`、`Update` 路由 switch、`StartSnap()`、`OnSnapComplete()`、`OnFsmStateChanged` Snapping→非 Idle 中断分支、`Shutdown` 清 latch）
- `Assets/Tests/EditMode/ObjectInteraction/GridSnapTests.cs` — new file（11 NUnit + DOTween reflection helper）

### 资料/工程文档同步

- `production/qa/grep-no-evt-objectinteraction-snap-2026-04-29.md` — new file
- `production/sprint-status.yaml` — S2-10 → done
- `production/session-state/active.md` — Session Extract 追加
- `.claude/memory/problem_2026-04-29_csharp-string-literal-nested-quotes.md` — 新增（C# 字符串字面量嵌套引号沉淀）
- `.claude/memory/problem_2026-04-29_test-fixture-fail-loud-boilerplate-skip.md` — 新增（测试 fixture boilerplate 复用 + Unity NUnit 把 ERROR log 视为失败）
- `src/MyGame/ShadowGame/CLAUDE.md` — 编码红线第 8 条（C# 字符串字面量嵌套引号约束）
- `src/MyGame/ShadowGame/.claude/skills/tengine-dev/references/conventions.md` — 新加"测试 fixture 规范"章节（R1/R2/R3 + checklist）
- `~/.cursor/rules/string-literal-nested-quotes.mdc` — 跨工程通用 Cursor rule
- `~/.cursor/rules/problem-to-rule-promotion.mdc` — 跨工程元规则（Problem → Rule 沉淀协议）

### 已解决的 Deviations

- 无（实施严格按 §Implementation Notes 模板，0 越界）

### 留作 Polish / 后续 Story 处理

- AC-2 EaseOutQuad ease 曲线 + 精确 `snapSpeed` duration 时序的 PlayMode 验证（连同 S2-09 EaseOutBack 一并 Polish 阶段补 PlayMode 集成测试）
- 抽 `InteractableObjectFixtureBase` abstract class（触发阈值：Object Interaction epic 出现第 3 个测试 fixture，即 S2-11 / S2-12 / S2-13 任一加进来时）

### Patches 历史

- 2026-04-29 night X1 patch v1：实施 InteractableObject 5 段改动 + GridSnapTests 11 NUnit
- 2026-04-29 night X2：修复中文字符串字面量嵌套 ASCII 双引号 CS1003（GridSnapTests.cs:214）
- 2026-04-29 night X3：补 stub Camera 注入 — fixture fail-loud boilerplate 漏复用导致 11/11 全失败 → 修复后全绿
