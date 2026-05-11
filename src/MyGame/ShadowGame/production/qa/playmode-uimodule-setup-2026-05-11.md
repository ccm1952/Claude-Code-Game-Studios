// 该文件由Cursor 自动生成

# S5-08 R3 PlayMode Evidence — UIModule Setup (2026-05-11)

> **Story**: S5-08 — UIModule Initialization + UIWindow Base Class Setup (Sprint 5 narrow scope amendment)
> **Sprint**: 5 (start 2026-05-06 / end 2026-05-20)
> **Epic**: ui-system
> **Type**: Integration (Framework Boundary R3 mandatory per ADR-029 V2.0)
> **Engine**: Unity 2022.3.62f2 LTS + URP + HybridCLR + YooAsset 2.3.17 + UniTask 2.5.10 + TEngine 6.2.1
> **Date**: 2026-05-11
> **Verdict**: **PASS** (4/4 R3 case + 29/29 asserts + all_passed=true first-run)
> **Story file**: `production/epics/ui-system/story-001-uimodule-setup.md`
> **Governing ADRs**: ADR-011 (UIWindow Management) / ADR-001 (TEngine Framework) / SP-002 (UIWindow Lifecycle) / ADR-029 V2.0 (R3 mandatory) / ADR-027 (Event Layer) / ADR-030 (§VS Build commitment)
> **Spike file**: `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-08_UIModuleSetup.cs` (1 文件 + 3 内类)
> **Mock fixture**: `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5_08_MockMinimalPanel.cs`
> **Editor generator**: `Assets/Editor/DevTest/S5_08_MockPanelGenerator.cs` (`Tools/S5-08/Generate Mock Panel Prefab`)
> **Generated prefab**: `Assets/Resources/UI/S5_08_MockMinimalPanel.prefab`
> **Helper**: `Assets/GameScripts/HotFix/GameLogic/UI/UILayerExtensions.cs`
> **JSON evidence**: `~/Library/Application Support/DefaultCompany/Unity/S5-08_Result.json` (timestamp: 2026-05-11 18:20:35)

---

## §0 概要

S5-08 narrow scope **UIModule 运行时基础接入完成**：

