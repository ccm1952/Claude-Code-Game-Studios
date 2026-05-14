// 该文件由Cursor 自动生成

# Story 005: Chapter 1 Scene Wiring — InteractionCoordinator GameObject + Object_01/02 InteractableObject MonoBehaviour 挂载

> **Epic**: VS Chapter 1
> **Story ID**: vs-chapter-1-005 (Sprint 6 emergent fix Track F NEW；S6-01 Phase 2.1 manual playtest NEEDS-WORK 派生 — S0-2 + S0-3 合并)
> **Sprint**: 6 (Track F NEW — chapter 1 production wiring emergent fix)
> **Story Type**: Asset / Integration (Unity scene structure + MonoBehaviour 挂载 + Inspector 拖入)
> **Complexity Points**: 1-1.5 (~1-1.5 hr 估时)
> **GDD Requirement**: `design/gdd/object-interaction.md` line 86 (Chapter 1 仅 2 物件 + 1 光源 design constraint — **无对应 TR-ID** per tr-registry.yaml 实证) + `design/concept/shadow-memory.md` line 106 (Chapter 1 关系弧线 "靠近" 2 物件 design constraint)。本 story 落地 Sprint 2 SP-013 InteractableObject + InteractionCoordinator MonoBehaviour Inspector 挂载 + Sprint 5 S5-01 scene 实体 wiring。**TR-ID 引用 verify (2026-05-14 night Session 33)**: 原 story line 10 引用 `TR-objint-002` (实际 registry "Selection visual feedback EaseOutBack scale animation") + `TR-objint-003` (实际 "Fat-finger compensation") **两个 TR-ID 都不对应** chapter 1 物件挂载场景 — chapter 1 仅 2 物件 + 1 光源 是 design constraint 不是 TR-ID 范畴；amend 已修正。**dp11 第 2 实战触发额外子项 (TR-ID semantic drift)** — Sprint 6 retro 议题 6 input data point。
> **ADR References**: **ADR-013 Object Interaction State Machine** (InteractableObject FSM + InteractionCoordinator + InteractionLockManager Sprint 2 done) + ADR-027 §3 IGestureEvent + §4 IInteractionEvent contract + ADR-029 V3.0.1 (R2 deficiency-flagged PASS path 候选；如 Object_01/02 layer 注册不齐则 deficiency-flag) + ADR-030 §VS Build commit
> **Status**: ✅ **Phase 1 readiness gate ✅ READY** (2026-05-14 night Session 33 morning continue — Phase 0 R2 verify 7/7 + Phase 1 readiness gate R1 ✅ + R2 ✅ + R3 N/A pass + 5 gap closure amend (TR-ID semantic drift fix + unresolved 'TBD verify' wording fix + perf budget note + §Test Evidence section NEW + §ADR-029 Verification section NEW)；ADR-029 verdict ✅ PASS；ready for Phase 2 implementation)
> **Created**: 2026-05-14 morning continue (Sprint 6 Session 32 — emergent fix Track F NEW second story)
> **Completed**: ""
> **Depends on**: story-004 input-pipeline-wiring (本 story InteractionCoordinator OnTap listener 需 IGestureEvent.OnTap fire 才有意义) + S5-01 ✅ (Chapter_01_Approach.unity scene 实体已构建 + Object_01_CoffeeMug + Object_02_Book Hierarchy 已存在但缺 InteractableObject MonoBehaviour) + Sprint 2 SP-013 ✅ (InteractableObject + InteractableObjectFsm + InteractableObjectFeedback + InteractionCoordinator + InteractionLockManager production code done + EditMode test pass)

---

## Context

**S6-01 Phase 2.1 manual playtest NEEDS-WORK 派生 emergent fix Track F 第 2 story (~1-1.5 hr scene wiring + Inspector 拖入)**：

S6-01 root cause analysis 5 处 wiring gap 中：
- **S0-2: Chapter_01_Approach.unity 未挂 InteractionCoordinator GameObject** —— Sprint 5 S5-01 scene build 时 wiring gap (S5-02 spike fire mock 简化路径绕过未 surface)；
- **S0-3: Object_01_CoffeeMug / Object_02_Book 缺 InteractableObject MonoBehaviour** —— Session 27 #2 unity-mcp 实测 componentTypes `[Transform, MeshFilter, BoxCollider, MeshRenderer]` (Object_01) / `[Transform, MeshFilter, CapsuleCollider, MeshRenderer]` (Object_02)；当时决策推迟到 Sprint 6 polish 但 Sprint 6 stories 未排此项。

本 story 合并 S0-2 + S0-3 — 同一 unity-mcp session 内一次性完成 chapter 1 scene wiring（InteractionCoordinator GameObject 加 + Inspector _objects 拖入 + Object_01/02 MonoBehaviour 挂 + layer 注册）；不另起 sub-story。

### Sprint 2 SP-013 InteractionCoordinator production code 现状 verify

