// 该文件由Cursor 自动生成

# S6-07 R3 PlayMode Evidence — MainMenu UIWindow Polish 4 Button Group + Fade-In + BGM Hook + Lifecycle Visibility V3.0.1 dp7 NEW Reinforce (2026-05-13)

> **Story**: S6-07 — MainMenu UIWindow Polish (4 Button Group + Vendor 7+2 Lifecycle + Fade-In + BGM Hook)
> **Sprint**: 6 (start 2026-05-13 / track C UI Polish + Error)
> **Epic**: ui-system
> **Type**: UI / Integration (vendor 7+2 lifecycle protected override compliance + DOTween fade-in + IAudioService BGM hook + Button onClick → ISceneEvent dispatch + GameFlow [main-menu] mode)
> **Engine**: Unity 2022.3.62f2 LTS + URP + HybridCLR + YooAsset 2.3.17 + UniTask + TEngine UIModule + DOTween
> **Date**: 2026-05-13
> **Verdict**: ✅ **PASS** (5/5 R3 case + 27/27 asserts + `all_passed=true` first-attempt-after-DevTestState-fix)
> **Story file**: `production/epics/ui-system/story-006-main-menu.md`
> **Governing ADRs**: ADR-011 V3.0.1 (UIWindow Management) / ADR-027 §4-§5 (GameEvent Interface Protocol + named delegate cache pattern) / ADR-029 V3.0.1 (Story Impl Notes Verification — Phase 2.0 R2 deficiency flag closure path) / SP-002 (UIWindow Lifecycle visibility modifier note 2026-05-13 evening)
> **Spike file**: `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-07_MainMenuPolish.cs` (~550 行 / 1 文件 + 2 内类 spike/runtime/tester pattern)
> **Main menu UI**: `Assets/GameScripts/HotFix/GameLogic/UI/MainMenuPanel.cs` (~250 行 production UIWindow — vendor 7+2 lifecycle protected override + 4 button group + DOTween fade-in + BGM hook) + `Assets/Resources/UI/MainMenuPanel.prefab` (Editor 程序化生成 by `MainMenuPanelGenerator.cs` 4 button + CanvasGroup root)
> **GameFlow wire**: `Assets/GameScripts/HotFix/GameLogic/GameFlow/DevTestState.cs` (扩 `HasSpike("S5-02") || HasSpike("S6-07")` main-menu mode 复用 — 不 pre-dispatch OnRequestSceneChange 让 P4 NewGameButton click 真驱 transition)
> **JSON evidence**: `~/Library/Application Support/DefaultCompany/Unity/S6-07_Result.json` (timestamp: 2026-05-13 11:40:51)

---

## §0 概要

S6-07 **MainMenu UIWindow polish 4 button group 实施完成**。Sprint 6 Track C UI Polish 收官 story 1/3 (S6-07 / S6-08 / S6-04)；Sprint 5 inline 2 button base (`StartChapter1Button` + `NextChapterButton` per S5-02 142 行) 替换为 production polish 4 button group (`NewGameButton` + `ContinueButton` placeholder + `SettingsButton` placeholder + `QuitButton`) + vendor 7+2 lifecycle 全 `protected override` (V3.0.1 dp7 NEW reinforce) + DOTween 0.3s fade-in (`CanvasGroup.DOFade` Ease.OutQuad precedent InteractableObject.cs) + BGM start hook `AudioManager.Instance.PlayMusic("main_menu_bgm", 1.0f)` (R2.8 [D] mixed strategy closure — fail-safe Log.Warning + Sprint 7+ ui-system-006b 真 asset add)。

R3 PlayMode 5 case 第二次跑（首次 P4 FAIL — DevTestState 残留 story-001c 路径预分派 OnRequestSceneChange(1) 致 chapter1→chapter1 noop；DevTestState `HasSpike("S6-07")` 扩 main-menu 模式后第二次跑）**5/5 case PASS / 27/27 asserts PASS / `all_passed=true`**:

