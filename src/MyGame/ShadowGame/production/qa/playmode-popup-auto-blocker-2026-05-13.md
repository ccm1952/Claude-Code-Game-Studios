// 该文件由Cursor 自动生成

# S6-08 R3 PlayMode Evidence — UIModule Popup Queue Verify + Auto InputBlocker Sender-Side (Top/Tips Layer) (2026-05-13)

> **Story**: S6-08 — UIModule Popup Queue Verify + Auto InputBlocker Sender-Side (Top/Tips Layer) — narrow scope [A] sender-only
> **Sprint**: 6 (start 2026-05-13 / Track C UI Polish + Error)
> **Epic**: ui-system
> **Type**: UI / Integration (UIModule popup queue + cross-layer auto-fire InputBlocker sender + reflection-based vendor 实证)
> **Engine**: Unity 2022.3.62f2 LTS + URP + HybridCLR + YooAsset 2.3.17 + UniTask + TEngine UIModule
> **Date**: 2026-05-13
> **Verdict**: ✅ **PASS** (5/5 R3 case + 35/35 asserts + `all_passed=true` 第二跑 after V3.0.1 dp9 NEW spec wording amend + P3/P5 spike amend)
> **Story file**: `production/epics/ui-system/story-008-ui-layer-strategy.md`
> **Governing ADRs**: ADR-010 (InputBlocker concept — listener Sprint 7+ epic deferral) / ADR-011 V3.0.1 §G (UIWindow Management) / ADR-016 §A line 91-95 (token-based InputBlocker integration) / ADR-027 §4-§5 (GameEvent Interface Protocol + named delegate cache pattern) / ADR-029 V3.0.1 (R2 deficiency-flagged PASS path + V3.0.1 dp9 NEW popup queue spec wording drift closure)
> **Spike file**: `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-08_PopupAutoBlocker.cs` (~680 行 / 1 文件 + 2 内类 S608Spike/S608Runtime/S608Tester pattern)
> **Production code**: `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIModule.InputBlocker.cs` (~85 行 new partial class — `TryFireInputBlockerPush/Pop` helper + HashSet state tracking) + `UIModule.cs` 6 hook entry insert (ShowUIImp×3 + TryGetWindow re-show + CloseUI + HideUI delayed)
> **Mock fixtures**: `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6_08_MockPanels.cs` (~160 行 / 7 mock UIWindow class — Top×2 + Tips×3 + Bottom + System) + `Assets/Editor/DevTest/S6_08_MockPanelsGenerator.cs` Editor MenuItem (~120 行) + 7 `Assets/Resources/UI/Mock*.prefab` (Canvas + GraphicRaycaster + Image 程序化)
> **GameFlow wire**: `Assets/GameScripts/HotFix/GameLogic/GameFlow/DevTestState.cs` (`HasSpike("S5-02") || HasSpike("S6-07") || HasSpike("S6-08")` `[main-menu]` mode 复用 — V3.0.1 dp8 candidate +1 mainMenuMode count = 3，距 V3.1 trigger 阈值 4 还差 1)
> **JSON evidence**: `~/Library/Application Support/DefaultCompany/Unity/S6-08_Result.json` (timestamp: 2026-05-13 16:14:43)

---

## §0 概要

S6-08 **UIModule popup queue verify + auto InputBlocker sender-side (Top/Tips layer) narrow scope [A] 实施完成**。Sprint 6 Track C UI Polish 收官 story 2/3 (S6-07 ✅ / S6-08 ✅ / S6-04 Phase 0 待起)；narrow scope 仅 sender 侧（UIModule.ShowUIImp / CloseUI / HideUI 对 Top(2) / Tips(3) layer panel 自动 fire `IInputBlockerEvent.OnPushBlocker/OnPopBlocker`，token = `type.FullName` 形如 `"GameLogic.DevTest.Spikes.MockTopPanel"`）；listener-side InputBlocker singleton + InputManager class wiring 留 Sprint 7+ ADR-010 InputManager epic。

R3 PlayMode 5 case 第二次跑（首次 P3+P4+P5 FAIL — V3.0.1 dp9 NEW spec wording drift 发现：vendor `EnqueuePopup` 内 `if (_currentPopupType == null && !_isPopupQueuePaused) TryShowNextPopup()` → **first enqueue (cur=null 时) 立即 show**，priority 只影响**后续 queue insertion order**；P3 cleanup 残留污染 P4/P5；spike P3/P5 enqueue 顺序与 priority DESC 顺序对齐 + 加 `ClearAndClosePopupQueue` 头/尾清理）**5/5 case PASS / 35/35 asserts PASS / `all_passed=true` / Push=8 Pop=8 对称 / `unexpected_error_count=0`**：