`InteractionCoordinator.cs` (Sprint 2 SP-013 done — `Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/InteractionCoordinator.cs`)：
- 持有 `[SerializeField] List<InteractableObject> _objects`（Inspector 预填，禁 FindObjectsOfType）
- 持有 `[SerializeField] LayerMask _interactableLayer` (raycast 仅命中 Interactable layer)
- 持有 `[SerializeField] Camera _gameplayCamera` (ADR-012 禁 Camera.main fallback)
- `Initialize()` / `Shutdown()` 显式 lifecycle hooks（OnEnable / OnDisable 自动委托）
- 订阅 IGestureEvent.OnTap → Raycast 选取 + fat finger comp + 200ms debounce + 单选切换
- 订阅 IGestureEvent.OnDrag → Began/Ended 转发到 CurrentSelectedObject.fsm
- 订阅 IGestureEvent.OnRotate → 全部 phase 转发到 CurrentSelectedObject.HandleRotate

本 story 仅 wire scene + InteractableObject MonoBehaviour 挂载，**0 InteractionCoordinator production code 修改**。

---

## Goal Flow (T0 → T6)

```
T0  Editor open Chapter_01_Approach.unity → Hierarchy 现状: Main Camera / Directional Light / Interactables (Object_01_CoffeeMug + Object_02_Book) / NarrativeTriggers / Walls
T1  Hierarchy root level 加 InteractionCoordinator GameObject (空 GameObject + AddComponent InteractionCoordinator)
    → Inspector tag: Untagged (R2.4 verify — InteractionCoordinator tag 不存在于 ProjectSettings/TagManager.asset；本 story 不 add tag — root 节点 Untagged 即可)
    → Inspector layer: Default (Layer 0)
    → Inspector _gameplayCamera: drag Main Camera GameObject (NOT Camera.main fallback per ADR-012)
    → Inspector _interactableLayer: LayerMask 勾选 `InteractableObject` (Layer 8；R2.3 verify — 实际 layer name `InteractableObject` 非 `Interactable`；TagManager.asset:19 line 8 = `InteractableObject` 已注册不需 add)
T2  Object_01_CoffeeMug AddComponent InteractableObject MonoBehaviour
    → Inspector layer: **InteractableObject** (Layer 8；从当前 Layer 0 改为 8；per R2.3 实证)
    → Inspector tag: Interactable (R2.4 实证 — 当前已是 `Interactable` Tag；不改)
    → Inspector _objectId: 1
    → Inspector _puzzleId: 1 (单 puzzle 配合 chapter 1)
    → Inspector _dragDepth: 10 (R2.1 实证 default 10f；可保持不改)
    → Inspector _gameplayCamera: drag Main Camera GameObject (必填；未配 InteractableObject.Initialize 内 Log.Error fail-loud per InteractableObject.cs:154-157)
    → AddComponent **BoxCollider2D** (Drift D fix per [A] 决策 — Sprint 2 SP-013 InteractionCoordinator.RaycastWithFatFinger 用 Physics2D.OverlapCircleAll；3D BoxCollider 保留给 ShadowMatch ADR-012 算法；2 套 collider 同存解 0 production C# change)
T3  Object_02_Book AddComponent InteractableObject MonoBehaviour (重复 T2 流程, _objectId: 2, _puzzleId: 1)
    → AddComponent **CircleCollider2D** (Drift D fix per [A] 决策；CapsuleCollider 3D 不能被 Physics2D 命中；CircleCollider2D 简化命中区；CapsuleCollider 3D 保留)
T4  InteractionCoordinator Inspector _objects: 拖入 [Object_01_CoffeeMug, Object_02_Book] (List<InteractableObject> 顺序无关，Count=2)
T5  Save scene → unity-mcp manage_scene save → git diff verify Chapter_01_Approach.unity 改动符合预期 + 0 production C# change（GameApp.cs / InteractionCoordinator.cs 不改）
T6  Evidence: unity-mcp manage_scene get_hierarchy parent=root verify InteractionCoordinator GameObject 存在 + componentTypes 含 InteractionCoordinator + parent=Interactables verify Object_01/02 componentTypes 含 InteractableObject + BoxCollider2D/CircleCollider2D
```

---

## ADR Decision Summary

**ADR-013 Object Interaction State Machine inherit** — 本 story 不 amend ADR：
- InteractionCoordinator (Sprint 2 SP-013 done) 持有 List<InteractableObject> + LayerMask + Camera (Inspector 预填，禁 runtime FindObjectsOfType)
- InteractableObject MonoBehaviour (Sprint 2 SP-013 done) 含 InteractableObjectFsm (POCO) + 4 state (Idle/Selected/Dragging/Locked) + PuzzleConfigProvider 静态注入

**ADR-027 §4 IInteractionEvent contract inherit** — 9 method 已冻结 (Sprint 2 SP-013 done)：
- `OnObjectSelected(int objectId)` / `OnObjectDeselected(int objectId)` / `OnObjectTransformChanged(int objectId, Vector3 pos, float rot)` 等
- 本 story 仅 wire scene；OnObjectTransformChanged sender 由 InteractableObject.SnapRotation Sprint 2 done

