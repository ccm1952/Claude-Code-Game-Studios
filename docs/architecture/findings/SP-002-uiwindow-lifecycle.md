// 该文件由Cursor 自动生成

# SP-002 Findings: UIWindow 生命周期调用时序

> **Status**: ✅ 已验证（源码审查 + Sprint 5 S5-08 R3 PlayMode 实证）
> **Date**: 2026-04-22 (initial) / **2026-05-13 amend** (Sprint 6 S6-05 systematic wording amend — align vendor 7+2 lifecycle reality + V3.0 §V3-1.b Type-8 destroy-and-recreate finding)
> **Source**: `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIModule.cs`（vendor under HotFix），`Assets/TEngine/Runtime/Module/UIModule/UIBase.cs:144-197`，`UIWindow.cs:504-509`，`WindowAttribute.cs:8` + WindowAttribute.cs:21
> **Verifying Story**: Sprint 5 S5-08 UIModule Setup R3 P3 `UIWindowLifecycleVendorOrder` (4/4 R3 PASS 2026-05-11) — `production/qa/playmode-uimodule-setup-2026-05-11.md`
> **V3 catch**: ADR-029 V3.0 §V3-1.b Type-5 dp2 (ShowUI wording) + dp3 (UILayer enum) + **Type-8 dp1** (UIWindow second show vendor 销毁后重建 vs spec hide-and-reshow)

## 重要发现

**UIModule 不在 TEngine Runtime 中，而是在 GameLogic 热更程序集中实现。** 路径为 `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/`。

**vendor 实际 lifecycle 是 7+2 hook，不是早期 spec 假设的 4 hook**。早期 spec wording（OnCreate/OnRefresh/OnUpdate/OnClose 4 hook）与 vendor reality drift 已通过本 SP-002 amend + ADR-011 §G amend (Sprint 6 S6-05) 对齐。

## Vendor 实际 API（per UIModule.cs:250-460 + UIBase.cs:144-197 + UIWindow.cs:504-509）

### Show / Close / Hide API

| 业务侧调用 | Vendor 实际 API | 说明 |
|------------|-----------------|------|
| 显示窗口（同步） | `GameModule.UI.ShowUI<T>(...)` | 同步形态；如 prefab 未 loaded → 先 load 再 show |
| 显示窗口（异步） | `GameModule.UI.ShowUIAsync<T>(...)` | 异步形态；返回 Task/UniTask；prefab load 异步 |
| 显示窗口（await） | `GameModule.UI.ShowUIAsyncAwait<T>(...)` | 异步形态 + await ready；适合 sequence |
| 关闭窗口 | `GameModule.UI.CloseUI<T>()` | **销毁** instance（destroy）+ 从 _uiStack 移除 |
| 隐藏窗口（保留 instance） | `GameModule.UI.HideUI<T>()` | 隐藏但 instance 保留（用于 HideTimeToClose 路径） |
| 全部关闭 | `GameModule.UI.CloseAll(bool)` | bool = 是否包含 System 层 |

