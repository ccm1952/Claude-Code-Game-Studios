// 该文件由Cursor 自动生成

# Epic: UI System

> **Layer**: Feature
> **GDD**: `design/gdd/ui-system.md`
> **Architecture Module**: UI screens (GameHUD, PauseMenu, PuzzleComplete, ChapterSelect, MainMenu, SettingsPanel, HintButton, TutorialOverlay, Credits)
> **Governing ADRs**: ADR-011 (UIWindow Management)
> **Engine Risk**: MEDIUM
> **Status**: Ready
> **Stories**: 11 stories created (10 original + ui-system-006b Sprint 7+ backlog placeholder 衍生自 S6-07 Phase 2.0 R2.8 [D] mixed strategy closure 2026-05-14 morning)

## Overview

UI System 负责影子回忆所有玩家界面的管理和呈现，基于 TEngine UIModule 的 UIWindow/UIWidget 架构构建 9 个 UIWindow 面板。系统管理 5 个 UI 层级（Background=100 / HUD=200 / Popup=300 / Overlay=400 / System=500），Popup/Overlay 层自动 push/pop InputBlocker 阻断游戏输入。

9 个面板包括：GameHUD（5 个 widget：HintButton、PuzzleIndicator、SaveIndicator、ChapterTitle、SettingsGear）、PauseMenu、PuzzleCompletePanel（auto-close 2.5s）、ChapterSelectScreen、MainMenuScreen、SettingsPanel、HintButton overlay、TutorialOverlay、Credits。Popup 队列 FIFO（最多 1 个可见）。Safe area 适配通过 `SetUISafeFitHelper` 实现。UI 动画保持 60fps，Gaussian blur 回退策略应对低端设备。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-011: UIWindow Management | 5 层 UI 分级；Popup/Overlay 自动 InputBlocker；9 UIWindow 定义；popup queue FIFO；safe area 适配；UIWindow 生命周期规范 | MEDIUM |

## GDD Requirements

| TR-ID | Requirement | ADR Coverage |
|-------|-------------|:------------:|
| TR-ui-001 | All UI via TEngine UIModule | ADR-011, ADR-001 ✅ |
| TR-ui-002 | 5 UI layer levels | ADR-011 ✅ |
| TR-ui-003 | Popup/Overlay auto InputBlocker | ADR-011, ADR-010 ✅ |
| TR-ui-004 | HUD pass-through to game | ADR-011 ✅ |
| TR-ui-005 | 9 UIWindows defined | ADR-011 ✅ |
| TR-ui-006 | GameHUD widgets (5) | ADR-011 ✅ |
| TR-ui-007 | Safe area fitting | ADR-011 ✅ |
| TR-ui-008 | Popup queue (1 visible) | ADR-011 ✅ |
| TR-ui-009 | TimeScale = 0 on PauseMenu | ADR-011 ⚠️ |
| TR-ui-010 | PuzzleCompletePanel auto-close 2.5s | ADR-011 ⚠️ |
| TR-ui-011 | Typewriter text effect | ADR-011 ⚠️ |
| TR-ui-012 | ChapterTransition 4-phase | ADR-011, ADR-009 ⚠️ |
| TR-ui-013 | Gaussian blur fallback | ADR-011 ⚠️ |
| TR-ui-014 | Animation scale accessibility | ❌ Deferred to ADR-020 (P2) |
| TR-ui-015 | HintButton opacity ramp | ADR-011, ADR-015 ⚠️ |
| TR-ui-016 | UI animations at 60fps | ADR-011, ADR-003 ✅ |
| TR-ui-017 | Gaussian blur < 2ms | ADR-011, ADR-003 ⚠️ |
| TR-ui-018 | UI prefab memory < 5MB | ADR-011, ADR-003 ✅ |
| TR-ui-019 | Touch target ≥ 44×44pt | ADR-011, ADR-003 ✅ |
| TR-ui-020 | Font size presets | ❌ Deferred to ADR-020 (P2) |
| TR-ui-021 | All text via localization keys | ❌ Deferred to ADR-022 (P2) |
| TR-ui-022 | Android back button | ADR-011 ⚠️ |

## Sprint 0 Findings Impact