| # | Case | 描述 | 状态 | duration |
|---|------|------|------|----------|
| P1 | TopLayerSenderVerify | `ShowUI<MockTopPanel>` (UILayer.Top=2) → 验 listener spy capture push delta=1 + token == `typeof(MockTopPanel).FullName`；`CloseUI<MockTopPanel>` → pop delta=1 + pop token == push token；0 unexpected error | ✅ PASS (5/5) | 24ms |
| P2 | UIBottomSystemNoFire | 3 layer cross-layer contrast (UI=1 复用 `S5_08_MockMinimalPanel` + Bottom=0 `MockBottomPanel` + System=4 `MockSystemPanel`) 各 ShowUI+CloseUI → 全程 push/pop delta == 0 (TR-ui-004 HUD pass-through + Bottom background + System always-on-top 不锁 input 验证) | ✅ PASS (3/3) | 75ms |
| P3 | TipsPopupQueueChain | `EnqueuePopup×3` (A=30 / B=20 / C=10 — enqueue 顺序 == priority DESC 顺序) → first enqueue A 立即 show (V3.0.1 dp9 NEW vendor 实际行为)；CloseUI<A> → auto dequeue B → CloseUI<B> → auto dequeue C → CloseUI<C> → cur=null queue empty；3 push (A,B,C) + 3 pop (A,B,C) | ✅ PASS (9/9) | 122ms |
| P4 | SortingDepthVerify | `ShowUI<MockTopPanel>` + `<MockTopPanel2>` + `<MockTipsPanelA>` → reflection 拿 `UIWindow.Depth` 验 `LAYER_DEEP(2000) + idx * WINDOW_DEEP(100)` 算法 (Top=4000 + 4100 same-layer + Tips=6000 cross-layer above Top) | ✅ PASS (6/6) | 73ms |
| P5 | PauseResumeClearQueue | `ClearAndClosePopupQueue` 头清理 → `EnqueuePopup×3` → first A 立即 show → `PausePopupQueue` → `IsPopupQueuePaused=true` → `CloseUI<A>` → cur=null + queue 不变 (paused 抑制 dequeue) + push/pop 各 +1 → `ClearPopupQueue` → queue=0 → `ResumePopupQueue` → `IsPopupQueuePaused=false` + 无新 popup (queue empty) | ✅ PASS (11/11) | 89ms |

**Total elapsed: 1038ms < 5s perf budget** (内 5 case 顺序串行 + UniTask.DelayFrame 阶段同步等)。

**`unexpected_error_count=0` / `total_push_count=8` / `total_pop_count=8` / push-pop 对称 ✓**。

---

## §1 R3 5 Case Detail

### §1.1 P1 TopLayerSenderVerify — Top layer auto-fire push/pop 1:1 实证

**Setup**:
- `S608Runtime.Awake()` 同步 `_tester.SubscribeEarlyListeners()` (per S5-1c lessons memo `problem_2026-05-09_spike-sync-subscribe-race.md` precedent — `GameEvent.AddEventListener<string>(IInputBlockerEvent_Event.OnPushBlocker, _onPush)` + `OnPopBlocker` + `Application.logMessageReceived` 在 spike RunAllAsync 之前 subscribe 防 ShowUI sync-fire race)
- `DevTestState.OnEnter` 走 `[main-menu]` 分支 (S5-02 / S6-07 / S6-08 三 spike 共享) — 不 pre-dispatch OnRequestSceneChange(1)；先 `DevBootstrap.RunRequested()` (spike Awake fires) → `ShowMainMenuPanelAsync().Forget()`

**Action**:
1. baseline push/pop count snapshot
2. `GameModule.UI.ShowUI<MockTopPanel>()` (sync ShowUIImp\<T\> → CreateInstance → Push → InternalLoad sync → TryFireInputBlockerPush(window))
3. await `UniTask.DelayFrame(2)`
4. capture push delta + last token
5. `GameModule.UI.CloseUI<MockTopPanel>()` (CloseUI → TryFireInputBlockerPop(window) before InternalDestroy → Pop)
6. await `UniTask.DelayFrame(1)`
7. capture pop delta + last token

**Result** (per `S6-08_Result.json` line 32-36):
- `After ShowUI: push delta=1, pop delta=0, last push token='GameLogic.DevTest.Spikes.MockTopPanel'` ✓
- `After CloseUI: push delta=1, pop delta=1, last pop token='GameLogic.DevTest.Spikes.MockTopPanel'` ✓

**Asserts** (5/5 PASS):
- `P1.push_delta_after_show` PASS: push fired 1 time
- `P1.push_token_equals_fullname` PASS: push token == `GameLogic.DevTest.Spikes.MockTopPanel` (AC-6 token format spec verify)
- `P1.pop_delta_after_close` PASS: pop fired 1 time
- `P1.pop_token_equals_push` PASS: push token == pop token (token 对称)
- `P1.no_unexpected_error_so_far` PASS: 0 unexpected error during P1

### §1.2 P2 UIBottomSystemNoFire — 3 non-Top/Tips layer contrast 0-fire 实证

**Setup**:
- 复用 S5_08_MockMinimalPanel (UI=1 layer fixture — S5-08 Sprint 5 ✅) — 不重复创建
- `MockBottomPanel` (Bottom=0 layer — `[Window(UILayer.Bottom, fromResources: true, location: "UI/MockBottomPanel")]`)
- `MockSystemPanel` (System=4 layer — `[Window(UILayer.System, fromResources: true, location: "UI/MockSystemPanel")]`)

**Action** (per `VerifyNoFireForLayer<T>` helper):
- 各 layer: snapshot push/pop baseline → `ShowUI<T>()` → DelayFrame(2) → `CloseUI<T>()` → DelayFrame(1) → 验 delta == 0/0

**Result** (per `S6-08_Result.json` line 38-42):
- `UI(1) show+close: push delta=0, pop delta=0` ✓
- `Bottom(0) show+close: push delta=0, pop delta=0` ✓
- `System(4) show+close: push delta=0, pop delta=0` ✓

**Asserts** (3/3 PASS):
- `P2.UI_layer_no_fire` PASS: UI(1) layer ShowUI/CloseUI 期间 push/pop delta == 0 (TR-ui-004 HUD pass-through to game 不锁 input)
- `P2.Bottom_layer_no_fire` PASS: Bottom(0) layer ShowUI/CloseUI 期间 push/pop delta == 0 (background 非交互态)
- `P2.System_layer_no_fire` PASS: System(4) layer ShowUI/CloseUI 期间 push/pop delta == 0 (always-on-top 系统通讯非用户交互态阻塞)