> ⚠️ **Legacy spec wording**: `ShowWindow<T>()` / `CloseWindow<T>()` — 是 Sprint 0-3 framework 时假设的 API 命名；vendor 实际**从未存在**此 API。Sprint 5 S5-08 R2.2 evidence collection (Session 26 #4) 首次实证 + Sprint 6 S6-05 systematic amend 完整 propagate fix。

## 生命周期时序（vendor 7+2 hook 完整）

### 首次打开 (ShowUI - first instance)

```
GameModule.UI.ShowUI<T>(...) [or ShowUIAsync<T>]
  → Activator.CreateInstance(typeof(T))      [vendor: new instance per show cycle]
  → Resources.Load + GameObject.Instantiate  [prefab via [Window(UILayer.X, fromResources: true)] attribute]
  → ScriptGenerator()                         [init phase 1: UIBase.cs:148]
  → BindMemberProperty()                      [init phase 2: UIBase.cs:152]
  → RegisterEvent()                           [init phase 3: UIBase.cs:156]
  → OnCreate()                                [init phase 4: UIBase.cs:165]    ← 业务首次入口
  → OnRefresh()                               [init phase 5: UIBase.cs:177]    ← 业务刷新入口
  → OnUpdate() × N                            [per-frame: UIBase.cs:192，仅 IsPrepare && Visible 时执行]
```

**业务侧典型 override**：`OnCreate` (绑定 UI 引用 + AddUIEvent) + `OnRefresh` (刷新 data binding) + `OnUpdate` (per-frame logic 可选)。

### 重新打开 (vendor 'destroy-and-recreate' 模式 — ⭐ V3.0 §V3-1.b Type-8 dp1)

```
[已有 instance + visible 时调 CloseUI<T>]
  → OnDestroy()                               [destroy phase: UIBase.cs:197]    ← 业务 cleanup 入口
  → _uiStack.Remove(window)                   [vendor: instance 完全销毁]

[再次调 GameModule.UI.ShowUI<T>(...)]
  → Activator.CreateInstance(typeof(T))      [**新 instance** — vendor 销毁后重建 destroy-and-recreate]
  → 完整重新走 ScriptGenerator → BindMemberProperty → RegisterEvent → OnCreate → OnRefresh → OnUpdate
```

> ⚠️ **Legacy spec drift**: 早期 SP-002 v1 (2026-04-22) 假设 vendor 是 'hide-and-reshow' 模式（second show 复用 instance，仅触发 OnRefresh，**OnCreate 不再调用**）。**Sprint 5 S5-08 R3 P3 PlayMode 实证完全推翻此假设** — vendor 实际是 destroy-and-recreate 模式：second show 创建新 instance + 完整 init phase replay。**业务侧设计含义**：
> - `OnCreate` **每次 show 都会调用**（不止首次）— 不应在 OnCreate 内做 "一次性昂贵初始化"，应做 "实例性 wire-up"
> - `RegisterEvent` 每次 show 都会调用 — listener subscribe 必须配 OnDestroy 内 unsubscribe（避免 duplicate listener）
> - main menu UIWindow 频繁 show/close 不是 "cheap toggle"，每次重建 init 4 method — 如果是 HUD 类常驻 panel 应避免频繁 close
>
> **真实源**：`production/qa/playmode-uimodule-setup-2026-05-11.md` §P3 vendor 销毁后重建实证 (frame=30 first show / frame=59 second show 新 instance) + ADR-029 V3.0 §V3-1.b Type-8 dp1 NEW

### OnUpdate 触发条件（vendor 内部 guard）

```csharp
// vendor UIBase.cs:192 内部 guard
if (!IsPrepare || !Visible)
    return false; // OnUpdate 不执行
```

仅 `IsPrepare == true && Visible == true` 时每帧调用。

### HideTimeToClose 行为（vendor 'HideUI + Timer to CloseUI' 路径）

| 值 | 行为 |
|----|------|
| ≤ 0 | 立即 `CloseUI` |
| > 0 | `Visible = false`, `IsHide = true` → Timer 到期后 `CloseUI` |

- `CancelHideToCloseTimer` 可取消定时关闭（清除 IsHide + RemoveTimer）
- Hide 期间 `Visible = false`，因此 **OnUpdate 不执行**
- 如 HideTimeToClose > 0 期间 再次 `ShowUI<T>` → 进 "Hide 期间 reshow" 路径（**not** destroy-and-recreate；保留 instance）；只触发 OnRefresh

> **注意**：HideTimeToClose > 0 路径才是早期 SP-002 v1 假设的 "hide-and-reshow" 模式；`CloseUI<T>` 直调路径才是 vendor 'destroy-and-recreate' 模式。两路径 **不同**。

## 7+2 Lifecycle Method 列表（vendor UIBase.cs:144-197 + UIWindow.cs:504-509 实证）

| Phase | Method | 触发时机 | 推荐业务用途 | 业务侧可 override |
|-------|--------|---------|---------|-----------------|
| Init 1 | `ScriptGenerator()` | 新 instance 创建后立即（vendor 内部）| TEngine 内部脚本初始化 | 通常不 override（vendor 内部用）|
| Init 2 | `BindMemberProperty()` | ScriptGenerator 之后 | TEngine 内部属性绑定 | 通常不 override |
| Init 3 | `RegisterEvent()` | BindMemberProperty 之后 | **业务 listener subscribe**（GameEvent.AddEventListener / AddUIEvent）| ✅ 业务推荐 override |
| Init 4 | `OnCreate()` | RegisterEvent 之后 | **业务 UI 引用绑定**（FindChild / GetComponent）+ 静态资源 setup | ✅ 业务推荐 override |
| Init 5 | `OnRefresh()` | OnCreate 之后（首次）+ 每次 show（含 destroy-and-recreate 新 instance）| **业务数据刷新**（data binding / userDatas 参数处理）| ✅ 业务推荐 override |
| Loop | `OnUpdate()` | per-frame，仅 IsPrepare && Visible 时执行 | 实时数据轮询 / 动画 / 倒计时 | ✅ 业务可 override（按需）|
| Destroy | `OnDestroy()` | `CloseUI<T>()` 调用 OR 全部 CloseAll | **业务 cleanup**（unsubscribe events / 释放引用 / null-out _handler）| ✅ 业务推荐 override |
| Extra 1 | `Hide()` | `HideUI<T>()` 调用（保留 instance）| 隐藏时业务挂钩（如 pause animation） | ✅ 业务可 override（按需）|
| Extra 2 | `Close()` | `CloseUI<T>()` 路径 + OnDestroy 之前 | 业务 close 入口（弃用建议：用 OnDestroy） | ⚠️ 业务可 override 但 deprecated |

> **OnDestroy ≠ OnClose**：早期 spec wording 用 `OnClose`，vendor 实际是 `OnDestroy`。Sprint 5 S5-08 R2.3 evidence collection (Session 26 #4) 实证。

> **Visibility modifier: `protected virtual` / `protected override`** (V3.0 §V3-1.b Type-5 **dp7 NEW** — S6-07 Phase 0 R2 verify surfaced 2026-05-13): vendor `UIBase.cs:144/151/158/165/172/184/197` 7 lifecycle method + `UIWindow.cs:504/509` extra 2 hook (`Hide` / `Close`) 全部签名是 `protected virtual void XxxName()`（不是 `public virtual`）。**业务侧 override 必须用 `protected override`** (LogUI.cs sample + S5-02 production MainMenuPanel.cs 一致；如用 `public override` 触发 `CS0507: cannot change access modifiers when overriding` 编译错)。Sprint 6 S6-05 commit 45ae96b ADR-011 §G Key Interfaces code block 5 处 `public override` 是新 spec wording drift，已通过 2026-05-13 evening hotfix amend 修正。

## UILayer enum（vendor WindowAttribute.cs:8 实证）

```csharp
// vendor 实际 enum (namespace GameLogic)
public enum UILayer : int
{
    Bottom = 0,
    UI = 1,
    Top = 2,
    Tips = 3,
    System = 4
}
```

| Vendor enum value | 业务 mapping (per ADR-011 §UI Layer Levels) | Sort Order Base |
|-------------------|--------------------------------------------|:---------------:|
| `Bottom` | Background / 预留 | 0 |
| `UI` | HUD / 游戏 HUD | 100 |
| `Top` | Popup / 模态弹窗 | 200 |
| `Tips` | Overlay / 全屏覆盖层 + Tooltip | 300 |
| `System` | System / 系统指示器 | 400 |

> ⚠️ **Legacy spec drift**: 早期 ADR-011 spec 用 `{Background, HUD, Popup, Overlay, System}` enum value 命名；vendor 实际是 `{Bottom, UI, Top, Tips, System}` 命名。Sprint 5 S5-08 R2.10 evidence collection (Session 26 #5) 实证 + Sprint 6 S6-05 systematic amend 完整 align。**业务侧用法**：`UILayerExtensions.cs` 提供 `GetSortingOrderBase(this UILayer)` extension method 桥接 vendor enum 与 sort order base。

## [Window] Attribute（vendor WindowAttribute.cs:21 实证）

```csharp
// vendor 4 ctor overload
[Window(UILayer.UI, fromResources: true)]                                // (UILayer, fromResources)
[Window(UILayer.UI, sortOrderOffset: 10)]                                 // (UILayer, sortOrderOffset)
[Window(UILayer.UI, fromResources: true, sortOrderOffset: 10)]            // (UILayer, fromResources, sortOrderOffset)
[Window(UILayer.UI, fromResources: true, sortOrderOffset: 10, fullScreen: true)]  // (UILayer, fromResources, sortOrderOffset, fullScreen)
public sealed class HUDPanel : UIWindow { ... }
```

业务侧 panel class 标注此 attribute 后，vendor `UIModule.OnInit()` 自动通过 reflection 创建 instance 时按 attribute 分配 layer + sortOrder。

## 编码规范（基于 vendor 实证 + Sprint 5 S5-08 V3.0 §V3-1.b align）

| 回调 | 触发时机 | 推荐用途 | 业务侧 override 必要性 |
|------|---------|---------|---------------------|
| `ScriptGenerator` | 新 instance 创建后立即 | vendor 内部用 | ❌ 通常不 override |
| `BindMemberProperty` | ScriptGenerator 之后 | vendor 内部属性绑定 | ❌ 通常不 override |
| `RegisterEvent` | BindMemberProperty 之后 | **GameEvent listener subscribe + AddUIEvent button onClick subscribe** | ✅ 必备 override 入口 |
| `OnCreate` | RegisterEvent 之后 | UI 引用绑定 (FindChild / GetComponent) + 静态资源 setup | ✅ 推荐 override |
| `OnRefresh` | OnCreate 之后（首次 + 每次 show）| 数据绑定 (userDatas 参数处理) + UI 内容刷新 | ✅ 推荐 override |
| `OnUpdate` | per-frame，仅 Visible == true | 实时数据轮询（HintButton opacity / 倒计时）| ⚠️ 可选 override（按需）|
| `OnDestroy` | `CloseUI<T>()` 路径 OR 全部 CloseAll | **GameEvent listener unsubscribe + null-out _handler + 资源释放** | ✅ 必备 override 入口 |
| `Hide` | `HideUI<T>()` 路径 | 隐藏时挂钩（如 pause animation） | ⚠️ 可选 override |
| `Close` | `CloseUI<T>()` 路径 + OnDestroy 之前 | （deprecated：用 OnDestroy） | ⚠️ 业务可 override 但 deprecated |

## V3.0 §V3-1.b Sprint 6 amend 闭环

本 SP-002 amend 与以下 ADR/SP file 系统性 align（Sprint 6 S6-05 AI-2 衍生 propagation per V3.0 §V3-1.b 修复模式）：

| File | Amend Scope | 状态 |
|------|-------------|------|
| **SP-002 (本文件)** | 整 doc rewrite — vendor 7+2 lifecycle reality + Type-8 destroy-and-recreate finding | ✅ Sprint 6 S6-05 (本次) |
| **ADR-011 §Architecture / §UI Layer Levels / §Key Interfaces / §Implementation Guidelines** | vendor wording (ShowUI/CloseUI/HideUI + 7+2 lifecycle + UILayer enum)；§G UIWindow 显示/隐藏行为 spec amend | ✅ Sprint 6 S6-05 (next commit) |
| **ADR-014 §A migration table line 357** | IPuzzleLockEvent reference 对齐 production reality（InputBlocker single-layer 替代）| ✅ Sprint 6 S6-05 |
| **ADR-016 §A migration table line 367-368** | 同上 align | ✅ Sprint 6 S6-05 |
| **ADR-017 §B AudioModule init 路径** | drift-v2-(a) supersede 2026-05-09 已 partial amend；本次追加 V3.0 §V3-1.b reference + §History | ✅ Sprint 6 S6-05 |
| **ADR-028 §1 AudioModule activation gate** | 同上；drift-v2-(a) supersede 已 partial amend；本次追加 V3.0 §V3-1.b reference + §History | ✅ Sprint 6 S6-05 |

## 行动项（Sprint 5 → Sprint 6 闭环）

- [x] 确认 OnCreate → OnRefresh 同帧顺序 — 已确认（2026-04-22 initial）
- [x] 确认 Hide 期间 OnUpdate 不执行 — 已确认（2026-04-22 initial）
- [x] UIModule 在 GameLogic 热更程序集 — 已确认（2026-04-22 initial；ADR-011 §G needs noting）
- [x] **vendor 7+2 lifecycle method 实证完整列出** — Sprint 5 S5-08 R3 P3 PlayMode 实证 2026-05-11
- [x] **vendor 'destroy-and-recreate' 模式实证** — Sprint 5 S5-08 R3 P3 PlayMode frame=30/59 实证 + Type-8 dp1 NEW
- [x] **UILayer enum vendor wording 实证** — Sprint 5 S5-08 R2.10 evidence collection 2026-05-11
- [x] **vendor [Window] attribute 4 ctor overload 实证** — Sprint 5 S5-08 R2.11 evidence collection 2026-05-11
- [x] **SP-002 整 doc systematic align vendor wording** — Sprint 6 S6-05 amend 2026-05-13（本次）
- [x] **ADR-011 §G systematic align vendor wording** — Sprint 6 S6-05 amend 2026-05-13（next commit per S6-05 batch）

## History

- **2026-04-22**: SP-002 v1 created — initial UIWindow lifecycle findings (Sprint 0 spike); 4 hook (OnCreate/OnRefresh/OnUpdate/OnClose); 'hide-and-reshow' 模式 spec 假设
- **2026-05-11** (Sprint 5 S5-08 R2/R3): ⚠️ Multiple vendor wording drift surfaced (R2.2 ShowWindow→ShowUI; R2.3 4 lifecycle→7+2 lifecycle; R2.10 UILayer enum {Background→Bottom, HUD→UI, Popup→Top, Overlay→Tips, System→System}; R2.11 [Window] attribute 4 ctor overload); R3 P3 PlayMode frame=30/59 vendor 'destroy-and-recreate' 模式实证（与 spec 'hide-and-reshow' 假设冲突）→ ADR-029 V3 Type-5 dp2/dp3 + Type-8 dp1 NEW
- **2026-05-12** (Sprint 5 retro AI-2 衍生): SP-002 systematic align 计入 Sprint 6 S6-05 (ADR-011/SP-002/ADR-014/-016/-017/-028) 6 file batch amend
- **2026-05-13** (Sprint 6 S6-05 systematic wording amend — V3.0 §V3-1.b 修复模式实战 propagation): SP-002 整 doc rewrite — vendor 7+2 lifecycle reality + 'destroy-and-recreate' 模式 + UILayer enum + [Window] attribute + vendor ShowUI/CloseUI/HideUI/CloseAll API + business override 推荐入口；§History 追加；与 ADR-011 §G amend (next commit) 系统性 align
- **2026-05-13 evening** (Sprint 6 S6-07 Phase 0 R2 verify hotfix — V3.0 §V3-1.b Type-5 dp7 NEW visibility modifier drift): SP-002 line 107 后插入 visibility modifier note (`protected virtual` / `protected override` 是 vendor 实际签名；`public override` 触发 CS0507 编译错)。Drift surfaced 在 S6-07 Phase 0 vendor source R2 verify (UIBase.cs:144-197 全 7 method 是 `protected virtual`)；S6-05 commit 45ae96b 引入的 `public override` 是新 spec drift，已经 hotfix 修正。dp7 NEW 是 V3.0 governance maturity 关键警示 — **ADR amend 时必须遵守 R2 协议自身 (S6-06 V2.0 §V2-1.b R2 增量子条款 read vendor source → list signatures → 逐 modifier verify)，不能假设已知 vendor reality**。讽刺地，本 dp7 是 V2.0 §V2-1.b R2 增量子条款 (S6-06 amend) 的最佳 reinforcement 实证案例。
