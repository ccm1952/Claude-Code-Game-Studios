// 该文件由Cursor 自动生成

# Story: UIModule Initialization + UIWindow Base Class Setup (Sprint 5 narrow scope amendment)

> **Epic**: ui-system
> **Story ID**: ui-system-001
> **Story Type**: **Integration** *(2026-05-11 amend: 原 Logic → Integration；Framework Boundary R3 mandatory per ADR-029 V2.0)*
> **GDD Requirement**: TR-ui-001 (All UI via TEngine UIModule), TR-ui-002 (5 UI layer levels — UILayer 枚举部分；Popup Queue/Auto InputBlocker 完整行为 → story-008 cover)
> **ADR References**: ADR-011 (UIWindow Management), ADR-001 (TEngine Framework), SP-002 (UIWindow Lifecycle), ADR-029 V2.0 (R3 mandatory), ADR-027 (Event Layer)
> **Sprint**: **Sprint 5** *(2026-05-11 promote should-have → must-have per Sprint 5 [A] serial 序列：S5-04 ✅ → S5-08 → S5-02 → S5-07；详 §History)*
> **Status**: **Draft** *(2026-05-11 amend pending /story-readiness gate)*
> **Manifest Version**: 2026-05-11

## Context

**Trigger**:

Sprint 5 [A] serial 序列第 2 步（2026-05-11 user 决策）；S5-04 art-bible sign-off ✅ DONE 2026-05-11 unlocks 本 story；本 story DONE 后 unblocks S5-02 Chapter 1 end-to-end dev-story。S5-08 carryover hard rule 满足（S3-08 → S4-09 → Sprint 5 promote must-have）。

**Goal (Sprint 5 narrow scope amendment)**:

建立 UIModule 运行时基础 —— **UIRoot Canvas 实例化 + base canvas 挂到 scene + GameModule.UI.ShowWindow API 通路 verified + UILayer 5 层枚举 + UIWindow lifecycle 文档注释**。本 story DONE 后，S5-02 minimal main menu（base canvas + 'Start Chapter 1' Button + 'Next Chapter' Button minimal inline）有 production API 可挂。

UIModule 本身由 TEngine 提供，位于 `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/`（GameLogic 热更程序集，非 TEngine Runtime）。本 story **不实现任何具体面板**（main menu / pause menu / settings 等），只建立 UIRoot + ShowWindow API + base canvas runtime 通路。

**Scope (2026-05-11 narrow amendment per S5-02 minimal_inline 决策)**:

- ✅ **In Scope**: UILayer 枚举（5 层 sorting order base） / UIRoot Canvas runtime 实例化 / base canvas 挂到 scene root / `GameModule.UI.ShowWindow<T>()` API 通路 verify / 1 个 mock minimal panel 实例化到 UIRoot 验证 framework wiring / UIWindow lifecycle (OnCreate/OnRefresh/OnUpdate/OnClose) 文档注释 / UIModule 程序集路径注释
- ❌ **Out of Scope (deferred to existing stories)**:
  - **Popup Queue Manager** / Auto-Dequeue / Auto InputBlocker (Popup/Overlay) / InputBlocker token 命名规范 / Overlay 不受 Popup Queue 限制 / 双 InputBlocker 叠加 / `CloseAllPopups()` 工具 → 全部由 **`story-008-ui-layer-strategy.md`** cover（已存在 9 AC + 5 TC，Status: Ready 依赖 ui-system-001）；Sprint 6 polish 时实施
  - **Full Main Menu UIWindow** (New Game / Continue / Settings 按钮 + 存档检查 + fade-in 动画 + 主菜单 BGM) → 由 **`story-006-main-menu.md`** cover (UI type, 7 AC, Status: Ready)；Sprint 6 polish 时实施
  - 其他 ui-system-002..-007/009/010 story 全部留 Sprint 6+

**GDD**:

- `design/gdd/ui-system.md` — UIModule / 5 UI layer levels / UIWindow lifecycle

**Requirement TR-IDs**:

- `TR-ui-001` ✅ All UI via TEngine UIModule (本 story Cover UIModule runtime init + GameModule.UI 访问通路)
- `TR-ui-002` ⚠️ 5 UI layer levels — **partial cover**: 本 story cover UILayer 枚举定义；Popup/InputBlocker 完整运行时行为由 story-008 cover
- `TR-ui-003` (Popup/Overlay auto InputBlocker) / `TR-ui-008` (Popup queue 1 visible) / `TR-ui-019` (Touch target ≥ 44×44pt) → **story-008** cover
- `TR-ui-005..-022` UI Window 系列 → **story-002..-007/009/010** cover
- `TR-vs-chapter-1-end-to-end-uimodule-prereq` ⚠️ **DEFICIENCY-FLAGGED 2026-05-11** — 本 story 是 S5-02 hard prerequisite 的 contract；按 ADR-029 V2.0 deficiency-flag 协议显式标记等待下次 `/architecture-review` 注册（不阻塞 dev-story）