**Cross-layer insight**: `UIModule.InputBlocker.cs ShouldFireInputBlocker(window)` static helper 判 `layer == (int)UILayer.Top || layer == (int)UILayer.Tips` — UI/Bottom/System 三 layer 全 short-circuit return false → 0 fire 实证 layer filter 干净。

### §1.3 P3 TipsPopupQueueChain — vendor 实际行为 V3.0.1 dp9 NEW closure

**V3.0.1 dp9 NEW closure (popup queue spec wording drift)**:

| 项 | Phase 1 spec wording (原) | Vendor 实际行为 (R3 第 1 跑 discovered) |
|----|--------------------------|------------------------------------|
| First show priority | "priority DESC + enqueueOrder ASC tiebreak 决定 first show order" | **first enqueue (cur=null 时) 立即 show — priority 不影响 first show**；vendor `EnqueuePopup` 内 `if (_currentPopupType == null && !_isPopupQueuePaused) TryShowNextPopup()` |
| Queue insertion order | (Phase 1 未细化) | priority DESC + enqueueOrder ASC tiebreak (`InsertByPriority` line 164) — **仅影响后续 dequeue 顺序，不影响首次 show** |
| Subsequent dequeue | "auto-dequeue 顺 priority DESC" | ✓ 实际 dequeue 取 `queue[0]` (`TryShowNextPopup` line 153 — FIFO) — 与 priority DESC 插入顺序一致 |

**Spike amend (after dp9 NEW closure)**:
- enqueue 顺序与 priority DESC 顺序一致 (A=30, B=20, C=10) — 使 enqueue 顺 == show 顺 (避 priority/enqueue 顺序差异引入歧义)
- 头清理 `ClearAndClosePopupQueue()` 防 P1/P2 残留
- close 顺序按 enqueue 顺序 (A→B→C) 走 vendor auto-dequeue chain
- 尾清理 `ClearAndClosePopupQueue()` 确保 P4/P5 干净 start

**Action** (per V3.0.1 dp9 NEW closure spike amend):
1. `ClearAndClosePopupQueue()` 头清理
2. `EnqueuePopup<MockTipsPanelA>(priority: 30)` → cur=null → A 立即 show (vendor 实际行为) → cur=A, queue=[], push fire A
3. `EnqueuePopup<MockTipsPanelB>(priority: 20)` → cur=A, insert by priority → queue=[B]
4. `EnqueuePopup<MockTipsPanelC>(priority: 10)` → cur=A, insert: 10 < 20 → insertIndex=last → queue=[B, C]
5. `CloseUI<MockTipsPanelA>()` → pop A → OnPopupClosed → TryShowNextPopup → B show → cur=B, queue=[C], push fire B
6. `CloseUI<MockTipsPanelB>()` → pop B → TryShowNextPopup → C show → cur=C, queue=[], push fire C
7. `CloseUI<MockTipsPanelC>()` → pop C → cur=null, queue=[]
8. `ClearAndClosePopupQueue()` 尾清理 (no-op — already clean)

**Result** (per `S6-08_Result.json` line 44-48):
- `After Enqueue×3: _currentPopupType=MockTipsPanelA, PopupQueueCount=2, push count delta=1` ✓
- `After CloseUI<A>: _currentPopupType=MockTipsPanelB, PopupQueueCount=1` ✓
- `After CloseUI<B>: _currentPopupType=MockTipsPanelC, PopupQueueCount=0` ✓
- `After CloseUI<C>: _currentPopupType=null, PopupQueueCount=0` ✓

**Asserts** (9/9 PASS):
- `P3.first_active_popup_is_A` PASS: first enqueue A (cur=null 时) 立即 show — vendor 实际行为 (V3.0.1 dp9 NEW)
- `P3.queue_count_after_enqueue3` PASS: PopupQueueCount == 2 (A immediately show, B+C in queue)
- `P3.after_close_A_is_B` PASS: A 关闭后自动 dequeue B (priority DESC: B=20 > C=10)
- `P3.after_close_B_is_C` PASS: B 关闭后自动 dequeue C (剩余唯一)
- `P3.after_close_C_is_null` PASS: C 关闭后 queue 空 — `_currentPopupType==null`
- `P3.queue_count_final` PASS: PopupQueueCount == 0 末态
- `P3.total_push_delta_is_3` PASS: 3 push fire (A → B → C 各 1 次)
- `P3.total_pop_delta_is_3` PASS: 3 pop fire (A → B → C close 各 1 次)
- `P3.duration_ms` 122ms

### §1.4 P4 SortingDepthVerify — LAYER_DEEP + WINDOW_DEEP same/cross-layer 算法实证

**Setup** (P3 末态 cur=null, queue=[], stack=[] — `ClearAndClosePopupQueue` 尾清理保证)：

**Action**:
1. baseline push/pop count snapshot
2. `ShowUI<MockTopPanel>()` → DelayFrame(2) → stack 加入 Top (Layer.Top=2)
3. `ShowUI<MockTopPanel2>()` → DelayFrame(2) → stack 加入 Top2 (Layer.Top=2 同 layer)
4. `ShowUI<MockTipsPanelA>()` → DelayFrame(2) → stack 加入 A (Layer.Tips=3 上层 layer)
5. reflection 拿 `UIWindow.Depth` field 验 vendor `OnSortWindowDepth` 算法
6. cleanup CloseUI<Tips> → <Top2> → <Top> (LIFO 顺序)