- **SP-002 (UIWindow Lifecycle)**: 已确认生命周期调用时序——首次打开：`OnCreate → OnRefresh`（同帧）；重新打开：仅 `OnRefresh`；`OnUpdate` 仅在 `Visible=true` 时触发。组件引用获取放 `OnCreate`，数据刷新放 `OnRefresh`。
- **SP-009 (I2 Localization)**: 已确认 TEngine 内嵌 I2 Localization 封装。运行时语言切换无需重启场景。TR-ui-021 的本地化键替换在 P2 阶段实施。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from the GDD are verified
- All Logic and Integration stories have passing test files in `tests/`
- All Visual/Feel and UI stories have evidence docs in `production/qa/evidence/`

## Dependencies

- **chapter-state**: ChapterSelectScreen 和 GameHUD 需要查询章节/谜题进度数据
- **shadow-puzzle**: GameHUD 需要订阅 `MatchScoreChanged` 事件更新 PuzzleIndicator

## Stories

| Story ID | Title | Type | GDD Requirements | Status |
|----------|-------|------|-----------------|--------|
| ui-system-001 | UIModule Initialization + UIWindow Base Class Setup *(2026-05-11 Sprint 5 narrow scope amendment — Logic → Integration；Popup Queue/InputBlocker 移至 story-008 Sprint 6 polish)* | Integration | TR-ui-001 ✅ + TR-ui-002 ⚠️ partial (UILayer 枚举本 story；Popup/InputBlocker story-008) | **Draft (Sprint 5)** |
| ui-system-002 | GameHUD Window (Hint Button, Puzzle Progress, Interaction Prompts) | UI | TR-ui-004, TR-ui-006, TR-ui-015 | Ready |
| ui-system-003 | PauseMenu Window (Resume, Settings, Quit) | UI | TR-ui-005, TR-ui-009 | Ready |
| ui-system-004 | PuzzleComplete Window (Score Display, Continue) | UI | TR-ui-005, TR-ui-010 | Ready |
| ui-system-005 | ChapterSelect Window (Chapter List, Lock/Unlock State) | UI | TR-ui-005, TR-ui-003 | Blocked (chapter-state) |
| ui-system-006 | MainMenu UIWindow Polish — 4 Button Group + Vendor 7+2 Lifecycle + Fade-In + BGM Hook *(2026-05-13 evening Sprint 6 S6-07 V3.0.1 vendor reality compliant rewrite + 2026-05-14 morning Phase 2.0 R2 deficiency closure)* | UI / Integration | TR-ui-005 ✅ + TR-ui-016 ✅ partial (fade-in 60fps；BGM 完整 playback 留 ui-system-006b Sprint 7+) | **Phase 2 ready (Sprint 6 S6-07)** |
| ui-system-006b | MainMenu BGM Asset + Luban Entry *(衍生自 S6-07 Phase 2.0 R2.8 [D] mixed strategy closure 2026-05-14 morning — epic 边界 cleanup)* | Audio Asset / Integration | TR-ui-005 partial (BGM 完整 audio 体验补齐) | Backlog (Sprint 7+ Production polish phase) |
| ui-system-007 | SettingsPanel Window (Volume, Sensitivity, Language) | UI | TR-ui-005, TR-ui-003 | Ready |
| ui-system-008 | UIWindow Layer/Order Management — Popup Queue Verify + Auto InputBlocker Sender-Side (Top/Tips Layer) *(2026-05-13 Sprint 6 S6-08 V3.0.1 vendor reality compliant rewrite + sender-only narrow scope [A] — listener-side 留 Sprint 7+ ADR-010 InputManager epic)* | Logic / Integration | TR-ui-002 ✅ + TR-ui-003 partial (sender-side fire only；listener-side wiring Sprint 7+ ADR-010) + TR-ui-008 ✅ verify-only (vendor popup queue 已 production) | **Phase 1 ready (Sprint 6 S6-08)** |
| ui-system-009 | Safe Area Fitting for Notch/Rounded Corner Devices | Integration | TR-ui-007 | Ready |
| ui-system-010 | UI Text Localization via ILocalizationModule (SP-009) | Integration | TR-ui-021 | Ready (依赖 001-007) |

## Next Step

Run `/dev-story ui-system-001` to begin implementation. Recommended order: 001 → 008 → 002 → 003 → 004 → 006 → 007 → 009 → 010 → 005 (005 blocked on chapter-state).

---

## Sprint 5 Override (2026-05-11)