**ADR Governing Implementation**:

- **ADR-011 (Accepted)** UIWindow Management — 5 层 UI 分级 / 9 UIWindow / Popup queue FIFO / safe area / UIWindow lifecycle 规范
- **ADR-001 (Accepted)** TEngine Framework — UIModule 是 TEngine 核心模块
- **SP-002** UIWindow Lifecycle — OnCreate (首次) / OnRefresh (每次打开) / OnUpdate (可见帧) / OnClose (清理) 时序
- **ADR-029 V2.0 (Accepted) R3 mandatory** — Integration type 必须 PlayMode probe
- **ADR-027 (Accepted)** Event Layer — UIModule + GameEvent.Get<IEvent>() 协议；V2-5 listener self-removal pattern
- **ADR-030 (Accepted)** §VS Build commitment 第 1 项 — Sprint 5 [A] serial 序列 S5-08 hard prerequisite for S5-02

**Engine Notes** *(R2 待 /story-readiness gate 实测；本 story 关键 wiring uncertainty 见 §Assumptions)*:

- `GameModule.UI` static accessor （per TEngine UIModule 6.2.1 vendor pattern）— 待 R2 grep verify 已暴露
- `UIModule.SetUIRoot(Canvas root)` 或等效 API — 待 R2 grep verify TEngine vendor exposed
- `UIModule.ShowWindow<T>()` / `CloseWindow<T>()` API — 待 R2 grep verify signature
- `UIWindow` abstract base class 路径 `TEngine.UIWindow` 或 `GameLogic.UIModule.UIWindow` — 待 R2 verify

**Performance**: R3 mandatory Integration type；S5-08 dev-story 总 workshift ≤ 3.5h（estimation 2 SP）。

**Control Manifest Rules (this layer)**:

- **Required**: UIRoot Canvas 必须在 GameApp 启动序列中实例化（per ADR-009 boot order；具体步骤号待 R2 verify 与 TEngine 16-step Init Order 对齐）
- **Required**: 所有 UIWindow / UIWidget 访问通过 `GameModule.UI` 静态门面，**禁止** `ModuleSystem.GetModule<UIModule>()`（per `tengine-dev` skill L0 protocol + ADR-001）
- **Required**: UIWindow 子类 OnCreate / OnRefresh / OnUpdate / OnClose 4 lifecycle method 必须含 `<summary>` 文档注释说明调用时序（per SP-002 验收的真实时序）
- **Required**: UIModule 所在程序集路径必须在代码注释中标注：`Assets/GameScripts/HotFix/GameLogic/Module/UIModule/`（防止团队错误地在 TEngine Runtime 查找）
- **Forbidden**: 不实施 Popup Queue Manager / Auto-Dequeue / Auto InputBlocker（留 story-008 Sprint 6 polish）
- **Forbidden**: 不实施任何具体 UIWindow 业务面板（main menu / pause menu / settings 等留 story-002..-007 Sprint 6+）
- **Forbidden**: 不修改 TEngine 核心代码（UIModule core 是 vendor 范畴；如发现 vendor bug 走 `tengine-dev` skill R1~R4 vendor patch 协议）

---

## Acceptance Criteria

*Integration type — Framework boundary + R3 PlayMode probe MANDATORY (ADR-029 V2.0)*