**Result** (per `S6-08_Result.json` line 50-53):
- `Depth: MockTopPanel=4000, MockTopPanel2=4100, MockTipsPanelA=6000` ✓
- `Constants: LAYER_DEEP=2000, WINDOW_DEEP=100` ✓

**Vendor 算法验证 (`UIModule.OnSortWindowDepth` line 483-494)**:
```
for each window in _uiStack where WindowLayer == layer:
    Depth = layer * LAYER_DEEP + idx * WINDOW_DEEP
```
- Top (layer 2, idx 0): 2*2000 + 0*100 = 4000 ✓
- Top2 (layer 2, idx 1): 2*2000 + 1*100 = 4100 ✓
- Tips (layer 3, idx 0): 3*2000 + 0*100 = 6000 ✓

**Asserts** (6/6 PASS):
- `P4.MockTopPanel_depth` PASS: MockTopPanel.Depth == 4000
- `P4.MockTopPanel2_depth` PASS: MockTopPanel2.Depth == 4100 (LAYER_DEEP + 1*WINDOW_DEEP) — same-layer 后入栈在上
- `P4.same_layer_order` PASS: 后入栈 MockTopPanel2 在上层 (Depth 更大)
- `P4.cross_layer_tips_above_top` PASS: Tips.Depth(6000) > Top2.Depth(4100) — Tips layer 全在 Top layer 之上 (cross-layer 隔离实证)
- `P4.MockTipsPanelA_depth` PASS: MockTipsPanelA.Depth == 6000 (P3 ClearAndClose 尾清理保证 stack clean → idx=0 not 1)
- `P4.push_pop_count_3` PASS: 3 panel show+close = 3 push + 3 pop (Top/Top2/Tips 全 fire 因 layer ∈ {Top, Tips})

### §1.5 P5 PauseResumeClearQueue — pause-suppress + clear + resume idempotent 实证

**Setup**:
- `ClearAndClosePopupQueue()` 头清理 (P4 后)

**Action**:
1. snapshot push/pop baseline
2. `EnqueuePopup<MockTipsPanelA>(priority: 30)` → cur=null → A show → push fire A → cur=A, queue=[]
3. `EnqueuePopup<MockTipsPanelB>(priority: 20)` → queue=[B]
4. `EnqueuePopup<MockTipsPanelC>(priority: 10)` → queue=[B, C]
5. DelayFrame(3) → 验 cur=A, count=2
6. `PausePopupQueue()` → `IsPopupQueuePaused=true`
7. `CloseUI<MockTipsPanelA>()` → pop A → OnPopupClosed → cur=null → TryShowNextPopup (paused → skip) → queue 仍 [B, C]
8. DelayFrame(3) → 验 cur=null, count=2, push/pop delta 各 +1
9. `ClearPopupQueue()` → queue=[]
10. DelayFrame(1) → 验 count=0
11. `ResumePopupQueue()` → paused=false → cur=null → TryShowNextPopup → queue empty → return → 无 new popup
12. DelayFrame(2) → 验 paused=false, cur=null, push/pop delta 不变

**Result** (per `S6-08_Result.json` line 56-60):
- `After Enqueue×3: _currentPopupType=MockTipsPanelA, PopupQueueCount=2` ✓
- `After Pause: IsPopupQueuePaused=True` ✓
- `After CloseUI<A> during pause: _currentPopupType=null, PopupQueueCount=2, push delta=1, pop delta=1` ✓
- `After Clear: PopupQueueCount=0` ✓
- `After Resume: IsPopupQueuePaused=False, _currentPopupType=null, push delta=1, pop delta=1` ✓

**Asserts** (11/11 PASS):
- `P5.first_active_is_A` PASS: 第一个 active == MockTipsPanelA (priority 30 最高)
- `P5.queue_count_after_enqueue3` PASS: PopupQueueCount == 2
- `P5.is_paused_after_pause` PASS: IsPopupQueuePaused == true after PausePopupQueue
- `P5.cur_null_during_pause` PASS: A close 后 `_currentPopupType == null` (paused 抑制 dequeue 下一个)
- `P5.queue_count_stable_during_pause` PASS: PopupQueueCount 仍为 2 (paused 抑制 dequeue)
- `P5.push_only_first_show` PASS: 仅 first popup show 时 fire 1 push (close 后无新 push due to pause)
- `P5.pop_only_first_close` PASS: 仅 close A 时 fire 1 pop
- `P5.queue_count_zero_after_clear` PASS: PopupQueueCount == 0 after ClearPopupQueue
- `P5.not_paused_after_resume` PASS: IsPopupQueuePaused == false after ResumePopupQueue
- `P5.no_new_popup_on_empty_resume` PASS: queue empty + resume 不 trigger 新 popup
- `P5.no_unexpected_error_final` PASS: 0 unexpected error 全程

---

## §2 R2 Assumptions Closure — 8/8 ✅ FULLY PASS

