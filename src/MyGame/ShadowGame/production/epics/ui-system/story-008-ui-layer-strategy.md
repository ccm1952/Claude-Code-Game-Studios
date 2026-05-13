// 该文件由Cursor 自动生成

# Story: UIWindow Layer/Order Management — Popup Queue Verify + Auto InputBlocker Sender-Side (Top/Tips Layer)

> **Epic**: ui-system
> **Story ID**: ui-system-008
> **Sprint**: 6 (S6-08 — Track C UI Polish + Error)
> **Story Type**: Logic / Integration
> **Complexity Points**: 2
> **GDD Requirement**: TR-ui-002 (5 UI layer levels) + TR-ui-003 (Popup/Overlay auto InputBlocker) + TR-ui-008 (Popup queue 1 visible)
> **ADR References**: ADR-011 V3.0.1 (UIWindow Management) + ADR-027 §4 (GameEvent Interface Protocol — IInputBlockerEvent contract source) + ADR-029 V3.0.1 (R2 deficiency-flagged PASS path) + ADR-029 V2.0 §V2-1.b (Interface Method Set Fan-out Check) + SP-002 (UIWindow Lifecycle visibility modifier) + ADR-010 (InputAbstraction listener-side ⚠️ deferred Sprint 7+)
> **Status**: ✅ **Done** (Phase 0 ✅ + Phase 1 ✅ + Phase 2 ✅ + Phase 3 ✅ + Phase 4 ✅ + Phase 5 ✅ — 5/5 R3 case PASS / 35/35 asserts / `all_passed=true` Push=8 Pop=8 对称 / 0 unexpected error / V3.0.1 dp9 NEW popup queue spec wording drift closure)
> **Created**: 2026-05-13 morning (Sprint 6 Session 30 — rewrite from Sprint 0 placeholder per V3.0.1 vendor reality compliant + S6-08 sender-only narrow scope [A] decision)
> **Updated**: 2026-05-13 afternoon (Sprint 6 Session 30 Phase 2+3+4+5 — production code + R3 PlayMode 5/5 PASS + V3.0.1 dp9 NEW popup queue spec wording drift closure + Phase 4 evidence doc + Phase 5 closure)
> **Completed**: 2026-05-13 afternoon (Session 30)
> **Depends on**: S5-08 ✅ (UIModule narrow scope) + S5-05 ✅ (NarrativeSequencePlayer + IInputBlockerEvent contract + sender precedent) + S6-05 ✅ (ADR-011 §G systematic wording amend) + S6-06 ✅ (ADR-029 V2.0 §V2-1.b R2 增量子条款) + S6-07 ✅ (DevTestState `[main-menu]` mode 复用 precedent + V3.0.1 dp7 NEW protected override + R2 deficiency-flagged PASS path 实战首次完整跑通)

---

## Context

