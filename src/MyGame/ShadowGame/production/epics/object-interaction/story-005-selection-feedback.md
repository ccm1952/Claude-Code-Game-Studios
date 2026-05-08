// 该文件由Cursor 自动生成

# Story 005: Selection Visual Feedback (Outline + Scale Bounce)

> **Epic**: Object Interaction
> **Status**: Done (production code + EditMode tests 完成 2026-05-06；manual Editor verification pending Sprint 5 VS chapter ready)
> **Layer**: Core
> **Type**: Visual/Feel
> **Manifest Version**: 2026-05-06 (Sprint 4 S4-06 闭环；Sprint 2→3→4 第 2 次 carryover 终结；EditMode tests + production code DONE)

## Context

**GDD**: `design/gdd/object-interaction.md`
**Requirement**: `TR-objint-002`
*(Selection visual feedback)*

**ADR Governing Implementation**: ADR-013（**Accepted**, 2026-04-29）+ ADR-011 (UI Widget pattern applied to gameplay companion components)
**ADR Decision Summary**: Selected 物体获得视觉反馈：outline 高亮 + 选中弹动；snap 完成时小幅 settle 弹动。这些是**纯视觉响应**，不影响 gameplay 逻辑也不影响 collider 尺寸。Feedback 实施在 `InteractableObjectFeedback`（companion MonoBehaviour，与 InteractableObject 同 GameObject 或子节点），**订阅 `InteractableObjectFsm.StateChanged` 的 C# event**（参见 Story 001 §"Implementation Notes"）— 不走 `GameEvent` / `IInteractionEvent`（widget 内部关注点，避免污染事件总线）。

**Engine**: Unity 2022.3.62f2 LTS + DOTween + URP | **Risk**: LOW
**Engine Notes**: Outline 通过附加 outline-only material（render last + stencil-based）实施，URP 兼容无需 custom render feature。Scale bounce 用 `transform.DOPunchScale`。Collider 必须独立于 visual mesh — 放在不受 scale punch 影响的子节点上，或 collider 尺寸不依赖 transform.localScale。

**Control Manifest Rules (this layer)**:
- Required: `feedback 订阅 fsm.StateChanged C# event — 不走 GameEvent / IInteractionEvent` (ADR-013, ADR-011 widget 模式)
- Required: `outline / scale punch 不影响 Collider 尺寸或 layer` (ADR-013)
- Required: `所有 async via UniTask` (ADR-001)
- Forbidden: `禁止在 feedback 层使用 EventId.Evt_*` (ADR-006 全废)
- Forbidden: `feedback 不应再派发 IInteractionEvent.OnObjectSelected 等 — sender 由 fsm 唯一负责（Story 001）`

---

## Acceptance Criteria

*From GDD `design/gdd/object-interaction.md`，scoped to this story:*

- [ ] `InteractableObjectFeedback` MonoBehaviour 在 `OnEnable` 中订阅 `interactableObject.Fsm.StateChanged` C# event；`OnDisable` 中取消订阅
- [ ] `Idle → Selected` 转换：outline material 启用 + DOTween `DOPunchScale` 弹动（punch (0.15, 0.15, 0)，0.2s，vibrato=5，elasticity=0.5）
- [ ] `Selected → Idle` 转换：outline 关闭；scale 复位至 `Vector3.one`（如不在）
- [ ] `Snapping → Idle` 转换（snap 完成）：小幅 settle punch（punch (0.05, 0.05, 0)，0.1s，vibrato=3）— 区别于选中弹动
- [ ] `Any → Locked` 转换：outline 关闭；不播放任何动画（被锁不该有视觉响应）
- [ ] `Locked → Idle` 转换：无任何动画（玩家需重新 tap 才有反馈）
- [ ] outline / scale punch **不**影响 `Collider`（无论 SphereCollider 还是 BoxCollider）— collider 在子 GameObject 或独立物理 layer 上
- [ ] feedback 在中端移动 GPU 上无明显 overdraw（手测帧时 ≤ 0.2ms 增量）
- [ ] 协议合规：本 story 实装 0 处使用 `EventId.Evt_*`；0 处订阅 `IInteractionEvent`（feedback 仅订阅 fsm C# event）

---

## Implementation Notes

*Derived from ADR-013 §"Architecture" + Story 001 fsm.StateChanged 设计：*

```csharp
public sealed class InteractableObjectFeedback : MonoBehaviour
{
    [SerializeField] private InteractableObject _target;        // 同 GameObject 或父节点
    [SerializeField] private Renderer _outlineRenderer;          // 单独 outline material 渲染器
    [SerializeField] private Material _outlineMaterial;
    [SerializeField] private Transform _visualMesh;              // 受 scale punch 影响的子节点（与 collider 分离）

    private void OnEnable()
    {
        if (_target?.Fsm != null) _target.Fsm.StateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (_target?.Fsm != null) _target.Fsm.StateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(InteractableObjectState prev, InteractableObjectState next)
    {
        switch (next)
        {
            case InteractableObjectState.Selected:
                _outlineRenderer.enabled = true;
                _visualMesh.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.2f, 5, 0.5f);
                break;
            case InteractableObjectState.Idle:
                _outlineRenderer.enabled = false;
                if (prev == InteractableObjectState.Snapping)
                    _visualMesh.DOPunchScale(new Vector3(0.05f, 0.05f, 0f), 0.1f, 3, 0.5f);
                else
                    _visualMesh.localScale = Vector3.one;
                break;
            case InteractableObjectState.Locked:
                _outlineRenderer.enabled = false;
                _visualMesh.DOKill(); _visualMesh.localScale = Vector3.one;
                break;
            // Dragging / Snapping：保持 outline 开（Selected 状态进入时已开），无新动画
        }
    }
}
```