**S5-08 promote**: ui-system-001 promoted should-have → must-have (sprint-status.yaml `S5-08` entry)；Sprint 5 [A] serial 序列：S5-04 ✅ → **S5-08 (本 story narrow scope)** → S5-02 → S5-07。

**Narrow scope amendment**:

- ui-system-001 本 sprint **narrow scope** 实施（10 AC + R3 PlayMode probe 4 case；详 story file §History 2026-05-11 entry）：UILayer 枚举 + UIRoot Canvas runtime 实例化 + GameModule.UI 通路 + ShowWindow/CloseWindow API + Mock minimal panel lifecycle verify + Button onClick path verify
- **Popup Queue / Auto-Dequeue / Auto InputBlocker / Overlay limit / 双 InputBlocker 叠加** → 全部由 **ui-system-008** cover (Sprint 6 polish；不新建 ui-system-001b — story-008 已存在 9 AC + 5 TC 同 scope)
- **Full Main Menu UIWindow** (New Game / Continue / Settings 按钮 + 存档检查 + fade-in 动画 + 主菜单 BGM) → **ui-system-006** Sprint 6 polish；S5-02 内 minimal main menu (2 minimal inline Button) 基于 ui-system-001 API 通路实施，不实施完整 main menu

**Sprint 5 不实施 ui-system-002..-010 任何 story**；本 sprint 仅 ui-system-001 narrow scope。

**Sprint 4 carryover hard rule satisfaction (S3-08 → S4-09 → S5-08 第 2 次 carryover)**：
- ✅ Promote: S5-08 must-have + Sprint 5 dev-story 实施
- ✅ Descope rationale: 完整 robust UI infrastructure (Popup Queue / Auto InputBlocker 等) descope 到 story-008 Sprint 6 polish；明示 rationale

---

## Sprint 6 Override (2026-05-13 evening — 2026-05-14 morning)

**S6-07 ui-system-006 main menu UIWindow polish 实施进展** (Sprint 6 Session 29-30):

- **2026-05-13 evening (Session 29)**: ui-system-006 早期 Sprint 0 placeholder 11+ wording drift (ShowWindow / UILayer.HUD / 2 hook lifecycle / Evt_* event 等) → 完整 rewrite per V3.0.1 vendor reality compliant + S6-07 narrow scope [A] 4 button group (NewGame / Continue placeholder / Settings placeholder / Quit) + vendor 7+2 lifecycle `protected override` 强制 (per V3.0.1 dp7 NEW hotfix reinforce) + fade-in 0.3s DOTween OutQuad + BGM hook IAudioService.PlayMusic + R3 PlayMode probe 5 case + R2 Assumptions Validated 9 items；Phase 1 R1+R2+R3 readiness gate verdict ✅ READY (R2 DEFICIENCY-FLAGGED PASS R2.6+R2.8 ⚠️ TBD)。
- **2026-05-14 morning (Session 30)**: Phase 2.0 R2 deficiency flag closure ✅ — R2.6 ✅ FULLY RESOLVED (`AudioManager : Singleton<AudioManager>, IAudioService` + `GameApp.cs:40-55` init order ✅)；R2.8 ✅ DEFICIENCY CLOSURE per **[D] mixed strategy** (epic 边界遵守 — UI epic 不 polluted Audio config table；BGM hook 完整保留 + 走 PlayMusic fail-safe Log.Warning+no-op；新建 Sprint 7+ follow-on backlog story `ui-system-006b: main_menu_bgm asset + Luban entry` 1 SP；R3 P3 走 mock spy invocation assert)。R2 Verdict 升级 DEFICIENCY-FLAGGED PASS → ✅ FULLY PASS。Phase 2 production code 实施 ready。

**ui-system-006b Sprint 7+ backlog 衍生**: 跨 epic boundary deficiency 留 follow-on placeholder 模式实战 (epic 边界 cleanup governance precedent — 而非污染当前 sprint scope；详 ui-system-006b story file)。

**S6-08 ui-system-008 popup/inputblocker robust 实施进展** (Sprint 6 Session 30):