- [ ] **AC-1 (UILayer 枚举定义)**: 创建 `Assets/GameScripts/HotFix/GameLogic/UI/UILayer.cs` enum：`Background = 0`, `HUD = 1`, `Popup = 2`, `Overlay = 3`, `System = 4`；每层 sorting order base = `layer × 100`；含 1 helper static method `GetSortingOrderBase(UILayer layer)` 返回 `(int)layer * 100`
- [ ] **AC-2 (UIRoot Canvas runtime 实例化)**: GameApp 启动序列中 UIRoot Canvas 实例化路径 verified —— Canvas GameObject 挂到 scene root + `RenderMode = ScreenSpaceOverlay` (或按 ADR-011 决定) + 子 layer container `Background` / `HUD` / `Popup` / `Overlay` / `System` 5 个空 GameObject 按 sorting order 排序 + UIRoot Canvas 持久化（`DontDestroyOnLoad` 或等效 TEngine 协议）
- [ ] **AC-3 (GameModule.UI 静态门面通路)**: `GameModule.UI` 已暴露且 non-null（不抛 NullReferenceException）；spike 通过 reflection 或直接调用 `GameModule.UI` accessor 拿到 UIModule instance；该 instance 可调用 `ShowWindow<T>()` / `CloseWindow<T>()` API（API 签名 TEngine vendor exposed）
- [ ] **AC-4 (UIWindow Show/Close API 通路 verify)**: 创建 1 个 mock minimal UIWindow 子类 `S5_08_MockMinimalPanel.cs`（**仅本 story spike 用**，DevTest 命名空间，不进入 GameLogic.UI production 路径）继承 UIWindow base class；spike `GameModule.UI.ShowWindow<S5_08_MockMinimalPanel>()` 后该 panel 实例化到 UIRoot 对应 layer container 子节点 + active=true；`GameModule.UI.CloseWindow<S5_08_MockMinimalPanel>()` 后 panel inactive 或 destroyed
- [ ] **AC-5 (UIWindow lifecycle 文档注释)**: `S5_08_MockMinimalPanel` 4 lifecycle method (`OnCreate` / `OnRefresh` / `OnUpdate` / `OnClose`) 含 `<summary>` 文档注释说明调用时序（per SP-002 实证：首次打开 OnCreate → OnRefresh 同帧 / 重新打开仅 OnRefresh / OnUpdate 仅 Visible=true / OnClose 清理）；mock panel 每个 lifecycle method 内 `Debug.Log` 记录调用顺序便于 R3 PlayMode verify
- [ ] **AC-6 (UIModule 程序集路径注释)**: `UILayer.cs` 顶部含代码注释明示 UIModule 所在程序集路径：`Assets/GameScripts/HotFix/GameLogic/Module/UIModule/`（GameLogic 热更程序集，非 TEngine Runtime）；防止团队错误地在 TEngine Runtime 中查找
- [ ] **AC-7 (Out of Scope 明示)**: story file §Out of Scope 段明示 Popup Queue / Auto InputBlocker / 完整 UIWindow 业务面板 全部由 story-008 / story-002..-007 cover；本 story 不实施
- [ ] **AC-8 (S5-02 main menu Button mount API verified)**: spike 内验证 `S5_08_MockMinimalPanel` 上可挂 UnityEngine.UI.Button 子组件 + `Button.onClick.AddListener` API 通路；spike 模拟 `Button.onClick.Invoke()` 后 listener handler 被调用（S5-02 minimal main menu 'Start Chapter 1' + 'Next Chapter' Button click path 同前置）
- [ ] **AC-9 (console clean)**: R3 PlayMode probe 全程 0 unexpected error / 0 unexpected warning（spike 用 `LogAssert.Expect` 主动标记 expected 项；如无 expected error/warning 则 0/0 实测）
- [ ] **AC-10 (R3 PlayMode probe ALL PASS)**: spike `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-08_UIModuleSetup.cs` 4 R3 case 全 PASS + JSON evidence `~/Library/Application Support/.../S5-08_Result.json` `all_passed=true` + `production/qa/playmode-uimodule-setup-2026-05-XX.md` evidence doc 写完

---

## Implementation Notes

- **UIModule 路径**：`Assets/GameScripts/HotFix/GameLogic/Module/UIModule/`（GameLogic 热更程序集；**禁止** `ModuleSystem.GetModule<UIModule>()`，所有访问通过 `GameModule.UI`）
- **UIRoot Canvas 实例化时机**：GameApp.Entrance 启动序列；具体步骤号 R2 readiness gate 实证（按 TEngine 16-step Init Order 与 ADR-011 决定）
- **UILayer.cs 路径**：`Assets/GameScripts/HotFix/GameLogic/UI/UILayer.cs`（新 enum 文件；不改 TEngine vendor 代码）
- **Mock minimal panel 路径**：`Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5_08_MockMinimalPanel.cs`（DevTest namespace；本 story spike 专用，不入 production UI 路径；S5-02 dev-story 实施时改写为正式 minimal main menu panel）
- **Spike 路径**：`Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-08_UIModuleSetup.cs`（1 file + 3 inner class per S5-1b/1c precedent：`S508Spike : IDevSpike` + `S508Runtime : MonoBehaviour` + `S508Tester` 纯逻辑）
- **GameApp.cs 改动**：`RegisterDevSpikes` 切换 `S51cSpike` → `S508Spike`（已是项目惯例 per S5-1c precedent）；其他改动需待 R2 实证 UIModule init 时机后决定（如 GameApp `_uiModule` field + Init/Dispose 等）
- **Listener 模式 (per S5-1c lessons memo)**：如本 story spike 需 subscribe 任何 TEngine event，**subscribe 必须在 `Awake()` 而非 `Start()`**（per `problem_2026-05-09_spike-sync-subscribe-race.md`）；本 story 主要 framework boundary 不涉同步事件 race，但仍按惯例执行
- **TEngine vendor 调研路径**：实施前先用 `SemanticSearch` / `Grep` 在 `src/MyGame/ShadowGame/repowiki/zh/content/` 内查 `UIModule` / `UIRoot` / `ShowWindow` / `UIWindow` 章节，找到 TEngine 6.2.1 vendor 实际 API 签名（按 `.cursor/rules/shadowgame-tengine.mdc` R3+ 任务 references 不足时协议）
- **TEngine vendor 不修改**：本 story 不修改 `Assets/TEngine/` 任何文件；如发现 vendor API gap → 走 `tengine-dev` skill R1~R4 vendor patch 协议

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Popup Queue Manager** / Auto-Dequeue / Auto InputBlocker (Popup/Overlay) / InputBlocker token 命名规范 / Overlay 不受 Popup Queue 限制 / 双 InputBlocker 叠加 / `CloseAllPopups()` 工具 → 已存在 [`story-008-ui-layer-strategy.md`](story-008-ui-layer-strategy.md)（Logic type, 9 AC + 5 TC, Status: Ready 依赖 ui-system-001）；Sprint 6 polish 时实施
- **Full Main Menu UIWindow** (New Game / Continue / Settings 按钮 + 存档检查 `ISaveService.HasValidSave()` + fade-in 动画 + 主菜单 BGM `Evt_PlayMusicRequest`) → 已存在 [`story-006-main-menu.md`](story-006-main-menu.md)（UI type, 7 AC, Status: Ready 依赖 ui-system-001）；Sprint 6 polish 时实施
- **GameHUD Window** (5 widgets) → `story-002-game-hud.md` Sprint 6+
- **PauseMenu / PuzzleComplete / ChapterSelect / SettingsPanel** → `story-003`/`-004`/`-005`/`-007` Sprint 6+
- **Safe Area Fitting** (notch / rounded corner) → `story-009-safe-area.md` Sprint 6+
- **UI Text Localization** (ILocalizationModule binding) → `story-010-localization-binding.md` Sprint 6+
- **Android back button** / **Gaussian blur fallback** / **Typewriter text effect** / **Animation scale accessibility** → 各对应 ui-system stories Sprint 6+
- **S5-02 内 minimal main menu UIWindow** (`Start Chapter 1` + `Next Chapter` 2 Button) → S5-02 dev-story 内基于本 story API 通路实施（本 story 不实施具体 main menu，仅 verify mock panel + API 通路）