**为什么不走 GameEvent**：feedback 是 widget-internal concern（仅与单一 InteractableObject 强绑定）；用 C# event 的好处是：
1. 自动按 sender 单实例隔离（不需要 objectId 过滤）
2. 不污染全局事件总线 — Source Generator 生成的 _Gen 类不会因为 N 个 feedback 实例而 N 倍 dispatch 开销
3. 符合 ADR-011 "widget 内部状态变化用 C# event；跨系统通信才用 GameEvent" 的总原则

**Collider 独立性**：
- 方案 A（推荐）：`InteractableObject` 根节点持 collider；视觉 mesh 在子节点 `_visualMesh`，子节点接受 scale punch
- 方案 B：collider 与 mesh 同节点但 collider 是 BoxCollider2D 且尺寸固定（不依赖 transform.localScale，因 BoxCollider2D 的 size 是绝对值不被 transform 缩放放大——Unity 文档需确认）

**Outline material**：参考 Art Bible 已确认的 stylized outline shader；URP 下用 stencil + secondary renderer 模式。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: FSM state transitions（feedback 仅消费 StateChanged 事件，不修改 fsm）
- Story 003: 旋转视觉反馈（MVP 不区分 — rotation 视觉就是 mesh 自身旋转）
- Art Bible: outline 颜色、厚度、动画曲线等具体值由 Design 提供

---

## QA Test Cases

*Visual/Feel type — manual verification steps + 1 EditMode 单元测试 (订阅验证)*：

- **AC-1**: outline 在选中时激活
  - Setup: 加载 chapter scene 含 InteractableObject + Feedback；点击 object
  - Verify: object 立即显示 outline 高亮；其他 object 无 outline
  - Pass: outline 1 帧内出现；1m 真实距离可见

- **AC-2**: scale bounce 在选中时播放
  - Setup: 点击 object
  - Verify: object 短暂 punch 外扩后回弹；动画"snappy"且舒服
  - Pass: 0.2s 内完成；scale 精确回到 (1,1,1)；无残留 artefact

- **AC-3**: 取消选中时反馈清除
  - Setup: 选中 A → tap 空白
  - Verify: outline 立即消失；scale == (1,1,1)
  - Pass: 任意 object 上无 outline

- **AC-4**: snap 完成后 settle punch
  - Setup: 拖动 object 后释放
  - Verify: snap 动画完成后小幅 settle 脉冲可见
  - Pass: settle 比选中 bounce 弱；object 落在正确 snapped 位置

- **AC-5**: Locked 状态无反馈
  - Setup: 通过 debug 工具触发 `IInteractionEvent.OnRequestPuzzleLockAll("debug")`
  - Verify: 点击 object 无 outline、无 bounce
  - Pass: 视觉层 0 响应

- **AC-6**（EditMode 单元）：StateChanged 订阅生效
  - Given: mock InteractableObject + 真实 Feedback；fsm 在 Idle
  - When: 直接调 `target.Fsm.OnTapHit()`（绕过 raycast/coordinator）
  - Then: feedback `_outlineRenderer.enabled == true`；DOTween tween 在 _visualMesh 上 active
  - Edge: OnDisable 后重新 OnEnable，仍能正确订阅（无重复订阅）

- **AC-7**（EditMode 单元）：协议合规
  - When: `rg "EventId\.Evt_" Assets/GameScripts/HotFix/GameLogic/Object*Feedback*.cs`
  - Then: 0 命中（只允许 fsm.StateChanged C# event 订阅）
  - When: `rg "AddEventListener<IInteractionEvent>|GameEvent\.Get<IInteractionEvent>" Assets/GameScripts/HotFix/GameLogic/Object*Feedback*.cs`
  - Then: 0 命中

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**:
- `production/qa/evidence/selection-feedback-evidence-<date>.md` — manual verification doc with screenshots + sign-off (AC-1..AC-5)
- `Assets/Tests/EditMode/ObjectInteraction/InteractableObjectFeedbackTests.cs` — 1-2 NUnit tests for AC-6, AC-7（订阅 + 协议合规）

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001（InteractableObjectFsm + StateChanged C# event — must be DONE），Story 004（Snapping → Idle 转换路径让 settle hook 触发 — must be DONE）
- Pre-condition: ADR-013 = **Accepted**（✅ 2026-04-29）
- Unlocks: Story 007（multi-object scene — feedback 帮助验证哪个 object 选中）