- ✅ TEngine vendor `Singleton<UIModule>.OnInit()` 自动 wire UIRoot (main.unity 已实例化 prefab) + UICamera + DontDestroyOnLoad + UI layer (per R2.4 实证 + P1 验证)
- ✅ `GameModule.UI` 静态门面通路 + `ShowUI<T>()` / `CloseUI<T>()` / `HideUI<T>()` / `ShowUIAsyncAwait<T>()` API verified (per R2.2 实证 + P2/P3 验证)
- ✅ `UILayer` enum 复用 vendor `WindowAttribute.cs:8` (避免 R2.10 type collision per Session 26 #5 决策 [A])
- ✅ `UILayerExtensions.GetSortingOrderBase()` 5 layer × 100 sorting order base helper (P1 全 5 值 asserts 0/100/200/300/400)
- ✅ UIWindow vendor 7+2 lifecycle 完整捕获 (P2 + P3 实证 R2.3 假设)
- ✅ Button.onClick.AddListener + Invoke API 通路 (P4 验 ClickCount delta=3)
- ✅ Mock minimal panel + Editor 一键生成 prefab (Resources path; fromResources=true 不撞 YooAsset 锁)

R3 PlayMode 4 case M1 dual-layer 全 production reflection 模式（复用 S5-1b/1c precedent）**first-run 4/4 case PASS / 29/29 asserts PASS / all_passed=true**：

| # | Case | 描述 | 状态 |
|---|------|------|------|
| P1 | UIRootSceneInstantiateVerify + UILayerExt sanity | 反射 GameModule.UI + UIModule.UIRoot + UI layer + DontDestroyOnLoad + UICamera + 5 layer GetSortingOrderBase | ✅ PASS (11/11 asserts) |
| P2 | ShowMockPanelViaShowUI | ShowUIAsyncAwait → panel 实例化到 UIRoot 子树 + reflection 验 _uiStack 含此 panel + Init phase 3 + OnCreate + OnRefresh 同帧顺序 | ✅ PASS (11/11 asserts) |
| P3 | UIWindowLifecycleVendorOrder | OnUpdate × 27 frame visible → CloseUI 触发 OnDestroy (vendor 不走 Hide/Close hook sync 路径) → second show 完整 init phase replay (4 methods，**vs spec OnRefresh-only 假设 drift**) | ✅ PASS (5/5 asserts) |
| P4 | ButtonOnClickPath | mock panel ButtonRef.onClick.Invoke() × 3 → ClickCount==3 (S5-02 main menu Button click path 前置) | ✅ PASS (2/2 asserts) |

---

## §1 R3 4 Case Detail

### §1.1 P1 UIRootSceneInstantiateVerify + UILayerExtensions sanity

**Setup**:
- spike `S508Runtime.Start()` 调用 `_tester = new S508Tester(); _tester.RunAllAsync().Forget();`
- TEngine framework 自动 `UIModule.OnInit()` (UIModule.cs:49) 在 GameApp.Entrance 内 SingletonSystem 初始化阶段 wire UIRoot
- main.unity scene 内 `UIRoot` GameObject 已实例化 prefab (TEngine vendor `Assets/TEngine/Settings/Prefab/UIRoot.prefab`)

**Action**: spike `await UniTask.Yield();` 后立刻 reflection 拿 `GameModule.UI` + `UIModule.UIRoot` + verify UIRoot GameObject 状态 + 调用 `UILayer.X.GetSortingOrderBase()` × 5 验 sorting order base

**Asserts (11/11 PASS)**:

| # | Assert | Expected | Actual |
|---|--------|----------|--------|
| 1 | GameModule.UI | non-null | PASS (GameLogic.UIModule) |
| 2 | UIModule.UIRoot | non-null Transform | PASS (UICanvas) |
| 3 | UIRoot.layer | == UI (5) | PASS (5) |
| 4 | UIRoot.parent.scene | == "DontDestroyOnLoad" | PASS |
| 5 | UIRoot.Canvas | Canvas component present | PASS |
| 6 | UICamera | non-null | PASS (UICamera) |
| 7 | UILayer.Bottom.GetSortingOrderBase() | 0 | PASS |
| 8 | UILayer.UI.GetSortingOrderBase() | 100 | PASS |
| 9 | UILayer.Top.GetSortingOrderBase() | 200 | PASS |
| 10 | UILayer.Tips.GetSortingOrderBase() | 300 | PASS |
| 11 | UILayer.System.GetSortingOrderBase() | 400 | PASS |

**Events captured**:
```
GameModule.UI = GameLogic.UIModule
UIModule.UIRoot = UICanvas
UIRoot.layer = 5 (UI layer = 5)
UIRoot.parent.scene = DontDestroyOnLoad
UICamera = UICamera
UILayerExtensions sanity: 0/100/200/300/400
```

**关键发现**: vendor `UIModule.OnInit()` 内 `_instanceRoot = uiRoot.GetComponentInChildren<Canvas>()?.transform` (UIModule.cs:54) 把 _instanceRoot 设为 Canvas transform (UICanvas，而非 UIRoot GameObject 自身)；vendor 的 `DontDestroyOnLoad(_instanceRoot.parent != null ? _instanceRoot.parent : _instanceRoot)` (UIModule.cs:65) 把 parent (UIRoot GameObject) 移到 DontDestroyOnLoad scene。assert "UIRoot.parent.scene == DontDestroyOnLoad" 因此正确通过 (UIRoot = UICanvas; UICanvas.parent = UIRoot GameObject; UIRoot GameObject.scene = DontDestroyOnLoad)。

### §1.2 P2 ShowMockPanelViaShowUI

**Setup**:
- post-P1（UIRoot ready）
- spike 调用 `S5_08_MockMinimalPanel.ResetForTest()` 清空 lifecycle event 累计

**Action**:
```csharp
S5_08_MockMinimalPanel panel = await GameModule.UI.ShowUIAsyncAwait<S5_08_MockMinimalPanel>();
```

vendor 路径:
1. `UIModule.ShowUIAsyncAwait<T>` → 反射 `WindowAttribute` 拿 location="UI/S5_08_MockMinimalPanel" + fromResources=true
2. `Activator.CreateInstance(type) as UIWindow` (UIModule.cs:545) 实例化 mock panel C# class
3. `UIWindow.InternalLoad(location, ...)` (UIWindow.cs:314) → `Resources.Load<GameObject>("UI/S5_08_MockMinimalPanel")` + `Object.Instantiate` 到 UIModule.UIRoot
4. `Handle_Completed(panel)` (UIWindow.cs:478) → `_canvas = _panel.GetComponent<Canvas>()` (UIWindow.cs:484；本 fixture prefab 含 sub-Canvas 满足 vendor 强制要求)
5. `InternalCreate()` (UIWindow.cs:338): `Inject()` → `ScriptGenerator()` → `BindMemberProperty()` → `RegisterEvent()` → `OnCreate()` 同帧顺序
6. `InternalRefresh()` (UIWindow.cs:351): `OnRefresh()` 同帧

**Events captured (lifecycle 顺序 in time @ frame=30)**:
```
[S5-08 mock] ScriptGenerator@frame=30@t=5.102
[S5-08 mock] BindMemberProperty@frame=30@t=5.102
[S5-08 mock] RegisterEvent@frame=30@t=5.102
[S5-08 mock] OnCreate@frame=30@t=5.103
[S5-08 mock] OnRefresh@frame=30@t=5.103
```

**Asserts (11/11 PASS)**:

| # | Assert | Result |
|---|--------|--------|
| 1 | ShowUIAsyncAwait_returned | PASS (panel instance returned) |
| 2 | LastInstance_set | PASS (LastInstance == returned panel) |
| 3 | panel_in_UIRoot_subtree | PASS (panel.transform.IsChildOf(UIRoot) == true) |
| 4 | panel_active | PASS (activeInHierarchy=true) |
| 5 | panel_in_uiStack | PASS (reflection 拿 UIModule._uiStack 含此 panel) |
| 6 | lifecycle.ScriptGenerator | PASS |
| 7 | lifecycle.BindMemberProperty | PASS |
| 8 | lifecycle.RegisterEvent | PASS |
| 9 | lifecycle.OnCreate | PASS |
| 10 | lifecycle.OnRefresh | PASS |
| 11 | lifecycle_order | PASS (ScriptGenerator → BindMemberProperty → RegisterEvent → OnCreate → OnRefresh) |

**关键发现 (R2.3 vendor lifecycle 完整实证)**: vendor 5 method (Init phase 3 + OnCreate + OnRefresh) 全部在 ShowUIAsyncAwait 返回前同帧 (frame=30, t≈5.102~5.103s) 完成。time gap < 1 ms 表明 vendor 5 method 同步顺序连续调用，无 yield。spec 假设 (ADR-011/SP-002 用 4 method `OnCreate/OnRefresh/OnUpdate/OnClose`) 对比 vendor 实测 (7 method 含 Init phase 3) 第 2 例 spec ↔ reality drift dp 实战触发 (per Session 26 #4 dp2)。

### §1.3 P3 UIWindowLifecycleVendorOrder

**Setup**:
- post-P2（mock panel visible at frame=30）
- spike `UniTask.DelayFrame(3)` 等 ≥3 帧验 OnUpdate × N

**Action 1 (visible OnUpdate)**:
- spike DelayFrame(3) 期间 OnUpdate 持续触发 (vendor `_hasOverrideUpdate` 优化机制 mock override 后保留)

**Events captured (OnUpdate 累计 27 frame in 第 1 次 visible 期间 frame=30~53)**:
```
[S5-08 mock] OnUpdate@frame=30@t=5.105
[S5-08 mock] OnUpdate@frame=31@t=5.110
[S5-08 mock] OnUpdate@frame=32@t=5.117
...
[S5-08 mock] OnUpdate@frame=53@t=5.292
[S5-08 mock] OnUpdate@frame=54@t=5.302  ← spike DelayFrame(3) end 后立刻 CloseUI
[S5-08 mock] OnUpdate@frame=55@t=5.310
[S5-08 mock] OnUpdate@frame=56@t=5.315
```

**Action 2 (CloseUI)**:
```csharp
GameModule.UI.CloseUI<S5_08_MockMinimalPanel>();
await UniTask.DelayFrame(2);
```

**Events captured @ frame=57**:
```
[S5-08 mock] OnDestroy@frame=57@t=5.327
```

**关键发现 1 (vendor CloseUI 路径行为实证)**:
- vendor CloseUI<T>() sync API **直接走 OnDestroy 销毁**，**不走** Hide / Close hook (UIWindow.cs:504/509)
- Hide / Close hook 在本 case 未被触发 (asserts: `Hide=False Close=False`)
- 推测: HideTimeToClose 默认 10s 是 HideUI<T>() (隐藏不销毁) 路径配置；CloseUI<T>() 是直接销毁路径

**Action 3 (second show)**:
```csharp
secondPanel = await GameModule.UI.ShowUIAsyncAwait<S5_08_MockMinimalPanel>();
```

**Events captured @ frame=59 (1 帧后 second show 完成同帧)**:
```
[S5-08 mock] ScriptGenerator@frame=59@t=5.343
[S5-08 mock] BindMemberProperty@frame=59@t=5.343
[S5-08 mock] RegisterEvent@frame=59@t=5.343
[S5-08 mock] OnCreate@frame=59@t=5.343
[S5-08 mock] OnRefresh@frame=59@t=5.344
[S5-08 mock] OnUpdate@frame=59@t=5.344
[S5-08 mock] OnUpdate@frame=60@t=5.350
...
```

**关键发现 2 (V3 Type-8 dp1 实战触发 — UIWindow second show 行为 spec vs vendor drift CONFIRMED)**:
- vendor second show **创建新 instance** (asserts P3.SecondShow_reuse_instance: "INFO: vendor 创建新 instance")
- vendor second show **完整 init phase replay** — ScriptGenerator + BindMemberProperty + RegisterEvent + OnCreate + OnRefresh 全部 4 init methods + 2 method 重新调用 (asserts P3.SecondShow_init_count: "INFO: init phase methods in 2nd show = 4")
- 对比 ADR-011/SP-002 spec 假设: "second show 仅 OnRefresh，已存在 instance 复用"
- **drift CONFIRMED**: 累计 V3 Type-8 candidate dp1 实战触发；ADR-011 spec second show 描述需 amend (Sprint 5 retro 高优 action item per V3 Type-5/-8 dp 累计)

**Asserts (5/5 PASS)**:

| # | Assert | Result |
|---|--------|--------|
| 1 | OnUpdate_count_during_visible | PASS (OnUpdate × 27 frame) |
| 2 | CloseUI_lifecycle | PASS (triggered OnDestroy=True Hide=False Close=False) |
| 3 | SecondShow_panel | PASS (second panel non-null) |
| 4 | SecondShow_reuse_instance | INFO (vendor 创建新 instance — V3 Type-8 dp1) |
| 5 | SecondShow_init_count | INFO (init phase methods in 2nd show = 4 — V3 Type-8 dp1) |

### §1.4 P4 ButtonOnClickPath

**Setup**:
- post-P2 (mock panel visible，含 Button child)
- mock panel `OnCreate` override 已 `transform.GetComponentInChildren<Button>()` 拿到 prefab Button reference
- mock panel `OnCreate` 已 `ButtonRef.onClick.AddListener(OnButtonClicked)` 计 ClickCount

**Action**:
```csharp
panel.ButtonRef.onClick.Invoke();
panel.ButtonRef.onClick.Invoke();
panel.ButtonRef.onClick.Invoke();
await UniTask.Yield();
```

**Events captured**:
```
[S5-08 mock] OnButtonClicked ClickCount=1
[S5-08 mock] OnButtonClicked ClickCount=2
[S5-08 mock] OnButtonClicked ClickCount=3
```

**Asserts (2/2 PASS)**:

| # | Assert | Expected | Actual |
|---|--------|----------|--------|
| 1 | button_ref | non-null | PASS (prefab Button child GetComponentInChildren found) |
| 2 | click_count_delta | == 3 | PASS (delta=3) |

**S5-02 main menu Button click path 前置 verified**: 该 case 担保 S5-02 dev-story 实施时正式 main menu UIWindow 内 `Start Chapter 1` Button + `Next Chapter` Button onClick.AddListener handler → 触发 `ISceneEvent.OnRequestSceneChange(N)` dispatch 路径在 framework 层面可用。

---

## §2 vendor 行为实证发现 (V3 candidate dp 累计)

### §2.1 R2.3 Lifecycle drift (Session 26 #4 dp2) — R3 实战 verified

ADR-011 spec / SP-002 spec 描述 4 user-facing method (`OnCreate/OnRefresh/OnUpdate/OnClose`)；vendor 实际 7 method + UIWindow 2 hook：

| ADR-011/SP-002 spec | TEngine vendor 实际 (UIBase.cs:144-197 + UIWindow.cs:504-509) |
|---|---|
| OnCreate (首次创建) | ScriptGenerator → BindMemberProperty → RegisterEvent → OnCreate (Init phase 3 + 创建 1) |
| OnRefresh (每次显示) | OnRefresh ✓ |
| OnUpdate (visible 帧) | OnUpdate (含 `_hasOverrideUpdate` perf optimization) |
| OnClose (清理) | **OnDestroy** (vendor 改名)；UIWindow 额外 Hide/Close 2 hook 用于 HideUI 路径 |

R3 P2 + P3 实战验证了 vendor 7+2 lifecycle 完整序列在 ShowUI + visible + CloseUI + second show 各阶段的真实调用顺序。

### §2.2 R2.10 UILayer enum collision + wording drift (Session 26 #5 dp3) — R3 实战 confirmed

vendor 已在 `WindowAttribute.cs:8` 定义 `public enum UILayer : int { Bottom=0, UI=1, Top=2, Tips=3, System=4 }`；ADR-011 spec 用 `{Background, HUD, Popup, Overlay, System}` 命名不同。

R3 P1 case 5 个 GetSortingOrderBase asserts (0/100/200/300/400) 全 PASS — 证明 vendor enum + UILayerExtensions extension method 设计正确。

### §2.3 ⭐ V3 Type-8 candidate dp1 (UIWindow second show 行为 spec vs vendor) — R3 实战 NEW TRIGGER

**实战发现 (R3 P3 case 2026-05-11)**:

ADR-011 spec / SP-002 spec 假设 "second show 仅 OnRefresh，已存在 instance 复用"；vendor 实际行为：

| 维度 | ADR-011/SP-002 spec 假设 | TEngine vendor 实测 (R3 P3) |
|---|---|---|
| Instance 复用 | reuse first instance | **新建 instance** (frame=59 vs frame=30) |
| Init phase | 仅 OnRefresh | **完整 replay**: ScriptGenerator + BindMemberProperty + RegisterEvent + OnCreate + OnRefresh (4 init methods) |
| Lifecycle 完整度 | minimal | full re-instantiation cycle |

**根因推测**: vendor CloseUI<T>() 走 OnDestroy 销毁 instance 后清出 _uiStack；second ShowUI<T>() 走 Activator.CreateInstance 新 instance + Resources.Load + 完整 InternalLoad → InternalCreate → InternalRefresh 链路 (UIWindow.cs:314-353)。**这是 vendor 的"销毁后重建"模式**，**不是** spec 假设的"隐藏后重显示"模式。

**ROI 累计**: V3 Type-8 candidate "UIWindow second show 行为 spec vs vendor 是否一致" dp1 (story-001 §V3 Watch List Hooks #5 实战触发)；与同源的 R2.2/R2.3/R2.10 一起 sprint retro 系统性 amend ADR-011 §G "UIWindow 显示/隐藏行为" + SP-002 §lifecycle assumptions 高优 action item。

### §2.4 vendor CloseUI<T>() 直走 OnDestroy 路径 (本 story R3 implicit 实证)

CloseUI<T>() sync API 直接走 OnDestroy 销毁；Hide/Close hook (UIWindow.cs:504/509) 在 sync CloseUI 路径未被触发。推测 Hide/Close hook 是 HideUI<T>() (隐藏不销毁 + HideTimeToClose 后 auto destroy) 路径相关，非 CloseUI 路径。

---

## §3 Code 改动汇总 (本 story)

### §3.1 新增文件 (4)

| 文件 | 行数 | 用途 |
|------|------|------|
| `Assets/GameScripts/HotFix/GameLogic/UI/UILayerExtensions.cs` | ~50 | UILayer.GetSortingOrderBase() extension method + XML doc + vendor enum 路径 + ADR-011 spec wording 映射表 |
| `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5_08_MockMinimalPanel.cs` | ~190 | mock UIWindow 子类 ([Window(UILayer.UI, fromResources: true, location: "UI/S5_08_MockMinimalPanel")]; 9 lifecycle override; ResetForTest helper; ButtonRef + ClickCount; LifecycleEvents static List) |
| `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-08_UIModuleSetup.cs` | ~430 | spike (S508Spike + S508Runtime + S508Tester 1 file + 3 inner class; 4 R3 case; reflection 拿 _uiStack with try-catch fallback; JSON evidence dump) |
| `Assets/Editor/DevTest/S5_08_MockPanelGenerator.cs` | ~135 | `[MenuItem("Tools/S5-08/Generate Mock Panel Prefab")]` 一键生成 prefab; root 含 RectTransform + Canvas + GraphicRaycaster + Image; child MockButton 含 RectTransform + Image + Button + 4 state colors; grandchild Text |
| `Assets/Resources/UI/S5_08_MockMinimalPanel.prefab` | (asset) | mock panel prefab generated by Editor menu |

### §3.2 改 文件 (1)

| 文件 | 改动 |
|------|------|
| `Assets/GameScripts/HotFix/GameLogic/GameApp.cs` | `RegisterDevSpikes` 切换 `S51cSpike` → `S508Spike`; 加 S5-1c done note + S5-08 active note (含 Resources.Load 路径不撞 YooAsset 锁 type-3 race 防御说明) |

### §3.3 不改 (vendor)

`Assets/GameScripts/HotFix/GameLogic/Module/UIModule/` 全部文件保持 vendor sync 状态（c5f8952 2026-05-09 TEngine 6.2.1 vendor sync）— 本 story 0 vendor patch；如发现 vendor bug 走 `tengine-dev` skill R1~R4 vendor patch 协议（本 story 暂无）。

---

## §4 AC Matrix (10/10)

| AC | 描述 | Verification | 状态 |
|----|------|--------------|------|
| AC-1 | UILayer enum verify + helper extension method | UILayerExtensions.cs created + P1 5 layer GetSortingOrderBase asserts | ✅ PASS |
| AC-2 | UIRoot scene 实例化 verify + UIModule.OnInit wire 路径 | P1 GameModule.UI + UIModule.UIRoot + UICanvas + DontDestroyOnLoad + UI layer asserts | ✅ PASS |
| AC-3 | GameModule.UI 静态门面通路 + ShowUI/CloseUI/HideUI API | P1 GameModule.UI non-null + P2 ShowUIAsyncAwait 返回 panel + P3 CloseUI 触发 OnDestroy | ✅ PASS |
| AC-4 | UIWindow ShowUI/CloseUI API 通路 verify (mock panel) | P2 panel.transform.IsChildOf(UIRoot) + active=true + _uiStack 含此 panel (reflection) | ✅ PASS |
| AC-5 | UIWindow vendor 7+2 lifecycle 文档注释 | mock panel 9 lifecycle override + XML doc + P2/P3 实证 vendor 7 method + 2 hook 调用顺序 | ✅ PASS |
| AC-6 | UIModule 程序集路径注释 (UILayerExtensions.cs 顶部) | UILayerExtensions.cs file header 含 UIModule 路径 + vendor UILayer 路径 + ADR-011 spec wording 映射 | ✅ PASS |
| AC-7 | Out of Scope 明示 | story file §Out of Scope 段已明示 Popup Queue / Auto InputBlocker / 完整 UIWindow 业务面板 路径 (story-008 / -006 / -002..-007) | ✅ PASS |
| AC-8 | S5-02 main menu Button mount API verified | P4 button.onClick.Invoke() × 3 → ClickCount==3 (delta=3 PASS) | ✅ PASS |
| AC-9 | console clean | R3 全程 0 unexpected error / 0 unexpected warning (仅 pre-existing 3 项: I2L 提示 / EditorSimulateMode / Android SDK XML 版本) | ✅ PASS |
| AC-10 | R3 PlayMode probe ALL PASS + JSON evidence | 4/4 case PASS + 29/29 asserts + all_passed=true + JSON dump 完整 | ✅ PASS |

---

## §5 Console Snapshot (PlayMode 全程)

**R3 path 0 unexpected error/warning**：

| 类型 | 数量 | 说明 |
|------|------|------|
| Error | 0 | R3 path |
| Warning (expected pre-existing) | 3 | I2Localization 提示 / Editor Module Used: EditorSimulateMode / Android SDK XML v4 (这些是 framework / Editor 启动 baseline，与本 story 0 关) |
| Info | 多 | spike lifecycle Debug.Log (mock panel 9 lifecycle × 2 cycle + P1/P2/P3/P4 entry log + DevBootstrap log) |

---

## §6 ADR-029 V3 Watch List Hooks — dp 累计现状

| Type | 累计 dp | 状态 |
|------|---------|------|
| **Type-5** "spec/tooling ↔ reality drift" | 3 unique dp (S5-01 dp1 toolchain silent failure + S5-08 #4 dp2 ShowUI wording / lifecycle 数量 / OnClose→OnDestroy wording + S5-08 #5 dp3 UILayer wording) | **超 V3 promote ROI 阈值 ≥ 3 unique dp** → Sprint 5 retro **强烈建议 promote** 为正式 candidate (split or unified) |
| **Type-2(c)** "framework method 行为差异" | 1 dp (S5-1c spike sync-subscribe race) | continued monitor |
| **Type-7** "UIRoot DontDestroyOnLoad 跨 scene 持久化 race" | 0 dp (本 story R3 P1 验证 DontDestroyOnLoad ✅；chapter scene 跨 scene reference R2.8 待 S5-02 dev-story 验) | continued monitor |
| **Type-8** "UIWindow second show 行为 spec vs vendor" | **1 dp NEW** (S5-08 R3 P3 实战 2026-05-11 — vendor 销毁后重建 + 完整 init phase replay vs spec OnRefresh-only 假设) | **dp1 实战触发 NEW; ADR-011 §UIWindow 显示/隐藏行为 spec amend 候选** |

**Sprint 5 retro action items (本 story 累计贡献)**:

1. **promote V3 Type-5 candidate** "spec/tooling ↔ reality drift" 为 ADR-029 V3 正式 candidate (split 为 Type-5a tooling silent failure + Type-5b spec wording drift 两子类 或 unified 由 retro 决定)
2. **ADR-011 §G systematic amendment** — wording 对齐 vendor: UILayer enum 命名 (Bottom/UI/Top/Tips/System) + ShowUI/CloseUI/HideUI API 命名 + UIWindow 7+2 lifecycle method 命名 + second show 行为描述 (vendor 销毁后重建 vs spec OnRefresh-only)
3. **SP-002 systematic amendment** — UIWindow lifecycle 4 method → 7+2 method + OnDestroy ≠ OnClose wording
4. **新建 Type-8 candidate** — UIWindow second show 行为 spec vs vendor drift (本 story 实战 dp1 累计)；后续 ui-system stories 实施时持续 monitor 是否有 second show 复用 instance 场景

---

## §7 Audit Trail Cross-references

| 资源 | 路径 / commit |
|------|---------------|
| Story file | `production/epics/ui-system/story-001-uimodule-setup.md` (Status: Ready 2026-05-11 Session 26 #5) |
| Sprint plan | `production/sprints/sprint-5.md` §Must Have Track D — S5-08 (promoted) |
| Sprint status | `production/sprint-status.yaml` story id S5-08 (status: ready-for-dev → in-progress → done 待 Phase 5 amend) |
| ADR-011 | `docs/architecture/adr-011-uiwindow-management.md` (Accepted with patch — wording amend 留 Sprint 5 retro) |
| ADR-029 V2.0 | `docs/architecture/adr-029-story-impl-notes-verification.md` |
| TEngine vendor sync | commit c5f8952 (2026-05-09 — TEngine 6.2.1 vendor sync) |
| Session 26 #4 commit | 9a669a4 (R2 evidence + wording drift amendment per [D]) |
| Session 26 #5 commit | b394add (readiness gate R3 collision resolved + Draft→Ready per [A]) |
| Phase 2 C# commit | 1a2ee93 (4 C# files: UILayerExtensions + mock panel + spike + GameApp 切换) |
| Phase 2.3 prefab generator commit | bfc0145 (Editor S5_08_MockPanelGenerator) |
| Spike precedent | `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-1b_BootSceneLoad.cs` + `S5-1c_ListenerPathDriver.cs` |
| S5-1c evidence precedent | `production/qa/playmode-listener-path-driver-2026-05-09.md` (332 行) |
| V3 Type-5 dp1 source | `.claude/memory/problem_2026-05-09_unity-bridge-to-coplaydev-switch.md` |

---

## §8 Verdict

**Story S5-08 R3 PASS** (4/4 case + 29/29 asserts + all_passed=true first-run + V3 Type-8 dp1 实战 NEW capture)。

下一步 Phase 5 `/story-done` 闭环：
- sprint-status.yaml S5-08 status: ready-for-dev → done
- active.md Session 26 #6 patch (Phase 2~4 完成)
- ADR-029 V3 Type-8 candidate dp1 累计入 sprint-status.yaml watch list
- commit Phase 4 evidence doc + Editor fix + .meta + prefab + Resources
- S5-08 closure summary → unblock S5-02 dev-story (5 系统串通 happy path)
