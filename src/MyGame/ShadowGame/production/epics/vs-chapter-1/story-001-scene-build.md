// 该文件由Cursor 自动生成

# Story 001: Chapter 1 (靠近) Unity scene 实体构建首版

> **Epic**: VS Chapter 1
> **Status**: **Done (2026-05-09)** — 8 AC 全部 PASS；evidence: `production/qa/s501-scene-build-2026-05-09.md`
> **Layer**: Vertical Slice (VS)
> **Type**: Asset
> **Manifest Version**: 2026-05-09

---

## Context

**GDD**:
- `design/gdd/scene-management.md` — Scene Architecture / Registry (line 67 `Chapter_01_Approach`) / Core Rules
- `design/gdd/object-interaction.md` — line 86 Chapter 1 光源不可交互
- `design/gdd/shadow-puzzle-system.md` — line 42 Chapter 1 不引入光源操作
- `design/art/art-bible.md` — line 53 (色温) / line 121 (投影质量) / line 180 (空间风格)
- `design/concept/shadow-memory.md` — line 106 Chapter 1 仅 2 物件 + 1 光源
- `design/gdd/urp-shadow-rendering.md` — line 117 Ch.1 投影 5:1 对比 / Penumbra 1.0

**Requirement TR-IDs**: TR-scene-002 + TR-art-bible-001 + TR-objint-002 + TR-puzzle-001 + TR-narr-trigger-stub

**ADR Governing Implementation**: ADR-009 (Scene Lifecycle) + ADR-030 (VS-Late Pattern §VS Build Commitment 第 1 项)

**ADR Decision Summary**: chapter 1 first-version scene 实体（layout/lighting/material/物件/光源/trigger stub），不接 SceneManager production boot（留 story-001b）。Asset Type → 视觉 evidence + Editor open happy path 已是 verification ceiling，R3 PlayMode probe N/A reasoned。

**Engine**: Unity 2022.3.62f2 LTS + URP | **Risk**: LOW
**Engine Notes**:
- **Scene file 位置**: `Assets/Scenes/Chapter_01_Approach.unity`（项目实际 Scenes 目录；`Assets/GameMain/Scenes/` 不存在）
- **Scene name**: `Chapter_01_Approach`（与 GDD scene-management.md line 67 + scene-management story-006 schema 一致）
- **不**进 Build Settings（chapter scene 通过 YooAsset 热更包加载；BootScene 是唯一 Build Settings entry per ADR-009）
- **YooAsset Collector**: 统一到 `DefaultPackage`（per SP-003 + SceneManager.cs:79 hardcoded）；本 story Editor 模拟下不需要 Collector 注册即可 SceneView 打开，但 production build 前必须补（Out of Scope，由 S5-1b 或 S5-02 触发时确认）
- **渲染管线**: URP（per ADR-005 / `design/gdd/urp-shadow-rendering.md`）
- **不写 production C# 代码**（Asset Type story；本 story 内不创建 MonoBehaviour 子类）
- **unity-mcp batch_execute 全自动构建**（per `.cursor/rules/shadowgame-tengine.mdc` §"unity-mcp 自动化优先"）

**Performance**: N/A — Asset/Integration story；GDD line 67 估算 chapter_01 scene ~15 MB（含贴图音频）；本首版用 placeholder primitive 远低于该额度。

**Control Manifest Rules (this layer)**:
- **Required**: 章节 scene 用 `LoadSceneMode.Additive`；不进 Build Settings (ADR-009)
- **Required**: scene name = `Chapter_0X_Xxxx`（ADR-009）
- **Required**: 第一章光源不可交互（GameObject tag/script 标 `FixedLight`）(`object-interaction.md` line 86)
- **Required**: Chapter 1 = 2 个可操作物件 + 1 个固定光源（`concept/shadow-memory.md` line 106 design constraint；`shadow-puzzle-system.md` line 42 第一章不引入光源操作）
- **Required**: 色温 4000-4500K 柔象牙 + 淡暖黄；主背景 `#3A3530`（`art-bible.md` line 53；`scene-management.md` line 443）
- **Forbidden**: 不创建任何 MonoBehaviour production 子类（本 story Asset only；下 commit grep `Assets/GameScripts/HotFix/` diff 应为空）
- **Forbidden**: 不接 SceneManager.LoadChapterSceneAsync 真实调用（留 story-001b）
- **Forbidden**: 不进 Build Settings (ADR-009)

---

## Acceptance Criteria

*Asset/Integration type — 视觉 + 文件路径 + Console error_count == 0 evidence；R3 N/A reasoned 见下节*