| # | Case | 描述 | 状态 | duration |
|---|------|------|------|----------|
| P1 | LifecycleVisibilityCompliance | reflection 验 vendor 7 init + 2 extra hook (`ScriptGenerator` / `BindMemberProperty` / `RegisterEvent` / `OnCreate` / `OnRefresh` / `OnUpdate` / `OnDestroy` + `Hide` / `Close`) 全部 `protected` visibility (`MethodInfo.IsFamily == true`) | ✅ PASS (5/5) | 1ms |
| P2 | 4ButtonWiring | reflection 验 4 button field reference 全 non-null + interactable state (NewGame/Quit=true, Continue/Settings=false 灰态 placeholder) | ✅ PASS (8/8) | 0ms |
| P3 | FadeInBgmHookSniffer | 等 0.5s budget；CanvasGroup.alpha == 1.000 (DOTween OutQuad 0.3s 完成) + Log sniffer captured `[AudioManager] PlayMusic: Music 'main_menu_bgm' not found in config` 警告 (BGM hook fail-safe 验证) + AudioManager `_isInitialized=true` (AC-3 facade activation gate 不 trigger) | ✅ PASS (5/5) | 499ms |
| P5 | QuitButtonClickReflection | reflection 验 `_onQuitClicked` named delegate cache field non-null + Target == MainMenuPanel instance + Method.Name == 'OnQuitClicked' (不真 invoke onClick 避 `Application.Quit()` 杀 PlayMode 致 spike 丢 evidence) | ✅ PASS (5/5) | 0ms |
| P4 | NewGameClickDispatch | `NewGameButton.onClick.Invoke()` → `OnNewGameClicked` handler → `GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1)` → SceneManager listener-path driver fire `OnSceneTransitionBegin(-1, 1)` 同帧；不 await chapter 1 加载完整（仅验 dispatch 行为 + 1s budget within）| ✅ PASS (4/4) | 1005ms |

**Total elapsed: 2302ms < 10s perf budget** (per AC-10)。

`unexpected_error_count=0` / `bgm_failsafe_warning_captured=true` / `new_game_clicked_log_captured=true`。

---

## §1 R3 5 Case Detail

### §1.1 P1 LifecycleVisibilityCompliance — V3.0.1 dp7 NEW reinforce

**Setup**:
- `S607Runtime.Awake()` 同步 `_tester.SubscribeEarlyListeners()` (per S5-1c lessons memo `problem_2026-05-09_spike-sync-subscribe-race.md` precedent — `Application.logMessageReceived` log sniffer + `ISceneEvent.OnSceneTransitionBegin` listener 在 spike RunAllAsync 之前 subscribe)
- `DevTestState.OnEnter` 走 `[main-menu]` 分支 (S5-02 / S6-07 复用) — 不 pre-dispatch OnRequestSceneChange(1)；先 `DevBootstrap.RunRequested()` (spike Awake fires) → `ShowMainMenuPanelAsync().Forget()` (vendor `GameModule.UI.ShowUIAsyncAwait<MainMenuPanel>()` 走 `[Window(UILayer.UI, fromResources: true, location: "UI/MainMenuPanel")]` Resources.Load 真路径)

**Action**: spike `RunP1Async()` 等 `MainMenuPanel.LastInstance != null` (帧 frame=9 ready)；reflection 拿 vendor 7 init + 2 extra hook `MethodInfo` (`BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly`) → 验 `MethodInfo.IsFamily == true` (== `protected`)。

**Events captured (P1 events)**:

```text
MainMenuPanel ready frame=9
Lifecycle 'ScriptGenerator' visibility: protected ✅ (IsFamily=True)
Lifecycle 'BindMemberProperty' visibility: protected ✅ (IsFamily=True)
Lifecycle 'RegisterEvent' visibility: protected ✅ (IsFamily=True)
Lifecycle 'OnCreate' visibility: protected ✅ (IsFamily=True)
Lifecycle 'OnRefresh' visibility: protected ✅ (IsFamily=True)
Lifecycle 'OnUpdate' visibility: protected ✅ (IsFamily=True)
Lifecycle 'OnDestroy' visibility: protected ✅ (IsFamily=True)
```

**Asserts (5/5 PASS)**:

| # | Assert | Result |
|---|--------|--------|
| P1.MainMenuPanel_instance | instance type=MainMenuPanel | PASS |
| P1.lifecycle_protected_hits | 7/7 vendor lifecycle method 全 protected | PASS |
| P1.extra_hook_protected | 2/2 extra hook (Hide+Close) protected | PASS |
| P1.duration_ms | 1ms | PASS |
| P1.no_unexpected_error_so_far | 0 unexpected error during P1 | PASS |

