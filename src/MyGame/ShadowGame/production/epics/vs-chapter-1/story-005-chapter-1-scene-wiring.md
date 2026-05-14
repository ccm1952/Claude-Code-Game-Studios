// 该文件由Cursor 自动生成

# Story 005: Chapter 1 Scene Wiring — InteractionCoordinator GameObject + Object_01/02 InteractableObject MonoBehaviour 挂载

> **Epic**: VS Chapter 1
> **Story ID**: vs-chapter-1-005 (Sprint 6 emergent fix Track F NEW；S6-01 Phase 2.1 manual playtest NEEDS-WORK 派生 — S0-2 + S0-3 合并)
> **Sprint**: 6 (Track F NEW — chapter 1 production wiring emergent fix)
> **Story Type**: Asset / Integration (Unity scene structure + MonoBehaviour 挂载 + Inspector 拖入)
> **Complexity Points**: 1-1.5 (~1-1.5 hr 估时)
> **GDD Requirement**: TR-objint-002 (Chapter 1 仅 2 物件 + 1 光源 per object-interaction.md line 86 + concept.md line 106；本 story 落地 InteractableObject MonoBehaviour 挂载) + TR-objint-003 (待 tr-registry verify — Object Interaction Coordinator 注入)
> **ADR References**: **ADR-013 Object Interaction State Machine** (InteractableObject FSM + InteractionCoordinator + InteractionLockManager Sprint 2 done) + ADR-027 §3 IGestureEvent + §4 IInteractionEvent contract + ADR-029 V3.0.1 (R2 deficiency-flagged PASS path 候选；如 Object_01/02 layer 注册不齐则 deficiency-flag) + ADR-030 §VS Build commit
> **Status**: 📝 **Draft** (2026-05-14 Session 32 morning continue — emergent fix epic Track F NEW story-004~008 outline approved per [A]，本 story Phase 0 R2 vendor reality verify pending)
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
    → Inspector tag: Untagged 或 InteractionCoordinator (待 R2 决定 tag policy)
    → Inspector layer: Default
    → Inspector _gameplayCamera: drag Main Camera GameObject (NOT Camera.main fallback)
    → Inspector _interactableLayer: LayerMask = Interactable layer (待 R2 verify Layer 是否已注册 — 如未 register 则 Project Settings/Tags & Layers add Layer 9 'Interactable')
T2  Object_01_CoffeeMug AddComponent InteractableObject MonoBehaviour
    → Inspector layer: Interactable
    → Inspector _objectId: 1 (per InteractableObject._objectId field)
    → Inspector _puzzleId: 1 (per InteractableObject._puzzleId field — 单 puzzle 配合 chapter 1)
    → 可能 Inspector _shadowCasterGroup / _renderer reference (R2 verify InteractableObject Inspector 字段)