- **2026-05-13 morning (Session 30)**: ui-system-008 Sprint 0 placeholder 5 wording drift (ShowWindow/CloseWindow API 不存在；UILayer Background/HUD/Popup/Overlay/System → vendor 实际 Bottom/UI/Top/Tips/System；PauseMenuPanel/ChapterTransitionPanel/HUDPanel/SettingsPanel 假设面板未实施；IInputService.GetBlockerStack 不存在 API；FIFO popup queue → 实际 priority DESC + enqueueOrder ASC tiebreak) → 完整 rewrite per V3.0.1 vendor reality compliant + S6-08 sender-only narrow scope [A]。
- Phase 0 R2 vendor reality verify — 7 finding 实证：(1) UIModule.PopupQueue.cs popup queue 已 production (priority DESC + enqueueOrder ASC tiebreak) (2) UIModule.OnSortWindowDepth same-layer sorting 已 production (`depth = layer * LAYER_DEEP(2000) + N * WINDOW_DEEP(100)`) (3) InputBlocker.cs stack semantic 已 production + 9 unit test (4) `Singleton<InputBlocker>` / `class InputManager` 0-production — listener-side 留 Sprint 7+ ADR-010 (5) UIModule.ShowUI/CloseUI/HideUI 全 0-call IInputBlockerEvent (真 deficiency = Auto InputBlocker sender-side 未实施) (6) WindowAttribute UILayer enum 4 ctor 已 verify per S5-08 + UIWindow.WindowLayer 是 `int` (7) S5-05 NarrativeSequencePlayer 已 IInputBlockerEvent sender precedent (NarrativeSequencePlayer.cs:312/323)。
- Narrow scope [A] sender-only 决策 (~2 SP)：UIModule.ShowUIImp/CloseUI/HideUI 内对 Top(2)/Tips(3) layer panel 自动 fire `GameEvent.Get<IInputBlockerEvent>().OnPushBlocker/OnPopBlocker(token = type.FullName)`；listener-side InputBlocker singleton refactor + IInputBlockerEvent listener wiring + InputManager class 创建 + S5-05 NarrativeSequencePlayer fire-and-forget closure 留 Sprint 7+ ADR-010 InputManager epic 一并实施 (epic boundary cleanup 模式复 S6-07 R2.8 [D] mixed strategy precedent)；R3 走 spike subscribe IInputBlockerEvent assert event count + token format pattern (S5-05 P3 listener spy precedent)。
- Phase 1 story-008-ui-layer-strategy.md 完整 rewrite — 10 AC (sender Top/Tips fire + UI/Bottom/System contrast no-fire + popup queue priority + sorting + InputBlocker stack semantic verify-only + R3 30+ asserts) + 5 R3 PlayMode probe case + 9 R2 表 (R2.1~R2.6 ✅ FULLY；R2.7 ⚠️ DEFERRED Sprint 7+ ADR-010 epic boundary；R2.8 + R2.9 ⚠️ TBD Phase 2) + V3.0.1 Watch List Hooks (Type-9 dp1 closure 应用 R2.6 + Type-5 dp7 NEW reinforce mock fixture protected override + Type-5 dp6 不触发 + Type-8 dp1 可能触发 R3 P3 留观察 + **NEW dp8 candidate** DevTestState `[main-menu]` mode 复用 阈值阶进 V3.1 trigger 候选)。Phase 1 R1+R2+R3 readiness gate verdict ✅ **DEFICIENCY-FLAGGED PASS** ready for Phase 2 transition。

**ui-system-008 narrow scope [A] descope 残留 mapping** (S5-08 → S6-08 closure trail):

| S5-08 descope 残留 item | vendor reality | S6-08 closure |
|---|---|---|
| Popup Queue / Auto-Dequeue | ✅ vendor 已 production (priority DESC + ASC tiebreak — 不 FIFO) | ❌ 不补 production code (R3 P3 verify only) |
| **Auto InputBlocker (Popup/Overlay 自动 push)** | ❌ NOT implemented (UIModule 全 0-call IInputBlockerEvent) | ✅ **核心 work — sender-side UIModule fire** |
| Overlay limit (Tips 层 panel 数限制) | ⚠️ vendor 无 panel 数限制 | ❌ 不引入新限制 |
| 双 InputBlocker 叠加 (multi token stack) | ✅ InputBlocker.PopBlocker LastIndexOf 安全弹 | ❌ 不补 production code (R3 P3 verify) |
| 同层多 panel sorting | ✅ vendor OnSortWindowDepth 已 production | ❌ 不补 production code (R3 P4 verify) |
| **IInputBlockerEvent listener wiring** | ❌ NOT wired (0 production listener) | ⚠️ DEFERRED Sprint 7+ ADR-010 InputManager epic |

---