**ADR-029 V3.0.1 R2 verify 实证 4 drift verdict (Phase 0 2026-05-14 afternoon)**：
- ✅ **Drift A** (Type-11 dp11 第 2 实战) — InteractableObject Inspector 字段 spec drift：实际 4 项 `_objectId/_puzzleId/_dragDepth/_gameplayCamera`；spec line 57 误列 `_shadowCasterGroup/_renderer` 不存在；漏列 `_dragDepth/_gameplayCamera` (必填，未配 Log.Error)。**story-005 已 amend** §Goal Flow T2/T3 + §Acceptance Criteria 修正。
- ✅ **Drift B** (Type-11 dp11 第 2 实战) — Layer 注册 drift：实际 **Layer 8 = `InteractableObject`** 已注册 (ProjectSettings/TagManager.asset:19)；spec line 52 写 `Layer 9 'Interactable'` index+name 错。**好消息：layer 已注册不需 add** → AC-8 自动满足。
- ✅ **Drift C** (Type-11 dp11 第 2 实战) — Tag policy drift：`InteractionCoordinator` tag 不存在；3 个自定义 tag = `FixedLight/Interactable/NarrativeTrigger`；Object_01/02 当前已 tag `Interactable`。**story-005 已 amend** Coordinator 用 Untagged。
- ✅ **Drift D** (Type-? dp18 candidate NEW) — **重大架构 dimensional mismatch**：Sprint 2 SP-013 `InteractionCoordinator.RaycastWithFatFinger` 用 `Physics2D.OverlapCircleAll` (2D API)；chapter 1 scene Object_01 `BoxCollider` + Object_02 `CapsuleCollider` 都是 **3D collider**；5 sprint 累积 architectural drift 未被任何 R2/R3/manual playtest 揭穿 (Sprint 2 EditMode test 用 Physics2D mock fixture 自洽；Sprint 5 S5-01 scene build 用 3D primitive；dp15 sniff sub-clause 验"production caller > 0"但不验"dimensional consistency")。**user [A] 决策 narrow scope 加 2D collider 同存解** (BoxCollider2D + CircleCollider2D；3D collider 保留给 ShadowMatch 可能需要)；0 production C# change；**dp18 candidate NEW** 沉淀 V3.0.1 watch list。

---

## Engine Notes

**Phase 0 R2 vendor reality verify ✅ DONE (2026-05-14 afternoon Session 32; ~30 min)**：
- R2.1 ✅ InteractableObject Inspector 字段实证（`InteractableObject.cs:44-56`） — 4 项: `_objectId` (int, -1) + `_puzzleId` (int, -1) + `_dragDepth` (float, 10f) + `_gameplayCamera` (Camera, ADR-012 禁 Camera.main fallback)。**Drift A 实证** spec line 57 误列 `_shadowCasterGroup/_renderer`不存在 + 漏列 `_dragDepth/_gameplayCamera`。
- R2.2 ✅ InteractionCoordinator Inspector 字段实证（`InteractionCoordinator.cs:48-55`） — 3 项: `_objects` (List<InteractableObject>) + `_interactableLayer` (LayerMask) + `_gameplayCamera` (Camera)。FULLY MATCH spec。
- R2.3 ✅ Project Settings Layer 实证（`ProjectSettings/TagManager.asset:19`） — **Layer 8 = `InteractableObject` 已注册**。**Drift B 实证** spec line 52 写 Layer 9 `Interactable` index+name 错；好消息: 不需 add，AC-8 自动满足。
- R2.4 ✅ Project Settings Tag 实证（`ProjectSettings/TagManager.asset:7-9`） — 3 个自定义 Tag = `FixedLight/Interactable/NarrativeTrigger`。**Drift C 实证** `InteractionCoordinator` tag 不存在；本 story Coordinator 用 Untagged；Object_01/02 当前 Tag 已是 `Interactable`不改。
- R2.5 ✅ PuzzleConfig POCO 实证 — **2 个 class 命名易混**: `GameLogic.PuzzleConfig` (`ObjectInteraction/PuzzleConfig.cs`; 5 fields: Id/InteractionBounds/GridSize/SnapSpeed/RotationStep — InteractableObject 用) + `GameLogic.PuzzleStateConfig` (`ShadowPuzzle/PuzzleConfig.cs`; 7 fields - PuzzleStateMachine ADR-014 用)。本 story 不消费 PuzzleConfig (provider 由 story-006 注入)，仅 wire Inspector；如 OnEnable 触发时 provider 未注 → InteractableObject.cs:496 Log.Error fail-loud but 不抛。
- R2.6 ✅ unity-mcp tool path 实证 — `manage_gameobject` (create) + `manage_components` (add_component) + `manage_scene` (save/get_hierarchy)；S5-01 batch_execute precedent 可复用。
- R2.7 ✅ Object_01/02 Collider 实证（`Chapter_01_Approach.unity` line 359/734/797/422） — Object_01_CoffeeMug 含 **`BoxCollider` (3D, !u!65)**；Object_02_Book 含 **`CapsuleCollider` (3D, !u!136)**。**Drift D 实证** Sprint 2 SP-013 `InteractionCoordinator.cs:298` 用 `Physics2D.OverlapCircleAll` (2D API) ≠ scene 3D collider 维度不一致；per [A] 决策本 story 加 2D collider 同存解。

**Performance budget**: no perf impact expected — scene structure only, runtime impact 0 (Chapter_01_Approach.unity 加 1 个 InteractionCoordinator GameObject + 2 个 MonoBehaviour 挂载 + 2 个 2D collider；scene loaded once at chapter boot via ADR-009 11-step transition；InteractableObject MonoBehaviour Update Tick 本 story 不调 (depends on PuzzleConfigProvider 注入 — story-006 范畴)；InteractionCoordinator Initialize/OnEnable 内 listener subscribe + InteractionLockManager init 一次性 ~0.1 ms < scene load 总预算)；R3 spike `S6-XX_ChapterSceneWiring.cs` < 5s budget per S6-13 precedent (5 case PASS + 27/27 asserts + 391ms ≪ 5s 实战参考)。