- [ ] **AC-1 (file path)**: `Assets/Scenes/Chapter_01_Approach.unity` 文件存在；scene name 在 Unity Editor Hierarchy 显示为 `Chapter_01_Approach`
- [ ] **AC-2 (root hierarchy)**: scene root 至少含以下 GameObject 节点（命名规范见 `references/conventions.md`）：
  - `Environment/`（环境根）
    - `Environment/Walls/ProjectionWall`（投影墙面，BoxCollider + URP/Lit material 主色 `#3A3530`）
    - `Environment/Floor`（地板，URP/Lit）
  - `Lighting/FixedLamp`（1 个固定光源 GameObject + Light 组件 spotlight + tag `FixedLight`）
  - `Interactables/`（可交互物件根）
    - `Interactables/Object_01_CoffeeMug`（placeholder Cube + tag `Interactable`）
    - `Interactables/Object_02_Book`（placeholder Capsule + tag `Interactable`）
  - `Triggers/NarrativeTrigger_01`（BoxCollider isTrigger=true + tag `NarrativeTrigger` + 空 stub script slot；不接 ADR-016 production code，留 S5-02）
  - `Camera/MainCamera`（chapter 内本地 camera；与 MainScene 协同由 S5-02 决定）
- [ ] **AC-3 (lighting baseline)**:
  - Ambient mode = `Color`，值 = `#3A3530` × 0.3 强度
  - Directional Light = 4500K 色温对应 ~`#F5E6CA`，intensity 1.0
  - Skybox material = 默认 procedural（first-version；S5-04 sign-off 后 polish）