---

## R3 Justification (Integration — MANDATORY)

按 ADR-029 V2.0 R3 mandatory criterion，本 story 是**典型的 Framework Boundary Integration story** —— TEngine UIModule + UIRoot Canvas runtime 实例化 + GameModule.UI static accessor + UIWindow base class lifecycle + Mock panel ShowWindow/CloseWindow API 通路 + Button onClick path verify。framework boundary probe ≥ 4 处，必须 PlayMode 实证（EditMode 不能验：UIModule init success / UIRoot Canvas 是否真挂到 scene root + 5 layer container 创建 + `GameModule.UI.ShowWindow<T>()` API 是否真把 panel 实例化到 canvas / UIWindow lifecycle 在真实 runtime 的调用时序 / Button.onClick.Invoke() 是否真触发 listener）。

### R3 PlayMode probe 4 cases（spike `S5-08_UIModuleSetup.cs` — **(M1) production reflection 全程**复用 S5-1c/S5-02 precedent）

> **Spike 模式**:
> - 全程**复用 production**：`GameApp._uiModule` 或 `GameModule.UI` reflection 拿 instance；listener subscribe production events；不构建 isolated UIModule
> - **subscribe 必须在 `Awake()` 而非 `Start()`** per S5-1c lessons memo（防 sync-subscribe race；本 story 主要 framework boundary 非同步事件 path，但仍按惯例）
> - Mock minimal panel (`S5_08_MockMinimalPanel`) 作为 spike 专用 fixture；S5-02 dev-story 时被正式 minimal main menu panel 替代
> - Button.onClick.Invoke() 走 production UnityEngine.UI.Button 完整 path（非 mock listener handler call）

| # | Case | Setup | Action | Assert |
|---|---|---|---|---|
| **P1** | UIRootCanvasRuntimeInit | spike `Awake()` 阶段 reflection 拿 `GameModule.UI` accessor + verify non-null；scene root 内 search `UIRoot` Canvas GameObject reference | spike `Start()` 阶段 walk UIRoot 子节点结构 + 收集 5 layer container 名称 / sorting order base | `GameModule.UI != null` + UIRoot Canvas GameObject 已挂到 scene root + 5 layer container (`Background` / `HUD` / `Popup` / `Overlay` / `System`) 全部存在 + sorting order base = 各 `(int)layer * 100` (0/100/200/300/400) + Canvas `RenderMode` 与 ADR-011 决定一致 |
| **P2** | ShowMockPanelToUIRoot | post-P1（UIRoot ready）；spike `S5_08_MockMinimalPanel` 子类 already registered；spike subscribe panel `OnCreate` / `OnRefresh` lifecycle Debug.Log event | spike 调用 `GameModule.UI.ShowWindow<S5_08_MockMinimalPanel>()` | mock panel 实例化到 UIRoot 对应 layer container (e.g. HUD) 子节点 + GameObject active=true + sorting order base + (layer-internal order delta) 正确 + `OnCreate` Log 在 `OnRefresh` Log 之前同帧调用 (per SP-002 时序) |
| **P3** | UIWindowLifecycleOrder | post-P2（mock panel 已 visible）；spike 持续 listen 4 lifecycle method Debug.Log | spike Tick 等 1 帧 → 调用 `GameModule.UI.CloseWindow<S5_08_MockMinimalPanel>()` → 等 1 帧 → 再次 `ShowWindow<S5_08_MockMinimalPanel>()` | 完整 lifecycle 顺序 capture：first show: `OnCreate` → `OnRefresh`（同帧）→ `OnUpdate` (≥1 帧 while visible) → `OnClose`；second show: 仅 `OnRefresh` (无 OnCreate per SP-002) → `OnUpdate` → ... |
| **P4** | ButtonOnClickPath | post-P2（mock panel visible 含 UnityEngine.UI.Button 子组件 mounted in spike setup）；spike subscribe button.onClick + 加 1 个 listener handler 函数（`_clickCount++`）| spike `button.onClick.Invoke()` 模拟点击 3 次 | listener handler 调用 3 次 + `_clickCount == 3`（验证 S5-02 minimal main menu Button click path 同前置；Button.onClick.AddListener + Invoke API 通路 production verified）|

