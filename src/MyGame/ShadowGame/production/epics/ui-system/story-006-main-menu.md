// 该文件由Cursor 自动生成

# Story: MainMenu UIWindow Polish — 4 Button Group + Vendor 7+2 Lifecycle + Fade-In + BGM Hook

> **Epic**: ui-system
> **Story ID**: ui-system-006
> **Sprint**: 6 (S6-07 — Track C UI Polish + Error)
> **Story Type**: UI / Integration
> **Complexity Points**: 2
> **GDD Requirement**: TR-ui-005 (9 UIWindows defined) + TR-ui-016 (UI animations at 60fps)
> **ADR References**: ADR-011 V3.0.1 (UIWindow Management) + ADR-027 §4 (GameEvent Interface Protocol) + ADR-029 V3.0.1 (Story Impl Notes Verification — Phase 0 R2 verify gate) + SP-002 (UIWindow Lifecycle — visibility modifier note 2026-05-13 evening)
> **Status**: Phase 0 ✅ + Phase 1 ✅ + Phase 2.0 R2 deficiency flag closure ✅ Ready for Phase 2 production code
> **Created**: 2026-05-13 evening (Session 29 — rewrite from Sprint 0 placeholder per V3.0.1 vendor reality compliant + S6-07 narrow scope [A] decision)
> **Updated**: 2026-05-14 morning (Session 30 — Phase 2.0 R2.6 + R2.8 deficiency flag closure per [D] mixed strategy: R2.6 ✅ FULLY RESOLVED `AudioManager.Instance` Singleton path + GameApp.cs:40-55 init order ✅；R2.8 ✅ DEFICIENCY CLOSURE — main_menu_bgm clipId 不存在 + PlayMusic fail-safe Log.Warning+no-op，遵 epic 边界 Phase 2 不改 AudioConfigFromLuban 跨 Audio epic；新建 Sprint 7+ follow-on backlog `ui-system-006b: main_menu_bgm asset + Luban entry` 1 SP；R3 P3 case 走 mock spy invocation assert 不真 playback verify)
> **Depends on**: S5-08 ✅ + S5-02 ✅ + S6-05 ✅ + S6-06 ✅ + S6-07 Phase 0 hotfix ✅

---

## Context

S5-02 dev-story Phase 2.3c 已实施 minimal inline 2 button base (`Start Chapter 1` + `Next Chapter`) per `q_panel_placement [A]` decision — production path `Assets/GameScripts/HotFix/GameLogic/UI/MainMenuPanel.cs` (142 行)；Sprint 6 polish 决策 `[A]` (Session 29 evening): **替换 inline 2 button base 为 production polish 4 button group** (NewGame / Continue / Settings / Quit) + 完整 vendor 7+2 lifecycle protected override + fade-in animation (0.3s OnRefresh) + BGM start hook (`IAudioService.PlayMusic("main_menu_bgm", 1.0f)`)。

### S6-07 Goal Flow (T0 → T6)

```
T0  game boot → ProcedureLaunch 链路完成 → ProcedureGetVersion → ... → MainMenu Procedure
T1  GameModule.UI.ShowUI<MainMenuPanel>() (vendor [Window] attribute Resources.Load "UI/MainMenuPanel")
T2  vendor 7 init hook 全 protected override 调用: ScriptGenerator → BindMemberProperty → RegisterEvent → OnCreate → OnRefresh
T3  OnCreate transform.Find 4 button reference (NewGameButton / ContinueButton / SettingsButton / QuitButton) + AddListener 4 onClick handler + cache named delegate (ADR-027 §5 防 raw double-remove)
T4  OnRefresh 同步: (a) Continue/Settings button.interactable = false (placeholder 灰态) (b) IAudioService.PlayMusic("main_menu_bgm", 1.0f) BGM start hook (c) CanvasGroup.alpha 0→1 fade-in tween DOTween 0.3s ease OutQuad
T5  user clicks NewGame → OnNewGameClicked → GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1) → SceneManager 11-step unload main menu scene + load chapter_1
T6  alt user clicks Quit → OnQuitClicked → Application.Quit (Procedure stage precedent — ProcedureInitResources / GetVersion / InitPackage 等多处已 production wired)
```

**S5-02 inline 2 button base 行为兼容性保证** — `MainMenuPanel.cs` 替换为 4 button group 后，S5-02 Spike `S5-02_EndToEndFlow.cs` 测试访问的 button 字段从 `StartChapter1Button`/`NextChapterButton` 改名为 `NewGameButton`/`QuitButton`；spike Action 同步 amend (P1 NewGameButton.onClick.Invoke 触发 OnRequestSceneChange(1)；P5 case rename + Quit button 测试 case 留 dev-story Phase 2 评估)。

---

## Acceptance Criteria