T3  Object_02_Book AddComponent InteractableObject MonoBehaviour (重复 T2 流程, _objectId: 2, _puzzleId: 1)
T4  InteractionCoordinator Inspector _objects: 拖入 [Object_01_CoffeeMug, Object_02_Book] (List<InteractableObject> 顺序无关)
T5  Save scene → unity-mcp manage_scene save → git diff verify Chapter_01_Approach.unity 改动符合预期 + 0 production C# change（GameApp.cs / InteractionCoordinator.cs 不改）
T6  Evidence: unity-mcp manage_scene get_hierarchy parent=root verify InteractionCoordinator GameObject 存在 + componentTypes 含 InteractionCoordinator + parent=Interactables verify Object_01/02 componentTypes 含 InteractableObject
```

---

## ADR Decision Summary

**ADR-013 Object Interaction State Machine inherit** — 本 story 不 amend ADR：
- InteractionCoordinator (Sprint 2 SP-013 done) 持有 List<InteractableObject> + LayerMask + Camera (Inspector 预填，禁 runtime FindObjectsOfType)
- InteractableObject MonoBehaviour (Sprint 2 SP-013 done) 含 InteractableObjectFsm (POCO) + 4 state (Idle/Selected/Dragging/Locked) + PuzzleConfigProvider 静态注入

**ADR-027 §4 IInteractionEvent contract inherit** — 9 method 已冻结 (Sprint 2 SP-013 done)：
- `OnObjectSelected(int objectId)` / `OnObjectDeselected(int objectId)` / `OnObjectTransformChanged(int objectId, Vector3 pos, float rot)` 等
- 本 story 仅 wire scene；OnObjectTransformChanged sender 由 InteractableObject.SnapRotation Sprint 2 done

**ADR-029 V3.0.1 R2 deficiency-flagged PASS path 候选** —— 如下任一情况触发 deficiency flag：
- Interactable layer 未在 Project Settings 注册 → 需 Layer 9 'Interactable' add（Project Settings 改动 vendor neutral，不算 vendor patch）
- InteractableObject Inspector 字段（_objectId / _puzzleId / _shadowCasterGroup 等）spec 与 Sprint 2 SP-013 实际不一致 → R2 grep verify 后 amend story 或 spec

---

## Engine Notes

**待 /story-readiness gate Phase 0 R2 vendor reality verify**：
- R2.1 ⚠️ TBD: InteractableObject Inspector 字段完整 list（`[SerializeField]` 声明 — _objectId / _puzzleId / _interactionBounds / _shadowCasterGroup / _renderer 等）
- R2.2 ⚠️ TBD: InteractionCoordinator Inspector 字段完整 list（`_objects` / `_interactableLayer` / `_gameplayCamera`）
- R2.3 ⚠️ TBD: Project Settings 现 Layer 注册状态 — Interactable layer 是否已 add (Layer 9 或其他)
- R2.4 ⚠️ TBD: Project Settings 现 Tag 注册状态 — InteractionCoordinator / Interactable / 其他自定义 tag 是否需 add
- R2.5 ⚠️ TBD: PuzzleConfig POCO 结构（Sprint 2 SP-013）字段 list — 与 story-006 GameApp provider injection 联动 verify
- R2.6 ⚠️ TBD: unity-mcp `manage_scene` add GameObject + add Component + save 路径完整 verify (S5-01 已实战 batch add GameObject precedent)
- R2.7 ⚠️ TBD: Object_01/02 当前 BoxCollider/CapsuleCollider 配合 InteractableObject FSM raycast 是否需 amend (Layer change → raycast hit)

---

## Control Manifest Rule References

**待 /story-readiness gate Phase 1 R1 grep audit**：
- ⚠️ TBD: Required — InteractionCoordinator must be Inspector-injected via _objects List, NOT runtime FindObjectsOfType
- ⚠️ TBD: Required — InteractionCoordinator._gameplayCamera must be Inspector-injected, NOT Camera.main fallback (per ADR-012)
- ⚠️ TBD: Forbidden — Object_01/02 layer = Default (must be Interactable for raycast filter)
- ⚠️ TBD: Required — InteractableObject._puzzleId must be Inspector-injected (NOT runtime guess) per Sprint 2 SP-013 spec

---

## Acceptance Criteria

| # | AC | Verify path |
|---|-----|------|
| AC-1 | Chapter_01_Approach.unity Hierarchy root level 加 InteractionCoordinator GameObject | unity-mcp manage_scene get_hierarchy parent=root |
| AC-2 | InteractionCoordinator Inspector _objects List = [Object_01_CoffeeMug, Object_02_Book] (顺序无关，Count=2) | unity-mcp manage_components get_serialized_field _objects |
| AC-3 | InteractionCoordinator Inspector _gameplayCamera = Main Camera GameObject (非 Camera.main fallback；属性 reference 可获取) | unity-mcp manage_components get_serialized_field _gameplayCamera |
| AC-4 | InteractionCoordinator Inspector _interactableLayer = Interactable layer (LayerMask value matches) | unity-mcp manage_components get_serialized_field _interactableLayer |
| AC-5 | Object_01_CoffeeMug AddComponent InteractableObject MonoBehaviour + Inspector _objectId=1 + _puzzleId=1 | unity-mcp manage_scene get_hierarchy parent=Interactables → componentTypes 含 InteractableObject |
| AC-6 | Object_02_Book AddComponent InteractableObject MonoBehaviour + Inspector _objectId=2 + _puzzleId=1 | 同 AC-5 |
| AC-7 | Object_01_CoffeeMug + Object_02_Book layer = Interactable (Layer 9 或其他 R2 决定的 layer index) | unity-mcp manage_scene get_hierarchy + componentTypes verify layer field |
| AC-8 | Project Settings → Tags and Layers → Interactable layer 已注册（Layer 9 或其他） | Project Settings inspect (R2.3) |
| AC-9 | 0 production C# change — GameApp.cs / InteractionCoordinator.cs / InteractableObject.cs 等 0 modify (本 story 仅 scene + Inspector + Project Settings 改动) | git diff --stat *.cs verify 0 modify |
| AC-10 | save scene + git commit；scene 文件大小变化合理（增加 ~3-5 KB InteractionCoordinator + 2 InteractableObject MonoBehaviour serialize data） | git diff Chapter_01_Approach.unity verify |

---

## R3 PlayMode Probe Plan（待 /story-readiness gate amend detail）

预计 spike `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-XX_ChapterSceneWiring.cs`：

- **P1 SceneHierarchyHasInteractionCoordinator** — Editor PlayMode load Chapter_01_Approach.unity → expect FindObjectOfType<InteractionCoordinator>() != null + _objects.Count == 2
- **P2 InteractableObjectsExistAndConfigured** — expect FindObjectsOfType<InteractableObject>() Length == 2 + _objectId in [1, 2] + _puzzleId == 1
- **P3 LayerFilterCorrect** — expect Object_01/02 layer == LayerMask.NameToLayer("Interactable") + Coordinator._interactableLayer mask 含此 layer
- **P4 CameraReferenceNonNull** — expect Coordinator._gameplayCamera != null + camera 是 chapter 1 scene Main Camera (不 Camera.main fallback)
- **P5 InteractionCoordinatorInitializeIdempotent** — call Initialize() 2 次 + expect 0 unexpected error + IsLocked == false

---

## R2 Assumptions Validated（待 Phase 0 实证）

| # | Assumption | Verify | Status |
|---|------------|--------|--------|
| R2.1 | InteractableObject Inspector 字段完整 list | Read InteractableObject.cs `[SerializeField]` | ⚠️ TBD |
| R2.2 | InteractionCoordinator Inspector 字段完整 list | Read InteractionCoordinator.cs `[SerializeField]` | ⚠️ TBD |
| R2.3 | Project Settings Interactable layer 注册状态 | Read ProjectSettings/TagManager.asset | ⚠️ TBD |
| R2.4 | Project Settings 自定义 tag 状态 | Read ProjectSettings/TagManager.asset | ⚠️ TBD |
| R2.5 | PuzzleConfig POCO 结构（与 story-006 联动） | Read PuzzleConfig.cs (Sprint 2 SP-013) | ⚠️ TBD |
| R2.6 | unity-mcp manage_scene add GameObject + Component path | unity-mcp tool descriptor | ⚠️ TBD |
| R2.7 | Object_01/02 Collider 配 raycast filter | Read Chapter_01_Approach.unity 现 Collider config | ⚠️ TBD |

---

## V3.0.1 Watch List Hooks

**Type-11 V3.0.1 dp15 candidate "EditMode green ≠ production wired sniff"** — 本 story 是 dp15 候选 sniff sub-clause **第 2 个 production wiring 修复 case**（chapter 1 scene 内 InteractableObject + InteractionCoordinator MonoBehaviour 挂载从 0 → 1+1+2）；与 story-004 input pipeline + story-006 provider injection + story-007 ShadowMatch wire 共同 surface dp15 governance 全貌。

**Type-2 (b) V3 candidate "Asset wiring drift"** —— 留观察 — 如 Sprint 5 S5-01 scene build 阶段 R2 grep verify InteractableObject MonoBehaviour 是否本应该挂（spec 隐含 contract）但 S5-01 evidence doc 未提及，dp 候选记录。

---

## Out of Scope（明示）

- ❌ **art asset 升级 (placeholder Cube primitive → 概念美术 asset)** — S6-09 separate Should story (per [A2] decision: replay 后再评估)
- ❌ **InteractableObject FSM behavior** — Sprint 2 SP-013 done (本 story 仅挂载 MonoBehaviour，不改 FSM 行为)
- ❌ **InteractionLockManager 实例化** — Sprint 2 SP-013 InteractionCoordinator OnEnable 内自动 new + Init()，本 story 不需新建
- ❌ **Camera.main fallback removal audit** —— ADR-012 已禁；本 story 仅确保 _gameplayCamera Inspector 注入
- ❌ **PuzzleConfig provider 注入** — story-006 范畴
- ❌ **Object_01/02 ShadowCasterGroup wiring** — story-007 ShadowMatch 范畴

---

## Implementation Notes（高层结构，待 Phase 0/1 R2 verify 后 amend 精细）

预计涉及文件：
1. **Assets/AssetRaw/Scenes/Chapter_01_Approach.unity** — 加 InteractionCoordinator GameObject + Object_01/02 InteractableObject MonoBehaviour
2. **ProjectSettings/TagManager.asset** — 如 R2.3 verify Interactable layer 未注册 → add Layer 9 'Interactable'
3. **Spike S6-XX_ChapterSceneWiring.cs** NEW — R3 PlayMode probe 5 case 验 scene wiring + Inspector field 注入正确

**0 production C# change 决策**：本 story 仅 scene 改动 + Project Settings 改动 + spike 新建；不改 Sprint 2 SP-013 production code (InteractableObject.cs / InteractionCoordinator.cs / GameApp.cs / DevTestState.cs 等)。

---

## History

- **2026-05-14 morning continue (Session 32)**: Draft 创建（emergent fix epic Track F NEW story-004~008 outline approved per [A]）；Status: Draft；S0-2 + S0-3 合并 narrow scope 1 story；本 story 与 story-004 input pipeline 联动（story-004 done 后 story-005 InteractionCoordinator OnTap listener 才 trigger）。