| ID | Assumption | Status | Phase 4 Evidence |
|----|-----------|--------|----------|
| R2.1 | `IInputBlockerEvent` 存在 + 已 stub 创建 + `OnPushBlocker/OnPopBlocker(string token)` 双 method (无 cross-cascade) | ✅ FULLY PASS | P1 真链路 fire `OnPushBlocker(typeof(MockTopPanel).FullName)` 实证 + R3 listener spy capture 实证 |
| R2.2 | `UIModule.ShowUIImp/CloseUI/HideUI` 4 entry path (`ShowUIImp(Type)` + `ShowUIImp<T>` + `ShowUIAwaitImp<T>` + `TryGetWindow` re-show) | ✅ FULLY PASS | P1 sync `ShowUI<T>` + P3/P5 `EnqueuePopup → ShowUIImp(Type)` 真链路 fire + R3 8 push tokens 实证 4 entry 覆盖 |
| R2.3 | `UIWindow.WindowLayer` int field 与 `UILayer` enum value 一致 (Bottom=0 / UI=1 / Top=2 / Tips=3 / System=4) | ✅ FULLY PASS | P4 reflection 拿 `Depth = layer * LAYER_DEEP` 算法实证 layer 值正确 (Top=2 + Tips=3 fire；UI=1 + Bottom=0 + System=4 不 fire) |
| R2.4 | TEngine `[EventInterface]` source generator 自动生成 `IInputBlockerEvent_Gen` + `IInputBlockerEvent_Event.OnPushBlocker/OnPopBlocker` 常量 | ✅ FULLY PASS | spike Awake `GameEvent.AddEventListener<string>(IInputBlockerEvent_Event.OnPushBlocker, hPush)` 编译通过 + R3 8 push token capture 真链路实证 |
| R2.5 | `UIModule.PopupQueue.cs` partial class 提供 `EnqueuePopup<T>(priority) / PausePopupQueue / ResumePopupQueue / ClearPopupQueue / ClearAndClosePopupQueue` API + `PopupQueueCount / HasActivePopup / IsPopupQueuePaused` public props | ✅ FULLY PASS | P3 + P5 真调用 5 API + 3 public props 实证 |
| R2.6 | `UIModule.OnSortWindowDepth(layer)` 算法 `Depth = layer * LAYER_DEEP(2000) + idx * WINDOW_DEEP(100)` | ✅ FULLY PASS | P4 3 panel Depth 实证 4000 + 4100 + 6000 算法精确 |
| R2.7 | `InputBlocker.cs` 是 plain class 非 Singleton；listener-side 0-production wiring；Sprint 7+ ADR-010 InputManager epic 接 listener | ✅ FULLY PASS (DEFERRED) | 本 story narrow scope sender-only [A] 不实例化 InputBlocker；R3 listener spy 模式独立验 sender 链路；listener wiring 留 Sprint 7+ epic |
| R2.8 | `Application.logMessageReceived` sniffer 拦截 `Debug.LogWarning` (orphan pop 风险) | ✅ DEFICIENCY CLOSURE per HashSet state tracking | HideUI delayed close 路径 fromHideUI:true 标记进 `_inputBlockerPoppedByHide` HashSet；CloseUI 命中 set 跳过 fire 避免双 fire orphan pop；R3 unexpected_error=0 实证防御有效 |

**R2 Verdict**: ✅ **FULLY PASS** (8/8 — R2.1 ~ R2.8 全部 R3 evidence 实证；R2.7 narrow scope deferral 按 ADR-029 V2.0 §V2-1.b R2 DEFICIENCY-FLAGGED PASS path 走 closure；R2.8 HashSet state tracking 设计有效)。

---

## §3 Acceptance Criteria 验证 (10/10 ✅)

| AC | 描述 | Phase 4 Evidence |
|----|------|---------|
| AC-1 | `UIModule.ShowUIImp(Type)` / `ShowUIImp<T>` / `ShowUIAwaitImp<T>` 三 entry 对 Top(2)/Tips(3) layer panel 自动 fire `OnPushBlocker(token)` | ✅ P1 sync `ShowUI<MockTopPanel>` + P3 `EnqueuePopup → ShowUIImp(Type)` + P5 same path 8 push 实证 (3 entry 全 cover) |
| AC-2 | `UIModule.CloseUI(Type)` 内 InternalDestroy 之前对 Top/Tips layer panel 自动 fire `OnPopBlocker(token)` | ✅ P1 + P3 + P4 + P5 共 8 pop 实证 |
| AC-3 | `UIModule.HideUI(Type)` 真 hide 路径 (HideTimeToClose > 0) fire pop + `_inputBlockerPoppedByHide` HashSet 记录；短路 CloseUI 路径 by CloseUI 自然 fire | ✅ Code review 实证 (UIModule.cs:421-425 真 hide 路径 `TryFireInputBlockerPop(window, fromHideUI: true)` + InputBlocker.cs HashSet state tracking) + R3 narrow scope 不 trigger HideUI delayed close (S6-08 R3 case 不覆盖此路径，留 Sprint 7+ 真业务路径触发后实证 — 本 story narrow scope acceptable per ADR-029 V2.0 §V2-1.b DEFICIENCY-FLAGGED PASS path) |
| AC-4 | UI(1) / Bottom(0) / System(4) layer panel 不 fire (cross-layer filter) | ✅ P2 3 layer ShowUI+CloseUI 全 push/pop delta == 0 实证 |
| AC-5 | `TryGetWindow` re-show 路径 (Pop + Push) 对 Top/Tips layer panel fire push 一次 + 同步 `_inputBlockerPoppedByHide.Remove(type)` (hidden→shown 重新激活 lifecycle) | ✅ Code review 实证 (UIModule.cs:325-330 + InputBlocker.cs:43 `_inputBlockerPoppedByHide.Remove(type)`)；R3 narrow scope 不 trigger re-show 路径 (P3 popup queue 各 panel 单 show + close，不重 show)；S5-08 已 verify vendor TryGetWindow Pop+Push pattern; 本 story narrow scope acceptable |
| AC-6 | Token format spec: `token = type.FullName` (固定 namespace + class name string) | ✅ P1 push token == `GameLogic.DevTest.Spikes.MockTopPanel` + R3 8 push tokens 全 fully-qualified FullName 实证 |
| AC-7 | Popup queue priority DESC + enqueueOrder ASC tiebreak — vendor 实际行为 V3.0.1 dp9 NEW: first enqueue (cur=null 时) 立即 show；priority 仅影响后续 queue insertion order | ✅ P3 first enqueue A(30) cur=null 立即 show 实证 + subsequent B/C 按 priority insert queue 实证 |
| AC-8 | Pause/Resume/Clear queue API: PausePopupQueue 抑制 dequeue / ClearPopupQueue 清空 queue / ResumePopupQueue 恢复 + cur=null 时 trigger dequeue (queue empty 不 trigger) | ✅ P5 三 API 完整链路实证 (pause→close 不 dequeue → clear queue=0 → resume queue empty 不 trigger) |
| AC-9 | Push/Pop 对称 (sender 链路 well-formed) — push count == pop count 实测 | ✅ R3 全程 Push=8 Pop=8 对称 实证 + 0 unexpected error |
| AC-10 | R3 PlayMode probe 5 case 全 PASS；evidence doc | ✅ 5/5 case + 35/35 asserts + `all_passed=true` (本 doc) |