- [ ] **AC-4 (material)**: 物件、墙、地板均用 URP/Lit 默认 material 并 set BaseColor 到 art-bible 范围（柔象牙 + 淡暖黄系；具体 hex 由 unity-mcp 操作时 inline 选；可用 `#E8DCC4` / `#F5E6CA` / `#3A3530` 三色循环）
- [ ] **AC-5 (visual evidence)**: Editor SceneView 截图保存到 `production/qa/s501-scene-build-2026-05-XX.md`；目测确认空间感（咖啡桌 / 公共空间 / 窗边氛围）+ 投影墙面清晰
- [ ] **AC-6 (Build Settings 不污染)**: `EditorBuildSettings.scenes` 不含 `Chapter_01_Approach.unity`
- [ ] **AC-7 (无 production C# 代码)**: 本 story commit diff 内 `Assets/GameScripts/HotFix/` 无任何变更（grep evidence in §Test Evidence）
- [ ] **AC-8 (Editor 双击打开 happy path)**: 双击 `.unity` 后 Unity Editor Console 不报 error / exception（warning 允许）；evidence: `read_console` snapshot

---

## R3 Justification (Asset/Integration type — N/A)

按 ADR-029 V2.0 R3 mandatory criterion，**本 story R3 PlayMode probe = Not Applicable**：

1. 本 story 不写 production C# 代码（**AC-7 grep evidence 担保**）
2. 不调任何 framework boundary API（无 `GameModule.Scene.LoadSceneAsync` / `GameModule.Resource.LoadAssetAsync` / `Instantiate` 等 runtime call）
3. 视觉 evidence (AC-5) + Editor 打开 happy path (AC-8) + Hierarchy/Material/Lighting 静态校验 (AC-2/3/4) 已构成 Asset type 的 verification ceiling
4. framework boundary probe 责任 propagated 到 **story-001b**（boot pipeline 接入是真正的 framework boundary）

R3 N/A reasoning 已在本节 record；如未来 S5-02 实测发现 chapter scene 在 Editor 与 LoadSceneAsync 真实运行 behavior diverge，应在 story-001b 或 S5-02 readiness 内 capture 为新的 R3 case（per ADR-029 V2.0 §V2-7 V3 watch list）。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **story-001b**: SceneManager production boot pipeline 接入 + fixture ChapterDataProvider + dev menu/FSM state 触发 LoadChapterSceneAsync(1) + R3 PlayMode probe
- **S5-02**: 5 系统端到端串通 (object interaction → puzzle → narrative → audio → scene transition)
- **S5-04**: art-bible Status: Draft → Accepted；本 story art 起步用 placeholder URP/Lit material，待 S5-04 sign-off 后再 polish (mesh/texture swap)
- **Luban TbChapter 真接入**（user decision 2026-05-09: post-VS）
- **投影 puzzle production logic 注入**（S5-03 ✅ DONE，但本 scene 不实例化具体 puzzle config，留 S5-02）
- **YooAsset Collector 注册到 DefaultPackage**（Editor 模拟下不需要；production build 前由 S5-1b 或 S5-02 触发时确认）

---

## Implementation Notes

*Asset/Integration via unity-mcp batch_execute (per `.cursor/rules/shadowgame-tengine.mdc` §"unity-mcp 自动化优先")*

按 mdc rule §"unity-mcp 自动化优先" 原则，本 story 全部走 `batch_execute`。预计 batch 拆分：

**Batch 1 (~10 commands)**: scene create + 根 hierarchy create + lighting/Camera 节点占位
- `manage_scene` action=`create` path=`Assets/Scenes/Chapter_01_Approach.unity`
- `manage_gameobject` create root: `Environment` / `Lighting` / `Interactables` / `Triggers` / `Camera`
- `manage_gameobject` create child: `Environment/Walls`

**Batch 2 (~8 commands)**: Environment 子树 (Walls/Floor) + URP/Lit material 创建 + 颜色 set
- `manage_gameobject` create primitive Cube `Environment/Walls/ProjectionWall` (scale 5, 3, 0.2)
- `manage_gameobject` create primitive Plane `Environment/Floor`
- `manage_material` create `Mat_ProjectionWall_Ivory` (URP/Lit, BaseColor `#3A3530`)
- `manage_material` create `Mat_Floor_LightWarm` (URP/Lit, BaseColor `#E8DCC4`)
- `manage_components` set MeshRenderer.material

**Batch 3 (~6 commands)**: Lighting/FixedLamp Light 组件 + 色温 set + tag 创建
- `manage_editor` add_tag `FixedLight` (如不存在)
- `manage_gameobject` create `Lighting/FixedLamp` (empty + Light component spot)
- `manage_components` set Light: type=Spot, color=`#F5E6CA`, intensity=1.0, range=8, spotAngle=45
- `manage_gameobject` set tag `FixedLight`

**Batch 4 (~6 commands)**: Interactables/Object_01 + Object_02 primitive + Tag/Material set
- `manage_editor` add_tag `Interactable` (如不存在)
- `manage_gameobject` create primitive Cube `Interactables/Object_01_CoffeeMug` (scale 0.3)
- `manage_gameobject` create primitive Capsule `Interactables/Object_02_Book` (scale 0.3, 0.5, 0.1)
- `manage_components` set MeshRenderer.material `Mat_Object_Default`
- `manage_gameobject` set tag `Interactable` (both)

**Batch 5 (~4 commands)**: Triggers/NarrativeTrigger_01 BoxCollider isTrigger + Tag set
- `manage_editor` add_tag `NarrativeTrigger` (如不存在)
- `manage_gameobject` create empty `Triggers/NarrativeTrigger_01`
- `manage_components` add BoxCollider isTrigger=true size=(2,2,2)
- `manage_gameobject` set tag `NarrativeTrigger`

**Batch 6**: scene save + Editor screenshot + read_console
- `manage_scene` action=`save`
- `manage_camera` action=`screenshot` capture_source=`scene_view` view_target=`Environment/Walls/ProjectionWall` screenshot_file_name=`s501-scenebuild-2026-05-09` include_image=`true`
  > **R2.6 修正**：CoplayDev/unity-mcp v9.6.x 把 screenshot 从 `manage_scene` 迁移到 `manage_camera`；description 明确建议 "For screenshots, use manage_camera"。详见 `.claude/memory/problem_2026-05-09_unity-bridge-to-coplaydev-switch.md`。
- `read_console` 读取最后 30 lines 验证 0 error

每 batch `failFast=true`。如某一 batch 报 `Tag not exists` 错（虽然已加 add_tag），先用 `manage_editor` 显式 `add_tag` 补 tag 再继续。

如 unity-mcp Bridge 中途 disconnect，重启 Bridge 后从未完成的 batch 续跑。

---

## QA Test Cases

*Asset/Integration type — manual visual evidence gate*

- **AC-1, AC-6, AC-7**: 文件系统 + grep 自动可验
  - `[ -f Assets/Scenes/Chapter_01_Approach.unity ]`
  - `git diff --stat HEAD -- Assets/GameScripts/HotFix/` 应空
  - `EditorBuildSettings.scenes` 不含本 scene 路径
- **AC-2, AC-3, AC-4**: unity-mcp `manage_scene` action=`get_hierarchy` + `find_gameobjects` 自动可验
- **AC-5**: Editor SceneView 截图（unity-mcp `manage_camera` action=`screenshot` capture_source=`scene_view` 或 manual）
- **AC-8**: Console 日志检查（unity-mcp `read_console` filter level=Error；error_count == 0）

---

## Test Evidence

**Story Type**: Asset
**Required evidence**:
- `production/qa/s501-scene-build-2026-05-XX.md` — 含
  - SceneView screenshot
  - scene hierarchy dump (`find_gameobjects searchMethod=by_path` 输出)
  - Material/Lighting properties dump
  - Console snapshot (0 error)
  - `git diff --stat HEAD -- Assets/GameScripts/HotFix/` 空 evidence
  - `EditorBuildSettings.scenes` 列表 dump 不含本 scene

**Status**: ✅ DONE 2026-05-09 — evidence 文件 `production/qa/s501-scene-build-2026-05-09.md` 已落盘 + 8/8 AC PASS + 用户 macOS Quick Look 视觉 sign-off + 0 production C# 改动 + 0 EditorBuildSettings 污染

---

## Dependencies

- **Depends on**:
  - S3-01 ✅ (LoadChapterSceneAsync) — 仅 scope reference 不真调
  - S4-01..03 ✅ (frameworks) — 仅 scope reference
  - S5-04 (art bible) — placeholder OK 不阻塞
- **Unlocks**:
  - **story-001b** (SceneManager boot pipeline 接入)
  - **S5-02 end-to-end** (经 story-001b 后)

---

## Implementation Log

### 实施时间 + 工具链

- **Date**: 2026-05-09 (Sprint 5 Track A 启动日)
- **Toolchain**: CoplayDev/unity-mcp v9.6.x (HTTP transport, port 8888) + `batch_execute` MCP tool（per Phase 1-4 unity-bridge → CoplayDev 切换 — 详 `/.claude/memory/problem_2026-05-09_unity-bridge-to-coplaydev-switch.md`）
- **Total elapsed**: ~3 h（Phase 1-4 toolchain 切换 ~60 min + Phase 5 6-batch 实施 ~90 min + Phase 6 evidence + lighting bug 修复 ~30 min）
- **R2 假设 sign-off (2026-05-09 morning)**: 4 项假设全部成立 — `manage_editor add_tag` 存在 + URP Skybox material = `Default-Skybox` (procedural) + Lighting Window ambient mode 通过 `execute_code` set `RenderSettings.ambientMode/ambientLight` + `Assets/Scenes/main.unity` 是 BootScene + EditorBuildSettings.scenes = [main, ShadowPrototype]，无需 patch story file。

### Batch 拆分实际执行情况

原 plan 6 batches 实际拆成 ~10 个 MCP call（含 silent failure 修复批 + screenshot 重抓批 + lighting bug 修复批）。`fail_fast=false` 用于诊断 batch（看哪些 cmd 失败），`fail_fast=true` 用于线性 build batch（确保 GameObject 顺序创建）。

| Phase | Batches | 状态 |
|---|---|---|
| **Phase 5a — 实体构建** | Batch 1 (scene + root hierarchy) → Batch 2 (Walls/Floor + 3 mat create) → Batch 3 (FixedLamp + tag) → Batch 4 (Object_01 + Object_02 + tag) → Batch 5 (Trigger + tag) → Batch 6 (save + screenshot) | 6 batches ✅ 全 success |
| **Phase 5b — Material 修复** | `manage_material assign_material_to_renderer` 默认 `search_method=by_name` 失败 → 用 `search_method=by_path` 重试 ✅ | 2 retry batches |
| **Phase 5c — Lighting intensity polish** | DL intensity 1.0 + FL intensity 1.0 视觉过曝 → set_property 调到 0.5 + 0.7 ✅ + 重抓 screenshot | 2 batches |
| **Phase 6a — Lighting silent fail 修复** | `manage_gameobject create component_properties.Light` silent fail（type / useColorTemperature / colorTemperature / color array）→ Phase 6 evidence dump 暴露 → `manage_components set_property` 单独 set 6 properties（5/6 成功，color array 失败 reasoned 为色温模式不需 color）| 1 修复 batch |
| **Phase 6b — DL rotation 修复** | "ProjectionWall 整体纯黑" → DL forward.z=+0.56 朝 wall 背面 → `execute_code` 设 rotation (50, 200, 0) → forward.z=-0.60 朝 wall 前面 ✅ | 1 final batch |

### Toolchain Silent Failures Surfaced（详 evidence §10）

实施暴露 4 类 CoplayDev/unity-mcp v9.6.x silent failure，全部已修复：

1. **D1**: `manage_gameobject action=create component_properties.Light` 对 type / useColorTemperature / color (array) / range / spotAngle 等字段 silent fail（server return success=true 但 final state 是 default 值）
2. **D2**: `manage_components action=set_property` 一次推 N properties 时 part-success 行为（部分成功不回滚，但 server return success=false 含失败列表）
3. **D3**: `manage_components properties.color` 不接受 `[r, g, b, a]` array 格式（要求 `{r, g, b, a}` object 格式）— 与 `manage_material set_material_color` 接受 array 格式不一致
4. **D4**: `manage_camera action=screenshot capture_source=game_view` 在 Game View tab 没活时 fallback 到 ScreenCapture of currently focused window（修复用 `execute_code` 强制 Game View focus + 去选中）

D5 (DL rotation forward 计算) 是 Unity light direction 几何 trap，非 toolchain 问题。

D6 (agent confirmation bias 误判 game view screenshot 内容) 沉淀到 lessons memo `/.claude/memory/problem_2026-05-09_image-preview-confirmation-bias.md`。

### Visual Sign-off 路径

Phase 5 期间 agent 多次依赖 Cursor image preview 评判 game view screenshot 内容，由于 confirmation bias 反复误判（详 D6）。最终 visual sign-off 路径采用 **用户 macOS Quick Look 仲裁** + agent 信任 server metadata + IHDR + md5 三条独立 ground truth 信号。

最终 sign-off image: `Assets/Screenshots/screenshot-20260509-152000.png` (1024×1024, 67 KB) — 用户 2026-05-09 15:25 macOS 原生工具核实通过。

### AC Closure Matrix

| # | AC | 状态 |
|---|---|---|
| AC-1 | scene 文件存在 + name `Chapter_01_Approach` | ✅ PASS |
| AC-2 | scene root 6 项节点 + 必须 child + 3 tag 添加 | ✅ PASS |
| AC-3 | Ambient `#3A3530` × 0.3 + DL 4500K + Skybox default | ✅ PASS（含 intensity polish 0.5/0.7 placeholder 待 S5-04） |
| AC-4 | 3 个 URP/Lit material + BaseColor in art-bible 三色循环 | ✅ PASS |
| AC-5 | 视觉 evidence + 用户 macOS Quick Look sign-off | ✅ PASS |
| AC-6 | EditorBuildSettings.scenes 不含本 scene | ✅ PASS |
| AC-7 | `Assets/GameScripts/HotFix/` 0 production C# 改动 | ✅ PASS |
| AC-8 | Editor 双击打开 happy path Console 0 new error | ✅ PASS |

**8 / 8 PASS** → S5-01 dev-story closed.

### Evidence Document

完整 evidence 见 `production/qa/s501-scene-build-2026-05-09.md`（含 Hierarchy dump / Material dump / Lighting state dump / Console snapshot / git diff / EditorBuildSettings dump / 7 PNG screenshot pathway / 8 AC 实测 verify / 6 Deviations 详细记录 / R3 N/A justification）。

### Lessons Memo Surfaced

- `/.claude/memory/problem_2026-05-09_unity-bridge-to-coplaydev-switch.md` — Phase 1-4 unity-bridge → CoplayDev/unity-mcp toolchain 切换教训（schema vs runtime drift / fastmcp SOCKS 代理 trap / Cursor 多 server 重复配置）
- `/.claude/memory/problem_2026-05-09_image-preview-confirmation-bias.md` — Phase 5 后期 agent 视觉判断 confirmation bias 教训（信任 server metadata + IHDR + md5 优先于 agent pattern recognition；遇到 ≥ 2 次"server 说 X 我看到 Y"冲突时让用户原生工具仲裁）

### Unlocks (per Dependencies)

- **story-001b**: SceneManager boot pipeline 接入 + fixture ChapterDataProvider（unblocked — scene asset ready）
- **S5-02**: Chapter 1 end-to-end 5 系统串通（unlocked once story-001b done — ADR-030 §VS Build Commitment 第 1 项核心交付）

---

## Notes

本 story scope 由 2026-05-09 scope split discussion 锁定。原 sprint-status.yaml line 770 描述含混，经讨论拆分如下：

| 原 sprint-status notes | 实际归属 |
|----------------------|---------|
| baseline lighting + 投影墙面 + ≥1 可操作物件 + ≥1 光源 + ≥1 narrative trigger zone | **本 story (S5-01)** —— 但 GDD design constraint 严格要求 chapter 1 = 2 物件 + 1 光源（不是 ≥1），已 strengthen 到 AC-2 |
| "通过 SceneManager.LoadChapterSceneAsync 可 load" | 拆出 **story-001b (S5-1b)** —— 需要 GameApp.Entrance 内 boot integration + fixture provider，本 story 不做 |

Per `production/session-state/active.md` 2026-05-09 update + `sprint-status.yaml` S5-01/S5-1b entry。