---

## Control Manifest Rule References

**Phase 0 R2 verify ✅ DONE — Phase 1 R1 grep audit pending /story-readiness gate**：
- ✅ Required — InteractionCoordinator 必 Inspector 注入 `_objects` List；禁 runtime `FindObjectsOfType`（`InteractionCoordinator.cs:48` `[SerializeField] private List<InteractableObject> _objects`；line 309 `if (!_objects.Contains(io)) continue` 仅 Inspector 预填列表内）
- ✅ Required — `_gameplayCamera` 必 Inspector 注入；禁 Camera.main fallback（`InteractionCoordinator.cs:54-55` + `Initialize()` line 126-129 fail-loud Log.Error per ADR-012）
- ✅ Required — Object_01/02 layer 改为 `InteractableObject` (Layer 8)；当前 Layer 0 → 改 8（per R2.3 实证；InteractionCoordinator.RaycastWithFatFinger line 298 用 `_interactableLayer` LayerMask filter）
- ✅ Required — InteractableObject `_objectId` + `_puzzleId` 必 Inspector 注入（default = -1；未注入运行时 PuzzleConfigProvider 解析失败 fail-loud per `InteractableObject.cs:494-507`）
- ✅ Required — `_dragDepth` Inspector 注入 default 10f 可保持（drag 期 ScreenToWorldPoint 的 Z 分量；2D 项目通常等于 camera 到物体 plane 距离）

---

## Acceptance Criteria

| # | AC | Verify path |
|---|-----|------|
| AC-1 | Chapter_01_Approach.unity Hierarchy root level 加 InteractionCoordinator GameObject | unity-mcp manage_scene get_hierarchy parent=root |
| AC-2 | InteractionCoordinator Inspector _objects List = [Object_01_CoffeeMug, Object_02_Book] (顺序无关，Count=2) | unity-mcp manage_components get_serialized_field _objects |
| AC-3 | InteractionCoordinator Inspector _gameplayCamera = Main Camera GameObject (非 Camera.main fallback；属性 reference 可获取) | unity-mcp manage_components get_serialized_field _gameplayCamera |
| AC-4 | InteractionCoordinator Inspector _interactableLayer LayerMask 勾 `InteractableObject` (Layer 8；per R2.3 实证 — 非 spec 原写 'Interactable layer/Layer 9') | unity-mcp manage_components get_serialized_field _interactableLayer |
| AC-5 | Object_01_CoffeeMug AddComponent InteractableObject MonoBehaviour + Inspector _objectId=1 + _puzzleId=1 + _dragDepth=10 + _gameplayCamera=Main Camera (R2.1 Drift A fix — _dragDepth/_gameplayCamera 必填) | unity-mcp manage_scene get_hierarchy parent=Interactables → componentTypes 含 InteractableObject + manage_components get_serialized_field 4 项 |
| AC-6 | Object_02_Book AddComponent InteractableObject MonoBehaviour + Inspector _objectId=2 + _puzzleId=1 + _dragDepth=10 + _gameplayCamera=Main Camera | 同 AC-5 |
| AC-7 | Object_01_CoffeeMug + Object_02_Book layer 字段 = `InteractableObject` (Layer 8 per R2.3 实证；从当前 Layer 0 改为 8 — 非 spec 原写 'Layer 9 Interactable') | unity-mcp manage_scene get_hierarchy + GameObject layer field verify |
| AC-8 | Project Settings → Tags and Layers → `InteractableObject` layer ✅ 已注册 (Layer 8 per TagManager.asset:19；本 story 不需 add；自动满足) | Project Settings inspect (R2.3 evidence) |
| AC-9 | 0 production C# change — GameApp.cs / InteractionCoordinator.cs / InteractableObject.cs 等 0 modify (本 story 仅 scene + Inspector + 不改 Project Settings — Layer 已注册不动) | git diff --stat verify 0 *.cs modify + 0 ProjectSettings/*.asset modify |
| AC-10 | save scene + git commit；scene 文件大小变化合理 (增加 ~4-6 KB — InteractionCoordinator GameObject + 2 InteractableObject MonoBehaviour serialize data + 2 个新加 2D collider serialize data) | git diff Chapter_01_Approach.unity verify |
| **AC-11** | **Object_01_CoffeeMug 加 `BoxCollider2D`** + **Object_02_Book 加 `CircleCollider2D`** (Drift D fix per [A] 决策 — Sprint 2 SP-013 InteractionCoordinator.RaycastWithFatFinger 用 Physics2D.OverlapCircleAll；2D collider 让 raycast 命中；3D BoxCollider/CapsuleCollider 保留给 ShadowMatch ADR-012 算法可能需要) | unity-mcp manage_scene get_hierarchy parent=Interactables → componentTypes 含 BoxCollider2D (Object_01) + CircleCollider2D (Object_02) + 原 3D collider 保留 |

---

## R3 PlayMode Probe Plan（Phase 0 R2 verify 后 amend；/story-readiness gate 进一步精化）

