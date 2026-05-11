// 该文件由Cursor 自动生成

# Story: UIModule Initialization + UIWindow Base Class Setup (Sprint 5 narrow scope amendment)

> **Epic**: ui-system
> **Story ID**: ui-system-001
> **Story Type**: **Integration** *(2026-05-11 amend: 原 Logic → Integration；Framework Boundary R3 mandatory per ADR-029 V2.0)*
> **GDD Requirement**: TR-ui-001 (All UI via TEngine UIModule), TR-ui-002 (5 UI layer levels — UILayer 枚举部分；Popup Queue/Auto InputBlocker 完整行为 → story-008 cover)
> **ADR References**: ADR-011 (UIWindow Management), ADR-001 (TEngine Framework), SP-002 (UIWindow Lifecycle), ADR-029 V2.0 (R3 mandatory), ADR-027 (Event Layer)
> **Sprint**: **Sprint 5** *(2026-05-11 promote should-have → must-have per Sprint 5 [A] serial 序列：S5-04 ✅ → S5-08 → S5-02 → S5-07；详 §History)*
> **Status**: **Done** *(2026-05-11 Session 26 #6 — dev-story Phase 2~4 完成：4/4 R3 case PASS first-run + 29/29 asserts + all_passed=true + JSON evidence + V3 Type-8 dp1 NEW 实战触发；evidence: `production/qa/playmode-uimodule-setup-2026-05-11.md`)*
> **Manifest Version**: 2026-05-11

## Context

**Trigger**:

Sprint 5 [A] serial 序列第 2 步（2026-05-11 user 决策）；S5-04 art-bible sign-off ✅ DONE 2026-05-11 unlocks 本 story；本 story DONE 后 unblocks S5-02 Chapter 1 end-to-end dev-story。S5-08 carryover hard rule 满足（S3-08 → S4-09 → Sprint 5 promote must-have）。

**Goal (Sprint 5 narrow scope amendment)**:

建立 UIModule 运行时基础 —— **UIRoot scene 实例化 verify + UIModule.OnInit auto-wire 路径 + GameModule.UI.ShowUI/CloseUI/HideUI API 通路 verified + UILayer 5 层枚举 + UIWindow vendor lifecycle 文档注释 (7+2 method)**。本 story DONE 后，S5-02 minimal main menu（base canvas + 'Start Chapter 1' Button + 'Next Chapter' Button minimal inline）有 production API 可挂。

UIModule 本身由 TEngine 提供，位于 `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/`（GameLogic 热更程序集，非 TEngine Runtime）。本 story **不实现任何具体面板**（main menu / pause menu / settings 等），只建立 UIRoot scene 实例化 verify + ShowUI/CloseUI/HideUI API + UIModule.OnInit auto-wire 通路。

**Scope (2026-05-11 narrow amendment per S5-02 minimal_inline 决策)**:

- ✅ **In Scope**: UILayer 枚举（5 层 sorting order base） / UIRoot scene 实例化 verify (TEngine vendor 提供 prefab + main.unity 已实例化；本 story 仅 verify auto-wire) / `GameModule.UI.ShowUI/CloseUI/HideUI<T>()` API 通路 verify (per R2.2 vendor wording) / 1 个 mock minimal panel 实例化到 UIRoot 验证 framework wiring / UIWindow vendor 完整 7+2 lifecycle (ScriptGenerator → BindMemberProperty → RegisterEvent → OnCreate → OnRefresh → OnUpdate × N → OnDestroy + Hide/Close) 文档注释 / UIModule 程序集路径注释
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
- **SP-002** UIWindow Lifecycle (historical spec; **R2.3 实证 drift**) — spec 描述 OnCreate (首次) / OnRefresh (每次打开) / OnUpdate (可见帧) / OnClose (清理) 4 method，**vendor 实际 7+2 method** (ScriptGenerator → BindMemberProperty → RegisterEvent → OnCreate → OnRefresh → OnUpdate × N → OnDestroy + Hide/Close hook；**OnDestroy ≠ OnClose** wording drift)；本 story 跟 vendor wording
- **ADR-029 V2.0 (Accepted) R3 mandatory** — Integration type 必须 PlayMode probe
- **ADR-027 (Accepted)** Event Layer — UIModule + GameEvent.Get<IEvent>() 协议；V2-5 listener self-removal pattern
- **ADR-030 (Accepted)** §VS Build commitment 第 1 项 — Sprint 5 [A] serial 序列 S5-08 hard prerequisite for S5-02

**Engine Notes** *(R2 grep + SemanticSearch 实证 2026-05-11；R2.2 + R2.3 wording drift discovered → 本 story amend wording 对齐 vendor)*:

- `GameModule.UI` static accessor ✅ R2.1 — TEngine UIModule 是 `Singleton<UIModule>` (UIModule.cs:15)；`GameModule.UI` 通过 TEngine framework 暴露 (per GameLobbyState.cs:18 实际用法 comment)
- **R2.2 ⚠️ DRIFT** — API wording: ADR-011 spec / SP-002 用 `ShowWindow<T>()` / `CloseWindow<T>()`，**TEngine vendor 实际 API 是 `ShowUI<T>()` / `CloseUI<T>()` / `HideUI<T>()`**（含 `ShowUIAsync<T>()` 异步 + `ShowUIAsyncAwait<T>()` await 版；签名 `where T : UIWindow, new()` + `params object[] userDatas`）。本 story 跟 **vendor wording** (ShowUI/CloseUI/HideUI)；ADR-011/SP-002 wording 残留留 Sprint 5 retro 评估系统性 amend (per V3 Type-5 dp)
- **R2.3 ⚠️ PARTIAL DRIFT** — Lifecycle methods: ADR-011 spec / SP-002 用 `OnCreate / OnRefresh / OnUpdate / OnClose` 4 method，**TEngine vendor 实际 7 method**:
  1. `ScriptGenerator()` — 代码自动生成绑定 (UIBase.cs:144)
  2. `BindMemberProperty()` — 绑定 UI 成员元素 (UIBase.cs:151)
  3. `RegisterEvent()` — 注册事件 (UIBase.cs:158)
  4. `OnCreate()` — 窗口创建 (UIBase.cs:165)
  5. `OnRefresh()` — 窗口刷新 (UIBase.cs:172)
  6. `OnUpdate()` — 窗口更新（含 `_hasOverrideUpdate` 优化标记） (UIBase.cs:184)
  7. `OnDestroy()` — 窗口销毁（**注意**: vendor 用 `OnDestroy` 而非 ADR-011/SP-002 的 `OnClose`） (UIBase.cs:197)

  + `UIWindow` extends `UIBase` 加 2 hook: `Hide()` (UIWindow.cs:504) + `Close()` (UIWindow.cs:509)
- **R2.4 ✅** UIRoot Canvas — TEngine vendor 已提供 `Assets/TEngine/Settings/Prefab/UIRoot.prefab` + main.unity scene 内已实例化 UIRoot GameObject (含 Canvas + UICamera)；`UIModule.OnInit()` (UIModule.cs:49) 自动 `GameObject.Find("UIRoot")` → 拿 Canvas transform → 拿 UI Camera → `DontDestroyOnLoad` 持久化 → 设 UI layer。**S5-08 dev-story 不需要 production code 创建 UIRoot**（已存在），spike 仅 verify scene 已 instantiate + UIModule.OnInit() 正确 wire
- `UIWindow` abstract base class ✅ R2.3 — `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIWindow.cs` namespace `GameLogic`；继承 `UIBase`（同 namespace）
- **R2.10 ⚠️ DRIFT (新发现 Session 26 #5)** — `UILayer` enum vendor 已存在！`Assets/GameScripts/HotFix/GameLogic/Module/UIModule/WindowAttribute.cs:8` `public enum UILayer : int { Bottom=0, UI=1, Top=2, Tips=3, System=4 }`（namespace `GameLogic`）。vendor 实际命名 vs ADR-011 spec 命名第 3 例 wording drift：
  - vendor `Bottom = 0` ≈ ADR-011 spec `Background` (底层背景)
  - vendor `UI = 1` ≈ ADR-011 spec `HUD` (主 UI 层)
  - vendor `Top = 2` ≈ ADR-011 spec `Popup` (顶层/弹窗)
  - vendor `Tips = 3` ≈ ADR-011 spec `Overlay` (Tips/Toast/Overlay)
  - vendor `System = 4` = ADR-011 spec `System` (系统层同名)

  **vendor `[Window]` attribute (WindowAttribute.cs) 已使用 `UILayer` 作参数类型**；vendor LogUI.cs:8 实际用法 `[Window(UILayer.System, fromResources: true)]`。本 story 跟 **vendor wording** (Bottom/UI/Top/Tips/System)，不新建 collision enum；ADR-011 spec wording amend 留 Sprint 5 retro 评估 (累计 V3 Type-5 candidate dp 第 3 例)
- `WindowAttribute` ✅ R2.11 — vendor 已 4 ctor overload 支持 `UILayer windowLayer` + optional location/fromResources/fullScreen/hideTimeToClose 参数 (WindowAttribute.cs:45-76)；mock panel 可用 `[Window(UILayer.UI, fromResources: true)]` 类似 vendor LogUI.cs:8 用法 wire

**Performance**: R3 mandatory Integration type；S5-08 dev-story 总 workshift ≤ 3.5h（estimation 2 SP）。

**Control Manifest Rules (this layer)**:

- **Required**: UIRoot Canvas 必须在 GameApp 启动序列中实例化（per ADR-009 boot order；具体步骤号待 R2 verify 与 TEngine 16-step Init Order 对齐）
- **Required**: 所有 UIWindow / UIWidget 访问通过 `GameModule.UI` 静态门面，**禁止** `ModuleSystem.GetModule<UIModule>()`（per `tengine-dev` skill L0 protocol + ADR-001）
- **Required**: UIWindow 子类至少 OnCreate / OnRefresh / OnUpdate / OnDestroy 4 user-facing lifecycle method 必须含 `<summary>` 文档注释说明调用时序（per R2.3 vendor 实证：完整 7+2 method，**OnDestroy 而非 OnClose**）
- **Required**: UIModule 所在程序集路径必须在代码注释中标注：`Assets/GameScripts/HotFix/GameLogic/Module/UIModule/`（防止团队错误地在 TEngine Runtime 查找）
- **Forbidden**: 不实施 Popup Queue Manager / Auto-Dequeue / Auto InputBlocker（留 story-008 Sprint 6 polish）
- **Forbidden**: 不实施任何具体 UIWindow 业务面板（main menu / pause menu / settings 等留 story-002..-007 Sprint 6+）
- **Forbidden**: 不修改 TEngine 核心代码（UIModule core 是 vendor 范畴；如发现 vendor bug 走 `tengine-dev` skill R1~R4 vendor patch 协议）

---

## Acceptance Criteria

*Integration type — Framework boundary + R3 PlayMode probe MANDATORY (ADR-029 V2.0)*

- [ ] **AC-1 (UILayer enum verify + helper extension method)**: vendor `UILayer` enum 已在 `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/WindowAttribute.cs:8` 提供 5 值 `{Bottom=0, UI=1, Top=2, Tips=3, System=4}` (namespace `GameLogic`)，**本 story 不新建 UILayer.cs**（per R2.10 collision discovery）；改为创建 `Assets/GameScripts/HotFix/GameLogic/UI/UILayerExtensions.cs` 含 1 static extension method `GetSortingOrderBase(this UILayer layer)` 返回 `(int)layer * 100`（每层 sorting order base = layer × 100）；XML doc 注释明示 vendor enum 路径 + ADR-011 spec wording 映射 (Bottom≈Background / UI≈HUD / Top≈Popup / Tips≈Overlay / System=System)
- [ ] **AC-2 (UIRoot scene 实例化 verify + UIModule.OnInit wire 路径)**: scene 内 `UIRoot` GameObject 已 instantiate (per TEngine vendor `Assets/TEngine/Settings/Prefab/UIRoot.prefab` 引用；main.unity scene 已含 `R2.4 ✅`)；spike 通过 reflection 拿 `UIModule.UIRoot` static property (UIModule.cs:36) verify non-null + `_instanceRoot.gameObject.layer == LayerMask.NameToLayer("UI")` + 父节点 `DontDestroyOnLoad` (UIModule.cs:65)。**不需要 production code 创建 UIRoot Canvas** — 已存在，本 AC 仅 verify 自动 wire 路径
- [ ] **AC-3 (GameModule.UI 静态门面通路)**: `GameModule.UI` 已暴露且 non-null（不抛 NullReferenceException）；spike 通过 reflection 或直接调用 `GameModule.UI` accessor 拿到 UIModule instance；该 instance 可调用 **`ShowUI<T>()`** / **`CloseUI<T>()`** / **`HideUI<T>()`** API（vendor 实际 wording per R2.2；签名 `where T : UIWindow, new()` + `params object[] userDatas`）
- [ ] **AC-4 (UIWindow ShowUI/CloseUI API 通路 verify)**: 创建 1 个 mock minimal UIWindow 子类 `S5_08_MockMinimalPanel.cs`（**仅本 story spike 用**，DevTest 命名空间，不进入 GameLogic.UI production 路径）继承 `GameLogic.UIWindow` base class；spike `GameModule.UI.ShowUI<S5_08_MockMinimalPanel>()` 后该 panel 实例化到 UIRoot 子节点 + active=true + `UIModule._uiStack` 内含此 panel；`GameModule.UI.CloseUI<S5_08_MockMinimalPanel>()` 后 panel inactive / removed from stack
- [ ] **AC-5 (UIWindow lifecycle 文档注释 — TEngine vendor 完整 7+2 lifecycle)**: `S5_08_MockMinimalPanel` 实现至少 4 个 user-facing lifecycle method (`OnCreate` / `OnRefresh` / `OnUpdate` / `OnDestroy`) **+** 文档注释明示完整 TEngine vendor lifecycle 链路 (per R2.3):
  - **Init phase (3)**: `ScriptGenerator → BindMemberProperty → RegisterEvent` (vendor 自动调用，本 story mock 可不 override)
  - **Lifecycle phase (4)**: `OnCreate` (首次创建) → `OnRefresh` (每次显示) → `OnUpdate × N` (visible 每帧；含 `_hasOverrideUpdate` 优化) → `OnDestroy` (销毁；**注意**: vendor 用 OnDestroy 而非 ADR-011/SP-002 wording OnClose)
  - **UIWindow 额外 2 hook**: `Hide` (UIWindow.cs:504 隐藏不销毁) / `Close` (UIWindow.cs:509)

  mock panel 每个 lifecycle method 内 `Debug.Log` 记录调用顺序便于 R3 PlayMode verify
- [ ] **AC-6 (UIModule 程序集路径注释)**: `UILayerExtensions.cs` 顶部含代码注释明示 (a) UIModule 所在程序集路径：`Assets/GameScripts/HotFix/GameLogic/Module/UIModule/`（GameLogic 热更程序集，非 TEngine Runtime） + (b) `UILayer` enum vendor 路径：`Assets/GameScripts/HotFix/GameLogic/Module/UIModule/WindowAttribute.cs:8` (defined alongside `WindowAttribute` class)；防止团队错误地在 TEngine Runtime 中查找 / 新建 collision enum
- [ ] **AC-7 (Out of Scope 明示)**: story file §Out of Scope 段明示 Popup Queue / Auto InputBlocker / 完整 UIWindow 业务面板 全部由 story-008 / story-002..-007 cover；本 story 不实施
- [ ] **AC-8 (S5-02 main menu Button mount API verified)**: spike 内验证 `S5_08_MockMinimalPanel` 上可挂 UnityEngine.UI.Button 子组件 + `Button.onClick.AddListener` API 通路；spike 模拟 `Button.onClick.Invoke()` 后 listener handler 被调用（S5-02 minimal main menu 'Start Chapter 1' + 'Next Chapter' Button click path 同前置）
- [ ] **AC-9 (console clean)**: R3 PlayMode probe 全程 0 unexpected error / 0 unexpected warning（spike 用 `LogAssert.Expect` 主动标记 expected 项；如无 expected error/warning 则 0/0 实测）
- [ ] **AC-10 (R3 PlayMode probe ALL PASS)**: spike `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-08_UIModuleSetup.cs` 4 R3 case 全 PASS + JSON evidence `~/Library/Application Support/.../S5-08_Result.json` `all_passed=true` + `production/qa/playmode-uimodule-setup-2026-05-XX.md` evidence doc 写完

---

## Implementation Notes

- **UIModule 路径**：`Assets/GameScripts/HotFix/GameLogic/Module/UIModule/`（GameLogic 热更程序集；**禁止** `ModuleSystem.GetModule<UIModule>()`，所有访问通过 `GameModule.UI`）
- **UIRoot Canvas (R2.4 ✅ 实证)**：
  - TEngine vendor 已提供 prefab `Assets/TEngine/Settings/Prefab/UIRoot.prefab` (含 Canvas + UI Camera)
  - main scene (`Assets/Scenes/main.unity`) 已实例化 UIRoot GameObject (prefab 引用)
  - `UIModule.OnInit()` 自动 `GameObject.Find("UIRoot")` (UIModule.cs:51) → 拿 Canvas transform → 拿 UI Camera → `DontDestroyOnLoad` 持久化 → 设 UI layer
  - **S5-08 dev-story 不需要 production code 创建 UIRoot**（已存在）；spike 仅 verify scene 已 instantiate + UIModule.OnInit() 自动 wire 路径
  - chapter scene (`Chapter_01_Approach.unity`) 是否需要 UIRoot prefab 引用？— R2.5 gate 决定（推测：UIRoot DontDestroyOnLoad 后跨 scene 持久化，chapter scene 不需要单独 reference）
- **UILayer enum (R2.10 ✅ 实证)**：vendor 已存在！`Assets/GameScripts/HotFix/GameLogic/Module/UIModule/WindowAttribute.cs:8` `public enum UILayer : int { Bottom=0, UI=1, Top=2, Tips=3, System=4 }` (namespace `GameLogic`)；本 story **不新建** `UILayer.cs` (避免 type collision per R2.10)；改为创建 `Assets/GameScripts/HotFix/GameLogic/UI/UILayerExtensions.cs` (~30 行) 含 1 static extension method `public static int GetSortingOrderBase(this UILayer layer) => (int)layer * 100;` + XML doc 注释 vendor enum 路径 + ADR-011 spec wording 映射表
- **`[Window]` attribute (R2.11 ✅ 实证)**：vendor 已提供 `WindowAttribute` (WindowAttribute.cs:21) + 4 ctor overload；mock panel 用法 `[Window(UILayer.UI, fromResources: true)]` 沿 vendor LogUI.cs:8 precedent
- **Mock minimal panel 路径**：`Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5_08_MockMinimalPanel.cs`（DevTest namespace；继承 `GameLogic.UIWindow` base class；含 `[Window(UILayer.UI, fromResources: true)]` attribute (per vendor LogUI.cs:8 precedent + R2.11 实证)；本 story spike 专用，不入 production UI 路径；S5-02 dev-story 实施时改写为正式 minimal main menu panel）
- **Spike 路径**：`Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-08_UIModuleSetup.cs`（1 file + 3 inner class per S5-1b/1c precedent：`S508Spike : IDevSpike` + `S508Runtime : MonoBehaviour` + `S508Tester` 纯逻辑）
- **GameApp.cs 改动**：`RegisterDevSpikes` 切换 `S51cSpike` → `S508Spike`（已是项目惯例 per S5-1c precedent）；UIModule 由 TEngine framework 自动 init (Singleton<UIModule>)，本 story **不需要** GameApp 内 `_uiModule` field 或 Init/Dispose 显式调用（TEngine 自动）
- **TEngine vendor API wording (per R2.2 实证)**:
  - 显示 panel: `GameModule.UI.ShowUI<T>(params object[] userDatas)` (sync, immediate) 或 `ShowUIAsync<T>(...)` (异步) 或 `ShowUIAsyncAwait<T>(...)` (UniTask awaitable)
  - 关闭 panel: `GameModule.UI.CloseUI<T>()`
  - 隐藏 panel (不销毁): `GameModule.UI.HideUI<T>()`
  - 全部关闭: `GameModule.UI.CloseAll(bool isShutDown = false)`
  - **不使用** `ShowWindow / CloseWindow` wording (ADR-011 spec 残留，vendor 不存在该 API)
- **TEngine vendor lifecycle (per R2.3 实证)**:
  - Init phase: `ScriptGenerator → BindMemberProperty → RegisterEvent` (vendor 自动调用, mock panel 可不 override)
  - Lifecycle phase: `OnCreate → OnRefresh → OnUpdate × N → OnDestroy` (4 user-facing method；**注意 OnDestroy ≠ OnClose** wording drift)
  - UIWindow 额外: `Hide` / `Close` 2 hook
- **Listener 模式 (per S5-1c lessons memo)**：如本 story spike 需 subscribe 任何 TEngine event，**subscribe 必须在 `Awake()` 而非 `Start()`**（per `problem_2026-05-09_spike-sync-subscribe-race.md`）；本 story 主要 framework boundary 不涉同步事件 race，但仍按惯例执行
- **TEngine vendor 调研已完成 R2 阶段**：UIModule / UIWindow / UIBase 路径与 lifecycle / API 签名 grep + 静态分析实证；如 dev-story 实施时发现新 vendor API gap → 走 `tengine-dev` skill R1~R4 vendor patch 协议
- **TEngine vendor 不修改**：本 story 不修改 `Assets/TEngine/` 任何文件；ADR-011/SP-002 spec wording drift 留 Sprint 5 retro 评估系统性 amend (per V3 Type-5 dp)

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

按 ADR-029 V2.0 R3 mandatory criterion，本 story 是**典型的 Framework Boundary Integration story** —— TEngine UIModule + UIRoot scene 实例化 + UIModule.OnInit auto-wire + GameModule.UI static accessor + UIWindow vendor 7+2 lifecycle + Mock panel `ShowUI/CloseUI/HideUI` API 通路 + Button onClick path verify。framework boundary probe ≥ 4 处，必须 PlayMode 实证（EditMode 不能验：UIModule.OnInit() 自动 wire UIRoot/Canvas/UICamera success / `GameModule.UI.ShowUI<T>()` / `CloseUI<T>()` API 是否真把 panel 实例化到 canvas + 加 to _uiStack / UIWindow vendor 7 lifecycle (ScriptGenerator → BindMemberProperty → RegisterEvent → OnCreate → OnRefresh → OnUpdate × N → OnDestroy) 在真实 runtime 的调用时序 / Button.onClick.Invoke() 是否真触发 listener）。

### R3 PlayMode probe 4 cases（spike `S5-08_UIModuleSetup.cs` — **(M1) production reflection 全程**复用 S5-1c/S5-02 precedent）

> **Spike 模式**:
> - 全程**复用 production**：`GameApp._uiModule` 或 `GameModule.UI` reflection 拿 instance；listener subscribe production events；不构建 isolated UIModule
> - **subscribe 必须在 `Awake()` 而非 `Start()`** per S5-1c lessons memo（防 sync-subscribe race；本 story 主要 framework boundary 非同步事件 path，但仍按惯例）
> - Mock minimal panel (`S5_08_MockMinimalPanel`) 作为 spike 专用 fixture；S5-02 dev-story 时被正式 minimal main menu panel 替代
> - Button.onClick.Invoke() 走 production UnityEngine.UI.Button 完整 path（非 mock listener handler call）

| # | Case | Setup | Action | Assert |
|---|---|---|---|---|
| **P1** | UIRootSceneInstantiateVerify + UILayerExtensions sanity | spike `Awake()` 阶段 reflection 拿 `GameModule.UI` accessor + verify non-null；scene root 内 `GameObject.Find("UIRoot")` 拿 UIRoot GameObject reference | spike `Start()` 阶段 reflection 拿 `UIModule.UIRoot` static property (UIModule.cs:36) + walk UIRoot/Canvas 子节点结构 + verify UIModule.OnInit wire 路径完成；call `UILayer.UI.GetSortingOrderBase()` + 4 other layers 验 extension method | `GameModule.UI != null` + `GameModule.UI is Singleton<UIModule>` + `UIModule.UIRoot != null` (Transform) + UIRoot GameObject layer == LayerMask.NameToLayer("UI") + 父节点已 `DontDestroyOnLoad` + UICamera != null + `UILayer.Bottom.GetSortingOrderBase() == 0` + `UI == 100` + `Top == 200` + `Tips == 300` + `System == 400`（注：vendor `LAYER_DEEP=2000` / `WINDOW_DEEP=100` 是堆栈深度限制 const，与 UILayer 枚举 sorting order base × 100 不冲突）|
| **P2** | ShowMockPanelViaShowUI | post-P1（UIRoot ready）；`S5_08_MockMinimalPanel` (继承 GameLogic.UIWindow) 含 `[Window(UILayer.UI, fromResources: true)]` attribute (per R2.11 实证 + LogUI.cs:8 precedent；fromResources=true 让 vendor 走 `Resources.Load` path 不需 YooAsset bundle wire；mock prefab 路径走 `[Window]` location 参数或 vendor 默认 lookup convention)；spike subscribe panel lifecycle Debug.Log event | spike 调用 **`GameModule.UI.ShowUI<S5_08_MockMinimalPanel>()`** (sync immediate) 或 `ShowUIAsyncAwait<S5_08_MockMinimalPanel>()` await 版（per R2.2 vendor wording）| mock panel 实例化到 UIRoot 子节点 + GameObject active=true + `UIModule._uiStack` (UIModule.cs:21, 通过 reflection) 内含此 panel instance + Init phase Log (`ScriptGenerator → BindMemberProperty → RegisterEvent`) 在 `OnCreate` 之前同帧调用 + `OnRefresh` Log 在 `OnCreate` Log 之后同帧 |
| **P3** | UIWindowLifecycleVendorOrder | post-P2（mock panel 已 visible）；spike 持续 listen vendor 7 lifecycle method Debug.Log (ScriptGenerator / BindMemberProperty / RegisterEvent / OnCreate / OnRefresh / OnUpdate / OnDestroy) | spike Tick 等 1 帧 → 调用 **`GameModule.UI.CloseUI<S5_08_MockMinimalPanel>()`** → 等 1 帧 → 再次 `ShowUI<S5_08_MockMinimalPanel>()` | 完整 TEngine vendor lifecycle 顺序 capture：first show: `ScriptGenerator → BindMemberProperty → RegisterEvent → OnCreate → OnRefresh`（同帧序列）→ `OnUpdate × ≥1 帧 while visible` → CloseUI 触发 → `OnDestroy` （vendor 销毁；**非 OnClose**）；second show: 重新走完整 init + lifecycle 流（per vendor — 注意：此处 mock panel 不复用 vs ADR-011/SP-002 spec "second show 仅 OnRefresh" 假设 — 待 R3 实证；如 drift surfaced 累计 V3 Type-2(c) dp） |
| **P4** | ButtonOnClickPath | post-P2（mock panel visible 含 UnityEngine.UI.Button 子组件 mounted in spike setup；可由 mock panel `ScriptGenerator` override 添加 Button child 或 spike 后挂）；spike subscribe button.onClick + 加 1 个 listener handler 函数（`_clickCount++`）| spike `button.onClick.Invoke()` 模拟点击 3 次 | listener handler 调用 3 次 + `_clickCount == 3`（验证 S5-02 minimal main menu Button click path 同前置；Button.onClick.AddListener + Invoke API 通路 production verified）|

**evidence JSON schema** (`Application.persistentDataPath/S5-08_Result.json`):

```json
{
  "story_id": "S5-08",
  "timestamp": "2026-05-XX HH:MM:SS",
  "all_passed": true,
  "overall_status": "All Passed",
  "total_time_ms": 1500,
  "cases": [
    {"id": "P1", "passed": true, "duration_ms": 100, "asserts": ["GameModule.UI!=null", "UIModule.UIRoot!=null", "UIRoot.layer==UI", "UICamera!=null", "DontDestroyOnLoad=true", "UILayer.Bottom.GetSortingOrderBase()==0", "UILayer.UI==100", "UILayer.Top==200", "UILayer.Tips==300", "UILayer.System==400"]},
    {"id": "P2", "passed": true, "duration_ms": 400, "events": ["ScriptGenerator frame=N", "BindMemberProperty frame=N", "RegisterEvent frame=N", "OnCreate frame=N", "OnRefresh frame=N (same frame)"], "asserts": ["panel.parent in UIRoot subtree", "panel.active=true", "_uiStack contains panel"]},
    {"id": "P3", "passed": true, "duration_ms": 600, "events": ["1st show: ScriptGenerator→BindMemberProperty→RegisterEvent→OnCreate→OnRefresh @frame=N", "OnUpdate ×≥1 @frame=N+1..", "CloseUI @frame=M", "OnDestroy @frame=M", "2nd show: full lifecycle replay @frame=M+1 (or partial per vendor TBD)"]},
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
  - vendor 7+2 lifecycle event 顺序 dump（ScriptGenerator / BindMemberProperty / RegisterEvent / OnCreate / OnRefresh / OnUpdate / OnDestroy + Hide/Close 完整时序）
  - AC matrix 10/10
  - Watch List Hooks（如出现 framework boundary drift / TEngine vendor gap / V3 candidate）
- **EditMode test**: 本 story Integration type；无强制 EditMode test 要求（Popup Queue / Auto InputBlocker 等 logic 留 story-008 写 EditMode test）

**Status**: **DONE 2026-05-11 Session 26 #6** — Phase 2~4 全完成：
- Spike `S5-08_UIModuleSetup.cs` ~430 行 (1 file + 3 inner class per S5-1b/1c precedent)
- Mock fixture `S5_08_MockMinimalPanel.cs` ~190 行 (9 lifecycle override + ResetForTest + ButtonRef + ClickCount + LifecycleEvents static List)
- Helper `UILayerExtensions.cs` ~50 行 (vendor UILayer enum + GetSortingOrderBase extension)
- Editor generator `S5_08_MockPanelGenerator.cs` ~135 行 (`Tools/S5-08/Generate Mock Panel Prefab`)
- Generated prefab `Assets/Resources/UI/S5_08_MockMinimalPanel.prefab` (RectTransform + Canvas + GraphicRaycaster + Image + Button child + Text)
- GameApp.cs RegisterDevSpikes 切换 S51cSpike → S508Spike
- JSON evidence dump 完成 (`~/Library/Application Support/DefaultCompany/Unity/S5-08_Result.json` 2026-05-11 18:20:35 — 29 asserts + 4 case events)
- QA evidence doc `production/qa/playmode-uimodule-setup-2026-05-11.md` ~280 行 (含 V3 Type-8 dp1 实战 capture)

---

## Dependencies

- **Depends on**:
  - **ADR-001 (Accepted)** TEngine Framework — UIModule 是 TEngine 核心模块
  - **ADR-011 (Accepted)** UIWindow Management — 层级和队列设计来源；本 story cover 5 层枚举 + UIWindow lifecycle；Popup queue/InputBlocker 留 story-008
  - **ADR-027 (Accepted)** Event Layer — UIWindow listener / GameModule.UI 协议
  - **ADR-029 V2.0 (Accepted)** R3 mandatory — Integration type 必须 PlayMode probe
  - **ADR-030 (Accepted)** §VS Build commitment — Sprint 5 [A] serial 序列
  - **SP-002** UIWindow Lifecycle (historical spec; R2.3 实证 drift 留 Sprint 5 retro 系统性 amend) — spec 4 method vs vendor 7+2 method (含 OnDestroy ≠ OnClose wording drift)
  - **S5-04 ✅ DONE 2026-05-11** — art-bible Accepted；VS art readiness gate ✅ open
- **Unlocks**:
  - **S5-02** Chapter 1 end-to-end happy path（hard prerequisite — main menu 2 minimal inline Button 需本 story UIRoot scene 实例化 verify + `GameModule.UI.ShowUI/CloseUI` API 通路）
  - **story-008** UI Layer Strategy（Sprint 6 polish — 依赖本 story 的 UIRoot + GameModule.UI 通路）
  - **story-006 / -002..-007** 各 UIWindow 业务面板（Sprint 6+ — 依赖本 story 的 UIRoot + UIWindow base class lifecycle 文档）

---

## Assumptions Validated (R2 — Evidence Collection 2026-05-11 ✅)

R2 grep + SemanticSearch + static analysis 实证已完成 2026-05-11（Session 26 #4）。**5 大块 TEngine vendor wiring uncertainty + 3 项 precedent 全部 verified**：

| # | 假设 | 实测 evidence | 结果 |
|---|------|--------------|------|
| **R2.1** | `GameModule.UI` static accessor 已在项目内暴露且 non-null | `UIModule.cs:15` `public sealed partial class UIModule : Singleton<UIModule>` ✅；`GameLobbyState.cs:18` 实际用法 comment `GameModule.UI.ShowUIAsync<LobbyUI>()` 证明 framework 已暴露 | ✅ |
| **R2.2** | `UIModule.ShowWindow<T>()` / `CloseWindow<T>()` API signature TEngine vendor exposed | **⚠️ DRIFT** `UIModule.cs:250-460` 实际 API 是 `ShowUI<T>()` / `ShowUIAsync<T>()` / `ShowUIAsyncAwait<T>()` / `CloseUI<T>()` / `HideUI<T>()` / `CloseAll(bool)`，**非** `ShowWindow/CloseWindow` (ADR-011 spec wording drift) | ⚠️ DRIFT (本 story amend wording 对齐 vendor) |
| **R2.3** | `UIWindow` abstract base class 路径 + 4 lifecycle method (`OnCreate`/`OnRefresh`/`OnUpdate`/`OnClose`) 已 exposed by TEngine | **⚠️ PARTIAL DRIFT** `UIBase.cs:144-197` 实际 7 lifecycle (`ScriptGenerator → BindMemberProperty → RegisterEvent → OnCreate → OnRefresh → OnUpdate × N → OnDestroy`) + `UIWindow.cs:504-509` 加 `Hide` / `Close` 2 hook；**OnDestroy ≠ OnClose** (ADR-011/SP-002 spec wording drift) | ⚠️ DRIFT (本 story amend lifecycle list) |
| **R2.4** | UIRoot Canvas runtime 实例化路径与时机 | ✅ TEngine vendor `Assets/TEngine/Settings/Prefab/UIRoot.prefab` 已存在 + `Assets/Scenes/main.unity` scene 内已实例化 UIRoot GameObject (含 Canvas + UICamera)；`UIModule.OnInit()` (UIModule.cs:49-94) 自动 `GameObject.Find("UIRoot")` → 拿 Canvas transform → 拿 UI Camera → `DontDestroyOnLoad` 持久化 → 设 UI layer；**S5-08 不需要 production code 创建 UIRoot** | ✅ |
| **R2.5** | spike 模式 `Awake()` subscribe 协议（per S5-1c lessons memo） | S5-1c spike `S5-1c_ListenerPathDriver.cs` PlayMode 5/5 PASS first-run verified | ✅ S5-1c precedent |
| **R2.6** | M1 dual-layer pattern 复用 (production reflection 全程) per S5-1c/S5-02 precedent | S5-1c spike PlayMode 5/5 PASS + S5-02 story-002 R3 design 复用 | ✅ S5-1c precedent |
| **R2.7** | Spike "1 文件 + 3 内类" 惯例 | Glob `Spikes/S*.cs` 全部命中（S301..S5-1c 8 个 spike 全部一致）| ✅ |
| **R2.8** *(新增)* | chapter scene 是否需要 UIRoot 引用 | DontDestroyOnLoad 持久化后跨 scene 可用；待 R3 dev-story 实施时 verify chapter scene 不需要 UIRoot 引用即可 ShowUI | TBD (R3 dev-story 阶段) |
| **R2.10** *(Session 26 #5 新增 — story-readiness gate 发现)* | `UILayer` enum 是否需新建 vs vendor 已存在 | **⚠️ DRIFT** vendor 已存在 `WindowAttribute.cs:8` `public enum UILayer : int { Bottom=0, UI=1, Top=2, Tips=3, System=4 }` (namespace `GameLogic`)；新建 `Assets/GameScripts/HotFix/GameLogic/UI/UILayer.cs` 会同 namespace collision；vendor 命名 vs ADR-011 spec wording 第 3 例 drift (Bottom≈Background / UI≈HUD / Top≈Popup / Tips≈Overlay / System=System) | ⚠️ DRIFT (本 story 改用 vendor enum + 新建 UILayerExtensions.cs helper；不 collision) |
| **R2.11** *(Session 26 #5 新增)* | `[Window]` attribute 是否 vendor 已提供 | ✅ vendor 已提供 `WindowAttribute.cs:21 public class WindowAttribute : Attribute` + 4 ctor overload (`UILayer windowLayer`, optional `string location`/`bool fromResources`/`bool fullScreen`/`int hideTimeToClose`)；vendor `LogUI.cs:8` 实际用法 `[Window(UILayer.System, fromResources: true)]` 模式 | ✅ |

**R2 Verdict**: **DEFICIENCY-FLAGGED PASS** (per ADR-029 V2.0) —— 2 项 wording drift (R2.2 + R2.3) 已通过本次 amend 在 story-001 内对齐 vendor wording；ADR-011 spec / SP-002 系统性 wording amend 留 Sprint 5 retro 评估（per V3 Type-5 dp）。R3 dev-story 可启动。

**R2 Discovery — V3 Type-5 dp 数据点累计** (per ADR-029 V2.0 §V2-7):
- `R2.2 ShowWindow → ShowUI wording drift` 是 **ADR-011 spec ↔ TEngine vendor API drift** 实战触发
- `R2.3 4-method → 7-method lifecycle drift + OnClose → OnDestroy wording drift` 同源
- **`R2.10 UILayer enum vendor 已存在 + wording drift` (Session 26 #5 story-readiness gate 发现)** — vendor `{Bottom/UI/Top/Tips/System}` vs ADR-011 spec `{Background/HUD/Popup/Overlay/System}` 同 namespace `GameLogic`；本 story 跟 vendor wording
- 累计 V3 Type-5 candidate "tooling/spec ↔ vendor reality drift" dp（与 S5-01 D1~D4 `unity-mcp tooling silent failure` 同 type；本 story 累计 3 个 unique 实战 dp + ADR-011/SP-002 系统性 amend 候选）

---

## S5-02 Coordination

本 story 是 **S5-02 Chapter 1 end-to-end happy path** 的 **hard prerequisite**（per 2026-05-11 Sprint 5 [A] serial 序列决策 + S5-02 main menu UI [A] minimal_inline 决策）：

| 本 story 产出 | S5-02 消费 |
|---|---|
| **AC-2**: UIRoot scene 已实例化 verify + UIModule.OnInit auto-wire 路径 (含 UICamera + DontDestroyOnLoad) | S5-02 main menu panel 通过 `GameModule.UI.ShowUI` 自动挂到 UIRoot 子节点 (DontDestroyOnLoad 跨 scene 可用) |
| **AC-3**: `GameModule.UI` 静态门面通路 (`GameModule.UI.ShowUI / CloseUI / HideUI` API per vendor R2.2 实证) | S5-02 main menu Button onClick handler 调用 **`GameModule.UI.ShowUI<T>()`** / **`CloseUI<T>()`** API |
| **AC-4**: `ShowUI<T>()` / `CloseUI<T>()` / `HideUI<T>()` API 通路 + Mock panel 实例化 verified | S5-02 minimal main menu UIWindow（正式 panel 子类替代本 story mock fixture）通过同 API 实例化 |
| **AC-5**: UIWindow 完整 vendor lifecycle 7+2 method 文档注释 (ScriptGenerator → BindMemberProperty → RegisterEvent → OnCreate → OnRefresh → OnUpdate × N → OnDestroy + Hide/Close) | S5-02 minimal main menu panel 子类按 vendor lifecycle 时序实现 4 user-facing method (OnCreate / OnRefresh / OnUpdate / OnDestroy) + 文档注释 |
| **AC-8**: Button.onClick.AddListener + Invoke API 通路 verified | S5-02 main menu 'Start Chapter 1' Button onClick → `ISceneEvent.OnRequestSceneChange(1)` dispatch；'Next Chapter' Button onClick → `OnRequestSceneChange(0)` dispatch（详 `story-002-end-to-end-flow.md` P1 + P5）|

S5-02 dev-story 内 spike (`S5-02_EndToEndFlow.cs`) 模拟点击 Button 走 production path：本 story 的 mock fixture (`S5_08_MockMinimalPanel`) DevTest namespace 不入 S5-02 path；**S5-02 dev-story 内基于本 story API 通路重写为正式 minimal main menu panel** 含 2 个 Button。

完整 main menu UIWindow（New Game / Continue / Settings 按钮 + 存档检查 + fade-in + BGM）见 `story-006-main-menu.md` Sprint 6 polish。

---

## ADR-029 V3 Watch List Hooks

本 story R3 实施过程中如出现以下情况应 capture 为新 drift type 候选并写入 sprint-status.yaml watch list：

1. **Type-2(c) candidate**: UIWindow 完整 7 lifecycle method 调用顺序与 vendor 实证不符（R3 P3 case 实测）—— first show 顺序 / second show 是否复用 init phase / OnUpdate 是否仅 Visible=true 触发 等任意 drift → framework boundary behavior assumption drift；累计 dp 数据点
2. **Type-5 candidate (S5-01 dp1 + 本 story R2 + R3 三触发累计)**:
   - **(a) S5-01 D1~D4 dp1**: `unity-mcp` toolchain silent failure (2026-05-09 实证)
   - **(b) S5-08 R2.2 + R2.3 dp 实战触发 2026-05-11 Session 26 #4**: **ADR-011 spec / SP-002 spec ↔ TEngine vendor API drift** — `ShowWindow → ShowUI/CloseUI/HideUI` wording (R2.2) + `4 lifecycle → 7 lifecycle + OnClose → OnDestroy` wording (R2.3)；同源 "spec ↔ reality drift" 模式（spec 文档 与 实际工具/vendor 实施 不一致）
   - **(c) S5-08 R2.10 dp 实战触发 2026-05-11 Session 26 #5 — story-readiness gate R3 阶段发现**: **ADR-011 spec UILayer wording ↔ TEngine vendor UILayer wording drift** — vendor `{Bottom=0, UI=1, Top=2, Tips=3, System=4}` vs spec `{Background=0, HUD=1, Popup=2, Overlay=3, System=4}` 第 3 例 wording drift；同源；avoid type collision 改用 vendor enum + helper extension method
   - **累计**: V3 Type-5 候选 promote 数据点 +2 (本 story 共贡献 dp2+dp3 两个 unique dp)；与 S5-01 D1~D4 同 type；累计 3 个 unique 实战 dp (1 toolchain failure + 2 ADR-011 spec ↔ vendor wording drift)；超过 V3 promote ROI 阈值 (≥ 3 unique dp) → Sprint 5 retro **强烈建议 promote** 为 ADR-029 V3 正式 candidate "spec ↔ reality drift" 类 (split or unified) + ADR-011 §G implementation expand + SP-002 系统性 wording amend (含 UILayer enum 命名 / ShowUI API 命名 / 7 lifecycle method 命名) 高优 action item
3. **Type-6 candidate (S5-1c lessons memo promote 候选累计)**: spike subscribe race 如再次出现（UIModule init 同步 fire event 路径或类似）→ 累计 V3 #6 dp 数据点；本 story spike Awake() subscribe 已采纳防御
4. **Type-7 candidate (新)**: UIRoot DontDestroyOnLoad 跨 scene 持久化路径如出现 race（如 chapter scene load 时 UIRoot reference 丢失 / Canvas 渲染异常）→ 累计 V3 candidate `boot-order / scene-transition drift` dp 数据点（R3 P1 + S5-02 R3 dev-story 实施时 verify）
5. **Type-8 candidate (本 story R3 P3 实战 NEW dp1 触发 2026-05-11 Session 26 #6)**: UIWindow second show 行为 spec vs vendor drift **CONFIRMED**:
   - ADR-011/SP-002 spec 假设 "second show 仅 OnRefresh，已存在 instance 复用"
   - **vendor 实测 (R3 P3 frame=59)**: **创建新 instance** (vs first show frame=30) + **完整 init phase replay** (ScriptGenerator + BindMemberProperty + RegisterEvent + OnCreate + OnRefresh 4 init methods + 2 method 重新调用) + OnUpdate × N continued
   - 推测根因: vendor CloseUI<T>() 走 OnDestroy 销毁 instance + 从 _uiStack 移除；second ShowUI<T>() 走 Activator.CreateInstance 新 instance + Resources.Load + 完整 InternalLoad → InternalCreate → InternalRefresh 链路。**这是 vendor 的"销毁后重建"模式**，**非** spec 假设的"隐藏后重显示"模式
   - **dp1 累计**: V3 Type-8 candidate "UIWindow second show 行为 spec vs vendor 是否一致" dp1 实战触发；ADR-011 §G "UIWindow 显示/隐藏行为" 系统性 amend 候选 (高优 Sprint 5 retro action item)

如出现以上任一，per ADR-029 V2.0 §V2-7：sprint-status.yaml `watch_list` triggers 内追加 drift type 描述 + 关联 story-001 R3 case 编号 + 沉淀 problem memo 到 `.claude/memory/`。

---

## History

### 2026-05-11 Session 26 #6 — dev-story Phase 2~4 完成 (Status: Ready → Done)

**Trigger**: 2026-05-11 Session 26 #6 /dev-story S5-08 实施；Phase 1 read refs + Phase 1.5 self-check auto PASS (R1/R2/R3 已 done by readiness gate Session 26 #5) + Phase 2 实施 4 C# files + Phase 2.3 prefab generated via Editor menu (unity-mcp `Tools/S5-08/Generate Mock Panel Prefab` execute_menu_item) + Phase 3 PlayMode 实跑 + Phase 4 evidence doc 完成。

**Phase 3 PlayMode 实跑结果**: **4/4 R3 case PASS first-run / 29/29 asserts / all_passed=true** (JSON: `~/Library/Application Support/DefaultCompany/Unity/S5-08_Result.json` 2026-05-11 18:20:35)

**Phase 3 重要 vendor 行为实证发现**:

1. **R2.3 lifecycle drift R3 verified** (P2/P3): vendor 7 method (ScriptGenerator → BindMemberProperty → RegisterEvent → OnCreate → OnRefresh → OnUpdate × N → OnDestroy) 同帧/连续帧调用顺序在 ShowUI + visible + CloseUI + second show 各阶段实测对齐 — spec 4 method 假设 confirmed drift
2. **R2.4 UIRoot scene 实例化 R3 verified** (P1): UIRoot = UICanvas Transform / parent.scene = DontDestroyOnLoad / layer = UI(5) / UICamera non-null — vendor UIModule.OnInit() auto-wire 路径 confirmed
3. **R2.10 UILayer enum R3 verified** (P1): vendor enum + UILayerExtensions.GetSortingOrderBase() 5 layer × 100 sorting order base — 0/100/200/300/400 asserts 全 PASS
4. **vendor CloseUI<T>() sync path 直走 OnDestroy** (P3): 不走 Hide/Close hook (UIWindow.cs:504/509)；推测 Hide/Close 是 HideUI<T>() 路径相关，非 CloseUI 路径
5. **⭐ V3 Type-8 candidate dp1 实战 NEW TRIGGER** (P3 second show): vendor **销毁后重建** 而非 spec 假设 **隐藏后重显示** — 完整 init phase replay (ScriptGenerator + BindMemberProperty + RegisterEvent + OnCreate + OnRefresh 4 init methods)；ADR-011 §G UIWindow 显示/隐藏行为 spec amend 候选

**Phase 3 prefab fix issue (中间 1 round 修复)**: 首次 prefab 生成时 root 缺 Canvas + GraphicRaycaster → vendor UIWindow.cs:484 `_panel.GetComponent<Canvas>()` throw "Not found Canvas in panel" → P2 直接 fail；Editor 脚本 `S5_08_MockPanelGenerator.cs` 加 `root.AddComponent<Canvas>()` + `root.AddComponent<GraphicRaycaster>()` 修复后重新生成 prefab → 4/4 PASS first-run

**Code 改动汇总**:

- **新增 (4 files + 1 prefab)**:
  - `Assets/GameScripts/HotFix/GameLogic/UI/UILayerExtensions.cs` (~50 行)
  - `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5_08_MockMinimalPanel.cs` (~190 行)
  - `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-08_UIModuleSetup.cs` (~430 行)
  - `Assets/Editor/DevTest/S5_08_MockPanelGenerator.cs` (~135 行)
  - `Assets/Resources/UI/S5_08_MockMinimalPanel.prefab` (Editor generated)
- **改 (1 file)**:
  - `Assets/GameScripts/HotFix/GameLogic/GameApp.cs` RegisterDevSpikes 切 S51cSpike → S508Spike
- **不改 vendor**: `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/` 0 patch

**ADR-029 V3 dp 累计现状 (本 story 贡献)**:
- Type-5 'spec/tooling ↔ reality drift': **3 unique dp** (dp1 S5-01 + dp2 S5-08 #4 + dp3 S5-08 #5) — **超 promote 阈值** → Sprint 5 retro 强烈建议 promote
- Type-8 'UIWindow second show 行为 spec vs vendor': **1 dp NEW** (S5-08 #6 R3 P3 实战) — ADR-011 §G UIWindow 显示/隐藏行为 spec amend 候选

**Sprint 5 retro action items (本 story 累计贡献)**:
1. promote V3 Type-5 candidate (split Type-5a tooling silent failure + Type-5b spec wording drift / 或 unified)
2. ADR-011 §G systematic amendment — UILayer enum 命名 + ShowUI/CloseUI/HideUI API 命名 + UIWindow 7+2 lifecycle method 命名 + second show 行为描述 (vendor 销毁后重建 vs spec OnRefresh-only)
3. SP-002 systematic amendment — UIWindow lifecycle 4 method → 7+2 method + OnDestroy ≠ OnClose wording
4. 新建 Type-8 candidate — UIWindow second show 行为 spec vs vendor drift；后续 ui-system stories monitor

**Sign-off**: 4/4 R3 case PASS first-run (29/29 asserts) + 完整 evidence dump + V3 Type-8 dp1 NEW 实战触发 + 0 unexpected console error/warning + 0 vendor patch + AC matrix 10/10 PASS。**S5-08 DONE**，解锁 S5-02 (Chapter 1 end-to-end 5 系统串通 happy path) dev-story 实施。

**Audit Trail Cross-references**:
- Evidence doc: `production/qa/playmode-uimodule-setup-2026-05-11.md` (~280 行)
- JSON: `~/Library/Application Support/DefaultCompany/Unity/S5-08_Result.json` 2026-05-11 18:20:35
- Session 26 #4 commit: 9a669a4 (R2 evidence + wording drift amend [D])
- Session 26 #5 commit: b394add (readiness gate R3 collision resolved [A])
- Phase 2 C# commit: 1a2ee93 (4 C# files)
- Phase 2.3 prefab generator commit: bfc0145 (Editor)
- Phase 5 closure commit: 见后续

---

### 2026-05-11 Session 26 #5 — /story-readiness gate R3 stub type collision discovery + amendment (per 决策 [A])

**Trigger**: 2026-05-11 Session 26 #5 /story-readiness gate R1+R3 复审阶段；R3 stub type construction signatures grep 发现 vendor `WindowAttribute.cs:8` 已存在 `public enum UILayer : int { Bottom=0, UI=1, Top=2, Tips=3, System=4 }` (namespace `GameLogic`)；story-001 AC-1 新建 `Assets/GameScripts/HotFix/GameLogic/UI/UILayer.cs` 含 `{Background/HUD/Popup/Overlay/System}` 与 vendor enum 同 namespace **C# type collision 阻塞编译**。同时发现 vendor `WindowAttribute.cs:21` 已提供 `WindowAttribute` 4 ctor overload (R2.11)。

**R3 stub type construction Verdict**: ❌ collision discovered → 升级为 NEEDS WORK；本次 amend 后转回 PASS

**R3 Findings**:

1. **R2.10 ⚠️ DRIFT** — `UILayer` enum vendor 已存在 (WindowAttribute.cs:8 namespace GameLogic 5 值 Bottom/UI/Top/Tips/System)；新建 `UILayer.cs` 必 collision；wording 与 ADR-011 spec drift (Bottom≈Background / UI≈HUD / Top≈Popup / Tips≈Overlay / System=System)
2. **R2.11 ✅ EXIST** — `WindowAttribute` vendor 已提供 4 ctor overload (`UILayer windowLayer`, optional `string location`/`bool fromResources`/`bool fullScreen`/`int hideTimeToClose`)；vendor LogUI.cs:8 实际用法 `[Window(UILayer.System, fromResources: true)]`

**Amendments (本次 per 决策 [A] — 跟 vendor wording 对齐 与 Session 26 #4 [D] 同源原则)**:

1. **§Engine Notes**: 加 R2.10 ⚠️ DRIFT (UILayer vendor 已存在 + spec wording 映射表) + R2.11 ✅ (WindowAttribute 已提供) 两 bullet
2. **§Acceptance Criteria**:
   - AC-1 重写: 不新建 UILayer.cs → 改为创建 `UILayerExtensions.cs` 含 `GetSortingOrderBase(this UILayer)` extension method + XML doc 注释 vendor 路径 + wording 映射表
   - AC-6 改: 注释指向 vendor enum 路径 (`WindowAttribute.cs:8`) 而非新 UILayer.cs
3. **§Implementation Notes**:
   - UILayer.cs bullet 替换为 UILayer enum R2.10 实证 (vendor 已存在 + 新建 UILayerExtensions.cs helper 描述)
   - 新增 `[Window]` attribute R2.11 实证 bullet (vendor 已 4 ctor overload)
   - Mock minimal panel 加 `[Window(UILayer.UI, fromResources: true)]` attribute 用法 (沿 LogUI.cs:8 precedent)
4. **§R3 PlayMode probe**:
   - P1 case 加 UILayerExtensions sanity verify (调用 5 layer GetSortingOrderBase + assert 0/100/200/300/400)
   - P2 case 加 `[Window(UILayer.UI, fromResources: true)]` 设置说明 (vendor attribute 路径)
   - JSON evidence schema P1 asserts 加 5 个 UILayer.X.GetSortingOrderBase() asserts
5. **§Assumptions Validated 表**: 加 R2.10 row (DRIFT) + R2.11 row (✅)
6. **§ADR-029 V3 Watch List Hooks Type-5 candidate**:
   - (b) Session 26 #4 dp 标注
   - **(c) Session 26 #5 R2.10 dp 新增**: ADR-011 spec UILayer wording ↔ vendor wording drift
   - **累计**: V3 Type-5 dp 第 3 例 → 超 V3 promote ROI 阈值 (≥ 3 unique dp) → Sprint 5 retro **强烈建议 promote** 为正式 candidate；ADR-011 §G + SP-002 系统性 amend 高优 action item

**Rationale**:
- vendor 是 ground truth — 已 实际用法 (LogUI.cs:8 `[Window(UILayer.System, fromResources: true)]`)
- ADR-011 spec wording 是 historical (Sprint 0 / Sprint 3 framework time)
- 新建 collision enum 不可行 (C# type collision 阻塞编译)
- 改名差异化方案 ([B]) 会导致 vendor `[Window]` 与业务 sorting order 用两套 enum，团队混淆成本高
- vendor patch ([C]) 侵入性大；vendor 命名虽不完全符合 ADR-011 spec 但功能等价 (Bottom 等价 Background 等)
- 删除整 UILayer 工作 ([D]) 牺牲 helper method 价值 (后续 story 仍需 sorting order 计算)
- 跟 vendor wording 是与 Session 26 #4 [D] 同源原则 (vendor 优先 / wording 对齐 / 累计 dp / 留 retro 系统性 amend)

**R3 stub type Verdict after amendment**: ✅ PASS (collision avoided；用 vendor enum + extension method)

**Sign-off**: User 决策落盘 (选项 [A] 2026-05-11 Session 26 #5 — 跟 vendor wording 对齐 + V3 Type-5 dp3 累计 retro 强烈建议 promote)

**Audit Trail Cross-references**:
- TEngine vendor sync: c5f8952 (2026-05-09 — TEngine 6.2.1 vendor sync)
- vendor UILayer enum 引用: `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/WindowAttribute.cs:8`
- vendor `[Window]` attribute 实际用法: `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/ErrorLogger/LogUI.cs:8`
- V3 Type-5 dp 累计: sprint-status.yaml `sprint_5_adr_029_v3_watch_list` (S5-01 D1~D4 dp1 + S5-08 dp2 + dp3 三 dp)
- ADR-011 spec wording 系统性 amend action item: Sprint 5 retro 高优 (含 UILayer enum 命名 / ShowUI API 命名 / 7 lifecycle method 命名)

---

### 2026-05-11 Session 26 #4 — R2 evidence collection + wording drift amendment (per 决策 [D])

**Trigger**: 2026-05-11 R2 evidence collection 阶段 (Session 26 #4)；SemanticSearch / Grep / static analysis 5 大 TEngine vendor wiring uncertainty (R2.1~R2.5)；发现 R2.2 + R2.3 ADR spec ↔ vendor API drift。

**R2 Verdict**: **DEFICIENCY-FLAGGED PASS** (per ADR-029 V2.0)

**R2 Findings**:

1. **R2.1 ✅ GameModule.UI** — TEngine `Singleton<UIModule>` 已暴露 (UIModule.cs:15)；GameLobbyState.cs:18 实际用法 comment 证明 framework wire-ready
2. **R2.2 ⚠️ DRIFT** — API wording: ADR-011 spec / SP-002 用 `ShowWindow / CloseWindow`，TEngine vendor 实际 `ShowUI / CloseUI / HideUI` (UIModule.cs:250-460)；含 `ShowUIAsync` / `ShowUIAsyncAwait` 异步变体
3. **R2.3 ⚠️ PARTIAL DRIFT** — Lifecycle 数量 + wording:
   - 数量: ADR-011/SP-002 假设 4 method (`OnCreate / OnRefresh / OnUpdate / OnClose`)；vendor 实际 7 method (UIBase.cs:144-197) + UIWindow 2 extra hook (Hide/Close, UIWindow.cs:504-509)
   - wording: vendor `OnDestroy` ≠ spec `OnClose`
4. **R2.4 ✅ UIRoot Canvas** — TEngine vendor `Assets/TEngine/Settings/Prefab/UIRoot.prefab` + main.unity scene 已实例化；UIModule.OnInit() (UIModule.cs:49-94) 自动 `GameObject.Find("UIRoot")` + Canvas + UICamera + `DontDestroyOnLoad` + UI layer 持久化。**S5-08 不需要 production code 创建 UIRoot**
5. **R2.5~7 ✅ S5-1c precedent** — Awake() subscribe / M1 dual-layer / spike 1 file + 3 inner class

**Amendments (本次)**:

1. **§Context Engine Notes**: 重写 5 项 R2 实证状态（R2.1 ✅ / R2.2 ⚠️ DRIFT / R2.3 ⚠️ PARTIAL DRIFT / R2.4 ✅）+ 标注 vendor 实际 7+2 lifecycle method + ShowUI/CloseUI/HideUI wording
2. **§Acceptance Criteria**:
   - AC-2 改 "UIRoot scene 实例化 verify + UIModule.OnInit auto-wire 路径"（不创建 UIRoot）
   - AC-3/-4 wording: `ShowWindow/CloseWindow` → **`ShowUI/CloseUI/HideUI`**
   - AC-5 lifecycle: 4 method → 完整 vendor 7+2 method 列表 + Init phase / Lifecycle phase 分类 + `OnDestroy ≠ OnClose` 明示
3. **§Implementation Notes**:
   - UIRoot bullet 改为 R2.4 实证版（不创建 + scene 已 instantiate + UIModule.OnInit auto-wire 描述）
   - 新增 "TEngine vendor API wording (per R2.2 实证)" bullet 列 6 API
   - 新增 "TEngine vendor lifecycle (per R2.3 实证)" bullet 列 9 method (7 + 2)
   - "TEngine vendor 调研已完成 R2 阶段" bullet 替换原 "调研路径" bullet
4. **§R3 PlayMode probe 4 cases**: P1 重写为 UIRootSceneInstantiateVerify (不验 5 layer container) / P2 改 ShowMockPanelViaShowUI (用 ShowUI API) / P3 改 UIWindowLifecycleVendorOrder (验完整 7 lifecycle + 标注 second show drift 不定) / P4 不变；JSON evidence schema 同步更新
5. **§Assumptions Validated**: 表头 "TBD" → "Evidence Collection 2026-05-11 ✅"；R2.1~R2.7 全部 evidence 填写；新增 R2.8 (chapter scene UIRoot 需求) TBD；末尾新增 V3 Type-5 dp 累计说明
6. **§S5-02 Coordination 表**: AC-3 / AC-4 / AC-5 wording 对齐 vendor (ShowUI / CloseUI / HideUI 等)
7. **§ADR-029 V3 Watch List Hooks**:
   - Type-5 (S5-01 dp1 + 本 story R2 双触发累计): "spec ↔ reality drift" 同源累计；ADR-011/SP-002 系统性 amend 候选 action item
   - Type-7 改 "UIRoot DontDestroyOnLoad 跨 scene 持久化 race"
   - 新增 Type-8 (本 story R2 surfaced): UIWindow second show 行为 spec vs vendor 是否一致

**Rationale**:
- TEngine vendor 是 ground truth (vendor sync 2026-05-09 c5f8952 已 verified)
- ADR-011 spec / SP-002 wording 是 historical (Sprint 0 findings + Sprint 3 framework time)
- story-001 wording 应 follow vendor 实际 API，不 follow stale spec
- ADR-011/SP-002 系统性 wording amend 留 Sprint 5 retro 评估（per V3 Type-5 candidate "spec ↔ reality drift" 模式累计 dp）

**Sign-off**: User 决策落盘 (选项 [D] 2026-05-11 — wording 对齐 vendor + V3 Type-5 dp 累计 retro 评估)

**Audit Trail Cross-references**:
- TEngine vendor sync: c5f8952 (2026-05-09 — TEngine 6.2.1 vendor sync)
- ADR-011 spec / SP-002 stale wording: 留 Sprint 5 retro action item
- V3 Type-5 dp 累计: sprint-status.yaml `sprint_5_adr_029_v3_watch_list` (S5-01 D1~D4 + S5-08 R2 双 dp 累计)

---

### 2026-05-11 Session 26 #3 — Sprint 5 narrow scope amendment + R3 mandatory addition

**Trigger**: Sprint 5 [A] serial 序列第 2 步（2026-05-11 user 决策）；S5-08 promote should-have → must-have；S5-02 Chapter 1 end-to-end happy path [A] minimal_inline 决策要求最小 UIModule 通路足够支撑 main menu 2 minimal inline Button。

**Original Status (Sprint 3/4)**:
- Story Type: Logic / 9 AC (含 Popup Queue + Auto-Dequeue + Auto InputBlocker + Overlay limit + 单元测试) / 5 EditMode TC / Test Evidence: `tests/unit/UIModule_PopupQueue_Test.cs` / Status: Ready (Sprint: TBD)

**Amendments**:

1. **Header**: Status `Ready` → `Draft` (待 amend 通过 /story-readiness gate 再 → Ready) / Story Type `Logic` → **`Integration`** (Framework Boundary R3 mandatory per ADR-029 V2.0) / Sprint `TBD` → **`Sprint 5`** / 加 `Manifest Version: 2026-05-11`
2. **AC narrow scope**: 删 AC-2..-6 (Popup Queue Manager / Auto-Dequeue / Auto InputBlocker / token 命名规范 / Overlay limit) + AC-9 (单元测试)；保留 AC-1 (UILayer 枚举) / AC-7 (UIWindow lifecycle 注释) / AC-8 (UIModule 路径注释)
3. **AC additions**: AC-1 加 helper static method / AC-2 UIRoot Canvas runtime 实例化 / AC-3 GameModule.UI 静态门面通路 / AC-4 ShowWindow/CloseWindow API 通路 + Mock panel (Session 26 #4 amend → ShowUI/CloseUI/HideUI per vendor R2.2) / AC-5 UIWindow lifecycle 文档注释 (Session 26 #4 amend → vendor 7+2 method per R2.3) / AC-6 UIModule 程序集路径注释 / AC-7 Out of Scope 明示 / AC-8 Button onClick path / AC-9 console clean / AC-10 R3 ALL PASS
4. **R3 PlayMode probe 新增**: 4 case (P1 UIRootCanvasRuntimeInit / P2 ShowMockPanelToUIRoot / P3 UIWindowLifecycleOrder / P4 ButtonOnClickPath) + M1 dual-layer 全 production reflection 复用 S5-1c/S5-02 precedent + spike `S5-08_UIModuleSetup.cs` 1 file + 3 inner class 项目惯例
5. **Test Evidence**: EditMode-only → PlayMode JSON + qa doc + grep evidence 多层；mock fixture `S5_08_MockMinimalPanel.cs` (DevTest namespace) S5-02 时被正式 minimal main menu panel 替代
6. **Out of Scope 明示**: Popup Queue / Auto InputBlocker / 完整 UIWindow 业务面板 → 已存在 stories 全部明示 (story-008 / -006 / -002..-007/009/010)；不新建 ui-system-001b（story-008 已 cover 同 scope）
7. **Dependencies + S5-02 Coordination + V3 Watch List Hooks 新增**

**Rationale**:
- Sprint 5 [A] serial 时间预算限制：S5-08 dev-story estimation 2 SP ≈ 3-3.5h workshift；原 9 AC + R3 跑全 ≥ 5h，over-spec
- 已有 story-008 cover Popup Queue/InputBlocker robust UI infrastructure（Sprint 6 polish）；narrow 不重复造轮子
- S5-02 minimal_inline 决策只需 minimal UIRoot verify + `ShowUI/CloseUI` API + Button mount path —— 本 story narrow scope 精准对应
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