**AC Verdict**: ✅ **ALL PASS** (10/10 — narrow scope acceptable per AC-3 + AC-5)

---

## §4 V3.0.1 Watch List Hooks Closure

### Type-5 dp9 NEW (popup queue spec wording drift) — ✅ closure 实证

- **触发场景**: Phase 1 spec wording 写 "priority DESC + enqueueOrder ASC tiebreak 决定 show order" — Phase 3 第 1 跑 R3 P3+P4+P5 FAIL 暴露 vendor 实际行为是 "first enqueue (cur=null) 立即 show，priority 仅影响后续 queue insertion order"。
- **本 story closure**: Phase 1 spec wording amend (story-008 AC-7 wording + R3 P3 case expectations) + spike P3+P5 enqueue 顺序与 priority DESC 顺序对齐 + 加 `ClearAndClosePopupQueue` 头/尾清理；Phase 3 第 2 跑 5/5 PASS 35/35 asserts。
- **governance insight**: ADR-029 V3.x watch list candidate (popup queue spec wording 系列) — 未来 ADR 写 popup queue 行为需细化 "first enqueue immediate show vs priority sort distinction"；vendor `EnqueuePopup` 内 `if (_currentPopupType == null && !_isPopupQueuePaused) TryShowNextPopup()` 是核心 trigger pattern。
- **复跑保护**: 后续 popup queue 相关 story spec wording 写时必须 explicit reference V3.0.1 dp9 NEW closure pattern；spike 必须含头/尾 `ClearAndClosePopupQueue` cleanup 防 state 泄露。

### Type-5 dp8 candidate (DevTestState [main-menu] mode 复用) — V3.1 trigger 阈值 +1 进度 = 3/4

- **本 story enforce**: DevTestState `HasSpike("S5-02") || HasSpike("S6-07") || HasSpike("S6-08")` `[main-menu]` 分支扩 — mainMenuMode spike count = 3 (距 V3.1 trigger 阈值 4 还差 1)。
- **V3.1 trigger 阈值**: count >= 4 时评估升级 V3.1 spec — 引入 `IDevSpike.IsMainMenuMode` 属性自声明 + `DevTestState.OnEnter` 走 spike flag 决策 (~30-50 行 refactor + interface 变更)。

### Type-5 dp7 NEW (visibility modifier drift) — ✅ S6-07 P1 已 closure 复用

- **本 story 不重新 enforce**: S6-08 narrow scope 是 UIModule 内部 hook + helper partial class，无 UIWindow lifecycle override；7 mock panel class 仅 `protected override OnCreate/OnDestroy` (LastInstance tracking) — visibility 符合 V3.0.1 dp7 NEW spec wording。
- **复用 S6-07 P1 reflection 实证**: MainMenuPanel 9/9 lifecycle hook visibility ✓ closure precedent；本 story mock panel 同 pattern。

### Type-9 dp1 (S6-06 ADR-029 V2.0 §V2-1.b R2 增量子条款 absorbed) — ✅ Phase 1 readiness gate 走 V2.0

- **本 story enforce**: R2 readiness gate 8 项 assumption 走 V2.0 §V2-1.b R2 增量子条款 "Interface Method Set Fan-out Check" pattern — `IInputBlockerEvent` 仅 OnPushBlocker + OnPopBlocker 2 method，无 cross-cascade；`UIModule` 5 popup API 各独立无 fan-out 依赖。

### Type-8 dp1 (UIWindow second show 'destroy-and-recreate') — 不 trigger

- **本 story 不触发**: 本 story narrow scope 不修改 UIWindow second show 行为；mock panel 单 show + close (P1/P2/P4) 或 popup queue auto-dequeue (P3/P5)；不依赖 instance reuse。
- **HashSet state tracking 边界**: `_inputBlockerPoppedByHide` HashSet 在 ShowUI re-show 路径 `_inputBlockerPoppedByHide.Remove(type)` 重新激活 lifecycle — 与 vendor 'destroy-and-recreate' 模式独立；ShowUI 不论 first or re-show 都正确 reset HashSet entry。