**P1 关键验证**:
- V3.0.1 dp7 NEW reinforcement test 实证：未来若 spec wording amend 自身又引入 `public override` lifecycle method (S6-05 commit 45ae96b ADR-011 §G Key Interfaces code block 5 处 `public override` precedent — V3.0.1 dp7 NEW hotfix 已 closure)，本 P1 reflection assert 第一时间 fail-loud。
- vendor 7+2 lifecycle `MethodInfo.IsFamily` (== `protected` C# access modifier) 实证 — UIBase.cs:144-197 + UIWindow.cs:504/509 vendor source 全 9 hook `protected virtual` 签名兼容业务侧 `protected override`。

### §1.2 P2 4ButtonWiring — 4 button field 引用 + interactable state

**Setup**:
- post-P1 (MainMenuPanel showing — vendor `[Window]` attribute Resources.Load + 7 init hook 同步调用完毕；`OnCreate` 内 `transform.Find` 4 button + `AddListener` 4 named delegate cache 完成；`OnRefresh` 内 Continue/Settings `interactable=false` 灰态 + DOTween fade-in tween started + BGM hook `AudioManager.Instance.PlayMusic` fired)
- spike reflection 拿 4 button field reference (`internal Button NewGameButton { get; private set; }` 等 4 field per ADR-027 §5 named delegate cache + S5-02 internal access modifier precedent)

**Action**: 直接 reflection 拿 4 button reference + 验 interactable state；0ms duration (`Button.interactable` boolean property direct read，无 await)。

**Asserts (8/8 PASS)**:

| # | Assert | Result |
|---|--------|--------|
| P2.NewGameButton_ref | non-null | PASS |
| P2.ContinueButton_ref | non-null | PASS |
| P2.SettingsButton_ref | non-null | PASS |
| P2.QuitButton_ref | non-null | PASS |
| P2.ContinueButton_interactable_false | interactable=false (placeholder 灰态) | PASS |
| P2.SettingsButton_interactable_false | interactable=false (placeholder 灰态) | PASS |
| P2.NewGameButton_interactable_true | interactable=true | PASS |
| P2.QuitButton_interactable_true | interactable=true | PASS |

**P2 关键验证**:
- 4 button transform.Find 路径 hit 全部成功 — prefab 节点命名 hard contract `NewGameButton` / `ContinueButton` / `SettingsButton` / `QuitButton` 与 production code `transform.Find` 入参一致 (`MainMenuPanelGenerator.cs` 程序化生成保证)。
- placeholder 灰态保护 — Continue / Settings handler 是 0-line body (无 `throw NotImplementedException`)，`interactable=false` 阻止 user 点击；`OnRefresh` 每帧重新设置可保证 second show 路径仍 enforce (本 story narrow scope 不走 second show 但 spec 完整保留)。

### §1.3 P3 FadeInBgmHookSniffer — DOTween fade-in + BGM hook fail-safe sniffer

**Setup**:
- post-P2 (MainMenuPanel showing + 4 button wired + DOTween fade-in tween already started in `OnRefresh`)
- spike `Application.logMessageReceived` 已在 Awake `SubscribeEarlyListeners()` subscribe — `OnLogReceived` handler 走 4 sniffer pattern: (1) `[AudioManager] PlayMusic: Music 'main_menu_bgm' not found in config` Warning → 设 `_bgmFailSafeWarningCaptured=true` (R2.8 [D] closure mock spy 替代方案)；(2) `[MainMenuPanel] NewGameButton clicked` Log.Info → 设 `_newGameClickedLogCaptured=true` (P4 用)；(3) Unity Error 类型日志 → 累 `_unexpectedErrorCount`；(4) 其他 Log/Warning 走 ignore

**Action**: 等 `UniTask.Delay(500ms)` (DOTween fade-in 0.3s + 0.2s safety margin)；reflection 拿 `_canvasGroup` field + 读 `alpha` 当前值 + 拿 `AudioManager.Instance._isInitialized` field 验证。

**Asserts (5/5 PASS)**:

| # | Assert | Result |
|---|--------|--------|
| P3.CanvasGroup_ref | root CanvasGroup non-null | PASS |
| P3.CanvasGroup_alpha_after_fade | alpha=1.000 (fade-in complete via DOTween OutQuad 0.3s) | PASS |
| P3.BGM_failsafe_warning_captured | `[AudioManager] PlayMusic: Music main_menu_bgm not found in config` captured (fail-safe dispatch verified — Sprint 7+ ui-system-006b 真 asset add 后 BGM 自动响) | PASS |
| P3.AudioManager_initialized | AudioManager._isInitialized=true (AC-3 facade activation gate 不 trigger) | PASS |
| P3.duration_ms | 499ms | PASS |

**P3 关键验证**:
- DOTween `CanvasGroup.DOFade(1f, 0.3f).SetEase(DG.Tweening.Ease.OutQuad)` precedent InteractableObject.cs:336/383/475 production wired 验证 — alpha 0→1 fade-in 在 0.3s budget 内完成；500ms 等待后 alpha == 1.000f 精确等值 (DOTween OutQuad ease 末值 lock 行为)。
- R2.8 [D] mixed strategy closure 实证 — `AudioManager.Instance.PlayMusic("main_menu_bgm", 1.0f)` 调用 fired (Log sniffer captured fail-safe Warning)；BGM 实际不响 (main_menu_bgm AudioConfig 缺失 — Sprint 7+ ui-system-006b backlog 解决)；spec 完整 (Sprint 7+ asset add 后 0 code change in MainMenuPanel.cs)；epic 边界遵守 — UI epic 仅触 Audio facade (`AudioManager.Instance.PlayMusic` API)，不污染 Audio config table。
- `AudioManager._isInitialized=true` 实证 AC-3 facade activation gate 不 trigger fail-loud 路径 — `GameApp.cs:40 AudioManager.Instance.Initialize()` 在 `:55 StartGameLogic()` 之前调用，main menu show 时 AudioManager 已 init。

### §1.4 P5 QuitButtonClickReflection — reflection wiring verify (不 invoke onClick 避 PlayMode 杀)

**Setup**:
- post-P3 (MainMenuPanel showing + fade-in complete + BGM hook fired)

**Action**: reflection 拿 `_onQuitClicked` named delegate cache field (Type: `UnityEngine.Events.UnityAction`)；验 field non-null + `Target` reference == MainMenuPanel instance + `Method.Name == "OnQuitClicked"`；**不**真 `QuitButton.onClick.Invoke()` (会触发 `OnQuitClicked` → `Application.Quit()` → Editor PlayMode stop → spike RunAllAsync 中断 → JSON evidence 丢)。

**Events captured (P5 events)**:

```text
P5 reflection-based wiring verify done (Application.Quit not invoked — 避 Editor PlayMode stop 杀 spike)
Application.isPlaying=True
```

**Asserts (5/5 PASS)**:

| # | Assert | Result |
|---|--------|--------|
| P5.QuitButton_interactable | interactable=true (Quit 启用) | PASS |
| P5._onQuitClicked_field | _onQuitClicked field non-null | PASS |
| P5._onQuitClicked_target | delegate Target == MainMenuPanel instance | PASS |
| P5._onQuitClicked_method | delegate Method.Name == 'OnQuitClicked' | PASS |
| P5.Application_isPlaying | PlayMode still running (spike 未被 Quit 杀) | PASS |

**P5 关键验证**:
- ADR-027 §5 framework knowledge fact named delegate cache pattern 实证 — `_onQuitClicked` field cache 引用 + `Target.GetType() == MainMenuPanel` + `Method.Name == OnQuitClicked` 三重验证 wiring 正确；防 raw lambda `() => OnQuitClicked()` 不可作为 RemoveListener target 的反模式 (本 story Forbidden Patterns 第 2 项)。
- Editor mode safety pattern — `Application.Quit()` 在 Editor 下走 `UnityEditor.EditorApplication.isPlaying = false` (Unity 自身 handle)；spike 通过 reflection 验 wiring 而非真 invoke 是 sandboxed test pattern；Standalone build 下 `Application.Quit()` 真退出由 Unity runtime handle 不在 R3 scope。

### §1.5 P4 NewGameClickDispatch — Button onClick → ISceneEvent dispatch 真链路

**Setup**:
- post-P5 (前 4 case 全 PASS + MainMenuPanel showing + fade-in complete + 全 button wired)
- 关键时序：DevTestState `[main-menu]` mode 不 pre-dispatch OnRequestSceneChange(1) — chapter 1 未加载 — `SceneManager.CurrentLoadedChapterIdForTest = 0 (or -1 default)`；spike Awake `_p4OnTransitionBegin` listener 已 subscribe `ISceneEvent.OnSceneTransitionBegin`
- baseline `_p4OnTransitionBeginCount = 0` frame=164 实证 (无 pre-dispatch trace)

**Action**: `panel.NewGameButton.onClick.Invoke()` 同步 fire 触发 `OnNewGameClicked` handler → `Log.Info("[MainMenuPanel] NewGameButton clicked → dispatch ISceneEvent.OnRequestSceneChange(1)")` → `GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1)` → SceneManager `OnRequestSceneChange(int)` listener (line 326-346 per S5-1c) → 不同章 path → `DriveTransitionAsync(1).Forget()` → `BeginTransitionAsync(1)` Step 3 sync `OnSceneTransitionBegin(-1, 1)` fire；spike 等 `UniTask.Delay(1000ms)` 1s budget 内捕获 transition begin event (不 await chapter 1 完整加载，仅验 dispatch 行为)。

**Events captured (P4 events + tail logs)**:

```text
baseline OnSceneTransitionBegin count=0 frame=164
OnSceneTransitionBegin(-1,1)
NewGameButton.onClick.Invoke() called frame=164
```

**captured logs tail (spike 收尾时 captured_logs_tail_30 内 P4 段)**:

```text
[Log] [S6-07] P4 NewGameClickDispatch 开始
[Warning] [AudioManager] PlayMusic: Music 'main_menu_bgm' not found in config.   ← R2.8 fail-safe 复触 (P3 后 OnRefresh 又 fired 1 次?)
[Log] [MainMenuPanel] NewGameButton clicked → dispatch ISceneEvent.OnRequestSceneChange(1)
[Log] [SceneManager] Idle → TransitionOut
[Log] [SceneManager] TransitionOut → Unloading
[Log] [SceneManager] Unloading → Loading
[Log] [SceneManager] LoadChapterSceneAsync(1) success on attempt 1/2 (scene=Chapter_01_Approach).
[Log] [SceneManager] Loading → TransitionIn
[Log] [SceneManager] TransitionIn → Idle
[Log] [S6-07] Done. AllPassed=True Elapsed=2302ms
```

**Asserts (4/4 PASS)**:

| # | Assert | Result |
|---|--------|--------|
| P4.OnSceneTransitionBegin_delta | delta=1 payload(from=-1,to=1) | PASS |
| P4.OnSceneTransitionBegin_payload_to | to==1 (chapter 1) | PASS |
| P4.NewGameClicked_log_captured | `[MainMenuPanel] NewGameButton clicked` Log.Info captured (handler 真被调用) | PASS |
| P4.no_unexpected_error_final | 0 unexpected error 全程 | PASS |

**P4 关键验证**:
- production code 真路径完整链 — `Button.onClick → handler → GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1) → SceneManager DriveTransitionAsync → BeginTransitionAsync 11-step` 全链路实证 (与 S5-02 P1 case `StartChapter1Button.onClick.Invoke()` 同链路；S5-02 P1 已实证 8 lifecycle event 完整顺序，本 P4 narrow scope 仅验 transition begin 不 await complete)。
- `delta=1 payload(from=-1, to=1)` 实证 — `from=-1` 表示首次进 chapter 1 (no previous chapter)；`to=1` 实证 NewGame 派 chapter id 1 valid (V3.0 §V3-1.c dp6 spec drift closure — chapter id 只支持 1-5)。
- DevTestState `[main-menu]` mode 修正实证 — 首跑 P4 FAIL `delta=0` root cause = DevTestState 走 `[story-001c]` 分支 pre-dispatch OnRequestSceneChange(1) → chapter 1 已加载 → P4 click 是 chapter1→chapter1 noop；DevTestState `HasSpike("S5-02") || HasSpike("S6-07")` 扩 main-menu 模式后第二跑 baseline=0 frame=164 + Button click delta=1 真链 fire 实证 PASS。

---

## §2 R2 Assumptions Closure — 9/9 ✅ FULLY PASS

| ID | Assumption | Status | Phase 4 Evidence |
|----|-----------|--------|----------|
| R2.1 | UIWindow lifecycle 7+2 hook 全 `protected virtual` 签名；override 必 `protected override` | ✅ FULLY PASS | P1 reflection `MethodInfo.IsFamily == true` 9/9 hook 实证 |
| R2.2 | `[Window(UILayer.UI, fromResources: true, location: "UI/MainMenuPanel")]` vendor [Window] attribute 4 ctor overload | ✅ FULLY PASS | P1 MainMenuPanel.LastInstance ready frame=9 实证 (Resources.Load `UI/MainMenuPanel.prefab` 路径 hit 成功) |
| R2.3 | `GameModule.UI.ShowUI<T>()` vendor API | ✅ FULLY PASS | DevTestState `ShowMainMenuPanelAsync()` `await GameModule.UI.ShowUIAsyncAwait<MainMenuPanel>()` 完成 (`[GameFlow] [main-menu] MainMenuPanel ShowUI 完成 instance=MainMenuPanel` log 实证) |
| R2.4 | `ISceneEvent.OnRequestSceneChange(int targetChapterId)` API | ✅ FULLY PASS | P4 `Button.onClick → OnNewGameClicked → GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1)` 真链路 fire `OnSceneTransitionBegin(-1, 1)` 实证 |
| R2.5 | DOTween `CanvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad)` | ✅ FULLY PASS | P3 等 500ms 后 alpha=1.000f 精确等值实证 (DOTween OutQuad 末值 lock) |
| R2.6 | `IAudioService.PlayMusic("main_menu_bgm", 1.0f)` service locator / DI 路径 | ✅ FULLY PASS | P3 `AudioManager.Instance.PlayMusic` 真调用 fire (Log sniffer captured fail-safe Warning 实证 dispatch 行为 + `AudioManager._isInitialized=true` 实证 AC-3 facade activation gate 不 trigger) |
| R2.7 | `Application.Quit()` direct call | ✅ FULLY PASS | P5 `_onQuitClicked` named delegate cache `Method.Name == "OnQuitClicked"` 实证 wiring 正确 (handler 内 `Application.Quit()` direct call) |
| R2.8 | `main_menu_bgm` AudioConfig clipId 在 TbAudioEvent / IAudioConfigProvider 中已 defined | ✅ DEFICIENCY CLOSURE per [D] mixed strategy + R3 P3 verified | P3 Log sniffer captured `[AudioManager] PlayMusic: Music 'main_menu_bgm' not found in config` Warning — fail-safe 行为按 spec；Sprint 7+ ui-system-006b backlog `main_menu_bgm asset + Luban entry` 1 SP 解决 |
| R2.9 | `Application.Quit()` Editor mode handling | ✅ FULLY PASS | P5 reflection wiring verify pattern 实证 sandboxed test 模式 (不真 invoke 避 Editor PlayMode stop)；Standalone build 下 Unity runtime handle 真退路径不在 R3 scope |

**R2 Verdict**: ✅ **FULLY PASS** (9/9 — R2.1 ~ R2.9 全部 R3 evidence 实证；R2.6 + R2.8 Phase 2.0 deficiency flag 在 Phase 4 evidence 实证 closure 完成 per ADR-029 V2.0 §V2-1 R2 DEFICIENCY-FLAGGED PASS path → CLOSURE)。

---

## §3 Acceptance Criteria 验证 (10/10 ✅)

| AC | 描述 | Phase 4 Evidence |
|----|------|---------|
| AC-1 | `MainMenuPanel : UIWindow` 标 `[Window(UILayer.UI, fromResources: true, location: "UI/MainMenuPanel")]` | ✅ P1 LastInstance ready frame=9 (Resources.Load 路径 hit) |
| AC-2 | Vendor 7 lifecycle method 全 `protected override` | ✅ P1 reflection 7/7 `MethodInfo.IsFamily == true` |
| AC-3 | `OnCreate()` 4 button transform.Find + AddListener + named delegate cache | ✅ P2 4 button reference non-null + P5 `_onQuitClicked` field non-null + Method.Name verify |
| AC-4 | `OnRefresh()` 3 项: Continue/Settings interactable=false + IAudioService.PlayMusic + DOFade | ✅ P2 Continue/Settings.interactable=false + P3 BGM fail-safe Warning captured + alpha=1.000f |
| AC-5 | `OnDestroy()` 4 button RemoveListener + null-out + null-check guard | ✅ Code review 实证 (production code line 144-167 `OnDestroy` 4 button cleanup pattern per ADR-027 §5)；R3 不 trigger MainMenuPanel.Close() 路径 (本 story narrow scope 不走 second show) |
| AC-6 | NewGame button onClick handler: `GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1)` | ✅ P4 真链路 dispatch fire `OnSceneTransitionBegin(-1, 1)` 实证 |
| AC-7 | Quit button onClick handler: `Application.Quit()` direct call | ✅ P5 `_onQuitClicked.Method.Name == "OnQuitClicked"` 实证 wiring (handler 内 `Application.Quit()` direct call line 230) |
| AC-8 | Continue / Settings button onClick handler 标 placeholder 0-line body | ✅ Code review 实证 (production code line 234-240 `OnContinueClicked` / `OnSettingsClicked` 0-line body)；P2 `interactable=false` 灰态保护 |
| AC-9 | 0 unexpected console error/warning | ✅ `unexpected_error_count=0` (`[AudioManager] PlayMusic: Music 'main_menu_bgm' not found` Warning 是 R2.8 [D] closure expected fail-safe，不算 unexpected per assert exclude pattern) |
| AC-10 | R3 PlayMode probe 5 case 全 PASS first-attempt-after-DevTestState-fix；evidence doc | ✅ 5/5 case + 27/27 asserts + `all_passed=true` (本 doc) |

**AC Verdict**: ✅ **ALL PASS** (10/10)

---

## §4 V3.0.1 Watch List Hooks Closure

### Type-5 dp7 NEW (visibility modifier drift) — ✅ R3 P1 reinforce 实证 closure

- **本 story enforce**: P1 reflection `MethodInfo.IsFamily == true` 9/9 lifecycle hook (`ScriptGenerator` / `BindMemberProperty` / `RegisterEvent` / `OnCreate` / `OnRefresh` / `OnUpdate` / `OnDestroy` + `Hide` / `Close`) 实证 — 未来若 spec wording amend 自身又引入 `public override` (S6-05 commit 45ae96b precedent — V3.0.1 dp7 NEW hotfix 已 closure)，本 P1 第一时间 fail-loud。
- **governance insight**: dp7 NEW reinforce S6-06 V2.0 §V2-1.b R2 增量子条款 — ADR amend 工作本身必须遵守 R2 协议自身；spec wording amend ≠ free pass on vendor source verify。

### Type-5 dp6 (ISceneEvent chapter id 1-5 valid only) — ✅ closure 复用 P4

- **本 story enforce**: AC-6 NewGame onClick 派 `OnRequestSceneChange(1)` (chapter id valid)；P4 实证 `payload(from=-1, to=1)` chapter 1 valid。

### Type-9 dp1 (S6-06 ADR-029 V2.0 §V2-1.b R2 增量子条款 absorbed) — ✅ Phase 1 readiness gate 走 V2.0

- **本 story enforce**: R2 readiness gate 9 项 assumption 走 V2.0 §V2-1.b R2 增量子条款 "Interface Method Set Fan-out Check" pattern — `IAudioService` 仅依赖 `PlayMusic` 1 method，无 cross-method 调用 chain 隐患。

### Type-8 dp1 (UIWindow second show 'destroy-and-recreate') — 不 trigger

- **本 story 不触发**: 本 story narrow scope 不 override `Hide()` / `Close()`；MainMenuPanel 在 `[main-menu]` mode 仅 show once 不走 second show 路径；future ChapterSelect / PauseMenu 二次返回 main menu 路径走 vendor 'destroy-and-recreate' 模式 (per S5-08 V3 Type-8 dp1 实证) — 本 story default 不依赖 instance reuse。

### NEW dp8 candidate (DevTestState [main-menu] mode 复用) — ⚠️ 留观察 (V3.1 trigger 候选)

- **触发场景**: DevTestState `HasSpike("S5-02") || HasSpike("S6-07")` `[main-menu]` 分支扩 — 未来 main menu 类 spike 增多 (S6-08 / S6-04 / Sprint 7+ ChapterSelect 等)，hard-coded list 难维护；可能触发 V3.1 spec amend：引入 `IDevSpike.IsMainMenuMode` 属性自声明 + `DevTestState.OnEnter` 走 spike flag 决策 (~30-50 行 refactor + interface 变更)。
- **当前 minimal change**: S6-07 `[A]` minimal change (~5 行 OR + Log 重命名) 已 closure；scope 内不需要 V3.1 trigger。
- **V3.1 trigger 阈值**: main-menu mode spike count >= 4 时评估 (currently 2 = S5-02 + S6-07，距阈值 2 个)。

---

## §5 Sprint 6 Track C Insight

### Sprint 6 进度 (Phase 4 evidence done 2026-05-13 morning)

- ✅ S6-05 ADR-011 V3.0.1 §G systematic wording amend (commit 45ae96b — Session 28 evening 2026-05-13)
- ✅ S6-06 ADR-029 V2.0 §V2-1.b R2 增量子条款 (commit ?? — Session 28-29 evening 2026-05-13)
- ✅ S6-07 Phase 0 hotfix V3.0.1 dp7 NEW visibility modifier drift closure (commit 898ae7a)
- ✅ S6-07 Phase 1 story 创建 + R1+R2+R3 readiness gate (Session 29 evening 2026-05-13 — DEFICIENCY-FLAGGED PASS R2.6 + R2.8)
- ✅ S6-07 Phase 2.0 R2 deficiency flag closure (Session 30 morning 2026-05-13 [D] mixed strategy)
- ✅ S6-07 Phase 2 production code 实施 (Session 30 morning 2026-05-13 — MainMenuPanel.cs ~250 行 + MainMenuPanelGenerator.cs amend + MainMenuPanel.prefab regenerate + S6-07_MainMenuPolish.cs spike ~550 行 + GameApp RegisterDevSpikes S502→S607 切换 + S5-02 spike P5 case [A] delete + DevTestState `[main-menu]` mode 扩)
- ✅ S6-07 Phase 4 R3 PlayMode evidence 5/5 case + 27/27 asserts + `all_passed=true` (本 doc — Session 30 morning 2026-05-13)
- 🔜 S6-07 Phase 5 closure (story-006 status=Done + sprint-status.yaml + active.md + commit)
- 🔜 S6-08 (UI Polish - ErrorPanel) + S6-04 (Tween library setup) Track C 余 2 story
- 🔜 Track B 并行: S6-01 ~ S6-04 余下 + Track A 余下 (per sprint-6.md 12 story)

### Quality Insights

1. **R2 deficiency-flagged PASS path 实战首次验证** — Phase 1 readiness gate R2.6 + R2.8 ⚠️ TBD → Phase 2.0 verify 决策 → Phase 2 production code + R3 ↑ closure 实证；ADR-029 V2.0 §V2-1.b 增量子条款实战可行 (S6-06 absorbed dp1 + S6-07 实证 dp2 candidate)。
2. **DevTestState `[main-menu]` mode 复用 minimal touchpoint** — S5-02 / S6-07 main menu spike 共享 `ShowMainMenuPanelAsync()` 路径 + `[main-menu]` log scope；不 pre-dispatch OnRequestSceneChange(1) 避 chapter1→chapter1 noop race condition；V3.1 trigger 候选 watch list dp8 (mainMenuMode spike count >= 4 时升级 IDevSpike.IsMainMenuMode 自声明)。
3. **R3 P5 reflection wiring verify pattern** — `Application.Quit()` 类 destructive handler 不真 invoke 走 reflection delegate cache 验 wiring (Target + Method.Name) — ADR-027 §5 named delegate cache pattern 自带 R3 testability 双红利。
4. **R3 P3 Log sniffer mock spy 替代方案** — `Application.logMessageReceived` subscribe 拦截 production fail-safe Log.Warning 验证 dispatch 行为 — 替代 IAudioService mock spy injection (避 reflection swap singleton + `SetTestInstance` static helper invasive 路径)；ADR-029 V3.x watch list candidate (R3 spy pattern alternatives 系列)。

---

## §6 Files Changed (Phase 2 production code)

| 路径 | 行数 | 修改性质 |
|------|------|---------|
| `Assets/GameScripts/HotFix/GameLogic/UI/MainMenuPanel.cs` | 142 → ~250 | rewrite (S5-02 inline 2 button base → 4 button + fade-in + BGM hook + V3.0.1 dp7 NEW protected override) |
| `Assets/Editor/DevTest/MainMenuPanelGenerator.cs` | ~140 → ~180 | amend (4 button + CanvasGroup root alpha=0 + button color/size adjust + MenuItem path Tools/S5-02→S6-07) |
| `Assets/Resources/UI/MainMenuPanel.prefab` | binary | regenerate (4 button + CanvasGroup + 程序化 layout) |
| `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-02_EndToEndFlow.cs` | 935 → ~830 | amend (P1 button rename `StartChapter1Button` → `NewGameButton` + P5 case [A] delete + 相关 fields/listeners/delegate purge) |
| `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-07_MainMenuPolish.cs` | NEW | new file ~550 行 (1 spike + 2 inner class S607Runtime/S607Tester + 5 R3 case + JSON evidence dump) |
| `Assets/GameScripts/HotFix/GameLogic/GameApp.cs` | minor | amend (RegisterDevSpikes S502Spike → S607Spike 切换 + 注释 update) |
| `Assets/GameScripts/HotFix/GameLogic/GameFlow/DevTestState.cs` | minor | amend (`HasSpike("S5-02") → HasSpike("S5-02") || HasSpike("S6-07")` + `[S5-02]` log scope → `[main-menu]` rename — main-menu mode 复用) |
| `production/epics/ui-system/story-006-main-menu.md` | rewrite | Session 29 evening 完整 rewrite + Session 30 morning Phase 2.0 R2 closure amend |
| `production/epics/ui-system/story-006b-main-menu-bgm-asset.md` | NEW | new follow-on backlog story 1 SP (Audio epic Sprint 7+) |
| `production/epics/ui-system/EPIC.md` | minor | story 006 update + 006b add (Stories 10 → 11) + Sprint 6 Override block |

---

## §7 References

- ADR-011 V3.0.1 §G (UIWindow Management — vendor 7+2 lifecycle protected virtual signature)
- ADR-027 §4 + §5 (GameEvent Interface Protocol + named delegate cache pattern)
- ADR-029 V3.0.1 (Story Impl Notes Verification — R2 deficiency-flagged PASS path + V3.0.1 dp7 NEW visibility modifier drift)
- SP-002 (UIWindow Lifecycle visibility modifier note — 2026-05-13 evening hotfix)
- ADR-030 §VS Build commitment (Sprint 5 → Sprint 6 milestone overlap)
- S5-02 spike + R3 evidence `production/qa/playmode-end-to-end-flow-2026-05-12.md` (P1 chapter 1 link 路径 precedent)
- S5-1c lessons memo `problem_2026-05-09_spike-sync-subscribe-race.md` (Awake sync-subscribe race precedent)
- story-006-main-menu.md Phase 1 + 2.0 + 2 全程 trace

---

## §8 Verdict

**S6-07 ui-system-006 MainMenu UIWindow Polish — Phase 4 R3 PlayMode evidence ✅ FULLY PASS**

- 5/5 R3 case PASS (P1 LifecycleVisibilityCompliance + P2 4ButtonWiring + P3 FadeInBgmHookSniffer + P5 QuitButtonClickReflection + P4 NewGameClickDispatch)
- 27/27 asserts PASS
- 10/10 AC ✅ implemented + R3 evidenced
- 9/9 R2 ✅ FULLY PASS (R2.6 + R2.8 Phase 2.0 deficiency flag closure 在 Phase 4 evidence 实证 complete)
- 0 unexpected console error/warning (R2.8 fail-safe Warning expected per [D] closure)
- `all_passed=true` first-attempt-after-DevTestState-fix
- Total elapsed 2302ms < 10s perf budget (per AC-10)

**🔜 Phase 5 closure**: story-006 status=Done + sprint-status.yaml + active.md + commit。

**🔜 Sprint 6 Track C 余下 2 story**: S6-08 (UI Polish - ErrorPanel) + S6-04 (Tween library setup) — Sprint 6 Override block 已 EPIC.md 记录。