**evidence JSON schema** (`Application.persistentDataPath/S5-08_Result.json`):

```json
{
  "story_id": "S5-08",
  "timestamp": "2026-05-XX HH:MM:SS",
  "all_passed": true,
  "overall_status": "All Passed",
  "total_time_ms": 1500,
  "cases": [
    {"id": "P1", "passed": true, "duration_ms": 100, "asserts": ["GameModule.UI!=null", "UIRoot.GameObject!=null", "Background:0", "HUD:100", "Popup:200", "Overlay:300", "System:400", "RenderMode=ScreenSpaceOverlay"]},
    {"id": "P2", "passed": true, "duration_ms": 400, "events": ["OnCreate(MockMinimalPanel) frame=N", "OnRefresh(MockMinimalPanel) frame=N (same frame)"], "asserts": ["panel.parent=UIRoot/HUD", "panel.active=true", "sortingOrder>=100"]},
    {"id": "P3", "passed": true, "duration_ms": 600, "events": ["OnCreate frame=N", "OnRefresh frame=N", "OnUpdate frame=N+1", "OnClose frame=M", "OnRefresh frame=M+1 (no OnCreate per SP-002)", "OnUpdate frame=M+2"]},
    {"id": "P4", "passed": true, "duration_ms": 400, "asserts": ["clickCount=3 after 3 button.onClick.Invoke()"]}
  ]
}
```

**ADR-029 V2-5 listener self-removal pattern**: 本 story spike 走 Init / Dispose × 1 cycle（mock panel Show + Close + Show，verify lifecycle 顺序；非 5-cycle stress test —— V2-5 第 4 次实战累计已由 S5-1c 完成；本 story 重点 framework boundary 而非 listener self-removal probe）。

---

## QA Test Cases

*Integration type — automated PlayMode (R3) + grep-based static evidence*

- **AC-1 (UILayer 枚举) grep evidence**: 自动可验
  - `rg "enum UILayer" Assets/GameScripts/HotFix/GameLogic/UI/UILayer.cs` ≥ 1 hit + 5 enum value (Background/HUD/Popup/Overlay/System) ≥ 5 hit
  - `rg "GetSortingOrderBase" Assets/GameScripts/HotFix/GameLogic/UI/` ≥ 1 hit
- **AC-2..-5 (R3 PlayMode probe)**: spike 内 assert + JSON evidence
- **AC-6 (UIModule 程序集路径注释) grep evidence**: `rg "GameLogic/Module/UIModule" Assets/GameScripts/HotFix/GameLogic/UI/UILayer.cs` ≥ 1 hit
- **AC-7 (Out of Scope 明示) grep evidence**: `rg "story-008\|story-006\|Sprint 6 polish" production/epics/ui-system/story-001-uimodule-setup.md` ≥ 3 hits
- **AC-8 (Button onClick path) R3 PlayMode P4**: spike Tester `_clickCount == 3` assert
- **AC-9 (console clean)**: `read_console` filter level=Error / level=Warning；R3 path 0 unexpected (LogAssert.Expect 标记除外)
- **AC-10 (R3 ALL PASS)**: `cat ~/Library/Application\ Support/.../S5-08_Result.json | jq .all_passed == true`

> **legacy TC-001..-005 已删除**：原 EditMode TC-001 (UILayer Sorting Order 映射) 已由 R3 P1 case + AC-1 grep evidence 替代；原 TC-002..-005 (Popup Queue / Auto-Dequeue / Overlay limit / Auto InputBlocker) 全部移到 `story-008-ui-layer-strategy.md` cover (Sprint 6 polish)。

---

## Test Evidence

**Story Type**: Integration

**Required evidence**:

- **Spike**: `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-08_UIModuleSetup.cs` — **1 文件 + 3 内类**（`S508Spike : IDevSpike` + `S508Runtime : MonoBehaviour` + `S508Tester` 纯逻辑）；与 S5-1b/1c precedent 一致；M1 全 production reflection 模式
- **Mock panel fixture**: `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5_08_MockMinimalPanel.cs` — 仅 spike 用，DevTest namespace；S5-02 dev-story 实施时改写为正式 minimal main menu panel
- **JSON evidence**: `~/Library/Application Support/<company>/<product>/S5-08_Result.json`（4 case 全 PASS schema 见上 §R3）
- **QA evidence doc**: `production/qa/playmode-uimodule-setup-2026-05-XX.md` — 含
  - JSON evidence summary table（4 case 全 PASS）
  - Console snapshot (R3 path 0 unexpected error/warning；expected 项 LogAssert.Expect 标记)
  - `git diff --stat HEAD` 改动 evidence（预期文件：UILayer.cs + S5_08_MockMinimalPanel.cs + S5-08_UIModuleSetup.cs spike + GameApp.cs RegisterDevSpikes 切换 S51cSpike→S508Spike + 可能 UIRoot bootstrap impl）
  - 4 lifecycle event 顺序 dump（OnCreate / OnRefresh / OnUpdate / OnClose 完整时序）
  - AC matrix 10/10
  - Watch List Hooks（如出现 framework boundary drift / TEngine vendor gap / V3 candidate）
- **EditMode test**: 本 story Integration type；无强制 EditMode test 要求（Popup Queue / Auto InputBlocker 等 logic 留 story-008 写 EditMode test）

**Status**: Pending — 待 dev-story 实施。

---

## Dependencies

- **Depends on**:
  - **ADR-001 (Accepted)** TEngine Framework — UIModule 是 TEngine 核心模块
  - **ADR-011 (Accepted)** UIWindow Management — 层级和队列设计来源；本 story cover 5 层枚举 + UIWindow lifecycle；Popup queue/InputBlocker 留 story-008
  - **ADR-027 (Accepted)** Event Layer — UIWindow listener / GameModule.UI 协议
  - **ADR-029 V2.0 (Accepted)** R3 mandatory — Integration type 必须 PlayMode probe
  - **ADR-030 (Accepted)** §VS Build commitment — Sprint 5 [A] serial 序列
  - **SP-002** UIWindow Lifecycle — OnCreate / OnRefresh / OnUpdate / OnClose 时序
  - **S5-04 ✅ DONE 2026-05-11** — art-bible Accepted；VS art readiness gate ✅ open
- **Unlocks**:
  - **S5-02** Chapter 1 end-to-end happy path（hard prerequisite — main menu 2 minimal inline Button 需本 story UIRoot Canvas + ShowWindow API 通路）
  - **story-008** UI Layer Strategy（Sprint 6 polish — 依赖本 story 的 UIRoot + GameModule.UI 通路）
  - **story-006 / -002..-007** 各 UIWindow 业务面板（Sprint 6+ — 依赖本 story 的 UIRoot + UIWindow base class lifecycle 文档）

---

## Assumptions Validated (R2 — TBD /story-readiness gate phase)

R2 grep verify 待 `/story-readiness` gate 实证。**5 大块 TEngine vendor wiring uncertainty 必须 R2 阶段 grep + 调研实证**（不实证不写 dev-story）：

| # | 假设 | 实测 grep / 调研 evidence (待) | 结果 |
|---|------|----------------------|------|
| **R2.1** | `GameModule.UI` static accessor 已在项目内暴露且 non-null | `rg "GameModule\.UI\b" Assets/GameScripts/HotFix/` ≥ 1 hit + check `GameModule` 类定义 | TBD |
| **R2.2** | `UIModule.ShowWindow<T>()` / `CloseWindow<T>()` API signature TEngine vendor exposed | `rg "ShowWindow<\|CloseWindow<" Assets/GameScripts/HotFix/GameLogic/Module/UIModule/` ≥ 1 hit 或 SemanticSearch `repowiki/zh/content/` UIModule 章节 | TBD |
| **R2.3** | `UIWindow` abstract base class 路径 + 4 lifecycle method (`OnCreate`/`OnRefresh`/`OnUpdate`/`OnClose`) 已 exposed by TEngine | SemanticSearch `repowiki/zh/content/` UIWindow lifecycle 章节 + `rg "abstract class UIWindow\|class UIWindow " Assets/` | TBD |
| **R2.4** | UIRoot Canvas runtime 实例化路径与时机（GameApp.Entrance 启动序列哪一步）+ 5 layer container 是否自动创建 or 需 production code 显式创建 | SemanticSearch `repowiki/zh/content/` UIRoot / Init Order 章节 + 看 `GameApp.cs` 现有启动序列 | TBD |
| **R2.5** | spike 模式 `Awake()` subscribe 协议（per S5-1c lessons memo） | S5-1c spike `S5-1c_ListenerPathDriver.cs` PlayMode 5/5 PASS first-run verified | ✅ S5-1c precedent |
| **R2.6** | M1 dual-layer pattern 复用 (production reflection 全程) per S5-1c/S5-02 precedent | S5-1c spike PlayMode 5/5 PASS + S5-02 story-002 R3 design 复用 | ✅ S5-1c precedent |
| **R2.7** | Spike "1 文件 + 3 内类" 惯例 | Glob `Spikes/S*.cs` 全部命中（S301..S5-1c 8 个 spike 全部一致）| ✅ |