预计 spike `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-XX_ChapterSceneWiring.cs` (~400-500 行 1 file + 3 inner class S6XXSpike : IDevSpike + S6XXRuntime + S6XXTester per S6-13 precedent)：

- **P1 SceneHierarchyHasInteractionCoordinator** — Editor PlayMode load Chapter_01_Approach.unity → expect `Object.FindObjectOfType<InteractionCoordinator>() != null` + `_objects.Count == 2` (reflection 读 private SerializeField)
- **P2 InteractableObjectsExistAndConfigured** — expect `Object.FindObjectsOfType<InteractableObject>().Length == 2` + `_objectId ∈ {1, 2}` + `_puzzleId == 1` + `_dragDepth == 10f` + `_gameplayCamera != null` (Drift A fix 4 字段全 verify)
- **P3 LayerFilterCorrect** — expect Object_01/02 `gameObject.layer == LayerMask.NameToLayer("InteractableObject")` (Layer 8 per R2.3) + `Coordinator._interactableLayer.value & (1 << 8) != 0` (mask 含 Layer 8) + **NEW**: Object_01 含 BoxCollider2D + Object_02 含 CircleCollider2D (Drift D AC-11 verify)
- **P4 CameraReferenceNonNull** — expect `Coordinator._gameplayCamera != null` + camera 是 chapter 1 scene Main Camera (`gameObject.name == "MainCamera"`；不 Camera.main fallback) + 每 InteractableObject._gameplayCamera 同 verify
- **P5 RaycastFatFingerDimensionalConsistency** — call `Coordinator.RaycastWithFatFinger(Vector2.zero)` 后 expect 0 unexpected error (verify Physics2D ↔ 2D collider 维度对齐 — Drift D 修复后 raycast 不抛异常；命中与否取决于 camera screen ↔ world 转换 + Object 实际 position，所以 P5 仅 verify 无异常 + dimensional consistency；具体 hit assert 留 story-008 final pilot)
- **P5b (optional)** InteractionCoordinatorInitializeIdempotent — call `Initialize()` 2 次 + expect 0 unexpected error + `IsLocked == false`

---

## R2 Assumptions Validated（Phase 0 ✅ DONE 2026-05-14 afternoon Session 32）

| # | Assumption | Verify | Status |
|---|------------|--------|--------|
| R2.1 | InteractableObject Inspector 字段完整 list | InteractableObject.cs:44-56 4 项 `_objectId/_puzzleId/_dragDepth/_gameplayCamera` | ✅ **Drift A 实证** spec line 57 误列 `_shadowCasterGroup/_renderer`不存在 + 漏列 `_dragDepth/_gameplayCamera` |
| R2.2 | InteractionCoordinator Inspector 字段完整 list | InteractionCoordinator.cs:48-55 3 项 `_objects/_interactableLayer/_gameplayCamera` | ✅ FULLY MATCH spec |
| R2.3 | Project Settings Interactable layer 注册状态 | ProjectSettings/TagManager.asset:19 Layer 8 = `InteractableObject` 已注册 | ✅ **Drift B 实证** spec line 52 Layer 9 'Interactable' index+name 错；好消息：不需 add，AC-8 自动满足 |
| R2.4 | Project Settings 自定义 tag 状态 | TagManager.asset:7-9 = `FixedLight/Interactable/NarrativeTrigger` 3 tag | ✅ **Drift C 实证** `InteractionCoordinator` tag 不存在；本 story Coordinator 用 Untagged；Object_01/02 当前 Tag 已是 `Interactable` 不改 |
| R2.5 | PuzzleConfig POCO 结构（与 story-006 联动） | 2 class 命名易混: `GameLogic.PuzzleConfig` (ObjectInteraction/, 5 fields) 与 `GameLogic.PuzzleStateConfig` (ShadowPuzzle/, 7 fields) | ✅ 实证 — 本 story 不消费 PuzzleConfig (provider 由 story-006 注入)；OnEnable 触发若 provider 未注 → Log.Error fail-loud (InteractableObject.cs:496) |
| R2.6 | unity-mcp manage_scene add GameObject + Component path | `manage_gameobject create` + `manage_components add_component` + `manage_scene save/get_hierarchy` | ✅ 路径完整可用 S5-01 batch_execute precedent 复用 |
| R2.7 | Object_01/02 Collider 配 raycast filter | Chapter_01_Approach.unity:797 Object_01 `BoxCollider` 3D + :422 Object_02 `CapsuleCollider` 3D；InteractionCoordinator.cs:298 用 `Physics2D.OverlapCircleAll` 2D | ✅ **Drift D 实证** 重大架构 dimensional mismatch；per [A] 决策 narrow scope 加 BoxCollider2D + CircleCollider2D 同存解 (AC-11 NEW)；3D collider 保留给 ShadowMatch ADR-012 |

---

## V3.0.1 Watch List Hooks

**Type-11 V3.0.1 dp15 candidate "EditMode green ≠ production wired sniff"** — 本 story 是 dp15 候选 sniff sub-clause **第 2 个 production wiring 修复 case**（chapter 1 scene 内 InteractableObject + InteractionCoordinator MonoBehaviour 挂载从 0 → 1+1+2）；与 story-004 input pipeline + story-006 provider injection + story-007 ShadowMatch wire + story-008 final pilot 共同 surface dp15 governance 全貌。