S5-08 UIModule narrow scope (Session 26 #4-6) descope rationale 明示 Popup Queue / Auto-Dequeue / Auto InputBlocker / Overlay limit / 双 InputBlocker 叠加 → ui-system-008 Sprint 6 polish (per EPIC.md §Sprint 5 Override 2026-05-11)；Sprint 6 决策 S6-08 narrow scope [A] sender-only：仅做 **UIModule.ShowUI/CloseUI/HideUI 对 Top/Tips layer panel 自动 fire `IInputBlockerEvent.OnPushBlocker/OnPopBlocker(token)`** sender-side wiring + 完整 R3 PlayMode probe verify vendor 已实现 popup queue + same-layer sorting + InputBlocker stack semantic 行为；listener-side InputBlocker singleton refactor + IInputBlockerEvent listener wiring + InputManager class 创建 + S5-05 NarrativeSequencePlayer fire-and-forget closure 留 Sprint 7+ ADR-010 InputManager epic 一并实施。

### S6-08 Goal Flow (T0 → T6)

```
T0  game boot → Sprint 6 S6-07 ✅ DONE main menu polish → user click NewGame → chapter 1 loaded → puzzle progress 中 ...
T1  game subsystem fires GameModule.UI.ShowUI<PauseMenuPanel>() (假设 future PauseMenuPanel Top layer attribute)
T2  vendor UIModule.ShowUIImp(typeof(PauseMenuPanel), isAsync=false, userDatas) → window.Init(WindowLayer=2 Top) → OnWindowPrepare → 7 init lifecycle hook 同步调用
T3  本 story 新增 UIModule.TryFireInputBlockerPush(window) helper — 检查 window.WindowLayer in {Top(2), Tips(3)} → fire `GameEvent.Get<IInputBlockerEvent>().OnPushBlocker(token = window.GetType().FullName)`
T4  IInputBlockerEvent broadcast → future Sprint 7+ ADR-010 InputManager listener subscribe → InputBlocker.PushBlocker(token) → InputBlocker.IsBlocked = true → 未来 InputManager raw touch input swallow
T5  user clicks PauseMenuPanel Resume button → GameModule.UI.CloseUI<PauseMenuPanel>() → vendor UIModule.CloseUI(Type) → window.InternalDestroy + Pop(window) + OnSortWindowDepth + OnSetWindowVisible + OnPopupClosed
T6  本 story 新增 UIModule.TryFireInputBlockerPop(window) helper — fire `OnPopBlocker(token = window.GetType().FullName)` → InputBlocker.PopBlocker(token) → IsBlocked=false → input pass-through 恢复
```

**S6-08 narrow scope rationale (sender-only [A])** — sender 完成是 listener 的前置；无 sender 则 listener 无 event 可接；本 story 完成 sender-side 后 Sprint 7+ ADR-010 InputManager 实施时仅需补 listener 一端，不需要修改 UIModule (epic boundary 清晰)。

### S5-08 narrow scope descope 残留 closure mapping

| 残留 item (per EPIC.md §Sprint 5 Override) | vendor reality | S6-08 closure |
|---|---|---|
| Popup Queue / Auto-Dequeue | ✅ vendor `UIModule.PopupQueue.cs` 已 production (priority DESC + enqueueOrder ASC tiebreak — 不 simple FIFO) | ❌ 不补 production code (R3 P3 verify only) |
| **Auto InputBlocker (Popup/Overlay 自动 push)** | ❌ NOT implemented (`UIModule.ShowUI/CloseUI` 全 0-call IInputBlockerEvent) | ✅ **核心 work — UIModule.ShowUIImp/CloseUI/HideUI sender-side fire**（本 story） |
| Overlay limit (Tips 层 panel 数限制) | ⚠️ vendor `LAYER_DEEP=2000 / WINDOW_DEEP=100` 无 panel 数限制 | ❌ 不引入新限制 (本 story 不修改 vendor sorting strategy) |
| 双 InputBlocker 叠加 (multi token stack) | ✅ `InputBlocker` stack semantic 自带 (LastIndexOf 安全弹 + duplicate token 配 duplicate pop) | ❌ 不补 production code (R3 P3 verify popup queue 3 panel 触发 3 push/pop 实证 stack semantic) |
| 同层多 panel sorting | ✅ vendor `OnSortWindowDepth` 已 production (`depth = layer * 2000 + N * 100`) | ❌ 不补 production code (R3 P4 verify only) |
| **IInputBlockerEvent listener wiring** | ❌ NOT wired (InputBlocker 0 production instance；S5-05 NarrativeSequencePlayer fire-and-forget anti-pattern) | ⚠️ DEFERRED Sprint 7+ ADR-010 InputManager epic (本 story sender 完成后 dual-source UIModule + NarrativeSequencePlayer 同向贡献) |

---

## Acceptance Criteria

- [ ] **AC-1** UIModule `ShowUIImp(Type type, bool isAsync, params object[] userDatas)` (line 298 vendor reality 实证 4 entry `ShowUI<T>/ShowUI(Type)/ShowUIAsync<T>/ShowUIAsync(Type)` 共用内部入口) 内对 Top(2)/Tips(3) layer panel 自动 fire `GameEvent.Get<IInputBlockerEvent>().OnPushBlocker(token)` 其中 `token = type.FullName` (e.g. `"GameLogic.MockTopPanel"`)；UI(1)/Bottom(0)/System(4) layer panel 不 fire (HUD pass-through to game per TR-ui-004 + Bottom 是 background 非交互 + System 是 always-on-top 系统通讯非用户交互态阻塞)
- [ ] **AC-2** UIModule `CloseUI(Type type)` (line 375) 内对 Top(2)/Tips(3) layer panel 自动 fire `OnPopBlocker(token)` 其中 `token = type.FullName` (与 AC-1 push 配对；CloseUI 调用顺序：`Pop(window)` 之前 fire OnPopBlocker — pre-destroy 顺序保证 token 配对清理)
- [ ] **AC-3** UIModule `HideUI(Type type)` (line 394) 内对 Top(2)/Tips(3) layer panel 自动 fire `OnPopBlocker(token)` (隐藏即解锁 input — vendor `HideUI` 内 `IsHide=true` → `Visible=false`，hide 是 transient 状态非真 close；token 在 hide 时 pop，re-show 时 ShowUI 再 push)
- [ ] **AC-4** UI(1) layer (e.g. MainMenuPanel + future GameHUDPanel) ShowUI/CloseUI/HideUI **不 fire** OnPushBlocker/OnPopBlocker；R3 P2 实证 push/pop count delta == 0 (与 AC-1/-3 形成层级 contrast verify)
- [ ] **AC-5** Bottom(0) / System(4) layer ShowUI/CloseUI/HideUI **不 fire** push/pop；R3 P2 verify only — 本 story narrow scope 仅 Top/Tips 双 layer 实施 auto blocker；Bottom/System layer 未来需要 input swallow 走 manual `IInputBlockerEvent.OnPushBlocker(custom_token)` (per S5-05 NarrativeSequencePlayer precedent)
- [ ] **AC-6** Token format spec — `token = type.FullName` (固定 namespace + class name string)；同 type 二次 ShowUI 触发 PushBlocker 二次 (duplicate token in stack 由 `InputBlocker.PopBlocker` LastIndexOf 安全弹回 1 个，per InputBlocker.cs:35 行实证语义)；Token format 与 NarrativeSequencePlayer (`"narrative_seq_<sequenceId>"`) 不冲突 — InputBlocker token 是 string 唯一性，无 namespace concept (future ADR-010 InputManager 实施时若需要 token registry 可 prefix scheme 统一)
- [ ] **AC-7** Popup queue 行为完整 verify (vendor `UIModule.PopupQueue.cs` 已 production)：`EnqueuePopup<T>(priority, userDatas)` 入队 + `_currentPopupType` 单 visible 控制 + `OnPopupClosed` 自动 dequeue + priority DESC 优先级 + enqueueOrder ASC tiebreak — R3 P3 PlayMode probe 实证 (不补 production code；ADR-011 spec "Popup queue FIFO（最多 1 个可见）" wording drift vs vendor 实际 priority DESC + ASC tiebreak — Phase 2 评估是否需要 ADR-011 amend 或保留 spec FIFO assumption + vendor 实现细节)
- [ ] **AC-8** Same-layer panel sorting 行为完整 verify (vendor `UIModule.OnSortWindowDepth` 已 production)：同 layer 后入栈 panel `Depth = layerBase + N * WINDOW_DEEP(100)` 显示在上层；cross-layer Compare: `MockTipsPanel.Depth(6000) > MockTopPanel.Depth(4000)` (Tips=3 > Top=2 — layer index 直接 depth multiplier) — R3 P4 实证 (不补 production code)
- [ ] **AC-9** InputBlocker stack semantic 完整 verify (`InputBlocker.cs` 已 production + 9 unit test `InputBlockerTests.cs`)：多 token 栈 + duplicate token 配 duplicate pop + non-existent token safe pop (log warning 不 throw) — R3 P3 通过 popup queue 3 panel 触发 6 event (3 push + 3 pop) 实证 stack semantic (本 story narrow scope **不实例化 InputBlocker** — listener-side wiring Sprint 7+ ADR-010 InputManager epic；R3 走 spike subscribe IInputBlockerEvent assert event count + token format pattern per S5-05 P3 listener spy precedent)
- [ ] **AC-10** R3 PlayMode probe 5 case 全 PASS first-attempt-after-DevTestState-fix (30+ asserts 估)；evidence doc `production/qa/playmode-popup-auto-blocker-2026-05-XX.md` (Phase 4 dev-story 写入)；DevTestState `[main-menu]` mode 复用 评估扩 `HasSpike("S5-02") || HasSpike("S6-07") || HasSpike("S6-08")` (V3.0.1 dp8 candidate 阈值阶进 +1 — main-menu mode spike count 2 → 3)

---

## Engine Notes

### Vendor API Realities (per Phase 0 R2 verify 实证 — 2026-05-13 morning Session 30)

- **`UIModule.ShowUIImp(Type type, bool isAsync, params object[] userDatas)`** (UIModule.cs:298) — `ShowUI<T>` / `ShowUI(Type)` / `ShowUIAsync<T>` / `ShowUIAsync(Type)` 4 entry (line 252/263/274/295) 全 dispatch 至此；本 story 在 `OnWindowPrepare(window)` 之后插入 `TryFireInputBlockerPush(window)` helper (Phase 2 评估具体 hook 位置 — 推荐在 vendor `_uiStack.Add(window)` 完成后 sync-with-window-state)
- **`UIModule.CloseUI(Type type)`** (UIModule.cs:375) — `Pop(window)` 之前 fire `TryFireInputBlockerPop(window)` helper；现有 closing flow: `window.InternalDestroy()` → `Pop(window)` → `OnSortWindowDepth` → `OnSetWindowVisible` → `OnPopupClosed(type)`；推荐 fire 在 `InternalDestroy` 之后 + `Pop` 之前 (window 状态尚 valid 但已注定 close)
- **`UIModule.HideUI(Type type)`** (UIModule.cs:394) — vendor 内 `HideTimeToClose <= 0` 直接 `CloseUI(type)` 短路 (此时由 CloseUI 路径自然 fire pop)；`> 0` 进入 timer-based delayed close 路径，`Visible = false` + `IsHide = true` + AddTimer；本 story 在 `Visible = false` 之后 fire pop (hide 即解锁，re-show 时 ShowUI 路径再 push)
- **`UIModule.PopupQueue.cs`** partial class (182 行) — `_popupQueue` (List) + `_enqueueCounter` + `_currentPopupType` (1 visible 控制) + `_isPopupQueuePaused` + `EnqueuePopup/PausePopupQueue/ResumePopupQueue/ClearPopupQueue/ClearAndClosePopupQueue` API + `OnPopupClosed(Type)` dequeue trigger (line 128) + `TryShowNextPopup` (line 146) + `InsertByPriority` (line 164) priority DESC + enqueueOrder ASC tiebreak
- **`UIModule.OnSortWindowDepth(int layer)`** (UIModule.cs:483-494) — `depth = layer * LAYER_DEEP(2000)`；遍历 `_uiStack` 内同 `WindowLayer == layer` 的 panel + 累加 `WINDOW_DEEP(100)`；后入栈 panel depth 更高 → 上层显示
- **`UIWindow.WindowLayer { private set; get; }`** (UIWindow.cs:60) 是 **`int`** 不是 `UILayer` enum；vendor `WindowAttribute(UILayer windowLayer, ...)` ctor 内 cast `(int)windowLayer` (WindowAttribute.cs:55/63/71)；business code compare `window.WindowLayer == (int)UILayer.Top` (显式 cast)
- **`WindowAttribute`** (WindowAttribute.cs:20) — 4 ctor overload (per S5-08 Session 26 #5 R2 verify)：(int) / (UILayer) / (UILayer, fromResources) / (UILayer, fromResources, location)
- **`UILayer` enum** (WindowAttribute.cs:8) `: int { Bottom=0, UI=1, Top=2, Tips=3, System=4 }` (与 ADR-011 早期 spec wording `Background/HUD/Popup/Overlay/System` 命名 drift — S5-08 R2 closure 已 align vendor 命名)
- **`InputBlocker`** (Input/InputBlocker.cs:15) — plain `public class` + stack semantic + `PushBlocker(token)` / `PopBlocker(token)` LastIndexOf 安全弹 + `IsBlocked` / `BlockerCount` props + `ForcePopAllBlockers()` + `CheckLeaks(realtime)` 30s leak detect；**0 production instance / GameModule wrapper / Singleton** — Sprint 7+ ADR-010 InputManager refactor
- **`IInputBlockerEvent.OnPushBlocker(string token)` / `OnPopBlocker(string token)`** (IEvent/IInputBlockerEvent.cs) — `[EventInterface(EEventGroup.GroupLogic)]` (per ADR-027 §4 source)；S5-05 NarrativeSequencePlayer 已 sender precedent (NarrativeSequencePlayer.cs:312/323 `GameEvent.Get<IInputBlockerEvent>().OnPushBlocker/OnPopBlocker(token)`)；Listener: 未来 InputManager (per IInputBlockerEvent.cs:9 注释 Sprint 6/7 实施 — 本 story 沿 narrow scope [A] 留 Sprint 7+ ADR-010 epic)

### Visibility Modifier 强制 (V3.0.1 dp7 NEW reinforce — 复 S6-07 precedent)

本 story 新增 mock UIWindow fixture (MockTopPanel + MockTipsPanelA/B/C) 必 `protected override` lifecycle method (per V3.0.1 dp7 NEW hotfix — UIBase.cs:144-197 + UIWindow.cs:504/509 全 9 hook `protected virtual void XxxName()`)：

```csharp
// ✅ 正确 (复 S6-07 MainMenuPanel.cs precedent)
protected override void OnCreate() { base.OnCreate(); }
protected override void OnRefresh() { base.OnRefresh(); }
protected override void OnDestroy() { base.OnDestroy(); }

// ❌ 错误 (触发 CS0507: cannot change access modifiers when overriding)
public override void OnCreate() { /* ... */ }
```

### Token Format Consistency

- **NarrativeSequencePlayer** token format (S5-05): `"narrative_seq_<sequenceId>"` (NarrativeSequencePlayer.cs:312)
- **UIModule** token format (本 story 引入): `type.FullName` (e.g. `"GameLogic.PauseMenuPanel"`)
- 两 source 不冲突 — token 是 InputBlocker 内部 string 唯一性 (LastIndexOf 安全弹依赖 reference equality + string equality)；namespace 不同自然区分
- Future ADR-010 InputManager listener 实施时若需要 token registry 可 prefix scheme 统一 (e.g. `"ui:GameLogic.PauseMenuPanel"` / `"narrative:seq_5"`)；本 story narrow scope 不引入 prefix

---

## Control Manifest

### Required Patterns

- UIModule.ShowUIImp / CloseUI / HideUI 4 entry 共用 `private void TryFireInputBlocker{Push,Pop}(UIWindow window, bool isPush)` helper 集中实现 layer check + token 生成 + fire (避免 4 entry 各自重复逻辑)
- Layer check: `window.WindowLayer == (int)UILayer.Top || window.WindowLayer == (int)UILayer.Tips` (显式 int cast — UIWindow.WindowLayer 是 int 不是 UILayer enum)
- Token 生成: `token = window.GetType().FullName` (e.g. `"GameLogic.MockTopPanel"`)；不 hardcode；不 lowercase；不 prefix
- Fire via GameEvent bus: `GameEvent.Get<IInputBlockerEvent>().OnPushBlocker(token)` / `OnPopBlocker(token)` (与 NarrativeSequencePlayer.cs:312/323 sender pattern 一致)
- Hook 位置: ShowUIImp 在 `OnWindowPrepare(window)` 之后 (sync-with-window-state) + `_uiStack.Add` 之后；CloseUI 在 `InternalDestroy()` 之后 + `Pop(window)` 之前 (close intent 已定 + window state 尚 valid)；HideUI 在 `Visible = false` 之后 (hide 即解锁)
- Mock UIWindow fixture 全 `protected override` lifecycle method (V3.0.1 dp7 NEW)
- ADR-027 §5 framework knowledge fact named delegate cache pattern 沿 S6-07 MainMenuPanel.cs precedent (mock fixture 简化 0-listener pattern — 不订阅 GameEvent listener；本 story spike subscribe IInputBlockerEvent listener 端 是 R3 verify spy 不是 production code)

### Forbidden Patterns

- ❌ `public override` lifecycle method visibility modifier (触发 CS0507 — V3.0.1 dp7 NEW)
- ❌ Mock UIWindow fixture 内 hardcode token 或自 fire `OnPushBlocker(token)` (token fire 走 UIModule helper 单一 source；mock fixture 仅 placeholder)
- ❌ UIModule.ShowUIImp/CloseUI/HideUI 内直接调 `InputBlocker.PushBlocker(token)` (跨 ADR-010 epic boundary — listener-side InputBlocker singleton 留 Sprint 7+；本 story 走 IInputBlockerEvent broadcast bus path)
- ❌ Layer check 用 `if (window.WindowLayer >= (int)UILayer.Top)` 大于等于范式 — System(4) layer 不应 trigger push/pop；显式 `== Top || == Tips` enumerate (per AC-5)
- ❌ Token 拼接 prefix (e.g. `"ui:" + type.FullName`) — 本 story narrow scope 保 token 是 plain FullName；Sprint 7+ ADR-010 InputManager 实施时若需要 prefix 统一会回头 amend (本 story Out of Scope)

---

## Out of Scope

- **listener-side InputBlocker singleton refactor** — Sprint 7+ ADR-010 InputManager epic：`Singleton<InputBlocker>` (TEngine pattern 与 AudioManager 对齐 per S6-07 Phase 2.0 R2.6 closure precedent) OR `class InputManager` (ADR-010 spec wording 对齐) + GameApp.cs Initialize wire + IInputBlockerEvent listener subscribe → `InputBlocker.PushBlocker/PopBlocker(token)` chain 完整闭环
- **InputManager 类创建** — Sprint 7+ ADR-010 epic (per IInputBlockerEvent.cs:9 注释 "Listener: 未来 InputManager")
- **NarrativeSequencePlayer fire-and-forget closure** — 与 listener-side wiring 一并 Sprint 7+ ADR-010 epic 完成；本 story 完成 sender-side 后 dual-source (UIModule + NarrativeSequencePlayer) 同向贡献 IInputBlockerEvent token
- **真 Popup/Overlay 业务面板** (PauseMenuPanel / SettingsPanel / ChapterTransitionPanel) — Sprint 7+ Production stage polish phase 起步实施 ui-system-002~007 真路径；本 story R3 走 mock UIWindow fixture
- **`IInputService.GetBlockerStack()` API** — story-008 早期 placeholder line 35 假设的 API 不存在；实际 `InputBlocker.BlockerCount` 公有 property 已足够 R3 verify (本 story 不引入新 API)
- **Overlay limit (单 Tips layer panel 限制)** — vendor `LAYER_DEEP=2000 / WINDOW_DEEP=100` 无 panel 数限制 (layer 内最多 20 panel 不冲突 sort)；本 story 不引入新限制
- **TimeScale = 0 on PauseMenu** (TR-ui-009) — 留 ui-system-003 Sprint 7+
- **Safe area / localization / android back button / gaussian blur** (TR-ui-007/021/022/013) — 各自独立 story (009/010/etc) Sprint 7+
- **Token prefix scheme** (`"ui:"`/`"narrative:"` 区分 source) — 留 Sprint 7+ ADR-010 InputManager 实施时评估是否引入
- **Bottom(0) / System(4) layer auto blocker** — 本 story narrow scope 仅 Top/Tips 双 layer；Bottom 是 background panel 非交互；System (e.g. SaveIndicatorPanel) always-on-top 系统通讯非用户阻塞；未来若需要 walk `IInputBlockerEvent.OnPushBlocker(custom_token)` manual fire (per S5-05 NarrativeSequencePlayer precedent)
- **Vendor UIModule.cs patch** — 本 story narrow scope 增加 helper method 不修改 vendor existing logic；不 patch ShowUI/CloseUI/HideUI signature；不修改 _uiStack 数据结构；保 backward compatibility 100%

---

## R3 PlayMode Probe Cases (5 case)

### P1 — UIModule Layer-aware Auto Push/Pop Sender Verify (~6 asserts)

- **Setup**: spike `Awake()` 同步 subscribe `IInputBlockerEvent.OnPushBlocker` + `OnPopBlocker` 2 listeners (M1 dual-layer pattern + S5-1c Awake sync-subscribe race precedent + S5-05 P3 listener spy precedent — Application.logMessageReceived sniffer / GameEvent.AddEventListener<string> 双路径)；GameApp DevTestState `[main-menu]` mode 复用 (per S6-07 closure precedent — 不 pre-dispatch OnRequestSceneChange)
- **Action**: spike `GameModule.UI.ShowUI<MockTopPanel>()` (mock fixture `[Window(UILayer.Top, fromResources: true, location: "UI/MockTopPanel")]`) → wait frame=N (vendor 7 init 完成) → spike capture push event (token + count)；then `GameModule.UI.CloseUI<MockTopPanel>()` → capture pop event
- **Assert**: (1) `OnPushBlocker` fired count == 1 (2) push token == `"GameLogic.MockTopPanel"` (typeof(MockTopPanel).FullName) (3) `OnPopBlocker` fired count == 1 (4) pop token == push token (5) push timing 在 ShowUI 之后 + CloseUI 之前 (capture frame index Compare) (6) 0 unexpected console error/warning

### P2 — UI(1)/Bottom(0)/System(4) Layer No-Push Verify (HUD pass-through + Bottom + System cross-layer contrast) (~7 asserts)

- **Setup**: post-P1 + spike subscribe maintained；3 mock fixture `MockUIPanel` (`UILayer.UI`) + `MockBottomPanel` (`UILayer.Bottom`) + `MockSystemPanel` (`UILayer.System`) — Phase 2 评估是否复用 S5-08 已 production `S5_08_MockMinimalPanel` (该 fixture 已 `UILayer.UI` attribute) 减少 fixture 数；OR 创建 3 separate variant
- **Action**: `ShowUI<MockUIPanel>()` → CloseUI；`ShowUI<MockBottomPanel>()` → CloseUI；`ShowUI<MockSystemPanel>()` → CloseUI；每次 verify push/pop count delta == 0
- **Assert**: (1) UI layer ShowUI/CloseUI 期间 push/pop count delta == 0 (2) Bottom layer 同 delta == 0 (3) System layer 同 delta == 0 (4) MainMenuPanel.LastInstance != null (S6-07 已 production；同时 S6-08 P2 复 S6-07 P2 4 button wiring assert subset — verify panel ready 状态) — 可选 (5) UIWindow.WindowLayer field 类型 `int` (reflection BindingFlags.NonPublic | Instance + GetValue 验) — V3.0.1 watch list dp8 candidate field check (6) UILayer enum value `Top=2 / Tips=3` (reflection `Enum.GetValues` Compare) — vendor source S5-08 实证 (7) 0 unexpected error

### P3 — Popup Queue Vendor Behavior Verify (priority DESC + enqueueOrder ASC tiebreak + Auto Push/Pop chain) (~9 asserts)

- **Setup**: post-P2 cleanup + spike subscribe maintained；3 mock fixture `MockTipsPanelA/B/C` (全 `[Window(UILayer.Tips, fromResources: true, location: "UI/MockTipsPanelX")]`)
- **Action**: 
  ```
  GameModule.UI.EnqueuePopup<MockTipsPanelA>(priority=10)
  GameModule.UI.EnqueuePopup<MockTipsPanelB>(priority=20)
  GameModule.UI.EnqueuePopup<MockTipsPanelC>(priority=10)
  → 等 ShowUIImp 同步执行 → 验 _currentPopupType == typeof(MockTipsPanelB) (priority 20 DESC 最高优先)
  CloseUI<MockTipsPanelB>() → OnPopupClosed → 验 _currentPopupType == typeof(MockTipsPanelA) (priority 10 + enqueueOrder ASC tiebreak A 在 C 前)
  CloseUI<MockTipsPanelA>() → 验 _currentPopupType == typeof(MockTipsPanelC)
  CloseUI<MockTipsPanelC>() → 验 _currentPopupType == null (末态)
  ```
- **Assert**: (1)-(3) 3 个 popup 出场顺序 B → A → C (priority + tiebreak verify reflection `_currentPopupType` field) (4) 每次 ShowUI/CloseUI fire 1 push + 1 pop → 3 round-trip → 6 events (PushCount=3 + PopCount=3) (5) Tips layer all 3 panel push token == `"GameLogic.MockTipsPanelA/B/C"` 各自 FullName (6) `PopupQueueCount` initial 3 (after EnqueueX3) → first ShowUI 后 2 → ... → 0 末态 (7) `_currentPopupType == null` 末态 (8) duplicate token safe (3 个 token 全 unique；本 case 不测 duplicate；duplicate 测留 future case) (9) 0 unexpected error

### P4 — Same-Layer + Cross-Layer Depth Sorting Verify (LAYER_DEEP+WINDOW_DEEP) (~7 asserts)

- **Setup**: post-P3 cleanup + spike subscribe maintained
- **Action**: `ShowUI<MockTopPanel>()` (Top=2) → `ShowUI<MockTopPanel2>()` (新 mock variant Top=2) → reflection 拿 `_uiStack` 验 2 panel `Depth` 字段；then `ShowUI<MockTipsPanelA>()` (Tips=3) → 验 cross-layer sorting；最后 cleanup CloseUI × 3
- **Assert**: (1) `MockTopPanel.Depth == 2 * LAYER_DEEP(2000) + 0 == 4000` (2) `MockTopPanel2.Depth == 4100` (后入栈 +WINDOW_DEEP(100)) (3) `MockTopPanel2.Depth > MockTopPanel.Depth` (后入栈在上层) (4) cross-layer: `MockTipsPanelA.Depth == 3 * 2000 + 0 == 6000 > MockTopPanel2.Depth(4100)` (Tips layer 全在 Top layer 之上) (5) push/pop event count 全程符合 3 push + 3 pop (3 panel 全 Top/Tips layer) (6) cleanup 后 `_uiStack.Count == 0` (7) 0 unexpected error

### P5 — Pause/Resume/Clear Popup Queue API + ForcePopAllBlockers Verify (~6 asserts)

- **Setup**: post-P4 cleanup + spike subscribe maintained
- **Action**: 
  ```
  EnqueuePopup<MockTipsPanelA> × 3 (priority 各异)
  → 验 _currentPopupType != null (first popup auto showing) + PopupQueueCount == 2
  PausePopupQueue() → 验 IsPopupQueuePaused == true
  CloseUI<MockTipsPanelA>() → 验 _currentPopupType == null + PopupQueueCount == 2 (paused 不 dequeue 下一个) + push/pop count 仅 1 round-trip
  ClearPopupQueue() → 验 PopupQueueCount == 0
  ResumePopupQueue() → 验 IsPopupQueuePaused == false + 不 trigger 新 popup (queue empty 也 trigger TryShowNextPopup return early)
  ```
- **Assert**: (1) `IsPopupQueuePaused == true` after `PausePopupQueue` (2) `_currentPopupType == null` + `PopupQueueCount == 2` after `CloseUI(first)` during pause (dequeue 抑制 verify) (3) `PopupQueueCount == 0` after `ClearPopupQueue` (4) `IsPopupQueuePaused == false` + `_currentPopupType == null` after `ResumePopupQueue` (queue empty 自然) (5) total push count == 1 + pop count == 1 (仅 first popup 走 push/pop chain — clear 不 trigger pop) (6) 0 unexpected error

---

## R2 Assumptions Validated (Phase 1 readiness gate evidence)

| ID | Assumption | Status | Evidence |
|----|-----------|--------|---------|
| R2.1 | `UIModule.ShowUIImp(Type, bool, params object[])` 是 ShowUI/ShowUIAsync 4 entry 共用内部入口 | ✅ FULLY | UIModule.cs:252/263/274/295 4 public entry + line 298/310 private impl dispatch (内 `ShowUIImp<T>(...)` generic 版调 `ShowUIImp(typeof(T), ...)` 路径) |
| R2.2 | `UIModule.PopupQueue.cs` partial class popup queue 已 production | ✅ FULLY | UIModule.PopupQueue.cs 182 行 + `EnqueuePopup` (line 52/63) + `OnPopupClosed` (line 128 dequeue trigger) + `TryShowNextPopup` (line 146) + `InsertByPriority` (line 164 priority DESC + enqueueOrder ASC) |
| R2.3 | `UIModule.OnSortWindowDepth(int layer)` same-layer sorting 已 production | ✅ FULLY | UIModule.cs:483-494 `depth = layer * LAYER_DEEP + N * WINDOW_DEEP`；遍历 `_uiStack` 同 layer 累加；`LAYER_DEEP=2000` + `WINDOW_DEEP=100` (line 25-26 const) |
| R2.4 | `InputBlocker` stack semantic + token uniqueness + leak detect | ✅ FULLY | Input/InputBlocker.cs 73 行 + `PushBlocker(token)` (line 27) + `PopBlocker(token)` (line 33 LastIndexOf 安全) + `IsBlocked` / `BlockerCount` props (line 24-25) + `ForcePopAllBlockers()` (line 45) + `CheckLeaks(realtime)` (line 56 30s threshold) + 9 unit test InputBlockerTests.cs |
| R2.5 | `WindowAttribute` UILayer enum 4 ctor + `UIWindow.WindowLayer` int field | ✅ FULLY | WindowAttribute.cs:8 (enum Bottom=0/UI=1/Top=2/Tips=3/System=4) + line 45/53/61/69 (4 ctor overload) + UIWindow.cs:60 (`public int WindowLayer { private set; get; }`) |
| R2.6 | `IInputBlockerEvent` contract + `GameEvent.Get<IInputBlockerEvent>()` 派发路径 + sender precedent | ✅ FULLY | IEvent/IInputBlockerEvent.cs (`[EventInterface(EEventGroup.GroupLogic)]` + 2 method `OnPushBlocker(string)` + `OnPopBlocker(string)`) + S5-05 NarrativeSequencePlayer.cs:312/323 sender precedent (`GameEvent.Get<IInputBlockerEvent>().OnPushBlocker(token)` / `OnPopBlocker(token)`) + S5-05 P3 R3 listener spy precedent (GameEvent.AddEventListener) |
| R2.7 | listener-side InputBlocker singleton / InputManager class 0-production | ⚠️ DEFERRED Sprint 7+ ADR-010 epic | `Singleton<InputBlocker>` / `class InputManager` 全 codebase 0 hit；InputBlocker 0 production `new InputBlocker()` instance (仅 InputBlockerTests.cs SetUp 实例化测试)；per IInputBlockerEvent.cs:9 注释 "Listener: 未来 InputManager — Sprint 6/7 实施" 中的 Sprint 7+ path (本 story narrow scope [A] sender-only — 不阻 Phase 2 transition) |
| R2.8 | mock UIWindow fixture for R3 (复用 S5_08_MockMinimalPanel pattern + Top/Tips/UI/Bottom/System variant) | ⚠️ TBD Phase 2 | S5_08_MockMinimalPanel.cs 已 production (`UILayer.UI` attribute) + `S5_08_MockPanelGenerator.cs` Editor menu pattern；Phase 2 评估 (i) 复用 S5_08_MockMinimalPanel 作为 UI(1) layer 测试 fixture + 新建 MockTopPanel + MockTipsPanelA/B/C + MockBottomPanel + MockSystemPanel 5 variant (Phase 2 估 ~150-200 行 mock fixture 总量) OR (ii) 改进 S5_08_MockMinimalPanel 为 generic mock + UILayer 参数 (vendor [Window] attribute 是 class-level 不接受 runtime arg — 必各 class) — 推荐 (i) per class-level attribute constraint |
| R2.9 | DevTestState `[main-menu]` mode 复用 for S6-08 spike (避 pre-dispatch OnRequestSceneChange race) | ⚠️ TBD Phase 2 | DevTestState.cs:32 `HasSpike("S5-02") || HasSpike("S6-07")` (S6-07 closure 增 V3.0.1 dp8 candidate `[main-menu]` mode 复用 watch list) — Phase 2 评估 option (i) 扩 `|| HasSpike("S6-08")` (~+1 行 minimal change) per S6-07 [A] precedent OR (ii) 升级 `IDevSpike.IsMainMenuMode` interface property + DevTestState 走 spike flag 决策 (~30-50 行 refactor) — 推荐 (i) per scope minimal；阈值阶进 main-menu mode spike count 2 → 3 (距阈值 4 仅 1 个) |

**R2 Verdict**: ✅ **DEFICIENCY-FLAGGED PASS** (R2.1~R2.6 ✅ FULLY；R2.7 ⚠️ DEFERRED 跨 Sprint 7+ ADR-010 epic boundary 明示；R2.8 + R2.9 ⚠️ TBD Phase 2 实施时确认 — 不阻 Phase 2 transition per ADR-029 V2.0 §V2-1 R2 DEFICIENCY-FLAGGED PASS 路径)

---

## V3.0.1 Watch List Hooks

### Type-9 dp1 (S6-06 closure absorbed into V2.0 §V2-1.b R2 增量子条款) — ✅ R2.6 走 Interface Method Set Fan-out Check

- **本 story enforce**: R2.6 IInputBlockerEvent fan-out check — interface method set `{ OnPushBlocker, OnPopBlocker }` 双向 grep verify sender (NarrativeSequencePlayer ✅ + 本 story UIModule ⏳) 和 listener (Sprint 7+ ⚠️ DEFERRED 明示) 端 fan-out gap detection (R2.7 deferred 明示)；与 ADR-029 V2.0 §V2-1.b "R2 增量子条款" 协议吻合 — 跨 sprint deferred 项 R2 grep gap 标 ⚠️ DEFERRED 不 ⚠️ TBD
- **governance insight**: V3.0.1 dp7 NEW (visibility modifier drift) + Type-9 dp1 (R2 grep 完备性 meta-drift) 双 closure 模式实战 — Sprint 6 governance maturity 持续沉淀

### Type-5 dp7 NEW (S6-07 closure visibility modifier drift) — ✅ R3 P1 mock fixture reinforce 应用

- **本 story enforce**: mock UIWindow fixture (MockTopPanel / MockTipsPanelA/B/C / MockUIPanel / MockBottomPanel / MockSystemPanel) 全 `protected override` lifecycle method (复 S6-07 MainMenuPanel.cs precedent)；R3 P2 case (5) optional reflection MethodInfo.IsFamily assert 作为 dp7 NEW reinforce baseline test (本 story narrow scope 不强制 P2 case 加 (5)；推荐留 watch list 实测时若再 drift 加)

### Type-5 dp6 (closure — ISceneEvent chapter id 1-5 valid only) — 不触发

- **本 story 不触发**: 本 story narrow scope 不涉及 ISceneEvent.OnRequestSceneChange (chapter switch 留 Sprint 6+ chapter epic + S6-04 error/restart)；R3 case 全程 chapter 1 内 popup queue + auto blocker verify (DevTestState [main-menu] mode 不 pre-dispatch chapter 切换)

### Type-8 dp1 (留观察 — S5-08 UIWindow second show 'destroy-and-recreate') — ⚠️ 可能触发 R3 P3

- **本 story 可能触发**: R3 P3 Popup Queue 3 panel 进出 `_uiStack` 链路 (MockTipsPanelA/B/C 每次 ShowUI 完整 7 init lifecycle + CloseUI vendor `InternalDestroy` 销毁 → 第 2 次 ShowUI 路径走 vendor 重建 'destroy-and-recreate' 模式 per S5-08 V3 Type-8 dp1 实证)；本 story narrow scope 不依赖 instance reuse；若 R3 P3 实测 second show 行为有 wording drift 需补 dp9 watch list candidate

### NEW dp8 candidate (S6-07 closure surfaced — DevTestState `[main-menu]` mode 复用) — ⚠️ 留观察 V3.1 trigger 候选 (阈值阶进)

- **触发场景**: 本 story Phase 2 时 DevTestState 是否 again 扩 `HasSpike("S6-08")` 走 main-menu mode (per R2.9 ⚠️ TBD)；若 YES → main-menu mode spike count = 3 (S5-02 + S6-07 + S6-08)；距阈值 4 仅 1 个；Sprint 6 Track A 启动 playtest spike (S6-01/-02/-03) 时若也走 main-menu mode → 阈值触发 → V3.1 spec amend `IDevSpike.IsMainMenuMode` flag 自声明
- **Phase 2 推荐决策**: option (i) 扩 `HasSpike("S5-02") || HasSpike("S6-07") || HasSpike("S6-08")` ~+1 行 minimal change 复 S6-07 [A] precedent；OR option (ii) 升级 `IDevSpike.IsMainMenuMode` interface property (~30-50 行 refactor)；推荐 (i) per scope minimal — V3.1 trigger 待阈值真正达成
- **governance insight**: dp8 candidate 阈值阶进模式实战 — V3.1 spec amend trigger 不是 reactive 触发 (问题先发生后修复) 而是 predictive (阈值阶进追踪 + 阈值即将达成时主动评估 trigger)

---

## Implementation Notes

### File Targets (S6-08 Phase 2 production code 实施 — defer 下一 session 起)

1. **Update** `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIModule.cs` (~+30-40 行 estimate)
   - `private void TryFireInputBlockerPush(UIWindow window)` helper — layer check `(window.WindowLayer == (int)UILayer.Top || window.WindowLayer == (int)UILayer.Tips)` + token `window.GetType().FullName` + `GameEvent.Get<IInputBlockerEvent>().OnPushBlocker(token)`
   - `private void TryFireInputBlockerPop(UIWindow window)` helper — 对称版调 `OnPopBlocker(token)`
   - `ShowUIImp(Type, bool, params object[])` (line 298) 内 `OnWindowPrepare(window)` 之后 + `_uiStack.Add` 之后插入 `TryFireInputBlockerPush(window)`
   - `ShowUIImp<T>(bool, params object[])` (line 310) 同模式 OR delegate to non-generic version (避重复)
   - `CloseUI(Type)` (line 375) 内 `InternalDestroy()` 之后 + `Pop(window)` 之前插入 `TryFireInputBlockerPop(window)`
   - `HideUI(Type)` (line 394) 内 `window.Visible = false` 之后插入 `TryFireInputBlockerPop(window)` (`HideTimeToClose <= 0` 短路 CloseUI 路径自然 fire — 避免双 fire 需评估)
   - Phase 2 评估 `HideUI` 双 fire 风险: short-circuit 路径 `CloseUI(type)` 已 fire pop → `HideUI` 不应再 fire；建议 short-circuit 早 return + `HideTimeToClose > 0` 真 hide 路径才 fire pop

2. **New** `Assets/GameScripts/HotFix/GameLogic/UI/MockTopPanel.cs` + `MockTipsPanelA.cs` + `MockTipsPanelB.cs` + `MockTipsPanelC.cs` + `MockBottomPanel.cs` + `MockSystemPanel.cs` (6 file × ~30-40 行 each = ~180-240 行 — 复用 S5_08_MockMinimalPanel pattern + `[Window(UILayer.Top/Tips/Bottom/System, fromResources: true, location: "UI/MockXxxPanel")]` attribute + 7+2 lifecycle protected override stub + 0 业务逻辑)
   - Phase 2 评估 (i) 6 file separate OR (ii) 1 file `MockUIPanels.cs` 内 6 partial class — vendor `[Window]` 是 class-level attribute 必各 class，但同 file 多 class 兼容；推荐 (i) per S5-08 precedent (1 mock = 1 file)
   - 复用 S5_08_MockMinimalPanel 作 MockUIPanel (UI(1) layer P2 case) 减少 fixture 数 → 实际新 5 file

3. **Update** `Assets/Editor/DevTest/S5_08_MockPanelGenerator.cs` OR new `S6_08_MockPanelsGenerator.cs` (Editor MenuItem `Tools/S6-08/Generate Mock Panel Prefabs` 程序化生成 5 prefab to `Assets/Resources/UI/Mock*.prefab`) — Phase 2 评估 (i) extend existing S5_08 generator OR (ii) new S6_08 generator；推荐 (ii) per S5-08 narrow scope file 隔离 + Tools menu scope clean

4. **New** `Assets/Resources/UI/MockTopPanel.prefab` + `MockTipsPanelA.prefab` + `MockTipsPanelB.prefab` + `MockTipsPanelC.prefab` + `MockBottomPanel.prefab` + `MockSystemPanel.prefab` (Generator 程序化生成；Canvas + GraphicRaycaster + Image 最小化 fixture per S5-08 vendor reality 强制 — UIWindow.cs:484 `_panel.GetComponent<Canvas>()` 强制要求 root component)

5. **New** `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-08_PopupAutoBlocker.cs` (~500-600 行 estimate — 1 file + 2 inner class S608Spike + S608Runtime + S608Tester per S5-08/S5-02/S6-07 precedent；5 R3 case + JSON evidence dump `~/Library/Application Support/DefaultCompany/Unity/S6-08_Result.json`)

6. **Update** `Assets/GameScripts/HotFix/GameLogic/GameApp.cs` (RegisterDevSpikes S607Spike → S608Spike 切换 — 单 spike 模式 OR 评估并存)

7. **Update** `Assets/GameScripts/HotFix/GameLogic/GameFlow/DevTestState.cs` (扩 `HasSpike("S5-02") || HasSpike("S6-07") || HasSpike("S6-08")` `[main-menu]` mode 复用 — V3.0.1 dp8 candidate 阈值阶进 +1 — main-menu mode spike count 2 → 3)

### Sprint 6 S6-08 Phase 1 — Story File 创建 + R1+R2+R3 Readiness Gate (本 session 完成)

Phase 1 完成内容 (Session 30 morning 2026-05-13)：

- ✅ story-008-ui-layer-strategy.md 完整 rewrite (本文件 ~550 行) per V3.0.1 vendor reality compliant + S6-08 sender-only narrow scope [A] 决策
- ✅ R1 readiness gate — 0 forbidden listener pattern (本 story 不订阅 GameEvent listener；UIModule helper 仅 sender fire；R3 spike subscribe IInputBlockerEvent 端是 verify spy 非 production code 不计 R1 违规)
- ✅ R2 readiness gate — DEFICIENCY-FLAGGED PASS:
  - R2.1~R2.6 ✅ FULLY (UIModule.ShowUIImp 4 entry + UIModule.PopupQueue.cs + UIModule.OnSortWindowDepth + InputBlocker stack + WindowAttribute UILayer + IInputBlockerEvent contract + S5-05 sender precedent — vendor source 实证)
  - R2.7 ⚠️ DEFERRED Sprint 7+ ADR-010 epic boundary (InputBlocker singleton / InputManager class 0-production — 明示 epic boundary 不阻 Phase 2)
  - R2.8 + R2.9 ⚠️ TBD Phase 2 实施时确认 (mock fixture set 数量 + DevTestState `[main-menu]` mode 复用 V3.0.1 dp8 candidate +1)
- ✅ R3 readiness gate — propagate spec write into 5 PlayMode probe case (P1 sender verify + P2 UI/Bottom/System no-push contrast + P3 popup queue priority + push/pop chain + P4 sorting + P5 pause/resume/clear)

✅ R2 deficiency flag **R2.7 DEFERRED + R2.8 + R2.9 TBD** 等 Phase 2 实施时 verify — per ADR-029 V2.0 §V2-1 R2 DEFICIENCY-FLAGGED PASS path (类似 S6-07 R2.6 + R2.8 ⚠️ TBD 路径) ✅ READY for Phase 2 transition。

**Status**: ✅ **DONE** (Phase 0 ✅ + Phase 1 ✅ + Phase 2 ✅ + Phase 3 ✅ + Phase 4 ✅ + Phase 5 ✅ — 2026-05-13 afternoon Session 30)。Phase 2 production code 6 file (UIModule.InputBlocker.cs NEW + UIModule.cs amend + 7 Mock UIWindow + Editor + 7 prefab + S6-08 spike + GameApp + DevTestState wire)；Phase 3 R3 PlayMode 5/5 case PASS / 35/35 asserts / `all_passed=true` 第 2 跑 after V3.0.1 dp9 NEW spec wording amend / Push=8 Pop=8 对称 / 0 unexpected error；Phase 4 evidence doc `production/qa/playmode-popup-auto-blocker-2026-05-13.md` 8 sections；Phase 5 closure (本 story Status + sprint-status.yaml + active.md + commit pending)。

---

## Dependencies

### Story Dependencies (✅ all done)

- **S5-08 ✅** (UIModule narrow scope — UILayer.cs vendor enum + WindowAttribute 4 ctor + ShowUI/CloseUI API + 7+2 lifecycle + Mock fixture pattern + Editor generator pattern)
- **S5-05 ✅** (NarrativeSequencePlayer + IInputBlockerEvent contract + sender pattern token format `"narrative_seq_<id>"` precedent)
- **S6-05 ✅** (ADR-011 §G systematic wording amend — vendor 7+2 lifecycle 完整 documented + UILayer enum vendor 命名 + ShowUI/CloseUI API documented)
- **S6-06 ✅** (ADR-029 V2.0 §V2-1.b R2 增量子条款 — Interface Method Set Fan-out Check pattern S6-08 R2.6 + R2.7 应用)
- **S6-07 ✅** (DevTestState [main-menu] mode 复用 precedent + V3.0.1 dp7 NEW protected override + ADR-027 §5 named delegate cache + R2 deficiency-flagged PASS path 实战首次完整跑通)

### Out-of-Scope Story Dependencies (Sprint 7+ Production stage / ADR-010 Implementation Expand)

- **ADR-010 InputManager epic** — listener-side InputBlocker singleton refactor + IInputBlockerEvent listener wiring + InputManager class 创建 + NarrativeSequencePlayer fire-and-forget closure (Sprint 7+)
- **ui-system-002~007 真业务面板** — PauseMenuPanel / SettingsPanel / ChapterTransitionPanel / etc (Sprint 7+ Production stage polish phase 起步实施)
- **Token prefix scheme 统一** — Sprint 7+ ADR-010 InputManager 实施时评估

### Framework Dependencies (✅ all verified Phase 1)

- TEngine UIModule + UIBase + UIWindow + WindowAttribute (vendor source ✅ per S5-08 R2 closure)
- UIModule.PopupQueue.cs partial class (✅ per Phase 0 R2 verify)
- UIModule.OnSortWindowDepth (✅)
- InputBlocker class (✅ + 9 unit test)
- IInputBlockerEvent contract + GameEvent.Get<IInputBlockerEvent>() (✅)
- S5-05 NarrativeSequencePlayer sender precedent (✅)

---

## Test Evidence Path

- **R3 PlayMode evidence**: `production/qa/playmode-popup-auto-blocker-2026-05-XX.md` (Phase 4 dev-story 写入 — next session 起)
- **R3 JSON evidence dump**: `~/Library/Application Support/DefaultCompany/Unity/S6-08_Result.json` (spike 输出 — Phase 3 实测)
- **Unit test evidence**: `Assets/Tests/EditMode/InputSystem/InputBlockerTests.cs` (✅ 9 test PASS — S5-05 已 production for InputBlocker stack semantic baseline coverage)

---

## History

- **2026-05-13 afternoon (Sprint 6 Session 30 Phase 2 + Phase 3 + Phase 4 + Phase 5 closure)** — ✅ **DONE**:
  - **Phase 2 production code 实施**:
    - `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIModule.InputBlocker.cs` **NEW** ~85 行 — partial class `TryFireInputBlockerPush/Pop(UIWindow, fromHideUI=false)` helper + `_inputBlockerPoppedByHide` HashSet state tracking (防 HideUI delayed close → timer → CloseUI 双 fire) + `ShouldFireInputBlocker(UIWindow)` static layer filter (`layer == (int)UILayer.Top || layer == (int)UILayer.Tips`)
    - `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIModule.cs` ~+20 行 minor amend — 6 hook entry insert: `ShowUIImp(Type)` / `ShowUIImp<T>` / `ShowUIAwaitImp<T>` 3 first-show 各 Push 后 fire push + `TryGetWindow` re-show 路径 (Pop+Push 后) fire push + `CloseUI(Type)` `InternalDestroy()` 之前 fire pop + `HideUI(Type)` 真 hide 路径 (HideTimeToClose > 0) `Visible=false` 之前 fire pop fromHideUI:true
    - `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6_08_MockPanels.cs` **NEW** ~160 行 — 7 mock UIWindow class (MockTopPanel + MockTopPanel2 [Top=2] + MockTipsPanelA/B/C [Tips=3] + MockBottomPanel [Bottom=0] + MockSystemPanel [System=4]) 各 [Window] attribute + 静态 LastInstance tracking
    - `Assets/Editor/DevTest/S6_08_MockPanelsGenerator.cs` **NEW** ~120 行 — Editor MenuItem `Tools/S6-08/Generate Mock Panel Prefabs (All)` 7 prefab batch generate (Canvas + GraphicRaycaster + Image 程序化 layout，无 Button child 因 spike 直接调 CloseUI<T>())
    - 7 `Assets/Resources/UI/Mock*.prefab` **NEW** (Tools/S6-08/Generate batch — MockTopPanel + MockTopPanel2 + MockTipsPanelA/B/C + MockBottomPanel + MockSystemPanel)
    - `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-08_PopupAutoBlocker.cs` **NEW** ~680 行 — 1 spike + 2 inner class (S608Spike/S608Runtime/S608Tester) + 5 R3 case (P1 TopLayerSenderVerify + P2 UIBottomSystemNoFire + P3 TipsPopupQueueChain + P4 SortingDepthVerify + P5 PauseResumeClearQueue) + JSON evidence dump `~/Library/Application Support/DefaultCompany/Unity/S6-08_Result.json` + Phase 3 dp9 NEW closure amend (P3 enqueue 顺序对齐 + P3/P5 头/尾 ClearAndClosePopupQueue cleanup)
    - `Assets/GameScripts/HotFix/GameLogic/GameApp.cs` minor amend — RegisterDevSpikes S607Spike → S608Spike 切换 + S6-07 done note added
    - `Assets/GameScripts/HotFix/GameLogic/GameFlow/DevTestState.cs` minor amend — `HasSpike("S5-02") || HasSpike("S6-07")` → `... || HasSpike("S6-08")` (V3.0.1 dp8 candidate +1 = 3，距 V3.1 trigger 阈值 4 还差 1)
  - **Phase 3 R3 PlayMode 实测**:
    - 第 1 跑 (16:10) — P1 ✅ + P2 ✅ + P3 ❌ + P4 ❌ + P5 ❌ → 暴露 **V3.0.1 dp9 NEW popup queue spec wording drift**：Phase 1 spec wording "priority DESC + enqueueOrder ASC tiebreak 决定 show order" 与 vendor 实际行为不符 — vendor `EnqueuePopup` 内 `if (_currentPopupType == null && !_isPopupQueuePaused) TryShowNextPopup()` → **first enqueue (cur=null 时) 立即 show**，priority 只影响**后续 queue insertion order**；P3 cleanup 残留污染 P4/P5 (cur=MockTipsPanelB 未真 close 因 CloseUI<B> no-op B 不在 stack)
    - Phase 3 spike amend (V3.0.1 dp9 NEW closure) — P3 enqueue 顺序与 priority DESC 顺序对齐 (A=30, B=20, C=10) 使 enqueue 顺 == show 顺 + P3/P5 头尾 `ClearAndClosePopupQueue()` cleanup 保证 state 隔离
    - 第 2 跑 (16:14) — **5/5 case PASS + 35/35 asserts + `all_passed=true` + Push=8 Pop=8 对称 + 0 unexpected error + Total elapsed 1038ms < 5s perf budget**
  - **Phase 4 Evidence doc 写入**:
    - `production/qa/playmode-popup-auto-blocker-2026-05-13.md` **NEW** ~430 行 — 8 section evidence doc (§0 概要 + §1 R3 5 case detail + §2 R2 8/8 closure + §3 AC 10/10 verify + §4 V3.0.1 dp9 NEW closure + dp8 candidate +1 + dp7 NEW 复用 + dp1 absorbed + NEW dp10 candidate (partial class scoped scope 模式) + §5 Sprint 6 Track C insight + §6 Files changed + §7 References + §8 Verdict)
  - **Phase 5 Closure** (本 entry):
    - Status: Phase 0+1 readiness-gate-done → **✅ Done** (Phase 2+3+4+5 全 closure)
    - sprint-status.yaml S6-08 status: phase-1-readiness-gate-done → done + completion details
    - session-state/active.md Phase + Next milestone update (Sprint 6 Track C: S6-07 ✅ + S6-08 ✅ + S6-04 🔜)
    - V3.0.1 dp9 NEW popup queue spec wording drift closure entry — ADR-029 V3.x watch list candidate 沉淀
  - **R3 Push tokens (8)**: P1 Top (1) + P3 TipsA + TipsB + TipsC (3) + P4 Top + Top2 + TipsA (3) + P5 TipsA (1) = 8 total
  - **R3 Pop tokens (8)**: P1 Top + P3 TipsA + TipsB + TipsC + P4 TipsA + Top2 + Top + P5 TipsA = 8 total (push-pop 对称 实证 sender 链路 well-formed)
  - **V3.0.1 dp9 NEW closure governance insight** — Phase 1 readiness gate R2 verify 阶段未完整模拟 vendor `EnqueuePopup` 内 trigger pattern；Phase 3 第 1 跑 R3 fail 暴露；Phase 3 spec amend + spike rewrite 走 V2.0 §V2-1.b 第二轮 verify 路径 closure；ADR-029 V3.x watch list candidate (popup queue spec wording 系列 — 未来 ADR 写 popup queue 行为需细化 "first enqueue immediate show vs priority sort distinction"；vendor `EnqueuePopup` 内 `if (_currentPopupType == null && !_isPopupQueuePaused) TryShowNextPopup()` 是核心 trigger pattern)
  - **NEW dp10 candidate** (UIModule partial class scoped scope 模式) — `UIModule.InputBlocker.cs` 与 `UIModule.PopupQueue.cs` 同模式；V3.1 trigger 阈值 partial class file count >= 5 时评估
  - **Phase 2/3/4/5 投入** ~3-4 hr (Phase 2 UIModule + 7 mock + Generator + 7 prefab + spike ~1.5-2 hr + Phase 3 第 1 跑 + dp9 NEW closure amend + 第 2 跑 ~30-45 min + Phase 4 evidence doc ~30-40 min + Phase 5 closure ~15-20 min)

- **2026-05-13 morning (Sprint 6 Session 30 Phase 0 R2 verify + Phase 1 readiness gate)**:
  - Phase 0 R2 vendor reality verify — 7 finding 实证：(1) UIModule.PopupQueue.cs popup queue 已 production (priority DESC + enqueueOrder ASC tiebreak — 不 simple FIFO) (2) UIModule.OnSortWindowDepth same-layer sorting 已 production (`depth = layer * LAYER_DEEP + N * WINDOW_DEEP`) (3) InputBlocker.cs stack semantic 已 production + 9 unit test (4) `Singleton<InputBlocker>` / `class InputManager` 0-production (Sprint 7+ ADR-010 epic) (5) UIModule.ShowUI/CloseUI/HideUI 全 0-call IInputBlockerEvent (真 deficiency = Auto InputBlocker sender-side 未实施) (6) WindowAttribute UILayer enum 4 ctor 已 R2 verify per S5-08 + UIWindow.WindowLayer 是 `int` 不是 UILayer enum (7) S5-05 NarrativeSequencePlayer 已 IInputBlockerEvent sender precedent (line 312/323)
  - Narrow scope [A] sender-only 决策 (~2 SP) — UIModule.ShowUIImp/CloseUI/HideUI 内对 Top(2)/Tips(3) layer panel 自动 fire `IInputBlockerEvent.OnPushBlocker/OnPopBlocker(token = type.FullName)`；listener-side 留 Sprint 7+ ADR-010 InputManager epic；R3 走 spike subscribe IInputBlockerEvent assert event count + token format pattern (S5-05 P3 listener spy precedent)
  - Phase 1 story-008-ui-layer-strategy.md 完整 rewrite — 从 Sprint 0 framework time placeholder (5 wording drift 包括 ShowWindow/CloseWindow → ShowUI/CloseUI; UILayer Background/HUD/Popup/Overlay/System → vendor Bottom/UI/Top/Tips/System; PauseMenuPanel/ChapterTransitionPanel/HUDPanel 等假设面板; IInputService.GetBlockerStack() → InputBlocker.BlockerCount 不存在; FIFO popup queue → 实际 priority DESC + enqueueOrder ASC tiebreak) → V3.0.1 vendor reality compliant + S6-08 sender-only narrow scope [A] 4 button group 类似 S6-07 rewrite 模式
  - Phase 1 R1+R2+R3 readiness gate verdict ✅ **DEFICIENCY-FLAGGED PASS** (R1 PASS 0 forbidden listener pattern + R2 DEFICIENCY-FLAGGED PASS R2.1~R2.6 ✅ + R2.7 ⚠️ DEFERRED + R2.8 + R2.9 ⚠️ TBD + R3 PASS 5 case spec consistency)
  - S5-08 narrow scope descope 残留 closure mapping 表完整列出 — popup queue / sorting / InputBlocker stack 三项 vendor 已自带 (verify-only R3 case)；Auto InputBlocker sender-side 是 S6-08 真核心 work；listener-side wiring Sprint 7+ ADR-010 epic
  - V3.0.1 Watch List Hooks — Type-9 dp1 closure 应用 (R2.6 IInputBlockerEvent fan-out check) + Type-5 dp7 NEW reinforce (mock fixture protected override 复 S6-07 precedent) + Type-5 dp6 不触发 + Type-8 dp1 可能触发 R3 P3 留观察 + **NEW dp8 candidate** (DevTestState `[main-menu]` mode 复用 阈值阶进 V3.1 trigger 候选 — 阈值 spike count >= 4 当前 2 + 本 story Phase 2 +1 = 3 距阈值 1 个)
  - **Phase 0/1 投入** ~1 hr (Phase 0 R2 grep verify 7 finding ~20 min + narrow scope 决策 AskQuestion 双轮 ~10 min + Phase 1 story rewrite outline + write ~30 min)
  - Phase 2 production code (UIModule.cs ~+30-40 行 TryFireInputBlockerPush/Pop helper + 5 mock UIWindow fixture file ~150-200 行 + S6_08_MockPanelsGenerator.cs Editor + 5 prefab regenerate + S6-08_PopupAutoBlocker.cs spike ~500-600 行 + GameApp RegisterDevSpikes 切换 + DevTestState `[main-menu]` mode 扩 V3.0.1 dp8 candidate +1) defer 下一 session 起 per CLAUDE.md session ≤5 hr hard rule
- **2026-XX-XX (Sprint 0 framework time placeholder — superseded 2026-05-13 morning per V3.0.1 vendor reality compliant rewrite)**: 早期 placeholder version — 5 wording drift 包括 ShowWindow/CloseWindow API / UILayer Background-Overlay 命名 / 假设业务面板 (PauseMenuPanel / ChapterTransitionPanel / HUDPanel / SettingsPanel) / IInputService.GetBlockerStack 不存在的 API / FIFO popup queue (实际 priority DESC + enqueueOrder ASC tiebreak) 等；本 story Phase 1 rewrite 全部 supersede per V3.0.1 vendor reality compliant + S6-08 sender-only narrow scope [A]
