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
| ui-system-008 | UIWindow Layer/Order Management (Normal, Popup, Overlay) | Logic | TR-ui-002, TR-ui-003, TR-ui-008 | Ready |
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

**S6-08 ui-system-008 popup/inputblocker robust** (Sprint 6 Must Have 2 SP) 仍 backlog — depends on S6-07 ✅ done (Sprint 6 Track C 序列：S6-07 → S6-08)。

---