**R2 PASS 路径**: 7/7 ✅ → 写 dev-story；任意 R2 0-hit → DEFICIENCY-FLAGGED PASS 路径（ADR-029 V2.0 deficiency-flag 协议）—— dev-story 阶段先补 wiring（如 GameModule.UI 暴露 / UIRoot bootstrap 创建 / UIWindow base class 引入等）然后跑 R3。

**4 大 TEngine vendor wiring uncertainty (R2.1~R2.4) 是本 story 主要 R2 工作量**；如发现 TEngine vendor API gap → `tengine-dev` skill R1~R4 vendor patch 协议。

---

## S5-02 Coordination

本 story 是 **S5-02 Chapter 1 end-to-end happy path** 的 **hard prerequisite**（per 2026-05-11 Sprint 5 [A] serial 序列决策 + S5-02 main menu UI [A] minimal_inline 决策）：

| 本 story 产出 | S5-02 消费 |
|---|---|
| **AC-2**: UIRoot Canvas runtime 实例化 + 5 layer container | S5-02 main menu base canvas 挂到 UIRoot/HUD 层 |
| **AC-3**: `GameModule.UI` 静态门面通路 | S5-02 main menu Button onClick handler 调用 `GameModule.UI.ShowWindow / CloseWindow` API |
| **AC-4**: `ShowWindow<T>()` / `CloseWindow<T>()` API 通路 + Mock panel 实例化 verified | S5-02 minimal main menu UIWindow（正式 panel 子类替代本 story mock fixture）通过同 API 实例化 |
| **AC-5**: UIWindow 4 lifecycle method 文档注释（OnCreate / OnRefresh / OnUpdate / OnClose）| S5-02 minimal main menu panel 子类按本 story 文档时序实现 lifecycle method |
| **AC-8**: Button.onClick.AddListener + Invoke API 通路 verified | S5-02 main menu 'Start Chapter 1' Button onClick → `ISceneEvent.OnRequestSceneChange(1)` dispatch；'Next Chapter' Button onClick → `OnRequestSceneChange(0)` dispatch（详 `story-002-end-to-end-flow.md` P1 + P5）|

S5-02 dev-story 内 spike (`S5-02_EndToEndFlow.cs`) 模拟点击 Button 走 production path：本 story 的 mock fixture (`S5_08_MockMinimalPanel`) DevTest namespace 不入 S5-02 path；**S5-02 dev-story 内基于本 story API 通路重写为正式 minimal main menu panel** 含 2 个 Button。

完整 main menu UIWindow（New Game / Continue / Settings 按钮 + 存档检查 + fade-in + BGM）见 `story-006-main-menu.md` Sprint 6 polish。

---

## ADR-029 V3 Watch List Hooks

本 story R3 实施过程中如出现以下情况应 capture 为新 drift type 候选并写入 sprint-status.yaml watch list：

1. **Type-2(c) candidate**: UIWindow 4 lifecycle method 调用顺序与 SP-002 实证不符（e.g. `OnCreate` 不在 `OnRefresh` 之前同帧 / `OnUpdate` 在 invisible 时仍触发 / 第二次 `ShowWindow` 触发 `OnCreate`）→ framework boundary behavior assumption drift；累计 dp 数据点
2. **Type-5 candidate (S5-01 dp1 promote 候选累计)**: TEngine vendor API gap discovered during R2 readiness gate（如 `GameModule.UI` 暴露 incomplete / `UIRoot.SetUIRoot` API 不存在 / `UIWindow` base class 路径与文档不符）→ 累计 V3 #2 dp 数据点；走 `tengine-dev` skill R1~R4 vendor patch 协议
3. **Type-6 candidate (S5-1c lessons memo promote 候选累计)**: spike subscribe race 如再次出现（UIModule init 同步 fire event 路径或类似）→ 累计 V3 #6 dp 数据点；本 story spike Awake() subscribe 已采纳防御
4. **Type-7 candidate (新)**: UIRoot Canvas runtime 实例化时机与 GameApp.Entrance 启动序列其他步骤 race condition（如 UIRoot 在 SceneManager init 之前 vs 之后；UIRoot 在 AudioModule init 之前 vs 之后）→ 累计 V3 candidate `boot-order drift` dp 数据点