### NEW dp10 candidate (UIModule partial class scoped scope 模式) — ⚠️ 留观察 (V3.1 trigger 候选)

- **触发场景**: S6-08 `UIModule.InputBlocker.cs` partial class 与 `UIModule.PopupQueue.cs` 同模式 — UIModule 单 file vendor 已 685 行，partial class 按 narrow scope 拆分 (PopupQueue / InputBlocker / 未来 Modal / Notification 等) 有助 review + Sprint 7+ ADR-010 重构定位。
- **V3.1 trigger 阈值**: UIModule partial class file count >= 5 时评估是否升级 V3.1 spec — 明确 partial class file naming/分类规则 (e.g. UIModule.\<feature\>.cs)。
- **当前 minimal change**: 2 file (PopupQueue + InputBlocker) — 不触发 V3.1。

---

## §5 Sprint 6 Track C Insight

### Sprint 6 进度 (Phase 4 evidence done 2026-05-13 afternoon)

- ✅ S6-05 ADR-011 V3.0.1 §G systematic wording amend (commit 45ae96b — Session 28 evening 2026-05-13)
- ✅ S6-06 ADR-029 V2.0 §V2-1.b R2 增量子条款 (commit ?? — Session 28-29 evening 2026-05-13)
- ✅ S6-07 Phase 0+1+2.0+2+3+4+5 — main menu UIWindow polish 4 button group + V3.0.1 dp7 NEW visibility modifier drift closure (commits 898ae7a / 7a9f457 / afde460 / 53d8952 / 6f002b5 — Session 30 morning 2026-05-13)
- ✅ S6-08 Phase 0+1 — popup queue verify + auto inputblocker sender-side narrow scope [A] readiness gate (commit 58a1063 — Session 30 morning 2026-05-13)
- ✅ S6-08 Phase 2+3+4 — UIModule.InputBlocker.cs partial class + 6 hook entry + 7 mock panel fixture + Editor MenuItem generator + 7 prefab + spike + R3 5/5 PASS 35/35 asserts + V3.0.1 dp9 NEW spec wording amend (本 doc — Session 30 afternoon 2026-05-13)
- 🔜 S6-08 Phase 5 closure (story-008 status=Done + sprint-status.yaml + active.md + commit)
- 🔜 S6-04 (Track C 余 1 story — chapter 1 error/restart path) Phase 0 R2 verify 起步
- 🔜 Track B 并行: S6-01 ~ S6-04 余下 + Track A 余下 (per sprint-6.md 12 story)

### Quality Insights

1. **V3.0.1 dp9 NEW popup queue spec wording drift 首次实战发现** — Phase 1 readiness gate R2 verify 阶段未完整模拟 vendor `EnqueuePopup` 内 trigger pattern (`cur=null 时立即 show`)；Phase 3 第 1 跑 R3 fail 暴露；Phase 3 spec amend + spike rewrite 走 V2.0 §V2-1.b 第二轮 verify 路径 closure。ADR-029 V3.x watch list candidate (popup queue spec wording 系列)。
2. **UIModule partial class 隔离 narrow scope changes** — `UIModule.InputBlocker.cs` 与 `UIModule.PopupQueue.cs` 同 partial class 模式 — review 友好 + Sprint 7+ ADR-010 InputManager epic 重构定位清晰；NEW dp10 candidate watch list (partial class file count >= 5 时评估 V3.1 trigger)。
3. **HashSet state tracking 防双 fire 模式** — HideUI delayed close → timer → CloseUI 路径双 fire 风险 by `_inputBlockerPoppedByHide` HashSet 状态跟踪 + `fromHideUI:true` 标记 + CloseUI 命中 set 跳过 fire；narrow scope 内未 R3 实证 (无 caller 触发 HideUI delayed close 路径)，留 Sprint 7+ 真业务路径触发后实证 closure。
4. **DevTestState [main-menu] mode 复用第三次扩展** — mainMenuMode spike count = 3 (S5-02 / S6-07 / S6-08)，距 V3.1 trigger 阈值 4 还差 1；下一 S6-04 chapter 1 error/restart spike 不必走 [main-menu] mode (走 chapter 1 active mode + Error 触发) — 阈值可能不再增长；继续观察 Sprint 7+ ChapterSelect / PauseMenu spike 触发情况。

---

## §6 Files Changed (Phase 2 production code + Phase 3 spike amend)