- [ ] **AC-1** `MainMenuPanel : UIWindow` 标 `[Window(UILayer.UI, fromResources: true, location: "UI/MainMenuPanel")]` (vendor [Window] attribute per `WindowAttribute.cs:21` 4 ctor overload；S5-02 已用 same pattern；prefab 路径 `Assets/Resources/UI/MainMenuPanel.prefab` 替换 S5-02 inline 2 button prefab)
- [ ] **AC-2** Vendor 7 lifecycle method 全 `protected override` visibility modifier 实施 — `ScriptGenerator()` / `BindMemberProperty()` / `RegisterEvent()` / `OnCreate()` / `OnRefresh()` / `OnUpdate()` / `OnDestroy()` (per ADR-011 V3.0.1 §G + SP-002 visibility modifier note — V3.0.1 dp7 NEW hotfix 2026-05-13 evening 强制 `protected override`，不可用 `public override` 触发 CS0507)
- [ ] **AC-3** `OnCreate()` 内 transform.Find 4 button child reference (`NewGameButton` / `ContinueButton` / `SettingsButton` / `QuitButton`) + `button.onClick.AddListener(_onXxxClicked)` 4 handler subscribe + named delegate cache (`_onNewGameClicked` / `_onContinueClicked` / `_onSettingsClicked` / `_onQuitClicked` per ADR-027 §5 防 raw double-remove)
- [ ] **AC-4** `OnRefresh()` 内同步执行 3 项: (a) `ContinueButton.interactable = false` + `SettingsButton.interactable = false` (placeholder 灰态 — SaveSystem epic / SettingsPanel polish 在 Sprint 7+ Production stage poly phase 起步)；(b) `IAudioService.PlayMusic("main_menu_bgm", 1.0f)` BGM start hook (待 Phase 2 verify 具体 service locator / inject path)；(c) `CanvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad)` fade-in tween (precedent: `InteractableObject.cs:336/383/475` 已 DOTween 使用)
- [ ] **AC-5** `OnDestroy()` 内 4 button RemoveListener + null-out (per ADR-027 §5 framework knowledge fact null-out + null-check guard pattern；S5-02 `MainMenuPanel.cs:95-112` 已实证 2 button pattern；4 button 扩展)
- [ ] **AC-6** NewGame button onClick handler: `GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1)` (chapter 1 start；与 S5-02 `OnStartChapter1ButtonClicked()` line 114-125 行为等价)
- [ ] **AC-7** Quit button onClick handler: `Application.Quit()` direct call (precedent: `Procedure/ProcedureInitResources.cs:126/133/148` + 5 other Procedure file production wired)；Editor mode + Standalone build 均生效 — Editor 内 PlayMode 退出由 Unity 自身 handle (`UnityEditor.EditorApplication.isPlaying = false` 留 Editor-only #if `UNITY_EDITOR` guard wrapper 待 Phase 2 评估)
- [ ] **AC-8** Continue / Settings button onClick handler 标 `[NotImplemented_S6_07_NarrowScope]` placeholder — Sprint 7+ Production stage polish phase 起步实施真路径；本 sprint button.interactable = false 灰态阻止点击，handler 0-line body (无 `throw NotImplementedException`，避免误触崩 game)
- [ ] **AC-9** 0 unexpected console error/warning (vendor `Handle_Completed` 自动 set Canvas overrideSorting/sortingOrder 不抛 warning；vendor [Window] attribute Resources.Load 路径 hit；4 button transform.Find hit 全部成功 — 否则 Log.Error 但不 throw)
- [ ] **AC-10** R3 PlayMode probe 5 case 全 PASS first-run (29+ asserts 估)；evidence doc `production/qa/playmode-main-menu-polish-2026-05-14.md` (next session 起 dev-story Phase 4 写入)

---

## Engine Notes

### Vendor API Realities (per S5-08 + S6-05 + S6-07 Phase 0 R2 verify 实证)

- **UIBase.cs:144/151/158/165/172/184/197**: 7 lifecycle method 全 `protected virtual void XxxName()` 签名 — 业务侧 override **必须用 `protected override`** (使用 `public override` 触发 `CS0507: cannot change access modifiers when overriding`；S6-05 commit 45ae96b ADR-011 §G Key Interfaces code block 5 处 `public override` 是 spec amend 自身引入 wording drift，已 V3.0.1 dp7 NEW hotfix 修正 2026-05-13 evening)
- **UIWindow.cs:504/509**: extra 2 hook `Hide()` / `Close()` 同 `protected virtual void XxxName()`；本 story narrow scope **不 override** `Hide()` / `Close()` (vendor 默认行为足够 — `Close()` 走 UIModule._uiStack remove + OnDestroy 销毁 instance per S5-08 R3 P3 V3 Type-8 dp1 实证 'destroy-and-recreate' 模式)
- **`[Window]` attribute** `WindowAttribute.cs:21` 4 ctor overload — S5-02 已用 `[Window(UILayer.UI, fromResources: true, location: "UI/MainMenuPanel")]` ✅ 沿用
- **`GameModule.UI.ShowUI<T>()` / `CloseUI<T>()`**: vendor API (per S6-05 ADR-011 §G + UIModule.cs:250-460)；S5-02 已 production wire (`DevTestState.OnEnter ShowMainMenuPanelAsync`)；本 story narrow scope 不改 entry — 沿用 ProcedureLaunch → MainMenu Procedure → `GameModule.UI.ShowUI<MainMenuPanel>()` 路径 (待 Phase 2 verify 当前 procedure stage main menu show entry — S5-02 DevTestState 路径 vs production procedure stage 路径)
- **DOTween**: `DG.Tweening` available — `InteractableObject.cs:336/383/475` 已 production wired (`Ease.OutBack` / `Ease.OutQuad`)；本 story `CanvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad)` fade-in tween
- **`IAudioService.PlayMusic(string clipId, float crossfadeDuration = 1.0f)`** `IAudioService.cs:39`: production API；BGM start hook 路径；Phase 2 verify 具体 service locator / DI 取 IAudioService instance 模式 (S5-02 audio ducking 已 production wired 但 BGM start 路径仍 TBD — 可能走 GameModule.Audio direct (per AudioManager.cs:218/263/296 实测) 或 DI inject)
- **`Application.Quit`**: production pattern multiple callsites (`Procedure/ProcedureInitResources.cs:126/133/148` + `ProcedureGetVersion.cs:78` + `ProcedureInitPackage.cs:83/109` + `ProcedureDownloadFile.cs:69` + `ProcedureCreateDownloader.cs:67`)；direct call OK；Editor 模式由 Unity 自身 handle PlayMode stop 行为

### Visibility Modifier 强制 (V3.0.1 dp7 NEW)

⚠️ **关键 framework knowledge fact** — UIWindow 业务派生类 lifecycle override 必须 `protected override` 不可 `public override`：

```csharp
// ✅ 正确 (vendor `UIBase.cs:144` `protected virtual` 签名兼容)
protected override void OnCreate() { base.OnCreate(); /* ... */ }

// ❌ 错误 (触发 CS0507: cannot change access modifiers when overriding)
public override void OnCreate() { base.OnCreate(); /* ... */ }
```

per ADR-029 V3.0.1 §V3-1.b dp7 NEW (2026-05-13 evening hotfix — S6-05 commit 45ae96b 自身 wording drift 引入 + S6-07 Phase 0 R2 verify 发现 + dp7 NEW reinforce S6-06 V2.0 §V2-1.b R2 增量子条款)。

---

## Control Manifest

### Required Patterns

- 4 button transform.Find by **exact prefab child node name** (`NewGameButton` / `ContinueButton` / `SettingsButton` / `QuitButton`) — prefab 节点命名固定 hard contract；不一致触发 Log.Error 但不 throw
- 4 button onClick AddListener 用 **named delegate cache field** (`_onNewGameClicked` / `_onContinueClicked` / `_onSettingsClicked` / `_onQuitClicked`) — per ADR-027 §5 framework knowledge fact 防 raw double-remove
- 4 button onClick RemoveListener + null-out + null-check guard pattern (与 cache delegate 配对) — per ADR-027 §5
- `protected override` visibility modifier 全 7 lifecycle method (per V3.0.1 dp7 NEW hotfix)
- `[Window(UILayer.UI, fromResources: true, location: "UI/MainMenuPanel")]` (沿 S5-02 inline 2 button base pattern + S5-08 LogUI.cs precedent)
- `IAudioService.PlayMusic("main_menu_bgm", 1.0f)` BGM start hook (Phase 2 verify 具体 service locator path)
- `CanvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad)` fade-in tween (precedent InteractableObject.cs)
- `Application.Quit()` direct call (Editor mode + Standalone build 均生效；可加 `#if UNITY_EDITOR` guard wrapper 待 Phase 2 评估)
- `GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1)` NewGame onClick dispatch (chapter 1 start；与 S5-02 ISceneEvent 调用 pattern 一致)

### Forbidden Patterns

- ❌ `public override` lifecycle method visibility modifier (触发 CS0507 — V3.0.1 dp7 NEW)
- ❌ 直接 `button.onClick.AddListener(() => OnXxxClicked())` lambda — lambda 不可作为 RemoveListener target，禁止 (ADR-027 §5 anti-pattern)
- ❌ `OnRefresh()` 内执行 heavy init work (例如 service locator construct / config table parsing)；OnRefresh 是 every-show frame，不是 OnCreate — heavy init 留 OnCreate
- ❌ Continue / Settings button onClick handler 内 `throw new NotImplementedException()` — 即使 button.interactable = false 灰态阻止点击，handler 0-line body 是 safer pattern (避免 vendor 误触 / Unity Editor Inspector 误调用 onClick)
- ❌ `Hide()` / `Close()` 业务侧 override (本 story narrow scope 不 override — vendor 默认 destroy-and-recreate 行为足够 per S5-08 R3 P3 V3 Type-8 dp1)
- ❌ `OnRequestSceneChange(0)` chapter 0 入参 (V3.0 §V3-1.c dp6 spec drift — vendor API 只支持 `targetChapterId in [1, 5]`；NewGame 派 `OnRequestSceneChange(1)`)

---

## Out of Scope

- **SaveSystem 接入** (Continue button 真路径 — `ISaveService.HasValidSave()` / `DeleteSave()` 等 API 等 SaveSystem epic Sprint 7+) — 本 story Continue button 标 placeholder 灰态
- **SettingsPanel 完整面板** (Settings button 真路径 — `GameModule.UI.ShowUI<SettingsPanel>()` 等 S5-09 backlog descope 终结后 production stage polish phase 起步) — 本 story Settings button 标 placeholder 灰态
- **`main_menu_bgm` AudioConfig + AudioClip asset** (BGM 实际 playback — Sprint 7+ follow-on backlog story `ui-system-006b: main_menu_bgm asset + Luban entry` 1 SP；本 story BGM hook spec 完整保留 + production code 调 `AudioManager.Instance.PlayMusic("main_menu_bgm", 1.0f)` 但走 PlayMusic fail-safe Log.Warning+no-op；Sprint 7+ asset 添加后 BGM 自动响 0 code change in MainMenuPanel.cs) per Session 30 Phase 2.0 R2.8 [D] mixed strategy closure
- **ChapterSelect** (留 Sprint 7+ chapter-state epic depends + S5-08 narrow scope decision retained)
- **Localization** (TR-ui-021 / 本地化键替换 — Production stage P2 阶段实施 per EPIC.md §52 SP-009 finding)
- **Android back button** (TR-ui-022 / 待 Production stage 后入)
- **Gaussian blur fallback** (TR-ui-013 / Production stage polish phase)
- **InputBlocker** (本 story UILayer.UI 层不需要；popup 层独立 story-008 Sprint 6 S6-08)
- **TimeScale = 0** (本 story 不是 PauseMenu；TR-ui-009 留 ui-system-003 Sprint 7+)

---

## Implementation Notes

### File Targets (S6-07 Phase 2 production code 实施 — defer 下一 session 起)

1. **Update** `Assets/GameScripts/HotFix/GameLogic/UI/MainMenuPanel.cs` (S5-02 142 行 baseline → 4 button + fade-in + BGM hook ~210-260 行 estimate)
   - 4 button field (NewGameButton / ContinueButton / SettingsButton / QuitButton) — internal Button { get; private set; } 沿 S5-02 internal access modifier 模式
   - 4 named delegate cache field (`_onNewGameClicked` / `_onContinueClicked` / `_onSettingsClicked` / `_onQuitClicked`)
   - `OnCreate()` 4 button transform.Find + AddListener
   - `OnRefresh()` Continue/Settings interactable=false + IAudioService.PlayMusic + CanvasGroup.DOFade
   - `OnUpdate()` 留 vendor 默认空实现 (本 story 不需要 per-frame logic)
   - `OnDestroy()` 4 button RemoveListener + null-out + null-check guard
   - 4 button onClick handler (NewGame: ISceneEvent.OnRequestSceneChange(1) / Continue: placeholder 0-line body / Settings: placeholder 0-line body / Quit: Application.Quit())
   - 1 private CanvasGroup field for fade-in target

2. **Update** `Assets/GameScripts/HotFix/GameLogic/UI/Editor/MainMenuPanelGenerator.cs` (S5-02 inline 2 button prefab generator → 4 button generator + CanvasGroup root component；MenuItem `Tools/S6-07/Generate Main Menu Panel Prefab`)
   - Root: Canvas + GraphicRaycaster + CanvasGroup (alpha=0 起始 — for fade-in)
   - 4 child Button (NewGameButton + ContinueButton + SettingsButton + QuitButton) with Button + TextMeshPro/Text label
   - Layout: VerticalLayoutGroup or manual position (Phase 2 estimate ~30-50 行扩展)

3. **Regenerate** `Assets/Resources/UI/MainMenuPanel.prefab` (run `MainMenuPanelGenerator` MenuItem 重生成；prefab 覆盖 S5-02 inline 2 button base)

4. **Update** `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-02_EndToEndFlow.cs` (P1 case button rename)
   - P1 `StartChapter1Button.onClick.Invoke()` → `NewGameButton.onClick.Invoke()`
   - P5 case `NextChapterButton.onClick.Invoke()` rename or rethink (S5-02 P5 chapter 2 switch case — S6-07 4 button group 无 NextChapter button；P5 case 可 delete 或改 Quit button 测试 case — Phase 2 评估)
   - 评估 P5 case 后续处理 — option (i) delete P5 (Spike scope shrink 4 case) / (ii) replace P5 with Quit button test case / (iii) keep P5 by reverting S5-02 inline 2 button as separate dev-only test fixture (out of S6-07 production code)

5. **New** `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-07_MainMenuPolish.cs` (~400-500 行 estimate — 1 file + 3 inner class spike/runtime/tester pattern per S5-08/S5-02 precedent)
   - 5 R3 case (详 §R3 PlayMode Probe Cases below)
   - JSON evidence dump `~/Library/Application Support/DefaultCompany/Unity/S6-07_Result.json`
   - GameApp.cs RegisterDevSpikes S502Spike → S607Spike 切换 (Phase 2 evaluate — 是否替换 S5-02 spike 还是并存)

### Sprint 6 S6-07 Phase 1 — Story File 创建 + R1+R2+R3 Readiness Gate (本 session 完成)

Phase 1 完成内容 (Session 29 evening 2026-05-13)：

- ✅ story-006-main-menu.md 完整 rewrite (本文件 ~400+ 行) per V3.0.1 vendor reality compliant + S6-07 narrow scope [A] 4 button group
- ✅ R1 readiness gate — 0 forbidden listener pattern (本 story 不订阅 GameEvent listener；Button.onClick 是 Unity UI subscribe-once 模式 / 不走 GameEvent listener path — ADR-027 §5 framework knowledge fact 不适用)
- ✅ R2 readiness gate — DEFICIENCY-FLAGGED PASS:
  - R2.1 ✅ `protected override` visibility modifier (V3.0.1 dp7 NEW hotfix 2026-05-13 evening — 已 R2 verify vendor UIBase.cs:144-197 + UIWindow.cs:504/509)
  - R2.2 ✅ `[Window(UILayer.UI, fromResources: true, location: "UI/MainMenuPanel")]` (S5-02 inline 2 button base 已 production wired ✅)
  - R2.3 ✅ `GameModule.UI.ShowUI<MainMenuPanel>()` (vendor API 已 S6-05 ADR-011 §G amend ✅；S5-02 DevTestState production wired ✅)
  - R2.4 ✅ `ISceneEvent.OnRequestSceneChange(int)` (S5-02 production wired + V3.0 §V3-1.c dp6 spec drift 已 closure；NewGame 派 1 chapter id valid)
  - R2.5 ✅ DOTween `CanvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad)` (precedent InteractableObject.cs:336/383/475 production wired)
  - R2.6 ✅ **FULLY RESOLVED** (2026-05-14 morning Session 30 Phase 2.0 verify) — `AudioManager : Singleton<AudioManager>, IAudioService` (AudioManager.cs:30) → 取 instance path = `AudioManager.Instance.PlayMusic("main_menu_bgm", 1.0f)` (per IAudioService.cs:14 注释 "API 路径同模块内 / 流程明确" 推荐)；`GameApp.cs:40 AudioManager.Instance.Initialize()` 在 `:55 StartGameLogic()` 之前调用，main menu show 时 `_isInitialized=true` 保证 (AC-3 facade activation gate 不 trigger fail-loud)；`PlayMusic` 内置 fail-safe (clipId null/empty + config not found → Log.Warning + return，不抛 exception 不影响 main menu show 路径)
  - R2.7 ✅ `Application.Quit()` (Procedure stage 多处 production wired)
  - R2.8 ✅ **DEFICIENCY CLOSURE per [D] mixed strategy** (2026-05-14 morning Session 30 Phase 2.0 verify) — `main_menu_bgm` clipId grep 0-hit in production code；`AudioConfigFromLuban.InitWithDefaults()` (line 154-158) 仅 1 Music entry `chapter1_ambient` (id=100)；`InitFromLuban` 是 stub TODO[S5-XX+] fallback 走 InitWithDefaults。**[D] 决策 (Phase 2.0 user)**: 遵 epic 边界 — Phase 2 production code **不改 AudioConfigFromLuban (跨 Audio epic 边界 ui-system epic 不 polluted)**；BGM hook 保留 production code OnRefresh `AudioManager.Instance.PlayMusic("main_menu_bgm", 1.0f)` 调用 (走 PlayMusic fail-safe Log.Warning+no-op — main menu show 不受影响)；新建 Sprint 7+ follow-on backlog placeholder story **`ui-system-006b: main_menu_bgm asset + Luban entry add`** 1 SP (Audio epic 范围 — Sprint 7+ Production polish phase 起步时实施 真 main_menu_bgm AudioClip asset add + AudioConfigFromLuban entry add 或 Luban TbAudio.Music 真表 schema 生成同期完成)；R3 P3 BGM hook assert 走 mock spy invocation count + clipId match verify 不真 playback (详 §R3 PlayMode Probe Cases P3 update)；governance impact: epic 边界 cleanup — UI epic 仅触 Audio facade `AudioManager.Instance.PlayMusic` API 不触 Audio config table；spec 不退 (BGM hook 完整保留)；Sprint 7+ 真 asset 添加后 BGM 自动响 (0 code change in MainMenuPanel.cs)
  - R2.9 ✅ `Application.Quit()` Editor mode handling — Procedure 多处 wired，Editor 下 Unity 自身 handle PlayMode stop；可 `#if UNITY_EDITOR` guard wrapper 待 Phase 2 评估
- ✅ R3 readiness gate — propagate spec write into 5 PlayMode probe case (详 §R3 PlayMode Probe Cases below)

✅ R2.6 + R2.8 deficiency flag **CLOSURE COMPLETE** (Session 30 Phase 2.0 verify — 2026-05-14 morning) per ADR-029 V2.0 §V2-1 R2 DEFICIENCY-FLAGGED PASS path → CLOSURE。

**Status**: Phase 0 R2 verify partial ✅ + Phase 1 readiness gate ✅ + **Phase 2.0 R2 deficiency flag CLOSURE ✅** (Session 30 2026-05-14 morning) → ready for Phase 2 production code 实施。

---

## R3 PlayMode Probe Cases (5 case)

### P1 — Vendor 7+2 Lifecycle Visibility Modifier Compliance Verify (~5 asserts)

- **Setup**: spike `Awake()` 同步 subscribe early listener (S5-1c precedent — 防 sync-fire race)；`Start()` async `GameModule.UI.ShowUI<MainMenuPanel>()`
- **Action**: 等 frame=N MainMenuPanel.LastInstance != null (vendor `Handle_Completed` 完整 7 init hook 同步 调用)
- **Assert**: (1) LastInstance != null (2) LastInstance 是 MainMenuPanel type (3) vendor lifecycle 7 init hook 调用顺序符合 ScriptGenerator → BindMemberProperty → RegisterEvent → OnCreate → OnRefresh (capture 通过 mock override 或 reflection — 沿 S5-08 P3 模式) (4) 全 7 lifecycle method 是 `protected` visibility modifier (reflection `MethodInfo.IsFamily == true` per `BindingFlags.NonPublic | BindingFlags.Instance`；本 assert 是 V3.0.1 dp7 NEW reinforcement test，避免未来 spec wording drift 再引入 `public override` 不被发现) (5) 0 unexpected console error/warning

### P2 — 4 Button transform.Find + onClick AddListener Subscribe Verify (~6 asserts)

- **Setup**: P1 後 MainMenuPanel showing
- **Action**: reflection 拿 4 button field reference (NewGameButton / ContinueButton / SettingsButton / QuitButton)
- **Assert**: (1)~(4) 4 button reference 全 != null (transform.Find hit 全部成功) (5) ContinueButton.interactable == false (6) SettingsButton.interactable == false (placeholder 灰态)

### P3 — Fade-In Animation + BGM Hook Mock Spy Verify (~6 asserts) (Phase 2.0 R2.8 [D] closure 同步 amend)

- **Setup**: P2 後 MainMenuPanel showing；spike 起步时 inject mock `IAudioService` (sealed test class implementing IAudioService 6 methods + spy fields `LastPlayMusicClipId` + `LastPlayMusicCrossfade` + `PlayMusicInvocationCount`)；通过 `AudioManager.Instance.SetConfigProviderForTest(...)` 模式无法 mock (那是 IAudioConfigProvider 不是 IAudioService)；改 spike 起步时直接 wrap `AudioManager.Instance` 通过 reflection 替换 `_singleton` field 或走 spike scope test fixture (Phase 2 实施时具体方案 evaluate — option (i) reflection swap singleton instance / option (ii) AudioManager 加 `SetTestInstance` static helper / option (iii) spike 跳过 mock 走 `Application Support/.../S6-07_Result.json` capture `AudioManager.Instance` 调用日志 via Log capture sniffer — option (iii) 最 minimal touchpoint 推荐)
- **Action**: 等 frame=N+30 (0.5s budget — fade-in 0.3s 完整)；reflection 拿 MainMenuPanel CanvasGroup field reference 检查 alpha；reflection 拿 mock spy fields 或 Log sniffer capture
- **Assert**: (1) CanvasGroup.alpha == 1.0f (fade-in 已 complete via DOTween OutQuad 0.3s budget) (2) `AudioManager.Instance.PlayMusic` 已被调用过 (mock spy invocation count >= 1 或 Log sniffer capture `[AudioManager] PlayMusic: Music 'main_menu_bgm' not found in config` warning 行) (3) PlayMusic 入参 clipId == `"main_menu_bgm"` (mock spy LastPlayMusicClipId 或 Log line parse) (4) PlayMusic 入参 crossfadeDuration == 1.0f (mock spy LastPlayMusicCrossfade 或 default 隐式) (5) AudioManager `_isInitialized=true` (reflection field check — confirm AC-3 facade activation gate 不 trigger) (6) 0 unexpected console error (Log.Warning `Music 'main_menu_bgm' not found in config` per R2.8 [D] closure 是 expected fail-safe 行为，不算 unexpected — assert exclude pattern)
- **R2.8 [D] closure governance note**: BGM 实际不响 (main_menu_bgm AudioConfig entry 缺失 — Sprint 7+ ui-system-006b backlog 解决)；R3 P3 verify dispatch 行为 + fail-safe 行为，不 verify actual playback；spec 完整 (Sprint 7+ asset 添加后 BGM 自动响 0 code change in MainMenuPanel.cs)

### P4 — NewGameButton onClick → ISceneEvent.OnRequestSceneChange(1) Dispatch Verify (~6 asserts)

- **Setup**: P3 後 fade-in complete + BGM playing
- **Action**: `NewGameButton.onClick.Invoke()` 模拟 user 点击
- **Assert**: (1) ISceneEvent.OnRequestSceneChange capture handler 收到 (sender check + handler param check via mock subscribe) (2) targetChapterId == 1 (3) chapter 1 scene 进入 transition state (capture via SceneManager state machine field) (4)~(6) 全 S5-02 P1 case assert 路径继承 (chapter 1 unload + chapter 1 load + state=Idle 等 — Phase 2 evaluate 是否完整 inherit S5-02 P1 asserts 还是 narrow scope 仅 NewGame onClick dispatch verify)

### P5 — QuitButton onClick → Application.Quit Dispatch Verify (~5 asserts)

- **Setup**: P3 後 fade-in complete + BGM playing
- **Action**: reflection mock `Application.Quit` (replace via `UnityEditor.EditorApplication.isPlaying = false` Editor mode wrapper 或 `Application.quitting` event subscribe spy)；`QuitButton.onClick.Invoke()` 模拟 user 点击
- **Assert**: (1) Application.quitting event fired (mock spy capture) (2) handler 调用 timing 在 onClick.Invoke 后 same frame 或 next frame (3) Editor mode 下 PlayMode 不会真退 (sandboxed — 通过 mock spy 验证 dispatch 行为) (4) 0 unexpected console error/warning (5) MainMenuPanel.LastInstance still 引用 valid (Quit 不触发 CloseUI<T>())

**Phase 2 evidence doc**: `production/qa/playmode-main-menu-polish-2026-05-14.md` (next session 起 dev-story Phase 4 写入)

---

## R2 Assumptions Validated (Phase 1 readiness gate evidence)

| ID | Assumption | Status | Evidence |
|----|-----------|--------|---------|
| R2.1 | UIWindow lifecycle 7+2 hook 全 `protected virtual` 签名；override 必 `protected override` | ✅ | UIBase.cs:144/151/158/165/172/184/197 + UIWindow.cs:504/509 vendor source 全 9 hook 实证 (S6-07 Phase 0 R2 verify 2026-05-13 evening — V3.0.1 dp7 NEW hotfix closure) |
| R2.2 | `[Window(UILayer.UI, fromResources: true, location: "UI/MainMenuPanel")]` vendor [Window] attribute 4 ctor overload | ✅ | WindowAttribute.cs:21 vendor source 实证 + S5-02 production wired ✅ |
| R2.3 | `GameModule.UI.ShowUI<T>()` vendor API | ✅ | UIModule.cs:250-460 vendor source 实证 (S6-05 ADR-011 §G amend) + S5-02 DevTestState production wired ✅ |
| R2.4 | `ISceneEvent.OnRequestSceneChange(int targetChapterId)` API | ✅ | ISceneEvent.cs:24 vendor source + S5-02 production wired (V3.0 §V3-1.c dp6 spec drift closure — chapter id 1-5 valid only) |
| R2.5 | DOTween `CanvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad)` | ✅ | DG.Tweening package + InteractableObject.cs:336/383/475 production wired precedent |
| R2.6 | `IAudioService.PlayMusic("main_menu_bgm", 1.0f)` service locator / DI 路径 | ✅ FULLY RESOLVED | `AudioManager : Singleton<AudioManager>, IAudioService` (AudioManager.cs:30) → `AudioManager.Instance.PlayMusic(...)` direct call；`GameApp.cs:40 AudioManager.Instance.Initialize()` 在 `:55 StartGameLogic()` 之前调用 (main menu show 时 `_isInitialized=true` AC-3 gate 不 trigger)；`PlayMusic` 自身 fail-safe (clipId null/empty + config not found → Log.Warning + return 不抛 exception) — Session 30 Phase 2.0 verify 2026-05-14 morning |
| R2.7 | `Application.Quit()` direct call | ✅ | Procedure/ProcedureInitResources.cs:126/133/148 + 5 other Procedure file production wired multiple precedent |
| R2.8 | `main_menu_bgm` AudioConfig clipId 在 TbAudioEvent / IAudioConfigProvider 中已 defined | ✅ DEFICIENCY CLOSURE per [D] mixed strategy | `main_menu_bgm` clipId grep **0-hit**；`AudioConfigFromLuban.InitWithDefaults()` (line 154-158) 仅 1 Music entry `chapter1_ambient` (id=100)；`InitFromLuban` TODO[S5-XX+] stub fallback 走 InitWithDefaults。**[D] decision**: Phase 2 production code 不改 AudioConfigFromLuban (epic 边界遵守 — UI epic 不 polluted Audio config table)；BGM hook 保留 OnRefresh `AudioManager.Instance.PlayMusic("main_menu_bgm", 1.0f)` 调用 走 PlayMusic fail-safe Log.Warning+no-op；新建 Sprint 7+ follow-on backlog story `ui-system-006b: main_menu_bgm asset + Luban entry` 1 SP；R3 P3 走 mock spy invocation assert (详 §R3 P3 update)；Session 30 Phase 2.0 verify 2026-05-14 morning |
| R2.9 | `Application.Quit()` Editor mode handling | ✅ | Editor mode 下 Unity 自身 handle (Procedure 多处 production wired)；可 `#if UNITY_EDITOR` guard 加 EditorApplication.isPlaying = false 显式 wrapper Phase 2 评估 |

**R2 Verdict**: ✅ FULLY PASS (Session 30 Phase 2.0 — R2.6 + R2.8 deficiency flag CLOSURE COMPLETE per ADR-029 V2.0 §V2-1 R2 DEFICIENCY-FLAGGED PASS path → CLOSURE)

---

## V3.0.1 Watch List Hooks

### Type-5 dp7 NEW (本 story Phase 0 R2 verify 触发 + hotfix closure 2026-05-13 evening)

- **closure status**: ✅ hotfix amend done (commit 898ae7a — ADR-011 §G 5 处 `public override` → `protected override` + SP-002 visibility modifier note + ADR-029 V3.0 → V3.0.1 sub-versioning + §V3-1.b dp7 NEW row append + §V3-5 V4 trigger row 1 Sprint 6 status amend + sprint-status.yaml watch_list dp7 NEW + active.md)
- **本 story enforce**: AC-2 + Engine Notes Visibility Modifier 强制 + Control Manifest Required `protected override` / Forbidden `public override` + R3 P1 case (5) reflection MethodInfo.IsFamily assert (dp7 NEW reinforcement — 避免未来 spec wording drift 再引入 `public override` 不被发现)
- **governance insight**: dp7 NEW reinforce S6-06 V2.0 §V2-1.b R2 增量子条款 — ADR amend 工作本身必须遵守 R2 协议自身；spec wording amend ≠ free pass on vendor source verify

### Type-5 dp6 (closure — S5-02 Session 27 #3 ISceneEvent chapter 0 spec drift)

- **本 story enforce**: AC-6 NewGame onClick 派 `OnRequestSceneChange(1)` (chapter id valid 1-5)；Control Manifest Forbidden `OnRequestSceneChange(0)`

### Type-9 dp1 (closure — S6-06 ADR-029 V2.0 §V2-1.b R2 增量子条款 absorbed)

- **本 story enforce**: R2 readiness gate 9 项 assumption 走 V2.0 §V2-1.b R2 增量子条款 "Interface Method Set Fan-out Check" pattern — 对 `IAudioService` 接口 method set fan-out check (PlayMusic + PlaySFX + StopAllSFX + StopMusic 等)；目前本 story 仅依赖 PlayMusic 1 method (BGM start hook)，无 cross-method 调用 chain 隐患

### Type-8 dp1 (留观察 — S5-08 UIWindow second show 'destroy-and-recreate')

- **本 story 不触发**: 本 story narrow scope 不 override `Hide()` / `Close()`；MainMenuPanel 在 main menu procedure stage 仅 show once 不走 second show 路径；future ChapterSelect / PauseMenu 二次返回 main menu 路径走 vendor 'destroy-and-recreate' 模式 (per S5-08 V3 Type-8 dp1 实证) — 本 story default 不依赖 instance reuse

---

## Dependencies

### Story Dependencies (✅ all done)

- **S5-08 ✅** (UIModule narrow scope — UILayer.UI enum + ShowUI/CloseUI API + Resources.Load + Vendor [Window] attribute + 7+2 lifecycle vendor reality)
- **S5-02 ✅** (inline 2 button base — production wiring DevTestState → ShowMainMenuPanelAsync + GameApp Spike 切换 + ISceneEvent.OnRequestSceneChange production wired)
- **S6-05 ✅** (ADR-011 §G systematic wording amend — vendor 7+2 lifecycle 完整 documented + UILayer enum vendor 命名 + ShowUI/CloseUI API documented)
- **S6-06 ✅** (ADR-029 V2.0 §V2-1.b R2 增量子条款 — Phase 1 R2 readiness gate 走 R2 协议增量版)
- **S6-07 Phase 0 hotfix ✅** (V3.0.1 dp7 NEW visibility modifier drift closure — AC-2 + Engine Notes 实证 baseline)

### Out-of-Scope Story Dependencies (Sprint 7+ Production stage)

- **SaveSystem epic** (Continue button 真路径 ISaveService.HasValidSave/DeleteSave — Sprint 7+)
- **ui-system-007 SettingsPanel** (Settings button 真路径 GameModule.UI.ShowUI<SettingsPanel> — Sprint 7+ production stage polish phase 起步)
- **ui-system-005 ChapterSelect** (blocked on chapter-state epic — Sprint 7+)

### Framework Dependencies (✅ all verified Phase 1)

- TEngine UIModule + UIBase + UIWindow + WindowAttribute (vendor source ✅)
- ISceneEvent.OnRequestSceneChange (✅)
- IAudioService.PlayMusic (✅ API + ⚠️ TBD service locator path Phase 2 verify)
- DG.Tweening DOTween (✅ DOFade + Ease.OutQuad InteractableObject.cs precedent)
- UnityEngine.UI.Button + UnityEngine.UI.CanvasGroup (Unity built-in ✅)
- Application.Quit (✅ Procedure stage multiple precedent)

---

## Test Evidence Path

- **R3 PlayMode evidence**: `production/qa/playmode-main-menu-polish-2026-05-14.md` (Phase 4 dev-story 写入 — next session 起)
- **R3 JSON evidence dump**: `~/Library/Application Support/DefaultCompany/Unity/S6-07_Result.json` (spike 输出 — next session Phase 3 实测)

---

## History

- **2026-05-14 morning (Sprint 6 Session 30 Phase 2.0 R2 deficiency flag closure)**:
  - R2.6 ✅ FULLY RESOLVED — `AudioManager : Singleton<AudioManager>, IAudioService` (AudioManager.cs:30) → `AudioManager.Instance.PlayMusic(...)` direct call path；`GameApp.cs:40 Initialize()` 在 `:55 StartGameLogic()` 之前调用 (main menu show 时 `_isInitialized=true` AC-3 gate 不 trigger)；`PlayMusic` 内置 fail-safe (clipId null/empty + config not found → Log.Warning + return)
  - R2.8 ✅ DEFICIENCY CLOSURE per [D] mixed strategy — `main_menu_bgm` clipId 缺失但 PlayMusic fail-safe；遵 epic 边界 Phase 2 不改 AudioConfigFromLuban 跨 Audio epic；新建 Sprint 7+ follow-on backlog story `ui-system-006b: main_menu_bgm asset + Luban entry` 1 SP；R3 P3 走 mock spy invocation assert 不真 playback verify
  - R3 P3 case amend — fade-in + BGM hook mock spy verify (~6 asserts ↑ from 5)：alpha == 1.0f + PlayMusic invocation count >= 1 + clipId == "main_menu_bgm" + crossfade == 1.0f + `_isInitialized=true` + Log.Warning `Music 'main_menu_bgm' not found` 是 expected fail-safe (assert exclude)
  - Out of Scope: +`main_menu_bgm` AudioConfig + AudioClip asset (Sprint 7+ ui-system-006b backlog) per [D] closure
  - Status header: Phase 0 R2 verify partial ✅ + Phase 1 readiness gate ✅ → + Phase 2.0 R2 deficiency flag CLOSURE ✅ Ready for Phase 2 production code 实施
  - **R2 Verdict 升级**: DEFICIENCY-FLAGGED PASS → ✅ FULLY PASS per ADR-029 V2.0 §V2-1 R2 DEFICIENCY-FLAGGED PASS path → CLOSURE
  - **Phase 2.0 投入** ~25 min (R2.6 vendor source verify 5 + R2.8 grep + decision matrix 10 + story amend + Out of Scope + History + R3 P3 + R2 表 + Status header 10)
- **2026-05-13 evening (Sprint 6 Session 29 Phase 0 R2 verify + Phase 1 story 创建 + readiness gate)**:
  - Phase 0 R2 verify vendor source UIBase.cs:144-197 + UIWindow.cs:504/509 全 7+2 lifecycle 实证 `protected virtual`；surfaced V3.0 §V3-1.b Type-5 dp7 NEW visibility modifier drift (S6-05 commit 45ae96b ADR-011 §G Key Interfaces code block 5 处 `public override` 误用 — spec amend 自身引入)；commit 898ae7a hotfix amend (ADR-011 + SP-002 + ADR-029 V3.0 → V3.0.1 + sprint-status.yaml + active.md) 完成
  - Phase 1 story-006-main-menu.md 完整 rewrite — 从 Sprint 0 framework time placeholder (placeholder ShowWindow / OnCreate-OnRefresh 2 hook / UILayer.HUD 错误命名 / Evt_RequestSceneChange / Evt_PlayMusicRequest 等 11+ wording drift) → V3.0.1 vendor reality compliant + S6-07 narrow scope [A] 4 button group (NewGame / Continue placeholder / Settings placeholder / Quit) + vendor 7+2 lifecycle `protected override` + fade-in 0.3s + BGM hook IAudioService.PlayMusic + R3 PlayMode probe 5 case + R2 Assumptions Validated 9 items table (R2.1-R2.5 + R2.7 + R2.9 ✅；R2.6 + R2.8 ⚠️ TBD deficiency flag Phase 2 verify) + V3.0.1 Watch List Hooks Type-5 dp7 NEW + dp6 + Type-9 + Type-8 ✅
  - Phase 1 R1+R2+R3 readiness gate verdict ✅ READY (R2 DEFICIENCY-FLAGGED PASS R2.6 + R2.8 ⚠️ TBD — Phase 2 强制实证；不阻 Phase 2 transition per ADR-029 V2.0 §V2-1 R2 DEFICIENCY-FLAGGED PASS 路径)
  - S6-07 Phase 2 production code (MainMenuPanel.cs S5-02 baseline → 4 button + fade-in + BGM polish + MainMenuPanelGenerator amend + prefab regenerate + S6-07_MainMenuPolish.cs spike + GameApp RegisterDevSpikes 切换 + S5-02 spike P5 case 评估) defer 下一 session 起 per CLAUDE.md session ≤5 hr hard rule
  - **Phase 1 投入** ~50 min (Phase 0 R2 vendor source 已 done in dp7 hotfix；本 step 仅 story file rewrite + R1+R2+R3 grep gate + sprint-status.yaml + active.md sync + commit)
- **2026-XX-XX (Sprint 0 framework time placeholder — superseded 2026-05-13 evening per V3.0.1 vendor reality compliant rewrite)**: 早期 placeholder version — 11+ wording drift 包括 ShowWindow API / UILayer.HUD / 2 hook lifecycle (OnCreate/OnRefresh) / Evt_RequestSceneChange / Evt_PlayMusicRequest / DOTween 0.3s budget / ISaveService.HasValidSave (Continue 灰态 SaveSystem 依赖前置假设) 等；本 story Phase 1 rewrite 全部 supersede per V3.0.1 vendor reality compliant + S6-07 narrow scope [A]