**Type-11 V3.0.1 dp11 candidate "sprint backlog placeholder wording drift" — 第 2 个实战触发 NEW (Phase 0 R2 verify 2026-05-14 afternoon)**：Phase 0 R2 verify 揭露 story-005 spec 3 处 wording drift (Drift A + B + C)：(A) InteractableObject Inspector 字段 spec line 57 误列 `_shadowCasterGroup/_renderer` 不存在 + 漏列 `_dragDepth/_gameplayCamera` (B) Layer 注册 spec line 52 写 Layer 9 'Interactable' 实际 Layer 8 'InteractableObject' (C) Tag policy spec line 49 'InteractionCoordinator' tag 不存在。与 dp11 第 1 个实战触发 (story-004 Sprint 2 SP-013 wording drift) 同根。**dp11 累计 2 个实战触发** → Sprint 6 retro 议题 6 评估 promote V3.x sub-version；建议 ADR-029 V2.0 §V2-1 R2 协议补条 "sprint backlog 内任何 spec 引述 (Sprint X ID / file path / ADR section / Layer index / Tag name 等) 必 R2 grep verify source-of-truth 后才能 write"。

**Type-? V3.0.1 dp18 candidate NEW "physics API dimensional mismatch (2D/3D collider drift)" — 第 1 个实战触发 (Phase 0 R2 verify 2026-05-14 afternoon)**：Sprint 2 SP-013 `InteractionCoordinator.RaycastWithFatFinger` 用 `Physics2D.OverlapCircleAll` (2D API)；chapter 1 scene Object_01 BoxCollider (3D) + Object_02 CapsuleCollider (3D)；**5 sprint 累积 architectural drift 未被任何 R2/R3/manual playtest 揭穿**：Sprint 2 EditMode test 用 Physics2D mock fixture 自洽；Sprint 5 S5-01 scene build 用 3D primitive；S6-13 R3 InputPipelineWiring 验 IGestureEvent.OnTap fire 但不验 collider 命中；S6-13 dp15 sniff sub-clause 验 "production caller hit > 0" 但不验 "dimensional consistency"。**dp18 与 dp15 关联**：dp15 验 wiring 存在；dp18 验 wiring 路径上各层维度一致 — 同根 architectural integrity 议题不同 sub-clause。本 story per [A] 决策 narrow scope 加 2D collider 同存解 (0 production C# change)；如 story-007 ShadowMatch wire 时再现同根 (ADR-012 算法 vs 3D collider 假设) → dp18 promote V3.x sub-version 优先级 升 + ADR-029 V3 R3 standard amend "dimensional consistency sniff sub-clause"；ADR-013 §Architecture + ADR-012 §Algorithm 评估 2D vs 3D physics 边界明示加 (V3.1 trigger 候选)。Sprint 7+ 影响：适用于全部 cross-sender→listener 链路含 physics 维度的系统。

**Type-2 (b) V3 candidate "Asset wiring drift"** — 留观察 — 如 Sprint 5 S5-01 scene build 阶段 R2 grep verify InteractableObject MonoBehaviour 是否本应该挂（spec 隐含 contract）但 S5-01 evidence doc 未提及，dp 候选记录。

---

## Out of Scope（明示）

- ❌ **art asset 升级 (placeholder Cube primitive → 概念美术 asset)** — S6-09 separate Should story (per [A2] decision: replay 后再评估)
- ❌ **InteractableObject FSM behavior** — Sprint 2 SP-013 done (本 story 仅挂载 MonoBehaviour，不改 FSM 行为)
- ❌ **InteractionLockManager 实例化** — Sprint 2 SP-013 InteractionCoordinator OnEnable 内自动 new + Init()，本 story 不需新建
- ❌ **Camera.main fallback removal audit** —— ADR-012 已禁；本 story 仅确保 _gameplayCamera Inspector 注入
- ❌ **PuzzleConfig provider 注入** — story-006 范畴
- ❌ **Object_01/02 ShadowCasterGroup wiring** — story-007 ShadowMatch 范畴

---

## Implementation Notes（Phase 0 R2 verify 后精化 — 2026-05-14 afternoon）

预计涉及文件：
1. **Assets/AssetRaw/Scenes/Chapter_01_Approach.unity** — 加 InteractionCoordinator GameObject (root level) + Inspector wire 3 字段 + Object_01_CoffeeMug AddComponent InteractableObject + BoxCollider2D + 4 字段 Inspector wire + Object_02_Book AddComponent InteractableObject + CircleCollider2D + 4 字段 Inspector wire + Object_01/02 layer 从 0 → 8 + InteractionCoordinator._objects 拖入 [Object_01, Object_02]
2. ❌ **ProjectSettings/TagManager.asset** NO CHANGE — Layer 8 `InteractableObject` 已注册 (R2.3 实证)；不需 add layer / tag
3. **Spike `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-XX_ChapterSceneWiring.cs`** NEW ~400-500 行 1 file + 3 inner class (S6XXSpike : IDevSpike + S6XXRuntime + S6XXTester) per S6-13 precedent — 5 R3 PlayMode probe case 验 scene wiring + Inspector field 注入 + 2D collider 维度一致
4. **`GameApp.cs`** RegisterDevSpikes 切换 S613Spike → S6XXSpike (~3 行 amend) — 或者保留 S613Spike，本 story 时通过手动 dev menu 跑 (待 Phase 2 实施时决定)
5. **`DevTestState.cs`** `[main-menu]` mode HasSpike list +1 (~1 行 amend；V3.0.1 dp8 candidate 阈值远超达 6 → 7；Sprint 6 retro 议题 1 强制评估 V3.1 trigger DevTestState central mode-dispatch refactor)

**0 production C# change 决策维持**：本 story 不改 Sprint 2 SP-013 / ADR-013 production code (InteractableObject.cs / InteractionCoordinator.cs)；仅 scene 改动 + spike 新建 + GameApp/DevTestState 注册切换 (5 行级 trivia amend 不算 production logic change)。

**Drift D narrow scope fix 实施细节**：
- Object_01 `BoxCollider2D` 加 — size 大小可参考 BoxCollider 3D 同 dimension (实测 Phase 2 时 ParseScale 微调)；本 story Inspector default 1×1 即可
- Object_02 `CircleCollider2D` 加 — radius default 0.5 (CapsuleCollider 3D 的 height/2 同量级，2D 用圆简化命中区即可；Phase 2 实施时 manual tune)
- 不改 3D collider — ShadowMatch ADR-012 算法 R2.1 verify 在 story-007 完成，到时确认 3D collider 是否真用；如不用则后续清理
- 不动 InteractionCoordinator.cs Physics2D → 3D — 留 dp18 candidate Sprint 6 retro 决策；narrow scope 优先 unblock fun loop

---

## Test Evidence

**Story Type**: Asset / Integration → Evidence doc 路径 (per S6-04 / S6-08 / S6-13 precedent)：

- **Evidence doc**: `production/qa/playmode-chapter-1-scene-wiring-2026-05-15.md` ~400-500 行 8 sections (§0 概要 + §1 R3 5+1 case detail + §2 R2 7/7 closure 表 + §3 AC 11/11 verify + §4 V3.0.1 Watch List Hooks dp11 第 2 + dp18 NEW + §5 Sprint 6 Track F insight + §6 Files changed + §7 References + §8 Verdict)
- **R3 spike JSON dump**: `~/Library/Application Support/DefaultCompany/Unity/S6-XX_Result.json` (per S6-13 precedent — write via `S6XXTester.WriteResultJson()`)
- **Unity scene diff**: `git diff Chapter_01_Approach.unity` (verify scene 改动符合 §Goal Flow T0-T6 + AC-1~AC-11 expectation)
- **0 production C# diff**: `git diff --stat *.cs` (verify 0 *.cs modify per AC-9；GameApp/DevTestState 注册切换 5 行级 trivia 例外)
- **0 ProjectSettings diff**: `git diff --stat ProjectSettings/*.asset` (verify Layer 8 InteractableObject 已注册不需 add per AC-8 / R2.3 实证)

---

## ADR-029 Verification

**Phase 1 R1+R2+R3 readiness gate verdict (Session 33 morning 2026-05-14)**：

- **R1 ✅ PASS** — per-event listener mode forbidden pattern grep audit:
  - `rg "AddEventListener<I\w+Event>\(this\)" production/epics/vs-chapter-1/story-005-chapter-1-scene-wiring.md` → 0 hits ✅
  - `rg "class \w+\s*:\s*\w+,\s*I\w+Event" production/epics/vs-chapter-1/story-005-chapter-1-scene-wiring.md` → 0 hits ✅
  - 本 story Asset/Integration 类型不含 listener code；Sprint 2 SP-013 production listener (InteractionCoordinator.Initialize → AddEventListener<GestureData>(IGestureEvent_Event.OnTap, OnTap) 等) 已 per-event 模式 + InteractableObject.Initialize 同模式；R3 spike listener spy 沿 S6-13 precedent per-event 模式
- **R2 ✅ PASS** — cross-component API existence grep verify (Phase 0 R2.1-R2.7 全 line-ref 实证 2026-05-14 afternoon):
  - InteractableObject.cs:44-56 SerializeField 4 项 ✅ + line 154-157 fail-loud ✅ + line 494-507 PuzzleConfigProvider resolve ✅
  - InteractionCoordinator.cs:48-55 SerializeField 3 项 ✅ + line 126-129 fail-loud ✅ + line 298 Physics2D.OverlapCircleAll ✅ + line 309 _objects.Contains filter ✅
  - PuzzleConfig.cs:24-49 (ObjectInteraction/) 5 fields ✅ + PuzzleStateConfig.cs (ShadowPuzzle/) 7 fields ✅
  - TagManager.asset:19 Layer 8 = InteractableObject ✅ + line 7-9 Tag 3 项 ✅
  - Drift A/B/C 已 inline closed via amend (story §Goal Flow / §Engine Notes / §AC)；Drift D inline narrow scope fix per AC-11 (BoxCollider2D + CircleCollider2D 同存解)；**ADR-013 §Architecture Physics2D vs 3D 边界 spec gap 留 dp18 candidate sub-version promote 决策**，不算本 story deficiency (narrow scope inline close 已 unblock fun loop)
- **R3 ✅ N/A auto-pass** — stub data type construction signature verify:
  - 本 story Asset/Integration 类型不构造任何 stub data type (PuzzleConfig / GestureData 等)；R3 spike `S6-XX_ChapterSceneWiring.cs` 是 NEW file Phase 2 才写，目前 story 内仅 conceptual outline
  - Phase 2 spike 写时如构造 stub data type → 复审 R3 (依据 PuzzleConfig 5-field ctor signature 实证 line 41 `PuzzleConfig(int id, InteractionBounds, float gridSize=1.0f, float snapSpeed=0.2f, float rotationStep=15f)`)

**ADR-029 verdict**: **✅ PASS** (R1 ✅ + R2 ✅ + R3 N/A auto-pass)

---

## History

- **2026-05-14 morning continue (Session 32)**: Draft 创建（emergent fix epic Track F NEW story-004~008 outline approved per [A]）；Status: Draft；S0-2 + S0-3 合并 narrow scope 1 story；本 story 与 story-004 input pipeline 联动（story-004 done 后 story-005 InteractionCoordinator OnTap listener 才 trigger）。
- **2026-05-14 afternoon continue (Session 32)**: **Phase 0 R2 vendor reality verify ✅ DONE (~30 min)** — 7/7 R2 evidence 完成 + 4 drift verdict 沉淀：
  - **Drift A** (Type-11 dp11 第 2 实战) — InteractableObject Inspector 字段 spec drift；实际 4 项 `_objectId/_puzzleId/_dragDepth/_gameplayCamera` (InteractableObject.cs:44-56)；spec 误列 `_shadowCasterGroup/_renderer` 不存在 + 漏列 `_dragDepth/_gameplayCamera` (必填 fail-loud)
  - **Drift B** (Type-11 dp11 第 2 实战) — Layer 注册 drift；实际 Layer 8 = `InteractableObject` 已注册 (TagManager.asset:19)；spec 写 Layer 9 'Interactable' index+name 错；好消息 不需 add，AC-8 自动满足
  - **Drift C** (Type-11 dp11 第 2 实战) — Tag policy drift；`InteractionCoordinator` tag 不存在；本 story Coordinator 用 Untagged
  - **Drift D** (Type-? dp18 candidate NEW) — **重大架构 dimensional mismatch** — Sprint 2 SP-013 InteractionCoordinator.RaycastWithFatFinger 用 Physics2D.OverlapCircleAll (2D API)；Object_01 BoxCollider + Object_02 CapsuleCollider 都是 3D collider；5 sprint 累积 architectural drift 未被任何 R2/R3/manual playtest 揭穿
  
  user 决策：(Drift D) **[A] narrow scope** Object_01/02 加 BoxCollider2D + CircleCollider2D 同存解 (0 production C# change；3D collider 保留给 ShadowMatch ADR-012 可能需要)；(Drift A/B/C) **[1] 原位 amend** story-005 关键 § (Goal Flow T1-T3 / ADR Decision Summary / Engine Notes R2.1-R2.7 / Control Manifest Rule References / Acceptance Criteria 含 AC-11 NEW / R3 PlayMode Probe Plan 5 case / R2 Assumptions Validated 表 / V3.0.1 Watch List Hooks 加 dp11 第 2 + dp18 NEW / Implementation Notes 文件清单)。Status: Draft → ✅ Phase 0 R2 verify DONE；next /story-readiness gate (R1+R2+R3 verdict)。
- **2026-05-14 night → 2026-05-14 night Session 33 morning continue (Session 33)**: **Phase 1 readiness gate ✅ READY** (~15 min) — `/story-readiness story-005` 27-checklist verdict `⚠️ NEEDS WORK (3 minor + 2 polish)` → user [A] 推荐 5 项 amend 一次性 clear all gap：
  - **Gap 1+2**: TR-ID semantic drift (story line 10 引用 `TR-objint-002`/`TR-objint-003` 都不对应 chapter 1 物件挂载 — 实际是 design constraint 非 TR-ID) + unresolved "TBD verify" wording —— amend 直接修正 GDD Requirement 行为 design constraint 引用 (object-interaction.md line 86 + concept.md line 106) + dp11 第 2 实战触发 sub-item (TR-ID semantic drift) 沉淀
  - **Gap 3**: performance budget note —— amend §Engine Notes 末尾加 perf budget paragraph (scene structure only / runtime impact 0 / InteractionCoordinator Initialize ~0.1 ms / R3 spike < 5s budget per S6-13 precedent)
  - **Gap 4**: §Test Evidence section NEW —— Asset/Integration 类型 evidence doc 路径明确 (`playmode-chapter-1-scene-wiring-2026-05-15.md` ~400-500 行 8 sections + R3 spike JSON dump + Unity scene diff + 0 production C# diff verify + 0 ProjectSettings diff verify)
  - **Gap 5**: §ADR-029 Verification section NEW —— R1 ✅ (per-event listener forbidden pattern grep audit 0 hits) + R2 ✅ (cross-component API existence 7/7 line-ref 实证 line 44-56 / 154-157 / 494-507 / 48-55 / 126-129 / 298 / 309 / TagManager.asset line 19) + R3 N/A auto-pass (本 story Asset/Integration 不构造 stub data type)；ADR-029 verdict ✅ PASS
  
  Verdict 升 `⚠️ NEEDS WORK` → `✅ READY`；Status: ✅ Phase 0 R2 verify DONE → ✅ Phase 1 readiness gate ✅ READY；next Phase 2 implementation (本 session 接 or 下 session 起始)。