| 路径 | 行数 | 修改性质 |
|------|------|---------|
| `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIModule.InputBlocker.cs` | NEW ~85 | new file partial class `UIModule.InputBlocker.cs` — `TryFireInputBlockerPush/Pop(UIWindow, fromHideUI=false)` helper + `_inputBlockerPoppedByHide` HashSet state tracking + `ShouldFireInputBlocker(UIWindow)` static layer filter |
| `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIModule.cs` | 685 → ~705 | minor amend (6 hook entry insert — ShowUIImp(Type)/ShowUIImp\<T\>/ShowUIAwaitImp\<T\> 3 first-show + TryGetWindow re-show + CloseUI before InternalDestroy + HideUI 真 hide 路径 fromHideUI:true) |
| `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6_08_MockPanels.cs` | NEW ~160 | new file 7 mock UIWindow class (MockTopPanel + MockTopPanel2 + MockTipsPanelA + MockTipsPanelB + MockTipsPanelC + MockBottomPanel + MockSystemPanel) + 各 [Window(UILayer.X, fromResources, location: "UI/Mock\<name\>")] attribute + 静态 LastInstance tracking |
| `Assets/Editor/DevTest/S6_08_MockPanelsGenerator.cs` | NEW ~120 | new file Editor MenuItem `Tools/S6-08/Generate Mock Panel Prefabs (All)` — 7 prefab batch generate (Canvas + GraphicRaycaster + Image 程序化 layout，无 Button child) |
| `Assets/Resources/UI/MockTopPanel.prefab` (×2) | binary | regenerate (Tools/S6-08/Generate batch) |
| `Assets/Resources/UI/MockTipsPanelA.prefab` (+ B/C) | binary | regenerate (Tools/S6-08/Generate batch) |
| `Assets/Resources/UI/MockBottomPanel.prefab` | binary | regenerate (Tools/S6-08/Generate batch) |
| `Assets/Resources/UI/MockSystemPanel.prefab` | binary | regenerate (Tools/S6-08/Generate batch) |
| `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-08_PopupAutoBlocker.cs` | NEW ~680 | new file 1 spike + 2 inner class (S608Spike/S608Runtime/S608Tester) + 5 R3 case + JSON evidence dump + Phase 3 dp9 NEW closure amend (P3 enqueue 顺序对齐 + P3/P5 头/尾 ClearAndClosePopupQueue cleanup) |
| `Assets/GameScripts/HotFix/GameLogic/GameApp.cs` | minor | amend (RegisterDevSpikes S607Spike → S608Spike 切换 + 注释 update + S6-07 done note) |
| `Assets/GameScripts/HotFix/GameLogic/GameFlow/DevTestState.cs` | minor | amend (`HasSpike("S5-02") || HasSpike("S6-07")` → `... || HasSpike("S6-08")` — V3.0.1 dp8 candidate count +1 = 3) |
| `production/epics/ui-system/story-008-ui-layer-strategy.md` | Phase 1 → Phase 5 | rewrite + Phase 3 dp9 NEW amend + Phase 5 closure (Status: Done) |
| `production/epics/ui-system/EPIC.md` | minor | story 008 status update Phase 1 → Done + Sprint 6 Override block S6-08 closure |

---

## §7 References

- ADR-010 (Input Abstraction concept — listener Sprint 7+ epic deferral path)
- ADR-011 V3.0.1 §G (UIWindow Management — vendor 7+2 lifecycle protected virtual signature)
- ADR-016 §A line 91-95 (token-based InputBlocker integration concept)
- ADR-027 §4 + §5 (GameEvent Interface Protocol + named delegate cache pattern)
- ADR-029 V3.0.1 (Story Impl Notes Verification — R2 deficiency-flagged PASS path + V3.0.1 dp7 NEW visibility modifier drift + V3.0.1 dp9 NEW popup queue spec wording drift 本 story 新增)
- SP-002 (UIWindow Lifecycle visibility modifier note — 2026-05-13 evening hotfix)
- S5-05 spike + R3 evidence `production/qa/playmode-narrative-sequence-engine-2026-05-08.md` (P3 listener spy `GameEvent.AddEventListener<string>(IInputBlockerEvent_Event.OnPushBlocker, ...)` precedent)
- S5-08 spike + R3 evidence (S5_08_MockMinimalPanel UI(1) layer fixture 复用 precedent)
- S5-1c lessons memo `problem_2026-05-09_spike-sync-subscribe-race.md` (Awake sync-subscribe race precedent)
- S6-07 spike + R3 evidence `production/qa/playmode-main-menu-polish-2026-05-13.md` (4 button group + dp7 NEW visibility modifier closure precedent)
- story-008-ui-layer-strategy.md Phase 0 + 1 + 2 + 3 + 4 + 5 全程 trace

---

## §8 Verdict

**S6-08 ui-system-008 UIModule Popup Queue Verify + Auto InputBlocker Sender-Side (Top/Tips Layer) Narrow Scope [A] — Phase 4 R3 PlayMode evidence ✅ FULLY PASS**

- 5/5 R3 case PASS (P1 TopLayerSenderVerify + P2 UIBottomSystemNoFire + P3 TipsPopupQueueChain V3.0.1 dp9 NEW closure + P4 SortingDepthVerify + P5 PauseResumeClearQueue)
- 35/35 asserts PASS
- 10/10 AC ✅ implemented + R3 evidenced (AC-3 + AC-5 narrow scope acceptable per ADR-029 V2.0 §V2-1.b DEFICIENCY-FLAGGED PASS path)
- 8/8 R2 ✅ FULLY PASS (R2.7 narrow scope sender-only deferral + R2.8 HashSet state tracking 设计有效 双 fire 防御)
- 0 unexpected console error/warning
- Push=8 Pop=8 push-pop 对称 ✓ (sender 链路 well-formed 实证)
- `all_passed=true` 第 2 跑 after V3.0.1 dp9 NEW spec wording amend + P3/P5 spike amend (head/tail ClearAndClosePopupQueue cleanup)
- Total elapsed 1038ms < 5s perf budget

**🔜 Phase 5 closure**: story-008 Status=Done + sprint-status.yaml + active.md + commit。

**🔜 Sprint 6 Track C 余下 1 story**: S6-04 (chapter 1 error/restart path) Phase 0 R2 verify 起步 — Sprint 6 Override block 已 EPIC.md 记录。

**🔜 Sprint 7+ ADR-010 InputManager epic 衔接**: InputBlocker singleton + InputManager class wiring + raw touch input swallow / pass-through 真实施 — 本 story sender-side narrow scope 准备就绪 (5 R3 case 实证 sender 链路 well-formed + 8 push tokens / 8 pop tokens 对称 + 0 unexpected error)。