如出现以上任一，per ADR-029 V2.0 §V2-7：sprint-status.yaml `watch_list` triggers 内追加 drift type 描述 + 关联 story-001 R3 case 编号 + 沉淀 problem memo 到 `.claude/memory/`。

---

## History

### 2026-05-11 — Sprint 5 narrow scope amendment + R3 mandatory addition

**Trigger**: Sprint 5 [A] serial 序列第 2 步（2026-05-11 user 决策）；S5-08 promote should-have → must-have；S5-02 Chapter 1 end-to-end happy path [A] minimal_inline 决策要求最小 UIModule 通路足够支撑 main menu 2 minimal inline Button。

**Original Status (Sprint 3/4)**:
- Story Type: Logic / 9 AC (含 Popup Queue + Auto-Dequeue + Auto InputBlocker + Overlay limit + 单元测试) / 5 EditMode TC / Test Evidence: `tests/unit/UIModule_PopupQueue_Test.cs` / Status: Ready (Sprint: TBD)

**Amendments**:

1. **Header**: Status `Ready` → `Draft` (待 amend 通过 /story-readiness gate 再 → Ready) / Story Type `Logic` → **`Integration`** (Framework Boundary R3 mandatory per ADR-029 V2.0) / Sprint `TBD` → **`Sprint 5`** / 加 `Manifest Version: 2026-05-11`
2. **AC narrow scope**: 删 AC-2..-6 (Popup Queue Manager / Auto-Dequeue / Auto InputBlocker / token 命名规范 / Overlay limit) + AC-9 (单元测试)；保留 AC-1 (UILayer 枚举) / AC-7 (UIWindow lifecycle 注释) / AC-8 (UIModule 路径注释)
3. **AC additions**: AC-1 加 helper static method / AC-2 UIRoot Canvas runtime 实例化 / AC-3 GameModule.UI 静态门面通路 / AC-4 ShowWindow/CloseWindow API 通路 + Mock panel / AC-5 UIWindow lifecycle 文档注释 / AC-6 UIModule 程序集路径注释 / AC-7 Out of Scope 明示 / AC-8 Button onClick path / AC-9 console clean / AC-10 R3 ALL PASS
4. **R3 PlayMode probe 新增**: 4 case (P1 UIRootCanvasRuntimeInit / P2 ShowMockPanelToUIRoot / P3 UIWindowLifecycleOrder / P4 ButtonOnClickPath) + M1 dual-layer 全 production reflection 复用 S5-1c/S5-02 precedent + spike `S5-08_UIModuleSetup.cs` 1 file + 3 inner class 项目惯例
5. **Test Evidence**: EditMode-only → PlayMode JSON + qa doc + grep evidence 多层；mock fixture `S5_08_MockMinimalPanel.cs` (DevTest namespace) S5-02 时被正式 minimal main menu panel 替代
6. **Out of Scope 明示**: Popup Queue / Auto InputBlocker / 完整 UIWindow 业务面板 → 已存在 stories 全部明示 (story-008 / -006 / -002..-007/009/010)；不新建 ui-system-001b（story-008 已 cover 同 scope）
7. **Dependencies + S5-02 Coordination + V3 Watch List Hooks 新增**

**Rationale**:
- Sprint 5 [A] serial 时间预算限制：S5-08 dev-story estimation 2 SP ≈ 3-3.5h workshift；原 9 AC + R3 跑全 ≥ 5h，over-spec
- 已有 story-008 cover Popup Queue/InputBlocker robust UI infrastructure（Sprint 6 polish）；narrow 不重复造轮子
- S5-02 minimal_inline 决策只需 minimal UIRoot + ShowWindow API + Button mount path —— 本 story narrow scope 精准对应
- ADR-029 V2.0 R3 mandatory：Framework Boundary type 必须 PlayMode probe；原 Logic + EditMode-only 不满足

**Sign-off**: User 决策落盘 (选项 [D'] 2026-05-11 narrow scope + R3 + 不拆新 story)

**Audit Trail Cross-references**:
- Sprint 5 plan: `production/sprints/sprint-5.md` §Must Have Track D — S5-08 (promoted)
- Sprint status: `production/sprint-status.yaml` story id `S5-08`
- ADR governance: `docs/architecture/adr-030-project-workflow-vs-late-pattern.md` §VS Build commitment
- Neighboring stories (cover deferred scope):
  - `production/epics/ui-system/story-008-ui-layer-strategy.md` (Sprint 6 polish — Popup Queue / Auto InputBlocker / Overlay limit 完整 robust UI infrastructure cover)
  - `production/epics/ui-system/story-006-main-menu.md` (Sprint 6 polish — full main menu UIWindow cover)
- S5-1c lessons memo: `.claude/memory/problem_2026-05-09_spike-sync-subscribe-race.md` (spike Awake() subscribe 协议)
- S5-02 hard prerequisite contract: `production/epics/vs-chapter-1/story-002-end-to-end-flow.md` §Dependencies (S5-08 hard prerequisite)






